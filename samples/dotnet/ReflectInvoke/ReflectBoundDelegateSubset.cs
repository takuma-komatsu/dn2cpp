#nullable enable
// SUBJECT: delegates CreateDelegate binds, compared, removed and called on a null
// receiver as .NET does. Bindings of distinct rows over one receiver are distinct
// delegates although the rows' bodies coincide: a generic method's instantiations
// sharing one body, and identical non-virtual and virtual bodies; a generic
// virtual row and its override are one delegate. Delegate.Remove removes the last
// run of entries equal to the removed invocation list, including separately
// created bindings, and leaves the list whole when no run matches. A binding
// closed over null raises NullReferenceException where the body reads its
// receiver: a value type's own and interface bodies, and System.Enum's CompareTo
// against a value, format and type-code bodies, while CompareTo(null) answers 0
// as .NET's reference comparison does. RunNullBoundBodies pins that a body closed
// over null, or bound open and called on null, runs as .NET's does when it never
// dereferences its receiver: a value type's that never loads it, and a reference
// type's that returns, compares or passes it, while a field read, GetType on the
// receiver and a generic dictionary lookup through null fault with
// NullReferenceException, and a synchronized body with Monitor's
// ArgumentNullException. RunRemoveRuns pins Delegate.Remove wherever the last
// equal run sits, through a binding of another row naming the same override,
// that an unmatched removal returns the source delegate itself, and that Remove
// and Combine across delegate types throw ArgumentException. RunNullBoundCalls
// pins that a null receiver passed on through a non-virtual call runs the callee
// as .NET's does, that a method group over null throws as .NET's does, and that
// a generic class's body faults only where .NET's shared code reads its generic
// dictionary off the receiver. RunNullBoundLookups pins that lookup for arguments
// that are value types holding a reference (a box and a constrained call fault,
// an element address and a box only a branch tests do not) and that a body
// passing its receiver only on to non-virtual calls runs the callees as .NET's
// does, a context read under a value-type instantiation and a struct's helpers.
// RunNullBoundOtherReceivers pins that a body only testing its null receiver runs
// as .NET's does beside a non-virtual call on another object.
// RunNullBoundSharedContext pins that one body shared over Int32 and an enum
// reads each type's context while its null receiver remains null.
// RunTemplateAccessors pins that the property accessors of a MakeGenericType
// instantiation the image mints at run time bind and run on that instantiation
// through every CreateDelegate overload and DynamicInvoke: a static accessor bound
// open or over its first argument, an instance one over a receiver or over null,
// and that Delegate.Method, DeclaringType and delegate equality name the
// instantiation, as .NET's do. RunNullBoundChains pins that a null receiver passed
// on through a long chain or a dense cycle of non-virtual calls runs every body it
// reaches as .NET's does, through null-safe, context-only and context-supplied
// bindings alike.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ReflectBoundDelegateSubset;

interface ICounter
{
    int Read();
}

struct Counter : ICounter
{
    public int Value;
    public int Bump() => Value + 1;
    public int Read() => Value;
}

enum Tone { Low, High }

class Twin
{
    public int First() => 1;
    public int Second() => 1;
    public int Shared<T>() => 1;
    public virtual int VirtualFirst() => 2;
    public virtual int VirtualSecond() => 2;
}

class PickBase
{
    public virtual string Pick<T>() => "base";
}

class PickLeaf : PickBase
{
    public override string Pick<T>() => "leaf";
}

struct Gauge
{
    public int Value;
    public int Read() => Value;
    public int Fixed() => 42;
    public string Label(int n) => "n" + n;
}

class Cell
{
    public int X = 7;
    public int Get() => X;
    public virtual int VirtualGet() => X;
    public int Fixed() => 5;
    public virtual int VirtualFixed() => 6;
    public int Forward(int v) => Twice(v) + 1;
    public object Self() => this;
    public bool IsNull() => this == null;
    public int Identity() => RuntimeHelpers.GetHashCode(this);
    public string Kind() => GetType().Name;
    [MethodImpl(MethodImplOptions.Synchronized)]
    public int Locked() => 9;
    private static int Twice(int v) => v * 2;
}

class SubCell : Cell
{
    public override int VirtualFixed() => 60;
}

class Holder<T>
{
    public int Fixed() => 4;
    public List<T> Make() => new List<T>();
}

class Journal
{
    public string Text = "";
    public void A() => Text += "A";
    public void B() => Text += "B";
    public virtual void C() => Text += "C";
}

class Named
{
    public string Log = "";

    public override string ToString()
    {
        Log += "N";
        return "named";
    }

    public string Other()
    {
        Log += "O";
        return "other";
    }
}

class Relay
{
    public int Value = 7;
    public int ViaHelper() => Helper();
    private int Helper() => 42;
    public int ViaFieldHelper() => FieldHelper();
    private int FieldHelper() => Value;
    public int ViaChain() => Middle();
    private int Middle() => Helper() + 1;
    public int ViaRecursion() => Countdown(3);
    private int Countdown(int n) => n == 0 ? 1 : Countdown(n - 1) + 1;
    public int PassesItself() => new Taker().Use(this);
    public int ViaVirtual() => Virtual();
    public virtual int Virtual() => 6;
    public int ViaLiteral() => this == null ? "abc".Length : 0;
    public int ViaStruct() => this == null ? new Gauge().Fixed() : 0;
    public int ViaGenericMethod() => Generic<string>() + Named<string>().Length;
    private int Generic<U>() => 1;
    private string Named<U>() => typeof(U).Name;
    public int ViaIdentityHash() => base.GetHashCode();
    public bool ViaReferenceEquals(object? other) => base.Equals(other);
    public string? ViaObjectToString() => base.ToString();
}

