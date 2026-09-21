namespace Recognition.Core;

public interface IRecognitionSession : IAsyncDisposable
{
    event EventHandler<RecognitionCycleResult>? CycleCompleted;

    event EventHandler<Exception>? Failed;

    bool IsRunning { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync();
}
