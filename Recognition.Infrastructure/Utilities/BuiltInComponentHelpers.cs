using OpenCvSharp;

namespace Recognition.Infrastructure;

internal static class BuiltInComponentHelpers
{
    public static Mat ConvertToGray(Mat input)
    {
        var output = new Mat();
        Cv2.CvtColor(input, output, input.Channels() == 4 ? ColorConversionCodes.BGRA2GRAY : ColorConversionCodes.BGR2GRAY);
        return output;
    }
}
