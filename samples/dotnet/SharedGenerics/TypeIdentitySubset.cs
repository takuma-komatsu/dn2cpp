#nullable enable
// Reflection identity of grouped instantiations: two enum-keyed dictionaries
// that share one canonical body still report distinct runtime types, equal to
// their own typeof and carrying the real enums in GetGenericArguments. Real
// System.Private.CoreLib (-r), run vs .NET.
using System;
using System.Collections.Generic;
namespace TypeIdentitySubset;

enum Alpha { A = 1, B = 2 }
enum Beta { C = 3, D = 4 }

class Program
{
    internal static void __GateEntry()
    {
        var d1 = new Dictionary<Alpha, string>();
        d1[Alpha.A] = "x";
        var d2 = new Dictionary<Beta, string>();
        d2[Beta.C] = "y";
        Console.WriteLine("distinct=" + (d1.GetType() != d2.GetType()));
        Console.WriteLine("self=" + (d1.GetType() == typeof(Dictionary<Alpha, string>)));
        Console.WriteLine("cross=" + (d1.GetType() == typeof(Dictionary<Beta, string>)));
        Console.WriteLine("vs int=" + (typeof(Dictionary<Alpha, string>) != typeof(Dictionary<int, string>)));
        var args = typeof(Dictionary<Alpha, string>).GetGenericArguments();
        Console.WriteLine("arg count=" + args.Length);
        Console.WriteLine("arg0=" + args[0].FullName);
        Console.WriteLine("arg1=" + args[1].FullName);
        Console.WriteLine("arg0 enum=" + args[0].IsEnum);
        Console.WriteLine("arg0 eq typeof=" + (args[0] == typeof(Alpha)));
        Console.WriteLine("alpha fullname=" + typeof(Alpha).FullName);
        Console.WriteLine("beta name=" + typeof(Beta).Name);
        Console.WriteLine("same gendef=" + (d1.GetType().GetGenericTypeDefinition() == d2.GetType().GetGenericTypeDefinition()));
    }
}
