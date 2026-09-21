using OpenCvSharp;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class ImageFileFrameSource : IFrameSource
{
    private readonly string imagePath;

    public ImageFileFrameSource(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            throw new FileNotFoundException("Image file was not found.", imagePath);
        }

        this.imagePath = imagePath;
    }

    public ValueTask<RecognitionFrame> CaptureAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var image = Cv2.ImRead(imagePath, ImreadModes.Unchanged);
        if (image.Empty())
        {
            throw new InvalidOperationException($"Image file '{imagePath}' could not be loaded.");
        }

        return ValueTask.FromResult(OpenCvFrameConversion.ToFrame(image, DateTimeOffset.UtcNow));
    }

    public void Dispose()
    {
    }
}
