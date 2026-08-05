using System;
using System.IO;

// System.IO.Directory.CreateDirectory(string) lowered to the
// dn2cpp_directory_create runtime helper (recursive mkdir, idempotent on an
// existing directory). The real BCL body pulls in the Path.GetFullPathInternal ->
// Sys.GetCwd -> ArrayPool -> EventSource -> INumberBase cascade (the largest
// remaining self-host gap subtree), so it is intercepted before real resolution
// AND excluded from reachability (the three-point pattern)..NET returns a
// DirectoryInfo we don't model; every caller (incl. dn2cpp's own code) discards
// it, so the transpiler pushes a null placeholder.
//
// `root` is the bucket's scratch directory (the gate hands each side a fresh
// mktemp dir, so native and real .NET use separate roots); the output prints only
// booleans, never absolute paths, so it diffs exact. Every name created here
// (a/, solo, trail) is this section's own, so the fixture the enumeration
// sections walk is untouched — and this section runs after them regardless.
namespace DirectoryCreateSubset;

internal static class Program
{
    internal static void __GateEntry(string root)
    {
        Console.WriteLine("-- Directory.CreateDirectory --");

        // Recursive: a nested path where NONE of the intermediate parents exist.
        // Every missing parent is created, then the leaf (matching `mkdir -p`).
        // The DirectoryInfo return value is discarded (the dn2cpp-own-code shape).
        string nested = Path.Combine(root, "a", "b", "c");
        Directory.CreateDirectory(nested);
        Console.WriteLine($"leafCreated={Directory.Exists(nested)}");
        Console.WriteLine($"parentA={Directory.Exists(Path.Combine(root, "a"))}");
        Console.WriteLine($"parentB={Directory.Exists(Path.Combine(root, "a", "b"))}");

        // Idempotent: re-creating an existing directory does NOT throw and leaves
        // it in place.
        Directory.CreateDirectory(nested);
        Console.WriteLine($"idempotent={Directory.Exists(nested)}");

        // A single new component under an existing parent.
        string single = Path.Combine(root, "solo");
        Directory.CreateDirectory(single);
        Console.WriteLine($"single={Directory.Exists(single)}");

        // Trailing separator: harmless, still creates the directory.
        string trailing = Path.Combine(root, "trail") + "/";
        Directory.CreateDirectory(trailing);
        Console.WriteLine($"trailing={Directory.Exists(Path.Combine(root, "trail"))}");

        // The created directory is reported by Directory.Exists but not by
        // File.Exists (it is a directory, not a regular file).
        Console.WriteLine($"notAFile={File.Exists(single)}");
    }
}
