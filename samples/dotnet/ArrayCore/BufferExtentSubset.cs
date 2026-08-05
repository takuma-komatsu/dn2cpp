#nullable disable
using System;

namespace BufferExtentSubset
{
    // Buffer.ByteLength, and the two Buffer operands whose element kind the RUNTIME
    // cannot answer for, with real .NET as the oracle. Three claims live here
    // and nowhere else.
    //
    // ByteLength returns the same byte extent BlockCopy bounds itself by, so it reads
    // every representation's extent DIRECTLY instead of only as a rejected bound —
    // BufferBlockCopyFaultSubset can only ever see the extent through a refusal, which
    // an off-by-one in the wrong direction still passes.
    //
    // A System.Array-typed operand states no representation at all: the C++ static type
    // is not an array type, so the layout, the element width and the element kind must
    // all come from the runtime type-info. It used to be a transpile-time refusal.
    //
    // And the element verdict is the EMIT site's, not the object header's. An array the
    // C++ runtime allocated carries an imprecise handle whose element type-info is null,
    // so a header-side element test answers "primitive" for anything — which is why a
    // struct[] must be refused from its static type, at every provenance a static type
    // can be lost on the way to the call (a local, an argument, a field, a call return,
    // an inline allocation).
    internal static class Program
    {
        private struct Pair { public byte A; public byte B; }

        private enum E32 { A, B }

        private enum E8 : byte { A, B }

        private enum E64 : long { A, B }

        private static void Show(string what, Func<object> body)
        {
            try
            {
                Console.WriteLine(what + " -> " + body());
            }
            catch (Exception e)
            {
                Console.WriteLine(what + " -> " + e.GetType().Name);
            }
        }

        private static byte[] MakeBytes() => new byte[6];

        private static Pair[] MakePairs() => new Pair[4];

        private static int ByteLengthOfArg(byte[] a) => Buffer.ByteLength(a);

        private static byte[] s_field = new byte[7];

        private static Pair[] s_pairField = new Pair[3];

