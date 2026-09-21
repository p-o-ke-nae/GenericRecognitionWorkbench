using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class PythonJapanesePaddleOcrEngineFactory : IOcrEngineFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "builtin.ocr.paddle-japanese-python",
        "Paddle OCR Japanese (Python)",
        "Runs official Python PaddleOCR Japanese recognition model through a persistent worker process.",
        []);

    public IOcrEngine Create(IReadOnlyDictionary<string, string> parameters)
    {
        return new PythonJapanesePaddleOcrEngine(parameters);
    }
}
