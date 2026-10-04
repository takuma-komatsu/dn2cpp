using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LdftnLocalSubset;

class VirtualBase
{
    public virtual int Scale(int value) => value * 2;
}

sealed class VirtualDerived : VirtualBase
{
    public override int Scale(int value) => value * 3;
}

sealed class InstanceHolder
{
    public int Delta;

    public int Offset(int value) => value + Delta;
}

interface ISealedScale
{
    sealed int Scale(int value) => value + 100;
    sealed int Shift<T>(int value) => value + 200;
}

// Virtuals of the sealed members' signatures, which a call through the interface never runs.
class SealedScaleHolder : ISealedScale
{
    public virtual int Scale(int value) => value * 3;
    public virtual int Shift<T>(int value) => value * 4;
}

// The fixture binds these differently named bodies to Object's virtual slots.
class ObjectMethodImpl
{
    public virtual string Render() => "alias";
    public virtual bool Same(object other) => other is ObjectMethodImpl;
    public virtual int Hash() => 701;
}

class ObjectMethodImplDerived : ObjectMethodImpl
{
    public override string Render() => "derived";
    public override bool Same(object other) => other is ObjectMethodImplDerived;
    public override int Hash() => 907;
}

class ObjectMethodImplHider : ObjectMethodImpl
{
    public new virtual string Render() => "hidden";
    public new virtual bool Same(object other) => false;
    public new virtual int Hash() => 999;
}

class ObjectMethodImplGeneric<T> : ObjectMethodImpl
{
    public override string Render() => "generic";
    public override bool Same(object other) => other is ObjectMethodImpl;
    public override int Hash() => 1103;
}

class RenamedSlotBase
{
    public virtual int Scale(int value) => value + 1;
    public virtual int Shift(int value) => value + 3;
}

// The fixture renames this override to Rescale and binds it to RenamedSlotBase.Scale's
// slot with a MethodImpl, as VB's Implements and raw IL may.
class RenamedSlotOverride : RenamedSlotBase
{
    public override int Scale(int value) => value + 2;
}

interface IRenamedFiller
{
    int Measure();
}

// The fixture moves the interface slot from the explicit stub to Weigh with a
// MethodImpl, as VB's Implements may.
class RenamedFillerImpl : IRenamedFiller
{
    public virtual int Weigh() => 42;
    int IRenamedFiller.Measure() => -1;
}

class RenamedFillerDerived : RenamedFillerImpl
{
    public override int Weigh() => 43;
}

// The fixture renames this override to Show and binds it to Object.ToString's slot.
class RenamedObjectReuse
{
    public override string ToString() => "reuse";
}

// Only typeof names these definitions, so MakeGenericType mints each instantiation
// from a runtime template. The fixture moves RenamedFillerBox's interface slot from
// the explicit stub to Weigh.
class RenamedFillerBox<T> : IRenamedFiller
{
    public virtual int Weigh() => 44;
    int IRenamedFiller.Measure() => -1;
}

class RenamedFillerInheritBox<T> : RenamedFillerImpl { }

class RenamedFillerOverrideBox<T> : RenamedFillerImpl
{
    public override int Weigh() => 45;
}

class ObjectMethodImplBox<T> : ObjectMethodImpl
{
    public override string Render() => "box";
    public override int Hash() => 1201;
}

class ObjectMethodImplPlainBox<T> : ObjectMethodImpl { }

interface IRenamedSource<out T>
{
    T Take();
}

// The fixture moves the IRenamedSource<string> slot from the explicit stub to Fetch,
// which a binding through the variant IRenamedSource<object> reaches.
class RenamedSource : IRenamedSource<string>
{
    public virtual string Fetch() => "source";
    string IRenamedSource<string>.Take() => "stub";
}

// After Build, gates/fixtures/ldftn-local/Program.cs replaces each throwing stub's
// body with IL that C# cannot express.
sealed class MemberRefTarget
{
    internal int Delta;
    internal static int Select(int value) => value + 20;
    internal static string Select(string value) => value + ":body";
    internal int Shift(int value) => value + Delta;
    internal static T Echo<T>(T value) => value;
}

