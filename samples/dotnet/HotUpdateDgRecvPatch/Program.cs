using System;
using HotUpdateBase;

namespace HotDgRecvPatch;

// Corruption fixture for the receiver a PATCH frame is entered with. A
// non-virtual patch `call` null-checks its receiver and nothing more, and
// dn2cpp_interp_dgcall installs whatever the delegate `newobj` captured. What
// follows a patch call is `ldfld`/`stfld` at a LOADER-ASSIGNED offset of the
// declaring type, which the receiver does not bound: on a shorter object that is
// an out-of-range write, not a misread.
//
// The corruption is one in-place pooled write with no length prefix to move,
// "HotUpdateBase.Describer" and "HotUpdateBase.Annotator" both being 23 bytes:
//
//   "HotUpdateBase.Describer"  ->  "HotUpdateBase.Annotator"
//
// Annotator's Invoke takes an int where Describer's takes nothing, so the
// `newobj` installs a ONE-argument interpreter thunk on a closure captured
// against a no-argument delegate. Nothing on the AOT side notices: Announce's
// parameter is still a Describer and the AOT invoke still uses Describer's ABI,
// so the thunk's second argument is whatever the caller's registers held — and
// dn2cpp_interp_dgcall, whose only evidence of staticness is arity (a
// MethodTable record carries no static bit), reads Describe's argCount of 1 as
// "a static method with one parameter" and puts that register value in the frame
// slot the body reads as `this`.
//
// So the delegate must be invoked from the AOT SIDE, through Counter.Announce.
// The patch may not invoke it itself: an interpreted `Invoke` is an import whose
// declaring type is the same pooled string as the `newobj` type import, so the
// relabel would move both and the two sides would agree again.
//
// Describe's body reaches its field through a second patch call rather than
// directly, so the value that arrives lands on the `call` arm's receiver test
// before any `ldfld`. That arm does not save this image: like every receiver
// test in this interpreter it presumes a slot in a reference position holds an
// object, and a register the caller never wrote does not — which is why the
// arity contradiction has to be refused at the capture, where it is visible as a
// contradiction, rather than where it is dereferenced. The two guards are
// complements, not a chain: this one covers a value that is not an object, the
// `call` arm covers a valid object of the wrong patch type.
//
// Do not add members here, and keep Describer's name the only 23-byte pooled
// string in the image.
internal sealed class Probe
{
    // A REFERENCE field, and deliberately: the offset `ldfld` adds is the
    // declaring type's, so a receiver that is not one yields a pointer read out
    // of an unrelated object — and the interpreter's string surface is the
    // Concat overloads, which an int field would have to reach through a
    // `ldflda` the converter fence does not carry anyway.
    private string _value = "7";

    public string Describe()
    {
        return Render();
    }

    private string Render()
    {
        return "probe#" + _value;
    }
}

internal static class Program
{
    private static void Main()
    {
        Probe p = new Probe();
        // The one delegate construction the gate re-labels, and the AOT-side
        // Invoke that makes the mismatch reachable.
        Counter.Announce(p.Describe);
    }
}
