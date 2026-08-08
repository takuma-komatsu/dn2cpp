# RELEASE — godot-dn2cpp エディタのリリース手順

上から順に実行すれば、macOS と Windows の 2 ホストで 1 本のリリースが完成する。

**この文書は手順書であり、設計の説明ではない。**なぜこの形なのかは
`docs/EDITOR-EXPORT-DESIGN.md` §11 にある。ここでは繰り返さない。

---

## 0. 最初に読むチェックリスト

### 必要なホスト

| ホスト | 焼くレーン | 必須 |
|---|---|---|
| macOS（Apple Silicon） | `editor-macos` / `web` / `macos` | Xcode Command Line Tools、Xcode（iOS 軸）、Android NDK、real な python3、`gh`（認証済み） |
| Windows（x86_64） | `editor-windows` | Visual Studio の C++ ワークロード、Android NDK、ホストの Python 3、`gh`（認証済み） |

- **順序は固定**（macOS → Windows）。理由は §B の冒頭。
- レーン名は 4 つで固定（`editor-macos` `editor-windows` `web` `macos`）。
  `dist/release-github.sh` のレーン表がその唯一の定義。
- **シェルは普段のもので構わない**（macOS は zsh のままで通る）。Windows だけは
  Git Bash / MSYS が前提。macOS で python3 の版が問われるのは Web エクスポート
  ゲート（`gates/build-and-run-godot-editor-export-web.sh` ほか）の中だけで、
  Homebrew などの実 python3 が PATH の先頭にあれば発火しない。

### 事前に決めること

**バージョン番号。** 形式は `<godot のバージョン>-dn2cpp.<n>` 以外を受け付けない
（`dist/release-github.sh`、両エディタパッケージャの 3 か所が独立に die する）。
エディタパッケージャはさらに、`<godot のバージョン>` がフォークの `version.py`
と一致することを要求する。

以降のコマンドは、両ホストでこの変数を置いてから実行する:

```bash
V=<godot のバージョン>-dn2cpp.<n>      # 例: 4.7.1-dn2cpp.1 の形
REPO=takuma-komatsu/godot-dn2cpp      # --repo の既定値。変えるなら両ホストで揃える
```

### 所要時間の目安

macOS（Apple Silicon）で、**セットアップ済み・温かいツリー**を実測した値。冷たい
ツリーは未実測で、桁が変わる。特に **エンジン C++ に差分があって
`gates/setup-godot-fork.sh` が scons ビルドに入ると、そこだけで別途 1〜2 時間**。

| 段階 | 温かいツリー |
|---|---|
| `gates/setup-buildtools.sh` / `gates/setup-emsdk.sh` | 各 1 秒未満（どちらもスキップ） |
| `gates/selfhost-emit.sh` | 約 13 分（スタンプ一致なら即） |
| `gates/setup-godot-fork.sh` | 約 2.5 分（engine hash 一致でエディタとテンプレートはスキップ、ツールチェーンバンドルと managed assemblies のみ再実行） |
| `dist/package-web-template.sh` | 1 秒未満 |
| `dist/package-macos-template.sh` | 約 10 秒 |
| `dist/package-editor-macos.sh`（smoke 込み） | 約 6.5 分 |
| `dist/smoke-test.sh` | 約 1 分 |
| `dist/release-github.sh --dry-run` | 約 10 秒 |

**2 ホストを同じ日に走らせる必要はないが、その間に両リポジトリの `main` /
`dn2cpp/main` を進めてはいけない**（§0-C）。

---

## フェーズ 0: 事前確認（両ホスト共通）

### 0-A. 両リポジトリの状態

`dist/release-github.sh` は、リモートに無いコミットを名指すリリースを拒否する。
**push はこの手順の前提であって、スクリプトはやってくれない。**

