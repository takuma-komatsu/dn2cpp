#!/usr/bin/env bash
# Companion process preservation, delegate callable bodies, GC collector
# boundaries and reference moves through the console-self-host primitives.
#
#  #3 Interop.GetRandomBytes -> the non-cryptographic random source behind
#     System.HashCode's process-global seed (the InternalCall
#     Interop+Sys::GetNonCryptographicallySecureRandomBytes, intercepted at the IL
#     forwarder Interop.GetRandomBytes and filled deterministically with a fixed
#     seed). A Dictionary / HashSet keyed on ValueTuple<string,string> drives
#     ValueTuple.GetHashCode -> HashCode.Combine -> GenerateGlobalSeed ->
#     GetRandomBytes. A fixed seed keeps every observed result identical to real
#     .NET: key lookups are hash-value-independent, and Dictionary/HashSet
#     enumeration is insertion order (no removals). Exercised end-to-end here.
#
#  #5 Exception.get_StackTrace -> null carve-out. An UN-THROWN exception has a null
#     StackTrace in real .NET too, so `ex.StackTrace is null` is exact in both. (A
#     thrown exception's StackTrace is an intentional divergence — native has no
#     stack-trace model — and is deliberately NOT exercised here.)
#
#  #4 AppContext.BaseDirectory / Environment.ProcessPath resolve the process image
#     (the PAL's executable-path query), the way NativeAOT does — a native binary has
#     no managed entry assembly whose Location it could derive them from. The strings
#     differ between the two runtimes by construction (`dotnet app.dll` reports the
#     dll's directory and the dotnet host's path), so the section asserts the
#     properties both must satisfy instead: the base directory exists, is rooted and
#     ends in a separator; ProcessPath is non-null, rooted and names a real file.
#
#  MiscIntrinsicSubset — the misc intrinsics dn2cpp's own
#     code reaches but that had no intrinsic mapping. Its real subjects are wider than
#     the section name suggests, and none of them is "reflection" or "math":
#       * Double/Single IsNaN / IsPositiveInfinity / IsNegativeInfinity (MethodCompiler
#         maps the special float constants to the C++ math macros; the System.Numerics
#         generic-math forms were already handled in TryEmitGenericMathIntrinsic);
#       * the static Object.Equals(objA, objB) (MethodCompiler.BranchTo merges the
#         per-slot StaticType record across a join) — reuses dn2cpp_object_equals,
#         whose null/reference/virtual-Equals semantics match the static overload;
#       * System.Numerics.BitOperations.{TrailingZeroCount, LeadingZeroCount, PopCount,
#         Log2} over int/uint/long/ulong, lowered to the ctz/clz/popcount clang builtins
#         at the argument's width (the nint/nuint overloads stay BCL-as-IL);
#       * Debugger.IsAttached, and the Trace/Debug write path: Debugger.IsLogging
#         const-folds false (NativeAOT parity), which prunes DebugProvider's
#         Debugger.Log arm and leaves the transpiled Interop.Sys.SysLog leaf — so a
#         Trace.WriteLine links the SystemNative_SysLog PAL entry and, like a direct
#         Debugger.Log, prints nothing. Trace puts System.Diagnostics.TraceSource on
#         the -r set below, and TraceListener.Attributes pulls
#         System.Collections.Specialized in behind it;
#       * the GC.CollectionCount / GetGCMemoryInfo bindings over
#         the vendored Boehm GC, and AssemblyLoadContext.LoadFromAssemblyPath's trap;
#       * the Thrive startup shape AppDomain.CurrentDomain.GetAssemblies().Where(...)
#         .Count() — the degraded set must carry the DECLARED Assembly[] runtime
#         identity, which is why System.Linq is on the -r set below;
#       * two null-receiver runtime helpers (dn2cpp_unbox and
#         dn2cpp_task_exception), which must raise a CATCHABLE NullReferenceException
#         rather than abort — i.e. the section's tail is an EH-recovery test.
#     It runs after the sections above, which must not inherit its collection.
#
#  GcAllocatedBytesSubset — GC.GetAllocatedBytesForCurrentThread and
#     GC.GetTotalAllocatedBytes(bool), lowered to the runtime's thread_local
#     accounting at the three dn2cpp_alloc entry points. Raw counts differ
#     (dn2cpp counts requested bytes, pinned included), so the section asserts
#     invariants: the per-thread counter is non-negative, monotone, covers a
#     1 MiB allocation, stays flat across a quiet window and across 8 MiB
#     allocated on another Thread — the line that separates per-thread
#     accounting from a process-wide approximation. The approximate total is
#     asserted non-negative and monotone ONLY: real .NET's figure reads stale
#     per-thread buffers and a fresh 1 MiB may not appear. It is also read while
#     a worker allocates, covering the synchronized Boehm statistics path. The
#     precise total has no such carve-out and must cover the 1 MiB. It sits in
#     this bucket because it starts a Thread: this gate is the bucket's sole
#     driver, so no wasm axis reaches the section.
#
# GC validation and wrapper behavior compare to the CLR; logical generation and
# unavailable notification/finalizer-worker behavior use native-only contracts.
# Buffer copies assert overlap and reference survival plus collector-aware
# lowering. A separate Gen2 base reuses one image for reflection and both patch
# codecs; minimal metadata and malformed inputs need emission only.
#
#  The reflection sections after BufferReferenceMoveSubset (RuntimeType queries,
#     activation, type/field/method handles, GC queries, delegates and
#     known-constant searches) reach private CoreLib members that Medium
#     retains, through public APIs only.
# Driven by the real System.Private.CoreLib (passed with -r) -> cross-assembly resolve
# + tree-shake -> native binary -> run, asserting the output diffs exact vs real .NET.
source "$(dirname "$0")/_common.sh"
prepare_cli
source "$(dirname "$0")/_delegate-callable-bodies.sh"
source "$(dirname "$0")/_gc-collector-contracts.sh"
source "$(dirname "$0")/_gen2-gate.sh"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS gates/fixtures/ldftn-local/RuntimeTypeIsPointerFixture.cs gates/fixtures/ldftn-local/RuntimeFieldHandleFixture.cs"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|runtime-type-routes:aggregate,public-wrapper-oracle,fieldhandle-boundary"

