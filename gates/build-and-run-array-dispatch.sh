#!/usr/bin/env bash
# Consolidated array-dispatch gate. Merges the former array covariance / interface
# dispatch / multidimensional subset gates into one multi-section program,
# transpiled once against the tree-shaken real CoreLib and diffed exactly against
# real .NET. Covers array covariance + covariant virtual dispatch, SZArray
# interface dispatch (IList<T>/IEnumerable<T> on T[]), runtime collection dispatch
# over arrays, multidimensional array literals, and general multidim arrays.
# Former gates: array-covariance, array-covariant-dispatch, array-interface-dispatch,
# array-runtime-collection-dispatch, md-array-literal, multidim-array.
#
# MdInterfaceDispatchSubset asserts interface DISPATCH on
# rank>=2 arrays — the one shared runtime-installed table
# (dn2cpp_array_set_md_fallback_interfaces) serving all six non-generic CLR MD-array
# interfaces element-agnostically through the MDArrayEnumerable wrapper: enumeration
# order (2-D/3-D/string/struct element, LINQ Cast/Sum), the enumerator protocol's
# exception texts, ICollection/IList behavior incl. the in-place Clear and every
# real-.NET throw message, ICloneable independence, and the structural pair's
# measured guard order. Its struct-element section is also the no-equality-needed
# proof (a wrapper row must never demand element equality), and its exception
# MESSAGES double as an oracle pin on the wrapper's hardcoded strings.
#
# It also carries generic variance on ORDINARY types (GenericVarianceDispatchSubset) —
# the same rule the array sections lean on, off the array path: a user `out T`/`in T`
# interface, List<T> read as IEnumerable<Base>, a contravariant IComparer<in T>, a
# two-parameter variant definition, and the C# 9 covariant return (an override the CLR
# binds through a MethodImpl row, not through a matching signature).
#
# A final negative section pins the failure mode of the support shim the SZArray
# interface map is built from (Dn2Cpp.Runtime.SZArrayEnumerable<T>). It used to go
# missing silently — the arrays simply lost their interface map, the C++ compiled
# and linked, and the program aborted on its first (IEnumerable<T>)arr. The
# transpile must refuse instead, naming the shim and how to supply it.
source "$(dirname "$0")/_common.sh"

# The negative section runs INSIDE the wrapper's cached region, through the
# gate_extra_asserts hook (`gates/_common.sh`), so a warm hit replays a green that
# includes it. That matters more here than for a cheap assert: the section copies the
# whole CLI output directory and runs a full real-CoreLib transpile, which is the
# single most expensive thing this gate does, and it used to do it on every suite pass
# after the wrapper had already reported success.
#
# The scratch CLI copy is torn down by an EXIT trap, and both the variable and the
# trap live at SCRIPT scope on purpose: an EXIT trap runs after the hook's frame is
# gone, so a `local shimless` is unbound by then and — under the suite's `set -u` —
# the trap itself fails the gate, exit 1 with every assert already green. (Observed,
# not theorized: that is exactly how this refactor first ran.) The guard also covers
# the warm-hit path, where the hook never runs and there is nothing to remove.
shimless=""
_cleanup_shimless() { [ -n "$shimless" ] && rm -rf "$shimless"; return 0; }
trap _cleanup_shimless EXIT

gate_extra_asserts() {
    echo "-- negative: transpiling without the support shim must be rejected --"
    local corelib app linq noshim_rc=0 noshim_err
    corelib=$(locate_corelib)
    app="samples/dotnet/ArrayDispatch/bin/$CONFIG/$TFM/ArrayDispatch.dll"
    # The same reference set the positive transpile used — a shimless run must fail on the
    # MISSING SHIM, not trip over an unreferenced assembly first and prove nothing.
    linq="$(dirname "$corelib")/System.Linq.dll"

    # A CLI whose sibling Dn2Cpp.Runtime.dll is absent, which is how a mispackaged
    # install looks. AppContext.BaseDirectory is the dll's own directory under
    # `dotnet exec` (and the executable's directory for a native build), so copying the
    # CLI out and deleting the shim defeats the auto-reference exactly as a bad bundle
    # would. The host resolves assemblies lazily, so the stale deps.json entry is inert.
    shimless=$(mktemp -d)
    cp -R "src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/." "$shimless/"
    rm -f "$shimless/Dn2Cpp.Runtime.dll"

    noshim_err=$(dotnet exec "$shimless/dn2cpp.dll" "$app" -r "$corelib" -r "$linq" -o "$shimless/gen" 2>&1 >/dev/null) || noshim_rc=$?
    if [ "$noshim_rc" -ne 2 ]; then
        echo "FAIL: transpiling without the support shim exited $noshim_rc (expected 2)" >&2
        echo "$noshim_err" >&2
        exit 1
    fi
    if ! grep -q "Dn2Cpp.Runtime.dll" <<<"$noshim_err"; then
        echo "FAIL: the missing-shim error does not name Dn2Cpp.Runtime.dll:" >&2
        echo "$noshim_err" >&2
        exit 1
    fi
    # Every generated*, not generated.cpp alone: emission STREAMS, so generated_b0.cpp
    # / generated_m0.cpp are written and released during body compilation while
    # generated.h and generated.cpp are written only after Emit returns. A refusal
    # that regressed into failing mid-emission would leave the directory full of C++
    # with a generated.cpp-only probe still green.
    if compgen -G "$shimless/gen/generated*" >/dev/null; then
        echo "FAIL: C++ was emitted despite the missing support shim:" >&2
        ls -1 "$shimless/gen" >&2
        exit 1
    fi
    echo "OK (missing support shim rejected)"
}

# What the section asserts is the TRANSPILER's conduct with a member of its own bundle
# removed — an exit code and a diagnostic's wording — and none of that appears in the
# positive transpile's output. So the key's surface term cannot see a regression in it:
# reword the refusal, or lose it, and OUT stays byte-identical and the cached green is
# replayed. The CLI hash closes it, the same stand-in a behavior gate uses; it also
# covers the copied tree, which IS the CLI output directory.
export DN2CPP_GATE_EXTRA_CONTEXT="noshim-refusal|cli:$(_gate_cli_hash)"

# System.Linq rides in for one section: IGrouping<out TKey, out TElement> is the only
# two-parameter variant interface the BCL exposes, and a two-parameter variant definition
# is precisely what the arity-1 runtime match could not see.
corelib_diff_gate ArrayDispatch System.Linq
