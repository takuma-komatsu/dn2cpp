using System;
using System.Buffers;
using System.Collections.Generic;
using MemoryPack;

namespace MemoryPackLayoutSubset
{
    [MemoryPackable(GenerateType.VersionTolerant)]
    internal partial class Versioned
    {
        [MemoryPackOrder(0)]
        public int Id { get; set; }

        [MemoryPackOrder(1)]
        public string Label { get; set; }

        [MemoryPackOrder(2)]
        public double Ratio { get; set; }
    }

    [MemoryPackable(SerializeLayout.Explicit)]
    internal partial class Explicitly
    {
        [MemoryPackOrder(2)]
        public int Third { get; set; }

        [MemoryPackOrder(0)]
        public int First { get; set; }

        [MemoryPackOrder(1)]
        public int Second { get; set; }
    }

    // All fields unmanaged: the generator takes the whole-struct memcpy path rather
    // than a member-by-member write, so the byte layout is the struct's own layout.
    [MemoryPackable]
    internal partial struct Fixed
    {
        public int A;
        public long B;
        public float C;
        public bool D;
    }

    // decimal is unmanaged, so a struct carrying one stays on that same memcpy path and
    // its 16 raw bytes reach the wire: the hex below is what pins the runtime decimal's
    // field order to .NET's. One value per axis of the layout — a scale with a trailing
    // zero, a negative, the top legal scale, and a full 96-bit mantissa.
    [MemoryPackable]
    internal partial struct FixedDecimal
    {
        public decimal Price;
        public decimal Delta;
        public decimal Tiny;
        public decimal Widest;
        public decimal NegativeZero;
        public int Tag;
    }

    [MemoryPackable]
    internal partial class Holder
    {
        public Fixed[] Items { get; set; }
        public int Total { get; set; }
    }

    internal static class Program
    {
        internal static void __GateEntry()
        {
            VersionTolerantLayout();
            ExplicitLayout();
            UnmanagedFastPath();
            BufferWriterPath();
            SequenceAndOverwrite();
            DecimalFastPath();
        }

        private static void VersionTolerantLayout()
        {
            var src = new Versioned { Id = 3, Label = "v", Ratio = 0.25 };
            byte[] bin = MemoryPackSerializer.Serialize(src);
            Console.WriteLine("[vt] len=" + bin.Length + " hex=" + Convert.ToHexString(bin));
            var back = MemoryPackSerializer.Deserialize<Versioned>(bin);
            Console.WriteLine("[vt] id=" + back.Id + " label=" + back.Label
                + " ratio=" + back.Ratio.ToString("R"));
        }

        private static void ExplicitLayout()
        {
            byte[] bin = MemoryPackSerializer.Serialize(new Explicitly
            {
                First = 1,
                Second = 2,
                Third = 3,
            });
            Console.WriteLine("[explicit] hex=" + Convert.ToHexString(bin));
            var back = MemoryPackSerializer.Deserialize<Explicitly>(bin);
            Console.WriteLine("[explicit] " + back.First + "," + back.Second + "," + back.Third);
        }

        private static void UnmanagedFastPath()
        {
            byte[] one = MemoryPackSerializer.Serialize(new Fixed { A = 1, B = 2, C = 0.5f, D = true });
            Console.WriteLine("[fixed] len=" + one.Length + " hex=" + Convert.ToHexString(one));
            Fixed back = MemoryPackSerializer.Deserialize<Fixed>(one);
            Console.WriteLine("[fixed] a=" + back.A + " b=" + back.B + " c=" + back.C.ToString("R")
                + " d=" + back.D);

            var holder = new Holder
            {
                Items = new Fixed[]
                {
                    new Fixed { A = 10, B = 20, C = 1.5f, D = false },
                    new Fixed { A = 30, B = 40, C = -2.5f, D = true },
                },
                Total = 2,
            };
            byte[] many = MemoryPackSerializer.Serialize(holder);
            Console.WriteLine("[fixed-array] len=" + many.Length + " hex=" + Convert.ToHexString(many));
            var holderBack = MemoryPackSerializer.Deserialize<Holder>(many);
            Console.WriteLine("[fixed-array] total=" + holderBack.Total
                + " a0=" + holderBack.Items[0].A + " c1=" + holderBack.Items[1].C.ToString("R"));
        }

        private static void BufferWriterPath()
        {
            var writer = new ArrayBufferWriter<byte>();
            MemoryPackSerializer.Serialize(writer, new Versioned { Id = 9, Label = "bw", Ratio = 1.5 });
            byte[] bin = writer.WrittenSpan.ToArray();
            Console.WriteLine("[bufwriter] len=" + bin.Length + " hex=" + Convert.ToHexString(bin));
            var back = MemoryPackSerializer.Deserialize<Versioned>(bin);
            Console.WriteLine("[bufwriter] id=" + back.Id + " label=" + back.Label);
        }

        private static void SequenceAndOverwrite()
        {
            byte[] bin = MemoryPackSerializer.Serialize(new List<int> { 4, 5, 6 });
            var seq = new ReadOnlySequence<byte>(bin);
            var fromSeq = MemoryPackSerializer.Deserialize<List<int>>(seq);
            Console.WriteLine("[sequence] " + string.Join(",", fromSeq));

            // The ref overload fills an existing instance instead of allocating one.
            var target = new Versioned { Id = -1, Label = "stale", Ratio = 0.0 };
            byte[] payload = MemoryPackSerializer.Serialize(new Versioned
            {
                Id = 11,
                Label = "fresh",
                Ratio = 2.25,
            });
            int consumed = MemoryPackSerializer.Deserialize(payload, ref target);
            Console.WriteLine("[overwrite] consumed=" + consumed + " id=" + target.Id
                + " label=" + target.Label + " ratio=" + target.Ratio.ToString("R"));
        }

        private static void DecimalFastPath()
        {
            byte[] one = MemoryPackSerializer.Serialize(new FixedDecimal
            {
                Price = 1.50m,
                Delta = -123.456m,
                Tiny = 0.0000000000000000000000000001m,
                Widest = decimal.MaxValue,
                NegativeZero = new decimal(0, 0, 0, true, 3),
                Tag = 7,
            });
            Console.WriteLine("[fixed-decimal] len=" + one.Length + " hex=" + Convert.ToHexString(one));
            FixedDecimal back = MemoryPackSerializer.Deserialize<FixedDecimal>(one);
            Console.WriteLine("[fixed-decimal] price=" + back.Price + " delta=" + back.Delta
                + " tiny=" + back.Tiny + " widest=" + back.Widest
                + " negative-zero=" + decimal.GetBits(back.NegativeZero)[3].ToString("X8")
                + " tag=" + back.Tag);
        }
    }
}
