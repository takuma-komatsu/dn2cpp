using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ReflectReturnLib;

[assembly: RecognizedReflection.AssemblyNote("assembly note")]

// SUBJECT: every reflection API family the transpiler recognizes, and every value
// form its method-local summary models, admits the bodies the program runs through
// it with no preserve: lookups by name, flags and parameter types; enumerations
// walked by foreach; invocation, property and field access; delegate binding;
// construction; generic instantiation over known arguments; and attribute
// instances. The values reach those calls through locals, closure captures, a
// nested closure, a Type[] built in place, BindingFlags arithmetic, two constant
// names joined at a branch, a method type argument and an object's exact type,
// a Type an exception filter assigns for its own or a sibling handler, an index a
// narrowing conversion wraps, and a copied or returned Type[] the program writes.
// The lookup rules follow .NET's: binding flags joined at a branch select under
// each alternative, an inherited indexer overload stays visible, interfaces come
// from the whole hierarchy and match by simple or namespace-qualified name, a
// default member is found by its declared name, and a name binding matches every
// delegate arity its delegate type may have. A type initializer runs for
// RunClassConstructor and before a delegate bound to its type's static method, and
// a Type[] a lambda writes is read as written. Attribute instances come from a
// parameter, a closed generic class and a generic definition whose owners are
// explicit in the reading method; a MakeGenericType over a known argument and a
// constant closed generic name form types nothing else constructs. A known argument whose clone could not run the
// invoked method instantiates the definition statically, and the assembly listing
// returns a generic definition nothing names with typeof.
// A clone's generic argument, base and definition project from its template; a
// library generic definition's template runs the member a site selects; a library
// member listed by GetMember or GetDefaultMembers is counted however the result is
// consumed; reading a constant runs no initializer; an attribute read over a known
// assembly finds its assembly row; binding
// flags known only at run time still enumerate inherited statics; Activator flags with
// no lookup bit construct as Public | Instance; and a typed lookup admits only the
// overload the default binder returns, so the base overload it passes over neither
// compiles nor runs its declaring type's initializer. The explicit default of a
// FirstOrDefault, LastOrDefault or SingleOrDefault call is a Type the empty sequence returns.
// A specialization only typeof names finds its base's members. An attribute whose
// constructor a later call reaches, and whose named argument's setter nothing runs, is
// still defined and described. GetMember reads a trailing '*' as a name prefix,
// ignoring case under IgnoreCase, and a lone "*" as every name.
// A property inherited from a library base exposes no private setter; a library generic
// method definition only a lookup names stays listed; an indexer lookup by index type
// runs only that indexer's getter; Assembly.CreateInstance ignoring case, a type name
// qualified by a forwarding facade and GetUninitializedObject each reach the type they
// name; and a widened argument binds the one public constructor of its arity. An index
// whose 32-bit sum wraps reads the element it wraps to, and a boxed Nullable, direct or
// through a type argument, has its underlying type. A primitive named as a string or
// read as a field's type answers lookups as its class; a name-only lookup returns the
// nearer of two identically signed statics; GetAccessors returns an array the program
// may write; and an element no store reached on every path holds its default.
// A named delegate retains its selected method identity for unfiltered attribute
// construction, including an object target and inherited private template statics.
// Wrong names and arities retain no attribute factories, bodies or initializers.
// Implicit OrDefault reads retain integer zero and reference null beside source
// elements; explicit defaults and throwing reads retain their own alternatives.
// An as expression can return null beside its finite operand; an explicit cast
// preserves the operand on its normal continuation.
// Each case prints what .NET prints, so a target the summary failed to admit shows
// as a refusal line. Lookups.Echo(Int32), Lookups(Int32), Built(Int32, Int32),
// Mixer.Mix(String, Int32), Joins.Unpicked, Defaulted.Other, Prefixed.Other, the String
// indexer of TwoIndexes, HidingBase.Make and the library's BinderBase.Pick(Object) and
// EntityBase.set_Id are the siblings no site selects.
namespace RecognizedReflection;

[AttributeUsage(AttributeTargets.All)]
sealed class LabelAttribute : Attribute
{
    public LabelAttribute(string text) => Text = text;

    public string Text { get; }

    public int Rank { get; set; }
}

[Label("lookups", Rank = 3)]
sealed class Lookups
{
    public static string Shared = "shared";

    public int Count = 4;

    private string secret = "hidden";

    public Lookups()
    {
    }

    public Lookups(string name) => Name = name;

    public Lookups(int count) => Count = count;

    public event Action? Changed;

    public string Name { get; set; } = "lookups";

    private static int Quiet { get; set; } = 9;

    public string Echo(string text) => "echo:" + text;

    public string Echo(int value) => "echo#" + value;

    public static string Plain() => "plain";

    private string Whisper(string text) => "whisper:" + text + "/" + secret;

    public void Raise() => Changed?.Invoke();
}

[Label("steps")]
sealed class Steps
{
    public int Value = 5;

    public int Other = 7;

    public Steps()
    {
    }

    public Steps(int value) => Value = value;

    public string First { get; set; } = "f";

    public string Second { get; set; } = "s";

    public static int StepA() => 1;

    public static int StepB() => 20;

    public static int StepC() => 300;

    public static int RunA() => 4000;

    public static int RunB() => 50000;
}

sealed class Binds
{
    private readonly string prefix;

    public Binds(string prefix) => this.prefix = prefix;

    public static string Make() => "made";

    public static int Seven() => 7;

    public string Tag(string text) => prefix + text;

    private static string Hushed() => "hushed";
}

class Built
{
    public readonly string Origin;

    public Built() => Origin = "default";

    public Built(string origin) => Origin = origin;

    public Built(int left, int right) => Origin = "pair " + (left + right);

    [Label("kind", Rank = 1)]
    public string Kind() => "built:" + Origin;
}

sealed class Secluded
{
    private Secluded()
    {
    }

    public override string ToString() => "secluded";
}

sealed class Named
{
    public string Hello() => "named";
}

struct Point2
{
    public int X;
    public int Y;
}

sealed class Box<T>
{
    public T? Value;

    public string Describe() => "box:" + typeof(T).Name + "=" + Value;
}

sealed class Pair<TKey, TValue>
{
    public string Shape() => "pair:" + typeof(TKey).Name + "," + typeof(TValue).Name;
}

static class Generics
{
    public static string Wrap<T>(T value) => "[" + typeof(T).Name + ":" + value + "]";
}

static class Mixer
{
    public static string Mix(int count, string text) => "mix:" + count + text;

    public static string Mix(string text, int count) => "xim:" + text + count;
}

static class Joins
{
    public static string Left() => "left";

    public static string Right() => "right";

    public static string Unpicked() => "unpicked";
}

static class Closures
{
    public static string Captured() => "captured";

    public static string Nested() => "nested";
}

sealed class SpecClass
{
    public static string Describe() => "spec class";
}

struct SpecValue
{
    public static string Describe() => "spec value";
}

static class FilterBefore
{
    public static string Describe() => "before";
}

static class FilterChosen
{
    public static string Describe() => "chosen";
}

static class FilterSibling
{
    public static string Describe() => "sibling";
}

static class Converted
{
    public static string Low() => "low";
}

static class CopyOriginal
{
    public static string Describe() => "original";
}

static class CopyWritten
{
    public static string Describe() => "written";
}

class FlagBase
{
    public string Inherited() => "inherited";
}

sealed class FlagDerived : FlagBase
{
}

class IndexerBase
{
    public string this[string key] => "base:" + key;
}

