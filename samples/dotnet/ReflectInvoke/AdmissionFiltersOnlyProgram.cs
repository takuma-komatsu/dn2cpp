using System;
using System.Globalization;
using System.Reflection;

// SUBJECT: recognized reflection admits only what the recorded filters can select. A
// metadata read of an attribute whose constructor the program runs directly selects no
// named-argument setter; GetMember with MemberTypes.Method selects no field of the same
// name; an accessor read without nonPublic selects no non-public accessor. Each excluded
// body touches a type whose static constructor prints, so admitting it shows up as
// output .NET never writes. The controls read the same shapes where the filter does
// select the member. Named enumerations select only their named ordinary or template
// method; a finite or partly unknown ignoreCase choice keeps each known alternative.
// Singular template lookups select hidden methods consistently, while named delegate
// binding includes private base methods with the delegate's arity.
// Referenced library bases retain the same targets through ILDiet.
namespace AdmissionFilters;

static class SetterProbe
{
    static SetterProbe() => Console.WriteLine("setter probe initialized");

    public static int Touch(int value) => value;
}

[AttributeUsage(AttributeTargets.Class)]
sealed class TagAttribute : Attribute
{
    private int _value;

    public int P
    {
        get => _value;
        set => _value = SetterProbe.Touch(value);
    }
}

[Tag(P = 1)]
sealed class TaggedOwner
{
}

[AttributeUsage(AttributeTargets.Class)]
sealed class LabelAttribute : Attribute
{
    public int P { get; set; }
}

[Label(P = 5)]
sealed class LabeledOwner
{
}

class RunBase
{
    public static int Run;

    static RunBase() => Console.WriteLine("run base initialized");
}

sealed class RunDerived : RunBase
{
    public static new void Run() => Console.WriteLine("run method");
}

class SeedBase
{
    public static int Seed;

    static SeedBase() => Seed = 42;
}

sealed class SeedDerived : SeedBase
{
    public static void Seeded() => Console.WriteLine("seed method");
}

static class AccessorProbe
{
    static AccessorProbe() => Console.WriteLine("accessor probe initialized");

    public static int Touch(int value) => value;
}

static class Guarded
{
    private static int _value;
    private static int _all;

    public static int P
    {
        get => _value;
        private set => _value = AccessorProbe.Touch(value);
    }

    public static int All
    {
        get => _all;
        private set => _all = AccessorProbe.Touch(value);
    }

    private static event Action? Hidden
    {
        add => AccessorProbe.Touch(1);
        remove { }
    }
}

static class Opened
{
    private static int _value;

    public static int P
    {
        get => _value;
        private set => _value = value * 2;
    }
}

static class EnumerationProbe
{
    static EnumerationProbe() => Console.WriteLine("enumeration probe initialized");

    public static string Touch() => "unselected";
}

sealed class NamedMethods
{
    public static string Safe() => "selected";

    public static string Other() => EnumerationProbe.Touch();
}

sealed class NamedTemplate<T>
{
    public static string Safe() => typeof(T).Name;

    public static string Other() => EnumerationProbe.Touch();
}

class DeclaredMethodBase
{
    public string Safe(string first, string second) => EnumerationProbe.Touch();
}

sealed class DeclaredMethodOwner : DeclaredMethodBase
{
    public static new string Safe() => EnumerationProbe.Touch();
    public static string Safe(int value) => EnumerationProbe.Touch();
    public static string SafeExtra() => EnumerationProbe.Touch();
}

sealed class DeclaredMethodTemplate<T> : DeclaredMethodBase
{
    public static new string Safe() => EnumerationProbe.Touch();
    public static string Safe(T value) => EnumerationProbe.Touch();
    public static string SafeExtra() => EnumerationProbe.Touch();
}

sealed class CaseBuilt { }
sealed class CASEBUILT { }
sealed class CaseTyped { }
sealed class CASETYPED { }
sealed class MixedBuilt { }
sealed class MixedTyped { }
sealed class OpaqueBuilt { }
sealed class OpaqueTyped { }

#nullable disable

[AttributeUsage(AttributeTargets.All)]
sealed class SelectedRowAttribute : Attribute
{
    public SelectedRowAttribute() { }
    public int Marker { get; set; }
}

[AttributeUsage(AttributeTargets.All)]
sealed class ExcludedRowAttribute : Attribute
{
    static ExcludedRowAttribute() => Console.WriteLine("excluded attribute initialized");
    public ExcludedRowAttribute() { }
}

class AttributeBase<T>
{
    [ExcludedRow]
    public int HiddenField;

    [ExcludedRow]
    public virtual void Hidden() { }

    [ExcludedRow]
    public int HiddenProperty => 0;

    [ExcludedRow]
    public event Action HiddenEvent { add { } remove { } }
}

sealed class AttributeTemplate<T> : AttributeBase<T>
{
    [SelectedRow]
    public new int HiddenField;

    [SelectedRow(Marker = 7)]
    public static void Run() { }
    [ExcludedRow]
    private static void Run(int value) { }

    [SelectedRow]
    public static void Bound(string value) { }
    [ExcludedRow]
    public static void Bound(object value) { }

    [SelectedRow]
    public static void Signature(SignatureBox<int> value) { }
    [ExcludedRow]
    public static void Signature(object value) { }
    [SelectedRow]
    public static void SignatureArray(SignatureValue<int>[] value) { }
    [ExcludedRow]
    public static void SignatureArray(object[] value) { }

    [SelectedRow]
    public static void Generic<U>() { }
    [ExcludedRow]
    public static void Generic() { }

    [SelectedRow]
    public override void Hidden() { }
    [SelectedRow]
    public new int HiddenProperty => 1;
    [SelectedRow]
    public new event Action HiddenEvent { add { } remove { } }

    [SelectedRow]
    public int Property { [ExcludedRow] get => 1; }
    public int Accessor { [SelectedRow] get => 1; [ExcludedRow] private set { } }
    [SelectedRow]
    public event Action Event { [ExcludedRow] add { } [ExcludedRow] remove { } }
    public event Action AccessorEvent { [SelectedRow] add { } [ExcludedRow] remove { } }

    [SelectedRow]
    public int Field;
    [ExcludedRow]
    private int OtherField;

    [SelectedRow]
    public AttributeTemplate() { }
    [ExcludedRow]
    private AttributeTemplate(int value) { }

    public static int Parameter([TemplateParameterRow] int value) => value;
}

[AttributeUsage(AttributeTargets.All)]
sealed class OpenRowAttribute : Attribute
{
    public OpenRowAttribute() { }
}

[AttributeUsage(AttributeTargets.All)]
sealed class OpenParameterRowAttribute : Attribute
{
    public OpenParameterRowAttribute() { }
}

[AttributeUsage(AttributeTargets.All)]
sealed class OpenPropertyRowAttribute : Attribute
{
    public OpenPropertyRowAttribute() { }
}

[AttributeUsage(AttributeTargets.All)]
sealed class OpenGetterRowAttribute : Attribute
{
    public OpenGetterRowAttribute() { }
}