```bash
# フォーク（godot-dn2cpp）
git -C <FORK> fetch --tags --prune --prune-tags --force origin
git -C <FORK> checkout dn2cpp/main
git -C <FORK> status --porcelain --untracked-files=no    # → 空であること
git -C <FORK> rev-parse HEAD                             # ← 控える（両ホストで一致必須）
git -C <FORK> merge-base --is-ancestor HEAD origin/dn2cpp/main && echo pushed

# dn2cpp — 1 番目のホストは main、2 番目は 1 番目が使ったコミット（下記）
git -C <DEV> fetch origin
git -C <DEV> checkout main
git -C <DEV> rev-parse HEAD                              # ← 控える
git -C <DEV> merge-base --is-ancestor HEAD origin/main && echo pushed
```

- フォークの追跡ファイルに未コミット変更があると die する（`bin/` などの
  untracked は対象外）。
- `dn2cpp` 側で確認するリモートは **`origin`（公開リポジトリ）**。`archive`
  にしか無いコミットは、ノートがリンクする先から辿れないので die する。
- タグが古いコミットを指したまま残っていると die する。消してから fetch し直す:
  `git -C <FORK> tag -d $V && git -C <FORK> fetch --tags origin`

### 0-B. ゲートが green か

リリースはゲートを走らせない。`dist/package-editor-*.sh` の `--smoke` が
エディタエクスポートの 2 ゲートを流すだけで、それ以外は一切検証しない。
**`gates/pre-merge.sh` が green な `main` からリリースを切ること。**

### 0-C. 両ホストが同じコミットにいること

- **フォークのコミット**は `dist/release-github.sh` が強制する。両エディタの
  `fork_commit` が一致し、かつタグを張るコミットと一致していなければ die。
- **`engine_provenance` / `base_pin` / `corelib_framework`** も全レーン一致を
  強制される（`.NET SDK` のバージョン違いは `corelib_framework` で落ちる）。
- **dn2cpp のコミットは強制されない。** レーンごとに別の値でも通り、ノートは
  エディタごとに行を分けて両方を印字する。これは既知の未解決点で、
  `docs/STATUS.md` に行がある。だから **2 番目のホストは `main` の先端では
  なく、1 番目のホストが使ったコミットをチェックアウトする**:

  ```bash
  git -C <DEV> checkout "$(sed -n 's/^dn2cpp_commit=//p' artifacts/release/editor-macos.metadata)"
  ```

  引き渡された metadata がその値の唯一の出所。**`main` を追うと 2 つの経路で
  壊れる** —— 1 番目のホストがリリースを切ったあとに `main` が進んでいれば
  ノートが 2 つの dn2cpp コミットを印字し、その進んだ分がまだ push されて
  いなければ「`origin/main` に含まれない」で die する。
- **リリースノートは、最後に走ったホストの `dist/release-notes-template.md`
  で全文が再レンダリングされる。**テンプレートが両ホストで違えば、後から
  走ったホストの版が公開本文になる。これがホスト間でコミットを揃える実務上の
  最大の理由。

### 0-D. 前回リリースの資産を退避する（**新バージョンの第一手**）

`dist/release-github.sh` は `SHA256SUMS.txt` の**行集合とアクティブレーンの
アセット集合が両方向で一致する**ことを要求する。一方、各パッケージャは
`SHA256SUMS.txt` の**自分の行しか**差し替えない。前回の資産を残したまま新
バージョンを焼くと、旧 3 行 + 新 3 行の 6 行になって
`rows for no active lane` で die する。

```bash
mv artifacts/release artifacts/release-<旧バージョン>
```

現行のレーン表に無い旧世代の metadata（例: 昔の `editor.metadata`）も、これで
同時に消える。**`--out` の中身を選んで消すのではなく、ディレクトリごと退けること。**

---

## フェーズ A: macOS ホスト

### A-1. セットアップ 2 本（**必ず既定の `--out` で**）

```bash
cd <DEV>
./gates/setup-buildtools.sh      # 先に。理由は下
./gates/setup-emsdk.sh
```

- **`--out` を指定しないこと。** 既定の出力先は、後段の解決関数
  （`dn2cpp_emsdk_resolve` と `dist/package-toolchain.sh` の buildtools 解決）が
  ピンのバージョンとホストタグから組み立てるパスと同一。別の場所に置くと、
  バンドルに何も同梱されないまま静かに進む。
