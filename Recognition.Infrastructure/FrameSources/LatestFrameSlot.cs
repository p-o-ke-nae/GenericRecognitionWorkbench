using Recognition.Core;

namespace Recognition.Infrastructure;

/// <summary>A single-consumer, newest-wins mailbox. Published frames must not be modified.</summary>
public sealed class LatestFrameSlot : IFrameSourceDiagnostics
{
    private readonly object gate = new();
    private TaskCompletionSource signal = CreateSignal();
    private RecognitionFrame? latest;
    private long publishedSequence;
    private long consumedSequence;
    private long droppedFrames;
    private Exception? completionError;

    public long FrameSequence { get { lock (gate) return consumedSequence; } }

    public long DroppedFrames { get { lock (gate) return droppedFrames; } }

    public void Publish(RecognitionFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        lock (gate)
        {
            if (completionError is not null) return;
            latest = frame;
            publishedSequence++;
            signal.TrySetResult();
        }
    }

    public async ValueTask<RecognitionFrame> CaptureAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            Task wait;
            lock (gate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (completionError is not null)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(completionError);
                if (publishedSequence > consumedSequence)
                {
                    droppedFrames += publishedSequence - consumedSequence - 1;
                    consumedSequence = publishedSequence;
                    var frame = latest!;
                    latest = null;
                    signal = CreateSignal();
                    return frame;
                }
                wait = signal.Task;
            }
            await wait.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public void Complete(Exception? error = null)
    {
        lock (gate)
        {
            completionError ??= error ?? new ObjectDisposedException(nameof(LatestFrameSlot));
            latest = null;
            signal.TrySetResult();
        }
    }

    private static TaskCompletionSource CreateSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
