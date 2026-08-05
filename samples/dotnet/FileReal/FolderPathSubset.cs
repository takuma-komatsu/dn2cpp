#nullable enable
using System;
using System.IO;

// Environment.GetFolderPath / Environment.SystemDirectory over the real CoreLib
// bodies. Their closure names two PAL entries the identity section does not —
// SystemNative_Access and SystemNative_SearchPath
// (runtime/core/platform/posix/dn2cpp_system_native.cpp) — so a regression here
// is a C++ LINK error naming both symbols, the same mode as its sibling
// PalIdentitySubset; the gate's step 5/6 asserts the symbol set from both ends.
//
// Nothing here prints an absolute path (this bucket's driver rule): the
// SearchPath-backed folders are printed HOME-RELATIVE, which is the strongest
// deterministic assertion available — on macOS the oracle answers through
// Foundation's NSSearchPathForDirectoriesInDomains, so an exact diff of the
// "/Music"-style suffixes proves the PAL returns byte-for-byte what Foundation
// does rather than something merely plausible. Both sides run as the same user
// with the same environment, so every value below is deterministic between them
// on any Unix host (on Linux the folders are the XDG defaults under the same
// HOME and the same suffixes print).
namespace FolderPathSubset;

static class Program
{
    internal static void __GateEntry()
    {
#if DN2CPP_HOST_WINDOWS
        // On Windows Environment.GetFolderPath is Interop.Shell32
        // (SHGetKnownFolderPath), a module outside
        // Compilation.IsRuntimeProvidedPInvokeModule — same compile-time arm and
        // same reasoning as PalIdentitySubset's passwd half (see FileReal.csproj).
        // Real .NET prints this same line, so the diff holds.
        Console.WriteLine("-- Environment.GetFolderPath: Interop.Sys is Unix-only, not exercised here --");
#else
        Console.WriteLine("-- Environment.GetFolderPath (SystemNative_Access + SearchPath closure) --");
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Console.WriteLine("UserProfile non-empty: " + (home.Length > 0));
        Console.WriteLine("UserProfile rooted: " + Path.IsPathRooted(home));
        Console.WriteLine("UserProfile exists: " + Directory.Exists(home));
        Console.WriteLine("UserProfile stable: "
            + (home == Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));

        // The SearchPath-backed kinds (on macOS these run through
        // Interop.Sys.SearchPath at run time, not just at link time).
        PrintHomeRelative(home, "MyMusic", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
        PrintHomeRelative(home, "MyPictures", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        PrintHomeRelative(home, "Desktop", Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

        Console.WriteLine("-- Environment.SystemDirectory --");
        string sys = Environment.SystemDirectory;
        // "/System" on macOS, "" on Linux — both deterministic per host, and the
        // booleans are computed identically on both sides of the diff either way.
        Console.WriteLine("SystemDirectory is /System: " + (sys == "/System"));
        Console.WriteLine("SystemDirectory empty-or-rooted: " + (sys.Length == 0 || Path.IsPathRooted(sys)));
#endif
    }

#if !DN2CPP_HOST_WINDOWS
    private static void PrintHomeRelative(string home, string name, string path)
    {
        // A home-relative print keeps the driver's no-absolute-paths rule while
        // still diffing the exact bytes the folder provider produced.
        string shown = path.StartsWith(home, StringComparison.Ordinal)
            ? path.Substring(home.Length)
            : (path.Length == 0 ? "(empty)" : "(outside home)");
        Console.WriteLine(name + ": " + shown);
    }
#endif
}
