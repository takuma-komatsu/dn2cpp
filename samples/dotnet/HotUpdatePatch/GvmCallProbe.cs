using System;
using HotUpdateBase;

namespace HotGvmCall;

// After Build, gates/fixtures/hotupdate-nonvirtual-call/Program.cs turns every
// callvirt in NonVirtual into call, which C# never emits for these receivers.
// .NET then runs the named row's own body whatever the receiver. A call of an
// abstract row is bad IL, which .NET raises as it compiles the calling method
// and the interpreter as the call executes; each such call here starts its
// method, so both raise it before any effect.
public static class NonVirtual
{
    public static string Hold(IBin bin)
    {
        return bin.Hold<int>(5);
    }

    public static string Name(IBin bin)
    {
        return bin.Name();
    }

    public static string Tag(ITagger tagger)
    {
        return tagger.Tag<int>(6);
    }

    public static string Mark(Stamp stamp)
    {
        return stamp.Mark<int>(7);
    }

    public static string Describe(IDescribable item)
    {
        return item.Describe();
    }

    public static long Count(AbstractRecord record)
    {
        return record.Count(1, 2L);
    }

    public static int Body(AbstractRecord record)
    {
        return record.Body(5);
    }

    public static int ReadField(AbstractRecord record)
    {
        return record.ReadField();
    }

    internal static string Cold(IOverriddenBin bin)
    {
        return bin.Cold();
    }

    internal static string ColdHold(IOverriddenBin bin)
    {
        return bin.ColdHold<int>(9);
    }

    public static string StringTypeIdentity(Holder<string> holder)
    {
        return holder.TypeIdentity();
    }

    public static string ObjectTypeIdentity(Holder<object> holder)
    {
        return holder.TypeIdentity();
    }

    public static string IntTypeIdentity(Holder<int> holder)
    {
        return holder.TypeIdentity();
    }

    public static string Shape(Holder<string> holder)
    {
        return holder.Shape();
    }

    public static string ForwardStringTypeIdentity(Holder<string> holder)
    {
        return holder.ForwardTypeIdentity();
    }

    public static string EnteredStringTypeIdentity(Holder<string> holder)
    {
        return holder.EnteredTypeIdentity();
    }

    public static int StringArrayLength(Holder<string> holder)
    {
        return holder.ArrayLength();
    }

    public static int ObjectArrayLength(Holder<object> holder)
    {
        return holder.ArrayLength();
    }

    public static int IntArrayLength(Holder<int> holder)
    {
        return holder.ArrayLength();
    }

    public static int IntArrayLengthTwice(Holder<int> holder)
    {
        return holder.ArrayLengthTwice();
    }

    public static int MixedArrayLengths(PairHolder<int, string> holder)
    {
        return holder.ArrayLengths();
    }

    public static int StringStaticArrayLength(Holder<string> holder)
    {
        return holder.ForwardStaticArrayLength();
    }

    public static int IntStaticArrayLength(Holder<int> holder)
    {
        return holder.ForwardStaticArrayLength();
    }

    public static string StringPredicates(Holder<string> holder) => holder.TypePredicates();
    public static string ObjectPredicates(Holder<object> holder) => holder.TypePredicates();
    public static string IntPredicates(Holder<int> holder) => holder.TypePredicates();
    public static string EnumPredicates(Holder<PredicateTone> holder) => holder.TypePredicates();
    public static bool StringIsClass(Holder<string> holder) => holder.IsClass();
    public static bool StringIsByRef(Holder<string> holder) => holder.IsByRef();
    public static bool StringIsPointer(Holder<string> holder) => holder.IsPointer();
    public static bool TryValueType(Holder<string> holder, bool branch) => holder.TryValueType(branch);
    public static bool StringMethodValueType(Counter counter) => counter.MethodValueType<string>();
    public static bool IntMethodValueType(Counter counter) => counter.MethodValueType<int>();

