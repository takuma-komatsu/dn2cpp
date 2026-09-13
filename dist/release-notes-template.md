## 概要

本リリースは、Godot @@BASE_VER@@ に [dn2cpp](https://github.com/takuma-komatsu/dn2cpp)（Unity の IL2CPP 相当の機能を提供する独立したオープンソースプロジェクト）による C# ゲームのネイティブビルド機能を追加したカスタムビルドです。本家（upstream）の Godot では非対応となっている **C# ゲームの Web エクスポートにも対応しています。**

※ 詳細とバグ報告は [dn2cpp リポジトリ](https://github.com/takuma-komatsu/dn2cpp) を参照してください。

## 前回リリース（@@PREV_VERSION@@）からの変更

- Godot フォークのエクスポートプリセットに独立したサイズ最適化オプション（IL Pre-stripping、Registry Trim、Reflection Trim）を追加しました（[godot-dn2cpp PR #15](https://github.com/takuma-komatsu/godot-dn2cpp/pull/15)）
- Godot フォークのエクスポートプリセットに DeClang による難読化オプションを追加し、Android エクスポートでの DeClang 難読化に対応しました（[godot-dn2cpp PR #14](https://github.com/takuma-komatsu/godot-dn2cpp/pull/14)）
- [PR #127](https://github.com/takuma-komatsu/dn2cpp/pull/127) で、ILDiet によるマネージドアセンブリ削減後も Godot 登録表の trim を適用するようにし、GDScript 内のエスケープシーケンス解決や独立したエクスポートサイズ最適化オプションを検証・導入しました
- [PR #124](https://github.com/takuma-komatsu/dn2cpp/pull/124) で、DeClang による C# メソッド単位の選択的難読化に対応し、Android (arm64-v8a) 向けネイティブビルドでの難読化コンパイルをサポートしました
- [PR #123](https://github.com/takuma-komatsu/dn2cpp/pull/123) で、生成される C++ メソッドシンボルの命名規則を IL2CPP 形式に統一し、可読性とデバッグ性を向上させました

全コミットは <https://github.com/takuma-komatsu/dn2cpp/compare/42deb6848a9b72b8c965ef730daba35d62168a72...@@DOCS_REF@@> を参照してください。

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
