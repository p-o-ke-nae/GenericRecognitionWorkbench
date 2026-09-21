namespace Recognition.Core;

public interface IFrameSourceDiagnostics
{
    long FrameSequence { get; }

    long DroppedFrames { get; }
}
