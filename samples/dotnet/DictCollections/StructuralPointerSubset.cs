using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Metadata;

namespace StructuralPointerSubset;

internal static class Program
{
    private static int checks;
    private static void Check(string label, bool value)
    {
        Console.WriteLine("structural-pointer-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
        checks++;
    }
    public static void __GateEntry() => Run();
    public static unsafe void Run()
    {
        checks = 0;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        byte* a = stackalloc byte[8];
        byte* b = stackalloc byte[8];
        for (int i = 0; i < 8; i++) { a[i] = 42; b[i] = 42; }
        var reader = new BlobReader(a, 8);
        var same = new BlobReader(a, 8);
        var different = new BlobReader(b, 8);
        var shifted = same; shifted.Offset = 1;
        Check("reader-same", reader.Equals(same));
        Check("reader-different-buffer", !reader.Equals(different));
        Check("reader-offset", !reader.Equals(shifted));
        Check("reader-length", !reader.Equals(new BlobReader(a, 7)));
        var x = new Item(new string(new[] { 'a' }), reader, 8);
        var y = new Item(new string(new[] { 'a' }), same, 8);
        var z = new Item("a", different, 8);
        var w = new Item("a", shifted, 8);
        Check("item-equal", EqualityComparer<Item>.Default.Equals(x, y));
        Check("item-buffer", !x.Equals(z));
        Check("item-offset", !x.Equals(w));
        Check("item-name", !x.Equals(new Item("b", reader, 8)));
        Check("item-length", !x.Equals(new Item("a", reader, 7)));
        Check("item-hash", x.GetHashCode() == y.GetHashCode());
        Item[] values = { x, z, x };
        Check("index", Array.IndexOf(values, y) == 0);
        Check("last", Array.LastIndexOf(values, y) == 2);
        Check("index-range", Array.IndexOf(values, y, 1, 2) == 2);
        Check("last-range", Array.LastIndexOf(values, y, 1, 2) == 0);
        Check("miss", Array.IndexOf(values, w) == -1);
        Check("empty-range", Array.IndexOf(values, y, 3, 0) == -1);
        Check("empty-array", Array.IndexOf(Array.Empty<Item>(), x) == -1);
        IList<Item> segment = new ArraySegment<Item>(new[] { z, x, z }, 1, 2);
        Check("segment-relative", segment.IndexOf(y) == 0);
        Check("segment-miss", segment.IndexOf(w) == -1);
        bool range = false;
        try { Array.IndexOf(values, y, -1, 1); }
        catch (ArgumentOutOfRangeException) { range = true; }
        Check("range-validation", range);
        var mp = new Mixed("n", a, double.NaN);
        var mq = new Mixed(new string(new[] { 'n' }), a, double.NaN);
        var md = new Mixed("n", b, double.NaN);
        Check("nan", mp.Equals(mq));
        Check("mixed-buffer", !mp.Equals(md));
        Check("mixed-hash", mp.GetHashCode() == mq.GetHashCode());
        var plus = new Mixed(null, a, 0.0); var minus = new Mixed(null, a, -0.0);
        Check("signed-zero", plus.Equals(minus));
        Check("signed-zero-hash", plus.GetHashCode() == minus.GetHashCode());
        Check("null", default(Mixed).Equals(new Mixed(null, null, 0.0)));
        Check("null-hash", default(Mixed).GetHashCode() == new Mixed(null, null, 0.0).GetHashCode());
        // These opaque values are NEVER dereferenced, even by oracle construction.
        var high = new Mixed(null, (byte*)unchecked((nuint)(-1)), 0.0);
        var highSame = new Mixed(null, (byte*)unchecked((nuint)(-1)), 0.0);
        var highOther = new Mixed(null, (byte*)unchecked((nuint)(-2)), 0.0);
        Check("high-equal", high.Equals(highSame));
        Check("high-different", !high.Equals(highOther));
        Check("high-hash", high.GetHashCode() == highSame.GetHashCode());
        Check("nested", new Nested(mp).Equals(new Nested(mq)));
        Check("nested-different", !new Nested(mp).Equals(new Nested(md)));
        Check("nullable", EqualityComparer<Mixed?>.Default.Equals(mp, mq));
        object box1 = mp, box2 = mq;
        Check("boxed-equal", box1.Equals(box2));
        Check("boxed-hash", box1.GetHashCode() == box2.GetHashCode());
        var dictionary = new Dictionary<Item, int>(); dictionary[x] = 17;
        Check("dictionary", dictionary.TryGetValue(y, out int value) && value == 17 && !dictionary.ContainsKey(z));
        var set = new HashSet<Mixed>(); set.Add(mp);
        Check("hashset", set.Contains(mq) && !set.Contains(md) && !set.Add(mq));
        var t1 = new Typed(1, 2); var t2 = new Typed(1, 3);
        Check("typed-precedence", EqualityComparer<Typed>.Default.Equals(t1, t2));
        Check("nested-object-precedence", !new TypedOuter(a, t1).Equals(new TypedOuter(a, t2)));
        Console.WriteLine("structural-pointer-checks:" + checks);
    }
}
internal readonly struct Item
{
    public readonly string Name; public readonly BlobReader Reader; public readonly int Length;
    public Item(string name, BlobReader reader, int length) { Name = name; Reader = reader; Length = length; }
}
internal unsafe readonly struct Mixed
{
    public readonly string Name; public readonly byte* Pointer; public readonly double Value;
    public Mixed(string name, byte* pointer, double value) { Name = name; Pointer = pointer; Value = value; }
}
internal readonly struct Nested { public readonly Mixed Value; public Nested(Mixed value) { Value = value; } }
internal readonly struct Typed : IEquatable<Typed>
{
    public readonly int Key; public readonly int Noise;
    public Typed(int key, int noise) { Key = key; Noise = noise; }
    public bool Equals(Typed other) => Key == other.Key;
}
internal unsafe readonly struct TypedOuter
{
    public readonly byte* Pointer; public readonly Typed Value;
    public TypedOuter(byte* pointer, Typed value) { Pointer = pointer; Value = value; }
}
