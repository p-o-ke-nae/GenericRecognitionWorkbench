using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Recognition.Core;

namespace Recognition.Wpf;

public partial class RecognitionWorkbenchControl : UserControl
{
    public event EventHandler<RecognitionCycleResult>? RecognitionEventRaised;

    public RecognitionWorkbenchControl()
    {
        InitializeComponent();
    }

    public RecognitionWorkbenchViewModel? ViewModel { get; private set; }

    public void Initialize(IRecognitionPluginCatalog pluginCatalog, IRecognitionRunner runner, IRecognitionProfileStore profileStore, IRecognitionProfileCalibrationService calibrationService)
    {
        Initialize(new RecognitionWorkbenchViewModel(pluginCatalog, runner, profileStore, calibrationService));
    }

    public void Initialize(RecognitionWorkbenchViewModel viewModel)
    {
        ViewModel = viewModel;
        ViewModel.RecognitionEventRaised += (_, result) => RecognitionEventRaised?.Invoke(this, result);
        ViewModel.ErrorOccurred += ViewModel_ErrorOccurred;
        DataContext = ViewModel;
    }

    private RecognitionWorkbenchViewModel RequireViewModel()
    {
        return ViewModel ?? throw new InvalidOperationException("RecognitionWorkbenchControl must be initialized before use.");
    }

