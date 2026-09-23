---
name: generic-recognition-workbench-consumer
description: Install Generic Recognition Workbench from NuGet, host its WPF UI, or create and register an external recognition component.
license: MIT
---

# Generic Recognition Workbench consumer integration

## Select packages

- Always reference `GenericRecognition.Workbench.Abstractions` when implementing
  external factories or consuming shared result models.
- Add `GenericRecognition.Workbench.Infrastructure` for built-in frame sources,
  processors, OCR engines, profile storage, plugin loading, and the runner.
- Add `GenericRecognition.Workbench.Wpf` to host the reusable WPF UI.
- Keep all GenericRecognition.Workbench packages on the same version.

```powershell
dotnet add package GenericRecognition.Workbench.Abstractions --version <VERSION>
dotnet add package GenericRecognition.Workbench.Infrastructure --version <VERSION>
dotnet add package GenericRecognition.Workbench.Wpf --version <VERSION>
```

## Host the workbench

1. Add `RecognitionWorkbenchControl` to a WPF Window or UserControl.
2. Create `RecognitionPluginCatalog`, `RecognitionRunner`,
   `JsonRecognitionProfileStore`, and
   `TemplateMatchingProfileCalibrationService`.
3. Call the control's `Initialize` overload with those services.
4. Subscribe to `RecognitionEventRaised` and keep application-specific behavior
   outside the shared control.

## Create an external component

1. Create a class library that references only `Abstractions` where possible.
2. Implement `IFrameSourceFactory`, `IImageProcessorFactory`,
   `IRecognitionMethodFactory`, or `IOcrEngineFactory` and its runtime contract.
3. Use a stable reverse-domain-style `ComponentDescriptor.Id`.
4. Keep parameter keys and semantics backward compatible.
5. Give plugin-discovered factory types a public parameterless constructor.
6. Copy the built DLL and its private dependencies to the host plugin directory,
   or inject an `IRecognitionMethodFactory` into `RecognitionPluginCatalog`.

## Validate

- Restore and build the consumer in Release configuration.
- Start the host and reload plugins.
- Confirm the component appears once, its settings round-trip through a profile,
  and recognition cancellation/errors propagate correctly.
- Confirm the host and extension use compatible Abstractions major versions.

Use only stable nuget.org versions for formal dependencies. Develop artifacts
are temporary test inputs and require an explicit local package source.
