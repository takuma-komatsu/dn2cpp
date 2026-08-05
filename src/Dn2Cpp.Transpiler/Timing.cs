namespace Dn2Cpp;

/// <summary>Env-gated phase instrumentation: with <c>DN2CPP_TIME=1</c> every
/// <see cref="Mark"/> prints one stderr line naming the phase just finished,
/// its duration (since the previous mark), the total since the first mark, the
/// current managed heap size, and — once a <see cref="Populations"/> reporter is
/// installed — the model-population counters behind that heap number. Unset (or
/// <c>0</c>) it is a no-op — each call costs one cached-bool check and prints
/// nothing.
/// <para>Self-host constraint: dn2cpp transpiles its own source, so this class
/// may only use BCL surface the transpiler already supports —
/// <see cref="Environment.TickCount64"/> for time (not Stopwatch or DateTime)
/// and <see cref="GC.GetTotalMemory(bool)"/> for heap (not
/// GC.GetTotalAllocatedBytes).</para>
/// <para>The stderr sink is also why this must not be relied on from a
/// self-hosted native binary — <c>Console.Error</c> is unusable there.
/// Profile the managed CLI.</para></summary>
internal static class Timing
{
    private static readonly bool Enabled = EnvKnobs.BoolNonZero(EnvKnobs.Time);

    /// <summary>With <c>DN2CPP_TIME_LIVE=1</c> each mark forces a collection and
    /// reports only live bytes. The default (<c>GetTotalMemory(false)</c>) counts
    /// uncollected garbage too, so a change that removes churn also removes
    /// collections and the heap number can *rise* on an improvement. The blocking
    /// GCs make this mode's durations meaningless — never read time and live heap
    /// from the same run.</summary>
    private static readonly bool LiveHeap = EnvKnobs.BoolNonZero(EnvKnobs.TimeLive);

    private static long _firstMs = -1;
    private static long _lastMs;

    /// <summary>Optional model-population reporter, installed by
    /// <see cref="Compilation"/>. A heap number alone cannot say *what* grew; this
    /// appends the counts that explain it — loaded classes, reached methods,
    /// generic type/method instantiations, deepest type-argument nesting — so a
    /// phase whose heap runs away names its own cause. Only invoked when
    /// instrumentation is on.</summary>
    internal static Func<string>? Populations;

    /// <summary>Reports a phase boundary: the named phase is the work since the
    /// previous Mark (the first call anchors the clock and reports 0 ms).</summary>
    internal static void Mark(string phase)
    {
        if (!Enabled)
            return;
        long now = Environment.TickCount64;
        if (_firstMs < 0)
        {
            _firstMs = now;
            _lastMs = now;
        }
        long heapMb = GC.GetTotalMemory(LiveHeap) / (1024 * 1024);
        string pop = Populations is null ? "" : " " + Populations();
        Console.Error.WriteLine(
            $"dn2cpp-time: {phase} {now - _lastMs} ms total {now - _firstMs} ms heap {heapMb} MB{pop}");
        _lastMs = now;
    }
}
