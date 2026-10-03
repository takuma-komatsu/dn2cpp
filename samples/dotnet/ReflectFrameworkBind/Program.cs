using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using ReflectFrameworkBindLib;
using ReflectReturnLib;

// SUBJECT: a framework assembly's CreateDelegate as the only reflection call a
// program makes outside reflected bodies. The binder library is framework code to
// dn2cpp, so its binding opens no application reflection route, yet the rows it
// binds run through the receiver's slot. An interface's default body only such a
// binding enters compares a struct only it boxes by value, and its own Invoke and
// GetCustomAttributes calls open the routes a called body's would: an uncalled
// application method runs, and an attribute nothing else constructs is read. A
// struct only such an Invoke writes back through an out argument dispatches its
// interface member through the box and compares by value, and so does one such an
// Invoke returns from an interface's default body through the interface row, from a
// generic virtual override through its base row, or from a generic method's
// MakeGenericMethod instantiation (run last, in that order). A
// framework generic virtual row looked up by a runtime string runs an application
// override, and its own body for a receiver that overrides nothing. With
// DN2CPP_STRIPPED_OVERRIDES=1 the row reports a library override the image stripped
// instead of running its own body, where .NET runs the override; the image runs it
// too when a descriptor (keep-library-override.xml) keeps it, and when a conditional
// (required="false") rule keeps the override of a library receiver that only a body
// the row's dispatcher reaches allocates.
// Last, a function pointer type over a type of this assembly differs from one over
// ReflectReturnLib's same-named type: Invoke refuses a Pointer box of either for a
// parameter of the other, as .NET does.
namespace ReflectFrameworkBind;

struct Pair
{
    public int Number;
    public string Text;
}

[AttributeUsage(AttributeTargets.Class)]
sealed class TagAttribute : Attribute
{
    public TagAttribute(string name) => Name = name;

    public string Name { get; }
}

[Tag("tagged")]
sealed class Tagged { }

interface IShown
{
    string Show();
}

struct Written : IShown
{
    public int Number;
    public string Text;

    public string Show() => "written:" + Number + Text;
}

sealed class Hidden
{
    public string Run() => "hidden ran";

    public static void Fill(out Written written) => written = new Written { Number = 2, Text = "two" };

    public static Carried<T> Wrap<T>(T value) => new Carried<T> { Value = value };
}

struct Made : IShown
{
    public int Number;

    public string Show() => "made:" + Number;
}

struct Carried<T> : IShown
{
    public T Value;

    public string Show() => "carried:" + Value;
}

// Only the interface row reaches Make's default body: Maker declares no Make.
interface IMaker
{
    Made Make() => new Made { Number = 3 };
}

sealed class Maker : IMaker { }

class CarrierBase
{
    public virtual Carried<T> Carry<T>(T value) => default;
}

sealed class Carrier : CarrierBase
{
    public override Carried<T> Carry<T>(T value) => new Carried<T> { Value = value };
}

interface IReflectedOnly
{
    string Compare()
    {
        object first = new Pair { Number = 1, Text = "one" };
        object second = new Pair { Number = 1, Text = "one" };
        return first.Equals(second) + "/" + object.Equals(first, second) + "/"
            + (first.GetHashCode() == second.GetHashCode());
    }

    string InvokeHidden() => (string)typeof(Hidden).GetMethod("Run")!.Invoke(new Hidden(), null)!;

    string ReadTags()
    {
        object[] tags = typeof(Tagged).GetCustomAttributes(false);
        return tags.Length + (tags.Length > 0 ? ":" + ((TagAttribute)tags[0]).Name : "");
    }

    string InvokeWritten()
    {
        MethodInfo fill = typeof(Hidden).GetMethod("Fill")!;
        object?[] first = { null }, second = { null };
        fill.Invoke(null, first);
        fill.Invoke(null, second);
        return ((IShown)first[0]!).Show() + " equal=" + first[0]!.Equals(second[0]) + "/"
            + (first[0]!.GetHashCode() == second[0]!.GetHashCode());
    }

    string InvokeSlotRows()
    {
        MethodInfo make = typeof(IMaker).GetMethod("Make")!;
        object first = make.Invoke(new Maker(), null)!, second = make.Invoke(new Maker(), null)!;
        CarrierBase carrier = new Carrier();
        int direct = carrier.Carry(4).Value;
        object carried = typeof(CarrierBase).GetMethod("Carry")!.MakeGenericMethod(typeof(int))
            .Invoke(carrier, new object[] { 4 })!;
        long wrappedDirect = Hidden.Wrap(5L).Value;
        object wrapped = typeof(Hidden).GetMethod("Wrap")!.MakeGenericMethod(typeof(long))
            .Invoke(null, new object[] { 5L })!;
        return ((IShown)first).Show() + " equal=" + first.Equals(second) + " " + direct + " "
            + ((IShown)carried).Show() + " " + wrappedDirect + " " + ((IShown)wrapped).Show();
    }
}

sealed class ReflectedOnly : IReflectedOnly { }

class ApplicationProvider : TypeDescriptionProvider
{
    public string Registered = "unregistered";

    public override void RegisterType<T>() => Registered = "application:" + typeof(T).Name;
}

class PlainProvider : TypeDescriptionProvider
{
    public string Registered = "plain";

