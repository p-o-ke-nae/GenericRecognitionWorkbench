using System.Reflection;
using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class RecognitionPluginCatalog : IRecognitionPluginCatalog
{
    private readonly string pluginDirectory;
    private readonly IReadOnlyList<IRecognitionMethodFactory> additionalRecognitionMethodFactories;

    public RecognitionPluginCatalog(string pluginDirectory)
        : this(pluginDirectory, null)
    {
    }

    public RecognitionPluginCatalog(
        string pluginDirectory,
        IEnumerable<IRecognitionMethodFactory>? additionalRecognitionMethodFactories)
    {
        this.pluginDirectory = pluginDirectory;
        this.additionalRecognitionMethodFactories = additionalRecognitionMethodFactories?.ToArray() ?? [];
        ReloadPlugins();
    }

    public IReadOnlyList<IFrameSourceFactory> FrameSourceFactories { get; private set; } = [];

    public IReadOnlyList<IImageProcessorFactory> ImageProcessorFactories { get; private set; } = [];

    public IReadOnlyList<IRecognitionMethodFactory> RecognitionMethodFactories { get; private set; } = [];

    public IReadOnlyList<IOcrEngineFactory> OcrEngineFactories { get; private set; } = [];

    public void ReloadPlugins()
    {
        var frameSources = new List<IFrameSourceFactory>
        {
            new ScreenRegionFrameSourceFactory(),
            new CameraFrameSourceFactory(),
            new ImageFileFrameSourceFactory()
        };

        var processors = new List<IImageProcessorFactory>
        {
            new AspectRatioImageProcessorFactory(),
            new CropImageProcessorFactory(),
            new GrayscaleImageProcessorFactory(),
            new ThresholdImageProcessorFactory(),
            new InvertImageProcessorFactory(),
            new ContourImageProcessorFactory()
        };

        var recognizers = new List<IRecognitionMethodFactory>
        {
            new TemplateMatchingRecognitionFactory()
        };
        recognizers.AddRange(additionalRecognitionMethodFactories);

        var ocrEngines = new List<IOcrEngineFactory>
        {
            new NdloCrLiteOcrEngineFactory(),
            new PythonJapanesePaddleOcrEngineFactory(),
            new JapaneseTesseractOcrEngineFactory(),
            new TesseractOcrEngineFactory(),
            new PaddleOcrEngineFactory()
        };

        if (Directory.Exists(pluginDirectory))
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
                .ToDictionary(static assembly => Path.GetFullPath(assembly.Location), StringComparer.OrdinalIgnoreCase);

            foreach (var pluginPath in Directory.EnumerateFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly))
            {
                var fullPath = Path.GetFullPath(pluginPath);
                if (!loadedAssemblies.TryGetValue(fullPath, out var assembly))
                {
                    assembly = Assembly.LoadFrom(fullPath);
                }

                foreach (var type in assembly.GetExportedTypes().Where(static type => type is { IsAbstract: false, IsInterface: false } && type.GetConstructor(Type.EmptyTypes) is not null))
                {
                    var instance = Activator.CreateInstance(type);
                    if (instance is IFrameSourceFactory frameSourceFactory)
                    {
                        frameSources.Add(frameSourceFactory);
                    }

                    if (instance is IImageProcessorFactory imageProcessorFactory)
                    {
                        processors.Add(imageProcessorFactory);
                    }

                    if (instance is IRecognitionMethodFactory recognitionMethodFactory)
                    {
                        recognizers.Add(recognitionMethodFactory);
                    }

                    if (instance is IOcrEngineFactory ocrEngineFactory)
                    {
                        ocrEngines.Add(ocrEngineFactory);
                    }
                }
            }
        }

        FrameSourceFactories = Deduplicate(frameSources);
        ImageProcessorFactories = Deduplicate(processors);
        RecognitionMethodFactories = Deduplicate(recognizers);
        OcrEngineFactories = Deduplicate(ocrEngines);
    }

    private static IReadOnlyList<T> Deduplicate<T>(IEnumerable<T> items) where T : class
    {
        return items
            .Select(static item => (Item: item, Descriptor: item switch
            {
                IFrameSourceFactory source => source.Descriptor,
                IImageProcessorFactory processor => processor.Descriptor,
                IRecognitionMethodFactory recognizer => recognizer.Descriptor,
                IOcrEngineFactory ocr => ocr.Descriptor,
                _ => throw new InvalidOperationException("Unsupported plugin type.")
            }))
            .GroupBy(static item => item.Descriptor.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First().Item)
            .OrderBy(static item => item switch
            {
                IFrameSourceFactory source => source.Descriptor.DisplayName,
                IImageProcessorFactory processor => processor.Descriptor.DisplayName,
                IRecognitionMethodFactory recognizer => recognizer.Descriptor.DisplayName,
                IOcrEngineFactory ocr => ocr.Descriptor.DisplayName,
                _ => string.Empty
            }, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
