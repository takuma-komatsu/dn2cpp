#!/usr/bin/env bash
# CoreCLR method/type-handle boundaries, public reflection, and companion process
# argument/stream/exit preservation and the console-self-host
# runtime primitives whose behaviour can be exercised exactly.
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
#     It is driven LAST, after the sections above: it forces a GC.Collect() and prints
#     process-wide GC facts, so nothing else may inherit its collection.
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
# Driven by the real System.Private.CoreLib (passed with -r) -> cross-assembly resolve
# + tree-shake -> native binary -> run, asserting the output diffs exact vs real .NET.
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/fixtures/ldftn-local/RuntimeTypeMaterializationFixture.cs gates/fixtures/ldftn-local/RuntimeMethodHandleFixture.cs gates/fixtures/ldftn-local/RuntimeMethodEnumerationFixture.cs gates/fixtures/ldftn-local/Program.cs gates/fixtures/ldftn-local/GcFinalizerWorkerFixture.cs gates/fixtures/ldftn-local/DelegateVmConstructorFixture.cs gates/fixtures/ldftn-local/DelegateNamedConstructorFixture.cs gates/fixtures/ldftn-local/LdftnLocalFixture.csproj"
source "$(dirname "$0")/_common.sh"

method_handle_members="GetAttributes GetImplAttributes GetMethodTable GetSlot GetMethodDef GetUtf8NameInternal HasMethodInstantiation GetStubIfNeededInternal GetMethodFromCanonical IsGenericMethodDefinition IsTypicalMethodDefinition GetGenericParameterCount IsDynamicMethod GetResolver IsConstructor GetLoaderAllocatorInternal"
method_handle_shapes() {
    local output="$1" member
    for member in $method_handle_members; do
        gate_output byte-witness "$output" "method-handle-shape-$member:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Runtime method handle metadata end' --count 1 --policy crlf
}

type_materialize_asserts() {
    local output="$1" label
    for label in public-roundtrip public-array-roundtrip public-default public-value public-object public-base public-invoke shape-method-table shape-method-table-parameter shape-handle shape-slow shape-qcall shape-maybe-null maybe-null-zero; do
        gate_output byte-witness "$output" "type-materialize-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Runtime type materialization contract end' --count 1 --policy crlf
}

named_ctor_asserts() {
    local output="$1" owner form label check
    for owner in Delegate MulticastDelegate; do
        for form in instance static; do
            label="named-$owner-$form"
            for check in bind identity target method list; do
                gate_output byte-witness "$output" "$label-$check:True" --count 1 --policy crlf || return 1
            done
            gate_output byte-witness "$output" "$label-target-null:ArgumentNullException:target" --count 1 --policy crlf || return 1
            gate_output byte-witness "$output" "$label-method-null:ArgumentNullException:method" --count 1 --policy crlf || return 1
        done
        gate_output byte-witness "$output" "named-$owner-static-initializer:True" --count 1 --policy crlf || return 1
        gate_output byte-witness "$output" "named-$owner-static-open:ArgumentException:target" --count 1 --policy crlf || return 1
        gate_output byte-witness "$output" "named-$owner-static-no-match:True" --count 1 --policy crlf || return 1
        gate_output byte-witness "$output" "named-$owner-instance-failed-unchanged:True" --count 1 --policy crlf || return 1
        for check in case static-only signature; do
            gate_output byte-witness "$output" "named-$owner-instance-$check:ArgumentException:" --count 1 --policy crlf || return 1
        done
    done
    gate_output byte-witness "$output" 'Retained delegate named constructors end' --count 1 --policy crlf || return 1
}

