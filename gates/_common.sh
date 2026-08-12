#!/usr/bin/env bash
# Shared helpers for the dn2cpp build-and-run gates. A gate sources this right
# after its header; sourcing enables `set -euo pipefail`, cd's to the repo root
# (so all paths are repo-root relative) and defines CONFIG / TFM plus helpers.
# Override CONFIG (Release/Debug) or GODOT via the environment.
#
# Speed levers (set by run-all-gates.sh):
#   DN2CPP_CLI_DLL     — pre-built dn2cpp.dll; gates `dotnet exec` it, no MSBuild.
#   DN2CPP_CORELIB     — pre-resolved CoreLib path; locate_corelib returns it.
#   DN2CPP_SKIP_BUILD  — build_proj() is a no-op (orchestrator pre-built all).

set -euo pipefail

# ── Never pipe into grep -q, head -N, or any consumer that can exit early ─────
# Under `pipefail` the producer's SIGPIPE 141 becomes the pipeline's status, so
# the assert inverts — silently, in both directions, at any input size. The
# fail-OPEN direction (a forbidden-text check that stops firing) is unreported.
# Sanctioned forms, none of which builds a pipeline:
#
#     grep -q  PATTERN <<<"$captured"
#     grep -Eq PATTERN "$dir"/generated*.cpp
#     first=${captured%%$'\n'*}
#     first_line "$(cmd --version)"
#
# Capture first when the text comes from a command. For "is the output
# non-empty", capture and test `[ -n "$x" ]`. Here-strings are safe at any size.
# Enforced, with no exemptions, by gates/build-and-run-doc-claims.sh.

# first_line STR — STR's first line, no pipeline. Exists for the sites where a
# two-step capture does not fit: inside a bigger interpolation.
# A native Windows tool writes CRLF and `$()` drops the CR off the LAST line
# only, so every earlier line still carries one; a no-op where there is none.
first_line() { local _l="${1%%$'\n'*}"; printf '%s' "${_l%$'\r'}"; }

# ── A text tool reading FOREIGN bytes is locale-sensitive: prefix `LC_ALL=C` ──
# Under a UTF-8 locale BSD `sed` (regex), `tr`, `sort` and `cut` abort on invalid
# bytes; awk/head/tail/uniq/wc pass them through. On a SUCCESS path that abort
# turns a green tree red and names the tree, not the locale. So a stage reading
# text this repository did not author (build logs, nm/lipo/unzip dumps, an
# engine's or binary's stdout, env, lsof, adb) takes an `LC_ALL=C` prefix, which
# binds ONE command. Repo-authored text stays unprefixed — UTF-8 by construction.

# Absolute path of the sourcing gate script, captured BEFORE the cd so a relative
# invocation resolves; hashed as a gate-cache key input.
DN2CPP_GATE_SCRIPT="$(cd "$(dirname "${BASH_SOURCE[1]}")" && pwd -P)/$(basename "${BASH_SOURCE[1]}")"
# Physical paths (`pwd -P` / `cd -P`) throughout: a stale logical $PWD would
# propagate a since-moved repo path into every build directory.
cd -P "$(dirname "${BASH_SOURCE[1]}")/.."

CONFIG=${CONFIG:-Release}
TFM=${TFM:-net10.0}

# ── Platform detection (cross-platform build-script seam) ─────────────────────
# detect_os — host OS family. Gates derive shared-library naming, the loader
# search-path env var and link flags from DN2CPP_OS instead of hard-coding macOS.
detect_os() {
    case "$(uname -s)" in
        Darwin)               printf 'macos\n' ;;
        Linux)                printf 'linux\n' ;;
        CYGWIN*|MINGW*|MSYS*) printf 'windows\n' ;;
        *)                    printf 'unknown\n' ;;
    esac
}
DN2CPP_OS=${DN2CPP_OS:-$(detect_os)}

# Windows's host toolchain is MSVC, so name it rather than making every caller
# do it: unset, cmake dies on its own "No CMAKE_CXX_COMPILER could be found"
# instead of reaching ensure_msvc_env's vcvarsall import. Anything else on
# Windows — clang++, clang-cl — is still spelled out here explicitly.
if [ "$DN2CPP_OS" = windows ]; then
    : "${CMAKE_CXX_COMPILER:=cl}"
    export CMAKE_CXX_COMPILER
fi

# LIB_EXT (extension, no dot) / LIB_PREFIX ("lib", empty on Windows) /
# LIB_PATH_ENV (loader's run-time shared-library search-path env var).
case "$DN2CPP_OS" in
    macos)   LIB_EXT=dylib; LIB_PREFIX=lib; LIB_PATH_ENV=DYLD_LIBRARY_PATH ;;
    linux)   LIB_EXT=so;    LIB_PREFIX=lib; LIB_PATH_ENV=LD_LIBRARY_PATH ;;
    windows) LIB_EXT=dll;   LIB_PREFIX=;    LIB_PATH_ENV=PATH ;;
    *)       LIB_EXT=so;    LIB_PREFIX=lib; LIB_PATH_ENV=LD_LIBRARY_PATH ;;
esac

# EXE_EXT — ".exe" on Windows, empty elsewhere. Only a script that NAMES a binary
# it did not compile needs it (the CMake wrappers hand back extension-less names).
# `test -x` says yes to an extension-less PE image under MSYS, so a guard that
# omits it does not skip — it proceeds and dies at exec.
case "$DN2CPP_OS" in
    windows) EXE_EXT=.exe ;;
    *)       EXE_EXT= ;;
esac

# lib_name BASE — on-disk file name of shared library BASE (libx.dylib / libx.so
# / x.dll).
lib_name() { printf '%s%s.%s\n' "$LIB_PREFIX" "$1" "$LIB_EXT"; }

# DN2CPP_GDEXT_LIB — base name of every GDExtension artifact this lane builds.
# Fixed rather than app-derived, matching the OUTPUT_NAME pin in
# runtime/CMakeLists.txt; a .gdextension declaring any other name is a dlopen
# failure at run time that names no cause.
DN2CPP_GDEXT_LIB=dn2cpp

# native_path PATH — PATH as a NATIVE (non-MSYS) program resolves it: `cygpath -m`
# on Windows, pass-through elsewhere. MSYS converts an argument only when the path
# is the WHOLE token (`-L/c/foo` converts, `-L/c/foo -lbar` does not), and never
# converts a path written into a FILE; both failures are silent. Route a path
# through here before embedding it in ANY multi-part argument. Sibling copies in
# _godot_fork.sh / _godot_dotnet.sh cannot delegate here (sourced without this file).
native_path() {
    if [ "$DN2CPP_OS" = windows ]; then
        cygpath -m "$1"
    else
        printf '%s\n' "$1"
    fi
}

# install_name_flags PATH — linker flags recording a shared library's runtime
# lookup identity, one token per line (read -a into an array). macOS: absolute
# -install_name. Linux: -soname plus an rpath to the dir. Otherwise: none.
install_name_flags() {
    case "$DN2CPP_OS" in
        macos) printf -- '-install_name\n%s\n' "$1" ;;
        linux) printf -- '-Wl,-soname,%s\n-Wl,-rpath,%s\n' "$(basename "$1")" "$(dirname "$1")" ;;
        *)     : ;;
    esac
}

# consumer_rpath_flags DIR — linker flag(s) making a *consumer* executable search
# DIR at run time, one token per line. Linux only: DT_SONAME on the .so is not
# enough, the consumer's own link must record DT_RUNPATH. macOS needs nothing (the
# .so's absolute -install_name lands in the consumer's LC_LOAD_DYLIB); Windows
# uses PATH at run time.
consumer_rpath_flags() {
    case "$DN2CPP_OS" in
        linux) printf -- '-Wl,-rpath,%s\n' "$1" ;;
        *)     : ;;
    esac
}

# is_msvc_compiler — true when CMAKE_CXX_COMPILER names cl.exe. The single MSVC
# opt-in test (ensure_msvc_env delegates here; so does a gate shelling out to the
# compiler directly to pick cl.exe vs clang++ and MSVC-vs-GNU flag syntax).
is_msvc_compiler() {
    case "${CMAKE_CXX_COMPILER:-}" in
        cl|cl.exe|*/cl.exe|*\\cl.exe) return 0 ;;
        *) return 1 ;;
    esac
}

# libpath_flag DIR — one-token library-search-path flag in the active toolchain's
# syntax (`-L<dir>` GNU-like, `/LIBPATH:<dir>` MSVC). Forwarded through
# DN2CPP_APP_LINK_FLAGS -> cmake_build_app -> target_link_options, which only
# tokenizes; it does not translate a GNU flag into an MSVC one.
libpath_flag() {
    if is_msvc_compiler; then
        # This token rides through a CMake cache variable into build.ninja as a
        # literal link.exe argument; nothing normalizes an embedded opaque flag
        # string, so an MSYS "/d/..." path would reach link.exe verbatim.
        printf -- '/LIBPATH:%s\n' "$(cygpath -w "$1")"
    else
        # native_path for the same reason: equally opaque to MSYS's argument
        # rewriting once a caller concatenates it with another flag.
        printf -- '-L%s\n' "$(native_path "$1")"
    fi
}

# locate_corelib — System.Private.CoreLib.dll of the highest installed
# Microsoft.NETCore.App shared runtime (the ref pack has no IL bodies); fails if
# not found. Echoes DN2CPP_CORELIB when set, else resolves via
# `dotnet --list-runtimes` and exports the result.
locate_corelib() {
    if [ -n "${DN2CPP_CORELIB:-}" ]; then
        printf '%s\n' "$DN2CPP_CORELIB"
        return 0
    fi
    local name version path candidate corelib=""
    while read -r name version path; do
        [ "$name" = "Microsoft.NETCore.App" ] || continue
        # Windows dotnet emits CRLF; `read -r` keeps '\r' on the last field,
        # defeating the ']' strip below.
        path="${path%$'\r'}"
        candidate="${path%]}/$version/System.Private.CoreLib.dll"
        candidate="${candidate#[}"
        [ -f "$candidate" ] && corelib="$candidate"
    done < <(dotnet --list-runtimes | LC_ALL=C sed 's/\[/ /')
    if [ -z "$corelib" ]; then
        echo "error: could not locate System.Private.CoreLib.dll via 'dotnet --list-runtimes'" >&2
        return 1
    fi
    export DN2CPP_CORELIB="$corelib"
    printf '%s\n' "$corelib"
}

# locate_corelib_cross_posix [net10] — locate_corelib, but a CoreLib whose native
# interop surface matches a POSIX CROSS-TARGET (Android/NDK and Web/Emscripten
# both want it). `net10` also pins the version; the two axes are independent, and
# taking only the version one is how a Windows-flavour CoreLib reached an NDK
# link. POSIX host: the two coincide, so delegate. Windows host: the local CoreLib
# P/Invokes kernel32/ntdll (the NDK link dies on `-lkernel32`, naming no cause)
# and its Guid path pulls ole32 (nothing under Emscripten). A linux-x64 runtime
# pack's CoreLib carries the POSIX implementations instead.
locate_corelib_cross_posix() {
    if [ -n "${DN2CPP_CORELIB_CROSS:-}" ]; then
        printf '%s\n' "$DN2CPP_CORELIB_CROSS"
        return 0
    fi
    if [ "$DN2CPP_OS" != windows ]; then
        if [ "${1:-}" = net10 ]; then resolve_net10_corelib; else locate_corelib; fi
        return $?
    fi
    # The runtime pack's version directory carries the version, so the pin is a
    # glob on it — same "highest that matches" rule as resolve_net10_corelib.
    local best pat='*'
    [ "${1:-}" = net10 ] && pat='10.0.*'
    best="$(ls -d "$HOME"/.nuget/packages/microsoft.netcore.app.runtime.linux-x64/$pat/runtimes/linux-x64/lib/*/System.Private.CoreLib.dll 2>/dev/null \
        | sort -V | tail -1)"
    if [ -z "$best" ] || [ ! -f "$best" ]; then
        echo "error: no ${1:+$1 }linux-x64 CoreLib found for a POSIX cross-build on Windows." >&2
        echo "       The local (Windows) CoreLib's FileStream/Console internals P/Invoke" >&2
        echo "       kernel32.dll/ntdll.dll directly and its Guid path pulls ole32, none of" >&2
        echo "       which the NDK sysroot or Emscripten can link against. Fetch a linux-x64" >&2
        echo "       runtime pack once (populates it under" >&2
        echo "       ~/.nuget/packages/microsoft.netcore.app.runtime.linux-x64/):" >&2
        echo "         dotnet new console -o /tmp/linux-corelib-probe" >&2
        echo "         dotnet publish /tmp/linux-corelib-probe -r linux-x64 --self-contained true" >&2
        echo "       or set DN2CPP_CORELIB_CROSS to an explicit CoreLib path." >&2
        return 1
    fi
    printf '%s\n' "$best"
}

# resolve_net10_corelib — CoreLib of the highest installed Microsoft.NETCore.App
# 10.0.* shared runtime; fails if none. The JSON gates pin net10.0 rather than
# calling locate_corelib because a highest-version pick can land on an 11.0
# preview whose CoreLib shape trips ResolveMemberRefField ("Sequence contains no
# matching element") — version skew, not a real gap.
resolve_net10_corelib() {
    local name version path best_ver="" best_path=""
    while read -r name version path; do
        [ "$name" = "Microsoft.NETCore.App" ] || continue
        case "$version" in 10.0.*) ;; *) continue ;; esac
        # Windows dotnet emits CRLF; `read -r` keeps '\r' on the last field,
        # defeating the ']' strip below.
        path="${path%$'\r'}"
        path="${path#[}"; path="${path%]}"
        if [ -z "$best_ver" ] \
            || [ "$(printf '%s\n%s\n' "$best_ver" "$version" | sort -V | tail -1)" = "$version" ]; then
            best_ver="$version"
            best_path="$path/$version/System.Private.CoreLib.dll"
        fi
    done < <(dotnet --list-runtimes)
    if [ -z "$best_path" ] || [ ! -f "$best_path" ]; then
        echo "error: no net10.0 (Microsoft.NETCore.App 10.0.*) shared runtime found" >&2
        return 1
    fi
    printf '%s\n' "$best_path"
}

# invoke_cli ARGS... — run the dn2cpp CLI. `dotnet exec` on DN2CPP_CLI_DLL when
# set (skips MSBuild), else `dotnet run --project`.
invoke_cli() {
    if [ -n "${DN2CPP_CLI_DLL:-}" ]; then
        dotnet exec "${DN2CPP_CLI_DLL}" "$@"
    else
        dotnet run --project src/Dn2Cpp.Cli -c "${CONFIG}" -- "$@"
    fi
}

# build_proj CSPROJ — `dotnet build` with the gate's standard flags. No-op under
# DN2CPP_SKIP_BUILD: run-all-gates.sh pre-builds every project, which also stops
# two gates rebuilding one shared ProjectReference concurrently.
build_proj() {
    [ -n "${DN2CPP_SKIP_BUILD:-}" ] && return 0
    dotnet build "$1" -c "$CONFIG" --nologo -v q
}

# ── Opting out: the skip protocol ─────────────────────────────────────────────
# gate_skip REASON — the only sanctioned opt-out: "SKIP: REASON", exit 77,
# counted apart from the greens (exit 0 after a SKIP line fails), and itself a
# failure under DN2CPP_REQUIRE_ALL=1.
GATE_SKIP_RC=77
gate_skip() {
    if [ "${DN2CPP_REQUIRE_ALL:-0}" = "1" ]; then
        printf 'FAIL: prerequisite absent, and DN2CPP_REQUIRE_ALL=1 demands every gate run: %s\n' "$*" >&2
        exit 1
    fi
    printf 'SKIP: %s\n' "$*"
    exit "$GATE_SKIP_RC"
}

# gate_partial REASON — the gate runs and passes with one coverage SECTION
# unavailable. Cached as partial; a failure under DN2CPP_REQUIRE_ALL=1.
gate_partial() {
    if [ "${DN2CPP_REQUIRE_ALL:-0}" = "1" ]; then
        printf 'FAIL: coverage section skipped, and DN2CPP_REQUIRE_ALL=1 demands every section run: %s\n' "$*" >&2
        exit 1
    fi
    # Latch for gate_cache_commit; never reset (conservative direction).
    _GATE_PARTIAL=1
    _GATE_PARTIAL_REASON="${_GATE_PARTIAL_REASON:+$_GATE_PARTIAL_REASON$'\n'}$*"
    printf 'PARTIAL: %s\n' "$*"
}

# gate_expected_partial REASON — a permanent, declared partial; the only way a
# DN2CPP_REQUIRE_ALL=1 run stays green with a non-asserting section. The hole
# must be STRUCTURAL (an absent NDK is not) and the reason must NAME the gate
# asserting that surface for real. Own variable: it cannot launder a partial.
gate_expected_partial() {
    _GATE_EXPECTED_PARTIAL=1
    _GATE_EXPECTED_PARTIAL_REASON="${_GATE_EXPECTED_PARTIAL_REASON:+$_GATE_EXPECTED_PARTIAL_REASON$'\n'}$*"
    printf 'EXPECTED PARTIAL: %s\n' "$*"
}

# gate_warn REASON — maintenance warning, not a coverage verdict: passes in
# every mode, no cache latch. A clock-keyed check calls it before
# gate_cache_check, unconditionally, so it prints on warm runs too. Prefix is
# GATE-WARNING because Godot prints "WARNING:" at line start itself.
gate_warn() {
    printf 'GATE-WARNING: %s\n' "$*"
}

# cacert_staleness_warn — the vendored CA bundle goes stale on a clock:
# warn (never fail) past the ~6-month bar of third_party/cacert/
# DN2CPP-VENDORED.md. A missing or unparseable header IS a hard fail.
cacert_staleness_warn() {
    local pem="third_party/cacert/cacert.pem"
    [ -f "$pem" ] || { echo "FAIL: $pem not found — the vendored CA bundle is gone" >&2; exit 1; }
    local hdr
    hdr=$(sed -n 's/^## Certificate data from Mozilla as of: \(.*\) GMT$/\1/p' "$pem")
    hdr=${hdr%%$'\n'*}
    if [ -z "$hdr" ]; then
        echo "FAIL: $pem carries no '## Certificate data from Mozilla as of: ... GMT' header — not the bundle curl publishes (see third_party/cacert/DN2CPP-VENDORED.md)" >&2
        exit 1
    fi
    # GNU date (-d), BSD strptime (-j -f) fallback; LC_ALL=C names, -u for GMT.
    local epoch
    epoch=$(LC_ALL=C date -u -d "$hdr" +%s 2>/dev/null) \
        || epoch=$(LC_ALL=C date -j -u -f '%a %b %d %H:%M:%S %Y' "$hdr" +%s 2>/dev/null) \
        || { echo "FAIL: cannot parse the CA bundle's header date ('$hdr GMT') with GNU or BSD date" >&2; exit 1; }
    local age_days=$(( ( $(date -u +%s) - epoch ) / 86400 ))
    if [ "$age_days" -ge 365 ]; then
        gate_warn "embedded CA bundle ($pem) is $age_days days old (Mozilla extraction: $hdr GMT) — over a YEAR past extraction, roots have expired/changed since; refresh it now per third_party/cacert/DN2CPP-VENDORED.md §'Updating to a new version'"
    elif [ "$age_days" -ge 183 ]; then
        gate_warn "embedded CA bundle ($pem) is $age_days days old (Mozilla extraction: $hdr GMT) — past the ~6-month due bar; refresh it per third_party/cacert/DN2CPP-VENDORED.md §'Updating to a new version'"
    else
        echo "OK: embedded CA bundle is $age_days days old (Mozilla extraction: $hdr GMT) — within the ~6-month refresh window"
    fi
}

# ── CRI ADX LE assets (NuGet package + sample repository) ─────────────────────
# Proprietary inputs. Each helper echoes its path(s) and returns non-zero
# WITHOUT printing when absent, so a gate turns the miss into a gate_skip. The
# package (2.30.196) is asymmetric: managed DLLs under SHORT RIDs, natives under
# arch-qualified ones; runtimes/browser-wasm has no native/ subfolder.

# Pinned SDK drop; DN2CPP_CRI_VERSION overrides the version,
# DN2CPP_CRI_NUGET_ROOT an unpacked root (wins over both). The sample repo's own
# CriWare.CriAtomLE directory is the UNITY package, and reads as missing.
CRI_ATOM_VERSION=${DN2CPP_CRI_VERSION:-2.30.196}

# cri_nuget_root — echo the package root (DN2CPP_CRI_NUGET_ROOT, else
# ~/.nuget/packages/criware.criatomle/<version>); 1 silently when absent.
cri_nuget_root() {
    local root="${DN2CPP_CRI_NUGET_ROOT:-$HOME/.nuget/packages/criware.criatomle/$CRI_ATOM_VERSION}"
    [ -d "$root" ] || return 1
    printf '%s\n' "$root"
}

# cri_managed_dll FLAVOR — echo CriWare.CriAtomLE.dll for desktop, browser or
# android; 1 silently when absent. An unknown FLAVOR complains on stderr — a
# typo must not become a silent skip.
cri_managed_dll() {
    local root sub dll
    root="$(cri_nuget_root)" || return 1
    case "$1" in
        desktop) sub="lib/net7.0" ;;
        browser) sub="runtimes/browser/lib/net7.0" ;;
        android) sub="runtimes/android/lib/net7.0" ;;
        *)
            echo "error: cri_managed_dll: unknown flavor '$1' (want desktop|browser|android)" >&2
            return 1
            ;;
    esac
    dll="$root/$sub/CriWare.CriAtomLE.dll"
    [ -f "$dll" ] || return 1
    printf '%s\n' "$dll"
}

# cri_wasm_native_files — echo the browser-wasm native set, one path per line,
# directly under runtimes/browser-wasm. All-or-nothing: 1 on the first miss.
cri_wasm_native_files() {
    local root dir f
    local files=(cri_atom.a libcri_ware_le.a crifs_io.jslib crinc_webaudio.jslib crinc_webaudio2.jslib)
    root="$(cri_nuget_root)" || return 1
    dir="$root/runtimes/browser-wasm"
    for f in "${files[@]}"; do
        [ -f "$dir/$f" ] || return 1
    done
    for f in "${files[@]}"; do
        printf '%s\n' "$dir/$f"
    done
}

# cri_android_native_files — the android-arm64 .so set, one path per line; same
# all-or-nothing contract.
cri_android_native_files() {
    local root dir f
    local files=(libcri_atom.so libcri_jni_shared.so libcri_ware_android_le.so)
    root="$(cri_nuget_root)" || return 1
    dir="$root/runtimes/android-arm64/native"
    for f in "${files[@]}"; do
        [ -f "$dir/$f" ] || return 1
    done
    for f in "${files[@]}"; do
        printf '%s\n' "$dir/$f"
    done
}

