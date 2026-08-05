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
    }
}
