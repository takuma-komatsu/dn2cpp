using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace GCHandleEqualitySubset;

internal static class Program
{
    private static int checks;
    private static void Check(string label, bool value)
    {
        Console.WriteLine("gchandle-equality-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
        checks++;
    }
    public static void __GateEntry() => Run();
    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        checks = 0;
        GCHandle first = default, other = default, pinned = default;
        try
        {
            var target = new byte[] { 1, 2, 3 };
            first = GCHandle.Alloc(target, GCHandleType.Normal);
            other = GCHandle.Alloc(target, GCHandleType.Normal);
            pinned = GCHandle.Alloc(target, GCHandleType.Pinned);
            GCHandle copy = first, pinnedCopy = pinned;
            var comparer = EqualityComparer<GCHandle>.Default;
            Check("copy", comparer.Equals(first, copy));
            Check("same-target-different-cell", !comparer.Equals(first, other));
            Check("pinned-copy", comparer.Equals(pinned, pinnedCopy));
            Check("normal-vs-pinned", !comparer.Equals(first, pinned));
            Check("default", comparer.Equals(default, default));
            Check("nondefault", !comparer.Equals(first, default));
            Check("copy-hash", comparer.GetHashCode(first) == comparer.GetHashCode(copy));
            Check("direct-hash", comparer.GetHashCode(first) == first.GetHashCode());
            Check("pinned-hash", comparer.GetHashCode(pinned) == pinned.GetHashCode());
            Check("default-hash", comparer.GetHashCode(default) == default(GCHandle).GetHashCode());
            var x = new Holder(pinned, 3); var y = new Holder(pinnedCopy, 3); var z = new Holder(other, 3);
            Check("holder", EqualityComparer<Holder>.Default.Equals(x, y));
            Check("holder-cell", !x.Equals(z));
            Check("holder-length", !x.Equals(new Holder(pinned, 4)));
            Check("holder-hash", x.GetHashCode() == y.GetHashCode());
            Check("holder-default", default(Holder).Equals(new Holder(default, 0)));
            Check("nested", new Nested(x).Equals(new Nested(y)));
            Check("nested-cell", !new Nested(x).Equals(new Nested(z)));
            Check("nullable", EqualityComparer<GCHandle?>.Default.Equals(first, copy));
            Check("nullable-missing", !EqualityComparer<GCHandle?>.Default.Equals(first, null));
            Check("nullable-null", EqualityComparer<GCHandle?>.Default.Equals(null, null));
            Check("nullable-holder", new NullableHolder(first).Equals(new NullableHolder(copy)));
            Check("nullable-holder-null", new NullableHolder(null).Equals(new NullableHolder(null)));
            Check("nullable-holder-hash", new NullableHolder(first).GetHashCode() == new NullableHolder(copy).GetHashCode());
            object box1 = x, box2 = y;
            Check("boxed-holder", box1.Equals(box2));
            Check("boxed-holder-hash", box1.GetHashCode() == box2.GetHashCode());
            var pair = new KeyValuePair<uint, Holder>(7u, x);
            var pairCopy = new KeyValuePair<uint, Holder>(7u, y);
            var pairOther = new KeyValuePair<uint, Holder>(7u, z);
            Check("pair", pair.Equals(pairCopy));
            Check("pair-key", !pair.Equals(new KeyValuePair<uint, Holder>(8u, y)));
            var values = new[] { pairOther, pair, pairCopy };
            Check("index", Array.IndexOf(values, pairCopy) == 1);
            Check("last", Array.LastIndexOf(values, pair) == 2);
            Check("range", Array.IndexOf(values, pairCopy, 2, 1) == 2);
            Check("last-range", Array.LastIndexOf(values, pair, 1, 2) == 1);
            Check("empty-range", Array.IndexOf(values, pairCopy, 3, 0) == -1);
            IList<KeyValuePair<uint, Holder>> segment = new ArraySegment<KeyValuePair<uint, Holder>>(values, 1, 2);
            Check("segment-relative", segment.IndexOf(pairCopy) == 0);
            Check("segment-miss", segment.IndexOf(pairOther) == -1);
            var dictionary = new Dictionary<Holder, int>(); dictionary[x] = 13;
            Check("dictionary", dictionary.TryGetValue(y, out int value) && value == 13 && !dictionary.ContainsKey(z));
            var handles = new HashSet<GCHandle>(); handles.Add(first);
            Check("hashset", handles.Contains(copy) && !handles.Contains(other) && !handles.Add(copy));
            int staleHash = comparer.GetHashCode(copy);
            first.Free();
            Check("freed-original-default", comparer.Equals(first, default));
            Check("stale-copy-token", !comparer.Equals(copy, first));
            Check("stale-copy-hash", comparer.GetHashCode(copy) == staleHash && comparer.GetHashCode(copy) == copy.GetHashCode());
            // Never read Target/AddrOfPinnedObject or Free any copied/stale handle.
        }
        finally
        {
            if (first.IsAllocated) first.Free();
            if (other.IsAllocated) other.Free();
            if (pinned.IsAllocated) pinned.Free();
        }
        Console.WriteLine("gchandle-equality-checks:" + checks);
    }
}
internal readonly struct Holder
{
    public readonly GCHandle Handle;
    public readonly int Length;
    public Holder(GCHandle handle, int length) { Handle = handle; Length = length; }
}
internal readonly struct Nested { public readonly Holder Value; public Nested(Holder value) { Value = value; } }
internal readonly struct NullableHolder { public readonly GCHandle? Value; public NullableHolder(GCHandle? value) { Value = value; } }
