using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Recognition.Core;
using Recognition.Infrastructure;

namespace Recognition.Wpf;

public sealed class RecognitionWorkbenchViewModel : ObservableObject
{
    private readonly IRecognitionPluginCatalog pluginCatalog;
    private readonly IRecognitionRunner runner;
    private readonly IRecognitionProfileStore profileStore;
    private readonly IRecognitionProfileCalibrationService calibrationService;
    private readonly string workspaceRootDirectory = Path.Combine(Environment.CurrentDirectory, "Profiles");
    private IRecognitionSession? session;
    private RecognitionCycleResult? lastResult;
    private RecognitionFrame? latestSourceFrame;
    private RecognitionFrame? latestProcessedFrame;
    private readonly RecognitionFrameHistoryMirror historyMirror = new();
    private bool retainSourceFramesInHistory;
    private RecognitionFrameHistoryEntry? selectedHistoryEntry;
    private string frameHistoryRetentionSecondsText = "10";
    private int selectedHistoryIndex = -1;
    private bool suppressProfileOperationStateUpdates;
    private string ocrReferencePreviewText = string.Empty;
    private string currentProfileFilePath = string.Empty;
    private string loadedProfileFilePath = string.Empty;
    private string loadedProfileName = string.Empty;
    private string profileName = "Default";
    private string targetFpsText = "60";
    private string captureScaleText = "1.00";
    private string eventActionFrameOffsetText = "0";
    private bool showOcrPreviewLabels = true;
    private bool showDistancePreviewAnnotations = true;
    private bool showDistanceCursorPreview = true;
    private bool ocrEnabled;
    private bool distanceMeasurementEnabled;
    private string statusMessage = string.Empty;
    private string latestDetection = string.Empty;
    private string latestText = string.Empty;
    private ImageSource? previewImage;
    private ImageSource? latestOcrTargetsPreviewImage;
    private ImageSource? latestDistanceMeasurementPreviewImage;
    private bool isRunning;
    private bool isOperationInProgress;
    private bool requiresProfileLoad = true;
    private string operationStatusText = string.Empty;
    private DateTimeOffset? operationStartedAt;
    private bool awaitingFirstRecognitionResult;
    private double lastFps;
    private readonly object pendingResultLock = new();
    private readonly IReadOnlyDictionary<string, IPreviewModule> previewModules;
    private RecognitionCycleResult? pendingUiResult;
    private Point? previewCursorPoint;
    private bool uiUpdateScheduled;
    private LocalizedChoice<UiLanguage>? selectedLanguageOption;
    private PreviewModuleOption? selectedPreviewModeOption;
    private LocalizedChoice<RecognitionEventMode>? selectedEventModeOption;
    private ProfileListEntry? selectedProfileEntry;

    public event EventHandler<RecognitionCycleResult>? RecognitionEventRaised;

    public event EventHandler<Exception>? ErrorOccurred;