# Heap accounting differs by runtime; assert the public query contracts.
gate_extra_asserts() {
    local out="$1" native="$1/native.stdout" line
    gate_capture_ok "$out/assert-before-gc-memory" run_bounded dotnet "$_CG_APP" before-gc-memory-queries || return 1
    gate_output select "$native" "$out/assert-prefix-gc-memory.stdout" --before '== GC memory query entry points ==' --policy crlf || return 1
    gate_output compare "$out/assert-prefix-gc-memory.stdout" "$out/assert-before-gc-memory.stdout" || return 1
    for line in '== GC memory query entry points ==' \
        'gc-public-direct-nonnegative:True' 'gc-public-delegate-nonnegative:True' \
        'gc-public-pointer-nonnegative:True' 'gc-public-collect-nonnegative:True' \
        'gc-live-payload:True' 'GC memory query entry points end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/assert-before-gc-generations" run_bounded dotnet "$_CG_APP" before-gc-generations || return 1
    gate_output select "$native" "$out/assert-prefix-gc-generations.stdout" --before '== GC logical generations ==' --policy crlf || return 1
    gate_output compare "$out/assert-prefix-gc-generations.stdout" "$out/assert-before-gc-generations.stdout" || return 1
    for line in '== GC logical generations ==' \
        'gc-generation-max-valid:True' 'gc-generation-direct:True' \
        'gc-generation-delegate:True' 'gc-generation-pointer:True' \
        'gc-generation-weak:True' 'gc-generation-collected:True' \
        'gc-generation-weak-collected:True' 'gc-generation-null:ArgumentNullException:obj' \
        'gc-generation-delegate-null:ArgumentNullException:obj' \
        'gc-generation-null-weak:NullReferenceException:' \
        'gc-generation-empty-weak:ArgumentNullException:wo' 'GC logical generations end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/assert-before-gc-collections" run_bounded dotnet "$_CG_APP" before-gc-collections || return 1
    gate_output select "$native" "$out/assert-prefix-gc-collections.stdout" --before '== GC collection overloads ==' --policy crlf || return 1
    gate_output compare "$out/assert-prefix-gc-collections.stdout" "$out/assert-before-gc-collections.stdout" || return 1
    for line in '== GC collection overloads ==' 'gc-collect-routes:True' \
        'gc-collect-counter-advanced:True' 'gc-collect-live-payload:True' \
        'gc-collect-negative:ArgumentOutOfRangeException:generation' \
        'gc-collect-negative-first:ArgumentOutOfRangeException:generation' \
        'gc-collect-mode-low:ArgumentOutOfRangeException:mode' \
        'gc-collect-mode-high:ArgumentOutOfRangeException:mode' \
        'gc-collect-aggressive-generation:ArgumentException:generation' \
        'gc-collect-aggressive-blocking:ArgumentException:blocking' \
        'gc-collect-aggressive-compacting:ArgumentException:compacting' 'GC collection overloads end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/assert-before-gc-worker" run_bounded dotnet "$_CG_APP" before-gc-worker-contract || return 1
    gate_output select "$native" "$out/assert-prefix-gc-worker.stdout" --before '== GC finalizer worker contract ==' --policy crlf || return 1
    gate_output compare "$out/assert-prefix-gc-worker.stdout" "$out/assert-before-gc-worker.stdout" || return 1
    for line in '== GC finalizer worker contract ==' 'gc-worker-private:True' \
        'gc-worker-static:True' 'gc-worker-nongeneric:True' 'gc-worker-uint32:True' \
        'gc-worker-zero:True' 'GC finalizer worker contract end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/assert-before-gc-notifications" run_bounded dotnet "$_CG_APP" before-gc-notifications || return 1
    gate_output select "$native" "$out/assert-prefix-gc-notifications.stdout" --before '== GC notification validation ==' --policy crlf || return 1
    gate_output compare "$out/assert-prefix-gc-notifications.stdout" "$out/assert-before-gc-notifications.stdout" || return 1
    for line in '== GC notification validation ==' \
        'fullgc-max-zero:ArgumentOutOfRangeException:maxGenerationThreshold:Argument must be between 1 and 99. (Parameter '"'"'maxGenerationThreshold'"'"')' \
        'fullgc-max-hundred:ArgumentOutOfRangeException:maxGenerationThreshold:Argument must be between 1 and 99. (Parameter '"'"'maxGenerationThreshold'"'"')' \
        'fullgc-loh-zero:ArgumentOutOfRangeException:largeObjectHeapThreshold:Argument must be between 1 and 99. (Parameter '"'"'largeObjectHeapThreshold'"'"')' \
        'fullgc-loh-hundred:ArgumentOutOfRangeException:largeObjectHeapThreshold:Argument must be between 1 and 99. (Parameter '"'"'largeObjectHeapThreshold'"'"')' \
        'fullgc-max-first:ArgumentOutOfRangeException:maxGenerationThreshold:Argument must be between 1 and 99. (Parameter '"'"'maxGenerationThreshold'"'"')' \
        'fullgc-approach-negative:ArgumentOutOfRangeException:millisecondsTimeout:millisecondsTimeout ('"'"'-2'"'"') must be greater than or equal to '"'"'-1'"'"'. (Parameter '"'"'millisecondsTimeout'"'"')' \
        'fullgc-complete-negative:ArgumentOutOfRangeException:millisecondsTimeout:millisecondsTimeout ('"'"'-2'"'"') must be greater than or equal to '"'"'-1'"'"'. (Parameter '"'"'millisecondsTimeout'"'"')' \
        'fullgc-approach-timespan:ArgumentOutOfRangeException:timeout:timeout ('"'"'-2'"'"') must be greater than or equal to '"'"'-1'"'"'. (Parameter '"'"'timeout'"'"')' \
        'fullgc-complete-timespan:ArgumentOutOfRangeException:timeout:timeout ('"'"'-2'"'"') must be greater than or equal to '"'"'-1'"'"'. (Parameter '"'"'timeout'"'"')' \
        'fullgc-approach-overflow:ArgumentOutOfRangeException:timeout:timeout ('"'"'2147483648'"'"') must be less than or equal to '"'"'2147483647'"'"'. (Parameter '"'"'timeout'"'"')' \
        'fullgc-complete-overflow:ArgumentOutOfRangeException:timeout:timeout ('"'"'2147483648'"'"') must be less than or equal to '"'"'2147483647'"'"'. (Parameter '"'"'timeout'"'"')' \
        'GC notification validation end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/assert-before-gc-pause" run_bounded dotnet "$_CG_APP" before-gc-pause || return 1
    gate_output prefix "$native" "$out/assert-before-gc-pause.stdout" --before '== GC pause duration ==' --policy crlf || return 1
    for line in '== GC pause duration ==' 'pause-nonnegative:True' 'pause-monotonic:True' \
        'pause-advanced:True' 'pause-delegate:True' 'pause-pointer:True' 'GC pause duration end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/assert-before-delegate-values" run_bounded dotnet "$_CG_APP" before-delegate-values || return 1
    gate_output prefix "$native" "$out/assert-before-delegate-values.stdout" --before '== Retained delegate value members ==' --policy crlf || return 1
    for line in '== Retained delegate value members ==' 'delegate-same:True' 'delegate-same-hash:True' \
        'delegate-closed-type:True' 'delegate-declaration:True' 'delegate-null:True' \
        'delegate-target-same:True' 'delegate-target-same-hash:True' 'delegate-target-other:True' \
        'delegate-chain:True' 'delegate-chain-hash:True' 'delegate-order:True' 'delegate-length:True' \
        'delegate-reflected-equals:True' 'delegate-reflected-hash:True' \
        'delegate-base-equals:True' 'delegate-base-hash:True' \
        'delegate-bound-equals:True' 'delegate-bound-hash:True' 'Retained delegate value members end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/assert-before-multicast-factory" run_bounded dotnet "$_CG_APP" before-multicast-factory || return 1
    gate_output prefix "$native" "$out/assert-before-multicast-factory.stdout" --before '== Retained multicast construction ==' --policy crlf || return 1
    for line in '== Retained multicast construction =='         'factory-order-0:True' 'factory-identity-0:True' 'factory-entries-0:True' 'factory-prefix-0:True'         'factory-order-1:True' 'factory-identity-1:True' 'factory-entries-1:True' 'factory-prefix-1:True'         'factory-order-2:True' 'factory-identity-2:True' 'factory-entries-2:True' 'factory-prefix-2:True'         'factory-closed-type:True' 'factory-public-transitions:True' 'Retained multicast construction end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/factory-boundary" run_bounded "./$out/SelfHostPrimSubset$EXE_EXT" native-multicast-factory-boundary || return 1
    for line in 'factory-boundary-0:True' 'factory-boundary-1:True'; do
        gate_output byte-witness "$out/factory-boundary.stdout" "$line" --count 1 --policy crlf || return 1
    done

    gate_capture_ok "$out/assert-before-multicast-operations" run_bounded dotnet "$_CG_APP" before-multicast-operations || return 1
    gate_output prefix "$native" "$out/assert-before-multicast-operations.stdout" --before '== Retained multicast operations ==' --policy crlf || return 1
    for line in '== Retained multicast operations ==' \
        'multicast-combine-order:True' 'multicast-combine-identity:True' 'multicast-null-noop:True' \
        'multicast-no-match:True' 'multicast-remove-last:True' 'multicast-source-immutable:True' \
        'multicast-remove-transitions:True' 'multicast-remove-longer:True' \
        'multicast-protected-mismatch:True' 'multicast-public-mismatch:True' \
        'multicast-closed-types:True' 'Retained multicast operations end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done

    gate_capture_ok "$out/assert-before-multicast-accessors" run_bounded dotnet "$_CG_APP" before-multicast-accessors || return 1
    gate_output prefix "$native" "$out/assert-before-multicast-accessors.stdout" --before '== Retained multicast accessors ==' --policy crlf || return 1
    for line in '== Retained multicast accessors ==' \
        'accessor-target:True' 'accessor-method:True' 'accessor-bound:True' 'accessor-list:True' \
        'accessor-list-isolated:True' 'accessor-single:True' 'accessor-index:True' \
        'accessor-closed-method:True' 'accessor-public-enumerator:True' 'Retained multicast accessors end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done

    gate_capture_ok "$out/assert-before-vm-constructors" run_bounded dotnet "$_CG_APP" before-vm-constructors || return 1
    gate_output prefix "$native" "$out/assert-before-vm-constructors.stdout" --before '== CoreCLR delegate construction boundary ==' --policy crlf || return 1
    for line in '== CoreCLR delegate construction boundary ==' 'vm-ctor-metadata:True' \
        'vm-ctor-public-value:True' 'vm-ctor-public-instance:True' 'vm-ctor-public-static:True' \
        'vm-ctor-public-generic:True' 'vm-ctor-public-bind:True' 'CoreCLR delegate construction boundary end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done

    gate_capture_ok "$out/assert-before-named-constructors" run_bounded dotnet "$_CG_APP" before-named-constructors || return 1
    gate_output prefix "$native" "$out/assert-before-named-constructors.stdout" --before '== Named delegate construction public paths ==' --policy crlf || return 1
    for line in named-public-instance named-public-static named-public-multicast; do
        gate_output byte-witness "$native" "$line:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$native" 'Named delegate construction public paths end' --count 1 --policy crlf || return 1

    gate_capture_ok "$out/assert-before-method-enum" run_bounded dotnet "$_CG_APP" before-runtime-method-enumeration || return 1
    gate_output prefix "$native" "$out/assert-before-method-enum.stdout" --before '== Runtime method enumeration contract ==' --policy crlf || return 1
    for label in first-shape next-shape public-methods public-ctors public-invoke public-delegate; do
        gate_output byte-witness "$native" "method-enum-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$native" 'Runtime method enumeration contract end' --count 1 --policy crlf || return 1

    gate_capture_ok "$out/assert-before-method-handle" run_bounded dotnet "$_CG_APP" before-runtime-method-handle || return 1
    gate_output prefix "$native" "$out/assert-before-method-handle.stdout" --before '== Runtime method handle contract ==' --policy crlf || return 1
    for label in default-value default-equality default-hash public-attributes public-constructor public-generic public-invoke; do
        gate_output byte-witness "$native" "method-handle-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$native" 'method-handle-default-validation:ArgumentNullException' --count 1 --policy crlf || return 1
    method_handle_shapes "$native" || return 1
    gate_output byte-witness "$native" 'Runtime method handle contract end' --count 1 --policy crlf || return 1

    gate_capture_ok "$out/assert-before-type-materialize" run_bounded dotnet "$_CG_APP" before-runtime-type-materialization || return 1
    gate_output prefix "$native" "$out/assert-before-type-materialize.stdout" --before '== Runtime type materialization contract ==' --policy crlf || return 1
    type_materialize_asserts "$native" || return 1

}

