namespace Recognition.Core;

public interface IRecognitionRunner
{
    Task<RecognitionCycleResult> ExecuteOnceAsync(RecognitionProfile profile, CancellationToken cancellationToken = default);

    Task<RecognitionCycleResult> ExecuteOnceAsync(RecognitionProfile profile, RecognitionFrame sourceFrame, CancellationToken cancellationToken = default);

    IRecognitionSession CreateContinuousSession(RecognitionProfile profile);
}
