using System;
using System.Globalization;
using System.Runtime.CompilerServices;

internal static class Program
{
    private struct Modulo
    {
        public long Value;
        public override bool Equals(object value) => value is Modulo other && Value % 10 == other.Value % 10;
        public override int GetHashCode() => (int)(Value % 10);
    }

#if ARRAY_JAGGED_ALIAS
    private static Type[][] _escaped;
    private static void Update() => _escaped[0][0] = typeof(Modulo);
#else
    private static object[] _escaped;
    private static void Update() => ((Type[])_escaped[0])[0] = typeof(Modulo);
#endif

#if ARRAY_UNKNOWN_ALIAS
    private static Type[] Pick(int index)
    {
        Type[][] values = new Type[1][];
        values[0] = new Type[] { typeof(int) };
        values[index] = new Type[] { typeof(Modulo) };
        return values[0];
    }
#endif

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#if ARRAY_REFERENCE_SLOT_ALIAS
        Type[] nested = new Type[] { typeof(int) };
        object slot = nested;
        object returned = RuntimeHelpers.Box(ref Unsafe.As<object, byte>(ref slot), typeof(object).TypeHandle);
        ((Type[])returned)[0] = typeof(Modulo);
        Console.WriteLine("array reference-slot identity=" + ReferenceEquals(slot, returned));
#elif ARRAY_UNKNOWN_ALIAS
        Type[] nested = Pick(0);
#else
#if ARRAY_JAGGED_ALIAS
        _escaped = new Type[1][];
#else
        _escaped = new object[1];
#endif
        Type[] nested = new Type[] { typeof(int) };
#if ARRAY_ERASED_ALIAS
        object erased = nested;
        _escaped[0] = erased;
#else
        _escaped[0] = nested;
#endif
        Update();
#endif
        Array values = Array.CreateInstance(nested[0], 2);
        object needle = new Modulo { Value = 10 };
        Console.WriteLine("array element alias=" + Array.IndexOf(values, needle)
            + ":" + Array.LastIndexOf(values, needle));
        Console.WriteLine("array element alias end");
    }
}
