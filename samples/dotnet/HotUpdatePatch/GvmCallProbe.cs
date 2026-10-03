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
    }
}
