using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed record ImageRecognitionPipelineTarget(
    string Name,
    IRecognitionMethod Recognizer,
    IReadOnlyList<IImageProcessor> Processors);
