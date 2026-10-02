using System;
using System.Globalization;
using System.Runtime.InteropServices;

internal static class Program
{
    private struct SelectedModulo
    {
        public int Value;
        public override bool Equals(object value) => value is SelectedModulo other
            && Value % 10 == other.Value % 10;
        public override int GetHashCode() => Value % 10;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FutureUnsupported
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public int[] Values;
    }

    private static object[] _escaped;

    private static void Touch() => GC.KeepAlive(_escaped);

    private static void Check(Type type)
    {
        Array values = Array.CreateInstance(type, 2);
        GC.Collect();
        object needle = new SelectedModulo { Value = 10 };
        Console.WriteLine("array prior store=" + Array.IndexOf(values, needle)
            + ":" + Array.LastIndexOf(values, needle));
    }

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#if ARRAY_PRIOR_NULL
        Type[] selector = new Type[1];
#else
        Type[] selector = new Type[] { typeof(SelectedModulo) };
#endif
        _escaped = new object[] { selector };
        Touch();
        Type before = selector[0];
        selector[0] = typeof(FutureUnsupported);
#if ARRAY_PRIOR_NULL
        try
        {
            Check(before);
        }
        catch (ArgumentNullException error)
        {
            Console.WriteLine("array prior null=" + error.GetType().Name + ":" + error.ParamName);
        }
        Console.WriteLine("array prior null end");
#else
        Check(before);
        Console.WriteLine("array prior store end");
#endif
    }
}
