using System;
using System.Globalization;
using System.Reflection;

// Roslyn emits namespaces in reverse order of first declaration, so default compile
// order emits this one after the sections that print metadata token values;
// compiling the file last would emit it first and move those tokens.
namespace InvokeMemberSubset;

// Only this section names these types, so the InvokeMember sites are the only
// producers of the member bodies they run.
public class Target
{
    public int Field = 7;
    public int[] Arr = { 10, 20, 30 };
    public static int StaticField = 3;
    public int PropValue = 5;

    public Target() { }

    public Target(int field) => Field = field;

    public int Prop { get => PropValue; set => PropValue = value; }

    public int WriteOnly { set { } }

    public int Bar(int x) => x + 1;

    public string Bar(string s) => "s:" + s;

    public int Baz(int x) => x * 10;

    public static int Stat(int x) => x * 2;

    public long Wide(long x) => x;

    public int Opt(int a, int b = 5) => a + b;

    public string Named(int a, string b) => a + "/" + b;

    public string Amb(object a, string b) => "os";

    public string Amb(string a, object b) => "so";

    public void Throw() => throw new InvalidOperationException("boom");

    public override string ToString() => "Target!";
}

public class Indexed
{
    public int[] Data = { 1, 2, 3 };

    public int this[int i] { get => Data[i]; set => Data[i] = value; }
}

[DefaultMember("Pick")]
public class DefaultMethod
{
    public string Pick(int x) => "pick" + x;
}

public class DerivedDefault : DefaultMethod { }

public class HideBase
{
    public int H(int x) => 1;
}

public class HideDerived : HideBase
{
    public new int H(int x) => 2;
}

public class Open<T>
{
    public void Clear() { }
}

public class ValuePick
{
    public string Pick(int x) => "int";

    public string Pick(object x) => "object";
}

public class RefPick
{
    public string Select(int x) => "int";

    public string Select(ref int x) => "ref int";
}

// int[] and uint[] are mutually assignable, so only the exact type ranks them.
public class ArrayPick
{
    public string Write(int[] x) => "int[]";

    public string Write(uint[] x) => "uint[]";
}

public class GenericPick
{
    public string Pick(object x) => "object";

    public string Pick<T>(T x) => "generic";
}

// For a byte, long and ulong tie and int outranks long; int and ulong also tie,
// but the binder's in-order walk never ranks them against each other.
public class WidenPick
{
    public static string Pick(long x) => "long";

    public static string Pick(ulong x) => "ulong";

    public static string Pick(int x) => "int";
}

// Declares no ToString, so the default-member fallback binds Object's.
public class Plain { }

public class OnlyDynamicHost
{
    public int OnlyDynamic(int x) => x * 100;
}

public sealed class CustomBinder : Binder
{
    public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value,
        CultureInfo culture) => throw new NotImplementedException();

    public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args,
        ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state)
        => throw new NotImplementedException();

    public override object ChangeType(object value, Type type, CultureInfo culture)
        => throw new NotImplementedException();

    public override void ReorderArgumentArray(ref object[] args, object state)
        => throw new NotImplementedException();

    public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types,
        ParameterModifier[] modifiers) => throw new NotImplementedException();

    public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType,
        Type[] indexes, ParameterModifier[] modifiers) => throw new NotImplementedException();
}

internal static class Program
{
    private const BindingFlags PI = BindingFlags.Public | BindingFlags.Instance;
    private const BindingFlags IM = BindingFlags.InvokeMethod;
    private const BindingFlags GetField = BindingFlags.GetField;
    private const BindingFlags SetField = BindingFlags.SetField;
    private const BindingFlags GetProperty = BindingFlags.GetProperty;
    private const BindingFlags SetProperty = BindingFlags.SetProperty;

    private static void Show(string label, Func<object> read)
    {
        try
        {
            Console.WriteLine(label + " = " + (read() ?? "null"));
        }
        catch (Exception e)
        {
            Console.WriteLine(label + " ! " + e.GetType().Name + ": " + e.Message
                + (e.InnerException is null ? ""
                    : " | inner " + e.InnerException.GetType().Name + ": " + e.InnerException.Message));
        }
    }

