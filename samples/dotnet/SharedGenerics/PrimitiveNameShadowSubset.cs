#nullable enable
// GLOBAL-NAMESPACE classes named after other types' C++ mangles, used as generic
// type arguments. `class Int32 {}` sanitizes to "Int32", exactly what
// Compilation.MangleArg prints for PrimitiveTypeCode.Int32; Int32Arr and
// MangleElemArr do the same for the SZArray closure of a primitive and of a named
// type. Unless CppNaming.MangleFragment keeps the kind spaces disjoint each pair
// collapses into ONE specialization — aliased statics, a field typed at whichever
// argument won, C++ that does not compile. The injectivity contract is stated at
// CppNaming.MangleFragment; every line below must match real .NET exactly.
//
// A BLOCK namespace, unlike the file-scoped siblings: the shadowing classes must
// sit in the global namespace, and a file-scoped declaration must precede every
// type in the file.
using System;
using System.Collections.Generic;

/// <summary>Global-namespace shadow of System.Int32's C++ mangle.</summary>
public sealed class Int32
{
    public override string ToString() => "cls:Int32";
}

/// <summary>Global-namespace shadow of System.Double's C++ mangle.</summary>
public sealed class Double
{
    public override string ToString() => "cls:Double";
}

/// <summary>Global-namespace shadow of the mangle of int[] ("Int32Arr").</summary>
public sealed class Int32Arr
{
    public override string ToString() => "cls:Int32Arr";
}

/// <summary>Element type whose SZArray mangles to "MangleElemArr".</summary>
public sealed class MangleElem
{
    public override string ToString() => "cls:MangleElem";
}

/// <summary>Global-namespace shadow of the mangle of MangleElem[]: a named element's
/// SZArray appends "Arr" like a primitive's, so this collides unless the fragment
/// escape covers Arr-suffixed names too.</summary>
public sealed class MangleElemArr
{
    public override string ToString() => "cls:MangleElemArr";
}

namespace PrimitiveNameShadowSubset
{
    internal sealed class Shadow<T>
    {
        public T? Value;

        // Per-instantiation storage, the sharpest observable of a collapse: two
        // arguments that mangle alike would read each other's writes.
        public static int Counter;

        public string Describe() =>
            "Shadow<" + typeof(T).Name + ">:" + (Value is null ? "null" : Value.ToString());
    }

