using System;
using System.Threading;

namespace IncrementalWriteBarrierSubset
{
    internal static class Program
    {
        private const int HolderCount = 384;
        private const int PathCount = 10;
        private const int CollectionCount = 4;
        private const int AllocationLimit = 100_000;

        private sealed class Payload
        {
            internal readonly int Id;
            internal readonly byte[] Body;

            internal Payload(int id)
            {
                Id = id;
                Body = new byte[256];
                Body[0] = (byte)id;
                Body[Body.Length - 1] = (byte)(id >> 8);
            }
        }

        private struct RefPair
        {
            internal Payload Value;
        }

        private sealed class Holder
        {
            internal Payload Field;
            internal readonly Payload[] Array = new Payload[1];
            internal readonly Payload[] ByRef = new Payload[1];
            internal RefPair Pair;
            internal RefPair Nested;
            internal readonly Payload[] Bulk = new Payload[1];
            internal Payload Atomic;
            internal Payload Volatile;
            internal readonly Payload[] Fill = new Payload[1];
            internal readonly RefPair[] PairBulk = new RefPair[1];
        }

        private sealed class ResizeHolder
        {
            internal Payload[] Values = new Payload[1];
        }

        private static void StoreByRef(ref Payload slot, Payload value)
        {
            slot = value;
        }

        private static void Resize(ref Payload[] array, int length)
        {
            Array.Resize(ref array, length);
        }

        private static void Store(Holder holder, int path, Payload value)
        {
            switch (path)
            {
                case 0:
                    holder.Field = value;
                    break;
                case 1:
                    holder.Array[0] = value;
                    break;
                case 2:
                    StoreByRef(ref holder.ByRef[0], value);
                    break;
                case 3:
                    holder.Pair = new RefPair { Value = value };
                    break;
                case 4:
                    holder.Nested.Value = value;
                    break;
                case 5:
                    Array.Copy(new Payload[] { value }, holder.Bulk, 1);
                    break;
                case 6:
                    Interlocked.Exchange(ref holder.Atomic, value);
                    break;
                case 7:
                    Volatile.Write(ref holder.Volatile, value);
                    break;
                case 8:
                    Array.Fill(holder.Fill, value);
                    break;
                default:
                    // The source must be statically RefPair[]: an Array-typed one
                    // fails the same-element proof and takes the dynamic helper.
                    var pairs = new RefPair[] { new RefPair { Value = value } };
                    Array.Copy(pairs, holder.PairBulk, 1);
                    break;
            }
        }

        private static Payload Load(Holder holder, int path)
        {
            return path switch
            {
                0 => holder.Field,
                1 => holder.Array[0],
                2 => holder.ByRef[0],
                3 => holder.Pair.Value,
                4 => holder.Nested.Value,
                5 => holder.Bulk[0],
                6 => holder.Atomic,
                7 => holder.Volatile,
                8 => holder.Fill[0],
                _ => holder.PairBulk[0].Value,
            };
        }

        private static void ExerciseResizeBarrier(byte[][] pressure)
        {
            var holders = new ResizeHolder[HolderCount];
            for (int i = 0; i < holders.Length; i++)
                holders[i] = new ResizeHolder();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            var arrayWeaks = new WeakReference<Payload[]>[HolderCount];
            var payloadWeaks = new WeakReference<Payload>[HolderCount];
            int completed = GC.CollectionCount(0);
            for (int cycle = 0; cycle < CollectionCount; cycle++)
            {
                int target = completed + 1;
                for (int i = 0; i < HolderCount; i++)
                {
                    ResizeHolder holder = holders[i];
                    Resize(ref holder.Values, holder.Values.Length + 1);
                    var payload = new Payload(cycle * HolderCount + i + 1);
                    holder.Values[holder.Values.Length - 1] = payload;
                    arrayWeaks[i] = new WeakReference<Payload[]>(holder.Values);
                    payloadWeaks[i] = new WeakReference<Payload>(payload);
                    pressure[i & (pressure.Length - 1)] = new byte[4096];
                }

                int allocations = 0;
                while (GC.CollectionCount(0) < target && allocations < AllocationLimit)
                {
                    pressure[allocations & (pressure.Length - 1)] = new byte[4096];
                    allocations++;
                }
                if (GC.CollectionCount(0) < target)
                    throw new InvalidOperationException("allocation pressure did not complete a resize GC cycle");
                completed = GC.CollectionCount(0);

                for (int i = 0; i < HolderCount; i++)
                {
                    Payload[] values = holders[i].Values;
                    Payload payload = values[values.Length - 1];
                    if (!arrayWeaks[i].TryGetTarget(out Payload[] weakValues)
                        || !ReferenceEquals(values, weakValues))
                        throw new InvalidOperationException("resized array became weakly unreachable");
                    if (payload is null
                        || !payloadWeaks[i].TryGetTarget(out Payload weakPayload)
                        || !ReferenceEquals(payload, weakPayload))
                        throw new InvalidOperationException("resized array lost its payload");
                }
            }

            GC.KeepAlive(holders);
        }