- **`setup-buildtools.sh` は `setup-godot-fork.sh` より先。**
  cmake/ninja 無しでのパッケージングは警告どまりで成功するが、
  `dist/package-editor-macos.sh` は size report の `buildtools` 行が無いと die する。
  同じ理由で `emsdk` 行と `emsdk/node` 行も必須。
- どちらも冪等。ピンの sha256 を検証してから展開するので、途中で切れた
  ダウンロードが完全なアーカイブとして読まれることはない。

### A-2. self-host CLI

再ベイクが要るかは、ソースの最終コミット日時と既存バイナリの mtime を並べれば
判断できる:

```bash
git log -1 --format=%ci -- src runtime third_party
ls -l artifacts/selfhost-fullcli/dn2cpp
```

ソースの方が新しければ焼き直す。

```bash
./gates/selfhost-emit.sh
cat artifacts/selfhost-fullcli/dn2cpp.src-hash
```

`dist/package-editor-macos.sh` は、このスタンプが `src/` `runtime/`
`third_party/` の現在のハッシュと一致しない限り die する。

**`dist/package-toolchain.sh` の自動再ベイクを当てにしないこと。** stale を
検出すると自分で `gates/selfhost-emit.sh` を呼ぶが、**それが失敗しても warning
を出して続行し、古いバイナリをそのまま詰める**。手で先に走らせて成功を見るのが
正しい。

### A-3. フォークのキャッシュとツールチェーンバンドル

```bash
./gates/setup-godot-fork.sh 2>&1 | tee /tmp/setup-godot-fork.log
```

エンジンのソースがベースコミットと同一なら、pristine clone のビルド済み
バイナリを再利用する。エンジン C++ に差分があると scons ビルドに入る
（長い。バックグラウンドで走らせる）。

ログの `engine hash:` を控える。**両ホストでこの値が一致しないと、
`engine_provenance` の照合で必ず死ぬ**（1 リリース = 1 エンジンツリー）。

このログには MSBuild の出力がホストのロケールで混ざる。**ログを機械判定に
使うなら英語を前提にしないこと。**

### A-4. Web テンプレートを焼く

```bash
./gates/setup-godot-fork-web.sh 2>&1 | tee /tmp/setup-godot-fork-web.log
```

- engine hash が動いていなければ、ここは実質スキップになる。フォークの差分が
  managed 側だけなら焼き直しは起きず、**zip の中身は前バージョンとバイト一致に
  なりうる**。それでも A-5 の `dist/package-web-template.sh` は必ず走らせる ——
  アセット名にバージョンが入り、`web.metadata` の `release_version` は全レーン
  一致検査の対象だから、前バージョンのファイルを流用することはできない。
- `CRI=1` を付けないこと。CRI 版は第三者 SDK を含む variant で、
  リリースアセットにしてはならない（`dist/package-web-template.sh` が
  flavor を検査して落とす）。

### A-5. アセットを焼く（**web → macos → editor の順で**）

```bash
./dist/package-web-template.sh   --version "$V"
./dist/package-macos-template.sh --version "$V"
./dist/package-editor-macos.sh   --version "$V" 2>&1 | tee /tmp/pkg-editor-macos.log
```

- **順序は強制。** `dist/package-editor-macos.sh` の smoke は、
  `<out>/godot-dn2cpp-$V-web-template.zip` とその `.provenance` を
  **リリース資産として**読み、それでエクスポートする。無ければ die して
  「先に Web テンプレートを切れ」と言う。同梱 SDK の `emcc_version` が
  `web.metadata` と一致することも要求する。食い違ったら順に:
  `FORCE=1 ./gates/setup-godot-fork-web.sh` → `./dist/package-web-template.sh --version "$V"`。
- **macOS テンプレートも毎バージョン焼き直す。** 入力は **upstream 公式の
  `macos.zip`**（エディタの「エクスポートテンプレートの管理」で入れたもの）で
  あり、その sha は動かないが、出力 zip は非決定的で毎回別の sha になる。
  流用できない理由は sha ではなく、アセット名のバージョンと
  `macos.metadata` の `release_version` の一致検査。
