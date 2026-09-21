namespace Recognition.Core;

public interface IRecognitionProfileCalibrationService
{
    Task<RecognitionProfileCalibrationResult> CalibrateAsync(
        RecognitionProfile profile,
        RecognitionFrame processedFrame,
        CancellationToken cancellationToken = default);
}
