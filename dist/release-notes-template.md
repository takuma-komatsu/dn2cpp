## 概要

本リリースは、Godot @@BASE_VER@@ のエディタに **dn2cpp .NET エクスポートバックエンド**を組み込んだものです。C# ゲームの IL を C++ にトランスパイルし、エンジンが直接ロードするネイティブライブラリへビルドします。したがってエクスポートされたゲームは **.NET ランタイムを同梱しません**。エディタ自体にこれ以外の変更はなく、プロジェクトや C# ソースコード、その他のエクスポートバックエンドは本家（upstream）の動作をそのまま維持しています。

<!--lane:web-->**upstream の Godot は C# ゲームを Web にエクスポートできません**。本エディタではエクスポートできます。

トランスパイラと C++ ランタイムは別プロジェクトとして公開されています: <https://github.com/takuma-komatsu/dn2cpp>

## アセット

| ファイル | 内容 | SHA-256 |
|---|---|---|
<!--lane:editor-macos-->| `@@ASSET_EDITOR_MACOS@@` | macOS エディタ（ad-hoc 署名） | `@@ASSET_EDITOR_MACOS_SHA256@@` |
<!--lane:editor-windows-->| `@@ASSET_EDITOR_WINDOWS@@` | Windows エディタ（x86_64、未署名） | `@@ASSET_EDITOR_WINDOWS_SHA256@@` |
<!--lane:web-->| `@@ASSET_WEB@@` | Web エクスポートテンプレート（Web に必須。後述） | `@@ASSET_WEB_SHA256@@` |
<!--lane:macos-->| `@@ASSET_MACOS@@` | macOS arm64 エクスポートテンプレート（macOS に必須。後述） | `@@ASSET_MACOS_SHA256@@` |
| `SHA256SUMS.txt` | 上記アセットのハッシュ（`shasum` 形式） | — |

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

<!--lane:editor-macos-->
## インストール（macOS エディタ）

```
unzip @@ASSET_EDITOR_MACOS@@
xattr -dr com.apple.quarantine Godot-dn2cpp.app
open Godot-dn2cpp.app
```

> このアプリは **ad-hoc 署名のみで、notarize されていません**。macOS はブラウザでダウンロードしたものに quarantine 属性を付け、それを持つ非 notarize アプリの起動を拒否します。上記の `xattr` コマンドはこのアプリを起動できるようにするための処置で、このアプリ 1 つの quarantine 属性のみを削除します。
>
> `spctl --master-disable` は**実行しないでください**。マシン全体で Gatekeeper を無効化してしまい、未署名のエディタを 1 つ入れることに比べてはるかに大きな変更になります。

<!--/lane-->
<!--lane:editor-windows-->
## インストール（Windows エディタ）

展開する前に、ブラウザが zip ファイルに付与したブロックを解除してください。プロパティ → 全般 → セキュリティで「許可する」を選択します。ブロックは展開後のファイルにも引き継がれます。

`@@ASSET_EDITOR_WINDOWS@@` を展開し、`Godot-dn2cpp\Godot-dn2cpp.exe` を実行してください。普段の編集はこれで行います。

> エディタは**署名されていない**ため、起動時に SmartScreen が発行元不明の警告を表示します。表示された場合は「詳細情報」→「実行」を選択すると起動できます。署名の代わりとなるのが `SHA256SUMS.txt` であり、実行前のハッシュ照合はそのためのものです。

**Windows ゲームをエクスポートする場合も、起動方法は同じです。** ビルドに使う MSVC の環境はエディタが自分で探して取り込むため、Developer Command Prompt for VS から起動する必要はありません。必要なのは Visual Studio の C++ ワークロードが入っていることだけです（*動作要件*を参照）。

隣にある `Godot-dn2cpp.console.exe` はコンソールウィンドウ付きで起動します。エクスポート中のログをその場で流し見たいときの選択肢で、できることは `Godot-dn2cpp.exe` と同じです。

```
cd <展開先>\Godot-dn2cpp
Godot-dn2cpp.console.exe
```

