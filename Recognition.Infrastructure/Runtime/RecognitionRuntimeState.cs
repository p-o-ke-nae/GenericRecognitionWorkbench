using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class RecognitionRuntimeState
{
    public List<(DateTimeOffset Timestamp, RecognitionFrame Source, RecognitionFrame Processed)> BufferedFrames { get; } = [];

    public RecognitionFrameHistory? FrameHistory { get; set; }

    public bool PreviousDetected { get; set; }

    public int PendingFutureActionFramesRemaining { get; set; }

    public long LastTimestamp { get; set; }
}
