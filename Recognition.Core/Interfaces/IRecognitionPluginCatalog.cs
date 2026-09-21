namespace Recognition.Core;

public interface IRecognitionPluginCatalog
{
    IReadOnlyList<IFrameSourceFactory> FrameSourceFactories { get; }

    IReadOnlyList<IImageProcessorFactory> ImageProcessorFactories { get; }

    IReadOnlyList<IRecognitionMethodFactory> RecognitionMethodFactories { get; }

    IReadOnlyList<IOcrEngineFactory> OcrEngineFactories { get; }

    void ReloadPlugins();
}
