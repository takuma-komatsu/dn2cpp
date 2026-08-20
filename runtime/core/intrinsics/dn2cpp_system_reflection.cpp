// dn2cpp_system_reflection.cpp — System.Reflection / System.Type intrinsics.
//
// The transpiler binds the reflection surface — Type/Enum queries, generic type
// reflection, Type.GetType(string), member and custom-attribute enumeration — to
// these walks over the embedded Dn2CppTypeInfo metadata (the tree-shaken
// descriptor tables the backend emits) instead of the BCL's reflection IL.
// Behavior matches real .NET for the modeled surface.
#include "dn2cpp_core.h"

#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <mutex>
#include <string>
#include <unordered_map>

// ---- generic reflection ----

// Type.IsGenericType: a closed instantiation (genericArgCount > 0) or an open
// definition (DN2CPP_TF_GENERICDEF). Type.IsGenericTypeDefinition / .NET's
// ContainsGenericParameters: only the open definition in our model (a closed
// instantiation's arguments are always concrete — open args nested inside an
// instantiation are a carve-out).
int32_t dn2cpp_type_is_generic_type(const Dn2CppTypeInfo* ti)
{
    return (ti->genericArgCount > 0 || (ti->flags & DN2CPP_TF_GENERICDEF) != 0) ? 1 : 0;
}

// Type.IsConstructedGenericType: a closed instantiation only (not the open definition).
int32_t dn2cpp_type_is_constructed_generic(const Dn2CppTypeInfo* ti)
{
    return ti->genericArgCount > 0 ? 1 : 0;
}

int32_t dn2cpp_type_is_generic_type_definition(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_GENERICDEF) != 0 ? 1 : 0;
}

int32_t dn2cpp_type_contains_generic_parameters(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_GENERICDEF) != 0 ? 1 : 0;
}

// Type.GetGenericTypeDefinition(): the open definition handle (the same one shared by
// every closed instantiation of it, so two List<T> closes' definitions are ==). An
// open definition returns itself. A non-generic type throws InvalidOperationException.
Dn2CppType* dn2cpp_type_get_generic_type_definition(Dn2CppType* t)
{
    const Dn2CppTypeInfo* ti = dn2cpp_type_require(t);
    if (ti->genericDef != nullptr)
        return dn2cpp_get_type_from_handle(ti->genericDef);
    dn2cpp_throw_invalid_operation();
}

// Type.GetGenericArguments(): the closed type arguments as a Type[] (empty for a
// non-generic type or an open definition, whose parameters we do not model).
Dn2CppArrayRef* dn2cpp_type_get_generic_arguments(Dn2CppType* t)
{
    const Dn2CppTypeInfo* ti = dn2cpp_type_require(t);
    int32_t n = ti->genericArgCount;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
        arr->data[i] = reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(ti->genericArgs[i]));
    return arr;
}

// ---- runtime-instantiation templates (the MakeGenericType synthesis fallback) ----

static const Dn2CppRuntimeTemplate* dn2cpp_runtime_template_by_def(const Dn2CppTypeInfo* def)
{
    for (int32_t i = 0; i < dn2cpp_runtime_template_count; i++)
        if (dn2cpp_runtime_templates[i].def == def)
            return &dn2cpp_runtime_templates[i];
    return nullptr;
}

static const Dn2CppRuntimeTemplate* dn2cpp_runtime_template_by_ti(const Dn2CppTypeInfo* ti)
{
    for (int32_t i = 0; i < dn2cpp_runtime_template_count; i++)
        if (dn2cpp_runtime_templates[i].templateTi == ti)
            return &dn2cpp_runtime_templates[i];
    return nullptr;
}

// Synthesized instantiations intern on (def, args): one managed type is exactly
// one Dn2CppTypeInfo* (the invariant every pointer-comparing walk rests on), so a
// second MakeGenericType with the same arguments must return the first pointer.
// Plain new, never freed — a type-info is immortal, like the emitted ones.
struct Dn2CppSynthInst
{
    const Dn2CppTypeInfo* def;
    const Dn2CppTypeInfo* ti;
    Dn2CppSynthInst* next;
};
static Dn2CppSynthInst* g_synth_insts = nullptr;
static std::mutex g_synth_mtx;

// Clones `row`'s template for the given argument vector (caller holds g_synth_mtx;
// argument count already checked against row->argCount). Recursive for the base
// chain: a template base is itself a row, and its identity argument projection
// (established by the emitter's eligibility shape bound) means it takes the same
// leading arguments.
static const Dn2CppTypeInfo* dn2cpp_synthesize_instantiation(
    const Dn2CppRuntimeTemplate* row, const Dn2CppTypeInfo* const* args)
{
    for (Dn2CppSynthInst* n = g_synth_insts; n != nullptr; n = n->next)
    {
        if (n->def != row->def)
            continue;
        bool same = true;
        for (int32_t i = 0; i < row->argCount; i++)
            if (n->ti->genericArgs[i] != args[i]) { same = false; break; }
        if (same)
            return n->ti;
    }
    Dn2CppTypeInfo* ti = new Dn2CppTypeInfo(*row->templateTi);
    ti->flags &= ~(DN2CPP_TF_SHARED_CANON | DN2CPP_TF_RUNTIME_TEMPLATE);
    auto** argv = new const Dn2CppTypeInfo*[row->argCount];
    for (int32_t i = 0; i < row->argCount; i++)
        argv[i] = args[i];
    ti->genericArgs = argv;
    ti->genericArgCount = row->argCount;
    ti->genericDef = row->def;
    ti->typeObject = nullptr;   // interned lazily by dn2cpp_get_type_from_handle_slow
    // dn2cpp_ctor_invoke_argv allocates from mi->declaringType, so a shared ctor
    // row would stamp the TEMPLATE (whose rgctx is null) onto the new instance.
    if (ti->ctorCount > 0)
    {
        auto* ctors = new Dn2CppMethodInfo[ti->ctorCount];
        for (int32_t i = 0; i < ti->ctorCount; i++)
        {
            ctors[i] = row->templateTi->ctors[i];
            ctors[i].declaringType = ti;
        }
        ti->ctors = ctors;
    }
    if (ti->base != nullptr && (ti->base->flags & DN2CPP_TF_RUNTIME_TEMPLATE) != 0)
    {
        const Dn2CppRuntimeTemplate* brow = dn2cpp_runtime_template_by_ti(ti->base);
        if (brow == nullptr || brow->argCount > row->argCount)
            dn2cpp_throw_invalid_operation();
        ti->base = dn2cpp_synthesize_instantiation(brow, args);
    }
    if (row->rgctxDescCount > 0)
    {
        auto** table = new const void*[row->rgctxDescCount];
        for (int32_t i = 0; i < row->rgctxDescCount; i++)
            table[i] = args[row->rgctxDesc[i]];
        ti->rgctx = table;
    }
    // The ToString spelling (Def`N[arg,arg]); FullName composes structurally off
    // genericDef/genericArgs and never reads this. Registered on the dynamic
    // side-chain so GetType round-trips resolve directly.
    std::string nm = row->def->name != nullptr ? row->def->name : "?";
    nm += '[';
    for (int32_t i = 0; i < row->argCount; i++)
    {
        if (i > 0)
            nm += ',';
        nm += args[i] != nullptr && args[i]->name != nullptr ? args[i]->name : "?";
    }
    nm += ']';
    char* name = new char[nm.size() + 1];
    std::memcpy(name, nm.c_str(), nm.size() + 1);
    ti->name = name;
    auto* node = new Dn2CppSynthInst{ row->def, ti, g_synth_insts };
    g_synth_insts = node;
    dn2cpp_type_registry_add(ti->name, ti);
    return ti;
}

// The synthesis fallback both resolvers (MakeGenericType and the composed-name
// resolution) share: null when the definition has no template row or the shape
// does not fit — the caller keeps its own miss behavior.
static const Dn2CppTypeInfo* dn2cpp_try_synthesize_generic(
    const Dn2CppTypeInfo* defTi, const Dn2CppTypeInfo* const* args, int32_t argc)
{
    const Dn2CppRuntimeTemplate* row = dn2cpp_runtime_template_by_def(defTi);
    if (row == nullptr || row->argCount != argc)
        return nullptr;
    for (int32_t i = 0; i < argc; i++)
        if (args[i] == nullptr)
            return nullptr;
    std::lock_guard<std::mutex> lock(g_synth_mtx);
    return dn2cpp_synthesize_instantiation(row, args);
}

// Type.MakeGenericType(args): resolve the (definition, args) instantiation against the
// program's reachability-generated closed types (scanned via the type registry). Like
// IL2CPP, an instantiation that AOT never generated is not built at runtime — it throws
// NotSupportedException, UNLESS the definition is typeof-only and the emitter shipped a
// runtime-instantiation template for it (see Dn2CppRuntimeTemplate), in which case the
// instantiation is synthesized by cloning that template. `def` may be an open definition
// or any handle whose genericDef matches (calling MakeGenericType on a closed type
// reuses its definition).
Dn2CppType* dn2cpp_type_make_generic(Dn2CppType* def, Dn2CppArrayRef* args)
{
    const Dn2CppTypeInfo* defTi = dn2cpp_type_require(def);
    // Normalize to the definition handle: a closed type's genericDef, else itself.
    if (defTi->genericDef != nullptr && (defTi->flags & DN2CPP_TF_GENERICDEF) == 0)
        defTi = defTi->genericDef;
    int32_t argc = args == nullptr ? 0 : args->length;
    for (int32_t k = 0; k < dn2cpp_type_registry_count; k++)
    {
        const Dn2CppTypeInfo* cand = dn2cpp_type_registry[k].type;
        if (cand->genericDef != defTi || (cand->flags & DN2CPP_TF_GENERICDEF) != 0)
            continue;
        if (cand->genericArgCount != argc)
            continue;
        bool match = true;
        for (int32_t i = 0; i < argc; i++)
        {
            auto* a = reinterpret_cast<Dn2CppType*>(args->data[i]);
            if (a == nullptr || a->typeInfo != cand->genericArgs[i]) { match = false; break; }
        }
        if (match)
            return dn2cpp_get_type_from_handle(cand);
    }
    // No AOT-generated candidate: the typeof-only template fallback, when the
    // emitter shipped one for this definition.
    {
        const Dn2CppTypeInfo* argv[32];
        if (argc <= static_cast<int32_t>(sizeof argv / sizeof argv[0]))
        {
            bool haveArgs = true;
            for (int32_t i = 0; i < argc; i++)
            {
                auto* a = reinterpret_cast<Dn2CppType*>(args->data[i]);
                if (a == nullptr || a->typeInfo == nullptr) { haveArgs = false; break; }
                argv[i] = a->typeInfo;
            }
            if (haveArgs)
                if (const Dn2CppTypeInfo* made = dn2cpp_try_synthesize_generic(defTi, argv, argc))
                    return dn2cpp_get_type_from_handle(made);
        }
    }
    // No AOT-generated candidate. The catch site (a deserializer's contract
    // resolver, typically) is far from the cause, so the message names WHICH
    // instantiation is missing and what would make it exist.
    std::string msg = "MakeGenericType: the program contains no AOT-generated instantiation ";
    msg += defTi->name != nullptr ? defTi->name : "<unnamed generic definition>";
    msg += "[";
    for (int32_t i = 0; i < argc; i++)
    {
        if (i > 0)
            msg += ",";
        auto* a = reinterpret_cast<Dn2CppType*>(args->data[i]);
        msg += (a != nullptr && a->typeInfo != nullptr && a->typeInfo->name != nullptr)
            ? a->typeInfo->name : "<null>";
    }
    msg += "]. dn2cpp, like IL2CPP, cannot construct a generic type at run time; "
           "the closed instantiation must be visible to the transpiler's static "
           "closure (e.g. declare a field, property or local of it).";
    dn2cpp_throw_not_supported_msg(msg.c_str());
}

// ---- Type/Enum reflection completion ----

// Whether an interface-table row is a shared-generics canonical ALIAS — a row carrying
// a placeholder instantiation (IEnumerable<$CnRef>) over the real row's slots, so a
// shared body's canonical-handle dispatch resolves on a real receiver. Dispatch and the
// type-test walks must see these rows; every reader that hands one OUTWARD as a managed
// Type must not, because .NET's interface list has no such member. See
// DN2CPP_TF_SHARED_CANON.
static inline bool dn2cpp_itf_row_is_canonical_alias(const Dn2CppInterfaceEntry& e)
{
    return e.itf != nullptr && (e.itf->flags & DN2CPP_TF_SHARED_CANON) != 0;
}

// The interface rows a type EXPOSES as CLR types: its own table, plus — for an array —
// the shared relation-only rows for the six non-generic interfaces every CLR array
// implements, which no per-type table can carry (see
// dn2cpp_array_set_nongeneric_interfaces in dn2cpp_core.h). All three enumerating
// readers below go through this one iterator, so they cannot disagree about what the
// interface list IS.
//
// Two exclusions: a canonical ALIAS row is a dispatch handle, not a CLR type, so it
// never reaches a managed Type; and a shared array row is dropped when the type's own
// table already carries it (an SZArray map carries IEnumerable/ICollection/IList).
// Hence the count is derived by COUNTING rather than read off interfaceCount — alias
// rows interleave with the real ones in the string and SZArray dispatch maps, so "the
// real rows are a prefix" does not hold.
namespace
{
struct Dn2CppEffectiveItfs
{
    const Dn2CppTypeInfo* ti;
    const Dn2CppInterfaceEntry* extra;
    int32_t extraCount;

    explicit Dn2CppEffectiveItfs(const Dn2CppTypeInfo* type) : ti(type), extra(nullptr), extraCount(0)
    {
        extra = dn2cpp_array_nongeneric_interfaces(type, &extraCount);
    }

    bool OwnRowCarries(const Dn2CppTypeInfo* itf) const
    {
        for (int32_t i = 0; i < ti->interfaceCount; i++)
            if (ti->interfaces[i].itf == itf)
                return true;
        return false;
    }

    // Calls fn(itf) once per reported interface, in table order then extra order.
    template <typename F> void ForEach(F fn) const
    {
        for (int32_t i = 0; i < ti->interfaceCount; i++)
            if (!dn2cpp_itf_row_is_canonical_alias(ti->interfaces[i]) && ti->interfaces[i].itf != nullptr)
                fn(ti->interfaces[i].itf);
        for (int32_t i = 0; i < extraCount; i++)
            if (extra[i].itf != nullptr && !OwnRowCarries(extra[i].itf))
                fn(extra[i].itf);
    }

    int32_t Count() const
    {
        int32_t n = 0;
        ForEach([&n](const Dn2CppTypeInfo*) { n++; });
        return n;
    }
};
}

// Type.GetInterfaces(): the interfaces recorded in the type's interface table, wrapped as
// Type[]. The table holds the transitive set built at emit time — for interfaces, abstract
// classes and never-boxed value types it is a relation-only table (nullptr slots), emitted
// precisely so this and IsAssignableFrom answer — plus the shared array rows above.
Dn2CppArrayRef* dn2cpp_type_get_interfaces(Dn2CppType* t)
{
    const Dn2CppTypeInfo* ti = dn2cpp_type_require(t);
    Dn2CppEffectiveItfs eff(ti);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(eff.Count());
    int32_t k = 0;
    eff.ForEach([&](const Dn2CppTypeInfo* itf) {
        arr->data[k++] = reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(itf));
    });
    return arr;
}

// Type.GetInterface(name[, ignoreCase]): the lone interface-table row whose name
// matches, over exactly the row set GetInterfaces reports (the canonical alias rows
// are skipped, and ambiguity is counted post-exclusion). Matching pins real .NET:
// the input splits at its LAST '.'; the simple-name part
// matches the interface's SIMPLE name (its generic definition's for a closed generic,
// so "IEnumerable`1" names every instantiation, as Type.Name does); a namespace part,
// when present, must equal the interface's namespace prefix CASE-SENSITIVELY even
// under ignoreCase, which folds the simple-name part only. Two matching rows — two
// instantiations of one generic interface, or one simple name in two namespaces —
// throw AmbiguousMatchException whether or not the input was qualified. A null name
// throws ArgumentNullException; no match (including a partial namespace, a trailing
// '*', or the empty string) returns null, never throws. Known divergence: a NESTED
// interface's namespace prefix here is "Ns.Outer" (the CLR name up to the '+'),
// where .NET splits Namespace="Ns" from Name="IInner" — the unqualified simple name
// matches identically either way.
Dn2CppType* dn2cpp_type_get_interface(Dn2CppType* t, Dn2CppString* name, int32_t ignoreCase)
{
    const Dn2CppTypeInfo* ti = dn2cpp_type_require(t);
    if (name == nullptr)
        dn2cpp_throw_argument_null();
    int32_t split = -1; // index of the last '.'; the simple part is [split+1, length)
    for (int32_t i = 0; i < name->length; i++)
        if (name->chars[i] == u'.')
            split = i;
    auto fold = [ignoreCase](char16_t c) {
        return (ignoreCase && c >= u'A' && c <= u'Z') ? (char16_t)(c + 32) : c;
    };
    const Dn2CppTypeInfo* found = nullptr;
    // Over exactly the row set GetInterfaces reports, shared array rows included —
    // the two must not disagree about what the interface list is.
    Dn2CppEffectiveItfs(ti).ForEach([&](const Dn2CppTypeInfo* itf) {
        if (itf->name == nullptr)
            return;
        // A closed generic matches by its DEFINITION's CLR name ("IEnumerable`1"),
        // exactly what dn2cpp_type_name reports for it.
        const Dn2CppTypeInfo* nameSrc =
            (itf->genericArgCount > 0 && itf->genericDef != nullptr) ? itf->genericDef : itf;
        const char* full = nameSrc->name;
        const char* simple = dn2cpp_simple_type_name(full);
        // Simple-name part, folded under ignoreCase.
        int32_t j = split + 1, k = 0;
        for (; j < name->length; j++, k++)
            if (simple[k] == '\0'
                || fold(static_cast<char16_t>(static_cast<unsigned char>(simple[k])))
                    != fold(name->chars[j]))
                break;
        if (j != name->length || simple[k] != '\0')
            return;
        // Namespace part, when the input carries one: case-sensitive whatever
        // ignoreCase says (pinned against real .NET).
        if (split >= 0)
        {
            int32_t prefix = static_cast<int32_t>(simple - full); // includes the separator
            if (prefix - 1 != split || full[prefix - 1] != '.')
                return;
            int32_t p = 0;
            for (; p < split; p++)
                if (static_cast<char16_t>(static_cast<unsigned char>(full[p])) != name->chars[p])
                    break;
            if (p != split)
                return;
        }
        if (found != nullptr)
            dn2cpp_throw_ambiguous_match();
        found = itf;
    });
    return (found != nullptr) ? dn2cpp_get_type_from_handle(found) : nullptr;
}

// Assembly.GetTypes(): every type-registry entry defined in the named assembly
// (the Assembly handle is its simple name; a null assemblyName on a type-info —
// the hand-written CoreLib handles — counts as System.Private.CoreLib). The
// registry holds the program's reachable user types/enums plus the well-known
// handles, so like IL2CPP this reports the AOT-preserved subset, not full metadata.
//
// The `<Module>` pseudo-type is excluded, matching .NET: an assembly that carries a
// [ModuleInitializer] has a real `<Module>` type (it holds the .cctor that calls the
// initializer) and so a real type-info and registry entry, but GetTypes() does not
// report it. Type.GetType("<Module>") still resolves it — .NET's does too — so the
// exclusion belongs here, on the enumerating surface, and not in the registry.
Dn2CppArrayRef* dn2cpp_assembly_get_types(const char* asmName)
{
    if (asmName == nullptr)
        asmName = "System.Private.CoreLib";
    auto owned = [asmName](int32_t k) {
        if (std::strcmp(dn2cpp_type_registry[k].name, "<Module>") == 0)
            return false;
        const char* owner = dn2cpp_type_registry[k].type->assemblyName;
        if (owner == nullptr)
            owner = "System.Private.CoreLib";
        return std::strcmp(owner, asmName) == 0;
    };
    int32_t n = 0;
    for (int32_t k = 0; k < dn2cpp_type_registry_count; k++)
        if (owned(k))
            n++;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    int32_t i = 0;
    for (int32_t k = 0; k < dn2cpp_type_registry_count; k++)
        if (owned(k))
            arr->data[i++] = reinterpret_cast<Dn2CppObject*>(
                dn2cpp_get_type_from_handle(dn2cpp_type_registry[k].type));
    return arr;
}

// RuntimeHelpers.Box(ref byte, RuntimeTypeHandle): box the payload at `value` under the
// handle's own type, at THAT type's payload width. The width must be read here because
// the handle is a run-time value — a fixed sizeof(int32_t) at the call site truncates a
// long/ulong-underlying enum.
Dn2CppObject* dn2cpp_box_by_handle(const Dn2CppTypeInfo* ti, const void* value)
{
    if (ti == nullptr)
        dn2cpp_throw_argument_null();
    if (value == nullptr)
        dn2cpp_throw_null_reference();  // a null `ref byte` — .NET faults, it does not box
    if ((ti->flags & DN2CPP_TF_VALUETYPE) == 0)
        dn2cpp_throw_argument();
    dn2cpp_require_layout(ti);
    return dn2cpp_box(ti, value, static_cast<size_t>(ti->instanceSize > 0 ? ti->instanceSize : 0));
}

// RuntimeHelpers.GetUninitializedObject(Type): a zeroed instance with the type
// header planted and no constructor run. Mirrors newobj's allocation (including
// finalizer registration) — GodotSharp's script-instance bridge constructs the
// object this way and invokes the constructor separately.
Dn2CppObject* dn2cpp_get_uninitialized_object(Dn2CppType* t)
{
    if (t == nullptr)
        dn2cpp_throw_argument_null();
    const Dn2CppTypeInfo* ti = t->typeInfo;
    // An exception type floors at the exception prefix, not the bare header: an
    // opaque one (System.Exception itself) reports no instanceSize, yet throwing
    // the instance stamps the trace slot and a catch reads the prefix fields.
    size_t floor = dn2cpp_type_is_exception(ti)
        ? sizeof(Dn2CppExceptionObject) : sizeof(Dn2CppObject);
    // A value type's instanceSize is its UNBOXED payload, so the box is header + payload
    // (dn2cpp_activator_create_instance_nonpublic sizes the same allocation the same way).
    // Read as a whole-object extent it floors at the bare header for every value type
    // 8 bytes or smaller — every enum included — and the payload reads run off the end.
    size_t want = (ti->flags & DN2CPP_TF_VALUETYPE) != 0
        ? sizeof(Dn2CppObject) + static_cast<size_t>(ti->instanceSize > 0 ? ti->instanceSize : 0)
        : static_cast<size_t>(ti->instanceSize > 0 ? ti->instanceSize : 0);
    size_t sz = want > floor ? want : floor;
    auto* o = static_cast<Dn2CppObject*>(dn2cpp_alloc(sz));
    o->type = ti;
    if (ti->finalize != nullptr)
        dn2cpp_register_finalizer(o);
    return o;
}

// True when an enum's boxed payload is 8 bytes — a long/ulong underlying, whose values
// do not fit the int32 model every narrower enum rides (CppTypes.Of).
static bool dn2cpp_enum_is_wide(const Dn2CppTypeInfo* ti)
{
    const Dn2CppTypeInfo* u = ti->enumUnderlying;
    return u == &dn2cpp_int64_type || u == &dn2cpp_uint64_type;
}

// The enum's underlying width (bytes) and signedness. The box carries the model
// width — int32 for every sub-64-bit underlying, int64 for Int64/UInt64 (CppTypes.Of);
// byteWidth here is the CLR underlying width: what the formatter masks and pads to,
// and the range Enum.Parse's numeric token has to fit.
static void enum_underlying_shape(const Dn2CppTypeInfo* ti, int32_t* byteWidth, bool* isUnsigned)
{
    const Dn2CppTypeInfo* u = ti->enumUnderlying;
    if (u == &dn2cpp_byte_type)        { *byteWidth = 1; *isUnsigned = true;  }
    else if (u == &dn2cpp_sbyte_type)  { *byteWidth = 1; *isUnsigned = false; }
    else if (u == &dn2cpp_int16_type)  { *byteWidth = 2; *isUnsigned = false; }
    else if (u == &dn2cpp_uint16_type) { *byteWidth = 2; *isUnsigned = true;  }
    else if (u == &dn2cpp_uint32_type) { *byteWidth = 4; *isUnsigned = true;  }
    else if (u == &dn2cpp_int64_type)  { *byteWidth = 8; *isUnsigned = false; }
    else if (u == &dn2cpp_uint64_type) { *byteWidth = 8; *isUnsigned = true;  }
    else                               { *byteWidth = 4; *isUnsigned = false; } // Int32 (default)
}

// A boxed enum's payload at the enum's MODEL width. The sign/zero extension is
// already baked in by whoever boxed it, so this is the whole value.
static int64_t dn2cpp_enum_box_value(const Dn2CppTypeInfo* ti, Dn2CppObject* box)
{
    const void* p = box + 1;
    return dn2cpp_enum_is_wide(ti) ? *reinterpret_cast<const int64_t*>(p)
                                   : *reinterpret_cast<const int32_t*>(p);
}

// Box `v` under the enum's type-info at that same model width.
static Dn2CppObject* dn2cpp_enum_box(const Dn2CppTypeInfo* ti, int64_t v)
{
    if (dn2cpp_enum_is_wide(ti))
        return dn2cpp_box(ti, &v, sizeof(int64_t));
    int32_t n = static_cast<int32_t>(v);
    return dn2cpp_box(ti, &n, sizeof(int32_t));
}

// Type.GetEnumUnderlyingType() / Enum.GetUnderlyingType(Type): the enum's underlying
// primitive type (defaulting to Int32 when the metadata is absent). A non-enum throws
// ArgumentException, matching .NET.
Dn2CppType* dn2cpp_type_get_enum_underlying(Dn2CppType* t)
{
    if ((dn2cpp_type_require(t)->flags & DN2CPP_TF_ENUM) == 0)
        dn2cpp_throw_argument();
    const Dn2CppTypeInfo* u = t->typeInfo->enumUnderlying;
    return dn2cpp_get_type_from_handle(u != nullptr ? u : &dn2cpp_int32_type);
}

// Enum.GetNames(Type): the member names (in the table's pre-sorted order) as a string[].
Dn2CppArrayRef* dn2cpp_enum_get_names(Dn2CppType* t)
{
    if (t == nullptr || (t->typeInfo->flags & DN2CPP_TF_ENUM) == 0)
        dn2cpp_throw_argument();
    const Dn2CppTypeInfo* ti = t->typeInfo;
    int32_t n = ti->enumMemberCount;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&arr->data[i], reinterpret_cast<Dn2CppObject*>(
            dn2cpp_string_from_utf8(ti->enumMembers[i].name,
                static_cast<int32_t>(std::strlen(ti->enumMembers[i].name)))));
    return arr;
}

// Enum.GetValues(Type): the declared values (pre-sorted order) as a reference array of
// boxed enums (object[]) — the non-generic form returns System.Array, so a foreach over it
// (lowered to System.Array.GetEnumerator) yields boxed `object` elements. Each element is
// the int32 value boxed as the enum's own type-info, so it unboxes back to the enum and
// ToStrings to its member name (the generic Enum.GetValues<T>() -> T[] form is handled separately).
Dn2CppArrayRef* dn2cpp_enum_get_values_boxed(Dn2CppType* t)
{
    if (t == nullptr || (t->typeInfo->flags & DN2CPP_TF_ENUM) == 0)
        dn2cpp_throw_argument();
    const Dn2CppTypeInfo* ti = t->typeInfo;
    int32_t n = ti->enumMemberCount;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&arr->data[i], dn2cpp_enum_box(ti, ti->enumMembers[i].value));
    return arr;
}

// Enum.GetValuesAsUnderlyingType(Type): the declared values as an array of the UNDERLYING
// primitive (byte[]/int[]/long[]/ulong[]/...), typed with that element's precise identity.
// dn2cpp_array_create_instance allocates under the underlying's storage rep and tags it with
// the (possibly fabricated) precise `Elem[]` type-info, so GetType()/Length/GetValue all read
// the right identity. enumMembers is pre-sorted by unsigned underlying magnitude, matching
// real .NET's ordering. The int64 member values are truncated to the underlying width by a
// low-byte copy (little-endian hosts, which are the only targets).
Dn2CppObject* dn2cpp_enum_get_values_underlying(Dn2CppType* t)
{
    if (t == nullptr || (t->typeInfo->flags & DN2CPP_TF_ENUM) == 0)
        dn2cpp_throw_argument();
    const Dn2CppTypeInfo* ti = t->typeInfo;
    const Dn2CppTypeInfo* underlyingTi = ti->enumUnderlying != nullptr
        ? ti->enumUnderlying : &dn2cpp_int32_type;
    int32_t n = ti->enumMemberCount;
    Dn2CppObject* obj = dn2cpp_array_create_instance(dn2cpp_get_type_from_handle(underlyingTi), &n, 1);
    // Element storage: int32/uint32 pack into Dn2CppArrayI4 (data at ->data), every other
    // primitive width into Dn2CppArrayN (element-sized inline storage at ->data).
    bool isI4 = underlyingTi == &dn2cpp_int32_type || underlyingTi == &dn2cpp_uint32_type;
    char* base = isI4
        ? reinterpret_cast<char*>(reinterpret_cast<Dn2CppArrayI4*>(obj)->data)
        : reinterpret_cast<Dn2CppArrayN*>(obj)->data;
    int32_t width = dn2cpp_prim_storage_width(dn2cpp_prim_code(underlyingTi));
    if (width <= 0 || width > static_cast<int32_t>(sizeof(int64_t)))
        width = static_cast<int32_t>(sizeof(int32_t)); // defensive: unmodeled underlying
    for (int32_t i = 0; i < n; i++)
    {
        int64_t v = ti->enumMembers[i].value;
        std::memcpy(base + static_cast<size_t>(i) * static_cast<size_t>(width), &v,
                    static_cast<size_t>(width));
    }
    return obj;
}

// Nullable.GetUnderlyingType(Type): U for a closed Nullable<U>, else null. A
// synthesized Nullable<U> type-info carries genericDef=Nullable`1 + a single genericArg U;
// a non-Nullable (or null) type returns null, matching .NET.
Dn2CppType* dn2cpp_nullable_get_underlying(Dn2CppType* t)
{
    if (t == nullptr)
        return nullptr;
    const Dn2CppTypeInfo* ti = t->typeInfo;
    if (ti->genericArgCount == 1 && ti->genericDef != nullptr && ti->genericArgs != nullptr
        && ti->genericDef->name != nullptr
        && std::strcmp(ti->genericDef->name, "System.Nullable`1") == 0)
        return dn2cpp_get_type_from_handle(ti->genericArgs[0]);
    return nullptr;
}

