using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class JapaneseTesseractOcrEngineFactory : IOcrEngineFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "builtin.ocr.tesseract-japanese",
        "Tesseract OCR (Japanese Best)",
        "Runs Tesseract with automatically managed Japanese traineddata from tessdata_best.",
        [
            new ParameterDefinition(
                "LanguageProfile",
                "Language Profile",
                ParameterValueKind.Choice,
                "jpn+eng",
                Options:
                [
                    new ParameterOption("jpn+eng", "Japanese + English"),
                    new ParameterOption("jpn", "Japanese"),
                    new ParameterOption("jpn_vert", "Japanese Vertical")
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
        return new JapaneseTesseractOcrEngine(parameters);
    }
}
