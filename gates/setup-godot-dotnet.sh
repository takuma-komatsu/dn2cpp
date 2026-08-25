#!/usr/bin/env bash
# SETUP AID (NOT a regression gate — the name is outside the `build-and-run-*.sh`
# glob, so run-all-gates.sh ignores it; the measure-* sibling of
# gates/measure-gcpause.sh). Prepares everything gates/dotnet-measure.sh (and the
# later engine E2E gates) need from a pinned godotengine/godot clone:
#
#   1. mono-enabled editor build        (scons target=editor module_mono_enabled=yes)
#   2. mono glue generation             (<editor> --headless --generate-mono-glue)
#   3. managed assemblies + local nuget feed
#      (modules/mono/build_scripts/build_assemblies.py --push-nupkgs-local)
#   4. mono-enabled release template    (scons target=template_release ...)
#   5. a self-contained artifact root:  $ROOT/{editor_bin,template_bin,GodotSharp/,
#                                       nuget/,pin.txt}
#
# Idempotent, and "already there" means "produced from the pinned tree" rather
# than merely present. A skip on the product existing is what let the
# 4.7.1-stable re-pin run green over every artifact of the old base while
# pin.txt was rewritten to the new one — the run reported success and the cache
# then lied about which commit it described, which is the one thing the ABI
# tripwire cannot catch (it fingerprints the clone's sources, not the products).
# The two kinds of evidence this script uses, and why they differ:
#
#   * the ENGINE BINARIES testify for themselves. A Godot binary reports its own
#     build commit — "4.7.2.stable.mono.custom_build.ed1daf0bf" for a scons build
#     of the clone, ".mono.official.<hash>" for a release download — so steps 1
#     and 4 ask the artifact rather than trusting a file somebody wrote beside
#     it. That is strictly stronger than a stamp (nothing outside the build can
#     forge it), it costs 0.15 s, and it is what makes this change free on an
#     existing cache: the binaries already there answer correctly, so nothing
#     re-scons.
#   * the GLUE and the ASSEMBLIES have no such testimony — the generated glue
#     records no version anywhere, and an assembly version is not a commit — so
#     steps 2 and 3 carry a stamp naming the clone tree they came from
#     (clone_tree_hash). A cache assembled before this existed has no stamp, and
#     an unstamped product is of unknown provenance rather than current, so the
#     first run after this change regenerates both. That is minutes, not the tens
#     the scons steps would have been.
#
# The scons steps take tens of minutes each on a cold clone — run this in the
# background, not inside a gate.
#
# TWO SOURCES for the same artifacts (steps 1-4), and they are interchangeable
# *because of the pin, not in spite of it*: the 4.7.2-stable tag IS the pinned
# commit, so an official godotengine/godot release binary is built from exactly
# the tree this script would otherwise compile. Which one runs:
#
#   - DN2CPP_GODOT_PREBUILT=<editor binary> — take the editor, the glue, the
#     GodotSharp assemblies and the nupkgs from an official install. Honoured on
#     EVERY OS. The engine's own rule (modules/mono/godotsharp_dirs.cpp) is what
#     makes naming the *binary* — not a root directory — the right handle:
#     GodotSharp/ hangs off its dirname, and off Contents/Resources instead when
#     that dirname is a .app's Contents/MacOS.
#   - unset, on Windows: auto-detect from $GODOT (else `godot` on PATH). scons
#     is not an option here — platform/windows/detect.py forces d3d12=yes and
#     exit 255s without the Mesa build deps — so a failed detect is a hard error
#     naming the remedy, never a silent fallback.
#   - unset, on macOS: build from the clone with scons, as it always did.
#   - unset, on Linux: no scons fallback either — the scons invocations below are
#     hardcoded platform=macos arch=arm64 (and --godot-platform=macos for
#     build_assemblies.py), so that branch only ever builds a macOS binary. A
#     PATH godot is taken when it IS the pinned mono build; otherwise the pinned
#     release's editor archive and export-templates tpz are downloaded into
#     $DN2CPP_GODOT_DOWNLOAD_CACHE. Unlike Windows, a non-matching PATH godot is
#     replaced rather than refused: the plain Linux build reports the pin and
#     ships no GodotSharp, so it is the common case, not a mistake.
#
# Whichever source, the pin is *verified*, not assumed: a prebuilt editor must
# report `<ver>.mono.official.<pin[0:9]>`. Running the ABI handshake against an
# engine that is not the pin would defeat this lane's whole purpose.
#
# Usage:
#   ./gates/setup-godot-dotnet.sh [ROOT]
#
#   ROOT                       artifact/cache root (default: $DN2CPP_GODOT_DOTNET_ROOT,
#                              then $HOME/.cache/dn2cpp-godot-dotnet)
#   DN2CPP_GODOT_CLONE         godot clone path (default: sibling directory of this
#                              dn2cpp checkout, i.e. ../godot)
#   DN2CPP_GODOT_PREBUILT      official editor binary to take steps 1-3 from
#   DN2CPP_GODOT_PREBUILT_TEMPLATE
#                              release export template for step 4 (default: the
#                              editor's own export_templates dir)
#   DN2CPP_GODOT_DOWNLOAD_CACHE
#                              where the Linux arm keeps the release archives it
#                              fetches (default: $HOME/.cache/dn2cpp-godot-downloads)
#
# The clone must be at the pinned commit below whichever source supplies the
# binaries: it is what the gates' interop-ABI tripwire fingerprints
# (godot_dotnet_pin_abi_check), so the GodotSharp IL, the glue, and the engine
# binaries can never disagree about the interop ABI.
set -euo pipefail

