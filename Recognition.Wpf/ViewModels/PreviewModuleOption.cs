namespace Recognition.Wpf;

public sealed class PreviewModuleOption : ObservableObject
{
    private readonly UiLocalization localization;

    public PreviewModuleOption(IPreviewModule module, UiLocalization localization)
    {
        Module = module;
        this.localization = localization;
    }

    public IPreviewModule Module { get; }

    public string Label => Module.GetDisplayName(localization);

    public void Refresh()
    {
        RaisePropertyChanged(nameof(Label));
    }
}