# cri_sample_root — echo the sample repo root (DN2CPP_CRI_SAMPLE_ROOT, else
# ../cri_adx_le_for_csharp), normalized absolute; 1 silently when absent.
cri_sample_root() {
    local root="${DN2CPP_CRI_SAMPLE_ROOT:-../cri_adx_le_for_csharp}"
    [ -d "$root" ] || return 1
    (cd "$root" && pwd)
}

# ── Godot's per-user data directory ───────────────────────────────────────────
# godot_user_data_dir — the editor's state dir (export_templates/<version>/
# included). Every export-template probe goes through here; hand-spelled macOS
# paths reported installed templates as missing elsewhere.
godot_user_data_dir() {
    case "$DN2CPP_OS" in
        macos)   printf '%s\n' "$HOME/Library/Application Support/Godot" ;;
        windows) printf '%s\n' "$(cygpath -u "${APPDATA:-$HOME/AppData/Roaming}")/Godot" ;;
        *)       printf '%s\n' "$HOME/.local/share/godot" ;;
    esac
}

# godot_editor_config_dir — where the editor keeps editor_settings-*.tres. The
# same directory as the data one on macOS/Windows, but NOT on Linux: there the
# editor splits XDG config from XDG data, and reading the settings out of the
# data dir finds no file on a fully configured host.
godot_editor_config_dir() {
    case "$DN2CPP_OS" in
        macos|windows) godot_user_data_dir ;;
        *)
            local config_home="$HOME/.config"
            case "${XDG_CONFIG_HOME:-}" in
                /*) config_home="$XDG_CONFIG_HOME" ;;
            esac
            printf '%s\n' "$config_home/godot"
            ;;
    esac
}

# ── Godot export-template version identity ────────────────────────────────────
# godot_template_version_dir [GODOT_BIN] — echo the export-template version dir
# parsed from `GODOT_BIN --version` (default $GODOT, then `godot`); the ONE
# parser, so installer and detectors cannot drift. A fixed four-component cut
# drops ".mono" and over-reads a 2-component version. Also sets:
#   GODOT_TEMPLATE_VERSION_DIR  the echo ("4.7.1.stable.mono")
#   GODOT_RELEASE_TAG           godot-builds tag ("4.7.1-stable"), never .mono
#   GODOT_VERSION_FLAVOR        ".mono" or ""
#   GODOT_FULL_VERSION          raw --version line ("" if the probe failed)
# Returns 1, everything empty, when missing or unparseable.
# godot_nuget_sdk_version FEED — echo the Godot.NET.Sdk version FEED carries,
# off its single Godot.NET.Sdk.<version>.nupkg. A gate re-pins a THIRD-PARTY
# sample's Sdk onto this rather than onto a literal nothing re-pins.
godot_nuget_sdk_version() {
    local pkg
    for pkg in "$1"/Godot.NET.Sdk.*.nupkg; do
        [ -f "$pkg" ] || continue
        pkg="${pkg##*/Godot.NET.Sdk.}"
        printf '%s\n' "${pkg%.nupkg}"
        return 0
    done
    echo "error: no Godot.NET.Sdk.*.nupkg in $1" >&2
    return 1
}

godot_template_version_dir() {
    GODOT_TEMPLATE_VERSION_DIR=""
    GODOT_RELEASE_TAG=""
    GODOT_VERSION_FLAVOR=""
    GODOT_FULL_VERSION=""
    local bin="${1:-${GODOT:-godot}}"
    local full
    full="$("$bin" --version 2>/dev/null | tail -1)" || true
    GODOT_FULL_VERSION="$full"
    [ -n "$full" ] || return 1
    local -a v
    IFS='.' read -ra v <<< "$full"
    local status_idx=-1 i
    for i in "${!v[@]}"; do
        case "${v[$i]}" in
            stable|beta*|rc*|dev*|alpha*) status_idx=$i; break ;;
        esac
    done
    # At least <major>.<minor> must precede the status token.
    [ "$status_idx" -ge 2 ] || return 1
    local version_num status flavor
    version_num="$(IFS='.'; echo "${v[*]:0:$status_idx}")"
    status="${v[$status_idx]}"
    flavor=""
    [ "${v[$((status_idx+1))]:-}" = "mono" ] && flavor=".mono"
    GODOT_VERSION_FLAVOR="$flavor"
    GODOT_RELEASE_TAG="${version_num}-${status}"
    GODOT_TEMPLATE_VERSION_DIR="${version_num}.${status}${flavor}"
    printf '%s\n' "$GODOT_TEMPLATE_VERSION_DIR"
}

# file_sig FILE — size+mtime signature for a cache-key term. GNU stat first, BSD
# fallback, "none" if neither: `stat -f` differs between them, so the BSD form
# degrades to a near-constant on GNU stat rather than failing.
file_sig() {
    stat -c '%s %Y' "$1" 2>/dev/null || stat -f '%z %m' "$1" 2>/dev/null || echo none
}

# THE RULE the four helpers below share: A CACHE-KEY TERM MUST NEVER GO EMPTY,
# AND MUST NOT GO CONSTANT ON AN INPUT THE KEY EXISTS TO DISCRIMINATE. A term
# answering its "no such file" marker for an input it merely failed to RESOLVE
# discriminates nothing, silently. Two producers: a dereference resolved against
# the wrong directory (file_sig_deref), and a fallback attached to a PIPELINE
# whose tail stage exits 0 on empty input (reachable only under `pipefail`).
# Test the real input, or capture the output and test THAT.

# or_none VALUE [MARKER] — VALUE, else MARKER (default `none`). For terms whose
# input is a command's output, where the capture itself is what gets tested.
or_none() {
    if [ -n "${1:-}" ]; then
        printf '%s\n' "$1"
    else
        printf '%s\n' "${2:-none}"
    fi
}

# file_hash FILE — SHA-256 of CONTENT; `none` when absent, `unhashable` when
# unreadable. Use where size+mtime is not enough (byte-different, same size).
file_hash() {
    local v
    if [ ! -f "$1" ]; then
        printf 'none\n'
        return 0
    fi
    v="$(shasum -a 256 "$1" 2>/dev/null | awk '{print $1}')" || v=""
    [ -n "$v" ] || v=unhashable
    printf '%s\n' "$v"
}

# file_text FILE [MAXLINES] — a file's text as ONE cache-key term: MAXLINES
# lines (default all), newlines to spaces, trailing blanks dropped, `none` when
# absent/unreadable/blank. For setup aids' stamp files.
file_text() {
    local v=""
    if [ -f "$1" ]; then
        if [ -n "${2:-}" ]; then
            v="$(head -n "$2" "$1" 2>/dev/null | LC_ALL=C tr '\n' ' ')" || v=""
        else
            v="$(LC_ALL=C tr '\n' ' ' < "$1" 2>/dev/null)" || v=""
        fi
    fi
    v="${v%"${v##*[! ]}"}"
    [ -n "$v" ] || v=none
    printf '%s\n' "$v"
}

# file_sig_deref FILE — file_sig of what FILE ultimately NAMES, resolving each
# target against its OWN link's directory: a relative target signed from the
# caller's cwd fails stat and yields `none`. A non-symlink is signed as itself.
file_sig_deref() {
    local p="$1" t i=0
    while [ "$i" -lt 16 ]; do
        t="$(readlink "$p" 2>/dev/null)" || break
        [ -n "$t" ] || break
        case "$t" in
            /*|[A-Za-z]:[/\\]*) p="$t" ;;
            *) p="$(dirname "$p")/$t" ;;
        esac
        i=$((i + 1))
    done
    file_sig "$p"
}

# src_tree_hash — fingerprint of what the self-hosted native `dn2cpp` is built
# from (`src/`, `runtime/`, `third_party/`); written by gates/selfhost-emit.sh,
# read by dist/package-toolchain.sh so it cannot reuse a stale binary. Hashes
# working-tree CONTENT, never git state: index blob ids miss an unstaged edit,
# HEAD-derived state re-hashes on merely committing. Deleted-but-tracked paths
# are filtered (a missing operand aborts under `set -e`).
src_tree_hash() {
    local paths=(src runtime third_party)
    # Refused outside a checkout: an empty enumeration hashes to a constant,
    # restoring the blind reuse this exists to remove.
    git rev-parse --git-dir >/dev/null 2>&1 || {
        echo "error: src_tree_hash needs a git checkout to enumerate ${paths[*]}" >&2
        return 1
    }
    (
        git ls-files --cached --others --exclude-standard -z -- "${paths[@]}" \
            | while IFS= read -r -d '' f; do
                  if [ -f "$f" ]; then printf '%s\n' "$f"; fi
              done \
            | LC_ALL=C sort | tr '\n' '\0' | xargs -0 shasum -a 256
    ) | shasum -a 256 | cut -c1-16
}

# selfhost_bin_fresh — 0 when the self-hosted native CLI was built from the
# sources now in the tree, 1 when it has to be rebuilt. Reports nothing and sets
# SELFHOST_BIN_PATH / SELFHOST_SRC_STAMPED / SELFHOST_SRC_NOW instead: the askers
# (godot_fork_preflight, dist/package-toolchain.sh, gates/pre-merge.sh) each phrase
# the same fact differently, and a rebuild one of them skips is a rebuild another
# demands — which is why the comparison lives here and not at any of them.
#
# A MISSING stamp is unknown provenance, not a pass. `[ -x ]` alone is how a binary
# from an older tree once got bundled into the fork's export toolchain and failed
# every gate in that chain, naming neither the binary nor its age.
selfhost_bin_fresh() {
    SELFHOST_BIN_PATH="artifacts/selfhost-fullcli/dn2cpp$EXE_EXT"
    SELFHOST_SRC_STAMP_FILE="artifacts/selfhost-fullcli/dn2cpp.src-hash"
    SELFHOST_SRC_STAMPED="$(cat "$SELFHOST_SRC_STAMP_FILE" 2>/dev/null || echo '<no stamp>')"
    SELFHOST_SRC_NOW="$(src_tree_hash)"
    [ -x "$SELFHOST_BIN_PATH" ] || return 1
    [ -f "$SELFHOST_SRC_STAMP_FILE" ] || return 1
    [ "$SELFHOST_SRC_STAMPED" = "$SELFHOST_SRC_NOW" ]
}

# ── Native build backend (CMake) ──────────────────────────────────────────────
# Sole native backend. compile_console / compile_gdextension link the runtime
# libs ensure_cmake_runtime builds once and exports as dn2cpp-targets.cmake.
CMAKE=${CMAKE:-cmake}

# bundled_cmake ROOT / bundled_ninja ROOT — the executables a bundle carries.
# Extension-less would be wrong on Windows for the reason bin/dn2cpp.exe is:
# CreateProcess cannot launch a PE image without the suffix.
bundled_cmake() { printf '%s/buildtools/cmake/bin/cmake%s\n' "$1" "$EXE_EXT"; }
bundled_ninja() { printf '%s/buildtools/ninja/ninja%s\n'     "$1" "$EXE_EXT"; }

# emsdk_node SDKROOT / bundled_node BUNDLE — the node the SDK carries, which
# every emcc link runs. The split is the upstream archives' own shape, not the
# suffix: nodejs.org roots the POSIX binary under bin/ and the Windows one at the
# tree top. It is absorbed here rather than normalized while unpacking because
# setup-*.sh stages upstream verbatim (gates/setup-buildtools.sh).
emsdk_node() {
    if [ "$DN2CPP_OS" = windows ]; then
        printf '%s/node/node.exe\n' "$1"
    else
        printf '%s/node/bin/node\n' "$1"
    fi
}
bundled_node() { emsdk_node "$1/emsdk"; }

# emsdk_node_cfg — the same path as a .emscripten NODE_JS value. Relative to
# $CFGDIR (the config's own directory, `<sdk>/emscripten/`) so the SDK tree stays
# movable; single-quoted so this shell does not expand what emcc must.
emsdk_node_cfg() {
    if [ "$DN2CPP_OS" = windows ]; then
        printf "'\$CFGDIR/../node/node.exe'\n"
    else
        printf "'\$CFGDIR/../node/bin/node'\n"
    fi
}

# ── MSVC (cl.exe) toolchain import ────────────────────────────────────────────
# Opt in with CMAKE_CXX_COMPILER=cl (or an absolute cl.exe path); imports
# vcvarsall x64 so a plain Git Bash gets cl.exe non-interactively.
ensure_msvc_env() {
    [ "$DN2CPP_OS" = windows ] || return 0
    is_msvc_compiler || return 0
    # cl.exe on PATH does not imply vcvars ran; without LIB link.exe is lost.
    command -v cl.exe >/dev/null 2>&1 && [ -n "${LIB:-}" ] && [ -n "${INCLUDE:-}" ] && return 0

    local vswhere="/c/Program Files (x86)/Microsoft Visual Studio/Installer/vswhere.exe"
    if [ ! -x "$vswhere" ]; then
        echo "error: cl.exe not on PATH and vswhere.exe not found at: $vswhere" >&2
        echo "       (install VS2022 Build Tools with the C++ workload, or launch this shell from a Developer Command Prompt)" >&2
        return 1
    fi
    # -products '*': a BuildTools-only install is invisible to the default filter.
    local vs_install
    vs_install=$("$vswhere" -latest -products '*' \
        -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 \
        -property installationPath)
    if [ -z "$vs_install" ]; then
        echo "error: no Visual Studio install with the C++ (VC.Tools.x86.x64) component found" >&2
        return 1
    fi
    local vcvarsall="$vs_install/VC/Auxiliary/Build/vcvarsall.bat"
    if [ ! -f "$vcvarsall" ]; then
        echo "error: vcvarsall.bat not found at: $vcvarsall" >&2
        return 1
    fi

    # A generated .bat, not an inline cmd /c: nested quoting does not survive
    # bash relaying. The trailing `set` dumps the env for re-export.
    local tmp_bat tmp_env
    tmp_bat=$(mktemp --suffix=.bat)
    tmp_env=$(mktemp)
    {
        printf '@echo off\r\n'
        printf 'call "%s" x64 || exit /b 1\r\n' "$(cygpath -w "$vcvarsall")"
        printf 'set\r\n'
    } > "$tmp_bat"
    if ! cmd //c "$(cygpath -w "$tmp_bat")" > "$tmp_env" 2>&1; then
        echo "error: vcvarsall.bat x64 failed:" >&2
        cat "$tmp_env" >&2
        rm -f "$tmp_bat" "$tmp_env"
        return 1
    fi

    local name value
    while IFS='=' read -r name value; do
        # `set` writes CRLF; an unstripped '\r' corrupts the last entry of LIB.
        value="${value%$'\r'}"
        case "$name" in
            INCLUDE|LIB|LIBPATH)
                # Consumed by native cl.exe/link.exe: keep Windows ';' format.
                export "$name=$value"
                ;;
            Path|PATH)
                # This bash reads PATH too: merge in Unix format.
                export PATH="$(cygpath -u -p "$value"):$PATH"
                ;;
        esac
    done < "$tmp_env"
    rm -f "$tmp_bat" "$tmp_env"

    command -v cl.exe >/dev/null 2>&1 || { echo "error: cl.exe still not on PATH after importing vcvarsall x64" >&2; return 1; }
}
ensure_msvc_env

# ensure_node_on_path — additive last resort: put EMSDK_NODE's dir on PATH
# (emsdk does not, on Windows) so wasm gates stop gate_skipping "node not found"
# on a complete toolchain. Probed by RUNNING it. DN2CPP_NODE names the BINARY.
# It is a last resort and stays one: it answers only for a gate that resolves no
# SDK at all. An SDK dn2cpp_emsdk_resolve picks carries its own node and puts it
# ahead of this one, which is the order that must hold — the node a gate finds
# has to be the node the link runs.
ensure_node_on_path() {
    local cand nodedir
    cand="${DN2CPP_NODE:-}"
    if [ -z "$cand" ]; then
        if command -v node >/dev/null 2>&1; then
            return 0
        fi
        cand="${EMSDK_NODE:-}"
    fi
    [ -n "$cand" ] || return 0
    # EMSDK_NODE is native form on Windows; cygpath exists nowhere else.
    nodedir="$(cygpath -u "$cand" 2>/dev/null || printf '%s' "$cand")"
    nodedir="$(dirname "$nodedir")"
    if [ -d "$nodedir" ] && "$nodedir/node" --version >/dev/null 2>&1; then
        export PATH="$nodedir:$PATH"
    fi
    return 0
}
ensure_node_on_path

# ── Pinned third-party downloads ──────────────────────────────────────────────
# fetch_pinned URL SHA CACHED — download URL into CACHED unless the cached copy
# already matches SHA. The hash is verified on `CACHED.part` and only a match is
# renamed into place, so a truncated transfer or a mirror's error page can never
# be read as a complete archive on a later run.
fetch_pinned() {
    local url="$1" sha="$2" cached="$3" got
    if [ "$(file_hash "$cached")" = "$sha" ]; then
        echo "skip: cached $cached"
        return 0
    fi
    echo "downloading $url"
    mkdir -p "$(dirname "$cached")"
    curl -fL --progress-bar -o "$cached.part" "$url"
    got="$(file_hash "$cached.part")"
    [ "$got" = "$sha" ] || {
        echo "error: sha256 mismatch for $(basename "$url")" >&2
        echo "       expected $sha" >&2
        echo "       got      $got" >&2
        rm -f "$cached.part"; return 1; }
    mv "$cached.part" "$cached"
}

# pin_field PIN KEY — a scalar from a pin file. Whole-file awk, never an early
# exit: the banned pipeline shape is banned here too.
pin_field() {
    awk -v k="$2" '$1 == k { v = $2 } END { print v }' "$1"
}

# release_version_split VERSION — split a release version into RELEASE_BASE_VER
# (the Godot version) and RELEASE_DN2CPP_VER (dn2cpp's own semver). Every dist/
# script asks here rather than spelling the form again: a second spelling drifts,
# and the one that drifts accepts a version the release is not named for.
release_version_split() {
    if [[ ! "$1" =~ ^([0-9]+\.[0-9]+\.[0-9]+)-dn2cpp\.([0-9]+\.[0-9]+)$ ]]; then
        echo "error: --version must read <major>.<minor>.<patch>-dn2cpp.<X>.<Y>, got: $1" >&2
        return 1
    fi
    RELEASE_BASE_VER="${BASH_REMATCH[1]}"
    RELEASE_DN2CPP_VER="${BASH_REMATCH[2]}"
}

# dn2cpp_host_tag — this machine's key in a pin file's `archive <host> …` rows,
# and the host term of gates/setup-emsdk.sh's default output directory.
dn2cpp_host_tag() {
    case "$DN2CPP_OS:$(uname -m)" in
        macos:arm64)    printf 'macos-arm64\n' ;;
        linux:x86_64)   printf 'linux-x64\n' ;;
        windows:x86_64) printf 'windows-x64\n' ;;
        *) return 1 ;;
    esac
}

# unzip_strip ZIP DEST STRIP — extract ZIP under DEST with STRIP leading path
# components removed, matching `tar --strip-components=STRIP`. The depth is
# always passed, never inferred from the member names: an archive whose root
# happens to be one shared directory and one that is deliberately flat are
# indistinguishable from the listing, and guessing wrong buries the tree a level
# down where every later assert reads as "missing".
#
# Execute bits do not survive: python's zipfile drops them. A caller unpacking a
# POSIX executable has to chmod it back.
unzip_strip() {
    local py
    py="$(resolve_python)" || return 1
    # shellcheck disable=SC2086
    $py - "$1" "$2" "$3" <<'PYEOF'
import os, sys, zipfile
zip_path, dest, strip = sys.argv[1], sys.argv[2], int(sys.argv[3])
with zipfile.ZipFile(zip_path) as z:
    for info in z.infolist():
        parts = [p for p in info.filename.split('/') if p][strip:]
        if not parts or info.filename.endswith('/'):
            continue
        target = os.path.join(dest, *parts)
        os.makedirs(os.path.dirname(target), exist_ok=True)
        with z.open(info) as src, open(target, 'wb') as out:
            while True:
                chunk = src.read(1 << 20)
                if not chunk:
                    break
                out.write(chunk)
PYEOF
}

# ── The pinned cmake + ninja a bundle carries ─────────────────────────────────
BUILDTOOLS_PIN=gates/expected/buildtools-pin.txt

# ── The Emscripten SDK ────────────────────────────────────────────────────────
EMSDK_PIN=gates/expected/emsdk-pin.txt

# ── The Node.js the SDK carries, because every emcc link runs one ─────────────
NODE_PIN=gates/expected/node-pin.txt

# _emsdk_set_ctx ORIGIN ROOT STAMP — record WHICH SDK was resolved, as the cache
# key term gate_cache_check reads. Deliberately NOT exported: a gate keys the SDK
# it resolved for itself, and one that resolves none must key the same whether or
# not the suite runner (which resolves for its own prebuild) is its parent.
_emsdk_set_ctx() {
    local id="$3"
    # Without a stamp the tree is identified only by where it sits, and one
    # rebuilt in place under that path moves nothing but the version.
    if [ -z "$id" ] || [ "$id" = none ]; then
        id="$(or_none "$(first_line "$(emcc --version 2>/dev/null)")")"
    fi
    _GATE_EMSDK_CTX="$1 $(or_none "$2") $id"
}

# dn2cpp_emsdk_resolve [--no-bundled] — put ONE Emscripten SDK on this process's
# PATH: an explicit override, else a staged toolchain bundle's, else the one
# gates/setup-emsdk.sh unpacks. Finding none is a silent no-op, so a host whose
# only emcc is on PATH behaves exactly as before.
#
# A bundled SDK outranks PATH because the Web template was baked by that emcc and
# the drop-in linked into it must agree on EH flavour, WASM_BIGINT and the dylink
# ABI — a disagreement links clean and dies at run time. Which one was chosen is
# therefore always logged.
#
# --no-bundled declines that answer. The bundle ships a FROZEN cache baked to the
# variants an EXPORT links, and a build that resolves any other one — the engine's
# own Web template pulls the pic/ WebGL2 archives, an unoptimized link the -debug
# archives — dies on a missing cache file. So the bundle answers only for a gate
# whose subject is the SHIPPED shape; every other wasm build takes an SDK that may
# still fill a cache, and owns the version match itself.
#
# Call it EXPLICITLY (never from the sourcing) and BEFORE gate_cache_check, which
# keys on _GATE_EMSDK_CTX, set here.
dn2cpp_emsdk_resolve() {
    local root="" origin="" cand cfg host ver stamp bundled=1
    [ "${1:-}" = --no-bundled ] && bundled=0
    if [ -n "${DN2CPP_EMSDK:-}" ]; then
        root="$DN2CPP_EMSDK"
        origin=DN2CPP_EMSDK
        [ -x "$root/emscripten/emcc" ] || {
            echo "error: DN2CPP_EMSDK=$root holds no emscripten/emcc (name the SDK root, the directory with bin/ and emscripten/)" >&2
            return 1; }
    fi
    if [ -z "$root" ] && [ "$bundled" -eq 1 ]; then
        # Same layout term as stage_editor_toolchain: the bundle a forked editor
        # exports with is the one whose emsdk baked its Web template.
        cand="artifacts/toolchain/dn2cpp-toolchain-0.1.0-${DN2CPP_OS}-$(uname -m)/emsdk"
        [ -x "$cand/emscripten/emcc" ] && { root="$cand"; origin=bundled; }
    fi
    if [ -z "$root" ]; then
        host="$(dn2cpp_host_tag)" || host=""
        ver="$(pin_field "$EMSDK_PIN" version)"
        cand="artifacts/emsdk/$ver-$host"
        [ -n "$host" ] && [ -n "$ver" ] && [ -x "$cand/emscripten/emcc" ] \
            && { root="$cand"; origin=setup-emsdk; }
    fi
    if [ -z "$root" ]; then
        _emsdk_set_ctx path "$(command -v emcc || true)" ""
        return 0
    fi
    root="$(cd -P "$root" && pwd -P)" || return 1

    # An inherited emsdk's variables all outrank the config file, so a second SDK
    # in the environment would keep steering the one chosen here.
    unset EM_CACHE EM_LLVM_ROOT EM_BINARYEN_ROOT EM_FROZEN_CACHE EMSDK EMSDK_NODE EMSDK_PYTHON EMSCRIPTEN EM_NODE_JS
    # A staged python is resolved through the environment, never the config file:
    # emcc.exe reads EMSDK_PYTHON ahead of any PATH search.
    [ -x "$root/python/python.exe" ] \
        && export EMSDK_PYTHON="$(native_path "$root/python/python.exe")"
    # DN2CPP_NODE names a binary, so it answers through the config's own key.
    # Otherwise the SDK's node leads PATH, because the config alone does not
    # settle it: the wasm gates ask `command -v node` for their skip decision,
    # and what they find must be what the link runs. Side effect in a dev tree,
    # which is untrimmed: node/bin holds npm/npx/corepack beside the binary, so
    # this hides the host's. A bundle stages node alone.
    if [ -n "${DN2CPP_NODE:-}" ]; then
        export EM_NODE_JS="$(native_path "$DN2CPP_NODE")"
    else
        cand="$(emsdk_node "$root")"
        if [ -x "$cand" ]; then
            cand="$(dirname "$cand")"
            case ":$PATH:" in
                *":$cand:"*) ;;
                *) export PATH="$cand:$PATH" ;;
            esac
        fi
    fi
    case ":$PATH:" in
        *":$root/emscripten:"*) ;;
        *) export PATH="$root/emscripten:$PATH" ;;
    esac
    cfg=""
    for cand in "$root/emscripten/.emscripten" "$root/.emscripten"; do
        [ -f "$cand" ] && { cfg="$cand"; break; }
    done
    if [ -n "$cfg" ]; then
        export EM_CONFIG="$(native_path "$cfg")"
        echo "emsdk: $root ($origin)"
    else
        # Without a config emcc finds clang and wasm-opt only off PATH, so bin/
        # has to go on it — and bin/ carries a clang++ that then shadows the
        # host's. gates/setup-emsdk.sh writes a config for exactly this reason.
        unset EM_CONFIG
        case ":$PATH:" in
            *":$root/bin:"*) ;;
            *) export PATH="$root/bin:$PATH" ;;
        esac
        echo "emsdk: $root ($origin, no .emscripten — bin/ on PATH)"
    fi
    # The stamp, not the version: two SDKs of one version carry different baked
    # caches (the bundle's is frozen and trimmed), and `emcc --version` cannot
    # tell them apart — a key resting on it replays the other one's green. Last,
    # because the unstamped arm falls back to that version and must read it off
    # the SDK just put on PATH.
    stamp="$(file_text "$root/.emsdk-stamp")"
    [ "$stamp" = none ] && stamp="$(file_text "$root.pin")"
    _emsdk_set_ctx "$origin" "$root" "$stamp"
    return 0
}

# The Emscripten front-ends, by name. Spelled out rather than matched as `em*`,
# which also names emacs.
EMSDK_FRONTENDS="emcc em++ emar emcmake emconfigure emmake emranlib emnm emstrip"
EMSDK_FRONTENDS="$EMSDK_FRONTENDS emsize emrun embuilder emscons emdwp emprofile"
EMSDK_FRONTENDS="$EMSDK_FRONTENDS emsymbolizer emscan-deps em-config empath-split"

# path_without_tools NAME… — echoes this PATH with every named tool taken out of
# it, for the runs that have to prove they need none. A directory holding one is
# usually shared (Homebrew's bin carries emcc next to cmake and ninja), so it is
# replaced by a symlink mirror of itself minus those names — dropping it outright
# would take the rest of the toolchain with it and the run would fail for the
# wrong reason.
path_without_tools() {
    local dir out="" sep="" mirror entry base name hit
    # The mirror root keys on the name set: sharing one across two different sets
    # would hand the second caller a PATH still resolving what it asked to drop.
    local names=" $* " root="artifacts/.tool-free-path/$(printf '%s\n' "$@" \
        | LC_ALL=C sort -u | shasum -a 256 | cut -c1-12)"
    while IFS= read -r dir; do
        [ -n "$dir" ] || continue
        hit=0
        for name in "$@"; do
            # A `.bat`/`.py` sibling is the same tool.
            if [ -x "$dir/$name" ] || [ -f "$dir/$name.bat" ] || [ -f "$dir/$name.py" ]; then
                hit=1
                break
            fi
        done
        if [ "$hit" -eq 1 ]; then
            mirror="$root/$(printf '%s' "$dir" | tr -c 'A-Za-z0-9._-' '-')"
            # Rebuilt when the mirrored directory changed under it; `ln -sfn`, so
            # two gates rebuilding one mirror at once write the same links rather
            # than collide. The caller reads this function through a command
            # substitution, so a cleanup trap set here would fire at once — the
            # mirrors are left behind, under artifacts/, as the symlinks they are.
            if [ ! -d "$mirror" ] || [ "$dir" -nt "$mirror" ]; then
                mkdir -p "$mirror"
                for entry in "$dir"/*; do
                    base="${entry##*/}"
                    case "$names" in *" ${base%.*} "*) continue ;; esac
                    ln -sfn "$entry" "$mirror/$base"
                done
            fi
            [ -n "$(ls -A "$mirror")" ] || continue
            dir="$(cd "$mirror" && pwd -P)"
        fi
        out="$out$sep$dir"
        sep=":"
    done <<<"${PATH//:/$'\n'}"
    printf '%s\n' "$out"
}