static class MemberRefBox<T>
{
    internal static T Read(T value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static T ThroughDefinition(T value) => throw new InvalidOperationException();
}

static class Program
{
    static int Add(int value) => value + 7;
    static int Subtract(int value) => value - 3;
    static string Decorate(string prefix, string value) => prefix + value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TypeDefInt(int value) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string TypeDefString(string value) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TypeDefInstance(MemberRefTarget receiver, int value) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TypeDefGeneric(int value) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> Stored() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> NopSeparated() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> NativeConvert() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> SnapshotBeforeOverwrite() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> Selected(bool first) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> StackJoin(bool first) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<string, string> ClosedStored() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RawCalli() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> DeadOrigins() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> VirtualStored(VirtualBase receiver) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> InstanceStored(InstanceHolder holder) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> Int64Stored() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> SealedInterface(ISealedScale receiver) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> SealedGenericInterface(ISealedScale receiver) => throw new InvalidOperationException();

    // ldvirtftn of the source delegate's own Invoke, which C# emits as ldftn.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Func<int, int> InvokeVirtualLoad(Func<int, int> source) => throw new InvalidOperationException();

    // callvirt System.ValueType::Equals/GetHashCode/ToString on the receiver: C# names
    // Object's declaration instead.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ValueTypeEquals(ValueType receiver, object other) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueTypeHash(ValueType receiver) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string ValueTypeText(ValueType receiver) => throw new InvalidOperationException();

    // Roslyn takes these locals' addresses in a body that also creates a delegate.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static string AddressTakenBesideDelegate()
    {
        IntPtr.TryParse("42", out IntPtr handle);
        long.TryParse("9", out long count);
        Func<int, int> add = Add;
        return handle.ToString() + "/" + count + "/" + add(5) + "/" + add.Method.Name;
    }

    internal static void RunTypeDefMemberRefs()
    {
        Console.WriteLine("== same-module TypeDef MemberRefs ==");
        Console.WriteLine("typedef overloads=" + TypeDefInt(5) + "/" + TypeDefString("x"));
        Console.WriteLine("typedef instance=" + TypeDefInstance(new MemberRefTarget { Delta = 10 }, 5));
        Console.WriteLine("typedef generic method=" + TypeDefGeneric(9));
        Console.WriteLine("typedef generic owner=" + MemberRefBox<int>.ThroughDefinition(31)
            + "/" + MemberRefBox<string>.ThroughDefinition("owner"));
        Console.WriteLine("same-module TypeDef MemberRefs end");
    }

    public static void Run()
    {
        Console.WriteLine("ldftn-local-begin");
        var stored = Stored();
        Console.WriteLine("ldftn-local-direct=" + stored(5) + "/" + stored.Method.Name);
        var separated = NopSeparated();
        Console.WriteLine("ldftn-local-nop=" + separated(5) + "/" + separated.Method.Name);
        var converted = NativeConvert();
        Console.WriteLine("ldftn-local-conv=" + converted(5) + "/" + converted.Method.Name);
        var snapshot = SnapshotBeforeOverwrite();
        Console.WriteLine("ldftn-local-snapshot=" + snapshot(5) + "/" + snapshot.Method.Name);
        var first = Selected(true);
        var second = Selected(false);
        Console.WriteLine("ldftn-local-selected=" + first(5) + "/" + first.Method.Name
            + "/" + second(5) + "/" + second.Method.Name);
        var stackFirst = StackJoin(true);
        var stackSecond = StackJoin(false);
        Console.WriteLine("ldftn-local-stack-join=" + stackFirst(5) + "/" + stackFirst.Method.Name
            + "/" + stackSecond(5) + "/" + stackSecond.Method.Name);
        var closed = ClosedStored();
        Console.WriteLine("ldftn-local-closed=" + closed("x") + "/" + closed.Method.Name);
        Console.WriteLine("ldftn-local-calli=" + RawCalli());
        var dead = DeadOrigins();
        Console.WriteLine("ldftn-local-dead-origin=" + dead(5) + "/" + dead.Method.Name);
        var virtualStored = VirtualStored(new VirtualDerived());
        Console.WriteLine("ldftn-local-virtual=" + virtualStored(5) + "/"
            + virtualStored.Method.DeclaringType.Name + "." + virtualStored.Method.Name);
        var instance = InstanceStored(new InstanceHolder { Delta = 10 });
        Console.WriteLine("ldftn-local-instance=" + instance(5) + "/" + instance.Method.Name);
        var wide = Int64Stored();
        Console.WriteLine("ldftn-local-int64=" + wide(5) + "/" + wide.Method.Name);
        Console.WriteLine("ldftn-local-address-taken=" + AddressTakenBesideDelegate());
        Console.WriteLine("ldftn-local-end");
    }

