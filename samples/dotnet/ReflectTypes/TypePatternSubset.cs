// type patterns (`o is int n`, `o switch { int n => … }`) over a boxed
// value type. `isinst <valueType>` yields the BOXED reference (or null) — a
// subsequent `unbox.any` recovers the value — so the result stack type is an
// object reference, not the unboxed primitive. The transpiler used to type that
// slot as the unboxed C++ type and cast the pointer to it, truncating the boxed
// reference and miscompiling the null test that drives every type pattern.
using System;

// Namespaced rather than global so it cannot collide with the bucket's driver.
// Nothing here prints a type NAME (only `is` answers and lengths), so the namespace
// does not reach the output.
namespace TypePatternSubset;

enum Team { Red, Blue, Green }

class Program
{
    static string Classify(object o) => o switch
    {
        int n when n > 0 => "pos:" + n,
        int => "nonpos",
        string s => "str:" + s,
        double d => "dbl:" + d,
        null => "null",
        _ => "other",
    };

    internal static void Run()
    {
        Console.WriteLine(Classify(5));
        Console.WriteLine(Classify(-2));
        Console.WriteLine(Classify("hi"));
        Console.WriteLine(Classify(1.5));
        Console.WriteLine(Classify(null));
        Console.WriteLine(Classify('z'));

        // `is` patterns with a captured binding over boxed value types.
        object a = 42;
        if (a is int v)
            Console.WriteLine("isint:" + v);
        Console.WriteLine(a is string);
        Console.WriteLine(a is long);

        object b = "text";
        Console.WriteLine(b is int);
        if (b is string bs)
            Console.WriteLine("len:" + bs.Length);

        // is over an enum value (also a value type).
        object e = Team.Green;
        Console.WriteLine(e is Team t ? "team:" + (int)t : "no");

        // isinst / castclass over the abstract System.Array base and precise array
        // handles. Every array is a System.Array, but array type-infos carry
        // base=nullptr, so `(array) is Array` reached nothing on the base chain and
        // would answer false for every array — a silent wrong answer. The
        // abstract-Array target is recognized through a type-info flag instead.
        object arr = new int[] { 1, 2, 3 };
        Console.WriteLine("arr is Array=" + (arr is Array));       // True
        Console.WriteLine("arr is object=" + (arr is object));     // True
        Console.WriteLine("arr is int[]=" + (arr is int[]));       // True (exact)
        Console.WriteLine("arr is string[]=" + (arr is string[])); // False
        object strarr = new string[] { "x" };
        Console.WriteLine("strarr is Array=" + (strarr is Array));       // True
        Console.WriteLine("strarr is object[]=" + (strarr is object[])); // True (covariant)
        object md = new int[2, 2];
        Console.WriteLine("md is Array=" + (md is Array));    // True (mdarray too)
        Console.WriteLine("md is int[,]=" + (md is int[,]));  // True (exact rank-2)
        object boxed = 5;
        Console.WriteLine("box is Array=" + (boxed is Array)); // False (not an array)
        // A failed castclass to System.Array raises InvalidCastException.
        try
        {
            var bad = (Array)boxed;
            Console.WriteLine("no throw:" + bad.Length);
        }
        catch (InvalidCastException)
        {
            Console.WriteLine("ICE");
        }

        // Thrive blocker #9: a typeof/isinst/castclass/box/array-covariance lowering
        // names an INTRINSIC-modeled type's own &ti_<T> (StringBuilder, CultureInfo,
        // WaitHandle, IFormatProvider, the SIMD vectors), but the opaque referenced-type
        // pass skips intrinsic classes — so the ti_ was named yet never emitted: an
        // undeclared identifier at C++ link inside an all-green transpile. Reference
        // intrinsics reach the bare ti_ through the inline Span<T>(T[]) covariance guard
        // (it names typeof(T)); an intrinsic VALUE type reaches it through box, which
        // (unlike ldtoken for non-intrinsic value types) never noted the type referenced.
        var sbArr = new System.Text.StringBuilder[] { new(), new() };
        Span<System.Text.StringBuilder> sbSpan = sbArr;
        Console.WriteLine("sbSpan:" + sbSpan.Length);
        var ciArr = new System.Globalization.CultureInfo[1];
        Span<System.Globalization.CultureInfo> ciSpan = ciArr;
        Console.WriteLine("ciSpan:" + ciSpan.Length);
        var whArr = new System.Threading.WaitHandle[1];
        Span<System.Threading.WaitHandle> whSpan = whArr;
        Console.WriteLine("whSpan:" + whSpan.Length);
        IFormatProvider[] fpArr = new IFormatProvider[1];
        Span<IFormatProvider> fpSpan = fpArr;
        Console.WriteLine("fpSpan:" + fpSpan.Length);
        var vec = System.Runtime.Intrinsics.Vector128.Create((byte)7);
        object vbox = vec;
        Console.WriteLine("v128box:" + (vbox is System.Runtime.Intrinsics.Vector128<byte>));

        // The sibling of #9, over C++ STRUCT types rather than
        // type-infos. A body names a type's t_ struct as a C++ type — a field-access cast, a
        // dispatch fn-ptr signature, a sizeof — while nothing emits it, and the transpile
        // succeeds while the C++ compile fails on the undeclared t_. Three naming contexts,
        // each a reduced form of a real Thrive failure, backstopped by AssertNamedStructsDefined.

        // (a) A transpiled BCL body pointer-form-field-reads an INTRINSIC-modeled type: the
        // AsyncLocal<T>.Value getter reaches ExecutionContext.GetLocalValue, whose real body
        // reads the intrinsic Thread's private _executionContext — casting to
        // t_System_Threading_Thread, which has no emitted struct. The ExecutionContext flow is
        // not modeled (Capture == null), so GetLocalValue/SetLocalValue are cut to the null
        // "nothing flowed" answer; a never-set AsyncLocal reads null in dn2cpp and .NET alike.
        var al = new System.Threading.AsyncLocal<string>();
        Console.WriteLine("asynclocal:" + (al.Value ?? "null"));

        // (b) Array.Empty<T> over a value struct reached through no other edge: the sizeof(t_T)
        // names the struct while nothing instantiates T. ParameterModifier is the Thrive case;
        // it must emit T's full layout for the sizeof.
        var pm = Array.Empty<System.Reflection.ParameterModifier>();
        Console.WriteLine("emptylen:" + pm.Length);

        // (c) A dispatched interface method whose signature names otherwise-unemitted types:
        // IAsyncEnumerable<int>.GetAsyncEnumerator returns IAsyncEnumerator<int> (an interface
        // named nowhere else) and takes a CancellationToken; the dispatch fn-ptr spells both as
        // t_. Null-guarded so it links but never runs — the naming is static (reachability),
        // the answer identical in dn2cpp and .NET.
        System.Collections.Generic.IAsyncEnumerable<int> src = null;
        var enumr = src is not null ? src.GetAsyncEnumerator(default) : null;
        Console.WriteLine("asyncenum:" + (enumr is null ? "null" : "x"));
    }
}
