#nullable disable
using System;
using System.Reflection;

namespace DelegateInvocationListSubset;

// List entries retain the original delegates across relinks, clones and enumeration.
class Journal
{
    public string Text = "";
    public void A() => Text += "A";
    public void B() => Text += "B";
    public virtual void C() => Text += "C";
}

delegate void Signal();

static class Program
{
    static string log = "";

    static void A() => log += "A";

    static void B() => log += "B";

    static void C() => log += "C";

    sealed class Counter
    {
        public int Count;

        public void Bump() => Count++;
    }

    static void Show(string label, Delegate d)
    {
        Delegate[] list = d.GetInvocationList();
        string line = label + ": " + list.Length + " " + list.GetType().Name;
        foreach (Delegate entry in list)
        {
            log = "";
            ((Action)entry)();
            line += " " + log + ":" + entry.Method.Name + ":" + entry.GetInvocationList().Length;
        }
        Console.WriteLine(line);
    }

    internal static void Run()
    {
        Console.WriteLine("== invocation lists ==");
        Action a = A, b = B, c = C;
        Show("unicast", a);
        Console.WriteLine("unicast answers itself: " + ReferenceEquals(a.GetInvocationList()[0], a));
        Show("combine", a + b);
        Show("combine three", a + b + c);
        Show("combine nested", a + (b + c));
        Show("remove head", a + b + c - a);
        Show("remove middle", a + b + c - b);
        Show("remove run", a + b + c + a + b - (a + b));
        Show("remove to one", a + b - a);
        Action abc = a + b + c;
        Delegate[] first = abc.GetInvocationList();
        Delegate[] second = abc.GetInvocationList();
        Console.WriteLine("fresh arrays: " + ReferenceEquals(first, second) + " equal entries: "
            + first[0].Equals(a) + "/" + first[1].Equals(b) + "/" + first[2].Equals(c)
            + " first is its own: " + ReferenceEquals(first[0], a));
        first[0] = c;
        Show("after writing the array", abc);
        var counter = new Counter();
        MethodInfo bump = typeof(Counter).GetMethod("Bump");
        Action bound = (Action)Delegate.CreateDelegate(typeof(Action), counter, bump);
        Delegate[] mixed = (a + bound + bound).GetInvocationList();
        foreach (Delegate entry in mixed)
            ((Action)entry)();
        Console.WriteLine("reflection-bound entries: " + mixed.Length + " " + mixed[1].Method.Name
            + " target=" + ReferenceEquals(mixed[2].Target, counter) + " count=" + counter.Count
            + " equal=" + mixed[1].Equals(bound));
        Func<int> f = () => 1;
        f += () => 2;
        int sum = 0;
        foreach (Func<int> g in f.GetInvocationList())
            sum += g();
        Console.WriteLine("func results summed: " + sum);
        Action none = null;
        try
        {
            none.GetInvocationList();
            Console.WriteLine("null receiver: no exception");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("null receiver: NullReferenceException");
        }
        Console.WriteLine("invocation lists end");
    }

    // The list's length and, per entry, whether it is the expected delegate itself.
    static string Same(Delegate d, params Delegate[] entries)
    {
        Delegate[] list = d.GetInvocationList();
        string line = list.Length.ToString();
        for (int i = 0; i < list.Length; i++)
            line += " " + (i < entries.Length && ReferenceEquals(list[i], entries[i]));
        return line;
    }

    // The entries are the delegates Combine took, whichever chain or overload they
    // came through, and a Remove leaving one entry answers that delegate.
    internal static void RunEntryIdentity()
    {
        Console.WriteLine("== invocation list entries ==");
        Action a = A, b = B, c = C;
        Action ab = a + b;
        Action abc = ab + c;
        Console.WriteLine("combine: " + Same(ab, a, b));
        Console.WriteLine("combine three: " + Same(abc, a, b, c));
        Console.WriteLine("combine nested: " + Same(a + (b + c), a, b, c));
        Console.WriteLine("combine multicasts: " + Same(ab + ab, a, b, a, b));
        Console.WriteLine("combine array: " + Same(Delegate.Combine(a, b, c), a, b, c));
        Console.WriteLine("remove head: " + Same(abc - a, b, c));
        Console.WriteLine("remove middle: " + Same(abc - b, a, c));
        Console.WriteLine("remove tail: " + Same(abc - c, a, b));
        Console.WriteLine("remove run: " + Same(abc + a + b - ab, a, b, c));
        Console.WriteLine("remove to one: " + ReferenceEquals(ab - a, b) + "/" + ReferenceEquals(ab - b, a)
            + "/" + ReferenceEquals(abc - (b + c), a) + "/" + ReferenceEquals(abc - ab, c));
        Action other = () => { };
        Console.WriteLine("remove nothing: " + ReferenceEquals(abc - other, abc) + "/" + (abc - abc == null));
        Console.WriteLine("remove all: " + Same(Delegate.RemoveAll(abc + a + b, a), b, c, b) + "/"
            + ReferenceEquals(Delegate.RemoveAll(abc, other), abc) + "/" + (Delegate.RemoveAll(a + a, a) == null));
        object copy = abc.Clone();
        object single = a.Clone();
        Console.WriteLine("clone: " + Same((Delegate)copy, a, b, c) + " unicast "
            + ReferenceEquals(single, a) + "/" + Same((Delegate)single, (Delegate)single));
        var counter = new Counter();
        Action bound = (Action)Delegate.CreateDelegate(typeof(Action), counter, typeof(Counter).GetMethod("Bump"));
        Console.WriteLine("reflection-bound entry: " + Same(a + bound, a, bound));
        Console.WriteLine("invocation list entries end");
    }

