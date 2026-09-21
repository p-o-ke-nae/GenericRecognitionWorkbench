namespace Recognition.Core;

public sealed class OcrTargetConfiguration
{
    public string Name { get; set; } = "OCR 1";

    public string ReferenceName { get; set; } = string.Empty;

    public RoiArea Region { get; set; } = RoiArea.Empty;

    public bool UseRecognitionAnchor { get; set; }

    public List<ComponentConfiguration> Preprocessors { get; set; } = [];

    public OcrTargetConfiguration Clone()
    {
        return new OcrTargetConfiguration
        {
            Name = Name,
            ReferenceName = ReferenceName,
            Region = Region,
            UseRecognitionAnchor = UseRecognitionAnchor,
            Preprocessors = [.. Preprocessors.Select(static preprocessor => preprocessor.Clone())]
        };
    }
}
