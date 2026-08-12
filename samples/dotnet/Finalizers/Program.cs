using System;
using System.Globalization;

namespace Finalizers
{
    // Auto-merged gate driver: runs each consolidated sample's __GateEntry
    // in order. Each section keeps its own namespace so reflected type names
    // and other namespace-sensitive output stay identical to the originals.
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            bool requireFinalizerWindows = args.Length == 1
                && args[0] == "--require-finalizer-windows";
            if (args.Length != 0 && !requireFinalizerWindows)
                throw new ArgumentException("unknown argument");

            FinalizerBasicSubset.Program.__GateEntry(requireFinalizerWindows);
            FinalizerInheritSubset.Program.__GateEntry();
            FinalizerSuppressSubset.Program.__GateEntry();
            FinalizerReRegisterSubset.Program.__GateEntry();
            FinalizerResurrectionSubset.Program.__GateEntry();
            FinalizerResurrectionWeakSubset.Program.__GateEntry();
            FinalizerCtorThrowSubset.Program.__GateEntry();
            FinalizerActivatorSubset.Program.__GateEntry();
            FinalizerExceptionTypeSubset.Program.__GateEntry();
            FinalizerReentrantWaitSubset.Program.__GateEntry();
            FinalizerNullArgSubset.Program.__GateEntry();
            FinalizerExitSubset.Program.__GateEntry();
            FinalizerKeepAliveSubset.Program.__GateEntry();
            FinalizerFloodSubset.Program.__GateEntry();
            // Appended at the tail rather than beside FinalizerActivatorSubset (its sibling
            // mouth) because the bucket's previous output must stay an unchanged PREFIX of
            // the new one: inserting mid-list moves every later section's lines and makes a
            // real perturbation look like an intentional reordering in the diff.
            FinalizerClonedSubset.Program.__GateEntry();
            FinalizerSuppressQueuedSubset.Program.__GateEntry(requireFinalizerWindows);
            // Must stay last: this section's finalizer throws an uncaught
            // exception and aborts the process, so nothing after it
            // would run.
            FinalizerExceptionSubset.Program.__GateEntry();
        }
    }
}
