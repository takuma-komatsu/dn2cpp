using System;

namespace HotArgPatch;

// A corruption fixture, the argument-position twin of HotUpdateRecvPatch's. A
// method import binds by NAME alone — declaring type, method name, sigShape and
// staticness, every one of them a string out of the image's own pool — so a
// malformed `.bpi` can label a call site with a row of the interpreter's
// intrinsic table whose PARAMETERS its operands do not satisfy, and the helpers
// behind the three reference-argument shapes cast those operands raw. Nothing
// signs a `.bpi`; hotupdate_read_file loads whatever is on disk.
//
// This is the smallest patch holding exactly one `String.Concat` import, and the
// gate re-labels it with a single pooled write:
//
//   "(String,String):String" (22)  ->  "(String[]):String" (17)
//
// The declaring type and the method name do not move — only the signature shape
// does, which is what makes this the minimal corruption of the pair: the
// intrinsic table's `String.Concat(String[])` row shares its C++ signature
// (`Dn2CppObject* (*)(Dn2CppString*)`) with `Type.GetType(string)` and
// reinterpret_casts its operand straight to a Dn2CppArrayRef, so the re-labelled
// call hands `intrinsic_string_concat_array` a *string*: `length` is then read
// out of the string's own length field and every `data[i]` out of the char
// storage past `chars`.
//
// So: do NOT add members here, and do not concatenate anything else. A second
// two-string `Concat` in the baked image breaks the locator — the gate asserts
// the uniqueness and fails loudly rather than corrupting the wrong bytes, but
// the fixture is gone. The concatenation must also stay non-constant (Head() is
// a call precisely so Roslyn cannot fold it), or there is no import at all.
internal static class Program
{
    private static string Head()
    {
        return "arg";
    }

    private static void Main()
    {
        // The one import the gate re-labels. Written as an explicit
        // String.Concat rather than `+` so the bound overload is not a matter of
        // which lowering the compiler picks.
        Console.WriteLine(string.Concat(Head(), "-probe"));
    }
}
