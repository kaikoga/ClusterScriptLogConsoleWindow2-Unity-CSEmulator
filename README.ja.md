# ClusterScript Log Console Window 2 Interop: CSEmulator

## これは何ですか？

- CSEmulator から ClusterScript Log Console Window 2 にログ出力するサンプルです  

## インストール方法

- ClusterScript Log Console Window 2 をインストールする
- manifest.json に追記する

```json
{
  "dependencies": {
    "net.kaikoga.cslcw2.interop.csemulator": "https://github.com/kaikoga/ClusterScriptLogConsoleWindow2-Unity-CSEmulator.git"
  }
}
```

- CSEmulator 2.82 以上をインストールする
- `Assets/KaomoLab/CSEmulator/Editor/Preview/EngineFacade.cs` 内の `DebugLogFactory` って書いてある場所を `Silksprite.ClusterScriptLogConsoleWindow2.Interop.CSEmulator.ScriptableItemLogExtLoggerFactory` に書き換える
- `DebugLogFactory` に `optionBridge.raw` を渡す代わりに `optionBridge` を渡す
- `Assets/KaomoLab/CSEmulator/Editor/Preview/KaomoLab.CSEmulator.Editor.Preview.asmdef` の `"references"` に `"Silksprite.ClusterScriptLogConsoleWindow2.Interop.CSEmulator"` を足す

## 使用方法

エディタプレビューを開始するか、 `"ClusterScript"` メニューの `"Editor Preview"` を選ぶとエディタプレビューのログが表示されます
