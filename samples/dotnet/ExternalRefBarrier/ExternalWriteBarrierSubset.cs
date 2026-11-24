using System;
using System.Threading.Tasks;

namespace ExternalWriteBarrierSubset
{
    // ParallelOptions lives outside CoreLib. Without its declaring assembly the
    // stores must recognize the runtime-mapped reference, including inside ExPair.
    internal static class Program
    {
        private const int HolderCount = 256;
        private const int PathCount = 5;
        private const int CycleCount = 4;
        // Drive collection by allocation so the gate can check actual cycles in GC stats.
        private const int AllocationBudget = 30_000;

        private struct ExPair
        {
            internal ParallelOptions Value;
        }

        private sealed class Holder
        {
            internal ParallelOptions ExField;
            internal ExPair Pair;
            internal readonly ExPair[] PairArr = new ExPair[1];
            internal readonly ParallelOptions[] Fill = new ParallelOptions[1];
            internal readonly ParallelOptions[,] Grid = new ParallelOptions[1, 1];
        }

        private static ParallelOptions MakePayload(int id)
        {
            return new ParallelOptions { MaxDegreeOfParallelism = id };
        }

        private static void Store(Holder holder, int path, ParallelOptions value)
        {
            switch (path)
            {
                case 0:
                    holder.ExField = value;
                    break;
                case 1:
                    holder.Pair = new ExPair { Value = value };
                    break;
                case 2:
                    holder.PairArr[0] = new ExPair { Value = value };
                    break;
                case 3:
                    Array.Fill(holder.Fill, value);
                    break;
                default:
                    holder.Grid[0, 0] = value;
                    break;
            }
        }

        private static ParallelOptions Load(Holder holder, int path)
        {
            return path switch
            {
                0 => holder.ExField,
                1 => holder.Pair.Value,
                2 => holder.PairArr[0].Value,
                3 => holder.Fill[0],
                _ => holder.Grid[0, 0],
            };
        }

        private static int Validate(Holder[] holders, int[] expected)
        {
            int checksum = 17;
            for (int i = 0; i < HolderCount; i++)
            {
                for (int path = 0; path < PathCount; path++)
                {
                    int id = expected[i * PathCount + path];
                    if (id == 0)
                        continue;
                    ParallelOptions payload = Load(holders[i], path);
                    if (payload is null || payload.GetType() != typeof(ParallelOptions)
                        || payload.MaxDegreeOfParallelism != id)
                        throw new InvalidOperationException("External-typed reference store lost its payload");
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

            var expected = new int[HolderCount * PathCount];
            var pressure = new byte[64][];
            // The same rooted holders survive every mutation/pressure round.
            // Held incremental mark windows are tested by the native barrier gate.
            for (int cycle = 0; cycle < CycleCount; cycle++)
            {
                for (int i = 0; i < HolderCount; i++)
                {
                    for (int path = 0; path < PathCount; path++)
                    {
                        int id = cycle * HolderCount * PathCount + i * PathCount + path + 1;
                        Store(holders[i], path, MakePayload(id));
                        expected[i * PathCount + path] = id;
                        pressure[id & (pressure.Length - 1)] = new byte[4096];
                    }
                }

                for (int allocations = 0; allocations < AllocationBudget; allocations++)
                    pressure[allocations & (pressure.Length - 1)] = new byte[4096];
                _ = Validate(holders, expected);
            }

            GC.KeepAlive(holders);
            GC.KeepAlive(pressure);
            Console.WriteLine("external reference stores: True");
        }
    }
}
