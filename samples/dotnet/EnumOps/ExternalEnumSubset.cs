#nullable enable
// External CoreLib enums as isinst / cast targets with correct enum identity.
// Each enum referenced as a type token gets a distinct synthesized Dn2CppTypeInfo
// (shared System.Enum base) so `is`/switch discriminates one enum from another and
// from int (without this, a boxed DayOfWeek would wrongly match `is int` /
// `is DateTimeKind`). Covers a user enum too. Note: a boxed enum formats by its
// underlying value (name formatting needs a static-typed site), so the sample uses
// (int) casts. Real System.Private.CoreLib (-r) -> run vs .NET.
using System;
namespace ExternalEnumSubset;


enum Team { Red, Blue, Green }

class Program
{internal static void __GateEntry()
    {
        object o1 = DayOfWeek.Wednesday;
        object o2 = "not an enum";
        object o3 = DateTimeKind.Utc;
        object o4 = Team.Green;
        object o5 = 42;

        // isinst: positive, negative, and cross-enum / enum-vs-int discrimination
        Console.WriteLine("is1=" + (o1 is DayOfWeek));
        Console.WriteLine("is2=" + (o2 is DayOfWeek));
        Console.WriteLine("is3=" + (o3 is DateTimeKind));
        Console.WriteLine("crossEnum=" + (o1 is DateTimeKind));
        Console.WriteLine("enumVsInt=" + (o1 is int));
        Console.WriteLine("intVsEnum=" + (o5 is DayOfWeek));
        Console.WriteLine("userEnum=" + (o4 is Team));
        Console.WriteLine("userVsExt=" + (o4 is DayOfWeek));

        // is-with-binding + compare against an enum constant
        if (o1 is DayOfWeek d)
            Console.WriteLine("bound int=" + (int)d + " isWed=" + (d == DayOfWeek.Wednesday));

        // explicit cast of a boxed enum
        var c = (DayOfWeek)o1;
        Console.WriteLine("cast int=" + (int)c);

        // switch type-pattern mixing external enums, a user enum, int, and a string
        object[] arr = { DayOfWeek.Monday, DateTimeKind.Local, Team.Blue, 7, "str" };
        foreach (var x in arr)
        {
            string s = x switch
            {
                DayOfWeek dw => "day:" + (int)dw,
                DateTimeKind dk => "kind:" + (int)dk,
                Team tm => "team:" + (int)tm,
                int n => "int:" + n,
                _ => "other",
            };
            Console.WriteLine(s);
        }
    }
}
