using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal static class RecognitionFrameScaler
{
    public static RecognitionFrame Scale(RecognitionFrame frame, double scale)
    {
        var normalizedScale = NormalizeScale(scale);
        if (Math.Abs(normalizedScale - 1.0d) < 0.0001d)
        {
            return frame;
        }

        using var input = OpenCvFrameConversion.ToMat(frame);
        using var output = new Mat();
        var interpolation = normalizedScale < 1.0d
            ? InterpolationFlags.Area
            : InterpolationFlags.Linear;

        Cv2.Resize(input, output, default, normalizedScale, normalizedScale, interpolation);
        return OpenCvFrameConversion.ToFrame(output, frame.CapturedAt);
    }

    public static double NormalizeScale(double scale)
    {
        if (double.IsNaN(scale) || double.IsInfinity(scale))
        {
            return 1.0d;
        }

        return Math.Clamp(scale, 0.05d, 8.0d);
    }
}