DN2CPP_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PINNED_COMMIT=ed1daf0bf001b61586d9930840f2f1394092c079
# The godot-builds release tag naming the SAME tree as PINNED_COMMIT — the two
# move together or the download arm fetches an engine the pin check then refuses.
# It cannot be derived: it is what names the asset to fetch before any binary exists.
PINNED_TAG=4.7.2-stable
ROOT="${1:-${DN2CPP_GODOT_DOTNET_ROOT:-$HOME/.cache/dn2cpp-godot-dotnet}}"
CLONE="${DN2CPP_GODOT_CLONE:-$(dirname "$DN2CPP_ROOT")/godot}"
DOWNLOADS="${DN2CPP_GODOT_DOWNLOAD_CACHE:-$HOME/.cache/dn2cpp-godot-downloads}"
JOBS="$(sysctl -n hw.ncpu 2>/dev/null || nproc 2>/dev/null || echo 8)"

# Mirrors gates/_common.sh's detect_os + EXE_EXT. Inlined rather than sourced:
# every setup-*.sh is standalone, and _common.sh is a *gate* helper — sourcing it
# would drag in the MSVC env import, the console code page and the gate cache.
detect_os() {
    case "$(uname -s)" in
        Darwin)               printf 'macos\n' ;;
        Linux)                printf 'linux\n' ;;
        CYGWIN*|MINGW*|MSYS*) printf 'windows\n' ;;
        *)                    printf 'unknown\n' ;;
    esac
}
DN2CPP_OS=${DN2CPP_OS:-$(detect_os)}
case "$DN2CPP_OS" in
    windows) EXE=.exe ;;
    *)       EXE= ;;
esac

EDITOR_BIN_REL=bin/godot.macos.editor.arm64.mono
EDITOR_BIN_DEV_REL=bin/godot.macos.editor.dev.arm64.mono
TEMPLATE_BIN_REL=bin/godot.macos.template_release.arm64.mono
TEMPLATE_BIN_DEV_REL=bin/godot.macos.template_release.dev.arm64.mono
GLUE_MARKER=modules/mono/glue/GodotSharp/GodotSharp/Generated/GeneratedIncludes.props
API_RELEASE_REL=bin/GodotSharp/Api/Release/GodotSharp.dll

echo "== 0/6 Verifying the pinned godot clone =="
if [ ! -d "$CLONE/.git" ] && [ ! -f "$CLONE/.git" ]; then
    echo "error: godot clone not found at $CLONE" >&2
    echo "       clone godotengine/godot there (or set DN2CPP_GODOT_CLONE) and" >&2
    echo "       check out commit $PINNED_COMMIT" >&2
    exit 1
