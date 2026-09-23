# Generic Recognition Workbench instructions

This is a .NET 10 Windows/WPF solution that packages reusable recognition
contracts, runtime implementations, and UI as three NuGet packages.

## Architecture

- `Recognition.Core`: dependency-light public contracts and shared models,
  packaged as `GenericRecognition.Workbench.Abstractions`.
- `Recognition.Infrastructure`: built-in OCR/OpenCV/runtime implementations and
  plugin discovery.
- `Recognition.Wpf`: reusable WPF control and view model.
- `Recognition.Tests`: xUnit tests spanning the three projects.

Do not introduce WPF or implementation dependencies into `Recognition.Core`.
Treat public interfaces, `ComponentDescriptor` IDs, parameter keys, and persisted
profiles as compatibility surfaces.

## Workflow

All work starts from a GitHub Issue. Feature PRs use
`feature/<issue>-<slug>`, target `develop`, and include `Issue: #<number>`.
Each implementation Issue has exactly one `semver:major`, `semver:minor`, or
`semver:patch` label. Stable releases flow through
`release/vMAJOR.MINOR.PATCH` to `main`; hotfixes start from `main`. Do not push
directly to `main` or `develop`.

Designs and decisions belong in Issues. README, package READMEs, and
CONTRIBUTING document stable consumer and contributor workflows.

## Validation

Run:

```powershell
dotnet restore .\GenericRecognitionWorkbench.slnx
dotnet build .\GenericRecognitionWorkbench.slnx -c Release --no-restore
dotnet test .\GenericRecognitionWorkbench.slnx -c Release --no-build --no-restore
```

Use `dotnet pack` for all three packable projects when package behavior changes.
Local packages are `0.0.0-local`; formal versions come only from release
automation and immutable `vMAJOR.MINOR.PATCH` tags.

Never commit credentials, API keys, packages, build outputs, or machine-specific
NuGet sources.
