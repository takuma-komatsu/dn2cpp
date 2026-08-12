#!/usr/bin/env bash
# Shared helpers for everything that consumes the *forked* Godot editor cache
# built by gates/setup-godot-fork.sh: the four editor-export gates
# (build-and-run-godot-editor-export{,-ios,-android,-web}.sh, sourced after
# _common.sh) and the setup aids that extend the cache
# (setup-godot-fork-{ios,web}.sh, which source only this file). One place owns
# the artifact-root default, the recorded paths into the fork (its two engine
# binaries, its GodotSharp tree, the worktree itself), the pin + interop-ABI
# tripwire, and the shared cache-context terms.
#
# Sourcing is side-effect-free beyond variable assignment, so the setup aids can
# source it without _common.sh; the functions that need _common.sh helpers
# (gate_skip, file_sig, file_sig_deref, file_hash, file_text, src_tree_hash) are
# only called by gates, which source _common.sh first. The repo-root-relative
# paths (gates/expected/..., artifacts/...) rely on _common.sh's cd-to-repo-root;
# the setup aids only call godot_fork_resolve, which reads nothing but $FORK_ROOT.
#
# CACHE-KEY CONSTRAINT: no function here may affect gate behavior past
# gate_cache_check. This file's own CONTENT is in every key (_gate_helpers_hash
# in gates/_common.sh folds the whole gates/_*.sh set in), but what these
# functions INSPECT is not: the ARTIFACTS under $FORK_ROOT (engine sources,
# export template zips, their .provenance stamps). A gate's `tmpl=` term
# fingerprints the stale zip ITSELF, so it holds still precisely while the
# artifact is wrong, and the engine sources a stamp is compared against are in no
# key at all — a template can go stale with every key unmoved and a warm green
# replayed over it. So everything here runs live BEFORE the check: the preflight
# and the pin/ABI tripwire assert on the spot, and godot_fork_ctx's output lands
# verbatim in the CONTEXT string.

FORK_ROOT="${DN2CPP_GODOT_FORK_ROOT:-$HOME/.cache/dn2cpp-godot-fork}"
FORK_PIN_EXPECTED=gates/expected/godot-fork-pin.txt
ABI_EXPECTED=gates/expected/godot-dotnet-abi.sha256

# The executable suffix. Derived here rather than taken from _common.sh's EXE_EXT
# because this file is also sourced WITHOUT it (the setup aids — see the header),
# but every source that IS available is preferred over uname in turn, so the three
# OS-keyed things in this file can never disagree: EXE_EXT first (a gate sourced
# _common.sh, which derived it from DN2CPP_OS), then DN2CPP_OS itself (a setup aid
# that detected or was handed one — the same variable godot_fork_native_path and
# godot_fork_desktop_template read), and only then the host's own uname.
if [ -n "${EXE_EXT+set}" ]; then
    FORK_EXE="$EXE_EXT"
elif [ -n "${DN2CPP_OS:-}" ]; then
    case "$DN2CPP_OS" in windows) FORK_EXE=.exe ;; *) FORK_EXE= ;; esac
else
    case "$(uname -s)" in CYGWIN*|MINGW*|MSYS*) FORK_EXE=.exe ;; *) FORK_EXE= ;; esac
fi

# The fork's two engine binaries and the GodotSharp tree beside them, named ONCE
# here as RECORDED PATHS into the fork — gates/setup-godot-fork.sh writes
# editor.txt / template.txt / clone.txt, and every reader takes the path there.
#
# Recorded rather than linked because the lane must not depend on a real symlink:
# MSYS/Git-Bash's `ln -s` silently COPIES unless the shell has both a winsymlinks
# mode and the privilege to create one. Both halves of that copy hurt — a copied
# editor name leaves `readlink` nothing to read, so the worktree derivation below
# produces "."; a copied GodotSharp is worse for being quiet, because it works:
# the editor loads a snapshot of the very tree build_assemblies.py rewrites, and
# the cache key's `tools=` then describes the snapshot.
#
# The old artifact-root names remain the fallback, for a root assembled before
# these files existed. A name a script materializes for a binary must carry the
# suffix: an extension-less PE image is not launchable on Windows, and `test -x`
# under MSYS says it is, so a guard on the bare name proceeds and dies at exec.
_fork_recorded() {   # _fork_recorded FILE FALLBACK
    if [ -s "$FORK_ROOT/$1" ]; then
        head -1 "$FORK_ROOT/$1"
    else
        printf '%s\n' "$2"
    fi
}
FORK_EDITOR="$(_fork_recorded editor.txt "$FORK_ROOT/editor_bin$FORK_EXE")"
FORK_TEMPLATE="$(_fork_recorded template.txt "$FORK_ROOT/template_bin$FORK_EXE")"
# The engine's own rule — "GodotSharp/ sits beside the executable"
# (godotsharp_dirs.cpp resolves it next to the running image) — so one expression
# covers both layouts: the fork's own bin/GodotSharp for a recorded editor, the
# artifact root's for a legacy one. This is the live tree the editor loads
# GodotTools and the staged Dn2Cpp toolchain from.
FORK_GODOTSHARP="$(dirname "$FORK_EDITOR")/GodotSharp"

