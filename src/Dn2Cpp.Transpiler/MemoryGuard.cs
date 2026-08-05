namespace Dn2Cpp;

/// <summary>An opt-in ceiling on the transpiler's managed heap: past it, the
/// transpile dies with an actionable error instead of taking the machine into swap.
///
/// <para>This is the operator's lever, not the structural guarantee. Unbounded
/// growth has one structural cause — monomorphization with no fixpoint — and
/// <see cref="Compilation.Instantiate"/> bounds that unconditionally. What is left
/// is growth that is finite but larger than this machine can afford, and only the
/// operator knows what that is, so it is off unless asked for
/// (<c>DN2CPP_MAX_HEAP_MB</c>, or the CLI's <c>--max-heap-mb</c>).</para>
///
/// <para><see cref="GC.GetTotalMemory(bool)"/> is the managed heap, not RSS: RSS is
/// that plus the GC's free space and any native mapping, so a budget set at roughly
/// two thirds of the RSS you can afford is about right. <c>DOTNET_GCHeapHardLimit</c>
/// is the belt to this brace — it caps commit at the runtime level and throws
/// OutOfMemoryException — but it cannot say which phase was at fault, and this can.</para>
///
/// <para>Self-host constraint: dn2cpp transpiles its own source, so this class may
/// only use BCL surface the transpiler already supports — the <see cref="EnvKnobs"/>
/// parsers (<see cref="Environment.GetEnvironmentVariable"/>,
/// <see cref="int.TryParse(string, out int)"/>) and
/// <see cref="GC.GetTotalMemory(bool)"/>.</para></summary>
internal static class MemoryGuard
{
    /// <summary>The budget in bytes; 0 disables the guard. Seeded from
    /// <c>DN2CPP_MAX_HEAP_MB</c> and overridable by the CLI's <c>--max-heap-mb</c>.</summary>
    internal static long LimitBytes =
        (long)EnvKnobs.PositiveInt(EnvKnobs.MaxHeapMb, 0) * 1024 * 1024;

    /// <summary>Sample rate. GC.GetTotalMemory is a real call and the check sites sit in
    /// loops that run once per loaded type and once per compiled method; growth that
    /// matters happens over millions of allocations, so one reading per 1024 calls still
    /// localizes the overrun to within a megabyte of heap.</summary>
    private const int SampleMask = 0x3FF;

    private static int _tick;

    /// <summary>Sampled check, for the loops that run once per type or once per
    /// compiled method.</summary>
    internal static void Check(string phase)
    {
        if (LimitBytes <= 0)
            return;
        if ((++_tick & SampleMask) != 0)
            return;
        CheckNow(phase);
    }

    /// <summary>Unsampled check, for a site that runs few enough times that sampling
    /// would step over it — loading an assembly, say, which happens a couple of hundred
    /// times even under a runaway --auto-ref closure.
    /// <para><paramref name="phase"/> names the work in progress. The whole point of
    /// failing here rather than letting the allocator fail is to be able to say which
    /// phase ran away, so the caller has a lever to pull.</para></summary>
    internal static void CheckNow(string phase)
    {
        if (LimitBytes <= 0)
            return;
        long used = GC.GetTotalMemory(false);
        if (used <= LimitBytes)
            return;
        throw new NotSupportedException(
            $"transpile exceeded its {LimitBytes / (1024 * 1024)} MB heap budget during '{phase}' "
            + $"(managed heap {used / (1024 * 1024)} MB). The usual cause is a reference closure far "
            + "larger than the program needs: drop --auto-ref and pass an explicit -r set, and point "
            + "at a Release rather than a Debug bin directory (a Debug one drags Roslyn / Cecil / "
            + "TestPlatform into the closure). Raise or remove the budget with "
            + "DN2CPP_MAX_HEAP_MB=<n> (0 disables it).");
    }
}
