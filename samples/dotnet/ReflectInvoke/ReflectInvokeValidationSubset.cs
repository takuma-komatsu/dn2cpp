#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
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

// Reached only through GetType(), so its rows are packed.
sealed class Planned
{
    public long Total;
    public Planned() { }
    public Planned(int a, long b) => Total = a + b;
    public long Wide { get; set; }
    public static long Sum(long a, long b) => a + b;

    public string Six(int a, long b, double c, string? d, object? e, Color f) =>
        a + "|" + b + "|" + c + "|" + (d ?? "null") + "|" + (e ?? "null") + "|" + f;

    public string Seven(int a, long b, double c, string? d, object? e, Color f, int? g) =>
        a + "|" + b + "|" + c + "|" + (d ?? "null") + "|" + (e ?? "null") + "|" + f + "|"
        + (g.HasValue ? g.Value.ToString() : "none");

    public string Eight(int a, long b, double c, string? d, object? e, Color f, int? g, short h) =>
        Seven(a, b, c, d, e, f, g) + "|" + h;
}

delegate void RefAction(ref int value);

unsafe delegate int* PointerSource();

unsafe delegate long PointerSink(int* pointer);

unsafe class PointerTarget
{
    public static int* s_cell = (int*)0x200;
    public static int* Address() => (int*)0x1230;
    public static int* Null() => null;
    public static void* Untyped() => (void*)0x40;
    public static int** Twice() => (int**)0x80;
    public static Point* Spot() => (Point*)0x90;
    public static delegate*<int> Entry() => (delegate*<int>)0x100;
    public static ref int* Cell() => ref s_cell;
    public static int* Property => (int*)0x300;
    public virtual int* Virtual() => (int*)0x600;
    public static long Int(int* pointer) => (long)pointer;
    public static long UInt(uint* pointer) => (long)pointer;
    public static long Long(long* pointer) => (long)pointer;
    public static long Void(void* pointer) => (long)pointer;
    public static long IntTwice(int** pointer) => (long)pointer;
    public static long LongTwice(long** pointer) => (long)pointer;
    public static void Retarget(ref int* pointer) => pointer = (int*)0x44;
}

unsafe class PointerDerived : PointerTarget
{
    public override int* Virtual() => (int*)0x700;
}

// Pointers past four levels and pointers to function pointers, whose types .NET
// tells apart by every level and by the function pointer's signature.
unsafe class DeepPointerTarget
{
    public static int***** s_cell = (int*****)0x58;
    public static int***** Five() => (int*****)0x50;
    public static int****** Six() => (int******)0x60;
    public static ref int***** FiveCell() => ref s_cell;
    public static long FiveSink(int***** pointer) => (long)pointer;
    public static long UIntFiveSink(uint***** pointer) => (long)pointer;
    public static long VoidSink(void* pointer) => (long)pointer;
    public static delegate*<int>* EntryCell() => (delegate*<int>*)0x78;
    public static long EntryCellSink(delegate*<int>* pointer) => (long)pointer;
    public static long LongEntryCellSink(delegate*<long>* pointer) => (long)pointer;
    public static long UnmanagedCellSink(delegate* unmanaged<int>* pointer) => (long)pointer;
    public static long EntrySink(delegate*<int> entry) => (long)entry;
    public static long WideEntrySink(delegate*<ref int, out long, int[], void> entry) => (long)entry;
    public static long NestedEntrySink(delegate* unmanaged[Cdecl]<delegate*<int>, int*> entry) => (long)entry;
    public static long ByRefEntrySink(ref delegate*<int> entry) => 1;
}

// Signatures whose parameters or returns are references, pointers or by-ref-like.
unsafe class ByRefTarget
{
    public static int Pointee = 31;
    private static int s_cell = 77;
    private static string s_text = "rt";
    private static Point s_point = new Point { X = 3, Y = 4 };
    private static int? s_maybe;
    private static sbyte s_small = -3;
    public int Seed = 10;

    public ByRefTarget() { }
    public ByRefTarget(ref int count, out string label)
    {
        count = 99;
        label = "ctor";
    }

