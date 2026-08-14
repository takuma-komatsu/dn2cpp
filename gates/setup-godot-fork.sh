#!/usr/bin/env bash
# SETUP AID (NOT a regression gate — the name is outside the `build-and-run-*.sh`
# glob, so run-all-gates.sh ignores it; the sibling of setup-godot-dotnet.sh).
# Prepares everything gates/build-and-run-godot-editor-export.sh needs from the
# dn2cpp *fork* of godot (branch dn2cpp/main):
#
#   1. an editor binary in the fork's bin/          (reused from the pristine
#                                                    clone, or scons-built)
#   2. mono glue generated into the fork tree
#   3. the dn2cpp export toolchain bundle           (dist/package-toolchain.sh)
#   4. managed assemblies + local nuget feed + the bundle installed into
#      bin/GodotSharp/Dn2Cpp                        (build_assemblies.py --bundle-dn2cpp)
#   5. a release export template + the desktop export template artifact the
#      export preset points at (godot_fork_desktop_template, gates/_godot_fork.sh)
#   6. an artifact root: $ROOT/{<desktop template>,nuget/,bin-cache/,pin.txt,
#                              fork_head.txt,clone.txt,editor.txt,template.txt}
#      — the engine binaries and their GodotSharp tree are referenced in place by
#      recorded path, never linked or copied in (see step 6 for why)
#
# It has one arm per OS whose engine it can build — macOS, Windows and Linux —
# and no fallback: an unported host is refused up front rather than tens of
# minutes into a scons run. gates/_godot_fork.sh's preflight names the same OS
# list, so the gates skip with a remedy that works exactly where one exists.
#
# Idempotent, and "already there" means "built from the sources now in the fork"
# rather than merely present — presence alone is what once let a stale editor pass
# for a fresh one. EVERY product carries a stamp beside it naming the tree it came
# from, keyed by the hash of that tree: the engine binaries on
# godot_fork_engine_hash (steps 1 and 5), the mono glue on the same hash (step 2 —
# the glue is the editor's own output, so the engine sources are its input), the
# desktop export template on godot_fork_engine_provenance (step 5's artifact, the
# same `<template>.provenance` line the Web and iOS zips carry), and the managed
# assemblies + feed on tools_tree_hash (step 4). The two source sets partition the
# fork, so the GodotTools edit-test loop rebuilds the assemblies without triggering
# a scons run, and an engine edit does the reverse.
#
# Why no engine rebuild in the common case
# ----------------------------------------
# GodotTools is a *managed* assembly that the editor loads from
# GodotSharp/Tools/ at run time (godotsharp_dirs.cpp resolves it next to the
# executable), and the fork's whole contract is that it changes GodotTools,
# build_scripts and docs — never engine C++. So a fork whose engine sources are
# byte-identical to the base commit can reuse the pristine clone's already-built
# editor and template binaries, and re-running build_assemblies.py is the entire
# edit-test loop. Step 0 *verifies* that invariant instead of assuming it.
#
# A fork that *does* edit engine C++ gets a scons build (tens of minutes each —
# run this in the background, never inside a gate). Which of the two happens is
# keyed on a hash of the engine sources rather than on presence: each binary
# carries a stamp naming the tree it was built from, and a copy is kept under that
# hash in $ROOT/bin-cache. Both halves are load-bearing. The fork's bin/ is
# *pre-populated* — the binaries were copied in from the pristine clone — so a
# step that skipped on presence alone would hand every downstream gate an editor
# built from before the engine edit and report success. And without the cache,
# reverting that edit, or switching off the branch carrying it, would cost another
# forty minutes rather than a copy, which is the tax that makes people bypass the
# check.
#
# The binaries are copied, not symlinked, into the fork's bin/: macOS resolves
# OS::get_executable_path() through proc_pidpath(), which reports the real path
# of the running image, so an editor launched through a symlink would look for
# GodotSharp/ next to the *link target* — i.e. the pristine clone's assemblies,
# not the fork's.
#
# Usage:
#   ./gates/setup-godot-fork.sh [ROOT]
#
#   ROOT                     artifact/cache root (default: $DN2CPP_GODOT_FORK_ROOT,
#                            then $HOME/.cache/dn2cpp-godot-fork)
#   DN2CPP_GODOT_FORK_CLONE  fork clone — its own checkout with its own .git,
#                            not a worktree of the pristine one (default:
#                            sibling directory of this dn2cpp checkout, i.e.
#                            ../godot-dn2cpp)
#   DN2CPP_GODOT_CLONE       pristine clone (default: sibling directory of this
#                            dn2cpp checkout, i.e. ../godot)
set -euo pipefail
cd "$(dirname "$0")/.."