// Enum.GetName(Type, value): the member name for the value, or null if undefined.
// The value arrives BOXED rather than pre-narrowed at the call site: the enum type is
// chosen at run time there, so only this side knows the payload's width.
Dn2CppString* dn2cpp_enum_get_name(Dn2CppType* t, Dn2CppObject* value)
{
    if (t == nullptr || (t->typeInfo->flags & DN2CPP_TF_ENUM) == 0)
        dn2cpp_throw_argument();
    const Dn2CppTypeInfo* ti = t->typeInfo;
    int64_t v = dn2cpp_enum_box_value(ti, value);
    for (int32_t i = 0; i < ti->enumMemberCount; i++)
        if (ti->enumMembers[i].value == v)
            return dn2cpp_string_from_utf8(ti->enumMembers[i].name,
                static_cast<int32_t>(std::strlen(ti->enumMembers[i].name)));
    return nullptr;
}

// Enum.IsDefined(Type, value): whether the value names a declared member (value form;
// the string-name form is a carve-out). Boxed for the reason GetName's is.
int32_t dn2cpp_enum_is_defined(Dn2CppType* t, Dn2CppObject* value)
{
    if (t == nullptr || (t->typeInfo->flags & DN2CPP_TF_ENUM) == 0)
        dn2cpp_throw_argument();
    const Dn2CppTypeInfo* ti = t->typeInfo;
    int64_t v = dn2cpp_enum_box_value(ti, value);
    for (int32_t i = 0; i < ti->enumMemberCount; i++)
        if (ti->enumMembers[i].value == v)
            return 1;
    return 0;
}

// Shared core for the non-generic Enum.Parse/TryParse(Type, …). Validates the TYPE
// (null / non-enum throw ArgumentException — .NET's ValidateRuntimeType does this in
// BOTH Parse and TryParse), builds the (int64 values, Dn2CppString* names) arrays the
// shared dn2cpp_enum_parse64 worker expects from the per-enum table, and on a VALUE parse
// miss returns 0 with *out left null (Parse turns that into a throw, TryParse into false).
// On success boxes the result at the enum's model width — matching the generic
// Enum.Parse<T> semantics.
static int32_t dn2cpp_enum_parse_type_core(Dn2CppType* t, Dn2CppString* s, int32_t ignoreCase,
    Dn2CppObject** out, int32_t* overflow)
{
    *out = nullptr;
    if (overflow != nullptr)
        *overflow = 0;
    if (t == nullptr || (t->typeInfo->flags & DN2CPP_TF_ENUM) == 0)
        dn2cpp_throw_argument();
    const Dn2CppTypeInfo* ti = t->typeInfo;
    int32_t n = ti->enumMemberCount;
    auto* values = static_cast<int64_t*>(dn2cpp_alloc(sizeof(int64_t) * (n > 0 ? n : 1)));
    auto* names = static_cast<Dn2CppString**>(dn2cpp_alloc(sizeof(Dn2CppString*) * (n > 0 ? n : 1)));
    for (int32_t i = 0; i < n; i++)
    {
        values[i] = ti->enumMembers[i].value;
        names[i] = dn2cpp_string_from_utf8(ti->enumMembers[i].name,
            static_cast<int32_t>(std::strlen(ti->enumMembers[i].name)));
    }
    int32_t uWidth = 4;
    bool uUnsigned = false;
    enum_underlying_shape(ti, &uWidth, &uUnsigned);
    int64_t v = 0;
    if (!dn2cpp_enum_parse64(s, values, names, n, ignoreCase, uWidth, uUnsigned ? 1 : 0, &v, overflow))
        return 0;
    *out = dn2cpp_enum_box(ti, v);
    return 1;
}

// Enum.Parse(Type, string[, ignoreCase]): the boxed enum value, or ArgumentException on
// failure (a bad type OR an unmatched value both surface as ArgumentException here) —
// except a numeric token outside the underlying's range, which is real .NET's
// OverflowException.
Dn2CppObject* dn2cpp_enum_parse_type(Dn2CppType* t, Dn2CppString* s, int32_t ignoreCase)
{
    Dn2CppObject* out = nullptr;
    int32_t overflow = 0;
    if (!dn2cpp_enum_parse_type_core(t, s, ignoreCase, &out, &overflow))
    {
        if (overflow != 0)
            dn2cpp_throw_of(&dn2cpp_overflow_exception_type);
        dn2cpp_throw_sr1(&dn2cpp_argument_exception_type, DN2CPP_SR_ENUM_VALUE_NOT_FOUND, s);
    }
    return out;
}

// Enum.TryParse(Type, string[, ignoreCase], out object): 1 + *out=boxed on success,
// 0 + *out=null on a value parse miss (empty/whitespace, an empty token, an unmatched
// name, or a null string). The TYPE is still validated (null / non-enum throw), exactly
// as .NET's non-generic TryParse does — only the value parse is non-throwing, an
// out-of-range numeric token included (null overflow sink: it is just another miss here).
int32_t dn2cpp_enum_try_parse_type(Dn2CppType* t, Dn2CppString* s, int32_t ignoreCase,
    Dn2CppObject** out)
{
    return dn2cpp_enum_parse_type_core(t, s, ignoreCase, out, nullptr);
}

// ---- Enum.Format(Type, object, string) ----
namespace {

// The "D"/"G-miss"/flags-leftover fallback: the underlying value as plain decimal.
Dn2CppString* enum_decimal(int64_t ev, uint64_t uv, int32_t byteWidth, bool isUnsigned)
{
    Dn2CppString* d = dn2cpp_string_from_utf8("D", 1);
    return isUnsigned ? dn2cpp_format_uint(uv, byteWidth, d)
                      : dn2cpp_format_int(ev, byteWidth, d);
}

// .NET's InternalFlagsFormat: consume named members from largest to smallest,
// prepending each so the result reads smallest-first (", "-joined). A value with bits
// no member accounts for returns null (the caller then formats the whole value as
// decimal). Zero maps to the zero-named member if one exists, else "0". The member
// table is pre-sorted ascending by unsigned underlying magnitude (SortedEnumMembers).
Dn2CppString* enum_flags_format(const Dn2CppTypeInfo* ti, uint64_t mask, uint64_t uv)
{
    uint64_t result = uv & mask;
    uint64_t saveResult = result;
    int32_t n = ti->enumMemberCount;
    std::string out;
    bool firstTime = true;
    for (int32_t i = n - 1; i >= 0; i--)
    {
        uint64_t mv = static_cast<uint64_t>(ti->enumMembers[i].value) & mask;
        if (i == 0 && mv == 0)
            break;
        if (mv != 0 && (result & mv) == mv)
        {
            result -= mv;
            if (firstTime) { out = ti->enumMembers[i].name; firstTime = false; }
            else out = std::string(ti->enumMembers[i].name) + ", " + out;
        }
    }
    if (result != 0)
        return nullptr; // unaccounted bits: fall back to decimal
    if (saveResult == 0)
    {
        if (n > 0 && (static_cast<uint64_t>(ti->enumMembers[0].value) & mask) == 0)
            return dn2cpp_string_from_utf8(ti->enumMembers[0].name,
                static_cast<int32_t>(std::strlen(ti->enumMembers[0].name)));
        return dn2cpp_string_from_utf8("0", 1);
    }
    return dn2cpp_string_from_utf8(out.c_str(), static_cast<int32_t>(out.size()));
}

} // namespace

// Enum.Format(Type, object value, string format): the static enum formatter
// EnumConverter.ConvertTo drives. See the header for the per-specifier contract.
Dn2CppString* dn2cpp_enum_format(Dn2CppType* t, Dn2CppObject* value, Dn2CppString* format)
{
    if (t == nullptr || value == nullptr || format == nullptr)
        dn2cpp_throw_argument_null();
    const Dn2CppTypeInfo* ti = t->typeInfo;
    if ((ti->flags & DN2CPP_TF_ENUM) == 0)
        dn2cpp_throw_argument();
    if (format->length != 1)
        dn2cpp_throw_format();
    char16_t fc = format->chars[0];

    int32_t byteWidth;
    bool isUnsigned;
    enum_underlying_shape(ti, &byteWidth, &isUnsigned);
    uint64_t mask = byteWidth >= 8 ? ~0ULL : ((1ULL << (8 * byteWidth)) - 1);
    // The box payload sits right after the header; a 64-bit underlying carries an
    // 8-byte payload, every narrower one the int32 model width (see dn2cpp_object_tostring).
    const void* payload = value + 1;
    int64_t ev = byteWidth == 8 ? *reinterpret_cast<const int64_t*>(payload)
                                : *reinterpret_cast<const int32_t*>(payload);
    uint64_t uv = static_cast<uint64_t>(ev) & mask;

    switch (fc)
    {
        case u'G': case u'g':
        {
            if ((ti->flags & DN2CPP_TF_FLAGS) != 0)
            {
                if (Dn2CppString* f = enum_flags_format(ti, mask, uv))
                    return f;
                return enum_decimal(ev, uv, byteWidth, isUnsigned);
            }
            for (int32_t i = 0; i < ti->enumMemberCount; i++)
                if ((static_cast<uint64_t>(ti->enumMembers[i].value) & mask) == uv)
                    return dn2cpp_string_from_utf8(ti->enumMembers[i].name,
                        static_cast<int32_t>(std::strlen(ti->enumMembers[i].name)));
            return enum_decimal(ev, uv, byteWidth, isUnsigned);
        }
        case u'F': case u'f':
        {
            if (Dn2CppString* f = enum_flags_format(ti, mask, uv))
                return f;
            return enum_decimal(ev, uv, byteWidth, isUnsigned);
        }
        case u'D': case u'd':
            return enum_decimal(ev, uv, byteWidth, isUnsigned);
        case u'X': case u'x':
        {
            // Zero-padded uppercase hex to the underlying width (byte -> 2 digits,
            // int -> 8, long -> 16); .NET's "x" is uppercase too, so both cases here.
            int32_t digits = byteWidth * 2;
            char16_t* buf;
            Dn2CppString* s = dn2cpp_string_alloc(&buf, digits);
            uint64_t h = uv;
            for (int32_t i = digits - 1; i >= 0; i--)
            {
                buf[i] = u"0123456789ABCDEF"[h & 0xF];
                h >>= 4;
            }
            return s;
        }
        default:
            dn2cpp_throw_format();
    }
    return nullptr; // unreachable (all arms return or throw)
}

// The boxed-enum interface map's ISpanFormattable.TryFormat slot. It lives here
// rather than beside the other dispatch thunks in dn2cpp_casts.cpp so that naming
// dn2cpp_enum_format does not pin this translation unit into every binary: only a
// program whose emitted map carries the slot references it. See the header.
int32_t dn2cpp_enum_box_try_format(Dn2CppObject* box, Dn2CppItfCharSpan dest, int32_t* written,
                                   Dn2CppItfCharSpan fmt, const Dn2CppNumberFormatInfo*)
{
    Dn2CppString* spec = fmt.length > 0 ? dn2cpp_string_from_chars(fmt.ptr, fmt.length)
                                        : dn2cpp_string_from_utf8("G", 1);
    return dn2cpp_string_try_copy_to_span(
        dn2cpp_enum_format(const_cast<Dn2CppType*>(box->type->typeObject), box, spec),
        dest.ptr, dest.length, written);
}

// Enum.ToObject(Type, <integral>): the value boxed as the enum type. Intercepted
// because the real body's ValidateRuntimeType asks `enumType is RuntimeType`, which is
// structurally false for a dn2cpp Type object. A null type throws
// ArgumentNullException, a non-enum type ArgumentException, exactly as .NET's
// ValidateRuntimeType reports them. The value is truncated to the underlying width
// and re-extended per its signedness, so the box payload keeps the model invariant
// (an int32 payload IS the sign/zero-extended underlying value; 64-bit underlyings
// carry the full int64) — Enum.ToObject(typeof(ByteE), 300) is (ByteE)44, and a
// negative int into a ulong-underlying enum wraps, both matching .NET.
Dn2CppObject* dn2cpp_enum_to_object(Dn2CppType* t, int64_t value)
{
    if (t == nullptr)
        dn2cpp_throw_argument_null();
    const Dn2CppTypeInfo* ti = t->typeInfo;
    if ((ti->flags & DN2CPP_TF_ENUM) == 0)
        dn2cpp_throw_argument();
    int32_t byteWidth;
    bool isUnsigned;
    enum_underlying_shape(ti, &byteWidth, &isUnsigned);
    if (byteWidth == 8)
        return dn2cpp_box(ti, &value, sizeof(int64_t));
    uint64_t uv = static_cast<uint64_t>(value) & ((1ULL << (8 * byteWidth)) - 1);
    int32_t v;
    if (isUnsigned)
        v = static_cast<int32_t>(uv);
    else
    {
        // Sign-extend from the underlying width back to the int32 box model.
        int shift = 64 - 8 * byteWidth;
        v = static_cast<int32_t>(static_cast<int64_t>(uv << shift) >> shift);
    }
    return dn2cpp_box(ti, &v, sizeof(int32_t));
}

// Type.GetNestedTypes(): the type's public nested types (emitted, non-generic), wrapped
// as Type[]. Non-public / generic / unreached nested types are not in the table.
Dn2CppArrayRef* dn2cpp_type_get_nested_types(Dn2CppType* t)
{
    const Dn2CppTypeInfo* ti = dn2cpp_type_require(t);
    int32_t n = ti->nestedCount;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
        arr->data[i] = reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(ti->nestedTypes[i]));
    return arr;
}

// Whole-string compare of a managed UTF-16 string against an ASCII C string (defined
// below in the field-enumeration section; forward-declared for the nested-type lookup).
static bool dn2cpp_ascii_str_eq(const char* c, const Dn2CppString* s);

// Type.GetNestedType(name): the matching public nested type or null. Name is the simple
// nested-type name (e.g. "Inner").
Dn2CppType* dn2cpp_type_get_nested_type(Dn2CppType* t, Dn2CppString* name)
{
    const Dn2CppTypeInfo* ti = dn2cpp_type_require(t);
    if (name == nullptr)
        return nullptr;
    for (int32_t i = 0; i < ti->nestedCount; i++)
    {
        // A nested type-info's name is its full CLR reflection name
        // ("Ns.Outer+Inner"); GetNestedType matches on the simple-name tail.
        if (dn2cpp_ascii_str_eq(dn2cpp_simple_type_name(ti->nestedTypes[i]->name), name))
            return dn2cpp_get_type_from_handle(ti->nestedTypes[i]);
    }
    return nullptr;
}

// The ']' closing the bracket group that opens at s[p], or len when unterminated.
static size_t dn2cpp_name_bracket_end(const char* s, size_t len, size_t p)
{
    int depth = 0;
    for (size_t i = p; i < len; i++)
    {
        if (s[i] == '[')
            depth++;
        else if (s[i] == ']' && --depth == 0)
            return i;
    }
    return len;
}

// The first ',' outside every bracket group, or len. Depth-aware because a closed
// generic's spelling carries commas both between arguments and inside an
// argument's own assembly-qualified suffix.
static size_t dn2cpp_name_top_comma(const char* s, size_t len)
{
    int depth = 0;
    for (size_t i = 0; i < len; i++)
    {
        if (s[i] == '[')
            depth++;
        else if (s[i] == ']')
            depth--;
        else if (s[i] == ',' && depth == 0)
            return i;
    }
    return len;
}

// Resolves a CLR type name, accepting a plain registry name and the composed
// closed-generic spellings dn2cpp_type_tostring/dn2cpp_type_fullname produce.
// NO registry entry carries the composed form — a closed instantiation is keyed by
// its dn2cpp-mangled name — so it resolves structurally instead, on the
// MakeGenericType relation. That is what keeps Type.GetType(t.FullName) and the
// AssemblyQualifiedName round-trip (a serializer's `$type`) working.
static const Dn2CppTypeInfo* dn2cpp_resolve_type_name(const char* s, size_t len, int depth = 0)
{
    // The nesting a REAL name can carry is the type graph's, which is shallow; the
    // string is caller-supplied, so cap the recursion rather than the input.
    if (depth > 16)
        return nullptr;
    while (len > 0 && s[0] == ' ') { s++; len--; }
    len = dn2cpp_name_top_comma(s, len);
    while (len > 0 && s[len - 1] == ' ') len--;
    if (len == 0)
        return nullptr;
    // A registry name wins outright: every non-generic type, every array and every
    // closed instantiation is registered under one, so nothing below can perturb a
    // lookup that already resolved.
    if (const Dn2CppTypeInfo* direct = dn2cpp_type_registry_find(s, static_cast<int32_t>(len)))
        return direct;
    const char* open = static_cast<const char*>(std::memchr(s, '[', len));
    // "[]" is an array suffix, not an argument list.
    if (open == nullptr || open + 1 >= s + len || open[1] == ']')
        return nullptr;
    size_t p = static_cast<size_t>(open - s);
    size_t close = dn2cpp_name_bracket_end(s, len, p);
    if (close >= len)
        return nullptr;
    const Dn2CppTypeInfo* def = dn2cpp_type_registry_find(s, static_cast<int32_t>(p));
    if (def == nullptr || (def->flags & DN2CPP_TF_GENERICDEF) == 0)
        return nullptr;
    const Dn2CppTypeInfo* args[32];
    int32_t argc = 0;
    const char* cur = s + p + 1;
    const char* end = s + close;
    while (cur < end)
    {
        if (argc == static_cast<int32_t>(sizeof args / sizeof args[0]))
            return nullptr;
        size_t cut = dn2cpp_name_top_comma(cur, static_cast<size_t>(end - cur));
        const char* a = cur;
        size_t alen = cut;
        while (alen > 0 && a[0] == ' ') { a++; alen--; }
        // The FullName spelling wraps each argument in its own brackets, with that
        // argument's assembly display name inside them.
        if (alen >= 2 && a[0] == '[' && a[alen - 1] == ']') { a++; alen -= 2; }
        const Dn2CppTypeInfo* at = dn2cpp_resolve_type_name(a, alen, depth + 1);
        if (at == nullptr)
            return nullptr;
        args[argc++] = at;
        cur += cut + 1;
    }
    if (argc == 0)
        return nullptr;
    for (int32_t k = 0; k < dn2cpp_type_registry_count; k++)
    {
        const Dn2CppTypeInfo* cand = dn2cpp_type_registry[k].type;
        if (cand->genericDef != def || (cand->flags & DN2CPP_TF_GENERICDEF) != 0
            || cand->genericArgCount != argc)
            continue;
        bool match = true;
        for (int32_t i = 0; i < argc; i++)
            if (cand->genericArgs[i] != args[i]) { match = false; break; }
        if (match)
            return cand;
    }
    // The typeof-only template fallback — the same relation MakeGenericType
    // resolves on, so a GetType(t.FullName) round-trip over a synthesized
    // instantiation answers the same pointer.
    return dn2cpp_try_synthesize_generic(def, args, argc);
}

// Narrows a managed name to ASCII bytes and resolves it. Candidate names are ASCII
// (CLR identifiers/namespaces), so byte equality against the narrowed unit is
// equivalent for units <= 0xFF, and any unit above 0xFF is a definite miss.
static const Dn2CppTypeInfo* dn2cpp_resolve_managed_type_name(const Dn2CppString* name)
{
    int32_t len = name->length;
    char stackBuf[256];
    char* buf = len <= static_cast<int32_t>(sizeof stackBuf) ? stackBuf : new char[len];
    bool representable = true;
    for (int32_t i = 0; i < len; i++)
    {
        char16_t c = name->chars[i];
        if (c > 0xFF) { representable = false; break; }
        buf[i] = static_cast<char>(static_cast<unsigned char>(c));
    }
    const Dn2CppTypeInfo* ti =
        representable ? dn2cpp_resolve_type_name(buf, static_cast<size_t>(len)) : nullptr;
    if (buf != stackBuf)
        delete[] buf;
    return ti;
}

// Type.GetType(string): resolve a CLR FullName against the embedded registry
// (whose static half is hash-indexed). A trailing assembly-qualified suffix is
// ignored, matching the common "Namespace.Type[, Assembly]" usage. Returns the
// same type handle as typeof on that type (registry entries point at the shared
// Dn2CppTypeInfo), so Type.GetType("System.Int32") == typeof(int).
Dn2CppType* dn2cpp_type_get_by_name(Dn2CppString* name, int32_t throwOnError)
{
    if (name != nullptr)
        if (const Dn2CppTypeInfo* ti = dn2cpp_resolve_managed_type_name(name))
            return dn2cpp_get_type_from_handle(ti);
    if (throwOnError)
        dn2cpp_throw_type_load();
    return nullptr;
}

// ---- reflection: field enumeration ----

// System.Reflection.BindingFlags bits we honor.
#define DN2CPP_BF_DECLAREDONLY 0x2
#define DN2CPP_BF_INSTANCE     0x4
#define DN2CPP_BF_STATIC       0x8
#define DN2CPP_BF_PUBLIC       0x10
#define DN2CPP_BF_NONPUBLIC    0x20
#define DN2CPP_BF_FLATTEN      0x40

// Whole-string compare of a managed UTF-16 string against an ASCII C string.
static bool dn2cpp_ascii_str_eq(const char* c, const Dn2CppString* s)
{
    int32_t i = 0;
    for (; i < s->length; i++)
        if (c[i] == '\0' ||
            static_cast<char16_t>(static_cast<unsigned char>(c[i])) != s->chars[i])
            return false;
    return c[i] == '\0';
}

// Member-name pattern for Type.GetMember: exact, or the documented trailing-'*'
// prefix wildcard ("Get*"). A null pattern matches everything (GetMembers).
static bool dn2cpp_name_pattern_matches(const char* c, const Dn2CppString* s)
{
    if (s == nullptr)
        return true;
    int32_t n = s->length;
    if (n > 0 && s->chars[n - 1] == u'*')
    {
        for (int32_t i = 0; i < n - 1; i++)
            if (c[i] == '\0' ||
                static_cast<char16_t>(static_cast<unsigned char>(c[i])) != s->chars[i])
                return false;
        return true;
    }
    return dn2cpp_ascii_str_eq(c, s);
}

// A method-flavored reflected handle header: MethodInfo or ConstructorInfo
// (both wrap a Dn2CppMethodRef; the distinct headers discriminate casts/`is`
// like real .NET's MethodBase split).
static bool dn2cpp_is_methodref_header(const Dn2CppTypeInfo* t)
{
    return t == &dn2cpp_methodinfo_type || t == &dn2cpp_constructorinfo_type;
}

// A non-null Binder is a custom binder (Type.DefaultBinder is modeled as null):
// honoring it would require re-implementing binder-driven overload selection,
// and ignoring it could silently pick a different member than the caller's
// binder would — fail loud instead (catchable, the NativeAOT posture).
static void dn2cpp_reflection_check_binder(Dn2CppObject* binder)
{
    if (binder != nullptr)
        dn2cpp_throw_platform_not_supported(
            "a custom reflection Binder is not supported in AOT-compiled code (pass null / Type.DefaultBinder)");
}

// System.Reflection.CallingConventions is observably a NO-OP filter in real .NET's
// Get{Method,Constructor} paths, and every transpiled method is Standard (| HasThis),
// so matching that behavior means ignoring the argument. The unified lookups accept it
// for arity fidelity and discard it.

// A type whose field/method/property metadata --trim-reflection removed (see
// DN2CPP_TF_METADATA_STRIPPED). Reading it must FAIL loudly: from the tables alone a
// stripped type is indistinguishable from a genuinely member-less one — both read
// (nullptr, 0) — so the alternative is GetMethods() returning an empty array, a silent
// wrong answer. Throws a catchable PlatformNotSupportedException naming the type and
// both remedies. Every base-chain walker calls this AT EVERY LEVEL: a kept class over a
// stripped base is how inherited members would silently go missing.
//
// Constructors are deliberately outside this rule — --trim-reflection keeps the ctor
// tables, because Type.GetConstructor over a type chosen at RUN time is a live
// cross-assembly path (GodotSharp's RuntimeTypeConversionHelper) no static keep-set can
// see. So dn2cpp_collect_ctors / _type_get_constructor_full / the two Activator entry
// points do NOT call this, and keep answering on a stripped type.
static void dn2cpp_require_metadata(const Dn2CppTypeInfo* ti)
{
    if (ti == nullptr || (ti->flags & DN2CPP_TF_METADATA_STRIPPED) == 0)
        return;
    const char* name = ti->name != nullptr ? ti->name : "<unnamed type>";
    char buf[512];
    std::snprintf(buf, sizeof(buf),
        "Reflection over the members of '%s' is not available: this image was transpiled "
        "with --trim-reflection, which removes the field, method and property metadata of "
        "every type the program was not seen to reflect over. Keep this one with "
        "'--reflection-root %s', or transpile without --trim-reflection. "
        "(GetConstructor and Activator.CreateInstance still work on this type.)",
        name, name);
    dn2cpp_throw_platform_not_supported(buf);
}

// Shared by fields and methods (attrs use the same STATIC/PUBLIC/PRIVATE bit layout).
// CLR member-binding rules: an inherited member is excluded if private, an inherited
// static needs FlattenHierarchy, and visibility/scope must both match the requested flags.
static bool dn2cpp_member_matches(int32_t attrs, int32_t flags, bool inherited)
{
    bool isStatic = (attrs & DN2CPP_FLDA_STATIC) != 0;
    bool isPublic = (attrs & DN2CPP_FLDA_PUBLIC) != 0;
    bool isPrivate = (attrs & DN2CPP_FLDA_PRIVATE) != 0;
    if (inherited)
    {
        if (isPrivate)
            return false;
        if (isStatic && (flags & DN2CPP_BF_FLATTEN) == 0)
            return false;
    }
    bool vis = isPublic ? (flags & DN2CPP_BF_PUBLIC) != 0 : (flags & DN2CPP_BF_NONPUBLIC) != 0;
    bool scope = isStatic ? (flags & DN2CPP_BF_STATIC) != 0 : (flags & DN2CPP_BF_INSTANCE) != 0;
    return vis && scope;
}

static bool dn2cpp_field_matches(const Dn2CppFieldInfo* f, int32_t flags, bool inherited)
{
    return dn2cpp_member_matches(f->attrs, flags, inherited);
}

// ---- reflected member-handle intern ----
// One managed wrapper per (metadata row, reflected type) — the MemberInfo-side sibling
// of the Type intern in dn2cpp_typeinfo.cpp, and the same key .NET's RuntimeType member
// cache uses (so typeof(D).GetMethod(m) and typeof(Base).GetMethod(m) are distinct
// instances for an inherited m). Reference identity is load-bearing: the virtual
// MemberInfo.Equals(object) IS reference equality (only op_Equality is row-aware), so
// EqualityComparer<MemberInfo>.Default — every List<MemberInfo>.Contains, HashSet
// dedup, Dictionary key — works by identity, and fresh per-call handles silently break
// callers that select members that way. The key can never dangle: every Dn2CppTypeInfo,
// and with it its fields/methods/ctors/props row arrays, is a data-segment static or
// GC-rooted forever (dyn registry / g_loadedImages). The maps' buffers are not
// GC-scanned, so the wrappers are allocated uncollectable (dn2cpp_alloc_pinned) — a
// MemberInfo lives for the process, as in .NET. A generic-method definition VIEW
// (isGenericDefView=1) is a distinct managed object over the same row and interns in
// its own map; minting it by retagging the plain handle would corrupt the shared one.
//
// The minters take the reflected type EXPLICITLY at every call: a Type-query path
// passes the ORIGINAL receiver's type-info (not the base-chain level the row was found
// at — that difference is the whole model), a propagating path passes its own handle's
// reflectedType, and every other mint passes nullptr, normalized to the row's
// declaringType here. After normalization the field is never null, so equality and the
// map key are plain pointer compares. The normalization matches .NET: delegate.Method,
// GetBaseDefinition and GetGenericMethodDefinition all hand back the declaring-typed
// instance.
struct Dn2CppMemberInternKey
{
    const void* row;
    const Dn2CppTypeInfo* reflected;
    bool operator==(const Dn2CppMemberInternKey& o) const
    {
        return row == o.row && reflected == o.reflected;
    }
};

struct Dn2CppMemberInternKeyHash
{
    size_t operator()(const Dn2CppMemberInternKey& k) const
    {
        // Fibonacci-fold the two pointer words (both alignment-shifted); plain
        // arithmetic, no seeded hashing (the self-host discipline).
        uint64_t h = static_cast<uint64_t>(reinterpret_cast<uintptr_t>(k.row)) >> 4;
        uint64_t r = static_cast<uint64_t>(reinterpret_cast<uintptr_t>(k.reflected)) >> 4;
        return static_cast<size_t>(h * 0x9E3779B97F4A7C15ULL ^ r);
    }
};

template <typename THandle>
using Dn2CppMemberInternMap =
    std::unordered_map<Dn2CppMemberInternKey, THandle*, Dn2CppMemberInternKeyHash>;

static std::mutex& g_memberref_intern_mtx = dn2cpp_never_destroyed<std::mutex>();
static Dn2CppMemberInternMap<Dn2CppFieldRef>& g_fieldref_intern =
    dn2cpp_never_destroyed<Dn2CppMemberInternMap<Dn2CppFieldRef>>();
static Dn2CppMemberInternMap<Dn2CppMethodRef>& g_methodref_intern =
    dn2cpp_never_destroyed<Dn2CppMemberInternMap<Dn2CppMethodRef>>();
static Dn2CppMemberInternMap<Dn2CppMethodRef>& g_methodref_defview_intern =
    dn2cpp_never_destroyed<Dn2CppMemberInternMap<Dn2CppMethodRef>>();
static Dn2CppMemberInternMap<Dn2CppPropRef>& g_propref_intern =
    dn2cpp_never_destroyed<Dn2CppMemberInternMap<Dn2CppPropRef>>();

static Dn2CppFieldRef* dn2cpp_make_fieldref(const Dn2CppFieldInfo* f,
                                            const Dn2CppTypeInfo* reflected)
{
    if (reflected == nullptr)
        reflected = f->declaringType;
    std::lock_guard<std::mutex> lk(g_memberref_intern_mtx);
    Dn2CppFieldRef*& slot = g_fieldref_intern[{ f, reflected }];
    if (slot == nullptr)
    {
        auto* r = static_cast<Dn2CppFieldRef*>(dn2cpp_alloc_pinned(sizeof(Dn2CppFieldRef)));
        r->type = &dn2cpp_fieldinfo_type;
        r->field = f;
        r->reflectedType = reflected;
        slot = r;
    }
    return slot;
}

