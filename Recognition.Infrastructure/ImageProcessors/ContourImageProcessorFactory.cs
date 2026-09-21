using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class ContourImageProcessorFactory : IImageProcessorFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "builtin.preprocess.contour",
        "Contour",
        "Extracts edges before recognition.",
        []);

    public IImageProcessor Create(IReadOnlyDictionary<string, string> parameters)
    {
        return new ContourImageProcessor();
    }
}