sealed class IndexerDerived : IndexerBase
{
    public string this[int index] => "derived:" + index;
}

interface IRooted
{
    string Root();
}

interface IMiddle : IRooted
{
}

interface IGeneric<T>
{
    string Generic();
}

class InterfaceBase : IMiddle, IGeneric<int>
{
    public string Root() => "root";

    public string Generic() => "generic";
}

sealed class InterfaceDerived : InterfaceBase
{
}

[DefaultMember("Pick")]
sealed class Defaulted
{
    public string Pick() => "picked";

    public string Other() => "other";
}

interface ISequenceDefaults
{
    string First();
    string Last();
    string Single();
    string ElementAt();
    string Predicate();
}

sealed class SequenceDefaultTarget : ISequenceDefaults
{
    string ISequenceDefaults.First() => "first";
    string ISequenceDefaults.Last() => "last";
    string ISequenceDefaults.Single() => "single";
    string ISequenceDefaults.ElementAt() => "element";
    string ISequenceDefaults.Predicate() => "predicate";
    private string Other() => "unselected";
}

sealed class SequenceZeroOptions
{
    public SequenceZeroOptions(string? value) => Made = "zero";
    private SequenceZeroOptions(object? value) => Made = "unselected";
    public string Made { get; }
}

sealed class SequenceJoinedOptions
{
    public SequenceJoinedOptions(string? value) => Made = "default alternative";
    private SequenceJoinedOptions(object? value) => Made = "source";
    public string Made { get; }
}

sealed class SequenceThrowingOptions
{
    public SequenceThrowingOptions(string? value) => Made = "unselected";
    private SequenceThrowingOptions(object? value) => Made = "throwing";
    public string Made { get; }
}

sealed class SequenceExplicitOptions
{
    public SequenceExplicitOptions(string? value) => Made = "unselected";
    private SequenceExplicitOptions(object? value) => Made = "explicit";
    public string Made { get; }
}

static class SequenceReferenceDefault
{
    public static string Run() => "reference";
    public static string Other() => "unselected";
}

static class Arities
{
    public static string Pick() => "none";

    public static string Pick(string text) => "one:" + text;
}

static class RunInitialized
{
    static RunInitialized() => Program.ranInitializer = "ran";
}

static class BoundInitialized
{
    static BoundInitialized() => Program.boundInitializer = "initialized";

    public static string Run() => "bound";
}

sealed class WrittenLeft
{
}

sealed class WrittenRight
{
}

sealed class WrittenWide
{
}

sealed class WrittenShape<TFirst, TSecond>
{
}

static class WrittenTake
{
    public static string Take(WrittenLeft left, WrittenWide wide) => "left,wide";

    public static string Take(WrittenWide first, WrittenWide second) => "wide,wide";
}

[AttributeUsage(AttributeTargets.All)]
sealed class NoteAttribute : Attribute
{
    public NoteAttribute(string text) => Text = text;

    public string Text { get; }
}

[AttributeUsage(AttributeTargets.All)]
sealed class DefinitionNoteAttribute : Attribute
{
    public DefinitionNoteAttribute(string text) => Text = text;

    public string Text { get; }
}

[AttributeUsage(AttributeTargets.All)]
sealed class ParameterNoteAttribute : Attribute
{
    public ParameterNoteAttribute(string text) => Text = text;

    public string Text { get; }
}

[AttributeUsage(AttributeTargets.All)]
sealed class ScannedNoteAttribute : Attribute
{
    public ScannedNoteAttribute(string text) => Text = text;

    public string Text { get; }
}

[Note("generic class")]
sealed class NotedBox<T>
{
}

[DefinitionNote("definition")]
sealed class DefinedBox<T>
{
}

[ScannedNote("scanned generic")]
sealed class ScannedBox<T>
{
}

static class NotedParameters
{
    public static string Take([ParameterNote("parameter")] string value) => value;

    public static void Scanned([ScannedNote("scanned parameter")] int value)
    {
    }
}

sealed class LaterArgument
{
}

sealed class LaterBox<T>
{
    public string Load() => "load:" + typeof(T).Name;
}

// A clone cannot create a List<T> over its own argument, so MakeGenericType over
// the known Int32 instantiates MintedBox<Int32>.
sealed class MintedBox<T>
{
    public object Make() => new List<T>();
}

sealed class ListedBox<T>
{
}

sealed class NamedBox<T>
{
    public static int Formed { get; set; }

    public string Describe() => "named:" + typeof(T).Name;
}

sealed class NestedNameInner<T>
{
    public static string Unselected() => "inner sibling";
}

sealed class NestedNameOuter<T>
{
    public static string Run() => "nested";
    public static string Unselected() => "outer sibling";
}

sealed class DeepNameLeaf<T>
{
    public static string Unselected() => "leaf sibling";
}

sealed class DeepNameMiddle<T>
{
    public static string Unselected() => "middle sibling";
}

sealed class DeepNameOuter<T>
{
    public static string Run() => "deep";
    public static string Unselected() => "deep sibling";
}

sealed class QualifiedNameInner<T>
{
    public static string Unselected() => "qualified inner sibling";
}

sealed class QualifiedNameOuter<T>
{
    public static string Run() => "qualified";
    public static string Unselected() => "qualified outer sibling";
}

sealed class ConstrainedNameInner<T> where T : struct
{
    public static string Unselected() => "constrained inner sibling";
}

sealed class ConstrainedNameOuter<T>
{
    public static string Run() => "constrained";
    public static string Rejected() => "rejected tuple";
    public static string Unselected() => "constrained outer sibling";
}

sealed class ChainedNameInner<T>
{
    public static string Unselected() => "chained inner sibling";
}

sealed class ChainedNameOuter<T>
{
    public static string Run() => "chained";
    public static string Unselected() => "chained outer sibling";
}

class HolderBase
{
    public static string BaseRun() => "base run";
}

// No code names an instantiation; each MakeGenericType clones its template.
class ProjectedHolder<T> : HolderBase
{
    public static string Describe() => "described:" + typeof(T).Name;
}

sealed class ProjectedArgument
{
    public static string Run() => "argument run";
}

sealed class ProjectedOther
{
}

sealed class Constants
{
    public const int Limit = 7;

    static Constants() => Program.constantsInitialized = "initialized";
}

[AttributeUsage(AttributeTargets.Assembly)]
sealed class AssemblyNoteAttribute : Attribute
{
    public AssemblyNoteAttribute(string text) => Text = text;

    public string Text { get; }
}

class FlatBase
{
    public static string Create() => "created";
}

sealed class FlatDerived : FlatBase
{
    public static string Other() => "other";
}

sealed class DefaultFlagged
{
    public readonly string Origin;

    public DefaultFlagged() => Origin = "default lookup";
}

static class SequenceDefaults
{
    public static string First() => "first";

    public static string Last() => "last";

    public static string Single() => "single";
}

class SpecializedBase
{
    public static string Run() => "specialized base";
}

// A field keeps the definition off the runtime-instantiation templates.
class OnlyNamed<T> : SpecializedBase
{
    public T? Value;
}

[AttributeUsage(AttributeTargets.Class)]
sealed class LateLabelAttribute : Attribute
{
    public int Weight { get; set; }

    public static LateLabelAttribute Make() => new LateLabelAttribute();
}

[LateLabel(Weight = 2)]
sealed class LateLabeled
{
}

static class Prefixed
{
    public static string RunAlpha() => "alpha";

    public static string RunBeta() => "beta";

    public static string Cased() => "cased";

    public static string Other() => "other";
}

