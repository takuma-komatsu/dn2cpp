using System.Reflection.Metadata;

namespace Dn2Cpp;

/// <summary>One System.IO.Path / System.IO.File lowering — one member per
/// dn2cpp_path_* / dn2cpp_file_* helper shape. <c>MethodCompiler.EmitIoIntrinsic</c> is
/// total over it, so a member added here without an emit arm fails to build.</summary>
internal enum IoLowering
{
    PathGetFileName,
    PathGetDirectoryName,
    PathGetExtension,
    PathGetFileNameWithoutExtension,
    PathGetFullPath,
    PathIsPathRooted,
    PathCombine2,
    PathCombine3,
    PathCombine4,
    FileExists,
    FileDelete,
    FileReadAllText,
    FileWriteAllText,
    FileReadAllBytes,
    FileWriteAllBytes,
}

/// <summary>
/// BCL types whose members the core emitter maps to runtime intrinsics
/// (<see cref="MethodCompiler.TranslateIntrinsic"/>) rather than transpiling their IL.
/// A real CoreLib loaded via -r makes these types resolvable like any other, so both
/// reachability and call emission must keep treating them as intrinsic — otherwise
/// compiling, say, the real <c>Console.WriteLine</c> body drags in most of the BCL.
/// Members of any other loaded BCL type transpile from their real IL.
/// </summary>
internal static partial class CoreIntrinsics
{
    private static readonly HashSet<string> s_intrinsicTypes = new()
    {
        "System.Object",
        // ValueType.Equals/GetHashCode/ToString use reflection/type identity we don't
        // model, and are inherited vtable slots of every struct. Mapping the type to
        // intrinsic keeps those slots unreached (-> nullptr) unless a struct provides its
        // own override, which is reached as usual when actually dispatched.
        "System.ValueType",
        "System.Exception",
        // Its real ctor (validates args, copies into a ReadOnlyCollection, formats a
        // culture-dependent Message) is untranspilable; model it as an opaque intrinsic
        // like Exception. InnerExceptions is served from the runtime aggregate object.
        "System.AggregateException",
        // The base of every custom attribute. Its Equals/GetHashCode override reflects over
        // fields, and is never dispatched virtually on an attribute we materialize — so
        // treat it as an opaque intrinsic base: a derived ctor's `base..ctor()` is a no-op
        // and the real Attribute IL stays out of the tree.
        "System.Attribute",
        // The base of every tracing provider. Its real machinery (manifest generation,
        // EventPipe/ETW writes, the EventListener registry, reflection over [Event]
        // methods) has no native counterpart, so treat it as an opaque intrinsic base: a
        // provider's own [Event] methods and fields transpile as ordinary C#, and
        // `base..ctor()` / WriteEvent / IsEnabled become no-ops
        // (TryEmitEventSourceIntrinsic). The framework's OWN providers are additionally
        // folded to no-ops at their call sites (IsFrameworkEventSourceProvider) — a
        // separate, narrower mechanism this does not disturb.
        "System.Diagnostics.Tracing.EventSource",
        // Array element access is via IL opcodes (newarr/ldelem/ldlen/...); the remaining
        // static helpers (Copy/Empty/Clear) reach MethodTable/covariance internals we do
        // not model. Map the few that real BCL collections (List<T>) call to runtime
        // intrinsics; the rest are never reached (used-slot).
        "System.Array",
        // Dictionary<K,V>'s prime-bucket sizing. Its real Primes table is a
        // ReadOnlySpan over RVA blob data (RuntimeHelpers.CreateSpan, a ref-struct
        // construction we don't model); reimplement the small stable algorithm in
        // the runtime and map the type to intrinsics.
        "System.Collections.HashHelpers",
        "System.Console",
        // The hot-update entry surface in the auto-referenced Dn2Cpp.Runtime
        // shim. Its managed body is a placeholder throw; the call lowers to the
        // runtime interpreter's dn2cpp_hotupdate_run (see docs/BPI-FORMAT.md).
        "Dn2Cpp.Runtime.HotUpdate",
        // The stderr-write surface Dn2CppConsoleWriter (the real Console.Error
        // managed writer) funnels into. Placeholder-throw bodies; each call
        // lowers to the matching dn2cpp_textwriter_* stderr helper. Dn2CppConsoleWriter
        // itself is NOT intrinsic — it transpiles as an ordinary TextWriter subclass so
        // its vtable dispatches a spilled callvirt.
        "Dn2Cpp.Runtime.ConsoleRuntime",
        "System.String",
        "System.Delegate",
        "System.Type",
        "System.Reflection.MemberInfo",
        // Reflected field handles. FieldInfo is abstract in the BCL; we model
        // a reflected field as a Dn2CppFieldRef and intrinsic-dispatch its members, so
        // its (and RuntimeFieldInfo's) real IL must stay out of the tree.
        "System.Reflection.FieldInfo",
        // Reflected method/parameter handles. MethodInfo/MethodBase are
        // abstract in the BCL; we model a reflected method as a Dn2CppMethodRef and a
        // parameter as a Dn2CppParamRef, intrinsic-dispatching their members so the
        // real RuntimeMethodInfo IL stays out of the tree.
        "System.Reflection.MethodInfo",
        "System.Reflection.MethodBase",
        "System.Reflection.ParameterInfo",
        // Reflected constructor handles. ConstructorInfo is modeled as a
        // Dn2CppMethodRef (like MethodInfo); its real RuntimeConstructorInfo IL stays
        // out of the tree. System.Activator's non-generic CreateInstance(Type) reflects
        // (untranspilable), so it is intrinsic-dispatched to dn2cpp_activator_*; the
        // generic CreateInstance<T> is lowered separately.
        "System.Reflection.ConstructorInfo",
        "System.Activator",
        // Reflected property handles. PropertyInfo is modeled as a Dn2CppPropRef;
        // GetValue/SetValue delegate to the accessor invoker thunks. Its real
        // RuntimePropertyInfo IL stays out of the tree.
        "System.Reflection.PropertyInfo",
        // System.Reflection.Assembly is opaque: an Assembly value is its defining
        // assembly's simple-name handle (Type.Assembly / GetEntryAssembly identity;
        // op_Equality is a name compare) and its reflected members (GetCustomAttributes/
        // IsDefined/GetName) dispatch to the generated assembly registry. The real
        // Assembly IL stays out of the tree. AssemblyName is NOT intrinsic-mapped:
        // it is a plain managed class (name/version/flags fields + display-name
        // parsing/formatting), so its real BCL IL transpiles; Assembly.GetName()
        // bridges the handle world into it via the AssemblyName(string) ctor.
        "System.Reflection.Assembly",
        // System.Reflection.Module is opaque like Assembly: dn2cpp assemblies are
        // single-module, so a Module value is the defining assembly's simple-name
        // handle (Type.Module / MemberInfo.Module; equality is a name compare).
        "System.Reflection.Module",
        // Reflected attribute rows (MemberInfo.CustomAttributes /
        // GetCustomAttributesData). Modeled as a Dn2CppAttrDataRef wrapping the
        // element's attribute-table row; AttributeType is intrinsic-dispatched, the
        // argument views throw a catchable PlatformNotSupportedException, and the
        // real RuntimeCustomAttributeData IL stays out of the tree.
        "System.Reflection.CustomAttributeData",
        // IntrospectionExtensions.GetTypeInfo(Type) is the identity in this
        // model (a runtime Type handle IS the TypeInfo, the RuntimeType
        // posture); intrinsic-mapping the extension class keeps the
        // TypeDelegator/TypeInfo-construction IL out of the tree.
        "System.Reflection.IntrospectionExtensions",
        // DEGRADED intrinsics: real runtime objects reporting zero captured frames,
        // because their real ctors reach StackFrameHelper's InternalCall stack walker and
        // would drag the whole MethodBase/RuntimeMethodHandle reflection subtree in. Why a
        // diagnostic API degrades where the dynamic-codegen surface fails loud:
        // docs/ARCHITECTURE.md §4-B.
        "System.Diagnostics.StackTrace",
        "System.Diagnostics.StackFrame",
        // The lookup half is served from the assembly's carried `<BaseName>.resources`
        // blob by runtime/core/intrinsics/dn2cpp_system_resources.cpp. Intrinsic because
        // the real body cannot run here rather than because it is large:
        // ManifestBasedResourceGroveler walks a CultureInfo.Parent chain and probes
        // satellites through RuntimeAssembly.InternalGetSatelliteAssembly, while
        // CultureInfo lowers to a headerless const Dn2CppNumberFormatInfo* with no Parent
        // and dn2cpp has no assembly loader — both halves of that walk have nothing to
        // walk. Mapping the type keeps the groveler, ResourceReader, the BinaryFormatter
        // subtree and the satellite probe out of the tree entirely.
        "System.Resources.ResourceManager",
        "System.Math",
        "System.MathF",
        "System.Int32",
        "System.UInt32",
        "System.Int64",
        "System.UInt64",
        "System.Double",
        "System.Single",
        "System.Boolean",
        // Convert.ToInt32/ToInt64/ToDouble/ToBoolean: the real bodies dispatch
        // through IConvertible + IFormatProvider (globalization) we don't model;
        // map the numeric/string/bool overloads to runtime intrinsics.
        "System.Convert",
        // Culture-aware formatting. The real CultureInfo/NumberFormatInfo
        // are vast (ICU, resource lookup, per-thread caches); model them as a
        // const Dn2CppNumberFormatInfo* carrying the separators/signs/symbols and
        // intercept the members below. IFormatProvider (their interface) lowers to
        // the same pointer so a CultureInfo/NumberFormatInfo flows as the provider.
        "System.Globalization.CultureInfo",
        "System.Globalization.NumberFormatInfo",
        "System.IFormatProvider",
        // TextInfo — only its ListSeparator is reached (a throw helper joins the
        // missing required-property names with CurrentUICulture.TextInfo.ListSeparator).
        // The real type is ICU-backed; model it as the same invariant culture pointer
        // and intercept the one member. Its casing methods (ToUpper/ToLower/ToTitleCase)
        // stay carve-outs (Char already maps casing to ASCII/invariant inline).
        "System.Globalization.TextInfo",
        // System.Buffers.SearchValues — the arity-0 static factory (Create). The generic
        // instance type SearchValues<T> is in s_intrinsicGenericCpp below (same split as
        // Vector128). The real Create picks a SIMD/ProbabilisticMap subclass we don't
        // model; the byte/char membership set scan replaces the whole hierarchy. STJ's
        // JSON token scanning reaches IndexOfAny(span, SearchValues<byte>).
        "System.Buffers.SearchValues",
        // Its real GetHashCode body uses object-header internals (sizeof/Unsafe)
        // we don't model; intercepted as an intrinsic instead.
        "System.Runtime.CompilerServices.RuntimeHelpers",
        // Pointer/byref primitives the JIT lowers; their real IL bodies are
        // [Intrinsic] stubs that just throw, so map them inline.
        "System.Runtime.CompilerServices.Unsafe",
        // The BCL's dead-throw closure. Its Throw* helpers build large exception objects
        // (message formatting drags in CultureInfo, resource managers, …); mapping them to
        // runtime trap intrinsics keeps that IL out of the tree while preserving the throw
        // semantics. Dropping this row transpiles and runs — the route to take if ParamName
        // and the full argument formatting ever need to be exact.
        "System.ThrowHelper",
        // The resource-string accessor the BCL routes every exception message through. Its
        // real bodies pull in ResourceManager + CultureInfo; the intrinsic instead recovers
        // the real English text from the assembly's embedded .resources blob at transpile
        // time (ResourceStrings / Compilation.SrResourceText), folds it in as a literal,
        // and lowers SR.Format onto the string.Format engine — so the message text is the
        // real one while that IL never enters the tree.
        "System.SR",
        // The non-generic async/await BCL types (the generic ones — Task<T>,
        // AsyncTaskMethodBuilder<T>, TaskAwaiter<T> — are matched by their
        // arity-stripped name via the Task-family map below). Their real bodies
        // are the entire TPL/scheduler; we model them with hand-written runtime
        // structs and intrinsics instead.
        "System.Threading.Tasks.Task",
        "System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
        "System.Runtime.CompilerServices.TaskAwaiter",
        // TaskCompletionSource(<T>) — modeled as the bare Dn2CppTask* it
        // completes (get_Task is the identity); newobj allocates a pending task,
        // (Try)SetResult/SetException/SetCanceled are exactly-once transitions on
        // it. Its real body is the TPL promise plumbing we don't model.
        "System.Threading.Tasks.TaskCompletionSource",
        // The interpolated string handler the compiler lowers $"..." to. Its
        // real body reaches Buffer.Memmove (InternalCall), so it is modeled by
        // a hand-written runtime struct and intrinsics instead.
        "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler",
        // Task.Yield()'s awaitable + its nested awaiter (an empty value type with
        // a bare "YieldAwaiter" full name). Modeled as the stateless
        // Dn2CppYieldAwaiter; awaiting it always suspends and reschedules on the
        // next scheduler turn.
        "System.Runtime.CompilerServices.YieldAwaitable",
        "YieldAwaiter",
        // task.ConfigureAwait(bool) returns a ConfiguredTaskAwaitable wrapping the
        // same task; its nested ConfiguredTaskAwaiter behaves exactly like a
        // TaskAwaiter. ConfigureAwait is a no-op on the single-threaded model, so both
        // lower to Dn2CppTaskAwaiter (carry the task pointer).
        "System.Runtime.CompilerServices.ConfiguredTaskAwaitable",
        "ConfiguredTaskAwaiter",
        // valueTask.ConfigureAwait(bool) mirrors it: ConfiguredValueTaskAwaitable
        // and its nested ConfiguredValueTaskAwaiter (bare nested name) also lower
        // to Dn2CppTaskAwaiter. The whole family must be intrinsic — the real
        // awaiter IL reaches into ValueTask's _obj field, which the fieldless
        // intrinsic ValueTask model does not carry.
        "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable",
        "ConfiguredValueTaskAwaiter",
        // ValueTask / ValueTask<T> and their async-method builder + awaiter. A
        // ValueTask is modeled as the same {task} struct as a TaskAwaiter (we always
        // back it by a Dn2CppTask), so its await path reuses the Task machinery; the
        // builder reuses Dn2CppAsyncBuilder. Their real bodies are the TPL/IValueTask
        // Source plumbing we don't model.
        "System.Threading.Tasks.ValueTask",
        "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder",
        // The pooling variant ([AsyncMethodBuilder] on the file-backed async
        // Stream paths, e.g. BufferedFileStreamStrategy). Pooling is a CLR
        // state-machine-box allocation optimization the dn2cpp task model has no
        // equivalent of, so it lowers exactly like the non-pooling builder; its
        // real IL (per-core box caches, debugger notification) stays out of the tree.
        "System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder",
        "System.Runtime.CompilerServices.ValueTaskAwaiter",
        // The builder Roslyn drives an `async IAsyncEnumerable<T>` iterator with.
        // Its real IL is an AsyncTaskMethodBuilder<VoidTaskResult> underneath, so
        // it reaches straight into the TPL internals the model replaces: the
        // static box-based AwaitUnsafeOnCompleted overload, SetExistingTaskResult,
        // and a `ldsfld Task::s_cachedCompleted` on the intrinsic Task type. It
        // lowers exactly like the other builders (Dn2CppAsyncBuilder over a task);
        // the iterator's *results* never flow through it — they go through the
        // state machine's own ManualResetValueTaskSourceCore promise, which is
        // transpiled from the real BCL IL.
        "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder",
        // The builder Roslyn drives an `async void` method with. Lowers to
        // Dn2CppAsyncBuilder like the others; only the ends differ — no observable Task, so
        // SetResult takes no value and there is no get_Task, and SetException re-raises the
        // fault off the synchronous stack (dn2cpp_task_throw_async, matching real .NET's
        // ThreadPool/SynchronizationContext crash). Without the intrinsic mapping,
        // transpiling AwaitUnsafeOnCompleted finds no Dn2CppAsyncBuilder field in the state
        // machine and gaps.
        "System.Runtime.CompilerServices.AsyncVoidMethodBuilder",
        // Task.get_Factory and TaskScheduler.Default/Current are opaque nullptr sentinels;
        // factory.StartNew dispatches to the same worker pool as Task.Run (the
        // TaskCreationOptions/TaskScheduler arguments are scheduling hints dn2cpp's fixed
        // pool does not model). Keeps the real TPL scheduler machinery out of the tree —
        // TaskScheduler..ctor alone drags in ThreadPoolTaskScheduler plus the
        // Debugger/ConditionalWeakTable/DependentHandle debug bookkeeping.
        "System.Threading.Tasks.TaskFactory",
        "System.Threading.Tasks.TaskScheduler",
        // Cancellation. CancellationTokenSource is a runtime object with a canceled
        // flag + the registrations (pending Delay tasks + Register(Action) callbacks)
        // bound to it; CancellationToken is the {source} value struct;
        // CancellationTokenRegistration is the opaque handle Register returns (a reg
        // node pointer) whose Dispose detaches the callback. Their real bodies are the
        // timer/callback plumbing we don't model.
        "System.Threading.CancellationTokenSource",
        "System.Threading.CancellationToken",
        "System.Threading.CancellationTokenRegistration",
        // Data-parallel loops (For/ForEach/Invoke) on a real per-call OS-thread fan-out
        // with a deterministic join barrier, lowered to dn2cpp_parallel_* helpers; the real
        // body is the TPL partitioner/scheduler. ParallelLoopResult is the value struct
        // For/ForEach return. ParallelOptions holds only MaxDegreeOfParallelism
        // (CancellationToken/TaskScheduler are a carve-out) and is built at newobj.
        // ParallelLoopState has no public constructor, so every member dispatches through
        // runtime helper calls and needs no newobj intercept.
        "System.Threading.Tasks.Parallel",
        "System.Threading.Tasks.ParallelLoopResult",
        "System.Threading.Tasks.ParallelOptions",
        "System.Threading.Tasks.ParallelLoopState",
        // System.Threading.Thread — real OS threads (std::thread). new Thread(...) is
        // intercepted at newobj; Start/Join/Sleep/Yield/CurrentThread/ManagedThreadId/
        // IsAlive/IsBackground/Name bind to dn2cpp_thread_* helpers.
        "System.Threading.Thread",
        // System.Threading.ThreadPool — fire-and-forget work on the same worker pool as
        // Task.Run. QueueUserWorkItem(WaitCallback [, object state]) is intercepted at the
        // call site and lowered to dn2cpp_threadpool_queue (a GC holder keeps the delegate +
        // state reachable while queued). A static class never instantiated, so no C++ type
        // is mapped. The generic QueueUserWorkItem<TState>(Action<TState>, TState, bool),
        // UnsafeQueueUserWorkItem/IThreadPoolWorkItem forms, and the SetMinThreads/
        // GetAvailableThreads/etc. tuning surface are carve-outs (loud NotSupported).
        "System.Threading.ThreadPool",
        // Blocking synchronization primitives backed by a real mutex+condvar
        // (dn2cpp_semaphore_* / dn2cpp_event_*). The concrete object is built at newobj
        // (skipping the real handle-allocating ctors). SemaphoreSlim.Wait/Release;
        // ManualResetEventSlim.Set/Reset/Wait; ManualResetEvent/AutoResetEvent dispatch
        // Set/Reset through EventWaitHandle and WaitOne through WaitHandle.
        "System.Threading.SemaphoreSlim",
        "System.Threading.ManualResetEventSlim",
        "System.Threading.ManualResetEvent",
        "System.Threading.AutoResetEvent",
        "System.Threading.EventWaitHandle",
        "System.Threading.WaitHandle",
        // The handle OF one of the above, and in this model it IS one of the above: the
        // WaitHandle family is backed by an opaque runtime mutex+condvar object, never by
        // an OS handle, so the safe handle maps to the very same Dn2CppObject* and
        // WaitHandle.SafeWaitHandle is the identity. Transpiling the real type does not
        // stay small: ReleaseHandle is Interop.Kernel32.CloseHandle over a Win32 HANDLE
        // this runtime never mints, and the base SafeHandle drags its
        // CriticalFinalizerObject refcount state machine in to guard a resource that does
        // not exist.
        "Microsoft.Win32.SafeHandles.SafeWaitHandle",
        // CountdownEvent (Signal counts down; Wait blocks until zero) and Barrier (a
        // cyclic phase barrier; each phase releases when all participants have signaled,
        // optionally running an Action<Barrier> post-phase action). Same model as above:
        // a real mutex+condvar object built at newobj (dn2cpp_countdown_* / dn2cpp_barrier_*).
        "System.Threading.CountdownEvent",
        "System.Threading.Barrier",
        // ReaderWriterLockSlim — a real reader-writer lock (many concurrent readers XOR one
        // writer) with writer preference. Same model as above: a real mutex+condvar object
        // built at newobj (dn2cpp_rwlock_*). Enter/Exit/TryEnter the read/write/upgradeable
        // locks; the upgrade-to-write path, recursion, and per-thread lock-held queries are
        // carve-outs (the slim model keeps no per-thread ownership).
        "System.Threading.ReaderWriterLockSlim",
        // System.Threading.Timer — a per-timer OS thread that waits dueTime, fires
        // TimerCallback(state), and (when period is finite > 0) re-fires every period.
        // Built at newobj (dn2cpp_timer_*); Change reschedules, Dispose stops + joins the
        // thread. GC-allocated (it holds the managed callback/state). The Dispose(WaitHandle)
        // and DisposeAsync overloads are carve-outs (loud NotSupported).
        "System.Threading.Timer",
        // Atomic primitives — real hardware atomics (the multithread model). Their real
        // bodies are InternalCall/extern; mapped to seq_cst runtime intrinsics. The
        // compiler-generated field-like `event` add/remove accessors call
        // CompareExchange, so this also unblocks events. Interlocked.MemoryBarrier
        // lowers to a fence.
        "System.Threading.Interlocked",
        // Volatile.Read/Write(ref T) — seq_cst atomic load/store (the multithread
        // model). Emitted inline as __atomic_load_n/store_n (int/ptr) or bit-cast
        // helpers (float/double). A static class — no C++ type mapping needed.
        "System.Threading.Volatile",
        // lock(object) / Monitor — real per-object recursive lock (the multithread
        // model). Enter/Exit/TryEnter and Wait/Pulse/PulseAll bind to the
        // dn2cpp_monitor_* table; the real bodies route through System.Threading.Lock
        // -> EventSource -> Calli.
        "System.Threading.Monitor",
        // .NET 9+ System.Threading.Lock. `lock(lockVar)` where lockVar is a Lock
        // lowers to `var scope = lockVar.EnterScope(); try {…} finally { scope.Dispose(); }`.
        // Its real EnterScope routes through EventSource -> Calli (untranspilable); backed
        // here by the same per-object mutex table as Monitor. Lock is a reference type (a
        // minimal allocated object); the nested ref struct Lock.Scope is an intrinsic
        // value type (see s_intrinsicNestedCpp) returned by EnterScope and consumed by
        // Dispose (which carries the Lock so it releases the right mutex).
        "System.Threading.Lock",
        // StringBuilder. The real chunked corelib type's Append reaches
        // Buffer.Memmove (InternalCall); modeled as a hand-written growable UTF-16
        // buffer instead.
        "System.Text.StringBuilder",
        // System.Char. The real classification/casing methods (IsLetter, ToUpper, …)
        // route through TextInfo/CultureInfo/globalization (InternalCall); map the
        // common ones to ASCII/invariant inline checks instead.
        "System.Char",
        // System.Decimal. The real Decimal.ToString reaches Number.FormatDecimal ->
        // ArrayPool -> EventSource -> Calli, and its arithmetic uses the
        // DecCalc/InternalCall path; modeled as an intrinsic value type backed by the
        // runtime Dn2CppDecimal (ctors/op_*/conversions/ToString lowered at the call
        // site).
        "System.Decimal",
        // System.TimeSpan / System.DateTime. The real corelib types reach Number
        // formatting / InternalCall (clock, calendar) / culture; modeled as intrinsic
        // value types backed by the runtime Dn2CppTimeSpan / Dn2CppDateTime.
        "System.TimeSpan",
        "System.DateTime",
        // System.DateTimeOffset: same treatment, backed by Dn2CppDateTimeOffset.
        "System.DateTimeOffset",
        // System.DateOnly / System.TimeOnly: same treatment, backed by the runtime
        // Dn2CppDateOnly / Dn2CppTimeOnly value structs.
        "System.DateOnly",
        "System.TimeOnly",
        // The real members are InternalCalls and low-level CLR layout reads. dn2cpp's heap
        // is non-moving, so Normal/Pinned pinning is identity while Weak kinds delegate to
        // the weak table: model GCHandle as the intrinsic value type Dn2CppGCHandle and
        // lower its members at the call site (TryEmitGCHandleIntrinsic). This row is what
        // cuts every GCHandle body, including the _Internal* InternalCalls.
        "System.Runtime.InteropServices.GCHandle",
        // The real members are InternalCalls over the CLR's ephemeron handle table. Modeled
        // as the intrinsic value type Dn2CppDependentHandle — one pointer to a runtime cell
        // holding the target as a Boehm weak link plus the dependent as a strong reference.
        // A bounded ephemeron approximation: the dependent does not keep the target alive
        // (correct), but a dependent that references its own target keeps the pair alive (a
        // leak real ephemerons avoid); a ConditionalWeakTable whose values are null or
        // independent of their keys is exact. This is what lets ConditionalWeakTable
        // transpile from real BCL IL.
        "System.Runtime.DependentHandle",
        // The file-backed map subset (POSIX mmap). The real bodies are the SafeHandle /
        // UnmanagedMemoryAccessor + OS-mapping P/Invoke cascade; the three reference types
        // lower to small by-value intrinsic structs and every member is lowered at the call
        // site (TryEmitMemoryMappedFileIntrinsic, plus the generic accessor forms in
        // TranslateGenericIntrinsic). The view inherits its typed accessors from
        // UnmanagedMemoryAccessor, so that base is intrinsic too. SafeBuffer/SafeHandle are
        // NOT intrinsic-typed wholesale — AcquirePointer/ReleasePointer/ByteLength/
        // DangerousGetHandle dispatch on the receiver's intrinsic C++ type instead (see
        // ResolveCallTarget's name-scoped cut).
        "System.IO.MemoryMappedFiles.MemoryMappedFile",
        "System.IO.MemoryMappedFiles.MemoryMappedViewAccessor",
        "System.IO.UnmanagedMemoryAccessor",
        "Microsoft.Win32.SafeHandles.SafeMemoryMappedViewHandle",
        // Portable SIMD vector STATIC helper classes (arity 0: Vector64/128/256/512,
        // System.Numerics.Vector). Their generic Create/Add/Equals/… land as
        // MethodSpecs (routed to TranslateGenericIntrinsic); the non-generic forms
        // (element-list Create, get_IsHardwareAccelerated) land here as MemberRefs and
        // are lowered to the software-vector runtime (dn2cpp_vec_*). The same-named
        // arity-1 GENERIC structs are registered in s_intrinsicGenericCpp below.
        "System.Runtime.Intrinsics.Vector64",
        "System.Runtime.Intrinsics.Vector128",
        "System.Runtime.Intrinsics.Vector256",
        "System.Runtime.Intrinsics.Vector512",
        "System.Numerics.Vector",
    };