    internal static class Program
    {
        internal static void __GateEntry()
        {
            var prim = new Shadow<int> { Value = 7 };
            var cls = new Shadow<global::Int32> { Value = new global::Int32() };
            var primD = new Shadow<double> { Value = 1.5 };
            var clsD = new Shadow<global::Double> { Value = new global::Double() };
            Console.WriteLine("shadow " + prim.Describe());
            Console.WriteLine("shadow " + cls.Describe());
            Console.WriteLine("shadow " + primD.Describe());
            Console.WriteLine("shadow " + clsD.Describe());

            // Four distinct specializations => four independent statics.
            Shadow<int>.Counter = 1;
            Shadow<global::Int32>.Counter = 2;
            Shadow<double>.Counter = 3;
            Shadow<global::Double>.Counter = 4;
            Console.WriteLine("statics " + Shadow<int>.Counter + "," + Shadow<global::Int32>.Counter
                + "," + Shadow<double>.Counter + "," + Shadow<global::Double>.Counter);

            // Runtime type identity: the two are different closed types.
            Console.WriteLine("same-type " + (typeof(Shadow<int>) == typeof(Shadow<global::Int32>)));
            Console.WriteLine("same-arg " + (typeof(int) == typeof(global::Int32)));
            Console.WriteLine("arg-names " + typeof(int).Name + "/" + typeof(global::Int32).Name);
            Console.WriteLine("full-names " + typeof(int).FullName + "/" + typeof(global::Int32).FullName);

            // The SZArray closure of the primitive token space: int[] mangles to
            // "Int32Arr", so a class of that name is the array-side collision.
            Shadow<int[]>.Counter = 5;
            Shadow<global::Int32Arr>.Counter = 6;
            Console.WriteLine("arr-statics " + Shadow<int[]>.Counter + "," + Shadow<global::Int32Arr>.Counter);
            Console.WriteLine("arr-same-type " + (typeof(Shadow<int[]>) == typeof(Shadow<global::Int32Arr>)));
            Console.WriteLine("arr-of-shadow " + (typeof(global::Int32[]) == typeof(int[])));

            // Arrays of the shadowing class are real arrays of it, not of int.
            var clsArr = new global::Int32[] { new global::Int32(), new global::Int32() };
            var primArr = new int[] { 11, 12 };
            Console.WriteLine("arrays " + clsArr.Length + "," + primArr.Length
                + " " + clsArr[0] + "," + primArr[1]);
            Console.WriteLine("arr-elem " + clsArr.GetType().GetElementType()!.Name
                + "/" + primArr.GetType().GetElementType()!.Name);

            // A BCL generic takes the shadow the same way.
            var clsList = new List<global::Int32> { new global::Int32() };
            var primList = new List<int> { 21, 22, 23 };
            Console.WriteLine("lists " + clsList.Count + "," + primList.Count
                + " " + clsList[0] + "," + primList[2]);
            Console.WriteLine("list-same-type " + (typeof(List<global::Int32>) == typeof(List<int>)));

            // …and so does a dictionary keyed by one of them.
            var map = new Dictionary<global::Int32, int>();
            var key = new global::Int32();
            map[key] = 31;
            Console.WriteLine("dict " + map.Count + "," + map[key] + "," + map.ContainsKey(new global::Int32()));

            // The NAMED-type array closure: MangleElem[] mangles to
            // "MangleElemArr", the sanitized name of the class above.
            Shadow<global::MangleElem[]>.Counter = 41;
            Shadow<global::MangleElemArr>.Counter = 42;
            Console.WriteLine("named-arr-statics " + Shadow<global::MangleElem[]>.Counter
                + "," + Shadow<global::MangleElemArr>.Counter);
            Console.WriteLine("named-arr-same-type "
                + (typeof(Shadow<global::MangleElem[]>) == typeof(Shadow<global::MangleElemArr>)));
            var elemArr = new Shadow<global::MangleElem[]> { Value = new[] { new global::MangleElem() } };
            var arrCls = new Shadow<global::MangleElemArr> { Value = new global::MangleElemArr() };
            Console.WriteLine("named-arr " + elemArr.Value!.Length + " " + elemArr.Value[0] + " " + arrCls.Value);

            // The closure of the ESCAPED space: global::Int32[]'s fragment is the
            // escape of "Int32" plus "Arr", which must not equal that of "Int32Arr".
            Shadow<global::Int32[]>.Counter = 43;
            Console.WriteLine("esc-arr-statics " + Shadow<global::Int32[]>.Counter
                + "," + Shadow<global::Int32Arr>.Counter);
            Console.WriteLine("esc-arr-same-type "
                + (typeof(Shadow<global::Int32[]>) == typeof(Shadow<global::Int32Arr>)));

            // The closure over SPECIALIZATION names: Shadow<int[]>'s own mangle ends
            // in "Arr", so as a type argument it must not read as Shadow<int> + [].
            Shadow<Shadow<int[]>>.Counter = 44;
            Shadow<Shadow<int>[]>.Counter = 45;
            Console.WriteLine("spec-arr-statics " + Shadow<Shadow<int[]>>.Counter
                + "," + Shadow<Shadow<int>[]>.Counter);
            Console.WriteLine("spec-arr-same-type "
                + (typeof(Shadow<Shadow<int[]>>) == typeof(Shadow<Shadow<int>[]>)));

            // Array type-infos take the same fragment key, so distinct element
            // types must keep distinct GetElementType answers.
            Console.WriteLine("named-arr-elem " + typeof(global::MangleElem[]).GetElementType()!.Name
                + "/" + typeof(global::MangleElemArr[]).GetElementType()!.Name);
            Console.WriteLine("jagged-elem "
                + (typeof(global::MangleElem[][]).GetElementType() == typeof(global::MangleElemArr[]).GetElementType()));
        }
    }
}