> `Godot-dn2cpp.exe` は隣にあるもの（`GodotSharp\`、`.console.exe`、D3D12 の DLL）を必要とします。**`.exe` だけを取り出すと動作しなくなります。** 別の場所へ移動する場合はフォルダごと移動してください。

展開先の `RELEASE.txt` に、このビルドの識別情報（`release`、`fork_commit`、`engine_provenance`、`toolchain_content_hash`、`editor_version_string`）が記載されています。バグ報告にはこのファイルの内容をそのまま貼り付けてください。

<!--/lane-->
## 動作要件

以下はすべて**エクスポートを実行するマシン**の要件です。エクスポートしたゲームの実行環境には、いずれも必要ありません。

### 対応ホスト

<!--lane:editor-macos-->- **macOS 13 以降 / Apple Silicon（arm64）。** universal ビルドも x86_64 ビルドもありません。
<!--lane:editor-windows-->- **Windows 10 / 11 の x86_64。** arm64 ビルドはありません。

### エクスポート先ごとに必要なもの

表中の ● は必要、— は不要を表します。.NET SDK がどのエクスポート先でも必要なのは、エクスポートがまずプロジェクトの IL をビルドするためです。dn2cpp が .NET ランタイムを取り除くのは*ゲーム*からであって、*エクスポートを実行するマシン*からではありません。テンプレートの入手方法は*エクスポートテンプレート*を、NDK と JDK の指定方法は*Android へのエクスポート*を参照してください。

<!--lane:editor-macos-->
**macOS（Apple Silicon）ホスト**

| エクスポート先 | .NET SDK @@CORELIB_FRAMEWORK@@ | Xcode Command Line Tools | Xcode（full） | Python 3.10 以降 | Node.js 18 以降 | Android NDK・JDK | エクスポートテンプレート |
|---|:-:|:-:|:-:|:-:|:-:|:-:|---|
<!--lane:macos-->| macOS | ● | ● | — | — | — | — | 本リリース同梱 |
| iOS | ● | ● | ● | — | — | — | Godot 公式 |
<!--lane:web-->| Web | ● | — | — | ●※ | ● | — | 本リリース同梱 |
| Android | ● | — | — | — | — | ● | Godot 公式 |

<!--lane:web-->※ リンク時に起動する `emcc` は Python で動きます。同梱の Emscripten SDK が Python を併せ持つのは Windows 版だけで、macOS 版は持っていません。Xcode / Command Line Tools の `/usr/bin/python3` はバージョンが古く `emcc` に拒否されるため、`brew install python` などで別途用意してください。
このホストから Windows および Linux へはエクスポートできません。

<!--/lane-->
<!--lane:editor-windows-->
**Windows（x86_64）ホスト**

| エクスポート先 | .NET SDK @@CORELIB_FRAMEWORK@@ | Visual Studio の C++ ワークロード | Node.js 18 以降 | Android NDK・JDK | エクスポートテンプレート |
|---|:-:|:-:|:-:|:-:|---|
| Windows | ● | ●※ | — | — | Godot 公式 |
<!--lane:web-->| Web | ● | — | ● | — | 本リリース同梱 |
| Android | ● | — | — | ● | Godot 公式 |

※ ビルドは MSVC で行いますが、必要なのはインストールされていることだけです。ビルド環境はエディタが自分で探して取り込むため、特別なシェルから起動する必要はありません。下の注記を参照してください。
このホストから macOS・iOS・Linux へはエクスポートできません。

<!--/lane-->
### エディタに同梱されているもの

いずれもエディタが同梱しているため、別途インストールする必要はありません。

- **cmake と ninja** — マシンに入っているものより同梱のものが優先されます。
<!--lane:web-->- **Emscripten SDK** — Web エクスポートのコンパイラです。別途インストールしないでください。
<!--lane:editor-windows-->- **Python**（Windows 版）— 同梱の Emscripten SDK が自前のものを持っています。

### エクスポートしたゲームに不要なもの

- **.NET ランタイム** — ゲームに同梱されず、プレイするユーザーの環境にも必要ありません。
<!--lane:editor-windows-->- **VC++ 再頒布パッケージ** — ランタイムは静的 CRT でリンクされます（*Windows へのエクスポート*を参照）。

<!--lane:editor-macos-->
> 事前チェックが `clang++` を「見つからない」と報告した場合は、まず `xcode-select --install` を実行してください。導入済みで解決しない場合は起動方法が原因です。Finder から起動した macOS エディタは最小限の `PATH` しか引き継がず、`/usr/bin` の外に置いたコンパイラを参照できません。ターミナルからエディタを起動してください。cmake と ninja はエディタが同梱しているため、この問題の対象外です。
>
> **Web と Android へのエクスポートは、ホストの C++ コンパイラを使いません。** どちらも自前のコンパイラを持つツールチェーンでビルドするため、事前チェックも `clang++` を探しません。Finder から起動したエディタでそのままエクスポートできます。

<!--/lane-->
<!--lane:editor-windows-->
> **Visual Studio が必要なのは、Windows ゲームをエクスポートするときだけです。** Ninja を使うと cmake は `cl.exe` を選びますが、`cl.exe` はマシンの `PATH` には無く、`INCLUDE` / `LIB` / `LIBPATH` も `vcvarsall` を実行済みのシェルの中にしか存在しません。そこでエディタは、起動時ではなくエクスポート時に `vswhere` で Visual Studio を探して `vcvarsall` を実行し、その環境をビルドの子プロセスにだけ渡します。Explorer から起動した `Godot-dn2cpp.exe` でそのままエクスポートできます。すでにビルド環境が入っているシェルから起動した場合は、そのまま何もせずそれを使います。
>
> **Web と Android へのエクスポートは Visual Studio 自体を必要としません。** どちらも MSVC ではなく、Emscripten と Android NDK でビルドするからです。ビルドを駆動する cmake と ninja もエディタが同梱しているため、Android へのエクスポートに別途用意するものは .NET SDK と NDK・JDK だけです。
<!--lane:web-->> Web も同様に、.NET SDK と `node` だけで済みます。

<!--/lane-->
## dn2cpp バックエンドの使い方

プロジェクト → エクスポート → プリセットを選択 → **`dotnet/export_backend`** を **`dn2cpp`** に設定し、あとは通常どおりエクスポートするだけです。上の*動作要件*以外に必要なものはありません。トランスパイラ、ランタイムのソース、エクスポートしたゲームがリンクする vendored ライブラリは、すべてエディタに同梱されています。

エディタ設定 → **`dotnet/export/dn2cpp_toolchain_path`** で別のツールチェーンレイアウトを指定できます。同梱のものではなく自分でビルドした dn2cpp を使用したい場合は、ここで指定します。同様に **`dotnet/export/dn2cpp_cmake_path`** と **`dotnet/export/dn2cpp_ninja_path`** で、同梱の cmake / ninja を自前のものに差し替えられます。

## エクスポートテンプレート

下に挙げていないターゲットは、**標準の Godot @@BASE_VER@@ .NET エクスポートテンプレート**をそのまま使います。通常どおりインストールしてください。このフォークが Web 以外でエンジンに加えている差分は `WEB_ENABLED` のガード 1 か所だけなので、同じバージョンの upstream テンプレートで問題ありません。

<!--lane:web-->
- **Web** — upstream は C# に対応した Web テンプレートを配布しておらず、また配布できないため、`@@ASSET_WEB@@` が必要です。
<!--/lane-->
<!--lane:macos-->
- **macOS** — `@@ASSET_MACOS@@` が必要です。これは upstream のテンプレート*そのもの*で、universal バイナリから arm64 アーキテクチャのみを抽出したものです。エクスポータがアーキテクチャを名前で探すためです。詳細は下の*macOS へのエクスポート*を参照してください。
<!--/lane-->
<!--lane:editor-windows-->

## Windows へのエクスポート

標準の Godot @@BASE_VER@@ エクスポートテンプレートをそのまま使い、追加の設定はありません。条件は 1 つ、Visual Studio の C++ ワークロードが入っていることだけです（*動作要件*を参照）。エディタの起動方法は問いません。

エクスポートしたゲームは **VC++ 再頒布パッケージを必要としません**。ランタイムは静的 CRT でリンクされます。

Windows ホストからエクスポートできるのは **Windows / Android / Web** だけです。macOS へのエクスポートはエラーになります。iOS は Apple SDK の検証に失敗し、「Xcode をインストールしてください」という、Windows 上では意味を成さないメッセージが表示されます。Linux はまだ対応していません。
<!--/lane-->

<!--lane:macos-->
## macOS へのエクスポート

バックエンドはエクスポートを実行しているマシン向けにゲームをコンパイルし、クロスコンパイルはできません。そのため macOS のプリセットはアーキテクチャを**ひとつ**だけ選ぶ必要があり、それがこのエディタ自身の `arm64` です。upstream のアーカイブには `universal` のバイナリしか入っておらず、エクスポータは `godot_macos_<cfg>.<architecture>` という正確な名前でバイナリを探すため、何も見つけられません。`@@ASSET_MACOS@@` があるのはそのためです。設定は 2 か所必要で、そのうち片方はエクスポートダイアログにありません:

1. プリセットの **カスタムテンプレート → リリース**と**デバッグ**の両方に、`@@ASSET_MACOS@@` そのものを指定します。この欄に指定するのはアーカイブそのものであって、その中身ではありません。1 つのアーカイブが両方を兼ねます。
2. エディタを終了してから、アーキテクチャをプリセットに手動で追記します。macOS のエクスポータは `binary_format/architecture` を保存はするものの表示しないため、ダイアログからは設定できません。`export_presets.cfg` の、macOS プリセットの `[preset.N.options]` ブロックに次を追加します:

   ```
   binary_format/architecture="arm64"
   ```

既定値の `universal` のままにすると、エクスポートはこの設定名を示すエラーで失敗します。`universal` は 2 つのアーキテクチャを選ぶ指定であり、バックエンドが対応できるのは 1 つだからです。
<!--/lane-->

<!--lane:web-->
## Web へのエクスポート

Web のプリセットで:

- **カスタムテンプレート（リリース）**: `@@ASSET_WEB@@` をアーカイブのまま指定します。
- **`variant/extensions_support` = `true`** — ゲームは GDExtension ライブラリと同じ経路で side module としてロードされます。
- **`variant/thread_support` = `false`** — ゲームのライブラリは `-pthread` なしでビルドされ、Emscripten はそれをスレッド有効のメインモジュールへロードすることを拒否します。

ゲームをコンパイルするのは、エディタが同梱する **Emscripten @@EMSDK_VERSION@@** の SDK です。これは `@@ASSET_WEB@@` 自体をビルドしたのと同じものです。同じ SDK を使うのは単に手間を省くためではなく、ゲームはそのテンプレートの side module であり、両者は例外方式と動的リンクの ABI を一致させる必要があるためです。自分で SDK をインストールする必要はなく、必要なのは `node` だけです（*動作要件*を参照）。

このプラットフォームには、他のターゲットには無い固有の制約があります:

- **スレッドがありません。** `Task.Run`、`Thread`、`Timer` は例外を投げます。
- **HTTP トランスポートがありません。** ブラウザには TCP ソケット層が無いため、`HttpClient` はリンクは通りますが、すべての呼び出しがプラットフォーム名を含む `HttpRequestException` で失敗します。
- **`FileStream` はリンクできません。** インターセプトされた `File.*` のサブセットはインメモリファイルシステム上で動作します。
- **`System.IO` は `res://` を読めません**（これはここに限らず、どのプラットフォームでも同じです）。`ProjectSettings.GlobalizePath(...)` で変換するか、パックされたアーカイブを読めるエンジン側（`FileAccess`、`ResourceLoader`）経由で読んでください。
<!--/lane-->