    /// <summary>Async/await BCL types modeled by hand-written runtime structs
    /// rather than transpiled from corelib IL. Keyed on the arity-stripped
    /// full name so both the generic (<c>Task&lt;T&gt;</c>) and non-generic
    /// (<c>Task</c>) forms match; the value is the C++ runtime type the managed
    /// type lowers to. <c>Task</c>/<c>Task&lt;T&gt;</c> are reference types
    /// (pointer); the builder and awaiter are value types embedded by value in
    /// the compiler-generated state-machine struct.</summary>
    private static readonly Dictionary<string, string> s_taskFamilyCpp = new()
    {
        ["System.Threading.Tasks.Task"] = "Dn2CppTask*",
        // A TCS is the bare task it completes (get_Task is the identity).
        ["System.Threading.Tasks.TaskCompletionSource"] = "Dn2CppTask*",
        ["System.Runtime.CompilerServices.AsyncTaskMethodBuilder"] = "Dn2CppAsyncBuilder",
        ["System.Runtime.CompilerServices.TaskAwaiter"] = "Dn2CppTaskAwaiter",
        // ValueTask family — modeled on the Task structs (a ValueTask is a {task}
        // struct, its builder reuses Dn2CppAsyncBuilder, its awaiter is {task}).
        ["System.Threading.Tasks.ValueTask"] = "Dn2CppTaskAwaiter",
        ["System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder"] = "Dn2CppAsyncBuilder",
        // The pooling variant lowers identically — pooling is a CLR allocation
        // optimization dn2cpp's task model doesn't need (see s_intrinsicTypes).
        ["System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder"] = "Dn2CppAsyncBuilder",
        ["System.Runtime.CompilerServices.ValueTaskAwaiter"] = "Dn2CppTaskAwaiter",
        // The async-iterator builder is the same {task} builder struct: it drives
        // the state machine (MoveNext<TSM> / AwaitUnsafeOnCompleted<TAwaiter,TSM>)
        // and nothing ever reads a task back out of it.
        ["System.Runtime.CompilerServices.AsyncIteratorMethodBuilder"] = "Dn2CppAsyncBuilder",
        // The async-void builder is the same {task} builder struct: it drives the state
        // machine identically; only its ends differ (no observable Task; SetException
        // re-raises off the synchronous stack). See s_intrinsicTypes.
        ["System.Runtime.CompilerServices.AsyncVoidMethodBuilder"] = "Dn2CppAsyncBuilder",
        // TaskFactory / TaskScheduler — opaque nullptr sentinels (Task.Factory /
        // TaskScheduler.Default are never dereferenced; StartNew is intercepted at
        // the call site), so a Dn2CppObject* handle suffices.
        ["System.Threading.Tasks.TaskFactory"] = "Dn2CppObject*",
        ["System.Threading.Tasks.TaskScheduler"] = "Dn2CppObject*",
        // Cancellation: the source is a runtime reference object, the token a value
        // struct holding the source pointer.
        ["System.Threading.CancellationTokenSource"] = "Dn2CppCancelSource*",
        ["System.Threading.CancellationToken"] = "Dn2CppCancelToken",
        // The handle Register returns; an opaque reg-node pointer (Dispose detaches it).
        ["System.Threading.CancellationTokenRegistration"] = "Dn2CppCancelReg*",
        // System.Threading.Thread — a runtime reference object wrapping a std::thread.
        ["System.Threading.Thread"] = "Dn2CppThread*",
        // Blocking sync primitives — opaque runtime reference objects (the helpers cast
        // internally), so a Dn2CppObject* handle suffices.
        ["System.Threading.SemaphoreSlim"] = "Dn2CppObject*",
        ["System.Threading.ManualResetEventSlim"] = "Dn2CppObject*",
        ["System.Threading.ManualResetEvent"] = "Dn2CppObject*",
        ["System.Threading.AutoResetEvent"] = "Dn2CppObject*",
        ["System.Threading.EventWaitHandle"] = "Dn2CppObject*",
        ["System.Threading.WaitHandle"] = "Dn2CppObject*",
        // Deliberately the SAME C++ type as WaitHandle above: the safe handle and the
        // handle it guards are one object here, which is what makes
        // WaitHandle.get_SafeWaitHandle an identity and lets the static
        // EventWaitHandle.Set(SafeWaitHandle) reuse the instance Set arm unchanged.
        ["Microsoft.Win32.SafeHandles.SafeWaitHandle"] = "Dn2CppObject*",
        ["System.Threading.CountdownEvent"] = "Dn2CppObject*",
        ["System.Threading.Barrier"] = "Dn2CppObject*",
        ["System.Threading.ReaderWriterLockSlim"] = "Dn2CppObject*",
        ["System.Threading.Timer"] = "Dn2CppObject*",
        // ParallelLoopResult — the value struct Parallel.For/ForEach return (no trailing
        // '*' -> IsValueType, passed by value), backed by the runtime Dn2CppParallelLoopResult.
        ["System.Threading.Tasks.ParallelLoopResult"] = "Dn2CppParallelLoopResult",
    };

