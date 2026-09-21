using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class InvertImageProcessorFactory : IImageProcessorFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "builtin.preprocess.invert",
        "Invert",
        "Inverts the frame colors before recognition.",
        []);

    public IImageProcessor Create(IReadOnlyDictionary<string, string> parameters)
    {
        return new InvertImageProcessor();
    }
}
