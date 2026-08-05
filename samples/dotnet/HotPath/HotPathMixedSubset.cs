#nullable disable
using System;
using System.Collections.Generic;
using Dn2Cpp.Runtime;

namespace HotPathMixedSubset
{
    internal abstract class Shape
    {
        public abstract int Sides();
    }

    internal sealed class Triangle : Shape
    {
        public override int Sides() => 3;
    }

    internal sealed class Square : Shape
    {
        public override int Sides() => 4;
    }

    // The bare attribute imposes no constraints: a marked body may allocate,
    // dispatch virtually, and throw/catch — semantics stay exactly real .NET's.
    internal static class Program
    {
        [HotPath]
        private static int BuildAndCount(int n)
        {
            var list = new List<Shape>();
            for (int i = 0; i < n; i++)
                list.Add(i % 2 == 0 ? (Shape)new Triangle() : new Square());
            int total = 0;
            foreach (var s in list)
                total += s.Sides();
            try
            {
                if (total > 0)
                    throw new InvalidOperationException("sides=" + total);
                return -1;
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e.Message);
                return total;
            }
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(BuildAndCount(7));
        }
    }
}