# The bundle needs a self-hosted native CLI. Building one takes many minutes, so
# a gate never triggers it: gates/selfhost-emit.sh produces it, and
# gates/setup-godot-fork.sh runs that on its way to the fork cache.
SELFHOST_BIN="artifacts/selfhost-fullcli/dn2cpp$FORK_EXE"

# godot_fork_host_arch [MACHINE] — echo the host architecture in Godot's
# spelling. Linux reports aarch64, while SCons, export presets and the engine's
# data-directory names use arm64.
godot_fork_host_arch() {
    local machine="${1:-$(uname -m)}"
    case "$machine" in
        aarch64|arm64) printf 'arm64\n' ;;
        amd64|x86_64)  printf 'x86_64\n' ;;
        *)             printf '%s\n' "$machine" ;;
    esac
}

# godot_fork_scons PYTHON — echo a working SCons launcher, probing each candidate
# by RUNNING it. Shared by the setup aids that build engine targets on a host this
# lane is ported to (setup-godot-fork.sh, setup-godot-fork-web.sh); the iOS aid
# still spells `scons` because it is macOS-only whatever happens here. No gate
# calls this, so it stays clear of this file's cache-key constraint.
#
# `command -v scons` is not the test, because on Windows the answer is routinely
# "no" while SCons is installed and perfectly usable: pip puts console scripts in
# a per-user Scripts directory that is not on PATH by default, and SCons is
# importable as a module regardless. PYTHON is the interpreter to try that with
# (the caller's resolve_python result — never a bare `python3`, which on a stock
# Windows box is an app-execution-alias stub that resolves AND launches).
godot_fork_scons() {
    local py="${1:-python}" cand
    for cand in scons "$py -m SCons"; do
        # shellcheck disable=SC2086
        if $cand --version >/dev/null 2>&1; then
            printf '%s\n' "$cand"
            return 0
        fi
    done
    echo "error: no working SCons found (tried: scons, $py -m SCons)" >&2
    echo "       Install it with '$py -m pip install --user scons'. 4.10.1+ is the" >&2
    echo "       floor for MSVC 14.5 — platform/windows/detect.py exits 255 below it." >&2
    return 1
}

# godot_fork_native_path PATH — echo PATH in the form the FORKED EDITOR resolves
# it. The editor is a native app, not an MSYS program, so a path this shell hands
# it (a preset's custom_template/release, a local feed in nuget.config) must be a
# native path: on Windows an MSYS /c/Users/... reaches FileAccess::exists as a
# nonexistent path, and the failure then names a missing file rather than a path
# format. cygpath -m gives the C:/Users/... form, which every consumer here
# accepts and which needs no sed escaping; a pass-through everywhere else.
godot_fork_native_path() {
    if [ "${DN2CPP_OS:-}" = windows ]; then
        cygpath -m "$1"
    else
        printf '%s\n' "$1"
    fi
}

# godot_fork_desktop_template ROOT — echo the DESKTOP export template artifact
# gates/setup-godot-fork.sh assembles into ROOT for this host. The platforms hand
# the exporter different things and the difference is the engine's, not a
# preference: the macOS exporter reads a zip laid out like misc/dist/
# macos_template.app and picks the binary out of it, while the Windows and Linux
# ones take the release template executable itself. Defined here so the setup
# script that WRITES the artifact and the gate that READS it cannot drift — the
# one name they must agree on has one home.
#
# The Linux name carries the architecture because the Linux exporter reads the
# custom template's ELF machine and refuses a preset whose
# binary_format/architecture disagrees — a mismatch there dies before the export
# plugin runs at all.
godot_fork_desktop_template() {
    case "${DN2CPP_OS:-}" in
        macos)   printf '%s/macos_template.zip\n' "$1" ;;
        windows) printf '%s/windows_template.exe\n' "$1" ;;
        linux)   printf '%s/linux_template.%s\n' "$1" "$(godot_fork_host_arch)" ;;
        *)
            echo "error: no desktop export template rule for ${DN2CPP_OS:-unknown}" >&2
            return 1
            ;;
    esac
}

# ── Engine provenance ─────────────────────────────────────────────────────────
# Everything the fork is allowed to touch. Anything else differing from the base
# means the prebuilt engine binaries no longer describe this tree. Defined here
# rather than in its main user gates/setup-godot-fork.sh: five scripts need one
# definition of "the engine sources", and a second copy of it is what drifts.
FORK_OWNED=(
    '.'
    ':(exclude)modules/mono/editor/GodotTools'
    ':(exclude)modules/mono/build_scripts'
    ':(exclude)*.md'
)

# godot_fork_base_commit — the pinned upstream base, read from the checked-in
# pin file by a path that does not depend on the caller's cwd. FORK_PIN_EXPECTED
# above is repo-root-relative and stays that way (it is gate cache-key material
# through godot_fork_ctx, and a gate has always been cd'd to the repo root); the
# setup aids are not cd'd anywhere, so they need this.
godot_fork_base_commit() {
    local here
    here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    head -1 "$here/expected/godot-fork-pin.txt"
}

