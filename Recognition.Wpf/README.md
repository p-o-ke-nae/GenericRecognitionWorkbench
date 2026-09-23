# GenericRecognition.Workbench.Wpf

Reusable WPF workbench UI for Generic Recognition Workbench.

```xml
<Window
    xmlns:workbench="clr-namespace:Recognition.Wpf;assembly=Recognition.Wpf">
  <workbench:RecognitionWorkbenchControl x:Name="Workbench" />
</Window>
```

Initialize the control after creating the infrastructure services:

```csharp
Workbench.Initialize(catalog, runner, profileStore, calibration);
Workbench.RecognitionEventRaised += (_, result) =>
{
    // Bridge recognition events to application-specific behavior.
};
```

Use `GenericRecognition.Workbench.Abstractions` to author external components
and `GenericRecognition.Workbench.Infrastructure` for the built-in runtime.

Project documentation: https://github.com/p-o-ke-nae/GenericRecognitionWorkbench
