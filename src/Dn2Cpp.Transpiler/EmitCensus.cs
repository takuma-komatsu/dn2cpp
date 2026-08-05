namespace Dn2Cpp;

/// <summary>Counts what EMISSION holds — the sibling of <see cref="ModelCensus"/>, which
/// counts what the MODEL holds. The split decides which lever applies: a model term is cut
/// by tree-shaking, an emission term by streaming.
///
/// <para>Counts are UTF-16 chars, because that is what a <c>StringBuilder</c> holds; the
/// emitted FILE is UTF-8 and roughly half the size. Each row prints held KB (chars×2)
/// beside the char count so the two are never confused.</para>
///
/// <para>Output-neutral, and it must stay that way: every method only walks tables and
/// writes to stderr. The walk itself allocates, so read peak RSS from a run with it
/// OFF.</para>
///
/// <para>dn2cpp transpiles its own source, so keep this class on BCL surface the transpiler
/// already supports, and print integers only — a formatted double would make the report
/// host-locale dependent.</para></summary>
internal static class EmitCensus
{
    /// <summary>Whether the census is on. Visible to callers because the tables it counts
    /// are theirs, so they must skip the summing walk when it is off.</summary>
    internal static readonly bool Enabled = EnvKnobs.BoolNonZero(EnvKnobs.EmitCensus);

    private static void Head(string when)
    {
        Console.Error.WriteLine($"dn2cpp-emit-census: @{when}");
    }

    /// <summary>One table row: entries held and chars of text. <paramref name="note"/> says
    /// what becomes of the table — a table standing at the peak and one released long before
    /// it call for different levers.</summary>
    private static void Row(string name, long entries, long chars, string note)
    {
        Console.Error.WriteLine($"  {name,-22} entries {entries,8}   chars {chars,10}"
                                + $"   held {chars * 2 / 1024,8} KB   {note}");
    }

    /// <summary>The type-metadata emitter's own tables. Reported from inside
    /// <c>TypeMetadataEmitter.Emit</c> because the emitter is unreferenced the instant
    /// <c>CppEmitter.EmitTypeInfos</c> returns — a census taken later would report these
    /// pools as free when the question was how big they got. They are big while the
    /// class-major loop runs but collectible before the next phase mark, so a lever aimed
    /// here cannot move the peak.</summary>
    internal static void ReportMetadata(
        int parmPools, long parmChars, int itfPools, long itfChars,
        int genargPools, long genargChars, int itfsPools, long itfsChars,
        int memberAddr, long memberAddrChars, int memberTabs, long memberTabChars,
        int itfTabSyms, long itfTabSymChars, int slotStubs, long slotStubChars,
        int invokerThunks, long invokerThunkChars)
    {
        Head("type-metadata emission (released on return from EmitTypeInfos)");
        Row("parmpool_", parmPools, parmChars, "interned parameter tables");
        Row("itfpool_", itfPools, itfChars, "interned interface slot tables");
        Row("itfspool_", itfsPools, itfsChars, "interned interface-entry tables");
        Row("genargpool_", genargPools, genargChars, "interned generic-arg vectors");
        Row("memberAddr", memberAddr, memberAddrChars, "per-class: a property's accessor row");
        Row("field/meth/ctor/prop", memberTabs, memberTabChars, "per-class: table symbol + count");
        Row("itfTabSyms", itfTabSyms, itfTabSymChars, "per-class: interface-table symbol");
        Row("slotmiss_/invmiss_", slotStubs, slotStubChars, "per-chunk: named trap stubs");
        Row("invoker thunks", invokerThunks, invokerThunkChars, "per-chunk: dedup keys");
    }

    /// <summary>What emission still holds when <c>CppEmitter.Emit</c> returns: the two
    /// builders the driver has yet to write, and the pools that fed them. This is the set
    /// that stands at the peak. The chunk streams are absent by construction — each
    /// body/metadata TU is written and dropped as the next one opens.</summary>
    internal static void ReportEmission(
        long headerChars, long dataChars, int literals, long literalChars,
        int blobs, long blobBytes, long enumToStringChars,
        int namedBodySyms, int definedBodySyms, long definedBodySymChars,
        int definedTypeInfoSyms, long definedTypeInfoSymChars,
        int bodyChunks, int metadataChunks)
    {
        Head("emission complete (what the driver still has to write)");
        Row("generated.h", 1, headerChars, "StringBuilder; ToString()d at write-files");
        Row("generated.cpp", 1, dataChars, "StringBuilder; ToString()d at write-files");
        Row("literal pool", literals, literalChars, "ldstr interning");
        Row("enum ToString bodies", 1, enumToStringChars, "appended to generated.cpp");
        Row("definedBodySymbols", definedBodySyms, definedBodySymChars,
            "cut-implies-route backstop, defined half");
        Row("definedTypeInfoSyms", definedTypeInfoSyms, definedTypeInfoSymChars,
            "cut-implies-route backstop, ti_ half");
        Console.Error.WriteLine($"  blobs {blobs} ({blobBytes} bytes of RVA data)"
                                + $"   namedBodySymbols {namedBodySyms} entries"
                                + $"   chunks already streamed: {bodyChunks} body + {metadataChunks} metadata");
    }
}
