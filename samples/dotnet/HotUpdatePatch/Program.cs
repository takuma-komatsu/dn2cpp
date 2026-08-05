using System;
using HotUpdateBase;

namespace HotUpdatePatch;

// The hot-update showcase: baked into a BPI by `dn2cpp --emit-patch` and
// executed by the runtime interpreter from inside the HotUpdateBase program. It
// exercises, in transcript order: the interpreter's scalar-instruction surface
// (arguments, locals, branches, arithmetic, comparisons, shifts, conversions,
// intra-patch calls); its AOT base-image access (newobj on base types, instance
// and static calls, fields, virtual dispatch through a base-typed variable);
// patch-type inheritance, patch-deriving-patch included; patch static state
// behind lazy — and throwing — static constructors; the SZArray surface over
// every fenced element kind; interfaces implemented implicitly and explicitly;
// delegates; generics; string building; derived-exception ctor bodies; and
// exception handling, down to the eleven null guards that live in the dispatch
// loop rather than in a runtime helper, each tripped and caught by the
// interpreted code that tripped it.
//
// Every value is printed through the Console.WriteLine intrinsic imports, so
// `dotnet HotUpdatePatch.dll` is the oracle. Two sections diverge from it
// deliberately and say so where they sit: the throwing cctor, and the final
// escaping throw — both crash real .NET, while the transpiled base catches them.
internal static class Program
{
    // An array parked in a patch static slot (a plain reference Slot copy).
    private static int[] s_latest;

    private static void Main()
    {
        // Probe-only load: when the base set Counter.ShadowProbeRequest (its
        // --shadow-trace mode), park a ShadowProbe where the base can reach it
        // and return WITHOUT printing, so that snapshot holds only the trace
        // lines. Every other mode (the flag defaults to 0) runs the transcript
        // below unchanged.
        if (Counter.ShadowProbeRequest != 0)
        {
            Counter.LastProbe = new ShadowProbe();
            return;
        }

        Console.WriteLine("Hello, interpreted world");

        // Arguments + recursion + branches + arithmetic.
        Console.WriteLine(Fib(10));

        // Locals and a loop.
        Console.WriteLine(SumTo(100));
        Console.WriteLine();

        // Comparisons, logic, shifts.
        int x = 12;
        int y = 10;
        Console.WriteLine(x & y);
        Console.WriteLine(x | y);
        Console.WriteLine(x ^ y);
        Console.WriteLine(~x);
        Console.WriteLine(x << 3);
        int neg = -1024;
        Console.WriteLine(neg >> 3);
        Console.WriteLine((int)((uint)neg >> 3));
        bool ordered = x > y;
        Console.WriteLine(ordered);
        Console.WriteLine(x <= y);
        char c = 'A';
        Console.WriteLine(c < 'Z');
        Console.WriteLine();

        // long / double / float arithmetic and conversions.
        long big = 1L << 40;
        Console.WriteLine(big);
        Console.WriteLine(big / 3);
        Console.WriteLine(big % 9973);
        Console.WriteLine(-big);
        double d = big / 3.0;
        Console.WriteLine(d);
        Console.WriteLine((long)(d * 0.5));
        float f = 1.5f;
        f = f * f + 0.25f;
        Console.WriteLine((double)f);
        Console.WriteLine();

        // Intra-patch calls: non-void returns, string arguments, dup via
        // chained assignment.
        long a, b;
        a = b = Cube(101);
        Console.WriteLine(a - b);
        Console.WriteLine(Cube(101));
        Announce("patch methods can call each other");
        Console.WriteLine(Describe(Fib(10) - 55));
        Console.WriteLine();

        // AOT-type access: construct base-image objects, call their instance
        // and static methods, read/write instance and static fields, and
        // dispatch a virtual method through a base-typed variable.
        Counter counter = new Counter(7);
        Console.WriteLine(Counter.Squared(9));
        Console.WriteLine(counter.Add(5));
        counter.Step = 3;
        Console.WriteLine(counter.Step);
        Console.WriteLine(counter.Add(4));
        counter.Label = "patched";
        Console.WriteLine(counter.Label);
        Counter.TotalCounters = 40;
        Counter.TotalCounters = Counter.TotalCounters + 2;
        Console.WriteLine(Counter.TotalCounters);
        Counter viaBase = new DoubleCounter(2);
        Console.WriteLine(viaBase.Describe());
        Console.WriteLine(counter.Describe());
        Console.WriteLine();

        // Patch-type inheritance: patch classes deriving from an AOT base
        // class (or from object) with their own instance fields. newobj runs
        // the interpreted ctor, whose body chains into the AOT base ctor
        // (asserted via the TotalCounters side effect printed from inside the
        // derived ctor body — the base ctor has already incremented it); own
        // fields live in loader-assigned slots appended after the live base
        // layout; inherited AOT fields/methods work through the same import
        // bindings as any base-image access. TaggedCounter overrides the
        // base's virtual Describe, so a patch instance flowing through
        // base-typed variables dispatches into the interpreted override from
        // both sides: the patch-side callvirt (asBase.Describe()) and the
        // callvirt inside the AOT method Counter.Render both land on it, the
        // override's base.Describe() reaches the AOT base implementation, and
        // an override throw unwinds across the AOT caller frame into the
        // patch-side handler below.
        Console.WriteLine("before TaggedCounter");
        TaggedCounter tagged = new TaggedCounter("first", 5);
        Console.WriteLine(tagged.Tag());
        Console.WriteLine(tagged.Weight);
        tagged.Weight = tagged.Weight + 3;
        Console.WriteLine(tagged.Weight);
        Console.WriteLine(tagged.Label);
        Console.WriteLine(tagged.Add(10));
        tagged.Step = 2;
        Console.WriteLine(tagged.Add(10));
        Counter asBase = tagged;
        Console.WriteLine(asBase.Describe());
        Console.WriteLine(Counter.Render(asBase));
        Console.WriteLine(tagged.Describe());
        tagged.Weight = 200;
        try
        {
            Console.WriteLine(Counter.Render(asBase));
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e.Message);
        }
        tagged.Weight = 13;
        Pair pair = new Pair(6, 1L << 33);
        Console.WriteLine(pair.Sum());
        pair.Bump(4);
        Console.WriteLine(pair.Sum());
        Console.WriteLine();

