using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Recognition.Core;

namespace Recognition.Wpf;

internal sealed class ImageCropSelectionWindow : Window
{
    private const double MinZoom = 0.25d;
    private const double MaxZoom = 8.0d;
    private const double ZoomStep = 0.25d;

    private BitmapSource imageSource;
    private readonly UiLocalization localization;
    private readonly Image imageControl;
    private readonly Canvas overlayCanvas;
    private readonly Rectangle selectionRectangle;
    private readonly TextBlock zoomValueText;
    private readonly Slider zoomSlider;
    private readonly Slider historySlider;
    private readonly TextBlock historyValueText;
    private readonly IReadOnlyList<RecognitionFrameHistoryEntry> historyEntries;
    private readonly Action<int>? historySelectionChanged;
    private readonly bool useProcessedFrame;
    private readonly Func<RecognitionFrameHistoryEntry, RecognitionFrame?>? sourceFrameResolver;
    private readonly Action<string>? showStatus;
    private readonly TextBox xTextBox;
    private readonly TextBox yTextBox;
    private readonly TextBox widthTextBox;
    private readonly TextBox heightTextBox;
    private Point? dragStart;
    private RoiArea currentRegion;
    private double zoomFactor = 1.0d;
    private bool suppressRegionTextUpdates;

    public ImageCropSelectionWindow(
        BitmapSource imageSource,
        string title,
        UiLocalization localization,
        RoiArea? initialRegion = null,
        IReadOnlyList<RecognitionFrameHistoryEntry>? historyEntries = null,
        int selectedHistoryIndex = -1,
        Action<int>? historySelectionChanged = null,
        bool useProcessedFrame = false,
        Func<RecognitionFrameHistoryEntry, RecognitionFrame?>? sourceFrameResolver = null,
        Action<string>? showStatus = null)
    {
        this.imageSource = imageSource;
        this.localization = localization;
        this.historyEntries = historyEntries ?? [];
        this.historySelectionChanged = historySelectionChanged;
        this.useProcessedFrame = useProcessedFrame;
        this.sourceFrameResolver = sourceFrameResolver;
        this.showStatus = showStatus;
        SelectedHistoryIndex = this.historyEntries.Count == 0
            ? -1
            : Math.Clamp(selectedHistoryIndex, 0, this.historyEntries.Count - 1);

        Title = title;
        Width = 1100;
        Height = 850;
        MinWidth = 860;
        MinHeight = 680;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        imageControl = new Image
        {
            Source = imageSource,
            Stretch = Stretch.Fill
        };

        overlayCanvas = new Canvas
        {
            Background = Brushes.Transparent
        };

        selectionRectangle = new Rectangle
        {
            Stroke = Brushes.OrangeRed,
            StrokeThickness = 2,
            Fill = new SolidColorBrush(Color.FromArgb(40, 255, 69, 0))
        };
        overlayCanvas.Children.Add(selectionRectangle);

        var imageLayer = new Grid();
        imageLayer.Children.Add(imageControl);
        imageLayer.Children.Add(overlayCanvas);

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = imageLayer
        };
        scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;

        zoomValueText = new TextBlock
        {
            Width = 56,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Right
        };

        zoomSlider = new Slider
        {
            Width = 180,
            Minimum = MinZoom,
            Maximum = MaxZoom,
            TickFrequency = ZoomStep,
            SmallChange = ZoomStep,
            LargeChange = 1.0d,
            IsSnapToTickEnabled = true,
            VerticalAlignment = VerticalAlignment.Center,
            Value = zoomFactor
        };
        zoomSlider.ValueChanged += (_, _) => ApplyZoom(zoomSlider.Value);

