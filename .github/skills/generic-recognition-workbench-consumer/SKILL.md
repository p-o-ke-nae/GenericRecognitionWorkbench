---
name: generic-recognition-workbench-consumer
description: Generic Recognition Workbench を NuGet から導入し、WPF UI をホストするか、外部認識コンポーネントを作成・登録します。
license: MIT
---

# Generic Recognition Workbench の利用側統合

## パッケージの選択

- 外部ファクトリを実装する場合、または共有結果モデルを使用する場合は、必ず `GenericRecognition.Workbench.Abstractions` を参照します。
- 組み込みのフレームソース、画像処理、OCR エンジン、プロファイル保存、プラグイン読み込み、ランナーを使用する場合は `GenericRecognition.Workbench.Infrastructure` を追加します。
- 再利用可能な WPF UI をホストする場合は `GenericRecognition.Workbench.Wpf` を追加します。
- GenericRecognition.Workbench の全パッケージを同じバージョンに揃えます。

```powershell
dotnet add package GenericRecognition.Workbench.Abstractions --version <VERSION>
dotnet add package GenericRecognition.Workbench.Infrastructure --version <VERSION>
dotnet add package GenericRecognition.Workbench.Wpf --version <VERSION>
```

## ワークベンチのホスト

1. `RecognitionWorkbenchControl` を WPF の Window または UserControl へ追加します。
2. `RecognitionPluginCatalog`、`RecognitionRunner`、`JsonRecognitionProfileStore`、`TemplateMatchingProfileCalibrationService` を作成します。
3. これらのサービスを渡してコントロールの `Initialize` オーバーロードを呼び出します。
4. `RecognitionEventRaised` を購読し、アプリケーション固有の処理は共有コントロールの外側に置きます。

## 外部コンポーネントの作成

1. 可能な限り `Abstractions` のみを参照するクラスライブラリを作成します。
2. `IFrameSourceFactory`、`IImageProcessorFactory`、`IRecognitionMethodFactory`、`IOcrEngineFactory` のいずれかと、対応するランタイム契約を実装します。
3. reverse domain 形式の安定した `ComponentDescriptor.Id` を使用します。
4. パラメータキーとその意味の後方互換性を維持します。
5. プラグイン検出対象のファクトリ型に public な引数なしコンストラクターを持たせます。
6. ビルドした DLL とプライベート依存関係をホストのプラグインディレクトリへコピーするか、`IRecognitionMethodFactory` を `RecognitionPluginCatalog` へ注入します。

## 検証

- 利用側を restore し、Release 構成でビルドします。
- ホストを起動し、プラグインを再読み込みします。
- コンポーネントが 1 件だけ表示されること、設定をプロファイルへ保存して再読込できること、認識のキャンセルとエラーが正しく伝播することを確認します。
- ホストと拡張機能が互換性のあるメジャーバージョンの Abstractions を使用していることを確認します。

正式な依存関係には、nuget.org の安定版のみを使用します。develop アーティファクトは一時的なテスト入力であり、明示的なローカルパッケージソースが必要です。