# godot_fork_engine_hash — a fingerprint of the engine sources this fork would
# compile: the base commit, the delta from it (--binary, so a modified binary
# file is fingerprinted by its content rather than by "Binary files differ"),
# and a hash of every untracked engine file. FORK_OWNED is the definition of
# "engine", so the GodotTools / build_scripts / docs edit-test loop the fork
# exists for cannot move this hash.
#
# It fingerprints *content*, never git state: staging an engine edit, committing
# it, or reaching the same sources from another branch all leave it alone,
# because none of them changes a byte the compiler reads — which is what makes a
# revert a cache hit instead of a rebuild. Everything under bin/ is gitignored,
# so the binaries and their stamps cannot feed the hash that keys them.
#
# Reads $FORK (godot_fork_resolve) and $BASE_COMMIT. Measured at 0.2 s on the
# real fork, which is what lets a gate run it live before its cache check.
godot_fork_engine_hash() {
    (
        cd "$FORK"
        printf 'base %s\n' "$BASE_COMMIT"
        git diff --no-ext-diff --no-textconv --no-color --no-renames --binary \
            "$BASE_COMMIT" -- "${FORK_OWNED[@]}"
        # Not `xargs`: with no untracked file GNU xargs still runs shasum, which
        # then hashes its own stdin — the same tree would fingerprint differently
        # on a GNU host than on a BSD one, and provenance must name an engine
        # tree and not the host that measured it. `-r` is no fix; BSD lacks it.
        git ls-files --others --exclude-standard -z -- "${FORK_OWNED[@]}" \
            | while IFS= read -r -d '' f; do shasum -a 256 "$f"; done
    ) | shasum -a 256 | cut -c1-16
}

# godot_fork_engine_provenance — the one line stamped beside every EXPORT
# TEMPLATE the fork setup aids bake (the two Web zips, the iOS zip, and the
# desktop macos_template.zip / windows_template.exe setup-godot-fork.sh step 5
# assembles), as `<template>.provenance`. Export templates are what the engine
# binaries' stamps are not: handed to a consumer that cannot check them. Godot
# validates a preset's `custom_template` with FileAccess::exists alone and
# bypasses the version-keyed template directory, so a template one patch release
# behind exports, runs and matches every expectation while shipping an engine
# that self-reports the wrong version.
#
# `base=` is redundant as a TEST — the base commit is the first line the hash is
# taken over — and is kept as the DIAGNOSTIC: the hash is opaque, and "stamped
# base=... wanted base=..." is the only place this failure is ever explained.
#
# The engine hash subsumes the official .tpz half of the iOS template: the tag
# naming the tpz derives from the fork editor's version, that editor is built
# from these sources, and the cached tpz is stored under that tag — so a stale
# tpz implies a differing engine hash. A separate `tpz=` term would assert
# nothing this does not.
godot_fork_engine_provenance() {
    printf 'engine=%s base=%s\n' "$(godot_fork_engine_hash)" "$BASE_COMMIT"
}

# godot_fork_template_check PATH WHAT REMEDY — refuse an export template that
# was not baked from the engine sources now in the fork. Reads $FORK and
# $BASE_COMMIT, so a gate calls it after godot_fork_pin_abi_check and BEFORE
# gate_cache_check. That ordering is the CACHE-KEY CONSTRAINT above: the `tmpl=`
# key term fingerprints the stale zip itself, so a check whose effect were only
# visible after the cache check would be served a cached green over it.
#
# A MISSING stamp is a refusal, not a warning. From the artifact alone an
# unstamped and a correctly stamped template are indistinguishable, so unstamped
# means provenance unknown, and warning-then-proceeding re-opens the hole in the
# shape that keeps it open: green run, warning scrolled past inside a parallel
# suite's per-gate log, stale template shipped. The lane's other two stamps
# (godot_fork_preflight's selfhost stamp, setup-godot-fork-web.sh's emcc stamp)
# answer the same way; a third that did not would make the scheme unstatable.
# The cost is bounded and one-time: a re-bake, which the message names.
#
# FAIL rather than gate_skip, for godot_fork_preflight's reason: a skip says
# "this box cannot carry the input", and this box carries one that is WRONG.
godot_fork_template_check() {
    local path="$1" what="$2" remedy="$3" stamp="$1.provenance" stamped want
    want="$(godot_fork_engine_provenance)"
    stamped="$(head -1 "$stamp" 2>/dev/null || true)"
    [ -n "$stamped" ] || stamped='<no stamp>'
    if [ "$stamped" != "$want" ]; then
        echo "FAIL: the $what was not baked from the engine sources in the fork" >&2
        echo "      $path" >&2
        echo "      stamped $stamped" >&2
        echo "      wanted  $want" >&2
        echo "      Re-bake it: $remedy" >&2
        exit 1
    fi
    echo "$what: baked from these engine sources ($want)"
}

