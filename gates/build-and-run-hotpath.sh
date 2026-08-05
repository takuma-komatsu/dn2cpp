#!/usr/bin/env bash
# Consolidated [HotPath] gate: the bare Dn2Cpp.Runtime.HotPathAttribute must not
# change semantics — marked bodies (numeric kernels, a generic over reference
# and value args, an allocating/virtual/throwing body, an AggressiveInlining
# combo) are exact-diffed vs real .NET — while landing in the dedicated
# generated_hot.cpp TU the build compiles with stronger per-file flags. The
# SkipBoundsChecks knob section (HotPathBoundsSubset) runs in the same diff:
# every index is in range, so the outputs match real .NET, where the attribute
# is inert and every access stays checked.
#
# Placement is then asserted structurally on the transpile output, from inside the
# wrapper's cached region (the gate_extra_asserts hook, `gates/_common.sh`) — so a
# warm hit replays a green that includes these asserts rather than re-running them
# against a build it did not make. They read nothing but the emitted C++, which IS
# the cache key's surface term, so the key already discriminates every axis they
# can see; that is why this hook needs no DN2CPP_GATE_EXTRA_CONTEXT, unlike the two
# gates whose extra asserts run the CLI again:
#   - every marked body's symbol is defined in generated_hot.cpp (the C++ link
#     above already proved each is defined nowhere else — a duplicate would not
#     link, a miss would grep-fail here);
#   - the hot TU carries no rgctx machinery: the marked generic method's
#     reference-type instantiations compiled monomorphic instead of binding a
#     shared canonical body ("hotpath" taint);
#   - the tiny [MethodImpl(AggressiveInlining)] marked body was NOT promoted
#     into the shared header, while its unmarked twin WAS — so the exclusion is
#     asserted against a working promotion mechanism, not vacuously;
#   - every SkipBoundsChecks body names no checked element helper (raw indexing
#     replaced the dn2cpp_bounds_check/wrapper family), the span-indexer bodies
#     carry raw f__reference arithmetic instead of a get_Item call, and a
#     plain-[HotPath] body still names its checked helper — so the absence
#     asserts prove elision, not a vanished mechanism;
#   - the FastMath subset (HotPathFastMathSubset) and the plain marked set
#     PARTITION across generated_hot_fast.cpp and generated_hot.cpp, with the
#     unmarked IEEE-exact twins in neither. That section cannot be snapshotted —
#     relaxed FP varies with host compiler and ISA — so it self-checks in-program
#     against those twins within a relative epsilon and prints only the verdict;
#     under real .NET the attribute is inert, both twins are exact, and the
#     difference is zero, so the exact-diff oracle still holds on the boolean.
#   - the NoAlias subset carries __restrict on exactly its array and byref
#     parameters (never the class-typed one, never a plain-[HotPath] body's
#     arrays), and its span parameters are hoisted into __restrict prologue
#     locals — except the one the body reseats, which opts out while keeping the
#     raw indexer.
source "$(dirname "$0")/_common.sh"

# Every block under an emitter-stamped `// Namespace.Class::Name` header — a
# generic method's instantiations share the header, so all are collected.
# Defined outside gate_extra_asserts but reading $hot from it: bash scoping is
# dynamic, so a helper called from that frame sees the frame's locals.
hot_bodies() {
    awk -v anchor="// $1" '
        $0 == anchor { infn = 1; next }
        infn && /^}/ { infn = 0; next }
        infn { print }
    ' "$hot"
}

