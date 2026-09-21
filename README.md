# Generic Recognition Workbench

Reusable projects extracted from AutoCountTool:

- `Recognition.Core` - plugin interfaces and shared models
- `Recognition.Infrastructure` - built-in OCR, preprocessing, and runtime implementations
- `Recognition.Wpf` - reusable WPF workbench UI

## Package versions

Package metadata and the developer default are centralized in
`Directory.Build.props`. Local packs use the `0.1.7-local` developer version by
default. That prerelease version is intended for isolated workbench development;
it does **not** satisfy a consumer's exact `0.1.7` Release reference.

Packages are produced in:

- `C:\Users\o_leg\source\repos\GenericRecognitionWorkbench\LocalPackages`

Package IDs:

- `GenericRecognition.Workbench.Abstractions`
- `GenericRecognition.Workbench.Infrastructure`
- `GenericRecognition.Workbench.Wpf`

`AutoCountTool` and other applications consume these packages via NuGet instead of direct project references.

公開手順と AutoCountTool 型 GUI の組み込み手順は [doc/publish-and-host-guide.md](doc/publish-and-host-guide.md) を参照してください。

## Building and publishing packages

To populate the local NuGet folder for a consumer that references the formal
`0.1.7` version, override the centralized developer suffix explicitly:

```powershell
dotnet pack .\Recognition.Core\Recognition.Core.csproj -c Release -o .\LocalPackages -p:Version=0.1.7
dotnet pack .\Recognition.Infrastructure\Recognition.Infrastructure.csproj -c Release -o .\LocalPackages -p:Version=0.1.7
dotnet pack .\Recognition.Wpf\Recognition.Wpf.csproj -c Release -o .\LocalPackages -p:Version=0.1.7
```

Omitting `-p:Version=0.1.7` preserves the convenient `0.1.7-local` default for
isolated development only.

Release packages are built by `.github/workflows/packages.yml`. Push a tag in
the form `vMAJOR.MINOR.PATCH` (or a strict SemVer prerelease such as
`v1.2.3-rc.1`) to publish all three packages to this repository's GitHub
Packages registry. Build metadata (`+...`) is intentionally not accepted
because it is not part of NuGet package identity.

The workflow derives the repository URL, owner, and package endpoint from the
GitHub Actions context. It uses the job-scoped `GITHUB_TOKEN`; no package
credential is stored in the repository.

See [doc/publish-and-host-guide.md](doc/publish-and-host-guide.md) for the complete publish and consume workflow.

## Local repository

This directory is initialized as a standalone Git repository so the reusable recognition tool can be versioned independently from the counter application.