// Walk the type and its base chain (stopping at DeclaredOnly), collecting matching
// fields. With out==nullptr only counts; otherwise fills out[] with the interned handles.
static int32_t dn2cpp_collect_fields(const Dn2CppTypeInfo* type, int32_t flags, Dn2CppObject** out)
{
    int32_t n = 0;
    for (const Dn2CppTypeInfo* ti = type; ti != nullptr; ti = ti->base)
    {
        dn2cpp_require_metadata(ti);
        bool inherited = (ti != type);
        for (int32_t i = 0; i < ti->fieldCount; i++)
            if (dn2cpp_field_matches(&ti->fields[i], flags, inherited))
            {
                if (out != nullptr)
                    out[n] = reinterpret_cast<Dn2CppObject*>(
                        dn2cpp_make_fieldref(&ti->fields[i], type));
                n++;
            }
        if (flags & DN2CPP_BF_DECLAREDONLY)
            break;
    }
    return n;
}

Dn2CppArrayRef* dn2cpp_type_get_fields(Dn2CppType* t, int32_t bindingFlags)
{
    dn2cpp_type_require(t);
    int32_t n = dn2cpp_collect_fields(t->typeInfo, bindingFlags, nullptr);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    dn2cpp_collect_fields(t->typeInfo, bindingFlags, arr->data);
    return arr;
}

Dn2CppFieldRef* dn2cpp_type_get_field(Dn2CppType* t, Dn2CppString* name, int32_t bindingFlags)
{
    dn2cpp_type_require(t);
    if (name == nullptr)
        return nullptr;
    for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr; ti = ti->base)
    {
        dn2cpp_require_metadata(ti);
        bool inherited = (ti != t->typeInfo);
        for (int32_t i = 0; i < ti->fieldCount; i++)
            if (dn2cpp_field_matches(&ti->fields[i], bindingFlags, inherited) &&
                dn2cpp_ascii_str_eq(ti->fields[i].name, name))
                return dn2cpp_make_fieldref(&ti->fields[i], t->typeInfo);
        if (bindingFlags & DN2CPP_BF_DECLAREDONLY)
            break;
    }
    return nullptr;
}

// A null FieldInfo handle reaching any accessor below is an instance call on a
// null reference (Type.GetField answered null and the caller didn't check —
// Newtonsoft's EnumUtils does exactly `field!.GetValue(null)`): real .NET
// raises NullReferenceException there, so throw the catchable managed
// equivalent instead of dereferencing — loud, never a SIGSEGV.
static const Dn2CppFieldInfo* dn2cpp_fieldref_require(Dn2CppFieldRef* f)
{
    if (f == nullptr || f->field == nullptr)
        dn2cpp_throw_null_reference();
    return f->field;
}

// The same rule for the other reflected-handle families (the Type handle's is
// dn2cpp_type_require, in dn2cpp_core.h, because the emitted lowerings read ->typeInfo
// directly). An unguarded entry point is a SIGSEGV on the null its own lookup is
// documented to return. One require-function per family, so a newly added entry point
// cannot quietly pick a different answer.
static const Dn2CppMethodInfo* dn2cpp_methodref_require(Dn2CppMethodRef* m)
{
    if (m == nullptr || m->method == nullptr)
        dn2cpp_throw_null_reference();
    return m->method;
}

static const Dn2CppPropInfo* dn2cpp_propref_require(Dn2CppPropRef* p)
{
    if (p == nullptr || p->prop == nullptr)
        dn2cpp_throw_null_reference();
    return p->prop;
}

// The ParameterInfo guard hands back the HANDLE, not the row: Position lives on
// the handle and Member reads its owner, so callers need both halves.
static Dn2CppParamRef* dn2cpp_paramref_require(Dn2CppParamRef* p)
{
    if (p == nullptr || p->param == nullptr)
        dn2cpp_throw_null_reference();
    return p;
}

static const Dn2CppAttrInfo* dn2cpp_attrdataref_require(Dn2CppAttrDataRef* d)
{
    if (d == nullptr || d->attr == nullptr)
        dn2cpp_throw_null_reference();
    return d->attr;
}

// The header-dispatched MemberInfo accessors (Name / DeclaringType / MemberType /
// MetadataToken / Module / CustomAttributes) take the handle as a bare
// Dn2CppObject* and switch on its header; without this guard each would answer a
// DEFAULT on null (null name, null declaring type, MemberTypes 0, token 0), which reads
// like a real answer at the call site. Equality is deliberately NOT routed here:
// `a.Equals(null)` and `a == null` are legal calls whose null is the ARGUMENT, so
// dn2cpp_memberinfo_equals keeps its own null arm.
static Dn2CppObject* dn2cpp_memberref_require(Dn2CppObject* m)
{
    if (m == nullptr)
        dn2cpp_throw_null_reference();
    return m;
}

Dn2CppType* dn2cpp_fieldref_field_type(Dn2CppFieldRef* f)
{
    return dn2cpp_get_type_from_handle(dn2cpp_fieldref_require(f)->fieldType);
}

int32_t dn2cpp_fieldref_is_static(Dn2CppFieldRef* f)
{
    return (dn2cpp_fieldref_require(f)->attrs & DN2CPP_FLDA_STATIC) != 0 ? 1 : 0;
}

int32_t dn2cpp_fieldref_is_public(Dn2CppFieldRef* f)
{
    return (dn2cpp_fieldref_require(f)->attrs & DN2CPP_FLDA_PUBLIC) != 0 ? 1 : 0;
}

int32_t dn2cpp_fieldref_is_initonly(Dn2CppFieldRef* f)
{
    return (dn2cpp_fieldref_require(f)->attrs & DN2CPP_FLDA_INITONLY) != 0 ? 1 : 0;
}

int32_t dn2cpp_fieldref_is_literal(Dn2CppFieldRef* f)
{
    return (dn2cpp_fieldref_require(f)->attrs & DN2CPP_FLDA_LITERAL) != 0 ? 1 : 0;
}

// FieldInfo.GetValue / SetValue: dispatch to the emitter-generated typed
// thunk, which boxes/unboxes value-type fields and passes reference fields through.
// A field with no reflectable storage (literal/const, opaque declaring type) has a
// null thunk -> InvalidOperationException — except an enum's literal member row,
// which carries its constant in literalValue and answers the boxed enum value
// like real .NET (the Newtonsoft EnumUtils name -> field -> value path).
Dn2CppObject* dn2cpp_fieldref_get_value(Dn2CppFieldRef* f, Dn2CppObject* obj)
{
    const Dn2CppFieldInfo* fi = dn2cpp_fieldref_require(f);
    if (fi->getter == nullptr)
    {
        if ((fi->attrs & DN2CPP_FLDA_LITERAL) != 0 && fi->declaringType != nullptr
            && (fi->declaringType->flags & DN2CPP_TF_ENUM) != 0)
        {
            // Box at the model width every other enum box uses (int32; int64
            // for a 64-bit-underlying enum), stamped with the declaring enum's
            // own type-info — the same shape dn2cpp_enum_parse_type_core emits.
            const Dn2CppTypeInfo* uti = fi->declaringType->enumUnderlying;
            if (uti == &dn2cpp_int64_type || uti == &dn2cpp_uint64_type)
            {
                int64_t v = fi->literalValue;
                return dn2cpp_box(fi->declaringType, &v, sizeof(v));
            }
            int32_t v = static_cast<int32_t>(fi->literalValue);
            return dn2cpp_box(fi->declaringType, &v, sizeof(v));
        }
        dn2cpp_throw_invalid_operation();
    }
    return fi->getter(obj);
}

void dn2cpp_fieldref_set_value(Dn2CppFieldRef* f, Dn2CppObject* obj, Dn2CppObject* value)
{
    const Dn2CppFieldInfo* fi = dn2cpp_fieldref_require(f);
    if (fi->setter == nullptr)
        dn2cpp_throw_invalid_operation();
    fi->setter(obj, value);
}

// MemberInfo.Name / DeclaringType: shared by FieldInfo and Type, so dispatch on the
// managed object header. A Type's DeclaringType (enclosing type of a nested type) is
// not modeled — top-level types report null, matching .NET.
Dn2CppString* dn2cpp_memberinfo_name(Dn2CppObject* m)
{
    dn2cpp_memberref_require(m);
    const char* nm = nullptr;
    if (m->type == &dn2cpp_fieldinfo_type)
        nm = reinterpret_cast<Dn2CppFieldRef*>(m)->field->name;
    else if (dn2cpp_is_methodref_header(m->type))
        nm = reinterpret_cast<Dn2CppMethodRef*>(m)->method->name;
    else if (m->type == &dn2cpp_propertyinfo_type)
        nm = reinterpret_cast<Dn2CppPropRef*>(m)->prop->name;
    if (nm != nullptr)
        return dn2cpp_string_from_utf8(nm, static_cast<int32_t>(std::strlen(nm)));
    return dn2cpp_type_name(reinterpret_cast<Dn2CppType*>(m)->typeInfo);
}

Dn2CppType* dn2cpp_memberinfo_declaring_type(Dn2CppObject* m)
{
    dn2cpp_memberref_require(m);
    if (m->type == &dn2cpp_fieldinfo_type)
        return dn2cpp_get_type_from_handle(reinterpret_cast<Dn2CppFieldRef*>(m)->field->declaringType);
    if (dn2cpp_is_methodref_header(m->type))
        return dn2cpp_get_type_from_handle(reinterpret_cast<Dn2CppMethodRef*>(m)->method->declaringType);
    if (m->type == &dn2cpp_propertyinfo_type)
        return dn2cpp_get_type_from_handle(reinterpret_cast<Dn2CppPropRef*>(m)->prop->declaringType);
    return nullptr;
}

// MemberInfo.ReflectedType: the type-info stamped on the handle at mint time
// (never null there — non-query mints normalize to the declaring type; see the
// member-handle intern note). A Type receiver mirrors what DeclaringType
// answers above: nested-type enclosure is not modeled, so it reports null —
// for a top-level type that matches .NET, for a nested type (where .NET
// answers the enclosing type for both properties) it is the same known
// divergence DeclaringType already carries.
Dn2CppType* dn2cpp_memberinfo_reflected_type(Dn2CppObject* m)
{
    dn2cpp_memberref_require(m);
    if (m->type == &dn2cpp_fieldinfo_type)
        return dn2cpp_get_type_from_handle(reinterpret_cast<Dn2CppFieldRef*>(m)->reflectedType);
    if (dn2cpp_is_methodref_header(m->type))
        return dn2cpp_get_type_from_handle(reinterpret_cast<Dn2CppMethodRef*>(m)->reflectedType);
    if (m->type == &dn2cpp_propertyinfo_type)
        return dn2cpp_get_type_from_handle(reinterpret_cast<Dn2CppPropRef*>(m)->reflectedType);
    return nullptr;
}

// MemberInfo equality: two handles are equal when they wrap the same underlying
// metadata entry (method/field/property row, or the same type-info for a Type receiver)
// AND were obtained through the same reflected type — .NET's rule, under which
// typeof(D).GetMethod(m) and typeof(Base).GetMethod(m) are unequal for an inherited m.
// Both reflectedType fields are mint-time normalized (never null), so the comparison is
// a plain pointer compare. Handles are interned per (row, reflectedType), so a == b
// answers the common case; the row comparison stays as the semantic definition — it
// adjudicates a Type receiver and keeps op_Equality independent of how a handle was
// minted. The header dispatch keeps one helper serving MethodInfo, ConstructorInfo
// (also a MethodRef), FieldInfo, PropertyInfo and Type alike.
int32_t dn2cpp_memberinfo_equals(Dn2CppObject* a, Dn2CppObject* b)
{
    if (a == b)
        return 1;
    if (a == nullptr || b == nullptr || a->type != b->type)
        return 0;
    if (dn2cpp_is_methodref_header(a->type))
    {
        auto* ma = reinterpret_cast<Dn2CppMethodRef*>(a);
        auto* mb = reinterpret_cast<Dn2CppMethodRef*>(b);
        if (ma->reflectedType != mb->reflectedType)
            return 0;
        // A generic-method definition view is never equal to a closed handle;
        // two definition views agree on (declaringType, definition token), so
        // the definitions reached from different instantiations compare equal.
        if (ma->isGenericDefView != mb->isGenericDefView)
            return 0;
        if (ma->isGenericDefView)
            return ma->method->declaringType == mb->method->declaringType
                && ma->method->metadataToken == mb->method->metadataToken ? 1 : 0;
        return ma->method == mb->method ? 1 : 0;
    }
    if (a->type == &dn2cpp_fieldinfo_type)
        return reinterpret_cast<Dn2CppFieldRef*>(a)->field == reinterpret_cast<Dn2CppFieldRef*>(b)->field
            && reinterpret_cast<Dn2CppFieldRef*>(a)->reflectedType
                == reinterpret_cast<Dn2CppFieldRef*>(b)->reflectedType ? 1 : 0;
    if (a->type == &dn2cpp_propertyinfo_type)
        return reinterpret_cast<Dn2CppPropRef*>(a)->prop == reinterpret_cast<Dn2CppPropRef*>(b)->prop
            && reinterpret_cast<Dn2CppPropRef*>(a)->reflectedType
                == reinterpret_cast<Dn2CppPropRef*>(b)->reflectedType ? 1 : 0;
    if (a->type == &dn2cpp_type_type)
        return reinterpret_cast<Dn2CppType*>(a)->typeInfo == reinterpret_cast<Dn2CppType*>(b)->typeInfo ? 1 : 0;
    return 0;
}

// ---- reflection: method enumeration ----

static Dn2CppMethodRef* dn2cpp_make_methodref(const Dn2CppMethodInfo* mi,
                                              const Dn2CppTypeInfo* reflected)
{
    if (reflected == nullptr)
        reflected = mi->declaringType;
    std::lock_guard<std::mutex> lk(g_memberref_intern_mtx);
    Dn2CppMethodRef*& slot = g_methodref_intern[{ mi, reflected }];
    if (slot == nullptr)
    {
        auto* r = static_cast<Dn2CppMethodRef*>(dn2cpp_alloc_pinned(sizeof(Dn2CppMethodRef)));
        // A ctortab row gets the ConstructorInfo header (a static .cctor is never a
        // reflected row), a methtab row the MethodInfo one — `is ConstructorInfo` /
        // `is MethodInfo` then discriminate like real .NET.
        r->type = std::strcmp(mi->name, ".ctor") == 0
            ? &dn2cpp_constructorinfo_type
            : &dn2cpp_methodinfo_type;
        r->method = mi;
        r->reflectedType = reflected;
        r->isGenericDefView = 0;
        slot = r;
    }
    return slot;
}

// The generic-method definition view over a row (see the isGenericDefView
// note in dn2cpp_core.h): a DISTINCT interned handle from the plain one —
// never mint it by retagging dn2cpp_make_methodref's result, which is shared.
static Dn2CppMethodRef* dn2cpp_make_methodref_defview(const Dn2CppMethodInfo* mi,
                                                      const Dn2CppTypeInfo* reflected)
{
    if (reflected == nullptr)
        reflected = mi->declaringType;
    std::lock_guard<std::mutex> lk(g_memberref_intern_mtx);
    Dn2CppMethodRef*& slot = g_methodref_defview_intern[{ mi, reflected }];
    if (slot == nullptr)
    {
        auto* r = static_cast<Dn2CppMethodRef*>(dn2cpp_alloc_pinned(sizeof(Dn2CppMethodRef)));
        r->type = &dn2cpp_methodinfo_type;
        r->method = mi;
        r->reflectedType = reflected;
        r->isGenericDefView = 1;
        slot = r;
    }
    return slot;
}

// Walk the type and its base chain (stopping at DeclaredOnly), collecting matching
// methods. A virtual method (vtableSlot >= 0) is reported only at its most-derived
// override: once a slot is seen walking derived→base, the inherited definition at the
// same slot is skipped. Non-virtual methods (slot -1, includes `new` hiding and
// overloads) are all kept. With out==nullptr only counts; otherwise fills out[].
static int32_t dn2cpp_collect_methods(const Dn2CppTypeInfo* type, int32_t flags, Dn2CppObject** out)
{
    int32_t n = 0;
    // Track virtual slots already emitted by a more-derived type (small linear set;
    // method counts per type are tiny).
    int32_t seen[256];
    int32_t seenCount = 0;
    for (const Dn2CppTypeInfo* ti = type; ti != nullptr; ti = ti->base)
    {
        dn2cpp_require_metadata(ti);
        bool inherited = (ti != type);
        for (int32_t i = 0; i < ti->methodCount; i++)
        {
            const Dn2CppMethodInfo* mi = &ti->methods[i];
            if (!dn2cpp_member_matches(mi->attrs, flags, inherited))
                continue;
            if (mi->vtableSlot >= 0)
            {
                bool hidden = false;
                for (int32_t s = 0; s < seenCount; s++)
                    if (seen[s] == mi->vtableSlot) { hidden = true; break; }
                if (hidden)
                    continue;
                if (seenCount < 256)
                    seen[seenCount++] = mi->vtableSlot;
            }
            if (out != nullptr)
                out[n] = reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(mi, type));
            n++;
        }
        if (flags & DN2CPP_BF_DECLAREDONLY)
            break;
    }
    return n;
}

Dn2CppArrayRef* dn2cpp_type_get_methods(Dn2CppType* t, int32_t bindingFlags)
{
    dn2cpp_type_require(t);
    int32_t n = dn2cpp_collect_methods(t->typeInfo, bindingFlags, nullptr);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    dn2cpp_collect_methods(t->typeInfo, bindingFlags, arr->data);
    return arr;
}

// Exact parameter-type-list match against a caller-supplied Type[] (the same
// identity rule as GetConstructor(Type[])). A null Type element throws
// ArgumentNullException like real .NET.
static bool dn2cpp_params_match_types(const Dn2CppMethodInfo* mi, Dn2CppArrayRef* types)
{
    if (mi->paramCount != types->length)
        return false;
    for (int32_t j = 0; j < types->length; j++)
    {
        auto* pt = reinterpret_cast<Dn2CppType*>(types->data[j]);
        if (pt == nullptr)
            dn2cpp_throw_argument_null();
        if (mi->parameters[j].paramType != pt->typeInfo)
            return false;
    }
    return true;
}

// Whether two method rows carry the identical parameter-type list (the
// "hide-by-name-and-sig" equality behind .NET's most-derived-wins rule).
static bool dn2cpp_params_equal(const Dn2CppMethodInfo* a, const Dn2CppMethodInfo* b)
{
    if (a->paramCount != b->paramCount)
        return false;
    for (int32_t j = 0; j < a->paramCount; j++)
        if (a->parameters[j].paramType != b->parameters[j].paramType)
            return false;
    return true;
}

// Resolves a GetMethod candidate set the way real .NET's GetMethodImpl does:
// one candidate wins outright; sig-equal candidates (a `new`-hiding chain)
// resolve to the most derived one (candidates are collected derived-first);
// several rows of ONE generic method definition (its per-instantiation methtab
// rows share the definition's metadata token) resolve to the first row — the
// definition itself is not materialized in an AOT image, so the caller gets a
// representative closed instantiation (IsGenericMethod == true). Anything else
// is genuinely ambiguous and throws AmbiguousMatchException.
static const Dn2CppMethodInfo* dn2cpp_resolve_method_candidates(const Dn2CppMethodInfo* const* c, int32_t n)
{
    if (n == 1)
        return c[0];
    bool allEqual = true;
    for (int32_t i = 1; i < n && allEqual; i++)
        allEqual = dn2cpp_params_equal(c[0], c[i]);
    if (allEqual)
        return c[0];
    for (int32_t i = 1; i < n; i++)
        if (c[i]->metadataToken == 0 || c[i]->metadataToken != c[0]->metadataToken
            || c[i]->declaringType != c[0]->declaringType)
            dn2cpp_throw_ambiguous_match();
    return c[0];
}

// ---- reflection over an intrinsic type's metadata-answerable members ----
//
// An intrinsic type (CoreIntrinsics.s_intrinsicTypes) carries NO reflection method
// rows: its members are lowered inline at every call site, so no transpiled body
// exists for a row to point at and its ti_ reads (nullptr, 0). From the tables alone
// that is indistinguishable from a type that really has no methods — the same hole
// --trim-reflection has. Most of it cannot be closed at all: no image dn2cpp emits
// carries the dozens of methods real .NET reports on Unsafe/Int32/Math/String, so
// GetMethods() over an intrinsic type stays a known divergence.
//
// One family CAN be closed: a member whose RESULT IS A FUNCTION OF THE RUNTIME TYPE
// METADATA — of its type arguments, of its receiver's type, or of both. Such a member
// needs neither a compiled body nor a statically reached instantiation, since the
// answer is derivable from a Dn2CppTypeInfo at the moment reflection asks — precisely
// what an AOT image cannot do for an ordinary generic method. Two members qualify
// today: Unsafe.SizeOf<T>, reached through MakeGenericMethod with T known only at run
// time, and Object.MemberwiseClone, the shallow copy every reflective cloner is built
// on and the only route by which an array or a string is ever a receiver.
//
// The rows are synthesized on demand and interned, so handle identity, Equals and
// GetHashCode behave like a real row's. They are reachable only through a NAMED
// lookup (Type.GetMethod): GetMethods() deliberately does not list them, because a
// one-element member table for a type that really has dozens is a different wrong
// answer, where a named lookup answering the one member dn2cpp can compute is right.

#define DN2CPP_META_MAX_ARITY 4

// One metadata-answerable member. `answer` receives the closed row's type arguments
// (null for a non-generic member) and the receiver MethodInfo.Invoke was handed (null
// for a static one), and returns the boxed result Invoke hands back.
//
// `attrs` and `ilAttrs` are per-row rather than fixed because the second member is an
// INSTANCE, NON-PUBLIC one: they drive both the row the intern mints and the
// BindingFlags filter in dn2cpp_meta_lookup, so a row cannot be found under flags
// that disagree with the row it would hand back.
struct Dn2CppMetaMember
{
    const char* typeName;
    const char* methodName;
    int32_t genericArity;
    const Dn2CppTypeInfo* retType;
    int32_t attrs;   // DN2CPP_MTHA_* visibility/scope (METAANSWER/GENERIC are added by dn2cpp_meta_row)
    int32_t ilAttrs; // the MethodAttributes word MethodBase.Attributes reads
    Dn2CppObject* (*answer)(const Dn2CppTypeInfo* const* args, Dn2CppObject* receiver);
};

// The CLR FIELD-LAYOUT size of a type — what `sizeof(T)` is in IL and what
// Unsafe.SizeOf<T>() answers. It is NOT Dn2CppTypeInfo::instanceSize; three
// divergences must be corrected here:
//   - a REFERENCE type's value is the reference, so the size is pointer width;
//     instanceSize is the whole object's;
//   - dn2cpp boxes every sub-32-bit PRIMITIVE at the int32 model width, so
//     instanceSize reads 4 for Boolean/Char/Byte/SByte/Int16/UInt16 where the
//     layout widths are 1/2/1/1/2/2;
//   - an ENUM's ti carries that same model width whatever its underlying type,
//     so the underlying is what to read.
// For an emitter-generated struct the two coincide: instanceSize is sizeof() of the
// C++ struct the fields were laid out into.
//
// Do not add a per-type override HERE. This function is only ONE reader of the size,
// and the others cannot route through it: a statically lowered Unsafe.SizeOf<T> and an
// IL sizeof are emit-time C++ sizeof expressions, and the array/Unsafe.Add stride is
// pointer arithmetic on the real struct. An override would answer .NET's number to the
// reflected spelling of the question while the arithmetic kept stepping the
// representation's — a caller sizing a buffer from it corrupts memory silently. The
// only coherent fix for an intrinsic-represented value type whose C++ struct differs
// from .NET's layout is to change the representation (s_intrinsicStructExtents in
// CppEmitter.cs).
static int32_t dn2cpp_layout_size(const Dn2CppTypeInfo* ti)
{
    if ((ti->flags & DN2CPP_TF_VALUETYPE) == 0)
        return static_cast<int32_t>(sizeof(void*));
    if ((ti->flags & DN2CPP_TF_ENUM) != 0)
    {
        int32_t width = 4;
        bool isUnsigned = false;
        enum_underlying_shape(ti, &width, &isUnsigned);
        return width;
    }
    if (ti == &dn2cpp_bool_type || ti == &dn2cpp_byte_type || ti == &dn2cpp_sbyte_type)
        return 1;
    if (ti == &dn2cpp_char_type || ti == &dn2cpp_int16_type || ti == &dn2cpp_uint16_type)
        return 2;
    // An opaque shell whose extent was never modeled stamps 1, and there is no correct
    // answer to substitute. Throw rather than return it — 1 is also a field-less
    // struct's honest size, so the number cannot carry the difference.
    dn2cpp_require_layout(ti);
    return ti->instanceSize;
}

// Unsafe.SizeOf<T>().
static Dn2CppObject* dn2cpp_meta_unsafe_sizeof(const Dn2CppTypeInfo* const* args, Dn2CppObject* receiver)
{
    (void)receiver; // static
    int32_t n = dn2cpp_layout_size(args[0]);
    return dn2cpp_box(&dn2cpp_int32_type, &n, sizeof(int32_t));
}

// Object.MemberwiseClone(): the answer is a function of the RECEIVER's runtime type
// metadata, so it needs no compiled body. It is also the only route by which an array
// or a string can ever be the receiver — the method is `protected`, so a call site's
// receiver is always the declaring type's own `this`, and neither String nor an array
// type can be derived from. Without this row
// `typeof(object).GetMethod("MemberwiseClone", Instance | NonPublic)` answers null.
static Dn2CppObject* dn2cpp_meta_object_memberwise_clone(const Dn2CppTypeInfo* const* args,
                                                         Dn2CppObject* receiver)
{
    (void)args; // non-generic
    // A null receiver is a catchable NullReferenceException from the helper itself,
    // which is what an instance Invoke(null, …) raises on both runtimes.
    return dn2cpp_object_memberwise_clone(receiver);
}

// MethodAttributes: Public | Static | HideBySig (0x0016) and Family | HideBySig
// (0x0084) — the words MethodBase.Attributes and IsVirtual/IsAbstract/IsFinal read.
static const Dn2CppMetaMember g_meta_members[] = {
    { "System.Runtime.CompilerServices.Unsafe", "SizeOf", 1, &dn2cpp_int32_type,
      DN2CPP_MTHA_STATIC | DN2CPP_MTHA_PUBLIC, 0x0016, dn2cpp_meta_unsafe_sizeof },
    { "System.Object", "MemberwiseClone", 0, &dn2cpp_object_type,
      0 /* instance, non-public */, 0x0084, dn2cpp_meta_object_memberwise_clone },
};

// A synthesized row plus the descriptor it answers from. Rows are interned per
// (descriptor, declaring type-info, type arguments) so repeated lookups hand back
// one Dn2CppMethodInfo* — which is what makes the member-handle intern, and with it
// MemberInfo.Equals / GetHashCode / ReferenceEquals, answer stably. The list is walked
// linearly: its length is the number of distinct instantiations the program reflects
// over, not a function of the image.
//
// `declaring` is a type-info POINTER, so the key relies on one CLR type having exactly
// one type-info. This intern stays keyed per row alone because the ROW is
// reflected-type-independent (it is the declaring-type half); the reflected half rides
// the HANDLE intern, which is what makes typeof(D).GetMethod(m) and
// typeof(Base).GetMethod(m) unequal for an inherited m, as on .NET.
struct Dn2CppMetaRow
{
    Dn2CppMetaRow* next;
    const Dn2CppMetaMember* desc;
    Dn2CppMethodInfo row;
    const Dn2CppTypeInfo* args[DN2CPP_META_MAX_ARITY];
};

static Dn2CppMetaRow* g_meta_rows = nullptr;
static std::mutex g_meta_rows_mtx;

// The descriptor a synthesized row answers from, or null when `mi` is an ordinary
// emitted row. Identity is the row ADDRESS: every synthesized row lives inside a
// Dn2CppMetaRow that the intern list owns for the process lifetime.
static const Dn2CppMetaMember* dn2cpp_meta_desc_of(const Dn2CppMethodInfo* mi)
{
    std::lock_guard<std::mutex> lk(g_meta_rows_mtx);
    for (Dn2CppMetaRow* r = g_meta_rows; r != nullptr; r = r->next)
        if (&r->row == mi)
            return r->desc;
    return nullptr;
}

// The interned row for one (descriptor, declaring type, type arguments). `args ==
// nullptr` mints the OPEN definition row (genericArgs stays null, so Invoke lands
// on the InvalidOperationException real .NET raises for a late-bound call on a
// generic method definition).
static const Dn2CppMethodInfo* dn2cpp_meta_row(const Dn2CppMetaMember* d,
                                               const Dn2CppTypeInfo* declaring,
                                               const Dn2CppTypeInfo* const* args, int32_t argc)
{
    if (argc < 0 || argc > DN2CPP_META_MAX_ARITY)
        dn2cpp_throw_argument();
    std::lock_guard<std::mutex> lk(g_meta_rows_mtx);
    for (Dn2CppMetaRow* r = g_meta_rows; r != nullptr; r = r->next)
    {
        if (r->desc != d || r->row.declaringType != declaring
            || (r->row.genericArgs != nullptr) != (args != nullptr))
            continue;
        bool same = true;
        for (int32_t i = 0; args != nullptr && i < argc; i++)
            if (r->args[i] != args[i]) { same = false; break; }
        if (same)
            return &r->row;
    }
    auto* r = static_cast<Dn2CppMetaRow*>(dn2cpp_alloc_pinned(sizeof(Dn2CppMetaRow)));
    *r = Dn2CppMetaRow{};
    r->desc = d;
    r->row.name = d->methodName;
    r->row.declaringType = declaring;
    r->row.returnType = d->retType;
    r->row.attrs = d->attrs | DN2CPP_MTHA_METAANSWER
        | (d->genericArity > 0 ? DN2CPP_MTHA_GENERIC : 0);
    r->row.vtableSlot = -1;
    // metadataToken stays 0 (the row has no metadata), the same value every
    // hand-written row in the tree carries.
    r->row.ilAttrs = d->ilAttrs;
    r->row.genericParamCount = d->genericArity;
    if (args != nullptr)
    {
        for (int32_t i = 0; i < argc; i++)
            r->args[i] = args[i];
        r->row.genericArgs = r->args;
    }
    dn2cpp_gc_store_ref(&r->next, g_meta_rows);
    g_meta_rows = r;
    return &r->row;
}

