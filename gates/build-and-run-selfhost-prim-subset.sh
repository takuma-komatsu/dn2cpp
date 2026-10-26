#!/usr/bin/env bash
# Companion process argument/stream/exit preservation and the console-self-host
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
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/fixtures/ldftn-local/Program.cs gates/fixtures/ldftn-local/GcFinalizerWorkerFixture.cs gates/fixtures/ldftn-local/LdftnLocalFixture.csproj"
source "$(dirname "$0")/_common.sh"

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
