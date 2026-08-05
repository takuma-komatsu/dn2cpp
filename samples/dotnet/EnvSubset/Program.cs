using System;
using System.IO;
using System.Globalization;

// System.Environment + System.IO.Directory lowered to the dn2cpp_env_*
// / dn2cpp_directory_* helpers (getenv / getcwd / chdir / stat). The real BCL bodies
// pull in the same file-I/O / globalization cascade File/Path do, so they are
// intercepted before real resolution AND excluded from reachability (the
// three-point pattern). args[0] is a caller-supplied scratch directory (the gate
// makes a fresh one per run, so native and real.NET use separate dirs); the output
// prints only env-var values + booleans, never absolute paths, so it diffs exact.
// The gate exports DN2CPP_PROBE (set) and DN2CPP_EMPTY (empty) to both runs and
// leaves DN2CPP_UNSET undefined. CoreLib only (no Linq shim).
internal static class Program
{
    static int Main(string[] args)
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        string dir = args[0];

        // GetEnvironmentVariable: a set value, an explicitly-empty value, and an
        // undefined variable (null, never empty string).
        Console.WriteLine($"probe=[{Environment.GetEnvironmentVariable("DN2CPP_PROBE")}]");
        Console.WriteLine($"empty=[{Environment.GetEnvironmentVariable("DN2CPP_EMPTY")}]");
        string unset = Environment.GetEnvironmentVariable("DN2CPP_UNSET");
        Console.WriteLine($"unsetNull={unset is null}");

        // GetEnvironmentVariable(name, EnvironmentVariableTarget) — the OTHER overload, and
        // the one the intercept could not lower. Only the one-argument form has a
        // dn2cpp_env_* helper, so this one is NOT intercepted and transpiles from its real
        // BCL body. That used to be impossible: the intercept was routed by method NAME
        // (which cut the reachability edge to the real body) while the lowering matched on
        // the SIGNATURE (so this overload found no lowering, with nothing left to fall back
        // on) — a hard transpile failure. The file even disagreed with itself, since the
        // intra-CoreLib MethodDefinition arm had gated it by shape all along.
        //
        // Its real body is transpilable, which is why falling through is the right remedy:
        // the Process arm tail-calls the one-argument form — the lowered one, so the cascade
        // the cut exists to avoid stays cut — and User/Machine read a registry that does not
        // exist on Unix, i.e. a body that returns null.
        Console.WriteLine($"probeProc=[{Environment.GetEnvironmentVariable("DN2CPP_PROBE", EnvironmentVariableTarget.Process)}]");
        Console.WriteLine($"emptyProc=[{Environment.GetEnvironmentVariable("DN2CPP_EMPTY", EnvironmentVariableTarget.Process)}]");
        Console.WriteLine($"unsetProcNull={Environment.GetEnvironmentVariable("DN2CPP_UNSET", EnvironmentVariableTarget.Process) is null}");
        // The two overloads are one operation: the Process target must agree exactly with
        // the lowered one-argument lane, or the fall-through has changed the meaning.
        Console.WriteLine("procVsDefaultAgree="
            + (Environment.GetEnvironmentVariable("DN2CPP_PROBE", EnvironmentVariableTarget.Process)
                   == Environment.GetEnvironmentVariable("DN2CPP_PROBE")
               && Environment.GetEnvironmentVariable("DN2CPP_EMPTY", EnvironmentVariableTarget.Process)
                   == Environment.GetEnvironmentVariable("DN2CPP_EMPTY")));
        // User/Machine have no registry on Unix: null, even for a variable that IS set in
        // the process environment.
        Console.WriteLine($"userNull={Environment.GetEnvironmentVariable("DN2CPP_PROBE", EnvironmentVariableTarget.User) is null}");
        Console.WriteLine($"machineNull={Environment.GetEnvironmentVariable("DN2CPP_PROBE", EnvironmentVariableTarget.Machine) is null}");
        // An undefined target is rejected by the real body's ValidateTarget. Print the
        // exception TYPE, not its message (the message is resource-string territory).
        try
        {
            Environment.GetEnvironmentVariable("DN2CPP_PROBE", (EnvironmentVariableTarget)99);
            Console.WriteLine("badTarget=noexc");
        }
        catch (ArgumentOutOfRangeException) { Console.WriteLine("badTarget=ArgumentOutOfRangeException"); }

