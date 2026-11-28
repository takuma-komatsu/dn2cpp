using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using ILDietControlLib;
using MemberTypes = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

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
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_RUNTIME_REFLECTION") == "1")
            return;
        Console.WriteLine("== runtime reflection preservation ==");
        Type fieldType = typeof(ReflectionFields).GetField("Payload")!.FieldType;
        Report("field-type-construction", () => ((Shape)Activator.CreateInstance(fieldType)!).Who());
        Console.WriteLine("field-type-constructors=" + fieldType.GetConstructors().Length);
        Type libraryFieldType = typeof(ReflectionFields).GetField("LibraryPayload")!.FieldType;
        Report("library-field-type", () => libraryFieldType.GetProperty("Label")!
            .GetValue(Activator.CreateInstance(libraryFieldType)));
        Console.WriteLine("library-field-constructors=" + libraryFieldType.GetConstructors().Length);
        Console.WriteLine("called-only-constructors=" + typeof(CalledOnly).GetConstructors().Length);
        Type? byType = Type.GetType("ILDietControl.RuntimeNamed");
        Type? byAssembly = typeof(Program).Assembly.GetType("ILDietControl.RuntimeNamed");
        Console.WriteLine("type-name=" + (byType is not null));
        Console.WriteLine("assembly-name=" + (byAssembly is not null));
        bool listedRuntime = false, listedUnused = false, listedNestedFoo = false;
        foreach (Type listedType in typeof(Program).Assembly.GetTypes())
        {
            listedRuntime |= listedType.FullName == "ILDietControl.RuntimeNamed";
            listedUnused |= listedType.FullName == "ILDietControl.UnusedAppType";
            listedNestedFoo |= listedType.FullName == "ILDietControl.MethodLookupOwner+Foo";
        }
        Console.WriteLine("assembly-types=" + listedRuntime + ":" + listedUnused);
        Report("runtime-name-construction", () => ((Shape)Activator.CreateInstance(byType!)!).Who());
        Report("runtime-name-invoke", () => byAssembly!.GetMethod("Value")!.Invoke(null, null));
        Console.WriteLine("runtime reflection preservation end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_RETURN_PARAMETER_ATTRIBUTES") == "1")
            return;
        ReflectInvoke.ReturnParameterAttributesSubset.Program.Run();
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_INVOKE_MEMBER") == "1")
            return;
        // No IL calls these members: only InvokeMember's constant names select them.
        Console.WriteLine("== constant-name InvokeMember ==");
        var invoked = new Invoked();
        Report("invoke-method",
            () => typeof(Invoked).InvokeMember("Triple", BindingFlags.InvokeMethod, null, null, new object[] { 7 }));
        Report("invoke-property",
            () => typeof(Invoked).InvokeMember("Size", BindingFlags.GetProperty, null, invoked, null));
        Report("invoke-field",
            () => typeof(Invoked).InvokeMember("Total", BindingFlags.GetField, null, invoked, null));
        Report("invoke-default",
            () => typeof(Invoked).InvokeMember("", BindingFlags.InvokeMethod, null, invoked, new object[] { 2 }));
        Console.WriteLine("constant-name InvokeMember end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_ANNOTATED_TYPE_PARAMETERS") == "1")
            return;
        Console.WriteLine("== annotated Type parameters ==");
        Report("annotated accessors", () => AnnotatedAccessors(typeof(AnnotatedMembers),
            typeof(AnnotatedMembers).GetProperty("Value")!));
        Report("annotated inherited", () => AnnotatedInherited(typeof(AnnotatedMembers),
            typeof(AnnotatedBase).GetNestedType("Nested")!));
        Report("annotated methods", () => AnnotatedMethods(typeof(AnnotatedMethodOwner)));
        Report("annotated interface body", () => AnnotatedInterface(typeof(IAnnotatedChild),
            typeof(IAnnotatedChild).GetInterfaces()[0]));
        Report("annotated value", () => CreateAnnotatedValue(
            Type.GetType("ILDietControlLib.AnnotatedValue, ILDietControlLib")!));
        Console.WriteLine("annotated Type parameters end");

        var customer = new AnnotatedFieldCustomer();
        var view = new AnnotatedFieldView(typeof(AnnotatedFieldCustomer));
        Report("annotated field", () => view.Read(customer));
        Report("ordinary field", () => new OrdinaryFieldView().Read(customer));
        Report("mixed field", () => view.ReadMixed(
            Environment.GetEnvironmentVariable("DN2CPP_READ_UNPROVED_FIELD") == "1", customer));
        unprovedField = typeof(UnprovedFieldCustomer);
        if (Environment.GetEnvironmentVariable("DN2CPP_READ_UNPROVED_FIELD") == "1")
            Report("unproved annotated field", () => ReadUnprovedField(new UnprovedFieldCustomer()));

        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_GENERIC_PARAMETER_CONTRACTS") == "1")
            return;
        GenericParameterContracts.Run();

        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_TYPE_CONTRACTS") == "1")
            return;
        var derived = new TypeContractDerived();
        Report("type contract base", () => new TypeContractBase().ReadCode());
        Report("type contract derived", () => derived.ReadCode());
        Report("type contract interface", () => ReadSize(derived));
        Report("type contract template", () => ReadArgumentName(
            typeof(TypeContractTemplate<>).MakeGenericType(typeof(int))));
        Report("type contract declaring", () => ReadArgumentName(
            Type.GetType("ILDietControl.NamedTypeContract+Nested")!.DeclaringType!));
        Report("type contract nested", () => ReadArgumentName(
            Type.GetType("ILDietControl.NamedTypeContract+Nested")!.GetNestedType("Selected")!));
        Report("type contract token", () => ReadArgumentName(typeof(TokenTypeContract.Nested).DeclaringType!));
        Report("type contract method lookup", () => listedNestedFoo + "/" + typeof(MethodLookupOwner).InvokeMember("Foo",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, new MethodLookupOwner(), null));
        GC.KeepAlive(new UnrelatedTypeContractTarget());
    }

    private static object? ReadSize(ITypeContract target) =>
        target.GetType().InvokeMember("Size", BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
            null, target, null);

    private static object? ReadArgumentName(Type type) =>
        type.InvokeMember("ArgumentName", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
            null, null, null);

    private sealed class AnnotatedFieldCustomer { public int FieldRetentionWitness => 42; }
    private sealed class UnprovedFieldCustomer { public int FieldRetentionWitness => 43; }

    private sealed class AnnotatedFieldView
    {
        [DynamicallyAccessedMembers(MemberTypes.PublicProperties)]
        private readonly Type type;
        private readonly Type ordinary = typeof(AnnotatedFieldCustomer);

        public AnnotatedFieldView([DynamicallyAccessedMembers(MemberTypes.PublicProperties)] Type type) => this.type = type;
        public object? Read(object target) => type.InvokeMember("FieldRetentionWitness", BindingFlags.GetProperty, null, target, null);
        public object? ReadMixed(bool useOrdinary, object target) =>
            (useOrdinary ? ordinary : type).InvokeMember("FieldRetentionWitness", BindingFlags.GetProperty, null, target, null);
    }

    private sealed class OrdinaryFieldView
    {
        private readonly Type type = typeof(AnnotatedFieldCustomer);
        public object? Read(object target) => type.InvokeMember("FieldRetentionWitness", BindingFlags.GetProperty, null, target, null);
    }

    [DynamicallyAccessedMembers(MemberTypes.PublicProperties)]
    private static Type unprovedField = null!;
    private static object? ReadUnprovedField(object target) =>
        unprovedField.InvokeMember("FieldRetentionWitness", BindingFlags.GetProperty, null, target, null);

    private static string AnnotatedAccessors(
        [DynamicallyAccessedMembers(MemberTypes.PublicParameterlessConstructor | MemberTypes.PublicProperties
            | MemberTypes.NonPublicEvents | MemberTypes.Interfaces)] Type type, PropertyInfo property)
    {
        object instance = Activator.CreateInstance(type)!;
        object? initial = property.GetValue(instance);
        property.SetValue(instance, 11);
        const BindingFlags accessor = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod;
        object?[] argument = { null };
        type.InvokeMember("add_Changed", accessor, null, instance, argument);
        type.InvokeMember("remove_Changed", accessor, null, instance, argument);
        return initial + "/" + property.GetValue(instance) + "/" + typeof(IAnnotatedContract).IsAssignableFrom(type);
    }

    private static string AnnotatedInherited(
        [DynamicallyAccessedMembers(MemberTypes.NonPublicConstructorsWithInherited
            | MemberTypes.NonPublicFieldsWithInherited | MemberTypes.PublicNestedTypesWithInherited)] Type type,
        Type nested)
    {
        const BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
        Type parent = type.BaseType!;
        object instance = Activator.CreateInstance(parent, hidden, null, new object[] { 19 }, null)!;
        return parent.InvokeMember("_seed", hidden | BindingFlags.GetField, null, instance, null) + "/"
            + nested.InvokeMember("Read", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod,
                null, null, null);
    }

    private static string AnnotatedMethods([DynamicallyAccessedMembers(MemberTypes.AllMethods)] Type type)
    {
        const BindingFlags method = BindingFlags.Static | BindingFlags.InvokeMethod;
        return type.BaseType!.InvokeMember("Hidden", method | BindingFlags.NonPublic, null, null, null) + "/"
            + type.InvokeMember("Visible", method | BindingFlags.Public, null, null, null);
    }

    private static object? AnnotatedInterface([DynamicallyAccessedMembers(MemberTypes.All)] Type type, Type parent) =>
        parent.IsAssignableFrom(type)
            ? parent.InvokeMember("Identifier", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                null, null, null)
            : null;

    private static object? CreateAnnotatedValue([DynamicallyAccessedMembers(MemberTypes.PublicParameterlessConstructor)] Type type) =>
        Activator.CreateInstance(type);

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

