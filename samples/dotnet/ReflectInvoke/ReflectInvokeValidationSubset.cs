#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization;

namespace ReflectInvokeValidationSubset;

enum Color { Red, Green, Blue }

enum Shade { Dark, Light }

enum Wide : long { Low, High = 2 }

enum Tiny : byte { A, B = 7 }

struct Point
{
    public int X;
    public int Y;
}

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
    public static float Single(float value) => value;
    public static string Boxed(ValueType value) => "boxed:" + value;
    public static bool IsValueType(object value) => value is ValueType;
    public static string MaybeDate(DateTime? value) => value.HasValue ? "has:" + value.Value.Ticks : "none";
    public static string MaybePoint(Point? value) => value.HasValue ? "has:" + value.Value.X + "," + value.Value.Y : "none";
    public static int? MaybeInt(bool has) => has ? 5 : null;
    public static Point? MaybeAt(bool has) => has ? new Point { X = 1, Y = 2 } : null;
    public static string Culture(CultureInfo culture) => "culture:[" + culture.Name + "]";
    public static string Separator(NumberFormatInfo info) => "separator:" + info.NumberDecimalSeparator;
    public static string Utf8(IUtf8SpanFormattable value) => "utf8:" + value;
    public static string Serial(ISerializable value) => "serializable:" + value;
    public static string Number(INumber<int> value) => "number:" + value;
    public static string Bounded(IMinMaxValue<long> value) => "bounded:" + value;
    public static string Chars(IEnumerable<char> value) => "chars:" + value;
    public static string Ordered(IComparable<string> value) => "ordered:" + value;
    public static string Copyable(ICloneable value) => "cloneable:" + value;
    public static string Parsable(ISpanParsable<string> value) => "parsable:" + value;
}

class Holder
{
    public Holder(int value) => Value = value;

    public int Value { get; set; }
    public Color Hue { get; set; }
    public int? Maybe { get; set; }
    public DateTime? When { get; set; }
}

abstract class MintedBase
{
    public abstract string Who();
    public abstract string Name { get; }
}

