#nullable disable
using System;

namespace BufferBlockCopyFaultSubset
{
    // Buffer.BlockCopy's validation, observed from a catch handler with real .NET
    // as the oracle. BlockCopy lowers to ONE memmove off the two arrays'
    // element bases, so an unchecked offset or count is an out-of-bounds write
    // over neighbouring heap objects — memory safety, and silent. As in
    // ArrayRangeFaultSubset, the payload lines after each rejected call carry the
    // assert: a run whose exception names all matched while a rejected call had
    // already moved bytes would still be the bug.
    //
    // Two things here are BlockCopy's alone and are asserted nowhere else. Its
    // offsets and count are BYTES, not elements, so the bound is the array's byte
    // extent — a number none of the four representations states the same way
    // (Dn2CppArrayI4 scales its length by 4, Dn2CppArrayN by a stored elemSize, an
    // MD array by the product of its lengths). And it REFUSES an operand whose
    // elements are not primitive, which no other block move does: a string[] or
    // struct[] blit would move bytes across GC-visible fields. Both operands are
    // driven at every representation for that reason.
    //
    // The order the refusals are asked in is .NET's and it is observable: a null
    // destination outranks a non-primitive source, and a non-primitive source
    // outranks a negative count, so each pair is probed as a pair and not only
    // apart.
    internal static class Program
    {
        private struct Pair { public byte A; public byte B; }

        private enum E32 { A, B }

        private static void Catches(string what, Action body)
        {
            try
            {
                body();
                Console.WriteLine(what + " -> no throw");
            }
            catch (Exception e)
            {
                Console.WriteLine(what + " -> " + e.GetType().Name);
            }
        }

        private static string Dump(byte[] a)
        {
            string s = "";
            for (int i = 0; i < a.Length; i++)
                s += (i == 0 ? "" : ",") + a[i];
            return s;
        }

        private static string Dump(int[] a)
        {
            string s = "";
            for (int i = 0; i < a.Length; i++)
                s += (i == 0 ? "" : ",") + a[i];
            return s;
        }

        private static string DumpMd(int[,] a)
        {
            string s = "";
            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < a.GetLength(1); j++)
                    s += (s.Length == 0 ? "" : ",") + a[i, j];
            return s;
        }

