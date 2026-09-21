namespace Recognition.Core;

public interface IImageProcessor
{
    RecognitionFrame Process(RecognitionFrame frame);
}
