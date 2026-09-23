---
applyTo: ".github/workflows/**/*.yml,.github/workflows/**/*.yaml,build/**/*.ps1"
---

Use least-privilege job permissions, pin third-party Actions to commit SHAs, and
fail explicitly on invalid tags, missing Issues, ambiguous SemVer labels, version
collisions, or publication errors. Stable packages may be published only from a
merged release or hotfix PR to main. Develop and release-candidate packages are
artifacts only. Never use `--skip-duplicate` for formal publication.