        internal static void Run()
        {
            // -- the extent of every representation, read directly. The int32-width rep
            //    scales a length by 4, the packed one by a stored width, the MD one by
            //    the product of its lengths; an enum reports its UNDERLYING width, which
            //    is the one place the element type and the storage width disagree. --
            Show("ByteLength(byte[8])", () => Buffer.ByteLength(new byte[8]));
            Show("ByteLength(int[4])", () => Buffer.ByteLength(new int[4]));
            Show("ByteLength(long[2])", () => Buffer.ByteLength(new long[2]));
            Show("ByteLength(double[3])", () => Buffer.ByteLength(new double[3]));
            Show("ByteLength(float[3])", () => Buffer.ByteLength(new float[3]));
            Show("ByteLength(char[4])", () => Buffer.ByteLength(new char[4]));
            Show("ByteLength(bool[3])", () => Buffer.ByteLength(new bool[3]));
            Show("ByteLength(sbyte[3])", () => Buffer.ByteLength(new sbyte[3]));
            Show("ByteLength(ushort[3])", () => Buffer.ByteLength(new ushort[3]));
            Show("ByteLength(int[0])", () => Buffer.ByteLength(new int[0]));
            Show("ByteLength(int[2,3])", () => Buffer.ByteLength(new int[2, 3]));
            Show("ByteLength(byte[2,3,4])", () => Buffer.ByteLength(new byte[2, 3, 4]));
            Show("ByteLength(E32[4])", () => Buffer.ByteLength(new E32[4]));
            Show("ByteLength(E8[5])", () => Buffer.ByteLength(new E8[5]));
            Show("ByteLength(E64[3])", () => Buffer.ByteLength(new E64[3]));
            // IntPtr is primitive to Buffer (its CorElementType is ELEMENT_TYPE_I) even
            // though Type.IsPrimitive is the test nobody here is asking.
            Show("ByteLength(nint[2])", () => Buffer.ByteLength(new nint[2]));

            // -- the refusals, which outrank the extent: null first, then an element
            //    .NET will not blit. decimal and DateTime are the two that look
            //    primitive and are not; a jagged array's element is a reference. --
            Show("ByteLength(null)", () => Buffer.ByteLength(null));
            Show("ByteLength(string[4])", () => Buffer.ByteLength(new string[4]));
            Show("ByteLength(object[4])", () => Buffer.ByteLength(new object[4]));
            Show("ByteLength(Pair[4])", () => Buffer.ByteLength(new Pair[4]));
            Show("ByteLength(decimal[2])", () => Buffer.ByteLength(new decimal[2]));
            Show("ByteLength(DateTime[2])", () => Buffer.ByteLength(new DateTime[2]));
            Show("ByteLength(int?[3])", () => Buffer.ByteLength(new int?[3]));
            Show("ByteLength(int[2][])", () => Buffer.ByteLength(new int[2][]));
            Show("ByteLength(Pair[2,2])", () => Buffer.ByteLength(new Pair[2, 2]));

            // -- the refusal must survive every provenance a static element type can be
            //    lost on: a local, an argument, a static field, a call return, an inline
            //    allocation. A header-side element test passes all five while a
            //    runtime-allocated array is in play, because such an array names no
            //    element type at all. --
            Pair[] localPairs = new Pair[2];
            byte[] localBytes = new byte[5];
            Show("ByteLength(local Pair[2])", () => Buffer.ByteLength(localPairs));
            Show("ByteLength(field Pair[3])", () => Buffer.ByteLength(s_pairField));
            Show("ByteLength(call Pair[4])", () => Buffer.ByteLength(MakePairs()));
            Show("ByteLength(local byte[5])", () => Buffer.ByteLength(localBytes));
            Show("ByteLength(field byte[7])", () => Buffer.ByteLength(s_field));
            Show("ByteLength(call byte[6])", () => Buffer.ByteLength(MakeBytes()));
            Show("ByteLength(arg byte[5])", () => ByteLengthOfArg(localBytes));
            Show("BlockCopy(local Pair[2] -> byte)", () =>
            {
                Buffer.BlockCopy(localPairs, 0, localBytes, 0, 4);
                return "no throw";
            });
            Show("BlockCopy(call Pair[4] -> byte)", () =>
            {
                Buffer.BlockCopy(MakePairs(), 0, localBytes, 0, 4);
                return "no throw";
            });
            Show("BlockCopy(byte -> field Pair[3])", () =>
            {
                Buffer.BlockCopy(localBytes, 0, s_pairField, 0, 4);
                return "no throw";
            });
            Console.WriteLine("after element refusals: bytes=" + localBytes[0] + "," + localBytes[4]
                              + " pairs=" + s_pairField[0].A + ":" + s_pairField[0].B);

            // -- the System.Array-typed operand, which states no representation: the
            //    layout, the element width and the element kind all come from the
            //    runtime type-info. Driven at every representation, on both sides of
            //    BlockCopy and mixed against a typed operand, because the two sides are
            //    classified independently. --
            Array aI4 = new int[] { 0x01020304, 0x05060708 };
            Array aN = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            Array aMd = new int[2, 3];
            Array aRef = new string[] { "a", "b" };
            Array aEnum = new E32[] { E32.B, E32.A };
            Show("ByteLength(Array int[2])", () => Buffer.ByteLength(aI4));
            Show("ByteLength(Array byte[8])", () => Buffer.ByteLength(aN));
            Show("ByteLength(Array int[2,3])", () => Buffer.ByteLength(aMd));
            Show("ByteLength(Array E32[2])", () => Buffer.ByteLength(aEnum));
            Show("ByteLength(Array string[2])", () => Buffer.ByteLength(aRef));

            Array dI4 = new int[2];
            Array dN = new byte[8];
            int[] typedI4 = { -1, -1 };
            Show("BlockCopy(Array i4 -> Array i4, 8)", () =>
            {
                Buffer.BlockCopy(aI4, 0, dI4, 0, 8);
                return ((int[])dI4)[0] + "," + ((int[])dI4)[1];
            });
            Show("BlockCopy(Array i4 -> typed i4, 8)", () =>
            {
                Buffer.BlockCopy(aI4, 0, typedI4, 0, 8);
                return typedI4[0] + "," + typedI4[1];
            });
            Show("BlockCopy(Array n -> Array n, 8)", () =>
            {
                Buffer.BlockCopy(aN, 0, dN, 0, 8);
                return ((byte[])dN)[0] + "," + ((byte[])dN)[7];
            });
            Show("BlockCopy(Array i4 -> Array md, 8)", () =>
            {
                Buffer.BlockCopy(aI4, 0, aMd, 0, 8);
                return ((int[,])aMd)[0, 0] + "," + ((int[,])aMd)[0, 1] + "," + ((int[,])aMd)[0, 2];
            });
            // The bound is still the BYTE extent, and it is still the one the runtime
            // type-info states rather than the one the static type would have.
            Show("BlockCopy(Array i4 -> Array n, 9)", () => { Buffer.BlockCopy(aI4, 0, dN, 0, 9); return "no throw"; });
            Show("BlockCopy(Array i4 -> Array n, 8)", () =>
            {
                Buffer.BlockCopy(aI4, 0, dN, 0, 8);
                return ((byte[])dN)[0] + "," + ((byte[])dN)[7];
            });
            // The MD source's own extent (2*3*4 = 24), off a destination wide enough
            // that the refusal can only be the source's.
            byte[] wide = new byte[32];
            Show("BlockCopy(Array md -> byte[32], 25)", () => { Buffer.BlockCopy(aMd, 0, wide, 0, 25); return "no throw"; });
            Show("BlockCopy(Array md -> byte[32], 24)", () =>
            {
                Buffer.BlockCopy(aMd, 0, wide, 0, 24);
                return wide[0] + "," + wide[4] + "," + wide[23];
            });
            Show("BlockCopy(Array ref -> Array n, 4)", () => { Buffer.BlockCopy(aRef, 0, dN, 0, 4); return "no throw"; });
            Show("BlockCopy(Array n -> Array ref, 4)", () => { Buffer.BlockCopy(aN, 0, aRef, 0, 4); return "no throw"; });
            Show("BlockCopy(Array n, -1)", () => { Buffer.BlockCopy(aN, 0, dN, 0, -1); return "no throw"; });
            Array aNull = null;
            Show("BlockCopy(Array null src)", () => { Buffer.BlockCopy(aNull, 0, dN, 0, 1); return "no throw"; });
            Show("BlockCopy(Array null dst)", () => { Buffer.BlockCopy(aN, 0, aNull, 0, 1); return "no throw"; });
            Console.WriteLine("after Array-typed ops: dN=" + ((byte[])dN)[0] + "," + ((byte[])dN)[7]
                              + " aRef=" + ((string[])aRef)[0] + "," + ((string[])aRef)[1]);
            GetSetByte();
        }

