using System.Reflection.Metadata;
using System.Text;

namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private void EmitTypeInfos(CppOutput o) => new TypeMetadataEmitter(this, o).Emit();

    /// <summary>Renders the type-metadata section of the emission: the enum and
    /// generic-open-definition type-infos, the shared-generics rgctx tables, and
    /// every class's metadata block (vtable, interface dispatch tables, reflection
    /// field/method/ctor/property tables, attribute factories, invoker thunks,
    /// ti_/ty_ definitions). One instance serves one emission: pool sequence
    /// numbers land in output bytes, so the class-major rendering order and the
    /// monotonic per-kind counters are part of the output contract (see the
    /// class-major loop in <see cref="Emit"/>).</summary>
    private sealed class TypeMetadataEmitter
    {
        private readonly CppEmitter _e;
        private readonly Compilation _c;
        private readonly CppOutput _o;
        // The active metadata sink: _o.Data for the shared sections; the
        // class-major loop in Emit points it at a per-class scratch block while
        // the renderers run, then back (see the loop comment there).
        private StringBuilder _sb;

        // The grouped instantiations whose rgctx_ table was emitted (shared
        // generics), read by RenderTypeInfo to wire the type-info's trailing
        // rgctx member.
        private readonly HashSet<ClassInfo> _rgctxTableSyms = new();
        // ---- per-class metadata state, shared by the renderers below ----
        // Interface dispatch tables emitted for the current class, consumed by its
        // type-info initializer within the same block.
        private readonly Dictionary<ClassInfo, List<ClassInfo>> _itfTables = new();
        // Emitted row count per class — the real rows plus any canonical alias
        // rows (shared generics), which the type-info's interfaceCount covers.
        private readonly Dictionary<ClassInfo, int> _itfRowCounts = new();
        // The class's interface-entry table symbol — a pooled itfspool_ array
        // (see RenderItfTables), consumed by the same class's type-info
        // initializer within the same rendered block (or, when the table
        // deduplicated globally, resolved through its generated.h extern).
        private readonly Dictionary<ClassInfo, string> _itfTabSyms = new();
        private readonly Dictionary<ClassInfo, (string Expr, int Count)> _fieldTabs = new();
        private readonly Dictionary<ClassInfo, (string Expr, int Count)> _methodTabs = new();
        private readonly Dictionary<ClassInfo, (string Expr, int Count)> _ctorTabs = new();
        private readonly Dictionary<ClassInfo, (string Expr, int Count)> _propTabs = new();
        // A method's address within its emitted table ("&methtab_X[k]"), so a property's
        // accessor can reference its method-table entry.
        private readonly Dictionary<MethodInfo, string> _memberAddr = new();
        // Signature-deduplicated invoker thunks: keyed by C++ ABI shape so
        // every member with the same (static?, param/return ABI) shares one thunk.
        // The thunks are file-local (static), so the dedup scope is one metadata
        // chunk TU: the set is cleared whenever the class-major loop below rolls to
        // a fresh chunk, making each chunk define every thunk it references
        // (duplicate static definitions across TUs are legal and deterministic).
        private readonly HashSet<string> _invokerThunks = new(System.StringComparer.Ordinal);
        // Content-deduplicated parameter tables, keyed by initializer text. Unlike
        // the invoker thunks these pools are GLOBAL (whole-emission, surviving chunk
        // rolls): the first chunk to mint an initializer defines the array with
        // external linkage and declares it in _o.Header, every later chunk references
        // it by name. Sound because pool naming is deterministic (one monotonic
        // counter per kind, claimed in TopoOrder first-encounter order, so definition
        // sites reproduce byte-identically — the self-host fixpoint relies on it) and
        // every symbol a shareable initializer names resolves globally; an initializer
        // naming a file-local symbol (ubthunk_, attribute factories) embeds its
        // uniquely named owner, so it is unique by construction.
        private readonly Dictionary<string, string> _parmPools = new(System.StringComparer.Ordinal);
        private int _parmPoolSeq;
        // Same pattern for interface slot tables (itfpool_) and generic-argument
        // vectors (genargpool_). Both are address-agnostic: interface dispatch
        // compares Dn2CppInterfaceEntry.itf and only indexes .slots, and every
        // genericArgs consumer (MakeGenericType candidate matching,
        // GetGenericArguments, array-element variance) reads the elements — no
        // runtime path keys on either array's address, so byte-identical
        // initializers can share one external-linkage array across the emission.
        private readonly Dictionary<string, string> _itfPools = new(System.StringComparer.Ordinal);
        private int _itfPoolSeq;
        private readonly Dictionary<string, string> _genargPools = new(System.StringComparer.Ordinal);
        private int _genargPoolSeq;
        // Interface-entry tables (itfspool_) intern the same way — rows pair &ti_
        // handles with the pooled slot tables above, and table address identity is
        // not load-bearing (the hot-update loader itself aliases whole tables via
        // ti->interfaces = base->interfaces). The global itfpool_ names feed this
        // pool's keys, so cross-chunk slot sharing is what makes entry tables
        // comparable across chunks at all.
        private readonly Dictionary<string, string> _itfsPools = new(System.StringComparer.Ordinal);
        private int _itfsPoolSeq;
        // Per-slot named trap stubs (slotmiss_). A dispatch slot whose return type is a
        // by-value struct cannot read its receiver out of argument 0 (an indirect return
        // spends that register on the caller's hidden result buffer), so the shared
        // receiver-reading traps would report "(unknown)"; the slot's descriptor is baked
        // into a stub instead. Deduplicated per chunk on (reporter, descriptor) text —
        // the file-local statics reset on a chunk roll like the pools above — while the
        // sequence counter stays monotonic across the emission so names are unique and
        // deterministic.
        private readonly Dictionary<string, string> _slotStubs = new(System.StringComparer.Ordinal);
        private int _slotStubSeq;
        // Per-row invoker trap stubs (invmiss_), the reflection-invoker sibling of the
        // slotmiss_ map above: a member-table row whose invoker thunk cannot be
        // materialized (InvokerThunkBlocker — its signature names a by-value struct
        // the emit set never declared) gets a stub with the invoker ABI that raises
        // the catchable NotSupportedException naming the method and the missing type,
        // so the row stays bindable by name (the hot-update loader requires a non-null
        // invoker) and the failure moves from the C++ compile to the actual invoke.
        // Same scoping rules as _slotStubs: per-chunk map (file-local statics),
        // sequence counter monotonic across the emission.
        private readonly Dictionary<string, string> _invokerMissStubs = new(System.StringComparer.Ordinal);
        private int _invokerMissSeq;
        // The C++ struct names the layout emission declares (every emitted class's
        // CppStructName — the set EmitStructs defines), computed once on first use:
        // this emitter runs strictly after the emit set is final (see the class doc),
        // so the snapshot cannot go stale. Consumed by the InvokerThunkBlocker test
        // at the two invoker-thunk call sites in BuildMemberTable.
        private HashSet<string>? _declaredStructs;
        private IReadOnlySet<string> DeclaredStructs =>
            _declaredStructs ??= _e.EmittedClasses.Select(c => c.CppStructName)
                .ToHashSet(System.StringComparer.Ordinal);
        // The enums whose ti_ is actually emitted — snapshotted by Emit once the
        // enum section has run (see the assignment there).
        private HashSet<ClassInfo> _emittedEnums = new();

        internal TypeMetadataEmitter(CppEmitter e, CppOutput o)
        {
            _e = e;
            _c = e._c;
            _o = o;
            _sb = o.Data;
        }

        /// <summary>Append an open generic DEFINITION's kind flags — the bits behind
        /// <c>Type.IsValueType</c>/<c>IsInterface</c>/<c>IsAbstract</c>/<c>IsClass</c>/
        /// <c>IsSealed</c> — to <paramref name="flagBits"/>. A <c>gendef_</c> handle is what
        /// a bare <c>typeof(Def&lt;&gt;)</c> reflects over, so both emit routes (natural off
        /// a closed instantiation, synthetic for a def with no close) must carry them or the
        /// definition reads as a non-sealed reference class. Interface ⟹ abstract, mirroring
        /// the closed-type flag rule.</summary>
        private static void AddGenericDefKindFlags(List<string> flagBits, bool isValueType, bool isInterface, bool isAbstract, bool isSealed)
        {
            if (isValueType)
                flagBits.Add("DN2CPP_TF_VALUETYPE");
            if (isInterface)
            {
                flagBits.Add("DN2CPP_TF_INTERFACE");
                flagBits.Add("DN2CPP_TF_ABSTRACT");
            }
            else if (isAbstract)
                flagBits.Add("DN2CPP_TF_ABSTRACT");
            if (isSealed)
                flagBits.Add("DN2CPP_TF_SEALED");
        }

        /// <summary>Whether <paramref name="defName"/> is one of the five generic collection
        /// interfaces an SZArray implements over its element. The bit rides the DEFINITION
        /// handle (one stamp covers every close), and <c>dn2cpp_isinst</c> reads it off a
        /// closed target's genericDef to answer <c>(array) is IList&lt;T&gt;</c> — with
        /// array-element covariance on the type argument — independent of the lazy dispatch
        /// map. Asked at BOTH gendef mint sites, because a definition first named by a
        /// relation-only array row mints its handle through the minimal-type-info route,
        /// where a missing bit is a type test that silently answers false.</summary>
        private static bool IsArrayGenericItfDef(string defName) =>
            defName is "System.Collections.Generic.IEnumerable`1"
                or "System.Collections.Generic.ICollection`1"
                or "System.Collections.Generic.IList`1"
                or "System.Collections.Generic.IReadOnlyList`1"
                or "System.Collections.Generic.IReadOnlyCollection`1";

        /// <summary>Wraps a <c>gendef_</c> row's positional initializer so the handle carries
        /// <paramref name="names"/>, its definition's comma-joined type-parameter names —
        /// the bracket group <c>Type.ToString()</c> appends. The member is the
        /// struct's last and a gendef row stops well before it, hence the constexpr wrapper;
        /// a null list leaves the row's text exactly as it was.</summary>
        private static string WithGenericParams(string init, string? names) =>
            names is null ? init : $"dn2cpp_ti_with_generic_params({init}, \"{names}\")";

        /// <summary>Record that this emission defines the type-info symbol
        /// <paramref name="sym"/> — the defined half of
        /// <see cref="CppEmitter.AssertNamedTypeInfosDefined"/>. Filtered to <c>ti_</c>
        /// symbols (a runtime-raised exception's <c>tibind_</c> or a shared handle is not a
        /// per-type <c>ti_</c> a body ever names), so the set holds exactly what the named
        /// half can hold.</summary>
        private void NoteDefinedTypeInfo(string sym)
        {
            if (sym.StartsWith("ti_", System.StringComparison.Ordinal))
                _e._definedTypeInfoSyms.Add(sym);
        }

        /// <summary>The intrinsic-modeled classes / interfaces a lowering named by their own
        /// <c>&amp;ti_&lt;T&gt;</c> handle — the generic Class arm of
        /// <see cref="MethodCompiler.TypeInfoExprOf"/>, reached by a
        /// <c>typeof</c>/<c>isinst</c>/<c>castclass</c>/<c>box</c>/array-covariance site — that
        /// no other pass emits. A non-intrinsic referenced reference type is pulled into the
        /// emit set opaquely by <see cref="CppEmitter.Emit"/>; an intrinsic one is skipped
        /// there, so its <c>ti_</c> would be undeclared at link time. The predicate is
        /// decode-free (names, flags and <see cref="ClassInfo.CppTypeInfoName"/> only) and
        /// selects exactly: a type whose body-path type-info expression IS its own <c>ti_</c>
        /// (not a shared runtime handle like Decimal/Task/an exception) yet which nothing
        /// emits. Enums are excluded (emitted by their own loop).</summary>
        private List<ClassInfo> ReferencedIntrinsicTypeInfos()
        {
            var topo = _e.TopoOrder().ToHashSet();
            return _c.ReferencedTypes
                .Where(c => !c.IsEnum
                            && !_e._emit.Contains(c)
                            && !topo.Contains(c)
                            // A runtime-OWNED type's ti_ is the runtime's own definition —
                            // minting a minimal one here would be the second identity this
                            // table exists to prevent. Not subsumed by the TypeInfoExprOf
                            // comparison below, which holds for every class since the Class
                            // arm answers "&" + CppTypeInfoName.
                            && !CoreIntrinsics.RuntimeOwnsTypeInfo(c.FullName)
                            && MethodCompiler.TypeInfoExprOf(TypeDesc.MakeClass(c)) == "&" + c.CppTypeInfoName)
                .OrderBy(c => c.CppName, System.StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>One SZArray member type the reflection tables will name: record its
        /// element (<see cref="Compilation.NoteArrayElementType"/> — the same route every
        /// <c>newarr</c>/<c>typeof(T[])</c> site takes) so the precise
        /// <c>ti_arr_&lt;elem&gt;</c> handle <see cref="CppEmitter.FieldTypeInfoExpr"/>'s
        /// SZArray arm names is forward-declared and emitted, and — for an enum element —
        /// note the enum referenced so its own <c>ti_</c> (the array handle's elementType,
        /// what GetElementType answers with) is emitted by the referenced-enum loops below —
        /// an enum only ever named as a reflected array element has no token site to have
        /// done either. Element kinds mirror the <c>ldtoken typeof(T[])</c> arm; a
        /// canonical-placeholder element is refused by NoteArrayElementType itself.</summary>
        private void NoteReflectedArrayType(TypeDesc t)
        {
            if (t is not { Kind: TypeKind.SZArray, Element: { Kind: TypeKind.Primitive or TypeKind.Class or TypeKind.External or TypeKind.SZArray or TypeKind.MDArray } el })
                return;
            _c.NoteArrayElementType(el);
            if (el is { Kind: TypeKind.Class, Class: { IsEnum: true } ec })
                _c.NoteReferencedType(ec);
        }

        /// <summary>Pre-notes every SZArray type the reflection tables emit as a member
        /// type — field types (<see cref="RenderFieldTable"/>), method/ctor return,
        /// parameter and generic-argument types (<see cref="BuildMemberTable"/> via
        /// <see cref="RenderMemberTables"/>), property types (<see cref="BuildPropTable"/>)
        /// and the closed generic-argument vectors (<see cref="RenderTypeInfo"/>) — by
        /// mirroring those emitters' class/member filters, so it reads exactly the member
        /// types they read (the trim rule is applied before any signature is touched, like
        /// BuildMemberTable's row loop). MUST run before the forward-declaration loops in
        /// <see cref="Emit"/>: the ti_arr_/enum-ti_ externs — and with them the
        /// declared-handle record <see cref="CppEmitter.ArrayTypeInfoDeclared"/> answers
        /// from — are taken there, and an element noted later can no longer be named by a
        /// member type. Each mirrored emitter names this method at its guard.</summary>
        private void NoteReflectedMemberArrayElements()
        {
            foreach (var cls in _e.TopoOrder())
            {
                if (cls.IsEnum)
                    continue;
                // RenderFieldTable's guard.
                if (cls.Fields.Count > 0 && !_e.IsCanonicalWorld(cls) && _c.KeepsReflectionMetadata(cls))
                    foreach (var f in cls.Fields)
                        NoteReflectedArrayType(f.Type);
                // RenderTypeInfo's generic-argument vector gate (its _genericDefSyms
                // lookup succeeds exactly when GenericDefInfo is non-null).
                if (!_e.IsCanonicalWorld(cls) && !_e.SkipsCanonicalMetadata(cls)
                    && _e.GenericDefInfo(cls) is not null)
                    foreach (var a in cls.Context.TypeArgs)
                        NoteReflectedArrayType(a);
                // RenderMemberTables' guard + BuildMemberTable's row trim.
                if (_e.IsOpaque(cls) || _e.IsCanonicalWorld(cls))
                    continue;
                bool keepRefl = _c.KeepsReflectionMetadata(cls);
                bool appCls = cls.Module == _c.AppModule && !_e.IsOpaque(cls);
                bool trim = !appCls && !_e._hotUpdateBase;
                var accessorRows = new HashSet<MethodDefinitionHandle>();
                foreach (var m in cls.Methods)
                {
                    bool ctorRow = m.Name == ".ctor" && !m.IsStatic;
                    bool methodRow = m.Name != ".ctor" && m.Name != ".cctor"
                        && !(_c.SharedGenericsEnabled
                            && m.Context.MethodArgs.Any(Compilation.ContainsCanonPlaceholder));
                    if (methodRow)
                        accessorRows.Add(m.Handle);
                    // The ctortab is built even for a reflection-stripped class
                    // (constructors are deliberately not stripped); methtab/proptab are not.
                    if (!(ctorRow || (methodRow && keepRefl)))
                        continue;
                    if (trim && m.Rva != 0 && !_c.Reachable.Contains(m))
                        continue;
                    NoteReflectedArrayType(m.Signature.ReturnType);
                    foreach (var p in m.Signature.ParameterTypes)
                        NoteReflectedArrayType(p);
                    foreach (var ga in m.Context.MethodArgs)
                        NoteReflectedArrayType(ga);
                }
                // BuildPropTable's rows: a property renders (with its decoded type) when
                // either accessor is in the type's method list, even if the accessor's own
                // row was trimmed — so the property type is noted off that same condition.
                if (!keepRefl)
                    continue;
                try
                {
                    var reader = cls.Module.Reader;
                    var td = reader.GetTypeDefinition(cls.Handle);
                    foreach (var ph in td.GetProperties())
                    {
                        var acc = reader.GetPropertyDefinition(ph).GetAccessors();
                        if ((acc.Getter.IsNil || !accessorRows.Contains(acc.Getter))
                            && (acc.Setter.IsNil || !accessorRows.Contains(acc.Setter)))
                            continue;
                        NoteReflectedArrayType(
                            reader.GetPropertyDefinition(ph).DecodeSignature(_c.SigProvider, cls.Context).ReturnType);
                    }
                }
                catch (Exception e) when (!Compilation.IsMustEscape(e))
                { /* no decodable property metadata — BuildPropTable yields no rows either */ }
            }
            // EmitReferencedIntrinsicTypeInfos' generic-argument vectors: those rows are
            // outside the emit set, so the walk above never reaches them.
            foreach (var cls in _e._referencedIntrinsicTis)
                if (_e.GenericDefInfo(cls) is not null)
                    foreach (var a in cls.Context.TypeArgs)
                        NoteReflectedArrayType(a);
        }

        /// <summary>The pooled <c>genargpool_</c> symbol for <paramref name="cls"/>'s closed
        /// type arguments. Byte-identical vectors are interned across the whole emission
        /// (first definition wins; explicit <c>extern</c> because a namespace-scope const
        /// array defaults to internal linkage) — every runtime consumer reads the elements,
        /// never the array's address.</summary>
        private string GenericArgVector(ClassInfo cls)
        {
            string gaInit = string.Join(", ", cls.Context.TypeArgs.Select(a => _e.FieldTypeInfoExpr(a, _emittedEnums)));
            if (_genargPools.TryGetValue(gaInit, out var pooled))
                return pooled;
            string arr = $"genargpool_{_genargPoolSeq++}";
            _sb.AppendLine($"extern const Dn2CppTypeInfo* const {arr}[] = {{ {gaInit} }};");
            _o.Header.AppendLine($"extern const Dn2CppTypeInfo* const {arr}[];");
            _genargPools[gaInit] = arr;
            return arr;
        }

        /// <summary>The minimal type-infos of the classes <see cref="ReferencedIntrinsicTypeInfos"/>
        /// forward-declared: name, base chain (a reference type chains to System.Object; a
        /// value type or interface roots at null, like the class emitter), unboxed size for a
        /// `box` that survives folding, and the flag bits <c>dn2cpp_isinst</c>/
        /// <c>dn2cpp_type_is_*</c> read. No vtable and no member/interface tables — those are
        /// intrinsic-dispatched — and no ty_ companion, so typeof interns lazily.
        ///
        /// A CLOSED intrinsic generic also carries genericDef + genericArgs, minting the
        /// family's open-definition handle when no emitted instantiation did: without them
        /// Type.Name/ToString/FullName have nothing to normalize or compose from. Hence the
        /// position — after the gendef loops and after <c>_emittedEnums</c>.</summary>
        private void EmitReferencedIntrinsicTypeInfos()
        {
            foreach (var c in _e._referencedIntrinsicTis)
            {
                if (_e.GenericDefInfo(c) is not { } gi || _e._genericDefSyms.ContainsKey(gi.DefName))
                    continue;
                string defSym = "gendef_" + CppNaming.Sanitize(gi.DefName);
                _e._genericDefSyms[gi.DefName] = defSym;
                var defFlagBits = new List<string> { "DN2CPP_TF_GENERICDEF" };
                AddGenericDefKindFlags(defFlagBits, c.IsValueType, c.IsInterface, c.IsAbstract, c.IsSealed);
                if (IsArrayGenericItfDef(gi.DefName))
                    defFlagBits.Add("DN2CPP_TF_ARRAY_GEN_ITF");
                string defFlags = "(" + string.Join(" | ", defFlagBits) + ")";
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo {defSym};");
                _sb.AppendLine($"extern const Dn2CppType ty_{defSym};");
                string defInit = $"{{ \"{gi.DefName}\", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, {defFlags}, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, &{defSym}, nullptr, 0, nullptr, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, nullptr, nullptr, &ty_{defSym} }}";
                _sb.AppendLine($"const Dn2CppTypeInfo {defSym} = "
                    + WithGenericParams(defInit, Compilation.GenericParamNames(c.Module, c.Handle)) + ";");
                _sb.AppendLine($"const Dn2CppType ty_{defSym} = {{ {{ &dn2cpp_type_type }}, &{defSym} }};");
            }
            foreach (var c in _e._referencedIntrinsicTis)
            {
                string baseExpr = c.IsValueType || c.IsInterface ? "nullptr" : "&dn2cpp_object_type";
                string size = c.IsValueType ? $"(int32_t)sizeof({CppTypes.Of(TypeDesc.MakeClass(c))})" : "0";
                var flagBits = new List<string>();
                if (c.IsValueType)
                    flagBits.Add("DN2CPP_TF_VALUETYPE");
                if (c.IsInterface)
                {
                    flagBits.Add("DN2CPP_TF_INTERFACE");
                    flagBits.Add("DN2CPP_TF_ABSTRACT");
                }
                else if (c.IsAbstract)
                    flagBits.Add("DN2CPP_TF_ABSTRACT");
                if (c.IsSealed)
                    flagBits.Add("DN2CPP_TF_SEALED");
                string flags = flagBits.Count == 0 ? "0" : string.Join(" | ", flagBits);
                // Non-generic: stop at flags and let the trailing members 0-fill, so this
                // row's text is what it always was. A closed generic has to spell the ten
                // members between flags and genericDef to reach its positional slots.
                string genTail = "";
                if (_e.GenericDefInfo(c) is { } gi && _e._genericDefSyms.TryGetValue(gi.DefName, out var defSym))
                    genTail = $", nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, &{defSym}, {GenericArgVector(c)}, {c.Context.TypeArgs.Length}";
                _sb.AppendLine($"const Dn2CppTypeInfo {c.CppTypeInfoName} = {{ \"{Compilation.ReflectionTypeName(c)}\", {baseExpr}, {size}, nullptr, nullptr, 0, nullptr, nullptr, nullptr, {flags}{genTail} }};");
            }
        }

        internal void Emit()
        {
            // Ahead of the note pass, which reads it: the set is a pure function of the
            // (final) emit set and ReferencedTypes, and the note pass only ever adds an
            // ENUM to the latter — which this set excludes. The row planting below does add
            // to it, which is why that step re-derives.
            _e._referencedIntrinsicTis = ReferencedIntrinsicTypeInfos();

            // Before anything is forward-declared: note every SZArray the reflection
            // tables below will type a member with, so its precise ti_arr_ handle (and
            // an enum element's ti_) is declared/emitted like a body-noted one.
            NoteReflectedMemberArrayElements();

            // The noted-array set is final HERE, so this is where the relation-only generic
            // interface rows are planted: an element the pass above just discovered is
            // invisible to every fixpoint round, and its array would report a short
            // interface list. The minted interfaces are named by nothing else, so they join
            // the minimal-type-info set — re-deriving keeps that list's CppName order
            // rather than appending to it.
            if (_c.PlantUnmappedArrayGenericItfRows().Count > 0)
                _e._referencedIntrinsicTis = ReferencedIntrinsicTypeInfos();

            // Type-info handles for classes, referenced enums, and per-element arrays are named
            // by method bodies in any TU (typeof / isinst / cast / typed array creation), so
            // each is forward-declared with external linkage in the header. The definitions
            // land in per-class metadata blocks that overflow into metadata-only chunk TUs
            // (see the class-major loop below); all the file-local tables/thunks a class's
            // type-info references are co-located inside its block.
            _o.Header.AppendLine("// ---- type metadata forward declarations ----");
            // Each ti_ has a ty_ companion: the interned System.Type object baked into
            // the type-info's typeObject member, making typeof/GetType a lock-free load.
            // Iterate TopoOrder(), not EmittedClasses: a tree-shaken abstract base (no
            // reachable body, so absent from the emit set) is still pulled into the
            // topo order as an emitted subclass's base, and its ti_/ty_ definition is
            // emitted by the definition loop below — so it needs a matching forward
            // declaration here, or a field table naming &ti_base fails to compile.
            foreach (var cls in _e.TopoOrder().Where(c => !c.IsEnum && !_e.SkipsCanonicalMetadata(c)
                                                          && !CoreIntrinsics.RuntimeOwnsTypeInfo(c.FullName)))
            {
                // CppTypeInfoDefName, not CppTypeInfoName: a runtime-raised exception type's
                // handle is declared by dn2cpp.h (and is mutable — the startup bind writes it),
                // so re-declaring it here as `extern const` would conflict. Its emitted
                // metadata carries the tibind_ name instead. A runtime-OWNED type is skipped
                // outright: the runtime defines both its handle and its Type object, and a
                // second definition here would be a second identity for one CLR type.
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo {cls.CppTypeInfoDefName};");
                _o.Header.AppendLine($"extern const Dn2CppType ty_{cls.CppName};");
                NoteDefinedTypeInfo(cls.CppTypeInfoDefName);
            }
            foreach (var en in _c.ReferencedTypes.Where(c => c.IsEnum).OrderBy(c => c.CppName, System.StringComparer.Ordinal))
            {
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo {en.CppTypeInfoName};");
                _o.Header.AppendLine($"extern const Dn2CppType ty_{en.CppName};");
                NoteDefinedTypeInfo(en.CppTypeInfoName);
            }
            foreach (var kv in _c.ArrayElementTypes.OrderBy(kv => kv.Key, System.StringComparer.Ordinal))
            {
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo ti_arr_{kv.Key};");
                _o.Header.AppendLine($"extern const Dn2CppType ty_arr_{kv.Key};");
                NoteDefinedTypeInfo("ti_arr_" + kv.Key);
            }
            // Static MD-array handles: named by an SZArray-of-MDArray entry's
            // elementType and by the type-registry row; defined by EmitArrayTypeInfos.
            foreach (var kv in _c.MdArrayTypes.OrderBy(kv => kv.Key, System.StringComparer.Ordinal))
            {
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo ti_md_{kv.Key};");
                _o.Header.AppendLine($"extern const Dn2CppType ty_md_{kv.Key};");
                NoteDefinedTypeInfo("ti_md_" + kv.Key);
            }
            // NoteDefinedTypeInfo above IS the declared-handle snapshot the two array-naming
            // mouths key on (CppEmitter.ArrayTypeInfoDeclared): an element noted after this
            // point (an attribute decode inside the class-major loop) still gets its ti_arr_
            // definition from EmitArrayTypeInfos, but no header extern — so member types must
            // never name it, and it is correctly absent from the recorded set.
            // Referenced intrinsic type-infos: a `typeof`/`isinst`/`castclass`/`box`/array-
            // covariance lowering names an intrinsic-modeled type's own `&ti_<T>` (StringBuilder,
            // CultureInfo, the SIMD vectors, IFormatProvider, …), but the opaque referenced-type
            // pass in CppEmitter.Emit skips intrinsic classes — so nothing else would emit that
            // symbol and the C++ link would fail on an undeclared identifier. (Forward-declared
            // here; defined by EmitReferencedIntrinsicTypeInfos, once the gendef handles exist.)
            foreach (var c in _e._referencedIntrinsicTis)
            {
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo {c.CppTypeInfoName};");
                NoteDefinedTypeInfo(c.CppTypeInfoName);
            }
            _o.Header.AppendLine();
            // The declaration block is complete: nothing below adds a ti_ definition that a
            // reference could be checked against, so from here on CppEmitter.TypeInfoRef can
            // answer definedness — and every emitter-side &ti_ mouth from here to the end of
            // emission goes through it.
            _e._typeInfoSymsFinal = true;

            _sb.AppendLine("// ---- type metadata ----");

            // Per-enum type-info: an enum referenced as a type token (isinst / box / cast)
            // gets a distinct identity so `o is SomeEnum` discriminates one enum from
            // another and from int. Modeled as int32 with the shared System.Enum base,
            // which the runtime keys on to format/hash the payload as its underlying value.
            // Emitted from ReferencedTypes (enums carry no layout, so they are not in the
            // emit/struct set), ordered for stable output. The `tostring` slot is wired to
            // a per-enum name-formatting function so a boxed enum's Object.ToString yields
            // the member name, not the number — its body references string literals and so
            // is emitted after the literal table; only the declaration goes here.
            foreach (var en in _c.ReferencedTypes.Where(c => c.IsEnum).OrderBy(c => c.CppName, System.StringComparer.Ordinal))
            {
                string toStr = "nullptr";
                if (MethodCompiler.EnumToStringFn(en, $"enumtostr_{en.CppName}", _e._literals) is { } body)
                {
                    _sb.AppendLine($"static Dn2CppString* enumtostr_{en.CppName}(Dn2CppObject*);");
                    _e._enumToStringFns.Append(body);
                    toStr = $"&enumtostr_{en.CppName}";
                }
                // Enum runtime metadata: the underlying primitive's handle + the
                // (name, value) member table pre-sorted by unsigned underlying magnitude
                // (matching .NET's Enum.GetNames/GetValues order). Backs the non-generic
                // Enum.GetNames/GetName/IsDefined/Parse(Type, …) + GetEnumUnderlyingType.
                string underlying = MethodCompiler.TypeInfoExprOf(TypeDesc.MakePrimitive(en.EnumUnderlying)) ?? "&dn2cpp_int32_type";
                var ordered = SortedEnumMembers(en);
                string membersExpr = "nullptr";
                if (ordered.Count > 0)
                {
                    string tab = $"enummembers_{en.CppName}";
                    // Member names are C# identifiers (ASCII letters/digits/underscore), so
                    // they inline safely as C string literals — no escaping/literal pool.
                    // Values render in hex, as the field-row site below does: a decimal
                    // long.MinValue has no C++ literal form (`-9223372036854775808LL` is a
                    // negation of an unsigned literal, which C++11-narrows and fails to compile).
                    var rows = ordered.Select(m => $"{{ \"{m.name}\", (int64_t)0x{(ulong)m.value:X}ULL }}");
                    _sb.AppendLine($"static const Dn2CppEnumMember {tab}[] = {{ {string.Join(", ", rows)} }};");
                    membersExpr = tab;
                }
                // The reflection field surface (value__ + one literal row per
                // member), backing Type.GetField(s) like any class's fldtab_.
                var (fldExpr, fldCount) = RenderEnumFieldTable(en);
                // Trailing 0-fills for methods/ctors/props/customAttrs/generic, then
                // the enum metadata (enumUnderlying, enumMembers, enumMemberCount).
                string enFlags = "(DN2CPP_TF_ENUM | DN2CPP_TF_VALUETYPE | DN2CPP_TF_SEALED"
                    + (IsNestedType(en) ? " | DN2CPP_TF_NESTED" : "")
                    + (EnumHasFlagsAttribute(en) ? " | DN2CPP_TF_FLAGS" : "") + ")";
                var (enIlAttrs, enToken) = TypeIlMeta(en);
                // instanceSize is the enum's MODEL width — int32, or int64 for a long/ulong
                // underlying — not its CLR underlying width: every reader of this field
                // sizes a BOX from it, and a box carries the model payload. The underlying
                // width is enumUnderlying's answer (dn2cpp_layout_size backs
                // Unsafe.SizeOf<E> from there).
                string enSize = MethodCompiler.IsWideEnum(en) ? "(int32_t)sizeof(int64_t)" : "(int32_t)sizeof(int32_t)";
                _sb.AppendLine($"const Dn2CppTypeInfo {en.CppTypeInfoName} = {{ \"{Compilation.ReflectionTypeName(en)}\", &dn2cpp_enum_type, {enSize}, nullptr, nullptr, 0, {toStr}, nullptr, nullptr, {enFlags}, {fldExpr}, {fldCount}, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, nullptr, 0, {underlying}, {membersExpr}, {ordered.Count}, nullptr, 0, nullptr, 0, {AsmLit(en)}, nullptr, nullptr, &ty_{en.CppName}, nullptr, 0x{enIlAttrs:X}u, {enToken} }};");
                _sb.AppendLine($"const Dn2CppType ty_{en.CppName} = {{ {{ &dn2cpp_type_type }}, {_e.TypeInfoRef(en, "enum ty_ companion")} }};");
            }

            // Generic open-definition type-infos: one synthetic Dn2CppTypeInfo per
            // distinct closed-generic definition, so GetGenericTypeDefinition() returns a
            // stable shared handle (two List<T> closes' definitions compare equal) and
            // MakeGenericType has something to key the closed instantiations on. Self-
            // referencing genericDef (a const's own address is known in its initializer);
            // carries DN2CPP_TF_GENERICDEF. Emitted before the closed type-infos that name it.
            foreach (var cls in _e.TopoOrder().Where(c => !c.IsEnum && !_e.IsCanonicalWorld(c)))
            {
                if (_e.GenericDefInfo(cls) is not { } gi || _e._genericDefSyms.ContainsKey(gi.DefName))
                    continue;
                string sym = "gendef_" + CppNaming.Sanitize(gi.DefName);
                _e._genericDefSyms[gi.DefName] = sym;
                // Generic variance rides the definition handle, which is the only place it can:
                // it is a property of the type PARAMETERS, so every closed instantiation reads
                // it from here. `varianceMask` carries the general answer (per parameter, either
                // direction, any arity — IComparer<in T>, IGrouping<out K, out E>); the older
                // DN2CPP_TF_COVARIANT bit still marks the single-parameter covariant defs it
                // always marked, because a base image emitted before the mask existed keeps
                // matching through it.
                int varMask = _c.GenericVarianceMask(cls);
                var flagBits = new List<string> { "DN2CPP_TF_GENERICDEF" };
                // The definition's KIND bits — invariant across every close, so read off
                // this closed instantiation. A bare typeof(Def<>) reflects over the gendef
                // handle, so IsInterface/IsAbstract/IsClass/IsSealed/IsValueType must answer
                // as real .NET does. An interface is also abstract, mirroring the
                // closed-type flag rule above.
                AddGenericDefKindFlags(flagBits, cls.IsValueType, cls.IsInterface, cls.IsAbstract, cls.IsSealed);
                if (varMask != 0)
                    flagBits.Add("DN2CPP_TF_VARIANT");
                if (_e.IsCovariantGenericDef(cls))
                    flagBits.Add("DN2CPP_TF_COVARIANT");
                if (IsArrayGenericItfDef(gi.DefName))
                    flagBits.Add("DN2CPP_TF_ARRAY_GEN_ITF");
                string defFlags = flagBits.Count == 1 ? flagBits[0] : "(" + string.Join(" | ", flagBits) + ")";
                // name, base, instanceSize, vtable, interfaces, interfaceCount, tostring,
                // gethashcode, equals, flags, fields, fieldCount, methods, methodCount,
                // ctors, ctorCount, props, propCount, customAttrs, customAttrCount,
                // genericDef(self), genericArgs, genericArgCount.
                // External linkage always: a closed instantiation's type-info names the
                // definition handle from its metadata chunk TU (and with shared generics
                // on, a shared body's rgctx walk anchors on it from any body TU).
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo {sym};");
                // The ty_ companion keeps its extern declaration next to the definition
                // (only the def's own initializer needs the companion's address).
                _sb.AppendLine($"extern const Dn2CppType ty_{sym};");
                // A variant def spells the members between typeObject and varianceMask that the
                // trailing 0-fill convention would otherwise leave implicit (formatspec, ilAttrs,
                // metadataToken, defaultMemberName); an invariant one stops at typeObject exactly
                // as before, so its text is unchanged.
                string varTail = varMask != 0 ? $", nullptr, 0u, 0, nullptr, {varMask}" : "";
                string init = $"{{ \"{gi.DefName}\", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, {defFlags}, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, &{sym}, nullptr, 0, nullptr, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, nullptr, nullptr, &ty_{sym}{varTail} }}";
                _sb.AppendLine($"const Dn2CppTypeInfo {sym} = "
                    + WithGenericParams(init, Compilation.GenericParamNames(cls.Module, cls.Handle)) + ";");
                _sb.AppendLine($"const Dn2CppType ty_{sym} = {{ {{ &dn2cpp_type_type }}, &{sym} }};");
            }

            // Synthetic open-definition handles for a bare typeof(Def<>) no emitted closed
            // instantiation minted a gendef for above (that loop keys on TopoOrder's emitted
            // classes; an open definition names no ClassInfo). A body that lowered such a
            // typeof named &gendef_<sym> (recorded in TypeofOpenGenericDefs), so the symbol
            // MUST be defined or the C++ link fails on it (cut ⟹ route). Minimal — name +
            // self-referencing genericDef + the KIND bits resolved at the token site:
            // with no closed instantiation there is nothing to covariance-match or
            // array-interface-test against (both bits are only ever read off a CLOSED
            // target's genericDef), and MakeGenericType finding no registry candidate
            // throws, matching IL2CPP. GenericDefKind.Unknown — a definition no loaded
            // module declares — keeps the minimal flags, a best-effort degrade.
            foreach (var (defName, def) in _c.TypeofOpenGenericDefs.OrderBy(kv => kv.Key, System.StringComparer.Ordinal))
            {
                if (_e._genericDefSyms.ContainsKey(defName))
                    continue;
                string sym = "gendef_" + CppNaming.Sanitize(defName);
                _e._genericDefSyms[defName] = sym;
                var flagBits = new List<string> { "DN2CPP_TF_GENERICDEF" };
                AddGenericDefKindFlags(flagBits,
                    (def.Kind & GenericDefKind.ValueType) != 0,
                    (def.Kind & GenericDefKind.Interface) != 0,
                    (def.Kind & GenericDefKind.Abstract) != 0,
                    (def.Kind & GenericDefKind.Sealed) != 0);
                string defFlags = flagBits.Count == 1 ? flagBits[0] : "(" + string.Join(" | ", flagBits) + ")";
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo {sym};");
                _sb.AppendLine($"extern const Dn2CppType ty_{sym};");
                string synthInit = $"{{ \"{defName}\", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, {defFlags}, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, &{sym}, nullptr, 0, nullptr, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, nullptr, nullptr, &ty_{sym} }}";
                _sb.AppendLine($"const Dn2CppTypeInfo {sym} = "
                    + WithGenericParams(synthInit, def.ParamNames) + ";");
                _sb.AppendLine($"const Dn2CppType ty_{sym} = {{ {{ &dn2cpp_type_type }}, &{sym} }};");
            }

            // Rgctx tables (shared generics): one per grouped real instantiation
            // whose canonical owner registered slots — the per-instantiation values
            // the shared bodies read (wired into the type-info's trailing rgctx
            // member below, and passed directly by the hidden-parameter forwarders).
            if (_c.SharedGenericsEnabled)
            {
                foreach (var (user, entries) in _c.RgctxTables())
                {
                    foreach (var e in entries)
                        _e.RequireRenderedTypeInfoDefined(e, "rgctx type slot", user);
                    _o.Header.AppendLine($"extern const void* const rgctx_{user.CppName}[];");
                    _sb.AppendLine($"const void* const rgctx_{user.CppName}[] = {{ "
                        + string.Join(", ", entries.Select(e => $"(const void*)({e})")) + " };");
                    _rgctxTableSyms.Add(user);
                }
                // Method-dimension tables: one per real generic-method
                // instantiation bound to a context-using shared body. Never wired
                // into a type-info (method context is not receiver-derivable) —
                // only the instantiation's forwarder and forwarding slot entries
                // name them.
                int methodTables = 0;
                foreach (var (user, entries) in _c.RgctxMethodTables())
                {
                    foreach (var e in entries)
                        _e.RequireRenderedTypeInfoDefined(e, "rgctx method-dimension type slot",
                            user.DeclaringClass);
                    _o.Header.AppendLine($"extern const void* const rgctx_{user.CppName}[];");
                    _sb.AppendLine($"const void* const rgctx_{user.CppName}[] = {{ "
                        + string.Join(", ", entries.Select(e => $"(const void*)({e})")) + " };");
                    methodTables++;
                }
                _c.RgctxTableCount = _rgctxTableSyms.Count + methodTables;
                _c.RgctxSlotCount = _c.RgctxSlotTotal();
            }

            // The enums whose ti_ is actually emitted (from ReferencedTypes) — a field of
            // any other (un-emitted, opaque) class/enum type must not name a dangling ti_.
            _emittedEnums = _c.ReferencedTypes.Where(c => c.IsEnum).ToHashSet();

            EmitReferencedIntrinsicTypeInfos();

            // Class-major metadata emission: each class's complete metadata — vtable,
            // interface slot tables + unboxing thunks, field table + accessor thunks,
            // method/ctor/param/property tables + attribute factories + invoker thunks, and
            // finally its ti_/ty_ definitions — renders into one scratch block appended
            // whole, so it overflows into metadata-only chunk TUs and is never split across
            // a chunk boundary. Every file-local symbol a block references is defined inside
            // that block; cross-class references resolve through the header externs. Two
            // dedup scopes:
            //
            // - PER-CHUNK: the invoker-thunk set and the slotmiss_/invmiss_ stub maps. They
            //   name functions, whose duplicate `static` definitions across TUs are legal
            //   and deterministic, so each chunk simply (re)defines what it references.
            //   Reset BEFORE rendering whenever the upcoming append opens a fresh chunk —
            //   sound because MetadataWillRoll ignores the incoming block's size, so the
            //   verdict cannot change between this check and the append.
            // - GLOBAL (whole emission): the intern pools (parmpool_/itfpool_/itfspool_/
            //   genargpool_), which deliberately survive the chunk roll below — duplicate
            //   DATA would be duplicate bytes in the binary. Soundness argument at the
            //   pool-map declarations above.
            //
            // Rendering order (TopoOrder) and the roll rule are deterministic, so chunk
            // boundaries — and therefore the whole output — reproduce byte-identically (the
            // self-host fixpoint relies on it). Rendering also interns string literals and
            // attr-factory sequence numbers in class-major order; those indices are
            // emission-order-significant but carry no cross-section invariant.
            var block = new StringBuilder();
            foreach (var cls in _e.TopoOrder())
            {
                if (cls.IsEnum)
                    continue;
                // A runtime-OWNED type contributes no metadata block at all: the runtime
                // defines its type-info, its Type object and its tables, and every reference
                // goes there. Skipping the WHOLE block rather than just the type-info is
                // what keeps the vtable/field/member tables from being emitted as statics
                // nothing names — for the primitives, which are not opaque, those are real
                // tables.
                if (CoreIntrinsics.RuntimeOwnsTypeInfo(cls.FullName))
                    continue;
                block.Clear();
                if (_o.MetadataWillRoll)
                {
                    _invokerThunks.Clear();
                    _slotStubs.Clear();
                    _invokerMissStubs.Clear();
                }
                _sb = block;
                RenderVtable(cls);
                RenderItfTables(cls);
                RenderFieldTable(cls);
                RenderMemberTables(cls);
                RenderTypeInfo(cls);
                _sb = _o.Data;
                if (block.Length > 0)
                    _o.AppendMetadata(block.ToString());
            }

            _e.EmitArrayTypeInfos(_sb, _emittedEnums);
            _e.EmitMdArrayItfMap(_sb);
            _e.EmitArrayNongenericItfRows(_sb);
            _e.EmitStringInterfaceMap(_sb);
            _e.EmitEnumInterfaceMap(_sb);
            _e.EmitIntrinsicInterfaceMaps(_sb);
            _sb.AppendLine();

            // Last statement of the emission, and it has to be: this object is unreferenced
            // the moment it returns (EmitTypeInfos keeps no field), so a census anywhere
            // later would report the pools as free rather than as big.
            Census();
        }

        /// <summary>Hands the emitter's own tables to <see cref="EmitCensus"/>. Gated at
        /// the call site rather than inside it: summing the lengths of ~40,000 pooled
        /// initializer strings is real work, and a diagnostic that costs what it measures
        /// on every transpile is worse than no diagnostic.</summary>
        private void Census()
        {
            if (!EmitCensus.Enabled)
                return;
            long parmChars = 0;
            foreach (var kv in _parmPools)
                parmChars += kv.Key.Length + kv.Value.Length;
            long itfChars = 0;
            foreach (var kv in _itfPools)
                itfChars += kv.Key.Length + kv.Value.Length;
            long genargChars = 0;
            foreach (var kv in _genargPools)
                genargChars += kv.Key.Length + kv.Value.Length;
            long itfsChars = 0;
            foreach (var kv in _itfsPools)
                itfsChars += kv.Key.Length + kv.Value.Length;
            long memberAddrChars = 0;
            foreach (var kv in _memberAddr)
                memberAddrChars += kv.Value.Length;
            long tabChars = 0;
            foreach (var kv in _fieldTabs)
                tabChars += kv.Value.Expr.Length;
            foreach (var kv in _methodTabs)
                tabChars += kv.Value.Expr.Length;
            foreach (var kv in _ctorTabs)
                tabChars += kv.Value.Expr.Length;
            foreach (var kv in _propTabs)
                tabChars += kv.Value.Expr.Length;
            long itfSymChars = 0;
            foreach (var kv in _itfTabSyms)
                itfSymChars += kv.Value.Length;
            long stubChars = 0;
            foreach (var kv in _slotStubs)
                stubChars += kv.Key.Length + kv.Value.Length;
            foreach (var kv in _invokerMissStubs)
                stubChars += kv.Key.Length + kv.Value.Length;
            long thunkChars = 0;
            foreach (var k in _invokerThunks)
                thunkChars += k.Length;
            EmitCensus.ReportMetadata(
                _parmPools.Count, parmChars, _itfPools.Count, itfChars,
                _genargPools.Count, genargChars, _itfsPools.Count, itfsChars,
                _memberAddr.Count, memberAddrChars,
                _fieldTabs.Count + _methodTabs.Count + _ctorTabs.Count + _propTabs.Count, tabChars,
                _itfTabSyms.Count, itfSymChars,
                _slotStubs.Count + _invokerMissStubs.Count, stubChars,
                _invokerThunks.Count, thunkChars);
        }

        // Type.Assembly identity: the type's defining-assembly simple name as a C string
        // literal (or nullptr when unknown — the runtime then reports CoreLib). Assembly
        // names are plain dotted identifiers, so no escaping is needed.
        private static string AsmLit(ClassInfo c) =>
            c.Module is { AssemblyName: { Length: > 0 } an } ? $"\"{an}\"" : "nullptr";

        // Emit (once per chunk) a stub that aborts with `desc` baked in and return its
        // symbol, for a slot the emitter cannot enter through the receiver. `reporter` is
        // the runtime entry point (dn2cpp_itf_slot_missing_named / _vcall_unimplemented_named).
        // The stub's C++ signature is irrelevant — it is entered through a mismatched cast
        // and never returns — so it is declared void() and taken as (const void*), exactly
        // like the shared _anon / _vcall traps it stands in for.
        private string SlotMissStub(string reporter, string desc)
        {
            string key = reporter + "\0" + desc;
            if (!_slotStubs.TryGetValue(key, out var name))
            {
                name = $"slotmiss_{_slotStubSeq++}";
                string lit = desc.Replace("\\", "\\\\").Replace("\"", "\\\"");
                _sb.AppendLine($"[[maybe_unused]] static void {name}() {{ {reporter}(\"{lit}\"); }}");
                _slotStubs[key] = name;
            }
            return name;
        }

        // Emit (once per chunk) the invoker-ABI trap stub standing in for a member-table
        // row's invoker thunk that cannot be materialized (see _invokerMissStubs). Unlike a
        // slotmiss_ stub this one is ENTERED through its real signature — the interpreter
        // and MethodBase.Invoke both call invokers with the shared (fn, self, args, retType)
        // ABI — so it carries that exact C++ signature and raises the CATCHABLE
        // NotSupportedException instead of aborting: the caller is a program that asked to
        // invoke a member, not a corrupted dispatch table, and the diagnosis has to reach
        // it. Deduplicated per chunk on the descriptor text; the sequence counter stays
        // monotonic across the emission.
        private string InvokerMissStub(ClassInfo cls, MethodInfo m, string blockedType)
        {
            // ASCII only: the text is a runtime exception message (terminals, logs,
            // frozen gate transcripts), not a source comment.
            string desc = $"{cls.FullName}.{m.Name}: no reflection invoker in this image "
                + $"(the signature names {blockedType}, whose layout was never emitted)";
            if (!_invokerMissStubs.TryGetValue(desc, out var name))
            {
                name = $"invmiss_{_invokerMissSeq++}";
                string lit = desc.Replace("\\", "\\\\").Replace("\"", "\\\"");
                _sb.AppendLine($"static Dn2CppObject* {name}([[maybe_unused]] void* fn, "
                    + "[[maybe_unused]] Dn2CppObject* self, [[maybe_unused]] Dn2CppObject** args, "
                    + $"[[maybe_unused]] const Dn2CppTypeInfo* retType) {{ dn2cpp_throw_not_supported_msg(\"{lit}\"); }}");
                _invokerMissStubs[desc] = name;
            }
            return name;
        }

        // Vtable: the class's virtual dispatch table, file-local — referenced only
        // by the class's own type-info initializer, within the same block.
        private void RenderVtable(ClassInfo cls)
        {
            if (cls.IsValueType || cls.IsEnum || cls.IsInterface || cls.IsDelegate || _e.IsOpaque(cls)
                || _e.SkipsCanonicalMetadata(cls))
                return;
            // A slot with no reached body is a slot nothing dispatches, but it holds a TRAP
            // rather than a null pointer: reaching one means the reachability model was
            // wrong about the slot and has to say which type and method it was wrong about,
            // where a null slot is a call through address 0 naming nothing. The shared trap
            // recovers the name at run time from the receiver's method table, so vtable
            // initializers stay internable. A struct-returning virtual is the exception —
            // the caller's hidden result buffer occupies `self`, so neither type nor method
            // is readable; when the emitter knows the method, bake its descriptor into a
            // per-slot stub instead.
            var entries = cls.Vtable.Select(m =>
            {
                if (m is not null && _c.Reachable.Contains(m))
                    return $"(const void*)&{m.Emittable.CppName}";
                if (m is not null && !ReceiverIsFirstArg(m.Signature.ReturnType))
                    return $"(const void*)&{SlotMissStub("dn2cpp_vcall_unimplemented_named", $"{cls.FullName}.{m.Name}")}";
                return "(const void*)&dn2cpp_vcall_unimplemented";
            }).ToList();
            if (entries.Count == 0)
                entries.Add("(const void*)&dn2cpp_vcall_unimplemented");
            _sb.AppendLine($"[[maybe_unused]] static const void* {cls.CppVtableName}[] = {{ {string.Join(", ", entries)} }};");
        }

        // Interface dispatch tables: one per (concrete class, interface),
        // resolved against the class itself so overrides bind correctly.
        private void RenderItfTables(ClassInfo cls)
        {
            // Reference types always get dispatch tables; a value type only when it is
            // boxed (a never-boxed one is dispatched solely via direct constrained calls).
            // An interface or abstract class gets a RELATION-ONLY table (nullptr slots; see
            // the relationOnly branch below): the `.itf`-only readers
            // (dn2cpp_type_is_assignable_from, dn2cpp_type_get_interfaces, dn2cpp_isinst,
            // dn2cpp_array_elem_assignable) need the rows, or IsAssignableFrom over two
            // interfaces silently answers False. The slot-resolving walkers skip
            // nullptr-slot rows, so a relation row on an abstract base can never shadow a
            // concrete ancestor's real slot table.
            if (cls.IsEnum || _e.SkipsCanonicalMetadata(cls) || _e.IsIntrinsicShaped(cls))
                return;
            // Three kinds get a relation-only table rather than none, each otherwise a
            // silent False out of IsAssignableFrom / an empty GetInterfaces():
            //
            // - a NEVER-BOXED value type: no instance to dispatch on, so the slots would
            //   resolve nothing — and the slot half is where the cost lives, since a value
            //   type's rows need one unboxing THUNK per interface method (code, not data).
            // - an OPAQUE shell, reached by a type token alone: nothing allocates it, so
            //   nothing dispatches through it, and its rows stay filtered by the emit set
            //   below so the table drags in nothing. That filter is why the answer can be a
            //   SUBSET of .NET's — an interface no other reachable type mentions has no
            //   type-info to point at — monotone in the right direction (every row present
            //   is a true relation), the same unsoundness the AOT reflection model carries.
            // - an ABSTRACT class or an INTERFACE.
            bool relationOnly = cls.IsInterface || cls.IsAbstract || _e.IsOpaqueShell(cls)
                || (cls.IsValueType && !_c.IsAllocated(cls));
            var transitive = new List<ClassInfo>();
            for (var c = cls; c is not null; c = c.BaseClass)
                foreach (var itf in c.Interfaces)
                    // Skip interfaces inherited from an opaque intrinsic base
                    // (e.g. Exception : ISerializable) — they are not in the emit
                    // set, so their dispatch tables/type-info aren't materialized
                    // and we never dispatch through them.
                    if (_e._emit.Contains(itf) && !transitive.Contains(itf))
                        transitive.Add(itf);
            if (transitive.Count == 0)
                return;
            _itfTables[cls] = transitive;

            // Relation-only table: rows carry the interface type-infos, slots stay nullptr —
            // implementation resolution against a type that can have no instance is
            // meaningless (an interface) or moot (an abstract class, whose every dispatch
            // resolves against the concrete receiver's own table, a superset of this one).
            // Canonical alias rows are deliberately NOT appended: they serve a shared body's
            // dispatch against a canonical handle, and a relation table serves no dispatch —
            // adding them would leak alias type-infos into the reflection-visible interface
            // list of a type nothing instantiates.
            if (relationOnly)
            {
                var relEntries = transitive
                    .Select(itf => $"{{ {_e.TypeInfoRef(itf, "interface relation-only table row")}, nullptr }}")
                    .ToList();
                _itfRowCounts[cls] = relEntries.Count;
                InternItfEntryTable(cls, string.Join(", ", relEntries));
                return;
            }

            // The class's slot-table symbol per interface — a pooled itfpool_
            // array (see the intern below), shared with any earlier table in
            // the emission whose initializer is byte-identical.
            var slotSyms = new Dictionary<ClassInfo, string>();
            foreach (var itf in transitive)
            {
                var slots = new List<string>();
                var ims = itf.Methods.ToList();
                for (int i = 0; i < ims.Count; i++)
                {
                    var im = ims[i];
                    // Null when the class has no implementation for the interface method —
                    // e.g. a boxed primitive's corlib ClassInfo gets a full interface table
                    // but IntPtr has no concrete IBinaryInteger.DivRem.
                    var impl = Compilation.ResolveItfImplOrNull(cls, im);
                    // A slot with no resolvable impl, or whose impl is unreachable, is never
                    // dispatched through — degrade it to a TRAP, not a null pointer.
                    // "Never dispatched" is a claim about the reachability closure, and when
                    // that claim is wrong (the runtime resolves a variance-satisfied row the
                    // closure never filled) a null slot is a call through 0x0: a SIGSEGV
                    // naming nothing.
                    //
                    // Which trap depends on the slot's RETURN SHAPE. A slot is entered
                    // through a pointer cast to the dispatch site's signature, whose first
                    // parameter is the receiver, so the shared trap can read the receiver's
                    // type name out of argument 0 — unless the return type is a struct
                    // returned INDIRECTLY, where the ABI spends that register on the hidden
                    // result pointer and argument 0 is a raw buffer. Real slots return such
                    // structs (IEnumerator<KeyValuePair<K,V>>.Current, ValueTuple, Decimal,
                    // Nullable<T>), so anything not void, scalar or pointer takes the
                    // anonymous trap: it loses the name, never the abort. Both traps are ONE
                    // shared symbol, so nothing is emitted per class and the pooled slot
                    // tables intern unchanged.
                    if (impl is null || !_c.Reachable.Contains(impl))
                    {
                        // Receiver-readable slot: the shared trap reads its type out of
                        // argument 0. Struct-returning slot: argument 0 may be the hidden
                        // result buffer, so bake the (class, interface, method) descriptor
                        // into a per-slot stub instead of losing the name to _anon.
                        slots.Add(ReceiverIsFirstArg(im.Signature.ReturnType)
                            ? "(const void*)&dn2cpp_itf_slot_missing"
                            : $"(const void*)&{SlotMissStub("dn2cpp_itf_slot_missing_named", $"{cls.FullName}::{itf.FullName}.{im.Name}")}");
                        continue;
                    }
                    // A reference-type impl receives the object pointer directly as
                    // `this`. A boxed value-type impl, however, expects the unboxed
                    // payload (`obj + 1`), so its interface slot points to an
                    // unboxing thunk that offsets past the box header before calling
                    // the impl — the dispatch site is type-agnostic and just calls
                    // through the slot with the object pointer.
                    if (!cls.IsValueType)
                    {
                        // Usually the implementation symbol goes straight into the
                        // slot; a concrete implementer of an interface whose CANONICAL
                        // form erases a headerless intrinsic parameter needs an
                        // unwrapping thunk instead (NfiErasedSlotThunk explains why the
                        // caller's wrap is right for the shared body and wrong here).
                        slots.Add("(const void*)&"
                            + (_e.NfiErasedSlotThunk(_sb, cls, itf, im, impl, i)
                               ?? impl.Emittable.CppName));
                        continue;
                    }
                    string thunk = $"ubthunk_{cls.CppName}_{itf.CppName}_{i}";
                    EmitUnboxingThunk(_sb, thunk, cls, im, impl);
                    slots.Add($"(const void*)&{thunk}");
                }
                // A zero-length array is a GNU/clang extension MSVC rejects (C2466);
                // this happens for a marker interface with no methods of its own
                // (e.g. IUnsignedNumber<T>, or a user interface like IAnimal). The
                // table is never indexed in that case (there is no method to dispatch
                // through it), so a single dummy nullptr element is a behavior-neutral
                // portable stand-in.
                string slotsInit = slots.Count > 0
                    ? string.Join(", ", slots)
                    : "nullptr /* unused: 0-method interface */";
                // Intern byte-identical slot tables across the whole emission
                // (first definition wins; see the pool-map declarations above).
                // The array's element type is const but the array itself is not,
                // so the plain namespace-scope definition already has external
                // linkage — no `extern` keyword (clang's -Wextern-initializer
                // fires on an extern non-const definition). A value-type table
                // embeds this class's unboxing-thunk symbols, so it is unique by
                // construction and simply claims a pool entry of its own.
                if (_itfPools.TryGetValue(slotsInit, out var pooled))
                {
                    slotSyms[itf] = pooled;
                }
                else
                {
                    string tn = $"itfpool_{_itfPoolSeq++}";
                    _sb.AppendLine($"const void* {tn}[] = {{ {slotsInit} }};");
                    _o.Header.AppendLine($"extern const void* {tn}[];");
                    _itfPools[slotsInit] = tn;
                    slotSyms[itf] = tn;
                }
            }
            var entries = transitive.Select(itf => $"{{ {_e.TypeInfoRef(itf, "interface-dispatch table row")}, {slotSyms[itf]} }}").ToList();
            // Canonical alias rows: for each implemented closed generic
            // interface whose canonical form differs, add a row carrying the
            // canonical interface's type-info over the SAME slot table, so a
            // shared body's dn2cpp_resolve_interface against the canonical
            // handle (e.g. ti_IEqualityComparer_$CnInt32) dispatches on this
            // receiver — including a user comparer implementing
            // IEqualityComparer<SomeEnum>. First row wins when two real
            // interfaces collapse onto one canonical form (deterministic; such
            // a dispatch is inherently ambiguous and the colliding class is
            // vtable-shape-dropped from sharing anyway).
            if (_c.SharedGenericsEnabled)
            {
                var aliased = new HashSet<ClassInfo>(transitive);
                foreach (var itf in transitive)
                    if (_c.CanonicalInterfaceOf(itf) is { } citf && aliased.Add(citf))
                        entries.Add($"{{ {_e.TypeInfoRef(citf, "interface-dispatch table (canonical alias row)")}, {slotSyms[itf]} }}");
            }
            _itfRowCounts[cls] = entries.Count;
            InternItfEntryTable(cls, string.Join(", ", entries));
        }

        // Intern byte-identical interface-entry tables across the whole
        // emission, same first-definition-wins rule as the slot-table pool
        // above. A const array defaults to internal linkage at namespace
        // scope, so the definition needs the explicit `extern`.
        private void InternItfEntryTable(ClassInfo cls, string itfsInit)
        {
            if (_itfsPools.TryGetValue(itfsInit, out var pooledTab))
            {
                _itfTabSyms[cls] = pooledTab;
            }
            else
            {
                string tabName = $"itfspool_{_itfsPoolSeq++}";
                _sb.AppendLine($"extern const Dn2CppInterfaceEntry {tabName}[] = {{ {itfsInit} }};");
                _o.Header.AppendLine($"extern const Dn2CppInterfaceEntry {tabName}[];");
                _itfsPools[itfsInit] = tabName;
                _itfTabSyms[cls] = tabName;
            }
        }

        // Enum reflection field table, matching real .NET: one public instance `value__`
        // row (typed at the underlying primitive) plus one public static literal row per
        // member (typed at the enum itself), in metadata declaration order. Reuses the
        // fldtab_ machinery — a literal row has no storage to thunk-read, so it carries its
        // constant in the trailing literalValue member and the runtime getter boxes it;
        // value__ gets a real getter thunk boxing the receiver's payload at the underlying
        // type. Rows are read straight off the metadata (no ClassInfo.Fields/FieldInfo.Type
        // decode), so alias members — two names, one value, deduped out of enummembers_ —
        // keep their own rows like real .NET.
        //
        // --trim-reflection interplay: every enum reaching here is in ReferencedTypes,
        // which is a keep-set seed, so it is always kept and never needs
        // DN2CPP_TF_METADATA_STRIPPED. The KeepsReflectionMetadata gate below states that
        // dependency; if the keep rules narrow past it, a stripped enum must start carrying
        // the bit (see dn2cpp_require_metadata).
        private (string Expr, int Count) RenderEnumFieldTable(ClassInfo en)
        {
            if (en.Handle.IsNil || !_c.KeepsReflectionMetadata(en))
                return ("nullptr", 0);
            var rows = new List<string>();
            try
            {
                var reader = en.Module.Reader;
                var td = reader.GetTypeDefinition(en.Handle);
                string underlyingTi = MethodCompiler.TypeInfoExprOf(
                    TypeDesc.MakePrimitive(en.EnumUnderlying)) ?? "&dn2cpp_int32_type";
                bool userFldCls = _c.IsUserModule(en.Module);
                foreach (var fdh in td.GetFields())
                {
                    var fd = reader.GetFieldDefinition(fdh);
                    string fname = reader.GetString(fd.Name);
                    bool literal = (fd.Attributes & System.Reflection.FieldAttributes.Literal) != 0;
                    bool isStatic = (fd.Attributes & System.Reflection.FieldAttributes.Static) != 0;
                    var access = fd.Attributes & System.Reflection.FieldAttributes.FieldAccessMask;
                    var bits = new List<string>();
                    if (isStatic) bits.Add("DN2CPP_FLDA_STATIC");
                    if (access == System.Reflection.FieldAttributes.Public) bits.Add("DN2CPP_FLDA_PUBLIC");
                    if (access == System.Reflection.FieldAttributes.Private) bits.Add("DN2CPP_FLDA_PRIVATE");
                    if ((fd.Attributes & System.Reflection.FieldAttributes.InitOnly) != 0)
                        bits.Add("DN2CPP_FLDA_INITONLY");
                    if (literal) bits.Add("DN2CPP_FLDA_LITERAL");
                    string attrs = bits.Count == 0 ? "0" : string.Join(" | ", bits);
                    // A literal member's constant, at full 64-bit width (rendered in
                    // hex so long.MinValue needs no unary minus). The int32
                    // truncation EnumMembers applies for the model does not apply
                    // here — the runtime boxes at the enum's own model width.
                    long lv = 0;
                    string get = "nullptr";
                    string ftInfo;
                    if (literal)
                    {
                        ftInfo = _e.TypeInfoRef(en, "enum literal field row's field type");
                        var ch = fd.GetDefaultValue();
                        if (!ch.IsNil)
                        {
                            var constant = reader.GetConstant(ch);
                            var blob = reader.GetBlobReader(constant.Value);
                            lv = constant.TypeCode switch
                            {
                                ConstantTypeCode.SByte => blob.ReadSByte(),
                                ConstantTypeCode.Byte => blob.ReadByte(),
                                ConstantTypeCode.Int16 => blob.ReadInt16(),
                                ConstantTypeCode.UInt16 => blob.ReadUInt16(),
                                ConstantTypeCode.Int32 => blob.ReadInt32(),
                                ConstantTypeCode.UInt32 => blob.ReadUInt32(),
                                ConstantTypeCode.Int64 => blob.ReadInt64(),
                                ConstantTypeCode.UInt64 => unchecked((long)blob.ReadUInt64()),
                                _ => 0,
                            };
                        }
                    }
                    else if (!isStatic)
                    {
                        // value__: box the receiver's payload as the underlying
                        // primitive (a boxed Tier answers a boxed Byte, like .NET).
                        // The payload is stored at the enum's MODEL width (int32,
                        // int64 for 64-bit underlyings — CppTypes.Of), read at that
                        // width and narrowed to the underlying type for the box.
                        ftInfo = underlyingTi;
                        string readT = en.EnumUnderlying is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
                            ? "int64_t" : "int32_t";
                        string underT = CppTypes.Of(TypeDesc.MakePrimitive(en.EnumUnderlying));
                        string gname = $"fldget_{en.CppName}_{fname}";
                        _sb.AppendLine($"static Dn2CppObject* {gname}(Dn2CppObject* o) {{ "
                            + $"{underT} v = ({underT})*({readT}*)((Dn2CppObject*)o + 1); "
                            + $"return dn2cpp_box({ftInfo}, &v, sizeof({underT})); }}");
                        get = $"&{gname}";
                    }
                    else
                    {
                        // A non-literal static field on an enum type does not occur
                        // in C#; report the row (typed at the underlying) with null
                        // thunks rather than invent storage for it.
                        ftInfo = underlyingTi;
                    }
                    (string Expr, int Count) ca = ("nullptr", 0);
                    if (userFldCls)
                        ca = _e.BuildAttrTable(_sb, $"{en.CppName}_fld{rows.Count}", en.Module,
                            fd.GetCustomAttributes());
                    int fldToken = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(fdh);
                    rows.Add($"{{ \"{fname}\", {_e.TypeInfoRef(en, "enum field row's declaring type")}, {ftInfo}, {attrs}, {get}, nullptr, "
                        + $"{ca.Expr}, {ca.Count}, 0x{(int)fd.Attributes:X}, {fldToken}, "
                        + $"(int64_t)0x{(ulong)lv:X}ULL }}");
                }
            }
            catch (System.Exception e) when (!Compilation.IsMustEscape(e))
            {
                // An enum without decodable field metadata reports no fields.
                return ("nullptr", 0);
            }
            if (rows.Count == 0)
                return ("nullptr", 0);
            _sb.AppendLine($"static const Dn2CppFieldInfo fldtab_{en.CppName}[] = {{ {string.Join(", ", rows)} }};");
            return ($"fldtab_{en.CppName}", rows.Count);
        }

        // Per-type reflection field tables: one Dn2CppFieldInfo[] per type that
        // declares fields, referenced by the type-info's fields/fieldCount below. Emitted
        // before the type-info definition so the initializer can name it. Each entry
        // carries the field's name, declaring/field type-info, and accessibility bits.
        private void RenderFieldTable(ClassInfo cls)
        {
            // Guard mirrored by NoteReflectedMemberArrayElements (which pre-notes the
            // SZArray field types this table names) — keep the two in step.
            if (cls.IsEnum || cls.Fields.Count == 0 || _e.IsCanonicalWorld(cls)
                || !_c.KeepsReflectionMetadata(cls))
                return;
            // Field custom-attribute + token lookup: map field name ->
            // FieldDefinitionHandle. Collected for every module (the metadata token is
            // emitted on BCL rows too); the attribute factories below stay gated to
            // user-module (app + referenced library) non-opaque classes.
            bool userFldCls = _c.IsUserModule(cls.Module) && !_e.IsOpaque(cls);
            var fieldHandles = new Dictionary<string, FieldDefinitionHandle>();
            try
            {
                var reader = cls.Module.Reader;
                var td = reader.GetTypeDefinition(cls.Handle);
                foreach (var fh in td.GetFields())
                    fieldHandles[reader.GetString(reader.GetFieldDefinition(fh).Name)] = fh;
            }
            catch (Exception e) when (!Compilation.IsMustEscape(e))
            { /* no decodable field metadata */ }
            var rows = new List<string>();
            foreach (var f in cls.Fields)
            {
                var bits = new List<string>();
                if (f.IsStatic) bits.Add("DN2CPP_FLDA_STATIC");
                if (f.IsPublic) bits.Add("DN2CPP_FLDA_PUBLIC");
                if (f.IsPrivate) bits.Add("DN2CPP_FLDA_PRIVATE");
                if ((f.Attributes & System.Reflection.FieldAttributes.InitOnly) != 0)
                    bits.Add("DN2CPP_FLDA_INITONLY");
                if (f.IsLiteral) bits.Add("DN2CPP_FLDA_LITERAL");
                string attrs = bits.Count == 0 ? "0" : string.Join(" | ", bits);
                string ftInfo = _e.FieldTypeInfoExpr(f.Type, _emittedEnums);
                // GetValue/SetValue accessor thunks, emitted only when the
                // field is a real member of the emitted layout: a non-literal field of
                // a normally-emitted struct. Delegates (synthetic f_target/f_method
                // layout) and intrinsic-/opaque-layout types don't carry their metadata
                // fields, so their thunks would not compile — leave those null (reflected
                // GetValue/SetValue then throws). A value-type field is boxed/unboxed via
                // its field type-info; a reference field passes the object pointer
                // through; a value-type receiver's payload is at o+1.
                // An [InlineArray] struct lays its single field out as a C array
                // (f_name[N]), which is neither copyable nor assignable — skip its thunks.
                (string get, string set) = ("nullptr", "nullptr");
                if (!f.IsLiteral && !_e.IsOpaque(cls) && !cls.IsDelegate
                    && cls.IntrinsicCppName is null && cls.InlineArrayLength <= 0)
                {
                    string cppT = CppTypes.Of(f.Type);
                    bool isRef = cppT.EndsWith("*");
                    // The spelling the member is actually declared with: a
                    // shared struct layout carries the canonical owner's erased
                    // field types, so the setter must cast to that spelling
                    // (statics stay per-real and keep the real spelling).
                    string memberT = cppT;
                    if (!f.IsStatic && _c.SharedGenericsEnabled
                        && cls.SharedOwner is { } layoutOwner
                        && layoutOwner.Fields.FirstOrDefault(x => x.Name == f.Name) is { } ownerFld)
                        memberT = CppTypes.Of(ownerFld.Type);
                    string access = f.IsStatic
                        ? f.CppStaticAccess
                        : (cls.IsValueType
                            ? $"(({cls.CppStructName}*)((Dn2CppObject*)o + 1))->{f.CppName}"
                            : $"(({cls.CppStructName}*)o)->{f.CppName}");
                    string getName = $"fldget_{cls.CppName}_{f.CppName}";
                    string setName = $"fldset_{cls.CppName}_{f.CppName}";
                    // A reflected static-field access must run the declaring type's
                    // .cctor first (.NET's lazy-initialization guarantee): unlike a
                    // compiled use site, this thunk has no emitted first-use guard,
                    // and under a deferred startup pass (the Godot mono-module
                    // backend) it can otherwise observe a not-yet-initialized static.
                    string ensure = "";
                    if (f.IsStatic && cls.StaticCctor is { } fldCc)
                    {
                        _c.NoteCctorEnsure(fldCc);
                        ensure = $"{fldCc.CppName}__ensure(); ";
                    }
                    // A headerless-typed field (the NFI trio, Assembly/Module — the
                    // headerless intrinsic pointers): a reflected GetValue is an
                    // escape into `object`, so it wraps like every other escape
                    // (MethodCompiler.Cast), and SetValue unwraps symmetrically.
                    // Keyed on the DECLARED member spelling: a shared-layout field
                    // (memberT erased to Dn2CppObject*) already holds the
                    // managed-object form, so it takes the plain reference path.
                    bool isHeaderless = MethodCompiler.IsHeaderlessWrapCpp(memberT);
                    string getBody = ensure + (isHeaderless
                        ? $"return {MethodCompiler.HeaderlessWrapExpr(access, memberT, f.Type)};"
                        : isRef
                        ? $"return (Dn2CppObject*)({access});"
                        : $"{cppT} v = {access}; return dn2cpp_box({ftInfo}, &v, sizeof({cppT}));");
                    string setBody = ensure + (isHeaderless
                        ? $"{access} = {MethodCompiler.HeaderlessUnwrapExpr("val", memberT)};"
                        : isRef
                        ? $"{access} = ({memberT})val;"
                        : $"{access} = *({cppT}*)((char*)val + sizeof(Dn2CppObject));");
                    _sb.AppendLine($"static Dn2CppObject* {getName}([[maybe_unused]] Dn2CppObject* o) {{ {getBody} }}");
                    _sb.AppendLine($"static void {setName}([[maybe_unused]] Dn2CppObject* o, [[maybe_unused]] Dn2CppObject* val) {{ {setBody} }}");
                    (get, set) = ($"&{getName}", $"&{setName}");
                }
                (string Expr, int Count) ca = ("nullptr", 0);
                int fldToken = 0;
                if (fieldHandles.TryGetValue(f.Name, out var fdh))
                {
                    if (userFldCls)
                        ca = _e.BuildAttrTable(_sb, $"{cls.CppName}_fld{rows.Count}", cls.Module,
                            cls.Module.Reader.GetFieldDefinition(fdh).GetCustomAttributes());
                    fldToken = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(fdh);
                }
                rows.Add($"{{ \"{f.Name}\", {_e.TypeInfoRef(cls, "field row's declaring type")}, {ftInfo}, {attrs}, {get}, {set}, {ca.Expr}, {ca.Count}, 0x{(int)f.Attributes:X}, {fldToken} }}");
            }
            _sb.AppendLine($"static const Dn2CppFieldInfo fldtab_{cls.CppName}[] = {{ {string.Join(", ", rows)} }};");
            _fieldTabs[cls] = ($"fldtab_{cls.CppName}", rows.Count);
        }

        // Builds one Dn2CppMethodInfo[] table (named "<prefix>_<cls>") for the given
        // members, emitting their parmtab_ arrays and invoker thunks; returns null for
        // an empty list. Shared by the method and constructor tables.
        private (string Expr, int Count)? BuildMemberTable(ClassInfo cls, List<MethodInfo> members, string prefix)
        {
            if (members.Count == 0)
                return null;
            string tab = $"{prefix}_{cls.CppName}";
            var rows = new List<string>();
            bool appCls = cls.Module == _c.AppModule && !_e.IsOpaque(cls);
            // Attribute factories cover user-module (app + referenced library) non-opaque
            // classes, so a library class's method/param attributes are emitted; the
            // row-trim rule below stays app-module-keyed (a library's *own* unreached
            // rows are still trimmed — this only widens which classes get attribute tables).
            bool attrCls = _c.IsUserModule(cls.Module) && !_e.IsOpaque(cls);
            // A reference assembly's unreached members are reflected but never callable:
            // their fnPtr/invoker are null below, so Invoke throws and the row is nothing
            // but GetMethods() bloat. Drop them. An app module keeps every declared member,
            // so GetMethods() over the user's own types is unchanged, mirroring how a
            // program that reflects-and-invokes force-reaches app-module bodies only. A
            // hot-update base keeps everything: the interpreter binds a patch's imports by
            // walking these tables.
            // (Row filter mirrored by NoteReflectedMemberArrayElements — keep in step.)
            bool trim = !appCls && !_e._hotUpdateBase;
            foreach (var m in members)
            {
                // Rva == 0 is a bodiless declaration -- an interface or abstract slot,
                // which is never reached (dispatch reaches the impl) yet must stay
                // visible, or the type's GetMethods() would come back empty.
                if (trim && m.Rva != 0 && !_c.Reachable.Contains(m))
                    continue;
                _memberAddr[m] = $"&{tab}[{rows.Count}]";
                var bits = new List<string>();
                if (m.IsStatic) bits.Add("DN2CPP_MTHA_STATIC");
                if (m.IsPublic) bits.Add("DN2CPP_MTHA_PUBLIC");
                if (m.IsPrivate) bits.Add("DN2CPP_MTHA_PRIVATE");
                if ((m.Attributes & System.Reflection.MethodAttributes.SpecialName) != 0)
                    bits.Add("DN2CPP_MTHA_SPECIALNAME");
                // A generic method's closed instantiation (MethodBase.IsGenericMethod);
                // ECMA MethodAttributes carries no genericness bit, so it rides ours.
                if (m.Context.MethodArgs.Length > 0)
                    bits.Add("DN2CPP_MTHA_GENERIC");
                string attrs = bits.Count == 0 ? "0" : string.Join(" | ", bits);
                // Method + parameter custom attributes: the MethodDefinition's own
                // attributes, and each Parameter's, for an app-module non-opaque class.
                // Parameter handles are collected for every module: the raw
                // ParameterAttributes word (IsOptional/HasDefault) is emitted on BCL
                // rows too, unlike the attribute factories.
                (string Expr, int Count) mca = ("nullptr", 0);
                var paramHandles = new Dictionary<int, ParameterHandle>();
                try
                {
                    var rdr = m.Module.Reader;
                    var md = rdr.GetMethodDefinition(m.Handle);
                    if (attrCls)
                        mca = _e.BuildAttrTable(_sb, $"{prefix}_{cls.CppName}_{rows.Count}", m.Module, md.GetCustomAttributes());
                    foreach (var pph in md.GetParameters())
                    {
                        var par = rdr.GetParameter(pph);
                        if (par.SequenceNumber >= 1)
                            paramHandles[par.SequenceNumber - 1] = pph;
                    }
                }
                catch (Exception e) when (!Compilation.IsMustEscape(e))
                { /* no decodable method metadata */ }
                var ps = m.Signature.ParameterTypes;
                string paramsExpr = "nullptr";
                if (ps.Length > 0)
                {
                    var names = ParamNames(m, ps.Length);
                    var prows = new List<string>();
                    for (int i = 0; i < ps.Length; i++)
                    {
                        string ptInfo = _e.MemberTypeInfoExpr(ps[i], _emittedEnums);
                        string nm = names[i] is { Length: > 0 } pn ? $"\"{pn}\"" : "nullptr";
                        (string Expr, int Count) pca = ("nullptr", 0);
                        int pAttrs = 0;
                        if (paramHandles.TryGetValue(i, out var pph))
                        {
                            if (attrCls)
                                pca = _e.BuildAttrTable(_sb, $"{prefix}_{cls.CppName}_{rows.Count}_p{i}", m.Module,
                                    m.Module.Reader.GetParameter(pph).GetCustomAttributes());
                            pAttrs = (int)m.Module.Reader.GetParameter(pph).Attributes;
                        }
                        prows.Add($"{{ {ptInfo}, {nm}, {pca.Expr}, {pca.Count}, 0x{pAttrs:X} }}");
                    }
                    // Intern byte-identical parameter tables across the whole
                    // emission (first definition wins; explicit `extern` because
                    // a namespace-scope const array defaults to internal
                    // linkage). Rows that reference a per-member attr table
                    // embed that unique symbol in the initializer text, so they
                    // never collide and simply get a pool entry of their own.
                    string init = string.Join(", ", prows);
                    if (_parmPools.TryGetValue(init, out var pooled))
                    {
                        paramsExpr = pooled;
                    }
                    else
                    {
                        string ptName = $"parmpool_{_parmPoolSeq++}";
                        _sb.AppendLine($"extern const Dn2CppParamInfo {ptName}[] = {{ {init} }};");
                        _o.Header.AppendLine($"extern const Dn2CppParamInfo {ptName}[];");
                        _parmPools[init] = ptName;
                        paramsExpr = ptName;
                    }
                }
                // fnPtr + invoker: the emitted member's function pointer for Invoke
                // and its signature-deduplicated invoker thunk; both null when
                // the body was not reached/emitted (same guard as the ToString slot).
                // A skipped method with a synthesized engine-call body (hot-update
                // base) counts as emitted: the wrapper carries the method's normal
                // name and signature, so the standard wiring applies.
                string fnPtr = "nullptr";
                string invoker = "nullptr";
                // Hot-update base build: a placeholder-bodied intrinsic surface
                // (the backend's call intrinsics own every call site; the emitted
                // body is dead `return default` code) is excluded from the wiring,
                // so a patch import fails at load as a standard unresolved import
                // instead of silently binding the placeholder. Gated on
                // --hotupdate-base so normal builds stay byte-identical.
                if (_c.Reachable.Contains(m)
                    && (!_e._backend.ShouldSkipMethodBody(m.DeclaringClass, m) || _e._syntheticBodies.Contains(m))
                    && !(_e._hotUpdateBase && _e._backend.HasPlaceholderBody(m.DeclaringClass, m)))
                {
                    fnPtr = $"(void*)&{m.Emittable.CppName}";
                    // The blocker can never fire here — a reached method's emitted body
                    // names the same CppTypes renderings in its own prototype, so they
                    // are declared (see InvokerThunkBlocker) — but the invariant "an
                    // invoker thunk is emitted only when its C++ compiles; otherwise
                    // the row's invoker traps" is asked at every mouth, not assumed.
                    invoker = "(void*)&" + (InvokerThunkBlocker(m, DeclaredStructs) is { } blocked
                        ? InvokerMissStub(cls, m, blocked)
                        : _e.EmitInvokerThunk(_sb, m, _invokerThunks));
                }
                // An interface method carries no body (fnPtr stays null), but two consumers
                // dispatch it late-bound by resolving the receiver's slot to a concrete fn
                // and calling that through the interface method's invoker thunk — the ABI
                // matches, because generated callvirts call slot functions through exactly
                // the interface shape the thunk spells. So emit that thunk (signature-only)
                // when its ABI is bridgeable: for every instance row, serving
                // MethodBase.Invoke / PropertyInfo.GetValue on an interface-declared member;
                // and for static rows too under --hotupdate-base, whose interpreter binds by
                // walking these tables. A static row's thunk is useless to Invoke (no
                // receiver to resolve), so normal builds skip it.
                else if (cls.IsInterface && (!m.IsStatic || _e._hotUpdateBase))
                {
                    try
                    {
                        // A signature-only row is kept whatever reachability says, so —
                        // unlike the reached arm above — its signature can name a by-value
                        // struct NOTHING in the image ever laid out (an uninstantiated
                        // specialization, an intrinsic-represented value type such as
                        // Span<char>), and a thunk over it is C++ that does not compile.
                        // Such a row gets the trapping stub instead: it stays bindable by
                        // name, and the actual invoke raises the catchable diagnosis.
                        invoker = "(void*)&" + (InvokerThunkBlocker(m, DeclaredStructs) is { } blocked
                            ? InvokerMissStub(cls, m, blocked)
                            : _e.EmitInvokerThunk(_sb, m, _invokerThunks));
                    }
                    // InstantiationBoundException IS a NotSupportedException, so the type
                    // alone does not filter out the must-escape set (Compilation.IsMustEscape).
                    catch (NotSupportedException e) when (!Compilation.IsMustEscape(e))
                    {
                        // A signature CppTypes cannot render stays un-invokable.
                    }
                }
                string retInfo = _e.MemberTypeInfoExpr(m.Signature.ReturnType, _emittedEnums);
                // sigShape: the method-import overload discriminator, baked only in
                // a --hotupdate-base build (the hot-update loader is the sole reader;
                // normal builds keep it null). It lets the loader tell same-(name,
                // arity, static) methods apart — chiefly a generic method's several
                // instantiations, all emitted under one name.
                string sigShape = _e._hotUpdateBase ? $"\"{m.SigShape}\"" : "nullptr";
                // Trailing raw ECMA words + token: MethodAttributes, MethodImplAttributes,
                // and the method's metadata token (MemberInfo.MetadataToken).
                int mdToken = m.Handle.IsNil ? 0 : System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(m.Handle);
                // Closed generic-method instantiation: bake the row's type arguments
                // so MakeGenericMethod resolves in-image (rows sharing the definition
                // token match argument-wise) and GetGenericArguments answers exactly.
                string genArgsExpr = "nullptr";
                if (m.Context.MethodArgs.Length > 0)
                {
                    var gargs = new List<string>(m.Context.MethodArgs.Length);
                    foreach (var ga in m.Context.MethodArgs)
                        gargs.Add(_e.MemberTypeInfoExpr(ga, _emittedEnums));
                    string gaName = $"mgargs_{cls.CppName}_{m.CppName}";
                    _sb.AppendLine($"static const Dn2CppTypeInfo* const {gaName}[] = {{ {string.Join(", ", gargs)} }};");
                    genArgsExpr = gaName;
                }
                rows.Add($"{{ \"{m.Name}\", {_e.TypeInfoRef(cls, "method/ctor row's declaring type")}, {retInfo}, {paramsExpr}, {ps.Length}, {attrs}, {m.VtableSlot}, {fnPtr}, {invoker}, {mca.Expr}, {mca.Count}, {sigShape}, 0x{(int)m.Attributes:X}, 0x{(int)m.ImplAttributes:X}, {mdToken}, {m.Context.MethodArgs.Length}, {genArgsExpr} }}");
            }
            // The trim can empty a table the member list did not. A zero-length array is
            // ill-formed, and no _memberAddr entry survives to point into it, so report
            // "no table" — the ti_ then carries nullptr/0, as for a type with no members.
            if (rows.Count == 0)
                return null;
            _sb.AppendLine($"static const Dn2CppMethodInfo {tab}[] = {{ {string.Join(", ", rows)} }};");
            return (tab, rows.Count);
        }

        // Builds the Dn2CppPropInfo[] property table for a type by reading its
        // PropertyDefs from metadata and pointing each accessor at its method-table
        // entry. `methods` is the type's deduped method list (whose addresses
        // are recorded in _memberAddr). Returns null when the type has no properties.
        // (Row condition + property-type decode mirrored by
        // NoteReflectedMemberArrayElements — keep the two in step.)
        private (string Expr, int Count)? BuildPropTable(ClassInfo cls, List<MethodInfo> methods)
        {
            var byHandle = new Dictionary<MethodDefinitionHandle, MethodInfo>();
            foreach (var m in methods)
                byHandle.TryAdd(m.Handle, m);
            var rows = new List<string>();
            try
            {
                var reader = cls.Module.Reader;
                var td = reader.GetTypeDefinition(cls.Handle);
                foreach (var ph in td.GetProperties())
                {
                    var pd = reader.GetPropertyDefinition(ph);
                    var acc = pd.GetAccessors();
                    MethodInfo? getter = null, setter = null;
                    if (!acc.Getter.IsNil)
                        byHandle.TryGetValue(acc.Getter, out getter);
                    if (!acc.Setter.IsNil)
                        byHandle.TryGetValue(acc.Setter, out setter);
                    if (getter is null && setter is null)
                        continue;
                    string name = reader.GetString(pd.Name);
                    var psig = pd.DecodeSignature(_c.SigProvider, cls.Context);
                    string propType = _e.MemberTypeInfoExpr(psig.ReturnType, _emittedEnums);
                    string getterExpr = getter is not null && _memberAddr.TryGetValue(getter, out var ga) ? ga : "nullptr";
                    string setterExpr = setter is not null && _memberAddr.TryGetValue(setter, out var sa) ? sa : "nullptr";
                    // Accessibility for BindingFlags: public if either accessor is public;
                    // static if the accessors are static (an instance/static property is
                    // uniform across its accessors).
                    var anyAcc = getter ?? setter!;
                    bool pub = (getter?.IsPublic ?? false) || (setter?.IsPublic ?? false);
                    var bits = new List<string>();
                    if (anyAcc.IsStatic) bits.Add("DN2CPP_MTHA_STATIC");
                    if (pub) bits.Add("DN2CPP_MTHA_PUBLIC");
                    else bits.Add("DN2CPP_MTHA_PRIVATE");
                    string attrs = string.Join(" | ", bits);
                    // Property custom attributes: user-module (app + referenced library)
                    // non-opaque classes.
                    (string Expr, int Count) pca = ("nullptr", 0);
                    if (_c.IsUserModule(cls.Module) && !_e.IsOpaque(cls))
                        pca = _e.BuildAttrTable(_sb, $"{cls.CppName}_prp{rows.Count}", cls.Module, pd.GetCustomAttributes());
                    int prpToken = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(ph);
                    rows.Add($"{{ \"{name}\", {_e.TypeInfoRef(cls, "property row's declaring type")}, {propType}, {getterExpr}, {setterExpr}, {attrs}, {pca.Expr}, {pca.Count}, {prpToken} }}");
                }
            }
            catch (Exception e) when (!Compilation.IsMustEscape(e))
            {
                // A metadata shape we can't decode just yields no properties.
            }
            if (rows.Count == 0)
                return null;
            string tab = $"proptab_{cls.CppName}";
            _sb.AppendLine($"static const Dn2CppPropInfo {tab}[] = {{ {string.Join(", ", rows)} }};");
            return (tab, rows.Count);
        }

        // Per-type reflection member tables: one Dn2CppMethodInfo[] of
        // non-ctor methods (methtab_) and one of instance constructors (ctortab_) per
        // type, plus a Dn2CppParamInfo[] per member that has parameters, and one invoker
        // thunk per distinct C++ ABI shape. Emitted before the type-info
        // definition so the initializer can name them. GetMethods excludes ctors and
        // GetConstructors excludes methods, matching .NET's split.
        private void RenderMemberTables(ClassInfo cls)
        {
            // Skip opaque/intrinsic types (System.Object/ValueType/Exception/…): their
            // members are intrinsic-dispatched and their ti_ sits in user types' base
            // chains, so emitting a table here would leak Object.ToString/Equals/etc.
            // into GetMethods — which, unlike real .NET, dn2cpp does not reflect.
            if (cls.IsEnum || _e.IsOpaque(cls) || _e.IsCanonicalWorld(cls))
                return;
            // Dedupe by CppName: a class can list the same method twice (e.g. an
            // interface-impl entry shares the method's handle), which would both emit a
            // duplicate GetMethods row and collide the per-method parmtab_ array name.
            // A generic-method instantiation over canonical placeholders is not a
            // real member (no runtime Type exposes it, and its parameter table
            // would name never-emitted canonical-world type-infos) — skip it.
            var seenMethod = new HashSet<string>(System.StringComparer.Ordinal);
            var methods = cls.Methods
                .Where(m => m.Name != ".ctor" && m.Name != ".cctor"
                    && !(_c.SharedGenericsEnabled
                        && m.Context.MethodArgs.Any(Compilation.ContainsCanonPlaceholder))
                    && seenMethod.Add(m.CppName))
                .ToList();
            // Constructors: instance .ctor only (the static .cctor is not a reflected
            // constructor); never inherited, so no base walk at runtime.
            var seenCtor = new HashSet<string>(System.StringComparer.Ordinal);
            var ctors = cls.Methods
                .Where(m => m.Name == ".ctor" && !m.IsStatic && seenCtor.Add(m.CppName))
                .ToList();
            // --trim-reflection strips the METHOD and PROPERTY tables but keeps the
            // CONSTRUCTOR table: constructors are the one member family a reachable
            // cross-assembly caller resolves against a type chosen at RUN time (GodotSharp's
            // RuntimeTypeConversionHelper calls Type.GetConstructor on a type it picks out
            // of a Variant), i.e. exactly the undecidable case the keep-set cannot see, and
            // ctor rows are a small fraction of the relocation budget the other three tables
            // are. Constructors are never inherited, so nothing base-walks them and the kept
            // table is self-contained.
            bool keepRefl = _c.KeepsReflectionMetadata(cls);
            if (keepRefl && BuildMemberTable(cls, methods, "methtab") is { } mt)
                _methodTabs[cls] = mt;
            if (BuildMemberTable(cls, ctors, "ctortab") is { } ct)
                _ctorTabs[cls] = ct;
            // Property table: read the type's PropertyDefs from metadata, map each
            // accessor to its method-table entry (so PropertyInfo.GetValue/SetValue and
            // CanRead/CanWrite work), and emit a Dn2CppPropInfo[]. An accessor the
            // method table trimmed away has no _memberAddr entry and lands on the same
            // null-accessor path as a property that never had one. A stripped class emits no
            // property table either — and could not: Dn2CppPropInfo::getter/setter are
            // literally `&methtab_X[k]`, so the property table cannot outlive the method
            // table it points into.
            if (keepRefl && BuildPropTable(cls, methods) is { } pt)
                _propTabs[cls] = pt;
        }

        /// <summary>Renders the positional TAIL of a <c>Dn2CppTypeInfo</c> initializer from
        /// one ordered list of trailing members, each given as its 0-fill spelling and the
        /// value this type has for it (null = none). The result stops after the LAST member
        /// with a value, so a type that has none of them emits nothing and keeps a
        /// byte-identical initializer.
        ///
        /// <para>A positional initializer cannot leave a hole: spelling member <i>k</i>
        /// obliges every member before it, which is why the zero spellings are carried here
        /// rather than assumed. Hence one function rather than one <c>?:</c> per feature:
        /// independently written suffixes that each re-spell a shared member concatenate
        /// into one feature's values landing in another's slots, and that failure is silent
        /// — the C++ still compiles.</para>
        ///
        /// <para>Order the list so the RARER member is last — it is the one the 0-fill
        /// convention then elides for everybody else.</para></summary>
        private static string TypeInfoTail(params (string Zero, string? Value)[] members)
        {
            int last = -1;
            for (int i = 0; i < members.Length; i++)
                if (members[i].Value is not null)
                    last = i;
            if (last < 0)
                return "";
            var sb = new StringBuilder();
            for (int i = 0; i <= last; i++)
                sb.Append(", ").Append(members[i].Value ?? members[i].Zero);
            return sb.ToString();
        }

        // The class's type-info + interned Type object: the externally-visible
        // ti_/ty_ definitions closing its metadata block, referencing the
        // block's own file-local vtable/tables/thunks and (across TUs) only
        // header-declared symbols.
        private void RenderTypeInfo(ClassInfo cls)
        {
            if (cls.IsEnum || _e.SkipsCanonicalMetadata(cls))
                return;
            // A base whose type-info the C++ runtime defines — System.Object, the exception
            // set the traps raise, System.Enum — chains to THAT handle: otherwise an object
            // the runtime allocated and one the generated code allocated are different types
            // to `is`/catch/typeof, and `typeof(MyClass).BaseType == typeof(object)` is
            // false. Such a base always chains, even from an OPAQUE class — the runtime's
            // handle is defined unconditionally, so there is nothing to be missing. An
            // opaque (referenced-only) class keeps a plain emitted base too when that base's
            // type-info is itself emitted (the opaque pass pulls base chains in), so isinst
            // / IsAssignableFrom over a typeof-only type still walks the real hierarchy.
            string baseExpr = !cls.IsValueType && cls.BaseClass is { } bc
                    && (CoreIntrinsics.RuntimeTypeInfoSymbol(bc.FullName) is not null
                        || !_e.IsOpaque(cls)
                        || (_e._emit.Contains(bc) && !bc.IsEnum && !_e.SkipsCanonicalMetadata(bc)))
                ? _e.TypeInfoRef(bc, "base type-info of an emitted class")
                // An UNLOADED BCL exception base (a corelib-less `MyEx : SystemException`):
                // no ClassInfo exists to chain through, so chain straight to the shared
                // runtime handle — the per-type one when the runtime raises that base, else
                // the root Exception handle. This is what makes `is Exception` / catch-walks
                // and the hot-update loader's type_is_exception see an exception where the
                // absent base would otherwise cut the chain at nullptr.
                : !cls.IsValueType && cls.BaseClass is null
                        && cls.ExternalBaseName is { } extBase
                        && CoreIntrinsics.IsExternalBclExceptionName(extBase)
                    ? CoreIntrinsics.RuntimeExceptionTypeInfo(extBase) ?? "&dn2cpp_exception_type"
                    : "nullptr";
            string vt = cls.IsValueType || cls.IsInterface || cls.IsDelegate || _e.IsOpaque(cls) ? "nullptr" : cls.CppVtableName;
            string itfs = _itfTables.TryGetValue(cls, out var list)
                ? $"{_itfTabSyms[cls]}, {_itfRowCounts[cls]}"
                : "nullptr, 0";
            // The overridden ToString dispatched for an instance of this type, cast
            // to the Object-receiver signature (the override accesses fields via the
            // concrete layout). Null when the type does not override ToString.
            // Only wire the override when its body is actually emitted — same
            // predicate as the method-body loop, so a reachable-but-skipped shim
            // method (e.g. a Godot engine ToString whose body is bridged) is not
            // referenced. A boxed value type's override expects the unboxed
            // payload (box + 1), so it is reached through an adjusting thunk that
            // offsets past the box header — this is what lets dn2cpp_object_tostring
            // format a boxed struct (Console.WriteLine(tuple) / string.Format / a
            // concat hole) via its own ToString rather than the type name.
            string toStr = "nullptr";
            if (Compilation.EffectiveToString(cls) is { } ts
                && _c.Reachable.Contains(ts)
                && !_e._backend.ShouldSkipMethodBody(ts.DeclaringClass, ts))
            {
                if (!cls.IsValueType)
                    toStr = $"(Dn2CppString*(*)(Dn2CppObject*))&{ts.Emittable.CppName}";
                else
                {
                    string thunk = $"tsthunk_{cls.CppName}";
                    _sb.AppendLine($"static Dn2CppString* {thunk}(Dn2CppObject* o) {{ return {ts.Emittable.CppName}(({cls.CppStructName}*)(o + 1)); }}");
                    toStr = $"&{thunk}";
                }
            }
            // GetHashCode / Equals(object) overrides wired into the type-info slots
            // so dn2cpp_object_gethashcode / _equals dispatch them. Same shape
            // as the ToString slot: a reference type's impl receives the object
            // pointer directly; a boxed value type's impl expects the unboxed payload
            // (box + 1), so it is reached through an adjusting thunk. The override's
            // Equals(object) takes the boxed `other` as-is (it isinst-checks/unboxes
            // internally), so only the receiver needs adjusting.
            //
            // A value type that overrides NEITHER must still not leave the slots null: null
            // means "reference equality / identity hash", which for two boxes of the same
            // value is false and two different numbers — silently. The minted field walk
            // (Compilation.SynthesizedValueEquals) fills the slot when reachability wired
            // one, which it does for a struct boxed in a program that compares objects at
            // all, so nothing else pays.
            string getHash = "nullptr";
            if ((Compilation.EffectiveGetHashCode(cls) ?? _c.ReachedSynthesizedValueHash(cls)) is { } gh
                && _c.Reachable.Contains(gh)
                && !_e._backend.ShouldSkipMethodBody(gh.DeclaringClass, gh))
            {
                if (!cls.IsValueType)
                    getHash = $"(int32_t(*)(Dn2CppObject*))&{gh.Emittable.CppName}";
                else
                {
                    // Both shapes take the unboxed payload, so one thunk serves the real
                    // override and the minted walk alike.
                    string thunk = $"ghthunk_{cls.CppName}";
                    _sb.AppendLine($"static int32_t {thunk}(Dn2CppObject* o) {{ return {gh.Emittable.CppName}(({cls.CppStructName}*)(o + 1)); }}");
                    getHash = $"&{thunk}";
                }
            }
            string equals = "nullptr";
            var synEq = _c.ReachedSynthesizedValueEquals(cls);
            if ((Compilation.EffectiveEquals(cls) ?? synEq) is { } eq
                && _c.Reachable.Contains(eq)
                && !_e._backend.ShouldSkipMethodBody(eq.DeclaringClass, eq))
            {
                if (!cls.IsValueType)
                    equals = $"(int32_t(*)(Dn2CppObject*,Dn2CppObject*))&{eq.Emittable.CppName}";
                else
                {
                    string thunk = $"eqthunk_{cls.CppName}";
                    // The minted walk is TYPED (its callers all hold two T's; boxing them to
                    // re-enter an object-taking signature would allocate on every probe), so
                    // the boxed entry is where the type check and the unbox live — the two
                    // lines a record's Equals(object) is. `b->type == a->type` rather than a
                    // baked &ti_: identical here (the receiver IS this class), and it keeps
                    // the thunk independent of which type-info symbol it sits next to.
                    _sb.AppendLine(ReferenceEquals(eq, synEq)
                        ? $"static int32_t {thunk}(Dn2CppObject* a, Dn2CppObject* b) {{ return (b && b->type == a->type) ? {eq.Emittable.CppName}(({cls.CppStructName}*)(a + 1), *({cls.CppStructName}*)(b + 1)) : 0; }}"
                        : $"static int32_t {thunk}(Dn2CppObject* a, Dn2CppObject* b) {{ return {eq.Emittable.CppName}(({cls.CppStructName}*)(a + 1), b); }}");
                    equals = $"&{thunk}";
                }
            }
            // The overridden Finalize() dispatched for an instance of this type
            // (what a C# ~T() destructor compiles down to), or null when the type
            // does not override Object.Finalize. Read by MethodCompiler.Newobj.cs
            // to decide whether allocating this type must call
            // dn2cpp_register_finalizer.
            // Finalizers are class-only in C# (no ~T() on a struct), so unlike
            // ToString/GetHashCode/Equals there is no boxed-value-type thunk case.
            string finalize = "nullptr";
            if (Compilation.EffectiveFinalize(cls) is { } fin
                && _c.Reachable.Contains(fin)
                && !_e._backend.ShouldSkipMethodBody(fin.DeclaringClass, fin))
            {
                finalize = $"(void(*)(Dn2CppObject*))&{fin.Emittable.CppName}";
            }
            // Boolean Type properties packed into flags so the dn2cpp_type_is_*
            // helpers answer for any instance (typeof or GetType()), not just static
            // folds. Enums are handled in their own loop above; here a class
            // marks ValueType/Interface/Abstract. In .NET an interface is also
            // abstract (typeof(IFoo).IsAbstract == true), so set both bits for one.
            var flagBits = new List<string>();
            if (cls.IsValueType)
                flagBits.Add("DN2CPP_TF_VALUETYPE");
            if (cls.IsInterface)
            {
                flagBits.Add("DN2CPP_TF_INTERFACE");
                flagBits.Add("DN2CPP_TF_ABSTRACT");
            }
            else if (cls.IsAbstract)
                flagBits.Add("DN2CPP_TF_ABSTRACT");
            // IsSealed = the metadata Sealed bit: value types, enums, delegates, sealed
            // classes and static (abstract+sealed) classes carry it; interfaces and
            // open/abstract classes do not. (Enums are emitted in their own loop above.)
            if (cls.IsSealed)
                flagBits.Add("DN2CPP_TF_SEALED");
            // IsByRefLike = ref struct (Span<T> and user ref structs).
            if (cls.IsByRefLike)
                flagBits.Add("DN2CPP_TF_BYREFLIKE");
            // IsNested = declared inside another type (metadata DeclaringType).
            if (IsNestedType(cls))
                flagBits.Add("DN2CPP_TF_NESTED");
            // Delegates: lets dn2cpp_object_gethashcode/_equals recognize a
            // delegate instance and dispatch the chain-aware delegate hash/
            // equality (delegates never wire the gethashcode/equals slots — see
            // the delegate carve-out in Compilation's override reach).
            if (cls.IsDelegate)
                flagBits.Add("DN2CPP_TF_DELEGATE");
            // --trim-reflection removed this type's field/method/property tables. The bit is
            // what keeps the strip HONEST: a stripped type and a genuinely member-less one
            // are indistinguishable from the tables alone (nullptr, 0), so without it
            // GetMethods() would answer an empty array instead of throwing. Constructors are
            // kept, so the bit does not speak for GetConstructor/Activator.
            if (!_c.KeepsReflectionMetadata(cls))
                flagBits.Add("DN2CPP_TF_METADATA_STRIPPED");
            // Abstract System.Array base. Array type-infos (ti_arr_<T> and the runtime's
            // built-in / dynamically-built handles) carry base=nullptr, so `(array) is
            // Array` / castclass reaches nothing on the base chain; this bit lets
            // dn2cpp_isinst recognize an abstract-Array target the way it recognizes
            // System.Object through the shared runtime handle.
            if (cls.FullName == "System.Array")
                flagBits.Add("DN2CPP_TF_SYSTEM_ARRAY");
            // The six NON-generic interfaces every array implements. Stamping the bit lets
            // dn2cpp_isinst answer `(array) is IList` / `is ICloneable` from the array source
            // + this target alone, independent of whether the lazy SZArray dispatch map was
            // wired — an un-enumerated array's interface table is empty, so the test would
            // silently answer False. The map still drives the actual dispatch when a visible
            // call site wires it.
            if (cls.FullName is "System.Collections.IEnumerable"
                or "System.Collections.ICollection"
                or "System.Collections.IList"
                or "System.ICloneable"
                or "System.Collections.IStructuralComparable"
                or "System.Collections.IStructuralEquatable")
                flagBits.Add("DN2CPP_TF_ARRAY_ITF");
            // The SZArrayEnumerable<object> wrapper the shared reference-element array
            // fallback table forwards into: its closed-generic interface rows
            // are element-erased (reference elements share one C++ layout), so the
            // runtime's dispatch walk may serve any all-reference-argument
            // instantiation of the same definition from them — the enumerator a
            // fallback GetEnumerator hands back is dispatched at the caller's element
            // typing (IEnumerator<Attribute>). Dispatch only; type tests never read it.
            if (_c.IsRefErasedItfWrapper(cls))
                flagBits.Add("DN2CPP_TF_REF_ERASED_ITF");
            // An opaque value-type shell whose CLR layout extent the model could not
            // compute: the shell stayed empty, so the instanceSize about to be stamped
            // from sizeof() reads 1 — the same number a genuinely field-less struct
            // stamps. The bit is what separates the two, so the size readers can throw
            // naming the type instead of handing managed code a 1 (see
            // CppEmitter._layoutUnknown, which the shell-pad site populates).
            if (_e.HasUnknownLayoutExtent(cls))
                flagBits.Add("DN2CPP_TF_LAYOUT_UNKNOWN");
            // The marshalling verdict, asked of EVERY emitted class, reference types
            // included: the runtime's "not a value type" arm refuses them, and for a class
            // carrying an explicit [StructLayout(Sequential)] that refusal would be a false
            // claim about .NET, which measures one. The classifier answers INEXACT for
            // exactly those, so the runtime raises the declared PlatformNotSupportedException
            // instead — hence the INEXACT test sits AHEAD of the value-type test in
            // dn2cpp_marshal_require_size. A closed generic and a primitive are answered
            // before any of these bits, so their stamps are inert.
            //
            // NOT_MARSHALABLE is the marshalled-layout model's verdict, because that model
            // walks the same rules .NET does and so can tell "auto-layout / a field with no
            // unmanaged form" from "a shape dn2cpp declines to place". MARSHAL_INEXACT has
            // its own source and a different meaning — "instanceSize is not the marshalled
            // size", what the COPY paths need — so a struct with a bool field is both: it
            // carries a marshalled size AND is inexact, i.e. Marshal.SizeOf answers 4 while
            // PtrToStructure still refuses.
            switch (_c.TopLevelMarshalExtent(cls).Verdict)
            {
                case MarshalSizeVerdict.Refused:
                    flagBits.Add("DN2CPP_TF_NOT_MARSHALABLE");
                    break;
            }
            if (_c.MarshalVerdictOf(cls) != MarshalVerdict.Exact)
                flagBits.Add("DN2CPP_TF_MARSHAL_INEXACT");
            // A canonical-world type-info (shared generics): not a CLR type, and in practice
            // always a canonical INTERFACE, since SkipsCanonicalMetadata keeps every other
            // canonical class's type-info from emitting at all. It is reachable from managed
            // code through exactly one door — the canonical ALIAS rows the interface tables
            // carry over the real rows' slots — and the bit is what lets the readers that
            // hand a row OUTWARD as a managed Type (Type.GetInterfaces / FindInterfaces)
            // drop it while the dispatch and type-test walks keep it.
            if (_e.IsCanonicalWorld(cls))
                flagBits.Add("DN2CPP_TF_SHARED_CANON");
            string flags = flagBits.Count == 0 ? "0" : string.Join(" | ", flagBits);
            var (fieldsExpr, fieldCount) = _fieldTabs.TryGetValue(cls, out var ftv) ? (ftv.Expr, ftv.Count) : ("nullptr", 0);
            var (methodsExpr, methodCount) = _methodTabs.TryGetValue(cls, out var mtv) ? (mtv.Expr, mtv.Count) : ("nullptr", 0);
            var (ctorsExpr, ctorCount) = _ctorTabs.TryGetValue(cls, out var ctv) ? (ctv.Expr, ctv.Count) : ("nullptr", 0);
            var (propsExpr, propCount) = _propTabs.TryGetValue(cls, out var ptv) ? (ptv.Expr, ptv.Count) : ("nullptr", 0);
            // Type-level custom attributes: user-module (app + referenced library)
            // non-opaque classes, so an attribute on a library-declared class is emitted.
            (string Expr, int Count) typeAttrs = ("nullptr", 0);
            if (_c.IsUserModule(cls.Module) && !_e.IsOpaque(cls))
                try
                {
                    var td = cls.Module.Reader.GetTypeDefinition(cls.Handle);
                    typeAttrs = _e.BuildAttrTable(_sb, cls.CppName, cls.Module, td.GetCustomAttributes());
                }
                catch (Exception e) when (!Compilation.IsMustEscape(e))
                { /* no decodable type metadata -> no attributes */ }
            // Generic reflection: a closed instantiation points at its shared
            // open-definition handle and lists its closed type arguments. Non-generic
            // types leave these 0 (trailing-member convention). (Gate mirrored by
            // NoteReflectedMemberArrayElements for SZArray type arguments.)
            string genDefExpr = "nullptr", genArgsExpr = "nullptr";
            int genArgCount = 0;
            if (!_e.IsCanonicalWorld(cls)
                && _e.GenericDefInfo(cls) is { } gdi && _e._genericDefSyms.TryGetValue(gdi.DefName, out var defSym))
            {
                genDefExpr = "&" + defSym;
                genArgsExpr = GenericArgVector(cls);
                genArgCount = cls.Context.TypeArgs.Length;
            }
            // Nested-type table: the type's public nested types. The enum metadata
            // members (24-26) are 0 for a class — spelled out so the nested fields land in
            // the right positional slots.
            var nested = _e.PublicNestedTypes(cls, _emittedEnums);
            string nestedExpr = "nullptr";
            if (nested.Count > 0)
            {
                string arr = "nested_" + cls.CppName;
                _sb.AppendLine($"static const Dn2CppTypeInfo* const {arr}[] = {{ {string.Join(", ", nested.Select(n => _e.TypeInfoRef(n, "nested-type table row")))} }};");
                nestedExpr = arr;
            }
            // Trailing rgctx member: the grouped instantiation's slot table
            // (see the rgctx tables above); every other type leaves it null.
            string rgctxExpr = _rgctxTableSyms.Contains(cls) ? $"rgctx_{cls.CppName}" : "nullptr";
            // The members after typeObject are formatspec (never wired for an
            // emitted class), the raw ECMA TypeAttributes word + metadata token,
            // and the [DefaultMember]-declared member name (GetDefaultMembers).
            var (tIlAttrs, tToken) = TypeIlMeta(cls);
            string dmName = DefaultMemberName(cls) is { } dm ? $"\"{dm}\"" : "nullptr";
            // The members AFTER defaultMemberName, in declaration order, built in one place
            // (TypeInfoTail) rather than as one suffix per feature — see that method for why
            // per-feature suffixes silently overwrite one another's slots.
            //
            // varianceMask is 0 for every CLASS whatever it declares: variance rides the
            // open-DEFINITION handle (see the gendef emitter above), never a closed class's
            // own type-info. It is in the list because it is a positional member the tail
            // has to step over, not because this site ever has a value for it.
            string? esName = null, esGuid = null;
            if (EventSourceIdentity(cls) is { } es)
            {
                esName = $"\"{es.Name}\"";
                esGuid = es.Guid is null ? null : $"\"{es.Guid}\"";
            }
            // The stamp still pins the model's 64-bit reading; adopting the rendered
            // sizeof(void*) form is the runtime reader's side of the change.
            string? marshalSize = _c.MarshalStampSize(cls) is null
                ? null
                : _c.TopLevelMarshalExtent(cls).Size.ToString();
            string tail = TypeInfoTail(
                ("0", null),                                 // varianceMask
                ("0", marshalSize),                          // marshalSize
                ("nullptr", esName),                         // eventSourceName
                ("nullptr", esGuid));                        // eventSourceGuid
            _sb.AppendLine($"const Dn2CppTypeInfo {cls.CppTypeInfoDefName} = {{ \"{Compilation.ReflectionTypeName(cls)}\", {baseExpr}, (int32_t)sizeof({cls.CppStructName}), {vt}, {itfs}, {toStr}, {getHash}, {equals}, {flags}, {fieldsExpr}, {fieldCount}, {methodsExpr}, {methodCount}, {ctorsExpr}, {ctorCount}, {propsExpr}, {propCount}, {typeAttrs.Expr}, {typeAttrs.Count}, {genDefExpr}, {genArgsExpr}, {genArgCount}, nullptr, nullptr, 0, {nestedExpr}, {nested.Count}, nullptr, 0, {AsmLit(cls)}, {finalize}, {rgctxExpr}, &ty_{cls.CppName}, nullptr, 0x{tIlAttrs:X}u, {tToken}, {dmName}{tail} }};");
            _sb.AppendLine($"const Dn2CppType ty_{cls.CppName} = {{ {{ &dn2cpp_type_type }}, {_e.TypeInfoRef(cls, "class ty_ companion")} }};");
            // A bind-kind type whose layout/vtable/metadata this emission knows: its handle
            // is the runtime's, so the metadata just rendered has to be copied INTO that
            // handle at startup. An OPAQUE one (System.Exception, System.AggregateException
            // — intrinsic, no fields, no vtable, and a runtime object layout the emitted
            // struct does not describe) is left alone: the runtime's own stub IS its truth.
            if (CoreIntrinsics.RuntimeTypeInfoSymbol(cls.FullName) is not null && !_e.IsOpaque(cls))
                _e._typeBinds.Add(cls);
        }
    }
}
