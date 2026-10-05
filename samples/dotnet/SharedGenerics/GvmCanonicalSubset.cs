#nullable enable
// The generic-virtual dispatcher's emit-set invariant, over this sample's OWN
// types: System.Linq is shaped the same way, but upstream may reshape it.
//
// A generic virtual has no vtable slot, so a callvirt routes through a
// dn2cpp_gvm_* type-switch over the ALLOCATED implementing types. Under shared
// generics that set contains canonical group OWNERS, which carry no ti_ of their
// own — no instance ever wears a canonical type-info, since the shared ctor stamps
// the real one out of an rgctx ClassAlloc slot. Naming an owner's ti_ leaves the
// transpile green and fails the C++ compile on an undeclared identifier.
//
// Three things must hold at once, or this compiles a dispatcher and asserts
// nothing:
//   1. The GVM is reached through an INTERFACE, so the call site is slotless and
//      a dispatcher is minted at all.
//   2. Its implementation is declared on a base whose closed form does NOT
//      mention the grouped type parameter (ChainBase<TElement>). Declared on
//      Chain<TElement,TKey> instead, the owner gets an impl nothing reaches and
//      the case is filtered out before any type-info is named.
//   3. Something ALLOCATES the owner. A newobj in a shared body does not — it
//      stamps the real type-info from an rgctx slot — but REACHABILITY does, by
//      resolving that newobj token under the canonical context. Hence Clone().
//
// The element type is a STRUCT on purpose: a reference element would collapse
// TElement into $CnRef and the group would key differently.
using System;
using System.Collections.Generic;
namespace GvmCanonicalSubset;

internal readonly struct Row
{
    internal Row(string module, string entryPoint, int order)
    {
        Module = module;
        EntryPoint = entryPoint;
        Order = order;
    }

    internal string Module { get; }
    internal string EntryPoint { get; }
    internal int Order { get; }
}

internal interface IChain<TElement>
{
    // The generic virtual: slotless, so every call on it is a callvirt through
    // this interface, which is what mints the dispatcher.
    IChain<TElement> Then<TNext>(Func<TElement, TNext> selector);

    string Render(TElement element);
}

internal abstract class ChainBase<TElement> : IChain<TElement>
{
    // Declared HERE, not on Chain<TElement,TKey>: header, point 2.
    public IChain<TElement> Then<TNext>(Func<TElement, TNext> selector)
        => new Chain<TElement, TNext>(this, selector, "t");

    public abstract string Render(TElement element);
}

internal sealed class Chain<TElement, TKey> : ChainBase<TElement>
{
    private readonly IChain<TElement>? _prev;
    private readonly Func<TElement, TKey> _selector;
    private readonly string _label;

    internal Chain(IChain<TElement>? prev, Func<TElement, TKey> selector, string label)
    {
        _prev = prev;
        _selector = selector;
        _label = label;
    }

    // Non-generic and taint-free, so it compiles ONCE for the group and reaching
    // its IL marks the OWNER allocated: header, point 3.
    internal Chain<TElement, TKey> Clone() => new Chain<TElement, TKey>(_prev, _selector, _label);

    public override string Render(TElement element)
    {
        string head = _prev is null ? "" : _prev.Render(element) + "|";
        return head + _label + "=" + _selector(element)!.ToString();
    }
}

internal enum TrialWidth : int { Value = 17 }

internal interface ITrial
{
    string InterfaceVisit<T>(T value);
}

internal class TrialBase
{
    public virtual string ClassVisit<T>(T value) => "base:" + typeof(T).Name;
}

internal sealed class TrialLeaf : TrialBase, ITrial
{
    public override string ClassVisit<T>(T value) => "leaf:" + typeof(T).Name;
    public string InterfaceVisit<T>(T value) => "interface:" + typeof(T).Name;
}

internal interface IOwnedTrial<T>
{
    string Owned<U>(U value);
}

internal sealed class OwnedTrial<T> : IOwnedTrial<T>
{
    public string Owned<U>(U value) => "owned:" + typeof(T).Name + "/" + typeof(U).Name;
}

