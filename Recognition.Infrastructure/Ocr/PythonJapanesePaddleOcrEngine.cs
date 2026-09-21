using System.Diagnostics;
using System.Drawing.Imaging;
using System.Text;
using System.Text.Json;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal sealed class PythonJapanesePaddleOcrEngine : IOcrEngine
{
    private const string QuitCommand = "__quit__";
    private const string WorkerScriptName = "paddle_japanese_worker.py";
    private static readonly object BootstrapLock = new();
    private static string? pythonExecutablePath;
    private static string? workerScriptPath;
    private static SharedWorker? sharedWorker;

    public PythonJapanesePaddleOcrEngine(IReadOnlyDictionary<string, string> parameters)
    {
        var bootstrap = EnsureBootstrap();
        lock (BootstrapLock)
        {
            sharedWorker ??= new SharedWorker(bootstrap.PythonExecutablePath, bootstrap.WorkerScriptPath);
        }
    }

    public OcrResult Read(RecognitionFrame frame)
    {
        var worker = sharedWorker ?? throw new InvalidOperationException("Python OCR worker is not initialized.");
        lock (worker.ProcessLock)
        {
            EnsureWorkerAlive(worker);

            var imagePath = Path.Combine(worker.TempDirectory, $"{Guid.NewGuid():N}.png");
            using (var bitmap = OpenCvFrameConversion.ToBitmap(frame))
            {
                bitmap.Save(imagePath, ImageFormat.Png);
            }

            try
            {
                worker.Process.StandardInput.WriteLine(imagePath);
                worker.Process.StandardInput.Flush();

                var responseLine = worker.Process.StandardOutput.ReadLine();
                if (string.IsNullOrWhiteSpace(responseLine))
                {
                    throw new InvalidOperationException(BuildWorkerFailureMessage(worker, "Python PaddleOCR worker returned no result."));
                }

                using var json = JsonDocument.Parse(responseLine);
                var root = json.RootElement;
                if (root.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.String)
                {
                    throw new InvalidOperationException(BuildWorkerFailureMessage(worker, errorElement.GetString() ?? "Python PaddleOCR worker failed."));
                }

                var text = root.TryGetProperty("text", out var textElement) ? textElement.GetString() ?? string.Empty : string.Empty;
                var confidence = root.TryGetProperty("confidence", out var confidenceElement) ? confidenceElement.GetDouble() : 0d;
                return new OcrResult(text.Trim(), confidence, []);
            }
            finally
            {
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }
            }
        }
    }

    public void Dispose()
    {
    }

    private static (string PythonExecutablePath, string WorkerScriptPath) EnsureBootstrap()
    {
        lock (BootstrapLock)
        {
            if (!string.IsNullOrWhiteSpace(pythonExecutablePath) && !string.IsNullOrWhiteSpace(workerScriptPath))
            {
                return (pythonExecutablePath, workerScriptPath);
            }

            var root = Path.Combine(AppContext.BaseDirectory, "PythonOcr");
            var venvDirectory = Path.Combine(root, "venv");
            var pythonPath = Path.Combine(venvDirectory, "Scripts", "python.exe");
            var scriptPath = Path.Combine(root, WorkerScriptName);

            Directory.CreateDirectory(root);

            if (!File.Exists(pythonPath))
            {
                RunProcess("python", $"-m venv \"{venvDirectory}\"", root, "Failed to create Python OCR virtual environment.");
            }

            RunProcess(pythonPath, "-m pip install --upgrade pip", root, "Failed to update pip for Japanese PaddleOCR.");
            RunProcess(pythonPath, "-m pip install paddleocr paddlepaddle", root, "Failed to install Japanese PaddleOCR dependencies.");

            File.WriteAllText(scriptPath, BuildWorkerScript(), Encoding.UTF8);

            pythonExecutablePath = pythonPath;
            workerScriptPath = scriptPath;
            return (pythonExecutablePath, workerScriptPath);
        }
    }

    private void EnsureWorkerAlive(SharedWorker worker)
    {
        if (!worker.Process.HasExited)
        {
            return;
        }

        throw new InvalidOperationException(BuildWorkerFailureMessage(worker, "Python PaddleOCR worker exited unexpectedly."));
    }

    private static string BuildWorkerFailureMessage(SharedWorker worker, string message)
    {
        lock (worker.ErrorBuffer)
        {
            var details = worker.ErrorBuffer.ToString().Trim();
            return string.IsNullOrWhiteSpace(details)
                ? message
                : $"{message}{Environment.NewLine}{details}";
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

    private static string BuildWorkerScript()
    {
        return
"""
import json
import os
import sys
os.environ.setdefault("PADDLE_PDX_DISABLE_MODEL_SOURCE_CHECK", "True")
from paddleocr import TextRecognition

recognizer = TextRecognition(model_name="japan_PP-OCRv3_mobile_rec")

for raw in sys.stdin:
    image_path = raw.strip()
    if not image_path:
        continue
    if image_path == "__quit__":
        break
    try:
        result = recognizer.predict(image_path)
        pairs = []
        for item in result:
            text = item.get("rec_text", "") if isinstance(item, dict) else ""
            score = float(item.get("rec_score", 0.0)) if isinstance(item, dict) else 0.0
            if text:
                pairs.append((text, score))
        text = "\n".join(pair[0] for pair in pairs if pair[0])
        confidence = sum(pair[1] for pair in pairs) / len(pairs) if pairs else 0.0
        print(json.dumps({"text": text, "confidence": confidence}, ensure_ascii=False), flush=True)
    except Exception as ex:
        print(json.dumps({"error": str(ex)}, ensure_ascii=False), flush=True)
""";
    }

    private sealed class SharedWorker
    {
        public SharedWorker(string pythonExecutablePath, string workerScriptPath)
        {
            TempDirectory = Path.Combine(Path.GetTempPath(), "AutoCountTool", "python-japanese-ocr", "shared");
            Directory.CreateDirectory(TempDirectory);
            Process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExecutablePath,
                    Arguments = $"\"{workerScriptPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            Process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    lock (ErrorBuffer)
                    {
                        ErrorBuffer.AppendLine(e.Data);
                    }
                }
            };
            Process.Start();
            Process.BeginErrorReadLine();
        }

        public object ProcessLock { get; } = new();

        public StringBuilder ErrorBuffer { get; } = new();

        public Process Process { get; }

        public string TempDirectory { get; }
    }
}
