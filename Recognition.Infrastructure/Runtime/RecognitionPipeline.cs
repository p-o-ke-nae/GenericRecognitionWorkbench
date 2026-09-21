using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed record RecognitionPipeline(
    IFrameSource FrameSource,
    IReadOnlyList<IImageProcessor> Processors,
    IRecognitionMethod RecognitionMethod,
    IOcrEngine? OcrEngine,
    IReadOnlyList<ImageRecognitionPipelineTarget> ImageRecognitionTargets,
    OcrReferenceEvaluator? OcrReferences,
    IReadOnlyList<OcrPipelineTarget> OcrTargets,
    DistanceMeasurementEvaluator? DistanceMeasurement) : IDisposable
{
    public void Dispose()
    {
        foreach (var processor in Processors.OfType<IDisposable>())
        {
            processor.Dispose();
        }

        foreach (var targetProcessor in OcrTargets.SelectMany(static target => target.Processors).OfType<IDisposable>())
        {
            targetProcessor.Dispose();
        }

        foreach (var targetProcessor in ImageRecognitionTargets.SelectMany(static target => target.Processors).OfType<IDisposable>())
        {
            targetProcessor.Dispose();
        }

        foreach (var recognizer in ImageRecognitionTargets.Select(static target => target.Recognizer))
        {
            recognizer.Dispose();
        }

        DistanceMeasurement?.Dispose();
        OcrReferences?.Dispose();
        OcrEngine?.Dispose();
        RecognitionMethod.Dispose();
        FrameSource.Dispose();
    }
}
