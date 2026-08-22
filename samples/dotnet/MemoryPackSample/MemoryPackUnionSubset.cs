using System;
using System.Collections.Generic;
using MemoryPack;

namespace MemoryPackUnionSubset
{
    [MemoryPackable]
    [MemoryPackUnion(0, typeof(Circle))]
    [MemoryPackUnion(1, typeof(Rect))]
    internal partial interface IShape
    {
        int Area { get; }
    }

    [MemoryPackable]
    internal partial class Circle : IShape
    {
        public int Radius { get; set; }

        [MemoryPackIgnore]
        public int Area => 3 * Radius * Radius;
    }

    [MemoryPackable]
    internal partial class Rect : IShape
    {
        public int W { get; set; }
        public int H { get; set; }

        [MemoryPackIgnore]
        public int Area => W * H;
    }

    [MemoryPackable]
    [MemoryPackUnion(10, typeof(Cat))]
    [MemoryPackUnion(20, typeof(Dog))]
    internal abstract partial class Animal
    {
        public string Name { get; set; }
    }

    [MemoryPackable]
    internal partial class Cat : Animal
    {
        public int Lives { get; set; }
    }

    [MemoryPackable]
    internal partial class Dog : Animal
    {
        public bool Loyal { get; set; }
    }

    [MemoryPackable]
    internal partial class Zoo
    {
        public List<Animal> Animals { get; set; }
    }

    internal static class Program
    {
        internal static void __GateEntry()
        {
            InterfaceUnion();
            AbstractUnion();
            UnionInsideCollection();
            NullUnion();
        }

        private static void InterfaceUnion()
        {
            IShape[] shapes = new IShape[] { new Circle { Radius = 4 }, new Rect { W = 2, H = 5 } };
            foreach (IShape s in shapes)
            {
                byte[] bin = MemoryPackSerializer.Serialize(s);
                IShape back = MemoryPackSerializer.Deserialize<IShape>(bin);
                Console.WriteLine("[iface] hex=" + Convert.ToHexString(bin)
                    + " type=" + back.GetType().Name + " area=" + back.Area);
            }
        }

        private static void AbstractUnion()
        {
            Animal a = new Cat { Name = "mimi", Lives = 9 };
            byte[] bin = MemoryPackSerializer.Serialize(a);
            Animal back = MemoryPackSerializer.Deserialize<Animal>(bin);
            Console.WriteLine("[abstract] hex=" + Convert.ToHexString(bin)
                + " type=" + back.GetType().Name + " name=" + back.Name
                + " lives=" + ((Cat)back).Lives);
        }

        private static void UnionInsideCollection()
        {
            var zoo = new Zoo
            {
                Animals = new List<Animal>
                {
                    new Dog { Name = "rex", Loyal = true },
                    new Cat { Name = "tom", Lives = 7 },
                },
            };
            byte[] bin = MemoryPackSerializer.Serialize(zoo);
            Console.WriteLine("[zoo] len=" + bin.Length + " hex=" + Convert.ToHexString(bin));
            var back = MemoryPackSerializer.Deserialize<Zoo>(bin);
            foreach (Animal animal in back.Animals)
            {
                Console.WriteLine("[zoo] " + animal.GetType().Name + " " + animal.Name);
            }
        }

        private static void NullUnion()
        {
            byte[] bin = MemoryPackSerializer.Serialize<IShape>(null);
            Console.WriteLine("[union-null] hex=" + Convert.ToHexString(bin)
                + " back=" + (MemoryPackSerializer.Deserialize<IShape>(bin) is null));
        }
    }
}
