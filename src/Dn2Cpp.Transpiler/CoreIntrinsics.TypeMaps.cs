using System.Reflection.Metadata;

namespace Dn2Cpp;

// Independent initialization lets intrinsic field initializers query these maps.
// The same data also bounds the original-definition retention graph.
internal static class CoreIntrinsicTypeMaps
{
    public static PrimitiveTypeCode? WellKnownPrimitiveCode(string? fullName) => fullName switch
    {
        "System.Boolean" => PrimitiveTypeCode.Boolean,
        "System.Char" => PrimitiveTypeCode.Char,
        "System.SByte" => PrimitiveTypeCode.SByte,
        "System.Byte" => PrimitiveTypeCode.Byte,
        "System.Int16" => PrimitiveTypeCode.Int16,
        "System.UInt16" => PrimitiveTypeCode.UInt16,
        "System.Int32" => PrimitiveTypeCode.Int32,
        "System.UInt32" => PrimitiveTypeCode.UInt32,
        "System.Int64" => PrimitiveTypeCode.Int64,
        "System.UInt64" => PrimitiveTypeCode.UInt64,
        "System.IntPtr" => PrimitiveTypeCode.IntPtr,
        "System.UIntPtr" => PrimitiveTypeCode.UIntPtr,
        "System.Single" => PrimitiveTypeCode.Single,
        "System.Double" => PrimitiveTypeCode.Double,
        // String/Object have runtime metadata (dn2cpp_string_type/object_type) and
        // dedicated C++ reps; normalizing corlib's own TypeDefs to the primitive
        // keeps typeof(string)/isinst string out of the per-class emit path.
        "System.String" => PrimitiveTypeCode.String,
        "System.Object" => PrimitiveTypeCode.Object,
        _ => null,
    };

    public static bool IsInlineLoweredPrimitiveMember(string? fullTypeName, string name) =>
        (fullTypeName is "System.Byte" or "System.SByte" or "System.Int16" or "System.UInt16"
            && name is "ToString" or "Parse" or "TryParse" or "TryFormat")
        || (name == "CompareTo" && IsScalarPrimitive(fullTypeName))
        || (name == "DivRem" && fullTypeName is "System.Byte" or "System.SByte"
            or "System.Int16" or "System.UInt16" or "System.Int32" or "System.UInt32"
            or "System.Int64" or "System.UInt64" or "System.IntPtr" or "System.UIntPtr");

    private static bool IsScalarPrimitive(string? fullTypeName) =>
        fullTypeName is not null
        && WellKnownPrimitiveCode(fullTypeName) is { } code
        && code is not (PrimitiveTypeCode.String or PrimitiveTypeCode.Object);

    public static string? RuntimeInterfaceDispatch(string owner)
    {
        if (owner == "System.RuntimeType" || owner == "System.Reflection.TypeInfo")
            return "System.Type";
        if (owner == "System.Reflection.RuntimeAssembly")
            return "System.Reflection.Assembly";
        if (owner == "System.Reflection.RuntimeModule")
            return "System.Reflection.Module";
        return IsIntrinsicType(owner) ? owner : null;
    }