[AttributeUsage(AttributeTargets.All)]
sealed class OpenEventRowAttribute : Attribute
{
    public OpenEventRowAttribute() { }
}

[AttributeUsage(AttributeTargets.All)]
sealed class OpenAdderRowAttribute : Attribute
{
    public OpenAdderRowAttribute() { }
}

[AttributeUsage(AttributeTargets.All)]
sealed class OpenFieldRowAttribute : Attribute
{
    public OpenFieldRowAttribute() { }
}

[AttributeUsage(AttributeTargets.All)]
sealed class OpenEnumerationRowAttribute : Attribute
{
    public OpenEnumerationRowAttribute() { }
}

[AttributeUsage(AttributeTargets.All)]
sealed class NestedRowAttribute : Attribute
{
    public NestedRowAttribute() { }
}

[AttributeUsage(AttributeTargets.All)]
sealed class TemplateParameterRowAttribute : Attribute
{
    public TemplateParameterRowAttribute() { }
}

[AttributeUsage(AttributeTargets.Class)]
sealed class FieldProjectionAttribute : Attribute
{
    public FieldProjectionAttribute() { }
}
[FieldProjection]
sealed class FieldProjection { }

[AttributeUsage(AttributeTargets.Class)]
sealed class PropertyProjectionAttribute : Attribute
{
    public PropertyProjectionAttribute() { }
}
[PropertyProjection]
sealed class PropertyProjection { }

[AttributeUsage(AttributeTargets.Class)]
sealed class ReturnProjectionAttribute : Attribute
{
    public ReturnProjectionAttribute() { }
}
[ReturnProjection]
sealed class ReturnProjection { }

[AttributeUsage(AttributeTargets.Class)]
sealed class ParameterProjectionAttribute : Attribute
{
    public ParameterProjectionAttribute() { }
}
[ParameterProjection]
sealed class ParameterProjection { }

sealed class OpenAttributeRows<T>
{
    [OpenRow]
    public static void Method([OpenParameterRow] ParameterProjection value) { }
    public static ReturnProjection Return() => null!;
    [OpenEnumerationRow]
    public static void Enumerated() { }
    [OpenPropertyRow]
    public PropertyProjection Property { [OpenGetterRow] get => null!; }
    [OpenEventRow]
    public event Action Event { [OpenAdderRow] add { } remove { } }
    [OpenFieldRow]
    public FieldProjection Field;
}

[AttributeUsage(AttributeTargets.Method)]
sealed class InterfaceRowAttribute : Attribute
{
    public InterfaceRowAttribute() { }
}
interface IOpenAttributeRows<T>
{
    [InterfaceRow]
    void Method();
}

class NestedAttributeBase<T> { }
sealed class NestedAttributeRows<T> : NestedAttributeBase<NestedAttributeRows<T>>
{
    [NestedRow]
    public static void Method() { }
    [ExcludedRow]
    public static void Method(NestedAttributeRows<NestedAttributeRows<T>> value) { }
}

class SubstitutedAttributeBase<T>
{
    [ExcludedRow]
    public virtual void Overridden(T value) { }
}
sealed class SubstitutedAttributeRows<T> : SubstitutedAttributeBase<int>
{
    [SelectedRow]
    public override void Overridden(int value) { }
}

[AttributeUsage(AttributeTargets.Class)]
sealed class TemplateFieldTypeAttribute : Attribute
{
    public TemplateFieldTypeAttribute() { }
}
[TemplateFieldType]
sealed class TemplateFieldType { }

[AttributeUsage(AttributeTargets.Class)]
sealed class TemplatePropertyTypeAttribute : Attribute
{
    public TemplatePropertyTypeAttribute() { }
}
[TemplatePropertyType]
sealed class TemplatePropertyType { }

[AttributeUsage(AttributeTargets.Class)]
sealed class TemplateReturnTypeAttribute : Attribute
{
    public TemplateReturnTypeAttribute() { }
}
[TemplateReturnType]
sealed class TemplateReturnType { }

[AttributeUsage(AttributeTargets.Class)]
sealed class TemplateGetterTypeAttribute : Attribute
{
    public TemplateGetterTypeAttribute() { }
}
[TemplateGetterType]
sealed class TemplateGetterType { }

[AttributeUsage(AttributeTargets.Class)]
sealed class TemplateParameterTypeAttribute : Attribute
{
    public TemplateParameterTypeAttribute() { }
}
[TemplateParameterType]
sealed class TemplateParameterType { }

[AttributeUsage(AttributeTargets.Class)]
sealed class ConstructedReturnTypeAttribute : Attribute
{
    public ConstructedReturnTypeAttribute() { }
}
[ConstructedReturnType]
sealed class ConstructedReturnType { }

[AttributeUsage(AttributeTargets.Class)]
sealed class ConstructedParameterTypeAttribute : Attribute
{
    public ConstructedParameterTypeAttribute() { }
}
[ConstructedParameterType]
sealed class ConstructedParameterType { }

sealed class ProjectedTemplate<T>
{
    public TemplateFieldType Field;
    public TemplatePropertyType Property => null!;
    public TemplateGetterType Accessor => null!;
    public TemplateReturnType Return() => null!;
    public void Parameter(TemplateParameterType value) { }
}

sealed class ProjectedDefinition<T>
{
    public ProjectedArrayElement<T>[] NestedArray() => null!;
}

[ExcludedRow]
sealed class ProjectedArrayElement<T> { }

[AttributeUsage(AttributeTargets.Method)]
sealed class ConstructedMethodRowAttribute : Attribute
{
    public ConstructedMethodRowAttribute() { }
    public int Marker { get; set; }
}
[AttributeUsage(AttributeTargets.Parameter)]
sealed class ConstructedParameterRowAttribute : Attribute
{
    public ConstructedParameterRowAttribute() { }
}
[AttributeUsage(AttributeTargets.Method)]
sealed class UnknownConstructedRowAttribute : Attribute
{
    public UnknownConstructedRowAttribute() { }
}
sealed class ConstructedOpenMethods<T>
{
    [ConstructedMethodRow(Marker = 31)]
    public U Map<U, V>([ConstructedParameterRow] V value) => default!;
    [UnknownConstructedRow]
    public U Unknown<U>() => default!;
    [ExcludedRow]
    public void Unselected<U>() { }
}

sealed class SignatureBox<T> { }
struct SignatureValue<T> { }

#nullable restore

static class DelegateArityProbe
{
    static DelegateArityProbe() => Console.WriteLine("unselected delegate arity initialized");
    public static string Touch() => "unselected";
}

static class PropertyReturnProbe
{
    static PropertyReturnProbe() => Console.WriteLine("unselected property return initialized");
    public static string Touch() => "unselected";
}

static class StaticPropertyReturnProbe
{
    static StaticPropertyReturnProbe() => Console.WriteLine("unselected static property return initialized");
    public static string Touch() => "unselected";
}

sealed class DelegateArityTemplate<T>
{
    public static string Pick() => typeof(T).Name;
    public static string Pick(int value) => DelegateArityProbe.Touch();
}

