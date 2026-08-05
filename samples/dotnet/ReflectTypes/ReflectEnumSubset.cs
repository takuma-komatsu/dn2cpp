#nullable enable
using System;

namespace ReflectEnumSubset
{
    enum Color { Red, Green, Blue }

    // Non-contiguous values (still the default int underlying).
    enum Status { Active = 1, Paused = 5, Stopped = 10 }

    [Flags]
    enum Perm { None = 0, Read = 1, Write = 2, Exec = 4 }

    static class Program
    {internal static void Run()
        {
            // GetValues<T>() -> a typed T[] of the declared values.
            Console.WriteLine("== GetValues ==");
            foreach (Color c in Enum.GetValues<Color>())
                Console.WriteLine((int)c);          // 0 1 2
            foreach (Status s in Enum.GetValues<Status>())
                Console.WriteLine((int)s);          // 1 5 10

            // GetNames<T>() -> the member names.
            Console.WriteLine("== GetNames ==");
            foreach (string n in Enum.GetNames<Color>())
                Console.WriteLine(n);               // Red Green Blue
            foreach (string n in Enum.GetNames<Status>())
                Console.WriteLine(n);               // Active Paused Stopped

            // GetName<T>(value) -> the matching member name, or null.
            Console.WriteLine("== GetName ==");
            Console.WriteLine(Enum.GetName(Color.Green));         // Green
            Console.WriteLine(Enum.GetName(Status.Stopped));      // Stopped
            Console.WriteLine(Enum.GetName((Color)99) ?? "null"); // null

            // Parse<T>(string) -> the value whose name matches.
            Console.WriteLine("== Parse ==");
            Console.WriteLine((int)Enum.Parse<Color>("Blue"));    // 2
            Console.WriteLine((int)Enum.Parse<Status>("Paused")); // 5

            // TryParse<T>(string, out T): true + value on a hit, false + default on a miss.
            Console.WriteLine("== TryParse ==");
            if (Enum.TryParse<Color>("Red", out Color r))
                Console.WriteLine((int)r);                        // 0
            Console.WriteLine(Enum.TryParse<Color>("Nope", out Color r2)); // False
            Console.WriteLine((int)r2);                           // 0 (default on failure)

            // IsDefined<T>(value) -> the value is a declared member.
            Console.WriteLine("== IsDefined ==");
            Console.WriteLine(Enum.IsDefined(Color.Red));     // True
            Console.WriteLine(Enum.IsDefined((Color)99));     // False
            Console.WriteLine(Enum.IsDefined(Status.Active)); // True
            Console.WriteLine(Enum.IsDefined((Status)7));     // False

            // Parse/TryParse with ignoreCase: ASCII case-fold over member names.
            Console.WriteLine("== Parse(ignoreCase) ==");
            Console.WriteLine((int)Enum.Parse<Color>("blue", true));    // 2
            Console.WriteLine((int)Enum.Parse<Color>("GREEN", true));   // 1
            Console.WriteLine((int)Enum.Parse<Status>("paused", true)); // 5
            // ignoreCase: false still requires an exact match.
            bool threw = false;
            try { Enum.Parse<Color>("blue", false); } catch (ArgumentException) { threw = true; }
            Console.WriteLine(threw);                                    // True

            Console.WriteLine("== TryParse(ignoreCase) ==");
            Console.WriteLine(Enum.TryParse<Color>("rEd", true, out Color cr));   // True
            Console.WriteLine((int)cr);                                           // 0
            Console.WriteLine(Enum.TryParse<Color>("rEd", false, out Color cr2)); // False
            Console.WriteLine((int)cr2);                                          // 0 (default on miss)
            Console.WriteLine(Enum.TryParse<Status>("STOPPED", true, out Status cs)); // True
            Console.WriteLine((int)cs);                                           // 10

            // Numeric-string + comma-separated flags parse. .NET parses the
            // *whole* trimmed string as a single decimal, else treats it as a
            // comma-separated list of member names (no per-token numeric).
            Console.WriteLine("== Parse(numeric/flags) ==");
            Console.WriteLine((int)Enum.Parse<Color>("2"));               // 2 (numeric)
            Console.WriteLine((int)Enum.Parse<Color>(" 99 "));            // 99 (undefined, still allowed; trimmed)
            Console.WriteLine((int)Enum.Parse<Color>("-1"));              // -1 (signed numeric)
            Console.WriteLine((int)Enum.Parse<Perm>("Read, Write"));      // 3 (OR of names)
            Console.WriteLine((int)Enum.Parse<Perm>("Read,Exec"));        // 5 (no-space variant)
            Console.WriteLine((int)Enum.Parse<Perm>(" Read ,  Write "));  // 3 (tokens trimmed)
            Console.WriteLine((int)Enum.Parse<Perm>("read, exec", true)); // 5 (flags + ignoreCase)

            Console.WriteLine("== TryParse(numeric/flags) ==");
            Console.WriteLine(Enum.TryParse<Perm>("Write, Exec", out Perm pw)); // True
            Console.WriteLine((int)pw);                                          // 6
            Console.WriteLine(Enum.TryParse<Color>("3", out Color c3));          // True
            Console.WriteLine((int)c3);                                          // 3
            Console.WriteLine(Enum.TryParse<Perm>("Read, 4", out Perm pmix));    // False (no per-token numeric)
            Console.WriteLine((int)pmix);                                        // 0
            Console.WriteLine(Enum.TryParse<Perm>("Read, Nope", out Perm pn));   // False (bad token)
            Console.WriteLine((int)pn);                                          // 0 (default on miss)
            Console.WriteLine(Enum.TryParse<Perm>("", out Perm pe));             // False (empty)
            Console.WriteLine((int)pe);                                          // 0
        }
    }
}
