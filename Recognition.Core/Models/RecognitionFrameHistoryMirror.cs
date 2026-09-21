using System.Collections.ObjectModel;

namespace Recognition.Core;

/// <summary>UI-thread history references, preserving the retention window across runtime sessions.</summary>
public sealed class RecognitionFrameHistoryMirror
{
    private RecognitionFrameHistory? history;
    private long lastSequence;

    public ObservableCollection<RecognitionFrameHistoryEntry> Entries { get; } = [];

    public int Apply(RecognitionCycleResult result, TimeSpan retention, bool retainSourceFrames)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsSingleShot || result.FrameHistory is null)
        {
            if (!Entries.Any(entry => entry.CycleId == result.CycleId))
            {
                var sourceFrame = result.CycleSourceFrame ?? result.SourceFrame;
                var previewFrame = result.CyclePreviewFrame ?? result.PreviewFrame;
                Entries.Add(new RecognitionFrameHistoryEntry(result.Timestamp,
                    retainSourceFrames ? sourceFrame : null, previewFrame)
                {
                    CycleId = result.CycleId
                });
            }
        }
        else
        {
            if (!ReferenceEquals(history, result.FrameHistory))
            {
                history = result.FrameHistory;
                lastSequence = 0;
            }
            foreach (var entry in history.GetEntriesAfter(lastSequence, out _))
            {
                if (!Entries.Any(candidate => candidate.CycleId == entry.CycleId))
                {
                    Entries.Add(entry);
                }
                lastSequence = entry.Sequence;
            }
        }

        var removed = 0;
        var cutoff = (Entries.Count > 0 ? Entries[^1].Timestamp : result.Timestamp)
            - (retention < TimeSpan.Zero ? TimeSpan.Zero : retention);
        while (Entries.Count > 1 && Entries[0].Timestamp < cutoff)
        {
            Entries.RemoveAt(0);
            removed++;
        }
        return removed;
    }

    public int IndexOf(Guid cycleId)
    {
        for (var index = 0; index < Entries.Count; index++)
        {
            if (Entries[index].CycleId == cycleId) return index;
        }

        return -1;
    }

    public RecognitionFrame? GetSourceFrame(RecognitionFrameHistoryEntry? selectedEntry, RecognitionFrame? latestSourceFrame)
    {
        if (selectedEntry?.SourceFrame is { } sourceFrame) return sourceFrame;
        return selectedEntry is null || (Entries.Count > 0 && ReferenceEquals(selectedEntry, Entries[^1]))
            ? latestSourceFrame
            : null;
    }

    public void Clear()
    {
        history = null;
        lastSequence = 0;
        Entries.Clear();
    }
}