corelib_diff_gate SelfHostPrimSubset System.Linq System.Diagnostics.TraceSource \
    System.Collections.Specialized

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

# Never execute the private CLR worker as an oracle. Verify its declaration/IL,
# then patch only an artifact copy for explicit native-only refusal checks.
gc_worker_boundary_asserts() {
    local output="$1" label
    for label in worker-direct worker-delegate worker-pointer worker-reflected-delegate worker-reflection; do
        gate_output byte-witness "$output" "$label:PlatformNotSupportedException:System.GC.RunFinalizers is a CoreCLR-owned worker entry point; dn2cpp owns finalization. Use GC.WaitForPendingFinalizers instead." --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'GC finalizer worker boundary end' --count 1 --policy crlf || return 1
}
build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
worker_input="artifacts/gc-worker-input-$CONFIG"
mkdir -p "$worker_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$worker_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --gc-finalizer-worker "$worker_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
worker_out="artifacts/gc-worker-boundary-$CONFIG"
invoke_cli "$worker_input/SelfHostPrimSubset.dll" -r "$_CG_CORELIB" \
    -r "$(dirname "$_CG_CORELIB")/System.Linq.dll" \
    -r "$(dirname "$_CG_CORELIB")/System.Diagnostics.TraceSource.dll" \
    -r "$(dirname "$_CG_CORELIB")/System.Collections.Specialized.dll" -o "$worker_out"
