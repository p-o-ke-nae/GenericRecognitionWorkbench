using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed record OcrExecutionResult(
    OcrResult Result,
    IReadOnlyList<OcrPreviewFrame> PreviewFrames,
    IReadOnlyDictionary<string, OcrResult> ResultsByTarget);
