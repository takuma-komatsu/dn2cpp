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