- `dist/package-editor-macos.sh` は既定で `--smoke` が付く
  （`gates/build-and-run-godot-editor-export.sh` と
  `gates/build-and-run-godot-editor-export-web.sh` を、組み立てた `.app` 自身に
  対して実走させる）。切らないこと。
- prebuilt 軸が 1 つでも欠けると die する。macOS の必須集合は
  `host` + iOS 3 軸 + `android-arm64-v8a` + `web-wasm32`。
  `--allow-partial-prebuilt` で降格を受け入れられるが、**ノートの
  `prebuilt_axes` 行が劣化する**ので、まず欠けている前提（Xcode / NDK / emsdk）を
  入れること。`host` 軸の欠落だけはこのフラグでも救われない。

Web エクスポートの smoke は、終了時に必ず `Terminated: 15 ... http.server` を
出す。**後片付けの SIGTERM であって失敗ではない。**

出力（`artifacts/release/`）:

```
Godot-dn2cpp-$V-macos-arm64.zip          + editor-macos.metadata
godot-dn2cpp-$V-web-template.zip{,.provenance} + web.metadata
godot-dn2cpp-$V-macos-arm64-template.zip + macos.metadata
SHA256SUMS.txt                           ← この時点で 3 行
```

### A-6. dry-run

```bash
./dist/release-github.sh --version "$V" --dry-run
```

`--lane` を 1 つも渡さないときの既定が、ちょうど macOS ホストの 3 レーン
（`editor-macos` `web` `macos`）。read-only で全プリコンディションを実走し、
ノートをレンダリングし、実行するはずだった git / gh コマンドを印字して終わる。

**認識が食い違ったら議論せずこれを回す。** 落ちた行がそのまま原因。

### A-7. draft を作る

```bash
./dist/release-github.sh --version "$V" 2>&1 | tee /tmp/release-macos.log
```

- **`--publish` を付けない。** draft のまま Windows に渡す。draft は
  「アセットが途中までしか載っていない状態が誰にも見えない」唯一の状態。
- **`SHA256SUMS.txt` を 3 行のまま置く。** Windows のアセット行を先回りして
  足すと、行集合とアクティブレーンの照合で die する
  （"rows for no active lane"）。4 行目は Windows のパッケージャが自分で足す。
- タグはここで作られて push される。以後の再実行では「既に origin にある」と
  報告してタグには触らない。

---

## フェーズ B: 引き渡し

Windows ホストへ渡すのは、`artifacts/release/` の **6 ファイル**と、フォーク
ルートの **`web_emcc.txt`** の計 7 点。

| 渡すもの | なぜ |
|---|---|
| `editor-macos.metadata` `web.metadata` `macos.metadata` | `--uploaded-lane` はメタデータを**ローカルの `<out>/<lane>.metadata` から読む**。公開中のノート本文が照合先になるので、手で書き直してはいけない |
| `SHA256SUMS.txt`（3 行） | Windows のパッケージャが 4 行目を追記する。全 4 行が最終レンダリングの入力 |
| `godot-dn2cpp-$V-web-template.zip` | 実体が要る。Windows エディタの smoke が**リリース版の**テンプレートでエクスポートするため |
| `godot-dn2cpp-$V-web-template.zip.provenance` | 同 smoke がエンジンの provenance を読む。無い場合は `web.metadata` から導出されるが、その場合ハッシュ一致が必須 |
| フォークルートの `web_emcc.txt` | `dist/package-editor-windows.sh` の smoke が smoke-root へコピーする必須ファイルの 1 つで、無ければ `the fork root has no web_emcc.txt` で die する。**書くのは `gates/setup-godot-fork-web.sh` だけ**で、Windows はそれを走らせない（C-1）ので macOS 側のものを置くしかない |

