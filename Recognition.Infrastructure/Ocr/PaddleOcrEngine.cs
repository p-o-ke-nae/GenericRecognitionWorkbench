using PaddleOCRSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class PaddleOcrEngine : IOcrEngine
{
    private readonly PaddleOCREngine engine;

    public PaddleOcrEngine(IReadOnlyDictionary<string, string> parameters)
    {
        PaddleRuntimeBootstrapper.EnsureLoaded();
        var config = CreateConfig(parameters);
        var ocrParameter = new OCRParameter
        {
            use_gpu = parameters.GetBoolean("UseGpu"),
            use_angle_cls = parameters.GetBoolean("UseAngleCls", true),
            cpu_math_library_num_threads = parameters.GetInt32("CpuThreads", 4)
        };

        engine = new PaddleOCREngine(config, ocrParameter);
    }

    public OcrResult Read(RecognitionFrame frame)
    {
        using var bitmap = OpenCvFrameConversion.ToBitmap(frame);
        var result = engine.DetectText(bitmap);
        var blocks = result.TextBlocks.Select(static block => new OcrTextBlock(block.Text, block.Score)).ToArray();
        var averageConfidence = blocks.Length == 0 ? 0d : blocks.Average(static block => block.Confidence);
        return new OcrResult(result.Text?.Trim() ?? string.Empty, averageConfidence, blocks);
    }

    public void Dispose()
    {
        engine.Dispose();
    }

    private static OCRModelConfig CreateConfig(IReadOnlyDictionary<string, string> parameters)
    {
        return parameters.GetString("Preset", "Default") switch
        {
            "V6_EN" => OCRModelConfig.V5_EN,
            "V6_Tiny" => OCRModelConfig.V6_Tiny,
            "V6_Small" => OCRModelConfig.V6_Small,
            "Custom" => new OCRModelConfig(
                parameters.GetString("DetModelPath"),
                parameters.GetString("ClsModelPath"),
                parameters.GetString("RecModelPath"),
                parameters.GetString("KeysPath")),
            _ => OCRModelConfig.Default
        };
    }
}
