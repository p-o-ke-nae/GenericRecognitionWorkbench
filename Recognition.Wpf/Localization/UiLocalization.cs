using Recognition.Core;

namespace Recognition.Wpf;

public sealed class UiLocalization : ObservableObject
{
    private static readonly IReadOnlyDictionary<string, (string Ja, string En)> Texts = new Dictionary<string, (string Ja, string En)>(StringComparer.OrdinalIgnoreCase)
    {
        ["Profile"] = ("プロファイル", "Profile"),
        ["ProfileName"] = ("プロファイル名", "Profile Name"),
        ["ProfileList"] = ("保存済みプロファイル", "Saved Profiles"),
        ["ReloadProfileList"] = ("一覧更新", "Refresh List"),
        ["RecognitionFps"] = ("認識 FPS", "Recognition FPS"),
        ["CaptureScale"] = ("キャプチャ倍率", "Capture Scale"),
        ["FrameHistoryRetention"] = ("履歴保持秒数", "History Retention (seconds)"),
        ["RetainSourceFramesInHistory"] = ("履歴に生画像も保持する（メモリ使用量増加）", "Retain raw history frames (uses more memory)"),
        ["SourceHistoryUnavailable"] = ("生画像履歴がオフのため、この過去フレームの生画像は使用できません。最新フレームを選択するか、生画像履歴を有効にして再取得してください。", "Raw image history is off for this older frame. Select the latest frame, or enable raw history and capture again."),
        ["PreviewMode"] = ("プレビュー表示", "Preview Mode"),
        ["EventAction"] = ("イベント処理", "Event Action"),
        ["EventActionFrameOffset"] = ("イベント処理フレームオフセット", "Event Action Frame Offset"),
        ["Language"] = ("表示言語", "Language"),
        ["EventMode"] = ("イベント条件", "Event Mode"),
        ["FrameSource"] = ("キャプチャ", "Frame Source"),
        ["SelectScreenRegion"] = ("画面領域を選択", "Select Screen Region"),
        ["Preprocessors"] = ("前処理", "Preprocessors"),
        ["AddPreprocessor"] = ("前処理を追加", "Add Preprocessor"),
        ["Remove"] = ("削除", "Remove"),
        ["RecognitionMethod"] = ("画像認識", "Recognition Method"),
        ["CreateTemplateFromCapture"] = ("最新キャプチャからテンプレート作成", "Create Template from Latest Capture"),
        ["CreateTemplateFromRawCapture"] = ("生画像からテンプレート作成", "Create Template from Raw Capture"),
        ["CreateTemplateFromProcessedCapture"] = ("画像処理後からテンプレート作成", "Create Template from Processed Capture"),
        ["Ocr"] = ("OCR", "OCR"),
        ["ImageRecognition"] = ("情報抽出用画像認識", "Image Recognition Extractors"),
        ["AddImageRecognitionTarget"] = ("画像認識ターゲットを追加", "Add Image Recognition Target"),
        ["ImageRecognitionTargetDefaultName"] = ("画像認識", "Image Recognition"),
        ["EnableOcr"] = ("OCR を有効化", "Enable OCR"),
        ["OcrReferences"] = ("OCR 基準点", "OCR Reference Points"),
        ["OcrTargets"] = ("OCR 箇所", "OCR Targets"),
        ["AddOcrReference"] = ("OCR 基準点を追加", "Add OCR Reference Point"),
        ["AddOcrTarget"] = ("OCR 箇所を追加", "Add OCR Target"),
        ["OcrTargetName"] = ("OCR 名", "OCR Name"),
        ["SelectOcrRegion"] = ("OCR 範囲を選択", "Select OCR Region"),
        ["UseRecognitionAnchor"] = ("認識基準点からの相対位置", "Position relative to detected anchor"),
        ["UseRecognitionAnchorHint"] = ("テンプレート一致領域の左上を基準に OCR 範囲を保持します。", "Keep the OCR area relative to the top-left of the detected template region."),
        ["OcrReferenceDefaultName"] = ("OCR 基準点", "OCR Reference"),
        ["OcrReferenceTemplate"] = ("OCR 基準点テンプレート", "OCR Reference Template"),
        ["OcrReferenceThreshold"] = ("OCR 基準点閾値", "OCR Reference Threshold"),
        ["OcrTargetDefaultName"] = ("OCR", "OCR"),
        ["Actions"] = ("操作", "Actions"),
        ["ReloadPlugins"] = ("プラグイン再読込", "Reload Plugins"),
        ["LoadProfile"] = ("プロファイル読込", "Load Profile"),
        ["SaveProfile"] = ("プロファイル保存", "Save Profile"),
        ["CreateNewProfile"] = ("新規作成", "Create New Profile"),
        ["AutoCalibrateProfile"] = ("環境向けに自動調整して保存", "Auto Calibrate and Save"),
        ["TestOnce"] = ("単発テスト", "Test Once"),
        ["TestSelectedHistoryFrame"] = ("選択履歴フレームをテスト", "Test Selected History Frame"),
        ["Start"] = ("開始", "Start"),
        ["Stop"] = ("停止", "Stop"),
        ["Preview"] = ("プレビュー", "Preview"),
        ["SetCropArea"] = ("最新キャプチャから範囲設定", "Set Area from Latest Capture"),
        ["RecognitionEvents"] = ("認識イベント", "Recognition Events"),
        ["RecognitionCycles"] = ("認識サイクル", "Recognition Cycles"),
        ["ErrorLog"] = ("エラーログ", "Error Log"),
        ["Time"] = ("時刻", "Time"),
        ["Mode"] = ("モード", "Mode"),
        ["Text"] = ("文字列", "Text"),
        ["Confidence"] = ("信頼度", "Confidence"),
        ["DetectionState"] = ("認識状態", "Detection State"),
        ["RecognizerLabel"] = ("判定ラベル", "Recognizer Label"),
        ["Triggered"] = ("イベント", "Triggered"),
        ["Source"] = ("発生箇所", "Source"),
        ["Summary"] = ("概要", "Summary"),
        ["Details"] = ("詳細", "Details"),
        ["MeasuredFps"] = ("実測 FPS", "Measured FPS"),
        ["TestRunInProgress"] = ("単発テスト実行中", "Running single test"),
        ["HistoryTestRunInProgress"] = ("選択履歴フレームをテスト中", "Running selected history frame test"),
        ["RecognitionStartInProgress"] = ("認識開始中", "Starting recognition"),
        ["RecognitionResumeInProgress"] = ("認識再開中", "Resuming recognition"),
        ["OperationElapsed"] = ("経過", "Elapsed"),
        ["ErrorOccurredStatus"] = ("エラーが発生しました。詳細はエラーログを確認してください。", "An error occurred. Check the error log for details."),
        ["UserFacingErrorMessage"] = ("処理中にエラーが発生しました。", "An error occurred while processing."),
        ["ShareErrorLogPrompt"] = ("エラーログを開発者へ共有してください。", "Please share the error log with the developer."),
        ["CaptureScaleHint"] = ("1.0 = 等倍、0.5 = 半分", "1.0 = original size, 0.5 = half size"),
        ["EventActionFrameOffsetHint"] = ("0 = 最新、-1 = 1コマ前、+1 = 1コマ後", "0 = latest, -1 = one frame earlier, +1 = one frame later"),
        ["PreviewCaptured"] = ("キャプチャ映像", "Captured Image"),
        ["PreviewProcessed"] = ("画像処理後", "Processed Image"),
        ["PreviewOcrReferences"] = ("OCR 基準点", "OCR References"),
        ["PreviewOcrTargets"] = ("OCR エリア", "OCR Areas"),
        ["PreviewDistanceMeasurement"] = ("距離計測", "Distance Measurement"),
        ["PreviewSettings"] = ("プレビュー設定", "Preview Settings"),
        ["ShowOcrPreviewLabels"] = ("OCR ラベルを表示", "Show OCR Labels"),
        ["ShowDistancePreviewAnnotations"] = ("距離とラベルを表示", "Show distances and labels"),
        ["ShowDistanceCursorPreview"] = ("カーソルまでの距離を表示", "Show cursor distance preview"),
        ["OcrReferencePreviewModeInfo"] = ("基準点照合: 生画像(倍率適用後)をグレースケール化して比較します。", "Reference matching: compare the scaled source image in grayscale."),
        ["OcrReferencePreviewNoReferences"] = ("OCR 基準点は未設定です。", "No OCR references are configured."),
        ["OcrReferenceTemplateMissing"] = ("テンプレート未設定またはファイル未検出", "Template is not configured or the file is missing."),
        ["OcrReferenceMatched"] = ("一致", "Matched"),
        ["OcrReferenceNotMatched"] = ("不一致", "Not matched"),
        ["DistanceMeasurement"] = ("距離計測", "Distance Measurement"),
        ["EnableDistanceMeasurement"] = ("距離計測を有効化", "Enable Distance Measurement"),
        ["DistanceReferences"] = ("基準点", "Reference Points"),
        ["AddDistanceReference"] = ("基準点を追加", "Add Reference Point"),
        ["DistanceReferenceDefaultName"] = ("基準点", "Reference"),
        ["DistanceTargets"] = ("距離計測ターゲット", "Distance Targets"),
        ["AddDistanceTarget"] = ("距離計測ターゲットを追加", "Add Distance Target"),
        ["DistanceTargetDefaultName"] = ("ターゲット", "Target"),
        ["ReferenceTemplate"] = ("基準点テンプレート", "Reference Template"),
        ["ReferenceName"] = ("基準点名", "Reference Name"),
        ["ReferenceThreshold"] = ("基準点閾値", "Reference Threshold"),
        ["TargetTemplate"] = ("ターゲットテンプレート", "Target Template"),
        ["TargetThreshold"] = ("ターゲット閾値", "Target Threshold"),
        ["Ready"] = ("準備完了。", "Ready."),
        ["TestRunCompleted"] = ("単発テストを実行しました。", "Single test run completed."),
        ["HistoryTestRunCompleted"] = ("選択履歴フレームで単発テストを実行しました。", "Ran a single test on the selected history frame."),
        ["RecognitionLoopRunning"] = ("認識ループを開始しました。", "Recognition loop is running."),
        ["RecognitionLoopStopped"] = ("認識ループを停止しました。", "Recognition loop stopped."),
        ["ProfileSaved"] = ("プロファイルを保存しました: {0}", "Saved profile: {0}"),
        ["ProfileCreated"] = ("新しいプロファイルを作成しました: {0}", "Created a new profile: {0}"),
        ["ProfileLoaded"] = ("プロファイルを読み込みました: {0}", "Loaded profile: {0}"),
        ["ProfileCalibrated"] = ("自動調整したプロファイルを保存しました: {0}", "Saved calibrated profile: {0}"),
        ["ProfileListReloaded"] = ("プロファイル一覧を更新しました。", "Profile list refreshed."),
        ["ProfileSelectionRequired"] = ("読込対象のプロファイルを選択してください。", "Select a profile to load."),
        ["ProfileLoadRequiredBeforeOperations"] = ("プロファイル名または選択中プロファイルが変更されています。続行する前にプロファイルを読み込んでください。", "The profile name or selected profile changed. Load the profile before continuing."),
        ["ProfileLoadRequiredHint"] = ("プロファイル名または選択中プロファイルを変更したため、プロファイルを読み込むまで操作できません。", "Operations are locked until you load the profile after changing the profile name or selection."),
        ["ProfileSaveAsHint"] = ("プロファイル名を変更しています。現在の設定を新規プロファイルとして保存できます。", "The profile name changed. You can save the current settings as a new profile."),
        ["ProfileNameAlreadyExists"] = ("同名の別プロファイルが既に存在します。別の名前にするか、そのプロファイルを読み込んでください: {0}", "A different profile with the same name already exists. Choose another name or load that profile first: {0}"),
        ["NewProfileDefaultName"] = ("新規プロファイル", "New Profile"),
        ["ScreenRegionSelected"] = ("画面領域を選択しました: {0}x{1} @ ({2}, {3})", "Screen region selected: {0}x{1} at ({2}, {3})"),
        ["TemplateNeedsCapture"] = ("先に単発テストまたは認識開始でキャプチャ画像を取得してください。", "Capture an image first by running a single test or starting recognition."),
        ["HistoryFrameTestNeedsCapture"] = ("先に単発テストまたは認識開始で履歴フレームを取得してください。", "Capture a history frame first by running a single test or starting recognition."),
        ["HistoryFrameSelectionRequired"] = ("先に履歴フレームを選択してください。", "Select a history frame first."),
        ["CorrespondingFrameExpired"] = ("対応する履歴フレームは保持期限切れです。", "The corresponding history frame has expired."),
        ["TemplateNeedsRecognizer"] = ("テンプレートマッチング認識を選択してください。", "Select template matching as the recognition method."),
        ["TemplateSaved"] = ("テンプレート画像を保存しました: {0}", "Template image saved: {0}"),
        ["TemplateSavedAs"] = ("テンプレート画像を別名保存しました: {0}", "Saved the template image as: {0}"),
        ["ImageEditNeedsFile"] = ("再トリミングする画像ファイルを先に指定してください。", "Select an image file before recropping it."),
        ["CropNeedsCapture"] = ("先に単発テストまたは認識開始でキャプチャ画像を取得してください。", "Capture an image first by running a single test or starting recognition."),
        ["CropNeedsPreprocessor"] = ("トリミング前処理を選択してください。", "Select the crop preprocessor."),
        ["CropAreaUpdated"] = ("トリミング範囲を設定しました: {0}x{1} @ ({2}, {3})", "Updated crop area: {0}x{1} at ({2}, {3})"),
        ["TemplateFileFilter"] = ("PNG 画像 (*.png)|*.png", "PNG Image (*.png)|*.png"),
        ["TemplateDialogFileName"] = ("template.png", "template.png"),
        ["ImageFileFilter"] = ("画像ファイル (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|すべてのファイル (*.*)|*.*", "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*"),
        ["Browse"] = ("参照", "Browse"),
        ["RecropAndOverwrite"] = ("再トリミングして上書き", "Recrop and Overwrite"),
        ["RecropAndSaveAs"] = ("再トリミングして別名保存", "Recrop and Save As"),
        ["TemplateCropTitle"] = ("テンプレート切り出し", "Template Crop"),
        ["CropAreaTitle"] = ("トリミング範囲選択", "Select Crop Area"),
        ["RawTemplateCropTitle"] = ("生画像テンプレート切り出し", "Raw Template Crop"),
        ["ProcessedTemplateCropTitle"] = ("画像処理後テンプレート切り出し", "Processed Template Crop"),
        ["Zoom"] = ("ズーム", "Zoom"),
        ["CropSelectionConfirm"] = ("この範囲で決定", "Use This Region"),
        ["CropSelectionCancel"] = ("キャンセル", "Cancel"),
        ["ProcessedTemplateNeedsCapture"] = ("先に単発テストまたは認識開始で画像処理後のプレビューを取得してください。", "Capture a processed preview first by running a single test or starting recognition."),
        ["OcrRegionUpdated"] = ("OCR 範囲を設定しました: {0}x{1} @ ({2}, {3})", "Updated OCR region: {0}x{1} at ({2}, {3})"),
        ["OcrAnchorNeedsDetection"] = ("相対 OCR 範囲を設定するには、先に基準点が認識されたフレームを取得してください。", "Capture a frame with a detected anchor before setting a relative OCR region."),
        ["Detected"] = ("認識中", "Detected"),
        ["NotDetected"] = ("未認識", "Not detected"),
        ["EventOnEnter"] = ("未認識→認識", "Not detected -> detected"),
        ["EventOnExit"] = ("認識→未認識", "Detected -> not detected"),
        ["EventWhileDetected"] = ("認識中", "While detected")
        ,
        ["EventActionNone"] = ("なし", "None"),
        ["EventActionOcr"] = ("OCR", "OCR"),
        ["EventActionDistance"] = ("距離計測", "Distance Measurement"),
        ["EventActionOcrAndDistance"] = ("OCR + 距離計測", "OCR + Distance Measurement")
    };