        // Patch static state + lazy cctor timing. Registry's explicit static
        // constructor (field initializers included) runs exactly once, on the
        // first call into the type — after the "before" line — and touches
        // its own statics re-entrantly while it is still running; later
        // accesses never re-run it.
        Console.WriteLine("before first Registry touch");
        Console.WriteLine(Registry.NextTicket());
        Console.WriteLine(Registry.NextTicket());
        Registry.Tag = "updated";
        Console.WriteLine(Registry.Tag);
        Console.WriteLine(Registry.CounterValue());
        Console.WriteLine(Registry.CounterValue());
        // Gauge's cctor is triggered by a static-field access instead of a
        // call; read-modify-write accumulates in the patch static slot.
        Console.WriteLine("before first Gauge touch");
        Console.WriteLine(Gauge.Reading);
        Gauge.Reading = Gauge.Reading + 5;
        Gauge.Reading = Gauge.Reading + 5;
        Console.WriteLine(Gauge.Reading);
        // A patch type whose static initializer THROWS: the type stays
        // uninitialized, so the first touch raises the initializer's exception
        // and every later touch — a static-field access and a static call alike
        // — re-raises the SAME recorded failure instead of reading the
        // half-assigned Ready (41). Like the AOT guard (dn2cpp_cctor_run_once)
        // the failure propagates raw rather than wrapped in
        // TypeInitializationException, so the catches bind the initializer's own
        // exception type. The dotnet oracle crashes on the first touch: skip
        // this section when re-deriving the transcript.
        Console.WriteLine("before first Doomed touch");
        try
        {
            Console.WriteLine(Doomed.Ready);
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e.Message);
        }
        try
        {
            Console.WriteLine(Doomed.Ready);
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e.Message);
        }
        try
        {
            Console.WriteLine(Doomed.Peek());
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e.Message);
        }
        Console.WriteLine();

        // Registry visibility: the loader registers each constructed patch
        // type-info in the type-name registry, so the standard type-system
        // surfaces see patch types — GetType()/FullName/Name on a patch
        // instance, the default object ToString, isinst (as) and castclass
        // through base-typed variables in both the matching and failing
        // directions (a failed cast raises the catchable InvalidCastException
        // with the real .NET message), and Type.GetType(string) by name from
        // interpreted code (the base program repeats that lookup from AOT
        // code after the interpreter run).
        Console.WriteLine(tagged.GetType().FullName);
        Console.WriteLine(tagged.GetType().Name);
        Console.WriteLine(tagged.ToString());
        Counter poly = tagged;
        TaggedCounter back = poly as TaggedCounter;
        Console.WriteLine(back != null);
        DoubleCounter wrongAot = poly as DoubleCounter;
        Console.WriteLine(wrongAot != null);
        Counter plainAot = new Counter(1);
        Console.WriteLine((plainAot as TaggedCounter) != null);
        TaggedCounter cast = (TaggedCounter)poly;
        Console.WriteLine(cast.Tag());
        try
        {
            DoubleCounter bad = (DoubleCounter)poly;
            Console.WriteLine(bad.Describe());
        }
        catch (InvalidCastException e)
        {
            Console.WriteLine(e.Message);
        }
        // `is not null` (a reference comparison) rather than `!= null`:
        // Type.op_Inequality is a real BCL operator method, outside the
        // import surface.
        Type byName = Type.GetType("HotUpdatePatch.TaggedCounter");
        Console.WriteLine(byName is not null);
        Console.WriteLine(byName.FullName);
        Console.WriteLine();

        // Patch-deriving-patch types: GoldTagged derives from TaggedCounter
        // (itself a patch type deriving from the AOT Counter), so the ctor
        // chain runs interpreted → interpreted → AOT, inherited patch fields
        // and methods resolve against the base-first TypeTable order, and
        // Describe re-overrides the already-overridden slot. Dispatch reaches
        // the re-override from every direction — a direct call, a patch-side
        // callvirt through a patch-base-typed variable, and an AOT-internal
        // callvirt (Counter.Render) — while Silver, which re-overrides
        // nothing, inherits TaggedCounter's interpreted override through the
        // shared patched vtable. isinst answers across the whole chain.
        GoldTagged gold = new GoldTagged("gold", 4, 9);
        Console.WriteLine(gold.Tag());
        Console.WriteLine(gold.Weight);
        Console.WriteLine(gold.Bonus);
        Console.WriteLine(gold.Add(3));
        Console.WriteLine(gold.Describe());
        TaggedCounter goldAsTagged = gold;
        Console.WriteLine(goldAsTagged.Describe());
        Console.WriteLine(Counter.Render(gold));
        Console.WriteLine(gold.GetType().FullName);
        Console.WriteLine(gold is TaggedCounter);
        Console.WriteLine(goldAsTagged is GoldTagged);
        Silver silver = new Silver(2);
        Console.WriteLine(Counter.Render(silver));
        Console.WriteLine(silver.GetType().Name);
        Console.WriteLine();

        // SZArrays over the fenced element kinds. int[]: newarr, stelem/ldelem
        // through a loop (foreach lowers to the same ldlen/ldelem shape),
        // ldlen, an array flowing through a patch static method (array-typed
        // parameter and return) and parked in a patch static field.
        int[] nums = new int[5];
        for (int i = 0; i < nums.Length; i++)
            nums[i] = (i + 1) * (i + 1);
        int total = 0;
        foreach (int v in nums)
            total += v;
        Console.WriteLine(nums.Length);
        Console.WriteLine(nums[3]);
        Console.WriteLine(total);
        s_latest = Doubled(nums);
        Console.WriteLine(s_latest[4]);

        // string[] elements: writes, reads, a null default, and an array-typed
        // patch instance field behind an interpreted ctor.
        string[] words = new string[3];
        words[0] = "alpha";
        words[1] = "beta";
        words[2] = "gamma";
        Console.WriteLine(words[1]);
        Console.WriteLine(words.Length);
        string[] blank = new string[2];
        Console.WriteLine(blank[1] == null);
        Bag bag = new Bag(words);
        Console.WriteLine(bag.Count());
        Console.WriteLine(bag.At(2));

        // Patch-class arrays: newobj'd elements virtual-dispatch out of the
        // array (the patched vtable, from patch and AOT call sites alike), and
        // isinst/castclass on array types ride the runtime's array-covariance
        // arm (element assignability across the patch base chain).
        TaggedCounter[] team = new TaggedCounter[2];
        team[0] = new TaggedCounter("lead", 1);
        team[1] = new GoldTagged("aux", 2, 3);
        Counter[] baseView = team;
        Console.WriteLine(baseView.Length);
        Console.WriteLine(baseView[0].Describe());
        Console.WriteLine(Counter.Render(baseView[1]));
        Console.WriteLine(baseView is TaggedCounter[]);
        Console.WriteLine(baseView is DoubleCounter[]);
        TaggedCounter[] back2 = (TaggedCounter[])baseView;
        Console.WriteLine(back2[0].Tag());

        // Bounds and length faults surface as the catchable .NET exceptions.
        try
        {
            Console.WriteLine(nums[7]);
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine("index out of range");
        }
        int badLen = nums.Length - 8;
        try
        {
            int[] bad = new int[badLen];
            Console.WriteLine(bad.Length);
        }
        catch (OverflowException)
        {
            Console.WriteLine("negative array length");
        }

        // Arrays across the AOT boundary: an interpreter-allocated array into
        // an AOT method, and an AOT-allocated array back into patch code.
        Console.WriteLine(Counter.SumArray(nums));
        int[] series = Counter.Series(4);
        Console.WriteLine(series.Length);
        Console.WriteLine(series[3]);
        Console.WriteLine(Counter.SumArray(s_latest));

        // Element storage kinds: long/double/float widths plus the packed
        // bool (stelem.i1/ldelem.u1) and char (stelem.i2/ldelem.u2) forms.
        long[] ticks = new long[2];
        ticks[0] = 1L << 34;
        ticks[1] = ticks[0] / 5;
        Console.WriteLine(ticks[1]);
        double[] weights = new double[3];
        weights[0] = 1.5;
        weights[1] = 2.25;
        weights[2] = weights[0] * weights[1];
        Console.WriteLine(weights[2]);
        float[] ratios = new float[2];
        ratios[0] = 1.5f;
        ratios[1] = ratios[0] * ratios[0] + 0.25f;
        Console.WriteLine((double)ratios[1]);
        bool[] seen = new bool[3];
        seen[1] = nums[3] > 10;
        Console.WriteLine(seen[0]);
        Console.WriteLine(seen[1]);
        char[] letters = new char[2];
        letters[0] = 'o';
        letters[1] = 'k';
        Console.WriteLine(letters[0] < letters[1]);
        Console.WriteLine((int)letters[0] + letters[1]);
        Console.WriteLine();

        // Interfaces: patch types implement a base-image interface. A patch
        // implementation is reached through an interface-typed receiver from
        // both sides — a patch-side callvirt (note.Describe()) and an AOT method
        // that dispatches through the interface (Counter.RenderVia) — landing on
        // the interpreted body via an N2M interface trampoline; an AOT
        // implementer (Badge) called through the same interface stays on its AOT
        // body; isinst/as answer the interface across patch and AOT receivers;
        // and an explicit implementation (Ticket) resolves the same way.
        Console.WriteLine("interfaces");
        IDescribable note = new Note("hello", 7);
        Console.WriteLine(note.Describe());
        Console.WriteLine(note.Weight());
        Console.WriteLine(Counter.RenderVia(note));
        IDescribable badge = new Badge("gold", 3);
        Console.WriteLine(badge.Describe());
        Console.WriteLine(Counter.RenderVia(badge));
        Console.WriteLine(note is IDescribable);
        Console.WriteLine(counter is IDescribable);
        IDescribable ticket = new Ticket();
        Console.WriteLine(ticket.Describe());
        Console.WriteLine(ticket.Weight());
        Console.WriteLine(Counter.RenderVia(ticket));
        Console.WriteLine((note as IDescribable) is not null);
        Console.WriteLine((counter as IDescribable) is not null);
        Console.WriteLine();

        // Delegates: a patch method bound to a base-image delegate type and
        // invoked from the patch — a static one (ldftn a patch method → the
        // delegate's interpreter thunk), and an instance one (the receiver is
        // captured into the closure). A patch delegate handed to an AOT helper
        // (ApplyTwice/Each) so the AOT-side Invoke lands on the interpreted body
        // through the thunk. An AOT instance method bound into a delegate and
        // invoked from the patch (the natural AOT path — f_method is the method
        // pointer, no interpreter hop). And delegates stored in an array and a
        // patch instance field and invoked later. (Bodies stay off String.Concat
        // — an unbound intrinsic on the patch surface — so they print through the
        // Console.WriteLine(int/string) intrinsics.)
        Console.WriteLine("delegates");
        IntTransform triple = Triple;
        Console.WriteLine(triple(7));
        Scaler scaler = new Scaler(10);
        IntTransform scale = scaler.Scale;
        Console.WriteLine(scale(4));
        Console.WriteLine(Counter.ApplyTwice(triple, 2));
        int[] src = new int[3];
        src[0] = 5;
        src[1] = 6;
        src[2] = 7;
        Counter.Each(src, Show);
        Counter addend = new Counter(100);
        IntTransform aotAdd = addend.Add;
        Console.WriteLine(aotAdd(5));
        IntTransform[] fns = new IntTransform[2];
        fns[0] = triple;
        fns[1] = scale;
        Console.WriteLine(fns[0](3));
        Console.WriteLine(fns[1](3));
        Pipeline pipe = new Pipeline(triple);
        Console.WriteLine(pipe.RunOn(6));
        Describer greeter = PatchGreeting;
        Console.WriteLine(greeter());
        Console.WriteLine();

        // Generics: a base-image closed generic type (Holder<int>)
        // constructed and driven through the patch — its mangled instantiation
        // binds by name against the base ABI manifest, so member calls resolve
        // on the monomorphized type with no runtime generic machinery. A
        // Holder<string> the base program never uses (emitted only through
        // hotupdate-refs.txt — the AOTGenericReferences pre-reference). And a
        // generic delegate (Mapper<int>) bound to a patch method and invoked
        // through the delegate's pre-emitted interpreter thunk (the
        // generic-delegate rejection is now a manifest-gated bind).
        Console.WriteLine("generics");
        Holder<int> box = new Holder<int>(40);
        box.Replace(box.Value() + 2);
        Console.WriteLine(box.Value());
        Console.WriteLine(box.Shape());
        Holder<string> tag = new Holder<string>("gen");
        Console.WriteLine(tag.Value());
        Mapper<int> quad = Quad;
        Console.WriteLine(quad(5));
        // Generic methods: a base-image generic method (Counter.Echo<T>)
        // the base program never calls — its closed instantiations come in only
        // through the hotupdate-refs.txt method roots — invoked at two type
        // arguments. Both emit under the one name "Echo", so the loader binds each
        // MethodSpecification import to the right instantiation by its sigShape.
        Console.WriteLine(Counter.Echo<int>(7));
        Console.WriteLine(Counter.Echo<string>("gen2"));
        Console.WriteLine();

        // String building: the String.Concat overloads Roslyn lowers string-only
        // `+` chains and string-holed interpolation to — the 2/3/4-operand typed
        // overloads, the params string[] fallback for 5+ operands, and a null
        // operand treated as empty.
        Console.WriteLine("strings");
        string who = "world";
        Console.WriteLine("hello " + who);
        string sep = "-";
        Console.WriteLine(who + sep + "wide");
        Console.WriteLine($"greetings, {who}{sep}wide");
        Console.WriteLine("a" + sep + who + sep + "z");
        string missing = null;
        Console.WriteLine("null is empty:" + missing + "!");
        Console.WriteLine();

        // Derived-exception ctor bodies: a patch exception deriving from a
        // base-image exception must run its OWN ctor body — not just seed the
        // Message — so a field the derived ctor writes lands. PatchIoEx derives
        // from a BCL exception (its base(message) chains through the
        // kShapeExceptionNew call arm); PatchEx derives directly from the opaque
        // System.Exception (base(message) is seed-only). Both are caught by a
        // base-image type (catch clauses bind only base-image types) and cast down
        // to read the derived field.
        Console.WriteLine("exception ctors");
        try
        {
            throw new PatchIoEx("io boom", 13);
        }
        catch (InvalidOperationException e)
        {
            PatchIoEx io = (PatchIoEx)e;
            Console.WriteLine(io.Message);          // io boom (base(message) seed)
            Console.WriteLine(io.Code);             // 13 (derived ctor body ran)
        }
        try
        {
            throw new PatchEx("plain boom");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);          // plain boom (opaque seed-only)
            Console.WriteLine(((PatchEx)e).Tag);   // t (derived ctor body)
        }
        Console.WriteLine();

        // The RESOLVED-base-ctor invoker path, which only an app-module
        // (non-opaque) exception base can exercise: QuotaEx is a base-image
        // exception whose own base is the External BCL System.SystemException,
        // so PatchQuotaEx's interpreted `base(message, quota)` binds
        // kShapeExceptionNew with a real fnPtr (the emitted QuotaEx ctor +
        // invoker). The (string, int) shape is positionally unrecognized
        // (excMsgArg -1), so the Message the catch reads can ONLY have been
        // written by the resolved AOT ctor body running — seed-only would leave
        // it null. Describe() is an AOT reader over the exception prefix + the
        // derived Quota field, reached through an ordinary method import.
        Console.WriteLine("external-base exception");
        try
        {
            throw new PatchQuotaEx("patch quota", 21);
        }
        catch (QuotaEx e)
        {
            Console.WriteLine(e.Message);                // patch quota (resolved body wrote it)
            Console.WriteLine(e.Describe());             // patch quota/none#21
            Console.WriteLine(((PatchQuotaEx)e).Origin); // patch (interpreted ctor body ran)
        }
        // The newobj arm of the same resolved path: constructing the base-image
        // type directly from the patch. dn2cpp_exception_new allocates and
        // stamps ti_QuotaEx, the (string, Exception) shape seeds message+inner
        // positionally, then the resolved real ctor body runs (Quota = -8).
        try
        {
            throw new QuotaEx("patch outer", new InvalidOperationException("patch inner"));
        }
        catch (QuotaEx e)
        {
            Console.WriteLine(e.Describe());             // patch outer/patch inner#-8
        }
        Console.WriteLine();

        // Object-inherited intrinsics through a DERIVED declaring type:
        // `base.ToString()` from a non-override method is a non-virtual call
        // Roslyn stamps against the nearest override at or above the base type
        // — `System.Exception::ToString()` — so the import's declaring type is a
        // base-image type BELOW the intrinsic row's System.Object, and the
        // binder resolves it through the intrinsic table's base-chain fallback.
        // An unthrown instance keeps the output trace-free ("FullTypeName:
        // Message"), identical on real .NET.
        Console.WriteLine("derived intrinsics");
        PatchIoEx probe = new PatchIoEx("probe", 3);
        Console.WriteLine(probe.Render());
        Console.WriteLine();

        // Exception handling: a typed catch receiving the thrown message.
        try
        {
            throw new InvalidOperationException("patch says boom");
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e.Message);
        }

        // finally on the normal path: a single return crossing two nested
        // finally regions runs them innermost-first.
        Console.WriteLine(DoubleFinally());

        // finally on the exceptional path: the inner finally runs before the
        // outer catch handler gets control.
        try
        {
            try
            {
                throw new InvalidOperationException("cross the finally");
            }
            finally
            {
                Console.WriteLine("finally on unwind");
            }
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e.Message);
        }

        // Nested try + rethrow: the inner handler re-raises the same object
        // for the outer, differently-typed handler.
        try
        {
            try
            {
                throw new NotSupportedException("rethrown");
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("inner catch");
                throw;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        // An exception thrown inside an AOT base-image method unwinds the
        // invoker boundary into a patch handler; the failed call leaves the
        // counter untouched.
        try
        {
            counter.Deduct(1000);
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e.Message);
        }
        Console.WriteLine(counter.Deduct(4));

        NullFaults();
        FourFaults();
        DelegateNullFaults();

        // An exception no patch handler consumes escapes the interpreter into
        // the base program's own try/catch (see HotUpdateBase.Program.Main).
        throw new InvalidOperationException("escaped to base");
    }

    // The interpreter's own null checks, each caught where it is raised.
    // Every guard below lives in the dispatch loop rather than in a runtime
    // helper: the receiver of a patch call, of an imported AOT call, of an
    // instance intrinsic and of a delegate invoke; the object of a patch and
    // of an imported field load and store; and the array of ldlen, ldelem and
    // stelem. Real .NET answers all eleven with a catchable
    // NullReferenceException and keeps running, so a runtime that ends the
    // process at one of them cannot print the rest of this transcript at all.
    // The probes are reached from Main, so the gate's stack-format replay
    // drives every one of them through the v1 dispatch loop as well.
    //
    // Two sibling guards are deliberately absent, because C# cannot reach
    // them: a `.ctor` import is only ever called on a receiver newobj has
    // just allocated or on `this`, so its null check has no source-level
    // shape. The "no throw" lines exist so a guard that stops raising is a
    // diff rather than a silently shorter transcript.
    private static void NullFaults()
    {
        TaggedCounter? nullTagged = null;
        Counter? nullCounter = null;
        int[]? nullInts = null;
        IntTransform? nullXform = null;
        Exception? nullEx = null;

        try
        {
            Console.WriteLine(nullTagged!.Tag());
            Console.WriteLine("no throw: patch call");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: patch call");
        }

        try
        {
            Console.WriteLine(nullTagged!.Weight);
            Console.WriteLine("no throw: patch field load");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: patch field load");
        }

        try
        {
            nullTagged!.Weight = 1;
            Console.WriteLine("no throw: patch field store");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: patch field store");
        }

        try
        {
            Console.WriteLine(nullCounter!.Add(1));
            Console.WriteLine("no throw: base call");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: base call");
        }

        try
        {
            Console.WriteLine(nullCounter!.Step);
            Console.WriteLine("no throw: base field load");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: base field load");
        }

        try
        {
            nullCounter!.Step = 2;
            Console.WriteLine("no throw: base field store");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: base field store");
        }

        try
        {
            Console.WriteLine(nullInts!.Length);
            Console.WriteLine("no throw: array length");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: array length");
        }

        try
        {
            Console.WriteLine(nullInts![0]);
            Console.WriteLine("no throw: array load");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: array load");
        }

        try
        {
            nullInts![0] = 1;
            Console.WriteLine("no throw: array store");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: array store");
        }

        try
        {
            Console.WriteLine(nullXform!(3));
            Console.WriteLine("no throw: delegate invoke");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: delegate invoke");
        }

        try
        {
            Console.WriteLine(nullEx!.Message);
            Console.WriteLine("no throw: intrinsic call");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: intrinsic call");
        }
    }

    // Four runtime-raised exception types that carry runtime type-info and must
    // also have a row in the type name registry. The probe is the CATCH CLAUSE,
    // not the throw: a BPI catch clause is a type import bound by name against
    // dn2cpp_type_registry, and an unresolved one binds to null WITHOUT failing
    // the load, so the damage lands later and harder than a rejected image —
    // eh_catch_matches fails the first time the clause is tested against a
    // thrown object, i.e. a patch that never faults runs clean and one that
    // faults aborts on its recovery path. Each fault is raised by AOT base code
    // (HotUpdateBase.Faults) so the object carries the shared runtime handle the
    // dn2cpp_throw_* helpers raise, which is the handle the registry row has to
    // hand back: a row naming any other Dn2CppTypeInfo of the same name would
    // let every clause below bind and then never match. The "no throw" lines
    // exist so a raiser that stops raising is a diff rather than a silently
    // shorter transcript.
    private static void FourFaults()
    {
        try
        {
            Console.WriteLine(Faults.SignOf(double.NaN));
            Console.WriteLine("no throw: arithmetic");
        }
        catch (ArithmeticException)
        {
            Console.WriteLine("fault: arithmetic");
        }

        try
        {
            Faults.MissingMethod();
            Console.WriteLine("no throw: missing method");
        }
        catch (MissingMethodException)
        {
            Console.WriteLine("fault: missing method");
        }

        try
        {
            Faults.Disposed();
            Console.WriteLine("no throw: object disposed");
        }
        catch (ObjectDisposedException)
        {
            Console.WriteLine("fault: object disposed");
        }

        try
        {
            Faults.Unsupported();
            Console.WriteLine("no throw: platform not supported");
        }
        catch (PlatformNotSupportedException)
        {
            Console.WriteLine("fault: platform not supported");
        }
    }

    // The null tests the dispatch-loop guards above cannot make. `ldftn` hands a
    // base-image instance method's raw AOT pointer (an intrinsic wrapper's
    // included) to the delegate `newobj` as the delegate's f_method, so an
    // Invoke never crosses the loop's receiver guard: the only interceptable
    // point is delegate CONSTRUCTION, which is also where real .NET refuses a
    // null `this` (an ArgumentException, NOT a NullReferenceException). The NRE
    // catches document the second layer — the intrinsic wrappers guard their own
    // receiver, so even a construction-guard regression answers the intrinsic
    // probe with a catchable NRE instead of a segfault at o->type. The AOT probe
    // (Counter.Add) has no second layer, an emitted body dereferencing `this`
    // unguarded, which is why the construction check is the load-bearing one.
    // The ToString probe pins the dispatch-mouth alignment: a null receiver
    // throws, never folds to "".
    private static void DelegateNullFaults()
    {
        // Both receivers are Counter (not object): a System.Object local is
        // outside the baked local surface, and the method-group conversion
        // binds Object::GetType / Object::ToString off a Counter receiver
        // identically.
        Counter? nullObj = null;
        Counter? nullCounter = null;

        try
        {
            TypeGetter f = nullObj!.GetType;
            Console.WriteLine("no throw: delegate over null this (intrinsic) " + f().FullName);
        }
        catch (ArgumentException)
        {
            Console.WriteLine("arg: delegate over null this (intrinsic)");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: delegate over null this (intrinsic)");
        }

        try
        {
            IntTransform g = nullCounter!.Add;
            Console.WriteLine("no throw: delegate over null this (aot)");
            Console.WriteLine(g(1));
        }
        catch (ArgumentException)
        {
            Console.WriteLine("arg: delegate over null this (aot)");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: delegate over null this (aot)");
        }

        // The third delegate route, and the one whose refusal has to be derived:
        // `ldftn` on a PATCH method yields a kDgFtnPatch closure whose captured
        // receiver is legitimately null for a STATIC patch method, and whose
        // instance-ness the BPI method record does not carry. So the
        // construction guard reads the delegate type's own Invoke arity out of
        // the base image's method table and refuses only when the patch method's
        // argCount includes a `this` — here Scaler.Scale(int) is 2 against
        // IntTransform.Invoke(int)'s 1, so this probe reports ArgumentException
        // like the two above. The static-target side of the same derivation is
        // asserted positively by `IntTransform triple = Triple;` earlier in the
        // transcript, which must keep working.
        try
        {
            Scaler? nullScaler = null;
            IntTransform s = nullScaler!.Scale;
            Console.WriteLine("built: delegate over null this (patch)");
            Console.WriteLine(s(1));
            Console.WriteLine("no throw: delegate over null this (patch)");
        }
        catch (ArgumentException)
        {
            Console.WriteLine("arg: delegate over null this (patch)");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: delegate over null this (patch)");
        }

        try
        {
            Console.WriteLine(nullObj!.ToString());
            Console.WriteLine("no throw: tostring on null");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("nre: tostring on null");
        }

        // The good path stays good: the same ldftn route over a live receiver
        // invokes the intrinsic through the delegate.
        Counter live = new Counter(1);
        TypeGetter h = live.GetType;
        Console.WriteLine("delegate GetType: " + h().FullName);
    }

    private static int DoubleFinally()
    {
        try
        {
            try
            {
                Console.WriteLine("body");
                return 7;
            }
            finally
            {
                Console.WriteLine("finally A");
            }
        }
        finally
        {
            Console.WriteLine("finally B");
        }
    }

    private static int Fib(int n)
    {
        if (n < 2)
            return n;
        return Fib(n - 1) + Fib(n - 2);
    }

    private static int SumTo(int n)
    {
        int sum = 0;
        for (int i = 1; i <= n; i++)
            sum += i;
        return sum;
    }

    private static long Cube(long v)
    {
        return v * v * v;
    }

    private static void Announce(string message)
    {
        Console.WriteLine(message);
    }

    private static string Describe(int v)
    {
        return v == 0 ? "zero" : "nonzero";
    }

    // Array-typed parameter and return on a patch method.
    private static int[] Doubled(int[] source)
    {
        int[] result = new int[source.Length];
        for (int i = 0; i < source.Length; i++)
            result[i] = source[i] * 2;
        return result;
    }

    // Delegate targets: a static IntTransform, a static IntSink (prints through
    // the Console.WriteLine(int) intrinsic — no String.Concat), and a
    // parameterless Describer.
    private static int Triple(int x)
    {
        return x * 3;
    }

    // A generic-delegate (Mapper<int>) target bound by the patch.
    private static int Quad(int x)
    {
        return x * 4;
    }

    private static void Show(int v)
    {
        Console.WriteLine(v);
    }

    private static string PatchGreeting()
    {
        return "patch-greeting";
    }
}