fi
head_commit="$(git -C "$CLONE" rev-parse HEAD)"
if [ "$head_commit" != "$PINNED_COMMIT" ]; then
    echo "error: godot clone at $CLONE has DRIFTED from the pinned commit" >&2
    echo "       expected: $PINNED_COMMIT" >&2
    echo "       actual:   $head_commit" >&2
    echo "       The GodotSharp IL, the mono glue, and the engine binaries must all" >&2
    echo "       come from one commit; check out the pin (or update the pin in this" >&2
    echo "       script deliberately, then rebuild everything from scratch)." >&2
    exit 1
fi
echo "clone: $CLONE @ $head_commit (pin OK)"
mkdir -p "$ROOT"

# ---------------------------------------------------------------------------
# Prebuilt (official-install) source — see the header.
# ---------------------------------------------------------------------------

# binary_reported_version BIN — what a Godot binary says it was built from.
# Godot's --version prints e.g. "4.7.2.stable.mono.custom_build.ed1daf0bf": the
# trailing field is the commit short hash and the field before it is the build
# source. The hash is the whole point — it is generated by the build, so it is
# the one piece of provenance nothing outside the build can forge, and it is why
# neither of this file's two binary steps needs a stamp.
binary_reported_version() {
    "$1" --version 2>/dev/null | tail -1 | tr -d '\r'
}

# prebuilt_pin_ok EDITOR — true when EDITOR reports itself as the mono flavour of
# an *official* build of the pinned commit. The stricter sibling of the test
# pick_binary applies: here ".official." is load-bearing on its own, because this
# arm goes on to take the GodotSharp assemblies and the packed nupkgs from beside
# the binary, and only an official install ships those. A non-mono official build
# would say "4.7.2.stable.official.<hash>" and ship no GodotSharp at all, so the
# ".mono." is load-bearing too.
prebuilt_pin_matches() {
    case "$(binary_reported_version "$1")" in
        *.mono.official."${PINNED_COMMIT:0:9}") return 0 ;;
    esac
    return 1
}

prebuilt_pin_ok() {
    prebuilt_pin_matches "$1" && return 0
    echo "error: prebuilt editor is not the pinned mono build: $1" >&2
    echo "       reported: $(binary_reported_version "$1")" >&2
    echo "       expected: *.mono.official.${PINNED_COMMIT:0:9}  (pin $PINNED_COMMIT)" >&2
    return 1
}

# godot_arch_tag — this machine's architecture as a godot-builds asset spells it.
godot_arch_tag() {
    case "$(uname -m)" in
        x86_64|amd64)  printf 'x86_64\n' ;;
        aarch64|arm64) printf 'arm64\n' ;;
        *)             uname -m ;;
    esac
}

# fetch_release_asset NAME — echo the path of $DOWNLOADS/NAME, downloading it
# from the pinned godot-builds release first if it is not already there.
# Verified non-empty BEFORE the atomic rename: `curl -f` catches an HTTP error,
# but a zero-length 200 answered by a proxy is a successful transfer, and
# renaming it makes every later run skip the download and hand the empty file to
# unzip. Narration goes to stderr — callers capture this function's stdout.
fetch_release_asset() {
    local name="$1" dest="$DOWNLOADS/$1"
    if [ -s "$dest" ]; then
        printf '%s\n' "$dest"
        return 0
    fi
    mkdir -p "$DOWNLOADS"
    echo "-- downloading $name" >&2
    curl -fL --progress-bar -o "$dest.part" \
        "https://github.com/godotengine/godot-builds/releases/download/$PINNED_TAG/$name" >&2
    [ -s "$dest.part" ] || {
        echo "error: downloaded $name is empty (bad mirror?)" >&2
        rm -f "$dest.part"
        return 1
    }
    mv "$dest.part" "$dest"
    printf '%s\n' "$dest"
}

