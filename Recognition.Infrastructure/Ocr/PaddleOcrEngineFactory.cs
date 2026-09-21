using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class PaddleOcrEngineFactory : IOcrEngineFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "builtin.ocr.paddle",
        "Paddle OCR",
        "Runs PaddleOCRSharp with either a built-in preset or custom model paths.",
        [
            new ParameterDefinition(
                "Preset",
                "Preset",
                ParameterValueKind.Choice,
                "Default",
                Options:
                [
                    new ParameterOption("Default", "Default"),
                    new ParameterOption("V6_EN", "V6 English"),
                    new ParameterOption("V6_Tiny", "V6 Tiny"),
                    new ParameterOption("V6_Small", "V6 Small"),
                    new ParameterOption("Custom", "Custom")
                ]),
            new ParameterDefinition("DetModelPath", "Det Model", ParameterValueKind.FilePath, ""),
            new ParameterDefinition("ClsModelPath", "Cls Model", ParameterValueKind.FilePath, ""),
            new ParameterDefinition("RecModelPath", "Rec Model", ParameterValueKind.FilePath, ""),
            new ParameterDefinition("KeysPath", "Keys File", ParameterValueKind.FilePath, ""),
            new ParameterDefinition("UseGpu", "Use GPU", ParameterValueKind.Boolean, "false"),
            new ParameterDefinition("UseAngleCls", "Use Angle Classification", ParameterValueKind.Boolean, "true"),
            new ParameterDefinition("CpuThreads", "CPU Threads", ParameterValueKind.Integer, "4", Minimum: 1, Step: 1)
        ]);

    public IOcrEngine Create(IReadOnlyDictionary<string, string> parameters)
    {
        return new PaddleOcrEngine(parameters);
    }
}
