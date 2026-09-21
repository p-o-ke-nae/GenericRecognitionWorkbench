using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class ScreenRegionFrameSourceFactory : IFrameSourceFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "builtin.screen-region",
        "Screen Region Capture",
        "Captures a desktop region selected in the configuration UI.",
        [
            new ParameterDefinition("X", "X", ParameterValueKind.Integer, "0", Step: 10),
            new ParameterDefinition("Y", "Y", ParameterValueKind.Integer, "0", Step: 10),
            new ParameterDefinition("Width", "Width", ParameterValueKind.Integer, "640", Minimum: 1, Step: 10),
            new ParameterDefinition("Height", "Height", ParameterValueKind.Integer, "360", Minimum: 1, Step: 10)
        ]);

    public IFrameSource Create(IReadOnlyDictionary<string, string> parameters)
    {
        return new ScreenRegionFrameSource(
            parameters.GetInt32("X"),
            parameters.GetInt32("Y"),
            Math.Max(1, parameters.GetInt32("Width", 640)),
            Math.Max(1, parameters.GetInt32("Height", 360)));
    }
}
