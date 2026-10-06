using System;
using System.Globalization;
using System.Reflection;

namespace ReflectionOpenSignature;

public sealed class OpenHolder<T> : ReflectionUnusedSignature.Base { }

public static class Subject
{
    public static int Used() => 42;
    public static OpenHolder<U> Pick<U>() => null!;
}

internal static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== open signature base layout ==");
        Console.WriteLine("used=" + Subject.Used());
        MethodInfo? method = typeof(Subject).GetMethod("Pick");
        Console.WriteLine("found=" + (method is not null));
        if (method is not null)
        {
            Console.WriteLine("definition=" + method.IsGenericMethodDefinition);
            Console.WriteLine("formal=" + method.GetGenericArguments()[0].Name);
            if (args.Length > 0 && args[0] == "return-type")
            {
                try
                {
                    Type result = method.ReturnType;
                    Console.WriteLine("return=" + result.Name + " contains=" + result.ContainsGenericParameters);
                }
                catch (Exception error)
                {
                    Console.WriteLine("return exception=" + error.GetType().Name);
                }
            }
        }
        Console.WriteLine("open signature base layout end");
    }
}