gate_prepare_app() {
    local out="$2" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    gc_collector_capture_raw_app "$2" || return 1
    # The CLR must use the original public wrappers, not the private native routes.
    gate_capture_ok "$out/runtime-type-oracle" run_bounded dotnet "$_CG_APP" runtime-type-routes || return 1
    DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS $out/runtime-type-oracle.stdout $out/runtime-type-oracle.stderr"
    delegate_callable_prepare_app "$@" || return 1
    gc_collector_prepare_app "$2" || return 1
    gate_capture_ok "$out/runtime-field-fixture" run_bounded dotnet "$fixture" \
        --runtime-field-handle "$_CG_APP" "$_CG_CORELIB" || return 1
    gate_assert_text "$out/runtime-field-fixture.stdout" 'field-handle-sites=GetAttributes-direct' || return 1
    gate_assert_text "$out/runtime-field-fixture.stderr" '' || return 1
    gate_capture_ok "$out/runtime-type-fixture" run_bounded dotnet "$fixture" \
        --runtime-type-is-pointer-aggregate "$_CG_APP" "$_CG_CORELIB" || return 1
    gate_assert_text "$out/runtime-type-fixture.stdout" 'runtime-type-is-pointer-entry:Program.Main' || return 1
    gate_assert_text "$out/runtime-type-fixture.stderr" ''
}

