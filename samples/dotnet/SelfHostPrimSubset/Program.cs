using System;
using System.Collections.Generic;
using System.IO;

// Console self-host runtime primitives, exercised exactly: the deterministic
// HashCode seed (every line here is hash-value-independent), an un-thrown
// exception's null StackTrace, and the process-image paths (asserted by property).
internal static class Program
{
    private static void Main()
    {
        // Dictionary keyed on ValueTuple<string,string>: enumeration is insertion
        // order (no removals here), so the output is seed-independent.
        var d = new Dictionary<(string, string), string>();
        d[("a", "b")] = "ab";
        d[("c", "d")] = "cd";
        d[("e", "f")] = "ef";
        d[("a", "b")] = "AB"; // overwrite keeps the original insertion slot

        Console.WriteLine("count " + d.Count);
        Console.WriteLine("idx " + d[("c", "d")]);
        Console.WriteLine("has-ab " + d.ContainsKey(("a", "b")));
        Console.WriteLine("has-xy " + d.ContainsKey(("x", "y")));
        Console.WriteLine("try-ef " + (d.TryGetValue(("e", "f"), out var v1) ? v1 : "<none>"));
        Console.WriteLine("try-zz " + (d.TryGetValue(("z", "z"), out var v2) ? v2 : "<none>"));
        foreach (var kv in d)
            Console.WriteLine("kv " + kv.Key.Item1 + "/" + kv.Key.Item2 + "=" + kv.Value);

        // HashSet: Count and membership only, no enumeration-order dependency.
        var set = new HashSet<(string, string)>();
        set.Add(("p", "q"));
        set.Add(("r", "s"));
        set.Add(("p", "q")); // duplicate
        Console.WriteLine("set-count " + set.Count);
        Console.WriteLine("set-has-pq " + set.Contains(("p", "q")));
        Console.WriteLine("set-has-tt " + set.Contains(("t", "t")));

        // An un-thrown exception has a null StackTrace in both runtimes. A thrown
        // one diverges (native has no stack-trace model) and is not exercised.
        var ex = new InvalidOperationException("boom");
        Console.WriteLine("st-null " + (ex.StackTrace is null));
        Console.WriteLine("msg " + ex.Message);

        // The process image. Properties only: under `dotnet app.dll` the base
        // directory is the dll's and ProcessPath is the host's.
        string baseDir = AppContext.BaseDirectory;
        Console.WriteLine("base-exists " + Directory.Exists(baseDir));
        Console.WriteLine("base-rooted " + Path.IsPathRooted(baseDir));
        Console.WriteLine("base-trailing-sep " + Path.EndsInDirectorySeparator(baseDir));

        string? procPath = Environment.ProcessPath;
        Console.WriteLine("proc-nonnull " + (procPath is not null));
        Console.WriteLine("proc-exists " + File.Exists(procPath));
        Console.WriteLine("proc-rooted " + Path.IsPathRooted(procPath));

        // Driven last so nothing it does can perturb the sections above.
        MiscIntrinsicSubset.Program.__GateEntry();
    }
}
