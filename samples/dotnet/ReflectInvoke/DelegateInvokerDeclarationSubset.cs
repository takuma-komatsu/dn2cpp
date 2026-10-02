using System;

namespace DelegateInvokerDeclarationSubset;

internal static class Program
{
    private struct ColdValue
    {
        public int Id;
        public ColdValue(int id) => Id = id;
    }

    private readonly ref struct ColdRef
    {
        public readonly int Id;
        public ColdRef(int id) => Id = id;
    }

    private delegate ColdValue ColdInvoker(ColdValue value);
    private delegate ColdRef ColdRefInvoker(ref ColdRef value);

    private struct Cell<T>
    {
        public T Value;
        public Cell(T value) => Value = value;
    }

    private delegate Cell<T> ColdSharedInvoker<T>(ref Cell<T> value);
    private delegate void ColdRecursive<T>(ColdRecursive<ColdRecursive<T>> next);

    internal static void Run()
    {
        Console.WriteLine("== delegate invoker declarations ==");
        Console.WriteLine("cold invoker=" + typeof(ColdInvoker).Name);
        Console.WriteLine("cold ref invoker=" + typeof(ColdRefInvoker).Name);
        Console.WriteLine("cold shared distinct="
            + (typeof(ColdSharedInvoker<string>) != typeof(ColdSharedInvoker<object>)));
        Func<object, string> original = value => value is null ? "null" : value.GetType().Name;
        Func<OperatingSystem, string> view = original;
        Console.WriteLine("unconstructed variance view=" + view(null));
        Console.WriteLine("delegate invoker declarations end");
    }

    internal static void RunRecursive()
    {
        Console.WriteLine("== recursive delegate declarations ==");
        Console.WriteLine("recursive delegate identities="
            + (typeof(ColdRecursive<int>) != typeof(ColdRecursive<long>)) + "/"
            + (typeof(ColdRecursive<int>) == typeof(ColdRecursive<int>)));
        Console.WriteLine("recursive delegate declarations end");
    }
}
