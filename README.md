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

### GitHub Actions ワークフロー

| ワークフロー | 起動条件 | 入力 | 主な処理と成果物 |
|---|---|---|---|
| **CI** | `main` または `develop` 向け PR。自動作成 PR では手動 dispatch も使用 | 手動実行時のみ`pr_number` | Issue・ブランチ規約を検証する`policy`と、restore/build/test/packを行う`build-test-pack` |
| **Close implemented Issue** | `feature/*`から`develop`へのPRがマージされたとき | なし | PR本文の`Issue: #番号`へ実装コミットを記録し、Issueをclose |
| **Develop packages** | `develop`へのpush | なし | `X.Y.Z-dev.RUN.SHORTSHA`で3パッケージを作成し、14日間保持するActions artifactとして保存 |
| **Start release** | Actions画面または`gh workflow run`から手動実行 | `release_issue` | 未リリースIssueを集計して次版を決定し、`release/vX.Y.Z`と`main`向けPRを作成 |
| **Publish stable release** | `release/v*`または`hotfix/*`から`main`へのPRがマージされたとき | なし | tag、nuget.org公開、GitHub Release、consumer skill ZIP、back-merge PRを作成 |

通常はワークフローを手動で再実行せず、原因を修正した新しいコミットで再検証します。自動作成されたrelease/back-merge PRのCIだけは、作成元ワークフローが`pr_number`を指定して自動dispatchします。

### 通常の開発手順

1. GitHub Issueを作成し、受け入れ条件と関連する設計Issueを記載します。
2. 変更内容に応じて`semver:major`、`semver:minor`、`semver:patch`のいずれか1つだけを付けます。
3. 最新の`develop`から`feature/<issue>-<slug>`を作成します。

```powershell
git switch develop
git pull --ff-only
git switch -c feature/<ISSUE-NUMBER>-<SLUG>
```

4. 実装・検証してcommitし、feature branchをpushします。

```powershell
dotnet restore .\GenericRecognitionWorkbench.slnx
dotnet build .\GenericRecognitionWorkbench.slnx -c Release --no-restore
dotnet test .\GenericRecognitionWorkbench.slnx -c Release --no-build --no-restore

git add --all
git commit -m "<変更内容>"
git push --set-upstream origin feature/<ISSUE-NUMBER>-<SLUG>
```

5. `develop`向けPRを作成します。PR本文には`Issue: #<ISSUE-NUMBER>`を正確に1つ記載します。

```powershell
gh pr create `
  --base develop `
  --head feature/<ISSUE-NUMBER>-<SLUG> `
  --title "<PRタイトル>" `
  --body "Issue: #<ISSUE-NUMBER>

## Summary

<変更内容>

## Validation

- [x] Release build succeeds
- [x] Tests succeed

## Compatibility and release impact

<互換性とリリースへの影響>"
```

6. `policy`と`build-test-pack`が成功したことを確認し、merge commit方式でマージします。

```powershell
gh pr checks <PR-NUMBER> --watch
gh pr merge <PR-NUMBER> --merge --delete-branch
```

マージ後、対応Issueは自動でcloseされ、**Develop packages**がdevelop版パッケージを作成します。

### developパッケージの確認

develop版は統合テスト専用で、nuget.orgやGitHub Packagesには公開しません。Actions画面の最新の**Develop packages**成功runから`packages-X.Y.Z-dev.RUN.SHORTSHA`をダウンロードするか、GitHub CLIを使用します。

```powershell
gh run list --workflow "Develop packages" --branch develop --limit 5
gh run download <RUN-ID> `
  --name packages-<VERSION> `
  --dir .\artifacts\develop
```

展開先をconsumerの一時NuGet sourceへ追加して確認します。

```powershell
dotnet nuget add source "<展開先>" --name GenericRecognitionDevelop
dotnet restore .\Consumer.slnx --no-cache
dotnet nuget remove source GenericRecognitionDevelop
```

develop artifactは14日後に削除されます。正式な依存関係、Release build、長期保存には使用しないでください。

### 初回公開前の一度限りの設定

リポジトリ管理者は、nuget.orgの**Trusted Publishing**で次のpolicyを登録します。

| 項目 | 値 |
|---|---|
| Repository owner | `p-o-ke-nae` |
| Repository | `GenericRecognitionWorkbench` |
| Workflow file | `publish.yml` |
| Environment | `release` |
| Package scope | `GenericRecognition.Workbench.*` |

