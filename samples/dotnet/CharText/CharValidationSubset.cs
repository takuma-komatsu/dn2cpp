using System;

namespace CharValidationSubset;

internal static class Program
{
    private static void Fault(string label, Exception ex)
    {
        Console.WriteLine(label + " type=" + ex.GetType().Name);
        Console.WriteLine(label + " param=" + (ex is ArgumentException arg ? arg.ParamName : null));
        Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
        Console.WriteLine(label + " message units=" + Units(ex.Message));
        object actual = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
        Console.WriteLine(label + " actual=" + (actual is null ? "null" : actual.GetType().Name + ":" + actual));
    }
    private static void Observe(string label, Func<string> action)
    {
        try { Console.WriteLine(label + " success=" + action()); }
        catch (Exception ex) { Fault(label, ex); }
    }

    private static string Units(string value)
    {
        if (value is null)
            return "null";
        string result = "";
        for (int i = 0; i < value.Length; i++)
            result += (i == 0 ? "" : ",") + (int)value[i];
        return result;
    }


    internal static void Run()
    {
        Console.WriteLine("== Char indexed validation fields ==");
        string[] values = { null, "", "a", "9", " ", "\0", "\u00a0", "\u20ac", "\ud83d\ude42" };
        int[] bounds = { int.MinValue, -1, 0, 1, 2, int.MaxValue };
        for (int s = 0; s < values.Length; s++)
            foreach (int index in bounds)
            {
                string value = values[s];
                if (value is not null && value.Length > 1 && index >= 0 && index < value.Length)
                    continue;
                string label = s + ":" + index;
                Observe("digit:" + label, () => char.IsDigit(value, index).ToString());
                Observe("letter:" + label, () => char.IsLetter(value, index).ToString());
                Observe("letter digit:" + label, () => char.IsLetterOrDigit(value, index).ToString());
                Observe("number:" + label, () => char.IsNumber(value, index).ToString());
                Observe("separator:" + label, () => char.IsSeparator(value, index).ToString());
                Observe("white:" + label, () => char.IsWhiteSpace(value, index).ToString());
                Observe("control:" + label, () => char.IsControl(value, index).ToString());
                Observe("punctuation:" + label, () => char.IsPunctuation(value, index).ToString());
                Observe("symbol:" + label, () => char.IsSymbol(value, index).ToString());
                Observe("upper:" + label, () => char.IsUpper(value, index).ToString());
                Observe("lower:" + label, () => char.IsLower(value, index).ToString());
                Observe("high:" + label, () => char.IsHighSurrogate(value, index).ToString());
                Observe("low:" + label, () => char.IsLowSurrogate(value, index).ToString());
                Observe("surrogate:" + label, () => char.IsSurrogate(value, index).ToString());
                Observe("pair:" + label, () => char.IsSurrogatePair(value, index).ToString());
                Observe("category:" + label, () => ((int)char.GetUnicodeCategory(value, index)).ToString());
                Observe("numeric:" + label, () => char.GetNumericValue(value, index).ToString());
                Observe("utf32:" + label, () => char.ConvertToUtf32(value, index).ToString());
            }
        Observe("valid utf32 pair", () => char.ConvertToUtf32("\ud83d\ude42", 0).ToString());
        Observe("valid surrogate pair", () => char.IsSurrogatePair("\ud83d\ude42", 0).ToString());
        Exception[] saved = new Exception[3];
        try { char.IsDigit(null, int.MaxValue); } catch (Exception ex) { saved[0] = ex; }
        try { char.IsLetter("a", int.MinValue); } catch (Exception ex) { saved[1] = ex; }
        try { char.ConvertToUtf32("a", int.MaxValue); } catch (Exception ex) { saved[2] = ex; }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        for (int i = 0; i < saved.Length; i++)
            Fault("char index after GC:" + i, saved[i]);
        Console.WriteLine("Char indexed validation fields end");
    }
}