    /// <summary>Individual BCL methods whose subtree is cut at reachability and
    /// which are neutralized at the call site (args dropped, default result
    /// pushed). Unlike an intrinsic *type*, the declaring type is still
    /// transpiled normally — only these methods are excised. Used for paths
    /// whose bodies pull unmodeled machinery but whose default result is
    /// semantically correct for our model. Backends contribute target-specific
    /// entries via <see cref="IEmitBackend.AdditionalBoundedMethods"/>, merged
    /// in <see cref="Compilation.IsBoundedMethod"/>.
    ///
    /// <para>This table is the CORE set, and the descriptor row over it
    /// (<see cref="BdCoreBounded"/>) represents only this half. The bounded set an asker
    /// actually consults is an instance union — this table plus the backend's entries plus
    /// the CLI's <c>--cut</c> specs — so <see cref="Compilation.IsBoundedMethod"/> stays the
    /// merge point every asker goes through.</para>
    ///
    /// <para>Rows that turn out to be bodyless <c>[DllImport]</c>s are reported at run time:
    /// every transpile prints a count line and <c>--verbose</c> names them
    /// (<see cref="Compilation.NoteBoundedImport"/>), because such a row substitutes a zero
    /// the caller cannot distinguish from a real answer rather than a modelled no-op. The
    /// shape to aim for is a <c>QCall</c> — a call into a CoreCLR a transpiled image does not
    /// have — never a symbol some platform could supply: a row naming a real external module
    /// is better closed by admitting the module
    /// (<see cref="Compilation.IsRuntimeProvidedPInvokeModule"/>).</para>
    ///
    /// <para>Every row carries a <see cref="BoundedVerdict"/>, a column rather than a second
    /// table precisely so that adding a row forces the question "is a zero here an ANSWER or
    /// a SUBSTITUTE?". <c>Silent</c> means the default is what a correct dn2cpp binary should
    /// report (no managed debugger attached, no COM, one static assembly world) — the true
    /// state, not a degrade. <c>Loud</c> means the caller asked the native world for data and
    /// would receive a zero indistinguishable from a real result; those rows throw a catchable
    /// <c>PlatformNotSupportedException</c> naming the module, entry point and remedy, the
    /// same posture <c>dn2cpp_require_metadata</c> takes for a stripped type.</para>
    ///
    /// <para>The verdict is consulted only where it can be honoured: a bodyless
    /// <c>[DllImport]</c>. A <c>Loud</c> verdict on a row with a real IL body would have no
    /// module to name, so <see cref="Compilation.TryBoundedImport"/> reads the verdict only
    /// after establishing <c>Rva == 0 &amp;&amp; PInvoke is not null</c>. Most rows never
    /// reach that test — they name ordinary managed bodies, or Windows-only types a POSIX
    /// CoreLib does not define. Which rows do is a property of the host's CoreLib; read it
    /// off a <c>--measure</c> run's <c>s0-bounded-imports.tsv</c> sidecar.</para></summary>
    private static readonly Dictionary<(string Type, string Method), BoundedVerdict> s_boundedMethods = new()
    {
        // String-comparer factories the Dictionary ctor / Resize use to pick a
        // non-randomized or DoS-mitigation randomized comparer. Their bodies
        // allocate the OrdinalComparer / RandomizedStringEqualityComparer family
        // (+ Interop.GetRandomBytes / Marvin hashing). Returning null means "use
        // the default comparer", which is exactly what our intrinsic
        // EqualityComparer<string> / interface-comparer devirtualization provides
        // — so string keys hash by content and value-type keys never reach here.
        [("System.Collections.Generic.NonRandomizedStringEqualityComparer", "GetRandomizedEqualityComparer")]
            = BoundedVerdict.Silent,
        [("System.Collections.Generic.NonRandomizedStringEqualityComparer", "GetStringComparer")]
            = BoundedVerdict.Silent,
        // JsonSerializerOptions.TrackOptionsInstance(options): registers the new options
        // instance into a static ConditionalWeakTable used only for cross-instance cache
        // coherence. Its body reaches DependentHandle's InternalCalls (the CWT's weak-key GC
        // primitive), and the table is never read on the (de)serialize path, so dropping the
        // registration is output-neutral.
        [("System.Text.Json.JsonSerializerOptions", "TrackOptionsInstance")]
            = BoundedVerdict.Silent,
        // Base System.IO.Stream APM internals: the BeginRead/EndRead-bridging path behind
        // the base virtual ReadAsync/WriteAsync slots. The bodies reach vtable
        // introspection (GetMethodTable + the HasOverriddenSlow QCall) and the
        // ReadWriteTask / FromAsyncTrimPromise scheduler machinery, on every branch, so the
        // subtree is cut at these roots.
        //
        // BeginEndReadAsync / BeginEndWriteAsync are the private funnels the BASE
        // ReadAsync/WriteAsync bodies call, and they are NOT neutralized to a default: they
        // are REWRITTEN (MethodCompiler.TryEmitStreamSyncOverAsyncFunnel) to a synchronous
        // dispatch through the Read/Write slot plus a completed Task, which is what the
        // machinery they replace computes anyway once the scheduler is taken out. The
        // neutralized default would be a NULL Task, and a user Stream subclass overriding
        // only sync Read/Write — every wrapper/decorator stream — inherits the base slot, so
        // `await myStream.ReadAsync(...)` would await a nullptr. See
        // IsStreamSyncOverAsyncFunnel. The other four are dead: nothing calls them.
        [("System.IO.Stream", "HasOverriddenBeginEndRead")] = BoundedVerdict.Silent,
        [("System.IO.Stream", "HasOverriddenBeginEndWrite")] = BoundedVerdict.Silent,
        [("System.IO.Stream", "BeginReadInternal")] = BoundedVerdict.Silent,
        [("System.IO.Stream", "BeginWriteInternal")] = BoundedVerdict.Silent,
        [("System.IO.Stream", "BeginEndReadAsync")] = BoundedVerdict.Silent,
        [("System.IO.Stream", "BeginEndWriteAsync")] = BoundedVerdict.Silent,
        // HexConverter's Vector128 fast-path arms (Guid formatting/parsing, hex
        // string encode/decode). Runtime-dead: every caller guards them with
        // Vector128.IsHardwareAccelerated / Ssse3.IsSupported, which the vector
        // intrinsic layer folds to false (scalar-emulation carve-out), so the
        // scalar fallback always runs — yet their bodies reach unfolded internal
        // vector helpers (Vector128.UnpackLow and friends). Cutting them turns
        // the folded-false branch's call into an unreachable trap.
        [("System.HexConverter", "AsciiToHexVector128")] = BoundedVerdict.Silent,
        [("System.HexConverter", "EncodeToUtf16_Vector128")] = BoundedVerdict.Silent,
        [("System.HexConverter", "TryDecodeFromUtf16_Vector128")] = BoundedVerdict.Silent,
        // The lazy message builder FileNotFoundException/FileLoadException.SetMessageField
        // falls to in its `else` arm. Its body reaches the GetFileLoadExceptionMessage /
        // GetMessageForHR VM QCalls. Runtime-dead for any FileNotFoundException constructed
        // WITH a message (the `_message == null` guard short-circuits SetMessageField), so
        // the trap at the call site is unreachable in practice.
        [("System.IO.FileLoadException", "FormatFileLoadExceptionMessage")] = BoundedVerdict.Silent,
        // SynchronizationContext.Post — the BASE body reaches the untranspilable generic
        // ThreadPool.QueueUserWorkItem<TState> (a value-typed tuple TState the pool's
        // uniform object-pointer invoke cannot carry), so it stays cut. A user-installed
        // context's Post must still run, and a C# call site always names this base
        // declaration (member lookup excludes overrides), so neutralizing the call site
        // would silently drop `ctx.Post(...)` on the override too. Hence the
        // IsVirtualDispatchBounded carve-out: a callvirt to Post dispatches through the
        // vtable, and only the base body is cut — a dispatch landing on a plain
        // SynchronizationContext hits its nullptr slot, the price of keeping the ThreadPool
        // subtree out.
        [("System.Threading.SynchronizationContext", "Post")] = BoundedVerdict.Silent,
        // Environment.FailFast must NOT be bounded: neutralization drops the arguments and
        // pushes the default result, and FailFast returns void — so the call would do
        // nothing and execution would continue past a request to halt on corrupted state.
        // It is an intrinsic instead (TryEmitEnvironmentIntrinsic ->
        // dn2cpp_environment_failfast, which reports and abort()s).
        // The ExecutionContext-flow interior of the thread-pool work-item /
        // IValueTaskSource continuation machinery. Runtime-dead: ExecutionContext.
        // Capture lowers to null (nothing to flow — see the intercept in
        // MethodCompiler.TranslateCall), so the null/IsDefault fast arms always
        // run: IThreadPoolWorkItem.Execute goes straight to ExecuteInternal
        // (never RunForThreadPoolUnsafe), and ManualResetValueTaskSourceCore's
        // SignalCompletion never sees a captured context (never
        // InvokeContinuationWithContext / ScheduleCapturedContext). Their bodies
        // read/write Thread._executionContext/_synchronizationContext on the
        // intrinsic Thread model (no transpiled struct) and build
        // SendOrPostCallback over Action<object>.Invoke — none of it modeled.
        [("System.Threading.ExecutionContext", "RunForThreadPoolUnsafe")] = BoundedVerdict.Silent,
        // The AsyncLocal read/write funnels — the same posture as Capture (cut to the null
        // "nothing to flow" encoding) and RunForThreadPoolUnsafe above. Both bodies reach
        // Thread._executionContext on the intrinsic Thread model (no transpiled struct — the
        // field access would name an undeclared t_System_Threading_Thread). SetLocalValue is a
        // no-op (nothing is ever flowed, matching Capture == null), so GetLocalValue always
        // reads a null context and returns null — AsyncLocal<T>.Value reads default(T), the
        // consistent degrade for the un-modeled ExecutionContext flow. Reached from
        // SerializationInfo.ThrowIfDeserializationInProgress's AsyncLocal<bool> and any user
        // AsyncLocal<T>.
        [("System.Threading.ExecutionContext", "GetLocalValue")] = BoundedVerdict.Silent,
        [("System.Threading.ExecutionContext", "SetLocalValue")] = BoundedVerdict.Silent,
        // The FLOW-SUPPRESSION surface, closed as one set: SuppressFlow / RestoreFlow /
        // IsFlowSuppressed plus the AsyncFlowControl handle they hand out (the other end of
        // the `using (ExecutionContext.SuppressFlow())` idiom). Every one of these bodies
        // touches Thread.CurrentThread._executionContext on the intrinsic Thread model, so
        // an emitted body would name an undeclared t_System_Threading_Thread — a
        // named-struct failure rather than a gap: --measure cannot see it, the transpile
        // SUCCEEDS, and the C++ compile fails on the type name with no cause attached
        // (CppEmitter.AssertNamedStructsDefined is the backstop).
        //
        // The defaults are the truth here, hence Silent: dn2cpp flows no execution context
        // at all — Capture() lowers to the null "nothing to flow" encoding two rows up — so
        // there is nothing to suppress and IsFlowSuppressed's false is a fact. The trio stays
        // self-consistent under the bound: real .NET's RestoreFlow throws when flow is not
        // suppressed, and the guarded idiom only calls it when the same false answer said
        // to, so a caller never reaches a state the bound cannot honour.
        [("System.Threading.ExecutionContext", "SuppressFlow")] = BoundedVerdict.Silent,
        [("System.Threading.ExecutionContext", "RestoreFlow")] = BoundedVerdict.Silent,
        [("System.Threading.ExecutionContext", "IsFlowSuppressed")] = BoundedVerdict.Silent,
        [("System.Threading.AsyncFlowControl", "Undo")] = BoundedVerdict.Silent,
        [("System.Threading.AsyncFlowControl", "Dispose")] = BoundedVerdict.Silent,
        [("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCoreShared", "InvokeContinuationWithContext")]
            = BoundedVerdict.Silent,
        [("System.Threading.Tasks.Sources.ManualResetValueTaskSourceCoreShared", "ScheduleCapturedContext")]
            = BoundedVerdict.Silent,
        // The Windows IOCP family. Runtime-dead: dn2cpp never binds a handle to a completion
        // port (only the synchronous FileStream strategy runs, so an async-opened handle
        // never materializes), yet real Windows FileStream construction statically reaches
        // all of it, and the bodies are ThreadPool-counter + NativeOverlapped bookkeeping
        // the intrinsic ThreadPool model does not carry.
        [("System.Threading.ThreadPoolBoundHandle", "OnNativeIOCompleted")] = BoundedVerdict.Silent,
        [("System.Threading.ThreadPoolBoundHandle", "BindHandleCore")] = BoundedVerdict.Silent,
        [("System.Threading.ThreadPoolBoundHandle", "BindHandle")] = BoundedVerdict.Silent,
        [("System.Threading.ThreadPoolBoundHandle", "AllocateNativeOverlapped")]
            = BoundedVerdict.Silent,
        [("System.Threading.ThreadPoolBoundHandle", "UnsafeAllocateNativeOverlapped")]
            = BoundedVerdict.Silent,
        [("System.Threading.ThreadPoolBoundHandle", "FreeNativeOverlapped")]
            = BoundedVerdict.Silent,
        [("System.Threading.ThreadPoolBoundHandle", "GetNativeOverlappedState")]
            = BoundedVerdict.Silent,
        [("System.Threading.ThreadPoolBoundHandle", "Dispose")] = BoundedVerdict.Silent,
        // PreAllocatedOverlapped — the reusable-NativeOverlapped holder of the
        // same dead IOCP family (SafeFileHandle's async ValueTaskSource pools
        // one per handle). Same posture: statically reached from FileStream
        // construction, can never execute (no IOCP, no async-opened handle).
        [("System.Threading.PreAllocatedOverlapped", "Dispose")] = BoundedVerdict.Silent,
        [("System.Threading.PreAllocatedOverlapped", "Release")] = BoundedVerdict.Silent,
        [("System.Threading.PreAllocatedOverlapped", "UnsafeCreate")] = BoundedVerdict.Silent,
        [("System.Threading.PreAllocatedOverlapped", "AddRef")] = BoundedVerdict.Silent,
        // RandomAccess's async-handle overlapped factory — only called for a
        // FILE_FLAG_OVERLAPPED handle (async FileStream), which dn2cpp never
        // opens. Same dead-IOCP-family posture as ThreadPoolBoundHandle above.
        [("System.IO.RandomAccess", "GetNativeOverlappedForAsyncHandle")] = BoundedVerdict.Silent,
        // RandomAccess's sync-I/O-on-an-async-handle fallbacks (they wait on a
        // CallbackResetEvent for the overlapped completion). Same dead family:
        // dn2cpp never opens a FILE_FLAG_OVERLAPPED handle.
        [("System.IO.RandomAccess", "ReadSyncUsingAsyncHandle")] = BoundedVerdict.Silent,
        [("System.IO.RandomAccess", "WriteSyncUsingAsyncHandle")] = BoundedVerdict.Silent,
        // SafeFileHandle.OverlappedValueTaskSource — another member of the same dead IOCP
        // family. The key is unqualified because a nested type's FullName here is its bare
        // Name (see Model.cs's ClassInfo.FullName). It is reached from any allocated
        // SafeFileHandle through the IValueTaskSource slots it implements, but both members
        // run only for an IOCP-bound handle: RegisterForCancellation is gated on
        // CanBeCanceled (never true for a synchronous read/write's default token) and
        // reaches CancellationToken.UnsafeRegister, and Complete is the completion-port
        // callback itself.
        [("OverlappedValueTaskSource", "RegisterForCancellation")] = BoundedVerdict.Silent,
        [("OverlappedValueTaskSource", "Complete")] = BoundedVerdict.Silent,
        // The same CanBeCanceled-gated UnsafeRegister shape, reached from RandomAccess's
        // async-over-sync fallback. The branch reaching the unmodeled callback-list
        // registration is never taken at run time but is still statically compiled.
        [("System.Threading.AsyncOverSyncWithIoCancellation", "RegisterCancellation")]
            = BoundedVerdict.Silent,
        // The async Windows FileStream strategy's overlapped CopyToAsync. Runtime-dead: the
        // transpiler pins every file open onto SyncWindowsFileStreamStrategy
        // (MethodCompiler.NeutralizeFileStreamAsyncArgs masks the isAsync argument), so
        // ChooseStrategyCore's async branch never runs — but the newobj in that dead branch
        // still instantiates the type, and this override is its only member reaching genuine
        // IOCP (via AsyncModeCopyToAsync -> PreAllocatedOverlapped -> NativeMemory.Alloc, a
        // bodyless extern). At run time Stream.CopyToAsync dispatches to the sync strategy's
        // buffered loop instead, exactly as on Unix.
        [("System.IO.Strategies.AsyncWindowsFileStreamStrategy", "CopyToAsync")]
            = BoundedVerdict.Silent,
        // NativeLibrary.SetDllImportResolver — a resolver registration lives in a
        // ConditionalWeakTable<Assembly, DllImportResolver> (DependentHandle
        // InternalCalls, not modeled), and a transpiled image resolves its
        // P/Invokes at link time — a registered resolver can never be consulted,
        // so dropping the registration is behavior-neutral on every lane.
        [("System.Runtime.InteropServices.NativeLibrary", "SetDllImportResolver")]
            = BoundedVerdict.Silent,
        // win-x64 CoreLib's COM-interop-aware weak reference path (absent from Unix CoreLib
        // entirely). dn2cpp never creates a COM RCW or registers a ComWrappers object — COM
        // interop is a permanent non-goal (docs/STATUS.md) — so PossiblyComObject is always
        // false, ComAwareBit is never set on any handle, and WeakReference's
        // `(taggedHandle & ComAwareBit) != 0` arms never execute. They are still statically
        // reachable from every WeakReference construction and Target access, and their
        // leaves are bodyless QCall externs. Non-COM WeakReference behavior (the real Boehm
        // weak-link path) never reads _comInfo/ComAwareBit at all and is untouched.
        [("System.ComAwareWeakReference", "ComWeakRefToObject")] = BoundedVerdict.Silent,
        [("System.ComAwareWeakReference", "ObjectToComWeakRef")] = BoundedVerdict.Silent,
        // The two managed-debugger InternalCalls. A transpiled AOT binary runs under no
        // managed debugger — there is no VM to attach one — so IsManagedDebuggerAttached's
        // false and BreakInternal's no-op are exactly what real .NET does with no debugger
        // present. Neither is a degrade.
        [("System.Diagnostics.Debugger", "IsManagedDebuggerAttached")] = BoundedVerdict.Silent,
        [("System.Diagnostics.Debugger", "BreakInternal")] = BoundedVerdict.Silent,
        // The public Debugger.Log calls LogInternal unconditionally (no IsLogging guard),
        // so app code reaching Log directly does not benefit from the IsLogging fold.
        // Bound at the LibraryImport WRAPPER, never at its `<LogInternal>g____PInvoke|N_0`
        // stub — that ordinal is a Roslyn artifact and moves with a CoreLib update.
        [("System.Diagnostics.Debugger", "LogInternal")] = BoundedVerdict.Silent,
        // ComWrappers' COM-vftbl builders — three bodyless QCall externs reached only
        // through ComWrappers..cctor, which TypeDescriptor.GetConverter drags in. With no
        // COM RCW ever created, TryGetComInstance answers false (a true answer: no COM
        // instance exists) and the zero-filled vftbls are never dispatched through.
        //
        // Silent despite being bodyless QCalls, because of that .cctor: dn2cpp runs .cctors
        // EAGERLY in reach order, so a throwing verdict would abort at static init every
        // program that merely drags ComWrappers in — before a line of it runs, and for a
        // vftbl nothing dispatches through.
        [("System.Runtime.InteropServices.ComWrappers", "GetTaggedImpl")] = BoundedVerdict.Silent,
        [("System.Runtime.InteropServices.ComWrappers", "GetDefaultIReferenceTrackerTargetVftbl")]
            = BoundedVerdict.Silent,
        [("System.Runtime.InteropServices.ComWrappers", "<GetIUnknownImplInternal>g____PInvoke|1_0")]
            = BoundedVerdict.Silent,
        // NativeLibrary.Load's two dlopen P/Invoke thunks. Reached only as the body of a
        // DllImportResolver delegate; SetDllImportResolver is itself bounded above, so the
        // registration is dropped and a transpiled image resolves its P/Invokes at LINK
        // time — the resolver can never be consulted at run time.
        //
        // LOUD, because neither `Load` nor `LoadFromPath` contains a throw opcode:
        // `throwOnError` is honoured on the NATIVE side of the QCall, so a bounded zero
        // would make the public, documented-to-throw NativeLibrary.Load hand back
        // IntPtr.Zero without a word and the first symptom would be whatever the caller does
        // with a null module handle. A transpiled image cannot load a library at run time at
        // all, so the honest substitute is the loud one.
        [("System.Runtime.InteropServices.NativeLibrary", "<LoadFromPath>g____PInvoke|1_0")]
            = BoundedVerdict.Loud,
        [("System.Runtime.InteropServices.NativeLibrary", "<LoadByName>g____PInvoke|2_0")]
            = BoundedVerdict.Loud,
        // macOS process self-inspection (libproc) IS DELIBERATELY NOT BOUNDED.
        // /usr/lib/libproc.dylib is a runtime-provided P/Invoke module
        // (Compilation.IsRuntimeProvidedPInvokeModule), so its thunks lower to direct native
        // calls and answer for real — proc_pidinfo/proc_pidpath/proc_listallpids are
        // exported by libSystem, which every macOS binary links already, so this needs no
        // shim TU and no -l token. Bounding them instead would leave Process.ProcessName
        // empty, Process.StartTime throwing and Process.MainModule null, and no per-row
        // verdict could do better: ProcessManager.OSX.CreateProcessInfo answers SessionId
        // and ProcessName out of one proc_pidinfo fill, so no row could make the broken half
        // loud without taking the working half down. Nothing is bounded in its place and no
        // host test was added — the ModuleRef itself is the host condition, since only the
        // OSX flavour of System.Diagnostics.Process declares the module at all.
        //
        // proc_pid_rusage and libSystem::mach_timebase_info remain unreachable through the
        // Process.get_TotalProcessorTime cut just below, not through anything here.
        // AssemblyLoadContext plumbing bounded to benign defaults (the surrounding
        // machinery of the LOAD trapped by IsAssemblyLoadContextRuntimeLoad — see its
        // doc for why only the load traps and the rest default). All three are the same
        // posture as the DotnetModuleBackend's ALC-reload bookkeeping cuts
        // (AddTypeForAlcReloading / TrackAlcForUnloading):
        //  - InitializeAssemblyLoadContext (the [LibraryImport] that mints the native
        //    ALC): default 0 handle. Creating an ALC object is harmless until something
        //    loads into it, and AssemblyLoadContext.Default's init runs this eagerly, so
        //    a trap here would abort startup.
        //  - GetLoadContextForAssembly (which context loaded an assembly): default null,
        //    a benign "no custom context" answer.
        //  - PrepareForAssemblyLoadContextRelease (unload bookkeeping, from the ALC
        //    finalizer): no-op. Tears down a native ALC never created here; a finalizer
        //    must not throw.
        [("System.Runtime.Loader.AssemblyLoadContext", "<InitializeAssemblyLoadContext>g____PInvoke|0_0")]
            = BoundedVerdict.Silent,
        [("System.Runtime.Loader.AssemblyLoadContext", "GetLoadContextForAssembly")]
            = BoundedVerdict.Silent,
        [("System.Runtime.Loader.AssemblyLoadContext", "PrepareForAssemblyLoadContextRelease")]
            = BoundedVerdict.Silent,
        // Process.TotalProcessorTime. Its body calls libproc's proc_pid_rusage, then converts
        // mach ticks to a TimeSpan through MapTime -> GetTimeBase ->
        // libSystem::mach_timebase_info. Bounding the getter to TimeSpan.Zero cuts that whole
        // subtree. proc_pid_rusage would lower to a real call now (libproc is runtime-
        // provided), but `libSystem` is NOT, so lifting this cut requires admitting that
        // module too. A bounded ROW for mach_timebase_info is the one shape that must never
        // be used: a 0 numer/denom makes MapTime's `ticks/denom` divide by zero.
        [("System.Diagnostics.Process", "get_TotalProcessorTime")] = BoundedVerdict.Silent,
        // grpc's two Windows-only imports — ntdll!RtlGetVersion and
        // kernel32!GetCurrentApplicationUserModelId — bounded as ONE pair because they are
        // one subtree: DetectWindowsVersion calls RtlGetVersion for the build number and
        // IsUwp (-> GetCurrentApplicationUserModelId) to decide whether to trust it, and its
        // only reader is grpc's OperatingSystem.IsWindowsServer capability gate. Bounding
        // one and not the other lowers the survivor to a direct native call, puts
        // `kernel32` in pinvoke-libs.txt and fails the macOS link — the sibling rule
        // (docs/ARCHITECTURE.md §4-B) protects the link manifest here.
        //
        // It cannot be transpiled instead, for two independent reasons. Its one parameter is
        // `ref OSVERSIONINFOEX`, whose CSDVersion field is [MarshalAs(ByValTStr)] — a
        // fixed-width inline character array the P/Invoke struct marshaller refuses to carry
        // across the boundary (Compilation.MarshalLayout still SIZES it, so
        // Marshal.SizeOf/OffsetOf answer). And `ntdll.dll` is in
        // Compilation.IsRuntimeProvidedPInvokeModule, so even an admitted shape would lower
        // to a direct native call and fail at C++ link time on a POSIX host.
        //
        // Silent because the zeroed default is truthful. RtlGetVersion returns an NTSTATUS,
        // and 0 over a caller-zeroed OSVERSIONINFOEX is STATUS_SUCCESS, so
        // DetectWindowsVersion reads build 0 and IsWindowsServer2022OrLater answers false —
        // the correct answer to the question grpc asks, since IsWindowsServer gates HTTP/3
        // and the DnHttp shim over vendored libcurl has no HTTP/3 path on any host. A Loud
        // verdict would abort a program over a capability probe it asked speculatively.
        // NoteBoundedImport still names the row in the bounded-imports report.
        [("Grpc.Net.Client.Internal.Native", "RtlGetVersion")] = BoundedVerdict.Silent,
        [("Grpc.Net.Client.Internal.Native", "GetCurrentApplicationUserModelId")] = BoundedVerdict.Silent,
    };

    public static bool IsIntrinsicType(string fullTypeName) => s_intrinsicTypes.Contains(fullTypeName);

    /// <summary>Members of the sub-word integer primitives (which are NOT intrinsic
    /// types — their other members transpile from real IL) that are nonetheless
    /// lowered inline at every call site: ToString to dn2cpp_int_to_string /
    /// dn2cpp_format_*, Parse/TryParse to the width-parameterized NumberStyles
    /// engine, TryFormat to dn2cpp_try_format_* (mirroring the reach-side edge
    /// cuts, which keep the System.Number format/parse cascade out of the tree).
    /// The constrained static-virtual resolution and its reach twin share this
    /// predicate so both route a resolved sub-word impl identically: a member in
    /// this set goes through the intrinsic table (its real body is reach-cut), any
    /// other member direct-calls the real transpiled body (which reach reaches).</summary>
    public static bool IsInlineLoweredPrimitiveMember(string? fullTypeName, string name) =>
        fullTypeName is "System.Byte" or "System.SByte" or "System.Int16" or "System.UInt16"
        && name is "ToString" or "Parse" or "TryParse" or "TryFormat";

