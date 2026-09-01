## 概要

本リリースは、Godot @@BASE_VER@@ に [dn2cpp](https://github.com/takuma-komatsu/dn2cpp)（Unity の IL2CPP 相当の機能を提供する独立したオープンソースプロジェクト）による C# ゲームのネイティブビルド機能を追加したカスタムビルドです。本家（upstream）の Godot では非対応となっている **C# ゲームの Web エクスポートにも対応しています。**

※ 詳細とバグ報告は [dn2cpp リポジトリ](https://github.com/takuma-komatsu/dn2cpp) を参照してください。

## 前回リリース（@@PREV_VERSION@@）からの変更

- Godot フォークでエクスポートプリセットに Incremental GC 設定（`dotnet/dn2cpp/incremental_gc`）を追加し、CMake 構成値の明示的な引き渡しに対応しました（[godot-dn2cpp PR #9](https://github.com/takuma-komatsu/godot-dn2cpp/pull/9)）
- Godot フォークの Linux CI 環境において godot-cpp のアーカイブ引数上限を回避するレスポンスファイル適用を行いました（[godot-dn2cpp PR #10](https://github.com/takuma-komatsu/godot-dn2cpp/pull/10)）
- [PR #96](https://github.com/takuma-komatsu/dn2cpp/pull/96) で、起動時コマンドライン引数（`--dn2cpp-gc-incremental=0|1`）による Incremental GC ポリシーのオーバーライドに対応しました
- [PR #95](https://github.com/takuma-komatsu/dn2cpp/pull/95) で、`Array.Resize` の ref 書き戻し、P/Invoke の byref 文字列書き戻し、デリゲート生成、参照配列初期化等における Incremental GC ライトバリアの網羅性を強化しました
- [PR #94](https://github.com/takuma-komatsu/dn2cpp/pull/94) で、プラットフォーム ISA 検証（`PlatformIsaProbe`）の到達性シャーディングを導入し、テスト・検証処理を最適化しました

全コミットは <https://github.com/takuma-komatsu/dn2cpp/compare/f8f12cd7c131e450e06c314cbea6eb57fca829a2...6a988e1f572886a113ec461f38e078f4a3e2b260> を参照してください。

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
