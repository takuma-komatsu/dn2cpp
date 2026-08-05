using System;
using HotUpdateBase;

namespace HotFtnPatch;

// Corruption fixture for the `ldftn` mouth onto a bound import's function
// pointer: the pointer becomes an AOT delegate's `method` and is called later
// with the receiver captured at the delegate `newobj`, never crossing a dispatch
// loop's call arm. So every receiver test those arms perform is reachable
// around, and the delegate `newobj` is the only point at which the interpreter
// ever sees that receiver (an Invoke re-enters through the delegate's own thunk,
// which knows nothing about the import).
//
// The corruption is the same single in-place pooled write as
// HotUpdateInvokerPatch's, both names being 21 bytes:
//
//   "HotUpdateBase.QuotaEx"  ->  "HotUpdateBase.Counter"
//
// so the `ldftn` binds Counter's Describe while the captured `this` is still the
// QuotaEx the base image handed over. Only the METHOD-GROUP conversion is here,
// not a call: what must be refused is the construction.
//
// So: do NOT add members here, and never `new` or catch a QuotaEx — see the
// twin fixture's header for why the instance has to arrive through
// Counter.SeedQuota. QuotaEx.Describe is non-virtual, which is what makes the
// conversion an `ldftn` at all: the converter refuses `ldvirtftn`.
internal static class Program
{
    private static void Main()
    {
        QuotaEx q = Counter.SeedQuota;
        // The one import the gate re-labels. The delegate construction on the
        // next line is what has to refuse it.
        Describer d = q.Describe;
        Console.WriteLine(d());
    }
}
