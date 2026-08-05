#nullable disable
using System;

namespace ArrayCopyCompatSubset
{
    // Array.Copy's TYPE-compatibility verdict between two arrays, with real .NET as the
    // oracle. Every pair reaches dn2cpp_array_copy_dyn (Array-typed operands) or the
    // emitter's static-pair screen (concretely typed locals). The rules are the CLR's:
    // same normalized integral (unsigned == signed of the width, enum == underlying)
    // moves raw; CanPrimitiveWiden converts per element; value -> compatible reference
    // boxes; reference -> value unboxes under an EXACT type test (a boxed short into
    // int[] is InvalidCastException, not a widen); reference -> reference moves raw when
    // the source element is assignable to the destination's and cast-checks per element
    // when only the reverse holds; anything else is ArrayTypeMismatchException —
    // including a ZERO-length copy, so the verdict is asserted apart from any element.
    // The per-element arms fault mid-copy exactly where .NET does, so the partial-write
    // lines are part of the assert.
    internal static class Program
    {
        private enum EInt { A = 5, B = 6, C = 7 }

        private enum EUInt : uint { X = 8, Y = 9 }

        private enum EByte : byte { P = 1, Q = 2 }

        // No ToString override: the boxed-struct prints below assert the PAYLOAD via
        // field reads, so the copy is checked without depending on virtual ToString
        // dispatch off a runtime-minted box.
        private struct S8 { public int a, b; }

        private struct T8 { public int c, d; }

        private class Base { public override string ToString() => "Base"; }

        private class Der : Base { public override string ToString() => "Der"; }

        private static void Try(string label, Action a)
        {
            try { a(); }
            catch (Exception e) { Console.WriteLine(label + ": throws " + e.GetType().Name); }
        }

        private static void Show(string label, Array dst)
        {
            Console.Write(label + ": ok [");
            for (int i = 0; i < dst.Length; i++)
                Console.Write((i > 0 ? " " : "") + (dst.GetValue(i) ?? "null"));
            Console.WriteLine("]");
        }

