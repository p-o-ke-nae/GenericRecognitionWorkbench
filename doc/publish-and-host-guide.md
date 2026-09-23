# Publishing and hosting guide

## Package selection

| Scenario | Packages |
|---|---|
| External frame source, processor, recognizer, or OCR engine | `GenericRecognition.Workbench.Abstractions` |
| Headless host using built-in implementations | `Abstractions`, `Infrastructure` |
| WPF host using the complete workbench | All three packages |

Use the same version for every Generic Recognition Workbench package in one
application.

## Install from nuget.org

```powershell
dotnet add .\MyHost\MyHost.csproj package GenericRecognition.Workbench.Abstractions --version <VERSION>
dotnet add .\MyHost\MyHost.csproj package GenericRecognition.Workbench.Infrastructure --version <VERSION>
dotnet add .\MyHost\MyHost.csproj package GenericRecognition.Workbench.Wpf --version <VERSION>
```

No authenticated package source is needed for formal releases.

## Initialize a host

```csharp
var pluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");
var catalog = new RecognitionPluginCatalog(pluginDirectory);
var runner = new RecognitionRunner(catalog);
var store = new JsonRecognitionProfileStore();
var calibration = new TemplateMatchingProfileCalibrationService();

Workbench.Initialize(catalog, runner, store, calibration);
```

`RecognitionWorkbenchControl` exposes `RecognitionEventRaised`. Subscribe in
the application layer and bridge the result to application-specific behavior.
Do not add application automation to the shared control.

## Build an external component

1. Create a .NET class library compatible with the host.
2. Reference the same major version of
   `GenericRecognition.Workbench.Abstractions`.
3. Implement a factory contract and the corresponding runtime contract.
4. Give the descriptor a stable ID such as `company.product.component`.
5. Define serializable parameters with stable keys and defaults.
6. Add unit tests for creation, parameter validation, cancellation, disposal,
   and recognition behavior.
7. Pack the component as its own NuGet package or copy its DLL and private
   dependencies into the host plugin directory.

Plugin discovery scans top-level `*.dll` files. Exported factory types must be
concrete and have a public parameterless constructor. The catalog deduplicates
factories by descriptor ID, so IDs must not collide.

Hosts that own the factory instance may inject recognition factories without
plugin discovery:

```csharp
var catalog = new RecognitionPluginCatalog(
    pluginDirectory,
    [new ApplicationSpecificRecognitionFactory()]);
```

## Local package testing

Pack to a disposable directory:

```powershell
$version = "0.0.0-local"
dotnet build .\GenericRecognitionWorkbench.slnx -c Release -p:Version=$version
dotnet pack .\Recognition.Core\Recognition.Core.csproj -c Release --no-build -o .\LocalPackages -p:Version=$version
dotnet pack .\Recognition.Infrastructure\Recognition.Infrastructure.csproj -c Release --no-build -o .\LocalPackages -p:Version=$version
dotnet pack .\Recognition.Wpf\Recognition.Wpf.csproj -c Release --no-build -o .\LocalPackages -p:Version=$version
```

Add `LocalPackages` as a temporary source in the consumer's user NuGet
configuration or a non-committed test configuration. Do not commit
machine-specific absolute paths or credentials.

For an integrated `develop` build, download the package artifact from the
**Develop packages** workflow. Its prerelease version is unique and the artifact
expires after the configured retention period.

## Formal publication

Formal publication is automated:

1. Feature PRs merge to `develop` and their implementation Issues are closed.
2. A Release Issue records intended scope and final checks.
3. **Start release** aggregates unreleased Issue SemVer labels and creates
   `release/vMAJOR.MINOR.PATCH` plus a PR to `main`.
4. The release PR passes policy, build, test, and package checks.
5. Merging it tags the merge commit, exchanges GitHub OIDC for a temporary
   nuget.org key, publishes all packages, creates a GitHub Release, and opens a
   back-merge PR.

The stable tag is the release version's single source of truth. Never move or
reuse a published tag. If publication fails after tag creation, fix the cause
through a new patch Issue and release a new version.

Repository administrators must configure nuget.org Trusted Publishing for:

- owner `p-o-ke-nae`
- repository `GenericRecognitionWorkbench`
- workflow `publish.yml`
- GitHub Environment `release`
- package scopes `GenericRecognition.Workbench.*`

Set the nuget.org profile name as `NUGET_USER` in the `release` Environment.
No long-lived NuGet API key is stored.

## Consumer skill

Each GitHub Release contains
`generic-recognition-workbench-consumer-skill-VERSION.zip`. Extract its skill
directory into `.github/skills/` in the consumer repository, or into a supported
personal skill directory. Skill installation is explicit; NuGet does not modify
the consumer's AI configuration.
