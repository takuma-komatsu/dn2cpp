#nullable enable
using System;
using System.Reflection;

namespace ReflectInvokeValidationSubset;

enum Color { Red, Green, Blue }

enum Shade { Dark, Light }

enum Wide : long { Low, High = 2 }

enum Tiny : byte { A, B = 7 }

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

static class Coerce
{
    public static int Int(int value) => value;
    public static long Long(long value) => value;
    public static double Double(double value) => value;
    public static Color Hue(Color value) => value;
    public static Wide Span(Wide value) => value;
    public static string Maybe(int? value) => value.HasValue ? "has:" + value.Value : "none";
    public static string MaybeHue(Color? value) => value.HasValue ? "has:" + value.Value : "none";
    public static string MaybeTiny(Tiny? value) => value.HasValue ? "has:" + value.Value : "none";
    public static string Echo<T>(T value) => typeof(T).Name + ":" + value;
}

class Holder
{
    public Holder(int value) => Value = value;

    public int Value { get; set; }
    public Color Hue { get; set; }
    public int? Maybe { get; set; }
}

abstract class MintedBase
{
    public abstract string Who();
    public abstract string Name { get; }
}

// Only typeof(Minted<>) names this definition, so every instantiation is minted
// at run time and shares the template's member rows. The base's virtual calls
// are what give those rows bodies.
class Minted<T> : MintedBase
{
    public override string Who() => "minted:" + typeof(T).Name;
    public override string Name => typeof(T).Name;
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

    private static void Fault(string label, Action invoke)
    {
        try
        {
            invoke();
            Console.WriteLine($"{label}: no fault");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{label}: {ex.GetType().Name} 0x{ex.HResult:X8} {ex.Message}");
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

        // CheckValue converts by value: widening primitives, enums read as their
        // underlying type, and a boxed U for Nullable<U>.
        MethodInfo toInt = typeof(Coerce).GetMethod("Int")!;
        MethodInfo toLong = typeof(Coerce).GetMethod("Long")!;
        MethodInfo toDouble = typeof(Coerce).GetMethod("Double")!;
        MethodInfo toHue = typeof(Coerce).GetMethod("Hue")!;
        MethodInfo toSpan = typeof(Coerce).GetMethod("Span")!;
        MethodInfo maybe = typeof(Coerce).GetMethod("Maybe")!;
        Try("widen int to long", () => toLong.Invoke(null, new object[] { 1 }));
        Try("widen short to int", () => toInt.Invoke(null, new object[] { (short)3 }));
        Try("widen char to int", () => toInt.Invoke(null, new object[] { 'A' }));
        Try("widen int to double", () => toDouble.Invoke(null, new object[] { 2 }));
        Try("enum from int", () => toHue.Invoke(null, new object[] { 1 }));
        Try("int from enum", () => toInt.Invoke(null, new object[] { Color.Blue }));
        Try("enum from other enum", () => toHue.Invoke(null, new object[] { Shade.Light }));
        Try("long from enum", () => toLong.Invoke(null, new object[] { Color.Blue }));
        Try("long enum from int", () => toSpan.Invoke(null, new object[] { 2 }));
        Try("enum from long enum", () => toHue.Invoke(null, new object[] { Wide.High }));
        Try("narrow long to int", () => toInt.Invoke(null, new object[] { 5L }));
        Try("uint to int", () => toInt.Invoke(null, new object[] { 5u }));
        Try("nullable from value", () => maybe.Invoke(null, new object[] { 5 }));
        Try("nullable from null", () => maybe.Invoke(null, new object?[] { null }));
        Try("nullable from wider", () => maybe.Invoke(null, new object[] { 5L }));
        Try("nullable enum", () => typeof(Coerce).GetMethod("MaybeHue")!.Invoke(null, new object[] { Color.Blue }));
        Try("nullable byte enum", () => typeof(Coerce).GetMethod("MaybeTiny")!.Invoke(null, new object[] { Tiny.B }));
        object[] converted = { (short)3 };
        toInt.Invoke(null, converted);
        Console.WriteLine($"caller array after widening: {converted[0].GetType().Name}");

        ConstructorInfo holderCtor = typeof(Holder).GetConstructor(new[] { typeof(int) })!;
        Try("ctor widen byte", () => ((Holder)holderCtor.Invoke(new object[] { (byte)4 })).Value);
        Try("ctor from enum", () => ((Holder)holderCtor.Invoke(new object[] { Color.Green })).Value);
        Try("ctor wrong type", () => holderCtor.Invoke(new object[] { "4" }));
        var holder = new Holder(0);
        Try("set enum from int", () =>
        {
            typeof(Holder).GetProperty("Hue")!.SetValue(holder, 2);
            return holder.Hue;
        });
        Try("set int from byte", () =>
        {
            typeof(Holder).GetProperty("Value")!.SetValue(holder, (byte)9);
            return holder.Value;
        });
        Try("set nullable from value", () =>
        {
            typeof(Holder).GetProperty("Maybe")!.SetValue(holder, 7);
            return holder.Maybe;
        });
        Try("set int from string", () =>
        {
            typeof(Holder).GetProperty("Value")!.SetValue(holder, "9");
            return holder.Value;
        });

        // Receivers .NET accepts beyond the declaring type's own instances.
        int? boxedNullable = 5;
        Console.WriteLine($"nullable direct: {boxedNullable.HasValue}");
        Try("nullable receiver", () => typeof(int?).GetProperty("HasValue")!.GetValue(5));
        Type minted = typeof(Minted<>).MakeGenericType(typeof(int));
        var mintedValue = (MintedBase)Activator.CreateInstance(minted)!;
        Console.WriteLine($"minted direct: {mintedValue.Who()} {mintedValue.Name}");
        Try("minted method", () => minted.GetMethod("Who")!.Invoke(mintedValue, null));
        Try("minted property", () => minted.GetProperty("Name")!.GetValue(mintedValue));
        Try("minted wrong receiver", () => minted.GetMethod("Who")!.Invoke(new object(), null));

        Console.WriteLine($"echo direct: {Coerce.Echo(0)}");
        MethodInfo echoInt = typeof(Coerce).GetMethod("Echo")!.MakeGenericMethod(typeof(int));
        MethodInfo echo = echoInt.GetGenericMethodDefinition();
        Try("closed generic", () => echoInt.Invoke(null, new object[] { 3 }));
        Try("closed generic wrong type", () => echoInt.Invoke(null, new object[] { "x" }));
        Try("open generic definition", () => echo.Invoke(null, new object[] { 3 }));

        MethodInfo clone = typeof(object).GetMethod("MemberwiseClone",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        Try("clone null receiver, extra argument", () => clone.Invoke(null, new object[] { 1 }));
        Try("clone extra argument", () => clone.Invoke(receiver, new object[] { 1 }));

        Fault("message null receiver", () => value.Invoke(null, new object?[] { 1 }));
        Fault("message wrong receiver", () => value.Invoke(new object(), new object?[] { 1 }));
        Fault("message parameter count", () => value.Invoke(receiver, Array.Empty<object>()));
        Fault("message value conversion", () => value.Invoke(receiver, new object?[] { 1L }));
        Fault("message reference conversion", () => text.Invoke(receiver, new object?[] { new object() }));
        Fault("message enum conversion", () => toHue.Invoke(null, new object[] { Wide.High }));
        Fault("message indexer count", () => plain.GetValue(indexer, new object[] { 1 }));
        Fault("message open generic", () => echo.Invoke(null, new object[] { 3 }));
    }
}
