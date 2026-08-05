namespace Dn2Cpp;

/// <summary>The transpiler's <c>DN2CPP_*</c> environment variables: one name
/// constant per knob, and the two-and-a-half parsers the read sites share.
///
/// <para><b>The standing rule</b>: an environment variable must never change the
/// output of a successful transpile. A cap (<see cref="MaxGenericDepth"/>,
/// <see cref="MaxInstantiations"/>, <see cref="MaxHeapMb"/>) may only turn a run
/// into an abort or back; a dump/diagnostic (<see cref="Time"/>,
/// <see cref="SharedDump"/>, …) may only report. Anything that changes the C++ a
/// successful transpile emits must be a CLI flag, because the self-host fixpoint
/// requires that nothing in the environment can perturb the bytes. The three
/// edges of the rule are documented at their constants: <see cref="SpecDrain"/>
/// (an order probe whose sanctioned end state is byte-identity — a diff between
/// drains is an emitter bug, and a gate holds it), <see cref="SplitBytes"/>
/// (moves chunk boundaries; read only by the full CLI, never by the self-hosted
/// one, so the fixpoint carries no dependency on it), and <see cref="GodotApi"/>
/// (locates an <i>input</i>, like a CLI path argument).</para>
///
/// <para>The parsers read the environment at call time and cache nothing —
/// whether a knob is read once (a static initializer) or per call is the read
/// site's decision, and this class must not quietly change it.</para>
///
/// <para>Self-host constraint: dn2cpp transpiles its own source, so this class
/// may only use BCL surface the transpiler already supports —
/// <see cref="Environment.GetEnvironmentVariable"/> and
/// <see cref="int.TryParse(string, out int)"/>.</para></summary>
internal static class EnvKnobs
{
    // --- diagnostics / dumps (report only; never perturb output) ---

    /// <summary>=1 arms the shared-generics backstop in Release builds (DEBUG arms it
    /// unconditionally); see <c>CppEmitter.SharedAssertEnabled</c>.</summary>
    internal const string SharedAssert = "DN2CPP_SHARED_ASSERT";

    /// <summary>=1 arms the intercept-registry self-check in Release builds (DEBUG arms
    /// it unconditionally); see <c>CoreIntrinsics.VerifyInterceptRegistry</c>. Like every
    /// cap, it can only turn a run into an abort or back — it never perturbs the output
    /// of a transpile that succeeds.</summary>
    internal const string InterceptSelfCheck = "DN2CPP_INTERCEPT_SELFCHECK";

    /// <summary>=1 dumps per-body shared-generics diagnostics: the unsupported-shape
    /// lines from the canonical trial loop and the per-body taint list after the
    /// summary histogram.</summary>
    internal const string SharedDump = "DN2CPP_SHARED_DUMP";

    /// <summary>=1 prints the full exception (stack included) behind a
    /// NotSupportedException CLI failure, ahead of the one-line error.</summary>
    internal const string DebugTrace = "DN2CPP_DEBUG_TRACE";

    /// <summary>Enables per-phase timing/heap instrumentation; see <see cref="Timing"/>.</summary>
    internal const string Time = "DN2CPP_TIME";

    /// <summary>Each <see cref="Timing.Mark"/> forces a collection and reports live
    /// bytes instead of total (at the price of the durations); see <see cref="Timing"/>.</summary>
    internal const string TimeLive = "DN2CPP_TIME_LIVE";

    /// <summary>Enables the model-population census report; see
    /// <see cref="Dn2Cpp.ModelCensus"/>.</summary>
    internal const string ModelCensus = "DN2CPP_MODEL_CENSUS";

    /// <summary>Enables the emission-side census report — what EMISSION holds, as
    /// opposed to what the model does; see <see cref="Dn2Cpp.EmitCensus"/>.</summary>
    internal const string EmitCensus = "DN2CPP_EMIT_CENSUS";

    /// <summary>Makes an un-asked-for member read throw instead of quietly decoding;
    /// see <c>ClassInfo.StrictCompletion</c>.</summary>
    internal const string StrictCompletion = "DN2CPP_STRICT_COMPLETION";

    // --- caps (may turn a run into an abort or back; never perturb output) ---

