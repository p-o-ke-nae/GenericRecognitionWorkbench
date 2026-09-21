namespace Recognition.Core;

public interface IRecognitionMethod : IDisposable
{
    RecognitionMatch Evaluate(RecognitionFrame frame);
}
