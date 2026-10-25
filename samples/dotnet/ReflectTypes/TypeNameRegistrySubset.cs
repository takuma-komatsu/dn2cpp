using System;

namespace TypeNameRegistrySubset
{
    interface IShape { }

    sealed class Widget : IShape { }

    enum Mood { Calm, Bold }

    static class Program
    {
        static string Show(Type t) => t is null ? "null" : t.Name;

        internal static void Run()
        {
            // Resolving well-known primitive / BCL names — and identity with typeof
            // (the registry entry points at the same Dn2CppTypeInfo handle).
            Console.WriteLine("== primitives ==");
            Console.WriteLine(Show(Type.GetType("System.Int32")));            // Int32
            Console.WriteLine(Type.GetType("System.Int32") == typeof(int));   // True
            Console.WriteLine(Show(Type.GetType("System.String")));           // String
            Console.WriteLine(Type.GetType("System.String") == typeof(string)); // True
            Console.WriteLine(Show(Type.GetType("System.Object")));           // Object
            Console.WriteLine(Show(Type.GetType("System.Boolean")));          // Boolean

            // User types (top-level): class, interface, enum.
            Console.WriteLine("== user types ==");
            Console.WriteLine(Show(Type.GetType("TypeNameRegistrySubset.Widget")));  // Widget
            Console.WriteLine(Type.GetType("TypeNameRegistrySubset.Widget") == typeof(Widget)); // True
            Console.WriteLine(Show(Type.GetType("TypeNameRegistrySubset.IShape")));  // IShape
            Console.WriteLine(Show(Type.GetType("TypeNameRegistrySubset.Mood")));    // Mood
            Console.WriteLine(Type.GetType("TypeNameRegistrySubset.Mood") == typeof(Mood)); // True

            // Round-trip through FullName, and a runtime GetType() round-trip.
            Console.WriteLine("== round-trip ==");
            Console.WriteLine(Type.GetType(typeof(Widget).FullName) == typeof(Widget)); // True
            Widget w = new Widget();
            Console.WriteLine(Type.GetType(w.GetType().FullName) == w.GetType());        // True

            // Missing names: 1-arg returns null; assembly-qualified suffix is ignored.
            Console.WriteLine("== missing / qualified ==");
            Console.WriteLine(Show(Type.GetType("Nope.Missing")));               // null
            Console.WriteLine(Show(Type.GetType("System.Int32, mscorlib")));     // Int32

            // throwOnError: found resolves; missing throws TypeLoadException.
            Console.WriteLine("== throwOnError ==");
            Console.WriteLine(Show(Type.GetType("System.Double", true)));        // Double
            try
            {
                Type.GetType("Nope.Missing", true);
                Console.WriteLine("no throw");
            }
            catch (TypeLoadException)
            {
                Console.WriteLine("TypeLoadException");
            }
        }