[DynamicallyAccessedMembers(MemberTypes.NonPublicMethods)]
internal sealed class NamedTypeContract
{
    private static string ArgumentName() => "declaring";

    internal sealed class Nested
    {
        [DynamicallyAccessedMembers(MemberTypes.NonPublicMethods)]
        public sealed class Selected
        {
            private static string ArgumentName() => "selected";
        }
    }
}

[DynamicallyAccessedMembers(MemberTypes.NonPublicMethods)]
internal sealed class TokenTypeContract
{
    private static string ArgumentName() => "token";
    internal sealed class Nested { }
}

internal sealed class MethodLookupOwner : Base
{
    [DynamicallyAccessedMembers(MemberTypes.NonPublicMethods)]
    public new sealed class Foo
    {
        private static int NestedOnly() => -1;
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

// The empty name selects the default member; Quadruple and Extent stay unselected.
[DefaultMember("Pick")]
internal sealed class Invoked
{
    public int Total = 6;

    public static int Triple(int value) => value * 3;

    public static int Quadruple(int value) => value * 4;

    public int Size => 8;

    public int Extent => 9;

    public string Pick(int value) => "pick:" + value;
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


internal sealed class ReflectionFields
{
    public FieldPayload? Payload = null;
    public ReflectedPayload? LibraryPayload = null;
}

internal sealed class FieldPayload : Shape
{
    public FieldPayload() { }

    public FieldPayload(int value) { }

    public override string Who() => "field-payload";
}

internal sealed class RuntimeNamed : Shape
{
    public override string Who() => "runtime-named";

    public static string Value() => "runtime-value";
}
