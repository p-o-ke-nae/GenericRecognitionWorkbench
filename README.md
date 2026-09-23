# Generic Recognition Workbench

Generic Recognition Workbench is a reusable .NET 10 Windows recognition runtime
and WPF workbench. Applications consume it through NuGet instead of copying its
source.

| Package | Purpose |
|---|---|
| `GenericRecognition.Workbench.Abstractions` | External component contracts and shared models |
| `GenericRecognition.Workbench.Infrastructure` | Built-in capture, preprocessing, OCR, recognition, persistence, and plugin loading |
| `GenericRecognition.Workbench.Wpf` | Reusable WPF workbench control and view model |

## Install

Keep all three packages on the same version. Extension-only projects normally
need only `Abstractions`.

```powershell
dotnet add package GenericRecognition.Workbench.Abstractions --version <VERSION>
dotnet add package GenericRecognition.Workbench.Infrastructure --version <VERSION>
dotnet add package GenericRecognition.Workbench.Wpf --version <VERSION>
```

Formal packages are published to
[nuget.org](https://www.nuget.org/profiles/p-o-ke-nae). Temporary builds from
`develop` are available only as short-lived GitHub Actions artifacts.

## Host the WPF workbench

Add the control to a Window or UserControl:

```xml
<Window
    xmlns:workbench="clr-namespace:Recognition.Wpf;assembly=Recognition.Wpf">
  <workbench:RecognitionWorkbenchControl x:Name="Workbench" />
</Window>
```

Create the built-in services and initialize it:

```csharp
var pluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");
var catalog = new RecognitionPluginCatalog(pluginDirectory);
var runner = new RecognitionRunner(catalog);
var profileStore = new JsonRecognitionProfileStore();
var calibration = new TemplateMatchingProfileCalibrationService();

Workbench.Initialize(catalog, runner, profileStore, calibration);
Workbench.RecognitionEventRaised += (_, result) =>
{
    // Send the shared result to application-specific behavior.
};
```

Keep application-specific buttons, automation, and result handling in the host.
Changes inside `RecognitionWorkbenchControl` then flow to the host when the
package is upgraded.

## Create an external component

Create a class library that references
`GenericRecognition.Workbench.Abstractions`, then implement one or more factory
contracts:

- `IFrameSourceFactory`
- `IImageProcessorFactory`
- `IRecognitionMethodFactory`
- `IOcrEngineFactory`

Factories expose a `ComponentDescriptor` and create a runtime component from the
saved parameter map:

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

Use a stable, globally unique descriptor ID and keep parameter names and
semantics backward compatible. For discovery from a plugin directory, exported
factory types must be concrete and have a public parameterless constructor.
Copy the component DLL and its private dependencies into the host's plugin
directory. The host can reload the catalog with
`RecognitionPluginCatalog.ReloadPlugins()`.

A host can instead inject an application-specific recognition factory directly:

```csharp
var catalog = new RecognitionPluginCatalog(
    pluginDirectory,
    [new CustomRecognitionFactory()]);
```

See [the complete publishing and hosting guide](doc/publish-and-host-guide.md)
and the package-specific README shown on nuget.org.

## Build

```powershell
dotnet restore .\GenericRecognitionWorkbench.slnx
dotnet build .\GenericRecognitionWorkbench.slnx -c Release --no-restore
dotnet test .\GenericRecognitionWorkbench.slnx -c Release --no-build --no-restore
```

Local packs use `0.0.0-local` and are never formally published.

## Development and releases

This repository uses Git Flow:

```text
feature/* -> develop -> release/vX.Y.Z -> main
                       \-> back-merge -> develop
hotfix/*  ---------------------------> main -> develop
```

Every implementation starts from an Issue and has exactly one
`semver:major`, `semver:minor`, or `semver:patch` label. Release automation
aggregates unreleased Issue labels and selects the highest required increment.
Only a merged release or hotfix PR to `main` creates an immutable tag and
publishes to nuget.org.

Designs and architectural decisions remain in GitHub Issues labeled
`type:design`. See [CONTRIBUTING.md](CONTRIBUTING.md) for branch, Issue, version,
CI, publication, and failure-recovery rules.

## AI agent guidance

Repository-wide rules are in `.github/copilot-instructions.md` and `AGENTS.md`.
Path-specific instructions cover C#, Actions, and documentation. The consumer
integration skill is in
`.github/skills/generic-recognition-workbench-consumer/SKILL.md` and is also
attached as a versioned ZIP to each GitHub Release. Copy that skill directory to
a consumer repository's `.github/skills/` directory to make it available there.

## License

[MIT](LICENSE)
