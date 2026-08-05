using System;
using System.Runtime.InteropServices;

namespace NativeMemoryOverflowSubset
{
    // NativeMemory.Alloc(elementCount, elementSize) whose PRODUCT does not fit in
    // nuint. The size computation fails before the allocator is asked anything, so
    // the exception object can still be built: real .NET raises a catchable
    // OutOfMemoryException and the program carries on. That "carries on" is the
    // whole subject -- the neighbouring arm, where the allocator itself refuses,
    // deliberately stays an abort, and only continuing past the catch tells the
    // two apart.
    internal static class Program
    {
        internal static unsafe void __GateEntry()
        {
            try
            {
                _ = NativeMemory.Alloc(nuint.MaxValue, 4);
                Console.WriteLine("overflow(max,4): no throw");
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine("overflow(max,4): OutOfMemoryException");
            }
            // The other operand order, and a pair whose operands are both far below
            // the maximum: neither is the MaxValue special case.
            try
            {
                _ = NativeMemory.Alloc(4, nuint.MaxValue);
                Console.WriteLine("overflow(4,max): no throw");
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine("overflow(4,max): OutOfMemoryException");
            }
            try
            {
                _ = NativeMemory.Alloc(nuint.MaxValue / 2 + 1, 2);
                Console.WriteLine("overflow(half+1,2): no throw");
            }
            catch (Exception e)
            {
                Console.WriteLine("overflow(half+1,2): " + e.GetType().Name);
            }
            // Control: the same call with a product that fits still allocates
            // usable memory, so the check above cannot be a blanket refusal.
            void* p = NativeMemory.Alloc(2, 3);
            ((byte*)p)[5] = 42;
            Console.WriteLine("control byte=" + ((byte*)p)[5]);
            NativeMemory.Free(p);
            Console.WriteLine("done-native-memory-overflow");
        }
    }
}
