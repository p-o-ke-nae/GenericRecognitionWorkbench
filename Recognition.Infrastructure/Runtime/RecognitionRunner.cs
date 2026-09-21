using System.Diagnostics;
using System.Text.Json;
using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class RecognitionRunner(IRecognitionPluginCatalog pluginCatalog) : IRecognitionRunner
{
    public Task<RecognitionCycleResult> ExecuteOnceAsync(RecognitionProfile profile, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            using var pipeline = BuildPipeline(profile);
            var state = new RecognitionRuntimeState();
            state.FrameHistory = new RecognitionFrameHistory(TimeSpan.FromSeconds(Math.Max(0, profile.FrameHistoryRetentionSeconds)));
            return ExecuteCycle(profile, pipeline, state, forceOcrWhenDetected: true, runActionsForTest: true, cancellationToken);
        }, cancellationToken);
    }

    public IRecognitionSession CreateContinuousSession(RecognitionProfile profile)
    {
        return new RecognitionSession(profile.Clone(), this);
    }

    internal RecognitionCycleResult ExecuteCycle(
        RecognitionProfile profile,
        RecognitionPipeline pipeline,
        RecognitionRuntimeState state,
        bool forceOcrWhenDetected,
        bool runActionsForTest,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var capturedFrame = pipeline.FrameSource.CaptureAsync(cancellationToken).AsTask().GetAwaiter().GetResult();
        var frame = RecognitionFrameScaler.Scale(capturedFrame, profile.CaptureScale);
        var processed = frame;
        var sourceOffsetX = 0;
        var sourceOffsetY = 0;
        foreach (var processor in pipeline.Processors)
        {
            if (processor is CropImageProcessor cropProcessor && !cropProcessor.Roi.IsEmpty)
            {
                sourceOffsetX += cropProcessor.Roi.X;
                sourceOffsetY += cropProcessor.Roi.Y;
            }

            processed = processor.Process(processed);
        }

        BufferFrame(state, now, frame, processed, Math.Abs(profile.EventActionFrameOffset));
        state.FrameHistory ??= new RecognitionFrameHistory(TimeSpan.FromSeconds(Math.Max(0, profile.FrameHistoryRetentionSeconds)));
        state.FrameHistory.Add(now, frame, processed);
        var match = pipeline.RecognitionMethod.Evaluate(processed);
        RoiArea? sourceMatchedRegion = match.Region is { } matchedRegion
            ? new RoiArea(matchedRegion.X + sourceOffsetX, matchedRegion.Y + sourceOffsetY, matchedRegion.Width, matchedRegion.Height)
            : null;
        var shouldTrigger = RecognitionEventEvaluator.ShouldTrigger(profile.EventMode, state.PreviousDetected, match.IsDetected);
        var eventFrameOffset = profile.EventActionFrameOffset;
        var pastFrameOffset = Math.Max(0, -eventFrameOffset);
        var futureFrameOffset = !forceOcrWhenDetected && profile.EventMode == RecognitionEventMode.OnDetectedEnter
            ? Math.Max(0, eventFrameOffset)
            : 0;
        var delayedEventTriggered = false;
        var shouldRunEventActions = runActionsForTest || (forceOcrWhenDetected && match.IsDetected);
        var eventTriggered = forceOcrWhenDetected && match.IsDetected;

        if (futureFrameOffset > 0)
        {
            if (shouldTrigger)
            {
                state.PendingFutureActionFramesRemaining = futureFrameOffset;
            }
            else if (state.PendingFutureActionFramesRemaining > 0)
            {
                state.PendingFutureActionFramesRemaining--;
                delayedEventTriggered = state.PendingFutureActionFramesRemaining == 0;
            }

            shouldRunEventActions |= delayedEventTriggered;
            eventTriggered |= delayedEventTriggered;
        }
        else
        {
            shouldRunEventActions |= shouldTrigger;
            eventTriggered |= shouldTrigger;
        }

        RecognitionFrame? eventSourceFrame = null;
        RecognitionFrame? eventPreviewFrame = null;
        if (shouldRunEventActions)
        {
            var eventPair = futureFrameOffset > 0
                ? (now, frame, processed)
                : GetBufferedFrame(state, pastFrameOffset) ?? (now, frame, processed);
            eventSourceFrame = eventPair.Item2.Clone();
            eventPreviewFrame = eventPair.Item3.Clone();
        }

        OcrExecutionResult? ocrExecution = null;
        if (eventSourceFrame is not null && pipeline.OcrEngine is not null && SupportsOcr(profile))
        {
            ocrExecution = RunOcrTargets(eventSourceFrame, pipeline, sourceMatchedRegion);
        }

        ImageRecognitionExecutionResult? imageRecognitionExecution = null;
        if (eventSourceFrame is not null && pipeline.ImageRecognitionTargets.Count > 0)
        {
            imageRecognitionExecution = RunImageRecognitionTargets(eventSourceFrame, pipeline);
        }

        DistanceMeasurementResult? distanceMeasurement = null;
        if (eventSourceFrame is not null && pipeline.DistanceMeasurement is not null && SupportsDistanceMeasurement(profile))
        {
            distanceMeasurement = pipeline.DistanceMeasurement.Measure(eventSourceFrame);
        }

        var currentTimestamp = Stopwatch.GetTimestamp();
        var fps = 0d;
        if (state.LastTimestamp != 0)
        {
            fps = Stopwatch.Frequency / (double)(currentTimestamp - state.LastTimestamp);
        }

        state.LastTimestamp = currentTimestamp;

        var outputText = string.Join(
            Environment.NewLine,
            new[] { ocrExecution?.Result.Text, distanceMeasurement?.Text }
                .Where(static text => !string.IsNullOrWhiteSpace(text)));

        var result = new RecognitionCycleResult
        {
            Timestamp = now,
            SourceFrame = eventSourceFrame?.Clone() ?? frame.Clone(),
            PreviewFrame = eventPreviewFrame?.Clone() ?? processed.Clone(),
            IsDetected = match.IsDetected,
            EventTriggered = eventTriggered,
            DetectionConfidence = match.Confidence,
            MatchedRegion = match.Region,
            SourceMatchedRegion = sourceMatchedRegion,
            RecognizedText = string.IsNullOrWhiteSpace(outputText) ? null : outputText,
            OcrConfidence = ocrExecution?.Result.Confidence,
            OcrPreviewFrames = ocrExecution?.PreviewFrames ?? [],
            DistanceMeasurementPreviewItems = distanceMeasurement?.Items ?? [],
            DistanceMeasurementPreviewFrame = distanceMeasurement?.PreviewFrame,
            FramesPerSecond = fps,
            StructuredDataJson = BuildStructuredDataJson(match, ocrExecution, imageRecognitionExecution, distanceMeasurement),
            Metadata = new Dictionary<string, string>
            {
                ["RecognizerLabel"] = match.Label ?? string.Empty,
                ["EventMode"] = profile.EventMode.ToString()
            }
        };

        state.PreviousDetected = match.IsDetected;
        return result;
    }

    internal RecognitionPipeline BuildPipeline(RecognitionProfile profile)
    {
        var frameSourceFactory = pluginCatalog.FrameSourceFactories.FirstOrDefault(factory => string.Equals(factory.Descriptor.Id, profile.FrameSource.ComponentId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Frame source '{profile.FrameSource.ComponentId}' was not found.");
        var recognizerFactory = pluginCatalog.RecognitionMethodFactories.FirstOrDefault(factory => string.Equals(factory.Descriptor.Id, profile.Recognizer.ComponentId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Recognition method '{profile.Recognizer.ComponentId}' was not found.");

        var processors = profile.Preprocessors
            .Select(configuration =>
            {
                var factory = pluginCatalog.ImageProcessorFactories.FirstOrDefault(candidate => string.Equals(candidate.Descriptor.Id, configuration.ComponentId, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException($"Image processor '{configuration.ComponentId}' was not found.");
                return factory.Create(configuration.Parameters);
            })
            .ToArray();

        IOcrEngine? ocrEngine = null;
        if (profile.OcrEnabled
            && profile.OcrTargets.Count > 0
            && !string.IsNullOrWhiteSpace(profile.OcrEngine.ComponentId))
        {
            var ocrFactory = pluginCatalog.OcrEngineFactories.FirstOrDefault(factory => string.Equals(factory.Descriptor.Id, profile.OcrEngine.ComponentId, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"OCR engine '{profile.OcrEngine.ComponentId}' was not found.");
            ocrEngine = ocrFactory.Create(profile.OcrEngine.Parameters);
        }

        var ocrReferences = new OcrReferenceEvaluator(profile.OcrReferences);
        var imageRecognitionTargets = profile.ImageRecognitionTargets.Select(BuildImageRecognitionTarget).ToArray();
        var ocrTargets = profile.OcrTargets.Select(BuildOcrTarget).ToArray();
        var distanceMeasurement = SupportsDistanceMeasurement(profile)
            && (profile.DistanceMeasurement.References.Count > 0
                || profile.DistanceMeasurement.Targets.Count > 0
                || !string.IsNullOrWhiteSpace(profile.DistanceMeasurement.ReferenceTemplatePath)
                || !string.IsNullOrWhiteSpace(profile.DistanceMeasurement.TargetTemplatePath))
            ? new DistanceMeasurementEvaluator(profile.DistanceMeasurement)
            : null;

        return new RecognitionPipeline(
            frameSourceFactory.Create(profile.FrameSource.Parameters),
            processors,
            recognizerFactory.Create(profile.Recognizer.Parameters),
            ocrEngine,
            imageRecognitionTargets,
            ocrReferences,
            ocrTargets,
            distanceMeasurement);
    }

    private ImageRecognitionPipelineTarget BuildImageRecognitionTarget(ImageRecognitionTargetConfiguration configuration)
    {
        var recognizerFactory = pluginCatalog.RecognitionMethodFactories.FirstOrDefault(factory => string.Equals(factory.Descriptor.Id, configuration.Recognizer.ComponentId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Image recognition method '{configuration.Recognizer.ComponentId}' was not found.");
        var processors = configuration.Preprocessors
            .Select(preprocessorConfiguration =>
            {
                var factory = pluginCatalog.ImageProcessorFactories.FirstOrDefault(candidate => string.Equals(candidate.Descriptor.Id, preprocessorConfiguration.ComponentId, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException($"Image recognition processor '{preprocessorConfiguration.ComponentId}' was not found.");
                return factory.Create(preprocessorConfiguration.Parameters);
            })
            .ToArray();

        return new ImageRecognitionPipelineTarget(
            configuration.Name,
            recognizerFactory.Create(configuration.Recognizer.Parameters),
            processors);
    }

    private OcrPipelineTarget BuildOcrTarget(OcrTargetConfiguration configuration)
    {
        var processors = configuration.Preprocessors
            .Select(preprocessorConfiguration =>
            {
                var factory = pluginCatalog.ImageProcessorFactories.FirstOrDefault(candidate => string.Equals(candidate.Descriptor.Id, preprocessorConfiguration.ComponentId, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException($"OCR image processor '{preprocessorConfiguration.ComponentId}' was not found.");
                return factory.Create(preprocessorConfiguration.Parameters);
            })
            .ToArray();

        return new OcrPipelineTarget(configuration.Name, configuration.Region, configuration.ReferenceName, configuration.UseRecognitionAnchor, processors);
    }

    private static OcrExecutionResult RunOcrTargets(RecognitionFrame frame, RecognitionPipeline pipeline, RoiArea? anchorRegion)
    {
        var targets = pipeline.OcrTargets;
        var referenceLocations = pipeline.OcrReferences?.Locate(frame) ?? new Dictionary<string, RoiArea>(StringComparer.OrdinalIgnoreCase);

        var texts = new List<string>();
        var confidences = new List<double>();
        var blocks = new List<OcrTextBlock>();
        var previewFrames = new List<OcrPreviewFrame>();
        var resultsByTarget = new Dictionary<string, OcrResult>(StringComparer.OrdinalIgnoreCase);

        foreach (var target in targets)
        {
            var resolvedRegion = ResolveOcrTargetRegion(target, anchorRegion, referenceLocations);
            var targetFrame = ExtractOcrFrame(frame, resolvedRegion);
            foreach (var processor in target.Processors)
            {
                targetFrame = processor.Process(targetFrame);
            }

            previewFrames.Add(new OcrPreviewFrame(
                target.Name,
                targetFrame.Clone(),
                resolvedRegion.IsEmpty ? new RoiArea(0, 0, frame.Width, frame.Height) : resolvedRegion));
            var result = pipeline.OcrEngine!.Read(targetFrame);
            resultsByTarget[target.Name] = result;
            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                texts.Add($"{target.Name}: {result.Text}");
            }

            confidences.Add(result.Confidence);
            blocks.AddRange(result.Blocks);
        }

        return new OcrExecutionResult(
            new OcrResult(
                string.Join(Environment.NewLine, texts),
                confidences.Count == 0 ? 0d : confidences.Average(),
                blocks),
            previewFrames,
            resultsByTarget);
    }

    private static RoiArea ResolveOcrTargetRegion(OcrPipelineTarget target, RoiArea? anchorRegion, IReadOnlyDictionary<string, RoiArea> referenceLocations)
    {
        if (target.UseRecognitionAnchor
            && !string.IsNullOrWhiteSpace(target.ReferenceName)
            && referenceLocations.TryGetValue(target.ReferenceName, out var referenceRegion))
        {
            return new RoiArea(
                referenceRegion.X + target.Region.X,
                referenceRegion.Y + target.Region.Y,
                target.Region.Width,
                target.Region.Height);
        }

        if (target.UseRecognitionAnchor && anchorRegion is { IsEmpty: false } anchor)
        {
            return new RoiArea(
                anchor.X + target.Region.X,
                anchor.Y + target.Region.Y,
                target.Region.Width,
                target.Region.Height);
        }

        return target.Region;
    }

    private static RecognitionFrame ExtractOcrFrame(RecognitionFrame frame, RoiArea region)
    {
        if (region.IsEmpty)
        {
            return frame.Clone();
        }

        using var mat = OpenCvFrameConversion.ToMat(frame);
        using var cropped = OpenCvFrameConversion.Crop(mat, region);
        return cropped.Empty()
            ? frame.Clone()
            : OpenCvFrameConversion.ToFrame(cropped, frame.CapturedAt);
    }

    private static bool SupportsOcr(RecognitionProfile profile)
    {
        return profile.OcrEnabled
            && profile.EventAction is RecognitionEventAction.Ocr or RecognitionEventAction.OcrAndDistance;
    }

    private static bool SupportsDistanceMeasurement(RecognitionProfile profile)
    {
        return profile.EventAction is RecognitionEventAction.DistanceMeasurement or RecognitionEventAction.OcrAndDistance;
    }

    private static ImageRecognitionExecutionResult RunImageRecognitionTargets(RecognitionFrame frame, RecognitionPipeline pipeline)
    {
        var results = new Dictionary<string, RecognitionMatch>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in pipeline.ImageRecognitionTargets)
        {
            var workingFrame = frame.Clone();
            foreach (var processor in target.Processors)
            {
                workingFrame = processor.Process(workingFrame);
            }

            results[target.Name] = target.Recognizer.Evaluate(workingFrame);
        }

        return new ImageRecognitionExecutionResult(results);
    }

    private static string BuildStructuredDataJson(RecognitionMatch match, OcrExecutionResult? ocrExecution, ImageRecognitionExecutionResult? imageRecognitionExecution, DistanceMeasurementResult? distanceMeasurement)
    {
        var detection = new
        {
            isDetected = match.IsDetected,
            confidence = match.Confidence,
            label = match.Label,
            region = match.Region is { } region
                ? new { x = region.X, y = region.Y, width = region.Width, height = region.Height }
                : null
        };

        var ocr = (ocrExecution?.PreviewFrames ?? [])
            .Select((preview, index) =>
            {
                ocrExecution!.ResultsByTarget.TryGetValue(preview.Name, out var targetResult);
                return new
                {
                    key = preview.Name,
                    value = new
                    {
                        text = targetResult?.Text ?? string.Empty,
                        confidence = targetResult?.Confidence ?? 0d,
                        blocks = (targetResult?.Blocks ?? []).Select(static block => new { text = block.Text, confidence = block.Confidence }).ToArray(),
                        region = new { x = preview.Region.X, y = preview.Region.Y, width = preview.Region.Width, height = preview.Region.Height },
                        order = index
                    }
                };
            })
            .ToDictionary(static item => item.key, static item => item.value, StringComparer.OrdinalIgnoreCase);

        var distance = (distanceMeasurement?.Items ?? [])
            .ToDictionary(
                static item => item.TargetName,
                static item => new
                {
                    referenceName = item.ReferenceName,
                    deltaX = item.DeltaX,
                    deltaY = item.DeltaY,
                    referenceRegion = new { x = item.ReferenceRegion.X, y = item.ReferenceRegion.Y, width = item.ReferenceRegion.Width, height = item.ReferenceRegion.Height },
                    targetRegion = new { x = item.TargetRegion.X, y = item.TargetRegion.Y, width = item.TargetRegion.Width, height = item.TargetRegion.Height }
                },
                StringComparer.OrdinalIgnoreCase);

        var imageRecognition = (imageRecognitionExecution?.ResultsByTarget ?? new Dictionary<string, RecognitionMatch>(StringComparer.OrdinalIgnoreCase))
            .ToDictionary(
                static item => item.Key,
                static item => new
                {
                    isDetected = item.Value.IsDetected,
                    confidence = item.Value.Confidence,
                    label = item.Value.Label,
                    region = item.Value.Region is { } region
                        ? new { x = region.X, y = region.Y, width = region.Width, height = region.Height }
                        : null
                },
                StringComparer.OrdinalIgnoreCase);

        var payload = new
        {
            detection,
            ocr,
            distanceMeasurement = distance,
            imageRecognition
        };

        return JsonSerializer.Serialize(payload);
    }

    private static void BufferFrame(RecognitionRuntimeState state, DateTimeOffset timestamp, RecognitionFrame source, RecognitionFrame processed, int frameOffset)
    {
        state.BufferedFrames.Add((timestamp, source.Clone(), processed.Clone()));
        var maxBufferedFrames = Math.Max(1, frameOffset + 1);
        while (state.BufferedFrames.Count > maxBufferedFrames)
            state.BufferedFrames.RemoveAt(0);
    }

    private static (DateTimeOffset Timestamp, RecognitionFrame Source, RecognitionFrame Processed)? GetBufferedFrame(RecognitionRuntimeState state, int frameOffset)
    {
        if (state.BufferedFrames.Count == 0) return null;
        var index = Math.Max(0, state.BufferedFrames.Count - 1 - Math.Max(0, frameOffset));
        return state.BufferedFrames[index];
    }
}