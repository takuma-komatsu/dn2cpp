using System;
using System.Runtime.InteropServices;

// SUBJECT: the marshalled-LAYOUT model, checked against the layout the C compiler actually
// chose — at BOTH pointer widths, since the wasm gate drives this bucket too. Each row makes
// three answers agree: dn2cpp's Marshal.SizeOf/OffsetOf, real .NET's (the gate diffs against
// `dotnet run`), and the native library's own sizeof/offsetof via dn2cpptest_layout_query.
// The third is what no other section adds: a model checked only against a second model can
// agree and both be wrong.
//
// Do NOT prune this as a duplicate of PInvokeMarshalStructSubset. That section asserts the
// VALUES that cross the boundary; this one the layout they cross in, and the two fail
// independently — a struct can marshal every field correctly and still report a size that
// mis-sizes an AllocHGlobal buffer.
//
// The wasm gate diffs a wasm32 subject against a 64-bit host oracle, so a pointer-dependent
// row crosses as a relation against IntPtr.Size or as the bare agreement verdict, never as a
// number. On that axis the emitted tn_<Name> static_asserts check the model per struct at
// 32-bit width before this program runs at all.
namespace PInvokeMarshalLayoutSubset
{
    internal static class Program
    {
        [DllImport("dn2cpptest")]
        private static extern int dn2cpptest_layout_query(int which);

        // Copies of PInvokeMarshalStructSubset's declarations, kept local so the assertion
        // does not depend on another section's fixtures staying put.
        [StructLayout(LayoutKind.Sequential)]
        private struct Person { public int Id; public string Name; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WidePerson { public int Id; public string Name; }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point2 { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct FlagPoint { public bool On; public Point2 Pt; public int Tag; }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rec { public int Id; public double Value; }

        [StructLayout(LayoutKind.Sequential)]
        private struct FixedVec
        {
            public int N;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public int[] Vals;
        }

        // Both numbers are printed, not just the verdict: a verdict-only diff stays green
        // when both sides move together, which is what a model change plus a native struct
        // change looks like.
        private static void Row(string label, long managed, int nativeWhich)
        {
            int nat = dn2cpptest_layout_query(nativeWhich);
            Console.WriteLine($"{label}: managed={managed} native={nat} agree={(managed == nat)}");
        }

        // Both numbers move with the pointer width, their agreement does not — the only form
        // in which a pointer-bearing row can carry the native-ABI comparison across the wasm
        // gate's 64-bit oracle.
        private static void Agree(string label, long managed, int nativeWhich)
        {
            Console.WriteLine($"{label}: agree={(managed == dn2cpptest_layout_query(nativeWhich))}");
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("-- marshalled layout vs the native ABI --");

            // A string field is a pointer, so the int ahead of it is padded to one: the total
            // is two pointers, not 4 + a pointer.
            Console.WriteLine($"Person.size==2ptr {Marshal.SizeOf<Person>() == 2 * IntPtr.Size} "
                + $"Id@0 {Marshal.OffsetOf<Person>("Id").ToInt64() == 0} "
                + $"Name@ptr {Marshal.OffsetOf<Person>("Name").ToInt64() == IntPtr.Size}");
            // CharSet.Unicode moves the ENCODING, not the width — still a pointer.
            Console.WriteLine($"WidePerson.size==2ptr {Marshal.SizeOf<WidePerson>() == 2 * IntPtr.Size} "
                + $"Name@ptr {Marshal.OffsetOf<WidePerson>("Name").ToInt64() == IntPtr.Size}");
            Agree("Person.size", Marshal.SizeOf<Person>(), 0);
            Agree("Person.Id", Marshal.OffsetOf<Person>("Id").ToInt64(), 1);
            Agree("Person.Name", Marshal.OffsetOf<Person>("Name").ToInt64(), 2);
            Agree("WidePerson.size", Marshal.SizeOf<WidePerson>(), 3);
            Agree("WidePerson.Name", Marshal.OffsetOf<WidePerson>("Name").ToInt64(), 4);

            // IntPtr's own marshalled size: the generic form folds at transpile time, the
            // Type form reads the runtime's primitive table. Pinning both to IntPtr.Size
            // reds either one on its own — agreeing with each other is not the claim.
            Console.WriteLine($"IntPtr {Marshal.SizeOf<IntPtr>() == IntPtr.Size} "
                + $"{Marshal.SizeOf(typeof(IntPtr)) == IntPtr.Size} "
                + $"{Marshal.SizeOf<UIntPtr>() == IntPtr.Size} "
                + $"{Marshal.SizeOf(typeof(UIntPtr)) == IntPtr.Size}");

            // On occupies 4 bytes unmanaged, so Pt starts at 4 — which alignment would also
            // produce from a 1-byte bool. Tag at 12 is what distinguishes the two readings.
            Row("FlagPoint.size", Marshal.SizeOf<FlagPoint>(), 5);
            Row("FlagPoint.On", Marshal.OffsetOf<FlagPoint>("On").ToInt64(), 6);
            Row("FlagPoint.Pt", Marshal.OffsetOf<FlagPoint>("Pt").ToInt64(), 7);
            Row("FlagPoint.Tag", Marshal.OffsetOf<FlagPoint>("Tag").ToInt64(), 8);

            // Blittable controls: a widening that moves these moved a number nobody meant to.
            Row("Point2.size", Marshal.SizeOf<Point2>(), 9);
            Row("Rec.size", Marshal.SizeOf<Rec>(), 10);
            Row("Rec.Value", Marshal.OffsetOf<Rec>("Value").ToInt64(), 11);

            // A ByValArray field is four inline ints, not a pointer: the descriptor changes
            // the EXTENT.
            Row("FixedVec.size", Marshal.SizeOf<FixedVec>(), 12);
            Row("FixedVec.Vals", Marshal.OffsetOf<FixedVec>("Vals").ToInt64(), 13);

            // The generic form folds to a compile-time constant while the Type form reads
            // the type-info at run time, so this is the only place they can disagree.
            Console.WriteLine("-- Type-based spelling agrees with the generic one --");
            Console.WriteLine("Person: " + (Marshal.SizeOf(typeof(Person)) == Marshal.SizeOf<Person>()));
            Console.WriteLine("FlagPoint: " + (Marshal.SizeOf(typeof(FlagPoint)) == Marshal.SizeOf<FlagPoint>()));
            Console.WriteLine("FixedVec: " + (Marshal.SizeOf(typeof(FixedVec)) == Marshal.SizeOf<FixedVec>()));
            Console.WriteLine("WidePerson: " + (Marshal.SizeOf(typeof(WidePerson)) == Marshal.SizeOf<WidePerson>()));
            Console.WriteLine("FlagPoint.Pt: "
                + (Marshal.OffsetOf(typeof(FlagPoint), "Pt") == Marshal.OffsetOf<FlagPoint>("Pt")));

            // The size has to be usable, not merely right: write the whole extent and read
            // the field back at its marshalled offset.
            IntPtr buf = Marshal.AllocHGlobal(Marshal.SizeOf<FlagPoint>());
            try
            {
                for (int i = 0; i < Marshal.SizeOf<FlagPoint>(); i++)
                    Marshal.WriteByte(buf, i, 0);
                Marshal.WriteInt32(buf, (int)Marshal.OffsetOf<FlagPoint>("Tag").ToInt64(), 4242);
                Console.WriteLine("tag readback: "
                    + Marshal.ReadInt32(buf, (int)Marshal.OffsetOf<FlagPoint>("Tag").ToInt64()));
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }
    }
}
