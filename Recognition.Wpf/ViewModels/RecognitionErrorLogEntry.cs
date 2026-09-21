namespace Recognition.Wpf;

public sealed class RecognitionErrorLogEntry(
    string timestamp,
    string source,
    string summary,
    string details)
{
    public string Timestamp { get; } = timestamp;

    public string Source { get; } = source;

    public string Summary { get; } = summary;

    public string Details { get; } = details;
}
