using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

// Admission from reflection summaries. A reached method's summary is resolved through the
// method's own instantiation, and each live site selects the members its recognized
// operands can name: lookups and enumerations select metadata only, an invocation, a
// construction or a delegate binding reaches the selected bodies. A site whose operand is
// not known selects only what is known and records a diagnostic that names a remedy.
internal sealed partial class Compilation
{
    private enum REntityKind : byte
    {
        Type, Definition, Method, Field, Property, Event, Assembly, Parameter, Str, Int, Null,
        Delegate, Object, TypeArray, Attribute,
        /// <summary>A clone of generic definition <see cref="REntity.Definition"/> that its
        /// runtime-instantiation template serves, over <see cref="REntity.Tuple"/> when the
        /// arguments are known.</summary>
        Template,
        /// <summary>The members <see cref="REntity.Lookup"/> selects on a template's clone,
        /// resolved on the template when planning builds it.</summary>
        TemplateMember,
        /// <summary>A member of an open definition: metadata can be read, but no body can run.</summary>
        DefinitionMember,
    }

    /// <summary>One resolved alternative of a summary value.</summary>
    private sealed class REntity
    {
        public REntityKind Kind;
        public TypeDesc? Type;
        public ClassInfo? Owner;
        public MethodInfo? Method;
        public FieldInfo? Field;
        public EntityHandle Member;
        public Module? Assembly;
        public TypeDefinitionHandle Definition;
        public string? Text;
        public long Number;
        public RResolved? Targets;
        public RResolved[]? Elements;
        public TypeDesc[]? Tuple;
        public TemplateLookup? Lookup;
        public REntity? Metadata;
        /// <summary>A closed base whose members a template's clone inherits: an owner for
        /// lookups, never the constructed type.</summary>
        public bool Inherited;
        /// <summary>A property or event a lookup found on a base of the type it reflects
        /// over: .NET associates none of its private accessors with it.</summary>
        public bool InheritedMember;
        /// <summary>A template member's accessor read without nonPublic: it selects only
        /// public accessors.</summary>
        public bool PublicAccessors;
    }

    /// <summary>A member selection on a template's clone, under ordinary lookup or named
    /// delegate-binding rules.</summary>
    private sealed record TemplateLookup(LookupMemberKind? Kind, string? Name, int Flags, RResolved? Types, int Arity,
        bool AllowPrefix = false, RResolved? Returns = null, bool MostDerived = false, bool NamedBinding = false);

    /// <summary>What a site asks of a template's clone: running the members a lookup
    /// selects, or constructing it with one of its constructors (<paramref name="Arguments"/>
    /// negative for any arity). Named delegate bindings bound the lookup by their
    /// Invoke arity. <paramref name="Tuple"/> is null when the type
    /// arguments are not known.</summary>
    private readonly record struct TemplateSelection(TemplateLookup Lookup, ReflectionAccessor Accessor, bool Construct,
        int Arguments, TypeDesc[]? Tuple, bool PublicAccessors = false);

    /// <summary>The closed argument tuples MakeGenericType sites ask a definition for while
    /// its template serves them; null stands for arguments not known ahead of time.</summary>
    private sealed class TemplateRequest
    {
        public required Module Module;
        public required TypeDefinitionHandle Definition;
        public readonly List<TypeDesc[]?> Tuples = new();

        public void Add(TypeDesc[]? tuple)
        {
            foreach (var t in Tuples)
                if (t is null ? tuple is null : tuple is not null && TypeArgsComparer.Instance.Equals(t, tuple))
                    return;
            Tuples.Add(tuple);
        }
    }

    /// <summary>A resolved value: its known alternatives and, when it may also be something
    /// the summary or the context cannot name, why. Alternatives are distinct by
    /// <see cref="REntityIdentity"/> and kept in insertion order, so a chain of lookups
    /// grows only with the distinct members it names; a value past
    /// <see cref="MaxItems"/> keeps its first alternatives and widens to Unknown.</summary>
    private sealed class RResolved
    {
        public const int MaxItems = 1 << 14;
        public const string TooBroad = "a value with more alternatives than admission follows";
        private const int IndexedFrom = 16;

        public readonly List<REntity> Items = new();
        public string? Unknown;
        /// <summary>The member names and preserve-descriptor kind an unknown-owner lookup
        /// selects by exact name, so a remedy names that member instead of preserving the
        /// whole owner; null when any unknown alternative leaves the member open.</summary>
        public List<string>? UnknownMembers;
        public string? UnknownMemberKind;
        public static readonly RResolved Empty = new();
        private HashSet<REntity>? _index;

        public void Add(REntity e)
        {
            if (_index is null && Items.Count >= IndexedFrom)
                _index = new HashSet<REntity>(Items, REntityIdentity.Instance);
            if (_index is not null ? _index.Contains(e) : ContainsLinear(e))
                return;
            if (Items.Count >= MaxItems)
            {
                Unknown ??= TooBroad;
                UnknownMembers = null;
                UnknownMemberKind = null;
                return;
            }
            _index?.Add(e);
            Items.Add(e);
        }

        private bool ContainsLinear(REntity e)
        {
            foreach (var existing in Items)
                if (REntityIdentity.Instance.Equals(existing, e))
                    return true;
            return false;
        }

        public void AddRange(List<REntity> entities)
        {
            foreach (var e in entities)
                Add(e);
        }

        public void Merge(RResolved other)
        {
            AddRange(other.Items);
            if (other.Unknown is null)
                return;
            if (Unknown is null)
            {
                Unknown = other.Unknown;
                UnknownMembers = other.UnknownMembers;
                UnknownMemberKind = other.UnknownMemberKind;
            }
            else if (UnknownMemberKind != other.UnknownMemberKind || !SameNames(UnknownMembers, other.UnknownMembers))
            {
                UnknownMembers = null;
                UnknownMemberKind = null;
            }
        }

        private static bool SameNames(List<string>? a, List<string>? b)
        {
            if (a is null || b is null || a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }
    }

    /// <summary>Whether two resolved alternatives name the same thing: one admits and
    /// reports exactly what the other does. Types compare structurally where their
    /// descriptors are not interned; nested values compare by reference. Membership only:
    /// no hash reaches the output.</summary>
    private sealed class REntityIdentity : IEqualityComparer<REntity>
    {
        public static readonly REntityIdentity Instance = new();

        public bool Equals(REntity? a, REntity? b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a is null || b is null)
                return false;
            return a.Kind == b.Kind && a.Number == b.Number && a.Inherited == b.Inherited
                && a.InheritedMember == b.InheritedMember && a.PublicAccessors == b.PublicAccessors
                && a.Definition == b.Definition && a.Member == b.Member
                && ReferenceEquals(a.Owner, b.Owner) && ReferenceEquals(a.Method, b.Method)
                && ReferenceEquals(a.Field, b.Field) && ReferenceEquals(a.Assembly, b.Assembly)
                && (a.Metadata is null ? b.Metadata is null : b.Metadata is not null && Equals(a.Metadata, b.Metadata))
                && ReferenceEquals(a.Targets, b.Targets) && ReferenceEquals(a.Elements, b.Elements)
                && string.Equals(a.Text, b.Text, StringComparison.Ordinal)
                && SameType(a.Type, b.Type) && SameTuple(a.Tuple, b.Tuple) && a.Lookup == b.Lookup;
        }

        public int GetHashCode(REntity e)
        {
            unchecked
            {
                int h = (int)e.Kind * 31 + (int)e.Number;
                h = h * 31 + TypeHash(e.Type);
                h = h * 31 + (e.Method?.GetHashCode() ?? 0);
                h = h * 31 + (e.Field?.GetHashCode() ?? 0);
                h = h * 31 + e.Member.GetHashCode();
                return h * 31 + e.Definition.GetHashCode();
            }
        }

        private static bool SameType(TypeDesc? a, TypeDesc? b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a is null || b is null || a.Kind != b.Kind)
                return false;
            return a.Kind switch
            {
                TypeKind.Class => ReferenceEquals(a.Class, b.Class),
                TypeKind.SZArray or TypeKind.ByRef => SameType(a.Element, b.Element),
                TypeKind.MDArray => a.Rank == b.Rank && SameType(a.Element, b.Element),
                TypeKind.External => a.ExternalHandle == b.ExternalHandle && ReferenceEquals(a.ExternalModule, b.ExternalModule)
                    && string.Equals(a.ExternalName, b.ExternalName, StringComparison.Ordinal)
                    && string.Equals(a.ExternalAssembly, b.ExternalAssembly, StringComparison.Ordinal),
                TypeKind.GenericVar => a.GenVarIndex == b.GenVarIndex && a.GenVarIsMethod == b.GenVarIsMethod,
                TypeKind.Template => a.TemplateHandle == b.TemplateHandle && ReferenceEquals(a.TemplateModule, b.TemplateModule),
                _ => false,
            };
        }

        private static bool SameTuple(TypeDesc[]? a, TypeDesc[]? b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a is null || b is null || a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
                if (!SameType(a[i], b[i]))
                    return false;
            return true;
        }