// A patch class whose instance method is bound into a delegate: the receiver
// (its captured factor) is closed over into the interpreter closure, so the
// delegate's Invoke runs Scale with `this` restored.
internal sealed class Scaler
{
    private readonly int _factor;

    public Scaler(int factor)
    {
        _factor = factor;
    }

    public int Scale(int x)
    {
        return x * _factor;
    }
}

// A patch class holding a delegate in an instance field (a loader-assigned
// reference slot) and invoking it later through an interpreted method.
internal sealed class Pipeline
{
    private readonly IntTransform _f;

    public Pipeline(IntTransform f)
    {
        _f = f;
    }

    public int RunOn(int x)
    {
        return _f(x);
    }
}

// A patch class holding an array in an instance field (a loader-assigned
// reference slot) and reading elements through interpreted instance methods.
internal sealed class Bag
{
    private readonly string[] _items;

    public Bag(string[] items)
    {
        _items = items;
    }

    public int Count()
    {
        return _items.Length;
    }

    public string At(int index)
    {
        return _items[index];
    }
}

// Patch static state behind an explicit static constructor (no
// beforefieldinit, so the initialization point is deterministic on real .NET
// too): scalar, string, and AOT-reference-type static fields with inline
// initializers plus cctor side effects, including a re-entrant touch of the
// type's own statics while the cctor is still running.
internal static class Registry
{
    public static string Tag;