    public static void Inc(ref int value) => value++;
    public static void SetText(out string text) => text = "set";
    public static void Move(ref Point point) => point.X += 5;
    public static void Produce(out int value) => value = 42;
    public static int Twice(in int value) => value * 2;
    public static void Swap(ref object? value) => value = value is string s ? s.Length : "none";
    public static void Flip(ref int? value) => value = value.HasValue ? null : 7;
    public static void Fail(out int value)
    {
        value = 9;
        throw new InvalidOperationException("after write");
    }
    public static void FailText(ref string text)
    {
        text = "written";
        throw new InvalidOperationException("after write");
    }
    public void AddSeed(ref int value) => value += Seed;
    public virtual void Tag(ref string text) => text += "-base";
    public static void Paint(ref Color color) => color = Color.Blue;
    public static void Widen(ref long value) => value = 5;
    public static void Mix(ref int a, out string b, int c)
    {
        a *= c;
        b = "c=" + c;
    }
    public static void Shrink(ref sbyte value) => value = (sbyte)(value - 10);
    public static void Double(ref short value) => value = (short)(value * 2);
    public static void Upper(ref char value) => value = char.ToUpperInvariant(value);
    public static void Negate(ref bool value) => value = !value;
    public static void Bump(ref byte value) => value = (byte)(value + 1);
    public static void Grow(ref Tiny value) => value = Tiny.B;
    public static int Read(int* pointer) => pointer == null ? -1 : *pointer;
    public static long Address(void* pointer) => (long)pointer;
    public static long Entry(delegate*<int> function) => (long)function;
    public static void Retarget(ref int* pointer) => pointer = null;
    public static int Length(ReadOnlySpan<char> text) => text.Length;
    public static void Clear(ref Span<int> span) => span = default;
    public static ref int Cell() => ref s_cell;
    public static ref string Text() => ref s_text;
    public static ref Point Spot() => ref s_point;
    public static ref int? Maybe() => ref s_maybe;
    public static ref sbyte Small() => ref s_small;
    public static ref readonly int Frozen() => ref s_cell;
    public static ref int Nowhere() => ref Unsafe.NullRef<int>();
    public static ReadOnlySpan<char> Chars() => "abc";
}

class ByRefDerived : ByRefTarget
{
    public override void Tag(ref string text) => text += "-derived";
}

ref struct RefCell
{
    public int Value;

    public RefCell(int value) => Value = value;
}

