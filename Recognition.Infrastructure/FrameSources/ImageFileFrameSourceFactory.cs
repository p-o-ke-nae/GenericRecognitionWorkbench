using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class ImageFileFrameSourceFactory : IFrameSourceFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "builtin.image-file",
        "Image File",
        "Loads frames from a still image file for testing.",
        [
            new ParameterDefinition("ImagePath", "Image File", ParameterValueKind.FilePath, "")
        ]);

    public IFrameSource Create(IReadOnlyDictionary<string, string> parameters)
    {
        return new ImageFileFrameSource(parameters.GetString("ImagePath"));
    }
}