    /// <summary>The non-generic <c>System.Collections.Comparer::Compare(object, object)</c> —
    /// body-intercepted. Its real IL does <c>x as IComparable</c> (and a CompareInfo string
    /// arm), so a BOXED primitive — which carries no interface map — makes <c>Comparer.Default</c>
    /// throw <c>ArgumentException</c> instead of ordering. Both askers share this ONE name-gated
    /// predicate: the reachability cut (<see cref="Compilation.ResolveCallTarget"/>) deletes the real
    /// body's edge and the emit route (<c>MethodCompiler.TranslateCall</c>) lowers a direct call — and
    /// <c>CppEmitter</c> synthesizes the METHOD BODY (<c>CompileCoreIntrinsicWrapper</c>) so the
    /// <c>IComparer.Compare</c> interface slot (how <c>ArrayList.Sort()</c> / <c>SortedList</c> and the
    /// boxed sort/search paths reach <c>Comparer.Default</c>) resolves to the same lowering:
    /// <c>dn2cpp_object_compare</c>, which orders a boxed primitive/enum/string inline and a user
    /// reference type through non-generic <c>IComparable</c>. Comparer declares exactly one
    /// <c>Compare(object, object)</c>, so the name+type gate is exact.</summary>
    public static bool LoweredComparerCompare(string? fullTypeName, string name) =>
        fullTypeName == "System.Collections.Comparer" && name == "Compare";

    /// <summary>The System.IO.Path / System.IO.File statics that are lowered inline to a
    /// dn2cpp_path_* / dn2cpp_file_* runtime helper — keyed by the exact OVERLOAD SHAPE
    /// each helper models, not by the method name. Null for every other member and every
    /// other overload, which transpiles from its real BCL IL like any other method.
    ///
    /// <para>Both askers call exactly this — the emit route
    /// (<c>MethodCompiler.TranslateCall</c> → <c>EmitIoIntrinsic</c>) and the reachability
    /// cut (<c>Compilation.ResolveCallTarget</c>) — so they cannot disagree about which
    /// overloads are lowered. Returning null is the fall-through: no cut, so the real body
    /// stays reachable; no route, so the call emits against it. A cut with no lowering
    /// behind it is not a transpile error but an undefined symbol at C++ LINK time, long
    /// after the transpile reported success, which is why the name gate lives INSIDE the
    /// predicate rather than at the two call sites.</para>
    ///
    /// <para><paramref name="sig"/> is a thunk so a token that is not a Path/File member
    /// never pays for a signature decode — a decode mints the closed generics the signature
    /// names (AGENTS.md). A generic member is declined outright, which is also what makes
    /// the two askers' differing GenericContext irrelevant.</para></summary>
    public static IoLowering? LoweredIoMember(string? declType, string name, Func<MethodSignature<TypeDesc>> sig)
    {
        if (declType is not ("System.IO.Path" or "System.IO.File"))
            return null;
        var decoded = sig();
        if (decoded.GenericParameterCount != 0)
            return null;
        var ps = decoded.ParameterTypes;
        static bool IsByteArray(TypeDesc t) =>
            t is { Kind: TypeKind.SZArray, Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Byte } };

