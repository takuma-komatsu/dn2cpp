#nullable disable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ArrayDataRefMdSubset
{
    // The non-generic MemoryMarshal.GetArrayDataReference(Array) over an operand whose
    // RANK the call site does not state. It lowers to dn2cpp_pinned_data_addr, which
    // recovers the representation from the runtime type-info; an MD array shares no
    // header field with the SZ reps and keeps its elements in a detached block, so
    // reading one as an SZArray silently hands back a pointer INTO the header.
    //
    // The dumps are positional on purpose — a wrong BASE and a wrong element STRIDE are
    // different failures and a total would hide both. The rank-1 rows are the control:
    // the rank test sits in front of the rep discrimination, so an SZ regression lands
    // here rather than in SpanOps, whose rows never reach this arm.
    //
    // The tail reaches the same helper through REAL CoreLib BODIES:
    // GCHandle.AddrOfPinnedObject and Marshal.UnsafeAddrOfPinnedArrayElement call the
    // non-generic overload intra-CoreLib, i.e. on the MethodDefinition asker route rather
    // than the MemberReference one every other caller uses. That second method scales its
    // index by RuntimeHelpers.GetElementSize, making its non-zero indices the tree's only
    // reader of dn2cpp_array_element_size.
    internal struct Cell
    {
        internal int A;
        internal int B;
    }

    internal enum Tag
    {
        Zero = 0,
        One = 1,
        Two = 2,
    }

    internal static class Program
    {
        // The array's first `count` 4-byte words off its data reference, as one line.
        private static void DumpInts(string label, Array a, int count)
        {
            ref byte r = ref MemoryMarshal.GetArrayDataReference(a);
            string s = label + ":";
            for (int i = 0; i < count; i++)
                s += " " + Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref r, i * 4));
            Console.WriteLine(s);
        }

        internal static void Run()
        {
            Console.WriteLine("-- MD data reference --");
            int[,] i2 = new int[2, 3];
            i2[0, 0] = 11; i2[0, 1] = 22; i2[0, 2] = 33;
            i2[1, 0] = 44; i2[1, 1] = 55; i2[1, 2] = 66;
            DumpInts("int[2,3]", i2, 6);

            int[,,] i3 = new int[2, 2, 2];
            i3[0, 0, 0] = 1; i3[0, 0, 1] = 2; i3[1, 1, 1] = 8;
            DumpInts("int[2,2,2]", i3, 8);

            // A 4-byte-underlying enum element: the I4 arm the rank test now precedes.
            Tag[,] tags = new Tag[2, 2];
            tags[0, 0] = Tag.Two; tags[1, 1] = Tag.One;
            DumpInts("Tag[2,2]", tags, 4);

            byte[,] b2 = new byte[2, 3];
            b2[0, 0] = 7; b2[1, 2] = 9;
            ref byte br = ref MemoryMarshal.GetArrayDataReference(b2);
            Console.WriteLine("byte[2,3] first=" + br + " last=" + Unsafe.Add(ref br, 5));

            long[,] l2 = new long[2, 2];
            l2[0, 0] = 100L; l2[1, 1] = 400L;
            ref byte lr = ref MemoryMarshal.GetArrayDataReference(l2);
            Console.WriteLine("long[2,2] first=" + Unsafe.ReadUnaligned<long>(ref lr)
                + " last=" + Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref lr, 24)));

            Cell[,] cells = new Cell[2, 2];
            cells[0, 0].A = 1; cells[0, 0].B = 2; cells[1, 1].A = 7; cells[1, 1].B = 8;
            DumpInts("Cell[2,2]", cells, 8);

            string[,] s2 = new string[2, 2];
            s2[0, 0] = "aa"; s2[1, 1] = "dd";
            ref byte sr = ref MemoryMarshal.GetArrayDataReference(s2);
            Console.WriteLine("string[2,2] first=" + Unsafe.As<byte, string>(ref sr)
                + " last=" + Unsafe.Add(ref Unsafe.As<byte, string>(ref sr), 3));

            Console.WriteLine("-- the ref is the payload, not a copy --");
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(i2), 12), 4242);
            Console.WriteLine("i2[1,0] after write = " + i2[1, 0]);

            Console.WriteLine("-- rank 1 still answers at its own rep --");
            DumpInts("int[]", new int[] { 91, 92, 93 }, 3);
            Cell[] szCells = new Cell[2];
            szCells[0].A = 5; szCells[0].B = 6; szCells[1].A = 7; szCells[1].B = 8;
            DumpInts("Cell[]", szCells, 4);
            string[] szStr = { "x", "y" };
            ref byte szr = ref MemoryMarshal.GetArrayDataReference((Array)szStr);
            Console.WriteLine("string[] first=" + Unsafe.As<byte, string>(ref szr));

            // .NET returns the ref unconditionally — there is no length test in it.
            Console.WriteLine("-- empty arrays answer without throwing --");
            Console.WriteLine("empty int[0,0] nullref=" + Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(new int[0, 0])));
            Console.WriteLine("empty int[0] nullref=" + Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(new int[0])));

            Console.WriteLine("-- through the CoreLib bodies that call it --");
            GCHandle hi = GCHandle.Alloc(i2, GCHandleType.Pinned);
            Console.WriteLine("i2 AddrOfPinnedObject[0] = " + Marshal.ReadInt32(hi.AddrOfPinnedObject()));
            Console.WriteLine("i2 UnsafeAddrOf[5] = " + Marshal.ReadInt32(Marshal.UnsafeAddrOfPinnedArrayElement(i2, 5)));
            hi.Free();
            long[] szLong = { 10L, 20L, 30L };
            GCHandle hl = GCHandle.Alloc(szLong, GCHandleType.Pinned);
            Console.WriteLine("long[] UnsafeAddrOf[2] = " + Marshal.ReadInt64(Marshal.UnsafeAddrOfPinnedArrayElement((Array)szLong, 2)));
            // The generic overload beside it: a different lowering (Unsafe.SizeOf<T>)
            // reaching the same address, so a stride bug shows as a disagreement.
            Console.WriteLine("long[] UnsafeAddrOf<T>[2] = " + Marshal.ReadInt64(Marshal.UnsafeAddrOfPinnedArrayElement(szLong, 2)));
            hl.Free();
            byte[] szByte = { 1, 2, 3, 4 };
            GCHandle hb = GCHandle.Alloc(szByte, GCHandleType.Pinned);
            Console.WriteLine("byte[] UnsafeAddrOf[3] = " + Marshal.ReadByte(Marshal.UnsafeAddrOfPinnedArrayElement((Array)szByte, 3)));
            hb.Free();
            GCHandle hc = GCHandle.Alloc(szCells, GCHandleType.Pinned);
            Console.WriteLine("Cell[] UnsafeAddrOf[1].A = " + Marshal.ReadInt32(Marshal.UnsafeAddrOfPinnedArrayElement((Array)szCells, 1)));
            hc.Free();
        }
    }
}
