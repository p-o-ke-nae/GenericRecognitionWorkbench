using System.Collections.ObjectModel;
using Recognition.Core;

namespace Recognition.Wpf;

public sealed class ComponentSelectionViewModel<TFactory> : ObservableObject
{
    private readonly Func<TFactory, ComponentDescriptor> descriptorSelector;
    private readonly UiLocalization localization;
    private FactoryOption<TFactory>? selectedOption;

    public ComponentSelectionViewModel(UiLocalization localization, Func<TFactory, ComponentDescriptor> descriptorSelector)
    {
        this.localization = localization;
        this.descriptorSelector = descriptorSelector;
    }

    public ObservableCollection<FactoryOption<TFactory>> Options { get; } = [];

    public ObservableCollection<ParameterEntryViewModel> Parameters { get; } = [];

    public FactoryOption<TFactory>? SelectedOption
    {
        get => selectedOption;
        set
        {
            if (SetProperty(ref selectedOption, value))
            {
                RaisePropertyChanged(nameof(SupportsCaptureAreaSelection));
                RebuildParameters(null);
            }
        }
    }

    public bool SupportsCaptureAreaSelection =>
        string.Equals(SelectedOption?.Descriptor.Id, "builtin.preprocess.crop", StringComparison.OrdinalIgnoreCase);

    public void Refresh(IEnumerable<TFactory> factories, string? selectedId = null)
    {
        var previousId = selectedId ?? SelectedOption?.Descriptor.Id;
        Options.Clear();

        foreach (var factory in factories)
        {
            Options.Add(new FactoryOption<TFactory>(factory, descriptorSelector(factory), localization));
        }

        SelectedOption = Options.FirstOrDefault(option => string.Equals(option.Descriptor.Id, previousId, StringComparison.OrdinalIgnoreCase))
            ?? Options.FirstOrDefault();
    }

    public ComponentConfiguration BuildConfiguration()
    {
        return new ComponentConfiguration
        {
            ComponentId = SelectedOption?.Descriptor.Id ?? string.Empty,
            Parameters = Parameters.ToDictionary(entry => entry.Definition.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase)
        };
    }

    public void ApplyConfiguration(ComponentConfiguration configuration)
    {
        SelectedOption = Options.FirstOrDefault(option => string.Equals(option.Descriptor.Id, configuration.ComponentId, StringComparison.OrdinalIgnoreCase))
            ?? Options.FirstOrDefault();
        RebuildParameters(configuration.Parameters);
    }

    private void RebuildParameters(IReadOnlyDictionary<string, string>? values)
    {
        Parameters.Clear();
        if (SelectedOption is null)
        {
            return;
        }

        foreach (var definition in SelectedOption.Descriptor.Parameters)
        {
            string? configuredValue = null;
            values?.TryGetValue(definition.Key, out configuredValue);
            Parameters.Add(new ParameterEntryViewModel(SelectedOption.Descriptor.Id, localization, definition, configuredValue));
        }
    }
}
