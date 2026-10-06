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
        Console.WriteLine("used=" + ReflectionUnusedSignature.Subject.Used(41));
        Console.WriteLine("supported=" + ReflectionUnusedSignature.Subject.Supported<int>(7));
        MethodInfo method = typeof(ReflectionUnusedSignature.Subject).GetMethod("Supported")!;
        Console.WriteLine("definition=" + method.IsGenericMethodDefinition
            + " parameter=" + method.GetGenericArguments()[0].Name);
        Console.WriteLine("unused generic signature end");
    }
}