        // The pure-lexical path ops, string overloads only. Every span / params /
        // (string, string) GetFullPath form falls through to the real BCL body, which is
        // just as lexical and transpiles fine — it was only ever the name test that made
        // reaching it impossible.
        if (declType == "System.IO.Path")
        {
            return name switch
            {
                "GetFileName" when ps is [{ IsString: true }] => IoLowering.PathGetFileName,
                "GetDirectoryName" when ps is [{ IsString: true }] => IoLowering.PathGetDirectoryName,
                "GetExtension" when ps is [{ IsString: true }] => IoLowering.PathGetExtension,
                "GetFileNameWithoutExtension" when ps is [{ IsString: true }] => IoLowering.PathGetFileNameWithoutExtension,
                "GetFullPath" when ps is [{ IsString: true }] => IoLowering.PathGetFullPath,
                "IsPathRooted" when ps is [{ IsString: true }] => IoLowering.PathIsPathRooted,
                "Combine" when ps is [{ IsString: true }, { IsString: true }] => IoLowering.PathCombine2,
                "Combine" when ps is [{ IsString: true }, { IsString: true }, { IsString: true }] => IoLowering.PathCombine3,
                "Combine" when ps is [{ IsString: true }, { IsString: true }, { IsString: true }, { IsString: true }] =>
                    IoLowering.PathCombine4,
                _ => null,
            };
        }
        // File: the default-encoding (UTF-8) string/byte[] overloads only. The Encoding,
        // ReadOnlySpan and append overloads fall through to the real bodies, which
        // transpile against the POSIX PAL.
        return name switch
        {
            "Exists" when ps is [{ IsString: true }] => IoLowering.FileExists,
            "Delete" when ps is [{ IsString: true }] => IoLowering.FileDelete,
            "ReadAllText" when ps is [{ IsString: true }] => IoLowering.FileReadAllText,
            "ReadAllBytes" when ps is [{ IsString: true }] => IoLowering.FileReadAllBytes,
            "WriteAllText" when ps is [{ IsString: true }, { IsString: true }] => IoLowering.FileWriteAllText,
            "WriteAllBytes" when ps is [{ IsString: true }, { } bytes] && IsByteArray(bytes) => IoLowering.FileWriteAllBytes,
            _ => null,
        };
    }

    /// <summary>The System.Environment members lowered inline to a dn2cpp_env_* /
    /// dn2cpp_process_path / dn2cpp_environment_* runtime helper — keyed by the exact
    /// OVERLOAD SHAPE, not by the method name. False for every other member and every
    /// other overload, which transpiles from its real BCL IL.
    ///
    /// <para>Both askers ask this one question — the emit route
    /// (<c>MethodCompiler.TranslateCall</c>) and the reachability cut
    /// (<c>Compilation.ResolveCallTarget</c>) — so they cannot disagree about which
    /// overloads are lowered. <c>GetEnvironmentVariable(string, EnvironmentVariableTarget)</c>
    /// deliberately falls through: its real body is transpilable, since the Process target
    /// tail-calls the one-argument form (which IS lowered here, so the cascade stops at the
    /// intercept) and User/Machine read the registry, which on Unix returns null.</para>
    ///
    /// <para>FailFast is deliberately shape-free — all five CoreLib overloads are lowered,
    /// matched by shape rather than arity (TryEmitEnvironmentIntrinsic). A half-mapped
    /// family is how a silent miscompile gets in: the public pair forwards to the internal
    /// three-argument form and that to the StackCrawlMark InternalCall, none of which is
    /// transpilable.</para></summary>
    public static bool LoweredEnvMember(string? declType, string name, Func<MethodSignature<TypeDesc>> sig)
    {
        if (declType != "System.Environment")
            return false;
        // Cheap name gate first: a signature decode is not free (it mints the closed
        // generics the signature names — AGENTS.md), so no non-Environment member and no
        // unrelated Environment member ever pays for one.
        if (name is not ("GetEnvironmentVariable" or "get_CurrentDirectory" or "set_CurrentDirectory"
            or "get_ProcessPath" or "Exit" or "FailFast"))
            return false;
        var decoded = sig();
        if (decoded.GenericParameterCount != 0)
            return false;
        var ps = decoded.ParameterTypes;
        return name switch
        {
            // The process-environment read. NOT the (string, EnvironmentVariableTarget)
            // form, which has a real body and now reaches it.
            "GetEnvironmentVariable" => ps is [{ IsString: true }],
            "get_CurrentDirectory" or "get_ProcessPath" => ps.Length == 0,
            "set_CurrentDirectory" => ps is [{ IsString: true }],
            "Exit" => ps is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }],
            "FailFast" => true,
            _ => false,
        };
    }

    /// <summary>The System.MemoryExtensions members lowered inline to the runtime's BMP
    /// invariant case fold (<c>dn2cpp_span_case_invariant</c>) — keyed by the exact
    /// OVERLOAD SHAPE, and asked by BOTH the emit route and the reachability cut so the
    /// two cannot disagree.
    ///
    /// <para>.NET 10 declares exactly one overload of each, so nothing falls in the gap
    /// today; asking one predicate from both askers is what keeps an overload added
    /// upstream from being cut with no lowering behind it — an undefined symbol at C++ link
    /// time, long after the transpile reported success.</para>
    ///
    /// <para>The real bodies reach GlobalizationMode.get_Invariant -> LoadICU /
    /// InitICUFunctions, so the modeled shapes must stay lowered.</para></summary>
    public static bool LoweredSpanCaseFold(string? declType, string name, Func<MethodSignature<TypeDesc>> sig)
    {
        if (declType != "System.MemoryExtensions")
            return false;
        if (name is not ("ToUpperInvariant" or "ToLowerInvariant"))
            return false;
        var decoded = sig();
        if (decoded.GenericParameterCount != 0)
            return false;
        return decoded.ParameterTypes is [{ } src, { } dst] && IsCharSpan(src) && IsCharSpan(dst);
    }

    /// <summary>A monomorphized <c>ReadOnlySpan&lt;char&gt;</c> / <c>Span&lt;char&gt;</c>,
    /// matched by the mangled FullName the specialization carries. Pure: it reads no
    /// Compilation state, which is what lets the reachability cut ask the same question the
    /// emitter does (MethodCompiler.IsCharSpan forwards here).</summary>
    public static bool IsCharSpan(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.FullName: "System.ReadOnlySpan_Char" or "System.Span_Char" };

    /// <summary>The intra-CoreLib runtime primitives lowered inline at every MethodDefinition
    /// call site — keyed by declaring type + member NAME, over the WHOLE overload family.
    ///
    /// <para>Both askers call exactly this — the emit route
    /// (<c>MethodCompiler.TranslateCall</c> → <c>EmitRuntimePrimitive</c>) and the
    /// reachability cut (<c>Compilation.ResolveCallTarget</c>) — so a member cannot be cut
    /// with no lowering behind it, which would be an undefined symbol at C++ LINK time long
    /// after the transpile reported success.</para>
    ///
    /// <para>The remedy for an unmodeled overload here is a LOUD THROW, not a fall-through,
    /// and that follows from WHAT this arm intercepts. The MemberReference arm intercepts
    /// public BCL surface whose unmodeled overloads are just as transpilable as the modeled
    /// ones, so falling through there costs nothing. Every member named HERE is private
    /// runtime plumbing: a bodyless InternalCall/QCall, a body whose whole purpose is the
    /// machinery dn2cpp replaces (the SIMD transcode subtree, the ICU cascade, the GC write
    /// barriers, native GC accounting), or — for <c>GC.KeepAlive</c> — an EMPTY body whose
    /// emission would degrade the call to something the optimizer erases. An overload added
    /// upstream to such a family is more of the same, so its fall-through IS the cascade the
    /// cut exists to delete. Cut by NAME, lower by SHAPE, and fail loudly on a shape nothing
    /// models: <c>EmitRuntimePrimitive</c> ends in a throw.</para>
    ///
    /// <para>Name-keyed, so answering costs no signature decode (a decode mints the closed
    /// generics the signature names; AGENTS.md). Where a family's overloads must be told
    /// apart, that SHAPE test lives in the emitter, which has already paid for the signature
    /// it is about to lower.</para></summary>
    public static bool LoweredRuntimePrimitive(string? declType, string name) => declType switch
    {
        // SuppressFinalize / ReRegisterForFinalize -> dn2cpp_gc_*_finalize (their real bodies
        // are RuntimeHelpers.GetMethodTable -> MethodTable.HasFinalizer -> the *Internal
        // FCalls); Collect / WaitForPendingFinalizers / GetTotalMemory -> the Boehm entry
        // points (real bodies: the _Collect / _WaitForPendingFinalizers InternalCalls and
        // GCMemoryInfo's native accounting); KeepAlive -> the opaque dn2cpp_keep_alive
        // barrier, whose real body is EMPTY — emitting it hands the optimizer a call it may
        // erase, which is the one thing KeepAlive exists to prevent.
        // _CollectionCount(generation) -> GC_get_gc_no (Boehm is non-generational, so
        // every generation reports the one collection-cycle counter); GetMemoryInfo(data,
        // kind) -> the Boehm heap-accounting fill of the GCMemoryInfoData the public
        // GetGCMemoryInfo news up. Both real bodies are bodyless FCalls into native GC
        // accounting dn2cpp answers from bdwgc instead.
        // GetAllocatedBytesForCurrentThread (a bodyless InternalCall) and
        // GetTotalAllocatedBytes (whose body branches to two bodyless InternalCalls)
        // -> the dn2cpp_gc allocation counters.
        "System.GC" => name is "SuppressFinalize" or "ReRegisterForFinalize" or "Collect"
            or "WaitForPendingFinalizers" or "GetTotalMemory" or "KeepAlive"
            or "_CollectionCount" or "GetMemoryInfo"
            or "GetAllocatedBytesForCurrentThread" or "GetTotalAllocatedBytes",
        // SpanHelpers.Memmove -> std::memmove. Cutting the managed entry makes both its
        // hand-unrolled Unsafe.* loop and the SpanHelpers::memmove InternalCall fallback
        // (MemmoveNative) unreachable.
        "System.SpanHelpers" => name == "Memmove",
        // Get/SetLastPInvokeError -> the per-thread errno slot (FCalls). PtrToStringUTF8 ->
        // the dn2cpp UTF-8 string decoders; its real body reaches
        // String.CreateStringFromEncoding — an intrinsic-type member with no mapping, i.e.
        // the UTF-8-decode + SIMD subtree.
        "System.Runtime.InteropServices.Marshal" =>
            name is "GetLastPInvokeError" or "SetLastPInvokeError" or "PtrToStringUTF8",
        // NativeLibrary.GetSymbol -> dn2cpp_native_library_get_symbol. Its real body is the
        // raw compiler-generated `<GetSymbol>g____PInvoke` QCall stub — module "QCall", no
        // managed implementation to link against.
        "System.Runtime.InteropServices.NativeLibrary" => name == "GetSymbol",
        // Interop.GetRandomBytes -> a deterministic fixed-seed fill. It is the IL forwarder to
        // the bodyless InternalCall Interop+Sys::GetNonCryptographicallySecureRandomBytes;
        // cutting the forwarder is what makes that InternalCall unreachable.
        "Interop" => name == "GetRandomBytes",
        // Assembly.GetEntryAssemblyNative -> a no-op null stub (a self-hosted native binary has
        // no managed entry assembly). The CUT here is redundant — System.Reflection.Assembly is
        // an s_intrinsicTypes member, so IsIntrinsicType has already answered — but the ROUTE
        // is not: it must fire ahead of the intrinsic-type route, which has no arm for this
        // bodyless InternalCall and would throw.
        "System.Reflection.Assembly" => name == "GetEntryAssemblyNative",
        // Ordinal's OrdinalIgnoreCase comparison primitives -> the ordinal-fold runtime helpers
        // (dn2cpp_ordinal_casing.cpp). Their real bodies fall out of a SIMD/scalar ASCII fast
        // path into CompareStringIgnoreCaseNonAscii -> GlobalizationMode.get_Invariant -> the
        // ICU InitICUFunctions InternalCall — the globalization carve-out. Ordinal's other
        // members (the pure-ordinal IndexOf/Compare) are NOT named here and transpile normally.
        "System.Globalization.Ordinal" => name is "EqualsIgnoreCase" or "CompareStringIgnoreCase"
            or "IndexOfOrdinalIgnoreCase",
        // The primitives the real ArrayPool<T>.Shared reaches, plus the registry read the
        // public GetEnvironmentVariable(name, User|Machine) forwards to.
        // GetEnvironmentVariableCore_NoArrayPool -> dn2cpp_env_get_variable (real body: the
        // GetEnvironmentVariableW QCall); get_TickCount64 -> dn2cpp_tickcount64 (real body: the
        // GetLowResolutionTimestamp QCall); GetEnvironmentVariableFromRegistry ->
        // dn2cpp_env_get_variable_from_registry (real Windows body reaches Advapi32.RegOpenKeyEx
        // through Internal.Win32.RegistryKey / SafeRegistryHandle — no intrinsic mapping; the
        // Unix body just returns null). The split with LoweredEnvMember is deliberate: that
        // predicate names System.Environment's PUBLIC surface over a DISJOINT set of names and
        // takes the opposite remedy — its unmodeled overloads fall through, because their real
        // bodies transpile. One type, two predicates, because one half is BCL surface and the
        // other is a QCall / the registry subtree.
        "System.Environment" => name is "GetEnvironmentVariableCore_NoArrayPool" or "get_TickCount64"
            or "GetEnvironmentVariableFromRegistry",
        // Utilities.GetMemoryPressure -> Low. Its real body reads GC.GetGCMemoryInfo — native GC
        // accounting dn2cpp does not model.
        "System.Buffers.Utilities" => name == "GetMemoryPressure",
        // Gen2GcCallback: the WHOLE class, name ignored. Register(Func<bool>) is a no-op (Boehm
        // has no gen2-notification hook, and pool trimming is QoS, not contract — parked buffers
        // simply stay parked), the class is never instantiated because Register is the only thing
        // that constructs it, and cutting it keeps its CriticalFinalizerObject/WeakGCHandle
        // machinery out of the tree. Naming the CLASS rather than the one member routes every
        // member into EmitRuntimePrimitive, where everything but Register hits the throw — so no
        // other member can fall through to a body the cut deleted.
        "System.Gen2GcCallback" => true,
        // Buffer's byte-granular surface -> dn2cpp_buffer_blockcopy / _bytelength /
        // _getbyte / _setbyte / std::memmove / std::memset. BlockCopy's and ByteLength's real
        // bodies reach the Array.NativeLength / GetCorElementTypeOfElementType / GetElementSize
        // InternalCalls, Get/SetByte's the non-generic MemoryMarshal.GetArrayDataReference(Array)
        // -> RuntimeHelpers.GetMethodTable pointer math;
        // BulkMoveWithWriteBarrier's is the write-barrier FCall pair plus a Thread.FastPollGC
        // poll, all meaningless under Boehm; ZeroMemoryInternal is a bodyless InternalCall.
        // Buffer's OTHER members (Memmove, MemoryCopy) are not named here and transpile normally.
        "System.Buffer" => name is "BlockCopy" or "ByteLength" or "GetByte" or "SetByte"
            or "BulkMoveWithWriteBarrier" or "ZeroMemoryInternal",
        // Number's private group-sizes readers -> the runtime NFI's modeled
        // NumberGroupSizes array, through the same helper the public properties use. Their
        // real bodies ldfld the intrinsic-mapped NumberFormatInfo, which has no
        // transpilable C++ layout at all.
        "System.Number" => name is "NumberGroupSizes" or "CurrencyGroupSizes" or "PercentGroupSizes",
        // AppContext.BaseDirectory -> the running executable's directory. Its real getter's
        // GetBaseDirectoryCore fallback resolves the entry assembly's Location — meaningless in a
        // binary with no managed entry assembly — and cutting it keeps Assembly.GetEntryAssembly
        // and the AppContext data dictionary out of the tree.
        "System.AppContext" => name == "get_BaseDirectory",
        // The transcode workhorses -> the portable .NET-exact runtime decoders/encoders.
        // Encoding.GetString's real body reaches CreateStringFromEncoding and with it the SIMD
        // UTF-8 transcode subtree; Ascii's Widen/Narrow ARE that subtree's leaves (the
        // `_Vector`/`_Intrinsified` paths over the generic ISimdVector abstraction, which dn2cpp
        // does not transpile); Utf8Utility.TranscodeToUtf8 is the DWORD/SIMD pointer-stepped
        // UTF-16 -> UTF-8 workhorse both Utf8.FromUtf16 and UTF8Encoding.GetBytes forward to.
        "System.Text.Encoding" or "System.Text.ASCIIEncoding" or "System.Text.UTF8Encoding"
            or "System.Text.UnicodeEncoding" => name == "GetString",
        "System.Text.Ascii" => name is "WidenAsciiToUtf16" or "NarrowUtf16ToAscii",
        "System.Text.Unicode.Utf8Utility" => name == "TranscodeToUtf8",
        // The compiled-Regex classes: the WHOLE of each, name ignored. Regex.Compile folds to
        // a dead null factory (its RuntimeFeature.IsDynamicCodeCompiled guard const-folds
        // false, so RegexOptions.Compiled degrades to the interpreter — the NativeAOT
        // posture), and cutting these four keeps RegexCompiler and the whole
        // System.Reflection.Emit closure out of the tree. Nothing constructs them today
        // precisely BECAUSE Compile folds to null; naming the classes here routes every member
        // into EmitRuntimePrimitive, which has no arm for them, so a path that ever reached one
        // hits the throw instead of emitting a call to a symbol nothing generated.
        "System.Text.RegularExpressions.RegexCompiler"
            or "System.Text.RegularExpressions.RegexLWCGCompiler"
            or "System.Text.RegularExpressions.CompiledRegexRunner"
            or "System.Text.RegularExpressions.CompiledRegexRunnerFactory" => true,
        _ => false,
    };

    /// <summary>The exception types the runtime *itself* raises (the trap helpers /
    /// File I/O paths), mapped to the stable runtime type-info handle it stamps. These
    /// must be referenced by the SAME symbol in the emitted code (isinst / castclass /
    /// catch dispatch / exception newobj), so a typed cast or catch matches both a
    /// runtime-trapped object and a managed <c>new</c> of the type (the runtime
    /// and emit sources must share one type-info per type). Other reference types use
    /// their emitted <c>ti_*</c>. System.Exception itself maps here too (its runtime
    /// handle is the canonical base every exception chains to).</summary>
    private static readonly Dictionary<string, string> s_runtimeExceptionTypeInfo = new()
    {
        ["System.Exception"] = "&dn2cpp_exception_type",
        ["System.OverflowException"] = "&dn2cpp_overflow_exception_type",
        ["System.IndexOutOfRangeException"] = "&dn2cpp_index_out_of_range_exception_type",
        ["System.ArgumentException"] = "&dn2cpp_argument_exception_type",
        ["System.ArgumentOutOfRangeException"] = "&dn2cpp_argument_out_of_range_exception_type",
        ["System.ArgumentNullException"] = "&dn2cpp_argument_null_exception_type",
        ["System.InvalidOperationException"] = "&dn2cpp_invalid_operation_exception_type",
        // ObjectDisposedException derives from InvalidOperationException, so raising the base
        // type instead would still be caught by `catch (InvalidOperationException)` while
        // `catch (ObjectDisposedException)` — what the caller of a disposed stream actually
        // writes — would silently never match.
        ["System.ObjectDisposedException"] = "&dn2cpp_object_disposed_exception_type",
        ["System.ArithmeticException"] = "&dn2cpp_arithmetic_exception_type",
        // Raised where a size computation overflows before the allocator is asked
        // (StringBuilder.Insert's value.Length * count). The allocator's OWN
        // failures stay aborts — minting the exception object needs the
        // allocation that just failed — so this handle covers the overflow half
        // only; the asymmetry is written out at dn2cpp_out_of_memory_exception_type.
        ["System.OutOfMemoryException"] = "&dn2cpp_out_of_memory_exception_type",
        ["System.InvalidCastException"] = "&dn2cpp_invalid_cast_exception_type",
        ["System.TypeLoadException"] = "&dn2cpp_type_load_exception_type",
        ["System.NotSupportedException"] = "&dn2cpp_not_supported_exception_type",
        ["System.PlatformNotSupportedException"] = "&dn2cpp_platform_not_supported_exception_type",
        ["System.FormatException"] = "&dn2cpp_format_exception_type",
        ["System.IO.IOException"] = "&dn2cpp_io_exception_type",
        ["System.IO.FileNotFoundException"] = "&dn2cpp_file_not_found_exception_type",
        // Raised by the Windows Path.GetFullPath arm. Without this row a
        // `catch (PathTooLongException)` binds an emitted type-info the runtime
        // never raises, and only the IOException base clause would ever fire.
        ["System.IO.PathTooLongException"] = "&dn2cpp_path_too_long_exception_type",
        ["System.UnauthorizedAccessException"] = "&dn2cpp_unauthorized_access_exception_type",
        ["System.Collections.Generic.KeyNotFoundException"] = "&dn2cpp_key_not_found_exception_type",
        ["System.AggregateException"] = "&dn2cpp_aggregate_exception_type",
        ["System.Reflection.AmbiguousMatchException"] = "&dn2cpp_ambiguous_match_exception_type",
        // Raised by the array block-move helpers on a rank mismatch, so a
        // `catch (RankException)` in the same program must bind THIS handle.
        ["System.RankException"] = "&dn2cpp_rank_exception_type",
        // Raised by Array.Copy's type-compatibility verdict — the element-type half of the
        // same check the RankException row above covers.
        ["System.ArrayTypeMismatchException"] = "&dn2cpp_array_type_mismatch_exception_type",
        ["System.MissingMethodException"] = "&dn2cpp_missing_method_exception_type",
        // ResourceManager's missing-set diagnosis. The .NET documentation tells a caller to
        // write `catch (MissingManifestResourceException)`, and a clause bound to an emitted
        // ti_System_Resources_MissingManifestResourceException would compile, link, and not
        // fire against the instance the runtime raises.
        ["System.Resources.MissingManifestResourceException"] =
            "&dn2cpp_missing_manifest_resource_exception_type",
        // Raised by runtime entry points handed a null managed receiver (a null
        // FieldInfo's GetValue), matching real .NET's instance-call NRE.
        ["System.NullReferenceException"] = "&dn2cpp_null_reference_exception_type",
        // The emitted div/rem guards raise it, so a `catch (DivideByZeroException)` in the
        // same program must bind THIS handle — an emitted ti_ would not match a
        // runtime-thrown instance and the catch would silently not fire.
        ["System.DivideByZeroException"] = "&dn2cpp_divide_by_zero_exception_type",
        // ReaderWriterLockSlim's per-thread ownership checks raise these: a recursion-policy
        // violation and a release-without-hold. The .NET docs tell a caller to catch them by
        // type, so the clause must bind the handle the runtime stamps.
        ["System.Threading.LockRecursionException"] = "&dn2cpp_lock_recursion_exception_type",
        ["System.Threading.SynchronizationLockException"] =
            "&dn2cpp_synchronization_lock_exception_type",
    };

    /// <summary>The shared runtime <c>Dn2CppTypeInfo*</c> expression for an exception
    /// type the runtime raises (see <see cref="s_runtimeExceptionTypeInfo"/>), or null
    /// for any other type (which uses its emitted <c>ti_*</c>).</summary>
    public static string? RuntimeExceptionTypeInfo(string? fullTypeName) =>
        fullTypeName is not null && s_runtimeExceptionTypeInfo.TryGetValue(fullTypeName, out var h) ? h : null;

    /// <summary>The CLR types whose <c>Dn2CppTypeInfo</c> the C++ runtime OWNS: it
    /// hand-writes the object, stamps it on every instance it allocates, and knows things
    /// about it the transpiled image does not (System.Int32's <c>DN2CPP_TF_PRIMITIVE</c>
    /// bit, System.String's real extent, System.Type's base chain through the reflection
    /// hierarchy). For these the emitter references the runtime handle and defines no
    /// rival <c>ti_</c> — the sibling posture to <see cref="s_runtimeExceptionTypeInfo"/>,
    /// where the emitted metadata is the truth and is copied INTO the runtime's stub at
    /// startup (<c>dn2cpp_type_binds</c>).
    ///
    /// <para><b>The invariant both tables serve is that ONE CLR type has exactly ONE
    /// <c>Dn2CppTypeInfo</c> in the linked program.</b> A dn2cpp <c>Type</c> IS its type-info
    /// pointer — <c>dn2cpp_get_type_from_handle</c> interns one <c>Dn2CppType</c> per handle,
    /// so <c>Type.Equals</c> / <c>op_Equality</c> / <c>GetHashCode</c> /
    /// <c>ReferenceEquals</c> all reduce to that pointer. Two handles for one type are not
    /// assignable to each other, compare unequal, hash apart, and intern two rows in every
    /// table keyed by a declaring type-info — so <c>typeof(MyClass).BaseType ==
    /// typeof(object)</c> would be false while <c>typeof(object) == new
    /// object().GetType()</c> is true.</para>
    ///
    /// <para>Membership is not a judgement call: it is exactly the set of CLR names the
    /// runtime sources define a <c>Dn2CppTypeInfo</c> for.
    /// <c>gates/build-and-run-reflect-types.sh</c> re-derives that set from
    /// <c>runtime/core/**.cpp</c> and fails on any name this file does not carry — a handle
    /// added there without a row here would go back to shadowing, the fail-<b>open</b>
    /// direction nothing downstream reports. The two tables must stay disjoint (a type
    /// cannot both own its metadata and receive it); the static constructor below asserts
    /// that rather than trusting it.</para></summary>
    private static readonly Dictionary<string, string> s_runtimeOwnedTypeInfo = new()
    {
        // The root. Its runtime handle is what `new object()`, every boxed value and every
        // runtime-allocated object carries. An emitted ti_System_Object would be a bare stub
        // (System.Object is intrinsic, so it carries no vtable, fields or member tables)
        // serving only as a second identity on emitted classes' base pointers.
        ["System.Object"] = "&dn2cpp_object_type",
        ["System.String"] = "&dn2cpp_string_type",
        ["System.Void"] = "&dn2cpp_void_type",
        // The primitives. The runtime handle carries DN2CPP_TF_PRIMITIVE, which the class
        // emitter does not model — so here the runtime really does know more, and binding
        // the emitted metadata in would silently drop Type.IsPrimitive.
        ["System.Boolean"] = "&dn2cpp_bool_type",
        ["System.Char"] = "&dn2cpp_char_type",
        ["System.SByte"] = "&dn2cpp_sbyte_type",
        ["System.Byte"] = "&dn2cpp_byte_type",
        ["System.Int16"] = "&dn2cpp_int16_type",
        ["System.UInt16"] = "&dn2cpp_uint16_type",
        ["System.Int32"] = "&dn2cpp_int32_type",
        ["System.UInt32"] = "&dn2cpp_uint32_type",
        ["System.Int64"] = "&dn2cpp_int64_type",
        ["System.UInt64"] = "&dn2cpp_uint64_type",
        ["System.IntPtr"] = "&dn2cpp_intptr_type",
        ["System.UIntPtr"] = "&dn2cpp_uintptr_type",
        ["System.Single"] = "&dn2cpp_single_type",
        ["System.Double"] = "&dn2cpp_double_type",
        // The value types the runtime represents with its own C++ struct: the payload a box
        // carries is the runtime's, so the handle stamped on it must be too.
        ["System.Decimal"] = "&dn2cpp_decimal_type",
        ["System.TimeSpan"] = "&dn2cpp_timespan_type",
        ["System.DateTime"] = "&dn2cpp_datetime_type",
        ["System.DateTimeOffset"] = "&dn2cpp_datetimeoffset_type",
        ["System.DateOnly"] = "&dn2cpp_dateonly_type",
        ["System.TimeOnly"] = "&dn2cpp_timeonly_type",
        // The reflection hierarchy (bases wired in dn2cpp_runtime.cpp). Every reflected
        // object the runtime hands out (Dn2CppType, Dn2CppMethodRef, …) carries one of these
        // headers, and the transpiled t_* shells do not describe those layouts, so the
        // runtime handle is the only sound identity.
        ["System.Type"] = "&dn2cpp_type_type",
        // System.RuntimeType resolves to the SAME handle as System.Type: every Type object
        // the runtime hands out carries the &dn2cpp_type_type header — a dn2cpp Type IS the
        // runtime's RuntimeType — so a BCL `(RuntimeType)typeof(bool)` castclass (the real
        // System.Enum..cctor) or an `enumType is RuntimeType` test verifies against that
        // header. Against an emitted ti_System_RuntimeType such a castclass is structurally
        // ALWAYS false, since nothing allocates a transpiled RuntimeType and the shared
        // handle's base chain never names it. The accepted residue: a body reading a
        // RuntimeType field off the cast result would misread a Dn2CppType as the
        // t_System_RuntimeType layout — but those members are InternalCall plumbing dn2cpp
        // cuts, so no such body enters the tree, and a virtual dispatch lands on vt_ entries
        // that fail loud (dn2cpp_vcall_unimplemented).
        ["System.RuntimeType"] = "&dn2cpp_type_type",
        // TypeInfo sits between Type and MemberInfo in real .NET, and every runtime Type
        // object IS its own TypeInfo (RuntimeType : TypeInfo) — so `t is TypeInfo` / a
        // (TypeInfo) cast must match the shared Type handle
        // (IntrospectionExtensions.GetTypeInfo returns t unchanged).
        ["System.Reflection.TypeInfo"] = "&dn2cpp_type_type",
        ["System.Reflection.MemberInfo"] = "&dn2cpp_memberinfo_type",
        ["System.Reflection.MethodBase"] = "&dn2cpp_methodbase_type",
        ["System.Reflection.MethodInfo"] = "&dn2cpp_methodinfo_type",
        ["System.Reflection.ConstructorInfo"] = "&dn2cpp_constructorinfo_type",
        ["System.Reflection.FieldInfo"] = "&dn2cpp_fieldinfo_type",
        ["System.Reflection.PropertyInfo"] = "&dn2cpp_propertyinfo_type",
        ["System.Reflection.ParameterInfo"] = "&dn2cpp_parameterinfo_type",
        ["System.Reflection.CustomAttributeData"] = "&dn2cpp_customattributedata_type",
        // The headerless `const char*` Assembly/Module handles escape into `object` through
        // an interned runtime wrapper (dn2cpp_asm_wrap), whose header is the PRIVATE
        // implementation identity real .NET reports — RuntimeAssembly/RuntimeModule, each
        // based on its public abstract shell — so GetType() == typeof(Assembly) is false and
        // `o is Assembly` true via the base chain, as on .NET. All four are rowed so
        // typeof/isinst/castclass name the wrapper's own symbols and no emitted ti_ shadows
        // them.
        ["System.Reflection.Assembly"] = "&dn2cpp_assembly_type",
        ["System.Reflection.Module"] = "&dn2cpp_module_type",
        ["System.Reflection.RuntimeAssembly"] = "&dn2cpp_runtime_assembly_type",
        ["System.Reflection.RuntimeModule"] = "&dn2cpp_runtime_module_type",
        // The degraded stack-trace objects and the NFI wrappers: same argument, a runtime
        // object with a hand-written layout stamped with a hand-written handle.
        ["System.Diagnostics.StackTrace"] = "&dn2cpp_stacktrace_type",
        ["System.Diagnostics.StackFrame"] = "&dn2cpp_stackframe_type",
        ["System.Globalization.CultureInfo"] = "&dn2cpp_cultureinfo_type",
        ["System.Globalization.NumberFormatInfo"] = "&dn2cpp_numberformatinfo_type",
        ["System.Globalization.TextInfo"] = "&dn2cpp_textinfo_type",
        ["System.Resources.ResourceManager"] = "&dn2cpp_resourcemanager_type",
        ["System.Text.StringBuilder"] = "&dn2cpp_stringbuilder_type",
        // The threading objects the runtime allocates and drives: an instance carries the
        // handle, so an emitted ti_ beside it is a second identity for one type. Keys are
        // ClassInfo.FullName, which is arity-stripped for a generic (ThreadLocal<T> reads
        // "System.Threading.ThreadLocal", the spelling s_specialTypeCpp already uses) —
        // NOT the display name in the runtime's own `name` field.
        ["System.Threading.Thread"] = "&dn2cpp_thread_type",
        ["System.Threading.Timer"] = "&dn2cpp_timer_type",
        ["System.Threading.Barrier"] = "&dn2cpp_barrier_type",
        ["System.Threading.CountdownEvent"] = "&dn2cpp_countdown_type",
        ["System.Threading.SemaphoreSlim"] = "&dn2cpp_semaphore_type",
        ["System.Threading.ReaderWriterLockSlim"] = "&dn2cpp_rwlock_type",
        ["System.Threading.CancellationTokenSource"] = "&dn2cpp_cancel_source_type",
        ["System.Threading.Tasks.ParallelLoopState"] = "&dn2cpp_parallel_loop_state_type",
        ["System.Threading.Tasks.ParallelOptions"] = "&dn2cpp_parallel_options_type",
        // The event family: four CLR types over one Dn2CppEvent — but, unlike the
        // Task/ThreadLocal families next door, NOT instantiations of one type, so a single
        // shared handle would be merely lossy. One row each, on the real base chain the
        // runtime wires (ManualResetEvent/AutoResetEvent sealed under EventWaitHandle,
        // EventWaitHandle under WaitHandle, ManualResetEventSlim not a WaitHandle at all),
        // so `o is ManualResetEvent` is true for one and false for its three relatives.
        // WaitHandle is rowed because it is the base the chain ends at: without a row,
        // typeof(WaitHandle) would name an emitted stub while every instance's chain names
        // the runtime handle, and `o is WaitHandle` would stay false.
        ["System.Threading.WaitHandle"] = "&dn2cpp_waithandle_type",
        ["System.Threading.EventWaitHandle"] = "&dn2cpp_event_type",
        ["System.Threading.ManualResetEvent"] = "&dn2cpp_manualresetevent_type",
        ["System.Threading.AutoResetEvent"] = "&dn2cpp_autoresetevent_type",
        ["System.Threading.ManualResetEventSlim"] = "&dn2cpp_manualreseteventslim_type",
    };

    /// <summary>The runtime type-info handles that are deliberately NOT rows above, each
    /// with the reason. The set exists because
    /// <c>gates/build-and-run-reflect-types.sh</c> re-derives every handle the runtime
    /// sources define and fails on one this file does not account for — so an omission has
    /// to be a decision somebody wrote down, not a gap somebody did not notice. Named by
    /// the C++ symbol, since that is what the gate reads out of the runtime sources.
    ///
    /// <para>Two reasons only. A handle that models MORE THAN ONE CLR type is a deliberate
    /// type ERASURE, and a row would make the erasure worse rather than better: every
    /// <c>Task&lt;T&gt;</c> shares <c>Dn2CppTask</c>, so a row for System.Threading.Tasks.Task
    /// would make <c>t.GetType() == typeof(Task)</c> true for a <c>Task&lt;int&gt;</c>. Their
    /// <c>typeof</c> route is keyed on the runtime STRUCT instead
    /// (<c>MethodCompiler.TypeInfoExprOf</c>'s <c>IntrinsicCppName</c> arms), which is the
    /// only key that can name a family. And a handle whose CLR name is not a
    /// <c>ClassInfo.FullName</c> at all cannot be keyed here: FullName is arity-stripped and
    /// carries no enclosing type, so a private nested runtime shape would need the key
    /// "Node", which any type in any namespace could match.</para></summary>
    private static readonly Dictionary<string, string> s_runtimeTypeInfoNotRowed = new()
    {
        ["dn2cpp_task_type"] = "erasure: the whole intrinsic Task family shares Dn2CppTask",
        ["dn2cpp_task_awaiter_type"] = "erasure: every TaskAwaiter(<T>)/ValueTaskAwaiter/Configured* form shares Dn2CppTaskAwaiter",
        ["dn2cpp_yield_awaiter_type"] = "erasure: YieldAwaitable+YieldAwaiter is nested, and its FullName is the bare \"YieldAwaiter\"",
        ["dn2cpp_threadlocal_type"] = "erasure: every ThreadLocal<T> shares one handle (T rides as data, not in the header)",
        ["dn2cpp_blockingcollection_type"] = "erasure: every BlockingCollection<T> shares one handle, ditto",
        ["dn2cpp_array_i4_type"] = "erasure: the conservative shared int32[] handle a runtime-allocated array carries; the precise identity is ti_arr_Int32",
        ["dn2cpp_array_ref_type"] = "erasure: the conservative shared object[] handle, ditto",
        ["dn2cpp_array_n_type"] = "erasure: the imprecise packed (element-unknown) array handle — its name is the abstract base's, not a CLR array type",
        ["dn2cpp_blockingnode_type"] = "not a FullName: BlockingCollection<T>+Node is private and nested, so its FullName is the bare \"Node\"",
        ["dn2cpp_reflbind_type"] = "not a CLR type: <ReflectionDelegateBind>, a runtime-internal shape",
        ["dn2cpp_vts_bridge_type"] = "not a CLR type: dn2cpp.ValueTaskSourceBridge",
        ["dn2cpp_workitem_invoker_type"] = "not a CLR type: dn2cpp.WorkItemInvoker",
        // The third pattern, and the only member of it: the handle is a FALLBACK the
        // generated code overwrites by registration (dn2cpp_set_canceled_exception_type),
        // so the two never coexist as identities — a program that names the type registers
        // its emitted ti_ and the runtime raises THAT, and one that never names it cannot
        // tell the two apart. A row here would be wrong rather than redundant: it would
        // make the emitter reference a symbol the runtime deliberately keeps file-local.
        ["dn2cpp_operation_canceled_exception_type"] =
            "registered, not shadowed: generated code installs the emitted ti_ through "
            + "dn2cpp_set_canceled_exception_type; this handle is only the never-named fallback",
    };

    /// <summary>The declared non-row reason for a runtime type-info handle, or null when
    /// the handle is expected to carry a row. Read by the doc/gate check described at
    /// <see cref="s_runtimeTypeInfoNotRowed"/>; exposed rather than private so the answer
    /// has one source.</summary>
    public static string? RuntimeTypeInfoNotRowedReason(string cppSymbol) =>
        s_runtimeTypeInfoNotRowed.TryGetValue(cppSymbol, out var why) ? why : null;

    /// <summary>Every (CLR name, handle expression) the three tables carry, ordinally
    /// ordered — the SEED of the type-name registry (<c>CppEmitter.EmitTypeRegistry</c>).
    /// One registry has two mouths: <c>typeof</c>, which asks
    /// <see cref="RuntimeTypeInfoSymbol"/> through <see cref="ClassInfo.CppTypeInfoName"/>,
    /// and <c>Type.GetType</c>, which reads the emitted table. They answer from this one
    /// list so a handle rowed above reaches both; a hand-written second list would let the
    /// name route answer null about a type typeof answers about. Ordered here rather than at
    /// the emitter because dictionary enumeration order is not a contract and the row order
    /// is emitted text.</summary>
    public static IEnumerable<(string Name, string Handle)> RuntimeTypeInfoRows() =>
        s_runtimeExceptionTypeInfo
            .Concat(s_runtimeBoundTypeInfo)
            .Concat(s_runtimeOwnedTypeInfo)
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => (kv.Key, kv.Value));

    /// <summary>Every runtime type-info handle this file carries a row for, by C++ symbol
    /// (no leading <c>&amp;</c>) — the other half of the same check.</summary>
    public static IEnumerable<string> RuntimeTypeInfoSymbols() =>
        s_runtimeExceptionTypeInfo.Values
            .Concat(s_runtimeBoundTypeInfo.Values)
            .Concat(s_runtimeOwnedTypeInfo.Values)
            .Select(v => v[1..])
            .Distinct();

    /// <summary>The CLR types whose runtime handle is a STUB the emitted metadata fills in
    /// — the non-exception half of the <c>dn2cpp_type_binds</c> family. One row today:
    /// System.Enum, whose handle every emitted enum's <c>base</c> points at while
    /// <c>typeof(Enum)</c> named the emitted <c>ti_System_Enum</c>, so
    /// <c>typeof(E).BaseType == typeof(Enum)</c> was false. Binding rather than owning is
    /// forced by which side knows more: the emitted metadata carries System.Enum's real
    /// vtable, field/method tables and base (ValueType), and the runtime's stub carries
    /// none of it, so owning would DELETE an answer typeof already gives.
    /// (<c>dn2cpp_enum_set_interfaces</c> runs after <c>dn2cpp_runtime_init</c> in the
    /// generated prologue, so the interface rows it installs survive the bind's whole-struct
    /// copy.)</summary>
    private static readonly Dictionary<string, string> s_runtimeBoundTypeInfo = new()
    {
        ["System.Enum"] = "&dn2cpp_enum_type",
    };

    static CoreIntrinsics()
    {
        // The three tables partition the runtime-handle set: a type either owns its
        // metadata or receives it, never both, and a name in two tables would make
        // ClassInfo.CppTypeInfoName's answer depend on lookup order — which is the two-lists
        // failure the tables exist to remove.
        foreach (var name in s_runtimeOwnedTypeInfo.Keys)
            if (s_runtimeExceptionTypeInfo.ContainsKey(name) || s_runtimeBoundTypeInfo.ContainsKey(name))
                throw new InvalidOperationException(
                    $"runtime type-info tables overlap on {name}: a type either owns its "
                    + "metadata (s_runtimeOwnedTypeInfo) or receives it at startup "
                    + "(s_runtimeExceptionTypeInfo / s_runtimeBoundTypeInfo), never both.");
        foreach (var name in s_runtimeBoundTypeInfo.Keys)
            if (s_runtimeExceptionTypeInfo.ContainsKey(name))
                throw new InvalidOperationException(
                    $"runtime type-info tables overlap on {name}: s_runtimeBoundTypeInfo is "
                    + "the NON-exception half of the bind family.");
    }

    /// <summary>THE funnel for "which <c>Dn2CppTypeInfo</c> symbol IS this CLR type" when the
    /// C++ runtime defines one — the union of the three tables above, and the single question
    /// <see cref="ClassInfo.CppTypeInfoName"/> asks. Null means the emitted <c>ti_</c> is the
    /// type's only type-info, which is the common case.</summary>
    public static string? RuntimeTypeInfoSymbol(string? fullTypeName) =>
        fullTypeName is null ? null
        : s_runtimeExceptionTypeInfo.TryGetValue(fullTypeName, out var ex) ? ex
        : s_runtimeBoundTypeInfo.TryGetValue(fullTypeName, out var bd) ? bd
        : s_runtimeOwnedTypeInfo.TryGetValue(fullTypeName, out var ow) ? ow
        : null;

    /// <summary>Whether the runtime OWNS this type's metadata, i.e. the emitter must define
    /// no type-info for it at all. False both for a type with no runtime handle (its emitted
    /// <c>ti_</c> is the definition) and for a bind-kind one (its emitted metadata is
    /// defined under <see cref="ClassInfo.CppTypeInfoDefName"/> and copied into the handle at
    /// startup).</summary>
    public static bool RuntimeOwnsTypeInfo(string? fullTypeName) =>
        fullTypeName is not null && s_runtimeOwnedTypeInfo.ContainsKey(fullTypeName);

    /// <summary>Whether an UNLOADED (External) type name is a BCL exception type —
    /// <c>System.Exception</c> itself or any <c>System.*</c> type whose name carries
    /// the mandatory <c>Exception</c> suffix (SystemException, ApplicationException,
    /// IO.IOException, …). Asked only where resolution has already failed: the
    /// base-TypeRef of a class whose corelib is not loaded
    /// (<see cref="Compilation.InheritsFromException"/>), the matching
    /// External ctor-param / CppTypes reference shapes, and the unresolved
    /// base-ctor <c>call</c> route in <c>MethodCompiler.TranslateIntrinsic</c>.
    /// A corelib-full build never consults it for these names — they resolve to
    /// loaded classes first — so it cannot change any resolved answer.
    /// <para>The suffix test leans on the Framework Design Guidelines rule that
    /// every BCL exception type (and only exception types) ends in "Exception";
    /// it subsumes the <see cref="s_runtimeExceptionTypeInfo"/> key set. A miss
    /// stays loud: an unrecognized External base keeps today's behavior (no
    /// exception rooting, and the base-ctor call fails the transpile naming the
    /// type), never a silent mis-layout.</para></summary>
    public static bool IsExternalBclExceptionName(string fullTypeName) =>
        fullTypeName == "System.Exception"
        || fullTypeName.StartsWith("System.", StringComparison.Ordinal)
           && fullTypeName.EndsWith("Exception", StringComparison.Ordinal);

    /// <summary>Whether a specific method is bounded (subtree cut + trap at the
    /// call site). See <see cref="s_boundedMethods"/>, and <see cref="BdCoreBounded"/>
    /// for the descriptor row that pairs this core set with its emit arm.</summary>
    public static bool IsBoundedMethod(string declType, string name) =>
        s_boundedMethods.ContainsKey((declType, name));

    /// <summary>The core set's per-row verdict — what the two substitute mouths (the
    /// call-site neutralization and the <c>ldftn</c> stub) do when the bounded callee turns
    /// out to be a bodyless <c>[DllImport]</c>.
    ///
    /// <para><see cref="BoundedVerdict.Silent"/> for a member outside the core table, the
    /// only sound answer: the other two halves of the bounded union are a backend's
    /// <see cref="IEmitBackend.AdditionalBoundedMethods"/> and the CLI's <c>--cut</c> specs,
    /// and neither can carry a verdict — a <c>--cut</c> is somebody deliberately deleting a
    /// call, so throwing would defeat the lever, and a backend's cuts model engine surface
    /// its own runtime answers for. Only the core table states a claim about what the zero
    /// MEANS, so only it may ask for a throw.</para></summary>
    public static BoundedVerdict BoundedVerdictOf(string declType, string name) =>
        s_boundedMethods.TryGetValue((declType, name), out var v) ? v : BoundedVerdict.Silent;

    /// <summary>A bounded VIRTUAL whose callvirt sites still dispatch through the
    /// vtable instead of being neutralized: only the base BODY is cut. Needed when
    /// user overrides must run (SynchronizationContext.Post — every C# call site
    /// names the base declaration, so neutralizing it would drop the override's
    /// dispatch too). See the s_boundedMethods entry's comment, and
    /// <see cref="BdVirtualDispatch"/> for the row the emit site asks.</summary>
    public static bool IsVirtualDispatchBounded(string declType, string name) =>
        (declType, name) == ("System.Threading.SynchronizationContext", "Post");

    /// <summary>The AssemblyLoadContext runtime-assembly-LOAD primitive: the
    /// [LibraryImport] QCall that maps and registers an IL assembly from a path. A
    /// transpiled binary resolves every input assembly at LINK time and cannot map, JIT or
    /// register an IL assembly at run time — mods/plugins are a structural non-goal — so its
    /// call site traps loudly with a catchable PlatformNotSupportedException naming the
    /// limitation (<c>MethodCompiler.EmitManagedCall</c>) rather than returning a 0/null
    /// default that would surface as a silent mod-load failure or a later NPE. Not a
    /// bounded-method default and not the fixed-message dynamic-codegen throw, but a
    /// written-out trap with a load-specific message.
    ///
    /// <para>Only the LOAD is trapped; the surrounding ALC plumbing is bounded to benign
    /// defaults instead (see s_boundedMethods). Creating an ALC object and querying a
    /// context are harmless until something loads INTO the context, and trapping them would
    /// abort startup — <c>AssemblyLoadContext.Default</c>'s own initialization runs the
    /// object ctor eagerly. GetLoadedAssemblies answers an empty array at its own call-site
    /// arm for the same startup-safety reason, and PrepareForAssemblyLoadContextRelease is
    /// unload bookkeeping bounded to a no-op (a finalizer must not throw).</para></summary>
    public static bool IsAssemblyLoadContextRuntimeLoad(string declType, string name) =>
        declType == "System.Runtime.Loader.AssemblyLoadContext"
        && name == "<LoadFromPath>g____PInvoke|5_0";

    /// <summary>The two private funnels the BASE <c>Stream.ReadAsync</c>/<c>WriteAsync</c>
    /// bodies call. Bounded — their real bodies are cut, see <see cref="s_boundedMethods"/> —
    /// but NOT neutralized to a default at the call site: they are rewritten to a synchronous
    /// dispatch through the <c>Read</c>/<c>Write</c> slot the subclass does override
    /// (<c>MethodCompiler.TryEmitStreamSyncOverAsyncFunnel</c>), which is what the scheduler
    /// machinery they replace computes anyway.
    ///
    /// <para>The rewrite lands on the BASE body only, structurally rather than by a name
    /// test: a type overriding the async surface has its own vtable slot, and these funnels
    /// are <c>private</c>, so an override cannot reach them.</para>
    ///
    /// <para>The synchronous call runs on the calling thread, so an exception out of
    /// <c>Read</c>/<c>Write</c> propagates from the <c>ReadAsync</c> CALL rather than
    /// surfacing as a faulted Task. Inside an <c>async</c> method the state machine faults
    /// its own task either way; the two part only for a caller that holds the Task before
    /// awaiting it, and .NET's own <c>ValidateBufferArguments</c> already throws
    /// synchronously off this same method.</para>
    ///
    /// <para>Both askers — the rewrite and the reach of the slot it dispatches through —
    /// reach this predicate through <see cref="BdStreamSyncFunnel"/>.</para>
    /// </summary>
    public static bool IsStreamSyncOverAsyncFunnel(string declType, string name) =>
        declType == "System.IO.Stream" && name is "BeginEndReadAsync" or "BeginEndWriteAsync";

    /// <summary>The dynamic-code-generation surface: BCL machinery that mints code
    /// at run time and therefore cannot exist in a transpiled image
    /// (RuntimeFeature.IsDynamicCodeSupported/Compiled already fold to false).
    /// Members of this surface are cut at reachability — so the enormous
    /// JIT-backed subtrees behind them (System.Linq.Expressions.Interpreter,
    /// ILGenerator, the DLR binder machinery) never enter the tree, no matter
    /// which library references them — and every call site / ldftn / newobj
    /// lowers to a catchable runtime PlatformNotSupportedException naming the
    /// member (the NativeAOT posture). Unlike <see cref="s_boundedMethods"/>
    /// (runtime-dead code neutralized to a default result), reaching this
    /// surface at run time is a real, diagnosable unsupported-feature error and
    /// must fail loudly.</summary>
    private static readonly HashSet<string> s_dynamicCodegenTypes = new()
    {
        // The DLR call-site cache (dynamic dispatch): CallSite.Create ->
        // Expression.Compile -> the interpreter. Arity-stripped, so this one
        // entry covers both the non-generic CallSite base and CallSite<T>.
        "System.Runtime.CompilerServices.CallSite",
    };

    /// <summary>Plain-data types in the System.Reflection.Emit namespace exempted
    /// from the surface's namespace-prefix rule: they generate no code (struct
    /// copies / enum values / the OpCodes constant table flowing through
    /// reachable signatures, e.g. Label[] arrays in ILGenerator-adjacent code)
    /// and must keep transpiling.</summary>
    private static readonly HashSet<string> s_reflectionEmitDataTypes = new()
    {
        "System.Reflection.Emit.Label",
        "System.Reflection.Emit.OpCode",
        "System.Reflection.Emit.OpCodes",
        "System.Reflection.Emit.FlowControl",
        "System.Reflection.Emit.OpCodeType",
        "System.Reflection.Emit.OperandType",
        "System.Reflection.Emit.StackBehaviour",
        "System.Reflection.Emit.PackingSize",
        "System.Reflection.Emit.AssemblyBuilderAccess",
    };

    /// <summary>Member-level surface entries on types that otherwise transpile:
    /// expression TREE BUILDING stays fully supported (libraries build trees as
    /// metadata without ever compiling them); only the tree-to-delegate step is
    /// cut. Keyed by arity-stripped open-definition name, so the
    /// Expression&lt;TDelegate&gt; entry matches every instantiation; matching is
    /// by name, so the Compile(bool preferInterpretation) overload is covered
    /// too.</summary>
    private static readonly HashSet<(string Type, string Method)> s_dynamicCodegenMethods = new()
    {
        ("System.Linq.Expressions.LambdaExpression", "Compile"),
        ("System.Linq.Expressions.Expression", "Compile"),
    };

    public static bool IsDynamicCodegenType(string defFullName) =>
        s_dynamicCodegenTypes.Contains(defFullName)
        || (defFullName.StartsWith("System.Reflection.Emit.", StringComparison.Ordinal)
            && !s_reflectionEmitDataTypes.Contains(defFullName));

    /// <summary>The dynamic-code-generation surface, keyed on the arity-stripped
    /// open-definition name. Reached by both askers through
    /// <see cref="BdDynamicCodegen"/>, wrapped in the namespace prefilter and the
    /// definition-name key that <see cref="Compilation.IsDynamicCodegenMember"/>
    /// adds.</summary>
    public static bool IsDynamicCodegenMember(string defFullName, string name) =>
        IsDynamicCodegenType(defFullName) || s_dynamicCodegenMethods.Contains((defFullName, name));

    /// <summary>Reference types under System.Net.Sockets exempted from the
    /// namespace rule in <see cref="IsAbsentNetworkPalType"/>: they carry data, not
    /// connectivity. A SocketException is CONSTRUCTED by the socket stack and CAUGHT
    /// by application code — it opens nothing and needs no platform layer — so cutting
    /// it would replace an error somebody handles with one nobody expects. (The
    /// namespace's enums and structs — SocketError, AddressFamily, IPPacketInformation
    /// — need no entry: <see cref="Compilation.IsAbsentNetworkPalMember"/> excludes
    /// value types and enums, as its dynamic-codegen sibling does.)</summary>
    private static readonly HashSet<string> s_absentNetworkPalDataTypes = new()
    {
        "System.Net.Sockets.SocketException",
    };

    /// <summary>A type whose members cannot work in a dn2cpp binary because the
    /// SOCKET platform layer is absent: there is no <c>SystemNative_Socket*</c> /
    /// <c>SystemNative_Connect*</c> / <c>SystemNative_GetHostEntry*</c> in
    /// <c>runtime/core/platform/posix/</c>, and never has been —
    /// <c>gates/build-and-run-http-get.sh</c> section 4 asserts the whole subtree
    /// stays out of the tree, because HTTP/HTTPS is served by the DnHttp transport
    /// (<see cref="IsInterceptedHttpHandlerMethod"/>) rather than by .NET's own
    /// connection pool.
    ///
    /// <para><b>Cutting it is what keeps the absence LOUD instead of a link error.</b>
    /// <c>libSystem.Native</c> is a runtime-PROVIDED P/Invoke module
    /// (<see cref="Compilation.IsRuntimeProvidedPInvokeModule"/>), so a socket import that
    /// entered the tree would lower to a direct native call to a symbol nothing defines — an
    /// undefined symbol at C++ link time. A library that opens its own socket (Grpc.Net
    /// .Client's load-balancing transport) reaches it without going anywhere near
    /// SocketsHttpHandler, so the BodyReplace on that handler is not a substitute.</para>
    ///
    /// <para>Namespace-keyed rather than a name list, the <see cref="IsDynamicCodegenType"/>
    /// shape and for the same reason: the members that reach the PAL are the whole live
    /// surface of Socket / SafeSocketHandle / SocketPal / SocketAsyncEngine /
    /// SocketAsyncContext / SocketAsyncEventArgs / NetworkStream, and a name list goes stale
    /// in the direction that fails silently. Nested types (whose dn2cpp FullName is a bare
    /// mangled name with no namespace) are not matched and need not be: each is reached only
    /// through a member of its declaring type, which this does match.</para></summary>
    public static bool IsAbsentNetworkPalType(string defFullName) =>
        defFullName.StartsWith("System.Net.Sockets.", StringComparison.Ordinal)
        && !s_absentNetworkPalDataTypes.Contains(defFullName);

    /// <summary>The absent-socket-PAL surface, keyed on the arity-stripped
    /// open-definition name. Reached by both askers through
    /// <see cref="BdAbsentNetworkPal"/>, wrapped in the namespace prefilter
    /// <see cref="Compilation.IsAbsentNetworkPalMember"/> adds.
    ///
    /// <para><c>System.Net.Dns</c> is here with a carve-out, and the carve-out is the
    /// honest half: <c>GetHostName</c> IS wired for real (the POSIX PAL implements
    /// <c>SystemNative_GetHostName</c>, and <c>gates/build-and-run-eventsource-probe.sh</c>
    /// drives it), while every other member — the <c>GetHostEntry*</c> /
    /// <c>GetHostAddresses*</c> family and the <c>RunAsync</c> helper behind their async
    /// forms — bottoms out in <c>SystemNative_GetHostEntryForName</c>, which nothing
    /// implements. Cut by NAME-exclusion rather than by enumerating the resolution
    /// members: the family gains overloads upstream, and an enumeration that missed one
    /// would fail as a link error rather than as a diagnostic.</para></summary>
    public static bool IsAbsentNetworkPalMember(string defFullName, string name) =>
        IsAbsentNetworkPalType(defFullName)
        || (defFullName == "System.Net.Dns" && name != "GetHostName");

    /// <summary>The three SocketsHttpHandler transport overrides whose real IL — the
    /// SetupHandlerChain -> connection-pool -> socket/DNS/TLS/QUIC subtree — is cut and
    /// replaced with a DnHttp shim call (see MethodCompiler.CompileHttpShimBody). ONE pure
    /// predicate both askers consult: the reachability cut (Compilation.Reach) keeps the
    /// method reachable — so its vtable slot stays a real pointer (CppEmitter.RenderVtable)
    /// and the abstract HttpMessageHandler dispatch lands on an emitted body — but never
    /// scans the real IL, and the emit route (CppEmitter) supplies the shim body in its
    /// place. NAME-gated, the intrinsic-edge discipline: an overload added upstream cannot
    /// slip through, because the shim body models exactly Send/SendAsync/Dispose. Decode-
    /// free — it reads FullName / Name only.</summary>
    public static bool IsInterceptedHttpHandlerMethod(string? declType, string name) =>
        declType == "System.Net.Http.SocketsHttpHandler"
        && name is "Send" or "SendAsync" or "Dispose";

    /// <summary>HttpClientHandler's ClientCertificateOptions accessors — both — whose real IL
    /// is cut and replaced (MethodCompiler.CompileHttpShimBody). The SETTER's own work is
    /// small — it installs a LocalCertificateSelectionCallback on the underlying handler's
    /// SslOptions and forwards the value — but each of its two arms takes the address of a
    /// lambda that calls CertificateHelper.GetEligibleClientCertificate, and reachability
    /// follows an ldftn: BOTH lambdas enter the tree whatever the caller's argument, dragging
    /// in X509Store -> the platform keychain -> ASN.1 -> BigInteger. That subtree is dead
    /// weight here — the callback is invoked only from .NET's own SslStream handshake, and the
    /// transport this build links (the DnHttp shim over vendored libcurl, see
    /// IsInterceptedHttpHandlerMethod) does its TLS in Mbed TLS instead, where no .NET callback
    /// is reachable. https:// works; a CLIENT certificate cannot be presented.
    ///
    /// <para>The setter is the cut point rather than GetEligibleClientCertificate itself
    /// because only here is the diagnostic addressed to the caller: a program that asks for
    /// client certificates learns so where it configured them, not from a callback that never
    /// fires. Hence the route REFUSES rather than no-ops — see CompileHttpShimBody.</para>
    ///
    /// <para>The GETTER is intercepted because the replaced setter stores nothing. The real
    /// getter bottoms out in HttpConnectionSettings._clientCertificateOptions, whose
    /// initializer is Automatic — Manual only ever lands there through the forward the
    /// replaced setter drops — so left alone it would answer Automatic after every accepted
    /// set of Manual. The synthesized getter answers Manual, and that constant is exact
    /// rather than approximate: Manual is the only value the guard body accepts (Automatic
    /// throws), so "the value the setter accepted" has one possible answer.</para>
    ///
    /// <para>Name-gated and decode-free (FullName / Name only), like its transport sibling. A
    /// property accessor cannot be overloaded, so each name identifies one method row; the
    /// route still asserts the SHAPE it models before emitting, per "cut by name, lower by
    /// shape".</para></summary>
    public static bool IsInterceptedHttpCertOptionMethod(string? declType, string name) =>
        declType == "System.Net.Http.HttpClientHandler"
        && name is "set_ClientCertificateOptions" or "get_ClientCertificateOptions";

    /// <summary>The system-proxy lookup behind <c>HttpClient.DefaultProxy</c>, per CoreLib
    /// flavor: MacProxy's two IWebProxy queries (macOS), and HttpWindowsProxy's
    /// parameterless constructor plus the same two queries (Windows). The real bodies
    /// P/Invoke the platform proxy stack — CFNetwork on macOS
    /// (<c>CFNetworkCopySystemProxySettings</c>, <c>CFNetworkCopyProxiesForURL</c>, the two
    /// proxy-auto-configuration entry points), WinHttp/IpHlpApi/Advapi32 on Windows — none
    /// of which a dn2cpp binary links; the body route answers the queries the way
    /// System.Net.Http's own <c>HttpNoProxy</c> does — <c>GetProxy</c> null,
    /// <c>IsBypassed</c> true (MethodCompiler.EmitSystemProxyNoProxy).
    ///
    /// <para>The CTOR is in the Windows set and not the macOS one because the Windows
    /// P/Invoke half starts there: <c>ConstructSystemProxy</c> news HttpWindowsProxy
    /// directly (<c>newobj</c> with a <c>ldnull</c> WinInetProxyHelper argument — no
    /// TryCreate seam), and the ctor opens a WinHttp session, walks the network interfaces
    /// (IpHlpApi) and registers a registry-change wait (Advapi32 +
    /// <c>ThreadPool.RegisterWaitForSingleObject</c>). Its replacement is an empty body,
    /// sound because the replaced queries read no instance state the real ctor would have
    /// filled. <c>Dispose</c> is in for the ctor's reason mirrored: the type is
    /// instantiated, so ANY reached <c>IDisposable.Dispose</c> dispatch marks its impl, whose
    /// real body unregisters the wait the replaced ctor never registered
    /// (RegisteredWaitHandle → the unmodeled GCHandle.Dispose) — empty, since the empty ctor
    /// acquired nothing. <c>GetMultiProxy</c> needs no row: its only callers are the
    /// replaced <c>GetProxy</c> and the connection pool the transport cut deletes, so it is
    /// never reached and its IMultiWebProxy slot degrades to the itable trap stub.</para>
    ///
    /// <para><b>A degrade and not a refusal, because "no proxy" is the TRUTHFUL description
    /// of what this build does</b> — the docs/ARCHITECTURE.md §4-B posture that a diagnostic
    /// answer degrades while a load-bearing one throws. Real .NET on a machine with no system
    /// proxy answers exactly this, so the answer needs no self-naming. Reporting a configured
    /// proxy would be the lie: the transport that issues the request is the DnHttp shim over
    /// vendored libcurl (<see cref="IsInterceptedHttpHandlerMethod"/>), wired to no proxy, so
    /// a caller told "use 10.0.0.1:8080" would watch the request go direct. Throwing is wrong
    /// too — <c>HttpClient.DefaultProxy</c> is read by callers that merely BRANCH on the
    /// result (Grpc.Net.Client's <c>GrpcChannel.IsProxied</c>), so a refusal would abort a
    /// program over a question it asked speculatively.</para>
    ///
    /// <para>The environment-variable proxy path (<c>HttpEnvironmentProxy</c>, which
    /// <c>SystemProxyInfo.ConstructSystemProxy</c> tries FIRST) is deliberately untouched: it
    /// is ordinary managed IL that transpiles, so an <c>HTTP_PROXY</c>-configured program
    /// still sees what real .NET sees. Only the framework-P/Invoke half is replaced.</para>
    ///
    /// <para>Name-gated and decode-free, like its two siblings. <c>get_Credentials</c> /
    /// <c>set_Credentials</c> are NOT here — on both types they are a plain field
    /// accessor pair that transpiles (answering null, right for a proxy that is not
    /// there) — and the helpers under the queries (<c>GetProxyUri</c>,
    /// <c>ExecuteProxyAutoConfiguration</c>, <c>UpdateConfiguration</c>) need no row: they
    /// are reached only from members whose scan this cut deletes.</para></summary>
    public static bool IsInterceptedSystemProxyMethod(string? declType, string name) =>
        (declType == "System.Net.Http.MacProxy"
            && name is "GetProxy" or "IsBypassed")
        || (declType == "System.Net.Http.HttpWindowsProxy"
            && name is ".ctor" or "Dispose" or "GetProxy" or "IsBypassed");

    /// <summary>Grpc.Net.Client's <c>GrpcChannel.HttpHandlerType</c> getter — the one member
    /// that decides which subchannel transport grpc builds. It answers
    /// <c>HttpHandlerType.Custom</c> here (MethodCompiler.EmitGrpcHandlerTypeCustom).
    ///
    /// <para><b>This corrects a TYPE-IDENTITY answer the transport body-replacement made
    /// stale; it does not hide a hole.</b> grpc sets that property by asking what the primary
    /// handler IS, and `SocketsHttpHandler` is its shorthand for "I may install a
    /// ConnectCallback on it and do my own socket connect" — which is why
    /// <c>GrpcChannel.SubChannelTransportFactory.Create</c> returns
    /// <c>SocketConnectivitySubchannelTransport</c> for it and
    /// <c>PassiveSubchannelTransport</c> for everything else. Under dn2cpp that inference is
    /// FALSE: the object is still a SocketsHttpHandler, but its Send/SendAsync/Dispose are
    /// body-replaced onto the DnHttp transport
    /// (<see cref="IsInterceptedHttpHandlerMethod"/>), so no ConnectCallback is ever
    /// consulted and there is no socket to connect. <c>Custom</c> makes grpc select the
    /// passive transport through its OWN code path, degrading as its authors designed for a
    /// non-socket handler. This is the DEFAULT path — a bare
    /// <c>GrpcChannel.ForAddress(...)</c> takes it — so it must not need a handler option in
    /// user code. The Bounded refusal on the socket surface itself
    /// (<see cref="Compilation.IsAbsentNetworkPalMember"/>) stays as the backstop for a
    /// program that genuinely configures load balancing.</para>
    ///
    /// <para>The narrowest possible member: a property GETTER, so nothing else in grpc's
    /// handler-context computation is disturbed. The setter is untouched — grpc may record
    /// whatever it likes; only the answer readers get is corrected.</para></summary>
    public static bool IsInterceptedGrpcHandlerTypeGetter(string? declType, string name) =>
        declType == "Grpc.Net.Client.GrpcChannel" && name == "get_HttpHandlerType";

    /// <summary>Every System.Net.Http method whose real IL the reachability cut deletes and
    /// whose body the emit route synthesizes in its place. Both askers ask THIS — the union is
    /// defined from the two intercepts rather than restated beside them, so a third intercept
    /// cannot reach one asker and miss the other. Which body to synthesize is decided by
    /// re-asking the same two predicates (CompileHttpShimBody), never by a separate list.
    ///
    /// <para>Both askers reach it through the descriptor row
    /// <see cref="BrHttpShim"/> (cut kind BodyReplace), which is where the pairing of this
    /// predicate with the emit arm that supplies the replacement body is written down.</para></summary>
    public static bool IsBodyReplacedHttpMethod(string? declType, string name) =>
        IsInterceptedHttpHandlerMethod(declType, name)
        || IsInterceptedHttpCertOptionMethod(declType, name)
        || IsInterceptedSystemProxyMethod(declType, name)
        || IsInterceptedGrpcHandlerTypeGetter(declType, name);

    /// <summary>A System.Enum instance method whose real IL reachability replaces with a
    /// synthesized body (the <see cref="BrEnumInstanceFormat"/> BodyReplace row): the four
    /// ToString overloads (parameterless, string, IFormatProvider, string+IFormatProvider),
    /// GetTypeCode, the three value-comparison overrides Equals(object) / GetHashCode() /
    /// CompareTo(object), and the private GetValue() (the boxer behind every IConvertible.To*
    /// override).
    ///
    /// <para>Their real bodies reach ToStringInlined/HandleRareTypes -> GetEnumInfo ->
    /// GetEnumValuesAndNames (an InternalCall) plus the RuntimeType-cache reflection cascade,
    /// and — the value trio and GetValue — switch on InternalGetCorElementType (a bodyless
    /// FCall, leaf-cut in Compilation.Resolve) into the underlying primitive's own
    /// GetHashCode/CompareTo or a per-CorElementType box arm. Deleting the scan of these
    /// members is what removes that whole cascade, which is why they are here rather than
    /// call-site intrinsics.</para>
    ///
    /// <para>Cut by NAME (which also catches the [Obsolete] ToString overloads) and lowered
    /// by SHAPE in CompileEnumInstanceFormatBody, total over the set — an unmodeled overload
    /// throws there rather than emitting a body that names the cut IL. The synthesized value
    /// bodies delegate to dn2cpp_object_gethashcode / dn2cpp_object_equals /
    /// dn2cpp_enum_compareto / dn2cpp_enum_get_value, which read the boxed enum's payload at
    /// its underlying width, so one body serves every enum across all storage widths.
    /// GetValue closes the whole IConvertible.To* family in one row: each member keeps its
    /// real body once GetValue no longer names the cut FCall.</para></summary>
    public static bool IsEnumInstanceFormatMethod(string? declType, string name) =>
        declType == "System.Enum"
        && name is "ToString" or "GetTypeCode" or "Equals" or "GetHashCode" or "CompareTo"
            or "GetValue";

    /// <summary>Boolean capability getters folded to a compile-time constant.
    /// Three mechanisms act on this table together: the call site pushes the
    /// constant (MethodCompiler.TryEmitConstFoldedGetter), the reachability
    /// edge to the getter body is cut (Compilation.ResolveCallTarget), and a
    /// `call getter; brtrue/brfalse` pair prunes the dead arm from both
    /// reachability and emission (BranchLiveness). Both the MethodDef form
    /// (within-CoreLib) and the MemberRef form (a public getter referenced
    /// cross-assembly) are intercepted on all three paths.
    ///
    /// GlobalizationMode: a dn2cpp binary runs with the invariant-globalization
    /// posture (the equivalent of building with InvariantGlobalization=true) —
    /// every `if (GlobalizationMode.Invariant)` guard in the BCL becomes
    /// constant-true, so the pure-managed invariant arms execute (ordinal
    /// casing, managed Punycode for IDN) and the ICU/NLS arms — whose leaves
    /// are libSystem.Globalization.Native P/Invokes the runtime does not
    /// provide — become dead (the getter's real body drags Settings..cctor ->
    /// LoadAppLocalIcu -> InitICUFunctions into the tree).
    ///
    /// RuntimeFeature: the dynamic-code capability probes are constant-false —
    /// the IL2CPP/NativeAOT posture: a native dn2cpp binary has no runtime
    /// codegen (their real bodies read an AppContext switch defaulting to
    /// true — wrong here). The RegexOptions.Compiled arm those flags guard
    /// degrades to the interpreter, exactly like NativeAOT; Regex.Compile
    /// itself folds to a null factory alongside them
    /// (MethodCompiler.TryEmitRegexCompileFold).
    ///
    /// RuntimeHelpers.TryEnsureSufficientExecutionStack: constant-true — the
    /// no-stack-probe posture (the native stack is the only stack; the void
    /// Ensure sibling is a no-op in the RuntimeHelpers intrinsic table, which
    /// also still serves this probe's MemberRef call sites — RuntimeHelpers is
    /// an intrinsic-mapped type, so its MemberRef form never reaches the
    /// const-fold call-site path). StackHelper.TryEnsureSufficientExecutionStack
    /// is the shared-source forwarder Regex's guards actually call
    /// (intra-assembly, so the fold must key on the wrapper too). The entries'
    /// value feeds the BranchLiveness verdict: the
    /// `if (!TryEnsure...) CallOnEmptyStack(...)` re-dispatch arm (Regex's
    /// deep-recursion escape hatch) is pruned, keeping StackHelper's
    /// Task.Run/ContinueWith plumbing out of the tree.
    ///
    /// Debugger.IsLogging: constant-false, NativeAOT's hard-coded answer — a
    /// transpiled binary has no VM to log into. BranchLiveness prunes
    /// DebugProvider.WriteToDebugger's then-arm (Debugger.Log, whose leaf is the
    /// bodyless QCall LogInternal) and leaves Interop.Sys.SysLog the live sink,
    /// where NativeAOT's Trace/Debug output goes on Unix.</summary>
    private static readonly Dictionary<(string Type, string Method), bool> s_constFoldedGetters = new()
    {
        [("System.Globalization.GlobalizationMode", "get_Invariant")] = true,
        [("System.Globalization.GlobalizationMode", "get_UseNls")] = false,
        [("System.Runtime.CompilerServices.RuntimeFeature", "get_IsDynamicCodeSupported")] = false,
        [("System.Runtime.CompilerServices.RuntimeFeature", "get_IsDynamicCodeCompiled")] = false,
        [("System.Runtime.CompilerServices.RuntimeHelpers", "TryEnsureSufficientExecutionStack")] = true,
        [("System.Threading.StackHelper", "TryEnsureSufficientExecutionStack")] = true,
        [("System.Diagnostics.Debugger", "IsLogging")] = false,
    };

    /// <summary>Method names appearing in <see cref="s_constFoldedGetters"/> —
    /// the cheap first-pass filter for per-call-token oracle lookups.</summary>
    private static readonly HashSet<string> s_constFoldedGetterNames =
        new(s_constFoldedGetters.Keys.Select(k => k.Method));

    /// <summary>The folded constant for a getter, or null when the method is not
    /// const-folded. See <see cref="s_constFoldedGetters"/>.</summary>
    public static bool? ConstFoldedGetter(string declType, string name) =>
        s_constFoldedGetters.TryGetValue((declType, name), out bool v) ? v : null;

    /// <summary>Name-only prefilter for <see cref="ConstFoldedGetter"/>, so a
    /// per-call-site probe skips declaring-type resolution for the overwhelming
    /// majority of calls.</summary>
    public static bool IsConstFoldedGetterName(string name) => s_constFoldedGetterNames.Contains(name);

    /// <summary>String-returning calls folded to a compile-time constant. Deliberately a
    /// SEPARATE table from <see cref="s_constFoldedGetters"/>: that table's value feeds
    /// BranchLiveness's `call getter; brtrue/brfalse` verdict, and a string constant can
    /// prune no branch. Both entries are private static, so the call-site pair is the only
    /// asker pair that can see them.
    ///
    /// <para>TimeZoneInfo.GetUtcStandardDisplayName: the win-x64 body has three exits and
    /// all three return this literal, so the fold IS what real .NET answers. The POSIX body
    /// of the same name reaches the same constant on its own (its guard is
    /// GlobalizationMode.Invariant, which the fold table carries, and it has no exception
    /// region), so it needs no intercept; the Windows one does, because its guard is a
    /// TZ-specific AppContext switch and its `using (RegistryKey)` try/finally puts every
    /// dead offset inside an exception region, where BranchLiveness bails by design.</para>
    ///
    /// <para>TimeZoneInfo.GetLocalizedNameByMuiNativeResource: its first guard returns
    /// String.Empty under `GlobalizationMode.Invariant &amp;&amp; PredefinedCulturesOnly` —
    /// the posture a dn2cpp binary runs with — and every caller consumes the result through
    /// String.IsNullOrEmpty. It needs its OWN row rather than riding on the one above
    /// because it has a second caller under the local-zone lookup, and it must NOT fold to
    /// the UTC literal: that would answer the LOCAL zone's display-name query with
    /// "Coordinated Universal Time". Its callee GetLocalizedNameByNativeResource is orphaned
    /// by this cut and needs no row of its own.</para></summary>
    private static readonly Dictionary<(string Type, string Method), string> s_constFoldedStringCalls = new()
    {
        [("System.TimeZoneInfo", "GetUtcStandardDisplayName")] = "Coordinated Universal Time",
        [("System.TimeZoneInfo", "GetLocalizedNameByMuiNativeResource")] = "",
    };

    /// <summary>Method names appearing in <see cref="s_constFoldedStringCalls"/> —
    /// the cheap first-pass filter for per-call-token oracle lookups.</summary>
    private static readonly HashSet<string> s_constFoldedStringCallNames =
        new(s_constFoldedStringCalls.Keys.Select(k => k.Method));

    /// <summary>The folded constant for a string-returning call, or null when the method
    /// is not const-folded. See <see cref="s_constFoldedStringCalls"/>.</summary>
    public static string? ConstFoldedStringCall(string declType, string name) =>
        s_constFoldedStringCalls.TryGetValue((declType, name), out string? v) ? v : null;

    /// <summary>Name-only prefilter for <see cref="ConstFoldedStringCall"/>, so a
    /// per-call-site probe skips declaring-type resolution for the overwhelming
    /// majority of calls.</summary>
    public static bool IsConstFoldedStringCallName(string name) => s_constFoldedStringCallNames.Contains(name);

    /// <summary>Whether an (arity-stripped) type name is an async/await type
    /// modeled by a runtime struct.</summary>
    public static bool IsTaskFamily(string strippedFullName) => s_taskFamilyCpp.ContainsKey(strippedFullName);

    /// <summary>The C++ runtime type an async/await type lowers to, or null when
    /// it is not a Task-family type.</summary>
    public static string? TaskFamilyCppType(string strippedFullName) =>
        s_taskFamilyCpp.TryGetValue(strippedFullName, out var v) ? v : null;

    /// <summary>Generic BCL types (other than the Task family) modeled inline
    /// rather than transpiled, keyed on the arity-stripped open-definition name.
    /// The value is the C++ type the managed type lowers to.</summary>
    private static readonly Dictionary<string, string> s_intrinsicGenericCpp = new()
    {
        // EqualityComparer<T>: the real Default/Equals/GetHashCode route through
        // reflection + per-type comparer subclasses (GenericEqualityComparer,
        // ObjectEqualityComparer, …) we never allocate. Model it as an opaque
        // reference sentinel; get_Default yields nullptr and Equals/GetHashCode
        // lower to type-specialized ops at the call site.
        ["System.Collections.Generic.EqualityComparer"] = "Dn2CppObject*",
        // DefaultInterpolatedStringHandler: the compiler lowers $"..." to this
        // value type. Its real body reaches Buffer.Memmove (InternalCall), so
        // we model it as a runtime growable UTF-16 buffer.
        ["System.Runtime.CompilerServices.DefaultInterpolatedStringHandler"] = "Dn2CppISB",
        // Task.Yield()'s awaitable and its nested awaiter both lower to the same
        // stateless value type. "YieldAwaiter" is the bare (namespace-less) name
        // of the nested awaiter, matching how nested TypeRefs decode here.
        ["System.Runtime.CompilerServices.YieldAwaitable"] = "Dn2CppYieldAwaiter",
        ["YieldAwaiter"] = "Dn2CppYieldAwaiter",
        // ConfigureAwait's awaitable/awaiter both wrap a task and behave like a
        // TaskAwaiter (ConfigureAwait is a no-op single-threaded), so reuse the Task
        // awaiter struct — the await-suspend path then matches the Task case.
        ["System.Runtime.CompilerServices.ConfiguredTaskAwaitable"] = "Dn2CppTaskAwaiter",
        ["ConfiguredTaskAwaiter"] = "Dn2CppTaskAwaiter",
        // valueTask.ConfigureAwait(bool)'s awaitable/awaiter (non-generic and <T>,
        // matched arity-stripped): same task-wrapping model as the Task pair above.
        ["System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable"] = "Dn2CppTaskAwaiter",
        ["ConfiguredValueTaskAwaiter"] = "Dn2CppTaskAwaiter",
        // StringBuilder lowers to a hand-written growable UTF-16 buffer.
        ["System.Text.StringBuilder"] = "Dn2CppStringBuilder*",
        // The degraded stack-trace model (see s_intrinsicTypes): real reference
        // objects carrying a (possibly empty) frame array and, for a frame, the
        // file/line a caller supplied. Their type-infos wire the tostring slot, so
        // Object.ToString on one held as `object` reports the honest text too.
        ["System.Diagnostics.StackTrace"] = "Dn2CppStackTrace*",
        ["System.Diagnostics.StackFrame"] = "Dn2CppStackFrame*",
        // ResourceManager lowers to (base name, assembly handle); the lookup reads the
        // assembly's carried .resources blob. See s_intrinsicTypes for why the real
        // body cannot be transpiled at all.
        ["System.Resources.ResourceManager"] = "Dn2CppResourceManager*",
        // CultureInfo / NumberFormatInfo / IFormatProvider all lower to the runtime
        // number-format struct pointer; the format helpers' culture variants take it
        // directly, so a provider local/field/parameter carries the same type.
        ["System.Globalization.CultureInfo"] = "const Dn2CppNumberFormatInfo*",
        ["System.Globalization.NumberFormatInfo"] = "const Dn2CppNumberFormatInfo*",
        ["System.IFormatProvider"] = "const Dn2CppNumberFormatInfo*",
        // TextInfo carries the same invariant culture pointer (only ListSeparator read).
        ["System.Globalization.TextInfo"] = "const Dn2CppNumberFormatInfo*",
        // System.Decimal: an intrinsic value type backed by the runtime 96-bit
        // Dn2CppDecimal (no trailing '*' -> IsValueType, passed by value).
        ["System.Decimal"] = "Dn2CppDecimal",
        // System.TimeSpan / System.DateTime: intrinsic value types backed by the
        // runtime structs (no trailing '*' -> IsValueType, passed by value).
        ["System.TimeSpan"] = "Dn2CppTimeSpan",
        ["System.DateTime"] = "Dn2CppDateTime",
        // System.DateTimeOffset: intrinsic value type backed by Dn2CppDateTimeOffset.
        ["System.DateTimeOffset"] = "Dn2CppDateTimeOffset",
        // System.DateOnly / System.TimeOnly: intrinsic value types backed by
        // Dn2CppDateOnly / Dn2CppTimeOnly.
        ["System.DateOnly"] = "Dn2CppDateOnly",
        ["System.TimeOnly"] = "Dn2CppTimeOnly",
        // System.Runtime.InteropServices.GCHandle: intrinsic value type backed by the
        // runtime Dn2CppGCHandle { cell } — one pointer to a shared handle-slot cell
        // (no trailing '*' -> passed by value; see dn2cpp.h). Non-moving heap =>
        // Normal/Pinned are identity-pinned; Weak kinds delegate to the weak table.
        ["System.Runtime.InteropServices.GCHandle"] = "Dn2CppGCHandle",
        // System.Runtime.DependentHandle: intrinsic value type backed
        // by the runtime Dn2CppDependentHandle { cell } — one pointer to a
        // weak-target/strong-dependent cell (no trailing '*' -> passed by value).
        ["System.Runtime.DependentHandle"] = "Dn2CppDependentHandle",
        // System.IO.MemoryMappedFiles file-backed map subset: the three BCL reference
        // types lower to small by-value intrinsic structs (no trailing '*' -> forced to
        // IsValueType in BuildClassInfo, mirroring the non-moving GCHandle handle model).
        ["System.IO.MemoryMappedFiles.MemoryMappedFile"] = "Dn2CppMappedFile",
        ["System.IO.MemoryMappedFiles.MemoryMappedViewAccessor"] = "Dn2CppMappedView",
        ["Microsoft.Win32.SafeHandles.SafeMemoryMappedViewHandle"] = "Dn2CppMappedSafeHandle",
        // .NET 9+ System.Threading.Lock — a minimal allocated reference object on the
        // single-threaded model (its real body is the lock-free slow-path machinery
        // routing to EventSource -> Calli).
        ["System.Threading.Lock"] = "Dn2CppObject*",
        // System.Threading.ThreadLocal<T> — per-instance, per-thread storage backed by
        // the runtime Dn2CppThreadLocal. The value is stored boxed in a uniform slot and
        // the optional Func<T> factory is invoked lazily in the runtime, dispatched by
        // result kind; its real body routes through the LinkedSlot/global-counter slow
        // path we don't model, so it is intrinsic (method bodies cut at reachability).
        ["System.Threading.ThreadLocal"] = "Dn2CppThreadLocal*",
        // System.Collections.Concurrent.BlockingCollection<T> — a producer/consumer
        // blocking queue backed by the runtime Dn2CppBlockingCollection (a real mutex +
        // condition-variable bounded/unbounded FIFO). Elements are stored boxed in a
        // uniform slot, boxed/unboxed by T's kind at each Add/Take call site; its real
        // body is the SemaphoreSlim/IProducerConsumerCollection machinery we don't model,
        // so it is intrinsic (method bodies cut at reachability, ctor intercepted at newobj).
        ["System.Collections.Concurrent.BlockingCollection"] = "Dn2CppBlockingCollection*",
        // Portable SIMD vector GENERIC structs (arity 1). Software-emulated as a
        // fixed-width POD byte buffer (no trailing '*' -> by-value value type); element
        // type is recovered per call from the closed generic context and supplied as a
        // C++ template arg to the dn2cpp_vec_* helpers. The same-named arity-0 static
        // helper classes are in s_intrinsicTypes above. System.Numerics.Vector<T> is
        // fixed at 128-bit (Dn2CppVectorT == Dn2CppVec<16>).
        ["System.Runtime.Intrinsics.Vector64"] = "Dn2CppVector64",
        ["System.Runtime.Intrinsics.Vector128"] = "Dn2CppVector128",
        ["System.Runtime.Intrinsics.Vector256"] = "Dn2CppVector256",
        ["System.Runtime.Intrinsics.Vector512"] = "Dn2CppVector512",
        ["System.Numerics.Vector"] = "Dn2CppVectorT",
        // System.Buffers.SearchValues<T> — a runtime byte/char membership set (built by
        // SearchValues.Create, queried by MemoryExtensions.IndexOfAny(span, sv)). The
        // arity-0 static factory class is in s_intrinsicTypes above (same split as the
        // SIMD vectors); the concrete BCL subclasses are never reached (Create is cut).
        ["System.Buffers.SearchValues"] = "Dn2CppSearchValues*",
    };

    /// <summary>The C++ type an intrinsic generic type (Task family or the
    /// <see cref="s_intrinsicGenericCpp"/> set) lowers to, or null.</summary>
    public static string? IntrinsicGenericCppType(string strippedFullName) =>
        s_taskFamilyCpp.TryGetValue(strippedFullName, out var v) ? v
        : s_intrinsicGenericCpp.TryGetValue(strippedFullName, out var w) ? w
        : null;

    /// <summary>An entry of <see cref="s_specialTypeCpp"/> whose value the loaded-Class
    /// path already owns via <see cref="IntrinsicGenericCppType"/> (every TypeDef is
    /// stamped with it as IntrinsicCppName, which <c>CppTypes.Of</c>'s Class arm probes
    /// BEFORE the shared table): mirror that same string rather than writing it down a
    /// second time, so the two sources cannot drift. Throws at type initialization if
    /// the name ever leaves the intrinsic tables — update both sides together.</summary>
    private static string SpecialTypeMirror(string fullName) =>
        IntrinsicGenericCppType(fullName) ?? throw new InvalidOperationException(
            $"{fullName} left the intrinsic C++ type tables; fix its s_specialTypeCpp entry too");

    /// <summary>The special-type C++ mappings shared by BOTH <c>CppTypes.Of</c> arms —
    /// the Class arm (a loaded TypeDef/TypeSpec; probes this right after its
    /// IntrinsicCppName probe) and the External arm (a TypeRef whose defining IL the
    /// transpile did not load; probes this first). ONE table, so a name both arms
    /// special-case cannot drift between the two paths.
    ///
    /// Only names the two arms already answer IDENTICALLY may live here: either both
    /// arms carried the literal entry, or (StackTrace/StackFrame/ParallelLoopResult)
    /// the Class arm answers from IntrinsicCppName first and the entry mirrors that
    /// same source (<see cref="SpecialTypeMirror"/>). The deliberately ARM-SCOPED
    /// names stay at their arm in <c>CppTypes.Of</c>, each with a comment naming this
    /// table: System.Enum (Class-only — the External arm has never mapped it and
    /// throws, and a table entry would silently convert that throw into an answer);
    /// System.Object / System.Type / ParallelOptions / ParallelLoopState and the
    /// <see cref="RuntimeExceptionTypeInfo"/> / <see cref="IsDynamicCodegenType"/>
    /// predicate arms (External-only — a loaded Class of those names has no
    /// IntrinsicCppName, so the Class arm's fallback gives the transpiled
    /// <c>t_*</c> shape instead, and a table entry would change it).</summary>
    private static readonly Dictionary<string, string> s_specialTypeCpp = new()
    {
        // The runtime string, both as CoreLib's own String TypeDef reached as a
        // Class-kind TypeDesc (the `this` of a transpiled String interface impl)
        // and as an unresolved TypeRef — never the opaque t_System_String shell.
        ["System.String"] = "Dn2CppString*",
        // System.Exception is intrinsic and opaque; a caught/stored Exception is
        // just a managed object reference, so use the base pointer rather than
        // depending on the per-type opaque struct being emitted.
        ["System.Exception"] = "Dn2CppObject*",
        // EventSource is an opaque intrinsic base (like Exception); a provider
        // held as its base is a managed object reference.
        ["System.Diagnostics.Tracing.EventSource"] = "Dn2CppObject*",
        // Reflected member handles (Dn2CppFieldRef / Dn2CppMethodRef /
        // Dn2CppParamRef / Dn2CppPropRef) are managed object references with
        // intrinsic-dispatched members, so the static type is just the base
        // pointer, like Exception.
        ["System.Reflection.FieldInfo"] = "Dn2CppObject*",
        ["System.Reflection.MethodInfo"] = "Dn2CppObject*",
        ["System.Reflection.MethodBase"] = "Dn2CppObject*",
        ["System.Reflection.ParameterInfo"] = "Dn2CppObject*",
        ["System.Reflection.ConstructorInfo"] = "Dn2CppObject*",
        ["System.Reflection.PropertyInfo"] = "Dn2CppObject*",
        // System.Reflection.Assembly is modeled as its defining assembly's
        // simple-name pointer (Type.Assembly identity / the assembly-registry
        // key). Module is the same handle (single-module assemblies).
        // AssemblyName is a plain transpiled class (no entry — arm fallbacks).
        ["System.Reflection.Assembly"] = "const char*",
        ["System.Reflection.Module"] = "const char*",
        // RuntimeTypeHandle is the type-info pointer everywhere (ldtoken,
        // Type.TypeHandle, GetTypeFromHandle) — the loaded CoreLib's struct
        // shape must not leak into signatures. Its External STACK kind stays a
        // CppTypes.KindOf special: the C++ type string is what the arms agree
        // on; the stack kind is arm-divergent (see there).
        ["System.RuntimeTypeHandle"] = "const Dn2CppTypeInfo*",
        // The degraded stack-trace objects and the Parallel.For/ForEach result
        // struct referenced without their defining IL (a signature naming them
        // in an assembly compiled against a CoreLib/facade the transpile did
        // not load): the same runtime types the loaded-Class path gives via
        // IntrinsicCppName — mirrored from that source, not restated.
        ["System.Diagnostics.StackTrace"] = SpecialTypeMirror("System.Diagnostics.StackTrace"),
        ["System.Diagnostics.StackFrame"] = SpecialTypeMirror("System.Diagnostics.StackFrame"),
        ["System.Threading.Tasks.ParallelLoopResult"] =
            SpecialTypeMirror("System.Threading.Tasks.ParallelLoopResult"),
    };

    /// <summary>The shared special-type C++ mapping for a full type name, or null
    /// when the name has none (see <see cref="s_specialTypeCpp"/>). Both
    /// <c>CppTypes.Of</c> arms probe this — the single source of truth for the
    /// special-type names they agree on.</summary>
    public static string? SpecialTypeCppName(string? fullName) =>
        fullName is not null && s_specialTypeCpp.TryGetValue(fullName, out var v) ? v : null;

    /// <summary>The software-vector C++ types (the portable SIMD vectors).</summary>
    private static bool IsSoftwareVectorCpp(string? cpp) =>
        cpp is "Dn2CppVector64" or "Dn2CppVector128" or "Dn2CppVector256"
            or "Dn2CppVector512" or "Dn2CppVectorT";

    /// <summary>Like <see cref="IntrinsicGenericCppType(string)"/>, but a portable SIMD
    /// vector type is software-emulable ONLY for a primitive element: the dn2cpp_vec_*
    /// helper templates need a C++ numeric element type. A non-primitive element (e.g.
    /// <c>Vector&lt;Int128&gt;</c>, which the BCL only ever touches on its element-wise
    /// software fallback) keeps its real BCL IL instead — it is NOT made intrinsic,
    /// matching the behavior before the vector intrinsics existed. Used at generic
    /// instantiation, where the closed element type arg is known.</summary>
    public static string? IntrinsicGenericCppType(string strippedFullName, TypeDesc[] typeArgs)
    {
        var cpp = IntrinsicGenericCppType(strippedFullName);
        if (IsSoftwareVectorCpp(cpp) && (typeArgs.Length == 0 || typeArgs[0].Kind != TypeKind.Primitive))
            return null;
        return cpp;
    }

    /// <summary>Intrinsic *nested* value types, keyed on (enclosing type full name,
    /// simple nested name). A nested type's TypeRef/TypeDef decodes to its bare simple
    /// name (e.g. "Scope"), which is not unique across declaring types — so an intrinsic
    /// check keyed on the bare name would collide with any other nested "Scope". These
    /// are matched only when the *enclosing* type also matches. The value is the C++
    /// type the nested type lowers to.</summary>
    private static readonly Dictionary<(string Enclosing, string Name), string> s_intrinsicNestedCpp = new()
    {
        // Lock.Scope: the ref struct EnterScope() returns and the `lock` body's finally
        // disposes. Both calls are intercepted, so it carries no state — a 0-byte struct.
        [("System.Threading.Lock", "Scope")] = "Dn2CppLockScope",
        // StringBuilder.AppendInterpolatedStringHandler: the handler the C# compiler
        // lowers `sb.Append($"...")` through (distinct from the top-level
        // DefaultInterpolatedStringHandler). Its real generic AppendFormatted<T> reaches
        // Enum.TryFormatUnconstrained -> the RuntimeType/EnumInfo/Number-format cascade,
        // so we model the handler as the underlying StringBuilder pointer itself: ctor
        // captures the StringBuilder, AppendLiteral/AppendFormatted append straight to it,
        // and StringBuilder.Append(ref handler) is then a no-op returning the builder.
        [("System.Text.StringBuilder", "AppendInterpolatedStringHandler")] = "Dn2CppStringBuilder*",
        // StringBuilder.ChunkEnumerator (sb.GetChunks() / foreach chunk): the
        // runtime StringBuilder is one contiguous buffer, so the enumerator
        // yields exactly one chunk — a snapshot string wrapped in a real
        // ReadOnlyMemory<char> (whose transpiled Span getter takes the string
        // arm). Regex's RegexCharClass.ToStringClass is the reaching consumer.
        [("System.Text.StringBuilder", "ChunkEnumerator")] = "Dn2CppSbChunkEnum",
    };

    /// <summary>Whether (enclosingFull, simpleName) is an intrinsic nested type.</summary>
    public static bool IsIntrinsicNested(string enclosingFull, string simpleName) =>
        s_intrinsicNestedCpp.ContainsKey((enclosingFull, simpleName));

    /// <summary>The C++ type an intrinsic nested type lowers to, or null.</summary>
    public static string? IntrinsicNestedCppType(string enclosingFull, string simpleName) =>
        s_intrinsicNestedCpp.TryGetValue((enclosingFull, simpleName), out var v) ? v : null;

    /// <summary>The enclosing-qualified dispatch key the call emitter uses for an
    /// intrinsic nested member (e.g. "System.Threading.Lock+Scope"), so EmitIntrinsic
    /// can switch on it without colliding with a bare same-named nested type.</summary>
    public static string NestedDispatchName(string enclosingFull, string simpleName) =>
        enclosingFull + "+" + simpleName;
}
