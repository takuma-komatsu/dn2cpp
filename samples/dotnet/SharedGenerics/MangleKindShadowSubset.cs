#nullable enable
// The arg-mangle's injectivity over the type-CONSTRUCTOR kinds — MDArray, Pointer
// and the array closures over both — and over the global-namespace classes that
// could spell their fragments. Sibling of PrimitiveNameShadowSubset, which covers
// the primitive and named-array closures; the contract both assert is stated at
// CppNaming.MangleFragment.
//
// A fragment outside the primitive/named/SZArray grammar must not fall back to a
// shared token: that gives one specialization, one static and one type-info for
// every such argument, and nothing fails — the C++ compiles, links and answers
// wrong. Hence every static below is written first and read afterwards, so a
// conflation shows up as a wrong NUMBER rather than a missing symbol.
//
// int*[] carries the Pointer arm (a pointer cannot itself be a type argument, an
// array of one can), which is why this bucket's csproj enables AllowUnsafeBlocks.
// A BLOCK namespace for the same reason as PrimitiveNameShadowSubset.
using System;

/// <summary>Global-namespace shadow of the mangle of int[,] ("Int32Md2").</summary>
public sealed class Int32Md2
{
    public override string ToString() => "cls:Int32Md2";
}

/// <summary>Shadow of the mangle of int[][,]: the SZArray closure over MDArray.</summary>
public sealed class Int32Md2Arr
{
    public override string ToString() => "cls:Int32Md2Arr";
}

/// <summary>Shadow of the mangle of int[,][]: the other nesting order.</summary>
public sealed class Int32ArrMd2
{
    public override string ToString() => "cls:Int32ArrMd2";
}

/// <summary>Global-namespace shadow of the mangle of int*[] ("Int32PointerArr").</summary>
public sealed class Int32PointerArr
{
    public override string ToString() => "cls:Int32PointerArr";
}

/// <summary>Shadow of the bare "T" token an unmodeled fragment could fall back to.
/// A type parameter of that name shadows it inside a generic scope, so it stays
/// invisible to the rest of the bucket.</summary>
public sealed class T
{
    public override string ToString() => "cls:T";
}

namespace MangleKindShadowSubset
{
    internal sealed class Shadow<TArg>
    {
        public TArg? Value;

        // Per-instantiation storage: two arguments that mangle alike share ONE.
        public static int Counter;

        public string Describe() => "Shadow<" + typeof(TArg).Name + ">";
    }

    internal static unsafe class Program
    {
        internal static void __GateEntry()
        {
            // Twelve distinct type arguments, written before any is read.
            Shadow<int[,]>.Counter = 1;
            Shadow<double[,]>.Counter = 2;
            Shadow<int[,,]>.Counter = 3;
            Shadow<global::Int32Md2>.Counter = 4;
            Shadow<int[][,]>.Counter = 5;
            Shadow<global::Int32Md2Arr>.Counter = 6;
            Shadow<int[,][]>.Counter = 7;
            Shadow<global::Int32ArrMd2>.Counter = 8;
            Shadow<int*[]>.Counter = 9;
            Shadow<double*[]>.Counter = 10;
            Shadow<global::Int32PointerArr>.Counter = 11;
            Shadow<global::T>.Counter = 12;

            Console.WriteLine("kind-statics " + Shadow<int[,]>.Counter
                + "," + Shadow<double[,]>.Counter
                + "," + Shadow<int[,,]>.Counter
                + "," + Shadow<global::Int32Md2>.Counter);
            Console.WriteLine("kind-closure-statics " + Shadow<int[][,]>.Counter
                + "," + Shadow<global::Int32Md2Arr>.Counter
                + "," + Shadow<int[,][]>.Counter
                + "," + Shadow<global::Int32ArrMd2>.Counter);
            Console.WriteLine("kind-ptr-statics " + Shadow<int*[]>.Counter
                + "," + Shadow<double*[]>.Counter
                + "," + Shadow<global::Int32PointerArr>.Counter
                + "," + Shadow<global::T>.Counter);

            // Runtime type identity over the same pairs: distinct closed types.
            Console.WriteLine("kind-same-type "
                + (typeof(Shadow<int[,]>) == typeof(Shadow<double[,]>))
                + "," + (typeof(Shadow<int[,]>) == typeof(Shadow<int[,,]>))
                + "," + (typeof(Shadow<int[,]>) == typeof(Shadow<global::Int32Md2>))
                + "," + (typeof(Shadow<int[,]>) == typeof(Shadow<global::T>)));
            Console.WriteLine("kind-closure-same-type "
                + (typeof(Shadow<int[][,]>) == typeof(Shadow<global::Int32Md2Arr>))
                + "," + (typeof(Shadow<int[,][]>) == typeof(Shadow<global::Int32ArrMd2>))
                + "," + (typeof(Shadow<int[][,]>) == typeof(Shadow<int[,][]>)));
            Console.WriteLine("kind-ptr-same-type "
                + (typeof(Shadow<int*[]>) == typeof(Shadow<double*[]>))
                + "," + (typeof(Shadow<int*[]>) == typeof(Shadow<global::Int32PointerArr>)));

            // A collapse would type the field at whichever argument won the group.
            var md = new Shadow<int[,]> { Value = new int[2, 3] };
            var mdD = new Shadow<double[,]> { Value = new double[4, 5] };
            Console.WriteLine("kind-values " + md.Value!.Length + "," + mdD.Value!.Length
                + " " + md.Describe() + " " + mdD.Describe());
            Console.WriteLine("kind-ranks " + md.Value.Rank + "," + md.Value.GetLength(0)
                + "," + md.Value.GetLength(1) + " " + new Shadow<global::T>().Describe());
        }
    }
}