    private static readonly IReadOnlyDictionary<string, (string Ja, string En)> ComponentTexts = new Dictionary<string, (string Ja, string En)>(StringComparer.OrdinalIgnoreCase)
    {
        ["builtin.screen-region"] = ("画面領域キャプチャ", "Screen Region Capture"),
        ["builtin.camera"] = ("カメラキャプチャ", "Camera Capture"),
        ["builtin.image-file"] = ("画像ファイル入力", "Image File"),
        ["builtin.preprocess.aspect-ratio"] = ("縦横比変更", "Aspect Ratio"),
        ["builtin.preprocess.grayscale"] = ("グレースケール", "Grayscale"),
        ["builtin.preprocess.crop"] = ("トリミング", "Crop"),
        ["builtin.preprocess.threshold"] = ("二値化", "Threshold"),
        ["builtin.preprocess.invert"] = ("反転", "Invert"),
        ["builtin.preprocess.contour"] = ("輪郭抽出", "Contour"),
        ["builtin.recognition.template-match"] = ("テンプレートマッチング", "Template Matching"),
        ["builtin.ocr.tesseract"] = ("Tesseract OCR", "Tesseract OCR"),
        ["builtin.ocr.tesseract-japanese"] = ("Tesseract OCR 日本語", "Tesseract OCR Japanese"),
        ["builtin.ocr.ndlocr-lite"] = ("NDLOCR-Lite", "NDLOCR-Lite"),
        ["builtin.ocr.paddle-japanese-python"] = ("Paddle OCR 日本語(Python)", "Paddle OCR Japanese (Python)"),
        ["builtin.ocr.paddle"] = ("Paddle OCR", "Paddle OCR")
    };

