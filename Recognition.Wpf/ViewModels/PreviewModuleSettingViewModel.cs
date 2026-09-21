namespace Recognition.Wpf;

public sealed class PreviewModuleSettingViewModel : ObservableObject
{
    private readonly Func<bool> getter;
    private readonly Action<bool> setter;
    private readonly UiLocalization localization;
    private readonly string labelKey;

    public PreviewModuleSettingViewModel(UiLocalization localization, string labelKey, Func<bool> getter, Action<bool> setter)
    {
        this.localization = localization;
        this.labelKey = labelKey;
        this.getter = getter;
        this.setter = setter;
    }

    public string Label => localization[labelKey];

    public bool IsEnabled
    {
        get => getter();
        set
        {
            if (value == getter())
            {
                return;
            }

            setter(value);
            RaisePropertyChanged();
        }
    }

    public void Refresh()
    {
        RaisePropertyChanged(nameof(Label));
        RaisePropertyChanged(nameof(IsEnabled));
    }
}
