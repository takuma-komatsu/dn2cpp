using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeMakeGenericSubset;

internal static class Program
{
    private class MakeGenericOwner<T> { }
    private class MakeGenericPair<T, U> { }
    private class MakeGenericClass<T> where T : class { }
    private class MakeGenericPlannedField<T> { public T Value; }
    private class MakeGenericForwardedField<T> { public T Value; }

    // No closed-type token roots this layout; the call's known arguments must plan it.
    private static Type Planned() => typeof(MakeGenericPlannedField<>).MakeGenericType(new[] { typeof(int) });
    private static Type Forwarded() => typeof(MakeGenericForwardedField<>).MakeGenericType(new[] { typeof(int) });

    private static void Check(string label, bool success)
    {
        Console.WriteLine("make-generic-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static bool Argument(Exception error, Type kind, string parameter = null) =>
        error is ArgumentException argument && error.GetType() == kind && argument.ParamName == parameter;

    private static Type Public(Type type, Type[] arguments) => type.MakeGenericType(arguments);

    private static void PublicRoute(string route, Func<Type, Type[], Type> query)
    {
        Type owner = typeof(MakeGenericOwner<>);
        Type pair = typeof(MakeGenericPair<,>);
        Type formal = owner.GetGenericArguments()[0];
        Check(route + "-single", ReferenceEquals(query(owner, new[] { typeof(int) }), typeof(MakeGenericOwner<int>)));
        Check(route + "-reference", ReferenceEquals(query(owner, new[] { typeof(string) }), typeof(MakeGenericOwner<string>)));
        Check(route + "-pair", ReferenceEquals(query(pair, new[] { typeof(int), typeof(string) }), typeof(MakeGenericPair<int, string>)));
        Check(route + "-constraint", ReferenceEquals(query(typeof(MakeGenericClass<>), new[] { typeof(string) }), typeof(MakeGenericClass<string>)));
        Type open = query(pair, new[] { typeof(int), formal });
        Check(route + "-open", open.ContainsGenericParameters && open.IsConstructedGenericType
            && open.GetGenericArguments()[0] == typeof(int) && ReferenceEquals(open.GetGenericArguments()[1], formal));
        Check(route + "-repeated", ReferenceEquals(open, query(pair, new[] { typeof(int), formal }))
            && ReferenceEquals(query(owner, new[] { typeof(int) }), query(owner, new[] { typeof(int) })));
        Check(route + "-null-array", Argument(Error(() => query(owner, null)), typeof(ArgumentNullException), "typeArguments"));
        Check(route + "-nondefinition", Error(() => query(typeof(int), Type.EmptyTypes)) is InvalidOperationException);
        Check(route + "-nondefinition-null", Argument(Error(() => query(typeof(int), null)), typeof(ArgumentNullException), "typeArguments"));
        Check(route + "-arity", Argument(Error(() => query(owner, Type.EmptyTypes)), typeof(ArgumentException), "typeArguments"));
        Check(route + "-null-element", Argument(Error(() => query(owner, new Type[] { null })), typeof(ArgumentNullException)));
        Check(route + "-arity-before-element", Argument(Error(() => query(pair, new Type[] { null })), typeof(ArgumentException), "typeArguments"));
        Check(route + "-void", Argument(Error(() => query(owner, new[] { typeof(void) })), typeof(ArgumentException)));
        Check(route + "-byref", Argument(Error(() => query(owner, new[] { typeof(int).MakeByRefType() })), typeof(ArgumentException)));
        Check(route + "-pointer", Argument(Error(() => query(owner, new[] { typeof(int).MakePointerType() })), typeof(ArgumentException)));
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type MakeGenericType contract ==");
        PublicRoute("public", Public);
        Type planned = Planned();
        Check("planned-closure", planned.GetGenericArguments()[0] == typeof(int)
            && planned.GetField("Value").FieldType == typeof(int));
        Type forwarded = Forwarded();
        Check("forwarded-closure", forwarded.GetGenericArguments()[0] == typeof(int)
            && forwarded.GetField("Value").FieldType == typeof(int));
        Check("public-null-receiver", Error(() => Public(null, new[] { typeof(int) })) is NullReferenceException);
        Check("public-both-null", Error(() => Public(null, null)) is NullReferenceException);
        Console.WriteLine("Runtime type MakeGenericType contract end");
    }

    private delegate Type MakeScalarInvoker(Type argument);
}
