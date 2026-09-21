namespace Recognition.Core;

public sealed class RecognitionCycleResult
{
    public required DateTimeOffset Timestamp { get; init; }

    public required RecognitionFrame SourceFrame { get; init; }

    public required RecognitionFrame PreviewFrame { get; init; }

    public required bool IsDetected { get; init; }

    public required bool EventTriggered { get; init; }

    public required double DetectionConfidence { get; init; }

    public RoiArea? MatchedRegion { get; init; }

    public RoiArea? SourceMatchedRegion { get; init; }

    public string? RecognizedText { get; init; }

    public double? OcrConfidence { get; init; }

    public IReadOnlyList<OcrPreviewFrame> OcrPreviewFrames { get; init; } = [];

    public IReadOnlyList<DistanceMeasurementPreviewItem> DistanceMeasurementPreviewItems { get; init; } = [];

    public RecognitionFrame? DistanceMeasurementPreviewFrame { get; init; }

    public required double FramesPerSecond { get; init; }

    public string? StructuredDataJson { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