    private static readonly IReadOnlyDictionary<string, (string Ja, string En)> ParameterTexts = new Dictionary<string, (string Ja, string En)>(StringComparer.OrdinalIgnoreCase)
    {
        ["builtin.screen-region:X"] = ("X", "X"),
        ["builtin.screen-region:Y"] = ("Y", "Y"),
        ["builtin.screen-region:Width"] = ("幅", "Width"),
        ["builtin.screen-region:Height"] = ("高さ", "Height"),
        ["builtin.camera:CameraIndex"] = ("カメラ", "Camera"),
        ["builtin.image-file:ImagePath"] = ("画像ファイル", "Image File"),
        ["builtin.camera:Width"] = ("幅", "Width"),
        ["builtin.camera:Height"] = ("高さ", "Height"),
        ["builtin.camera:Fps"] = ("カメラ FPS", "Camera FPS"),
        ["builtin.preprocess.aspect-ratio:WidthScale"] = ("幅倍率", "Width Scale"),
        ["builtin.preprocess.aspect-ratio:HeightScale"] = ("高さ倍率", "Height Scale"),
        ["builtin.preprocess.crop:X"] = ("X", "X"),
        ["builtin.preprocess.crop:Y"] = ("Y", "Y"),
        ["builtin.preprocess.crop:Width"] = ("幅", "Width"),
        ["builtin.preprocess.crop:Height"] = ("高さ", "Height"),
        ["builtin.ocr-target:X"] = ("X", "X"),
        ["builtin.ocr-target:Y"] = ("Y", "Y"),
        ["builtin.ocr-target:Width"] = ("幅", "Width"),
        ["builtin.ocr-target:Height"] = ("高さ", "Height"),
        ["builtin.preprocess.threshold:Threshold"] = ("しきい値", "Threshold"),
        ["builtin.preprocess.threshold:Mode"] = ("モード", "Mode"),
        ["builtin.recognition.template-match:TemplatePath"] = ("テンプレート画像", "Template Image"),
        ["builtin.recognition.template-match:Threshold"] = ("一致しきい値", "Match Threshold"),
        ["builtin.recognition.template-match:Method"] = ("比較手法", "Method"),
        ["builtin.recognition.template-match:SearchX"] = ("検索 X", "Search X"),
        ["builtin.recognition.template-match:SearchY"] = ("検索 Y", "Search Y"),
        ["builtin.recognition.template-match:SearchWidth"] = ("検索幅", "Search Width"),
        ["builtin.recognition.template-match:SearchHeight"] = ("検索高さ", "Search Height"),
        ["builtin.ocr.tesseract:DataPath"] = ("tessdata フォルダ", "Tessdata Folder"),
        ["builtin.ocr.tesseract:Language"] = ("言語", "Language"),
        ["builtin.ocr.tesseract:EngineMode"] = ("エンジンモード", "Engine Mode"),
        ["builtin.ocr.tesseract:PageSegMode"] = ("ページ分割", "Page Segmentation"),
        ["builtin.ocr.tesseract:Whitelist"] = ("ホワイトリスト", "Whitelist"),
        ["builtin.ocr.tesseract-japanese:LanguageProfile"] = ("言語プロファイル", "Language Profile"),
        ["builtin.ocr.tesseract-japanese:PageSegMode"] = ("ページ分割", "Page Segmentation"),
        ["builtin.ocr.tesseract-japanese:Whitelist"] = ("ホワイトリスト", "Whitelist"),
        ["builtin.ocr.ndlocr-lite:EnableTcy"] = ("縦中横補正を使う", "Enable TateChuYoko"),
        ["builtin.ocr.paddle:Preset"] = ("プリセット", "Preset"),
        ["builtin.ocr.paddle:DetModelPath"] = ("検出モデル", "Det Model"),
        ["builtin.ocr.paddle:ClsModelPath"] = ("分類モデル", "Cls Model"),
        ["builtin.ocr.paddle:RecModelPath"] = ("認識モデル", "Rec Model"),
        ["builtin.ocr.paddle:KeysPath"] = ("文字辞書", "Keys File"),
        ["builtin.ocr.paddle:UseGpu"] = ("GPU を使う", "Use GPU"),
        ["builtin.ocr.paddle:UseAngleCls"] = ("角度分類を使う", "Use Angle Classification"),
        ["builtin.ocr.paddle:CpuThreads"] = ("CPU スレッド数", "CPU Threads")
    };

