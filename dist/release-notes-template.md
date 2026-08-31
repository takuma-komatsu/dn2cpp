## 概要

本リリースは、Godot @@BASE_VER@@ に [dn2cpp](https://github.com/takuma-komatsu/dn2cpp)（Unity の IL2CPP 相当の機能を提供する独立したオープンソースプロジェクト）による C# ゲームのネイティブビルド機能を追加したカスタムビルドです。本家（upstream）の Godot では非対応となっている **C# ゲームの Web エクスポートにも対応しています。**

※ 詳細とバグ報告は [dn2cpp リポジトリ](https://github.com/takuma-komatsu/dn2cpp) を参照してください。

## 前回リリース（@@PREV_VERSION@@）からの変更

- [PR #82](https://github.com/takuma-komatsu/dn2cpp/pull/82) で、Windows エクスポートテンプレートの Release / Debug ペア配布（`godot-<リリース名>-windows-x86_64-templates.zip`）および各プリセットのリリース／デバッグテンプレート指定に対応しました
- [PR #90](https://github.com/takuma-komatsu/dn2cpp/pull/90) で、クロスプラットフォーム ISA コントラクト（x86 SIMD / AVX / AVX-512 / ARM Neon 等）の体系化とトランスパイル対応を統合しました
- [PR #93](https://github.com/takuma-komatsu/dn2cpp/pull/93) で、安全なメソッド本体のトランスパイル処理を並列化し、トランスパイル性能を改善しました
- [PR #92](https://github.com/takuma-komatsu/dn2cpp/pull/92) で、読み取り専用環境におけるエディタツールチェーンのステージング処理およびメモリ上限対応を行いました
- [PR #91](https://github.com/takuma-komatsu/dn2cpp/pull/91) で、Godot 関連の検証ゲートの完全な独立性と隔離の強化を行いました

全コミットは <https://github.com/takuma-komatsu/dn2cpp/compare/e5f3b41522e64d258fb18b4150682cffa8d2e34c...730121fc66dbe1e13e00fc9e663a8a3ce2ea7d0c> を参照してください。

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