# download_prebuilt_editor — echo an official mono editor binary for this host,
# fetching and unpacking the pinned release's editor archive when needed. The
# archive carries GodotSharp/ beside the binary, which is the whole reason the
# prebuilt arm can take steps 1-3 from it.
download_prebuilt_editor() {
    local arch name zip dir editor
    arch="$(godot_arch_tag)"
    name="Godot_v${PINNED_TAG}_mono_${DN2CPP_OS}_${arch}.zip"
    dir="$DOWNLOADS/${name%.zip}"
    editor="$dir/Godot_v${PINNED_TAG}_mono_${DN2CPP_OS}.$arch"
    if [ ! -x "$editor" ]; then
        zip="$(fetch_release_asset "$name")" || return 1
        echo "-- unpacking $name" >&2
        rm -rf "$dir"
        # Into $DOWNLOADS, not $dir: the archive's single top-level directory is
        # named after itself, so unpacking one level up reconstructs $dir.
        unzip -q "$zip" -d "$DOWNLOADS" >&2
        chmod +x "$editor" 2>/dev/null || true
    fi
    [ -x "$editor" ] || {
        echo "error: $name unpacked no editor at $editor" >&2
        return 1
    }
    printf '%s\n' "$editor"
}

# clone_tree_hash — a fingerprint of the sources this clone would compile: the
# pin, the delta from it (--binary, so a modified binary file is fingerprinted by
# its content rather than by "Binary files differ"), and a hash of every
# non-ignored untracked file. It keys the two steps whose products cannot testify
# for themselves (the glue, the assemblies + feed).
#
# Step 0 has already refused a clone whose HEAD is not the pin, so on any run
# that reaches here the `base` term is constant and the rest of the hash
# discriminates working-tree DIRT — which is exactly the residue the pin check
# cannot see. Across a re-pin the `base` term is what moves, and it moves before
# any product does, which is the case this check exists for.
#
# It fingerprints content, never git state: staging an edit, committing it, or
# reaching the same sources from another branch all leave it alone, because none
# of them changes a byte the build reads. Everything these steps WRITE is
# gitignored — bin/, obj/, SdkPackageVersions.props, the generated glue — so no
# product, and no stamp, can feed the hash that keys it.
clone_tree_hash() {
    (
        cd "$CLONE"
        printf 'base %s\n' "$PINNED_COMMIT"
        git diff --no-ext-diff --no-textconv --no-color --no-renames --binary \
            "$PINNED_COMMIT"
        git ls-files --others --exclude-standard -z | xargs -0 shasum -a 256
    ) | shasum -a 256 | cut -c1-16
}

# resolve_prebuilt — echo the official editor binary to source steps 1-3 from, or
# nothing when this run should build from the clone instead.
resolve_prebuilt() {
    local editor="${DN2CPP_GODOT_PREBUILT:-}"
    if [ -z "$editor" ]; then
        # Windows and Linux only: the scons arm below is hardcoded platform=macos,
        # so those two hosts have nothing to fall back to.
        case "$DN2CPP_OS" in windows|linux) ;; *) return 0 ;; esac
        editor="$(command -v "${GODOT:-godot}" 2>/dev/null || true)"
        editor="$(readlink -f "$editor" 2>/dev/null || printf '%s\n' "$editor")"
        if [ "$DN2CPP_OS" = linux ]; then
            # A PATH godot is very often the plain build, which reports the pin
            # and ships no GodotSharp at all — so a candidate that does not match
            # is REPLACED here rather than refused, and only the download's own
            # failure is fatal.
            if [ -z "$editor" ] || [ ! -x "$editor" ] || ! prebuilt_pin_matches "$editor"; then
                editor="$(download_prebuilt_editor)" || return 1
            fi
        elif [ -z "$editor" ]; then
            echo "error: no Godot on PATH, and Windows has no scons fallback" >&2
            echo "       Install the official Godot ${PINNED_COMMIT:0:9} mono build (the $PINNED_TAG" >&2
            echo "       release IS the pin), put it on PATH or point GODOT at it, or name it" >&2
            echo "       directly with DN2CPP_GODOT_PREBUILT=<editor binary>." >&2
            return 1
        fi
    fi
    # GodotSharp/ sits beside the REAL binary, so resolve the link before anything
    # reads the dirname — a named editor is very often a symlink into an install
    # elsewhere, whether it came off PATH or out of DN2CPP_GODOT_PREBUILT. Left
    # unresolved, a correct mono install is refused as "not a mono install".
    editor="$(readlink -f "$editor" 2>/dev/null || printf '%s\n' "$editor")"
    [ -x "$editor" ] || { echo "error: prebuilt editor not executable: $editor" >&2; return 1; }
    prebuilt_pin_ok "$editor" || return 1
    printf '%s\n' "$editor"
}

