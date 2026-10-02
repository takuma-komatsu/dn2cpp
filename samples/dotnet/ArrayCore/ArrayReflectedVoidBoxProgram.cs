using System;
using System.Globalization;
using System.Runtime.CompilerServices;

internal static class Program
{
    public static void VoidMethod()
    {
    }

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        VoidMethod();
        int value = 7;
        try
        {
            object boxed = RuntimeHelpers.Box(ref Unsafe.As<int, byte>(ref value),
                typeof(Program).GetMethod(nameof(VoidMethod)).ReturnType.TypeHandle);
            Array array = Array.CreateInstance(boxed.GetType(), 2);
            Console.WriteLine("reflected void search=" + Array.IndexOf(array, null));
        }
        catch (ArgumentException error)
        {
            Console.WriteLine("reflected void box=" + error.GetType().Name);
        }
        Console.WriteLine("reflected void box end");
    }
}