gate_extra_asserts() {
# $1 is the OUT corelib_diff_gate actually transpiled into, not a path re-derived
# from the project name: the wrapper appends -hwy/-scalar/$DN2CPP_OUT_SUFFIX, so a
# re-derivation would point every assert below at the DEFAULT build's artifacts
# whenever this gate runs on a non-default axis — and pass against a program this
# run never produced. Reading it as the hook's argument makes that
# re-derivation impossible rather than merely discouraged.
# `local`, not bare assignment: bash scoping is dynamic, so an unqualified `out=`
# here would rebind the CALLER's `local out` — the wrapper's own OUT — from inside
# the hook it invoked. `local` also keeps $hot/$hotfast visible to hot_bodies and
# the readers below, which are called from within this frame.
local hp_out hot hotfast
hp_out="$1"
hot="$hp_out/generated_hot.cpp"
hotfast="$hp_out/generated_hot_fast.cpp"

echo "== [HotPath] TU placement asserts =="
[ -f "$hot" ] || { echo "FAIL: $hot was not emitted" >&2; exit 1; }

for sym in SumInts DotProduct AxpyVecs PickLast BuildAndCount HotTinyAdd; do
    grep -q "$sym" "$hot" || { echo "FAIL: $sym not defined in generated_hot.cpp" >&2; exit 1; }
done

# The NoAlloc verifying-knob kernels are [HotPath] too, so they land in the hot
# TU — and the whole transpile completing is the verifier's own pass (a violation
# would have failed corelib_diff_gate's own transpile with error:/exit 2, before
# this hook ran at all; the negative section asserts that failure path). Matched
# via hot_bodies on the full emitter-stamped symbol, not a bare substring:
# `grep -q SumSpan` would be satisfied by HotPathBoundsSubset's SumSpanUnchecked
# in the same TU.
for sym in SumOfSquares SumSpan; do
    [ -n "$(hot_bodies "HotPathNoAllocSubset.Program::$sym")" ] \
        || { echo "FAIL: NoAlloc kernel $sym not defined in generated_hot.cpp" >&2; exit 1; }
done

if grep -q "rgctx" "$hot"; then
    echo "FAIL: rgctx machinery in generated_hot.cpp — a marked generic body was shared instead of monomorphized" >&2
    exit 1
fi

if grep -Eq "inline .*HotTinyAdd" "$hp_out/generated.h"; then
    echo "FAIL: HotTinyAdd was inline-promoted into generated.h despite [HotPath]" >&2
    exit 1
fi
grep -Eq "inline .*ColdTinyAdd" "$hp_out/generated.h" \
    || { echo "FAIL: ColdTinyAdd was not inline-promoted — the promotion mechanism itself regressed, so the HotTinyAdd assert above proves nothing" >&2; exit 1; }

echo "== [HotPath] SkipBoundsChecks asserts =="

# Every diagnostic match below reads a here-string, never `printf … | grep -q`.
# That pipeline is unsound under this suite's `set -o pipefail`: grep -q exits at the
# first match, and if it wins the race against printf's write the pipe has no reader,
# printf dies of SIGPIPE and the PIPELINE reports 141 — so a message that IS present
# reads as absent. It is not a big-output hazard: measured here at 3,083 bytes, well
# under the pipe buffer, because the race is against grep's EXIT and not against the
# buffer filling. A here-string is fully materialized before grep starts.
for sym in SumIntsUnchecked ReverseIntsUnchecked MaxByRefUnchecked SwapEndsUnchecked \
           ScaleFloatsUnchecked FillBytesUnchecked SumBytesUnchecked SumLongsUnchecked \
           AxpyVecsUnchecked CopyVecsUnchecked LastUnchecked SetFirstUnchecked \
           SumSpanUnchecked UpperAsciiSpanUnchecked; do
    body="$(hot_bodies "HotPathBoundsSubset.Program::$sym")"
    [ -n "$body" ] || { echo "FAIL: no $sym body in generated_hot.cpp" >&2; exit 1; }
    if grep -Eq 'dn2cpp_(bounds_check|ldelem_|stelem_|elem_addr)\(' <<<"$body"; then
        echo "FAIL: $sym still names a checked element helper despite SkipBoundsChecks" >&2
        exit 1
    fi
done

for sym in SumSpanUnchecked UpperAsciiSpanUnchecked; do
    body="$(hot_bodies "HotPathBoundsSubset.Program::$sym")"
    grep -q 'f__reference) +' <<<"$body" \
        || { echo "FAIL: $sym does not read the span through raw f__reference arithmetic" >&2; exit 1; }
    if grep -q 'get_Item' <<<"$body"; then
        echo "FAIL: $sym still calls the checked span get_Item" >&2
        exit 1
    fi
done

# Positive control: a plain-[HotPath] body keeps its checked access, so the
# absence asserts above prove elision, not a vanished mechanism.
grep -q 'dn2cpp_ldelem_i4' <<<"$(hot_bodies "HotPathBasicSubset.Program::SumInts")" \
    || { echo "FAIL: plain [HotPath] SumInts lost its checked element access — the SkipBoundsChecks asserts above prove nothing" >&2; exit 1; }

echo "OK: hot TU placement, monomorphization, header-promotion exclusion, and bounds-check elision"

echo "== [HotPath(FastMath)] TU partition asserts =="
# The FastMath knob's whole mechanism is the SECOND hot TU: relaxed floating
# point is a translation-unit property, so a marked kernel has to leave
# generated_hot.cpp entirely. The two TUs must therefore PARTITION the marked
# set — a symbol in both would not link, one in neither would silently compile
# under the flat flags — so each assert below has its complement.
[ -f "$hotfast" ] || { echo "FAIL: $hotfast was not emitted" >&2; exit 1; }

for sym in DotFast SumReciprocalsFast; do
    grep -q "$sym" "$hotfast" || { echo "FAIL: FastMath kernel $sym not defined in generated_hot_fast.cpp" >&2; exit 1; }
    if grep -q "$sym" "$hot"; then
        echo "FAIL: FastMath kernel $sym is ALSO in generated_hot.cpp — the two hot TUs do not partition" >&2
        exit 1
    fi
done

# The complement, in both directions. A plain-[HotPath] body must stay in the
# plain TU (so FastMath routing did not swallow the unmarked marked set), and the
# unmarked IEEE-exact twins must be in neither (so they keep compiling under the
# flat flags, which is what makes them a valid oracle for the in-program epsilon
# check).
if grep -q "SumInts" "$hotfast"; then
    echo "FAIL: plain [HotPath] SumInts landed in generated_hot_fast.cpp — FastMath routing is not knob-gated" >&2
    exit 1
fi
for sym in DotExact SumReciprocalsExact; do
    if grep -q "$sym" "$hot" || grep -q "$sym" "$hotfast"; then
        echo "FAIL: unmarked twin $sym landed in a hot TU — its relaxed-FP-free compilation is what the epsilon check compares against" >&2
        exit 1
    fi
done

echo "OK: the two hot TUs partition the marked set on the FastMath knob"

echo "== [HotPath(NoAlias)] __restrict asserts =="

# The emitter stamps `// Ns.Cls::Name` immediately above the signature, so the
# signature is the first line hot_bodies() yields. awk (not `head`) reads the
# whole stream, so no SIGPIPE under `set -o pipefail`.
hot_sig() { hot_bodies "$1" | awk 'NR == 1'; }
restrict_count() { printf '%s' "$1" | awk '{ n += gsub(/__restrict/, "") } END { print n+0 }'; }

# Each expected count is the number of parameters whose C++ type is a pointer
# INTO data: Axpy's two arrays; MinMax's array plus two byrefs; CountOver's one
# array — its Threshold parameter is a class reference, which must stay
# unqualified because two object references may legitimately alias. DotSpans
# takes its spans by value (a struct, not a pointer), so its signature carries
# none at all — the span half of the knob lives in the body, asserted below.
assert_restrict_count() {
    local sym="$1" want="$2" sig got
    sig="$(hot_sig "HotPathNoAliasSubset.Program::$sym")"
    [ -n "$sig" ] || { echo "FAIL: no $sym body in generated_hot.cpp" >&2; exit 1; }
    got="$(restrict_count "$sig")"
    if [ "$got" != "$want" ]; then
        echo "FAIL: $sym signature carries $got __restrict, expected $want:" >&2
        echo "  $sig" >&2
        exit 1
    fi
}
assert_restrict_count Axpy 2
assert_restrict_count MinMax 3
assert_restrict_count CountOver 1
assert_restrict_count DotSpans 0

# The class-typed parameter, named explicitly: the count above would also pass if
# the qualifier had landed on Threshold and fallen off the array.
countover_sig="$(hot_sig "HotPathNoAliasSubset.Program::CountOver")"
if grep -q "Threshold\* __restrict" <<<"$countover_sig"; then
    echo "FAIL: CountOver's class-typed parameter was qualified __restrict — two object references may alias:" >&2
    echo "  $countover_sig" >&2
    exit 1
fi
grep -q "Dn2CppArrayI4\* __restrict" <<<"$countover_sig" \
    || { echo "FAIL: CountOver's array parameter was NOT qualified __restrict: $countover_sig" >&2; exit 1; }

# Positive control: an unmarked-knob [HotPath] body with two array parameters
# keeps a bare signature, so the asserts above test the knob rather than a
# blanket change to every emitted parameter.
axpyvecs_sig="$(hot_sig "HotPathBasicSubset.Program::AxpyVecs")"
[ -n "$axpyvecs_sig" ] || { echo "FAIL: no AxpyVecs body in generated_hot.cpp" >&2; exit 1; }
if [ "$(restrict_count "$axpyvecs_sig")" != "0" ]; then
    echo "FAIL: plain [HotPath] AxpyVecs carries __restrict — the qualifier is not NoAlias-gated, so the asserts above prove nothing:" >&2
    echo "  $axpyvecs_sig" >&2
    exit 1
fi

# The span half: each span parameter's element pointer hoisted once into a
# __restrict prologue local, which the raw indexer then addresses instead of
# re-reading f__reference through the span struct at every access.
dotspans_body="$(hot_bodies "HotPathNoAliasSubset.Program::DotSpans")"
for na in "__restrict __na0 = " "__restrict __na1 = " "__na0 + " "__na1 + "; do
    grep -qF -- "$na" <<<"$dotspans_body" \
        || { echo "FAIL: DotSpans is missing the NoAlias span hoist '$na'" >&2; exit 1; }
done
# `->f__reference`, not a bare one: the PROLOGUE reads `a0.f__reference` to
# initialize the hoist, and it is the per-access read THROUGH the span pointer
# that the hoist replaces.
if grep -q -- '->f__reference' <<<"$dotspans_body"; then
    echo "FAIL: DotSpans still reads f__reference per access despite the NoAlias hoist" >&2
    exit 1
fi

# The complement: a body that RESEATS its span parameter (s = s.Slice(...)) opts
# out of the hoist — the hoisted pointer would go stale — while keeping the raw
# SkipBoundsChecks indexer. Both halves are asserted, so neither can regress into
# the other.
sumtail_body="$(hot_bodies "HotPathNoAliasSubset.Program::SumTail")"
[ -n "$sumtail_body" ] || { echo "FAIL: no SumTail body in generated_hot.cpp" >&2; exit 1; }
if grep -q '__na' <<<"$sumtail_body"; then
    echo "FAIL: SumTail hoisted a span parameter it reseats — the hoisted pointer would be stale" >&2
    exit 1
fi
grep -q 'f__reference) +' <<<"$sumtail_body" \
    || { echo "FAIL: SumTail lost the raw span indexer — the hoist opt-out must not disable SkipBoundsChecks" >&2; exit 1; }
if grep -q 'get_Item' <<<"$sumtail_body"; then
    echo "FAIL: SumTail fell back to the checked span get_Item" >&2
    exit 1
fi

echo "OK: __restrict on the qualifying parameters only, and the span hoist with its reseat opt-out"
}

