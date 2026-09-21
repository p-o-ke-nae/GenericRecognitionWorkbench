using System.Globalization;
using Recognition.Core;

namespace Recognition.Wpf;

public sealed class DistanceMeasurementReferenceViewModel : ObservableObject
{
    private readonly UiLocalization localization;
    private string name;
    private string templatePath = string.Empty;
    private string thresholdText = "0.92";

    public DistanceMeasurementReferenceViewModel(UiLocalization localization, string name)
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

    public string ThresholdText
    {
        get => thresholdText;
        set => SetProperty(ref thresholdText, NumericInputHelper.Normalize(value));
    }

    public DistanceMeasurementReferenceConfiguration BuildConfiguration()
    {
        return new DistanceMeasurementReferenceConfiguration
        {
            Name = string.IsNullOrWhiteSpace(Name) ? localization["DistanceReferenceDefaultName"] : Name,
            TemplatePath = TemplatePath,
            Threshold = NumericInputHelper.ParseDoubleOrDefault(ThresholdText, 0.92d)
        };
    }

    public void ApplyConfiguration(DistanceMeasurementReferenceConfiguration configuration)
    {
        Name = configuration.Name;
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