## Android へのエクスポート

Godot 公式の Android エクスポートテンプレートに加えて、次の 2 つを用意する必要があります。

- **NDK** — 環境変数 `ANDROID_NDK_ROOT` か `ANDROID_NDK_HOME` を参照します。どちらも設定されていない場合は、エディタ設定 `export/android/android_sdk_path` 配下の `ndk/` から探します。
- **JDK** — エディタ設定 `export/android/java_sdk_path` に指定してください。**`PATH` の `java` は Godot が参照しません。**

## Provenance

| | |
|---|---|
| fork のコミット | `@@FORK_COMMIT@@` |
| upstream ベース | `@@BASE_PIN@@`（Godot @@BASE_VER@@） |
| エンジンの provenance | `@@ENGINE_PROVENANCE@@` |
<!--lane:editor-macos-->| macOS エディタの `--version` | `@@EDITOR_VERSION_STRING_MACOS@@` |
<!--lane:editor-macos-->| macOS エディタの dn2cpp コミット | `@@DN2CPP_COMMIT_MACOS@@` |
<!--lane:editor-macos-->| macOS エディタのツールチェーン content hash | `@@TOOLCHAIN_CONTENT_HASH_MACOS@@` |
<!--lane:editor-macos-->| macOS エディタの prebuilt ランタイム軸 | `@@PREBUILT_AXES_MACOS@@` |
<!--lane:editor-macos-->| macOS エディタの同梱ビルドツール | cmake `@@CMAKE_VERSION_MACOS@@` + ninja `@@NINJA_VERSION_MACOS@@` |
<!--lane:editor-windows-->| Windows エディタの `--version` | `@@EDITOR_VERSION_STRING_WINDOWS@@` |
<!--lane:editor-windows-->| Windows エディタの dn2cpp コミット | `@@DN2CPP_COMMIT_WINDOWS@@` |
<!--lane:editor-windows-->| Windows エディタのツールチェーン content hash | `@@TOOLCHAIN_CONTENT_HASH_WINDOWS@@` |
<!--lane:editor-windows-->| Windows エディタの prebuilt ランタイム軸 | `@@PREBUILT_AXES_WINDOWS@@` |
<!--lane:editor-windows-->| Windows エディタの同梱ビルドツール | cmake `@@CMAKE_VERSION_WINDOWS@@` + ninja `@@NINJA_VERSION_WINDOWS@@` |
<!--lane:web-->| Web テンプレートのビルドに使用 | emcc `@@EMCC@@`（Emscripten `@@EMSDK_VERSION@@`） |
<!--lane:macos-->| macOS テンプレートの抽出元 | upstream `macos.zip`、sha256 `@@UPSTREAM_MACOS_SHA256@@` |

