using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class AspectRatioImageProcessorFactory : IImageProcessorFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "builtin.preprocess.aspect-ratio",
        "Aspect Ratio",
        "Scales the width and height independently to emphasize features in either direction.",
        [
            new ParameterDefinition("WidthScale", "Width Scale", ParameterValueKind.Decimal, "1.0", Minimum: 0.1d, Maximum: 10.0d, Step: 0.1d),
            new ParameterDefinition("HeightScale", "Height Scale", ParameterValueKind.Decimal, "1.0", Minimum: 0.1d, Maximum: 10.0d, Step: 0.1d)
        ]);

    public IImageProcessor Create(IReadOnlyDictionary<string, string> parameters)
    {
        return new AspectRatioImageProcessor(
            parameters.GetDouble("WidthScale", 1.0d),
            parameters.GetDouble("HeightScale", 1.0d));
    }
}