// The named-lookup hook. Consulted only after the type's own rows produced no
// candidate, so a real row always wins and an image that did carry Unsafe's methods
// would not be shadowed.
//
// It walks the BASE CHAIN, like the caller it stands in for: Object.MemberwiseClone is
// an inherited member of every reference type, so `obj.GetType().GetMethod(
// "MemberwiseClone", Instance | NonPublic)` has to find it on Object — and the row it
// hands back must name Object as its DeclaringType, which is why `declaring` is the
// matched link rather than the queried type. DeclaredOnly stops the walk for the same
// reason it stops the real one.
//
// A GENERIC member answers the definition VIEW (real .NET's GetMethod("SizeOf") gives
// the open definition and MakeGenericMethod closes it); a non-generic row is already
// closed, so it answers the plain handle — a def-view there would be a MethodInfo that
// Invoke refuses.
static Dn2CppMethodRef* dn2cpp_meta_lookup(const Dn2CppTypeInfo* queried, Dn2CppString* name,
                                           int32_t genericParamCount,
                                           Dn2CppArrayRef* paramTypes, int32_t bindingFlags)
{
    for (const Dn2CppTypeInfo* ti = queried; ti != nullptr; ti = ti->base)
    {
        if (ti->name == nullptr)
            continue;
        bool inherited = (ti != queried);
        for (size_t k = 0; k < sizeof(g_meta_members) / sizeof(g_meta_members[0]); k++)
        {
            const Dn2CppMetaMember* d = &g_meta_members[k];
            if (std::strcmp(ti->name, d->typeName) != 0 || !dn2cpp_ascii_str_eq(d->methodName, name))
                continue;
            if (genericParamCount >= 0 && genericParamCount != d->genericArity)
                continue;
            // Every member modeled here is parameterless, so a Type[] filter naming
            // any parameter cannot match it.
            if (paramTypes != nullptr && paramTypes->length != 0)
                continue;
            if (!dn2cpp_member_matches(d->attrs, bindingFlags, inherited))
                continue;
            const Dn2CppMethodInfo* row = dn2cpp_meta_row(d, ti, nullptr, 0);
            return d->genericArity > 0 ? dn2cpp_make_methodref_defview(row, queried)
                                       : dn2cpp_make_methodref(row, queried);
        }
        if (bindingFlags & DN2CPP_BF_DECLAREDONLY)
            break;
    }
    return nullptr;
}

Dn2CppMethodRef* dn2cpp_type_get_method_full(Dn2CppType* t, Dn2CppString* name,
                                             int32_t genericParamCount, Dn2CppArrayRef* paramTypes,
                                             int32_t bindingFlags, int32_t callConv,
                                             Dn2CppObject* binder)
{
    (void)callConv; // observably a no-op filter in real .NET (see the note above)
    dn2cpp_reflection_check_binder(binder);
    dn2cpp_type_require(t);
    if (name == nullptr)
        dn2cpp_throw_argument_null();
    // Collect every candidate derived-first: BindingFlags + name + optional
    // generic-arity / parameter-type filters, with virtual-slot hiding (an
    // override hides its base definition; a `new` non-virtual keeps both and
    // is resolved by the sig-equality rule above).
    const Dn2CppMethodInfo* cands[64];
    int32_t n = 0;
    int32_t seen[256];
    int32_t seenCount = 0;
    for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr; ti = ti->base)
    {
        dn2cpp_require_metadata(ti);
        bool inherited = (ti != t->typeInfo);
        for (int32_t i = 0; i < ti->methodCount; i++)
        {
            const Dn2CppMethodInfo* mi = &ti->methods[i];
            if (!dn2cpp_member_matches(mi->attrs, bindingFlags, inherited))
                continue;
            bool hidden = false;
            if (mi->vtableSlot >= 0)
            {
                for (int32_t s = 0; s < seenCount; s++)
                    if (seen[s] == mi->vtableSlot) { hidden = true; break; }
                if (!hidden && seenCount < 256)
                    seen[seenCount++] = mi->vtableSlot;
            }
            if (hidden || !dn2cpp_ascii_str_eq(mi->name, name))
                continue;
            if (genericParamCount >= 0 && mi->genericParamCount != genericParamCount)
                continue;
            if (paramTypes != nullptr && !dn2cpp_params_match_types(mi, paramTypes))
                continue;
            if (n < 64)
                cands[n++] = mi;
        }
        if (bindingFlags & DN2CPP_BF_DECLAREDONLY)
            break;
    }
    if (n == 0)
        return dn2cpp_meta_lookup(t->typeInfo, name, genericParamCount, paramTypes, bindingFlags);
    return dn2cpp_make_methodref(dn2cpp_resolve_method_candidates(cands, n), t->typeInfo);
}

Dn2CppMethodRef* dn2cpp_type_get_method(Dn2CppType* t, Dn2CppString* name, int32_t bindingFlags)
{
    return dn2cpp_type_get_method_full(t, name, -1, nullptr, bindingFlags, 0, nullptr);
}

Dn2CppType* dn2cpp_methodref_return_type(Dn2CppMethodRef* m)
{
    return dn2cpp_get_type_from_handle(dn2cpp_methodref_require(m)->returnType);
}

int32_t dn2cpp_methodref_is_static(Dn2CppMethodRef* m)
{
    return (dn2cpp_methodref_require(m)->attrs & DN2CPP_MTHA_STATIC) != 0 ? 1 : 0;
}

int32_t dn2cpp_methodref_is_public(Dn2CppMethodRef* m)
{
    return (dn2cpp_methodref_require(m)->attrs & DN2CPP_MTHA_PUBLIC) != 0 ? 1 : 0;
}

int32_t dn2cpp_methodref_is_specialname(Dn2CppMethodRef* m)
{
    return (dn2cpp_methodref_require(m)->attrs & DN2CPP_MTHA_SPECIALNAME) != 0 ? 1 : 0;
}

Dn2CppArrayRef* dn2cpp_methodref_get_parameters(Dn2CppMethodRef* m)
{
    int32_t n = dn2cpp_methodref_require(m)->paramCount;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
    {
        auto* p = static_cast<Dn2CppParamRef*>(dn2cpp_alloc(sizeof(Dn2CppParamRef)));
        p->type = &dn2cpp_parameterinfo_type;
        p->param = &m->method->parameters[i];
        p->position = i;
        p->owner = m->method;
        p->ownerReflected = m->reflectedType;
        dn2cpp_gc_store_ref(&arr->data[i], reinterpret_cast<Dn2CppObject*>(p));
    }
    return arr;
}

// MethodInfo.ReturnParameter: a synthesized ParameterInfo over the return type
// (Position -1, Name null — matching real .NET's return pseudo-parameter).
Dn2CppObject* dn2cpp_methodref_return_parameter(Dn2CppMethodRef* m)
{
    auto* pi = static_cast<Dn2CppParamInfo*>(dn2cpp_alloc(sizeof(Dn2CppParamInfo)));
    *pi = Dn2CppParamInfo{};
    pi->paramType = dn2cpp_methodref_require(m)->returnType;
    auto* p = static_cast<Dn2CppParamRef*>(dn2cpp_alloc(sizeof(Dn2CppParamRef)));
    p->type = &dn2cpp_parameterinfo_type;
    p->param = pi;
    p->position = -1;
    p->owner = m->method;
    p->ownerReflected = m->reflectedType;
    return reinterpret_cast<Dn2CppObject*>(p);
}

Dn2CppType* dn2cpp_paramref_parameter_type(Dn2CppParamRef* p)
{
    return dn2cpp_get_type_from_handle(dn2cpp_paramref_require(p)->param->paramType);
}

int32_t dn2cpp_paramref_position(Dn2CppParamRef* p)
{
    return dn2cpp_paramref_require(p)->position;
}

Dn2CppString* dn2cpp_paramref_name(Dn2CppParamRef* p)
{
    const char* nm = dn2cpp_paramref_require(p)->param->name;
    if (nm == nullptr)
        return nullptr;
    return dn2cpp_string_from_utf8(nm, static_cast<int32_t>(std::strlen(nm)));
}

// Shared method dispatch: validate, adjust the receiver for a value-type
// instance method (pass the unboxed payload at obj+1), and call through the per-shape
// invoker thunk (which unboxes/casts the args, calls fnPtr, and boxes the result).
static Dn2CppObject* dn2cpp_invoke_mi(const Dn2CppMethodInfo* mi, Dn2CppObject* obj, Dn2CppObject** args, int32_t argc)
{
    // A metadata-answerable row carries no body at all: it answers from its own type
    // arguments and receiver. A non-generic row is always closed; a generic one is
    // closed only once MakeGenericMethod has filled genericArgs, and an OPEN
    // definition falls through to the InvalidOperationException real .NET raises for
    // a late-bound call on one.
    if (mi != nullptr && (mi->attrs & DN2CPP_MTHA_METAANSWER) != 0
        && (mi->genericParamCount == 0 || mi->genericArgs != nullptr))
    {
        if (argc != mi->paramCount)
            dn2cpp_throw_argument();
        const Dn2CppMetaMember* d = dn2cpp_meta_desc_of(mi);
        if (d != nullptr)
            return d->answer(mi->genericArgs, obj);
    }
    if (mi == nullptr || mi->invoker == nullptr)
        dn2cpp_throw_invalid_operation();
    void* fn = mi->fnPtr;
    // Late-bound call on an interface-declared row: the row is signature-only (an
    // interface method has no body, so fnPtr is null), but its invoker thunk was
    // emitted, and the receiver's implementation is what a callvirt would resolve —
    // same walk, same slot index, same ABI. The thunk passes the receiver unadjusted,
    // since the value-type adjustment below keys on the DECLARING type and an
    // interface is never a value type. A miss in the walk stays the walk's own loud
    // abort; a receiverless call falls through to the InvalidOperationException below.
    if (fn == nullptr && obj != nullptr && mi->vtableSlot >= 0
        && (mi->declaringType->flags & DN2CPP_TF_INTERFACE) != 0)
        fn = const_cast<void*>(dn2cpp_resolve_interface(obj->type, mi->declaringType)[mi->vtableSlot]);
    if (fn == nullptr)
        dn2cpp_throw_invalid_operation();
    if (argc != mi->paramCount)
        dn2cpp_throw_argument();
    bool isStatic = (mi->attrs & DN2CPP_MTHA_STATIC) != 0;
    Dn2CppObject* self = obj;
    if (!isStatic && obj != nullptr && (mi->declaringType->flags & DN2CPP_TF_VALUETYPE) != 0)
        self = reinterpret_cast<Dn2CppObject*>(reinterpret_cast<char*>(obj) + sizeof(Dn2CppObject));
    auto invoker = reinterpret_cast<Dn2CppObject* (*)(void*, Dn2CppObject*, Dn2CppObject**, const Dn2CppTypeInfo*)>(mi->invoker);
    return invoker(fn, self, args, mi->returnType);
}

// MethodInfo.Invoke.
Dn2CppObject* dn2cpp_methodref_invoke(Dn2CppMethodRef* m, Dn2CppObject* obj, Dn2CppArrayRef* args)
{
    return dn2cpp_invoke_mi(dn2cpp_methodref_require(m), obj, (args == nullptr) ? nullptr : args->data,
                            (args == nullptr) ? 0 : args->length);
}

// ---- reflection: CreateDelegate (the reflection -> delegate bridge) ----

// The bind node's header tag (identity only — never surfaced as a managed Type).
const Dn2CppTypeInfo dn2cpp_reflbind_type = { "<ReflectionDelegateBind>", nullptr, (int32_t)sizeof(Dn2CppReflBind), nullptr, nullptr, 0 };

// The boxed-invoker dispatch behind a dgrefl_* trampoline: same value-type
// receiver adjustment + invoker-thunk call as MethodInfo.Invoke. A null
// receiver on an instance binding (the null-bound closed delegate real .NET
// admits from the explicit-firstArgument overloads) fails loud.
Dn2CppObject* dn2cpp_reflbind_invoke(Dn2CppReflBind* ctx, Dn2CppObject* self, Dn2CppObject** argv)
{
    const Dn2CppMethodInfo* mi = ctx->method;
    if ((mi->attrs & DN2CPP_MTHA_STATIC) == 0 && self == nullptr)
        dn2cpp_throw_null_reference();
    return dn2cpp_invoke_mi(mi, self, argv, mi->paramCount);
}

static const Dn2CppMethodInfo* dn2cpp_delegate_invoke_row(const Dn2CppTypeInfo* ti)
{
    for (int32_t i = 0; i < ti->methodCount; i++)
        if (std::strcmp(ti->methods[i].name, "Invoke") == 0)
            return &ti->methods[i];
    return nullptr;
}

// Delegate-binding parameter compatibility: exact type-info identity, or a
// reference widening `from` -> `to` (argument contravariance / return
// covariance). Value types must match exactly — .NET delegate binding admits
// no boxing conversions either. (Divergence: .NET also admits enum <->
// underlying-primitive bindings; those are rejected here.)
static bool dn2cpp_dgbind_widens(const Dn2CppTypeInfo* from, const Dn2CppTypeInfo* to)
{
    if (from == to)
        return true;
    if (from == nullptr || to == nullptr)
        return false;
    if ((from->flags & DN2CPP_TF_VALUETYPE) != 0 || (to->flags & DN2CPP_TF_VALUETYPE) != 0)
        return false;
    return dn2cpp_type_is_assignable_from(dn2cpp_get_type_from_handle(to),
                                          dn2cpp_get_type_from_handle(from)) != 0;
}

Dn2CppObject* dn2cpp_delegate_create(Dn2CppType* dt, Dn2CppObject* target,
                                     Dn2CppMethodRef* m, int32_t closedForm,
                                     int32_t throwOnFailure)
{
    if (dt == nullptr || m == nullptr)
        dn2cpp_throw_argument_null();
    // A bind failure is ArgumentException ("Cannot bind to the target method"),
    // or null under the throwOnBindFailure: false overloads — matching .NET.
    auto fail = [&]() -> Dn2CppObject* {
        if (throwOnFailure != 0)
            dn2cpp_throw_argument();
        return nullptr;
    };
    const Dn2CppTypeInfo* dti = dt->typeInfo;
    if ((dti->flags & DN2CPP_TF_DELEGATE) == 0)
        dn2cpp_throw_argument();
    const Dn2CppMethodInfo* mi = m->method;
    // A definition view has no invokable body of its own; .NET rejects binding
    // an open generic method with ArgumentException.
    if (m->isGenericDefView != 0)
        return fail();
    const Dn2CppMethodInfo* inv = dn2cpp_delegate_invoke_row(dti);
    if (inv == nullptr)
        dn2cpp_throw_platform_not_supported(
            "CreateDelegate: the delegate type carries no reflected Invoke row in this image");
    void* tramp = nullptr;
    for (int32_t i = 0; i < dn2cpp_delegate_refl_registry_count; i++)
        if (dn2cpp_delegate_refl_registry[i].ti == dti)
        {
            tramp = dn2cpp_delegate_refl_registry[i].tramp;
            break;
        }
    if (tramp == nullptr)
        dn2cpp_throw_platform_not_supported(
            "CreateDelegate: the delegate type is not in this image's reflection-bind registry "
            "(AOT: only delegate types the transpile emitted can be bound)");
    if (mi->fnPtr == nullptr || mi->invoker == nullptr)
        dn2cpp_throw_platform_not_supported(
            "CreateDelegate: the target method's body was not compiled into this image");

    // Binding mode: .NET's shape rules. Open static / closed instance are the
    // classic forms; a delegate one parameter LONGER than an instance method is
    // open-instance (its first argument is the receiver); a delegate one
    // parameter SHORTER than a static method is closed-static (the explicit
    // firstArgument becomes the method's first parameter). The explicit-target
    // overloads (closedForm) admit a null-bound closed-instance delegate.
    bool mStatic = (mi->attrs & DN2CPP_MTHA_STATIC) != 0;
    int32_t dgArity = inv->paramCount;
    int32_t mode;
    if (mStatic)
    {
        if (dgArity == mi->paramCount && target == nullptr)
            mode = DN2CPP_DGBIND_OPEN_STATIC;
        else if (dgArity == mi->paramCount - 1 && closedForm != 0)
            mode = DN2CPP_DGBIND_CLOSED_STATIC;
        else
            return fail();
    }
    else
    {
        if (target != nullptr && dgArity == mi->paramCount)
            mode = DN2CPP_DGBIND_CLOSED_INSTANCE;
        else if (target == nullptr && dgArity == mi->paramCount + 1)
            mode = DN2CPP_DGBIND_OPEN_INSTANCE;
        else if (target == nullptr && dgArity == mi->paramCount && closedForm != 0)
            mode = DN2CPP_DGBIND_CLOSED_INSTANCE; // null-bound; invoking it faults
        else
            return fail();
    }

    const Dn2CppTypeInfo* declTi = mi->declaringType;
    if (mode == DN2CPP_DGBIND_CLOSED_INSTANCE && target != nullptr
        && target->type != declTi && !dn2cpp_dgbind_widens(target->type, declTi))
        return fail();
    if (mode == DN2CPP_DGBIND_OPEN_INSTANCE)
    {
        // A value-type receiver would need a byref first argument (real .NET's
        // open-instance-over-struct shape) — not modeled.
        if ((declTi->flags & DN2CPP_TF_VALUETYPE) != 0)
            dn2cpp_throw_platform_not_supported(
                "CreateDelegate: an open-instance delegate over a value-type receiver is not supported");
        if (!dn2cpp_dgbind_widens(inv->parameters[0].paramType, declTi))
            return fail();
    }
    if (mode == DN2CPP_DGBIND_CLOSED_STATIC)
    {
        // .NET's first-argument binding stores the bound object unconverted, so
        // the method's first parameter must be a reference type (a value-typed
        // first parameter is ArgumentException in real .NET too).
        const Dn2CppTypeInfo* p0 = mi->parameters[0].paramType;
        if ((p0->flags & DN2CPP_TF_VALUETYPE) != 0)
            return fail();
        if (target != nullptr && !dn2cpp_dgbind_widens(target->type, p0))
            return fail();
    }
    // Remaining parameters: delegate argument j feeds method parameter
    // (j - dgFirst) + mFirst; contravariant reference widening allowed.
    int32_t dgFirst = (mode == DN2CPP_DGBIND_OPEN_INSTANCE) ? 1 : 0;
    int32_t mFirst = (mode == DN2CPP_DGBIND_CLOSED_STATIC) ? 1 : 0;
    for (int32_t j = dgFirst; j < dgArity; j++)
        if (!dn2cpp_dgbind_widens(inv->parameters[j].paramType,
                                  mi->parameters[mFirst + (j - dgFirst)].paramType))
            return fail();
    // Return: covariant reference widening from the method's to the delegate's.
    if (!dn2cpp_dgbind_widens(mi->returnType, inv->returnType))
        return fail();

    auto* node = static_cast<Dn2CppReflBind*>(dn2cpp_alloc(sizeof(Dn2CppReflBind)));
    node->type = &dn2cpp_reflbind_type;
    node->method = mi;
    node->target = target;
    node->mode = mode;
    size_t sz = sizeof(Dn2CppDelegate);
    if (dti->instanceSize > 0 && static_cast<size_t>(dti->instanceSize) > sz)
        sz = static_cast<size_t>(dti->instanceSize);
    auto* dg = static_cast<Dn2CppDelegate*>(dn2cpp_alloc(sz));
    dg->type = dti;
    dg->target = reinterpret_cast<Dn2CppObject*>(node);
    dg->method = tramp;
    dg->prev = nullptr;
    return reinterpret_cast<Dn2CppObject*>(dg);
}

Dn2CppObject* dn2cpp_delegate_get_target(Dn2CppObject* d)
{
    if (d == nullptr)
        return nullptr;
    Dn2CppObject* t = reinterpret_cast<Dn2CppDelegate*>(d)->target;
    if (t != nullptr && t->type == &dn2cpp_reflbind_type)
        return reinterpret_cast<Dn2CppReflBind*>(t)->target;
    return t;
}

Dn2CppObject* dn2cpp_delegate_get_method(Dn2CppObject* d)
{
    if (d == nullptr)
        return nullptr;
    Dn2CppObject* t = reinterpret_cast<Dn2CppDelegate*>(d)->target;
    if (t != nullptr && t->type == &dn2cpp_reflbind_type)
        // Declaring-normalized (null): .NET's delegate.Method is the declaring-typed
        // instance even when the delegate was created from a derived-reflected row.
        return reinterpret_cast<Dn2CppObject*>(
            dn2cpp_make_methodref(reinterpret_cast<Dn2CppReflBind*>(t)->method, nullptr));
    // An IL-constructed delegate's method slot is a bare code address with no
    // metadata back-reference; callers null-propagate a missing MethodInfo.
    return nullptr;
}

// ---- reflection: constructors ----

// Allocates a fresh instance of mi->declaringType (a boxed payload for a value type),
// runs the ctor through its invoker thunk (instance, void return), and returns it.
static Dn2CppObject* dn2cpp_ctor_invoke_argv(const Dn2CppMethodInfo* mi, Dn2CppObject** args, int32_t argc)
{
    if (mi->invoker == nullptr || mi->fnPtr == nullptr)
        dn2cpp_throw_invalid_operation();
    if (argc != mi->paramCount)
        dn2cpp_throw_argument();
    const Dn2CppTypeInfo* ti = mi->declaringType;
    bool isValue = (ti->flags & DN2CPP_TF_VALUETYPE) != 0;
    size_t sz = isValue ? sizeof(Dn2CppObject) + static_cast<size_t>(ti->instanceSize)
                        : static_cast<size_t>(ti->instanceSize);
    auto* obj = static_cast<Dn2CppObject*>(dn2cpp_alloc(sz));
    obj->type = ti;
    // Mirror the emitted newobj path: a reflectively constructed instance of a
    // finalizable type must reach the finalizer queue too, and (like newobj)
    // it is registered before the ctor runs — an object is finalizable from
    // the moment it is allocated, so a throwing ctor still finalizes the
    // partially constructed instance. Value types never have finalizers.
    if (!isValue && ti->finalize != nullptr)
        dn2cpp_register_finalizer(obj);
    // For a value type the ctor's `this` is the unboxed payload (obj+1).
    Dn2CppObject* self = isValue
        ? reinterpret_cast<Dn2CppObject*>(reinterpret_cast<char*>(obj) + sizeof(Dn2CppObject))
        : obj;
    auto invoker = reinterpret_cast<Dn2CppObject* (*)(void*, Dn2CppObject*, Dn2CppObject**, const Dn2CppTypeInfo*)>(mi->invoker);
    invoker(mi->fnPtr, self, args, nullptr);
    return obj;
}

static Dn2CppObject* dn2cpp_ctor_invoke_impl(const Dn2CppMethodInfo* mi, Dn2CppArrayRef* args)
{
    return dn2cpp_ctor_invoke_argv(mi, (args == nullptr) ? nullptr : args->data,
                                   (args == nullptr) ? 0 : args->length);
}

// Collects a type's own constructors (no base walk — ctors are never inherited,
// so the queried type IS the declaring type and the reflected-type stamp below
// is the same pointer normalization would pick; passed for query-path uniformity).
static int32_t dn2cpp_collect_ctors(const Dn2CppTypeInfo* ti, int32_t flags, Dn2CppObject** out)
{
    int32_t n = 0;
    for (int32_t i = 0; i < ti->ctorCount; i++)
        if (dn2cpp_member_matches(ti->ctors[i].attrs, flags, false))
        {
            if (out != nullptr)
                out[n] = reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(&ti->ctors[i], ti));
            n++;
        }
    return n;
}

Dn2CppArrayRef* dn2cpp_type_get_constructors(Dn2CppType* t, int32_t bindingFlags)
{
    dn2cpp_type_require(t);
    int32_t n = dn2cpp_collect_ctors(t->typeInfo, bindingFlags, nullptr);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    dn2cpp_collect_ctors(t->typeInfo, bindingFlags, arr->data);
    return arr;
}

Dn2CppMethodRef* dn2cpp_type_get_constructor_full(Dn2CppType* t, Dn2CppArrayRef* paramTypes,
                                                  int32_t bindingFlags, int32_t callConv,
                                                  Dn2CppObject* binder)
{
    (void)callConv; // observably a no-op filter in real .NET (see the note above)
    dn2cpp_reflection_check_binder(binder);
    dn2cpp_type_require(t);
    int32_t want = (paramTypes == nullptr) ? 0 : paramTypes->length;
    const Dn2CppTypeInfo* ti = t->typeInfo;
    for (int32_t i = 0; i < ti->ctorCount; i++)
    {
        const Dn2CppMethodInfo* ci = &ti->ctors[i];
        if (!dn2cpp_member_matches(ci->attrs, bindingFlags, false) || ci->paramCount != want)
            continue;
        bool match = true;
        for (int32_t j = 0; j < want; j++)
        {
            auto* pt = reinterpret_cast<Dn2CppType*>(paramTypes->data[j]);
            if (pt == nullptr || ci->parameters[j].paramType != pt->typeInfo)
            {
                match = false;
                break;
            }
        }
        if (match)
            return dn2cpp_make_methodref(ci, ti);
    }
    return nullptr;
}

Dn2CppMethodRef* dn2cpp_type_get_constructor(Dn2CppType* t, Dn2CppArrayRef* paramTypes, int32_t bindingFlags)
{
    return dn2cpp_type_get_constructor_full(t, paramTypes, bindingFlags, 0, nullptr);
}

Dn2CppObject* dn2cpp_ctorref_invoke(Dn2CppMethodRef* c, Dn2CppArrayRef* args)
{
    return dn2cpp_ctor_invoke_impl(dn2cpp_methodref_require(c), args);
}

// Activator.CreateInstance(Type[, bool nonPublic]): the parameterless form.
// Matches real .NET's exceptions: an abstract class or interface throws
// MissingMethodException, and so does a type whose only
// parameterless ctor is non-public when nonPublic wasn't requested — the
// parameterless CreateInstance(Type) binds PUBLIC ctors only. A value type
// with no explicit parameterless ctor yields the zero-initialized boxed value.
Dn2CppObject* dn2cpp_activator_create_instance_nonpublic(Dn2CppType* t, int32_t nonPublic)
{
    if (t == nullptr)
        dn2cpp_throw_argument_null();
    const Dn2CppTypeInfo* ti = t->typeInfo;
    if ((ti->flags & (DN2CPP_TF_ABSTRACT | DN2CPP_TF_INTERFACE)) != 0)
        dn2cpp_throw_missing_method("Cannot create an instance of an abstract class or interface");
    for (int32_t i = 0; i < ti->ctorCount; i++)
        if (ti->ctors[i].paramCount == 0
            && (nonPublic != 0 || (ti->ctors[i].attrs & DN2CPP_MTHA_PUBLIC) != 0))
            return dn2cpp_ctor_invoke_impl(&ti->ctors[i], nullptr);
    // A value type with no explicit parameterless ctor: zero-initialized boxed value.
    if ((ti->flags & DN2CPP_TF_VALUETYPE) != 0)
    {
        auto* obj = static_cast<Dn2CppObject*>(
            dn2cpp_alloc(sizeof(Dn2CppObject) + static_cast<size_t>(ti->instanceSize)));
        obj->type = ti;
        return obj;
    }
    // No (visible) parameterless ctor — real .NET's MissingMethodException. This
    // also covers a ctor stripped from the image (an honest AOT miss).
    dn2cpp_throw_missing_method("No parameterless constructor defined for this type");
}

Dn2CppObject* dn2cpp_activator_create_instance(Dn2CppType* t)
{
    return dn2cpp_activator_create_instance_nonpublic(t, 0);
}

// ---- reflection: properties ----

static Dn2CppPropRef* dn2cpp_make_propref(const Dn2CppPropInfo* p,
                                          const Dn2CppTypeInfo* reflected)
{
    if (reflected == nullptr)
        reflected = p->declaringType;
    std::lock_guard<std::mutex> lk(g_memberref_intern_mtx);
    Dn2CppPropRef*& slot = g_propref_intern[{ p, reflected }];
    if (slot == nullptr)
    {
        auto* r = static_cast<Dn2CppPropRef*>(dn2cpp_alloc_pinned(sizeof(Dn2CppPropRef)));
        r->type = &dn2cpp_propertyinfo_type;
        r->prop = p;
        r->reflectedType = reflected;
        slot = r;
    }
    return slot;
}

// Walk the type + base chain (stopping at DeclaredOnly), collecting matching properties.
// A derived property hides a base property of the same name (most-derived wins); this
// matches .NET for an overridden/redeclared property in the non-hiding hierarchies we
// support (a `new` property that genuinely duplicates a base one is a carve-out).
static int32_t dn2cpp_collect_props(const Dn2CppTypeInfo* type, int32_t flags, Dn2CppObject** out)
{
    int32_t n = 0;
    const char* seen[256];
    int32_t seenCount = 0;
    for (const Dn2CppTypeInfo* ti = type; ti != nullptr; ti = ti->base)
    {
        dn2cpp_require_metadata(ti);
        bool inherited = (ti != type);
        for (int32_t i = 0; i < ti->propCount; i++)
        {
            const Dn2CppPropInfo* p = &ti->props[i];
            if (!dn2cpp_member_matches(p->attrs, flags, inherited))
                continue;
            bool hidden = false;
            for (int32_t s = 0; s < seenCount; s++)
                if (std::strcmp(seen[s], p->name) == 0) { hidden = true; break; }
            if (hidden)
                continue;
            if (seenCount < 256)
                seen[seenCount++] = p->name;
            if (out != nullptr)
                out[n] = reinterpret_cast<Dn2CppObject*>(dn2cpp_make_propref(p, type));
            n++;
        }
        if (flags & DN2CPP_BF_DECLAREDONLY)
            break;
    }
    return n;
}

Dn2CppArrayRef* dn2cpp_type_get_properties(Dn2CppType* t, int32_t bindingFlags)
{
    dn2cpp_type_require(t);
    int32_t n = dn2cpp_collect_props(t->typeInfo, bindingFlags, nullptr);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    dn2cpp_collect_props(t->typeInfo, bindingFlags, arr->data);
    return arr;
}

// A property's indexer parameters, read off its accessor rows (a PropInfo row
// carries no parameter table of its own): the getter's parameters, or the
// setter's minus the trailing value.
static int32_t dn2cpp_prop_index_count(const Dn2CppPropInfo* p)
{
    if (p->getter != nullptr)
        return p->getter->paramCount;
    if (p->setter != nullptr && p->setter->paramCount > 0)
        return p->setter->paramCount - 1;
    return 0;
}

static const Dn2CppTypeInfo* dn2cpp_prop_index_type(const Dn2CppPropInfo* p, int32_t i)
{
    return p->getter != nullptr ? p->getter->parameters[i].paramType
                                : p->setter->parameters[i].paramType;
}

// Whether two property rows have the same reflection signature (property type +
// indexer parameter list) — the equality behind .NET's most-derived-wins rule
// for an overridden/`new`-redeclared property.
static bool dn2cpp_props_sig_equal(const Dn2CppPropInfo* a, const Dn2CppPropInfo* b)
{
    if (a->propType != b->propType || dn2cpp_prop_index_count(a) != dn2cpp_prop_index_count(b))
        return false;
    for (int32_t i = 0; i < dn2cpp_prop_index_count(a); i++)
        if (dn2cpp_prop_index_type(a, i) != dn2cpp_prop_index_type(b, i))
            return false;
    return true;
}

