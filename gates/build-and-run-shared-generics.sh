#!/usr/bin/env bash
# Canonical shared generics gate: transpile the consolidated SharedGenerics
# sample (enum-keyed Dictionary/HashSet/List/Sorted* side by side with int
# instantiations, reference-argument (CnRef) collapsing — Dictionary<int,string>
# and Dictionary<Hue,object> in ONE canonical group, string/record keys through
# the object-path comparers, List<ref> sharing with covariance and exact array
# identity, typeof(T) leak fallbacks, the alias-row collision policy — a user
# IEqualityComparer<TEnum>, generic statics, reflection identity, width-split
# enums) with shared generics on (the default; the explicit --shared-generics
# below also exercises the compat no-op) against the tree-shaken real CoreLib
# and diff the native output exactly vs real .NET — with the shared-body
# symbol backstop armed (DN2CPP_SHARED_ASSERT). The PrimitiveNameShadowSubset
# section's real subject is the ARG-MANGLE's injectivity across its kind and
# array-closure boundaries (CppNaming.MangleFragment): shadow
# classes named "Int32"/"Int32Arr"/"MangleElemArr" vs the primitive, escaped
# and named Arr closures, and a specialization's own Arr-suffixed name — not
# shared-generics sharing per se. Its sibling MangleKindShadowSubset carries the
# same subject over the type-CONSTRUCTOR kinds: MDArray by rank and
# element, Pointer (which is why this bucket's csproj enables AllowUnsafeBlocks),
# both nesting orders of the array closure over them, and a global-namespace
# `class T {}` — the token the mangle's old catch-all printed for every one of
# them. Both live here because this bucket owns the
# instantiation-cache surface those keys feed. Additionally asserts that the
# transpile is deterministic (two runs, byte-identical output), that the
# epic's original example genuinely shares one body set (no per-instantiation
# Dictionary bodies beyond the string-fastpath-tainted ctors; both vtables
# bind the canonical CnInt32/CnRef symbols), that generic-METHOD
# instantiations share canonically too (canonical NameSuffix bodies,
# per-method rgctx tables, per-real forwarders — and the List<ref> group's
# Contains/IndexOf/Remove stay shared instead of cascading through
# Array.IndexOf<T>), that non-async Task producers read each real closed identity
# through rgctx without forfeiting their shared reference-group bodies, and
# that sharing genuinely shrinks the output vs the
# same transpile with --no-shared-generics (total generated*.cpp bytes AND
# the header's method forward-declaration count must be strictly smaller).
#
# Sharing collapses instantiations that COINCIDE; it does not bound how many are
# created, and a generic that calls itself at an ever-deeper type argument creates
# them without end. That bound, and the transpiler's other resource limits, are
# asserted by gates/build-and-run-transpiler-limits.sh.
#
# The last section (GenericMethodSubset, folded from the retired
# build-and-run-generic-method-subset.sh) is NOT about sharing: it is the
# generic-method *pipeline* — Container<T>.Map<U> (a MethodSpec over a MemberRef
# with a TypeSpec parent, instantiated in a context binding both the class and
# the method type args), generic-interface dispatch, and static-virtual generic
# interface dispatch over a value-type TSelf reached through `constrained. call`.
# It lives here because this bucket is where the method dimension of
# monomorphization is asserted, and the move strengthened it twice: the retired
# gate passed no expected string (exit 0 only), so real .NET is a new oracle for
# it, and it now transpiles under --shared-generics with DN2CPP_SHARED_ASSERT
# armed, which it never did standalone.
source "$(dirname "$0")/_common.sh"

project=SharedGenerics
out="artifacts/sharedgenerics"

echo "== 1/7 Locating the real CoreLib =="
corelib=$(locate_corelib)
bcl=$(dirname "$corelib")
echo "corlib: $corelib"

echo "== 2/7 Building app assembly =="
build_proj "samples/dotnet/$project/$project.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"