sealed class DelegateArityEligibility<T>
{
    public static string Pick() => typeof(T).Name;
    public static string Pick(int value) => SelectionDependency<T>.Text();
}

sealed class DelegateArityStatic<T>
{
    public static string Pick() => SelectionDependency<T>.Text();
    public static string Pick(int value) => DelegateArityProbe.Touch();
}

static class SelectionDependency<T>
{
    public static string Text() => "static:" + typeof(T).Name;
    public static int Number() => 30;
}

sealed class PropertyReturnTemplate<T>
{
    public int this[int value] => value + 10;
    public string this[string value] => PropertyReturnProbe.Touch();
}

sealed class PropertyReturnEligibility<T>
{
    public int this[int value] => value + 20;
    public string this[string value] => SelectionDependency<T>.Text();
}

sealed class PropertyReturnStatic<T>
{
    public int this[int value] => SelectionDependency<T>.Number() + value;
    public string this[string value] => StaticPropertyReturnProbe.Touch();
}

struct HiddenFieldBox
{
    static HiddenFieldBox() => Console.WriteLine("hidden field box initialized");
    public override string ToString() => "hidden";
}

struct InheritedFieldBox
{
    static InheritedFieldBox() => Console.WriteLine("inherited field box initialized");
    public override string ToString() => "inherited";
}

struct PrivateFieldBox
{
    static PrivateFieldBox() => Console.WriteLine("private field box initialized");
    public override string ToString() => "private";
}

struct SelectedFieldBox
{
    public override string ToString() => "selected";
}

struct GenericFieldBox<T>
{
    public override string ToString() => "generic:" + typeof(T).Name;
}

class FieldSelectionBase<T>
{
    public HiddenFieldBox Hidden;
    public InheritedFieldBox Inherited;
}

sealed class FieldSelectionTemplate<T> : FieldSelectionBase<T>
{
    public new int Hidden;
    private PrivateFieldBox Private;
    public SelectedFieldBox Selected;
    public GenericFieldBox<int> Generic;
}

sealed class AssemblyChoiceTarget
{
    public static string Run() => "application";
}

sealed class AssemblyConstructedTarget
{
    public override string ToString() => "application constructed";
}

sealed class MixedAssemblyTarget
{
    public static string Run() => "known assembly";
}

sealed class NullArrayType<T>
{
    static NullArrayType() { }
    public static string Name() => typeof(T).Name;
    public static string Other() => "unselected";
}

sealed class OpaqueArrayType<T>
{
    static OpaqueArrayType() { }
    public static string Name() => typeof(T).Name;
    public static string Other() => "unselected";
}

sealed class OpaqueElementType<T>
{
    static OpaqueElementType() { }
    public static string Name() => typeof(T).Name;
    public static string Other() => "unselected";
}

sealed class NullElementType<T>
{
    static NullElementType() { }
    public static string Name() => typeof(T).Name;
    public static string Other() => "unselected";
}

sealed class InvalidElementType<T>
{
    static InvalidElementType() { }
    public static string Name() => typeof(T).Name;
    public static string Other() => "unselected";
}

static class AlternativeMethods
{
    public static string NullArray<T>() => typeof(T).Name;
    public static string OpaqueArray<T>() => typeof(T).Name;
    public static string OpaqueElement<T>() => typeof(T).Name;
    public static string NullElement<T>() => typeof(T).Name;
    public static string Other<T>() => "unselected";
}

interface IOpenLookupFixed
{
    static string Run() => "open lookup";
    static string Other() => "unselected";
}
interface IOpenEnumerationFixed
{
    static string Run() => "open enumeration";
    static string Other() => "unselected";
}
interface IOpenInheritedFixed
{
    static string Run() => "open inherited";
    static string Other() => "unselected";
}
interface IOpenGenericFixed<T>
{
    static string Run() => "open fixed generic:" + typeof(T).Name;
    static string Other() => "unselected";
}
interface IOpenDependent<T> { static string Run() => "unselected dependent"; }
sealed class OpenLookupOwner<T> : IOpenLookupFixed { }
sealed class OpenEnumerationOwner<T> : IOpenEnumerationFixed { }
class OpenInterfaceBase<T> : IOpenInheritedFixed { }
sealed class OpenInheritedOwner<T> : OpenInterfaceBase<T> { }
sealed class OpenGenericFixedOwner<T> : IOpenGenericFixed<int> { }
sealed class OpenDependentOwner<T> : IOpenDependent<T> { }

interface ICaseExact
{
    static string Run() => "exact case";
    static string Other() => "unselected";
}
interface ICASEEXACT
{
    static string Run() => "unselected collision";
}
sealed class CaseInterfaceOwner : ICaseExact, ICASEEXACT { }
interface IAmbiguousName { static string Run() => "unselected ambiguous upper"; }
interface iambiguousname { static string Run() => "unselected ambiguous lower"; }
sealed class AmbiguousInterfaceOwner : IAmbiguousName, iambiguousname { }
interface IFirstChoice { static string Run() => "first name"; }
interface ISecondChoice { static string Run() => "second name"; }
sealed class NamedInterfaceOwner : IFirstChoice, ISecondChoice { }
interface IUnicodeChoice { static string Run() => "unicode known"; }
interface IUnicodeRemainder { static string Run() => "unicode unknown"; }
sealed class UnicodeInterfaceOwner : IUnicodeChoice, IUnicodeRemainder { }

static class HiddenLookupProbe
{
    static HiddenLookupProbe() => Console.WriteLine("hidden lookup initialized");
    public static string Touch() => "hidden lookup";
}

class MostDerivedTemplateBase<T> { public static string Make() => HiddenLookupProbe.Touch(); }
sealed class MostDerivedTemplate<T> : MostDerivedTemplateBase<T>
{
    public new static string Make() => typeof(T).Name;
}
class MostDerivedOpaqueBase<T> { public static string Make() => HiddenLookupProbe.Touch(); }
sealed class MostDerivedOpaque<T> : MostDerivedOpaqueBase<T>
{
    public new static string Make() => typeof(T).Name;
}
class MostDerivedStaticBase<T> { public static string Make() => HiddenLookupProbe.Touch(); }
sealed class MostDerivedStatic<T> : MostDerivedStaticBase<T>
{
    public new static string Make() => SelectionDependency<T>.Text();
}
class MostDerivedGenericBase<T> { public static string Make<U>() => HiddenLookupProbe.Touch(); }
sealed class MostDerivedGeneric<T> : MostDerivedGenericBase<T>
{
    public new static string Make<U>() => typeof(T).Name + ":" + typeof(U).Name;
}
class AmbiguousTemplateBase<T> { public static string Make(int value) => "base"; }
sealed class AmbiguousTemplate<T> : AmbiguousTemplateBase<T>
{
    public static string Make(string value) => "derived";
}

static class NamedBindingProbe
{
    static NamedBindingProbe() => Console.WriteLine("wrong binding arity initialized");
    public static string Touch() => "wrong binding arity";
}

