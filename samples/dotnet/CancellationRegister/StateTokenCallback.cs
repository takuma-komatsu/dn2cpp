#nullable enable
using System;
using System.Threading;

namespace StateTokenCallback
{
    // SUBJECT: the two Register overload shapes with a TOKEN parameter —
    // Register/UnsafeRegister(Action<object?, CancellationToken>, object?), the
    // state-AND-token form, and UnsafeRegister(Action<object?>, object?). Both are
    // lowered by the same arms as their Register(Action<object?>, object?) sibling
    // (MethodCompiler.EmitIntrinsic.AwaitersTasks.cs), so what is asserted here is the
    // part that is NOT shared with it:
    //
    //   * the callback receives the CANCELLING token, not a default one — the token
    //     argument is read back for IsCancellationRequested, which is the one thing a
    //     lowering that passed CancellationToken.None would get wrong while looking
    //     right;
    //   * the state argument arrives beside it, in the right position;
    //   * the LIFO / already-canceled / Dispose-detaches discipline is the same for
    //     these shapes as for the one-argument form (the runtime node is a sibling of
    //     it, so a divergence here would be a divergence in that discipline);
    //   * Register and UnsafeRegister are indistinguishable, which is the claim dn2cpp
    //     makes about them: their only difference is the ExecutionContext capture, and
    //     nothing is ever flowed here.
    //
    // Every Cancel() is on this thread, so the ordering is deterministic and diffs exact
    // against real .NET.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            // --- the token handed to the callback is the cancelling one ---
            var cts = new CancellationTokenSource();
            cts.Token.Register(
                static (s, t) => Console.WriteLine($"reg state={s} requested={t.IsCancellationRequested}"),
                "a");
            cts.Token.UnsafeRegister(
                static (s, t) => Console.WriteLine($"unsafe state={s} requested={t.IsCancellationRequested}"),
                "b");
            cts.Token.UnsafeRegister(static s => Console.WriteLine($"unsafe1 state={s}"), "c");
            // LIFO: c, b, a.
            cts.Cancel();

            // --- a null state is legal and arrives as null ---
            var nul = new CancellationTokenSource();
            nul.Token.Register(static (s, t) => Console.WriteLine($"null state={s ?? "(null)"}"), null);
            nul.Cancel();

            // --- an ALREADY-canceled source runs the callback immediately, on this
            //     thread, with the same token ---
            var already = new CancellationTokenSource();
            already.Cancel();
            already.Token.Register(
                static (s, t) => Console.WriteLine($"immediate state={s} requested={t.IsCancellationRequested}"),
                "d");
            already.Token.UnsafeRegister(
                static (s, t) => Console.WriteLine($"immediate-unsafe state={s} requested={t.IsCancellationRequested}"),
                "e");

            // --- Dispose() detaches before the cancel ---
            var detach = new CancellationTokenSource();
            CancellationTokenRegistration reg = detach.Token.Register(
                static (s, t) => Console.WriteLine($"SHOULD NOT RUN {s}"), "f");
            reg.Dispose();
            detach.Token.Register(static (s, t) => Console.WriteLine($"kept state={s}"), "g");
            detach.Cancel();

            // --- CancellationToken.None can never cancel, so the registration is a no-op ---
            CancellationToken.None.Register(static (s, t) => Console.WriteLine($"SHOULD NOT RUN {s}"), "h");
            Console.WriteLine("state-token callback section done");
        }
    }
}