class SubRelay : Relay
{
    public override int Virtual() => base.Virtual() + 1;
}

class Taker
{
    public int Use(object? o) => o == null ? 11 : 12;
}

class Scoped<T>
{
    public static int Counter = 5;
    public int Fixed() => 3;
    public string Name() => typeof(T).Name;
    public string ArrayName() => new T[0].GetType().Name;
    public int Make() => new List<T>().Count;
    public string ViaName() => Name();
    public bool Is(object o) => o is T;
    public object? BoxIt(T v) => v;
    public int ReadStatic() => Counter;
    public int CountInterface(IList<T> l) => l.Count;
    public int CountClass(List<T> l) => l.Count;
    public string? ToText(T v) => v?.ToString();
    public int Bind()
    {
        Func<int> f = Fixed;
        return f is null ? 0 : 1;
    }
}

class Duo<A, B>
{
    public string First() => typeof(A).Name;
    public string Second() => typeof(B).Name;
}

interface ILookupShow
{
    string Show();
}

struct LookupHolder<X> : ILookupShow
{
    public X Item;
    public string Show() => "holder";
}

class LookupShowClass : ILookupShow
{
    public string Show() => "class";
}

class Lookups<T>
{
    public object? BoxIt(T v) => v;
    public string? Text(T v) => v!.ToString();
    public int Hash(T v) => v!.GetHashCode();
    public int Address(T[] items)
    {
        ref T item = ref items[0];
        return 1;
    }
    public int ReadAddress(T[] items)
    {
        ref readonly T item = ref items[0];
        return 2;
    }
    public int IsDefault()
    {
        T value = default!;
        return value == null ? 5 : 6;
    }
    public int IsFormattable(T v) => v is IFormattable ? 7 : 8;
    public int Forward() => Count();
    public int ForwardTwice() => Count() + Forward() + 1;
    private int Count() => new List<T>().Count;
}

class LookupCaller<T> where T : ILookupShow
{
    public string Call(T v) => v.Show();
}

struct Forwarder<T>
{
    public int Value;
    public string Forward() => Name();
    public int ForwardConstant() => Constant(2) + 1;
    public int ForwardRead() => Read();
    private string Name() => typeof(T).Name;
    private int Constant(int n) => 40 + n;
    private int Read() => Value;
}

class SelfTester
{
    public string ArrayName() => (this == null ? "null " : "self ") + new int[0].GetType().Name;
    public int ViaNew() => this == null ? new SelfTester().Kind().Length : 0;
    private string Kind() => GetType().Name;
}

class WideTester<T>
{
    public string ArrayName() => (this == null ? "null " : "self ") + new T[0].GetType().Name;
}

class WideRelay<T>
{
    public string Forward() => Child();
    private string Child() => (this == null ? "null " : "self ") + new T[0].GetType().Name;
}

// Only typeof names this definition, so MakeGenericType mints each instantiation
// at run time and its property accessors are the template's rows.
class MintedAccessors<T>
{
    private readonly string label = "minted";
    public static string Shared => "static:" + typeof(T).Name;
    public static string Sink { set { Program.Sunk = value + ":" + typeof(T).Name; } }
    public string Own => label + ":" + typeof(T).Name;
    public string Kind => typeof(T).Name;
    public int Fixed => 7;
}

// Each hop passes its receiver on through a non-virtual call and nothing else.
class LongRelay
{
    public int Value = 9;
    public int Enter(bool read) => Hop1(read);
    private int Hop1(bool read) => Hop2(read);
    private int Hop2(bool read) => Hop3(read);
    private int Hop3(bool read) => Hop4(read);
    private int Hop4(bool read) => Hop5(read);
    private int Hop5(bool read) => Hop6(read);
    private int Hop6(bool read) => Hop7(read);
    private int Hop7(bool read) => Hop8(read);
    private int Hop8(bool read) => Hop9(read);
    private int Hop9(bool read) => Hop10(read);
    private int Hop10(bool read) => read ? Value : 42;
}

struct LongGauge
{
    public int Value;
    public int Enter() => Hop1();
    private int Hop1() => Hop2();
    private int Hop2() => Hop3();
    private int Hop3() => Hop4();
    private int Hop4() => Hop5();
    private int Hop5() => Hop6();
    private int Hop6() => Hop7();
    private int Hop7() => Hop8();
    private int Hop8() => Hop9();
    private int Hop9() => Hop10();
    private int Hop10() => 7;
}