class PrivateBindingTemplateBase<T>
{
    private static string Pick() => "private:" + typeof(T).Name;
    private static string Pick(int value) => NamedBindingProbe.Touch();
}
sealed class PrivateBindingTemplate<T> : PrivateBindingTemplateBase<T> { }
class PrivateBindingOpaqueBase<T>
{
    private static string Pick() => "opaque:" + typeof(T).Name;
    private static string Pick(int value) => NamedBindingProbe.Touch();
}
sealed class PrivateBindingOpaque<T> : PrivateBindingOpaqueBase<T> { }
class PrivateBindingStaticBase<T>
{
    private static string Pick() => SelectionDependency<T>.Text();
    private static string Pick(int value) => NamedBindingProbe.Touch();
}
sealed class PrivateBindingStatic<T> : PrivateBindingStaticBase<T> { }

static class Program
{
    private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

    static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        bool beforeAttributeSelection = args.Length > 0 && args[0] == "before-attribute-selection";
        bool beforeMetadataProjections = args.Length > 0 && args[0] == "before-metadata-projections";
        bool beforeSelectionFilters = args.Length > 0 && args[0] == "before-selection-filters";
        bool beforeAlternatives = args.Length > 0 && args[0] == "before-reflection-alternatives";
        bool beforeTemplateLookups = args.Length > 0 && args[0] == "before-template-lookups";
        bool beforeLibraryBindings = args.Length > 0 && args[0] == "before-library-bindings";
        bool beforeDeclaredMethods = args.Length > 0 && args[0] == "before-typeinfo-declared-methods";
        if (beforeAttributeSelection || beforeMetadataProjections || beforeSelectionFilters || beforeAlternatives
            || beforeTemplateLookups || beforeLibraryBindings || beforeDeclaredMethods)
            args = Array.Empty<string>();

        Console.WriteLine("== attribute metadata over a directly constructed attribute ==");
        var made = new TagAttribute();
        Console.WriteLine("made: " + made.P);
        foreach (CustomAttributeData data in typeof(TaggedOwner).GetCustomAttributesData())
            Console.WriteLine("data: " + data.AttributeType.Name);
        foreach (object label in typeof(LabeledOwner).GetCustomAttributes(false))
            Console.WriteLine("instance: " + ((LabelAttribute)label).P);
        Console.WriteLine("attribute metadata end");

        Console.WriteLine("== GetMember with a member-kind filter ==");
        MemberInfo[] runs = typeof(RunDerived).GetMember("Run", MemberTypes.Method, PublicStatic);
        Console.WriteLine("run members: " + runs.Length);
        foreach (MemberInfo run in runs)
            ((MethodInfo)run).Invoke(null, null);
        MemberInfo[] seeds = typeof(SeedDerived).GetMember("Seed", MemberTypes.Field, PublicStatic);
        Console.WriteLine("seed members: " + seeds.Length);
        foreach (MemberInfo seed in seeds)
            Console.WriteLine("seed: " + ((FieldInfo)seed).GetValue(null));
        Console.WriteLine("member-kind filter end");

        Console.WriteLine("== accessors without nonPublic ==");
        MethodInfo? setter = typeof(Guarded).GetProperty("P")!.GetSetMethod();
        Console.WriteLine("public setter: " + (setter is null ? "null" : setter.Name));
        if (setter != null)
            setter.Invoke(null, new object[] { 1 });
        MethodInfo[] accessors = typeof(Guarded).GetProperty("All")!.GetAccessors(false);
        Console.WriteLine("public accessors: " + accessors.Length);
        foreach (MethodInfo accessor in accessors)
            if (accessor.GetParameters().Length == 1)
                accessor.Invoke(null, new object[] { 1 });
        MethodInfo? adder = typeof(Guarded).GetEvent("Hidden", BindingFlags.NonPublic | BindingFlags.Static)!.GetAddMethod();
        Console.WriteLine("public adder: " + (adder is null ? "null" : adder.Name));
        if (adder != null)
            adder.Invoke(null, new object?[] { null });
        MethodInfo? opened = typeof(Opened).GetProperty("P")!.GetSetMethod(true);
        opened!.Invoke(null, new object[] { 21 });
        Console.WriteLine("non-public setter: " + Opened.P);
        Console.WriteLine("accessors end");

        Console.WriteLine("== named enumeration admission ==");
        // This branch admits the call's selected body even when the runtime does not
        // take it; an unrelated admitted body must not initialize its dependencies.
        if (args.Length > 0)
        {
            foreach (MethodInfo method in typeof(NamedMethods).GetTypeInfo().GetDeclaredMethods("Safe"))
                Console.WriteLine(method.Invoke(null, null));
            foreach (MethodInfo method in typeof(NamedTemplate<>).MakeGenericType(typeof(int))
                .GetTypeInfo().GetDeclaredMethods("Safe"))
                Console.WriteLine(method.Invoke(null, null));
            OpaqueChoices(bool.Parse(args[0]));
        }
        Console.WriteLine("named enumeration admission end");

        Console.WriteLine("== type-name case alternatives ==");
        Console.WriteLine("construct choice: " + ConstructChoice(args));
        Console.WriteLine("type choice: " + TypeChoice(args));
        Console.WriteLine("construct mixed: " + ConstructMixed(args));
        Console.WriteLine("type mixed: " + TypeMixed(args));
        Console.WriteLine("type-name case alternatives end");
        if (beforeAttributeSelection)
            return;

        Console.WriteLine("== selected generic member attributes ==");
        Type template = typeof(AttributeTemplate<>).MakeGenericType(typeof(string));
        const BindingFlags publicInstance = BindingFlags.Public | BindingFlags.Instance;
        Console.WriteLine("template method=" + template.GetMethod("Run", PublicStatic)!.GetCustomAttributes(false).Length);
        Console.WriteLine("template typed=" + template.GetMethod("Bound", PublicStatic, null, new[] { typeof(string) }, null)!.GetCustomAttributes(false).Length);
        Console.WriteLine("template signature=" + template.GetMethod("Signature", new[] { typeof(SignatureBox<int>) })!.GetCustomAttributes(false).Length);
        Console.WriteLine("template struct array=" + template.GetMethod("SignatureArray", new[] { typeof(SignatureValue<int>[]) })!.GetCustomAttributes(false).Length);
        Console.WriteLine("template arity=" + template.GetMethod("Generic", 1, PublicStatic, null, Type.EmptyTypes, null)!.GetCustomAttributes(false).Length);
        Console.WriteLine("template substituted override=" + typeof(SubstitutedAttributeRows<>).MakeGenericType(typeof(string))
            .GetMethod("Overridden", publicInstance)!.GetCustomAttributes(false).Length);
        Console.WriteLine("template override=" + template.GetMethod("Hidden", publicInstance)!.GetCustomAttributes(false).Length);
        Console.WriteLine("template hidden property=" + template.GetProperty("HiddenProperty", publicInstance)!.GetCustomAttributes(false).Length);
        Console.WriteLine("template hidden event=" + template.GetEvent("HiddenEvent", publicInstance)!.GetCustomAttributes(false).Length);
        Console.WriteLine("template property=" + template.GetProperty("Property")!.GetCustomAttributes(false).Length);
        Console.WriteLine("template getter=" + template.GetProperty("Accessor")!.GetGetMethod()!.GetCustomAttributes(false).Length);
        Console.WriteLine("template event=" + template.GetEvent("Event")!.GetCustomAttributes(false).Length);
        Console.WriteLine("template adder=" + template.GetEvent("AccessorEvent")!.GetAddMethod()!.GetCustomAttributes(false).Length);
        Console.WriteLine("template hidden field=" + template.GetField("HiddenField")!.GetCustomAttributes(false).Length);
        Console.WriteLine("template field enumeration=" + template.GetFields(publicInstance).Length);
        Console.WriteLine("template field members=" + template.GetMember("HiddenField", MemberTypes.Field, publicInstance).Length);
        Console.WriteLine("template field=" + template.GetField("Field")!.GetCustomAttributes(false).Length);
        Console.WriteLine("template constructor=" + template.GetConstructor(Type.EmptyTypes)!.GetCustomAttributes(false).Length);
        Console.WriteLine("template parameter=" + template.GetMethod("Parameter")!.GetParameters()[0].GetCustomAttributes(false).Length);

