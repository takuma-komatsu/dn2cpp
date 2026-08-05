using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Dn2Cpp;

/// <summary>Counts the model — what <see cref="Timing"/>'s heap number is made of — split by
/// whether the program reaches an object at all.
///
/// <para>Two paired instruments. <b>Allocation counters</b> (always on, an increment at each
/// model-construction site) count what was MADE, garbage included, so an interning lever's
/// ceiling is visible before it is built. The <b>live census</b>
/// (<c>DN2CPP_MODEL_CENSUS=1</c>, walked at a named point) counts what is HELD, and for
/// TypeDesc how much of that is duplicate: occurrences vs distinct objects vs distinct
/// structural keys, the last gap being exactly what interning would collect.</para>
///
/// <para>Both are output-neutral: the counters are write-only and the census walks the model
/// without touching it. The census allocates two hash sets over the whole graph, so read peak
/// RSS from a run that has it off.</para></summary>
internal static class ModelCensus
{
    private static readonly bool Enabled = EnvKnobs.BoolNonZero(EnvKnobs.ModelCensus);

    // --- allocation counters, bumped at the construction sites ---
    internal static int ClassesPass1;
    internal static int ClassesSpec;
    internal static int MethodsPass2;
    internal static int MethodsSpec;
    internal static int MethodsGenericInst;
    internal static int FieldsPass2;
    internal static int FieldsSpec;

    /// <summary>Method signatures actually decoded (<c>Compilation.DecodeSignature</c>), out
    /// of the MethodInfo the model holds per method row of every loaded assembly. It is what
    /// deferring the decode bought, and — per phase through <see cref="Timing.Populations"/> —
    /// where the residue comes from: override matching and the emitted reflection rows.</summary>
    internal static int SignaturesDecoded;

    /// <summary>Field types actually decoded (<c>Compilation.DecodeFieldType</c>). The field
    /// ROWS are the type's shape and are always held; only a type that is laid out, reflected
    /// over or touched by a body has to answer what its rows are TYPED at.</summary>
    internal static int FieldTypesDecoded;

    /// <summary>TypeDesc objects constructed, by <see cref="TypeKind"/>. Counts construction,
    /// not interning hits, so an interned kind stays at its table size while an un-interned
    /// one reports its true churn.</summary>
    internal static readonly int[] TypeDescMade = new int[16];

    internal static void TypeDescBorn(TypeKind kind) => TypeDescMade[(int)kind]++;

    private static readonly string[] KindNames =
    {
        "prim", "class", "ext", "extgen", "szarray", "byref", "genvar", "template", "mdarray", "ptr",
    };

    /// <summary>Structural identity of a descriptor — what an interned model would collapse
    /// it to. Derived from the descriptor's content, never its object identity.</summary>
    private static string Key(TypeDesc t) => t.Kind switch
    {
        TypeKind.Primitive => (t.IsCanonPlaceholder ? "$" : "") + "P" + (int)t.Primitive,
        TypeKind.Class => "C" + t.Class!.CppName,
        TypeKind.External => "E" + t.ExternalName,
        TypeKind.ExternalGeneric => "X" + t.ExternalName,
        TypeKind.SZArray => "A[" + Key(t.Element!) + "]",
        TypeKind.MDArray => "M" + t.Rank + "[" + Key(t.Element!) + "]",
        TypeKind.ByRef => "R[" + Key(t.Element!) + "]",
        TypeKind.Pointer => "*[" + Key(t.Element!) + "]",
        TypeKind.GenericVar => (t.GenVarIsMethod ? "!!" : "!") + t.GenVarIndex,
        TypeKind.Template => "T" + t.TemplateModule!.Index + ":"
                             + MetadataTokens.GetRowNumber(t.TemplateHandle),
        _ => "?",
    };

    private sealed class Walk
    {
        internal readonly int[] Occurrences = new int[16];
        internal readonly HashSet<TypeDesc> Objects = new();
        internal readonly HashSet<string> Keys = new();
        private readonly int[] _perKindObjects = new int[16];
        private readonly HashSet<string> _perKindKeys = new();

        internal void Visit(TypeDesc? t)
        {
            if (t is null)
                return;
            Occurrences[(int)t.Kind]++;
            if (!Objects.Add(t))
                return;   // already walked this object — its children too
            _perKindObjects[(int)t.Kind]++;
            Keys.Add(Key(t));
            Visit(t.Element);
            if (t.GenericArgs is { } ga)
                foreach (var a in ga)
                    Visit(a);
        }

        internal int ObjectsOf(TypeKind k) => _perKindObjects[(int)k];
    }

