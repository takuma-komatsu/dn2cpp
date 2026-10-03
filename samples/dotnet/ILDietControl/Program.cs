using System;
using System.Globalization;
using System.Reflection;
using ILDietControlLib;

namespace ILDietControl;

internal static class Program
{
    private static unsafe void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("initializers=" + Initialization.Value + ":" + StaticInitialization.Read());
        IFoo inherited = new Derived();
        Console.WriteLine("inherited=" + inherited.Foo());
        IGeneric<int> number = new ExplicitGeneric<int>();
        IGeneric<string> text = new ExplicitGeneric<string>();
        Console.WriteLine("generic=" + number.Identity(8) + ":" + text.Identity("kept"));
        Console.WriteLine("static-interface=" + RunStatic<StaticValue>(3));
        Func<int, int> callback = Callbacks.ManagedDelegate;
        Console.WriteLine("delegate=" + callback(5));
        var layout = new Layout { Used = 7, Tail = 4 };
        Console.WriteLine("layout=" + sizeof(Layout) + ":" + (layout.Used + layout.Tail));
        // Loose's type token precedes the first constructing call and the others
        // follow it: constructors survive in either order.
        Console.WriteLine("activator=" + ((Shape)Activator.CreateInstance(typeof(Loose))!).Who());
        Console.WriteLine("activator-closed=" + ((Shape)Activator.CreateInstance(typeof(Box<int>))!).Who());
        Type made = typeof(Box<>).MakeGenericType(typeof(string));
        Console.WriteLine("activator-made=" + ((Shape)Activator.CreateInstance(made)!).Who());
        ConstructorInfo empty = typeof(Seeded).GetConstructor(Type.EmptyTypes)!;
        ConstructorInfo sized = typeof(Seeded).GetConstructor(new[] { typeof(int) })!;
        Console.WriteLine("ctor-info=" + ((Shape)empty.Invoke(null)).Who() + ":"
            + ((Shape)sized.Invoke(new object[] { 5 })).Who());
        Console.WriteLine("called-only=" + CalledOnly.Name());
        // The method group must keep the element constructor even though no IL names it.
        Array cells = new Cell[2];
        Action initialize = cells.Initialize;
        initialize();
        Console.WriteLine("array-initialize-group=" + ((Cell[])cells)[1].Value);
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_ATTRIBUTE_OBJECT") == "1")
            return;
        Console.WriteLine("== attribute construction roots ==");
        SelectAttribute selection = typeof(Holder).GetCustomAttribute<SelectAttribute>()!;
        Console.WriteLine("attribute-argument=" + Construct(selection.Argument));
        Console.WriteLine("attribute-field=" + Construct(selection.Field!));
        Console.WriteLine("attribute-property=" + Construct(selection.Property!));
        Type[] listed = typeof(Holder).GetCustomAttribute<SelectAllAttribute>()!.Types;
        Console.WriteLine("attribute-array=" + Construct(listed[0]) + ":" + Construct(listed[1]));
        Console.WriteLine("attribute-object="
            + Construct((Type)typeof(Holder).GetCustomAttribute<BoxedAttribute>()!.Value));
        Console.WriteLine("attribute construction roots end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_NAMED_REFLECTION") == "1")
            return;
        // No IL calls these members: only their constant names select them.
        Console.WriteLine("== constant-name reflection ==");
        Report("method-found", () => typeof(Named).GetMethod("Twice") is not null);
        Report("method-static", () => typeof(Named).GetMethod("Twice")!.Invoke(null, new object[] { 21 }));
        var named = new Named();
        Report("method-instance",
            () => typeof(Named).GetMethod("Describe")!.Invoke(named, new object[] { "describe" }));
        Report("property-set", () =>
        {
            typeof(Named).GetProperty("Label")!.SetValue(named, "moved");
            return "done";
        });
        Report("property-get", () => typeof(Named).GetProperty("Label")!.GetValue(named));
        Report("field-get", () => typeof(Named).GetField("Count")!.GetValue(named));
        Report("constructor", () => ((Shape)typeof(NamedBuilt).GetConstructor(new[] { typeof(string) })!
            .Invoke(new object[] { "made" })).Who());
        Console.WriteLine("constant-name reflection end");
    }

    private static int RunStatic<T>(int value) where T : IStatic<T> => T.Evaluate(value);

    private static string Construct(Type type) => ((Shape)Activator.CreateInstance(type)!).Who();

    private static void Report(string label, Func<object?> read)
    {
        string text;
        try
        {
            text = read()?.ToString() ?? "null";
        }
        catch (Exception e)
        {
            text = e.GetType().Name;
        }
        Console.WriteLine(label + "=" + text);
    }
}

internal sealed class Named
{
    private string _label = "start";

    public int Count = 2;

    public static int Twice(int value) => value * 2;

    public string Describe(string prefix) => prefix + ":" + _label;

    public string Label
    {
        get => _label;
        set => _label = value;
    }
}

internal sealed class NamedBuilt : Shape
{
    private readonly string _text;

    public NamedBuilt(string text) => _text = text;

    public override string Who() => "named-built:" + _text;
}

public static class UnusedAppType
{
    public static int UnusedPublic() => -4;
}

internal abstract class Shape
{
    public abstract string Who();
}

internal struct Cell
{
    public int Value;

    public Cell() { Value = 3; }
}

internal sealed class Loose : Shape
{
    public override string Who() => "loose";
}

internal sealed class Box<T> : Shape
{
    public override string Who() => "box:" + typeof(T).Name;
}

internal sealed class Seeded : Shape
{
    private readonly int _value;

    public Seeded() => _value = -1;

    public Seeded(int value) => _value = value;

    public override string Who() => "seeded:" + _value;
}

// A static call token alone never selects the instance constructor.
internal sealed class CalledOnly
{
    public static string Name() => "called-only";
}

internal sealed class SelectAttribute : Attribute
{
    public Type? Field;

    public SelectAttribute(Type argument) => Argument = argument;

    public Type Argument { get; }

    public Type? Property { get; set; }
}

internal sealed class SelectAllAttribute : Attribute
{
    public SelectAllAttribute(params Type[] types) => Types = types;

    public Type[] Types { get; }
}

internal sealed class BoxedAttribute : Attribute
{
    public BoxedAttribute(object value) => Value = value;

    public object Value { get; }
}

// Only Holder's attribute arguments name the Labeled subclasses.
[Select(typeof(ByArgument), Field = typeof(ByField), Property = typeof(ByProperty))]
[SelectAll(typeof(ByArrayFirst), typeof(ByArraySecond))]
[Boxed(typeof(ByObject))]
internal static class Holder { }

internal abstract class Labeled : Shape
{
    private readonly string _label;

    protected Labeled(string label) => _label = label;

    public override string Who() => _label;
}

internal sealed class ByArgument : Labeled
{
    public ByArgument() : base("by-argument") { }
}

internal sealed class ByField : Labeled
{
    public ByField() : base("by-field") { }
}

internal sealed class ByProperty : Labeled
{
    public ByProperty() : base("by-property") { }
}

internal sealed class ByArrayFirst : Labeled
{
    public ByArrayFirst() : base("by-array-first") { }
}

internal sealed class ByArraySecond : Labeled
{
    public ByArraySecond() : base("by-array-second") { }
}

internal sealed class ByObject : Labeled
{
    public ByObject() : base("by-object") { }
}
