#nullable disable
using System;

// Type.GetInterfaces + the non-generic enum bridge.
// A per-enum runtime table (underlying handle + (name, value) members, pre-sorted by
// unsigned underlying magnitude) backs Type.GetEnumUnderlyingType/GetEnumNames and the
// non-generic Enum.GetUnderlyingType/GetNames/GetName/IsDefined/Parse(Type, …) — the
// runtime-Type complement to the generic Enum.*<T> inline lowering.
// GetInterfaces reads the type's emitted interface dispatch table. CoreLib only; diffed
// exact vs real .NET.

namespace ReflectEnumTypeSubset
{
    interface IAlpha { int A(); }
    interface IBeta { int B(); }

    sealed class Widget : IAlpha, IBeta
    {
        public int A() => 1;
        public int B() => 2;
    }

    enum Color : byte { Red = 1, Green = 2, Blue = 4 }
    enum Mode { First, Second, Third }

    class Program
    {internal static void Run()
        {
            // Keep the interfaces emitted + dispatched (so the dispatch table exists).
            IAlpha a = new Widget();
            IBeta b = new Widget();
            Console.WriteLine("seed " + a.A() + b.B());

            // Type.GetInterfaces() — order is unspecified, so sort by name.
            Type[] itfs = typeof(Widget).GetInterfaces();
            string[] names = new string[itfs.Length];
            for (int i = 0; i < itfs.Length; i++) names[i] = itfs[i].Name;
            Array.Sort(names, StringComparer.Ordinal);
            Console.WriteLine("interfaces=" + names.Length + " [" + string.Join(",", names) + "]");

            // Enum underlying type (Type instance + non-generic Enum static).
            Console.WriteLine("Color underlying=" + typeof(Color).GetEnumUnderlyingType().Name
                + " / " + Enum.GetUnderlyingType(typeof(Color)).Name);
            Console.WriteLine("Mode underlying=" + typeof(Mode).GetEnumUnderlyingType().Name);

            // Enum names (Type instance + non-generic Enum static), sorted by value.
            Console.WriteLine("Color names=[" + string.Join(",", typeof(Color).GetEnumNames()) + "]");
            Console.WriteLine("Color names2=[" + string.Join(",", Enum.GetNames(typeof(Color))) + "]");

            // GetName(Type, value): defined -> name, undefined -> null.
            Console.WriteLine("GetName(Green)=" + Enum.GetName(typeof(Color), Color.Green));
            Console.WriteLine("GetName(3)=" + (Enum.GetName(typeof(Color), (Color)3) ?? "<null>"));

            // IsDefined(Type, value).
            Console.WriteLine("IsDefined(Blue)=" + Enum.IsDefined(typeof(Color), Color.Blue));
            Console.WriteLine("IsDefined(8)=" + Enum.IsDefined(typeof(Color), (Color)8));

            // Parse(Type, string[, ignoreCase]) -> boxed enum.
            Color g = (Color)Enum.Parse(typeof(Color), "Green");
            Console.WriteLine("Parse(Green)=" + (int)g);
            Color gi = (Color)Enum.Parse(typeof(Color), "green", true);
            Console.WriteLine("Parse(green,ic)=" + (int)gi);
            Color orVal = (Color)Enum.Parse(typeof(Color), "Red,Blue");
            Console.WriteLine("Parse(Red,Blue)=" + (int)orVal);

            // Parse failure -> ArgumentException.
            try
            {
                Color bad = (Color)Enum.Parse(typeof(Color), "Nope");
                Console.WriteLine("Parse(Nope)=" + (int)bad);
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Parse(Nope): ArgumentException");
            }
        }
    }
}