Dn2CppPropRef* dn2cpp_type_get_property_full(Dn2CppType* t, Dn2CppString* name, int32_t bindingFlags,
                                             Dn2CppType* returnType, Dn2CppArrayRef* indexTypes,
                                             Dn2CppObject* binder)
{
    dn2cpp_reflection_check_binder(binder);
    dn2cpp_type_require(t);
    if (name == nullptr)
        dn2cpp_throw_argument_null();
    // Collect candidates derived-first (flags + name + optional propType /
    // indexer-signature filters), then resolve like the method lookup: one
    // match wins; sig-equal matches (override / `new` redeclaration chains)
    // resolve to the most derived; anything else throws AmbiguousMatchException
    // (e.g. GetProperty("Item") over overloaded indexers), matching real .NET.
    const Dn2CppPropInfo* cands[32];
    int32_t n = 0;
    for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr; ti = ti->base)
    {
        dn2cpp_require_metadata(ti);
        bool inherited = (ti != t->typeInfo);
        for (int32_t i = 0; i < ti->propCount; i++)
        {
            const Dn2CppPropInfo* p = &ti->props[i];
            if (!dn2cpp_member_matches(p->attrs, bindingFlags, inherited)
                || !dn2cpp_ascii_str_eq(p->name, name))
                continue;
            if (returnType != nullptr && p->propType != returnType->typeInfo)
                continue;
            if (indexTypes != nullptr)
            {
                if (dn2cpp_prop_index_count(p) != indexTypes->length)
                    continue;
                bool match = true;
                for (int32_t j = 0; j < indexTypes->length; j++)
                {
                    auto* pt = reinterpret_cast<Dn2CppType*>(indexTypes->data[j]);
                    if (pt == nullptr)
                        dn2cpp_throw_argument_null();
                    if (dn2cpp_prop_index_type(p, j) != pt->typeInfo) { match = false; break; }
                }
                if (!match)
                    continue;
            }
            if (n < 32)
                cands[n++] = p;
        }
        if (bindingFlags & DN2CPP_BF_DECLAREDONLY)
            break;
    }
    if (n == 0)
        return nullptr;
    for (int32_t i = 1; i < n; i++)
        if (!dn2cpp_props_sig_equal(cands[0], cands[i]))
            dn2cpp_throw_ambiguous_match();
    return dn2cpp_make_propref(cands[0], t->typeInfo);
}

Dn2CppPropRef* dn2cpp_type_get_property(Dn2CppType* t, Dn2CppString* name, int32_t bindingFlags)
{
    return dn2cpp_type_get_property_full(t, name, bindingFlags, nullptr, nullptr, nullptr);
}

Dn2CppType* dn2cpp_propref_property_type(Dn2CppPropRef* p)
{
    return dn2cpp_get_type_from_handle(dn2cpp_propref_require(p)->propType);
}

int32_t dn2cpp_propref_can_read(Dn2CppPropRef* p)
{
    return dn2cpp_propref_require(p)->getter != nullptr ? 1 : 0;
}

int32_t dn2cpp_propref_can_write(Dn2CppPropRef* p)
{
    return dn2cpp_propref_require(p)->setter != nullptr ? 1 : 0;
}

// PropertyInfo.GetGetMethod/GetSetMethod: wrap the accessor's method-table entry
// in a reflected handle; null when the property has no such accessor, or when it
// is non-public and nonPublic wasn't requested (the no-arg overloads pass 0).
Dn2CppMethodRef* dn2cpp_propref_accessor(Dn2CppPropRef* p, int32_t setter, int32_t nonPublic)
{
    const Dn2CppPropInfo* pi = dn2cpp_propref_require(p);
    const Dn2CppMethodInfo* m = setter != 0 ? pi->setter : pi->getter;
    if (m == nullptr)
        return nullptr;
    if (nonPublic == 0 && (m->attrs & DN2CPP_MTHA_PUBLIC) == 0)
        return nullptr;
    // The accessor inherits the PROPERTY handle's reflected type, so
    // typeof(D).GetProperty("P").GetGetMethod() is the same instance as
    // typeof(D).GetMethod("get_P"), as on .NET.
    return dn2cpp_make_methodref(m, p->reflectedType);
}

Dn2CppObject* dn2cpp_propref_get_value(Dn2CppPropRef* p, Dn2CppObject* obj)
{
    return dn2cpp_invoke_mi(dn2cpp_propref_require(p)->getter, obj, nullptr, 0);
}

void dn2cpp_propref_set_value(Dn2CppPropRef* p, Dn2CppObject* obj, Dn2CppObject* value)
{
    Dn2CppObject* args[1] = { value };
    dn2cpp_invoke_mi(dn2cpp_propref_require(p)->setter, obj, args, 1);
}

// PropertyInfo.GetIndexParameters(): the indexer parameters, read off the
// accessor rows exactly like dn2cpp_prop_index_count (the getter's parameters,
// or the setter's minus the trailing value). A non-indexed property yields an
// empty array, matching .NET.
Dn2CppArrayRef* dn2cpp_propref_get_index_parameters(Dn2CppPropRef* p)
{
    const Dn2CppPropInfo* pi = dn2cpp_propref_require(p);
    const Dn2CppMethodInfo* acc = pi->getter != nullptr ? pi->getter : pi->setter;
    int32_t n = dn2cpp_prop_index_count(p->prop);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
    {
        auto* pr = static_cast<Dn2CppParamRef*>(dn2cpp_alloc(sizeof(Dn2CppParamRef)));
        pr->type = &dn2cpp_parameterinfo_type;
        pr->param = &acc->parameters[i];
        pr->position = i;
        pr->owner = acc;
        pr->ownerReflected = p->reflectedType;
        dn2cpp_gc_store_ref(&arr->data[i], reinterpret_cast<Dn2CppObject*>(pr));
    }
    return arr;
}

// PropertyInfo.GetValue(obj, object[] index) / SetValue(obj, value, object[]
// index): the indexed forms, routed through the accessor rows' boxed invokers.
// A null/empty index behaves like the plain forms; an index-count mismatch
// surfaces as the invoker's argument-count ArgumentException (a documented
// divergence from .NET's TargetParameterCountException, same posture as
// MethodInfo.Invoke). The BindingFlags/Binder/CultureInfo forms route here too
// (a non-null Binder is the AOT PlatformNotSupportedException; culture is a
// no-op like real .NET's default binder on the non-parse path).
Dn2CppObject* dn2cpp_propref_get_value_indexed(Dn2CppPropRef* p, Dn2CppObject* obj,
                                               Dn2CppArrayRef* index, Dn2CppObject* binder)
{
    dn2cpp_reflection_check_binder(binder);
    return dn2cpp_invoke_mi(dn2cpp_propref_require(p)->getter, obj,
                            (index == nullptr) ? nullptr : index->data,
                            (index == nullptr) ? 0 : index->length);
}

void dn2cpp_propref_set_value_indexed(Dn2CppPropRef* p, Dn2CppObject* obj, Dn2CppObject* value,
                                      Dn2CppArrayRef* index, Dn2CppObject* binder)
{
    dn2cpp_reflection_check_binder(binder);
    int32_t n = (index == nullptr) ? 0 : index->length;
    // The setter's signature is (indices..., value): cap indexer arity at a
    // generous fixed bound rather than allocating (real indexers are tiny).
    if (n > 30)
        dn2cpp_throw_argument();
    Dn2CppObject* args[31];
    for (int32_t i = 0; i < n; i++)
        args[i] = index->data[i];
    args[n] = value;
    dn2cpp_invoke_mi(dn2cpp_propref_require(p)->setter, obj, args, n + 1);
}

// ---- reflection: mixed member lookup (GetMember/GetMembers/GetDefaultMembers) ----

// System.Reflection.MemberTypes bits.
#define DN2CPP_MT_CONSTRUCTOR 0x01
#define DN2CPP_MT_FIELD       0x04
#define DN2CPP_MT_METHOD      0x08
#define DN2CPP_MT_PROPERTY    0x10
#define DN2CPP_MT_TYPEINFO    0x20
#define DN2CPP_MT_NESTEDTYPE  0x80
#define DN2CPP_MT_ALL         0xBF

// One walk serving GetMember/GetMembers/GetDefaultMembers: every method, ctor,
// property, field and nested type matching (name pattern, MemberTypes set,
// BindingFlags), in that kind order. Methods use the virtual-slot hiding of
// GetMethods; properties the most-derived-name hiding of GetProperties; nested
// types are the type's own public set (never inherited). With out==nullptr only
// counts; otherwise fills out[] with the interned handles.
static int32_t dn2cpp_collect_member_matches(const Dn2CppTypeInfo* type, Dn2CppString* name,
                                             int32_t memberTypes, int32_t flags, Dn2CppObject** out)
{
    int32_t n = 0;
    if (memberTypes & DN2CPP_MT_METHOD)
    {
        int32_t seen[256];
        int32_t seenCount = 0;
        for (const Dn2CppTypeInfo* ti = type; ti != nullptr; ti = ti->base)
        {
            dn2cpp_require_metadata(ti);
            bool inherited = (ti != type);
            for (int32_t i = 0; i < ti->methodCount; i++)
            {
                const Dn2CppMethodInfo* mi = &ti->methods[i];
                if (!dn2cpp_member_matches(mi->attrs, flags, inherited))
                    continue;
                bool hidden = false;
                if (mi->vtableSlot >= 0)
                {
                    for (int32_t s = 0; s < seenCount; s++)
                        if (seen[s] == mi->vtableSlot) { hidden = true; break; }
                    if (!hidden && seenCount < 256)
                        seen[seenCount++] = mi->vtableSlot;
                }
                if (hidden || !dn2cpp_name_pattern_matches(mi->name, name))
                    continue;
                if (out != nullptr)
                    out[n] = reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(mi, type));
                n++;
            }
            if (flags & DN2CPP_BF_DECLAREDONLY)
                break;
        }
    }
    if (memberTypes & DN2CPP_MT_CONSTRUCTOR)
        for (int32_t i = 0; i < type->ctorCount; i++)
        {
            const Dn2CppMethodInfo* ci = &type->ctors[i];
            if (!dn2cpp_member_matches(ci->attrs, flags, false)
                || !dn2cpp_name_pattern_matches(ci->name, name))
                continue;
            if (out != nullptr)
                out[n] = reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(ci, type));
            n++;
        }
    if (memberTypes & DN2CPP_MT_PROPERTY)
    {
        // Hide by name + reflection signature (not by name alone): overloaded
        // indexers at one level all surface (GetMember("Item") reports every
        // overload, like real .NET), while an inherited property redeclared by
        // a derived level stays hidden by its most-derived row.
        const Dn2CppPropInfo* seenP[256];
        int32_t seenCount = 0;
        for (const Dn2CppTypeInfo* ti = type; ti != nullptr; ti = ti->base)
        {
            dn2cpp_require_metadata(ti);
            bool inherited = (ti != type);
            for (int32_t i = 0; i < ti->propCount; i++)
            {
                const Dn2CppPropInfo* p = &ti->props[i];
                if (!dn2cpp_member_matches(p->attrs, flags, inherited))
                    continue;
                bool hidden = false;
                for (int32_t s = 0; s < seenCount; s++)
                    if (std::strcmp(seenP[s]->name, p->name) == 0
                        && dn2cpp_props_sig_equal(seenP[s], p)) { hidden = true; break; }
                if (hidden)
                    continue;
                if (seenCount < 256)
                    seenP[seenCount++] = p;
                if (!dn2cpp_name_pattern_matches(p->name, name))
                    continue;
                if (out != nullptr)
                    out[n] = reinterpret_cast<Dn2CppObject*>(dn2cpp_make_propref(p, type));
                n++;
            }
            if (flags & DN2CPP_BF_DECLAREDONLY)
                break;
        }
    }
    if (memberTypes & DN2CPP_MT_FIELD)
        for (const Dn2CppTypeInfo* ti = type; ti != nullptr; ti = ti->base)
        {
            dn2cpp_require_metadata(ti);
            bool inherited = (ti != type);
            for (int32_t i = 0; i < ti->fieldCount; i++)
            {
                if (!dn2cpp_field_matches(&ti->fields[i], flags, inherited)
                    || !dn2cpp_name_pattern_matches(ti->fields[i].name, name))
                    continue;
                if (out != nullptr)
                    out[n] = reinterpret_cast<Dn2CppObject*>(
                        dn2cpp_make_fieldref(&ti->fields[i], type));
                n++;
            }
            if (flags & DN2CPP_BF_DECLAREDONLY)
                break;
        }
    if ((memberTypes & (DN2CPP_MT_NESTEDTYPE | DN2CPP_MT_TYPEINFO)) != 0
        && (flags & DN2CPP_BF_PUBLIC) != 0)
        for (int32_t i = 0; i < type->nestedCount; i++)
        {
            // The nested table holds the public emitted set; its entries carry the
            // full CLR reflection name ("Ns.Outer+Inner"), and .NET's GetMember
            // contract matches the pattern against the simple-name tail.
            if (!dn2cpp_name_pattern_matches(
                    dn2cpp_simple_type_name(type->nestedTypes[i]->name), name))
                continue;
            if (out != nullptr)
                out[n] = reinterpret_cast<Dn2CppObject*>(
                    dn2cpp_get_type_from_handle(type->nestedTypes[i]));
            n++;
        }
    return n;
}

Dn2CppArrayRef* dn2cpp_type_get_member(Dn2CppType* t, Dn2CppString* name,
                                       int32_t memberTypes, int32_t bindingFlags)
{
    dn2cpp_type_require(t);
    if (name == nullptr)
        dn2cpp_throw_argument_null();
    int32_t n = dn2cpp_collect_member_matches(t->typeInfo, name, memberTypes, bindingFlags, nullptr);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    dn2cpp_collect_member_matches(t->typeInfo, name, memberTypes, bindingFlags, arr->data);
    return arr;
}

Dn2CppArrayRef* dn2cpp_type_get_members(Dn2CppType* t, int32_t bindingFlags)
{
    dn2cpp_type_require(t);
    int32_t n = dn2cpp_collect_member_matches(t->typeInfo, nullptr, DN2CPP_MT_ALL, bindingFlags, nullptr);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    dn2cpp_collect_member_matches(t->typeInfo, nullptr, DN2CPP_MT_ALL, bindingFlags, arr->data);
    return arr;
}

// Type.GetDefaultMembers(): the members named by the type's [DefaultMember]
// attribute ("Item" for a type with an indexer). The name is stamped on the
// type-info at emit time (the attribute is a framework attribute, outside the
// reflected attribute tables); the base chain is walked like real .NET. A type
// without the attribute reports an empty array.
Dn2CppArrayRef* dn2cpp_type_get_default_members(Dn2CppType* t)
{
    dn2cpp_type_require(t);
    const char* nm = nullptr;
    for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr && nm == nullptr; ti = ti->base)
        nm = ti->defaultMemberName;
    if (nm == nullptr)
        return dn2cpp_newarr_ref(0);
    Dn2CppString* s = dn2cpp_string_from_utf8(nm, static_cast<int32_t>(std::strlen(nm)));
    return dn2cpp_type_get_member(t, s, DN2CPP_MT_ALL, 28);
}

// Type.GetMemberWithSameMetadataDefinitionAs(MemberInfo): the member of `t`
// wrapping the same metadata definition — the same (metadata token, defining
// assembly), so it crosses generic instantiations (List<int>'s Add row answers
// for List<string>'s). Rows with no recorded token (hand-written/synthetic)
// only match themselves. ArgumentNullException on a null member and
// ArgumentException when no member of `t` matches, like real .NET.
static bool dn2cpp_same_metadata_def(int32_t tokA, const Dn2CppTypeInfo* declA,
                                     int32_t tokB, const Dn2CppTypeInfo* declB)
{
    if (tokA == 0 || tokB == 0 || tokA != tokB)
        return false;
    const char* asmA = declA != nullptr && declA->assemblyName != nullptr
        ? declA->assemblyName : "System.Private.CoreLib";
    const char* asmB = declB != nullptr && declB->assemblyName != nullptr
        ? declB->assemblyName : "System.Private.CoreLib";
    return std::strcmp(asmA, asmB) == 0;
}

Dn2CppObject* dn2cpp_type_get_member_same_metadata(Dn2CppType* t, Dn2CppObject* member)
{
    dn2cpp_type_require(t);
    if (member == nullptr)
        dn2cpp_throw_argument_null();
    // A query path: the answer is a member OF `t`, so its reflected type is `t`
    // whatever base-chain level the row was found at, as on .NET.
    if (dn2cpp_is_methodref_header(member->type))
    {
        const Dn2CppMethodInfo* m = reinterpret_cast<Dn2CppMethodRef*>(member)->method;
        for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr; ti = ti->base)
        {
            dn2cpp_require_metadata(ti);
            for (int32_t i = 0; i < ti->methodCount; i++)
                if (&ti->methods[i] == m || dn2cpp_same_metadata_def(
                        ti->methods[i].metadataToken, ti,
                        m->metadataToken, m->declaringType))
                    return reinterpret_cast<Dn2CppObject*>(
                        dn2cpp_make_methodref(&ti->methods[i], t->typeInfo));
            for (int32_t i = 0; i < ti->ctorCount; i++)
                if (&ti->ctors[i] == m || dn2cpp_same_metadata_def(
                        ti->ctors[i].metadataToken, ti,
                        m->metadataToken, m->declaringType))
                    return reinterpret_cast<Dn2CppObject*>(
                        dn2cpp_make_methodref(&ti->ctors[i], t->typeInfo));
        }
    }
    else if (member->type == &dn2cpp_fieldinfo_type)
    {
        const Dn2CppFieldInfo* f = reinterpret_cast<Dn2CppFieldRef*>(member)->field;
        for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr; ti = ti->base)
        {
            dn2cpp_require_metadata(ti);
            for (int32_t i = 0; i < ti->fieldCount; i++)
                if (&ti->fields[i] == f || dn2cpp_same_metadata_def(
                        ti->fields[i].metadataToken, ti,
                        f->metadataToken, f->declaringType))
                    return reinterpret_cast<Dn2CppObject*>(
                        dn2cpp_make_fieldref(&ti->fields[i], t->typeInfo));
        }
    }
    else if (member->type == &dn2cpp_propertyinfo_type)
    {
        const Dn2CppPropInfo* p = reinterpret_cast<Dn2CppPropRef*>(member)->prop;
        for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr; ti = ti->base)
        {
            dn2cpp_require_metadata(ti);
            for (int32_t i = 0; i < ti->propCount; i++)
                if (&ti->props[i] == p || dn2cpp_same_metadata_def(
                        ti->props[i].metadataToken, ti,
                        p->metadataToken, p->declaringType))
                    return reinterpret_cast<Dn2CppObject*>(
                        dn2cpp_make_propref(&ti->props[i], t->typeInfo));
        }
    }
    else if (member->type == &dn2cpp_type_type)
    {
        const Dn2CppTypeInfo* mi = reinterpret_cast<Dn2CppType*>(member)->typeInfo;
        for (int32_t i = 0; i < t->typeInfo->nestedCount; i++)
        {
            const Dn2CppTypeInfo* nt = t->typeInfo->nestedTypes[i];
            if (nt == mi || dn2cpp_same_metadata_def(nt->metadataToken, nt,
                    mi->metadataToken, mi))
                return reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(nt));
        }
    }
    dn2cpp_throw_argument();
}

// ---- reflection: custom attributes ----

// The custom-attribute table of a reflected element handle, dispatched on the managed
// object header (a Type / FieldInfo / MethodInfo|ConstructorInfo / PropertyInfo /
// ParameterInfo). Returns nullptr/0 for an element with no reflected attributes.
static const Dn2CppAttrInfo* dn2cpp_member_custom_attrs(Dn2CppObject* m, int32_t* count)
{
    *count = 0;
    dn2cpp_memberref_require(m);
    if (m->type == &dn2cpp_type_type)
    {
        const Dn2CppTypeInfo* ti = reinterpret_cast<Dn2CppType*>(m)->typeInfo;
        *count = ti->customAttrCount;
        return ti->customAttrs;
    }
    if (m->type == &dn2cpp_fieldinfo_type)
    {
        const Dn2CppFieldInfo* f = reinterpret_cast<Dn2CppFieldRef*>(m)->field;
        *count = f->customAttrCount;
        return f->customAttrs;
    }
    if (dn2cpp_is_methodref_header(m->type))
    {
        const Dn2CppMethodInfo* mi = reinterpret_cast<Dn2CppMethodRef*>(m)->method;
        *count = mi->customAttrCount;
        return mi->customAttrs;
    }
    if (m->type == &dn2cpp_propertyinfo_type)
    {
        const Dn2CppPropInfo* p = reinterpret_cast<Dn2CppPropRef*>(m)->prop;
        *count = p->customAttrCount;
        return p->customAttrs;
    }
    if (m->type == &dn2cpp_parameterinfo_type)
    {
        const Dn2CppParamInfo* p = reinterpret_cast<Dn2CppParamRef*>(m)->param;
        *count = p->customAttrCount;
        return p->customAttrs;
    }
    return nullptr;
}

// True if an instance of `sub` is-a `super` (super is sub or one of its bases) — the
// GetCustomAttributes(attrType) filter, matching .NET's `is attrType` semantics.
static bool dn2cpp_type_is_a(const Dn2CppTypeInfo* sub, const Dn2CppTypeInfo* super)
{
    for (const Dn2CppTypeInfo* t = sub; t != nullptr; t = t->base)
        if (t == super)
            return true;
    return false;
}

// Materializes an attribute table as an object[] (each entry a fresh instance via
// its create factory), filtered to `filter` when non-null. Shared by the member
// element forms and the assembly forms below.
static Dn2CppArrayRef* dn2cpp_attr_table_to_array(const Dn2CppAttrInfo* tab, int32_t count,
                                                  const Dn2CppTypeInfo* filter)
{
    int32_t n = 0;
    for (int32_t i = 0; i < count; i++)
        if (filter == nullptr || dn2cpp_type_is_a(tab[i].attrType, filter))
            n++;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    // .NET's typed forms return an array whose RUNTIME type is attrType[], which is what
    // lets the caller cast/enumerate it as IEnumerable<attrType> (an `a is T[]` fast
    // path depends on it). Stamp the precise identity when the image carries the
    // element's ti_arr_ handle; a miss keeps the shared object[] handle, whose emitted
    // call-site retag then applies the static element. Independent of n: an EMPTY typed
    // result is attrType[]-typed in .NET too.
    if (filter != nullptr)
        if (const Dn2CppTypeInfo* arrTi = dn2cpp_find_array_ti(filter))
            reinterpret_cast<Dn2CppObject*>(arr)->type = arrTi;
    int32_t k = 0;
    for (int32_t i = 0; i < count; i++)
        if (filter == nullptr || dn2cpp_type_is_a(tab[i].attrType, filter))
            dn2cpp_gc_store_ref(&arr->data[k++], tab[i].create());
    return arr;
}

Dn2CppArrayRef* dn2cpp_get_custom_attributes(Dn2CppObject* member, Dn2CppType* attrType)
{
    int32_t count;
    const Dn2CppAttrInfo* tab = dn2cpp_member_custom_attrs(member, &count);
    const Dn2CppTypeInfo* filter = (attrType != nullptr) ? attrType->typeInfo : nullptr;
    return dn2cpp_attr_table_to_array(tab, count, filter);
}

// Typed-filter form: the caller always names an attribute type (GetCustomAttribute<T> /
// GetCustomAttributes(Type) / IsDefined(Type)). A null attrType here means the named type
// is not in the reachable set (its type-info was never emitted, so typeof() folded to a
// null token) — which, by tree-shaking, is exactly a type no reflected element carries.
// So a null filter matches *nothing* (not everything): the query is well-formed in .NET,
// the type simply isn't present on the element. This differs from the no-argument
// GetCustomAttributes() forms, where a null filter legitimately means "all".
Dn2CppArrayRef* dn2cpp_get_custom_attributes_typed(Dn2CppObject* member, Dn2CppType* attrType)
{
    if (attrType == nullptr)
        return dn2cpp_newarr_ref(0);
    return dn2cpp_get_custom_attributes(member, attrType);
}

Dn2CppObject* dn2cpp_get_custom_attribute(Dn2CppObject* member, Dn2CppType* attrType)
{
    if (attrType == nullptr)
        return nullptr; // named type absent from the reachable set -> no match
    const Dn2CppTypeInfo* filter = attrType->typeInfo;
    int32_t count;
    const Dn2CppAttrInfo* tab = dn2cpp_member_custom_attrs(member, &count);
    const Dn2CppAttrInfo* found = nullptr;
    for (int32_t i = 0; i < count; i++)
        if (dn2cpp_type_is_a(tab[i].attrType, filter))
        {
            if (found != nullptr)
                dn2cpp_throw_ambiguous_match();
            found = &tab[i];
        }
    return (found != nullptr) ? found->create() : nullptr;
}

int32_t dn2cpp_is_defined(Dn2CppObject* member, Dn2CppType* attrType)
{
    if (attrType == nullptr)
        return 0; // named type absent from the reachable set -> not defined
    const Dn2CppTypeInfo* filter = attrType->typeInfo;
    int32_t count;
    const Dn2CppAttrInfo* tab = dn2cpp_member_custom_attrs(member, &count);
    for (int32_t i = 0; i < count; i++)
        if (dn2cpp_type_is_a(tab[i].attrType, filter))
            return 1;
    return 0;
}

// ---- reflection: assembly-level custom attributes ----

// The assembly-registry attribute table for an Assembly handle (its simple name),
// resolved by name compare — the handle from Type.Assembly and the one from
// GetEntryAssembly may be distinct pointers to the same name. Null/unknown -> none.
static const Dn2CppAttrInfo* dn2cpp_assembly_custom_attrs(const char* name, int32_t* count)
{
    *count = 0;
    if (name == nullptr)
        return nullptr;
    for (int32_t i = 0; i < dn2cpp_assembly_registry_count; i++)
        if (std::strcmp(dn2cpp_assembly_registry[i].name, name) == 0)
        {
            *count = dn2cpp_assembly_registry[i].customAttrCount;
            return dn2cpp_assembly_registry[i].customAttrs;
        }
    return nullptr;
}

Dn2CppArrayRef* dn2cpp_assembly_get_custom_attributes(const char* name, Dn2CppType* attrType)
{
    int32_t count;
    const Dn2CppAttrInfo* tab = dn2cpp_assembly_custom_attrs(name, &count);
    const Dn2CppTypeInfo* filter = (attrType != nullptr) ? attrType->typeInfo : nullptr;
    return dn2cpp_attr_table_to_array(tab, count, filter);
}

// Typed-filter form: same null-attrType semantics as the member form above (a null
// filter means the named type isn't in the reachable set -> matches nothing).
Dn2CppArrayRef* dn2cpp_assembly_get_custom_attributes_typed(const char* name, Dn2CppType* attrType)
{
    if (attrType == nullptr)
        return dn2cpp_newarr_ref(0);
    return dn2cpp_assembly_get_custom_attributes(name, attrType);
}

Dn2CppObject* dn2cpp_assembly_get_custom_attribute(const char* name, Dn2CppType* attrType)
{
    if (attrType == nullptr)
        return nullptr; // named type absent from the reachable set -> no match
    const Dn2CppTypeInfo* filter = attrType->typeInfo;
    int32_t count;
    const Dn2CppAttrInfo* tab = dn2cpp_assembly_custom_attrs(name, &count);
    const Dn2CppAttrInfo* found = nullptr;
    for (int32_t i = 0; i < count; i++)
        if (dn2cpp_type_is_a(tab[i].attrType, filter))
        {
            if (found != nullptr)
                dn2cpp_throw_ambiguous_match();
            found = &tab[i];
        }
    return (found != nullptr) ? found->create() : nullptr;
}

int32_t dn2cpp_assembly_is_defined(const char* name, Dn2CppType* attrType)
{
    if (attrType == nullptr)
        return 0;
    const Dn2CppTypeInfo* filter = attrType->typeInfo;
    int32_t count;
    const Dn2CppAttrInfo* tab = dn2cpp_assembly_custom_attrs(name, &count);
    for (int32_t i = 0; i < count; i++)
        if (dn2cpp_type_is_a(tab[i].attrType, filter))
            return 1;
    return 0;
}

// ---- reflection: assembly identity (display name / AQN / GetType / modules) ----

// The registry row for an Assembly handle (null -> CoreLib, mirroring
// dn2cpp_type_assembly_name), or null when the name has no row (an assembly
// name synthesized outside the loaded module set).
static const Dn2CppAssemblyRegEntry* dn2cpp_assembly_reg_find(const char* name)
{
    if (name == nullptr)
        name = "System.Private.CoreLib";
    for (int32_t i = 0; i < dn2cpp_assembly_registry_count; i++)
        if (std::strcmp(dn2cpp_assembly_registry[i].name, name) == 0)
            return &dn2cpp_assembly_registry[i];
    return nullptr;
}

// Composes the .NET assembly display name "Name, Version=a.b.c.d, Culture=xx,
// PublicKeyToken=hhhh…" into a UTF-8 buffer. Missing registry pieces print the
// CLR defaults ("0.0.0.0" / "neutral" / "null"), matching what real .NET shows
// for an unversioned, culture-neutral, unsigned assembly.
const char* dn2cpp_assembly_display_name_utf8(const char* name)
{
    if (name == nullptr)
        name = "System.Private.CoreLib";
    const Dn2CppAssemblyRegEntry* e = dn2cpp_assembly_reg_find(name);
    const char* version = (e != nullptr && e->version != nullptr) ? e->version : "0.0.0.0";
    const char* culture = (e != nullptr && e->culture != nullptr) ? e->culture : "neutral";
    const char* pkt = (e != nullptr && e->publicKeyToken != nullptr) ? e->publicKeyToken : "null";
    size_t len = std::strlen(name) + std::strlen(version) + std::strlen(culture)
        + std::strlen(pkt) + sizeof(", Version=, Culture=, PublicKeyToken=");
    char* buf = static_cast<char*>(dn2cpp_alloc(len));
    std::snprintf(buf, len, "%s, Version=%s, Culture=%s, PublicKeyToken=%s",
        name, version, culture, pkt);
    return buf;
}

static Dn2CppString* dn2cpp_assembly_display_name(const char* name)
{
    const char* s = dn2cpp_assembly_display_name_utf8(name);
    return dn2cpp_string_from_utf8(s, static_cast<int32_t>(std::strlen(s)));
}

Dn2CppString* dn2cpp_assembly_full_name(const char* name)
{
    return dn2cpp_assembly_display_name(name);
}

// ---- Assembly/Module escaped into `object` ----------------------------------
// The managed types lower to the headerless simple-name `const char*`, so an
// escape into an `object` context allocates a real managed object around the
// handle (the Dn2CppNfiBox pattern). The box carries the PRIVATE implementation
// identity real .NET reports — RuntimeAssembly/RuntimeModule, each based on its
// public abstract shell — so GetType().Name says "RuntimeAssembly",
// GetType() == typeof(Assembly) is false and `o is Assembly` is true through the
// generic base-chain walk, exactly as on .NET. ToString dispatches to the same
// strings the statically-typed arms print: the assembly display FullName, the
// module file name.
struct Dn2CppAsmBox : Dn2CppObject
{
    const char* name;
};

