## 概要

- Godot @@BASE_VER@@ のフォークです
- dn2cpp は Unity IL2CPP 相当の機能を提供する独立したオープンソースプロジェクトで、C# ゲームをネイティブコードへ変換します。詳細とバグ報告は <https://github.com/takuma-komatsu/dn2cpp> を参照してください
<!--lane:web-->- upstream の Godot では対応していない C# ゲームの Web エクスポートにも対応しています

## 前回リリース（@@PREV_VERSION@@）からの変更

- Godot フォークで macOS deployment target の設定がビルドへ反映されるようにしました
- [PR #76](https://github.com/takuma-komatsu/dn2cpp/pull/76) で、`ValueTask` / `ValueTask<T>` の `IValueTaskSource` ステータス管理とトークン検証を修正しました
- [PR #74](https://github.com/takuma-komatsu/dn2cpp/pull/74) で、Web の乱数生成が誤って Apple 向け実装を参照する問題を修正しました
- [PR #73](https://github.com/takuma-komatsu/dn2cpp/pull/73)、[PR #67](https://github.com/takuma-komatsu/dn2cpp/pull/67)、[PR #71](https://github.com/takuma-komatsu/dn2cpp/pull/71)、[PR #64](https://github.com/takuma-komatsu/dn2cpp/pull/64) で、`Enum.Parse` / `Enum.TryParse` と浮動小数点数の Parse 系 API の対応を拡充しました
- [PR #72](https://github.com/takuma-komatsu/dn2cpp/pull/72)、[PR #65](https://github.com/takuma-komatsu/dn2cpp/pull/65)、[PR #62](https://github.com/takuma-komatsu/dn2cpp/pull/62) で、プリミティブ型の比較、`ToString`、補間文字列、`Vector`、`CancellationToken` などの BCL 互換性を改善しました

全コミットは <https://github.com/takuma-komatsu/dn2cpp/compare/4be3443bac325eb54b7e488243cbb635b01013d1...170be524673898a3e7efcbef70a931a742ea3b2f> を参照してください。

## ダウンロード

配布ファイルは、このページ下部の **Assets** 一覧からダウンロードしてください。`SHA256SUMS.txt` と照合する手順は[ダウンロードファイルの検証](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md#ダウンロードファイルの検証)にあります。

<!--lane:!editor-windows-->このリリースには **Windows エディタが含まれていません**。ホストプラットフォームとしてはバックエンド自体が対応しています。

インストール、動作要件、エクスポート手順、トラブルシューティング、既知の制限、ライセンスは[エディタ利用ガイド](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md)を参照してください。リンク先は本リリースを切った時点のコミットに固定されています。

## Provenance

| | |
|---|---|
| エンジンの provenance | `@@ENGINE_PROVENANCE@@` |
| dn2cpp のコミット | `@@DN2CPP_COMMIT@@` |
<!--lane:web-->| Web テンプレートのビルドに使用 | emcc `@@EMCC@@`（Emscripten `@@EMSDK_VERSION@@`） |
<!--lane:macos-->| macOS テンプレートの抽出元 | upstream `macos.zip`、sha256 `@@UPSTREAM_MACOS_SHA256@@` |