# godot_fork_template_check_soft PATH WHAT REMEDY — the DEGRADING twin of
# godot_fork_template_check, for a consumer that holds no $FORK/$BASE_COMMIT and
# that tolerates the template's ABSENCE by design. There is exactly one:
# gates/build-and-run-cri-wasm-smoke.sh, which is no editor-export gate, runs no
# godot_fork_preflight, and answers a missing Web template with gate_partial
# because a box that never baked one is supported. The hard check would fail that
# supported state; checking nothing leaves its reference templates unverified.
#
# The rule is: check what is PRESENT, degrade only on what is genuinely
# unknowable, and never soften the verdict on a present-but-wrong artifact.
#
#   * template absent            — return quietly. The caller's own arm reports
#                                  it, and names which half went unasserted.
#   * present, fork unresolvable — a loud warning naming the pin it wanted and
#                                  the stamp it found. The only genuine degrade:
#                                  with no worktree there is no engine tree to
#                                  hash, so no verdict exists to reach.
#                                  godot_fork_resolve's message prints first —
#                                  it is the half that says WHY — and this one
#                                  then says the run continues, which its
#                                  "error:" prefix would otherwise deny.
#   * present, fork resolvable   — godot_fork_template_check's refusal,
#                                  unsoftened. Tolerating an ABSENT input says
#                                  nothing about a WRONG one.
#
# FORK and BASE_COMMIT are declared local, so the resolution cannot leak the four
# editor-export gates' globals into a gate that deliberately does not have them;
# the callees read both through bash's dynamic scope.
godot_fork_template_check_soft() {
    local path="$1" what="$2" remedy="$3"
    [ -f "$path" ] || return 0
    local FORK BASE_COMMIT stamped
    if ! godot_fork_resolve; then
        stamped="$(head -1 "$path.provenance" 2>/dev/null || true)"
        echo "WARNING: the $what's provenance could NOT be verified — no fork worktree" >&2
        echo "         resolved (the error above says why), so there is no engine tree to" >&2
        echo "         hash it against. Continuing with it UNCHECKED:" >&2
        echo "           $path" >&2
        echo "           stamped ${stamped:-<no stamp>}" >&2
        echo "           wanted  base=$(head -1 "$FORK_PIN_EXPECTED")" >&2
        echo "         Re-bake it against a resolvable fork: $remedy" >&2
        return 0
    fi
    BASE_COMMIT="$(cat "$FORK_PIN_EXPECTED")"
    godot_fork_template_check "$path" "$what" "$remedy"
}

# ── The Web export template's own content ─────────────────────────────────────
# Both functions read a template zip and answer about the bytes in it, so they
# belong beside the provenance stamp rather than in either caller: the script
# that BAKES a Web template and the script that SHIPS one must ask the same
# question, and a second spelling of it is what drifts. No gate calls either, so
# both stay clear of this file's cache-key constraint.

# godot_fork_web_template_flavor ZIP — "cri" when the zip's JS glue carries the
# CRI JS libraries AND its main wasm carries the CRI native archives, "stock"
# when neither, "none" when ZIP does not exist. The JS probe symbol is the
# WebAudio server pump entry (crinc_webaudio2.jslib) no non-CRI link could
# contain; the native probe is an exported engine-archive function. A glue-only
# zip (baked before the main-link archive channel existed) answers "cri-js": it
# matches neither flavor, so a caller rebuilds rather than publishing it — a
# stock consumer must not ship a CRI-glued zip under the stock name.
#
# The probes use grep -c, never -q, and that is load-bearing under pipefail: -q
# exits at the first match, unzip -p takes SIGPIPE mid-write, and the pipeline's
# status becomes unzip's 141 — a hit reported as a miss (observed: a correctly
# CRI-linked wasm read as flavorless). grep -c drains its whole input, so unzip
# always finishes.
godot_fork_web_template_flavor() {
    [ -f "$1" ] || { echo none; return; }
    if unzip -p "$1" godot.js 2>/dev/null | grep -ac WAASRJS_SetMainFunc >/dev/null; then
        if unzip -p "$1" godot.wasm 2>/dev/null | grep -ac criAtom_GetVersionString >/dev/null; then
            echo cri
        else
            echo cri-js
        fi
    else
        echo stock
    fi
}

# godot_fork_web_template_assert ZIP — refuse a Web template the mono drop-in
# cannot load. Two structural properties, neither of which announces itself at
# export time:
#
#   * godot.side.wasm present. The engine is itself a side module loaded by a
#     thin MAIN_MODULE; its presence in the zip is what distinguishes a dlink
#     build from a stock one, and without dlink the engine has no dlopen at all
#     (Emscripten's is a stub returning NULL).
#   * the MAIN module DEFINES the C++ exception tag. A side module can only
#     import it, so built without -fwasm-exceptions every drop-in fails to
#     instantiate with a WebAssembly.LinkError that names nothing a Godot user
#     would recognise.
#
# Exits 1 on either: the artifact is WRONG, not missing.
godot_fork_web_template_assert() {
    local zip="$1" tmp
    tmp="$(mktemp -d)"
    unzip -q "$zip" -d "$tmp"
    if [ ! -f "$tmp/godot.side.wasm" ]; then
        echo "error: $zip carries no godot.side.wasm -- this is not a dlink build" >&2
        rm -rf "$tmp"
        exit 1
    fi
    if ! grep -aq '__cpp_exception' "$tmp/godot.wasm"; then
        echo "error: $zip's main module does not export the C++ exception tag" >&2
        echo "       (rebuild with disable_exceptions=no cxxflags/linkflags=-fwasm-exceptions)" >&2
        rm -rf "$tmp"
        exit 1
    fi
    rm -rf "$tmp"
    echo "ok: godot.side.wasm present (dlink), main module exports __cpp_exception"
}

