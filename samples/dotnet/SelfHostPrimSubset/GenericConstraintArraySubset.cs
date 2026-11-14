using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GenericConstraintArraySubset;

internal static class Program
{
    private interface ConstraintArrayLink<T> { }
    private class ConstraintArrayRoot { }
    private class ConstraintArrayHolder<T> : ConstraintArrayRoot { }
    private class ConstraintArrayOwner<T> where T : ConstraintArrayRoot
    {
        public static void Empty<U>() { }
        public static void Reference<U>() where U : class { }
        public static void Constructor<U>() where U : new() { }
        public static void Value<U>() where U : struct { }
        public static void Owner<U>() where U : T { }
        public static void Dependent<U, V>() where U : V where V : ConstraintArrayRoot { }
        public static void Composed<U, V>() where U : ConstraintArrayHolder<V>, ConstraintArrayLink<T[]> where V : T { }
        public static void Recursive<U>() where U : ConstraintArrayLink<U> { }
        public static void Mutual<U, V>() where U : ConstraintArrayLink<V> where V : ConstraintArrayLink<U> { }
    }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("constraint-array-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Type Parameter(string method, int index = 0) =>
        typeof(ConstraintArrayOwner<ConstraintArrayRoot>).GetMethod(method).GetGenericArguments()[index];
    private static Type[] Public(Type type) => type.GetGenericParameterConstraints();

    private static void Route(string route, Func<Type, Type[]> query, bool privateWrapper)
    {
        foreach (string method in new[] { "Empty", "Reference", "Constructor" })
        {
            Type[] empty = query(Parameter(method));
            Check(route + "-empty-" + method, privateWrapper ? empty is null : empty is not null && empty.Length == 0);
        }
        Type owner = typeof(ConstraintArrayOwner<>).GetGenericArguments()[0];
        Check(route + "-type-formal", query(owner)[0] == typeof(ConstraintArrayRoot));
        Check(route + "-method-formal", ReferenceEquals(query(Parameter("Owner"))[0], owner));
        Check(route + "-value", query(Parameter("Value"))[0] == typeof(ValueType));
        Type dependent = Parameter("Dependent", 1);
        Check(route + "-dependent", ReferenceEquals(query(Parameter("Dependent"))[0], dependent)
            && query(dependent)[0] == typeof(ConstraintArrayRoot));
        Type[] composed = query(Parameter("Composed"));
        Check(route + "-composed", composed.Length == 2 && composed[0].GetGenericTypeDefinition() == typeof(ConstraintArrayHolder<>)
            && ReferenceEquals(composed[0].GetGenericArguments()[0], Parameter("Composed", 1))
            && composed[1].GetGenericTypeDefinition() == typeof(ConstraintArrayLink<>)
            && ReferenceEquals(composed[1].GetGenericArguments()[0].GetElementType(), owner));
        Type recursive = Parameter("Recursive");
        Type first = Parameter("Mutual"), second = Parameter("Mutual", 1);
        Check(route + "-recursive", ReferenceEquals(query(recursive)[0].GetGenericArguments()[0], recursive)
            && ReferenceEquals(query(first)[0].GetGenericArguments()[0], second)
            && ReferenceEquals(query(second)[0].GetGenericArguments()[0], first));
        Type saved = composed[0];
        composed[0] = null;
        Type[] again = query(Parameter("Composed"));
        Check(route + "-fresh", !ReferenceEquals(composed, again) && ReferenceEquals(saved, again[0]));
        bool stable = true;
        for (int i = 0; i < 16; i++) stable &= ReferenceEquals(saved, query(Parameter("Composed"))[0]);
        GC.Collect();
        Check(route + "-repeated", stable && ReferenceEquals(saved, query(Parameter("Composed"))[0]));
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Generic constraint array contract ==");
        Route("public-payload", Public, false);
        Console.WriteLine("Generic constraint array contract end");
    }
}
