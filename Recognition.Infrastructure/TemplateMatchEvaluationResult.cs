using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed record TemplateMatchEvaluationResult(
    RoiArea Region,
    double Confidence,
    double Threshold)
{
    public bool IsMatched => !Region.IsEmpty && Confidence >= Threshold;
}
