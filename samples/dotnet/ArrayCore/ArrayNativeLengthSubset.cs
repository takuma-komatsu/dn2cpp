using System;
using System.Reflection;

namespace ArrayNativeLengthSubset
{
    // The private Array.NativeLength getter, reached by name, counts every element of
    // SZ, multidimensional and nonzero-lower-bound arrays as an unsigned native integer.
    internal static class Program
    {
        private static object NativeLength(Array receiver)
        {
            MethodInfo method = typeof(Array).GetMethod("get_NativeLength",
                BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            return method.Invoke(receiver, null);
        }

        private static void Show(string label, Array value)
        {
            object length = NativeLength(value);
            Console.WriteLine(label + ":" + length.GetType().Name + ":" + length
                + ":" + ((ulong)(nuint)length == (ulong)value.LongLength));
        }

        internal static void Run()
        {
            Console.WriteLine("== Array.NativeLength named reflection ==");
            Show("sz-int", new[] { 3, 7, 11 });
            Show("sz-reference", new object[] { new object(), "x", null, "y" });
            Show("sz-empty", Array.Empty<byte>());
            Show("md-two", new int[2, 3]);
            Show("md-three", new object[2, 3, 4]);
            Show("md-empty", new int[0, 7]);
            Show("md-nonzero-lower", Array.CreateInstance(typeof(int), new[] { 2, 3 }, new[] { -2, 5 }));
            Show("rank-one-nonzero-lower", Array.CreateInstance(typeof(object), new[] { 5 }, new[] { -3 }));
            try
            {
                NativeLength(null);
                Console.WriteLine("null-receiver:none");
            }
            catch (TargetException e)
            {
                Console.WriteLine("null-receiver:" + e.GetType().Name);
            }
            Console.WriteLine("Array.NativeLength named reflection end");
        }
    }
}