# The upstream commit the fork is based on. Not the fork's HEAD: that moves with
# every fork commit, while the contract dn2cpp depends on is "dn2cpp/main
# descends from this 4.7.1-stable commit, whose interop ABI we fingerprinted".
BASE_COMMIT=a13da4feb8d8aefc283c3763d33a2f170a18d541

ROOT="${1:-${DN2CPP_GODOT_FORK_ROOT:-$HOME/.cache/dn2cpp-godot-fork}}"
FORK="${DN2CPP_GODOT_FORK_CLONE:-$(dirname "$(pwd)")/godot-dn2cpp}"
PRISTINE="${DN2CPP_GODOT_CLONE:-$(dirname "$(pwd)")/godot}"
JOBS="$(sysctl -n hw.ncpu 2>/dev/null || nproc 2>/dev/null || echo 8)"
HOST_MACHINE="$(uname -m)"

# ── Host platform seam ────────────────────────────────────────────────────────
# Inlined rather than sourced from gates/_common.sh, like the copy in
# gates/setup-godot-dotnet.sh: every setup-*.sh is standalone, and _common.sh is
# a *gate* helper — sourcing it would drag in the MSVC env import, the console
# code page and the gate cache. gates/_godot_fork.sh IS sourced (as the web setup
# aid already does), because the names this script WRITES are the ones the gates
# READ and a second copy of them is what drifts: godot_fork_desktop_template,
# FORK_EXE, and the pin/ABI expectation paths all come from there.
detect_os() {
    case "$(uname -s)" in
        Darwin)               printf 'macos\n' ;;
        Linux)                printf 'linux\n' ;;
        CYGWIN*|MINGW*|MSYS*) printf 'windows\n' ;;
        *)                    printf 'unknown\n' ;;
    esac
}
DN2CPP_OS=${DN2CPP_OS:-$(detect_os)}
# _godot_fork.sh reads the recorded editor/template paths out of
# DN2CPP_GODOT_FORK_ROOT, which this script's ROOT can override from $1. Pin the
# two together before sourcing, or a run with an explicit root would resolve
# against the default one.
export DN2CPP_GODOT_FORK_ROOT="$ROOT"
source "$(dirname "$0")/_godot_fork.sh"
ARCH="$(godot_fork_host_arch "$HOST_MACHINE")"

# One arm per OS a fork engine can be built on. There is no fallback arm: an
# unported host must be told so here, not discover it tens of minutes into a
# scons build for a platform this script cannot assemble a cache from — which is
# also what gates/_godot_fork.sh's preflight promises, and its OS list has to
# track this case exactly.
case "$DN2CPP_OS" in
    macos)   SCONS_PLATFORM=macos ;;
    windows) SCONS_PLATFORM=windows ;;
    # scons spells Linux "linuxbsd", and so do the binary names EDITOR_REL builds
    # from it — the same name the engine uses for the platform everywhere else.
    linux)   SCONS_PLATFORM=linuxbsd ;;
    *)
        echo "error: gates/setup-godot-fork.sh has no $DN2CPP_OS arm — it can build a fork" >&2
        echo "       engine for macOS, Windows and Linux only, so no fork cache can be" >&2
        echo "       produced on this host. The editor-export gates gate_skip accordingly." >&2
        exit 1
        ;;
esac

# resolve_python — pick an interpreter by RUNNING a candidate, never by testing
# its name or asking `command -v`. The probe is not defensiveness: a stock Windows
# install answers `python3` with an app-execution-alias stub that resolves AND
# launches, so both of the cheap tests report success on something that is not a
# Python.
#
# This duplicates gates/_common.sh's copy, deliberately: that file is a *gate*
# helper whose source-time effects (MSVC env import, console code page, node PATH)
# have no business in a build aid, and this script must stay runnable without it.
# The rule the two copies share is the probe, not the code. (The SCons resolver
# has no such constraint and is shared for real — see godot_fork_scons below.)
resolve_python() {
    local cand
    for cand in python3 python "py -3"; do
        # shellcheck disable=SC2086
        if $cand -c 'print(1)' >/dev/null 2>&1; then
            printf '%s\n' "$cand"
            return 0
        fi
    done
    echo "error: no working Python 3 interpreter found (tried: python3, python, py -3)" >&2
    return 1
}

