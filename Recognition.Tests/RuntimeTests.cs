using System.Diagnostics;
using System.Globalization;
using Recognition.Core;
using Recognition.Infrastructure;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Recognition.Tests;

public sealed class RuntimeTests(ITestOutputHelper output)
{
    private const int SourceBytes = 1280 * 720 * 3;
    private const int ProcessedBytes = 77 * 80 * 3;

    [Fact]
    public async Task MemoryPlateausWithRealSessionAndCroppedHistory()
    {
        var runner = new RecognitionRunner(new SyntheticCatalog());
        await using var session = runner.CreateContinuousSession(CreateProfile());
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(40));
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var watch = Stopwatch.StartNew();
        long baseline = 0;
        long final = 0;
        var cycles = 0;
        RecognitionFrameHistory? history = null;
        session.Failed += (_, error) => done.TrySetException(error);
        session.CycleCompleted += (_, result) =>
        {
            cycles++;
            history = result.FrameHistory;
            if (baseline == 0 && watch.Elapsed.TotalSeconds >= 10.5)
            {
                baseline = GC.GetTotalMemory(true);
                using var baselineProcess = Process.GetCurrentProcess();
                output.WriteLine($"Baseline working set={baselineProcess.WorkingSet64:N0}");
            }
            if (watch.Elapsed.TotalSeconds >= 21 && cycles >= 1200)
            {
                final = GC.GetTotalMemory(true);
                done.TrySetResult();
                cancellation.Cancel();
            }
        };
        await session.StartAsync(cancellation.Token);
        await done.Task.WaitAsync(TimeSpan.FromSeconds(40));
        await session.StopAsync();
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        output.WriteLine($"Cycles={cycles}, live baseline={baseline:N0}, final={final:N0}, working set={process.WorkingSet64:N0}");
        output.WriteLine($"GC committed={GC.GetGCMemoryInfo().TotalCommittedBytes:N0}, fragmented={GC.GetGCMemoryInfo().FragmentedBytes:N0}");
        Assert.True(final < baseline * 1.1, $"Memory grew from {baseline} to {final}");
        Assert.True(process.WorkingSet64 < 1500L * 1024 * 1024);
        Assert.NotNull(history);
        Assert.InRange(history.Entries.Count, 1, 800);
        Assert.All(history.Entries, static entry =>
        {
            Assert.Null(entry.SourceFrame);
            Assert.False(entry.HasSourceFrame);
            Assert.Equal(77, entry.ProcessedFrame.Width);
            Assert.Equal(80, entry.ProcessedFrame.Height);
        });
    }

    [Fact]
    public void OneCycleAllocatesOnlySourceCropAndSmallOverhead()
    {
        var runner = new RecognitionRunner(new SyntheticCatalog());
        var profile = CreateProfile();
        using var pipeline = runner.BuildPipeline(profile);
        var state = new RecognitionRuntimeState();
        for (var i = 0; i < 30; i++)
            runner.ExecuteCycle(profile, pipeline, state, false, false, default);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = runner.ExecuteCycle(profile, pipeline, state, false, false, default);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        output.WriteLine($"Cycle allocated {allocated:N0} bytes");
        Assert.InRange(allocated, SourceBytes + ProcessedBytes, SourceBytes + ProcessedBytes + 64 * 1024);
        Assert.Same(result.SourceFrame, state.BufferedFrames[^1].Source);
        Assert.Same(result.PreviewFrame, state.BufferedFrames[^1].Processed);
        Assert.Same(result.PreviewFrame, state.FrameHistory!.Entries[^1].ProcessedFrame);
        Assert.NotEqual(Guid.Empty, result.CycleId);
        Assert.Equal(result.CycleId, state.FrameHistory.Entries[^1].CycleId);
        Assert.Same(result.SourceFrame, result.CycleSourceFrame);
        Assert.Same(result.PreviewFrame, result.CyclePreviewFrame);
        Assert.True(double.Parse(result.Metadata["CycleMilliseconds"], CultureInfo.InvariantCulture) >= 0);
        Assert.Contains(result.Metadata["TargetPeriodExceeded"], new[] { "true", "false" });
        Assert.Equal("31", result.Metadata["FrameSequence"]);
        Assert.Equal("0", result.Metadata["DroppedFrames"]);
        Assert.True(result.Metadata.ContainsKey("RecognizerLabel"));
        Assert.True(result.Metadata.ContainsKey("EventMode"));
    }

    [Fact]
    public void DelayedEventUsesSharedBufferedFrame()
    {
        var runner = new RecognitionRunner(new SyntheticCatalog());
        var profile = CreateProfile();
        profile.EventActionFrameOffset = -1;
        using var pipeline = runner.BuildPipeline(profile);
        var state = new RecognitionRuntimeState();
        var previous = runner.ExecuteCycle(profile, pipeline, state, false, false, default);
        var current = runner.ExecuteCycle(profile, pipeline, state, true, true, default);
        Assert.Same(previous.SourceFrame, current.SourceFrame);
        Assert.Same(previous.PreviewFrame, current.PreviewFrame);
        Assert.NotSame(current.CycleSourceFrame, current.SourceFrame);
        Assert.NotSame(current.CyclePreviewFrame, current.PreviewFrame);
        Assert.Equal(current.CycleId, state.FrameHistory!.Entries[^1].CycleId);
        Assert.Same(current.CyclePreviewFrame, state.FrameHistory.Entries[^1].ProcessedFrame);
    }

    [Fact]
    public void DiagnosticsUseInvariantCultureAndCompareTargetPeriod()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var runner = new RecognitionRunner(new SyntheticCatalog());
            var profile = CreateProfile();
            using var pipeline = runner.BuildPipeline(profile);
            var state = new RecognitionRuntimeState();
            profile.TargetFps = int.MaxValue;
            var exceeded = runner.ExecuteCycle(profile, pipeline, state, false, false, default);
            Assert.Equal("true", exceeded.Metadata["TargetPeriodExceeded"]);
            Assert.DoesNotContain(",", exceeded.Metadata["CycleMilliseconds"]);
            profile.TargetFps = 0;
            var unlimited = runner.ExecuteCycle(profile, pipeline, state, false, false, default);
            Assert.Equal("false", unlimited.Metadata["TargetPeriodExceeded"]);
        }

        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public async Task ExecuteOnceMarksSingleShotWithoutChangingLiveSource()
    {
        var result = await new RecognitionRunner(new SyntheticCatalog()).ExecuteOnceAsync(CreateProfile());
        Assert.True(result.IsSingleShot);
        Assert.Equal(1280, result.SourceFrame.Width);
        Assert.Equal(77, result.PreviewFrame.Width);
        Assert.Null(Assert.Single(result.FrameHistory!.Entries).SourceFrame);
    }

    [Fact]
    public async Task ExecuteOnceWithSuppliedSourceFrameSkipsCaptureAndCaptureDiagnostics()
    {
        var catalog = new SyntheticCatalog();
        catalog.SourceFactory.ThrowOnCapture = true;
        var runner = new RecognitionRunner(catalog);
        var sourceFrame = new RecognitionFrame(new byte[SourceBytes], 1280, 720, 1280 * 3, FramePixelFormat.Bgr24, DateTimeOffset.UtcNow);

        var result = await runner.ExecuteOnceAsync(CreateProfile(), sourceFrame);

        Assert.True(result.IsSingleShot);
        Assert.Same(sourceFrame, result.SourceFrame);
        Assert.Equal(77, result.PreviewFrame.Width);
        Assert.Equal(0, catalog.SourceFactory.CaptureCount);
        Assert.DoesNotContain("FrameSequence", result.Metadata.Keys);
        Assert.DoesNotContain("DroppedFrames", result.Metadata.Keys);
    }

    private static RecognitionProfile CreateProfile() => new()
    {
        TargetFps = 60,
        FrameHistoryRetentionSeconds = 10,
        OcrEnabled = false,
        FrameSource = new() { ComponentId = "synthetic" },
        Recognizer = new() { ComponentId = "trivial" },
        Preprocessors =
        [
            new()
            {
                ComponentId = "builtin.preprocess.crop",
                Parameters = new() { ["X"] = "0", ["Y"] = "0", ["Width"] = "77", ["Height"] = "80" }
            }
        ]
    };

    private sealed class SyntheticCatalog : IRecognitionPluginCatalog
    {
        public SourceFactory SourceFactory { get; } = new();
        public IReadOnlyList<IFrameSourceFactory> FrameSourceFactories => [SourceFactory];
        public IReadOnlyList<IRecognitionMethodFactory> RecognitionMethodFactories { get; } = [new MethodFactory()];
        public IReadOnlyList<IImageProcessorFactory> ImageProcessorFactories { get; } = [new CropImageProcessorFactory()];
        public IReadOnlyList<IOcrEngineFactory> OcrEngineFactories => [];
        public void ReloadPlugins() { }
    }

    private sealed class SourceFactory : IFrameSourceFactory
    {
        public int CaptureCount { get; private set; }

        public bool ThrowOnCapture { get; set; }

        public ComponentDescriptor Descriptor { get; } = new("synthetic", "Synthetic", "", []);

        public IFrameSource Create(IReadOnlyDictionary<string, string> parameters) => new SyntheticSource(this);

        public void RecordCapture()
        {
            CaptureCount++;
            if (ThrowOnCapture)
            {
                throw new InvalidOperationException("Capture should not have been called.");
            }
        }
    }

    private sealed class SyntheticSource(SourceFactory owner) : IFrameSource, IFrameSourceDiagnostics
    {
        private readonly byte[][] buffers = [new byte[SourceBytes], new byte[SourceBytes]];
        public long FrameSequence { get; private set; }
        public long DroppedFrames => 0;

        public ValueTask<RecognitionFrame> CaptureAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            owner.RecordCapture();
            var buffer = buffers[++FrameSequence % 2];
            buffer[0] = (byte)FrameSequence;
            var owned = new byte[SourceBytes];
            Buffer.BlockCopy(buffer, 0, owned, 0, SourceBytes);
            return ValueTask.FromResult(new RecognitionFrame(owned, 1280, 720, 1280 * 3, FramePixelFormat.Bgr24, DateTimeOffset.UtcNow));
        }

        public void Dispose() { }
    }

    private sealed class MethodFactory : IRecognitionMethodFactory
    {
        public ComponentDescriptor Descriptor { get; } = new("trivial", "Trivial", "", []);
        public IRecognitionMethod Create(IReadOnlyDictionary<string, string> parameters) => new TrivialMethod();
    }

    private sealed class TrivialMethod : IRecognitionMethod
    {
        public RecognitionMatch Evaluate(RecognitionFrame frame) => new(true, 1);
        public void Dispose() { }
    }

}
