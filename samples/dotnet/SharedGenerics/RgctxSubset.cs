#nullable enable
// Runtime-generic-context coverage: bodies that hit every rgctx slot kind FROM
// INSIDE a shared body — TypeInfo (boxing T), ArrayTypeInfo (new T[]), ClassAlloc
// (satellite allocation inside Dictionary's shared getters), StaticFieldAddr +
// CctorEnsureFn (a generic static counter through hidden-parameter forwarders),
// and a struct receiver dispatched both directly and through an interface. Every
// shape runs over two same-width enums so the bodies genuinely share. Real
// System.Private.CoreLib (-r), diffed against .NET.
using System;
using System.Collections.Generic;
namespace RgctxSubset;

enum Metal { Gold = 1, Silver = 2, Lead = 3 }
enum Gem { Ruby = 1, Opal = 2, Jade = 3 }

// Reference receiver: the context derives from the receiver's dynamic type at
// the declaring level.
class Pack<T> where T : struct
{
    public object BoxIt(T v) => v;
    public T[] MakeArr(int n) => new T[n];
}

// The counter is per real instantiation and its cctor runs on first use there,
// so the first Bump of each returns seed+1.
class Tally<T> where T : struct
{
    private static int s_count = 5;

    public static int Bump()
    {
        s_count++;
        return s_count;
    }
}

interface IBoxer
{
    object Box();
}

// Box() takes the hidden rgctx parameter: the direct call and the boxed
// interface dispatch must reach the same per-instantiation forwarder.
struct SBox<T> : IBoxer where T : struct
{
    private readonly T _v;

    public SBox(T v)
    {
        _v = v;
    }

    public object Box() => _v;
}

// The Keys/Values satellites allocate lazily INSIDE Dictionary's shared getters.
class DictUser<T> where T : struct
{
    public ICollection<T> KeysOf(Dictionary<T, string> d) => d.Keys;

    public ICollection<string> ValuesOf(Dictionary<T, string> d) => d.Values;

    // Boxes the struct enumerator to its interface inside a shared body, then
    // drains it through interface dispatch.
    public List<string> Drain(Dictionary<T, string> d)
    {
        IEnumerator<KeyValuePair<T, string>> e = d.GetEnumerator();
        var got = new List<string>();
        while (e.MoveNext())
            got.Add(e.Current.Key + "=" + e.Current.Value);
        return got;
    }
}

class Program
{
    internal static void __GateEntry()
    {
        // The box must carry the REAL enum's identity, not the placeholder's.
        var pm = new Pack<Metal>();
        var pg = new Pack<Gem>();
        object bm = pm.BoxIt(Metal.Silver);
        object bg = pg.BoxIt(Gem.Jade);
        Console.WriteLine("box metal=" + bm + " is Metal=" + (bm is Metal) + " is Gem=" + (bm is Gem));
        Console.WriteLine("box gem=" + bg + " is Gem=" + (bg is Gem));
        Console.WriteLine("box types=" + (bm.GetType() == typeof(Metal)) + "," + (bg.GetType() == typeof(Gem)));

        // A shared-body new T[] has the EXACT array type.
        Metal[] ma = pm.MakeArr(2);
        Gem[] ga = pg.MakeArr(3);
        Console.WriteLine("arr types=" + (ma.GetType() == typeof(Metal[])) + "," + (ga.GetType() == typeof(Gem[])));
        Console.WriteLine("arr cross=" + (ma.GetType() == (object)ga.GetType()));
        var lm = new List<Metal> { Metal.Gold, Metal.Lead };
        Console.WriteLine("list arr=" + (lm.ToArray().GetType() == typeof(Metal[])));

        // Independent per-instantiation counters, cctor seed observed on first use.
        Console.WriteLine("bump metal=" + Tally<Metal>.Bump() + "," + Tally<Metal>.Bump());
        Console.WriteLine("bump gem=" + Tally<Gem>.Bump());
        Func<int> viaDelegate = Tally<Metal>.Bump;
        Console.WriteLine("bump delegate=" + viaDelegate());

        // The struct receiver, direct and boxed.
        var sm = new SBox<Metal>(Metal.Gold);
        var sg = new SBox<Gem>(Gem.Ruby);
        Console.WriteLine("sbox direct=" + sm.Box() + " is Metal=" + (sm.Box() is Metal));
        IBoxer boxed = sg;
        Console.WriteLine("sbox itf=" + boxed.Box() + " is Gem=" + (boxed.Box() is Gem));

        // Satellites allocated inside shared bodies carry their real
        // instantiation's type identity.
        var dm = new Dictionary<Metal, string> { [Metal.Gold] = "au", [Metal.Lead] = "pb" };
        var dg = new Dictionary<Gem, string> { [Gem.Ruby] = "rb" };
        var um = new DictUser<Metal>();
        var ug = new DictUser<Gem>();
        Console.WriteLine("keys count=" + um.KeysOf(dm).Count + "," + ug.KeysOf(dg).Count);
        Console.WriteLine("keys type=" + (um.KeysOf(dm).GetType() == typeof(Dictionary<Metal, string>.KeyCollection)));
        Console.WriteLine("values type=" + (um.ValuesOf(dm).GetType() == typeof(Dictionary<Metal, string>.ValueCollection)));
        Console.WriteLine("keys cross=" + ((object)um.KeysOf(dm).GetType() == (object)ug.KeysOf(dg).GetType()));
        foreach (var s in um.Drain(dm))
            Console.WriteLine("drain " + s);
        foreach (var s in ug.Drain(dg))
            Console.WriteLine("drain " + s);
    }
}
