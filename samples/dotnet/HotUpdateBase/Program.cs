using System;
using System.Globalization;
using Dn2Cpp.Runtime;

namespace HotUpdateBase;

// A non-generic base-image interface a hot-update patch type can implement: its
// method slots are frozen into the base ABI manifest, so the loader can wire an
// N2M interface trampoline into a patch type's interface-dispatch map.
public interface IDescribable
{
    string Describe();
    int Weight();
}

// Base-image delegate types a hot-update patch can bind its own methods to.
// Their Invoke signatures are frozen into the base ABI (D lines) and a
// --hotupdate-base build pre-emits an interpreter thunk + invoke bridge per
// type, so a patch method bound into one dispatches through the interpreter
// while an AOT method bound into one keeps the natural AOT path.
public delegate int IntTransform(int x);
public delegate void IntSink(int x);
public delegate string Describer();
// The patch binds Object.GetType (an instance INTRINSIC import) into this via
// ldftn, driving the delegate route: it reaches the wrapper without crossing a
// dispatch loop's call arm, so the delegate capture is the only point at which
// the interpreter ever sees the receiver.
public delegate Type TypeGetter();
// Fixture surface for the negative sections of build-and-run-hotupdate-subset.sh
// and for nothing else. Annotator is Describer's TWIN: both type names are 23
// bytes, so relabelling one to the other is an in-place pooled overwrite with no
// length prefix to move — while their Invoke ARITIES differ, which is the whole
// point. The relabel installs a one-argument thunk on a closure captured against
// a no-argument delegate, so the extra argument the thunk packs is whatever the
// AOT caller's ABI left in the register.
public delegate string Annotator(int n);
// Tuner's Invoke takes a float, so a bound method taking a STRING is called with
// the pointer register never set. QuotaEx.Rate/Warn are its pair (see there).
public delegate string Tuner(float f);

// An AOT type implementing the interface, so an interface-typed call from patch
// code onto a base-image instance dispatches through the ordinary AOT interface
// map (no interpreter involved) — and so RenderVia's callvirt sites and the
// interface's method-slot trampolines are emitted into the base image.
public sealed class Badge : IDescribable
{
    private readonly string _name;
    private readonly int _weight;

    public Badge(string name, int weight)
    {
        _name = name;
        _weight = weight;
    }

    public string Describe()
    {
        return "badge:" + _name;
    }

    public int Weight()
    {
        return _weight;
    }
}

// A stateful AOT class the hot-update interpreter touches from patch code:
// instance + static fields, a constructor, an instance method, a virtual
// method (overridden below), and a static helper. The base Main exercises
// every member so tree-shaking keeps them — with their reflection metadata
// and vtable slots — in the base image the patch imports bind against.
public class Counter
{
    public static int TotalCounters;

    // Reference-typed static slots deployment patches install their probe
    // instances into (stsfld through a field import). The directory-load mode
    // below reads them back to observe which registration each instance
    // carries: the newest same-name registration wins name lookups while
    // older instances keep their original type-info.
    public static Counter FirstProbe;
    public static Counter LastProbe;

    // Hands a QuotaEx to the HotUpdateInvokerPatch / HotUpdateFtnPatch fixtures
    // through a well-known static on a DIFFERENT type: those patches must leave
    // "HotUpdateBase.QuotaEx" with exactly ONE user in their baked image (a
    // `new` bakes a ctor import, a catch clause bakes an EH type, and either
    // would follow that string when the gate rewrites it). QuotaEx is the type
    // on offer because its name is 21 bytes like "HotUpdateBase.Counter", so the
    // rewrite is an in-place overwrite with no length prefix to move.
    public static QuotaEx SeedQuota;

    // The --shadow-trace mode below sets this before loading the patch, and the
    // patch entry answers by parking a ShadowProbe instance in LastProbe and
    // returning WITHOUT printing — so every other mode (the flag defaults to 0)
    // runs the classic transcript unchanged. An int rather than a bool keeps the
    // patch on the proven int static-field import surface.
    public static int ShadowProbeRequest;

    public int Step = 1;
    public string Label;
    private int _count;

    public Counter(int start)
    {
        Label = "counter";
        _count = start;
        TotalCounters++;
    }

