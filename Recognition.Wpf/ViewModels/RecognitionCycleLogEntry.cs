namespace Recognition.Wpf;

public sealed class RecognitionCycleLogEntry(
    string timestamp,
    string detectionState,
    string recognizerLabel,
    bool eventTriggered,
    string? recognizedText,
    double confidence,
    double fps)
{
    public string Timestamp { get; } = timestamp;

    public string DetectionState { get; } = detectionState;

    public string RecognizerLabel { get; } = recognizerLabel;

    public bool EventTriggered { get; } = eventTriggered;

    public string? RecognizedText { get; } = recognizedText;

    public double Confidence { get; } = confidence;

    public double FramesPerSecond { get; } = fps;
}