    // Only a base call names the row's instantiation, so no callvirt dispatches it.
    public void Instantiate() => base.RegisterType<Tagged>();
}

class ChainProvider : TypeDescriptionProvider
{
    public LateProvider? Next;

    // Only the row's dispatcher reaches this body, so the library receiver it
    // allocates arrives after that dispatcher registered.
    public override void RegisterType<T>() => Next = new LateProvider();
}

static class Program
{
    // Read at run time, so no member name follows a type token.
    private static string s_register = "RegisterType";
    private static string s_make = "Make";
    private static string s_makeVirtual = "MakeVirtual";
    private static string s_makeValue = "MakeValue";
    private static string s_makeLate = "MakeLate";
    private static string s_find = "Find";

    private static string LateLookup()
    {
        object result = typeof(LateFactory).GetMethod(s_makeLate)!.Invoke(null, null)!;
        return result.GetType().Name + "/" + ((ILibraryResult)result).Label();
    }

    private static void Try(string label, Func<string> run)
    {
        try
        {
            Console.WriteLine(label + ": " + run());
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + ": " + ex.GetType().Name + " 0x" + ex.HResult.ToString("X8") + " " + ex.Message);
        }
    }

    private static void Main()
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var reflected = new ReflectedOnly();
        Try("default body, boxed struct equality",
            () => FrameworkBinder.Call(typeof(IReflectedOnly).GetMethod("Compare")!, reflected));
        Try("default body, invoke route",
            () => FrameworkBinder.Call(typeof(IReflectedOnly).GetMethod("InvokeHidden")!, reflected));
        Try("default body, attribute route",
            () => FrameworkBinder.Call(typeof(IReflectedOnly).GetMethod("ReadTags")!, reflected));

        var plain = new PlainProvider();
        plain.Instantiate();
        MethodInfo register = typeof(TypeDescriptionProvider).GetMethod(s_register)!.MakeGenericMethod(typeof(Tagged));
        var application = new ApplicationProvider();
        Try("framework generic virtual row, application override", () =>
        {
            FrameworkBinder.Run(register, application);
            return application.Registered;
        });
        Try("framework generic virtual row, no override", () =>
        {
            FrameworkBinder.Run(register, plain);
            return plain.Registered;
        });
        if (Environment.GetEnvironmentVariable("DN2CPP_STRIPPED_OVERRIDES") == "1")
        {
            var library = new LibraryProvider();
            Try("framework generic virtual row, library override", () =>
            {
                FrameworkBinder.Run(register, library);
                return library.Registered;
            });
        }
        Console.WriteLine("framework bind end");
        if (Environment.GetEnvironmentVariable("DN2CPP_STRIPPED_OVERRIDES") == "1")
        {
            var chain = new ChainProvider();
            Try("framework generic virtual row, library receiver allocated late", () =>
            {
                FrameworkBinder.Run(register, chain);
                FrameworkBinder.Run(register, chain.Next!);
                return chain.Next!.Registered;
            });
        }
        Try("default body, invoke route write-back",
            () => FrameworkBinder.Call(typeof(IReflectedOnly).GetMethod("InvokeWritten")!, reflected));
        Try("default body, slot-row boxes",
            () => FrameworkBinder.Call(typeof(IReflectedOnly).GetMethod("InvokeSlotRows")!, reflected));
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_LIBRARY_STRUCT_RETURN") == "1")
            return;
        Console.WriteLine("== direct library struct return ==");
        LibraryFactory.Make();
        Try("runtime name, boxed return", () =>
        {
            object result = typeof(LibraryFactory).GetMethod(s_make)!.Invoke(null, null)!;
            return result.GetType().Name + "/" + ((ILibraryResult)result).Label()
                + "/" + result.ToString();
        });
        var virtualFactory = new VirtualFactory();
        virtualFactory.MakeVirtual(23);
        Try("runtime name, virtual boxed return", () =>
        {
            object result = virtualFactory.GetType().GetMethod(s_makeVirtual)!
                .Invoke(virtualFactory, new object[] { 23 })!;
            return result.GetType().Name + "/" + ((ILibraryResult)result).Label();
        });
        var valueFactory = new ValueFactory();
        valueFactory.MakeValue(41);
        object boxedFactory = valueFactory;
        Try("runtime name, boxed value owner", () =>
        {
            object result = boxedFactory.GetType().GetMethod(s_makeValue)!
                .Invoke(boxedFactory, new object[] { 41 })!;
            return result.GetType().Name + "/" + ((ILibraryResult)result).Label();
        });
        LateFactory.MakeLate();
        Try("runtime name, late owner", () =>
            (string)typeof(Program).GetMethod("LateLookup", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, null)!);
        var items = new List<GenericResult> { new GenericResult { Number = 29 } };
        Predicate<GenericResult> find = value => value.Number == 29;
        items.Find(find);
        Try("runtime name, framework generic return", () =>
        {
            object result = items.GetType().GetMethod(s_find)!.Invoke(items, new object[] { find })!;
            return result.GetType().Name + "/" + ((ILibraryResult)result).Label();
        });
        DeadFactory.MakeDead();
        Console.WriteLine("direct library struct return end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_FUNCTION_POINTER_IDENTITY") == "1")
            return;
        FunctionPointerIdentity.Run();
    }
}
