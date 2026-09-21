using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class ScreenRegionFrameSource(int x, int y, int width, int height) : IFrameSource
{
    public ValueTask<RecognitionFrame> CaptureAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

        try
        {
            var buffer = new byte[data.Stride * data.Height];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
            return ValueTask.FromResult(new RecognitionFrame(buffer, bitmap.Width, bitmap.Height, data.Stride, FramePixelFormat.Bgra32, DateTimeOffset.UtcNow));
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    public void Dispose()
    {
    }
}
