using System;

namespace CancellationRegister
{
    // Consolidated gate driver for CancellationToken.Register(Action): runs each
    // section's __GateEntry() in order. Each section keeps its own namespace so the
    // output stays stable. Every cross-thread Cancel() is joined before the result is
    // printed, so the program is fully deterministic and diffed exact vs real .NET.
    internal static class Program
    {
        private static void Main()
        {
            CrossThreadCancel.Program.__GateEntry();
            LifoOrder.Program.__GateEntry();
            AlreadyCanceled.Program.__GateEntry();
            UnregisterDispose.Program.__GateEntry();
            Contention.Program.__GateEntry();
            CancelAfterTimer.Program.__GateEntry();
            CancelAfterRoot.Program.__GateEntry();
            StateTokenCallback.Program.__GateEntry();
            TernaryDefaultMerge.Program.__GateEntry();
            RegistrationToken.Program.__GateEntry();
            // APPENDED LAST (the prove-it-ran prefix rule, AGENTS.md).
            CancellationIntrinsicToString.Program.__GateEntry();
        }
    }
}
