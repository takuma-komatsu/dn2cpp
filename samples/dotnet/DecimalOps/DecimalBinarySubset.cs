using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace Dn2Cpp;

internal static class DecimalBinarySubset
{
    private static int _checks;
    private static string _order = "";
    private static byte[] _ordered = new byte[7];
    private static int _value;
    private static readonly ArgumentException Marker = new ArgumentException("decimal-binary-marker");

    private static void Check(string label, bool value)
    {
        if (!value) throw new InvalidOperationException("decimal-binary:" + label);
        _checks++;
        Console.WriteLine("db." + label + "=ok");
    }

    private static string Description(Exception e) => e.GetType().FullName + ":" + e.HResult
        + ":" + e.Message.Replace("\r", "\\r").Replace("\n", "\\n")
        + ":param=" + (e is ArgumentException a ? a.ParamName : "-");

    private static void Detail(string label, Exception e) => Console.WriteLine("db.detail." + label + "="
        + Description(e) + ":inner=" + (e.InnerException is null ? "-" : Description(e.InnerException)));

    private static bool Bytes(byte[] actual, int offset, byte[] expected)
    {
        for (int i = 0; i < expected.Length; i++) if (actual[offset + i] != expected[i]) return false;
        return true;
    }

    private static byte[] Filled(int length)
    {
        var bytes = new byte[length];
        for (int i = 0; i < length; i++) bytes[i] = 0xa7;
        return bytes;
    }

    private static bool Bits(decimal value, uint lo, uint mid, uint hi, uint flags)
    {
        int[] bits = decimal.GetBits(value);
        return unchecked((uint)bits[0]) == lo && unchecked((uint)bits[1]) == mid
            && unchecked((uint)bits[2]) == hi && unchecked((uint)bits[3]) == flags;
    }

