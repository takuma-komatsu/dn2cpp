using System;
using System.Collections;
using System.Globalization;
sealed class NullMatch
{
    public int Calls;
    public override bool Equals(object value) { Calls++; return value is null; }
    public override int GetHashCode() => 0;
}
sealed class NeverMatch
{
    public int Calls;
    public override bool Equals(object value) { Calls++; return false; }
    public override int GetHashCode() => 0;
}
static class ArrayLowerBoundsOnlyProgram
{
    static void Extreme(string label, Func<object> call)
    {
        try { Console.WriteLine($"{label}={call()}"); }
        catch (ArgumentOutOfRangeException e) { Console.WriteLine($"{label}={e.GetType().Name}/{e.ParamName}/{e.Message}"); }
    }

    static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== rank1 non-SZ collection search ==");
        Array a = Array.CreateInstance(typeof(int), new[] { 2 }, new[] { -2 });
        a.SetValue(17, -2); a.SetValue(19, -1);
        int sum = 0;
        foreach (object value in a) sum += (int)value;
        IList list = (IList)a;
        Console.WriteLine($"rank1-only={a.GetType()}:{a.GetType().IsSZArray}:{sum}:{list[-2]}:{list.IndexOf(19)}:{list.IndexOf(99)}:{list.Contains(17)}:{list.Contains(99)}");
        int[] copy = new int[2]; ((ICollection)a).CopyTo(copy, 0);
        Console.WriteLine($"copied={copy[0]}/{copy[1]}");
        list.Clear(); Console.WriteLine($"cleared={a.GetValue(-2)}/{a.GetValue(-1)}");
        Console.WriteLine("rank1 non-SZ collection search end");
        Console.WriteLine("== rank1 collection virtual equality ==");
        foreach (int lower in new[] { -2, 5 })
        {
            NullMatch value = new NullMatch();
            Array refs = Array.CreateInstance(typeof(NullMatch), new[] { 2 }, new[] { lower });
            refs.SetValue(value, lower);
            IList listRefs = (IList)refs;
            Console.WriteLine($"collection null={lower}:{listRefs.IndexOf(null)}/{value.Calls}");
            value.Calls = 0;
            Console.WriteLine($"collection contains null={lower}:{listRefs.Contains(null)}/{value.Calls}");
            NeverMatch self = new NeverMatch();
            Array misses = Array.CreateInstance(typeof(NeverMatch), new[] { 1 }, new[] { lower });
            misses.SetValue(self, lower);
            IList listMisses = (IList)misses;
            Console.WriteLine($"collection self={lower}:{listMisses.IndexOf(self)}/{self.Calls}");
            self.Calls = 0;
            Console.WriteLine($"collection contains self={lower}:{listMisses.Contains(self)}/{self.Calls}");
        }
        Console.WriteLine("rank1 collection virtual equality end");
        if (args.Length != 0 && args[0] == "before-extreme-collection") return;
        Console.WriteLine("== rank1 collection extreme bounds ==");
        foreach (int lower in new[] { int.MaxValue, int.MinValue })
        {
            foreach (int length in new[] { 1, 0 })
            {
                Array extreme = Array.CreateInstance(typeof(int), new[] { length }, new[] { lower });
                if (length != 0) extreme.SetValue(7, lower);
                IList extremeList = (IList)extreme;
                string label = $"collection extreme={lower}/{length}";
                Extreme(label + " index hit", () => extremeList.IndexOf(7));
                Extreme(label + " index miss", () => extremeList.IndexOf(9));
                Extreme(label + " contains hit", () => extremeList.Contains(7));
                Extreme(label + " contains miss", () => extremeList.Contains(9));
            }
        }
        Console.WriteLine("rank1 collection extreme bounds end");
    }
}