static class Starred
{
    public static string Only() => "starred";
}

sealed class Customer : EntityBase
{
}

sealed class TwoIndexes
{
    public string this[int index] => "int:" + index;

    public string this[string key] => "string:" + key;
}

sealed class PrimitiveOptions
{
    public object? Tag;
}

static class HiddenLog
{
    internal static string? Text;
}

// A precise initializer: only a compiled HidingBase.Make reaches it.
static class HiddenProbe
{
    static HiddenProbe() => HiddenLog.Text = "probe initialized";

    public static string Touch() => "base";
}

class HidingBase
{
    public static string Make() => HiddenProbe.Touch();
}

class HidingDerived : HidingBase
{
    public static new string Make() => "derived";
}

static class AccessorSource
{
    public static string Value => "accessor";
}

static class AccessorReplacement
{
    public static string Run() => "replaced";
}

sealed class DefaultElementTarget
{
    public string Made = "default element";
}

sealed class CasedWidget
{
    public override string ToString() => "cased";
}

sealed class ForwardedElement
{
}

static class Wrapped
{
    public static string Run() => "wrapped";
}

struct NullableBoxed
{
    public string Run() => "nullable";
}

struct NullableArgument
{
    public string Run() => "argument";
}

abstract class UninitBase
{
    public abstract string Who();
}

// Only GetUninitializedObject allocates it; no constructor runs.
sealed class UninitDerived : UninitBase
{
    public override string Who() => "derived";
}

sealed class ObjectBindingNoteAttribute : Attribute
{
    public ObjectBindingNoteAttribute() { }
    public override string ToString() => "object note";
}
sealed class InheritedBindingNoteAttribute : Attribute
{
    public InheritedBindingNoteAttribute() { }
    public override string ToString() => "inherited note";
}
sealed class TemplateBindingNoteAttribute : Attribute
{
    public TemplateBindingNoteAttribute() { }
    public override string ToString() => "template note";
}
sealed class ExcludedBindingNoteAttribute : Attribute
{
    public ExcludedBindingNoteAttribute() => NamedAttributeProbe.Touch();
}
static class NamedAttributeProbe
{
    static NamedAttributeProbe() => Console.WriteLine("unselected delegate attribute initialized");
    public static string Touch() => "unselected delegate attribute";
}
sealed class NamedAttributeObject
{
    [ObjectBindingNote]
    private string Pick() => "object";
    [ExcludedBindingNote]
    private string Pick(int value) => NamedAttributeProbe.Touch();
    [ExcludedBindingNote]
    private string Other() => NamedAttributeProbe.Touch();
}
class NamedAttributeBase
{
    [InheritedBindingNote]
    private static string Pick() => "inherited";
    [ExcludedBindingNote]
    private static string Pick(int value) => NamedAttributeProbe.Touch();
    [ExcludedBindingNote]
    private static string Other() => NamedAttributeProbe.Touch();
}
sealed class NamedAttributeDerived : NamedAttributeBase { }
class NamedAttributeTemplateBase<T>
{
    [TemplateBindingNote]
    private static string Pick() => "template:" + typeof(T).Name;
    [ExcludedBindingNote]
    private static string Pick(int value) => NamedAttributeProbe.Touch();
    [ExcludedBindingNote]
    private static string Other() => NamedAttributeProbe.Touch();
}
sealed class NamedAttributeTemplate<T> : NamedAttributeTemplateBase<T> { }

sealed class AsFailedOptions
{
    public readonly string Made;
    public AsFailedOptions() => Made = "default";
    public AsFailedOptions(object? value) => Made = "argument";
    private AsFailedOptions(string value) => Made = "private";
}

sealed class AsSuccessfulOptions
{
    public readonly string Made;
    public AsSuccessfulOptions() => Made = "default";
    public AsSuccessfulOptions(object? value) => Made = "argument";
    private AsSuccessfulOptions(string value) => Made = "private";
}

sealed class CastclassOptions
{
    public readonly string Made;
    public CastclassOptions() => Made = "default";
    public CastclassOptions(object? value) => Made = "argument";
    private CastclassOptions(string value) => Made = "private";
}

static class Program
{
    private static int raised;

    internal static string? ranInitializer, boundInitializer, constantsInitialized;

    private static void Main(string[] args)
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        bool beforeNestedNames = args.Length > 0 && args[0] == "before-nested-names";
        bool beforeDelegateAttributes = args.Length > 0 && args[0] == "before-delegate-attributes";
        bool beforeSequenceDefaults = args.Length > 0 && args[0] == "before-sequence-defaults";
        bool beforeIsinstNull = args.Length > 0 && args[0] == "before-isinst-null";
        if (beforeNestedNames || beforeDelegateAttributes || beforeSequenceDefaults || beforeIsinstNull)
            args = Array.Empty<string>();