        internal static void Run()
        {
            byte[] sb = { 1, 2, 3, 4, 5, 6, 7, 8 };
            byte[] db = { 90, 91, 92, 93, 94, 95, 96, 97 };

            // -- null operands, which outrank every other refusal. A typed null
            //    keeps the operand's representation, which is what the lowering
            //    dispatches on; an Array-typed one carries none and is refused at
            //    transpile time instead. --
            byte[] nb = null;
            Catches("BlockCopy(null,0,db,0,1)", () => Buffer.BlockCopy(nb, 0, db, 0, 1));
            Catches("BlockCopy(sb,0,null,0,1)", () => Buffer.BlockCopy(sb, 0, nb, 0, 1));
            Catches("BlockCopy(null,0,null,0,1)", () => Buffer.BlockCopy(nb, 0, nb, 0, 1));
            Catches("BlockCopy(null,-1,db,0,-1)", () => Buffer.BlockCopy(nb, -1, db, 0, -1));
            Catches("BlockCopy(null,0,db,0,99999)", () => Buffer.BlockCopy(nb, 0, db, 0, 99999));
            Console.WriteLine("after null faults: db=" + Dump(db));

            // -- negative operands, which outrank the overrun test. The order the
            //    three are asked in is invisible from the exception type (all
            //    three are the same family), so they are driven together as well. --
            Catches("BlockCopy(sb,-1,db,0,1)", () => Buffer.BlockCopy(sb, -1, db, 0, 1));
            Catches("BlockCopy(sb,0,db,-1,1)", () => Buffer.BlockCopy(sb, 0, db, -1, 1));
            Catches("BlockCopy(sb,0,db,0,-1)", () => Buffer.BlockCopy(sb, 0, db, 0, -1));
            Catches("BlockCopy(sb,-1,db,-1,-1)", () => Buffer.BlockCopy(sb, -1, db, -1, -1));
            Catches("BlockCopy(sb,0,db,-1,99)", () => Buffer.BlockCopy(sb, 0, db, -1, 99));
            Console.WriteLine("after negative faults: db=" + Dump(db));

            // -- the overrun family, the one the bug was: 999999 bytes off an
            //    8-byte array. int32 `offset + count` wraps back under the extent,
            //    so the check has to be wider than the operands it compares. --
            Catches("BlockCopy(sb,0,db,0,999999)", () => Buffer.BlockCopy(sb, 0, db, 0, 999999));
            Catches("BlockCopy(sb,0,db,0,9)", () => Buffer.BlockCopy(sb, 0, db, 0, 9));
            Catches("BlockCopy(sb,9,db,0,0)", () => Buffer.BlockCopy(sb, 9, db, 0, 0));
            Catches("BlockCopy(sb,0,db,9,0)", () => Buffer.BlockCopy(sb, 0, db, 9, 0));
            Catches("BlockCopy(sb,1,db,0,int.Max)", () => Buffer.BlockCopy(sb, 1, db, 0, int.MaxValue));
            Catches("BlockCopy(sb,0,db,1,int.Max)", () => Buffer.BlockCopy(sb, 0, db, 1, int.MaxValue));
            Catches("BlockCopy(sb,int.Max,db,0,1)", () => Buffer.BlockCopy(sb, int.MaxValue, db, 0, 1));
            Console.WriteLine("after overrun faults: db=" + Dump(db));

            // -- the boundaries that must NOT throw: an offset AT the byte extent
            //    with a zero count, and a count covering the whole array. --
            Catches("BlockCopy(sb,8,db,0,0)", () => Buffer.BlockCopy(sb, 8, db, 0, 0));
            Catches("BlockCopy(sb,0,db,8,0)", () => Buffer.BlockCopy(sb, 0, db, 8, 0));
            Catches("BlockCopy(sb,0,db,0,8)", () => Buffer.BlockCopy(sb, 0, db, 0, 8));
            Console.WriteLine("after byte edges: db=" + Dump(db));

            // -- BYTE semantics, not element semantics: a 4-element int[] takes a
            //    count of 16 and refuses 17, and a count of 8 off it fills exactly
            //    two elements of the destination. --
            int[] si = { 0x01020304, 0x05060708, 0x090A0B0C, 0x0D0E0F10 };
            int[] di = { -1, -1, -1, -1 };
            Catches("BlockCopy(si,0,di,0,16)", () => Buffer.BlockCopy(si, 0, di, 0, 16));
            Console.WriteLine("i4 16 bytes: di=" + Dump(di));
            di[0] = -1; di[1] = -1; di[2] = -1; di[3] = -1;
            Catches("BlockCopy(si,0,di,0,17)", () => Buffer.BlockCopy(si, 0, di, 0, 17));
            Catches("BlockCopy(si,0,di,0,8)", () => Buffer.BlockCopy(si, 0, di, 0, 8));
            Console.WriteLine("i4 8 bytes: di=" + Dump(di));
            Catches("BlockCopy(si,16,di,0,1)", () => Buffer.BlockCopy(si, 16, di, 0, 1));
            Catches("BlockCopy(si,0,db,0,8)", () => Buffer.BlockCopy(si, 0, db, 0, 8));
            Console.WriteLine("i4 to byte: db=" + Dump(db));

            // -- the element-sized representation reads its width from the array
            //    rather than from the type, so its extent is asserted apart: a
            //    3-element double[] is 24 bytes and refuses 25. --
            double[] sd = { 1.0, 2.0, 3.0 };
            double[] dd = { 0.0, 0.0, 0.0 };
            Catches("BlockCopy(sd,0,dd,0,24)", () => Buffer.BlockCopy(sd, 0, dd, 0, 24));
            Console.WriteLine("double 24 bytes: " + dd[0] + "," + dd[1] + "," + dd[2]);
            Catches("BlockCopy(sd,0,dd,0,25)", () => Buffer.BlockCopy(sd, 0, dd, 0, 25));
            char[] sc = { 'a', 'b', 'c', 'd' };
            char[] dc = { 'z', 'z', 'z', 'z' };
            Catches("BlockCopy(sc,0,dc,0,8)", () => Buffer.BlockCopy(sc, 0, dc, 0, 8));
            Console.WriteLine("char 8 bytes: " + new string(dc));
            Catches("BlockCopy(sc,0,dc,0,9)", () => Buffer.BlockCopy(sc, 0, dc, 0, 9));

            // -- the primitive-element refusal, at every representation that can
            //    carry a non-primitive element. An enum is primitive here because
            //    .NET asks the element's CorElementType, not Type.IsPrimitive. --
            string[] sr = { "a", "b", "c", "d" };
            string[] dr = { "w", "x", "y", "z" };
            Pair[] sp = new Pair[4];
            Pair[] dp = new Pair[4];
            for (int i = 0; i < 4; i++)
            {
                sp[i].A = (byte)(i + 1);
                sp[i].B = (byte)(i + 5);
                dp[i].A = 200;
                dp[i].B = 201;
            }
            Catches("BlockCopy(sr,0,db,0,4)", () => Buffer.BlockCopy(sr, 0, db, 0, 4));
            Catches("BlockCopy(sb,0,dr,0,4)", () => Buffer.BlockCopy(sb, 0, dr, 0, 4));
            Catches("BlockCopy(sr,0,dr,0,4)", () => Buffer.BlockCopy(sr, 0, dr, 0, 4));
            Catches("BlockCopy(sp,0,db,0,4)", () => Buffer.BlockCopy(sp, 0, db, 0, 4));
            Catches("BlockCopy(sb,0,dp,0,4)", () => Buffer.BlockCopy(sb, 0, dp, 0, 4));
            Console.WriteLine("after primitive faults: dr=" + dr[0] + "," + dr[3]
                              + " dp=" + dp[0].A + ":" + dp[0].B + " db=" + Dump(db));
            // The two orderings a single-refusal probe cannot see.
            Catches("BlockCopy(sr,0,null,0,4)", () => Buffer.BlockCopy(sr, 0, nb, 0, 4));
            Catches("BlockCopy(sr,0,db,0,-1)", () => Buffer.BlockCopy(sr, 0, db, 0, -1));
            Catches("BlockCopy(sr,0,db,0,99999)", () => Buffer.BlockCopy(sr, 0, db, 0, 99999));
            // An enum element is allowed, and at its underlying width.
            E32[] se = { E32.B, E32.A, E32.B, E32.A };
            E32[] de = { E32.A, E32.A, E32.A, E32.A };
            Catches("BlockCopy(se,0,de,0,16)", () => Buffer.BlockCopy(se, 0, de, 0, 16));
            Console.WriteLine("enum 16 bytes: " + de[0] + "," + de[1] + "," + de[3]);
            Catches("BlockCopy(se,0,de,0,17)", () => Buffer.BlockCopy(se, 0, de, 0, 17));

            // -- rank >= 2, which real .NET allows and treats as one flat byte
            //    block: an MD array's extent is the product of its lengths and its
            //    elements live in a separately allocated block, so both the bound
            //    and the base come from a header that shares no field with the SZ
            //    one (the word an SZArray reads as its length is the MD rank). --
            int[,] md = new int[2, 3];
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 3; j++)
                    md[i, j] = i * 3 + j + 1;
            int[,] md2 = new int[2, 3];
            int[] flat = { 0, 0, 0, 0, 0, 0 };
            Catches("BlockCopy(md,0,md2,0,25)", () => Buffer.BlockCopy(md, 0, md2, 0, 25));
            Console.WriteLine("md after overrun: " + DumpMd(md2));
            Catches("BlockCopy(md,0,md2,0,24)", () => Buffer.BlockCopy(md, 0, md2, 0, 24));
            Console.WriteLine("md flat copy: " + DumpMd(md2));
            Catches("BlockCopy(md,0,flat,0,24)", () => Buffer.BlockCopy(md, 0, flat, 0, 24));
            Console.WriteLine("md to sz: " + Dump(flat));
            Catches("BlockCopy(flat,0,md2,4,20)", () => Buffer.BlockCopy(flat, 0, md2, 4, 20));
            Console.WriteLine("sz to md: " + DumpMd(md2));
            Catches("BlockCopy(md,-1,md2,0,4)", () => Buffer.BlockCopy(md, -1, md2, 0, 4));

            // -- overlap: memmove's semantics are BlockCopy's documented ones, so a
            //    forward-overlapping move must not smear its own source. --
            byte[] ov = { 1, 2, 3, 4, 5, 6, 7, 8 };
            Catches("BlockCopy(ov,0,ov,2,6)", () => Buffer.BlockCopy(ov, 0, ov, 2, 6));
            Console.WriteLine("overlap: " + Dump(ov));
        }
    }
}
