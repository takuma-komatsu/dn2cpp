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

    internal sealed class Consumer<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal int Forward() => Provider<T>.Bump();

        internal string Read() => typeof(T).Name;
    }

    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine("rgctx cold forwarding=" + new Consumer<Warm>().Forward());
            // Cold shares the class table but never reaches the forwarding body.
            Console.WriteLine("rgctx cold identity=" + new Consumer<Cold>().Read());
        }
    }
}