    private static int s_ticket = 100;
    private static Counter s_counter;

    static Registry()
    {
        Console.WriteLine("Registry cctor: begin");
        Tag = "registry";
        s_counter = new Counter(1000);
        // Re-enters the type while the cctor is running: Bump reads Tag —
        // the guard must neither deadlock nor re-run the initializer.
        s_ticket = s_ticket + Bump(11);
        Console.WriteLine(s_ticket);
        Console.WriteLine("Registry cctor: end");
    }

    public static int NextTicket()
    {
        s_ticket = s_ticket + 1;
        return s_ticket;
    }

    public static int CounterValue()
    {
        return s_counter.Add(24);
    }

    private static int Bump(int by)
    {
        Console.WriteLine(Tag);
        return by;
    }
}

// A second stateful patch type whose cctor is triggered by a static-field
// access rather than a call.
internal static class Gauge
{
    public static int Reading = 41;

    static Gauge()
    {
        Console.WriteLine("Gauge cctor");
    }
}

// A patch type whose explicit static constructor throws after assigning one
// static: the interpreter's cctor guard records the failure, leaves the type
// uninitialized, and re-raises the same exception on every later touch — never
// exposing the half-assigned Ready. Mirrors the AOT dn2cpp_cctor_run_once
// semantics (raw propagation, no TypeInitializationException wrapping).
internal static class Doomed
{
    public static int Ready;

