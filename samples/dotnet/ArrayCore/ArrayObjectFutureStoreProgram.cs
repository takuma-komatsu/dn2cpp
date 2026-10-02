using System;
using System.Globalization;
using System.Runtime.InteropServices;
internal static class Program
{
    private struct UniqueModulo
    {
        public int Value;
        public override bool Equals(object value) => value is UniqueModulo other && Value % 10 == other.Value % 10;
        public override int GetHashCode() => Value % 10;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct FutureUnsupported
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public int[] Values;
    }
    private static object[] escaped;
    private static void Touch() => GC.KeepAlive(escaped);
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        object[] selector = new object[] { new UniqueModulo { Value = 0 } };
        escaped = selector;
        Touch();
        object before = selector[0];
        selector[0] = default(FutureUnsupported);
        Array array = Array.CreateInstance(before.GetType(), 2);
        GC.Collect();
        object needle = new UniqueModulo { Value = 10 };
        Console.WriteLine(array.GetType().Name + ":" + Array.IndexOf(array, needle) + ":" + Array.LastIndexOf(array, needle));
        Console.WriteLine("ArrayObjectFutureUnsupportedOnly end");
    }
}
