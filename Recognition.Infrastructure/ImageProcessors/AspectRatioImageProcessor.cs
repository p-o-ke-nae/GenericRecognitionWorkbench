using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class AspectRatioImageProcessor(double widthScale, double heightScale) : IImageProcessor
{
    private readonly double widthScale = NormalizeScale(widthScale);
    private readonly double heightScale = NormalizeScale(heightScale);

    public RecognitionFrame Process(RecognitionFrame frame)
    {
        var targetWidth = ScaleDimension(frame.Width, widthScale);
        var targetHeight = ScaleDimension(frame.Height, heightScale);
        if (targetWidth == frame.Width && targetHeight == frame.Height)
        {
            return frame;
        }

        using var input = OpenCvFrameConversion.ToMat(frame);
        using var output = new Mat();
        var interpolation = targetWidth <= frame.Width && targetHeight <= frame.Height
            ? InterpolationFlags.Area
            : InterpolationFlags.Linear;
        Cv2.Resize(input, output, new Size(targetWidth, targetHeight), 0d, 0d, interpolation);
        return OpenCvFrameConversion.ToFrame(output, frame.CapturedAt);
    }

    private static double NormalizeScale(double scale)
    {
        return double.IsFinite(scale)
            ? Math.Clamp(scale, 0.1d, 10.0d)
            : 1.0d;
    }

    private static int ScaleDimension(int dimension, double scale)
    {
        return Math.Max(1, (int)Math.Min(int.MaxValue, Math.Round(dimension * scale, MidpointRounding.AwayFromZero)));
    }
}
