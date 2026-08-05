using System;

namespace HotUpdateGodotBadPatchMathf;

// A deliberately out-of-surface patch: Mathf is a placeholder-bodied shim
// (its calls are lowered inline by the transpiler, the emitted body is dead
// `return default` code), so a hot-update base image leaves it un-invokable
// and this import must fail at BPI load as a standard unresolved import.
// Before that exclusion the bind silently succeeded and Sqrt returned 0 —
// the "silent default" marker below must therefore never print (nor the ok
// one: the entry must not run at all).
internal static class Program
{
    private static void Main()
    {
        Console.WriteLine(Godot.Mathf.Sqrt(4.0) == 2.0
            ? "patch: mathf ok"
            : "patch: mathf silent default");
    }
}
