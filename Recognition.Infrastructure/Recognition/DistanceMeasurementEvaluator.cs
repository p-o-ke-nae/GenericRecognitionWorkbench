using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class DistanceMeasurementEvaluator : IDisposable
{
    private readonly IReadOnlyList<DistanceMeasurementReferenceEntry> references;
    private readonly IReadOnlyList<DistanceMeasurementTargetEntry> targets;

    public DistanceMeasurementEvaluator(DistanceMeasurementConfiguration configuration)
    {
        references = BuildReferences(configuration);
        targets = BuildTargets(configuration);
    }

    public DistanceMeasurementResult? Measure(RecognitionFrame frame)
    {
        using var mat = OpenCvFrameConversion.ToMat(frame);
        using var gray = mat.Channels() == 1
            ? mat.Clone()
            : BuiltInComponentHelpers.ConvertToGray(mat);

        var referenceLocations = new Dictionary<string, Point>(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in references)
        {
            var location = Locate(gray, reference.Template, reference.Threshold);
            if (location is not null && !referenceLocations.ContainsKey(reference.Name))
            {
                referenceLocations.Add(reference.Name, location.Value);
            }
        }

        var measurements = new List<string>();
        var previewItems = new List<DistanceMeasurementPreviewItem>();
        foreach (var target in targets)
        {
            if (!referenceLocations.TryGetValue(target.ReferenceName, out var referenceLocation))
            {
                continue;
            }

            var location = Locate(gray, target.Template, target.Threshold);
            if (location is null)
            {
                continue;
            }

            var dx = location.Value.X - referenceLocation.X;
            var dy = location.Value.Y - referenceLocation.Y;
            measurements.Add($"{target.Name} - {target.ReferenceName}: ΔX={dx}, ΔY={dy}");
            previewItems.Add(new DistanceMeasurementPreviewItem(
                target.ReferenceName,
                new RoiArea(referenceLocation.X, referenceLocation.Y, target.ReferenceWidth, target.ReferenceHeight),
                target.Name,
                new RoiArea(location.Value.X, location.Value.Y, target.Template.Width, target.Template.Height),
                dx,
                dy));
        }

        return measurements.Count == 0
            ? null
            : new DistanceMeasurementResult(
                string.Join(Environment.NewLine, measurements),
                OpenCvFrameConversion.ToFrame(gray, frame.CapturedAt),
                previewItems);
    }

    public void Dispose()
    {
        foreach (var reference in references)
        {
            reference.Template.Dispose();
        }

        foreach (var target in targets)
        {
            target.Template.Dispose();
        }
    }

    private static IReadOnlyList<DistanceMeasurementReferenceEntry> BuildReferences(DistanceMeasurementConfiguration configuration)
    {
        var configuredReferences = configuration.References.Count > 0
            ? configuration.References
            : string.IsNullOrWhiteSpace(configuration.ReferenceTemplatePath)
                ? []
                :
                [
                    new DistanceMeasurementReferenceConfiguration
                    {
                        Name = "Reference 1",
                        TemplatePath = configuration.ReferenceTemplatePath,
                        Threshold = configuration.ReferenceThreshold
                    }
                ];

        return configuredReferences
            .Select((reference, index) => new DistanceMeasurementReferenceEntry(
                string.IsNullOrWhiteSpace(reference.Name) ? $"Reference {index + 1}" : reference.Name,
                LoadTemplate(reference.TemplatePath, $"{nameof(configuration.References)}[{index}]"),
                reference.Threshold))
            .ToArray();
    }

    private static IReadOnlyList<DistanceMeasurementTargetEntry> BuildTargets(DistanceMeasurementConfiguration configuration)
    {
        var configuredTargets = configuration.Targets.Count > 0
            ? configuration.Targets
            : string.IsNullOrWhiteSpace(configuration.TargetTemplatePath)
                ? []
                :
                [
                    new DistanceMeasurementTargetConfiguration
                    {
                        Name = "Target 1",
                        ReferenceName = "Reference 1",
                        TemplatePath = configuration.TargetTemplatePath,
                        Threshold = configuration.TargetThreshold
                    }
                ];

        return configuredTargets
            .Select((target, index) => new DistanceMeasurementTargetEntry(
                string.IsNullOrWhiteSpace(target.Name) ? $"Target {index + 1}" : target.Name,
                string.IsNullOrWhiteSpace(target.ReferenceName) ? "Reference 1" : target.ReferenceName,
                LoadTemplate(target.TemplatePath, $"{nameof(configuration.Targets)}[{index}]"),
                ResolveReferenceTemplateSize(configuration, target.ReferenceName),
                target.Threshold))
            .ToArray();
    }

    private static Size ResolveReferenceTemplateSize(DistanceMeasurementConfiguration configuration, string? referenceName)
    {
        var configuredReferences = configuration.References.Count > 0
            ? configuration.References
            : string.IsNullOrWhiteSpace(configuration.ReferenceTemplatePath)
                ? []
                :
                [
                    new DistanceMeasurementReferenceConfiguration
                    {
                        Name = "Reference 1",
                        TemplatePath = configuration.ReferenceTemplatePath,
                        Threshold = configuration.ReferenceThreshold
                    }
                ];
        var reference = configuredReferences.FirstOrDefault(candidate => string.Equals(candidate.Name, referenceName, StringComparison.OrdinalIgnoreCase))
            ?? configuredReferences.FirstOrDefault();
        if (reference is null || string.IsNullOrWhiteSpace(reference.TemplatePath))
        {
            return new Size();
        }

        using var mat = LoadTemplate(reference.TemplatePath, reference.TemplatePath);
        return new Size(mat.Width, mat.Height);
    }

    private static Mat LoadTemplate(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new FileNotFoundException($"Distance measurement template '{parameterName}' was not found.", path);
        }

        var template = Cv2.ImRead(path, ImreadModes.Grayscale);
        if (template.Empty())
        {
            template.Dispose();
            throw new InvalidOperationException($"Distance measurement template '{path}' could not be loaded.");
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

    private sealed record DistanceMeasurementReferenceEntry(string Name, Mat Template, double Threshold);

    private sealed record DistanceMeasurementTargetEntry(string Name, string ReferenceName, Mat Template, Size ReferenceTemplateSize, double Threshold)
    {
        public int ReferenceWidth => ReferenceTemplateSize.Width;

        public int ReferenceHeight => ReferenceTemplateSize.Height;
    }
}
