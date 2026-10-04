using System;
using System.Runtime.CompilerServices;

// Object's Equals/GetHashCode/ToString and String's Equals/CompareTo/GetHashCode/ToString
// on a null receiver — a direct callvirt, a `constrained. !T` call under a generic, and
// the typed IComparable<string>/IEquatable<string> forms — raise NullReferenceException,
// as a callvirt does. The static object.Equals/string.Equals overloads accept null, and an
// empty Nullable<T> answers its own members without a receiver fault.
namespace NullReceiverVirtualSubset
{
    internal struct Point
    {
        public int X;
    }

    internal static class MonomorphicStringText<T>
    {
        // A generic owner's static synchronized member retains its specialization.
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal static string Read(T value) => value.ToString();
    }

    internal static class Program
    {
        private static string Try(Func<object> call)
        {
            try
            {
                return call().ToString();
            }
            catch (NullReferenceException)
            {
                return "NRE";
            }
        }

        private static int Untyped<T>(T value, object other) where T : IComparable => value.CompareTo(other);
        private static int Typed<T>(T value, T other) where T : IComparable<T> => value.CompareTo(other);
        private static bool Same<T>(T value, T other) where T : IEquatable<T> => value.Equals(other);
        private static bool Eq<T>(T value, object other) => value.Equals(other);
        private static int Hash<T>(T value) => value.GetHashCode();
        private static string Text<T>(T value) => value.ToString();

        private static string TextResult(Func<string> call)
        {
            try
            {
                string result = call();
                return result is null ? "<null>" : result;
            }
            catch (NullReferenceException)
            {
                return "NRE";
            }
        }

        internal static void RunStringText()
        {
            Console.WriteLine("== constrained String ToString receiver ==");
            string value = new string(new[] { 't', 'e', 'x', 't' });
            string none = null;
            Console.WriteLine("direct string text: " + TextResult(() => value.ToString()) + " "
                + TextResult(() => none.ToString()));
            Console.WriteLine("shared string text: " + TextResult(() => Text(value)) + " "
                + TextResult(() => Text(none)));
            Console.WriteLine("monomorphic string text: " + TextResult(() => MonomorphicStringText<string>.Read(value)) + " "
                + TextResult(() => MonomorphicStringText<string>.Read(none)));
            Console.WriteLine("constrained string text identity: " + ReferenceEquals(value, Text(value))
                + " " + ReferenceEquals(value, MonomorphicStringText<string>.Read(value)));
            Console.WriteLine("constrained String ToString receiver end");
        }

        internal static void Run()
        {
            Console.WriteLine("== null receivers of Object and IComparable members ==");
            string s = null;
            object o = null;
            Console.WriteLine("direct string: " + Try(() => s.Equals("x")) + " " + Try(() => s.Equals((object)"x")) + " "
                + Try(() => s.Equals("x", StringComparison.Ordinal)) + " " + Try(() => s.GetHashCode()) + " "
                + Try(() => s.CompareTo("x")) + " " + Try(() => s.CompareTo((object)"x")));
            Console.WriteLine("direct object: " + Try(() => o.Equals(null)) + " " + Try(() => o.Equals(1)) + " "
                + Try(() => o.GetHashCode()) + " " + Try(() => o.ToString()));
            Console.WriteLine("constrained string: " + Try(() => Untyped(s, "x")) + " " + Try(() => Untyped(s, null)) + " "
                + Try(() => Typed(s, "x")) + " " + Try(() => Typed(s, s)) + " " + Try(() => Same(s, "x")) + " "
                + Try(() => Eq(s, "x")) + " " + Try(() => Hash(s)) + " " + Try(() => Text(s)));
            Console.WriteLine("constrained object: " + Try(() => Eq(o, null)) + " " + Try(() => Hash(o)) + " " + Try(() => Text(o)));
            Console.WriteLine("static: " + object.Equals(s, null) + " " + string.Equals(s, null) + " "
                + string.Equals(s, "x", StringComparison.Ordinal) + " " + string.CompareOrdinal(s, "x") + " "
                + Untyped("x", null) + " " + Typed("b", "a"));
            int? none = null;
            Console.WriteLine("nullable: " + none.GetHashCode() + " " + none.Equals(null) + " " + Hash(none) + " " + Eq(none, null)
                + " [" + Text(none) + "]");
        }

        private static Type TypeOf<T>(T value) => value.GetType();

        // GetType reads the receiver's header, so a null receiver raises
        // NullReferenceException however the call reaches it: a callvirt of Object's or
        // Exception's GetType, the box of an empty Nullable<T>, a generic receiver, or
        // Attribute.TypeId, which answers GetType(). A generic value receiver reports its
        // own type, a Nullable<T> with a value T.
        internal static void RunGetType()
        {
            Console.WriteLine("== null receivers of GetType ==");
            object o = null;
            Exception e = null;
            ArgumentException a = null;
            string s = null;
            int? none = null;
            Console.WriteLine("null GetType: " + Try(() => o.GetType()) + " " + Try(() => e.GetType()) + " "
                + Try(() => a.GetType()) + " " + Try(() => s.GetType()) + " " + Try(() => none.GetType()) + " "
                + Try(() => TypeOf(s)) + " " + Try(() => TypeOf(none)) + " " + Try(() => TypeOf(o)));
            int? three = 3;
            Console.WriteLine("GetType: " + Try(() => ((object)1).GetType()) + " " + Try(() => new ArgumentException().GetType())
                + " " + Try(() => three.GetType()) + " " + Try(() => TypeOf(three)) + " " + Try(() => TypeOf("x")));
            Console.WriteLine("generic value GetType: " + Try(() => TypeOf(5)) + " " + Try(() => TypeOf(DayOfWeek.Monday)) + " "
                + Try(() => TypeOf(new Point { X = 7 })) + " " + Try(() => TypeOf(2.5m)) + " " + Try(() => TypeOf('c')));
            Attribute attr = null;
            Console.WriteLine("TypeId: " + Try(() => attr.TypeId) + " " + Try(() => new ObsoleteAttribute().TypeId));
        }
    }
}
