---
applyTo: "**/*.cs,**/*.csproj,Directory.Build.*"
---

Preserve nullable type safety and existing async/cancellation patterns. Keep
public extension contracts in Recognition.Core and implementation details in
Infrastructure or Wpf. Add or update xUnit tests for behavior changes. Public API
breaks require a `semver:major` Issue; backward-compatible API additions require
`semver:minor`.
