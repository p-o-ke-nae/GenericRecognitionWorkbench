namespace Recognition.Core;

public sealed class OcrPreviewFrame
{
    public OcrPreviewFrame(string name, RecognitionFrame frame, RoiArea region)
    {
        Name = name;
        Frame = frame;
        Region = region;
    }

    public string Name { get; }

    public RecognitionFrame Frame { get; }

    public RoiArea Region { get; }
}