        private static void Mutate(Holder[] holders, int[,] expected,
            WeakReference<Payload>[,] weaks, int holderIndex, int path, int id)
        {
            var payload = new Payload(id);
            Store(holders[holderIndex], path, payload);
            expected[holderIndex, path] = id;
            weaks[holderIndex, path] = new WeakReference<Payload>(payload);
        }

        private static int Validate(Holder[] holders, int[,] expected,
            WeakReference<Payload>[,] weaks)
        {
            int checksum = 17;
            for (int i = 0; i < HolderCount; i++)
            {
                for (int path = 0; path < PathCount; path++)
                {
                    int id = expected[i, path];
                    if (id == 0)
                        continue;
                    Payload payload = Load(holders[i], path);
                    if (payload is null || payload.Id != id
                        || payload.Body[0] != (byte)id
                        || payload.Body[payload.Body.Length - 1] != (byte)(id >> 8))
                        throw new InvalidOperationException("managed reference store lost its payload");
                    if (weaks[i, path] is null
                        || !weaks[i, path].TryGetTarget(out Payload weakTarget)
                        || !ReferenceEquals(payload, weakTarget))
                        throw new InvalidOperationException("strongly held payload became weakly unreachable");
                    checksum = unchecked(checksum * 31 + id);
                }
            }
            return checksum;
        }

        internal static void __GateEntry()
        {
            var holders = new Holder[HolderCount];
            for (int i = 0; i < holders.Length; i++)
                holders[i] = new Holder();

            // The destinations must predate the incremental cycles. Mutations below
            // then race the collector's scan of these already-reachable objects.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            var expected = new int[HolderCount, PathCount];
            var weaks = new WeakReference<Payload>[HolderCount, PathCount];
            var pressure = new byte[64][];
            int completed = GC.CollectionCount(0);
            for (int cycle = 0; cycle < CollectionCount; cycle++)
            {
                int target = completed + 1;
                for (int i = 0; i < HolderCount; i++)
                {
                    for (int path = 0; path < PathCount; path++)
                    {
                        int id = cycle * HolderCount * PathCount + i * PathCount + path + 1;
                        Mutate(holders, expected, weaks, i, path, id);
                        pressure[id & (pressure.Length - 1)] = new byte[4096];
                    }
                }

                int allocations = 0;
                while (GC.CollectionCount(0) < target && allocations < AllocationLimit)
                {
                    pressure[allocations & (pressure.Length - 1)] = new byte[4096];
                    allocations++;
                }
                if (GC.CollectionCount(0) < target)
                    throw new InvalidOperationException("allocation pressure did not complete a GC cycle");
                completed = GC.CollectionCount(0);
                _ = Validate(holders, expected, weaks);
            }

            GC.KeepAlive(holders);
            GC.KeepAlive(pressure);
            Console.WriteLine("incremental write barriers: True");
        }

        internal static void __ResizeGateEntry()
        {
            var pressure = new byte[64][];
            ExerciseResizeBarrier(pressure);
            GC.KeepAlive(pressure);
            Console.WriteLine("Array.Resize field stress: True");
        }
    }
}
