using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class CropImageProcessor(RoiArea roi) : IImageProcessor
{
    public RoiArea Roi { get; } = roi;

    public RecognitionFrame Process(RecognitionFrame frame)
    {
        if (Roi.IsEmpty)
        {
            return frame;
        }

        var x = Math.Clamp(Roi.X, 0, frame.Width);
        var y = Math.Clamp(Roi.Y, 0, frame.Height);
        var width = Math.Clamp(Roi.Width, 0, frame.Width - x);
        var height = Math.Clamp(Roi.Height, 0, frame.Height - y);
        if (width == 0 || height == 0) return frame;

        var bytesPerPixel = frame.PixelFormat switch
        {
            FramePixelFormat.Bgr24 => 3,
            FramePixelFormat.Bgra32 => 4,
            FramePixelFormat.Gray8 => 1,
            _ => throw new NotSupportedException($"Unsupported pixel format: {frame.PixelFormat}.")
        };
        var stride = width * bytesPerPixel;
        var pixels = new byte[height * stride];
        for (var row = 0; row < height; row++)
        {
            Buffer.BlockCopy(frame.PixelData, (y + row) * frame.Stride + x * bytesPerPixel, pixels, row * stride, stride);
        }
        return new RecognitionFrame(pixels, width, height, stride, frame.PixelFormat, frame.CapturedAt);
    }
}