# resolve_python — echo a Python 3 command that WORKS. Does not memoize;
# DN2CPP_PYTHON is a caller-supplied override. Accepted only when
# `-c 'print(1)'` exits 0: Windows's python3.exe alias stub resolves but fails.
resolve_python() {
    if [ -n "${DN2CPP_PYTHON:-}" ]; then
        printf '%s\n' "$DN2CPP_PYTHON"
        return 0
    fi
    local cand
    for cand in python3 python "py -3"; do
        # shellcheck disable=SC2086
        if $cand -c 'print(1)' >/dev/null 2>&1; then
            DN2CPP_PYTHON="$cand"
            printf '%s\n' "$DN2CPP_PYTHON"
            return 0
        fi
    done
    echo "error: no working Python 3 interpreter found (tried: python3, python, py -3)" >&2
    return 1
}

# ── Windows oracle console encoding ───────────────────────────────────────────
# dn2cpp natives always write UTF-8; real .NET on Windows encodes redirected
# Console output with the console code page. Force 65001 so oracles match.
[ "$DN2CPP_OS" = windows ] && chcp.com 65001 >/dev/null 2>&1

# _ccache_pch_env — pch_defines + time_macros sloppiness, ccache's requirement
# for PCH (DN2CPP_PCH); without them every PCH compile is a miss. Appends,
# idempotent. CCACHE_PCH_EXTSUM stays unset: CMake drops no .sum for it.
_ccache_pch_env() {
    case ",${CCACHE_SLOPPINESS:-}," in
        *,pch_defines,*) ;;
        *) export CCACHE_SLOPPINESS="${CCACHE_SLOPPINESS:+${CCACHE_SLOPPINESS},}pch_defines,time_macros" ;;
    esac
}

# _cmake_app_builddir OUT — per-app CMake build dir for the active build axis.
# Each axis gets its own dir so different builds of one OUT never clobber or
# race each other; add an axis below and it must gain an arm here too.
_cmake_app_builddir() {
    if [ -n "${WASM:-}" ]; then printf '%s\n' "$PWD/$1/.cmake-wasm"
    elif [ -n "${IOS_DEV:-}" ]; then printf '%s\n' "$PWD/$1/.cmake-ios-dev"
    elif [ -n "${IOS_SIM:-}" ]; then printf '%s\n' "$PWD/$1/.cmake-ios-sim"
    elif [ -n "${ANDROID:-}" ]; then printf '%s\n' "$PWD/$1/.cmake-android"
    elif [ -n "${SCALAR:-}" ]; then printf '%s\n' "$PWD/$1/.cmake-scalar"
    elif [ -n "${HIGHWAY:-}" ]; then printf '%s\n' "$PWD/$1/.cmake-hwy"
    elif [ -n "${DN2CPP_NO_CURL:-}" ]; then printf '%s\n' "$PWD/$1/.cmake-nocurl"
    elif [ -n "${PAL_REFERENCE:-}" ]; then printf '%s\n' "$PWD/$1/.cmake-palref"
    elif [ -n "${DN2CPP_NO_GC:-}" ]; then printf '%s\n' "$PWD/$1/.cmake-nogc"
    elif [ -n "${DN2CPP_GC_BACKEND:-}" ] && [ "${DN2CPP_GC_BACKEND}" != unity ]; then printf '%s\n' "$PWD/$1/.cmake-gc$DN2CPP_GC_BACKEND"
    else printf '%s\n' "$PWD/$1/.cmake"; fi
}

# _android_toolchain_args — echo CMake cache entries (one per line) retargeting
# a configure at Android via the NDK toolchain file (ANDROID_NDK_ROOT required).
ANDROID_ABI=${ANDROID_ABI:-arm64-v8a}
ANDROID_PLATFORM=${ANDROID_PLATFORM:-android-24}
_android_toolchain_args() {
    printf -- '-DCMAKE_TOOLCHAIN_FILE=%s/build/cmake/android.toolchain.cmake\n' "$ANDROID_NDK_ROOT"
    printf -- '-DANDROID_ABI=%s\n' "$ANDROID_ABI"
    printf -- '-DANDROID_PLATFORM=%s\n' "$ANDROID_PLATFORM"
}

# _ios_toolchain_args [SYSROOT] — CMake cache entries (one per line) retargeting
# at iOS; SYSROOT is iphonesimulator (default)/iphoneos. Simulator is
# arm64/x86_64 universal: Godot's broken arm64-sim template slice forces the E2E
# gate onto Rosetta x86_64 (godot#118161). Floor 16.3 — Apple's libc++ marks the
# float std::to_chars overloads unavailable before it.
IOS_DEPLOYMENT_TARGET=${IOS_DEPLOYMENT_TARGET:-16.3}
_ios_toolchain_args() {
    local sysroot="${1:-iphonesimulator}" archs="arm64;x86_64"
    [ "$sysroot" = iphoneos ] && archs="arm64"
    printf -- '-DCMAKE_SYSTEM_NAME=iOS\n'
    printf -- '-DCMAKE_OSX_SYSROOT=%s\n' "$sysroot"
    printf -- '-DCMAKE_OSX_ARCHITECTURES=%s\n' "$archs"
    printf -- '-DCMAKE_OSX_DEPLOYMENT_TARGET=%s\n' "$IOS_DEPLOYMENT_TARGET"
}

# ── The configure stamp ───────────────────────────────────────────────────────
# `-D` acts at CONFIGURE time and a warm dir keeps what it was configured with,
# while the dir NAME covers only the target axis (DN2CPP_NO_GC and
# DN2CPP_HIGHWAY_ARCH arrive via the args array). So each dir stamps its
# exact configure command, one argument per line, and any difference deletes the
# dir and configures cold — `cmake -D` cannot un-set a cached value. The stamp
# holds the absolute -B path, so a moved repository rebuilds too.
#
# A front-end that picks the toolchain file itself (emcmake) is invisible in that
# command, so which SDK configured the dir is not recoverable from it. Such an
# arm passes that identity as STAMP_EXTRA; without it a dir configured by another
# Emscripten reads as current and keeps its cached CMAKE_TOOLCHAIN_FILE forever.
_CMAKE_CONFIGURE_STAMP=.dn2cpp-configure-stamp

# _cmake_stamp_extra — the stamp term for the active build axis. WASM only: the
# identity is the gate cache's own emsdk term, so bundle-vs-pinned and a tree
# re-unpacked at one path both read as different (a path compare would not).
_cmake_stamp_extra() {
    [ -n "${WASM:-}" ] && printf 'emsdk %s\n' "${_GATE_EMSDK_CTX:-}"
    return 0
}

# _cmake_configure MODE STAMP_EXTRA DIR LOG WHAT CMD... — configure DIR by
# running CMD..., keeping DIR's configure stamp in agreement with it.
#   MODE=once   — configure only when the stamp does not name CMD... (runtime dirs).
#   MODE=always — configure every call (app dirs: sources re-globbed, OUT
#                 re-transpiled); the stamp then only discards a stale dir.
# STAMP_EXTRA joins the stamp ahead of the command; empty leaves it unchanged.
# _cmake_stamp_extra is what the two callers pass.
# The stamp is written only after the configure succeeded.
_cmake_configure() {
    local mode="$1" extra="$2" dir="$3" log="$4" what="$5"
    shift 5
    # A mistyped mode must not read as `once`: an app dir would silently stop
    # reconfiguring.
    case "$mode" in
        once|always) ;;
        *) echo "error: _cmake_configure: unknown mode '$mode' (once|always)" >&2; return 1 ;;
    esac
    local stamp="$dir/$_CMAKE_CONFIGURE_STAMP" want current=0
    want="$(printf '%s\n' "$@")"
    if [ -n "$extra" ]; then
        want="$extra
$want"
    fi
    if [ -f "$dir/CMakeCache.txt" ] && [ -f "$stamp" ] && [ "$(cat "$stamp")" = "$want" ]; then
        current=1
    fi
    if [ "$current" = 0 ] && [ -f "$dir/CMakeCache.txt" ]; then
        echo "note: $dir was configured by a different command — reconfiguring it from scratch" >&2
        rm -rf "$dir"
    fi
    mkdir -p "$dir"
    if [ "$current" = 0 ] || [ "$mode" = always ]; then
        _cmake_step "$log" "$what" "$@" || return 1
        printf '%s\n' "$want" > "$stamp"
    fi
}

# ensure_cmake_runtime — build the runtime libraries once into a shared dir and
# echo the exported dn2cpp-targets.cmake; honors DN2CPP_CMAKE_RUNTIME_EXPORT*
# when run-all-gates.sh prebuilt that axis. Axes, each in its own dir: SCALAR,
# HIGHWAY (isolates a DN2CPP_HIGHWAY_ARCH override), WASM (emcmake), IOS_DEV,
# IOS_SIM, ANDROID, DN2CPP_NO_CURL, PAL_REFERENCE, DN2CPP_NO_GC,
# DN2CPP_GC_BACKEND (non-unity) — an axis never falls through to the native
# export. Always builds; configure per the stamp.
ensure_cmake_runtime() {
    # GC=OFF makes the source-tree choice moot, and the two together would
    # otherwise silently pick one (dir naming order) rather than say so.
    if [ -n "${DN2CPP_NO_GC:-}" ] && [ -n "${DN2CPP_GC_BACKEND:-}" ] && [ "${DN2CPP_GC_BACKEND}" != unity ]; then
        echo "error: DN2CPP_NO_GC and DN2CPP_GC_BACKEND=${DN2CPP_GC_BACKEND} together — GC is off, so the backend choice is moot" >&2
        return 1
    fi
    if [ -n "${WASM:-}" ]; then
        [ -n "${DN2CPP_CMAKE_RUNTIME_EXPORT_WASM:-}" ] && { printf '%s\n' "$DN2CPP_CMAKE_RUNTIME_EXPORT_WASM"; return 0; }
    elif [ -n "${IOS_DEV:-}" ]; then
        [ -n "${DN2CPP_CMAKE_RUNTIME_EXPORT_IOS_DEV:-}" ] && { printf '%s\n' "$DN2CPP_CMAKE_RUNTIME_EXPORT_IOS_DEV"; return 0; }
    elif [ -n "${IOS_SIM:-}" ]; then
        [ -n "${DN2CPP_CMAKE_RUNTIME_EXPORT_IOS_SIM:-}" ] && { printf '%s\n' "$DN2CPP_CMAKE_RUNTIME_EXPORT_IOS_SIM"; return 0; }
    elif [ -n "${ANDROID:-}" ]; then
        [ -n "${DN2CPP_CMAKE_RUNTIME_EXPORT_ANDROID:-}" ] && { printf '%s\n' "$DN2CPP_CMAKE_RUNTIME_EXPORT_ANDROID"; return 0; }
    elif [ -n "${SCALAR:-}" ]; then
        [ -n "${DN2CPP_CMAKE_RUNTIME_EXPORT_SCALAR:-}" ] && { printf '%s\n' "$DN2CPP_CMAKE_RUNTIME_EXPORT_SCALAR"; return 0; }
    elif [ -n "${HIGHWAY:-}" ]; then
        [ -n "${DN2CPP_CMAKE_RUNTIME_EXPORT_HWY:-}" ] && { printf '%s\n' "$DN2CPP_CMAKE_RUNTIME_EXPORT_HWY"; return 0; }
    elif [ -n "${DN2CPP_NO_CURL:-}" ]; then
        # Never pre-built; the arm stops the `else` handing back a curl-ful one.
        :
    elif [ -n "${PAL_REFERENCE:-}" ] || [ -n "${DN2CPP_NO_GC:-}" ]; then
        # Same: not pre-built, and the default export is host-PAL + Boehm GC.
        :
    elif [ -n "${DN2CPP_GC_BACKEND:-}" ] && [ "${DN2CPP_GC_BACKEND}" != unity ]; then
        # Same: not pre-built, and the default export links the Unity fork.
        :
    else
        [ -n "${DN2CPP_CMAKE_RUNTIME_EXPORT:-}" ] && { printf '%s\n' "$DN2CPP_CMAKE_RUNTIME_EXPORT"; return 0; }
    fi
    local dir gc=ON
    [ -n "${DN2CPP_NO_GC:-}" ] && gc=OFF
    local configure=("$CMAKE")
    local args=(-S runtime -G Ninja -DDN2CPP_GODOT=ON -DDN2CPP_DOTNET_MODULE=ON -DDN2CPP_USE_GC="$gc")
    if [ -n "${WASM:-}" ]; then
        # Godot runtime off (native-host bridge); .NET-module host ON, since the
        # web export ships the drop-in as a side module.
        dir="$PWD/artifacts/.cmake-runtime-wasm"
        configure=(emcmake "$CMAKE")
        args=(-S runtime -G Ninja -DDN2CPP_GODOT=OFF -DDN2CPP_DOTNET_MODULE=ON -DDN2CPP_USE_GC="$gc")
    elif [ -n "${IOS_DEV:-}" ] || [ -n "${IOS_SIM:-}" ]; then
        # Native source set (POSIX PAL, Boehm GC) retargeted at the iOS SDK;
        # the .NET-module host is a desktop-engine concern, so off.
        local _ios_sysroot=iphonesimulator _ios_suffix=ios-sim
        if [ -n "${IOS_DEV:-}" ]; then _ios_sysroot=iphoneos; _ios_suffix=ios-dev; fi
        dir="$PWD/artifacts/.cmake-runtime-$_ios_suffix"
        args=(-S runtime -G Ninja -DDN2CPP_GODOT=ON -DDN2CPP_USE_GC="$gc")
        local _ios_arg
        while IFS= read -r _ios_arg; do args+=("$_ios_arg"); done < <(_ios_toolchain_args "$_ios_sysroot")
    elif [ -n "${ANDROID:-}" ]; then
        # bionic is POSIX (posix PAL, the GC's __ANDROID__ arms), via the NDK
        # toolchain file. Both hosts on — an APK ships the drop-in in lib/<abi>/.
        [ -n "${ANDROID_NDK_ROOT:-}" ] || { echo "error: ANDROID_NDK_ROOT is not set (required for the ANDROID build axis)" >&2; return 1; }
        dir="$PWD/artifacts/.cmake-runtime-android"
        args=(-S runtime -G Ninja -DDN2CPP_GODOT=ON -DDN2CPP_DOTNET_MODULE=ON -DDN2CPP_USE_GC="$gc")
        local _android_arg
        while IFS= read -r _android_arg; do args+=("$_android_arg"); done < <(_android_toolchain_args)
    elif [ -n "${SCALAR:-}" ]; then
        dir="$PWD/artifacts/.cmake-runtime-scalar"
        args+=(-DDN2CPP_USE_HIGHWAY=OFF)
    elif [ -n "${HIGHWAY:-}" ]; then
        dir="$PWD/artifacts/.cmake-runtime-hwy"
        args+=(-DDN2CPP_USE_HIGHWAY=ON)
        [ -n "${DN2CPP_HIGHWAY_ARCH:-}" ] && args+=(-DDN2CPP_HIGHWAY_ARCH="${DN2CPP_HIGHWAY_ARCH}")
    elif [ -n "${DN2CPP_NO_CURL:-}" ]; then
        # The curl OPT-OUT axis — the only build of USE_CURL=OFF now the option
        # defaults ON. Sole consumer: section 16 of build-and-run-http-get.sh
        # (an HTTP program must fail LOUDLY at link). Only this variable differs.
        dir="$PWD/artifacts/.cmake-runtime-nocurl"
        args+=(-DDN2CPP_USE_CURL=OFF)
    elif [ -n "${PAL_REFERENCE:-}" ]; then
        # The PAL-seam axis: platform/reference/ replaces the host PAL,
        # nothing else moves. Asserted by build-and-run-pal-reference.sh.
        dir="$PWD/artifacts/.cmake-runtime-palref"
        args+=(-DDN2CPP_PAL_REFERENCE=ON)
    elif [ -n "${DN2CPP_NO_GC:-}" ]; then
        # The calloc-fallback axis (docs/PORTING.md §7 step 4). Its own dir is
        # what makes DN2CPP_NO_GC=1 settable: gc=OFF would otherwise reconfigure
        # the SHARED .cmake-runtime and every later gate would link calloc too.
        dir="$PWD/artifacts/.cmake-runtime-nogc"
    elif [ -n "${DN2CPP_GC_BACKEND:-}" ] && [ "${DN2CPP_GC_BACKEND}" != unity ]; then
        # The GC source-tree axis (dev-only; gates/build-and-run-gc-upstream.sh).
        # Own dir for the reason DN2CPP_NO_GC has one: a configure cannot un-set
        # -DDN2CPP_GC_BACKEND once cached, so reusing .cmake-runtime would pin
        # every later gate to whichever tree ran here first.
        dir="$PWD/artifacts/.cmake-runtime-gc${DN2CPP_GC_BACKEND}"
        args+=(-DDN2CPP_GC_BACKEND="${DN2CPP_GC_BACKEND}")
    else
        dir="$PWD/artifacts/.cmake-runtime"
    fi
    # MSVC opt-in, forwarded on every native axis and keyed into the dir name —
    # a dir configured for one compiler must not be reused under another (ABI
    # mismatch). Excluded on WASM/iOS/Android: their toolchain files decide.
    if [ -n "${CMAKE_CXX_COMPILER:-}" ] && [ -z "${WASM:-}${IOS_DEV:-}${IOS_SIM:-}${ANDROID:-}" ]; then
        args+=(-DCMAKE_CXX_COMPILER="${CMAKE_CXX_COMPILER}")
        dir="$dir-$(basename "${CMAKE_CXX_COMPILER%.*}")"
    fi
    # ccache wraps compiles via CMake's launcher vars; DN2CPP_NO_CCACHE=1 opts
    # out. The -U arm is required: `cmake -D` cannot un-set a cached value.
    if [ -z "${DN2CPP_NO_CCACHE:-}" ] && command -v ccache >/dev/null 2>&1; then
        _ccache_pch_env
        args+=(-DCMAKE_C_COMPILER_LAUNCHER=ccache -DCMAKE_CXX_COMPILER_LAUNCHER=ccache)
    else
        args+=(-UCMAKE_C_COMPILER_LAUNCHER -UCMAKE_CXX_COMPILER_LAUNCHER)
    fi
    local cmd=("${configure[@]}" "${args[@]}" -B "$dir")
    _cmake_configure once "$(_cmake_stamp_extra)" "$dir" "$dir/dn2cpp-configure.log" \
        "configuring the runtime build ($dir)" "${cmd[@]}" || return 1
    # Check the build: ninja leaves the previously linked libdn2cpp_runtime.a,
    # so without this the app links the STALE archive and passes. `set -e` misses
    # it (callers read this function through a command substitution). _cmake_step
    # prints to stderr: this function's stdout is the export path.
    _cmake_step "$dir/dn2cpp-build.log" "the runtime failed to build ($dir)" \
        "$CMAKE" --build "$dir" || return 1
    printf '%s\n' "$dir/dn2cpp-targets.cmake"
}