// Only typeof(Minted<>) names this definition, so every instantiation is minted
// at run time from the template: its methods name the instantiation and its
// property accessors are the template's rows. The base's virtual calls are what
// give those rows bodies.
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

    private static string Describe(object? value) =>
        value is null ? "null" : value.GetType().Name + ":" + value;

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

        // A delegate CreateDelegate binds calls its target as typed; only the
        // late-bound forms check and convert.
        var boxed = (Func<ValueType, string>)Delegate.CreateDelegate(
            typeof(Func<ValueType, string>), typeof(Coerce).GetMethod("Boxed")!);
        Try("bound ValueType delegate", () => boxed(5));
        Try("ValueType argument", () => typeof(Coerce).GetMethod("Boxed")!.Invoke(null, new object[] { 5 }));
        Console.WriteLine($"int is ValueType: {Coerce.IsValueType(5)}");
        Try("double from ulong max", () => toDouble.Invoke(null, new object[] { ulong.MaxValue }));
        Try("single from ulong high bit",
            () => typeof(Coerce).GetMethod("Single")!.Invoke(null, new object[] { 1UL << 63 }));
        MethodInfo maybeDate = typeof(Coerce).GetMethod("MaybeDate")!;
        Try("nullable struct from value", () => maybeDate.Invoke(null, new object[] { new DateTime(2020, 1, 2) }));
        Try("nullable struct from null", () => maybeDate.Invoke(null, new object?[] { null }));
        Try("nullable struct from other", () => maybeDate.Invoke(null, new object[] { 5 }));
        Try("nullable user struct", () => typeof(Coerce).GetMethod("MaybePoint")!
            .Invoke(null, new object[] { new Point { X = 1, Y = 2 } }));
        Try("nullable struct receiver",
            () => typeof(DateTime?).GetProperty("HasValue")!.GetValue(new DateTime(2020, 1, 2)));
        Try("set nullable struct", () =>
        {
            typeof(Holder).GetProperty("When")!.SetValue(holder, new DateTime(2020, 1, 2));
            return holder.When?.Ticks;
        });
        MethodInfo maybeInt = typeof(Coerce).GetMethod("MaybeInt")!;
        Try("nullable result with value", () => Describe(maybeInt.Invoke(null, new object[] { true })));
        Try("nullable result without value", () => Describe(maybeInt.Invoke(null, new object[] { false })));
        Try("nullable struct result",
            () => Describe(typeof(Coerce).GetMethod("MaybeAt")!.Invoke(null, new object[] { true })));
        Try("nullable property value", () => Describe(typeof(Holder).GetProperty("Maybe")!.GetValue(holder)));
        object mintedText = Activator.CreateInstance(typeof(Minted<>).MakeGenericType(typeof(string)))!;
        Try("minted property, other instantiation", () => minted.GetProperty("Name")!.GetValue(mintedText));
        Fault("message minted other instantiation",
            () => minted.GetProperty("Name")!.GetGetMethod()!.Invoke(mintedText, null));
        ICloneable culture = CultureInfo.InvariantCulture;
        Try("culture through an interface",
            () => typeof(Coerce).GetMethod("Culture")!.Invoke(null, new object[] { culture }));
        Try("current number format",
            () => typeof(Coerce).GetMethod("Separator")!.Invoke(null, new object[] { NumberFormatInfo.CurrentInfo }));

        // A boxed built-in or a string passes for every CLR interface its type
        // implements, beyond the ones a dispatch table serves, and fails for one it
        // does not implement.
        MethodInfo utf8 = typeof(Coerce).GetMethod("Utf8")!;
        MethodInfo serial = typeof(Coerce).GetMethod("Serial")!;
        MethodInfo number = typeof(Coerce).GetMethod("Number")!;
        MethodInfo bounded = typeof(Coerce).GetMethod("Bounded")!;
        Try("utf8 from int", () => utf8.Invoke(null, new object[] { 5 }));
        Try("utf8 from decimal", () => utf8.Invoke(null, new object[] { 2.5m }));
        Try("utf8 from TimeSpan", () => utf8.Invoke(null, new object[] { TimeSpan.FromSeconds(3) }));
        Try("utf8 from DateOnly", () => utf8.Invoke(null, new object[] { new DateOnly(2020, 1, 2) }));
        Try("utf8 from bool", () => utf8.Invoke(null, new object[] { true }));
        Try("utf8 from string", () => utf8.Invoke(null, new object[] { "s" }));
        Try("serializable from decimal", () => serial.Invoke(null, new object[] { 2.5m }));
        Try("serializable from DateTime", () => serial.Invoke(null, new object[] { new DateTime(2020, 1, 2) }));
        Try("serializable from IntPtr", () => serial.Invoke(null, new object[] { (nint)7 }));
        Try("serializable from TimeSpan", () => serial.Invoke(null, new object[] { TimeSpan.FromSeconds(3) }));
        Try("number from int", () => number.Invoke(null, new object[] { 7 }));
        Try("number from long", () => number.Invoke(null, new object[] { 7L }));
        Try("bounded from long", () => bounded.Invoke(null, new object[] { 7L }));
        Try("bounded from int", () => bounded.Invoke(null, new object[] { 7 }));
        Try("chars from string", () => typeof(Coerce).GetMethod("Chars")!.Invoke(null, new object[] { "abc" }));
        Try("ordered from string", () => typeof(Coerce).GetMethod("Ordered")!.Invoke(null, new object[] { "abc" }));
        Try("cloneable from string", () => typeof(Coerce).GetMethod("Copyable")!.Invoke(null, new object[] { "abc" }));
        Try("parsable from string", () => typeof(Coerce).GetMethod("Parsable")!.Invoke(null, new object[] { "abc" }));
    }
}
