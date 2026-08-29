## 概要

本リリースは、Godot @@BASE_VER@@ に [dn2cpp](https://github.com/takuma-komatsu/dn2cpp)（Unity の IL2CPP 相当の機能を提供する独立したオープンソースプロジェクト）による C# ゲームのネイティブビルド機能を追加したカスタムビルドです。本家（upstream）の Godot では非対応となっている **C# ゲームの Web エクスポートにも対応しています。**

※ 詳細とバグ報告は [dn2cpp リポジトリ](https://github.com/takuma-komatsu/dn2cpp) を参照してください。

## 前回リリース（@@PREV_VERSION@@）からの変更

- Godot フォークで Web エクスポート時の診断シンボル保持オプション（`dotnet/dn2cpp/keep_symbols`）に対応しました（[godot-dn2cpp PR #5](https://github.com/takuma-komatsu/godot-dn2cpp/pull/5)）
- Web エクスポートテンプレートを Release / Debug の 2 構成で配布し、デバッグ付きエクスポートが対応する Debug テンプレートを使うようになりました
- Godot フォークで Windows エクスポート時の NativeAOT 依存 DLL 探索処理を修正しました（[godot-dn2cpp PR #6](https://github.com/takuma-komatsu/godot-dn2cpp/pull/6)）
- [PR #81](https://github.com/takuma-komatsu/dn2cpp/pull/81) で、Windows 上での NativeAOT 依存ライブラリのリンクおよび探索パス解決に対応しました
- [PR #80](https://github.com/takuma-komatsu/dn2cpp/pull/80) で、Web 環境における入れ子呼び出し引数の GC 保護および文字列診断処理の堅牢化を行いました
- [PR #78](https://github.com/takuma-komatsu/dn2cpp/pull/78) で、Windows 上でのプロセス待機ハンドル・スレッド同期プリミティブ・HRESULT 変換などの互換性を改善しました

全コミットは <https://github.com/takuma-komatsu/dn2cpp/compare/6600b909fb8359a11f4ca2ff95016cba1ad6d823...45edcbd89cf5e0622557e4e1329aee5d7a641198> を参照してください。

## ダウンロード

配布ファイルは、このページ下部の **Assets** 一覧からダウンロードしてください。`SHA256SUMS.txt` と照合する手順は[ダウンロードファイルの検証](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md#ダウンロードファイルの検証)にあります。

<!--lane:!editor-windows-->このリリースには **Windows エディタが含まれていません**。ホストプラットフォームとしてはバックエンド自体が対応しています。

インストール、動作要件、エクスポート手順、トラブルシューティング、既知の制限、ライセンスは[エディタ利用ガイド](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md)を参照してください。リンク先は本リリースを切った時点のコミットに固定されています。

<!--lane:editor-windows-->Windows ゲームのエクスポートには、Assets の `godot-<リリース名>-windows-x86_64-templates.zip` を展開し、`godot_windows_release_x86_64.exe` と `godot_windows_debug_x86_64.exe` をプリセットの「カスタムテンプレート → リリース / デバッグ」にそれぞれ指定してください。

<!--lane:web-->Web ゲームのエクスポートには、Assets の `godot-<リリース名>-web-templates.zip` を展開し、内側の `godot_web_release.zip` と `godot_web_debug.zip` を Web プリセットの「カスタムテンプレート → リリース / デバッグ」にそれぞれ指定してください。

## Provenance

| | |
|---|---|
| エンジンの provenance | `@@ENGINE_PROVENANCE@@` |
| dn2cpp のコミット | `@@DN2CPP_COMMIT@@` |
<!--lane:web-->| Web テンプレートのビルドに使用 | emcc `@@EMCC@@`（Emscripten `@@EMSDK_VERSION@@`） |
<!--lane:macos-->| macOS テンプレートの抽出元 | upstream `macos.zip`、sha256 `@@UPSTREAM_MACOS_SHA256@@` |