    static Doomed()
    {
        Ready = 41;
        throw new InvalidOperationException("doomed cctor");
    }

    public static int Peek()
    {
        return Ready;
    }
}

// A patch class deriving from an AOT base-image class: its ctor chains into
// the base ctor (whose TotalCounters side effect is printed from the derived
// body to assert the order), its own string/int fields live in loader-assigned
// slots after the live base layout, and inherited base state and methods are
// reached through the ordinary import bindings. Describe overrides the base's
// virtual method: the loader rewrites the copied vtable slot to an N2M
// trampoline, so AOT and patch callvirt sites alike land on the interpreted
// body — which calls back into the AOT base implementation (a non-virtual
// call import) and, past the weight limit, throws across the AOT caller frame.
internal class TaggedCounter : Counter
{
    private readonly string _tag;
    public int Weight;

    public TaggedCounter(string tag, int start)
        : base(start)
    {
        Console.WriteLine("TaggedCounter ctor after base");
        Console.WriteLine(Counter.TotalCounters);
        _tag = tag;
        Weight = start * 2;
    }

    public string Tag()
    {
        return _tag;
    }

    public override string Describe()
    {
        if (Weight >= 100)
            throw new InvalidOperationException("weight limit");
        Console.WriteLine("TaggedCounter.Describe intercepts");
        return base.Describe();
    }
}

