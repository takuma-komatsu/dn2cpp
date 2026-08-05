using System;
using System.Runtime.CompilerServices;

namespace ModuleInitSubset;

// Sequence numbers handed out in program order. No static field INITIALIZER here, so
// the type has no .cctor of its own and `s_next` is 0 before anything runs — under
// real .NET's lazy type init and dn2cpp's eager one alike.
internal static class Seq
{
    private static int s_next;

    internal static int Next() => ++s_next;
}

// The registration-hook shape the feature exists for: static state a module
// initializer sets up before anything can observe it. Its own initializer (`"<unset>"`)
// is what a type initializer would have left, so the printed value says which of the
// two ran last.
internal static class Registry
{
    internal static string Registered = "<unset>";
}

// [ModuleInitializer] (C# 9). The compiler emits NO call to Init() anywhere it can be
// reached from: it hangs the method off the `<Module>` pseudo-type's .cctor, which the
// CLR runs before the first access to any type in the module — hence before Main. The
// attribute itself carries no runtime behavior, so a transpiler that drops the
// `<Module>` row drops the initializer with it, and a call-graph tree-shaker can never
// notice: the program simply runs with the initializer's side effect missing. That is
// what this section pins — that it ran, and that it ran before the entry point.
internal static class Program
{
    internal static int InitSeq = -1;
    internal static int MainSeq = -1;

    [ModuleInitializer]
    internal static void Init()
    {
        InitSeq = Seq.Next();
        Registry.Registered = "by-module-initializer";
    }

    // Called on the FIRST line of the bucket's Main, so the sequence numbers order the
    // module initializer against the entry point itself rather than against a section
    // body that runs later anyway.
    internal static void MarkMainEntered() => MainSeq = Seq.Next();

    internal static void Run()
    {
        Console.WriteLine($"moduleInit.ran={InitSeq > 0}");
        Console.WriteLine($"moduleInit.beforeMain={InitSeq > 0 && MainSeq > 0 && InitSeq < MainSeq}");
        Console.WriteLine($"moduleInit.seq={InitSeq} main.seq={MainSeq}");
        // The initializer's write must be the surviving one: it runs after the type
        // initializer that set "<unset>", because touching Registry.Registered from
        // inside Init() is itself the first access that triggers that type's .cctor.
        Console.WriteLine($"moduleInit.registry={Registry.Registered}");

        // Making `<Module>` a real type (it is what holds the .cctor that ran Init) must
        // not make it a VISIBLE one: .NET does not report the pseudo-type from
        // Assembly.GetTypes(), though Type.GetType does resolve it. Pin both halves — the
        // list itself is not printed, because a native build reports only the
        // AOT-preserved subset of types, which is a deliberate divergence.
        bool inGetTypes = false;
        foreach (Type ty in typeof(Program).Assembly.GetTypes())
            if (ty.Name == "<Module>")
                inGetTypes = true;
        Console.WriteLine($"moduleInit.inGetTypes={inGetTypes}");
        Console.WriteLine($"moduleInit.resolvable={Type.GetType("<Module>") is not null}");
    }
}
