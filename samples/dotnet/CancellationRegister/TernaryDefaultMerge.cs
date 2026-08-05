using System;
using System.Threading;

namespace TernaryDefaultMerge
{
    // SUBJECT: the eval-stack join of a struct-returning call with `default` — not the
    // registration itself, and not reflection. An intrinsic value type modeled as a raw
    // pointer is Struct-kinded where an initobj'd local is read and carries a different
    // kind on the other arm, so `flag ? call() : default` can fail the transpile with
    // `inconsistent stack`. Both disagreeing kinds are here (Ref for
    // CancellationTokenRegistration, Ptr for RuntimeTypeHandle) in both arm orders,
    // because the two spellings reach the join in opposite recording order.
    internal static class Program
    {
        private static bool s_ran;

        // Not a constant: a folded condition emits no branch and no join at all.
        private static bool Yes() => "x".Length == 1;

        internal static void __GateEntry()
        {
            var cts = new CancellationTokenSource();
            CancellationToken tok = cts.Token;
            s_ran = false;

            // Taken arm is the call; the `default` arm is recorded at the join first.
            CancellationTokenRegistration reg = Yes() ? tok.Register(() => { s_ran = true; }) : default;
            cts.Cancel();
            Console.WriteLine("ternary call arm ran: " + s_ran);
            reg.Dispose();
            cts.Dispose();

            // Taken arm is `default`: nothing is registered and disposing the zero
            // handle is a no-op.
            var cts2 = new CancellationTokenSource();
            CancellationToken tok2 = cts2.Token;
            s_ran = false;
            CancellationTokenRegistration reg2 = Yes() ? default : tok2.Register(() => { s_ran = true; });
            cts2.Cancel();
            Console.WriteLine("ternary default arm ran: " + s_ran);
            reg2.Dispose();
            cts2.Dispose();

            // Reversed arm order, so the CALL arm is what the join records first.
            var cts3 = new CancellationTokenSource();
            CancellationToken tok3 = cts3.Token;
            s_ran = false;
            CancellationTokenRegistration reg3 = !Yes() ? default : tok3.Register(() => { s_ran = true; });
            reg3.Dispose();
            cts3.Cancel();
            Console.WriteLine("ternary reversed detached: " + s_ran);
            cts3.Dispose();

            // The Ptr-kinded spelling of the same join: RuntimeTypeHandle. Both arm
            // orders again, and the taken handle must still resolve to its own type.
            RuntimeTypeHandle h = Yes() ? typeof(Program).TypeHandle : default;
            Console.WriteLine("ternary handle set: " + (h.Value != IntPtr.Zero)
                + ", " + Type.GetTypeFromHandle(h)!.FullName);
            RuntimeTypeHandle h2 = Yes() ? default : typeof(Program).TypeHandle;
            Console.WriteLine("ternary handle default: " + (h2.Value == IntPtr.Zero));
        }
    }
}