// A patch class deriving from another patch class: its ctor chains through
// the interpreted TaggedCounter ctor into the AOT Counter ctor, Bonus lives
// after TaggedCounter's loader-assigned layout, the inherited patch members
// (Tag, Weight) resolve like any patch member, and Describe re-overrides the
// slot TaggedCounter already overrides — the vtable copy starts from the
// patch base's patched vtable, and the interpreter's slot resolution walks
// the receiver's patch chain, so the deepest override wins from AOT and
// patch call sites alike.
internal class GoldTagged : TaggedCounter
{
    public int Bonus;

    public GoldTagged(string tag, int start, int bonus)
        : base(tag, start)
    {
        Bonus = bonus;
    }

    public override string Describe()
    {
        Console.WriteLine("GoldTagged.Describe adds a bonus");
        Console.WriteLine(Bonus);
        return base.Describe();
    }
}

// A patch-derived class that re-overrides nothing: it shares its patch base's
// patched vtable, so virtual dispatch inherits TaggedCounter's interpreted
// override (the slot resolution walks up to the declaring ancestor).
internal sealed class Silver : TaggedCounter
{
    public Silver(int start)
        : base("silver", start)
    {
    }
}

// A patch class rooted directly at object with mixed-width instance fields
// (4-byte int + 8-byte long exercises the loader's alignment run) and an
// instance method mutating its own state; its ctor chain to
// System.Object::.ctor() folds away at bake time.
internal sealed class Pair
{
    public int A;
    private long _b;

