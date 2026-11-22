#if METHODIMPL_SHAPES_ONLY
using System;
using System.Globalization;

internal static class MethodImplShapesOnlyProgram
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        MethodImplShapeSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-static-method-groups")
            return;

        Console.WriteLine("== constrained static method groups ==");
        Show(Group<RefFirst>());
        Show(Group<RefSecond>());
        Show(Group<ValueImpl>());
    }

    private static Func<string> Group<T>() where T : IStat<T> => T.SA;

    private static void Show(Func<string> bound)
    {
        var method = bound.Method;
        Console.WriteLine(bound() + ":" + method.DeclaringType.Name + "." + method.Name);
    }

    private interface IStat<T> where T : IStat<T>
    {
        static abstract string SA();
    }

    private sealed class RefFirst : IStat<RefFirst>
    {
        public static string SA() => "first";
    }

    private sealed class RefSecond : IStat<RefSecond>
    {
        public static string SA() => "second";
    }

    private struct ValueImpl : IStat<ValueImpl>
    {
        public static string SA() => "value";
    }
}
#endif
