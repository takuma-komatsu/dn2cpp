using System;
using System.Globalization;

namespace MarshalPinning
{
    // Auto-merged gate driver: runs each consolidated sample's __GateEntry() in
    // order. Each section keeps its own namespace.
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            if (args.Length == 1 && args[0] == "typed-handles")
            {
                TypedGCHandleSubset.Program.Run();
                return;
            }

            if (args.Length == 1 && args[0] == "--unprepared-box")
            {
                StructMarshalRoundtripSubset.Program.UnpreparedBox();
                return;
            }

            PinnedArraySubset.Program.__GateEntry();
            StructMarshalRoundtripSubset.Program.__GateEntry();
            SizeOfOffsetOfSubset.Program.__GateEntry();
            MarshalCopySubset.Program.__GateEntry();
            ConservativeGcPinSubset.Program.__GateEntry();
            NativeMemPrimitivesSubset.Program.__GateEntry();
            NativeMemoryBulkSubset.Program.__GateEntry();
            MarshalStringNativeSubset.Program.__GateEntry();
            NativeMemorySubset.Program.__GateEntry();
            StructMarshalSubset.Program.__GateEntry();
            StructMarshalOffsetOfSubset.Program.__GateEntry();
            AlignedAllocSubset.Program.__GateEntry();   // was aligned-alloc-subset
            MarshalNullFaultSubset.Program.__GateEntry();
            MarshalHResultSubset.Program.__GateEntry();
            NativeMemoryOverflowSubset.Program.__GateEntry();
            // GCHandlePinSubset runs after every heap-sensitive section. The
            // following marshalling reads only its own native buffer.
            GCHandlePinSubset.Program.__GateEntry();    // was gchandle-pin-subset
            StructMarshalRoundtripSubset.Program.TypeBoxing();
            if (args.Length == 1 && args[0] == "before-typed-handles") return;
            TypedGCHandleSubset.Program.Run();
        }
    }
}
