using System;

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
    }
}