        Console.WriteLine("== recognized reflection table ==");
        Show("lookup method by name", LookupByName);
        Show("lookup method by types", LookupByTypes);
        Show("lookup method by flags", LookupByFlags);
        Show("lookup method by flags and types", LookupByFlagsAndTypes);
        Show("lookup field", LookupField);
        Show("lookup property", LookupProperty);
        Show("lookup constructor", LookupConstructor);
        Show("lookup constructor by flags", LookupConstructorByFlags);
        Show("lookup event", LookupEvent);
        Show("lookup member", LookupMember);
        Show("enumerate methods", EnumerateMethods);
        Show("enumerate runtime methods", EnumerateRuntimeMethods);
        Show("enumerate fields", EnumerateFields);
        Show("enumerate properties", EnumerateProperties);
        Show("enumerate constructors", EnumerateConstructors);
        Show("bind static", BindStatic);
        Show("bind instance", BindInstance);
        Show("bind generic", BindGeneric);
        Show("bind named instance", BindNamedInstance);
        Show("bind named static", BindNamedStatic);
        Show("construct default", ConstructDefault);
        Show("construct arguments", ConstructArguments);
        Show("construct nonpublic", ConstructNonPublic);
        Show("construct value type", ConstructValueType);
        Show("construct generic", ConstructGeneric);
        Show("construct named type", ConstructNamedType);
        Show("exact object type", ExactObjectType);
        Show("instantiate type", InstantiateType);
        Show("instantiate type array", InstantiateTypeArray);
        Show("instantiate method", InstantiateMethod);
        Show("attribute instances", AttributeInstances);
        Show("attribute generic", AttributeGeneric);
        Show("attribute member", AttributeMember);
        Show("type array lookup", TypeArrayLookup);
        Show("binding flags arithmetic", BindingFlagsArithmetic);
        Show("joined names", () => JoinedNames(args));
        Show("method type argument", () => TypeArgument<SpecClass>() + "/" + TypeArgument<SpecValue>());
        Type captured = typeof(Closures);
        string member = nameof(Closures.Captured);
        Show("closure capture", () => (string)captured.GetMethod(member)!.Invoke(null, null)!);
        string outer = nameof(Closures.Nested);
        Func<Func<string>> make = () =>
        {
            Type inner = typeof(Closures);
            return () => (string)inner.GetMethod(outer)!.Invoke(null, null)!;
        };
        Show("nested closure", make());
        Show("filter assignment", FilterAssignment);
        Show("filter declined", FilterDeclined);
        Show("converted index", ConvertedIndex);
        Show("copied array write", CopiedArrayWrite);
        Show("written generic arguments", WrittenGenericArguments);
        Show("binding flags join", () => BindingFlagsJoin(args));
        Show("inherited indexer", InheritedIndexer);
        Show("interface closure", InterfaceClosure);
        Show("interface names", InterfaceNames);
        Show("default members", DefaultMembers);
        Show("delegate arity join", () => DelegateArityJoin(args));
        Show("run class constructor", RunClassConstructor);
        Show("bound initializer", BoundInitializer);
        Show("captured array write", () => CapturedArrayWrite(args));
        Show("attribute parameter", AttributeParameter);
        Show("attribute generic class", () => typeof(NotedBox<int>).GetCustomAttribute<NoteAttribute>()!.Text);
        Show("attribute definition", () => typeof(DefinedBox<>).GetCustomAttribute<DefinitionNoteAttribute>()!.Text);
        Show("attribute known owners", () => KnownOwnerAttribute() + "/" + KnownParameterAttribute());
        Show("known generic arguments", MadeKnown);
        Show("named generic", NamedGeneric);
        Show("template fallback", TemplateFallback);
        Show("listed definition", ListedDefinition);
        Show("template projections", TemplateProjections);
        Show("library template", LibraryTemplateMember);
        Show("library listing", LibraryListing);
        Show("constant field", ConstantField);
        Show("attribute known assembly", KnownAssemblyAttribute);
        Show("unknown flags inherited", () => UnknownFlagsInherited(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy));
        Show("construct default lookup flags", ConstructDefaultLookupFlags);
        Show("typed lookup binder choice", TypedLookupBinderChoice);
        Show("sequence default", SequenceDefault);
        Show("specialization base", SpecializationBase);
        Show("attribute defined before construction", AttributeDefinedBeforeConstruction);
        Show("member name prefix", MemberNamePrefix);
        Show("inherited private accessor", InheritedPrivateAccessor);
        Show("listed generic definition", ListedGenericDefinition);
        Show("typed indexer", () => (string)typeof(TwoIndexes).GetProperty("Item", new[] { typeof(int) })!
            .GetValue(new TwoIndexes(), new object[] { 3 })!);
        Show("create instance ignoring case", () =>
            typeof(Program).Assembly.CreateInstance("recognizedreflection.casedwidget", true)!.ToString()!);
        Show("forwarded qualifier", ForwardedQualifier);
        Show("uninitialized object", () =>
            ((UninitBase)RuntimeHelpers.GetUninitializedObject(typeof(UninitDerived))).Who());
        Show("construct widened argument", () =>
            ((WidenedOptions)Activator.CreateInstance(typeof(WidenedOptions), 16)!).Made);
        Show("wrapped index", WrappedIndex);
        Show("boxed nullable", () => BoxedNullable() + "/" + BoxedArgument<NullableArgument?>(new NullableArgument()));
        Show("primitive owner", PrimitiveOwner);
        Show("hidden base method", HiddenBaseMethod);
        Show("written accessors", WrittenAccessors);
        Show("default flag element", () => DefaultFlagElement(args));
        Console.WriteLine("recognized reflection table end");
        if (beforeNestedNames)
            return;
        Console.WriteLine("== nested generic constant names ==");
        Show("nested chained control", ChainedNamedGeneric);
        Show("nested constant", NestedNamedGeneric);
        Show("nested deep", DeepNamedGeneric);
        Show("nested qualified", QualifiedNamedGeneric);
        Show("nested constrained", ConstrainedNamedGeneric);
        if (args.Length < 0)
            Type.GetType("RecognizedReflection.ConstrainedNameOuter`1[RecognizedReflection.ConstrainedNameInner`1[System.String]]")!
                .GetMethod("Rejected")!.Invoke(null, null);
        Console.WriteLine("nested generic constant names end");
        if (beforeDelegateAttributes)
            return;
        Console.WriteLine("== named delegate method attributes ==");
        Show("delegate object attributes", NamedObjectAttributes);
        Show("delegate inherited attributes", NamedInheritedAttributes);
        Show("delegate template attributes", NamedTemplateAttributes);
        Console.WriteLine("named delegate method attributes end");
        if (beforeSequenceDefaults)
            return;
        Console.WriteLine("== implicit sequence defaults ==");
        Show("implicit first", ImplicitFirstDefault);
        Show("implicit last", ImplicitLastDefault);
        Show("implicit single", ImplicitSingleDefault);
        Show("implicit element", ImplicitElementDefault);
        Show("implicit predicate", ImplicitPredicateDefault);
        Show("implicit zero", ImplicitZeroDefault);
        Show("implicit joined", ImplicitJoinedDefault);
        Show("implicit reference", ImplicitReferenceDefault);
        Show("throwing sequence", ThrowingSequenceElement);
        Show("explicit sequence", ExplicitSequenceElement);
        Console.WriteLine("implicit sequence defaults end");
        if (beforeIsinstNull)
            return;
        Console.WriteLine("== nullable type tests ==");
        Show("failed as arguments", FailedAsArguments);
        Show("successful as arguments", SuccessfulAsArguments);
        Show("castclass arguments", CastclassArguments);
        Console.WriteLine("nullable type tests end");
    }

    private static void Show(string label, Func<string> run)
    {
        string result;
        try
        {
            result = run();
        }
        catch (Exception e)
        {
            Exception shown = e is TargetInvocationException { InnerException: { } inner } ? inner : e;
            result = shown.GetType().Name + ": " + shown.Message;
        }
        Console.WriteLine(label + ": " + result);
    }

    private static string LookupByName() =>
        (string)typeof(Lookups).GetMethod("Plain")!.Invoke(null, null)!;

    private static string LookupByTypes()
    {
        var target = new Lookups();
        MethodInfo echo = typeof(Lookups).GetMethod("Echo", new[] { typeof(string) })!;
        return (string)echo.Invoke(target, new object[] { "typed" })!;
    }

    private static string LookupByFlags()
    {
        var target = new Lookups();
        MethodInfo whisper = typeof(Lookups).GetMethod("Whisper", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (string)whisper.Invoke(target, new object[] { "flags" })!;
    }

    private static string LookupByFlagsAndTypes()
    {
        var target = new Lookups();
        MethodInfo echo = typeof(Lookups).GetMethod("Echo", BindingFlags.Public | BindingFlags.Instance, null,
            new[] { typeof(string) }, null)!;
        return (string)echo.Invoke(target, new object[] { "binder" })!;
    }

    private static string LookupField()
    {
        var target = new Lookups();
        FieldInfo count = typeof(Lookups).GetField("Count")!;
        count.SetValue(target, 11);
        FieldInfo secret = typeof(Lookups).GetField("secret", BindingFlags.NonPublic | BindingFlags.Instance)!;
        secret.SetValue(target, "revealed");
        FieldInfo shared = typeof(Lookups).GetField("Shared", BindingFlags.Public | BindingFlags.Static)!;
        return count.GetValue(target) + "/" + secret.GetValue(target) + "/" + shared.GetValue(null);
    }

    private static string LookupProperty()
    {
        var target = new Lookups();
        PropertyInfo name = typeof(Lookups).GetProperty("Name")!;
        name.SetValue(target, "renamed");
        PropertyInfo quiet = typeof(Lookups).GetProperty("Quiet", BindingFlags.NonPublic | BindingFlags.Static)!;
        quiet.SetValue(null, 12);
        PropertyInfo typed = typeof(Lookups).GetProperty("Name", typeof(string))!;
        return name.GetValue(target) + "/" + quiet.GetValue(null) + "/" + typed.GetValue(new Lookups());
    }

    private static string LookupConstructor()
    {
        ConstructorInfo ctor = typeof(Lookups).GetConstructor(new[] { typeof(string) })!;
        var made = (Lookups)ctor.Invoke(new object[] { "constructed" });
        return made.Name + "/" + made.Count;
    }

    private static string LookupConstructorByFlags()
    {
        ConstructorInfo ctor = typeof(Built).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null,
            Type.EmptyTypes, null)!;
        return ((Built)ctor.Invoke(null)).Kind();
    }

    private static string LookupEvent()
    {
        var target = new Lookups();
        EventInfo changed = typeof(Lookups).GetEvent("Changed")!;
        Action handler = () => raised++;
        changed.AddEventHandler(target, handler);
        target.Raise();
        target.Raise();
        changed.RemoveEventHandler(target, handler);
        target.Raise();
        return "raised " + raised;
    }

    private static string LookupMember()
    {
        MemberInfo[] found = typeof(Lookups).GetMember("Plain");
        return found.Length + " " + (string)((MethodInfo)found[0]).Invoke(null, null)!;
    }

    private static string EnumerateMethods()
    {
        int total = 0;
        foreach (MethodInfo step in typeof(Steps).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            if (step.Name.StartsWith("Step", StringComparison.Ordinal))
                total += (int)step.Invoke(null, null)!;
        return "steps " + total;
    }

    private static string EnumerateRuntimeMethods()
    {
        int total = 0;
        IEnumerable<MethodInfo> methods = typeof(Steps).GetRuntimeMethods();
        foreach (MethodInfo run in methods)
            if (run.Name.StartsWith("Run", StringComparison.Ordinal))
                total += (int)run.Invoke(null, null)!;
        return "runs " + total;
    }

    private static string EnumerateFields()
    {
        var steps = new Steps();
        int total = 0;
        foreach (FieldInfo field in typeof(Steps).GetFields())
            total += (int)field.GetValue(steps)!;
        return "fields " + total;
    }

    private static string EnumerateProperties()
    {
        var steps = new Steps();
        var parts = new List<string>();
        foreach (PropertyInfo property in typeof(Steps).GetProperties())
            parts.Add(property.Name + "=" + property.GetValue(steps));
        parts.Sort(string.CompareOrdinal);
        return string.Join(",", parts);
    }

    private static string EnumerateConstructors()
    {
        var values = new List<string>();
        foreach (ConstructorInfo ctor in typeof(Steps).GetConstructors())
        {
            object made = ctor.GetParameters().Length == 0 ? ctor.Invoke(null) : ctor.Invoke(new object[] { 9 });
            values.Add(((Steps)made).Value.ToString());
        }
        values.Sort(string.CompareOrdinal);
        return string.Join(",", values);
    }

    private static string BindStatic()
    {
        var make = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), typeof(Binds).GetMethod("Make")!);
        return make();
    }

    private static string BindInstance()
    {
        MethodInfo tag = typeof(Binds).GetMethod("Tag")!;
        var bound = (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), new Binds("<"), tag);
        return bound("bound") + " " + tag.CreateDelegate<Func<string, string>>(new Binds(">"))("typed");
    }

    private static string BindGeneric()
    {
        var seven = (Func<int>)typeof(Binds).GetMethod("Seven")!.CreateDelegate(typeof(Func<int>));
        return "seven " + seven();
    }

    private static string BindingNote(object[] attributes)
    {
        foreach (object attribute in attributes)
        {
            string text = attribute.ToString()!;
            if (text.EndsWith(" note", StringComparison.Ordinal))
                return text;
        }
        return "missing note";
    }

    private static string NamedObjectAttributes()
    {
        var call = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), new NamedAttributeObject(), "pick", true);
        return call() + "/" + BindingNote(call.Method.GetCustomAttributes(false));
    }

    private static string NamedInheritedAttributes()
    {
        var call = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), typeof(NamedAttributeDerived), "Pick");
        return call() + "/" + BindingNote(call.Method.GetCustomAttributes(false));
    }

    private static string NamedTemplateAttributes()
    {
        var call = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>),
            typeof(NamedAttributeTemplate<>).MakeGenericType(typeof(int)), "PICK", true);
        return call() + "/" + BindingNote(call.Method.GetCustomAttributes(false));
    }

    private static string BindNamedInstance()
    {
        var tag = (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), new Binds("#"), "Tag");
        return tag("named");
    }

    private static string BindNamedStatic()
    {
        var hushed = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), typeof(Binds), "Hushed");
        return hushed();
    }

    private static string ConstructDefault() => ((Built)Activator.CreateInstance(typeof(Built))!).Kind();

    private static string ConstructArguments() =>
        ((Built)Activator.CreateInstance(typeof(Built), new object[] { "argument" })!).Kind();

    private static string ConstructNonPublic() => Activator.CreateInstance(typeof(Secluded), true)!.ToString()!;

    private static string ConstructValueType()
    {
        var point = (Point2)Activator.CreateInstance(typeof(Point2))!;
        return "point " + point.X + "," + point.Y;
    }

    private static string ConstructGeneric() => Activator.CreateInstance<Built>().Kind();

    private static string ConstructNamedType()
    {
        Type named = Type.GetType("RecognizedReflection.Named")!;
        object made = Activator.CreateInstance(named)!;
        return (string)named.GetMethod("Hello")!.Invoke(made, null)!;
    }

    private static string ExactObjectType()
    {
        object built = new Built("exact");
        return (string)built.GetType().GetMethod("Kind")!.Invoke(built, null)!;
    }

    private static string InstantiateType()
    {
        Type closed = typeof(Box<>).MakeGenericType(typeof(int));
        object box = Activator.CreateInstance(closed)!;
        closed.GetField("Value")!.SetValue(box, 42);
        return (string)closed.GetMethod("Describe")!.Invoke(box, null)!;
    }

    private static string InstantiateTypeArray()
    {
        Type[] arguments = new[] { typeof(long), typeof(string) };
        Type closed = typeof(Pair<,>).MakeGenericType(arguments);
        object pair = Activator.CreateInstance(closed)!;
        return (string)closed.GetMethod("Shape")!.Invoke(pair, null)!;
    }

    private static string InstantiateMethod()
    {
        MethodInfo wrap = typeof(Generics).GetMethod("Wrap")!.MakeGenericMethod(typeof(string));
        return (string)wrap.Invoke(null, new object[] { "wrapped" })!;
    }

    private static string AttributeInstances()
    {
        var labels = new List<string>();
        foreach (object attribute in typeof(Lookups).GetCustomAttributes(false))
            if (attribute is LabelAttribute label)
                labels.Add(label.Text + "#" + label.Rank);
        return string.Join(",", labels);
    }

    private static string AttributeGeneric()
    {
        LabelAttribute label = typeof(Steps).GetCustomAttribute<LabelAttribute>()!;
        return label.Text + "#" + label.Rank + " defined=" + typeof(Steps).IsDefined(typeof(LabelAttribute), false);
    }

    private static string AttributeMember()
    {
        MethodInfo kind = typeof(Built).GetMethod("Kind")!;
        var label = (LabelAttribute)Attribute.GetCustomAttribute(kind, typeof(LabelAttribute))!;
        return label.Text + "#" + label.Rank;
    }

    private static string TypeArrayLookup()
    {
        Type[] parameters = new[] { typeof(int), typeof(string) };
        MethodInfo mix = typeof(Mixer).GetMethod("Mix", parameters)!;
        return (string)mix.Invoke(null, new object[] { 3, "x" })!;
    }

    private static string BindingFlagsArithmetic()
    {
        var target = new Lookups();
        BindingFlags flags = BindingFlags.Instance;
        flags |= BindingFlags.NonPublic | BindingFlags.Public;
        flags &= ~BindingFlags.Public;
        MethodInfo whisper = typeof(Lookups).GetMethod("Whisper", flags)!;
        return (string)whisper.Invoke(target, new object[] { "arithmetic" })!;
    }

    private static string JoinedNames(string[] args)
    {
        string name;
        if (args.Length == 0)
            name = "Left";
        else
            name = "Right";
        return (string)typeof(Joins).GetMethod(name)!.Invoke(null, null)!;
    }

    private static string TypeArgument<T>() => (string)typeof(T).GetMethod("Describe")!.Invoke(null, null)!;

    // The handler runs after its filter, so it sees the Type the filter stored.
    private static string FilterAssignment()
    {
        Type target = typeof(FilterBefore);
        try
        {
            throw new InvalidOperationException("filtered");
        }
        catch (InvalidOperationException) when ((target = typeof(FilterChosen)) is not null)
        {
            return (string)target.GetMethod("Describe")!.Invoke(null, null)!;
        }
    }

    // A declining filter's store reaches the next handler of the same protected range.
    private static string FilterDeclined()
    {
        Type target = typeof(FilterBefore);
        try
        {
            throw new InvalidOperationException("declined");
        }
        catch (InvalidOperationException) when ((target = typeof(FilterSibling)) is null)
        {
            return "unreached";
        }
        catch (InvalidOperationException)
        {
            return (string)target.GetMethod("Describe")!.Invoke(null, null)!;
        }
    }

    // conv.u1 wraps 256 to element 0.
    private static string ConvertedIndex()
    {
        Type[] targets = new Type[1];
        targets[0] = typeof(Converted);
        int wide = 256;
        Type target = targets[(byte)wide];
        return (string)target.GetMethod("Low")!.Invoke(null, null)!;
    }

    // ToArray copies: a write to the copy leaves the source unchanged.
    private static string CopiedArrayWrite()
    {
        Type[] originals = { typeof(CopyOriginal) };
        Type[] copy = originals.ToArray();
        copy[0] = typeof(CopyWritten);
        return (string)copy[0].GetMethod("Describe")!.Invoke(null, null)! + "/"
            + (string)originals[0].GetMethod("Describe")!.Invoke(null, null)!;
    }

    private static string WrittenGenericArguments()
    {
        Type[] arguments = { typeof(int), typeof(string) };
        arguments[1] = typeof(long);
        Type closed = typeof(Pair<,>).MakeGenericType(arguments);
        return (string)closed.GetMethod("Shape")!.Invoke(Activator.CreateInstance(closed), null)!;
    }

    // DeclaredOnly in the alternative that does not run must not hide the inherited method.
    private static string BindingFlagsJoin(string[] args)
    {
        BindingFlags flags = args.Length == 0 ? BindingFlags.Public | BindingFlags.Instance
            : BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        return (string)typeof(FlagDerived).GetMethod("Inherited", flags)!.Invoke(new FlagDerived(), null)!;
    }

    // A derived indexer hides an inherited one only by name and signature, so the
    // typed lookup reaches the base indexer past the derived one.
    private static string InheritedIndexer()
    {
        var target = new IndexerDerived();
        PropertyInfo inherited = typeof(IndexerDerived).GetProperty("Item", typeof(string), new[] { typeof(string) })!;
        PropertyInfo declared = typeof(IndexerDerived).GetProperty("Item", typeof(string), new[] { typeof(int) })!;
        return (string)inherited.GetValue(target, new object[] { "k" })! + ","
            + (string)declared.GetValue(target, new object[] { 3 })!;
    }

    // InterfaceDerived declares no interface; its base and IMiddle supply IRooted.
    private static string InterfaceClosure()
    {
        var target = new InterfaceDerived();
        var roots = new List<string>();
        foreach (Type implemented in typeof(InterfaceDerived).GetInterfaces())
            if (implemented.Name == nameof(IRooted))
                roots.Add((string)implemented.GetMethod("Root")!.Invoke(target, null)!);
        return string.Join(",", roots);
    }

    private static string InterfaceNames()
    {
        var target = new InterfaceDerived();
        Type type = typeof(InterfaceDerived);
        return (string)type.GetInterface("IRooted")!.GetMethod("Root")!.Invoke(target, null)! + "/"
            + (string)type.GetInterface("IGeneric`1")!.GetMethod("Generic")!.Invoke(target, null)! + "/"
            + (string)type.GetInterface("RecognizedReflection.IRooted")!.GetMethod("Root")!.Invoke(target, null)! + "/"
            + (string)type.GetInterface("irooted", true)!.GetMethod("Root")!.Invoke(target, null)!;
    }

    private static string DefaultMembers()
    {
        MemberInfo[] members = typeof(Defaulted).GetDefaultMembers();
        return members.Length + " " + (string)((MethodInfo)members[0]).Invoke(new Defaulted(), null)!;
    }

    // The binding runs with the one-argument delegate type.
    private static string DelegateArityJoin(string[] args)
    {
        Type delegateType = args.Length == 0 ? typeof(Func<string, string>) : typeof(Func<string>);
        Delegate pick = Delegate.CreateDelegate(delegateType, typeof(Arities), "Pick");
        return (string)pick.DynamicInvoke(args.Length == 0 ? new object[] { "x" } : null)!;
    }

    private static string RunClassConstructor()
    {
        RuntimeHelpers.RunClassConstructor(typeof(RunInitialized).TypeHandle);
        return ranInitializer ?? "not run";
    }

    private static string BoundInitializer()
    {
        var run = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), typeof(BoundInitialized), "Run");
        return run() + "/" + (boundInitializer ?? "not initialized");
    }

    // The lambda writes the array GetGenericArguments returned, so the lookup sees
    // WrittenLeft beside WrittenWide, a pair no single stored array holds.
    private static string CapturedArrayWrite(string[] args)
    {
        Type[] parameters = args.Length > 0 ? new[] { typeof(WrittenWide), typeof(WrittenWide) }
            : typeof(WrittenShape<WrittenLeft, WrittenRight>).GetGenericArguments();
        Action write = () => parameters[1] = typeof(WrittenWide);
        write();
        return (string)typeof(WrittenTake).GetMethod("Take", parameters)!
            .Invoke(null, new object[] { new WrittenLeft(), new WrittenWide() })!;
    }

    private static string AttributeParameter()
    {
        ParameterInfo value = typeof(NotedParameters).GetMethod("Take")!.GetParameters()[0];
        return value.GetCustomAttribute<ParameterNoteAttribute>()!.Text;
    }

    private static string KnownOwnerAttribute() => typeof(ScannedBox<int>).GetCustomAttribute<ScannedNoteAttribute>()!.Text;

    private static string KnownParameterAttribute() =>
        typeof(NotedParameters).GetMethod("Scanned")!.GetParameters()[0].GetCustomAttribute<ScannedNoteAttribute>()!.Text;

    private static string MadeKnown()
    {
        Type made = typeof(LaterBox<>).MakeGenericType(typeof(LaterArgument));
        return (string)made.GetMethod("Load")!.Invoke(Activator.CreateInstance(made), null)!;
    }

    // No code names NamedBox<int>; the constant name forms it.
    private static string NamedGeneric()
    {
        Type named = Type.GetType("RecognizedReflection.NamedBox`1[[System.Int32, System.Private.CoreLib]]")!;
        return (string)named.GetMethod("Describe")!.Invoke(Activator.CreateInstance(named), null)!;
    }

    // The chained control uses different definitions, so it cannot supply a closed
    // argument identity that would hide a constant-name admission failure.
    private static string ChainedNamedGeneric()
    {
        Type inner = typeof(ChainedNameInner<>).MakeGenericType(typeof(int));
        Type outer = typeof(ChainedNameOuter<>).MakeGenericType(inner);
        return (string)outer.GetMethod("Run")!.Invoke(null, null)!;
    }

    private static string NestedNamedGeneric()
    {
        GC.KeepAlive(typeof(NestedNameInner<>).MakeGenericType(typeof(int)));
        GC.KeepAlive(typeof(NestedNameOuter<>));
        Type named = Type.GetType("RecognizedReflection.NestedNameOuter`1[RecognizedReflection.NestedNameInner`1[System.Int32]]")!;
        return (string)named.GetMethod("Run")!.Invoke(null, null)!;
    }

    private static string DeepNamedGeneric()
    {
        GC.KeepAlive(typeof(DeepNameLeaf<>));
        GC.KeepAlive(typeof(DeepNameMiddle<>));
        GC.KeepAlive(typeof(DeepNameOuter<>));
        Type named = Type.GetType("RecognizedReflection.DeepNameOuter`1[RecognizedReflection.DeepNameMiddle`1[RecognizedReflection.DeepNameLeaf`1[System.Int32]]]")!;
        return (string)named.GetMethod("Run")!.Invoke(null, null)!;
    }

    private static string QualifiedNamedGeneric()
    {
        GC.KeepAlive(typeof(QualifiedNameInner<>));
        GC.KeepAlive(typeof(QualifiedNameOuter<>));
        Type named = Type.GetType("RecognizedReflection.QualifiedNameOuter`1[[RecognizedReflection.QualifiedNameInner`1[[System.Int32, System.Private.CoreLib]], RecognizedReflectionOnly]], RecognizedReflectionOnly")!;
        return (string)named.GetMethod("Run")!.Invoke(null, null)!;
    }

    private static string ConstrainedNamedGeneric()
    {
        GC.KeepAlive(typeof(ConstrainedNameInner<>));
        GC.KeepAlive(typeof(ConstrainedNameOuter<>));
        Type named = Type.GetType("RecognizedReflection.ConstrainedNameOuter`1[RecognizedReflection.ConstrainedNameInner`1[System.Int32]]")!;
        return (string)named.GetMethod("Run")!.Invoke(null, null)!;
    }

    private static string TemplateFallback()
    {
        Type made = typeof(MintedBox<>).MakeGenericType(typeof(int));
        object list = made.GetMethod("Make")!.Invoke(Activator.CreateInstance(made), null)!;
        return list.GetType().Name + "/" + (list is List<int>);
    }

    private static string ListedDefinition()
    {
        foreach (Type type in typeof(Program).Assembly.GetTypes())
            if (type.Name == "ListedBox`1")
                return type.Name + ":" + type.IsGenericTypeDefinition;
        return "missing";
    }

    private static string TemplateProjections()
    {
        Type made = typeof(ProjectedHolder<>).MakeGenericType(typeof(ProjectedArgument));
        string argument = (string)made.GetGenericArguments()[0].GetMethod("Run")!.Invoke(null, null)!;
        string baseRun = (string)made.BaseType!.GetMethod("BaseRun")!.Invoke(null, null)!;
        Type remade = made.GetGenericTypeDefinition().MakeGenericType(typeof(ProjectedOther));
        return argument + "/" + baseRun + "/" + (string)remade.GetMethod("Describe")!.Invoke(null, null)!;
    }

    private static string LibraryTemplateMember() =>
        (string)typeof(LibraryTemplate<>).MakeGenericType(typeof(ProjectedArgument)).GetMethod("Describe")!
            .Invoke(null, null)!;

    // Nothing reads the members these lookups return beyond counting them.
    private static string LibraryListing()
    {
        GC.KeepAlive(new LibraryMembers());
        return typeof(LibraryMembers).GetMember("Listed").Length + "/" + typeof(LibraryMembers).GetDefaultMembers().Length;
    }

    private static string ConstantField() =>
        (int)typeof(Constants).GetField("Limit")!.GetValue(null)! + "/" + (constantsInitialized ?? "not initialized");

    private static string KnownAssemblyAttribute() => typeof(Program).Assembly.GetCustomAttribute<AssemblyNoteAttribute>()!.Text;

    private static string UnknownFlagsInherited(BindingFlags flags)
    {
        foreach (MethodInfo method in typeof(FlatDerived).GetMethods(flags))
            if (method.Name == "Create")
                return (string)method.Invoke(null, null)!;
        return "missing";
    }

    private static string ConstructDefaultLookupFlags() =>
        ((DefaultFlagged)Activator.CreateInstance(typeof(DefaultFlagged), BindingFlags.CreateInstance, null, null, null)!)
            .Origin;

    private static string TypedLookupBinderChoice()
    {
        MethodInfo pick = typeof(BinderDerived).GetMethod("Pick",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy, null, new[] { typeof(string) }, null)!;
        return (string)pick.Invoke(null, new object[] { "s" })! + "/" + (BinderLog.Text ?? "base not initialized");
    }

    private static string SequenceDefault()
    {
        Type[] none = Array.Empty<Type>();
        return (string)none.FirstOrDefault(typeof(SequenceDefaults))!.GetMethod("First")!.Invoke(null, null)!
            + "/" + (string)none.LastOrDefault(t => t.IsPublic, typeof(SequenceDefaults))!.GetMethod("Last")!.Invoke(null, null)!
            + "/" + (string)none.SingleOrDefault(typeof(SequenceDefaults))!.GetMethod("Single")!.Invoke(null, null)!;
    }

    private static string SpecializationBase() =>
        (string)typeof(OnlyNamed<int>).BaseType!.GetMethod("Run")!.Invoke(null, null)!;

    private static string AttributeDefinedBeforeConstruction()
    {
        bool defined = typeof(LateLabeled).IsDefined(typeof(LateLabelAttribute), false);
        string data = string.Join(",", typeof(LateLabeled).GetCustomAttributesData());
        return defined + "/" + data + "/" + LateLabelAttribute.Make().Weight;
    }

    private static string MemberNamePrefix()
    {
        var ran = new List<string>();
        foreach (MemberInfo member in typeof(Prefixed).GetMember("Run*"))
            ran.Add((string)((MethodInfo)member).Invoke(null, null)!);
        ran.Sort(StringComparer.Ordinal);
        MemberInfo cased = typeof(Prefixed).GetMember("cASe*", MemberTypes.Method,
            BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)[0];
        MemberInfo[] starred = typeof(Starred).GetMember("*",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        return string.Join(",", ran) + "/" + (string)((MethodInfo)cased).Invoke(null, null)! + "/"
            + starred.Length + ":" + (string)((MethodInfo)starred[0]).Invoke(null, null)!;
    }

    private static string InheritedPrivateAccessor()
    {
        var customer = new Customer();
        var seen = new List<string>();
        foreach (PropertyInfo property in typeof(Customer).GetProperties())
        {
            bool writable = property.GetSetMethod(true) is not null;
            if (writable)
                property.SetValue(customer, 7);
            seen.Add(property.Name + ":" + writable + ":" + property.GetValue(customer));
        }
        return string.Join(",", seen);
    }

    private static string ListedGenericDefinition()
    {
        MethodInfo? make = typeof(GenericListed).GetMethod("Make");
        return make is null ? "missing" : make.Name + ":" + make.IsGenericMethodDefinition;
    }

    // The sum overflows 32 bits and wraps to 0.
    private static string WrappedIndex()
    {
        Type[] targets = { typeof(Wrapped) };
        int start = int.MaxValue;
        int index = unchecked(start + int.MaxValue + 2);
        return (string)targets[index].GetMethod("Run")!.Invoke(null, null)!;
    }

    private static string BoxedNullable()
    {
        NullableBoxed? value = new NullableBoxed();
        object boxed = value;
        return (string)boxed.GetType().GetMethod("Run")!.Invoke(boxed, null)!;
    }

    private static string BoxedArgument<T>(T value)
    {
        object boxed = value!;
        return (string)boxed.GetType().GetMethod("Run")!.Invoke(boxed, null)!;
    }

    private static string ForwardedQualifier()
    {
        Type listed = Type.GetType("System.Collections.Generic.List`1[[RecognizedReflection.ForwardedElement, "
            + "RecognizedReflectionOnly]], mscorlib")!;
        var made = (System.Collections.IList)Activator.CreateInstance(listed)!;
        return listed.Name + ":" + made.Count;
    }

    // A primitive named as a string or read as a field's type answers lookups as its class.
    private static string PrimitiveOwner()
    {
        var shared = new object();
        object named = Type.GetType("System.Object")!.GetMethod("ReferenceEquals")!
            .Invoke(null, new[] { shared, shared })!;
        object field = typeof(PrimitiveOptions).GetField("Tag")!.FieldType
            .GetMethod("Equals", new[] { typeof(object), typeof(object) })!.Invoke(null, new object[] { "x", "x" })!;
        return named + "/" + field;
    }

    // Of two identically signed statics a name-only lookup returns the nearer one.
    private static string HiddenBaseMethod()
    {
        MethodInfo make = typeof(HidingDerived).GetMethod("Make",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!;
        return (string)make.Invoke(null, null)! + "/" + (HiddenLog.Text ?? "probe not initialized");
    }

    // GetAccessors returns an array of its own: the element written runs.
    private static string WrittenAccessors()
    {
        MethodInfo[] methods = typeof(AccessorSource).GetProperty("Value")!.GetAccessors();
        methods[0] = typeof(AccessorReplacement).GetMethod("Run")!;
        return (string)methods[0].Invoke(null, null)!;
    }

    private static string ImplicitFirstDefault() =>
        (string)typeof(SequenceDefaultTarget).GetInterface("ISequenceDefaults", new bool[0].FirstOrDefault())!
            .GetMethod("First")!.Invoke(new SequenceDefaultTarget(), null)!;

    private static string ImplicitLastDefault() =>
        (string)typeof(SequenceDefaultTarget).GetInterface("ISequenceDefaults", new bool[0].LastOrDefault())!
            .GetMethod("Last")!.Invoke(new SequenceDefaultTarget(), null)!;

    private static string ImplicitSingleDefault() =>
        (string)typeof(SequenceDefaultTarget).GetInterface("ISequenceDefaults", new bool[0].SingleOrDefault())!
            .GetMethod("Single")!.Invoke(new SequenceDefaultTarget(), null)!;

    private static string ImplicitElementDefault() =>
        (string)typeof(SequenceDefaultTarget).GetInterface("ISequenceDefaults", new bool[0].ElementAtOrDefault(0))!
            .GetMethod("ElementAt")!.Invoke(new SequenceDefaultTarget(), null)!;

    private static string ImplicitPredicateDefault() =>
        (string)typeof(SequenceDefaultTarget).GetInterface("ISequenceDefaults", new bool[0].FirstOrDefault(value => value))!
            .GetMethod("Predicate")!.Invoke(new SequenceDefaultTarget(), null)!;

    private static string ImplicitZeroDefault() =>
        ((SequenceZeroOptions)Activator.CreateInstance(typeof(SequenceZeroOptions),
            (BindingFlags)new int[0].FirstOrDefault(), null, new object?[] { null }, null)!).Made;

    private static string ImplicitJoinedDefault() =>
        ((SequenceJoinedOptions)Activator.CreateInstance(typeof(SequenceJoinedOptions),
            new[] { BindingFlags.Instance | BindingFlags.NonPublic }.FirstOrDefault(), null,
            new object?[] { null }, null)!).Made;

    private static string ImplicitReferenceDefault() =>
        (string)typeof(SequenceReferenceDefault).GetMethod("Run", BindingFlags.Public | BindingFlags.Static,
            new Binder[0].FirstOrDefault(), Type.EmptyTypes, null)!.Invoke(null, null)!;

    private static string ThrowingSequenceElement()
    {
        BindingFlags[] flags = { BindingFlags.Instance | BindingFlags.NonPublic };
        return ((SequenceThrowingOptions)Activator.CreateInstance(typeof(SequenceThrowingOptions), flags.First(), null,
                new object?[] { null }, null)!).Made + "/"
            + ((SequenceThrowingOptions)Activator.CreateInstance(typeof(SequenceThrowingOptions), flags.Last(), null,
                new object?[] { null }, null)!).Made + "/"
            + ((SequenceThrowingOptions)Activator.CreateInstance(typeof(SequenceThrowingOptions), flags.Single(), null,
                new object?[] { null }, null)!).Made + "/"
            + ((SequenceThrowingOptions)Activator.CreateInstance(typeof(SequenceThrowingOptions), flags.ElementAt(0), null,
                new object?[] { null }, null)!).Made;
    }

    private static string ExplicitSequenceElement()
    {
        BindingFlags[] flags = new BindingFlags[0];
        return ((SequenceExplicitOptions)Activator.CreateInstance(typeof(SequenceExplicitOptions),
                flags.FirstOrDefault(BindingFlags.Instance | BindingFlags.NonPublic), null,
                new object?[] { null }, null)!).Made + "/"
            + ((SequenceExplicitOptions)Activator.CreateInstance(typeof(SequenceExplicitOptions),
                flags.LastOrDefault(value => value != 0, BindingFlags.Instance | BindingFlags.NonPublic), null,
                new object?[] { null }, null)!).Made + "/"
            + ((SequenceExplicitOptions)Activator.CreateInstance(typeof(SequenceExplicitOptions),
                flags.SingleOrDefault(BindingFlags.Instance | BindingFlags.NonPublic), null,
                new object?[] { null }, null)!).Made;
    }

    private static string FailedAsArguments()
    {
        object source = new int[1];
        object[]? arguments = source as object[];
        return ((AsFailedOptions)Activator.CreateInstance(typeof(AsFailedOptions), arguments)!).Made;
    }

    private static string SuccessfulAsArguments()
    {
        object source = new object[] { "value" };
        object[]? arguments = source as object[];
        return ((AsSuccessfulOptions)Activator.CreateInstance(typeof(AsSuccessfulOptions), arguments)!).Made;
    }

    private static string CastclassArguments()
    {
        object source = new object[] { "value" };
        object[] arguments = (object[])source;
        return ((CastclassOptions)Activator.CreateInstance(typeof(CastclassOptions), arguments)!).Made;
    }

    // Without arguments no store reaches the element, whose zero flags construct as
    // Public | Instance.
    private static string DefaultFlagElement(string[] args)
    {
        var flags = new BindingFlags[1];
        if (args.Length != 0)
            flags[0] = BindingFlags.Instance | BindingFlags.NonPublic;
        return ((DefaultElementTarget)Activator.CreateInstance(typeof(DefaultElementTarget), flags[0], null, null, null)!)
            .Made;
    }
}
