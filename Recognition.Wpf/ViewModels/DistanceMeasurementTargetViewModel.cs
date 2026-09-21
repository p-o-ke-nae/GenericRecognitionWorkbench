using System.Globalization;
using Recognition.Core;

namespace Recognition.Wpf;

public sealed class DistanceMeasurementTargetViewModel : ObservableObject
{
    private readonly UiLocalization localization;
    private string name;
    private string referenceName = string.Empty;
    private string templatePath = string.Empty;
    private string thresholdText = "0.92";

    public DistanceMeasurementTargetViewModel(UiLocalization localization, string name)
    {
        this.localization = localization;
        this.name = name;
    }

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    public string TemplatePath
    {
        get => templatePath;
        set => SetProperty(ref templatePath, value);
    }

    public string ReferenceName
    {
        get => referenceName;
        set => SetProperty(ref referenceName, value);
    }

    public string ThresholdText
    {
        get => thresholdText;
        set => SetProperty(ref thresholdText, NumericInputHelper.Normalize(value));
    }

    public DistanceMeasurementTargetConfiguration BuildConfiguration()
    {
        return new DistanceMeasurementTargetConfiguration
        {
            Name = string.IsNullOrWhiteSpace(Name) ? localization["DistanceTargetDefaultName"] : Name,
            ReferenceName = ReferenceName,
            TemplatePath = TemplatePath,
            Threshold = NumericInputHelper.ParseDoubleOrDefault(ThresholdText, 0.92d)
        };
    }

    public void ApplyConfiguration(DistanceMeasurementTargetConfiguration configuration)
    {
        Name = configuration.Name;
        ReferenceName = configuration.ReferenceName;
        TemplatePath = configuration.TemplatePath;
        ThresholdText = configuration.Threshold.ToString("F2", CultureInfo.InvariantCulture);
    }

    public bool IsValidThresholdText(string proposedText)
    {
        return NumericInputHelper.IsValidProposedText(proposedText, allowsDecimal: true, minimum: 0d);
    }

    public void StepThreshold(int direction)
    {
        ThresholdText = NumericInputHelper.StepValue(ThresholdText, "0.92", allowsDecimal: true, step: 0.01d, direction, minimum: 0d, maximum: 1d);
    }
}
