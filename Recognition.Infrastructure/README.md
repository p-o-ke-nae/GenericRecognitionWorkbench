# GenericRecognition.Workbench.Infrastructure

組み込みのフレームソース、画像処理、OCR エンジン、認識手段、プロファイル永続化、プラグイン検出、認識ランタイムを提供します。

```csharp
var pluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");
var catalog = new RecognitionPluginCatalog(pluginDirectory);
var runner = new RecognitionRunner(catalog);
var profileStore = new JsonRecognitionProfileStore();
var calibration = new TemplateMatchingProfileCalibrationService();
```

組み込みランタイムをコピーせず、アプリケーション固有の認識手段を追加できます。

```csharp
var catalog = new RecognitionPluginCatalog(
    pluginDirectory,
    [new CustomRecognitionFactory()]);
```

このパッケージは Windows x64 を対象とし、OCR/OpenCV のネイティブ依存関係を含みます。拡張機能の契約は `GenericRecognition.Workbench.Abstractions` が提供します。

プロジェクト文書: https://github.com/p-o-ke-nae/GenericRecognitionWorkbench
