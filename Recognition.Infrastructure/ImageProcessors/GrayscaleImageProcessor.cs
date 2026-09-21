using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class GrayscaleImageProcessor : IImageProcessor
{
    public RecognitionFrame Process(RecognitionFrame frame)
    {
        using var mat = OpenCvFrameConversion.ToMat(frame);
        if (mat.Channels() == 1)
        {
            return frame;
        }

        using var gray = new Mat();
        var conversion = mat.Channels() == 4 ? ColorConversionCodes.BGRA2GRAY : ColorConversionCodes.BGR2GRAY;
        Cv2.CvtColor(mat, gray, conversion);
        return OpenCvFrameConversion.ToFrame(gray, frame.CapturedAt);
    }
}
