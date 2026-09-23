using System;
using System.Runtime.CompilerServices;

namespace RgctxForwardingSubset
{
    internal sealed class Warm { }
    internal sealed class Cold { }

    internal static class Provider<T>
    {
        private static int _count;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int Bump() => ++_count;
    }

    internal static class Tally<T>
    {
        internal static int Count;
    }

    // A generic method on a generic class: the forwarded table is per-method,
    // and its canonical counterpart hangs off the declaring class's owner.
    internal static class PairProvider<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int Pair<U>() => ++Tally<U>.Count;
    }

    internal sealed class Consumer<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal int Forward() => Provider<T>.Bump();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal int ForwardPair() => PairProvider<T>.Pair<T>();

        internal string Read() => typeof(T).Name;
    }

    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine("rgctx cold forwarding=" + new Consumer<Warm>().Forward());
            Console.WriteLine("rgctx cold method forwarding=" + new Consumer<Warm>().ForwardPair());
            // Cold shares the class table but never reaches the forwarding bodies.
            Console.WriteLine("rgctx cold identity=" + new Consumer<Cold>().Read());
        }
    }
}