    public Pair(int a, long b)
    {
        A = a;
        _b = b;
    }

    public long Sum()
    {
        return A + _b;
    }

    public void Bump(int by)
    {
        A = A + by;
    }
}

// A patch class implementing a base-image interface with implicit (name +
// signature) implementations. Its Describe/Weight are new virtual slots that
// fill IDescribable's interface-dispatch slots (not a class vtable slot); the
// loader wires an N2M interface trampoline into the patch type's interface map,
// so an interface-typed call — from patch code or an AOT method that dispatches
// through the interface — lands on the interpreted body.
internal sealed class Note : IDescribable
{
    private readonly string _text;
    private readonly int _w;

    public Note(string text, int w)
    {
        _text = text;
        _w = w;
    }

    public string Describe()
    {
        // Interpreted bodies stay off String.Concat (an unbound intrinsic on the
        // patch surface); RenderVia does the AOT-side formatting.
        return _text;
    }

    public int Weight()
    {
        return _w;
    }
}

// A patch class implementing the same interface explicitly (`IDescribable.M`):
// the interface-slot resolution rides the .override MethodImpl rows rather than
// name matching, but the dispatch path is identical.
internal sealed class Ticket : IDescribable
{
    string IDescribable.Describe()
    {
        return "ticket";
    }

