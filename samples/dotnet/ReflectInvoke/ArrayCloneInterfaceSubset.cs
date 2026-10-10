using System;
using System.Collections;

namespace ArrayCloneInterfaceSubset;

static class Program
{
    private static object CloneConstrained<T>(T value) where T : ICloneable => value.Clone();

    internal static void RunConstrained()
    {
        int[] source = { 2, 5, 8 };
        Inspect("array clone constrained", source, CloneConstrained(source));
    }

    private static object CloneErased(object value) => ((ICloneable)value).Clone();
    private static Func<object> GroupErased(object value) => ((ICloneable)value).Clone;

    private static void Inspect(string label, int[] source, object result)
    {
        var copy = (int[])result;
        copy[0] = 99;
        Console.WriteLine(label + ": " + !ReferenceEquals(source, copy) + "/"
            + (source.GetType() == copy.GetType()) + "/" + copy.Length + "/"
            + source[0] + "/" + copy[2]);
    }

    internal static void RunTyped()
    {
        int[] source = { 2, 5, 8 };
        ICloneable value = source;
        Inspect("array clone typed", source, value.Clone());
    }

    internal static void RunErased()
    {
        int[] source = { 2, 5, 8 };
        Inspect("array clone erased", source, CloneErased(source));
    }

    internal static void RunInterfaceGroup()
    {
        int[] source = { 2, 5, 8 };
        Func<object> group = GroupErased(source);
        Inspect("array clone interface group", source, group());
    }

    internal static void RunClassGroup()
    {
        int[] source = { 2, 5, 8 };
        Func<object> group = source.Clone;
        Inspect("array clone class group", source, group());
    }

    internal static void RunMd()
    {
        Array source = Array.CreateInstance(typeof(int), new[] { 2, 2 }, new[] { 1, 3 });
        source.SetValue(7, 1, 3);
        var copy = (Array)((ICloneable)source).Clone();
        copy.SetValue(11, 1, 3);
        int sum = 0;
        foreach (object value in (IEnumerable)copy)
            sum += (int)value;
        Console.WriteLine("array clone MD: " + !ReferenceEquals(source, copy) + "/"
            + (source.GetType() == copy.GetType()) + "/" + copy.Rank + "/"
            + copy.GetLowerBound(0) + "/" + copy.Length + "/" + source.GetValue(1, 3) + "/" + sum);
    }

    internal static void RunAliases()
    {
        int[] source = { 2, 5, 8 };
        Array baseReceiver = source;
        Func<object> direct = source.Clone;
        Func<object> viaBase = baseReceiver.Clone;
        Func<object> viaInterface = ((ICloneable)source).Clone;
        Console.WriteLine("array clone aliases: " + (direct == viaBase) + "/" + (direct == viaInterface));
        Console.WriteLine("array clone remove: " + (Delegate.Remove(direct, viaInterface) is null));
        Inspect("array clone alias invoked", source, viaBase());
    }

    internal static void RunNull()
    {
        Array source = null;
        try
        {
            Func<object> clone = source.Clone;
            Console.WriteLine("array clone class null: bound/" + (clone.Target is null));
        }
        catch (Exception e) { Console.WriteLine("array clone class null: " + e.GetType().Name); }
        ICloneable value = null;
        try
        {
            Func<object> clone = value.Clone;
            Console.WriteLine("array clone interface null: bound/" + (clone.Target is null));
        }
        catch (Exception e) { Console.WriteLine("array clone interface null: " + e.GetType().Name); }
    }
    internal static void RunReferences()
    {
        object element = new object();
        object[] source = { element };
        var copy = (object[])((ICloneable)source).Clone();
        bool shallow = ReferenceEquals(element, copy[0]);
        copy[0] = null;
        Console.WriteLine("array clone references: " + !ReferenceEquals(source, copy) + "/"
            + (source.GetType() == copy.GetType()) + "/" + shallow + "/"
            + ReferenceEquals(source[0], element));
    }

    internal static void RunAll()
    {
        Console.WriteLine("== array Clone interface dispatch ==");
        RunTyped();
        RunErased();
        RunConstrained();
        RunInterfaceGroup();
        RunClassGroup();
        ArrayCloneReflectionOnlySubset.Program.Run();
        RunMd();
        RunAliases();
        RunReferences();
        RunNull();
        Console.WriteLine("array Clone interface dispatch end");
    }
}