    public static string OwnerPredicates(Holder<string> holder) => holder.OwnerPredicates();
    public static string NopPredicates(Holder<string> holder) => holder.NopPredicates();
    public static bool OwnerIsClass(Holder<string> holder) => holder.OwnerIsClass();
    public static bool ArrayPredicate(Holder<string> holder, int kind) => holder.ArrayPredicate(kind);
    public static bool CellPredicate(Holder<string> holder, int kind) => holder.CellPredicate(kind);
    public static bool NopBeforeClass(Holder<string> holder) => holder.NopBeforeClass();

}

internal sealed class PatchRecord : AotRecord
{
    public override long Count(int value, long offset) => value + offset + 200;
    public override string Text(string value) => "patch:" + value;
    public override AbstractRecord Identity(AbstractRecord value) => value;
    public override int Body(int value) => value + 80;
}

internal static class Program
{
    private static void Main()
    {
        PredicateBindings.PinInvariantCulture();
        Console.WriteLine("== non-virtual calls of virtual imports ==");
        IBin glass = new GlassBin();
        Console.WriteLine(NonVirtual.Hold(new Bin()));
        Console.WriteLine(NonVirtual.Hold(glass));
        Console.WriteLine(NonVirtual.Name(glass));
        // The same imports reached by callvirt dispatch on the receiver.
        Console.WriteLine(glass.Hold<int>(8));
        Console.WriteLine(glass.Name());
        try
        {
            Console.WriteLine(NonVirtual.Tag(new Crate()));
        }
        catch (Exception e)
        {
            Console.WriteLine("tag: " + e.GetType().FullName);
        }
        try
        {
            Console.WriteLine(NonVirtual.Mark(new InkStamp()));
        }
        catch (Exception e)
        {
            Console.WriteLine("mark: " + e.GetType().FullName);
        }
        try
        {
            Console.WriteLine(NonVirtual.Describe(new Badge("plain", 1)));
        }
        catch (Exception e)
        {
            Console.WriteLine("describe: " + e.GetType().FullName);
        }
        Console.WriteLine("== non-virtual calls of virtual imports end ==");
        Console.WriteLine("== abstract class method imports ==");
        RunAbstract("aot", new AotRecord());
        RunAbstract("patch", new PatchRecord());
        try
        {
            AbstractRecord missing = null!;
            Console.WriteLine(missing.Count(1, 2L));
        }
        catch (Exception fault)
        {
            Console.WriteLine("abstract null: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine(NonVirtual.Count(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("abstract direct null: " + AbstractRecord.FaultIdentity(fault));
        }
        Console.WriteLine("abstract class method imports end");
        Console.WriteLine("== null receivers of instance body calls ==");
        try
        {
            Console.WriteLine("class call");
            Console.WriteLine(NonVirtual.Body(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("class call: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine("interface call=" + NonVirtual.Name(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("interface call: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine("generic interface call=" + NonVirtual.Hold(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("generic interface call: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            AbstractRecord missing = null!;
            Console.WriteLine(missing.Body(5));
        }
        catch (Exception fault)
        {
            Console.WriteLine("class callvirt: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            IBin missing = null!;
            Console.WriteLine(missing.Name());
        }
        catch (Exception fault)
        {
            Console.WriteLine("interface callvirt: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            IBin missing = null!;
            Console.WriteLine(missing.Hold<int>(5));
        }
        catch (Exception fault)
        {
            Console.WriteLine("generic interface callvirt: " + AbstractRecord.FaultIdentity(fault));
        }
        Console.WriteLine("field read");
        Console.WriteLine(NonVirtual.ReadField(new AotRecord()));
        try
        {
            Console.WriteLine(NonVirtual.ReadField(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("field read call: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            AbstractRecord missing = null!;
            Console.WriteLine(missing.ReadField());
        }
        catch (Exception fault)
        {
            Console.WriteLine("field read callvirt: " + AbstractRecord.FaultIdentity(fault));
        }
        Console.WriteLine("null receivers of instance body calls end");
        Console.WriteLine("== null receivers of shared generic bodies ==");
        try
        {
            Console.WriteLine("generic shape=" + NonVirtual.Shape(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("generic shape: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine("int identity=" + NonVirtual.IntTypeIdentity(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("int identity: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine("string identity=" + NonVirtual.StringTypeIdentity(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("string identity: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine("object identity=" + NonVirtual.ObjectTypeIdentity(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("object identity: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine(NonVirtual.ForwardStringTypeIdentity(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("forward identity: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine(NonVirtual.EnteredStringTypeIdentity(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("entered identity: " + AbstractRecord.FaultIdentity(fault));
        }
        Console.WriteLine("string array length");
        Console.WriteLine(NonVirtual.StringArrayLength(new Holder<string>()));
        Console.WriteLine("object array length");
        Console.WriteLine(NonVirtual.ObjectArrayLength(new Holder<object>()));
        try
        {
            Console.WriteLine(NonVirtual.StringArrayLength(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("string array: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine(NonVirtual.ObjectArrayLength(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("object array: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Holder<string> missing = null!;
            Console.WriteLine(missing.ArrayLength());
        }
        catch (Exception fault)
        {
            Console.WriteLine("array callvirt: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine("int null array length");
            Console.WriteLine(NonVirtual.IntArrayLength(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("int array: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine("int null array lengths");
            Console.WriteLine(NonVirtual.IntArrayLengthTwice(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("int array twice: " + AbstractRecord.FaultIdentity(fault));
        }
        Console.WriteLine("mixed array lengths");
        Console.WriteLine(NonVirtual.MixedArrayLengths(new PairHolder<int, string>()));
        try
        {
            Console.WriteLine(NonVirtual.MixedArrayLengths(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("mixed arrays: " + AbstractRecord.FaultIdentity(fault));
        }
        Console.WriteLine("null receivers of shared generic bodies end");
        Console.WriteLine("== overridden default interface bodies ==");
        IOverriddenBin overridden = new GlassBin();
        try
        {
            Console.WriteLine("plain call=" + NonVirtual.Cold(overridden));
        }
        catch (Exception fault)
        {
            Console.WriteLine("plain call: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine("generic call=" + NonVirtual.ColdHold(overridden));
        }
        catch (Exception fault)
        {
            Console.WriteLine("generic call: " + AbstractRecord.FaultIdentity(fault));
        }
        Console.WriteLine("plain callvirt=" + overridden.Cold());
        Console.WriteLine("generic callvirt=" + overridden.ColdHold<int>(9));
        try
        {
            Console.WriteLine("plain null call=" + NonVirtual.Cold(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("plain null call: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine("generic null call=" + NonVirtual.ColdHold(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("generic null call: " + AbstractRecord.FaultIdentity(fault));
        }
        Console.WriteLine("overridden default interface bodies end");
        Console.WriteLine("== context passed to static generic bodies ==");
        Console.WriteLine("static string forward length");
        Console.WriteLine(NonVirtual.StringStaticArrayLength(new Holder<string>()));
        try
        {
            Console.WriteLine(NonVirtual.StringStaticArrayLength(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("static string forward: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine("static int null forward length");
            Console.WriteLine(NonVirtual.IntStaticArrayLength(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("static int forward: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Holder<string> missing = null!;
            Console.WriteLine(missing.ForwardStaticArrayLength());
        }
        catch (Exception fault)
        {
            Console.WriteLine("static forward callvirt: " + AbstractRecord.FaultIdentity(fault));
        }
        Console.WriteLine("context passed to static generic bodies end");
        RunPredicates();
        RunPredicateWindows();
    }

    private static void RunPredicates()
    {
        Console.WriteLine("== folded generic type predicates ==");
        Console.WriteLine("string positive=" + NonVirtual.StringPredicates(new Holder<string>()));
        Console.WriteLine("string null=" + NonVirtual.StringPredicates(null!));
        Console.WriteLine("object null=" + NonVirtual.ObjectPredicates(null!));
        Console.WriteLine("int null=" + NonVirtual.IntPredicates(null!));
        Console.WriteLine("cell null=" + PredicateBindings.CellPredicates());
        Console.WriteLine("enum null=" + NonVirtual.EnumPredicates(null!));
        Console.WriteLine("null-bound predicates=" + Holder<string>.NullBoundPredicates());
        Console.WriteLine("try false=" + (NonVirtual.TryValueType(null!, false) ? "True" : "False"));
        Console.WriteLine("try true=" + (NonVirtual.TryValueType(null!, true) ? "True" : "False"));
        Console.WriteLine("method string=" + (NonVirtual.StringMethodValueType(null!) ? "True" : "False"));
        Console.WriteLine("method int=" + (NonVirtual.IntMethodValueType(null!) ? "True" : "False"));
        try
        {
            Holder<string> missing = null!;
            Console.WriteLine(missing.IsValueType());
        }
        catch (Exception fault)
        {
            Console.WriteLine("predicate callvirt: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine(NonVirtual.StringIsClass(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("class predicate: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine(NonVirtual.StringIsByRef(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("byref predicate: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine(NonVirtual.StringIsPointer(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("pointer predicate: " + AbstractRecord.FaultIdentity(fault));
        }
        Console.WriteLine("folded generic type predicates end");
    }

    private static void RunPredicateWindows()
    {
        Console.WriteLine("== generic predicate operand windows ==");
        Console.WriteLine("owner positive=" + NonVirtual.OwnerPredicates(new Holder<string>()));
        Console.WriteLine("owner null=" + NonVirtual.OwnerPredicates(null!));
        Console.WriteLine("null-bound owner=" + Holder<string>.NullBoundOwnerPredicates());
        Console.WriteLine("nop positive=" + NonVirtual.NopPredicates(new Holder<string>()));
        Console.WriteLine("nop null=" + NonVirtual.NopPredicates(null!));
        Console.WriteLine("null-bound nop=" + (Holder<string>.NullBoundNopValue() ? "True" : "False"));
        Console.WriteLine("array positive=" + (NonVirtual.ArrayPredicate(new Holder<string>(), 0) ? "True" : "False"));
        Console.WriteLine("cell positive=" + (NonVirtual.CellPredicate(new Holder<string>(), 0) ? "True" : "False"));
        for (int kind = 0; kind < 4; kind++)
        {
            string kindName = kind == 0 ? "0" : kind == 1 ? "1" : kind == 2 ? "2" : "3";
            try
            {
                Console.WriteLine(NonVirtual.ArrayPredicate(null!, kind));
            }
            catch (Exception fault)
            {
                Console.WriteLine("array predicate " + kindName + ": " + AbstractRecord.FaultIdentity(fault));
            }
            try
            {
                Console.WriteLine(NonVirtual.CellPredicate(null!, kind));
            }
            catch (Exception fault)
            {
                Console.WriteLine("cell predicate " + kindName + ": " + AbstractRecord.FaultIdentity(fault));
            }
        }
        try
        {
            Holder<string> missing = null!;
            Console.WriteLine(missing.NopBeforeValue());
        }
        catch (Exception fault)
        {
            Console.WriteLine("nop callvirt: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine(NonVirtual.OwnerIsClass(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("owner class predicate: " + AbstractRecord.FaultIdentity(fault));
        }
        try
        {
            Console.WriteLine(NonVirtual.NopBeforeClass(null!));
        }
        catch (Exception fault)
        {
            Console.WriteLine("nop class predicate: " + AbstractRecord.FaultIdentity(fault));
        }
        Console.WriteLine("generic predicate operand windows end");
    }

    private static void RunAbstract(string label, AbstractRecord record)
    {
        Console.WriteLine(label + " abstract count");
        Console.WriteLine(record.Count(3, 4294967296L));
        Console.WriteLine(label + " text=" + record.Text("value"));
        Console.WriteLine(label + " identity");
        Console.WriteLine(record == record.Identity(record));
        Console.WriteLine(label + " body");
        Console.WriteLine(record.Body(5));
        Console.WriteLine(NonVirtual.Body(record));
        try
        {
            Console.WriteLine(NonVirtual.Count(record));
        }
        catch (Exception fault)
        {
            Console.WriteLine(label + " abstract direct: " + AbstractRecord.FaultIdentity(fault));
        }
    }
}