    internal static void Run()
    {
        Console.WriteLine("== Type.InvokeMember ==");
        var t = typeof(Target);
        var o = new Target();
        Show("invoke int", () => t.InvokeMember("Bar", IM | PI, null, o, new object[] { 1 }));
        Show("invoke string", () => t.InvokeMember("Bar", IM | PI, null, o, new object[] { "x" }));
        Show("default flags instance", () => t.InvokeMember("Bar", IM, null, o, new object[] { 1 }));
        Show("default flags static", () => t.InvokeMember("Stat", IM, null, null, new object[] { 4 }));
        Show("instance without public", () => t.InvokeMember("Bar", IM | BindingFlags.Instance, null, o, new object[] { 1 }));
        Show("widen int to long", () => t.InvokeMember("Wide", IM | PI, null, o, new object[] { 3 }));
        Show("wrong argument type", () => t.InvokeMember("Bar", IM | PI, null, o, new object[] { 1.5 }));
        Show("wrong arity", () => t.InvokeMember("Bar", IM | PI, null, o, new object[] { 1, 2 }));
        Show("ambiguous", () => t.InvokeMember("Amb", IM | PI, null, o, new object[] { "a", "b" }));
        Show("value over object",
            () => typeof(ValuePick).InvokeMember("Pick", IM | PI, null, new ValuePick(), new object[] { 7 }));
        Show("by-ref sibling",
            () => typeof(RefPick).InvokeMember("Select", IM | PI, null, new RefPick(), new object[] { 7 }));
        Show("exact array", () => typeof(ArrayPick).InvokeMember("Write", IM | PI, null, new ArrayPick(),
            new object[] { new uint[] { 1 } }));
        Show("generic sibling", () =>
        {
            // Compiles Pick<int>, whose row then stands for the generic definition.
            new GenericPick().Pick(0);
            return typeof(GenericPick).InvokeMember("Pick", IM | PI, null, new GenericPick(), new object[] { 7 });
        });
        Show("widening past ambiguity",
            () => typeof(WidenPick).InvokeMember("Pick", IM, null, null, new object[] { (byte)7 }));
        Show("missing", () => t.InvokeMember("Nope", IM | PI, null, o, null));
        Show("null target", () => t.InvokeMember("Bar", IM | PI, null, null, new object[] { 1 }));
        Show("target throws", () => t.InvokeMember("Throw", IM | PI, null, o, null));
        Show("target throws unwrapped",
            () => t.InvokeMember("Throw", IM | PI | BindingFlags.DoNotWrapExceptions, null, o, null));
        Show("ignore case", () => t.InvokeMember("bar", IM | PI | BindingFlags.IgnoreCase, null, o, new object[] { 1 }));
        Show("case miss", () => t.InvokeMember("bar", IM | PI, null, o, new object[] { 1 }));
        Show("prefix one", () => t.InvokeMember("Wi*", IM | PI, null, o, new object[] { 9 }));
        Show("prefix ambiguous", () => t.InvokeMember("Ba*", IM | PI, null, o, new object[] { 9 }));
        Show("prefix string", () => t.InvokeMember("Ba*", IM | PI, null, o, new object[] { "q" }));
        Show("prefix star", () => t.InvokeMember("*", IM | PI, null, o, new object[] { "q" }));
        Show("optional without flag", () => t.InvokeMember("Opt", IM | PI, null, o, new object[] { 1 }));
        Show("optional binding",
            () => t.InvokeMember("Opt", IM | PI | BindingFlags.OptionalParamBinding, null, o, new object[] { 1 }));
        Show("hide by signature",
            () => typeof(HideDerived).InvokeMember("H", IM | PI, null, new HideDerived(), new object[] { 1 }));
        Show("exception get type",
            () => typeof(Exception).InvokeMember("GetType", IM, null, new ArgumentException(), null));
        Show("default binder", () => t.InvokeMember("Bar", IM | PI, Type.DefaultBinder, o, new object[] { 1 }));
        Show("culture overload",
            () => t.InvokeMember("Bar", IM | PI, null, o, new object[] { 4 }, CultureInfo.InvariantCulture));
        Show("named overload", () => t.InvokeMember("Bar", IM | PI, null, o, new object[] { 6 }, null, null, null));
        Show("named too many",
            () => t.InvokeMember("Named", IM | PI, null, o, new object[] { 2 }, null, null, new[] { "a", "b" }));
        Show("named null",
            () => t.InvokeMember("Named", IM | PI, null, o, new object[] { 2, "b" }, null, null, new string[] { null }));

        Show("empty name", () => t.InvokeMember("", IM | PI, null, o, null));
        Show("dispid 0", () => t.InvokeMember("[DISPID=0]", IM | PI, null, o, null));
        Show("empty name inherited", () => typeof(Plain).InvokeMember("", IM | PI, null, new Plain(), null));
        Show("default member",
            () => typeof(DefaultMethod).InvokeMember("", IM | PI, null, new DefaultMethod(), new object[] { 4 }));
        Show("inherited default member",
            () => typeof(DerivedDefault).InvokeMember("", IM | PI, null, new DerivedDefault(), new object[] { 5 }));
        Show("default indexer",
            () => typeof(Indexed).InvokeMember("", GetProperty | PI, null, new Indexed(), new object[] { 1 }));
        Show("invoke or default indexer",
            () => typeof(Indexed).InvokeMember("", IM | GetProperty | PI, null, new Indexed(), new object[] { 2 }));
        Show("null name", () => t.InvokeMember(null, IM | PI, null, o, null));

        Show("get field", () => t.InvokeMember("Field", GetField | PI, null, o, null));
        Show("get static field default flags", () => t.InvokeMember("StaticField", GetField, null, null, null));
        Show("set field", () => { t.InvokeMember("Field", SetField | PI, null, o, new object[] { 42 }); return o.Field; });
        Show("set field widen",
            () => { t.InvokeMember("Field", SetField | PI, null, o, new object[] { (short)8 }); return o.Field; });
        Show("get field missing", () => t.InvokeMember("Nope", GetField | PI, null, o, null));
        Show("set field missing", () => t.InvokeMember("Nope", SetField | PI, null, o, new object[] { 1 }));
        Show("get field with arguments", () => t.InvokeMember("Field", GetField | PI, null, o, new object[] { 1 }));
        Show("set field without arguments", () => t.InvokeMember("Field", SetField | PI, null, o, null));
        Show("set field two arguments", () => t.InvokeMember("Field", SetField | PI, null, o, new object[] { 1, 2 }));
        Show("field falls to property", () => t.InvokeMember("Prop", GetField | GetProperty | PI, null, o, null));
        Show("field falls to method", () => t.InvokeMember("Bar", GetField | IM | PI, null, o, new object[] { 1 }));

        Show("get property", () => t.InvokeMember("Prop", GetProperty | PI, null, o, null));
        Show("set property",
            () => { t.InvokeMember("Prop", SetProperty | PI, null, o, new object[] { 11 }); return o.PropValue; });
        Show("get write-only property", () => t.InvokeMember("WriteOnly", GetProperty | PI, null, o, null));
        Show("invoke or get property", () => t.InvokeMember("Prop", IM | GetProperty | PI, null, o, null));
        Show("put dispatch property", () =>
        {
            t.InvokeMember("Prop", BindingFlags.PutDispProperty | PI, null, o, new object[] { 12 });
            return o.PropValue;
        });
        Show("get indexer",
            () => typeof(Indexed).InvokeMember("Item", GetProperty | PI, null, new Indexed(), new object[] { 0 }));
        Show("set indexer", () =>
        {
            var indexed = new Indexed();
            typeof(Indexed).InvokeMember("Item", SetProperty | PI, null, indexed, new object[] { 0, 9 });
            return indexed.Data[0];
        });

        Show("create", () => t.InvokeMember("ignored", BindingFlags.CreateInstance, null, null, null));
        Show("create with arguments", () => ((Target)t.InvokeMember("ignored", BindingFlags.CreateInstance | PI,
            null, null, new object[] { 33 })).Field);
        Show("create without constructor",
            () => t.InvokeMember("", BindingFlags.CreateInstance | PI, null, null, new object[] { "s" }));

        Show("flags without access", () => t.InvokeMember("Bar", PI, null, o, new object[] { 1 }));
        Show("flags create and invoke", () => t.InvokeMember("Bar", BindingFlags.CreateInstance | IM | PI, null, o, null));
        Show("flags get and set field", () => t.InvokeMember("Field", GetField | SetField | PI, null, o, null));
        Show("flags get field and set property",
            () => t.InvokeMember("Field", GetField | SetProperty | PI, null, o, null));
        Show("flags set field and get property",
            () => t.InvokeMember("Field", SetField | GetProperty | PI, null, o, new object[] { 1 }));
        Show("flags set field and invoke", () => t.InvokeMember("Field", SetField | IM | PI, null, o, new object[] { 1 }));
        Show("flags get and set property", () => t.InvokeMember("Prop", GetProperty | SetProperty | PI, null, o, null));
        Show("flags set property and invoke",
            () => t.InvokeMember("Prop", SetProperty | IM | PI, null, o, new object[] { 1 }));

        Show("generic parameter",
            () => typeof(Open<>).GetGenericArguments()[0].InvokeMember("Clear", IM, null, null, null));
        Show("open generic", () => typeof(Open<>).InvokeMember("Clear", IM | PI, null, null, null));
        Console.WriteLine("Type.InvokeMember end");
    }

    // .NET runs these; AOT-compiled code refuses each with a catchable
    // PlatformNotSupportedException.
    internal static void RunBoundaries(string name)
    {
        Console.WriteLine("== Type.InvokeMember AOT boundaries ==");
        var t = typeof(Target);
        var o = new Target();
        Show("custom binder", () => t.InvokeMember("Bar", IM | PI, new CustomBinder(), o, new object[] { 1 }));
        Show("named arguments",
            () => t.InvokeMember("Named", IM | PI, null, o, new object[] { "b", 2 }, null, null, new[] { "b", "a" }));
        Show("array field get", () => t.InvokeMember("Arr", GetField | PI, null, o, new object[] { 1 }));
        Show("array field set", () =>
        {
            t.InvokeMember("Arr", SetField | PI, null, o, new object[] { 2, 99 });
            return o.Arr[2];
        });
        Show("non-constant name",
            () => typeof(OnlyDynamicHost).InvokeMember(name, IM | PI, null, new OnlyDynamicHost(), new object[] { 3 }));
        Console.WriteLine("Type.InvokeMember AOT boundaries end");
    }
}
