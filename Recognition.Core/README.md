# GenericRecognition.Workbench.Abstractions

Generic Recognition Workbench の拡張機能向け契約と共有モデルを提供します。このパッケージは WPF および組み込み認識ランタイムに依存しません。

## 拡張機能の作成

次のファクトリインターフェースを 1 つ以上実装します。

- `IFrameSourceFactory`
- `IImageProcessorFactory`
- `IRecognitionMethodFactory`
- `IOcrEngineFactory`

各ファクトリは、安定したグローバルに一意の `ComponentDescriptor.Id` とパラメータ定義を公開し、渡されたパラメータマップからランタイムコンポーネントを作成します。

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

ホストが使用する `Abstractions` と同じメジャーバージョンを参照してください。拡張機能のアセンブリをホストのプラグインディレクトリへ配置します。公開するファクトリ型は抽象型ではない型とし、public な引数なしコンストラクターを持たせてください。ホストは `RecognitionPluginCatalog` の構築時に認識ファクトリを直接注入することもできます。

プロジェクト文書: https://github.com/p-o-ke-nae/GenericRecognitionWorkbench