        _ = new OpenAttributeRows<int>();
        const BindingFlags declared = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
        Console.WriteLine("definition method=" + typeof(OpenAttributeRows<>).GetMethod("Method", declared)!.GetCustomAttributes(false).Length);
        Console.WriteLine("definition parameter=" + typeof(OpenAttributeRows<>).GetMethod("Method", declared)!.GetParameters()[0].GetCustomAttributes(false).Length);
        Console.WriteLine("definition property=" + typeof(OpenAttributeRows<>).GetProperty("Property")!.GetCustomAttributes(false).Length);
        Console.WriteLine("definition getter=" + typeof(OpenAttributeRows<>).GetProperty("Property")!.GetGetMethod()!.GetCustomAttributes(false).Length);
        Console.WriteLine("definition event=" + typeof(OpenAttributeRows<>).GetEvent("Event")!.GetCustomAttributes(false).Length);
        Console.WriteLine("definition adder=" + typeof(OpenAttributeRows<>).GetEvent("Event")!.GetAddMethod()!.GetCustomAttributes(false).Length);
        Console.WriteLine("definition field=" + typeof(OpenAttributeRows<>).GetField("Field")!.GetCustomAttributes(false).Length);
        Console.WriteLine("definition field type=" + typeof(OpenAttributeRows<>).GetField("Field")!.FieldType.GetCustomAttributes(false).Length);
        Console.WriteLine("definition property type=" + typeof(OpenAttributeRows<>).GetProperty("Property")!.PropertyType.GetCustomAttributes(false).Length);
        Console.WriteLine("definition return type=" + typeof(OpenAttributeRows<>).GetMethod("Return", declared)!.ReturnType.GetCustomAttributes(false).Length);
        Console.WriteLine("definition parameter type=" + typeof(OpenAttributeRows<>).GetMethod("Method", declared)!.GetParameters()[0].ParameterType.GetCustomAttributes(false).Length);
        int enumerated = 0;
        foreach (MethodInfo row in typeof(OpenAttributeRows<>).GetMethods(declared))
            enumerated += row.GetCustomAttributes(false).Length;
        Console.WriteLine("definition enumeration=" + enumerated);
        Console.WriteLine("interface definition=" + typeof(IOpenAttributeRows<>).GetMethod("Method")!.GetCustomAttributes(false).Length);
        Console.WriteLine("nested definition=" + typeof(NestedAttributeRows<>).GetMethod("Method", declared, null, Type.EmptyTypes, null)!.GetCustomAttributes(false).Length);
        Console.WriteLine("selected generic member attributes end");
        if (beforeMetadataProjections)
            return;

        Console.WriteLine("== template and constructed method projections ==");
        Type projected = typeof(ProjectedTemplate<>).MakeGenericType(typeof(int));
        Console.WriteLine("template field type=" + projected.GetField("Field")!.FieldType.GetCustomAttributes(false).Length);
        Console.WriteLine("template property type=" + projected.GetProperty("Property")!.PropertyType.GetCustomAttributes(false).Length);
        Console.WriteLine("template return type=" + projected.GetMethod("Return")!.ReturnType.GetCustomAttributes(false).Length);
        Console.WriteLine("template getter type=" + projected.GetProperty("Accessor")!.GetGetMethod()!.ReturnType.GetCustomAttributes(false).Length);
        Console.WriteLine("template parameter type=" + projected.GetMethod("Parameter")!.GetParameters()[0].ParameterType.GetCustomAttributes(false).Length);
        MethodInfo constructed = typeof(ConstructedOpenMethods<>).GetMethod("Map")!
            .MakeGenericMethod(typeof(ConstructedReturnType), typeof(ConstructedParameterType));
        Console.WriteLine("constructed method marker=" + ((ConstructedMethodRowAttribute)constructed.GetCustomAttributes(false)[0]).Marker);
        Console.WriteLine("constructed method open=" + constructed.ContainsGenericParameters + ":" + constructed.IsGenericMethodDefinition);
        Console.WriteLine("constructed parameter row=" + constructed.GetParameters()[0].GetCustomAttributes(false).Length);
        Console.WriteLine("constructed return type=" + constructed.ReturnType.GetCustomAttributes(false).Length);
        Console.WriteLine("constructed parameter type=" + constructed.GetParameters()[0].ParameterType.GetCustomAttributes(false).Length);
        Console.WriteLine("unknown constructed row=" + UnknownConstructedRow(typeof(int)));
        if (args.Length > 0)
            InspectDetachedProjection();
        Console.WriteLine("template and constructed method projections end");
        if (beforeSelectionFilters)
            return;

        Console.WriteLine("== template selection filters ==");
        try
        {
            NamedDelegateSelections();
        }
        catch (Exception e)
        {
            Console.WriteLine("named delegates fault=" + e.GetType().Name);
        }
        try
        {
            PropertyReturnSelections();
        }
        catch (Exception e)
        {
            Console.WriteLine("property returns fault=" + e.GetType().Name);
        }
        try
        {
            TemplateFieldSelections();
        }
        catch (Exception e)
        {
            Console.WriteLine("template fields fault=" + e.GetType().Name);
        }
        try
        {
            Console.WriteLine("assembly application=" + AssemblyChoice(false, false));
            Console.WriteLine("assembly library=" + AssemblyChoice(true, true));
            Console.WriteLine("construct application=" + AssemblyConstructionChoice(false, false));
            Console.WriteLine("construct library=" + AssemblyConstructionChoice(true, true));
        }
        catch (Exception e)
        {
            Console.WriteLine("assembly choices fault=" + e.GetType().Name);
        }
        try
        {
            Console.WriteLine("assembly mixed=" + MixedAssemblyChoice(typeof(SelectionAnchor).Assembly, false));
        }
        catch (Exception e)
        {
            Console.WriteLine("assembly mixed fault=" + e.GetType().Name);
        }
        ArrayAssemblySelections();
        Console.WriteLine("template selection filters end");
        if (beforeAlternatives)
            return;

