#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;

// String dispatched through its CLR interfaces: CharEnumerator, the non-generic
// IEnumerable path, IComparable/IEquatable/ICloneable, generic constrained dispatch
// with T=string, and isinst. CompareTo is printed as Math.Sign over same-case pairs,
// where dn2cpp's ordinal ordering and real .NET's culture compare agree.
namespace StringEnumerableSubset;

internal static class Program
{
    private static int Cmp<T>(T a, T b) where T : IComparable<T> => Math.Sign(a.CompareTo(b));

    internal static int Run()
    {
        string s = "abc";

        // Explicit CharEnumerator: MoveNext/Current, Reset, Clone (shares state
        // at the cloned position), Dispose via using.
        var en = s.GetEnumerator();
        Console.WriteLine($"move1={en.MoveNext()}/{en.Current}");
        Console.WriteLine($"move2={en.MoveNext()}/{en.Current}");
        var clone = (CharEnumerator)((ICloneable)en).Clone();
        Console.WriteLine($"clone={clone.Current}/{clone.MoveNext()}/{clone.Current}");
        en.Reset();
        Console.WriteLine($"reset={en.MoveNext()}/{en.Current}");
        using (var d = "z".GetEnumerator())
        {
            d.MoveNext();
            Console.WriteLine($"using={d.Current}");
        }

        // foreach over the interface (no index-loop lowering — real dispatch).
        int n = 0;
        IEnumerable<char> e = "Hello World";
        foreach (char c in e)
            if (c == 'l')
                n++;
        Console.WriteLine($"generic={n}");

        // Non-generic IEnumerable — each element arrives boxed.
        var sb = new System.Text.StringBuilder();
        foreach (object o in (IEnumerable)s)
            sb.Append((char)o);
        Console.WriteLine($"nonGeneric={sb}");

        // Interface-typed comparison/equality/clone dispatch.
        Console.WriteLine($"cmpObj={Math.Sign(((IComparable)s).CompareTo("abd"))}");
        Console.WriteLine($"cmpTyped={Math.Sign(((IComparable<string>)s).CompareTo("ab"))}");
        Console.WriteLine($"cmpSelf={((IComparable<string>)s).CompareTo("abc")}");
        Console.WriteLine($"equatable={((IEquatable<string>)s).Equals("abc")}/{((IEquatable<string>)s).Equals("abd")}");
        Console.WriteLine($"cloneSelf={ReferenceEquals(((ICloneable)s).Clone(), s)}");

        // Generic constrained dispatch, T = string.
        Console.WriteLine($"generic3way={Cmp("apple", "banana")}/{Cmp("pear", "pear")}/{Cmp("walnut", "pear")}");

        // IConvertible dispatch — the impls delegate to the modeled Convert
        // forms (provider dropped as invariant).
        IConvertible conv = (IConvertible)(object)"42";
        Console.WriteLine($"convI32={conv.ToInt32(null)}");
        Console.WriteLine($"convI64={conv.ToInt64(null)}");
        Console.WriteLine($"convDbl={conv.ToDouble(null)}");
        Console.WriteLine($"convBool={((IConvertible)(object)"true").ToBoolean(null)}");
        Console.WriteLine($"convChar={((IConvertible)(object)"x").ToChar(null)}");
        Console.WriteLine($"convStr={conv.ToString(null)}");
        Console.WriteLine($"typeCode={conv.GetTypeCode()}");

        // isinst through object.
        object boxed = s;
        Console.WriteLine($"is={boxed is IEnumerable<char>}/{boxed is IEnumerable}/{boxed is IComparable}/{boxed is ICloneable}");
        Console.WriteLine($"isTyped={boxed is IEquatable<string>}/{boxed is IComparable<string>}");
        return 0;
    }
}
