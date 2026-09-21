namespace Recognition.Core;

public sealed record RecognitionFrameHistoryEntry(
    DateTimeOffset Timestamp,
    RecognitionFrame? SourceFrame,
    RecognitionFrame ProcessedFrame)
{
    public long Sequence { get; internal init; }

    public bool HasSourceFrame => SourceFrame is not null;

    public RecognitionFrameHistoryEntry Copy()
    {
        return new RecognitionFrameHistoryEntry(Timestamp, SourceFrame?.Clone(), ProcessedFrame.Clone());
    }
}

public sealed class RecognitionFrameHistory
{
    private readonly object gate = new();
    private RecognitionFrameHistoryEntry?[] entries = new RecognitionFrameHistoryEntry?[16];
    private int head;
    private int count;
    private long sequence;

    public RecognitionFrameHistory(TimeSpan retention)
        : this(retention, false)
    {
    }

    public RecognitionFrameHistory(TimeSpan retention, bool retainSourceFrames)
    {
        Retention = retention < TimeSpan.Zero ? TimeSpan.Zero : retention;
        RetainSourceFrames = retainSourceFrames;
    }

    public TimeSpan Retention { get; }

    public bool RetainSourceFrames { get; }

    public IReadOnlyList<RecognitionFrameHistoryEntry> Entries => GetEntriesAfter(0, out _);

    public IReadOnlyList<RecognitionFrameHistoryEntry> GetEntriesAfter(long afterSequence, out long firstSequence)
    {
        lock (gate)
        {
            firstSequence = sequence - count + 1;
            var skip = (int)Math.Clamp(afterSequence - firstSequence + 1, 0, count);
            var result = new RecognitionFrameHistoryEntry[count - skip];
            for (var i = skip; i < count; i++)
            {
                result[i - skip] = entries[(head + i) % entries.Length]!;
            }
            return result;
        }
    }

    public void Add(DateTimeOffset timestamp, RecognitionFrame sourceFrame, RecognitionFrame processedFrame)
    {
        ArgumentNullException.ThrowIfNull(sourceFrame);
        ArgumentNullException.ThrowIfNull(processedFrame);
        lock (gate)
        {
            if (count == entries.Length)
            {
                var expanded = new RecognitionFrameHistoryEntry?[entries.Length * 2];
                for (var i = 0; i < count; i++)
                    expanded[i] = entries[(head + i) % entries.Length];
                entries = expanded;
                head = 0;
            }
            entries[(head + count++) % entries.Length] = new RecognitionFrameHistoryEntry(
                timestamp, RetainSourceFrames ? sourceFrame : null, processedFrame) { Sequence = ++sequence };
            Trim(timestamp);
        }
    }

    public void Clear()
    {
        lock (gate)
        {
            Array.Clear(entries);
            head = 0;
            count = 0;
        }
    }

    private void Trim(DateTimeOffset newestTimestamp)
    {
        var cutoff = newestTimestamp - Retention;
        while (count > 1 && entries[head]!.Timestamp < cutoff)
        {
            entries[head] = null;
            head = (head + 1) % entries.Length;
            count--;
        }
    }
}
