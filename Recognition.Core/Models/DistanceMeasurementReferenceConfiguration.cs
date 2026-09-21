namespace Recognition.Core;

public sealed class DistanceMeasurementReferenceConfiguration
{
    public string Name { get; set; } = string.Empty;

    public string TemplatePath { get; set; } = string.Empty;

    public double Threshold { get; set; } = 0.92d;

    public DistanceMeasurementReferenceConfiguration Clone()
    {
        return new DistanceMeasurementReferenceConfiguration
        {
            Name = Name,
            TemplatePath = TemplatePath,
            Threshold = Threshold
        };
    }
}