PY="$(resolve_python)"
# The SCons launcher is godot_fork_scons (gates/_godot_fork.sh), shared with the
# Web setup aid — same probe-by-running rule, one definition.
SCONS="$(godot_fork_scons "$PY")"

EDITOR_REL="bin/godot.$SCONS_PLATFORM.editor.$ARCH.mono$FORK_EXE"
TEMPLATE_REL="bin/godot.$SCONS_PLATFORM.template_release.$ARCH.mono$FORK_EXE"
GLUE_MARKER=modules/mono/glue/GodotSharp/GodotSharp/Generated/GeneratedIncludes.props
API_RELEASE_REL=bin/GodotSharp/Api/Release/GodotSharp.dll
TOOLCHAIN_MARKER=bin/GodotSharp/Dn2Cpp/manifest.json

echo "== 0/6 Verifying the fork =="
if [ ! -e "$FORK/.git" ]; then
    echo "error: fork clone not found at $FORK" >&2
    echo "       git clone https://github.com/takuma-komatsu/godot-dn2cpp.git $FORK" >&2
    echo "       git -C $FORK checkout dn2cpp/main" >&2
    exit 1
fi
if ! git -C "$FORK" merge-base --is-ancestor "$BASE_COMMIT" HEAD; then
    echo "error: the fork at $FORK does not descend from the pinned base commit" >&2
    echo "       base: $BASE_COMMIT" >&2
    echo "       head: $(git -C "$FORK" rev-parse HEAD)" >&2
    echo "       Rebase dn2cpp/main onto the base (or re-pin deliberately: see" >&2
    echo "       docs/EDITOR-EXPORT-DESIGN.md §7)." >&2
    exit 1
fi
FORK_HEAD="$(git -C "$FORK" rev-parse HEAD)"
echo "fork: $FORK @ $FORK_HEAD (descends from $BASE_COMMIT)"

if [ ! -f "$FORK_PIN_EXPECTED" ]; then
    printf '%s\n' "$BASE_COMMIT" > "$FORK_PIN_EXPECTED"
    echo "generated $FORK_PIN_EXPECTED (first run) — commit it"
elif [ "$(cat "$FORK_PIN_EXPECTED")" != "$BASE_COMMIT" ]; then
    echo "error: $FORK_PIN_EXPECTED disagrees with this script's BASE_COMMIT" >&2
    echo "       file:   $(cat "$FORK_PIN_EXPECTED")" >&2
    echo "       script: $BASE_COMMIT" >&2
    exit 1
fi

# The interop-ABI surfaces the mono-module drop-in hard-codes assumptions about,
# fingerprinted over the *fork* tree against the same expectation the DM gates
# hold the pristine clone to. Same shape as _godot_dotnet.sh's tripwire.
abi_actual="$(
    awk '/^static const void \*unmanaged_callbacks\[\]\{/{f=1} f{print} f&&/^\};/{exit}' \
        "$FORK/modules/mono/glue/runtime_interop.cpp" \
        | shasum -a 256 | awk '{print $1"  unmanaged_callbacks"}'
    shasum -a 256 "$FORK/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ManagedCallbacks.cs" \
        | awk '{print $1"  ManagedCallbacks.cs"}'
    shasum -a 256 "$FORK/modules/mono/glue/GodotSharp/GodotSharp/Core/NativeInterop/NativeFuncs.cs" \
        | awk '{print $1"  NativeFuncs.cs"}'
)"
if [ "$abi_actual" != "$(cat "$ABI_EXPECTED")" ]; then
    echo "error: the fork's interop ABI fingerprints differ from $ABI_EXPECTED" >&2
    echo "expected:" >&2; cat "$ABI_EXPECTED" >&2
    echo "actual:" >&2; printf '%s\n' "$abi_actual" >&2
    echo "       The fork must never touch unmanaged_callbacks[], ManagedCallbacks or NativeFuncs." >&2
    exit 1
fi
echo "interop ABI fingerprints OK (fork is drop-in compatible)"

# FORK_OWNED (the definition of "the engine sources") and godot_fork_engine_hash
# (the fingerprint of them) were written here and now live in
# gates/_godot_fork.sh, sourced above: the two Web/iOS setup aids stamp the
# export templates they bake with the same hash, and the export gates refuse a
# template stamped with another one. Same value, same rules — the
# definition is stated once there, including why it fingerprints content rather
# than git state.
ENGINE_HASH="$(godot_fork_engine_hash)"
CACHE="$ROOT/bin-cache/$ENGINE_HASH"

