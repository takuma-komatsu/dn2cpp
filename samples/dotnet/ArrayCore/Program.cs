using System;
using System.Globalization;

namespace ArrayCore
{
    // Auto-merged gate driver: runs each consolidated sample's Run() in
    // order. Each section keeps its own namespace so reflected type names
    // and other namespace-sensitive output stay identical to the originals.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            // FIRST on purpose: the only section that asserts a fact about process-wide
            // state (ArrayPool<T>.Shared's thread-local bucket), so it sees a pristine pool.
            ArrayPoolSubset.Program.Run();
            ArrayOpsSubset.Program.Run();
            ArrayContainsSubset.Program.Run();
            ArrayElementEqualitySubset.Program.Run();
            ArrayRangeSubset.Program.Run();
            ArrayResizeSubset.Program.Run();
            ArraySortSubset.Program.Run();
            ArraySortGeneralSubset.Program.Run();
            NonGenericArraySortSubset.Program.Run();
            ArrayDataRefSubset.Program.Run();
            ByteArraySubset.Program.Run();
            GetSubArraySubset.Program.Run();
            PackedArraySubset.Program.Run();
            ArrayCollectionSubset.Program.Run();
            EnumArraySubset.Program.Run();
            ArraySortGCPressureSubset.Program.Run();
            ArrayReflectionSubset.Program.Run();
            ArrayAllocThrowSubset.Program.Run();
            ArrayNullFaultSubset.Program.Run();
            ArrayIndexFaultSubset.Program.Run();
            // New sections go at the TAIL: the bucket's previous output must stay a
            // prefix of the new one, or a perturbed earlier section reads as intentional.
            ArrayInterfaceIndexFaultSubset.Program.Run();
            ArrayRangeFaultSubset.Program.Run();
            BufferBlockCopyFaultSubset.Program.Run();
            BufferExtentSubset.Program.Run();
            JaggedMdArraySubset.Program.Run();
            ArrayCopyCompatSubset.Program.Run();
            ArrayDataRefMdSubset.Program.Run();
            InterfaceElementArraySubset.Program.Run();
            NonArrayOperandSubset.Program.Run();
        }
    }
}
