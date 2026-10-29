using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeUtf8NameSubset;

internal static class Program
{
    public class Generic<T> { public class Nested<U> { } }
    public class 日本語 { }
    private delegate object Query(Type type);
    private static void Direct(Type type) => throw new InvalidOperationException("fixture missing");
    private static void Pointer(Type type) => throw new InvalidOperationException("fixture missing");
    private static Query Address() => throw new InvalidOperationException("fixture missing");
    private static void DelegateRoute(Type type) { Address()(type); }
    private static void Reflect(Type type) { Address().Method.Invoke(null, new object[] { type }); }

    private static unsafe Type[] Inputs() => new[]
    {
        typeof(object), typeof(Generic<>), typeof(Generic<int>), typeof(Generic<int>.Nested<string>),
        typeof(日本語), typeof(Generic<>).GetGenericArguments()[0], typeof(int*), typeof(int).MakeByRefType(),
        typeof(delegate*<int, void>), typeof(int[]), typeof(int[,]), null
    };

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

    private static void Check(string route, Action<Type> query)
    {
        Type[] types = Inputs();
        for (int i = 0; i < types.Length; i++)
        {
            bool ok = false;
            try { query(types[i]); }
            catch (Exception error)
            {
                bool wrapped = error is TargetInvocationException;
                Exception leaf = wrapped ? error.InnerException : error;
                bool expected = i == 11 ? leaf is NullReferenceException
                    : i >= 5 ? leaf.GetType() == typeof(ArgumentException)
                        && ((ArgumentException)leaf).ParamName == null && leaf.Message == "Invalid handle."
                    : leaf is PlatformNotSupportedException
                        && leaf.Message == "RuntimeTypeHandle.GetUtf8Name requires CoreCLR metadata-name pointers. Use Type.Name instead.";
                ok = expected && wrapped == (route == "reflection");
            }
            Console.WriteLine("utf8-name-" + route + "-" + i + ":" + ok);
        }
    }

    private static void ReflectionShape()
    {
        Type result = Address().Method.ReturnType;
        Console.WriteLine("utf8-name-reflection-shape:" + (result.IsValueType && result.FullName == "System.MdUtf8String"));
    }

    // Independent entry proves value-return layout without address/reflection roots.
    internal static void RunDirectChecks(string[] args)
    {
        Run();
        Check("direct", Direct);
    }

    internal static void RunArtifactChecks(string[] args)
    {
        Run();
        Check("direct", Direct);
        Check("calli", Pointer);
        Check("delegate", DelegateRoute);
        ReflectionShape();
        Check("reflection", Reflect);
    }

    internal static void RunReflectionChecks(string[] args)
    {
        Run();
        ReflectionShape();
        Check("reflection", Reflect);
    }
}