# _cmake_step LOG STEP CMD... — run a cmake/ninja step with output captured to
# LOG, printed on failure. Ninja sends each edge's diagnostics to its own
# stdout, so discarding stdout would discard the error itself.
_cmake_step() {
    local log="$1" step="$2"
    shift 2
    if ! "$@" >"$log" 2>&1; then
        echo "FAIL: $step — the captured output follows ($log):" >&2
        LC_ALL=C sed 's/^/  /' "$log" >&2
        return 1
    fi
}

# cmake_build_app OUT NAME MODE — configure + build OUT/generated*.cpp into a
# native binary via CMake, importing the prebuilt runtime. MODE: "gdext" =
# GDExtension shared lib, "shared" = plain shared lib ([UnmanagedCallersOnly]
# entry points), "dotnet" = Godot mono-module shared lib, else console exe.
# Leaves the artifact in OUT/.cmake.
cmake_build_app() {
    local out="$1" name="$2" mode="${3-}"
    # `|| return 1` explicitly: a bare `x="$(f)"` does not reliably abort under
    # `set -e`, so a failed runtime build would link the stale archive.
    local export_file; export_file="$(ensure_cmake_runtime)" || return 1
    local builddir; builddir="$(_cmake_app_builddir "$out")"
    local configure=("$CMAKE")
    local args=(-S runtime -B "$builddir" -G Ninja
        -DDN2CPP_RUNTIME_EXPORT="$export_file"
        -DDN2CPP_APP_DIR="$PWD/$out"
        -DDN2CPP_APP_NAME="$name")
    if [ -n "${WASM:-}" ]; then
        # WASM: configure through emcmake. GC default-ON (DN2CPP_NO_GC=1 is the
        # calloc escape) and must match the imported runtime's state — this arm
        # reads the env every call while the runtime read it at ITS configure,
        # so the configure stamp is what keeps the two ends together.
        configure=(emcmake "$CMAKE")
        local gc=ON
        [ -n "${DN2CPP_NO_GC:-}" ] && gc=OFF
        args+=(-DDN2CPP_USE_GC="$gc")
    elif [ -n "${IOS_DEV:-}" ] || [ -n "${IOS_SIM:-}" ]; then
        # iOS: must target the same SDK as the imported runtime.
        local _ios_sysroot=iphonesimulator
        [ -n "${IOS_DEV:-}" ] && _ios_sysroot=iphoneos
        local _ios_arg
        while IFS= read -r _ios_arg; do args+=("$_ios_arg"); done < <(_ios_toolchain_args "$_ios_sysroot")
    elif [ -n "${ANDROID:-}" ]; then
        # Android: same toolchain file as the imported runtime's configure.
        local _android_arg
        while IFS= read -r _android_arg; do args+=("$_android_arg"); done < <(_android_toolchain_args)
    fi
    case "$mode" in
        gdext)  args+=(-DDN2CPP_GDEXTENSION=ON) ;;
        shared) args+=(-DDN2CPP_SHARED=ON) ;;
        dotnet) args+=(-DDN2CPP_DOTNET_MODULE=ON) ;;
    esac
    # Real MSVC opt-in: must match the imported runtime's compiler. Excluded on
    # the cross-compile axes — see the guard in ensure_cmake_runtime.
    [ -n "${CMAKE_CXX_COMPILER:-}" ] && [ -z "${WASM:-}${IOS_DEV:-}${IOS_SIM:-}${ANDROID:-}" ] \
        && args+=(-DCMAKE_CXX_COMPILER="${CMAKE_CXX_COMPILER}")
    # ccache launcher, same opt-out as ensure_cmake_runtime. The PCH sloppiness
    # must be exported before the --build below, where the launcher runs.
    if [ -z "${DN2CPP_NO_CCACHE:-}" ] && command -v ccache >/dev/null 2>&1; then
        _ccache_pch_env
        args+=(-DCMAKE_C_COMPILER_LAUNCHER=ccache -DCMAKE_CXX_COMPILER_LAUNCHER=ccache)
    else
        args+=(-UCMAKE_C_COMPILER_LAUNCHER -UCMAKE_CXX_COMPILER_LAUNCHER)
    fi
    [ -n "${DN2CPP_EXTRA_LINK_FLAGS:-}" ] && args+=(-DDN2CPP_APP_LINK_FLAGS="${DN2CPP_EXTRA_LINK_FLAGS}")
    [ -n "${DN2CPP_EXTRA_LINK_LIBS:-}" ] && args+=(-DDN2CPP_APP_LINK_LIBS="${DN2CPP_EXTRA_LINK_LIBS}")
    # Word-split on purpose: a caller needing an unstripped binary to `nm` passes
    # -DDN2CPP_STRIP=OFF -DDN2CPP_DEAD_STRIP=OFF here.
    # shellcheck disable=SC2206
    [ -n "${DN2CPP_EXTRA_CMAKE_ARGS:-}" ] && args+=(${DN2CPP_EXTRA_CMAKE_ARGS})
    # MODE=always: a gate re-transpiles into the same OUT and the source set is
    # globbed. The stamp still matters — a configure cannot un-set an option the
    # previous one passed (the three conditional args above), and a build dir
    # carried along by a moved repository cannot be reconfigured in place.
    # The stamp term matters here despite MODE=always: reconfiguring a warm dir
    # keeps the CMAKE_TOOLCHAIN_FILE the other SDK's emcmake put in it.
    _cmake_configure always "$(_cmake_stamp_extra)" "$builddir" "$builddir/dn2cpp-configure.log" \
        "configuring the app build ($name)" "${configure[@]}" "${args[@]}" || return 1
    _cmake_step "$builddir/dn2cpp-build.log" "building the app ($name)" \
        "$CMAKE" --build "$builddir" || return 1
}

# stage_binary SRC DST — install the freshly built binary at DST atomically:
# copy beside it, then rename over it.
#
# On macOS this is CORRECTNESS, not hygiene: rewriting the bytes of an
# inode that has ALREADY been exec'd makes the next exec of that path die by
# SIGKILL — no output, no crash report. The kernel's validated-vnode state
# decays, so the window is sub-0.1s idle but stretches past 3s under load: an
# idle A/B "refutes" this and is answering about the wrong row. Do not simplify
# back to a `cp`. A rename also cannot be observed half-written.
stage_binary() {
    local src="$1" dst="$2"
    cp -f "$src" "$dst.new" || return 1
    mv -f "$dst.new" "$dst"
}

# compile_console OUT BIN — build OUT/generated*.cpp into the native executable
# OUT/BIN via CMake (runtime/CMakeLists.txt), importing the prebuilt runtime.
compile_console() {
    local out="$1" bin="$2"
    # A generated `main` must leave through dn2cpp_main_exit: plain `return` runs
    # the static destructors while the finalizer thread and Task.Run workers are
    # live, which then lock destroyed mutexes and abort. The race itself is rare
    # and load-dependent, so this is the only deterministic check.
    grep -q 'dn2cpp_main_exit' "$out/generated.cpp" \
        || { echo "FAIL: generated main does not exit through dn2cpp_main_exit" >&2; return 1; }
    cmake_build_app "$out" "$bin" ""
    stage_binary "$(_cmake_app_builddir "$out")/$bin" "$out/$bin"
}

# compile_console_dual_backend OUT BIN — build OUT's generated C++ twice, on the
# SCALAR and HIGHWAY axes, leaving OUT/BIN.scalar and OUT/BIN.hwy for an A/B.
# The plain OUT/BIN left behind is the Highway build (the default backend).
compile_console_dual_backend() {
    local out="$1" bin="$2"
    ( export SCALAR=1; compile_console "$out" "$bin" )
    stage_binary "$out/$bin" "$out/$bin.scalar"
    ( export HIGHWAY=1; compile_console "$out" "$bin" )
    stage_binary "$out/$bin" "$out/$bin.hwy"
}

# compile_console_wasm OUT BIN — WASM=1 variant of compile_console. Emscripten
# emits BIN.js (node launcher) + BIN.wasm; the caller runs `node OUT/BIN.js`.
compile_console_wasm() {
    local out="$1" bin="$2"
    cmake_build_app "$out" "$bin" ""
    local builddir; builddir="$(_cmake_app_builddir "$out")"
    stage_binary "$builddir/$bin.js" "$out/$bin.js"
    stage_binary "$builddir/$bin.wasm" "$out/$bin.wasm"
}

# _assert_gdext_dest DEST — DEST's basename must be the fixed base name, with or
# without the platform's lib prefix. The three builders below no longer derive
# the CMake target from DEST, so nothing else keeps the two ends in agreement.
_assert_gdext_dest() {
    local b; b="$(basename "$1")"; b="${b#lib}"
    case "$b" in
        "$DN2CPP_GDEXT_LIB".*) ;;
        *) echo "FAIL: GDExtension destination '$1' does not name $DN2CPP_GDEXT_LIB" >&2; exit 1 ;;
    esac
}

# compile_gdextension OUT DYLIB — build OUT/generated*.cpp into the GDExtension
# shared library DYLIB via CMake (links the Godot runtime).
compile_gdextension() {
    local out="$1" dylib="$2"
    _assert_gdext_dest "$dylib"
    cmake_build_app "$out" "$DN2CPP_GDEXT_LIB" gdext
    stage_binary "$(_cmake_app_builddir "$out")/$(lib_name "$DN2CPP_GDEXT_LIB")" "$dylib"
}

# compile_gdextension_ios OUT XCFRAMEWORK — build an iOS GDExtension
# .xcframework: one dylib per SDK (device arm64 + simulator universal), bundled
# by `xcodebuild -create-xcframework` so one `ios.*` .gdextension entry serves
# both. The subshells scope one iOS axis each and drop any the caller exported.
compile_gdextension_ios() {
    local out="$1" xcfw="$2"
    _assert_gdext_dest "$xcfw"
    local base="$DN2CPP_GDEXT_LIB"
    (unset IOS_SIM WASM HIGHWAY SCALAR; export IOS_DEV=1; cmake_build_app "$out" "$base" gdext)
    (unset IOS_DEV WASM HIGHWAY SCALAR; export IOS_SIM=1; cmake_build_app "$out" "$base" gdext)
    local dev_dir sim_dir
    dev_dir="$(unset IOS_SIM WASM HIGHWAY SCALAR; export IOS_DEV=1; _cmake_app_builddir "$out")"
    sim_dir="$(unset IOS_DEV WASM HIGHWAY SCALAR; export IOS_SIM=1; _cmake_app_builddir "$out")"
    rm -rf "$xcfw"
    xcodebuild -create-xcframework \
        -library "$dev_dir/$(lib_name "$base")" \
        -library "$sim_dir/$(lib_name "$base")" \
        -output "$xcfw" >/dev/null
}

# compile_gdextension_android OUT SO — build the Android GDExtension shared
# library SO. The ANDROID axis routes the configure through the NDK toolchain
# file, so the artifact is lib<base>.so whatever the host is (lib_name derives
# the HOST extension and does not apply).
compile_gdextension_android() {
    local out="$1" so="$2"
    _assert_gdext_dest "$so"
    local base="$DN2CPP_GDEXT_LIB"
    (unset IOS_DEV IOS_SIM WASM HIGHWAY SCALAR; export ANDROID=1; cmake_build_app "$out" "$base" gdext)
    local builddir
    builddir="$(unset IOS_DEV IOS_SIM WASM HIGHWAY SCALAR; export ANDROID=1; _cmake_app_builddir "$out")"
    stage_binary "$builddir/lib$base.so" "$so"
}

# compile_shared OUT DYLIB — build OUT/generated*.cpp into the plain shared
# library DYLIB (no Godot runtime): exports the app's [UnmanagedCallersOnly]
# entry points plus the generated `main` (a dlopen host calls it once to
# initialize the runtime + run the static constructors).
compile_shared() {
    local out="$1" dylib="$2"
    local base; base="$(basename "$dylib")"; base="${base#lib}"; base="${base%.*}"
    cmake_build_app "$out" "$base" shared
    stage_binary "$(_cmake_app_builddir "$out")/$(lib_name "$base")" "$dylib"
}

# compile_shared_wasm OUT SO — WASM axis of compile_shared: an Emscripten SIDE
# MODULE (SIDE_MODULE=2) exporting the generated `main`. Artifact is lib<base>.so
# whatever the host is — the CMake target pins PREFIX/SUFFIX so this copy has
# something stable to name.
compile_shared_wasm() {
    local out="$1" so="$2"
    local base; base="$(basename "$so")"; base="${base#lib}"; base="${base%%.*}"
    (unset IOS_DEV IOS_SIM ANDROID HIGHWAY SCALAR; export WASM=1; cmake_build_app "$out" "$base" shared)
    local builddir
    builddir="$(unset IOS_DEV IOS_SIM ANDROID HIGHWAY SCALAR; export WASM=1; _cmake_app_builddir "$out")"
    stage_binary "$builddir/lib$base.so" "$so"
}

# compile_dotnet_module OUT DYLIB — build OUT/generated*.cpp into the Godot
# mono-module shared library DYLIB (links the .NET-module host runtime; exports
# godotsharp_game_main_init).
compile_dotnet_module() {
    local out="$1" dylib="$2"
    local base; base="$(basename "$dylib")"; base="${base#lib}"; base="${base%.*}"
    cmake_build_app "$out" "$base" dotnet
    stage_binary "$(_cmake_app_builddir "$out")/$(lib_name "$base")" "$dylib"
}

# compile_dotnet_module_android OUT SO — ANDROID axis of compile_dotnet_module.
# Artifact is lib<base>.so whatever the host is, and <base> must be the game's
# assembly name exactly: try_load_native_aot_library dlopens the bare soname
# lib<AssemblyName>.so, resolved out of the APK's lib/<abi>/.
compile_dotnet_module_android() {
    local out="$1" so="$2"
    local base; base="$(basename "$so")"; base="${base#lib}"; base="${base%%.*}"
    (unset IOS_DEV IOS_SIM WASM HIGHWAY SCALAR; export ANDROID=1; cmake_build_app "$out" "$base" dotnet)
    local builddir
    builddir="$(unset IOS_DEV IOS_SIM WASM HIGHWAY SCALAR; export ANDROID=1; _cmake_app_builddir "$out")"
    stage_binary "$builddir/lib$base.so" "$so"
}

# compile_dotnet_module_wasm OUT SO — WASM axis of compile_dotnet_module: the
# Emscripten SIDE MODULE the web export preloads and dlopens. Artifact is
# lib<base>.so whatever the host is — the CMake target pins PREFIX/SUFFIX for
# Emscripten precisely so this copy has something stable to name.
compile_dotnet_module_wasm() {
    local out="$1" so="$2"
    local base; base="$(basename "$so")"; base="${base#lib}"; base="${base%%.*}"
    (unset IOS_DEV IOS_SIM ANDROID HIGHWAY SCALAR; export WASM=1; cmake_build_app "$out" "$base" dotnet)
    local builddir
    builddir="$(unset IOS_DEV IOS_SIM ANDROID HIGHWAY SCALAR; export WASM=1; _cmake_app_builddir "$out")"
    stage_binary "$builddir/lib$base.so" "$so"
}

