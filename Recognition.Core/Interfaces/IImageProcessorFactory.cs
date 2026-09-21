namespace Recognition.Core;

public interface IImageProcessorFactory
{
    ComponentDescriptor Descriptor { get; }

    IImageProcessor Create(IReadOnlyDictionary<string, string> parameters);
}
