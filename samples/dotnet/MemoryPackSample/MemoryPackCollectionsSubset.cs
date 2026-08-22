using System;
using System.Collections.Generic;
using MemoryPack;

namespace MemoryPackCollectionsSubset
{
    internal enum Rank : byte
    {
        Low = 1,
        High = 200,
    }

    [MemoryPackable]
    internal partial class Bag
    {
        public int[] Numbers { get; set; }
        public List<string> Words { get; set; }
        public Dictionary<string, int> Counts { get; set; }
        public byte[] Blob { get; set; }
    }

    [MemoryPackable]
    internal partial class Scalars
    {
        public int? MaybeInt { get; set; }
        public Rank Rank { get; set; }
        public Guid Id { get; set; }
        public DateTime Stamp { get; set; }
        public TimeSpan Span { get; set; }
        public KeyValuePair<string, int> Pair { get; set; }
    }

    internal static class Program
    {
        internal static void __GateEntry()
        {
            MemberCollections();
            ScalarMembers();
            TopLevelCollections();
            StringEncodings();
        }

        private static void MemberCollections()
        {
            // A Dictionary with no removals enumerates its entries array in insertion
            // order, so the serialized bytes are stable on both sides of the diff.
            var counts = new Dictionary<string, int>();
            counts.Add("alpha", 1);
            counts.Add("beta", 2);
            counts.Add("gamma", 3);

            var src = new Bag
            {
                Numbers = new int[] { 5, -6, 7 },
                Words = new List<string> { "one", "two" },
                Counts = counts,
                Blob = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF },
            };
            byte[] bin = MemoryPackSerializer.Serialize(src);
            Console.WriteLine("[bag] len=" + bin.Length + " hex=" + Convert.ToHexString(bin));

            var back = MemoryPackSerializer.Deserialize<Bag>(bin);
            Console.WriteLine("[bag] numbers=" + string.Join(",", back.Numbers));
            Console.WriteLine("[bag] words=" + string.Join(",", back.Words));
            Console.WriteLine("[bag] alpha=" + back.Counts["alpha"] + " gamma=" + back.Counts["gamma"]
                + " count=" + back.Counts.Count);
            Console.WriteLine("[bag] blob=" + Convert.ToHexString(back.Blob));
        }

        private static void ScalarMembers()
        {
            var src = new Scalars
            {
                MaybeInt = 42,
                Rank = Rank.High,
                Id = new Guid("0f1e2d3c-4b5a-6978-8796-a5b4c3d2e1f0"),
                Stamp = new DateTime(2020, 2, 29, 13, 45, 6, DateTimeKind.Utc),
                Span = TimeSpan.FromMilliseconds(1234567),
                Pair = new KeyValuePair<string, int>("kv", 9),
            };
            byte[] bin = MemoryPackSerializer.Serialize(src);
            Console.WriteLine("[scalars] len=" + bin.Length + " hex=" + Convert.ToHexString(bin));

            var back = MemoryPackSerializer.Deserialize<Scalars>(bin);
            Console.WriteLine("[scalars] maybe=" + back.MaybeInt.Value + " rank=" + back.Rank
                + " id=" + back.Id.ToString("D"));
            Console.WriteLine("[scalars] stamp=" + back.Stamp.ToString("O") + " kind=" + back.Stamp.Kind
                + " span=" + back.Span.ToString("c"));
            Console.WriteLine("[scalars] pair=" + back.Pair.Key + "/" + back.Pair.Value);

            var none = new Scalars { MaybeInt = null };
            var noneBack = MemoryPackSerializer.Deserialize<Scalars>(MemoryPackSerializer.Serialize(none));
            Console.WriteLine("[scalars] hasValue=" + noneBack.MaybeInt.HasValue);
        }

        private static void TopLevelCollections()
        {
            byte[] arr = MemoryPackSerializer.Serialize(new int[] { 1, 2, 3 });
            Console.WriteLine("[top-array] hex=" + Convert.ToHexString(arr)
                + " back=" + string.Join(",", MemoryPackSerializer.Deserialize<int[]>(arr)));

            byte[] list = MemoryPackSerializer.Serialize(new List<string> { "a", "bb" });
            Console.WriteLine("[top-list] hex=" + Convert.ToHexString(list)
                + " back=" + string.Join(",", MemoryPackSerializer.Deserialize<List<string>>(list)));

            byte[] str = MemoryPackSerializer.Serialize("plain");
            Console.WriteLine("[top-string] hex=" + Convert.ToHexString(str)
                + " back=" + MemoryPackSerializer.Deserialize<string>(str));
        }

        private static void StringEncodings()
        {
            // The default is Utf8; Utf16 is the raw-copy path. Both must round-trip a
            // non-BMP scalar, which is where a surrogate-unaware encoder would diverge.
            string s = "aAé日\U0001F600";
            byte[] utf8 = MemoryPackSerializer.Serialize(s, MemoryPackSerializerOptions.Utf8);
            byte[] utf16 = MemoryPackSerializer.Serialize(s, MemoryPackSerializerOptions.Utf16);
            Console.WriteLine("[enc] utf8=" + Convert.ToHexString(utf8));
            Console.WriteLine("[enc] utf16=" + Convert.ToHexString(utf16));
            Console.WriteLine("[enc] back8=" + MemoryPackSerializer.Deserialize<string>(utf8,
                MemoryPackSerializerOptions.Utf8));
            Console.WriteLine("[enc] back16=" + MemoryPackSerializer.Deserialize<string>(utf16,
                MemoryPackSerializerOptions.Utf16));
        }
    }
}
