using System;
using System.Globalization;
using System.Reflection;

namespace GenericParameterPositionSubset;

internal static class Program
{
    private class PositionOwner<T, U>
    {
        public class Nested<V> { public static void Other<A, B>() { } }
        public static void Method<A, B>() { }
    }
    private class PositionChild : PositionOwner<int, string> { }
    private const string NotParameter = "Method may only be called on a Type for which Type.IsGenericParameter is true.";

    private static void Check(string label, bool success)
    {
        Console.WriteLine("generic-position-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static bool Nonformal(Exception error) => error is InvalidOperationException
        && error.Message == NotParameter && error.HResult == unchecked((int)0x80131509);
    private static bool NullReceiver(Exception error) => error is NullReferenceException
        && error.HResult == unchecked((int)0x80004003) && error.Message == "Object reference not set to an instance of an object.";
    private static int Public(Type type) => type.GenericParameterPosition;

    private static void Route(string route, Func<Type, int> query)
    {
        Type[] closed = typeof(PositionOwner<int, string>).GetMethod("Method").GetGenericArguments();
        Type[] inherited = typeof(PositionChild).GetMethod("Method", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetGenericArguments();
        Type[] typical = typeof(PositionOwner<,>).GetMethod("Method").GetGenericArguments();
        Check(route + "-method", query(closed[0]) == 0 && query(closed[1]) == 1);
        Check(route + "-typical", ReferenceEquals(closed[0], typical[0]) && ReferenceEquals(inherited[1], typical[1])
            && query(inherited[1]) == 1 && closed[0].DeclaringType == typeof(PositionOwner<,>));
        Type[] formals = typeof(PositionOwner<,>).GetGenericArguments();
        Check(route + "-type", query(formals[0]) == 0 && query(formals[1]) == 1);
        Type[] nested = typeof(PositionOwner<,>.Nested<>).GetGenericArguments();
        Check(route + "-nested", query(nested[0]) == 0 && query(nested[1]) == 1 && query(nested[2]) == 2
            && nested[0].DeclaringType == typeof(PositionOwner<,>.Nested<>));
        Type[] method = typeof(PositionOwner<int, string>.Nested<double>).GetMethod("Other").GetGenericArguments();
        Check(route + "-nested-method", query(method[0]) == 0 && query(method[1]) == 1);
        Type reordered = typeof(PositionOwner<,>).MakeGenericType(new[] { formals[1], formals[0] });
        Type[] arguments = reordered.GetGenericArguments();
        Check(route + "-reordered", query(arguments[0]) == 1 && query(arguments[1]) == 0
            && ReferenceEquals(arguments[0], formals[1]) && ReferenceEquals(arguments[1], formals[0]));
        Check(route + "-repeated", query(formals[1]) == query(formals[1]) && query(closed[1]) == query(typical[1]));
        Type[] nonformals = { typeof(int), typeof(void), typeof(PositionOwner<,>), typeof(PositionOwner<int, string>), reordered,
            typeof(int[]), typeof(int[,]), typeof(int).MakePointerType(), typeof(int).MakeByRefType(),
            formals[0].MakeArrayType(), formals[0].MakePointerType(), formals[0].MakeByRefType() };
        for (int i = 0; i < nonformals.Length; i++)
        {
            Type type = nonformals[i];
            Check(route + "-nonformal-" + i, Nonformal(Error(() => query(type))));
        }
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Generic parameter position contract ==");
        Route("public", Public);
        Check("public-null", NullReceiver(Error(() => Public(null))));
        Console.WriteLine("Generic parameter position contract end");
    }
}
