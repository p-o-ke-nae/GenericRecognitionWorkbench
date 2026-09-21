using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class OcrReferenceEvaluator : IDisposable
{
    private readonly IReadOnlyList<OcrReferenceEntry> references;

    public OcrReferenceEvaluator(IEnumerable<OcrReferenceConfiguration> configuration)
    {
        references = configuration
            .Where(static reference => !string.IsNullOrWhiteSpace(reference.TemplatePath))
            .Select((reference, index) => new OcrReferenceEntry(
                string.IsNullOrWhiteSpace(reference.Name) ? $"OCR Reference {index + 1}" : reference.Name,
                LoadTemplate(reference.TemplatePath, $"{nameof(configuration)}[{index}]"),
                reference.Threshold))
            .ToArray();
    }

    public IReadOnlyDictionary<string, RoiArea> Locate(RecognitionFrame frame)
    {
        using var mat = OpenCvFrameConversion.ToMat(frame);
        using var gray = mat.Channels() == 1
            ? mat.Clone()
            : BuiltInComponentHelpers.ConvertToGray(mat);

        var locations = new Dictionary<string, RoiArea>(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in references)
        {
            var location = Locate(gray, reference.Template, reference.Threshold);
            if (location is not null && !locations.ContainsKey(reference.Name))
            {
                locations.Add(reference.Name, new RoiArea(location.Value.X, location.Value.Y, reference.Template.Width, reference.Template.Height));
            }
        }

        return locations;
    }

    public void Dispose()
    {
        foreach (var reference in references)
        {
            reference.Template.Dispose();
        }
    }

    private static Mat LoadTemplate(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new FileNotFoundException($"OCR reference template '{parameterName}' was not found.", path);
        }

        var template = Cv2.ImRead(path, ImreadModes.Grayscale);
        if (template.Empty())
        {
            template.Dispose();
            throw new InvalidOperationException($"OCR reference template '{path}' could not be loaded.");
        }

        return template;
    }

    private static Point? Locate(Mat gray, Mat template, double threshold)
    {
        if (gray.Width < template.Width || gray.Height < template.Height)
        {
            return null;
        }

        using var result = new Mat();
        Cv2.MatchTemplate(gray, template, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out _, out var maxValue, out _, out var maxLocation);
        if (maxValue < threshold)
        {
            return null;
        }

        return maxLocation;
    }

    private sealed record OcrReferenceEntry(string Name, Mat Template, double Threshold);
}
