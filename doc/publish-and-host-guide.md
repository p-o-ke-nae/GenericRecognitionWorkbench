# 汎用型自動認識ツールの公開とホスト作成手順

## 概要

このガイドは、共有の汎用型自動認識ツールをローカル NuGet へ公開する手順と、AutoCountTool のようなツール特化 GUI から組み込む最短手順をまとめる。

| 項目 | 内容 |
|---|---|
| 共有ソースの正本 | `C:\Users\o_leg\source\repos\GenericRecognitionWorkbench` |
| ローカル NuGet 出力先 | `C:\Users\o_leg\source\repos\GenericRecognitionWorkbench\LocalPackages` |
| 正式な利用版 | `0.1.16` |
| 単体開発用の既定版 | `0.1.16-local` (`Directory.Build.props` で管理) |
| AutoCountTool 側の利用方法 | `AutoCountTool\AutoCountTool.csproj` の `PackageReference` |

## 1. ローカル NuGet へ公開する

正式版 `0.1.16` を参照する利用側へ供給する場合は、リポジトリルートで
`Version` を明示して次を実行する。

```powershell
dotnet pack .\Recognition.Core\Recognition.Core.csproj -c Release -o .\LocalPackages -p:Version=0.1.16
dotnet pack .\Recognition.Infrastructure\Recognition.Infrastructure.csproj -c Release -o .\LocalPackages -p:Version=0.1.16
dotnet pack .\Recognition.Wpf\Recognition.Wpf.csproj -c Release -o .\LocalPackages -p:Version=0.1.16
```

`-p:Version=0.1.16` を省略すると `Directory.Build.props` の
`VersionPrefix=0.1.16` と `VersionSuffix=local` により
`0.1.16-local` が生成される。これは共有パッケージ単体の開発には便利だが、
正式版 `0.1.16` を完全一致で参照する Release ビルドの復元には使用できない。

公開後は利用側プロジェクトで復元・ビルドする。

```powershell
dotnet restore ..\AutoCountTool\AutoCountTool\AutoCountTool.csproj --no-cache
dotnet build ..\AutoCountTool\AutoCountTool\AutoCountTool.csproj -c Debug
```

## 2. 他リポジトリから参照する

対象リポジトリの `NuGet.Config` にローカル feed を追加する。

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="LocalPackages" value="..\GenericRecognitionWorkbench\LocalPackages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

`csproj` には共有パッケージを追加する。

```xml
<ItemGroup>
  <PackageReference Include="GenericRecognition.Workbench.Abstractions" Version="[0.1.16]" />
  <PackageReference Include="GenericRecognition.Workbench.Infrastructure" Version="[0.1.16]" />
  <PackageReference Include="GenericRecognition.Workbench.Wpf" Version="[0.1.16]" />
</ItemGroup>
```

将来の公開版を利用する場合は、`0.1.16` をリリースタグに対応する版
（例: `1.2.3`）へ置き換える。

## 3. GitHub Packages へ公開する

`.github/workflows/packages.yml` は push / pull request ごとに三つの
プロジェクトを restore、Release build、test、pack する。タグが厳密な
`vMAJOR.MINOR.PATCH` または SemVer prerelease
（例: `v1.2.3-rc.1`）の場合だけ、同じ版の三パッケージを GitHub
Packages へ公開する。NuGet のパッケージ識別では build metadata が
使われないため、`+metadata` を含むタグは受け付けない。

公開先 URL と所有者は `github.repository` /
`github.repository_owner` から取得し、認証には権限を
`packages: write` に限定したジョブの `GITHUB_TOKEN` を使う。リポジトリへ
PAT や API key を保存する必要はない。

利用側では `<OWNER>` を GitHub のユーザー名または Organization 名へ
置き換えて package source を登録する。

```powershell
dotnet nuget add source "https://nuget.pkg.github.com/<OWNER>/index.json" `
  --name github `
  --username <GITHUB-USER> `
  --password <TOKEN>
