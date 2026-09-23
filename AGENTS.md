# エージェント向けガイド

このリポジトリを変更する前に、`.github/copilot-instructions.md`、`README.md`、`CONTRIBUTING.md` を読んでください。

- すべての実装を、SemVer ラベルが 1 つだけ付いた GitHub Issue から開始します。
- 文書化された Git Flow に従い、feature 作業を `main` または `develop` へ直接 commit しません。
- `Recognition.Core` に WPF または Infrastructure の依存関係を追加しません。
- 公開契約と永続化プロファイル形式を互換性維持の対象として扱います。
- リリースバージョンを Git タグとワークフローから決定し、正式版のパッケージバージョンをハードコードしません。
- マージを提案する前に restore、Release ビルド、テストを実行します。
- 資格情報、NuGet キー、生成済みパッケージ、ローカルプロファイルを commit しません。
- 設計と決定は Issue に記録し、リポジトリ文書には安定した利用方法と運用方法だけを記載します。
- リポジトリ内の文書、Issue/PR テンプレート、利用者向けスキルの説明文は日本語で記述します。コマンド、API 名、識別子は正確な表記を維持します。