        // Buffer.GetByte / SetByte — the SINGLE-byte half of the same extent, which
        // nothing else here reaches. Two claims are only visible from this half:
        //
        // The extent is read POSITIONALLY, not merely as a bound. ByteLength above proves the
        // total; only an indexed read proves the base address and the element packing under it,
        // which differ per representation (I4 stores 4-byte slots, N a stored width, MD a flat
        // block) — an offset error inside a correct total is invisible to every row above.
        //
        // And the refusals outrank the index, because .NET's own bound IS ByteLength(array):
        // a struct[] is refused with ArgumentException at an index that is perfectly in range,
        // and a null one with ArgumentNullException. So the provenance rows are repeated here —
        // an emit site that lost the static element type would answer a byte instead. Real
        // .NET is the oracle throughout; the byte values assume a little-endian host, as the
        // BlockCopy rows above already do.
        private static void GetSetByte()
        {
            byte[] gN = { 10, 20, 30, 40 };
            int[] gI4 = { 0x01020304, 0x05060708 };
            long[] gI8 = { 0x0102030405060708L };
            E32[] gE32 = { (E32)0x11223344 };
            E8[] gE8 = { (E8)7, (E8)8 };
            E64[] gE64 = { (E64)0x1122334455667788L };
            int[,] gMd = { { 0x01020304, 0x05060708 }, { 0x090A0B0C, 0x0D0E0F10 } };

            // -- an indexed read at every representation, at both ends of the extent. --
            Show("GetByte(byte[4],0)", () => Buffer.GetByte(gN, 0));
            Show("GetByte(byte[4],3)", () => Buffer.GetByte(gN, 3));
            Show("GetByte(int[2],0)", () => Buffer.GetByte(gI4, 0));
            Show("GetByte(int[2],3)", () => Buffer.GetByte(gI4, 3));
            Show("GetByte(int[2],4)", () => Buffer.GetByte(gI4, 4));
            Show("GetByte(int[2],7)", () => Buffer.GetByte(gI4, 7));
            Show("GetByte(long[1],0)", () => Buffer.GetByte(gI8, 0));
            Show("GetByte(long[1],7)", () => Buffer.GetByte(gI8, 7));
            Show("GetByte(E32[1],0)", () => Buffer.GetByte(gE32, 0));
            Show("GetByte(E32[1],3)", () => Buffer.GetByte(gE32, 3));
            Show("GetByte(E8[2],1)", () => Buffer.GetByte(gE8, 1));
            Show("GetByte(E64[1],7)", () => Buffer.GetByte(gE64, 7));
            Show("GetByte(int[2,2],0)", () => Buffer.GetByte(gMd, 0));
            Show("GetByte(int[2,2],8)", () => Buffer.GetByte(gMd, 8));
            Show("GetByte(int[2,2],15)", () => Buffer.GetByte(gMd, 15));

            // -- the index bound is the BYTE extent, and a negative index arrives at it as a
            //    huge unsigned rather than as a separate test. --
            Show("GetByte(byte[4],4)", () => Buffer.GetByte(gN, 4));
            Show("GetByte(byte[4],-1)", () => Buffer.GetByte(gN, -1));
            Show("GetByte(int[2],8)", () => Buffer.GetByte(gI4, 8));
            Show("GetByte(int[2],int.MinValue)", () => Buffer.GetByte(gI4, int.MinValue));
            Show("GetByte(int[2,2],16)", () => Buffer.GetByte(gMd, 16));
            Show("GetByte(int[0],0)", () => Buffer.GetByte(new int[0], 0));

            // -- the refusals, which outrank the index: each is asked at an index the extent
            //    would have accepted, so an answer here is a wrong answer and not a wrong
            //    diagnostic. The provenances repeat the ByteLength set, because Get/SetByte take
            //    the element verdict from the operand's static type the same way. --
            Show("GetByte(null,0)", () => Buffer.GetByte(null, 0));
            Show("GetByte(string[4],0)", () => Buffer.GetByte(new string[4], 0));
            Show("GetByte(object[4],0)", () => Buffer.GetByte(new object[4], 0));
            Show("GetByte(Pair[4],0)", () => Buffer.GetByte(new Pair[4], 0));
            Show("GetByte(decimal[2],0)", () => Buffer.GetByte(new decimal[2], 0));
            Show("GetByte(DateTime[2],0)", () => Buffer.GetByte(new DateTime[2], 0));
            Show("GetByte(int?[3],0)", () => Buffer.GetByte(new int?[3], 0));
            Show("GetByte(int[2][],0)", () => Buffer.GetByte(new int[2][], 0));
            Show("GetByte(Pair[2,2],0)", () => Buffer.GetByte(new Pair[2, 2], 0));
            Pair[] localPairs2 = new Pair[2];
            Show("GetByte(local Pair[2],0)", () => Buffer.GetByte(localPairs2, 0));
            Show("GetByte(field Pair[3],0)", () => Buffer.GetByte(s_pairField, 0));
            Show("GetByte(call Pair[4],0)", () => Buffer.GetByte(MakePairs(), 0));
            Show("GetByte(call byte[6],5)", () => Buffer.GetByte(MakeBytes(), 5));

            // -- SetByte writes one byte and leaves its neighbours alone, at every
            //    representation. Read back through the TYPED array, so a write that landed at
            //    the right offset of the wrong layout still fails. --
            Show("SetByte(byte[4],1,99)", () => { Buffer.SetByte(gN, 1, 99); return gN[0] + "," + gN[1] + "," + gN[2]; });
            Show("SetByte(int[2],0,0xFF)", () => { Buffer.SetByte(gI4, 0, 0xFF); return gI4[0].ToString("X8") + "," + gI4[1].ToString("X8"); });
            Show("SetByte(int[2],7,0xFF)", () => { Buffer.SetByte(gI4, 7, 0xFF); return gI4[0].ToString("X8") + "," + gI4[1].ToString("X8"); });
            Show("SetByte(long[1],3,0xAB)", () => { Buffer.SetByte(gI8, 3, 0xAB); return gI8[0].ToString("X16"); });
            Show("SetByte(E32[1],1,0xEE)", () => { Buffer.SetByte(gE32, 1, 0xEE); return ((int)gE32[0]).ToString("X8"); });
            Show("SetByte(E8[2],0,0x5A)", () => { Buffer.SetByte(gE8, 0, 0x5A); return ((byte)gE8[0]) + "," + ((byte)gE8[1]); });
            Show("SetByte(int[2,2],4,0x7F)", () => { Buffer.SetByte(gMd, 4, 0x7F); return gMd[0, 0].ToString("X8") + "," + gMd[0, 1].ToString("X8"); });

            // -- a refused SetByte must write NOTHING, whichever refusal fires. --
            Show("SetByte(byte[4],4,1)", () => { Buffer.SetByte(gN, 4, 1); return "no throw"; });
            Show("SetByte(byte[4],-1,1)", () => { Buffer.SetByte(gN, -1, 1); return "no throw"; });
            Show("SetByte(null,0,1)", () => { Buffer.SetByte(null, 0, 1); return "no throw"; });
            Show("SetByte(local Pair[2],0,1)", () => { Buffer.SetByte(localPairs2, 0, 1); return "no throw"; });
            Show("SetByte(field Pair[3],0,1)", () => { Buffer.SetByte(s_pairField, 0, 1); return "no throw"; });
            Show("SetByte(string[2],0,1)", () => { Buffer.SetByte(new string[2], 0, 1); return "no throw"; });
            Console.WriteLine("after refused SetByte: gN=" + gN[0] + "," + gN[1] + "," + gN[2] + "," + gN[3]
                              + " pairs=" + localPairs2[0].A + ":" + localPairs2[0].B
                              + " field=" + s_pairField[0].A + ":" + s_pairField[0].B);

            // -- the System.Array-typed operand again, where nothing but the runtime type-info
            //    states the layout: an indexed read/write is the one thing that can catch a
            //    correct extent over the wrong base. --
            Array bI4 = new int[] { 0x01020304, 0x05060708 };
            Array bN = new byte[] { 1, 2, 3, 4 };
            Array bMd = new int[2, 2];
            Array bRef = new string[2];
            Array bNull = null;
            Show("GetByte(Array int[2],1)", () => Buffer.GetByte(bI4, 1));
            Show("GetByte(Array int[2],6)", () => Buffer.GetByte(bI4, 6));
            Show("GetByte(Array byte[4],3)", () => Buffer.GetByte(bN, 3));
            Show("GetByte(Array byte[4],4)", () => Buffer.GetByte(bN, 4));
            Show("GetByte(Array string[2],0)", () => Buffer.GetByte(bRef, 0));
            Show("GetByte(Array null,0)", () => Buffer.GetByte(bNull, 0));
            Show("SetByte(Array int[2],2,0xC3)", () => { Buffer.SetByte(bI4, 2, 0xC3); return ((int[])bI4)[0].ToString("X8"); });
            Show("SetByte(Array byte[4],0,0x42)", () => { Buffer.SetByte(bN, 0, 0x42); return ((byte[])bN)[0] + "," + ((byte[])bN)[1]; });
            Show("SetByte(Array int[2,2],5,0x9)", () => { Buffer.SetByte(bMd, 5, 0x9); return ((int[,])bMd)[0, 1].ToString("X8"); });
            Show("SetByte(Array string[2],0,1)", () => { Buffer.SetByte(bRef, 0, 1); return "no throw"; });
            Show("SetByte(Array int[2],8,1)", () => { Buffer.SetByte(bI4, 8, 1); return "no throw"; });
            Console.WriteLine("after Array-typed byte ops: bI4=" + ((int[])bI4)[0].ToString("X8")
                              + " bN=" + ((byte[])bN)[0] + " bMd=" + ((int[,])bMd)[0, 1].ToString("X8")
                              + " bRef=" + (((string[])bRef)[0] ?? "null"));
        }
    }
}
