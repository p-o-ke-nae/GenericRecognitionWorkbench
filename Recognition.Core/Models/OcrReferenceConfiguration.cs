namespace Recognition.Core;

public sealed class OcrReferenceConfiguration
{
    public string Name { get; set; } = string.Empty;

    public string TemplatePath { get; set; } = string.Empty;

    public double Threshold { get; set; } = 0.92d;

    public OcrReferenceConfiguration Clone()
    {
        return new OcrReferenceConfiguration
        {
            Name = Name,
            TemplatePath = TemplatePath,
            Threshold = Threshold
        };
    }
}
