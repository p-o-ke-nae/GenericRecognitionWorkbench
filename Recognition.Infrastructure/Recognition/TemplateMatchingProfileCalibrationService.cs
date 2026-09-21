using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class TemplateMatchingProfileCalibrationService : IRecognitionProfileCalibrationService
{
    private static readonly double[] ScaleCandidates =
    [
        0.60d, 0.70d, 0.80d, 0.90d, 0.95d, 1.00d, 1.05d, 1.10d, 1.20d, 1.30d, 1.40d
    ];

    public Task<RecognitionProfileCalibrationResult> CalibrateAsync(
        RecognitionProfile profile,
        RecognitionFrame processedFrame,
        CancellationToken cancellationToken = default)
    {
        var templatePath = profile.Recognizer.Parameters.GetString("TemplatePath");
        if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
        {
            throw new FileNotFoundException("Template image was not found.", templatePath);
        }

        using var template = Cv2.ImRead(templatePath, ImreadModes.Grayscale);
        if (template.Empty())
        {
            throw new InvalidOperationException($"Template image '{templatePath}' could not be loaded.");
        }

        using var frameMat = OpenCvFrameConversion.ToMat(processedFrame);
        using var gray = frameMat.Channels() == 1
            ? frameMat.Clone()
            : BuiltInComponentHelpers.ConvertToGray(frameMat);
        var searchRoi = profile.Recognizer.Parameters.GetRoi("Search");
        using var searchArea = OpenCvFrameConversion.Crop(gray, searchRoi);
        if (searchArea.Empty())
        {
            throw new InvalidOperationException("Calibration search area is empty.");
        }

        var matchMode = profile.Recognizer.Parameters.GetString("Method", "CCoeffNormed") switch
        {
            "CCorrNormed" => TemplateMatchModes.CCorrNormed,
            "SqDiffNormed" => TemplateMatchModes.SqDiffNormed,
            _ => TemplateMatchModes.CCoeffNormed
        };
        var currentThreshold = profile.Recognizer.Parameters.GetDouble("Threshold", 0.92d);

        CalibrationCandidate? best = null;
        foreach (var scaleX in ScaleCandidates)
        {
            foreach (var scaleY in ScaleCandidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var width = Math.Max(1, (int)Math.Round(template.Width * scaleX));
                var height = Math.Max(1, (int)Math.Round(template.Height * scaleY));
                if (searchArea.Width < width || searchArea.Height < height)
                {
                    continue;
                }

                using var resizedTemplate = new Mat();
                Cv2.Resize(template, resizedTemplate, new OpenCvSharp.Size(width, height), interpolation: InterpolationFlags.Linear);
                using var result = new Mat();
                Cv2.MatchTemplate(searchArea, resizedTemplate, result, matchMode);
                Cv2.MinMaxLoc(result, out var minValue, out var maxValue, out var minLocation, out var maxLocation);
                var confidence = matchMode == TemplateMatchModes.SqDiffNormed ? 1d - minValue : maxValue;
                var location = matchMode == TemplateMatchModes.SqDiffNormed ? minLocation : maxLocation;

                if (best is null || confidence > best.Confidence)
                {
                    best = new CalibrationCandidate(location, width, height, confidence);
                }
            }
        }

        if (best is null || best.Confidence < 0.45d)
        {
            throw new InvalidOperationException("A reliable template match could not be found for calibration.");
        }

        var offsetX = searchRoi.IsEmpty ? 0 : searchRoi.X;
        var offsetY = searchRoi.IsEmpty ? 0 : searchRoi.Y;
        var matchedRegion = new RoiArea(best.Location.X + offsetX, best.Location.Y + offsetY, best.Width, best.Height);
        using var cropped = OpenCvFrameConversion.Crop(gray, matchedRegion);
        var templateFrame = OpenCvFrameConversion.ToFrame(cropped, processedFrame.CapturedAt);
        var suggestedThreshold = Math.Round(Math.Clamp(Math.Min(currentThreshold, best.Confidence - 0.02d), 0.45d, 0.99d), 2);
        return Task.FromResult(new RecognitionProfileCalibrationResult(templateFrame, matchedRegion, suggestedThreshold, best.Confidence));
    }

    private sealed record CalibrationCandidate(Point Location, int Width, int Height, double Confidence);
}
