#!/usr/bin/env bash
# Runs the platform-ISA probe as an x86-64 binary under Rosetta 2 on an Apple
# silicon Mac — the one x86 execution path this host has. The x86 helpers run for
# real: Rosetta implements SSE4.2 with POPCNT, AES and PCLMULQDQ, and the probe's
# contract block prints exactly what it exposes.
#
# A developer procedure, not a gate. The x86 gate excludes Rosetta on purpose: no
# real .NET oracle runs here (the host SDK is arm64), and Rosetta's CPUID is a
# feature set no shipping CPU has. So the checks are the probe's own — every
# `ref=` cross-check of a generated exercise prints ref=OK, the one-past-range
# immediate prints ArgumentOutOfRangeException, DN2CPP_CPU_FEATURES=none folds
# every row to False with one diag line, and a partial mask removes the named
# family together with the families that imply it.
#
#   tools/platform-isa-rosetta.sh [--preview Arch.Family,...] [--mask MASK]... [SELECTION]
#
# SELECTION is the probe's (default X86). --mask adds a DN2CPP_CPU_FEATURES run
# (default -Sse41, which must also remove Sse42 and Popcnt). --preview
# regenerates the tree with gen-isa-map --lowered-preview so a family whose map
# is still incomplete runs its mapped methods; the tree is regenerated without
# the preview on exit, whatever happened, so the checked-in state is restored.
#
# Builds live beside the gates' but never in their directories:
# artifacts/.cmake-runtime-rosetta (the x86-64 runtime) and
# artifacts/platformisaprobe-rosetta (the transpiled probe and its build).
source "$(dirname "$0")/../gates/_common.sh"

preview=""
masks=()
selection=""
while [ $# -gt 0 ]; do
    case "$1" in
        --preview) [ $# -ge 2 ] || { echo "error: --preview takes Arch.Family,..." >&2; exit 2; }
                   preview="$2"; shift 2 ;;
        --mask)    [ $# -ge 2 ] || { echo "error: --mask takes a DN2CPP_CPU_FEATURES value" >&2; exit 2; }
                   masks+=("$2"); shift 2 ;;
        -*)        echo "error: unknown option '$1'" >&2; exit 2 ;;
        *)         [ -z "$selection" ] || { echo "error: one selection only" >&2; exit 2; }
                   selection="$1"; shift ;;
    esac