compile_console "$worker_out" GcWorkerBoundary
gate_capture_ok "$worker_out/native" run_bounded "./$worker_out/GcWorkerBoundary$EXE_EXT" native-finalizer-boundary
gc_worker_boundary_asserts "$worker_out/native.stdout"

# Intentional native capability boundary, separate from CLR validation parity.
# CoreCLR may register successfully; never execute this mode as a CLR oracle.
gc_notification_unavailable_asserts() {
    local output="$1" label
    for label in fullgc-register-direct fullgc-register-delegate fullgc-register-pointer fullgc-cancel-direct fullgc-cancel-delegate; do
        gate_output byte-witness "$output" "$label:InvalidOperationException::This API is not available when the concurrent GC is enabled." --count 1 --policy crlf || return 1
    done
    for label in fullgc-approach-default fullgc-complete-default fullgc-approach-zero fullgc-complete-positive fullgc-approach-pointer fullgc-approach-time fullgc-complete-time fullgc-approach-fraction fullgc-complete-upper; do
        gate_output byte-witness "$output" "$label:NotApplicable" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'GC notification unavailable end' --count 1 --policy crlf || return 1
}
gate_capture_ok "$_CG_OUT/notifications" run_bounded "./$_CG_OUT/SelfHostPrimSubset$EXE_EXT" native-gc-notification-unavailable
gc_notification_unavailable_asserts "$_CG_OUT/notifications.stdout"

