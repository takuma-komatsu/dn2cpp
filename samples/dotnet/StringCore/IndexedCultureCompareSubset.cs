using System;
using System.Globalization;

namespace IndexedCultureCompareSubset;

internal static class Program
{
    private static string _evaluation = "";

    private static string Text(string step, string value)
    {
        _evaluation += step;
        return value;
    }

    private static int Number(string step, int value)
    {
        _evaluation += step;
        return value;
    }

    private static bool Flag(bool value)
    {
        _evaluation += "F";
        return value;
    }

    private static CultureInfo Culture(bool fail)
    {
        _evaluation += "C";
        if (fail)
            throw new InvalidOperationException();
        return CultureInfo.InvariantCulture;
    }

    private static CompareOptions Options()
    {
        _evaluation += "O";
        return CompareOptions.IgnoreCase;
    }

    private static string Show(Func<int> compare)
    {
        try
        {
            return Math.Sign(compare()).ToString();
        }
        catch (ArgumentException ex)
        {
            return ex.GetType().Name + ":" + ex.ParamName + ":"
                + ex.Message.Replace("\r", "").Replace("\n", "|");
        }
    }

    internal static void Run()
    {
        CultureInfo inv = CultureInfo.InvariantCulture;
        Console.WriteLine("== indexed culture compare ==");
        Console.WriteLine("bool fold=" + Show(() => string.Compare("xxApple", 2, "appleZ", 0, 5, true)));
        Console.WriteLine("bool order=" + Show(() => string.Compare("apple", 0, "banana", 0, 5, false)));
        Console.WriteLine("bool clamp=" + Show(() => string.Compare("ab", 0, "abcd", 0, 4, false)));
        Console.WriteLine("bool offsets=" + Show(() => string.Compare("xxa", 2, "yyb", 2, 1, false)));
        Console.WriteLine("culture null=" + Show(() => string.Compare("Apple", 0, "apple", 0, 5, true, null)));
        Console.WriteLine("culture inv=" + Show(() => string.Compare("HELLO", 0, "hello", 0, 5, true, inv)));
        Console.WriteLine("options fold=" + Show(() => string.Compare("xxApple", 2, "appleZ", 0, 5, inv, CompareOptions.IgnoreCase)));
        Console.WriteLine("options offsets=" + Show(() => string.Compare("xxApple", 2, "yyApplf", 2, 5, inv, CompareOptions.IgnoreCase)));
        Console.WriteLine("options ordinal=" + string.Compare("Apple", 0, "apple", 0, 5, inv, CompareOptions.Ordinal));
        Console.WriteLine("options ordinal-ci=" + Show(() => string.Compare("Apple", 0, "apple", 0, 5, inv, CompareOptions.OrdinalIgnoreCase)));
        Console.WriteLine("options null culture=" + Show(() => string.Compare("ab", 0, "abcd", 0, 4, null, CompareOptions.None)));
        Console.WriteLine("both null=" + Show(() => string.Compare(null, 0, null, 0, 0, inv, CompareOptions.None)));
        Console.WriteLine("null bad window=" + Show(() => string.Compare(null, 1, "a", 0, 0, true)));
        Console.WriteLine("null bad options=" + Show(() => string.Compare(null, 0, "a", 0, 0, inv, (CompareOptions)int.MinValue)));
        Console.WriteLine("bad options=" + Show(() => string.Compare("a", 0, "a", 0, 1, inv, (CompareOptions)int.MinValue)));
        Console.WriteLine("bad ordinal mix=" + Show(() => string.Compare("a", 0, "a", 0, 1, inv, CompareOptions.Ordinal | CompareOptions.IgnoreCase)));
        Console.WriteLine("window before options=" + Show(() => string.Compare("a", 2, "a", 0, 1, inv, (CompareOptions)int.MinValue)));
        Console.WriteLine("end window=" + Show(() => string.Compare("a", 1, "b", 1, int.MaxValue, true)));
        Console.WriteLine("negative length=" + Show(() => string.Compare("a", 0, "a", 0, -1, false)));
        Console.WriteLine("negative first index=" + Show(() => string.Compare("a", -1, "a", 0, 1, false)));
        Console.WriteLine("negative second index=" + Show(() => string.Compare("a", 0, "a", -1, 1, true, inv)));
        Console.WriteLine("wrapped room=" + Show(() => string.Compare("a", int.MinValue, "a", 0, int.MaxValue, false)));
        Console.WriteLine("maximum index=" + Show(() => string.Compare("a", int.MaxValue, "a", 0, 0, inv, CompareOptions.None)));
        Console.WriteLine("null positive length=" + Show(() => string.Compare(null, 0, "a", 0, 1, false)));
        Console.WriteLine("second null bad window=" + Show(() => string.Compare("a", 0, null, 1, 0, inv, CompareOptions.None)));
        Console.WriteLine("both null bad options=" + Show(() => string.Compare(null, 0, null, 0, 0, inv, CompareOptions.Ordinal | CompareOptions.IgnoreCase)));
        Console.WriteLine("ordinal null shortcut=" + Show(() => string.CompareOrdinal(null, -1, "a", 0, -1)));
        Console.WriteLine("comparison null shortcut=" + Show(() => string.Compare(null, -1, "a", 0, -1, StringComparison.Ordinal)));
        _evaluation = "";
        Console.WriteLine("bool evaluation result=" + string.Compare(Text("A", "a"), Number("I", 0),
            Text("B", "a"), Number("J", 0), Number("L", 1), Flag(true), Culture(false)));
        Console.WriteLine("bool evaluation=" + _evaluation);
        _evaluation = "";
        Console.WriteLine("options evaluation result=" + string.Compare(Text("A", "a"), Number("I", 0),
            Text("B", "a"), Number("J", 0), Number("L", 1), Culture(false), Options()));
        Console.WriteLine("options evaluation=" + _evaluation);
        _evaluation = "";
        try
        {
            string.Compare(Text("A", "a"), Number("I", 0), Text("B", "a"), Number("J", 0),
                Number("L", 1), Culture(true), Options());
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("throwing culture evaluation=" + _evaluation);
        }
        Console.WriteLine("indexed culture compare end");
    }

    public static void RunBooleanEquality()
    {
        Console.WriteLine("== Boolean string comparison equality ==");
        string[] names = { null, "", "Name", "name", "NAME", "Count", "count", "名前" };
        foreach (string left in names)
        {
            foreach (string right in names)
            {
                foreach (bool ignoreCase in new[] { false, true })
                    Console.WriteLine((left ?? "<null>") + ":" + (right ?? "<null>") + ":" + ignoreCase + "=" +
                        (string.Compare(left, right, ignoreCase) == 0) + ":" +
                        (string.Compare(left, right, ignoreCase, CultureInfo.InvariantCulture) == 0));
            }
        }
        Console.WriteLine("Boolean string comparison equality end");
    }
}
