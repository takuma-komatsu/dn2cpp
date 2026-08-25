#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace StructTaskReferenceBarrierSubset
{
    internal sealed class Payload
    {
        internal static int Finalized;

        internal Payload(string text)
        {
            Text = text;
        }

        internal string Text { get; }

        ~Payload()
        {
            Finalized++;
        }
    }

    internal readonly struct Item
    {
        internal Item(Payload payload)
        {
            Payload = payload;
        }

        internal Payload Payload { get; }
    }

    internal static class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Item[] Join()
        {
            var first = new Payload(new string(new[] { 'b', 'a', 'r', 'r', 'i', 'e', 'r', '-', 'a' }));
            var second = new Payload(new string(new[] { 'b', 'a', 'r', 'r', 'i', 'e', 'r', '-', 'b' }));
            Task<Item> firstTask = Task.FromResult(new Item(first));
            Task<Item> secondTask = Task.FromResult(new Item(second));
            first = null!;
            second = null!;
            return Task.WhenAll(firstTask, secondTask).Result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Churn()
        {
            for (int i = 0; i < 4096; i++)
            {
                byte[] bytes = new byte[128];
                bytes[0] = (byte)i;
            }
        }

        internal static void __GateEntry()
        {
            Payload.Finalized = 0;
            Item[] items = Join();
            Churn();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Console.WriteLine($"whenall struct refs: {Payload.Finalized == 0},"
                + $"{items[0].Payload.Text}|{items[1].Payload.Text}");
            GC.KeepAlive(items);
        }
    }
}
