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
}
