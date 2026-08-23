#nullable disable
using System;

// The indexed-string siblings of Char's category-backed classification and
// numeric/category queries. The probes distinguish each Unicode category.
namespace CharStringClassifySubset;

class Program
{
    static void Classify(string s, int i)
    {
        Console.WriteLine(((int)s[i]).ToString("X4")
            + " C=" + char.IsControl(s, i)
            + " D=" + char.IsDigit(s, i)
            + " L=" + char.IsLetter(s, i)
            + " LD=" + char.IsLetterOrDigit(s, i)
            + " l=" + char.IsLower(s, i)
            + " N=" + char.IsNumber(s, i)
            + " P=" + char.IsPunctuation(s, i)
            + " Sep=" + char.IsSeparator(s, i)
            + " Sym=" + char.IsSymbol(s, i)
            + " U=" + char.IsUpper(s, i)
            + " WS=" + char.IsWhiteSpace(s, i)
            + " Cat=" + (int)char.GetUnicodeCategory(s, i)
            + " Num=" + char.GetNumericValue(s, i));
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("-- indexed char classification --");
        string probes = "\u0009Aσ٣²«\u2028€";
        for (int i = 0; i < probes.Length; i++)
        {
            Classify(probes, i);
        }

        try { char.IsLower(null, 0); }
        catch (ArgumentNullException) { Console.WriteLine("lower-null: ArgumentNull"); }
        try { char.IsSymbol("A", 1); }
        catch (ArgumentOutOfRangeException) { Console.WriteLine("symbol-end: ArgumentOutOfRange"); }
        try { char.GetUnicodeCategory("A", -1); }
        catch (ArgumentOutOfRangeException) { Console.WriteLine("category-neg: ArgumentOutOfRange"); }
        try { char.GetNumericValue("", 0); }
        catch (ArgumentOutOfRangeException) { Console.WriteLine("numeric-empty: ArgumentOutOfRange"); }
    }
}
