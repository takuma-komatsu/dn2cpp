using System;

namespace ArgsEntrypointSubset;

// `static int Main(string[] args)` — the transpiler emits
// `int main(int argc, char** argv)`, builds the managed args array from
// argv[1..] (the program name argv[0] is excluded, matching.NET), and passes
// it in. Exercises Length, per-element values + lengths (UTF-8 round trip),
// indexing, string.Join, and a runtime type check that the array carries the
// precise String[] type-info. Diffed exact vs real.NET with the same argv.
//
// The bucket also owns what the emitted `main()` does BEFORE it calls the managed entry
// point — see ModuleInitSubset.cs, which pins that a [ModuleInitializer] runs there, and
// FailedCctorSubset.cs, which pins what that prologue does with a type initializer that
// THROWS (it must not kill the process, and the type must stay uninitialized forever).
internal static class Program
{
    static int Main(string[] args)
    {
        // FIRST statement: stamps the entry point's place in the startup sequence, so the
        // module-initializer section can assert the initializer ran BEFORE Main rather than
        // merely before its own body (which runs later anyway).
        ModuleInitSubset.Program.MarkMainEntered();

        Console.WriteLine($"count={args.Length}");

        object boxed = args;
        Console.WriteLine($"isStringArray={boxed is string[]}");
        Console.WriteLine($"isObjectArray={boxed is object[]}");
        Console.WriteLine($"typeName={args.GetType().Name}");

        for (int i = 0; i < args.Length; i++)
            Console.WriteLine($"[{i}]={args[i]} (len {args[i].Length})");

        Console.WriteLine("joined=" + string.Join("|", args));

        if (args.Length > 0)
            Console.WriteLine($"firstUpper={args[0].ToUpperInvariant()}");
        else
            Console.WriteLine("no-args");

        ModuleInitSubset.Program.Run();
        FailedCctorSubset.Program.Run();

        return 0;
    }
}