`web_emcc.txt` の**中身**は emcc の版文字列で、Web エクスポートゲートの
キャッシュキーに入るだけ。emcc の一致検査（C-2 / `godot_fork_web_template_emcc_assert`）
は、`release_version` の合う `web.metadata` があればそちらを witness に使うので、
このファイルは実質「存在が要る」ファイル。

作る側（macOS）:

```bash
tar czf ~/dn2cpp-windows-handoff-$V.tgz \
  -C artifacts/release \
    editor-macos.metadata web.metadata macos.metadata SHA256SUMS.txt \
    "godot-dn2cpp-$V-web-template.zip" "godot-dn2cpp-$V-web-template.zip.provenance" \
  -C "${DN2CPP_GODOT_FORK_ROOT:-$HOME/.cache/dn2cpp-godot-fork}" web_emcc.txt
```

**渡さないもの:**

- **macOS エディタの zip と macOS テンプレートの zip**（合わせて数百 MB）。
  `--uploaded-lane` は「アップロードすること」と「アセットが `--out` に
  あること」の 2 つだけを免除する。**検証は 1 つも免除しない** ——
  バイトは GitHub が配信している digest と、メタデータの各値は公開中の
  ノート本文と、それぞれ照合される。
- **これが 2 ホストの順序が固定である理由。** 逆順にすると、Windows ホストに
  macOS のアセット実体を置く必要が出る。

配置（Windows 側）:

```bash
mkdir -p <DEV>/artifacts/release
tar xzf <受け取った tgz> -C <DEV>/artifacts/release
mv <DEV>/artifacts/release/web_emcc.txt "${DN2CPP_GODOT_FORK_ROOT:-$HOME/.cache/dn2cpp-godot-fork}/web_emcc.txt"
```

Windows 側も、前回リリースの資産が `artifacts/release/` に残っていれば先に
退避する（§0-D）。

### tgz を運ぶ手段が無いとき

7 点のうち**バイトが要るのは Web テンプレートの zip だけ**で、残る 6 点は
合わせて 2 KB に満たないテキスト。だから 2 ホスト間にファイル共有が無くても
渡せる:

```bash
# Windows 側。draft のアセットは、リリースの所有者なら gh で取得できる
gh release download "$V" --repo "$REPO" \
  --pattern "godot-dn2cpp-$V-web-template.zip" --dir artifacts/release
```

残り 6 点は中身を書き写す。`SHA256SUMS.txt` は区切りが**半角スペース 2 個**で、
書き写しで壊しやすい唯一の箇所 —— 置いたあとに
`shasum -a 256 -c SHA256SUMS.txt` が通ることまで見ておくとよい。

これは metadata を「手で書く」ことにはあたらない。禁じているのは**値を考えて
書く**ことで、パッケージしたホストの内容をそのまま複製するのは経路が違うだけ。

---

## フェーズ C: Windows ホスト

シェルは Git Bash / MSYS。

### C-0. Windows 固有の前提

- **ホストに Python 3 を入れる。** `resolve_python` の候補は `python3` /
  `python` / `py -3` の 3 つだけで、同梱 Emscripten SDK が持つ portable
  CPython は候補に入らない（あれは `EMSDK_PYTHON` として `emcc.exe` にだけ
  渡される）。`dist/package-editor-windows.sh` は manifest の読み出しから
  zip の作成まで全部これを使う。逃げ道は `DN2CPP_PYTHON` の明示だけ。
- **`CMAKE_CXX_COMPILER=cl` を export する。**

  ```bash
  export CMAKE_CXX_COMPILER=cl
  ```

  `gates/_common.sh` はこの値を見たときだけ `vcvarsall x64` を取り込む
  （`is_msvc_compiler` → `ensure_msvc_env`、source 時に実行）。設定しないと
  素の Git Bash には `cl.exe` も `INCLUDE` / `LIB` も無く、prebuilt runtime の
  host 軸の configure が失敗する。そして **`dist/package-toolchain.sh` は
  それを警告で流して `prebuilt/` ごと削除し、exit 0 で終わる。**
  失敗が表面化するのは、その後 `dist/package-editor-windows.sh` が
  `the staged toolchain carries no prebuilt/host` で die するとき。
  （Developer Command Prompt から起動した Git Bash でも同じ効果が得られる。）