# android_llvm_nm — echo the NDK's llvm-nm (the host `nm` reads Mach-O/PE, not
# Android ELF). The prebuilt dir carries a host tag, so glob it. $EXE_EXT is
# load-bearing on Windows (the file is llvm-nm.exe; without it every Android
# symbol check takes its "no llvm-nm" arm). Empty output when the NDK has none.
android_llvm_nm() {
    local nm
    for nm in "$ANDROID_NDK_ROOT"/toolchains/llvm/prebuilt/*/bin/llvm-nm"$EXE_EXT"; do
        [ -x "$nm" ] && { printf '%s\n' "$nm"; return 0; }
    done
    return 1
}

# android_sdk_root — echo this host's Android SDK root; status 1 and no output
# when it has none. ANDROID_HOME / ANDROID_SDK_ROOT win, then the vendor
# installer's per-OS default — a stock Android Studio sets NEITHER variable, so
# an environment-only probe reads "no SDK" on a provisioned box. The OS-keyed
# default is why this is a function rather than a test at each call site.
android_sdk_root() {
    local cand default
    case "$DN2CPP_OS" in
        macos)   default="$HOME/Library/Android/sdk" ;;
        windows) default="$(cygpath -u "${LOCALAPPDATA:-$HOME/AppData/Local}")/Android/Sdk" ;;
        *)       default="$HOME/Android/Sdk" ;;
    esac
    for cand in "${ANDROID_HOME:-}" "${ANDROID_SDK_ROOT:-}" "$default"; do
        [ -n "$cand" ] || continue
        # The environment's copy is a NATIVE path on Windows; every consumer here
        # is a shell one, so convert on the way out and let a caller that needs
        # the native form convert back.
        if [ "$DN2CPP_OS" = windows ]; then cand="$(cygpath -u "$cand")"; fi
        [ -d "$cand" ] && { printf '%s\n' "$cand"; return 0; }
    done
    return 1
}

# android_ensure_java — prepend a real JDK to PATH when the `java` already there
# is not one, and report whether the result works. Call as a STATEMENT (or an
# `if` condition), never in a command substitution: a subshell's PATH edit is
# discarded. Probing means RUNNING the candidate — macOS ships a /usr/bin/java
# stub that exists and is executable but cannot run. Candidate homes come from
# android_jdk_candidates. The editor's own export/android/java_sdk_path is a
# separate question — android_editor_java_sdk's.
android_ensure_java() {
    java -version >/dev/null 2>&1 && return 0
    local d
    while IFS= read -r d; do
        if "$d/bin/java$EXE_EXT" -version >/dev/null 2>&1; then
            PATH="$d/bin:$PATH"
            return 0
        fi
    done < <(android_jdk_candidates)
    return 1
}

# android_jdk_candidates — print the JDK HOMES (not their bin/) this host is
# likely to have, one per line, most-specific first. MSYS-flavored paths.
# Shared with setup-android-export.sh, which wants the HOME itself (Godot's
# export/android/java_sdk_path appends bin/java) — two lists would drift, and
# the setup aid would then provision nothing on a host the gates call equipped.
android_jdk_candidates() {
    case "$DN2CPP_OS" in
        macos)
            printf '%s\n' "$HOME/Applications/Android Studio.app/Contents/jbr/Contents/Home" \
                          "/Applications/Android Studio.app/Contents/jbr/Contents/Home"
            ;;
        windows)
            printf '%s\n' "$(cygpath -u "${LOCALAPPDATA:-$HOME/AppData/Local}")/Programs/Android Studio/jbr"
            [ -n "${PROGRAMFILES:-}" ] \
                && printf '%s\n' "$(cygpath -u "$PROGRAMFILES")/Android/Android Studio/jbr"
            ;;
        *)
            printf '%s\n' "/opt/android-studio/jbr" "$HOME/android-studio/jbr"
            ;;
    esac
    return 0
}

# godot_editor_settings_file [GODOT_BIN] — echo
# godot_editor_config_dir()/editor_settings-<major>.<minor>.tres. Keyed on
# major.minor ALONE: the editor shares one settings file across patch releases
# and flavors, so one export/android/java_sdk_path serves stock and fork alike.
# Status 1 and no output when the binary's version does not parse.
godot_editor_settings_file() {
    godot_template_version_dir "$@" >/dev/null || return 1
    local num="${GODOT_RELEASE_TAG%-*}" major rest minor
    major="${num%%.*}"
    rest="${num#*.}"
    minor="${rest%%.*}"
    printf '%s\n' "$(godot_editor_config_dir)/editor_settings-${major}.${minor}.tres"
    return 0
}

# android_editor_java_sdk [GODOT_BIN] — echo the JDK the editor's
# `export/android/java_sdk_path` names, if that JDK works; status 1 and no output
# otherwise. The echo is a NATIVE path — `cygpath -u` it before a shell file test.
#
# A SEPARATE question from android_ensure_java's: Godot's Android exporter never
# consults PATH. EditorExportPlatformAndroid::can_export reads this setting and
# refuses the whole preset when it is empty — before any dn2cpp code runs — so a
# box whose PATH java IS the JBR still fails the export. Provision it with
# gates/setup-android-export.sh.
android_editor_java_sdk() {
    local es val probe
    es="$(godot_editor_settings_file "$@")" || return 1
    [ -f "$es" ] || return 1
    # Last assignment wins, matching the tres reader.
    val="$(LC_ALL=C sed -n 's|^export/android/java_sdk_path[[:space:]]*=[[:space:]]*"\(.*\)"[[:space:]]*$|\1|p' "$es" | tail -1)"
    [ -n "$val" ] || return 1
    probe="$val"
    if [ "$DN2CPP_OS" = windows ]; then probe="$(cygpath -u "$val")"; fi
    # Run it, do not merely stat it: Godot itself only tests that bin/java
    # EXISTS, so a stub passes can_export and breaks the signing step instead.
    "$probe/bin/java$EXE_EXT" -version >/dev/null 2>&1 || return 1
    printf '%s\n' "$val"
    return 0
}

# android_ensure_adb — prepend the SDK's platform-tools to PATH when `adb` is not
# already there, so the bare `adb` call sites keep working. Same
# statement-not-substitution rule as android_ensure_java. The SDK installs adb
# but nothing puts it on PATH, so a `command -v adb` test alone answers "no
# device lane" on a fully provisioned box.
android_ensure_adb() {
    command -v adb >/dev/null 2>&1 && return 0
    local sdk
    sdk="$(android_sdk_root)" || return 1
    [ -x "$sdk/platform-tools/adb$EXE_EXT" ] || return 1
    PATH="$sdk/platform-tools:$PATH"
    return 0
}

# android_first_device — echo the serial of the first device adb reports as
# fully booted, or nothing. NEVER fails: a simple assignment carries its
# substitution's exit status, so under `set -e` a box without adb would kill the
# gate instead of recording "no device". The \r strip is not optional either —
# adb on Windows writes CRLF, so the state column reads "device\r".
android_first_device() {
    command -v adb >/dev/null 2>&1 || return 0
    # Drain, then take the first line off the captured string: an `awk … exit`
    # would quit mid-stream, SIGPIPE the `tr` behind it, and pipefail would then
    # report failure on a box that DOES have a device attached.
    local devs
    devs=$(adb devices 2>/dev/null | LC_ALL=C tr -d '\r' | awk 'NR>1 && $2=="device" {print $1}')
    # `if`, not `[ -n … ] &&`: a trailing `&&` that fails becomes the function's
    # status, and this must still return 0 with no device attached.
    if [ -n "$devs" ]; then printf '%s\n' "${devs%%$'\n'*}"; fi
}

# strip_cr_win STR — echo STR with every '\r' removed, on Windows only. Windows
# .NET's Console writes native \r\n, so a NATIVE-vs-live-`dotnet`-oracle diff
# needs nothing. Apply this to the NATIVE side only where the other side is a
# checked-in LF fixture (corelib_freeze_gate) or an inline LF literal
# (corelib_subset_gate, direct assert_output calls) — never to a
# native-vs-oracle diff, where stripping one side introduces a mismatch. On the
# WASM axis it is the ORACLE side that gets stripped (wasm_corelib_diff_gate):
# node never emits \r\n, so on Windows that pair is LF-vs-CRLF the other way.
strip_cr_win() {
    if [ "$DN2CPP_OS" = windows ]; then
        printf '%s' "$1" | LC_ALL=C tr -d '\r'
    else
        printf '%s' "$1"
    fi
}

# strip_cr_win_file FILE — cat FILE with every '\r' removed, on Windows only.
# The file counterpart of strip_cr_win, for a different producer: the
# TRANSPILER'S own text output. AppendLine + Environment.NewLine make every
# generated .cpp/.h and sidecar CRLF on a Windows host, and the self-host
# fixpoint cannot notice (it compares two emits on ONE host), so the COMPARISON
# is what must be told. Same discipline: only where the other side is an LF
# literal or a checked-in LF fixture, never on one side of a two-emit diff.
strip_cr_win_file() {
    if [ "$DN2CPP_OS" = windows ]; then
        LC_ALL=C tr -d '\r' < "$1"
    else
        cat "$1"
    fi
}

# dump_symbols BINARY — print one "<space>T<space><name>" line per external
# symbol the linked BINARY carries (defined, plus undefined on POSIX). POSIX:
# `nm`. Windows: a linked EXE carries no symbol table by default, so this reads
# the linker /MAP CMakeLists.txt emits beside the build tree's binary whenever
# DN2CPP_STRIP=OFF ("Publics by Value" section), and adds back the leading "_"
# Mach-O nm prints so existing "_Brotli"-style anchors stay platform-neutral.
dump_symbols() {
    local bin="$1"
    if [ "$DN2CPP_OS" = windows ]; then
        local map="$(_cmake_app_builddir "$(dirname "$bin")")/$(basename "$bin" .exe).map"
        if [ ! -f "$map" ]; then
            echo "error: dump_symbols: no $map — link with /MAP (pass -DDN2CPP_STRIP=OFF, e.g. via DN2CPP_EXTRA_CMAKE_ARGS)" >&2
            return 1
        fi
        # The "entry point at" trailer follows the Publics section; stopping
        # there also keeps "Static symbols" (internal linkage) out.
        awk '
            /Publics by Value/ { insec = 1; next }
            insec && /^ entry point at/ { exit }
            insec && NF >= 4 { print " T _" $2 }
        ' "$map"
    else
        nm "$bin"
    fi
}

# dump_exports LIB — dump the symbols LIB exports to a dlopen/dlsym consumer, in
# whatever format the platform's tool prints (callers grep for a NAME; the three
# formats share no columns). A DIFFERENT question from dump_symbols: what a
# loader can FIND, not what a linked binary contains.
#   - Mach-O: the export trie. BSD `nm -gU` means "defined only" — NOT GNU's -U,
#     so this spelling cannot be shared with the Linux arm.
#   - ELF: .dynsym, the only table a dlopen consumer resolves through.
#   - PE: the export directory, inside the DLL (no /MAP needed). The `//FLAG`
#     spelling is deliberate — `/EXPORTS` would be mangled into a path by MSYS.
dump_exports() {
    local lib="$1"
    case "$DN2CPP_OS" in
        macos)   nm -gU "$lib" ;;
        windows) dumpbin //NOLOGO //EXPORTS "$(cygpath -w "$lib")" ;;
        *)       nm -D --defined-only "$lib" ;;
    esac
}

# find_generated_objects DIR [PREFIX] — echo, one per line, every object file
# under DIR (recursive: a Ninja+cl build nests them several levels deep) whose
# base name starts with PREFIX (default "generated", the transpiler's own TU
# naming) and ends in .cpp.o (Unix) or .cpp.obj (Windows). A caller after a
# non-generated object passes that file's stem as PREFIX.
find_generated_objects() {
    local dir="$1" prefix="${2:-generated}"
    find "$dir" \( -name "${prefix}*.cpp.o" -o -name "${prefix}*.cpp.obj" \) 2>/dev/null
}

# dump_object_symbols_defined / dump_object_symbols_undefined DIR [PREFIX] —
# print one plain (undecorated) name per line for every DEFINED / UNDEFINED
# external symbol in the objects find_generated_objects finds under DIR. Reads
# OBJECT files, not a linked binary (which is stripped to undefined imports);
# both toolchains print a bare name, so one caller pattern matches either.
#   - non-Windows: nm per object, NEVER batched — nm given several objects at
#     once has, in this exact use, silently resolved a cross-object reference
#     away. Strips the Itanium `_Z<len>` prefix without demangling: callers
#     substring-match, and dropping the digits is what restores a word boundary
#     before `m_`.
#   - Windows: dumpbin //SYMBOLS (an object always has a COFF symbol table).
#     SECT<hex> = defined, UNDEF = undefined external; the plain name comes from
#     the readable signature dumpbin prints in trailing parens. A row with no
#     trailing comment is already plain at x64; one whose comment has no NESTED
#     parens is data, not a function, and is skipped. UNDEF mode also resolves
#     the object's `/alternatename:` directives — see _dump_obj_symbols_win.
dump_object_symbols_defined() { _dump_object_symbols SECT "$1" "${2:-generated}"; }
dump_object_symbols_undefined() { _dump_object_symbols UNDEF "$1" "${2:-generated}"; }

_dump_object_symbols() {
    local kind="$1" dir="$2" prefix="$3" o objs
    objs="$(find_generated_objects "$dir" "$prefix")"
    while IFS= read -r o; do
        [ -n "$o" ] || continue
        if [ "$DN2CPP_OS" = windows ]; then
            _dump_obj_symbols_win "$o" "$kind"
        else
            _dump_obj_symbols_nix "$o" "$kind"
        fi
    done <<< "$objs"
}

# _dump_obj_symbols_win OBJECT SECT|UNDEF — the dumpbin //SYMBOLS side of the
# pair above, for one object file. Row shapes: see dump_object_symbols_defined.
_dump_obj_symbols_win() {
    local obj="$1" kind="$2" want_re
    if [ "$kind" = SECT ]; then
        want_re='^[0-9A-Fa-f]+ [0-9A-Fa-f]+ SECT[0-9A-Fa-f]+ +'
    else
        want_re='^[0-9A-Fa-f]+ [0-9A-Fa-f]+ UNDEF +'
    fi
    dumpbin //NOLOGO //SYMBOLS "$(cygpath -w "$obj")" 2>/dev/null | awk -v want_re="$want_re" '
        {
            bar = index($0, "| ")
            if (bar == 0) next
            left = substr($0, 1, bar - 1)
            if (left !~ want_re) next
            rest = substr($0, bar + 2)
            name = ""
            p = index(rest, " (")
            if (p > 0) {
                # Trailing comment present: only a REAL function shows a nested
                # "(args)" inside it (a data/string comment does not), so that
                # nested paren is the function-vs-not discriminator.
                sig = substr(rest, p + 2)
                sub(/\)[ \t]*$/, "", sig)
                q = index(sig, "(")
                if (q > 0) {
                    n = split(substr(sig, 1, q - 1), toks)
                    if (n > 0) name = toks[n]
                }
            } else {
                # No comment at all: an undecorated extern "C" import — the raw
                # field is already the plain name.
                name = rest
                sub(/[ \t]+$/, "", name)
            }
            # Still-decorated leftovers (an aux `$unwind$?...` row, say) never
            # look like a plain C/C++ identifier — drop them rather than leak
            # a decorated name into a normalized stream.
            if (name != "" && name !~ /[@$?]/) print name
        }
    '
    # UNDEF only: on MSVC a P/Invoke call never names its native entry point —
    # Model.cs emits a `dn2cpp_pinvoke_<entrypoint>_<hash>` wrapper retargeted by
    # `#pragma comment(linker, "/alternatename:<wrapper>=<entrypoint>")`, where
    # clang/gcc instead rename the emitted symbol itself. The alias exists only
    # in .drectve, invisible to //SYMBOLS, so emit its TARGET too and a caller's
    # grep for the plain entry point matches on both toolchains.
    if [ "$kind" = UNDEF ]; then
        dumpbin //NOLOGO //DIRECTIVES "$(cygpath -w "$obj")" 2>/dev/null \
            | LC_ALL=C sed -n 's/^[ \t]*\/alternatename:[^=]*=\(.*\)$/\1/p' \
            | LC_ALL=C tr -d '\r'
    fi
}

# _dump_obj_symbols_nix OBJECT SECT|UNDEF — the nm side of the pair above, for
# one object file.
_dump_obj_symbols_nix() {
    local obj="$1" kind="$2" nmflag
    [ "$kind" = SECT ] && nmflag=--defined-only || nmflag=-u
    # shellcheck disable=SC2086
    # Mach-O prefixes every symbol with an ABI leading underscore (`__Z<len>`);
    # the optional leading `_` tolerates that without breaking the ELF case.
    nm $nmflag "$obj" 2>/dev/null | awk '{print $NF}' | LC_ALL=C sed -E 's/^_?_Z[0-9]+//'
}

# assert_output ACTUAL EXPECTED — print ACTUAL; if it differs from EXPECTED, echo
# the expectation to stderr and fail; otherwise print "OK".
assert_output() {
    local actual="$1" expected="$2"
    echo "$actual"
    if [ "$actual" != "$expected" ]; then
        echo "FAIL: expected:" >&2
        echo "$expected" >&2
        return 1
    fi
    echo "OK"
}

# assert_exit_code ACTUAL EXPECTED — the native binary's exit status must match.
# An output diff cannot stand in: macOS's abort() flushes stdio, so a binary that
# aborts during teardown (a finalizer thread or pool worker touching a lock
# exit() destroyed) still prints everything it wrote.
assert_exit_code() {
    local actual="$1" expected="$2"
    if [ "$actual" != "$expected" ]; then
        echo "FAIL: native exited $actual, expected $expected" >&2
        return 1
    fi
}

# ── Gate result cache ─────────────────────────────────────────────────────────
# Generation is deterministic, so everything after a gate's transpile is a pure
# function of {generated files, runtime/+third_party/ sources, oracle inputs,
# gate script, every gate HELPER, toolchain, axis env}. Hash those right after
# the transpile; on a match skip compile+run ("cached green"). Record the key
# only after every assert passed. Misses are always safe; hits are only as sound
# as the key.
#
#   - A hit does NOT rebuild artifacts/<out>'s binary — never use a gate as the
#     build step for a measure-*.sh run.
#   - A gate that must always execute sets DN2CPP_GATE_CACHE=0.
#
# DN2CPP_GATE_CACHE=0 disables the cache globally. Keys: artifacts/.gate-cache/
# <out-dir slug>.sha (gitignored); the slug keeps multi-call gates and
# DN2CPP_OUT_SUFFIX reusers apart.
#
# Entry = key hash on line 1, status line 2 ("status:green", "status:partial" or
# "status:expected-partial"), reason text from line 3 on. A warm partial hit
# REPLAYS the PARTIAL line, never "cached green". DN2CPP_REQUIRE_ALL=1 refuses a
# cached partial (miss ⇒ live run) but NOT a cached expected-partial. A missing
# status line (pre-status format) reads as a miss once and is rewritten.

# _gate_runtime_hash — content hash over runtime/ + third_party/ (tracked +
# modified + untracked). run-all-gates.sh computes it once and exports
# DN2CPP_RUNTIME_HASH.
_gate_runtime_hash() {
    if [ -n "${DN2CPP_RUNTIME_HASH:-}" ]; then
        printf '%s\n' "$DN2CPP_RUNTIME_HASH"
        return 0
    fi
    git ls-files -co --exclude-standard -- runtime third_party \
        | LC_ALL=C sort \
        | while IFS= read -r f; do [ -f "$f" ] && printf '%s\n' "$f" || true; done \
        | tr '\n' '\0' | xargs -0 shasum -a 256 | shasum -a 256 | awk '{print $1}'
}

# _gate_helpers_hash — the GATE-HELPER term of the key: one hash over EVERY
# helper in gates/ (the `_*.sh` set), named by basename so the term does not move
# with the worktree path.
#
# The whole SET, not the sourced subset: hashing only the sourced ones is
# fail-OPEN — edit gates/_godot_dotnet.sh and its six consumers replay a green
# recorded over the old behaviour, and per-gate wiring is one forgotten argument
# away from the same hole. The over-approximation — editing gates/_godot_fork.sh
# invalidates every cache, including the majority that never source it — is
# accepted: it is fail-CLOSED, and _common.sh already invalidates everything
# anyway. Shadowing `source` was rejected: it would run helper top levels inside
# a function (local scoping) and cannot intercept `.`.
#
# Returns non-zero if the set cannot be read (same contract as
# _gate_surface_lines: caller turns it into a MISS and records no key).
_gate_helpers_hash() {
    local dir listing sums
    dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" 2>/dev/null && pwd -P)" || return 1
    [ -n "$dir" ] || return 1
    # Relative names: hash is a function of content + basename only. The
    # emptiness test is load-bearing — with an empty argument list `xargs shasum`
    # reads STDIN and hangs. This file is always in the set, so an empty listing
    # is a broken read, never legitimate.
    listing=$(cd "$dir" && LC_ALL=C ls -1 _*.sh 2>/dev/null) || return 1
    [ -n "$listing" ] || return 1
    sums=$(cd "$dir" && printf '%s\n' "$listing" | LC_ALL=C sort \
        | tr '\n' '\0' | xargs -0 shasum -a 256) || return 1
    [ -n "$sums" ] || return 1
    printf '%s\n' "$sums" | shasum -a 256 | awk '{print $1}'
}

# _gate_dotnet_hash — hash of the installed shared-runtime set: `dotnet $app`
# resolves by roll-forward, so a new 10.0.x patch changes the oracle without
# touching any other key input. Exported as DN2CPP_DOTNET_RUNTIMES_HASH.
_gate_dotnet_hash() {
    if [ -n "${DN2CPP_DOTNET_RUNTIMES_HASH:-}" ]; then
        printf '%s\n' "$DN2CPP_DOTNET_RUNTIMES_HASH"
        return 0
    fi
    dotnet --list-runtimes 2>/dev/null | shasum -a 256 | awk '{print $1}'
}

# _gate_paths_hash PATH... — content hash over every file under PATHs, minus
# build products (.godot caches, bin/, .cmake* trees, generated nuget.config).
# Used by the Godot gates for project dirs: sources are inputs, gate output is
# not.
_gate_paths_hash() {
    find "$@" -type f \
        ! -path '*/.godot/*' ! -path '*/bin/*' ! -path '*/.cmake*/*' \
        ! -name 'nuget.config' 2>/dev/null \
        | LC_ALL=C sort | tr '\n' '\0' | xargs -0 shasum -a 256 \
        | shasum -a 256 | awk '{print $1}'
}

# _gate_cli_hash — content hash standing in for the TRANSPILER itself. The
# regular key omits it (a changed transpiler moves the generated* surface), but a
# BEHAVIOR gate asserts the transpiler's own conduct and often emits no surface:
# fold `cli:$(_gate_cli_hash)` into its CONTEXT instead.
#
# Two arms matching invoke_cli's: DN2CPP_CLI_DLL set ⇒ hash every assembly beside
# the entry dll; otherwise invoke_cli rebuilds from src/, so hash that closure.
_gate_cli_hash() {
    if [ -n "${DN2CPP_CLI_DLL:-}" ] && [ -f "$DN2CPP_CLI_DLL" ]; then
        find "$(dirname "$DN2CPP_CLI_DLL")" -maxdepth 1 -name '*.dll' -type f \
            | LC_ALL=C sort | tr '\n' '\0' | xargs -0 shasum -a 256 \
            | shasum -a 256 | awk '{print $1}'
    else
        git ls-files -co --exclude-standard -- src \
            | LC_ALL=C sort \
            | while IFS= read -r f; do [ -f "$f" ] && printf '%s\n' "$f" || true; done \
            | tr '\n' '\0' | xargs -0 shasum -a 256 | shasum -a 256 | awk '{print $1}'
    fi
}

# _gate_surface_lines OUT — the TRANSPILE-SURFACE term of the key: one
# `shasum -a 256` line per generated* TU/header and per sidecar CMake consumes,
# named relative to OUT, on stdout. The term the cache rests on — lose it and two
# runs with different C++ key the same. Three outcomes stay separate:
#
#   - files matched     → their shasum lines;
#   - nothing matched   → the marker line `no-generated`, a legitimate state
#                         (e.g. the editor-export gates) that must be ENCODED,
#                         since the empty string is also a broken read;
#   - could not be read → non-zero return, printing nothing. Deliberately NOT a
#                         marker: a shared error constant is the very collision
#                         this function prevents. The caller makes it a MISS and
#                         records no key.
_gate_surface_lines() {
    local out="$1"
    local listing matched sums st
    # `cd ""` succeeds and stays put, so the emptiness test is load-bearing: a
    # missing OUT argument would otherwise list the repository root.
    [ -n "$out" ] && [ -d "$out" ] || return 1
    listing=$(cd "$out" 2>/dev/null && ls -1 2>/dev/null) || return 1
    st=0
    matched=$(printf '%s\n' "$listing" \
        | LC_ALL=C sort \
        | grep -E '^(generated|pinvoke-libs\.txt$|base-abi\.json$)') || st=$?
    # grep's contract: 1 is "no match", anything else is a real failure.
    [ "$st" -eq 1 ] && { printf 'no-generated\n'; return 0; }
    [ "$st" -eq 0 ] || return 1
    sums=$(cd "$out" && printf '%s\n' "$matched" | tr '\n' '\0' | xargs -0 shasum -a 256) || return 1
    # Files were listed, so lines are owed; none means shasum read nothing.
    [ -n "$sums" ] || return 1
    printf '%s\n' "$sums"
    return 0
}

# gate_cache_check OUT CONTEXT [FILE_OR_DIR...] — key the gate step that just
# transpiled into OUT and compare against the recorded last-green key. Returns 0
# on a hit (caller prints gate_cache_hit_msg); on a miss leaves the key in
# _GATE_CACHE_{FILE,HASH} for gate_cache_commit. CONTEXT is a free-form
# discriminator (helper name, argv, corelib path, inline expected text, engine
# version); extra args are content inputs — a directory goes through
# _gate_paths_hash, an absent path is keyed as absent, not an error.
# Call right after the transpile, and put the native link BELOW the check: that
# is exactly the work a warm hit exists to skip.
gate_cache_check() {
    _GATE_CACHE_FILE=""
    _GATE_CACHE_HASH=""
    _GATE_CACHE_HIT_PARTIAL=""
    _GATE_CACHE_HIT_EXPECTED_PARTIAL=""
    [ "${DN2CPP_GATE_CACHE:-1}" = "0" ] && return 1
    local out="$1" context="$2"; shift 2
    local slug; slug=$(printf '%s' "$out" | tr '/' '-')
    local keyfile="artifacts/.gate-cache/$slug.sha"
    # DN2CPP_GATE_CACHE_DEBUG=1 keeps the raw key material at <slug>.material so
    # an unexpected miss can be diagnosed by diffing two runs' materials.
    local material="/dev/null"
    [ "${DN2CPP_GATE_CACHE_DEBUG:-0}" = "1" ] && material="$keyfile.material" && mkdir -p "$(dirname "$keyfile")"
    # Computed HERE, ahead of the key material: an unreadable surface must abort
    # the check, and inside the material's brace group there is nothing to abort
    # to. _GATE_CACHE_FILE is still empty, so gate_cache_commit is a no-op too.
    local surface
    if ! surface=$(_gate_surface_lines "$out"); then
        gate_warn "gate cache off for this step: transpile surface unreadable in '$out' (absent, unreadable or unlistable); running live, recording no key"
        return 1
    fi
    # Same treatment, same reason, for the gate-helper set.
    local helpers
    if ! helpers=$(_gate_helpers_hash); then
        gate_warn "gate cache off for this step: gate helper set (gates/_*.sh) unreadable; running live, recording no key"
        return 1
    fi
    local f key
    key=$(
        {
            printf 'context:%s\n' "$context"
            # Every env var that selects a build axis MUST appear here: add an
            # axis (ensure_cmake_runtime, _cmake_app_builddir) ⇒ add it here.
            printf 'env:%s|%s|%s|%s|%s|%s|%s|%s|%s|%s|%s|%s|%s|%s|%s|%s|%s|%s|%s|%s|%s|%s|%s\n' \
                "${CONFIG:-}" "${TFM:-}" "${SCALAR:-}" "${HIGHWAY:-}" "${WASM:-}" \
                "${IOS_SIM:-}" "${IOS_DEV:-}" "${ANDROID:-}" "${DN2CPP_NO_GC:-}" \
                "${DN2CPP_GC_BACKEND:-}" \
                "${DN2CPP_NO_CURL:-}" "${PAL_REFERENCE:-}" \
                "${DN2CPP_SPLIT_BYTES:-}" "${DN2CPP_OUT_SUFFIX:-}" \
                "${CMAKE_CXX_COMPILER:-}" "${DN2CPP_EXTRA_CMAKE_ARGS:-}" \
                "${DN2CPP_EXTRA_LINK_FLAGS:-}" "${DN2CPP_EXTRA_LINK_LIBS:-}" \
                "${DN2CPP_HIGHWAY_ARCH:-}" \
                "${IOS_DEPLOYMENT_TARGET:-}" "${LANG:-}" "${LC_ALL:-}" "${TZ:-}"
            printf 'os:%s\n' "$(uname -sm)"
            printf 'cc:%s\n' "$(first_line "$(cc --version 2>/dev/null)")"
            if [ -n "${WASM:-}" ]; then
                printf 'emcc:%s node:%s\n' \
                    "$(first_line "$(emcc --version 2>/dev/null)")" "$(node --version 2>/dev/null)"
            fi
            # WHICH Emscripten SDK dn2cpp_emsdk_resolve picked, for the gates that
            # call it — the version above cannot tell two of one version apart, and
            # they carry different baked caches. Absent when the gate resolves none.
            if [ -n "${_GATE_EMSDK_CTX:-}" ]; then
                printf 'emsdk:%s\n' "$_GATE_EMSDK_CTX"
            fi
            if [ -n "${IOS_SIM:-}" ] || [ -n "${IOS_DEV:-}" ]; then
                printf 'ios-sdk:%s\n' "$(xcrun --sdk iphonesimulator --show-sdk-version 2>/dev/null)"
            fi
            printf 'runtime-tree:%s\n' "$(_gate_runtime_hash)"
            printf 'dotnet-runtimes:%s\n' "$(_gate_dotnet_hash)"
            shasum -a 256 "$DN2CPP_GATE_SCRIPT"
            # Every gate helper, not just the sourced ones — see
            # _gate_helpers_hash.
            printf 'helpers:%s\n' "$helpers"
            # Whole output surface: shasum lines carry file names, so
            # appearing/disappearing files move the key; or `no-generated`.
            printf '%s\n' "$surface"
            for f in "$@"; do
                if [ -d "$f" ]; then printf 'dir:%s:%s\n' "$f" "$(_gate_paths_hash "$f")"
                elif [ -f "$f" ]; then shasum -a 256 "$f"
                else printf 'absent:%s\n' "$f"; fi
            done
        } | tee "$material" | shasum -a 256 | awk '{print $1}'
    )
    _GATE_CACHE_FILE="$keyfile"
    _GATE_CACHE_HASH="$key"
    [ -f "$keyfile" ] || return 1
    [ "$(sed -n '1p' "$keyfile" 2>/dev/null)" = "$key" ] || return 1
    case "$(sed -n '2p' "$keyfile")" in
        status:green) return 0 ;;
        status:partial)
            # REQUIRE_ALL: never serve a cached partial — MISS, so it runs live.
            [ "${DN2CPP_REQUIRE_ALL:-0}" = "1" ] && return 1
            # Otherwise replay the recorded PARTIAL.
            printf 'PARTIAL: %s\n' "$(sed -n '3,$p' "$keyfile")"
            _GATE_CACHE_HIT_PARTIAL=1
            return 0 ;;
        status:expected-partial)
            # A DECLARED permanent partial: served warm even under REQUIRE_ALL
            # (a live re-run only hits the same structural wall). The
            # declaration is replayed, so the log still names the hole.
            printf 'EXPECTED PARTIAL: %s\n' "$(sed -n '3,$p' "$keyfile")"
            _GATE_CACHE_HIT_EXPECTED_PARTIAL=1
            return 0 ;;
        *) return 1 ;;   # no status line (pre-status entry) or corrupt — one-time invalidation
    esac
}

# gate_cache_hit_msg — one-line success marker for a hit. Runner contract: it
# matches "cached green" / "cached partial" to tag the gate, so a partial hit
# must say "cached partial", never "cached green", and must not start with SKIP.
gate_cache_hit_msg() {
    if [ -n "${_GATE_CACHE_HIT_EXPECTED_PARTIAL:-}" ]; then
        echo "OK (cached green: inputs unchanged since this gate's last pass, which carried the declared expected-partial section above; DN2CPP_GATE_CACHE=0 forces a run)"
    elif [ -n "${_GATE_CACHE_HIT_PARTIAL:-}" ]; then
        echo "OK (cached partial: inputs unchanged since this gate's last run, which passed with the section above skipped; DN2CPP_GATE_CACHE=0 forces a run; DN2CPP_REQUIRE_ALL=1 refuses cached partials)"
    else
        echo "OK (cached green: inputs unchanged since this gate's last pass; DN2CPP_GATE_CACHE=0 forces a run)"
    fi
}

# gate_cache_commit — record the preceding gate_cache_check's key as green, or
# as partial with the accumulated reasons. Call only after every assert passed;
# no-op when the check never ran or the cache is off. tmp+mv: no torn read.
gate_cache_commit() {
    [ -n "${_GATE_CACHE_FILE:-}" ] || return 0
    mkdir -p "$(dirname "$_GATE_CACHE_FILE")"
    {
        printf '%s\n' "$_GATE_CACHE_HASH"
        if [ -n "${_GATE_PARTIAL:-}" ]; then
            printf 'status:partial\n'
            printf '%s\n' "$_GATE_PARTIAL_REASON"
        elif [ -n "${_GATE_EXPECTED_PARTIAL:-}" ]; then
            # Order matters: an ordinary partial (teeth) outranks a declaration.
            printf 'status:expected-partial\n'
            printf '%s\n' "$_GATE_EXPECTED_PARTIAL_REASON"
        else
            printf 'status:green\n'
        fi
    } >"$_GATE_CACHE_FILE.tmp.$$"
    mv -f "$_GATE_CACHE_FILE.tmp.$$" "$_GATE_CACHE_FILE"
    _GATE_CACHE_FILE=""
    _GATE_CACHE_HASH=""
}

# ── Optional wrapper features (run-args, scratch dirs, extra key inputs) ──────
# Opt-in knobs every corelib_* wrapper honors. All OFF by default and each
# contributes NOTHING to the key when off, so legacy keys stay byte-identical.
#
#   DN2CPP_GATE_RUN_ARGS — argv for the RUN step, applied identically to the
#       native binary and the oracle so the two stay an exact diff. Shell-quoted
#       (eval): DN2CPP_GATE_RUN_ARGS='alpha "beta gamma" 42'. The token
#       @SCRATCH@ expands per side to a FRESH mktemp dir (removed after the
#       runs). The RAW string enters the cache context; expanded paths do not.
#   DN2CPP_GATE_EXTRA_INPUTS — space-separated extra files/dirs added to
#       gate_cache_check's key inputs, for run inputs the transpile surface
#       cannot see (a fixture the binary reads). No spaces in paths.
#   DN2CPP_GATE_EXTRA_CONTEXT — extra cache-CONTEXT discriminator for an axis a
#       gate_extra_asserts body depends on that nothing else keys — e.g. an
#       assert that runs the CLI again, which a transpiler change can break with
#       the first transpile's output byte-identical. Such a gate sets
#       "<what>|cli:$(_gate_cli_hash)" before calling its wrapper.
#   gate_extra_asserts — optional bash function; if the sourcing gate defines it,
#       every _corelib_gate_check wrapper (subset / diff / diff_split / freeze)
#       calls it with OUT as $1 after its own asserts, INSIDE the cached region.
#       All four honor it because a hook only some wrappers respect fails
#       silently. (The wasm/ios-sim wrappers call gate_cache_check directly and
#       decline these features.)
_GATE_SCRATCH_DIRS=()

# _gate_extra_asserts OUT — run the sourcing gate's optional extra asserts.
# Called by each _corelib_gate_check user right before its gate_cache_commit.
_gate_extra_asserts() {
    if declare -F gate_extra_asserts >/dev/null; then
        gate_extra_asserts "$1"
    fi
}

# _gate_run_argv — parse DN2CPP_GATE_RUN_ARGS into the global _GATE_RUN_ARGV,
# expanding each @SCRATCH@ to a fresh mktemp dir. Call once per SIDE (native /
# oracle); each call mints new scratch dirs.
#
# A parse failure is LOUD (exit 1), never a silent empty argv: callers run it
# inside `set +e`, and both sides going argless is a green diff.
_gate_run_argv() {
    _GATE_RUN_ARGV=()
    [ -n "${DN2CPP_GATE_RUN_ARGS:-}" ] || return 0
    local parsed=() a d
    if ! eval "parsed=( $DN2CPP_GATE_RUN_ARGS )" 2>/dev/null; then
        echo "FAIL: DN2CPP_GATE_RUN_ARGS unparsable as shell-quoted argv: $DN2CPP_GATE_RUN_ARGS" >&2
        exit 1
    fi
    for a in ${parsed[@]+"${parsed[@]}"}; do
        if [ "$a" = "@SCRATCH@" ]; then
            d=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp_gate_scratch.XXXXXX")
            _GATE_SCRATCH_DIRS+=("$d")
            a="$d"
        fi
        _GATE_RUN_ARGV+=("$a")
    done
}

# _gate_scratch_cleanup — remove every scratch dir minted since the last call.
# Called right after the runs, not via an EXIT trap (which would clobber the
# sourcing gate's own trap); a death mid-assert leaks one $TMPDIR dir.
_gate_scratch_cleanup() {
    rm -rf ${_GATE_SCRATCH_DIRS[@]+"${_GATE_SCRATCH_DIRS[@]}"}
    _GATE_SCRATCH_DIRS=()
}

# _gate_ctx_extras — echo the cache-context suffix for the opt-in features;
# empty when none is set, keeping legacy keys byte-identical.
_gate_ctx_extras() {
    local extra=""
    [ -n "${DN2CPP_GATE_RUN_ARGS:-}" ] && extra="$extra|runargs:$DN2CPP_GATE_RUN_ARGS"
    [ -n "${DN2CPP_GATE_EXTRA_CONTEXT:-}" ] && extra="$extra|xctx:$DN2CPP_GATE_EXTRA_CONTEXT"
    printf '%s' "$extra"
}

# _gate_extra_inputs — echo DN2CPP_GATE_EXTRA_INPUTS one path per line, for
# wrappers to append to their gate_cache_check arguments.
_gate_extra_inputs() {
    [ -n "${DN2CPP_GATE_EXTRA_INPUTS:-}" ] || return 0
    local f
    for f in $DN2CPP_GATE_EXTRA_INPUTS; do printf '%s\n' "$f"; done
}

# ── The corelib wrapper family ────────────────────────────────────────────────
# Six wrappers over one 4-step skeleton (steps 1–3 = _corelib_gate_core); each
# keeps its own step 4 and cache CONTEXT. Every step-4 run uses run_bounded.

# _corelib_gate_out PROJECT — default OUT artifacts/<lowercased PROJECT>.
_corelib_gate_out() {
    printf 'artifacts/%s\n' "$(printf '%s' "$1" | tr '[:upper:]' '[:lower:]')"
}

# _corelib_gate_core PROJECT OUT [EXTRA_BCL_NAME...] — steps 1/4–3/4; extras are
# BCL simple names beside CoreLib (absent = hard error). Sets _CG_CORELIB, _CG_APP,
# _CG_OUT. Assert on _CG_OUT: re-deriving _corelib_gate_out gives the DEFAULT dir,
# so on a non-default axis the asserts read another build and pass.
_corelib_gate_core() {
    local project="$1" out="$2"; shift 2
    _CG_OUT="$out"

    echo "== 1/4 Locating the real CoreLib =="
    local bcl
    _CG_CORELIB=$(locate_corelib)
    bcl=$(dirname "$_CG_CORELIB")
    echo "corlib: $_CG_CORELIB"

    echo "== 2/4 Building app assembly =="
    build_proj "samples/dotnet/$project/$project.csproj"
    _CG_APP="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"

    echo "== 3/4 Transpiling app + real CoreLib (tree-shaken) =="
    # Hard error, not a skip: the CONTEXT carries the REQUESTED names, so a
    # narrowed program's green would replay under the resolved run's key.
    local refs=(-r "$_CG_CORELIB") name
    for name in "$@"; do
        [ -f "$bcl/$name.dll" ] \
            || { echo "error: requested reference $name not found beside the CoreLib: $bcl/$name.dll" >&2; return 1; }
        refs+=(-r "$bcl/$name.dll")
    done
    invoke_cli "$_CG_APP" "${refs[@]}" -o "$out"
}

# _corelib_gate_check OUT CONTEXT [EXTRA_KEY_INPUT...] — gate_cache_check over the
# standard key inputs, the wrapper's extras, then DN2CPP_GATE_EXTRA_INPUTS; the
# order is key material. wasm/ios-sim call gate_cache_check directly (frozen keys).
_corelib_gate_check() {
    local out="$1" ctx="$2"; shift 2
    local extra_inputs=() f
    while IFS= read -r f; do extra_inputs+=("$f"); done < <(_gate_extra_inputs)
    gate_cache_check "$out" "$ctx" \
        "$_CG_APP" "${_CG_APP%.dll}.runtimeconfig.json" "${_CG_APP%.dll}.deps.json" \
        "$@" ${extra_inputs[@]+"${extra_inputs[@]}"}
}

# corelib_subset_gate PROJECT [EXPECTED] [EXTRA_BCL_NAME...] — transpile against
# the tree-shaken real CoreLib, compile, run. EXPECTED is asserted when non-empty,
# else the output streams. Honors DN2CPP_GATE_RUN_ARGS.
corelib_subset_gate() {
    local project="$1" expected="${2-}"
    [ $# -ge 1 ] && shift
    [ $# -ge 1 ] && shift
    local out
    out="$(_corelib_gate_out "$project")"

    _corelib_gate_core "$project" "$out" "$@"
    # Byte-identical to the pre-EXTRA_BCL format when unused, so legacy keys hit.
    local ctx="corelib_subset_gate|$project|$_CG_CORELIB|$expected"
    [ $# -gt 0 ] && ctx="$ctx|extra-bcl:$*"
    ctx="$ctx$(_gate_ctx_extras)"
    if _corelib_gate_check "$out" "$ctx"; then
        gate_cache_hit_msg
        return 0
    fi

    echo "== 4/4 Compiling C++ and running =="
    compile_console "$out" "$project"
    _gate_run_argv
    if [ -n "$expected" ]; then
        # Capture the status explicitly: inline `$(...)` swallows it, so an abort
        # after the full output would diff green.
        local native native_code
        set +e
        native=$(run_bounded "./$out/$project" ${_GATE_RUN_ARGV[@]+"${_GATE_RUN_ARGV[@]}"}); native_code=$?
        set -e
        _gate_scratch_cleanup
        assert_output "$(strip_cr_win "$native")" "$expected"
        assert_exit_code "$native_code" 0
    else
        run_bounded "./$out/$project" ${_GATE_RUN_ARGV[@]+"${_GATE_RUN_ARGV[@]}"}
        _gate_scratch_cleanup
    fi
    _gate_extra_asserts "$out"
    gate_cache_commit
}

# corelib_diff_gate PROJECT — corelib_subset_gate oracled by the program's own
# real-.NET output (`dotnet $app`). Deterministic programs only.
corelib_diff_gate() {
    local project="$1"; shift
    local out
    out="$(_corelib_gate_out "$project")"
    [ -n "${HIGHWAY:-}" ] && out="$out-hwy"
    [ -n "${SCALAR:-}" ] && out="$out-scalar"
    # A second gate on the *same* project sets DN2CPP_OUT_SUFFIX to claim its own
    # artifacts dir + build tree; sharing one races under the parallel runner.
    [ -n "${DN2CPP_OUT_SUFFIX:-}" ] && out="$out$DN2CPP_OUT_SUFFIX"

    _corelib_gate_core "$project" "$out" "$@"
    if _corelib_gate_check "$out" "corelib_diff_gate|$project|$*|$_CG_CORELIB$(_gate_ctx_extras)"; then
        gate_cache_hit_msg
        return 0
    fi

    echo "== 4/4 Compiling C++ and running (exact diff vs real .NET) =="
    compile_console "$out" "$project"
    # Real .NET is the oracle for the exit status too (Finalizers' last section
    # aborts deliberately). DN2CPP_GATE_RUN_ARGS: fresh @SCRATCH@ per side.
    local native native_code expected expected_code
    set +e
    _gate_run_argv
    native=$(run_bounded "./$out/$project" ${_GATE_RUN_ARGV[@]+"${_GATE_RUN_ARGV[@]}"}); native_code=$?
    _gate_run_argv
    expected=$(run_bounded dotnet "$_CG_APP" ${_GATE_RUN_ARGV[@]+"${_GATE_RUN_ARGV[@]}"}); expected_code=$?
    set -e
    _gate_scratch_cleanup
    assert_output "$native" "$expected"
    assert_exit_code "$native_code" "$expected_code"
    _gate_extra_asserts "$out"
    gate_cache_commit
}

# corelib_diff_split_gate PROJECT [EXTRA_BCL_NAME...] — corelib_diff_gate diffing
# stdout and stderr SEPARATELY (a leak between streams must fail); statuses too.
corelib_diff_split_gate() {
    local project="$1"; shift
    local out
    out="$(_corelib_gate_out "$project")"
    [ -n "${DN2CPP_OUT_SUFFIX:-}" ] && out="$out$DN2CPP_OUT_SUFFIX"

    _corelib_gate_core "$project" "$out" "$@"
    if _corelib_gate_check "$out" "corelib_diff_split_gate|$project|$*|$_CG_CORELIB$(_gate_ctx_extras)"; then
        gate_cache_hit_msg
        return 0
    fi

    echo "== 4/4 Compiling C++ and running (stdout/stderr exact diff vs real .NET) =="
    compile_console "$out" "$project"
    local native_code expected_code
    set +e
    _gate_run_argv
    run_bounded "./$out/$project" ${_GATE_RUN_ARGV[@]+"${_GATE_RUN_ARGV[@]}"} \
        >"$out/native.out" 2>"$out/native.err"; native_code=$?
    _gate_run_argv
    run_bounded dotnet "$_CG_APP" ${_GATE_RUN_ARGV[@]+"${_GATE_RUN_ARGV[@]}"} \
        >"$out/expected.out" 2>"$out/expected.err"; expected_code=$?
    set -e
    _gate_scratch_cleanup

    echo "---- native STDOUT ----"; cat "$out/native.out"
    echo "---- native STDERR ----"; cat "$out/native.err"
    local ok=1
    if ! diff -u "$out/expected.out" "$out/native.out"; then
        echo "FAIL: STDOUT differs from real .NET" >&2
        ok=0
    fi
    if ! diff -u "$out/expected.err" "$out/native.err"; then
        echo "FAIL: STDERR differs from real .NET" >&2
        ok=0
    fi
    [ "$ok" -eq 1 ] || return 1
    assert_exit_code "$native_code" "$expected_code"
    # Extra asserts run INSIDE the cached region, so a warm hit includes them.
    _gate_extra_asserts "$out"
    gate_cache_commit
    echo "OK"
}

# wasm_corelib_diff_gate PROJECT [EXTRA_BCL_NAME...] — WASM axis of
# corelib_diff_gate (caller exports WASM=1): same transpile, built by emcmake/em++,
# run under node (Boehm GC, MEMFS), diffed vs real .NET. -wasm OUT dir.
wasm_corelib_diff_gate() {
    local project="$1"; shift
    local out
    out="$(_corelib_gate_out "$project")-wasm"

    _corelib_gate_core "$project" "$out" "$@"
    if gate_cache_check "$out" "wasm_corelib_diff_gate|$project|$*|$_CG_CORELIB" \
            "$_CG_APP" "${_CG_APP%.dll}.runtimeconfig.json" "${_CG_APP%.dll}.deps.json"; then
        gate_cache_hit_msg
        return 0
    fi

    echo "== 4/4 Compiling to wasm and running under node (exact diff vs real .NET) =="
    compile_console_wasm "$out" "$project"
    # Exact code equality cannot hold across runtimes (node reports an abort as 1,
    # .NET as 134): the sides must only FAIL ALIKE, both 0 or both nonzero.
    local native native_code expected expected_code
    set +e
    native=$(run_bounded node "./$out/$project.js"); native_code=$?
    expected=$(run_bounded dotnet "$_CG_APP"); expected_code=$?
    set -e
    # strip_cr_win on the ORACLE side: node never emits \r\n, the oracle does.
    assert_output "$native" "$(strip_cr_win "$expected")"
    if [ "$native_code" -eq 0 ] || [ "$expected_code" -eq 0 ]; then
        assert_exit_code "$native_code" "$expected_code"
    fi
    gate_cache_commit
}

# ios_sim_corelib_diff_gate PROJECT [EXTRA_BCL_NAME...] — iOS-simulator axis of
# corelib_diff_gate (caller exports IOS_SIM=1 and IOS_SIM_UDID): built against the
# iphonesimulator SDK, run via `simctl spawn`, diffed vs real .NET. -ios-sim OUT.
ios_sim_corelib_diff_gate() {
    local project="$1"; shift
    local out
    out="$(_corelib_gate_out "$project")-ios-sim"

    _corelib_gate_core "$project" "$out" "$@"
    # IOS_SIM_UDID stays out of the key: the device does not change the binary.
    if gate_cache_check "$out" "ios_sim_corelib_diff_gate|$project|$*|$_CG_CORELIB" \
            "$_CG_APP" "${_CG_APP%.dll}.runtimeconfig.json" "${_CG_APP%.dll}.deps.json"; then
        gate_cache_hit_msg
        return 0
    fi

    echo "== 4/4 Compiling for the iOS simulator and running via simctl spawn (exact diff vs real .NET) =="
    compile_console "$out" "$project"
    # simctl spawn relays the app's status faithfully, so real .NET oracles it too.
    # Only the spawn takes sim_lock (a machine-wide singleton) — never widen
    # it over the compile or the oracle.
    local native native_code expected expected_code
    sim_lock
    set +e
    native=$(run_bounded xcrun simctl spawn "$IOS_SIM_UDID" "$PWD/$out/$project"); native_code=$?
    set -e
    sim_unlock
    set +e
    expected=$(run_bounded dotnet "$_CG_APP"); expected_code=$?
    set -e
    assert_output "$native" "$expected"
    assert_exit_code "$native_code" "$expected_code"
    gate_cache_commit
}

# net10_bcl_diff_gate [OPTION...] PROJECT BCL_ASSEMBLY_NAME... — corelib_diff_gate
# pinned to net10.0 (resolve_net10_corelib) + the named real BCL assemblies and
# --auto-ref. Options may appear anywhere among the positionals; with none passed,
# argv/OUT/CONTEXT are byte-identical to the pre-option form.
#   --cli-arg TOKEN     append TOKEN to the CLI argv before --auto-ref (repeatable)
#   --out-suffix S      append S to OUT; two arms of one gate MUST NOT share an OUT
#   --keep-symbols      -DDN2CPP_STRIP=OFF -DDN2CPP_DEAD_STRIP=OFF, for a
#                       --post-assert that reads the symbol table
#   --post-assert FN    after the diff, call FN with (OUT, PROJECT); miss only
# All but --out-suffix fold into the CONTEXT (the last two change only what happens
# after the transpile the key hashes); --out-suffix already moves the key FILE.
net10_bcl_diff_gate() {
    local cli_args=() post_assert="" out_suffix="" keep_symbols=0 positional=()
    while [ $# -gt 0 ]; do
        case "$1" in
            --cli-arg)      cli_args+=("$2"); shift 2 ;;
            --out-suffix)   out_suffix="$2"; shift 2 ;;
            --post-assert)  post_assert="$2"; shift 2 ;;
            --keep-symbols) keep_symbols=1; shift ;;
            *)              positional+=("$1"); shift ;;
        esac
    done
    set -- ${positional[@]+"${positional[@]}"}

    local project="$1"; shift
    local out
    out="artifacts/$(printf '%s' "$project" | tr '[:upper:]' '[:lower:]')$out_suffix"
    [ -n "${HIGHWAY:-}" ] && out="$out-hwy"
    [ -n "${SCALAR:-}" ] && out="$out-scalar"
    # Publish the OUT actually written; no _corelib_gate_core here.
    _CG_OUT="$out"

    echo "== 1/4 Locating the real net10.0 CoreLib + $* =="
    local corelib bcl name real_asm refs=()
    corelib=$(resolve_net10_corelib)
    bcl=$(dirname "$corelib")
    echo "corlib:   $corelib"
    for name in "$@"; do
        real_asm="$bcl/$name.dll"
        [ -f "$real_asm" ] || { echo "error: real $name not found: $real_asm" >&2; return 1; }
        echo "real bcl: $real_asm"
        refs+=(-r "$real_asm")
    done

    echo "== 2/4 Building app assembly =="
    build_proj "samples/dotnet/$project/$project.csproj"
    local app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"
    [ -f "$app" ] || { echo "error: not built: $app" >&2; return 1; }

    echo "== 3/4 Transpiling app + real CoreLib + $* (--auto-ref, tree-shaken) =="
    invoke_cli "$app" -r "$corelib" "${refs[@]}" \
        ${cli_args[@]+"${cli_args[@]}"} --auto-ref -o "$out"
    # Empty when no option was passed, keeping legacy keys byte-identical.
    local ctx_extra=""
    [ ${#cli_args[@]} -gt 0 ] && ctx_extra="$ctx_extra|cliargs:${cli_args[*]}"
    [ "$keep_symbols" = 1 ] && ctx_extra="$ctx_extra|keepsyms"
    [ -n "$post_assert" ] && ctx_extra="$ctx_extra|postassert:$post_assert"
    if gate_cache_check "$out" "net10_bcl_diff_gate|$project|$*|$corelib$ctx_extra" \
            "$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
        gate_cache_hit_msg
        return 0
    fi

    echo "== 4/4 Compiling C++ and running (exact diff vs real .NET) =="
    if [ "$keep_symbols" = 1 ]; then
        ( export DN2CPP_EXTRA_CMAKE_ARGS="${DN2CPP_EXTRA_CMAKE_ARGS:-} -DDN2CPP_STRIP=OFF -DDN2CPP_DEAD_STRIP=OFF"
          compile_console "$out" "$project" )
    else
        compile_console "$out" "$project"
    fi
    # Capture BOTH statuses: an abort after the full output diffs green, and so do
    # two empty strings from a crashed ORACLE (a nonzero oracle is a broken input).
    local native_out native_code oracle_out oracle_code
    set +e
    native_out="$(run_bounded "./$out/$project")"; native_code=$?
    oracle_out="$(run_bounded dotnet "$app")"; oracle_code=$?
    set -e
    [ "$native_code" -eq 0 ] \
        || { echo "FAIL: native binary exited $native_code, expected 0" >&2; return 1; }
    [ "$oracle_code" -eq 0 ] \
        || { echo "FAIL: real-.NET oracle \`dotnet $app\` exited $oracle_code, expected 0" >&2; return 1; }
    assert_output "$native_out" "$oracle_out"
    [ -n "$post_assert" ] && { "$post_assert" "$out" "$project" || return 1; }
    gate_cache_commit
}

# net10_json_diff_gate PROJECT — net10_bcl_diff_gate for real System.Text.Json.
net10_json_diff_gate() {
    net10_bcl_diff_gate "$1" System.Text.Json
}

# corelib_freeze_gate PROJECT EXPECTED_FILE [EXTRA_BCL_NAME...] — subset gate
# against a frozen snapshot, for programs that intentionally diverge from real
# .NET (so `dotnet $app` is not a valid oracle).
corelib_freeze_gate() {
    local project="$1" expfile="$2"; shift 2
    local out
    out="$(_corelib_gate_out "$project")"
    [ -n "${HIGHWAY:-}" ] && out="$out-hwy"
    [ -n "${SCALAR:-}" ] && out="$out-scalar"

    _corelib_gate_core "$project" "$out" "$@"
    if _corelib_gate_check "$out" "corelib_freeze_gate|$project|$*|$_CG_CORELIB$(_gate_ctx_extras)" \
            "$expfile"; then
        gate_cache_hit_msg
        return 0
    fi

    echo "== 4/4 Compiling C++ and running (diff vs frozen snapshot) =="
    compile_console "$out" "$project"
    local native native_code
    set +e
    _gate_run_argv
    native=$(run_bounded "./$out/$project" ${_GATE_RUN_ARGV[@]+"${_GATE_RUN_ARGV[@]}"}); native_code=$?
    set -e
    _gate_scratch_cleanup
    assert_output "$(strip_cr_win "$native")" "$(cat "$expfile")"
    assert_exit_code "$native_code" 0
    _gate_extra_asserts "$out"
    gate_cache_commit
}

# xasm_gate PROJECT LIBDLL OUT — cross-assembly gate: build PROJECT (which also
# emits LIBDLL), transpile app + lib together, compile and run.
xasm_gate() {
    local project="$1" libdll="$2" out="$3"

    echo "== 1/3 Building app + library assemblies =="
    build_proj "samples/dotnet/$project/$project.csproj"
    local app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"
    local lib="samples/dotnet/$project/bin/$CONFIG/$TFM/$libdll"

    echo "== 2/3 Transpiling app + library together =="
    invoke_cli "$app" -r "$lib" -o "$out"
    if gate_cache_check "$out" "xasm_gate|$project|$libdll|$out" \
            "$app" "$lib" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
        gate_cache_hit_msg
        return 0
    fi

    echo "== 3/3 Compiling C++ and running =="
    compile_console "$out" "$project"
    run_bounded "./$out/$project"
    gate_cache_commit
}

# ── Watchdogs ─────────────────────────────────────────────────────────────────
# kill_tree PID — SIGKILL PID and every descendant, deepest first. STOP each node
# before enumerating its children so it cannot fork into the scan/kill window;
# only the pgrep route does, since under MSYS a stop is visible to the caller's
# `wait`. Descendants: pgrep -P, else the MSYS procfs `ppid` files, else just the
# direct child. POSIX kill only — the caller reads a timeout off SIGKILL status.
kill_tree() {
    local pid="$1" child p pp
    if command -v pgrep >/dev/null 2>&1; then
        kill -STOP "$pid" 2>/dev/null || true
        for child in $(pgrep -P "$pid" 2>/dev/null); do
            kill_tree "$child"
        done
    elif [ -e /proc/self/ppid ]; then
        # read, not cat: a fork per process is visible under MSYS. Braces are
        # load-bearing — the shell reports a failed `<` redirect itself, so
        # `2>/dev/null` on `read` alone would leak it to the caller's stderr.
        for p in /proc/[0-9]*; do
            { read -r pp < "$p/ppid"; } 2>/dev/null || continue
            if [ "$pp" = "$pid" ]; then
                kill_tree "${p#/proc/}"
            fi
        done
    fi
    kill -KILL "$pid" 2>/dev/null || true
}

# _wd_owns_target PID OWNER_PID — is PID still the process OWNER_PID forked?
# `kill -0` cannot answer: a recycled pid is alive, just not yours. Where ps
# cannot answer (MSYS, an already-gone pid) degrade to OWNER_PID's own liveness —
# the weak arm may fail to protect, it may never manufacture a kill.
_wd_owns_target() {
    local pid="$1" owner="$2" pp
    pp=$(ps -o ppid= -p "$pid" 2>/dev/null | tr -d '[:space:]')
    if [ -n "$pp" ]; then
        [ "$pp" = "$owner" ]
        return
    fi
    kill -0 "$owner" 2>/dev/null
}

# run_with_watchdog SECONDS CMD... — run CMD under a hard timeout (macOS has no
# `timeout`; a broken Godot run hangs rather than fails, so wrap every engine
# launch). Returns CMD's exit code, or 137 when the dog killed it. The kill takes
# CMD's whole tree and names the command on stderr; the dog is reaped with
# kill_tree so its `sleep` child does not linger.
run_with_watchdog() {
    local secs="$1"; shift
    "$@" &
    local pid=$!
    # Who forked $pid — the dog's licence to signal it. NOT `$$`: every caller of
    # run_bounded captures through a command substitution, whose subshell is the
    # forking process. BASHPID-or-fallback: the macOS system bash is 3.2.
    local owner
    owner="${BASHPID:-}"
    [ -n "$owner" ] || owner=$(exec sh -c 'echo $PPID')
    # The dog's stdout redirect is LOAD-BEARING: callers capture through a command
    # substitution, and the dog plus its `sleep` would hold that pipe's write end
    # open (MSYS does not reliably reap them), hanging the substitution forever.
    # Only the `sleep`'s status tells expiry from a reap: >=128 is the reap, any
    # other failure is an unusable budget and must say so rather than arm nothing
    # (ask explicitly — `set -e` is inert under a `||` list).
    # An orphaned dog outlives its run and pids wrap fast, so its pid is a
    # stranger's — test ownership BEFORE announcing or killing, and spend the
    # budget in poll-sized bites asking the SAME test between them (a weaker one
    # would disarm a live ceiling). Chunking is declined for a non-integer $secs
    # so the unusable-budget arm below stays reachable.
    (
        _wd_st=0
        _wd_poll=15
        case "$secs" in
            ''|*[!0-9]*) _wd_left=0 ;;
            *)           _wd_left="$secs" ;;
        esac
        if [ "$_wd_left" -gt "$_wd_poll" ]; then
            while [ "$_wd_left" -gt 0 ]; do
                if [ "$_wd_left" -lt "$_wd_poll" ]; then
                    _wd_bite="$_wd_left"
                else
                    _wd_bite="$_wd_poll"
                fi
                { sleep "$_wd_bite"; } 2>/dev/null || { _wd_st=$?; break; }
                _wd_left=$((_wd_left - _wd_bite))
                # Stand down silently: the owner is gone, so nothing is left to
                # read the status a kill would have produced.
                if [ "$_wd_left" -gt 0 ]; then
                    _wd_owns_target "$pid" "$owner" || exit 0
                fi
            done
        else
            { sleep "$secs"; } 2>/dev/null || _wd_st=$?
        fi
        if [ "$_wd_st" -ge 128 ]; then exit 0; fi
        if [ "$_wd_st" -ne 0 ]; then
            echo "WATCHDOG: unusable budget '${secs}' — no timeout is armed for: $*" >&2
            exit 0
        fi
        _wd_owns_target "$pid" "$owner" || exit 0
        echo "WATCHDOG: no exit after ${secs}s, killing process tree: $*" >&2
        kill_tree "$pid"
    ) >/dev/null &
    local dog=$!
    local rc=0
    wait "$pid" || rc=$?
    # Wait for the dog's `sleep` to become observable before scanning: kill_tree's
    # /proc route expands `/proc/[0-9]*` once, so a child born later is absent
    # from the list and would be orphaned. No signal closes this — process-group
    # membership is not retroactive. Ceiling is WALL CLOCK ($SECONDS, fork-free),
    # not a count: a walk's cost scales with the live process count. Condition
    # mirrors kill_tree's route choice — only the /proc route needs this.
    if ! command -v pgrep >/dev/null 2>&1 && [ -e /proc/self/ppid ]; then
        local t0=$SECONDS seen p pp
        while [ $((SECONDS - t0)) -lt 2 ]; do
            seen=0
            for p in /proc/[0-9]*; do
                { read -r pp < "$p/ppid"; } 2>/dev/null || continue
                if [ "$pp" = "$dog" ]; then seen=1; break; fi
            done
            if [ "$seen" = "1" ]; then break; fi
            kill -0 "$dog" 2>/dev/null || break
        done
        # Then freeze it: the wait covers a `sleep` forked but not yet listed,
        # the STOP covers a dog that has not forked at all. A stopped process
        # still enumerates children and still dies to the SIGKILL below.
        # kill_tree's /proc route must NOT do this — there the stop is visible to
        # a caller reading `wait`; here the dog's status is discarded next line.
        kill -STOP "$dog" 2>/dev/null || true
    fi
    kill_tree "$dog"
    wait "$dog" 2>/dev/null || true
    return "$rc"
}

# run_bounded CMD... — a gate's final run step (transpiled binary, its real-.NET
# oracle, node/simctl) under a 600s budget, so an infinite loop is a bounded
# failure instead of holding an xargs -P worker slot forever.
# DN2CPP_RUN_WATCHDOG_SECS overrides; a run budget may be env-driven because it
# can only turn a run into an abort or back.
run_bounded() {
    run_with_watchdog "${DN2CPP_RUN_WATCHDOG_SECS:-600}" "$@"
}

# wait_ready_line NAME PID OUT ERR [SECONDS] — echo the first COMPLETE line the
# background server PID writes to OUT, or print the evidence and return 1.
# A pid that already exited fails at once: waiting the deadline out for a dead
# process reports only "the file is empty", which is what made this unreadable.
# The deadline governs only the slow case, and a slow case costs one xargs -P
# worker slot for its length — so it is set well past a loaded runner's start,
# far below run_bounded's own budget.
wait_ready_line() {
    local name="$1" pid="$2" out="$3" err="$4" secs="${5:-60}"
    local t0=$SECONDS line="" waited=0 dead=0
    while :; do
        # `read` succeeds only on a NEWLINE-terminated line. `[ -s "$out" ]` also
        # accepts a half-written "READY 5324", and the caller then parses a port
        # that is not there yet.
        if { IFS= read -r line < "$out"; } 2>/dev/null && [ -n "$line" ]; then
            waited=$((SECONDS - t0))
            # stdout is this function's return channel, so the warning takes stderr;
            # run-all-gates.sh folds stderr into the gate log its grep reads.
            [ "$waited" -lt $((secs / 2)) ] \
                || gate_warn "the $name took ${waited}s of its ${secs}s budget to print READY — a start this slow is one load spike from failing the gate" >&2
            printf '%s\n' "${line%$'\r'}"
            return 0
        fi
        # Liveness AFTER the read: a server that prints READY and exits is ready.
        # kill -0 can read a zombie or a recycled pid as alive; both only cost the
        # full deadline, neither manufactures a failure.
        if ! kill -0 "$pid" 2>/dev/null; then
            if { IFS= read -r line < "$out"; } 2>/dev/null && [ -n "$line" ]; then
                printf '%s\n' "${line%$'\r'}"
                return 0
            fi
            dead=1; break
        fi
        waited=$((SECONDS - t0))
        [ "$waited" -lt "$secs" ] || break
        sleep 0.1
    done
    waited=$((SECONDS - t0))
    local state="still running"
    [ "$dead" = 0 ] || state="already gone — it exited before printing READY"
    {
        printf 'FAIL: the %s never printed a READY line (waited %ss of %ss; pid %s %s)\n' \
            "$name" "$waited" "$secs" "$pid" "$state"
        # $(cat) rather than cat: a server killed mid-line leaves no trailing
        # newline, and the next heading would then run onto its own evidence.
        if [ -s "$out" ]; then printf -- '--- %s ---\n%s\n' "$out" "$(cat "$out")"
        else printf -- '--- %s: EMPTY ---\n' "$out"; fi
        if [ -s "$err" ]; then printf -- '--- %s ---\n%s\n' "$err" "$(cat "$err")"
        else printf -- '--- %s: EMPTY ---\n' "$err"; fi
    } >&2
    return 1
}

# xcodebuild_pipe_budget [HELD] — bytes the kernel grants a FRESH pipe while HELD
# pipes (default 10) are already open. Prints one integer, or 0 if it cannot
# measure. The grant is graded out of a machine-wide budget (16384 down to 512
# observed) and decides whether xcodebuild can build here at all — see the third
# wedge cause at xcodebuild_bounded. HELD is 10 because xcodebuild and
# SWBBuildService open about that many before the compiler-macro probe's does.
xcodebuild_pipe_budget() {
    local held="${1:-10}" out=""
    # perl, not python: perl is already a gate prerequisite; do not add one.
    out="$(perl -e '
        use Fcntl;
        my $held = shift @ARGV;
        my @keep;
        for (1 .. $held) { my ($r, $w); last unless pipe($r, $w); push @keep, [$r, $w]; }
        my ($r, $w);
        exit 1 unless pipe($r, $w);
        my $fl = fcntl($w, F_GETFL, 0);
        exit 1 unless defined $fl;
        fcntl($w, F_SETFL, $fl | O_NONBLOCK);
        my ($buf, $total) = ("x" x 128, 0);
        while (1) {
            my $n = syswrite($w, $buf);
            last if !defined($n) || $n <= 0;
            $total += $n;
        }
        print "$total\n";
    ' "$held" 2>/dev/null)" || out=""
    case "$out" in
        ''|*[!0-9]*) echo 0 ;;
        *) echo "$out" ;;
    esac
}

# xcodebuild_pipe_budget_diagnose BEFORE — print the pipe-budget diagnosis for a
# wedged build: measurement, mechanism, remedy. Failure path only, because the
# pipe census costs a machine-wide lsof.
xcodebuild_pipe_budget_diagnose() {
    local before="$1" now
    now="$(xcodebuild_pipe_budget)"
    echo "note: pipe-buffer budget: a fresh pipe here got ${before} bytes before the" >&2
    echo "      first attempt and ${now} bytes now, with the 10 pipes xcodebuild takes" >&2
    echo "      for itself held. A full-size grant is 16384; below that, the build" >&2
    echo "      service's compiler-macro probe cannot finish and the build never" >&2
    echo "      starts (see the third cause at xcodebuild_bounded in gates/_common.sh)." >&2
    echo "      Remedy: lower the machine's OPEN PIPE COUNT — the budget is shared and" >&2
    echo "      recovers the moment pipes close. The largest holders right now:" >&2
    # Top rows come off a CAPTURED string: `head` quitting early would SIGPIPE
    # the `sort` behind it, which pipefail reports as the status — a diagnosis
    # that fails while diagnosing replaces the cause with a red herring.
    local census top
    census="$(lsof 2>/dev/null | awk '$5=="PIPE"{print $1" (pid "$2")"}' \
        | LC_ALL=C sort | uniq -c | LC_ALL=C sort -rn)" || census=""
    top="$(head -5 <<<"$census")"
    LC_ALL=C sed 's/^/        /' <<<"$top" >&2
}

# xcodebuild_bounded LOG ARGS... — `xcodebuild ARGS...` with all output in LOG,
# exported FUNCTIONS stripped from its environment, serialized machine-wide,
# under a per-attempt watchdog and a bounded retry. Returns xcodebuild's own
# status; 137 means every attempt was killed by the dog.
#
# Three independent things wedge it; 1 and 2 are fixed here, 3 is diagnosed:
#   1. inherited `export -f` entries (`BASH_FUNC_name%%`, which run-all-gates.sh
#      needs for its workers) — stripped HERE, at the one command that breaks.
#   2. a concurrent second xcodebuild (Phase 5 chains A and E overlap) — that is
#      xcodebuild_lock.
#   3. a fresh pipe below 16384 bytes: SWBBuildService never drains its
#      compiler-macro probe's stdio, and the probe's unbuffered `-v` writes are
#      too small to trigger pipe expansion, so they must fit the INITIAL buffer
#      or block forever. The budget is machine-wide and tracks the open PIPE
#      COUNT, so the remedy is the operator's (…_pipe_budget_diagnose).
#
# The retry is armed ONLY by the dog's 137, so a real compile break fails on the
# first attempt; it is sound because kill_tree takes SWBBuildService (a child of
# xcodebuild, not a shared daemon) and each attempt re-takes the lock. 3 x 300s
# keeps the ceiling at the 900s these gates used before.
xcodebuild_bounded() {
    local log="$1"; shift
    local rc=0 attempt n
    # Measured BEFORE the first attempt (xcodebuild's own pipes move the number).
    # Recorded and reported, never acted on.
    local pipe_budget
    pipe_budget="$(xcodebuild_pipe_budget)"
    # `env -u` pairs for every exported-function entry. Read off `env`: the entry
    # NAME (`%%` suffix included) is what env needs and bash spells it nowhere
    # else. Recomputed per call — a gate may source more shell between two.
    local -a envclean=()
    while IFS= read -r n; do
        envclean[${#envclean[@]}]=-u
        envclean[${#envclean[@]}]="$n"
    done < <(env | LC_ALL=C sed -n 's/^\(BASH_FUNC_[^=]*\)=.*/\1/p')
    for attempt in 1 2 3; do
        rc=0
        # Queue OUTSIDE the dog: a waiting gate must not spend its build budget
        # in line, and a lock timeout is a hard failure, not a silent
        # unserialized build.
        xcodebuild_lock || return 1
        # `${a[@]+"${a[@]}"}` — bash 3.2 under `set -u` errors on a plain
        # "${a[@]}" for an EMPTY array, which is exactly the hand-run case.
        run_with_watchdog 300 env ${envclean[@]+"${envclean[@]}"} xcodebuild "$@" \
            >"$log" 2>&1 || rc=$?
        xcodebuild_unlock
        if [ "$rc" -ne 137 ]; then break; fi
        # Where it stopped IS the diagnosis and the next attempt truncates the log
        # holding it. Not the last line: the dog's stderr lands here too, so the
        # WATCHDOG line and the shell's "Killed" notice sit after it.
        local last=""
        cp "$log" "$log.wedged$attempt" 2>/dev/null || true
        last="$(grep -vE '^WATCHDOG:|Killed: 9|^[[:space:]]*$' "$log" 2>/dev/null | tail -1)" || true
        echo "note: xcodebuild attempt $attempt wedged after: $last" >&2
        echo "note: killed it and its build service; retrying (log kept: $log.wedged$attempt)" >&2
        # First wedge only: the census costs a machine-wide lsof.
        if [ "$attempt" = 1 ]; then
            xcodebuild_pipe_budget_diagnose "$pipe_budget"
        fi
    done
    return "$rc"
}

