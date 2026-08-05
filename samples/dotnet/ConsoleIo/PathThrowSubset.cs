#nullable disable
using System;
using System.IO;

namespace PathThrowSubset
{
    // The Path validation faults, observed from a catch handler. A path
    // assembled from configuration or user input is untrusted text like any
    // other, so `try { Path.Combine(dir, name) } catch (ArgumentNullException)`
    // is a normal shape and must not end the process.
    //
    // Combine's null and GetFullPath's null are ArgumentNullException;
    // GetFullPath("") is ArgumentException — a distinction the runtime's one
    // combined null-or-empty check could not express, so it is probed at both
    // ends here. Type name only; the messages are localized.
    //
    // The two GetFullPathNameW refusals are covered from the bottom of this section.
    // They only fault on a Windows host — on POSIX the same inputs are ordinary
    // relative paths — and that asymmetry needs no host switch here, because the
    // expectation is real .NET on the SAME host and diverges with the subject.
    //
    // Not covered, and not an oversight: getcwd's failure is an ENVIRONMENT failure no
    // argument selects, and a section here cannot remove the process's working
    // directory without changing what every later ConsoleIo section sees — it is asserted
    // in EnvSubset, which owns the cwd surface and gets a private scratch dir per run.
    internal static class Program
    {
        private static void Catches(string what, Action body)
        {
            try
            {
                body();
                Console.WriteLine(what + " -> no throw");
            }
            catch (Exception e)
            {
                Console.WriteLine(what + " -> " + e.GetType().Name);
            }
        }

        internal static void __GateEntry()
        {
            Catches("Path.Combine(null, \"b\")", () => { string r = Path.Combine((string)null, "b"); });
            Catches("Path.Combine(\"a\", null)", () => { string r = Path.Combine("a", (string)null); });
            Catches("Path.Combine(null, null)", () => { string r = Path.Combine((string)null, (string)null); });
            Catches("Path.Combine(a, b, null) [3]",
                () => { string r = Path.Combine("a", "b", (string)null); });
            Catches("Path.Combine(a, b, c, null) [4]",
                () => { string r = Path.Combine("a", "b", "c", (string)null); });

            Catches("Path.GetFullPath(null)", () => { string r = Path.GetFullPath((string)null); });
            Catches("Path.GetFullPath(\"\")", () => { string r = Path.GetFullPath(""); });

            // A typed catch selects the fault over a broader one, and the two
            // GetFullPath faults are told apart rather than collapsed.
            try
            {
                string r = Path.Combine((string)null, "b");
                Console.WriteLine("unreachable " + r);
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("typed catch: ArgumentNullException");
            }
            catch (Exception)
            {
                Console.WriteLine("typed catch: fell through to Exception");
            }

            try
            {
                string r = Path.GetFullPath("");
                Console.WriteLine("unreachable " + r);
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("empty path: wrongly ArgumentNullException");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("typed catch: ArgumentException");
            }

            // A finally on the unwind path runs.
            try
            {
                try
                {
                    string r = Path.Combine((string)null, "b");
                    Console.WriteLine("unreachable " + r);
                }
                finally
                {
                    Console.WriteLine("finally ran");
                }
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("caught after finally");
            }

            // Recovery is real: the surface keeps working after the faults.
            Console.WriteLine("after faults: " + Path.Combine("a", "b"));
            Console.WriteLine("rooted still wins: " + Path.Combine("a", "/b"));

            // A combine loop over mixed rows: the shape this section exists for —
            // one null row must not take the program down.
            string[] names = { "one", null, "two", null, "three" };
            int made = 0, bad = 0;
            foreach (string name in names)
            {
                try
                {
                    if (Path.Combine("root", name).Length > 0) made++;
                }
                catch (ArgumentNullException)
                {
                    bad++;
                }
            }
            Console.WriteLine("combine loop: made=" + made + " bad=" + bad);

            // GetFullPath inputs the Win32 normalizer refuses. An all-space path is
            // "effectively empty" on Windows and answers the same ArgumentException as
            // ""; a path past the 32K ceiling is PathTooLongException. Both are
            // catchable, which is the whole point — the alternative reading of a
            // refused path is a process abort no handler can see.
            Catches("Path.GetFullPath(\" \")", () => { string r = Path.GetFullPath(" "); });
            Catches("Path.GetFullPath(32800 chars)",
                () => { string r = Path.GetFullPath(new string('a', 32800)); });

            // PathTooLongException derives from IOException, so the narrow clause has
            // to win — a runtime raising the base type would still satisfy a bare
            // `catch (IOException)` and hide the difference.
            try
            {
                string r = Path.GetFullPath(new string('a', 32800));
                Console.WriteLine("long path -> no throw, rooted=" + Path.IsPathRooted(r));
            }
            catch (PathTooLongException)
            {
                Console.WriteLine("typed catch: PathTooLongException");
            }
            catch (IOException)
            {
                Console.WriteLine("typed catch: fell through to IOException");
            }

            // Recovery again, after the normalizer faults rather than the argument ones.
            Console.WriteLine("after full-path faults: " + Path.Combine("a", "b"));
        }
    }
}