# The COMPLEMENT of FORK_OWNED: the managed trees step 4's build_assemblies.py
# compiles, which is exactly what the engine hash excludes so that the fork's
# edit-test loop does not trigger a scons run. The two hashes partition the fork's
# sources by which product they describe — this one keys GodotSharp/{Tools,Api,
# Dn2Cpp} and the local nuget feed, the engine one keys the two engine binaries.
#
# Derived from what build_assemblies.py's build_all actually reads, not from the
# exclusion list: generate_sdk_package_versions (version.py -> the gitignored
# SdkPackageVersions.props), build_godot_api (the GodotSharp glue sources ->
# Api/Release/GodotSharp.dll), GodotTools.sln (-> Tools/GodotTools.dll) and
# Godot.NET.Sdk (-> the nupkgs pushed to the feed). So it is a superset of the
# complement: version.py and modules/mono/glue/GodotSharp are FORK_OWNED too, and
# feed BOTH hashes, because they feed both products.
#
# `modules/mono/editor` is deliberately NOT taken whole — that directory also
# holds engine C++ (editor_internal_calls.cpp, bindings_generator.cpp), which is
# the engine hash's and whose change already forces a scons build.
TOOLS_OWNED=(
    'version.py'
    'modules/mono/build_scripts'
    'modules/mono/editor/GodotTools'
    'modules/mono/editor/Godot.NET.Sdk'
    'modules/mono/glue/GodotSharp'
)

# tools_tree_hash — godot_fork_engine_hash's twin over TOOLS_OWNED, same rules and for
# the same reasons: content rather than git state (so staging or committing a
# GodotTools edit is not a rebuild, and reaching the same sources from another
# branch is not either), --binary so a modified binary file is fingerprinted by
# content, and untracked files hashed in. Everything build_assemblies.py *writes*
# is gitignored (bin/, obj/, SdkPackageVersions.props, the generated glue and
# source-generator constants), so its own products cannot feed the hash that keys
# them — which is what keeps the stamp from moving on every build.
tools_tree_hash() {
    (
        cd "$FORK"
        printf 'base %s\n' "$BASE_COMMIT"
        git diff --no-ext-diff --no-textconv --no-color --no-renames --binary \
            "$BASE_COMMIT" -- "${TOOLS_OWNED[@]}"
        # Not `xargs`, for godot_fork_engine_hash's reason: with no untracked
        # file GNU xargs still runs shasum, which hashes its own stdin, and the
        # same tree would then key a different cache entry per host flavour.
        git ls-files --others --exclude-standard -z -- "${TOOLS_OWNED[@]}" \
            | while IFS= read -r -d '' f; do shasum -a 256 "$f"; done
    ) | shasum -a 256 | cut -c1-16
}
TOOLS_HASH="$(tools_tree_hash)"
# Beside the tree it describes, under the fork's gitignored bin/ — the same
# placement and the same rule as an engine binary's .engine-hash stamp.
TOOLS_STAMP="$FORK/bin/GodotSharp/.tools-hash"

engine_drift="$(
    git -C "$FORK" diff --name-only "$BASE_COMMIT" -- "${FORK_OWNED[@]}"
    git -C "$FORK" ls-files --others --exclude-standard -- "${FORK_OWNED[@]}"
)"
if [ -n "$engine_drift" ]; then
    ENGINE_UNCHANGED=0
    echo "note: engine sources differ from the base commit — the pristine clone's"
    echo "      binaries do not describe this tree, so each engine binary is taken"
    echo "      from the cache entry for it, or scons-built (tens of minutes):"
    printf '%s\n' "$engine_drift" | LC_ALL=C sed 's/^/      /'
else
    ENGINE_UNCHANGED=1
    echo "engine sources are identical to $BASE_COMMIT — prebuilt binaries are reusable"
fi
echo "engine hash: $ENGINE_HASH (binary cache: $CACHE)"
mkdir -p "$ROOT"

# stamp_binary REL — record the engine tree $FORK/REL was built from, beside it.
# The stamp lives in the fork's bin/, which Godot's .gitignore covers.
stamp_binary() {
    printf '%s\n' "$ENGINE_HASH" > "$FORK/$1.engine-hash"
}

