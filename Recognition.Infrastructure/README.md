# GenericRecognition.Workbench.Infrastructure

Built-in frame sources, image processors, OCR engines, recognition methods,
profile persistence, plugin discovery, and recognition runtime.

```csharp
var pluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");
var catalog = new RecognitionPluginCatalog(pluginDirectory);
var runner = new RecognitionRunner(catalog);
var profileStore = new JsonRecognitionProfileStore();
var calibration = new TemplateMatchingProfileCalibrationService();
```

To add an application-specific recognition method without copying the built-in
runtime:

```csharp
var catalog = new RecognitionPluginCatalog(
    pluginDirectory,
    [new CustomRecognitionFactory()]);
```

This package targets Windows x64 and brings native OCR/OpenCV dependencies.
Extension contracts are provided by
`GenericRecognition.Workbench.Abstractions`.

Project documentation: https://github.com/p-o-ke-nae/GenericRecognitionWorkbench
