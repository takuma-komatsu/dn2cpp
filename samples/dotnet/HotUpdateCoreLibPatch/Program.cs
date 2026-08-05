using System;
using System.Collections;
using HotUpdateCoreLibBase;

namespace HotUpdateCoreLibPatch;

// The well-behaved patch for the real-CoreLib hot-update base: binds and runs
// against an image whose member tables were transpiled from the real CoreLib —
// static imports onto the base's Registry, and interface-typed callvirts through
// System.Collections.IDictionaryEnumerator, whose OTHER row (get_Entry) carries
// a trapping stub. This patch drives only the marshallable row (MoveNext — the
// converter's external call fence takes scalars/string/reference returns, so the
// object-returning get_Key/get_Value are off the table like get_Entry), so it
// must load and run; its sibling HotUpdateCoreLibBadPatch is the one that names
// the trapped row. `dotnet HotUpdateCoreLibPatch.dll` — after the base's own
// Main has run, i.e. with alpha/beta present — is the oracle for the
// interpreted lines.
internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("patch: start");
        Registry.Set("gamma", true);
        Console.WriteLine(Registry.Count());
        Console.WriteLine(Registry.Get("gamma"));
        Console.WriteLine(Registry.Get("beta"));
        IDictionaryEnumerator cur = Registry.Cursor();
        int seen = 0;
        while (cur.MoveNext())
        {
            seen = seen + 1;
        }
        Console.WriteLine(seen);
        Console.WriteLine("patch: done");
    }
}
