#nullable enable
using System;
using System.Runtime.InteropServices;

// The run-time half of two native-import contracts: NativeLibrary.Load exercises the
// implemented dynamic loader, while Debugger.IsAttached exercises a bounded import's
// substituted call site. There is no frozen snapshot here, so the exact diff against real
// .NET is the assertion and every line printed must be one both hosts can agree on.
//
//   LOAD   — NativeLibrary.Load over a path no host has throws DllNotFoundException.
//   SILENT — Debugger.IsAttached, bounded to its `false` default. That default is the truth
//           on both hosts; flip the row to Loud and the native side throws instead.
//
// A verdict is a choice between a truthful default and a loud refusal — when neither fits,
// the row itself is what to question.
//
// Never print an exception Message here: a Message can embed an absolute path and the two
// hosts run in separate scratch directories. Messages are only ever tested.
//
// The sections below cover the two mouths a bound has besides the call site. `ldftn` (a
// method group over a STATIC target) needs a bounded import that is never called, only
// bound — CutNative.Probe, whose row in the bounded-import report therefore exists solely
// because the ldftn arm reported it. `ldvirtftn` (a virtual or interface method group)
// needs a bounded method that is virtually dispatched, so it cannot be a [DllImport]: these
// are ordinary managed methods the gate's `--cut` bounds, the only lever that manufactures
// the shape. Their real bodies throw so that real .NET's caught 0 and dn2cpp's bounded
// substitute's 0 print the same line. The return types are deliberately not int — step 6
// greps the emitted stubs' ABI, and an `int Answer(int)` would render the same lambda as
// the ldftn section's, collapsing three disjoint greps into one that two mouths satisfy.
namespace BoundedImportSubset;

static class CutNative
{
    /// <summary>A native import that exists nowhere, cut by the gate's <c>--cut</c> so the
    /// method group below binds the bounded substitute rather than a real symbol. Reached
    /// only as an address — a direct call would answer through the OTHER mouth and the
    /// report row would no longer be evidence about this one.</summary>
    [DllImport("dn2cpp-absent-module", EntryPoint = "dn2cpp_absent_entry")]
    internal static extern int Probe(int x);
}

/// <summary>A VIRTUAL method the gate's <c>--cut</c> bounds, reached only as a method group,
/// so <c>ldvirtftn</c> reads a vtable slot reachability never filled. The body throws so that
/// real .NET, which runs it, prints the same line dn2cpp's substitute does.</summary>
class CutVirtual
{
    internal virtual long Answer(int x) =>
        throw new InvalidOperationException("the cut virtual's real body ran");
}

/// <summary>The interface twin: same shape, dispatched through the interface table rather
/// than the vtable, so it covers the other arm of a bounded <c>ldvirtftn</c>. The
/// <c>--cut</c> names the INTERFACE declaration, because that is the token the method group
/// carries — cutting the implementation would leave the mouth asking about an unbounded
/// <c>ICutItf.Answer</c> and the section would silently stop testing anything.</summary>
interface ICutItf
{
    double Answer(int x);
}

sealed class CutItfImpl : ICutItf
{
    public double Answer(int x) =>
        throw new InvalidOperationException("the cut interface method's real body ran");
}

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- a missing dynamic library: NativeLibrary.Load --");
        bool loud;
        try
        {
            // An exact path spelling no loader can resolve on any host.
            NativeLibrary.Load("dn2cpp-no-such-native-library");
            loud = false;
        }
        catch (DllNotFoundException)
        {
            // Real .NET.
            loud = true;
        }
        Console.WriteLine("Load fails loudly: " + loud);

        Console.WriteLine("-- a SILENT bounded import: Debugger.IsAttached --");
        Console.WriteLine("IsAttached: " + System.Diagnostics.Debugger.IsAttached);

        Console.WriteLine("-- a bounded import as a METHOD GROUP: the ldftn mouth --");
        // `Func<int, int> mg = CutNative.Probe;` is a delegate creation over a method
        // group, which the C# compiler lowers to `ldftn` + `newobj` (cached in a static
        // field) — the instruction pair this section exists for. dn2cpp answers the ldftn
        // with a stub carrying the delegate invoker's ABI, and the invoke below is what
        // calls through it.
        Func<int, int> mg = CutNative.Probe;
        int answered;
        try
        {
            answered = mg(7);
        }
        catch (DllNotFoundException)
        {
            // Real .NET, and the only sound normalization available: the module does not
            // exist on any host, so the oracle cannot produce a number at all. The value
            // under test is dn2cpp's — the bounded substitute's zero, reached through the
            // synthesized stub. What the oracle contributes is the statement that nothing
            // in this section could have come from a real native call.
            answered = 0;
        }

        Console.WriteLine("cut import bound as a method group answers: " + answered);

        Console.WriteLine("-- a cut VIRTUAL as a method group: the ldvirtftn mouth --");
        // `Func<int, long> vg = cv.Answer;` over a virtual method lowers to
        // `dup; ldvirtftn; newobj` — the instruction this section exists for, and the one
        // the ldftn section above cannot reach because its target is static.
        var cv = new CutVirtual();
        Func<int, long> vg = cv.Answer;
        long virtAnswered;
        try
        {
            virtAnswered = vg(7);
        }
        catch (InvalidOperationException)
        {
            // Real .NET, which has no cut and runs the real body.
            virtAnswered = 0;
        }

        Console.WriteLine("cut virtual bound as a method group answers: " + virtAnswered);

        ICutItf ii = new CutItfImpl();
        Func<int, double> ig = ii.Answer;
        double itfAnswered;
        try
        {
            itfAnswered = ig(7);
        }
        catch (InvalidOperationException)
        {
            itfAnswered = 0;
        }

        Console.WriteLine("cut interface method bound as a method group answers: " + itfAnswered);

        // Reachable, never executed — see __NeverRun. The call is what puts its IL in the
        // tree; the guard is what keeps it off both hosts' transcripts.
        if (s_never != 0)
        {
            __NeverRun(null!);
        }
    }

    /// <summary>The dynamic-codegen arm of the same mouth, which no program can run: the
    /// surface it names is precisely what a transpiled binary cannot do, so there is no
    /// receiver to bind. <see cref="s_never"/> is a mutable static so that neither Roslyn nor
    /// dn2cpp's branch liveness folds the test away — the IL must be scanned and the arm
    /// emitted, because the emitted text is the only oracle for a stub ABI error that is
    /// invisible natively and fatal on wasm. Step 6 greps for it.</summary>
    private static int s_never = 0;

    private static void __NeverRun(System.Reflection.Emit.ILGenerator ilg)
    {
        if (s_never == 0)
        {
            return;
        }

        Action<Type> te = ilg.ThrowException;
        te(typeof(InvalidOperationException));
    }
}
