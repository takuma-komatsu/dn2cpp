using System;
using System.Globalization;

namespace PInvokeNative
{
    // Gate driver: runs each section's __GateEntry() in order. Each section keeps
    // its own namespace, so reflected type names stay stable.
    internal static class Program
    {
        private static void Main()
        {
            // Gate output must not depend on the host locale.
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            PInvokeArraySubset.Program.__GateEntry();
            PInvokeBoolSubset.Program.__GateEntry();
            PInvokeByRefStringSubset.Program.__GateEntry();
            PInvokeCallConvSubset.Program.__GateEntry();
            PInvokeCharSubset.Program.__GateEntry();
            PInvokeMarshalAsSubset.Program.__GateEntry();
            PInvokeCustomLibSubset.Program.__GateEntry();
            PInvokeLastErrorSubset.Program.__GateEntry();
            PInvokeStringSubset.Program.__GateEntry();
            PInvokeStringBuilderSubset.Program.__GateEntry();
            PInvokeStructSubset.Program.__GateEntry();
            PInvokeSubWordStructSubset.Program.__GateEntry();
            PInvokeMarshalStructSubset.Program.__GateEntry();
            PInvokeMarshalLayoutSubset.Program.__GateEntry();
            PInvokeByValArraySubset.Program.__GateEntry();
            PInvokeWideCharArraySubset.Program.__GateEntry();
            PInvokeAnsiCharArraySubset.Program.__GateEntry();
            PInvokeByRefBlittableSubset.Program.__GateEntry();
            PInvokeStringArraySubset.Program.__GateEntry();
            PInvokeStructArraySubset.Program.__GateEntry();
            PInvokeWideStringSubset.Program.__GateEntry();
            PInvokeCallbackSubset.Program.__GateEntry();
            PInvokeStoredCallbackSubset.Program.__GateEntry();
            MarshalFnPtrSubset.Program.__GateEntry();
            RuntimePrimSubset.Program.__GateEntry();
            NativeImplSubset.Program.__GateEntry();
            PInvokeCrossAsmSubset.Program.__GateEntry();
            PInvokeFtnDelegateSubset.Program.__GateEntry();
            PInvokeUcoReverseSubset.Program.__GateEntry();
            PInvokeVtblStructSubset.Program.__GateEntry();
            PInvokeExplicitUnionSubset.Program.__GateEntry();
            SequentialSizeSubset.Program.__GateEntry();
            PInvokeVariantByRefSubset.Program.__GateEntry();
            PInvokeSingleFieldStructSubset.Program.__GateEntry();
            PInvokeStackallocUtf8Subset.Program.__GateEntry();
            // The one section binding the platform C library rather than dn2cpptest.
            PInvokeLibcSubset.Program.__GateEntry();
        }
    }
}
