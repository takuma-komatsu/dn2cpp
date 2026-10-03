#nullable enable
// SUBJECT: the reflection routes reach the classes that the bodies they reach mint,
// and the values reflection boxes dispatch as a box site's do. A closed generic
// first constructed by a method only Invoke runs has its methods invocable, and one
// first named by such a method is constructible through Activator.CreateInstance.
// A generic struct that nests itself one level deeper per call stays invocable at
// the levels the program reaches without the transpile expanding it further. A struct
// that reflection alone boxes, returned by Invoke (directly or as a Nullable<T>),
// read through FieldInfo.GetValue or PropertyInfo.GetValue, or created by
// Activator.CreateInstance or ConstructorInfo.Invoke, answers its interface members
// through the box, and two field reads of equal structs compare equal. So does one
// written back through a method's or a constructor's out argument (directly or as a
// Nullable<T>) or returned by reference. A generic that such a method first
// constructs nested deeper than anything the program had instantiated has its
// methods invocable too, also when the program instantiated it shallower first.
// So does a struct that an interface's default method, property getter or out
// argument yields through the interface row, that a generic virtual override returns
// through its base row, or that a generic method's MakeGenericMethod instantiation
// returns. A generic virtual method whose signature names an application generic
// struct over its own type parameter also runs from a call site. A closed generic that
// only another class's attribute row names carries its own attribute rows, at every
// depth of a chain of such classes.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReflectRouteClassSubset;

class Minted<T>
{
    public string Run() => "run:" + typeof(T).Name;
}

class Named<T>
{
    public string Show() => "show:" + typeof(T).Name;
}

class DeepMinted<T>
{
    public string Run() => "deep:" + typeof(T).Name;
}

struct Nest<T>
{
    public Nest<Nest<T>> Wrap() => new Nest<Nest<T>>();

    public string Arg() => "arg:" + typeof(T).Name;
}

interface IRouteShow
{
    string Show();
}

struct Returned : IRouteShow
{
    public int V;

    public string Show() => "returned:" + V;
}

struct Held : IRouteShow
{
    public int V;

    public string Show() => "held:" + V;
}

struct Kept : IRouteShow
{
    public int V;

    public string Show() => "kept:" + V;
}

struct Made : IRouteShow
{
    public int V;

    public Made(int v) => V = v;

    public string Show() => "made:" + V;
}

struct Plain
{
    public int Number;
    public string Text;
}

struct WrittenOut : IRouteShow
{
    public int Number;
    public string Text;

    public string Show() => "out:" + Number + Text;
}

struct WrittenMaybe : IRouteShow
{
    public int Number;

    public string Show() => "maybe:" + Number;
}

struct WrittenRef : IRouteShow
{
    public int Number;

    public string Show() => "ref:" + Number;
}

struct WrittenCtor : IRouteShow
{
    public int Number;

    public string Show() => "ctor:" + Number;
}

class WrittenMaker
{
    public WrittenMaker(out WrittenCtor made) => made = new WrittenCtor { Number = 3 };
}

class DeepShallow<T>
{
    public string Run() => "shallow:" + typeof(T).Name;
}

class Carrier
{
    public Held Field = new Held { V = 1 };
    public Plain Pair = new Plain { Number = 7, Text = "seven" };

    public Kept Value { get; set; } = new Kept { V = 2 };

    public Returned Get() => new Returned { V = 3 };

    public Returned? GetMaybe() => new Returned { V = 4 };

    public Returned? GetNone() => null;
}

struct SlotMade : IRouteShow
{
    public int Number;

    public string Show() => "made:" + Number;
}

struct SlotHeld : IRouteShow
{
    public int Number;

    public string Show() => "held:" + Number;
}

struct SlotFilled : IRouteShow
{
    public int Number;

    public string Show() => "filled:" + Number;
}

struct SlotCarried<T> : IRouteShow
{
    public T Value;

    public string Show() => "carried:" + Value;
}

// Only the interface rows reach these default bodies: SlotMaker declares none of them.
interface ISlotMaker
{
    SlotMade Make() => new SlotMade { Number = 1 };

    SlotHeld Held => new SlotHeld { Number = 2 };

    void Fill(out SlotFilled filled) => filled = new SlotFilled { Number = 3 };
}

class SlotMaker : ISlotMaker
{
}

class SlotBase
{
    public virtual SlotCarried<T> Carry<T>(T value) => default;
}

class SlotDerived : SlotBase
{
    public override SlotCarried<T> Carry<T>(T value) => new SlotCarried<T> { Value = value };
}

[AttributeUsage(AttributeTargets.Class)]
sealed class NextLevelAttribute : Attribute
{
    public NextLevelAttribute(Type next) => Next = next;

    public Type Next { get; }
}

// Only the row of the level before names each closed generic level, and a static class
// has no member another route could reach, so walking a level reaches nothing new.
[NextLevel(typeof(AttrLevel1<int>))]
static class AttrRoot
{
}