# prebuilt_template EDITOR — echo the release export template to use as
# template_bin. Same directory the editor itself reads export templates from,
# and the same per-OS data dir gates/build-and-run-android-gdext.sh resolves
# (macOS: ~/Library/Application Support/Godot, Windows: %APPDATA%\Godot, Linux:
# ~/.local/share/godot).
prebuilt_template() {
    local editor="$1"
    if [ -n "${DN2CPP_GODOT_PREBUILT_TEMPLATE:-}" ]; then
        printf '%s\n' "$DN2CPP_GODOT_PREBUILT_TEMPLATE"
        return 0
    fi
    local data_dir tpl_name
    case "$DN2CPP_OS" in
        macos)   data_dir="$HOME/Library/Application Support/Godot" ;;
        windows) data_dir="$(cygpath -u "${APPDATA:-$HOME/AppData/Roaming}")/Godot" ;;
        *)       data_dir="$HOME/.local/share/godot" ;;
    esac
    case "$DN2CPP_OS" in
        windows) tpl_name=windows_release_x86_64.exe ;;
        linux)   tpl_name="linux_release.$(godot_arch_tag)" ;;
        *)
            # macOS ships macos.zip — an .app bundle, not a bare binary that
            # template_bin can point at. Say so rather than guess a path.
            echo "error: no automatic release-template rule for $DN2CPP_OS" >&2
            echo "       (macOS's official template is macos.zip, an .app bundle, not a binary)" >&2
            echo "       Set DN2CPP_GODOT_PREBUILT_TEMPLATE=<release template binary>." >&2
            return 1
            ;;
    esac
    # The template directory is named after the FULL version string minus the
    # build source — "4.7.2.stable.mono" for a mono build. Strip the trailing
    # ".official.<hash>" with a suffix removal rather than reassembling a fixed
    # number of dot-fields: a fixed `$1.$2.$3.$4` is right only for a 3-segment
    # version (4.7 -> 4.7.stable.mono) and drops ".mono" for a 4-segment one
    # (4.7.1 -> 4.7.1.stable, no such directory) — which is exactly what the
    # 4.7.1-stable re-pin turned red. prebuilt_pin_ok has already asserted the
    # ".mono.official.<hash>" shape, so the suffix is always present to strip.
    local ver
    ver="$("$editor" --version 2>/dev/null | tail -1 | tr -d '\r')"
    ver="${ver%.official.*}"
    local tpl="$data_dir/export_templates/$ver/$tpl_name"
    if [ ! -f "$tpl" ] && [ "$DN2CPP_OS" = linux ]; then
        # Same arm as the editor download: the host that cannot scons is the host
        # that has to be handed the official artifact. Only the ONE template is
        # extracted — the tpz carries every platform's, and the rest is ~1.4 GB of
        # files nothing here opens.
        local tpz
        tpz="$(fetch_release_asset "Godot_v${PINNED_TAG}_mono_export_templates.tpz")" || return 1
        echo "-- extracting $tpl_name into $data_dir/export_templates/$ver" >&2
        mkdir -p "$data_dir/export_templates/$ver"
        unzip -q -j -o "$tpz" "templates/$tpl_name" -d "$data_dir/export_templates/$ver" >&2
        chmod +x "$tpl" 2>/dev/null || true
    fi
    if [ ! -f "$tpl" ]; then
        echo "error: release export template not found: $tpl" >&2
        echo "       Install the export templates for $ver — the editor's" >&2
        echo "       Editor > Manage Export Templates, or extract $tpl_name from the" >&2
        echo "       official Godot_v${PINNED_TAG}_mono_export_templates.tpz into that directory." >&2
        return 1
    fi
    printf '%s\n' "$tpl"
}

