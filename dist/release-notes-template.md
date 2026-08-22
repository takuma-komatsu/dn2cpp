## 概要

本リリースは、Godot @@BASE_VER@@ のエディタに **dn2cpp .NET エクスポートバックエンド**を組み込んだものです。C# ゲームの IL を C++ にトランスパイルし、エンジンが直接ロードするネイティブライブラリへビルドします。したがってエクスポートされたゲームは **.NET ランタイムを同梱しません**。エディタにはこのバックエンドに加え、ヘッドレスエクスポートでエクスポートプラグインのエラーを失敗として扱う変更があります。このエラー判定はすべてのエクスポートバックエンドに適用されます。プロジェクトと C# ソースコードは本家（upstream）との互換性を維持しています。

<!--lane:web-->**upstream の Godot は C# ゲームを Web にエクスポートできません**。本エディタではエクスポートできます。

トランスパイラと C++ ランタイムは別プロジェクトとして公開されています: <https://github.com/takuma-komatsu/dn2cpp>

## 前回リリース（@@PREV_VERSION@@）からの変更

- [PR #54](https://github.com/takuma-komatsu/dn2cpp/pull/54) で、ZLinq・ZString・MessagePipe のサポートと検証ゲートを追加し、オープンジェネリック定義の閉じた祖先型の保持や Span `TryFormat` の `IFormatProvider` ルーティングを改善しました
- [PR #53](https://github.com/takuma-komatsu/dn2cpp/pull/53) で、R3 ライブラリのサポートと非同期ランタイムプリミティブ（Timer、アドレス取得されたカットインターセプトのボディ合成など）を追加しました

全コミットは <https://github.com/takuma-komatsu/dn2cpp/compare/ba13925269ae28abd2489aca76a86b34257456b4...8bccf337a5b8f05076b3be196db5188b88c8e97b> を参照してください。

## アセット

| ファイル | 内容 | SHA-256 |
|---|---|---|
<!--lane:editor-macos-->| `@@ASSET_EDITOR_MACOS@@` | macOS エディタ（ad-hoc 署名） | `@@ASSET_EDITOR_MACOS_SHA256@@` |
<!--lane:editor-windows-->| `@@ASSET_EDITOR_WINDOWS@@` | Windows エディタ（x86_64、未署名） | `@@ASSET_EDITOR_WINDOWS_SHA256@@` |
<!--lane:web-->| `@@ASSET_WEB@@` | Web エクスポートテンプレート（Web に必須。後述） | `@@ASSET_WEB_SHA256@@` |
<!--lane:macos-->| `@@ASSET_MACOS@@` | macOS arm64 エクスポートテンプレート（macOS に必須。後述） | `@@ASSET_MACOS_SHA256@@` |
| `SHA256SUMS.txt` | 上記アセットのハッシュ（`shasum` 形式） | — |

<!--lane:!editor-windows-->このリリースには **Windows エディタが含まれていません**。ホストプラットフォームとしてはバックエンド自体が対応しています。

展開する前に、ダウンロードしたファイルを検証してください:

```
shasum -a 256 -c SHA256SUMS.txt
```

<!--lane:editor-windows-->
Windows には `shasum` がないため、PowerShell で次のコマンドを実行してください:

```
Get-FileHash @@ASSET_EDITOR_WINDOWS@@ -Algorithm SHA256
```

出力は大文字の 16 進表記です。`SHA256SUMS.txt` の該当行とは大文字小文字を無視して照合してください。
<!--/lane-->

## エディタ利用ガイド

インストール手順・動作要件・各プラットフォームへのエクスポート手順・トラブルシューティング・既知の制限・ライセンスは、リリース間で変わらないため**エディタ利用ガイド**にまとめてあります。以下のリンクは本リリースを切った時点のコミットに固定してあります。

- [インストール](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md#インストール)
- [動作要件](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md#動作要件)
- [dn2cpp バックエンドの使い方](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md#dn2cpp-バックエンドの使い方)
- [エクスポートテンプレート](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md#エクスポートテンプレート)
- [Windows へのエクスポート](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md#windows-へのエクスポート)
- [macOS へのエクスポート](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md#macos-へのエクスポート)
- [Web へのエクスポート](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md#web-へのエクスポート)
- [Android へのエクスポート](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md#android-へのエクスポート)
- [トラブルシューティング](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md#トラブルシューティング)
- [既知の制限](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md#既知の制限)
- [ライセンス](https://github.com/takuma-komatsu/dn2cpp/blob/@@DOCS_REF@@/docs/EDITOR-GUIDE.ja.md#ライセンス)

## Provenance

| | |
|---|---|
| fork のコミット | `@@FORK_COMMIT@@` |
| upstream ベース | `@@BASE_PIN@@`（Godot @@BASE_VER@@） |
| エンジンの provenance | `@@ENGINE_PROVENANCE@@` |
| .NET SDK（対象フレームワーク） | `@@CORELIB_FRAMEWORK@@` |
| dn2cpp のコミット | `@@DN2CPP_COMMIT@@` |
<!--lane:editor-macos-->| macOS エディタの `--version` | `@@EDITOR_VERSION_STRING_MACOS@@` |
<!--lane:editor-macos-->| macOS エディタのツールチェーン content hash | `@@TOOLCHAIN_CONTENT_HASH_MACOS@@` |
<!--lane:editor-macos-->| macOS エディタの prebuilt ランタイム軸 | `@@PREBUILT_AXES_MACOS@@` |
<!--lane:editor-macos-->| macOS エディタの同梱ビルドツール | cmake `@@CMAKE_VERSION_MACOS@@` + ninja `@@NINJA_VERSION_MACOS@@` |
<!--lane:editor-macos-->| macOS エディタ同梱 Emscripten の Node.js | `@@NODE_VERSION_MACOS@@` |
<!--lane:editor-windows-->| Windows エディタの `--version` | `@@EDITOR_VERSION_STRING_WINDOWS@@` |
<!--lane:editor-windows-->| Windows エディタのツールチェーン content hash | `@@TOOLCHAIN_CONTENT_HASH_WINDOWS@@` |
<!--lane:editor-windows-->| Windows エディタの prebuilt ランタイム軸 | `@@PREBUILT_AXES_WINDOWS@@` |
<!--lane:editor-windows-->| Windows エディタの同梱ビルドツール | cmake `@@CMAKE_VERSION_WINDOWS@@` + ninja `@@NINJA_VERSION_WINDOWS@@` |
<!--lane:editor-windows-->| Windows エディタ同梱 Emscripten の Node.js | `@@NODE_VERSION_WINDOWS@@` |
<!--lane:web-->| Web テンプレートのビルドに使用 | emcc `@@EMCC@@`（Emscripten `@@EMSDK_VERSION@@`） |
<!--lane:macos-->| macOS テンプレートの抽出元 | upstream `macos.zip`、sha256 `@@UPSTREAM_MACOS_SHA256@@` |

> エディタの `--version` 文字列が示すのは、そのバイナリが**リンクされた**ときのコミットです。エンジンのソースに変更が無いツリーは再リンクされないため、このコミットはタグのコミットより古いことがあります。どちらも同じバイナリを指しています。バグ報告は <https://github.com/takuma-komatsu/dn2cpp/issues> へ、この文字列をそのまま貼り付けてください — エンジン（フォーク）側の問題もそちらで受け付けます。フォークのリポジトリは issue を無効にしてあります。エディタはそれぞれ別のホストでパッケージされるので、エディタ名の付いた行はホストごとの値です。`fork のコミット` と `dn2cpp のコミット` は両エディタ共通で、食い違ったままリリースを作ることはできません。
