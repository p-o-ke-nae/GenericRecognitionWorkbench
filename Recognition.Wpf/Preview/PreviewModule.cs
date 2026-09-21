using System.Windows.Media;
using Recognition.Core;

namespace Recognition.Wpf;

public sealed class PreviewModule(
    string id,
    PreviewDisplayMode? legacyMode,
    Func<UiLocalization, string> displayNameFactory,
    Func<RecognitionWorkbenchViewModel, RecognitionCycleResult, ImageSource?> previewBuilder,
    IReadOnlyList<PreviewSettingDefinition>? settings = null) : IPreviewModule
{
    public string Id { get; } = id;

    public PreviewDisplayMode? LegacyMode { get; } = legacyMode;

    public IReadOnlyList<PreviewSettingDefinition> Settings { get; } = settings ?? [];

    public string GetDisplayName(UiLocalization localization)
    {
        return displayNameFactory(localization);
    }

    public ImageSource? BuildPreview(RecognitionWorkbenchViewModel viewModel, RecognitionCycleResult result)
    {
        return previewBuilder(viewModel, result);
    }
}