        // CurrentDirectory getter == Directory.GetCurrentDirectory (both getcwd).
        string orig = Environment.CurrentDirectory;
        Console.WriteLine($"cwdGettersAgree={orig == Directory.GetCurrentDirectory()}");

        // Directory.Exists: true only for a directory, false for a file / missing /
        // empty path (never throws).
        Console.WriteLine($"existsDir={Directory.Exists(dir)}");
        string f = Path.Combine(dir, "f.txt");
        File.WriteAllText(f, "x");
        Console.WriteLine($"existsFile={Directory.Exists(f)}");
        Console.WriteLine($"existsMissing={Directory.Exists(Path.Combine(dir, "nope"))}");
        Console.WriteLine($"existsEmpty={Directory.Exists("")}");

        // SetCurrentDirectory: chdir into the scratch dir, confirm both getters track
        // it and it is itself an existing directory, then restore. Absolute paths
        // differ per run, so we print only booleans.
        Environment.CurrentDirectory = dir;
        string now = Directory.GetCurrentDirectory();
        Console.WriteLine($"setChanged={now != orig}");
        Console.WriteLine($"settersAgree={Environment.CurrentDirectory == now}");
        Console.WriteLine($"cwdExists={Directory.Exists(now)}");

        Directory.SetCurrentDirectory(orig);
        Console.WriteLine($"restored={Directory.GetCurrentDirectory() == orig}");

        // getcwd's OWN failure. Nothing a caller passes selects it — the working
        // directory has to go away underneath the process — which is exactly why it
        // must throw rather than abort: the program that hits it did nothing wrong.
        // Create a throwaway directory, enter it, remove it, then ask the two
        // surfaces that call getcwd: Path.GetFullPath on a RELATIVE path (the
        // relative arm prepends the cwd) and Directory.GetCurrentDirectory. Real
        // .NET picks the exception type from errno, and a removed directory gives
        // ENOENT, so both answer FileNotFoundException rather than a flat IOException.
        //
        // POSIX only, and both sides skip together: on Windows a directory that
        // is a live process's working directory cannot be removed at all, and
        // Path.GetFullPath goes through GetFullPathNameW rather than getcwd
        // there. The gate diffs native against real .NET on the SAME host, so
        // the guard below takes the same branch on both sides.
        string victim = Path.Combine(dir, "gone");
        Directory.CreateDirectory(victim);
        Directory.SetCurrentDirectory(victim);
        bool removed;
        try
        {
            Directory.Delete(victim);
            removed = true;
        }
        catch (IOException)
        {
            removed = false;
        }
        Console.WriteLine($"cwdRemoved={removed}");
        if (removed)
        {
            try
            {
                string r = Path.GetFullPath("rel");
                Console.WriteLine($"relFullPath=noexc:{r.Length > 0}");
            }
            catch (Exception e) { Console.WriteLine($"relFullPath={e.GetType().Name}"); }

            try
            {
                string r = Directory.GetCurrentDirectory();
                Console.WriteLine($"deadCwd=noexc:{r.Length > 0}");
            }
            catch (Exception e) { Console.WriteLine($"deadCwd={e.GetType().Name}"); }

            // The DERIVED type is what makes a narrow catch match at all, but the
            // base one has to keep matching or code written against a plain
            // IOException stops catching. Both are asserted.
            bool asIo = false;
            try { string r = Path.GetFullPath("rel"); }
            catch (IOException) { asIo = true; }
            Console.WriteLine($"caughtAsIOException={asIo}");

            // An ABSOLUTE path never consults the cwd, so it still resolves: the
            // fault belongs to the environment, not to the surface.
            string abs = Path.GetFullPath(Path.Combine(dir, "x", "..", "y"));
            Console.WriteLine($"absoluteStillWorks={abs.Length > 0 && abs.EndsWith("y")}");
        }
        Directory.SetCurrentDirectory(orig);
        Console.WriteLine($"restoredAfterCwdFault={Directory.GetCurrentDirectory() == orig}");

        // Environment.Exit: terminates with the given code, having flushed what was
        // already written, and never reaches the line below. The gate asserts the
        // process exit status, not just the output.
        Console.WriteLine("exiting");
        Environment.Exit(3);
        Console.WriteLine("UNREACHABLE");
        return 0;
    }
}