// Every body passes its receiver to each body of the next layer, and the last
// layer's back to Root, so the calls form one dense cycle.
class DenseRelay
{
    public int Root(int n) => n <= 0 ? 0 : A0(n) + A1(n) + A2(n) + A3(n);
    private int A0(int n) => B0(n) + B1(n) + B2(n) + B3(n);
    private int A1(int n) => B0(n) + B1(n) + B2(n) + B3(n);
    private int A2(int n) => B0(n) + B1(n) + B2(n) + B3(n);
    private int A3(int n) => B0(n) + B1(n) + B2(n) + B3(n);
    private int B0(int n) => C0(n) + C1(n) + C2(n) + C3(n);
    private int B1(int n) => C0(n) + C1(n) + C2(n) + C3(n);
    private int B2(int n) => C0(n) + C1(n) + C2(n) + C3(n);
    private int B3(int n) => C0(n) + C1(n) + C2(n) + C3(n);
    private int C0(int n) => D0(n) + D1(n) + D2(n) + D3(n);
    private int C1(int n) => D0(n) + D1(n) + D2(n) + D3(n);
    private int C2(int n) => D0(n) + D1(n) + D2(n) + D3(n);
    private int C3(int n) => D0(n) + D1(n) + D2(n) + D3(n);
    private int D0(int n) => E0(n) + E1(n) + E2(n) + E3(n);
    private int D1(int n) => E0(n) + E1(n) + E2(n) + E3(n);
    private int D2(int n) => E0(n) + E1(n) + E2(n) + E3(n);
    private int D3(int n) => E0(n) + E1(n) + E2(n) + E3(n);
    private int E0(int n) => F0(n) + F1(n) + F2(n) + F3(n);
    private int E1(int n) => F0(n) + F1(n) + F2(n) + F3(n);
    private int E2(int n) => F0(n) + F1(n) + F2(n) + F3(n);
    private int E3(int n) => F0(n) + F1(n) + F2(n) + F3(n);
    private int F0(int n) => G0(n) + G1(n) + G2(n) + G3(n);
    private int F1(int n) => G0(n) + G1(n) + G2(n) + G3(n);
    private int F2(int n) => G0(n) + G1(n) + G2(n) + G3(n);
    private int F3(int n) => G0(n) + G1(n) + G2(n) + G3(n);
    private int G0(int n) => Root(n - 1) + 1;
    private int G1(int n) => Root(n - 1) + 1;
    private int G2(int n) => Root(n - 1) + 1;
    private int G3(int n) => Root(n - 1) + 1;
}

// Only typeof names this definition, so MakeGenericType mints each instantiation
// at run time and its methods are the template's rows. Each hop only forwards its
// receiver; Kind10 reads the context alone, and Tested10 also tests the receiver.
class MintedChain<T>
{
    public string Kind() => Kind1();
    private string Kind1() => Kind2();
    private string Kind2() => Kind3();
    private string Kind3() => Kind4();
    private string Kind4() => Kind5();
    private string Kind5() => Kind6();
    private string Kind6() => Kind7();
    private string Kind7() => Kind8();
    private string Kind8() => Kind9();
    private string Kind9() => Kind10();
    private string Kind10() => typeof(T).Name;
    public string Tested() => Tested1();
    private string Tested1() => Tested2();
    private string Tested2() => Tested3();
    private string Tested3() => Tested4();
    private string Tested4() => Tested5();
    private string Tested5() => Tested6();
    private string Tested6() => Tested7();
    private string Tested7() => Tested8();
    private string Tested8() => Tested9();
    private string Tested9() => Tested10();
    private string Tested10() => (this == null ? "null " : "self ") + typeof(T).Name;
}

delegate void Signal();

static class Program
{
    internal static string Sunk = "";

    private static void Try(string label, Func<object?> invoke)
    {
        string text;
        try
        {
            text = invoke()?.ToString() ?? "null";
        }
        catch (Exception ex)
        {
            text = ex.GetType().Name;
        }
        Console.WriteLine(label + ": " + text);
    }

    private static T Unbound<T>(MethodInfo row) where T : Delegate =>
        (T)Delegate.CreateDelegate(typeof(T), null, row);

    private static string Replay(Journal journal, Action? chain)
    {
        journal.Text = "";
        chain?.Invoke();
        return journal.Text;
    }

    internal static void Run()
    {
        Console.WriteLine("== bound delegates ==");
        var twin = new Twin();
        Console.WriteLine("direct: " + (twin.First() + twin.Second() + twin.Shared<string>() + twin.Shared<object>()
            + twin.VirtualFirst() + twin.VirtualSecond()));
        Func<MethodInfo, Delegate> bind = row => Delegate.CreateDelegate(typeof(Func<int>), twin, row);
        MethodInfo shared = typeof(Twin).GetMethod("Shared")!;
        Try("distinct instantiations", () =>
            bind(shared.MakeGenericMethod(typeof(string))).Equals(bind(shared.MakeGenericMethod(typeof(object)))));
        Try("distinct identical bodies", () =>
            bind(typeof(Twin).GetMethod("First")!).Equals(bind(typeof(Twin).GetMethod("Second")!)));
        Try("distinct identical virtual bodies", () =>
            bind(typeof(Twin).GetMethod("VirtualFirst")!).Equals(bind(typeof(Twin).GetMethod("VirtualSecond")!)));
        Try("one row twice", () =>
        {
            var first = bind(typeof(Twin).GetMethod("First")!);
            var again = bind(typeof(Twin).GetMethod("First")!);
            return first.Equals(again) + "/" + (first.GetHashCode() == again.GetHashCode());
        });
        Try("set of distinct rows", () =>
        {
            var set = new HashSet<Delegate> { bind(typeof(Twin).GetMethod("First")!), bind(typeof(Twin).GetMethod("Second")!) };
            return set.Count + "/" + set.Contains(bind(typeof(Twin).GetMethod("Second")!));
        });
        var leaf = new PickLeaf();
        Console.WriteLine("direct: " + ((PickBase)leaf).Pick<int>() + "/" + leaf.Pick<int>());
        Try("generic virtual row and override", () =>
        {
            var viaBase = Delegate.CreateDelegate(typeof(Func<string>), leaf,
                typeof(PickBase).GetMethod("Pick")!.MakeGenericMethod(typeof(int)));
            var viaLeaf = Delegate.CreateDelegate(typeof(Func<string>), leaf,
                typeof(PickLeaf).GetMethod("Pick")!.MakeGenericMethod(typeof(int)));
            return viaBase.Equals(viaLeaf) + "/" + (viaBase.GetHashCode() == viaLeaf.GetHashCode());
        });

        var journal = new Journal();
        Func<string, Action> bound = name =>
            (Action)Delegate.CreateDelegate(typeof(Action), journal, typeof(Journal).GetMethod(name)!);
        Try("remove binding", () => Replay(journal, (bound("A") + bound("B")) - bound("A")));
        Try("remove virtual binding", () => Replay(journal, (bound("C") + bound("B")) - bound("C")));
        Try("remove only binding", () => (bound("A") - bound("A")) is null);
        Action a = journal.A, b = journal.B, c = journal.C;
        Try("remove run", () => Replay(journal, (a + b + c) - (b + c)));
        Try("remove inner run", () => Replay(journal, (a + b + c + a) - (b + c)));
        Try("remove absent run", () => Replay(journal, (a + b + c) - (a + c)));
        Try("remove last run", () => Replay(journal, (a + b + a + b) - (a + b)));
        Try("remove whole list", () => ((a + b) - (a + b)) is null);

        var counter = new Counter { Value = 3 };
        Enum tone = Tone.High;
        // The calls put each System.Enum row reflected below in the image.
        Console.WriteLine("direct: " + counter.Bump() + "/" + ((ICounter)counter).Read() + "/" + tone.ToString("D")
            + "/" + tone.GetTypeCode() + "/" + tone.CompareTo(Tone.Low));
        Try("null-bound struct body", () => Unbound<Func<int>>(typeof(Counter).GetMethod("Bump")!)());
        Try("null-bound struct interface body", () => Unbound<Func<int>>(typeof(Counter).GetMethod("Read")!)());
        MethodInfo compareTo = typeof(Enum).GetMethod("CompareTo")!;
        Try("null-bound enum CompareTo, value", () => Unbound<Func<object?, int>>(compareTo)(Tone.Low));
        Try("null-bound enum CompareTo, null", () => Unbound<Func<object?, int>>(compareTo)(null));
        Try("null-bound enum format", () =>
            Unbound<Func<string, string>>(typeof(Enum).GetMethod("ToString", new[] { typeof(string) })!)("D"));
        Try("null-bound enum type code", () => Unbound<Func<TypeCode>>(typeof(Enum).GetMethod("GetTypeCode")!)());
        Console.WriteLine("bound delegates end");
    }