# install_binary SRC REL — put SRC at $FORK/REL and stamp it. Copied, never
# symlinked, for the proc_pidpath() reason in the header. Staged through a temp
# file: an interrupted copy must not leave a truncated binary standing behind a
# stamp that calls it current, and the stamp is written last for the same reason.
install_binary() {
    local src="$1" rel="$2" dst="$FORK/$2"
    mkdir -p "$(dirname "$dst")"
    cp "$src" "$dst.tmp"
    chmod +x "$dst.tmp"
    mv -f "$dst.tmp" "$dst"
    stamp_binary "$rel"
}

# cache_binary SRC REL — keep SRC as this engine tree's cache entry for REL.
cache_binary() {
    local src="$1" dst="$CACHE/$(basename "$2")"
    mkdir -p "$CACHE"
    cp "$src" "$dst.tmp"
    chmod +x "$dst.tmp"
    mv -f "$dst.tmp" "$dst"
}

# provide_binary REL DESCRIPTION SCONS_TARGET — make $FORK/REL exist *and be the
# product of the engine sources now in the fork*. In order: the binary already
# there was built from this tree (its stamp says so); the cache holds one that
# was; the engine is unchanged, so the pristine clone's prebuilt one describes
# this tree; else scons.
provide_binary() {
    local rel="$1" what="$2" scons_target="$3"
    local stamp="$FORK/$rel.engine-hash" cached="$CACHE/$(basename "$rel")"

    if [ -x "$FORK/$rel" ] && [ -f "$stamp" ] && [ "$(cat "$stamp")" = "$ENGINE_HASH" ]; then
        echo "skip: $what already built from these engine sources: $FORK/$rel"
        return
    fi
    if [ -x "$FORK/$rel" ]; then
        echo "-- the $what in the fork predates the current engine sources"
    fi
    if [ -x "$cached" ]; then
        echo "-- cache hit: the $what built from engine $ENGINE_HASH"
        install_binary "$cached" "$rel"
        return
    fi
    if [ "$ENGINE_UNCHANGED" -eq 1 ] && [ -x "$PRISTINE/$rel" ] \
        && [ "$(git -C "$PRISTINE" rev-parse HEAD)" = "$BASE_COMMIT" ]; then
        echo "-- copying the pristine clone's $what (engine sources unchanged)"
        cache_binary "$PRISTINE/$rel" "$rel"
        install_binary "$cached" "$rel"
        return
    fi
    echo "-- scons: building the $what in the fork (tens of minutes)"
    # Deleted first. scons decides a target is current from its own signature
    # database, which knows nothing about the binaries this script copies into
    # bin/ — so a cached or pristine binary left at the target path could outlive
    # a build scons treats as a no-op, and then get stamped as this tree's
    # product. A *missing* target is always relinked.
    rm -f "$FORK/$rel"
    (cd "$FORK" && $SCONS platform="$SCONS_PLATFORM" "arch=$ARCH" "target=$scons_target" \
        module_mono_enabled=yes dev_build=no "-j$JOBS")
    [ -x "$FORK/$rel" ] || { echo "error: scons produced no $rel" >&2; exit 1; }
    cache_binary "$FORK/$rel" "$rel"
    stamp_binary "$rel"
}

echo "== 1/6 Editor binary =="
provide_binary "$EDITOR_REL" "mono editor" editor

echo "== 2/6 Mono glue =="
# Keyed on the engine tree, not on the marker file existing. The glue is
# the EDITOR's output — `--generate-mono-glue` walks the ClassDB the binary was
# compiled with — so its input is exactly what ENGINE_HASH fingerprints, and a
# skip on presence handed step 4 a glue generated by a previous engine. That is
# the shape the re-pin hit: a checkout alone leaves the marker standing, so the
# glue (and the assemblies compiled from it) stayed at the old base while
# pin.txt was rewritten to the new one.
#
# The stamp goes beside the marker, inside the generated directory, which Godot's
# modules/mono/glue/GodotSharp/.gitignore covers — so it can neither feed
# ENGINE_HASH nor TOOLS_HASH (both hash only tracked deltas and *non-ignored*
# untracked files), which is what keeps a stamp from moving the hash that keys
# it. Same placement rule as an engine binary's .engine-hash.
GLUE_STAMP="$FORK/$GLUE_MARKER.engine-hash"
if [ -f "$FORK/$GLUE_MARKER" ] && [ -f "$GLUE_STAMP" ] \
    && [ "$(cat "$GLUE_STAMP")" = "$ENGINE_HASH" ]; then
    echo "skip: glue already generated from these engine sources: $FORK/$GLUE_MARKER"