    public int Add(int amount)
    {
        _count += amount * Step;
        return _count;
    }

    public int Deduct(int amount)
    {
        if (amount > _count)
            throw new InvalidOperationException("counter underflow");
        _count -= amount;
        return _count;
    }

    public virtual string Describe()
    {
        return Label + ":" + _count;
    }

    public static int Squared(int v)
    {
        return v * v;
    }

    // A base-typed parameter surface: patch code passes its own derived
    // instances through here, and the virtual dispatch inside runs on the
    // receiver's (possibly patch-constructed) type-info.
    public static string Render(Counter c)
    {
        return "render " + c.Describe();
    }

    // An array-typed parameter surface: patch code passes an int[] — its own
    // interpreter-allocated arrays included — through the import boundary.
    public static int SumArray(int[] values)
    {
        int total = 0;
        for (int i = 0; i < values.Length; i++)
            total += values[i];
        return total;
    }

    // An array-typed return surface: patch code receives an AOT-allocated
    // array (carrying its precise per-element type-info) and indexes it.
    public static int[] Series(int count)
    {
        int[] values = new int[count];
        for (int i = 0; i < count; i++)
            values[i] = i + 1;
        return values;
    }

    // An interface-typed parameter surface: patch code passes its own
    // interface-implementing types (and AOT ones) through here, and the
    // interface dispatch inside lands on the receiver's implementation — an
    // interpreted patch body through an N2M interface trampoline, or an AOT
    // body directly.
    public static string RenderVia(IDescribable d)
    {
        return "via " + d.Describe() + "#" + d.Weight();
    }

    // Delegate-typed parameter surfaces: patch code hands its own delegates
    // (wrapping interpreted patch methods) through here, so the Invoke inside
    // dispatches back into the interpreter through the delegate's thunk. Also
    // exercised by the base itself (below) so the delegate types, their invokers
    // and the helpers are emitted into the base image.
    public static int ApplyTwice(IntTransform f, int x)
    {
        return f(f(x));
    }

    public static void Each(int[] xs, IntSink sink)
    {
        for (int i = 0; i < xs.Length; i++)
            sink(xs[i]);
    }

    // Static methods the base binds into the delegates (also usable by the patch
    // as AOT delegate targets, though the patch prefers its own methods).
    public static int Increment(int x)
    {
        return x + 1;
    }

    public static void Print(int v)
    {
        Console.WriteLine("base-each:" + v);
    }

    public static string Greeting()
    {
        return "base-greeting";
    }

    // A Describer-typed parameter surface: the patch hands its own delegate over
    // a PATCH instance method here and the Invoke inside dispatches back through
    // the delegate's thunk. It has to be an AOT-side Invoke — an interpreted one
    // would import the Invoke through the SAME pooled declaring-type string as
    // the `newobj` type import, so the fixtures' relabel would move both and the
    // mismatch under test could never arise. Replaces the base's own
    // `Console.WriteLine(greet())` below, so the transcript is unchanged.
    public static void Announce(Describer d)
    {
        Console.WriteLine(d());
    }

    // The Annotator target the base binds so the type, its interpreter thunk and
    // its invoke bridge are emitted into the base image. Prints nothing.
    public static string Tag(int n)
    {
        return "tag" + n;
    }

    // Emits the delegate fixtures' surface — Annotator and Tuner with their
    // interpreter thunk/bridge, QuotaEx.Rate/Warn with bodies and invokers —
    // without adding a transcript line. The concatenation is non-empty for every
    // input, so the branch never runs; nothing here may construct a Counter (the
    // transcript prints TotalCounters).
    public static void EmitDelegateFixtureSurface()
    {
        Annotator ann = Tag;
        Tuner tune = SeedQuota.Rate;
        string s = ann(2) + tune(2f) + SeedQuota.Warn("w");
        if (s.Length == 0)
            Console.WriteLine("unreachable");
    }

    // An AOT target for TypeGetter, so the base exercise below emits the
    // delegate type with its invoke bridge into the base image (the patch's
    // delegate-over-null-this probes bind against them).
    public static Type CounterType()
    {
        return typeof(Counter);
    }