# GC capabilities and delegate callable refusals use isolated processes of the
# driver image; the ordinary run retains its unchanged GC and Buffer prefix.
gate_extra_asserts() {
    local out="$1"
    gate_output witness "$out/native.stdout" 'Nullable reboxing contract end' \
        'Activator default construction end' 'GC memory query entry points end' \
        'Runtime method enumeration contract end' 'Runtime type handle accessors end' 'Principal public neighbor end' \
        'known-constant-public-searches:True' 'known-constant-search-values:70:70:0:-1:-1' 'Known constant public searches end' \
        --count 1 --policy crlf || return 1
    gate_capture_ok "$out/nullable-before" run_bounded dotnet "$_CG_APP" before-nullable-reboxing || return 1
    gate_output select "$out/native.stdout" "$out/nullable-prefix.stdout" \
        --before '== Nullable reboxing contract ==' --policy crlf || return 1
    gate_output compare "$out/nullable-prefix.stdout" "$out/nullable-before.stdout" || return 1
    gate_capture_ok "$out/runtime-prefix" run_bounded dotnet "$_CG_APP" before-runtime-primitives || return 1
    gate_output prefix "$out/native.stdout" "$out/runtime-prefix.stdout" \
        --before '== Runtime method enumeration contract ==' --policy crlf || return 1
    delegate_callable_asserts "$out"
    delegate_callable_retained_only "$out"
    gc_collector_asserts "$out"
    gen2_retained_boundary_gate "$out/gen2-retained"
    # Private call, calli, virtual, address and reflection routes share this
    # image, but run separately so the ordinary transcript is unchanged.
    gate_capture_ok "$out/runtime-type-native" run_bounded "$out/SelfHostPrimSubset$EXE_EXT" runtime-type-routes
    gate_output witness "$out/runtime-type-native.stdout" 'Runtime is-pointer routes end' --count 1 --policy crlf
    gate_output compare "$out/runtime-type-oracle.stdout" "$out/runtime-type-native.stdout"
    gate_output compare "$out/runtime-type-oracle.stderr" "$out/runtime-type-native.stderr"
    gate_capture_ok "$out/runtime-field-boundary" run_bounded "$out/SelfHostPrimSubset$EXE_EXT" native-boundary
    gate_output byte-witness "$out/runtime-field-boundary.stdout" 'field-handle-GetAttributes-direct:PlatformNotSupportedException:System.RuntimeFieldHandle.GetAttributes requires CoreCLR field descriptors. Use FieldInfo instead.' --count 1 --policy crlf
}

corelib_diff_gate SelfHostPrimSubset System.Linq System.Diagnostics.TraceSource \
    System.Collections.Specialized System.Security.Claims \
    -r "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/DefaultStructCtorLib.dll"

