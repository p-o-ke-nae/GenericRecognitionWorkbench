# Generic Recognition Workbench

Generic Recognition Workbench は、再利用可能な .NET 10 Windows 向け認識ランタイムおよび WPF ワークベンチです。アプリケーションはソースコードをコピーせず、NuGet パッケージとして利用します。

| パッケージ | 用途 |
|---|---|
| `GenericRecognition.Workbench.Abstractions` | 外部コンポーネント用の契約と共有モデル |
| `GenericRecognition.Workbench.Infrastructure` | 組み込みのキャプチャ、前処理、OCR、認識、永続化、プラグイン読み込み |
| `GenericRecognition.Workbench.Wpf` | 再利用可能な WPF ワークベンチコントロールと ViewModel |

## インストール

3 パッケージのバージョンは揃えてください。拡張機能だけを実装するプロジェクトでは、通常 `Abstractions` のみが必要です。

```powershell
dotnet add package GenericRecognition.Workbench.Abstractions --version <VERSION>
dotnet add package GenericRecognition.Workbench.Infrastructure --version <VERSION>
dotnet add package GenericRecognition.Workbench.Wpf --version <VERSION>
```

正式版パッケージは [nuget.org](https://www.nuget.org/packages?q=GenericRecognition.Workbench) で公開します。`develop` の一時ビルドは、保存期間の短い GitHub Actions アーティファクトとしてのみ提供します。

## WPF ワークベンチのホスト

Window または UserControl にコントロールを追加します。

```xml
<Window
    xmlns:workbench="clr-namespace:Recognition.Wpf;assembly=Recognition.Wpf">
  <workbench:RecognitionWorkbenchControl x:Name="Workbench" />
</Window>
```

組み込みサービスを作成して初期化します。

```csharp
var pluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");
var catalog = new RecognitionPluginCatalog(pluginDirectory);
var runner = new RecognitionRunner(catalog);
var profileStore = new JsonRecognitionProfileStore();
var calibration = new TemplateMatchingProfileCalibrationService();

Workbench.Initialize(catalog, runner, profileStore, calibration);
Workbench.RecognitionEventRaised += (_, result) =>
{
    // 共有された認識結果をアプリケーション固有の処理へ渡します。
};
```

アプリケーション固有のボタン、自動処理、結果処理はホスト側に置いてください。これにより、`RecognitionWorkbenchControl` 内の変更をパッケージ更新によってホストへ反映できます。

## 外部コンポーネントの作成

`GenericRecognition.Workbench.Abstractions` を参照するクラスライブラリを作成し、次のファクトリ契約を 1 つ以上実装します。

- `IFrameSourceFactory`
- `IImageProcessorFactory`
- `IRecognitionMethodFactory`
- `IOcrEngineFactory`

ファクトリは `ComponentDescriptor` を公開し、保存済みのパラメータマップからランタイムコンポーネントを作成します。

```csharp
public sealed class CustomRecognitionFactory : IRecognitionMethodFactory
{
    public ComponentDescriptor Descriptor { get; } = new(
        "example.recognition.custom",
        "カスタム認識",
        "アプリケーション固有の対象を認識します。",
        []);

    public IRecognitionMethod Create(IReadOnlyDictionary<string, string> parameters)
        => new CustomRecognitionMethod();
}
```

安定したグローバルに一意の descriptor ID を使用し、パラメータ名と意味の後方互換性を維持してください。プラグインディレクトリから検出する場合、公開するファクトリ型は具象型とし、public な引数なしコンストラクターを持たせます。コンポーネント DLL とそのプライベート依存関係をホストのプラグインディレクトリへコピーします。ホストは `RecognitionPluginCatalog.ReloadPlugins()` でカタログを再読み込みできます。

ホストからアプリケーション固有の認識ファクトリを直接注入することもできます。

```csharp
var catalog = new RecognitionPluginCatalog(
    pluginDirectory,
    [new CustomRecognitionFactory()]);
```

詳細は[公開・ホスト手順](doc/publish-and-host-guide.md)と、nuget.org に表示される各パッケージの README を参照してください。

## ビルド

```powershell
dotnet restore .\GenericRecognitionWorkbench.slnx
dotnet build .\GenericRecognitionWorkbench.slnx -c Release --no-restore
dotnet test .\GenericRecognitionWorkbench.slnx -c Release --no-build --no-restore
```

ローカルパッケージのバージョンは `0.0.0-local` とし、正式公開には使用しません。

## 開発とリリース

このリポジトリでは Git Flow を使用します。

```text
feature/* -> develop -> release/vX.Y.Z -> main
                       \-> back-merge -> develop
hotfix/*  ---------------------------> main -> develop
```

すべての実装は Issue から開始し、`semver:major`、`semver:minor`、`semver:patch` のいずれか 1 つのラベルを付けます。リリース自動化は未リリースの Issue ラベルを集計し、必要な変更レベルのうち最も大きいものを選択します。`main` へマージされた release PR または hotfix PR だけが、不変のタグを作成して nuget.org へ公開します。

設計およびアーキテクチャ上の決定は、`type:design` ラベルを付けた GitHub Issue に記録します。ブランチ、Issue、バージョン、CI、公開、障害復旧の規則は [CONTRIBUTING.md](CONTRIBUTING.md) を参照してください。

## AI エージェント向け案内

リポジトリ全体の規則は `.github/copilot-instructions.md` と `AGENTS.md` にあります。C#、Actions、文書にはパス別 instruction があります。利用者向け統合スキルは `.github/skills/generic-recognition-workbench-consumer/SKILL.md` にあり、各 GitHub Release にバージョン付き ZIP としても添付します。利用先で使用するには、そのスキルディレクトリを利用先リポジトリの `.github/skills/` へコピーしてください。

## ライセンス

[MIT](LICENSE)
