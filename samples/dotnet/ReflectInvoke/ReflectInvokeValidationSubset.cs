#nullable enable
using System;
using System.Reflection;

namespace ReflectInvokeValidationSubset;

class Receiver
{
    public int Calls;

    public int Value(int value)
    {
        Calls++;
        return value;
    }

    public string Text(string value)
    {
        Calls++;
        return value;
    }

    public void Throw()
    {
        Calls++;
        throw new InvalidOperationException("target ran");
    }
}

class Indexer
{
    private readonly string[] _values = { "x", "y" };

    public string this[int index]
    {
        get => _values[index];
        set => _values[index] = value;
    }

    public string Plain { get; set; } = "p";
}

static class Program
{
    private static void Try(string label, Func<object?> invoke)
    {
        try
        {
            Console.WriteLine($"{label}: {invoke() ?? "null"}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{label}: {ex.GetType().Name}" +
                (ex.InnerException is null ? "" : $"/{ex.InnerException.GetType().Name}"));
        }
    }

    internal static void Run()
    {
        Console.WriteLine("== reflection invoke validation ==");
        var receiver = new Receiver();
        MethodInfo value = typeof(Receiver).GetMethod("Value")!;
        MethodInfo text = typeof(Receiver).GetMethod("Text")!;
        MethodInfo throws = typeof(Receiver).GetMethod("Throw")!;

        Try("instance null receiver", () => value.Invoke(null, new object?[] { 1 }));
        Try("instance wrong receiver", () => value.Invoke(new object(), new object?[] { 1 }));
        Try("value wrong box", () => value.Invoke(receiver, new object?[] { 1L }));
        Try("reference wrong type", () => text.Invoke(receiver, new object?[] { new object() }));
        Try("value null argument", () => value.Invoke(receiver, new object?[] { null }));
        Try("method missing argument", () => value.Invoke(receiver, Array.Empty<object>()));
        Try("method extra argument", () => value.Invoke(receiver, new object?[] { 1, 2 }));
        Try("target exception", () => throws.Invoke(receiver, null));
        Console.WriteLine($"target calls: {receiver.Calls}");

        var indexer = new Indexer();
        PropertyInfo item = typeof(Indexer).GetProperty("Item")!;
        PropertyInfo plain = typeof(Indexer).GetProperty("Plain")!;
        Try("indexed get, no index", () => item.GetValue(indexer));
        Try("indexed get, null index", () => item.GetValue(indexer, null));
        Try("indexed get, extra index", () => item.GetValue(indexer, new object[] { 1, 2 }));
        Try("plain get, stray index", () => plain.GetValue(indexer, new object[] { 1 }));
    }
}