    private static readonly IReadOnlyDictionary<string, (string Ja, string En)> OptionTexts = new Dictionary<string, (string Ja, string En)>(StringComparer.OrdinalIgnoreCase)
    {
        ["builtin.preprocess.threshold:Mode:Binary"] = ("通常二値化", "Binary"),
        ["builtin.preprocess.threshold:Mode:BinaryInv"] = ("反転二値化", "Binary Inverted"),
        ["builtin.preprocess.threshold:Mode:Otsu"] = ("大津の二値化", "Otsu"),
        ["builtin.recognition.template-match:Method:CCoeffNormed"] = ("相関係数 (正規化)", "CCoeff Normed"),
        ["builtin.recognition.template-match:Method:CCorrNormed"] = ("相関 (正規化)", "CCorr Normed"),
        ["builtin.recognition.template-match:Method:SqDiffNormed"] = ("差分二乗和 (正規化)", "SqDiff Normed"),
        ["builtin.ocr.tesseract:EngineMode:Default"] = ("既定", "Default"),
        ["builtin.ocr.tesseract:EngineMode:TesseractOnly"] = ("Tesseract のみ", "Tesseract Only"),
        ["builtin.ocr.tesseract:EngineMode:LstmOnly"] = ("LSTM のみ", "LSTM Only"),
        ["builtin.ocr.tesseract:EngineMode:TesseractAndLstm"] = ("Tesseract + LSTM", "Tesseract + LSTM"),
        ["builtin.ocr.tesseract:PageSegMode:Auto"] = ("自動", "Auto"),
        ["builtin.ocr.tesseract:PageSegMode:SingleBlock"] = ("単一ブロック", "Single Block"),
        ["builtin.ocr.tesseract:PageSegMode:SingleLine"] = ("単一行", "Single Line"),
        ["builtin.ocr.tesseract:PageSegMode:SingleWord"] = ("単語", "Single Word"),
        ["builtin.ocr.tesseract:PageSegMode:SingleChar"] = ("単一文字", "Single Char"),
        ["builtin.ocr.tesseract-japanese:LanguageProfile:jpn+eng"] = ("日本語 + 英語", "Japanese + English"),
        ["builtin.ocr.tesseract-japanese:LanguageProfile:jpn"] = ("日本語", "Japanese"),
        ["builtin.ocr.tesseract-japanese:LanguageProfile:jpn_vert"] = ("日本語(縦書き)", "Japanese (Vertical)"),
        ["builtin.ocr.tesseract-japanese:PageSegMode:Auto"] = ("自動", "Auto"),
        ["builtin.ocr.tesseract-japanese:PageSegMode:SingleBlock"] = ("単一ブロック", "Single Block"),
        ["builtin.ocr.tesseract-japanese:PageSegMode:SingleLine"] = ("単一行", "Single Line"),
        ["builtin.ocr.tesseract-japanese:PageSegMode:SingleWord"] = ("単語", "Single Word"),
        ["builtin.ocr.tesseract-japanese:PageSegMode:SingleChar"] = ("単一文字", "Single Char"),
        ["builtin.ocr.paddle:Preset:Default"] = ("既定", "Default"),
        ["builtin.ocr.paddle:Preset:V6_EN"] = ("V6 英語", "V6 English"),
        ["builtin.ocr.paddle:Preset:V6_Tiny"] = ("V6 Tiny", "V6 Tiny"),
        ["builtin.ocr.paddle:Preset:V6_Small"] = ("V6 Small", "V6 Small"),
        ["builtin.ocr.paddle:Preset:Custom"] = ("カスタム", "Custom")
    };