> エディタの `--version` 文字列が示すのは、そのバイナリが**リンクされた**ときのコミットです。エンジンのソースに変更が無いツリーは再リンクされないため、このコミットはタグのコミットより古いことがあります。どちらも同じバイナリを指しています。バグ報告にはこの文字列をそのまま貼り付けてください。エディタはそれぞれ別のホストでパッケージされるので、エンジンの行より下はエディタごとに分かれています。`dn2cpp コミット` がエディタ間で食い違うのも同じ理由で、パッケージした時刻が違うだけです。エンジンのソースは上の `fork のコミット` 1 つが示すとおり共通です。

## ライセンス

Godot は MIT で、dn2cpp バックエンドも MIT です。エディタが同梱するツールチェーンには、エクスポートしたゲームがリンクする vendored な C ライブラリが加わり、それぞれ `third_party/` 以下に自身の条項があります。

<!--lane:web-->さらに Emscripten SDK も同梱しています。upstream の Emscripten（MIT / University of Illinois NCSA）、その下の LLVM および Binaryen（LLVM 例外付き Apache-2.0）、エクスポートした Web ゲームがリンクする wasm ランタイムライブラリ（libc++、libc++abi、libunwind、compiler-rt、および musl 由来の libc）、それにリンク時に `emcc` が読み込む npm パッケージ群です。同梱したバイナリに対応するライセンス文書は `emsdk/LICENSES/` 以下にまとめてあります。ただし LLVM と Binaryen の実行ファイルについては、upstream のアーカイブ自体がライセンス文書をどこにも含んでいないため `emsdk/LICENSES/` にもありません。条項は <https://llvm.org/LICENSE.txt> を参照してください。