GitHubの`release` Environmentには、nuget.orgのプロフィール名を`NUGET_USER`として登録します。メールアドレスや長期間有効なNuGet API keyは登録しません。`publish.yml`はGitHub OIDC tokenをnuget.orgの有効期間が短いAPI keyへ交換します。

### 正式リリース手順

1. リリース対象のfeature PRがすべて`develop`へマージ済みで、最新のdevelop artifactを確認済みであることを確認します。
2. Release Issue FormからIssueを作成し、対象Issueと最終確認結果を記載します。`type:release`ラベルが付いていることを確認します。
3. GitHubの**Actions → Start release → Run workflow**を開き、branchに`main`、`release_issue`にRelease Issue番号を指定します。CLIでも実行できます。

```powershell
gh workflow run "Start release" `
  --ref main `
  -f release_issue=<RELEASE-ISSUE-NUMBER>
```

4. **Start release**が成功すると、SemVerラベルの最大値から版を計算し、`release/vX.Y.Z`と`main`向けrelease PRを作成します。PR本文の`Release-Issue`、`Included Issues`、計算された版を確認します。
5. release PRの`policy`と`build-test-pack`を確認します。CIがアップロードする`X.Y.Z-rc.RUN.SHORTSHA` artifactを必要に応じてconsumerで最終確認します。
6. 問題がなければrelease PRをmerge commit方式で`main`へマージします。これが正式公開の開始操作です。

```powershell
gh pr checks <RELEASE-PR-NUMBER> --watch
gh pr merge <RELEASE-PR-NUMBER> --merge
```

7. **Publish stable release**で、build/test/pack、tag作成、OIDC login、nuget.org push、GitHub Release作成、back-merge PR作成がすべて成功したことを確認します。
8. nuget.org上で3パッケージの版、README、MITライセンス、repository URL、依存関係、symbolsを確認します。GitHub Releaseには`.nupkg`、`.snupkg`、`generic-recognition-workbench-consumer-skill-VERSION.zip`が添付されます。
9. 自動作成された`backmerge/vX.Y.Z`から`develop`へのPRで必須checkを確認し、merge commit方式でマージします。

```powershell
git fetch origin --tags
git show vX.Y.Z --no-patch
gh release view vX.Y.Z
```

### hotfix手順

1. 修正Issueを作成してSemVerラベルを1つ付けます。
2. 最新の`main`から`hotfix/<issue>-<slug>`を作成します。
3. `main`向けPRを作成し、本文へ`Issue: #<number>`を記載します。
4. 必須check成功後にmerge commit方式でマージします。
5. **Publish stable release**の成功とnuget.org/GitHub Releaseを確認します。
6. 自動作成されたback-merge PRを`develop`へマージし、hotfix Issueへ公開版を記録してcloseします。

### 失敗時の復旧

- PRの検証失敗は、失敗したstepのログを確認し、同じbranchへ修正commitをpushします。
- `Start release`が失敗した場合は、Release Issue、未リリースIssueのSemVerラベル、既存release branchを確認してから再実行します。
- tag作成前のrelease PRは修正できます。release branchでは新機能を追加せず、安定化修正だけを行います。
- tagを作成した後にnuget.org公開やGitHub Release作成が失敗しても、tagを移動・削除・再利用しません。原因を記録した新しい`semver:patch` Issueを作り、新しいpatch版で復旧します。
- 公開済みパッケージを上書きせず、`--skip-duplicate`で不整合を成功扱いにしません。
- 不完全なGitHub Releaseができた場合は、成果物を確認してdraft化または削除し、Issueへ原因と復旧版を記録します。

consumerへの組込み、外部コンポーネント作成、ローカルpack、Trusted Publishingの詳細は[公開・ホスト手順](doc/publish-and-host-guide.md)を参照してください。

## AI エージェント向け案内

リポジトリ全体の規則は `.github/copilot-instructions.md` と `AGENTS.md` にあります。C#、Actions、文書にはパス別 instruction があります。利用者向け統合スキルは `.github/skills/generic-recognition-workbench-consumer/SKILL.md` にあり、各 GitHub Release にバージョン付き ZIP としても添付します。利用先で使用するには、そのスキルディレクトリを利用先リポジトリの `.github/skills/` へコピーしてください。

## ライセンス

[MIT](LICENSE)