        Console.WriteLine("== reflection operand alternatives ==");
        string[] empty = Array.Empty<string>();
        string[] other = new[] { "other" };
        Console.WriteLine("type null array=" + TypeNullArray(empty));
        Console.WriteLine("type null array fault=" + TypeNullArray(other));
        Console.WriteLine("type opaque array=" + TypeOpaqueArray(empty));
        Console.WriteLine("type opaque element=" + TypeOpaqueElement(empty));
        Console.WriteLine("type null element=" + TypeNullElement(empty));
        Console.WriteLine("type null element fault=" + TypeNullElement(other));
        Console.WriteLine("type invalid element=" + TypeInvalidElement(empty));
        Console.WriteLine("method null array=" + MethodNullArray(empty));
        Console.WriteLine("method null array fault=" + MethodNullArray(other));
        Console.WriteLine("method opaque array=" + MethodOpaqueArray(empty));
        Console.WriteLine("method opaque element=" + MethodOpaqueElement(empty));
        Console.WriteLine("method null element=" + MethodNullElement(empty));
        Console.WriteLine("method null element fault=" + MethodNullElement(other));
        OpenDefinitionInterfaces();
        if (args.Length > 0)
            InvokeDependentInterface();
        Console.WriteLine("interface case choice=" + InterfaceCaseChoice(empty));
        Console.WriteLine("interface case choice fault=" + InterfaceCaseChoice(other));
        Console.WriteLine("interface definite ambiguity=" + InterfaceAmbiguity());
        Console.WriteLine("interface first choice=" + InterfaceNameChoice(empty));
        Console.WriteLine("interface second choice=" + InterfaceNameChoice(other));
        Console.WriteLine("interface unicode=" + InterfaceUnicode(empty));
        Console.WriteLine("reflection operand alternatives end");
        if (beforeTemplateLookups)
            return;

        Console.WriteLine("== template lookup policies ==");
        MostDerivedSelections();
        PrivateNamedBindings();
        Console.WriteLine("template lookup policies end");
        if (beforeLibraryBindings)
            return;

        Console.WriteLine("== library private named bindings ==");
        LibraryPrivateNamedBindings();
        Console.WriteLine("library private named bindings end");
        if (beforeDeclaredMethods)
            return;

