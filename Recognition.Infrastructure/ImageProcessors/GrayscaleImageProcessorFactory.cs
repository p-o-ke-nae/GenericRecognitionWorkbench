using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class GrayscaleImageProcessorFactory : IImageProcessorFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "builtin.preprocess.grayscale",
        "Grayscale",
        "Converts frames to grayscale before recognition.",
        []);

    public IImageProcessor Create(IReadOnlyDictionary<string, string> parameters)
    {
        return new GrayscaleImageProcessor();
    }
}
