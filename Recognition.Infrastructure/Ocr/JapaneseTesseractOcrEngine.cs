using System.Net.Http;
using Recognition.Core;
using Tesseract;

namespace Recognition.Infrastructure;

internal sealed class JapaneseTesseractOcrEngine : IOcrEngine
{
    private static readonly HttpClient HttpClient = new();
    private static readonly object DownloadSync = new();
    private static readonly Dictionary<string, string> TrainedDataUrls = new(StringComparer.OrdinalIgnoreCase)
    {
        ["jpn"] = "https://github.com/tesseract-ocr/tessdata_best/raw/main/jpn.traineddata",
        ["jpn_vert"] = "https://github.com/tesseract-ocr/tessdata_best/raw/main/jpn_vert.traineddata",
        ["eng"] = "https://github.com/tesseract-ocr/tessdata_best/raw/main/eng.traineddata"
    };

    private readonly TesseractEngine engine;
    private readonly PageSegMode pageSegMode;

    public JapaneseTesseractOcrEngine(IReadOnlyDictionary<string, string> parameters)
    {
        var languageProfile = parameters.GetString("LanguageProfile", "jpn+eng");
        var dataPath = EnsureLanguageData(languageProfile);
        engine = new TesseractEngine(dataPath, languageProfile, EngineMode.LstmOnly);
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

    private static string EnsureLanguageData(string languageProfile)
    {
        var tessdataDirectory = Path.Combine(AppContext.BaseDirectory, "Tessdata", "best");
        Directory.CreateDirectory(tessdataDirectory);

        lock (DownloadSync)
        {
            foreach (var language in languageProfile.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!TrainedDataUrls.TryGetValue(language, out var url))
                {
                    throw new InvalidOperationException($"Unsupported Japanese OCR language profile entry '{language}'.");
                }

                var destinationPath = Path.Combine(tessdataDirectory, $"{language}.traineddata");
                if (File.Exists(destinationPath))
                {
                    continue;
                }

                var bytes = HttpClient.GetByteArrayAsync(url).GetAwaiter().GetResult();
                var tempPath = destinationPath + ".download";
                File.WriteAllBytes(tempPath, bytes);
                File.Move(tempPath, destinationPath, overwrite: true);
            }
        }

        return tessdataDirectory;
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
