namespace Recognition.Wpf;

public sealed class RecognitionLogEntry(string timestamp, string eventDescription, string? recognizedText, double confidence)
{
    public string Timestamp { get; } = timestamp;

    public string EventDescription { get; } = eventDescription;

    public string? RecognizedText { get; } = recognizedText;

    public double Confidence { get; } = confidence;
}
