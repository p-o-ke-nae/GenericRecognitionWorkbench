# コントリビューションガイド

## 正本

設計、決定、要件、実装作業の正本は GitHub Issue です。リポジトリ内の文書には安定した利用方法と運用方法を記載し、設計記録を重複して掲載せず Issue へリンクします。文書とテンプレートの説明文は日本語で記述します。

## Git Flow

| 作業 | 開始元 | Pull Request のマージ先 | ブランチ |
|---|---|---|---|
| 機能、修正、保守 | `develop` | `develop` | `feature/<issue>-<slug>` |
| 安定版リリース | `develop` | `main` | `release/vMAJOR.MINOR.PATCH` |
| 本番 hotfix | `main` | `main` | `hotfix/<issue>-<slug>` |
| release/hotfix のバックマージ | `main` | `develop` | `backmerge/vMAJOR.MINOR.PATCH` |

`main` または `develop` へ直接 push しないでください。feature PR ごとに実装 Issue を 1 つ対応させます。1 つの設計 Issue から複数の実装 Issue を作成しても構いません。

## Issue と Pull Request

1. 設計作業を Design Issue Form に記録します。
2. 受け入れ条件を記載した実装 Issue を作成し、`semver:major`、`semver:minor`、`semver:patch` のいずれか 1 つだけを付けます。
3. `develop` から feature ブランチを作成します。
4. PR 本文に `Issue: #<number>` を記載します。デフォルトブランチ以外を対象とする PR では GitHub の closing keyword によって Issue が閉じられないため、リポジトリのワークフローが検証し、`develop` へのマージ後に Issue を閉じます。
5. 必須チェックがすべて成功してからマージします。

公開 API または永続化データを破壊的に変更する場合は `semver:major`、後方互換性のある機能追加には `semver:minor`、後方互換性のある修正、文書、ビルド、保守の変更には `semver:patch` を使用します。

## ビルドと検証

```powershell
dotnet restore .\GenericRecognitionWorkbench.slnx
dotnet build .\GenericRecognitionWorkbench.slnx -c Release --no-restore
dotnet test .\GenericRecognitionWorkbench.slnx -c Release --no-build --no-restore
dotnet pack .\Recognition.Core\Recognition.Core.csproj -c Release --no-build -o .\artifacts\packages
dotnet pack .\Recognition.Infrastructure\Recognition.Infrastructure.csproj -c Release --no-build -o .\artifacts\packages
dotnet pack .\Recognition.Wpf\Recognition.Wpf.csproj -c Release --no-build -o .\artifacts\packages
```

ローカルビルドでは `0.0.0-local` を使用します。このバージョンは公開しないでください。

## develop パッケージ

`develop` へマージするたびに、一意な `MAJOR.MINOR.PATCH-dev.RUN.SHORTSHA` のプレリリースをビルドします。これは GitHub Actions アーティファクトとしてのみアップロードされます。ワークフロー実行からダウンロードし、展開先ディレクトリを一時的なローカル NuGet ソースとして追加してください。develop パッケージは永続的な成果物ではなく、正式な依存関係には使用できません。

## リリース

対象範囲を記載した Release Issue を作成し、その Issue 番号を指定して **Start release** ワークフローを実行します。ワークフローは、未リリースの実装 Issue に付いた SemVer ラベルのうち最も大きい変更レベルから次のバージョンを決定し、`main` に対する `release/vMAJOR.MINOR.PATCH` PR を作成します。

release PR をマージすると、不変のタグを作成し、Trusted Publishing によって全パッケージを nuget.org へ公開し、GitHub Release と `develop` へのバックマージ PR を作成します。公開済みタグは移動または再利用しないでください。失敗した、または誤ったリリースは、新しい patch Issue と新しいバージョンで修正します。

Hotfix は `main` から開始し、同じ Issue と SemVer の規則を適用します。`main` へのマージ後に公開し、`develop` へバックマージします。

## 必須のリポジトリ設定

- `main` と `develop` を Pull Request、および必須の `policy` と `build-test-pack` チェックで保護します。
- force push とブランチ削除を禁止します。
- `release` GitHub Environment を設定します。
- `publish.yml`、`release` Environment、3 つのパッケージ ID に対して nuget.org Trusted Publishing を設定します。
- `release` Environment の `NUGET_USER` に nuget.org のプロファイル名を設定します。
- Issue Form を有効にする前に、`semver:major`、`semver:minor`、`semver:patch`、`type:design`、`type:release` ラベルを作成します。

初回のリポジトリ構築だけは通常の Git Flow の例外です。履歴が共通でない MIT ライセンスをマージし、`main` を push してから `develop` を作成・push し、ruleset を有効にする前に両方の長期ブランチへワークフローを導入します。この直接構築は Issue に記録します。それ以降の変更は、すべて保護された PR フローに従います。
