#!/usr/bin/env bash
# stage-gdextension-libs.sh PROJECT_DIR DEST_DIR [FEATURE...]
#
# Stage the native binaries of a game's OWN GDExtensions — the ones the project
# declares in its `.gdextension` files, not the one dn2cpp generates — where the
# engine will find them when the game runs from a PCK in DEST_DIR.
#
# A manual aid outside the `build-and-run-*.sh` glob (like gates/verify-locks.sh
# and the measure-* scripts): it builds nothing and asserts nothing about
# dn2cpp, it moves files. What runs it is a gate section
# (build-and-run-godot-dotnet-sample.sh 7/7) and the drop-in run flow, whose
# repo-external half is the Thrive harness.
#
# WHY THIS EXISTS, AND WHY IT STAGES FLAT
# ---------------------------------------
# A `.gdextension` names its binary in `res://` terms, and Thrive's says
#
#     macos.arm64 = "res://lib/libthrive_extension_without_avx.dylib"
#
# Two layouts resolve that for free and one does not:
#
#   - run from the PROJECT DIRECTORY (the editor): `resource_path` is the
#     project dir, so `res://lib/x` globalizes to an absolute path inside it.
#   - an EXPORTED .app / .apk / web bundle: the engine's own GDExtension export
#     plugin (editor/export/gdextension_export_plugin.h) calls
#     `add_shared_object()` for the selected library, and the platform exporter
#     writes it out — into `Contents/Frameworks/` on macOS, beside the
#     executable on Windows and Linux, flat beside `index.html` on Web.
#   - the DROP-IN RUN layout — a template engine binary plus `godot.pck` plus
#     `data_<Assembly>_<platform>_<arch>/`, which is what this lane's gates
#     assemble by hand and what the Thrive harness runs. **Here nothing stages
#     anything**: `--export-pack` runs no export platform, so no export plugin
#     runs, so `add_shared_object` is never called.
#
# In that third layout `ProjectSettings::resource_path` is EMPTY (the data comes
# from a pck, not a directory), and `globalize_path()` (core/config/
# project_settings.cpp:266) falls through to `p_path.replace("res://", "")` —
# it hands the loader the bare RELATIVE path `lib/libthrive_extension...`.
# Measured, that is exactly what the failure says:
#
#     ERROR: Can't open dynamic library: lib/libthrive_extension_without_avx.dylib.
#        at: open_dynamic_library (platform/macos/os_macos.mm:420)
#     ERROR: Can't open GDExtension dynamic library: 'res://lib/thrive_extension.gdextension'.
#
# The engine then tries, in order (macOS `OS_MacOS::open_dynamic_library`,
# os_macos.mm:401; Unix `OS_Unix::open_dynamic_library`, drivers/unix/
# os_unix.cpp:1044; Windows `OS_Windows::open_dynamic_library`,
# platform/windows/os_windows.cpp:477):
#
#   1. the path as declared — resolved against the CURRENT WORKING DIRECTORY;
#   2. `<exe_dir>/<basename>`  — every one of the three platforms, unconditionally;
#   3. `<exe_dir>/../Frameworks/<basename>` (macOS) or `<exe_dir>/../lib/<basename>`
#      (Unix); Windows has no third tier.
#
# So tier 2 is the one location that works on all three platforms, regardless of
# the caller's CWD, and regardless of which `res://` sub-directory the
# declaration happened to name. **That is why this script stages FLAT into
# DEST_DIR by basename** rather than reproducing the `res://lib/` sub-directory
# the ticket's title names. Reproducing `lib/` also works — but only through
# tier 1, i.e. only while the game is launched with its own directory as the
# CWD, which is a property of the launcher and not of the layout. Tier 2 is also
# the tier a real macOS `.app` export relies on, so staging flat is not a
# dn2cpp-specific arrangement: it is the same resolution the engine's own
# exporter targets.
#
# WHY THE SET IS DERIVED AND NOT ENUMERATED
# -----------------------------------------
# The set comes from `<project>/.godot/extension_list.cfg` — the file
# `GDExtensionManager::load_extensions()` itself reads — and then from each
# named `.gdextension`'s `[libraries]` section, selected by the same rule the
# loader uses (`GDExtensionLibraryLoader::find_extension_library`: every tag in
# the dotted key must be a feature of this run, and among those the key with the
# MOST tags wins). Naming ThriveExtension in a script would be the same shape of
# mistake `dist/package-toolchain.sh` shipped once with its hardcoded sibling
# list: it is right until the day a project adds a second extension, and then it
# is silently short. Deriving from extension_list.cfg also excludes
# `.gdextension` files that merely sit under the project root without being
# loaded — Thrive has one, `third_party/godot-cpp/test/project/example.gdextension`.
#
# What it does NOT cover, deliberately: a native library the game loads by
# `DllImport`/`dlopen` without declaring it anywhere (Thrive's
# libthrive_native_without_avx.dylib is one). Nothing declares those, so nothing
# can derive them; they stay the caller's business. This script covers exactly
# the declared surface, and says what it staged so the caller can see the rest.
#
# Failures are loud and specific. A `.gdextension` with no entry matching this
# run, a declared file that is not there, and two different sources colliding on
# one basename are each an error naming the file — because the failure this
# replaces (stage nothing, notice at run time) is a dlopen error deep in the
# engine that names no cause.
set -uo pipefail