- **Android NDK を入れる。** 無いと `dist/package-toolchain.sh` は android 軸を
  黙って外し（情報行 1 行）、`dist/package-editor-windows.sh` が
  `missing prebuilt axes` で die する。回避は `--allow-partial-prebuilt` のみ
  だが、それはノートの `prebuilt_axes` を劣化させる。
  Windows の必須軸は `host` + `android-arm64-v8a` + `web-wasm32` の 3 つ
  （iOS 軸は Windows では焼けないので要求されない）。
- **.NET SDK は macOS 側と同じバージョン。** 違うと `corelib_framework` の
  一致検査で die する。

### C-1. セットアップ（macOS と同じ 2 本 + self-host + fork）

チェックアウトは `main` ではなく、macOS 側が使った dn2cpp のコミット（§0-C）。

```bash
cd <DEV>
export CMAKE_CXX_COMPILER=cl
./gates/setup-buildtools.sh
./gates/setup-emsdk.sh
./gates/selfhost-emit.sh
./gates/setup-godot-fork.sh 2>&1 | tee /tmp/setup-godot-fork.log
```

`engine hash:` が macOS 側で控えた値と一致すること。**違ったらここで止める。**
一致しないまま進めると `engine_provenance` の照合で必ず死ぬ。

`gates/setup-godot-fork-web.sh` は **走らせない**。Web テンプレートは 1 リリースに
1 本で、macOS 側で焼いたものを使う。

### C-2. emcc の一致を先に確認する

```bash
python -c "import json;print(json.load(open('artifacts/toolchain/dn2cpp-toolchain-0.1.0-windows-x86_64/emsdk/emsdk.json'))['emcc_version'])"
sed -n 's/^emcc=//p' artifacts/release/web.metadata
```

**2 つが一致すること。** 一致しないと C-3 が die する。Windows 側に逃げ道は
無い（Web テンプレートを焼き直せるのは macOS 側だけ）。

### C-3. Windows エディタをパッケージ

```bash
./dist/package-editor-windows.sh --version "$V" 2>&1 | tee /tmp/pkg-editor-windows.log
```

確認:

- `web template: emcc matches the bundled toolchain (.../artifacts/release/web.metadata)`
- `prebuilt:` に host / android / web の 3 軸が並ぶ
- smoke の 2 ゲートが緑
- 末尾 `OK: ...-windows-x86_64.zip`

```bash
grep '^fork_commit=' artifacts/release/editor-windows.metadata   # macOS 側と同じ SHA
wc -l < artifacts/release/SHA256SUMS.txt                          # → 4
```

### C-4. dry-run

```bash
./dist/release-github.sh --version "$V" \
  --lane editor-windows \
  --uploaded-lane editor-macos --uploaded-lane web --uploaded-lane macos \
  --dry-run
```

- `--uploaded-lane` は「そのレーンを active にする」+「アセットは既にリリース上に
  ある」の 2 つを意味する。**4 レーンすべてを名指すこと。**
- 各 uploaded レーンについて
  `on the release as ..., digest and published notes agree` が出ること。

### C-5. 本番 + 公開

```bash
./dist/release-github.sh --version "$V" \
  --lane editor-windows \
  --uploaded-lane editor-macos --uploaded-lane web --uploaded-lane macos \
  --publish 2>&1 | tee /tmp/release-windows.log
```

draft のうちは `SHA256SUMS.txt` が先、アセットは小さい順。公開済みリリースへ
追記する場合はアセットが先で `SHA256SUMS.txt` が最後（リリースが、まだ載って
いないアセットを名指す窓を開けないため）。どちらもスクリプトが選ぶ。

---

## フェーズ D: 公開後の確認

```bash
gh release view "$V" --repo "$REPO" --json isDraft,targetCommitish,assets \
  --jq '{isDraft, targetCommitish, assets:[.assets[]|{name,digest}]}'
```

