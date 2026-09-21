using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class ThresholdImageProcessor(int threshold, string mode) : IImageProcessor
{
    public RecognitionFrame Process(RecognitionFrame frame)
    {
        using var mat = OpenCvFrameConversion.ToMat(frame);
        using var gray = mat.Channels() == 1
            ? mat.Clone()
            : BuiltInComponentHelpers.ConvertToGray(mat);

        using var result = new Mat();
        var thresholdType = mode switch
        {
            "BinaryInv" => ThresholdTypes.BinaryInv,
            "Otsu" => ThresholdTypes.Binary | ThresholdTypes.Otsu,
            _ => ThresholdTypes.Binary
        };

        Cv2.Threshold(gray, result, threshold, 255, thresholdType);
        return OpenCvFrameConversion.ToFrame(result, frame.CapturedAt);
    }
}