        public static void Run()
        {
            Console.WriteLine("-- copycompat: same identity / normalized integral --");
            Try("int->int", () => { Array s = new int[] { 1, 2, 3 }; Array d = new int[3]; Array.Copy(s, d, 3); Show("int->int", d); });
            Try("int->uint", () => { Array s = new int[] { -1, 2 }; Array d = new uint[2]; Array.Copy(s, d, 2); Show("int->uint", d); });
            Try("uint->int", () => { Array s = new uint[] { 4294967295, 2 }; Array d = new int[2]; Array.Copy(s, d, 2); Show("uint->int", d); });
            Try("byte->sbyte", () => { Array s = new byte[] { 255, 1 }; Array d = new sbyte[2]; Array.Copy(s, d, 2); Show("byte->sbyte", d); });
            Try("ushort->short", () => { Array s = new ushort[] { 65535 }; Array d = new short[1]; Array.Copy(s, d, 1); Show("ushort->short", d); });
            Try("EInt->int", () => { Array s = new EInt[] { EInt.A, EInt.B }; Array d = new int[2]; Array.Copy(s, d, 2); Show("EInt->int", d); });
            Try("int->EInt", () => { Array s = new int[] { 6 }; Array d = new EInt[1]; Array.Copy(s, d, 1); Show("int->EInt", d); });
            Try("EInt->EUInt", () => { Array s = new EInt[] { EInt.A }; Array d = new EUInt[1]; Array.Copy(s, d, 1); Show("EInt->EUInt", d); });
            Try("EByte->byte", () => { Array s = new EByte[] { EByte.Q }; Array d = new byte[1]; Array.Copy(s, d, 1); Show("EByte->byte", d); });

            Console.WriteLine("-- copycompat: primitive widening (per-element convert) --");
            Try("int->long", () => { Array s = new int[] { -3, 4 }; Array d = new long[2]; Array.Copy(s, d, 2); Show("int->long", d); });
            Try("byte->int", () => { Array s = new byte[] { 200 }; Array d = new int[1]; Array.Copy(s, d, 1); Show("byte->int", d); });
            Try("byte->char", () => { Array s = new byte[] { 65 }; Array d = new char[1]; Array.Copy(s, d, 1); Show("byte->char", d); });
            Try("char->ushort", () => { Array s = new char[] { 'A', 'B' }; Array d = new ushort[2]; Array.Copy(s, d, 2); Show("char->ushort", d); });
            Try("ushort->char", () => { Array s = new ushort[] { 67, 68 }; Array d = new char[2]; Array.Copy(s, d, 2); Show("ushort->char", d); });
            Try("char->int", () => { Array s = new char[] { 'Z' }; Array d = new int[1]; Array.Copy(s, d, 1); Show("char->int", d); });
            Try("int->float", () => { Array s = new int[] { 16777217 }; Array d = new float[1]; Array.Copy(s, d, 1); Show("int->float", d); });
            Try("int->double", () => { Array s = new int[] { 7 }; Array d = new double[1]; Array.Copy(s, d, 1); Show("int->double", d); });
            Try("float->double", () => { Array s = new float[] { 1.5f }; Array d = new double[1]; Array.Copy(s, d, 1); Show("float->double", d); });
            Try("long->float", () => { Array s = new long[] { 123456789012345 }; Array d = new float[1]; Array.Copy(s, d, 1); Show("long->float", d); });
            Try("ulong->double", () => { Array s = new ulong[] { ulong.MaxValue }; Array d = new double[1]; Array.Copy(s, d, 1); Show("ulong->double", d); });
            Try("EByte->EInt", () => { Array s = new EByte[] { EByte.P }; Array d = new EInt[1]; Array.Copy(s, d, 1); Show("EByte->EInt", d); });
            Try("EInt->long", () => { Array s = new EInt[] { EInt.A }; Array d = new long[1]; Array.Copy(s, d, 1); Show("EInt->long", d); });

            Console.WriteLine("-- copycompat: ArrayTypeMismatch refusals --");
            Try("long->int", () => { Array s = new long[] { 3 }; Array d = new int[1]; Array.Copy(s, d, 1); });
            Try("double->float", () => { Array s = new double[] { 1.5 }; Array d = new float[1]; Array.Copy(s, d, 1); });
            Try("float->int", () => { Array s = new float[] { 2.5f }; Array d = new int[1]; Array.Copy(s, d, 1); });
            Try("bool->byte", () => { Array s = new bool[] { true }; Array d = new byte[1]; Array.Copy(s, d, 1); });
            Try("byte->bool", () => { Array s = new byte[] { 2 }; Array d = new bool[1]; Array.Copy(s, d, 1); });
            Try("short->char", () => { Array s = new short[] { 66 }; Array d = new char[1]; Array.Copy(s, d, 1); });
            Try("sbyte->ushort", () => { Array s = new sbyte[] { -1 }; Array d = new ushort[1]; Array.Copy(s, d, 1); });
            Try("S8->T8", () => { Array s = new S8[1]; Array d = new T8[1]; Array.Copy(s, d, 1); });
            Try("string->int", () => { Array s = new string[] { "x" }; Array d = new int[1]; Array.Copy(s, d, 1); });
            Try("string->Base", () => { Array s = new string[] { "a" }; Array d = new Base[1]; Array.Copy(s, d, 1); });
            // int[] -> IDisposable[] lives in InterfaceElementArraySubset, which owns
            // every reader of the precise element type-info.
            Try("intq->int", () => { Array s = new int?[] { 1 }; Array d = new int[1]; Array.Copy(s, d, 1); });
            Try("int->intq", () => { Array s = new int[] { 1 }; Array d = new int?[1]; Array.Copy(s, d, 1); });
            // IntPtr/UIntPtr: interchangeable with each other (one normalization
            // class) but with NO widening row — nint->long refuses (measured).
            Try("nint->nuint", () => { Array s = new nint[] { 3 }; Array d = new nuint[1]; Array.Copy(s, d, 1); Show("nint->nuint", d); });
            Try("nint->long", () => { Array s = new nint[] { 3 }; Array d = new long[1]; Array.Copy(s, d, 1); });
            // The verdict precedes the move: a zero-length copy of an incompatible
            // pair still refuses (measured on net10).
            Try("zero-len long->int", () => { Array s = new long[] { 1 }; Array d = new int[1]; Array.Copy(s, d, 0); });
            // ... but sits AFTER the rank and range screens (measured order).
            Try("neglen long->int", () => { Array s = new long[2]; Array d = new int[2]; Array.Copy(s, 0, d, 0, -1); });
            Try("overrun long->int", () => { Array s = new long[2]; Array d = new int[2]; Array.Copy(s, 0, d, 0, 5); });
            Try("rank long->int[,]", () => { Array s = new long[2]; Array d = new int[2, 2]; Array.Copy(s, 0, d, 0, 1); });
            // The refusal is catchable BY TYPE.
            try { Array s = new long[] { 1 }; Array d = new int[1]; Array.Copy(s, d, 1); }
            catch (ArrayTypeMismatchException) { Console.WriteLine("typed catch: ArrayTypeMismatchException bound"); }

            Console.WriteLine("-- copycompat: boxing (value -> reference) --");
            Try("int->object", () => { Array s = new int[] { 1, 2 }; Array d = new object[2]; Array.Copy(s, d, 2); Show("int->object", d); });
            Try("S8->object", () => { Array s = new S8[] { new S8 { a = 1, b = 2 } }; Array d = new object[1]; Array.Copy(s, d, 1); S8 got = (S8)d.GetValue(0); Console.WriteLine("S8->object payload: " + got.a + "," + got.b); });
            Try("int->IComparable", () => { Array s = new int[] { 9 }; Array d = new IComparable[1]; Array.Copy(s, d, 1); Show("int->IComparable", d); });
            Try("intq->object", () => { Array s = new int?[] { 1, null }; Array d = new object[2]; Array.Copy(s, d, 2); Show("intq->object", d); });

            Console.WriteLine("-- copycompat: unboxing (reference -> value, exact element type) --");
            Try("object(int)->int", () => { Array s = new object[] { 1, 2 }; Array d = new int[2]; Array.Copy(s, d, 2); Show("object(int)->int", d); });
            Try("object(short)->int", () => { Array s = new object[] { (short)5 }; Array d = new int[1]; Array.Copy(s, d, 1); });
            Try("object(EInt)->int", () => { Array s = new object[] { EInt.A }; Array d = new int[1]; Array.Copy(s, d, 1); });
            Try("object(mixed)->int", () => { Array s = new object[] { 1, "x", 3 }; Array d = new int[] { 7, 8, 9 }; try { Array.Copy(s, d, 3); } finally { Show("object(mixed)->int partial", d); } });
            Try("object(null)->int", () => { Array s = new object[] { 1, null, 3 }; Array d = new int[3]; try { Array.Copy(s, d, 3); } finally { Show("object(null)->int partial", d); } });
            Try("object(S8)->S8", () => { Array s = new object[] { new S8 { a = 3, b = 4 } }; Array d = new S8[1]; Array.Copy(s, d, 1); S8 got = (S8)d.GetValue(0); Console.WriteLine("object(S8)->S8 payload: " + got.a + "," + got.b); });
            Try("IComparable(int)->int", () => { Array s = new IComparable[] { 5 }; Array d = new int[1]; Array.Copy(s, d, 1); Show("IComparable(int)->int", d); });
            Try("object(int)->intq", () => { Array s = new object[] { 1 }; Array d = new int?[1]; Array.Copy(s, d, 1); Show("object(int)->intq", d); });
            Try("object(null)->intq", () => { Array s = new object[] { null }; Array d = new int?[1]; Array.Copy(s, d, 1); Show("object(null)->intq", d); });
            Try("object(long)->intq", () => { Array s = new object[] { 5L }; Array d = new int?[1]; Array.Copy(s, d, 1); });

            Console.WriteLine("-- copycompat: reference <-> reference --");
            Try("string->object", () => { Array s = new string[] { "a", "b" }; Array d = new object[2]; Array.Copy(s, d, 2); Show("string->object", d); });
            Try("object(string)->string", () => { Array s = new object[] { "a", "b" }; Array d = new string[2]; Array.Copy(s, d, 2); Show("object(string)->string", d); });
            Try("object(mixed)->string", () => { Array s = new object[] { "a", 1 }; Array d = new string[] { "p", "q" }; try { Array.Copy(s, d, 2); } finally { Show("object(mixed)->string partial", d); } });
            Try("Der->Base", () => { Array s = new Der[] { new Der() }; Array d = new Base[1]; Array.Copy(s, d, 1); Show("Der->Base", d); });
            Try("Base(Der)->Der", () => { Array s = new Base[] { new Der() }; Array d = new Der[1]; Array.Copy(s, d, 1); Show("Base(Der)->Der", d); });
            Try("Base(Base)->Der", () => { Array s = new Base[] { new Base() }; Array d = new Der[1]; Array.Copy(s, d, 1); });
            Try("string->IComparable", () => { Array s = new string[] { "a" }; Array d = new IComparable[1]; Array.Copy(s, d, 1); Show("string->IComparable", d); });
            Try("jagged->object", () => { Array s = new int[][] { new[] { 1 } }; Array d = new object[1]; Array.Copy(s, d, 1); Console.WriteLine("jagged->object: ok len=" + ((int[])((object[])d)[0]).Length); });

            Console.WriteLine("-- copycompat: MD pairs (same rules, flat window) --");
            Try("md int->uint", () => { Array s = new int[1, 2] { { -1, 2 } }; Array d = new uint[1, 2]; Array.Copy(s, d, 2); Console.WriteLine("md int->uint: ok " + ((uint[,])d)[0, 0] + " " + ((uint[,])d)[0, 1]); });
            Try("md int->long", () => { Array s = new int[1, 2] { { 3, 4 } }; Array d = new long[1, 2]; Array.Copy(s, d, 2); Console.WriteLine("md int->long: ok " + ((long[,])d)[0, 0] + " " + ((long[,])d)[0, 1]); });
            Try("md object->int", () => { Array s = new object[1, 2] { { 1, 2 } }; Array d = new int[1, 2]; Array.Copy(s, d, 2); Console.WriteLine("md object->int: ok " + ((int[,])d)[0, 1]); });
            Try("md long->int", () => { Array s = new long[1, 1]; Array d = new int[1, 1]; Array.Copy(s, d, 1); });
            Try("md string->object", () => { Array s = new string[1, 1] { { "x" } }; Array d = new object[1, 1]; Array.Copy(s, d, 1); Console.WriteLine("md string->object: ok " + ((object[,])d)[0, 0]); });

            Console.WriteLine("-- copycompat: statically typed operands (the emitter's screen) --");
            // Concretely typed locals take the emitter's inline arms; a pair it cannot
            // prove same-element funnels into the same runtime verdict as above.
            Try("static int->long", () => { int[] s = { -3, 4 }; long[] d = new long[2]; Array.Copy(s, d, 2); Show("static int->long", d); });
            Try("static long->int", () => { long[] s = { 3 }; int[] d = new int[1]; Array.Copy(s, d, 1); });
            Try("static short->byte", () => { short[] s = { 1 }; byte[] d = new byte[1]; Array.Copy(s, d, 1); });
            Try("static int->object", () => { int[] s = { 1, 2 }; object[] d = new object[2]; Array.Copy(s, d, 2); Show("static int->object", d); });
            Try("static object->int", () => { object[] s = { 7 }; int[] d = new int[1]; Array.Copy(s, d, 1); Show("static object->int", d); });
            Try("static Base->Der", () => { Base[] s = { new Base() }; Der[] d = new Der[1]; Array.Copy(s, d, 1); });
            Try("static copyto int->long", () => { int[] s = { 1 }; long[] d = new long[1]; s.CopyTo(d, 0); });
            Try("static copyto int->int", () => { int[] s = { 4, 5 }; int[] d = new int[3]; s.CopyTo(d, 1); Show("static copyto int->int", d); });
            Try("self-overlap", () => { Array s = new int[] { 1, 2, 3, 4 }; Array.Copy(s, 0, s, 1, 3); Show("self-overlap", s); });
        }
    }
}