die() { echo "error: stage-gdextension-libs: $*" >&2; exit 2; }

[ "$#" -ge 2 ] || die "usage: $0 PROJECT_DIR DEST_DIR [FEATURE...]"
PROJECT="$1"; DEST="$2"; shift 2

[ -d "$PROJECT" ] || die "PROJECT_DIR=$PROJECT is not a directory"
[ -d "$DEST" ] || die "DEST_DIR=$DEST is not a directory"

# ── the feature set of the run we are staging for ────────────────────────────
# Mirrors OS::has_feature (core/os/os.cpp:471) for the tags a `.gdextension`
# key can realistically carry. `debug`, `editor` and `template_debug` are
# deliberately absent: this stages a release drop-in run, and a key gated on
# them must not win. On Linux both spellings of the platform are supplied —
# `linuxbsd` is what OS_LinuxBSD::get_identifier() answers at run time, while
# `linux` is the spelling the export-time feature set uses and the one nearly
# every real `.gdextension` in the wild is written against (Thrive's is).
# Supplying both can only ever widen the match, and a widened match that picks
# two different files for one basename is caught by the collision check below.
FEATURES=()
if [ "$#" -gt 0 ]; then
    FEATURES=("$@")
else
    case "$(uname -s)" in
        Darwin)  FEATURES+=(macos) ;;
        Linux)   FEATURES+=(linuxbsd linux) ;;
        MINGW*|MSYS*|CYGWIN*|Windows_NT) FEATURES+=(windows) ;;
        *)       die "unknown host $(uname -s) — pass the feature tags explicitly" ;;
    esac
    case "$(uname -m)" in
        arm64|aarch64) FEATURES+=(arm64 arm 64) ;;
        x86_64|amd64)  FEATURES+=(x86_64 x86 64) ;;
        i386|i686)     FEATURES+=(x86_32 x86 32) ;;
        armv7l|armv7)  FEATURES+=(arm32 armv7 arm 32) ;;
        riscv64)       FEATURES+=(rv64 riscv 64) ;;
        *)             die "unknown machine $(uname -m) — pass the feature tags explicitly" ;;
    esac
    # macOS templates are universal binaries; the engine answers `universal`
    # true there for both slices (os.cpp's MACOS_ENABLED arms).
    [ "$(uname -s)" = Darwin ] && FEATURES+=(universal)
    FEATURES+=(release template_release template single threads)
fi

has_feature() {
    local f
    for f in "${FEATURES[@]}"; do [ "$f" = "$1" ] && return 0; done
    return 1
}

