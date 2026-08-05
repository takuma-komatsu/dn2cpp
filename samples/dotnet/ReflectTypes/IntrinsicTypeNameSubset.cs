#nullable disable
using System;
using System.Runtime.Intrinsics;

// A REFERENCED INTRINSIC's type-info in the type-name registry. An
// intrinsic-modeled type a lowering names by its own &ti_ is emitted as a minimal
// type-info and nothing else — no member tables, since its members are emitted inline
// and never transpiled. It used to be absent from the type-name registry too, so
// Type.GetType answered null about a type typeof answers about, and MakeGenericType
// plus the composed-name resolution — both of which scan that one table for a
// candidate — found none at all.
//
// The section's subject is therefore the REGISTRY, not the names: every line below
// asks whether some route reaches the same handle typeof carries — the one-type-info
// identity guarantee, one route further. Only the mangled-name line is dn2cpp-only,
// and it is marked at its site.

namespace IntrinsicTypeNameSubset
{
    class Program
    {
        internal static void Run()
        {
            // Reach the closed instantiation: nothing below would emit a handle for a
            // type the transpiler never saw a token of.
            Vector128<int> v = Vector128.Create(1, 2, 3, 4);
            Console.WriteLine("seed " + Vector128.Sum(v));

            Type t = typeof(Vector128<int>);
            Type def = typeof(Vector128<>);
            Console.WriteLine("vec Name=" + t.Name + " Namespace=" + t.Namespace);
            Console.WriteLine("vec ToString=" + t.ToString());
            Console.WriteLine("vec FullName=" + t.FullName);
            Console.WriteLine("vec byclrname=" + (Type.GetType("System.Runtime.Intrinsics.Vector128`1[System.Int32]") == t));
            Console.WriteLine("vec byfullname=" + (Type.GetType(t.FullName) == t)
                + " byaqn=" + (Type.GetType(t.AssemblyQualifiedName) == t)
                + " bytostring=" + (Type.GetType(t.ToString()) == t));
            // DIVERGES: the dn2cpp-mangled instantiation name IS this handle's registry
            // key, as it is for every closed generic; real .NET knows no such name and
            // answers null.
            Console.WriteLine("vec bymangled=" + (Type.GetType("System.Runtime.Intrinsics.Vector128_Int32") == t));
            // The definition handle, and the MakeGenericType relation — which reads the
            // same table the rows above joined, so it failed for the same reason.
            Console.WriteLine("vecdef byname=" + (Type.GetType("System.Runtime.Intrinsics.Vector128`1") == def)
                + " isdef=" + def.IsGenericTypeDefinition + " ToString=" + def.ToString());
            Console.WriteLine("vec mkgen=" + (def.MakeGenericType(typeof(int)) == t)
                + " getgendef=" + (t.GetGenericTypeDefinition() == def));
            // A NON-generic referenced intrinsic takes the same route: an interface
            // nothing emits, whose handle a typeof already names.
            Type ifp = typeof(IFormatProvider);
            Console.WriteLine("ifp byname=" + (Type.GetType("System.IFormatProvider") == ifp)
                + " isitf=" + ifp.IsInterface + " FullName=" + ifp.FullName);
        }
    }
}