    public static void RunSealedInterface()
    {
        var sealedHolder = new SealedScaleHolder();
        var sealedPlain = SealedInterface(sealedHolder);
        var sealedGeneric = SealedGenericInterface(sealedHolder);
        Console.WriteLine("ldftn-local-sealed-interface=" + sealedPlain(5) + "/"
            + sealedPlain.Method.DeclaringType.Name + "." + sealedPlain.Method.Name + "/" + sealedGeneric(5) + "/"
            + sealedGeneric.Method.DeclaringType.Name + "." + sealedGeneric.Method.Name);
    }

    static string NullFault(Func<object> call)
    {
        try
        {
            return call().ToString();
        }
        catch (NullReferenceException)
        {
            return "NRE";
        }
    }

    // A callvirt of System.ValueType's overrides checks its receiver like any callvirt.
    public static void RunValueTypeReceivers()
    {
        Console.WriteLine("ldftn-local-valuetype-null=" + NullFault(() => ValueTypeEquals(null, 1)) + "/"
            + NullFault(() => ValueTypeEquals(null, null)) + "/" + NullFault(() => ValueTypeHash(null)) + "/"
            + NullFault(() => ValueTypeText(null)));
        Console.WriteLine("ldftn-local-valuetype-boxed=" + ValueTypeEquals(5, 5) + "/" + ValueTypeEquals(5, 6) + "/"
            + ValueTypeHash(5) + "/" + ValueTypeText(5));
    }

    static void ObjectMethodImplCase(string label, ObjectMethodImpl receiver)
    {
        Console.WriteLine("object-methodimpl-body-" + label + "=" + receiver.Render()
            + "/" + receiver.Same(receiver) + "/" + receiver.Same(null) + "/" + receiver.Hash());
        object boxed = receiver;
        Func<string> text = boxed.ToString;
        Func<object, bool> same = boxed.Equals;
        Func<int> hash = boxed.GetHashCode;
        Console.WriteLine("object-methodimpl-" + label + "=" + receiver.Render() + "/" + text()
            + "/" + same(receiver) + "/" + same(new ObjectMethodImplDerived()) + "/" + same(null) + "/" + hash());
        Console.WriteLine("object-methodimpl-call-" + label + "=" + boxed.ToString()
            + "/" + boxed.Equals(receiver) + "/" + boxed.Equals(new ObjectMethodImplDerived())
            + "/" + boxed.Equals(null) + "/" + boxed.GetHashCode());
    }

    public static void RunObjectMethodImpl()
    {
        Console.WriteLine("== Object slots with MethodImpl bodies ==");
        ObjectMethodImplCase("base", new ObjectMethodImpl());
        ObjectMethodImplCase("derived", new ObjectMethodImplDerived());
        ObjectMethodImplCase("hider", new ObjectMethodImplHider());
        ObjectMethodImplCase("generic-string", new ObjectMethodImplGeneric<string>());
        ObjectMethodImplCase("generic-object", new ObjectMethodImplGeneric<object>());
        Console.WriteLine("Object slots with MethodImpl bodies end");
    }

