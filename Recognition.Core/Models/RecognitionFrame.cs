namespace Recognition.Core;

public sealed class RecognitionFrame
{
    public RecognitionFrame(byte[] pixelData, int width, int height, int stride, FramePixelFormat pixelFormat, DateTimeOffset capturedAt)
    {
        PixelData = pixelData;
        Width = width;
        Height = height;
        Stride = stride;
        PixelFormat = pixelFormat;
        CapturedAt = capturedAt;
    }

    public byte[] PixelData { get; }

    public int Width { get; }

    public int Height { get; }

    public int Stride { get; }

    public FramePixelFormat PixelFormat { get; }

    public DateTimeOffset CapturedAt { get; }

    public RecognitionFrame Clone()
    {
        return new RecognitionFrame([.. PixelData], Width, Height, Stride, PixelFormat, CapturedAt);
    }
}
