#nullable disable
// (practical reflection, follow-up): the non-generic Enum.GetValues(Type) used
// in a foreach — the shape carved out. It returns System.Array (not T[]), so a
// foreach lowers to the non-generic System.Array.GetEnumerator / IEnumerator path with
// boxed `object` elements, which dn2cpp didn't model for a bare array. Now GetValues(Type)
// returns an object[] of boxed enum values, System.Array.GetEnumerator boxes the array into
// an SZArrayEnumerable<object> (the wrapper, which implements the non-generic
// IEnumerator), and a boxed enum ToStrings to its member name (recoverable from the per-enum
// table the type-info carries since ). Diffed exact vs real.NET.
using System;

namespace ReflectEnumValuesSubset;

public enum Status { Inactive = 0, Active = 1, Banned = 2 }
public enum Priority { Low = 10, Medium = 20, High = 30 }

public static class Program
{
    internal static void Run()
    {
        // foreach over the non-generic Enum.GetValues(Type): boxed object elements that
        // unbox back to the enum and ToString to the member name
        foreach (var v in Enum.GetValues(typeof(Status)))
            Console.WriteLine(v + "=" + (int)(Status)v);

        // a second enum, with an explicitly object-typed loop variable + counting
        int count = 0;
        foreach (object v in Enum.GetValues(typeof(Priority)))
        {
            count++;
            Console.WriteLine("P:" + v.ToString());
        }
        Console.WriteLine("count=" + count);

        // a boxed enum stringifies to its member name (the table is now reachable
        // through a boxed object, so this matches real.NET rather than printing the number)
        object boxed = Status.Active;
        Console.WriteLine("boxed=" + boxed);
    }
}
