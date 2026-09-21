using Recognition.Core;

namespace Recognition.Wpf;

public sealed class LocalizedParameterOption : ObservableObject
{
    private readonly string componentId;
    private readonly string parameterKey;
    private readonly UiLocalization localization;

    public LocalizedParameterOption(string componentId, string parameterKey, ParameterOption option, UiLocalization localization)
    {
        this.componentId = componentId;
        this.parameterKey = parameterKey;
        Option = option;
        this.localization = localization;
        localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UiLocalization.CurrentLanguage))
            {
                RaisePropertyChanged(nameof(Label));
            }
        };
    }

    public ParameterOption Option { get; }

    public string Value => Option.Value;

    public string Label => localization.Option(componentId, parameterKey, Option.Value, Option.Label);
}
