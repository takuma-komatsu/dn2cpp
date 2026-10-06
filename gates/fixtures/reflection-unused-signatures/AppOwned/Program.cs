using System;
using System.Globalization;
using System.Reflection;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== unused generic signature ==");
        Console.WriteLine("used=" + Subject.Used(41));
        Console.WriteLine("supported=" + Subject.Supported<int>(7));
        MethodInfo method = typeof(Subject).GetMethod("Supported")!;
        Console.WriteLine("definition=" + method.IsGenericMethodDefinition
            + " parameter=" + method.GetGenericArguments()[0].Name);
        Console.WriteLine("unused generic signature end");
    }
}

internal static class Subject
{
    public static int Used(int value) => value + 1;
    public static U Supported<U>(U value) => value;
    public static void Unused<U>(Ext.Box<U> value) { }
}
