#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;

// an array whose element type is an INTRINSIC reference type — CultureInfo /
// IFormatProvider render as `const Dn2CppNumberFormatInfo*` in the C++ backend —
// used through IList<T> / ICollection<T>. The array's interface-dispatch thunk
// forwards the element argument to the shared SZArrayEnumerable<T> __CnRef
// canonical body, whose element parameter is `Dn2CppObject*`. The thunk parameter
// is typed at the concrete `const X*` element, so it must cast the argument down to
// `Dn2CppObject*` (dropping const, widening the reference) — the same cast `self`
// gets to the ctor's array-parameter type. IndexOf(T) and Contains(T) are the two
// element-taking slots that exercise it; without the cast the C++ does not compile.
// Reference identity (the culture singletons) keeps the
// answer deterministic and equal to real .NET.

namespace ArrayIntrinsicElementDispatchSubset
{
    static class Program
    {
        internal static int Run()
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            CultureInfo[] arr = { inv };

            // IList<T>.IndexOf(T) — slot forwards the `const X*` element to the
            // canonical Dn2CppObject* body.
            IList<CultureInfo> list = arr;
            Console.WriteLine("indexOf=" + list.IndexOf(inv));     // 0

            // ICollection<T>.Contains(T) — the sibling element-taking slot.
            ICollection<CultureInfo> col = arr;
            Console.WriteLine("contains=" + col.Contains(inv));    // True
            Console.WriteLine("listContains=" + list.Contains(inv)); // True
            return 0;
        }
    }
}