# godot_export_step SECS LOG ARTIFACT CMD... — headless editor export under the
# engine watchdog. Tolerate signal death only when the artifact exists and the
# log has no watchdog or named failure verdict; callers verify its contents.
# Every other nonzero exit fails.
godot_export_step() {
    local secs="$1" log="$2" artifact="$3"; shift 3
    local rc=0
    run_with_watchdog "$secs" "$@" >"$log" 2>&1 || rc=$?
    [ "$rc" -eq 0 ] && return 0
    # Whether the crash handler is still armed when the editor dies in teardown is
    # a function of how far teardown got, not of whether the export ran: the late
    # abort (EditorNode already gone, "singleton is null") prints no handle_crash
    # line at all, and requiring one failed exports that had already written their
    # artifact. The verdict is the artifact plus the log's failure lines; every
    # caller re-verifies the artifact's contents afterwards.
    if [ "$rc" -gt 128 ] && [ -e "$artifact" ] \
        && ! grep -q "^WATCHDOG: " "$log" \
        && ! grep -q "Project export for preset .* failed" "$log" \
        && ! grep -q "Cannot export project with preset" "$log"; then
        echo "note: editor died of signal $((rc - 128)) in teardown after the export verdict;"
        echo "      artifact present, no failure verdict — the known teardown race, tolerated"
        return 0
    fi
    return "$rc"
}

