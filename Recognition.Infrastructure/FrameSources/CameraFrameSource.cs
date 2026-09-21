using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class CameraFrameSource : IFrameSource
{
    private readonly VideoCapture capture;

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
    }

    public ValueTask<RecognitionFrame> CaptureAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var frame = new Mat();
        if (!capture.Read(frame) || frame.Empty())
        {
            throw new InvalidOperationException("Failed to capture a frame from the camera.");
        }

        return ValueTask.FromResult(OpenCvFrameConversion.ToFrame(frame, DateTimeOffset.UtcNow));
    }

    public void Dispose()
    {
        capture.Dispose();
    }
}