internal static class Program
{
    private static string InterfaceTrial<T>(ITrial receiver, T value) => receiver.InterfaceVisit<T>(value);
    private static string ClassTrial<T>(TrialBase receiver, T value) => receiver.ClassVisit<T>(value);

    private static string BoundTrial<T>(TrialBase receiver, T value)
    {
        Func<T, string> bound = receiver.ClassVisit<T>;
        return bound(value);
    }

    private static string NestedTrial<T>(ITrial receiver, T value) => receiver.InterfaceVisit<T[]>(new[] { value });

    // The caller fixes the GVM method argument while its owner type varies.
    private static string ConcreteMethodTrial<T>(IOwnedTrial<T> receiver) => receiver.Owned<int>(23);

    internal static void RunTrialDispatch()
    {
        Console.WriteLine("== shared trial generic virtual dispatch ==");
        TrialBase instance = new TrialLeaf();
        ITrial viaInterface = (ITrial)instance;
        Console.WriteLine("trial interface reference=" + InterfaceTrial(viaInterface, "s")
            + "/" + InterfaceTrial<object>(viaInterface, "o"));
        Console.WriteLine("trial interface width=" + InterfaceTrial(viaInterface, 17)
            + "/" + InterfaceTrial(viaInterface, TrialWidth.Value) + "/" + InterfaceTrial(viaInterface, 29L));
        Console.WriteLine("trial class reference=" + ClassTrial(instance, "s")
            + "/" + ClassTrial<object>(instance, "o"));
        Console.WriteLine("trial class width=" + ClassTrial(instance, 17)
            + "/" + ClassTrial(instance, TrialWidth.Value) + "/" + ClassTrial(instance, 29L));
        Console.WriteLine("trial bound reference=" + BoundTrial(instance, "s")
            + "/" + BoundTrial<object>(instance, "o"));
        Console.WriteLine("trial bound width=" + BoundTrial(instance, 17) + "/" + BoundTrial(instance, 29L));
        Console.WriteLine("trial nested=" + NestedTrial(viaInterface, "s") + "/" + NestedTrial(viaInterface, 17));
        Console.WriteLine("trial concrete method args=" + ConcreteMethodTrial<string>(new OwnedTrial<string>())
            + "/" + ConcreteMethodTrial<object>(new OwnedTrial<object>()));
        Console.WriteLine("shared trial generic virtual dispatch end");
    }

    internal static void RunInterfaceRows()
    {
        Console.WriteLine("== signature-only interface rows ==");
        var method = typeof(IChain<Row>).GetMethod(nameof(IChain<Row>.Render))!;
        Console.WriteLine("interface row name=" + method.Name);
        Console.WriteLine("interface row abstract=" + method.IsAbstract);
        Console.WriteLine("interface row owner=" + (method.DeclaringType == typeof(IChain<Row>)));
        Console.WriteLine("signature-only interface rows end");
    }

    private static List<Row> Rows() => new List<Row>
    {
        new Row("b", "y", 2),
        new Row("a", "z", 1),
        new Row("a", "y", 3),
    };

    internal static void __GateEntry()
    {
        // Reference-typed key: groups under Chain<Row,$CnRef>, and the Clone is
        // what allocates that owner.
        IChain<Row> byModule = new Chain<Row, string>(null, r => r.Module, "mod").Clone();
        // Value-typed key, one group over: Chain<Row,$CnInt32>.
        IChain<Row> byOrder = new Chain<Row, int>(byModule, r => r.Order, "ord").Clone();

        // Two dispatches, at a reference and a value method type argument. A
        // dispatcher that dropped its REAL cases with the dead canonical one
        // would trap here instead of chaining.
        IChain<Row> byEntry = byOrder.Then<string>(r => r.EntryPoint);
        IChain<Row> byBucket = byEntry.Then<int>(r => r.Order % 2);

        foreach (Row r in Rows())
            Console.WriteLine("gvm " + byBucket.Render(r));

        // A different element type mints a SECOND dispatcher, so the case filter
        // is exercised per-dispatcher, not once in whichever is emitted first.
        IChain<int> byParity = new Chain<int, string>(null, v => "p" + (v % 2), "par").Clone();
        Console.WriteLine("gvm int " + byParity.Then<int>(v => v * 3).Render(7));
    }
}
