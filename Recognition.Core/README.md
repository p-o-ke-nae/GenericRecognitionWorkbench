# GenericRecognition.Workbench.Abstractions

Contracts and shared models for Generic Recognition Workbench extensions.
This package has no dependency on WPF or the built-in recognition runtime.

## Create an extension

Implement one or more factory interfaces:

- `IFrameSourceFactory`
- `IImageProcessorFactory`
- `IRecognitionMethodFactory`
- `IOcrEngineFactory`

Each factory exposes a stable, globally unique `ComponentDescriptor.Id`, describes
its parameters, and creates the runtime component from the supplied parameter map.

```csharp
public sealed class CustomRecognitionFactory : IRecognitionMethodFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "example.recognition.custom",
        "Custom Recognition",
        "Recognizes an application-specific target.",
        []);

    public IRecognitionMethod Create(IReadOnlyDictionary<string, string> parameters)
        => new CustomRecognitionMethod();
}
```

Reference the same major version of `Abstractions` used by the host. Place the
extension assembly in the host's plugin directory; exported factory types must be
non-abstract and have a public parameterless constructor. A host may also inject
recognition factories directly when constructing `RecognitionPluginCatalog`.

Project documentation: https://github.com/p-o-ke-nae/GenericRecognitionWorkbench