        Console.WriteLine("== TypeInfo declared methods ==");
        int ordinary = 0;
        foreach (MethodInfo method in typeof(DeclaredMethodOwner).GetTypeInfo().GetDeclaredMethods("Safe"))
        {
            Console.WriteLine("ordinary " + method.Name + "/" + method.GetParameters().Length);
            ordinary++;
        }
        Console.WriteLine("ordinary count=" + ordinary);
        int ordinaryNull = 0;
        foreach (MethodInfo method in typeof(DeclaredMethodOwner).GetTypeInfo().GetDeclaredMethods(null!))
            ordinaryNull++;
        Console.WriteLine("ordinary null=" + ordinaryNull);
        int ordinaryWildcard = 0;
        foreach (MethodInfo method in typeof(DeclaredMethodOwner).GetTypeInfo().GetDeclaredMethods("Safe*"))
            ordinaryWildcard++;
        Console.WriteLine("ordinary wildcard=" + ordinaryWildcard);
        int declaredTemplateCount = 0;
        foreach (MethodInfo method in typeof(DeclaredMethodTemplate<>).MakeGenericType(typeof(int))
            .GetTypeInfo().GetDeclaredMethods("Safe"))
        {
            Console.WriteLine("template " + method.Name + "/" + method.GetParameters().Length);
            declaredTemplateCount++;
        }
        Console.WriteLine("template count=" + declaredTemplateCount);
        int templateNull = 0;
        foreach (MethodInfo method in typeof(DeclaredMethodTemplate<>).MakeGenericType(typeof(int))
            .GetTypeInfo().GetDeclaredMethods(null!))
            templateNull++;
        Console.WriteLine("template null=" + templateNull);
        int templateWildcard = 0;
        foreach (MethodInfo method in typeof(DeclaredMethodTemplate<>).MakeGenericType(typeof(int))
            .GetTypeInfo().GetDeclaredMethods("Safe*"))
            templateWildcard++;
        Console.WriteLine("template wildcard=" + templateWildcard);
        int objectOne = 0;
        int objectTwo = 0;
        foreach (MethodInfo method in typeof(object).GetTypeInfo().GetDeclaredMethods("Equals"))
        {
            if (method.GetParameters().Length == 1)
                objectOne++;
            else if (method.GetParameters().Length == 2)
                objectTwo++;
        }
        Console.WriteLine("object Equals=" + objectOne + "/" + objectTwo);
        Console.WriteLine("TypeInfo declared methods end");
    }

    private static void LibraryPrivateNamedBindings()
    {
        try
        {
            var call = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), typeof(LibraryPrivateBinding), "Pick");
            Console.WriteLine("library private=" + call());
        }
        catch (Exception e) { Console.WriteLine("library private fault=" + e.GetType().Name); }
        try
        {
            var call = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>),
                typeof(LibraryPrivateTemplate<>).MakeGenericType(typeof(int)), "Pick");
            Console.WriteLine("library template=" + call());
        }
        catch (Exception e) { Console.WriteLine("library template fault=" + e.GetType().Name); }
        try
        {
            var call = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>),
                typeof(LibraryPrivateOpaque<>).MakeGenericType(OpaqueArgument()), "pick", true);
            Console.WriteLine("library opaque=" + call());
        }
        catch (Exception e) { Console.WriteLine("library opaque fault=" + e.GetType().Name); }
        try
        {
            var call = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>),
                typeof(LibraryPrivateStatic<>).MakeGenericType(typeof(int)), "Pick");
            Console.WriteLine("library static=" + call());
        }
        catch (Exception e) { Console.WriteLine("library static fault=" + e.GetType().Name); }
    }

    private static void MostDerivedSelections()
    {
        try
        {
            Console.WriteLine("hidden template=" + typeof(MostDerivedTemplate<>).MakeGenericType(typeof(int))
                .GetMethod("Make", PublicStatic)!.Invoke(null, null));
        }
        catch (Exception e) { Console.WriteLine("hidden template fault=" + e.GetType().Name); }
        try
        {
            Console.WriteLine("hidden opaque=" + typeof(MostDerivedOpaque<>).MakeGenericType(OpaqueArgument())
                .GetMethod("Make", PublicStatic)!.Invoke(null, null));
        }
        catch (Exception e) { Console.WriteLine("hidden opaque fault=" + e.GetType().Name); }
        try
        {
            Console.WriteLine("hidden static=" + typeof(MostDerivedStatic<>).MakeGenericType(typeof(int))
                .GetMethod("Make", PublicStatic)!.Invoke(null, null));
        }
        catch (Exception e) { Console.WriteLine("hidden static fault=" + e.GetType().Name); }
        try
        {
            Console.WriteLine("hidden generic=" + typeof(MostDerivedGeneric<>).MakeGenericType(typeof(int))
                .GetMethod("Make", PublicStatic)!.MakeGenericMethod(typeof(string)).Invoke(null, null));
        }
        catch (Exception e) { Console.WriteLine("hidden generic fault=" + e.GetType().Name); }
        try
        {
            typeof(AmbiguousTemplate<>).MakeGenericType(typeof(int)).GetMethod("Make", PublicStatic)!.Invoke(null, null);
        }
        catch (Exception e) { Console.WriteLine("hidden ambiguity=" + e.GetType().Name); }
    }

    private static void PrivateNamedBindings()
    {
        try
        {
            var call = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>),
                typeof(PrivateBindingTemplate<>).MakeGenericType(typeof(int)), "Pick");
            Console.WriteLine("private template=" + call());
        }
        catch (Exception e) { Console.WriteLine("private template fault=" + e.GetType().Name); }
        try
        {
            var call = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>),
                typeof(PrivateBindingOpaque<>).MakeGenericType(OpaqueArgument()), "pick", true);
            Console.WriteLine("private opaque=" + call());
        }
        catch (Exception e) { Console.WriteLine("private opaque fault=" + e.GetType().Name); }
        try
        {
            var call = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>),
                typeof(PrivateBindingStatic<>).MakeGenericType(typeof(int)), "Pick");
            Console.WriteLine("private static=" + call());
        }
        catch (Exception e) { Console.WriteLine("private static fault=" + e.GetType().Name); }
    }

    private static Type[] OpaqueArguments() => new[] { typeof(int) };
    private static Type OpaqueArgument() => typeof(int);

    private static string TypeNullArray(string[] args)
    {
        Type[]? tuple = args.Length == 0 ? new[] { typeof(int) } : null;
        try { return (string)typeof(NullArrayType<>).MakeGenericType(tuple!).GetMethod("Name")!.Invoke(null, null)!; }
        catch (Exception e) { return e.GetType().Name; }
    }

    private static string TypeOpaqueArray(string[] args)
    {
        Type[] tuple = args.Length == 0 ? new[] { typeof(int) } : OpaqueArguments();
        try { return (string)typeof(OpaqueArrayType<>).MakeGenericType(tuple).GetMethod("Name")!.Invoke(null, null)!; }
        catch (Exception e) { return e.GetType().Name; }
    }

    private static string TypeOpaqueElement(string[] args)
    {
        Type argument = args.Length == 0 ? typeof(int) : OpaqueArgument();
        try { return (string)typeof(OpaqueElementType<>).MakeGenericType(new[] { argument }).GetMethod("Name")!.Invoke(null, null)!; }
        catch (Exception e) { return e.GetType().Name; }
    }

    private static string TypeNullElement(string[] args)
    {
        Type? argument = args.Length == 0 ? typeof(int) : null;
        try { return (string)typeof(NullElementType<>).MakeGenericType(new[] { argument! }).GetMethod("Name")!.Invoke(null, null)!; }
        catch (Exception e) { return e.GetType().Name; }
    }

    private static string TypeInvalidElement(string[] args)
    {
        Type argument = args.Length == 0 ? typeof(int) : typeof(void);
        try { return (string)typeof(InvalidElementType<>).MakeGenericType(new[] { argument }).GetMethod("Name")!.Invoke(null, null)!; }
        catch (Exception e) { return e.GetType().Name; }
    }

    private static string MethodNullArray(string[] args)
    {
        Type[]? tuple = args.Length == 0 ? new[] { typeof(int) } : null;
        try { return (string)typeof(AlternativeMethods).GetMethod("NullArray")!.MakeGenericMethod(tuple!).Invoke(null, null)!; }
        catch (Exception e) { return e.GetType().Name; }
    }

    private static string MethodOpaqueArray(string[] args)
    {
        Type[] tuple = args.Length == 0 ? new[] { typeof(int) } : OpaqueArguments();
        try { return (string)typeof(AlternativeMethods).GetMethod("OpaqueArray")!.MakeGenericMethod(tuple).Invoke(null, null)!; }
        catch (Exception e) { return e.GetType().Name; }
    }

    private static string MethodOpaqueElement(string[] args)
    {
        Type argument = args.Length == 0 ? typeof(int) : OpaqueArgument();
        try { return (string)typeof(AlternativeMethods).GetMethod("OpaqueElement")!.MakeGenericMethod(new[] { argument }).Invoke(null, null)!; }
        catch (Exception e) { return e.GetType().Name; }
    }

    private static string MethodNullElement(string[] args)
    {
        Type? argument = args.Length == 0 ? typeof(int) : null;
        try { return (string)typeof(AlternativeMethods).GetMethod("NullElement")!.MakeGenericMethod(new[] { argument! }).Invoke(null, null)!; }
        catch (Exception e) { return e.GetType().Name; }
    }

    private static void OpenDefinitionInterfaces()
    {
        try
        {
            Console.WriteLine("interface open lookup=" + typeof(OpenLookupOwner<>).GetInterface("IOpenLookupFixed")!
                .GetMethod("Run")!.Invoke(null, null));
        }
        catch (Exception e) { Console.WriteLine("open lookup fault=" + e.GetType().Name); }
        try
        {
            foreach (Type itf in typeof(OpenEnumerationOwner<>).GetInterfaces())
                Console.WriteLine("interface open enumeration=" + itf.GetMethod("Run")!.Invoke(null, null));
        }
        catch (Exception e) { Console.WriteLine("open enumeration fault=" + e.GetType().Name); }
        try
        {
            Console.WriteLine("interface open inherited=" + typeof(OpenInheritedOwner<>).GetInterface("IOpenInheritedFixed")!
                .GetMethod("Run")!.Invoke(null, null));
        }
        catch (Exception e) { Console.WriteLine("open inherited fault=" + e.GetType().Name); }
        try
        {
            Console.WriteLine("interface open fixed generic=" + typeof(OpenGenericFixedOwner<>).GetInterface("IOpenGenericFixed`1")!
                .GetMethod("Run")!.Invoke(null, null));
        }
        catch (Exception e) { Console.WriteLine("open fixed generic fault=" + e.GetType().Name); }
    }

    private static void InvokeDependentInterface() => typeof(OpenDependentOwner<>)
        .GetInterface("IOpenDependent`1")!.GetMethod("Run")!.Invoke(null, null);

    private static string InterfaceCaseChoice(string[] args)
    {
        bool ignoreCase = args.Length == 0 ? false : true;
        try { return (string)typeof(CaseInterfaceOwner).GetInterface("ICaseExact", ignoreCase)!.GetMethod("Run")!.Invoke(null, null)!; }
        catch (Exception e) { return e.GetType().Name; }
    }

    private static string InterfaceAmbiguity()
    {
        try { return (string)typeof(AmbiguousInterfaceOwner).GetInterface("iambiguousname", true)!.GetMethod("Run")!.Invoke(null, null)!; }
        catch (Exception e) { return e.GetType().Name; }
    }

    private static string InterfaceNameChoice(string[] args)
    {
        string name = args.Length == 0 ? "IFirstChoice" : "ISecondChoice";
        try { return (string)typeof(NamedInterfaceOwner).GetInterface(name)!.GetMethod("Run")!.Invoke(null, null)!; }
        catch (Exception e) { return e.GetType().Name; }
    }

    private static string InterfaceUnicode(string[] args)
    {
        string name = args.Length == 0 ? "IUnicodeChoice" : "IÅlpha";
        try { return (string)typeof(UnicodeInterfaceOwner).GetInterface(name, true)!.GetMethod("Run")!.Invoke(null, null)!; }
        catch (Exception e) { return e.GetType().Name; }
    }

    private static void NamedDelegateSelections()
    {
        var template = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>),
            typeof(DelegateArityTemplate<>).MakeGenericType(typeof(int)), "Pick");
        Console.WriteLine("delegate template=" + template());
        var eligibility = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>),
            typeof(DelegateArityEligibility<>).MakeGenericType(typeof(int)), "Pick");
        Console.WriteLine("delegate eligibility=" + eligibility());
        var fallback = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>),
            typeof(DelegateArityStatic<>).MakeGenericType(typeof(int)), "Pick");
        Console.WriteLine("delegate static=" + fallback());
    }

    private static void PropertyReturnSelections()
    {
        Type template = typeof(PropertyReturnTemplate<>).MakeGenericType(typeof(int));
        Console.WriteLine("property template=" + template.GetProperty("Item", typeof(int))!
            .GetValue(Activator.CreateInstance(template), new object[] { 3 }));
        Type eligibility = typeof(PropertyReturnEligibility<>).MakeGenericType(typeof(int));
        Console.WriteLine("property eligibility=" + eligibility.GetProperty("Item", typeof(int))!
            .GetValue(Activator.CreateInstance(eligibility), new object[] { 3 }));
        Type fallback = typeof(PropertyReturnStatic<>).MakeGenericType(typeof(int));
        Console.WriteLine("property static=" + fallback.GetProperty("Item", typeof(int))!
            .GetValue(Activator.CreateInstance(fallback), new object[] { 3 }));
    }

    private static void TemplateFieldSelections()
    {
        Type template = typeof(FieldSelectionTemplate<>).MakeGenericType(typeof(int));
        object receiver = Activator.CreateInstance(template)!;
        Console.WriteLine("field hidden=" + template.GetField("Hidden")!.GetValue(receiver));
        foreach (FieldInfo field in template.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            Console.WriteLine("field declared " + field.Name + "=" + field.GetValue(receiver));
    }

    private static string AssemblyChoice(bool library, bool unique)
    {
        Assembly assembly = typeof(Program).Assembly;
        if (library)
            assembly = typeof(SelectionAnchor).Assembly;
        string name = "AdmissionFilters.AssemblyChoiceTarget";
        if (unique)
            name = "AdmissionFilters.LibraryAssemblyChoiceTarget";
        return (string)assembly.GetType(name)!.GetMethod("Run")!.Invoke(null, null)!;
    }

    private static string AssemblyConstructionChoice(bool library, bool unique)
    {
        Assembly assembly = typeof(Program).Assembly;
        if (library)
            assembly = typeof(SelectionAnchor).Assembly;
        string name = "AdmissionFilters.AssemblyConstructedTarget";
        if (unique)
            name = "AdmissionFilters.LibraryAssemblyConstructedTarget";
        return assembly.CreateInstance(name)!.ToString()!;
    }

    private static string MixedAssemblyChoice(Assembly unknown, bool useUnknown)
    {
        Assembly assembly = typeof(Program).Assembly;
        if (useUnknown)
            assembly = unknown;
        return (string)assembly.GetType("AdmissionFilters.MixedAssemblyTarget")!.GetMethod("Run")!.Invoke(null, null)!;
    }

    private static void ArrayAssemblySelections()
    {
        Console.WriteLine("array sz row=" + typeof(SelectionSzTarget).GetMethod("Run")!.Name);
        try
        {
            Console.WriteLine("array sz=" + typeof(SelectionSzTarget[]).Assembly
                .GetType("AdmissionFilters.SelectionSzTarget")!.GetMethod("Run")!
                .Invoke(new SelectionSzTarget(), null));
        }
        catch (Exception e)
        {
            Console.WriteLine("array sz fault=" + e.GetType().Name);
        }
        Console.WriteLine("array md row=" + typeof(SelectionMdTarget).GetMethod("Run")!.Name);
        try
        {
            Console.WriteLine("array md=" + typeof(SelectionMdTarget[,]).Assembly
                .GetType("AdmissionFilters.SelectionMdTarget")!.GetMethod("Run")!
                .Invoke(new SelectionMdTarget(), null));
        }
        catch (Exception e)
        {
            Console.WriteLine("array md fault=" + e.GetType().Name);
        }
    }

    private static int UnknownConstructedRow(Type argument) => typeof(ConstructedOpenMethods<>)
        .GetMethod("Unknown")!.MakeGenericMethod(argument).GetCustomAttributes(false).Length;

    private static void InspectDetachedProjection() => _ = typeof(ProjectedDefinition<>)
        .GetMethod("NestedArray")!.ReturnType.GetCustomAttributes(false);

    private static string ConstructChoice(string[] args)
    {
        bool ignoreCase;
        if (args.Length == 0)
            ignoreCase = false;
        else
            ignoreCase = true;
        return Assembly.GetExecutingAssembly().CreateInstance("AdmissionFilters.CaseBuilt", ignoreCase)!.GetType().Name;
    }

    private static string TypeChoice(string[] args)
    {
        bool ignoreCase;
        if (args.Length == 0)
            ignoreCase = false;
        else
            ignoreCase = true;
        Type type = Assembly.GetExecutingAssembly().GetType("AdmissionFilters.CaseTyped", false, ignoreCase)!;
        return Activator.CreateInstance(type)!.GetType().Name;
    }

    private static string ConstructMixed(string[] args)
    {
        bool ignoreCase = false;
        if (args.Length > 0)
            ignoreCase = bool.Parse(args[0]);
        return Assembly.GetExecutingAssembly().CreateInstance("AdmissionFilters.MixedBuilt", ignoreCase)!.GetType().Name;
    }

    private static string TypeMixed(string[] args)
    {
        bool ignoreCase = false;
        if (args.Length > 0)
            ignoreCase = bool.Parse(args[0]);
        Type type = Assembly.GetExecutingAssembly().GetType("AdmissionFilters.MixedTyped", false, ignoreCase)!;
        return Activator.CreateInstance(type)!.GetType().Name;
    }

    private static void OpaqueChoices(bool ignoreCase)
    {
        Console.WriteLine(Assembly.GetExecutingAssembly().CreateInstance("AdmissionFilters.OpaqueBuilt", ignoreCase));
        Type type = Assembly.GetExecutingAssembly().GetType("AdmissionFilters.OpaqueTyped", false, ignoreCase)!;
        Console.WriteLine(Activator.CreateInstance(type));
    }
}
