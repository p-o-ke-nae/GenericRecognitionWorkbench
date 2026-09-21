namespace Recognition.Core;

public sealed class DistanceMeasurementResult
{
    public DistanceMeasurementResult(string text, RecognitionFrame previewFrame, IReadOnlyList<DistanceMeasurementPreviewItem> items)
    {
        Text = text;
        PreviewFrame = previewFrame;
        Items = items;
    }

    public string Text { get; }

    public RecognitionFrame PreviewFrame { get; }

    public IReadOnlyList<DistanceMeasurementPreviewItem> Items { get; }
}
