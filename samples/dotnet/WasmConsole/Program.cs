using System;
using System.Globalization;

namespace WasmConsole;

internal static class Program
{
    // Each mode starts a fresh process while sharing the compiled Wasm image.
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length < 1 || args.Length > 2)
            throw new ArgumentException("expected a console section and optional prefix selector");

        string[] sectionArgs = args.Length == 2 ? new[] { args[1] } : Array.Empty<string>();
        switch (args[0])
        {
            case "StringCore":
                StringCore.Program.Main(sectionArgs);
                break;
            case "NestedFinallySubset":
                NestedFinallySubset.Program.Main();
                break;
            case "WeakReferences":
                WeakReferences.Program.Main(sectionArgs);
                break;
            case "MonotonicClock":
                MonotonicClock.Program.Main();
                break;
            case "PalSurface":
                PalSurface.Program.Main(sectionArgs);
                break;
            default:
                throw new ArgumentException("unknown console section");
        }
    }
}