- `isDraft: false`
- アセットは 5 点（エディタ 2 + テンプレート 2 + `SHA256SUMS.txt`）
- `targetCommitish` がタグを張ったフォークのコミット

```bash
gh release view "$V" --repo "$REPO" --json body --jq .body | grep -n '@@'   # → 何も出ないこと
gh release view "$V" --repo "$REPO" --json body --jq .body | grep -n '<!--' # → 何も出ないこと
```

（スクリプトもレンダリング時に同じ 2 つを検査して die するが、公開本文を
目で確認する価値はある。）

最後に、実際にダウンロードして展開できることを 1 度だけ確認する:
`SHA256SUMS.txt` の照合 → macOS なら `xattr -dr com.apple.quarantine`、
Windows なら zip のブロック解除。

---

## リリースノート

- **原本は `dist/release-notes-template.md`（日本語）。** リリースページの本文を
  直接編集してはいけない。`dist/release-github.sh` の再実行だけが本文を変える。
- **GitHub は段落内の単一改行を改行として描画する。**したがってテンプレートは
  **1 段落 = 1 行**で書く。折り返すと公開本文が改行だらけになる。
- `@@KEY@@` は、レーン表が bind している集合の**部分集合**でなければならない。
  未 bind の `@@KEY@@` が 1 つでも残るとレンダリングが die する。
- `<!--lane:NAME-->` は、行頭に置いて後ろに本文があればその 1 行、単独なら
  `<!--/lane-->` までのブロックを、そのレーンが無いカットから落とす。
  `!NAME` は否定。**入れ子にはできない**（内側の `-->` が外側を早く閉じ、
  残りが本文としてリリースページに出る）。存在しないレーン名は die。
- `dist/` の md に Emscripten / Node.js / cmake / ninja のバージョンを直書き
  しない。ピンが唯一の出所で、ノートは `@@EMSDK_VERSION@@` などのプレース
  ホルダで受け取る。直書きは `gates/build-and-run-doc-claims.sh` が落とす。

---

## トラブルシューティング

**まず `--dry-run`。** read-only で全プリコンディションを実走する。認識が
食い違ったときに議論する必要はない。

| 症状 | 原因と対処 |
|---|---|
| `--version '...' is not of the form <godot version>-dn2cpp.<n>` | 形式違反。`<base>-dn2cpp.<n>` 以外は受け付けない |
| `--version base X != the fork's version.py (Y)` | フォークのチェックアウトが別のベース |
| `lanes 'A' and 'B' disagree on engine_provenance` | 2 ホストのエンジンツリーが違う。片方を焼き直す |
| `... disagree on corelib_framework` | 2 ホストの .NET SDK が違う |
| `the packaged X editor was built from A, but the tag would name B` | パッケージ時と現在のフォーク HEAD が違う。焼き直すか `--commit A` |
| `is not reachable from origin/...` | push していない。フォークまたは dn2cpp を push する |
| `SHA256SUMS.txt does not describe exactly the active lanes' assets` | `rows for no active lane` なら前バージョンの資産が `--out` に残っている（§0-D）。それ以外はレーンの名指し忘れか、先回りして足した行 |
| `the notes on release ... do not mention KEY=...` | 手元の metadata が公開中のノートと食い違う。**手で直さず**、パッケージしたホストからコピーし直す |
| `no working Python 3 interpreter found` | Windows。ホストに Python 3 を入れる |
| `the staged toolchain carries no prebuilt/host` | Windows なら `CMAKE_CXX_COMPILER=cl` の未設定をまず疑う。`dist/package-toolchain.sh` のログ（`prebuilt-host-configure.log`）に本当の原因がある |
| `missing prebuilt axes: android-arm64-v8a ...` | NDK / emsdk / Xcode が無い。入れるのが本筋 |
| `the Web template and the bundled toolchain were linked by different emcc` | `FORCE=1 gates/setup-godot-fork-web.sh` → `dist/package-web-template.sh --version "$V"` の順で焼き直す（macOS 側のみ） |
| `predates the current sources` | `gates/selfhost-emit.sh` を焼き直す |
| `the fork root has no web_emcc.txt` | Windows。macOS から引き渡した `web_emcc.txt` をフォークルート直下に置き忘れている（§B） |

