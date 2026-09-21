namespace Recognition.Core;

public sealed class RecognitionProfile
{
    public string Name { get; set; } = "Default";

    public int TargetFps { get; set; } = 60;

    public double CaptureScale { get; set; } = 1.0d;

    public int FrameHistoryRetentionSeconds { get; set; } = 10;

    public bool RetainSourceFramesInHistory { get; set; } = false;

    public PreviewDisplayMode PreviewMode { get; set; } = PreviewDisplayMode.Processed;

    public string PreviewModuleId { get; set; } = string.Empty;

    public bool ShowOcrPreviewLabels { get; set; } = true;

    public bool ShowDistancePreviewAnnotations { get; set; } = true;

    public bool ShowDistanceCursorPreview { get; set; } = true;

    public RecognitionEventMode EventMode { get; set; } = RecognitionEventMode.OnDetectedEnter;

    public RecognitionEventAction EventAction { get; set; } = RecognitionEventAction.Ocr;

    public int EventActionFrameOffset { get; set; }

    public ComponentConfiguration FrameSource { get; set; } = new();

    public List<ComponentConfiguration> Preprocessors { get; set; } = [];

    public ComponentConfiguration Recognizer { get; set; } = new();

    public bool OcrEnabled { get; set; } = true;

    public ComponentConfiguration OcrEngine { get; set; } = new();

    public List<ImageRecognitionTargetConfiguration> ImageRecognitionTargets { get; set; } = [];

    public List<OcrReferenceConfiguration> OcrReferences { get; set; } = [];

    public List<OcrTargetConfiguration> OcrTargets { get; set; } = [];

    public DistanceMeasurementConfiguration DistanceMeasurement { get; set; } = new();

    public RecognitionProfile Clone()
    {
        return new RecognitionProfile
        {
            Name = Name,
            TargetFps = TargetFps,
            CaptureScale = CaptureScale,
            FrameHistoryRetentionSeconds = FrameHistoryRetentionSeconds,
            RetainSourceFramesInHistory = RetainSourceFramesInHistory,
            PreviewMode = PreviewMode,
            PreviewModuleId = PreviewModuleId,
            ShowOcrPreviewLabels = ShowOcrPreviewLabels,
            ShowDistancePreviewAnnotations = ShowDistancePreviewAnnotations,
            ShowDistanceCursorPreview = ShowDistanceCursorPreview,
            EventMode = EventMode,
            EventAction = EventAction,
            EventActionFrameOffset = EventActionFrameOffset,
            FrameSource = FrameSource.Clone(),
            Preprocessors = [.. Preprocessors.Select(static preprocessor => preprocessor.Clone())],
            Recognizer = Recognizer.Clone(),
            OcrEnabled = OcrEnabled,
            OcrEngine = OcrEngine.Clone(),
            ImageRecognitionTargets = [.. ImageRecognitionTargets.Select(static target => target.Clone())],
            OcrReferences = [.. OcrReferences.Select(static reference => reference.Clone())],
            OcrTargets = [.. OcrTargets.Select(static target => target.Clone())],
            DistanceMeasurement = DistanceMeasurement.Clone()
        };
    }
}