    private UiLanguage currentLanguage = UiLanguage.Japanese;

    public UiLanguage CurrentLanguage
    {
        get => currentLanguage;
        set
        {
            if (SetProperty(ref currentLanguage, value))
            {
                RaisePropertyChanged("Item[]");
            }
        }
    }

    public string this[string key] => Text(key);

    public string Text(string key)
    {
        return Texts.TryGetValue(key, out var value)
            ? Select(value)
            : key;
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(Text(key), args);
    }

    public string Component(string componentId, string fallback)
    {
        return ComponentTexts.TryGetValue(componentId, out var value)
            ? Select(value)
            : fallback;
    }

    public string Parameter(string componentId, string parameterKey, string fallback)
    {
        return ParameterTexts.TryGetValue($"{componentId}:{parameterKey}", out var value)
            ? Select(value)
            : fallback;
    }

    public string Option(string componentId, string parameterKey, string valueKey, string fallback)
    {
        return OptionTexts.TryGetValue($"{componentId}:{parameterKey}:{valueKey}", out var value)
            ? Select(value)
            : fallback;
    }

    public string EventMode(RecognitionEventMode mode)
    {
        return mode switch
        {
            RecognitionEventMode.OnDetectedEnter => Text("EventOnEnter"),
            RecognitionEventMode.OnDetectedExit => Text("EventOnExit"),
            RecognitionEventMode.WhileDetected => Text("EventWhileDetected"),
            _ => mode.ToString()
        };
    }

    public string EventAction(RecognitionEventAction action)
    {
        return action switch
        {
            RecognitionEventAction.None => Text("EventActionNone"),
            RecognitionEventAction.Ocr => Text("EventActionOcr"),
            RecognitionEventAction.DistanceMeasurement => Text("EventActionDistance"),
            RecognitionEventAction.OcrAndDistance => Text("EventActionOcrAndDistance"),
            _ => action.ToString()
        };
    }

    private string Select((string Ja, string En) pair)
    {
        return CurrentLanguage == UiLanguage.Japanese ? pair.Ja : pair.En;
    }
}
