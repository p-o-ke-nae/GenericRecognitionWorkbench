using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class ThresholdImageProcessorFactory : IImageProcessorFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "builtin.preprocess.threshold",
        "Threshold",
        "Applies a binary threshold to the input frame.",
        [
            new ParameterDefinition("Threshold", "Threshold", ParameterValueKind.Integer, "160", Minimum: 0, Maximum: 255, Step: 1),
            new ParameterDefinition(
                "Mode",
                "Mode",
                ParameterValueKind.Choice,
                "Binary",
                Options:
                [
                    new ParameterOption("Binary", "Binary"),
                    new ParameterOption("BinaryInv", "Binary Inverted"),
                    new ParameterOption("Otsu", "Otsu")
                ])
        ]);

    public IImageProcessor Create(IReadOnlyDictionary<string, string> parameters)
    {
        return new ThresholdImageProcessor(parameters.GetInt32("Threshold", 160), parameters.GetString("Mode", "Binary"));
    }
}
