using System.Reflection;
using Recognition.Core;
using Recognition.Wpf;
using Xunit;

namespace Recognition.Tests;

public sealed class RecognitionWorkbenchViewModelTests
{
    [Fact]
    public void LogAndHistorySelectionsSynchronizeByCycleId()
    {
        var viewModel = CreateViewModel();
        var timestamp = DateTimeOffset.UtcNow;
        var first = Result(timestamp, eventTriggered: true);
        var second = Result(timestamp, eventTriggered: true);

        Apply(viewModel, first);
        Apply(viewModel, second);

        viewModel.SelectedCycleLogEntry = viewModel.CycleLog.Single(entry => entry.CycleId == first.CycleId);
        Assert.Equal(0, viewModel.SelectedHistoryIndex);
        Assert.Equal(first.CycleId, viewModel.SelectedEventLogEntry?.CycleId);

        viewModel.SelectedHistoryIndex = 1;
        Assert.Equal(second.CycleId, viewModel.SelectedCycleLogEntry?.CycleId);
        Assert.Equal(second.CycleId, viewModel.SelectedEventLogEntry?.CycleId);
    }

    [Fact]
    public void ExpiredLogSelectionDoesNotSelectAnotherFrame()
    {
        var viewModel = CreateViewModel();
        viewModel.FrameHistoryRetentionSecondsText = "0";
        var first = Result(DateTimeOffset.UtcNow, eventTriggered: true);
        var second = Result(first.Timestamp.AddTicks(1), eventTriggered: false);

        Apply(viewModel, first);
        Apply(viewModel, second);
        viewModel.SelectedEventLogEntry = Assert.Single(viewModel.EventLog);

        Assert.Equal(-1, viewModel.SelectedHistoryIndex);
        Assert.Equal("対応する履歴フレームは保持期限切れです。", viewModel.StatusMessage);
        Assert.Null(viewModel.PreviewImage);
    }

    [Fact]
    public void NewResultsDoNotOverridePastHistorySelection()
    {
        var viewModel = CreateViewModel();
        var timestamp = DateTimeOffset.UtcNow;
        var first = Result(timestamp, eventTriggered: false);
        Apply(viewModel, first);
        Apply(viewModel, Result(timestamp.AddSeconds(1), eventTriggered: false));
        viewModel.SelectedHistoryIndex = 0;

        Apply(viewModel, Result(timestamp.AddSeconds(2), eventTriggered: false));

        Assert.Equal(0, viewModel.SelectedHistoryIndex);
        Assert.Equal(first.CycleId, viewModel.SelectedCycleLogEntry?.CycleId);
    }

    private static RecognitionWorkbenchViewModel CreateViewModel() =>
        new(new EmptyCatalog(), new UnusedRunner(), new UnusedStore(), new UnusedCalibration());

    private static void Apply(RecognitionWorkbenchViewModel viewModel, RecognitionCycleResult result)
    {
        typeof(RecognitionWorkbenchViewModel)
            .GetMethod("ApplyResult", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(viewModel, [result, false, true]);
    }

    private static RecognitionCycleResult Result(DateTimeOffset timestamp, bool eventTriggered)
    {
        var source = new RecognitionFrame([1, 2, 3], 1, 1, 3, FramePixelFormat.Bgr24, timestamp);
        var processed = source.Clone();
        return new RecognitionCycleResult
        {
            Timestamp = timestamp,
            CycleSourceFrame = source,
            CyclePreviewFrame = processed,
            SourceFrame = source,
            PreviewFrame = processed,
            IsSingleShot = true,
            IsDetected = eventTriggered,
            EventTriggered = eventTriggered,
            DetectionConfidence = eventTriggered ? 1 : 0,
            FramesPerSecond = 60,
            Metadata = new Dictionary<string, string>
            {
                ["EventMode"] = RecognitionEventMode.OnDetectedEnter.ToString(),
                ["RecognizerLabel"] = "test"
            }
        };
    }

    private sealed class EmptyCatalog : IRecognitionPluginCatalog
    {
        public IReadOnlyList<IFrameSourceFactory> FrameSourceFactories => [];
        public IReadOnlyList<IImageProcessorFactory> ImageProcessorFactories => [];
        public IReadOnlyList<IRecognitionMethodFactory> RecognitionMethodFactories => [];
        public IReadOnlyList<IOcrEngineFactory> OcrEngineFactories => [];
        public void ReloadPlugins() { }
    }

    private sealed class UnusedRunner : IRecognitionRunner
    {
        public Task<RecognitionCycleResult> ExecuteOnceAsync(RecognitionProfile profile, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<RecognitionCycleResult> ExecuteOnceAsync(RecognitionProfile profile, RecognitionFrame sourceFrame, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IRecognitionSession CreateContinuousSession(RecognitionProfile profile) =>
            throw new NotSupportedException();
    }

    private sealed class UnusedStore : IRecognitionProfileStore
    {
        public Task SaveAsync(RecognitionProfile profile, string filePath, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<RecognitionProfile> LoadAsync(string filePath, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class UnusedCalibration : IRecognitionProfileCalibrationService
    {
        public Task<RecognitionProfileCalibrationResult> CalibrateAsync(
            RecognitionProfile profile,
            RecognitionFrame processedFrame,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