        var toolbar = new WrapPanel
        {
            Margin = new Thickness(0, 0, 0, 8),
            VerticalAlignment = VerticalAlignment.Center
        };
        toolbar.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Text = localization["Zoom"]
        });
        toolbar.Children.Add(CreateToolbarButton("-", (_, _) => ApplyZoom(zoomFactor - ZoomStep)));
        toolbar.Children.Add(zoomSlider);
        toolbar.Children.Add(CreateToolbarButton("+", (_, _) => ApplyZoom(zoomFactor + ZoomStep)));
        toolbar.Children.Add(zoomValueText);

        if (this.historyEntries.Count > 0)
        {
            toolbar.Children.Add(new TextBlock
            {
                Margin = new Thickness(16, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Text = "履歴"
            });
            toolbar.Children.Add(CreateToolbarButton("<", (_, _) => SelectHistoryFrame(SelectedHistoryIndex - 1)));
            historySlider = new Slider
            {
                Width = 180,
                Minimum = 0,
                Maximum = Math.Max(0, this.historyEntries.Count - 1),
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                VerticalAlignment = VerticalAlignment.Center,
                Value = Math.Max(0, SelectedHistoryIndex)
            };
            historySlider.ValueChanged += (_, _) =>
                SelectHistoryFrame((int)Math.Round(historySlider.Value));
            toolbar.Children.Add(historySlider);
            toolbar.Children.Add(CreateToolbarButton(">", (_, _) => SelectHistoryFrame(SelectedHistoryIndex + 1)));
            historyValueText = new TextBlock
            {
                Width = 80,
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            historyValueText.Text = $"{SelectedHistoryIndex + 1}/{this.historyEntries.Count}";
            toolbar.Children.Add(historyValueText);
        }
        else
        {
            historySlider = null!;
            historyValueText = null!;
        }

        xTextBox = CreateRegionEditor(localization.Parameter("builtin.preprocess.crop", "X", "X"), out var xPanel);
        yTextBox = CreateRegionEditor(localization.Parameter("builtin.preprocess.crop", "Y", "Y"), out var yPanel);
        widthTextBox = CreateRegionEditor(localization.Parameter("builtin.preprocess.crop", "Width", "Width"), out var widthPanel);
        heightTextBox = CreateRegionEditor(localization.Parameter("builtin.preprocess.crop", "Height", "Height"), out var heightPanel);

        var regionEditors = new WrapPanel
        {
            Margin = new Thickness(0, 8, 0, 0)
        };
        regionEditors.Children.Add(xPanel);
        regionEditors.Children.Add(yPanel);
        regionEditors.Children.Add(widthPanel);
        regionEditors.Children.Add(heightPanel);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        buttonPanel.Children.Add(CreateActionButton(localization["CropSelectionConfirm"], ConfirmSelection));
        buttonPanel.Children.Add(CreateActionButton(localization["CropSelectionCancel"], (_, _) => DialogResult = false));

        var root = new DockPanel
        {
            Margin = new Thickness(12)
        };
        DockPanel.SetDock(toolbar, Dock.Top);
        DockPanel.SetDock(regionEditors, Dock.Bottom);
        DockPanel.SetDock(buttonPanel, Dock.Bottom);
        root.Children.Add(toolbar);
        root.Children.Add(regionEditors);
        root.Children.Add(buttonPanel);
        root.Children.Add(scrollViewer);
        Content = root;

        overlayCanvas.MouseLeftButtonDown += OnMouseLeftButtonDown;
        overlayCanvas.MouseMove += OnMouseMove;
        overlayCanvas.MouseLeftButtonUp += OnMouseLeftButtonUp;
        KeyDown += OnKeyDown;

        ApplyZoom(zoomFactor);
        SetCurrentRegion(NormalizeRegion(initialRegion ?? CreateDefaultRegion()), updateTextBoxes: true);
    }

    public RoiArea? SelectedRegion { get; private set; }

    public int SelectedHistoryIndex { get; private set; }

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            return;
        }

        ApplyZoom(zoomFactor + (e.Delta > 0 ? ZoomStep : -ZoomStep));
        e.Handled = true;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        dragStart = ToImagePoint(e.GetPosition(overlayCanvas));
        overlayCanvas.CaptureMouse();
        UpdateDraggedSelection(dragStart.Value, dragStart.Value);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (dragStart is null)
        {
            return;
        }

        UpdateDraggedSelection(dragStart.Value, ToImagePoint(e.GetPosition(overlayCanvas)));
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (dragStart is null)
        {
            return;
        }

        UpdateDraggedSelection(dragStart.Value, ToImagePoint(e.GetPosition(overlayCanvas)));
        dragStart = null;
        overlayCanvas.ReleaseMouseCapture();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                DialogResult = false;
                break;
            case Key.Enter:
                ConfirmSelection(sender, new RoutedEventArgs());
                break;
        }
    }

    private void ConfirmSelection(object? sender, RoutedEventArgs e)
    {
        SelectedRegion = currentRegion;
        DialogResult = true;
    }

    private void UpdateDraggedSelection(Point start, Point end)
    {
        var left = (int)Math.Floor(Math.Min(start.X, end.X));
        var top = (int)Math.Floor(Math.Min(start.Y, end.Y));
        var right = (int)Math.Ceiling(Math.Max(start.X, end.X));
        var bottom = (int)Math.Ceiling(Math.Max(start.Y, end.Y));
        SetCurrentRegion(new RoiArea(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top)), updateTextBoxes: true);
    }

    private Point ToImagePoint(Point point)
    {
        var clampedX = Math.Clamp(point.X, 0d, overlayCanvas.Width);
        var clampedY = Math.Clamp(point.Y, 0d, overlayCanvas.Height);
        return new Point(clampedX / zoomFactor, clampedY / zoomFactor);
    }

    private void ApplyZoom(double requestedZoom)
    {
        zoomFactor = Math.Clamp(requestedZoom, MinZoom, MaxZoom);
        imageControl.Width = imageSource.PixelWidth * zoomFactor;
        imageControl.Height = imageSource.PixelHeight * zoomFactor;
        overlayCanvas.Width = imageControl.Width;
        overlayCanvas.Height = imageControl.Height;
        zoomSlider.Value = zoomFactor;
        zoomValueText.Text = $"{zoomFactor * 100:0}%";
        UpdateSelectionRectangle();
    }

    private void SelectHistoryFrame(int requestedIndex)
    {
        if (historyEntries.Count == 0)
        {
            return;
        }

        var index = Math.Clamp(requestedIndex, 0, historyEntries.Count - 1);
        if (index == SelectedHistoryIndex && imageSource.PixelWidth > 0)
        {
            return;
        }

        var entry = historyEntries[index];
        var selectedFrame = useProcessedFrame
            ? entry.ProcessedFrame
            : sourceFrameResolver?.Invoke(entry) ?? entry.SourceFrame;
        if (selectedFrame is null)
        {
            var message = localization["SourceHistoryUnavailable"];
            showStatus?.Invoke(message);
            historyValueText.Width = 360;
            historyValueText.TextWrapping = TextWrapping.Wrap;
            historyValueText.ToolTip = message;
            historyValueText.Text = message;
            historySlider.Value = SelectedHistoryIndex;
            return;
        }
        SelectedHistoryIndex = index;
        imageSource = CreateBitmapSource(selectedFrame);
        imageControl.Source = imageSource;
        ApplyZoom(zoomFactor);
        SetCurrentRegion(CreateDefaultRegion(), updateTextBoxes: true);
        historySlider.Value = index;
        historyValueText.Width = 80;
        historyValueText.ToolTip = null;
        historyValueText.Text = $"{index + 1}/{historyEntries.Count}";
        historySelectionChanged?.Invoke(index);
    }

    private static BitmapSource CreateBitmapSource(RecognitionFrame frame)
    {
        var pixelFormat = frame.PixelFormat switch
        {
            FramePixelFormat.Bgra32 => PixelFormats.Bgra32,
            FramePixelFormat.Bgr24 => PixelFormats.Bgr24,
            FramePixelFormat.Gray8 => PixelFormats.Gray8,
            _ => PixelFormats.Bgra32
        };
        var bitmap = BitmapSource.Create(frame.Width, frame.Height, 96, 96, pixelFormat, null, frame.PixelData, frame.Stride);
        bitmap.Freeze();
        return bitmap;
    }

    private void SetCurrentRegion(RoiArea region, bool updateTextBoxes)
    {
        currentRegion = NormalizeRegion(region);
        SelectedRegion = currentRegion;
        UpdateSelectionRectangle();
        if (updateTextBoxes)
        {
            UpdateRegionTextBoxes();
        }
    }

    private RoiArea NormalizeRegion(RoiArea region)
    {
        var maxWidth = Math.Max(1, imageSource.PixelWidth);
        var maxHeight = Math.Max(1, imageSource.PixelHeight);
        var x = Math.Clamp(region.X, 0, maxWidth - 1);
        var y = Math.Clamp(region.Y, 0, maxHeight - 1);
        var width = Math.Clamp(region.Width, 1, maxWidth - x);
        var height = Math.Clamp(region.Height, 1, maxHeight - y);
        return new RoiArea(x, y, width, height);
    }

    private RoiArea CreateDefaultRegion()
    {
        return new RoiArea(0, 0, Math.Max(1, imageSource.PixelWidth), Math.Max(1, imageSource.PixelHeight));
    }

    private void UpdateSelectionRectangle()
    {
        Canvas.SetLeft(selectionRectangle, currentRegion.X * zoomFactor);
        Canvas.SetTop(selectionRectangle, currentRegion.Y * zoomFactor);
        selectionRectangle.Width = currentRegion.Width * zoomFactor;
        selectionRectangle.Height = currentRegion.Height * zoomFactor;
    }

    private void UpdateRegionTextBoxes()
    {
        suppressRegionTextUpdates = true;
        try
        {
            xTextBox.Text = currentRegion.X.ToString();
            yTextBox.Text = currentRegion.Y.ToString();
            widthTextBox.Text = currentRegion.Width.ToString();
            heightTextBox.Text = currentRegion.Height.ToString();
        }
        finally
        {
            suppressRegionTextUpdates = false;
        }
    }

    private TextBox CreateRegionEditor(string label, out FrameworkElement container)
    {
        var textBox = new TextBox
        {
            Width = 70,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0)
        };
        textBox.PreviewTextInput += RegionTextBox_PreviewTextInput;
        DataObject.AddPastingHandler(textBox, RegionTextBox_Pasting);
        textBox.TextChanged += RegionTextBox_TextChanged;
        textBox.LostFocus += (_, _) => UpdateRegionTextBoxes();

        var minusButton = CreateToolbarButton("-", (_, _) => StepRegionTextBox(textBox, -1));
        var plusButton = CreateToolbarButton("+", (_, _) => StepRegionTextBox(textBox, 1));
        minusButton.Margin = new Thickness(4, 0, 0, 0);
        plusButton.Margin = new Thickness(4, 0, 0, 0);

        var panel = new Grid
        {
            Margin = new Thickness(0, 0, 12, 4)
        };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        panel.Children.Add(new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Text = label
        });
        Grid.SetColumn(textBox, 1);
        panel.Children.Add(textBox);
        Grid.SetColumn(minusButton, 2);
        panel.Children.Add(minusButton);
        Grid.SetColumn(plusButton, 3);
        panel.Children.Add(plusButton);
        container = panel;
        return textBox;
    }

    private void RegionTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        var proposedText = BuildProposedText(textBox, e.Text);
        e.Handled = !NumericInputHelper.IsValidProposedText(proposedText, allowsDecimal: false, minimum: 0d);
    }

    private void RegionTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            e.CancelCommand();
            return;
        }

        var pastedText = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (!NumericInputHelper.IsValidProposedText(BuildProposedText(textBox, pastedText), allowsDecimal: false, minimum: 0d))
        {
            e.CancelCommand();
        }
    }

    private void RegionTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (suppressRegionTextUpdates)
        {
            return;
        }

        if (!TryBuildRegionFromText(out var region))
        {
            return;
        }

        SetCurrentRegion(region, updateTextBoxes: false);
    }

    private void StepRegionTextBox(TextBox textBox, int direction)
    {
        var minimum = textBox == widthTextBox || textBox == heightTextBox ? 1d : 0d;
        textBox.Text = NumericInputHelper.StepValue(textBox.Text, minimum == 0d ? "0" : "1", allowsDecimal: false, step: 1d, direction, minimum: minimum);
    }

    private bool TryBuildRegionFromText(out RoiArea region)
    {
        if (!int.TryParse(xTextBox.Text, out var x)
            || !int.TryParse(yTextBox.Text, out var y)
            || !int.TryParse(widthTextBox.Text, out var width)
            || !int.TryParse(heightTextBox.Text, out var height))
        {
            region = currentRegion;
            return false;
        }

        region = new RoiArea(x, y, width, height);
        return true;
    }

    private static string BuildProposedText(TextBox textBox, string input)
    {
        var currentText = textBox.Text ?? string.Empty;
        var selectionStart = textBox.SelectionStart;
        var selectionLength = textBox.SelectionLength;
        return currentText.Remove(selectionStart, selectionLength).Insert(selectionStart, input);
    }

    private static Button CreateToolbarButton(string content, RoutedEventHandler onClick)
    {
        var button = new Button
        {
            Width = 28,
            Height = 24,
            Padding = new Thickness(0),
            Content = content,
            Margin = new Thickness(4, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        button.Click += onClick;
        return button;
    }

    private static Button CreateActionButton(string content, RoutedEventHandler onClick)
    {
        var button = new Button
        {
            MinWidth = 96,
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(8, 0, 0, 0),
            Content = content
        };
        button.Click += onClick;
        return button;
    }
}
