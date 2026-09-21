using System.Runtime.InteropServices;

namespace Recognition.Infrastructure;

internal static class PaddleRuntimeBootstrapper
{
    private static readonly Lock SyncRoot = new();
    private static bool isLoaded;

    public static void EnsureLoaded()
    {
        lock (SyncRoot)
        {
            if (isLoaded)
            {
                return;
            }

            var baseDirectory = Path.GetDirectoryName(typeof(PaddleRuntimeBootstrapper).Assembly.Location)
                ?? AppContext.BaseDirectory;
            PrependToPath(baseDirectory);
            LoadRequiredLibrary(baseDirectory, "libiomp5md.dll");
            LoadRequiredLibrary(baseDirectory, "mklml.dll");
            isLoaded = true;
        }
    }

    private static void LoadRequiredLibrary(string baseDirectory, string fileName)
    {
        var filePath = Path.Combine(baseDirectory, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"PaddleOCR dependency '{fileName}' was not found in the application output directory.", filePath);
        }

        NativeLibrary.Load(filePath);
    }

    private static void PrependToPath(string baseDirectory)
    {
        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var entries = currentPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (entries.Contains(baseDirectory, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        Environment.SetEnvironmentVariable("PATH", $"{baseDirectory}{Path.PathSeparator}{currentPath}");
    }
}