        internal static void RunIgnoreCase()
        {
            Console.WriteLine("== Type.GetType throwOnError and ignoreCase ==");
            Type nestedDefinition = typeof(DelegateBaseTypeSubset.GenericOwner<int>.Handler<string>)
                .GetGenericTypeDefinition();
            string nestedName = nestedDefinition.FullName;
            Console.WriteLine("nested dynamic one=" + (Type.GetType(nestedName) == nestedDefinition));
            Console.WriteLine("nested dynamic two=" + (Type.GetType(nestedName, true) == nestedDefinition));
            Console.WriteLine("nested dynamic three exact=" + (Type.GetType(nestedName, false, false) == nestedDefinition));
            Console.WriteLine("nested dynamic three folded=" + (Type.GetType(nestedName.ToLowerInvariant(), false, true) == nestedDefinition));
            Type alphaLeaf = typeof(Q.Alpha<int>.Mid<string>.Leaf<long>).GetGenericTypeDefinition();
            Type otherLeaf = typeof(Q.aLpha<int>.Mid<string>.Leaf<long>).GetGenericTypeDefinition();
            string foldedDeepName = alphaLeaf.FullName.ToLowerInvariant();
            Type foldedDeep = Type.GetType(foldedDeepName, false, true);
            Console.WriteLine("nested deep exact alpha=" + (Type.GetType(alphaLeaf.FullName, false, false) == alphaLeaf));
            Console.WriteLine("nested deep exact other=" + (Type.GetType(otherLeaf.FullName, false, false) == otherLeaf));
            Console.WriteLine("nested deep folded alpha=" + (foldedDeep == alphaLeaf));
            Console.WriteLine("nested deep folded other=" + (foldedDeep == otherLeaf));
            Console.WriteLine("app exact=" + Show(Type.GetType("TypeNameRegistrySubset.Widget", false, false)));
            Console.WriteLine("app folded=" + Show(Type.GetType("typenameregistrysubset.widget", false, true)));
            Console.WriteLine("app one=" + Show(Type.GetType("TypeNameRegistrySubset.Widget")));
            Console.WriteLine("app two=" + Show(Type.GetType("TypeNameRegistrySubset.Widget", true)));
            Console.WriteLine("bcl exact=" + Show(Type.GetType("System.Int32", false, false)));
            Console.WriteLine("bcl folded=" + Show(Type.GetType("system.int32", false, true)));
            Console.WriteLine("bcl qualified=" + Show(Type.GetType("system.int32, System.Private.CoreLib", false, true)));
            Console.WriteLine("mscorlib exact=" + Show(Type.GetType("System.Int32, MSCORLIB", false, false)));
            Console.WriteLine("mscorlib folded=" + Show(Type.GetType("system.int32, MSCORLIB", false, true)));
            Console.WriteLine("facade exact=" + Show(Type.GetType("System.Int32, System.Runtime", false, false)));
            Console.WriteLine("facade folded=" + Show(Type.GetType("system.int32, System.Runtime", false, true)));
            Console.WriteLine("facade array=" + Show(Type.GetType("system.int32[], System.Runtime", false, true)));
            Console.WriteLine("facade private miss=" + Show(Type.GetType("System.RuntimeType, System.Runtime", false, false)));
            Console.WriteLine("facade private array miss=" + Show(Type.GetType("System.RuntimeType[], System.Runtime", false, false)));
            Console.WriteLine("netstandard private miss=" + Show(Type.GetType("System.RuntimeType, netstandard", false, false)));
            Console.WriteLine("mscorlib private miss=" + Show(Type.GetType("System.RuntimeType, mscorlib", false, false)));
            Console.WriteLine("linq control=" + Show(typeof(System.Linq.Enumerable)));
            Console.WriteLine("netstandard linq exact=" + Show(Type.GetType("System.Linq.Enumerable, netstandard", false, false)));
            Console.WriteLine("netstandard linq folded=" + Show(Type.GetType("system.linq.enumerable, netstandard", false, true)));
            Console.WriteLine("netstandard linq throw exact=" + Show(Type.GetType("System.Linq.Enumerable, netstandard", true, false)));
            Console.WriteLine("netstandard linq throw folded=" + Show(Type.GetType("system.linq.enumerable, netstandard", true, true)));
            Console.WriteLine("runtime linq miss=" + Show(Type.GetType("System.Linq.Enumerable, System.Runtime", false, false)));
            Console.WriteLine("miss false=" + Show(Type.GetType("TypeNameRegistrySubset.Missing", false, true)));
            try
            {
                Type.GetType("TypeNameRegistrySubset.Missing", true, true);
                Console.WriteLine("miss true=<no throw>");
            }
            catch (TypeLoadException)
            {
                Console.WriteLine("miss true=TypeLoadException");
            }
            Console.WriteLine("Type.GetType throwOnError and ignoreCase end");
        }
    }
}

namespace Q
{
    internal class Alpha<T>
    {
        internal class Mid<U>
        {
            internal class Leaf<V> { }
        }
    }

    internal class aLpha<T>
    {
        internal class Mid<U>
        {
            internal class Leaf<V> { }
        }
    }
}