corelib_diff_gate HotPath

# ── [HotPath(NoAlloc)] verifier: the negative side ────────────────────────────
# The positive kernels above proved a clean closure transpiles; these four
# mini-projects prove a dirty one is REJECTED — loudly (error:/exit 2), naming
# the method, the offending construct, and (for the depth-2 case) the
# intermediate helper in the call chain. They are transpile-only (never built to
# native), so this is the only section that exercises the failure path.
#
# Cached like transpiler-limits' bad-project section: the asserts key on the CLI
# (via _gate_cli_hash) and the resolved CoreLib, so a warm rerun with an
# unchanged transpiler skips the four CoreLib transpiles (seconds each). The
# out dir is EMPTIED — cleared and recreated — before the check, so the key's
# surface term is the stable `no-generated` marker; a failing transpile still
# streams partial junk there, which is why the key never keys on it. It has to
# exist: an absent dir is unreadable rather than empty, and gate_cache_check
# answers that with a warning and no key, leaving this section
# permanently uncached since it clears the dir on every run.
#
# The four build_proj calls stay OUTSIDE this region, and that is not an oversight:
# they produce the very dlls the check hashes as key inputs, so they have to run
# before it — the same order _corelib_gate_core uses for its own app build. What a
# warm hit skips is the four real-CoreLib transpiles, which is the cost that matters.
echo "== [HotPath(NoAlloc)] negative asserts =="
build_proj samples/dotnet/HotPathNoAllocArrayBad/HotPathNoAllocArrayBad.csproj
build_proj samples/dotnet/HotPathNoAllocDeepBad/HotPathNoAllocDeepBad.csproj
build_proj samples/dotnet/HotPathNoAllocVirtBad/HotPathNoAllocVirtBad.csproj
build_proj samples/dotnet/HotPathNoAllocMixedBad/HotPathNoAllocMixedBad.csproj
nb_corelib=$(locate_corelib)
arr_app="samples/dotnet/HotPathNoAllocArrayBad/bin/$CONFIG/$TFM/HotPathNoAllocArrayBad.dll"
deep_app="samples/dotnet/HotPathNoAllocDeepBad/bin/$CONFIG/$TFM/HotPathNoAllocDeepBad.dll"
virt_app="samples/dotnet/HotPathNoAllocVirtBad/bin/$CONFIG/$TFM/HotPathNoAllocVirtBad.dll"
mixed_app="samples/dotnet/HotPathNoAllocMixedBad/bin/$CONFIG/$TFM/HotPathNoAllocMixedBad.dll"
nb_out="artifacts/hotpath-noalloc-bad"
rm -rf "$nb_out"; mkdir -p "$nb_out"
if gate_cache_check "$nb_out" "hotpath-noalloc-bad|cli:$(_gate_cli_hash)|$nb_corelib" \
        "$arr_app" "$deep_app" "$virt_app" "$mixed_app"; then
    gate_cache_hit_msg
    exit 0
