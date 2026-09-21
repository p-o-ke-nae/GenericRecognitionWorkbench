using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class MissingTemplateRecognitionMethod : IRecognitionMethod
{
    public RecognitionMatch Evaluate(RecognitionFrame frame)
    {
        return new RecognitionMatch(false, 0d, "template-missing");
    }

    public void Dispose()
    {
    }
}