    // How many entries EnumerateInvocationList yields and, per entry, whether it is the
    // expected delegate itself.
    static string Enumerated<T>(T d, params Delegate[] entries) where T : Delegate
    {
        string line = "";
        int count = 0;
        foreach (T entry in Delegate.EnumerateInvocationList(d))
        {
            line += " " + (count < entries.Length && ReferenceEquals(entry, entries[count]));
            count++;
        }
        return count + line;
    }

    // A body shared over its type argument enumerates as a closed one does.
    static int EnumeratedCount<T>(T d) where T : Delegate
    {
        int count = 0;
        foreach (T entry in Delegate.EnumerateInvocationList(d))
            count++;
        return count;
    }

    // Delegate.EnumerateInvocationList yields the entries GetInvocationList answers, in
    // invocation order, through Combine, nesting and Remove; a unicast delegate yields
    // itself and null yields nothing. A copy of the enumerator advances on its own, and
    // one past the end keeps answering false with a null Current.
    internal static void RunEnumeration()
    {
        Console.WriteLine("== invocation list enumeration ==");
        Action a = A, b = B, c = C;
        Action abc = a + b + c;
        Console.WriteLine("unicast: " + Enumerated(a, a));
        Console.WriteLine("combine three: " + Enumerated(abc, a, b, c));
        Console.WriteLine("combine nested: " + Enumerated(a + (b + c), a, b, c));
        Console.WriteLine("remove middle: " + Enumerated(abc - b, a, c));
        Console.WriteLine("remove to one: " + Enumerated(abc - (b + c), a));
        Console.WriteLine("null: " + Enumerated<Action>(null));
        Console.WriteLine("as GetInvocationList: " + Enumerated(abc, abc.GetInvocationList()));
        log = "";
        foreach (Action entry in Delegate.EnumerateInvocationList(abc + a))
            entry();
        Console.WriteLine("runs in order: " + log);
        var first = Delegate.EnumerateInvocationList(abc);
        first.MoveNext();
        var copy = first;
        first.MoveNext();
        copy.MoveNext();
        Console.WriteLine("copies advance apart: " + ReferenceEquals(first.Current, b) + "/" + ReferenceEquals(copy.Current, b));
        first.MoveNext();
        Console.WriteLine("past the end: " + first.MoveNext() + "/" + first.MoveNext() + " current null " + (first.Current is null));
        var counter = new Counter();
        Action bound = (Action)Delegate.CreateDelegate(typeof(Action), counter, typeof(Counter).GetMethod("Bump"));
        foreach (Action entry in Delegate.EnumerateInvocationList(a + bound + bound))
            entry();
        Console.WriteLine("reflection-bound entries: " + Enumerated(a + bound, a, bound) + " count=" + counter.Count);
        Func<int> f = () => 1;
        f += () => 2;
        int sum = 0;
        foreach (Func<int> g in Delegate.EnumerateInvocationList(f))
            sum += g();
        Console.WriteLine("func results summed: " + sum);
        Console.WriteLine("shared bodies: " + EnumeratedCount(abc) + " " + EnumeratedCount(f) + " " + EnumeratedCount<Action>(null));
        Console.WriteLine("invocation list enumeration end");
    }

