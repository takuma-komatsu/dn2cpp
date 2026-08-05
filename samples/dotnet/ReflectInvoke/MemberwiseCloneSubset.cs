#nullable enable
using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading;

// SUBJECT: Object.MemberwiseClone through both of its mouths — the call-site
// route (lowered to a runtime helper) and a reflective Invoke, which must reach
// the same helper through the methtab invoker. Reflection is also the only way
// an ARRAY or a STRING is ever the receiver, since neither can be derived from.
//
// A cloned string is diffed by length, type and identity only: real .NET returns
// the right Length and first char over uninitialized heap after that, so there is
// no deterministic content to match.
namespace MemberwiseCloneSubset;

static class Program
{
    class Base
    {
        public int Id;
        public string Tag = "";
        public override string ToString() => "Base(" + Id + "," + Tag + ")";
    }

    sealed class Derived : Base
    {
        public int[] Payload = Array.Empty<int>();
        public double Weight;
        // The clone must be a Derived of the full derived size, not a Base-sized
        // truncation: Payload/Weight live past sizeof(Base).
        public Derived ShallowCopy() => (Derived)MemberwiseClone();
        public override string ToString() =>
            "Derived(" + Id + "," + Tag + "," + Payload.Length + "," + Weight + ")";
    }

    struct Holder
    {
        public int A;
        public string S;
        public Holder(int a, string s) { A = a; S = s; }
        // A struct's MemberwiseClone boxes `this` and clones the box.
        public object BoxClone() => MemberwiseClone();
        public override string ToString() => "Holder(" + A + "," + S + ")";
    }

    sealed class Generic<T>
    {
        public T Value = default!;
        public string Name = "";
        public Generic<T> ShallowCopy() => (Generic<T>)MemberwiseClone();
        public override string ToString() => "Generic(" + Value + "," + Name + ")";
    }