# pick_binary PREFERRED_REL DEV_REL — echo the clone-relative path of an existing
# binary THAT REPORTS THE PIN, preferring the dev_build=no name but accepting an
# already-built dev binary (functionally equivalent for glue-gen / editor-driven
# steps); empty if neither exists or neither is the pin, which sends the caller
# to scons.
#
# The pin test is what makes this a freshness check rather than a presence check.
# It asks the ARTIFACT, not a stamp beside it: a scons build of the
# clone reports "…mono.custom_build.<hash>" and an official one
# "…mono.official.<hash>", so the build source is deliberately wildcarded and
# only the ".mono." flavour and the commit are asserted. A binary left over from
# before a re-pin reports the old hash and is rejected here — where the whole
# failure was that it was accepted, and every product downstream then described
# a commit pin.txt no longer named.
pick_binary() {
    local rel ver
    for rel in "$1" "$2"; do
        [ -x "$CLONE/$rel" ] || continue
        ver="$(binary_reported_version "$CLONE/$rel")"
        case "$ver" in
            *.mono.*."${PINNED_COMMIT:0:9}")
                if [ "$rel" = "$2" ]; then
                    echo "note: using existing dev build $rel (dev_build=no binary absent)" >&2
                fi
                printf '%s\n' "$rel"
                return 0
                ;;
        esac
        echo "note: $rel predates the pin and will be rebuilt — it reports" >&2
        echo "      ${ver:-<no --version output>}, wanted *.mono.*.${PINNED_COMMIT:0:9}" >&2
    done
}

PREBUILT="$(resolve_prebuilt)"

