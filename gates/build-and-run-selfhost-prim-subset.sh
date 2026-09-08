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
source "$(dirname "$0")/_common.sh"

corelib_diff_gate SelfHostPrimSubset System.Linq System.Diagnostics.TraceSource \
    System.Collections.Specialized

# The real runner source is linked into this small fixture so this block proves
# its MethodDef interception independently of a full self-host native build.
probe_project=gates/fixtures/tool-process/ToolProcessProbe.csproj
build_gate_proj "$probe_project"
probe_bin="$PWD/gates/fixtures/tool-process/bin/$CONFIG/$TFM"
probe_out="artifacts/selfhost-tool-process-$CONFIG"
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
