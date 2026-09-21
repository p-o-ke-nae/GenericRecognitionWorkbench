using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Recognition.Wpf;

public partial class ImagePathEditorControl : UserControl
{
    public static readonly DependencyProperty LabelTextProperty =
        DependencyProperty.Register(nameof(LabelText), typeof(string), typeof(ImagePathEditorControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PathTextProperty =
        DependencyProperty.Register(nameof(PathText), typeof(string), typeof(ImagePathEditorControl), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty BrowseCommandProperty =
        DependencyProperty.Register(nameof(BrowseCommand), typeof(ICommand), typeof(ImagePathEditorControl));

    public static readonly DependencyProperty CreateRawCommandProperty =
        DependencyProperty.Register(nameof(CreateRawCommand), typeof(ICommand), typeof(ImagePathEditorControl));

    public static readonly DependencyProperty CreateProcessedCommandProperty =
        DependencyProperty.Register(nameof(CreateProcessedCommand), typeof(ICommand), typeof(ImagePathEditorControl));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(ImagePathEditorControl));

    public static readonly DependencyProperty BrowseButtonTextProperty =
        DependencyProperty.Register(nameof(BrowseButtonText), typeof(string), typeof(ImagePathEditorControl), new PropertyMetadata("..."));

    public static readonly DependencyProperty RawCaptureButtonTextProperty =
        DependencyProperty.Register(nameof(RawCaptureButtonText), typeof(string), typeof(ImagePathEditorControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ProcessedCaptureButtonTextProperty =
        DependencyProperty.Register(nameof(ProcessedCaptureButtonText), typeof(string), typeof(ImagePathEditorControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty EditOverwriteButtonTextProperty =
        DependencyProperty.Register(nameof(EditOverwriteButtonText), typeof(string), typeof(ImagePathEditorControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty EditSaveAsButtonTextProperty =
        DependencyProperty.Register(nameof(EditSaveAsButtonText), typeof(string), typeof(ImagePathEditorControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty EditOverwriteCommandProperty =
        DependencyProperty.Register(nameof(EditOverwriteCommand), typeof(ICommand), typeof(ImagePathEditorControl));

    public static readonly DependencyProperty EditSaveAsCommandProperty =
        DependencyProperty.Register(nameof(EditSaveAsCommand), typeof(ICommand), typeof(ImagePathEditorControl));

    public static readonly DependencyProperty ShowCreateButtonsProperty =
        DependencyProperty.Register(nameof(ShowCreateButtons), typeof(bool), typeof(ImagePathEditorControl), new PropertyMetadata(true));

    public ImagePathEditorControl()
    {
        InitializeComponent();
    }

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    public string PathText
    {
        get => (string)GetValue(PathTextProperty);
        set => SetValue(PathTextProperty, value);
    }

    public ICommand? BrowseCommand
    {
        get => (ICommand?)GetValue(BrowseCommandProperty);
        set => SetValue(BrowseCommandProperty, value);
    }

    public ICommand? CreateRawCommand
    {
        get => (ICommand?)GetValue(CreateRawCommandProperty);
        set => SetValue(CreateRawCommandProperty, value);
    }

    public ICommand? CreateProcessedCommand
    {
        get => (ICommand?)GetValue(CreateProcessedCommandProperty);
        set => SetValue(CreateProcessedCommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public string BrowseButtonText
    {
        get => (string)GetValue(BrowseButtonTextProperty);
        set => SetValue(BrowseButtonTextProperty, value);
    }

    public string RawCaptureButtonText
    {
        get => (string)GetValue(RawCaptureButtonTextProperty);
        set => SetValue(RawCaptureButtonTextProperty, value);
    }

    public string ProcessedCaptureButtonText
    {
        get => (string)GetValue(ProcessedCaptureButtonTextProperty);
        set => SetValue(ProcessedCaptureButtonTextProperty, value);
    }

    public string EditOverwriteButtonText
    {
        get => (string)GetValue(EditOverwriteButtonTextProperty);
        set => SetValue(EditOverwriteButtonTextProperty, value);
    }

    public string EditSaveAsButtonText
    {
        get => (string)GetValue(EditSaveAsButtonTextProperty);
        set => SetValue(EditSaveAsButtonTextProperty, value);
    }

    public ICommand? EditOverwriteCommand
    {
        get => (ICommand?)GetValue(EditOverwriteCommandProperty);
        set => SetValue(EditOverwriteCommandProperty, value);
    }

    public ICommand? EditSaveAsCommand
    {
        get => (ICommand?)GetValue(EditSaveAsCommandProperty);
        set => SetValue(EditSaveAsCommandProperty, value);
    }

    public bool ShowCreateButtons
    {
        get => (bool)GetValue(ShowCreateButtonsProperty);
        set => SetValue(ShowCreateButtonsProperty, value);
    }
}
