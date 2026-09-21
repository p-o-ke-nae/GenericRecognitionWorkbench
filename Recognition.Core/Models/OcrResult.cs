namespace Recognition.Core;

public sealed record OcrResult(string Text, double Confidence, IReadOnlyList<OcrTextBlock> Blocks);
