namespace Recognition.Core;

public interface IOcrEngine : IDisposable
{
    OcrResult Read(RecognitionFrame frame);
}