done
selection=${selection:-X86}
[ ${#masks[@]} -gt 0 ] || masks=(-Sse41)

[ "$(uname -s)" = Darwin ] && [ "$(uname -m)" = arm64 ] \
    || { echo "error: this procedure is for an Apple silicon Mac (Rosetta 2)" >&2; exit 2; }
arch -x86_64 /usr/bin/true 2>/dev/null \
    || { echo "error: Rosetta 2 is not installed (softwareupdate --install-rosetta)" >&2; exit 2; }

corelib=$(locate_corelib)
project=PlatformIsaProbe
out=artifacts/platformisaprobe-rosetta
runtime_dir=$PWD/artifacts/.cmake-runtime-rosetta
build_dir=$PWD/$out/.cmake-rosetta

if [ -n "$preview" ]; then
    # The preview is never a checked-in state: the restore runs on every exit.
    restore_generated() {
        echo "== restoring the generated tree (no preview) =="
        dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib "$corelib" >/dev/null
    }
    gate_add_exit_hook restore_generated
    echo "== 0/5 Regenerating with --lowered-preview $preview =="
    dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib "$corelib" --lowered-preview "$preview"
fi

echo "== 1/5 Building and transpiling $project =="
build_proj "samples/dotnet/$project/$project.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"
invoke_cli "$app" -r "$corelib" -o "$out"

launcher=()
if [ -z "${DN2CPP_NO_CCACHE:-}" ] && command -v ccache >/dev/null 2>&1; then
    _ccache_pch_env
    launcher=(-DCMAKE_C_COMPILER_LAUNCHER=ccache -DCMAKE_CXX_COMPILER_LAUNCHER=ccache)
fi

echo "== 2/5 Building the x86-64 runtime ($runtime_dir) =="
"$CMAKE" -S runtime -B "$runtime_dir" -G Ninja -DCMAKE_OSX_ARCHITECTURES=x86_64 \
    -DDN2CPP_USE_GC=ON ${launcher[@]+"${launcher[@]}"} >"$runtime_dir.configure.log" 2>&1 \
    || { cat "$runtime_dir.configure.log" >&2; exit 1; }
"$CMAKE" --build "$runtime_dir" >"$runtime_dir.build.log" 2>&1 \
    || { tail -60 "$runtime_dir.build.log" >&2; exit 1; }

echo "== 3/5 Building the x86-64 probe ($build_dir) =="
"$CMAKE" -S runtime -B "$build_dir" -G Ninja -DCMAKE_OSX_ARCHITECTURES=x86_64 \
    -DDN2CPP_RUNTIME_EXPORT="$runtime_dir/dn2cpp-targets.cmake" \
    -DDN2CPP_APP_DIR="$PWD/$out" -DDN2CPP_APP_NAME="$project" \
    ${launcher[@]+"${launcher[@]}"} >"$build_dir.configure.log" 2>&1 \
    || { cat "$build_dir.configure.log" >&2; exit 1; }
"$CMAKE" --build "$build_dir" >"$build_dir.build.log" 2>&1 \
    || { tail -60 "$build_dir.build.log" >&2; exit 1; }
bin="$build_dir/$project"
file "$bin" | grep -q x86_64 || { echo "error: $bin is not an x86_64 binary" >&2; exit 1; }

fails=0
# run LABEL ENV... — one probe run under Rosetta; prints the contract block and the
# exercise summary, fails on a MISMATCH or a non-zero exit.
run() {
    local label="$1"; shift
    local log="$build_dir/$label.log" err="$build_dir/$label.err" rc=0
    echo "== run $label: ${*:-<no mask>} =="
    env "$@" arch -x86_64 "$bin" "$selection" >"$log" 2>"$err" || rc=$?
    awk '/^== contract ==$/ { c = 1; next } /^== / { c = 0 } c' "$log" | sed 's/^/  /'
    local ok mismatch thrown probes
    ok=$(grep -c ' ref=OK' "$log" || true)
    mismatch=$(grep -c ' ref=MISMATCH' "$log" || true)
    thrown=$(grep -c 'ArgumentOutOfRangeException$' "$log" || true)
    probes=$(grep -c '^probe=PlatformNotSupportedException$' "$log" || true)
    echo "  exit=$rc lines=$(grep -c . "$log") ref=OK:$ok ref=MISMATCH:$mismatch out-of-range-immediates:$thrown unsupported-probes:$probes"
    if [ "$rc" -ne 0 ]; then
        echo "  FAIL: the probe exited $rc; stderr follows" >&2
        sed 's/^/    /' "$err" >&2
        fails=$((fails + 1))
    fi
    if [ "$mismatch" -ne 0 ]; then
        echo "  FAIL: portable cross-checks disagree:" >&2
        grep ' ref=MISMATCH' "$log" | sed 's/^/    /' >&2
        fails=$((fails + 1))
    fi
    if grep -q '^probe=returned$' "$log"; then
        echo "  FAIL: an unsupported family's probe returned instead of throwing" >&2
        fails=$((fails + 1))
    fi
    echo "  full output: $log"
}

echo "== 4/5 Running under Rosetta =="
run unmasked
run none DN2CPP_CPU_FEATURES=none DN2CPP_CPU_FEATURES_DIAG=1
if grep -q '=True$' "$build_dir/none.log"; then
    echo "  FAIL: a row is True under DN2CPP_CPU_FEATURES=none" >&2; fails=$((fails + 1))
fi
if [ "$(grep -c 'effective=(none)' "$build_dir/none.err" || true)" -ne 1 ]; then
    echo "  FAIL: expected exactly one 'effective=(none)' diag line" >&2; fails=$((fails + 1))
fi
for m in "${masks[@]}"; do
    run "mask$m" DN2CPP_CPU_FEATURES="$m"
done

echo "== 5/5 Verdict =="
if [ "$fails" -ne 0 ]; then
    echo "FAIL: $fails check(s) failed" >&2
    exit 1
fi
echo "ok: every run held (see the contract blocks above for what Rosetta exposes)"