    // A generic method the base program never calls: its closed instantiations
    // come in only through the hotupdate-refs.txt method roots
    // (Counter::Echo[System.Int32] / [System.String]). Both are emitted under the
    // one reflected name "Echo" with the method type parameter fully erased, so
    // the hot-update loader tells the two instantiations a patch binds apart by
    // their sigShape ((Int32):Int32 vs (String):String) — the
    // generic-method-on-the-patch-surface path.
    public static T Echo<T>(T value)
    {
        return value;
    }
}

public class DoubleCounter : Counter
{
    public DoubleCounter(int start)
        : base(start)
    {
    }

    public override string Describe()
    {
        return "double " + base.Describe();
    }
}

// A user-defined generic reference type the base image monomorphizes. A
// hot-update patch binds closed instantiations of it by their mangled registry
// name (Holder`1<int> -> HotUpdateBase.Holder_Int32) against the base ABI
// manifest's instantiations map. Holder<int> is reachable from the base program
// below, so its members are emitted; Holder<string> is emitted only through the
// hotupdate-refs.txt roots (the base program never uses it), exercising the
// AOTGenericReferences-style pre-reference path.
public sealed class Holder<T>
{
    private T _value;

    public Holder(T value)
    {
        _value = value;
    }

    public T Value()
    {
        return _value;
    }

    public void Replace(T value)
    {
        _value = value;
    }

    public string Shape()
    {
        return "holder";
    }
}

// A user-defined generic delegate. Its closed instantiation Mapper<int> is an
// ordinary base-image delegate type; a --hotupdate-base build pre-emits its
// interpreter thunk + invoke bridge, so a patch method bound into it dispatches
// through the interpreter.
public delegate T Mapper<T>(T x);

// A base-image exception whose base is an External BCL exception the
// corelib-less build never loads (System.SystemException). Both ctor shapes and
// Describe are exercised by the base Main below so their bodies + invokers are
// emitted into the base image — which is what lets the patch derive from this
// type and chain its interpreted ctor into the REAL emitted ctor body (a BCL
// exception base can only ever seed the message).
public class QuotaEx : SystemException
{
    public int Quota;

    public QuotaEx(string message, int quota)
        : base(message)
    {
        Quota = quota;
    }

    public QuotaEx(string message, Exception inner)
        : base(message, inner)
    {
        Quota = -8;
    }

    // A non-virtual AOT reader over the exception prefix + the derived field,
    // callable from patch code through an ordinary method import: formats the
    // Message, the inner Message (read AOT-side — the interpreter's intrinsic
    // import table carries no get_InnerException) and Quota.
    public string Describe()
    {
        string inner = InnerException != null ? InnerException.Message : "none";
        return Message + "/" + inner + "#" + Quota;
    }

    // The pair HotUpdateDgSigPatch's corruption swaps: same arity, same NAME
    // length (4 bytes) and same sigShape length ("(Single):String" and
    // "(String):String" are both 15), so relabelling that patch's one method
    // import onto the other is two in-place pooled writes. What it buys is a
    // bound method whose parameter is a REFERENCE where the delegate's Invoke
    // declares a float: the Invoke ABI puts the argument in the floating-point
    // register and leaves the pointer one untouched, so Warn dereferences
    // whatever was live in it.
    public string Rate(float f)
    {
        return "rate#" + (int)f;
    }

    public string Warn(string s)
    {
        return "warn:" + s;
    }
}

// AOT raisers for four runtime-raised exception types: ArithmeticException,
// MissingMethodException, ObjectDisposedException and
// PlatformNotSupportedException. None of them is a class the transpiler ever
// emits — they answer with the runtime's own shared Dn2CppTypeInfo
// (CoreIntrinsics.RuntimeExceptionTypeInfo) — so raising one here does NOT put a
// row in the type name registry the way reaching an ordinary base-image class
// would. That is what makes the probes in HotUpdatePatch.FourFaults a test of
// the registry seed rather than of this file: the exception object they catch
// carries exactly the handle the dn2cpp_throw_* helpers raise, and the
// interpreted catch clause has to find that same handle by NAME.
public static class Faults
{
    // The one of the four the runtime raises with no managed `throw` in sight:
    // Math.Sign(double) lowers to an isnan test plus dn2cpp_throw_arithmetic()
    // at the call site (real .NET throws ArithmeticException here too).
    public static int SignOf(double v)
    {
        return Math.Sign(v);
    }