static Dn2CppString* dn2cpp_asm_box_tostring(Dn2CppObject* o)
{
    return dn2cpp_assembly_full_name(reinterpret_cast<Dn2CppAsmBox*>(o)->name);
}

static Dn2CppString* dn2cpp_mod_box_tostring(Dn2CppObject* o)
{
    return dn2cpp_module_name(reinterpret_cast<Dn2CppAsmBox*>(o)->name);
}

// The abstract shells: instanceSize 0 is the "never an instance's header word"
// claim (like MemberInfo/MethodBase in dn2cpp_core.h's Dn2CppTypeInfo note).
const Dn2CppTypeInfo dn2cpp_assembly_type =
    { "System.Reflection.Assembly", &dn2cpp_object_type, 0, nullptr, nullptr, 0 };
const Dn2CppTypeInfo dn2cpp_module_type =
    { "System.Reflection.Module", &dn2cpp_object_type, 0, nullptr, nullptr, 0 };
const Dn2CppTypeInfo dn2cpp_runtime_assembly_type =
    { "System.Reflection.RuntimeAssembly", &dn2cpp_assembly_type,
      (int32_t)sizeof(Dn2CppAsmBox), nullptr, nullptr, 0, dn2cpp_asm_box_tostring };
const Dn2CppTypeInfo dn2cpp_runtime_module_type =
    { "System.Reflection.RuntimeModule", &dn2cpp_module_type,
      (int32_t)sizeof(Dn2CppAsmBox), nullptr, nullptr, 0, dn2cpp_mod_box_tostring };

// Interned per (assembly NAME, kind): by string compare, because two intrinsics
// can hand out two different `const char*`s for one assembly (a registry entry
// vs an emitted literal), and .NET's Assembly/Module instances are singletons
// reference-equal across every escape. The chain is GC-visible (nodes on the GC
// heap, head a scanned static slot). It doubles as the box-detection set: a box
// is recognized by chain MEMBERSHIP (pointer identity against the nodes), never
// by reading a header word — a raw `const char*` can be shorter than a pointer,
// so even a value-compare read of its first bytes would over-read.
struct Dn2CppAsmBoxNode
{
    Dn2CppAsmBox box;
    Dn2CppAsmBoxNode* next;
};
static DN2CPP_GC_STATIC_ROOT Dn2CppAsmBoxNode* g_asmBoxes = nullptr;
static std::mutex g_asmBoxMutex;

static Dn2CppAsmBox* dn2cpp_asm_box_of(const void* p)
{
    std::lock_guard<std::mutex> lock(g_asmBoxMutex);
    for (Dn2CppAsmBoxNode* n = g_asmBoxes; n != nullptr; n = n->next)
        if (&n->box == p)
            return &n->box;
    return nullptr;
}

Dn2CppObject* dn2cpp_asm_wrap(const char* h, int32_t kind)
{
    if (h == nullptr)
        return nullptr;
    // Already a wrapper (a value that crossed the boundary twice): pass through.
    if (Dn2CppAsmBox* b = dn2cpp_asm_box_of(h))
        return b;
    const Dn2CppTypeInfo* ti = kind == DN2CPP_ASM_KIND_MODULE
        ? &dn2cpp_runtime_module_type : &dn2cpp_runtime_assembly_type;
    std::lock_guard<std::mutex> lock(g_asmBoxMutex);
    for (Dn2CppAsmBoxNode* n = g_asmBoxes; n != nullptr; n = n->next)
        if (n->box.type == ti && std::strcmp(n->box.name, h) == 0)
            return &n->box;
    auto* node = static_cast<Dn2CppAsmBoxNode*>(dn2cpp_alloc(sizeof(Dn2CppAsmBoxNode)));
    node->box.type = ti;
    node->box.name = h;
    dn2cpp_gc_store_ref(&node->next, g_asmBoxes);
    g_asmBoxes = node;
    return &node->box;
}

const char* dn2cpp_asm_unwrap(Dn2CppObject* o)
{
    if (o == nullptr)
        return nullptr;
    if (Dn2CppAsmBox* b = dn2cpp_asm_box_of(o))
        return b->name;
    return reinterpret_cast<const char*>(o);
}

// castclass/isinst with an Assembly/Module target (the dn2cpp_nfi_castclass
// shape): a wrapper is matched by kind and unwrapped; anything else — including
// a wrapper of the OTHER kind — falls back to the generic walk against the
// target's type-info, which raises the proper InvalidCastException (a Module box
// cast to Assembly names RuntimeModule vs Assembly) or answers null.
const char* dn2cpp_asm_castclass(Dn2CppObject* o, int32_t kind,
                                 const Dn2CppTypeInfo* fallbackTi)
{
    if (o == nullptr)
        return nullptr;
    if (Dn2CppAsmBox* b = dn2cpp_asm_box_of(o))
        if ((b->type == &dn2cpp_runtime_assembly_type)
            == (kind == DN2CPP_ASM_KIND_ASSEMBLY))
            return b->name;
    return reinterpret_cast<const char*>(dn2cpp_castclass(o, fallbackTi));
}

const char* dn2cpp_asm_isinst(Dn2CppObject* o, int32_t kind,
                              const Dn2CppTypeInfo* fallbackTi)
{
    if (o == nullptr)
        return nullptr;
    if (Dn2CppAsmBox* b = dn2cpp_asm_box_of(o))
        return (b->type == &dn2cpp_runtime_assembly_type)
               == (kind == DN2CPP_ASM_KIND_ASSEMBLY)
            ? b->name : nullptr;
    return reinterpret_cast<const char*>(dn2cpp_isinst(o, fallbackTi));
}

// ---- reflection: embedded manifest resources ----

// A managed string as a UTF-8 std::string (the resource-table names are UTF-8 C
// strings, so the compare happens in that encoding).
static std::string dn2cpp_utf8_of(Dn2CppString* s)
{
    int32_t n = dn2cpp_string_to_utf8(s, nullptr, 0);
    std::string out(static_cast<size_t>(n > 0 ? n : 0), '\0');
    if (n > 0)
        dn2cpp_string_to_utf8(s, out.data(), n);
    return out;
}

// The resource name GetManifestResourceStream actually looks up. The (Type, string)
// overload scopes the name by the type's NAMESPACE ("Ns.name"); real .NET falls back
// to the bare name when the type has no namespace, and that is what a null/empty
// dn2cpp_type_namespace means here.
static std::string dn2cpp_manifest_resource_key(Dn2CppType* scope, Dn2CppString* resourceName)
{
    std::string key = dn2cpp_utf8_of(resourceName);
    if (scope == nullptr)
        return key;
    std::string ns = dn2cpp_utf8_of(dn2cpp_type_namespace(dn2cpp_type_require(scope)));
    if (ns.empty())
        return key;
    return ns + "." + key;
}

// DN2CPP_MANIFEST_TRACE: report every manifest-resource lookup. The keep-set a
// real game reads cannot be derived statically, so it is measured here.
static bool dn2cpp_manifest_trace_on()
{
    static const bool on = std::getenv("DN2CPP_MANIFEST_TRACE") != nullptr;
    return on;
}

// The named embedded resource of a registry entry, or null when it carries none.
static const Dn2CppManifestResource* dn2cpp_manifest_resource_find(
    const Dn2CppAssemblyRegEntry* e, const std::string& key)
{
    const Dn2CppManifestResource* hit = nullptr;
    if (e != nullptr && e->resources != nullptr)
        for (int32_t i = 0; i < e->resourceCount; i++)
            if (std::strcmp(e->resources[i].name, key.c_str()) == 0)
            {
                hit = &e->resources[i];
                break;
            }
    if (dn2cpp_manifest_trace_on())
        std::fprintf(stderr, "DN2CPP_MANIFEST_TRACE lookup asm=%s key=%s -> %s len=%d\n",
            (e != nullptr && e->name != nullptr) ? e->name : "<unregistered>", key.c_str(),
            hit != nullptr ? "hit" : "miss", hit != nullptr ? hit->length : -1);
    return hit;
}

// The resources of this assembly were dropped at transpile time
// (--no-manifest-resources), so a MISSED lookup cannot honestly answer the null/empty
// that means "no such resource" in real .NET — from the table alone a dropped resource
// and a genuinely absent one are indistinguishable. Throws a catchable
// PlatformNotSupportedException naming the assembly and both remedies (the
// dn2cpp_require_metadata pattern one level up). Callers test AFTER a lookup hit: a
// resource a --manifest-resource-root kept is still in the table and answers truthfully.
//
// `api` is the FULL member name including its declaring type ("Assembly.GetString",
// "ResourceManager.GetString"): the ResourceManager lookup reaches this same refusal
// through dn2cpp_assembly_require_manifest_resources, and a hardcoded "Assembly."
// prefix would name the wrong API in the one message whose job is telling somebody
// which read to root.
static void dn2cpp_require_manifest_resources(const Dn2CppAssemblyRegEntry* e, const char* api)
{
    if (e == nullptr || e->resourcesDropped == 0)
        return;
    char buf[512];
    std::snprintf(buf, sizeof(buf),
        "%s: the embedded manifest resources of assembly '%s' were dropped at "
        "transpile time (--no-manifest-resources %s). Transpile without that flag, or keep "
        "the resource this read wants with '--manifest-resource-root <manifest name>'.",
        api, e->name, e->name);
    dn2cpp_throw_platform_not_supported(buf);
}

Dn2CppArrayRef* dn2cpp_assembly_get_manifest_resource_names(const char* name)
{
    const Dn2CppAssemblyRegEntry* e = dn2cpp_assembly_reg_find(name);
    // A dropped assembly cannot enumerate honestly at all: even with roots kept the
    // list would be incomplete, so unlike the two lookups below this one throws on
    // the mere ask, not on a miss.
    dn2cpp_require_manifest_resources(e, "Assembly.GetManifestResourceNames");
    int32_t n = (e != nullptr && e->resources != nullptr) ? e->resourceCount : 0;
    if (dn2cpp_manifest_trace_on())
        std::fprintf(stderr, "DN2CPP_MANIFEST_TRACE names asm=%s -> %d\n",
            (e != nullptr && e->name != nullptr) ? e->name : "<unregistered>", n);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&arr->data[i], reinterpret_cast<Dn2CppObject*>(dn2cpp_string_from_utf8(
            e->resources[i].name, static_cast<int32_t>(std::strlen(e->resources[i].name)))));
    return arr;
}

Dn2CppArrayN* dn2cpp_assembly_get_manifest_resource_bytes(const char* name, Dn2CppType* scope,
    Dn2CppString* resourceName, const Dn2CppTypeInfo* byteArrayType)
{
    // .NET: a null name is ArgumentNullException, an empty one ArgumentException
    // ("String cannot have zero length"). The (Type, string) overload allows a null
    // NAME when the type supplies the whole key, so the null check is on the composed
    // key's source, not on the argument alone.
    if (resourceName == nullptr && scope == nullptr)
        dn2cpp_throw_argument_null();
    if (resourceName != nullptr && resourceName->length == 0)
        dn2cpp_throw_argument();
    const Dn2CppAssemblyRegEntry* e = dn2cpp_assembly_reg_find(name);
    const Dn2CppManifestResource* r =
        dn2cpp_manifest_resource_find(e, dn2cpp_manifest_resource_key(scope, resourceName));
    if (r == nullptr)
    {
        // Miss-path only: a kept (rooted) resource of a dropped assembly hit above
        // and answers; on a dropped assembly the null that would mean "no such
        // resource" is a lie, so the miss throws instead.
        dn2cpp_require_manifest_resources(e, "Assembly.GetManifestResourceStream");
        return nullptr;
    }
    // A byte[] copy rather than a view over .rodata: dn2cpp has no Stream over a raw
    // pointer (UnmanagedMemoryStream's real body is untranspilable), so the caller
    // wraps this in a read-only MemoryStream. The copy is also what makes the
    // resource immutable no matter what the caller does with the buffer.
    Dn2CppArrayN* arr = dn2cpp_newarr_n_atomic_t(r->length, 1, byteArrayType);
    if (r->length > 0)
        std::memcpy(arr->data, r->data, static_cast<size_t>(r->length));
    return arr;
}

int32_t dn2cpp_assembly_has_manifest_resource(const char* name, Dn2CppString* resourceName)
{
    if (resourceName == nullptr)
        dn2cpp_throw_argument_null();
    if (resourceName->length == 0)
        dn2cpp_throw_argument();
    const Dn2CppAssemblyRegEntry* e = dn2cpp_assembly_reg_find(name);
    if (dn2cpp_manifest_resource_find(e, dn2cpp_utf8_of(resourceName)) != nullptr)
        return 1;
    // Same miss-path posture as the stream read: null off a dropped assembly lies.
    dn2cpp_require_manifest_resources(e, "Assembly.GetManifestResourceInfo");
    return 0;
}

// The two exports the System.Resources unit reads (see dn2cpp_core.h): the same table
// walk and the same dropped-bit refusal, not a copy of either.
const Dn2CppManifestResource* dn2cpp_assembly_manifest_resource(const char* asmName,
    const char* key)
{
    return dn2cpp_manifest_resource_find(dn2cpp_assembly_reg_find(asmName), std::string(key));
}

void dn2cpp_assembly_require_manifest_resources(const char* asmName, const char* api)
{
    dn2cpp_require_manifest_resources(dn2cpp_assembly_reg_find(asmName), api);
}

const char* dn2cpp_assembly_neutral_resources_culture(const char* asmName,
    int32_t* satelliteOut)
{
    const Dn2CppAssemblyRegEntry* e = dn2cpp_assembly_reg_find(asmName);
    if (satelliteOut != nullptr)
        *satelliteOut = (e != nullptr) ? e->neutralResourcesSatellite : 0;
    return (e != nullptr) ? e->neutralResourcesCulture : nullptr;
}

Dn2CppString* dn2cpp_type_assembly_qualified_name(Dn2CppType* t)
{
    Dn2CppString* full = dn2cpp_type_fullname(dn2cpp_type_require(t));
    Dn2CppString* comma = dn2cpp_string_from_utf8(", ", 2);
    Dn2CppString* asmName = dn2cpp_assembly_display_name(dn2cpp_type_assembly_name(t));
    return dn2cpp_string_concat3(full, comma, asmName);
}

// Assembly.GetType(name[, throwOnError[, ignoreCase]]): the type-name registry
// restricted to the receiver assembly's own rows. Same name normalization as
// dn2cpp_type_get_by_name (cut at the first ','); ignoreCase folds ASCII case
// (CLR identifiers). Includes the dynamic side-chain like the global lookup —
// name-lookup surfaces see runtime-registered patch types.
Dn2CppType* dn2cpp_assembly_get_type(const char* asmName, Dn2CppString* name,
    int32_t throwOnError, int32_t ignoreCase)
{
    if (asmName == nullptr)
        asmName = "System.Private.CoreLib";
    if (name != nullptr)
    {
        int32_t len = name->length;
        for (int32_t i = 0; i < name->length; i++)
            if (name->chars[i] == u',') { len = i; break; }
        auto fold = [ignoreCase](char16_t c) {
            return (ignoreCase && c >= u'A' && c <= u'Z') ? (char16_t)(c + 32) : c;
        };
        auto matches = [name, len, fold](const char* cand) {
            int32_t j = 0;
            for (; j < len; j++)
                if (cand[j] == '\0'
                    || fold(static_cast<char16_t>(static_cast<unsigned char>(cand[j])))
                        != fold(name->chars[j]))
                    break;
            return j == len && cand[len] == '\0';
        };
        auto owned = [asmName](const Dn2CppTypeInfo* ti) {
            const char* owner = ti->assemblyName;
            if (owner == nullptr)
                owner = "System.Private.CoreLib";
            return std::strcmp(owner, asmName) == 0;
        };
        for (int32_t k = 0; k < dn2cpp_type_registry_count; k++)
            if (owned(dn2cpp_type_registry[k].type) && matches(dn2cpp_type_registry[k].name))
                return dn2cpp_get_type_from_handle(dn2cpp_type_registry[k].type);
        for (const Dn2CppDynTypeReg* e = dn2cpp_type_registry_dynamic_head(); e != nullptr; e = e->next)
            if (owned(e->type) && matches(e->name))
                return dn2cpp_get_type_from_handle(e->type);
        // A closed generic spelled the way FullName spells it matches no registry
        // name; resolve it structurally. Case-sensitive — the composed form comes
        // from FullName, never from a user's casing.
        if (const Dn2CppTypeInfo* ti = dn2cpp_resolve_managed_type_name(name))
            if (owned(ti))
                return dn2cpp_get_type_from_handle(ti);
    }
    if (throwOnError)
        dn2cpp_throw_type_load();
    return nullptr;
}

Dn2CppArrayRef* dn2cpp_assembly_get_modules(const char* name)
{
    if (name == nullptr)
        name = "System.Private.CoreLib";
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(1);
    arr->data[0] = reinterpret_cast<Dn2CppObject*>(const_cast<char*>(name));
    return arr;
}

// Assembly.Load(String/AssemblyName) / LoadWithPartialName(String): resolve a
// requested assembly name against the linked-assembly registry. An AOT image has every
// assembly it can ever "load" statically linked, so loading by name is a LOOKUP: the
// request's simple-name part (cut at the first ',', ASCII-whitespace-trimmed) matches a
// registry name ASCII case-insensitively, as CLR simple-name binding does. INTENTIONAL
// DIVERGENCE: a display name's Version/Culture/PublicKeyToken are ignored, where real
// .NET refuses a requested Version above the loaded one — the static image has exactly
// one candidate per name, and handing it back beats failing a "Type, Assembly"
// round-trip over the very assembly the caller is linked against. Null name ->
// ArgumentNullException, empty/blank -> ArgumentException. Returns the registry's own
// name pointer — the canonical Assembly handle, so op_Equality's pointer-or-strcmp
// compare holds against Type.Assembly / GetEntryAssembly handles.
static const char* dn2cpp_assembly_lookup_by_name(Dn2CppString* name)
{
    if (name == nullptr)
        dn2cpp_throw_argument_null();
    int32_t b = 0, e = name->length;
    for (int32_t i = 0; i < name->length; i++)
        if (name->chars[i] == u',') { e = i; break; }
    while (b < e && name->chars[b] <= u' ') b++;
    while (e > b && name->chars[e - 1] <= u' ') e--;
    if (b == e)
        dn2cpp_throw_argument();
    auto fold = [](char16_t c) {
        return (c >= u'A' && c <= u'Z') ? static_cast<char16_t>(c + 32) : c;
    };
    for (int32_t i = 0; i < dn2cpp_assembly_registry_count; i++)
    {
        const char* cand = dn2cpp_assembly_registry[i].name;
        int32_t j = 0;
        for (; j < e - b; j++)
            if (cand[j] == '\0'
                || fold(static_cast<char16_t>(static_cast<unsigned char>(cand[j])))
                    != fold(name->chars[b + j]))
                break;
        if (j == e - b && cand[j] == '\0')
            return cand;
    }
    return nullptr;
}

const char* dn2cpp_assembly_load(Dn2CppString* name)
{
    const char* found = dn2cpp_assembly_lookup_by_name(name);
    if (found == nullptr)
    {
        static constexpr char prefix[] = "Could not load file or assembly '";
        static constexpr char suffix[] =
            "': the AOT image's assembly set is fixed at transpile time and "
            "no linked assembly has this simple name.";
        dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_file_not_found_exception_type,
            dn2cpp_string_concat3(
                dn2cpp_string_from_utf8(prefix, static_cast<int32_t>(sizeof(prefix) - 1)),
                name,
                dn2cpp_string_from_utf8(suffix, static_cast<int32_t>(sizeof(suffix) - 1))),
            nullptr));
    }
    return found;
}

const char* dn2cpp_assembly_load_partial(Dn2CppString* name)
{
    // The obsolete partial-name form reports a miss as null, never a throw.
    return dn2cpp_assembly_lookup_by_name(name);
}

Dn2CppString* dn2cpp_module_name(const char* name)
{
    if (name == nullptr)
        name = "System.Private.CoreLib";
    size_t n = std::strlen(name);
    char* buf = static_cast<char*>(dn2cpp_alloc(n + sizeof(".dll")));
    std::memcpy(buf, name, n);
    std::memcpy(buf + n, ".dll", sizeof(".dll"));
    return dn2cpp_string_from_utf8(buf, static_cast<int32_t>(n + 4));
}

// ---- reflection: CustomAttributeData (MemberInfo.CustomAttributes) ----

// Materializes an attribute table as CustomAttributeData handles. Each handle
// wraps its Dn2CppAttrInfo row without instantiating the attribute (matching
// the declarative CustomAttributeData contract); only AttributeType is modeled.
static Dn2CppArrayRef* dn2cpp_attr_table_to_data_array(const Dn2CppAttrInfo* tab, int32_t count)
{
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(count);
    for (int32_t i = 0; i < count; i++)
    {
        auto* d = static_cast<Dn2CppAttrDataRef*>(dn2cpp_alloc(sizeof(Dn2CppAttrDataRef)));
        d->type = &dn2cpp_customattributedata_type;
        d->attr = &tab[i];
        dn2cpp_gc_store_ref(&arr->data[i], reinterpret_cast<Dn2CppObject*>(d));
    }
    return arr;
}

Dn2CppArrayRef* dn2cpp_member_custom_attributes_data(Dn2CppObject* member)
{
    int32_t count;
    const Dn2CppAttrInfo* tab = dn2cpp_member_custom_attrs(member, &count);
    return dn2cpp_attr_table_to_data_array(tab, count);
}

Dn2CppArrayRef* dn2cpp_assembly_custom_attributes_data(const char* name)
{
    int32_t count;
    const Dn2CppAttrInfo* tab = dn2cpp_assembly_custom_attrs(name, &count);
    return dn2cpp_attr_table_to_data_array(tab, count);
}

Dn2CppType* dn2cpp_attrdata_attribute_type(Dn2CppAttrDataRef* d)
{
    return dn2cpp_get_type_from_handle(dn2cpp_attrdataref_require(d)->attrType);
}

// ---- reflection long-tail: raw ECMA attribute words + Type completion ----

// ECMA-335 TypeAttributes bits consumed below (II.23.1.15).
#define DN2CPP_TA_VISMASK   0x7
#define DN2CPP_TA_PUBLIC    0x1
#define DN2CPP_TA_NESTEDPUB 0x2
#define DN2CPP_TA_INTERFACE 0x20
#define DN2CPP_TA_ABSTRACT  0x80
#define DN2CPP_TA_SEALED    0x100

int32_t dn2cpp_type_il_attrs(const Dn2CppTypeInfo* ti)
{
    // Presence test: emitted types always stamp their (nonzero) metadata token,
    // so a genuine all-zero TypeAttributes word (an internal class with a static
    // ctor) is still served exactly rather than falling to the synthesis.
    if (ti->ilAttrs != 0 || ti->metadataToken != 0)
        return static_cast<int32_t>(ti->ilAttrs);
    // Synthesized fallback for type-infos that predate the raw word (hand-written
    // runtime handles, array/generic-def synthetics): all are public BCL surface,
    // so fold the modeled flag bits into a best-effort word.
    int32_t a = DN2CPP_TA_PUBLIC;
    if ((ti->flags & DN2CPP_TF_SEALED) != 0)
        a |= DN2CPP_TA_SEALED;
    if ((ti->flags & DN2CPP_TF_INTERFACE) != 0)
        a |= DN2CPP_TA_INTERFACE;
    if ((ti->flags & DN2CPP_TF_ABSTRACT) != 0)
        a |= DN2CPP_TA_ABSTRACT;
    return a;
}

int32_t dn2cpp_type_is_public(const Dn2CppTypeInfo* ti)
{
    return (dn2cpp_type_il_attrs(ti) & DN2CPP_TA_VISMASK) == DN2CPP_TA_PUBLIC ? 1 : 0;
}

int32_t dn2cpp_type_is_not_public(const Dn2CppTypeInfo* ti)
{
    return (dn2cpp_type_il_attrs(ti) & DN2CPP_TA_VISMASK) == 0 ? 1 : 0;
}

// IsVisible: public top-level or nested-public. The enclosing chain is not
// walkable in this model, so a nested-public type inside a non-public enclosing
// type over-reports true (carve-out); generic arguments are not consulted.
int32_t dn2cpp_type_is_visible(const Dn2CppTypeInfo* ti)
{
    int32_t vis = dn2cpp_type_il_attrs(ti) & DN2CPP_TA_VISMASK;
    return (vis == DN2CPP_TA_PUBLIC || vis == DN2CPP_TA_NESTEDPUB) ? 1 : 0;
}

int32_t dn2cpp_type_is_nested_public(const Dn2CppTypeInfo* ti)
{
    return (dn2cpp_type_il_attrs(ti) & DN2CPP_TA_VISMASK) == DN2CPP_TA_NESTEDPUB ? 1 : 0;
}

// HasElementType/GetRootElementType: arrays are the only element-bearing Types
// that materialize at runtime here (no byref/pointer Type objects exist; the
// static typeof folds handle those in the transpiler).
int32_t dn2cpp_type_has_element_type(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_ARRAY) != 0 ? 1 : 0;
}

Dn2CppType* dn2cpp_type_get_root_element_type(Dn2CppType* t)
{
    const Dn2CppTypeInfo* ti = dn2cpp_type_require(t);
    while (ti->elementType != nullptr)
        ti = ti->elementType;
    return dn2cpp_get_type_from_handle(ti);
}

// Type.FormatTypeName (internal; the type rendering inside MethodBase.ToString):
// the simple name when the root element type is nested, otherwise the full name
// with the "System." prefix stripped for primitive/void roots — mirroring
// RuntimeType.FormatTypeName's legacy trim.
Dn2CppString* dn2cpp_type_format_type_name(Dn2CppType* t)
{
    const Dn2CppTypeInfo* root = dn2cpp_type_require(t);
    while (root->elementType != nullptr)
        root = root->elementType;
    if ((root->flags & DN2CPP_TF_NESTED) != 0)
        return dn2cpp_type_name(t->typeInfo);
    const char* full = t->typeInfo->name;
    if (((root->flags & DN2CPP_TF_PRIMITIVE) != 0 || root == &dn2cpp_void_type)
        && std::strncmp(full, "System.", 7) == 0)
        full += 7;
    return dn2cpp_string_from_utf8(full, static_cast<int32_t>(std::strlen(full)));
}

// Type.MakeArrayType(): resolve the SZ-array type-info for this element against
// the AOT image (registry scan over the emitted ti_arr_ handles). Like
// MakeGenericType, an array type never statically instantiated does not exist
// at runtime — NotSupportedException (the IL2CPP-style AOT boundary).
Dn2CppType* dn2cpp_type_make_array_type(Dn2CppType* t)
{
    dn2cpp_type_require(t);
    for (int32_t k = 0; k < dn2cpp_type_registry_count; k++)
    {
        const Dn2CppTypeInfo* cand = dn2cpp_type_registry[k].type;
        if ((cand->flags & DN2CPP_TF_ARRAY) != 0 && cand->elementType == t->typeInfo
            && cand->arrayRank == 1)
            return dn2cpp_get_type_from_handle(cand);
    }
    dn2cpp_throw_not_supported();
}

// The registry's array type-info over (elem, rank), or null when the image never
// emitted one (used to stamp precise array identity where available).
// External linkage (declared in dn2cpp_core.h): dn2cpp_array_ti in the Array
// unit resolves requests against the image's static registry through these.
// Rank > 1 rows exist because an SZArray-of-MDArray's elementType must be a linkable
// constant, so the emitter statically defines the MD type-info it names (ti_md_<T>)
// and registers it — the interner then answers with that same handle for every
// `new T[,]` of the shape, keeping the identity unique per (elem, rank).
const Dn2CppTypeInfo* dn2cpp_find_array_ti_rank(const Dn2CppTypeInfo* elem, int32_t rank)
{
    for (int32_t k = 0; k < dn2cpp_type_registry_count; k++)
    {
        const Dn2CppTypeInfo* cand = dn2cpp_type_registry[k].type;
        if ((cand->flags & DN2CPP_TF_ARRAY) != 0 && cand->elementType == elem
            && cand->arrayRank == rank)
            return cand;
    }
    return nullptr;
}

const Dn2CppTypeInfo* dn2cpp_find_array_ti(const Dn2CppTypeInfo* elem)
{
    return dn2cpp_find_array_ti_rank(elem, 1);
}

// Type.GetEnumValuesAsUnderlyingType(): the declared constants as an array of
// the underlying primitive, at its real width. int/uint use the packed int32
// array representation; the other widths use the element-sized representation
// — both retagged with the precise T[] identity when the image carries it.
Dn2CppObject* dn2cpp_type_get_enum_values_as_underlying(Dn2CppType* t)
{
    if ((dn2cpp_type_require(t)->flags & DN2CPP_TF_ENUM) == 0)
        dn2cpp_throw_argument();
    const Dn2CppTypeInfo* ti = t->typeInfo;
    const Dn2CppTypeInfo* u = ti->enumUnderlying != nullptr ? ti->enumUnderlying : &dn2cpp_int32_type;
    int32_t n = ti->enumMemberCount;
    const Dn2CppTypeInfo* arrTi = dn2cpp_find_array_ti(u);
    if (u == &dn2cpp_int32_type || u == &dn2cpp_uint32_type)
    {
        Dn2CppArrayI4* a = arrTi != nullptr ? dn2cpp_newarr_i4_t(n, arrTi) : dn2cpp_newarr_i4(n);
        for (int32_t i = 0; i < n; i++)
            a->data[i] = static_cast<int32_t>(ti->enumMembers[i].value);
        return reinterpret_cast<Dn2CppObject*>(a);
    }
    int32_t sz = (u == &dn2cpp_byte_type || u == &dn2cpp_sbyte_type) ? 1
        : (u == &dn2cpp_int16_type || u == &dn2cpp_uint16_type) ? 2
        : 8;
    Dn2CppArrayN* a = arrTi != nullptr ? dn2cpp_newarr_n_t(n, sz, arrTi) : dn2cpp_newarr_n(n, sz);
    for (int32_t i = 0; i < n; i++)
    {
        int64_t v = ti->enumMembers[i].value;
        char* at = a->data + static_cast<size_t>(i) * static_cast<size_t>(sz);
        if (sz == 1)
        {
            int8_t b = static_cast<int8_t>(v);
            std::memcpy(at, &b, 1);
        }
        else if (sz == 2)
        {
            int16_t s = static_cast<int16_t>(v);
            std::memcpy(at, &s, 2);
        }
        else
        {
            std::memcpy(at, &v, 8);
        }
    }
    return reinterpret_cast<Dn2CppObject*>(a);
}

