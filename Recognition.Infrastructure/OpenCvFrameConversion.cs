using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal static class OpenCvFrameConversion
{
    public static Mat ToMat(RecognitionFrame frame)
    {
        var matType = frame.PixelFormat switch
        {
            FramePixelFormat.Bgra32 => MatType.CV_8UC4,
            FramePixelFormat.Bgr24 => MatType.CV_8UC3,
            FramePixelFormat.Gray8 => MatType.CV_8UC1,
            _ => throw new NotSupportedException($"Unsupported pixel format: {frame.PixelFormat}.")
        };

        using var mat = Mat.FromPixelData(frame.Height, frame.Width, matType, frame.PixelData, frame.Stride);
        return mat.Clone();
    }

    public static RecognitionFrame ToFrame(Mat mat, DateTimeOffset? capturedAt = null)
    {
        using var normalized = NormalizePixelFormat(mat);
        var data = new byte[normalized.Rows * normalized.Step()];
        System.Runtime.InteropServices.Marshal.Copy(normalized.Data, data, 0, data.Length);

        return new RecognitionFrame(
            data,
            normalized.Width,
            normalized.Height,
            (int)normalized.Step(),
            normalized.Channels() switch
            {
                4 => FramePixelFormat.Bgra32,
                3 => FramePixelFormat.Bgr24,
                1 => FramePixelFormat.Gray8,
                _ => throw new NotSupportedException($"Unsupported channel count: {normalized.Channels()}.")
            },
            capturedAt ?? DateTimeOffset.UtcNow);
    }

    public static Bitmap ToBitmap(RecognitionFrame frame)
    {
        using var mat = ToMat(frame);
        return BitmapConverter.ToBitmap(mat);
    }

    public static Mat Crop(Mat mat, RoiArea roi)
    {
        if (roi.IsEmpty)
        {
            return mat.Clone();
        }

        var bounded = new Rect(
            Math.Clamp(roi.X, 0, mat.Width),
            Math.Clamp(roi.Y, 0, mat.Height),
            Math.Clamp(roi.Width, 0, Math.Max(0, mat.Width - roi.X)),
            Math.Clamp(roi.Height, 0, Math.Max(0, mat.Height - roi.Y)));

        if (bounded.Width <= 0 || bounded.Height <= 0)
        {
            return new Mat();
        }

        return new Mat(mat, bounded).Clone();
    }

    private static Mat NormalizePixelFormat(Mat input)
    {
        return input.Channels() switch
        {
            4 => input.Clone(),
            3 => input.Clone(),
            1 => input.Clone(),
            _ => throw new NotSupportedException($"Unsupported channel count: {input.Channels()}.")
        };
    }
}
