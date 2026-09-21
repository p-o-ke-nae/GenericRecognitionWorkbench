using System.Diagnostics;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class RecognitionSession(RecognitionProfile profile, RecognitionRunner runner) : IRecognitionSession
{
    private readonly RecognitionProfile profile = profile.Clone();
    private readonly RecognitionRunner runner = runner;
    private readonly CancellationTokenSource stopSource = new();
    private Task? runTask;

    public event EventHandler<RecognitionCycleResult>? CycleCompleted;

    public event EventHandler<Exception>? Failed;

    public bool IsRunning { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        Console.Error.WriteLine("[RecognitionSession.StartAsync] Starting recognition loop");
        IsRunning = true;
        var linked = CancellationTokenSource.CreateLinkedTokenSource(stopSource.Token, cancellationToken);
        runTask = Task.Run(async () =>
        {
            using (linked)
            {
                Console.Error.WriteLine("[RecognitionSession.RunAsync] Background task started");
                await RunAsync(linked.Token).ConfigureAwait(false);
                Console.Error.WriteLine("[RecognitionSession.RunAsync] Background task completed");
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        stopSource.Cancel();
        if (runTask is not null)
        {
            await runTask.ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        stopSource.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            Console.Error.WriteLine("[RunAsync] Building pipeline...");
            using var pipeline = runner.BuildPipeline(profile);
            Console.Error.WriteLine("[RunAsync] Pipeline built successfully");
            
            var state = new RecognitionRuntimeState();
            var targetDelay = profile.TargetFps <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(1d / profile.TargetFps);
            Console.Error.WriteLine($"[RunAsync] Target delay: {targetDelay.TotalMilliseconds}ms for {profile.TargetFps} FPS");

            int cycleCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                var startedAt = Stopwatch.GetTimestamp();
                cycleCount++;
                if (cycleCount <= 3)
                {
                    Console.Error.WriteLine($"[RunAsync] Cycle {cycleCount}: About to call ExecuteCycle");
                }
                
                // ここにブレークポイントを設定してください
                var result = runner.ExecuteCycle(profile, pipeline, state, forceOcrWhenDetected: false, runActionsForTest: false, cancellationToken);
                
                if (cycleCount <= 3)
                {
                    Console.Error.WriteLine($"[RunAsync] Cycle {cycleCount}: ExecuteCycle completed, IsDetected={result.IsDetected}, FPS={result.FramesPerSecond:F1}");
                }
                CycleCompleted?.Invoke(this, result);

                if (targetDelay > TimeSpan.Zero)
                {
                    var elapsed = TimeSpan.FromSeconds((Stopwatch.GetTimestamp() - startedAt) / (double)Stopwatch.Frequency);
                    var remaining = targetDelay - elapsed;
                    if (remaining > TimeSpan.Zero)
                    {
                        await Task.Delay(remaining, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            Console.Error.WriteLine("[RunAsync] Cancellation requested, exiting loop");
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("[RunAsync] OperationCanceledException");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RunAsync] Exception: {ex}");
            Console.Error.WriteLine($"[RunAsync] Exception: {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine($"[RunAsync] StackTrace: {ex.StackTrace}");
            Failed?.Invoke(this, ex);
        }
        finally
        {
            Console.Error.WriteLine("[RunAsync] Finally block: setting IsRunning = false");
            IsRunning = false;
        }
    }
}