    internal static void RunEnumerationScale()
    {
        Console.WriteLine("== invocation list enumeration scale ==");
        Action a = A, b = B, c = C;
        Action chain = a + b + c + a + b;
        Delegate[] list = chain.GetInvocationList();
        var left = Delegate.EnumerateInvocationList(chain);
        var right = Delegate.EnumerateInvocationList(chain);
        left.MoveNext();
        left.MoveNext();
        right.MoveNext();
        left.MoveNext();
        right.MoveNext();
        Console.WriteLine("interleaved indexes: "
            + ReferenceEquals(left.Current, list[2]) + "/" + ReferenceEquals(right.Current, list[1]));
        int outerCount = 0;
        int innerCount = 0;
        bool nestedOrder = true;
        foreach (Action outer in Delegate.EnumerateInvocationList(chain))
        {
            nestedOrder &= ReferenceEquals(outer, list[outerCount++]);
            int innerIndex = 0;
            foreach (Action inner in Delegate.EnumerateInvocationList(chain))
                nestedOrder &= ReferenceEquals(inner, list[innerIndex++]);
            innerCount += innerIndex;
        }
        Console.WriteLine("nested enumerators: " + outerCount + "/" + innerCount + " entries " + nestedOrder);

        const int size = 512;
        Action[] entries = new Action[size];
        for (int i = 0; i < entries.Length; i++)
            entries[i] = i % 3 == 0 ? a : i % 3 == 1 ? b : c;
        Action many = (Action)Delegate.Combine(entries);
        int count = 0;
        bool sameEntries = true;
        foreach (Action entry in Delegate.EnumerateInvocationList(many))
        {
            sameEntries &= count < entries.Length && ReferenceEquals(entry, entries[count]);
            count++;
        }
        int secondCount = 0;
        foreach (Action entry in Delegate.EnumerateInvocationList(many))
        {
            sameEntries &= secondCount < entries.Length && ReferenceEquals(entry, entries[secondCount]);
            secondCount++;
        }
        Console.WriteLine("long chain: " + count + "/" + secondCount + " entries " + sameEntries);
        Console.WriteLine("invocation list enumeration scale end");
    }

    private static void Try(string label, Func<object> invoke)
    {
        string text;
        try
        {
            text = invoke()?.ToString() ?? "null";
        }
        catch (Exception ex)
        {
            text = ex.GetType().Name;
        }
        Console.WriteLine(label + ": " + text);
    }

    private static string Replay(Journal journal, Action chain)
    {
        journal.Text = "";
        chain?.Invoke();
        return journal.Text;
    }

    internal static void RunRemoveRuns()
    {
        Console.WriteLine("== remove runs ==");
        var journal = new Journal();
        Action a = journal.A, b = journal.B, c = journal.C;
        Try("remove head run", () => Replay(journal, (a + b + c) - (a + b)));
        Try("remove last repeated entry", () => Replay(journal, (a + b + a + c) - a));
        Try("remove middle entry", () => Replay(journal, (a + b + c) - b));
        Try("remove from single", () => Replay(journal, a - b));
        Try("remove longer list", () =>
        {
            Action ab = a + b;
            return Replay(journal, ab - (a + b + c)) + "/" + ReferenceEquals(ab - (a + b + c), ab);
        });
        Try("remove absent run", () =>
        {
            Action abc = a + b + c;
            return ReferenceEquals(abc - (c + a), abc);
        });
        Action chain = null;
        for (int i = 0; i < 40; i++)
            chain += i % 3 == 0 ? a : i % 3 == 1 ? b : c;
        Try("long list, last run", () => Replay(journal, chain - (a + b + c)));
        Try("long list, entries left", () => Replay(journal, chain - (a + b + c)).Length);
        Try("long list, source entries", () => Replay(journal, chain).Length);
        Signal signal = journal.A;
        Try("remove across delegate types", () => Delegate.Remove(a, signal) is null);
        Try("combine across delegate types", () => Delegate.Combine(a, signal) is null);
        Try("remove null across delegate types", () => Delegate.Remove(null, signal) is null);
        Console.WriteLine("remove runs end");
    }

    internal static void RunCacheGc()
    {
        Console.WriteLine("== invocation list cache GC ==");
        Action a = A, b = B, c = C;
        Action chain = a + b + c;
        var first = Delegate.EnumerateInvocationList(chain);
        first.MoveNext();
        GC.Collect();
        bool same = ReferenceEquals(first.Current, a);
        first.MoveNext();
        same &= ReferenceEquals(first.Current, b);
        Action copy = (Action)chain.Clone();
        GC.Collect();
        same &= EnumeratedCount(copy) == 3;
        same &= ReferenceEquals(copy.GetInvocationList()[2], c);
        Console.WriteLine("cache survives GC=" + same);
        Console.WriteLine("span combine=" + Same(Delegate.Combine(new ReadOnlySpan<Delegate>(new Delegate[] { a, null, b, c })), a, b, c));
        Console.WriteLine("empty span null=" + (Delegate.Combine(ReadOnlySpan<Delegate>.Empty) is null));
        Console.WriteLine("invocation list cache GC end");
    }
}
