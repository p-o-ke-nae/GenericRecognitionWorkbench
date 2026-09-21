using Recognition.Core;

namespace Recognition.Wpf;

public sealed class ParameterEntryViewModel : ObservableObject
{
    private readonly string componentId;
    private readonly UiLocalization localization;
    private string value;

    public ParameterEntryViewModel(string componentId, UiLocalization localization, ParameterDefinition definition, string? initialValue = null)
    {
        this.componentId = componentId;
        this.localization = localization;
        Definition = definition;
        value = string.IsNullOrWhiteSpace(initialValue) ? definition.DefaultValue : initialValue;
        Options = definition.Options?.Select(option => new LocalizedParameterOption(componentId, definition.Key, option, localization)).ToArray()
            ?? [];
        localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UiLocalization.CurrentLanguage))
            {
                RaisePropertyChanged(nameof(DisplayName));
                RaisePropertyChanged(nameof(SelectedOption));
            }
        };
    }

    public ParameterDefinition Definition { get; }

    public string DisplayName => localization.Parameter(componentId, Definition.Key, Definition.DisplayName);

    public IReadOnlyList<LocalizedParameterOption> Options { get; }

    public bool IsNumeric => Definition.ValueKind is ParameterValueKind.Integer or ParameterValueKind.Decimal;

    public bool IsImagePath => Definition.ValueKind == ParameterValueKind.FilePath
        && (Definition.Key.Contains("TemplatePath", StringComparison.OrdinalIgnoreCase)
            || Definition.Key.Contains("ImagePath", StringComparison.OrdinalIgnoreCase));

    public bool AllowsDecimal => Definition.ValueKind == ParameterValueKind.Decimal;

    public double SpinStep => Definition.Step ?? (AllowsDecimal ? 0.1d : 1d);

    public string Value
    {
        get => value;
        set
        {
            if (SetProperty(ref this.value, value ?? string.Empty))
            {
                RaisePropertyChanged(nameof(BooleanValue));
                RaisePropertyChanged(nameof(SelectedOption));
            }
        }
    }

    public bool BooleanValue
    {
        get => bool.TryParse(Value, out var parsed) && parsed;
        set => Value = value.ToString();
    }

    public LocalizedParameterOption? SelectedOption
    {
        get => Options.FirstOrDefault(option => string.Equals(option.Value, Value, StringComparison.OrdinalIgnoreCase))
            ?? Options.FirstOrDefault();
        set => Value = value?.Value ?? Definition.DefaultValue;
    }

    public bool IsValidNumericText(string proposedText)
    {
        return NumericInputHelper.IsValidProposedText(proposedText, AllowsDecimal, Definition.Minimum);
    }

    public void StepValue(int direction)
    {
        Value = NumericInputHelper.StepValue(Value, Definition.DefaultValue, AllowsDecimal, SpinStep, direction, Definition.Minimum, Definition.Maximum);
    }
}