# VM constructor stubs require CoreCLR PCODE/loader state. Only the native copy
# executes the rewritten direct/address sites and the explicit refusal probes.
vm_input="artifacts/delegate-vm-input-$CONFIG"
mkdir -p "$vm_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$vm_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"     --delegate-vm-constructors "$vm_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
vm_out="artifacts/delegate-vm-boundary-$CONFIG"
invoke_cli "$vm_input/SelfHostPrimSubset.dll" -r "$_CG_CORELIB"     -r "$(dirname "$_CG_CORELIB")/System.Linq.dll"     -r "$(dirname "$_CG_CORELIB")/System.Diagnostics.TraceSource.dll"     -r "$(dirname "$_CG_CORELIB")/System.Collections.Specialized.dll" -o "$vm_out"
compile_console "$vm_out" DelegateVmBoundary
gate_capture_ok "$vm_out/native" run_bounded "./$vm_out/DelegateVmBoundary$EXE_EXT" native-vm-constructor-boundary
vm_ctor_refusal_asserts() {
    local output="$1" member route
    for member in CtorClosed CtorClosedStatic CtorRTClosed CtorOpened CtorVirtualDispatch CtorCollectibleClosedStatic CtorCollectibleOpened CtorCollectibleVirtualDispatch; do
        for route in direct reflection; do
            gate_output byte-witness "$output" "vm-$member-$route:PlatformNotSupportedException:System.MulticastDelegate.$member is a CoreCLR-owned delegate initialization stub. Use normal delegate construction or Delegate.CreateDelegate instead." --count 1 --policy crlf || return 1
        done
    done
    for route in pointer bound; do
        gate_output byte-witness "$output" "vm-$route:PlatformNotSupportedException:System.MulticastDelegate.CtorRTClosed is a CoreCLR-owned delegate initialization stub. Use normal delegate construction or Delegate.CreateDelegate instead." --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'vm-ctor-receiver-unchanged:True' --count 1 --policy crlf || return 1
    gate_output byte-witness "$output" 'CoreCLR delegate constructor refusal end' --count 1 --policy crlf || return 1
}
vm_ctor_refusal_asserts "$vm_out/native.stdout"