    /// <summary>Type-argument nesting ceiling (default 32); see
    /// <c>Compilation.MaxInstantiationDepth</c>.</summary>
    internal const string MaxGenericDepth = "DN2CPP_MAX_GENERIC_DEPTH";

    /// <summary>Ceiling on total closed generic type + method instantiations
    /// (default 1,000,000); see <c>Compilation.MaxInstantiations</c>.</summary>
    internal const string MaxInstantiations = "DN2CPP_MAX_INSTANTIATIONS";

    /// <summary>Opt-in managed-heap ceiling in MB (unset/0 = off); the CLI's
    /// <c>--max-heap-mb</c> overrides it. See <see cref="MemoryGuard"/>.</summary>
    internal const string MaxHeapMb = "DN2CPP_MAX_HEAP_MB";

    /// <summary>Ceiling on the number of member types / attribute-argument allocations that
    /// degrade because the precise <c>ti_arr_&lt;element&gt;</c> handle was never declared —
    /// default <b>0</b>, i.e. one is a failure. See
    /// <c>CppEmitter.AssertArrayTypeInfoDegradesWithinCap</c>, which measures the zero and
    /// says why neither degrade is benign. Within the cap the tally is printed rather than
    /// thrown, so the number is never invisible either way.</summary>
    internal const string MaxArrayTypeInfoDegrades = "DN2CPP_MAX_ARRAY_TI_DEGRADES";

    // --- the three documented edges (see the class doc) ---

    /// <summary>=lifo drains the specialization queue LIFO instead of FIFO — an order
    /// probe: both drains reach the same set, and a byte diff between them is an
    /// emitter order-dependence bug; see <c>Compilation.DrainLifo</c>.</summary>
    internal const string SpecDrain = "DN2CPP_SPEC_DRAIN";

    /// <summary>Method-body chunk size for the split C++ output (0 disables
    /// splitting). Read by the full CLI only — the self-hosted console CLI keeps the
    /// default and adds no environment dependency.</summary>
    internal const string SplitBytes = "DN2CPP_SPLIT_BYTES";

    /// <summary>Path to the <c>extension_api.json</c> driving the Godot call map —
    /// an input locator, like a CLI path argument. The documented FALLBACK for
    /// callers that cannot pass a flag: the <c>--godot-api</c> flag wins over it,
    /// and <c>extension_api.json</c> in the working directory is the default when
    /// neither is set (resolution in <c>GodotBackend</c>).</summary>
    internal const string GodotApi = "DN2CPP_GODOT_API";

    // --- parsers (read at call time; no caching here) ---

    /// <summary>The raw value, or null when unset. For the knobs whose value is a
    /// mode string (<see cref="SpecDrain"/>) rather than a bool or an int.</summary>
    internal static string? Raw(string name) =>
        Environment.GetEnvironmentVariable(name);

    /// <summary>Strict opt-in: true only when the variable is exactly <c>"1"</c> —
    /// unset, empty, <c>"true"</c>, anything else reads false.</summary>
    internal static bool BoolIsOne(string name) =>
        Environment.GetEnvironmentVariable(name) == "1";

    /// <summary>Loose opt-in: true when the variable is set to any non-empty value
    /// other than <c>"0"</c> — so <c>"1"</c>, <c>"true"</c>, <c>"yes"</c> all enable,
    /// and unset, empty, or <c>"0"</c> disable.</summary>
    internal static bool BoolNonZero(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } v && v != "0";

    /// <summary>A strictly positive integer, or <paramref name="fallback"/> when the
    /// variable is unset, empty, unparseable, zero, or negative.</summary>
    internal static int PositiveInt(string name, int fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } v
        && int.TryParse(v, out int n) && n > 0
            ? n : fallback;

    /// <summary>Any integer (zero and negative included — <see cref="SplitBytes"/>
    /// uses 0 to disable splitting), or null when unset, empty, or unparseable.</summary>
    internal static int? Int(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } v
        && int.TryParse(v, out int n)
            ? n : null;

    /// <summary>A non-empty string value, or <paramref name="fallback"/> when the
    /// variable is unset or empty.</summary>
    internal static string StringOr(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : fallback;
}
