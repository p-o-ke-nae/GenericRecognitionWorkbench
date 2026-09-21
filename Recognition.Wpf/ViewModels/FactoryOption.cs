using Recognition.Core;

namespace Recognition.Wpf;

public sealed class FactoryOption<TFactory> : ObservableObject
{
    private readonly UiLocalization localization;

    public FactoryOption(TFactory factory, ComponentDescriptor descriptor, UiLocalization localization)
    {
        Factory = factory;
        Descriptor = descriptor;
        this.localization = localization;
        localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UiLocalization.CurrentLanguage))
            {
                RaisePropertyChanged(nameof(DisplayName));
            }
        };
    }

    public TFactory Factory { get; }

    public ComponentDescriptor Descriptor { get; }

    public string DisplayName => localization.Component(Descriptor.Id, Descriptor.DisplayName);
}