## トラブルシューティング

**エクスポートの成否は、ログでしか判定できません。** エクスポートに失敗しても、エディタは「警告付きで完了」と表示します（エクスポートプラグインのエラーが警告レベルに落とされるためです）。終了コードも 0 を返し、それらしい成果物も残るため、表示にも成果物の有無にも成否は現れません。

ログは `<プロジェクトの出力パス>/dn2cpp/logs/export-<timestamp>.log` に、最新 20 世代が保存されます。

事前チェックが次の行を出力した場合は、この行に続けて不足しているツールの名前と、その導入方法が示されます。*動作要件*を確認してください。

```
The dn2cpp export backend is missing tools it cannot build without:
```
<!--lane:editor-windows-->

Windows でこのメッセージが出る場合は、Visual Studio の C++ ワークロードが入っていないか、エディタの探索が届かない場所にインストールされています。後者であれば、ビルド環境をロード済みのシェル（Developer Command Prompt for VS）からエディタを起動すれば、それがそのまま使われます。
<!--/lane-->

## 既知の制限

<!--lane:editor-macos-->
- macOS エディタは **notarize されておらず**（上の*インストール（macOS エディタ）*を参照）、**arm64 のみ**です。エクスポートされる macOS ゲームも同様で、universal ビルドも Intel ビルドも無く、クロスコンパイルもできません。
<!--/lane-->
<!--lane:editor-windows-->
- Windows エディタは **x86_64 のみ**で**未署名**です。arm64 ビルドはありません。
- **Windows ゲームのエクスポートには MSVC が必要**なため、その場合のみ Visual Studio の C++ ワークロードが要ります。Web と Android へのエクスポートは対象外です（*動作要件*を参照）。
<!--/lane-->
<!--lane:!editor-windows-->
- このリリースには **Windows エディタが含まれていません**。ホストプラットフォームとしてはバックエンド自体が対応しています。
<!--/lane-->
- このリリースには **Linux エディタが含まれていません**。ホストプラットフォームとしてはバックエンド自体が対応しています。
- 同梱の prebuilt ランタイムキャッシュは、パッケージしたホストのツールチェーンをキーにしています。clang や NDK が異なるマシンでは、そのターゲットへの初回エクスポート時にランタイムをソースからビルドします（一度だけ遅くなりますが、結果はバイト単位で同一です）。Web 軸だけは例外です。そのツールチェーンはバンドルに同梱されているため、どのマシンでも同じキーになります。
