using System;
using System.Globalization;

namespace UnsafePtr
{
    // Gate driver: runs each section's __GateEntry() in order. Each section keeps
    // its own namespace, since reflected type names reach the output.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            UnsafeBlockSubset.Program.__GateEntry();
            UnsafeReadWriteSubset.Program.__GateEntry();
            UnsafeSurfaceSubset.Program.__GateEntry();
            PointerArithSubset.Program.__GateEntry();
            PointerCompareSubset.Program.__GateEntry();
            FixedStatementSubset.Program.__GateEntry();
            FunctionPointerSubset.Program.__GateEntry();
            LdftnExternalSubset.Program.__GateEntry();
            // Subject is NOT Span: the cross-assembly resolve and tree-shake of
            // real corlib leaf bodies, the RuntimeHelpers.GetHashCode
            // InternalCall, and the [Intrinsic]-stub lowering of Unsafe.SizeOf /
            // Unsafe.Add — which is why it belongs in this bucket.
            CorelibSubset.Program.__GateEntry();
        }
    }
}