```

GitHub Actions から同一リポジトリのパッケージを復元する場合は
`GITHUB_TOKEN` と `packages: read` を使用できる。別リポジトリや開発者
PC からの復元には、パッケージへのアクセス権と `read:packages` を持つ
トークンが必要になる。この source を含むユーザー設定はコミットしない。
パッケージの可視性と接続リポジトリ設定は GitHub 上で管理する。

## 4. AutoCountTool のような GUI を作る

### 最小構成

1. 共有パッケージを参照する。
2. `RecognitionWorkbenchHost.CreateDefault()` 相当の初期化を行う。
3. `RecognitionWorkbenchControl` をホストする WPF Window/UserControl を作る。
4. 必要なら `RecognitionEventRaised` を購読して、認識結果をツール固有ロジックへ橋渡しする。

用途特化の認識方式は汎用パッケージへ直接追加せず、ホスト側から factory を注入する。

```csharp
var catalog = new RecognitionPluginCatalog(
    pluginDirectory,
    [new ApplicationSpecificRecognitionFactory()]);
```

注入する factory は `IRecognitionMethodFactory` を実装する。これにより、汎用の
テンプレートマッチング等と同じ認識方式選択欄へ表示される。

### AutoCountTool の現行構成

| 役割 | 実装 |
|---|---|
| 共有コントロールのホスト | `AutoCountTool\RecognitionWorkbenchWindow.xaml` |
| 共有 ViewModel/ランナーの生成 | `AutoCountTool\Startup\RecognitionWorkbenchHost.cs` |
| 認識イベントの連携 | `AutoCountTool\RecognitionWorkbenchWindow.xaml.cs` |

## 5. どこまで自動追従するか

`RecognitionWorkbenchControl` 自体に追加したボタン、文言、テンプレート編集機能、保存ロジックは、そのコントロールを直接埋め込んでいる GUI なら原則自動追従する。

一方で、次のものは自動追従しない。

- 共有コントロールの外側に置いたメニュー、補助パネル、独自ボタン
- 共有イベントを受け取った後のツール固有処理
- 共有コントロールを使わず独自 XAML で作り直した画面

## 6. 今回の変更で見るべき場所

- プロファイル保存/新規作成: `Recognition.Wpf\ViewModels\RecognitionWorkbenchViewModel.cs`
- テンプレート編集 UI: `Recognition.Wpf\Windows\ImageCropSelectionWindow.cs`
- 共有画面レイアウト: `Recognition.Wpf\RecognitionWorkbenchControl.xaml`
- AutoCountTool 側の追従確認: `AutoCountTool\RecognitionWorkbenchWindow.xaml`

## 7. フレーム履歴を使ったテンプレート作成

認識プロファイルには、取り込んだフレームを保持する秒数を指定できる。
動画ファイル、カメラ、画面キャプチャなど入力方法に依存せず、認識ループが
保持する同じ履歴を参照する。ワークベンチの履歴スライダーまたは前後ボタンで
対象フレームを選び、生画像と処理後画像を切り替えてテンプレート作成、
トリミング、OCR 領域指定、**選択履歴フレームをテスト** などを実行する。

過去フレームを前処理から再評価したい場合は、`RetainSourceFramesInHistory` を
有効にして生画像履歴を保持する。無効時は最新フレームだけが生画像を再利用でき、
古い履歴フレームでは案内メッセージを表示して停止する。

履歴はメモリ上で保持され、設定秒数を超えた古いフレームは破棄される。高解像度・
高 FPS で長時間保持する場合は、メモリ使用量を考慮して保持秒数を調整する。

認識サイクルログまたは認識イベントログを選択すると、同じ判定サイクルの履歴
フレームへ移動する。履歴スライダーや前後ボタンから選択した場合も、保持されて
いる対応ログが選択される。ログだけが残り履歴が期限切れの場合は、別フレームへ
誤移動せず、対応フレームが期限切れである旨を表示する。