[NextLevel(typeof(AttrLevel2<int>))]
static class AttrLevel1<T>
{
}

[NextLevel(typeof(AttrLevel3<int>))]
static class AttrLevel2<T>
{
}

[NextLevel(typeof(AttrLevel4<int>))]
static class AttrLevel3<T>
{
}

[NextLevel(typeof(AttrLevel5<int>))]
static class AttrLevel4<T>
{
}

[NextLevel(typeof(AttrLevel6<int>))]
static class AttrLevel5<T>
{
}

[NextLevel(typeof(AttrLevel7<int>))]
static class AttrLevel6<T>
{
}

[NextLevel(typeof(AttrLevel8<int>))]
static class AttrLevel7<T>
{
}

[NextLevel(typeof(AttrLevel9<int>))]
static class AttrLevel8<T>
{
}

[NextLevel(typeof(AttrRoot))]
static class AttrLevel9<T>
{
}

static class Program
{
    public static object MakeMinted() => new Minted<int>();

    public static object MakeMintedShared() => new Minted<string>();

    public static Type NameNamed() => typeof(Named<long>);

    public static object MakeNest() => new Nest<int>();

    public static object MakeDeepMinted() =>
        new DeepMinted<List<List<List<List<List<List<List<List<List<List<List<List<int>>>>>>>>>>>>>();

    public static void MakeOut(out WrittenOut made) => made = new WrittenOut { Number = 1, Text = "o" };

    public static void MakeMaybe(out WrittenMaybe? made) => made = new WrittenMaybe { Number = 4 };

    private static readonly WrittenRef[] s_spots = { new WrittenRef { Number = 2 } };

    public static ref WrittenRef Spot() => ref s_spots[0];

    public static SlotCarried<T> Wrap<T>(T value) => new SlotCarried<T> { Value = value };

    public static object MakeDeepShallow() =>
        new DeepShallow<List<List<List<List<List<List<List<List<List<List<List<List<int>>>>>>>>>>>>>();

