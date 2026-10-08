using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using HotGvmCall.Library;

namespace HotGvmCall;

internal static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Console.WriteLine("late layout base");
        if (args.Length > 0 && args[0] == "--prefix")
            return;

        Console.WriteLine("== late retained default layout ==");
        object probe = args.Length == 0 ? "x" : new object();
        Console.WriteLine(probe is ILateLayoutBin<string>);
        Unsafe.SkipInit(out GvmCallCell<string> value);
        Console.WriteLine("ok");
        Console.WriteLine("late retained default layout end");

        Console.WriteLine("== late NoAlloc closure ==");
        Console.WriteLine("warm helper=" + LateNoAllocHelper.Count());
        Console.WriteLine("late NoAlloc closure end");
    }
}
