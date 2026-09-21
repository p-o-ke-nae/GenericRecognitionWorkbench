namespace Recognition.Wpf;

public sealed class RecognitionCycleLogEntry(
    Guid cycleId,
    string timestamp,
    string detectionState,
    string recognizerLabel,
    bool eventTriggered,
    string? recognizedText,
    double confidence,
    double fps)
{
    public RecognitionCycleLogEntry(
        string timestamp,
        string detectionState,
        string recognizerLabel,
        bool eventTriggered,
        string? recognizedText,
        double confidence,
        double fps)
        : this(Guid.NewGuid(), timestamp, detectionState, recognizerLabel, eventTriggered, recognizedText, confidence, fps)
    {
    }

    public Guid CycleId { get; } = cycleId;

    public string Timestamp { get; } = timestamp;

    public string DetectionState { get; } = detectionState;

    public string RecognizerLabel { get; } = recognizerLabel;

    public bool EventTriggered { get; } = eventTriggered;

    public string? RecognizedText { get; } = recognizedText;

    public double Confidence { get; } = confidence;

    public double FramesPerSecond { get; } = fps;
}
