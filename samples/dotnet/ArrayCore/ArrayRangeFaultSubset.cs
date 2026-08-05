#nullable disable
using System;

namespace ArrayRangeFaultSubset
{
    // The RANGE validation of the array block-move lowerings, observed from a catch
    // handler with real .NET as the oracle. Array.Copy/Clear/CopyTo each lower to ONE
    // memmove/memset off the element pointers, so an unchecked out-of-range index is a
    // silent write over neighbouring heap objects. The payload lines printed after each
    // rejected call are therefore as much of the assert as the exception names: matching
    // names over a rejected Copy that had already moved bytes would still be the bug.
    //
    // Every element representation is probed, because the check sits in a different place
    // for each: int[] (Dn2CppArrayI4), string[] (Dn2CppArrayRef) and a 2-byte struct
    // (Dn2CppArrayN) reach the emitter's inline arms in MethodCompiler.Arithmetic.cs,
    // System.Array-typed operands reach dn2cpp_array_copy_dyn / _clear_dyn, and a rank-2
    // receiver reaches the latter's MD arm, whose length is a product of the
    // per-dimension lengths rather than a header field.
    //
    // The exception families are .NET's and do not agree with one another: Copy answers a
    // negative operand with ArgumentOutOfRangeException and an overrun with
    // ArgumentException, Clear answers both with IndexOutOfRangeException, and a null
    // operand outranks either.
    internal static class Program
    {
        private struct Pair { public byte A; public byte B; }

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

        private static string Dump(int[] a)
        {
            string s = "";
            for (int i = 0; i < a.Length; i++)
                s += (i == 0 ? "" : ",") + a[i];
            return s;
        }

        private static string Dump(string[] a)
        {
            string s = "";
            for (int i = 0; i < a.Length; i++)
                s += (i == 0 ? "" : ",") + (a[i] ?? "-");
            return s;
        }

        // Row-major, which is both the MD data block's element order and .NET's
        // enumeration order — so a flat Copy is observable as a flat dump.
        private static string DumpMd(int[,] a)
        {
            string s = "";
            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < a.GetLength(1); j++)
                    s += (s.Length == 0 ? "" : ",") + a[i, j];
            return s;
        }

