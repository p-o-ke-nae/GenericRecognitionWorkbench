using System.Diagnostics.CodeAnalysis;
using Recognition.Core;

namespace Recognition.Infrastructure;

/// <summary>Preserves event results in order while coalescing previews. Callers must synchronize access.</summary>
public sealed class RecognitionResultCoalescer
{
    public const int EventBacklogWarningThreshold = 64;

    private readonly Queue<RecognitionCycleResult> pendingEvents = new();
    private RecognitionCycleResult? latestPreview;
    private bool backlogWarningPending;
    private bool backlogWarningReported;

    public void Enqueue(RecognitionCycleResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.EventTriggered)
        {
            latestPreview = result;
            return;
        }

        pendingEvents.Enqueue(result);
        latestPreview = null;
        if (pendingEvents.Count > EventBacklogWarningThreshold && !backlogWarningReported)
        {
            backlogWarningPending = true;
            backlogWarningReported = true;
        }
    }

    public bool TryDequeue([NotNullWhen(true)] out RecognitionCycleResult? result)
    {
        if (pendingEvents.TryDequeue(out result))
        {
            if (pendingEvents.Count == 0) backlogWarningReported = false;
            return true;
        }

        result = latestPreview;
        latestPreview = null;
        return result is not null;
    }

    public bool TakeBacklogWarning()
    {
        var pending = backlogWarningPending;
        backlogWarningPending = false;
        return pending;
    }

    public void Clear()
    {
        pendingEvents.Clear();
        latestPreview = null;
        backlogWarningPending = false;
        backlogWarningReported = false;
    }
}