    private static TDelegate Open<TDelegate>(MethodInfo row) where TDelegate : Delegate =>
        (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), row);

    internal static void RunNullBoundBodies()
    {
        Console.WriteLine("== null-bound bodies ==");
        var gauge = new Gauge { Value = 3 };
        var cell = new SubCell();
        var text = new Holder<string>();
        var item = new Holder<object>();
        Console.WriteLine("direct: " + gauge.Read() + gauge.Fixed() + gauge.Label(1) + cell.Get() + cell.VirtualGet()
            + cell.Fixed() + cell.VirtualFixed() + cell.Forward(2) + cell.Self().GetType().Name + cell.IsNull()
            + (cell.Identity() == RuntimeHelpers.GetHashCode(cell)) + cell.Kind() + text.Fixed() + text.Make().Count
            + item.Fixed() + item.Make().Count + cell.Locked());
        Try("null-bound struct field body", () => Unbound<Func<int>>(typeof(Gauge).GetMethod("Read")!)());
        Try("null-bound struct constant body", () => Unbound<Func<int>>(typeof(Gauge).GetMethod("Fixed")!)());
        Try("null-bound struct argument body", () => Unbound<Func<int, string>>(typeof(Gauge).GetMethod("Label")!)(4));
        Try("null-bound class field body", () => Unbound<Func<int>>(typeof(Cell).GetMethod("Get")!)());
        Try("null-bound class virtual field body", () => Unbound<Func<int>>(typeof(Cell).GetMethod("VirtualGet")!)());
        Try("null-bound class constant body", () => Unbound<Func<int>>(typeof(Cell).GetMethod("Fixed")!)());
        Try("null-bound class virtual constant body", () => Unbound<Func<int>>(typeof(Cell).GetMethod("VirtualFixed")!)());
        Try("null-bound class override constant body", () => Unbound<Func<int>>(typeof(SubCell).GetMethod("VirtualFixed")!)());
        Try("null-bound class static call", () => Unbound<Func<int, int>>(typeof(Cell).GetMethod("Forward")!)(3));
        Try("null-bound class receiver returned", () => Unbound<Func<object>>(typeof(Cell).GetMethod("Self")!)());
        Try("null-bound class receiver compared", () => Unbound<Func<bool>>(typeof(Cell).GetMethod("IsNull")!)());
        Try("null-bound class receiver passed", () => Unbound<Func<int>>(typeof(Cell).GetMethod("Identity")!)());
        Try("null-bound class receiver type", () => Unbound<Func<string>>(typeof(Cell).GetMethod("Kind")!)());
        Try("null-bound shared class constant", () => Unbound<Func<int>>(typeof(Holder<string>).GetMethod("Fixed")!)());
        Try("null-bound shared class context", () =>
            Unbound<Func<List<string>>>(typeof(Holder<string>).GetMethod("Make")!)().Count);
        Try("open null field body", () => Open<Func<Cell?, int>>(typeof(Cell).GetMethod("Get")!)(null));
        Try("open null constant body", () => Open<Func<Cell?, int>>(typeof(Cell).GetMethod("Fixed")!)(null));
        Try("open null virtual constant body", () => Open<Func<Cell?, int>>(typeof(Cell).GetMethod("VirtualFixed")!)(null));
        Try("null-bound class synchronized body", () => Unbound<Func<int>>(typeof(Cell).GetMethod("Locked")!)());
        Console.WriteLine("null-bound bodies end");
    }

    private static string Replay(Named named, Func<string>? chain)
    {
        named.Log = "";
        chain?.Invoke();
        return chain is null ? "null" : named.Log;
    }

    // Delegate.Remove wherever the last equal run sits: at the head, as the last of
    // repeated single entries, inside a long list, or through a binding of another
    // row naming the same override; a removed list longer than the source, or not
    // in it, leaves the very source delegate, and a removal or combination across
    // delegate types throws ArgumentException.
    internal static void RunRemoveRuns()
    {
        Console.WriteLine("== remove runs ==");
        var journal = new Journal();
        Action a = journal.A, b = journal.B, c = journal.C;
        Try("remove head run", () => Replay(journal, (a + b + c) - (a + b)));
        Try("remove last repeated entry", () => Replay(journal, (a + b + a + c) - a));
        Try("remove middle entry", () => Replay(journal, (a + b + c) - b));
        Try("remove from single", () => Replay(journal, a - b));
        Try("remove longer list", () =>
        {
            Action ab = a + b;
            return Replay(journal, ab - (a + b + c)) + "/" + ReferenceEquals(ab - (a + b + c), ab);
        });
        Try("remove absent run", () =>
        {
            Action abc = a + b + c;
            return ReferenceEquals(abc - (c + a), abc);
        });
        Action? chain = null;
        for (int i = 0; i < 40; i++)
            chain += i % 3 == 0 ? a : i % 3 == 1 ? b : c;
        Try("long list, last run", () => Replay(journal, chain - (a + b + c)));
        Try("long list, entries left", () => Replay(journal, chain - (a + b + c)).Length);
        Try("long list, source entries", () => Replay(journal, chain).Length);
        var named = new Named();
        Func<string> other = named.Other;
        Func<string> viaObject = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), named,
            typeof(object).GetMethod("ToString")!);
        Func<string> viaNamed = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), named,
            typeof(Named).GetMethod("ToString")!);
        Try("remove Object row binding through its override", () => Replay(named, (viaObject + other + viaObject) - viaNamed));
        Try("remove override binding through the Object row", () => Replay(named, (other + viaNamed) - viaObject));
        Signal signal = journal.A;
        Try("remove across delegate types", () => Delegate.Remove(a, signal) is null);
        Try("combine across delegate types", () => Delegate.Combine(a, signal) is null);
        Try("remove null across delegate types", () => Delegate.Remove(null, signal) is null);
        Console.WriteLine("remove runs end");
    }

    // A null receiver a body passes through a non-virtual call runs the callee as
    // .NET's does: a helper of the class or a base, chained or recursive, and
    // Object's identity hash and reference equality, while a virtual call, the
    // callee's field read and Object.ToString fault. A method group over null
    // raises ArgumentException for an instance method and NullReferenceException
    // for a virtual one, inside a body too. A generic class's body shared over a
    // reference type argument faults where .NET's code looks its generic
    // dictionary up through the receiver: typeof, a new array or List, a type
    // test, a static member and an interface slot, but not a box or a class slot.
    // A value-type instantiation's body runs whatever it names.
    internal static void RunNullBoundCalls()
    {
        Console.WriteLine("== null-bound calls ==");
        var relay = new SubRelay();
        Console.WriteLine("direct: " + relay.ViaHelper() + relay.ViaFieldHelper() + relay.ViaChain()
            + relay.ViaRecursion() + relay.PassesItself() + relay.ViaVirtual() + relay.ViaLiteral()
            + relay.ViaStruct() + relay.ViaGenericMethod() + (relay.ViaIdentityHash() == RuntimeHelpers.GetHashCode(relay))
            + relay.ViaReferenceEquals(relay) + relay.ViaObjectToString());
        foreach (string name in new[] { "ViaHelper", "ViaFieldHelper", "ViaChain", "ViaRecursion", "PassesItself",
            "ViaVirtual", "ViaLiteral", "ViaStruct", "ViaGenericMethod", "ViaIdentityHash" })
            Try("null-bound call, " + name, () => Unbound<Func<int>>(typeof(Relay).GetMethod(name)!)());
        Try("null-bound call, ViaReferenceEquals", () =>
            Unbound<Func<object?, bool>>(typeof(Relay).GetMethod("ViaReferenceEquals")!)(null));
        Try("null-bound call, ViaObjectToString", () =>
            Unbound<Func<string?>>(typeof(Relay).GetMethod("ViaObjectToString")!)());
        Try("null-bound call, base body", () => Unbound<Func<int>>(typeof(SubRelay).GetMethod("Virtual")!)());
        Try("open null call, ViaHelper", () => Open<Func<Relay?, int>>(typeof(Relay).GetMethod("ViaHelper")!)(null));
        Relay? none = null;
        Try("method group over null", () =>
        {
            Func<int> group = none!.ViaHelper;
            return group();
        });
        Try("virtual method group over null", () =>
        {
            Func<int> group = none!.Virtual;
            return group();
        });
        RunScoped("string", new Scoped<string>(), "s", new List<string>());
        RunScoped("object", new Scoped<object>(), new object(), new List<object>());
        RunScoped("int", new Scoped<int>(), 1, new List<int>());
        RunScoped("Tone", new Scoped<Tone>(), Tone.High, new List<Tone>());
        var textDuo = new Duo<int, string>();
        var itemDuo = new Duo<int, object>();
        Console.WriteLine("direct duo: " + textDuo.First() + textDuo.Second() + itemDuo.First() + itemDuo.Second());
        foreach (var duo in new[] { typeof(Duo<int, string>), typeof(Duo<int, object>) })
            foreach (string name in new[] { "First", "Second" })
                Try("null-bound " + duo.Name + "<" + duo.GetGenericArguments()[1].Name + ">, " + name,
                    () => Unbound<Func<string>>(duo.GetMethod(name)!)());
        Try("open null Scoped<string>, Name", () =>
            Open<Func<Scoped<string>?, string>>(typeof(Scoped<string>).GetMethod("Name")!)(null));
        Try("open null Scoped<int>, Name", () =>
            Open<Func<Scoped<int>?, string>>(typeof(Scoped<int>).GetMethod("Name")!)(null));
        Console.WriteLine("null-bound calls end");
    }

    private static void RunScoped<T>(string label, Scoped<T> scoped, T value, List<T> list)
    {
        Console.WriteLine("direct " + label + ": " + scoped.Fixed() + scoped.Name() + scoped.ArrayName() + scoped.Make()
            + scoped.ViaName() + scoped.Is(value!) + scoped.BoxIt(value) + scoped.ReadStatic() + scoped.CountInterface(list)
            + scoped.CountClass(list) + scoped.ToText(value) + scoped.Bind());
        var type = typeof(Scoped<T>);
        string prefix = "null-bound Scoped<" + label + ">, ";
        foreach (string name in new[] { "Fixed", "ReadStatic", "Bind" })
            Try(prefix + name, () => Unbound<Func<int>>(type.GetMethod(name)!)());
        foreach (string name in new[] { "Name", "ArrayName", "ViaName" })
            Try(prefix + name, () => Unbound<Func<string>>(type.GetMethod(name)!)());
        Try(prefix + "Make", () => Unbound<Func<int>>(type.GetMethod("Make")!)());
        Try(prefix + "Is", () => Unbound<Func<object, bool>>(type.GetMethod("Is")!)(value!));
        Try(prefix + "BoxIt", () => Unbound<Func<T, object?>>(type.GetMethod("BoxIt")!)(value));
        Try(prefix + "CountInterface", () => Unbound<Func<IList<T>, int>>(type.GetMethod("CountInterface")!)(list));
        Try(prefix + "CountClass", () => Unbound<Func<List<T>, int>>(type.GetMethod("CountClass")!)(list));
        Try(prefix + "ToText", () => Unbound<Func<T, string?>>(type.GetMethod("ToText")!)(value));
    }

    // A null receiver faults where .NET's code shared over a value type holding a
    // reference looks its generic dictionary up through the receiver: boxing T and a
    // constrained call on T, but not an element address, whose type check a value type
    // skips, nor a box only a branch tests, directly or through a type test. A
    // reference type argument needs no lookup for a box or a constrained call, and
    // does for an element address. A body passing its receiver only on to non-virtual
    // calls runs the callees as .NET's does: a context read under a value-type
    // instantiation, and a struct's helpers.
    internal static void RunNullBoundLookups()
    {
        Console.WriteLine("== null-bound lookups ==");
        RunLookups("KeyValuePair<string,int>", new KeyValuePair<string, int>("a", 1));
        RunLookups("LookupHolder<string>", new LookupHolder<string> { Item = "x" });
        RunLookups("LookupHolder<int>", new LookupHolder<int> { Item = 1 });
        RunLookups("string", "s");
        RunLookups("int", 5);
        RunLookups("Tone", Tone.High);
        Try("null-bound LookupCaller<LookupHolder<string>>", () =>
            Unbound<Func<LookupHolder<string>, string>>(typeof(LookupCaller<LookupHolder<string>>).GetMethod("Call")!)(default));
        Try("null-bound LookupCaller<LookupHolder<int>>", () =>
            Unbound<Func<LookupHolder<int>, string>>(typeof(LookupCaller<LookupHolder<int>>).GetMethod("Call")!)(default));
        Try("null-bound LookupCaller<LookupShowClass>", () =>
            Unbound<Func<LookupShowClass, string>>(typeof(LookupCaller<LookupShowClass>).GetMethod("Call")!)(new LookupShowClass()));
        RunForwarder<int>("int");
        RunForwarder<Tone>("Tone");
        Console.WriteLine("null-bound lookups end");
    }

    private static void RunLookups<T>(string label, T value)
    {
        var lookups = new Lookups<T>();
        Console.WriteLine("direct lookups " + label + ": " + lookups.BoxIt(value) + lookups.Text(value)
            + (lookups.Hash(value) == value!.GetHashCode()) + lookups.Address(new T[1]) + lookups.ReadAddress(new T[1])
            + lookups.IsDefault() + lookups.IsFormattable(value) + lookups.Forward() + lookups.ForwardTwice());
        var type = typeof(Lookups<T>);
        string prefix = "null-bound Lookups<" + label + ">, ";
        Try(prefix + "BoxIt", () => Unbound<Func<T, object?>>(type.GetMethod("BoxIt")!)(value));
        Try(prefix + "Text", () => Unbound<Func<T, string?>>(type.GetMethod("Text")!)(value));
        Try(prefix + "Hash", () => Unbound<Func<T, int>>(type.GetMethod("Hash")!)(value) == value!.GetHashCode());
        Try(prefix + "Address", () => Unbound<Func<T[], int>>(type.GetMethod("Address")!)(new T[1]));
        Try(prefix + "ReadAddress", () => Unbound<Func<T[], int>>(type.GetMethod("ReadAddress")!)(new T[1]));
        Try(prefix + "IsDefault", () => Unbound<Func<int>>(type.GetMethod("IsDefault")!)());
        Try(prefix + "IsFormattable", () => Unbound<Func<T, int>>(type.GetMethod("IsFormattable")!)(value));
        Try(prefix + "Forward", () => Unbound<Func<int>>(type.GetMethod("Forward")!)());
        Try(prefix + "ForwardTwice", () => Unbound<Func<int>>(type.GetMethod("ForwardTwice")!)());
    }

    private static void RunForwarder<T>(string label)
    {
        var forwarder = new Forwarder<T> { Value = 3 };
        Console.WriteLine("direct Forwarder<" + label + ">: " + forwarder.Forward() + forwarder.ForwardConstant()
            + forwarder.ForwardRead());
        var type = typeof(Forwarder<T>);
        string prefix = "null-bound Forwarder<" + label + ">, ";
        Try(prefix + "Forward", () => Unbound<Func<string>>(type.GetMethod("Forward")!)());
        Try(prefix + "ForwardConstant", () => Unbound<Func<int>>(type.GetMethod("ForwardConstant")!)());
        Try(prefix + "ForwardRead", () => Unbound<Func<int>>(type.GetMethod("ForwardRead")!)());
    }

    // A null receiver a body only tests runs the body as .NET's does beside a
    // non-virtual call on another object: GetType on a new array, and a helper of a
    // new instance that would fault on the null receiver, in a class and in a
    // generic class's instantiation over an 8-byte value type.
    internal static void RunNullBoundOtherReceivers()
    {
        Console.WriteLine("== null-bound other receivers ==");
        var tester = new SelfTester();
        Console.WriteLine("direct: " + tester.ArrayName() + tester.ViaNew() + new WideTester<long>().ArrayName());
        Try("null-bound SelfTester, ArrayName", () =>
            Unbound<Func<string>>(typeof(SelfTester).GetMethod("ArrayName")!)());
        Try("null-bound SelfTester, ViaNew", () => Unbound<Func<int>>(typeof(SelfTester).GetMethod("ViaNew")!)());
        Try("null-bound WideTester<long>, ArrayName", () =>
            Unbound<Func<string>>(typeof(WideTester<long>).GetMethod("ArrayName")!)());
        Console.WriteLine("null-bound other receivers end");
    }

    internal static void RunNullBoundSharedContext()
    {
        Console.WriteLine("== null-bound shared context ==");
        Console.WriteLine("direct Int32/Tone: " + new WideTester<int>().ArrayName() + "/"
            + new WideTester<Tone>().ArrayName());
        Try("null-bound shared Int32", () =>
            Unbound<Func<string>>(typeof(WideTester<int>).GetMethod("ArrayName")!)());
        Try("null-bound shared Tone", () =>
            Unbound<Func<string>>(typeof(WideTester<Tone>).GetMethod("ArrayName")!)());
        Try("null-bound shared Int32 again", () =>
            Unbound<Func<string>>(typeof(WideTester<int>).GetMethod("ArrayName")!)());
        Console.WriteLine("direct forwarded Int32/Tone: " + new WideRelay<int>().Forward() + "/"
            + new WideRelay<Tone>().Forward());
        Try("null-bound forwarded Int32", () =>
            Unbound<Func<string>>(typeof(WideRelay<int>).GetMethod("Forward")!)());
        Try("null-bound forwarded Tone", () =>
            Unbound<Func<string>>(typeof(WideRelay<Tone>).GetMethod("Forward")!)());
        Try("null-bound field control", () =>
            Unbound<Func<int>>(typeof(Cell).GetMethod("Get")!)());
        Try("null-bound masked lookup control", () =>
            Unbound<Func<KeyValuePair<string, int>, object?>>(typeof(Lookups<KeyValuePair<string, int>>)
                .GetMethod("BoxIt")!)(new KeyValuePair<string, int>("x", 1)));
        Try("direct null after shared bindings", () =>
        {
            WideTester<int>? none = null;
            return none!.ArrayName();
        });
        Console.WriteLine("null-bound shared context end");
    }

    private static object? Dynamic(Delegate d)
    {
        try
        {
            return d.DynamicInvoke();
        }
        catch (TargetInvocationException ex)
        {
            return "TargetInvocationException/" + ex.InnerException!.GetType().Name;
        }
    }

    internal static void RunTemplateAccessors()
    {
        Console.WriteLine("== template accessor bindings ==");
        var shared = new List<Func<string>>();
        foreach (Type arg in new[] { typeof(int), typeof(string) })
        {
            Type minted = typeof(MintedAccessors<>).MakeGenericType(arg);
            object receiver = Activator.CreateInstance(minted)!;
            MethodInfo get = minted.GetProperty("Shared")!.GetGetMethod()!;
            MethodInfo set = minted.GetProperty("Sink")!.GetSetMethod()!;
            MethodInfo own = minted.GetProperty("Own")!.GetGetMethod()!;
            MethodInfo kind = minted.GetProperty("Kind")!.GetGetMethod()!;
            MethodInfo fixedGet = minted.GetProperty("Fixed")!.GetGetMethod()!;
            string prefix = "template " + arg.Name + ", ";
            Try(prefix + "invoke", () => get.Invoke(null, null));
            Try(prefix + "static", () => ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), get))());
            Try(prefix + "static as object", () => ((Func<object>)Delegate.CreateDelegate(typeof(Func<object>), get))());
            Try(prefix + "static null target", () =>
                ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), null, get))());
            Try(prefix + "static no throw", () =>
                ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), get, false)!)());
            Try(prefix + "MethodInfo static", () => ((Func<string>)get.CreateDelegate(typeof(Func<string>)))());
            Try(prefix + "MethodInfo generic static", () => get.CreateDelegate<Func<string>>()());
            Try(prefix + "dynamic static", () => Dynamic(Delegate.CreateDelegate(typeof(Func<string>), get)));
            Try(prefix + "open setter", () =>
            {
                ((Action<string>)Delegate.CreateDelegate(typeof(Action<string>), set))("open");
                return Sunk;
            });
            Try(prefix + "setter over its argument", () =>
            {
                ((Action)Delegate.CreateDelegate(typeof(Action), "closed", set))();
                return Sunk;
            });
            Try(prefix + "dynamic setter over its argument", () =>
            {
                Dynamic(Delegate.CreateDelegate(typeof(Action), "dynamic", set));
                return Sunk;
            });
            Try(prefix + "closed", () => ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), receiver, own))());
            Try(prefix + "MethodInfo closed", () => ((Func<string>)own.CreateDelegate(typeof(Func<string>), receiver))());
            Try(prefix + "MethodInfo generic closed", () => own.CreateDelegate<Func<string>>(receiver)());
            Try(prefix + "dynamic closed", () => Dynamic(own.CreateDelegate(typeof(Func<string>), receiver)));
            Try(prefix + "closed context", () =>
                ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), receiver, kind))());
            Try(prefix + "other receiver", () =>
                Delegate.CreateDelegate(typeof(Func<string>), new object(), own, false) is null);
            Try(prefix + "null-bound context", () =>
                ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), null, kind))());
            Try(prefix + "null-bound field", () =>
                ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), null, own))());
            Try(prefix + "null-bound constant", () =>
                ((Func<int>)Delegate.CreateDelegate(typeof(Func<int>), null, fixedGet))());
            Try(prefix + "dynamic null-bound context", () =>
                Dynamic(Delegate.CreateDelegate(typeof(Func<string>), null, kind)));
            var first = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), get);
            var second = get.CreateDelegate<Func<string>>();
            shared.Add(first);
            Try(prefix + "method", () => first.Method.Name + "/" + (first.Method == get) + "/"
                + (first.Method.ReflectedType == minted) + "/" + (first.Method.DeclaringType == minted));
            Try(prefix + "method invoke", () => first.Method.Invoke(null, null));
            Try(prefix + "method rebind", () =>
                ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), first.Method))());
            Try(prefix + "declaring", () => (get.DeclaringType == minted) + "/"
                + (minted.GetProperty("Kind")!.DeclaringType == minted) + "/"
                + (minted.GetField("label", BindingFlags.Instance | BindingFlags.NonPublic)!.DeclaringType == minted));
            Try(prefix + "equal", () => first.Equals(second) + "/" + (first.GetHashCode() == second.GetHashCode())
                + "/" + (first == second));
            Try(prefix + "closed equal", () =>
            {
                var a = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), receiver, kind);
                var b = kind.CreateDelegate<Func<string>>(receiver);
                return a.Equals(b) + "/" + (a.GetHashCode() == b.GetHashCode()) + "/" + (a.Method == kind);
            });
            Try(prefix + "null-bound equal", () =>
            {
                var a = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), null, kind);
                var b = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), null, kind);
                return a.Equals(b) + "/" + (a.GetHashCode() == b.GetHashCode());
            });
            Try(prefix + "null-bound method", () =>
                ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), null, kind)).Method.Name);
            Try(prefix + "null-bound non-generic method", () =>
                ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), null,
                    typeof(Cell).GetMethod("Kind")!)).Method.Name);
        }
        Try("template across instantiations", () => shared[0].Equals(shared[1]) + "/" + (shared[0] == shared[1])
            + " " + shared[0]() + " " + shared[1]());
        Console.WriteLine("template accessor bindings end");
    }

    // However long the chain or dense the cycle of non-virtual calls a null receiver
    // is passed on through, every body it reaches runs as .NET's does: a class's and a
    // struct's, a template's that reads only its context, and one that also tests its
    // receiver. A field read at the end of the chain and a context .NET looks up
    // through the receiver fault with NullReferenceException.
    internal static void RunNullBoundChains()
    {
        Console.WriteLine("== null-bound call chains ==");
        var relay = new LongRelay();
        Console.WriteLine("direct: " + relay.Enter(false) + "/" + relay.Enter(true) + "/" + new LongGauge().Enter()
            + "/" + new DenseRelay().Root(1));
        MethodInfo enter = typeof(LongRelay).GetMethod("Enter")!;
        Try("null-bound long chain", () => Unbound<Func<bool, int>>(enter)(false));
        Try("null-bound long chain, field", () => Unbound<Func<bool, int>>(enter)(true));
        Try("open null long chain", () => Open<Func<LongRelay?, bool, int>>(enter)(null, false));
        Try("null-bound struct long chain", () => Unbound<Func<int>>(typeof(LongGauge).GetMethod("Enter")!)());
        Try("null-bound dense cycle", () => Unbound<Func<int, int>>(typeof(DenseRelay).GetMethod("Root")!)(1));
        foreach (Type arg in new[] { typeof(int), typeof(string) })
        {
            Type minted = typeof(MintedChain<>).MakeGenericType(arg);
            object receiver = Activator.CreateInstance(minted)!;
            MethodInfo kind = minted.GetMethod("Kind")!;
            MethodInfo tested = minted.GetMethod("Tested")!;
            string prefix = "template " + arg.Name + " long chain, ";
            Try(prefix + "closed", () =>
                ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), receiver, kind))() + "/"
                + ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), receiver, tested))());
            Try(prefix + "null-bound context", () =>
                ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), null, kind))());
            Try(prefix + "null-bound tested", () =>
                ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), null, tested))());
        }
        Console.WriteLine("null-bound call chains end");
    }
}