# Much of this gate is transpiler BEHAVIOR (determinism across two runs, the
# sharing-evidence greps, the on/off size comparison), so the cache key stands
# in for the transpiler itself via _gate_cli_hash (see that helper's doc). The
# out dirs are EMPTIED — cleared and recreated — BEFORE the check, so the key's
# surface term is the stable `no-generated` marker; on a miss the sections below
# rewrite them all. The keyed dir has to EXIST: an absent one is unreadable
# rather than empty, and gate_cache_check answers that with a warning and no key
# which would leave this gate uncacheable since it clears the dirs on
# every run. The transpiler-behavior
# env axis rides in the context for the same reason: an ambient cap, drain
# order, or strict/assert knob changes what the transpiles below do, with no
# surface in the key to catch it.
tenv="tenv:${DN2CPP_MAX_GENERIC_DEPTH:-}/${DN2CPP_MAX_INSTANTIATIONS:-}/${DN2CPP_MAX_HEAP_MB:-}/${DN2CPP_SHARED_ASSERT:-}/${DN2CPP_STRICT_COMPLETION:-}/${DN2CPP_SPEC_DRAIN:-}"
rm -rf "$out" "$out-again" "$out-off"; mkdir -p "$out"
if gate_cache_check "$out" "shared-generics|cli:$(_gate_cli_hash)|$corelib|$tenv" \
        "$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi
refs=(-r "$corelib" -r "$bcl/System.Collections.dll")

echo "== 3/7 Transpiling twice with --shared-generics (determinism) =="
export DN2CPP_SHARED_ASSERT=1
invoke_cli "$app" "${refs[@]}" --shared-generics -o "$out"
invoke_cli "$app" "${refs[@]}" --shared-generics -o "$out-again"
for f in "$out"/generated*; do
    diff "$f" "$out-again/$(basename "$f")"
done
echo "deterministic: OK"

echo "== 4/7 Sharing evidence: Dictionary<int,string> == Dictionary<Hue,object> group =="
# Both instantiations must donate their bodies to the ONE canonical
# Dictionary<$CnInt32,$CnRef> world: no per-instantiation body definitions
# except the ctors (whose typeof(TKey)==typeof(string) comparer fast path is
# type-identity-tainted by design), and both vtables must bind the canonical
# symbols.
for inst in Dictionary_Int32_String Dictionary_DictSharedSubset_Hue_Object; do
    bad=$(grep -hoE "^[A-Za-z_][A-Za-z0-9_* ]* m_System_Collections_Generic_${inst}_[A-Za-z0-9_]*\(" \
        "$out"/generated*.cpp | grep -vE "__ctor|__cctor" || true)
    if [ -n "$bad" ]; then
        echo "FAIL: $inst emits per-instantiation bodies (expected shared canonical):" >&2
        echo "$bad" >&2
        exit 1
    fi
    if ! grep -q "vt_System_Collections_Generic_${inst}\[\].*__CnInt32__CnRef" "$out"/generated*.cpp; then
        echo "FAIL: vt_...${inst} does not bind the canonical CnInt32/CnRef bodies" >&2
        exit 1
    fi
done
if ! grep -q "m_System_Collections_Generic_Dictionary__CnInt32__CnRef_TryInsert" "$out/generated.h"; then
    echo "FAIL: no shared canonical Dictionary<\$CnInt32,\$CnRef> body set emitted" >&2
    exit 1
fi
echo "one shared canonical Dictionary group: OK"

echo "== 4b/7 Method-dimension sharing: canonical generic-method bodies =="
# The generic-method (NameSuffix) cascade is broken: the List<ref> group's
# Contains/IndexOf/Remove bind the one canonical body set (they fall through
# the Array.IndexOf<T> intrinsic, whose CnRef arm compares via
# dn2cpp_object_equals), user generic-method instantiations share canonical
# bodies keyed by canonicalized method arguments, and a context-needing
# instantiation leaves its per-real symbol behind as a one-line forwarder
# appending its per-method rgctx table.
bad=$(grep -hoE "^[A-Za-z_][A-Za-z0-9_* ]* m_System_Collections_Generic_List_(String|Object|RefListSubset_Widget)_(Contains|IndexOf|Remove)_[0-9]+\(" \
    "$out"/generated*.cpp || true)
if [ -n "$bad" ]; then
    echo "FAIL: reference List instantiations emit per-instantiation Contains/IndexOf/Remove:" >&2
    echo "$bad" >&2
    exit 1
fi
if ! grep -q "m_System_Collections_Generic_List__CnRef_IndexOf_" "$out/generated.h"; then
    echo "FAIL: no canonical List<\$CnRef>.IndexOf body" >&2
    exit 1
fi
for sym in "m_MethodShareSubset_Ops_ArrName_[0-9]+___CnRef\(" \
           "rgctx_m_MethodShareSubset_Ops_ArrName_[0-9]+__String"; do
    if ! grep -qE "$sym" "$out/generated.h"; then
        echo "FAIL: method-dimension sharing artifact missing: $sym" >&2
        exit 1
    fi
