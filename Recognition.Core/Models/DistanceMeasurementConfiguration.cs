namespace Recognition.Core;

public sealed class DistanceMeasurementConfiguration
{
    public string ReferenceTemplatePath { get; set; } = string.Empty;

    public double ReferenceThreshold { get; set; } = 0.92d;

    public List<DistanceMeasurementReferenceConfiguration> References { get; set; } = [];

    public List<DistanceMeasurementTargetConfiguration> Targets { get; set; } = [];

    public string TargetTemplatePath { get; set; } = string.Empty;

    public double TargetThreshold { get; set; } = 0.92d;

    public DistanceMeasurementConfiguration Clone()
    {
        return new DistanceMeasurementConfiguration
        {
            ReferenceTemplatePath = ReferenceTemplatePath,
            ReferenceThreshold = ReferenceThreshold,
            References = [.. References.Select(static reference => reference.Clone())],
            Targets = [.. Targets.Select(static target => target.Clone())],
            TargetTemplatePath = TargetTemplatePath,
            TargetThreshold = TargetThreshold
        };
    }
}
