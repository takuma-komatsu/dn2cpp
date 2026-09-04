## 概要

本リリースは、Godot @@BASE_VER@@ に [dn2cpp](https://github.com/takuma-komatsu/dn2cpp)（Unity の IL2CPP 相当の機能を提供する独立したオープンソースプロジェクト）による C# ゲームのネイティブビルド機能を追加したカスタムビルドです。本家（upstream）の Godot では非対応となっている **C# ゲームの Web エクスポートにも対応しています。**

※ 詳細とバグ報告は [dn2cpp リポジトリ](https://github.com/takuma-komatsu/dn2cpp) を参照してください。

## 前回リリース（@@PREV_VERSION@@）からの変更

- Godot フォークで静的エクスポートターゲット（iOS / Web）における直接 P/Invoke 選択に対応しました（[godot-dn2cpp PR #11](https://github.com/takuma-komatsu/godot-dn2cpp/pull/11)）
- Godot フォークで Windows エクスポート時にプロジェクト定義の依存 DLL を実行ファイルと同階層へ配置するようにしました（[godot-dn2cpp PR #12](https://github.com/takuma-komatsu/godot-dn2cpp/pull/12)）
- [PR #109](https://github.com/takuma-komatsu/dn2cpp/pull/109) で、マネージドオブジェクトによる WaitHandle の所有権管理とライフタイム保護を強化しました
- [PR #107](https://github.com/takuma-komatsu/dn2cpp/pull/107) で、ホスト終了時およびファイナライザ実行時におけるランタイム状態とオブジェクトグラフの保護を強化しました
- [PR #106](https://github.com/takuma-komatsu/dn2cpp/pull/106) で、GC 参照を保持する組み込み構造体配列の GC スキャンに対応しました
- [PR #105](https://github.com/takuma-komatsu/dn2cpp/pull/105) で、Windows エクスポートにおいて Godot 公式 .NET テンプレートの直接利用へ移行しました
- [PR #102](https://github.com/takuma-komatsu/dn2cpp/pull/102) で、Windows プラットフォーム ISA シャード起動の再試行処理を追加しました
- [PR #101](https://github.com/takuma-komatsu/dn2cpp/pull/101) で、HTTP/2 キャンセル処理とハンドシェイクの同期を改善しました
- [PR #99](https://github.com/takuma-komatsu/dn2cpp/pull/99) で、Wasm データ再配置関数の分割境界を跨ぐローカル変数の生存期間を保護しました
- [PR #97](https://github.com/takuma-komatsu/dn2cpp/pull/97) で、NativeAOT スタイルの遅延 P/Invoke 解決を導入し、`NativeLibrary` API に対応しました

全コミットは <https://github.com/takuma-komatsu/dn2cpp/compare/2d14c9182f7c163b272f3298606780fdac5f75eb...@@DOCS_REF@@> を参照してください。

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
