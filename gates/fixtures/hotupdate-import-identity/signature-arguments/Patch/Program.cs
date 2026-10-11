using System;
using System.Globalization;

namespace SignatureArgumentImports;

internal static unsafe class PatchProgram
{
    private static void Report(string label, int first, int second)
    {
        Console.WriteLine(label);
        Console.WriteLine(first);
        Console.WriteLine(second);
    }

    private static void Main()
    {
        Surface.Reset();
        Console.WriteLine("== imported generic signature arguments ==");
        Report("signature method imports", Surface.Read<delegate*<int>[]>(), Surface.Read<delegate*<uint>[]>());
        Report("signature type imports", new Cell<delegate*<int>[]>(31).Read(), new Cell<delegate*<uint>[]>(47).Read());
        Surface.Write<delegate*<int>[]>(31);
        Surface.Write<delegate*<uint>[]>(47);
        Report("signature imports agree", Surface.Read<delegate*<int>[]>(), Surface.Read<delegate*<uint>[]>());
        Report("signature named method imports", Surface.Read<delegate*<Leaf>[]>(), Surface.Read<global::Gfn0_5_Int32EndArr>());
        Report("signature named type imports", new Cell<delegate*<Leaf>[]>(61).Read(), new Cell<global::Gfn0_5_Int32EndArr>(73).Read());
        Surface.Write<delegate*<Leaf>[]>(83);
        Surface.Write<global::Gfn0_5_Int32EndArr>(97);
        Report("signature named imports agree", Surface.Read<delegate*<Leaf>[]>(), Surface.Read<global::Gfn0_5_Int32EndArr>());
        Console.WriteLine("imported generic signature arguments end");

        Console.WriteLine("== imported generic fields ==");
        Console.WriteLine("generic fields before initialization");
        Console.WriteLine(Surface.FieldInitializations);
        Report("generic field defaults", Cell<int>.Value, Cell<string>.Value);
        Console.WriteLine("generic fields initialized once");
        Console.WriteLine(Surface.FieldInitializations);
        Cell<int>.Value = 17;
        Cell<string>.Value = 29;
        Report("generic fixed fields", Cell<int>.Value, Cell<string>.Value);
        Report("generic field method agreement", Surface.Read<int>(), Surface.Read<string>());
        Cell<int>.Item = 41;
        Cell<string>.Item = "generic static item";
        Console.WriteLine("generic typed static fields");
        Console.WriteLine(Cell<int>.Item);
        Console.WriteLine(Cell<string>.Item);
        var ints = new Cell<int>(43);
        var strings = new Cell<string>(53);
        ints.InstanceItem = 67;
        strings.InstanceItem = "generic instance item";
        Console.WriteLine("generic typed instance fields");
        Console.WriteLine(ints.InstanceItem);
        Console.WriteLine(strings.InstanceItem);
        Report("generic instances retain constructor fields", ints.Read(), strings.Read());
        Cell<delegate*<int>[]>.Value = 107;
        Cell<delegate*<uint>[]>.Value = 109;
        Report("generic signature fields", Cell<delegate*<int>[]>.Value, Cell<delegate*<uint>[]>.Value);
        Report("generic signature field method agreement", Surface.Read<delegate*<int>[]>(), Surface.Read<delegate*<uint>[]>());
        Console.WriteLine("generic fields still initialized once");
        Console.WriteLine(Surface.FieldInitializations);
        Console.WriteLine("imported generic fields end");

        Console.WriteLine("== imported managed providers ==");
        IFormatProvider culture = Surface.CultureProvider();
        Surface.ProviderField = Surface.ForwardProvider(culture);
        Console.WriteLine(ReferenceEquals(Surface.ProviderField, culture));
        Console.WriteLine(Surface.ProviderField is CultureInfo);
        IFormatProvider dates = Surface.DateProvider();
        Surface.ProviderField = Surface.ForwardProvider(dates);
        Console.WriteLine(ReferenceEquals(Surface.ProviderField, dates));
        Console.WriteLine(Surface.ProviderField is DateTimeFormatInfo);
        Surface.ProviderField = Surface.ForwardProvider(null);
        Console.WriteLine(Surface.ProviderField is null);
        Console.WriteLine("imported managed providers end");
    }
}
