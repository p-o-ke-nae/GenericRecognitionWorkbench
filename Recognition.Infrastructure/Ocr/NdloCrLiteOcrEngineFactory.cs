using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class NdloCrLiteOcrEngineFactory : IOcrEngineFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "builtin.ocr.ndlocr-lite",
        "NDLOCR-Lite",
        "Runs National Diet Library NDLOCR-Lite for high-accuracy Japanese OCR.",
        [
            new ParameterDefinition("EnableTcy", "Enable TateChuYoko", ParameterValueKind.Boolean, "true")
        ]);

    public IOcrEngine Create(IReadOnlyDictionary<string, string> parameters)
    {
        return new NdloCrLiteOcrEngine(parameters);
    }
}