# ── minimal ConfigFile reading ───────────────────────────────────────────────
# ini_section FILE SECTION — echo `key<TAB>value` for each entry of SECTION,
# with surrounding whitespace and the value's quotes stripped. Comments (`#`,
# `;`) and blank lines are dropped. Godot writes these files itself, so the
# grammar in play is the one ConfigFile emits; anything richer (a value spanning
# lines) is not something a `[libraries]` entry can be.
ini_section() {
    awk -v want="$2" '
        /^[[:space:]]*[;#]/ { next }
        /^[[:space:]]*\[/ {
            s = $0; sub(/^[[:space:]]*\[/, "", s); sub(/\].*$/, "", s)
            in_section = (s == want); next
        }
        !in_section { next }
        /=/ {
            eq = index($0, "=")
            k = substr($0, 1, eq - 1); v = substr($0, eq + 1)
            gsub(/^[[:space:]]+|[[:space:]]+$/, "", k)
            gsub(/^[[:space:]]+|[[:space:]]+$/, "", v)
            if (k == "") next
            printf "%s\t%s\n", k, v
        }
    ' "$1"
}

unquote() { local v="$1"; v="${v%\"}"; v="${v#\"}"; printf '%s' "$v"; }

# tags_met KEY — true when every dot-separated tag of KEY is a feature here.
tags_met() {
    local IFS=. tag
    for tag in $1; do
        tag="${tag// /}"
        [ -n "$tag" ] || continue
        has_feature "$tag" || return 1
    done
    return 0
}

# resolve_res EXT_FILE PATH — turn a `res://…` or extension-relative PATH into a
# filesystem path. `find_extension_library` joins a relative value onto the
# `.gdextension`'s own directory; `res://` is the project root.
resolve_res() {
    local ext="$1" p="$2"
    case "$p" in
        res://*) printf '%s/%s' "$PROJECT" "${p#res://}" ;;
        /*)      printf '%s' "$p" ;;
        *)       printf '%s/%s' "$(dirname "$ext")" "$p" ;;
    esac
}

# ── the extensions this project actually loads ───────────────────────────────
LIST="$PROJECT/.godot/extension_list.cfg"
[ -f "$LIST" ] || die "no $LIST.
       That file is what GDExtensionManager::load_extensions() itself reads, so
       it is the only honest source for which .gdextension files this project
       loads. It is written by an import:
           godot --headless --path $PROJECT --import"

# ── select, then stage ───────────────────────────────────────────────────────
# STAGED_FROM accumulates `basename<TAB>source` lines, so a second source
# claiming a basename already taken is an error rather than a silent overwrite.
# Two libraries with one basename cannot both be found by tier-2 resolution —
# the same reason a macOS `Contents/Frameworks` cannot hold them either. It is a
# string and not an associative array because the bash the gates run under on
# macOS is the system 3.2, which has none.
STAGED_FROM=""
staged=0

stage_one() {
    local ext="$1" declared="$2" src base prev
    src="$(resolve_res "$ext" "$declared")"
    [ -e "$src" ] || die "$ext selects \"$declared\" for this run, but $src does not exist.
       Build or fetch the game's own extension binary before staging it."
    base="$(basename "$src")"
    # Herestring, not a pipeline: `awk … exit` is an early-exiting consumer, and
    # under `set -o pipefail` a producer SIGPIPEd by it fails the assignment.
    prev="$(awk -F'\t' -v b="$base" '$1 == b { print $2; exit }' <<<"$STAGED_FROM")"
    if [ -n "$prev" ] && [ "$prev" != "$src" ]; then
        die "two different sources both stage as $base:
         $prev
         $src
       The engine resolves a GDExtension binary by BASENAME from the executable's
       directory, so one of them would shadow the other wherever it is put."
    fi
    STAGED_FROM="$STAGED_FROM$base	$src
"
    # -L: a project's lib/ entry is routinely a symlink into a distributable
    # tree (Thrive's are), and the run dir must not depend on that tree.
    cp -Lf "$src" "$DEST/$base.new" || die "cannot copy $src into $DEST"
    mv -f "$DEST/$base.new" "$DEST/$base" || die "cannot place $base in $DEST"
    echo "   staged $base  <- $src"
    staged=$((staged + 1))
}

while read -r res_path; do
    [ -n "$res_path" ] || continue
    ext="$(resolve_res "" "$res_path")"
    [ -f "$ext" ] || die "$LIST names $res_path, but $ext does not exist"
    echo "== $res_path =="

    # [libraries]: the loader's rule verbatim — all tags met, most tags wins.
    best_key=""; best_val=""; best_n=-1
    while IFS=$'\t' read -r key val; do
        tags_met "$key" || continue
        n="$(printf '%s' "$key" | awk -F. '{ print NF }')"
        [ "$n" -gt "$best_n" ] || continue
        best_n="$n"; best_key="$key"; best_val="$(unquote "$val")"
    done < <(ini_section "$ext" libraries)

    if [ -z "$best_val" ]; then
        if [ -n "$(ini_section "$ext" configuration | awk -F'\t' '$1 == "autodetect_library_prefix" { print $2 }')" ]; then
            die "$ext selects its library through autodetect_library_prefix, which this
       script does not implement. Declare the binary in [libraries] instead, or
       stage it by hand and say so where the run dir is assembled."
        fi
        die "$ext declares no [libraries] entry matching this run.
       features: ${FEATURES[*]}
       declared: $(ini_section "$ext" libraries | cut -f1 | tr '\n' ' ')
       The engine would fail the same way at load time, with a message naming
       only the .gdextension."
    fi
    echo "   [libraries] $best_key"
    stage_one "$ext" "$best_val"

    # [dependencies]: `<tags> = { "src": "target_subdir", … }`. The loader takes
    # the FIRST entry whose tags are all met (find_extension_dependencies breaks
    # out of the loop), so this does too. The declared target sub-directory is
    # an export-layout hint; here every dependency goes flat beside the
    # executable for the same tier-2 reason the library does.
    while IFS=$'\t' read -r key val; do
        tags_met "$key" || continue
        for dep in $(printf '%s' "$val" | grep -oE '"[^"]+"[[:space:]]*:' | sed 's/[[:space:]]*:$//'); do
            stage_one "$ext" "$(unquote "$dep")"
        done
        break
    done < <(ini_section "$ext" dependencies)
done < <(grep -v '^[[:space:]]*$' "$LIST")

echo "== staged $staged GDExtension binaries into $DEST =="
[ "$staged" -gt 0 ] || die "$LIST lists no extension — nothing was staged, which is
       almost certainly not what the caller meant."