else
    if [ -f "$FORK/$GLUE_MARKER" ]; then
        echo "-- the glue in the fork predates the current engine sources"
    fi
    # Removed before the run and written after it, for install_binary's reason: an
    # interrupted generation must leave a glue nothing calls current.
    rm -f "$GLUE_STAMP"
    (cd "$FORK" && "./$EDITOR_REL" --headless --generate-mono-glue modules/mono/glue)
    [ -f "$FORK/$GLUE_MARKER" ] || { echo "error: glue generation produced no $GLUE_MARKER" >&2; exit 1; }
    printf '%s\n' "$ENGINE_HASH" > "$GLUE_STAMP"
fi

echo "== 3/6 dn2cpp export toolchain bundle =="
# Reuses a prebuilt artifacts/selfhost-fullcli/dn2cpp when one exists, otherwise
# builds it via gates/selfhost-emit.sh — many minutes on a cold tree.
bash dist/package-toolchain.sh --layout-only
# The bundle name records uname's host identity; its Godot products use $ARCH.
LAYOUT="$PWD/artifacts/toolchain/dn2cpp-toolchain-0.1.0-$DN2CPP_OS-$HOST_MACHINE"
[ -x "$LAYOUT/bin/dn2cpp$FORK_EXE" ] || { echo "error: no toolchain layout at $LAYOUT" >&2; exit 1; }

echo "== 4/6 Managed assemblies + nuget feed + toolchain install =="
# Keyed on the MANAGED source tree, never on the products alone — the same rule
# provide_binary applies to the engine binaries, and here it is the load-bearing
# half of the whole fork edit-test loop.
#
# A guard that skips on the products existing self-hides, and nothing downstream
# can see through it: the fork gates READ Tools/GodotTools.dll as their subject,
# and godot_fork_ctx's `tools=` hashes that same DLL into their cache KEY. So a
# GodotTools edit that never got compiled leaves an editor that is
# self-consistently green — the gates assert the old exporter's behavior against
# the old exporter, and the key does not move because the subject did not.
# Neither the pin nor the ABI tripwire can help: both fingerprint the clone's
# sources, which are correct. This is not hypothetical: a staged GodotTools.dll has
# already outlived the Dn2CppExporter.cs edit that was supposed to be under test,
# with the whole suite green over an editor that did not contain it.
#
# The dn2cpp bundle installed by --bundle-dn2cpp is NOT in this hash — it is a
# dn2cpp-side product, and the gates re-package and re-stage it every run
# (stage_editor_toolchain), so this stamp has no business claiming it is current.
if compgen -G "$ROOT/nuget/Godot.NET.Sdk.*.nupkg" >/dev/null && [ -f "$FORK/$API_RELEASE_REL" ] \
    && [ -f "$FORK/$TOOLCHAIN_MARKER" ] \
    && [ -f "$TOOLS_STAMP" ] && [ "$(cat "$TOOLS_STAMP")" = "$TOOLS_HASH" ]; then
    echo "skip: assemblies + feed + toolchain already built from these managed sources: $FORK/bin/GodotSharp"
else
    if [ -f "$FORK/$API_RELEASE_REL" ] && [ -f "$TOOLS_STAMP" ] \
        && [ "$(cat "$TOOLS_STAMP")" != "$TOOLS_HASH" ]; then
        echo "-- the assemblies in the fork predate the current managed sources"
        echo "   stamped $(cat "$TOOLS_STAMP") != $TOOLS_HASH"
    fi
    mkdir -p "$ROOT/nuget"
    # $PY, never a bare `python3` — see resolve_python at the top of this file for
    # what that name resolves to on a stock Windows box.
    # shellcheck disable=SC2086
    (cd "$FORK" && $PY modules/mono/build_scripts/build_assemblies.py \
        --godot-output-dir=bin \
        --godot-platform="$SCONS_PLATFORM" \
        --push-nupkgs-local "$ROOT/nuget" \
        --bundle-dn2cpp "$LAYOUT")
    [ -f "$FORK/$API_RELEASE_REL" ] || { echo "error: build_assemblies produced no $API_RELEASE_REL" >&2; exit 1; }
    [ -f "$FORK/$TOOLCHAIN_MARKER" ] || { echo "error: --bundle-dn2cpp installed no $TOOLCHAIN_MARKER" >&2; exit 1; }
    # Written LAST, after both products are asserted, for the same reason
    # install_binary stamps last: an interrupted build must not leave a stamp
    # standing behind a half-written GodotSharp tree.
    printf '%s\n' "$TOOLS_HASH" > "$TOOLS_STAMP"
