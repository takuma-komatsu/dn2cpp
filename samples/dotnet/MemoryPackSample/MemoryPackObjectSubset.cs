using System;
using MemoryPack;

namespace MemoryPackObjectSubset
{
    [MemoryPackable]
    internal partial class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Score { get; set; }

        [MemoryPackIgnore]
        public string Transient { get; set; }
    }

    [MemoryPackable]
    internal partial struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    [MemoryPackable]
    internal partial record Tag(string Key, int Weight);

    [MemoryPackable]
    internal partial class Ordered
    {
        [MemoryPackOrder(1)]
        public int Second { get; set; }

        [MemoryPackOrder(0)]
        public int First { get; set; }
    }

    [MemoryPackable]
    internal partial class Ctored
    {
        public int A { get; }
        public string B { get; }

        [MemoryPackConstructor]
        public Ctored(int a, string b)
        {
            A = a;
            B = b;
        }
    }

    [MemoryPackable]
    internal partial class Hooked
    {
        public int Value { get; set; }

        [MemoryPackOnSerializing]
        private void OnSerializing() => Console.WriteLine("[hook] serializing " + Value);

        [MemoryPackOnSerialized]
        private void OnSerialized() => Console.WriteLine("[hook] serialized " + Value);

        [MemoryPackOnDeserializing]
        private static void OnDeserializing() => Console.WriteLine("[hook] deserializing");

        [MemoryPackOnDeserialized]
        private void OnDeserialized() => Console.WriteLine("[hook] deserialized " + Value);
    }

    [MemoryPackable]
    internal partial class Nested
    {
        public Person Owner { get; set; }
        public Point Where { get; set; }
    }

    internal static class Program
    {
        internal static void __GateEntry()
        {
            ReferenceType();
            ValueTypeAndRecord();
            OrderAndConstructor();
            Callbacks();
            NestedGraph();
            NullAndEmpty();
        }

        private static void ReferenceType()
        {
            var src = new Person { Id = 7, Name = "ada", Score = 12.5, Transient = "dropped" };
            byte[] bin = MemoryPackSerializer.Serialize(src);
            Console.WriteLine("[obj] len=" + bin.Length + " hex=" + Convert.ToHexString(bin));
            var back = MemoryPackSerializer.Deserialize<Person>(bin);
            Console.WriteLine("[obj] id=" + back.Id + " name=" + back.Name
                + " score=" + back.Score.ToString("R") + " transient=" + (back.Transient ?? "<null>"));
        }

        private static void ValueTypeAndRecord()
        {
            byte[] pt = MemoryPackSerializer.Serialize(new Point { X = -3, Y = 9 });
            Console.WriteLine("[struct] len=" + pt.Length + " hex=" + Convert.ToHexString(pt));
            Point p = MemoryPackSerializer.Deserialize<Point>(pt);
            Console.WriteLine("[struct] x=" + p.X + " y=" + p.Y);

            byte[] tag = MemoryPackSerializer.Serialize(new Tag("hp", 3));
            Console.WriteLine("[record] len=" + tag.Length + " hex=" + Convert.ToHexString(tag));
            var t = MemoryPackSerializer.Deserialize<Tag>(tag);
            Console.WriteLine("[record] key=" + t.Key + " weight=" + t.Weight
                + " eq=" + t.Equals(new Tag("hp", 3)));
        }

        private static void OrderAndConstructor()
        {
            byte[] ord = MemoryPackSerializer.Serialize(new Ordered { First = 1, Second = 2 });
            Console.WriteLine("[order] hex=" + Convert.ToHexString(ord));
            var o = MemoryPackSerializer.Deserialize<Ordered>(ord);
            Console.WriteLine("[order] first=" + o.First + " second=" + o.Second);

            byte[] ct = MemoryPackSerializer.Serialize(new Ctored(41, "ctor"));
            var c = MemoryPackSerializer.Deserialize<Ctored>(ct);
            Console.WriteLine("[ctor] a=" + c.A + " b=" + c.B);
        }

        private static void Callbacks()
        {
            byte[] bin = MemoryPackSerializer.Serialize(new Hooked { Value = 5 });
            var h = MemoryPackSerializer.Deserialize<Hooked>(bin);
            Console.WriteLine("[hook] value=" + h.Value);
        }

        private static void NestedGraph()
        {
            var src = new Nested
            {
                Owner = new Person { Id = 1, Name = "root", Score = 0.5 },
                Where = new Point { X = 2, Y = 4 },
            };
            byte[] bin = MemoryPackSerializer.Serialize(src);
            Console.WriteLine("[nested] len=" + bin.Length + " hex=" + Convert.ToHexString(bin));
            var back = MemoryPackSerializer.Deserialize<Nested>(bin);
            Console.WriteLine("[nested] owner=" + back.Owner.Name + " x=" + back.Where.X
                + " y=" + back.Where.Y);
        }

        private static void NullAndEmpty()
        {
            byte[] nul = MemoryPackSerializer.Serialize<Person>(null);
            Console.WriteLine("[null] len=" + nul.Length + " hex=" + Convert.ToHexString(nul));
            Console.WriteLine("[null] back=" + (MemoryPackSerializer.Deserialize<Person>(nul) is null));

            byte[] empty = MemoryPackSerializer.Serialize(new Person { Name = null });
            var back = MemoryPackSerializer.Deserialize<Person>(empty);
            Console.WriteLine("[empty] name=" + (back.Name is null ? "<null>" : back.Name)
                + " id=" + back.Id);
        }
    }
}