    // A binding through a slot and one through the renamed body filling it are one
    // delegate, so they hash alike and deduplicate.
    public static void RunRenamedSlotBindings()
    {
        Console.WriteLine("== reflection-bound delegates over a renamed override ==");
        RenamedSlotBase receiver = new RenamedSlotOverride();
        var slot = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), receiver,
            typeof(RenamedSlotBase).GetMethod("Scale"));
        var body = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), receiver,
            typeof(RenamedSlotOverride).GetMethod("Rescale"));
        var other = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), receiver,
            typeof(RenamedSlotBase).GetMethod("Shift"));
        var set = new HashSet<Func<int, int>> { slot, body };
        var map = new Dictionary<Delegate, string> { [slot] = "slot" };
        Console.WriteLine("renamed-slot-calls=" + receiver.Scale(1) + "/" + slot(1) + "/" + body(1) + "/" + other(1));
        Console.WriteLine("renamed-slot-identity=" + slot.Equals(body) + "/" + body.Equals(slot) + "/"
            + (slot.GetHashCode() == body.GetHashCode()) + "/" + slot.Equals(other) + "/" + slot.Method.Name + "/"
            + body.Method.Name);
        Console.WriteLine("renamed-slot-dedup=" + set.Count + "/" + set.Contains(body) + "/" + map.ContainsKey(body));
        var chainSlot = (Func<int, int>)Delegate.Combine(slot, other);
        var chainBody = (Func<int, int>)Delegate.Combine(body, other);
        Console.WriteLine("renamed-slot-chain=" + chainSlot.Equals(chainBody) + "/"
            + (chainSlot.GetHashCode() == chainBody.GetHashCode()));
        Console.WriteLine("reflection-bound delegates over a renamed override end");
    }

    static string Name(MethodInfo method) => method.DeclaringType.Name + "." + method.Name;

    static string TextFiller(object receiver, MethodInfo slot, MethodInfo body)
    {
        var viaSlot = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), receiver, slot);
        var viaBody = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), receiver, body);
        var set = new HashSet<Delegate> { viaSlot, viaBody };
        return viaSlot.Equals(viaBody) + "/" + viaBody.Equals(viaSlot) + "/"
            + (viaSlot.GetHashCode() == viaBody.GetHashCode()) + "/" + set.Count + "/" + viaSlot() + "/" + viaBody()
            + "/" + Name(viaSlot.Method) + "/" + Name(viaBody.Method);
    }

    static string NumberFiller(object receiver, MethodInfo slot, MethodInfo body)
    {
        var viaSlot = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), receiver, slot);
        var viaBody = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), receiver, body);
        var set = new HashSet<Delegate> { viaSlot, viaBody };
        return viaSlot.Equals(viaBody) + "/" + viaBody.Equals(viaSlot) + "/"
            + (viaSlot.GetHashCode() == viaBody.GetHashCode()) + "/" + set.Count + "/" + viaSlot() + "/" + viaBody()
            + "/" + Name(viaSlot.Method) + "/" + Name(viaBody.Method);
    }

    // A slot whose filler is a differently named MethodImpl body, or an override of
    // one, binds that body: a delegate through the slot and one through the body are
    // one delegate, and Delegate.Method names the body, as does a delegate over an
    // Object virtual loaded with ldvirtftn. That holds on a MakeGenericType
    // instantiation and through a variant instantiation of the slot's interface.
    public static void RunRenamedSlotFillers()
    {
        Console.WriteLine("== renamed slot fillers ==");
        IRenamedFiller filled = new RenamedFillerImpl();
        IRenamedFiller inherited = new RenamedFillerDerived();
        Func<int> measured = inherited.Measure;
        Console.WriteLine("renamed-filler-calls=" + filled.Measure() + "/" + inherited.Measure() + "/"
            + new RenamedObjectReuse() + "/" + Name(measured.Method));
        var measure = typeof(IRenamedFiller).GetMethod("Measure");
        var weigh = typeof(RenamedFillerImpl).GetMethod("Weigh");
        Console.WriteLine("renamed-filler-interface=" + NumberFiller(new RenamedFillerImpl(), measure, weigh));
        Console.WriteLine("renamed-filler-interface-inherited=" + NumberFiller(new RenamedFillerDerived(), measure, weigh));
        Console.WriteLine("renamed-filler-interface-override=" + NumberFiller(new RenamedFillerDerived(), measure,
            typeof(RenamedFillerDerived).GetMethod("Weigh")));
        var toString = typeof(object).GetMethod("ToString");
        var hash = typeof(object).GetMethod("GetHashCode");
        var render = typeof(ObjectMethodImpl).GetMethod("Render");
        var hashBody = typeof(ObjectMethodImpl).GetMethod("Hash");
        foreach (var (label, receiver) in new (string, ObjectMethodImpl)[]
            {
                ("base", new ObjectMethodImpl()), ("derived", new ObjectMethodImplDerived()),
                ("hider", new ObjectMethodImplHider()), ("generic", new ObjectMethodImplGeneric<string>()),
            })
        {
            Console.WriteLine("renamed-filler-object-" + label + "=" + TextFiller(receiver, toString, render) + "|"
                + NumberFiller(receiver, hash, hashBody));
            object boxed = receiver;
            Func<string> text = boxed.ToString;
            Func<int> code = boxed.GetHashCode;
            Console.WriteLine("renamed-filler-object-load-" + label + "=" + Name(text.Method) + "/" + Name(code.Method)
                + "/" + text() + "/" + code());
        }
        Console.WriteLine("renamed-filler-reuse=" + TextFiller(new RenamedObjectReuse(), toString,
            typeof(RenamedObjectReuse).GetMethod("Show")));
        object box = Mint(typeof(RenamedFillerBox<>));
        object inheritBox = Mint(typeof(RenamedFillerInheritBox<>));
        object overrideBox = Mint(typeof(RenamedFillerOverrideBox<>));
        Console.WriteLine("renamed-filler-minted=" + NumberFiller(box, measure, box.GetType().GetMethod("Weigh")) + "|"
            + NumberFiller(inheritBox, measure, weigh) + "|" + NumberFiller(overrideBox, measure, weigh));
        object objectBox = Mint(typeof(ObjectMethodImplBox<>));
        object plainBox = Mint(typeof(ObjectMethodImplPlainBox<>));
        Console.WriteLine("renamed-filler-minted-object=" + TextFiller(objectBox, toString, render) + "|"
            + NumberFiller(objectBox, hash, hashBody) + "|" + TextFiller(plainBox, toString, render));
        Func<string> boxText = objectBox.ToString;
        Func<string> plainText = plainBox.ToString;
        Func<int> boxMeasure = ((IRenamedFiller)box).Measure;
        Func<int> inheritMeasure = ((IRenamedFiller)inheritBox).Measure;
        Console.WriteLine("renamed-filler-minted-load=" + Name(boxText.Method) + "/" + Name(plainText.Method) + "/"
            + Name(boxMeasure.Method) + "/" + Name(inheritMeasure.Method));
        var take = typeof(IRenamedSource<object>).GetMethod("Take");
        var exactTake = typeof(IRenamedSource<string>).GetMethod("Take");
        var fetch = typeof(RenamedSource).GetMethod("Fetch");
        Console.WriteLine("renamed-filler-variant=" + SourceFiller(new RenamedSource(), take, fetch) + "|"
            + SourceFiller(new RenamedSource(), take, exactTake));
        IRenamedSource<object> source = new RenamedSource();
        Func<object> taken = source.Take;
        Console.WriteLine("renamed-filler-variant-load=" + taken() + "/" + Name(taken.Method));
        Console.WriteLine("renamed slot fillers end");
    }

    static object Mint(Type definition) => Activator.CreateInstance(definition.MakeGenericType(typeof(string)));

    static string SourceFiller(object receiver, MethodInfo slot, MethodInfo body)
    {
        var viaSlot = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), receiver, slot);
        var viaBody = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), receiver, body);
        var set = new HashSet<Delegate> { viaSlot, viaBody };
        return viaSlot.Equals(viaBody) + "/" + viaBody.Equals(viaSlot) + "/"
            + (viaSlot.GetHashCode() == viaBody.GetHashCode()) + "/" + set.Count + "/" + viaSlot() + "/" + viaBody()
            + "/" + Name(viaSlot.Method) + "/" + Name(viaBody.Method);
    }
}
