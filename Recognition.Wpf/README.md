# GenericRecognition.Workbench.Wpf

Generic Recognition Workbench の再利用可能な WPF ワークベンチ UI を提供します。

```xml
<Window
    xmlns:workbench="clr-namespace:Recognition.Wpf;assembly=Recognition.Wpf">
  <workbench:RecognitionWorkbenchControl x:Name="Workbench" />
</Window>
```

Infrastructure のサービスを作成してからコントロールを初期化します。

```csharp
Workbench.Initialize(catalog, runner, profileStore, calibration);
Workbench.RecognitionEventRaised += (_, result) =>
{
    // 認識イベントをアプリケーション固有の処理へ渡します。
};
```

外部コンポーネントの作成には `GenericRecognition.Workbench.Abstractions` を、組み込みランタイムには `GenericRecognition.Workbench.Infrastructure` を使用してください。

プロジェクト文書: https://github.com/p-o-ke-nae/GenericRecognitionWorkbench
