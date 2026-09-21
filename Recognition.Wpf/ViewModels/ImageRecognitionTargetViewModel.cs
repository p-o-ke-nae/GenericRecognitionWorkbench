using System.Collections.ObjectModel;
using Recognition.Core;

namespace Recognition.Wpf;

public sealed class ImageRecognitionTargetViewModel : ObservableObject
{
    private readonly UiLocalization localization;
    private readonly IEnumerable<IRecognitionMethodFactory> recognitionFactories;
    private readonly IEnumerable<IImageProcessorFactory> imageProcessorFactories;
    private string name;

    public ImageRecognitionTargetViewModel(
        UiLocalization localization,
        IEnumerable<IRecognitionMethodFactory> recognitionFactories,
        IEnumerable<IImageProcessorFactory> imageProcessorFactories,
        string name)
    {
        this.localization = localization;
        this.recognitionFactories = recognitionFactories.ToArray();
        this.imageProcessorFactories = imageProcessorFactories.ToArray();
        this.name = name;

        Recognizer = new ComponentSelectionViewModel<IRecognitionMethodFactory>(localization, static factory => factory.Descriptor);
        Recognizer.Refresh(this.recognitionFactories);
        AddPreprocessorCommand = new DelegateCommand(AddPreprocessor);
    }

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    public ComponentSelectionViewModel<IRecognitionMethodFactory> Recognizer { get; }

    public ObservableCollection<ComponentSelectionViewModel<IImageProcessorFactory>> Preprocessors { get; } = [];

    public DelegateCommand AddPreprocessorCommand { get; }

    public void RefreshFactories(IEnumerable<IRecognitionMethodFactory> recognizerFactories, IEnumerable<IImageProcessorFactory> processorFactories)
    {
        Recognizer.Refresh(recognizerFactories, Recognizer.SelectedOption?.Descriptor.Id);
        foreach (var preprocessor in Preprocessors)
        {
            preprocessor.Refresh(processorFactories, preprocessor.SelectedOption?.Descriptor.Id);
        }
    }

    public void RemovePreprocessor(ComponentSelectionViewModel<IImageProcessorFactory> preprocessor)
    {
        Preprocessors.Remove(preprocessor);
    }

    public ImageRecognitionTargetConfiguration BuildConfiguration()
    {
        return new ImageRecognitionTargetConfiguration
        {
            Name = string.IsNullOrWhiteSpace(Name) ? localization["ImageRecognitionTargetDefaultName"] : Name,
            Recognizer = Recognizer.BuildConfiguration(),
            Preprocessors = [.. Preprocessors.Select(static preprocessor => preprocessor.BuildConfiguration())]
        };
    }

    public void ApplyConfiguration(ImageRecognitionTargetConfiguration configuration)
    {
        Name = configuration.Name;
        Recognizer.ApplyConfiguration(configuration.Recognizer);

        Preprocessors.Clear();
        foreach (var preprocessorConfiguration in configuration.Preprocessors)
        {
            var selection = new ComponentSelectionViewModel<IImageProcessorFactory>(localization, static factory => factory.Descriptor);
            selection.Refresh(imageProcessorFactories);
            selection.ApplyConfiguration(preprocessorConfiguration);
            Preprocessors.Add(selection);
        }
    }

    private void AddPreprocessor()
    {
        var selection = new ComponentSelectionViewModel<IImageProcessorFactory>(localization, static factory => factory.Descriptor);
        selection.Refresh(imageProcessorFactories);
        Preprocessors.Add(selection);
    }
}
