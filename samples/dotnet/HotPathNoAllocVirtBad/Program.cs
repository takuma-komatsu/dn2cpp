using System;

namespace Dn2Cpp.Runtime
{
    // Internal copy of Dn2Cpp.Runtime.HotPathAttribute (matched by full name only).
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class HotPathAttribute : Attribute
    {
        public bool SkipBoundsChecks { get; set; }
        public bool NoAlloc { get; set; }
    }
}

namespace HotPathNoAllocVirtBad
{
    using Dn2Cpp.Runtime;

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

    /// <summary>A [HotPath(NoAlloc = true)] method that dispatches dynamically:
    /// <c>s.Sides()</c> is a virtual (here abstract) call resolved through the
    /// receiver's vtable, whose target is not statically provable. Transpiling
    /// this must FAIL (error: / exit 2), naming the method and the dispatch. The
    /// receiver's runtime type is chosen at run time so no devirtualization is
    /// possible. The program is never built to native.</summary>
    internal static class Program
    {
        [HotPath(NoAlloc = true)]
        private static int CountSides(Shape s)
        {
            return s.Sides();
        }

        private static void Main(string[] args)
        {
            Shape s = args.Length > 100 ? new Square() : new Triangle();
            Console.WriteLine(CountSides(s));
        }
    }
}