    // The other three have no runtime-raised route into an intrinsic-BCL base
    // image (their runtime raisers sit behind the real CoreLib's ThrowHelper,
    // behind Activator's ctor resolution, and behind the reflection-metadata
    // and dynamic-codegen traps), so they are raised from managed `throw`s —
    // which the transpiler lowers to dn2cpp_exception_new against the same
    // shared runtime handle, so the identity under test is unchanged.
    public static void Disposed()
    {
        throw new ObjectDisposedException("faults");
    }

    public static void MissingMethod()
    {
        throw new MissingMethodException("faults");
    }

    public static void Unsupported()
    {
        throw new PlatformNotSupportedException("faults");
    }
}

// The hot-update base program: an ordinary AOT-transpiled binary (built with
// --hotupdate-base) that loads a BPI given on the command line and runs its
// entry method in the runtime interpreter. The typed try/catch around the
// interpreter run receives a patch exception no patch handler consumed — the
// exception unwinds interpreted frames and this AOT frame on one machine —
// while anything else (e.g. the loader's stale-BPI NotSupportedException)
// still escapes and aborts.
internal static class Program
{
    private static void Main(string[] args)
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        // Deployment-driver modes, dispatched before any output so the classic
        // single-BPI transcript below stays byte-identical. --load-dir applies
        // a directory of *.bpi files in (patchVersion, filename) order and
        // reports how many loaded, then probes newest-wins registration: the
        // last-installed probe's type-info matches the current registration of
        // its FullName (True) while the first probe — an instance of the older
        // same-name type — keeps its original type-info (False). --load loads
        // a single BPI whose entry method is optional and probes that its
        // types registered anyway.
        if (args.Length == 2 && args[0] == "--load-dir")
        {
            int n = HotUpdate.LoadDirectory(args[1]);
            Console.WriteLine("loaded:" + n);
            Console.WriteLine(Counter.LastProbe.Describe());
            Type probe = Type.GetType("HotDirPatch.Probe");
            Console.WriteLine(probe == Counter.LastProbe.GetType());
            Console.WriteLine(probe == Counter.FirstProbe.GetType());
            return;
        }
        if (args.Length == 2 && args[0] == "--load")
        {
            HotUpdate.Load(args[1]);
            Console.WriteLine(Type.GetType("HotDirPatch.Probe") != null ? "registered" : "missing");
            return;
        }
        // Interpreted frames join the shadow stack: set the probe request, load
        // the patch (whose entry parks a ShadowProbe in Counter.LastProbe and
        // returns silently), then drive an AOT → interp → AOT → interp call
        // chain whose deepest patch method throws; the catch back in AOT code
        // prints the trace. Under --shadow-stack the trace is EXACT and frozen
        // by gates/build-and-run-shadow-stack.sh; on a plain base the same chain
        // runs and the kind-0 PC trace must DROP the interpreter frames, never
        // misattribute them — gates/build-and-run-hotupdate-subset.sh greps that
        // no "at HotUpdatePatch." line appears while the verdict line does.
        if (args.Length == 2 && args[0] == "--shadow-trace")
        {
            Counter.ShadowProbeRequest = 1;
            HotUpdate.Load(args[1]);
            ShadowTraceScenario();
            return;
        }

