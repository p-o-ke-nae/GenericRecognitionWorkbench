namespace Recognition.Core;

public sealed class RecognitionProfileCalibrationResult
{
    public RecognitionProfileCalibrationResult(RecognitionFrame templateFrame, RoiArea matchedRegion, double suggestedThreshold, double confidence)
    {
        TemplateFrame = templateFrame;
        MatchedRegion = matchedRegion;
        SuggestedThreshold = suggestedThreshold;
        Confidence = confidence;
    }

    public RecognitionFrame TemplateFrame { get; }

    public RoiArea MatchedRegion { get; }

    public double SuggestedThreshold { get; }

    public double Confidence { get; }
}
