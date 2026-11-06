using System;
using System.Globalization;
using System.Reflection;

namespace StaticGenericVariablesSubset;

internal static class Program
{
    public class Pair<T, U> { }
    public class G<T>
    {
        public class N<U> { }
        public static void M<U>() { }
    }

    private delegate bool Query(Type type);
    private static bool Direct(Type type) => throw new InvalidOperationException("fixture missing");
    private static bool Pointer(Type type) => throw new InvalidOperationException("fixture missing");
    private static Query Address() => throw new InvalidOperationException("fixture missing");
    private static bool DelegateRoute(Type type) => Address()(type);
    private static bool Reflect(Type type) => (bool)Address().Method.Invoke(null, new object[] { type });

    private static unsafe Type[] Inputs()
    {
        Type formal = typeof(G<>).GetGenericArguments()[0];
        Type methodFormal = typeof(G<int>).GetMethod("M").GetGenericArguments()[0];
        return new[]
        {
            typeof(int), typeof(object), typeof(void), typeof(G<>), typeof(G<int>),
            typeof(G<>.N<>), typeof(G<int>.N<string>), formal, methodFormal,
            typeof(Pair<,>).MakeGenericType(typeof(int), formal), formal.MakeArrayType(),
            methodFormal.MakeArrayType(2), formal.MakePointerType(), methodFormal.MakeByRefType(),
            formal.MakeArrayType().MakePointerType().MakeByRefType(), typeof(int[]),
            typeof(int[,]), typeof(int*), typeof(int).MakeByRefType(), typeof(delegate*<int, int>)
        };
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Static generic variables public contract ==");
        Type[] inputs = Inputs();
        for (int i = 0; i < inputs.Length; i++)
            Console.WriteLine("static-generic-public-" + i + ":" + inputs[i].ContainsGenericParameters);
        Console.WriteLine("Static generic variables public contract end");
    }

    private static void Check(string route, Func<Type, bool> query)
    {
        Type[] inputs = Inputs();
        for (int i = 0; i < inputs.Length; i++)
            Console.WriteLine("static-generic-" + route + "-" + i + ":"
                + (query(inputs[i]) == inputs[i].ContainsGenericParameters));
        bool ok = false;
        try { query(null); }
        catch (Exception error)
        {
            bool wrapped = error is TargetInvocationException;
            ok = wrapped == (route == "reflection")
                && (wrapped ? error.InnerException : error) is NullReferenceException;
        }
        Console.WriteLine("static-generic-" + route + "-null:" + ok);
    }

    internal static void RunArtifactChecks(string[] args)
    {
        Run();
        Check("direct", Direct);
        Check("calli", Pointer);
        Check("delegate", DelegateRoute);
        Check("reflection", Reflect);
    }

    internal static void RunReflectionChecks(string[] args)
    {
        Run();
        Check("reflection", Reflect);
    }
}
