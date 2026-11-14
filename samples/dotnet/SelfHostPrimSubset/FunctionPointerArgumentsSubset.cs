using System;
using System.Globalization;
using System.Reflection;

namespace FunctionPointerArgumentsSubset;

internal static class Program
{
    private delegate Type[] Query(Type type);

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
}