# godot_fork_web_template_emcc_assert BUNDLE_EMCC OUT_DIR VERSION — refuse an
# editor package whose toolchain and the release's Web template were linked by
# different emcc. The template is the wasm program's MAIN module and the drop-in
# that toolchain compiles is a side module of it, so a version split links clean
# on both sides and dies in the browser.
#
# The witness is the release asset's own metadata when this cut has one, else the
# stamp beside the fork root's published template — the zip that asset is cut
# from. Neither present is a refusal: an unwitnessed match is not a match.
godot_fork_web_template_emcc_assert() {
    local bundle_emcc="$1" out_dir="$2" version="$3"
    local meta="$out_dir/web.metadata" tmpl_emcc="" origin=""
    if [ -f "$meta" ] \
        && [ "$(first_line "$(sed -n 's/^release_version=//p' "$meta")")" = "$version" ]; then
        tmpl_emcc="$(first_line "$(sed -n 's/^emcc=//p' "$meta")")"
        origin="$meta"
    elif [ -f "$FORK_ROOT/web_emcc.txt" ]; then
        tmpl_emcc="$(head -1 "$FORK_ROOT/web_emcc.txt")"
        origin="$FORK_ROOT/web_emcc.txt"
    fi
    if [ -z "$tmpl_emcc" ]; then
        echo "error: no Web template emcc stamp to check the bundled toolchain against" >&2
        echo "       Looked in $meta (release_version=$version) and $FORK_ROOT/web_emcc.txt." >&2
        echo "       Bake the template with the bundle's SDK and cut it, in that order:" >&2
        echo "         FORCE=1 gates/setup-godot-fork-web.sh" >&2
        echo "         dist/package-web-template.sh --version $version" >&2
        exit 1
    fi
    if [ "$tmpl_emcc" != "$bundle_emcc" ]; then
        echo "error: the Web template and the bundled toolchain were linked by different emcc" >&2
        echo "       template ($origin):" >&2
        echo "         $tmpl_emcc" >&2
        echo "       bundle (Dn2Cpp/manifest.json emcc_version):" >&2
        echo "         $bundle_emcc" >&2
        echo "       Re-bake the template with the bundle's SDK and re-cut it, in that order:" >&2
        echo "         FORCE=1 gates/setup-godot-fork-web.sh" >&2
        echo "         dist/package-web-template.sh --version $version" >&2
        exit 1
    fi
    echo "web template:  emcc matches the bundled toolchain ($origin)"
}

# godot_fork_preflight — the skip-or-proceed test every editor-export gate runs
# first: the fork cache must be complete (editor, pin, GodotSharp, Sdk feed) and
# the self-hosted native CLI must exist AND be built from the sources now in the
# tree. Per-gate prerequisites (a platform template, Xcode, an NDK) stay in the
# gate, each with its own gate_skip.
#
# The staleness test is a hard FAIL, and it lives HERE rather than in either
# other candidate home. dist/package-toolchain.sh honours an explicit
# --dn2cpp-bin as given, unchecked, by stated contract, and every gate reaches it
# through stage_editor_toolchain naming $SELFHOST_BIN explicitly — so its own
# stamp check protects hand-run packaging and never a gate. stage_editor_toolchain
# runs AFTER gate_cache_check in every consumer, where godot_fork_ctx's keys
# fingerprint the stale binary itself and a rerun reports "cached green" BECAUSE
# nothing changed. The preflight is upstream of the cache check in all six
# consumers, which is what lets a stale binary fail even on a warm cache, and is
# what the CACHE-KEY CONSTRAINT in this file's header demands of any assert here.
#
# FAIL rather than gate_skip: a skip is for what the box cannot carry, which is
# certainly the wrong reading for a binary the tree builds itself.
godot_fork_preflight() {
    if [ ! -x "$FORK_EDITOR" ] || [ ! -f "$FORK_ROOT/pin.txt" ] \
        || [ ! -d "$FORK_GODOTSHARP" ] \
        || ! compgen -G "$FORK_ROOT/nuget/Godot.NET.Sdk.*.nupkg" >/dev/null; then
        # Name the remedy only on the host where the remedy exists. This list
        # must track gates/setup-godot-fork.sh's per-OS arms exactly: anywhere
        # else "run gates/setup-godot-fork.sh" is an instruction that cannot
        # succeed and costs a long scons build to disprove, and it reads as
        # "this box is under-provisioned" when it means "this lane has no port
        # to your OS".
        case "${DN2CPP_OS:-}" in
            macos|windows|linux)
                gate_skip "fork cache absent/incomplete at $FORK_ROOT — run gates/setup-godot-fork.sh"
                ;;
            *)
                gate_skip "the Godot editor-export fork lane has no ${DN2CPP_OS:-non-desktop} arm — gates/setup-godot-fork.sh builds an engine only for macOS, Windows and Linux, so no fork cache can be produced at $FORK_ROOT on this host (running that script will NOT close this skip)"
                ;;
        esac
    fi
    # selfhost_bin_fresh (gates/_common.sh) is the one definition of "this binary
    # describes today's sources", shared with dist/package-toolchain.sh's
    # blind-reuse guard and with gates/pre-merge.sh's self-host phase; its
    # SELFHOST_BIN_PATH is $SELFHOST_BIN, since EXE_EXT is where FORK_EXE came from.
    # The absent binary and the stale one are told apart here because they are
    # different answers: nothing to judge versus a judgement.
    if ! selfhost_bin_fresh; then
        if [ ! -x "$SELFHOST_BIN" ]; then
            gate_skip "no $SELFHOST_BIN — run gates/selfhost-emit.sh or gates/setup-godot-fork.sh"
        fi
        echo "FAIL: $SELFHOST_BIN predates the current sources" >&2
        echo "      stamped $SELFHOST_SRC_STAMPED != $SELFHOST_SRC_NOW (src_tree_hash of src/ runtime/ third_party/)" >&2
        echo "      Rebuild the self-hosted CLI: gates/selfhost-emit.sh" >&2
        exit 1
    fi
}

