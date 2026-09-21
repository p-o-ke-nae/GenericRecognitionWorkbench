namespace Recognition.Wpf;

public sealed class RecognitionLogEntry(Guid cycleId, string timestamp, string eventDescription, string? recognizedText, double confidence)
{
    public RecognitionLogEntry(string timestamp, string eventDescription, string? recognizedText, double confidence)
        : this(Guid.NewGuid(), timestamp, eventDescription, recognizedText, confidence)
    {
    }

    public Guid CycleId { get; } = cycleId;

    public string Timestamp { get; } = timestamp;

    public string EventDescription { get; } = eventDescription;

    public string? RecognizedText { get; } = recognizedText;

    public double Confidence { get; } = confidence;
}