# ── mkdir spinlocks (cross-process mutual exclusion) ─────────────────────────
# _pidlock_acquire LOCK_DIR TIMEOUT_SECS LABEL [OWNER_PATTERN] [OVERRIDE_ENV] —
# advisory cross-process lock (core of sim_lock, _toolchain_stage_lock,
# suite_machine_lock): mkdir atomicity arbitrates, the pid file names the owner
# so a leaked dir is reclaimed; waits loudly, gives up past TIMEOUT_SECS.
# Records $$ — the CALLER arms release, via gate_add_exit_hook. OWNER_PATTERN is
# a pid-reuse guard for locks held for HOURS; it degrades to kill -0 where ps
# cannot answer (MSYS), never to a steal, and the pattern deciding a reclaim is
# the OWNER's, recorded in the dir and written before the pid — a waiter testing
# its own would STEAL — one lock, owners of different shapes.
# _pidlock_reclaim LOCK_DIR LABEL [OVERRIDE_ENV] — remove a stale lock dir, or
# FAIL LOUDLY if it cannot be removed (sticky /tmp: another user's dir).
# Test `rm`'s status, not the dir's existence; losing to a recreating reclaimer
# is fine. OVERRIDE_ENV names the variable relocating THIS lock (several live on
# /tmp; the wrong name reads as actionable). Defaults to the suite lock's.
_pidlock_reclaim() {
    local lock_dir="$1" label="$2" env_name="${3:-DN2CPP_SUITE_MACHINE_LOCK_DIR}" owner
    rm -rf "$lock_dir" 2>/dev/null && return 0
    [ -d "$lock_dir" ] || return 0
    owner=$(ls -ld "$lock_dir" 2>/dev/null | awk '{ print $3 }')
    echo "error: $label: cannot reclaim the stale lock dir $lock_dir" >&2
    echo "       (it belongs to ${owner:-another user} and this process may not remove it;" >&2
    echo "        have that user delete it, or point this run elsewhere with" >&2
    echo "        $env_name=/some/path-you-own.d)" >&2
    return 1
}
_pidlock_acquire() {
    local lock_dir="$1" timeout="$2" label="$3" pattern="${4:-}" env_name="${5:-}"
    local waited=0 pid cmd pat
    until mkdir "$lock_dir" 2>/dev/null; do
        pid=$(cat "$lock_dir/pid" 2>/dev/null || true)
        if [ -n "$pid" ]; then
            if ! kill -0 "$pid" 2>/dev/null; then
                # Owner is gone (SIGKILL leaks the dir).
                _pidlock_reclaim "$lock_dir" "$label" "$env_name" || return 1
                continue
            fi
            pat=$(cat "$lock_dir/owner_pattern" 2>/dev/null || true)
            [ -n "$pat" ] || pat="$pattern"
            if [ -n "$pat" ]; then
                cmd=$(ps -o command= -p "$pid" 2>/dev/null || true)
                if [ -n "$cmd" ] && ! grep -q "$pat" <<<"$cmd"; then
                    # Alive, but no longer a LABEL owner: recycled pid.
                    _pidlock_reclaim "$lock_dir" "$label" "$env_name" || return 1
                    continue
                fi
            fi
        elif [ "$waited" -ge 10 ]; then
            # Owner died between mkdir and its pid write.
            _pidlock_reclaim "$lock_dir" "$label" "$env_name" || return 1
            continue
        fi
        if [ "$waited" -ge "$timeout" ]; then
            echo "error: $label: gave up after ${waited}s waiting on $lock_dir (owner pid: ${pid:-unknown})" >&2
            return 1
        fi
        if [ "$waited" -eq 0 ] || [ $((waited % 60)) -eq 0 ]; then
            echo "$label: waiting on $lock_dir (held by pid ${pid:-unknown}; ${waited}s so far, timeout ${timeout}s)"
        fi
        sleep 2
        waited=$((waited + 2))
    done
    # owner_pattern BEFORE pid — see the write-order note above.
    if [ -n "$pattern" ]; then
        printf '%s\n' "$pattern" > "$lock_dir/owner_pattern"
    fi
    printf '%s\n' "$$" > "$lock_dir/pid"
}