done
if ! grep -q "rgctx_m_MethodShareSubset_Ops_ArrName" <<<"$(
        grep -A 3 -hE "^[A-Za-z_][A-Za-z0-9_* ]* m_MethodShareSubset_Ops_ArrName_[0-9]+__String\(" \
            "$out"/generated*.cpp)"; then
    echo "FAIL: Ops.ArrName<string> is not a forwarder appending its per-method table" >&2
    exit 1
fi
echo "method-dimension sharing: OK"

echo "== 4c/7 Task producer identities stay shared through rgctx =="
# Each non-async producer runs for Alpha and Beta, which collapse to one CnRef
# method body. Async Task/ValueTask identity is covered by the runtime diff; its
# existing state-machine fallback also keeps each kickoff body concrete.
for method in FromResult ValueTaskResult Source; do
    if ! grep -qE "m_TaskRgctxSubset_Ops_${method}_[0-9]+___CnRef\(" "$out/generated.h"; then
        echo "FAIL: no canonical TaskRgctxSubset.Ops.$method<CnRef> body" >&2
        exit 1
    fi
    for arg in TaskRgctxSubset_Alpha TaskRgctxSubset_Beta; do
        sym="m_TaskRgctxSubset_Ops_${method}_[0-9]+__${arg}"
        if ! grep -qE "rgctx_${sym}" "$out/generated.h"; then
            echo "FAIL: TaskRgctxSubset.Ops.$method<$arg> has no rgctx table" >&2
            exit 1
        fi
        if ! grep -A 3 -hE "^[A-Za-z_][A-Za-z0-9_* ]* ${sym}\(" \
                "$out/generated.h" | grep -qE "rgctx_${sym}"; then
            echo "FAIL: TaskRgctxSubset.Ops.$method<$arg> is not an rgctx forwarder" >&2
            exit 1
        fi
    done
done
echo "Task producer rgctx sharing: OK"

echo "== 4d/7 typeof(T)==typeof(X) fold prunes cross-product instantiations =="
# TypeofFoldSubset.Dispatch<T> chains `if (typeof(T) == typeof(X)) return
# Tag<T, X>();` over five X — folding the comparison at transpile time must
# leave only the DIAGONAL Tag instantiations reachable, so no Tag<int, not-int>
# body or declaration may exist anywhere in the output.
#
# Asserted in BOTH configs. This section used to run under Release only, on the
# claim that "a Debug build routes the comparison through a local, which the
# strict six-instruction window deliberately does not fold" — which made it a
# section that silently did nothing on every run of the Debug suite, announced
# by a lowercase `skipped` the runner's SKIP tripwire cannot see. Measured:
# a CONFIG=Debug transpile emits exactly the five diagonal
# Tag instantiations and no off-diagonal one, so both asserts below hold
# verbatim there and the carve-out was buying nothing. Retired rather than
# declared, for the reason the pinvoke-wasm Windows-ACP declaration was retired:
# a carve-out is evidence that somebody stopped, not that the hole is permanent.
bad=$(grep -hoE "m_TypeofFoldSubset_Program_Tag_[0-9]+__Int32_[A-Za-z0-9_]+" "$out/generated.h" \
    | grep -vE "__Int32_Int32$" || true)
if [ -n "$bad" ]; then
    echo "FAIL: off-diagonal Tag<int,*> instantiations survived the typeof fold:" >&2
    echo "$bad" >&2
    exit 1
fi
if ! grep -qE "m_TypeofFoldSubset_Program_Tag_[0-9]+__Int32_Int32\(" "$out/generated.h"; then
    echo "FAIL: diagonal Tag<int,int> body missing (grep pattern rot?)" >&2
    exit 1
fi
echo "typeof fold prunes off-diagonal instantiations: OK"