// Type.FindInterfaces(TypeFilter filter, object criteria): the interface table
// filtered through the managed predicate. The delegate's method thunk uniformly
// takes the target as its first argument (the emitted delegate ABI), so a
// TypeFilter(Type, object) is a 3-pointer call returning int32.
Dn2CppArrayRef* dn2cpp_type_find_interfaces(Dn2CppType* t, Dn2CppObject* filter, Dn2CppObject* criteria)
{
    dn2cpp_type_require(t);
    if (filter == nullptr)
        dn2cpp_throw_argument_null();
    auto* d = reinterpret_cast<Dn2CppDelegate*>(filter);
    auto fn = reinterpret_cast<int32_t (*)(Dn2CppObject*, Dn2CppObject*, Dn2CppObject*)>(d->method);
    const Dn2CppTypeInfo* ti = t->typeInfo;
    // Exactly the row set dn2cpp_type_get_interfaces reports: the canonical alias
    // exclusion (a dispatch handle is not a CLR type, and the managed filter would be
    // asked about a type .NET's interface list never contains) and the shared
    // non-generic array rows both ride on the shared iterator.
    Dn2CppEffectiveItfs eff(ti);
    int32_t total = eff.Count();
    auto** keep = static_cast<Dn2CppObject**>(
        dn2cpp_alloc(sizeof(Dn2CppObject*) * (total > 0 ? static_cast<size_t>(total) : 1)));
    int32_t n = 0;
    eff.ForEach([&](const Dn2CppTypeInfo* row) {
        auto* itf = reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(row));
        if (fn(d->target, itf, criteria) != 0)
            keep[n++] = itf;
    });
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
        arr->data[i] = keep[i];
    return arr;
}

// Type.ImplementInterface (internal BCL helper behind TypeInfo.IsAssignableFrom):
// interface-set membership over the base chain's dispatch tables.
int32_t dn2cpp_type_implement_interface(Dn2CppType* t, Dn2CppType* itf)
{
    dn2cpp_type_require(t);
    if (itf == nullptr)
        return 0;
    const Dn2CppTypeInfo* target = itf->typeInfo;
    for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr; ti = ti->base)
        for (int32_t i = 0; i < ti->interfaceCount; i++)
            if (ti->interfaces[i].itf == target)
                return 1;
    return 0;
}

// MemberInfo.MemberType (System.Reflection.MemberTypes): constructors share the
// MethodRef representation, discriminated by the ".ctor" name.
int32_t dn2cpp_memberinfo_member_type(Dn2CppObject* m)
{
    dn2cpp_memberref_require(m);
    if (dn2cpp_is_methodref_header(m->type))
        return reinterpret_cast<Dn2CppMethodRef*>(m)->method->name[0] == '.' ? 0x01 : 0x08;
    if (m->type == &dn2cpp_fieldinfo_type)
        return 0x04;
    if (m->type == &dn2cpp_propertyinfo_type)
        return 0x10;
    if (m->type == &dn2cpp_type_type)
        return (reinterpret_cast<Dn2CppType*>(m)->typeInfo->flags & DN2CPP_TF_NESTED) != 0
            ? 0x80  // MemberTypes.NestedType
            : 0x20; // MemberTypes.TypeInfo
    return 0;
}

// MemberInfo.MetadataToken: the real ECMA token CppEmitter stamped on the row
// (0 for hand-written/synthetic entries, which carry no metadata).
int32_t dn2cpp_memberinfo_metadata_token(Dn2CppObject* m)
{
    dn2cpp_memberref_require(m);
    if (dn2cpp_is_methodref_header(m->type))
        return reinterpret_cast<Dn2CppMethodRef*>(m)->method->metadataToken;
    if (m->type == &dn2cpp_fieldinfo_type)
        return reinterpret_cast<Dn2CppFieldRef*>(m)->field->metadataToken;
    if (m->type == &dn2cpp_propertyinfo_type)
        return reinterpret_cast<Dn2CppPropRef*>(m)->prop->metadataToken;
    if (m->type == &dn2cpp_type_type)
        return reinterpret_cast<Dn2CppType*>(m)->typeInfo->metadataToken;
    return 0;
}

// MemberInfo.Module: modeled as the defining assembly's simple-name handle
// (each loaded assembly is single-module), the same handle Assembly is.
const char* dn2cpp_memberinfo_module(Dn2CppObject* m)
{
    dn2cpp_memberref_require(m);
    const Dn2CppTypeInfo* ti = nullptr;
    if (dn2cpp_is_methodref_header(m->type))
        ti = reinterpret_cast<Dn2CppMethodRef*>(m)->method->declaringType;
    else if (m->type == &dn2cpp_fieldinfo_type)
        ti = reinterpret_cast<Dn2CppFieldRef*>(m)->field->declaringType;
    else if (m->type == &dn2cpp_propertyinfo_type)
        ti = reinterpret_cast<Dn2CppPropRef*>(m)->prop->declaringType;
    else if (m->type == &dn2cpp_type_type)
        ti = reinterpret_cast<Dn2CppType*>(m)->typeInfo;
    const char* nm = ti != nullptr ? ti->assemblyName : nullptr;
    return nm != nullptr ? nm : "System.Private.CoreLib";
}

// ECMA-335 MethodAttributes bits consumed below (II.23.1.10).
#define DN2CPP_MA_FINAL    0x20
#define DN2CPP_MA_VIRTUAL  0x40
#define DN2CPP_MA_ABSTRACT 0x400

int32_t dn2cpp_methodref_attributes(Dn2CppMethodRef* m)
{
    return dn2cpp_methodref_require(m)->ilAttrs;
}

int32_t dn2cpp_methodref_impl_flags(Dn2CppMethodRef* m)
{
    return dn2cpp_methodref_require(m)->ilImplAttrs;
}

int32_t dn2cpp_methodref_is_virtual(Dn2CppMethodRef* m)
{
    return (dn2cpp_methodref_require(m)->ilAttrs & DN2CPP_MA_VIRTUAL) != 0 ? 1 : 0;
}

int32_t dn2cpp_methodref_is_abstract(Dn2CppMethodRef* m)
{
    return (dn2cpp_methodref_require(m)->ilAttrs & DN2CPP_MA_ABSTRACT) != 0 ? 1 : 0;
}

int32_t dn2cpp_methodref_is_final(Dn2CppMethodRef* m)
{
    return (dn2cpp_methodref_require(m)->ilAttrs & DN2CPP_MA_FINAL) != 0 ? 1 : 0;
}

// MethodBase.IsConstructor: an instance .ctor (the static .cctor is never a
// reflected row; see the emitter's ctor table filter).
int32_t dn2cpp_methodref_is_constructor(Dn2CppMethodRef* m)
{
    return std::strcmp(dn2cpp_methodref_require(m)->name, ".ctor") == 0
        && (m->method->attrs & DN2CPP_MTHA_STATIC) == 0 ? 1 : 0;
}

int32_t dn2cpp_methodref_is_generic(Dn2CppMethodRef* m)
{
    return (dn2cpp_methodref_require(m)->attrs & DN2CPP_MTHA_GENERIC) != 0 ? 1 : 0;
}

// ---- reflection: the generic-method dimension ----
// The image carries per-closed-instantiation rows only (no open definitions);
// see the Dn2CppMethodRef::isGenericDefView note for the definition-view model.

int32_t dn2cpp_methodref_is_generic_def(Dn2CppMethodRef* m)
{
    dn2cpp_methodref_require(m); // the null-handle guard; the flag lives on the handle
    return m->isGenericDefView;
}

// MethodBase.GetGenericArguments: the row's closed type arguments (empty for a
// non-generic method). A definition view reports the same closed arguments —
// the divergence documented on isGenericDefView (no open T handles exist).
Dn2CppArrayRef* dn2cpp_methodref_get_generic_arguments(Dn2CppMethodRef* m)
{
    const Dn2CppMethodInfo* mi = dn2cpp_methodref_require(m);
    int32_t n = (mi->genericArgs != nullptr) ? mi->genericParamCount : 0;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
        arr->data[i] = reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(mi->genericArgs[i]));
    return arr;
}

// MethodInfo.GetGenericMethodDefinition: the definition view over the same row.
// Matches .NET in throwing InvalidOperationException on a non-generic method.
// Declaring-normalized (null), NOT the receiver's reflected type: on .NET,
// typeof(D).GetMethod("GM").MakeGenericMethod(int).GetGenericMethodDefinition()
// .ReflectedType is Base, the declaring type.
Dn2CppMethodRef* dn2cpp_methodref_get_generic_definition(Dn2CppMethodRef* m)
{
    if ((dn2cpp_methodref_require(m)->attrs & DN2CPP_MTHA_GENERIC) == 0)
        dn2cpp_throw_invalid_operation();
    return dn2cpp_make_methodref_defview(m->method, nullptr);
}

// MethodInfo.MakeGenericMethod(Type[]): in-image resolution. Rows on the
// declaring type sharing the receiver's definition metadata token are the
// definition's compiled instantiations; match their closed arguments against
// the request. An instantiation the transpile never generated cannot be built
// at runtime (the IL2CPP AOT boundary, like MakeGenericType) — it throws a
// catchable PlatformNotSupportedException naming the missing instantiation.
Dn2CppMethodRef* dn2cpp_methodref_make_generic(Dn2CppMethodRef* m, Dn2CppArrayRef* types)
{
    const Dn2CppMethodInfo* def = dn2cpp_methodref_require(m);
    if ((def->attrs & DN2CPP_MTHA_GENERIC) == 0)
        dn2cpp_throw_invalid_operation();
    int32_t argc = (types == nullptr) ? 0 : types->length;
    // Arity mismatch is a caller error, matching .NET's ArgumentException.
    if (argc != def->genericParamCount)
        dn2cpp_throw_argument();
    for (int32_t i = 0; i < argc; i++)
        if (types->data[i] == nullptr)
            dn2cpp_throw_argument_null();
    // A metadata-answerable member has no compiled instantiations to resolve
    // against, and needs none: its answer is a function of the type argument, so
    // EVERY argument closes it — which is the whole point of the mechanism (Arch
    // asks for component types no static call site named).
    if ((def->attrs & DN2CPP_MTHA_METAANSWER) != 0)
    {
        const Dn2CppMetaMember* d = dn2cpp_meta_desc_of(def);
        if (d != nullptr)
        {
            const Dn2CppTypeInfo* ga[DN2CPP_META_MAX_ARITY] = {};
            if (argc > DN2CPP_META_MAX_ARITY)
                dn2cpp_throw_argument();
            for (int32_t i = 0; i < argc; i++)
                ga[i] = dn2cpp_type_require(reinterpret_cast<Dn2CppType*>(types->data[i]));
            // MakeGenericMethod PROPAGATES the receiver's reflected type, as .NET
            // does — here and in the in-image arm below.
            return dn2cpp_make_methodref(dn2cpp_meta_row(d, def->declaringType, ga, argc),
                                         m->reflectedType);
        }
    }
    const Dn2CppTypeInfo* ti = def->declaringType;
    for (int32_t k = 0; k < ti->methodCount; k++)
    {
        const Dn2CppMethodInfo* row = &ti->methods[k];
        if (row->metadataToken != def->metadataToken || row->metadataToken == 0
            || row->genericParamCount != argc || row->genericArgs == nullptr)
            continue;
        bool match = true;
        for (int32_t i = 0; i < argc; i++)
        {
            auto* a = reinterpret_cast<Dn2CppType*>(types->data[i]);
            if (a->typeInfo != row->genericArgs[i]) { match = false; break; }
        }
        if (match)
            return dn2cpp_make_methodref(row, m->reflectedType);
    }
    // Compose "Declaring.Name<Arg1,Arg2>" for the loud AOT-boundary message.
    char buf[512];
    int32_t pos = std::snprintf(buf, sizeof(buf),
        "MakeGenericMethod: no compiled instantiation %s.%s<", ti->name, def->name);
    for (int32_t i = 0; i < argc && pos > 0 && pos < static_cast<int32_t>(sizeof(buf)); i++)
    {
        auto* a = reinterpret_cast<Dn2CppType*>(types->data[i]);
        pos += std::snprintf(buf + pos, sizeof(buf) - static_cast<size_t>(pos), "%s%s",
                             i == 0 ? "" : ",", a->typeInfo->name);
    }
    if (pos > 0 && pos < static_cast<int32_t>(sizeof(buf)))
        std::snprintf(buf + pos, sizeof(buf) - static_cast<size_t>(pos),
                      "> in this image (AOT: only statically reached instantiations exist)");
    dn2cpp_throw_platform_not_supported(buf);
}

// MethodInfo.GetBaseDefinition: walk the base chain for the shallowest ancestor
// declaring a row on the same vtable slot (slot numbering is chain-consistent:
// a derived vtable extends its base, an override keeps the base's slot, and a
// `new` redeclaration gets a fresh slot — so slot equality is definition
// identity; the name check guards the interface-row slot numbering). A
// non-virtual method (slot -1) is its own base definition. A base whose member
// table was trimmed (unreached non-app-module rows) yields the deepest
// surviving declaration — best-effort, like the rest of the AOT surface.
Dn2CppMethodRef* dn2cpp_methodref_get_base_definition(Dn2CppMethodRef* m)
{
    const Dn2CppMethodInfo* mi = dn2cpp_methodref_require(m);
    if (mi->vtableSlot < 0)
        return m;
    const Dn2CppMethodInfo* best = mi;
    for (const Dn2CppTypeInfo* ti = mi->declaringType->base; ti != nullptr; ti = ti->base)
    {
        dn2cpp_require_metadata(ti);
        for (int32_t i = 0; i < ti->methodCount; i++)
            if (ti->methods[i].vtableSlot == mi->vtableSlot
                && std::strcmp(ti->methods[i].name, mi->name) == 0)
            {
                best = &ti->methods[i];
                break;
            }
    }
    // Declaring-normalized (null): .NET's GetBaseDefinition answers the base declaring
    // type's own instance, so a derived-reflected receiver does not propagate.
    // `best == mi` short-circuits only when the receiver IS declaring-reflected — a
    // derived-reflected handle to a method declared on its own type re-mints the
    // declaring-typed one below, which is .NET's answer there too.
    return best == mi && m->reflectedType == mi->declaringType
        ? m : dn2cpp_make_methodref(best, nullptr);
}

Dn2CppArrayRef* dn2cpp_methodref_get_parameter_types(Dn2CppMethodRef* m)
{
    int32_t n = dn2cpp_methodref_require(m)->paramCount;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&arr->data[i], reinterpret_cast<Dn2CppObject*>(
            dn2cpp_get_type_from_handle(m->method->parameters[i].paramType)));
    return arr;
}

// ECMA-335 FieldAttributes bits consumed below (II.23.1.5).
#define DN2CPP_FA_SPECIALNAME 0x200

int32_t dn2cpp_fieldref_attributes(Dn2CppFieldRef* f)
{
    return dn2cpp_fieldref_require(f)->ilAttrs;
}

int32_t dn2cpp_fieldref_is_private(Dn2CppFieldRef* f)
{
    return (dn2cpp_fieldref_require(f)->attrs & DN2CPP_FLDA_PRIVATE) != 0 ? 1 : 0;
}

int32_t dn2cpp_fieldref_is_specialname(Dn2CppFieldRef* f)
{
    return (dn2cpp_fieldref_require(f)->ilAttrs & DN2CPP_FA_SPECIALNAME) != 0 ? 1 : 0;
}

// ParameterInfo.Member: the declaring member's reflected handle (the owner row
// and its reflected type are stamped when the ParamRef is created, so the
// intern hands back the very instance GetParameters was called on —
// ReferenceEquals(mi.GetParameters()[0].Member, mi), like real .NET). A handle
// created without an owner (never the case for GetParameters results) fails loud.
Dn2CppObject* dn2cpp_paramref_member(Dn2CppParamRef* p)
{
    if (dn2cpp_paramref_require(p)->owner == nullptr)
        dn2cpp_throw_invalid_operation();
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(p->owner, p->ownerReflected));
}

int32_t dn2cpp_paramref_attributes(Dn2CppParamRef* p)
{
    return dn2cpp_paramref_require(p)->param->ilAttrs;
}

int32_t dn2cpp_paramref_is_optional(Dn2CppParamRef* p)
{
    return (dn2cpp_paramref_require(p)->param->ilAttrs & 0x10) != 0 ? 1 : 0; // ParameterAttributes.Optional
}

// Enum.InternalGetCorElementType: the CorElementType code of a boxed enum's
// underlying primitive, read from its type-info. The model default is I4.
int32_t dn2cpp_enum_cor_element_type(Dn2CppObject* e)
{
    dn2cpp_memberref_require(e);
    const Dn2CppTypeInfo* u = e->type != nullptr ? e->type->enumUnderlying : nullptr;
    if (u == &dn2cpp_bool_type) return 0x02;   // ELEMENT_TYPE_BOOLEAN
    if (u == &dn2cpp_char_type) return 0x03;   // ELEMENT_TYPE_CHAR
    if (u == &dn2cpp_sbyte_type) return 0x04;  // ELEMENT_TYPE_I1
    if (u == &dn2cpp_byte_type) return 0x05;   // ELEMENT_TYPE_U1
    if (u == &dn2cpp_int16_type) return 0x06;  // ELEMENT_TYPE_I2
    if (u == &dn2cpp_uint16_type) return 0x07; // ELEMENT_TYPE_U2
    if (u == &dn2cpp_uint32_type) return 0x09; // ELEMENT_TYPE_U4
    if (u == &dn2cpp_int64_type) return 0x0A;  // ELEMENT_TYPE_I8
    if (u == &dn2cpp_uint64_type) return 0x0B; // ELEMENT_TYPE_U8
    return 0x08;                               // ELEMENT_TYPE_I4
}

// Enum.GetValue(): boxes the enum's underlying primitive value. The result box's
// type-info is the UNDERLYING primitive's, not the enum's, matching real .NET — that is
// what Convert.ToXxx and the IConvertible.To* overrides read. The enum's own box carries
// its payload at the model width (4 bytes for every sub-64-bit underlying, 8 for
// Int64/UInt64) little-endian, so the low `byteWidth` bytes at (e + 1) are the value, and
// re-boxing them under `u` at that natural width reproduces what `box <underlying>`
// emits. The real IL switches on InternalGetCorElementType() (a bodyless FCall, leaf-cut
// in Compilation.Resolve), so the whole method is BodyReplace'd
// (CoreIntrinsics.BrEnumInstanceFormat) and that cut symbol never enters an emitted body.
Dn2CppObject* dn2cpp_enum_get_value(Dn2CppObject* e)
{
    dn2cpp_memberref_require(e);
    const Dn2CppTypeInfo* u = e->type != nullptr ? e->type->enumUnderlying : nullptr;
    int32_t byteWidth = 4;
    if (e->type != nullptr)
    {
        bool isUnsigned;
        enum_underlying_shape(e->type, &byteWidth, &isUnsigned);
    }
    if (u == nullptr)
        u = &dn2cpp_int32_type;
    return dn2cpp_box(u, e + 1, static_cast<size_t>(byteWidth));
}

// RuntimeHelpers.AllocateTypeAssociatedMemory(Type, int): zero-initialized,
// GC-scanned, never-collected memory (the lifetime of a type in an AOT image is
// the process), matching the static-storage allocation posture.
void* dn2cpp_alloc_type_associated(Dn2CppType* t, int32_t size)
{
    if (t == nullptr)
        dn2cpp_throw_argument();
    if (size < 0)
        dn2cpp_throw_argument_out_of_range();
    return dn2cpp_alloc_pinned(static_cast<size_t>(size));
}

// ---- reflection: boxed-value conversion (Array.SetValue / Activator argument binding) ----
// The element<->box cores and the primitive-code/width queries below have
// external linkage (declared in dn2cpp_core.h): the non-generic System.Array
// GetValue/SetValue/CreateInstance surface, which lives in
// dn2cpp_system_array.cpp, reads element slots through them. They stay in
// this unit beside the Activator argument binder, which shares the
// CanPrimitiveWiden matrix and the Nullable/enum layout rules.

// CorElementType-style primitive codes backing the CLR's CanPrimitiveWiden
// matrix. IntPtr/UIntPtr deliberately stay outside (exact-type-only, like a
// plain struct) — real .NET's widening table does not include them either.
#define DN2CPP_PC_NONE (-1)
#define DN2CPP_PC_BOOL 0
#define DN2CPP_PC_CHAR 1
#define DN2CPP_PC_I1   2
#define DN2CPP_PC_U1   3
#define DN2CPP_PC_I2   4
#define DN2CPP_PC_U2   5
#define DN2CPP_PC_I4   6
#define DN2CPP_PC_U4   7
#define DN2CPP_PC_I8   8
#define DN2CPP_PC_U8   9
#define DN2CPP_PC_R4   10
#define DN2CPP_PC_R8   11

int32_t dn2cpp_prim_code(const Dn2CppTypeInfo* ti)
{
    if (ti == &dn2cpp_bool_type) return DN2CPP_PC_BOOL;
    if (ti == &dn2cpp_char_type) return DN2CPP_PC_CHAR;
    if (ti == &dn2cpp_sbyte_type) return DN2CPP_PC_I1;
    if (ti == &dn2cpp_byte_type) return DN2CPP_PC_U1;
    if (ti == &dn2cpp_int16_type) return DN2CPP_PC_I2;
    if (ti == &dn2cpp_uint16_type) return DN2CPP_PC_U2;
    if (ti == &dn2cpp_int32_type) return DN2CPP_PC_I4;
    if (ti == &dn2cpp_uint32_type) return DN2CPP_PC_U4;
    if (ti == &dn2cpp_int64_type) return DN2CPP_PC_I8;
    if (ti == &dn2cpp_uint64_type) return DN2CPP_PC_U8;
    if (ti == &dn2cpp_single_type) return DN2CPP_PC_R4;
    if (ti == &dn2cpp_double_type) return DN2CPP_PC_R8;
    return DN2CPP_PC_NONE;
}

// The primitive's array-storage width (StorageOf — the packed element width,
// NOT the widened int32 box-payload width).
int32_t dn2cpp_prim_storage_width(int32_t code)
{
    switch (code)
    {
        case DN2CPP_PC_BOOL: case DN2CPP_PC_I1: case DN2CPP_PC_U1: return 1;
        case DN2CPP_PC_CHAR: case DN2CPP_PC_I2: case DN2CPP_PC_U2: return 2;
        case DN2CPP_PC_I4: case DN2CPP_PC_U4: case DN2CPP_PC_R4: return 4;
        default: return 8;
    }
}

// CanPrimitiveWiden(src -> dst), the exact CLR matrix: sbyte->ushort no,
// byte->char yes, uint->long yes, int->uint no, bool widens to nothing,
// long->float yes.
static bool dn2cpp_prim_widens(int32_t src, int32_t dst)
{
    if (src == dst)
        return true;
    switch (src)
    {
        case DN2CPP_PC_CHAR:
            return dst == DN2CPP_PC_U2 || dst == DN2CPP_PC_I4 || dst == DN2CPP_PC_U4
                || dst == DN2CPP_PC_I8 || dst == DN2CPP_PC_U8 || dst == DN2CPP_PC_R4 || dst == DN2CPP_PC_R8;
        case DN2CPP_PC_U1:
            return dst == DN2CPP_PC_CHAR || dst == DN2CPP_PC_I2 || dst == DN2CPP_PC_U2
                || dst == DN2CPP_PC_I4 || dst == DN2CPP_PC_U4 || dst == DN2CPP_PC_I8
                || dst == DN2CPP_PC_U8 || dst == DN2CPP_PC_R4 || dst == DN2CPP_PC_R8;
        case DN2CPP_PC_I1:
            return dst == DN2CPP_PC_I2 || dst == DN2CPP_PC_I4 || dst == DN2CPP_PC_I8
                || dst == DN2CPP_PC_R4 || dst == DN2CPP_PC_R8;
        case DN2CPP_PC_I2:
            return dst == DN2CPP_PC_I4 || dst == DN2CPP_PC_I8 || dst == DN2CPP_PC_R4 || dst == DN2CPP_PC_R8;
        case DN2CPP_PC_U2:
            return dst == DN2CPP_PC_CHAR || dst == DN2CPP_PC_I4 || dst == DN2CPP_PC_U4
                || dst == DN2CPP_PC_I8 || dst == DN2CPP_PC_U8 || dst == DN2CPP_PC_R4 || dst == DN2CPP_PC_R8;
        case DN2CPP_PC_I4:
            return dst == DN2CPP_PC_I8 || dst == DN2CPP_PC_R4 || dst == DN2CPP_PC_R8;
        case DN2CPP_PC_U4:
            return dst == DN2CPP_PC_I8 || dst == DN2CPP_PC_U8 || dst == DN2CPP_PC_R4 || dst == DN2CPP_PC_R8;
        case DN2CPP_PC_I8: case DN2CPP_PC_U8:
            return dst == DN2CPP_PC_R4 || dst == DN2CPP_PC_R8;
        case DN2CPP_PC_R4:
            return dst == DN2CPP_PC_R8;
        default: // BOOL widens to nothing; R8 is terminal
            return false;
    }
}

// A boxed numeric source: its primitive code + the payload read at the box
// convention widths (small ints/char/bool carry a 4-byte payload, 64-bit and
// floating types their natural width; a boxed enum reads as its underlying —
// 4-byte payload, or 8 for a 64-bit underlying). Returns false for a
// non-numeric source (string, struct, Nullable-erased boxes are their
// underlying already).
static bool dn2cpp_read_boxed_num(Dn2CppObject* v, int32_t* code, int64_t* i, double* r)
{
    const Dn2CppTypeInfo* ti = v->type;
    bool isEnum = (ti->flags & DN2CPP_TF_ENUM) != 0;
    const Dn2CppTypeInfo* eff = isEnum
        ? (ti->enumUnderlying != nullptr ? ti->enumUnderlying : &dn2cpp_int32_type)
        : ti;
    int32_t c = dn2cpp_prim_code(eff);
    if (c < 0)
        return false;
    const char* p = reinterpret_cast<const char*>(v + 1);
    *code = c;
    *i = 0;
    *r = 0.0;
    switch (c)
    {
        case DN2CPP_PC_I1: case DN2CPP_PC_I2: case DN2CPP_PC_I4:
        {
            int32_t x;
            std::memcpy(&x, p, 4);
            *i = x;
            break;
        }
        case DN2CPP_PC_BOOL: case DN2CPP_PC_CHAR: case DN2CPP_PC_U1:
        case DN2CPP_PC_U2: case DN2CPP_PC_U4:
        {
            uint32_t x;
            std::memcpy(&x, p, 4);
            *i = static_cast<int64_t>(x);
            break;
        }
        case DN2CPP_PC_I8: case DN2CPP_PC_U8:
            std::memcpy(i, p, 8);
            break;
        case DN2CPP_PC_R4:
        {
            float f;
            std::memcpy(&f, p, 4);
            *r = f;
            break;
        }
        default:
            std::memcpy(r, p, 8);
            break;
    }
    return true;
}

// Enum.ToObject(Type, object): the boxed-value form. .NET dispatches on
// Convert.GetTypeCode(value) — the eight integer widths plus Char and Boolean are
// accepted (a boxed enum reads as its underlying), everything else (float, double,
// string, an arbitrary object) throws ArgumentException; a null value throws
// ArgumentNullException. dn2cpp_read_boxed_num answers exactly that classification
// off the box's own type-info, so the integral read + the R4/R8 rejection ride one
// shared reader.
Dn2CppObject* dn2cpp_enum_to_object_boxed(Dn2CppType* t, Dn2CppObject* value)
{
    if (value == nullptr)
        dn2cpp_throw_argument_null();
    int32_t code;
    int64_t i;
    double r;
    if (!dn2cpp_read_boxed_num(value, &code, &i, &r)
        || code == DN2CPP_PC_R4 || code == DN2CPP_PC_R8)
        dn2cpp_throw_argument();
    return dn2cpp_enum_to_object(t, i);
}

// Write a numeric value (carried as int64 bits or double per the source code)
// into a `dst` slot of primitive code `dstCode` at its storage width. The
// caller has already verified dn2cpp_prim_widens(srcCode, dstCode).
static void dn2cpp_write_prim(void* dst, int32_t dstCode, int32_t srcCode, int64_t i, double r)
{
    bool srcFloat = srcCode == DN2CPP_PC_R4 || srcCode == DN2CPP_PC_R8;
    bool srcUnsigned = srcCode == DN2CPP_PC_U4 || srcCode == DN2CPP_PC_U8
        || srcCode == DN2CPP_PC_BOOL || srcCode == DN2CPP_PC_CHAR
        || srcCode == DN2CPP_PC_U1 || srcCode == DN2CPP_PC_U2;
    switch (dstCode)
    {
        case DN2CPP_PC_R8:
        {
            double d = srcFloat ? r : (srcUnsigned ? static_cast<double>(static_cast<uint64_t>(i))
                                                   : static_cast<double>(i));
            std::memcpy(dst, &d, 8);
            return;
        }
        case DN2CPP_PC_R4:
        {
            float f = srcFloat ? static_cast<float>(r)
                               : (srcUnsigned ? static_cast<float>(static_cast<uint64_t>(i))
                                              : static_cast<float>(i));
            std::memcpy(dst, &f, 4);
            return;
        }
        default:
        {
            // Integer target from an integer source (float sources never widen
            // into integer targets): store the low bits at the storage width.
            switch (dn2cpp_prim_storage_width(dstCode))
            {
                case 1: { uint8_t b = static_cast<uint8_t>(i); std::memcpy(dst, &b, 1); return; }
                case 2: { uint16_t h = static_cast<uint16_t>(i); std::memcpy(dst, &h, 2); return; }
                case 4: { uint32_t w = static_cast<uint32_t>(i); std::memcpy(dst, &w, 4); return; }
                default: { std::memcpy(dst, &i, 8); return; }
            }
        }
    }
}

// The Nullable<U> underlying type-info of `ti`, or null when ti is not a
// closed Nullable instantiation (same detection as Nullable.GetUnderlyingType).
static const Dn2CppTypeInfo* dn2cpp_nullable_underlying_ti(const Dn2CppTypeInfo* ti)
{
    if (ti != nullptr && ti->genericArgCount == 1 && ti->genericDef != nullptr
        && ti->genericArgs != nullptr && ti->genericDef->name != nullptr
        && std::strcmp(ti->genericDef->name, "System.Nullable`1") == 0)
        return ti->genericArgs[0];
    return nullptr;
}

// The transpiled Nullable<U> struct layout is { hasValue; U value; } with the
// value field at U's natural alignment. Supported for a primitive or enum U
// (alignment == storage width there); a struct U's alignment is not modeled.
// Returns false when unsupported.
static bool dn2cpp_nullable_layout(const Dn2CppTypeInfo* u, int32_t* valueOffset, int32_t* width, int32_t* uCode)
{
    const Dn2CppTypeInfo* eff = u;
    if ((u->flags & DN2CPP_TF_ENUM) != 0)
        eff = u->enumUnderlying != nullptr ? u->enumUnderlying : &dn2cpp_int32_type;
    int32_t c = dn2cpp_prim_code(eff);
    if (c < 0)
        return false;
    int32_t w = dn2cpp_prim_storage_width(c);
    *valueOffset = w; // hasValue byte, padded to the value's alignment
    *width = w;
    *uCode = c;
    return true;
}

