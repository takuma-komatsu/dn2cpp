using System;

namespace GenericSignatureRecursionBad;

/// <summary>A pair, so the recursion below deepens without depending on ValueTuple.</summary>
public struct Pair<TFirst, TSecond>
{
    public TFirst First;
    public TSecond Second;
}

/// <summary>The second AOT-hostile monomorphization shape: a generic type one of whose MEMBER
/// SIGNATURES names a deeper instantiation of its own declaring type. <c>Deeper</c> is never
/// called — <c>Main</c> only ever mentions <c>Box&lt;int&gt;</c> — so an AOT compiler that
/// decodes a closed generic's member signatures to lay the type out instantiates
/// <c>Box&lt;Pair&lt;bool, int&gt;&gt;</c>, whose own <c>Deeper</c> instantiates
/// <c>Box&lt;Pair&lt;bool, Pair&lt;bool, int&gt;&gt;&gt;</c>, without end.
///
/// This is not contrived: it is exactly GDTask/UniTask's
/// <c>GDTask&lt;T&gt;.SuppressCancellationThrow() -> GDTask&lt;(bool IsCanceled, T Result)&gt;</c>,
/// which needs only <c>-r GDTask.dll</c> — no GDTask code anywhere in the program — to run
/// the transpiler's heap away.
///
/// It is a distinct hazard from GenericRecursionBad's, not a restatement: that one is driven
/// by a CALL, so it shows up in the reach chain and dropping the call removes it. This one has
/// no call to drop, and is HANDLED rather than bounded — a method's signature is decoded on
/// demand, nothing reaches <c>Deeper</c>, and the deeper <c>Box</c> is never named. So the
/// transpile COMPLETES and stops after exactly one step, which the transpiler-limits gate
/// asserts and which makes this program the suite's proof that the deferral is real. The name
/// keeps its <c>Bad</c> for what the shape IS; the shapes that must still be REFUSED are
/// GenericRecursionBad (call-driven) and GenericFieldRecursionBad (field-driven).</summary>
public struct Box<T>
{
    public T Value;

    public Box<Pair<bool, T>> Deeper() => default;
}

/// <summary>The <c>--cut</c> lever's test surface: <c>Tracked</c> is genuinely CALLED
/// and drags a private subtree (<c>Helper</c>) only it reaches. The transpiler-limits
/// gate transpiles this program a second time with
/// <c>--cut GenericSignatureRecursionBad.Tracker::Tracked</c> and asserts the whole
/// subtree fell out of the generated C++ while the neutralized call site yielded the
/// default result (null, printed as "cut").</summary>
internal static class Tracker
{
    internal static string Tracked()
    {
        Helper();
        return "tracked";
    }

    private static void Helper() => Console.WriteLine("tracker-helper");
}

internal static class Program
{
    private static void Main()
    {
        var b = new Box<int> { Value = 1 };
        Console.WriteLine(b.Value);
        Console.WriteLine(Tracker.Tracked() ?? "cut");
    }
}
