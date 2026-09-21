namespace Recognition.Core;

public interface IFrameSource : IDisposable
{
    ValueTask<RecognitionFrame> CaptureAsync(CancellationToken cancellationToken);
}
