using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Dn2Cpp.Runtime;

namespace Dn2Cpp.Runtime
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    internal sealed class ObfuscateAttribute : Attribute { }
}

namespace Obfuscation
{
    internal sealed class Counter
    {
        private readonly int _value;
        [Obfuscate]
        public Counter(int value) { _value = value; }
        public int Value => _value;
    }

    internal static class Program
    {
        [Obfuscate]
        private static int Mix(int value)
        {
            for (int i = 0; i < 7; i++)
                value = (value * 31 + i) % 1009;
            return value;
        }

        [Obfuscate]
        private static int Mix(int value, int salt) => Mix(value + salt);

        [Obfuscate, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Tiny(int value) => value + 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PlainTiny(int value) => value + 1;

        [Obfuscate]
        private static T Pick<T>(T first, T second, bool chooseFirst) => chooseFirst ? first : second;

        [Obfuscate]
        private static string ArrayName<T>(int count) => new T[count].GetType().Name;

        [Obfuscate, System.Runtime.InteropServices.DllImport("unreachable")]
        private static extern int Unreachable();

        public static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Console.WriteLine("obfuscation-selection");
            Console.WriteLine(new Counter(12).Value);
            Console.WriteLine(Mix(42));
            Console.WriteLine(Mix(42, 7));
            Console.WriteLine(Tiny(8));
            Console.WriteLine(PlainTiny(8));
            Console.WriteLine(Pick("first", "second", false));
            Console.WriteLine(Pick<object>("left", "right", true));
            Console.WriteLine(Pick(3, 9, true));
            Console.WriteLine(ArrayName<string>(2));
            Console.WriteLine(ArrayName<object>(3));
        }
    }
}
