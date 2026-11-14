using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeDeclaringSubset;

public class Container<T>
{
    public class Nested<U> { }
    public class Plain { }
    public enum Choice { One }
    public static void GenericMethod<U>() { }
}
public class OrdinaryOwner { public class Nested { } public enum Choice { One } private class Hidden { } public static Type HiddenType() => typeof(Hidden); }
public static unsafe class DeclaringChecks
{
    public delegate Type Caller(Type type);
    static Type[] Supported() => new Type[] {
        typeof(int), typeof(Container<>), typeof(Container<int>),
        typeof(Container<>).GetGenericArguments()[0],
        typeof(Container<>).GetMethod("GenericMethod").GetGenericArguments()[0],
        typeof(Container<int>).GetMethod("GenericMethod").GetGenericArguments()[0],
        typeof(Container<int>.Plain[]), typeof(Container<int>.Plain[,]), Array.CreateInstance(typeof(Container<int>.Plain), new[] { 1 }, new[] { 1 }).GetType(),
        typeof(Container<int>.Plain).MakePointerType(), typeof(Container<int>.Plain).MakeByRefType(), typeof(delegate* managed<int, void>) };
    static void Culture() { CultureInfo.CurrentCulture=CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture; }
    public static void Run()
    {
        Culture(); Console.WriteLine("== RuntimeType declaring owner =="); int i=0;
        foreach(Type type in Supported()) Console.WriteLine("declaring-type-public-"+(i++)+":"+(type.DeclaringType?.ToString()??"<null>"));
        Console.WriteLine("RuntimeType declaring owner end");
    }
}
