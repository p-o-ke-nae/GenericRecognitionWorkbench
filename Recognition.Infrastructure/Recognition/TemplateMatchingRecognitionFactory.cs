using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class TemplateMatchingRecognitionFactory : IRecognitionMethodFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "builtin.recognition.template-match",
        "Template Matching",
        "Detects an image region by OpenCV template matching.",
        [
            new ParameterDefinition("TemplatePath", "Template Image", ParameterValueKind.FilePath, ""),
            new ParameterDefinition("Threshold", "Match Threshold", ParameterValueKind.Decimal, "0.92", Minimum: 0, Maximum: 1, Step: 0.01),
            new ParameterDefinition(
                "Method",
                "Method",
                ParameterValueKind.Choice,
                "CCoeffNormed",
                Options:
                [
                    new ParameterOption("CCoeffNormed", "CCoeff Normed"),
                    new ParameterOption("CCorrNormed", "CCorr Normed"),
                    new ParameterOption("SqDiffNormed", "SqDiff Normed")
                ]),
            new ParameterDefinition("SearchX", "Search X", ParameterValueKind.Integer, "0", Step: 10),
            new ParameterDefinition("SearchY", "Search Y", ParameterValueKind.Integer, "0", Step: 10),
            new ParameterDefinition("SearchWidth", "Search Width", ParameterValueKind.Integer, "0", Step: 10),
            new ParameterDefinition("SearchHeight", "Search Height", ParameterValueKind.Integer, "0", Step: 10)
        ]);

    public IRecognitionMethod Create(IReadOnlyDictionary<string, string> parameters)
    {
        var templatePath = parameters.GetString("TemplatePath");
        if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
        {
            return new MissingTemplateRecognitionMethod();
        }

        return new TemplateMatchingRecognizer(
            templatePath,
            parameters.GetDouble("Threshold", 0.92),
            parameters.GetString("Method", "CCoeffNormed"),
            parameters.GetRoi("Search"));
    }
}
