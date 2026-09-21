namespace Recognition.Core;

public interface IFrameSourceFactory
{
    ComponentDescriptor Descriptor { get; }

    IFrameSource Create(IReadOnlyDictionary<string, string> parameters);
}
