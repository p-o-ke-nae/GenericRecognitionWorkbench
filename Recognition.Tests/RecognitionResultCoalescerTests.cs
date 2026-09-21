using Recognition.Core;
using Recognition.Infrastructure;
using Xunit;

namespace Recognition.Tests;

public sealed class RecognitionResultCoalescerTests
{
    [Fact]
    public void NonEventsCoalesceToOneLatestResult()
    {
        var coalescer = new RecognitionResultCoalescer();
        var results = Enumerable.Range(0, 100).Select(static index => Result(index)).ToArray();
        foreach (var result in results) coalescer.Enqueue(result);

        Assert.Same(results[^1], Assert.Single(Drain(coalescer)));
        Assert.Empty(Drain(coalescer));
    }

    [Fact]
    public void BriefEventSurvivesFiftyLaterNonEvents()
    {
        var coalescer = new RecognitionResultCoalescer();
        var eventResult = Result(0, eventTriggered: true);
        coalescer.Enqueue(eventResult);
        var previews = Enumerable.Range(1, 50).Select(static index => Result(index)).ToArray();
        foreach (var preview in previews) coalescer.Enqueue(preview);

        var applied = Drain(coalescer);
        Assert.Equal(2, applied.Count);
        Assert.Same(eventResult, applied[0]);
        Assert.Same(previews[^1], applied[1]);
    }

    [Fact]
    public void MultipleEventsDrainInOrderBeforeLatestPreview()
    {
        var coalescer = new RecognitionResultCoalescer();
        var first = Result(0, eventTriggered: true);
        var second = Result(2, eventTriggered: true);
        var latest = Result(3);
        coalescer.Enqueue(first);
        coalescer.Enqueue(Result(1));
        coalescer.Enqueue(second);
        coalescer.Enqueue(latest);

        Assert.Equal(new[] { first, second, latest }, Drain(coalescer));
    }

    [Fact]
    public void ConsecutiveEventsBothDrainAndSupersedeOlderPreview()
    {
        var coalescer = new RecognitionResultCoalescer();
        var first = Result(1, eventTriggered: true);
        var second = Result(2, eventTriggered: true);
        coalescer.Enqueue(Result(0));
        coalescer.Enqueue(first);
        coalescer.Enqueue(second);
        Assert.Equal(new[] { first, second }, Drain(coalescer));
    }

    [Fact]
    public void OverflowWarnsOnceWithoutDroppingEvents()
    {
        var coalescer = new RecognitionResultCoalescer();
        var events = Enumerable.Range(0, 100).Select(static index => Result(index, eventTriggered: true)).ToArray();
        foreach (var result in events.Take(64)) coalescer.Enqueue(result);
        Assert.False(coalescer.TakeBacklogWarning());
        foreach (var result in events.Skip(64)) coalescer.Enqueue(result);
        Assert.True(coalescer.TakeBacklogWarning());
        Assert.False(coalescer.TakeBacklogWarning());
        Assert.Equal(events, Drain(coalescer));

        foreach (var result in events) coalescer.Enqueue(result);
        Assert.True(coalescer.TakeBacklogWarning());
        Assert.Equal(events, Drain(coalescer));
    }

    [Fact]
    public void ResultsArrivingDuringDrainAreNotLost()
    {
        var coalescer = new RecognitionResultCoalescer();
        var first = Result(0, eventTriggered: true);
        var second = Result(1, eventTriggered: true);
        coalescer.Enqueue(first);
        Assert.True(coalescer.TryDequeue(out var result));
        Assert.Same(first, result);
        coalescer.Enqueue(second);
        coalescer.Enqueue(Result(2));
        var remainder = Drain(coalescer);
        Assert.Equal(2, remainder.Count);
        Assert.Same(second, remainder[0]);
    }

    [Fact]
    public void ClearResetsPendingResultsAndWarning()
    {
        var coalescer = new RecognitionResultCoalescer();
        for (var i = 0; i < 65; i++) coalescer.Enqueue(Result(i, eventTriggered: true));
        coalescer.Enqueue(Result(66));
        coalescer.Clear();
        Assert.False(coalescer.TakeBacklogWarning());
        Assert.Empty(Drain(coalescer));
    }

    private static List<RecognitionCycleResult> Drain(RecognitionResultCoalescer coalescer)
    {
        var results = new List<RecognitionCycleResult>();
        while (coalescer.TryDequeue(out var result)) results.Add(result);
        return results;
    }

    private static RecognitionCycleResult Result(int index, bool eventTriggered = false)
    {
        var timestamp = DateTimeOffset.UnixEpoch.AddMilliseconds(index * 16);
        var frame = new RecognitionFrame([0], 1, 1, 1, FramePixelFormat.Gray8, timestamp);
        return new RecognitionCycleResult
        {
            Timestamp = timestamp,
            SourceFrame = frame,
            PreviewFrame = frame,
            IsDetected = eventTriggered,
            EventTriggered = eventTriggered,
            DetectionConfidence = eventTriggered ? 1 : 0,
            FramesPerSecond = 60
        };
    }
}
