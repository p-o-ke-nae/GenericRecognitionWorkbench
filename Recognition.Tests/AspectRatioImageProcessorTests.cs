using Recognition.Core;
using Recognition.Infrastructure;
using Xunit;

namespace Recognition.Tests;

public sealed class AspectRatioImageProcessorTests
{
    [Theory]
    [InlineData(FramePixelFormat.Gray8, 1)]
    [InlineData(FramePixelFormat.Bgr24, 3)]
    [InlineData(FramePixelFormat.Bgra32, 4)]
    public void ProcessScalesDimensionsAndPreservesFrameMetadata(FramePixelFormat pixelFormat, int bytesPerPixel)
    {
        var capturedAt = DateTimeOffset.UtcNow;
        var frame = Frame(4, 6, pixelFormat, bytesPerPixel, capturedAt);

        var processed = new AspectRatioImageProcessor(2.0d, 0.5d).Process(frame);

        Assert.Equal(8, processed.Width);
        Assert.Equal(3, processed.Height);
        Assert.Equal(8 * bytesPerPixel, processed.Stride);
        Assert.Equal(3 * processed.Stride, processed.PixelData.Length);
        Assert.Equal(pixelFormat, processed.PixelFormat);
        Assert.Equal(capturedAt, processed.CapturedAt);
    }

    [Fact]
    public void ProcessReturnsOriginalFrameWhenDimensionsAreUnchanged()
    {
        var frame = Frame(4, 6, FramePixelFormat.Gray8, 1, DateTimeOffset.UtcNow);

        var processed = new AspectRatioImageProcessor(1.0d, 1.0d).Process(frame);

        Assert.Same(frame, processed);
    }

    [Fact]
    public void FactoryNormalizesInvalidAndOutOfRangeScales()
    {
        var factory = new AspectRatioImageProcessorFactory();
        var frame = Frame(10, 10, FramePixelFormat.Gray8, 1, DateTimeOffset.UtcNow);

        var bounded = factory.Create(new Dictionary<string, string>
        {
            ["WidthScale"] = "-1",
            ["HeightScale"] = "100"
        }).Process(frame);
        var defaults = factory.Create(new Dictionary<string, string>
        {
            ["WidthScale"] = "not-a-number",
            ["HeightScale"] = "NaN"
        }).Process(frame);

        Assert.Equal((1, 100), (bounded.Width, bounded.Height));
        Assert.Same(frame, defaults);
    }

    [Fact]
    public void FactoryDescribesSpinnerParametersAndIsBuiltIn()
    {
        var factory = new AspectRatioImageProcessorFactory();

        Assert.Equal("builtin.preprocess.aspect-ratio", factory.Descriptor.Id);
        Assert.Collection(
            factory.Descriptor.Parameters,
            parameter => AssertParameter(parameter, "WidthScale"),
            parameter => AssertParameter(parameter, "HeightScale"));

        var pluginDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var catalog = new RecognitionPluginCatalog(pluginDirectory);
        Assert.Contains(catalog.ImageProcessorFactories, candidate => candidate.Descriptor.Id == factory.Descriptor.Id);
    }

    private static void AssertParameter(ParameterDefinition parameter, string key)
    {
        Assert.Equal(key, parameter.Key);
        Assert.Equal(ParameterValueKind.Decimal, parameter.ValueKind);
        Assert.Equal("1.0", parameter.DefaultValue);
        Assert.Equal(0.1d, parameter.Minimum);
        Assert.Equal(10.0d, parameter.Maximum);
        Assert.Equal(0.1d, parameter.Step);
    }

    private static RecognitionFrame Frame(
        int width,
        int height,
        FramePixelFormat pixelFormat,
        int bytesPerPixel,
        DateTimeOffset capturedAt)
    {
        var stride = width * bytesPerPixel;
        var pixels = Enumerable.Range(0, stride * height).Select(static value => (byte)value).ToArray();
        return new RecognitionFrame(pixels, width, height, stride, pixelFormat, capturedAt);
    }
}
