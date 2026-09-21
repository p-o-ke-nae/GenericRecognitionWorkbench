using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed record ImageRecognitionExecutionResult(
    IReadOnlyDictionary<string, RecognitionMatch> ResultsByTarget);