[[noreturn]] static void dn2cpp_throw_array_store_mismatch()
{
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_invalid_cast_exception_type,
        dn2cpp_string_from_utf8("Object cannot be stored in an array of this type.", 49), nullptr));
}

// Convert the boxed `v` into the element representation of `elem` at `dst`
// (`elemSize` bytes) — real .NET's Array.SetValue coercion rules, matched exactly:
//   - a reference element checks assignability (InvalidCastException on miss);
//   - null zeroes a value element (default(T)), clears a Nullable, stores a
//     null reference;
//   - a Nullable<U> element takes exactly a boxed U (no widening);
//   - an enum element takes exactly its own enum type (int -> enum[], another
//     enum of the same underlying, and enum-width widenings all throw
//     InvalidCastException); a boxed enum SOURCE widens as its underlying
//     primitive into primitive targets (enum -> long[] works);
//   - a primitive element admits the CLR CanPrimitiveWiden conversions; a
//     primitive-to-primitive non-widening throws ArgumentException, any other
//     mismatch InvalidCastException;
//   - any other value type requires the exact type (bitwise copy).
void dn2cpp_array_store_boxed(Dn2CppObject* v, const Dn2CppTypeInfo* elem,
                              void* dst, int32_t elemSize, bool elemIsRef)
{
    if (elemIsRef)
    {
        if (v != nullptr
            && dn2cpp_type_is_assignable_from(dn2cpp_get_type_from_handle(elem),
                                              dn2cpp_get_type_from_handle(v->type)) == 0)
            dn2cpp_throw_array_store_mismatch();
        std::memcpy(dst, &v, sizeof(Dn2CppObject*));
        dn2cpp_gc_write_barrier_if_heap(dst);
        return;
    }
    if (v == nullptr)
    {
        std::memset(dst, 0, static_cast<size_t>(elemSize));
        dn2cpp_gc_write_barrier_if_heap(dst);
        return;
    }
    if (const Dn2CppTypeInfo* u = dn2cpp_nullable_underlying_ti(elem))
    {
        if (v->type != u)
            dn2cpp_throw_array_store_mismatch();
        int32_t off, w, uc;
        if (!dn2cpp_nullable_layout(u, &off, &w, &uc))
            dn2cpp_throw_platform_not_supported(
                "Array.SetValue into a Nullable<T> element of a non-primitive T is not supported");
        std::memset(dst, 0, static_cast<size_t>(elemSize));
        *static_cast<uint8_t*>(dst) = 1;
        int32_t sc;
        int64_t i;
        double r;
        dn2cpp_read_boxed_num(v, &sc, &i, &r);
        dn2cpp_write_prim(static_cast<char*>(dst) + off, uc, sc, i, r);
        dn2cpp_gc_write_barrier_if_heap(dst);
        return;
    }
    if ((elem->flags & DN2CPP_TF_ENUM) != 0)
    {
        if (v->type != elem)
            dn2cpp_throw_array_store_mismatch();
        int32_t sc;
        int64_t i;
        double r;
        dn2cpp_read_boxed_num(v, &sc, &i, &r);
        dn2cpp_write_prim(dst, sc, sc, i, r);
        dn2cpp_gc_write_barrier_if_heap(dst);
        return;
    }
    int32_t dc = dn2cpp_prim_code(elem);
    if (dc >= 0)
    {
        int32_t sc;
        int64_t i;
        double r;
        if (!dn2cpp_read_boxed_num(v, &sc, &i, &r))
            dn2cpp_throw_array_store_mismatch();
        if (!dn2cpp_prim_widens(sc, dc))
        {
            // Both sides primitive, no widening: ArgumentException ("Cannot
            // widen from source type to target type…"), matching real .NET.
            dn2cpp_throw_argument();
        }
        dn2cpp_write_prim(dst, dc, sc, i, r);
        dn2cpp_gc_write_barrier_if_heap(dst);
        return;
    }
    // Any other value type (struct / IntPtr / decimal / …): exact type, bitwise.
    if (v->type != elem)
        dn2cpp_throw_array_store_mismatch();
    std::memcpy(dst, v + 1, static_cast<size_t>(elemSize));
    dn2cpp_gc_write_barrier_if_heap(dst);
}

// Box the element of `elem` stored at `src` (`elemSize` bytes) — the
// Array.GetValue side. A reference element loads the pointer; a Nullable<U>
// element yields the boxed U or null; an enum/primitive element widens its
// packed storage to the box-payload convention (4-byte small ints, natural
// 8-byte wides); any other value type boxes bitwise under its exact type-info.
Dn2CppObject* dn2cpp_array_box_element(const Dn2CppTypeInfo* elem, const void* src,
                                       int32_t elemSize, bool elemIsRef)
{
    if (elemIsRef)
    {
        Dn2CppObject* v;
        std::memcpy(&v, src, sizeof(Dn2CppObject*));
        return v;
    }
    if (const Dn2CppTypeInfo* u = dn2cpp_nullable_underlying_ti(elem))
    {
        int32_t off, w, uc;
        if (!dn2cpp_nullable_layout(u, &off, &w, &uc))
            dn2cpp_throw_platform_not_supported(
                "Array.GetValue from a Nullable<T> element of a non-primitive T is not supported");
        if (*static_cast<const uint8_t*>(src) == 0)
            return nullptr;
        return dn2cpp_array_box_element(u, static_cast<const char*>(src) + off, w, false);
    }
    bool isEnum = (elem->flags & DN2CPP_TF_ENUM) != 0;
    const Dn2CppTypeInfo* eff = isEnum
        ? (elem->enumUnderlying != nullptr ? elem->enumUnderlying : &dn2cpp_int32_type)
        : elem;
    int32_t c = dn2cpp_prim_code(eff);
    if (c < 0)
        return dn2cpp_box(elem, src, static_cast<size_t>(elemSize));
    int32_t w = dn2cpp_prim_storage_width(c);
    // Widen the packed storage to the box-payload width (sign-extend the
    // signed narrow types, zero-extend the unsigned/char/bool ones).
    if (w >= 4)
        return dn2cpp_box(elem, src, static_cast<size_t>(w));
    int32_t payload;
    switch (c)
    {
        case DN2CPP_PC_I1: { int8_t x; std::memcpy(&x, src, 1); payload = x; break; }
        case DN2CPP_PC_I2: { int16_t x; std::memcpy(&x, src, 2); payload = x; break; }
        case DN2CPP_PC_BOOL: case DN2CPP_PC_U1: { uint8_t x; std::memcpy(&x, src, 1); payload = x; break; }
        default: { uint16_t x; std::memcpy(&x, src, 2); payload = x; break; }
    }
    return dn2cpp_box(elem, &payload, 4);
}

// ---- Array.Copy type compatibility ----

// GetNormalizedIntegralArrayElementType: unsigned reads as signed of the width.
// CHAR and BOOL deliberately normalize only to themselves, matching .NET:
// ushort[] <-> short[] copies raw and char[] <-> ushort[] goes through the
// widening arm, while short[] -> char[] refuses.
static int32_t dn2cpp_prim_norm(int32_t code)
{
    switch (code)
    {
        case DN2CPP_PC_U1: return DN2CPP_PC_I1;
        case DN2CPP_PC_U2: return DN2CPP_PC_I2;
        case DN2CPP_PC_U4: return DN2CPP_PC_I4;
        case DN2CPP_PC_U8: return DN2CPP_PC_I8;
        default: return code;
    }
}

// Read a primitive element at its packed storage width into the int64/double
// carriers dn2cpp_write_prim consumes (signed narrows sign-extend, unsigned/
// char/bool zero-extend — the convention dn2cpp_read_boxed_num reads a box with).
static void dn2cpp_read_prim_storage(const void* p, int32_t code, int64_t* i, double* r)
{
    *i = 0;
    *r = 0.0;
    switch (code)
    {
        case DN2CPP_PC_I1: { int8_t x; std::memcpy(&x, p, 1); *i = x; break; }
        case DN2CPP_PC_BOOL: case DN2CPP_PC_U1: { uint8_t x; std::memcpy(&x, p, 1); *i = x; break; }
        case DN2CPP_PC_I2: { int16_t x; std::memcpy(&x, p, 2); *i = x; break; }
        case DN2CPP_PC_CHAR: case DN2CPP_PC_U2: { uint16_t x; std::memcpy(&x, p, 2); *i = x; break; }
        case DN2CPP_PC_I4: { int32_t x; std::memcpy(&x, p, 4); *i = x; break; }
        case DN2CPP_PC_U4: { uint32_t x; std::memcpy(&x, p, 4); *i = static_cast<int64_t>(x); break; }
        case DN2CPP_PC_I8: case DN2CPP_PC_U8: std::memcpy(i, p, 8); break;
        case DN2CPP_PC_R4: { float f; std::memcpy(&f, p, 4); *r = f; break; }
        default: std::memcpy(r, p, 8); break;
    }
}

[[noreturn]] static void dn2cpp_throw_array_copy_mismatch()
{
    const char* m = "Source array type cannot be assigned to destination array type.";
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_array_type_mismatch_exception_type,
        dn2cpp_string_from_utf8(m, static_cast<int32_t>(std::strlen(m))), nullptr));
}

// The per-element cast/unbox arms' fault, .NET's own message. Elements copied
// before the faulting one stay copied, as on .NET.
[[noreturn]] static void dn2cpp_throw_array_copy_elem_cast()
{
    const char* m = "At least one element in the source array could not be cast down to the destination array type.";
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_invalid_cast_exception_type,
        dn2cpp_string_from_utf8(m, static_cast<int32_t>(std::strlen(m))), nullptr));
}

// Whether a value of type-info `v` boxes into (or, arguments swapped, unboxes out
// of) a reference-element array of `r` — .NET's CanCastTo between the boxed form
// and the reference element. dn2cpp_typeinfo_assignable answers the object and
// interface cases; System.Object/ValueType/Enum are ALSO matched by name because
// a hand-written primitive type-info's base chain is empty (dn2cpp_int32_type has
// no base), so the walk alone cannot see them, and the emitted abstract shells
// are rival handles to the runtime's own.
static bool dn2cpp_array_copy_box_ok(const Dn2CppTypeInfo* v, const Dn2CppTypeInfo* r)
{
    if (dn2cpp_typeinfo_assignable(v, r) != 0)
        return true;
    if (r->name == nullptr)
        return false;
    if (std::strcmp(r->name, "System.Object") == 0 || std::strcmp(r->name, "System.ValueType") == 0)
        return true;
    if (std::strcmp(r->name, "System.Enum") == 0)
        return (v->flags & DN2CPP_TF_ENUM) != 0;
    return false;
}

// One side of a Copy pair: flat element base, stride, element kind. Serves the
// three SZ reps and the MD layout with one shape (MD elements are flat in the
// data block, in the row-major order .NET's own MD Copy moves them in).
struct Dn2CppArrCopyView
{
    char* data;
    size_t stride;
    bool isRef;
    const Dn2CppTypeInfo* elem; // null = element unknown (an imprecise array handle)
};

static Dn2CppArrCopyView dn2cpp_array_copy_view(Dn2CppObject* a)
{
    const Dn2CppTypeInfo* t = a->type;
    if (t != nullptr && t->arrayRank > 1)
    {
        auto* md = reinterpret_cast<Dn2CppMDArray*>(a);
        const Dn2CppTypeInfo* el = t->elementType;
        bool isRef = el != nullptr && (el->flags & DN2CPP_TF_VALUETYPE) == 0;
        return { md->data, static_cast<size_t>(md->elemSize), isRef, el };
    }
    const Dn2CppTypeInfo* el = t != nullptr ? t->elementType : nullptr;
    bool isRef = el == nullptr ? (t == &dn2cpp_array_ref_type)
                               : (el->flags & DN2CPP_TF_VALUETYPE) == 0;
    if (isRef)
    {
        auto* r = static_cast<Dn2CppArrayRef*>(a);
        return { reinterpret_cast<char*>(r->data), sizeof(Dn2CppObject*), true, el };
    }
    bool isI4 = el == &dn2cpp_int32_type || el == &dn2cpp_uint32_type
        || (el != nullptr && (el->flags & DN2CPP_TF_ENUM) != 0
            && (el->enumUnderlying == &dn2cpp_int32_type
                || el->enumUnderlying == &dn2cpp_uint32_type));
    if (isI4)
    {
        auto* i = static_cast<Dn2CppArrayI4*>(a);
        return { reinterpret_cast<char*>(i->data), sizeof(int32_t), false, el };
    }
    auto* n = static_cast<Dn2CppArrayN*>(a);
    return { n->data, static_cast<size_t>(n->elemSize), false, el };
}

// Array.Copy between two arrays of DIFFERENT identity — the CLR's compatibility
// verdict:
//   - same element, or both integral and equal after dn2cpp_prim_norm (enums as
//     their underlying; IntPtr/UIntPtr one class of their own): raw memmove;
//   - CanPrimitiveWiden(src, dst): per-element numeric conversion;
//   - value -> compatible reference element: per-element box;
//   - reference -> value element with the boxed form castable to the source
//     element: per-element unbox under an EXACT type test (a boxed short into
//     int[] is InvalidCastException — no widening at unbox), null faulting too
//     except into a Nullable<U> element (which takes exactly a boxed U or null);
//   - reference -> reference: raw memmove when the source element is assignable
//     to the destination's, a per-element cast check when only the reverse
//     holds, so a Base into Der[] faults after the castable prefix copied;
//   - anything else: ArrayTypeMismatchException — including at length 0, the
//     verdict being about the PAIR, not the elements.
// An element-UNKNOWN side (an imprecise array handle: the Godot packed arrays, the
// shared ref-element header, a legacy null MD header) keeps the raw move when the
// layouts agree; a disagreeing pair has no honest answer — .NET's verdict needs the
// element types this handle does not carry — so it refuses as a mismatch rather than
// moving bytes under the wrong layout. One precision residue is accepted knowingly: an
// element type OUTSIDE the emit set still states element object, and such a pair boxes
// here where .NET refuses — consistent with the object GetElementType() answers for
// that same array, memory-safe (the boxes are real; a later cast faults catchably), and
// not distinguishable from a true object[] at this site.
void dn2cpp_array_copy_checked(Dn2CppObject* src, int32_t srcIdx,
                               Dn2CppObject* dst, int32_t dstIdx, int32_t len)
{
    Dn2CppArrCopyView s = dn2cpp_array_copy_view(src);
    Dn2CppArrCopyView d = dn2cpp_array_copy_view(dst);
    char* sp = s.data + static_cast<size_t>(srcIdx) * s.stride;
    char* dp = d.data + static_cast<size_t>(dstIdx) * d.stride;
    const Dn2CppTypeInfo* se = s.elem;
    const Dn2CppTypeInfo* de = d.elem;
    if (se == nullptr || de == nullptr)
    {
        if (s.isRef == d.isRef && s.stride == d.stride)
        {
            dn2cpp_gc_memmove_refs(dp, sp, static_cast<size_t>(len) * s.stride);
            return;
        }
        dn2cpp_throw_array_copy_mismatch();
    }
    if (se == de)
    {
        dn2cpp_gc_memmove_refs(dp, sp, static_cast<size_t>(len) * s.stride);
        return;
    }
    bool sv = (se->flags & DN2CPP_TF_VALUETYPE) != 0;
    bool dv = (de->flags & DN2CPP_TF_VALUETYPE) != 0;
    if (sv && dv)
    {
        const Dn2CppTypeInfo* es = (se->flags & DN2CPP_TF_ENUM) != 0
            ? (se->enumUnderlying != nullptr ? se->enumUnderlying : &dn2cpp_int32_type)
            : se;
        const Dn2CppTypeInfo* ed = (de->flags & DN2CPP_TF_ENUM) != 0
            ? (de->enumUnderlying != nullptr ? de->enumUnderlying : &dn2cpp_int32_type)
            : de;
        if (es == ed)
        {
            dn2cpp_gc_memmove_refs(dp, sp, static_cast<size_t>(len) * s.stride);
            return;
        }
        int32_t cs = dn2cpp_prim_code(es);
        int32_t cd = dn2cpp_prim_code(ed);
        if (cs >= 0 && cd >= 0)
        {
            if (dn2cpp_prim_norm(cs) == dn2cpp_prim_norm(cd))
            {
                dn2cpp_gc_memmove_refs(dp, sp, static_cast<size_t>(len) * s.stride);
                return;
            }
            if (dn2cpp_prim_widens(cs, cd))
            {
                for (int32_t k = 0; k < len; k++)
                {
                    int64_t i;
                    double r;
                    dn2cpp_read_prim_storage(sp + static_cast<size_t>(k) * s.stride, cs, &i, &r);
                    dn2cpp_write_prim(dp + static_cast<size_t>(k) * d.stride, cd, cs, i, r);
                }
                return;
            }
            dn2cpp_throw_array_copy_mismatch();
        }
        // IntPtr/UIntPtr: one normalization class of their own, with no widening
        // row — nint[] <-> nuint[] copies raw; nint[] -> long[] refuses.
        if ((es == &dn2cpp_intptr_type || es == &dn2cpp_uintptr_type)
            && (ed == &dn2cpp_intptr_type || ed == &dn2cpp_uintptr_type))
        {
            dn2cpp_gc_memmove_refs(dp, sp, static_cast<size_t>(len) * s.stride);
            return;
        }
        dn2cpp_throw_array_copy_mismatch();
    }
    if (sv)
    {
        // Boxing copy: value elements into a compatible reference-element array.
        if (!dn2cpp_array_copy_box_ok(se, de))
            dn2cpp_throw_array_copy_mismatch();
        for (int32_t k = 0; k < len; k++)
        {
            Dn2CppObject* b = dn2cpp_array_box_element(
                se, sp + static_cast<size_t>(k) * s.stride, static_cast<int32_t>(s.stride), false);
            char* slot = dp + static_cast<size_t>(k) * d.stride;
            std::memcpy(slot, &b, sizeof(Dn2CppObject*));
            dn2cpp_gc_write_barrier_if_heap(slot);
        }
        return;
    }
    if (dv)
    {
        // Unboxing copy: exact element type per element (dn2cpp_array_store_boxed
        // is NOT reused for the primitive case — its SetValue contract widens,
        // and Copy's unbox arm does not); only the Nullable<U> element shares its
        // arm, whose exact-U-or-null rule IS the Copy rule.
        if (!dn2cpp_array_copy_box_ok(de, se))
            dn2cpp_throw_array_copy_mismatch();
        const Dn2CppTypeInfo* u = dn2cpp_nullable_underlying_ti(de);
        const Dn2CppTypeInfo* eff = (de->flags & DN2CPP_TF_ENUM) != 0
            ? (de->enumUnderlying != nullptr ? de->enumUnderlying : &dn2cpp_int32_type)
            : de;
        int32_t c = u != nullptr ? DN2CPP_PC_NONE : dn2cpp_prim_code(eff);
        for (int32_t k = 0; k < len; k++)
        {
            Dn2CppObject* v;
            std::memcpy(&v, sp + static_cast<size_t>(k) * s.stride, sizeof(Dn2CppObject*));
            char* slot = dp + static_cast<size_t>(k) * d.stride;
            if (u != nullptr)
            {
                dn2cpp_array_store_boxed(v, de, slot, static_cast<int32_t>(d.stride), false);
                continue;
            }
            if (v == nullptr || v->type != de)
                dn2cpp_throw_array_copy_elem_cast();
            if (c >= 0)
            {
                int32_t sc;
                int64_t i;
                double r;
                dn2cpp_read_boxed_num(v, &sc, &i, &r);
                dn2cpp_write_prim(slot, c, sc, i, r);
            }
            else
            {
                std::memcpy(slot, v + 1, d.stride);
            }
            dn2cpp_gc_write_barrier_if_heap(slot);
        }
        return;
    }
    // Reference <-> reference.
    if (dn2cpp_typeinfo_assignable(se, de) != 0)
    {
        dn2cpp_gc_memmove_refs(dp, sp, static_cast<size_t>(len) * s.stride);
        return;
    }
    if (dn2cpp_typeinfo_assignable(de, se) == 0)
        dn2cpp_throw_array_copy_mismatch();
    for (int32_t k = 0; k < len; k++)
    {
        Dn2CppObject* v;
        std::memcpy(&v, sp + static_cast<size_t>(k) * s.stride, sizeof(Dn2CppObject*));
        if (v != nullptr && dn2cpp_typeinfo_assignable(v->type, de) == 0)
            dn2cpp_throw_array_copy_elem_cast();
        char* slot = dp + static_cast<size_t>(k) * d.stride;
        std::memcpy(slot, &v, sizeof(Dn2CppObject*));
        dn2cpp_gc_write_barrier_if_heap(slot);
    }
    return;
}

// ---- reflection: Activator.CreateInstance with constructor arguments ----

// Whether the boxed argument binds to a parameter of type `p` under the
// DefaultBinder rules: null matches EVERY parameter type
// (value types construct from default); a reference parameter checks
// assignability; a Nullable<U> parameter takes exactly a boxed U; an enum
// parameter its exact enum type; a primitive parameter admits the
// CanPrimitiveWiden conversions (an enum argument widening as its underlying).
static bool dn2cpp_binder_arg_matches(Dn2CppObject* a, const Dn2CppTypeInfo* p)
{
    if (a == nullptr)
        return true;
    const Dn2CppTypeInfo* at = a->type;
    if (at == p)
        return true;
    if ((p->flags & DN2CPP_TF_VALUETYPE) == 0)
        return dn2cpp_type_is_assignable_from(dn2cpp_get_type_from_handle(p),
                                              dn2cpp_get_type_from_handle(at)) != 0;
    if (const Dn2CppTypeInfo* u = dn2cpp_nullable_underlying_ti(p))
        return at == u;
    if ((p->flags & DN2CPP_TF_ENUM) != 0)
        return false;
    int32_t pc = dn2cpp_prim_code(p);
    if (pc < 0)
        return false;
    const Dn2CppTypeInfo* aeff = (at->flags & DN2CPP_TF_ENUM) != 0
        ? (at->enumUnderlying != nullptr ? at->enumUnderlying : &dn2cpp_int32_type)
        : at;
    int32_t ac = dn2cpp_prim_code(aeff);
    return ac >= 0 && dn2cpp_prim_widens(ac, pc);
}

// Whether parameter type `a` is at least as specific as `b` — the ordering the
// DefaultBinder resolves multi-candidate matches with: reference types by
// assignability (string beats object), primitives by the widening matrix (int
// beats long for a short argument); a value/reference mix is incomparable.
static bool dn2cpp_binder_param_at_least_as_specific(const Dn2CppTypeInfo* a, const Dn2CppTypeInfo* b)
{
    if (a == b)
        return true;
    bool aVal = (a->flags & DN2CPP_TF_VALUETYPE) != 0;
    bool bVal = (b->flags & DN2CPP_TF_VALUETYPE) != 0;
    if (!aVal && !bVal)
        return dn2cpp_type_is_assignable_from(dn2cpp_get_type_from_handle(b),
                                              dn2cpp_get_type_from_handle(a)) != 0;
    if (aVal && bVal)
    {
        int32_t ac = dn2cpp_prim_code(a);
        int32_t bc = dn2cpp_prim_code(b);
        return ac >= 0 && bc >= 0 && dn2cpp_prim_widens(ac, bc);
    }
    return false;
}

// Adapt one bound argument to the invoker thunk's parameter representation: a
// widened primitive re-boxes under the parameter's type at the box-payload
// convention; null against a value type materializes default(T); a Nullable<U>
// parameter materializes the {hasValue, value} struct box the thunk reads.
// Reference parameters (and exact-type value args) pass through.
static Dn2CppObject* dn2cpp_binder_adapt_arg(Dn2CppObject* a, const Dn2CppTypeInfo* p)
{
    if ((p->flags & DN2CPP_TF_VALUETYPE) == 0)
        return a;
    if (const Dn2CppTypeInfo* u = dn2cpp_nullable_underlying_ti(p))
    {
        int32_t off, w, uc;
        if (!dn2cpp_nullable_layout(u, &off, &w, &uc))
            dn2cpp_throw_platform_not_supported(
                "Activator: binding a Nullable<T> parameter of a non-primitive T is not supported");
        int32_t sz = p->instanceSize > 0 ? p->instanceSize : off + w;
        auto* box = static_cast<Dn2CppObject*>(dn2cpp_alloc(sizeof(Dn2CppObject) + static_cast<size_t>(sz)));
        box->type = p;
        if (a != nullptr)
        {
            *reinterpret_cast<uint8_t*>(box + 1) = 1;
            int32_t sc;
            int64_t i;
            double r;
            dn2cpp_read_boxed_num(a, &sc, &i, &r);
            dn2cpp_write_prim(reinterpret_cast<char*>(box + 1) + off, uc, sc, i, r);
        }
        return box;
    }
    if (a != nullptr && a->type == p)
        return a;
    // The box-payload width of `p` (the width the invoker thunk reads).
    bool isEnum = (p->flags & DN2CPP_TF_ENUM) != 0;
    const Dn2CppTypeInfo* eff = isEnum
        ? (p->enumUnderlying != nullptr ? p->enumUnderlying : &dn2cpp_int32_type)
        : p;
    int32_t pc = dn2cpp_prim_code(eff);
    int32_t payloadW = pc < 0
        ? (p->instanceSize > 0 ? p->instanceSize : 8)
        : (dn2cpp_prim_storage_width(pc) < 4 ? 4 : dn2cpp_prim_storage_width(pc));
    auto* box = static_cast<Dn2CppObject*>(dn2cpp_alloc(sizeof(Dn2CppObject) + static_cast<size_t>(payloadW)));
    box->type = p;
    if (a == nullptr || pc < 0)
        return box; // zeroed default(T) (a non-primitive p with a != null was exact-matched above)
    int32_t sc;
    int64_t i;
    double r;
    dn2cpp_read_boxed_num(a, &sc, &i, &r);
    // Write at the BOX payload width (>= 4), not the packed storage width.
    switch (pc)
    {
        case DN2CPP_PC_R4:
        {
            float f = (sc == DN2CPP_PC_R4 || sc == DN2CPP_PC_R8) ? static_cast<float>(r) : static_cast<float>(i);
            std::memcpy(box + 1, &f, 4);
            break;
        }
        case DN2CPP_PC_R8:
        {
            double d = (sc == DN2CPP_PC_R4 || sc == DN2CPP_PC_R8) ? r : static_cast<double>(i);
            std::memcpy(box + 1, &d, 8);
            break;
        }
        case DN2CPP_PC_I8: case DN2CPP_PC_U8:
            std::memcpy(box + 1, &i, 8);
            break;
        default:
        {
            int32_t w32 = static_cast<int32_t>(i);
            std::memcpy(box + 1, &w32, 4);
            break;
        }
    }
    return box;
}

// Activator.CreateInstance(Type, object[] args[, BindingFlags, Binder,
// activationAttributes]): DefaultBinder-style constructor resolution matching real
// .NET — candidates filter on BindingFlags + arity + per-argument admissibility;
// several candidates resolve to the unique most-specific one (AmbiguousMatchException
// when none dominates, e.g. a null argument against (int) and (string) ctors); no
// candidate is MissingMethodException. A non-null Binder is the AOT
// PlatformNotSupportedException (the shared member-lookup posture); non-empty
// activation attributes throw PlatformNotSupportedException like real .NET.
Dn2CppObject* dn2cpp_activator_create_instance_args(Dn2CppType* t, Dn2CppArrayRef* args,
                                                    int32_t bindingFlags, Dn2CppObject* binder,
                                                    Dn2CppArrayRef* activationAttrs)
{
    dn2cpp_reflection_check_binder(binder);
    if (activationAttrs != nullptr && activationAttrs->length > 0)
        dn2cpp_throw_platform_not_supported("Activation Attributes are not supported.");
    if (t == nullptr)
        dn2cpp_throw_argument_null();
    const Dn2CppTypeInfo* ti = t->typeInfo;
    int32_t argc = (args == nullptr) ? 0 : args->length;
    if (argc == 0)
        return dn2cpp_activator_create_instance_nonpublic(
            t, (bindingFlags & DN2CPP_BF_NONPUBLIC) != 0 ? 1 : 0);
    if ((ti->flags & (DN2CPP_TF_ABSTRACT | DN2CPP_TF_INTERFACE)) != 0)
        dn2cpp_throw_missing_method("Cannot create an instance of an abstract class or interface");
    if (argc > 30)
        dn2cpp_throw_platform_not_supported("Activator: more than 30 constructor arguments");
    const Dn2CppMethodInfo* cands[32];
    int32_t n = 0;
    for (int32_t c = 0; c < ti->ctorCount; c++)
    {
        const Dn2CppMethodInfo* ci = &ti->ctors[c];
        if (!dn2cpp_member_matches(ci->attrs, bindingFlags, false) || ci->paramCount != argc)
            continue;
        bool ok = true;
        for (int32_t j = 0; j < argc && ok; j++)
            ok = dn2cpp_binder_arg_matches(args->data[j], ci->parameters[j].paramType);
        if (ok && n < 32)
            cands[n++] = ci;
    }
    if (n == 0)
        dn2cpp_throw_missing_method("Constructor on this type not found");
    const Dn2CppMethodInfo* best = nullptr;
    if (n == 1)
        best = cands[0];
    else
    {
        // The unique candidate whose whole parameter list is at least as
        // specific as every rival's, strictly better somewhere.
        for (int32_t x = 0; x < n && best == nullptr; x++)
        {
            bool dominates = true;
            for (int32_t y = 0; y < n && dominates; y++)
            {
                if (x == y)
                    continue;
                bool geAll = true, gtAny = false;
                for (int32_t j = 0; j < argc; j++)
                {
                    const Dn2CppTypeInfo* px = cands[x]->parameters[j].paramType;
                    const Dn2CppTypeInfo* py = cands[y]->parameters[j].paramType;
                    if (!dn2cpp_binder_param_at_least_as_specific(px, py))
                    {
                        geAll = false;
                        break;
                    }
                    if (px != py)
                        gtAny = true;
                }
                dominates = geAll && gtAny;
            }
            if (dominates)
                best = cands[x];
        }
        if (best == nullptr)
            dn2cpp_throw_ambiguous_match();
    }
    Dn2CppObject* adapted[30];
    for (int32_t j = 0; j < argc; j++)
        adapted[j] = dn2cpp_binder_adapt_arg(args->data[j], best->parameters[j].paramType);
    return dn2cpp_ctor_invoke_argv(best, adapted, argc);
}
