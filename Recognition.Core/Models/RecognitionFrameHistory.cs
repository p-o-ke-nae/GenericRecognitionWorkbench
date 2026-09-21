namespace Recognition.Core;

public sealed record RecognitionFrameHistoryEntry(
    DateTimeOffset Timestamp,
    RecognitionFrame SourceFrame,
    RecognitionFrame ProcessedFrame)
{
    public RecognitionFrameHistoryEntry Copy()
    {
        return new RecognitionFrameHistoryEntry(Timestamp, SourceFrame.Clone(), ProcessedFrame.Clone());
    }
}

public sealed class RecognitionFrameHistory
{
    private readonly List<RecognitionFrameHistoryEntry> entries = [];

    public RecognitionFrameHistory(TimeSpan retention)
    {
        Retention = retention < TimeSpan.Zero ? TimeSpan.Zero : retention;
    }

    public TimeSpan Retention { get; }

    public IReadOnlyList<RecognitionFrameHistoryEntry> Entries => entries;

    public void Add(DateTimeOffset timestamp, RecognitionFrame sourceFrame, RecognitionFrame processedFrame)
    {
        entries.Add(new RecognitionFrameHistoryEntry(timestamp, sourceFrame.Clone(), processedFrame.Clone()));
        Trim(timestamp);
    }

    public void Clear() => entries.Clear();

    private void Trim(DateTimeOffset newestTimestamp)
    {
        var cutoff = newestTimestamp - Retention;
        while (entries.Count > 1 && entries[0].Timestamp < cutoff)
        {
            entries.RemoveAt(0);
        }
    }
}
