using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeUtf8NameSubset;

internal static class Program
{
    public class Generic<T> { public class Nested<U> { } }
    public class 日本語 { }
    private delegate object Query(Type type);

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== UTF8 name public contract ==");
        Type[] types = { typeof(object), typeof(Generic<>), typeof(Generic<int>), typeof(Generic<int>.Nested<string>), typeof(日本語) };
        for (int i = 0; i < types.Length; i++)
            Console.WriteLine("utf8-name-public-" + i + ":" + types[i].Name);
        Console.WriteLine("UTF8 name public contract end");
    }
}