    public RecognitionWorkbenchViewModel(
        IRecognitionPluginCatalog pluginCatalog,
        IRecognitionRunner runner,
        IRecognitionProfileStore profileStore,
        IRecognitionProfileCalibrationService calibrationService,
        IEnumerable<IPreviewModule>? previewModules = null)
    {
        this.pluginCatalog = pluginCatalog;
        this.runner = runner;
        this.profileStore = profileStore;
        this.calibrationService = calibrationService;

        Localization = new UiLocalization();
        FrameSource = new ComponentSelectionViewModel<IFrameSourceFactory>(Localization, static factory => factory.Descriptor);
        RecognitionMethod = new ComponentSelectionViewModel<IRecognitionMethodFactory>(Localization, static factory => factory.Descriptor);
        OcrEngine = new ComponentSelectionViewModel<IOcrEngineFactory>(Localization, static factory => factory.Descriptor);

        AddPreprocessorCommand = new DelegateCommand(AddPreprocessor);
        AddImageRecognitionTargetCommand = new DelegateCommand(AddImageRecognitionTarget);
        AddOcrReferenceCommand = new DelegateCommand(AddOcrReference);
        AddOcrTargetCommand = new DelegateCommand(AddOcrTarget);
        AddDistanceMeasurementReferenceCommand = new DelegateCommand(AddDistanceMeasurementReference);
        AddDistanceMeasurementTargetCommand = new DelegateCommand(AddDistanceMeasurementTarget);
        BrowseImagePathCommand = new ParameterizedDelegateCommand<object>(BrowseImagePath);
        CreateRawImagePathCommand = new ParameterizedDelegateCommand<object>(target => _ = CreateImageFromLatestCaptureAsync(target, useProcessedFrame: false));
        CreateProcessedImagePathCommand = new ParameterizedDelegateCommand<object>(target => _ = CreateImageFromLatestCaptureAsync(target, useProcessedFrame: true));
        RecropImageOverwriteCommand = new ParameterizedDelegateCommand<object>(target => _ = RecropImageAsync(target, saveAsNewFile: false));
        RecropImageSaveAsCommand = new ParameterizedDelegateCommand<object>(target => _ = RecropImageAsync(target, saveAsNewFile: true));
        PreviousHistoryFrameCommand = new DelegateCommand(SelectPreviousHistoryFrame, () => SelectedHistoryIndex > 0);
        NextHistoryFrameCommand = new DelegateCommand(SelectNextHistoryFrame, () => SelectedHistoryIndex >= 0 && SelectedHistoryIndex < FrameHistoryEntries.Count - 1);
        var configuredPreviewModules = (previewModules ?? CreateBuiltInPreviewModules()).ToArray();
        this.previewModules = configuredPreviewModules.ToDictionary(module => module.Id, StringComparer.OrdinalIgnoreCase);
        Languages =
        [
            new LocalizedChoice<UiLanguage>(UiLanguage.Japanese, Localization, static (_, value) => value == UiLanguage.Japanese ? "日本語" : "English"),
            new LocalizedChoice<UiLanguage>(UiLanguage.English, Localization, static (_, value) => value == UiLanguage.Japanese ? "日本語" : "English")
        ];
        PreviewModes = [.. configuredPreviewModules.Select(module => new PreviewModuleOption(module, Localization))];
        EventModes =
        [
            new LocalizedChoice<RecognitionEventMode>(RecognitionEventMode.OnDetectedEnter, Localization, static (localization, value) => localization.EventMode(value)),
            new LocalizedChoice<RecognitionEventMode>(RecognitionEventMode.OnDetectedExit, Localization, static (localization, value) => localization.EventMode(value)),
            new LocalizedChoice<RecognitionEventMode>(RecognitionEventMode.WhileDetected, Localization, static (localization, value) => localization.EventMode(value))
        ];
        SelectedLanguageOption = Languages.First(static option => option.Value == UiLanguage.Japanese);
        SelectedPreviewModeOption = PreviewModes.FirstOrDefault(static option => option.Module.LegacyMode == PreviewDisplayMode.Processed) ?? PreviewModes.FirstOrDefault();
        SelectedEventModeOption = EventModes.First(static option => option.Value == RecognitionEventMode.OnDetectedEnter);
        Localization.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == "Item[]")
            {
                RefreshLocalizedState();
            }
        };
        OcrReferences.CollectionChanged += OnOcrReferencesChanged;
        operationTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        operationTimer.Tick += (_, _) => UpdateOperationStatusText();
        Directory.CreateDirectory(workspaceRootDirectory);
        RefreshFactories();
        RefreshProfileList();
        StatusMessage = Localization["Ready"];
        LatestDetection = Localization["Ready"];
    }

    public UiLocalization Localization { get; }

    public ObservableCollection<LocalizedChoice<UiLanguage>> Languages { get; }

    public ObservableCollection<PreviewModuleOption> PreviewModes { get; }

    public ObservableCollection<LocalizedChoice<RecognitionEventMode>> EventModes { get; }

    public ObservableCollection<PreviewModuleSettingViewModel> ActivePreviewSettings { get; } = [];

    public ObservableCollection<ComponentSelectionViewModel<IImageProcessorFactory>> Preprocessors { get; } = [];

    public ObservableCollection<ImageRecognitionTargetViewModel> ImageRecognitionTargets { get; } = [];

    public ObservableCollection<OcrReferenceViewModel> OcrReferences { get; } = [];

    public ObservableCollection<OcrTargetViewModel> OcrTargets { get; } = [];

    public ObservableCollection<DistanceMeasurementReferenceViewModel> DistanceMeasurementReferences { get; } = [];

    public ObservableCollection<DistanceMeasurementTargetViewModel> DistanceMeasurementTargets { get; } = [];

    public ObservableCollection<RecognitionLogEntry> EventLog { get; } = [];

    public ObservableCollection<RecognitionCycleLogEntry> CycleLog { get; } = [];

    public ObservableCollection<RecognitionErrorLogEntry> ErrorLog { get; } = [];

    public ObservableCollection<ProfileListEntry> AvailableProfiles { get; } = [];

    public ComponentSelectionViewModel<IFrameSourceFactory> FrameSource { get; }

    public ComponentSelectionViewModel<IRecognitionMethodFactory> RecognitionMethod { get; }

    public ComponentSelectionViewModel<IOcrEngineFactory> OcrEngine { get; }

    public DelegateCommand AddPreprocessorCommand { get; }

    public DelegateCommand AddImageRecognitionTargetCommand { get; }

    public DelegateCommand AddOcrReferenceCommand { get; }

    public DelegateCommand AddOcrTargetCommand { get; }

    public DelegateCommand AddDistanceMeasurementReferenceCommand { get; }

    public DelegateCommand AddDistanceMeasurementTargetCommand { get; }

    public ParameterizedDelegateCommand<object> BrowseImagePathCommand { get; }

    public ParameterizedDelegateCommand<object> CreateRawImagePathCommand { get; }

    public ParameterizedDelegateCommand<object> CreateProcessedImagePathCommand { get; }

    public ParameterizedDelegateCommand<object> RecropImageOverwriteCommand { get; }

    public ParameterizedDelegateCommand<object> RecropImageSaveAsCommand { get; }

    public DelegateCommand PreviousHistoryFrameCommand { get; }

    public DelegateCommand NextHistoryFrameCommand { get; }

    public ObservableCollection<RecognitionFrameHistoryEntry> FrameHistoryEntries => historyMirror.Entries;

    public int FrameHistoryMaximum => Math.Max(0, FrameHistoryEntries.Count - 1);

    public int SelectedHistoryIndex
    {
        get => selectedHistoryIndex;
        set
        {
            var index = Math.Clamp(value, -1, FrameHistoryMaximum);
            if (SetProperty(ref selectedHistoryIndex, index))
            {
                selectedHistoryEntry = index >= 0 ? FrameHistoryEntries[index] : null;
                RaisePropertyChanged(nameof(SelectedHistoryLabel));
                RefreshPreviewImage();
                RefreshOcrReferencePreview();
                PreviousHistoryFrameCommand.RaiseCanExecuteChanged();
                NextHistoryFrameCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedHistoryLabel => selectedHistoryEntry is null
        ? "-"
        : $"{selectedHistoryEntry.Timestamp.ToLocalTime():HH:mm:ss.fff} ({SelectedHistoryIndex + 1}/{FrameHistoryEntries.Count})";

    public LocalizedChoice<UiLanguage>? SelectedLanguageOption
    {
        get => selectedLanguageOption;
        set
        {
            if (SetProperty(ref selectedLanguageOption, value) && value is not null)
            {
                Localization.CurrentLanguage = value.Value;
            }
        }
    }

    public string ProfileName
    {
        get => profileName;
        set
        {
            if (SetProperty(ref profileName, value))
            {
                UpdateProfileOperationState();
            }
        }
    }

    public ProfileListEntry? SelectedProfileEntry
    {
        get => selectedProfileEntry;
        set
        {
            if (SetProperty(ref selectedProfileEntry, value))
            {
                UpdateProfileOperationState();
            }
        }
    }

    public bool RequiresProfileLoad
    {
        get => requiresProfileLoad;
        private set
        {
            if (SetProperty(ref requiresProfileLoad, value))
            {
                RaisePropertyChanged(nameof(CanEditLoadedProfile));
                RaisePropertyChanged(nameof(CanExecuteProfileActions));
                RaisePropertyChanged(nameof(CanSaveProfile));
                RaisePropertyChanged(nameof(ProfileLoadHint));
            }
        }
    }

    public bool CanEditLoadedProfile => !RequiresProfileLoad;

    public bool CanExecuteProfileActions => !RequiresProfileLoad;

    public bool CanSaveProfile => !IsDifferentProfileSelected();

    public string ProfileLoadHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(loadedProfileFilePath))
            {
                return string.Empty;
            }

            if (IsDifferentProfileSelected())
            {
                return Localization["ProfileLoadRequiredHint"];
            }

            return HasProfileNameChanged()
                ? Localization["ProfileSaveAsHint"]
                : string.Empty;
        }
    }

    public string TargetFpsText
    {
        get => targetFpsText;
        set => SetProperty(ref targetFpsText, NumericInputHelper.Normalize(value));
    }

    public string CaptureScaleText
    {
        get => captureScaleText;
        set => SetProperty(ref captureScaleText, NumericInputHelper.Normalize(value));
    }

    public string FrameHistoryRetentionSecondsText
    {
        get => frameHistoryRetentionSecondsText;
        set => SetProperty(ref frameHistoryRetentionSecondsText, NumericInputHelper.Normalize(value));
    }

    public bool RetainSourceFramesInHistory
    {
        get => retainSourceFramesInHistory;
        set => SetProperty(ref retainSourceFramesInHistory, value);
    }

    public string EventActionFrameOffsetText
    {
        get => eventActionFrameOffsetText;
        set => SetProperty(ref eventActionFrameOffsetText, NumericInputHelper.Normalize(value));
    }

    public PreviewModuleOption? SelectedPreviewModeOption
    {
        get => selectedPreviewModeOption;
        set
        {
            if (SetProperty(ref selectedPreviewModeOption, value))
            {
                previewCursorPoint = null;
                RefreshPreviewSettings();
                RefreshPreviewImage();
            }
        }
    }

    public LocalizedChoice<RecognitionEventMode>? SelectedEventModeOption
    {
        get => selectedEventModeOption;
        set => SetProperty(ref selectedEventModeOption, value);
    }

    public bool OcrEnabled
    {
        get => ocrEnabled;
        set => SetProperty(ref ocrEnabled, value);
    }

    public bool DistanceMeasurementEnabled
    {
        get => distanceMeasurementEnabled;
        set => SetProperty(ref distanceMeasurementEnabled, value);
    }

    public bool IsOperationInProgress
    {
        get => isOperationInProgress;
        private set => SetProperty(ref isOperationInProgress, value);
    }

    public string OperationStatusText
    {
        get => operationStatusText;
        private set => SetProperty(ref operationStatusText, value);
    }

    public bool ShowOcrPreviewLabels
    {
        get => showOcrPreviewLabels;
        set
        {
            if (SetProperty(ref showOcrPreviewLabels, value))
            {
                RefreshPreviewSettings();
                RefreshPreviewImage();
            }
        }
    }

    public bool ShowDistancePreviewAnnotations
    {
        get => showDistancePreviewAnnotations;
        set
        {
            if (SetProperty(ref showDistancePreviewAnnotations, value))
            {
                RefreshPreviewSettings();
                RefreshPreviewImage();
            }
        }
    }

    public bool ShowDistanceCursorPreview
    {
        get => showDistanceCursorPreview;
        set
        {
            if (SetProperty(ref showDistanceCursorPreview, value))
            {
                if (!value)
                {
                    previewCursorPoint = null;
                }

                RefreshPreviewSettings();
                RefreshPreviewImage();
            }
        }
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public string LatestDetection
    {
        get => latestDetection;
        private set => SetProperty(ref latestDetection, value);
    }

    public string LatestText
    {
        get => latestText;
        private set => SetProperty(ref latestText, value);
    }

    public ImageSource? PreviewImage
    {
        get => previewImage;
        private set => SetProperty(ref previewImage, value);
    }

    public string OcrReferencePreviewText
    {
        get => ocrReferencePreviewText;
        private set => SetProperty(ref ocrReferencePreviewText, value);
    }

    public bool IsRunning
    {
        get => isRunning;
        private set => SetProperty(ref isRunning, value);
    }

    public double LastFps
    {
        get => lastFps;
        private set => SetProperty(ref lastFps, value);
    }

    public void RefreshFactories()
    {
        pluginCatalog.ReloadPlugins();

        FrameSource.Refresh(pluginCatalog.FrameSourceFactories);
        RecognitionMethod.Refresh(pluginCatalog.RecognitionMethodFactories);
        OcrEngine.Refresh(pluginCatalog.OcrEngineFactories);
        foreach (var ocrTarget in OcrTargets)
        {
            ocrTarget.RefreshFactories(pluginCatalog.ImageProcessorFactories);
        }
        foreach (var imageTarget in ImageRecognitionTargets)
        {
            imageTarget.RefreshFactories(pluginCatalog.RecognitionMethodFactories, pluginCatalog.ImageProcessorFactories);
        }

        if (Preprocessors.Count > 0)
        {
            foreach (var preprocessor in Preprocessors)
            {
                preprocessor.Refresh(pluginCatalog.ImageProcessorFactories);
            }
        }

        RefreshOcrReferencePreview();
        RefreshPreviewImage();
    }

    private void UpdateProfileOperationState()
    {
        if (suppressProfileOperationStateUpdates)
        {
            return;
        }

        RequiresProfileLoad =
            string.IsNullOrWhiteSpace(loadedProfileFilePath)
            || IsDifferentProfileSelected()
            || HasProfileNameChanged();
        RaisePropertyChanged(nameof(CanSaveProfile));
        RaisePropertyChanged(nameof(ProfileLoadHint));
    }

    private void SetLoadedProfileState(string profileFilePath, string profileName)
    {
        loadedProfileFilePath = profileFilePath;
        loadedProfileName = NormalizeProfileName(profileName);
        UpdateProfileOperationState();
    }

    private bool IsDifferentProfileSelected()
    {
        var selectedPath = SelectedProfileEntry?.FilePath;
        return !string.IsNullOrWhiteSpace(loadedProfileFilePath)
            && !string.IsNullOrWhiteSpace(selectedPath)
            && !string.Equals(selectedPath, loadedProfileFilePath, StringComparison.OrdinalIgnoreCase);
    }

    private bool HasProfileNameChanged()
    {
        return !string.IsNullOrWhiteSpace(loadedProfileFilePath)
            && !string.Equals(NormalizeProfileName(ProfileName), loadedProfileName, StringComparison.Ordinal);
    }

    private void EnsureProfileLoadedForOperations()
    {
        if (CanExecuteProfileActions)
        {
            return;
        }

        throw new InvalidOperationException(Localization["ProfileLoadRequiredBeforeOperations"]);
    }

    private IEnumerable<IPreviewModule> CreateBuiltInPreviewModules()
    {
        return
        [
            new PreviewModule(
                "builtin.preview.captured",
                PreviewDisplayMode.Captured,
                localization => localization["PreviewCaptured"],
                static (viewModel, result) => ToBitmapSource(
                    viewModel.GetDefaultPreviewFrame(result),
                    viewModel.GetDefaultPreviewOverlayRegion(result),
                    result.IsDetected,
                    viewModel.GetDefaultPreviewSearchRegion())),
            new PreviewModule(
                "builtin.preview.processed",
                PreviewDisplayMode.Processed,
                localization => localization["PreviewProcessed"],
                static (viewModel, result) => ToBitmapSource(
                    viewModel.GetDefaultPreviewFrame(result),
                    viewModel.GetDefaultPreviewOverlayRegion(result),
                    result.IsDetected,
                    viewModel.GetDefaultPreviewSearchRegion())),
            new PreviewModule(
                "builtin.preview.ocr-references",
                PreviewDisplayMode.OcrReferences,
                localization => localization["PreviewOcrReferences"],
                static (viewModel, result) => viewModel.GetSelectedSourceFrame() is { } frame
                    ? viewModel.BuildOcrReferencePreviewImage(frame, viewModel.EvaluateOcrReferences(frame))
                    : null,
                [new PreviewSettingDefinition(nameof(ShowOcrPreviewLabels), "ShowOcrPreviewLabels")]),
            new PreviewModule(
                "builtin.preview.ocr-targets",
                PreviewDisplayMode.OcrTargets,
                localization => localization["PreviewOcrTargets"],
                static (viewModel, result) =>
                {
                    if (viewModel.GetSelectedSourceFrame() is not { } frame) return null;
                    if (result.OcrPreviewFrames.Count > 0)
                    {
                        viewModel.latestOcrTargetsPreviewImage = viewModel.BuildOcrTargetPreviewImage(frame, result.OcrPreviewFrames);
                    }

                    return viewModel.latestOcrTargetsPreviewImage;
                },
                [new PreviewSettingDefinition(nameof(ShowOcrPreviewLabels), "ShowOcrPreviewLabels")]),
            new PreviewModule(
                "builtin.preview.distance-measurement",
                PreviewDisplayMode.DistanceMeasurement,
                localization => localization["PreviewDistanceMeasurement"],
                static (viewModel, result) =>
                {
                    if (result.DistanceMeasurementPreviewFrame is not null)
                    {
                        viewModel.latestDistanceMeasurementPreviewImage = viewModel.BuildDistanceMeasurementPreviewImage(result.DistanceMeasurementPreviewFrame, result.DistanceMeasurementPreviewItems);
                    }

                    return viewModel.latestDistanceMeasurementPreviewImage;
                },
                [
                    new PreviewSettingDefinition(nameof(ShowDistancePreviewAnnotations), "ShowDistancePreviewAnnotations"),
                    new PreviewSettingDefinition(nameof(ShowDistanceCursorPreview), "ShowDistanceCursorPreview")
                ])
        ];
    }

    public void RemovePreprocessor(ComponentSelectionViewModel<IImageProcessorFactory> preprocessor)
    {
        Preprocessors.Remove(preprocessor);
    }

    public void RemoveOcrReference(OcrReferenceViewModel reference)
    {
        foreach (var target in OcrTargets.Where(target => string.Equals(target.ReferenceName, reference.Name, StringComparison.OrdinalIgnoreCase)))
        {
            target.ReferenceName = string.Empty;
        }

        OcrReferences.Remove(reference);
    }

    public void RemoveImageRecognitionTarget(ImageRecognitionTargetViewModel target)
    {
        ImageRecognitionTargets.Remove(target);
    }

    public async Task RunTestAsync()
    {
        EnsureProfileLoadedForOperations();
        var restartRecognition = IsRunning;
        if (restartRecognition)
        {
            await StopAsync();
        }

        BeginOperation(Localization["TestRunInProgress"]);
        var result = await runner.ExecuteOnceAsync(BuildRuntimeProfile());
        EndOperation();
        ApplyResult(result, forceEventLog: true);
        if (restartRecognition)
        {
            BeginOperation(Localization["RecognitionResumeInProgress"]);
            await StartAsync();
            return;
        }

        StatusMessage = Localization["TestRunCompleted"];
    }

    public async Task StartAsync()
    {
        EnsureProfileLoadedForOperations();
        if (IsRunning)
        {
            return;
        }

        try
        {
            var profile = BuildRuntimeProfile();
            Console.Error.WriteLine($"[StartAsync] Building profile: {profile.Name}");
            Console.Error.WriteLine($"[StartAsync] FrameSource: {profile.FrameSource.ComponentId}");
            Console.Error.WriteLine($"[StartAsync] Recognizer: {profile.Recognizer.ComponentId}");
            Console.Error.WriteLine($"[StartAsync] TargetFps: {profile.TargetFps}");
            Console.Error.WriteLine($"[StartAsync] CaptureScale: {profile.CaptureScale}");
            Console.Error.WriteLine($"[StartAsync] PreviewMode: {profile.PreviewMode}");
            Console.Error.WriteLine($"[StartAsync] EventAction: {profile.EventAction}");

            session = runner.CreateContinuousSession(profile);
            session.CycleCompleted += OnSessionCycleCompleted;
            session.Failed += OnSessionFailed;
            
            Console.Error.WriteLine("[StartAsync] Session created, calling StartAsync()...");
            awaitingFirstRecognitionResult = true;
            BeginOperation(Localization["RecognitionStartInProgress"]);
            await session.StartAsync();
            IsRunning = true;
            StatusMessage = Localization["RecognitionLoopRunning"];
            Console.Error.WriteLine("[StartAsync] Session started successfully");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[StartAsync] Exception: {ex}");
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (session is null)
        {
            return;
        }

        session.CycleCompleted -= OnSessionCycleCompleted;
        session.Failed -= OnSessionFailed;
        await session.StopAsync();
        await session.DisposeAsync();
        session = null;
        IsRunning = false;
        awaitingFirstRecognitionResult = false;
        EndOperation();
        StatusMessage = Localization["RecognitionLoopStopped"];
    }

    public async Task SaveProfileAsync()
    {
        var defaultProfilePath = GetDefaultProfileFilePath(ProfileName);
        EnsureProfileCanBeSaved(defaultProfilePath);
        currentProfileFilePath = defaultProfilePath;
        var profileToSave = BuildProfileForStorage(currentProfileFilePath);
        await profileStore.SaveAsync(profileToSave, currentProfileFilePath);
        ApplyProfile(profileToSave);
        RefreshProfileList();
        SelectProfileEntry(currentProfileFilePath);
        SetLoadedProfileState(currentProfileFilePath, profileToSave.Name);
        StatusMessage = Localization.Format("ProfileSaved", currentProfileFilePath);
    }

    public async Task CreateBlankProfileAsync()
    {
        var newProfileName = GetNextAvailableProfileName(string.IsNullOrWhiteSpace(ProfileName) ? Localization["NewProfileDefaultName"] : ProfileName);
        var profileFilePath = GetDefaultProfileFilePath(newProfileName);
        var blankProfile = new RecognitionProfile
        {
            Name = newProfileName
        };

        currentProfileFilePath = profileFilePath;
        await profileStore.SaveAsync(blankProfile, profileFilePath);
        ApplyProfile(blankProfile);
        RefreshProfileList();
        SelectProfileEntry(profileFilePath);
        SetLoadedProfileState(profileFilePath, blankProfile.Name);
        StatusMessage = Localization.Format("ProfileCreated", profileFilePath);
    }

    public async Task LoadProfileAsync()
    {
        if (SelectedProfileEntry is null)
        {
            throw new InvalidOperationException(Localization["ProfileSelectionRequired"]);
        }

        currentProfileFilePath = SelectedProfileEntry.FilePath;
        var profile = await profileStore.LoadAsync(currentProfileFilePath);
        ApplyProfile(profile);
        SelectProfileEntry(currentProfileFilePath);
        SetLoadedProfileState(currentProfileFilePath, profile.Name);
        StatusMessage = Localization.Format("ProfileLoaded", currentProfileFilePath);
    }

    public void RefreshOcrReferencePreview()
    {
        var selectedFrame = GetSelectedSourceFrame();
        if (selectedFrame is null)
        {
            OcrReferencePreviewText = GetSourceUnavailableMessage("CropNeedsCapture");
            return;
        }

        if (OcrReferences.Count == 0)
        {
            OcrReferencePreviewText = Localization["OcrReferencePreviewNoReferences"];
            return;
        }

        var lines = new List<string>
        {
            Localization["OcrReferencePreviewModeInfo"]
        };

        foreach (var reference in EvaluateOcrReferences(selectedFrame))
        {
            if (!reference.HasTemplateFile)
            {
                lines.Add($"{reference.Name}: {Localization["OcrReferenceTemplateMissing"]}");
                continue;
            }

            var state = reference.Evaluation?.IsMatched == true
                ? Localization["OcrReferenceMatched"]
                : Localization["OcrReferenceNotMatched"];
            var confidence = reference.Evaluation?.Confidence ?? 0d;
            var region = reference.Evaluation?.Region ?? RoiArea.Empty;
            var regionText = region.IsEmpty ? "-" : $"{region.X}, {region.Y}, {region.Width}x{region.Height}";
            lines.Add($"{reference.Name}: {state}  score={confidence:F3} / threshold={reference.Threshold:F3}  region={regionText}");
        }

        OcrReferencePreviewText = string.Join(Environment.NewLine, lines);
    }

    public async Task AutoCalibrateAndSaveProfileAsync()
    {
        EnsureProfileLoadedForOperations();
        if (!string.Equals(RecognitionMethod.SelectedOption?.Descriptor.Id, "builtin.recognition.template-match", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = Localization["TemplateNeedsRecognizer"];
            return;
        }

        if (GetSelectedProcessedFrame() is null)
        {
            var previewResult = await runner.ExecuteOnceAsync(BuildRuntimeProfile());
            ApplyResult(previewResult, updateEventLog: false);
        }

        var selectedProcessedFrame = GetSelectedProcessedFrame();
        if (selectedProcessedFrame is null)
        {
            StatusMessage = Localization["ProcessedTemplateNeedsCapture"];
            return;
        }

        var runtimeProfile = BuildRuntimeProfile();
        var calibration = await calibrationService.CalibrateAsync(runtimeProfile, selectedProcessedFrame);
        var calibratedProfile = BuildProfile();
        calibratedProfile.Name = $"{ProfileName}-calibrated";

        var sourceProfileDirectory = GetCurrentImageBaseDirectory();
        currentProfileFilePath = GetDefaultProfileFilePath(calibratedProfile.Name);
        var profileDirectory = GetProfileDirectory();
        var calibratedTemplatePath = Path.Combine(profileDirectory, RecognitionProfileStorageConventions.ImagesDirectoryName, "recognition", "template.png");
        SaveRecognitionFrame(calibration.TemplateFrame, calibratedTemplatePath);

        calibratedProfile.Recognizer.Parameters["TemplatePath"] = calibratedTemplatePath;
        calibratedProfile.Recognizer.Parameters["Threshold"] = calibration.SuggestedThreshold.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        CopyProfileImagesIntoWorkspace(calibratedProfile, sourceProfileDirectory, profileDirectory);
        RelativizeImagePaths(calibratedProfile, profileDirectory);

        await profileStore.SaveAsync(calibratedProfile, currentProfileFilePath);
        ApplyProfile(calibratedProfile);
        RefreshProfileList();
        SelectProfileEntry(currentProfileFilePath);
        SetLoadedProfileState(currentProfileFilePath, calibratedProfile.Name);
        StatusMessage = Localization.Format("ProfileCalibrated", currentProfileFilePath);
    }

    public void RefreshProfileList()
    {
        var selectedFilePath = SelectedProfileEntry?.FilePath
            ?? currentProfileFilePath;

        var entries = Directory.EnumerateDirectories(workspaceRootDirectory)
            .Select(directory => new ProfileListEntry(
                Path.GetFileName(directory),
                directory,
                RecognitionProfileStorageConventions.GetProfileFilePath(directory)))
            .Where(entry => File.Exists(entry.FilePath))
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        AvailableProfiles.Clear();
        foreach (var entry in entries)
        {
            AvailableProfiles.Add(entry);
        }

        suppressProfileOperationStateUpdates = true;
        try
        {
            SelectedProfileEntry = entries.FirstOrDefault(entry => string.Equals(entry.FilePath, selectedFilePath, StringComparison.OrdinalIgnoreCase))
                ?? entries.FirstOrDefault(entry => string.Equals(entry.Name, SanitizeFileName(ProfileName), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            suppressProfileOperationStateUpdates = false;
        }

        UpdateProfileOperationState();
        StatusMessage = Localization["ProfileListReloaded"];
    }

    public void ApplyScreenRegion(RoiArea region)
    {
        EnsureProfileLoadedForOperations();
        if (!string.Equals(FrameSource.SelectedOption?.Descriptor.Id, "builtin.screen-region", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        SetParameter(FrameSource, "X", region.X.ToString());
        SetParameter(FrameSource, "Y", region.Y.ToString());
        SetParameter(FrameSource, "Width", region.Width.ToString());
        SetParameter(FrameSource, "Height", region.Height.ToString());
        StatusMessage = Localization.Format("ScreenRegionSelected", region.Width, region.Height, region.X, region.Y);
    }

    public Task CreateTemplateFromLatestCaptureAsync(bool useProcessedFrame)
    {
        EnsureProfileLoadedForOperations();
        var templateParameter = RecognitionMethod.Parameters.FirstOrDefault(entry => string.Equals(entry.Definition.Key, "TemplatePath", StringComparison.OrdinalIgnoreCase));
        if (templateParameter is null)
        {
            StatusMessage = Localization["TemplateNeedsRecognizer"];
            return Task.CompletedTask;
        }

        return CreateImageFromLatestCaptureAsync(templateParameter, useProcessedFrame);
    }

    public Task CreateTemplateFromLatestCaptureAsync()
    {
        return CreateTemplateFromLatestCaptureAsync(useProcessedFrame: false);
    }

    private RecognitionFrame? GetSelectedSourceFrame() => historyMirror.GetSourceFrame(selectedHistoryEntry, latestSourceFrame);

    private string GetSourceUnavailableMessage(string captureRequiredKey) =>
        selectedHistoryEntry is { HasSourceFrame: false }
            ? Localization["SourceHistoryUnavailable"]
            : Localization[captureRequiredKey];

    private RecognitionFrame? GetSelectedProcessedFrame() => selectedHistoryEntry?.ProcessedFrame ?? latestProcessedFrame;

    private void SelectPreviousHistoryFrame()
    {
        if (SelectedHistoryIndex > 0)
        {
            SelectedHistoryIndex--;
        }
    }

    private void SelectNextHistoryFrame()
    {
        if (SelectedHistoryIndex < FrameHistoryMaximum)
        {
            SelectedHistoryIndex++;
        }
    }

    private void BrowseImagePath(object? target)
    {
        EnsureProfileLoadedForOperations();
        if (!TryGetImagePath(target, out _))
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = Localization["ImageFileFilter"],
            InitialDirectory = GetDefaultImageDirectory(target)
        };

        if (dialog.ShowDialog() == true)
        {
            SetImagePath(target, ToDisplayPath(dialog.FileName, GetProfileDirectory()));
        }
    }

    private Task CreateImageFromLatestCaptureAsync(object? target, bool useProcessedFrame)
    {
        EnsureProfileLoadedForOperations();
        var templateFrame = useProcessedFrame ? GetSelectedProcessedFrame() : GetSelectedSourceFrame();
        if (templateFrame is null)
        {
            StatusMessage = useProcessedFrame ? Localization["ProcessedTemplateNeedsCapture"] : GetSourceUnavailableMessage("TemplateNeedsCapture");
            return Task.CompletedTask;
        }

        if (!TryGetImagePath(target, out _))
        {
            return Task.CompletedTask;
        }

        var sourceBitmap = CreateBitmapSource(templateFrame);
        var region = ShowCropSelection(
            sourceBitmap,
            useProcessedFrame ? Localization["ProcessedTemplateCropTitle"] : Localization["RawTemplateCropTitle"],
            useProcessedFrame: useProcessedFrame);
        if (region is not { } selectedRegion)
        {
            return Task.CompletedTask;
        }

        var selectedTemplateFrame = useProcessedFrame ? GetSelectedProcessedFrame() : GetSelectedSourceFrame();
        if (selectedTemplateFrame is null)
        {
            StatusMessage = useProcessedFrame ? Localization["ProcessedTemplateNeedsCapture"] : GetSourceUnavailableMessage("TemplateNeedsCapture");
            return Task.CompletedTask;
        }

        var cropped = CreateCroppedBitmap(CreateBitmapSource(selectedTemplateFrame), selectedRegion);
        var savePath = ResolveImageSavePath(target);
        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
        SaveBitmap(cropped, savePath);

        SetImagePath(target, ToDisplayPath(savePath, GetProfileDirectory()));
        StatusMessage = Localization.Format("TemplateSaved", savePath);
        return Task.CompletedTask;
    }

    public Task ApplyCropAreaFromLatestCaptureAsync(ComponentSelectionViewModel<IImageProcessorFactory> preprocessor)
    {
        EnsureProfileLoadedForOperations();
        var selectedFrame = GetSelectedSourceFrame();
        if (selectedFrame is null)
        {
            StatusMessage = GetSourceUnavailableMessage("CropNeedsCapture");
            return Task.CompletedTask;
        }

        if (!preprocessor.SupportsCaptureAreaSelection)
        {
            StatusMessage = Localization["CropNeedsPreprocessor"];
            return Task.CompletedTask;
        }

        var sourceBitmap = CreateBitmapSource(selectedFrame);
        var region = ShowCropSelection(
            sourceBitmap,
            Localization["CropAreaTitle"],
            GetConfiguredRegion(preprocessor));
        if (region is not { } selectedRegion)
        {
            return Task.CompletedTask;
        }

        SetParameter(preprocessor, "X", selectedRegion.X.ToString());
        SetParameter(preprocessor, "Y", selectedRegion.Y.ToString());
        SetParameter(preprocessor, "Width", selectedRegion.Width.ToString());
        SetParameter(preprocessor, "Height", selectedRegion.Height.ToString());
        StatusMessage = Localization.Format("CropAreaUpdated", selectedRegion.Width, selectedRegion.Height, selectedRegion.X, selectedRegion.Y);
        return Task.CompletedTask;
    }

    public Task ApplyOcrTargetRegionFromLatestCaptureAsync(OcrTargetViewModel target)
    {
        EnsureProfileLoadedForOperations();
        var selectedFrame = GetSelectedSourceFrame();
        if (selectedFrame is null)
        {
            StatusMessage = GetSourceUnavailableMessage("CropNeedsCapture");
            return Task.CompletedTask;
        }

        var anchorRegion = ResolveOcrAnchorRegion(target);
        if (target.UseRecognitionAnchor && anchorRegion is not { IsEmpty: false })
        {
            StatusMessage = Localization["OcrAnchorNeedsDetection"];
            return Task.CompletedTask;
        }

        var sourceBitmap = CreateBitmapSource(selectedFrame);
        var region = ShowCropSelection(
            sourceBitmap,
            Localization["SelectOcrRegion"],
            GetCurrentOcrRegion(target, anchorRegion));
        if (region is not { } selectedRegion)
        {
            return Task.CompletedTask;
        }

        target.ApplyRegion(selectedRegion, anchorRegion, target.UseRecognitionAnchor);
        StatusMessage = Localization.Format("OcrRegionUpdated", selectedRegion.Width, selectedRegion.Height, selectedRegion.X, selectedRegion.Y);
        return Task.CompletedTask;
    }

    private Task RecropImageAsync(object? target, bool saveAsNewFile)
    {
        EnsureProfileLoadedForOperations();
        if (!TryGetImagePath(target, out var configuredPath))
        {
            return Task.CompletedTask;
        }

        var sourcePath = ResolveImagePath(configuredPath, GetProfileDirectory());
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            StatusMessage = Localization["ImageEditNeedsFile"];
            return Task.CompletedTask;
        }

        var sourceBitmap = LoadBitmapSource(sourcePath);
        var region = ShowCropSelection(
            sourceBitmap,
            Localization["TemplateCropTitle"],
            new RoiArea(0, 0, sourceBitmap.PixelWidth, sourceBitmap.PixelHeight),
            includeHistory: false);
        if (region is not { } selectedRegion)
        {
            return Task.CompletedTask;
        }

        var destinationPath = saveAsNewFile
            ? PromptImageSavePath(sourcePath)
            : sourcePath;
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return Task.CompletedTask;
        }

        var cropped = CreateCroppedBitmap(sourceBitmap, selectedRegion);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        SaveBitmap(cropped, destinationPath);
        SetImagePath(target, ToDisplayPath(destinationPath, GetProfileDirectory()));
        StatusMessage = Localization.Format(saveAsNewFile ? "TemplateSavedAs" : "TemplateSaved", destinationPath);
        return Task.CompletedTask;
    }

    public void RemoveOcrTarget(OcrTargetViewModel target)
    {
        OcrTargets.Remove(target);
    }

    public void RemoveDistanceMeasurementTarget(DistanceMeasurementTargetViewModel target)
    {
        DistanceMeasurementTargets.Remove(target);
    }

    public void RemoveDistanceMeasurementReference(DistanceMeasurementReferenceViewModel reference)
    {
        foreach (var target in DistanceMeasurementTargets.Where(target => string.Equals(target.ReferenceName, reference.Name, StringComparison.OrdinalIgnoreCase)))
        {
            target.ReferenceName = string.Empty;
        }

        DistanceMeasurementReferences.Remove(reference);
    }

    private void AddPreprocessor()
    {
        var selection = new ComponentSelectionViewModel<IImageProcessorFactory>(Localization, static factory => factory.Descriptor);
        selection.Refresh(pluginCatalog.ImageProcessorFactories);
        Preprocessors.Add(selection);
    }

    private void AddOcrReference()
    {
        var referenceIndex = OcrReferences.Count + 1;
        OcrReferences.Add(new OcrReferenceViewModel(Localization, $"{Localization["OcrReferenceDefaultName"]} {referenceIndex}"));
    }

    private void AddOcrTarget()
    {
        var targetIndex = OcrTargets.Count + 1;
        OcrTargets.Add(new OcrTargetViewModel(Localization, pluginCatalog.ImageProcessorFactories, $"{Localization["OcrTargetDefaultName"]} {targetIndex}"));
    }

    private void AddDistanceMeasurementTarget()
    {
        var targetIndex = DistanceMeasurementTargets.Count + 1;
        var target = new DistanceMeasurementTargetViewModel(Localization, $"{Localization["DistanceTargetDefaultName"]} {targetIndex}")
        {
            ReferenceName = DistanceMeasurementReferences.FirstOrDefault()?.Name ?? $"{Localization["DistanceReferenceDefaultName"]} 1"
        };
        DistanceMeasurementTargets.Add(target);
    }

    private void AddDistanceMeasurementReference()
    {
        var referenceIndex = DistanceMeasurementReferences.Count + 1;
        DistanceMeasurementReferences.Add(new DistanceMeasurementReferenceViewModel(Localization, $"{Localization["DistanceReferenceDefaultName"]} {referenceIndex}"));
    }

    private void AddImageRecognitionTarget()
    {
        var targetIndex = ImageRecognitionTargets.Count + 1;
        ImageRecognitionTargets.Add(new ImageRecognitionTargetViewModel(
            Localization,
            pluginCatalog.RecognitionMethodFactories,
            pluginCatalog.ImageProcessorFactories,
            $"{Localization["ImageRecognitionTargetDefaultName"]} {targetIndex}"));
    }

    private RecognitionEventAction BuildEventAction()
    {
        return (OcrEnabled, DistanceMeasurementEnabled) switch
        {
            (true, true) => RecognitionEventAction.OcrAndDistance,
            (true, false) => RecognitionEventAction.Ocr,
            (false, true) => RecognitionEventAction.DistanceMeasurement,
            _ => RecognitionEventAction.None
        };
    }

    private RecognitionProfile BuildProfile()
    {
        return new RecognitionProfile
        {
            Name = NormalizeProfileName(ProfileName),
            TargetFps = Math.Max(1, NumericInputHelper.ParseInt32OrDefault(TargetFpsText, 60)),
            CaptureScale = NormalizeCaptureScale(NumericInputHelper.ParseDoubleOrDefault(CaptureScaleText, 1.0d)),
            FrameHistoryRetentionSeconds = Math.Clamp(NumericInputHelper.ParseInt32OrDefault(FrameHistoryRetentionSecondsText, 10), 0, 3600),
            RetainSourceFramesInHistory = RetainSourceFramesInHistory,
            PreviewMode = SelectedPreviewModeOption?.Module.LegacyMode ?? PreviewDisplayMode.Processed,
            PreviewModuleId = SelectedPreviewModeOption?.Module.Id ?? string.Empty,
            ShowOcrPreviewLabels = ShowOcrPreviewLabels,
            ShowDistancePreviewAnnotations = ShowDistancePreviewAnnotations,
            ShowDistanceCursorPreview = ShowDistanceCursorPreview,
            EventMode = SelectedEventModeOption?.Value ?? RecognitionEventMode.OnDetectedEnter,
            EventAction = BuildEventAction(),
            EventActionFrameOffset = Math.Clamp(NumericInputHelper.ParseInt32OrDefault(EventActionFrameOffsetText, 0), -600, 600),
            FrameSource = FrameSource.BuildConfiguration(),
            Preprocessors = [.. Preprocessors.Select(static preprocessor => preprocessor.BuildConfiguration())],
            Recognizer = RecognitionMethod.BuildConfiguration(),
            OcrEnabled = OcrEnabled,
            OcrEngine = OcrEngine.BuildConfiguration(),
            ImageRecognitionTargets = [.. ImageRecognitionTargets.Select(static target => target.BuildConfiguration())],
            OcrReferences = [.. OcrReferences.Select(static reference => reference.BuildConfiguration())],
            OcrTargets = [.. OcrTargets.Select(static target => target.BuildConfiguration())],
            DistanceMeasurement = new DistanceMeasurementConfiguration
            {
                References = [.. DistanceMeasurementReferences.Select(static reference => reference.BuildConfiguration())],
                Targets = [.. DistanceMeasurementTargets.Select(static target => target.BuildConfiguration())]
            }
        };
    }

    private void ApplyProfile(RecognitionProfile profile)
    {
        var displayProfile = profile.Clone();
        RelativizeImagePaths(displayProfile, GetProfileDirectory());
        latestOcrTargetsPreviewImage = null;
        latestDistanceMeasurementPreviewImage = null;
        previewCursorPoint = null;
        suppressProfileOperationStateUpdates = true;
        try
        {
            ProfileName = displayProfile.Name;
            TargetFpsText = displayProfile.TargetFps.ToString(System.Globalization.CultureInfo.InvariantCulture);
            CaptureScaleText = NormalizeCaptureScale(displayProfile.CaptureScale).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            FrameHistoryRetentionSecondsText = Math.Clamp(displayProfile.FrameHistoryRetentionSeconds, 0, 3600).ToString(System.Globalization.CultureInfo.InvariantCulture);
            RetainSourceFramesInHistory = displayProfile.RetainSourceFramesInHistory;
            EventActionFrameOffsetText = Math.Clamp(displayProfile.EventActionFrameOffset, -600, 600).ToString(System.Globalization.CultureInfo.InvariantCulture);
            OcrEnabled = displayProfile.OcrEnabled;
            DistanceMeasurementEnabled = displayProfile.EventAction is RecognitionEventAction.DistanceMeasurement or RecognitionEventAction.OcrAndDistance;
            ShowOcrPreviewLabels = displayProfile.ShowOcrPreviewLabels;
            ShowDistancePreviewAnnotations = displayProfile.ShowDistancePreviewAnnotations;
            ShowDistanceCursorPreview = displayProfile.ShowDistanceCursorPreview;
            SelectedPreviewModeOption = ResolvePreviewModeOption(displayProfile.PreviewModuleId, displayProfile.PreviewMode);
            SelectedEventModeOption = EventModes.FirstOrDefault(option => option.Value == displayProfile.EventMode) ?? EventModes.First();

            FrameSource.ApplyConfiguration(displayProfile.FrameSource);
            RecognitionMethod.ApplyConfiguration(displayProfile.Recognizer);
            OcrEngine.ApplyConfiguration(displayProfile.OcrEngine);

            ImageRecognitionTargets.Clear();
            foreach (var targetConfiguration in displayProfile.ImageRecognitionTargets)
            {
                var target = new ImageRecognitionTargetViewModel(Localization, pluginCatalog.RecognitionMethodFactories, pluginCatalog.ImageProcessorFactories, targetConfiguration.Name);
                target.ApplyConfiguration(targetConfiguration);
                ImageRecognitionTargets.Add(target);
            }

            Preprocessors.Clear();
            foreach (var preprocessorConfiguration in displayProfile.Preprocessors)
            {
                var selection = new ComponentSelectionViewModel<IImageProcessorFactory>(Localization, static factory => factory.Descriptor);
                selection.Refresh(pluginCatalog.ImageProcessorFactories);
                selection.ApplyConfiguration(preprocessorConfiguration);
                Preprocessors.Add(selection);
            }

            OcrReferences.Clear();
            foreach (var referenceConfiguration in displayProfile.OcrReferences)
            {
                var reference = new OcrReferenceViewModel(Localization, referenceConfiguration.Name);
                reference.ApplyConfiguration(referenceConfiguration);
                OcrReferences.Add(reference);
            }
            RefreshOcrReferencePreview();

            OcrTargets.Clear();
            foreach (var targetConfiguration in displayProfile.OcrTargets)
            {
                var target = new OcrTargetViewModel(Localization, pluginCatalog.ImageProcessorFactories, targetConfiguration.Name);
                target.ApplyConfiguration(targetConfiguration, pluginCatalog.ImageProcessorFactories);
                OcrTargets.Add(target);
            }

            DistanceMeasurementReferences.Clear();
            var distanceReferences = displayProfile.DistanceMeasurement.References.Count > 0
                ? displayProfile.DistanceMeasurement.References
                : string.IsNullOrWhiteSpace(displayProfile.DistanceMeasurement.ReferenceTemplatePath)
                    ? []
                    :
                    [
                        new DistanceMeasurementReferenceConfiguration
                        {
                            Name = Localization["DistanceReferenceDefaultName"],
                            TemplatePath = displayProfile.DistanceMeasurement.ReferenceTemplatePath,
                            Threshold = displayProfile.DistanceMeasurement.ReferenceThreshold
                        }
                    ];
            foreach (var referenceConfiguration in distanceReferences)
            {
                var reference = new DistanceMeasurementReferenceViewModel(Localization, referenceConfiguration.Name);
                reference.ApplyConfiguration(referenceConfiguration);
                DistanceMeasurementReferences.Add(reference);
            }

            DistanceMeasurementTargets.Clear();
            var distanceTargets = displayProfile.DistanceMeasurement.Targets.Count > 0
                ? displayProfile.DistanceMeasurement.Targets
                : string.IsNullOrWhiteSpace(displayProfile.DistanceMeasurement.TargetTemplatePath)
                    ? []
                    :
                    [
                        new DistanceMeasurementTargetConfiguration
                        {
                            Name = Localization["DistanceTargetDefaultName"],
                            ReferenceName = distanceReferences.FirstOrDefault()?.Name ?? Localization["DistanceReferenceDefaultName"],
                            TemplatePath = displayProfile.DistanceMeasurement.TargetTemplatePath,
                            Threshold = displayProfile.DistanceMeasurement.TargetThreshold
                        }
                    ];
            foreach (var targetConfiguration in distanceTargets)
            {
                var target = new DistanceMeasurementTargetViewModel(Localization, targetConfiguration.Name);
                target.ApplyConfiguration(targetConfiguration);
                DistanceMeasurementTargets.Add(target);
            }
        }
        finally
        {
            suppressProfileOperationStateUpdates = false;
        }

        historyMirror.Clear();
        selectedHistoryEntry = null;
        SelectedHistoryIndex = -1;
        RaisePropertyChanged(nameof(FrameHistoryMaximum));
        UpdateProfileOperationState();
        RefreshPreviewImage();
    }

    private void ApplyResult(RecognitionCycleResult result, bool forceEventLog = false, bool updateEventLog = true)
    {
        lastResult = result;
        latestSourceFrame = result.SourceFrame;
        latestProcessedFrame = result.PreviewFrame;
        var wasAtLatest = SelectedHistoryIndex < 0 || SelectedHistoryIndex == FrameHistoryEntries.Count - 1;
        var retention = TimeSpan.FromSeconds(Math.Clamp(
            NumericInputHelper.ParseInt32OrDefault(FrameHistoryRetentionSecondsText, 10), 0, 3600));
        var removed = historyMirror.Apply(result, retention, RetainSourceFramesInHistory);
        RaisePropertyChanged(nameof(FrameHistoryMaximum));
        if (FrameHistoryEntries.Count == 0)
        {
            selectedHistoryEntry = null;
            SelectedHistoryIndex = -1;
        }
        else if (wasAtLatest || SelectedHistoryIndex < 0)
        {
            selectedHistoryEntry = FrameHistoryEntries[^1];
            SelectedHistoryIndex = FrameHistoryEntries.Count - 1;
            RaisePropertyChanged(nameof(SelectedHistoryLabel));
        }
        else
        {
            selectedHistoryIndex = Math.Clamp(selectedHistoryIndex - removed, 0, FrameHistoryEntries.Count - 1);
            selectedHistoryEntry = FrameHistoryEntries[selectedHistoryIndex];
            RaisePropertyChanged(nameof(SelectedHistoryIndex));
            RaisePropertyChanged(nameof(SelectedHistoryLabel));
        }
        PreviousHistoryFrameCommand.RaiseCanExecuteChanged();
        NextHistoryFrameCommand.RaiseCanExecuteChanged();
        PreviewImage = BuildPreviewImage(result);
        RefreshOcrReferencePreview();
        LastFps = result.FramesPerSecond;
        var stateText = result.IsDetected ? Localization["Detected"] : Localization["NotDetected"];
        if (result.IsDetected)
        {
            LatestDetection = $"{stateText} ({result.DetectionConfidence:P1})";
            LatestText = result.RecognizedText ?? string.Empty;
        }
        else if (string.IsNullOrEmpty(LatestDetection))
        {
            LatestDetection = $"{stateText} ({result.DetectionConfidence:P1})";
            LatestText = result.RecognizedText ?? string.Empty;
        }

        if (updateEventLog && (forceEventLog || result.EventTriggered))
        {
            EventLog.Insert(0, new RecognitionLogEntry(
                result.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff"),
                Localization.EventMode(ParseEventMode(result.Metadata)),
                result.RecognizedText,
                result.DetectionConfidence));

            while (EventLog.Count > 100)
            {
                EventLog.RemoveAt(EventLog.Count - 1);
            }
        }

        CycleLog.Insert(0, new RecognitionCycleLogEntry(
            result.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff"),
            stateText,
            result.Metadata.TryGetValue("RecognizerLabel", out var recognizerLabel) ? recognizerLabel : string.Empty,
            result.EventTriggered,
            result.RecognizedText,
            result.DetectionConfidence,
            result.FramesPerSecond));

        while (CycleLog.Count > 200)
        {
            CycleLog.RemoveAt(CycleLog.Count - 1);
        }

        if (awaitingFirstRecognitionResult)
        {
            awaitingFirstRecognitionResult = false;
            EndOperation();
        }

        if (result.EventTriggered)
        {
            RecognitionEventRaised?.Invoke(this, result);
        }
    }

    private void RefreshPreviewImage()
    {
        if (lastResult is null)
        {
            return;
        }

        PreviewImage = BuildPreviewImage(lastResult);
    }

    private void OnOcrReferencesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<OcrReferenceViewModel>())
            {
                item.PropertyChanged -= OnOcrReferencePropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<OcrReferenceViewModel>())
            {
                item.PropertyChanged += OnOcrReferencePropertyChanged;
            }
        }

        RefreshOcrReferencePreview();
        RefreshPreviewImage();
    }

    private void OnOcrReferencePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshOcrReferencePreview();
        RefreshPreviewImage();
    }

    private void OnSessionCycleCompleted(object? sender, RecognitionCycleResult e)
    {
        lock (pendingResultLock)
        {
            pendingUiResult = e;
            if (uiUpdateScheduled)
            {
                return;
            }

            uiUpdateScheduled = true;
        }

        _ = Application.Current.Dispatcher.InvokeAsync(ProcessPendingRecognitionResult);
    }

    private void OnSessionFailed(object? sender, Exception exception)
    {
        _ = Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await HandleErrorAsync("Recognition Runtime", exception, notifyUser: true);
        });
    }

    private void RefreshLocalizedState()
    {
        foreach (var previewMode in PreviewModes)
        {
            previewMode.Refresh();
        }

        foreach (var setting in ActivePreviewSettings)
        {
            setting.Refresh();
        }

        RaisePropertyChanged(nameof(ProfileLoadHint));

        if (lastResult is not null)
        {
            ApplyResult(lastResult, updateEventLog: false);
        }

        StatusMessage = IsRunning
            ? Localization["RecognitionLoopRunning"]
            : Localization["Ready"];

        if (lastResult is null)
        {
            LatestDetection = Localization["Ready"];
        }

        RefreshOcrReferencePreview();
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

    private static void SaveRecognitionFrame(RecognitionFrame frame, string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var bitmap = CreateBitmapSource(frame);
        SaveBitmap(bitmap, filePath);
    }

    private static ImageSource? ToBitmapSource(RecognitionFrame? frame, RoiArea? overlayRegion = null, bool isDetected = true, RoiArea? searchRegion = null)
    {
        if (frame is null) return null;
        var bitmap = CreateBitmapSource(frame);
        var hasOverlayRegion = overlayRegion is { IsEmpty: false };
        var hasSearchRegion = searchRegion is { IsEmpty: false };
        if (!hasOverlayRegion && !hasSearchRegion)
        {
            return bitmap;
        }

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(bitmap, new Rect(0, 0, frame.Width, frame.Height));
            if (searchRegion is { IsEmpty: false } searchRoi)
            {
                var searchPen = new Pen(Brushes.Orange, Math.Max(2d, Math.Min(frame.Width, frame.Height) / 260d));
                searchPen.Freeze();
                context.DrawRectangle(null, searchPen, new Rect(searchRoi.X, searchRoi.Y, searchRoi.Width, searchRoi.Height));
            }

            if (overlayRegion is { IsEmpty: false } roi)
            {
                var thickness = Math.Max(2d, Math.Min(frame.Width, frame.Height) / 160d);
                var pen = new Pen(isDetected ? Brushes.Red : Brushes.DodgerBlue, thickness);
                pen.Freeze();
                context.DrawRectangle(null, pen, new Rect(roi.X, roi.Y, roi.Width, roi.Height));
            }
        }
        var rendered = new RenderTargetBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Pbgra32);
        rendered.Render(visual);
        rendered.Freeze();
        return rendered;
    }

    private static void SetParameter<TFactory>(ComponentSelectionViewModel<TFactory> selection, string key, string value)
    {
        var parameter = selection.Parameters.FirstOrDefault(entry => string.Equals(entry.Definition.Key, key, StringComparison.OrdinalIgnoreCase));
        if (parameter is not null)
        {
            parameter.Value = value;
        }
    }

    private RoiArea? ShowCropSelection(BitmapSource sourceBitmap, string title, RoiArea? initialRegion = null,
        bool useProcessedFrame = false, bool includeHistory = true)
    {
        var selector = new ImageCropSelectionWindow(
            sourceBitmap,
            title,
            Localization,
            initialRegion,
            includeHistory ? FrameHistoryEntries : null,
            SelectedHistoryIndex,
            index => SelectedHistoryIndex = index,
            useProcessedFrame,
            entry => historyMirror.GetSourceFrame(entry, latestSourceFrame),
            message => StatusMessage = message)
        {
            Owner = Application.Current.MainWindow
        };

        if (selector.ShowDialog() != true)
        {
            return null;
        }

        if (includeHistory) SelectedHistoryIndex = selector.SelectedHistoryIndex;
        return selector.SelectedRegion;
    }

    private static RoiArea GetConfiguredRegion(ComponentSelectionViewModel<IImageProcessorFactory> preprocessor)
    {
        return new RoiArea(
            GetParameter(preprocessor, "X"),
            GetParameter(preprocessor, "Y"),
            Math.Max(1, GetParameter(preprocessor, "Width")),
            Math.Max(1, GetParameter(preprocessor, "Height")));
    }

    private static bool TryGetImagePath(object? target, out string path)
    {
        switch (target)
        {
            case ParameterEntryViewModel parameter:
                path = parameter.Value;
                return true;
            case OcrReferenceViewModel ocrReference:
                path = ocrReference.TemplatePath;
                return true;
            case DistanceMeasurementReferenceViewModel reference:
                path = reference.TemplatePath;
                return true;
            case DistanceMeasurementTargetViewModel distanceTarget:
                path = distanceTarget.TemplatePath;
                return true;
            default:
                path = string.Empty;
                return false;
        }
    }

    private static void SetImagePath(object? target, string path)
    {
        switch (target)
        {
            case ParameterEntryViewModel parameter:
                parameter.Value = path;
                break;
            case OcrReferenceViewModel ocrReference:
                ocrReference.TemplatePath = path;
                break;
            case DistanceMeasurementReferenceViewModel reference:
                reference.TemplatePath = path;
                break;
            case DistanceMeasurementTargetViewModel distanceTarget:
                distanceTarget.TemplatePath = path;
                break;
        }
    }

    private RoiArea? ResolveOcrAnchorRegion(OcrTargetViewModel target)
    {
        var selectedFrame = GetSelectedSourceFrame();
        if (selectedFrame is null)
        {
            return null;
        }

        if (target.UseRecognitionAnchor && !string.IsNullOrWhiteSpace(target.ReferenceName))
        {
            var reference = EvaluateOcrReferences(selectedFrame) 
                .FirstOrDefault(candidate => string.Equals(candidate.Name, target.ReferenceName, StringComparison.OrdinalIgnoreCase));
            if (reference?.Evaluation is { IsMatched: true } evaluation)
            {
                return evaluation.Region;
            }
        }

        return target.UseRecognitionAnchor ? lastResult?.SourceMatchedRegion : null;
    }

    private static RoiArea GetCurrentOcrRegion(OcrTargetViewModel target, RoiArea? anchorRegion)
    {
        var currentRegion = target.GetRegion();
        if (!target.UseRecognitionAnchor || anchorRegion is not { IsEmpty: false } anchor)
        {
            return new RoiArea(
                currentRegion.X,
                currentRegion.Y,
                Math.Max(1, currentRegion.Width),
                Math.Max(1, currentRegion.Height));
        }

        return new RoiArea(
            anchor.X + currentRegion.X,
            anchor.Y + currentRegion.Y,
            Math.Max(1, currentRegion.Width),
            Math.Max(1, currentRegion.Height));
    }

    private static BitmapSource LoadBitmapSource(string filePath)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapSource CreateCroppedBitmap(BitmapSource sourceBitmap, RoiArea region)
    {
        var cropped = new CroppedBitmap(sourceBitmap, new Int32Rect(region.X, region.Y, region.Width, region.Height));
        cropped.Freeze();
        return cropped;
    }

    private static void SaveBitmap(BitmapSource bitmap, string filePath)
    {
        var encoder = CreateBitmapEncoder(filePath);
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(filePath);
        encoder.Save(stream);
    }

    private static BitmapEncoder CreateBitmapEncoder(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 },
            ".bmp" => new BmpBitmapEncoder(),
            _ => new PngBitmapEncoder()
        };
    }

    private IReadOnlyList<OcrReferencePreviewEntry> EvaluateOcrReferences(RecognitionFrame frame)
    {
        var profileDirectory = GetProfileDirectory();
        return [.. OcrReferences.Select(reference =>
        {
            var resolvedTemplatePath = ResolveImagePath(reference.TemplatePath, profileDirectory);
            var threshold = NumericInputHelper.ParseDoubleOrDefault(reference.ThresholdText, 0.92d);
            var hasTemplateFile = !string.IsNullOrWhiteSpace(resolvedTemplatePath) && File.Exists(resolvedTemplatePath);
            var evaluation = hasTemplateFile
                ? TemplateMatchingHelper.Evaluate(frame, resolvedTemplatePath, threshold)
                : null;
            return new OcrReferencePreviewEntry(reference.Name, resolvedTemplatePath, threshold, hasTemplateFile, evaluation);
        })];
    }

    private RecognitionProfile BuildRuntimeProfile()
    {
        var profile = BuildProfile();
        ResolveImagePaths(profile, GetProfileDirectory());
        return profile;
    }

    private RecognitionProfile BuildProfileForStorage(string profileFilePath)
    {
        var profile = BuildProfile();
        var destinationProfileDirectory = Path.GetDirectoryName(profileFilePath) ?? GetProfileDirectory();
        var sourceProfileDirectory = GetCurrentImageBaseDirectory(destinationProfileDirectory);
        Directory.CreateDirectory(destinationProfileDirectory);
        CopyProfileImagesIntoWorkspace(profile, sourceProfileDirectory, destinationProfileDirectory);
        RelativizeImagePaths(profile, destinationProfileDirectory);
        return profile;
    }

    private void ResolveImagePaths(RecognitionProfile profile, string profileDirectory)
    {
        if (TryGetComponentImageParameter(profile.FrameSource, "ImagePath", out var frameSourceImagePath))
        {
            SetComponentImageParameter(profile.FrameSource, "ImagePath", ResolveImagePath(frameSourceImagePath, profileDirectory));
        }

        if (TryGetRecognitionTemplateParameter(profile, out var templatePath))
        {
            SetRecognitionTemplateParameter(profile, ResolveImagePath(templatePath, profileDirectory));
        }

        foreach (var reference in profile.DistanceMeasurement.References)
        {
            reference.TemplatePath = ResolveImagePath(reference.TemplatePath, profileDirectory);
        }

        foreach (var reference in profile.OcrReferences)
        {
            reference.TemplatePath = ResolveImagePath(reference.TemplatePath, profileDirectory);
        }

        foreach (var target in profile.DistanceMeasurement.Targets)
        {
            target.TemplatePath = ResolveImagePath(target.TemplatePath, profileDirectory);
        }
    }

    private void CopyProfileImagesIntoWorkspace(RecognitionProfile profile, string sourceProfileDirectory, string destinationProfileDirectory)
    {
        if (TryGetComponentImageParameter(profile.FrameSource, "ImagePath", out var frameSourceImagePath))
        {
            SetComponentImageParameter(
                profile.FrameSource,
                "ImagePath",
                CopyImageIntoWorkspace(
                    frameSourceImagePath,
                    Path.Combine(destinationProfileDirectory, RecognitionProfileStorageConventions.ImagesDirectoryName, "frame-source", "source.png"),
                    sourceProfileDirectory));
        }

        if (TryGetRecognitionTemplateParameter(profile, out var templatePath))
        {
            var destination = Path.Combine(destinationProfileDirectory, RecognitionProfileStorageConventions.ImagesDirectoryName, "recognition", "template.png");
            SetRecognitionTemplateParameter(profile, CopyImageIntoWorkspace(templatePath, destination, sourceProfileDirectory));
        }

        foreach (var reference in profile.DistanceMeasurement.References)
        {
            reference.TemplatePath = CopyImageIntoWorkspace(
                reference.TemplatePath,
                Path.Combine(destinationProfileDirectory, RecognitionProfileStorageConventions.ImagesDirectoryName, "distance", "references", $"{SanitizeFileName(reference.Name)}.png"),
                sourceProfileDirectory);
        }

        foreach (var reference in profile.OcrReferences)
        {
            reference.TemplatePath = CopyImageIntoWorkspace(
                reference.TemplatePath,
                Path.Combine(destinationProfileDirectory, RecognitionProfileStorageConventions.ImagesDirectoryName, "ocr", "references", $"{SanitizeFileName(reference.Name)}.png"),
                sourceProfileDirectory);
        }

        foreach (var target in profile.DistanceMeasurement.Targets)
        {
            target.TemplatePath = CopyImageIntoWorkspace(
                target.TemplatePath,
                Path.Combine(destinationProfileDirectory, RecognitionProfileStorageConventions.ImagesDirectoryName, "distance", "targets", $"{SanitizeFileName(target.Name)}.png"),
                sourceProfileDirectory);
        }
    }

    private void RelativizeImagePaths(RecognitionProfile profile, string profileDirectory)
    {
        if (TryGetComponentImageParameter(profile.FrameSource, "ImagePath", out var frameSourceImagePath))
        {
            SetComponentImageParameter(profile.FrameSource, "ImagePath", ToDisplayPath(frameSourceImagePath, profileDirectory));
        }

        if (TryGetRecognitionTemplateParameter(profile, out var templatePath))
        {
            SetRecognitionTemplateParameter(profile, ToDisplayPath(templatePath, profileDirectory));
        }

        foreach (var reference in profile.DistanceMeasurement.References)
        {
            reference.TemplatePath = ToDisplayPath(reference.TemplatePath, profileDirectory);
        }

        foreach (var reference in profile.OcrReferences)
        {
            reference.TemplatePath = ToDisplayPath(reference.TemplatePath, profileDirectory);
        }

        foreach (var target in profile.DistanceMeasurement.Targets)
        {
            target.TemplatePath = ToDisplayPath(target.TemplatePath, profileDirectory);
        }
    }

    private static bool TryGetRecognitionTemplateParameter(RecognitionProfile profile, out string templatePath)
    {
        if (TryGetComponentImageParameter(profile.Recognizer, "TemplatePath", out var existingTemplatePath))
        {
            templatePath = existingTemplatePath;
            return true;
        }

        templatePath = string.Empty;
        return false;
    }

    private static void SetRecognitionTemplateParameter(RecognitionProfile profile, string templatePath)
    {
        SetComponentImageParameter(profile.Recognizer, "TemplatePath", templatePath);
    }

    private string CopyImageIntoWorkspace(string path, string destinationPath, string sourceProfileDirectory)
    {
        var resolvedPath = ResolveImagePath(path, sourceProfileDirectory);
        if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
        {
            return path;
        }

        destinationPath = NormalizeDestinationImagePath(destinationPath, resolvedPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        if (!string.Equals(Path.GetFullPath(resolvedPath), Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(resolvedPath, destinationPath, overwrite: true);
        }

        return destinationPath;
    }

    private string GetCurrentImageBaseDirectory(string? fallbackProfileDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(loadedProfileFilePath))
        {
            return Path.GetDirectoryName(loadedProfileFilePath)!;
        }

        if (!string.IsNullOrWhiteSpace(currentProfileFilePath))
        {
            return Path.GetDirectoryName(currentProfileFilePath)!;
        }

        return fallbackProfileDirectory ?? GetProfileDirectory();
    }

    private void EnsureProfileCanBeSaved(string destinationProfilePath)
    {
        if (IsDifferentProfileSelected())
        {
            throw new InvalidOperationException(Localization["ProfileLoadRequiredBeforeOperations"]);
        }

        if (File.Exists(destinationProfilePath)
            && !string.Equals(destinationProfilePath, loadedProfileFilePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(Localization.Format("ProfileNameAlreadyExists", destinationProfilePath));
        }
    }

    private string GetProfileDirectory()
    {
        if (!string.IsNullOrWhiteSpace(currentProfileFilePath))
        {
            return Path.GetDirectoryName(currentProfileFilePath)!;
        }

        var directory = Path.Combine(workspaceRootDirectory, SanitizeFileName(ProfileName));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private string GetDefaultProfileFilePath(string name)
    {
        return RecognitionProfileStorageConventions.GetProfileFilePath(GetDefaultProfileDirectory(name));
    }

    private string GetDefaultProfileDirectory(string name)
    {
        return Path.Combine(workspaceRootDirectory, SanitizeFileName(NormalizeProfileName(name)));
    }

    private string GetNextAvailableProfileName(string preferredName)
    {
        var normalizedBaseName = NormalizeProfileName(preferredName);
        if (!File.Exists(GetDefaultProfileFilePath(normalizedBaseName)))
        {
            return normalizedBaseName;
        }

        for (var index = 2; ; index++)
        {
            var candidate = $"{normalizedBaseName} {index}";
            if (!File.Exists(GetDefaultProfileFilePath(candidate)))
            {
                return candidate;
            }
        }
    }

    private string GetDefaultImageDirectory(object? target)
    {
        var directory = target switch
        {
            ParameterEntryViewModel parameter when string.Equals(parameter.Definition.Key, "ImagePath", StringComparison.OrdinalIgnoreCase) => Path.Combine(GetProfileDirectory(), RecognitionProfileStorageConventions.ImagesDirectoryName, "frame-source"),
            ParameterEntryViewModel => Path.Combine(GetProfileDirectory(), RecognitionProfileStorageConventions.ImagesDirectoryName, "recognition"),
            OcrReferenceViewModel => Path.Combine(GetProfileDirectory(), RecognitionProfileStorageConventions.ImagesDirectoryName, "ocr", "references"),
            DistanceMeasurementReferenceViewModel => Path.Combine(GetProfileDirectory(), RecognitionProfileStorageConventions.ImagesDirectoryName, "distance", "references"),
            DistanceMeasurementTargetViewModel => Path.Combine(GetProfileDirectory(), RecognitionProfileStorageConventions.ImagesDirectoryName, "distance", "targets"),
            _ => Path.Combine(GetProfileDirectory(), RecognitionProfileStorageConventions.ImagesDirectoryName)
        };
        Directory.CreateDirectory(directory);
        return directory;
    }

    private string GetDefaultImageFileName(object? target)
    {
        return target switch
        {
            ParameterEntryViewModel parameter when string.Equals(parameter.Definition.Key, "ImagePath", StringComparison.OrdinalIgnoreCase) => "source.png",
            OcrReferenceViewModel ocrReference => $"{SanitizeFileName(ocrReference.Name)}.png",
            DistanceMeasurementReferenceViewModel reference => $"{SanitizeFileName(reference.Name)}.png",
            DistanceMeasurementTargetViewModel distanceTarget => $"{SanitizeFileName(distanceTarget.Name)}.png",
            _ => Localization["TemplateDialogFileName"]
        };
    }

    private string ResolveImageSavePath(object? target)
    {
        if (TryGetImagePath(target, out var existingPath))
        {
            var resolvedExistingPath = ResolveImagePath(existingPath, GetProfileDirectory());
            if (!string.IsNullOrWhiteSpace(resolvedExistingPath)
                && resolvedExistingPath.StartsWith(Path.GetFullPath(GetProfileDirectory()), StringComparison.OrdinalIgnoreCase))
            {
                return resolvedExistingPath;
            }
        }

        return Path.Combine(GetDefaultImageDirectory(target), GetDefaultImageFileName(target));
    }

    private string? PromptImageSavePath(string currentFilePath)
    {
        var dialog = new SaveFileDialog
        {
            Filter = Localization["ImageFileFilter"],
            InitialDirectory = Path.GetDirectoryName(currentFilePath),
            FileName = Path.GetFileName(currentFilePath),
            AddExtension = true,
            DefaultExt = Path.GetExtension(currentFilePath)
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }

    private static string NormalizeProfileName(string profileName)
    {
        return string.IsNullOrWhiteSpace(profileName) ? "Default" : profileName.Trim();
    }

    private static void ValidateProfileFilePath(string filePath)
    {
        if (!RecognitionProfileStorageConventions.HasProfileFileName(filePath))
        {
            throw new InvalidOperationException($"Profiles must be loaded from '{RecognitionProfileStorageConventions.ProfileFileName}' inside a dedicated profile folder.");
        }
    }

    private void SelectProfileEntry(string profileFilePath)
    {
        suppressProfileOperationStateUpdates = true;
        try
        {
            SelectedProfileEntry = AvailableProfiles.FirstOrDefault(entry => string.Equals(entry.FilePath, profileFilePath, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            suppressProfileOperationStateUpdates = false;
        }

        UpdateProfileOperationState();
    }

    private void ProcessPendingRecognitionResult()
    {
        while (true)
        {
            RecognitionCycleResult? resultToApply;
            lock (pendingResultLock)
            {
                resultToApply = pendingUiResult;
                pendingUiResult = null;
                if (resultToApply is null)
                {
                    uiUpdateScheduled = false;
                    return;
                }
            }

            ApplyResult(resultToApply);

            lock (pendingResultLock)
            {
                if (pendingUiResult is null)
                {
                    uiUpdateScheduled = false;
                    return;
                }
            }
        }
    }

    public Task HandleUserVisibleErrorAsync(string source, Exception exception)
    {
        return HandleErrorAsync(source, exception, notifyUser: false);
    }

    public string GetUserFacingErrorMessage()
    {
        return $"{Localization["UserFacingErrorMessage"]}{Environment.NewLine}{Localization["ShareErrorLogPrompt"]}";
    }

    private async Task HandleErrorAsync(string source, Exception exception, bool notifyUser)
    {
        Console.Error.WriteLine(exception);
        RecordError(source, exception);

        lock (pendingResultLock)
        {
            pendingUiResult = null;
            uiUpdateScheduled = false;
        }

        if (session is not null)
        {
            try
            {
                await StopAsync();
            }
            catch (Exception stopException)
            {
                Console.Error.WriteLine(stopException);
                session = null;
                IsRunning = false;
            }
        }

        awaitingFirstRecognitionResult = false;
        EndOperation();
        StatusMessage = Localization["ErrorOccurredStatus"];
        if (notifyUser)
        {
            ErrorOccurred?.Invoke(this, exception);
        }
    }

    private void RecordError(string source, Exception exception)
    {
        var details = exception.ToString();
        ErrorLog.Insert(0, new RecognitionErrorLogEntry(
            DateTimeOffset.Now.ToString("HH:mm:ss.fff"),
            source,
            $"{exception.GetType().Name}: {exception.Message}",
            details));

        while (ErrorLog.Count > 200)
        {
            ErrorLog.RemoveAt(ErrorLog.Count - 1);
        }
    }

    private readonly DispatcherTimer operationTimer;

    private void BeginOperation(string operationText)
    {
        operationStartedAt = DateTimeOffset.Now;
        IsOperationInProgress = true;
        OperationStatusText = operationText;
        UpdateOperationStatusText();
        operationTimer.Start();
    }

    private void EndOperation()
    {
        operationTimer.Stop();
        operationStartedAt = null;
        IsOperationInProgress = false;
        OperationStatusText = string.Empty;
    }

    private void UpdateOperationStatusText()
    {
        if (!IsOperationInProgress || operationStartedAt is null)
        {
            return;
        }

        var baseText = OperationStatusText.Split(" (", 2, StringSplitOptions.None)[0];
        var elapsed = DateTimeOffset.Now - operationStartedAt.Value;
        OperationStatusText = $"{baseText} ({Localization["OperationElapsed"]}: {elapsed:mm\\:ss})";
    }

    private static bool TryGetComponentImageParameter(ComponentConfiguration component, string parameterKey, out string imagePath)
    {
        if (component.Parameters.TryGetValue(parameterKey, out var existingImagePath))
        {
            imagePath = existingImagePath;
            return true;
        }

        imagePath = string.Empty;
        return false;
    }

    private static void SetComponentImageParameter(ComponentConfiguration component, string parameterKey, string imagePath)
    {
        if (component.Parameters.ContainsKey(parameterKey))
        {
            component.Parameters[parameterKey] = imagePath;
        }
    }

    private static string NormalizeDestinationImagePath(string destinationPath, string sourcePath)
    {
        var sourceExtension = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(sourceExtension))
        {
            return destinationPath;
        }

        return Path.ChangeExtension(destinationPath, sourceExtension);
    }

    private static string ResolveImagePath(string path, string profileDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(profileDirectory, path));
    }

    private static string ToDisplayPath(string path, string profileDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var fullPath = Path.GetFullPath(path);
        var fullDirectory = Path.GetFullPath(profileDirectory);
        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase)
            ? Path.GetRelativePath(fullDirectory, fullPath)
            : path;
    }

    private static string SanitizeFileName(string value)
    {
        var safe = string.Concat((value ?? string.Empty).Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch)).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "Default" : safe;
    }

    private static RecognitionEventMode ParseEventMode(IReadOnlyDictionary<string, string> metadata)
    {
        return metadata.TryGetValue("EventMode", out var eventModeText)
            && Enum.TryParse<RecognitionEventMode>(eventModeText, out var parsed)
                ? parsed
                : RecognitionEventMode.OnDetectedEnter;
    }

    public void UpdatePreviewCursor(Point? point)
    {
        var normalizedPoint = point is { } cursor
            ? new Point(Math.Round(cursor.X), Math.Round(cursor.Y))
            : (Point?)null;
        if (previewCursorPoint == normalizedPoint)
        {
            return;
        }

        previewCursorPoint = normalizedPoint;
        if (GetActivePreviewModule()?.Module.LegacyMode == PreviewDisplayMode.DistanceMeasurement)
        {
            RefreshPreviewImage();
        }
    }

    public bool TryGetPreviewFrameSize(out Size size)
    {
        if (lastResult is null || TryGetCurrentPreviewFrame(lastResult) is not { } frame)
        {
            size = default;
            return false;
        }

        size = new Size(frame.Width, frame.Height);
        return true;
    }

    private PreviewModuleOption ResolvePreviewModeOption(string? previewModuleId, PreviewDisplayMode legacyMode)
    {
        if (!string.IsNullOrWhiteSpace(previewModuleId)
            && PreviewModes.FirstOrDefault(option => string.Equals(option.Module.Id, previewModuleId, StringComparison.OrdinalIgnoreCase)) is { } explicitOption)
        {
            return explicitOption;
        }

        return PreviewModes.FirstOrDefault(option => option.Module.LegacyMode == legacyMode)
            ?? PreviewModes.First();
    }

    private PreviewModuleOption? GetActivePreviewModule()
    {
        return selectedPreviewModeOption ?? PreviewModes.FirstOrDefault();
    }

    private void RefreshPreviewSettings()
    {
        ActivePreviewSettings.Clear();
        foreach (var definition in GetActivePreviewModule()?.Module.Settings ?? [])
        {
            ActivePreviewSettings.Add(new PreviewModuleSettingViewModel(
                Localization,
                definition.LabelKey,
                () => GetPreviewSettingValue(definition.Key),
                value => SetPreviewSettingValue(definition.Key, value)));
        }
    }

    private bool GetPreviewSettingValue(string key)
    {
        return key switch
        {
            nameof(ShowOcrPreviewLabels) => ShowOcrPreviewLabels,
            nameof(ShowDistancePreviewAnnotations) => ShowDistancePreviewAnnotations,
            nameof(ShowDistanceCursorPreview) => ShowDistanceCursorPreview,
            _ => false
        };
    }

    private void SetPreviewSettingValue(string key, bool value)
    {
        switch (key)
        {
            case nameof(ShowOcrPreviewLabels):
                ShowOcrPreviewLabels = value;
                break;
            case nameof(ShowDistancePreviewAnnotations):
                ShowDistancePreviewAnnotations = value;
                break;
            case nameof(ShowDistanceCursorPreview):
                ShowDistanceCursorPreview = value;
                break;
        }
    }

    private RecognitionFrame? GetDefaultPreviewFrame(RecognitionCycleResult result)
    {
        return (GetActivePreviewModule()?.Module.LegacyMode ?? PreviewDisplayMode.Processed) == PreviewDisplayMode.Captured
            ? GetSelectedSourceFrame()
            : GetSelectedProcessedFrame() ?? result.PreviewFrame;
    }

    private RoiArea? GetDefaultPreviewOverlayRegion(RecognitionCycleResult result)
    {
        return (GetActivePreviewModule()?.Module.LegacyMode ?? PreviewDisplayMode.Processed) == PreviewDisplayMode.Captured
            ? result.SourceMatchedRegion
            : result.MatchedRegion;
    }

    private RoiArea? GetDefaultPreviewSearchRegion()
    {
        var region = GetRecognitionSearchRegion();
        if (region is not { IsEmpty: false } searchRegion)
        {
            return null;
        }

        if ((GetActivePreviewModule()?.Module.LegacyMode ?? PreviewDisplayMode.Processed) == PreviewDisplayMode.Captured)
        {
            var (offsetX, offsetY) = GetCapturePreviewOffsets();
            return new RoiArea(searchRegion.X + offsetX, searchRegion.Y + offsetY, searchRegion.Width, searchRegion.Height);
        }

        return searchRegion;
    }

    private RecognitionFrame? TryGetCurrentPreviewFrame(RecognitionCycleResult result)
    {
        var activeModule = GetActivePreviewModule()?.Module;
        if (activeModule is null)
        {
            return GetDefaultPreviewFrame(result);
        }

        return activeModule.LegacyMode switch
        {
            PreviewDisplayMode.Captured => GetSelectedSourceFrame(),
            PreviewDisplayMode.Processed => GetSelectedProcessedFrame() ?? result.PreviewFrame,
            PreviewDisplayMode.OcrReferences => GetSelectedSourceFrame(),
            PreviewDisplayMode.OcrTargets => GetSelectedSourceFrame(),
            PreviewDisplayMode.DistanceMeasurement => result.DistanceMeasurementPreviewFrame,
            _ => result.PreviewFrame
        };
    }

    private ImageSource? BuildPreviewImage(RecognitionCycleResult result)
    {
        var mode = GetActivePreviewModule()?.Module.LegacyMode ?? PreviewDisplayMode.Processed;
        if (mode is PreviewDisplayMode.Captured or PreviewDisplayMode.OcrReferences or PreviewDisplayMode.OcrTargets
            && GetSelectedSourceFrame() is null)
        {
            StatusMessage = GetSourceUnavailableMessage("CropNeedsCapture");
            return null;
        }
        if (GetActivePreviewModule()?.Module is not { } module)
        {
            return ToBitmapSource(GetDefaultPreviewFrame(result), GetDefaultPreviewOverlayRegion(result), result.IsDetected, GetDefaultPreviewSearchRegion());
        }

        return module.BuildPreview(this, result);
    }

    private RoiArea? GetRecognitionSearchRegion()
    {
        var x = GetRecognizerParameter("SearchX");
        var y = GetRecognizerParameter("SearchY");
        var width = GetRecognizerParameter("SearchWidth");
        var height = GetRecognizerParameter("SearchHeight");
        return width > 0 && height > 0
            ? new RoiArea(x, y, width, height)
            : null;
    }

    private int GetRecognizerParameter(string key)
    {
        var parameter = RecognitionMethod.Parameters.FirstOrDefault(entry => string.Equals(entry.Definition.Key, key, StringComparison.OrdinalIgnoreCase));
        return parameter is null ? 0 : NumericInputHelper.ParseInt32OrDefault(parameter.Value, 0);
    }

    private (int OffsetX, int OffsetY) GetCapturePreviewOffsets()
    {
        var offsetX = 0;
        var offsetY = 0;
        foreach (var preprocessor in Preprocessors)
        {
            if (!string.Equals(preprocessor.SelectedOption?.Descriptor.Id, "builtin.preprocess.crop", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            offsetX += GetParameter(preprocessor, "X");
            offsetY += GetParameter(preprocessor, "Y");
        }

        return (offsetX, offsetY);
    }

    private static int GetParameter(ComponentSelectionViewModel<IImageProcessorFactory> selection, string key)
    {
        var parameter = selection.Parameters.FirstOrDefault(entry => string.Equals(entry.Definition.Key, key, StringComparison.OrdinalIgnoreCase));
        return parameter is null ? 0 : NumericInputHelper.ParseInt32OrDefault(parameter.Value, 0);
    }

    public bool IsProfileNumericTextValid(string propertyName, string proposedText)
    {
        return propertyName switch
        {
            nameof(TargetFpsText) => NumericInputHelper.IsValidProposedText(proposedText, allowsDecimal: false, minimum: 1),
            nameof(CaptureScaleText) => NumericInputHelper.IsValidProposedText(proposedText, allowsDecimal: true, minimum: 0.05d),
            nameof(FrameHistoryRetentionSecondsText) => NumericInputHelper.IsValidProposedText(proposedText, allowsDecimal: false, minimum: 0d),
            nameof(EventActionFrameOffsetText) => NumericInputHelper.IsValidProposedText(proposedText, allowsDecimal: false, minimum: -600d),
            _ => false
        };
    }

    public void StepProfileNumericValue(string propertyName, int direction)
    {
        switch (propertyName)
        {
            case nameof(TargetFpsText):
                TargetFpsText = NumericInputHelper.StepValue(TargetFpsText, "60", allowsDecimal: false, step: 1d, direction, minimum: 1d);
                break;
            case nameof(CaptureScaleText):
                CaptureScaleText = NumericInputHelper.StepValue(CaptureScaleText, "1.00", allowsDecimal: true, step: 0.05d, direction, minimum: 0.05d, maximum: 8.0d);
                break;
            case nameof(FrameHistoryRetentionSecondsText):
                FrameHistoryRetentionSecondsText = NumericInputHelper.StepValue(FrameHistoryRetentionSecondsText, "10", allowsDecimal: false, step: 1d, direction, minimum: 0d, maximum: 3600d);
                break;
            case nameof(EventActionFrameOffsetText):
                EventActionFrameOffsetText = NumericInputHelper.StepValue(EventActionFrameOffsetText, "0", allowsDecimal: false, step: 1d, direction, minimum: -600d, maximum: 600d);
                break;
        }
    }

    private ImageSource BuildOcrTargetPreviewImage(RecognitionFrame sourceFrame, IReadOnlyList<OcrPreviewFrame> previewFrames)
    {
        var sourceBitmap = CreateBitmapSource(sourceFrame);
        var overlays = previewFrames
            .Select(frame => (frame.Name, frame.Region, Bitmap: CreateBitmapSource(frame.Frame)))
            .ToArray();
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(sourceBitmap, new Rect(0, 0, sourceFrame.Width, sourceFrame.Height));
            var pen = new Pen(Brushes.Gold, Math.Max(2d, Math.Min(sourceFrame.Width, sourceFrame.Height) / 220d));
            pen.Freeze();
            foreach (var item in overlays)
            {
                var region = item.Region.IsEmpty
                    ? new Rect(0, 0, sourceFrame.Width, sourceFrame.Height)
                    : new Rect(item.Region.X, item.Region.Y, item.Region.Width, item.Region.Height);
                context.DrawImage(item.Bitmap, region);
                context.DrawRectangle(null, pen, region);
                if (ShowOcrPreviewLabels)
                {
                    var label = new FormattedText(
                        item.Name,
                        System.Globalization.CultureInfo.CurrentUICulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        14,
                        Brushes.Gold,
                        96);
                    var labelRect = new Rect(region.X, Math.Max(0, region.Y - label.Height - 2), label.Width + 6, label.Height + 2);
                    context.DrawRectangle(Brushes.Black, null, labelRect);
                    context.DrawText(label, new Point(region.X + 3, Math.Max(0, region.Y - label.Height - 1)));
                }
            }
        }

        var rendered = new RenderTargetBitmap(sourceFrame.Width, sourceFrame.Height, 96, 96, PixelFormats.Pbgra32);
        rendered.Render(visual);
        rendered.Freeze();
        return rendered;
    }

    private ImageSource BuildOcrReferencePreviewImage(RecognitionFrame sourceFrame, IReadOnlyList<OcrReferencePreviewEntry> references)
    {
        var sourceBitmap = CreateBitmapSource(sourceFrame);
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(sourceBitmap, new Rect(0, 0, sourceFrame.Width, sourceFrame.Height));
            var matchedPen = new Pen(Brushes.Red, Math.Max(2d, Math.Min(sourceFrame.Width, sourceFrame.Height) / 220d));
            var unmatchedPen = new Pen(Brushes.DodgerBlue, Math.Max(2d, Math.Min(sourceFrame.Width, sourceFrame.Height) / 220d));
            matchedPen.Freeze();
            unmatchedPen.Freeze();

            foreach (var reference in references)
            {
                if (reference.Evaluation is not { Region.IsEmpty: false } evaluation)
                {
                    continue;
                }

                var region = new Rect(evaluation.Region.X, evaluation.Region.Y, evaluation.Region.Width, evaluation.Region.Height);
                var pen = evaluation.IsMatched ? matchedPen : unmatchedPen;
                context.DrawRectangle(null, pen, region);

                if (ShowOcrPreviewLabels)
                {
                    DrawOverlayLabel(
                        context,
                        $"{reference.Name} {evaluation.Confidence:F3}/{reference.Threshold:F3}",
                        region.X,
                        region.Y,
                        evaluation.IsMatched ? Brushes.Red : Brushes.DodgerBlue);
                }
            }
        }

        var rendered = new RenderTargetBitmap(sourceFrame.Width, sourceFrame.Height, 96, 96, PixelFormats.Pbgra32);
        rendered.Render(visual);
        rendered.Freeze();
        return rendered;
    }

    private ImageSource BuildDistanceMeasurementPreviewImage(RecognitionFrame sourceFrame, IReadOnlyList<DistanceMeasurementPreviewItem> items)
    {
        var sourceBitmap = CreateBitmapSource(sourceFrame);
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(sourceBitmap, new Rect(0, 0, sourceFrame.Width, sourceFrame.Height));
            var referencePen = new Pen(Brushes.LimeGreen, Math.Max(2d, Math.Min(sourceFrame.Width, sourceFrame.Height) / 220d));
            var targetPen = new Pen(Brushes.OrangeRed, Math.Max(2d, Math.Min(sourceFrame.Width, sourceFrame.Height) / 220d));
            referencePen.Freeze();
            targetPen.Freeze();

            foreach (var item in items)
            {
                var referenceRect = new Rect(item.ReferenceRegion.X, item.ReferenceRegion.Y, item.ReferenceRegion.Width, item.ReferenceRegion.Height);
                var targetRect = new Rect(item.TargetRegion.X, item.TargetRegion.Y, item.TargetRegion.Width, item.TargetRegion.Height);
                context.DrawRectangle(null, referencePen, referenceRect);
                context.DrawRectangle(null, targetPen, targetRect);

                var referenceCenter = new Point(referenceRect.X + (referenceRect.Width / 2d), referenceRect.Y + (referenceRect.Height / 2d));
                var targetCenter = new Point(targetRect.X + (targetRect.Width / 2d), targetRect.Y + (targetRect.Height / 2d));
                context.DrawLine(targetPen, referenceCenter, targetCenter);

                if (ShowDistancePreviewAnnotations)
                {
                    DrawOverlayLabel(context, item.ReferenceName, referenceRect.X, referenceRect.Y, Brushes.LimeGreen);
                    DrawOverlayLabel(context, item.TargetName, targetRect.X, targetRect.Y, Brushes.OrangeRed);
                    var midX = (referenceCenter.X + targetCenter.X) / 2d;
                    var midY = (referenceCenter.Y + targetCenter.Y) / 2d;
                    DrawOverlayLabel(context, $"ΔX={item.DeltaX}, ΔY={item.DeltaY}", midX, midY, Brushes.DeepSkyBlue);
                }
            }

            if (ShowDistanceCursorPreview && previewCursorPoint is { } cursorPoint && items.Count > 0)
            {
                var cursorPen = new Pen(Brushes.DeepSkyBlue, Math.Max(1.5d, Math.Min(sourceFrame.Width, sourceFrame.Height) / 320d))
                {
                    DashStyle = DashStyles.Dash
                };
                cursorPen.Freeze();

                var referenceEntries = items
                    .Select(item =>
                    {
                        var rect = new Rect(item.ReferenceRegion.X, item.ReferenceRegion.Y, item.ReferenceRegion.Width, item.ReferenceRegion.Height);
                        var center = new Point(rect.X + (rect.Width / 2d), rect.Y + (rect.Height / 2d));
                        return (item.ReferenceName, item.ReferenceRegion, Center: center);
                    })
                    .DistinctBy(static entry => (entry.ReferenceName, entry.ReferenceRegion.X, entry.ReferenceRegion.Y, entry.ReferenceRegion.Width, entry.ReferenceRegion.Height))
                    .ToArray();

                context.DrawEllipse(Brushes.DeepSkyBlue, null, cursorPoint, 4, 4);

                var labelY = Math.Min(sourceFrame.Height - 8d, cursorPoint.Y + 18d);
                foreach (var entry in referenceEntries)
                {
                    var deltaX = cursorPoint.X - entry.Center.X;
                    var deltaY = cursorPoint.Y - entry.Center.Y;
                    var distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
                    context.DrawLine(cursorPen, entry.Center, cursorPoint);
                    DrawOverlayLabel(
                        context,
                        $"{entry.ReferenceName}: {distance:F1}px (ΔX={deltaX:F0}, ΔY={deltaY:F0})",
                        Math.Min(sourceFrame.Width - 220d, cursorPoint.X + 12d),
                        labelY,
                        Brushes.DeepSkyBlue);
                    labelY += 22d;
                }
            }
        }

        var rendered = new RenderTargetBitmap(sourceFrame.Width, sourceFrame.Height, 96, 96, PixelFormats.Pbgra32);
        rendered.Render(visual);
        rendered.Freeze();
        return rendered;
    }

    private static void DrawOverlayLabel(DrawingContext context, string text, double x, double y, Brush foreground)
    {
        var label = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            14,
            foreground,
            96);
        var labelRect = new Rect(x, Math.Max(0, y - label.Height - 2), label.Width + 6, label.Height + 2);
        context.DrawRectangle(Brushes.Black, null, labelRect);
        context.DrawText(label, new Point(x + 3, Math.Max(0, y - label.Height - 1)));
    }

    private static double NormalizeCaptureScale(double value)
    {
        return double.IsNaN(value) || double.IsInfinity(value)
            ? 1.0d
            : Math.Clamp(value, 0.05d, 8.0d);
    }

    private sealed record OcrReferencePreviewEntry(
        string Name,
        string TemplatePath,
        double Threshold,
        bool HasTemplateFile,
        TemplateMatchEvaluationResult? Evaluation);
}