    /// <summary>Walks the held model and prints the census to stderr. <paramref name="when"/>
    /// names the point (the phase whose heap number this explains).</summary>
    internal static void Report(string when, Compilation c)
    {
        if (!Enabled)
            return;

        var w = new Walk();
        int specs = 0, deadSpecs = 0, liveSpecs = 0, shellSpecs = 0;
        int methodsOnDead = 0, fieldsOnDead = 0, methodsHeld = 0, fieldsHeld = 0;
        int emptyClassLists = 0, emptyClassDicts = 0, emptySharedUsers = 0;
        // Report-scoped, not static: Report runs at @build and again at @emit, and the delta
        // between them is how many signatures emission itself forced.
        int sigsDecoded = 0, fieldTypesDecoded = 0;

        foreach (var cls in c.Classes)
        {
            bool isSpec = cls.Context.TypeArgs.Length > 0;
            if (isSpec)
                specs++;
            foreach (var a in cls.Context.TypeArgs)
                w.Visit(a);

            // Ask before reading: touching .Methods would decode them, and a specialization
            // whose members were never pulled is precisely what this census counts — the
            // measurement would consume its own subject.
            bool ready = cls.MembersReady;
            bool anyReachable = false;
            if (ready)
                foreach (var m in cls.Methods)
                {
                    methodsHeld++;
                    if (c.Reachable.Contains(m))
                        anyReachable = true;
                    // Ask before reading, for the members guard's reason plus one: a signature
                    // decode instantiates the generics the signature names, and those are
                    // appended to c.Classes — the list this loop is walking.
                    if (m.SignatureReady)
                    {
                        sigsDecoded++;
                        w.Visit(m.Signature.ReturnType);
                        foreach (var p in m.Signature.ParameterTypes)
                            w.Visit(p);
                    }
                    foreach (var a in m.Context.TypeArgs)
                        w.Visit(a);
                    foreach (var a in m.Context.MethodArgs)
                        w.Visit(a);
                    if (m.SharedUsers.Count == 0)
                        emptySharedUsers++;
                }
            foreach (var f in cls.Fields)
            {
                fieldsHeld++;
                // The signature guard's two reasons, one tier down. The field ROWS stay safe
                // to read — they are the type's shape; only what they are typed at is on
                // demand.
                if (f.TypeReady)
                {
                    fieldTypesDecoded++;
                    w.Visit(f.Type);
                }
            }

            if (isSpec)
            {
                if (!ready)
                    shellSpecs++;           // members never decoded
                else if (anyReachable)
                    liveSpecs++;
                else
                {
                    // Decoded but unreached: pulled because emitted code NAMES the type — a
                    // field/parameter type needs a layout and a vtable even when no method on
                    // it is called. Irreducible residue; a jump here means a missed guard.
                    deadSpecs++;
                    methodsOnDead += cls.Methods.Count;
                    fieldsOnDead += cls.Fields.Count;
                }
            }

            if (cls.Interfaces.Count == 0) emptyClassLists++;
            if (cls.ExternalInterfaceNames.Count == 0) emptyClassLists++;
            if (cls.Fields.Count == 0) emptyClassLists++;
            if (cls.SharedUsers.Count == 0) emptyClassLists++;
            if (!ready || cls.Methods.Count == 0) emptyClassLists++;
            if (!ready || cls.Vtable.Count == 0) emptyClassLists++;
            if (!ready || cls.SlotOwners.Count == 0) emptyClassLists++;
            if (!ready || cls.MethodByTemplate.Count == 0) emptyClassDicts++;
            if (!ready || cls.ExplicitInterfaceImpls.Count == 0) emptyClassDicts++;
        }

        int tdMade = 0, tdOcc = 0;
        for (int i = 0; i < KindNames.Length; i++)
        {
            tdMade += TypeDescMade[i];
            tdOcc += w.Occurrences[i];
        }

        var e = Console.Error;
        e.WriteLine($"dn2cpp-census: @{when}");
        e.WriteLine($"  classes {c.Classes.Count} = pass1 {ClassesPass1} + spec {ClassesSpec}"
                    + $"   [specs held {specs}: live {liveSpecs} / dead {deadSpecs}"
                    + $" / SHELL {shellSpecs}]");
        e.WriteLine($"  methods held {methodsHeld} = pass2 {MethodsPass2} + spec {MethodsSpec}"
                    + $" + geninst {MethodsGenericInst}   [on DEAD specs: {methodsOnDead}]"
                    + $"   reached {c.Reachable.Count}");
        e.WriteLine($"  signatures DECODED {sigsDecoded} of {methodsHeld} held"
                    + $" ({(methodsHeld == 0 ? 0 : 100L * sigsDecoded / methodsHeld)}%)"
                    + $"   [reached {c.Reachable.Count}; the rest is override matching,"
                    + " interface slots and emitted reflection rows]");
        e.WriteLine($"  fields  held {fieldsHeld} = pass2 {FieldsPass2} + spec {FieldsSpec}"
                    + $"   [on DEAD specs: {fieldsOnDead}]");
        e.WriteLine($"  field types DECODED {fieldTypesDecoded} of {fieldsHeld} held"
                    + $" ({(fieldsHeld == 0 ? 0 : 100L * fieldTypesDecoded / fieldsHeld)}%)"
                    + "   [the rest is typed at nothing the program lays out, reflects over"
                    + " or touches]");
        e.WriteLine($"  typedesc made {tdMade}  held-occurrences {tdOcc}"
                    + $"  distinct-objects {w.Objects.Count}  distinct-keys {w.Keys.Count}"
                    + $"   -> interning would drop {w.Objects.Count - w.Keys.Count} objects");
        for (int i = 0; i < KindNames.Length; i++)
            if (TypeDescMade[i] > 0 || w.Occurrences[i] > 0)
                e.WriteLine($"    {KindNames[i],-9} made {TypeDescMade[i],9}"
                            + $"  occurrences {w.Occurrences[i],9}  objects {w.ObjectsOf((TypeKind)i),9}");
        e.WriteLine($"  empty collections: class-lists {emptyClassLists} (of {c.Classes.Count * 7})"
                    + $"  class-dicts {emptyClassDicts} (of {c.Classes.Count * 2})"
                    + $"  method-sharedusers {emptySharedUsers} (of {methodsHeld})");
    }
}
