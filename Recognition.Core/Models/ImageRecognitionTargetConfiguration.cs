namespace Recognition.Core;

public sealed class ImageRecognitionTargetConfiguration
{
    public string Name { get; set; } = "Image Recognition 1";

    public ComponentConfiguration Recognizer { get; set; } = new();

    public List<ComponentConfiguration> Preprocessors { get; set; } = [];

    public ImageRecognitionTargetConfiguration Clone()
    {
        return new ImageRecognitionTargetConfiguration
        {
            Name = Name,
            Recognizer = Recognizer.Clone(),
            Preprocessors = [.. Preprocessors.Select(static preprocessor => preprocessor.Clone())]
        };
    }
}
