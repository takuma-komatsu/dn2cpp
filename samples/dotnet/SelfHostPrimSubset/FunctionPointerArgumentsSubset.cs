using System;
using System.Globalization;
using System.Reflection;

namespace FunctionPointerArgumentsSubset;

internal static class Program
{
    private delegate Type[] Query(Type type);
    private static Type[] Direct(Type type) => throw new InvalidOperationException("fixture missing");
    private static Type[] Pointer(Type type) => throw new InvalidOperationException("fixture missing");
    private static Query Address() => throw new InvalidOperationException("fixture missing");
    private static Type[] DelegateRoute(Type type) => Address()(type);
    private static Type[] Reflect(Type type) => (Type[])Address().Method.Invoke(null, new object[] { type });

    private static unsafe Type[] Inputs() => new[]
    {
        typeof(delegate*<void>), typeof(delegate*<int>), typeof(delegate*<int, string, long>),
        typeof(delegate* unmanaged[Cdecl]<int, double>), typeof(delegate* unmanaged<void>),
        typeof(delegate*<int*, ref int, void>), typeof(delegate*<delegate*<int, void>, int>)
    };

    private static Type[] Public(Type type)
    {
        Type[] parameters = type.GetFunctionPointerParameterTypes();
        Type[] result = new Type[parameters.Length + 1];
        result[0] = type.GetFunctionPointerReturnType();
        for (int i = 0; i < parameters.Length; i++) result[i + 1] = parameters[i];
        return result;
    }

    private static string Rows(Type[] types)
    {
        string text = "";
        for (int i = 0; i < types.Length; i++)
        {
            if (i != 0) text += ";";
            text += types[i].ToString();
        }
        return text;
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Function pointer combined arguments public contract ==");
        Type[] inputs = Inputs();
        for (int i = 0; i < inputs.Length; i++)
            Console.WriteLine("fnptr-arguments-public-" + i + ":" + Rows(Public(inputs[i])));
        Console.WriteLine("Function pointer combined arguments public contract end");
    }

    private static void Check(string route, Func<Type, Type[]> query)
    {
        Type[] inputs = Inputs();
        for (int i = 0; i < inputs.Length; i++)
        {
            Type[] expected = Public(inputs[i]);
            Type[] first = query(inputs[i]);
            Type[] second = query(inputs[i]);
            bool ok = first.GetType() == typeof(Type[]) && second.GetType() == typeof(Type[])
                && !ReferenceEquals(first, second) && first.Length == expected.Length && second.Length == expected.Length;
            for (int j = 0; j < expected.Length && ok; j++)
                ok = ReferenceEquals(first[j], expected[j]) && ReferenceEquals(second[j], expected[j]);
            // Even zero-parameter inputs have a mutable return-type slot, in an owned array.
            first[0] = null;
            Type[] third = query(inputs[i]);
            ok &= second[0] == expected[0] && third[0] == expected[0] && !ReferenceEquals(second, third);
            Console.WriteLine("fnptr-arguments-" + route + "-" + i + ":" + ok);
        }
        foreach (Type type in new Type[] { null, typeof(int) })
        {
            bool ok = false;
            try { query(type); }
            catch (Exception error)
            {
                bool wrapped = error is TargetInvocationException;
                Exception leaf = wrapped ? error.InnerException : error;
                ok = wrapped == (route == "reflection")
                    && (type == null ? leaf is NullReferenceException : leaf is InvalidOperationException);
            }
            Console.WriteLine("fnptr-arguments-" + route + "-" + (type == null ? "null" : "nonfnptr") + ":" + ok);
        }
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
