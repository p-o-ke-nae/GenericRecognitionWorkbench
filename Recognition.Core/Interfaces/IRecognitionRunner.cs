namespace Recognition.Core;

public interface IRecognitionRunner
{
    Task<RecognitionCycleResult> ExecuteOnceAsync(RecognitionProfile profile, CancellationToken cancellationToken = default);

    IRecognitionSession CreateContinuousSession(RecognitionProfile profile);
}
