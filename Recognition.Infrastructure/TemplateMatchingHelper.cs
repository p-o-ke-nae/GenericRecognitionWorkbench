using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

public static class TemplateMatchingHelper
{
    public static TemplateMatchEvaluationResult? Evaluate(RecognitionFrame frame, string templatePath, double threshold)
    {
        if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
        {
            return null;
        }

        using var template = Cv2.ImRead(templatePath, ImreadModes.Grayscale);
        if (template.Empty())
        {
            return null;
        }

        using var mat = OpenCvFrameConversion.ToMat(frame);
        using var gray = mat.Channels() == 1
            ? mat.Clone()
            : BuiltInComponentHelpers.ConvertToGray(mat);

        if (gray.Width < template.Width || gray.Height < template.Height)
        {
            return new TemplateMatchEvaluationResult(RoiArea.Empty, 0d, threshold);
        }

        using var result = new Mat();
        Cv2.MatchTemplate(gray, template, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out _, out var maxValue, out _, out var maxLocation);
        return new TemplateMatchEvaluationResult(
            new RoiArea(maxLocation.X, maxLocation.Y, template.Width, template.Height),
            maxValue,
            threshold);
    }

    public static RoiArea? TryLocate(RecognitionFrame frame, string templatePath, double threshold)
    {
        var evaluation = Evaluate(frame, templatePath, threshold);
        return evaluation is { IsMatched: true }
            ? evaluation.Region
            : null;
    }
}
