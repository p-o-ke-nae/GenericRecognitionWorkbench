namespace Recognition.Wpf;

public sealed class LocalizedChoice<T> : ObservableObject
{
    private readonly UiLocalization localization;
    private readonly Func<UiLocalization, T, string> labelFactory;

    public LocalizedChoice(T value, UiLocalization localization, Func<UiLocalization, T, string> labelFactory)
    {
        Value = value;
        this.localization = localization;
        this.labelFactory = labelFactory;
        localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UiLocalization.CurrentLanguage))
            {
                RaisePropertyChanged(nameof(Label));
            }
        };
    }

    public T Value { get; }

    public string Label => labelFactory(localization, Value);
}