    private static void Try(string label, Func<object?> run)
    {
        try
        {
            Console.WriteLine(label + ": " + (run() ?? "null"));
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + ": " + ex.GetType().Name);
        }
    }

    private static object? Call(object target, string name) =>
        target.GetType().GetMethod(name) is { } method ? method.Invoke(target, null) : "no row";

    private static object? CallStatic(string name) => typeof(Program).GetMethod(name)!.Invoke(null, null);

    internal static void Run()
    {
        Console.WriteLine("== reflection route classes ==");
        Try("minted by an invoked body", () => Call(CallStatic("MakeMinted")!, "Run"));
        Try("minted by an invoked body, shared", () => Call(CallStatic("MakeMintedShared")!, "Run"));
        Try("named by an invoked body", () =>
        {
            var type = (Type)CallStatic("NameNamed")!;
            return Call(Activator.CreateInstance(type)!, "Show");
        });
        Try("self-nesting generic", () =>
        {
            object first = CallStatic("MakeNest")!;
            object second = Call(first, "Wrap")!;
            return Call(first, "Arg") + " " + Call(second, "Arg") + " " + Call(second, "Wrap")!.GetType().Name;
        });

        var carrier = new Carrier();
        Type carrierType = typeof(Carrier);
        Try("invoke return", () => ((IRouteShow)carrierType.GetMethod("Get")!.Invoke(carrier, null)!).Show());
        Try("invoke nullable return", () =>
        {
            object boxed = carrierType.GetMethod("GetMaybe")!.Invoke(carrier, null)!;
            return boxed.GetType().Name + " " + ((IRouteShow)boxed).Show();
        });
        Try("invoke empty nullable return", () => carrierType.GetMethod("GetNone")!.Invoke(carrier, null) is null);
        Try("field read", () => ((IRouteShow)carrierType.GetField("Field")!.GetValue(carrier)!).Show());
        Try("field reads equal", () =>
        {
            FieldInfo pair = carrierType.GetField("Pair")!;
            object first = pair.GetValue(carrier)!, second = pair.GetValue(new Carrier())!;
            return first.Equals(second) + "/" + (first.GetHashCode() == second.GetHashCode());
        });
        Try("property read", () => ((IRouteShow)carrierType.GetProperty("Value")!.GetValue(carrier)!).Show());
        Try("created", () => ((IRouteShow)Activator.CreateInstance(typeof(Made))!).Show());
        Try("constructed", () =>
            ((IRouteShow)typeof(Made).GetConstructor(new[] { typeof(int) })!.Invoke(new object[] { 5 })).Show());
        Console.WriteLine("reflection route classes end");
    }

    internal static void RunDeepMinted()
    {
        Console.WriteLine("== reflection route deep classes ==");
        Try("minted past every nesting", () => Call(CallStatic("MakeDeepMinted")!, "Run"));
        Console.WriteLine("reflection route deep classes end");
    }

    internal static void RunShallowFirst()
    {
        Console.WriteLine("== reflection route shallow-first classes ==");
        Try("minted past every nesting after a shallow one", () =>
            new DeepShallow<int>().Run() + " " + Call(CallStatic("MakeDeepShallow")!, "Run"));
        Console.WriteLine("reflection route shallow-first classes end");
    }

    internal static void RunWrittenBack()
    {
        Console.WriteLine("== reflection route written-back boxes ==");
        Try("out argument", () =>
        {
            MethodInfo make = typeof(Program).GetMethod("MakeOut")!;
            object?[] first = { null }, second = { null };
            make.Invoke(null, first);
            make.Invoke(null, second);
            return ((IRouteShow)first[0]!).Show() + " equal=" + first[0]!.Equals(second[0])
                + "/" + (first[0]!.GetHashCode() == second[0]!.GetHashCode());
        });
        Try("nullable out argument", () =>
        {
            object?[] args = { null };
            typeof(Program).GetMethod("MakeMaybe")!.Invoke(null, args);
            return args[0]!.GetType().Name + " " + ((IRouteShow)args[0]!).Show();
        });
        Try("ref return", () => ((IRouteShow)typeof(Program).GetMethod("Spot")!.Invoke(null, null)!).Show());
        Try("constructor out argument", () =>
        {
            object?[] args = { null };
            object made = typeof(WrittenMaker).GetConstructors()[0].Invoke(args);
            return made.GetType().Name + " " + ((IRouteShow)args[0]!).Show();
        });
        Console.WriteLine("reflection route written-back boxes end");
    }

    internal static void RunWrittenBackPairs()
    {
        Console.WriteLine("== reflection route written-back pairs ==");
        Type type = typeof(Program);
        MethodInfo outMethod = type.GetMethod("MakeOut")!;
        MethodInfo maybeMethod = type.GetMethod("MakeMaybe")!;
        MethodInfo refMethod = type.GetMethod("Spot")!;
        ConstructorInfo ctor = typeof(WrittenMaker).GetConstructors()[0];
        Try("out argument pair", () => ShowTwice(() =>
        {
            object?[] args = { null };
            outMethod.Invoke(null, args);
            return args[0];
        }));
        Try("nullable out argument pair", () => ShowTwice(() =>
        {
            object?[] args = { null };
            maybeMethod.Invoke(null, args);
            return args[0];
        }));
        Try("ref return pair", () => ShowTwice(() => refMethod.Invoke(null, null)));
        Try("constructor out argument pair", () => ShowTwice(() =>
        {
            object?[] args = { null };
            ctor.Invoke(args);
            return args[0];
        }));
        Console.WriteLine("reflection route written-back pairs end");
    }

    private static string ShowTwice(Func<object?> invoke)
    {
        object first = invoke()!, second = invoke()!;
        return ((IRouteShow)first).Show() + " equal=" + first.Equals(second)
            + "/" + (first.GetHashCode() == second.GetHashCode());
    }

    internal static void RunSlotRows()
    {
        Console.WriteLine("== reflection route slot-row boxes ==");
        var maker = new SlotMaker();
        Type makerType = typeof(ISlotMaker);
        Try("interface default return", () => ShowTwice(() => makerType.GetMethod("Make")!.Invoke(maker, null)));
        Try("interface default property", () => ShowTwice(() => makerType.GetProperty("Held")!.GetValue(maker)));
        Try("interface default out argument", () =>
        {
            object?[] args = { null };
            makerType.GetMethod("Fill")!.Invoke(maker, args);
            return ((IRouteShow)args[0]!).Show();
        });
        SlotBase derived = new SlotDerived();
        int direct = derived.Carry(5).Value;
        MethodInfo carry = typeof(SlotBase).GetMethod("Carry")!.MakeGenericMethod(typeof(int));
        Try("generic virtual return", () => direct + " " + ShowTwice(() => carry.Invoke(derived, new object[] { 5 })));
        long wrapped = Wrap(6L).Value;
        MethodInfo wrap = typeof(Program).GetMethod("Wrap")!.MakeGenericMethod(typeof(long));
        Try("generic method return", () => wrapped + " " + ShowTwice(() => wrap.Invoke(null, new object[] { 6L })));
        Console.WriteLine("reflection route slot-row boxes end");
    }

    internal static void RunAttributeMinted()
    {
        Console.WriteLine("== reflection route attribute-minted classes ==");
        Type level = typeof(AttrRoot);
        do
        {
            object[] rows = level.GetCustomAttributes(typeof(NextLevelAttribute), false);
            if (rows.Length == 0)
            {
                Console.WriteLine(level.Name + " rows=0");
                break;
            }
            Type next = ((NextLevelAttribute)rows[0]).Next;
            Console.WriteLine(level.Name + " rows=" + rows.Length + " next=" + next.Name);
            level = next;
        }
        while (level != typeof(AttrRoot));
        Console.WriteLine("reflection route attribute-minted classes end");
    }
}
