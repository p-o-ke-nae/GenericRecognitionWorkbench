using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class InvertImageProcessor : IImageProcessor
{
    public RecognitionFrame Process(RecognitionFrame frame)
    {
        using var mat = OpenCvFrameConversion.ToMat(frame);
        using var result = new Mat();
        Cv2.BitwiseNot(mat, result);
        return OpenCvFrameConversion.ToFrame(result, frame.CapturedAt);
    }
}
