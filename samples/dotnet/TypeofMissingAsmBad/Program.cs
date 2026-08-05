// A `typeof` whose operand lives in an assembly the transpile was not given: Complex is in
// System.Runtime.Numerics, not the CoreLib. A token-only use is the one route that can fail
// OPEN — the TypeRef degrades to an External and the `ldtoken` folds to `nullptr`, which
// links fine and hands the shipped binary a null `Type`; a field, local or call already
// throws in CppTypes.Of. So nothing here but the `typeof` needs the type. The gate
// transpiles it twice: without the reference it must refuse naming both the type and the
// assembly, with it it must succeed.
using System;
using System.Numerics;

namespace TypeofMissingAsmBad;

internal static class Program
{
    private static void Main()
    {
        Type t = typeof(Complex);
        Console.WriteLine(t is null ? "<null>" : t.FullName);
    }
}