# _fork_abs PATH — PATH in the canonical form gates/setup-godot-fork.sh records
# (plain `cd && pwd`, the same normalization that produced clone.txt), so that
# two spellings of one directory — a tab-completed trailing slash, a relative
# path — compare equal. A path that does not exist passes through unchanged:
# the caller's own "worktree not found at ..." is the better diagnostic.
_fork_abs() {
    ( cd "$1" 2>/dev/null && pwd ) || printf '%s\n' "$1"
}

# godot_fork_resolve — resolve the fork worktree into FORK: the explicit
# override first, then what gates/setup-godot-fork.sh recorded, then the legacy
# inference from an editor_bin symlink (kept for a root assembled before
# clone.txt existed). Returns 1 with a message when nothing resolves, when the
# override and the recorded clone disagree, or when the worktree is missing —
# the setup aids exit on that; the gates reach this only through
# godot_fork_pin_abi_check, after godot_fork_preflight has established that the
# cache exists.
#
# INVARIANT: the recorded ROOT and the resolved WORKTREE must describe ONE clone.
# The root supplies the binary that actually runs (editor.txt / template.txt and
# the GodotSharp tree staged beside it); the worktree is what
# godot_fork_pin_abi_check fingerprints. Let the two name different clones and
# the tripwire's whole guarantee — "the prebuilt binary describes this tree" —
# detaches with nothing said: clone B is certified while clone A's editor runs.
# So the override is honored only where it cannot lie (a legacy root with no
# clone.txt), and a disagreement is a hard error, not a silent preference.
#
# The legacy arm is last for a reason beyond ordering: `readlink` answers nothing
# at all when the name is a copy rather than a link (stock MSYS/Git-Bash `ln -s`),
# and `dirname "$(dirname "")"` is ".", so that arm would hand the caller a FORK
# that merely fails the sanity test below — a hard FAIL naming the wrong thing.
godot_fork_resolve() {
    local recorded=""
    # `if`/`fi`, not `[ -s x ] && recorded=...`: under `set -euo pipefail` an
    # AND-list whose test fails is the function's own exit status, and a
    # missing clone.txt would then abort the caller instead of falling through
    # to the legacy arm.
    if [ -s "$FORK_ROOT/clone.txt" ]; then
        recorded="$(head -1 "$FORK_ROOT/clone.txt")"
    fi
    FORK="${DN2CPP_GODOT_FORK_CLONE:-}"
    if [ -n "$FORK" ] && [ -n "$recorded" ] \
        && [ "$(_fork_abs "$FORK")" != "$(_fork_abs "$recorded")" ]; then
        echo "error: \$DN2CPP_GODOT_FORK_CLONE and $FORK_ROOT/clone.txt name different forks:" >&2
        echo "         env:       $FORK" >&2
        echo "         clone.txt: $recorded" >&2
        echo "       The root supplies the editor that runs; the worktree is what the pin/ABI" >&2
        echo "       check fingerprints — they must be one clone. Assemble a root from the" >&2
        echo "       clone you want: DN2CPP_GODOT_FORK_CLONE=$FORK gates/setup-godot-fork.sh <root>" >&2
        echo "       (or drop the override and use the clone this root was built from)." >&2
        return 1
    fi
    if [ -z "$FORK" ]; then
        FORK="$recorded"
    fi
    if [ -z "$FORK" ]; then
        local editor_target
        editor_target="$(readlink "$FORK_EDITOR" 2>/dev/null || true)"
        case "$editor_target" in
            /*) FORK="$(dirname "$(dirname "$editor_target")")" ;;
        esac
    fi
    if [ -z "$FORK" ]; then
        echo "error: cannot resolve the fork worktree — no \$DN2CPP_GODOT_FORK_CLONE, no" >&2
        echo "       $FORK_ROOT/clone.txt, and $FORK_EDITOR is no symlink either." >&2
        echo "       Run gates/setup-godot-fork.sh (it records clone.txt)." >&2
        return 1
    fi
    if [ ! -e "$FORK/modules/mono/mono_gd/gd_mono.cpp" ]; then
        echo "error: fork worktree not found at $FORK (from $FORK_ROOT) — run gates/setup-godot-fork.sh" >&2
        return 1
    fi
}

# godot_fork_emcc_python_check SEARCH_PATH [EMSDK_PYTHON] — skip when the python
# emcc would start is older than the floor the fork's exporter refuses below, so
# a host that cannot export says so here instead of dying inside the link.
# Presence is not the question: macOS answers `python3` with an Xcode stub stuck
# at 3.9.6 that resolves, runs, and is refused.
#
# Both arguments describe the EXPORT's answer, not the gate's, and they are the
# whole difference between callers — the PATH the editor is launched with, and
# the EMSDK_PYTHON it will see (empty where it is launched under `env -u`). The
# gate's own interpreter is a separate question that resolve_python answers, for
# reading wasm exports and serving over http.server; one fit for those is not
# thereby fit for emcc, which is why neither call may stand in for the other.
godot_fork_emcc_python_check() {
    local search_path="$1" exe="${2-}" origin=EMSDK_PYTHON src floor ver
    godot_fork_resolve || exit 1
    src="$FORK/modules/mono/editor/GodotTools/GodotTools/Export/Dn2CppExporter.cs"
    # Read off the fork rather than restated: a floor that drifts from the one
    # the editor enforces skips runs the export would take, or admits ones it
    # refuses.
    floor="$(LC_ALL=C awk '
        /const int RequiredPythonMajor/ && match($0, /[0-9]+/) { maj = substr($0, RSTART, RLENGTH) }
        /const int RequiredPythonMinor/ && match($0, /[0-9]+/) { min = substr($0, RSTART, RLENGTH) }
        END { if (maj != "" && min != "") print maj "." min }' "$src")"
    if [ -z "$floor" ]; then
        echo "FAIL: no RequiredPythonMajor/Minor constants in $src," >&2
        echo "      which is this probe's only source for the version the export demands." >&2
        exit 1
    fi
    if [ -n "$exe" ]; then
        # Native form, because Windows is the only host whose SDK stages one.
        if [ "${DN2CPP_OS:-}" = windows ]; then
            exe="$(cygpath -u "$exe" 2>/dev/null || printf '%s' "$exe")"
        fi
    else
        origin=PATH
        # The launchers' own names in their own order — the sh script tries
        # python3 then python, pylauncher.exe python alone. Probing a name emcc
        # does not look for is a check that passes while the link fails.
        if [ "${DN2CPP_OS:-}" = windows ]; then
            exe="$(PATH="$search_path" command -v python 2>/dev/null || true)"
        else
            exe="$(PATH="$search_path" command -v python3 2>/dev/null \
                || PATH="$search_path" command -v python 2>/dev/null || true)"
        fi
        [ -n "$exe" ] || gate_skip "no python on PATH for emcc, a launcher over python $floor or newer; the Emscripten SDK stages an interpreter on Windows alone, and no repository can ship the host one"
    fi
    ver="$("$exe" -E -c 'import sys; print("%d.%d.%d" % sys.version_info[:3])' 2>/dev/null || true)"
    case "$ver" in [0-9]*.[0-9]*) ;; *) ver="" ;; esac
    if [ -z "$ver" ] \
        || [ "$(printf '%s\n%s\n' "$floor" "$ver" | LC_ALL=C sort -V | tail -1)" != "$ver" ]; then
        gate_skip "the python emcc would run, $exe ($origin), is ${ver:-of no readable version} and the export needs $floor or newer; the Emscripten SDK stages an interpreter on Windows alone, and no repository can ship the host one"
    fi
    echo "emcc python: $exe ($ver, $origin) against the fork's floor of $floor"
}

# godot_fork_pin_abi_check — the shared pin + interop-ABI tripwire of the four
# editor-export gates. The artifact root, the fork worktree and the gate's
# checked-in expectations must all describe one base commit, and the fork must
# leave the two surfaces the mono-module handshake hard-codes assumptions
# about byte-identical to that base — the engine's interop function table
# (order/count of unmanaged_callbacks) and the ManagedCallbacks struct — or it
# is not a drop-in host. Sets FORK and BASE_COMMIT for the caller; exits 1 on
# any mismatch (this runs inside a gate, where the mismatch IS the failure).
godot_fork_pin_abi_check() {
    godot_fork_resolve || exit 1

    BASE_COMMIT="$(cat "$FORK_PIN_EXPECTED")"
    if [ "$(cat "$FORK_ROOT/pin.txt")" != "$BASE_COMMIT" ]; then
        echo "FAIL: $FORK_ROOT/pin.txt ($(cat "$FORK_ROOT/pin.txt")) != $FORK_PIN_EXPECTED ($BASE_COMMIT)" >&2
        echo "      Rebuild the fork cache (gates/setup-godot-fork.sh)." >&2
        exit 1
    fi
    if ! git -C "$FORK" merge-base --is-ancestor "$BASE_COMMIT" HEAD; then
        echo "FAIL: the fork at $FORK no longer descends from the pinned base $BASE_COMMIT" >&2
        exit 1
    fi
    local abi_actual
    abi_actual="$(
        awk '/^static const void \*unmanaged_callbacks\[\]\{/{f=1} f{print} f&&/^\};/{exit}' \
            "$FORK/modules/mono/glue/runtime_interop.cpp" \
            | shasum -a 256 | awk '{print $1"  unmanaged_callbacks"}'
        shasum -a 256 "$FORK/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ManagedCallbacks.cs" \
            | awk '{print $1"  ManagedCallbacks.cs"}'
    )"
    if [ "$abi_actual" != "$(cat "$ABI_EXPECTED")" ]; then
        echo "FAIL: the fork's interop ABI fingerprints differ from $ABI_EXPECTED" >&2
        echo "expected:" >&2; cat "$ABI_EXPECTED" >&2
        echo "actual:" >&2; printf '%s\n' "$abi_actual" >&2
        exit 1
    fi
    echo "fork: $FORK @ $(git -C "$FORK" rev-parse --short HEAD) (base $BASE_COMMIT); ABI fingerprints OK"
}

# godot_fork_ctx — echo the cache-context terms shared by the four editor-export
# gates' inputs-only keys (no local transpile happens in them: the forked editor
# drives the whole pipeline from the packaged toolchain) — the checked-in pin,
# the artifact root's pin, the editor identity and the GodotTools.dll hash.
#
# `editor=` is a size+mtime SIGNATURE, never the readlink path string: a path
# does not move when the engine is REBUILT at it, and answers nothing at all when
# the name is a copy instead of a link. The dereference belongs to file_sig_deref,
# which resolves a relative target against the LINK's directory rather than the
# caller's cwd. `tools=` is hashed by CONTENT, not size+mtime: Dn2CppExporter.cs
# — the code deciding which arguments the transpiler is invoked with — compiles
# into GodotTools.dll, NOTHING else in these keys covers it, and a rebuild has
# produced a byte-different DLL of identical size. The caller prepends its gate
# name and appends its platform terms (template signature, NDK, Xcode/emcc).
#
# Every term goes through a helper that can answer neither the empty string nor
# "no such file" for a file it merely failed to RESOLVE, and that is load-bearing:
# `editor=` and `tools=` are the only terms a fork rebuild moves, so a key whose
# live terms have quietly gone constant still hits, and the four gates then replay
# a green over an engine they never ran. The rule and both mechanisms that
# produced it are written out above file_hash in gates/_common.sh.
godot_fork_ctx() {
    printf 'forkpin=%s|rootpin=%s|editor=%s|tools=%s' \
        "$(file_text "$FORK_PIN_EXPECTED")" \
        "$(file_text "$FORK_ROOT/pin.txt")" \
        "$(file_sig_deref "$FORK_EDITOR")" \
        "$(file_hash "$FORK_GODOTSHARP/Tools/GodotTools.dll")"
}

# godot_fork_assert_bundled_buildtools ENGINE_LOG EXPORTER_LOG DN2CPP_ROOT WORKDIR
# — that the export configured through the BUNDLE's cmake and ninja and not the
# host's. The resolver prefers the bundled pair over PATH, so on any machine that
# develops this lane a regression back to the host's pair is invisible: the host's
# cmake builds the same game.
#
# Two logs, because the halves are written by different processes. The engine's
# carries one `dn2cpp: <tool> <path> (<origin>)` line per tool, and the origin is
# the whole assertion — `bundled` is a different fact from `PATH`. Only the
# exporter's carries the configure command line, which is where ninja reaches
# cmake at all: the Ninja generator otherwise looks for one on PATH, which is
# exactly where a bundled ninja is not.
#
# The editor is a native app, so on Windows it logs native BACKSLASH paths; both
# sides fold to forward slashes first, a no-op on POSIX.
godot_fork_assert_bundled_buildtools() {
    local engine_log="$1" exporter_log="$2" root="$3" work="$4"
    local tool want line fwd
    for tool in cmake ninja; do
        case "$tool" in
            cmake) want="$(godot_fork_native_path "$(bundled_cmake "$root")" | tr '\\' /)" ;;
            ninja) want="$(godot_fork_native_path "$(bundled_ninja "$root")" | tr '\\' /)" ;;
        esac
        line="$(first_line "$(grep -F "dn2cpp: $tool " "$engine_log" || true)")"
        [ -n "$line" ] || {
            echo "FAIL: the export log names no $tool at all ($engine_log)" >&2
            return 1; }
        if [ "${line//\\//}" != "dn2cpp: $tool $want (bundled)" ]; then
            echo "FAIL: the export did not build through the bundle's own $tool" >&2
            echo "      line:   $line" >&2
            echo "      wanted: dn2cpp: $tool $want (bundled)" >&2
            return 1
        fi
        echo "$line"
    done

    fwd="$work/exporter-log-fwd.txt"
    tr '\\' / < "$exporter_log" > "$fwd"
    want="$(godot_fork_native_path "$(bundled_ninja "$root")" | tr '\\' /)"
    grep -qF -- "-DCMAKE_MAKE_PROGRAM:FILEPATH=$want" "$fwd" || {
        echo "FAIL: the configure did not hand cmake the bundled ninja ($exporter_log)" >&2
        echo "      wanted: -DCMAKE_MAKE_PROGRAM:FILEPATH=$want" >&2
        grep -n 'CMAKE_MAKE_PROGRAM' "$fwd" >&2 \
            || echo "      the configure spells CMAKE_MAKE_PROGRAM nowhere — cmake found ninja itself" >&2
        return 1; }
    echo "the configure passed CMAKE_MAKE_PROGRAM=$want as a typed FILEPATH"
}
