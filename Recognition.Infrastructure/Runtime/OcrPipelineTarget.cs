using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed record OcrPipelineTarget(
    string Name,
    RoiArea Region,
    string ReferenceName,
    bool UseRecognitionAnchor,
    IReadOnlyList<IImageProcessor> Processors);