if [ -n "$PREBUILT" ]; then
    # An official install already IS the product of steps 1-3: it was built from
    # the pinned tree (verified above), its GodotSharp was generated from that
    # tree's glue, and it ships the very nupkgs build_assemblies.py packs.
    prebuilt_dir="$(cd "$(dirname "$PREBUILT")" && pwd)"
    # macOS ships the official mono editor as a .app, and there GodotSharp/ sits
    # in Contents/Resources rather than beside the executable — the engine's own
    # MACOS_ENABLED branch in godotsharp_dirs.cpp. Without this the prebuilt arm
    # cannot be taken on macOS at all.
    prebuilt_sharp="$prebuilt_dir/GodotSharp"
    case "$prebuilt_dir" in
        */Contents/MacOS)
            [ -d "$prebuilt_sharp" ] \
                || prebuilt_sharp="${prebuilt_dir%/MacOS}/Resources/GodotSharp"
            ;;
    esac

    echo "== 1/6 Mono-enabled editor build =="
    echo "skip: official pinned build supplies it: $PREBUILT"
    editor_abs="$PREBUILT"

    echo "== 2/6 Generating mono glue =="
    echo "skip: official build ships GodotSharp generated from the pinned glue"

    echo "== 3/6 Building managed assemblies + local nuget feed =="
    [ -f "$prebuilt_sharp/Api/Release/GodotSharp.dll" ] || {
        echo "error: no Api/Release/GodotSharp.dll under $prebuilt_sharp" >&2
        echo "       That directory is where the engine itself looks for it, so this is" >&2
        echo "       not a mono install — use the *_mono_* download, not the plain one." >&2
        exit 1
    }
    compgen -G "$prebuilt_sharp/Tools/nupkgs/Godot.NET.Sdk.*.nupkg" >/dev/null || {
        echo "error: no Godot.NET.Sdk nupkg under $prebuilt_sharp/Tools/nupkgs" >&2
        exit 1
    }
    rm -rf "$ROOT/nuget"
    mkdir -p "$ROOT/nuget"
    cp -f "$prebuilt_sharp"/Tools/nupkgs/*.nupkg "$ROOT/nuget/"
    echo "feed: $(ls "$ROOT"/nuget/*.nupkg | wc -l | tr -d ' ') nupkgs from $prebuilt_sharp/Tools/nupkgs"
    godotsharp_src="$prebuilt_sharp"

    echo "== 4/6 Mono-enabled release template build =="
    template_abs="$(prebuilt_template "$PREBUILT")"
    echo "template: $template_abs"
else
    CLONE_HASH="$(clone_tree_hash)"
    echo "clone tree hash: $CLONE_HASH"

    echo "== 1/6 Mono-enabled editor build =="
    editor_rel="$(pick_binary "$EDITOR_BIN_REL" "$EDITOR_BIN_DEV_REL")"
    if [ -n "$editor_rel" ]; then
        echo "skip: editor already built from the pin: $CLONE/$editor_rel"
        echo "      ($(binary_reported_version "$CLONE/$editor_rel"))"
    else
        # Deleted first, for provide_binary's reason in gates/setup-godot-fork.sh:
        # scons decides a target is current from its own signature database, which
        # knows nothing about a pin, so a binary of the old base left at the target
        # path could outlive a build scons treats as a no-op. A *missing* target is
        # always relinked.
        rm -f "$CLONE/$EDITOR_BIN_REL"
        (cd "$CLONE" && scons platform=macos arch=arm64 target=editor \
            module_mono_enabled=yes dev_build=no "-j$JOBS")
        editor_rel="$EDITOR_BIN_REL"
        [ -x "$CLONE/$editor_rel" ] || { echo "error: editor build produced no $editor_rel" >&2; exit 1; }
    fi

    echo "== 2/6 Generating mono glue =="
    # Keyed on the clone tree, not on the marker existing. The glue is the
    # EDITOR's output — `--generate-mono-glue` walks the ClassDB the binary was
    # compiled with — so a skip on presence handed step 3 a glue generated by a
    # previous pin's editor. The stamp goes beside the marker, inside the
    # generated directory, which Godot's own
    # modules/mono/glue/GodotSharp/.gitignore covers, so it can never feed the
    # hash that keys it.
    GLUE_STAMP="$CLONE/$GLUE_MARKER.clone-hash"
    if [ -f "$CLONE/$GLUE_MARKER" ] && [ -f "$GLUE_STAMP" ] \
        && [ "$(cat "$GLUE_STAMP")" = "$CLONE_HASH" ]; then
        echo "skip: glue already generated from these sources: $CLONE/$GLUE_MARKER"
    else
        if [ -f "$CLONE/$GLUE_MARKER" ]; then
            echo "-- the glue in the clone does not describe these sources"
            echo "   stamped $(cat "$GLUE_STAMP" 2>/dev/null || echo '<no stamp>') != $CLONE_HASH"
        fi
        # Removed before the run and written after it: an interrupted generation
        # must leave a glue nothing calls current.
        rm -f "$GLUE_STAMP"
        (cd "$CLONE" && "./$editor_rel" --headless --generate-mono-glue modules/mono/glue)
        [ -f "$CLONE/$GLUE_MARKER" ] || { echo "error: glue generation produced no $GLUE_MARKER" >&2; exit 1; }
        printf '%s\n' "$CLONE_HASH" > "$GLUE_STAMP"
    fi

    echo "== 3/6 Building managed assemblies + local nuget feed =="
    # Product existence AND the stamp: the products are split across two roots
    # (the assemblies land in the clone's gitignored bin/, the nupkgs in $ROOT,
    # which $1 can move), so neither test subsumes the other — a stamp alone
    # would call a feed current that a differently-rooted run never populated.
    ASSEMBLIES_STAMP="$CLONE/bin/GodotSharp/.clone-hash"
    if compgen -G "$ROOT/nuget/Godot.NET.Sdk.*.nupkg" >/dev/null && [ -f "$CLONE/$API_RELEASE_REL" ] \
        && [ -f "$ASSEMBLIES_STAMP" ] && [ "$(cat "$ASSEMBLIES_STAMP")" = "$CLONE_HASH" ]; then
        echo "skip: assemblies + feed already built from these sources: $ROOT/nuget"
    else
        if [ -f "$CLONE/$API_RELEASE_REL" ] && [ -f "$ASSEMBLIES_STAMP" ] \
            && [ "$(cat "$ASSEMBLIES_STAMP")" != "$CLONE_HASH" ]; then
            echo "-- the assemblies in the clone do not describe these sources"
            echo "   stamped $(cat "$ASSEMBLIES_STAMP") != $CLONE_HASH"
        fi
        rm -f "$ASSEMBLIES_STAMP"
        mkdir -p "$ROOT/nuget"
        # A bare `python3` is portable only because of where this sits: the
        # source-build arm, pinned to --godot-platform=macos on the next line but
        # one, so the hosts that reach it are hosts where that name IS a real
        # interpreter. Windows takes the prebuilt arm above and never arrives
        # here. Whoever ports this arm must route through _common.sh's
        # resolve_python, which probes a candidate by RUNNING it: a stock Windows
        # install answers `python3` with an app-execution-alias stub that resolves
        # and launches, so `command -v` and a plain call both look like success.
        (cd "$CLONE" && python3 modules/mono/build_scripts/build_assemblies.py \
            --godot-output-dir=bin \
            --godot-platform=macos \
            --push-nupkgs-local "$ROOT/nuget")
        [ -f "$CLONE/$API_RELEASE_REL" ] || { echo "error: build_assemblies produced no $API_RELEASE_REL" >&2; exit 1; }
        # Written LAST, after the product is asserted: an interrupted build must
        # not leave a stamp standing behind a half-written GodotSharp tree.
        printf '%s\n' "$CLONE_HASH" > "$ASSEMBLIES_STAMP"
    fi

    echo "== 4/6 Mono-enabled release template build =="
    template_rel="$(pick_binary "$TEMPLATE_BIN_REL" "$TEMPLATE_BIN_DEV_REL")"
    if [ -n "$template_rel" ]; then
        echo "skip: template already built from the pin: $CLONE/$template_rel"
        echo "      ($(binary_reported_version "$CLONE/$template_rel"))"
    else
        rm -f "$CLONE/$TEMPLATE_BIN_REL"   # see step 1 for why
        (cd "$CLONE" && scons platform=macos arch=arm64 target=template_release \
            module_mono_enabled=yes dev_build=no "-j$JOBS")
        template_rel="$TEMPLATE_BIN_REL"
        [ -x "$CLONE/$template_rel" ] || { echo "error: template build produced no $template_rel" >&2; exit 1; }
    fi

    editor_abs="$CLONE/$editor_rel"
    template_abs="$CLONE/$template_rel"
    godotsharp_src="$CLONE/bin/GodotSharp"
fi

echo "== 5/6 Assembling the artifact root =="
ln -sfn "$editor_abs" "$ROOT/editor_bin$EXE"
ln -sfn "$template_abs" "$ROOT/template_bin$EXE"
rm -rf "$ROOT/GodotSharp"
cp -R "$godotsharp_src" "$ROOT/GodotSharp"
printf '%s\n' "$head_commit" > "$ROOT/pin.txt"
# Record the clone the gates' ABI tripwire must fingerprint. It used to be
# recoverable from editor_bin alone — the link pointed at <clone>/bin/<binary>,
# so dirname twice was the clone — but a prebuilt editor lives nowhere near the
# clone, and that inference silently lands two directories up from an install
# dir. Write it down instead of re-deriving it.
printf '%s\n' "$(cd "$CLONE" && pwd)" > "$ROOT/clone.txt"

echo "== 6/6 Summary =="
# Two-step rather than `| head -1`: head quits, ls dies of SIGPIPE and
# `pipefail` makes that the status. This file is standalone by design
# (see the header), so it does not get _common.sh's first_line.
sdk_nupkgs="$(ls "$ROOT"/nuget/Godot.NET.Sdk.*.nupkg 2>/dev/null || true)"
sdk_nupkg="${sdk_nupkgs%%$'\n'*}"
echo "root:         $ROOT"
if [ -n "$PREBUILT" ]; then
    echo "source:       official prebuilt install ($PREBUILT)"
else
    echo "source:       scons build from $CLONE"
    echo "clone hash:   $CLONE_HASH (stamped in $GLUE_STAMP"
    echo "                                     and $ASSEMBLIES_STAMP)"
fi
echo "engine says:  $(binary_reported_version "$editor_abs") (pin ${PINNED_COMMIT:0:9})"
echo "editor_bin:   $ROOT/editor_bin$EXE -> $editor_abs"
echo "template_bin: $ROOT/template_bin$EXE -> $template_abs"
echo "GodotSharp:   $ROOT/GodotSharp/Api/Release/GodotSharp.dll"
echo "nuget feed:   $ROOT/nuget (${sdk_nupkg:-Godot.NET.Sdk nupkg MISSING})"
echo "pin:          $ROOT/pin.txt ($head_commit)"
echo
echo "export DN2CPP_GODOT_DOTNET_ROOT=\"$ROOT\""
echo "then: ./gates/dotnet-measure.sh"