ref struct RefMade
{
    public int Value;

    public RefMade() => Value = 9;
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

    // A repeated call over a packed row checks, converts and rejects its arguments
    // exactly as the first, for methods, a constructor and a property setter, over
    // seven parameters and over eight.
    internal static void RunPlannedChecks()
    {
        Console.WriteLine("== planned argument checks ==");
        object planned = new Planned();
        Type type = planned.GetType();
        MethodInfo seven = type.GetMethod("Seven")!;
        MethodInfo eight = type.GetMethod("Eight")!;
        MethodInfo sum = type.GetMethod("Sum")!;
        ConstructorInfo ctor = type.GetConstructor(new[] { typeof(int), typeof(long) })!;
        PropertyInfo wide = type.GetProperty("Wide")!;
        for (int round = 0; round < 2; round++)
        {
            string at = ", round " + round;
            Try("seven" + at, () => seven.Invoke(planned, new object?[] { 1, 2, 3, "d", 5, Color.Blue, 7 }));
            Try("seven, nulls" + at, () => seven.Invoke(planned, new object?[] { null, null, null, null, null, null, null }));
            Try("seven, mismatch" + at, () => seven.Invoke(planned, new object?[] { 1, 2, 3, "d", 5, Color.Blue, "x" }));
            Try("eight" + at, () => eight.Invoke(planned, new object?[] { 1, 2, 3.5, "d", 5, Color.Green, 7, (short)8 }));
            Try("eight, mismatch" + at, () => eight.Invoke(planned, new object?[] { 1, 2, 3.5, "d", 5, Color.Green, 7, 8 }));
            Try("static widening" + at, () => sum.Invoke(null, new object[] { 1, (byte)2 }));
            Try("constructor widening" + at, () => ((Planned)ctor.Invoke(new object[] { 3, 4 })).Total);
            Try("constructor mismatch" + at, () => ctor.Invoke(new object[] { 3, "4" }));
            Try("setter widening" + at, () =>
            {
                wide.SetValue(planned, 9 + round);
                return wide.GetValue(planned);
            });
        }
        Console.WriteLine("planned argument checks end");
    }

    private static void Probe(string label, Func<string> run)
    {
        string result;
        try
        {
            result = run();
        }
        catch (Exception ex)
        {
            result = ex.GetType().Name + ": " + ex.Message;
            if (ex.InnerException is { } inner)
                result += " / " + inner.GetType().Name + ": " + inner.Message;
        }
        Console.WriteLine(label + ": " + result);
    }

    private static string Call(string name, params object?[] args)
    {
        typeof(ByRefTarget).GetMethod(name)!.Invoke(null, args);
        return string.Join(",", Array.ConvertAll(args, Describe));
    }

    private static string Returned(string name, BindingFlags flags = BindingFlags.Default) =>
        Describe(typeof(ByRefTarget).GetMethod(name)!.Invoke(null, flags, null, null, null));

    private static string Coordinates(object? value) =>
        value is Point p ? p.X + "/" + p.Y : Describe(value);

    // Invoke passes a by-ref argument as a reference to a copy of it and writes the
    // copy back into the argument array only when the target returns; a pointer
    // parameter takes a boxed IntPtr's value; a by-ref-like parameter takes no
    // argument; a by-ref return is dereferenced. Refusals match .NET's.
    internal static unsafe void RunByRefArguments()
    {
        Console.WriteLine("== by-ref arguments ==");
        Type type = typeof(ByRefTarget);
        ConstructorInfo refCtor = Array.Find(type.GetConstructors(), c => c.GetParameters().Length == 2)!;
        for (int round = 0; round < 2; round++)
        {
            string at = ", round " + round;
            Probe("ref int" + at, () =>
            {
                object?[] args = { 5 };
                object? source = args[0];
                type.GetMethod("Inc")!.Invoke(null, args);
                return Describe(args[0]) + " copy=" + !ReferenceEquals(args[0], source) + " source=" + Describe(source);
            });
            Probe("out string" + at, () => Call("SetText", new object?[] { null }));
            Probe("ref struct" + at, () =>
            {
                object?[] args = { new Point { X = 1, Y = 2 } };
                object? source = args[0];
                type.GetMethod("Move")!.Invoke(null, args);
                return Coordinates(args[0]) + " source=" + Coordinates(source);
            });
            Probe("constructor" + at, () =>
            {
                object?[] args = { 1, null };
                return (refCtor.Invoke(args) is ByRefTarget) + " " + Describe(args[0]) + " " + Describe(args[1]);
            });
        }
        Probe("ref int, null", () => Call("Inc", new object?[] { null }));
        Probe("out string, given", () => Call("SetText", "old"));
        Probe("ref struct, null", () =>
        {
            object?[] args = { null };
            type.GetMethod("Move")!.Invoke(null, args);
            return Coordinates(args[0]);
        });
        Probe("out int, null", () => Call("Produce", new object?[] { null }));
        Probe("in int", () =>
        {
            object?[] args = { 21 };
            object? source = args[0];
            object? result = type.GetMethod("Twice")!.Invoke(null, args);
            return Describe(result) + " " + Describe(args[0]) + " copy=" + !ReferenceEquals(args[0], source);
        });
        Probe("ref object, string", () => Call("Swap", "abc"));
        Probe("ref object, null", () => Call("Swap", new object?[] { null }));
        Probe("ref nullable, null", () => Call("Flip", new object?[] { null }));
        Probe("ref nullable, value", () => Call("Flip", 3));
        Probe("out int, target throws", () =>
        {
            object?[] args = { 1 };
            object? source = args[0];
            try
            {
                type.GetMethod("Fail")!.Invoke(null, args);
            }
            catch (TargetInvocationException ex)
            {
                return ex.InnerException!.Message + " " + Describe(args[0]) + " same=" + ReferenceEquals(args[0], source);
            }
            return "no fault";
        });
        Probe("ref string, target throws", () =>
        {
            object?[] args = { "in" };
            try
            {
                type.GetMethod("FailText")!.Invoke(null, args);
            }
            catch (TargetInvocationException ex)
            {
                return ex.InnerException!.Message + " " + Describe(args[0]);
            }
            return "no fault";
        });
        Probe("out int, unwrapped throw", () =>
        {
            object?[] args = { 1 };
            try
            {
                type.GetMethod("Fail")!.Invoke(null, BindingFlags.DoNotWrapExceptions, null, args, null);
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message + " " + Describe(args[0]);
            }
            return "no fault";
        });
        Probe("instance ref", () =>
        {
            object?[] args = { 1 };
            type.GetMethod("AddSeed")!.Invoke(new ByRefTarget(), args);
            return Describe(args[0]);
        });
        Probe("virtual ref", () =>
        {
            object?[] args = { "x" };
            type.GetMethod("Tag")!.Invoke(new ByRefDerived(), args);
            return Describe(args[0]);
        });
        Probe("ref enum", () => Call("Paint", Color.Red));
        Probe("ref enum, int", () => Call("Paint", 0));
        Probe("ref long, int", () => Call("Widen", 1));
        Probe("ref int, long", () => Call("Inc", 1L));
        Probe("ref int, string", () => Call("Inc", "s"));
        Probe("ref int, enum", () => Call("Inc", Color.Green));
        Probe("out string, int", () => Call("SetText", 3));
        Probe("mixed", () => Call("Mix", 3, null, 4));
        Probe("ref sbyte", () => Call("Shrink", (sbyte)3));
        Probe("ref short", () => Call("Double", (short)-300));
        Probe("ref char", () => Call("Upper", 'q'));
        Probe("ref bool", () => Call("Negate", true));
        Probe("ref byte", () => Call("Bump", (byte)255));
        Probe("ref byte, int", () => Call("Bump", 3));
        Probe("ref byte enum", () => Call("Grow", Tiny.A));
        Probe("activator", () =>
        {
            object?[] args = { 1, null };
            return (Activator.CreateInstance(type, args) is ByRefTarget) + " " + Describe(args[0]) + " " + Describe(args[1]);
        });
        Probe("activator, long", () => Describe(Activator.CreateInstance(type, new object?[] { 1L, null })));
        Probe("pointer, IntPtr", () =>
        {
            fixed (int* pointee = &ByRefTarget.Pointee)
                return Describe(type.GetMethod("Read")!.Invoke(null, new object[] { (IntPtr)pointee }));
        });
        Probe("pointer, null", () => Describe(type.GetMethod("Read")!.Invoke(null, new object?[] { null })));
        Probe("pointer, int", () => Call("Read", 5));
        Probe("pointer, UIntPtr", () => Call("Read", (UIntPtr)8));
        Probe("void pointer, IntPtr", () => Describe(type.GetMethod("Address")!.Invoke(null, new object[] { (IntPtr)16 })));
        Probe("void pointer, null", () => Describe(type.GetMethod("Address")!.Invoke(null, new object?[] { null })));
        Probe("void pointer, string", () => Call("Address", "s"));
        Probe("function pointer, IntPtr", () => Describe(type.GetMethod("Entry")!.Invoke(null, new object[] { (IntPtr)64 })));
        Probe("function pointer, null", () => Call("Entry", new object?[] { null }));
        Probe("ref pointer, null", () => Call("Retarget", new object?[] { null }));
        Probe("ref pointer, IntPtr", () => Call("Retarget", (IntPtr)8));
        Probe("span, null", () => Call("Length", new object?[] { null }));
        Probe("span, string", () => Call("Length", "abc"));
        Probe("ref span, null", () => Call("Clear", new object?[] { null }));
        Probe("ref span, string", () => Call("Clear", "x"));
        Probe("ref return", () => Returned("Cell"));
        Probe("ref return, string", () => Returned("Text"));
        Probe("ref return, struct", () => Coordinates(type.GetMethod("Spot")!.Invoke(null, null)));
        Probe("ref return, nullable", () => Returned("Maybe"));
        Probe("ref return, sbyte", () => Returned("Small"));
        Probe("readonly ref return", () => Returned("Frozen"));
        Probe("null ref return", () => Returned("Nowhere"));
        Probe("null ref return, unwrapped", () => Returned("Nowhere", BindingFlags.DoNotWrapExceptions));
        Probe("by-ref-like return", () => Returned("Chars"));
        Probe("bound ref delegate", () =>
        {
            var bump = (RefAction)Delegate.CreateDelegate(typeof(RefAction), type.GetMethod("Inc")!);
            int value = 1;
            bump(ref value);
            return value.ToString();
        });
        Console.WriteLine("by-ref arguments end");
    }

    internal static unsafe void RunInvokePlanReuse()
    {
        Console.WriteLine("== invoke plan reuse ==");
        Type type = typeof(ByRefTarget);
        MethodInfo inc = type.GetMethod("Inc")!;
        MethodInfo read = type.GetMethod("Read")!;
        MethodInfo spot = type.GetMethod("Spot")!;
        MethodInfo chars = type.GetMethod("Chars")!;
        MethodInfo length = type.GetMethod("Length")!;
        object planned = new Planned();
        MethodInfo six = planned.GetType().GetMethod("Six")!;
        for (int round = 0; round < 2; round++)
        {
            string at = ", round " + round;
            Probe("ref argument" + at, () =>
            {
                object?[] args = { round };
                inc.Invoke(null, args);
                return Describe(args[0]);
            });
            Probe("pointer argument" + at, () =>
            {
                fixed (int* pointee = &ByRefTarget.Pointee)
                    return Describe(read.Invoke(null, new object[] { (IntPtr)pointee }));
            });
            Probe("ref return" + at, () => Coordinates(spot.Invoke(null, null)));
            Probe("by-ref-like argument" + at, () => Describe(length.Invoke(null, new object?[] { null })));
            Probe("by-ref-like return" + at, () => Describe(chars.Invoke(null, null)));
            Try("six" + at, () => six.Invoke(planned, new object?[] { 1, 2, 3, "d", 5, Color.Blue }));
            Try("six, mismatch" + at,
                () => six.Invoke(planned, new object?[] { 1, 2, 3, "d", 5, "wrong" }));
        }
        Console.WriteLine("invoke plan reuse end");
    }

    // Reflection never boxes or stores a by-ref-like value: Activator.CreateInstance
    // and RuntimeHelpers.GetUninitializedObject refuse the type with
    // NotSupportedException, with or without a declared parameterless constructor, its
    // constructors refuse Invoke with TargetException, and Array.CreateInstance refuses
    // it as an element type with NotSupportedException, as it refuses void.
    internal static void RunByRefLikeConstructions()
    {
        Console.WriteLine("== by-ref-like constructions ==");
        Probe("activator", () => Describe(Activator.CreateInstance(typeof(RefCell))));
        Probe("activator, non-public", () => Describe(Activator.CreateInstance(typeof(RefCell), true)));
        Probe("activator, declared constructor", () => Describe(Activator.CreateInstance(typeof(RefMade))));
        Probe("activator, empty arguments", () =>
            Describe(Activator.CreateInstance(typeof(RefCell), Array.Empty<object>())));
        Probe("activator, arguments", () => Describe(Activator.CreateInstance(typeof(RefCell), new object[] { 1 })));
        Probe("constructor", () =>
            Describe(typeof(RefCell).GetConstructor(new[] { typeof(int) })!.Invoke(new object[] { 1 })));
        Probe("uninitialized", () => Describe(RuntimeHelpers.GetUninitializedObject(typeof(RefCell))));
        Probe("array", () => Describe(Array.CreateInstance(typeof(RefCell), 1)));
        Probe("array, two dimensions", () => Describe(Array.CreateInstance(typeof(RefCell), 1, 2)));
        Probe("array of void", () => Describe(Array.CreateInstance(typeof(void), 1)));
        Console.WriteLine("by-ref-like constructions end");
    }

    // RuntimeHelpers.Box boxes the value a reference points at under a run-time type
    // handle, as the value's own box equals and hashes: a small primitive widened, a
    // Nullable<T> as null or its T. It refuses void with ArgumentException and a
    // by-ref-like type with NotSupportedException.
    internal static void RunBoxesByTypeHandle()
    {
        Console.WriteLine("== boxes by type handle ==");
        Probe("int", () => BoxAs(7, typeof(int)));
        Probe("short", () => BoxAs((short)-3, typeof(short)));
        Probe("bool", () => BoxAs(true, typeof(bool)));
        Probe("enum", () => BoxAs(2L, typeof(Wide)));
        Probe("nullable with a value", () => BoxAs((int?)5, typeof(int?)));
        Probe("empty nullable", () => BoxAs((int?)null, typeof(int?)));
        Probe("void", () => BoxAs((byte)0, typeof(void)));
        Probe("by-ref-like", () => BoxAs((byte)0, typeof(RefCell)));
        Console.WriteLine("boxes by type handle end");
    }

    private static string BoxAs<T>(T value, Type type)
    {
        object? boxed = RuntimeHelpers.Box(ref Unsafe.As<T, byte>(ref value), type.TypeHandle);
        if (boxed is null)
            return "null";
        return Describe(boxed) + " equal=" + boxed.Equals(value)
            + "/" + (boxed.GetHashCode() == value!.GetHashCode());
    }

    private static unsafe string Pointed(object? value) =>
        value is Pointer ? Describe(value) + ":0x" + ((long)Pointer.Unbox(value)).ToString("x") : Describe(value);

    private static MethodInfo PointerMethod(string name) => typeof(PointerTarget).GetMethod(name)!;

    private static string PointerCall(string name, params object?[] args) =>
        Pointed(PointerMethod(name).Invoke(null, args));

    private static object? DeepPointerBox(string name) =>
        typeof(DeepPointerTarget).GetMethod(name)!.Invoke(null, null);

    private static string DeepPointerCall(string name, params object?[] args) =>
        Pointed(typeof(DeepPointerTarget).GetMethod(name)!.Invoke(null, args));

    // Invoke and DynamicInvoke box an unmanaged pointer result, read through a by-ref
    // result too, as a System.Reflection.Pointer of the pointer type and a function
    // pointer as an IntPtr; a pointer parameter takes such a box of its own type, of
    // a primitive pointee of the same width and kind one level deep, or any as void*.
    // A pointer type counts every level, a function pointer type is its signature
    // and calling convention, and a refused argument names the parameter type as
    // .NET formats it.
    internal static unsafe void RunPointerReturns()
    {
        Console.WriteLine("== pointer returns ==");
        Probe("pointer", () => PointerCall("Address"));
        Probe("null pointer", () => PointerCall("Null"));
        Probe("void pointer", () => PointerCall("Untyped"));
        Probe("pointer to pointer", () => PointerCall("Twice"));
        Probe("struct pointer", () => PointerCall("Spot"));
        Probe("function pointer", () => PointerCall("Entry"));
        Probe("ref pointer", () => PointerCall("Cell"));
        Probe("property", () => Pointed(typeof(PointerTarget).GetProperty("Property")!.GetValue(null)));
        Probe("virtual", () => Pointed(PointerMethod("Virtual").Invoke(new PointerDerived(), null)));
        Probe("dynamic invoke", () => Pointed(((PointerSource)PointerTarget.Address).DynamicInvoke()));
        Probe("dynamic invoke, created", () =>
            Pointed(Delegate.CreateDelegate(typeof(PointerSource), PointerMethod("Address")).DynamicInvoke()));
        object box = PointerMethod("Address").Invoke(null, null)!;
        object none = PointerMethod("Null").Invoke(null, null)!;
        object untyped = PointerMethod("Untyped").Invoke(null, null)!;
        object twice = PointerMethod("Twice").Invoke(null, null)!;
        object spot = PointerMethod("Spot").Invoke(null, null)!;
        Probe("type", () => (box is Pointer) + "/" + box.GetType().Name + "/" + box.GetType().Namespace + "/" + box);
        Probe("equals", () => box.Equals(PointerMethod("Address").Invoke(null, null)) + "/" + box.Equals(untyped)
            + "/" + box.Equals(null) + "/" + none.Equals(PointerMethod("Null").Invoke(null, null)));
        Probe("hash", () => (box.GetHashCode() == ((nuint)0x1230).GetHashCode()) + "/"
            + (none.GetHashCode() == ((nuint)0).GetHashCode()));
        Probe("fresh box", () => ReferenceEquals(box, PointerMethod("Address").Invoke(null, null)).ToString());
        Probe("unbox string", () => ((long)Pointer.Unbox("x")).ToString());
        Probe("int*, int* box", () => PointerCall("Int", box));
        Probe("int*, null pointer box", () => PointerCall("Int", none));
        Probe("int*, void* box", () => PointerCall("Int", untyped));
        Probe("int*, int** box", () => PointerCall("Int", twice));
        Probe("int*, struct pointer box", () => PointerCall("Int", spot));
        Probe("uint*, int* box", () => PointerCall("UInt", box));
        Probe("long*, int* box", () => PointerCall("Long", box));
        Probe("void*, int* box", () => PointerCall("Void", box));
        Probe("void*, int** box", () => PointerCall("Void", twice));
        Probe("void*, void* box", () => PointerCall("Void", untyped));
        Probe("int**, int** box", () => PointerCall("IntTwice", twice));
        Probe("int**, int* box", () => PointerCall("IntTwice", box));
        Probe("long**, int** box", () => PointerCall("LongTwice", twice));
        Probe("ref int*, int* box", () =>
        {
            object?[] args = { box };
            PointerMethod("Retarget").Invoke(null, args);
            return Pointed(args[0]);
        });
        Probe("dynamic invoke, int* box", () => Pointed(((PointerSink)PointerTarget.Int).DynamicInvoke(box)));
        Probe("dynamic invoke, void* box", () => Pointed(((PointerSink)PointerTarget.Int).DynamicInvoke(untyped)));
        Probe("five levels", () => DeepPointerCall("Five"));
        Probe("six levels", () => DeepPointerCall("Six"));
        Probe("ref five levels", () => DeepPointerCall("FiveCell"));
        Probe("int*****, int***** box", () => DeepPointerCall("FiveSink", DeepPointerBox("Five")));
        Probe("int*****, ref int***** box", () => DeepPointerCall("FiveSink", DeepPointerBox("FiveCell")));
        Probe("int*****, int****** box", () => DeepPointerCall("FiveSink", DeepPointerBox("Six")));
        Probe("uint*****, int***** box", () => DeepPointerCall("UIntFiveSink", DeepPointerBox("Five")));
        Probe("void*, int***** box", () => DeepPointerCall("VoidSink", DeepPointerBox("Five")));
        Probe("int*****, string", () => DeepPointerCall("FiveSink", "x"));
        object cell = DeepPointerBox("EntryCell")!;
        Probe("function pointer pointer", () => Pointed(cell));
        Probe("delegate*<int>*, delegate*<int>* box", () => DeepPointerCall("EntryCellSink", cell));
        Probe("delegate*<long>*, delegate*<int>* box", () => DeepPointerCall("LongEntryCellSink", cell));
        Probe("delegate* unmanaged<int>*, delegate*<int>* box", () => DeepPointerCall("UnmanagedCellSink", cell));
        Probe("delegate*<int>*, int* box", () => DeepPointerCall("EntryCellSink", box));
        Probe("void*, delegate*<int>* box", () => DeepPointerCall("VoidSink", cell));
        Probe("delegate*<int>*, string", () => DeepPointerCall("EntryCellSink", "x"));
        Probe("delegate*<int>, string", () => DeepPointerCall("EntrySink", "x"));
        Probe("delegate*<int>, IntPtr", () => DeepPointerCall("EntrySink", (nint)0x88));
        Probe("delegate*<int>, Pointer box", () => DeepPointerCall("EntrySink", box));
        Probe("delegate*<ref int, out long, int[], void>, string", () => DeepPointerCall("WideEntrySink", "x"));
        Probe("delegate* unmanaged[Cdecl]<delegate*<int>, int*>, string", () => DeepPointerCall("NestedEntrySink", "x"));
        Probe("ref delegate*<int>, IntPtr", () => DeepPointerCall("ByRefEntrySink", (nint)0x88));
        Console.WriteLine("pointer returns end");
    }
}
