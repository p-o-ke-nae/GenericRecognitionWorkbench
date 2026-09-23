# Agent guidance

Read `.github/copilot-instructions.md`, `README.md`, and `CONTRIBUTING.md`
before changing this repository.

- Start all implementation from a GitHub Issue with exactly one SemVer label.
- Follow the documented Git Flow; never commit feature work directly to
  `main` or `develop`.
- Keep `Recognition.Core` free of WPF and infrastructure dependencies.
- Treat public contracts and persisted profile formats as compatibility
  surfaces.
- Derive release versions from Git tags and workflows; never hard-code a formal
  package version.
- Run restore, Release build, and tests before proposing a merge.
- Do not commit credentials, NuGet keys, generated packages, or local profiles.
- Keep designs and decisions in Issues; keep repository docs focused on stable
  usage and operations.