        Console.WriteLine("base: start");
        Counter plain = new Counter(3);
        plain.Step = 2;
        plain.Label = "warm";
        plain.Add(2);
        Counter twice = new DoubleCounter(1);
        twice.Add(1);
        Console.WriteLine(plain.Describe());
        Console.WriteLine(twice.Describe());
        Console.WriteLine(Counter.Render(twice));
        Console.WriteLine(Counter.Squared(4));
        Console.WriteLine(Counter.TotalCounters);
        Console.WriteLine(plain.Deduct(3));
        // Exercise the array surfaces (and emit the int[] type-info the patch
        // binds by name): 1+2+3.
        Console.WriteLine(Counter.SumArray(Counter.Series(3)));
        // Emit the interface surface (RenderVia's dispatch sites + IDescribable's
        // method-slot trampolines) and exercise an AOT implementer.
        Console.WriteLine(Counter.RenderVia(new Badge("base", 9)));
        // Emit the delegate surface (IntTransform/IntSink/Describer, their
        // invokers, and the ApplyTwice/Each helpers) and exercise AOT-side
        // delegate creation + invocation so the patch binds against them.
        Console.WriteLine(Counter.ApplyTwice(Counter.Increment, 10));
        Counter.Each(Counter.Series(3), Counter.Print);
        Describer greet = Counter.Greeting;
        // Through Announce rather than inline, so the Describer-typed parameter
        // surface HotUpdateDgRecvPatch binds against is emitted. Same line printed.
        Counter.Announce(greet);
        // Emit the TypeGetter surface (creation + invocation) for the patch's
        // delegate-over-null-this probes.
        TypeGetter kind = Counter.CounterType;
        Console.WriteLine(kind().FullName);
        // Emit the closed generic surface (Holder<int>'s Value/Replace/Shape +
        // ctor, Mapper<int>'s interpreter thunk/bridge) so the patch binds
        // against them; Holder<string> comes in only via hotupdate-refs.txt.
        Holder<int> hi = new Holder<int>(4);
        hi.Replace(hi.Value() + 3);
        Console.WriteLine(hi.Value());
        Console.WriteLine(hi.Shape());
        Mapper<int> dbl = Counter.Increment;
        Console.WriteLine(dbl(20));
        // The External-BCL-exception-based QuotaEx works AOT-side: both ctor
        // shapes (the real ctor chain writes Message/inner through the
        // System.Exception intrinsic and the derived field lands), throw/catch
        // on the derived type, and an object-typed isinst whose True proves the
        // emitted type-info chains to the shared runtime Exception handle (the
        // absent SystemException base would otherwise cut the chain at null).
        // This also emits both ctors + Describe with invokers into the base
        // image, the surface the patch's external-base section binds against.
        try
        {
            throw new QuotaEx("base quota", 7);
        }
        catch (QuotaEx e)
        {
            Console.WriteLine(e.Describe());
            object probe = e;
            Console.WriteLine(probe is Exception);
        }
        try
        {
            throw new QuotaEx("outer", new QuotaEx("inner cause", 5));
        }
        catch (QuotaEx e)
        {
            Console.WriteLine(e.Describe());
        }
        // The seed (see Counter.SeedQuota): park a QuotaEx where a patch can
        // pick it up through a field import alone. Prints nothing, so the
        // transcript above is unchanged.
        Counter.SeedQuota = new QuotaEx("seed", 1);
        // The delegate fixtures' surface, emitted after the seed it reads. Silent.
        Counter.EmitDelegateFixtureSurface();
        try
        {
            HotUpdate.Run(args[0]);
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine("base caught: " + e.Message);
        }
        // The loader registered the patch's types in the type-name registry,
        // so plain AOT reflection resolves them by name after the run.
        Type patched = Type.GetType("HotUpdatePatch.TaggedCounter");
        Console.WriteLine(patched != null ? patched.FullName : "patch type not registered");
        Console.WriteLine("base: done");
    }

    // The AOT half of the interleave chain, kept out of Main so the trace
    // carries a non-Main AOT frame between Main and the patch frames.
    // Counter.Render dispatches Describe on the patch-constructed probe; the
    // interpreted override re-enters AOT through Counter.RenderVia, which
    // dispatches onto a second patch type whose Describe throws — and the catch
    // lands here, in AOT code. The verdict line prints in BOTH flag states (the
    // plain-base negative arm greps for it); e.StackTrace is the exact kind-1
    // text under --shadow-stack and whatever kind-0 captured (possibly null — an
    // empty line) on a plain base.
    private static void ShadowTraceScenario()
    {
        try
        {
            Console.WriteLine(Counter.Render(Counter.LastProbe));
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine("caught: " + e.Message);
            Console.WriteLine("interleaved trace:");
            Console.WriteLine(e.StackTrace);
            Console.WriteLine("shadow-trace: caught in AOT base");
        }
    }
}
