using System;
using XGenericMethodLib;

namespace XAsmGenericMethod
{
    internal struct Point
    {
        public int X;
        public int Y;
    }

    // Calls a generic method on a non-generic type in another assembly: each site
    // is a MethodSpec over a MemberRef with a TypeReference parent, and the
    // template is instantiated in the library's module with the call-site args.
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine(Lib.Echo<int>(42));               // 42
            Console.WriteLine(Lib.Pick<int>(true, 5, 9));       // 5
            Console.WriteLine(Lib.Pick<int>(false, 5, 9));      // 9
            Console.WriteLine(Lib.PairTag<int, bool>(1, true)); // 2

            // A user value type as the argument: monomorphized in the library.
            Point p = Lib.Echo<Point>(new Point { X = 3, Y = 4 });
            Console.WriteLine(p.X + p.Y);                       // 7
        }
    }
}