**紛らわしいが失敗ではないログ:**

- Web エクスポート smoke 末尾の `Terminated: 15 ... http.server` —— 後片付けの
  SIGTERM。
- `gates/setup-godot-fork.sh` のログに混ざる非英語の MSBuild 出力 —— ホストの
  ロケール。

**検証用のヘルパを `gates/` の外に置かないこと。** `gates/_common.sh` は
`BASH_SOURCE[1]`（source した側）からリポジトリルートを導くので、リポジトリ外の
スクリプトから source するとルートを誤認して die する。

**アセットを触らずノート本文だけ直したいとき:** 全 4 レーンを
`--uploaded-lane` で宣言して再実行する。アップロード対象が空になり、
`SHA256SUMS.txt` の再アップロードとノートの再レンダリングだけが走る。
metadata 4 本と 4 行の `SHA256SUMS.txt` が手元に揃っていることが条件。

---

## やってはいけないこと

1. **前回リリースの資産を残したまま新バージョンを焼かない。** ディレクトリごと
   退避する（§0-D）。
2. **全レーンが載ったあとに、レーンの部分集合で `dist/release-github.sh` を
   再実行しない。** `--lane` / `--uploaded-lane` に無いレーンはノートから
   ブロックごと落ち、`SHA256SUMS.txt` の行も消える。一方 GitHub 上のアセットは
   残る ——「Windows エディタが添付されているのに、ノートは無いと言い、
   `SHA256SUMS.txt` にも行が無い」リリースになる。**スクリプトはこれを検出
   しない。**やり直すときは常に全レーンを名指す。
3. **macOS 側で `--publish` しない。** draft のまま渡す。
4. **macOS 側で `SHA256SUMS.txt` を 4 行にしない。**
5. **`--uploaded-lane` 用の metadata を手で書かない。** パッケージしたホストの
   ものをそのままコピーする。必須キーの集合はバージョンをまたいで増えるので
   （`node_version` はある版で追加された）、**古い metadata を手直しして新しい
   バージョンを名乗らせることはできない**。
6. **セットアップ 2 本を `--out` 付きで走らせない。**
7. **リリースページの本文を web UI で編集しない。**次の再実行で消える。
8. **`--allow-partial-prebuilt` を常用しない。** 前提が入っていないことの
   宣言であって、回避策ではない。

---

## 署名について（ダウンロード側への告知事項）

- **macOS エディタは ad-hoc 署名のみで、公証（notarize）されていない。**
  ブラウザ経由でダウンロードすると quarantine 属性が付き、起動を拒否される。
  ノートには `xattr -dr com.apple.quarantine` の手順が入っている。
  （`spctl --master-disable` を案内しないこと。）
- **Windows エディタは未署名。** SmartScreen が発行元不明と報告する。
  身元を保証するのは `SHA256SUMS.txt` とパッケージ内の `RELEASE.txt` だけ。

どちらも現状の仕様で、`docs/STATUS.md` に行がある。

---

## 未検証（この手順で裏が取れていない項目）

- **冷たいツリーの所要時間。** 上の表は温かいツリーの 1 ホスト 1 回の実測で、
  冷たい側は測っていない。
- **Windows で `CMAKE_CXX_COMPILER=cl` を設定しなくても、既に vcvars 済みの
  シェルなら通るか** —— コード上は `ensure_msvc_env` が何もせず、cmake の
  既定検出に委ねられる。通る可能性はあるがコードからは断定できない。
  設定しておくのが安全。
- **`web_emcc.txt` の内容がホスト間で必ず一致するか** —— スタンプは `emcc` の
  版文字列で、同じピンの SDK なら一致するはずだが、ホスト差が出ないことを
  保証する検査は無い。emcc の一致は `web.metadata` を witness に取る側で
  担保されるので実害は Web エクスポートゲートのキャッシュキーだけだが、
  実測はしていない。
