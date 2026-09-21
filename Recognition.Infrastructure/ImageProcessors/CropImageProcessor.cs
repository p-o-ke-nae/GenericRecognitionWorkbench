using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class CropImageProcessor(RoiArea roi) : IImageProcessor
{
    public RoiArea Roi { get; } = roi;

    public RecognitionFrame Process(RecognitionFrame frame)
    {
        if (Roi.IsEmpty)
        {
            return frame.Clone();
        }

        using var mat = OpenCvFrameConversion.ToMat(frame);
        using var cropped = OpenCvFrameConversion.Crop(mat, Roi);
        return cropped.Empty()
            ? frame.Clone()
            : OpenCvFrameConversion.ToFrame(cropped, frame.CapturedAt);
    }
}
