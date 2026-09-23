# Contributing

## Source of truth

GitHub Issues are the source of truth for designs, decisions, requirements, and
implementation work. Repository documents describe stable usage and operations;
they link to Issues instead of duplicating design records.

## Git Flow

| Work | Start | Pull request target | Branch |
|---|---|---|---|
| Feature, fix, maintenance | `develop` | `develop` | `feature/<issue>-<slug>` |
| Stable release | `develop` | `main` | `release/vMAJOR.MINOR.PATCH` |
| Production hotfix | `main` | `main` | `hotfix/<issue>-<slug>` |
| Release/hotfix back-merge | `main` | `develop` | `backmerge/vMAJOR.MINOR.PATCH` |

Do not push directly to `main` or `develop`. Keep one implementation Issue per
feature PR. A design Issue may produce multiple implementation Issues.

## Issues and pull requests

1. Record design work with the Design issue form.
2. Create an implementation Issue with acceptance criteria and exactly one of
   `semver:major`, `semver:minor`, or `semver:patch`.
3. Create a feature branch from `develop`.
4. Put `Issue: #<number>` in the PR body. GitHub closing keywords do not close
   Issues for PRs targeting a non-default branch, so the repository workflow
   validates and closes the Issue after merge to `develop`.
5. Merge only after all required checks pass.

Use `semver:major` for breaking public API or persisted-data changes,
`semver:minor` for backward-compatible features, and `semver:patch` for
backward-compatible fixes, documentation, build, and maintenance changes.

## Build and validation

```powershell
dotnet restore .\GenericRecognitionWorkbench.slnx
dotnet build .\GenericRecognitionWorkbench.slnx -c Release --no-restore
dotnet test .\GenericRecognitionWorkbench.slnx -c Release --no-build --no-restore
dotnet pack .\Recognition.Core\Recognition.Core.csproj -c Release --no-build -o .\artifacts\packages
dotnet pack .\Recognition.Infrastructure\Recognition.Infrastructure.csproj -c Release --no-build -o .\artifacts\packages
dotnet pack .\Recognition.Wpf\Recognition.Wpf.csproj -c Release --no-build -o .\artifacts\packages
```

Local builds use `0.0.0-local`. Never publish that version.

## Develop packages

Every merge to `develop` builds a unique
`MAJOR.MINOR.PATCH-dev.RUN.SHORTSHA` prerelease. It is uploaded only as a
GitHub Actions artifact. Download it from the workflow run and add its extracted
directory as a temporary local NuGet source. Develop packages are not durable
and must not be used as a formal dependency.

## Releases

Create a Release issue listing the intended scope, then run the **Start release**
workflow with that Issue number. The workflow derives the next version from the
highest SemVer label among unreleased implementation Issues and opens a
`release/vMAJOR.MINOR.PATCH` PR to `main`.

Merging the release PR creates the immutable tag, publishes all packages to
nuget.org through Trusted Publishing, creates a GitHub Release, and opens a
back-merge PR to `develop`. Never move or reuse a published tag. Fix a failed or
incorrect release with a new patch Issue and version.

Hotfixes start from `main`, use the same Issue and SemVer rules, publish after
merge to `main`, and are back-merged to `develop`.

## Required repository settings

- Protect `main` and `develop` with pull requests and required `policy` and
  `build-test-pack` checks.
- Block force pushes and branch deletion.
- Configure the `release` GitHub Environment.
- Configure nuget.org Trusted Publishing for `publish.yml`, the `release`
  environment, and the three package IDs.
- Set `NUGET_USER` in the `release` environment to the nuget.org profile name.
- Create the labels `semver:major`, `semver:minor`, `semver:patch`,
  `type:design`, and `type:release` before enabling Issue Forms.

The initial repository bootstrap is the only exception to normal Git Flow:
merge the unrelated MIT license history, push `main`, create and push `develop`,
and install these workflows on both long-lived branches before enabling their
rulesets. Record that direct bootstrap in its Issue. All later changes follow
the protected PR flow.