fi

# assert_noalloc_reject APP DESC PATTERN... — transpiling APP must exit 2, print
# an `error:` line, and its diagnostic must contain every PATTERN (fixed strings).
assert_noalloc_reject() {
    local app="$1" desc="$2"; shift 2
    local rc=0 err pat
    err=$(invoke_cli "$app" -r "$nb_corelib" -o "$nb_out" 2>&1 >/dev/null) || rc=$?
    if [ "$rc" -ne 2 ]; then
        echo "FAIL: transpiling $desc exited $rc (expected 2)" >&2
        echo "$err" >&2
        exit 1
    fi
    if ! grep -q "^error:" <<<"$err"; then
        echo "FAIL: $desc rejection printed no error: line" >&2
        echo "$err" >&2
        exit 1
    fi
    for pat in "$@"; do
        if ! grep -qF -- "$pat" <<<"$err"; then
            echo "FAIL: $desc rejection does not mention '$pat':" >&2
            echo "$err" >&2
            exit 1
        fi
    done
    echo "OK ($desc rejected: $*)"
}

# Depth-1 allocation: new int[n] in the marked body itself.
assert_noalloc_reject "$arr_app" "a direct array allocation" \
    "HotPath(NoAlloc)" "MakeBuffer" "dn2cpp_newarr_"
