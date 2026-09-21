namespace Recognition.Core;

public sealed record RecognitionMatch(bool IsDetected, double Confidence, string? Label = null, RoiArea? Region = null);