fi

echo "== 5/6 Release template + desktop export template artifact =="
provide_binary "$TEMPLATE_REL" "mono release template" template_release

# What `custom_template/release` is allowed to point at is the exporter's rule,
# not ours, and the two desktop platforms differ: macOS wants a ZIP laid out like
# misc/dist/macos_template.app (it picks Contents/MacOS/godot_macos_release.<arch>
# out of it and renames that to the project name), Windows wants the release
# template EXECUTABLE itself (it copies it to <project>.exe). The name each lands
# under is godot_fork_desktop_template's, so the gate reading it cannot drift from
# this script writing it.
#
# Only the release binary is provided either way: the gate exports release, and
# pairing a debug entry with the same binary would misreport what an
# --export-debug produced.
TEMPLATE_ARTIFACT="$(godot_fork_desktop_template "$ROOT")"
# Keyed on a provenance stamp, exactly like the Web and iOS zips the two setup
# aids bake — `<template>.provenance`, godot_fork_engine_provenance — and read by
# gates/build-and-run-godot-editor-export.sh, which refuses a template stamped
# with another engine.
#
# What it replaces was an `-nt` chain against the release binary, and the failure
# was the one an mtime comparison always has: "newer than" is not "built from".
# Concretely, the engine binary is only *touched* when provide_binary installs
# one, so the common path — a run whose engine stamp already matches, which
# leaves $FORK/$TEMPLATE_REL's mtime alone — left any artifact assembled after
# it looking fresh forever. A zip restored from a backup, copied between
# machines, or left over from before a re-pin therefore read as current while
# carrying an engine nobody asked for, and the exporter cannot see through it:
# `custom_template` is validated by FileAccess::exists alone (the reasoning is
# written out at godot_fork_engine_provenance).
TEMPLATE_STAMP="$TEMPLATE_ARTIFACT.provenance"
ENGINE_PROVENANCE="$(godot_fork_engine_provenance)"
if [ -f "$TEMPLATE_ARTIFACT" ] \
    && [ "$(head -1 "$TEMPLATE_STAMP" 2>/dev/null || true)" = "$ENGINE_PROVENANCE" ]; then
    echo "skip: desktop template artifact already assembled from these engine sources: $TEMPLATE_ARTIFACT"
else
    # The stamp never outlives the artifact it describes: removed before the
    # bake, written after it. An interrupted bake then leaves an UNSTAMPED
    # template — which the gate refuses — rather than a stamped half-written one,
    # which it would use. Same ordering, and the same reason, as install_binary's.
    rm -f "$TEMPLATE_STAMP"
    if [ "$DN2CPP_OS" = macos ]; then
        STAGE="$ROOT/template_stage"
        rm -rf "$STAGE"
        mkdir -p "$STAGE/macos_template.app/Contents/MacOS"
        cp -R "$FORK/misc/dist/macos_template.app/Contents/Info.plist" \
              "$FORK/misc/dist/macos_template.app/Contents/PkgInfo" \
              "$FORK/misc/dist/macos_template.app/Contents/Resources" \
              "$STAGE/macos_template.app/Contents/"
        cp "$FORK/$TEMPLATE_REL" "$STAGE/macos_template.app/Contents/MacOS/godot_macos_release.$ARCH"
        chmod +x "$STAGE/macos_template.app/Contents/MacOS/godot_macos_release.$ARCH"
        rm -f "$TEMPLATE_ARTIFACT"
        (cd "$STAGE" && zip -qry "$TEMPLATE_ARTIFACT" macos_template.app)
        rm -rf "$STAGE"
    else
        # Copied, not symlinked: this path is handed to the *engine*, which opens
        # it with the Win32 file API rather than through this shell.
        cp -f "$FORK/$TEMPLATE_REL" "$TEMPLATE_ARTIFACT.tmp"
        chmod +x "$TEMPLATE_ARTIFACT.tmp"
        mv -f "$TEMPLATE_ARTIFACT.tmp" "$TEMPLATE_ARTIFACT"
    fi
    printf '%s\n' "$ENGINE_PROVENANCE" > "$TEMPLATE_STAMP"
    echo "-- desktop template artifact: $TEMPLATE_ARTIFACT ($ENGINE_PROVENANCE)"
fi