# _pidlock_release LOCK_DIR — only the owner may remove the dir, and A SUBSHELL
# IS NOT THE OWNER: `$$` still reads the parent's pid there, so identity comes
# from BASHPID, falling back to `exec sh -c 'echo $PPID'` (macOS bash is 3.2) —
# inline, since a `$(helper)` would report its own subshell. Does not
# disarm its EXIT hook; a second call is a no-op by the owner test.
_pidlock_release() {
    local lock_dir="$1" me
    me="${BASHPID:-}"
    [ -n "$me" ] || me=$(exec sh -c 'echo $PPID')
    [ "$me" = "$$" ] || return 0
    if [ "$(cat "$lock_dir/pid" 2>/dev/null)" = "$$" ]; then
        rm -rf "$lock_dir"
    fi
    return 0
}

# ── Composable EXIT hooks ─────────────────────────────────────────────────────
# INVARIANT: a gate that takes the machine lock may not write a bare
# `trap … EXIT` — one EXIT slot, so the second caller evicts the first and takes
# the lock release with it. Register here: hooks run most-recently-added first,
# a failing hook does not stop the rest, exit status is preserved.
_gate_exit_hooks=()
gate_add_exit_hook() {
    _gate_exit_hooks[${#_gate_exit_hooks[@]}]="$1"
    trap _gate_run_exit_hooks EXIT
}
_gate_run_exit_hooks() {
    local rc=$? i
    for (( i = ${#_gate_exit_hooks[@]} - 1; i >= 0; i-- )); do
        eval "${_gate_exit_hooks[i]}" || true
    done
    return "$rc"
}

# ── Simulator mutex ───────────────────────────────────────────────────────────
# sim_lock — serialize the ensure_booted_sim → simctl terminate/spawn window
# over the one shared booted simulator. Keep builds OUTSIDE it: never widen into
# a build lock. MACHINE-wide, not repo-local — the resource is one
# per-user simulator set, so a lock under artifacts/ excluded nothing between
# worktrees.
DN2CPP_SIM_LOCK_DIR=${DN2CPP_SIM_LOCK_DIR:-/tmp/dn2cpp-sim-lock.d}
_sim_lock_hooked=0
sim_lock() {
    _pidlock_acquire "$DN2CPP_SIM_LOCK_DIR" "${DN2CPP_SIM_LOCK_TIMEOUT_SECS:-900}" \
        sim_lock "" DN2CPP_SIM_LOCK_DIR || return 1
    # Hook, not a bare trap (the gate may hold the machine lock); once, not per
    # acquire — a hook list growing with a section count reads as a leak.
    if [ "$_sim_lock_hooked" != 1 ]; then
        _sim_lock_hooked=1
        gate_add_exit_hook sim_unlock
    fi
}
sim_unlock() {
    _pidlock_release "$DN2CPP_SIM_LOCK_DIR"
}

# ── xcodebuild mutex ─────────────────────────────────────────────────────────
# Two concurrent `xcodebuild` BUILDS on one machine deadlock each other (6 of 6
# measured, 0 of 8 solo), and chains A and E overlap by schedule. Only real
# builds take it — only they start a build service; `-create-xcframework`,
# `-list`, `-version` stay out. Machine-wide: the resource is Xcode's per-user
# build service. ORDER is machine lock → this one, never the reverse, and
# nothing else is acquired while held; the WAIT sits outside the attempt
# watchdog, so a queued gate keeps its build budget.
DN2CPP_XCODEBUILD_LOCK_DIR=${DN2CPP_XCODEBUILD_LOCK_DIR:-/tmp/dn2cpp-xcodebuild-lock.d}
_xcodebuild_lock_hooked=0
xcodebuild_lock() {
    _pidlock_acquire "$DN2CPP_XCODEBUILD_LOCK_DIR" \
        "${DN2CPP_XCODEBUILD_LOCK_TIMEOUT_SECS:-3600}" \
        xcodebuild_lock "$(basename "$0")" || return 1
    # Once per gate, not per attempt (see sim_lock).
    if [ "$_xcodebuild_lock_hooked" != 1 ]; then
        _xcodebuild_lock_hooked=1
        gate_add_exit_hook xcodebuild_unlock
    fi
}
xcodebuild_unlock() {
    _pidlock_release "$DN2CPP_XCODEBUILD_LOCK_DIR"
}

# ── Suite machine lock ────────────────────────────────────────────────────────
# One runner's Godot/simulator phase at a time, MACHINE-wide: LOGDIR=/other is
# permitted, and two runners then starve each other's engine launches past their
# watchdogs, reading as flaky reds. run-all-gates.sh takes it once around Phase
# 5 (chains inside one runner keep their parallelism; phases 1-4 contend only
# for CPU). Each Godot gate takes it too. On /tmp, not artifacts/: a
# per-worktree lock cannot reach the other worktree's suite. A ps pattern is
# passed since the hold lasts hours. DN2CPP_NO_MACHINE_LOCK=1 opts out;
# DN2CPP_SUITE_MACHINE_LOCK_DIR relocates it for gates/verify-locks.sh.
DN2CPP_SUITE_MACHINE_LOCK_DIR=${DN2CPP_SUITE_MACHINE_LOCK_DIR:-/tmp/dn2cpp-suite-machine-lock.d}
suite_machine_lock() {
    if [ "${DN2CPP_NO_MACHINE_LOCK:-0}" = "1" ]; then
        return 0
    fi
    _pidlock_acquire "$DN2CPP_SUITE_MACHINE_LOCK_DIR" \
        "${DN2CPP_MACHINE_LOCK_TIMEOUT_SECS:-21600}" \
        suite_machine_lock run-all-gates
}
suite_machine_unlock() {
    _pidlock_release "$DN2CPP_SUITE_MACHINE_LOCK_DIR"
}

# ── The gate side of the machine lock ────────────────────────────────────────
# gate_machine_lock — taken by every gate in a Phase-5 chain, so a hand-run
# Godot gate contends with a suite rather than starving its engine launches.
# Dispatched from the ONE table below; membership is exactly the runner's
# GODOT_GATES. Consequence: it is taken while _common.sh is SOURCED, before the
# gate's own gate_skip checks, so a hand-run gate missing a prerequisite waits
# for the lock and skips after. A standalone acquirer records its OWN ps pattern.
# Re-entrancy: run-all-gates.sh exports DN2CPP_MACHINE_LOCK_HELD=$$ while
# holding, and a gate stands down only when that value names the pid the lock
# dir ITSELF records as owner — the variable alone is a claim anybody can make,
# and one stale value would disable the lock for every gate that shell runs
# (fail-OPEN, invisible). Rejected: an ancestor walk, which where ps cannot
# answer finds none, acquires, and self-deadlocks silently.
gate_machine_lock() {
    if [ "${DN2CPP_NO_MACHINE_LOCK:-0}" = "1" ]; then
        return 0
    fi
    local held="${DN2CPP_MACHINE_LOCK_HELD:-}"
    if [ -n "$held" ] \
        && [ "$(cat "$DN2CPP_SUITE_MACHINE_LOCK_DIR/pid" 2>/dev/null)" = "$held" ]; then
        return 0
    fi
    _pidlock_acquire "$DN2CPP_SUITE_MACHINE_LOCK_DIR" \
        "${DN2CPP_MACHINE_LOCK_TIMEOUT_SECS:-21600}" \
        gate_machine_lock "$(basename "$0")" || return 1
    gate_add_exit_hook suite_machine_unlock
}

# The table, and the dispatch. Newline-delimited so membership is an exact line
# match. It restates run-all-gates.sh's CHAIN_A..CHAIN_F, which the runner diffs
# at the top of Phase 5, dying on drift: a gate in a chain but not here would
# silently stop locking (fail-OPEN). ios-sim-console is deliberately NOT here:
# this is chain membership, not a registry of singleton users, and a chain move
# would drop it out of SKIP_GODOT=1 runs and still race chain A;
# sim_lock serializes the simulator. `$0` names the top-level script even when
# sourced; BASH_SOURCE would name _godot_dotnet.sh / _godot_fork.sh.
DN2CPP_MACHINE_LOCK_GATES='build-and-run-godot-sample.sh
build-and-run-godot-ios-export.sh
build-and-run-godot-ios-sim.sh
build-and-run-android-gdext.sh
build-and-run-hotupdate-godot.sh
build-and-run-sdk-sample.sh
build-and-run-godot-dotnet-handshake.sh
build-and-run-godot-dotnet-sample.sh
build-and-run-godot-dotnet-trim.sh
build-and-run-godot-dotnet-wasm.sh
build-and-run-godot-editor-export.sh
build-and-run-godot-editor-export-ios.sh
build-and-run-godot-editor-export-android.sh
build-and-run-godot-editor-export-web.sh
build-and-run-godot-editor-export-web-hermetic.sh
build-and-run-cri-android.sh
build-and-run-cri-web.sh
build-and-run-gdtask.sh'
if grep -qxF "$(basename "$0")" <<<"$DN2CPP_MACHINE_LOCK_GATES"; then
    gate_machine_lock
fi

# ensure_booted_sim — echo the UDID of a booted iOS simulator, booting one if
# none is up (`simctl bootstatus -b` blocks until userspace is ready). Left
# booted: a boot/shutdown cycle would dominate the gate's runtime.
ensure_booted_sim() {
    local udid
    # No `head -1` in a pipeline here: head quits early, the grep behind it dies
    # of SIGPIPE and pipefail makes THAT the status, so the assignment fails and
    # set -e kills the gate. Take the first line off the captured string.
    local all
    all=$(xcrun simctl list devices booted | grep -Eo '[0-9A-F-]{36}' || true)
    udid="${all%%$'\n'*}"
    if [ -z "$udid" ]; then
        all=$(xcrun simctl list devices available | grep iPhone | grep -Eo '[0-9A-F-]{36}' || true)
        udid="${all%%$'\n'*}"
    fi
    if [ -z "$udid" ]; then
        echo "error: no available iPhone simulator (xcrun simctl list devices available)" >&2
        return 1
    fi
    xcrun simctl bootstatus "$udid" -b >/dev/null 2>&1
    printf '%s\n' "$udid"
}

# ── Editor-export toolchain staging ───────────────────────────────────────────
# The editor-export gates install the toolchain into the SHARED fork editor's
# GodotSharp tree ($FORK_GODOTSHARP/Dn2Cpp), which another suite's export
# compile reads as an include root — an unlocked rm -rf + cp -R re-stage showed
# it a half-copied tree. stage_editor_toolchain closes that (stage lock, content
# match skip, atomic swap); assert_editor_toolchain_current tripwires the case a
# shared dir cannot serve at all: two suites staging DIFFERENT content.
# _toolchain_stage_lock / _toolchain_stage_unlock LOCK_DIR_PARENT — the lock dir
# lives BESIDE the tree it protects: under artifacts/ it could not reach the
# other worktree's suite, and keying it on DN2CPP_GODOT_FORK_ROOT would split
# the lock between two roots sharing one tree. Timeout 600s.
_toolchain_stage_lock() {
    local lock_parent="$1"
    _pidlock_acquire "$lock_parent/.dn2cpp-stage-lock.d" 600 _toolchain_stage_lock || return 1
    # Expanded NOW (double quotes): $lock_parent is a function local.
    gate_add_exit_hook "_toolchain_stage_unlock '$lock_parent'"
}
_toolchain_stage_unlock() {
    _pidlock_release "$1/.dn2cpp-stage-lock.d"
}

# stage_editor_toolchain GODOTSHARP SELFHOST_BIN PACKAGE_LOG — package the
# working tree's toolchain and install it into GODOTSHARP/Dn2Cpp (pass
# $FORK_GODOTSHARP), atomically and idempotently.
stage_editor_toolchain() {
    local godotsharp="$1" selfhost_bin="$2" package_log="$3"
    local lock_parent
    lock_parent="$(dirname "$godotsharp")"
    _toolchain_stage_lock "$lock_parent" || return 1

    # Packaging runs INSIDE the lock: package-toolchain.sh rm -rf's the shared
    # layout dir, so this closes the layout-dir race too.
    bash dist/package-toolchain.sh --layout-only --dn2cpp-bin "$selfhost_bin" \
        >"$package_log" 2>&1 || {
        echo "FAIL: packaging the toolchain failed (see below)" >&2
        cat "$package_log" >&2
        _toolchain_stage_unlock "$lock_parent"
        exit 1
    }
    # DN2CPP_OS is this file's platform seam; the arch term matches
    # package-toolchain.sh's HOST_ARCH.
    local layout
    layout="artifacts/toolchain/dn2cpp-toolchain-0.1.0-${DN2CPP_OS}-$(uname -m)"
    [ -x "$layout/bin/dn2cpp$EXE_EXT" ] || {
        echo "FAIL: no bin/dn2cpp$EXE_EXT in the layout $layout" >&2
        _toolchain_stage_unlock "$lock_parent"
        exit 1
    }

    local dest="$godotsharp/Dn2Cpp"
    # manifest.json carries a deterministic content_hash + dn2cpp_commit, so
    # comparing manifests IS a whole-tree identity test. The marker gates the
    # skip: a tree placed by the old non-atomic cp may be half-broken with a
    # manifest present.
    # The manifest's content hash deliberately excludes emsdk/, so a bundle whose
    # only change is its SDK compares EQUAL here — and every gate reading the
    # staged tree would keep exporting through the previous SDK. The stamp is
    # what identifies that tree; both sides absent is a match (no SDK bundled).
    if cmp -s "$layout/manifest.json" "$dest/manifest.json" \
            && [ "$(file_text "$layout/emsdk/.emsdk-stamp")" \
                 = "$(file_text "$dest/emsdk/.emsdk-stamp")" ] \
            && [ -f "$dest/.dn2cpp-staged-atomic" ]; then
        echo "toolchain already current (content match) — install not repeated"
    else
        # Atomic swap: assemble beside $dest, then two renames, so $dest is at
        # every instant absent, the old full tree, or the new full one.
        local stage="$godotsharp/.Dn2Cpp.stage.$$"
        local old="$godotsharp/.Dn2Cpp.old.$$"
        rm -rf "$stage"
        mkdir -p "$stage"
        # The staged Emscripten SDK is 1.4 GB and identical whenever its stamp is,
        # so it is RENAMED out of the tree being replaced rather than copied again
        # — every gate that stages would otherwise pay that copy. The swap below is
        # untouched: the window in which $dest is short an emsdk/ is inside the one
        # in which it is about to be replaced wholesale.
        local entry base reuse_emsdk=0
        if [ -d "$dest/emsdk" ] && [ -f "$layout/emsdk/.emsdk-stamp" ] \
                && cmp -s "$dest/emsdk/.emsdk-stamp" "$layout/emsdk/.emsdk-stamp"; then
            mv "$dest/emsdk" "$stage/emsdk"
            reuse_emsdk=1
        fi
        for entry in "$layout"/* "$layout"/.[!.]*; do
            [ -e "$entry" ] || continue
            base="${entry##*/}"
            [ "$reuse_emsdk" -eq 1 ] && [ "$base" = emsdk ] && continue
            cp -R "$entry" "$stage/"
        done
        touch "$stage/.dn2cpp-staged-atomic"
        [ -x "$stage/bin/dn2cpp" ] || {
            echo "FAIL: no bin/dn2cpp in the staged toolchain $stage" >&2
            _toolchain_stage_unlock "$lock_parent"
            exit 1
        }
        [ -e "$dest" ] && mv "$dest" "$old"
        mv "$stage" "$dest"
        rm -rf "$old"
    fi
    _toolchain_stage_unlock "$lock_parent"

    # bin/ must carry every assembly the CLI auto-references from its OWN
    # directory: the runtime shim plus the three conditional backends. A missing
    # one is SILENT — InjectDefaultRefs prints nothing on NotFound, this lane
    # meets DnZlib's trigger via GodotSharp regardless, and the emitted C++ is a
    # function of the LOAD SET, so the gate would assert against a DIFFERENT
    # program. Hard FAIL, not gate_skip: the bundle came from this tree above,
    # so a missing file means broken packaging, not an under-provisioned box.
    local bundle_missing="" sib
    for sib in Dn2Cpp.Runtime DnZlib DnBrotli DnHttp; do
        [ -f "$dest/bin/$sib.dll" ] || bundle_missing="$bundle_missing $sib"
    done
    [ -z "$bundle_missing" ] || {
        echo "FAIL: the staged toolchain $dest/bin is missing:$bundle_missing" >&2
        echo "      The bundled native CLI auto-references these from AppContext.BaseDirectory," >&2
        echo "      which for it is bin/. A missing one is not an error at transpile time — it" >&2
        echo "      silently shrinks the load set, and with it the emitted C++, so this gate" >&2
        echo "      would assert against a program no other lane builds." >&2
        echo "      Re-package from a tree that installs them (dist/package-toolchain.sh)." >&2
        exit 1; }
}

# assert_editor_toolchain_current GODOTSHARP — run AFTER the export's compile:
# two suites staging DIFFERENT content into one shared tree cannot both be
# served, so make it a self-explaining FAIL, not a cryptic include error.
assert_editor_toolchain_current() {
    local godotsharp="$1" layout
    layout="artifacts/toolchain/dn2cpp-toolchain-0.1.0-${DN2CPP_OS}-$(uname -m)"
    cmp -s "$layout/manifest.json" "$godotsharp/Dn2Cpp/manifest.json" || {
        # The remedy names BOTH variables: the staged tree lives beside the
        # fork's editor, so a distinct FORK_ROOT still shares it, and a distinct
        # FORK_CLONE only moves the worktree the pin/ABI check fingerprints.
        echo "FAIL: a concurrent suite re-staged $godotsharp/Dn2Cpp mid-run (toolchain manifest changed under this export). Run truly different toolchains in parallel by giving each suite its own fork clone AND its own DN2CPP_GODOT_FORK_ROOT assembled from that clone (DN2CPP_GODOT_FORK_CLONE=<clone> gates/setup-godot-fork.sh <root>) — either one alone leaves the staged tree shared, because it lives beside the fork's editor and the root is what records which editor that is." >&2
        exit 1
    }
}
