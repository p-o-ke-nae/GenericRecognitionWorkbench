namespace Recognition.Core;

public interface IOcrEngineFactory
{
    ComponentDescriptor Descriptor { get; }

    IOcrEngine Create(IReadOnlyDictionary<string, string> parameters);
}
