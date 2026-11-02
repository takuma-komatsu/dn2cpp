using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GenericMathEndian;

internal static class EndianReads
{
    private static int _checks;
    private static string _order;
    private static char _destination;

    private static void Check(string label, bool condition)
    {
        if (!condition) throw new InvalidOperationException("endian." + label);
        _checks++;
        Console.WriteLine("endian." + label + "=ok");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T Read<T>(byte[] source, bool unsigned, bool little, int route) where T : IBinaryInteger<T>
    {
        if (route == 0) return little ? T.ReadLittleEndian(source, unsigned) : T.ReadBigEndian(source, unsigned);
        if (route == 1) return little ? T.ReadLittleEndian(new ReadOnlySpan<byte>(source), unsigned) : T.ReadBigEndian(new ReadOnlySpan<byte>(source), unsigned);
        byte[] padded = new byte[source.Length + 1];
        padded[0] = 0x69;
        Array.Copy(source, 0, padded, 1, source.Length);
        return little ? T.ReadLittleEndian(padded, 1, unsigned) : T.ReadBigEndian(padded, 1, unsigned);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Try<T>(ReadOnlySpan<byte> source, bool unsigned, bool little, out T value) where T : IBinaryInteger<T> =>
        little ? T.TryReadLittleEndian(source, unsigned, out value) : T.TryReadBigEndian(source, unsigned, out value);

    private static void Case<T>(string label, byte[] big, bool unsigned, bool success, T expected, T sentinel) where T : IBinaryInteger<T>
    {
        for (int endian = 0; endian < 2; endian++)
        {
            bool little = endian != 0;
            byte[] source = (byte[])big.Clone();
            if (little) Array.Reverse(source);
            string key = label + (little ? ".le" : ".be");
            T value = sentinel;
            bool result = Try<T>(source, unsigned, little, out value);
            Check(key + ".try", result == success && value.Equals(expected));
            for (int route = 0; route < 3; route++)
            {
                bool overflow = false;
                T read = sentinel;
                try { read = Read<T>(source, unsigned, little, route); }
                catch (OverflowException) { overflow = true; }
                Check(key + ".read" + route, overflow == !success && (!success || read.Equals(expected)));
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T ReadStart<T>(byte[] source, int start, bool unsigned, bool little) where T : IBinaryInteger<T> =>
        little ? T.ReadLittleEndian(source, start, unsigned) : T.ReadBigEndian(source, start, unsigned);

    private static void ArrayBounds<T>(string label, T zero) where T : IBinaryInteger<T>
    {
        for (int endian = 0; endian < 2; endian++)
        {
            bool little = endian != 0;
            string key = label + (little ? ".le" : ".be");
            Check(key + ".null-array", Read<T>(null, true, little, 0).Equals(zero));
            Check(key + ".null-span", Read<T>(null, true, little, 1).Equals(zero));
            Check(key + ".null-start-zero", ReadStart<T>(null, 0, true, little).Equals(zero));
            Check(key + ".end", ReadStart<T>(new byte[] { 0xff }, 1, true, little).Equals(zero));
            for (int shape = 0; shape < 4; shape++)
            {
                byte[] bytes = shape < 2 ? null : new byte[] { 0x12 };
                int start = (shape & 1) == 0 ? -1 : 2;
                bool caught = false;
                try { ReadStart<T>(bytes, start, true, little); }
                catch (ArgumentOutOfRangeException error) { caught = error.ParamName is null; }
                Check(key + ".bad-start" + shape, caught);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CharFields { public char Before; public char Value; public char After; }

    private static void CharStorage()
    {
        byte[] valid = { 0xd8, 0x00 };
        byte[] invalid = { 0x01, 0x00, 0x00 };
        char local = '\uffff';
        Check("char.local-success", Try<char>(valid, true, false, out local) && local == '\ud800');
        Check("char.local-failure", !Try<char>(invalid, true, false, out local) && local == 0);
        char[] array = { '\u1234', '\uffff', '\u5678' };
        Check("char.array-success", Try<char>(valid, true, false, out array[1]) && array[1] == '\ud800' && array[0] == '\u1234' && array[2] == '\u5678');
        Check("char.array-failure", !Try<char>(invalid, true, false, out array[1]) && array[1] == 0 && array[0] == '\u1234' && array[2] == '\u5678');
        CharFields fields = new CharFields { Before = '\u1234', Value = '\uffff', After = '\u5678' };
        Check("char.field-success", Try<char>(valid, true, false, out fields.Value) && fields.Value == '\ud800' && fields.Before == '\u1234' && fields.After == '\u5678');
        Check("char.field-failure", !Try<char>(invalid, true, false, out fields.Value) && fields.Value == 0 && fields.Before == '\u1234' && fields.After == '\u5678');
        char[] alias = { '\u1234', '\u5678' };
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(alias.AsSpan(0, 1));
        char expected = BitConverter.IsLittleEndian ? '\u3412' : '\u1234';
        Check("char.overlap", Try<char>(bytes, true, false, out alias[0]) && alias[0] == expected && alias[1] == '\u5678');
    }

    private sealed class EvaluationException : Exception { }
    private static byte[] Source(bool fail) { _order += "S"; if (fail) throw new EvaluationException(); return new byte[] { 0x12, 0x34 }; }
    private static bool Unsigned(bool fail) { _order += "U"; if (fail) throw new EvaluationException(); return true; }
    private static ref char Destination(bool fail) { _order += "O"; if (fail) throw new EvaluationException(); return ref _destination; }
    private static int Start() { _order += "I"; return -1; }

    private static void EvaluationOrder()
    {
        _order = ""; _destination = '\uffff';
        bool result = Try<char>(Source(false), Unsigned(false), false, out Destination(false));
        Check("order.success", result && _order == "SUO" && _destination == '\u1234');
        for (int fail = 0; fail < 3; fail++)
        {
            _order = ""; _destination = '\uffff'; bool caught = false;
            try { Try<char>(Source(fail == 0), Unsigned(fail == 1), false, out Destination(fail == 2)); }
            catch (EvaluationException) { caught = true; }
            Check("order.throw" + fail, caught && _order == (fail == 0 ? "S" : fail == 1 ? "SU" : "SUO") && _destination == '\uffff');
        }
        _order = ""; bool range = false;
        try { ReadStart<char>(Source(false), Start(), Unsigned(false), false); }
        catch (ArgumentOutOfRangeException error) { range = error.ParamName is null; }
        Check("order.range", range && _order == "SIU");
    }

    internal static void __GateEntry()
    {
        _checks = 0;
        Console.WriteLine("== IBinaryInteger endian managed bodies ==");
        Check("native-width", IntPtr.Size == 8 && UIntPtr.Size == 8);
        Case<char>("char.b00.s", new byte[] {  }, false, true, (char)0x0000, (char)1);
        Case<char>("char.b00.u", new byte[] {  }, true, true, (char)0x0000, (char)1);
        Case<char>("char.b01.s", new byte[] { 0x00 }, false, true, (char)0x0000, (char)1);
        Case<char>("char.b01.u", new byte[] { 0x00 }, true, true, (char)0x0000, (char)1);
        Case<char>("char.b02.s", new byte[] { 0x7f }, false, true, (char)0x007f, (char)1);
        Case<char>("char.b02.u", new byte[] { 0x7f }, true, true, (char)0x007f, (char)1);
        Case<char>("char.b03.s", new byte[] { 0x80 }, false, false, (char)0x0000, (char)1);
        Case<char>("char.b03.u", new byte[] { 0x80 }, true, true, (char)0x0080, (char)1);
        Case<char>("char.b04.s", new byte[] { 0xff }, false, false, (char)0x0000, (char)1);
        Case<char>("char.b04.u", new byte[] { 0xff }, true, true, (char)0x00ff, (char)1);
        Case<char>("char.b05.s", new byte[] { 0x12 }, false, true, (char)0x0012, (char)1);
        Case<char>("char.b05.u", new byte[] { 0x12 }, true, true, (char)0x0012, (char)1);
        Case<char>("char.b06.s", new byte[] { 0x7f, 0xff }, false, true, (char)0x7fff, (char)1);
        Case<char>("char.b06.u", new byte[] { 0x7f, 0xff }, true, true, (char)0x7fff, (char)1);
        Case<char>("char.b07.s", new byte[] { 0x80, 0x00 }, false, false, (char)0x0000, (char)1);
        Case<char>("char.b07.u", new byte[] { 0x80, 0x00 }, true, true, (char)0x8000, (char)1);
        Case<char>("char.b08.s", new byte[] { 0xff, 0xff }, false, false, (char)0x0000, (char)1);
        Case<char>("char.b08.u", new byte[] { 0xff, 0xff }, true, true, (char)0xffff, (char)1);
        Case<char>("char.b09.s", new byte[] { 0x80, 0x01 }, false, false, (char)0x0000, (char)1);
        Case<char>("char.b09.u", new byte[] { 0x80, 0x01 }, true, true, (char)0x8001, (char)1);
        Case<char>("char.b10.s", new byte[] { 0x00, 0xff, 0xff }, false, true, (char)0xffff, (char)1);
        Case<char>("char.b10.u", new byte[] { 0x00, 0xff, 0xff }, true, true, (char)0xffff, (char)1);
        Case<char>("char.b11.s", new byte[] { 0xff, 0x80, 0x00 }, false, false, (char)0x0000, (char)1);
        Case<char>("char.b11.u", new byte[] { 0xff, 0x80, 0x00 }, true, false, (char)0x0000, (char)1);
        Case<char>("char.b12.s", new byte[] { 0x01, 0x00, 0x00 }, false, false, (char)0x0000, (char)1);
        Case<char>("char.b12.u", new byte[] { 0x01, 0x00, 0x00 }, true, false, (char)0x0000, (char)1);
        Case<char>("char.b13.s", new byte[] { 0xff, 0x7f, 0xff }, false, false, (char)0x0000, (char)1);
        Case<char>("char.b13.u", new byte[] { 0xff, 0x7f, 0xff }, true, false, (char)0x0000, (char)1);
        Case<char>("char.b14.s", new byte[] { 0x01, 0x23, 0x23 }, false, false, (char)0x0000, (char)1);
        Case<char>("char.b14.u", new byte[] { 0x01, 0x23, 0x23 }, true, false, (char)0x0000, (char)1);
        Case<char>("char.b15.s", new byte[] { 0x00, 0x00, 0x7f }, false, true, (char)0x007f, (char)1);
        Case<char>("char.b15.u", new byte[] { 0x00, 0x00, 0x7f }, true, true, (char)0x007f, (char)1);
        Case<char>("char.b16.s", new byte[] { 0x00, 0x80 }, false, true, (char)0x0080, (char)1);
        Case<char>("char.b16.u", new byte[] { 0x00, 0x80 }, true, true, (char)0x0080, (char)1);
        Case<char>("char.b17.s", new byte[] { 0x01, 0x00 }, false, true, (char)0x0100, (char)1);
        Case<char>("char.b17.u", new byte[] { 0x01, 0x00 }, true, true, (char)0x0100, (char)1);
        Case<char>("char.b18.s", new byte[] { 0xd8, 0x00 }, false, false, (char)0x0000, (char)1);
        Case<char>("char.b18.u", new byte[] { 0xd8, 0x00 }, true, true, (char)0xd800, (char)1);
        Case<char>("char.b19.s", new byte[] { 0xdf, 0xff }, false, false, (char)0x0000, (char)1);
        Case<char>("char.b19.u", new byte[] { 0xdf, 0xff }, true, true, (char)0xdfff, (char)1);
        ArrayBounds<char>("char", (char)0);
        Case<int>("int.b00.s", new byte[] {  }, false, true, 0, (int)1);
        Case<int>("int.b00.u", new byte[] {  }, true, true, 0, (int)1);
        Case<int>("int.b01.s", new byte[] { 0x00 }, false, true, 0, (int)1);
        Case<int>("int.b01.u", new byte[] { 0x00 }, true, true, 0, (int)1);
        Case<int>("int.b02.s", new byte[] { 0x7f }, false, true, 127, (int)1);
        Case<int>("int.b02.u", new byte[] { 0x7f }, true, true, 127, (int)1);
        Case<int>("int.b03.s", new byte[] { 0x80 }, false, true, -128, (int)1);
        Case<int>("int.b03.u", new byte[] { 0x80 }, true, true, 128, (int)1);
        Case<int>("int.b04.s", new byte[] { 0xff }, false, true, -1, (int)1);
        Case<int>("int.b04.u", new byte[] { 0xff }, true, true, 255, (int)1);
        Case<int>("int.b05.s", new byte[] { 0x12, 0x00, 0x00 }, false, true, 1179648, (int)1);
        Case<int>("int.b05.u", new byte[] { 0x12, 0x00, 0x00 }, true, true, 1179648, (int)1);
        Case<int>("int.b06.s", new byte[] { 0x7f, 0xff, 0xff, 0xff }, false, true, 2147483647, (int)1);
        Case<int>("int.b06.u", new byte[] { 0x7f, 0xff, 0xff, 0xff }, true, true, 2147483647, (int)1);
        Case<int>("int.b07.s", new byte[] { 0x80, 0x00, 0x00, 0x00 }, false, true, -2147483648, (int)1);
        Case<int>("int.b07.u", new byte[] { 0x80, 0x00, 0x00, 0x00 }, true, false, 0, (int)1);
        Case<int>("int.b08.s", new byte[] { 0xff, 0xff, 0xff, 0xff }, false, true, -1, (int)1);
        Case<int>("int.b08.u", new byte[] { 0xff, 0xff, 0xff, 0xff }, true, false, 0, (int)1);
        Case<int>("int.b09.s", new byte[] { 0x80, 0x00, 0x00, 0x01 }, false, true, -2147483647, (int)1);
        Case<int>("int.b09.u", new byte[] { 0x80, 0x00, 0x00, 0x01 }, true, false, 0, (int)1);
        Case<int>("int.b10.s", new byte[] { 0x00, 0xff, 0xff, 0xff, 0xff }, false, false, 0, (int)1);
        Case<int>("int.b10.u", new byte[] { 0x00, 0xff, 0xff, 0xff, 0xff }, true, false, 0, (int)1);
        Case<int>("int.b11.s", new byte[] { 0xff, 0x80, 0x00, 0x00, 0x00 }, false, true, -2147483648, (int)1);
        Case<int>("int.b11.u", new byte[] { 0xff, 0x80, 0x00, 0x00, 0x00 }, true, false, 0, (int)1);
        Case<int>("int.b12.s", new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00 }, false, false, 0, (int)1);
        Case<int>("int.b12.u", new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00 }, true, false, 0, (int)1);
        Case<int>("int.b13.s", new byte[] { 0xff, 0x7f, 0xff, 0xff, 0xff }, false, false, 0, (int)1);
        Case<int>("int.b13.u", new byte[] { 0xff, 0x7f, 0xff, 0xff, 0xff }, true, false, 0, (int)1);
        Case<int>("int.b14.s", new byte[] { 0x01, 0x23, 0x23, 0x23, 0x23 }, false, false, 0, (int)1);
        Case<int>("int.b14.u", new byte[] { 0x01, 0x23, 0x23, 0x23, 0x23 }, true, false, 0, (int)1);
        Case<int>("int.b15.s", new byte[] { 0x00, 0x00, 0x7f }, false, true, 127, (int)1);
        Case<int>("int.b15.u", new byte[] { 0x00, 0x00, 0x7f }, true, true, 127, (int)1);
        ArrayBounds<int>("int", (int)0);
        Case<long>("long.b00.s", new byte[] {  }, false, true, 0L, (long)1);
        Case<long>("long.b00.u", new byte[] {  }, true, true, 0L, (long)1);
        Case<long>("long.b01.s", new byte[] { 0x00 }, false, true, 0L, (long)1);
        Case<long>("long.b01.u", new byte[] { 0x00 }, true, true, 0L, (long)1);
        Case<long>("long.b02.s", new byte[] { 0x7f }, false, true, 127L, (long)1);
        Case<long>("long.b02.u", new byte[] { 0x7f }, true, true, 127L, (long)1);
        Case<long>("long.b03.s", new byte[] { 0x80 }, false, true, -128L, (long)1);
        Case<long>("long.b03.u", new byte[] { 0x80 }, true, true, 128L, (long)1);
        Case<long>("long.b04.s", new byte[] { 0xff }, false, true, -1L, (long)1);
        Case<long>("long.b04.u", new byte[] { 0xff }, true, true, 255L, (long)1);
        Case<long>("long.b05.s", new byte[] { 0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, true, 5066549580791808L, (long)1);
        Case<long>("long.b05.u", new byte[] { 0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, true, 5066549580791808L, (long)1);
        Case<long>("long.b06.s", new byte[] { 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, true, 9223372036854775807L, (long)1);
        Case<long>("long.b06.u", new byte[] { 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, true, 9223372036854775807L, (long)1);
        Case<long>("long.b07.s", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, true, long.MinValue, (long)1);
        Case<long>("long.b07.u", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, false, 0L, (long)1);
        Case<long>("long.b08.s", new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, true, -1L, (long)1);
        Case<long>("long.b08.u", new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, false, 0L, (long)1);
        Case<long>("long.b09.s", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, false, true, -9223372036854775807L, (long)1);
        Case<long>("long.b09.u", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, true, false, 0L, (long)1);
        Case<long>("long.b10.s", new byte[] { 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, false, 0L, (long)1);
        Case<long>("long.b10.u", new byte[] { 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, false, 0L, (long)1);
        Case<long>("long.b11.s", new byte[] { 0xff, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, true, long.MinValue, (long)1);
        Case<long>("long.b11.u", new byte[] { 0xff, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, false, 0L, (long)1);
        Case<long>("long.b12.s", new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, false, 0L, (long)1);
        Case<long>("long.b12.u", new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, false, 0L, (long)1);
        Case<long>("long.b13.s", new byte[] { 0xff, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, false, 0L, (long)1);
        Case<long>("long.b13.u", new byte[] { 0xff, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, false, 0L, (long)1);
        Case<long>("long.b14.s", new byte[] { 0x01, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23 }, false, false, 0L, (long)1);
        Case<long>("long.b14.u", new byte[] { 0x01, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23 }, true, false, 0L, (long)1);
        Case<long>("long.b15.s", new byte[] { 0x00, 0x00, 0x7f }, false, true, 127L, (long)1);
        Case<long>("long.b15.u", new byte[] { 0x00, 0x00, 0x7f }, true, true, 127L, (long)1);
        ArrayBounds<long>("long", (long)0);
        Case<nint>("nint.b00.s", new byte[] {  }, false, true, (nint)(0L), (nint)1);
        Case<nint>("nint.b00.u", new byte[] {  }, true, true, (nint)(0L), (nint)1);
        Case<nint>("nint.b01.s", new byte[] { 0x00 }, false, true, (nint)(0L), (nint)1);
        Case<nint>("nint.b01.u", new byte[] { 0x00 }, true, true, (nint)(0L), (nint)1);
        Case<nint>("nint.b02.s", new byte[] { 0x7f }, false, true, (nint)(127L), (nint)1);
        Case<nint>("nint.b02.u", new byte[] { 0x7f }, true, true, (nint)(127L), (nint)1);
        Case<nint>("nint.b03.s", new byte[] { 0x80 }, false, true, (nint)(-128L), (nint)1);
        Case<nint>("nint.b03.u", new byte[] { 0x80 }, true, true, (nint)(128L), (nint)1);
        Case<nint>("nint.b04.s", new byte[] { 0xff }, false, true, (nint)(-1L), (nint)1);
        Case<nint>("nint.b04.u", new byte[] { 0xff }, true, true, (nint)(255L), (nint)1);
        Case<nint>("nint.b05.s", new byte[] { 0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, true, (nint)(5066549580791808L), (nint)1);
        Case<nint>("nint.b05.u", new byte[] { 0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, true, (nint)(5066549580791808L), (nint)1);
        Case<nint>("nint.b06.s", new byte[] { 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, true, (nint)(9223372036854775807L), (nint)1);
        Case<nint>("nint.b06.u", new byte[] { 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, true, (nint)(9223372036854775807L), (nint)1);
        Case<nint>("nint.b07.s", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, true, (nint)(long.MinValue), (nint)1);
        Case<nint>("nint.b07.u", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, false, (nint)(0L), (nint)1);
        Case<nint>("nint.b08.s", new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, true, (nint)(-1L), (nint)1);
        Case<nint>("nint.b08.u", new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, false, (nint)(0L), (nint)1);
        Case<nint>("nint.b09.s", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, false, true, (nint)(-9223372036854775807L), (nint)1);
        Case<nint>("nint.b09.u", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, true, false, (nint)(0L), (nint)1);
        Case<nint>("nint.b10.s", new byte[] { 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, false, (nint)(0L), (nint)1);
        Case<nint>("nint.b10.u", new byte[] { 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, false, (nint)(0L), (nint)1);
        Case<nint>("nint.b11.s", new byte[] { 0xff, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, true, (nint)(long.MinValue), (nint)1);
        Case<nint>("nint.b11.u", new byte[] { 0xff, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, false, (nint)(0L), (nint)1);
        Case<nint>("nint.b12.s", new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, false, (nint)(0L), (nint)1);
        Case<nint>("nint.b12.u", new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, false, (nint)(0L), (nint)1);
        Case<nint>("nint.b13.s", new byte[] { 0xff, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, false, (nint)(0L), (nint)1);
        Case<nint>("nint.b13.u", new byte[] { 0xff, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, false, (nint)(0L), (nint)1);
        Case<nint>("nint.b14.s", new byte[] { 0x01, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23 }, false, false, (nint)(0L), (nint)1);
        Case<nint>("nint.b14.u", new byte[] { 0x01, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23 }, true, false, (nint)(0L), (nint)1);
        Case<nint>("nint.b15.s", new byte[] { 0x00, 0x00, 0x7f }, false, true, (nint)(127L), (nint)1);
        Case<nint>("nint.b15.u", new byte[] { 0x00, 0x00, 0x7f }, true, true, (nint)(127L), (nint)1);
        ArrayBounds<nint>("nint", (nint)0);
        Case<uint>("uint.b00.s", new byte[] {  }, false, true, 0U, (uint)1);
        Case<uint>("uint.b00.u", new byte[] {  }, true, true, 0U, (uint)1);
        Case<uint>("uint.b01.s", new byte[] { 0x00 }, false, true, 0U, (uint)1);
        Case<uint>("uint.b01.u", new byte[] { 0x00 }, true, true, 0U, (uint)1);
        Case<uint>("uint.b02.s", new byte[] { 0x7f }, false, true, 127U, (uint)1);
        Case<uint>("uint.b02.u", new byte[] { 0x7f }, true, true, 127U, (uint)1);
        Case<uint>("uint.b03.s", new byte[] { 0x80 }, false, false, 0U, (uint)1);
        Case<uint>("uint.b03.u", new byte[] { 0x80 }, true, true, 128U, (uint)1);
        Case<uint>("uint.b04.s", new byte[] { 0xff }, false, false, 0U, (uint)1);
        Case<uint>("uint.b04.u", new byte[] { 0xff }, true, true, 255U, (uint)1);
        Case<uint>("uint.b05.s", new byte[] { 0x12, 0x00, 0x00 }, false, true, 1179648U, (uint)1);
        Case<uint>("uint.b05.u", new byte[] { 0x12, 0x00, 0x00 }, true, true, 1179648U, (uint)1);
        Case<uint>("uint.b06.s", new byte[] { 0x7f, 0xff, 0xff, 0xff }, false, true, 2147483647U, (uint)1);
        Case<uint>("uint.b06.u", new byte[] { 0x7f, 0xff, 0xff, 0xff }, true, true, 2147483647U, (uint)1);
        Case<uint>("uint.b07.s", new byte[] { 0x80, 0x00, 0x00, 0x00 }, false, false, 0U, (uint)1);
        Case<uint>("uint.b07.u", new byte[] { 0x80, 0x00, 0x00, 0x00 }, true, true, 2147483648U, (uint)1);
        Case<uint>("uint.b08.s", new byte[] { 0xff, 0xff, 0xff, 0xff }, false, false, 0U, (uint)1);
        Case<uint>("uint.b08.u", new byte[] { 0xff, 0xff, 0xff, 0xff }, true, true, 4294967295U, (uint)1);
        Case<uint>("uint.b09.s", new byte[] { 0x80, 0x00, 0x00, 0x01 }, false, false, 0U, (uint)1);
        Case<uint>("uint.b09.u", new byte[] { 0x80, 0x00, 0x00, 0x01 }, true, true, 2147483649U, (uint)1);
        Case<uint>("uint.b10.s", new byte[] { 0x00, 0xff, 0xff, 0xff, 0xff }, false, true, 4294967295U, (uint)1);
        Case<uint>("uint.b10.u", new byte[] { 0x00, 0xff, 0xff, 0xff, 0xff }, true, true, 4294967295U, (uint)1);
        Case<uint>("uint.b11.s", new byte[] { 0xff, 0x80, 0x00, 0x00, 0x00 }, false, false, 0U, (uint)1);
        Case<uint>("uint.b11.u", new byte[] { 0xff, 0x80, 0x00, 0x00, 0x00 }, true, false, 0U, (uint)1);
        Case<uint>("uint.b12.s", new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00 }, false, false, 0U, (uint)1);
        Case<uint>("uint.b12.u", new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00 }, true, false, 0U, (uint)1);
        Case<uint>("uint.b13.s", new byte[] { 0xff, 0x7f, 0xff, 0xff, 0xff }, false, false, 0U, (uint)1);
        Case<uint>("uint.b13.u", new byte[] { 0xff, 0x7f, 0xff, 0xff, 0xff }, true, false, 0U, (uint)1);
        Case<uint>("uint.b14.s", new byte[] { 0x01, 0x23, 0x23, 0x23, 0x23 }, false, false, 0U, (uint)1);
        Case<uint>("uint.b14.u", new byte[] { 0x01, 0x23, 0x23, 0x23, 0x23 }, true, false, 0U, (uint)1);
        Case<uint>("uint.b15.s", new byte[] { 0x00, 0x00, 0x7f }, false, true, 127U, (uint)1);
        Case<uint>("uint.b15.u", new byte[] { 0x00, 0x00, 0x7f }, true, true, 127U, (uint)1);
        ArrayBounds<uint>("uint", (uint)0);
        Case<ulong>("ulong.b00.s", new byte[] {  }, false, true, 0UL, (ulong)1);
        Case<ulong>("ulong.b00.u", new byte[] {  }, true, true, 0UL, (ulong)1);
        Case<ulong>("ulong.b01.s", new byte[] { 0x00 }, false, true, 0UL, (ulong)1);
        Case<ulong>("ulong.b01.u", new byte[] { 0x00 }, true, true, 0UL, (ulong)1);
        Case<ulong>("ulong.b02.s", new byte[] { 0x7f }, false, true, 127UL, (ulong)1);
        Case<ulong>("ulong.b02.u", new byte[] { 0x7f }, true, true, 127UL, (ulong)1);
        Case<ulong>("ulong.b03.s", new byte[] { 0x80 }, false, false, 0UL, (ulong)1);
        Case<ulong>("ulong.b03.u", new byte[] { 0x80 }, true, true, 128UL, (ulong)1);
        Case<ulong>("ulong.b04.s", new byte[] { 0xff }, false, false, 0UL, (ulong)1);
        Case<ulong>("ulong.b04.u", new byte[] { 0xff }, true, true, 255UL, (ulong)1);
        Case<ulong>("ulong.b05.s", new byte[] { 0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, true, 5066549580791808UL, (ulong)1);
        Case<ulong>("ulong.b05.u", new byte[] { 0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, true, 5066549580791808UL, (ulong)1);
        Case<ulong>("ulong.b06.s", new byte[] { 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, true, 9223372036854775807UL, (ulong)1);
        Case<ulong>("ulong.b06.u", new byte[] { 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, true, 9223372036854775807UL, (ulong)1);
        Case<ulong>("ulong.b07.s", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, false, 0UL, (ulong)1);
        Case<ulong>("ulong.b07.u", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, true, 9223372036854775808UL, (ulong)1);
        Case<ulong>("ulong.b08.s", new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, false, 0UL, (ulong)1);
        Case<ulong>("ulong.b08.u", new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, true, 18446744073709551615UL, (ulong)1);
        Case<ulong>("ulong.b09.s", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, false, false, 0UL, (ulong)1);
        Case<ulong>("ulong.b09.u", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, true, true, 9223372036854775809UL, (ulong)1);
        Case<ulong>("ulong.b10.s", new byte[] { 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, true, 18446744073709551615UL, (ulong)1);
        Case<ulong>("ulong.b10.u", new byte[] { 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, true, 18446744073709551615UL, (ulong)1);
        Case<ulong>("ulong.b11.s", new byte[] { 0xff, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, false, 0UL, (ulong)1);
        Case<ulong>("ulong.b11.u", new byte[] { 0xff, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, false, 0UL, (ulong)1);
        Case<ulong>("ulong.b12.s", new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, false, 0UL, (ulong)1);
        Case<ulong>("ulong.b12.u", new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, false, 0UL, (ulong)1);
        Case<ulong>("ulong.b13.s", new byte[] { 0xff, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, false, 0UL, (ulong)1);
        Case<ulong>("ulong.b13.u", new byte[] { 0xff, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, false, 0UL, (ulong)1);
        Case<ulong>("ulong.b14.s", new byte[] { 0x01, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23 }, false, false, 0UL, (ulong)1);
        Case<ulong>("ulong.b14.u", new byte[] { 0x01, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23 }, true, false, 0UL, (ulong)1);
        Case<ulong>("ulong.b15.s", new byte[] { 0x00, 0x00, 0x7f }, false, true, 127UL, (ulong)1);
        Case<ulong>("ulong.b15.u", new byte[] { 0x00, 0x00, 0x7f }, true, true, 127UL, (ulong)1);
        ArrayBounds<ulong>("ulong", (ulong)0);
        Case<nuint>("nuint.b00.s", new byte[] {  }, false, true, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b00.u", new byte[] {  }, true, true, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b01.s", new byte[] { 0x00 }, false, true, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b01.u", new byte[] { 0x00 }, true, true, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b02.s", new byte[] { 0x7f }, false, true, (nuint)(127UL), (nuint)1);
        Case<nuint>("nuint.b02.u", new byte[] { 0x7f }, true, true, (nuint)(127UL), (nuint)1);
        Case<nuint>("nuint.b03.s", new byte[] { 0x80 }, false, false, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b03.u", new byte[] { 0x80 }, true, true, (nuint)(128UL), (nuint)1);
        Case<nuint>("nuint.b04.s", new byte[] { 0xff }, false, false, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b04.u", new byte[] { 0xff }, true, true, (nuint)(255UL), (nuint)1);
        Case<nuint>("nuint.b05.s", new byte[] { 0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, true, (nuint)(5066549580791808UL), (nuint)1);
        Case<nuint>("nuint.b05.u", new byte[] { 0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, true, (nuint)(5066549580791808UL), (nuint)1);
        Case<nuint>("nuint.b06.s", new byte[] { 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, true, (nuint)(9223372036854775807UL), (nuint)1);
        Case<nuint>("nuint.b06.u", new byte[] { 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, true, (nuint)(9223372036854775807UL), (nuint)1);
        Case<nuint>("nuint.b07.s", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, false, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b07.u", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, true, (nuint)(9223372036854775808UL), (nuint)1);
        Case<nuint>("nuint.b08.s", new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, false, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b08.u", new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, true, (nuint)(18446744073709551615UL), (nuint)1);
        Case<nuint>("nuint.b09.s", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, false, false, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b09.u", new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, true, true, (nuint)(9223372036854775809UL), (nuint)1);
        Case<nuint>("nuint.b10.s", new byte[] { 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, true, (nuint)(18446744073709551615UL), (nuint)1);
        Case<nuint>("nuint.b10.u", new byte[] { 0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, true, (nuint)(18446744073709551615UL), (nuint)1);
        Case<nuint>("nuint.b11.s", new byte[] { 0xff, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, false, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b11.u", new byte[] { 0xff, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, false, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b12.s", new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, false, false, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b12.u", new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, true, false, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b13.s", new byte[] { 0xff, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, false, false, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b13.u", new byte[] { 0xff, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, true, false, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b14.s", new byte[] { 0x01, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23 }, false, false, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b14.u", new byte[] { 0x01, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23, 0x23 }, true, false, (nuint)(0UL), (nuint)1);
        Case<nuint>("nuint.b15.s", new byte[] { 0x00, 0x00, 0x7f }, false, true, (nuint)(127UL), (nuint)1);
        Case<nuint>("nuint.b15.u", new byte[] { 0x00, 0x00, 0x7f }, true, true, (nuint)(127UL), (nuint)1);
        ArrayBounds<nuint>("nuint", (nuint)0);
        CharStorage();
        EvaluationOrder();
        int overflowKind = 0;
        try { Read<char>(new byte[] { 1, 0, 0 }, true, false, 0); }
        catch (OverflowException) { overflowKind = 1; }
        catch (InvalidOperationException) { overflowKind = 2; }
        Check("overflow.exact-catch", overflowKind == 1);
        if (_checks != 1982) throw new InvalidOperationException("endian check count");
        Console.WriteLine("endian.checks=" + _checks);
        Console.WriteLine("== endian complete ==");
    }
}
