# Generic Recognition Workbench の指示

このリポジトリは、再利用可能な認識契約、ランタイム実装、UI を 3 つの NuGet パッケージとして提供する .NET 10 Windows/WPF ソリューションです。

## アーキテクチャ

- `Recognition.Core`: 依存関係を抑えた公開契約と共有モデル。`GenericRecognition.Workbench.Abstractions` としてパッケージ化します。
- `Recognition.Infrastructure`: 組み込みの OCR/OpenCV/ランタイム実装とプラグイン検出。
- `Recognition.Wpf`: 再利用可能な WPF コントロールと ViewModel。
- `Recognition.Tests`: 3 プロジェクトを対象とする xUnit テスト。

`Recognition.Core` に WPF または実装の依存関係を追加しないでください。公開インターフェース、`ComponentDescriptor` の ID、パラメータキー、永続化プロファイルは互換性維持の対象として扱います。

## ワークフロー

すべての作業を GitHub Issue から開始します。feature PR は `feature/<issue>-<slug>` を使用し、`develop` を対象として、本文に `Issue: #<number>` を含めます。各実装 Issue には `semver:major`、`semver:minor`、`semver:patch` のいずれか 1 つを必ず付けます。安定版リリースは `release/vMAJOR.MINOR.PATCH` から `main` へ、hotfix は `main` から開始します。`main` または `develop` へ直接 push しないでください。

設計と決定は Issue に記録します。README、パッケージ README、CONTRIBUTING には安定した利用方法とコントリビューション手順を記載します。リポジトリ内の文書、Issue/PR テンプレート、利用者向けスキルの説明文は日本語で記述してください。コマンド、API 名、識別子などは正確性のため原文表記を維持します。

## 検証

次を実行します。

```powershell
dotnet restore .\GenericRecognitionWorkbench.slnx
dotnet build .\GenericRecognitionWorkbench.slnx -c Release --no-restore
dotnet test .\GenericRecognitionWorkbench.slnx -c Release --no-build --no-restore
```

パッケージの動作を変更した場合は、pack 対象の 3 プロジェクトすべてに `dotnet pack` を実行します。ローカルパッケージは `0.0.0-local` とし、正式版のバージョンはリリース自動化と不変の `vMAJOR.MINOR.PATCH` タグだけから決定します。

資格情報、API キー、パッケージ、ビルド成果物、端末固有の NuGet ソースを commit しないでください。
