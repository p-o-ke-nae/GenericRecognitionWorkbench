using Recognition.Core;
using Tesseract;

namespace Recognition.Infrastructure;

internal sealed class TesseractOcrEngine : IOcrEngine
{
    private readonly TesseractEngine engine;
    private readonly PageSegMode pageSegMode;

    public TesseractOcrEngine(IReadOnlyDictionary<string, string> parameters)
    {
        var dataPath = parameters.GetString("DataPath");
        var language = parameters.GetString("Language", "eng");
        if (string.IsNullOrWhiteSpace(dataPath))
        {
            throw new InvalidOperationException("Tesseract tessdata folder must be configured.");
        }

        engine = new TesseractEngine(dataPath, language, ParseEngineMode(parameters.GetString("EngineMode", "Default")));
        pageSegMode = ParsePageSegMode(parameters.GetString("PageSegMode", "SingleLine"));

        var whitelist = parameters.GetString("Whitelist");
        if (!string.IsNullOrWhiteSpace(whitelist))
        {
            engine.SetVariable("tessedit_char_whitelist", whitelist);
        }
    }

    public OcrResult Read(RecognitionFrame frame)
    {
        using var bitmap = OpenCvFrameConversion.ToBitmap(frame);
        using var pix = PixConverter.ToPix(bitmap);
        using var page = engine.Process(pix, pageSegMode);

        var text = (page.GetText() ?? string.Empty).Trim();
        var confidence = page.GetMeanConfidence();
        return new OcrResult(text, confidence, []);
    }

    public void Dispose()
    {
        engine.Dispose();
    }

    private static EngineMode ParseEngineMode(string value)
    {
        return value switch
        {
            "TesseractOnly" => EngineMode.TesseractOnly,
            "LstmOnly" => EngineMode.LstmOnly,
            "TesseractAndLstm" => EngineMode.TesseractAndLstm,
            _ => EngineMode.Default
        };
    }

    private static PageSegMode ParsePageSegMode(string value)
    {
        return value switch
        {
            "Auto" => PageSegMode.Auto,
            "SingleBlock" => PageSegMode.SingleBlock,
            "SingleWord" => PageSegMode.SingleWord,
            "SingleChar" => PageSegMode.SingleChar,
            _ => PageSegMode.SingleLine
        };
    }
}
