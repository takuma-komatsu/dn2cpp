using System;
using System.Globalization;

namespace SharedSummaryTerms;

// Every body of a nest that stores into a closure field is interpreted while the nest's
// field table is built, reached or not. Each level of Unreached forms a generic type
// whose arguments are all the previous level's type, so its stored value has one term per
// level while its tree unfolding multiplies by the argument count at every level.
public static class Witness
{
    public static Type Reached()
    {
        Type target = typeof(Built);
        Func<object?> make = () => Activator.CreateInstance(target);
        return make()!.GetType();
    }

    public static Func<Type> Unreached()
    {
        Type wide = typeof(Func<,,,,,,,,,,,,,,,,>);
        Type t0 = typeof(int);
        Type t1 = wide.MakeGenericType(t0, t0, t0, t0, t0, t0, t0, t0, t0, t0, t0, t0, t0, t0, t0, t0, t0);
        Type t2 = wide.MakeGenericType(t1, t1, t1, t1, t1, t1, t1, t1, t1, t1, t1, t1, t1, t1, t1, t1, t1);
        Type t3 = wide.MakeGenericType(t2, t2, t2, t2, t2, t2, t2, t2, t2, t2, t2, t2, t2, t2, t2, t2, t2);
        Type t4 = wide.MakeGenericType(t3, t3, t3, t3, t3, t3, t3, t3, t3, t3, t3, t3, t3, t3, t3, t3, t3);
        Type t5 = wide.MakeGenericType(t4, t4, t4, t4, t4, t4, t4, t4, t4, t4, t4, t4, t4, t4, t4, t4, t4);
        Type t6 = wide.MakeGenericType(t5, t5, t5, t5, t5, t5, t5, t5, t5, t5, t5, t5, t5, t5, t5, t5, t5);
        Type t7 = wide.MakeGenericType(t6, t6, t6, t6, t6, t6, t6, t6, t6, t6, t6, t6, t6, t6, t6, t6, t6);
        Type t8 = wide.MakeGenericType(t7, t7, t7, t7, t7, t7, t7, t7, t7, t7, t7, t7, t7, t7, t7, t7, t7);
        Type t9 = wide.MakeGenericType(t8, t8, t8, t8, t8, t8, t8, t8, t8, t8, t8, t8, t8, t8, t8, t8, t8);
        Type t10 = wide.MakeGenericType(t9, t9, t9, t9, t9, t9, t9, t9, t9, t9, t9, t9, t9, t9, t9, t9, t9);
        Type t11 = wide.MakeGenericType(t10, t10, t10, t10, t10, t10, t10, t10, t10, t10, t10, t10, t10, t10, t10, t10, t10);
        Type t12 = wide.MakeGenericType(t11, t11, t11, t11, t11, t11, t11, t11, t11, t11, t11, t11, t11, t11, t11, t11, t11);
        return () => t12;
    }
}

public sealed class Built
{
}

public static class Program
{
    public static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("built: " + Witness.Reached().Name);
    }
}
