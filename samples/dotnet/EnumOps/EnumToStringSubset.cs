#nullable enable
// a boxed enum's Object.ToString formats by member name.
// A boxed enum reaching Object.ToString (o.ToString, Console.WriteLine(enum),
// "s" + enum) used to print its underlying number — only the statically-typed
// interpolation hole ($"{e}") looked up the name. Now each enum's per-type-info
// `tostring` slot is wired to a generated name-formatting function (a plain enum
// maps the value through a switch; a [Flags] enum decomposes into "A, B"; an
// undefined value falls back to the decimal), so dn2cpp_object_tostring dispatches
// it. Covers a user enum, a [Flags] enum, and an external CoreLib enum. Real
// System.Private.CoreLib (-r) -> run vs.NET.
using System;
namespace EnumToStringSubset;


enum Team { Red, Blue, Green }

[Flags]
enum Access { None = 0, Read = 1, Write = 2, Execute = 4 }

class Program
{internal static void __GateEntry()
    {
        object o = Team.Green;
        Console.WriteLine(o.ToString());   // Green
        Console.WriteLine("v=" + o);       // v=Green (Concat(object) boxes)
        Console.WriteLine(Team.Blue);      // Blue (WriteLine(object))

        object d = DayOfWeek.Wednesday;
        Console.WriteLine(d);              // Wednesday (external CoreLib enum)

        object a = Access.Read | Access.Execute;
        Console.WriteLine(a);              // Read, Execute
        Console.WriteLine(Access.None);    // None

        object u = (Team)42;
        Console.WriteLine(u);              // 42 (undefined -> number)

        Console.WriteLine($"{Team.Red}");  // Red (static-typed hole, existing path)
    }
}