# Direct protected calls live only in an artifact with an isolated entry point.
named_input="artifacts/delegate-named-input-$CONFIG"
mkdir -p "$named_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$named_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --delegate-named-constructors "$named_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
named_out="artifacts/delegate-named-$CONFIG"
invoke_cli "$named_input/SelfHostPrimSubset.dll" --auto-ref -o "$named_out"
compile_console "$named_out" DelegateNamed
gate_capture_ok "$named_out/direct" run_bounded "./$named_out/DelegateNamed$EXE_EXT" native-boundary
for label in delegate-instance delegate-static multicast-instance multicast-static; do
    gate_output byte-witness "$named_out/direct.stdout" "named-direct-$label:True" --count 1 --policy crlf
done
for owner in Delegate MulticastDelegate; do
    for form in instance static; do
        gate_output byte-witness "$named_out/direct.stdout" "named-hybrid-$owner-$form:PlatformNotSupportedException:" --count 1 --policy crlf
    done
    gate_output byte-witness "$named_out/direct.stdout" "named-hybrid-$owner-missing-instance:ArgumentException:" --count 1 --policy crlf
    gate_output byte-witness "$named_out/direct.stdout" "named-hybrid-$owner-unchanged:True" --count 1 --policy crlf
    for label in copied-instance copied-static removed-entry; do
        gate_output byte-witness "$named_out/direct.stdout" "named-hybrid-$owner-$label:PlatformNotSupportedException:" --count 1 --policy crlf
    done
    gate_output byte-witness "$named_out/direct.stdout" "named-hybrid-$owner-copied-missing:ArgumentException:" --count 1 --policy crlf
    for label in copied-unchanged clone-independent removed-unchanged; do
        gate_output byte-witness "$named_out/direct.stdout" "named-hybrid-$owner-$label:True" --count 1 --policy crlf
    done
done
gate_output byte-witness "$named_out/direct.stdout" 'Named constructor artifact end' --count 1 --policy crlf
gate_capture_ok "$named_out/reflection" run_bounded "./$named_out/DelegateNamed$EXE_EXT" contracts
named_ctor_asserts "$named_out/reflection.stdout"
gate_capture_ok "$named_out/oracle" run_bounded dotnet "$_CG_APP" named-contracts-only
gate_output compare "$named_out/reflection.stdout" "$named_out/oracle.stdout" --policy crlf

# The private VM leaves are never invoked by the CLR oracle. Generated artifact
# calls and calli sites use their actual CoreLib value/byref signatures.
enum_input="artifacts/runtime-method-enum-input-$CONFIG"
mkdir -p "$enum_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$enum_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --runtime-method-enumeration "$enum_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
enum_out="artifacts/runtime-method-enum-$CONFIG"
invoke_cli "$enum_input/SelfHostPrimSubset.dll" --auto-ref -o "$enum_out"
compile_console "$enum_out" RuntimeMethodEnum
gate_capture_ok "$enum_out/native" run_bounded "./$enum_out/RuntimeMethodEnum$EXE_EXT"
for leaf in first next; do
    member=GetFirstIntroducedMethod
    [ "$leaf" != next ] || member=GetNextIntroducedMethod
    for route in direct pointer reflection; do
        gate_output byte-witness "$enum_out/native.stdout" "method-enum-$leaf-$route:PlatformNotSupportedException:System.RuntimeTypeHandle.$member requires CoreCLR method descriptors. Use Type.GetMethods or Type.GetConstructors instead." --count 1 --policy crlf
    done
