# ClusterScript Log Console Window 2 Interop: CSEmulator

## これは何ですか？

- CSEmulator から ClusterScript Log Console Window 2 にログ出力する拡張です

## インストール方法

- ClusterScript Log Console Window 2 をインストールする
- CSEmulator 2.86 以上をインストールする
- manifest.json に追記する

```json
{
  "dependencies": {
    "net.kaikoga.cslcw2.interop.csemulator": "https://github.com/kaikoga/ClusterScriptLogConsoleWindow2-Unity-CSEmulator.git"
  }
}
```

## 使用方法

エディタプレビューを開始するか、 `"ClusterScript"` メニューの `"Editor Preview"` を選ぶとエディタプレビューのログが表示されます
