using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RuntimeTypeHandleNameSubset;

public static unsafe class NameChecks
{
    public class Outer<T> { public class Inner<U> { } }
    public delegate string Bound(int flags);
    delegate string Route(ref RuntimeTypeHandle handle, int flags);
    static string Public(ref RuntimeTypeHandle handle, int flags)
    {
        Type type = Type.GetTypeFromHandle(handle);
        if (type == null) throw new ArgumentNullException(null, "Invalid handle.");
        return flags == 0 ? type.Name : flags == 1 ? type.ToString() : flags == 3 ? type.FullName : type.AssemblyQualifiedName;
    }
    static Type[] Supported() => new Type[] { typeof(int), typeof(void), typeof(Outer<>), typeof(Outer<int>.Inner<string>),
        typeof(int[]), typeof(int[,]), typeof(int*), typeof(int).MakeByRefType() };
    static Type[] PublicNullTypes()
    {
        Type formal = typeof(Outer<>).GetGenericArguments()[0];
        return new[] { formal, typeof(Dictionary<,>).MakeGenericType(typeof(string), formal), typeof(delegate* managed<int, void>) };
    }
    static void Culture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
    public static void Run()
    {
        Culture(); Console.WriteLine("== RuntimeTypeHandle names ==");
        int index = 0;
        foreach (Type type in Supported())
        {
            RuntimeTypeHandle handle = type.TypeHandle;
            foreach (int flags in new int[] { 0, 1, 3, 7 })
                Console.WriteLine("type-name-public-" + index + "-" + flags + ":" + Public(ref handle, flags));
            index++;
        }
        index = 0;
        foreach (Type type in PublicNullTypes())
        {
            Console.WriteLine("type-name-public-domain-" + index + "-name:" + type.Name);
            Console.WriteLine("type-name-public-domain-" + index + "-display:" + type.ToString());
            Console.WriteLine("type-name-public-domain-" + index + "-null:" + (type.FullName == null && type.AssemblyQualifiedName == null));
            index++;
        }
        Console.WriteLine("RuntimeTypeHandle names end");
    }
}
