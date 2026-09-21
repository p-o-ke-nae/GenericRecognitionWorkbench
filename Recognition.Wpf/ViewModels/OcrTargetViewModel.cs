using System.Collections.ObjectModel;
using Recognition.Core;

namespace Recognition.Wpf;

public sealed class OcrTargetViewModel : ObservableObject
{
    private static readonly IReadOnlyList<ParameterDefinition> RegionDefinitions =
    [
        new("X", "X", ParameterValueKind.Integer, "0", Step: 10),
        new("Y", "Y", ParameterValueKind.Integer, "0", Step: 10),
        new("Width", "Width", ParameterValueKind.Integer, "0", Minimum: 0, Step: 10),
        new("Height", "Height", ParameterValueKind.Integer, "0", Minimum: 0, Step: 10)
    ];

    private readonly UiLocalization localization;
    private IImageProcessorFactory[] availableFactories;
    private string name;
    private string referenceName = string.Empty;
    private bool useRecognitionAnchor;

    public OcrTargetViewModel(UiLocalization localization, IEnumerable<IImageProcessorFactory> availableFactories, string name)
    {
        this.localization = localization;
        this.name = name;
        this.availableFactories = availableFactories.ToArray();
        AddPreprocessorCommand = new DelegateCommand(AddPreprocessor);

        foreach (var definition in RegionDefinitions)
        {
            RegionParameters.Add(new ParameterEntryViewModel("builtin.ocr-target", localization, definition));
        }

        RefreshFactories(this.availableFactories);
    }

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    public bool UseRecognitionAnchor
    {
        get => useRecognitionAnchor;
        set
        {
            if (SetProperty(ref useRecognitionAnchor, value) && !value)
            {
                ReferenceName = string.Empty;
            }
        }
    }

    public string ReferenceName
    {
        get => referenceName;
        set => SetProperty(ref referenceName, value);
    }

    public ObservableCollection<ParameterEntryViewModel> RegionParameters { get; } = [];

    public ObservableCollection<ComponentSelectionViewModel<IImageProcessorFactory>> Preprocessors { get; } = [];

    public DelegateCommand AddPreprocessorCommand { get; }

    public void RefreshFactories(IEnumerable<IImageProcessorFactory> availableFactories)
    {
        availableFactories = availableFactories.ToArray();
        this.availableFactories = [.. availableFactories];
        foreach (var preprocessor in Preprocessors)
        {
            preprocessor.Refresh(this.availableFactories);
        }
    }

    public void RemovePreprocessor(ComponentSelectionViewModel<IImageProcessorFactory> preprocessor)
    {
        Preprocessors.Remove(preprocessor);
    }

    public void ApplyRegion(RoiArea region, RoiArea? anchorRegion = null, bool useRelativeAnchor = false)
    {
        var anchorX = useRelativeAnchor && anchorRegion is { IsEmpty: false } ? anchorRegion.Value.X : 0;
        var anchorY = useRelativeAnchor && anchorRegion is { IsEmpty: false } ? anchorRegion.Value.Y : 0;
        var offsetX = useRelativeAnchor ? region.X - anchorX : region.X;
        var offsetY = useRelativeAnchor ? region.Y - anchorY : region.Y;
        SetRegionValue("X", offsetX);
        SetRegionValue("Y", offsetY);
        SetRegionValue("Width", region.Width);
        SetRegionValue("Height", region.Height);
    }

    public RoiArea GetRegion()
    {
        return new RoiArea(
            NumericInputHelper.ParseInt32OrDefault(GetRegionValue("X"), 0),
            NumericInputHelper.ParseInt32OrDefault(GetRegionValue("Y"), 0),
            NumericInputHelper.ParseInt32OrDefault(GetRegionValue("Width"), 0),
            NumericInputHelper.ParseInt32OrDefault(GetRegionValue("Height"), 0));
    }

    public OcrTargetConfiguration BuildConfiguration()
    {
        return new OcrTargetConfiguration
        {
            Name = string.IsNullOrWhiteSpace(Name) ? localization["OcrTargetDefaultName"] : Name,
            ReferenceName = UseRecognitionAnchor ? ReferenceName : string.Empty,
            Region = GetRegion(),
            UseRecognitionAnchor = UseRecognitionAnchor,
            Preprocessors = [.. Preprocessors.Select(static preprocessor => preprocessor.BuildConfiguration())]
        };
    }

    public void ApplyConfiguration(OcrTargetConfiguration configuration, IEnumerable<IImageProcessorFactory> availableFactories)
    {
        Name = configuration.Name;
        ReferenceName = configuration.ReferenceName;
        UseRecognitionAnchor = configuration.UseRecognitionAnchor;
        ApplyRegion(configuration.Region);

        Preprocessors.Clear();
        foreach (var preprocessorConfiguration in configuration.Preprocessors)
        {
            var selection = new ComponentSelectionViewModel<IImageProcessorFactory>(localization, static factory => factory.Descriptor);
            selection.Refresh(this.availableFactories);
            selection.ApplyConfiguration(preprocessorConfiguration);
            Preprocessors.Add(selection);
        }
    }

    private void AddPreprocessor()
    {
        var selection = new ComponentSelectionViewModel<IImageProcessorFactory>(localization, static factory => factory.Descriptor);
        selection.Refresh(availableFactories);
        Preprocessors.Add(selection);
    }

    private string GetRegionValue(string key)
    {
        return RegionParameters.First(entry => string.Equals(entry.Definition.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private void SetRegionValue(string key, int value)
    {
        RegionParameters.First(entry => string.Equals(entry.Definition.Key, key, StringComparison.OrdinalIgnoreCase)).Value = value.ToString();
    }
}