    public static void Run()
    {
        Console.WriteLine("-- MemberwiseCloneSubset --");

        // 1. A class instance: fresh object, fields bit-copied, reference fields SHARED.
        int[] shared = { 1, 2, 3 };
        var d = new Derived { Id = 7, Tag = "t", Payload = shared, Weight = 2.5 };
        Derived dc = d.ShallowCopy();
        Console.WriteLine(dc);
        Console.WriteLine(ReferenceEquals(d, dc));
        Console.WriteLine(ReferenceEquals(d.Payload, dc.Payload)); // shallow: same array
        Console.WriteLine(dc.GetType().FullName);
        dc.Id = 99;
        Console.WriteLine(d.Id + " " + dc.Id); // independent storage

        // 2. A boxed value type.
        var h = new Holder(5, "s");
        object hb = h.BoxClone();
        Console.WriteLine((Holder)hb);
        Console.WriteLine(hb.GetType().FullName);

        // 3. A closed generic class.
        var g = new Generic<string> { Value = "gv", Name = "gn" };
        Console.WriteLine(g.ShallowCopy());
        var gi = new Generic<int> { Value = 42, Name = "gi" };
        Console.WriteLine(gi.ShallowCopy());

        // 4. The reflective mouth. `MemberwiseClone` is protected, so this is the only
        //    way an array or a string is ever the receiver.
        MethodInfo? mwc = typeof(object).GetMethod(
            "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        Console.WriteLine(mwc is null ? "<null>" : mwc.Name);
        Console.WriteLine(mwc!.DeclaringType!.FullName + " " + mwc.ReturnType.FullName
            + " " + mwc.IsStatic + " " + mwc.IsPublic + " " + mwc.GetParameters().Length);
        // Handle identity: repeated lookups intern to one MethodInfo, as on real .NET.
        MethodInfo again = typeof(object).GetMethod(
            "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Console.WriteLine(ReferenceEquals(mwc, again) + " " + mwc.Equals(again));
        // Found from a DERIVED type, still declared on Object, and invocable. Both
        // runtimes hand back a DISTINCT MethodInfo because ReflectedType differs:
        // handles intern per (row, reflectedType), as .NET's cache is keyed.
        MethodInfo? viaDerived = typeof(Derived).GetMethod(
            "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        Console.WriteLine(viaDerived is null ? "<null>" : viaDerived.DeclaringType!.FullName);
        Console.WriteLine(viaDerived!.ReflectedType!.Name + " " + (viaDerived == mwc)
            + " " + viaDerived.Equals(mwc));
        Console.WriteLine(((Derived)viaDerived!.Invoke(d, null)!).Tag);
        // DeclaredOnly stops at the queried type, so a derived type does not answer it.
        Console.WriteLine(typeof(Derived).GetMethod("MemberwiseClone",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is null);
        // Wrong BindingFlags do not find a protected instance member.
        Console.WriteLine(typeof(object).GetMethod("MemberwiseClone",
            BindingFlags.Instance | BindingFlags.Public) is null);
        Console.WriteLine(typeof(object).GetMethod("MemberwiseClone",
            BindingFlags.Static | BindingFlags.NonPublic) is null);

        int[] ai = { 1, 2, 3, 4, 5 };
        var ai2 = (int[])mwc!.Invoke(ai, null)!;
        Console.WriteLine(ai2.Length + " [" + string.Join(",", ai2) + "] " + ReferenceEquals(ai, ai2));
        ai2[0] = 99;
        Console.WriteLine(ai[0] + " " + ai2[0]); // independent element block

        string[] asr = { "a", "b", "c" };
        var asr2 = (string[])mwc.Invoke(asr, null)!;
        Console.WriteLine(asr2.Length + " [" + string.Join(",", asr2) + "] "
            + ReferenceEquals(asr[0], asr2[0])); // shallow: elements shared

        var ab = new byte[1000];
        ab[999] = 77;
        var ab2 = (byte[])mwc.Invoke(ab, null)!;
        Console.WriteLine(ab2.Length + " " + ab2[999]);

        var ae = (int[])mwc.Invoke(Array.Empty<int>(), null)!;
        Console.WriteLine(ae.Length + " " + ae.GetType().FullName);

        var al = new long[] { -1L, 2L };
        var al2 = (long[])mwc.Invoke(al, null)!;
        Console.WriteLine(al2.Length + " " + al2[0] + " " + al2[1]);

        // An MD array's lengths and element block live INSIDE the one allocation, so
        // a clone that copied only the header would alias the source's elements.
        var md = new int[2, 3];
        md[1, 2] = 7;
        var md2 = (int[,])mwc.Invoke(md, null)!;
        Console.WriteLine(md2.Length + " " + md2.GetLength(0) + "x" + md2.GetLength(1)
            + " " + md2[1, 2] + " " + ReferenceEquals(md, md2));
        md2[1, 2] = 8;
        Console.WriteLine(md[1, 2] + " " + md2[1, 2]);

        // The string: length, type and identity only — see the file header.
        string s = "hello world";
        object sc = mwc.Invoke(s, null)!;
        Console.WriteLine(((string)sc).Length + " " + sc.GetType().FullName + " " + ReferenceEquals(s, sc));
        Console.WriteLine(((string)mwc.Invoke("", null)!).Length);

        // A reflected clone of an ordinary class agrees with the call-site route.
        var rd = (Derived)mwc.Invoke(d, null)!;
        Console.WriteLine(rd + " " + ReferenceEquals(rd.Payload, d.Payload));

        // A bare System.Object: no fields, but still a distinct instance of the same type.
        object bare = new object();
        object bareClone = mwc.Invoke(bare, null)!;
        Console.WriteLine(bareClone.GetType().FullName + " " + ReferenceEquals(bare, bareClone));

        // A null receiver is a TargetException/NullReferenceException wrapped by Invoke
        // on both runtimes; only the fact that it threw is diffed.
        try
        {
            mwc.Invoke(null, null);
            Console.WriteLine("no-throw");
        }
        catch (Exception)
        {
            Console.WriteLine("null receiver threw");
        }

        // 5. INTRINSIC-REPRESENTED reference types: hand-written C++ structs rather
        //    than emitted classes, so the clone's byte extent comes from the type-info
        //    or the allocator floor. The shapes dn2cpp still refuses are frozen in
        //    ReflectShallowCloneRefusalSubset.
        //
        //    StringBuilder and CancellationTokenSource print GetType().FullName rather
        //    than casting: for an intrinsic-MAPPED reference type the emitter mints its
        //    own shell type-info, so `o is StringBuilder` over an `object` is False and
        //    the cast throws. The name is still the whole claim — a refusal would have
        //    thrown before printing anything. Their bit copy IS .NET's shallow copy.
        var sb = new StringBuilder("abc");
        object sbc = mwc.Invoke(sb, null)!;
        Console.WriteLine(sbc.GetType().FullName + "|" + ReferenceEquals(sb, sbc));
        var cts = new CancellationTokenSource();
        object ctsc = mwc.Invoke(cts, null)!;
        Console.WriteLine(ctsc.GetType().FullName + "|" + ReferenceEquals(cts, ctsc));

        //    The hand-written exception type-infos state no extent of their own — they
        //    are shells over one prefix struct — so the clone's extent comes from the
        //    allocator's floor. Both rows read fields PAST the object header: a clone
        //    truncated to the header would still print a plausible type name.
        var iex = new InvalidOperationException("boom");
        var iexc = (InvalidOperationException)mwc.Invoke(iex, null)!;
        Console.WriteLine(iexc.GetType().FullName + "|" + iexc.Message + "|"
            + ReferenceEquals(iex, iexc));
        var inner = new Exception("inner");
        var bex = new Exception("outer", inner);
        var bexc = (Exception)mwc.Invoke(bex, null)!;
        Console.WriteLine(bexc.GetType().FullName + "|" + bexc.Message + "|"
            + bexc.InnerException!.Message + "|"
            + ReferenceEquals(inner, bexc.InnerException)); // shallow: the inner is SHARED

        //    A reflection handle and a culture handle: cloned by content, not by their
        //    interning, so the clone is a distinct object answering the same questions.
        var tc = (Type)mwc.Invoke(typeof(int), null)!;
        Console.WriteLine(tc.FullName + "|" + ReferenceEquals(tc, typeof(int)));
        //    The culture row omits the clone's identity: dn2cpp lowers a CultureInfo to
        //    a headerless descriptor pointer re-boxed on every escape into `object`, so
        //    its reference equality is by wrapped content — a statement about the
        //    culture representation, not about cloning. Name is read off the wrapped
        //    pointer, past the object header, so a truncated clone cannot print it.
        var cc = (CultureInfo)mwc.Invoke(CultureInfo.InvariantCulture, null)!;
        Console.WriteLine("[" + cc.Name + "]|" + (cc.Name.Length == 0));
    }
}
