using System;
using System.Collections;
using HotUpdateCoreLibBase;

namespace HotUpdateCoreLibBadPatch;

// The patch that invokes a trapped row: an interface-typed callvirt of
// System.Collections.IDictionaryEnumerator::get_Entry, whose DictionaryEntry
// return the base image never laid out. The row itself stays bindable — its
// invoker is the trapping invmiss_ stub, not a C++ compile error — but the
// import's SIGNATURE names the missing struct, and the interpreter's marshal
// surface (scalars + references) is strictly narrower than the invoker ABI, so
// the one-shot bind pass refuses the image with a catchable
// NotSupportedException the base's own catch prints. That refusal is the
// asserted behavior: an attempted invoke of a trapped row fails LOUDLY at the
// first surface that can see it, and nothing silently binds or aborts. (Should
// the interpreter's marshalling ever widen to by-value structs, this bind starts
// succeeding and the call below lands on the invmiss_ stub instead — the same
// catchable NotSupportedException, then naming the method and the missing
// layout.)
internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("badpatch: start");
        IDictionaryEnumerator cur = Registry.Cursor();
        cur.MoveNext();
        _ = cur.Entry;
        Console.WriteLine("badpatch: entry did not trap");
    }
}
