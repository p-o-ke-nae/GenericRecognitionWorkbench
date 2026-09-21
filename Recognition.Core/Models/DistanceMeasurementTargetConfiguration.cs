namespace Recognition.Core;

public sealed class DistanceMeasurementTargetConfiguration
{
    public string Name { get; set; } = string.Empty;

    public string ReferenceName { get; set; } = string.Empty;

    public string TemplatePath { get; set; } = string.Empty;

    public double Threshold { get; set; } = 0.92d;

    public DistanceMeasurementTargetConfiguration Clone()
    {
        return new DistanceMeasurementTargetConfiguration
        {
            Name = Name,
            ReferenceName = ReferenceName,
            TemplatePath = TemplatePath,
            Threshold = Threshold
        };
    }
}
