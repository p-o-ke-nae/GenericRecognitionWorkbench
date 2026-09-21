using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class TesseractOcrEngineFactory : IOcrEngineFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "builtin.ocr.tesseract",
        "Tesseract OCR",
        "Runs Tesseract over the selected frame.",
        [
            new ParameterDefinition("DataPath", "Tessdata Folder", ParameterValueKind.FolderPath, ""),
            new ParameterDefinition("Language", "Language", ParameterValueKind.Text, "eng"),
            new ParameterDefinition(
                "EngineMode",
                "Engine Mode",
                ParameterValueKind.Choice,
                "Default",
                Options:
                [
                    new ParameterOption("Default", "Default"),
                    new ParameterOption("TesseractOnly", "Tesseract Only"),
                    new ParameterOption("LstmOnly", "LSTM Only"),
                    new ParameterOption("TesseractAndLstm", "Tesseract + LSTM")
                ]),
            new ParameterDefinition(
                "PageSegMode",
                "Page Segmentation",
                ParameterValueKind.Choice,
                "SingleLine",
                Options:
                [
                    new ParameterOption("Auto", "Auto"),
                    new ParameterOption("SingleBlock", "Single Block"),
                    new ParameterOption("SingleLine", "Single Line"),
                    new ParameterOption("SingleWord", "Single Word"),
                    new ParameterOption("SingleChar", "Single Char")
                ]),
            new ParameterDefinition("Whitelist", "Whitelist", ParameterValueKind.Text, "")
        ]);

    public IOcrEngine Create(IReadOnlyDictionary<string, string> parameters)
    {
        return new TesseractOcrEngine(parameters);
    }
}
