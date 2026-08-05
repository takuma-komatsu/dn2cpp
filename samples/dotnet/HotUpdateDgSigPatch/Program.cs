using System;
using HotUpdateBase;

namespace HotDgSigPatch;

// Corruption fixture for the SIGNATURE of an AOT delegate's target. Gating the
// capture on the receiver and on the call SHAPE keeps an M2N bridge or a
// constructor out of an Invoke thunk, but says nothing about the target's
// signature: an AOT delegate's `method` is the bound import's raw function
// pointer, invoked through the delegate's Invoke C++ ABI, so a target of a
// different shape is called with registers the caller never set.
//
// The corruption is two in-place pooled writes onto the single method import:
//
//   "Rate"             ->  "Warn"              (4 bytes, no prefix to move)
//   "(Single):String"  ->  "(String):String"   (15 bytes, likewise)
//
// so the `ldftn` binds QuotaEx.Warn(string) while the delegate stays a Tuner,
// whose Invoke passes its argument in the floating-point register. Warn's
// pointer parameter is then whatever was live in the register the caller never
// wrote, and its first act is to concatenate the string it points at. Both
// pooled strings occur exactly once in the baked image, which is what lets the
// corruption be located by CONTENT rather than by a record offset the next
// sample edit would silently invalidate.
//
// The instance arrives through Counter.SeedQuota, a well-known static on a
// DIFFERENT type, for the reason the sibling fixtures do the same: a `new` or a
// catch would bake a second user of the declaring type's pooled name. Unlike
// those, this fixture INVOKES the delegate, because the point is that a capture
// nothing refuses is later called — so keep the invoke, and keep "Rate" and
// "(Single):String" the only occurrences of themselves.
internal static class Program
{
    private static void Main()
    {
        QuotaEx q = Counter.SeedQuota;
        // The one import the gate re-labels; the construction on the next line
        // is what has to refuse it.
        Tuner t = q.Rate;
        Console.WriteLine(t(3f));
    }
}
