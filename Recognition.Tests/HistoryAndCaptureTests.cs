using Recognition.Core;
using Recognition.Infrastructure;
using Xunit;

namespace Recognition.Tests;

public sealed class HistoryAndCaptureTests
{
    [Theory]
    [InlineData(FramePixelFormat.Gray8, 1)]
    [InlineData(FramePixelFormat.Bgr24, 3)]
    [InlineData(FramePixelFormat.Bgra32, 4)]
    public void CropCopiesOnlyRegionAndPreservesFormatAndTimestamp(FramePixelFormat format, int bytesPerPixel)
    {
        var stride = 4 * bytesPerPixel + 4;
        var pixels = Enumerable.Range(0, stride * 3).Select(static value => (byte)value).ToArray();
        var frame = new RecognitionFrame(pixels, 4, 3, stride, format, DateTimeOffset.UtcNow);
        var cropped = new CropImageProcessor(new RoiArea(1, 1, 2, 2)).Process(frame);
        Assert.Equal(2, cropped.Width);
        Assert.Equal(2, cropped.Height);
        Assert.Equal(2 * bytesPerPixel, cropped.Stride);
        Assert.Equal(format, cropped.PixelFormat);
        Assert.Equal(frame.CapturedAt, cropped.CapturedAt);
        Assert.Equal(
            pixels.Skip(stride + bytesPerPixel).Take(2 * bytesPerPixel)
                .Concat(pixels.Skip(2 * stride + bytesPerPixel).Take(2 * bytesPerPixel)),
            cropped.PixelData);
        Assert.Equal(Enumerable.Range(0, stride * 3).Select(static value => (byte)value), frame.PixelData);
        Assert.Same(frame, new CropImageProcessor(new RoiArea(99, 99, 1, 1)).Process(frame));
        Assert.Same(frame, new CropImageProcessor(new RoiArea(0, 0, 0, 0)).Process(frame));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HistoryExpiresAndSharesFrames(bool retainSource)
    {
        var now = DateTimeOffset.UtcNow;
        var history = new RecognitionFrameHistory(TimeSpan.FromSeconds(10), retainSource);
        var source = Frame();
        var processed = Frame();
        history.Add(now, source, processed);
        history.Add(now.AddSeconds(10), source, processed);
        Assert.Equal(2, history.Entries.Count);
        history.Add(now.AddSeconds(11), source, processed);
        Assert.Equal(2, history.Entries.Count);
        Assert.Equal(now.AddSeconds(10), history.Entries[0].Timestamp);
        Assert.All(history.Entries, entry =>
        {
            Assert.Same(processed, entry.ProcessedFrame);
            Assert.Same(retainSource ? source : null, entry.SourceFrame);
            Assert.Equal(retainSource, entry.HasSourceFrame);
        });
        var additions = history.GetEntriesAfter(2, out var firstSequence);
        Assert.Equal(2, firstSequence);
        Assert.Equal(3, Assert.Single(additions).Sequence);
        history.Clear();
        Assert.Empty(history.Entries);
    }

    [Fact]
    public void HistoryRingWrapsAndZeroRetentionKeepsLatest()
    {
        var now = DateTimeOffset.UtcNow;
        var history = new RecognitionFrameHistory(TimeSpan.FromSeconds(10));
        for (var i = 0; i < 2000; i++)
            history.Add(now.AddSeconds(i / 60d), Frame(), Frame());
        var entries = history.Entries;
        Assert.InRange(entries.Count, 600, 601);
        Assert.Equal(2000, entries[^1].Sequence);
        Assert.Equal(entries.Count, entries.Select(static entry => entry.Sequence).Distinct().Count());
        var zero = new RecognitionFrameHistory(TimeSpan.Zero);
        zero.Add(now, Frame(), Frame());
        zero.Add(now.AddTicks(1), Frame(), Frame());
        Assert.Single(zero.Entries);
    }

    [Fact]
    public async Task SlotIsNewestWinsWithSequenceGapsAndTimestamp()
    {
        var slot = new LatestFrameSlot();
        slot.Publish(Frame());
        var newest = Frame();
        slot.Publish(newest);
        Assert.Same(newest, await slot.CaptureAsync());
        Assert.Equal(2, slot.FrameSequence);
        Assert.Equal(1, slot.DroppedFrames);
        var waiting = slot.CaptureAsync().AsTask();
        Assert.False(waiting.IsCompleted);
        var next = Frame();
        slot.Publish(next);
        Assert.Same(next, await waiting.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.Equal(3, slot.FrameSequence);
        Assert.Equal(1, slot.DroppedFrames);
        slot.Publish(Frame());
        slot.Publish(Frame());
        slot.Publish(Frame());
        await slot.CaptureAsync();
        Assert.Equal(6, slot.FrameSequence);
        Assert.Equal(3, slot.DroppedFrames);
    }

    [Fact]
    public async Task SlotWaitCanBeCancelledAndCompletionWakesWaiter()
    {
        var slot = new LatestFrameSlot();
        using var cancellation = new CancellationTokenSource();
        var pending = slot.CaptureAsync(cancellation.Token).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        slot.Publish(Frame());
        await slot.CaptureAsync();
        var waiting = slot.CaptureAsync().AsTask();
        slot.Complete();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => waiting);
    }

    [Fact]
    public async Task SlotPropagatesCaptureFailure()
    {
        var slot = new LatestFrameSlot();
        var pending = slot.CaptureAsync().AsTask();
        slot.Complete(new InvalidOperationException("camera disconnected"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => pending);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProfileRoundTripsRetentionAndDefaultsWhenAbsent(bool retainSource)
    {
        var directory = Path.Combine(Environment.CurrentDirectory, $"test-artifacts-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "profile.json");
        var store = new JsonRecognitionProfileStore();
        try
        {
            var profile = new RecognitionProfile { RetainSourceFramesInHistory = retainSource };
            Assert.Equal(retainSource, profile.Clone().RetainSourceFramesInHistory);
            await store.SaveAsync(profile, path);
            Assert.Equal(retainSource, (await store.LoadAsync(path)).RetainSourceFramesInHistory);
            await File.WriteAllTextAsync(path, "{}");
            Assert.False((await store.LoadAsync(path)).RetainSourceFramesInHistory);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static RecognitionFrame Frame() => new([1, 2, 3], 1, 1, 3, FramePixelFormat.Bgr24, DateTimeOffset.UtcNow);

    [Fact]
    public void MirrorPreservesHistoryAcrossSingleShotsAndSessionRestarts()
    {
        var now = DateTimeOffset.UtcNow;
        var retention = TimeSpan.FromSeconds(10);
        var firstSession = new RecognitionFrameHistory(retention);
        var mirror = new RecognitionFrameHistoryMirror();
        firstSession.Add(now, Frame(), Frame());
        mirror.Apply(Result(now, firstSession), retention, false);
        var original = Assert.Single(mirror.Entries);

        var singleShotHistory = new RecognitionFrameHistory(retention);
        singleShotHistory.Add(now.AddSeconds(1), Frame(), Frame());
        mirror.Apply(Result(now.AddSeconds(1), singleShotHistory, singleShot: true), retention, false);
        Assert.Equal(2, mirror.Entries.Count);
        Assert.Same(original, mirror.Entries[0]);

        firstSession.Add(now.AddSeconds(2), Frame(), Frame());
        mirror.Apply(Result(now.AddSeconds(2), firstSession), retention, false);
        Assert.Equal(3, mirror.Entries.Count);

        var restartedSession = new RecognitionFrameHistory(retention);
        restartedSession.Add(now.AddSeconds(3), Frame(), Frame());
        mirror.Apply(Result(now.AddSeconds(3), restartedSession), retention, false);
        Assert.Equal(4, mirror.Entries.Count);
        Assert.Same(original, mirror.Entries[0]);
        Assert.All(mirror.Entries, static entry => Assert.Null(entry.SourceFrame));

        restartedSession.Add(now.AddSeconds(11), Frame(), Frame());
        var removed = mirror.Apply(Result(now.AddSeconds(11), restartedSession), retention, false);
        Assert.Equal(1, removed);
        Assert.Equal(now.AddSeconds(1), mirror.Entries[0].Timestamp);
        Assert.Equal(4, mirror.Entries.Count);
        mirror.Apply(Result(now.AddSeconds(11), restartedSession), retention, false);
        Assert.Equal(4, mirror.Entries.Count);
        mirror.Clear();
        Assert.Empty(mirror.Entries);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MirrorUsesLiveRawFrameOnlyForLatestEntry(bool retainSource)
    {
        var now = DateTimeOffset.UtcNow;
        var mirror = new RecognitionFrameHistoryMirror();
        var older = Result(now, null, singleShot: true);
        var latest = Result(now.AddSeconds(1), null, singleShot: true);
        mirror.Apply(older, TimeSpan.FromSeconds(10), retainSource);
        mirror.Apply(latest, TimeSpan.FromSeconds(10), retainSource);
        Assert.Same(retainSource ? older.SourceFrame : null,
            mirror.GetSourceFrame(mirror.Entries[0], latest.SourceFrame));
        Assert.Same(latest.SourceFrame, mirror.GetSourceFrame(mirror.Entries[1], latest.SourceFrame));
        Assert.Same(latest.SourceFrame, mirror.GetSourceFrame(null, latest.SourceFrame));
        Assert.NotSame(latest.PreviewFrame, mirror.GetSourceFrame(mirror.Entries[1], latest.SourceFrame));
        Assert.Equal(retainSource, mirror.Entries[0].Copy().HasSourceFrame);
    }

    private static RecognitionCycleResult Result(DateTimeOffset timestamp, RecognitionFrameHistory? history, bool singleShot = false) => new()
    {
        Timestamp = timestamp,
        SourceFrame = new(new byte[4 * 4 * 3], 4, 4, 12, FramePixelFormat.Bgr24, timestamp),
        PreviewFrame = Frame(),
        FrameHistory = history,
        IsSingleShot = singleShot,
        IsDetected = false,
        EventTriggered = false,
        DetectionConfidence = 0,
        FramesPerSecond = 60
    };
}