    public static bool IsDelegateBaseType(string? fullName) =>
        fullName is "System.Delegate" or "System.MulticastDelegate";

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
        // not model, so they lower to runtime intrinsics. Members whose real bodies are
        // plain managed code are still intercepted, and the interception calls that body
        // (IsRealBodyGeneric).
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
        // RuntimeTypeHandle is the type-info pointer itself. Its managed CoreLib field
        // layout and member bodies must not leak into the emitted representation.
        "System.RuntimeTypeHandle",
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
        "System.Reflection.EventInfo",
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
        // intercept the members below. IFormatProvider retains a managed header
        // so each implementation preserves its type and object identity.
        "System.Globalization.CultureInfo",
        "System.Globalization.NumberFormatInfo",
        "System.Globalization.DateTimeFormatInfo",
        "System.IFormatProvider",
        // TextInfo rides the culture pointer for ListSeparator and its culture-bearing
        // ToString display. Its ICU-backed casing methods stay bounded carve-outs.
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
        // TaskCompletionSource(<T>) — a wrapper around the Dn2CppTask* it
        // completes; newobj allocates both objects,
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
        // Task.get_Factory is an opaque nullptr sentinel; TaskScheduler.Default/Current
        // share a runtime singleton so explicit null schedulers remain distinguishable.
        // factory.StartNew dispatches to the same worker pool as Task.Run (the
        // TaskCreationOptions/scheduler selection are hints dn2cpp's fixed pool does not
        // model). Keeps the real TPL scheduler machinery out of the tree —
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
        // is mapped. Generic Action<TState> and IThreadPoolWorkItem forms share the same
        // pool; SetMinThreads/GetAvailableThreads and the tuning surface stay loud.
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
        // The handle OF one of the above. Runtime-created events wrap their native event
        // object, while WaitHandle.set_SafeWaitHandle can attach a real OS handle (the
        // ProcessWaitHandle path). Transpiling the real type does not stay small:
        // ReleaseHandle and the base SafeHandle refcount state machine are replaced by the
        // runtime wrapper that distinguishes those two resources.
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
        // thread. GC-allocated (it holds the managed callback/state). DisposeAsync stops
        // synchronously; Dispose(WaitHandle) remains a loud carve-out.
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
        // UnmanagedMemoryAccessor + OS-mapping P/Invoke cascade; members lower at the call
        // site (TryEmitMemoryMappedFileIntrinsic, plus the generic accessor forms in
        // TranslateGenericIntrinsic). The view inherits its typed accessors from
        // UnmanagedMemoryAccessor, so that base is intrinsic too. SafeMemoryMappedViewHandle
        // and its SafeBuffer/SafeHandle bases retain their real managed layout and refcounts.
        "System.IO.MemoryMappedFiles.MemoryMappedFile",
        "System.IO.MemoryMappedFiles.MemoryMappedViewAccessor",
        "System.IO.UnmanagedMemoryAccessor",
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
        ["System.Threading.Tasks.TaskCompletionSource"] = "Dn2CppTaskCompletionSource*",
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
        // Task.Factory is an opaque nullptr sentinel; TaskScheduler.Default/Current
        // share a runtime object. StartNew is intercepted at the call site, so a
        // Dn2CppObject* handle suffices for both types.
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
        // Both are opaque runtime references at generated call sites, but a
        // SafeWaitHandle is a distinct wrapper carrying either a runtime-event alias or
        // an OS handle. The runtime helpers resolve that wrapper before operating.
        ["Microsoft.Win32.SafeHandles.SafeWaitHandle"] = "Dn2CppObject*",
        ["System.Threading.CountdownEvent"] = "Dn2CppObject*",
        ["System.Threading.Barrier"] = "Dn2CppObject*",
        ["System.Threading.ReaderWriterLockSlim"] = "Dn2CppObject*",
        ["System.Threading.Timer"] = "Dn2CppObject*",
        // ParallelLoopResult — the value struct Parallel.For/ForEach return (no trailing
        // '*' -> IsValueType, passed by value), backed by the runtime Dn2CppParallelLoopResult.
        ["System.Threading.Tasks.ParallelLoopResult"] = "Dn2CppParallelLoopResult",
    };

    public static bool IsIntrinsicType(string fullTypeName) => s_intrinsicTypes.Contains(fullTypeName);

    /// <summary>The generic members of intrinsic-mapped types whose real CoreLib bodies are
    /// plain managed code. System.Array's: ThrowHelper argument checks, element reads, a
    /// delegate invoke, a List&lt;T&gt; or ReadOnlyCollection&lt;T&gt;, and calls to each
    /// other. System.Delegate's EnumerateInvocationList: the InvocationListEnumerator
    /// construction, whose MoveNext reaches the chain through
    /// <c>MulticastDelegate.TryGetAt</c> (<see cref="LoweredRuntimePrimitive"/>). Their call
    /// sites stay intercepted with the rest of the intrinsic type, but the lowering and an
    /// address-taken use both name the real transpiled body, so .NET's argument order and
    /// messages hold by construction.</summary>
    public static bool IsRealBodyGeneric(string declType, string name) => declType switch
    {
        "System.Array" => name is "Find" or "FindLast" or "FindAll" or "FindIndex" or "FindLastIndex"
            or "Exists" or "TrueForAll" or "ConvertAll" or "ForEach" or "AsReadOnly",
        "System.Delegate" => name == "EnumerateInvocationList",
        _ => false,
    };

    public static bool IsArrayRealBodyMember(string declType, string name, bool hasInt64Parameter) =>
        declType == "System.Array"
        && (name is "get_IsFixedSize" or "get_IsReadOnly" or "get_IsSynchronized" or "get_SyncRoot" or "GetLongLength"
            || hasInt64Parameter && name is ("Copy" or "CopyTo" or "GetValue" or "SetValue" or "CreateInstance"));

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
        // ObjectEqualityComparer, …) we never transpile. Model it as an opaque
        // reference; get_Default yields a non-null per-T synthetic singleton and
        // Equals/GetHashCode lower to type-specialized ops at the call site.
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
        // Typed culture data uses the raw formatter payload. An interface keeps a
        // managed header so providers with different CLR identities stay distinguishable.
        ["System.Globalization.CultureInfo"] = "const Dn2CppNumberFormatInfo*",
        ["System.Globalization.NumberFormatInfo"] = "const Dn2CppNumberFormatInfo*",
        ["System.IFormatProvider"] = "Dn2CppObject*",
        // TextInfo carries the same culture pointer for ListSeparator and ToString.
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
        // Files and views keep reference identity across object/interface conversions.
        ["System.IO.MemoryMappedFiles.MemoryMappedFile"] = "Dn2CppMappedFile*",
        ["System.IO.MemoryMappedFiles.MemoryMappedViewAccessor"] = "Dn2CppMappedViewObject*",
        ["System.IO.UnmanagedMemoryAccessor"] = "Dn2CppMappedViewObject*",
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

    /// <summary>Intrinsic *nested* value types, keyed on (enclosing type full name,
    /// simple nested name). A nested type's TypeRef/TypeDef decodes to its bare simple
    /// name (e.g. "Scope"), which is not unique across declaring types — so an intrinsic
    /// check keyed on the bare name would collide with any other nested "Scope". These
    /// are matched only when the *enclosing* type also matches. The value is the C++
    /// type the nested type lowers to.</summary>
    private static readonly Dictionary<(string Enclosing, string Name), string> s_intrinsicNestedCpp = new()
    {
        // TimeProvider.System.CreateTimer returns this private CoreLib adapter. Its real
        // ctor enters TimerQueue's runtime-owned thread and ThreadPool machinery; use the
        // same per-timer thread object as the public System.Threading.Timer intrinsic.
        [("System.TimeProvider", "SystemTimeProviderTimer")] = "Dn2CppObject*",
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

    public static bool IsEnumElementTypeMethod(string? declType, string name,
        bool isStatic, int parameterCount, string? returnType, string? parameterType) =>
        declType == "System.Enum" && name == "InternalGetCorElementType"
        && returnType == "System.Reflection.CorElementType"
        && (!isStatic && parameterCount == 0
            || isStatic && parameterCount == 1 && parameterType == "System.RuntimeType");

    public static bool IsEnumUnderlyingTypeMethod(string? declType, string name,
        bool isStatic, int parameterCount, string? returnType, string? parameterType) =>
        declType == "System.Enum" && name == "InternalGetUnderlyingType"
        && isStatic && parameterCount == 1 && returnType == "System.RuntimeType"
        && parameterType == "System.RuntimeType";

    public static bool IsEnumScalarGetNameMethod(string? declType, string name,
        bool isStatic, int parameterCount, string? returnType, string? firstType, string? secondType) =>
        declType == "System.Enum" && name == "GetName" && isStatic && parameterCount == 2
        && returnType == "System.String" && firstType == "System.RuntimeType" && secondType == "System.UInt64";

    public static bool IsEnumTryFormatMethod(string? declType, string name) =>
        declType == "System.Enum" && name == "System.ISpanFormattable.TryFormat";

    // Managed delegations and interface bodies exist within these runtime-mapped
    // families. A retention analysis may traverse a superset of those bodies.
    public static bool IntrinsicManagedBodyFamily(string fullName) => fullName is
        "System.String" or "System.Decimal" or "System.Collections.HashHelpers";

    /// <summary>The software-vector C++ types (the portable SIMD vectors).</summary>
    private static bool IsSoftwareVectorCpp(string? cpp) =>
        cpp is "Dn2CppVector64" or "Dn2CppVector128" or "Dn2CppVector256"
            or "Dn2CppVector512" or "Dn2CppVectorT";

    public static string? IntrinsicGenericCppType(string strippedFullName, bool primitiveElement)
    {
        var cpp = IntrinsicGenericCppType(strippedFullName);
        if (IsSoftwareVectorCpp(cpp) && !primitiveElement)
            return null;
        return cpp;
    }
}
