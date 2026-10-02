using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

internal static class Program
{
    private static void A() { }
    private static void B() { }
    private static void C() { }

    private static void NullSpan(int count)
    {
        try
        {
            Delegate result = Delegate.Combine(MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.NullRef<Delegate>(), count));
            Console.WriteLine("null span " + count + "=" + (result is null));
        }
        catch (Exception ex)
        {
            Console.WriteLine("null span " + count + "=" + ex.GetType().Name + ":" + ex.Message);
        }
    }

    private static void ConcurrentClone()
    {
        bool all = true;
        for (int round = 0; round < 32; round++)
        {
            Action a = A, b = B, c = C;
            Action chain = a + b + c;
            bool[] correct = new bool[2];
            Thread[] workers = new Thread[2];
            for (int index = 0; index < workers.Length; index++)
            {
                int slot = index;
                workers[index] = new Thread(() =>
                {
                    bool same = true;
                    for (int iteration = 0; iteration < 64; iteration++)
                    {
                        Action copy = (Action)chain.Clone();
                        Delegate[] entries = copy.GetInvocationList();
                        same &= entries.Length == 3 && ReferenceEquals(entries[0], a)
                            && ReferenceEquals(entries[1], b) && ReferenceEquals(entries[2], c);
                        var cursor = Delegate.EnumerateInvocationList(copy);
                        same &= cursor.MoveNext() && ReferenceEquals(cursor.Current, a);
                        same &= cursor.MoveNext() && ReferenceEquals(cursor.Current, b);
                        same &= cursor.MoveNext() && ReferenceEquals(cursor.Current, c);
                        same &= !cursor.MoveNext();
                    }
                    correct[slot] = same;
                });
                workers[index].Start();
            }
            var source = Delegate.EnumerateInvocationList(chain);
            while (source.MoveNext()) { }
            foreach (Thread worker in workers)
                worker.Join();
            all &= correct[0] && correct[1];
        }
        Console.WriteLine("parallel clone cache=" + all);
    }

    private static object EqualsArgument(Delegate value)
    {
        Console.WriteLine("equals argument evaluated");
        return value;
    }

    private static void NullReceiver()
    {
        Delegate none = null;
        Action a = A;
        try
        {
            Console.WriteLine(none.GetHashCode());
        }
        catch (Exception ex)
        {
            Console.WriteLine("null hash=" + ex.GetType().Name + ":" + ex.Message);
        }
        try
        {
            Console.WriteLine(none.Equals(EqualsArgument(a)));
        }
        catch (Exception ex)
        {
            Console.WriteLine("null equals=" + ex.GetType().Name + ":" + ex.Message);
        }
        Console.WriteLine("static equals=" + object.Equals(none, a) + "/" + object.Equals(none, null));
    }

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        DelegateInvocationListSubset.Program.RunCacheGc();
        ConcurrentClone();
        NullSpan(0);
        NullSpan(1);
        NullSpan(2);
        NullReceiver();
        Console.WriteLine("delegate cache fixture end");
    }
}
