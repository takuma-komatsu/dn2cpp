## 概要

本リリースは、Godot @@BASE_VER@@ に [dn2cpp](https://github.com/takuma-komatsu/dn2cpp)（Unity の IL2CPP 相当の機能を提供する独立したオープンソースプロジェクト）による C# ゲームのネイティブビルド機能を追加したカスタムビルドです。本家（upstream）の Godot では非対応となっている **C# ゲームの Web エクスポートにも対応しています。**

※ 詳細とバグ報告は [dn2cpp リポジトリ](https://github.com/takuma-komatsu/dn2cpp) を参照してください。

## 前回リリース（@@PREV_VERSION@@）からの変更

- Godot フォークのエクスポートプリセットに IL Pre-stripping（ILDiet によるマネージドアセンブリの事前ストリッピング）オプションを追加し、デフォルトで有効化しました（[godot-dn2cpp PR #13](https://github.com/takuma-komatsu/godot-dn2cpp/pull/13)）
- [PR #122](https://github.com/takuma-komatsu/dn2cpp/pull/122) で、ILDiet による .NET モジュール内の未使用 Godot 型の削減に対応しました
- [PR #121](https://github.com/takuma-komatsu/dn2cpp/pull/121) で、ネイティブ ILDiet ストリッピングの統合とマネージド出力パリティ検証を行いました
- [PR #120](https://github.com/takuma-komatsu/dn2cpp/pull/120) で、Windows スモークテスト時のネイティブ ISA ゲートスキップを修正しました
- [PR #119](https://github.com/takuma-komatsu/dn2cpp/pull/119) で、トランスパイル前に不要な型・メソッドを IL レベルで削減するアセンブリ前処理 ILDiet を導入しました
- [PR #118](https://github.com/takuma-komatsu/dn2cpp/pull/118)（[PR #113](https://github.com/takuma-komatsu/dn2cpp/pull/113), [PR #114](https://github.com/takuma-komatsu/dn2cpp/pull/114), [PR #115](https://github.com/takuma-komatsu/dn2cpp/pull/115), [PR #117](https://github.com/takuma-komatsu/dn2cpp/pull/117)）で、プランニング時の C++ 本文描画省略やインターフェース閉包キャッシュによる性能改善、および文字列変性呼び出しの到達性判定を修正しました

全コミットは <https://github.com/takuma-komatsu/dn2cpp/compare/308e30e00cdda083d7166a1b397d0fa417bc1898...@@DOCS_REF@@> を参照してください。

## ダウンロード

配布ファイルは、このページ下部の **Assets** 一覧からダウンロードしてください。`SHA256SUMS.txt` と照合する手順は[ダウンロードファイルの検証](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md#ダウンロードファイルの検証)にあります。

<!--lane:!editor-windows-->このリリースには **Windows エディタが含まれていません**。ホストプラットフォームとしてはバックエンド自体が対応しています。

インストール、動作要件、エクスポート手順、トラブルシューティング、既知の制限、ライセンスは[エディタ利用ガイド](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md)を参照してください。リンク先は本リリースを切った時点のコミットに固定されています。

<!--lane:editor-windows-->Windows ゲームのエクスポートには、対応する Godot バージョンの公式 .NET エクスポートテンプレートを使用してください。

<!--lane:web-->Web ゲームのエクスポートには、Assets の `godot-<リリース名>-web-templates.zip` を展開し、内側の `godot_web_release.zip` と `godot_web_debug.zip` を Web プリセットの「カスタムテンプレート → リリース / デバッグ」にそれぞれ指定してください。

## Provenance

| | |
|---|---|
| エンジンの provenance | `@@ENGINE_PROVENANCE@@` |
| dn2cpp のコミット | `@@DN2CPP_COMMIT@@` |
<!--lane:web-->| Web テンプレートのビルドに使用 | emcc `@@EMCC@@`（Emscripten `@@EMSDK_VERSION@@`） |
<!--lane:macos-->| macOS テンプレートの抽出元 | upstream `macos.zip`、sha256 `@@UPSTREAM_MACOS_SHA256@@` |