# Depth-2 allocation: the marked method is clean, its static helper allocates a
# class — the chain must name the intermediate helper Build.
assert_noalloc_reject "$deep_app" "a depth-2 allocation via a helper" \
    "HotPath(NoAlloc)" "Compute" "dn2cpp_alloc" "Build"
# Dynamic dispatch: a virtual/abstract call the verifier cannot prove.
assert_noalloc_reject "$virt_app" "a dynamic dispatch" \
    "HotPath(NoAlloc)" "CountSides" "dispatches dynamically"
# Directly-emitted allocation helpers, one marked method per token family —
# ToString on object, string concat, Substring, multi-dimensional array. None
# lowers to dn2cpp_alloc/dn2cpp_newarr_, so each line is a positive control for
# its own token; the verifier reports all four in one deterministic message.
# The diagnostic prints the matched table TOKEN (a family prefix, e.g.
# dn2cpp_string_concat for the concat2 the body spells), so that is what is
# asserted.
assert_noalloc_reject "$mixed_app" "directly-emitted allocation helpers" \
    "HotPath(NoAlloc)" \
    "StringifyBox" "dn2cpp_object_tostring" \
    "JoinPair" "dn2cpp_string_concat" \
    "ChopFirst" "dn2cpp_str_substring" \
    "CornerSum" "dn2cpp_newmdarr"

gate_cache_commit
echo "OK: NoAlloc verifier rejects direct/deep allocation, dynamic dispatch, and the intrinsic allocation-helper families"
