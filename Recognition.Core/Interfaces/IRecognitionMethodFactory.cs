namespace Recognition.Core;

public interface IRecognitionMethodFactory
{
    ComponentDescriptor Descriptor { get; }

    IRecognitionMethod Create(IReadOnlyDictionary<string, string> parameters);
}