    private static void Record(string label, uint lo, uint mid, uint hi, uint flags, byte[] wire)
    {
        decimal value = new decimal(unchecked((int)lo), unchecked((int)mid), unchecked((int)hi),
            (flags & 0x80000000u) != 0, (byte)(flags >> 16));
        var output = Filled(18);
        using (var stream = new MemoryStream(output, 1, 16, true, true))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(value);
            Check("write." + label, stream.Position == 16 && output[0] == 0xa7 && output[17] == 0xa7
                && Bytes(output, 1, wire) && Bits(value, lo, mid, hi, flags));
        }
        var input = Filled(18);
        for (int i = 0; i < 16; i++) input[i + 1] = wire[i];
        using (var stream = new MemoryStream(input, 1, 16, false, true))
        using (var reader = new BinaryReader(stream))
        {
            decimal actual = reader.ReadDecimal();
            Check("read." + label, stream.Position == 16 && input[0] == 0xa7 && input[17] == 0xa7
                && Bytes(input, 1, wire) && Bits(actual, lo, mid, hi, flags));
        }
    }

    private static void BadRecord(string label, uint flags)
    {
        var input = new byte[] { 0x67, 0x45, 0x23, 0x01, 0xef, 0xcd, 0xab, 0x89,
            0x98, 0xba, 0xdc, 0xfe, (byte)flags, (byte)(flags >> 8), (byte)(flags >> 16), (byte)(flags >> 24), 0xa7 };
        using var stream = new MemoryStream(input, false);
        using var reader = new BinaryReader(stream);
        Exception? error = null;
        try { reader.ReadDecimal(); } catch (Exception e) { error = e; }
        Check("invalid." + label, error?.GetType() == typeof(IOException)
            && error.InnerException?.GetType() == typeof(ArgumentException)
            && stream.Position == 16 && input[16] == 0xa7);
        Detail("invalid." + label, error!);
    }

    private static void ShortRecords()
    {
        for (int length = 0; length < 16; length++)
        {
            var input = Filled(length);
            using var stream = new MemoryStream(input, false);
            using var reader = new BinaryReader(stream);
            Exception? error = null;
            try { reader.ReadDecimal(); } catch (Exception e) { error = e; }
            Check("short." + length, error?.GetType() == typeof(EndOfStreamException)
                && error.InnerException is null && stream.Position == length && Bytes(input, 0, Filled(length)));
            Detail("short." + length, error!);
        }
    }

    private sealed class ChunkStream : Stream
    {
        private readonly byte[] _bytes;
        private readonly Exception? _error;
        private int _position;
        internal int Calls;
        internal ChunkStream(byte[] bytes, Exception? error = null) { _bytes = bytes; _error = error; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _bytes.Length;
        public override long Position { get => _position; set => throw new NotSupportedException(); }
        public override int Read(Span<byte> buffer)
        {
            Calls++;
            if (_error is not null) throw _error;
            int count = Math.Min(Math.Min(buffer.Length, 3), _bytes.Length - _position);
            for (int i = 0; i < count; i++) buffer[i] = _bytes[_position + i];
            _position += count;
            return count;
        }
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private static void ChunkedRecords()
    {
        var wire = new byte[] { 0x67, 0x45, 0x23, 0x01, 0xef, 0xcd, 0xab, 0x89,
            0x98, 0xba, 0xdc, 0xfe, 0, 0, 7, 0x80 };
        using (var stream = new ChunkStream(wire))
        using (var reader = new BinaryReader(stream))
        {
            decimal value = reader.ReadDecimal();
            Check("chunk.complete", Bits(value, 0x01234567u, 0x89abcdefu, 0xfedcba98u, 0x80070000u)
                && stream.Position == 16 && stream.Calls == 6);
        }
        using (var stream = new ChunkStream(Filled(11)))
        using (var reader = new BinaryReader(stream))
        {
            Exception? error = null;
            try { reader.ReadDecimal(); } catch (Exception e) { error = e; }
            Check("chunk.short", error?.GetType() == typeof(EndOfStreamException)
                && stream.Position == 11 && stream.Calls == 5);
            Detail("chunk.short", error!);
        }
        using (var stream = new ChunkStream(wire, Marker))
        using (var reader = new BinaryReader(stream))
        {
            Exception? error = null;
            try { reader.ReadDecimal(); } catch (Exception e) { error = e; }
            Check("chunk.throw", ReferenceEquals(error, Marker) && stream.Position == 0 && stream.Calls == 1);
            Detail("chunk.throw", error!);
        }
    }

    private static byte[] ReadBuffer() => new byte[] { 0xa7, 0x78, 0x56, 0x34, 0x12, 0xa7, 0xa7 };

    private static void Int32Spans()
    {
        for (int length = 0; length <= 5; length++)
        {
            var input = ReadBuffer();
            int actual = 0x55667788;
            Exception? readError = null;
            try { actual = MemoryMarshal.Read<int>(input.AsSpan(1, length)); } catch (Exception e) { readError = e; }
            Check("mm.read." + length, Bytes(input, 0, ReadBuffer()) && (length < 4
                ? readError?.GetType() == typeof(ArgumentOutOfRangeException)
                    && ((ArgumentOutOfRangeException)readError).ParamName == "length" && actual == 0x55667788
                : readError is null && actual == 0x12345678));
            if (length < 4) Detail("mm.read." + length, readError!);
            var output = Filled(7);
            int value = 0x12345678;
            Exception? writeError = null;
            try { MemoryMarshal.Write<int>(output.AsSpan(1, length), in value); } catch (Exception e) { writeError = e; }
            Check("mm.write." + length, value == 0x12345678 && (length < 4
                ? writeError?.GetType() == typeof(ArgumentOutOfRangeException)
                    && ((ArgumentOutOfRangeException)writeError).ParamName == "length" && Bytes(output, 0, Filled(7))
                : writeError is null && Bytes(output, 0, ReadBuffer())));
            if (length < 4) Detail("mm.write." + length, writeError!);
        }
        var bytes = ReadBuffer();
        Check("bp.read", BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(1, 4)) == 0x12345678
            && Bytes(bytes, 0, ReadBuffer()));
        Exception? error = null;
        try { BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(1, 3)); } catch (Exception e) { error = e; }
        Check("bp.read.short", error?.GetType() == typeof(ArgumentOutOfRangeException)
            && ((ArgumentOutOfRangeException)error).ParamName == "length" && Bytes(bytes, 0, ReadBuffer()));
        Detail("bp.read.short", error!);
        bytes = Filled(7);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(1, 5), 0x12345678);
        Check("bp.write", Bytes(bytes, 0, ReadBuffer()));
        bytes = Filled(7); error = null;
        try { BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(1, 3), 0x12345678); } catch (Exception e) { error = e; }
        Check("bp.write.short", error?.GetType() == typeof(ArgumentOutOfRangeException)
            && ((ArgumentOutOfRangeException)error).ParamName == "length" && Bytes(bytes, 0, Filled(7)));
        Detail("bp.write.short", error!);
    }

    private static Span<byte> Destination(int length) { _order += "D"; return _ordered.AsSpan(1, length); }
    private static ref int Value(bool fail) { _order += "V"; if (fail) throw Marker; return ref _value; }
    private static ReadOnlySpan<byte> Source(bool fail) { _order += "S"; if (fail) throw Marker; return _ordered.AsSpan(1, 3); }

    private static void Order()
    {
        _order = ""; _ordered = Filled(7); _value = 0x12345678;
        Exception? error = null;
        try { MemoryMarshal.Write<int>(Destination(3), in Value(false)); } catch (Exception e) { error = e; }
        Check("order.write.short", _order == "DV" && error?.GetType() == typeof(ArgumentOutOfRangeException)
            && ((ArgumentOutOfRangeException)error).ParamName == "length" && Bytes(_ordered, 0, Filled(7)) && _value == 0x12345678);
        _order = ""; _ordered = Filled(7);
        MemoryMarshal.Write<int>(Destination(4), in Value(false));
        Check("order.write", _order == "DV" && Bytes(_ordered, 0, ReadBuffer()) && _value == 0x12345678);
        _order = ""; _ordered = Filled(7); error = null;
        try { MemoryMarshal.Write<int>(Destination(3), in Value(true)); } catch (Exception e) { error = e; }
        Check("order.value.throw", _order == "DV" && ReferenceEquals(error, Marker) && Bytes(_ordered, 0, Filled(7)));
        _order = ""; error = null;
        try { MemoryMarshal.Read<int>(Source(false)); } catch (Exception e) { error = e; }
        Check("order.read.short", _order == "S" && error?.GetType() == typeof(ArgumentOutOfRangeException)
            && ((ArgumentOutOfRangeException)error).ParamName == "length" && Bytes(_ordered, 0, Filled(7)));
        _order = ""; error = null;
        try { MemoryMarshal.Read<int>(Source(true)); } catch (Exception e) { error = e; }
        Check("order.source.throw", _order == "S" && ReferenceEquals(error, Marker) && Bytes(_ordered, 0, Filled(7)));
    }

    internal static void Run()
    {
        _checks = 0;
        Console.WriteLine("[DecimalBinarySubset]");
        Check("width", IntPtr.Size == 8 && BitConverter.IsLittleEndian);
        Record("p0s0", 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        Record("n0s0", 0x00000000u, 0x00000000u, 0x00000000u, 0x80000000u, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 });
        Record("p0s28", 0x00000000u, 0x00000000u, 0x00000000u, 0x001c0000u, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1c, 0x00 });
        Record("n0s28", 0x00000000u, 0x00000000u, 0x00000000u, 0x801c0000u, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1c, 0x80 });
        Record("p1s0", 0x00000001u, 0x00000000u, 0x00000000u, 0x00000000u, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        Record("n1s0", 0x00000001u, 0x00000000u, 0x00000000u, 0x80000000u, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 });
        Record("p1s1", 0x00000001u, 0x00000000u, 0x00000000u, 0x00010000u, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00 });
        Record("n1s1", 0x00000001u, 0x00000000u, 0x00000000u, 0x80010000u, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x80 });
        Record("p1s28", 0x00000001u, 0x00000000u, 0x00000000u, 0x001c0000u, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1c, 0x00 });
        Record("n1s28", 0x00000001u, 0x00000000u, 0x00000000u, 0x801c0000u, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1c, 0x80 });
        Record("mid", 0x00000000u, 0x00000001u, 0x00000000u, 0x00000000u, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        Record("high", 0x00000000u, 0x00000000u, 0x00000001u, 0x00000000u, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        Record("maxp", 0xffffffffu, 0xffffffffu, 0xffffffffu, 0x00000000u, new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00 });
        Record("maxn", 0xffffffffu, 0xffffffffu, 0xffffffffu, 0x80000000u, new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x80 });
        Record("mixedp", 0x01234567u, 0x89abcdefu, 0xfedcba98u, 0x00070000u, new byte[] { 0x67, 0x45, 0x23, 0x01, 0xef, 0xcd, 0xab, 0x89, 0x98, 0xba, 0xdc, 0xfe, 0x00, 0x00, 0x07, 0x00 });
        Record("mixedn", 0x01234567u, 0x89abcdefu, 0xfedcba98u, 0x80070000u, new byte[] { 0x67, 0x45, 0x23, 0x01, 0xef, 0xcd, 0xab, 0x89, 0x98, 0xba, 0xdc, 0xfe, 0x00, 0x00, 0x07, 0x80 });
        Record("trailingp", 0x0000000au, 0x00000000u, 0x00000000u, 0x00010000u, new byte[] { 0x0a, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00 });
        Record("trailingn", 0x0000000au, 0x00000000u, 0x00000000u, 0x80010000u, new byte[] { 0x0a, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x80 });
        BadRecord("reserved0", 0x00000001u);
        BadRecord("reserved1", 0x00000002u);
        BadRecord("reserved2", 0x00000004u);
        BadRecord("reserved3", 0x00000008u);
        BadRecord("reserved4", 0x00000010u);
        BadRecord("reserved5", 0x00000020u);
        BadRecord("reserved6", 0x00000040u);
        BadRecord("reserved7", 0x00000080u);
        BadRecord("reserved8", 0x00000100u);
        BadRecord("reserved9", 0x00000200u);
        BadRecord("reserved10", 0x00000400u);
        BadRecord("reserved11", 0x00000800u);
        BadRecord("reserved12", 0x00001000u);
        BadRecord("reserved13", 0x00002000u);
        BadRecord("reserved14", 0x00004000u);
        BadRecord("reserved15", 0x00008000u);
        BadRecord("reserved24", 0x01000000u);
        BadRecord("reserved25", 0x02000000u);
        BadRecord("reserved26", 0x04000000u);
        BadRecord("reserved27", 0x08000000u);
        BadRecord("reserved28", 0x10000000u);
        BadRecord("reserved29", 0x20000000u);
        BadRecord("reserved30", 0x40000000u);
        BadRecord("scale29p", 0x001d0000u);
        BadRecord("scale29n", 0x801d0000u);
        BadRecord("scale255p", 0x00ff0000u);
        BadRecord("scale255n", 0x80ff0000u);
        ShortRecords();
        ChunkedRecords();
        Int32Spans();
        Order();
        if (_checks != 104) throw new InvalidOperationException("decimal-binary count");
        Console.WriteLine("decimal-binary.checks=" + _checks);
        Console.WriteLine("decimal-binary.done");
    }
}
