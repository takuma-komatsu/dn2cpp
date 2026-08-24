using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool TryEmitThreadingIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        switch (declType, name)
        {
            // lock(object) / Monitor — real per-object recursive lock (the multithread
            // model). The lock(obj){…} lowering is Monitor.Enter(obj, ref taken); try {…}
            // finally { if (taken) Monitor.Exit(obj); }; Enter blocks until acquired then
            // sets lockTaken=true. Wait/Pulse/PulseAll add condition-variable signaling
            // (release-block-reacquire) on the same per-object lock.
            case ("System.Threading.Monitor", "Enter"):
            {
                if (sig.ParameterTypes is [.., { Kind: TypeKind.ByRef }])
                {
                    var taken = Pop(); // ref bool lockTaken
                    var obj = Pop();   // obj
                    Emit($"dn2cpp_monitor_enter((Dn2CppObject*)({obj.Expr}));");
                    // Low-byte write: the bool local is zero-initialised, so this yields
                    // the value 1 whether its storage is 1- or 4-byte (little-endian).
                    Emit($"*(uint8_t*)({taken.Expr}) = 1;");
                }
                else
                {
                    var obj = Pop(); // obj
                    Emit($"dn2cpp_monitor_enter((Dn2CppObject*)({obj.Expr}));");
                }
                return true;
            }
            case ("System.Threading.Monitor", "Exit"):
            {
                var obj = Pop(); // obj
                Emit($"dn2cpp_monitor_exit((Dn2CppObject*)({obj.Expr}));");
                return true;
            }
            case ("System.Threading.Monitor", "TryEnter"):
            {
                // TryEnter(obj [, timeout] [, ref taken]). A timeout (int ms or TimeSpan) is
                // a real timed acquire (try_enter_timeout); the bare TryEnter and the
                // zero-ms form reduce to a non-blocking attempt (ms == 0 is one immediate
                // check). The ref-bool forms write the byte result; the others push a bool.
                var ps = sig.ParameterTypes;
                bool hasRef = ps[^1].Kind == TypeKind.ByRef;
                bool timed = ps.Length > 1 && IsTimeoutParam(ps[1]);
                StackEntry? takenRef = hasRef ? Pop() : null; // ref bool result (last arg)
                string acquired;
                if (timed)
                {
                    var to = Pop();                  // timeout (int ms or TimeSpan)
                    var obj = Pop();                 // obj (first arg)
                    acquired = $"dn2cpp_monitor_try_enter_timeout((Dn2CppObject*)({obj.Expr}), {TimeoutMs(to, ps[1])})";
                }
                else
                {
                    var obj = Pop();                 // obj (first arg)
                    acquired = $"dn2cpp_monitor_try_enter((Dn2CppObject*)({obj.Expr}))";
                }
                if (hasRef)
                    Emit($"*(uint8_t*)({takenRef!.Expr}) = (uint8_t)({acquired});");
                else
                    Push(StackKind.I4, "int32_t", acquired);
                return true;
            }
            case ("System.Threading.Monitor", "Wait"):
            {
                // Wait(obj [, timeout] [, exitContext]) — all overloads return bool. A
                // timeout (int ms or TimeSpan) is a real timed park (wait_timeout) that
                // returns false if no Pulse arrives in time; without a timeout the wait
                // blocks until pulsed and returns true. A trailing exitContext bool is
                // ignored (always-false context flow).
                var ps = sig.ParameterTypes;
                bool timed = ps.Length > 1 && IsTimeoutParam(ps[1]);
                if (timed)
                {
                    for (int i = ps.Length - 1; i >= 2; i--) // drop a trailing exitContext bool
                        Pop();
                    var to = Pop();                  // timeout (int ms or TimeSpan)
                    var obj = Pop();                 // obj (first arg)
                    Push(StackKind.I4, "int32_t",
                        $"dn2cpp_monitor_wait_timeout((Dn2CppObject*)({obj.Expr}), {TimeoutMs(to, ps[1])})");
                }
                else
                {
                    var obj = Pop();                 // obj (first arg)
                    Push(StackKind.I4, "int32_t", $"dn2cpp_monitor_wait((Dn2CppObject*)({obj.Expr}))");
                }
                return true;
            }
            case ("System.Threading.Monitor", "Pulse"):
            {
                var obj = Pop(); // obj
                Emit($"dn2cpp_monitor_pulse((Dn2CppObject*)({obj.Expr}));");
                return true;
            }
            case ("System.Threading.Monitor", "PulseAll"):
            {
                var obj = Pop(); // obj
                Emit($"dn2cpp_monitor_pulse_all((Dn2CppObject*)({obj.Expr}));");
                return true;
            }
            //.NET 9+ System.Threading.Lock. `lock(lockVar)` lowers to
            // `var s = lockVar.EnterScope(); try {…} finally { s.Dispose(); }`. Backed by
            // the same per-object mutex table as Monitor (keyed on the Lock object). The
            // Scope carries the Lock so Dispose releases it.
            case ("System.Threading.Lock", "EnterScope"):
            {
                var lk = Pop(); // this (the Lock)
                Emit($"dn2cpp_monitor_enter((Dn2CppObject*)({lk.Expr}));");
                Push(StackKind.Struct, "Dn2CppLockScope", $"Dn2CppLockScope{{(Dn2CppObject*)({lk.Expr})}}");
                return true;
            }
            case ("System.Threading.Lock", "Enter"):
            {
                var lk = Pop(); // this
                Emit($"dn2cpp_monitor_enter((Dn2CppObject*)({lk.Expr}));");
                return true;
            }
            case ("System.Threading.Lock", "Exit"):
            {
                var lk = Pop(); // this
                Emit($"dn2cpp_monitor_exit((Dn2CppObject*)({lk.Expr}));");
                return true;
            }
            case ("System.Threading.Lock", "TryEnter"):
            {
                for (int i = 0; i < sig.ParameterTypes.Length; i++)
                    Pop(); // [timeout] (not honored — try_lock)
                var lk = Pop(); // this
                Push(StackKind.I4, "int32_t", $"dn2cpp_monitor_try_enter((Dn2CppObject*)({lk.Expr}))");
                return true;
            }
            // IsHeldByCurrentThread is intentionally NOT intrinsified: the monitor table
            // does not track the owning thread, so it would have to return a constant.
            // Leaving it unimplemented fails loudly rather than silently mismodeling.
            // Lock.Scope.Dispose — the `lock` body's finally; releases the lock the Scope
            // captured. The dispatch key is enclosing-qualified (see TranslateIntrinsic) so
            // it can't collide with another nested "Scope" type's Dispose.
            case ("System.Threading.Lock+Scope", "Dispose"):
            {
                var scope = Pop(); // this (the Scope, by address)
                Emit($"dn2cpp_monitor_exit(((Dn2CppLockScope*)({scope.Expr}))->lock);");
                return true;
            }
            // Thread.MemoryBarrier / Interlocked.MemoryBarrier — full fence.
            case ("System.Threading.Interlocked", "MemoryBarrier"):
            case ("System.Threading.Thread", "MemoryBarrier"):
                Emit("__atomic_thread_fence(__ATOMIC_SEQ_CST);");
                return true;
            // Interlocked.MemoryBarrierProcessWide — a barrier every OTHER thread of
            // the process also observes as a full fence (the asymmetric-fence
            // pattern); the per-OS mechanism lives behind the runtime's PAL seam.
            case ("System.Threading.Interlocked", "MemoryBarrierProcessWide"):
                Emit("dn2cpp_interlocked_membarrier_processwide();");
                return true;
            // Volatile.Read/Write(ref T): acquire load / release store via seq_cst
            // atomics. Integer/pointer overloads inline __atomic_load_n/store_n at the
            // element's storage width; float/double route through bit-cast helpers.
            case ("System.Threading.Volatile", "Read"):
            {
                var loc = Pop(); // ref T
                var elem = sig.ParameterTypes[0].Element!;
                EmitVolatileRead(elem, loc);
                return true;
            }
            case ("System.Threading.Volatile", "Write"):
            {
                var value = Pop();
                var loc = Pop(); // ref T
                var elem = sig.ParameterTypes[0].Element!;
                EmitVolatileWrite(elem, loc, value);
                return true;
            }
            // Thread.VolatileRead/VolatileWrite — the legacy pre-Volatile spellings
            // (every overload is ref X -> X / ref X, X); same semantics, same arms.
            case ("System.Threading.Thread", "VolatileRead"):
            {
                var loc = Pop(); // ref X
                EmitVolatileRead(sig.ParameterTypes[0].Element!, loc);
                return true;
            }
            case ("System.Threading.Thread", "VolatileWrite"):
            {
                var value = Pop();
                var loc = Pop(); // ref X
                EmitVolatileWrite(sig.ParameterTypes[0].Element!, loc, value);
                return true;
            }
            // ---- System.Threading.Thread (real OS threads) ----
            // The receiver may arrive as Dn2CppObject* (e.g. loaded from a Thread[]),
            // so cast it to Dn2CppThread* for each helper call.
            case ("System.Threading.Thread", "Start"):
            {
                if (sig.ParameterTypes.Length == 1)
                {
                    var arg = Pop();  // object parameter (ParameterizedThreadStart)
                    var th = Pop();   // this
                    Emit($"dn2cpp_thread_start_param({Cast(th, "Dn2CppThread*")}, (Dn2CppObject*)({arg.Expr}));");
                }
                else
                {
                    var th = Pop();   // this
                    Emit($"dn2cpp_thread_start({Cast(th, "Dn2CppThread*")});");
                }
                return true;
            }
            case ("System.Threading.Thread", "Join") when sig.ParameterTypes.Length == 0:
            {
                var th = Pop(); // this
                Emit($"dn2cpp_thread_join({Cast(th, "Dn2CppThread*")});");
                return true;
            }
            case ("System.Threading.Thread", "Join") when sig.ParameterTypes.Length == 1:
            {
                // Join(int ms) / Join(TimeSpan): wait up to the timeout for the thread to
                // finish, returning true if it terminated and false on timeout. A negative
                // ms (Timeout.Infinite) waits indefinitely.
                var to = Pop(); // timeout (int ms or TimeSpan)
                var th = Pop(); // this
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_thread_join_timeout({Cast(th, "Dn2CppThread*")}, {TimeoutMs(to, sig.ParameterTypes[0])})");
                return true;
            }
            case ("System.Threading.Thread", "Sleep")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var ms = Pop();
                Emit($"dn2cpp_thread_sleep({ms.Expr});");
                return true;
            }
            case ("System.Threading.Thread", "Yield"):
                Push(StackKind.I4, "int32_t", "dn2cpp_thread_yield()");
                return true;
            // Thread.UninterruptibleSleep0() — the internal Thread.Sleep(0) the BCL's spin
            // paths use to yield the current time slice. Route to the yield helper (its bool
            // result is discarded); no interruptibility state to honor.
            case ("System.Threading.Thread", "UninterruptibleSleep0"):
                Emit("dn2cpp_thread_yield();");
                return true;
            case ("System.Threading.Thread", "SpinWait"):
                Pop(); // iterations — a hint; nothing to do
                return true;
            // SpinWait.SpinOnceCore's per-iteration spin budget — a runtime-measured
            // tuning constant on real .NET (an InternalCall). Any small positive
            // value is semantically fine (it only scales busy-wait iterations
            // before yielding); 8 matches the observed order of magnitude.
            case ("System.Threading.Thread", "get_OptimalMaxSpinWaitsPerSpinIteration"):
                Push(StackKind.I4, "int32_t", "8");
                return true;
            // Thread.GetCurrentProcessorId — a partition-selection HINT (the BCL contract
            // allows any value). The runtime helper hands each thread a stable round-robin
            // id instead of the real ProcessorIdCache/SchedGetCpu path.
            case ("System.Threading.Thread", "GetCurrentProcessorId")
                when sig.ParameterTypes.Length == 0:
                Push(StackKind.I4, "int32_t", "dn2cpp_current_processor_id()");
                return true;
            case ("System.Threading.Thread", "get_CurrentThread"):
                Push(StackKind.Ref, "Dn2CppThread*", "dn2cpp_thread_current()");
                return true;
            case ("System.Threading.Thread", "get_ManagedThreadId"):
            {
                var th = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_thread_managed_id({Cast(th, "Dn2CppThread*")})");
                return true;
            }
            case ("System.Threading.Thread", "get_IsAlive"):
            {
                var th = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_thread_is_alive({Cast(th, "Dn2CppThread*")})");
                return true;
            }
            case ("System.Threading.Thread", "get_IsBackground"):
            {
                var th = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_thread_get_background({Cast(th, "Dn2CppThread*")})");
                return true;
            }
            case ("System.Threading.Thread", "set_IsBackground"):
            {
                var v = Pop();
                var th = Pop();
                Emit($"dn2cpp_thread_set_background({Cast(th, "Dn2CppThread*")}, {v.Expr});");
                return true;
            }
            case ("System.Threading.Thread", "get_Name"):
            {
                var th = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_thread_get_name({Cast(th, "Dn2CppThread*")})");
                return true;
            }
            case ("System.Threading.Thread", "set_Name"):
            {
                var n = Pop();
                var th = Pop();
                Emit($"dn2cpp_thread_set_name({Cast(th, "Dn2CppThread*")}, {Cast(n, "Dn2CppString*")});");
                return true;
            }
            // ---- SemaphoreSlim (real counting semaphore) ----
            // Wait([timeout] [, CancellationToken]). A timeout (int ms or TimeSpan) is a
            // real timed acquire returning true if a token was taken / false on timeout;
            // Wait() and Wait(CancellationToken) block until a token is available. The
            // CancellationToken is not honored (dropped).
            case ("System.Threading.SemaphoreSlim", "Wait"):
            {
                var ps = sig.ParameterTypes;
                bool timed = ps.Length >= 1 && IsTimeoutParam(ps[0]);
                if (timed)
                {
                    for (int i = ps.Length - 1; i >= 1; i--) // drop a trailing CancellationToken
                        Pop();
                    var to = Pop(); // timeout (int ms or TimeSpan)
                    var o = Pop();
                    Push(StackKind.I4, "int32_t",
                        $"dn2cpp_semaphore_wait_timeout((Dn2CppObject*)({o.Expr}), {TimeoutMs(to, ps[0])})");
                }
                else
                {
                    for (int i = 0; i < ps.Length; i++)
                        Pop(); // CancellationToken (ignored)
                    var o = Pop();
                    Emit($"dn2cpp_semaphore_wait((Dn2CppObject*)({o.Expr}));");
                }
                return true;
            }
            case ("System.Threading.SemaphoreSlim", "Release"):
            {
                string n = "1";
                if (sig.ParameterTypes.Length == 1)
                    n = Pop().Expr;
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_semaphore_release((Dn2CppObject*)({o.Expr}), {n})");
                return true;
            }
            case ("System.Threading.SemaphoreSlim", "get_CurrentCount"):
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_semaphore_count((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            // WaitAsync() / WaitAsync(CancellationToken) -> Task: an immediate
            // acquire yields a pre-completed task; a contended acquire escapes the
            // blocking wait to the worker pool (see dn2cpp_semaphore_wait_async).
            // The CancellationToken is not honored (dropped), mirroring the sync
            // Wait; the timed WaitAsync(int/TimeSpan) overloads stay unmapped.
            case ("System.Threading.SemaphoreSlim", "WaitAsync")
                when sig.ParameterTypes.Length == 0 || !IsTimeoutParam(sig.ParameterTypes[0]):
            {
                for (int i = 0; i < sig.ParameterTypes.Length; i++)
                    Pop(); // CancellationToken (ignored)
                var o = Pop();
                PushStampedTask($"dn2cpp_semaphore_wait_async((Dn2CppObject*)({o.Expr}))", sig.ReturnType);
                return true;
            }
            // ---- ManualResetEvent(Slim) / AutoResetEvent ----
            // Set/Reset arrive on ManualResetEventSlim directly or on EventWaitHandle (the
            // base of ManualResetEvent/AutoResetEvent). EventWaitHandle.Set/Reset return
            // bool; the Slim ones return void.
            //
            // The Set arm also serves the internal STATIC EventWaitHandle.Set(SafeWaitHandle)
            // without a case of its own: a SafeWaitHandle is the same Dn2CppObject* as the
            // event it guards (see the SafeWaitHandle block below), so there is one operand
            // either way and popping "the receiver" pops the handle.
            case ("System.Threading.ManualResetEventSlim", "Set"):
            case ("System.Threading.EventWaitHandle", "Set"):
            {
                var o = Pop();
                Emit($"dn2cpp_event_set((Dn2CppObject*)({o.Expr}));");
                if (!sig.ReturnType.IsVoid)
                    Push(StackKind.I4, "int32_t", "1");
                return true;
            }
            case ("System.Threading.ManualResetEventSlim", "Reset"):
            case ("System.Threading.EventWaitHandle", "Reset"):
            {
                var o = Pop();
                Emit($"dn2cpp_event_reset((Dn2CppObject*)({o.Expr}));");
                if (!sig.ReturnType.IsVoid)
                    Push(StackKind.I4, "int32_t", "1");
                return true;
            }
            // WaitOne (WaitHandle, for MRE/ARE) and ManualResetEventSlim.Wait. A timeout
            // (int ms or TimeSpan) is a real timed wait returning true if signaled / false
            // on timeout; the no-timeout forms block until signaled. WaitHandle.WaitOne()
            // returns bool, ManualResetEventSlim.Wait()/Wait(CancellationToken) return void.
            // A trailing exitContext bool / CancellationToken is ignored.
            case ("System.Threading.ManualResetEventSlim", "Wait"):
            case ("System.Threading.WaitHandle", "WaitOne"):
            {
                var ps = sig.ParameterTypes;
                bool timed = ps.Length >= 1 && IsTimeoutParam(ps[0]);
                if (timed)
                {
                    for (int i = ps.Length - 1; i >= 1; i--) // drop trailing exitContext / token
                        Pop();
                    var to = Pop(); // timeout (int ms or TimeSpan)
                    var o = Pop();
                    Push(StackKind.I4, "int32_t",
                        $"dn2cpp_event_wait_timeout((Dn2CppObject*)({o.Expr}), {TimeoutMs(to, ps[0])})");
                }
                else
                {
                    for (int i = 0; i < ps.Length; i++)
                        Pop(); // CancellationToken (ignored)
                    var o = Pop();
                    if (sig.ReturnType.IsVoid)
                        Emit($"dn2cpp_event_wait((Dn2CppObject*)({o.Expr}));");
                    else
                        Push(StackKind.I4, "int32_t", $"dn2cpp_event_wait((Dn2CppObject*)({o.Expr}))");
                }
                return true;
            }
            case ("System.Threading.ManualResetEventSlim", "get_IsSet"):
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_event_is_set((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            // WaitHandle.WaitAny(WaitHandle[]) -> int: block until any handle in the array is
            // signaled, returning its index. Only the single-array overload is mapped — the
            // timed WaitAny(WaitHandle[], int/TimeSpan [, bool]) overloads DECLINE here (fall
            // through to the default), so a differently-shaped call is never popped as this one.
            case ("System.Threading.WaitHandle", "WaitAny") when sig.ParameterTypes.Length == 1:
            {
                var arr = Pop(); // WaitHandle[]
                Push(StackKind.I4, "int32_t", $"dn2cpp_event_wait_any((Dn2CppArrayRef*)({arr.Expr}))");
                return true;
            }
            // Dispose/Close on these primitives are no-ops (the native object lives for
            // the program, like the monitor table).
            case ("System.Threading.SemaphoreSlim", "Dispose"):
            case ("System.Threading.ManualResetEventSlim", "Dispose"):
            case ("System.Threading.WaitHandle", "Dispose"):
            case ("System.Threading.WaitHandle", "Close"):
            // The safe handle is the same never-closed runtime object (see the
            // SafeWaitHandle block below), so its Dispose/Close are the same no-op.
            case ("Microsoft.Win32.SafeHandles.SafeWaitHandle", "Dispose"):
            case ("Microsoft.Win32.SafeHandles.SafeWaitHandle", "Close"):
            {
                Pop(); // this
                return true;
            }
            // ---- SafeWaitHandle: the handle of a WaitHandle, which here IS the WaitHandle ----
            // dn2cpp models the WaitHandle family as an opaque runtime mutex+condvar
            // object rather than as an OS handle, so the SafeWaitHandle wrapping one is
            // mapped to that same Dn2CppObject* (CoreIntrinsics.s_intrinsicCppTypes) and
            // this getter is the identity. The real getter's lazy
            // `new SafeWaitHandle(InvalidHandle, ownsHandle: false)` fallback needs no
            // counterpart: it exists for a WaitHandle whose _waitHandle field is null, and
            // every WaitHandle this model can produce is built by dn2cpp_event_new
            // (MethodCompiler.Newobj) with the object in hand.
            case ("System.Threading.WaitHandle", "get_SafeWaitHandle"):
            {
                var o = Pop(); // this
                Push(StackKind.Ref, "Dn2CppObject*", $"((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            // The raw handle value. It must be a value DISTINCT from 0 and from -1 for
            // every live handle, not merely non-null: RegisteredWaitHandle.IsBlocking is
            // `UserUnregisterWaitHandleValue == (nint)(-1)` and its
            // SignalUserWaitHandle skips the signal on 0, so both sentinels are read as
            // sentinels by live code. A real object address is neither.
            case ("Microsoft.Win32.SafeHandles.SafeWaitHandle", "DangerousGetHandle"):
            {
                var o = Pop(); // this
                Push(StackKind.I8, "intptr_t", $"(intptr_t)({o.Expr})");
                return true;
            }
            // DangerousAddRef(ref bool success) / DangerousRelease — the refcount that
            // keeps an OS handle alive across a P/Invoke. There is no OS handle and the
            // object is GC-owned for the life of the program (Dispose/Close above are
            // already no-ops), so the count has nothing to protect: the add always
            // succeeds and the release is a no-op — the answer a live, never-closed handle
            // gives in real .NET too. The low-byte write mirrors Monitor.Enter's ref-bool
            // store above.
            case ("Microsoft.Win32.SafeHandles.SafeWaitHandle", "DangerousAddRef"):
            {
                var success = Pop(); // ref bool success
                Pop();               // this
                Emit($"*(uint8_t*)({success.Expr}) = 1;");
                return true;
            }
            case ("Microsoft.Win32.SafeHandles.SafeWaitHandle", "DangerousRelease"):
            {
                Pop(); // this
                return true;
            }
            // IsInvalid comes from SafeHandleZeroOrMinusOneIsInvalid (handle is 0 or -1);
            // here the only unrepresentable handle is the null object. IsClosed is always
            // false for the same reason DangerousRelease is a no-op.
            case ("Microsoft.Win32.SafeHandles.SafeWaitHandle", "get_IsInvalid"):
            {
                var o = Pop(); // this
                Push(StackKind.I4, "int32_t", $"((({o.Expr}) == nullptr) ? 1 : 0)");
                return true;
            }
            case ("Microsoft.Win32.SafeHandles.SafeWaitHandle", "get_IsClosed"):
            {
                Pop(); // this
                Push(StackKind.I4, "int32_t", "0");
                return true;
            }
            // ---- CountdownEvent (real countdown latch) ----
            // Signal([n]) decrements (default 1) and returns whether the count reached zero;
            // AddCount/TryAddCount grow it; Reset rewinds it; Wait blocks (or times out) until
            // it is zero. A trailing CancellationToken is dropped (cancellation is not honored).
            case ("System.Threading.CountdownEvent", "Signal"):
            {
                string n = sig.ParameterTypes.Length == 1 ? Pop().Expr : "1";
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_countdown_signal((Dn2CppObject*)({o.Expr}), {n})");
                return true;
            }
            case ("System.Threading.CountdownEvent", "AddCount"):
            {
                string n = sig.ParameterTypes.Length == 1 ? Pop().Expr : "1";
                var o = Pop();
                Emit($"dn2cpp_countdown_add((Dn2CppObject*)({o.Expr}), {n});");
                return true;
            }
            case ("System.Threading.CountdownEvent", "TryAddCount"):
            {
                string n = sig.ParameterTypes.Length == 1 ? Pop().Expr : "1";
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_countdown_try_add((Dn2CppObject*)({o.Expr}), {n})");
                return true;
            }
            case ("System.Threading.CountdownEvent", "Reset"):
            {
                if (sig.ParameterTypes.Length == 1)
                {
                    var n = Pop();
                    var o = Pop();
                    Emit($"dn2cpp_countdown_reset((Dn2CppObject*)({o.Expr}), {n.Expr});");
                }
                else
                {
                    var o = Pop();
                    Emit($"dn2cpp_countdown_reset_default((Dn2CppObject*)({o.Expr}));");
                }
                return true;
            }
            case ("System.Threading.CountdownEvent", "Wait"):
            {
                var ps = sig.ParameterTypes;
                bool timed = ps.Length >= 1 && IsTimeoutParam(ps[0]);
                if (timed)
                {
                    for (int i = ps.Length - 1; i >= 1; i--) // drop a trailing CancellationToken
                        Pop();
                    var to = Pop(); // timeout (int ms or TimeSpan)
                    var o = Pop();
                    Push(StackKind.I4, "int32_t",
                        $"dn2cpp_countdown_wait_timeout((Dn2CppObject*)({o.Expr}), {TimeoutMs(to, ps[0])})");
                }
                else
                {
                    for (int i = 0; i < ps.Length; i++)
                        Pop(); // CancellationToken (ignored)
                    var o = Pop();
                    Emit($"dn2cpp_countdown_wait((Dn2CppObject*)({o.Expr}));");
                }
                return true;
            }
            case ("System.Threading.CountdownEvent", "get_CurrentCount"):
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_countdown_current((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            case ("System.Threading.CountdownEvent", "get_InitialCount"):
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_countdown_initial((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            case ("System.Threading.CountdownEvent", "get_IsSet"):
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_countdown_is_set((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            case ("System.Threading.CountdownEvent", "Dispose"):
            case ("System.Threading.Barrier", "Dispose"):
            {
                Pop(); // this
                return true;
            }
            // ---- Barrier (real cyclic phase barrier) ----
            // SignalAndWait blocks until every participant has signaled (a timed overload
            // returns true if the phase completed / false on timeout). Add/Remove adjust the
            // participant count. A trailing CancellationToken is dropped.
            case ("System.Threading.Barrier", "SignalAndWait"):
            {
                var ps = sig.ParameterTypes;
                bool timed = ps.Length >= 1 && IsTimeoutParam(ps[0]);
                if (timed)
                {
                    for (int i = ps.Length - 1; i >= 1; i--) // drop a trailing CancellationToken
                        Pop();
                    var to = Pop(); // timeout (int ms or TimeSpan)
                    var o = Pop();
                    Push(StackKind.I4, "int32_t",
                        $"dn2cpp_barrier_signal_and_wait_timeout((Dn2CppObject*)({o.Expr}), {TimeoutMs(to, ps[0])})");
                }
                else
                {
                    for (int i = 0; i < ps.Length; i++)
                        Pop(); // CancellationToken (ignored)
                    var o = Pop();
                    if (sig.ReturnType.IsVoid)
                        Emit($"dn2cpp_barrier_signal_and_wait((Dn2CppObject*)({o.Expr}));");
                    else
                        Push(StackKind.I4, "int32_t", $"dn2cpp_barrier_signal_and_wait((Dn2CppObject*)({o.Expr}))");
                }
                return true;
            }
            case ("System.Threading.Barrier", "AddParticipant"):
            {
                var o = Pop();
                Push(StackKind.I8, "int64_t", $"dn2cpp_barrier_add((Dn2CppObject*)({o.Expr}), 1)");
                return true;
            }
            case ("System.Threading.Barrier", "AddParticipants"):
            {
                var n = Pop();
                var o = Pop();
                Push(StackKind.I8, "int64_t", $"dn2cpp_barrier_add((Dn2CppObject*)({o.Expr}), {n.Expr})");
                return true;
            }
            case ("System.Threading.Barrier", "RemoveParticipant"):
            {
                var o = Pop();
                Emit($"dn2cpp_barrier_remove((Dn2CppObject*)({o.Expr}), 1);");
                return true;
            }
            case ("System.Threading.Barrier", "RemoveParticipants"):
            {
                var n = Pop();
                var o = Pop();
                Emit($"dn2cpp_barrier_remove((Dn2CppObject*)({o.Expr}), {n.Expr});");
                return true;
            }
            case ("System.Threading.Barrier", "get_ParticipantCount"):
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_barrier_participant_count((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            case ("System.Threading.Barrier", "get_ParticipantsRemaining"):
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_barrier_participants_remaining((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            case ("System.Threading.Barrier", "get_CurrentPhaseNumber"):
            {
                var o = Pop();
                Push(StackKind.I8, "int64_t", $"dn2cpp_barrier_current_phase((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            // ---- ReaderWriterLockSlim (real reader-writer lock) ----
            // Enter/Exit the read, write, and upgradeable-read locks (all void). TryEnter*
            // takes a timeout (int ms or TimeSpan) and returns true if the lock was taken /
            // false on timeout. CurrentReadCount/WaitingWriteCount are state queries. The
            // runtime tracks per-thread ownership: a forbidden same-thread re-entry
            // throws LockRecursionException instead of deadlocking, SupportsRecursion counts
            // re-entries, the upgrade-to-write path works, and the per-thread lock-held
            // queries answer for the calling thread — all matching real .NET.
            case ("System.Threading.ReaderWriterLockSlim", "EnterReadLock"):
            {
                var o = Pop();
                Emit($"dn2cpp_rwlock_enter_read((Dn2CppObject*)({o.Expr}));");
                return true;
            }
            case ("System.Threading.ReaderWriterLockSlim", "ExitReadLock"):
            {
                var o = Pop();
                Emit($"dn2cpp_rwlock_exit_read((Dn2CppObject*)({o.Expr}));");
                return true;
            }
            case ("System.Threading.ReaderWriterLockSlim", "EnterWriteLock"):
            {
                var o = Pop();
                Emit($"dn2cpp_rwlock_enter_write((Dn2CppObject*)({o.Expr}));");
                return true;
            }
            case ("System.Threading.ReaderWriterLockSlim", "ExitWriteLock"):
            {
                var o = Pop();
                Emit($"dn2cpp_rwlock_exit_write((Dn2CppObject*)({o.Expr}));");
                return true;
            }
            case ("System.Threading.ReaderWriterLockSlim", "EnterUpgradeableReadLock"):
            {
                var o = Pop();
                Emit($"dn2cpp_rwlock_enter_upgradeable((Dn2CppObject*)({o.Expr}));");
                return true;
            }
            case ("System.Threading.ReaderWriterLockSlim", "ExitUpgradeableReadLock"):
            {
                var o = Pop();
                Emit($"dn2cpp_rwlock_exit_upgradeable((Dn2CppObject*)({o.Expr}));");
                return true;
            }
            case ("System.Threading.ReaderWriterLockSlim", "TryEnterReadLock"):
            {
                var ps = sig.ParameterTypes;
                var to = Pop(); // timeout (int ms or TimeSpan)
                var o = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_rwlock_try_enter_read((Dn2CppObject*)({o.Expr}), {TimeoutMs(to, ps[0])})");
                return true;
            }
            case ("System.Threading.ReaderWriterLockSlim", "TryEnterWriteLock"):
            {
                var ps = sig.ParameterTypes;
                var to = Pop(); // timeout (int ms or TimeSpan)
                var o = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_rwlock_try_enter_write((Dn2CppObject*)({o.Expr}), {TimeoutMs(to, ps[0])})");
                return true;
            }
            case ("System.Threading.ReaderWriterLockSlim", "TryEnterUpgradeableReadLock"):
            {
                var ps = sig.ParameterTypes;
                var to = Pop(); // timeout (int ms or TimeSpan)
                var o = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_rwlock_try_enter_upgradeable((Dn2CppObject*)({o.Expr}), {TimeoutMs(to, ps[0])})");
                return true;
            }
            case ("System.Threading.ReaderWriterLockSlim", "get_CurrentReadCount"):
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_rwlock_current_read_count((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            case ("System.Threading.ReaderWriterLockSlim", "get_WaitingWriteCount"):
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_rwlock_waiting_write_count((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            case ("System.Threading.ReaderWriterLockSlim", "Dispose"):
            {
                Pop(); // this — no-op (the native object lives for the program, like monitors)
                return true;
            }
            // The per-thread lock-held queries, answered from the ownership tracking the
            // recursion checks keep.
            case ("System.Threading.ReaderWriterLockSlim", "get_IsReadLockHeld"):
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_rwlock_is_read_held((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            case ("System.Threading.ReaderWriterLockSlim", "get_IsWriteLockHeld"):
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_rwlock_is_write_held((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            case ("System.Threading.ReaderWriterLockSlim", "get_IsUpgradeableReadLockHeld"):
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_rwlock_is_upgradeable_held((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            // RecursionPolicy: the LockRecursionPolicy enum rides as its int32 value.
            case ("System.Threading.ReaderWriterLockSlim", "get_RecursionPolicy"):
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_rwlock_recursion_policy((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            // ---- System.Threading.Timer (per-timer OS thread) ----
            // Change(dueTime, period): reschedule (int/long/uint/TimeSpan overloads, converted
            // to int64 ms). Returns false after Dispose, true otherwise — matching .NET.
            case ("System.Threading.Timer", "Change"):
            case ("System.TimeProvider+SystemTimeProviderTimer", "Change"):
            {
                var ps = sig.ParameterTypes;
                // Two operands plus the receiver are popped below and both parameters read,
                // so the shape is asserted first: a name-gated route may decline a shape,
                // never pop one.
                if (ps.Length != 2)
                    throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: {declType}.Change "
                        + $"expects (dueTime, period), got {SigArgs(sig)}");
                var period = Pop();
                var dueTime = Pop();
                var o = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_timer_change((Dn2CppObject*)({o.Expr}), {TimerMs(dueTime, ps[0])}, {TimerMs(period, ps[1])})");
                return true;
            }
            // Dispose(): stop the timer and join its thread. Timer.Dispose() returns void, so
            // discard the helper's result. DisposeAsync stops synchronously and returns a
            // completed ValueTask; Dispose(WaitHandle) remains a carve-out below.
            // A `when` clause binds to its own label only, so each zero-arg label carries its
            // own guard and the throw below covers both type names — an unguarded label would
            // pop the receiver and strand the extra argument on the stack.
            case ("System.Threading.Timer", "Dispose") when sig.ParameterTypes.Length == 0:
            case ("System.TimeProvider+SystemTimeProviderTimer", "Dispose")
                when sig.ParameterTypes.Length == 0:
            {
                var o = Pop();
                Emit($"dn2cpp_timer_dispose((Dn2CppObject*)({o.Expr}));");
                return true;
            }
            case ("System.Threading.Timer", "Dispose"):
            case ("System.TimeProvider+SystemTimeProviderTimer", "Dispose"):
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: only {declType}.Dispose() is " +
                    "supported (Timer's Dispose(WaitHandle) signal overload is a carve-out)");
            case ("System.Threading.Timer", "DisposeAsync"):
            case ("System.TimeProvider+SystemTimeProviderTimer", "DisposeAsync"):
            {
                var o = Pop();
                Emit($"dn2cpp_timer_dispose((Dn2CppObject*)({o.Expr}));");
                Push(StackKind.Struct, "Dn2CppTaskAwaiter", "Dn2CppTaskAwaiter{ nullptr }");
                return true;
            }
            // ---- System.Threading.ThreadLocal<T> (per-instance, per-thread storage) ----
            // The value is stored boxed in a uniform slot; the get unboxes (value-type T)
            // or casts (reference T) to the element representation, the set boxes (value-
            // type T) or passes the reference directly. T is recovered from the property
            // signature (get returns T, set takes T) — the receiver's generic context
            // closed it when this intrinsic call was bound.
            case ("System.Threading.ThreadLocal", "get_Value"):
            {
                var elemT = sig.ReturnType;
                var h = Pop(); // this
                string got = $"dn2cpp_threadlocal_get((Dn2CppObject*)({h.Expr}))";
                if (CppTypes.KindOf(elemT) == StackKind.Ref)
                {
                    string ct = CppTypes.Of(elemT);
                    Push(StackKind.Ref, ct, $"({ct})({got})");
                }
                else
                {
                    string ct = CppTypes.Of(elemT);
                    string ti = TypeInfoExpr(elemT)
                        ?? throw new NotSupportedException(
                            $"{Method.DeclaringClass.FullName}.{Method.Name}: ThreadLocal<{elemT}>.Value has no boxable type-info");
                    Push(CppTypes.KindOf(elemT), ct, $"*(({ct}*)dn2cpp_unbox({got}, {ti}))");
                }
                return true;
            }
            case ("System.Threading.ThreadLocal", "set_Value"):
            {
                var elemT = sig.ParameterTypes[0];
                var val = Pop(); // value
                var h = Pop();   // this
                string stored;
                if (CppTypes.KindOf(elemT) == StackKind.Ref)
                {
                    stored = $"(Dn2CppObject*)({val.Expr})";
                }
                else
                {
                    string ct = CppTypes.Of(elemT);
                    string ti = TypeInfoExpr(elemT)
                        ?? throw new NotSupportedException(
                            $"{Method.DeclaringClass.FullName}.{Method.Name}: ThreadLocal<{elemT}>.Value has no boxable type-info");
                    string tmp = NewTemp(ct); // an addressable lvalue of the exact unboxed layout
                    Emit($"{tmp} = {Cast(val, ct)};");
                    stored = $"dn2cpp_box({ti}, &{tmp}, sizeof({ct}))";
                }
                Emit($"dn2cpp_threadlocal_set((Dn2CppObject*)({h.Expr}), {stored});");
                return true;
            }
            case ("System.Threading.ThreadLocal", "get_IsValueCreated"):
            {
                var h = Pop(); // this
                Push(StackKind.I4, "int32_t", $"dn2cpp_threadlocal_is_created((Dn2CppObject*)({h.Expr}))");
                return true;
            }
            // Dispose is a no-op: the per-thread storage lives for the program (like the
            // other threading primitives' Dispose); there is no per-thread teardown.
            case ("System.Threading.ThreadLocal", "Dispose"):
            {
                Pop(); // this
                return true;
            }
            // Values (trackAllValues all-thread snapshot) is not modeled — loud carve-out.
            case ("System.Threading.ThreadLocal", "get_Values"):
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: ThreadLocal<T>.Values " +
                    "(the trackAllValues all-thread snapshot) is not modeled");

            default:
                return false;
        }
    }
}
