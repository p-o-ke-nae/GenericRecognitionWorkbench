using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class CameraFrameSource : IFrameSource, IFrameSourceDiagnostics
{
    private readonly VideoCapture capture;
    private readonly LatestFrameSlot latestFrame = new();
    private readonly Thread grabThread;
    private volatile bool stopping;
    private int disposed;

    public CameraFrameSource(int cameraIndex, int width, int height, int fps)
    {
        capture = new VideoCapture(cameraIndex, VideoCaptureAPIs.DSHOW);
        if (!capture.IsOpened())
        {
            capture.Dispose();
            throw new InvalidOperationException($"Camera index {cameraIndex} could not be opened.");
        }

        capture.Set(VideoCaptureProperties.FrameWidth, width);
        capture.Set(VideoCaptureProperties.FrameHeight, height);
        capture.Set(VideoCaptureProperties.Fps, fps);
        grabThread = new Thread(GrabFrames) { IsBackground = true, Name = $"Camera {cameraIndex} capture" };
        grabThread.Start();
    }

    public long FrameSequence => latestFrame.FrameSequence;

    public long DroppedFrames => latestFrame.DroppedFrames;

    public ValueTask<RecognitionFrame> CaptureAsync(CancellationToken cancellationToken) => latestFrame.CaptureAsync(cancellationToken);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        stopping = true;
        latestFrame.Complete();
        grabThread.Join();
    }

    private void GrabFrames()
    {
        try
        {
            using var frame = new Mat();
            while (!stopping)
            {
                if (!capture.Read(frame) || frame.Empty())
                    throw new InvalidOperationException("Failed to capture a frame from the camera.");
                latestFrame.Publish(OpenCvFrameConversion.ToFrame(frame, DateTimeOffset.UtcNow));
            }
        }
        catch (Exception error)
        {
            latestFrame.Complete(error);
        }
        finally
        {
            capture.Dispose();
        }
    }
}
