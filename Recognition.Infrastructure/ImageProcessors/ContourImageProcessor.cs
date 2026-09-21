using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class ContourImageProcessor : IImageProcessor
{
    public RecognitionFrame Process(RecognitionFrame frame)
    {
        using var mat = OpenCvFrameConversion.ToMat(frame);
        using var gray = mat.Channels() == 1
            ? mat.Clone()
            : BuiltInComponentHelpers.ConvertToGray(mat);
        using var edges = new Mat();
        Cv2.Canny(gray, edges, 60, 180);
        return OpenCvFrameConversion.ToFrame(edges, frame.CapturedAt);
    }
}