        private static string DumpMd3(int[,,] a)
        {
            string s = "";
            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < a.GetLength(1); j++)
                    for (int k = 0; k < a.GetLength(2); k++)
                        s += (s.Length == 0 ? "" : ",") + a[i, j, k];
            return s;
        }

        private static string Dump(Pair[] a)
        {
            string s = "";
            for (int i = 0; i < a.Length; i++)
                s += (i == 0 ? "" : ",") + a[i].A + ":" + a[i].B;
            return s;
        }

        internal static void Run()
        {
            int[] si = { 1, 2, 3, 4 };
            int[] di = { 9, 9, 9, 9 };

            // -- Array.Copy, int32 rep, both arities. A negative operand is an
            //    ArgumentOutOfRangeException and outranks the overrun test, so
            //    the two families are probed together as well as apart. --
            Catches("Copy(si,di,-1)", () => Array.Copy(si, di, -1));
            Catches("Copy(si,di,5)", () => Array.Copy(si, di, 5));
            Catches("Copy(si,-1,di,0,1)", () => Array.Copy(si, -1, di, 0, 1));
            Catches("Copy(si,0,di,-1,1)", () => Array.Copy(si, 0, di, -1, 1));
            Catches("Copy(si,0,di,0,-1)", () => Array.Copy(si, 0, di, 0, -1));
            Catches("Copy(si,-1,di,0,99)", () => Array.Copy(si, -1, di, 0, 99));
            Catches("Copy(si,2,di,0,3)", () => Array.Copy(si, 2, di, 0, 3));
            Catches("Copy(si,0,di,2,3)", () => Array.Copy(si, 0, di, 2, 3));
            Catches("Copy(si,9,di,9,1)", () => Array.Copy(si, 9, di, 9, 1));
            // int32 arithmetic wraps `index + length` back under the length; the
            // check has to be wider than the operands it compares.
            Catches("Copy(si,1,di,0,int.Max)", () => Array.Copy(si, 1, di, 0, int.MaxValue));
            Catches("Copy(si,int.Max,di,0,1)", () => Array.Copy(si, int.MaxValue, di, 0, 1));
            Console.WriteLine("after i4 copy faults: si=" + Dump(si) + " di=" + Dump(di));

            // The boundary cases that must NOT throw: an index AT the length
            // with a zero length, and a length covering the whole array.
            Catches("Copy(si,4,di,0,0)", () => Array.Copy(si, 4, di, 0, 0));
            Catches("Copy(si,0,di,4,0)", () => Array.Copy(si, 0, di, 4, 0));
            Catches("Copy(si,0,di,0,4)", () => Array.Copy(si, 0, di, 0, 4));
            Console.WriteLine("after i4 copy edges: di=" + Dump(di));
            // …and one past it, which must.
            Catches("Copy(si,5,di,0,0)", () => Array.Copy(si, 5, di, 0, 0));
            Catches("Copy(si,0,di,5,0)", () => Array.Copy(si, 0, di, 5, 0));

            // -- reference rep --
            string[] sr = { "a", "b", "c", "d" };
            string[] dr = { "w", "x", "y", "z" };
            Catches("Copy(sr,dr,5)", () => Array.Copy(sr, dr, 5));
            Catches("Copy(sr,0,dr,3,2)", () => Array.Copy(sr, 0, dr, 3, 2));
            Catches("Copy(sr,-1,dr,0,1)", () => Array.Copy(sr, -1, dr, 0, 1));
            Catches("Clear(dr,1,9)", () => Array.Clear(dr, 1, 9));
            Console.WriteLine("after ref faults: sr=" + Dump(sr) + " dr=" + Dump(dr));

            // -- element-sized (packed struct) rep --
            Pair[] sp = new Pair[4];
            Pair[] dp = new Pair[4];
            for (int i = 0; i < 4; i++)
            {
                sp[i].A = (byte)(i + 1);
                sp[i].B = (byte)(i + 5);
                dp[i].A = 200;
                dp[i].B = 201;
            }
            Catches("Copy(sp,dp,5)", () => Array.Copy(sp, dp, 5));
            Catches("Copy(sp,-1,dp,0,1)", () => Array.Copy(sp, -1, dp, 0, 1));
            Catches("Copy(sp,0,dp,2,3)", () => Array.Copy(sp, 0, dp, 2, 3));
            Catches("Clear(dp,-1,-1)", () => Array.Clear(dp, -1, -1));
            Catches("Clear(dp,2,3)", () => Array.Clear(dp, 2, 3));
            Console.WriteLine("after packed faults: sp=" + Dump(sp) + " dp=" + Dump(dp));

            // -- the runtime-dispatched shape: System.Array-typed operands miss
            //    the emitter's inline arms and reach the _dyn helpers instead --
            Array sa = si;
            Array da = di;
            Catches("Copy(sa,da,5)", () => Array.Copy(sa, da, 5));
            Catches("Copy(sa,0,da,0,-1)", () => Array.Copy(sa, 0, da, 0, -1));
            Catches("Copy(sa,3,da,0,2)", () => Array.Copy(sa, 3, da, 0, 2));
            Catches("Copy(sa,0,da,3,2)", () => Array.Copy(sa, 0, da, 3, 2));
            Catches("Clear(da,2,3)", () => Array.Clear(da, 2, 3));
            Catches("Clear(da,-1,0)", () => Array.Clear(da, -1, 0));
            Console.WriteLine("after dyn faults: si=" + Dump(si) + " di=" + Dump(di));

            // -- Array.Clear over the int32 rep, whose whole family is
            //    IndexOutOfRangeException — not the Argument* pair Copy uses --
            Catches("Clear(di,-1,0)", () => Array.Clear(di, -1, 0));
            Catches("Clear(di,0,-1)", () => Array.Clear(di, 0, -1));
            Catches("Clear(di,2,3)", () => Array.Clear(di, 2, 3));
            Catches("Clear(di,5,0)", () => Array.Clear(di, 5, 0));
            Catches("Clear(di,1,int.Max)", () => Array.Clear(di, 1, int.MaxValue));
            Console.WriteLine("after clear faults: di=" + Dump(di));
            Catches("Clear(di,4,0)", () => Array.Clear(di, 4, 0));
            Catches("Clear(di,1,2)", () => Array.Clear(di, 1, 2));
            Console.WriteLine("after clear edges: di=" + Dump(di));

            // -- the MD arm of _clear_dyn: the length it validates against is the
            //    product of the dimensions, and an MD array carries no length
            //    header at all (the field at that offset is its rank) --
            int[,] md = new int[2, 3];
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 3; j++)
                    md[i, j] = i * 3 + j + 1;
            Array mda = md;
            Catches("Clear(mda,0,7)", () => Array.Clear(mda, 0, 7));
            Catches("Clear(mda,-1,0)", () => Array.Clear(mda, -1, 0));
            Console.WriteLine("md after faults: " + md[0, 0] + "," + md[1, 2]);
            Catches("Clear(mda,3,3)", () => Array.Clear(mda, 3, 3));
            Console.WriteLine("md after clear: " + md[0, 0] + "," + md[1, 2]);

            // -- Array.CopyTo: the destination index is an argument, the length
            //    the receiver's own, so an overlong destination window is an
            //    ArgumentException while a negative index is out-of-range --
            int[] four = { 1, 2, 3, 4 };
            int[] small = { 0, 0 };
            int[] wide = { 0, 0, 0, 0, 0, 0 };
            Catches("four.CopyTo(wide,-1)", () => four.CopyTo(wide, -1));
            Catches("four.CopyTo(small,0)", () => four.CopyTo(small, 0));
            Catches("four.CopyTo(wide,3)", () => four.CopyTo(wide, 3));
            Console.WriteLine("after copyto faults: small=" + Dump(small) + " wide=" + Dump(wide));
            Catches("four.CopyTo(wide,2)", () => four.CopyTo(wide, 2));
            Console.WriteLine("after copyto: wide=" + Dump(wide));
            Catches("sr.CopyTo(dr,3)", () => sr.CopyTo(dr, 3));
            Catches("sp.CopyTo(dp,3)", () => sp.CopyTo(dp, 3));
            Catches("sa.CopyTo(da,3)", () => sa.CopyTo(da, 3));
            Console.WriteLine("after copyto reps: dr=" + Dump(dr) + " dp=" + Dump(dp) + " di=" + Dump(di));

            // A zero-length array is the degenerate window on both sides.
            int[] empty = new int[0];
            Catches("Copy(empty,empty,0)", () => Array.Copy(empty, empty, 0));
            Catches("Clear(empty,0,0)", () => Array.Clear(empty, 0, 0));
            Catches("empty.CopyTo(wide,6)", () => empty.CopyTo(wide, 6));
            Catches("empty.CopyTo(wide,7)", () => empty.CopyTo(wide, 7));

            // The typed catches select the exact type over a broader handler —
            // the three families are catchable, not an abort.
            try
            {
                Array.Copy(si, 0, di, 0, 99);
                Console.WriteLine("unreachable");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("typed catch: ArgumentOutOfRangeException");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("typed catch: ArgumentException");
            }

            try
            {
                Array.Clear(di, 0, 99);
                Console.WriteLine("unreachable");
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("typed catch: IndexOutOfRangeException");
            }
            catch (Exception)
            {
                Console.WriteLine("typed catch: fell through to Exception");
            }

            // Recovery is real: the arrays still work after the faults.
            di[0] = 42;
            Array.Copy(si, 1, di, 1, 2);
            Console.WriteLine("recovered: di=" + Dump(di));

            // -- Array.Copy over the MD layout. The rank>=2 header shares no field with
            //    the SZ one (the word an SZArray reads as its length is the MD rank), so
            //    an MD pair moved through the SZ arms silently memmoves over the
            //    destination's header; the payload lines below are the assert. .NET
            //    copies MD arrays FLAT in row-major order, so the dimension shapes need
            //    not agree — only the RANKS, whose mismatch is a RankException checked
            //    after the nulls and before the range. --
            int[,] ma = new int[2, 3];
            int[,] mb = new int[2, 3];
            int[,] mc = new int[3, 2];
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 3; j++)
                    ma[i, j] = i * 3 + j + 1;
            Array maa = ma, mba = mb, mca = mc;
            Catches("Copy(ma,mb,6)", () => Array.Copy(maa, mba, 6));
            Console.WriteLine("md copy: mb=" + DumpMd(mb));
            Catches("Copy(ma,1,mb,0,4)", () => Array.Copy(maa, 1, mba, 0, 4));
            Console.WriteLine("md copy window: mb=" + DumpMd(mb));
            // The same pair with its STATIC `int[,]` type rather than System.Array:
            // a distinct emitter input, and the one that would break silently if a
            // future rep classifier ever answered SZ for the MD C++ type.
            Catches("Copy(int[,],int[,],6)", () => Array.Copy(ma, mb, 6));
            Console.WriteLine("md static copy: mb=" + DumpMd(mb));
            // Same rank, different dimension shape: a flat move, not a refusal.
            Catches("Copy(ma,mc,6)", () => Array.Copy(maa, mca, 6));
            Console.WriteLine("md reshape: mc=" + DumpMd(mc));
            // Overlapping self-copy is a memmove, not a memcpy.
            Catches("Copy(ma,0,ma,2,4)", () => Array.Copy(maa, 0, maa, 2, 4));
            Console.WriteLine("md overlap: ma=" + DumpMd(ma));
            // The range family is the same one the SZ arms answer with, but the
            // length it validates against is the PRODUCT of the dimensions.
            Catches("Copy(ma,mb,7)", () => Array.Copy(maa, mba, 7));
            Catches("Copy(ma,-1,mb,0,1)", () => Array.Copy(maa, -1, mba, 0, 1));
            Catches("Copy(ma,0,mb,3,4)", () => Array.Copy(maa, 0, mba, 3, 4));
            Catches("Copy(ma,7,mb,0,0)", () => Array.Copy(maa, 7, mba, 0, 0));
            Catches("Copy(ma,6,mb,0,0)", () => Array.Copy(maa, 6, mba, 0, 0));
            Console.WriteLine("md after range faults: ma=" + DumpMd(ma) + " mb=" + DumpMd(mb));

            // -- The mixed pairs, in both directions. Neither array can be read
            //    through the other's layout at all, and .NET says so by rank
            //    rather than by argument. A rank-3 destination is the same
            //    refusal between two MD shapes. --
            int[] flat = { 10, 20, 30, 40, 50, 60 };
            int[] flatd = new int[6];
            int[,,] m3 = new int[1, 2, 3];
            Array fa = flat, fda = flatd, m3a = m3;
            Catches("Copy(flat,mb,6)", () => Array.Copy(fa, mba, 6));
            Catches("Copy(ma,flatd,6)", () => Array.Copy(maa, fda, 6));
            Catches("Copy(ma,m3,6)", () => Array.Copy(maa, m3a, 6));
            // The rank test outranks the range one: a negative length behind a
            // rank mismatch still reports the rank.
            Catches("Copy(flat,mb,-1)", () => Array.Copy(fa, mba, -1));
            Catches("Copy(flat,-1,mb,0,1)", () => Array.Copy(fa, -1, mba, 0, 1));
            // …and the null test outranks the rank one.
            Catches("Copy(null,mb,6)", () => Array.Copy(null, mba, 6));
            Catches("Copy(ma,null,6)", () => Array.Copy(maa, null, 6));
            Console.WriteLine("after mixed faults: flat=" + Dump(flat) + " flatd=" + Dump(flatd)
                + " mb=" + DumpMd(mb) + " m3=" + DumpMd3(m3));

            // A typed catch selects RankException over the broader handler.
            try
            {
                Array.Copy(fa, mba, 6);
                Console.WriteLine("unreachable");
            }
            catch (RankException)
            {
                Console.WriteLine("typed catch: RankException");
            }
            catch (Exception)
            {
                Console.WriteLine("typed catch: fell through to Exception");
            }

            // -- Array.CopyTo's rank refusal is a DIFFERENT family from Copy's,
            //    because CopyTo tests the destination itself instead of leaving
            //    it to the Copy underneath: a rank>=2 destination is an
            //    ArgumentException, while a rank>=2 RECEIVER falls through to
            //    Copy's rank match. --
            Catches("flat.CopyTo(mb,0)", () => fa.CopyTo(mba, 0));
            Catches("ma.CopyTo(flatd,0)", () => maa.CopyTo(fda, 0));
            Catches("ma.CopyTo(mb,0)", () => maa.CopyTo(mba, 0));
            Console.WriteLine("after copyto rank: flatd=" + Dump(flatd) + " mb=" + DumpMd(mb));

            // -- Array.Clear over a System.Array-typed MD receiver, both arities.
            //    The one-argument form's length is the receiver's own, which for
            //    an MD array is the product of the dimensions and NOT the word an
            //    SZArray would read there. --
            Catches("Clear(ma,2,4)", () => Array.Clear(maa, 2, 4));
            Console.WriteLine("md clear window: ma=" + DumpMd(ma));
            Catches("Clear(mb)", () => Array.Clear(mba));
            Console.WriteLine("md clear all: mb=" + DumpMd(mb));

            // -- Array.Resize screens newSize itself: the newarr it lowers to would answer
            //    a negative size with OverflowException, which is not .NET's family. The
            //    array is left untouched, and a null slot takes the same refusal. --
            int[] rz = { 1, 2, 3 };
            Catches("Resize(rz,-1)", () => Array.Resize(ref rz, -1));
            Console.WriteLine("after resize fault: rz=" + Dump(rz));
            Catches("Resize(rz,int.Min)", () => Array.Resize(ref rz, int.MinValue));
            int[] rzNull = null;
            Catches("Resize(null,-1)", () => Array.Resize(ref rzNull, -1));
            Console.WriteLine("resize null slot still null: " + (rzNull is null));
            Catches("Resize(rz,0)", () => Array.Resize(ref rz, 0));
            Console.WriteLine("after resize edge: rz.Length=" + rz.Length);
            try
            {
                int[] rz2 = { 1, 2 };
                Array.Resize(ref rz2, -5);
                Console.WriteLine("unreachable");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("typed catch: ArgumentOutOfRangeException");
            }
            catch (Exception)
            {
                Console.WriteLine("typed catch: fell through to Exception");
            }
        }
    }
}