echo "== 6/6 Assembling the artifact root =="
# The two engine binaries and the GodotSharp tree beside them are referenced IN
# PLACE, by recorded path — never linked into the root, and never copied there.
# They used to be symlinks, which is a dependency this lane must not have: on
# stock MSYS/Git-Bash `ln -s` COPIES (no winsymlinks mode, or no privilege to
# create a link) and says nothing about it, so the root ended up holding a copy
# of the editor that gates/_godot_fork.sh had no link to read — the worktree
# derivation collapsed to "." and every fork gate FAILed naming that — and a
# copy of GodotSharp, which is the quieter half: it works, while the editor loads
# a snapshot of the very tree build_assemblies.py rewrites in the GodotTools
# edit-test loop. A recorded path has neither failure and needs no privilege.
FORK_ABS="$(cd "$FORK" && pwd)"
printf '%s\n' "$FORK_ABS/$EDITOR_REL" > "$ROOT/editor.txt"
printf '%s\n' "$FORK_ABS/$TEMPLATE_REL" > "$ROOT/template.txt"
printf '%s\n' "$FORK_ABS" > "$ROOT/clone.txt"
# Retire a previous layout's names, so nothing reads an editor or a GodotSharp
# beside the recorded ones. `rm` never traverses a symlink, so this removes the
# link (or the copy MSYS left in its place), never the fork's own tree — and the
# guard covers the one root that would alias it, $FORK/bin itself.
#
# Both sides must be CANONICAL, because what the guard protects is a directory,
# not a string: ROOT comes straight off $1 / $DN2CPP_GODOT_FORK_ROOT and so
# arrives in whatever spelling the caller typed (a tab-completed
# "<fork>/bin/", a relative "../godot-dn2cpp/bin"), while FORK_ABS is already
# `cd && pwd`. A raw comparison calls those unequal and the rm -rf below then
# deletes the fork's OWN bin/GodotSharp — the glue assemblies, Api/Release,
# GodotTools.dll and the Dn2Cpp toolchain steps 4/5 just staged — silently,
# leaving every fork gate to fail godot_fork_preflight and skip. The existence
# of this guard says ROOT == $FORK/bin is a supported invocation, so every
# spelling of it has to be caught. ROOT exists by now (mkdir -p, step 3).
ROOT_ABS="$(_fork_abs "$ROOT")"
if [ "$ROOT_ABS" != "$FORK_ABS/bin" ]; then
    rm -f "$ROOT/editor_bin$FORK_EXE" "$ROOT/template_bin$FORK_EXE"
    rm -rf "$ROOT/GodotSharp"
fi
printf '%s\n' "$BASE_COMMIT" > "$ROOT/pin.txt"
printf '%s\n' "$FORK_HEAD" > "$ROOT/fork_head.txt"

# Two-step rather than `| head -1`: head quits, ls dies of SIGPIPE and
# `pipefail` makes that the status. This file is standalone by design
# (see the header), so it does not get _common.sh's first_line.
sdk_nupkgs="$(ls "$ROOT"/nuget/Godot.NET.Sdk.*.nupkg 2>/dev/null || true)"
sdk_nupkg="${sdk_nupkgs%%$'\n'*}"
cache_size="$(du -sh "$ROOT/bin-cache" 2>/dev/null | cut -f1 || true)"
echo
echo "root:          $ROOT"
echo "editor:        $FORK_ABS/$EDITOR_REL (recorded in $ROOT/editor.txt)"
echo "template:      $FORK_ABS/$TEMPLATE_REL (recorded in $ROOT/template.txt)"
echo "desktop tmpl:  $TEMPLATE_ARTIFACT"
echo "GodotSharp:    $FORK_ABS/bin/GodotSharp (beside the editor)"
echo "fork clone:    $ROOT/clone.txt ($FORK_ABS)"
echo "toolchain:     $FORK/$TOOLCHAIN_MARKER"
echo "nuget feed:    $ROOT/nuget (${sdk_nupkg:-Godot.NET.Sdk nupkg MISSING})"
echo "base pin:      $ROOT/pin.txt ($BASE_COMMIT)"
echo "fork head:     $ROOT/fork_head.txt ($FORK_HEAD)"
echo "engine hash:   $ENGINE_HASH"
echo "tools hash:    $TOOLS_HASH (stamped in $TOOLS_STAMP)"
echo "binary cache:  $ROOT/bin-cache (${cache_size:-empty}, one entry per engine tree)"
echo
echo "export DN2CPP_GODOT_FORK_ROOT=\"$ROOT\""
echo "then: ./gates/build-and-run-godot-editor-export.sh"