# The real runner source is linked into this small fixture so this block proves
# its MethodDef interception independently of a full self-host native build.
probe_project=gates/fixtures/tool-process/ToolProcessProbe.csproj
build_gate_proj "$probe_project"
probe_bin="$PWD/gates/fixtures/tool-process/bin/$CONFIG/$TFM"
probe_out="artifacts/selfhost-tool-process-$CONFIG"
tool_process_diagnostics() {
    [ "${_GATE_EXIT_RC:-0}" -ne 0 ] || return 0
    local log
    for log in "$probe_out"/*.out "$probe_out"/*.err; do
        [ -f "$log" ] || continue
        printf '\n---- %s ----\n' "$log" >&2
        cat "$log" >&2
    done
}
gate_add_exit_hook tool_process_diagnostics
mkdir -p "$probe_out/child path 日本語"
cp -R "$probe_bin/." "$probe_out/child path 日本語/"
probe_child="$PWD/$probe_out/child path 日本語/ToolProcessProbe$EXE_EXT"
invoke_cli "$probe_bin/ToolProcessProbe.dll" --auto-ref -o "$probe_out/emitted"
compile_console "$probe_out/emitted" ToolProcessProbe
run_bounded "$PWD/$probe_out/emitted/ToolProcessProbe$EXE_EXT" "$probe_child" \
    > "$probe_out/native.out" 2> "$probe_out/native.err"
run_bounded dotnet "$probe_bin/ToolProcessProbe.dll" "$probe_child" \
    > "$probe_out/managed.out" 2> "$probe_out/managed.err"
diff -u "$probe_out/managed.out" "$probe_out/native.out"
diff -u "$probe_out/managed.err" "$probe_out/native.err"
grep -Fx 'child exit 23' "$probe_out/native.out"
grep -Fx 'launch failure caught' "$probe_out/native.out"
grep -Fx 'NUL argument rejected' "$probe_out/native.out"
grep -Fx 'tool process complete' "$probe_out/native.out"
grep -Fx 'child stderr' "$probe_out/native.err"

# Bare names must resolve through PATH in both host and native launchers.
PATH="$PWD/$probe_out/child path 日本語:$PATH" \
    run_bounded "$PWD/$probe_out/emitted/ToolProcessProbe$EXE_EXT" "ToolProcessProbe$EXE_EXT" \
    > "$probe_out/path-native.out" 2> "$probe_out/path-native.err"
diff -u "$probe_out/managed.out" "$probe_out/path-native.out"
diff -u "$probe_out/managed.err" "$probe_out/path-native.err"

source "$(dirname "$0")/_delegate-signatures.sh"
delegate_signature_regressions

fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
corelib=$(resolve_net10_corelib)
prim_bin="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM"
routes="artifacts/selfhost-prim-routes-$CONFIG"
mkdir -p "$routes"
# The compile-only refusals read original sample outputs; the positive routes
# are already covered by the aggregate cache and need no second native image.
route_inputs=()
while IFS= read -r f; do route_inputs+=("$f"); done < <(git ls-files -co --exclude-standard -- \
    gates/fixtures/ldftn-local gates/fixtures/default-principal)
if gate_cache_check "$routes" "selfhost-prim-route-refusals|cli:$(_gate_cli_hash)|corelib:$corelib|flags:--auto-ref,--no-ildiet" \
        "$prim_bin/"* "${route_inputs[@]}"; then
    gate_cache_hit_msg
    exit 0
fi
routes=$(mktemp -d "$routes/run.XXXXXX")
echo "RuntimeType refusal artifacts: $routes"
mkdir -p "$routes/negative-input" "$routes/raw-input" "$routes/claims/copy"
cp "$prim_bin/"* "$routes/negative-input/"
cp "$prim_bin/"* "$routes/raw-input/"

# Malformed inputs are compile-only and fail closed: an ldftn of the pointer query
# with a wrong return type, a raw call whose return names Boolean as a class (the
# refusal must not fall back to the intrinsic table), a missing Claims
# implementation for the default principal, a Claims copy whose
# GenericPrincipal.GetDefaultInstance returns object, and the unrewritten
# principal probe, whose entry takes a typed GCHandle member's address.
refusal() {
    local name="$1" diagnostic="$2"
    shift 2
    gate_capture "$routes/$name" invoke_cli "$@" -o "$routes/$name-out"
    gate_output status "$(cat "$routes/$name.status")" 2
    gate_output match-record "$routes/$name.stderr" ".*error: $diagnostic\$" --count 1 --policy crlf
}
dotnet "$fixture" --runtime-type-is-pointer-negative-address-return "$routes/negative-input/SelfHostPrimSubset.dll" "$corelib"
refusal pointer-memberref 'RuntimeTypeIsPointerSubset[.]IsPointerChecks[.]Address: RuntimeType pointer query requires its exact MemberRef signature \[chain: .*\]' \
    "$routes/negative-input/SelfHostPrimSubset.dll" --auto-ref
dotnet "$fixture" --runtime-type-is-pointer-negative-call-class-return "$routes/raw-input/SelfHostPrimSubset.dll" "$corelib"
refusal pointer-call-raw 'RuntimeTypeIsPointerSubset[.]IsPointerChecks[.]Direct: RuntimeType pointer query requires its exact MemberRef signature \[chain: .*\]' \
    "$routes/raw-input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet
build_gate_proj gates/fixtures/default-principal/ImplicitPrincipalProbe.csproj
cp "gates/fixtures/default-principal/bin/$CONFIG/$TFM/"* "$routes/claims/"
dotnet "$fixture" --default-principal "$routes/claims/ImplicitPrincipalProbe.dll" "$corelib"
dotnet "$fixture" --default-principal-target-copy "$(dirname "$corelib")/System.Security.Claims.dll" \
    "$routes/claims/copy/System.Security.Claims.dll" wrong-return
refusal principal-missing 'default principal requires the System[.]Security[.]Claims implementation assembly' \
    "$routes/claims/ImplicitPrincipalProbe.dll" -r "$corelib" \
    -r "$(dirname "$corelib")/System.Runtime.dll" -r "$(dirname "$corelib")/System.Console.dll"
refusal principal-factory 'default principal requires the exact managed GenericPrincipal[.]GetDefaultInstance factory' \
    "$routes/claims/ImplicitPrincipalProbe.dll" --auto-ref -r "$routes/claims/copy/System.Security.Claims.dll"
refusal typed-gchandle-address "Entry[.]Main: System[.]Runtime[.]InteropServices[.]GCHandle_Object::ToIntPtr: typed GCHandle<T> members are lowered only at call sites; taking a member's address is not supported, so call it from a lambda instead \[chain: Entry[.]Main\]" \
    "gates/fixtures/default-principal/bin/$CONFIG/$TFM/ImplicitPrincipalProbe.dll" --auto-ref
gate_cache_commit