done
gate_output byte-witness "$enum_out/native.stdout" 'method-enum-constructor:PlatformNotSupportedException:System.RuntimeTypeHandle.GetFirstIntroducedMethod requires CoreCLR method descriptors. Use Type.GetMethods or Type.GetConstructors instead.' --count 1 --policy crlf
gate_output byte-witness "$enum_out/native.stdout" 'method-enum-default-empty:True' --count 1 --policy crlf
gate_output byte-witness "$enum_out/native.stdout" 'Runtime method enumeration boundary end' --count 1 --policy crlf

# Native-only calls use imported value/interface/pointer signatures. The CLR
# oracle inspects metadata and public/default handles, never raw VM leaves.
handle_input="artifacts/runtime-method-handle-input-$CONFIG"
mkdir -p "$handle_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$handle_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --runtime-method-handle "$handle_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
handle_out="artifacts/runtime-method-handle-$CONFIG"
invoke_cli "$handle_input/SelfHostPrimSubset.dll" --auto-ref -o "$handle_out"
compile_console "$handle_out" RuntimeMethodHandle
gate_capture_ok "$handle_out/native" run_bounded "./$handle_out/RuntimeMethodHandle$EXE_EXT"
method_handle_shapes "$handle_out/native.stdout"
for member in $method_handle_members; do
    for route in direct pointer reflection; do
        gate_output byte-witness "$handle_out/native.stdout" "method-handle-$member-$route:PlatformNotSupportedException:System.RuntimeMethodHandle.$member requires CoreCLR method descriptors. Use MethodInfo or ConstructorInfo instead." --count 1 --policy crlf
    done
done
gate_output byte-witness "$handle_out/native.stdout" 'method-handle-managed-overload:NullReferenceException:Object reference not set to an instance of an object.' --count 1 --policy crlf
gate_output byte-witness "$handle_out/native.stdout" 'Runtime method handle boundary end' --count 1 --policy crlf

# Raw VM pointers execute only in the native artifact, never on the CLR oracle.
type_input="artifacts/runtime-type-materialization-input-$CONFIG"
mkdir -p "$type_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$type_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --runtime-type-materialization "$type_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
type_out="artifacts/runtime-type-materialization-$CONFIG"
invoke_cli "$type_input/SelfHostPrimSubset.dll" --auto-ref -o "$type_out"
compile_console "$type_out" RuntimeTypeMaterialization
gate_capture_ok "$type_out/native" run_bounded "./$type_out/RuntimeTypeMaterialization$EXE_EXT"
type_materialize_asserts "$type_out/native.stdout"
for shape in method-table handle slow qcall maybe-null; do
    case "$shape" in
        method-table) member=GetRuntimeType;;
        handle) member=GetRuntimeTypeFromHandle;;
        slow|qcall) member=GetRuntimeTypeFromHandleSlow;;
        maybe-null) member=GetRuntimeTypeFromHandleMaybeNull;;
    esac
    for route in direct pointer; do
        for value in 0 1; do
            if [ "$shape" = maybe-null ] && [ "$value" = 0 ]; then
                gate_output byte-witness "$type_out/native.stdout" "type-materialize-$shape-$route-$value:True" --count 1 --policy crlf
            else
                gate_output byte-witness "$type_out/native.stdout" "type-materialize-$shape-$route-$value:PlatformNotSupportedException:System.RuntimeTypeHandle.$member requires CoreCLR type handles. Use Type.TypeHandle and Type.GetTypeFromHandle instead." --count 1 --policy crlf
            fi
        done
    done
    if [ "$shape" = qcall ]; then
        gate_output byte-witness "$type_out/native.stdout" 'type-materialize-qcall-reflection:NotSupportedException:Cannot create boxed ByRef-like values.' --count 1 --policy crlf
    elif [ "$shape" != method-table ]; then
        gate_output byte-witness "$type_out/native.stdout" "type-materialize-$shape-reflection:PlatformNotSupportedException:System.RuntimeTypeHandle.$member requires CoreCLR type handles. Use Type.TypeHandle and Type.GetTypeFromHandle instead." --count 1 --policy crlf
    fi
done
gate_output byte-witness "$type_out/native.stdout" 'Runtime type materialization boundary end' --count 1 --policy crlf
