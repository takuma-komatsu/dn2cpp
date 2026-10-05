#nullable enable
using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
namespace OrdinaryReflectionLeaves;
interface IGreeting
{
    string Hello();
    sealed string Shout() => Hello().ToUpperInvariant();
}
class Greeting : IGreeting
{
    public string Hello() => "custom-hello";
    public virtual string Shout() => "class-shout";
}
interface IFactory
{
    static virtual string Virt() => "virt";
    static virtual string Echo(string text) => text;
    static abstract string Abs();
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
class Visible
{
    private Visible(int value) { }
    protected Visible(long value) { }
    public Visible() { }
    internal Visible(string value) { }
    protected internal Visible(double value) { }
    private protected Visible(char value) { }
    private void Hidden() { }
    protected void Fam() { }
    public void Pub() { }
    internal void Asm() { }
    protected internal void FamOrAsm() { }
    private protected void FamAndAsm() { }
}
#pragma warning disable CS0169, CS0649, SYSLIB0050
class VisibleFields
{
    private int Hidden;
    protected int Fam;
    public int Pub;
    internal int Asm;
    protected internal int FamOrAsm;
    private protected int FamAndAsm;
    [NonSerialized] public int Skipped;
}
#pragma warning restore CS0169, CS0649, SYSLIB0050
interface ICounter { int Get(); }
struct Counter : ICounter
{
    public int Value;
    public int Get() => Value;
}
class ConstructorCell
{
    public int Value;
    public ConstructorCell(int value) => Value = value;
}
class PropertyCell
{
    public int Value { get; set; }
    private int Private { get; set; }
    public int ReadOnly => Value;
    public int this[int index] { get => Value + index; set => Value = value - index; }
}
class AccessorBase
{
    public int Inherited { get; protected set; } = 4;
    public int PrivateInherited { get; private set; } = 6;
    public int WriteInherited { private get; set; } = 8;
}
class AccessorCell : AccessorBase
{
    private int _number;
    public int Value { get; private set; } = 1;
    private int Hidden { get; set; } = 2;
    public int ReadOnly => _number;
    public int WriteOnly { set => _number = value; }
    public static int Shared { get; set; }
    public int Reverse { set => _number = value; get => _number; }
    public int this[int index] { get => _number + index; set => _number = value - index; }
}
static class Program
{
    private static string AccessorNames(MethodInfo[] methods)
    {
        string names = "";
        foreach (MethodInfo method in methods)
            names += (names.Length == 0 ? "" : ",") + method.Name;
        return names;
    }
    private static void Accessors(Type type, string name)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        PropertyInfo property = type.GetProperty(name, all)!;
        MethodInfo[] methods = property.GetAccessors(true);
        bool same = methods.GetType() == typeof(MethodInfo[])
            && methods.Length == (property.GetMethod is null ? 0 : 1) + (property.SetMethod is null ? 0 : 1)
            && ReferenceEquals(property.GetGetMethod(), property.GetGetMethod(false))
            && ReferenceEquals(property.GetSetMethod(), property.GetSetMethod(false))
            && ReferenceEquals(property.GetMethod, property.GetGetMethod(true))
            && ReferenceEquals(property.SetMethod, property.GetSetMethod(true));
        foreach (MethodInfo method in methods)
            same &= ReferenceEquals(method, method.Name.StartsWith("get_") ? property.GetGetMethod(true) : property.GetSetMethod(true))
                && ReferenceEquals(method, type.GetMethod(method.Name, all));
        Console.WriteLine("accessors " + type.Name + "/" + name + " default=" + AccessorNames(property.GetAccessors())
            + ";false=" + AccessorNames(property.GetAccessors(false)) + ";true=" + AccessorNames(methods)
            + ";identity=" + same + ";declared=" + property.DeclaringType!.Name
            + ";reflected=" + property.ReflectedType!.Name);
    }
    private static void RunPropertyAccessors()
    {
        Console.WriteLine("== property accessor arrays ==");
        foreach (string name in new[] { "Value", "Hidden", "ReadOnly", "WriteOnly", "Shared", "Item", "Reverse", "Inherited", "PrivateInherited", "WriteInherited" })
            Accessors(typeof(AccessorCell), name);
        Accessors(typeof(AccessorBase), "PrivateInherited");
        Accessors(typeof(AccessorBase), "WriteInherited");
        var cell = new AccessorCell();
        MethodInfo[] value = typeof(AccessorCell).GetProperty("Value")!.GetAccessors(true);
        value[1].Invoke(cell, new object[] { 15 });
        Console.WriteLine("accessors invoke value=" + value[0].Invoke(cell, null));
        MethodInfo[] shared = typeof(AccessorCell).GetProperty("Shared")!.GetAccessors();
        shared[1].Invoke(null, new object[] { 23 });
        Console.WriteLine("accessors invoke static=" + shared[0].Invoke(null, null));
        MethodInfo[] indexed = typeof(AccessorCell).GetProperty("Item")!.GetAccessors();
        indexed[1].Invoke(cell, new object[] { 2, 21 });
        Console.WriteLine("accessors invoke indexed=" + indexed[0].Invoke(cell, new object[] { 2 }));
        MethodInfo[] inherited = typeof(AccessorCell).GetProperty("Inherited")!.GetAccessors(true);
        inherited[1].Invoke(cell, new object[] { 17 });
        Console.WriteLine("accessors invoke inherited=" + inherited[0].Invoke(cell, null));
        Fault("accessors null default", () => ((PropertyInfo)null!).GetAccessors());
        Fault("accessors null true", () => ((PropertyInfo)null!).GetAccessors(true));
        Console.WriteLine("property accessor arrays end");
    }
    private static int Twice(int value) => value * 2;
    private static long Thrice(long value) => value * 3;
    private static int Read(Counter value) => value.Value;
    private static int ReadInterface(ICounter value) => value.Get();
    private static int ReadObject(object value) => ((Counter)value).Value;
    private static void Fault(string label, Func<object?> call)
    {
        try
        {
            object? result = call();
            Console.WriteLine(label + ": " + (result ?? "<null>"));
        }
        catch (Exception error)
        {
            Console.WriteLine(label + ": " + error.GetType().Name + "/" + (error.InnerException?.GetType().Name ?? "<null>"));
        }
    }
    private static void ConstructorFault(string label, object[] arguments)
    {
        try
        {
            var value = (ConstructorCell)Activator.CreateInstance(typeof(ConstructorCell), arguments)!;
            Console.WriteLine(label + "=" + value.Value);
        }
        catch (MissingMethodException error)
        {
            Console.WriteLine(label + "=" + error.Message + "/" + error.HResult.ToString("X8"));
            Exception? inner = error.InnerException;
            Console.WriteLine(label + " inner=" + (inner is null ? "<null>"
                : inner.GetType().Name + "/" + inner.Message + "/" + inner.HResult.ToString("X8")));
        }
    }
    private static void BindFault(string label, Func<Delegate?> call)
    {
        try { Console.WriteLine(label + ": " + (call() is null ? "null" : "bound")); }
        catch (ArgumentException error)
        {
            Console.WriteLine(label + ": " + error.GetType().Name + "/" + (error.ParamName ?? "<null>")
                + "/" + error.HResult.ToString("X8") + "/" + error.Message);
        }
    }
    private static string Access(MethodBase method) =>
        (method.IsPublic ? "u" : "") + (method.IsPrivate ? "p" : "") + (method.IsFamily ? "f" : "")
        + (method.IsAssembly ? "a" : "") + (method.IsFamilyOrAssembly ? "o" : "")
        + (method.IsFamilyAndAssembly ? "n" : "") + (method.IsHideBySig ? "h" : "");
#pragma warning disable SYSLIB0050
    private static string Access(FieldInfo field) =>
        (field.IsPublic ? "u" : "") + (field.IsPrivate ? "p" : "") + (field.IsFamily ? "f" : "")
        + (field.IsAssembly ? "a" : "") + (field.IsFamilyOrAssembly ? "o" : "")
        + (field.IsFamilyAndAssembly ? "n" : "") + (field.IsNotSerialized ? "s" : "")
        + (field.IsPinvokeImpl ? "i" : "");
#pragma warning restore SYSLIB0050
    private static string ReturnModifierCount(ParameterInfo parameter, bool required)
    {
        try
        {
            Type[] modifiers = required ? parameter.GetRequiredCustomModifiers() : parameter.GetOptionalCustomModifiers();
            return modifiers.Length.ToString();
        }
        catch (PlatformNotSupportedException)
        {
            return nameof(PlatformNotSupportedException);
        }
    }
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        ReflectFieldValidationSubset.Program.Run();
        OrdinaryAmbiguousMatchSubset.Program.Run();
        Console.WriteLine("== ordinary reflection leaves ==");
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        foreach (string name in new[] { "Hidden", "Fam", "Pub", "Asm", "FamOrAsm", "FamAndAsm" })
            Console.WriteLine("method " + name + "=" + Access(typeof(Visible).GetMethod(name, all)!));
        foreach (Type parameter in new[] { typeof(int), typeof(long), typeof(string), typeof(double), typeof(char) })
            Console.WriteLine("constructor " + parameter.Name + "=" + Access(typeof(Visible).GetConstructor(all, null, new[] { parameter }, null)!));
        Console.WriteLine("constructor default=" + Access(typeof(Visible).GetConstructor(Type.EmptyTypes)!));
        foreach (string name in new[] { "Hidden", "Fam", "Pub", "Asm", "FamOrAsm", "FamAndAsm", "Skipped" })
            Console.WriteLine("field " + name + "=" + Access(typeof(VisibleFields).GetField(name, all)!));
        MethodInfo size = typeof(Unsafe).GetMethod("SizeOf")!;
        foreach (MethodInfo row in new[] { size, size.MakeGenericMethod(typeof(int)) })
            Console.WriteLine("SizeOf attributes=" + ((int)row.Attributes).ToString("X4") + ":" + row.IsHideBySig + ":" + ((int)row.MethodImplementationFlags).ToString("X4"));
        IGreeting greeting = new Greeting();
        Console.WriteLine("sealed direct=" + greeting.Shout());
        MethodInfo shout = typeof(IGreeting).GetMethod("Shout")!;
        var bound = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), greeting, shout);
        Console.WriteLine("sealed bound=" + bound() + "/" + bound.Method.DeclaringType!.Name + "." + bound.Method.Name);
        MethodInfo twice = typeof(Program).GetMethod(nameof(Twice), BindingFlags.NonPublic | BindingFlags.Static)!;
        Console.WriteLine("bind ordinary=" + ((Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), twice))(21));
        Console.WriteLine("bind generic=" + typeof(Program).GetMethod(nameof(Thrice), BindingFlags.NonPublic | BindingFlags.Static)!.CreateDelegate<Func<long, long>>()(4));
        object counter = new Counter { Value = 7 };
        Console.WriteLine("bind boxed=" + ((Func<int>)Delegate.CreateDelegate(typeof(Func<int>), counter, typeof(Counter).GetMethod("Get")!))());
        Console.WriteLine("bind first object=" + ((Func<int>)Delegate.CreateDelegate(typeof(Func<int>), counter, typeof(Program).GetMethod(nameof(ReadObject), BindingFlags.NonPublic | BindingFlags.Static)!))());
        Console.WriteLine("bind first interface=" + ((Func<int>)Delegate.CreateDelegate(typeof(Func<int>), counter, typeof(Program).GetMethod(nameof(ReadInterface), BindingFlags.NonPublic | BindingFlags.Static)!))());
        BindFault("bind value parameter", () => Delegate.CreateDelegate(typeof(Func<int>), counter, typeof(Program).GetMethod(nameof(Read), BindingFlags.NonPublic | BindingFlags.Static)!));
        BindFault("bind wrong signature", () => Delegate.CreateDelegate(typeof(Func<string>), twice));
        BindFault("bind declined signature", () => Delegate.CreateDelegate(typeof(Func<string>), twice, false));
        BindFault("Delegate type null", () => Delegate.CreateDelegate(null!, twice));
        BindFault("Delegate method null", () => Delegate.CreateDelegate(typeof(Func<int, int>), (MethodInfo)null!));
        BindFault("MethodInfo type null", () => twice.CreateDelegate(null!));
        BindFault("Delegate nondelegate", () => Delegate.CreateDelegate(typeof(string), twice));
        BindFault("MethodInfo nondelegate", () => twice.CreateDelegate(typeof(string)));
        MethodInfo abs = typeof(IFactory).GetMethod("Abs")!;
        Fault("static abstract invoke", () => abs.Invoke(null, null));
        Fault("static abstract unwrapped", () => abs.Invoke(null, BindingFlags.DoNotWrapExceptions, null, null, null));
        Fault("static abstract count", () => abs.Invoke(null, new object?[] { 1 }));
        foreach (string name in new[] { "Abs", "Virt" })
        {
            var open = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), typeof(IFactory).GetMethod(name)!);
            Fault("static interface bound " + name, () => open());
        }
        Fault("static virtual closed", () => Delegate.CreateDelegate(typeof(Func<string>), "x", typeof(IFactory).GetMethod("Echo")!));
        Fault("byref activator", () => Activator.CreateInstance(typeof(RefCell)));
        Fault("byref private activator", () => Activator.CreateInstance(typeof(RefCell), true));
        Fault("byref default constructor", () => Activator.CreateInstance(typeof(RefMade)));
        Fault("byref empty arguments", () => Activator.CreateInstance(typeof(RefCell), Array.Empty<object>()));
        Fault("byref arguments", () => Activator.CreateInstance(typeof(RefCell), new object[] { 1 }));
        Fault("byref constructor", () => typeof(RefCell).GetConstructor(new[] { typeof(int) })!.Invoke(new object[] { 1 }));
        Fault("byref uninitialized", () => RuntimeHelpers.GetUninitializedObject(typeof(RefCell)));
        Fault("byref array", () => Array.CreateInstance(typeof(RefCell), 1));
        Fault("byref array rank2", () => Array.CreateInstance(typeof(RefCell), 1, 2));
        PropertyInfo property = typeof(PropertyCell).GetProperty("Value")!;
        var cell = new PropertyCell();
        for (int round = 0; round < 2; round++)
        {
            property.SetValue(cell, 7 + round);
            Console.WriteLine("property " + round + "=" + property.GetValue(cell) + ":"
                + ReferenceEquals(property.GetGetMethod(), property.GetGetMethod()) + ":"
                + ReferenceEquals(property.GetSetMethod(), property.GetSetMethod()));
        }
        PropertyInfo item = typeof(PropertyCell).GetProperty("Item")!;
        item.SetValue(cell, 10, new object[] { 2 });
        Console.WriteLine("indexed property=" + item.GetValue(cell, new object[] { 2 }));
        Console.WriteLine("private accessor=" + (typeof(PropertyCell).GetProperty("Private", all)!.GetGetMethod() is null)
            + ":" + (typeof(PropertyCell).GetProperty("Private", all)!.GetGetMethod(true) is not null));
        Console.WriteLine("missing setter=" + (typeof(PropertyCell).GetProperty("ReadOnly")!.GetSetMethod() is null));
        Fault("property wrong receiver", () => property.GetValue("wrong"));
        Fault("property wrong value", () => { property.SetValue(cell, "wrong"); return null; });
        OrdinaryWideLookupSubset.Program.Run();
        Console.WriteLine("== constructor binder faults ==");
        ConstructorFault("constructor wrong type", new object[] { "wrong" });
        ConstructorFault("constructor wrong count", new object[] { 1, 2 });
        ConstructorFault("constructor matched", new object[] { 17 });
        Console.WriteLine("constructor binder faults end");
        Console.WriteLine("ordinary reflection leaves end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_RUNTIME_MEMBER_ATTRIBUTES") == "1")
            return;
        Console.WriteLine("== runtime member attributes ==");
        MethodInfo clone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Console.WriteLine("memberwise clone attributes=" + ((int)clone.Attributes).ToString("X4")
            + "/" + clone.IsFamily + "/" + clone.IsFamilyOrAssembly);
        Console.WriteLine("runtime member attributes end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_RUNTIME_RETURN_MODIFIERS") == "1")
            return;
        Console.WriteLine("== runtime return modifiers ==");
        MethodInfo[] rows = { clone, size, size.MakeGenericMethod(typeof(int)), size.MakeGenericMethod(typeof(string)) };
        string[] names = { "MemberwiseClone", "SizeOf definition", "SizeOf Int32", "SizeOf String" };
        for (int i = 0; i < rows.Length; i++)
        {
            MethodInfo row = rows[i];
            ParameterInfo result = row.ReturnParameter;
            Console.WriteLine("return modifiers " + names[i] + "=" + row.GetParameters().Length + "/" + result.Position
                + ":" + ReturnModifierCount(result, true) + "/" + ReturnModifierCount(result, false));
        }
        Console.WriteLine("runtime clone method display=" + clone.ToString());
        Console.WriteLine("runtime clone return display=" + clone.ReturnParameter.ToString());
        Console.WriteLine("runtime SizeOf return display=" + size.ReturnParameter.ToString());
        Console.WriteLine("runtime return modifiers end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_POINTER_FIELDS") == "1")
            return;
        ReflectFieldValidationSubset.Program.RunPointerFields();
        if (args.Length != 0 && args[0] == "before-object-method-enumeration")
            return;
        OrdinaryWideLookupSubset.ObjectMethods.Run();
        if (args.Length != 0 && args[0] == "before-property-accessors")
            return;
        RunPropertyAccessors();
    }
}
