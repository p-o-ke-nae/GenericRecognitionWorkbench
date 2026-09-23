# 公開・ホスト手順

## パッケージの選択

| 用途 | パッケージ |
|---|---|
| 外部フレームソース、画像処理、認識手段、OCR エンジン | `GenericRecognition.Workbench.Abstractions` |
| 組み込み実装を使用するヘッドレスホスト | `Abstractions`、`Infrastructure` |
| ワークベンチ全体を使用する WPF ホスト | 3 パッケージすべて |

1 つのアプリケーションで使用する Generic Recognition Workbench パッケージは、すべて同じバージョンに揃えてください。

## nuget.org からのインストール

```powershell
dotnet add .\MyHost\MyHost.csproj package GenericRecognition.Workbench.Abstractions --version <VERSION>
dotnet add .\MyHost\MyHost.csproj package GenericRecognition.Workbench.Infrastructure --version <VERSION>
dotnet add .\MyHost\MyHost.csproj package GenericRecognition.Workbench.Wpf --version <VERSION>
```

正式版の利用に、認証が必要なパッケージソースはありません。

## ホストの初期化

```csharp
var pluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");
var catalog = new RecognitionPluginCatalog(pluginDirectory);
var runner = new RecognitionRunner(catalog);
var store = new JsonRecognitionProfileStore();
var calibration = new TemplateMatchingProfileCalibrationService();

Workbench.Initialize(catalog, runner, store, calibration);
```

`RecognitionWorkbenchControl` は `RecognitionEventRaised` を公開します。アプリケーション層で購読し、結果をアプリケーション固有の処理へ渡してください。共有コントロールへアプリケーション固有の自動処理を追加しないでください。

## 外部コンポーネントのビルド

1. ホストと互換性のある .NET クラスライブラリを作成します。
2. ホストと同じメジャーバージョンの `GenericRecognition.Workbench.Abstractions` を参照します。
3. ファクトリ契約と、対応するランタイム契約を実装します。
4. descriptor に `company.product.component` のような安定した ID を設定します。
5. 安定したキーと既定値を持つ、シリアライズ可能なパラメータを定義します。
6. 作成、パラメータ検証、キャンセル、破棄、認識動作の単体テストを追加します。
7. コンポーネントを独自の NuGet パッケージとして pack するか、DLL とプライベート依存関係をホストのプラグインディレクトリへコピーします。

プラグイン検出は、トップレベルの `*.dll` ファイルを走査します。公開するファクトリ型は具象型とし、public な引数なしコンストラクターを持たせてください。カタログは descriptor ID によってファクトリの重複を排除するため、ID が衝突しないようにします。

ファクトリのインスタンスを所有するホストは、プラグイン検出を使わずに認識ファクトリを注入できます。

```csharp
var catalog = new RecognitionPluginCatalog(
    pluginDirectory,
    [new ApplicationSpecificRecognitionFactory()]);
```

## ローカルパッケージのテスト

破棄可能なディレクトリへ pack します。

```powershell
$version = "0.0.0-local"
dotnet build .\GenericRecognitionWorkbench.slnx -c Release -p:Version=$version
dotnet pack .\Recognition.Core\Recognition.Core.csproj -c Release --no-build -o .\LocalPackages -p:Version=$version
dotnet pack .\Recognition.Infrastructure\Recognition.Infrastructure.csproj -c Release --no-build -o .\LocalPackages -p:Version=$version
dotnet pack .\Recognition.Wpf\Recognition.Wpf.csproj -c Release --no-build -o .\LocalPackages -p:Version=$version
```

`LocalPackages` を利用側ユーザーの NuGet 設定、または commit しないテスト用設定へ一時的なソースとして追加します。端末固有の絶対パスや資格情報を commit しないでください。

統合済みの `develop` ビルドを使用する場合は、**Develop packages** ワークフローからパッケージアーティファクトをダウンロードします。プレリリースバージョンは一意で、設定された保存期間を過ぎるとアーティファクトは削除されます。

## 正式公開

正式公開は次の手順で自動化されています。

1. feature PR を `develop` へマージし、対応する実装 Issue を閉じます。
2. Release Issue に対象範囲と最終確認を記録します。
3. **Start release** が未リリース Issue の SemVer ラベルを集計し、`release/vMAJOR.MINOR.PATCH` と `main` に対する PR を作成します。
4. release PR で policy、build、test、package のチェックを実行します。
5. マージすると、マージコミットへタグを付け、GitHub OIDC を一時的な nuget.org キーへ交換し、全パッケージを公開して、GitHub Release とバックマージ PR を作成します。

安定版タグがリリースバージョンの唯一の正本です。公開済みタグは移動または再利用しないでください。タグ作成後に公開が失敗した場合は、新しい patch Issue で原因を修正し、新しいバージョンをリリースします。

リポジトリ管理者は、nuget.org Trusted Publishing を次の設定で構成します。

- owner: `p-o-ke-nae`
- repository: `GenericRecognitionWorkbench`
- workflow: `publish.yml`
- GitHub Environment: `release`
- package scopes: `GenericRecognition.Workbench.*`

`release` Environment の `NUGET_USER` に nuget.org のプロファイル名を設定します。長期間有効な NuGet API キーは保存しません。

## 利用者向けスキル

各 GitHub Release には `generic-recognition-workbench-consumer-skill-VERSION.zip` が含まれます。スキルディレクトリを利用先リポジトリの `.github/skills/`、または対応する個人用スキルディレクトリへ展開します。スキルのインストールは明示的に行う必要があり、NuGet が利用先の AI 設定を変更することはありません。
