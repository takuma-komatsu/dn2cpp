using System;
using System.Globalization;
using System.Reflection;

namespace FormalDeclaringMethodSubset;

internal static class Program
{
    private class FormalMethodOwner<T>
    {
        public static U Pick<U, V>(U value) => value;
        public static U Other<U>(U value) => value;
    }
    private class FormalMethodChild<T> : FormalMethodOwner<T> { }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("formal-declaring-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (Exception error) { return error; }
        return null;
    }

    private static Type Formal() => typeof(FormalMethodOwner<int>).GetMethod("Pick").GetGenericArguments()[0];
    private static MethodBase Typical() => typeof(FormalMethodOwner<>).GetMethod("Pick");

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Formal declaring method contract ==");
        _ = FormalMethodOwner<int>.Pick<int, string>(3);
        Type formal = Formal();
        MethodBase method = formal.DeclaringMethod;
        Check("public-typical", ReferenceEquals(method, Typical())
            && ReferenceEquals(method.DeclaringType, typeof(FormalMethodOwner<>))
            && ReferenceEquals(method.ReflectedType, typeof(FormalMethodOwner<>)) && method.IsGenericMethodDefinition);
        Check("public-contexts", ReferenceEquals(method, typeof(FormalMethodOwner<string>).GetMethod("Pick").GetGenericArguments()[0].DeclaringMethod)
            && ReferenceEquals(method, typeof(FormalMethodChild<int>).GetMethod("Pick", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetGenericArguments()[0].DeclaringMethod));
        Check("public-positions", ReferenceEquals(method, typeof(FormalMethodOwner<int>).GetMethod("Pick").GetGenericArguments()[1].DeclaringMethod)
            && !ReferenceEquals(method, typeof(FormalMethodOwner<int>).GetMethod("Other").GetGenericArguments()[0].DeclaringMethod));
        Check("public-type-formal", typeof(FormalMethodOwner<>).GetGenericArguments()[0].DeclaringMethod is null);
        Check("public-nonformal", Error(() => _ = typeof(int).DeclaringMethod) is InvalidOperationException);
        Check("public-null", Error(() => _ = ((Type)null).DeclaringMethod) is NullReferenceException);
        Check("public-repeated", ReferenceEquals(method, formal.DeclaringMethod));
        Console.WriteLine("Formal declaring method contract end");
    }
}
