#nullable enable
using System;
using System.Globalization;
using System.Threading;

// ThreadLocal<T> whose valueFactory returns a VALUE type. A struct returned by value is a
// layout, not a width, so the factory is invoked through a per-T thunk the emitter
// supplies rather than a switch over scalar result kinds. The sections cross the ABI
// boundaries a by-value return has and a scalar does not: register-returned, 16-byte edge,
// hidden-return-pointer, GC-reference-bearing (read back after a forced collection), and
// the narrow enums whose box must be sizeof(T) rather than the low bytes of a wide slot.
namespace ThreadLocalStructFactory;

static class Program
{
    const int N = 8;

    enum ByteEnum : byte { A = 1, B = 200 }
    enum ShortEnum : short { X = -2, Y = 30000 }
    enum LongEnum : long { L = 5_000_000_000L }

    // 8 bytes: register-returned everywhere.
    struct Small
    {
        public int A;
        public int B;
        public Small(int a, int b) { A = a; B = b; }
        public override string ToString() => A + "/" + B;
    }

    // 40 bytes: above the register-return threshold on both SysV-x86-64 and AAPCS64, so
    // the factory returns through a hidden pointer.
    struct Big
    {
        public long A, B, C, D, E;
        public Big(long seed) { A = seed; B = seed + 1; C = seed + 2; D = seed + 3; E = seed + 4; }
        public override string ToString() => A + "," + B + "," + C + "," + D + "," + E;
    }

    // A value type whose layout carries references: the box must stay traced.
    struct RefBearing
    {
        public string Tag;
        public int[] Data;
        public int N;
        public RefBearing(string tag, int[] data, int n) { Tag = tag; Data = data; N = n; }
        public override string ToString() => Tag + ":" + Data.Length + ":" + N;
    }

    static int s_seq;
    static readonly ThreadLocal<TimeSpan> s_tlPerThread =
        new ThreadLocal<TimeSpan>(() => TimeSpan.FromSeconds(Interlocked.Increment(ref s_seq)));

    static long s_perThreadTicks;
    static int s_arrived;

    static void Worker(object? arg)
    {
        _ = arg;
        Interlocked.Increment(ref s_arrived);
        while (Volatile.Read(ref s_arrived) < N)
            Thread.Yield();
        // The factory runs once per thread, so the total is the fixed sum of 1..N seconds
        // however the threads interleave.
        Interlocked.Add(ref s_perThreadTicks, s_tlPerThread.Value.Ticks);
        Interlocked.Add(ref s_perThreadTicks, s_tlPerThread.Value.Ticks); // cached, not re-run
    }

    public static void __GateEntry()
    {
        Console.WriteLine("-- ThreadLocalStructFactory --");

        var tlTs = new ThreadLocal<TimeSpan>(() => TimeSpan.FromSeconds(3));
        Console.WriteLine(tlTs.Value);
        Console.WriteLine(tlTs.IsValueCreated);
        var tlIp = new ThreadLocal<IntPtr>(() => new IntPtr(42));
        Console.WriteLine(tlIp.Value.ToInt64());
        var tlUp = new ThreadLocal<UIntPtr>(() => new UIntPtr(43));
        Console.WriteLine(tlUp.Value.ToUInt64());

        // Register-returned, edge-of-register, and memory-class struct returns.
        Console.WriteLine(new ThreadLocal<Small>(() => new Small(11, 22)).Value);
        Console.WriteLine(new ThreadLocal<Guid>(
            () => new Guid("00112233-4455-6677-8899-aabbccddeeff")).Value);
        Console.WriteLine(new ThreadLocal<decimal>(() => 12.5m).Value.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine(new ThreadLocal<Big>(() => new Big(100)).Value);

        // Read back after a forced collection: a box the collector did not trace would
        // come back with a dead Tag/Data.
        var tlRef = new ThreadLocal<RefBearing>(
            () => new RefBearing("tagged", new int[] { 1, 2, 3, 4 }, 9));
        RefBearing first = tlRef.Value;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        RefBearing again = tlRef.Value;
        Console.WriteLine(again);
        Console.WriteLine(again.Tag + " " + again.Data[3] + " " + ReferenceEquals(first.Data, again.Data));

        // The same shape reached through the BCL's own ValueTuple.
        var tlTup = new ThreadLocal<(string, int)>(() => ("hello", 7));
        (string, int) tv = tlTup.Value;
        Console.WriteLine(tv.Item1 + "/" + tv.Item2);

        // Nullable<T> — a struct with a flag beside the payload.
        var tlNull = new ThreadLocal<int?>(() => 5);
        Console.WriteLine(tlNull.Value.HasValue + " " + tlNull.Value!.Value);
        var tlNullNone = new ThreadLocal<TimeSpan?>(() => null);
        Console.WriteLine(tlNullNone.Value.HasValue);

        // Narrow enums: the box is sizeof(T), not the low bytes of a widened slot.
        Console.WriteLine(new ThreadLocal<ByteEnum>(() => ByteEnum.B).Value);
        Console.WriteLine(new ThreadLocal<ShortEnum>(() => ShortEnum.X).Value);
        Console.WriteLine(new ThreadLocal<ShortEnum>(() => ShortEnum.Y).Value);
        Console.WriteLine(new ThreadLocal<LongEnum>(() => LongEnum.L).Value);

        // A capturing factory (real delegate target) beside the static ones above (null
        // target) — both mouths of the same thunk.
        int captured = 17;
        var tlCap = new ThreadLocal<Small>(() => new Small(captured, captured * 2));
        Console.WriteLine(tlCap.Value);

        // Set/get round-trip over a struct T, and the no-factory default.
        var tlSet = new ThreadLocal<Big>(() => new Big(1));
        Console.WriteLine(tlSet.Value);
        tlSet.Value = new Big(50);
        Console.WriteLine(tlSet.Value);
        var tlDefault = new ThreadLocal<TimeSpan>();
        Console.WriteLine(tlDefault.IsValueCreated);
        Console.WriteLine(tlDefault.Value);
        Console.WriteLine(new ThreadLocal<Guid>().Value);
        Console.WriteLine(new ThreadLocal<RefBearing>().Value.Tag is null);

        // The scalar kinds, so the thunk is proven a replacement rather than an addition.
        Console.WriteLine(new ThreadLocal<int>(() => -7).Value);
        Console.WriteLine(new ThreadLocal<long>(() => -8L).Value);
        Console.WriteLine(new ThreadLocal<float>(() => 1.5f).Value.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine(new ThreadLocal<double>(() => 2.25).Value.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine(new ThreadLocal<bool>(() => true).Value);
        Console.WriteLine(new ThreadLocal<char>(() => 'q').Value);
        Console.WriteLine(new ThreadLocal<string>(() => "ref-T").Value); // the no-thunk arm

        // Per-thread isolation with a struct factory: N threads, one factory run each,
        // two reads each, fixed total.
        var threads = new Thread[N];
        for (int i = 0; i < N; i++)
        {
            threads[i] = new Thread(Worker);
            threads[i].Start(i);
        }
        for (int i = 0; i < N; i++)
            threads[i].Join();
        Console.WriteLine(TimeSpan.FromTicks(Volatile.Read(ref s_perThreadTicks)));
        Console.WriteLine(Volatile.Read(ref s_seq));
    }
}