    int IDescribable.Weight()
    {
        return 42;
    }
}

// The interleave surface for gates/build-and-run-shadow-stack.sh arm 3: a patch
// override that re-enters AOT code before the throw, so the frozen kind-1 trace
// shows interpreted frames SANDWICHED between AOT frames. Counter.Render (AOT)
// dispatches Describe onto this patch type (interp), which calls
// Counter.RenderVia (AOT) on a second patch type whose Describe (interp) throws,
// leaving the shadow stack reading interp, AOT, interp, AOT innermost-first.
// Constructed only on the ShadowProbeRequest branch above, so no other mode's
// transcript changes.
internal sealed class ShadowProbe : Counter
{
    // Constructed in the ctor, not in Describe: the converter bakes a
    // non-virtual method (ShadowNote's implicit .ctor) only when the
    // reachability walk sees it, and the walk enters this type through the
    // reachable ctor — Describe itself is baked as a virtual override, dispatch
    // being its only caller.
    private readonly ShadowNote _note;

    public ShadowProbe()
        : base(1)
    {
        _note = new ShadowNote();
    }

    public override string Describe()
    {
        return Counter.RenderVia(_note);
    }
}

// The second interp hop: an IDescribable whose Describe IS the patch method
// that throws (directly — a private static helper would not bake: the
// converter bakes a non-virtual method only when the reachability walk sees
// it, and this body's only caller is interface dispatch). RenderVia evaluates
// d.Describe() before d.Weight(), so Weight is never reached.
internal sealed class ShadowNote : IDescribable
{
    public string Describe()
    {
        throw new InvalidOperationException("shadow probe boom");
    }

    public int Weight()
    {
        return 0;
    }
}

// A patch exception deriving from a base-image exception
// (InvalidOperationException): its ctor chains `base(message)` through the
// kShapeExceptionNew call arm, resolving the real (non-opaque)
// InvalidOperationException(string) body, then writes its own field. Message
// comes from the seeded/base-run prefix, Code from this derived ctor body;
// caught by the base type and cast down to read Code.
internal sealed class PatchIoEx : InvalidOperationException
{
    public int Code;

    public PatchIoEx(string message, int code)
        : base(message)
    {
        Code = code;
    }

    // A non-virtual `base.ToString()` outside any override — the token Roslyn
    // emits names the nearest override at or above the base type,
    // System.Exception::ToString(), an Object-inherited intrinsic imported
    // through a derived declaring type (the base-chain-fallback bind).
    public string Render()
    {
        return base.ToString();
    }
}

// A patch exception over a base-image exception whose base is the External BCL
// System.SystemException (QuotaEx). `base(message, quota)` resolves to the REAL
// emitted QuotaEx ctor body — the resolved-base-ctor invoker path (fnPtr
// non-null in exc_seed_and_run_base_ctor), which a BCL exception base can never
// exercise: its runtime handle carries no ctor bodies, so it can only seed.
internal sealed class PatchQuotaEx : QuotaEx
{
    public string Origin;

    public PatchQuotaEx(string message, int quota)
        : base(message, quota)
    {
        Origin = "patch";
    }
}

// A patch exception deriving directly from System.Exception (an opaque intrinsic):
// `base(message)` has no real ctor body to run (fnPtr null), so the Message is a
// seed-only degrade — the derived ctor still sets Tag.
internal sealed class PatchEx : Exception
{
    public string Tag;

    public PatchEx(string message)
        : base(message)
    {
        Tag = "t";
    }
}
