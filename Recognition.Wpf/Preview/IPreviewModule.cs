using System.Windows.Media;
using Recognition.Core;

namespace Recognition.Wpf;

public interface IPreviewModule
{
    string Id { get; }

    PreviewDisplayMode? LegacyMode { get; }

    IReadOnlyList<PreviewSettingDefinition> Settings { get; }

    string GetDisplayName(UiLocalization localization);

    ImageSource? BuildPreview(RecognitionWorkbenchViewModel viewModel, RecognitionCycleResult result);
}