echo "== 4e/7 Generic-virtual dispatch over a canonical group owner =="
# The emit-set invariant of the dn2cpp_gvm_* dispatcher, and the widest thing
# that walks the type-info funnel's refusal path. A generic virtual has no
# vtable slot, so a callvirt routes through a type-switch whose cases are the
# ALLOCATED implementing types — and under shared generics that set contains
# CANONICAL GROUP OWNERS, which carry no ti_ of their own. Emitting the branch
# anyway made the transpile report success and the C++ compile fail on an
# undeclared identifier. GvmCanonicalSubset builds that state out of the
# sample's own types; the header of that file records what each ingredient is
# for and how each was measured.
#
# The transpile above IS the assert (CppEmitter.TypeInfoRef throws on an
# undefined handle). These greps do two things a throw cannot: they report a
# regression as itself rather than as a clang error, and — more importantly —
# they pin that the SHAPE is still present, because every one of the three
# ingredients can dissolve silently under a refactor and leave a section that
# compiles a dispatcher and asserts nothing.
#
#   (a) the group owners exist and are shared     -> canonical Clone bodies
#   (b) the owners have no type-info of their own -> no ti_ ..._Row__Cn* anywhere
#   (c) the owners INHERIT a reachable GVM impl,
#       which is what keeps them in the case set  -> ChainBase<Row>.Then bodies
for sym in "m_GvmCanonicalSubset_Chain_GvmCanonicalSubset_Row__CnRef_Clone_[0-9]+\(" \
           "m_GvmCanonicalSubset_Chain_GvmCanonicalSubset_Row__CnInt32_Clone_[0-9]+\(" \
           "m_GvmCanonicalSubset_ChainBase_GvmCanonicalSubset_Row_Then_[0-9]+__String\(" \
           "dn2cpp_gvm_GvmCanonicalSubset_IChain_GvmCanonicalSubset_Row_Then_[0-9]+__Int32\("; do
    if ! grep -qE "$sym" "$out/generated.h"; then
        echo "FAIL: the GVM/canonical-owner shape has dissolved — missing: $sym" >&2
        exit 1
    fi
done
bad=$(grep -hoE "ti_GvmCanonicalSubset_Chain_GvmCanonicalSubset_Row__Cn[A-Za-z0-9]+" \
    "$out/generated.h" "$out"/generated*.cpp | sort -u || true)
if [ -n "$bad" ]; then
    echo "FAIL: a canonical group owner grew a type-info — the section no longer" >&2
    echo "      tests what it exists to test (found: $bad)" >&2
    exit 1
fi
# The invariant itself, over the WHOLE corpus rather than this one dispatcher:
# every type-info a dispatcher case branches on must be header-declared.
syms=$(grep -hoE 'if \(__t == &ti_[A-Za-z0-9_]+\)' "$out"/generated*.cpp \
    | sed 's/.*&//; s/)$//' | sort -u)
if [ -z "$syms" ]; then
    echo "FAIL: no generic-virtual dispatcher case found in $out (grep pattern rot?)" >&2
    exit 1
fi
missing=""
for sym in $syms; do
    grep -q "^extern const Dn2CppTypeInfo $sym;\$" "$out/generated.h" || missing="$missing $sym"
done
if [ -n "$missing" ]; then
    echo "FAIL: generic-virtual dispatcher branches on undeclared type-info:$missing" >&2
    exit 1
fi
# ... and the REAL cases survive the filter: a dispatcher that dropped them
# along with the dead canonical branch would trap instead of dispatching.
for sym in ti_GvmCanonicalSubset_Chain_GvmCanonicalSubset_Row_String \
           ti_GvmCanonicalSubset_Chain_GvmCanonicalSubset_Row_Int32; do
    if ! grep -q "if (__t == &$sym)" "$out"/generated*.cpp; then
        echo "FAIL: real dispatcher case missing: $sym" >&2
        exit 1
    fi
done
echo "gvm dispatcher over a canonical group: shape present, all cases declared: OK"

echo "== 5/7 Transpiling with --no-shared-generics (size regression check) =="
invoke_cli "$app" "${refs[@]}" --no-shared-generics -o "$out-off"
on_bytes=$(cat "$out"/generated*.cpp | wc -c | tr -d ' ')
off_bytes=$(cat "$out-off"/generated*.cpp | wc -c | tr -d ' ')
on_decls=$(grep -c ' m_' "$out/generated.h")
off_decls=$(grep -c ' m_' "$out-off/generated.h")
echo "generated*.cpp bytes: on=$on_bytes off=$off_bytes"
echo "method decls (generated.h): on=$on_decls off=$off_decls"
if [ "$on_bytes" -ge "$off_bytes" ]; then
    echo "FAIL: --shared-generics output is not smaller ($on_bytes >= $off_bytes bytes)" >&2
    exit 1
fi
if [ "$on_decls" -ge "$off_decls" ]; then
    echo "FAIL: --shared-generics emits no fewer method bodies ($on_decls >= $off_decls decls)" >&2
    exit 1
fi

echo "== 6/7 Compiling C++ =="
compile_console "$out" "$project"

echo "== 7/7 Running (exact diff vs real .NET) =="
# Exit statuses captured explicitly (`$(...)` inline would swallow them) and
# compared: a binary aborting after printing everything must not pass.
set +e
native=$("./$out/$project"); native_code=$?
expected=$(dotnet "$app"); expected_code=$?
set -e
assert_output "$native" "$expected"
assert_exit_code "$native_code" "$expected_code"
gate_cache_commit
