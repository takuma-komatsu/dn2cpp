## 概要

本リリースは、Godot @@BASE_VER@@ のエディタに **dn2cpp .NET エクスポートバックエンド**を組み込んだものです。C# ゲームの IL を C++ にトランスパイルし、エンジンが直接ロードするネイティブライブラリへビルドします。したがってエクスポートされたゲームは **.NET ランタイムを同梱しません**。エディタ自体にこれ以外の変更はなく、プロジェクトや C# ソースコード、その他のエクスポートバックエンドは本家（upstream）の動作をそのまま維持しています。

<!--lane:web-->**upstream の Godot は C# ゲームを Web にエクスポートできません**。本エディタではエクスポートできます。

トランスパイラと C++ ランタイムは別プロジェクトとして公開されています: <https://github.com/takuma-komatsu/dn2cpp>

## 前回リリース（@@PREV_VERSION@@）からの変更

- **破壊的変更**: アセットのファイル名から重複していた `dn2cpp` を削除し、`Godot-<バージョン>-macos-arm64.zip` のような形に変更（展開後の `.app` / `.exe` と zip 内トップディレクトリの名前は従来どおり `Godot-dn2cpp`）（dn2cpp#18）
- **破壊的変更**: リリースのバージョン表記を `<Godot バージョン>-dn2cpp.<X>.<Y>` に変更 — エディタ（フォーク）が更新されると `X`、dn2cpp だけの更新なら `Y` が上がる（dn2cpp#18）
- **破壊的変更**: GDExtension 出力のライブラリ名を `libdn2cpp.dylib` / `libdn2cpp.so` / `dn2cpp.dll` に固定 — CLI の `--gdextension` を直接使っている場合は `.gdextension` の `[libraries]` の書き換えが必要（エディタからのエクスポートは別経路のため影響なし）（dn2cpp#19）
- GC の実装を Unity Technologies の bdwgc フォークへ差し替え、インクリメンタル GC（Godot ランの既定）が必要とする書き込みバリアを追加（dn2cpp#10）
- プリミティブ・enum を `constrained.` 経由で `object.Equals` に渡すとクラッシュしていた問題を修正（dn2cpp#15）
- タスクの完了待ちが、実行可能な継続が残っているのに稀にデッドロックと判定していた問題を修正（dn2cpp#13）
- インストール手順・動作要件・エクスポート手順・トラブルシューティング・既知の制限を、リリースノートからエディタ利用ガイド（`docs/EDITOR-GUIDE.ja.md`）へ移動（dn2cpp#17）

全コミットは <https://github.com/takuma-komatsu/dn2cpp/compare/d89b8f474f6b72be56ec86dbaf46b3e7d88e5569...be3bd576e0be2be739886ffbf9045c71604120e1> を参照してください。

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

## 使い方・インストール・動作要件

インストール手順・動作要件・各プラットフォームへのエクスポート手順・トラブルシューティング・既知の制限は、リリース間で変わらないため**エディタ利用ガイド**にまとめてあります。以下のリンクは本リリースを切った時点のコミットに固定してあります。

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

## Provenance

| | |
|---|---|
| fork のコミット | `@@FORK_COMMIT@@` |
| upstream ベース | `@@BASE_PIN@@`（Godot @@BASE_VER@@） |
| エンジンの provenance | `@@ENGINE_PROVENANCE@@` |
| .NET SDK（対象フレームワーク） | `@@CORELIB_FRAMEWORK@@` |
<!--lane:editor-macos-->| macOS エディタの `--version` | `@@EDITOR_VERSION_STRING_MACOS@@` |
<!--lane:editor-macos-->| macOS エディタの dn2cpp コミット | `@@DN2CPP_COMMIT_MACOS@@` |
<!--lane:editor-macos-->| macOS エディタのツールチェーン content hash | `@@TOOLCHAIN_CONTENT_HASH_MACOS@@` |
<!--lane:editor-macos-->| macOS エディタの prebuilt ランタイム軸 | `@@PREBUILT_AXES_MACOS@@` |
<!--lane:editor-macos-->| macOS エディタの同梱ビルドツール | cmake `@@CMAKE_VERSION_MACOS@@` + ninja `@@NINJA_VERSION_MACOS@@` |
<!--lane:editor-macos-->| macOS エディタ同梱 Emscripten の Node.js | `@@NODE_VERSION_MACOS@@` |
<!--lane:editor-windows-->| Windows エディタの `--version` | `@@EDITOR_VERSION_STRING_WINDOWS@@` |
<!--lane:editor-windows-->| Windows エディタの dn2cpp コミット | `@@DN2CPP_COMMIT_WINDOWS@@` |
<!--lane:editor-windows-->| Windows エディタのツールチェーン content hash | `@@TOOLCHAIN_CONTENT_HASH_WINDOWS@@` |
<!--lane:editor-windows-->| Windows エディタの prebuilt ランタイム軸 | `@@PREBUILT_AXES_WINDOWS@@` |
<!--lane:editor-windows-->| Windows エディタの同梱ビルドツール | cmake `@@CMAKE_VERSION_WINDOWS@@` + ninja `@@NINJA_VERSION_WINDOWS@@` |
<!--lane:editor-windows-->| Windows エディタ同梱 Emscripten の Node.js | `@@NODE_VERSION_WINDOWS@@` |
<!--lane:web-->| Web テンプレートのビルドに使用 | emcc `@@EMCC@@`（Emscripten `@@EMSDK_VERSION@@`） |
<!--lane:macos-->| macOS テンプレートの抽出元 | upstream `macos.zip`、sha256 `@@UPSTREAM_MACOS_SHA256@@` |

> エディタの `--version` 文字列が示すのは、そのバイナリが**リンクされた**ときのコミットです。エンジンのソースに変更が無いツリーは再リンクされないため、このコミットはタグのコミットより古いことがあります。どちらも同じバイナリを指しています。バグ報告は <https://github.com/takuma-komatsu/dn2cpp/issues> へ、この文字列をそのまま貼り付けてください — エンジン（フォーク）側の問題もそちらで受け付けます。フォークのリポジトリは issue を無効にしてあります。エディタはそれぞれ別のホストでパッケージされるので、エンジンの行より下はエディタごとに分かれています。`dn2cpp コミット` がエディタ間で食い違うのも同じ理由で、パッケージした時刻が違うだけです。エンジンのソースは上の `fork のコミット` 1 つが示すとおり共通です。

## ライセンス

Godot は MIT で、dn2cpp バックエンドも MIT です。エディタが同梱するツールチェーンには、エクスポートしたゲームがリンクする vendored な C ライブラリが加わり、それぞれ `third_party/` 以下に自身の条項があります。

<!--lane:web-->さらに Emscripten SDK も同梱しています。upstream の Emscripten（MIT / University of Illinois NCSA）、その下の LLVM（LLVM 例外付き Apache-2.0）および Binaryen（Apache-2.0）、エクスポートした Web ゲームがリンクする wasm ランタイムライブラリ（libc++、libc++abi、libunwind、compiler-rt、および musl 由来の libc）、それにリンク時に `emcc` が読み込む npm パッケージ群です。同梱したバイナリに対応するライセンス文書は、LLVM と Binaryen のものも含めてすべて `emsdk/LICENSES/` 以下にまとめてあります — 両者は upstream のアーカイブ自体がライセンス文書を含んでいないため、ビルド元のリビジョンから取得した原文を dn2cpp 側で同梱しています。