    private async void ReloadFactories_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteWithErrorHandlingAsync(() =>
        {
            RequireViewModel().RefreshFactories();
            return Task.CompletedTask;
        }, "Plugin Reload Error");
    }

    private async void LoadProfile_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteWithErrorHandlingAsync(() => RequireViewModel().LoadProfileAsync(), "Profile Load Error");
    }

    private async void ReloadProfileList_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteWithErrorHandlingAsync(() =>
        {
            RequireViewModel().RefreshProfileList();
            return Task.CompletedTask;
        }, "Profile List Error");
    }

    private async void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteWithErrorHandlingAsync(() => RequireViewModel().SaveProfileAsync(), "Profile Save Error");
    }

    private async void CreateBlankProfile_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteWithErrorHandlingAsync(() => RequireViewModel().CreateBlankProfileAsync(), "Profile Create Error");
    }

    private async void AutoCalibrateProfile_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteWithErrorHandlingAsync(() => RequireViewModel().AutoCalibrateAndSaveProfileAsync(), "Profile Calibration Error");
    }

    private async void TestRun_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteWithErrorHandlingAsync(() => RequireViewModel().RunTestAsync(), "Test Run Error");
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteWithErrorHandlingAsync(async () =>
        {
            Console.Error.WriteLine("[Start_Click] Button clicked");
            await RequireViewModel().StartAsync();
            Console.Error.WriteLine("[Start_Click] StartAsync completed");
        }, "Start Error");
    }

    private async void Stop_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteWithErrorHandlingAsync(() => RequireViewModel().StopAsync(), "Stop Error");
    }

    private void RemovePreprocessor_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is ComponentSelectionViewModel<IImageProcessorFactory> preprocessor)
        {
            RequireViewModel().RemovePreprocessor(preprocessor);
        }
    }

    private async void SelectPreprocessorCropArea_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteWithErrorHandlingAsync(async () =>
        {
            if ((sender as FrameworkElement)?.Tag is ComponentSelectionViewModel<IImageProcessorFactory> preprocessor)
            {
                await RequireViewModel().ApplyCropAreaFromLatestCaptureAsync(preprocessor);
            }
        }, "Crop Area Error");
    }

    private async void SelectScreenRegion_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteWithErrorHandlingAsync(() =>
        {
            var selector = new ScreenRegionSelectionWindow();
            if (selector.ShowDialog() == true && selector.SelectedRegion is { } region)
            {
                RequireViewModel().ApplyScreenRegion(region);
            }

            return Task.CompletedTask;
        }, "Screen Region Error");
    }

    private async void CreateTemplateFromRawCapture_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteWithErrorHandlingAsync(() => RequireViewModel().CreateTemplateFromLatestCaptureAsync(), "Template Error");
    }

    private async void CreateTemplateFromProcessedCapture_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteWithErrorHandlingAsync(() => RequireViewModel().CreateTemplateFromLatestCaptureAsync(useProcessedFrame: true), "Template Error");
    }

    private void ProfileNumericTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.Tag is not string propertyName)
        {
            return;
        }

        e.Handled = !RequireViewModel().IsProfileNumericTextValid(propertyName, BuildProposedText(textBox, e.Text));
    }

    private void ProfileNumericTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.Tag is not string propertyName)
        {
            e.CancelCommand();
            return;
        }

        var pastedText = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (!RequireViewModel().IsProfileNumericTextValid(propertyName, BuildProposedText(textBox, pastedText)))
        {
            e.CancelCommand();
        }
    }

    private void IncreaseProfileNumeric_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string propertyName)
        {
            RequireViewModel().StepProfileNumericValue(propertyName, 1);
        }
    }

    private void DecreaseProfileNumeric_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string propertyName)
        {
            RequireViewModel().StepProfileNumericValue(propertyName, -1);
        }
    }

    private void ParameterNumericTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        if ((sender as TextBox)?.Tag is not ParameterEntryViewModel parameter)
        {
            return;
        }

        e.Handled = !parameter.IsValidNumericText(BuildProposedText((TextBox)sender, e.Text));
    }

    private void ParameterNumericTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if ((sender as TextBox)?.Tag is not ParameterEntryViewModel parameter)
        {
            e.CancelCommand();
            return;
        }

        var pastedText = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (!parameter.IsValidNumericText(BuildProposedText((TextBox)sender, pastedText)))
        {
            e.CancelCommand();
        }
    }

    private void IncreaseParameterNumeric_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is ParameterEntryViewModel parameter)
        {
            parameter.StepValue(1);
        }
    }

    private void DecreaseParameterNumeric_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is ParameterEntryViewModel parameter)
        {
            parameter.StepValue(-1);
        }
    }

    private static string BuildProposedText(TextBox textBox, string insertedText)
    {
        var existingText = textBox.Text ?? string.Empty;
        var selectionStart = textBox.SelectionStart;
        var selectionLength = textBox.SelectionLength;
        var before = existingText[..selectionStart];
        var after = existingText[(selectionStart + selectionLength)..];
        return before + insertedText + after;
    }

    private async void SelectOcrRegion_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteWithErrorHandlingAsync(async () =>
        {
            if ((sender as FrameworkElement)?.Tag is OcrTargetViewModel target)
            {
                await RequireViewModel().ApplyOcrTargetRegionFromLatestCaptureAsync(target);
            }
        }, "OCR Region Error");
    }

    private void RemoveOcrTarget_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is OcrTargetViewModel target)
        {
            RequireViewModel().RemoveOcrTarget(target);
        }
    }

    private void RemoveImageRecognitionTarget_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is ImageRecognitionTargetViewModel target)
        {
            RequireViewModel().RemoveImageRecognitionTarget(target);
        }
    }

    private void RemoveOcrReference_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is OcrReferenceViewModel reference)
        {
            RequireViewModel().RemoveOcrReference(reference);
        }
    }

    private void RemoveDistanceMeasurementTarget_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is DistanceMeasurementTargetViewModel target)
        {
            RequireViewModel().RemoveDistanceMeasurementTarget(target);
        }
    }

    private void RemoveDistanceMeasurementReference_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is DistanceMeasurementReferenceViewModel reference)
        {
            RequireViewModel().RemoveDistanceMeasurementReference(reference);
        }
    }

    private void DistanceReferenceThreshold_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        if ((sender as TextBox)?.Tag is not DistanceMeasurementReferenceViewModel reference)
        {
            return;
        }

        e.Handled = !reference.IsValidThresholdText(BuildProposedText((TextBox)sender, e.Text));
    }

    private void DistanceReferenceThreshold_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if ((sender as TextBox)?.Tag is not DistanceMeasurementReferenceViewModel reference)
        {
            e.CancelCommand();
            return;
        }

        var pastedText = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (!reference.IsValidThresholdText(BuildProposedText((TextBox)sender, pastedText)))
        {
            e.CancelCommand();
        }
    }

    private void IncreaseDistanceReferenceThreshold_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is DistanceMeasurementReferenceViewModel reference)
        {
            reference.StepThreshold(1);
        }
    }

    private void DecreaseDistanceReferenceThreshold_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is DistanceMeasurementReferenceViewModel reference)
        {
            reference.StepThreshold(-1);
        }
    }

    private void DistanceTargetThreshold_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        if ((sender as TextBox)?.Tag is not DistanceMeasurementTargetViewModel target)
        {
            return;
        }

        e.Handled = !target.IsValidThresholdText(BuildProposedText((TextBox)sender, e.Text));
    }

    private void DistanceTargetThreshold_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if ((sender as TextBox)?.Tag is not DistanceMeasurementTargetViewModel target)
        {
            e.CancelCommand();
            return;
        }

        var pastedText = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (!target.IsValidThresholdText(BuildProposedText((TextBox)sender, pastedText)))
        {
            e.CancelCommand();
        }
    }

    private void IncreaseDistanceTargetThreshold_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is DistanceMeasurementTargetViewModel target)
        {
            target.StepThreshold(1);
        }
    }

    private void DecreaseDistanceTargetThreshold_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is DistanceMeasurementTargetViewModel target)
        {
            target.StepThreshold(-1);
        }
    }

    private void IncreaseOcrReferenceThreshold_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is OcrReferenceViewModel reference)
        {
            reference.StepThreshold(1);
        }
    }

    private void DecreaseOcrReferenceThreshold_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is OcrReferenceViewModel reference)
        {
            reference.StepThreshold(-1);
        }
    }

    private void OcrReferenceThreshold_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        if ((sender as TextBox)?.Tag is not OcrReferenceViewModel reference)
        {
            return;
        }

        e.Handled = !reference.IsValidThresholdText(BuildProposedText((TextBox)sender, e.Text));
    }

    private void OcrReferenceThreshold_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if ((sender as TextBox)?.Tag is not OcrReferenceViewModel reference)
        {
            e.CancelCommand();
            return;
        }

        var pastedText = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (!reference.IsValidThresholdText(BuildProposedText((TextBox)sender, pastedText)))
        {
            e.CancelCommand();
        }
    }

    private void RemoveOcrTargetPreprocessor_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ComponentSelectionViewModel<IImageProcessorFactory> preprocessor
            && FindAncestorDataContext<OcrTargetViewModel>(sender as DependencyObject) is { } target)
        {
            target.RemovePreprocessor(preprocessor);
        }
    }

    private void RemoveImageRecognitionPreprocessor_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ComponentSelectionViewModel<IImageProcessorFactory> preprocessor
            && FindAncestorDataContext<ImageRecognitionTargetViewModel>(sender as DependencyObject) is { } target)
        {
            target.RemovePreprocessor(preprocessor);
        }
    }

    private void MoveItemUp_Click(object sender, RoutedEventArgs e)
    {
        MoveBoundItem(sender as DependencyObject, -1);
    }

    private void MoveItemDown_Click(object sender, RoutedEventArgs e)
    {
        MoveBoundItem(sender as DependencyObject, 1);
    }

    private void PreviewImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        if (ViewModel is null || !ViewModel.TryGetPreviewFrameSize(out var frameSize))
        {
            return;
        }

        var pointer = e.GetPosition(element);
        if (!TryMapToPreviewFrame(pointer, element.RenderSize, frameSize, out var framePoint))
        {
            ViewModel.UpdatePreviewCursor(null);
            return;
        }

        ViewModel.UpdatePreviewCursor(framePoint);
    }

    private void PreviewImage_MouseLeave(object sender, MouseEventArgs e)
    {
        ViewModel?.UpdatePreviewCursor(null);
    }

    private static T? FindAncestorDataContext<T>(DependencyObject? start) where T : class
    {
        var current = start;
        while (current is not null)
        {
            if (current is FrameworkElement element && element.DataContext is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static void MoveBoundItem(DependencyObject? start, int direction)
    {
        if (start is null)
        {
            return;
        }

        var container = FindAncestor<ContentPresenter>(start);
        var itemsControl = FindAncestor<ItemsControl>(start);
        var moveMethod = itemsControl?.ItemsSource?.GetType().GetMethod("Move", [typeof(int), typeof(int)]);
        if (container?.DataContext is null
            || itemsControl?.ItemsSource is not System.Collections.IList items
            || moveMethod is null)
        {
            return;
        }

        var currentIndex = items.IndexOf(container.DataContext);
        var targetIndex = currentIndex + direction;
        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= items.Count)
        {
            return;
        }

        moveMethod.Invoke(itemsControl.ItemsSource, [currentIndex, targetIndex]);
    }

    private static T? FindAncestor<T>(DependencyObject? start) where T : DependencyObject
    {
        var current = start;
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static bool TryMapToPreviewFrame(Point pointer, Size renderSize, Size frameSize, out Point framePoint)
    {
        framePoint = default;
        if (renderSize.Width <= 0
            || renderSize.Height <= 0
            || frameSize.Width <= 0
            || frameSize.Height <= 0)
        {
            return false;
        }

        var scale = Math.Min(renderSize.Width / frameSize.Width, renderSize.Height / frameSize.Height);
        var imageWidth = frameSize.Width * scale;
        var imageHeight = frameSize.Height * scale;
        var offsetX = (renderSize.Width - imageWidth) / 2d;
        var offsetY = (renderSize.Height - imageHeight) / 2d;
        if (pointer.X < offsetX
            || pointer.Y < offsetY
            || pointer.X > offsetX + imageWidth
            || pointer.Y > offsetY + imageHeight)
        {
            return false;
        }

        var frameX = Math.Clamp((pointer.X - offsetX) / scale, 0d, Math.Max(0d, frameSize.Width - 1d));
        var frameY = Math.Clamp((pointer.Y - offsetY) / scale, 0d, Math.Max(0d, frameSize.Height - 1d));
        framePoint = new Point(frameX, frameY);
        return true;
    }

    private async void ViewModel_ErrorOccurred(object? sender, Exception exception)
    {
        await ShowErrorAsync("Recognition Error", exception, stopRecognition: false);
    }

    private async Task ExecuteWithErrorHandlingAsync(Func<Task> action, string title)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(title, exception, stopRecognition: true);
        }
    }

    private async Task ShowErrorAsync(string title, Exception exception, bool stopRecognition)
    {
        Console.Error.WriteLine($"[{title}] {exception}");
        if (stopRecognition && ViewModel is not null)
        {
            await ViewModel.HandleUserVisibleErrorAsync(title, exception);
        }

        MessageBox.Show(
            ViewModel?.GetUserFacingErrorMessage() ?? "エラーが発生しました。ログを開発者へ共有してください。",
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
