using System.Diagnostics;
using System.Drawing.Imaging;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class NdloCrLiteOcrEngine : IOcrEngine
{
    private static readonly object BootstrapLock = new();
    private static string? pythonExecutablePath;
    private static string? repositoryRootPath;
    private readonly bool enableTcy;

    public NdloCrLiteOcrEngine(IReadOnlyDictionary<string, string> parameters)
    {
        enableTcy = parameters.GetBoolean("EnableTcy", true);
        _ = EnsureBootstrap();
    }

    public OcrResult Read(RecognitionFrame frame)
    {
        var bootstrap = EnsureBootstrap();
        var workingDirectory = Path.Combine(Path.GetTempPath(), "AutoCountTool", "ndlocr-lite", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);
        var imagePath = Path.Combine(workingDirectory, "input.png");
        var outputPath = Path.Combine(workingDirectory, "output");
        Directory.CreateDirectory(outputPath);

        using (var bitmap = OpenCvFrameConversion.ToBitmap(frame))
        {
            bitmap.Save(imagePath, ImageFormat.Png);
        }

        try
        {
            var arguments = $"\"{Path.Combine(bootstrap.RepositoryRootPath, "src", "ocr.py")}\" --sourceimg \"{imagePath}\" --output \"{outputPath}\"";
            if (enableTcy)
            {
                arguments += " --enable-tcy";
            }

            RunProcess(bootstrap.PythonExecutablePath, arguments, bootstrap.RepositoryRootPath, "NDLOCR-Lite OCR execution failed.");

            var textFilePath = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(imagePath)}.txt");
            if (!File.Exists(textFilePath))
            {
                throw new InvalidOperationException($"NDLOCR-Lite output file was not created: {textFilePath}");
            }

            var text = File.ReadAllText(textFilePath).Trim();
            return new OcrResult(text, 0d, []);
        }
        finally
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
        }
    }

    public void Dispose()
    {
    }

    private static (string PythonExecutablePath, string RepositoryRootPath) EnsureBootstrap()
    {
        lock (BootstrapLock)
        {
            if (!string.IsNullOrWhiteSpace(pythonExecutablePath) && !string.IsNullOrWhiteSpace(repositoryRootPath))
            {
                return (pythonExecutablePath, repositoryRootPath);
            }

            var root = Path.Combine(AppContext.BaseDirectory, "NDLOCRLite");
            var venvDirectory = Path.Combine(root, "venv");
            var pythonPath = Path.Combine(venvDirectory, "Scripts", "python.exe");
            var repoPath = Path.Combine(root, "repo");

            Directory.CreateDirectory(root);

            if (!File.Exists(pythonPath))
            {
                RunProcess("python", $"-m venv \"{venvDirectory}\"", root, "Failed to create NDLOCR-Lite virtual environment.");
            }

            if (!Directory.Exists(repoPath))
            {
                RunProcess("git", $"clone --depth 1 https://github.com/ndl-lab/ndlocr-lite \"{repoPath}\"", root, "Failed to clone NDLOCR-Lite repository.");
            }

            RunProcess(pythonPath, "-m pip install --upgrade pip", root, "Failed to update pip for NDLOCR-Lite.");
            RunProcess(pythonPath, $"-m pip install -r \"{Path.Combine(repoPath, "requirements.txt")}\"", repoPath, "Failed to install NDLOCR-Lite dependencies.");

            pythonExecutablePath = pythonPath;
            repositoryRootPath = repoPath;
            return (pythonExecutablePath, repositoryRootPath);
        }
    }

    private static void RunProcess(string fileName, string arguments, string workingDirectory, string failureMessage)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{failureMessage}{Environment.NewLine}Command: {fileName} {arguments}{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}".Trim());
    }
}