        private static int TypeHash(TypeDesc? t)
        {
            int h = 0;
            for (int depth = 0; t is not null && depth < 4; depth++, t = t.Element)
                h = unchecked(h * 31 + (int)t.Kind * 7 + (t.Class?.GetHashCode() ?? 0));
            return h;
        }
    }

    /// <summary>One application of a summary in one instantiation context.</summary>
    private sealed class ReflectionApplication
    {
        /// <summary>The alternatives one application's terms may hold together; every term
        /// resolved past it is Unknown.</summary>
        public const int MaxResolved = 1 << 17;

        public required MethodInfo Method;
        public required ReflectionSummary Summary;
        public required bool Canonical;
        /// <summary>The alternatives the resolved terms hold together.</summary>
        public int Resolved;
        public readonly Dictionary<int, RResolved> Terms = new();
        public readonly HashSet<int> Visiting = new();
        /// <summary>The site being applied; a selection that grows with the program's
        /// instantiations re-applies it.</summary>
        public int Site = -1;
        /// <summary>The terms being resolved, innermost last.</summary>
        public readonly List<int> Resolving = new();
        /// <summary>Terms whose value lists a definition's instantiations as they exist
        /// now, with those definitions: each site that reads such a term, through the memo
        /// too, is re-applied when one of them gains an instantiation.</summary>
        public readonly Dictionary<int, List<(int Module, int Definition)>> Growing = new();
    }

    internal enum ReflectionDiagnosticKind : byte { Execution, ClosedGeneric, AttributeInstances, Metadata }

    /// <summary>A reflection site whose target admission could not select.</summary>
    internal sealed class ReflectionDiagnostic
    {
        public ReflectionDiagnosticKind Kind;
        public string Assembly = "";
        public string Method = "";
        public int Offset;
        public string Api = "";
        public string Reason = "";
        public string Fix = "";
        public bool User;
        public bool BackendCovered;
        public string Chain = "";
        /// <summary>What each instantiation context that reported the site asked for; the
        /// row merges them, so it does not depend on which context reached the site first.</summary>
        public List<ReflectionDiagnosticVariant>? Variants;
    }

    internal sealed record ReflectionDiagnosticVariant(ReflectionDiagnosticKind Kind, string Reason, string? Assembly,
        string? Owner, List<string>? Names, string? MemberKind);

    private readonly Dictionary<(int Module, int Token, int Offset), ReflectionDiagnostic> _reflectionDiagnosticIndex = new();
    private readonly List<ReflectionDiagnostic> _reflectionDiagnostics = new();

    /// <summary>MakeGenericType sites whose request only a runtime-instantiation template of
    /// the named definition can serve. Which templates survive is known only after planning,
    /// so each reports as ClosedGeneric unless its definition got an eligible template or
    /// every tuple the site asked for gained a compiled instantiation. A site only a template
    /// level body holds (<c>Body</c>) stands only while planning retains that body.</summary>
    private readonly List<((int, int, int) Key, string Definition, ReflectionDiagnostic Diagnostic, TemplateRequest Request,
        MethodInfo? Body)> _templateDiagnostics = new();

    /// <summary>Sites of template level bodies, applied only in the template's placeholder
    /// context: no exact context resolves or reports them, and a clone runs the body as it
    /// is. Each stands while planning retains its body for the clones
    /// (<see cref="RuntimeTemplateBodies"/>), merged with the row exact contexts report.</summary>
    private readonly Dictionary<(int, int, int), (MethodInfo Body, ReflectionDiagnostic Diagnostic)>
        _templateBodyDiagnostics = new();

    /// <summary>The diagnostics in report order: ordinal by assembly, method display and
    /// IL offset, so the order does not depend on which context reached a site first.</summary>
    internal List<ReflectionDiagnostic> ReflectionDiagnostics
    {
        get
        {
            var sorted = new List<ReflectionDiagnostic>(_reflectionDiagnostics);
            var bodySites = AddTemplateBodyDiagnostics(sorted);
            // MakeGenericType clones any level row of an eligible template, keyed by the
            // level's definition, so a definition with no template of its own is served too.
            var served = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (defName, _, levels) in EligibleRuntimeTemplates)
            {
                served.Add(defName);
                foreach (var level in levels)
                    served.Add(BacktickFullName(level.Module, level.Handle));
            }
            // A site whose definitions several templates would serve reports once, with
            // every definition's reason.
            var standing = new Dictionary<(int, int, int), List<ReflectionDiagnostic>>();
            var standingOrder = new List<(int, int, int)>();
            foreach (var (key, definition, diagnostic, request, body) in _templateDiagnostics)
                if (!served.Contains(definition) && TemplateStillRequested(request)
                    && (body is null || RuntimeTemplateBodies.Contains(body))
                    && !_reflectionDiagnosticIndex.ContainsKey(key) && !bodySites.Contains(key))
                {
                    if (!standing.TryGetValue(key, out var forSite))
                    {
                        standing.Add(key, forSite = new List<ReflectionDiagnostic>());
                        standingOrder.Add(key);
                    }
                    forSite.Add(diagnostic);
                }
            foreach (var key in standingOrder)
            {
                var forSite = standing[key];
                if (forSite.Count == 1)
                {
                    sorted.Add(forSite[0]);
                    continue;
                }
                var reasons = new List<string>();
                foreach (var d in forSite)
                    if (!reasons.Contains(d.Reason))
                        reasons.Add(d.Reason);
                reasons.Sort(string.CompareOrdinal);
                var first = forSite[0];
                sorted.Add(new ReflectionDiagnostic
                {
                    Kind = first.Kind, Assembly = first.Assembly, Method = first.Method, Offset = first.Offset,
                    Api = first.Api, Reason = string.Join("; ", reasons), Fix = first.Fix, User = first.User,
                    BackendCovered = first.BackendCovered, Chain = first.Chain,
                });
            }
            // Every printed column takes part, so rows that tie on the site still print in
            // one order.
            sorted.Sort((a, b) =>
            {
                int c = string.CompareOrdinal(a.Assembly, b.Assembly);
                if (c == 0)
                    c = string.CompareOrdinal(a.Method, b.Method);
                if (c == 0)
                    c = a.Offset.CompareTo(b.Offset);
                if (c == 0)
                    c = string.CompareOrdinal(a.Api, b.Api);
                if (c == 0)
                    c = a.Kind.CompareTo(b.Kind);
                if (c == 0)
                    c = string.CompareOrdinal(a.Reason, b.Reason);
                if (c == 0)
                    c = string.CompareOrdinal(a.Fix, b.Fix);
                return c;
            });
            return sorted;
        }
    }

    /// <summary>Adds to <paramref name="rows"/> each site of a template level body planning
    /// retained, merged with the row exact contexts report for it; returns those sites.</summary>
    private HashSet<(int, int, int)> AddTemplateBodyDiagnostics(List<ReflectionDiagnostic> rows)
    {
        var added = new HashSet<(int, int, int)>();
        foreach (var entry in _templateBodyDiagnostics)
        {
            var (body, diagnostic) = entry.Value;
            if (!RuntimeTemplateBodies.Contains(body))
                continue;
            added.Add(entry.Key);
            if (!_reflectionDiagnosticIndex.TryGetValue(entry.Key, out var exact))
            {
                rows.Add(diagnostic);
                continue;
            }
            var row = new ReflectionDiagnostic
            {
                Kind = exact.Kind, Assembly = exact.Assembly, Method = exact.Method, Offset = exact.Offset,
                Api = exact.Api, Reason = exact.Reason, Fix = exact.Fix, User = exact.User,
                BackendCovered = exact.BackendCovered, Chain = exact.Chain,
                Variants = new List<ReflectionDiagnosticVariant>(exact.Variants!),
            };
            foreach (var variant in diagnostic.Variants!)
                MergeDiagnosticVariant(row, variant);
            rows[rows.IndexOf(exact)] = row;
        }
        return added;
    }

    /// <summary>Whether <paramref name="m"/> belongs to a runtime-instantiation template
    /// level, whose clones run its body as the placeholder context resolves it.</summary>
    private bool IsTemplateLevelBody(MethodInfo m) =>
        _runtimeTemplateLevels.Count > 0 && _runtimeTemplateLevels.Contains(m.DeclaringClass);

    /// <summary>Classes a recognized lookup, enumeration, attribute-data read or type
    /// identity names, in first-named order: the metadata a trimmed reflection image keeps.</summary>
    private readonly List<ClassInfo> _reflectionMetadataOwners = new();
    private readonly HashSet<ClassInfo> _reflectionMetadataOwnersSeen = new();
    internal IReadOnlyList<ClassInfo> ReflectionMetadataOwners => _reflectionMetadataOwners;

    private void NoteReflectionMetadataOwner(ClassInfo cls)
    {
        if (_reflectionMetadataOwnersSeen.Add(cls))
            _reflectionMetadataOwners.Add(cls);
    }

    /// <summary>Methods a recognized lookup, enumeration or MakeGenericMethod lists. Their
    /// rows survive the unreached-row trim, so a listed library member stays visible as .NET
    /// lists it; a listing admits no body.</summary>
    private readonly HashSet<MethodInfo> _reflectionListedRows = new();

    /// <summary>Generic method definitions a lookup or enumeration lists, by owner and
    /// definition: each metadata view of a definition is its own object.</summary>
    private readonly HashSet<(ClassInfo Owner, MethodDefinitionHandle Handle)> _reflectionListedDefinitions = new();

    internal bool IsReflectionListedRow(MethodInfo m) => _reflectionListedRows.Contains(m)
        || m.Context.MethodArgs.Length == 0 && !m.Handle.IsNil && _reflectionListedDefinitions.Count > 0
            && _reflectionListedDefinitions.Contains((m.DeclaringClass, m.Handle));

    /// <summary>Lists the method and constructor rows a lookup or enumeration site reads on
    /// its known owners, however its result is consumed.</summary>
    private void NoteListedSiteRows(ReflectionApplication app, RSite site, RResolved owners)
    {
        var api = site.Api;
        LookupMemberKind? kind = api.Op switch
        {
            ReflectionOp.GetMethod or ReflectionOp.GetMethods => LookupMemberKind.Method,
            ReflectionOp.GetConstructor or ReflectionOp.GetConstructors => LookupMemberKind.Constructor,
            ReflectionOp.GetProperty or ReflectionOp.GetProperties => LookupMemberKind.Property,
            ReflectionOp.GetField or ReflectionOp.GetFields => LookupMemberKind.Field,
            ReflectionOp.GetEvent or ReflectionOp.GetEvents => LookupMemberKind.Event,
            _ => null,
        };
        if (api.Op == ReflectionOp.GetDefaultMembers)
        {
            foreach (var cls in ClassesOf(owners))
                if (DefaultMemberName(cls) is { } defaultName)
                    NoteListedRows(SelectMembers(cls, null, defaultName, ReflectionApis.DefaultLookup, null, -1, allowPrefix: true));
            return;
        }
        if (kind is null && api.Op is not (ReflectionOp.GetMembers or ReflectionOp.GetMember))
            return;
        List<string>? names = null;
        if (api.Name >= 0 && (names = KnownStrings(Operand(app, site, api.Name), out _)) is null)
            return;
        var flagSet = api.Flags >= 0 ? FlagAlternatives(Operand(app, site, api.Flags), api.DefaultFlags, out _)
            : new List<int> { api.DefaultFlags };
        foreach (var cls in ClassesOf(owners))
            foreach (int flags in flagSet)
                if (names is null)
                    NoteListedRows(SelectMembers(cls, kind, null, flags, null, -1));
                else
                    foreach (string name in names)
                        NoteListedRows(SelectMembers(cls, kind, name, flags, null, -1, api.Op == ReflectionOp.GetMember));
    }

    /// <summary>An assembly listing returns each generic definition's handle as typeof(G&lt;&gt;)
    /// would, whether or not a later site reads the element.</summary>
    private void NoteListedGenericDefinitions(RResolved owners)
    {
        foreach (var e in owners.Items)
            if (e.Kind == REntityKind.Assembly && e.Assembly is { } module)
                foreach (var h in module.Reader.TypeDefinitions)
                    if (module.Reader.GetTypeDefinition(h).GetGenericParameters().Count != 0)
                        NoteTypeofOpenGenericDef(BacktickFullName(module, h), ResolveOpenGenericDefKind(module, h),
                            GenericParamNames(module, h));
    }

    /// <summary>Keeps the rows a lookup or enumeration selects. A row's declaring class is
    /// emitted in full: an opaque shell carries no member table, so a row the lookup found
    /// on a type-token-only owner or one of its bases would answer null.</summary>
    private List<REntity> NoteListedRows(List<REntity> members)
    {
        foreach (var e in members)
        {
            if (e.Kind == REntityKind.Method && e.Method is { } method)
            {
                if (GenericParameterCount(method) == 0)
                    _reflectionListedRows.Add(method);
                else if (IsOpenGenericMethod(method))
                    _reflectionListedDefinitions.Add((method.DeclaringClass, method.Handle));
            }
            if (e.Kind is REntityKind.Method or REntityKind.Field or REntityKind.Property or REntityKind.Event
                && e.Owner is { IntrinsicCppName: null } owner && !CoreIntrinsics.IsIntrinsicType(owner.FullName)
                && !ContainsCanonPlaceholder(owner) && !ContainsGenericVar(owner))
                NoteForceEmit(owner);
        }
        return members;
    }

    /// <summary>A site re-applied when its finite template alternative gains a static
    /// instantiation.</summary>
    private readonly record struct PendingSelection(int Module, int Definition, MethodInfo Method, int Site);

    private readonly List<PendingSelection> _pendingReflectionSelections = new();
    private readonly HashSet<(int, int, MethodInfo, int)> _pendingReflectionSelectionsSeen = new();
    private int _pendingSelectionClassCursor;

    /// <summary>Definitions a recognized MakeGenericType receives whose closed type exists
    /// nowhere, by backtick name; each gets a runtime-instantiation template identity,
    /// built in the typeof'd-definition order. Member selections admit bodies separately.</summary>
    private readonly HashSet<string> _reflectionTemplateSelections = new(StringComparer.Ordinal);

    /// <summary>How many selections the summary applications have made; part of the
    /// discovery fixpoint's measure.</summary>
    private int _reflectionSelectionCount;

    /// <summary>Applies the summary of a scanned body in its exact instantiation context.
    /// Only sites live in that context select, so a folded-away arm admits nothing.</summary>
    private void ApplyReflectionSummary(MethodInfo m, ReflectionSummary summary, BranchLiveness? liveness)
    {
        if (summary.Sites.Length == 0)
            return;
        var app = new ReflectionApplication
        {
            Method = m, Summary = summary, Canonical = ContextIsCanonical(m.Context),
        };
        for (int i = 0; i < summary.Sites.Length; i++)
        {
            var site = summary.Sites[i];
            if (liveness is not null && !liveness.LiveAt(site.Offset))
                continue;
            try
            {
                ApplySite(app, i);
            }
            catch (NotSupportedException e) when (!IsMustEscape(e))
            {
                // A member that does not decode is not reflectively reachable either.
            }
        }
    }

    private static bool ContextIsCanonical(GenericContext ctx)
    {
        foreach (var t in ctx.TypeArgs)
            if (ContainsCanonPlaceholder(t) || ContainsGenericVar(t))
                return true;
        foreach (var t in ctx.MethodArgs)
            if (ContainsCanonPlaceholder(t) || ContainsGenericVar(t))
                return true;
        return false;
    }

    private void ApplySite(ReflectionApplication app, int index)
    {
        var site = app.Summary.Sites[index];
        var api = site.Api;
        var m = app.Method;
        app.Site = index;
        NoteReflectionCapability(api, m.Module);
        if (!api.Recognized)
        {
            if (api.Kind is ReflectionOpKind.Execute or ReflectionOpKind.Construct or ReflectionOpKind.Bind)
                RecordReflectionDiagnostic(app, site, ReflectionDiagnosticKind.Execution,
                    "the overload is not one admission recognizes", null, null, null);
            return;
        }
        RResolved Op(sbyte slot) => Operand(app, site, slot);
        switch (api.Kind)
        {
            case ReflectionOpKind.TypeIdentity:
            case ReflectionOpKind.Lookup:
            case ReflectionOpKind.Enumerate:
            case ReflectionOpKind.AttributeData:
            {
                var owner = Op(api.Target);
                foreach (var cls in ClassesOf(owner))
                    NoteReflectionMetadataOwner(cls);
                if (api.Kind is ReflectionOpKind.Lookup or ReflectionOpKind.Enumerate)
                    NoteListedSiteRows(app, site, owner);
                if (api.Op == ReflectionOp.AssemblyGetTypes)
                    NoteListedGenericDefinitions(owner);
                if (api.Kind == ReflectionOpKind.AttributeData)
                {
                    ApplyAttributeRead(app, site, owner, construct: false);
                    return;
                }
                if (api.Op is ReflectionOp.TypeGetType or ReflectionOp.AssemblyGetType)
                    FormNamedTypes(app, index, owner);
                if (owner.Unknown is not null && api.Kind is ReflectionOpKind.Lookup or ReflectionOpKind.Enumerate
                    && _trimReflection)
                    RecordReflectionDiagnostic(app, site, ReflectionDiagnosticKind.Metadata,
                        Subject(api) + " comes from " + owner.Unknown, null,
                        api.Name >= 0 ? StringsOf(Op(api.Name)) : null, null);
                return;
            }
            case ReflectionOpKind.Instantiate:
            {
                if (api.Op == ReflectionOp.MakeGenericType)
                    MakeGeneric(app, index, Op(api.Target), Op(api.Types), report: true);
                else if (api.Op == ReflectionOp.MakeGenericMethod)
                    ApplyMakeGenericMethod(app, index, Op(api.Target), Op(api.Types));
                return;
            }
            case ReflectionOpKind.Execute:
                ApplyExecute(app, index, site);
                return;
            case ReflectionOpKind.Construct:
                ApplyConstruct(app, site);
                return;
            case ReflectionOpKind.Bind:
            {
                bool previous = _selectingBindTargets;
                _selectingBindTargets = true;
                try
                {
                    ApplyBind(app, site);
                }
                finally
                {
                    _selectingBindTargets = previous;
                }
                return;
            }
            case ReflectionOpKind.AttributeInstances:
                ApplyAttributeRead(app, site, Op(api.Target), construct: true);
                return;
        }
    }

    /// <summary>The emission switches a classified site turns on, whatever its token
    /// shape. They select no body; every one is set while discovery scans the site, before
    /// planning reads it.</summary>
    private void NoteReflectionCapability(ReflectionApi api, Module module)
    {
        bool invoke = _reflectionInvokeUsed, bind = _reflectionDelegateBindScanned,
            named = _reflectionNamedDelegateBindUsed, dynamic = _delegateDynamicInvokeUsed,
            generic = _makeGenericTypeUsed, attributes = _reflectionAttrUsed,
            constructors = _reflectionConstructorBindingUsed;
        SetReflectionCapability(api, module);
        if (SharedGenericsEnabled && Phase == EmitPhase.Emission
            && (invoke != _reflectionInvokeUsed || bind != _reflectionDelegateBindScanned
                || named != _reflectionNamedDelegateBindUsed || dynamic != _delegateDynamicInvokeUsed
                || generic != _makeGenericTypeUsed || attributes != _reflectionAttrUsed
                || constructors != _reflectionConstructorBindingUsed))
            throw new InvalidOperationException(
                "emit protocol: a reflection capability was first set by the emission pass, after planning read it ("
                + api.Op + ")");
    }

    private void SetReflectionCapability(ReflectionApi api, Module module)
    {
        if (api.Args >= 0 && api.Op is ReflectionOp.ActivatorCreate or ReflectionOp.ActivatorCreateNamed
            or ReflectionOp.AssemblyCreateInstance or ReflectionOp.InvokeMember)
            _reflectionConstructorBindingUsed = true;
        if (ReflectionRoutes.RunsReflectedMethod(api.Op) || api.Op == ReflectionOp.InvokeMember)
        {
            _reflectionInvokeUsed = true;
            NoteObjectEqualityDispatch();
        }
        else if (ReflectionRoutes.BindsReflectedMethod(api.Op))
        {
            NoteReflectionDelegateBind(module);
            if (api.Op == ReflectionOp.CreateDelegateNamed && IsUserModule(module))
                _reflectionNamedDelegateBindUsed = true;
        }
        else if (api.Op == ReflectionOp.DynamicInvoke)
            _delegateDynamicInvokeUsed = true;
        else if (api.Op == ReflectionOp.MakeGenericType)
            _makeGenericTypeUsed = true;
        else if (ReflectionRoutes.ReadsReflectedAttributes(api.Kind))
            _reflectionAttrUsed = true;
    }

    // ---- op effects ----

    private void ApplyExecute(ReflectionApplication app, int index, RSite site)
    {
        var api = site.Api;
        var m = app.Method;
        NoteObjectEqualityDispatch();
        var target = Operand(app, site, api.Target);
        switch (api.Op)
        {
            case ReflectionOp.RunClassConstructor:
                foreach (var cls in ClassesOf(target))
                    if (!ContainsCanonPlaceholder(cls) && !ContainsGenericVar(cls))
                        ReachCctor(cls, m);
                if (target.Unknown is not null)
                    RecordReflectionDiagnostic(app, site, ReflectionDiagnosticKind.Execution,
                        "Type comes from " + target.Unknown + " — no type initializer retained",
                        null, new List<string> { ".cctor" }, "method");
                break;
            case ReflectionOp.DynamicInvoke:
                // The targets are retained by the ldftn that made the delegate; only the
                // boxes of a known delegate type's Invoke row are new.
                foreach (var e in target.Items)
                    if (e.Kind == REntityKind.Delegate && e.Type is { Kind: TypeKind.Class, Class: { } delegateClass }
                        && delegateClass.EnsureMembers().MethodsNamed("Invoke") is { Count: > 0 } invoke)
                        NoteReflectionInvokeBoxes(invoke[0]);
                break;
            case ReflectionOp.FieldGet:
            case ReflectionOp.FieldSet:
                foreach (var e in target.Items)
                    if (e.Kind == REntityKind.Field && e.Field is { } f)
                    {
                        _reflectionSelectionCount++;
                        if (api.Op == ReflectionOp.FieldGet && !f.IsLiteral)
                            NoteReflectionBoxed(f.Type);
                        // A constant answers from metadata without running the initializer.
                        if (f.IsStatic && !f.IsLiteral)
                            ReachCctor(f.DeclaringClass, m);
                    }
                    else if (e is { Kind: REntityKind.TemplateMember,
                                 Lookup: { Kind: LookupMemberKind.Field or null } })
                        foreach (var type in TemplateFieldTypes(e))
                        {
                            _reflectionSelectionCount++;
                            if (api.Op == ReflectionOp.FieldGet)
                                NoteReflectionBoxed(type);
                        }
                if (target.Unknown is not null)
                {
                    // A field the summary cannot name may be pointer-typed, and no preserve
                    // supplies the runtime-allocated Pointer box its value needs.
                    if (api.Op == ReflectionOp.FieldGet)
                        ReachPointerBox();
                    // A static field access runs its type's initializer, which nothing
                    // here retains.
                    RecordReflectionDiagnostic(app, site, ReflectionDiagnosticKind.Execution,
                        Subject(api) + " comes from " + target.Unknown
                            + " — a static field's type initializer is not retained",
                        null, new List<string> { ".cctor" }, "method");
                }
                break;
            case ReflectionOp.InvokeMember:
            {
                var owners = target;
                var names = KnownStrings(Operand(app, site, api.Name), out bool namesComplete);
                var flagSet = FlagAlternatives(Operand(app, site, api.Flags), api.DefaultFlags, out bool flagsKnown);
                // Known flags that select one member kind by exact name let a remedy name
                // that member; a field access may also run an initializer, so it preserves
                // the owner.
                string? memberKind = null;
                for (int i = 0; i < flagSet.Count; i++)
                {
                    int flags = flagSet[i];
                    if ((flags & (ReflectionApis.Public | ReflectionApis.NonPublic)) == 0)
                        flags |= ReflectionApis.Public;
                    if ((flags & (ReflectionApis.Instance | ReflectionApis.Static)) == 0)
                        flags |= ReflectionApis.Instance | ReflectionApis.Static;
                    foreach (var e in owners.Items)
                    {
                        if (e.Kind != REntityKind.Template)
                            continue;
                        if (names is not null)
                            foreach (string name in names)
                                SelectTemplateMember(e, new TemplateLookup(null, name, flags, null, -1, AllowPrefix: true),
                                    ReflectionAccessor.All, construct: false, -1, app);
                        if ((flags & 0x200) != 0)
                            SelectTemplateMember(e, new TemplateLookup(LookupMemberKind.Constructor, null, flags, null, -1),
                                ReflectionAccessor.None, construct: true, -1, app);
                    }
                    foreach (var cls in ClassesOf(owners))
                    {
                        if (names is not null)
                            foreach (string name in names)
                                foreach (var e in SelectMembers(cls, null, name, flags, null, -1, allowPrefix: true))
                                    InvokeSelected(e, app, ReflectionAccessor.All);
                        // CreateInstance ignores the name.
                        if ((flags & 0x200) != 0)
                            foreach (var e in SelectMembers(cls, LookupMemberKind.Constructor, null, flags, null, -1))
                                ConstructSelected(e.Method!, m);
                    }
                    string? kind = (flags & 1) != 0 ? null : (flags & 0x3F00) switch
                    {
                        0x100 => "method",
                        0x200 => ".ctor",
                        0x1000 or 0x2000 or 0x3000 => "property",
                        _ => null,
                    };
                    memberKind = i == 0 || memberKind == kind ? kind : null;
                }
                string? ownerUnknown = OwnerUnknown(owners);
                if (ownerUnknown is not null || !namesComplete || !flagsKnown)
                    RecordReflectionDiagnosticOwners(app, site, ReflectionDiagnosticKind.Execution,
                        "member comes from " + (ownerUnknown ?? (!namesComplete ? "a name not known statically" : "unknown binding flags")),
                        owners, namesComplete ? names : null, flagsKnown ? memberKind : null);
                break;
            }
            default:
            {
                var accessor = api.Op switch
                {
                    ReflectionOp.PropertyGet => ReflectionAccessor.Getter,
                    ReflectionOp.PropertySet => ReflectionAccessor.Setter,
                    ReflectionOp.EventAdd => ReflectionAccessor.Adder,
                    ReflectionOp.EventRemove => ReflectionAccessor.Remover,
                    _ => ReflectionAccessor.None,
                };
                foreach (var e in target.Items)
                    InvokeSelected(e, app, accessor);
                if (target.Unknown is not null)
                {
                    string? memberKind = MemberKindOf(api.Op);
                    RecordReflectionDiagnostic(app, site, ReflectionDiagnosticKind.Execution,
                        Subject(api) + " comes from " + target.Unknown + " — no method body retained",
                        null, target.UnknownMemberKind == memberKind ? target.UnknownMembers : null, memberKind);
                }
                break;
            }
        }
    }

    private void InvokeSelected(REntity e, ReflectionApplication app, ReflectionAccessor accessor, bool publicAccessors = false)
    {
        var caller = app.Method;
        switch (e.Kind)
        {
            case REntityKind.Method:
                InvokeTarget(e.Method!, caller);
                break;
            case REntityKind.Property:
            case REntityKind.Event:
                foreach (var accessorMethod in AccessorsOf(e, accessor == ReflectionAccessor.None ? ReflectionAccessor.All : accessor,
                             publicAccessors))
                    InvokeTarget(accessorMethod, caller);
                break;
            case REntityKind.Field when e.Field is { } f:
                if (!f.IsLiteral)
                    NoteReflectionBoxed(f.Type);
                if (f.IsStatic && !f.IsLiteral)
                    ReachCctor(f.DeclaringClass, caller);
                break;
            case REntityKind.Delegate when e.Targets is { } targets:
                foreach (var t in targets.Items)
                    InvokeSelected(t, app, accessor);
                break;
            case REntityKind.TemplateMember when e.Lookup is { } lookup:
                SelectTemplateMember(e, lookup, e.Number != 0 ? (ReflectionAccessor)e.Number : accessor,
                    lookup.Kind == LookupMemberKind.Constructor, -1, app, e.PublicAccessors || publicAccessors);
                break;
        }
    }

    /// <summary>Set while a delegate binding selects its targets: binding runs no body, so
    /// the initializers a bound body reaches keep their first-use timing instead of
    /// joining startup.</summary>
    private bool _selectingBindTargets;

    /// <summary>Admits a method reflection runs: its body, the slot or generic virtual
    /// dispatcher a receiver's override runs through, a static's initializer, and the
    /// values Invoke boxes. A generic method definition is not invocable.</summary>
    private void InvokeTarget(MethodInfo target, MethodInfo caller)
    {
        var cls = target.DeclaringClass;
        if (IsOpenGenericMethod(target) || ContainsCanonPlaceholder(cls) || ContainsGenericVar(cls)
            || _backend?.ShouldSkipMethodBody(cls, target) == true)
            return;
        _reflectionSelectionCount++;
        if (target.Rva != 0 && cls.IntrinsicCppName is null)
        {
            if (_selectingBindTargets)
                ReachBindingInitializer(target);
            else
                Reach(target);
        }
        if (target.IsVirtual && !target.IsStatic && CarriesReflectedSlots(cls))
        {
            if (IsGvmCall(target))
                ReachReflectedGvm(cls, target, strips: false);
            else
                MarkReflectedSlot(target, named: true);
        }
        // A bound static method of a precise-init type runs that type's initializer when
        // the delegate first calls it. A bound initializer itself keeps first-use timing.
        if (target.IsStatic && (!_selectingBindTargets || !cls.IsBeforeFieldInit && target.Name != ".cctor"))
            ReachCctor(cls, caller);
        try
        {
            // A runtime numeric row consumes a managed provider through the demanded
            // GetFormat slot, as a call site or ldftn of the same method demands it.
            if (RuntimeNumericProviderConsumer(target))
                ReachFormatProviderGetFormat();
            NoteReflectionInvokeBoxes(target);
        }
        catch (NotSupportedException e) when (!IsMustEscape(e))
        {
        }
    }

    /// <summary>Whether <paramref name="m"/> is a generic method definition. A lookup
    /// selects one in its typical form, whose arguments are its own parameters.</summary>
    private static bool IsOpenGenericMethod(MethodInfo m)
    {
        if (m.Handle.IsNil)
            return false;
        int arity = m.Module.Reader.GetMethodDefinition(m.Handle).GetGenericParameters().Count;
        return arity > 0 && (m.Context.MethodArgs.Length != arity || m.Context.MethodArgs.Any(ContainsGenericVar));
    }

    private void ApplyConstruct(ReflectionApplication app, RSite site)
    {
        var api = site.Api;
        var m = app.Method;
        RResolved types;
        switch (api.Op)
        {
            case ReflectionOp.CtorInvoke:
            {
                var ctors = Operand(app, site, api.Target);
                foreach (var e in ctors.Items)
                    if (e.Kind == REntityKind.Method && e.Method is { Name: ".ctor" } ctor)
                        ConstructSelected(ctor, m);
                    else if (e is { Kind: REntityKind.TemplateMember, Lookup: { Kind: LookupMemberKind.Constructor } lookup })
                        SelectTemplateMember(e, lookup, ReflectionAccessor.None, construct: true, -1, app);
                if (ctors.Unknown is not null)
                    RecordReflectionDiagnostic(app, site, ReflectionDiagnosticKind.Execution,
                        "ConstructorInfo comes from " + ctors.Unknown + " — no constructor retained", null, null, ".ctor");
                return;
            }
            case ReflectionOp.GetUninitializedObject:
            {
                // An allocation that runs the type's initializer and no constructor: the
                // instance dispatches through the type's slots and interface map.
                var allocated = Operand(app, site, api.Target);
                string? unknown = allocated.Unknown;
                foreach (var e in allocated.Items)
                    if (e.Kind == REntityKind.Template)
                        unknown ??= "a runtime-instantiated generic type";
                foreach (var cls in ClassesOf(allocated))
                {
                    if (ContainsCanonPlaceholder(cls) || ContainsGenericVar(cls))
                        continue;
                    _reflectionSelectionCount++;
                    if (EnsureShape(cls).IsValueType)
                        NoteReflectionBoxed(TypeDesc.MakeClass(cls));
                    else if (!cls.IsAbstract && !cls.IsInterface)
                        ReachAllocatedType(cls);
                    ReachCctor(cls, m);
                }
                if (unknown is not null)
                    RecordReflectionDiagnostic(app, site, ReflectionDiagnosticKind.Execution,
                        "Type comes from " + unknown + " — no type allocated", null, null, ".ctor");
                return;
            }
            case ReflectionOp.ActivatorCreateGeneric:
                types = site.TypeArgument.IsEmpty ? Operand(app, site, api.Target) : Resolve(app, site.TypeArgument);
                break;
            case ReflectionOp.ActivatorCreateNamed:
            case ReflectionOp.AssemblyCreateInstance:
                types = NamedTypes(app, site);
                break;
            default:
                types = Operand(app, site, api.Target);
                break;
        }
        int arguments = -1;
        bool anyArity = false;
        if (api.Args >= 0)
        {
            var args = Operand(app, site, api.Args);
            arguments = KnownLength(args);
            anyArity = arguments < 0;
        }
        else
            arguments = 0;
        bool nonPublic = false;
        if (api.NonPublic >= 0)
            nonPublic = MayBeTrue(Operand(app, site, api.NonPublic));
        int defaultFlags = ReflectionApis.PublicInstance | (nonPublic ? ReflectionApis.NonPublic : 0);
        var flagSet = api.Flags >= 0 ? FlagAlternatives(Operand(app, site, api.Flags), defaultFlags, out _)
            : new List<int> { defaultFlags };
        // Activator.CreateInstance reads flags naming no lookup bit (mask 0xFF) as
        // Public | Instance. Instance stays implied: the runtime's parameterless
        // activation ignores it.
        for (int i = 0; i < flagSet.Count; i++)
            flagSet[i] = ((flagSet[i] & 0xFF) == 0 ? flagSet[i] | ReflectionApis.PublicInstance : flagSet[i])
                | ReflectionApis.Instance;
        foreach (var e in types.Items)
        {
            if (e.Kind == REntityKind.Template)
            {
                foreach (int flags in flagSet)
                    SelectTemplateMember(e, new TemplateLookup(LookupMemberKind.Constructor, null, flags, null, -1),
                        ReflectionAccessor.None, construct: true, anyArity ? -1 : arguments, app);
                continue;
            }
            if (e.Kind != REntityKind.Type || e.Inherited || e.Type is not { Kind: TypeKind.Class, Class: { } cls })
                continue;
            if (ContainsCanonPlaceholder(cls) || ContainsGenericVar(cls))
                continue;
            bool constructed = false;
            var selected = new List<MethodInfo>();
            foreach (int flags in flagSet)
                foreach (var c in SelectMembers(cls, LookupMemberKind.Constructor, null, flags, null, -1))
                    if (!selected.Contains(c.Method!))
                        selected.Add(c.Method!);
            foreach (var ctor in selected)
            {
                int count = ctor.Signature.ParameterTypes.Length;
                if (!anyArity && count != arguments)
                    continue;
                ConstructSelected(ctor, m);
                constructed = true;
            }
            if (!constructed && EnsureShape(cls).IsValueType)
            {
                _reflectionSelectionCount++;
                NoteReflectionBoxed(cls);
                ReachCctor(cls, m);
            }
        }
        if (types.Unknown is not null)
            RecordReflectionDiagnostic(app, site, ReflectionDiagnosticKind.Execution,
                "Type comes from " + types.Unknown + " — no constructor retained", null, null, ".ctor");
    }

    private void ConstructSelected(MethodInfo ctor, MethodInfo caller)
    {
        var cls = ctor.DeclaringClass;
        if (ContainsCanonPlaceholder(cls) || ContainsGenericVar(cls) || _backend?.ShouldSkipMethodBody(cls, ctor) == true)
            return;
        _reflectionSelectionCount++;
        if (ctor.Rva != 0 && cls.IntrinsicCppName is null)
            Reach(ctor);
        if (EnsureShape(cls).IsValueType)
            NoteReflectionBoxed(cls);
        else if (!cls.IsAbstract && !cls.IsInterface && cls.IntrinsicCppName is null)
            ReachAllocatedType(cls);
        ReachCctor(cls, caller);
        try
        {
            NoteReflectionInvokeBoxes(ctor);
        }
        catch (NotSupportedException e) when (!IsMustEscape(e))
        {
        }
    }

    private void ApplyBind(ReflectionApplication app, RSite site)
    {
        var api = site.Api;
        NoteObjectEqualityDispatch();
        var delegateTypes = api.DelegateType >= 0 ? Operand(app, site, api.DelegateType)
            : !site.TypeArgument.IsEmpty ? Resolve(app, site.TypeArgument) : RResolved.Empty;
        // A specialization knows it is a delegate only with its shape.
        foreach (var cls in ClassesOf(delegateTypes))
            if (EnsureShape(cls).IsDelegate && !cls.IsAbstract)
                ReachAllocatedType(cls);
        if (api.Op == ReflectionOp.CreateDelegate)
        {
            var targets = Operand(app, site, api.Target);
            foreach (var e in targets.Items)
                InvokeSelected(e, app, ReflectionAccessor.None);
            if (targets.Unknown is not null)
                RecordReflectionDiagnostic(app, site, ReflectionDiagnosticKind.Execution,
                    "MethodInfo comes from " + targets.Unknown + " — no method body retained", null,
                    targets.UnknownMemberKind == "method" ? targets.UnknownMembers : null, "method");
            return;
        }
        var owners = Operand(app, site, api.Target);
        var names = KnownStrings(Operand(app, site, api.Name), out bool namesComplete);
        int nameFlags = api.IgnoreCase >= 0 && MayBeTrue(Operand(app, site, api.IgnoreCase)) ? 1 : 0;
        bool isStatic = (api.DefaultFlags & ReflectionApis.Static) != 0;
        int bindFlags = nameFlags | (isStatic ? ReflectionApis.Static : ReflectionApis.Instance);
        var selected = NamedBindingMembers(owners, names, new List<int> { bindFlags }, delegateTypes);
        foreach (var member in selected.Items)
            InvokeSelected(member, app, ReflectionAccessor.None);
        if (owners.Unknown is not null || selected.Unknown is not null || !namesComplete)
            RecordReflectionDiagnosticOwners(app, site, ReflectionDiagnosticKind.Execution,
                "target comes from " + (owners.Unknown ?? selected.Unknown ?? "a name not known statically") + " — no method body retained",
                owners, namesComplete ? names : null, "method");
    }

    /// <summary>Execution and Delegate.Method select the same named binding: every
    /// visibility in the base chain, bounded by name, case and delegate Invoke arity.</summary>
    private RResolved NamedBindingMembers(RResolved owners, List<string>? names, List<int> flags, RResolved delegateTypes)
    {
        var result = new RResolved();
        var arities = new List<int>();
        bool anyArity = delegateTypes.Unknown is not null;
        foreach (var cls in ClassesOf(delegateTypes))
            if (EnsureShape(cls).IsDelegate && cls.EnsureMembers().MethodsNamed("Invoke") is { Count: > 0 } invoke)
            {
                int arity = invoke[0].Signature.ParameterTypes.Length;
                if (!arities.Contains(arity))
                    arities.Add(arity);
            }
        if (anyArity || arities.Count == 0)
            arities = new List<int> { -1 };
        if (names is null)
            return result;
        foreach (var owner in owners.Items)
            foreach (int bindingFlags in flags)
                foreach (string name in names)
                    foreach (int arity in arities)
                    {
                        var lookup = new TemplateLookup(LookupMemberKind.Method, name, bindingFlags, null, arity, NamedBinding: true);
                        if (owner.Kind == REntityKind.Template)
                            result.Add(new REntity
                            {
                                Kind = REntityKind.TemplateMember, Assembly = owner.Assembly,
                                Definition = owner.Definition, Tuple = owner.Tuple, Lookup = lookup,
                            });
                        else if (owner.Kind is REntityKind.Type or REntityKind.Object && OwnerClass(owner.Type) is { } cls)
                            result.AddRange(SelectLookupMembers(cls, lookup));
                    }
        return result;
    }

    /// <summary>An attribute read on its owners' rows. Every row the read can see is
    /// listed; an instance read also constructs the rows its attribute-type filter
    /// admits, a data read (IsDefined, CustomAttributeData) none: a user attribute's row
    /// renders without its factory. An unknown owner reports without selecting
    /// additional providers or executable attribute bodies.</summary>
    private void ApplyAttributeRead(ReflectionApplication app, RSite site, RResolved owners, bool construct)
    {
        var api = site.Api;
        var filters = api.AttributeType >= 0 ? Operand(app, site, api.AttributeType)
            : !site.TypeArgument.IsEmpty ? Resolve(app, site.TypeArgument) : null;
        bool open = true;
        var classes = filters is null ? new List<ClassInfo>() : AttributeFilterClasses(filters, out open);
        var known = open ? null : classes;
        bool inherit = api.Inherit >= 0 ? MayBeTrue(Operand(app, site, api.Inherit)) : api.DefaultInherit;
        // A clone's closed base is an owner for lookups only; the clone's rows are its
        // template's.
        foreach (var e in owners.Items)
            if (!e.Inherited)
                ReachOwnerAttributes(e, inherit, known, construct);
        string? ownerUnknown = OwnerUnknown(owners);
        if (ownerUnknown is null)
            return;
        RecordReflectionDiagnostic(app, site, ReflectionDiagnosticKind.AttributeInstances,
            "attribute owner comes from " + ownerUnknown,
            null, null, null);
    }

    /// <summary>The attribute classes a filter's known alternatives admit rows of, by
    /// derivation. <paramref name="open"/> is true when some alternative may be any type
    /// or is an interface, which admits every row.</summary>
    private static List<ClassInfo> AttributeFilterClasses(RResolved filters, out bool open)
    {
        open = filters.Unknown is not null;
        var classes = new List<ClassInfo>();
        foreach (var e in filters.Items)
        {
            if (e.Kind == REntityKind.Null)
                continue;
            if (e.Kind != REntityKind.Type || e.Type is not { Kind: TypeKind.Class, Class: { IsInterface: false } cls })
                open = true;
            else if (!classes.Contains(cls))
                classes.Add(cls);
        }
        return classes;
    }

    /// <summary>The rows of one owner: a type's or a generic definition's own and, with
    /// <paramref name="inherit"/>, its bases'; a member's, a parameter's or an
    /// assembly's own, which is all the runtime serves for them.</summary>
    private void ReachOwnerAttributes(REntity e, bool inherit, List<ClassInfo>? admitted, bool construct)
    {
        if (e.Kind == REntityKind.DefinitionMember && e.Metadata is { } metadata)
        {
            ReachOwnerAttributes(metadata, inherit, admitted, construct);
            return;
        }
        switch (e.Kind)
        {
            case REntityKind.Type when e.Type is { Kind: TypeKind.Class, Class: { } cls }:
                // A specialization learns its base only with its shape.
                for (ClassInfo? c = cls; c is not null; c = inherit ? EnsureShape(c).BaseClass : null)
                {
                    if (c.Handle.IsNil)
                        break;
                    ReachAttributeRows(c.Module, c.Module.Reader.GetTypeDefinition(c.Handle).GetCustomAttributes(), admitted, construct);
                }
                break;
            // A template's clone carries its definition's rows.
            case REntityKind.Definition or REntityKind.Template when e.Assembly is { } module:
                for ((Module, TypeDefinitionHandle)? level = (module, e.Definition); level is ({ } m, var h);)
                {
                    var td = m.Reader.GetTypeDefinition(h);
                    ReachAttributeRows(m, td.GetCustomAttributes(), admitted, construct);
                    level = inherit && !td.BaseType.IsNil ? TypeDefinitionOf(m, td.BaseType) : null;
                }
                break;
            case REntityKind.Method when e.Method is { Handle.IsNil: false } method:
                ReachAttributeRows(method.Module, method.Module.Reader.GetMethodDefinition(method.Handle).GetCustomAttributes(),
                    admitted, construct);
                break;
            case REntityKind.Parameter when e.Method is { Handle.IsNil: false } method:
            {
                var reader = method.Module.Reader;
                foreach (var ph in reader.GetMethodDefinition(method.Handle).GetParameters())
                {
                    var parameter = reader.GetParameter(ph);
                    if (parameter.SequenceNumber == e.Number + 1)
                        ReachAttributeRows(method.Module, parameter.GetCustomAttributes(), admitted, construct);
                }
                break;
            }
            case REntityKind.Field when e.Field is { Handle.IsNil: false } field:
                ReachAttributeRows(field.DeclaringClass.Module,
                    field.DeclaringClass.Module.Reader.GetFieldDefinition(field.Handle).GetCustomAttributes(), admitted, construct);
                break;
            case REntityKind.Property when e.Owner is { } owner:
                ReachAttributeRows(owner.Module,
                    owner.Module.Reader.GetPropertyDefinition((PropertyDefinitionHandle)e.Member).GetCustomAttributes(),
                    admitted, construct);
                break;
            case REntityKind.Event when e.Owner is { } owner:
                ReachAttributeRows(owner.Module,
                    owner.Module.Reader.GetEventDefinition((EventDefinitionHandle)e.Member).GetCustomAttributes(),
                    admitted, construct);
                break;
            case REntityKind.Assembly when e.Assembly is { } module:
                ReachAttributeRows(module, module.Reader.GetAssemblyDefinition().GetCustomAttributes(), admitted, construct);
                break;
            case REntityKind.TemplateMember when e is { Assembly: { } module, Lookup: { } lookup }:
                foreach (var member in TemplateMetadataMembers(e))
                    ReachOwnerAttributes(member, inherit, admitted, construct);
                break;
        }
    }

    /// <summary>Lists every row of one element and constructs those a filter admits
    /// (<paramref name="admitted"/> null admits all) when <paramref name="construct"/>. Only
    /// an instance read runs named-argument setters: a row whose constructor another route
    /// reached keeps a factory-less row for a data read, which answers from metadata.</summary>
    private void ReachAttributeRows(Module module, CustomAttributeHandleCollection handles, List<ClassInfo>? admitted,
        bool construct)
    {
        if (handles.Count == 0)
            return;
        _reflectionSelectionCount++;
        ReachAttributesOf(module, handles, reach: false);
        List<DecodedAttribute> rows;
        try
        {
            rows = DecodeCustomAttributes(module, handles, reachedCtorsOnly: false);
        }
        catch (NotSupportedException e) when (!IsMustEscape(e))
        {
            return;
        }
        if (construct)
            foreach (var row in rows)
                if (Admits(admitted, row.AttrClass))
                    ConstructAttributeRow(row);
    }

    private static bool Admits(List<ClassInfo>? admitted, ClassInfo attribute)
    {
        if (admitted is null)
            return true;
        foreach (var filter in admitted)
            if (DerivesFromOrIs(attribute, filter))
                return true;
        return false;
    }

    /// <summary>The reaching half of <see cref="ReachAttributesOf"/> for one row: its
    /// constructor, named-property setters, allocated type and argument notes.</summary>
    private void ConstructAttributeRow(DecodedAttribute da)
    {
        Reach(da.Ctor);
        NoteArrayElementType(TypeDesc.MakeClass(da.AttrClass));
        if (!da.AttrClass.IsValueType && !da.AttrClass.IsAbstract)
            ReachAllocatedType(da.AttrClass);
        var ps = da.Ctor.Signature.ParameterTypes;
        for (int i = 0; i < da.Fixed.Length; i++)
            NoteAttrArg(da.Fixed[i].Type, da.Fixed[i].Value, i < ps.Length && ps[i].IsObject, true);
        foreach (var na in da.Named)
        {
            bool boxed = false;
            if (na.Kind == CustomAttributeNamedArgumentKind.Property && na.Name is { } pn
                && da.AttrClass.InstanceMethodOnBaseChain("set_" + pn) is { } sm)
            {
                Reach(sm);
                boxed = sm.Signature.ParameterTypes.Length > 0 && sm.Signature.ParameterTypes[0].IsObject;
            }
            else if (na.Kind == CustomAttributeNamedArgumentKind.Field && na.Name is { } fn
                && da.AttrClass.InstanceFieldOnBaseChain(fn) is { } f)
                boxed = f.Type.IsObject;
            NoteAttrArg(na.Type, na.Value, boxed, true);
        }
    }

    private bool _attributeArrayNoted;

    /// <summary>Gives System.Attribute, the base filter a GetCustomAttributes(Type) commonly
    /// names beside a concrete attribute type, the array handle the runtime's attrType[]
    /// stamp (dn2cpp_find_array_ti) answers with, once anything reads attributes.</summary>
    internal void NoteAttributeArrayHandle()
    {
        if (!_reflectionAttrUsed || _attributeArrayNoted)
            return;
        _attributeArrayNoted = true;
        if (TypeIndex().TryGetValue(("System", "Attribute"), out var candidates)
            && candidates[0].Item1.ClassMap.TryGetValue(candidates[0].Item2, out var attribute))
            NoteArrayElementType(TypeDesc.MakeClass(attribute));
    }

    // ---- generic instantiation ----

    /// <summary>The closed types a MakeGenericType over <paramref name="defs"/> and
    /// <paramref name="args"/> yields. Each known closed argument tuple takes the first of:
    /// an instantiation the program already has; the definition's runtime-instantiation
    /// template, which serves the tuple without minting it while it can run what the sites
    /// ask of the clone (<see cref="SelectTemplateMember"/>); a static instantiation when the
    /// arguments meet the constraints, under the normal instantiation bound; else a
    /// ClosedGeneric diagnostic. A tuple the constraints definitely reject yields nothing,
    /// as .NET throws. Unknown arguments request only template identity, and report
    /// ClosedGeneric when no template survives planning. They supply no executable
    /// selection candidates. Forming the type admits no body.</summary>
    private RResolved MakeGeneric(ReflectionApplication app, int site, RResolved defs, RResolved args, bool report)
    {
        var result = new RResolved();
        string? receiverUnknown = defs.Unknown, unclosable = null, argumentsUnknown = null;
        foreach (var e in defs.Items)
        {
            if (e.Kind != REntityKind.Definition || e.Assembly is not { } module)
            {
                // A closed or non-generic type, a clone and null throw as .NET throws. An
                // object may be any Type; a definition only the runtime models has no
                // template and no static instantiation.
                if (e.Kind == REntityKind.Object)
                    receiverUnknown ??= "an object the summary cannot read as a generic definition";
                else if (e is { Kind: REntityKind.Type, Inherited: false, Type: { Kind: TypeKind.External, ExternalName: { } name } }
                         && name.Contains('`'))
                    unclosable ??= name;
                continue;
            }
            var tuples = ArgumentTuples(args, out bool argsKnown, closeTemplates: true);
            bool templated = false;
            bool oneType = RuntimeModelsAsOneType(BacktickFullName(module, e.Definition));
            if (oneType && report && site >= 0)
                RecordReflectionDiagnostic(app, app.Summary.Sites[site], ReflectionDiagnosticKind.ClosedGeneric,
                    BacktickFullName(module, e.Definition) + OneRuntimeTypeReason, null, null, DirectConstructionRemedy);
            if (tuples.Count > 0)
            {
                var parameters = module.Reader.GetTypeDefinition(e.Definition).GetGenericParameters();
                bool? servable = null;
                foreach (var tuple in tuples)
                {
                    if (!InstancesFor(module, e.Definition).TryGetValue(tuple, out var cls))
                    {
                        bool? admitted = GenericArgumentsAdmit(module, parameters, tuple, Array.Empty<TypeDesc>());
                        if (admitted == false)
                            continue;
                        servable ??= RuntimeTemplateMayServe(module, e.Definition);
                        if (servable == true)
                        {
                            SelectTemplate(app, site, module, e.Definition, report,
                                "the program names no instantiation over these type arguments", tuple);
                            templated = true;
                            result.Add(new REntity
                            {
                                Kind = REntityKind.Template, Assembly = module, Definition = e.Definition, Tuple = tuple,
                            });
                            if (ClosedTemplateBase(module, e.Definition, tuple) is { } inherited)
                                result.Add(new REntity { Kind = REntityKind.Type, Type = TypeDesc.MakeClass(inherited), Inherited = true });
                            continue;
                        }
                        if (admitted is null)
                        {
                            if (report && site >= 0)
                                RecordReflectionDiagnostic(app, app.Summary.Sites[site], ReflectionDiagnosticKind.ClosedGeneric,
                                    "the constraints of " + BacktickFullName(module, e.Definition)
                                    + " over these type arguments cannot be decided ahead of time, and it has no"
                                    + " runtime-instantiation template; only instantiations the program names resolve",
                                    null, null, null);
                            continue;
                        }
                        cls = StaticReflectionType(module, e.Definition, tuple);
                        if (cls is null)
                            continue;
                        // A runtime-modeled closed type is a lowering, not a class the image
                        // emits for reflection to find.
                        if (cls.IntrinsicCppName is not null && !oneType && report && site >= 0)
                            RecordReflectionDiagnostic(app, app.Summary.Sites[site], ReflectionDiagnosticKind.ClosedGeneric,
                                BacktickFullName(module, e.Definition) + " is modeled by the runtime and has no"
                                + " runtime-instantiation template; only instantiations the program names resolve",
                                null, null, null);
                    }
                    var type = TypeDesc.MakeClass(cls);
                    NoteTypeIdentityClosure(type);
                    result.Add(TypeEntity(type));
                }
            }
            if (!argsKnown)
            {
                string? unknown = ArgumentsUnknown(args);
                SelectTemplate(app, site, module, e.Definition, report && unknown is not null && !oneType,
                    "the type arguments come from " + unknown, null);
                argumentsUnknown ??= unknown ?? "type arguments with no known closed tuple";
            }
            if (templated)
                NoteGrowingSelection(app, module.Index, SRME.GetToken(e.Definition));
        }
        if (receiverUnknown is not null)
        {
            _makeGenericTypeUnknownReceiver = true;
            result.Unknown = "MakeGenericType over a definition that comes from " + receiverUnknown;
        }
        else if (unclosable is not null)
            result.Unknown = "MakeGenericType over " + unclosable + ", which only the runtime models";
        else if (argumentsUnknown is not null)
            result.Unknown = "MakeGenericType over " + argumentsUnknown;
        // The site reports even when its result goes unused: no template or body is
        // selected for a definition it cannot name.
        if (report && site >= 0 && receiverUnknown is null && unclosable is not null && RuntimeModelsAsOneType(unclosable))
            RecordReflectionDiagnostic(app, app.Summary.Sites[site], ReflectionDiagnosticKind.ClosedGeneric,
                unclosable + OneRuntimeTypeReason, null, null, DirectConstructionRemedy);
        else if (report && site >= 0 && (receiverUnknown ?? unclosable) is not null)
            RecordReflectionDiagnostic(app, app.Summary.Sites[site], ReflectionDiagnosticKind.ClosedGeneric,
                (receiverUnknown is not null ? "the generic definition comes from " + receiverUnknown
                    : unclosable + " is modeled by the runtime and has no runtime-instantiation template")
                + "; only instantiations the program names resolve", null, null, null);
        return result;
    }

    private const string OneRuntimeTypeReason =
        " is modeled by the runtime as one type for every instantiation, so MakeGenericType cannot form it";

    /// <summary>The ClosedGeneric member kind whose remedy bypasses MakeGenericType.</summary>
    private const string DirectConstructionRemedy = "direct construction";

    /// <summary>Whether the runtime models every instantiation of a definition, by its
    /// backtick full name, as one type that records no type arguments: MakeGenericType
    /// finds no closed type of it, even one the program constructs.</summary>
    private static bool RuntimeModelsAsOneType(string backtickName)
    {
        int tick = backtickName.IndexOf('`');
        return tick > 0 && CoreIntrinsics.SharesOneRuntimeType(backtickName.Substring(0, tick));
    }

    /// <summary>Re-applies the site being applied when the definition (module index and
    /// token) gains an instantiation, and marks the terms being resolved as listing its
    /// instantiations, so every other site that reads one of them is re-applied too.</summary>
    private void NoteGrowingSelection(ReflectionApplication app, int module, int definition)
    {
        foreach (int term in app.Resolving)
        {
            if (!app.Growing.TryGetValue(term, out var growing))
                app.Growing[term] = growing = new List<(int, int)>();
            if (!growing.Contains((module, definition)))
                growing.Add((module, definition));
        }
        if (app.Site >= 0 && _pendingReflectionSelectionsSeen.Add((module, definition, app.Method, app.Site)))
            _pendingReflectionSelections.Add(new PendingSelection(module, definition, app.Method, app.Site));
    }

    /// <summary>The first level below the template levels of a clone of
    /// <paramref name="def"/> over <paramref name="tuple"/> that the image already has: the
    /// clone inherits that class's members, so lookups on it select them there. Null when
    /// every base is a template level or the matching closed class does not exist.</summary>
    private ClassInfo? ClosedTemplateBase(Module module, TypeDefinitionHandle def, TypeDesc[] tuple)
    {
        try
        {
            for (var (m, h, args) = (module, def, tuple); ;)
            {
                var baseHandle = m.Reader.GetTypeDefinition(h).BaseType;
                if (baseHandle.IsNil)
                    return null;
                if (baseHandle.Kind != HandleKind.TypeSpecification
                    || !m.Reader.GetTypeSpecification((TypeSpecificationHandle)baseHandle).DecodeSignature(ParameterMentionProbe.Instance, null))
                    return ResolveTypeTokenForScan(m, baseHandle, GenericContext.Empty) is { Kind: TypeKind.Class, Class: { } closed } ? closed : null;
                var next = ProjectedBaseArguments(m, baseHandle, args);
                if (TypeDefinitionOf(m, baseHandle) is not ({ } nm, var nh))
                    return null;
                if (InstancesFor(nm, nh).TryGetValue(next, out var cls))
                    return cls;
                (m, h, args) = (nm, nh, next);
            }
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            return null;
        }
    }

    /// <summary>The arguments of a template level's base <paramref name="baseHandle"/>, a
    /// projection of the level's own <paramref name="args"/>:
    /// <see cref="RuntimeTemplateMayServe"/> admitted only bare parameter projections.</summary>
    private static TypeDesc[] ProjectedBaseArguments(Module m, EntityHandle baseHandle, TypeDesc[] args)
    {
        var blob = m.Reader.GetBlobReader(m.Reader.GetTypeSpecification((TypeSpecificationHandle)baseHandle).Signature);
        blob.ReadSignatureTypeCode();
        blob.ReadSignatureTypeCode();
        blob.ReadTypeHandle();
        var next = new TypeDesc[blob.ReadCompressedInteger()];
        for (int i = 0; i < next.Length; i++)
        {
            blob.ReadSignatureTypeCode();
            next[i] = args[blob.ReadCompressedInteger()];
        }
        return next;
    }

    /// <summary>Type.BaseType of a clone of <paramref name="def"/> over
    /// <paramref name="tuple"/>, null when the arguments are not known: the closed class a
    /// base naming no parameter is, or the instantiation of a projected base level the image
    /// has; any other base exists only as the runtime clones it.</summary>
    private void AddTemplateBase(RResolved result, ReflectionApplication app, Module module, TypeDefinitionHandle def,
        TypeDesc[]? tuple)
    {
        var baseHandle = module.Reader.GetTypeDefinition(def).BaseType;
        if (baseHandle.IsNil)
            return;
        if (baseHandle.Kind != HandleKind.TypeSpecification
            || !module.Reader.GetTypeSpecification((TypeSpecificationHandle)baseHandle).DecodeSignature(ParameterMentionProbe.Instance, null))
        {
            AddType(result, ResolveTypeTokenForScan(module, baseHandle, GenericContext.Empty), app);
            return;
        }
        if (tuple is not null && TypeDefinitionOf(module, baseHandle) is ({ } nm, var nh)
            && InstancesFor(nm, nh).TryGetValue(ProjectedBaseArguments(module, baseHandle, tuple), out var cls))
        {
            result.Add(TypeEntity(TypeDesc.MakeClass(cls)));
            return;
        }
        result.Unknown ??= "the base of a runtime-instantiated generic type";
    }

    /// <summary>Whether a runtime-instantiation template can serve generic definition
    /// <paramref name="def"/>: the shape test of <see cref="RuntimeTemplateShapeEligible"/>
    /// read from metadata, so asking it before planning builds no template and mints
    /// nothing. A definition it admits still loses its template when a body fails the shared
    /// trial; its sites then report ClosedGeneric.</summary>
    private bool RuntimeTemplateMayServe(Module module, TypeDefinitionHandle def)
    {
        if (!SharedGenericsEnabled)
            return false;
        try
        {
            var td = module.Reader.GetTypeDefinition(def);
            if ((td.Attributes & (TypeAttributes.Interface | TypeAttributes.Abstract)) != 0)
                return false;
            if (!td.BaseType.IsNil && TypeDefinitionOf(module, td.BaseType) is ({ } bm, var bh)
                && RawSignatureProvider.TypeDefinitionName(bm.Reader, bh)
                    is "System.ValueType" or "System.Enum" or "System.Delegate" or "System.MulticastDelegate")
                return false;
            for (var (m, h) = (module, def); ;)
            {
                var reader = m.Reader;
                var level = reader.GetTypeDefinition(h);
                string ns = reader.GetString(level.Namespace);
                string name = StripArity(reader.GetString(level.Name));
                if (!level.GetDeclaringType().IsNil
                    || CoreIntrinsics.IntrinsicGenericCppType(ns.Length == 0 ? name : ns + "." + name) is not null
                    || AdoptedAsyncCpp(m, h) is not null)
                    return false;
                foreach (var mh in level.GetMethods())
                    if (reader.GetString(reader.GetMethodDefinition(mh).Name) == ".cctor")
                        return false;
                foreach (var fh in level.GetFields())
                {
                    var field = reader.GetFieldDefinition(fh);
                    if ((field.Attributes & FieldAttributes.Literal) != 0)
                        continue;
                    if ((field.Attributes & FieldAttributes.Static) != 0
                        || field.DecodeSignature(ParameterMentionProbe.Instance, null))
                        return false;
                }
                foreach (var ih in level.GetInterfaceImplementations())
                    if (reader.GetInterfaceImplementation(ih).Interface is { Kind: HandleKind.TypeSpecification } itf
                        && reader.GetTypeSpecification((TypeSpecificationHandle)itf).DecodeSignature(ParameterMentionProbe.Instance, null))
                        return false;
                var baseHandle = level.BaseType;
                if (baseHandle.Kind != HandleKind.TypeSpecification)
                    return true;
                var spec = reader.GetTypeSpecification((TypeSpecificationHandle)baseHandle);
                if (!spec.DecodeSignature(ParameterMentionProbe.Instance, null))
                    return true;
                // A base naming the parameters is a template level too, reached through an
                // injective projection of bare parameters.
                var blob = reader.GetBlobReader(spec.Signature);
                if (blob.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance)
                    return false;
                blob.ReadSignatureTypeCode();
                blob.ReadTypeHandle();
                var used = new HashSet<int>();
                for (int i = blob.ReadCompressedInteger(); i > 0; i--)
                    if (blob.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeParameter
                        || !used.Add(blob.ReadCompressedInteger()))
                        return false;
                if (TypeDefinitionOf(m, baseHandle) is not ({ } nm, var nh))
                    return false;
                (m, h) = (nm, nh);
            }
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            return false;
        }
    }

    /// <summary>The instantiations of an open generic method that a MakeGenericMethod over
    /// <paramref name="tuples"/> yields: an admitted tuple instantiates it as a static
    /// mention would, a rejected one yields nothing, as .NET throws, and
    /// <paramref name="undecided"/> tells whether some tuple's constraints could not be
    /// decided. Executable owners list each instance; open owners retain a detached
    /// signature context for the metadata the runtime constructs.</summary>
    private List<MethodInfo> MakeGenericMethods(MethodInfo generic, List<TypeDesc[]> tuples, out bool undecided)
    {
        undecided = false;
        var made = new List<MethodInfo>();
        bool metadata = _reflectionMetadataViews.Contains(generic.DeclaringClass);
        var parameters = generic.Module.Reader.GetMethodDefinition(generic.Handle).GetGenericParameters();
        foreach (var tuple in tuples)
            switch (GenericArgumentsAdmit(generic.Module, parameters, generic.DeclaringClass.Context.TypeArgs, tuple, metadata))
            {
                case true:
                    var instance = metadata ? ReflectionDefinitionMethod(generic, tuple)
                        : InstantiateMethodOnClass(generic.DeclaringClass, generic.Module, generic.Handle, tuple);
                    if (!metadata)
                        _reflectionListedRows.Add(instance);
                    made.Add(instance);
                    break;
                case null:
                    undecided = true;
                    if (metadata)
                        made.Add(ReflectionDefinitionMethod(generic, tuple));
                    break;
            }
        return made;
    }

    /// <summary>A MakeGenericMethod site: known admitted arguments instantiate the method.
    /// Unknown arguments add no executable candidates and report ClosedGeneric; at run
    /// time they can still resolve independently retained instantiations. A receiver the
    /// site cannot name also reports ClosedGeneric.</summary>
    private void ApplyMakeGenericMethod(ReflectionApplication app, int index, RResolved methods, RResolved args)
    {
        var tuples = ArgumentTuples(args, out bool known, closeTemplates: true);
        var generics = GenericMethodReceivers(methods, out string? receiverUnknown);
        if (receiverUnknown is not null)
            RecordReflectionDiagnostic(app, app.Summary.Sites[index], ReflectionDiagnosticKind.ClosedGeneric,
                "the generic method comes from " + receiverUnknown + "; only instantiations the program names resolve",
                null, null, null);
        foreach (var generic in generics)
        {
            if (!IsOpenGenericMethod(generic))
                continue;
            // An open declaring type constructs method metadata at run time; no
            // executable instantiation is needed, even with unknown method arguments.
            if (_reflectionMetadataViews.Contains(generic.DeclaringClass))
                continue;
            MakeGenericMethods(generic, tuples, out bool undecided);
            string reason;
            if (!known && ArgumentsUnknown(args) is { } unknown)
                reason = "the type arguments come from " + unknown;
            else if (undecided)
                reason = "the constraints over these type arguments cannot be decided ahead of time";
            else
                continue;
            RecordReflectionDiagnostic(app, app.Summary.Sites[index], ReflectionDiagnosticKind.ClosedGeneric,
                reason + "; only instantiations of " + generic.DeclaringClass.FullName + "::" + generic.Name
                + " the program names resolve", null, null, null);
        }
    }

    /// <summary>The methods a MakeGenericMethod receiver can be. A method a lookup selects
    /// on a template's clone is the static instantiation's (<see cref="StaticTemplateMembers"/>):
    /// no clone serves a generic method's instantiations. <paramref name="unknown"/> says
    /// why the receiver may be another method.</summary>
    private List<MethodInfo> GenericMethodReceivers(RResolved methods, out string? unknown)
    {
        unknown = methods.Unknown;
        var generics = new List<MethodInfo>();
        foreach (var e in methods.Items)
            if (e.Kind == REntityKind.Method && e.Method is { } method)
                generics.Add(method);
            else if (e.Kind == REntityKind.DefinitionMember && e.Metadata is { Kind: REntityKind.Method, Method: { } definition })
                generics.Add(definition);
            else if (e.Kind == REntityKind.TemplateMember)
            {
                if (StaticTemplateMembers(e) is not { } members)
                    unknown ??= e.Tuple is null
                        ? "a method of a runtime-instantiated generic type whose type arguments are not known"
                        : "a method of a runtime-instantiated generic type whose constraints over these type arguments cannot be proved";
                else
                    foreach (var member in members)
                        if (member.Kind == REntityKind.Method && member.Method is { } instance)
                            generics.Add(instance);
            }
        return generics;
    }

    /// <summary>The members the lookup of template member <paramref name="e"/> selects on
    /// the static instantiation of its known tuple, minted as <see cref="SelectTemplateMember"/>
    /// mints one for a request the template cannot run; every site naming the tuple then
    /// resolves to that class. Null when the tuple is not known or its constraints cannot
    /// be proved.</summary>
    private List<REntity>? StaticTemplateMembers(REntity e)
    {
        if (e is not { Kind: REntityKind.TemplateMember, Assembly: { } module, Tuple: { } tuple, Lookup: { } lookup })
            return null;
        if (StaticReflectionType(module, e.Definition, tuple) is not { } cls)
            return null;
        NoteTypeIdentityClosure(TypeDesc.MakeClass(cls));
        return NoteListedRows(SelectLookupMembers(cls, lookup));
    }

    /// <summary>The types of the non-literal fields a lookup on a clone may select.
    /// A template's fields name no type parameter (<see cref="RuntimeTemplateMayServe"/>), so each type is closed;
    /// a definition no template serves reports at its MakeGenericType site.</summary>
    private List<TypeDesc> TemplateFieldTypes(REntity entity)
    {
        var types = new List<TypeDesc>();
        foreach (var member in TemplateMetadataMembers(entity))
        {
            if ((member.Metadata ?? member) is not { Kind: REntityKind.Field, Field: { IsLiteral: false } field })
                continue;
            TypeDesc type;
            if (_reflectionMetadataViews.Contains(field.DeclaringClass))
            {
                var definition = field.DeclaringClass.Module.Reader.GetFieldDefinition(field.Handle);
                if (definition.DecodeSignature(ParameterMentionProbe.Instance, null))
                    continue;
                // Reading the selected value boxes its fixed closed type, so this
                // signature demand creates its executable identity too.
                type = definition.DecodeSignature(SigProvider, GenericContext.Empty);
            }
            else
                type = field.Type;
            if (!types.Contains(type))
                types.Add(type);
        }
        return types;
    }

    /// <summary>A metadata view uses the ordinary lookup rules without publishing a
    /// runtime instantiation or reaching any member body.</summary>
    private readonly List<ClassInfo> _reflectionMetadataViews = new();
    private readonly HashSet<ClassInfo> _reflectionDefinitionCompleted = new();
    private readonly List<MethodInfo> _reflectionMetadataMethods = new();

    private MethodInfo ReflectionDefinitionMethod(MethodInfo definition, TypeDesc[] tuple)
    {
        foreach (var method in _reflectionMetadataMethods)
            if (method.DeclaringClass == definition.DeclaringClass && method.Handle == definition.Handle
                && TypeArgsComparer.Instance.Equals(method.Context.MethodArgs, tuple))
                return method;
        var instance = new MethodInfo
        {
            DeclaringClass = definition.DeclaringClass, Module = definition.Module, Handle = definition.Handle,
            Name = definition.Name, Attributes = definition.Attributes, ImplAttributes = definition.ImplAttributes,
            Context = new GenericContext(definition.Context.TypeArgs, tuple),
        };
        _reflectionMetadataMethods.Add(instance);
        return instance;
    }

    private ClassInfo ReflectionDefinitionOwner(Module module, TypeDefinitionHandle handle, TypeDesc[]? tuple)
    {
        var td = module.Reader.GetTypeDefinition(handle);
        if (tuple is null)
        {
            tuple = new TypeDesc[td.GetGenericParameters().Count];
            for (int i = 0; i < tuple.Length; i++)
                tuple[i] = TypeDesc.MakeGenericVar(i, false);
        }
        foreach (var existing in _reflectionMetadataViews)
            if (existing.Module == module && existing.Handle == handle
                && TypeArgsComparer.Instance.Equals(existing.Context.TypeArgs, tuple))
                return existing;
        var reader = module.Reader;
        string name = StripArity(reader.GetString(td.Name));
        for (var declaring = td.GetDeclaringType(); !declaring.IsNil;)
        {
            var parent = reader.GetTypeDefinition(declaring);
            name = StripArity(reader.GetString(parent.Name)) + "_" + name;
            declaring = parent.GetDeclaringType();
        }
        name += "_" + string.Join("_", tuple.Select(MangleArg));
        string? baseName = td.BaseType.IsNil ? null : td.BaseType.Kind switch
        {
            HandleKind.TypeReference => RawSignatureProvider.TypeReferenceName(reader, (TypeReferenceHandle)td.BaseType),
            HandleKind.TypeDefinition => RawSignatureProvider.TypeDefinitionName(reader, (TypeDefinitionHandle)td.BaseType),
            _ => null,
        };
        var owner = new ClassInfo
        {
            Module = module, Handle = handle, Namespace = reader.GetString(td.Namespace), Name = name,
            CppNamePrefix = GenericTypeCppPrefix(module, handle),
            IsValueType = baseName is "System.ValueType" or "System.Enum", IsEnum = baseName == "System.Enum",
            IsAbstract = (td.Attributes & TypeAttributes.Abstract) != 0, IsSealed = (td.Attributes & TypeAttributes.Sealed) != 0,
            GenericArity = tuple.Length, Context = new GenericContext(tuple, Array.Empty<TypeDesc>()),
            ShapeCompleted = true, MembersCompleted = true,
            IsInterface = (td.Attributes & TypeAttributes.Interface) != 0,
        };
        _reflectionMetadataViews.Add(owner);
        return owner;
    }

    /// <summary>Only an actual lookup or base-chain walk expands a metadata identity.
    /// Signature arguments can nest the declaring type without expanding its base again.</summary>
    private ClassInfo CompleteReflectionDefinitionOwner(ClassInfo owner)
    {
        if (!_reflectionMetadataViews.Contains(owner) || !_reflectionDefinitionCompleted.Add(owner))
            return EnsureShape(owner);
        var module = owner.Module;
        var reader = module.Reader;
        var td = reader.GetTypeDefinition(owner.Handle);
        if (!td.BaseType.IsNil)
        {
            var provider = new ReflectionDefinitionSignatureProvider(this);
            var baseType = td.BaseType.Kind == HandleKind.TypeSpecification
                ? reader.GetTypeSpecification((TypeSpecificationHandle)td.BaseType).DecodeSignature(provider, owner.Context)
                : ResolveTypeTokenForScan(module, td.BaseType, owner.Context);
            owner.BaseClass = baseType is { Kind: TypeKind.Class } ? baseType.Class : null;
            owner.ExternalBaseName = baseType is { Kind: TypeKind.External } ? baseType.ExternalName : null;
        }
        foreach (var ih in td.GetInterfaceImplementations())
        {
            var handle = reader.GetInterfaceImplementation(ih).Interface;
            var type = handle.Kind == HandleKind.TypeSpecification
                ? reader.GetTypeSpecification((TypeSpecificationHandle)handle)
                    .DecodeSignature(new ReflectionDefinitionSignatureProvider(this), owner.Context)
                : ResolveTypeTokenForScan(module, handle, owner.Context);
            if (type is { Kind: TypeKind.Class, Class: { } itf })
                owner.Interfaces.Add(CompleteReflectionDefinitionOwner(itf));
            else if (type is { Kind: TypeKind.External, ExternalName: { } name })
                owner.ExternalInterfaceNames.Add(name);
        }
        foreach (var mh in td.GetMethods())
        {
            var md = reader.GetMethodDefinition(mh);
            owner.Methods.Add(new MethodInfo
            {
                DeclaringClass = owner, Module = module, Handle = mh, Name = reader.GetString(md.Name),
                Attributes = md.Attributes, ImplAttributes = md.ImplAttributes, Context = owner.Context,
            });
        }
        foreach (var fh in td.GetFields())
        {
            var fd = reader.GetFieldDefinition(fh);
            owner.Fields.Add(new FieldInfo
            {
                DeclaringClass = owner, Handle = fh, Name = reader.GetString(fd.Name),
                IsStatic = (fd.Attributes & FieldAttributes.Static) != 0,
                IsLiteral = (fd.Attributes & FieldAttributes.Literal) != 0,
                Attributes = fd.Attributes,
            });
        }
        return owner;
    }

    /// <summary>Generic signature types remain detached metadata views too: reading an
    /// open member never creates an executable closed tuple as a side effect.</summary>
    private sealed class ReflectionDefinitionSignatureProvider : ISignatureTypeProvider<TypeDesc, object?>
    {
        private readonly Compilation _compilation;
        private SignatureProvider Regular => _compilation.SigProvider;
        public ReflectionDefinitionSignatureProvider(Compilation compilation) => _compilation = compilation;
        public TypeDesc GetGenericInstantiation(TypeDesc type, ImmutableArray<TypeDesc> args)
        {
            if (type.Kind != TypeKind.Template)
                return Regular.GetGenericInstantiation(type, args);
            var tuple = args.ToArray();
            return TypeDesc.MakeClass(_compilation.InstancesFor(type.TemplateModule!, type.TemplateHandle).TryGetValue(tuple, out var known)
                ? known : _compilation.ReflectionDefinitionOwner(type.TemplateModule!, type.TemplateHandle, tuple));
        }
        public TypeDesc GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte kind) => Regular.GetTypeFromDefinition(reader, handle, kind);
        public TypeDesc GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte kind) => Regular.GetTypeFromReference(reader, handle, kind);
        public TypeDesc GetTypeFromSpecification(MetadataReader reader, object? context, TypeSpecificationHandle handle, byte kind) => reader.GetTypeSpecification(handle).DecodeSignature(this, context);
        public TypeDesc GetGenericMethodParameter(object? context, int index) => Regular.GetGenericMethodParameter(context, index);
        public TypeDesc GetGenericTypeParameter(object? context, int index) => Regular.GetGenericTypeParameter(context, index);
        public TypeDesc GetPrimitiveType(PrimitiveTypeCode code) => Regular.GetPrimitiveType(code);
        public TypeDesc GetSZArrayType(TypeDesc element) => Regular.GetSZArrayType(element);
        public TypeDesc GetArrayType(TypeDesc element, ArrayShape shape) => Regular.GetArrayType(element, shape);
        public TypeDesc GetByReferenceType(TypeDesc element) => Regular.GetByReferenceType(element);
        public TypeDesc GetPointerType(TypeDesc element) => Regular.GetPointerType(element);
        public TypeDesc GetFunctionPointerType(MethodSignature<TypeDesc> signature) => Regular.GetFunctionPointerType(signature);
        public TypeDesc GetModifiedType(TypeDesc modifier, TypeDesc type, bool required) => type;
        public TypeDesc GetPinnedType(TypeDesc type) => type;
    }

    private MethodSignature<TypeDesc> ReflectionSignature(MethodInfo method)
    {
        if (!method.SignatureReady && _reflectionMetadataViews.Contains(method.DeclaringClass))
            method.Signature = method.Module.Reader.GetMethodDefinition(method.Handle)
                .DecodeSignature(new ReflectionDefinitionSignatureProvider(this), method.Context);
        return method.Signature;
    }

    private MethodSignature<TypeDesc> ReflectionPropertySignature(ClassInfo owner, PropertyDefinition property) =>
        _reflectionMetadataViews.Contains(owner)
            ? property.DecodeSignature(new ReflectionDefinitionSignatureProvider(this), owner.Context)
            : property.DecodeSignature(SigProvider, owner.Context);

    /// <summary>A lookup keeps its selection policy on metadata views, runtime templates
    /// and compiled owners. Named delegate binding searches every base visibility.</summary>
    private List<REntity> SelectLookupMembers(ClassInfo owner, TemplateLookup lookup)
    {
        if (lookup.NamedBinding)
        {
            var selected = new List<REntity>();
            bool isStatic = (lookup.Flags & ReflectionApis.Static) != 0;
            foreach (var cls in BaseChain(owner))
            {
                EnsureCompleted(cls);
                foreach (var method in cls.Methods.ToList())
                    if (method.IsStatic == isStatic
                        && ReflectionLookupRules.NameMatches(method.Name, lookup.Name!, lookup.Flags)
                        && (lookup.Arity < 0 || ReflectionSignature(method).ParameterTypes.Length == lookup.Arity))
                        selected.Add(new REntity { Kind = REntityKind.Method, Method = method, Owner = cls });
            }
            return selected;
        }
        var members = SelectMembers(owner, lookup.Kind, lookup.Name, lookup.Flags, lookup.Types, lookup.Arity,
            lookup.AllowPrefix, lookup.Returns);
        if (lookup.MostDerived)
        {
            if (lookup.Kind == LookupMemberKind.Method && lookup.Types is null)
                KeepMostDerivedMethod(members);
            else if (lookup.Kind == LookupMemberKind.Field)
                KeepMostDerivedField(members);
        }
        return members;
    }

    private List<REntity> DefinitionMembers(Module module, TypeDefinitionHandle def, TypeDesc[]? tuple, TemplateLookup lookup)
    {
        var members = SelectLookupMembers(ReflectionDefinitionOwner(module, def, tuple), lookup);
        var result = new List<REntity>();
        foreach (var member in members)
            result.Add(member.Owner is { } owner && _reflectionMetadataViews.Contains(owner)
                ? new REntity { Kind = REntityKind.DefinitionMember, Metadata = member } : member);
        return result;
    }

    /// <summary>A template's closed base is looked up through the derived definition,
    /// so hidden base rows do not become independent alternatives.</summary>
    private List<REntity> LookupOwnerMembers(RResolved owners, REntity entity, ClassInfo cls, TemplateLookup lookup)
    {
        if (!entity.Inherited)
            return SelectLookupMembers(cls, lookup);
        var result = new List<REntity>();
        foreach (var owner in owners.Items)
            if (owner is { Kind: REntityKind.Template, Assembly: { } module, Tuple: { } tuple }
                && ClosedTemplateBase(module, owner.Definition, tuple) == cls)
                foreach (var row in DefinitionMembers(module, owner.Definition, tuple, lookup))
                    if (row.Kind != REntityKind.DefinitionMember)
                        result.Add(row);
        return result;
    }

    private List<REntity> TemplateMetadataMembers(REntity e)
    {
        if (e.Assembly is not { } module || e.Lookup is not { } lookup)
            return new List<REntity>();
        var members = DefinitionMembers(module, e.Definition, e.Tuple, lookup);
        if (e.Number == 0)
            return members;
        var accessors = new List<REntity>();
        foreach (var member in members)
            foreach (var accessor in AccessorsOf(member.Metadata ?? member, (ReflectionAccessor)e.Number, e.PublicAccessors))
                accessors.Add(new REntity { Kind = REntityKind.DefinitionMember,
                    Metadata = new REntity { Kind = REntityKind.Method, Method = accessor, Owner = accessor.DeclaringClass } });
        return accessors;
    }

    /// <summary>Selects the runtime-instantiation template of a definition a site closes
    /// over arguments no compiled instantiation is known to match. The site reports
    /// ClosedGeneric unless planning keeps an eligible template for the definition: without
    /// one the runtime serves only the instantiations the image already carries.</summary>
    private void SelectTemplate(ReflectionApplication app, int site, Module module, TypeDefinitionHandle def, bool report,
        string reason, TypeDesc[]? tuple)
    {
        _makeGenericTypeUsed = true;
        string name = BacktickFullName(module, def);
        if (SharedGenericsEnabled && _reflectionTemplateSelections.Add(name) && _typeofOpenGenericDefSeen.Add(name))
            _typeofOpenGenericDefNames.Add(name);
        if (!_templateRequests.TryGetValue(name, out var request))
            _templateRequests[name] = request = new TemplateRequest { Module = module, Definition = def };
        request.Add(tuple);
        if (report && site >= 0)
            RecordTemplateDiagnostic(app, app.Summary.Sites[site], name,
                reason + ", and " + name + " has no runtime-instantiation template; only instantiations the program names resolve",
                module, def, tuple);
    }

    /// <summary>The tuples recognized MakeGenericType sites ask each selected template for,
    /// by backtick name.</summary>
    private readonly Dictionary<string, TemplateRequest> _templateRequests = new(StringComparer.Ordinal);

    /// <summary>What sites ask of each selected template's clone, by backtick name.</summary>
    private readonly Dictionary<string, List<TemplateSelection>> _templateMemberSelections = new(StringComparer.Ordinal);

    /// <summary>Whether the template of <paramref name="defName"/> still serves a tuple no
    /// compiled instantiation covers.</summary>
    private bool TemplateStillRequested(string defName) =>
        _templateRequests.TryGetValue(defName, out var request) && TemplateStillRequested(request);

    /// <summary>A site asks a template's clone (<paramref name="e"/>) to run the members
    /// <paramref name="lookup"/> selects, or to be constructed. Planning reaches only what
    /// such sites ask (<see cref="ReachAskedTemplateMembers"/>). A known tuple whose request
    /// the template cannot run (<see cref="TemplateMayServe"/>) instantiates statically
    /// instead when its constraints are proved, and the request selects on that class,
    /// which every site naming the tuple then resolves to. An unproved tuple keeps the
    /// template's refusal and reports why no static body can be admitted.</summary>
    private void SelectTemplateMember(REntity e, TemplateLookup lookup, ReflectionAccessor accessor, bool construct,
        int arguments, ReflectionApplication app, bool publicAccessors = false)
    {
        var caller = app.Method;
        if (e.Assembly is not { } module)
            return;
        var def = e.Definition;
        if (e.Tuple is { } tuple && !TemplateMayServe(module, def, lookup, accessor, construct, arguments, publicAccessors))
        {
            if (StaticReflectionType(module, def, tuple) is not { } cls)
            {
                RecordReflectionDiagnostic(app, app.Summary.Sites[app.Site], ReflectionDiagnosticKind.ClosedGeneric,
                    "the constraints of " + BacktickFullName(module, def)
                    + " over these type arguments cannot be proved, and its runtime-instantiation template cannot serve this member"
                    + "; only instantiations the program names resolve", null, null, null);
                return;
            }
            NoteTypeIdentityClosure(TypeDesc.MakeClass(cls));
            if (construct)
            {
                foreach (var c in SelectMembers(cls, LookupMemberKind.Constructor, null, lookup.Flags | ReflectionApis.Instance,
                             lookup.Types, -1))
                    if (c.Method is { } ctor && (arguments < 0 || ctor.Signature.ParameterTypes.Length == arguments))
                        ConstructSelected(ctor, caller);
            }
            else
                foreach (var member in SelectLookupMembers(cls, lookup))
                    if (arguments < 0 || member.Method is { } method && method.Signature.ParameterTypes.Length == arguments)
                        InvokeSelected(member, app, accessor, publicAccessors);
            return;
        }
        string name = BacktickFullName(module, def);
        if (!_templateMemberSelections.TryGetValue(name, out var selections))
            _templateMemberSelections[name] = selections = new List<TemplateSelection>();
        var selection = new TemplateSelection(lookup, accessor, construct, arguments, e.Tuple, publicAccessors);
        foreach (var s in selections)
            if (s.Lookup == lookup && s.Accessor == accessor && s.Construct == construct && s.Arguments == arguments
                && s.PublicAccessors == publicAccessors
                && (s.Tuple is null ? selection.Tuple is null
                    : selection.Tuple is not null && TypeArgsComparer.Instance.Equals(s.Tuple, selection.Tuple)))
                return;
        selections.Add(selection);
    }

    /// <summary>Whether a clone of <paramref name="def"/> can run what a site asks of it,
    /// read from metadata before any template exists: each body the request selects on the
    /// placeholder levels, and for a construction every virtual body a dispatch through the
    /// clone can enter, uses the type parameters only as <see cref="IsReflectedTemplateBody"/>
    /// admits. The planning verdict stays the authority; this test only keeps a request the
    /// template could never serve from relying on it.</summary>
    private bool TemplateMayServe(Module module, TypeDefinitionHandle def, TemplateLookup lookup, ReflectionAccessor accessor,
        bool construct, int arguments, bool publicAccessors)
    {
        try
        {
            var levels = TemplateLevelDefinitions(module, def);
            var visiting = new HashSet<(Module, MethodDefinitionHandle)>();
            var selected = new HashSet<(Module, MethodDefinitionHandle)>();
            var members = construct
                ? SelectMembers(ReflectionDefinitionOwner(module, def, null), LookupMemberKind.Constructor, null,
                    lookup.Flags | ReflectionApis.Instance, lookup.Types, -1)
                : DefinitionMembers(module, def, null, lookup);
            foreach (var member in members)
            {
                var row = member.Metadata ?? member;
                if (row.Kind == REntityKind.Method && row.Method is { } method)
                {
                    if (arguments < 0 || ReflectionSignature(method).ParameterTypes.Length == arguments)
                        selected.Add((method.Module, method.Handle));
                }
                else if (row.Kind is REntityKind.Property or REntityKind.Event)
                    foreach (var accessorMethod in AccessorsOf(row, accessor == ReflectionAccessor.None ? ReflectionAccessor.All : accessor,
                                 publicAccessors))
                        selected.Add((accessorMethod.Module, accessorMethod.Handle));
            }
            foreach (var (m, h) in levels)
            {
                var reader = m.Reader;
                var td = reader.GetTypeDefinition(h);
                foreach (var mh in td.GetMethods())
                {
                    var md = reader.GetMethodDefinition(mh);
                    bool asked = selected.Contains((m, mh));
                    bool dispatched = construct && (md.Attributes & (MethodAttributes.Virtual | MethodAttributes.Abstract))
                        == MethodAttributes.Virtual;
                    if ((asked || dispatched) && !TemplateBodyMayServe(m, mh, levels, dispatched && !asked, visiting))
                        return false;
                }
            }
            return true;
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            return false;
        }
    }

    /// <summary>The placeholder levels of <paramref name="def"/>'s template, derived-first:
    /// the definition and each base that projects its type parameters
    /// (<see cref="RuntimeTemplateMayServe"/> admitted only bare projections).</summary>
    private List<(Module Module, TypeDefinitionHandle Handle)> TemplateLevelDefinitions(Module module, TypeDefinitionHandle def)
    {
        var levels = new List<(Module, TypeDefinitionHandle)> { (module, def) };
        for (var (m, h) = (module, def); ;)
        {
            var baseHandle = m.Reader.GetTypeDefinition(h).BaseType;
            if (baseHandle.Kind != HandleKind.TypeSpecification
                || !m.Reader.GetTypeSpecification((TypeSpecificationHandle)baseHandle).DecodeSignature(ParameterMentionProbe.Instance, null)
                || TypeDefinitionOf(m, baseHandle) is not ({ } nm, var nh))
                return levels;
            levels.Add((nm, nh));
            (m, h) = (nm, nh);
        }
    }

    /// <summary>The metadata half of <see cref="IsReflectedTemplateBody"/> for method
    /// <paramref name="h"/> of a placeholder level. A <paramref name="dispatched"/> body is
    /// entered through a slot, so a generic virtual one qualifies over closed method
    /// arguments.</summary>
    private bool TemplateBodyMayServe(Module m, MethodDefinitionHandle h, List<(Module Module, TypeDefinitionHandle Handle)> levels,
        bool dispatched, HashSet<(Module, MethodDefinitionHandle)> visiting)
    {
        var reader = m.Reader;
        var md = reader.GetMethodDefinition(h);
        if (md.RelativeVirtualAddress == 0 || md.GetGenericParameters().Count > 0 && !dispatched)
            return false;
        if (!visiting.Add((m, h)))
            return true;
        if (ReflectsThroughContext(m, h, md))
            return false;
        var signature = md.DecodeSignature(TemplateAbiProbe.Instance, null);
        int mentions = signature.ReturnType;
        foreach (int p in signature.ParameterTypes)
            mentions |= p;
        if ((mentions & TemplateAbiProbe.Value) != 0)
            return false;
        foreach (var insn in ILDecoder.Decode(m.PE.GetMethodBody(md.RelativeVirtualAddress).GetILBytes()!.ToImmutableArrayCompat()))
        {
            if ((mentions & TemplateAbiProbe.ReferenceToBare) != 0 && insn.OpCode is ILOpCode.Ldind_ref or ILOpCode.Stind_ref)
                return false;
            if (insn.Token == 0 || insn.OpCode is ILOpCode.Ldstr or ILOpCode.Ldfld or ILOpCode.Stfld or ILOpCode.Ldflda)
                continue;
            var token = SRME.EntityHandle(insn.Token);
            if (ClassTypeParameters.Of(reader, token) == 0)
                continue;
            if (insn.OpCode == ILOpCode.Calli)
                return false;
            if (insn.OpCode is ILOpCode.Ldtoken or ILOpCode.Isinst or ILOpCode.Castclass
                    or ILOpCode.Unbox_any or ILOpCode.Box or ILOpCode.Newarr
                && ClassTypeParameters.IsBare(reader, token))
                continue;
            if (insn.OpCode is ILOpCode.Call or ILOpCode.Ldftn && TemplateLevelCallees(m, token, levels) is { Count: > 0 } callees)
            {
                bool all = true;
                foreach (var (cm, ch) in callees)
                    all &= TemplateBodyMayServe(cm, ch, levels, false, visiting);
                if (all)
                    continue;
            }
            return false;
        }
        return true;
    }

    /// <summary>Whether body <paramref name="h"/> holds a reflection site that runs, binds,
    /// constructs or instantiates what an operand names while that operand depends on the
    /// class type parameters or is not known. A clone's body is applied only at its
    /// placeholder level, where such a site admits and reports nothing; a static
    /// instantiation's exact context resolves and reports it.</summary>
    private static bool ReflectsThroughContext(Module m, MethodDefinitionHandle h, MethodDefinition md)
    {
        if (!m.ReflectionSummaries.TryGetValue(h, out var summary))
        {
            m.ReflectionSummarizer ??= new ReflectionSummarizer(m.Reader, m.PE);
            var body = m.PE.GetMethodBody(md.RelativeVirtualAddress);
            summary = m.ReflectionSummarizer.Summarize(h, body, ILDecoder.Decode(body.GetILBytes()!.ToImmutableArrayCompat()));
        }
        var reader = m.Reader;
        var seen = new HashSet<int>();
        bool Depends(RValue value)
        {
            if (value.Unknown >= 0)
                return true;
            for (int i = 0; i < value.Count; i++)
            {
                int id = value.Alternatives![i];
                if (!seen.Add(id))
                    continue;
                var t = summary.Terms[id];
                if (t.Kind == TermKind.Set && t.Number >= 0
                    || t.Token != 0 && t.Kind is TermKind.TypeToken or TermKind.MethodSpecArg or TermKind.MemberToken
                        or TermKind.ExactObject or TermKind.Closure
                    && ClassTypeParameters.Of(reader, SRME.EntityHandle(t.Token)) != 0)
                    return true;
                foreach (int child in t.Children)
                    if (Depends(summary.ValueOf(child)))
                        return true;
            }
            return false;
        }
        RValue Slot(RSite site, sbyte slot) =>
            slot >= 0 ? (slot < site.Operands.Length ? site.Operands[slot] : RValue.None)
            : slot == ReflectionApi.Receiver ? site.Receiver
            : slot == ReflectionApi.MethodTypeArgument ? site.TypeArgument : RValue.None;
        foreach (var site in summary.Sites)
        {
            var api = site.Api;
            if (api.Kind is not (ReflectionOpKind.Execute or ReflectionOpKind.Bind or ReflectionOpKind.Construct
                    or ReflectionOpKind.Instantiate or ReflectionOpKind.AttributeInstances))
                continue;
            if (!api.Recognized || Depends(site.TypeArgument))
                return true;
            foreach (sbyte slot in new[] { api.Target, api.Name, api.AssemblyName, api.Flags, api.Types, api.DelegateType,
                         api.AttributeType, api.IgnoreCase })
                if (slot != ReflectionApi.Absent && Depends(Slot(site, slot)))
                    return true;
        }
        return false;
    }

    /// <summary>The non-virtual methods of a placeholder level a call or ldftn token may
    /// name, matched by name and parameter count over a bare-parameter instantiation;
    /// empty when the token names anything else.</summary>
    private List<(Module, MethodDefinitionHandle)> TemplateLevelCallees(Module m, EntityHandle token,
        List<(Module Module, TypeDefinitionHandle Handle)> levels)
    {
        var found = new List<(Module, MethodDefinitionHandle)>();
        if (token.Kind != HandleKind.MemberReference)
            return found;
        var reader = m.Reader;
        var reference = reader.GetMemberReference((MemberReferenceHandle)token);
        if (reference.Parent.Kind != HandleKind.TypeSpecification
            || TypeDefinitionOf(m, reference.Parent) is not ({ } tm, var th) || !levels.Contains((tm, th)))
            return found;
        var blob = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
        blob.ReadSignatureTypeCode();
        blob.ReadSignatureTypeCode();
        blob.ReadTypeHandle();
        for (int i = blob.ReadCompressedInteger(); i > 0; i--)
        {
            if (blob.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeParameter)
                return found;
            blob.ReadCompressedInteger();
        }
        string name = reader.GetString(reference.Name);
        int count = reference.DecodeMethodSignature(ParameterMentionProbe.Instance, null).ParameterTypes.Length;
        var target = tm.Reader;
        foreach (var mh in target.GetTypeDefinition(th).GetMethods())
        {
            var md = target.GetMethodDefinition(mh);
            if (target.GetString(md.Name) != name
                || md.DecodeSignature(ParameterMentionProbe.Instance, null).ParameterTypes.Length != count)
                continue;
            if ((md.Attributes & MethodAttributes.Virtual) != 0)
                return new List<(Module, MethodDefinitionHandle)>();
            found.Add((tm, mh));
        }
        return found;
    }

    /// <summary>How a signature type mentions class type parameters: <see cref="Value"/>
    /// when a parameter's value crosses the boundary, <see cref="Bare"/> for a parameter
    /// itself, <see cref="ReferenceToBare"/> for an address of one.</summary>
    private sealed class TemplateAbiProbe : ISignatureTypeProvider<int, object?>
    {
        internal const int Value = 1, Bare = 2, ReferenceToBare = 4;
        internal static readonly TemplateAbiProbe Instance = new();

        public int GetArrayType(int elementType, ArrayShape shape) => elementType & (Value | ReferenceToBare);
        public int GetByReferenceType(int elementType) => (elementType & Bare) != 0 ? ReferenceToBare : elementType & ReferenceToBare;
        public int GetFunctionPointerType(MethodSignature<int> signature) => 0;
        public int GetGenericInstantiation(int genericType, ImmutableArray<int> typeArguments)
        {
            int mentions = 0;
            foreach (int a in typeArguments)
                mentions |= a & (Value | ReferenceToBare);
            return mentions;
        }
        public int GetGenericMethodParameter(object? genericContext, int index) => 0;
        public int GetGenericTypeParameter(object? genericContext, int index) => Value | Bare;
        public int GetModifiedType(int modifier, int unmodifiedType, bool isRequired) => unmodifiedType;
        public int GetPinnedType(int elementType) => elementType;
        public int GetPointerType(int elementType) => GetByReferenceType(elementType);
        public int GetPrimitiveType(PrimitiveTypeCode typeCode) => 0;
        public int GetSZArrayType(int elementType) => elementType & (Value | ReferenceToBare);
        public int GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => 0;
        public int GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => 0;
        public int GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle,
            byte rawTypeKind) => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
    }

    /// <summary>Why a Type[] operand is not a finite set of closed argument tuples, or null
    /// when every element is known and some resolves to no closed type: a generic parameter
    /// or definition the runtime closes symbolically, or a name nothing defines. A clone is
    /// closed, so one no exact instantiation stands for is a reason.</summary>
    private static string? ArgumentsUnknown(RResolved args)
    {
        if (args.Unknown is not null)
            return args.Unknown;
        bool open = false;
        foreach (var e in args.Items)
        {
            if (e.Kind != REntityKind.TypeArray || e.Number < 0 || e.Elements is not { } elements)
                return "an array whose length or elements the summary does not know";
            foreach (var element in elements)
            {
                if (element.Unknown is not null)
                    return element.Unknown;
                if (element.Items.Exists(alt => alt.Kind == REntityKind.Template))
                    return "a type a runtime-instantiation template mints";
                open |= element.Items.Count == 0 || element.Items.Exists(alt => alt.Kind != REntityKind.Type);
            }
        }
        return open ? null : "more argument combinations than the summary follows";
    }

    /// <summary>Whether closed arguments can instantiate <paramref name="parameters"/>, the
    /// generic parameters of a type (arguments in <paramref name="typeArgs"/>) or of a
    /// method (arguments in <paramref name="methodArgs"/>): true when each is a legal generic
    /// argument meeting its constraints, false when one definitely is not (.NET throws
    /// ArgumentException), null when the model cannot decide.</summary>
    private bool? GenericArgumentsAdmit(Module module, GenericParameterHandleCollection parameters,
        TypeDesc[] typeArgs, TypeDesc[] methodArgs, bool metadata = false)
    {
        var args = methodArgs.Length > 0 ? methodArgs : typeArgs;
        if (parameters.Count != args.Length)
            return false;
        var context = new GenericContext(typeArgs, methodArgs);
        bool? verdict = true;
        int index = 0;
        foreach (var handle in parameters)
        {
            bool? one = GenericArgumentAdmits(module, module.Reader.GetGenericParameter(handle), args[index++], context, metadata);
            if (one == false)
                return false;
            if (one is null)
                verdict = null;
        }
        return verdict;
    }

    private bool? GenericArgumentAdmits(Module module, GenericParameter parameter, TypeDesc argument, GenericContext context,
        bool metadata)
    {
        var a = PrimitiveBacked(argument);
        switch (a.Kind)
        {
            case TypeKind.Primitive when a.Primitive is PrimitiveTypeCode.Void or PrimitiveTypeCode.TypedReference:
            case TypeKind.ByRef or TypeKind.Pointer:
            case TypeKind.Class when a.Class?.FullName is "System.Void" or "System.TypedReference":
                return false;
            case TypeKind.Primitive or TypeKind.SZArray or TypeKind.MDArray:
                break;
            case TypeKind.Class when a.Class is not null:
                break;
            default:
                return null;
        }
        var cls = a.Kind == TypeKind.Class ? EnsureShape(a.Class!) : null;
        const GenericParameterAttributes allowByRefLike = (GenericParameterAttributes)0x0020;
        if (cls is { IsByRefLike: true } && (parameter.Attributes & allowByRefLike) == 0)
            return false;
        var special = parameter.Attributes & GenericParameterAttributes.SpecialConstraintMask;
        if (special == 0 && parameter.GetConstraints().Count == 0)
            return true;
        if (!BinderDecides(a))
            return null;
        bool value = IsBinderValueType(a);
        bool nullable = cls is not null && value && GenericDefFullName(cls) == "System.Nullable";
        if ((special & GenericParameterAttributes.ReferenceTypeConstraint) != 0 && value)
            return false;
        if ((special & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0 && (!value || nullable))
            return false;
        if ((special & GenericParameterAttributes.DefaultConstructorConstraint) != 0 && !value)
        {
            // Object meets new() and still meets the type constraints below or not.
            if (cls is null)
            {
                if (!a.IsObject)
                    return false;
            }
            else if (cls.Handle.IsNil)
                return null;
            else if (cls.IsAbstract || cls.IsInterface || !HasPublicParameterlessCtor(cls))
                return false;
        }
        bool? verdict = true;
        foreach (var handle in parameter.GetConstraints())
        {
            TypeDesc? resolved;
            try
            {
                var type = module.Reader.GetGenericParameterConstraint(handle).Type;
                resolved = metadata && type.Kind == HandleKind.TypeSpecification
                    ? module.Reader.GetTypeSpecification((TypeSpecificationHandle)type)
                        .DecodeSignature(new ReflectionDefinitionSignatureProvider(this), context)
                    : ResolveTypeTokenForScan(module, type, context);
            }
            catch (NotSupportedException e) when (!IsMustEscape(e))
            {
                resolved = null;
            }
            if (resolved is null)
            {
                verdict = null;
                continue;
            }
            var constraint = PrimitiveBacked(resolved);
            if (metadata && ContainsReflectionMetadataView(constraint))
            {
                verdict = null;
                continue;
            }
            if (constraint.Kind == TypeKind.External && ResolveExternalClass(constraint) is { } external)
                constraint = TypeDesc.MakeClass(external);
            if (!BinderDecides(constraint))
            {
                verdict = null;
                continue;
            }
            if (BinderSame(a, constraint) || constraint.IsObject)
                continue;
            bool? admitted = AssignableKnown(constraint, a);
            if (admitted == false)
                return false;
            if (admitted is null)
                verdict = null;
        }
        return verdict;
    }

    private static bool HasPublicParameterlessCtor(ClassInfo cls)
    {
        var reader = cls.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(cls.Handle).GetMethods())
        {
            var md = reader.GetMethodDefinition(handle);
            if ((md.Attributes & (MethodAttributes.Static | MethodAttributes.MemberAccessMask)) != MethodAttributes.Public
                || !reader.StringComparer.Equals(md.Name, ".ctor"))
                continue;
            var blob = reader.GetBlobReader(md.Signature);
            blob.ReadSignatureHeader();
            if (blob.ReadCompressedInteger() == 0)
                return true;
        }
        return false;
    }

    private static string BacktickFullName(Module module, TypeDefinitionHandle def)
        => RawSignatureProvider.TypeDefinitionName(module.Reader, def);

    /// <summary>Re-applies the sites whose generic definition gained an instantiation since
    /// the last visit. Driven by discovery's fixpoint.</summary>
    internal bool ApplyPendingReflectionSelections()
    {
        if (_pendingReflectionSelections.Count == 0)
        {
            _pendingSelectionClassCursor = Classes.Count;
            return false;
        }
        int reached = Reachable.Count;
        bool applied = false;
        while (_pendingSelectionClassCursor < Classes.Count)
        {
            var cls = Classes[_pendingSelectionClassCursor++];
            if (cls.GenericArity == 0 || ContainsCanonPlaceholder(cls) || ContainsGenericVar(cls))
                continue;
            int token = SRME.GetToken(cls.Handle);
            foreach (var pending in _pendingReflectionSelections.ToList())
            {
                if (pending.Module != cls.Module.Index || pending.Definition != token)
                    continue;
                var m = pending.Method;
                if (!m.Module.ReflectionSummaries.TryGetValue(m.Handle, out var summary))
                    continue;
                var app = new ReflectionApplication { Method = m, Summary = summary, Canonical = ContextIsCanonical(m.Context) };
                try
                {
                    ApplySite(app, pending.Site);
                }
                catch (NotSupportedException e) when (!IsMustEscape(e))
                {
                }
                applied = true;
            }
        }
        if (applied)
            DrainReachability();
        return Reachable.Count != reached;
    }

    /// <summary>The finite closed argument tuples among a <c>Type[]</c> operand's
    /// alternatives; <paramref name="known"/> says whether they cover every successful
    /// alternative. Null arrays and elements throw and contribute no tuple. With
    /// <paramref name="closeTemplates"/>,
    /// as a site that forms a type or method over the arguments asks, a clone over a known
    /// tuple stands for its exact instantiation (<see cref="ExactTemplateType"/>).</summary>
    private List<TypeDesc[]> ArgumentTuples(RResolved args, out bool known, bool closeTemplates = false)
    {
        var tuples = new List<TypeDesc[]>();
        known = args.Unknown is null && args.Items.Count > 0;
        foreach (var e in args.Items)
        {
            if (e.Kind == REntityKind.Null)
                continue;
            if (e.Kind != REntityKind.TypeArray || e.Number < 0 || e.Elements is not { } elements)
            {
                known = false;
                continue;
            }
            var partial = new List<TypeDesc[]> { Array.Empty<TypeDesc>() };
            foreach (var element in elements)
            {
                if (element.Unknown is not null)
                    known = false;
                var types = new List<TypeDesc>();
                foreach (var alt in element.Items)
                {
                    // A clone's closed base stands for no type of its own.
                    if (alt.Inherited || alt.Kind == REntityKind.Null)
                        continue;
                    var type = alt.Kind == REntityKind.Type ? alt.Type
                        : closeTemplates && alt.Kind == REntityKind.Template ? ExactTemplateType(alt) : null;
                    if (type is null)
                    {
                        known = false;
                        continue;
                    }
                    type = SignatureForm(type);
                    if (!types.Exists(existing => TypeArgsComparer.ElementEquals(existing, type)))
                        if (types.Count < ReflectionSummarizer.MaxAlternatives)
                            types.Add(type);
                        else
                            known = false;
                }
                if (types.Count == 0)
                {
                    partial.Clear();
                    break;
                }
                var next = new List<TypeDesc[]>();
                foreach (var prefix in partial)
                    foreach (var type in types)
                    {
                        if (next.Count >= ReflectionSummarizer.MaxAlternatives)
                        {
                            known = false;
                            break;
                        }
                        var tuple = new TypeDesc[prefix.Length + 1];
                        Array.Copy(prefix, tuple, prefix.Length);
                        tuple[prefix.Length] = type;
                        next.Add(tuple);
                    }
                partial = next;
            }
            foreach (var tuple in partial)
                if (!tuples.Exists(existing => TypeArgsComparer.Instance.Equals(existing, tuple)))
                    if (tuples.Count < RResolved.MaxItems)
                        tuples.Add(tuple);
                    else
                        known = false;
        }
        return tuples;
    }

    /// <summary>The exact instantiation a clone over a known tuple stands for as a generic
    /// argument. An enclosing instantiation closes over a class, so the tuple is instantiated
    /// statically, under the normal bound, once its constraints are known to admit it; the
    /// runtime then resolves the clone's own sites to that class. Null otherwise.</summary>
    private TypeDesc? ExactTemplateType(REntity clone)
    {
        if (clone is not { Assembly: { } module, Tuple: { } tuple })
            return null;
        if (StaticReflectionType(module, clone.Definition, tuple) is not { } cls)
            return null;
        var type = TypeDesc.MakeClass(cls);
        NoteTypeIdentityClosure(type);
        return type;
    }

    /// <summary>Reuses an AOT instantiation or mints one only after its constraints are
    /// proved. A runtime template's shape or body eligibility proves no constraint.</summary>
    private ClassInfo? StaticReflectionType(Module module, TypeDefinitionHandle def, TypeDesc[] tuple)
    {
        if (InstancesFor(module, def).TryGetValue(tuple, out var cls))
            return cls;
        var parameters = module.Reader.GetTypeDefinition(def).GetGenericParameters();
        if (GenericArgumentsAdmit(module, parameters, tuple, Array.Empty<TypeDesc>()) != true)
            return null;
        return Instantiate(module, def, tuple);
    }

    // ---- member selection ----

    private readonly Dictionary<(ClassInfo Owner, MethodDefinitionHandle Handle), MethodInfo> _genericMethodDefinitions = new();

    /// <summary>The methods a lookup over <paramref name="cls"/> sees. The member list holds
    /// a generic method only as the instantiations the program made, which no lookup
    /// returns; its definition stands in for them.</summary>
    private List<MethodInfo> LookupMethods(ClassInfo cls)
    {
        var methods = new List<MethodInfo>();
        foreach (var method in cls.Methods.ToList())
            if (GenericParameterCount(method) == 0)
                methods.Add(method);
        var reader = cls.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(cls.Handle).GetMethods())
            if (reader.GetMethodDefinition(handle).GetGenericParameters().Count > 0)
                methods.Add(GenericMethodDefinition(cls, handle));
        return methods;
    }

    /// <summary>A generic method definition as reflection returns it: open over its own
    /// parameters, so it is never reached; MakeGenericMethod instantiates it.</summary>
    private MethodInfo GenericMethodDefinition(ClassInfo cls, MethodDefinitionHandle handle)
    {
        if (_genericMethodDefinitions.TryGetValue((cls, handle), out var known))
            return known;
        var reader = cls.Module.Reader;
        var md = reader.GetMethodDefinition(handle);
        var definition = new MethodInfo
        {
            DeclaringClass = cls,
            Name = reader.GetString(md.Name),
            Handle = handle,
            Module = cls.Module,
            Attributes = md.Attributes,
            ImplAttributes = md.ImplAttributes,
            Rva = md.RelativeVirtualAddress,
            Context = new GenericContext(cls.Context.TypeArgs, Array.Empty<TypeDesc>()),
        };
        _genericMethodDefinitions.Add((cls, handle), definition);
        return definition;
    }

    /// <summary>The members a lookup over <paramref name="owner"/> can return: the owner and
    /// its base chain (an interface only itself) under the RuntimeType binding rules, an
    /// override collapsed to the declaration nearest the owner. A <c>Type[]</c> operand keeps
    /// the overloads DefaultBinder.SelectMethod may pick for any alternative of it: the same
    /// arity, and each known element identical, assignable or primitive-widening to its
    /// parameter; over closed tuples, only the overload it returns
    /// (<see cref="KeepBinderChoices"/>); a property lookup's index types and
    /// <paramref name="returns"/> narrow as GetProperty does (<see cref="KeepPropertyChoices"/>).
    /// <paramref name="allowPrefix"/> reads the name as
    /// <see cref="ReflectionLookupRules.LookupName"/> does.</summary>
    private List<REntity> SelectMembers(ClassInfo owner, LookupMemberKind? kind, string? name, int flags,
        RResolved? types, int arity, bool allowPrefix = false, RResolved? returns = null)
    {
        var selected = new List<REntity>();
        CompleteReflectionDefinitionOwner(owner);
        if (owner.IntrinsicCppName is not null && owner.Handle.IsNil)
            return selected;
        bool prefix = false;
        if (name is not null)
            name = ReflectionLookupRules.LookupName(name, allowPrefix, out prefix);
        var seenSlots = new HashSet<int>();
        var overridden = new HashSet<string>(StringComparer.Ordinal);
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in owner.IsInterface ? new List<ClassInfo> { owner } : BaseChain(owner))
        {
            bool declared = c == owner;
            if (c.Handle.IsNil)
                continue;
            EnsureCompleted(c);
            if (kind is null or LookupMemberKind.Method or LookupMemberKind.Constructor)
                foreach (var method in LookupMethods(c))
                {
                    bool ctor = method.Name is ".ctor" or ".cctor";
                    var memberKind = ctor ? LookupMemberKind.Constructor : LookupMemberKind.Method;
                    if (kind is not null && kind != memberKind || method.IsSynthetic)
                        continue;
                    if (kind == LookupMemberKind.Constructor && method.Name == ".cctor" != (flags == ReflectionApis.TypeInitializerLookup))
                        continue;
                    if (name is not null && !ReflectionLookupRules.NameMatches(method.Name, name, flags, prefix))
                        continue;
                    var row = new LookupRow(method.Name, memberKind, MethodVisibility(method.Attributes), method.IsStatic);
                    if (!ReflectionLookupRules.Admits(row, flags, declared))
                        continue;
                    if (arity >= 0 && GenericParameterCount(method) != arity)
                        continue;
                    if (types is not null && !MayBindParameters(method, types))
                        continue;
                    if (method.IsVirtual && method.VtableSlot >= 0 && !c.IsInterface && !seenSlots.Add(method.VtableSlot))
                        continue;
                    if (method.IsVirtual && _reflectionMetadataViews.Contains(owner) && !c.IsInterface)
                    {
                        string key = method.Name + "\0" + ReflectionOverrideSignature(method);
                        if (overridden.Contains(key))
                            continue;
                        if (!method.IsNewSlot)
                            overridden.Add(key);
                    }
                    selected.Add(new REntity { Kind = REntityKind.Method, Method = method, Owner = c });
                }
            if (kind is null or LookupMemberKind.Field)
                foreach (var field in c.Fields.ToList())
                {
                    if (name is not null && !ReflectionLookupRules.NameMatches(field.Name, name, flags, prefix))
                        continue;
                    var visibility = field.IsPublic ? LookupVisibility.Public
                        : field.IsPrivate ? LookupVisibility.Private : LookupVisibility.NonPublic;
                    if (!ReflectionLookupRules.Admits(new LookupRow(field.Name, LookupMemberKind.Field, visibility, field.IsStatic), flags, declared))
                        continue;
                    selected.Add(new REntity { Kind = REntityKind.Field, Field = field, Owner = c });
                }
            var reader = c.Module.Reader;
            var td = reader.GetTypeDefinition(c.Handle);
            if (kind is null or LookupMemberKind.Property)
                foreach (var ph in td.GetProperties())
                {
                    var property = reader.GetPropertyDefinition(ph);
                    string propertyName = reader.GetString(property.Name);
                    if (name is not null && !ReflectionLookupRules.NameMatches(propertyName, name, flags, prefix))
                        continue;
                    var accessors = property.GetAccessors();
                    if (!AdmitsAccessorMember(c, propertyName, LookupMemberKind.Property, flags, declared, accessors.Getter, accessors.Setter))
                        continue;
                    // A nearer property hides an inherited one only by name and signature, so
                    // an inherited indexer overload stays visible.
                    if (!seenNames.Add("p:" + propertyName + "\0" + PropertySignatureKey(property)) && !declared)
                        continue;
                    if (types is not null && BindsByArity(types)
                        && !MayBindTypes(ReflectionPropertySignature(c, property).ParameterTypes, types))
                        continue;
                    selected.Add(new REntity
                    {
                        Kind = REntityKind.Property, Owner = c, Member = ph, Text = propertyName, InheritedMember = !declared,
                    });
                }
            if (kind is null or LookupMemberKind.Event)
                foreach (var eh in td.GetEvents())
                {
                    var ev = reader.GetEventDefinition(eh);
                    string eventName = reader.GetString(ev.Name);
                    if (name is not null && !ReflectionLookupRules.NameMatches(eventName, name, flags, prefix))
                        continue;
                    var accessors = ev.GetAccessors();
                    if (!AdmitsAccessorMember(c, eventName, LookupMemberKind.Event, flags, declared, accessors.Adder, accessors.Remover))
                        continue;
                    if (!declared && !seenNames.Add("e:" + eventName))
                        continue;
                    if (declared)
                        seenNames.Add("e:" + eventName);
                    selected.Add(new REntity
                    {
                        Kind = REntityKind.Event, Owner = c, Member = eh, Text = eventName, InheritedMember = !declared,
                    });
                }
            if (kind is null or LookupMemberKind.NestedType)
                selected.AddRange(SelectNestedTypes(c.Module, c.Handle, name, flags, prefix, declared));
        }
        if (types is not null && kind is LookupMemberKind.Method or LookupMemberKind.Constructor && selected.Count > 1)
            KeepBinderChoices(selected, types);
        if (kind == LookupMemberKind.Property && (types is not null || returns is not null) && selected.Count > 0)
            KeepPropertyChoices(selected, types, returns);
        return selected;
    }

    /// <summary>The nested types of <paramref name="owner"/> a lookup admits by
    /// <paramref name="name"/>, already read by <see cref="ReflectionLookupRules.LookupName"/>:
    /// generic definitions under a generic owner, as .NET returns them.</summary>
    private List<REntity> SelectNestedTypes(Module module, TypeDefinitionHandle owner, string? name, int flags, bool prefix,
        bool declared)
    {
        var selected = new List<REntity>();
        var reader = module.Reader;
        foreach (var nh in reader.GetTypeDefinition(owner).GetNestedTypes())
        {
            var nested = reader.GetTypeDefinition(nh);
            string nestedName = reader.GetString(nested.Name);
            if (name is not null && !ReflectionLookupRules.NameMatches(nestedName, name, flags, prefix))
                continue;
            var visibility = (nested.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic
                ? LookupVisibility.Public
                : (nested.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate
                    ? LookupVisibility.Private : LookupVisibility.NonPublic;
            if (!ReflectionLookupRules.Admits(new LookupRow(nestedName, LookupMemberKind.NestedType, visibility, false), flags, declared))
                continue;
            selected.Add(DefinitionEntity(module, nh));
        }
        return selected;
    }

    /// <summary>Narrows a property lookup to what RuntimeType.GetPropertyImpl returns for
    /// some alternative of its index types and return type: with no index types, the one
    /// candidate when its type is the return type exactly; otherwise each candidate
    /// DefaultBinder.SelectProperty may return, whose index parameters bind the tuple and
    /// whose type the return type converts to. An alternative the model cannot decide,
    /// and several candidates it would have to rank, keep every candidate.</summary>
    private void KeepPropertyChoices(List<REntity> selected, RResolved? types, RResolved? returns)
    {
        var tuples = new List<TypeDesc[]?>();
        if (types is null)
            tuples.Add(null);
        else
        {
            var known = ArgumentTuples(types, out bool decided);
            if (!decided)
                return;
            tuples.AddRange(known);
        }
        var returnTypes = new List<TypeDesc?>();
        if (returns is null)
            returnTypes.Add(null);
        else
        {
            if (returns.Unknown is not null)
                return;
            foreach (var e in returns.Items)
                if (e.Kind == REntityKind.Null)
                    returnTypes.Add(null);
                else if (e is { Kind: REntityKind.Type, Inherited: false, Type: { } type })
                    returnTypes.Add(type);
                else
                    return;
            if (returnTypes.Count == 0)
                return;
        }
        var signatures = new Dictionary<REntity, MethodSignature<TypeDesc>>();
        foreach (var e in selected)
            if (e.Kind == REntityKind.Property && e.Owner is { } owner)
                signatures[e] = ReflectionPropertySignature(owner,
                    owner.Module.Reader.GetPropertyDefinition((PropertyDefinitionHandle)e.Member));
        var keep = new HashSet<REntity>();
        foreach (var tuple in tuples)
            foreach (var returnType in returnTypes)
            {
                var candidates = new List<REntity>();
                foreach (var (e, signature) in signatures)
                    if (tuple is null || signature.ParameterTypes.Length == tuple.Length)
                        candidates.Add(e);
                if (candidates.Count == 0)
                    continue;
                if (tuple is null || tuple.Length == 0)
                {
                    if (candidates.Count == 1)
                    {
                        if (returnType is null || BinderSame(returnType, signatures[candidates[0]].ReturnType))
                            keep.Add(candidates[0]);
                        else if (!BinderDecides(returnType) || !BinderDecides(signatures[candidates[0]].ReturnType))
                            return;
                        continue;
                    }
                    if (returnType is null)
                        return;
                }
                var applicable = new List<REntity>();
                foreach (var e in candidates)
                {
                    var signature = signatures[e];
                    bool? binds = true;
                    for (int i = 0; tuple is not null && i < tuple.Length && binds != false; i++)
                        binds = BinderMatches(signature.ParameterTypes[i], tuple[i]) is { } one ? binds & one : null;
                    if (binds == true && returnType is not null)
                    {
                        var propertyType = PrimitiveBacked(signature.ReturnType);
                        binds = IsBinderPrimitive(propertyType)
                            ? BinderDecides(returnType) ? BinderTypeCode(PrimitiveBacked(returnType)) is { } source
                                && CanChangePrimitive(source, propertyType.Primitive) : null
                            : AssignableKnown(propertyType, returnType);
                    }
                    if (binds is null)
                        return;
                    if (binds == true)
                        applicable.Add(e);
                }
                if (applicable.Count > 1)
                    return;
                keep.UnionWith(applicable);
            }
        selected.RemoveAll(e => e.Kind == REntityKind.Property && !keep.Contains(e));
    }

    /// <summary>A singular field lookup selects the nearest declaration. Several
    /// matching rows on that owner remain ambiguous, including IgnoreCase collisions.</summary>
    private static void KeepMostDerivedField(List<REntity> selected)
    {
        if (selected.Count < 2 || selected[0] is not { Kind: REntityKind.Field, Owner: { } owner })
            return;
        for (int i = 1; i < selected.Count; i++)
            if (selected[i].Kind != REntityKind.Field || selected[i].Owner == owner)
                return;
        selected.RemoveRange(1, selected.Count - 1);
    }

    /// <summary>Narrows a name-only GetMethod to what RuntimeType.GetMethodImpl returns:
    /// candidates whose parameter types all agree resolve to the one declared nearest the
    /// owner. Differing or undecidable parameter types, or two candidates on that class,
    /// make the runtime throw AmbiguousMatchException and keep every candidate.</summary>
    private void KeepMostDerivedMethod(List<REntity> selected)
    {
        if (selected.Count < 2 || selected[0] is not { Kind: REntityKind.Method, Method: { } nearest } first)
            return;
        var parameters = ReflectionSignature(nearest).ParameterTypes;
        for (int i = 1; i < selected.Count; i++)
        {
            if (selected[i] is not { Kind: REntityKind.Method, Method: { } other } e || e.Owner == first.Owner)
                return;
            var others = ReflectionSignature(other).ParameterTypes;
            if (others.Length != parameters.Length)
                return;
            for (int p = 0; p < parameters.Length; p++)
                if (!BinderDecides(parameters[p]) || !BinderDecides(others[p]) || !BinderSame(parameters[p], others[p]))
                    return;
        }
        selected.RemoveRange(1, selected.Count - 1);
    }

    /// <summary>Narrows a <c>Type[]</c> lookup's overloads to those DefaultBinder.SelectMethod
    /// returns for some alternative of the operand: each candidate whose parameters bind the
    /// tuple, then the one more specific than every rival. An alternative the model cannot
    /// decide, or one the binder finds ambiguous (AmbiguousMatchException), keeps every
    /// overload.</summary>
    private void KeepBinderChoices(List<REntity> selected, RResolved types)
    {
        var tuples = ArgumentTuples(types, out bool known);
        if (!known)
            return;
        var keep = new HashSet<MethodInfo>();
        foreach (var tuple in tuples)
        {
            var candidates = new List<MethodInfo>();
            foreach (var e in selected)
            {
                if (e.Method is not { } method || ReflectionSignature(method).ParameterTypes.Length != tuple.Length)
                    continue;
                bool? binds = true;
                for (int i = 0; i < tuple.Length; i++)
                {
                    bool? one = BinderMatches(ReflectionSignature(method).ParameterTypes[i], tuple[i]);
                    if (one == false)
                    {
                        binds = false;
                        break;
                    }
                    if (one is null)
                        binds = null;
                }
                if (binds is null)
                    return;
                if (binds == true)
                    candidates.Add(method);
            }
            if (candidates.Count == 0)
                continue;
            if (MostSpecificMethod(candidates, tuple) is not { } chosen)
                return;
            keep.Add(chosen);
        }
        selected.RemoveAll(e => e.Method is { } method && !keep.Contains(method));
    }

    /// <summary>DefaultBinder.SelectMethod's filter of one parameter against an argument
    /// type: identity, an object parameter, primitive widening into a primitive parameter,
    /// else <c>IsAssignableFrom</c>; null when the model cannot decide.</summary>
    private bool? BinderMatches(TypeDesc parameter, TypeDesc argument)
    {
        var p = PrimitiveBacked(parameter);
        var a = PrimitiveBacked(argument);
        if (!BinderDecides(p) || !BinderDecides(a))
            return null;
        if (BinderSame(p, a) || p.IsObject)
            return true;
        if (IsBinderPrimitive(p))
            return BinderTypeCode(a) is { } source && CanChangePrimitive(source, p.Primitive);
        return AssignableKnown(p, a);
    }

    /// <summary>The unique candidate DefaultBinder.FindMostSpecificMethod ranks above every
    /// rival, or null when none is, or when the model cannot rank a pair.</summary>
    private MethodInfo? MostSpecificMethod(List<MethodInfo> candidates, TypeDesc[] tuple)
    {
        if (candidates.Count == 1)
            return candidates[0];
        foreach (var chosen in candidates)
        {
            bool dominates = true;
            foreach (var rival in candidates)
                if (rival != chosen && MoreSpecificMethod(chosen, rival, tuple) != 1)
                {
                    dominates = false;
                    break;
                }
            if (dominates)
                return chosen;
        }
        return null;
    }

    /// <summary>DefaultBinder.FindMostSpecificMethod: 1 when <paramref name="m1"/> is more
    /// specific for <paramref name="tuple"/>, 2 when <paramref name="m2"/> is, 0 when
    /// neither; equal parameter lists rank the more derived declaring type higher. Null
    /// when the model cannot decide.</summary>
    private int? MoreSpecificMethod(MethodInfo m1, MethodInfo m2, TypeDesc[] tuple)
    {
        var p1 = ReflectionSignature(m1).ParameterTypes;
        var p2 = ReflectionSignature(m2).ParameterTypes;
        bool p1Less = false, p2Less = false;
        for (int i = 0; i < tuple.Length; i++)
        {
            if (BinderSame(p1[i], p2[i]))
                continue;
            switch (MoreSpecificType(p1[i], p2[i], tuple[i]))
            {
                case null:
                    return null;
                case 0:
                    return 0;
                case 1:
                    p1Less = true;
                    break;
                case 2:
                    p2Less = true;
                    break;
            }
        }
        if (p1Less != p2Less)
            return p1Less ? 1 : 2;
        for (int i = 0; i < p1.Length; i++)
            if (!BinderSame(p1[i], p2[i]))
                return 0;
        int depth1 = BaseChain(m1.DeclaringClass).Count, depth2 = BaseChain(m2.DeclaringClass).Count;
        return depth1 == depth2 ? 0 : depth1 > depth2 ? 1 : 2;
    }

    /// <summary>DefaultBinder.FindMostSpecificType over parameter types
    /// <paramref name="c1"/> and <paramref name="c2"/> for argument type
    /// <paramref name="t"/>; null when the model cannot decide.</summary>
    private int? MoreSpecificType(TypeDesc c1, TypeDesc c2, TypeDesc t)
    {
        if (BinderSame(c1, c2))
            return 0;
        if (BinderSame(c1, t))
            return 1;
        if (BinderSame(c2, t))
            return 2;
        if (c1.Kind == TypeKind.ByRef || c2.Kind == TypeKind.ByRef)
            return null;
        c1 = PrimitiveBacked(c1);
        c2 = PrimitiveBacked(c2);
        bool? c1FromC2, c2FromC1;
        if (IsBinderPrimitive(c1) && IsBinderPrimitive(c2))
        {
            c1FromC2 = CanChangePrimitive(c2.Primitive, c1.Primitive);
            c2FromC1 = CanChangePrimitive(c1.Primitive, c2.Primitive);
        }
        else
        {
            c1FromC2 = AssignableKnown(c1, c2);
            c2FromC1 = AssignableKnown(c2, c1);
        }
        if (c1FromC2 is not { } oneFromTwo || c2FromC1 is not { } twoFromOne)
            return null;
        if (oneFromTwo == twoFromOne)
            return 0;
        return oneFromTwo ? 2 : 1;
    }

    /// <summary><c><paramref name="to"/>.IsAssignableFrom(<paramref name="from"/>)</c> where
    /// <see cref="BinderAssignable"/> answers it for certain, else null.</summary>
    private bool? AssignableKnown(TypeDesc to, TypeDesc from)
    {
        to = PrimitiveBacked(to);
        from = PrimitiveBacked(from);
        if (!BinderDecides(to) || !BinderDecides(from))
            return null;
        if (BinderSame(to, from) || to.IsObject)
            return true;
        if (to.Kind == TypeKind.Primitive)
            return false;
        if (to.Kind is TypeKind.SZArray or TypeKind.MDArray)
        {
            bool shapeMatches = from.Kind == TypeKind.MDArray
                ? to.Kind == TypeKind.MDArray && from.Rank == to.Rank
                : from.Kind == TypeKind.SZArray && (to.Kind == TypeKind.SZArray || to.Rank == 1);
            if (!shapeMatches)
                return false;
            var fromElement = PrimitiveBacked(from.Element!);
            var toElement = PrimitiveBacked(to.Element!);
            bool valueFrom = IsBinderValueType(fromElement), valueTo = IsBinderValueType(toElement);
            // Distinct value elements may still share a size the CLR equates.
            if (valueFrom || valueTo)
                return valueFrom && valueTo ? null : false;
            return AssignableKnown(toElement, fromElement);
        }
        if (to.Kind != TypeKind.Class)
            return null;
        var toClass = EnsureShape(to.Class!);
        if (toClass.IsValueType || toClass.IsEnum)
            return GenericDefFullName(toClass) == "System.Nullable" && toClass.Context.TypeArgs is [{ } underlying]
                && BinderSame(underlying, from);
        if (from.Kind is TypeKind.SZArray or TypeKind.MDArray)
            return toClass.FullName == "System.Array" ? true : toClass.IsInterface ? null : false;
        var fromClass = from.Kind == TypeKind.Class ? from.Class! : FindClassByFullName("System." + from.Primitive);
        if (fromClass is null || !HierarchyFinal(fromClass))
            return null;
        if (IsBinderValueType(from))
        {
            if (!toClass.IsInterface)
                return toClass.FullName == "System.ValueType" || toClass.FullName == "System.Enum" && fromClass.IsEnum;
            if (ImplementsInterface(fromClass, toClass))
                return true;
        }
        else if (RefAssignable(from, to))
            return true;
        // A primitive-backing argument may close an instantiation identity reads as distinct.
        return toClass.GenericArity > 0 && HasInstantiationOf(fromClass, toClass) ? null : false;
    }

    private bool AdmitsAccessorMember(ClassInfo c, string name, LookupMemberKind kind, int flags, bool declared,
        MethodDefinitionHandle first, MethodDefinitionHandle second)
    {
        var reader = c.Module.Reader;
        bool anyPublic = false, allPrivate = true, isStatic = false, any = false;
        foreach (var h in new[] { first, second })
        {
            if (h.IsNil)
                continue;
            var md = reader.GetMethodDefinition(h);
            any = true;
            var access = md.Attributes & MethodAttributes.MemberAccessMask;
            anyPublic |= access == MethodAttributes.Public;
            allPrivate &= access == MethodAttributes.Private;
            isStatic |= (md.Attributes & MethodAttributes.Static) != 0;
        }
        if (!any)
            return false;
        var visibility = anyPublic ? LookupVisibility.Public : allPrivate ? LookupVisibility.Private : LookupVisibility.NonPublic;
        return ReflectionLookupRules.Admits(new LookupRow(name, kind, visibility, isStatic), flags, declared);
    }

    /// <summary>A property's signature as .NET compares it for hiding: the raw metadata
    /// form, generic parameters by position, so a signature equal only in some
    /// instantiation does not hide.</summary>
    private static string PropertySignatureKey(PropertyDefinition property)
    {
        var signature = property.DecodeSignature(RawSignatureProvider.Instance, null);
        var key = new StringBuilder(signature.Header.IsInstance ? "i:" : "s:").Append(signature.ReturnType).Append('(');
        foreach (string parameter in signature.ParameterTypes)
            key.Append(parameter).Append(',');
        return key.Append(')').ToString();
    }

    private string ReflectionOverrideSignature(MethodInfo method)
    {
        var signature = ReflectionSignature(method);
        return signature.GenericParameterCount + ":" + string.Join(",", signature.ParameterTypes.Select(MangleArg));
    }

    private static LookupVisibility MethodVisibility(MethodAttributes attributes) =>
        (attributes & MethodAttributes.MemberAccessMask) switch
        {
            MethodAttributes.Public => LookupVisibility.Public,
            MethodAttributes.Private => LookupVisibility.Private,
            _ => LookupVisibility.NonPublic,
        };

    private static int GenericParameterCount(MethodInfo m) =>
        m.Handle.IsNil ? 0 : m.Module.Reader.GetMethodDefinition(m.Handle).GetGenericParameters().Count;

    /// <summary><paramref name="cls"/> and its bases, nearest first. A specialization learns
    /// its base only with its shape, so each link is read from a completed shape.</summary>
    private List<ClassInfo> BaseChain(ClassInfo cls)
    {
        var chain = new List<ClassInfo>();
        for (ClassInfo? c = cls; c is not null; c = CompleteReflectionDefinitionOwner(c).BaseClass)
            chain.Add(c);
        return chain;
    }

    /// <summary>Whether DefaultBinder.SelectMethod may pick <paramref name="method"/> for an
    /// alternative of a <c>Type[]</c> operand. It expands neither params arrays nor optional
    /// parameters, so the count is exact. An alternative of unknown length, and an element
    /// not known to be a type, admit any parameter.</summary>
    private bool MayBindParameters(MethodInfo method, RResolved types) =>
        !BindsByArity(types) || MayBindTypes(ReflectionSignature(method).ParameterTypes, types);

    /// <summary>Whether every alternative of a <c>Type[]</c> operand is null or an array of
    /// known length, so the binder's count and element tests decide.</summary>
    private static bool BindsByArity(RResolved types)
    {
        if (types.Unknown is not null || types.Items.Count == 0)
            return false;
        foreach (var e in types.Items)
            if (e.Kind != REntityKind.Null && (e.Kind != REntityKind.TypeArray || e.Number < 0))
                return false;
        return true;
    }

    /// <summary><see cref="MayBindParameters"/> over a parameter list: a method's, or a
    /// property's index parameters.</summary>
    private bool MayBindTypes(ImmutableArray<TypeDesc> parameters, RResolved types)
    {
        if (!BindsByArity(types))
            return true;
        foreach (var e in types.Items)
        {
            if (parameters.Length != (e.Kind == REntityKind.Null ? 0 : e.Number))
                continue;
            bool admits = true;
            for (int i = 0; admits && i < parameters.Length; i++)
                admits = e.Elements is not { } elements || i >= elements.Length
                    || ElementMayBind(parameters[i], elements[i]);
            if (admits)
                return true;
        }
        return false;
    }

    /// <summary>Whether a <c>Type[]</c> element may select <paramref name="parameter"/>: one
    /// of its types does, or it may be something other than a known type.</summary>
    private bool ElementMayBind(TypeDesc parameter, RResolved element)
    {
        if (element.Unknown is not null || element.Items.Count == 0)
            return true;
        foreach (var e in element.Items)
            if (e.Kind != REntityKind.Type || e.Type is not { } type || BinderAdmits(parameter, type))
                return true;
        return false;
    }

    /// <summary>DefaultBinder.SelectMethod's test of an argument type against a parameter:
    /// identity, an object parameter, primitive widening into a primitive parameter, else
    /// <c>IsAssignableFrom</c>. A by-ref compares its element, and a shape the model cannot
    /// decide admits.</summary>
    private bool BinderAdmits(TypeDesc parameter, TypeDesc argument)
    {
        var p = PrimitiveBacked(parameter.Kind == TypeKind.ByRef ? parameter.Element! : parameter);
        var a = PrimitiveBacked(argument.Kind == TypeKind.ByRef ? argument.Element! : argument);
        if (!BinderDecides(p) || !BinderDecides(a) || BinderSame(p, a) || p.IsObject)
            return true;
        if (IsBinderPrimitive(p))
            return BinderTypeCode(a) is { } source && CanChangePrimitive(source, p.Primitive);
        return BinderAssignable(a, p);
    }

    /// <summary><paramref name="t"/> as signatures spell it, primitive-backing classes as
    /// primitives at every array depth, so an argument tuple keys the instantiation a
    /// static mention creates instead of minting a second one.</summary>
    private static TypeDesc SignatureForm(TypeDesc t) => t.Kind switch
    {
        TypeKind.SZArray => SignatureForm(t.Element!) is var e && e != t.Element ? TypeDesc.MakeSZArray(e) : t,
        TypeKind.MDArray => SignatureForm(t.Element!) is var e && e != t.Element ? TypeDesc.MakeMDArray(e, t.Rank) : t,
        _ => PrimitiveBacked(t),
    };

    /// <summary>A corlib primitive-backing class as the primitive signatures spell it.</summary>
    private static TypeDesc PrimitiveBacked(TypeDesc t) =>
        t.Kind == TypeKind.Class && WellKnownPrimitive(t.Class!.FullName) is { } primitive ? primitive : t;

    /// <summary>Whether the binder relation over <paramref name="t"/> is decidable: not a
    /// generic variable, a canonical stand-in, an open shell, an unloaded type or a
    /// pointer.</summary>
    private static bool BinderDecides(TypeDesc t) =>
        !ContainsCanonPlaceholder(t) && !ContainsGenericVar(t) && t.Kind switch
        {
            TypeKind.Primitive => true,
            TypeKind.Class => t.Class is { } c && (c.GenericArity == 0 || c.Context.TypeArgs.Length == c.GenericArity),
            TypeKind.SZArray or TypeKind.MDArray => BinderDecides(t.Element!),
            _ => false,
        };

    /// <summary>Exact identity, reading corlib's primitive-backing classes as their
    /// primitives at every array depth.</summary>
    private static bool BinderSame(TypeDesc a, TypeDesc b)
    {
        a = PrimitiveBacked(a);
        b = PrimitiveBacked(b);
        if ((a.Kind is TypeKind.SZArray or TypeKind.MDArray) && a.Kind == b.Kind)
            return a.Rank == b.Rank && BinderSame(a.Element!, b.Element!);
        return SameTypeArg(a, b);
    }

    /// <summary>The <c>Type.IsPrimitive</c> types, whose parameters the binder matches
    /// through <see cref="CanChangePrimitive"/>.</summary>
    private static bool IsBinderPrimitive(TypeDesc t) =>
        t.Kind == TypeKind.Primitive && t.Primitive is not (PrimitiveTypeCode.String or PrimitiveTypeCode.Object
            or PrimitiveTypeCode.Void or PrimitiveTypeCode.TypedReference);

    /// <summary>The type code the binder widens from: a primitive's own, an enum's
    /// underlying one.</summary>
    private PrimitiveTypeCode? BinderTypeCode(TypeDesc t)
    {
        if (IsBinderPrimitive(t))
            return t.Primitive;
        if (t.Kind == TypeKind.Class && EnsureShape(t.Class!).IsEnum)
            return t.Class!.EnumUnderlying;
        return null;
    }

    /// <summary>DefaultBinder.CanChangePrimitive: identity or one of its widenings. A native
    /// integer matches only itself.</summary>
    private static bool CanChangePrimitive(PrimitiveTypeCode source, PrimitiveTypeCode target) =>
        source == target || source switch
        {
            PrimitiveTypeCode.Char => target is PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.UInt32
                or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.Int64
                or PrimitiveTypeCode.Single or PrimitiveTypeCode.Double,
            PrimitiveTypeCode.SByte => target is PrimitiveTypeCode.Int16 or PrimitiveTypeCode.Int32
                or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.Single or PrimitiveTypeCode.Double,
            PrimitiveTypeCode.Byte => target is PrimitiveTypeCode.Char or PrimitiveTypeCode.UInt16
                or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.Int32
                or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.Single
                or PrimitiveTypeCode.Double,
            PrimitiveTypeCode.Int16 => target is PrimitiveTypeCode.Int32 or PrimitiveTypeCode.Int64
                or PrimitiveTypeCode.Single or PrimitiveTypeCode.Double,
            PrimitiveTypeCode.UInt16 => target is PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.Int32
                or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.Single
                or PrimitiveTypeCode.Double,
            PrimitiveTypeCode.Int32 => target is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.Single
                or PrimitiveTypeCode.Double,
            PrimitiveTypeCode.UInt32 => target is PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.Int64
                or PrimitiveTypeCode.Single or PrimitiveTypeCode.Double,
            PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 => target is PrimitiveTypeCode.Single
                or PrimitiveTypeCode.Double,
            PrimitiveTypeCode.Single => target is PrimitiveTypeCode.Double,
            _ => false,
        };

    /// <summary><c>IsAssignableFrom</c> between decidable, distinct types for a parameter that
    /// is neither object nor primitive. A negative answer stands only over a hierarchy the
    /// model holds in full.</summary>
    private bool BinderAssignable(TypeDesc a, TypeDesc p)
    {
        if (p.Kind is TypeKind.SZArray or TypeKind.MDArray)
        {
            // Array casting also takes an SZ array to a rank-one MD array.
            bool shapeMatches = a.Kind == TypeKind.MDArray
                ? p.Kind == TypeKind.MDArray && a.Rank == p.Rank
                : a.Kind == TypeKind.SZArray && (p.Kind == TypeKind.SZArray || p.Rank == 1);
            if (!shapeMatches)
                return false;
            var ae = PrimitiveBacked(a.Element!);
            var pe = PrimitiveBacked(p.Element!);
            bool valueA = IsBinderValueType(ae), valueP = IsBinderValueType(pe);
            // Value elements never cast to reference ones. Among themselves the CLR also
            // equates same-size integral and enum elements, so any pair admits.
            if (valueA || valueP)
                return valueA && valueP;
            return BinderSame(ae, pe) || pe.IsObject || BinderAssignable(ae, pe);
        }
        if (p.Kind != TypeKind.Class)
            return false;
        var pc = EnsureShape(p.Class!);
        if (pc.IsValueType || pc.IsEnum)
            return GenericDefFullName(pc) == "System.Nullable" && pc.Context.TypeArgs is [{ } underlying]
                && BinderSame(underlying, a);
        if (a.Kind is TypeKind.SZArray or TypeKind.MDArray)
            return pc.IsInterface || pc.FullName == "System.Array";
        var ac = a.Kind == TypeKind.Class ? a.Class! : FindClassByFullName("System." + a.Primitive);
        if (ac is null || !HierarchyFinal(ac))
            return true;
        if (IsBinderValueType(a))
        {
            if (!pc.IsInterface)
                return pc.FullName == "System.ValueType" || (pc.FullName == "System.Enum" && ac.IsEnum);
            if (ImplementsInterface(ac, pc))
                return true;
        }
        else if (RefAssignable(a, p))
            return true;
        // Reflection may close an instantiation over a primitive-backing class, which
        // identity reads as distinct; any instantiation of the parameter's definition admits.
        return pc.GenericArity > 0 && HasInstantiationOf(ac, pc);
    }

    /// <summary>Whether <paramref name="t"/> is a value type; a specialization learns that
    /// with its shape.</summary>
    private bool IsBinderValueType(TypeDesc t)
    {
        if (t.Kind == TypeKind.Class)
            CompleteReflectionDefinitionOwner(t.Class!);
        return IsValueElement(t);
    }

    /// <summary>Whether derivation and implementation answers over <paramref name="cls"/>
    /// are final: its bases and interfaces are loaded and shaped, and a class's chain
    /// reaches System.Object.</summary>
    private bool HierarchyFinal(ClassInfo cls)
    {
        var last = cls;
        for (var b = cls; b is not null; b = b.BaseClass)
        {
            CompleteReflectionDefinitionOwner(b);
            if (b.ExternalBaseName is not null || b.ExternalInterfaceNames.Count > 0)
                return false;
            last = b;
        }
        if (!cls.IsInterface && !cls.IsValueType && !cls.IsEnum && last.FullName != "System.Object")
            return false;
        foreach (var itf in GetInterfaceClosure(cls).Ordered)
            if (!itf.ShapeReady || itf.ExternalInterfaceNames.Count > 0)
                return false;
        return true;
    }

    /// <summary>Whether <paramref name="cls"/>, a base of it or an interface it implements
    /// instantiates the generic definition of <paramref name="instance"/>.</summary>
    private bool HasInstantiationOf(ClassInfo cls, ClassInfo instance)
    {
        for (var b = cls; b is not null; b = b.BaseClass)
            if (b.Module == instance.Module && b.Handle == instance.Handle)
                return true;
        return instance.IsInterface && ImplementsInterfaceDefinition(cls, instance);
    }

    /// <summary>The methods behind a property or event entity, as RuntimeType associates
    /// them: a member inherited from a base exposes no private accessor, and
    /// <paramref name="publicOnly"/> keeps only public ones, as an accessor read without
    /// nonPublic returns.</summary>
    private List<MethodInfo> AccessorsOf(REntity e, ReflectionAccessor which, bool publicOnly = false)
    {
        var result = new List<MethodInfo>();
        if (e.Owner is not { } owner)
            return result;
        var reader = owner.Module.Reader;
        var handles = new List<MethodDefinitionHandle>();
        if (e.Kind == REntityKind.Property)
        {
            var accessors = reader.GetPropertyDefinition((PropertyDefinitionHandle)e.Member).GetAccessors();
            if (which is ReflectionAccessor.Getter or ReflectionAccessor.All)
                handles.Add(accessors.Getter);
            if (which is ReflectionAccessor.Setter or ReflectionAccessor.All)
                handles.Add(accessors.Setter);
        }
        else if (e.Kind == REntityKind.Event)
        {
            var accessors = reader.GetEventDefinition((EventDefinitionHandle)e.Member).GetAccessors();
            if (which is ReflectionAccessor.Adder or ReflectionAccessor.All)
                handles.Add(accessors.Adder);
            if (which is ReflectionAccessor.Remover or ReflectionAccessor.All)
                handles.Add(accessors.Remover);
            if (which is ReflectionAccessor.Raiser or ReflectionAccessor.All)
                handles.Add(accessors.Raiser);
        }
        foreach (var h in handles)
        {
            if (h.IsNil)
                continue;
            var access = reader.GetMethodDefinition(h).Attributes & MethodAttributes.MemberAccessMask;
            if ((!e.InheritedMember || access != MethodAttributes.Private) && (!publicOnly || access == MethodAttributes.Public)
                && MethodOf(owner, h) is { } method)
                result.Add(method);
        }
        return result;
    }

    private MethodInfo? MethodOf(ClassInfo owner, MethodDefinitionHandle handle)
    {
        EnsureCompleted(owner);
        if (_reflectionMetadataViews.Contains(owner))
            return owner.Methods.Find(method => method.Handle == handle);
        if (owner.GenericArity > 0)
            return owner.MethodByTemplate.TryGetValue(handle, out var instance) ? instance : null;
        return owner.Module.MethodMap.TryGetValue(handle, out var method) ? method : null;
    }

    // ---- resolution ----

    private RResolved Operand(ReflectionApplication app, RSite site, sbyte slot)
    {
        if (slot >= 0)
            return slot < site.Operands.Length ? Resolve(app, site.Operands[slot]) : RResolved.Empty;
        if (slot == ReflectionApi.Receiver)
            return Resolve(app, site.Receiver);
        if (slot == ReflectionApi.MethodTypeArgument)
            return Resolve(app, site.TypeArgument);
        return RResolved.Empty;
    }

    private RResolved Resolve(ReflectionApplication app, RValue value)
    {
        var result = new RResolved();
        for (int i = 0; i < value.Count; i++)
            result.Merge(ResolveTerm(app, value.Alternatives![i]));
        if (value.Unknown >= 0 && result.Unknown is null)
            result.Unknown = DescribeUnknown(app, app.Summary.Unknowns[value.Unknown]);
        return result;
    }

    private RResolved Child(ReflectionApplication app, RTerm t, int slot) =>
        slot < t.Children.Length && t.Children[slot] >= 0 ? Resolve(app, app.Summary.ValueOf(t.Children[slot])) : RResolved.Empty;

    private RResolved ResolveTerm(ReflectionApplication app, int id)
    {
        if (app.Terms.TryGetValue(id, out var memo))
        {
            if (app.Growing.TryGetValue(id, out var growing))
                foreach (var (module, definition) in growing)
                    NoteGrowingSelection(app, module, definition);
            return memo;
        }
        if (!app.Visiting.Add(id))
            return new RResolved { Unknown = "a value that depends on itself" };
        RResolved result;
        app.Resolving.Add(id);
        try
        {
            result = app.Resolved > ReflectionApplication.MaxResolved
                ? new RResolved { Unknown = RResolved.TooBroad }
                : ResolveTermUncached(app, id);
        }
        catch (Exception e) when (!IsMustEscape(e) && e is NotSupportedException or BadImageFormatException)
        {
            result = new RResolved { Unknown = "a member that does not decode" };
        }
        app.Resolved += result.Items.Count;
        app.Resolving.RemoveAt(app.Resolving.Count - 1);
        app.Visiting.Remove(id);
        app.Terms[id] = result;
        return result;
    }

    private RResolved ResolveTermUncached(ReflectionApplication app, int id)
    {
        var t = app.Summary.Terms[id];
        var m = app.Method;
        var module = m.Module;
        var result = new RResolved();
        switch (t.Kind)
        {
            case TermKind.TypeToken:
            {
                var type = ResolveTypeTokenForScan(module, SRME.EntityHandle(t.Token), m.Context);
                // Boxing a Nullable makes its underlying value.
                if (t.Number == ReflectionSummary.InexactBox && type is not null) type = NullableUnderlying(type) ?? type;
                AddType(result, type, app);
                NoteInexactBox(result, t, type);
                break;
            }
            case TermKind.MethodSpecArg:
            {
                var handle = SRME.EntityHandle(t.Token);
                if (handle.Kind != HandleKind.MethodSpecification)
                    break;
                var args = module.Reader.GetMethodSpecification((MethodSpecificationHandle)handle)
                    .DecodeSignature(SigProvider, m.Context);
                if (t.Number < args.Length)
                    AddType(result, args[(int)t.Number], app);
                break;
            }
            case TermKind.TypeName:
                ResolveTypeName(app, t, result);
                break;
            case TermKind.MadeGeneric:
                result.Merge(MakeGeneric(app, -1, Child(app, t, 0), Child(app, t, 1), report: false));
                break;
            case TermKind.ArrayOf:
            {
                // A clone's closed base stands for no type of its own: it only answers
                // the clone's inherited lookups.
                var of = Child(app, t, 0);
                result.Unknown = of.Unknown;
                foreach (var e in of.Items)
                    if (e.Inherited)
                        continue;
                    else if (e.Kind == REntityKind.Type && e.Type is { } element)
                    {
                        if (t.Number == global::Dn2Cpp.ReflectionTypeName.SZArray)
                            result.Add(TypeEntity(TypeDesc.MakeSZArray(element)));
                        else if (t.Number > 0)
                            result.Add(TypeEntity(TypeDesc.MakeMDArray(element, (int)t.Number)));
                        else
                            result.Unknown ??= "MakeArrayType with a rank not known statically";
                    }
                    else if (e.Kind == REntityKind.Template)
                        result.Unknown ??= "an array of a runtime-instantiated generic type";
                break;
            }
            case TermKind.ElementOf:
            {
                var of = Child(app, t, 0);
                foreach (var e in of.Items)
                    if (e.Kind == REntityKind.Type && !e.Inherited && e.Type?.Element is { } element)
                        AddType(result, element, app);
                result.Unknown ??= of.Unknown;
                break;
            }
            case TermKind.GenericDefOf:
            {
                var of = Child(app, t, 0);
                foreach (var e in of.Items)
                    if (e.Inherited)
                        continue;
                    else if (e.Kind == REntityKind.Type && e.Type is { Kind: TypeKind.Class, Class: { GenericArity: > 0 } cls })
                        result.Add(DefinitionEntity(cls.Module, cls.Handle));
                    else if (e.Kind == REntityKind.Definition)
                        result.Add(e);
                    else if (e.Kind == REntityKind.Template && e.Assembly is { } definitionModule)
                        result.Add(DefinitionEntity(definitionModule, e.Definition));
                result.Unknown = of.Unknown;
                break;
            }
            case TermKind.GenericArgOf:
            {
                var of = Child(app, t, 0);
                result.Unknown = of.Unknown;
                foreach (var e in of.Items)
                    if (e.Inherited)
                        continue;
                    else if (e.Kind == REntityKind.Type && e.Type is { Kind: TypeKind.Class, Class: { } cls })
                    {
                        var args = cls.Context.TypeArgs;
                        for (int i = 0; i < args.Length; i++)
                            if (t.Number < 0 || t.Number == i)
                                AddType(result, args[i], app);
                    }
                    else if (e.Kind == REntityKind.Template)
                    {
                        if (e.Tuple is not { } tuple)
                            result.Unknown ??= "the type arguments of a runtime-instantiated generic type";
                        else
                            for (int i = 0; i < tuple.Length; i++)
                                if (t.Number < 0 || t.Number == i)
                                    AddType(result, tuple[i], app);
                    }
                break;
            }
            case TermKind.BaseOf:
            {
                var of = Child(app, t, 0);
                result.Unknown = of.Unknown;
                foreach (var e in of.Items)
                    if (e.Inherited)
                        continue;
                    else if (e.Kind == REntityKind.Type && e.Type is { Kind: TypeKind.Class, Class: { } cls })
                    {
                        // A specialization learns its base only with its shape.
                        if (EnsureShape(cls).BaseClass is { } b)
                            result.Add(TypeEntity(TypeDesc.MakeClass(b)));
                    }
                    else if (e.Kind == REntityKind.Template && e.Assembly is { } definitionModule)
                        AddTemplateBase(result, app, definitionModule, e.Definition, e.Tuple);
                    else if (e.Kind == REntityKind.Definition && e.Assembly is { } openModule
                        && ClosedDefinitionBase(openModule, e.Definition) is { } closedBase)
                        result.Add(TypeEntity(TypeDesc.MakeClass(closedBase)));
                break;
            }
            case TermKind.TypeOfMember:
            {
                var of = Child(app, t, 0);
                foreach (var e in of.Items)
                    if (e.Kind == REntityKind.TemplateMember)
                        foreach (var member in TemplateMetadataMembers(e))
                            AddMemberType(result, member, (ReflectionOp)t.Number, app);
                    else
                        AddMemberType(result, e, (ReflectionOp)t.Number, app);
                result.Unknown ??= of.Unknown;
                break;
            }
            case TermKind.Str:
                result.Add(new REntity { Kind = REntityKind.Str, Text = t.Text });
                break;
            case TermKind.Int:
                result.Add(new REntity { Kind = REntityKind.Int, Number = t.Number });
                break;
            case TermKind.Null:
                result.Add(new REntity { Kind = REntityKind.Null });
                break;
            case TermKind.TypeArray:
            {
                if (t.Number < 0)
                {
                    var all = Child(app, t, 0);
                    result.Add(new REntity { Kind = REntityKind.TypeArray, Number = -1, Elements = new[] { all } });
                    break;
                }
                var elements = new RResolved[t.Children.Length];
                for (int i = 0; i < elements.Length; i++)
                    elements[i] = Child(app, t, i);
                result.Add(new REntity { Kind = REntityKind.TypeArray, Number = t.Number, Elements = elements });
                break;
            }
            case TermKind.Set:
                result.Merge(Resolve(app, app.Summary.ValueOf(id)));
                break;
            case TermKind.MemberToken:
                AddMemberToken(result, module, t.Token, m);
                break;
            case TermKind.Lookup:
                ResolveLookup(app, t, result);
                break;
            case TermKind.Enumeration:
                ResolveEnumeration(app, t, result);
                break;
            case TermKind.Element:
            {
                var of = Child(app, t, 0);
                foreach (var e in of.Items)
                    if (e.Kind == REntityKind.TypeArray && e.Elements is { } elements)
                    {
                        for (int i = 0; i < elements.Length; i++)
                            if (t.Number < 0 || t.Number == i || e.Number < 0)
                                result.Merge(elements[i]);
                    }
                    else
                        result.Add(e);
                // An array from an unknown source may hold any member, whatever its known
                // elements name.
                if (of.Unknown is not null)
                {
                    result.Unknown ??= of.Unknown;
                    result.UnknownMembers = null;
                    result.UnknownMemberKind = null;
                }
                break;
            }
            case TermKind.Accessor:
            {
                var of = Child(app, t, 0);
                // GetXxxMethod() and GetAccessors() without nonPublic return only public
                // accessors; unknown flags may ask for any.
                bool publicOnly = true;
                foreach (int flags in FlagAlternatives(Child(app, t, 1), ReflectionApis.Public | ReflectionApis.NonPublic, out _))
                    publicOnly &= (flags & ReflectionApis.NonPublic) == 0;
                foreach (var e in of.Items)
                    if (e.Kind is REntityKind.Property or REntityKind.Event)
                        foreach (var accessor in AccessorsOf(e, (ReflectionAccessor)t.Number, publicOnly))
                            result.Add(new REntity { Kind = REntityKind.Method, Method = accessor, Owner = e.Owner });
                    else if (e.Kind == REntityKind.DefinitionMember && e.Metadata is { } metadata)
                        foreach (var accessor in AccessorsOf(metadata, (ReflectionAccessor)t.Number, publicOnly))
                            result.Add(new REntity { Kind = REntityKind.DefinitionMember,
                                Metadata = new REntity { Kind = REntityKind.Method, Method = accessor, Owner = accessor.DeclaringClass } });
                    // A clone's accessor stays a lookup on its template, narrowed to the accessor.
                    else if (e is { Kind: REntityKind.TemplateMember, Lookup.Kind: LookupMemberKind.Property or LookupMemberKind.Event })
                        result.Add(new REntity
                        {
                            Kind = REntityKind.TemplateMember, Assembly = e.Assembly, Definition = e.Definition,
                            Tuple = e.Tuple, Lookup = e.Lookup, Number = t.Number, PublicAccessors = publicOnly,
                        });
                result.Unknown = of.Unknown is null ? null : "an accessor of a member that comes from " + of.Unknown;
                break;
            }
            case TermKind.MadeGenericMethod:
            {
                var arguments = Child(app, t, 1);
                var tuples = ArgumentTuples(arguments, out bool known, closeTemplates: true);
                var generics = GenericMethodReceivers(Child(app, t, 0), out string? receiverUnknown);
                foreach (var generic in generics)
                {
                    if (!IsOpenGenericMethod(generic))
                        continue;
                    bool metadata = _reflectionMetadataViews.Contains(generic.DeclaringClass);
                    var made = MakeGenericMethods(generic, tuples, out _);
                    // Unknown arguments do not change an open owner's original attribute provider.
                    if (!known && metadata) made.Add(generic);
                    foreach (var instance in made)
                    {
                        var member = new REntity { Kind = REntityKind.Method, Method = instance, Owner = instance.DeclaringClass };
                        result.Add(metadata ? new REntity { Kind = REntityKind.DefinitionMember, Metadata = member } : member);
                    }
                }
                result.Unknown = receiverUnknown ?? (known ? null : ArgumentsUnknown(arguments)
                    ?? "type arguments with no known closed tuple");
                break;
            }
            case TermKind.Parameter:
            {
                var of = Child(app, t, 0);
                result.Unknown = of.Unknown;
                foreach (var e in of.Items)
                    if (e.Kind == REntityKind.Method)
                        result.Add(new REntity { Kind = REntityKind.Parameter, Method = e.Method, Number = t.Number });
                    else if (e.Kind == REntityKind.DefinitionMember && e.Metadata is { Kind: REntityKind.Method } metadata)
                        result.Add(new REntity { Kind = REntityKind.DefinitionMember,
                            Metadata = new REntity { Kind = REntityKind.Parameter, Method = metadata.Method, Number = t.Number } });
                    else if (e.Kind == REntityKind.TemplateMember)
                        foreach (var member in TemplateMetadataMembers(e))
                            if ((member.Metadata ?? member) is { Kind: REntityKind.Method, Method: { } method })
                                result.Add(new REntity { Kind = REntityKind.DefinitionMember,
                                    Metadata = new REntity { Kind = REntityKind.Parameter, Method = method, Number = t.Number } });
                break;
            }
            case TermKind.Delegate:
            {
                var targets = Child(app, t, 0);
                var types = Child(app, t, 1);
                var delegateClasses = ClassesOf(types);
                if (delegateClasses.Count == 0)
                    result.Add(new REntity { Kind = REntityKind.Delegate, Targets = targets });
                foreach (var cls in delegateClasses)
                    result.Add(new REntity { Kind = REntityKind.Delegate, Targets = targets, Type = TypeDesc.MakeClass(cls) });
                if (types.Unknown is not null)
                    result.Unknown = "a delegate whose type comes from " + types.Unknown;
                break;
            }
            case TermKind.ExactObject:
            case TermKind.Closure:
            {
                var type = ResolveTypeTokenForScan(module, SRME.EntityHandle(t.Token), m.Context);
                if (t.Number == ReflectionSummary.InexactBox && type is not null) type = NullableUnderlying(type) ?? type;
                if (type is not null)
                    result.Add(new REntity { Kind = REntityKind.Object, Type = type });
                NoteInexactBox(result, t, type);
                break;
            }
            case TermKind.Assembly:
                ResolveAssembly(app, t, result);
                break;
        }
        return result;
    }

    private void AddType(RResolved result, TypeDesc? type, ReflectionApplication app)
    {
        if (type is null)
            return;
        if (ContainsCanonPlaceholder(type) || ContainsGenericVar(type))
        {
            result.Unknown ??= "a shared generic instantiation, whose type arguments are known only at run time";
            return;
        }
        if (type.Kind == TypeKind.Template && type.TemplateModule is { } templateModule)
        {
            result.Add(DefinitionEntity(templateModule, type.TemplateHandle));
            return;
        }
        if (type.Kind == TypeKind.External && ResolveExternalClass(type) is { } external)
            type = TypeDesc.MakeClass(external);
        // Owner consumers read classes: a signature's primitive takes the class form
        // typeof gives it, or the site is unknown rather than an empty selection.
        if (IsClassBackedPrimitive(type))
        {
            if (OwnerClass(type) is not { } backing)
            {
                result.Unknown ??= "the primitive type " + type + ", whose CoreLib class is not loaded";
                return;
            }
            type = TypeDesc.MakeClass(backing);
        }
        result.Add(TypeEntity(type));
    }

    private static bool IsClassBackedPrimitive(TypeDesc type) =>
        type is { Kind: TypeKind.Primitive, IsCanonPlaceholder: false }
        && type.Primitive is not (PrimitiveTypeCode.Void or PrimitiveTypeCode.TypedReference);

    /// <summary>The class whose rows answer a member lookup on <paramref name="type"/>,
    /// or null when no class does.</summary>
    private ClassInfo? OwnerClass(TypeDesc? type) => type switch
    {
        { Kind: TypeKind.Class, Class: { } cls } => cls,
        { } primitive when IsClassBackedPrimitive(primitive) => FindClassByFullName("System." + primitive.Primitive),
        _ => null,
    };

    /// <summary>A box whose operand may be a reference type yields that object, whose
    /// runtime type is the token's only when no type derives from it.</summary>
    private static void NoteInexactBox(RResolved result, RTerm t, TypeDesc? type)
    {
        if (t.Number == ReflectionSummary.InexactBox && type is not null && !IsExactRuntimeType(type))
            result.Unknown ??= "an object whose type may derive from " + type;
    }

    private static bool IsExactRuntimeType(TypeDesc t) => t.Kind switch
    {
        TypeKind.Primitive => !t.IsCanonPlaceholder && t.Primitive != PrimitiveTypeCode.Object,
        TypeKind.SZArray or TypeKind.MDArray => t.Element is { } e && IsExactRuntimeType(e),
        TypeKind.Class => t.Class!.IsValueType || t.Class.IsSealed,
        _ => false,
    };

    private static REntity TypeEntity(TypeDesc type) => new() { Kind = REntityKind.Type, Type = type };

    private REntity DefinitionEntity(Module module, TypeDefinitionHandle handle)
    {
        if (module.Reader.GetTypeDefinition(handle).GetGenericParameters().Count == 0
            && module.ClassMap.TryGetValue(handle, out var cls))
            return TypeEntity(TypeDesc.MakeClass(cls));
        return new REntity { Kind = REntityKind.Definition, Assembly = module, Definition = handle };
    }

    private void AddMemberToken(RResolved result, Module module, int token, MethodInfo context)
    {
        var handle = SRME.EntityHandle(token);
        switch (handle.Kind)
        {
            case HandleKind.FieldDefinition:
            {
                var fd = module.Reader.GetFieldDefinition((FieldDefinitionHandle)handle);
                if (module.ClassMap.TryGetValue(fd.GetDeclaringType(), out var owner))
                    foreach (var f in owner.Fields)
                        if (f.Handle == (FieldDefinitionHandle)handle)
                            result.Add(new REntity { Kind = REntityKind.Field, Field = f, Owner = owner });
                return;
            }
            case HandleKind.MemberReference
                when module.Reader.GetMemberReference((MemberReferenceHandle)handle).GetKind() == MemberReferenceKind.Field:
            {
                var (owner, field) = ResolveMemberRefField(module, (MemberReferenceHandle)handle, context.Context);
                result.Add(new REntity { Kind = REntityKind.Field, Field = field, Owner = owner });
                return;
            }
            case HandleKind.MethodDefinition or HandleKind.MemberReference or HandleKind.MethodSpecification:
            {
                var method = ResolveMethodHandle(module, handle, context.Context, context.DeclaringClass);
                if (method is not null)
                    result.Add(new REntity { Kind = REntityKind.Method, Method = method, Owner = method.DeclaringClass });
                return;
            }
        }
    }

    private TypeDesc? MemberType(REntity e, ReflectionOp which)
    {
        e = e.Metadata ?? e;
        switch (which)
        {
            case ReflectionOp.FieldType when e.Kind == REntityKind.Field:
                return e.Field is { } field && _reflectionMetadataViews.Contains(field.DeclaringClass)
                    ? field.DeclaringClass.Module.Reader.GetFieldDefinition(field.Handle)
                        .DecodeSignature(new ReflectionDefinitionSignatureProvider(this), field.DeclaringClass.Context)
                    : e.Field!.Type;
            case ReflectionOp.PropertyType when e.Kind == REntityKind.Property && e.Owner is { } owner:
                return ReflectionPropertySignature(owner,
                    owner.Module.Reader.GetPropertyDefinition((PropertyDefinitionHandle)e.Member)).ReturnType;
            case ReflectionOp.ReturnType when e.Kind == REntityKind.Method:
                return ReflectionSignature(e.Method!).ReturnType;
            case ReflectionOp.ParameterType when e.Kind == REntityKind.Parameter:
            {
                var signature = ReflectionSignature(e.Method!);
                var parameters = signature.ParameterTypes;
                return e.Number == -1 ? signature.ReturnType
                    : e.Number >= 0 && e.Number < parameters.Length ? parameters[(int)e.Number] : null;
            }
        }
        return null;
    }

    private void AddMemberType(RResolved result, REntity member, ReflectionOp which, ReflectionApplication app)
    {
        var type = MemberType(member, which);
        if (type is not null && ContainsReflectionMetadataView(type))
        {
            if (type is { Kind: TypeKind.Class, Class: { } definition })
                result.Add(DefinitionEntity(definition.Module, definition.Handle));
            result.Unknown ??= "a generic member signature without an executable instantiation";
            return;
        }
        AddType(result, type, app);
    }

    private bool ContainsReflectionMetadataView(TypeDesc type) => type.Kind switch
    {
        TypeKind.Class => _reflectionMetadataViews.Contains(type.Class!)
            || type.Class!.Context.TypeArgs.Any(ContainsReflectionMetadataView),
        TypeKind.Pointer when type.FunctionSignature is { } signature => signature.Types.Any(ContainsReflectionMetadataView),
        TypeKind.Pointer or TypeKind.ByRef or TypeKind.SZArray or TypeKind.MDArray =>
            type.Element is { } element && ContainsReflectionMetadataView(element),
        _ => false,
    };

    private void ResolveTypeName(ReflectionApplication app, RTerm t, RResolved result)
    {
        var parsed = global::Dn2Cpp.ReflectionTypeName.Parse(t.Text ?? "");
        if (parsed is null)
            return;
        List<Module>? assemblies = null;
        if (t.Children.Length > 0 && t.Children[0] >= 0)
        {
            assemblies = new List<Module>();
            var named = Child(app, t, 0);
            foreach (var e in named.Items)
                if (e.Kind == REntityKind.Assembly && e.Assembly is { } a)
                    assemblies.Add(a);
            if (named.Unknown is not null)
                result.Unknown = "a type name searched in an assembly that comes from " + named.Unknown;
        }
        ResolveNamedType(parsed, assemblies, (t.Number & 1) != 0, app, result, noteNamed: true);
    }

    /// <summary>Each Assembly value is an alternative receiver, so its lookup contributes
    /// independently. A null receiver set denotes Type.GetType's ordered caller/CoreLib
    /// search; an empty set has no known receiver.</summary>
    private void ResolveNamedType(global::Dn2Cpp.ReflectionTypeName parsed, List<Module>? assemblies, bool ignoreCase,
        ReflectionApplication app, RResolved result, bool noteNamed, int site = -1)
    {
        if (assemblies is null)
            ResolveNamedTypeScope(parsed, null, ignoreCase, app, result, noteNamed, site);
        else
            foreach (var assembly in assemblies)
                ResolveNamedTypeScope(parsed, new List<Module> { assembly }, ignoreCase, app, result, noteNamed, site);
    }

    private void ResolveNamedTypeScope(global::Dn2Cpp.ReflectionTypeName parsed, List<Module>? scope, bool ignoreCase,
        ReflectionApplication app, RResolved result, bool noteNamed, int site)
    {
        var formed = new RResolved();
        var type = ResolveParsedTypeName(parsed, app.Method.Module, scope, ignoreCase, out string? unknown, app, formed, site);
        if (unknown is not null)
        {
            result.Unknown ??= unknown;
            return;
        }
        if (type is null)
        {
            result.Merge(formed);
            return;
        }
        AddType(result, noteNamed ? NoteNamedType(type) : type, app);
    }

    /// <summary>A resolved name hands the program this type as a typeof would: its
    /// identity and member metadata are emitted, its bodies and initializer are not.</summary>
    private TypeDesc NoteNamedType(TypeDesc type)
    {
        if (type.Kind == TypeKind.External && ResolveExternalClass(type) is { } external)
            type = TypeDesc.MakeClass(external);
        if (!ContainsCanonPlaceholder(type) && !ContainsGenericVar(type) && type.Kind != TypeKind.Template)
        {
            NoteTypeIdentityClosure(type);
            if (type is { Kind: TypeKind.Class, Class: { } named })
                NoteForceEmit(named);
        }
        return type;
    }

    /// <summary>Resolves a parsed name the way Type.GetType searches: the named assembly
    /// when qualified, after the type forwarders it declares (a framework facade the load
    /// set omits forwards to CoreLib), otherwise the calling assembly and then CoreLib. A
    /// miss is a legitimate null result. <paramref name="ignoreCase"/> matches as the
    /// runtime's assembly-scoped lookup does, folding ASCII case. With
    /// <paramref name="app"/>, a closed generic name the program never instantiates forms
    /// as a MakeGenericType over the same arguments would (<see cref="FormClosedType"/>),
    /// reporting at <paramref name="site"/> when that is a site index; what it yields
    /// without the closed class itself lands in <paramref name="formed"/>.
    /// <paramref name="unknown"/> says why a name has no answer the model can name.</summary>
    private TypeDesc? ResolveParsedTypeName(global::Dn2Cpp.ReflectionTypeName name, Module caller, List<Module>? searched,
        bool ignoreCase, out string? unknown, ReflectionApplication? app = null, RResolved? formed = null, int site = -1)
    {
        unknown = null;
        string top = name.TopLevelName;
        if (ignoreCase && (searched is null || name.GenericArguments.Length > 0 || !IsAscii(top)
                || name.NestedNames.Any(n => !IsAscii(n))))
        {
            unknown = "the type name \"" + top + "\" matched ignoring case";
            return null;
        }
        int dot = top.LastIndexOf('.');
        var key = dot < 0 ? ("", top) : (top.Substring(0, dot), top.Substring(dot + 1));
        var scope = new List<Module>();
        if (searched is not null)
            scope.AddRange(searched);
        else if (name.AssemblySimpleName is { } simple)
            scope.AddRange(QualifiedScope(simple, key.Item1, key.Item2));
        else
        {
            scope.Add(caller);
            foreach (var module in Modules)
                if (module.AssemblyName is "System.Private.CoreLib" or "mscorlib" && module != caller)
                    scope.Add(module);
        }
        List<(Module, TypeDefinitionHandle)>? candidates;
        if (ignoreCase)
        {
            candidates = new List<(Module, TypeDefinitionHandle)>();
            foreach (var module in scope)
                foreach (var h in module.Reader.TypeDefinitions)
                {
                    var td = module.Reader.GetTypeDefinition(h);
                    if (!td.GetDeclaringType().IsNil)
                        continue;
                    string ns = module.Reader.GetString(td.Namespace), simpleName = module.Reader.GetString(td.Name);
                    if (string.Equals(ns.Length == 0 ? simpleName : ns + "." + simpleName, top, StringComparison.OrdinalIgnoreCase))
                        candidates.Add((module, h));
                }
        }
        else if (!TypeIndex().TryGetValue(key, out candidates))
            return null;
        (Module Module, TypeDefinitionHandle Handle)? found = null;
        foreach (var module in scope)
        {
            foreach (var candidate in candidates)
                if (candidate.Item1 == module && module.Reader.GetTypeDefinition(candidate.Item2).GetDeclaringType().IsNil)
                {
                    if (found is not null && found.Value.Handle != candidate.Item2)
                    {
                        unknown = "the type name \"" + top + "\", which names more than one type";
                        return null;
                    }
                    found = (candidate.Item1, candidate.Item2);
                }
            if (found is not null)
                break;
        }
        if (found is not { } definition)
            return null;
        var (defModule, handle) = definition;
        foreach (string nestedName in name.NestedNames)
        {
            TypeDefinitionHandle next = default;
            foreach (var nh in defModule.Reader.GetTypeDefinition(handle).GetNestedTypes())
                if (string.Equals(defModule.Reader.GetString(defModule.Reader.GetTypeDefinition(nh).Name), nestedName,
                        ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    if (!next.IsNil)
                    {
                        unknown = "the nested type name \"" + nestedName + "\", which names more than one type";
                        return null;
                    }
                    next = nh;
                }
            if (next.IsNil)
                return null;
            handle = next;
        }
        TypeDesc? type;
        int arity = defModule.Reader.GetTypeDefinition(handle).GetGenericParameters().Count;
        if (name.GenericArguments.Length > 0)
        {
            if (arity != name.GenericArguments.Length)
                return null;
            var args = new TypeDesc[arity];
            for (int i = 0; i < arity; i++)
            {
                var arg = ResolveParsedTypeName(name.GenericArguments[i], caller, null, false, out unknown, app, null, site);
                if (arg is null)
                    return null;
                args[i] = arg;
            }
            if (!InstancesFor(defModule, handle).TryGetValue(args, out var instance))
            {
                if (app is null || FormClosedType(app, site, defModule, handle, args,
                        name.Modifiers.Length == 0 ? formed : null) is not { } made)
                    return null;
                instance = made;
            }
            type = TypeDesc.MakeClass(instance);
        }
        else if (arity > 0)
            type = TypeDesc.MakeTemplate(defModule, handle);
        else
            type = GetTypeDescForDefinition(defModule, handle);
        foreach (int modifier in name.Modifiers)
        {
            if (modifier == global::Dn2Cpp.ReflectionTypeName.SZArray)
                type = TypeDesc.MakeSZArray(type);
            else if (modifier > 0)
                type = TypeDesc.MakeMDArray(type, modifier);
            else
            {
                unknown = "the type name \"" + top + "\" with a by-ref or pointer modifier";
                return null;
            }
        }
        return type;
    }

    /// <summary>The modules a type name qualified by assembly <paramref name="simple"/> is
    /// searched in: that assembly and where its forwarder row for the name leads. A
    /// framework facade the load set omits forwards to CoreLib.</summary>
    private List<Module> QualifiedScope(string simple, string ns, string name)
    {
        var scope = new List<Module>();
        for (int hops = 0; hops <= Modules.Count; hops++)
        {
            bool loaded = false;
            string? next = null;
            foreach (var module in Modules)
                if (string.Equals(module.AssemblyName, simple, StringComparison.OrdinalIgnoreCase))
                {
                    loaded = true;
                    scope.Add(module);
                    var reader = module.Reader;
                    foreach (var h in reader.ExportedTypes)
                    {
                        var exported = reader.GetExportedType(h);
                        if (exported.Implementation.Kind == HandleKind.AssemblyReference
                            && reader.StringComparer.Equals(exported.Name, name)
                            && reader.StringComparer.Equals(exported.Namespace, ns))
                            next = reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)exported.Implementation).Name);
                    }
                }
            if (!loaded && simple is "mscorlib" or "netstandard" or "System.Runtime")
            {
                foreach (var module in Modules)
                    if (module.AssemblyName == "System.Private.CoreLib")
                        scope.Add(module);
                break;
            }
            if (next is null || string.Equals(next, simple, StringComparison.OrdinalIgnoreCase))
                break;
            simple = next;
        }
        return scope;
    }

    /// <summary>The closed class a MakeGenericType of <paramref name="def"/> over
    /// <paramref name="args"/> yields, under the same policy and reports; null when that
    /// yields no such class, with what it yields instead merged into
    /// <paramref name="formed"/>. Without a destination, a finite template tuple closes
    /// through the same constraint proof as a MakeGenericType argument.</summary>
    private ClassInfo? FormClosedType(ReflectionApplication app, int site, Module module, TypeDefinitionHandle def,
        TypeDesc[] args, RResolved? formed)
    {
        var elements = new RResolved[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            elements[i] = new RResolved();
            elements[i].Add(TypeEntity(args[i]));
        }
        var tuple = new RResolved();
        tuple.Add(new REntity { Kind = REntityKind.TypeArray, Number = args.Length, Elements = elements });
        var defs = new RResolved();
        defs.Add(DefinitionEntity(module, def));
        var made = MakeGeneric(app, site, defs, tuple, report: site >= 0);
        foreach (var e in made.Items)
            if (e.Type is { Kind: TypeKind.Class, Class: { } cls } && cls.Module == module && cls.Handle == def)
                return cls;
        if (formed is null)
            foreach (var e in made.Items)
                if (!e.Inherited && e.Kind == REntityKind.Template
                    && ExactTemplateType(e) is { Kind: TypeKind.Class, Class: { } cls })
                    return cls;
        formed?.Merge(made);
        return null;
    }

    /// <summary>Notes the types a Type.GetType or Assembly.GetType site names by a
    /// constant, whether or not anything reads the result, and forms a closed generic
    /// one as MakeGenericType over the same arguments would, so the runtime's name
    /// lookup finds it; the site reports a name it cannot form.</summary>
    private void FormNamedTypes(ReflectionApplication app, int index, RResolved receivers)
    {
        var site = app.Summary.Sites[index];
        var api = site.Api;
        var cases = api.IgnoreCase >= 0 ? BooleanAlternatives(Operand(app, site, api.IgnoreCase), out _) : new List<bool> { false };
        var texts = KnownStrings(Operand(app, site, api.Name), out _);
        if (texts is null)
            return;
        List<Module>? searched = null;
        if (api.Op == ReflectionOp.AssemblyGetType)
        {
            searched = new List<Module>();
            foreach (var e in receivers.Items)
                if (e.Kind == REntityKind.Assembly && e.Assembly is { } module)
                    searched.Add(module);
            if (searched.Count == 0)
                return;
        }
        foreach (string text in texts)
            if (global::Dn2Cpp.ReflectionTypeName.Parse(text) is { } parsed)
                foreach (bool ignoreCase in cases)
                    ResolveNamedType(parsed, searched, ignoreCase, app, new RResolved(), noteNamed: true, index);
    }

    private void ResolveAssembly(ReflectionApplication app, RTerm t, RResolved result)
    {
        if (t.Text is { } text)
        {
            int comma = text.IndexOf(',');
            string simple = (comma < 0 ? text : text.Substring(0, comma)).Trim();
            foreach (var module in Modules)
                if (string.Equals(module.AssemblyName, simple, StringComparison.OrdinalIgnoreCase))
                    result.Add(new REntity { Kind = REntityKind.Assembly, Assembly = module });
            return;
        }
        if (t.Children.Length > 0 && t.Children[0] >= 0)
        {
            var of = Child(app, t, 0);
            foreach (var e in of.Items)
                if (e.Kind == REntityKind.Type && !e.Inherited && e.Type is { } type)
                {
                    while (type.Kind is TypeKind.SZArray or TypeKind.MDArray && type.Element is { } element)
                        type = element;
                    var cls = OwnerClass(type) ?? (type.Kind == TypeKind.External ? ResolveExternalClass(type) : null);
                    if (cls is not null)
                        result.Add(new REntity { Kind = REntityKind.Assembly, Assembly = cls.Module });
                    else if (type.Kind == TypeKind.Template && type.TemplateModule is { } templateModule)
                        result.Add(new REntity { Kind = REntityKind.Assembly, Assembly = templateModule });
                }
                else if (e.Kind is REntityKind.Definition or REntityKind.Template && e.Assembly is { } module)
                    result.Add(new REntity { Kind = REntityKind.Assembly, Assembly = module });
            result.Unknown = of.Unknown;
            return;
        }
        result.Add(new REntity { Kind = REntityKind.Assembly, Assembly = t.Number == 2 ? AppModule : app.Method.Module });
    }

    private const int EveryMemberType = (int)(MemberTypes.Constructor | MemberTypes.Event | MemberTypes.Field
        | MemberTypes.Method | MemberTypes.Property | MemberTypes.TypeInfo | MemberTypes.NestedType);

    /// <summary>The member kinds a GetMember MemberTypes operand may select, as a
    /// <see cref="MemberTypes"/> mask; null when it may select every kind.</summary>
    private static int? MemberTypesMask(RResolved value)
    {
        if (value.Unknown is not null || value.Items.Count == 0)
            return null;
        int mask = 0;
        foreach (var e in value.Items)
        {
            if (e.Kind != REntityKind.Int)
                return null;
            mask |= (int)e.Number;
        }
        return (mask & EveryMemberType) == EveryMemberType ? null : mask;
    }

    /// <summary>The lookup kinds a GetMember over a template's clone asks for: the lookup's
    /// own kind when <paramref name="memberTypes"/> leaves every kind open, else one kind
    /// per kind the mask names.</summary>
    private static List<LookupMemberKind?> LookupKinds(LookupMemberKind? kind, int? memberTypes)
    {
        var kinds = new List<LookupMemberKind?>();
        if (kind is not null || memberTypes is not { } mask)
        {
            kinds.Add(kind);
            return kinds;
        }
        if ((mask & (int)MemberTypes.Method) != 0)
            kinds.Add(LookupMemberKind.Method);
        if ((mask & (int)MemberTypes.Constructor) != 0)
            kinds.Add(LookupMemberKind.Constructor);
        if ((mask & (int)MemberTypes.Field) != 0)
            kinds.Add(LookupMemberKind.Field);
        if ((mask & (int)MemberTypes.Property) != 0)
            kinds.Add(LookupMemberKind.Property);
        if ((mask & (int)MemberTypes.Event) != 0)
            kinds.Add(LookupMemberKind.Event);
        if ((mask & (int)(MemberTypes.NestedType | MemberTypes.TypeInfo)) != 0)
            kinds.Add(LookupMemberKind.NestedType);
        return kinds;
    }

    /// <summary>The members of <paramref name="selected"/> whose kind a GetMember
    /// MemberTypes mask names; all of them when the mask is null.</summary>
    private static List<REntity> OfMemberTypes(List<REntity> selected, int? memberTypes)
    {
        if (memberTypes is not { } mask)
            return selected;
        var kept = new List<REntity>();
        foreach (var e in selected)
        {
            var row = e.Metadata ?? e;
            var memberType = row.Kind switch
            {
                REntityKind.Method => row.Method is { Name: ".ctor" or ".cctor" } ? MemberTypes.Constructor : MemberTypes.Method,
                REntityKind.Field => MemberTypes.Field,
                REntityKind.Property => MemberTypes.Property,
                REntityKind.Event => MemberTypes.Event,
                _ => MemberTypes.NestedType | MemberTypes.TypeInfo,
            };
            if ((mask & (int)memberType) != 0)
                kept.Add(e);
        }
        return kept;
    }

    private void ResolveLookup(ReflectionApplication app, RTerm t, RResolved result)
    {
        var op = (ReflectionOp)t.Number;
        var owners = Child(app, t, ReflectionSummary.LookupOwner);
        var nameValue = t.Children.Length > ReflectionSummary.LookupName && t.Children[ReflectionSummary.LookupName] >= 0
            ? Child(app, t, ReflectionSummary.LookupName) : null;
        bool namesComplete = true;
        var names = nameValue is null ? null : KnownStrings(nameValue, out namesComplete);
        var flagSet = FlagAlternatives(Child(app, t, ReflectionSummary.LookupFlags), ReflectionApis.DefaultLookup,
            out bool flagsKnown);
        if (op == ReflectionOp.CreateDelegateNamed)
        {
            var delegates = t.Children.Length > ReflectionSummary.LookupDelegateType && t.Children[ReflectionSummary.LookupDelegateType] >= 0
                ? Child(app, t, ReflectionSummary.LookupDelegateType) : RResolved.Empty;
            var selected = NamedBindingMembers(owners, names, flagSet, delegates);
            result.AddRange(NoteListedRows(selected.Items));
            result.Unknown ??= selected.Unknown;
            if (owners.Unknown is not null)
                result.Unknown ??= "named delegate target comes from " + owners.Unknown;
            else if (!namesComplete)
                result.Unknown ??= "named delegate method name comes from " + (nameValue?.Unknown ?? "a value not known statically");
            else if (!flagsKnown)
                result.Unknown ??= "named delegate binding flags are not known statically";
            return;
        }
        var types = t.Children.Length > ReflectionSummary.LookupTypes && t.Children[ReflectionSummary.LookupTypes] >= 0
            ? Child(app, t, ReflectionSummary.LookupTypes) : null;
        int arity = -1;
        if (t.Children.Length > ReflectionSummary.LookupArity && t.Children[ReflectionSummary.LookupArity] >= 0)
            arity = SingleInt(Child(app, t, ReflectionSummary.LookupArity)) is long a ? (int)a : -1;
        var returns = t.Children.Length > ReflectionSummary.LookupReturnType && t.Children[ReflectionSummary.LookupReturnType] >= 0
            ? Child(app, t, ReflectionSummary.LookupReturnType) : null;
        int? memberTypes = op == ReflectionOp.GetMember
            && t.Children.Length > ReflectionSummary.LookupMemberTypes && t.Children[ReflectionSummary.LookupMemberTypes] >= 0
            ? MemberTypesMask(Child(app, t, ReflectionSummary.LookupMemberTypes)) : null;
        LookupMemberKind? kind = op switch
        {
            ReflectionOp.GetMethod => LookupMemberKind.Method,
            ReflectionOp.GetConstructor or ReflectionOp.TypeInitializer => LookupMemberKind.Constructor,
            ReflectionOp.GetProperty => LookupMemberKind.Property,
            ReflectionOp.GetField => LookupMemberKind.Field,
            ReflectionOp.GetEvent => LookupMemberKind.Event,
            ReflectionOp.GetNestedType => LookupMemberKind.NestedType,
            _ => null,
        };
        if (op == ReflectionOp.TypeInitializer)
            flagSet = new List<int> { ReflectionApis.TypeInitializerLookup };
        string? nameRuleUnknown = null;
        foreach (var e in owners.Items)
            foreach (int flags in flagSet)
            {
                List<ClassInfo> interfaces;
                if (e.Kind == REntityKind.Template && e.Assembly is { } templateModule)
                {
                    if (op == ReflectionOp.GetInterface)
                        interfaces = TemplateInterfaces(app, templateModule, e.Definition, result);
                    else
                    {
                        if (names is null && nameValue is not null)
                            continue;
                        foreach (string? name in (IEnumerable<string?>?)names ?? new string?[] { null })
                            foreach (var memberKind in LookupKinds(kind, memberTypes))
                                if (memberKind == LookupMemberKind.NestedType)
                                    result.AddRange(SelectNestedTypes(templateModule, e.Definition,
                                        name is null ? null : ReflectionLookupRules.LookupName(name, false, out _), flags, false, true));
                                else
                                    result.Add(new REntity
                                    {
                                        Kind = REntityKind.TemplateMember, Assembly = templateModule, Definition = e.Definition,
                                        Tuple = e.Tuple,
                                        Lookup = new TemplateLookup(memberKind, name, flags, types, arity, op == ReflectionOp.GetMember, returns, op is ReflectionOp.GetMethod or ReflectionOp.GetField),
                                    });
                        continue;
                    }
                }
                else
                {
                    // A clone inherits its closed base's members, never a nested type or a
                    // constructor.
                    if (e.Inherited && ((flags & ReflectionApis.DeclaredOnly) != 0
                            || kind is LookupMemberKind.Constructor or LookupMemberKind.NestedType))
                        continue;
                    if (e.Kind == REntityKind.Definition && e.Assembly is { } definitionModule)
                    {
                        if (op == ReflectionOp.GetInterface)
                        {
                            interfaces = DefinitionInterfaces(definitionModule, e.Definition, result);
                            SelectInterfaces(interfaces, names, flags, result, ref nameRuleUnknown);
                            continue;
                        }
                        if (names is null && nameValue is not null)
                            continue;
                        foreach (string? name in (IEnumerable<string?>?)names ?? new string?[] { null })
                            result.AddRange(NoteListedRows(OfMemberTypes(DefinitionMembers(definitionModule, e.Definition, null,
                                new TemplateLookup(kind, name, flags, types, arity, op == ReflectionOp.GetMember,
                                    returns, op is ReflectionOp.GetMethod or ReflectionOp.GetField)), memberTypes)));
                        continue;
                    }
                    var type = e.Kind == REntityKind.Type ? e.Type : null;
                    if (type is not { Kind: TypeKind.Class, Class: { } cls })
                        continue;
                    NoteReflectionMetadataOwner(cls);
                    if (op != ReflectionOp.GetInterface)
                    {
                        if (names is null && nameValue is not null)
                            continue;
                        if (names is null)
                            result.AddRange(NoteListedRows(OfMemberTypes(
                                LookupOwnerMembers(owners, e, cls, new TemplateLookup(kind, null, flags, types, arity, Returns: returns)), memberTypes)));
                        else
                            foreach (string name in names)
                            {
                                var members = LookupOwnerMembers(owners, e, cls, new TemplateLookup(kind, name, flags, types, arity,
                                    op == ReflectionOp.GetMember, returns, op is ReflectionOp.GetMethod or ReflectionOp.GetField));
                                result.AddRange(NoteListedRows(OfMemberTypes(members, memberTypes)));
                            }
                        continue;
                    }
                    interfaces = ReflectedInterfaces(cls);
                }
                SelectInterfaces(interfaces, names, flags, result, ref nameRuleUnknown);
            }
        string lookup = op + (names is { Count: > 0 } ? "(\"" + names[0] + "\")" : "()");
        if (OwnerUnknown(owners) is { } ownerUnknown)
        {
            result.Unknown = lookup + " on a Type that comes from " + ownerUnknown;
            if (names is { Count: > 0 } && namesComplete && flagsKnown && !AnyIgnoreCase(flagSet))
            {
                result.UnknownMemberKind = kind switch
                {
                    LookupMemberKind.Method => "method",
                    LookupMemberKind.Property => "property",
                    LookupMemberKind.Field => "field",
                    LookupMemberKind.Event => "event",
                    _ => null,
                };
                if (result.UnknownMemberKind is not null)
                    result.UnknownMembers = names;
            }
        }
        else if (nameValue is not null && !namesComplete)
            result.Unknown = op + " with a name that comes from " + (nameValue.Unknown ?? "a value not known statically");
        else if (!flagsKnown)
            result.Unknown = lookup + " with binding flags not known statically";
        else if (nameRuleUnknown is not null)
            result.Unknown = nameRuleUnknown;
    }

    /// <summary>Each owner/name/flags lookup returns one matching interface or throws
    /// when several match. Unknown name comparisons retain their possible results.</summary>
    private static void SelectInterfaces(List<ClassInfo> interfaces, List<string>? names, int flags,
        RResolved result, ref string? unknown)
    {
        if (names is null)
        {
            foreach (var itf in interfaces)
                result.Add(TypeEntity(TypeDesc.MakeClass(itf)));
            return;
        }
        foreach (string name in names)
        {
            var matches = new List<ClassInfo>();
            int definite = 0;
            foreach (var itf in interfaces)
            {
                bool? match = InterfaceNameMatches(itf, name, (flags & 1) != 0);
                if (match == false)
                    continue;
                matches.Add(itf);
                if (match == true)
                    definite++;
                else
                    unknown ??= "GetInterface(\"" + name + "\") ignoring case over a name outside ASCII";
            }
            if (definite > 1)
                continue;
            foreach (var itf in matches)
                result.Add(TypeEntity(TypeDesc.MakeClass(itf)));
        }
    }

    /// <summary>An open definition exposes its fixed interfaces without creating an
    /// executable owner. Parameter-dependent interfaces remain unknown; their fixed
    /// inherited interfaces can still be projected from detached signature contexts.</summary>
    private List<ClassInfo> DefinitionInterfaces(Module module, TypeDefinitionHandle def, RResolved result)
    {
        var interfaces = new List<ClassInfo>();
        var pending = new List<ClassInfo> { ReflectionDefinitionOwner(module, def, null) };
        var seen = new HashSet<ClassInfo>();
        var provider = new ReflectionDefinitionSignatureProvider(this);
        for (int i = 0; i < pending.Count; i++)
        {
            var owner = pending[i];
            if (!seen.Add(owner) || owner.Handle.IsNil)
                continue;
            if (seen.Count > RResolved.MaxItems)
            {
                result.Unknown ??= RResolved.TooBroad;
                break;
            }
            var reader = owner.Module.Reader;
            var td = reader.GetTypeDefinition(owner.Handle);
            TypeDesc? Decode(EntityHandle handle) => handle.IsNil ? null
                : handle.Kind == HandleKind.TypeSpecification
                    ? reader.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(provider, owner.Context)
                    : ResolveTypeTokenForScan(owner.Module, handle, owner.Context);
            var baseType = Decode(td.BaseType);
            if (baseType is { Kind: TypeKind.Class, Class: { } parent })
                pending.Add(parent);
            else if (baseType is { Kind: TypeKind.External or TypeKind.ExternalGeneric })
                result.Unknown ??= "a base whose interfaces are not known statically";
            foreach (var ih in td.GetInterfaceImplementations())
            {
                var type = Decode(reader.GetInterfaceImplementation(ih).Interface);
                if (type is { Kind: TypeKind.Class, Class: { } itf })
                    pending.Add(itf);
                if (type is null || ContainsGenericVar(type) || ContainsCanonPlaceholder(type))
                {
                    result.Unknown ??= "an interface whose type depends on an open generic parameter";
                    continue;
                }
                if (ExecutableDefinitionType(type) is { Kind: TypeKind.Class, Class: { } fixedInterface })
                {
                    if (!interfaces.Contains(fixedInterface))
                        interfaces.Add(fixedInterface);
                }
                else
                    result.Unknown ??= "an interface whose executable type is not known statically";
            }
        }
        return interfaces;
    }

    private TypeDesc? ExecutableDefinitionType(TypeDesc type)
    {
        if (!ContainsReflectionMetadataView(type))
            return type;
        if (type is { Kind: TypeKind.Class, Class: { } cls })
        {
            var tuple = new TypeDesc[cls.Context.TypeArgs.Length];
            for (int i = 0; i < tuple.Length; i++)
            {
                if (ExecutableDefinitionType(cls.Context.TypeArgs[i]) is not { } argument)
                    return null;
                tuple[i] = argument;
            }
            return StaticReflectionType(cls.Module, cls.Handle, tuple) is { } closed ? TypeDesc.MakeClass(closed) : null;
        }
        if (type.Kind is TypeKind.SZArray or TypeKind.MDArray
            && ExecutableDefinitionType(type.Element!) is { } element)
            return type.Kind == TypeKind.SZArray ? TypeDesc.MakeSZArray(element) : TypeDesc.MakeMDArray(element, type.Rank);
        return null;
    }

    /// <summary>The interfaces Type.GetInterfaces lists for <paramref name="cls"/>: those
    /// every class of its base chain declares and every interface they inherit; for an
    /// interface, the interfaces it inherits.</summary>
    private List<ClassInfo> ReflectedInterfaces(ClassInfo cls)
    {
        var closure = new List<ClassInfo>();
        var pending = BaseChain(cls);
        for (int i = 0; i < pending.Count; i++)
            foreach (var itf in EnsureShape(pending[i]).Interfaces.ToList())
                if (!closure.Contains(itf))
                {
                    closure.Add(itf);
                    pending.Add(itf);
                }
        return closure;
    }

    /// <summary>The interfaces a clone of <paramref name="def"/> lists: each placeholder
    /// level's own and those they inherit. An interface naming a type parameter leaves
    /// <paramref name="result"/> unknown; no eligible template has one.</summary>
    private List<ClassInfo> TemplateInterfaces(ReflectionApplication app, Module module, TypeDefinitionHandle def,
        RResolved result)
    {
        var interfaces = new List<ClassInfo>();
        foreach (var (lm, lh) in TemplateLevelDefinitions(module, def))
            foreach (var ih in lm.Reader.GetTypeDefinition(lh).GetInterfaceImplementations())
            {
                var declared = new RResolved();
                AddType(declared, ResolveTypeTokenForScan(lm, lm.Reader.GetInterfaceImplementation(ih).Interface,
                    GenericContext.Empty), app);
                result.Unknown ??= declared.Unknown;
                foreach (var itf in ClassesOf(declared))
                {
                    if (!interfaces.Contains(itf))
                        interfaces.Add(itf);
                    foreach (var inherited in ReflectedInterfaces(itf))
                        if (!interfaces.Contains(inherited))
                            interfaces.Add(inherited);
                }
            }
        return interfaces;
    }

    /// <summary>Whether Type.GetInterface(<paramref name="query"/>) matches
    /// <paramref name="itf"/>: the text after the last '.' against the metadata name, arity
    /// suffix kept, and the text before it, when there is a dot, against the namespace, a
    /// nested interface's being its outermost declaring type's. Ignoring case, the name
    /// compares as the runtime's ASCII-only folding does and the namespace ordinally
    /// ignoring case; null when such a name leaves ASCII.</summary>
    private static bool? InterfaceNameMatches(ClassInfo itf, string query, bool ignoreCase)
    {
        string name = itf.Name, space = itf.Namespace;
        if (!itf.Handle.IsNil)
        {
            var reader = itf.Module.Reader;
            var td = reader.GetTypeDefinition(itf.Handle);
            name = reader.GetString(td.Name);
            while (!td.GetDeclaringType().IsNil)
                td = reader.GetTypeDefinition(td.GetDeclaringType());
            space = reader.GetString(td.Namespace);
        }
        int dot = query.LastIndexOf('.');
        string queryName = dot < 0 ? query : query.Substring(dot + 1);
        if (dot >= 0 && (string.IsNullOrEmpty(space)
                || !string.Equals(space, query.Substring(0, dot), ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
            return false;
        if (!ignoreCase)
            return string.Equals(name, queryName, StringComparison.Ordinal);
        if (!IsAscii(queryName))
            return null;
        return IsAscii(name) && string.Equals(name, queryName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAscii(string text)
    {
        foreach (char c in text)
            if (c > 0x7F)
                return false;
        return true;
    }

    /// <summary>The base of generic definition <paramref name="def"/> when that base is
    /// closed, else null. A definition's own members are never invocable, but the members it
    /// inherits from a closed base are, on any instance of that base.</summary>
    private ClassInfo? ClosedDefinitionBase(Module module, TypeDefinitionHandle def)
    {
        var handle = module.Reader.GetTypeDefinition(def).BaseType;
        if (handle.IsNil)
            return null;
        if (handle.Kind == HandleKind.TypeSpecification
            && module.Reader.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(ParameterMentionProbe.Instance, null))
            return null;
        return ResolveTypeTokenForScan(module, handle, GenericContext.Empty) is { Kind: TypeKind.Class, Class: { } cls } ? cls : null;
    }

    private void ResolveEnumeration(ReflectionApplication app, RTerm t, RResolved result)
    {
        var op = (ReflectionOp)t.Number;
        var owners = Child(app, t, 0);
        result.Unknown = OwnerUnknown(owners) is { } ownerUnknown ? op + "() on a value that comes from " + ownerUnknown : null;
        if (op is ReflectionOp.GetCustomAttributes or ReflectionOp.GetCustomAttributesData)
        {
            result.Add(new REntity { Kind = REntityKind.Attribute });
            return;
        }
        var nameValue = t.Children.Length > 2 && t.Children[2] >= 0 ? Child(app, t, 2) : null;
        bool namesComplete = true;
        var names = nameValue is null ? null : KnownStrings(nameValue, out namesComplete);
        if (!namesComplete)
            result.Unknown ??= op + " with a name that comes from " + (nameValue!.Unknown ?? "a value not known statically");
        if (nameValue is not null && names is null)
            return;
        var flagSet = FlagAlternatives(Child(app, t, 1),
            op == ReflectionOp.GetConstructors ? ReflectionApis.PublicInstance : ReflectionApis.DefaultLookup, out _);
        foreach (var e in owners.Items)
        {
            switch (op)
            {
                case ReflectionOp.GetParameters when e.Kind is REntityKind.DefinitionMember or REntityKind.TemplateMember:
                {
                    var members = e.Kind == REntityKind.TemplateMember ? TemplateMetadataMembers(e) : new List<REntity> { e };
                    foreach (var member in members)
                        if ((member.Metadata ?? member) is { Kind: REntityKind.Method, Method: { } method })
                            for (int i = 0; i < ReflectionSignature(method).ParameterTypes.Length; i++)
                                result.Add(new REntity { Kind = REntityKind.DefinitionMember,
                                    Metadata = new REntity { Kind = REntityKind.Parameter, Method = method, Number = i } });
                    continue;
                }
                case ReflectionOp.GetParameters when e.Kind == REntityKind.Method:
                {
                    int count = e.Method!.Signature.ParameterTypes.Length;
                    for (int i = 0; i < count; i++)
                        result.Add(new REntity { Kind = REntityKind.Parameter, Method = e.Method, Number = i });
                    continue;
                }
                case ReflectionOp.AssemblyGetTypes when e.Kind == REntityKind.Assembly && e.Assembly is { } module:
                    foreach (var h in module.Reader.TypeDefinitions)
                        if (!module.Reader.GetTypeDefinition(h).Name.IsNil && module.Reader.GetString(module.Reader.GetTypeDefinition(h).Name) != "<Module>")
                            result.Add(DefinitionEntity(module, h));
                    continue;
                case ReflectionOp.GetGenericArguments when e.Kind == REntityKind.Template && e.Tuple is { } tuple:
                {
                    var elements = new RResolved[tuple.Length];
                    for (int i = 0; i < elements.Length; i++)
                    {
                        elements[i] = new RResolved();
                        AddType(elements[i], tuple[i], app);
                    }
                    result.Add(new REntity { Kind = REntityKind.TypeArray, Number = elements.Length, Elements = elements });
                    continue;
                }
                case ReflectionOp.GetMethods or ReflectionOp.GetConstructors or ReflectionOp.GetProperties
                    or ReflectionOp.GetEvents or ReflectionOp.GetFields or ReflectionOp.GetMembers
                    when e.Kind is REntityKind.Template or REntityKind.Definition:
                {
                    LookupMemberKind? memberKind = op switch
                    {
                        ReflectionOp.GetMethods => LookupMemberKind.Method,
                        ReflectionOp.GetConstructors => LookupMemberKind.Constructor,
                        ReflectionOp.GetProperties => LookupMemberKind.Property,
                        ReflectionOp.GetFields => LookupMemberKind.Field,
                        ReflectionOp.GetEvents => LookupMemberKind.Event,
                        _ => null,
                    };
                    foreach (int flags in flagSet)
                        foreach (string? name in (IEnumerable<string?>?)names ?? new string?[] { null })
                            if (e.Kind == REntityKind.Definition && e.Assembly is { } definitionModule)
                                result.AddRange(NoteListedRows(DefinitionMembers(definitionModule, e.Definition, null,
                                    new TemplateLookup(memberKind, name, flags, null, -1))));
                            else
                                result.Add(new REntity
                                {
                                    Kind = REntityKind.TemplateMember, Assembly = e.Assembly, Definition = e.Definition,
                                    Tuple = e.Tuple, Lookup = new TemplateLookup(memberKind, name, flags, null, -1),
                                });
                    continue;
                }
                // A template level's interfaces name no type parameter
                // (RuntimeTemplateMayServe); the clone's closed base lists its own.
                case ReflectionOp.GetInterfaces when e.Kind == REntityKind.Definition && e.Assembly is { } definitionModule:
                    foreach (var itf in DefinitionInterfaces(definitionModule, e.Definition, result))
                        result.Add(TypeEntity(TypeDesc.MakeClass(itf)));
                    continue;
                case ReflectionOp.GetInterfaces when e.Kind == REntityKind.Template && e.Assembly is { } templateModule:
                    foreach (var itf in TemplateInterfaces(app, templateModule, e.Definition, result))
                        result.Add(TypeEntity(TypeDesc.MakeClass(itf)));
                    continue;
                case ReflectionOp.GetNestedTypes when e.Kind == REntityKind.Template && e.Assembly is { } nestingModule:
                    foreach (int flags in flagSet)
                        result.AddRange(SelectNestedTypes(nestingModule, e.Definition, null, flags, false, true));
                    continue;
            }
            // Any other listing of a clone or of its member is not known ahead of time.
            if (e.Kind is REntityKind.Template or REntityKind.TemplateMember)
            {
                result.Unknown ??= op + "() on a runtime-instantiated generic type";
                continue;
            }
            if (e.Inherited && op is ReflectionOp.GetConstructors or ReflectionOp.GetGenericArguments)
                continue;
            var type = e.Kind == REntityKind.Type ? e.Type : null;
            if (type is not { Kind: TypeKind.Class, Class: { } cls })
                continue;
            NoteReflectionMetadataOwner(cls);
            switch (op)
            {
                case ReflectionOp.GetGenericArguments:
                {
                    // One vector per class, so a MakeGenericType over it keeps the tuple.
                    var elements = new RResolved[cls.Context.TypeArgs.Length];
                    for (int i = 0; i < elements.Length; i++)
                    {
                        elements[i] = new RResolved();
                        AddType(elements[i], cls.Context.TypeArgs[i], app);
                    }
                    result.Add(new REntity { Kind = REntityKind.TypeArray, Number = elements.Length, Elements = elements });
                    break;
                }
                case ReflectionOp.GetInterfaces:
                    foreach (var itf in ReflectedInterfaces(cls))
                        result.Add(TypeEntity(TypeDesc.MakeClass(itf)));
                    break;
                case ReflectionOp.GetDefaultMembers:
                    if (DefaultMemberName(cls) is { } defaultName)
                        foreach (int flags in flagSet)
                            result.AddRange(NoteListedRows(SelectMembers(cls, null, defaultName, flags, null, -1, allowPrefix: true)));
                    break;
                default:
                    LookupMemberKind? kind = op switch
                    {
                        ReflectionOp.GetMethods => LookupMemberKind.Method,
                        ReflectionOp.GetConstructors => LookupMemberKind.Constructor,
                        ReflectionOp.GetProperties => LookupMemberKind.Property,
                        ReflectionOp.GetFields => LookupMemberKind.Field,
                        ReflectionOp.GetEvents => LookupMemberKind.Event,
                        ReflectionOp.GetNestedTypes => LookupMemberKind.NestedType,
                        _ => null,
                    };
                    foreach (int flags in flagSet)
                        foreach (string? name in (IEnumerable<string?>?)names ?? new string?[] { null })
                            result.AddRange(NoteListedRows(LookupOwnerMembers(owners, e, cls,
                                new TemplateLookup(kind, name, flags, null, -1))));
                    break;
            }
        }
    }

    /// <summary>The member name the nearest [DefaultMember] up <paramref name="cls"/>'s base
    /// chain declares, which GetDefaultMembers looks up; null when none declares one.</summary>
    private string? DefaultMemberName(ClassInfo cls)
    {
        foreach (var c in BaseChain(cls))
        {
            if (c.Handle.IsNil)
                continue;
            var reader = c.Module.Reader;
            foreach (var handle in reader.GetTypeDefinition(c.Handle).GetCustomAttributes())
            {
                var attribute = reader.GetCustomAttribute(handle);
                EntityHandle parent = default;
                if (attribute.Constructor.Kind == HandleKind.MemberReference)
                    parent = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
                else if (attribute.Constructor.Kind == HandleKind.MethodDefinition)
                    parent = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
                string? parentName = parent.Kind switch
                {
                    HandleKind.TypeReference => RawSignatureProvider.TypeReferenceName(reader, (TypeReferenceHandle)parent),
                    HandleKind.TypeDefinition => RawSignatureProvider.TypeDefinitionName(reader, (TypeDefinitionHandle)parent),
                    _ => null,
                };
                if (parentName != "System.Reflection.DefaultMemberAttribute")
                    continue;
                try
                {
                    var blob = reader.GetBlobReader(attribute.Value);
                    if (blob.RemainingBytes >= 2 && blob.ReadUInt16() == 1)
                        return blob.ReadSerializedString();
                }
                catch (BadImageFormatException)
                {
                    // A malformed blob names no member.
                }
            }
        }
        return null;
    }

    private RResolved NamedTypes(ReflectionApplication app, RSite site)
    {
        var api = site.Api;
        var result = new RResolved();
        var names = Operand(app, site, api.Name);
        var assemblies = new List<Module>();
        if (api.Op == ReflectionOp.AssemblyCreateInstance)
        {
            var receiver = Operand(app, site, ReflectionApi.Receiver);
            foreach (var e in receiver.Items)
                if (e.Kind == REntityKind.Assembly && e.Assembly is { } a)
                    assemblies.Add(a);
            if (receiver.Unknown is not null)
                result.Unknown = "an assembly that comes from " + receiver.Unknown;
        }
        else if (api.AssemblyName >= 0)
        {
            var assemblyOperand = Operand(app, site, api.AssemblyName);
            var assemblyNames = KnownStrings(assemblyOperand, out _);
            if (assemblyOperand.Unknown is not null
                || assemblyOperand.Items.Exists(e => e.Kind is not (REntityKind.Str or REntityKind.Null)))
                result.Unknown = "an assembly name not known statically";
            // A null assembly name searches the calling assembly.
            if (assemblyOperand.Items.Exists(e => e.Kind == REntityKind.Null))
                assemblies.Add(app.Method.Module);
            if (assemblyNames is not null)
                foreach (string assemblyName in assemblyNames)
                {
                    // An assembly display name binds by its simple name.
                    int comma = assemblyName.IndexOf(',');
                    string simple = (comma < 0 ? assemblyName : assemblyName.Substring(0, comma)).Trim();
                    foreach (var module in Modules)
                        if (string.Equals(module.AssemblyName, simple, StringComparison.OrdinalIgnoreCase))
                            assemblies.Add(module);
                }
        }
        var cases = new List<bool> { false };
        if (api.IgnoreCase >= 0)
        {
            var operand = Operand(app, site, api.IgnoreCase);
            cases = BooleanAlternatives(operand, out bool complete);
            if (!complete)
                result.Unknown ??= "an ignoreCase value that comes from " + (operand.Unknown ?? "a value not known statically");
        }
        var texts = KnownStrings(names, out bool textsComplete);
        if (!textsComplete)
            result.Unknown ??= "a type name that comes from " + (names.Unknown ?? "a value not known statically");
        if (texts is null)
            return result;
        foreach (string text in texts)
            if (global::Dn2Cpp.ReflectionTypeName.Parse(text) is { } parsed)
                foreach (bool ignoreCase in cases)
                    ResolveNamedType(parsed, assemblies, ignoreCase, app, result, noteNamed: false, app.Site);
        return result;
    }

    // ---- value helpers ----

    private static List<ClassInfo> ClassesOf(RResolved value)
    {
        var classes = new List<ClassInfo>();
        foreach (var e in value.Items)
            if (e.Kind == REntityKind.Type && e.Type is { Kind: TypeKind.Class, Class: { } cls })
                classes.Add(cls);
        return classes;
    }

    /// <summary>Why a value that should be a reflection owner is not known. An object is
    /// no owner: a Type or member instance the program constructs answers lookups by its
    /// own overrides, which no metadata row describes.</summary>
    private static string? OwnerUnknown(RResolved owners)
    {
        if (owners.Unknown is not null)
            return owners.Unknown;
        foreach (var e in owners.Items)
            if (e.Kind == REntityKind.Object)
                return "an object the summary cannot read as a reflection owner";
        return null;
    }

    // The classes a remedy names: a delegate's target object names its own class.
    private static List<ClassInfo> RemedyOwners(RResolved value)
    {
        var owners = new List<ClassInfo>();
        foreach (var e in value.Items)
            if (e.Kind is REntityKind.Type or REntityKind.Object && e.Type is { Kind: TypeKind.Class, Class: { } cls }
                && !owners.Contains(cls))
                owners.Add(cls);
        return owners;
    }

    /// <summary>The name a link.xml <c>&lt;type fullname&gt;</c> selector matches for
    /// <paramref name="cls"/>'s definition, as <c>PreservationReader</c> spells type
    /// definitions: the namespace, '+' between nested types and backtick arity.</summary>
    internal static string PreservationTypeName(ClassInfo cls) =>
        cls.Handle.IsNil ? cls.FullName : MethodCompiler.OpenDefBacktickName(cls.Module, cls.Handle) ?? cls.FullName;

    /// <summary>The strings a value can be, or null when any alternative is not a constant
    /// string. A null alternative contributes nothing.</summary>
    private static List<string>? StringsOf(RResolved value) =>
        KnownStrings(value, out bool complete) is { } strings && complete ? strings : null;

    /// <summary>The constant strings among a value's alternatives, or null when there is
    /// none. <paramref name="complete"/> is false when some alternative is not a constant
    /// string: a site selects by the known names and still reports the rest.</summary>
    private static List<string>? KnownStrings(RResolved value, out bool complete)
    {
        complete = value.Unknown is null;
        var strings = new List<string>();
        foreach (var e in value.Items)
        {
            if (e.Kind == REntityKind.Str && e.Text is { } s)
            {
                if (!strings.Contains(s))
                    strings.Add(s);
            }
            else if (e.Kind != REntityKind.Null)
                complete = false;
        }
        if (strings.Count > 0)
            return strings;
        complete = false;
        return null;
    }

    /// <summary>The binding flags a value can be, one entry per alternative. A lookup
    /// selects under each and unions the results: a narrowing bit such as DeclaredOnly
    /// in one alternative must not narrow what another returns. An unknown value adds
    /// <see cref="AnyLookup"/>.</summary>
    private static List<int> FlagAlternatives(RResolved value, int absent, out bool known)
    {
        known = value.Unknown is null;
        var flags = new List<int>();
        if (value.Items.Count == 0 && known)
        {
            flags.Add(absent);
            return flags;
        }
        foreach (var e in value.Items)
            if (e.Kind == REntityKind.Int && !flags.Contains((int)e.Number))
                flags.Add((int)e.Number);
        if (!known && !flags.Contains(AnyLookup))
            flags.Add(AnyLookup);
        return flags;
    }

    /// <summary>Flags under which a lookup selects every member some binding flags value
    /// can return: each visibility and storage, inherited statics, and names that differ
    /// only in case.</summary>
    private const int AnyLookup = ReflectionApis.Everything | ReflectionApis.FlattenHierarchy | 1;

    private static bool AnyIgnoreCase(List<int> flags)
    {
        foreach (int f in flags)
            if ((f & 1) != 0)
                return true;
        return false;
    }

    private static long? SingleInt(RResolved value)
    {
        if (value.Unknown is not null || value.Items.Count != 1 || value.Items[0].Kind != REntityKind.Int)
            return null;
        return value.Items[0].Number;
    }

    private static bool MayBeTrue(RResolved value)
    {
        if (value.Unknown is not null)
            return true;
        foreach (var e in value.Items)
            if (e.Kind == REntityKind.Int && e.Number != 0)
                return true;
        return false;
    }

    private static List<bool> BooleanAlternatives(RResolved value, out bool complete)
    {
        complete = value.Unknown is null;
        var choices = new List<bool>();
        foreach (var e in value.Items)
            if (e.Kind == REntityKind.Int)
            {
                bool choice = e.Number != 0;
                if (!choices.Contains(choice))
                    choices.Add(choice);
            }
            else
                complete = false;
        return choices;
    }

    /// <summary>The one length every alternative array has, or -1.</summary>
    private static int KnownLength(RResolved value)
    {
        if (value.Unknown is not null || value.Items.Count == 0)
            return -1;
        int length = -2;
        foreach (var e in value.Items)
        {
            int l = e.Kind == REntityKind.TypeArray && e.Number >= 0 ? (int)e.Number
                : e.Kind == REntityKind.Null ? 0 : -1;
            if (l < 0 || length != -2 && length != l)
                return -1;
            length = l;
        }
        return length < 0 ? -1 : length;
    }

    // ---- diagnostics ----

    private static string Subject(ReflectionApi api) => api.Op switch
    {
        ReflectionOp.MethodInvoke => "MethodInfo",
        ReflectionOp.PropertyGet or ReflectionOp.PropertySet => "PropertyInfo",
        ReflectionOp.EventAdd or ReflectionOp.EventRemove => "EventInfo",
        ReflectionOp.FieldGet or ReflectionOp.FieldSet => "FieldInfo",
        ReflectionOp.CtorInvoke => "ConstructorInfo",
        ReflectionOp.DynamicInvoke => "delegate",
        _ when api.Kind is ReflectionOpKind.Lookup or ReflectionOpKind.Enumerate => "the reflected value",
        _ => "Type",
    };

    private static string? MemberKindOf(ReflectionOp op) => op switch
    {
        ReflectionOp.PropertyGet or ReflectionOp.PropertySet => "property",
        ReflectionOp.EventAdd or ReflectionOp.EventRemove => "event",
        ReflectionOp.FieldGet or ReflectionOp.FieldSet => "field",
        _ => "method",
    };

    private string DescribeUnknown(ReflectionApplication app, RUnknown u)
    {
        var reader = app.Method.Module.Reader;
        string Member() => new SummaryRenderer(app.Summary, reader).Member(u.Token);
        return u.Reason switch
        {
            UnknownReason.Parameter => "parameter " + (u.Index + 1) + (u.Token != 0 ? " of " + Member() : ""),
            UnknownReason.InstanceField => "field " + Member(),
            UnknownReason.StaticField => "static field " + Member(),
            UnknownReason.CallResult => "the result of " + Member(),
            UnknownReason.ArrayEscaped => "an array another method may write",
            UnknownReason.ArrayDefault => "an array element whose default value is not modeled",
            UnknownReason.AddressTaken => "a variable whose address is taken",
            UnknownReason.HandlerLocal => "a variable assigned in a finally block",
            UnknownReason.InitialLocal => "a local whose entry value is not modeled",
            UnknownReason.NonConstantString => "a string computed by " + Member(),
            UnknownReason.ResolverDelegate => "a type resolver delegate",
            UnknownReason.NonNullBinder => "a custom binder",
            UnknownReason.ThisType => "the runtime type of this",
            UnknownReason.UnsupportedOpcode => "an operation the summary does not model",
            UnknownReason.StackShape => "a body the summary cannot follow",
            UnknownReason.Budget => "a value too complex to follow",
            UnknownReason.CaughtException => "a caught exception",
            UnknownReason.MethodGroup => "a delegate over the API",
            UnknownReason.VirtualTarget => "an override of " + Member() + " that the delegate's target selects",
            _ => "a path the summary does not reach",
        };
    }

    private void RecordReflectionDiagnostic(ReflectionApplication app, RSite site, ReflectionDiagnosticKind kind,
        string reason, ClassInfo? owner, List<string>? names, string? memberKind)
    {
        var m = app.Method;
        var key = (m.Module.Index, SRME.GetToken(m.Handle), site.Offset);
        var variant = new ReflectionDiagnosticVariant(kind, reason, owner?.Module.AssemblyName,
            owner is null ? null : PreservationTypeName(owner), names, memberKind);
        // A canonical context cannot name the type arguments a site depends on; only exact
        // contexts report, and a template level body, which no exact context runs for a clone.
        if (app.Canonical)
        {
            if (!IsTemplateLevelBody(m))
                return;
            if (_templateBodyDiagnostics.TryGetValue(key, out var known))
                MergeDiagnosticVariant(known.Diagnostic, variant);
            else
            {
                var bodyRow = NewReflectionDiagnostic(m, site, kind, reason, owner, names, memberKind);
                bodyRow.Variants = new List<ReflectionDiagnosticVariant> { variant };
                _templateBodyDiagnostics.Add(key, (m, bodyRow));
            }
            return;
        }
        if (_reflectionDiagnosticIndex.TryGetValue(key, out var existing))
        {
            MergeDiagnosticVariant(existing, variant);
            return;
        }
        var diagnostic = NewReflectionDiagnostic(m, site, kind, reason, owner, names, memberKind);
        diagnostic.Variants = new List<ReflectionDiagnosticVariant> { variant };
        _reflectionDiagnosticIndex.Add(key, diagnostic);
        _reflectionDiagnostics.Add(diagnostic);
    }

    /// <summary>Records the site once per class a remedy names, so the fix preserves each.</summary>
    private void RecordReflectionDiagnosticOwners(ReflectionApplication app, RSite site, ReflectionDiagnosticKind kind,
        string reason, RResolved owners, List<string>? names, string? memberKind)
    {
        var classes = RemedyOwners(owners);
        if (classes.Count == 0)
            RecordReflectionDiagnostic(app, site, kind, reason, null, names, memberKind);
        foreach (var cls in classes)
            RecordReflectionDiagnostic(app, site, kind, reason, cls, names, memberKind);
    }

    /// <summary>Adds what another context asks of a reported site and recomputes the row
    /// from the set of variants: the least kind, the reasons in ordinal order, and a fix
    /// that preserves every owner, by the names all variants share a member kind for.</summary>
    private static void MergeDiagnosticVariant(ReflectionDiagnostic diagnostic, ReflectionDiagnosticVariant variant)
    {
        var variants = diagnostic.Variants!;
        foreach (var known in variants)
            if (known.Kind == variant.Kind && known.Reason == variant.Reason && known.Assembly == variant.Assembly
                && known.Owner == variant.Owner && known.MemberKind == variant.MemberKind
                && (known.Names is null ? variant.Names is null
                    : variant.Names is not null && known.Names.SequenceEqual(variant.Names)))
                return;
        variants.Add(variant);
        var kind = variants[0].Kind;
        var reasons = new List<string>();
        var owners = new List<(string? Assembly, string? Owner)>();
        bool sameMemberKind = true, allNamed = true;
        var names = new List<string>();
        foreach (var v in variants)
        {
            if (v.Kind < kind)
                kind = v.Kind;
            if (!reasons.Contains(v.Reason))
                reasons.Add(v.Reason);
            if (!owners.Contains((v.Assembly, v.Owner)))
                owners.Add((v.Assembly, v.Owner));
            sameMemberKind &= v.MemberKind == variants[0].MemberKind;
            if (v.Names is null)
                allNamed = false;
            else
                foreach (string name in v.Names)
                    if (!names.Contains(name))
                        names.Add(name);
        }
        reasons.Sort(string.CompareOrdinal);
        names.Sort(string.CompareOrdinal);
        owners.Sort((a, b) =>
        {
            int c = string.CompareOrdinal(a.Owner ?? "", b.Owner ?? "");
            return c != 0 ? c : string.CompareOrdinal(a.Assembly ?? "", b.Assembly ?? "");
        });
        string? memberKind = sameMemberKind ? variants[0].MemberKind : null;
        diagnostic.Kind = kind;
        diagnostic.Reason = string.Join("; ", reasons);
        diagnostic.Fix = MergedFixText(kind, owners, allNamed && memberKind is not null ? names : null, memberKind);
        if (kind == ReflectionDiagnosticKind.ClosedGeneric && !sameMemberKind)
        {
            // Every remedy some variant needs, in ordinal order.
            var fixes = new List<string>();
            foreach (var v in variants)
                if (ReflectionFixText(kind, null, null, null, v.MemberKind) is var fix && !fixes.Contains(fix))
                    fixes.Add(fix);
            fixes.Sort(string.CompareOrdinal);
            diagnostic.Fix = string.Join("; ", fixes);
        }
    }

    /// <summary>Records a ClosedGeneric diagnostic that stands only if no eligible
    /// runtime-instantiation template of <paramref name="definition"/> survives planning
    /// and some tuple the site asks for (<paramref name="tuple"/>, null when not known) has
    /// no compiled instantiation.</summary>
    private void RecordTemplateDiagnostic(ReflectionApplication app, RSite site, string definition, string reason,
        Module module, TypeDefinitionHandle def, TypeDesc[]? tuple)
    {
        var m = app.Method;
        // As in RecordReflectionDiagnostic, a template level body reports from its placeholder
        // context; an exact context's report of the site stands whatever planning retains.
        MethodInfo? body = null;
        if (app.Canonical)
        {
            if (!IsTemplateLevelBody(m))
                return;
            body = m;
        }
        var key = (m.Module.Index, SRME.GetToken(m.Handle), site.Offset);
        for (int i = 0; i < _templateDiagnostics.Count; i++)
        {
            var (k, d, _, request, known) = _templateDiagnostics[i];
            if (k != key || d != definition)
                continue;
            request.Add(tuple);
            if (known is not null && body is null)
                _templateDiagnostics[i] = (k, d,
                    NewReflectionDiagnostic(m, site, ReflectionDiagnosticKind.ClosedGeneric, reason, null, null, null), request, null);
            return;
        }
        var fresh = new TemplateRequest { Module = module, Definition = def };
        fresh.Add(tuple);
        _templateDiagnostics.Add((key, definition,
            NewReflectionDiagnostic(m, site, ReflectionDiagnosticKind.ClosedGeneric, reason, null, null, null), fresh, body));
    }

    /// <summary>Whether a tuple of <paramref name="request"/> still has no compiled
    /// instantiation, so only the template could serve it.</summary>
    private bool TemplateStillRequested(TemplateRequest request)
    {
        foreach (var tuple in request.Tuples)
            if (tuple is null || !InstancesFor(request.Module, request.Definition).ContainsKey(tuple))
                return true;
        return false;
    }

    private ReflectionDiagnostic NewReflectionDiagnostic(MethodInfo m, RSite site, ReflectionDiagnosticKind kind,
        string reason, ClassInfo? owner, List<string>? names, string? memberKind)
    {
        var reader = m.Module.Reader;
        var definition = reader.GetMethodDefinition(m.Handle);
        var signature = definition.DecodeSignature(RawSignatureProvider.Instance, null);
        string method = RawSignatureProvider.TypeDefinitionName(reader, definition.GetDeclaringType()) + "::"
            + reader.GetString(definition.Name) + "(" + string.Join(",", signature.ParameterTypes.ToArray()) + ")";
        string api = ApiDisplay(m.Module, site.Token);
        string assembly = m.Module.AssemblyName;
        string fix = ReflectionFixText(kind, owner?.Module.AssemblyName,
            owner is null ? null : PreservationTypeName(owner), names, memberKind);
        return new ReflectionDiagnostic
        {
            Kind = kind, Assembly = assembly, Method = method, Offset = site.Offset, Api = api,
            Reason = reason, Fix = fix, User = IsUserModule(m.Module),
            BackendCovered = _backend?.CoversReflectionSite(m) == true,
            Chain = ReachChain(m),
        };
    }

    private static string ApiDisplay(Module module, int token)
    {
        var call = module.ReflectionSummarizer?.Call(token);
        if (call is null)
            return "0x" + token.ToString("x8");
        var text = new StringBuilder(call.Parent).Append('.').Append(call.Name).Append('(');
        for (int i = 0; i < call.Parameters.Length; i++)
        {
            if (i > 0)
                text.Append(", ");
            string p = call.Parameters[i];
            int dot = p.LastIndexOf('.');
            text.Append(dot < 0 ? p : p.Substring(dot + 1));
        }
        return text.Append(')').ToString();
    }

    /// <summary>The remedy a reflection warning prints. The runtime's refusal of the same
    /// unretained target prints the same text after "fix:". <paramref name="owner"/> is a
    /// <see cref="PreservationTypeName"/> and <paramref name="assembly"/> its defining
    /// assembly, so the descriptor selects the type when pasted into a link.xml; an unknown
    /// target prints placeholders for both.</summary>
    internal static string ReflectionFixText(ReflectionDiagnosticKind kind, string? assembly, string? owner,
        List<string>? names, string? memberKind)
    {
        if (kind == ReflectionDiagnosticKind.Metadata)
            return "--reflection-root " + (owner ?? "TYPE");
        if (kind == ReflectionDiagnosticKind.ClosedGeneric)
            return memberKind == DirectConstructionRemedy
                ? "construct the closed type directly instead of through MakeGenericType"
                : "name the closed generic instantiation statically";
        return PreserveFixPrefix + PreserveDescriptor(assembly, owner, names, memberKind);
    }

    private const string PreserveFixPrefix = "[Preserve] on the target, or link.xml ";

    /// <summary><see cref="ReflectionFixText"/> over several owners: one descriptor or
    /// root per owner, in the given order.</summary>
    private static string MergedFixText(ReflectionDiagnosticKind kind, List<(string? Assembly, string? Owner)> owners,
        List<string>? names, string? memberKind)
    {
        if (owners.Count == 1 || kind == ReflectionDiagnosticKind.ClosedGeneric)
            return ReflectionFixText(kind, owners[0].Assembly, owners[0].Owner, names, memberKind);
        var text = new StringBuilder();
        foreach (var (assembly, owner) in owners)
            if (kind == ReflectionDiagnosticKind.Metadata)
                text.Append(text.Length == 0 ? "" : " ").Append("--reflection-root ").Append(owner ?? "TYPE");
            else
                text.Append(PreserveDescriptor(assembly, owner, names, memberKind));
        return kind == ReflectionDiagnosticKind.Metadata ? text.ToString() : PreserveFixPrefix + text;
    }

    private static string PreserveDescriptor(string? assembly, string? owner, List<string>? names, string? memberKind)
    {
        // Compiler-generated names such as <Main>b__0_0 stay well-formed when pasted.
        assembly = XmlAttributeText(assembly ?? "ASSEMBLY");
        string type = XmlAttributeText(owner ?? "TYPE");
        string member;
        if (memberKind == ".ctor")
            member = "<method name=\".ctor\" />";
        else if (names is { Count: > 0 } && memberKind is not null)
        {
            var text = new StringBuilder();
            for (int i = 0; i < names.Count; i++)
                if (names.IndexOf(names[i]) == i)
                    text.Append('<').Append(memberKind).Append(" name=\"").Append(XmlAttributeText(names[i])).Append("\" />");
            member = text.ToString();
        }
        else
            return "<assembly fullname=\"" + assembly + "\"><type fullname=\"" + type + "\" preserve=\"all\" /></assembly>";
        return "<assembly fullname=\"" + assembly + "\"><type fullname=\"" + type + "\">" + member + "</type></assembly>";
    }

    /// <summary><paramref name="text"/> as a double-quoted XML attribute value.</summary>
    internal static string XmlAttributeText(string text)
    {
        if (text.IndexOfAny(XmlAttributeSpecials) < 0)
            return text;
        var escaped = new StringBuilder(text.Length + 8);
        foreach (char c in text)
            switch (c)
            {
                case '&':
                    escaped.Append("&amp;");
                    break;
                case '<':
                    escaped.Append("&lt;");
                    break;
                case '>':
                    escaped.Append("&gt;");
                    break;
                case '"':
                    escaped.Append("&quot;");
                    break;
                default:
                    escaped.Append(c);
                    break;
            }
        return escaped.ToString();
    }

    private static readonly char[] XmlAttributeSpecials = { '&', '<', '>', '"' };
}
