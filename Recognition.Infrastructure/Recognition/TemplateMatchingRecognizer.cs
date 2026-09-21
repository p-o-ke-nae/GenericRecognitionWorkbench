using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class TemplateMatchingRecognizer : IRecognitionMethod
{
    private readonly Mat template;
    private readonly TemplateMatchModes matchMode;
    private readonly double threshold;
    private readonly RoiArea searchRoi;

    public TemplateMatchingRecognizer(string templatePath, double threshold, string method, RoiArea searchRoi)
    {
        template = Cv2.ImRead(templatePath, ImreadModes.Grayscale);
        if (template.Empty())
        {
            throw new InvalidOperationException($"Template image '{templatePath}' could not be loaded.");
        }

        this.threshold = threshold;
        this.searchRoi = searchRoi;
        matchMode = method switch
        {
            "CCorrNormed" => TemplateMatchModes.CCorrNormed,
            "SqDiffNormed" => TemplateMatchModes.SqDiffNormed,
            _ => TemplateMatchModes.CCoeffNormed
        };
    }

    public RecognitionMatch Evaluate(RecognitionFrame frame)
    {
        using var mat = OpenCvFrameConversion.ToMat(frame);
        using var gray = mat.Channels() == 1
            ? mat.Clone()
            : BuiltInComponentHelpers.ConvertToGray(mat);
        using var searchArea = OpenCvFrameConversion.Crop(gray, searchRoi);

        if (searchArea.Empty() || searchArea.Width < template.Width || searchArea.Height < template.Height)
        {
            return new RecognitionMatch(false, 0, "template-too-large");
        }

        using var result = new Mat();
        Cv2.MatchTemplate(searchArea, template, result, matchMode);
        Cv2.MinMaxLoc(result, out var minValue, out var maxValue, out var minLocation, out var maxLocation);

        var confidence = matchMode == TemplateMatchModes.SqDiffNormed
            ? 1d - minValue
            : maxValue;
        var bestLocation = matchMode == TemplateMatchModes.SqDiffNormed
            ? minLocation
            : maxLocation;
        var offsetX = searchRoi.IsEmpty ? 0 : searchRoi.X;
        var offsetY = searchRoi.IsEmpty ? 0 : searchRoi.Y;
        var region = new RoiArea(bestLocation.X + offsetX, bestLocation.Y + offsetY, template.Width, template.Height);

        return new RecognitionMatch(confidence >= threshold, confidence, "template-match", region);
    }

    public void Dispose()
    {
        template.Dispose();
    }
}
