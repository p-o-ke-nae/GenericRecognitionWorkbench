using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class CropImageProcessorFactory : IImageProcessorFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "builtin.preprocess.crop",
        "Crop",
        "Crops the input frame to a selected region before later processing.",
        [
            new ParameterDefinition("X", "X", ParameterValueKind.Integer, "0", Minimum: 0, Step: 10),
            new ParameterDefinition("Y", "Y", ParameterValueKind.Integer, "0", Minimum: 0, Step: 10),
            new ParameterDefinition("Width", "Width", ParameterValueKind.Integer, "0", Minimum: 0, Step: 10),
            new ParameterDefinition("Height", "Height", ParameterValueKind.Integer, "0", Minimum: 0, Step: 10)
        ]);

    public IImageProcessor Create(IReadOnlyDictionary<string, string> parameters)
    {
        return new CropImageProcessor(parameters.GetRoi(string.Empty));
    }
}
