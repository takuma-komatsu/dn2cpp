// dn2cpp_system_reflection.cpp — System.Reflection / System.Type intrinsics.
//
// The transpiler binds the reflection surface — Type/Enum queries, generic type
// reflection, Type.GetType(string), member and custom-attribute enumeration — to
// these walks over the embedded Dn2CppTypeInfo metadata (the tree-shaken
// descriptor tables the backend emits) instead of the BCL's reflection IL.
// Behavior matches real .NET for the modeled surface.
#include "dn2cpp_core.h"

#include <algorithm>
#include <atomic>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <mutex>
#include <string>
#include <unordered_map>
#include <vector>

#ifdef _WIN32
#include <windows.h>
#endif

// ---- generic reflection ----

// Type.IsGenericType: a closed instantiation (genericArgCount > 0) or an open
// definition (DN2CPP_TF_GENERICDEF). ContainsGenericParameters additionally
// recognizes formal method parameters; those are not generic type definitions.
// Constraint recipes may expose metadata-only compositions of formal parameters.
int32_t dn2cpp_type_is_generic_type(const Dn2CppTypeInfo* ti)
{
    return (ti->genericArgCount > 0 || (ti->flags & DN2CPP_TF_GENERICDEF) != 0) ? 1 : 0;
}

// Constructed types have arguments, including metadata-only open constraint shapes.
int32_t dn2cpp_type_is_constructed_generic(const Dn2CppTypeInfo* ti)
{
    return ti->genericArgCount > 0 ? 1 : 0;
}

int32_t dn2cpp_type_is_generic_type_definition(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_GENERICDEF) != 0 ? 1 : 0;
}

// Definitions include open generic declarations and builtins, not compound or formal identities.
int32_t dn2cpp_type_is_type_definition(const Dn2CppTypeInfo* ti)
{
    // These erased runtime handles only represent constructed instances; open
    // declarations use separate generic-definition handles.
    if (ti == &dn2cpp_threadlocal_type || ti == &dn2cpp_blockingcollection_type)
        return 0;
    // Enum rows omit generic-owner arguments, so an ordinary enum cannot be
    // distinguished from a constructed nested enum. Synthetic comparers likewise
    // do not model the CLR's concrete generic/non-generic implementation identity.
    if ((ti->flags & DN2CPP_TF_DEFAULT_EQ_COMPARER) != 0
        || ((ti->flags & DN2CPP_TF_ENUM) != 0
            && (ti->flags & DN2CPP_TF_GENERICDEF) == 0 && ti->genericArgCount == 0))
        dn2cpp_throw_platform_not_supported("Reflection: type definition identity is unavailable");
    return dn2cpp_signature_kind(ti) == 0
        && (ti->flags & (DN2CPP_TF_ARRAY | DN2CPP_TF_GENERICPARAM)) == 0
        && !dn2cpp_type_is_constructed_generic(ti) ? 1 : 0;
}

int32_t dn2cpp_type_contains_generic_parameters(const Dn2CppTypeInfo* ti)
{
    if ((ti->flags & (DN2CPP_TF_GENERICDEF | DN2CPP_TF_GENERICPARAM)) != 0)
        return 1;
    if (ti->elementType != nullptr && dn2cpp_type_contains_generic_parameters(ti->elementType))
        return 1;
    for (int32_t i = 0; i < ti->genericArgCount; i++)
        if (dn2cpp_type_contains_generic_parameters(ti->genericArgs[i]))
            return 1;
    return 0;
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

static std::vector<const Dn2CppTypeInfo*> dn2cpp_formal_type_arguments(const Dn2CppTypeInfo* owner);

// Every call returns a fresh array over the same interned argument identities.
Dn2CppArrayRef* dn2cpp_type_get_generic_arguments(Dn2CppType* t)
{
    const Dn2CppTypeInfo* ti = dn2cpp_type_require(t);
    std::vector<const Dn2CppTypeInfo*> formals;
    if ((ti->flags & DN2CPP_TF_GENERICDEF) != 0)
        formals = dn2cpp_formal_type_arguments(ti);
    int32_t n = (ti->flags & DN2CPP_TF_GENERICDEF) != 0 ? static_cast<int32_t>(formals.size()) : ti->genericArgCount;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&arr->data[i],
            reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(formals.empty() ? ti->genericArgs[i] : formals[i])));
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

// The type a member row belongs to: the one it runs on and DeclaringType reports.
// A MakeGenericType clone's own member rows name the clone, but a row read off a
// template level names the template, and no clone's base chain reaches a template:
// such a row belongs to the level of the reflected type that instantiates the
// template's definition.
static const Dn2CppTypeInfo* dn2cpp_invoke_declaring(const Dn2CppTypeInfo* declaring,
    const Dn2CppTypeInfo* reflected)
{
    if ((declaring->flags & DN2CPP_TF_GENERICDEF) != 0 && reflected != nullptr)
        for (const auto* type = reflected; type != nullptr; type = dn2cpp_type_reflection_base(type))
            if (type == declaring || type->genericDef == declaring)
                return type;
    if ((declaring->flags & DN2CPP_TF_RUNTIME_TEMPLATE) == 0)
        return declaring;
    const Dn2CppRuntimeTemplate* level = dn2cpp_runtime_template_by_ti(declaring);
    if (level == nullptr)
        return declaring;
    for (const Dn2CppTypeInfo* t = reflected; t != nullptr; t = t->base)
        if (t->genericDef == level->def && (t->flags & DN2CPP_TF_RUNTIME_TEMPLATE) == 0)
            return t;
    return declaring;
}

static std::string dn2cpp_utf8_of(Dn2CppString* s);

// Why a reflective call found no body to run. Each reason heads a refusal that names
// the member, its owner and assembly, and the remedy the transpiler's reflection
// warning prints for the same target.
static constexpr char kDn2CppUnretainedMember[] =
    "reflection found this member but its body was not retained";
static constexpr char kDn2CppUnretainedOverride[] = "the receiver's override was not retained";
static constexpr char kDn2CppUnretainedClosedGeneric[] =
    "this closed generic instantiation has no compiled code";
static constexpr char kDn2CppUnretainedOneType[] =
    "the runtime models every instantiation of this definition as one type, so MakeGenericType cannot form it";
static constexpr char kDn2CppUnretainedOneTypeFix[] =
    "construct the closed type directly instead of through MakeGenericType";

static std::string dn2cpp_unretained_subject(const Dn2CppTypeInfo* owner, const std::string& member)
{
    return dn2cpp_utf8_of(dn2cpp_type_tostring(owner)) + "::" + member;
}

// A link.xml attribute value: a compiler-generated name such as <Main>b__0_0 stays
// well-formed XML.
static void dn2cpp_append_xml_attribute(std::string& text, const char* value)
{
    for (const char* p = value; *p != 0; p++)
        switch (*p)
        {
            case '&': text += "&amp;"; break;
            case '<': text += "&lt;"; break;
            case '>': text += "&gt;"; break;
            case '"': text += "&quot;"; break;
            default: text += *p; break;
        }
}

// "<subject> in <assembly>: <reason>; fix: <remedy>", the assembly being that of `in`,
// the subject's type, or else the owner's. A null `element` is a closed-generic remedy,
// `name` when given and otherwise naming the instantiation statically; a null `name`
// preserves the whole definition; otherwise the link.xml descriptor keeps `name` as
// that element of the owner's definition. The
// definition's type-info name is the spelling a link.xml type selector matches ('+'
// between nested types, backtick arity), as the transpiler's warning renders the same
// remedy.
static std::string dn2cpp_unretained_text(const Dn2CppTypeInfo* owner, const std::string& subject,
    const std::string& reason, const char* element, const char* name, const Dn2CppTypeInfo* in = nullptr)
{
    const char* assembly = dn2cpp_ti_assembly_name(owner);
    std::string text = subject + " in ";
    text += in != nullptr ? dn2cpp_ti_assembly_name(in) : assembly;
    text += ": " + reason + "; fix: ";
    if (element == nullptr)
        return text + (name != nullptr ? name : "name the closed generic instantiation statically");
    const Dn2CppTypeInfo* definition = owner->genericArgCount > 0 && owner->genericDef != nullptr
        ? owner->genericDef : owner;
    text += "[Preserve] on the target, or link.xml <assembly fullname=\"";
    dn2cpp_append_xml_attribute(text, assembly);
    text += "\"><type fullname=\"";
    dn2cpp_append_xml_attribute(text, definition->name != nullptr ? definition->name : "?");
    if (name == nullptr)
        return text + "\" preserve=\"all\" /></assembly>";
    text += "\"><";
    text += element;
    text += " name=\"";
    dn2cpp_append_xml_attribute(text, name);
    text += "\" /></type></assembly>";
    return text;
}

// The refusal is a PlatformNotSupportedException raised before any target runs, so
// reflection invoke never wraps it.
[[noreturn]] static void dn2cpp_throw_unretained(const Dn2CppTypeInfo* owner, const std::string& subject,
    const std::string& reason, const char* element, const char* name, const Dn2CppTypeInfo* in = nullptr)
{
    const std::string text = dn2cpp_unretained_text(owner, subject, reason, element, name, in);
    Dn2CppObject* e = dn2cpp_exception_new(&dn2cpp_platform_not_supported_exception_type,
        dn2cpp_string_from_utf8(text.data(), static_cast<int32_t>(text.size())), nullptr);
    reinterpret_cast<Dn2CppExceptionObject*>(e)->hresult = static_cast<int32_t>(0x80131539u);
    dn2cpp_throw(e);
}

// Reflection makes no instance of a type the image never allocates
// (DN2CPP_TA_UNALLOCATED): the instance would dispatch through overrides and slots
// nothing wired. Preserving a class's constructor allocates the class; preserving a
// struct whole boxes it.
static bool dn2cpp_unallocated(const Dn2CppTypeInfo* ti)
{
    return (ti->reflection().activation & DN2CPP_TA_UNALLOCATED) != 0;
}

// `verb` opens the reason: what makes the instance. `in` is the subject's type when the
// subject is not a member of `ti`.
[[noreturn]] static void dn2cpp_throw_unallocated(const Dn2CppTypeInfo* ti, const std::string& subject,
    const char* verb, const Dn2CppTypeInfo* in = nullptr)
{
    const std::string type = dn2cpp_utf8_of(dn2cpp_type_tostring(ti));
    if ((ti->flags & DN2CPP_TF_VALUETYPE) != 0)
        dn2cpp_throw_unretained(ti, subject, std::string(verb) + " boxes " + type
            + ", which this image never allocates", "type", nullptr, in);
    dn2cpp_throw_unretained(ti, subject, std::string(verb) + " makes an instance of " + type
        + ", which this image never allocates", "method", ".ctor");
}

// Synthesized instantiations intern on (def, args): one managed type is exactly
// one Dn2CppTypeInfo* (the invariant every pointer-comparing walk rests on), so a
// second MakeGenericType with the same arguments must return the first pointer.
// Plain new, never freed — a type-info is immortal, like the emitted ones.
struct Dn2CppSynthInst
{
    const Dn2CppTypeInfo* def;
    const Dn2CppTypeInfo* ti;
    const Dn2CppTypeInfo* templateTi;
    Dn2CppSynthInst* next;
};
static std::atomic<Dn2CppSynthInst*> g_synth_insts{ nullptr };
static std::mutex& g_synth_mtx = dn2cpp_never_destroyed<std::mutex>();

// A definition can appear at differently permuted placeholder levels. Dispatch
// and binding use the actual source of the interned clone, not the first row
// with its definition. Publication precedes every escape of the clone.
const Dn2CppTypeInfo* dn2cpp_runtime_template_of(const Dn2CppTypeInfo* clone)
{
    for (Dn2CppSynthInst* n = g_synth_insts.load(std::memory_order_acquire); n != nullptr; n = n->next)
        if (n->ti == clone)
            return n->templateTi;
    return clone;
}

// The rgctx anchor tables of the synthesized instantiations, one node per
// placeholder source level, keyed (clone type-info, source type-info). A clone's
// base interns onto the AOT type-info when the image carries that instantiation,
// and an AOT base cannot anchor the clone's shared bodies (see
// DN2CPP_TF_RUNTIME_SYNTH) — only a table shaped by the TEMPLATE's descriptor
// matches the slot indices compiled into those bodies. Written under g_synth_mtx
// before the clone escapes; read lock-free in shared-body prologues, so the head
// publishes release/acquire and nodes are immutable once linked.
struct Dn2CppSynthAnchor
{
    const Dn2CppTypeInfo* ti;
    const Dn2CppTypeInfo* source;
    const void* const* table;
    Dn2CppSynthAnchor* next;
};
static std::atomic<Dn2CppSynthAnchor*> g_synth_anchors{ nullptr };

const void* const* dn2cpp_rgctx_template(const Dn2CppTypeInfo* t, const Dn2CppTypeInfo* source)
{
    for (Dn2CppSynthAnchor* n = g_synth_anchors.load(std::memory_order_acquire);
         n != nullptr; n = n->next)
        if (n->ti == t && n->source == source)
            return n->table;
    dn2cpp_throw_invalid_operation();
}

const void* const* dn2cpp_rgctx_synth(const Dn2CppTypeInfo* t, const Dn2CppTypeInfo* genericDef)
{
    // Ordinary emitted bodies retain the actual base's layout. Template bodies
    // request their source layout through dn2cpp_rgctx_template instead.
    while (t != nullptr && t->genericDef != genericDef)
        t = t->base;
    return t != nullptr ? t->rgctx : nullptr;
}

// The AOT image's own (definition, args) instantiation, if it emitted one. Every
// synthesis entry point — top-level MakeGenericType AND the clone's base-chain
// recursion — must consult this first: a synthesized duplicate of an emitted
// type-info breaks every pointer-comparing walk (isinst, cast, IsAssignableFrom,
// BaseType identity). A null argument matches nothing; the caller keeps its own
// miss behavior.
static const Dn2CppTypeInfo* dn2cpp_find_aot_instantiation(
    const Dn2CppTypeInfo* def, const Dn2CppTypeInfo* const* args, int32_t argc)
{
    for (int32_t k = 0; k < dn2cpp_type_registry_count; k++)
    {
        const Dn2CppTypeInfo* cand = dn2cpp_type_registry[k].type;
        if (cand->genericDef != def || (cand->flags & DN2CPP_TF_GENERICDEF) != 0)
            continue;
        if (cand->genericArgCount != argc)
            continue;
        bool match = true;
        for (int32_t i = 0; i < argc; i++)
            if (args[i] == nullptr || cand->genericArgs[i] != args[i]) { match = false; break; }
        if (match)
            return cand;
    }
    return nullptr;
}

// Each row projects its own local argument vector into its base's.
static std::vector<const Dn2CppTypeInfo*> dn2cpp_project_base_arguments(
    const Dn2CppRuntimeTemplate* row, const Dn2CppRuntimeTemplate* base,
    const Dn2CppTypeInfo* const* args)
{
    if (base == nullptr || row->baseArgumentCount != base->argCount)
        dn2cpp_throw_invalid_operation();
    std::vector<const Dn2CppTypeInfo*> projected;
    for (int32_t i = 0; i < row->baseArgumentCount; i++)
    {
        const int32_t index = row->baseArguments[i];
        if (index < 0 || index >= row->argCount)
            dn2cpp_throw_invalid_operation();
        projected.push_back(args[index]);
    }
    return projected;
}

// Clones `row`'s template for the argument vector (caller holds g_synth_mtx;
// argument count already checked against row->argCount), recursively for bases.
static const Dn2CppTypeInfo* dn2cpp_synthesize_instantiation(
    const Dn2CppRuntimeTemplate* row, const Dn2CppTypeInfo* const* args)
{
    // Recursive bases use the same preferred metadata source as direct creation.
    // Otherwise an intermediate source can discard a rooted constructor body.
    row = dn2cpp_runtime_template_by_def(row->def);
    for (Dn2CppSynthInst* n = g_synth_insts.load(std::memory_order_relaxed); n != nullptr; n = n->next)
    {
        if (n->def != row->def)
            continue;
        bool same = true;
        for (int32_t i = 0; i < row->argCount; i++)
            if (n->ti->genericArgs[i] != args[i]) { same = false; break; }
        if (same)
            return n->ti;
    }
    if (const Dn2CppTypeInfo* aot = dn2cpp_find_aot_instantiation(row->def, args, row->argCount))
        return aot;
    struct SynthesizedType
    {
        Dn2CppTypeInfo type;
        Dn2CppTypeReflection reflection;
    };
    auto* synthesized = new SynthesizedType{ *row->templateTi, row->templateTi->reflection() };
    Dn2CppTypeInfo* ti = &synthesized->type;
    ti->reflectionData = &synthesized->reflection;
    ti->flags = (ti->flags & ~(DN2CPP_TF_SHARED_CANON | DN2CPP_TF_RUNTIME_TEMPLATE))
        | DN2CPP_TF_RUNTIME_SYNTH;
    auto** argv = new const Dn2CppTypeInfo*[row->argCount];
    for (int32_t i = 0; i < row->argCount; i++)
        argv[i] = args[i];
    ti->genericArgs = argv;
    ti->genericArgCount = row->argCount;
    ti->genericDef = row->def;
    ti->typeObject = nullptr;   // interned lazily by dn2cpp_get_type_from_handle_slow
    // dn2cpp_ctor_invoke_argv allocates from mi->declaringType, so a shared ctor
    // row would stamp the TEMPLATE (whose rgctx is null) onto the new instance.
    if (ti->reflection().ctorCount > 0)
    {
        auto* ctors = new Dn2CppMethodDelta[ti->reflection().ctorCount];
        for (int32_t i = 0; i < ti->reflection().ctorCount; i++)
        {
            ctors[i].original = row->templateTi->reflection().ctors[i];
            ctors[i].declaringType = ti;
        }
        synthesized->reflection.ctors = Dn2CppMetadataTable<Dn2CppMethodInfo>::from_raw(ctors);
    }
    if (const auto initializer = row->templateTi->reflection().initializer)
    {
        auto* delta = new Dn2CppMethodDelta{};
        delta->original = initializer;
        delta->declaringType = ti;
        synthesized->reflection.initializer = Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_raw(delta);
    }
    // Method rows name the clone as their declaring type, so every binding
    // resolved against the clone's rows reports the clone, as .NET does.
    Dn2CppMethodDelta* methods = nullptr;
    if (ti->reflection().methodCount > 0)
    {
        methods = new Dn2CppMethodDelta[ti->reflection().methodCount];
        for (int32_t i = 0; i < ti->reflection().methodCount; i++)
        {
            methods[i].original = row->templateTi->reflection().methods[i];
            methods[i].declaringType = ti;
        }
        synthesized->reflection.methods = Dn2CppMetadataTable<Dn2CppMethodInfo>::from_raw(methods);
    }
    // Field and property rows name the clone too, copied as native rows. A
    // property's accessors are the clone's method rows, so GetGetMethod() and
    // GetMethod("get_P") are one handle.
    const Dn2CppTypeReflection tmpl = row->templateTi->reflection();
    if (tmpl.fieldCount > 0)
    {
        auto* fields = new Dn2CppFieldInfo[tmpl.fieldCount];
        for (int32_t i = 0; i < tmpl.fieldCount; i++)
        {
            fields[i] = *tmpl.fields[i];
            fields[i].declaringType = ti;
        }
        synthesized->reflection.fields = Dn2CppMetadataTable<Dn2CppFieldInfo>::from_raw(fields);
    }
    auto clone_accessor = [&](Dn2CppMetadataHandle<Dn2CppMethodInfo> accessor) {
            for (int32_t j = 0; accessor && j < tmpl.methodCount; j++)
                if (methods[j].original == accessor)
                    return Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_raw(&methods[j]);
            return accessor;
        };
    if (tmpl.propCount > 0)
    {
        auto* props = new Dn2CppPropInfo[tmpl.propCount];
        for (int32_t i = 0; i < tmpl.propCount; i++)
        {
            props[i] = *tmpl.props[i];
            props[i].declaringType = ti;
            props[i].getter = clone_accessor(props[i].getter);
            props[i].setter = clone_accessor(props[i].setter);
        }
        synthesized->reflection.props = Dn2CppMetadataTable<Dn2CppPropInfo>::from_raw(props);
    }
    if (tmpl.eventCount > 0)
    {
        auto* events = new Dn2CppEventInfo[tmpl.eventCount];
        for (int32_t i = 0; i < tmpl.eventCount; i++)
        {
            events[i] = *tmpl.events[i];
            events[i].declaringType = ti;
            events[i].adder = clone_accessor(events[i].adder);
            events[i].remover = clone_accessor(events[i].remover);
            events[i].raiser = clone_accessor(events[i].raiser);
            if (events[i].otherCount > 0)
            {
                auto* others = new Dn2CppMetadataHandle<Dn2CppMethodInfo>[events[i].otherCount];
                for (int32_t j = 0; j < events[i].otherCount; j++)
                    others[j] = clone_accessor(events[i].others[j]);
                events[i].others = others;
            }
        }
        synthesized->reflection.events = events;
    }
    if (ti->base != nullptr && (ti->base->flags & DN2CPP_TF_RUNTIME_TEMPLATE) != 0)
    {
        const Dn2CppRuntimeTemplate* brow = dn2cpp_runtime_template_by_ti(ti->base);
        const auto baseArgs = dn2cpp_project_base_arguments(row, brow, args);
        // Interns onto the AOT instantiation when the image carries it (the
        // pointer-comparing walks demand the one type-info); such a base's
        // rgctx never serves the clone — the anchor loop below does.
        ti->base = dn2cpp_synthesize_instantiation(brow, baseArgs.data());
    }
    // An inherited row can come from any interned source of its definition,
    // whose reached bodies need not have the same slot layout. Supply each
    // source layout over the actual level's local arguments on this receiver.
    // Allocate all tables before filling forwarded source tables.
    struct SynthesizedLevel
    {
        const Dn2CppRuntimeTemplate* row;
        const void** table;
        std::vector<const Dn2CppTypeInfo*> arguments;
    };
    std::vector<SynthesizedLevel> levels;
    for (const Dn2CppTypeInfo* closed = ti; closed != nullptr; closed = closed->base)
        for (int32_t j = 0; j < dn2cpp_runtime_template_count; j++)
        {
            const auto* r = &dn2cpp_runtime_templates[j];
            if (r->def != closed->genericDef)
                continue;
            levels.push_back({ r, r->rgctxDescCount > 0 ? new const void*[r->rgctxDescCount] : nullptr,
                std::vector<const Dn2CppTypeInfo*>(closed->genericArgs, closed->genericArgs + closed->genericArgCount) });
        }
    for (size_t k = 0; k < levels.size(); k++)
    {
        const auto& level = levels[k];
        const auto* r = level.row;
        const auto table = level.table;
        if (table == nullptr)
            continue;
        for (int32_t i = 0; i < r->rgctxDescCount; i++)
        {
            const int32_t entry = r->rgctxDesc[i];
            if (entry >= 0)
            {
                if (entry >= r->argCount)
                    dn2cpp_throw_invalid_operation();
                table[i] = level.arguments[entry];
                continue;
            }
            const auto* target = r->templateTi;
            for (int32_t depth = ~entry; depth > 0 && target != nullptr; depth--)
                target = target->base;
            const void** forwarded = nullptr;
            for (const auto& candidate : levels)
                if (candidate.row->templateTi == target)
                    forwarded = candidate.table;
            if (forwarded == nullptr)
                dn2cpp_throw_invalid_operation();
            table[i] = forwarded;
        }
        if (r == row)
            ti->rgctx = table;
        auto* an = new Dn2CppSynthAnchor{ ti, r->templateTi, table,
            g_synth_anchors.load(std::memory_order_relaxed) };
        g_synth_anchors.store(an, std::memory_order_release);
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
    auto* node = new Dn2CppSynthInst{ row->def, ti, row->templateTi, g_synth_insts.load(std::memory_order_relaxed) };
    g_synth_insts.store(node, std::memory_order_release);
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
        if (args[i] == nullptr || dn2cpp_type_contains_generic_parameters(args[i]) || dn2cpp_type_metadata_only(args[i]))
            return nullptr;
    std::lock_guard<std::mutex> lock(g_synth_mtx);
    return dn2cpp_synthesize_instantiation(row, args);
}

extern Dn2CppTypeInfo dn2cpp_threadlocal_type;
extern Dn2CppTypeInfo dn2cpp_blockingcollection_type;

// Whether the runtime models every instantiation of a definition as one shared type
// whose header records no type arguments, so no closed type of it carries the identity
// MakeGenericType resolves, even one the program constructs.
static bool dn2cpp_definition_is_one_runtime_type(const Dn2CppTypeInfo* defTi)
{
    if (defTi->name == nullptr)
        return false;
    for (const Dn2CppTypeInfo* shared : { &dn2cpp_threadlocal_type, &dn2cpp_blockingcollection_type })
        if (std::strcmp(shared->name, defTi->name) == 0)
            return true;
    return false;
}

// Type.MakeGenericType(args): resolve the (definition, args) instantiation against the
// program's reachability-generated closed types (scanned via the type registry). Like
// IL2CPP, an instantiation that AOT never generated is not built at runtime — it throws
// NotSupportedException, UNLESS the definition is typeof-only and the emitter shipped a
// runtime-instantiation template for it (see Dn2CppRuntimeTemplate), in which case the
// instantiation is synthesized by cloning that template. A null args array is
// ArgumentNullException before the definition check, as in .NET.
static const Dn2CppTypeInfo* dn2cpp_open_generic_type(const Dn2CppTypeInfo* definition,
    const std::vector<const Dn2CppTypeInfo*>& arguments);
static void dn2cpp_validate_open_arguments(const Dn2CppMethodGenericParameter* parameters,
    const std::vector<const Dn2CppTypeInfo*>& arguments, const std::vector<const Dn2CppTypeInfo*>& types,
    const std::vector<const Dn2CppTypeInfo*>& methods);

Dn2CppType* dn2cpp_type_make_generic(Dn2CppType* def, Dn2CppArrayRef* args)
{
    const Dn2CppTypeInfo* defTi = dn2cpp_type_require(def);
    if (args == nullptr)
        dn2cpp_throw_argument_null_param("typeArguments");
    if ((defTi->flags & DN2CPP_TF_GENERICDEF) == 0)
        dn2cpp_throw_sr1(&dn2cpp_invalid_operation_exception_type,
            DN2CPP_SR_NOT_GENERIC_TYPE_DEFINITION, dn2cpp_type_tostring(defTi));
    int32_t argc = args->length;
    const auto* genericMetadata = defTi->reflection().genericMetadata;
    if (genericMetadata != nullptr && argc != genericMetadata->parameterCount)
        dn2cpp_throw_argument_param(DN2CPP_SR_GENERIC_ARGS_COUNT, "typeArguments");
    for (int32_t i = 0; i < argc; i++)
        if (args->data[i] == nullptr)
            dn2cpp_throw_argument_null();
    // Keep symbolic arguments out of the executable AOT/template lookup.
    std::vector<const Dn2CppTypeInfo*> argv(static_cast<size_t>(argc), nullptr);
    bool haveArgs = true;
    bool open = false;
    bool metadataOnly = false;
    for (int32_t i = 0; i < argc; i++)
    {
        auto* a = reinterpret_cast<Dn2CppType*>(args->data[i]);
        if (a->typeInfo == nullptr) { haveArgs = false; continue; }
        if (a->typeInfo == &dn2cpp_void_type || dn2cpp_signature_kind(a->typeInfo) != 0)
            dn2cpp_throw_sr1(&dn2cpp_argument_exception_type,
                DN2CPP_SR_NEVER_VALID_GENERIC_ARGUMENT, dn2cpp_type_tostring(a->typeInfo));
        open |= dn2cpp_type_contains_generic_parameters(a->typeInfo);
        metadataOnly |= dn2cpp_type_metadata_only(a->typeInfo);
        argv[i] = a->typeInfo;
    }
    if (open)
        return dn2cpp_get_type_from_handle(dn2cpp_open_generic_type(defTi, argv));
    if (metadataOnly)
        dn2cpp_throw_platform_not_supported("Reflection: generic type argument has no runtime layout");
    if (const Dn2CppTypeInfo* cand = dn2cpp_find_aot_instantiation(defTi, argv.data(), argc))
        return dn2cpp_get_type_from_handle(cand);
    // No AOT-generated candidate: the typeof-only template fallback, when the
    // emitter shipped one for this definition.
    if (haveArgs)
        if (const Dn2CppTypeInfo* made = dn2cpp_try_synthesize_generic(defTi, argv.data(), argc))
            return dn2cpp_get_type_from_handle(made);
    // No AOT-generated candidate. The catch site (a deserializer's contract
    // resolver, typically) is far from the cause, so the message names WHICH
    // instantiation is missing and what would make it exist.
    std::string closed = defTi->name != nullptr ? defTi->name : "<unnamed generic definition>";
    closed += "[";
    for (int32_t i = 0; i < argc; i++)
    {
        if (i > 0)
            closed += ",";
        closed += argv[i] != nullptr ? dn2cpp_utf8_of(dn2cpp_type_tostring(argv[i])) : "<null>";
    }
    if (dn2cpp_definition_is_one_runtime_type(defTi))
        dn2cpp_throw_unretained(defTi, closed + "]", kDn2CppUnretainedOneType, nullptr, kDn2CppUnretainedOneTypeFix);
    dn2cpp_throw_unretained(defTi, closed + "]", kDn2CppUnretainedClosedGeneric, nullptr, nullptr);
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
// dn2cpp_array_set_nongeneric_interfaces in dn2cpp_core.h), plus the relation rows of a
// type-info whose own table cannot carry them (dn2cpp_set_relation_rows). All three
// enumerating readers below go through this one iterator, so they cannot disagree about
// what the interface list IS.
//
// Two exclusions: a canonical ALIAS row is a dispatch handle, not a CLR type, so it
// never reaches a managed Type; and a relation or shared array row is dropped when an
// earlier source already carries it (an SZArray map carries IEnumerable/ICollection/
// IList, String's dispatch map its dispatched interfaces, System.Array's relation rows
// all six shared array rows).
// Hence the count is derived by COUNTING rather than read off interfaceCount — alias
// rows interleave with the real ones in the string and SZArray dispatch maps, so "the
// real rows are a prefix" does not hold.
namespace
{
struct Dn2CppEffectiveItfs
{
    const Dn2CppInterfaceEntry* own;
    int32_t ownCount;
    const Dn2CppInterfaceEntry* extra;
    int32_t extraCount;
    const Dn2CppInterfaceEntry* rel;
    int32_t relCount;

    explicit Dn2CppEffectiveItfs(const Dn2CppTypeInfo* type)
        : own(nullptr), ownCount(0), extra(nullptr), extraCount(0), rel(nullptr), relCount(0)
    {
        own = dn2cpp_type_interfaces(type, &ownCount);
        extra = dn2cpp_array_nongeneric_interfaces(type, &extraCount);
        rel = dn2cpp_relation_interfaces(type, &relCount);
    }

    bool OwnRowCarries(const Dn2CppTypeInfo* itf) const
    {
        for (int32_t i = 0; i < ownCount; i++)
            if (own[i].itf == itf)
                return true;
        return false;
    }

    bool RelationRowCarries(const Dn2CppTypeInfo* itf) const
    {
        for (int32_t i = 0; i < relCount; i++)
            if (rel[i].itf == itf)
                return true;
        return false;
    }

    // Calls fn(itf) once per reported interface: table order, then relation order (the
    // CLR type's own metadata order), then extra order.
    template <typename F> void ForEach(F fn) const
    {
        for (int32_t i = 0; i < ownCount; i++)
            if (!dn2cpp_itf_row_is_canonical_alias(own[i]) && own[i].itf != nullptr)
                fn(own[i].itf);
        for (int32_t i = 0; i < relCount; i++)
            if (rel[i].itf != nullptr && !OwnRowCarries(rel[i].itf))
                fn(rel[i].itf);
        for (int32_t i = 0; i < extraCount; i++)
            if (extra[i].itf != nullptr && !OwnRowCarries(extra[i].itf)
                && !RelationRowCarries(extra[i].itf))
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
// precisely so this and IsAssignableFrom answer — plus the relation rows and the shared
// array rows above.
Dn2CppArrayRef* dn2cpp_type_get_interfaces(Dn2CppType* t)
{
    const Dn2CppTypeInfo* ti = dn2cpp_type_require(t);
    dn2cpp_type_ensure_metadata_relations(ti);
    Dn2CppEffectiveItfs eff(ti);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(eff.Count());
    int32_t k = 0;
    eff.ForEach([&](const Dn2CppTypeInfo* itf) {
        dn2cpp_gc_store_ref(&arr->data[k++],
            reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(itf)));
    });
    return arr;
}

// AmbiguousMatchException as .NET's reflection lookups raise it, with its HResult
// (COR_E_AMBIGUOUSMATCH). A null text (no CoreLib resources) keeps the default message.
[[noreturn]] static void dn2cpp_throw_ambiguous_text(Dn2CppString* message)
{
    Dn2CppObject* e = dn2cpp_exception_new(&dn2cpp_ambiguous_match_exception_type,
        message != nullptr ? message : dn2cpp_default_message(&dn2cpp_ambiguous_match_exception_type),
        nullptr);
    reinterpret_cast<Dn2CppExceptionObject*>(e)->hresult = static_cast<int32_t>(0x8000211Du);
    dn2cpp_throw(e);
}

// A member lookup names its first match after that member's DeclaringType, both as
// the reflection surface answers them (a Type's DeclaringType is null, so an
// interface reads as "' Ns.IFoo'").
[[noreturn]] static void dn2cpp_throw_ambiguous_member(Dn2CppObject* member)
{
    Dn2CppType* declaring = dn2cpp_memberinfo_declaring_type(member);
    Dn2CppString* names[2] = {
        declaring != nullptr ? dn2cpp_type_tostring(declaring->typeInfo) : nullptr,
        dn2cpp_object_tostring_virtual(member) };
    dn2cpp_throw_ambiguous_text(dn2cpp_sr_message(DN2CPP_SR_AMBIGUOUS_MATCH_MEMBER, names, 2));
}

// A single-attribute getter names the first matching attribute by its ToString.
[[noreturn]] static void dn2cpp_throw_ambiguous_attribute(Dn2CppObject* attribute)
{
    Dn2CppString* name = dn2cpp_object_tostring_virtual(attribute);
    dn2cpp_throw_ambiguous_text(dn2cpp_sr_message(DN2CPP_SR_AMBIGUOUS_MATCH_ATTRIBUTE, &name, 1));
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
        dn2cpp_throw_argument_null_param("fullname");
    if (name->length == 0)
        return nullptr;
    dn2cpp_type_ensure_metadata_relations(ti);
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
            dn2cpp_throw_ambiguous_member(reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(found)));
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
        const char* owner = dn2cpp_type_registry[k].type->reflection().assemblyName;
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
            dn2cpp_gc_store_ref(&arr->data[i++], reinterpret_cast<Dn2CppObject*>(
                dn2cpp_get_type_from_handle(dn2cpp_type_registry[k].type)));
    return arr;
}

// RuntimeHelpers.Box(ref byte, RuntimeTypeHandle): box the value at `value` under the
// handle's own type. The width must be read here because the handle is a run-time
// value — a fixed sizeof(int32_t) at the call site truncates a long/ulong-underlying
// enum. The value sits at its type's own width, as an array element does, so it boxes
// as Array.GetValue boxes one: a small primitive or enum widened to the box payload, a
// Nullable<T> as null or its T. A reference type's `ref byte` is the reference slot,
// which .NET returns unboxed. A by-ref-like type and void are refused before the
// reference is read.
Dn2CppObject* dn2cpp_box_by_handle(const Dn2CppTypeInfo* ti, const void* value)
{
    if (ti == nullptr)
        dn2cpp_throw_argument_null_param("type");
    if (dn2cpp_signature_kind(ti) != 0)
        dn2cpp_throw_argument_msg("Type is not supported.");
    if (dn2cpp_type_contains_generic_parameters(ti))
        dn2cpp_throw_sr0(&dn2cpp_argument_exception_type, DN2CPP_SR_TYPE_NOT_SUPPORTED);
    if (dn2cpp_type_metadata_only(ti))
        dn2cpp_throw_platform_not_supported("Reflection: the constraint type has no runtime layout");
    if ((ti->flags & DN2CPP_TF_BYREFLIKE) != 0)
        dn2cpp_throw_not_supported_msg("Cannot create boxed ByRef-like values.");
    if (ti == &dn2cpp_void_type)
        dn2cpp_throw_argument_msg("Type is not supported.");
    if (value == nullptr)
        dn2cpp_throw_null_reference();  // a null `ref byte` — .NET faults, it does not box
    if ((ti->flags & DN2CPP_TF_VALUETYPE) == 0)
        return *static_cast<Dn2CppObject* const*>(value);
    dn2cpp_require_layout(ti);
    return dn2cpp_array_box_element(ti, value, ti->instanceSize > 0 ? ti->instanceSize : 0, false);
}

// RuntimeHelpers.GetUninitializedObject(Type): a zeroed instance with the type
// header planted and no constructor run. Mirrors newobj's allocation (including
// finalizer registration) — GodotSharp's script-instance bridge constructs the
// object this way and invokes the constructor separately.
Dn2CppObject* dn2cpp_get_uninitialized_object(Dn2CppType* t)
{
    if (t == nullptr)
        dn2cpp_throw_argument_null_param("type");
    const Dn2CppTypeInfo* ti = t->typeInfo;
    if (dn2cpp_signature_kind(ti) != 0)
        dn2cpp_throw_argument_msg("Type is not supported.");
    if ((ti->flags & DN2CPP_TF_GENERICPARAM) == 0 && dn2cpp_type_contains_generic_parameters(ti))
        dn2cpp_throw_member_access("Cannot create an instance of a type with unbound generic parameters.");
    if ((ti->flags & DN2CPP_TF_GENERICPARAM) != 0)
        dn2cpp_throw_sr0(&dn2cpp_argument_exception_type, DN2CPP_SR_TYPE_NOT_SUPPORTED);
    if (dn2cpp_type_metadata_only(ti))
        dn2cpp_throw_platform_not_supported("Reflection: the constraint type has no runtime layout");
    if ((ti->flags & DN2CPP_TF_BYREFLIKE) != 0)
        dn2cpp_throw_not_supported_msg("Cannot create boxed ByRef-like values.");
    if (dn2cpp_unallocated(ti))
        dn2cpp_throw_unallocated(ti, dn2cpp_utf8_of(dn2cpp_type_tostring(ti)), "GetUninitializedObject");
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

static Dn2CppObject* dn2cpp_enum_box(const Dn2CppTypeInfo* ti, int64_t v)
{
    if (dn2cpp_enum_is_wide(ti))
        return dn2cpp_box(ti, &v, sizeof(int64_t));
    int32_t n = static_cast<int32_t>(v);
    return dn2cpp_box(ti, &n, sizeof(int32_t));
}

// The enumType check of Enum's Type-taking statics: null is ArgumentNullException, a
// non-enum ArgumentException, both naming `enumType`. The Type.GetEnum* queries reject
// a non-enum the same way, after their receiver's NullReferenceException check.
static const Dn2CppTypeInfo* dn2cpp_enum_type_require(Dn2CppType* t)
{
    if (t == nullptr)
        dn2cpp_throw_argument_null_param("enumType");
    if ((t->typeInfo->flags & DN2CPP_TF_ENUM) == 0)
        dn2cpp_throw_argument_param(DN2CPP_SR_MUST_BE_ENUM, "enumType");
    return t->typeInfo;
}

// Enum.GetUnderlyingType(Type): the enum's underlying primitive type (defaulting to
// Int32 when the metadata is absent).
Dn2CppType* dn2cpp_enum_get_underlying_type(Dn2CppType* t)
{
    const Dn2CppTypeInfo* u = dn2cpp_enum_type_require(t)->enumUnderlying;
    return dn2cpp_get_type_from_handle(u != nullptr ? u : &dn2cpp_int32_type);
}

Dn2CppType* dn2cpp_type_get_enum_underlying(Dn2CppType* t)
{
    dn2cpp_type_require(t);
    return dn2cpp_enum_get_underlying_type(t);
}

Dn2CppArrayRef* dn2cpp_type_get_enum_names(Dn2CppType* t)
{
    dn2cpp_type_require(t);
    return dn2cpp_enum_get_names(t);
}

// Enum.GetNames(Type): the member names (in the table's pre-sorted order) as a string[].
Dn2CppArrayRef* dn2cpp_enum_get_names(Dn2CppType* t)
{
    const Dn2CppTypeInfo* ti = dn2cpp_enum_type_require(t);
    const auto reflection = ti->reflection();
    int32_t n = reflection.enumMemberCount;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
    {
        const char* name = reflection.enumMembers[i]->name;
        dn2cpp_gc_store_ref(&arr->data[i], reinterpret_cast<Dn2CppObject*>(
            dn2cpp_string_from_utf8(name, static_cast<int32_t>(std::strlen(name)))));
    }
    return arr;
}

// Enum.GetValues(Type): the declared values (pre-sorted order) as a reference array of
// boxed enums (object[]) — the non-generic form returns System.Array, so a foreach over it
// (lowered to System.Array.GetEnumerator) yields boxed `object` elements. Each element is
// the int32 value boxed as the enum's own type-info, so it unboxes back to the enum and
// ToStrings to its member name (the generic Enum.GetValues<T>() -> T[] form is handled separately).
Dn2CppArrayRef* dn2cpp_enum_get_values_boxed(Dn2CppType* t)
{
    const Dn2CppTypeInfo* ti = dn2cpp_enum_type_require(t);
    const auto reflection = ti->reflection();
    int32_t n = reflection.enumMemberCount;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&arr->data[i], dn2cpp_enum_box(ti, reflection.enumMembers[i]->value));
    return arr;
}

// Type.GetEnumValues keeps every literal, including aliases that the name lookup
// table deduplicates. Array.CreateInstance supplies the precise enum array identity
// and packed underlying storage, including for a runtime-only element type.
Dn2CppObject* dn2cpp_type_get_enum_values(Dn2CppType* t)
{
    const Dn2CppTypeInfo* ti = dn2cpp_type_require(t);
    if ((ti->flags & DN2CPP_TF_ENUM) == 0)
        dn2cpp_throw_argument_param(DN2CPP_SR_MUST_BE_ENUM, "enumType");
    int32_t width;
    bool isUnsigned;
    enum_underlying_shape(ti, &width, &isUnsigned);
    uint64_t mask = width == 8 ? UINT64_MAX : (uint64_t{1} << (width * 8)) - 1;
    std::vector<uint64_t> values;
    const auto reflection = ti->reflection();
    for (int32_t i = 0; i < reflection.fieldCount; i++)
    {
        const auto field = reflection.fields[i].operator->();
        if ((field->attrs & DN2CPP_FLDA_LITERAL) != 0)
            values.push_back(static_cast<uint64_t>(field->literalValue) & mask);
    }
    std::sort(values.begin(), values.end());
    int32_t count = static_cast<int32_t>(values.size());
    Dn2CppObject* result = dn2cpp_array_create_instance(t, &count, 1);
    char* data = width == 4
        ? reinterpret_cast<char*>(reinterpret_cast<Dn2CppArrayI4*>(result)->data)
        : reinterpret_cast<Dn2CppArrayN*>(result)->data;
    for (int32_t i = 0; i < count; i++)
        std::memcpy(data + static_cast<size_t>(i) * width, &values[i], static_cast<size_t>(width));
    return result;
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
    const Dn2CppTypeInfo* ti = dn2cpp_enum_type_require(t);
    const Dn2CppTypeInfo* underlyingTi = ti->enumUnderlying != nullptr
        ? ti->enumUnderlying : &dn2cpp_int32_type;
    const auto reflection = ti->reflection();
    int32_t n = reflection.enumMemberCount;
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
        int64_t v = reflection.enumMembers[i]->value;
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

// The private RuntimeType/UInt64 overload consumes the source bits before narrowing.
Dn2CppString* dn2cpp_enum_get_name_u64(Dn2CppType* t, uint64_t value)
{
    dn2cpp_type_require(t);
    const Dn2CppTypeInfo* ti = dn2cpp_enum_type_require(t);
    int32_t width;
    bool isUnsigned;
    enum_underlying_shape(ti, &width, &isUnsigned);
    if (width < 8)
    {
        if (isUnsigned)
        {
            if (value > ((uint64_t{1} << (width * 8)) - 1)) return nullptr;
        }
        else
        {
            int64_t signedValue;
            std::memcpy(&signedValue, &value, sizeof(signedValue));
            int64_t limit = int64_t{1} << (width * 8 - 1);
            if (signedValue < -limit || signedValue >= limit) return nullptr;
        }
    }
    const auto reflection = ti->reflection();
    for (int32_t i = 0; i < reflection.enumMemberCount; i++)
    {
        const auto member = reflection.enumMembers[i].operator->();
        // Sub-64-bit enum metadata stores an int32 model value, including UInt32.
        uint64_t memberValue = static_cast<uint64_t>(member->value);
        if (isUnsigned && width < 8) memberValue &= (uint64_t{1} << (width * 8)) - 1;
        if (memberValue == value)
            return dn2cpp_string_from_utf8(member->name,
                static_cast<int32_t>(std::strlen(member->name)));
    }
    return nullptr;
}

// Shared core for the non-generic Enum.Parse/TryParse(Type, …) over a non-null string.
// Validates the TYPE (dn2cpp_enum_type_require — .NET's ValidateRuntimeType does this in
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
    const Dn2CppTypeInfo* ti = dn2cpp_enum_type_require(t);
    const auto reflection = ti->reflection();
    int32_t n = reflection.enumMemberCount;
    auto* values = static_cast<int64_t*>(dn2cpp_alloc(sizeof(int64_t) * (n > 0 ? n : 1)));
    auto* names = static_cast<Dn2CppString**>(dn2cpp_alloc(sizeof(Dn2CppString*) * (n > 0 ? n : 1)));
    for (int32_t i = 0; i < n; i++)
    {
        const auto member = reflection.enumMembers[i].operator->();
        values[i] = member->value;
        dn2cpp_gc_store_ref(&names[i], dn2cpp_string_from_utf8(member->name,
            static_cast<int32_t>(std::strlen(member->name))));
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
// OverflowException. A null string is rejected before the type, as .NET does.
Dn2CppObject* dn2cpp_enum_parse_type(Dn2CppType* t, Dn2CppString* s, int32_t ignoreCase)
{
    if (s == nullptr)
        dn2cpp_throw_argument_null_param("value");
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
// name, or a null string). The TYPE is still validated (null / non-enum throw) unless
// the string is null, exactly as .NET's non-generic TryParse does — only the value parse
// is non-throwing, an out-of-range numeric token included (null overflow sink: it is
// just another miss here).
int32_t dn2cpp_enum_try_parse_type(Dn2CppType* t, Dn2CppString* s, int32_t ignoreCase,
    Dn2CppObject** out)
{
    if (s == nullptr)
    {
        *out = nullptr;
        return 0;
    }
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
    const auto reflection = ti->reflection();
    int32_t n = reflection.enumMemberCount;
    std::string out;
    bool firstTime = true;
    for (int32_t i = n - 1; i >= 0; i--)
    {
        const auto member = reflection.enumMembers[i].operator->();
        uint64_t mv = static_cast<uint64_t>(member->value) & mask;
        if (i == 0 && mv == 0)
            break;
        if (mv != 0 && (result & mv) == mv)
        {
            result -= mv;
            if (firstTime) { out = member->name; firstTime = false; }
            else out = std::string(member->name) + ", " + out;
        }
    }
    if (result != 0)
        return nullptr; // unaccounted bits: fall back to decimal
    if (saveResult == 0)
    {
        if (n > 0)
        {
            const auto member = reflection.enumMembers[0].operator->();
            if ((static_cast<uint64_t>(member->value) & mask) == 0)
                return dn2cpp_string_from_utf8(member->name,
                    static_cast<int32_t>(std::strlen(member->name)));
        }
        return dn2cpp_string_from_utf8("0", 1);
    }
    return dn2cpp_string_from_utf8(out.c_str(), static_cast<int32_t>(out.size()));
}

} // namespace

// Enum.Format(Type, object value, string format): the static enum formatter
// EnumConverter.ConvertTo drives. See the header for the per-specifier contract.
Dn2CppString* dn2cpp_enum_format(Dn2CppType* t, Dn2CppObject* value, Dn2CppString* format)
{
    // .NET checks value and format for null before the type.
    if (value == nullptr)
        dn2cpp_throw_argument_null_param("value");
    if (format == nullptr)
        dn2cpp_throw_argument_null_param("format");
    const Dn2CppTypeInfo* ti = dn2cpp_enum_type_require(t);
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
            const auto reflection = ti->reflection();
            for (int32_t i = 0; i < reflection.enumMemberCount; i++)
            {
                const auto member = reflection.enumMembers[i].operator->();
                if ((static_cast<uint64_t>(member->value) & mask) == uv)
                    return dn2cpp_string_from_utf8(member->name,
                        static_cast<int32_t>(std::strlen(member->name)));
            }
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
                                   Dn2CppItfCharSpan fmt, Dn2CppObject*)
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
    const Dn2CppTypeInfo* ti = dn2cpp_enum_type_require(t);
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
    int32_t n = ti->reflection().nestedCount;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&arr->data[i],
            reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(ti->reflection().nestedTypes[i])));
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
    for (int32_t i = 0; i < ti->reflection().nestedCount; i++)
    {
        // A nested type-info's name is its full CLR reflection name
        // ("Ns.Outer+Inner"); GetNestedType matches on the simple-name tail.
        if (dn2cpp_ascii_str_eq(dn2cpp_simple_type_name(ti->reflection().nestedTypes[i]->name), name))
            return dn2cpp_get_type_from_handle(ti->reflection().nestedTypes[i]);
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

static char dn2cpp_type_name_fold(char c)
{
    return c >= 'A' && c <= 'Z' ? static_cast<char>(c - 'A' + 'a') : c;
}

static bool dn2cpp_type_name_eq(const char* left, const char* right, size_t len, bool ignoreCase)
{
    for (size_t i = 0; i < len; i++)
    {
        if (left[i] == '\0')
            return false;
        if ((ignoreCase ? dn2cpp_type_name_fold(left[i]) : left[i])
            != (ignoreCase ? dn2cpp_type_name_fold(right[i]) : right[i]))
            return false;
    }
    return left[len] == '\0';
}

static bool dn2cpp_type_assembly_eq(const char* left, const char* right)
{
    for (; *left != '\0' && *right != '\0'; left++, right++)
        if (dn2cpp_type_name_fold(*left) != dn2cpp_type_name_fold(*right))
            return false;
    return *left == '\0' && *right == '\0';
}

static bool dn2cpp_type_parent_eq(const char* candidate, const char* parentName, size_t parentLen)
{
    return parentName == nullptr
        || (std::strncmp(candidate, parentName, parentLen) == 0 && candidate[parentLen] == '+');
}

static const Dn2CppTypeInfo* dn2cpp_type_registry_find_in_assembly(const char* name, size_t len,
    int32_t ignoreCase, const char* assembly, const char* parentName,
    const char* parentAssembly, int32_t depth, bool canonicalParentsOnly)
{
    if (depth > 16)
        return nullptr;
    size_t parentLen = parentName == nullptr ? 0 : std::strlen(parentName);
    const Dn2CppTypeInfo* found = nullptr;
    int32_t firstToken = INT32_MAX;
    int32_t lastRank = -1;
    for (int32_t i = 0; i < dn2cpp_type_registry_count; i++)
    {
        const Dn2CppTypeRegEntry& row = dn2cpp_type_registry[i];
        if (!dn2cpp_type_assembly_eq(dn2cpp_ti_assembly_name(row.type), assembly)
            || (parentAssembly != nullptr
                && !dn2cpp_type_assembly_eq(dn2cpp_ti_assembly_name(row.type), parentAssembly))
            || (canonicalParentsOnly && ignoreCase != 0 && !row.caseFoldPathWinner)
            || !dn2cpp_type_parent_eq(row.name, parentName, parentLen)
            || !dn2cpp_type_name_eq(row.name, name, len, ignoreCase != 0))
            continue;
        int32_t token = row.type->reflection().metadataToken;
        if (found == nullptr || row.caseFoldRank > lastRank
            || (row.caseFoldRank == lastRank && token > 0 && token < firstToken))
        {
            found = row.type;
            firstToken = token > 0 ? token : INT32_MAX;
            lastRank = row.caseFoldRank;
        }
    }
    if (found == nullptr)
        for (const Dn2CppDynTypeReg* e = dn2cpp_type_registry_dynamic_head(); e != nullptr; e = e->next)
            if (dn2cpp_type_assembly_eq(dn2cpp_ti_assembly_name(e->type), assembly)
                && (parentAssembly == nullptr
                    || dn2cpp_type_assembly_eq(dn2cpp_ti_assembly_name(e->type), parentAssembly))
                && dn2cpp_type_parent_eq(e->name, parentName, parentLen)
                && dn2cpp_type_name_eq(e->name, name, len, ignoreCase != 0))
                return e->type;
    const Dn2CppTypeForwarder* forward = nullptr;
    int32_t forwardRank = -1;
    const Dn2CppTypeForwarder* forwarders = dn2cpp_type_registry_count == 0
        ? nullptr : dn2cpp_type_registry[0].forwarders;
    int32_t forwarderCount = dn2cpp_type_registry_count == 0
        ? 0 : dn2cpp_type_registry[0].forwarderCount;
    for (int32_t i = 0; i < forwarderCount; i++)
    {
        const Dn2CppTypeForwarder& row = forwarders[i];
        if (!dn2cpp_type_assembly_eq(row.sourceAssembly, assembly)
            || (parentAssembly != nullptr
                && !dn2cpp_type_assembly_eq(row.sourceAssembly, parentAssembly))
            || (canonicalParentsOnly && ignoreCase != 0 && !row.caseFoldPathWinner)
            || !dn2cpp_type_parent_eq(row.name, parentName, parentLen)
            || !dn2cpp_type_name_eq(row.name, name, len, ignoreCase != 0))
            continue;
        if (forward == nullptr || row.caseFoldRank > forwardRank)
        {
            forward = &row;
            forwardRank = row.caseFoldRank;
        }
    }
    if (forward == nullptr || forwardRank <= lastRank
        || dn2cpp_type_assembly_eq(forward->targetAssembly, assembly))
        return found;
    return dn2cpp_type_registry_find_in_assembly(name, len, ignoreCase,
        forward->targetAssembly, parentName, parentAssembly, depth + 1, canonicalParentsOnly);
}

static const Dn2CppTypeInfo* dn2cpp_type_registry_find_scoped(const char* name, size_t len,
    int32_t ignoreCase, const char* callerAssembly, const char* requestedAssembly,
    const char* parentName = nullptr, const char* parentAssembly = nullptr,
    bool canonicalParentsOnly = false)
{
    if (std::memchr(name, '\0', len) != nullptr)
        return nullptr;
    if (callerAssembly == nullptr)
        return dn2cpp_type_registry_find(name, static_cast<int32_t>(len));
    if (requestedAssembly != nullptr)
        return dn2cpp_type_registry_find_in_assembly(name, len, ignoreCase,
            requestedAssembly, parentName, parentAssembly, 0, canonicalParentsOnly);
    if (const Dn2CppTypeInfo* local = dn2cpp_type_registry_find_in_assembly(name, len,
            ignoreCase, callerAssembly, parentName, parentAssembly, 0, canonicalParentsOnly))
        return local;
    return dn2cpp_type_assembly_eq(callerAssembly, "System.Private.CoreLib")
        ? nullptr : dn2cpp_type_registry_find_in_assembly(name, len, ignoreCase,
            "System.Private.CoreLib", parentName, parentAssembly, 0, canonicalParentsOnly);
}

static const Dn2CppTypeInfo* dn2cpp_type_registry_find_nested(const char* name, size_t len,
    int32_t ignoreCase, const char* callerAssembly, const char* requestedAssembly)
{
    if (callerAssembly == nullptr || std::memchr(name, '[', len) != nullptr)
        return dn2cpp_type_registry_find_scoped(name, len, ignoreCase, callerAssembly, requestedAssembly);
    const char* plus = static_cast<const char*>(std::memchr(name, '+', len));
    if (plus == nullptr)
        return dn2cpp_type_registry_find_scoped(name, len, ignoreCase, callerAssembly, requestedAssembly);
    size_t cut = static_cast<size_t>(plus - name);
    const Dn2CppTypeInfo* chosen = dn2cpp_type_registry_find_scoped(
        name, cut, ignoreCase, callerAssembly, requestedAssembly);
    while (chosen != nullptr && cut < len)
    {
        const char* parentName = nullptr;
        for (int32_t i = 0; i < dn2cpp_type_registry_count; i++)
            if (dn2cpp_type_registry[i].type == chosen)
            {
                parentName = dn2cpp_type_registry[i].name;
                break;
            }
        if (parentName == nullptr)
            return nullptr;
        const char* next = static_cast<const char*>(std::memchr(name + cut + 1, '+', len - cut - 1));
        cut = next == nullptr ? len : static_cast<size_t>(next - name);
        const char* definingAssembly = dn2cpp_ti_assembly_name(chosen);
        chosen = dn2cpp_type_registry_find_scoped(name, cut, ignoreCase,
            callerAssembly, definingAssembly, parentName, definingAssembly);
    }
    if (chosen != nullptr)
        return chosen;
    return dn2cpp_type_registry_find_scoped(name, len, ignoreCase,
        callerAssembly, requestedAssembly, nullptr, nullptr, ignoreCase != 0);
}

// Resolves a CLR type name, accepting a plain registry name and the composed
// closed-generic spellings dn2cpp_type_tostring/dn2cpp_type_fullname produce.
// NO registry entry carries the composed form — a closed instantiation is keyed by
// its dn2cpp-mangled name — so it resolves structurally instead, on the
// MakeGenericType relation. That is what keeps Type.GetType(t.FullName) and the
// AssemblyQualifiedName round-trip (a serializer's `$type`) working.
static const Dn2CppTypeInfo* dn2cpp_resolve_type_name(const char* s, size_t len, int depth = 0,
    int32_t ignoreCase = 0, const char* callerAssembly = nullptr,
    const char* forcedAssembly = nullptr)
{
    // The nesting a REAL name can carry is the type graph's, which is shallow; the
    // string is caller-supplied, so cap the recursion rather than the input.
    if (depth > 16)
        return nullptr;
    while (len > 0 && s[0] == ' ') { s++; len--; }
    size_t comma = dn2cpp_name_top_comma(s, len);
    std::string requestedAssembly;
    const char* requested = forcedAssembly;
    if (callerAssembly != nullptr && comma < len)
    {
        size_t begin = comma + 1;
        while (begin < len && s[begin] == ' ') begin++;
        size_t end = begin;
        while (end < len && s[end] != ',') end++;
        while (end > begin && s[end - 1] == ' ') end--;
        requestedAssembly.assign(s + begin, end - begin);
        if (requestedAssembly.empty())
            return nullptr;
        requested = requestedAssembly.c_str();
    }
    len = comma;
    while (len > 0 && s[len - 1] == ' ') len--;
    if (len == 0)
        return nullptr;
    if (callerAssembly != nullptr && s[len - 1] == ']')
    {
        size_t openArray = len;
        while (openArray > 0 && s[openArray - 1] != '[')
            openArray--;
        if (openArray > 0 && openArray < len)
        {
            size_t bracket = openArray - 1;
            int32_t rank = 1;
            bool arraySuffix = true;
            bool nonSz = false;
            for (size_t i = openArray; i + 1 < len; i++)
            {
                if (s[i] == ',') rank++;
                else if (s[i] == '*' && i + 2 == len) nonSz = true;
                else { arraySuffix = false; break; }
            }
            if (arraySuffix)
            {
                const Dn2CppTypeInfo* element = dn2cpp_resolve_type_name(
                    s, bracket, depth + 1, ignoreCase, callerAssembly, requested);
                if (element == nullptr)
                    return nullptr;
                for (int32_t i = 0; i < dn2cpp_type_registry_count; i++)
                {
                    const Dn2CppTypeInfo* candidate = dn2cpp_type_registry[i].type;
                    if ((candidate->flags & DN2CPP_TF_ARRAY) != 0
                        && candidate->elementType == element && candidate->arrayRank == rank
                        && ((candidate->flags & DN2CPP_TF_NON_SZ_ARRAY) != 0) == nonSz)
                        return candidate;
                }
                return nullptr;
            }
        }
    }
    // A registry name wins outright: every non-generic type, every array and every
    // closed instantiation is registered under one, so nothing below can perturb a
    // lookup that already resolved.
    if (const Dn2CppTypeInfo* direct = dn2cpp_type_registry_find_nested(
            s, len, ignoreCase, callerAssembly, requested))
        return direct;
    const char* open = static_cast<const char*>(std::memchr(s, '[', len));
    // "[]" is an array suffix, not an argument list.
    if (open == nullptr || open + 1 >= s + len || open[1] == ']')
        return nullptr;
    size_t p = static_cast<size_t>(open - s);
    size_t close = dn2cpp_name_bracket_end(s, len, p);
    if (close >= len)
        return nullptr;
    const Dn2CppTypeInfo* def = dn2cpp_type_registry_find_nested(
        s, p, ignoreCase, callerAssembly, requested);
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
        const Dn2CppTypeInfo* at = dn2cpp_resolve_type_name(a, alen, depth + 1, ignoreCase, callerAssembly);
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
static const Dn2CppTypeInfo* dn2cpp_resolve_managed_type_name(const Dn2CppString* name,
    int32_t ignoreCase = 0, const char* callerAssembly = nullptr)
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
        representable ? dn2cpp_resolve_type_name(buf, static_cast<size_t>(len), 0,
            ignoreCase, callerAssembly) : nullptr;
    if (buf != stackBuf)
        delete[] buf;
    return ti;
}

// Type.GetType(string): resolve a CLR FullName against the embedded registry
// (whose static half is hash-indexed). A trailing assembly-qualified suffix is
// ignored, matching the common "Namespace.Type[, Assembly]" usage. Returns the
// same type handle as typeof on that type (registry entries point at the shared
// Dn2CppTypeInfo), so Type.GetType("System.Int32") == typeof(int).
Dn2CppType* dn2cpp_type_get_by_name(Dn2CppString* name, int32_t throwOnError,
    int32_t ignoreCase, const char* callerAssembly)
{
    if (name != nullptr)
        if (const Dn2CppTypeInfo* ti = dn2cpp_resolve_managed_type_name(name, ignoreCase, callerAssembly))
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
#define DN2CPP_BF_DO_NOT_WRAP_EXCEPTIONS 0x02000000

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

static bool dn2cpp_member_name_matches(const char* candidate, Dn2CppString* name, int32_t flags, bool pattern);

// A method-flavored reflected handle header: MethodInfo or ConstructorInfo
// (both wrap a Dn2CppMethodRef; the distinct headers discriminate casts/`is`
// like real .NET's MethodBase split).
static bool dn2cpp_is_methodref_header(const Dn2CppTypeInfo* t)
{
    return t == &dn2cpp_methodinfo_type || t == &dn2cpp_constructorinfo_type;
}

static bool dn2cpp_native_member_header(const Dn2CppTypeInfo* type);

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

static const Dn2CppTypeInfo* dn2cpp_reflection_base(const Dn2CppTypeInfo* ti)
{
    // DeclaredOnly reads its own rows without resolving unrelated constraints.
    return dn2cpp_type_reflection_base(ti);
}

static const Dn2CppTypeInfo* dn2cpp_symbolic_member_type(const Dn2CppBindingSignature* signature,
    const Dn2CppTypeInfo* declaring);

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
static Dn2CppMemberInternMap<Dn2CppEventRef>& g_eventref_intern =
    dn2cpp_never_destroyed<Dn2CppMemberInternMap<Dn2CppEventRef>>();
static Dn2CppMemberInternMap<Dn2CppPropRef>& g_propref_intern =
    dn2cpp_never_destroyed<Dn2CppMemberInternMap<Dn2CppPropRef>>();

static Dn2CppFieldRef* dn2cpp_make_fieldref(Dn2CppMetadataHandle<Dn2CppFieldInfo> f,
                                            const Dn2CppTypeInfo* reflected)
{
    if (reflected == nullptr)
        reflected = f->declaringType;
    std::lock_guard<std::mutex> lk(g_memberref_intern_mtx);
    Dn2CppFieldRef*& slot = g_fieldref_intern[{ f.identity(), reflected }];
    if (slot == nullptr)
    {
        auto* r = new (dn2cpp_alloc_pinned(sizeof(Dn2CppFieldRef))) Dn2CppFieldRef{};
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
    for (const Dn2CppTypeInfo* ti = type; ti != nullptr; ti = dn2cpp_reflection_base(ti))
    {
        dn2cpp_require_metadata(ti);
        const auto reflection = ti->reflection();
        bool inherited = (ti != type);
        for (int32_t i = 0; i < reflection.fieldCount; i++)
        {
            const auto field = reflection.fields[i];
            const auto row = field.operator->();
            if (dn2cpp_field_matches(row.operator->(), flags, inherited))
            {
                if (out != nullptr)
                    dn2cpp_gc_store_ref(&out[n], reinterpret_cast<Dn2CppObject*>(
                        dn2cpp_make_fieldref(field, type)));
                n++;
            }
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
    for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr; ti = dn2cpp_reflection_base(ti))
    {
        dn2cpp_require_metadata(ti);
        const auto reflection = ti->reflection();
        bool inherited = (ti != t->typeInfo);
        Dn2CppMetadataHandle<Dn2CppFieldInfo> hit;
        for (int32_t i = 0; i < reflection.fieldCount; i++)
        {
            const auto field = reflection.fields[i];
            const auto row = field.operator->();
            if (dn2cpp_field_matches(row.operator->(), bindingFlags, inherited) &&
                dn2cpp_member_name_matches(row->name, name, bindingFlags, false))
            {
                if (hit != nullptr)
                    dn2cpp_throw_ambiguous_member(reinterpret_cast<Dn2CppObject*>(
                        dn2cpp_make_fieldref(hit, t->typeInfo)));
                hit = field;
            }
        }
        if (hit != nullptr)
            return dn2cpp_make_fieldref(hit, t->typeInfo);
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
static Dn2CppMetadataHandle<Dn2CppFieldInfo> dn2cpp_fieldref_require(Dn2CppFieldRef* f)
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
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_methodref_require(Dn2CppMethodRef* m)
{
    if (m == nullptr || m->method == nullptr)
        dn2cpp_throw_null_reference();
    return m->method;
}

static Dn2CppMetadataHandle<Dn2CppPropInfo> dn2cpp_propref_require(Dn2CppPropRef* p)
{
    if (p == nullptr || p->prop == nullptr)
        dn2cpp_throw_null_reference();
    return p->prop;
}

static Dn2CppMetadataHandle<Dn2CppEventInfo> dn2cpp_eventref_require(Dn2CppEventRef* event)
{
    if (event == nullptr || event->event == nullptr)
        dn2cpp_throw_null_reference();
    return event->event;
}

// The ParameterInfo guard hands back the HANDLE, not the row: Position lives on
// the handle and Member reads its owner, so callers need both halves.
static Dn2CppParamRef* dn2cpp_paramref_require(Dn2CppParamRef* p)
{
    if (p == nullptr || p->param == nullptr)
        dn2cpp_throw_null_reference();
    return p;
}

static Dn2CppMetadataHandle<Dn2CppAttrInfo> dn2cpp_attrdataref_require(Dn2CppAttrDataRef* d)
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
    const auto row = dn2cpp_fieldref_require(f).operator->();
    const auto* declaring = dn2cpp_invoke_declaring(row->declaringType, f->reflectedType);
    const auto* type = row->reflectionSignature != nullptr
        ? (row->declaringType->flags & DN2CPP_TF_GENERICDEF) != 0
            ? dn2cpp_symbolic_member_type(row->reflectionSignature, declaring)
            : dn2cpp_signature_type(row->reflectionSignature, declaring)
        : row->fieldType;
    return dn2cpp_get_type_from_handle(type);
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

static void dn2cpp_field_check_target(const Dn2CppFieldRef* f, const Dn2CppFieldInfo* row,
    Dn2CppObject* obj);
static Dn2CppObject* dn2cpp_field_check_value(const Dn2CppFieldInfo* row, Dn2CppObject* value,
    const Dn2CppTypeInfo* declaring);
static Dn2CppObject* dn2cpp_field_literal(const Dn2CppFieldInfo* row);
[[noreturn]] static void dn2cpp_field_refuse_constant();
[[noreturn]] static void dn2cpp_field_refuse_initonly(const Dn2CppFieldRef* f,
    const Dn2CppFieldInfo* row);
static Dn2CppObject* dn2cpp_invoke_box_result(const Dn2CppTypeInfo* returnType, Dn2CppObject* result);

// FieldInfo.GetValue / SetValue: dispatch to the emitter-generated typed
// thunk, which boxes/unboxes value-type fields and passes reference fields through.
// The thunk reads and writes through the receiver and the value unchecked, so both
// pass .NET's checks first, the receiver before the value. A literal answers from
// its row whatever the receiver; a field with no reflectable storage (an opaque
// declaring type) has null thunks -> InvalidOperationException.
Dn2CppObject* dn2cpp_fieldref_get_value(Dn2CppFieldRef* f, Dn2CppObject* obj)
{
    const auto row = dn2cpp_fieldref_require(f).operator->();
    if ((row->attrs & DN2CPP_FLDA_LITERAL) == 0 && dn2cpp_type_contains_generic_parameters(
        dn2cpp_invoke_declaring(row->declaringType, f->reflectedType)))
        dn2cpp_throw_invalid_operation();
    if (row->getter == nullptr)
    {
        if ((row->attrs & DN2CPP_FLDA_LITERAL) != 0)
            return dn2cpp_field_literal(row.operator->());
        dn2cpp_throw_invalid_operation();
    }
    dn2cpp_field_check_target(f, row.operator->(), obj);
    Dn2CppObject* result = row->getter(obj);
    const int32_t staticInitOnly = DN2CPP_FLDA_STATIC | DN2CPP_FLDA_INITONLY;
    if ((row->attrs & staticInitOnly) == staticInitOnly)
        f->initOnlyAccessed.store(true, std::memory_order_release);
    return dn2cpp_invoke_box_result(row->fieldType, result);
}

void dn2cpp_fieldref_set_value(Dn2CppFieldRef* f, Dn2CppObject* obj, Dn2CppObject* value)
{
    const auto row = dn2cpp_fieldref_require(f).operator->();
    if ((row->attrs & DN2CPP_FLDA_LITERAL) != 0)
        dn2cpp_field_refuse_constant();
    if (dn2cpp_type_contains_generic_parameters(dn2cpp_invoke_declaring(row->declaringType, f->reflectedType)))
        dn2cpp_throw_invalid_operation();
    const int32_t staticInitOnly = DN2CPP_FLDA_STATIC | DN2CPP_FLDA_INITONLY;
    bool initOnly = (row->attrs & staticInitOnly) == staticInitOnly;
    if (row->setter == nullptr && !initOnly)
        dn2cpp_throw_invalid_operation();
    dn2cpp_field_check_target(f, row.operator->(), obj);
    if (initOnly && f->initOnlyAccessed.load(std::memory_order_acquire))
        dn2cpp_field_refuse_initonly(f, row.operator->());
    Dn2CppObject* stored = dn2cpp_field_check_value(row.operator->(), value,
        dn2cpp_invoke_declaring(row->declaringType, f->reflectedType));
    if (initOnly)
        dn2cpp_field_refuse_initonly(f, row.operator->());
    row->setter(obj, stored);
}

// MemberInfo.Name / DeclaringType: shared by FieldInfo and Type, so dispatch on the
// managed object header. Formal parameters retain their typical declaring type;
// nested-type enclosures are not modeled.
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
    else if (m->type == &dn2cpp_eventinfo_type)
        nm = reinterpret_cast<Dn2CppEventRef*>(m)->event->name;
    if (nm != nullptr)
        return dn2cpp_string_from_utf8(nm, static_cast<int32_t>(std::strlen(nm)));
    return dn2cpp_type_name(reinterpret_cast<Dn2CppType*>(m)->typeInfo);
}

Dn2CppType* dn2cpp_memberinfo_declaring_type(Dn2CppObject* m)
{
    dn2cpp_memberref_require(m);
    if (m->type == &dn2cpp_fieldinfo_type)
    {
        auto* f = reinterpret_cast<Dn2CppFieldRef*>(m);
        return dn2cpp_get_type_from_handle(dn2cpp_invoke_declaring(f->field->declaringType, f->reflectedType));
    }
    if (dn2cpp_is_methodref_header(m->type))
    {
        auto* r = reinterpret_cast<Dn2CppMethodRef*>(m);
        return dn2cpp_get_type_from_handle(dn2cpp_invoke_declaring(r->method->declaringType, r->reflectedType));
    }
    if (m->type == &dn2cpp_propertyinfo_type)
    {
        auto* p = reinterpret_cast<Dn2CppPropRef*>(m);
        return dn2cpp_get_type_from_handle(dn2cpp_invoke_declaring(p->prop->declaringType, p->reflectedType));
    }
    if (m->type == &dn2cpp_eventinfo_type)
    {
        auto* event = reinterpret_cast<Dn2CppEventRef*>(m);
        return dn2cpp_get_type_from_handle(dn2cpp_invoke_declaring(event->event->declaringType, event->reflectedType));
    }
    if (m->type == &dn2cpp_type_type)
    {
        const auto* ti = reinterpret_cast<Dn2CppType*>(m)->typeInfo;
        if ((ti->flags & DN2CPP_TF_GENERICPARAM) != 0)
            return dn2cpp_get_type_from_handle(dn2cpp_type_generic_parameter_declaring_type(ti));
    }
    return nullptr;
}

// MemberInfo.ReflectedType: the type-info stamped on the handle at mint time
// (never null there — non-query mints normalize to the declaring type; see the
// member-handle intern note). Formal parameters use their typical declaring type;
// other Type receivers have no recorded reflected type.
Dn2CppType* dn2cpp_memberinfo_reflected_type(Dn2CppObject* m)
{
    dn2cpp_memberref_require(m);
    if (m->type == &dn2cpp_fieldinfo_type)
        return dn2cpp_get_type_from_handle(reinterpret_cast<Dn2CppFieldRef*>(m)->reflectedType);
    if (dn2cpp_is_methodref_header(m->type))
        return dn2cpp_get_type_from_handle(reinterpret_cast<Dn2CppMethodRef*>(m)->reflectedType);
    if (m->type == &dn2cpp_propertyinfo_type)
        return dn2cpp_get_type_from_handle(reinterpret_cast<Dn2CppPropRef*>(m)->reflectedType);
    if (m->type == &dn2cpp_eventinfo_type)
        return dn2cpp_get_type_from_handle(reinterpret_cast<Dn2CppEventRef*>(m)->reflectedType);
    if (m->type == &dn2cpp_type_type)
    {
        const auto* ti = reinterpret_cast<Dn2CppType*>(m)->typeInfo;
        if ((ti->flags & DN2CPP_TF_GENERICPARAM) != 0)
            return dn2cpp_get_type_from_handle(dn2cpp_type_generic_parameter_declaring_type(ti));
    }
    return nullptr;
}

// Method/field/property equality includes the metadata row and normalized reflected type.
// RuntimeType equality is object identity, including for MemberwiseClone copies.
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
    if (a->type == &dn2cpp_eventinfo_type)
        return reinterpret_cast<Dn2CppEventRef*>(a)->event == reinterpret_cast<Dn2CppEventRef*>(b)->event
            && reinterpret_cast<Dn2CppEventRef*>(a)->reflectedType == reinterpret_cast<Dn2CppEventRef*>(b)->reflectedType ? 1 : 0;
    if (a->type == &dn2cpp_propertyinfo_type)
        return reinterpret_cast<Dn2CppPropRef*>(a)->prop == reinterpret_cast<Dn2CppPropRef*>(b)->prop
            && reinterpret_cast<Dn2CppPropRef*>(a)->reflectedType
                == reinterpret_cast<Dn2CppPropRef*>(b)->reflectedType ? 1 : 0;
    return 0;
}

// ---- reflection: method enumeration ----

static Dn2CppMethodRef* dn2cpp_make_methodref(Dn2CppMetadataHandle<Dn2CppMethodInfo> mi,
                                              const Dn2CppTypeInfo* reflected)
{
    if (reflected == nullptr)
        reflected = mi->declaringType;
    std::lock_guard<std::mutex> lk(g_memberref_intern_mtx);
    Dn2CppMethodRef*& slot = g_methodref_intern[{ mi.identity(), reflected }];
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
struct Dn2CppOpenMethod
{
    Dn2CppMethodInfo row{};
    Dn2CppMetadataHandle<Dn2CppMethodInfo> definition;
    std::vector<const Dn2CppTypeInfo*> arguments;
    std::vector<Dn2CppParamInfo> parameters;
};

static std::recursive_mutex& g_open_methods_mtx = dn2cpp_never_destroyed<std::recursive_mutex>();
static auto& g_open_methods = dn2cpp_never_destroyed<std::vector<Dn2CppOpenMethod*>>();

static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_open_method_definition(
    Dn2CppMetadataHandle<Dn2CppMethodInfo> method)
{
    std::lock_guard<std::recursive_mutex> lock(g_open_methods_mtx);
    for (auto* node : g_open_methods)
        if (method.identity() == &node->row)
            return node->definition;
    return method;
}

static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_canonical_method_definition(
    Dn2CppMetadataHandle<Dn2CppMethodInfo> mi);

static Dn2CppMethodRef* dn2cpp_make_methodref_defview(Dn2CppMetadataHandle<Dn2CppMethodInfo> mi,
                                                      const Dn2CppTypeInfo* reflected)
{
    mi = dn2cpp_canonical_method_definition(mi);
    if (reflected == nullptr)
        reflected = mi->declaringType;
    std::lock_guard<std::mutex> lk(g_memberref_intern_mtx);
    Dn2CppMethodRef*& slot = g_methodref_defview_intern[{ mi.identity(), reflected }];
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

// A row of a class's generic virtual method. It has no slot, so
// the override relation is its definition's.
static bool dn2cpp_is_gvm_row(const Dn2CppMethodInfo& row)
{
    return row.genericParamCount != 0
        && (row.attrs & DN2CPP_MTHA_STATIC) == 0 && (row.ilAttrs & DN2CPP_MA_VIRTUAL) != 0;
}

// An override: virtual without a new slot.
static bool dn2cpp_is_gvm_override(const Dn2CppMethodInfo& row)
{
    return dn2cpp_is_gvm_row(row) && (row.ilAttrs & DN2CPP_MA_NEWSLOT) == 0;
}

static bool dn2cpp_symbolic_method(const Dn2CppMethodInfo& row)
{
    return (row.declaringType->flags & DN2CPP_TF_GENERICDEF) != 0 && row.definitionSignature != nullptr;
}

static bool dn2cpp_symbolic_parameters_equal(Dn2CppMetadataHandle<Dn2CppMethodInfo> a,
    Dn2CppMetadataHandle<Dn2CppMethodInfo> b, const Dn2CppTypeInfo* reflected, bool positional);

// Whether two definitions spell one signature type, method type parameters by
// position: by key where a definition's type names one, else by the closed type
// both rows then carry.
static bool dn2cpp_definition_types_equal(const Dn2CppTypeInfo* a, const char* aKey,
                                          const Dn2CppTypeInfo* b, const char* bKey)
{
    if (aKey == nullptr || bKey == nullptr)
        return aKey == bKey && a == b;
    return std::strcmp(aKey, bKey) == 0;
}

// A class generic virtual row's slot: the new-slot definition its override chain
// starts at, the row's own for a new slot. Null type where an override row names
// none.
struct Dn2CppGvmSlot
{
    const Dn2CppTypeInfo* type;
    int32_t token;

    bool known() const { return type != nullptr && token != 0; }
    bool operator==(const Dn2CppGvmSlot& other) const { return type == other.type && token == other.token; }
};

static Dn2CppGvmSlot dn2cpp_gvm_slot(const Dn2CppMethodInfo& row)
{
    if ((row.ilAttrs & DN2CPP_MA_NEWSLOT) != 0)
        return { row.declaringType, row.metadataToken };
    const Dn2CppTypeInfo* root = row.gvmRootDepth > 0 ? row.declaringType : nullptr;
    for (int32_t step = 0; root != nullptr && step < row.gvmRootDepth; step++)
        root = root->base;
    return { root, row.gvmRootToken };
}

// Whether two generic virtual rows close definitions of one name, generic arity and
// signature: the relation by which an override replaces the base method it
// overrides, whatever instantiations the rows close.
static bool dn2cpp_gvm_same_signature(const Dn2CppMethodInfo& a, const Dn2CppMethodInfo& b)
{
    if (a.genericParamCount != b.genericParamCount || a.paramCount != b.paramCount
        || std::strcmp(a.name, b.name) != 0
        || !dn2cpp_definition_types_equal(a.returnType, a.genericDefinitionReturnKey,
            b.returnType, b.genericDefinitionReturnKey))
        return false;
    for (int32_t p = 0; p < a.paramCount; p++)
    {
        const Dn2CppParamInfo pa = *a.parameters[p];
        const Dn2CppParamInfo pb = *b.parameters[p];
        if (!dn2cpp_definition_types_equal(pa.paramType, pa.genericDefinitionKey,
                pb.paramType, pb.genericDefinitionKey))
            return false;
    }
    return true;
}

// A member lookup's working list: the first Inline entries stay on the stack and
// more spill to the heap, since a dropped entry would change the lookup's answer.
template<class T, int32_t Inline>
class Dn2CppWalkList
{
    T local_[Inline];
    std::vector<T> heap_;
    int32_t count_ = 0;

public:
    int32_t size() const { return count_; }
    const T* data() const { return heap_.empty() ? local_ : heap_.data(); }
    T& operator[](int32_t i) { return heap_.empty() ? local_[i] : heap_[i]; }
    const T& operator[](int32_t i) const { return data()[i]; }
    bool contains(const T& value) const
    {
        const T* items = data();
        for (int32_t i = 0; i < count_; i++)
            if (items[i] == value)
                return true;
        return false;
    }
    void push_back(const T& value)
    {
        if (heap_.empty() && count_ < Inline)
        {
            local_[count_++] = value;
            return;
        }
        if (heap_.empty())
            heap_.assign(local_, local_ + count_);
        heap_.push_back(value);
        count_++;
    }
    void truncate(int32_t n)
    {
        count_ = n;
        if (!heap_.empty())
            heap_.resize(static_cast<size_t>(n));
    }
};

// Slot hiding for generic virtual rows, walking derived→base: a base row is hidden
// when a more derived override takes its slot. Rows that name their slots compare
// them; otherwise a row's slot is taken by an override of its signature. The
// override goes on hiding through an overriding base row and stops at the
// introducing one. A type's own overrides start hiding with its base, so the rows
// of one definition never hide each other.
struct Dn2CppGvmHiding
{
    const Dn2CppTypeInfo* reflected = nullptr;
    struct Hider
    {
        Dn2CppMetadataHandle<Dn2CppMethodInfo> row;
        const Dn2CppTypeInfo* declaringType;
        int32_t metadataToken;
        Dn2CppGvmSlot slot;
        bool ends;
    };
    Dn2CppWalkList<Hider, 8> hiders;
    int32_t active = 0;

    bool hides(Dn2CppMetadataHandle<Dn2CppMethodInfo> mi, const Dn2CppMethodInfo& row)
    {
        const bool symbolic = dn2cpp_symbolic_method(row) && (row.ilAttrs & DN2CPP_MA_VIRTUAL) != 0;
        const bool generic = dn2cpp_is_gvm_row(row);
        if (!generic && !symbolic && (active == 0 || (row.ilAttrs & DN2CPP_MA_VIRTUAL) == 0))
            return false;
        const Dn2CppGvmSlot slot = dn2cpp_gvm_slot(row);
        for (int32_t h = 0; h < active; h++)
        {
            const bool taken = symbolic || dn2cpp_symbolic_method(*hiders[h].row)
                ? std::strcmp(hiders[h].row->name, row.name) == 0
                    && dn2cpp_symbolic_parameters_equal(hiders[h].row, mi, reflected, true)
                : hiders[h].slot.known() && slot.known()
                    ? hiders[h].slot == slot : dn2cpp_gvm_same_signature(*hiders[h].row, row);
            if (!taken)
                continue;
            if ((row.ilAttrs & DN2CPP_MA_NEWSLOT) != 0)
                hiders[h].ends = true;
            return true;
        }
        if ((!generic && !symbolic) || (row.ilAttrs & DN2CPP_MA_NEWSLOT) != 0)
            return false;
        for (int32_t h = active; h < hiders.size(); h++)
            if (hiders[h].metadataToken == row.metadataToken && hiders[h].declaringType == row.declaringType)
                return false;
        hiders.push_back({ mi, row.declaringType, row.metadataToken, slot, false });
        return false;
    }

    // Moves the walk to the next base type.
    void next_type()
    {
        int32_t kept = 0;
        for (int32_t h = 0; h < hiders.size(); h++)
            if (!hiders[h].ends)
                hiders[kept++] = hiders[h];
        hiders.truncate(kept);
        active = kept;
    }
};

// The virtual slots a derived→base walk has reported: an inherited row on one of
// them is the definition an override already stands for.
struct Dn2CppSeenSlots
{
    Dn2CppWalkList<int32_t, 64> slots;

    // Whether `slot` was reported before; records it otherwise.
    bool seen(int32_t slot)
    {
        if (slots.contains(slot))
            return true;
        slots.push_back(slot);
        return false;
    }
};

using Dn2CppMethodCandidates = Dn2CppWalkList<Dn2CppMetadataHandle<Dn2CppMethodInfo>, 16>;

static bool dn2cpp_meta_family_name(const Dn2CppMethodInfo& row);
static int32_t dn2cpp_meta_collect_family(const Dn2CppTypeInfo* type, int32_t flags,
    Dn2CppMethodCandidates& emitted, Dn2CppObject** out, Dn2CppString* name);
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_meta_object_base(
    Dn2CppMetadataHandle<Dn2CppMethodInfo> mi, const Dn2CppMethodInfo& row);


// Walk the type and its base chain (stopping at DeclaredOnly), collecting matching
// methods. A virtual method (vtableSlot >= 0) is reported only at its most-derived
// override: once a slot is seen walking derived→base, the inherited definition at the
// same slot is skipped, and a generic virtual row is hidden by Dn2CppGvmHiding.
// Non-virtual methods (slot -1, includes `new` hiding and overloads) are all kept.
// With out==nullptr only counts; otherwise fills out[].
static int32_t dn2cpp_collect_methods(const Dn2CppTypeInfo* type, int32_t flags,
    Dn2CppObject** out, Dn2CppString* name = nullptr)
{
    int32_t n = 0;
    Dn2CppSeenSlots seen;
    Dn2CppGvmHiding gvm;
    gvm.reflected = type;
    Dn2CppMethodCandidates family;
    for (const Dn2CppTypeInfo* ti = type; ti != nullptr; ti = dn2cpp_reflection_base(ti))
    {
        dn2cpp_require_metadata(ti);
        const auto reflection = ti->reflection();
        bool inherited = (ti != type);
        std::vector<int32_t> definitions;
        for (int32_t i = 0; i < reflection.methodCount; i++)
        {
            const auto mi = reflection.methods[i];
            const auto row = mi.operator->();
            if (!dn2cpp_member_matches(row->attrs, flags, inherited))
                continue;
            if (row->genericParamCount > 0 && row->metadataToken != 0)
            {
                bool duplicate = false;
                for (int32_t token : definitions)
                    if (token == row->metadataToken) { duplicate = true; break; }
                if (duplicate)
                    continue;
                definitions.push_back(row->metadataToken);
            }
            if ((row->vtableSlot >= 0 && seen.seen(row->vtableSlot)) || gvm.hides(mi, *row.operator->()))
                continue;
            if (dn2cpp_meta_family_name(*row.operator->()))
                family.push_back(mi);
            if (name != nullptr && !dn2cpp_member_name_matches(row->name, name, flags, false))
                continue;
            if (out != nullptr)
                dn2cpp_gc_store_ref(&out[n],
                    reinterpret_cast<Dn2CppObject*>(row->genericParamCount > 0
                        ? dn2cpp_make_methodref_defview(mi, type) : dn2cpp_make_methodref(mi, type)));
            n++;
        }
        if (flags & DN2CPP_BF_DECLAREDONLY)
            break;
        gvm.next_type();
    }
    return n + dn2cpp_meta_collect_family(type, flags, family, out != nullptr ? out + n : nullptr, name);
}

Dn2CppArrayRef* dn2cpp_type_get_methods(Dn2CppType* t, int32_t bindingFlags)
{
    dn2cpp_type_require(t);
    int32_t n = dn2cpp_collect_methods(t->typeInfo, bindingFlags, nullptr);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    dn2cpp_collect_methods(t->typeInfo, bindingFlags, arr->data);
    return arr;
}

Dn2CppArrayRef* dn2cpp_type_get_declared_methods(Dn2CppType* t, Dn2CppString* name)
{
    dn2cpp_type_require(t);
    dn2cpp_require_metadata(t->typeInfo);
    if (name == nullptr)
        return dn2cpp_newarr_ref(0);
    constexpr int32_t flags = DN2CPP_BF_DECLAREDONLY | DN2CPP_BF_INSTANCE | DN2CPP_BF_STATIC
        | DN2CPP_BF_PUBLIC | DN2CPP_BF_NONPUBLIC;
    int32_t n = dn2cpp_collect_methods(t->typeInfo, flags, nullptr, name);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    dn2cpp_collect_methods(t->typeInfo, flags, arr->data, name);
    return arr;
}

static const Dn2CppTypeInfo* dn2cpp_method_parameter_type(
    Dn2CppMetadataHandle<Dn2CppMethodInfo> mi, int32_t index);

static int32_t dn2cpp_bare_method_parameter_index(const char* key, int32_t count)
{
    if (key == nullptr || std::strncmp(key, "GvarM", 5) != 0)
        return -1;
    int32_t index = 0;
    const char* p = key + 5;
    bool digit = false;
    while (*p >= '0' && *p <= '9' && index < count)
    {
        digit = true;
        index = index * 10 + *p++ - '0';
    }
    return digit && *p == '\0' && index < count ? index : -1;
}

const Dn2CppTypeInfo* dn2cpp_signature_type(const Dn2CppBindingSignature* signature,
    const Dn2CppTypeInfo* declaring)
{
    if (signature == nullptr)
        dn2cpp_throw_platform_not_supported("Reflection: unresolved signature type");
    const auto& sig = *signature;
    if (sig.kind == 0)
    {
        if (sig.type == nullptr || (sig.type->flags & (DN2CPP_TF_SHARED_CANON | DN2CPP_TF_GENERICDEF | DN2CPP_TF_GENERICPARAM)) != 0)
            dn2cpp_throw_platform_not_supported("Reflection: unresolved signature type");
        return sig.type;
    }
    if (sig.kind == 5)
    {
        if (declaring == nullptr || declaring->genericArgs == nullptr
            || sig.value < 0 || sig.value >= declaring->genericArgCount)
            dn2cpp_throw_platform_not_supported("Reflection: composed open generic signature types are not supported");
        Dn2CppBindingSignature actual{0, declaring->genericArgs[sig.value], 0, nullptr, 0, nullptr};
        return dn2cpp_signature_type(&actual);
    }
    if (sig.kind >= 1 && sig.kind <= 3)
    {
        std::vector<const Dn2CppTypeInfo*> children;
        for (int32_t i = 0; i < sig.childCount; i++)
            children.push_back(dn2cpp_signature_type(&sig.children[i], declaring));
        return dn2cpp_compound_type(sig.kind, sig.value, children.data(), sig.childCount);
    }
    if ((sig.kind == 6 || sig.kind == 7) && sig.childCount == 1)
        return dn2cpp_array_ti(dn2cpp_signature_type(&sig.children[0], declaring),
            sig.kind == 6 ? 1 : sig.value, sig.kind == 7);
    if (sig.kind == 4 && sig.type != nullptr)
    {
        auto* args = dn2cpp_newarr_ref(sig.childCount);
        for (int32_t i = 0; i < sig.childCount; i++)
            dn2cpp_gc_store_ref(&args->data[i], reinterpret_cast<Dn2CppObject*>(
                dn2cpp_get_type_from_handle(dn2cpp_signature_type(&sig.children[i], declaring))));
        return dn2cpp_type_make_generic(dn2cpp_get_type_from_handle(sig.type), args)->typeInfo;
    }
    dn2cpp_throw_platform_not_supported("Reflection: unresolved signature type");
}

static const Dn2CppTypeInfo* dn2cpp_reflection_parameter_type(const Dn2CppParamInfo& parameter,
    const Dn2CppTypeInfo* declaring)
{
    return parameter.reflectionSignature != nullptr
        ? dn2cpp_signature_type(parameter.reflectionSignature, declaring) : parameter.paramType;
}

static const Dn2CppTypeInfo* dn2cpp_method_definition_type(
    Dn2CppMetadataHandle<Dn2CppMethodInfo> mi, const char* key, const Dn2CppTypeInfo* closed,
    const Dn2CppTypeInfo* declaring, int32_t parameter = -1);

static const Dn2CppBindingSignature* dn2cpp_parameter_array_signature(
    const Dn2CppParamInfo& parameter, bool referent = false)
{
    const auto* signature = parameter.reflectionSignature;
    if (referent && signature != nullptr && signature->kind == 2 && signature->childCount == 1)
        signature = &signature->children[0];
    return signature != nullptr && (signature->kind == 6 || signature->kind == 7) ? signature : nullptr;
}

// Array validation uses its full element identity; the invoker's ABI columns
// still describe the unchanged reference/by-ref calling convention.
static const Dn2CppTypeInfo* dn2cpp_invoke_parameter_type(const Dn2CppParamInfo& parameter,
    const Dn2CppTypeInfo* declaring, bool referent = false)
{
    if (const auto* signature = dn2cpp_parameter_array_signature(parameter, referent))
        return dn2cpp_signature_type(signature, declaring);
    return referent ? parameter.passType : parameter.paramType;
}

// Exact parameter identities, including a formal parameter of this MethodDef.
// Composed open signatures are not modeled. A null Type element throws like .NET.
static bool dn2cpp_params_match_types(Dn2CppMetadataHandle<Dn2CppMethodInfo> mi, Dn2CppArrayRef* types,
    const Dn2CppTypeInfo* reflected)
{
    const Dn2CppMethodInfo row = *mi;
    if (row.paramCount != types->length)
        return false;
    for (int32_t j = 0; j < types->length; j++)
    {
        auto* pt = reinterpret_cast<Dn2CppType*>(types->data[j]);
        if (pt == nullptr)
            dn2cpp_throw_argument_null_param("types");
        const Dn2CppParamInfo param = *row.parameters[j];
        if ((row.declaringType->flags & DN2CPP_TF_GENERICDEF) != 0 && row.definitionSignature != nullptr)
        {
            if (dn2cpp_method_definition_type(mi, "symbolic", param.paramType,
                dn2cpp_invoke_declaring(row.declaringType, reflected), j) != pt->typeInfo)
                return false;
            continue;
        }
        if (param.genericDefinitionKey != nullptr)
        {
            const int32_t index = dn2cpp_bare_method_parameter_index(
                param.genericDefinitionKey, row.genericParamCount);
            if (index < 0 || dn2cpp_method_parameter_type(mi, index) != pt->typeInfo)
                return false;
        }
        else if (dn2cpp_reflection_parameter_type(param, row.declaringType) != pt->typeInfo)
            return false;
    }
    return true;
}

// Rows of one method definition: the per-instantiation rows of a generic method.
static bool dn2cpp_same_definition(const Dn2CppMethodInfo& a, const Dn2CppMethodInfo& b)
{
    return a.metadataToken != 0 && a.metadataToken == b.metadataToken && a.declaringType == b.declaringType;
}

// .NET's CompareMethodSig: equal parameter types of the definitions. Each generic
// method's type parameters are types of its own, so rows of two definitions differ
// wherever either definition's parameter names one, and otherwise carry the
// definitions' types.
static bool dn2cpp_candidates_sig_equal(Dn2CppMetadataHandle<Dn2CppMethodInfo> aHandle,
    Dn2CppMetadataHandle<Dn2CppMethodInfo> bHandle, const Dn2CppTypeInfo* reflected)
{
    const Dn2CppMethodInfo a = *aHandle;
    const Dn2CppMethodInfo b = *bHandle;
    if (dn2cpp_same_definition(a, b))
        return true;
    if (a.paramCount != b.paramCount)
        return false;
    if (dn2cpp_symbolic_method(a) || dn2cpp_symbolic_method(b))
        return dn2cpp_symbolic_parameters_equal(aHandle, bHandle, reflected, false);
    for (int32_t j = 0; j < a.paramCount; j++)
    {
        const Dn2CppParamInfo pa = *a.parameters[j];
        const Dn2CppParamInfo pb = *b.parameters[j];
        if (pa.genericDefinitionKey != nullptr || pb.genericDefinitionKey != nullptr
            || pa.paramType != pb.paramType)
            return false;
    }
    return true;
}

// Resolves a GetMethod candidate set the way real .NET's GetMethodImpl does. The
// candidates are collected derived-first, and the rows of ONE generic method
// definition stand for it together. The selected row supplies its definition
// view, while its closed instantiations remain available to MakeGenericMethod.
// One definition wins outright; sig-equal definitions
// (a `new`-hiding chain) resolve to the most derived one unless another is declared
// beside it, as FindMostDerivedNewSlotMeth refuses two at one hierarchy depth.
// Anything else is ambiguous and throws AmbiguousMatchException naming the first
// candidate, reflected through `reflected`.
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_resolve_method_candidates(Dn2CppMetadataHandle<Dn2CppMethodInfo> const* c, int32_t n,
    const Dn2CppTypeInfo* reflected)
{
    if (n == 1)
        return c[0];
    const Dn2CppMethodInfo first = *c[0];
    bool allEqual = true;
    for (int32_t i = 1; i < n && allEqual; i++)
        allEqual = dn2cpp_candidates_sig_equal(c[0], c[i], reflected);
    for (int32_t i = 1; i < n; i++)
    {
        const Dn2CppMethodInfo row = *c[i];
        if (!dn2cpp_same_definition(first, row) && (!allEqual || row.declaringType == first.declaringType))
            dn2cpp_throw_ambiguous_member(reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(c[0], reflected)));
    }
    return c[0];
}

// ---- reflection over an intrinsic type's metadata-answerable members ----
//
// An intrinsic type (CoreIntrinsicTypeMaps.s_intrinsicTypes) carries NO reflection method
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
// what an AOT image cannot do for an ordinary generic method. Unsafe.SizeOf<T>
// qualifies, reached through MakeGenericMethod with T known only at run time, and so
// does Object.MemberwiseClone, the shallow copy every reflective cloner is built on and
// the only route by which an array or a string is ever a receiver. The other members
// of System.Object and System.ValueType qualify too: each answers from the receiver's
// type-info hooks, which is what a callvirt of it runs, so neither the runtime-owned
// Object nor the intrinsic ValueType needs an emitted table.
//
// The rows are synthesized on demand and interned, so handle identity, Equals and
// GetHashCode behave like a real row's. They are reachable only through a NAMED
// lookup (Type.GetMethod): GetMethods() deliberately does not list them, because a
// partial member table for a type that really has dozens is a different wrong
// answer, and listing Object's members would change every type's enumeration.

#define DN2CPP_META_MAX_ARITY 4

// One metadata-answerable member. `answer` receives the closed row's type arguments
// (null for a non-generic member), the receiver MethodInfo.Invoke was handed (null
// for a static one) and the arguments, already checked against `params`, and returns
// the boxed result Invoke hands back.
//
// `attrs` and `ilAttrs` are per-row because the members differ in scope and
// visibility: they drive both the row the intern mints and the BindingFlags filter in
// dn2cpp_meta_lookup, so a row cannot be found under flags that disagree with the row
// it would hand back.
//
// A GATED member is inherited only through levels carrying
// DN2CPP_TF_OBJECT_MEMBER_ROWS: found through a level whose override has no row, it
// would be a wrong method where null is only a missing one.
struct Dn2CppMetaMember
{
    const char* typeName;
    const char* methodName;
    int32_t genericArity;
    const Dn2CppTypeInfo* retType;
    int32_t attrs;   // DN2CPP_MTHA_* visibility/scope (METAANSWER/GENERIC are added by dn2cpp_meta_row)
    int32_t ilAttrs; // the MethodAttributes word MethodBase.Attributes reads
    Dn2CppObject* (*answer)(const Dn2CppTypeInfo* const* args, Dn2CppObject* receiver,
                            Dn2CppObject** argv);
    const Dn2CppParamInfo* params;
    int32_t paramCount;
    const char* display; // MethodInfo.ToString; null for a generic member
    const char* returnDisplay;
    bool gated;
    int32_t ilImplAttrs; // the MethodImplAttributes word MethodImplementationFlags reads
    const Dn2CppMethodGenericParameter* genericParameters = nullptr;
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
static Dn2CppObject* dn2cpp_meta_unsafe_sizeof(const Dn2CppTypeInfo* const* args, Dn2CppObject* receiver,
                                               Dn2CppObject** argv)
{
    (void)receiver; // static
    (void)argv;
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
                                                         Dn2CppObject* receiver, Dn2CppObject** argv)
{
    (void)args; // non-generic
    (void)argv;
    return dn2cpp_object_memberwise_clone(receiver);
}

static Dn2CppObject* dn2cpp_meta_box_bool(bool value)
{
    int32_t v = value ? 1 : 0;
    return dn2cpp_box(&dn2cpp_bool_type, &v, sizeof(int32_t));
}

// Object's own members run what a call of them runs: a virtual one the receiver's
// type-info hook, which a boxed value and a runtime-owned object answer too, Equals for
// a null or identical argument as well. Only a binding closed over null hands them a
// null receiver, and then each does what Object's own body does with a null `this`.
static Dn2CppObject* dn2cpp_meta_object_tostring(const Dn2CppTypeInfo* const* args,
                                                 Dn2CppObject* receiver, Dn2CppObject** argv)
{
    (void)args;
    (void)argv;
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_object_tostring_virtual(receiver));
}

static Dn2CppObject* dn2cpp_meta_object_equals(const Dn2CppTypeInfo* const* args,
                                               Dn2CppObject* receiver, Dn2CppObject** argv)
{
    (void)args;
    return dn2cpp_meta_box_bool((receiver == nullptr ? dn2cpp_object_equals(receiver, argv[0])
                                                     : dn2cpp_object_equals_virtual(receiver, argv[0])) != 0);
}

static Dn2CppObject* dn2cpp_meta_object_gethashcode(const Dn2CppTypeInfo* const* args,
                                                    Dn2CppObject* receiver, Dn2CppObject** argv)
{
    (void)args;
    (void)argv;
    int32_t hash = dn2cpp_object_gethashcode(receiver);
    return dn2cpp_box(&dn2cpp_int32_type, &hash, sizeof(int32_t));
}

static Dn2CppObject* dn2cpp_meta_object_gettype(const Dn2CppTypeInfo* const* args,
                                                Dn2CppObject* receiver, Dn2CppObject** argv)
{
    (void)args;
    (void)argv;
    if (receiver == nullptr)
        dn2cpp_throw_null_reference();
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(receiver->type));
}

static Dn2CppObject* dn2cpp_meta_object_finalize(const Dn2CppTypeInfo* const* args,
                                                 Dn2CppObject* receiver, Dn2CppObject** argv)
{
    (void)args;
    (void)argv;
    if (receiver != nullptr && receiver->type->finalize != nullptr)
        receiver->type->finalize(receiver);
    return nullptr;
}

static Dn2CppObject* dn2cpp_meta_object_static_equals(const Dn2CppTypeInfo* const* args,
                                                      Dn2CppObject* receiver, Dn2CppObject** argv)
{
    (void)args;
    (void)receiver; // static
    return dn2cpp_meta_box_bool(dn2cpp_object_equals(argv[0], argv[1]) != 0);
}

static Dn2CppObject* dn2cpp_meta_object_reference_equals(const Dn2CppTypeInfo* const* args,
                                                         Dn2CppObject* receiver, Dn2CppObject** argv)
{
    (void)args;
    (void)receiver; // static
    return dn2cpp_meta_box_bool(argv[0] == argv[1]);
}

// A receiver dispatches its own Equals, as through Object's row. A null one (a binding
// closed over null) runs ValueType's bodies, which read its type, except that Equals
// answers false for a null argument first.
static Dn2CppObject* dn2cpp_meta_valuetype_equals(const Dn2CppTypeInfo* const* args,
                                                  Dn2CppObject* receiver, Dn2CppObject** argv)
{
    (void)args;
    if (receiver != nullptr)
        return dn2cpp_meta_box_bool(dn2cpp_object_equals_virtual(receiver, argv[0]) != 0);
    if (argv[0] == nullptr)
        return dn2cpp_meta_box_bool(false);
    dn2cpp_throw_null_reference();
}

static Dn2CppObject* dn2cpp_meta_valuetype_gethashcode(const Dn2CppTypeInfo* const* args,
                                                       Dn2CppObject* receiver, Dn2CppObject** argv)
{
    if (receiver == nullptr)
        dn2cpp_throw_null_reference();
    return dn2cpp_meta_object_gethashcode(args, receiver, argv);
}

static Dn2CppObject* dn2cpp_meta_method_generic_count(const Dn2CppTypeInfo* const*,
    Dn2CppObject* receiver, Dn2CppObject**)
{
    int32_t count = dn2cpp_methodref_generic_parameter_count(reinterpret_cast<Dn2CppMethodRef*>(receiver));
    return dn2cpp_box(&dn2cpp_int32_type, &count, sizeof(count));
}

static const Dn2CppParamInfo g_meta_params_obj[] = {
    { &dn2cpp_object_type, "obj", {}, 0, 0, nullptr, 0, nullptr, 0, 1, "System.Object obj", nullptr },
};

static const Dn2CppParamInfo g_meta_params_obj_pair[] = {
    { &dn2cpp_object_type, "objA", {}, 0, 0, nullptr, 0, nullptr, 0, 1, "System.Object objA", nullptr },
    { &dn2cpp_object_type, "objB", {}, 0, 0, nullptr, 0, nullptr, 0, 1, "System.Object objB", nullptr },
};

// SizeOf admits ref structs without imposing a value/reference constraint.
static const Dn2CppMethodGenericParameter g_meta_unsafe_sizeof_generic_parameters[] = {
    { "T", 0x20 },
};

// The MethodAttributes words are .NET's (MethodBase.Attributes, IsVirtual/IsFinal
// read them): 0x0096 Public|Static|HideBySig, 0x0085 FamORAssem|HideBySig, 0x01C6
// Public|Virtual|HideBySig|NewSlot, 0x01C4 its Family form, 0x0086 Public|HideBySig,
// and 0x00C6 Public|Virtual|HideBySig for an override. So is each MethodImplAttributes
// word: 0x0100 AggressiveInlining on SizeOf, 0 (IL) on the rest.
// The transpiler's CoreIntrinsics.IsObjectMemberRowName lists the gated rows' names.
static const Dn2CppMetaMember g_meta_members[] = {
    // Runtime-owned MethodInfo has no emitted member table or managed vtable.
    { "System.Reflection.MethodInfo", "get_GenericParameterCount", 0, &dn2cpp_int32_type,
      0 /* instance, non-public */, 0x0BC3, dn2cpp_meta_method_generic_count,
      nullptr, 0, "Int32 get_GenericParameterCount()", "Int32", false },
    { "System.Runtime.CompilerServices.Unsafe", "SizeOf", 1, &dn2cpp_int32_type,
      DN2CPP_MTHA_STATIC | DN2CPP_MTHA_PUBLIC, 0x0096, dn2cpp_meta_unsafe_sizeof,
      nullptr, 0, nullptr, "Int32", false, 0x0100, g_meta_unsafe_sizeof_generic_parameters },
    { "System.Object", "GetType", 0, &dn2cpp_public_type_type, DN2CPP_MTHA_PUBLIC, 0x0086,
      dn2cpp_meta_object_gettype, nullptr, 0, "System.Type GetType()", "System.Type", true },
    { "System.Object", "MemberwiseClone", 0, &dn2cpp_object_type,
      0 /* instance, non-public */, 0x0085, dn2cpp_meta_object_memberwise_clone,
      nullptr, 0, "System.Object MemberwiseClone()", "System.Object", false },
    { "System.Object", "Finalize", 0, &dn2cpp_void_type, 0 /* instance, non-public */, 0x01C4,
      dn2cpp_meta_object_finalize, nullptr, 0, "Void Finalize()", "Void", true },
    { "System.Object", "ToString", 0, &dn2cpp_string_type, DN2CPP_MTHA_PUBLIC, 0x01C6,
      dn2cpp_meta_object_tostring, nullptr, 0, "System.String ToString()", "System.String", true },
    { "System.Object", "Equals", 0, &dn2cpp_bool_type, DN2CPP_MTHA_PUBLIC, 0x01C6,
      dn2cpp_meta_object_equals, g_meta_params_obj, 1, "Boolean Equals(System.Object)", "Boolean", true },
    { "System.Object", "Equals", 0, &dn2cpp_bool_type, DN2CPP_MTHA_STATIC | DN2CPP_MTHA_PUBLIC, 0x0096,
      dn2cpp_meta_object_static_equals, g_meta_params_obj_pair, 2,
      "Boolean Equals(System.Object, System.Object)", "Boolean", true },
    { "System.Object", "ReferenceEquals", 0, &dn2cpp_bool_type, DN2CPP_MTHA_STATIC | DN2CPP_MTHA_PUBLIC, 0x0096,
      dn2cpp_meta_object_reference_equals, g_meta_params_obj_pair, 2,
      "Boolean ReferenceEquals(System.Object, System.Object)", "Boolean", true },
    { "System.Object", "GetHashCode", 0, &dn2cpp_int32_type, DN2CPP_MTHA_PUBLIC, 0x01C6,
      dn2cpp_meta_object_gethashcode, nullptr, 0, "Int32 GetHashCode()", "Int32", true },
    { "System.Exception", "ToString", 0, &dn2cpp_string_type, DN2CPP_MTHA_PUBLIC, 0x00C6,
      dn2cpp_meta_object_tostring, nullptr, 0, "System.String ToString()", "System.String", true },
    { "System.Exception", "GetType", 0, &dn2cpp_public_type_type, DN2CPP_MTHA_PUBLIC, 0x0086,
      dn2cpp_meta_object_gettype, nullptr, 0, "System.Type GetType()", "System.Type", true },
    { "System.ValueType", "Equals", 0, &dn2cpp_bool_type, DN2CPP_MTHA_PUBLIC, 0x00C6,
      dn2cpp_meta_valuetype_equals, g_meta_params_obj, 1, "Boolean Equals(System.Object)", "Boolean", true },
    { "System.ValueType", "GetHashCode", 0, &dn2cpp_int32_type, DN2CPP_MTHA_PUBLIC, 0x00C6,
      dn2cpp_meta_valuetype_gethashcode, nullptr, 0, "Int32 GetHashCode()", "Int32", true },
    { "System.ValueType", "ToString", 0, &dn2cpp_string_type, DN2CPP_MTHA_PUBLIC, 0x00C6,
      dn2cpp_meta_object_tostring, nullptr, 0, "System.String ToString()", "System.String", true },
};
static constexpr int32_t g_meta_member_count =
    static_cast<int32_t>(sizeof(g_meta_members) / sizeof(g_meta_members[0]));

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
static_assert(std::is_standard_layout_v<Dn2CppMetaRow>);

static Dn2CppMetaRow* g_meta_rows = nullptr;
static std::mutex& g_meta_rows_mtx = dn2cpp_never_destroyed<std::mutex>();

// The descriptor a synthesized row answers from, or null when `mi` is an ordinary
// emitted row. Only dn2cpp_meta_row sets METAANSWER, on the row inside a
// Dn2CppMetaRow the intern list owns for the process lifetime, so the record is
// found from the row's address without the list's lock.
static const Dn2CppMetaMember* dn2cpp_meta_desc_of(Dn2CppMetadataHandle<Dn2CppMethodInfo> mi)
{
    const Dn2CppMethodInfo* row = mi.native();
    if (row == nullptr || (row->attrs & DN2CPP_MTHA_METAANSWER) == 0)
        return nullptr;
    return reinterpret_cast<const Dn2CppMetaRow*>(
        reinterpret_cast<const char*>(row) - offsetof(Dn2CppMetaRow, row))->desc;
}

// The interned row for one (descriptor, declaring type, type arguments). A generic
// member with `args == nullptr` has an OPEN definition row; its definition view
// rejects late-bound Invoke before entering the invocation plan.
static int32_t dn2cpp_meta_emitted_token(const Dn2CppMetaMember* d, const Dn2CppTypeInfo* declaring);
static Dn2CppMetadataTable<Dn2CppMethodInfo> dn2cpp_delegate_private_methods(
    const Dn2CppTypeInfo* owner, int32_t& count);

static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_meta_row(const Dn2CppMetaMember* d,
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
    r->row.parameters = Dn2CppMetadataTable<Dn2CppParamInfo>(d->params);
    r->row.paramCount = d->paramCount;
    r->row.attrs = d->attrs | DN2CPP_MTHA_METAANSWER
        | (d->genericArity > 0 ? DN2CPP_MTHA_GENERIC : 0);
    r->row.vtableSlot = -1;
    r->row.metadataToken = dn2cpp_meta_emitted_token(d, declaring);
    r->row.ilAttrs = d->ilAttrs;
    r->row.ilImplAttrs = d->ilImplAttrs;
    r->row.genericParamCount = d->genericArity;
    r->row.genericParameters = d->genericParameters;
    r->row.returnCustomModifiersKnown = 1;
    r->row.display = d->display;
    r->row.returnDisplay = d->returnDisplay;
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

static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_canonical_method_definition(
    Dn2CppMetadataHandle<Dn2CppMethodInfo> mi)
{
    mi = dn2cpp_open_method_definition(mi);
    const Dn2CppMethodInfo row = *mi;
    if ((row.attrs & DN2CPP_MTHA_METAANSWER) != 0)
        if (const auto* descriptor = dn2cpp_meta_desc_of(mi))
            return dn2cpp_meta_row(descriptor, row.declaringType, nullptr, row.genericParamCount);
    if (row.metadataToken != 0)
    {
        const auto reflection = row.declaringType->reflection();
        for (int32_t i = 0; i < reflection.methodCount; i++)
        {
            const auto candidate = reflection.methods[i];
            if (candidate->metadataToken == row.metadataToken && candidate->genericParamCount > 0)
                return candidate;
        }
        int32_t count;
        const auto methods = dn2cpp_delegate_private_methods(row.declaringType, count);
        for (int32_t i = 0; i < count; ++i)
            if (methods[i]->metadataToken == row.metadataToken && methods[i]->genericParamCount > 0)
                return methods[i];
    }
    return mi;
}

struct Dn2CppMethodParameterKey
{
    const Dn2CppTypeInfo* owner;
    int32_t token;
    const void* row;
    bool operator==(const Dn2CppMethodParameterKey& other) const
    {
        return owner == other.owner && token == other.token && row == other.row;
    }
};

struct Dn2CppMethodParameterKeyHash
{
    size_t operator()(const Dn2CppMethodParameterKey& key) const
    {
        return (reinterpret_cast<uintptr_t>(key.owner) >> 4)
            ^ (reinterpret_cast<uintptr_t>(key.row) >> 4) ^ static_cast<uint32_t>(key.token);
    }
};

struct Dn2CppFormalType
{
    Dn2CppTypeInfo type{};
    int32_t attributes = 0;
    const Dn2CppTypeInfo* declaringType = nullptr;
    Dn2CppTypeReflection reflection{};
    const Dn2CppMethodGenericParameter* descriptor = nullptr;
    Dn2CppMetadataHandle<Dn2CppMethodInfo> declaringMethod;
    std::vector<const Dn2CppTypeInfo*> typeArguments;
    std::vector<const Dn2CppTypeInfo*> methodArguments;
    std::vector<const Dn2CppTypeInfo*> constraints;
    std::vector<Dn2CppInterfaceEntry> interfaces;
    int state = 0;
    bool parameter = false;
    bool constraintsAvailable = true;
};

// The recursive cold lock permits F-bound recipes to reuse published skeletons.
// Identity and immutable parameter contexts publish before relation decoding.
// Relation queries complete under this lock and guard the original declaring type.
static std::recursive_mutex& g_formal_types_mtx = dn2cpp_never_destroyed<std::recursive_mutex>();
static auto& g_formal_types = dn2cpp_never_destroyed<std::unordered_map<const Dn2CppTypeInfo*, Dn2CppFormalType*>>();
static auto& g_method_parameters = dn2cpp_never_destroyed<
    std::unordered_map<Dn2CppMethodParameterKey, std::vector<Dn2CppFormalType*>, Dn2CppMethodParameterKeyHash>>();
static auto& g_type_parameters = dn2cpp_never_destroyed<
    std::unordered_map<const Dn2CppTypeInfo*, std::vector<Dn2CppFormalType*>>>();
static auto& g_constraint_instances = dn2cpp_never_destroyed<std::vector<Dn2CppFormalType*>>();

static const Dn2CppTypeInfo* dn2cpp_meta_valuetype();
static void dn2cpp_complete_formal_type(Dn2CppFormalType* node);
static std::vector<const Dn2CppTypeInfo*> dn2cpp_formal_type_arguments(const Dn2CppTypeInfo* owner);

static const Dn2CppTypeInfo* dn2cpp_constraint_type(const Dn2CppBindingSignature& recipe,
    const std::vector<const Dn2CppTypeInfo*>& types, const std::vector<const Dn2CppTypeInfo*>& methods);

static const Dn2CppTypeInfo* dn2cpp_constraint_instantiation(
    const Dn2CppTypeInfo* definition, const std::vector<const Dn2CppTypeInfo*>& arguments)
{
    if (const auto* aot = dn2cpp_find_aot_instantiation(definition, arguments.data(), static_cast<int32_t>(arguments.size())))
        return aot;
    if (const auto* synthesized = dn2cpp_try_synthesize_generic(definition, arguments.data(), static_cast<int32_t>(arguments.size())))
        return synthesized;
    for (auto* node : g_constraint_instances)
        if (node->type.genericDef == definition && node->typeArguments == arguments)
            return &node->type;
    auto* node = new Dn2CppFormalType();
    node->type.name = definition->name;
    node->type.flags = definition->flags & ~(DN2CPP_TF_GENERICDEF | DN2CPP_TF_RUNTIME_TEMPLATE);
    node->type.genericDef = definition;
    node->typeArguments = arguments;
    node->type.genericArgs = node->typeArguments.data();
    node->type.genericArgCount = static_cast<int32_t>(arguments.size());
    node->reflection = definition->reflection();
    node->type.reflectionData = &node->reflection;
    g_formal_types[&node->type] = node;
    g_constraint_instances.push_back(node);
    return &node->type;
}

static const Dn2CppTypeInfo* dn2cpp_open_generic_type(const Dn2CppTypeInfo* definition,
    const std::vector<const Dn2CppTypeInfo*>& arguments)
{
    std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
    const auto* metadata = definition->reflection().genericMetadata;
    if (metadata == nullptr)
        dn2cpp_throw_platform_not_supported("Reflection: generic definition metadata is not retained");
    if (static_cast<int32_t>(arguments.size()) != metadata->parameterCount)
        dn2cpp_throw_argument_param(DN2CPP_SR_GENERIC_ARGS_COUNT, "typeArguments");
    dn2cpp_validate_open_arguments(metadata->parameters, arguments, arguments, {});
    return dn2cpp_constraint_instantiation(definition, arguments);
}

static const Dn2CppTypeInfo* dn2cpp_constraint_type(const Dn2CppBindingSignature& recipe,
    const std::vector<const Dn2CppTypeInfo*>& types, const std::vector<const Dn2CppTypeInfo*>& methods)
{
    if (recipe.kind == 5 || recipe.kind == 9)
    {
        const auto& arguments = recipe.kind == 5 ? types : methods;
        if (recipe.value < 0 || static_cast<size_t>(recipe.value) >= arguments.size())
            dn2cpp_throw_invalid_operation();
        return arguments[recipe.value];
    }
    if (recipe.kind == 0 || recipe.kind == 8)
    {
        const auto* type = recipe.type != nullptr ? recipe.type : recipe.name != nullptr
            ? dn2cpp_type_registry_find(recipe.name, static_cast<int32_t>(std::strlen(recipe.name))) : nullptr;
        if (type == nullptr)
            dn2cpp_throw_platform_not_supported("Reflection: generic constraint type is not retained");
        return type;
    }
    std::vector<const Dn2CppTypeInfo*> children;
    for (int32_t i = 0; i < recipe.childCount; i++)
        children.push_back(dn2cpp_constraint_type(recipe.children[i], types, methods));
    if (recipe.kind == 4)
    {
        const auto* definition = recipe.type != nullptr ? recipe.type : recipe.name != nullptr
            ? dn2cpp_type_registry_find(recipe.name, static_cast<int32_t>(std::strlen(recipe.name))) : nullptr;
        if (definition == nullptr)
            dn2cpp_throw_platform_not_supported("Reflection: generic constraint type is not retained");
        if ((definition->flags & DN2CPP_TF_GENERICDEF) == 0)
            dn2cpp_throw_invalid_operation();
        return dn2cpp_constraint_instantiation(definition, children);
    }
    if ((recipe.kind == 6 || recipe.kind == 7) && children.size() == 1)
        return dn2cpp_array_ti(children[0], recipe.kind == 6 ? 1 : recipe.value, recipe.kind == 7);
    if (recipe.kind >= 1 && recipe.kind <= 3)
    {
        if (recipe.kind == 3)
            for (const auto* child : children)
                if (dn2cpp_type_contains_generic_parameters(child))
                    dn2cpp_throw_platform_not_supported("Reflection: open function-pointer signatures are not supported");
        return dn2cpp_compound_type(recipe.kind, recipe.value, children.data(), static_cast<int32_t>(children.size()));
    }
    dn2cpp_throw_invalid_operation();
}

static void dn2cpp_constraint_interfaces(const Dn2CppTypeInfo* type,
    std::vector<const Dn2CppTypeInfo*>& visited, std::vector<Dn2CppInterfaceEntry>& result, bool includeSelf)
{
    if (type == nullptr || std::find(visited.begin(), visited.end(), type) != visited.end())
        return;
    visited.push_back(type);
    if (includeSelf && (type->flags & DN2CPP_TF_INTERFACE) != 0)
        result.push_back({type, nullptr});
    auto found = g_formal_types.find(type);
    if (found != g_formal_types.end())
    {
        auto* node = found->second;
        dn2cpp_complete_formal_type(node);
        if (node->parameter)
        {
            for (const auto* constraint : node->constraints)
                dn2cpp_constraint_interfaces(constraint, visited, result, true);
            return;
        }
    }
    const auto* metadata = type->reflection().genericMetadata;
    if (metadata != nullptr)
    {
        std::vector<const Dn2CppTypeInfo*> arguments;
        if ((type->flags & DN2CPP_TF_GENERICDEF) != 0)
            arguments = dn2cpp_formal_type_arguments(type);
        else if (type->genericArgCount > 0)
            arguments.assign(type->genericArgs, type->genericArgs + type->genericArgCount);
        if (metadata->base != nullptr)
            dn2cpp_constraint_interfaces(dn2cpp_constraint_type(*metadata->base, arguments, {}), visited, result, false);
        for (int32_t i = 0; i < metadata->interfaceCount; i++)
            dn2cpp_constraint_interfaces(dn2cpp_constraint_type(metadata->interfaces[i], arguments, {}), visited, result, true);
    }
    else
    {
        Dn2CppEffectiveItfs(type).ForEach([&](const Dn2CppTypeInfo* itf) {
            dn2cpp_constraint_interfaces(itf, visited, result, true);
        });
        dn2cpp_constraint_interfaces(type->base, visited, result, false);
    }
}

static void dn2cpp_complete_formal_type(Dn2CppFormalType* node)
{
    if (node->parameter && !node->constraintsAvailable)
        dn2cpp_require_metadata(node->declaringType);
    if (node->state == 3)
        dn2cpp_throw_platform_not_supported("Reflection: generic constraint metadata could not be resolved");
    if (node->state != 0)
        return;
    node->state = 1;
    try
    {
        if (node->parameter)
        {
            const auto* descriptor = node->descriptor;
            if (descriptor != nullptr)
                for (int32_t i = 0; i < descriptor->constraintCount; i++)
                    node->constraints.push_back(dn2cpp_constraint_type(descriptor->constraints[i], node->typeArguments, node->methodArguments));
            for (const auto* constraint : node->constraints)
                if ((constraint->flags & (DN2CPP_TF_INTERFACE | DN2CPP_TF_GENERICPARAM)) == 0)
                    node->type.base = constraint;
            std::vector<const Dn2CppTypeInfo*> visited;
            for (const auto* constraint : node->constraints)
                dn2cpp_constraint_interfaces(constraint, visited, node->interfaces, true);
        }
        else if (const auto* metadata = node->reflection.genericMetadata)
        {
            if (metadata->base != nullptr)
                node->type.base = dn2cpp_constraint_type(*metadata->base, node->typeArguments, {});
            std::vector<const Dn2CppTypeInfo*> visited;
            if (node->type.base != nullptr)
                dn2cpp_constraint_interfaces(node->type.base, visited, node->interfaces, false);
            for (int32_t i = 0; i < metadata->interfaceCount; i++)
                dn2cpp_constraint_interfaces(dn2cpp_constraint_type(metadata->interfaces[i], node->typeArguments, {}), visited, node->interfaces, true);
        }
        node->type.interfaces = node->interfaces.data();
        node->type.interfaceCount = static_cast<int32_t>(node->interfaces.size());
        node->state = 2;
    }
    catch (...)
    {
        node->state = 3;
        throw;
    }

}

static Dn2CppFormalType* dn2cpp_formal_skeleton(const Dn2CppTypeInfo* owner,
    const Dn2CppMethodGenericParameter* descriptor)
{
    auto* node = new Dn2CppFormalType();
    node->parameter = true;
    node->descriptor = descriptor;
    node->attributes = descriptor != nullptr ? descriptor->attrs : 0;
    node->declaringType = owner;
    node->type.name = descriptor != nullptr ? descriptor->name : "T";
    const bool valueType = (node->attributes & 0x8) != 0;
    node->type.base = valueType ? dn2cpp_meta_valuetype() : &dn2cpp_object_type;
    node->type.flags = DN2CPP_TF_GENERICPARAM | DN2CPP_TF_NESTED | (valueType ? DN2CPP_TF_VALUETYPE : 0);
    node->reflection.assemblyName = dn2cpp_ti_assembly_name(owner);
    node->type.reflectionData = &node->reflection;
    g_formal_types[&node->type] = node;
    return node;
}

static std::vector<const Dn2CppTypeInfo*> dn2cpp_formal_type_arguments(const Dn2CppTypeInfo* owner)
{
    std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
    auto& parameters = g_type_parameters[owner];
    const auto* descriptor = owner->reflection().genericMetadata;
    if (parameters.empty() && descriptor != nullptr && descriptor->parameterCount > 0)
    {
        for (int32_t i = 0; i < descriptor->parameterCount; i++)
        {
            auto* parameter = dn2cpp_formal_skeleton(owner, &descriptor->parameters[i]);
            parameter->constraintsAvailable = descriptor->parameterMetadataAvailable;
            parameters.push_back(parameter);
        }
        std::vector<const Dn2CppTypeInfo*> arguments;
        for (auto* node : parameters)
            arguments.push_back(&node->type);
        for (auto* node : parameters)
            node->typeArguments = arguments;
    }
    std::vector<const Dn2CppTypeInfo*> result;
    for (auto* node : parameters)
        result.push_back(&node->type);
    return result;
}

// Formal parameters belong to the typical MethodDef, never a reflected close.
static const Dn2CppTypeInfo* dn2cpp_method_parameter_type(
    Dn2CppMetadataHandle<Dn2CppMethodInfo> mi, int32_t index)
{
    const Dn2CppMethodInfo row = *mi;
    if (index < 0 || index >= row.genericParamCount)
        dn2cpp_throw_invalid_operation();
    const auto* owner = row.declaringType->genericDef != nullptr ? row.declaringType->genericDef : row.declaringType;
    const Dn2CppMethodParameterKey key{ owner, row.metadataToken,
        row.metadataToken == 0 ? dn2cpp_canonical_method_definition(mi).identity() : nullptr };
    std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
    auto& parameters = g_method_parameters[key];
    if (parameters.empty())
    {
        for (int32_t i = 0; i < row.genericParamCount; i++)
            parameters.push_back(dn2cpp_formal_skeleton(owner, row.genericParameters != nullptr ? &row.genericParameters[i] : nullptr));
        auto types = dn2cpp_formal_type_arguments(owner);
        std::vector<const Dn2CppTypeInfo*> methods;
        for (auto* node : parameters)
            methods.push_back(&node->type);
        for (auto* node : parameters)
        {
            node->typeArguments = types;
            node->methodArguments = methods;
            node->declaringMethod = mi;
        }
    }
    return &parameters[index]->type;
}

bool dn2cpp_type_metadata_only(const Dn2CppTypeInfo* ti)
{
    if (ti == nullptr)
        return false;
    std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
    if (g_formal_types.find(ti) != g_formal_types.end())
        return true;
    if (ti->elementType != nullptr && dn2cpp_type_metadata_only(ti->elementType))
        return true;
    for (int32_t i = 0; i < ti->genericArgCount; i++)
        if (dn2cpp_type_metadata_only(ti->genericArgs[i]))
            return true;
    return false;
}

void dn2cpp_type_ensure_metadata_relations(const Dn2CppTypeInfo* ti)
{
    std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
    auto found = g_formal_types.find(ti);
    if (found != g_formal_types.end())
        dn2cpp_complete_formal_type(found->second);
}

const Dn2CppTypeInfo* dn2cpp_type_reflection_base(const Dn2CppTypeInfo* ti)
{
    dn2cpp_type_ensure_metadata_relations(ti);
    if ((ti->flags & DN2CPP_TF_GENERICDEF) != 0)
    {
        std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
        const auto* metadata = ti->reflection().genericMetadata;
        if (metadata != nullptr && metadata->base != nullptr)
            return dn2cpp_constraint_type(*metadata->base, dn2cpp_formal_type_arguments(ti), {});
    }
    return ti->base;
}

static const Dn2CppTypeInfo* dn2cpp_symbolic_member_type(const Dn2CppBindingSignature* signature,
    const Dn2CppTypeInfo* declaring)
{
    std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
    auto arguments = (declaring->flags & DN2CPP_TF_GENERICDEF) != 0
        ? dn2cpp_formal_type_arguments(declaring) : std::vector<const Dn2CppTypeInfo*>();
    if (declaring->genericArgs != nullptr)
        arguments.assign(declaring->genericArgs, declaring->genericArgs + declaring->genericArgCount);
    return dn2cpp_constraint_type(*signature, arguments, {});
}

static void dn2cpp_complete_metadata_assignability(const Dn2CppTypeInfo* ti,
    std::vector<const Dn2CppTypeInfo*>& visited)
{
    if (ti == nullptr || std::find(visited.begin(), visited.end(), ti) != visited.end())
        return;
    visited.push_back(ti);
    auto found = g_formal_types.find(ti);
    if (found != g_formal_types.end())
        dn2cpp_complete_formal_type(found->second);
    dn2cpp_complete_metadata_assignability(ti->elementType, visited);
    for (int32_t i = 0; i < ti->genericArgCount; i++)
        dn2cpp_complete_metadata_assignability(ti->genericArgs[i], visited);
    dn2cpp_complete_metadata_assignability(ti->base, visited);
    Dn2CppEffectiveItfs(ti).ForEach([&](const Dn2CppTypeInfo* itf) {
        dn2cpp_complete_metadata_assignability(itf, visited);
    });
}

void dn2cpp_type_ensure_metadata_assignability(const Dn2CppTypeInfo* ti)
{
    std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
    if (!dn2cpp_type_metadata_only(ti))
        return;
    // Array covariance and variance can read nested formal relations before caching.
    std::vector<const Dn2CppTypeInfo*> visited;
    dn2cpp_complete_metadata_assignability(ti, visited);
}

Dn2CppArrayRef* dn2cpp_type_get_generic_parameter_constraints(Dn2CppType* t)
{
    const auto* ti = dn2cpp_type_require(t);
    if ((ti->flags & DN2CPP_TF_GENERICPARAM) == 0)
        dn2cpp_throw_platform_not_supported("Type.GetGenericParameterConstraints: receiver is not a generic parameter");
    std::vector<const Dn2CppTypeInfo*> constraints;
    {
        std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
        auto* node = g_formal_types.at(ti);
        dn2cpp_complete_formal_type(node);
        constraints = node->constraints;
    }
    auto* result = dn2cpp_newarr_ref(static_cast<int32_t>(constraints.size()));
    for (size_t i = 0; i < constraints.size(); i++)
        dn2cpp_gc_store_ref(&result->data[i], reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(constraints[i])));
    return result;
}

int32_t dn2cpp_type_generic_parameter_position(const Dn2CppTypeInfo* ti)
{
    std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
    const auto found = g_formal_types.find(ti);
    if (found == g_formal_types.end() || found->second == nullptr || !found->second->parameter
        || &found->second->type != ti)
        dn2cpp_throw_platform_not_supported("Reflection: generic parameter context is not retained");
    const auto* node = found->second;
    // A formal's defining vector is independent of any constructed use of it.
    const auto& arguments = node->methodArguments.empty() ? node->typeArguments : node->methodArguments;
    for (size_t i = 0; i < arguments.size(); i++)
        if (arguments[i] == ti)
            return static_cast<int32_t>(i);
    dn2cpp_throw_platform_not_supported("Reflection: generic parameter context is not retained");
}

int32_t dn2cpp_type_generic_parameter_attributes(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_GENERICPARAM) != 0
        ? reinterpret_cast<const Dn2CppFormalType*>(ti)->attributes : 0;
}

Dn2CppString* dn2cpp_type_generic_parameter_namespace(const Dn2CppTypeInfo* ti)
{
    return dn2cpp_type_namespace(dn2cpp_type_generic_parameter_declaring_type(ti));
}

const Dn2CppTypeInfo* dn2cpp_type_generic_parameter_declaring_type(const Dn2CppTypeInfo* ti)
{
    return reinterpret_cast<const Dn2CppFormalType*>(ti)->declaringType;
}

Dn2CppMethodRef* dn2cpp_type_generic_parameter_declaring_method(const Dn2CppTypeInfo* ti)
{
    if ((ti->flags & DN2CPP_TF_GENERICPARAM) == 0)
        dn2cpp_throw_invalid_operation();
    Dn2CppMetadataHandle<Dn2CppMethodInfo> method;
    const Dn2CppTypeInfo* owner;
    {
        std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
        const auto* node = g_formal_types.at(ti);
        method = node->declaringMethod;
        owner = node->declaringType;
    }
    if (!method)
        return nullptr;
    // A formal first queried through a closed owner still belongs to its typical MethodDef.
    if (method->declaringType != owner)
    {
        const auto reflection = owner->reflection();
        const int32_t token = method->metadataToken;
        method = {};
        for (int32_t i = 0; token != 0 && i < reflection.methodCount; i++)
            if (reflection.methods[i]->metadataToken == token && reflection.methods[i]->genericParamCount > 0)
            {
                method = reflection.methods[i];
                break;
            }
        if (!method)
            dn2cpp_throw_platform_not_supported("Reflection: typical method definition metadata is not retained");
    }
    return dn2cpp_make_methodref_defview(method, owner);
}

static const Dn2CppTypeInfo* dn2cpp_method_definition_type(
    Dn2CppMetadataHandle<Dn2CppMethodInfo> mi, const char* key, const Dn2CppTypeInfo* closed,
    const Dn2CppTypeInfo* declaring, int32_t parameter)
{
    if (key == nullptr)
        return closed;
    const Dn2CppMethodInfo row = *mi;
    if (row.definitionSignature != nullptr)
    {
        std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
        std::vector<const Dn2CppTypeInfo*> types;
        if (declaring->genericArgs != nullptr)
            types.assign(declaring->genericArgs, declaring->genericArgs + declaring->genericArgCount);
        else if ((declaring->flags & DN2CPP_TF_GENERICDEF) != 0)
            types = dn2cpp_formal_type_arguments(declaring);
        std::vector<const Dn2CppTypeInfo*> methods;
        for (int32_t i = 0; i < row.genericParamCount; i++)
            methods.push_back(dn2cpp_symbolic_method(row) && row.genericArgs != nullptr
                ? row.genericArgs[i] : dn2cpp_method_parameter_type(mi, i));
        const auto* signature = parameter < 0 ? row.definitionSignature->returnType
            : parameter < row.definitionSignature->parameterCount ? &row.definitionSignature->parameters[parameter] : nullptr;
        if (signature == nullptr)
            dn2cpp_throw_invalid_operation();
        return dn2cpp_constraint_type(*signature, types, methods);
    }
    const int32_t index = dn2cpp_bare_method_parameter_index(key, mi->genericParamCount);
    if (index >= 0)
        return dn2cpp_method_parameter_type(mi, index);
    dn2cpp_throw_platform_not_supported("Reflection: composed open generic signature types are not supported");
}

// Override signatures compare method parameters by position; named lookup keeps
// each MethodDef's formal identity. Class parameters always use the actual level.
static bool dn2cpp_symbolic_parameters_equal(Dn2CppMetadataHandle<Dn2CppMethodInfo> a,
    Dn2CppMetadataHandle<Dn2CppMethodInfo> b, const Dn2CppTypeInfo* reflected, bool positional)
{
    if (a->paramCount != b->paramCount || positional && a->genericParamCount != b->genericParamCount)
        return false;
    std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
    std::vector<const Dn2CppTypeInfo*> methods;
    for (int32_t i = 0; i < a->genericParamCount; i++)
        methods.push_back(dn2cpp_method_parameter_type(a, i));
    auto parameter = [&](Dn2CppMetadataHandle<Dn2CppMethodInfo> method, int32_t index) {
        const auto row = *method;
        const auto* declaring = dn2cpp_invoke_declaring(row.declaringType, reflected);
        if (row.definitionSignature == nullptr)
            return dn2cpp_reflection_parameter_type(*row.parameters[index], declaring);
        std::vector<const Dn2CppTypeInfo*> types;
        if ((declaring->flags & DN2CPP_TF_GENERICDEF) != 0)
            types = dn2cpp_formal_type_arguments(declaring);
        else if (declaring->genericArgs != nullptr)
            types.assign(declaring->genericArgs, declaring->genericArgs + declaring->genericArgCount);
        std::vector<const Dn2CppTypeInfo*> ownMethods = methods;
        if (!positional && method != a)
        {
            ownMethods.clear();
            for (int32_t i = 0; i < row.genericParamCount; i++)
                ownMethods.push_back(dn2cpp_method_parameter_type(method, i));
        }
        return dn2cpp_constraint_type(row.definitionSignature->parameters[index], types, ownMethods);
    };
    for (int32_t i = 0; i < a->paramCount; i++)
        if (parameter(a, i) != parameter(b, i))
            return false;
    return true;
}

bool dn2cpp_type_reference_argument(const Dn2CppTypeInfo* argument)
{
    if ((argument->flags & DN2CPP_TF_GENERICPARAM) == 0)
        return (argument->flags & DN2CPP_TF_VALUETYPE) == 0;
    std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
    auto* node = g_formal_types.at(argument);
    if ((node->attributes & 4) != 0)
        return true;
    dn2cpp_complete_formal_type(node);
    for (const auto* constraint : node->constraints)
    {
        // A dependent formal does not establish the CLR's special class constraint.
        if ((constraint->flags & (DN2CPP_TF_GENERICPARAM | DN2CPP_TF_INTERFACE | DN2CPP_TF_VALUETYPE)) == 0
            && constraint != &dn2cpp_object_type && constraint != dn2cpp_meta_valuetype()
            && (constraint->name == nullptr || std::strcmp(constraint->name, "System.Enum") != 0))
            return true;
    }
    return false;
}

static bool dn2cpp_argument_constraint(const Dn2CppTypeInfo* argument, const Dn2CppTypeInfo* constraint,
    std::vector<const Dn2CppTypeInfo*>& visited)
{
    if (argument == constraint)
        return true;
    if (std::find(visited.begin(), visited.end(), argument) != visited.end())
        return false;
    visited.push_back(argument);
    dn2cpp_type_ensure_metadata_assignability(argument);
    dn2cpp_type_ensure_metadata_assignability(constraint);
    if (dn2cpp_typeinfo_assignable(argument, constraint) != 0)
        return true;
    if ((argument->flags & DN2CPP_TF_GENERICPARAM) != 0)
        for (const auto* inherited : g_formal_types.at(argument)->constraints)
            if (dn2cpp_argument_constraint(inherited, constraint, visited))
                return true;
    return false;
}

static void dn2cpp_validate_open_arguments(const Dn2CppMethodGenericParameter* parameters,
    const std::vector<const Dn2CppTypeInfo*>& arguments, const std::vector<const Dn2CppTypeInfo*>& types,
    const std::vector<const Dn2CppTypeInfo*>& methods)
{
    if (parameters == nullptr)
        return;
    for (size_t i = 0; i < arguments.size(); i++)
    {
        const auto* argument = arguments[i];
        const auto& parameter = parameters[i];
        if (argument == &dn2cpp_void_type || dn2cpp_signature_kind(argument) != 0
            || ((argument->flags & DN2CPP_TF_BYREFLIKE) != 0 && (parameter.attrs & 32) == 0))
            dn2cpp_throw_argument();
        if ((parameter.attrs & 8) != 0 && ((argument->flags & DN2CPP_TF_VALUETYPE) == 0
            || dn2cpp_nullable_underlying_ti(argument) != nullptr))
            dn2cpp_throw_argument();
        std::vector<const Dn2CppTypeInfo*> visited;
        if ((parameter.attrs & 4) != 0 && !dn2cpp_type_reference_argument(argument))
            dn2cpp_throw_argument();
        if ((parameter.attrs & 16) != 0)
        {
            bool constructor = (argument->flags & DN2CPP_TF_VALUETYPE) != 0;
            if ((argument->flags & DN2CPP_TF_GENERICPARAM) != 0)
                constructor = (g_formal_types.at(argument)->attributes & (8 | 16)) != 0;
            else if ((argument->flags & DN2CPP_TF_ABSTRACT) == 0)
            {
                const auto* definition = argument->genericDef != nullptr ? argument->genericDef : argument;
                const auto* metadata = definition->reflection().genericMetadata;
                constructor |= metadata != nullptr && metadata->publicDefaultConstructor;
                const auto reflection = argument->reflection();
                for (int32_t j = 0; j < reflection.ctorCount; j++)
                    constructor |= reflection.ctors[j]->paramCount == 0
                        && (reflection.ctors[j]->attrs & DN2CPP_MTHA_PUBLIC) != 0;
            }
            if (!constructor)
                dn2cpp_throw_argument();
        }
        for (int32_t j = 0; j < parameter.constraintCount; j++)
        {
            const auto* constraint = dn2cpp_constraint_type(parameter.constraints[j], types, methods);
            visited.clear();
            if (!dn2cpp_argument_constraint(argument, constraint, visited))
                dn2cpp_throw_argument();
        }
    }
}

static std::string dn2cpp_reflection_type_spelling(const Dn2CppTypeInfo* type, bool genericArgument = false)
{
    Dn2CppString* text = dn2cpp_type_tostring(type);
    int32_t size = dn2cpp_string_to_utf8(text, nullptr, 0);
    std::string result(static_cast<size_t>(size), '\0');
    dn2cpp_string_to_utf8(text, result.data(), size);
    const auto* root = type;
    while (root->elementType != nullptr)
        root = root->elementType;
    if ((((root->flags & DN2CPP_TF_PRIMITIVE) != 0 && root != &dn2cpp_string_type && root != &dn2cpp_object_type)
        || root == &dn2cpp_void_type || genericArgument && (root == &dn2cpp_string_type || root == &dn2cpp_object_type))
        && result.compare(0, 7, "System.") == 0)
        result.erase(0, 7);
    return result;
}

static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_open_method(
    Dn2CppMetadataHandle<Dn2CppMethodInfo> definition, const Dn2CppTypeInfo* declaring,
    const std::vector<const Dn2CppTypeInfo*>& arguments)
{
    std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
    std::lock_guard<std::recursive_mutex> methodLock(g_open_methods_mtx);
    for (auto* node : g_open_methods)
        if (node->definition == definition && node->row.declaringType == declaring && node->arguments == arguments)
            return &node->row;
    const Dn2CppMethodInfo original = *definition;
    std::vector<const Dn2CppTypeInfo*> types;
    if (declaring->genericArgs != nullptr)
        types.assign(declaring->genericArgs, declaring->genericArgs + declaring->genericArgCount);
    else if ((declaring->flags & DN2CPP_TF_GENERICDEF) != 0)
        types = dn2cpp_formal_type_arguments(declaring);
    dn2cpp_validate_open_arguments(original.genericParameters, arguments, types, arguments);
    auto resolve = [&](int32_t parameter) {
        if (original.definitionSignature != nullptr)
        {
            const auto* signature = parameter < 0 ? original.definitionSignature->returnType
                : &original.definitionSignature->parameters[parameter];
            return dn2cpp_constraint_type(*signature, types, arguments);
        }
        const char* key = parameter < 0 ? original.genericDefinitionReturnKey
            : original.parameters[parameter]->genericDefinitionKey;
        const auto* closed = parameter < 0 ? original.returnType : original.parameters[parameter]->paramType;
        int32_t index = dn2cpp_bare_method_parameter_index(key, original.genericParamCount);
        if (index >= 0)
            return arguments[index];
        if (key != nullptr)
            dn2cpp_throw_platform_not_supported("Reflection: generic method signature metadata is not retained");
        return closed;
    };
    auto* node = new Dn2CppOpenMethod();
    node->definition = definition;
    node->arguments = arguments;
    node->row = original;
    node->row.declaringType = declaring;
    node->row.genericArgs = node->arguments.data();
    node->row.attrs &= ~DN2CPP_MTHA_METAANSWER;
    node->row.fnPtr = nullptr;
    node->row.invoker = nullptr;
    node->row.returnType = resolve(-1);
    node->row.returnReflectionSignature = nullptr;
    auto retain = [](const std::string& text) {
        auto* result = new char[text.size() + 1];
        std::memcpy(result, text.c_str(), text.size() + 1);
        return result;
    };
    std::string display = dn2cpp_reflection_type_spelling(node->row.returnType) + " " + original.name + "[";
    for (size_t i = 0; i < arguments.size(); i++)
    {
        if (i != 0)
            display += ',';
        display += dn2cpp_reflection_type_spelling(arguments[i], true);
    }
    display += "](";
    for (int32_t i = 0; i < original.paramCount; i++)
    {
        Dn2CppParamInfo parameter = *original.parameters[i];
        parameter.paramType = resolve(i);
        parameter.reflectionSignature = nullptr;
        if (i != 0)
            display += ", ";
        display += dn2cpp_signature_kind(parameter.paramType) == 2
            ? dn2cpp_reflection_type_spelling(parameter.paramType->elementType) + " ByRef" : dn2cpp_reflection_type_spelling(parameter.paramType);
        std::string parameterDisplay = dn2cpp_reflection_type_spelling(parameter.paramType);
        if (parameter.name != nullptr)
            parameterDisplay += std::string(" ") + parameter.name;
        parameter.display = retain(parameterDisplay);
        node->parameters.push_back(parameter);
    }
    display += ')';
    node->row.display = retain(display);
    node->row.returnDisplay = retain(dn2cpp_reflection_type_spelling(node->row.returnType));
    node->row.parameters = node->parameters.data();
    g_open_methods.push_back(node);
    return &node->row;
}

// System.ValueType's type-info, found by name because no value type-info names it as
// its base. Null when the image emitted none; a value type then reaches no gated row.
static const Dn2CppTypeInfo* dn2cpp_meta_valuetype()
{
    static const Dn2CppTypeInfo* const valueType = dn2cpp_type_registry_find("System.ValueType", 16);
    return valueType;
}

// The next level of the chain the rows are inherited along. Every value type derives
// from System.ValueType and every array from System.Array, whose members Object's
// rows describe, though neither type-info names that base.
static const Dn2CppTypeInfo* dn2cpp_meta_next_level(const Dn2CppTypeInfo* ti)
{
    if (const auto* base = dn2cpp_reflection_base(ti))
        return base;
    if (ti == &dn2cpp_exception_type)
        return &dn2cpp_object_type;
    if ((ti->flags & DN2CPP_TF_ARRAY) != 0)
        return &dn2cpp_object_type;
    if ((ti->flags & DN2CPP_TF_VALUETYPE) != 0)
        return dn2cpp_meta_valuetype();
    return nullptr;
}

static bool dn2cpp_meta_declares(const Dn2CppTypeInfo* ti, const Dn2CppMetaMember* d)
{
    if (std::strcmp(d->typeName, "System.Exception") == 0)
        return ti == &dn2cpp_exception_type;
    return ti->name != nullptr && std::strcmp(ti->name, d->typeName) == 0;
}

// Whether a gated row stays reachable past `ti`: the level has a row for every method
// it declares under an Object member name, is an array, is Object or ValueType, whose
// members are these rows, or is a patch type, which overrides no Object member and
// which reflection reads as declaring none. A formal parameter declares no members.
static bool dn2cpp_meta_level_passes(const Dn2CppTypeInfo* ti)
{
    if (ti == &dn2cpp_exception_type)
        return true;
    if ((ti->flags & (DN2CPP_TF_OBJECT_MEMBER_ROWS | DN2CPP_TF_ARRAY | DN2CPP_TF_PATCH | DN2CPP_TF_GENERICPARAM)) != 0)
        return true;
    return ti->name != nullptr
        && (std::strcmp(ti->name, "System.Object") == 0 || std::strcmp(ti->name, "System.ValueType") == 0);
}

static bool dn2cpp_meta_params_match_types(const Dn2CppMetaMember* d, Dn2CppArrayRef* types)
{
    if (d->paramCount != types->length)
        return false;
    for (int32_t j = 0; j < types->length; j++)
    {
        auto* pt = reinterpret_cast<Dn2CppType*>(types->data[j]);
        if (pt == nullptr)
            dn2cpp_throw_argument_null_param("types");
        if (d->params[j].paramType != pt->typeInfo)
            return false;
    }
    return true;
}

// Whether an emitted row has a member's parameter list.
static bool dn2cpp_meta_same_params(const Dn2CppMethodInfo& row, const Dn2CppMetaMember* d)
{
    if (row.paramCount != d->paramCount)
        return false;
    for (int32_t j = 0; j < d->paramCount; j++)
        if (row.parameters[j]->paramType != d->params[j].paramType)
            return false;
    return true;
}

// The Object virtual whose name and parameters an instance virtual `row` has: the
// member it overrides, unless the row is a new slot, which starts a chain of its own.
static const Dn2CppMetaMember* dn2cpp_meta_object_virtual_named(const Dn2CppMethodInfo& row)
{
    if ((row.attrs & DN2CPP_MTHA_STATIC) != 0 || (row.ilAttrs & DN2CPP_MA_VIRTUAL) == 0
        || row.genericParamCount != 0)
        return nullptr;
    for (int32_t k = 0; k < g_meta_member_count; k++)
    {
        const Dn2CppMetaMember* d = &g_meta_members[k];
        if (d->gated && (d->ilAttrs & DN2CPP_MA_VIRTUAL) != 0 && std::strcmp(d->typeName, "System.Object") == 0
            && std::strcmp(d->methodName, row.name) == 0 && dn2cpp_meta_same_params(row, d))
            return d;
    }
    return nullptr;
}

static bool dn2cpp_meta_same_member(const Dn2CppMetaMember* a, const Dn2CppMetaMember* b)
{
    if (std::strcmp(a->methodName, b->methodName) != 0 || a->paramCount != b->paramCount)
        return false;
    for (int32_t j = 0; j < a->paramCount; j++)
        if (a->params[j].paramType != b->params[j].paramType)
            return false;
    return true;
}

// The body a call of Object virtual `root` dispatches to on an instance of `ti`: the
// type-info's dispatch field for that member, null for Object's own body.
static const void* dn2cpp_object_slot_body(const Dn2CppTypeInfo* ti, const Dn2CppMetaMember* root)
{
    if (std::strcmp(root->methodName, "ToString") == 0)
        return reinterpret_cast<const void*>(ti->tostring);
    if (std::strcmp(root->methodName, "Equals") == 0)
        return reinterpret_cast<const void*>(ti->equals);
    if (std::strcmp(root->methodName, "GetHashCode") == 0)
        return reinterpret_cast<const void*>(ti->gethashcode);
    return reinterpret_cast<const void*>(ti->finalize);
}

// Whether class level `ti` overrides Object virtual `root`, read from the dispatch
// fields: 1 when its field differs from its base's, 0 when they are equal, -1 when
// `ti` or its base has no vtable and the base is not Object. A level that inherits
// copies its base's field, so 1 is exact; 0 is not, since a folded identical body, an
// unemitted override or a patch level's copied field also leaves them equal.
static int dn2cpp_object_slot_overridden(const Dn2CppTypeInfo* ti, const Dn2CppMetaMember* root)
{
    const Dn2CppTypeInfo* base = ti->base;
    if (ti->vtable == nullptr || (ti->flags & DN2CPP_TF_VALUETYPE) != 0 || base == nullptr
        || (base->vtable == nullptr && base != &dn2cpp_object_type))
        return -1;
    return dn2cpp_object_slot_body(ti, root) != dn2cpp_object_slot_body(base, root) ? 1 : 0;
}

// Whether `row`, declared on class level `ti` and named as Object virtual `root`, is
// the body a call of `root` runs there whatever the levels above declare. A
// runtime-defined or vtable-less base gives no field to compare, so there the
// level's field must be the row's own body and not the base's.
static bool dn2cpp_object_slot_settles(const Dn2CppTypeInfo* ti, const Dn2CppMetaMember* root,
    const Dn2CppMethodInfo& row)
{
    const int overridden = dn2cpp_object_slot_overridden(ti, root);
    if (overridden != -1 || (ti->flags & DN2CPP_TF_VALUETYPE) != 0)
        return overridden == 1;
    const void* body = dn2cpp_object_slot_body(ti, root);
    return body != nullptr && body == row.fnPtr
        && (ti->base == nullptr || body != dn2cpp_object_slot_body(ti->base, root));
}

// The index Dn2CppItfImplSlots' System.Object row gives a virtual member of Object or
// ValueType, or -1 for any other member.
static int32_t dn2cpp_meta_object_virtual_index(const Dn2CppMetaMember* d)
{
    static constexpr const char* kMembers[] = { "ToString", "Equals", "GetHashCode", "Finalize" };
    if (d == nullptr || (d->ilAttrs & DN2CPP_MA_VIRTUAL) == 0)
        return -1;
    for (int32_t i = 0; i < static_cast<int32_t>(sizeof(kMembers) / sizeof(kMembers[0])); i++)
        if (std::strcmp(d->methodName, kMembers[i]) == 0)
            return i;
    return -1;
}

// The Object virtual a synthesized override replaces.
static const Dn2CppMetaMember* dn2cpp_meta_object_counterpart(const Dn2CppMetaMember* d)
{
    for (int32_t k = 0; k < g_meta_member_count; k++)
    {
        const Dn2CppMetaMember* o = &g_meta_members[k];
        if ((o->ilAttrs & DN2CPP_MA_VIRTUAL) != 0 && std::strcmp(o->typeName, "System.Object") == 0
            && dn2cpp_meta_same_member(o, d))
            return o;
    }
    return nullptr;
}

// Whether the emitted walk yielded a row of level `ti` with member `d`'s arity and
// parameters. A descriptor names its level by type name only, so a type of that
// name with real rows keeps them.
static bool dn2cpp_meta_emitted_on_level(const Dn2CppMethodCandidates& cands,
    const Dn2CppTypeInfo* ti, const Dn2CppMetaMember* d)
{
    for (int32_t i = 0; i < cands.size(); i++)
    {
        const auto row = cands[i].operator->();
        if (row->declaringType == ti && (row->attrs & DN2CPP_MTHA_METAANSWER) == 0
            && row->genericParamCount == d->genericArity && dn2cpp_meta_same_params(*row.operator->(), d))
            return true;
    }
    return false;
}

// Object's complete family is enumerable; partial intrinsic member catalogs are not.
static bool dn2cpp_meta_family(const Dn2CppMetaMember* d)
{
    return std::strcmp(d->typeName, "System.Object") == 0
        || std::strcmp(d->typeName, "System.ValueType") == 0
        || std::strcmp(d->typeName, "System.Exception") == 0;
}

static bool dn2cpp_meta_family_name(const Dn2CppMethodInfo& row)
{
    for (int32_t k = 0; k < g_meta_member_count; k++)
        if (dn2cpp_meta_family(&g_meta_members[k]) && std::strcmp(row.name, g_meta_members[k].methodName) == 0)
            return true;
    return false;
}

// Overrides replace their Object slot; new slots and nonvirtual names keep both rows.
static int32_t dn2cpp_meta_collect_family(const Dn2CppTypeInfo* type, int32_t flags,
    Dn2CppMethodCandidates& emitted, Dn2CppObject** out, Dn2CppString* name)
{
    int32_t n = 0;
    bool gatedOpen = true;
    for (const Dn2CppTypeInfo* ti = type; ti != nullptr; ti = dn2cpp_meta_next_level(ti))
    {
        dn2cpp_require_metadata(ti);
        for (int32_t k = 0; k < g_meta_member_count; k++)
        {
            const Dn2CppMetaMember* d = &g_meta_members[k];
            if (!dn2cpp_meta_family(d) || (d->gated && !gatedOpen) || !dn2cpp_meta_declares(ti, d)
                || !dn2cpp_member_matches(d->attrs, flags, ti != type)
                || (name != nullptr && !dn2cpp_member_name_matches(d->methodName, name, flags, false)))
                continue;
            bool present = false;
            const Dn2CppMetaMember* root = (d->ilAttrs & DN2CPP_MA_VIRTUAL) != 0
                ? dn2cpp_meta_object_counterpart(d) : nullptr;
            for (int32_t i = 0; i < emitted.size() && !present; i++)
            {
                const Dn2CppMethodInfo row = *emitted[i];
                present = row.declaringType == ti && row.genericParamCount == d->genericArity
                    && std::strcmp(row.name, d->methodName) == 0 && dn2cpp_meta_same_params(row, d);
                if (!present && root != nullptr)
                {
                    const auto base = dn2cpp_meta_object_base(emitted[i], row);
                    present = base && dn2cpp_meta_desc_of(base) == root;
                }
            }
            if (present)
                continue;
            const auto mi = dn2cpp_meta_row(d, ti, nullptr, 0);
            emitted.push_back(mi);
            if (out != nullptr)
                dn2cpp_gc_store_ref(&out[n], reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(mi, type)));
            n++;
        }
        if (flags & DN2CPP_BF_DECLAREDONLY)
            break;
        if (!dn2cpp_meta_level_passes(ti))
            gatedOpen = false;
    }
    return n;
}

// The image installs this exact managed row before startup; ordinary invocation owns its body.
static Dn2CppMetadataHandle<Dn2CppMethodInfo> g_member_not_found_factory_row;

void dn2cpp_set_member_not_found_factory_row(Dn2CppMetadataHandle<Dn2CppMethodInfo> method)
{
    g_member_not_found_factory_row = method;
}

// The named-lookup hook: appends the rows a lookup on `queried` finds after the
// emitted candidates, so dn2cpp_resolve_method_candidates lets an emitted override
// hide the Object or ValueType member it overrides and reports an overload beside one
// as ambiguous, as .NET does.
//
// It walks the BASE CHAIN, like the emitted walk: Object.MemberwiseClone is an
// inherited member of every reference type, so `obj.GetType().GetMethod(
// "MemberwiseClone", Instance | NonPublic)` has to find it on Object — and the row it
// hands back must name Object as its DeclaringType, which is why `declaring` is the
// matched link rather than the queried type. DeclaredOnly stops the walk for the same
// reason it stops the real one, and a gated row stops being reachable past a level
// dn2cpp_meta_level_passes refuses.
static void dn2cpp_meta_lookup(const Dn2CppTypeInfo* queried, Dn2CppString* name,
    int32_t genericParamCount, Dn2CppArrayRef* paramTypes, int32_t bindingFlags,
    Dn2CppMethodCandidates& cands)
{
    const auto factory = g_member_not_found_factory_row;
    const bool factoryNamed = factory != nullptr
        && dn2cpp_member_name_matches(factory->name, name, bindingFlags, false);
    bool named = factoryNamed;
    for (int32_t k = 0; k < g_meta_member_count && !named; k++)
        named = dn2cpp_member_name_matches(g_meta_members[k].methodName, name, bindingFlags, false);
    if (!named)
        return;
    bool gatedOpen = true;
    for (const Dn2CppTypeInfo* ti = queried; ti != nullptr; ti = dn2cpp_meta_next_level(ti))
    {
        bool inherited = (ti != queried);
        if (factoryNamed)
        {
            const auto row = factory.operator->();
            if (row->declaringType == ti && dn2cpp_member_matches(row->attrs, bindingFlags, inherited)
                && (genericParamCount < 0 || row->genericParamCount == genericParamCount)
                && (paramTypes == nullptr || dn2cpp_params_match_types(factory, paramTypes, queried)))
                cands.push_back(factory);
        }
        for (int32_t k = 0; k < g_meta_member_count; k++)
        {
            const Dn2CppMetaMember* d = &g_meta_members[k];
            if ((d->gated && !gatedOpen) || !dn2cpp_member_name_matches(d->methodName, name, bindingFlags, false)
                || !dn2cpp_meta_declares(ti, d))
                continue;
            if (genericParamCount >= 0 && genericParamCount != d->genericArity)
                continue;
            if (paramTypes != nullptr && !dn2cpp_meta_params_match_types(d, paramTypes))
                continue;
            if (!dn2cpp_member_matches(d->attrs, bindingFlags, inherited)
                || dn2cpp_meta_emitted_on_level(cands, ti, d))
                continue;
            cands.push_back(dn2cpp_meta_row(d, ti, nullptr, 0));
        }
        if (bindingFlags & DN2CPP_BF_DECLAREDONLY)
            break;
        if (!dn2cpp_meta_level_passes(ti))
            gatedOpen = false;
    }
}

Dn2CppMethodRef* dn2cpp_type_get_method_full(Dn2CppType* t, Dn2CppString* name,
                                             int32_t genericParamCount, Dn2CppArrayRef* paramTypes,
                                             int32_t bindingFlags, int32_t callConv,
                                             Dn2CppObject* binder, int32_t declared)
{
    (void)callConv; // observably a no-op filter in real .NET (see the note above)
    dn2cpp_type_require(t);
    // Type.GetMethod validates every argument, a null element of `types` included,
    // before any lookup or binder use.
    if (name == nullptr)
        dn2cpp_throw_argument_null_param("name");
    if ((declared & DN2CPP_LOOKUP_ARITY) != 0 && genericParamCount < 0)
        dn2cpp_throw_argument_out_of_range_value(DN2CPP_SR_MUST_BE_NON_NEGATIVE,
            "genericParameterCount", genericParamCount);
    if ((declared & DN2CPP_LOOKUP_TYPES) != 0 && paramTypes == nullptr)
        dn2cpp_throw_argument_null_param("types");
    if (paramTypes != nullptr)
        for (int32_t j = 0; j < paramTypes->length; j++)
            if (paramTypes->data[j] == nullptr)
                dn2cpp_throw_argument_null_param("types");
    dn2cpp_reflection_check_binder(binder);
    // Collect every candidate derived-first: BindingFlags + name + optional
    // generic-arity / parameter-type filters, with virtual-slot hiding (an
    // override hides its base definition; a `new` non-virtual keeps both and
    // is resolved by the sig-equality rule above).
    Dn2CppMethodCandidates cands;
    Dn2CppSeenSlots seen;
    Dn2CppGvmHiding gvm;
    gvm.reflected = t->typeInfo;
    for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr; ti = dn2cpp_reflection_base(ti))
    {
        dn2cpp_require_metadata(ti);
        const auto reflection = ti->reflection();
        bool inherited = (ti != t->typeInfo);
        for (int32_t i = 0; i < reflection.methodCount; i++)
        {
            const auto mi = reflection.methods[i];
            const auto row = mi.operator->();
            if (!dn2cpp_member_matches(row->attrs, bindingFlags, inherited))
                continue;
            bool hidden = row->vtableSlot >= 0 && seen.seen(row->vtableSlot);
            if (hidden || !dn2cpp_member_name_matches(row->name, name, bindingFlags, false))
                continue;
            if (gvm.hides(mi, *row.operator->()))
                continue;
            if (genericParamCount >= 0 && row->genericParamCount != genericParamCount)
                continue;
            if (paramTypes != nullptr && !dn2cpp_params_match_types(mi, paramTypes, t->typeInfo))
                continue;
            cands.push_back(mi);
        }
        if (bindingFlags & DN2CPP_BF_DECLAREDONLY)
            break;
        gvm.next_type();
    }
    dn2cpp_meta_lookup(t->typeInfo, name, genericParamCount, paramTypes, bindingFlags, cands);
    if (cands.size() == 0)
        return nullptr;
    const Dn2CppMetadataHandle<Dn2CppMethodInfo> hit =
        dn2cpp_resolve_method_candidates(cands.data(), cands.size(), t->typeInfo);
    if (hit->genericParamCount > 0)
        return dn2cpp_make_methodref_defview(hit, t->typeInfo);
    return dn2cpp_make_methodref(hit, t->typeInfo);
}

Dn2CppMethodRef* dn2cpp_type_get_method(Dn2CppType* t, Dn2CppString* name, int32_t bindingFlags)
{
    return dn2cpp_type_get_method_full(t, name, -1, nullptr, bindingFlags, 0, nullptr, 0);
}

Dn2CppType* dn2cpp_methodref_return_type(Dn2CppMethodRef* m)
{
    const auto mi = dn2cpp_methodref_require(m);
    const Dn2CppMethodInfo row = *mi;
    const auto* declaring = dn2cpp_invoke_declaring(row.declaringType, m->reflectedType);
    const auto* type = (m->isGenericDefView != 0 || (row.declaringType->flags & DN2CPP_TF_GENERICDEF) != 0) && row.genericDefinitionReturnKey != nullptr
        ? dn2cpp_method_definition_type(mi, row.genericDefinitionReturnKey, row.returnType, declaring)
        : row.returnReflectionSignature != nullptr
            ? dn2cpp_signature_type(row.returnReflectionSignature, declaring) : row.returnType;
    return dn2cpp_get_type_from_handle(type);
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
    const Dn2CppMethodInfo row = *dn2cpp_methodref_require(m);
    int32_t n = row.paramCount;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
    {
        Dn2CppMetadataHandle<Dn2CppParamInfo> param = row.parameters[i];
        if (m->isGenericDefView != 0)
        {
            const Dn2CppParamInfo decoded = *param;
            if (decoded.genericDefinitionDisplay != nullptr || decoded.genericDefinitionKey != nullptr)
            {
                auto* openParam = static_cast<Dn2CppParamInfo*>(dn2cpp_alloc(sizeof(Dn2CppParamInfo)));
                *openParam = decoded;
                openParam->display = decoded.genericDefinitionDisplay;
                param = openParam;
            }
        }
        auto* p = static_cast<Dn2CppParamRef*>(dn2cpp_alloc(sizeof(Dn2CppParamRef)));
        p->type = &dn2cpp_parameterinfo_type;
        p->param = param;
        p->position = i;
        p->owner = m->method;
        p->ownerReflected = m->reflectedType;
        p->ownerGenericDefView = m->isGenericDefView;
        dn2cpp_gc_store_ref(&arr->data[i], reinterpret_cast<Dn2CppObject*>(p));
    }
    return arr;
}

// MethodInfo.ReturnParameter: a synthesized ParameterInfo over the return type
// (Position -1, Name null — matching real .NET's return pseudo-parameter).
Dn2CppObject* dn2cpp_methodref_return_parameter(Dn2CppMethodRef* m)
{
    const Dn2CppMethodInfo row = *dn2cpp_methodref_require(m);
    auto* pi = static_cast<Dn2CppParamInfo*>(dn2cpp_alloc(sizeof(Dn2CppParamInfo)));
    *pi = Dn2CppParamInfo{};
    pi->paramType = row.returnType;
    pi->reflectionSignature = row.returnReflectionSignature;
    pi->genericDefinitionKey = row.genericDefinitionReturnKey;
    pi->requiredCustomModifiers = row.returnRequiredCustomModifiers;
    pi->requiredCustomModifierCount = row.returnRequiredCustomModifierCount;
    pi->optionalCustomModifiers = row.returnOptionalCustomModifiers;
    pi->optionalCustomModifierCount = row.returnOptionalCustomModifierCount;
    pi->customModifiersKnown = row.returnCustomModifiersKnown;
    pi->display = m->isGenericDefView != 0 && row.genericDefinitionReturnDisplay != nullptr
        ? row.genericDefinitionReturnDisplay : row.returnDisplay;
    auto* p = static_cast<Dn2CppParamRef*>(dn2cpp_alloc(sizeof(Dn2CppParamRef)));
    p->type = &dn2cpp_parameterinfo_type;
    p->param = pi;
    p->position = -1;
    p->owner = m->method;
    p->ownerReflected = m->reflectedType;
    p->ownerGenericDefView = m->isGenericDefView;
    return reinterpret_cast<Dn2CppObject*>(p);
}

Dn2CppType* dn2cpp_paramref_parameter_type(Dn2CppParamRef* p)
{
    const Dn2CppParamInfo parameter = *dn2cpp_paramref_require(p)->param;
    // Display and attributes remain usable without a composed open Type handle.
    const auto* declaring = p->owner ? dn2cpp_invoke_declaring(p->owner->declaringType, p->ownerReflected) : nullptr;
    return dn2cpp_get_type_from_handle((p->ownerGenericDefView != 0 || (p->owner && (p->owner->declaringType->flags & DN2CPP_TF_GENERICDEF) != 0)) && parameter.genericDefinitionKey != nullptr
        ? dn2cpp_method_definition_type(p->owner, parameter.genericDefinitionKey, parameter.paramType, declaring, p->position)
        : dn2cpp_reflection_parameter_type(parameter, declaring));
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

void dn2cpp_throw_target_invocation(Dn2CppObject* inner)
{
    Dn2CppObject* wrapper = dn2cpp_exception_new(&dn2cpp_target_invocation_exception_type,
        dn2cpp_default_message(&dn2cpp_target_invocation_exception_type), inner);
    reinterpret_cast<Dn2CppExceptionObject*>(wrapper)->hresult = static_cast<int32_t>(0x80131604u);
    dn2cpp_exc_inflight_pop(inner);
    dn2cpp_throw(wrapper);
}

// Reflection validation and image-boundary faults occur before the target runs.
// None is wrapped in TargetInvocationException.
[[noreturn]] static void dn2cpp_throw_reflection_fault(const Dn2CppTypeInfo* ti,
    Dn2CppString* message, uint32_t hresult)
{
    Dn2CppObject* e = dn2cpp_exception_new(ti,
        message != nullptr ? message : dn2cpp_default_message(ti), nullptr);
    reinterpret_cast<Dn2CppExceptionObject*>(e)->hresult = static_cast<int32_t>(hresult);
    dn2cpp_throw(e);
}

// A row reflection found with no body to run. A template row's diagnosis names the
// concrete declaring instantiation, whose code exists only for the members its
// template carried; a preserve rule admits a member the template could run
// (Dn2CppMethodInfo::preservable). Decode the display only on failure, outside the
// successful invocation plan path.
[[noreturn]] static void dn2cpp_throw_invoke_unavailable(Dn2CppMetadataHandle<Dn2CppMethodInfo> mi,
    const Dn2CppTypeInfo* reflected)
{
    const Dn2CppMethodInfo row = *mi;
    const Dn2CppTypeInfo* declaring = dn2cpp_invoke_declaring(row.declaringType, reflected);
    const char* name = row.name != nullptr ? row.name : "?";
    const std::string member = row.display != nullptr ? dn2cpp_utf8_of(dn2cpp_metadata_string(row.display)) : name;
    const int32_t runtimeInstantiation = DN2CPP_TF_RUNTIME_SYNTH | DN2CPP_TF_RUNTIME_TEMPLATE;
    if (((declaring->flags | row.declaringType->flags) & runtimeInstantiation) != 0
        && row.preservable == 0)
        dn2cpp_throw_unretained(declaring, dn2cpp_unretained_subject(declaring, member),
            kDn2CppUnretainedClosedGeneric, nullptr, nullptr);
    dn2cpp_throw_unretained(declaring, dn2cpp_unretained_subject(declaring, member),
        kDn2CppUnretainedMember, "method", name);
}

// A constructor binding found no row, but `ti` declares a constructor the image omits
// a row for (DN2CPP_TA_OMITTED_*): the binder cannot tell .NET's MissingMethodException
// from a constructor it was not given. A runtime instantiation's code is whatever its
// template carried.
[[noreturn]] static void dn2cpp_throw_omitted_ctor(const Dn2CppTypeInfo* ti, const std::string& member)
{
    if ((ti->flags & (DN2CPP_TF_RUNTIME_SYNTH | DN2CPP_TF_RUNTIME_TEMPLATE)) != 0)
        dn2cpp_throw_unretained(ti, dn2cpp_unretained_subject(ti, member),
            kDn2CppUnretainedClosedGeneric, nullptr, nullptr);
    dn2cpp_throw_unretained(ti, dn2cpp_unretained_subject(ti, member),
        kDn2CppUnretainedMember, "method", ".ctor");
}

[[noreturn]] static void dn2cpp_throw_invoke_target(const Dn2CppObject* obj,
    const Dn2CppTypeInfo* declaring)
{
    Dn2CppString* message;
    if (obj == nullptr)
        message = dn2cpp_sr_message(DN2CPP_SR_TARGET_REQUIRED, nullptr, 0);
    else
    {
        Dn2CppString* names[2] = { dn2cpp_type_tostring(declaring), dn2cpp_type_tostring(obj->type) };
        message = dn2cpp_sr_message(DN2CPP_SR_TARGET_MISMATCH, names, 2);
    }
    dn2cpp_throw_reflection_fault(&dn2cpp_target_exception_type, message, 0x80131603u);
}

[[noreturn]] static void dn2cpp_throw_invoke_parameter_count()
{
    dn2cpp_throw_reflection_fault(&dn2cpp_target_parameter_count_exception_type,
        dn2cpp_sr_message(DN2CPP_SR_PARAMETER_COUNT, nullptr, 0), 0x8002000Eu);
}

// .NET finds no entry point for a static virtual or abstract interface member
// bound to a delegate.
[[noreturn]] static void dn2cpp_throw_static_virtual_entry_point()
{
    dn2cpp_throw_reflection_fault(&dn2cpp_entry_point_not_found_exception_type, nullptr, 0x80131523u);
}

// A fault .NET raises inside the region it wraps like a fault of the target:
// wrapped in TargetInvocationException unless the caller asked otherwise.
[[noreturn]] static void dn2cpp_throw_invoke_wrapped(const Dn2CppTypeInfo* ti,
    Dn2CppString* message, uint32_t hresult, bool wrapExceptions)
{
    Dn2CppObject* fault = dn2cpp_exception_new(ti,
        message != nullptr ? message : dn2cpp_default_message(ti), nullptr);
    reinterpret_cast<Dn2CppExceptionObject*>(fault)->hresult = static_cast<int32_t>(hresult);
    if (!wrapExceptions)
        dn2cpp_throw(fault);
    dn2cpp_throw_target_invocation(fault);
}

// .NET runs no body for a static abstract interface member: Invoke faults as bad
// IL, wrapped like a fault of the target unless the caller asked otherwise.
[[noreturn]] static void dn2cpp_throw_invoke_static_abstract(bool wrapExceptions)
{
    dn2cpp_throw_invoke_wrapped(&dn2cpp_bad_image_format_exception_type,
        dn2cpp_sr_message(DN2CPP_SR_BAD_IL_FORMAT, nullptr, 0), 0x8007000Bu, wrapExceptions);
}

// .NET calls a bodiless row bound to a null receiver without dispatch, which faults
// as a bad image with the HResult's system message.
[[noreturn]] static void dn2cpp_throw_null_bound_bodiless()
{
#ifdef _WIN32
    static constexpr char text[] = "An attempt was made to load a program with an incorrect format.\r\n (0x8007000B)";
#else
    static constexpr char text[] = "An attempt was made to load a program with an incorrect format.\n (0x8007000B)";
#endif
    dn2cpp_throw_reflection_fault(&dn2cpp_bad_image_format_exception_type,
        dn2cpp_string_from_utf8(text, static_cast<int32_t>(sizeof text - 1)), 0x8007000Bu);
}

// `suffix` spells the by-ref ("&") or pointer ("*") type over `to`, which .NET
// names in the message while this image has no Type for it.
[[noreturn]] static void dn2cpp_throw_invoke_argument(const Dn2CppTypeInfo* from,
    const Dn2CppTypeInfo* to, const char* suffix = "")
{
    Dn2CppString* target = dn2cpp_type_tostring(to);
    if (*suffix != '\0')
        target = dn2cpp_string_concat2(target,
            dn2cpp_string_from_utf8(suffix, static_cast<int32_t>(std::strlen(suffix))));
    Dn2CppString* names[2] = { dn2cpp_type_tostring(from), target };
    dn2cpp_throw_reflection_fault(&dn2cpp_argument_exception_type,
        dn2cpp_sr_message(DN2CPP_SR_OBJECT_CONVERSION, names, 2), 0x80070057u);
}

[[noreturn]] static void dn2cpp_throw_invoke_not_supported(const char* message)
{
    dn2cpp_throw_reflection_fault(&dn2cpp_not_supported_exception_type,
        message != nullptr ? dn2cpp_string_from_utf8(message, static_cast<int32_t>(std::strlen(message)))
                           : nullptr,
        0x80131515u);
}

static bool dn2cpp_prim_widens(int32_t src, int32_t dst);
static Dn2CppObject* dn2cpp_binder_adapt_arg(Dn2CppObject* a, const Dn2CppTypeInfo* p);

static const Dn2CppTypeInfo* dn2cpp_enum_underlying_or_self(const Dn2CppTypeInfo* ti)
{
    if ((ti->flags & DN2CPP_TF_ENUM) == 0)
        return ti;
    return ti->enumUnderlying != nullptr ? ti->enumUnderlying : &dn2cpp_int32_type;
}

// A parameter the invoker thunk receives headerless: CultureInfo, NumberFormatInfo,
// TextInfo as the culture pointer, Assembly and Module as the
// name. The thunk unwraps a wrapper and passes anything else through as the raw
// handle an erased path can leave in the argument array, whose first word is no
// type-info. Each name is "System." followed by Gl or Re, so a parameter type
// outside those prefixes is rejected without a string compare.
static bool dn2cpp_invoke_headerless_param(const Dn2CppTypeInfo* p)
{
    static constexpr char prefix[] = "System.";
    const char* n = p->name;
    if (n == nullptr)
        return false;
    for (std::size_t i = 0; i + 1 < sizeof prefix; i++)
        if (n[i] != prefix[i])
            return false;
    const char* rest = n + sizeof prefix - 1;
    if (rest[0] == 'G' && rest[1] == 'l')
        return std::strcmp(rest, "Globalization.CultureInfo") == 0
            || std::strcmp(rest, "Globalization.NumberFormatInfo") == 0
            || std::strcmp(rest, "Globalization.TextInfo") == 0;
    if (rest[0] == 'R' && rest[1] == 'e')
        return std::strcmp(rest, "Reflection.Assembly") == 0
            || std::strcmp(rest, "Reflection.Module") == 0;
    return false;
}

// RuntimeType.CheckValue for one non-null argument: the value to pass, or the
// conversion ArgumentException. Besides an argument the parameter admits, .NET
// takes a boxed U for Nullable<U>, and an enum or primitive for an enum or
// primitive parameter whose underlying type CanPrimitiveWiden reaches from the
// argument's. Those re-box in the representation the invoker thunk reads.
static Dn2CppObject* dn2cpp_invoke_check_arg(Dn2CppObject* arg, const Dn2CppTypeInfo* expected)
{
    if (dn2cpp_invoke_headerless_param(expected))
        return arg;
    if (dn2cpp_typeinfo_assignable(arg->type, expected) != 0)
        return arg;
    if ((expected->flags & DN2CPP_TF_VALUETYPE) != 0)
    {
        if (const Dn2CppTypeInfo* u = dn2cpp_nullable_underlying_ti(expected))
        {
            if (arg->type == u)
                return dn2cpp_binder_adapt_arg(arg, expected);
        }
        else
        {
            int32_t to = dn2cpp_prim_code(dn2cpp_enum_underlying_or_self(expected));
            int32_t from = dn2cpp_prim_code(dn2cpp_enum_underlying_or_self(arg->type));
            if (to >= 0 && from >= 0 && dn2cpp_prim_widens(from, to))
                return from == to ? arg : dn2cpp_binder_adapt_arg(arg, expected);
        }
    }
    dn2cpp_throw_invoke_argument(arg->type, expected);
}

static const Dn2CppTypeInfo* g_missing_type = nullptr;
static Dn2CppObject* (*g_missing_value_factory)() = nullptr;

void dn2cpp_set_missing_value_factory(const Dn2CppTypeInfo* type, Dn2CppObject* (*factory)())
{
    g_missing_type = type;
    g_missing_value_factory = factory;
}

static bool dn2cpp_invoke_is_missing(Dn2CppObject* value)
{
    return value != nullptr && value->type == g_missing_type
        && g_missing_value_factory != nullptr && value == g_missing_value_factory();
}

// Default substitution precedes conversion. Only successful calls copy back, and
// CheckValue suppresses a by-value copy-back when the default needs conversion.
static Dn2CppObject* dn2cpp_invoke_default(const Dn2CppParamInfo& parameter,
    Dn2CppObject* missing, int32_t index, int32_t argc, Dn2CppArrayI4** copyBack)
{
    Dn2CppObject* value;
    if (parameter.defaultValue != nullptr)
        value = parameter.defaultValue();
    else if ((parameter.ilAttrs & 0x1000) != 0)
        dn2cpp_throw_platform_not_supported("ParameterInfo: the default value is not recorded in the image");
    else if ((parameter.ilAttrs & 0x10) != 0)
        value = missing;
    else
        dn2cpp_throw_argument_text(&dn2cpp_argument_exception_type,
            "Missing parameter does not have a default value.", "parameters");
    const Dn2CppTypeInfo* type = parameter.paramType;
    if (copyBack != nullptr && parameter.passKind == 0 && type != nullptr
        && (value != nullptr ? value->type == type || dn2cpp_nullable_underlying_ti(type) != nullptr
            : (type->flags & DN2CPP_TF_VALUETYPE) == 0))
    {
        if (*copyBack == nullptr)
            *copyBack = dn2cpp_newarr_i4(argc);
        (*copyBack)->data[index] = 1;
    }
    return value;
}

static void dn2cpp_invoke_write_defaults(Dn2CppMetadataTable<Dn2CppParamInfo> parameters,
    Dn2CppObject** prepared, Dn2CppObject** caller, const Dn2CppArrayI4* copyBack, int32_t argc)
{
    if (copyBack == nullptr)
        return;
    for (int32_t i = 0; i < argc; i++)
        if (copyBack->data[i] != 0)
            dn2cpp_gc_store_ref(&caller[i], dn2cpp_invoke_box_result(parameters[i]->paramType, prepared[i]));
}

// The argument list the invoker thunk receives: `args` itself unless an argument
// converts, then a copy, since a by-value conversion never writes the caller's
// array. The caller has already matched argc against the row's parameter count.
// `types` holds the parameter types when the caller's plan carries them;
// otherwise the row's parameter table answers.
static Dn2CppObject** dn2cpp_invoke_check_args(Dn2CppMetadataHandle<Dn2CppMethodInfo> mi,
    const Dn2CppTypeInfo* const* types, Dn2CppObject** args, int32_t argc,
    Dn2CppArrayI4** defaultCopyBack, const Dn2CppTypeInfo* declaring)
{
    if (argc == 0)
        return args;
    Dn2CppMetadataTable<Dn2CppParamInfo> parameters{};
    if (types == nullptr)
        parameters = mi->parameters;
    Dn2CppArrayRef* copy = nullptr;
    for (int32_t i = 0; i < argc; i++)
    {
        if (args[i] == nullptr)
            continue;
        Dn2CppObject* arg = args[i];
        if (dn2cpp_invoke_is_missing(arg))
        {
            if (types != nullptr)
                parameters = mi->parameters;
            arg = dn2cpp_invoke_default(*parameters[i].operator->().operator->(), arg, i, argc, defaultCopyBack);
        }
        if (arg != nullptr)
            arg = dn2cpp_invoke_check_arg(arg, types != nullptr ? types[i]
                : dn2cpp_invoke_parameter_type(*parameters[i], declaring));
        if (arg == args[i])
            continue;
        if (copy == nullptr)
        {
            copy = dn2cpp_newarr_ref(argc);
            for (int32_t k = 0; k < argc; k++)
                dn2cpp_gc_store_ref(&copy->data[k], args[k]);
        }
        dn2cpp_gc_store_ref(&copy->data[i], arg);
    }
    return copy != nullptr ? copy->data : args;
}

// A by-ref argument's referent, stored as a field of its type is
// (dn2cpp_array_store_boxed), behind the header its write-back reads.
struct Dn2CppInvokeCell
{
    const Dn2CppTypeInfo* type;
    int32_t size;
    int32_t isRef;
};
constexpr size_t kInvokeCellData = 16;
static_assert(sizeof(Dn2CppInvokeCell) <= kInvokeCellData);

// The bytes a field of the referent type `t` occupies; a type whose layout the
// image does not state is refused.
static int32_t dn2cpp_invoke_cell_size(const Dn2CppTypeInfo* t)
{
    if ((t->flags & DN2CPP_TF_VALUETYPE) == 0)
        return static_cast<int32_t>(sizeof(Dn2CppObject*));
    int32_t code = dn2cpp_prim_code(dn2cpp_enum_underlying_or_self(t));
    if (code >= 0)
        return dn2cpp_prim_storage_width(code);
    if ((t->flags & DN2CPP_TF_LAYOUT_UNKNOWN) != 0 || t->instanceSize <= 0)
    {
        char buf[512];
        std::snprintf(buf, sizeof(buf), "MethodBase.Invoke: the layout of '%s' is not in this image.",
            t->name != nullptr ? t->name : "?");
        dn2cpp_throw_invoke_not_supported(buf);
    }
    return t->instanceSize;
}

// RuntimeType.TryChangeType for a by-ref parameter: an instance of the referent's
// type, null for its default, or a boxed U for Nullable<U>. No primitive widens.
static bool dn2cpp_invoke_byref_accepts(Dn2CppObject* arg, const Dn2CppTypeInfo* t)
{
    if (arg == nullptr)
        return true;
    if ((t->flags & DN2CPP_TF_VALUETYPE) == 0)
        return dn2cpp_typeinfo_assignable(arg->type, t) != 0;
    return arg->type == t || (arg->type != nullptr && arg->type == dn2cpp_nullable_underlying_ti(t));
}

// The System.Reflection.Pointer class the image boxes reflected pointers as.
static const Dn2CppTypeInfo* g_pointer_box_ti = nullptr;
static Dn2CppObject* (*g_pointer_box_make)(void* value, Dn2CppType* type) = nullptr;
static void* (*g_pointer_box_read)(Dn2CppObject* box, Dn2CppType** type) = nullptr;

void dn2cpp_set_pointer_box_type(const Dn2CppTypeInfo* ti,
    Dn2CppObject* (*make)(void* value, Dn2CppType* type),
    void* (*read)(Dn2CppObject* box, Dn2CppType** type))
{
    g_pointer_box_make = make;
    g_pointer_box_read = read;
    g_pointer_box_ti = ti;
}

struct Dn2CppPointerTiNode
{
    const Dn2CppTypeInfo* pointee;
    const Dn2CppTypeInfo* ti;
    Dn2CppPointerTiNode* next;
};

// The type-info of a pointer to `pointee` at `depth` levels that a Pointer box
// carries as its pointer type, interned per pointee so equal pointer types share
// one. A Type over it reaches no user code; it identifies the box's pointer type
// to the argument check.
static const Dn2CppTypeInfo* dn2cpp_pointer_ti(const Dn2CppTypeInfo* pointee, int32_t depth)
{
    static std::mutex& mtx = dn2cpp_never_destroyed<std::mutex>();
    static Dn2CppPointerTiNode* head = nullptr;
    std::lock_guard<std::mutex> lk(mtx);
    for (int32_t level = 0; level < depth; level++)
    {
        const Dn2CppTypeInfo* found = nullptr;
        for (Dn2CppPointerTiNode* n = head; n != nullptr && found == nullptr; n = n->next)
            if (n->pointee == pointee)
                found = n->ti;
        if (found == nullptr)
        {
            const size_t length = std::strlen(pointee->name);
            char* name = static_cast<char*>(dn2cpp_alloc_pinned(length + 2));
            std::memcpy(name, pointee->name, length);
            name[length] = '*';
            name[length + 1] = '\0';
            auto* ti = static_cast<Dn2CppTypeInfo*>(dn2cpp_alloc_pinned(sizeof(Dn2CppTypeInfo)));
            *ti = Dn2CppTypeInfo{};
            ti->name = name;
            ti->flags = DN2CPP_TF_SEALED;
            ti->elementType = pointee;
            auto* node = static_cast<Dn2CppPointerTiNode*>(dn2cpp_alloc_pinned(sizeof(Dn2CppPointerTiNode)));
            node->pointee = pointee;
            node->ti = ti;
            node->next = head;
            head = node;
            found = ti;
        }
        pointee = found;
    }
    return pointee;
}

// The width .NET interchanges a primitive pointee at, as int* with uint* or an
// enum with its underlying type, or 0 for bool, char and a non-primitive.
static int32_t dn2cpp_pointee_width(const Dn2CppTypeInfo* t, bool* isFloat)
{
    t = dn2cpp_enum_underlying_or_self(t);
    *isFloat = t == &dn2cpp_single_type || t == &dn2cpp_double_type;
    if (t == &dn2cpp_intptr_type || t == &dn2cpp_uintptr_type)
        return static_cast<int32_t>(sizeof(void*));
    if (t == &dn2cpp_bool_type || t == &dn2cpp_char_type)
        return 0;
    const int32_t code = dn2cpp_prim_code(t);
    return code >= 0 ? dn2cpp_prim_storage_width(code) : 0;
}

// Whether .NET passes a Pointer box of pointer type `from` to a parameter of
// `pointee` at `depth` levels: void* takes any, an equal type passes, and one
// level deep a primitive pointee passes for another of its width and kind.
static bool dn2cpp_pointer_box_accepts(const Dn2CppTypeInfo* from, const Dn2CppTypeInfo* pointee,
    int32_t depth)
{
    if (depth == 1 && pointee == &dn2cpp_void_type)
        return true;
    if (from == nullptr)
        return false;
    if (from == dn2cpp_pointer_ti(pointee, depth))
        return true;
    if (depth != 1)
        return false;
    bool fromFloat = false, toFloat = false;
    const int32_t width = dn2cpp_pointee_width(from->elementType, &fromFloat);
    return width != 0 && width == dn2cpp_pointee_width(pointee, &toFloat) && fromFloat == toFloat;
}

// Invoke's box of a pointer result: an unmanaged pointer to `pointee` at `depth`
// levels as a System.Reflection.Pointer, a function pointer (null pointee) as an
// IntPtr.
Dn2CppObject* dn2cpp_invoke_box_pointer(void* value, const Dn2CppTypeInfo* pointee,
    int32_t depth)
{
    if (pointee == nullptr)
        return dn2cpp_box(&dn2cpp_intptr_type, &value, sizeof value);
    if (g_pointer_box_make == nullptr)
        dn2cpp_throw_invoke_not_supported("This image cannot box a pointer without System.Reflection.Pointer.");
    return g_pointer_box_make(value, dn2cpp_get_type_from_handle(dn2cpp_pointer_ti(pointee, depth)));
}

// The suffix naming a pointer parameter's type after its innermost pointee.
static const char* dn2cpp_pointer_suffix(char (&buf)[260], int32_t depth, bool byRef)
{
    int32_t n = 0;
    while (n < depth && n < 256)
        buf[n++] = '*';
    if (byRef)
        buf[n++] = '&';
    buf[n] = '\0';
    return buf;
}

// The value the invoker thunk receives for one parameter the row marks with
// DN2CPP_PASS_* bits, refusing what .NET refuses: a by-ref argument passes as the
// address of a fresh cell holding a copy of it, a pointer as a boxed IntPtr's
// value or a System.Reflection.Pointer's of a type the parameter takes; no argument
// converts to a by-ref-like type. `cell` receives the cell.
static Dn2CppObject* dn2cpp_invoke_pass_arg(const Dn2CppParamInfo& param, Dn2CppObject* arg,
    bool wrapExceptions, char** cell, const Dn2CppTypeInfo* declaring = nullptr)
{
    const int32_t kind = param.passKind;
    const Dn2CppTypeInfo* t = dn2cpp_invoke_parameter_type(param, declaring, true);
    const bool byRef = (kind & DN2CPP_PASS_BYREF) != 0;
    if ((kind & DN2CPP_PASS_UNSUPPORTED) != 0 || t == nullptr)
    {
        char buf[512];
        std::snprintf(buf, sizeof(buf),
            "MethodBase.Invoke: this image cannot copy the referent of by-ref parameter '%s'.",
            param.name != nullptr ? param.name : "?");
        dn2cpp_throw_invoke_not_supported(buf);
    }
    if ((kind & DN2CPP_PASS_BYREFLIKE) != 0)
    {
        if (arg == nullptr)
            dn2cpp_throw_invoke_not_supported("Cannot create boxed ByRef-like values.");
        dn2cpp_throw_invoke_argument(arg->type, t, byRef ? "&" : "");
    }
    if ((kind & DN2CPP_PASS_POINTER) != 0)
    {
        const int32_t depth = ((kind >> DN2CPP_PASS_POINTER_DEPTH_SHIFT) & 0xFF) + 1;
        // A function pointer parameter's passType is its own type.
        const int32_t named = (kind & DN2CPP_PASS_FNPTR) != 0 ? 0 : depth;
        char suffix[260];
        if (byRef)
        {
            if (arg == nullptr)
                dn2cpp_throw_null_reference();
            dn2cpp_throw_invoke_argument(arg->type, t, dn2cpp_pointer_suffix(suffix, named, true));
        }
        if (arg == nullptr)
        {
            if ((kind & DN2CPP_PASS_FNPTR) != 0)
                dn2cpp_throw_invoke_wrapped(&dn2cpp_null_reference_exception_type, nullptr,
                    0x80004003u, wrapExceptions);
            return nullptr;
        }
        if (arg->type == &dn2cpp_intptr_type)
            return *reinterpret_cast<Dn2CppObject**>(arg + 1);
        if ((kind & DN2CPP_PASS_FNPTR) == 0 && g_pointer_box_ti != nullptr
            && arg->type == g_pointer_box_ti)
        {
            Dn2CppType* type = nullptr;
            void* value = g_pointer_box_read(arg, &type);
            // Public Pointer.Box types use query companions; legacy Invoke boxes
            // keep their ABI identities. Only the full descriptor can equate them.
            if (type != nullptr && dn2cpp_signature_kind(type->typeInfo) == 1
                && param.reflectionSignature != nullptr
                && type->typeInfo == dn2cpp_signature_type(param.reflectionSignature, declaring))
                return static_cast<Dn2CppObject*>(value);
            if (dn2cpp_pointer_box_accepts(type != nullptr ? type->typeInfo : nullptr, t, depth))
                return static_cast<Dn2CppObject*>(value);
        }
        if ((kind & DN2CPP_PASS_FNPTR) == 0 && depth == 1 && t == &dn2cpp_void_type)
            dn2cpp_throw_invoke_not_supported(nullptr);
        dn2cpp_throw_invoke_argument(arg->type, t, dn2cpp_pointer_suffix(suffix, named, false));
    }
    if (!dn2cpp_invoke_byref_accepts(arg, t))
        dn2cpp_throw_invoke_argument(arg->type, t, "&");
    const int32_t size = dn2cpp_invoke_cell_size(t);
    const bool isRef = (t->flags & DN2CPP_TF_VALUETYPE) == 0;
    *cell = static_cast<char*>(dn2cpp_alloc(kInvokeCellData + static_cast<size_t>(size)));
    auto* header = reinterpret_cast<Dn2CppInvokeCell*>(*cell);
    header->type = t;
    header->size = size;
    header->isRef = isRef ? 1 : 0;
    dn2cpp_array_store_boxed(arg, t, *cell + kInvokeCellData, size, isRef);
    return reinterpret_cast<Dn2CppObject*>(*cell + kInvokeCellData);
}

// The argument list the invoker thunk reads for a row with a DN2CPP_PASS_*
// parameter. Its second half holds each by-ref argument's cell, which keeps the
// cell reachable (the collector takes no heap word pointing into an object as a
// reference to it) and names what dn2cpp_invoke_write_back copies back.
static Dn2CppObject** dn2cpp_invoke_pass_args(Dn2CppMetadataTable<Dn2CppParamInfo> parameters,
    Dn2CppObject** args, int32_t argc, bool wrapExceptions, Dn2CppArrayI4** defaultCopyBack,
    const Dn2CppTypeInfo* declaring)
{
    Dn2CppArrayRef* list = dn2cpp_newarr_ref(argc * 2);
    for (int32_t i = 0; i < argc; i++)
    {
        const auto param = parameters[i].operator->();
        Dn2CppObject* pass = args[i];
        if (dn2cpp_invoke_is_missing(pass))
            pass = dn2cpp_invoke_default(*param.operator->(), pass, i, argc, defaultCopyBack);
        if (param->passKind != 0)
        {
            char* cell = nullptr;
            pass = dn2cpp_invoke_pass_arg(*param.operator->(), pass, wrapExceptions, &cell, declaring);
            if (cell != nullptr)
                dn2cpp_gc_store_ref(&list->data[argc + i], reinterpret_cast<Dn2CppObject*>(cell));
        }
        else if (pass != nullptr)
        {
            pass = dn2cpp_invoke_check_arg(pass, dn2cpp_invoke_parameter_type(*param.operator->(), declaring));
        }
        dn2cpp_gc_store_ref(&list->data[i], pass);
    }
    return list->data;
}

// After the target returns, each by-ref argument's cell goes back into the caller's
// array, a value-type cell as a fresh box, as .NET copies back, and a reference cell
// as its reference. A target that throws writes nothing back.
static void dn2cpp_invoke_write_back(Dn2CppObject* const* pass, Dn2CppObject** args, int32_t argc)
{
    for (int32_t i = 0; i < argc; i++)
    {
        const auto* cell = reinterpret_cast<const char*>(pass[argc + i]);
        if (cell == nullptr)
            continue;
        const auto* header = reinterpret_cast<const Dn2CppInvokeCell*>(cell);
        args[i] = dn2cpp_array_box_element(header->type, cell + kInvokeCellData, header->size,
            header->isRef != 0);
        dn2cpp_gc_write_barrier_if_heap(&args[i]);
    }
}

// A return the thunk cannot box is refused before the target runs: a by-ref-like
// value as .NET refuses it, a referent this image cannot copy, and an unmanaged
// pointer in an image that keeps no System.Reflection.Pointer.
static void dn2cpp_invoke_check_return(int32_t attrs, const Dn2CppTypeInfo* returnPassType)
{
    if ((attrs & DN2CPP_MTHA_RETURN_BYREFLIKE) != 0)
        dn2cpp_throw_invoke_not_supported(nullptr);
    if ((attrs & DN2CPP_MTHA_RETURN_UNBOXABLE) != 0
        || ((attrs & DN2CPP_MTHA_RETURN_POINTER) != 0 && returnPassType != nullptr
            && g_pointer_box_ti == nullptr))
        dn2cpp_throw_invoke_not_supported(
            "MethodBase.Invoke: this image cannot box a pointer or by-ref return of this type.");
}

// The fault .NET raises, inside the region it wraps, for a null by-ref result.
[[noreturn]] static void dn2cpp_throw_invoke_null_result(bool wrapExceptions)
{
    static constexpr char text[] = "The target method returned a null reference.";
    dn2cpp_throw_invoke_wrapped(&dn2cpp_null_reference_exception_type,
        dn2cpp_string_from_utf8(text, static_cast<int32_t>(sizeof text - 1)), 0x80004003u,
        wrapExceptions);
}

// A by-ref return dereferenced and boxed.
static Dn2CppObject* dn2cpp_invoke_deref_result(const Dn2CppTypeInfo* t, Dn2CppObject* result,
    bool wrapExceptions)
{
    if (result == nullptr)
        dn2cpp_throw_invoke_null_result(wrapExceptions);
    return dn2cpp_array_box_element(t, result, dn2cpp_invoke_cell_size(t),
        (t->flags & DN2CPP_TF_VALUETYPE) == 0);
}

// A DN2CPP_MTHA_RETURN_POINTER result boxed, read through the by-ref result first
// when the row returns one.
static Dn2CppObject* dn2cpp_invoke_pointer_result(int32_t attrs, const Dn2CppTypeInfo* pointee,
    Dn2CppObject* result, bool wrapExceptions)
{
    void* value = result;
    if ((attrs & DN2CPP_MTHA_RETURN_BYREF) != 0)
    {
        if (result == nullptr)
            dn2cpp_throw_invoke_null_result(wrapExceptions);
        value = *reinterpret_cast<void* const*>(result);
    }
    const int32_t depth = ((attrs >> DN2CPP_MTHA_RETURN_POINTER_DEPTH_SHIFT)
        & DN2CPP_MTHA_RETURN_POINTER_DEPTH_MASK) + 1;
    return dn2cpp_invoke_box_pointer(value, pointee, depth);
}

// The generic context a static row taking it (DN2CPP_MTHA_CONTEXTARG) runs with: the
// table of the clone of the row's level that `declaring` or the reflected type is.
static Dn2CppObject* dn2cpp_invoke_context_arg(const Dn2CppTypeInfo* declaring,
    const Dn2CppTypeInfo* reflected)
{
    const Dn2CppTypeInfo* clone = dn2cpp_invoke_declaring(declaring, reflected);
    if ((clone->flags & DN2CPP_TF_RUNTIME_SYNTH) == 0 || clone->rgctx == nullptr)
        dn2cpp_throw_invalid_operation();
    return reinterpret_cast<Dn2CppObject*>(const_cast<void**>(clone->rgctx));
}

// Whether .NET shares code over a value type argument: it holds a reference type
// argument, directly or through a nested value type.
static bool dn2cpp_value_holds_reference(const Dn2CppTypeInfo* t)
{
    for (int32_t i = 0; i < t->genericArgCount; i++)
    {
        const Dn2CppTypeInfo* a = t->genericArgs[i];
        if ((a->flags & DN2CPP_TF_VALUETYPE) == 0 || dn2cpp_value_holds_reference(a))
            return true;
    }
    return false;
}

// Whether a runtime template row's lookups (DN2CPP_MTHA_NULLLOOKUP_*) fault a null
// receiver of the type it is read through: a clone argument hits its mask. Read
// through the template itself, which has no arguments and no context, any lookup
// or context read faults.
static bool dn2cpp_null_lookups_hit(int32_t attrs, const Dn2CppTypeInfo* declaring)
{
    const uint32_t bits = static_cast<uint32_t>(attrs);
    const uint32_t refs = (bits >> DN2CPP_MTHA_NULLLOOKUP_REF_SHIFT) & 0xFFu;
    const uint32_t values = (bits >> DN2CPP_MTHA_NULLLOOKUP_VALUE_SHIFT) & 0xFFu;
    if ((declaring->flags & DN2CPP_TF_RUNTIME_TEMPLATE) != 0)
        return (refs | values) != 0
            || (attrs & (DN2CPP_MTHA_NULLCONTEXT | DN2CPP_MTHA_NULLCTX_NULL)) != 0;
    if ((refs | values) == 0)
        return false;
    for (int32_t i = 0; i < declaring->genericArgCount; i++)
    {
        const Dn2CppTypeInfo* arg = declaring->genericArgs[i];
        const uint32_t mask = (arg->flags & DN2CPP_TF_VALUETYPE) == 0 ? refs
            : dn2cpp_value_holds_reference(arg) ? values
            : 0u;
        if ((mask & (1u << (i < 7 ? i : 7))) != 0)
            return true;
    }
    return false;
}

// Whether `objType` is an instance of the row's declaring type. A template row
// reached through no instantiation of it is matched by generic definition.
static bool dn2cpp_invoke_target_matches(const Dn2CppTypeInfo* objType,
    const Dn2CppTypeInfo* declaring)
{
    if (dn2cpp_typeinfo_assignable(objType, declaring) != 0)
        return true;
    if ((declaring->flags & DN2CPP_TF_RUNTIME_TEMPLATE) == 0)
        return false;
    const Dn2CppRuntimeTemplate* level = dn2cpp_runtime_template_by_ti(declaring);
    if (level == nullptr)
        return false;
    for (const Dn2CppTypeInfo* t = objType; t != nullptr; t = t->base)
        if ((t->flags & DN2CPP_TF_RUNTIME_SYNTH) != 0 && t->genericDef == level->def)
            return true;
    return false;
}

// ValidateInvokeTarget: the receiver an instance row runs on. Boxing a
// Nullable<U> yields a boxed U, which .NET accepts for a Nullable<U> method, so
// that receiver re-boxes in the Nullable layout the method's `this` reads.
template<class Method>
static Dn2CppObject* dn2cpp_invoke_receiver(const Method& row, Dn2CppObject* obj,
    const Dn2CppTypeInfo* reflected)
{
    if ((row.attrs & DN2CPP_MTHA_STATIC) != 0)
        return obj;
    const Dn2CppTypeInfo* declaring = dn2cpp_invoke_declaring(row.declaringType, reflected);
    if (obj == nullptr)
        dn2cpp_throw_invoke_target(obj, declaring);
    if (dn2cpp_invoke_target_matches(obj->type, declaring))
        return obj;
    const Dn2CppTypeInfo* u = dn2cpp_nullable_underlying_ti(declaring);
    if (u == nullptr || obj->type != u)
        dn2cpp_throw_invoke_target(obj, declaring);
    return dn2cpp_binder_adapt_arg(obj, declaring);
}

// RtFieldInfo.CheckConsistency: an instance field takes an instance of its
// declaring type, and a static field ignores the receiver.
static void dn2cpp_field_check_target(const Dn2CppFieldRef* f, const Dn2CppFieldInfo* row,
    Dn2CppObject* obj)
{
    if ((row->attrs & DN2CPP_FLDA_STATIC) != 0)
        return;
    if (obj == nullptr)
        dn2cpp_throw_reflection_fault(&dn2cpp_target_exception_type,
            dn2cpp_sr_message(DN2CPP_SR_FIELD_TARGET_REQUIRED, nullptr, 0), 0x80131603u);
    const Dn2CppTypeInfo* declaring = dn2cpp_invoke_declaring(row->declaringType, f->reflectedType);
    if (dn2cpp_invoke_target_matches(obj->type, declaring))
        return;
    const char* nm = row->name != nullptr ? row->name : "";
    Dn2CppString* args[3] = { dn2cpp_string_from_utf8(nm, static_cast<int32_t>(std::strlen(nm))),
        dn2cpp_type_tostring(declaring), dn2cpp_type_tostring(obj->type) };
    dn2cpp_throw_reflection_fault(&dn2cpp_argument_exception_type,
        dn2cpp_sr_message(DN2CPP_SR_FIELD_TARGET_MISMATCH, args, 3), 0x80070057u);
}

// Pointer field conversion shares Invoke's validation without exception wrapping.
Dn2CppObject* dn2cpp_field_pointer_value(Dn2CppObject* value, int32_t passKind,
    const Dn2CppTypeInfo* passType, const Dn2CppBindingSignature* signature)
{
    // FieldInfo accepts UIntPtr for void*, while MethodBase.Invoke does not.
    if ((passKind & ~(DN2CPP_PASS_SIGNATURE_UNKNOWN | DN2CPP_PASS_FNPTR_POINTEE)) == DN2CPP_PASS_POINTER
        && passType == &dn2cpp_void_type
        && value != nullptr && value->type == &dn2cpp_uintptr_type)
        return *reinterpret_cast<Dn2CppObject**>(value + 1);
    Dn2CppParamInfo param{};
    param.passKind = passKind;
    param.passType = passType;
    param.reflectionSignature = signature;
    char* cell = nullptr;
    return dn2cpp_invoke_pass_arg(param, value, false, &cell);
}

// The box a field's setter thunk stores: a value converts as a reflected argument
// does, and null stores the default, which a value-type thunk reads from a zeroed box.
static Dn2CppObject* dn2cpp_field_check_value(const Dn2CppFieldInfo* row, Dn2CppObject* value,
    const Dn2CppTypeInfo* declaring)
{
    if (row->valueCheck != nullptr)
        return row->valueCheck(value);
    const auto* type = row->reflectionSignature != nullptr
        ? dn2cpp_signature_type(row->reflectionSignature, declaring) : row->fieldType;
    if (value == nullptr)
        return dn2cpp_binder_adapt_arg(nullptr, type);
    return dn2cpp_invoke_check_arg(value, type);
}

// An invoker or field-getter thunk boxes a Nullable<T> as its raw struct; .NET
// hands back null or a boxed T.
static Dn2CppObject* dn2cpp_invoke_box_result(const Dn2CppTypeInfo* returnType, Dn2CppObject* result)
{
    return result != nullptr && result->type == returnType
        ? dn2cpp_rebox_from_nullable(result) : result;
}

// A literal row without a getter: its constant's bits boxed at `type`, a primitive
// or enum at the model width, or a reference type's null.
static Dn2CppObject* dn2cpp_field_literal_as(const Dn2CppFieldInfo* row, const Dn2CppTypeInfo* type)
{
    int32_t code = dn2cpp_prim_code(dn2cpp_enum_underlying_or_self(type));
    if ((type->flags & DN2CPP_TF_VALUETYPE) == 0 || code < 0)
        return nullptr;
    if (dn2cpp_prim_storage_width(code) == static_cast<int32_t>(sizeof(int64_t)))
    {
        int64_t v = row->literalValue;
        return dn2cpp_box(type, &v, sizeof(v));
    }
    int32_t v = static_cast<int32_t>(row->literalValue);
    return dn2cpp_box(type, &v, sizeof(v));
}

static Dn2CppObject* dn2cpp_field_literal(const Dn2CppFieldInfo* row)
{
    return dn2cpp_field_literal_as(row, row->fieldType);
}

// MdFieldInfo.SetValue refuses a constant before it looks at the receiver or the value.
[[noreturn]] static void dn2cpp_field_refuse_constant()
{
    dn2cpp_throw_reflection_fault(&dn2cpp_field_access_exception_type,
        dn2cpp_sr_message(DN2CPP_SR_FIELD_CONSTANT, nullptr, 0), 0x80131507u);
}

// A static read-only field refuses SetValue once its type is initialized, and the
// call initializes the type first — which the static getter thunk does here — so
// the refusal is unconditional. A getter that refuses to box the value initializes
// the type before it refuses, and that refusal does not replace this one; a getter
// whose type initializer the image did not retain refuses under no call, and that
// refusal propagates. The message names the declaring TypeDef, whose namespace a
// nested type does not carry.
[[noreturn]] static void dn2cpp_field_refuse_initonly(const Dn2CppFieldRef* f,
    const Dn2CppFieldInfo* row)
{
    if (row->getter != nullptr)
    {
        try
        {
            row->getter(nullptr);
        }
        catch (Dn2CppInvokerMissing& refusal)
        {
            if (refusal.call != reinterpret_cast<const void*>(row->getter))
                throw;
            dn2cpp_exc_inflight_pop(refusal.obj);
        }
    }
    const Dn2CppTypeInfo* owner = dn2cpp_invoke_declaring(row->declaringType, f->reflectedType);
    if (owner->genericArgCount > 0 && owner->genericDef != nullptr)
        owner = owner->genericDef;
    const char* type = owner->name != nullptr ? owner->name : "";
    if (const char* nested = std::strrchr(type, '+'))
        type = nested + 1;
    const char* name = row->name != nullptr ? row->name : "";
    Dn2CppString* args[2] = {
        dn2cpp_string_from_utf8(name, static_cast<int32_t>(std::strlen(name))),
        dn2cpp_string_from_utf8(type, static_cast<int32_t>(std::strlen(type))) };
    dn2cpp_throw_reflection_fault(&dn2cpp_field_access_exception_type,
        dn2cpp_sr_message(DN2CPP_SR_FIELD_INITONLY_STATIC, args, 2), 0x80131507u);
}

// A getter on a literal row boxes at the constant's own type already: it serves a
// string or a constant encoded at another type than the field's.
Dn2CppObject* dn2cpp_fieldref_get_raw_constant_value(Dn2CppFieldRef* f)
{
    const auto row = dn2cpp_fieldref_require(f).operator->();
    if ((row->attrs & DN2CPP_FLDA_LITERAL) == 0)
        dn2cpp_throw_reflection_fault(&dn2cpp_invalid_operation_exception_type, nullptr, 0x80131509u);
    if (row->getter != nullptr)
        return row->getter(nullptr);
    return dn2cpp_field_literal_as(row.operator->(), dn2cpp_enum_underlying_or_self(row->fieldType));
}

// How a reflective call enters a row. Invoke covers MethodInfo.Invoke and the
// PropertyInfo accessors: .NET checks the receiver and the arguments first and
// returns a Nullable<T> as null or a boxed T. A CreateDelegate trampoline enters
// Bound: its typed call already matches the row, and it reads the raw result box.
enum class Dn2CppInvokeMode { Invoke, Bound };

// Calls through the per-shape invoker thunk (which unboxes/casts the args, calls
// fnPtr, and boxes the result), wrapping what the target throws unless the
// caller asked for DoNotWrapExceptions. This call's own refusal, raised by its
// invoker stub or by a trap matching its slot scope `scope`, stays unwrapped.
static Dn2CppObject* dn2cpp_invoke_target(void* thunk, void* fn, Dn2CppObject* self,
    Dn2CppObject** args, const Dn2CppTypeInfo* returnType, bool wrapExceptions,
    const void* scope = nullptr)
{
    auto invoker = reinterpret_cast<Dn2CppObject* (*)(void*, Dn2CppObject*, Dn2CppObject**, const Dn2CppTypeInfo*)>(thunk);
    try
    {
        return invoker(fn, self, args, returnType);
    }
    catch (Dn2CppInvokerMissing& refusal)
    {
        if (!wrapExceptions || refusal.call == thunk || (scope != nullptr && refusal.call == scope))
            throw;
        dn2cpp_throw_target_invocation(refusal.obj);
    }
    catch (Dn2CppException& exception)
    {
        if (!wrapExceptions)
            throw;
        dn2cpp_throw_target_invocation(exception.obj);
    }
}

// The dispatch slot the innermost reflective call is entering. A trap entered as
// that slot is a body the image stripped, which the caller may catch; a trap that a
// compiled body reaches instead stays a reachability abort. A dispatcher's trap
// also matches `receiver`, since a body the dispatcher ran may re-enter it.
struct Dn2CppReflectiveSlot
{
    const void* fn;
    const Dn2CppTypeInfo* receiver;
    Dn2CppMetadataHandle<Dn2CppMethodInfo> method;
    const Dn2CppReflectiveSlot* outer;
};

static thread_local const Dn2CppReflectiveSlot* t_reflective_slot = nullptr;

namespace {
struct Dn2CppReflectiveSlotScope
{
    Dn2CppReflectiveSlot slot;

    Dn2CppReflectiveSlotScope(const void* fn, const Dn2CppTypeInfo* receiver,
        Dn2CppMetadataHandle<Dn2CppMethodInfo> method)
        : slot{ fn, receiver, method, t_reflective_slot }
    {
        t_reflective_slot = &slot;
    }
    ~Dn2CppReflectiveSlotScope() { t_reflective_slot = slot.outer; }
    Dn2CppReflectiveSlotScope(const Dn2CppReflectiveSlotScope&) = delete;
    Dn2CppReflectiveSlotScope& operator=(const Dn2CppReflectiveSlotScope&) = delete;
};
}

static const Dn2CppInterfaceMethodTarget* dn2cpp_delegate_interface_identity(
    const Dn2CppTypeInfo* receiver, const Dn2CppTypeInfo* itf, int32_t slot);
static const Dn2CppTypeInfo* dn2cpp_recorded_receiver(const Dn2CppTypeInfo* receiver, bool gvmCase);

void dn2cpp_reflective_slot_check(const void* slotFn, const Dn2CppTypeInfo* receiver)
{
    const Dn2CppReflectiveSlot* entered = t_reflective_slot;
    if (entered == nullptr || entered->fn != slotFn
        || (receiver != nullptr && entered->receiver != receiver))
        return;
    const Dn2CppMethodInfo row = *entered->method;
    const char* name = row.name != nullptr ? row.name : "?";
    const Dn2CppTypeInfo* owner = entered->receiver != nullptr ? entered->receiver : row.declaringType;
    const bool interfaceRow = (row.declaringType->flags & DN2CPP_TF_INTERFACE) != 0;
    const bool gvm = !interfaceRow && row.vtableSlot < 0 && dn2cpp_is_gvm_row(row);
    const int32_t runtimeInstantiation = DN2CPP_TF_RUNTIME_SYNTH | DN2CPP_TF_RUNTIME_TEMPLATE;
    const char* implName = name;
    bool templateBound = false;
    bool templateCapabilityUnknown = false;
    if (interfaceRow && entered->receiver != nullptr)
    {
        // The dispatch map records the selected MethodImpl even when its body or
        // reflection rows were stripped. A name match cannot identify an interface slot.
        const auto* selected = dn2cpp_delegate_interface_identity(entered->receiver,
            row.declaringType, row.vtableSlot);
        if (selected != nullptr && selected->declaringType != nullptr)
        {
            owner = dn2cpp_invoke_declaring(selected->declaringType, entered->receiver);
            implName = selected->methodName;
            if (implName == nullptr && selected->method)
                implName = selected->method->name;
            if (implName == nullptr && (selected->declaringType->flags & DN2CPP_TF_METADATA_STRIPPED) == 0)
            {
                const auto reflection = selected->declaringType->reflection();
                for (int32_t i = 0; i < reflection.methodCount; i++)
                    if (reflection.methods[i]->metadataToken == selected->metadataToken)
                    {
                        implName = reflection.methods[i]->name;
                        break;
                    }
            }
            templateBound = ((owner->flags | selected->declaringType->flags) & runtimeInstantiation) != 0
                && selected->preservable == 0;
        }
        if (selected == nullptr || selected->declaringType == nullptr || implName == nullptr)
        {
            const std::string subject = dn2cpp_unretained_subject(row.declaringType, name);
            const std::string message = dn2cpp_unretained_text(row.declaringType, subject,
                kDn2CppUnretainedOverride, nullptr,
                "preserve the interface implementation selected for this receiver with [Preserve] or link.xml");
            dn2cpp_throw_invoker_missing(message.c_str(), entered);
        }
    }
    else if (entered->receiver != nullptr && (gvm || row.vtableSlot >= 0))
    {
        bool found = false;
        if (!gvm)
        {
            for (int32_t i = 0; i < dn2cpp_virtual_method_target_count; i++)
            {
                const auto& selected = *dn2cpp_virtual_method_targets[i];
                if (selected.vtable != entered->receiver->vtable || selected.slot != row.vtableSlot
                    || selected.method.declaringType == nullptr || selected.method.methodName == nullptr)
                    continue;
                owner = dn2cpp_invoke_declaring(selected.method.declaringType, entered->receiver);
                implName = selected.method.methodName;
                templateBound = ((owner->flags | selected.method.declaringType->flags) & runtimeInstantiation) != 0
                    && selected.method.preservable == 0;
                found = true;
                break;
            }
        }
        else if (const auto* dispatch = dn2cpp_gvm_row_dispatch_of(row))
        {
            const auto* recorded = dn2cpp_recorded_receiver(entered->receiver, true);
            for (int32_t i = 0; i < dispatch->strippedTargetCount; i++)
            {
                const auto& selected = dispatch->strippedTargets[i];
                if (selected.receiver != recorded || selected.method.declaringType == nullptr
                    || selected.method.methodName == nullptr)
                    continue;
                owner = dn2cpp_invoke_declaring(selected.method.declaringType, entered->receiver);
                implName = selected.method.methodName;
                const bool runtimeTemplate = ((owner->flags | selected.method.declaringType->flags) & runtimeInstantiation) != 0;
                templateBound = runtimeTemplate && selected.method.preservable == 0;
                templateCapabilityUnknown = runtimeTemplate
                    && selected.method.preservable == DN2CPP_METHOD_PRESERVABILITY_UNKNOWN;
                found = true;
                break;
            }
        }
        if (!found)
        {
            const std::string subject = dn2cpp_unretained_subject(row.declaringType, name);
            const std::string message = dn2cpp_unretained_text(row.declaringType, subject,
                kDn2CppUnretainedOverride, nullptr,
                "preserve the virtual implementation selected for this receiver with [Preserve] or link.xml");
            dn2cpp_throw_invoker_missing(message.c_str(), entered);
        }
    }
    const std::string subject = dn2cpp_unretained_subject(owner, implName);
    const std::string message = templateCapabilityUnknown
        ? dn2cpp_unretained_text(owner, subject, kDn2CppUnretainedOverride, nullptr,
            "preserve the selected virtual implementation with [Preserve] or link.xml, or name its closed generic instantiation statically")
        : templateBound
            ? dn2cpp_unretained_text(owner, subject, kDn2CppUnretainedClosedGeneric, nullptr, nullptr)
            : dn2cpp_unretained_text(owner, subject, kDn2CppUnretainedOverride, "method", implName);
    dn2cpp_throw_invoker_missing(message.c_str(), entered);
}

bool dn2cpp_gvm_row_dispatched(const Dn2CppMethodInfo& row)
{
    return dn2cpp_is_gvm_row(row) && (row.ilAttrs & DN2CPP_MA_FINAL) == 0
        && (row.declaringType->flags & DN2CPP_TF_SEALED) == 0;
}

// The table is sorted by token. A decoded row's argument vector is a copy, so the
// arguments compare element-wise.
const Dn2CppGvmRowDispatch* dn2cpp_gvm_row_dispatch_of(const Dn2CppMethodInfo& row)
{
    if (!dn2cpp_gvm_row_dispatched(row))
        return nullptr;
    int32_t lo = 0;
    int32_t hi = dn2cpp_gvm_row_dispatch_count;
    while (lo < hi)
    {
        const int32_t mid = lo + (hi - lo) / 2;
        if (dn2cpp_gvm_row_dispatch[mid].identity.metadataToken < row.metadataToken)
            lo = mid + 1;
        else
            hi = mid;
    }
    for (int32_t i = lo; i < dn2cpp_gvm_row_dispatch_count
         && dn2cpp_gvm_row_dispatch[i].identity.metadataToken == row.metadataToken; i++)
    {
        const Dn2CppDelegateMethodIdentity& identity = dn2cpp_gvm_row_dispatch[i].identity;
        if (identity.declaringType != row.declaringType || identity.genericArgCount != row.genericParamCount)
            continue;
        int32_t arg = 0;
        while (arg < row.genericParamCount && identity.genericArgs[arg] == row.genericArgs[arg])
            arg++;
        if (arg == row.genericParamCount)
            return &dn2cpp_gvm_row_dispatch[i];
    }
    return nullptr;
}

// What a call through a method row reads of it, decoded once per row.
struct Dn2CppInvokePlan
{
    // The most parameter types a plan holds inline.
    static constexpr int32_t inlineParamTypes = 5;
    const Dn2CppTypeInfo* declaringType;
    const Dn2CppTypeInfo* returnType;
    const Dn2CppTypeInfo* returnPassType;
    Dn2CppMetadataTable<Dn2CppParamInfo> parameters;
    void* fnPtr;
    void* invoker;
    // The dispatcher a callvirt of a closed generic virtual row calls, or null.
    void* gvmDispatcher;
    // A list longer than the inline array, kept with a published plan
    // (dn2cpp_invoke_plan_publish).
    const Dn2CppTypeInfo* const* paramTypeList;
    int32_t paramCount;
    int32_t attrs;
    // The ECMA MethodAttributes word, which is 16 bits wide.
    uint16_t ilAttrs;
    // The k* bits below.
    uint16_t flags;
    int32_t vtableSlot;
    const Dn2CppTypeInfo* paramTypes[inlineParamTypes];

    // The plan carries the types Invoke checks arguments against.
    static constexpr uint16_t kParamTypes = 1;
    // A parameter carries DN2CPP_PASS_* bits, which Invoke reads from its table.
    static constexpr uint16_t kPassArgs = 2;
    static constexpr uint16_t kSignatureArrays = 4;
    // Invoke would hand its caller a box no allocation wired
    // (dn2cpp_invoke_unallocated_box); set only on a plan that carries parameter types.
    static constexpr uint16_t kUnallocatedBox = 8;

    // The parameter types, or null when the row's parameter table answers.
    const Dn2CppTypeInfo* const* checkedTypes() const
    {
        if ((flags & kParamTypes) == 0 || (flags & kSignatureArrays) != 0)
            return nullptr;
        return paramTypeList != nullptr ? paramTypeList : paramTypes;
    }
};

// A value type no box of which the image allocates (DN2CPP_TA_UNALLOCATED) has no
// slots or interface map wired, so Invoke hands no such box to its caller: a result,
// read through a by-ref one, or a by-ref argument written back, a Nullable<T> as its
// T. A delegate binding's call unboxes these values itself and never refuses them.
static const Dn2CppTypeInfo* dn2cpp_unallocated_box(const Dn2CppTypeInfo* t)
{
    if (t == nullptr || (t->flags & DN2CPP_TF_VALUETYPE) == 0)
        return nullptr;
    if (const Dn2CppTypeInfo* underlying = dn2cpp_nullable_underlying_ti(t))
        t = underlying;
    return dn2cpp_unallocated(t) ? t : nullptr;
}

// The first such box Invoke of `plan` would make, its result before its parameters
// in order, or null.
static const Dn2CppTypeInfo* dn2cpp_invoke_unallocated_box(const Dn2CppInvokePlan& plan)
{
    if ((plan.attrs & DN2CPP_MTHA_RETURN_POINTER) == 0)
        if (const Dn2CppTypeInfo* t = dn2cpp_unallocated_box(
                (plan.attrs & DN2CPP_MTHA_RETURN_BYREF) != 0 ? plan.returnPassType : plan.returnType))
            return t;
    for (int32_t i = 0; i < plan.paramCount; i++)
    {
        const auto param = plan.parameters[i].operator->();
        if ((param->passKind & (DN2CPP_PASS_BYREF | DN2CPP_PASS_POINTER)) == DN2CPP_PASS_BYREF)
            if (const Dn2CppTypeInfo* t = dn2cpp_unallocated_box(param->passType))
                return t;
    }
    return nullptr;
}

// Invoke's refusal of a kUnallocatedBox plan, raised where the target would run.
// `declaring` places the member's subject.
[[noreturn]] static void dn2cpp_throw_invoke_unallocated_box(Dn2CppMetadataHandle<Dn2CppMethodInfo> mi,
    const Dn2CppInvokePlan& plan, const Dn2CppTypeInfo* declaring)
{
    const Dn2CppMethodInfo row = *mi;
    const std::string member = row.display != nullptr ? dn2cpp_utf8_of(dn2cpp_metadata_string(row.display))
        : std::string(row.name != nullptr ? row.name : "?");
    dn2cpp_throw_unallocated(dn2cpp_invoke_unallocated_box(plan), dn2cpp_unretained_subject(declaring, member),
        "invoking it", declaring);
}

// A plan that serves Invoke carries the parameter types; a delegate call checks
// no argument. A list longer than the inline array is carried only when the
// caller supplies a copy that outlives the plan.
static Dn2CppInvokePlan dn2cpp_invoke_plan(const Dn2CppMethodInfo& row, bool withParamTypes,
    const Dn2CppTypeInfo* const* longList = nullptr)
{
    const Dn2CppGvmRowDispatch* dispatch = dn2cpp_gvm_row_dispatch_of(row);
    Dn2CppInvokePlan plan{ row.declaringType, row.returnType, row.returnPassType,
        row.parameters, row.fnPtr, row.invoker,
        dispatch != nullptr ? dispatch->dispatcher : nullptr, nullptr,
        row.paramCount, row.attrs, static_cast<uint16_t>(row.ilAttrs), 0, row.vtableSlot, {} };
    if (!withParamTypes)
        return plan;
    const bool inlineTypes = row.paramCount <= Dn2CppInvokePlan::inlineParamTypes;
    for (int32_t i = 0; i < row.paramCount; i++)
    {
        const auto param = row.parameters[i].operator->();
        if (inlineTypes)
            plan.paramTypes[i] = param->paramType;
        if (param->passKind != 0)
            plan.flags |= Dn2CppInvokePlan::kPassArgs;
        if (dn2cpp_parameter_array_signature(*param.operator->()) != nullptr)
            plan.flags |= Dn2CppInvokePlan::kSignatureArrays;
    }
    if (inlineTypes)
        plan.flags |= Dn2CppInvokePlan::kParamTypes;
    else if (longList != nullptr)
    {
        plan.paramTypeList = longList;
        plan.flags |= Dn2CppInvokePlan::kParamTypes;
    }
    if (dn2cpp_invoke_unallocated_box(plan) != nullptr)
        plan.flags |= Dn2CppInvokePlan::kUnallocatedBox;
    return plan;
}

// The plan of a row that lives as long as the process, published once and shared
// by every thread: a native row, a record of the generated image's method or
// constructor tables, or a delta row. A published node never changes and is never
// freed, so a call reads its plan in place and a nested Invoke cannot disturb it.
struct Dn2CppPublishedPlan
{
    const void* identity;
    const Dn2CppPublishedPlan* next;
    Dn2CppInvokePlan plan;
};

// Prepend-only lists: a node is complete before the release that links it, and a
// reader acquires the head it walks from.
constexpr int32_t kInvokePlanBucketBits = 12;
static std::atomic<const Dn2CppPublishedPlan*> g_invoke_plans[std::size_t{ 1 } << kInvokePlanBucketBits]{};

static std::atomic<const Dn2CppPublishedPlan*>& dn2cpp_invoke_plan_bucket(const void* identity)
{
    const uint64_t key = static_cast<uint64_t>(reinterpret_cast<uintptr_t>(identity));
    return g_invoke_plans[(key * 0x9E3779B97F4A7C15ull) >> (64 - kInvokePlanBucketBits)];
}

// Publishes the plan of `row`, unless another thread published one for `identity`
// first, so each row holds at most one node. The plan carries the parameter types
// whatever their count.
DN2CPP_NOINLINE static const Dn2CppInvokePlan& dn2cpp_invoke_plan_publish(
    std::atomic<const Dn2CppPublishedPlan*>& bucket, const void* identity,
    const Dn2CppMethodInfo& row)
{
    const Dn2CppTypeInfo** list = nullptr;
    if (row.paramCount > Dn2CppInvokePlan::inlineParamTypes)
    {
        list = new const Dn2CppTypeInfo*[static_cast<size_t>(row.paramCount)];
        for (int32_t i = 0; i < row.paramCount; i++)
            list[i] = row.parameters[i]->paramType;
    }
    auto* node = new Dn2CppPublishedPlan{ identity, nullptr, dn2cpp_invoke_plan(row, true, list) };
    const Dn2CppPublishedPlan* head = bucket.load(std::memory_order_acquire);
    do
    {
        for (const Dn2CppPublishedPlan* n = head; n != nullptr; n = n->next)
        {
            if (n->identity != identity)
                continue;
            delete[] list;
            delete node;
            return n->plan;
        }
        node->next = head;
    } while (!bucket.compare_exchange_weak(head, node, std::memory_order_release,
                 std::memory_order_acquire));
    return node->plan;
}

// A call through a row no plan was published for. A delta row (a clone's, or a
// clone level's) is allocated once and never freed, and only a generated method- or
// constructor-table extent proves a packed record's lifetime: locally encoded
// records are planned into `local` per call.
DN2CPP_NOINLINE static const Dn2CppInvokePlan& dn2cpp_invoke_plan_miss(
    std::atomic<const Dn2CppPublishedPlan*>& bucket, Dn2CppMetadataHandle<Dn2CppMethodInfo> mi,
    bool withParamTypes, Dn2CppInvokePlan& local)
{
    if (const Dn2CppMethodInfo* row = mi.native())
        return dn2cpp_invoke_plan_publish(bucket, mi.identity(), *row);
    Dn2CppMethodInfo row;
    dn2cpp_metadata_decode(&row, Dn2CppMetadataKind::Method, mi.identity());
    // Neither native nor a tagged record: a delta row.
    const bool delta = (reinterpret_cast<uintptr_t>(mi.identity()) & 1) == 0;
    if (delta || dn2cpp_metadata_is_image_method(mi.identity()))
        return dn2cpp_invoke_plan_publish(bucket, mi.identity(), row);
    local = dn2cpp_invoke_plan(row, withParamTypes);
    return local;
}

// A metadata-answerable row carries no body at all: it answers from its own type
// arguments and receiver. A non-generic row is always closed; a generic one is
// closed only once MakeGenericMethod has filled genericArgs. The OPEN definition
// view rejects Invoke before planning. Such rows are synthesized native, never
// emitted, so only a native row is asked. Null when `row` answers nothing.
static const Dn2CppMetaMember* dn2cpp_invoke_answerer(Dn2CppMetadataHandle<Dn2CppMethodInfo> mi,
    const Dn2CppMethodInfo& row)
{
    if ((row.attrs & DN2CPP_MTHA_METAANSWER) == 0
        || (row.genericParamCount != 0 && row.genericArgs == nullptr))
        return nullptr;
    return dn2cpp_meta_desc_of(mi);
}

static Dn2CppObject* dn2cpp_invoke_answer(Dn2CppMetadataHandle<Dn2CppMethodInfo> mi,
    const Dn2CppMethodInfo& row, const Dn2CppMetaMember* d, Dn2CppObject* obj, Dn2CppObject** args,
    int32_t argc, bool wrapExceptions, Dn2CppInvokeMode mode, const Dn2CppTypeInfo* reflected)
{
    Dn2CppObject** callerArgs = args;
    Dn2CppArrayI4* defaultCopyBack = nullptr;
    if (mode == Dn2CppInvokeMode::Invoke)
    {
        obj = dn2cpp_invoke_receiver(row, obj, reflected);
        if (argc != row.paramCount)
            dn2cpp_throw_invoke_parameter_count();
        args = dn2cpp_invoke_check_args(mi, nullptr, args, argc, &defaultCopyBack,
            dn2cpp_invoke_declaring(row.declaringType, reflected));
    }
    // An answer refuses nothing itself: a refusal reaching here is a nested call's.
    try
    {
        Dn2CppObject* result = d->answer(row.genericArgs, obj, args);
        dn2cpp_invoke_write_defaults(row.parameters, args, callerArgs, defaultCopyBack, argc);
        return result;
    }
    catch (Dn2CppException& exception)
    {
        if (!wrapExceptions)
            throw;
        dn2cpp_throw_target_invocation(exception.obj);
    }
}

static Dn2CppObject* dn2cpp_invoke_row(Dn2CppMetadataHandle<Dn2CppMethodInfo> mi,
    const Dn2CppInvokePlan& row, Dn2CppObject* obj, Dn2CppObject** args, int32_t argc, bool wrapExceptions,
    Dn2CppInvokeMode mode, const Dn2CppTypeInfo* reflected)
{
    bool invoke = mode == Dn2CppInvokeMode::Invoke;
    bool isStatic = (row.attrs & DN2CPP_MTHA_STATIC) != 0;
    // A CreateDelegate trampoline passes by-ref and pointer arguments raw and reads
    // a by-ref result raw; only Invoke builds and dereferences them.
    Dn2CppObject** callerArgs = args;
    Dn2CppArrayI4* defaultCopyBack = nullptr;
    const bool passArgs = invoke && (row.flags & Dn2CppInvokePlan::kPassArgs) != 0;
    const Dn2CppTypeInfo* referent = nullptr;
    if (invoke)
    {
        dn2cpp_invoke_check_return(row.attrs, row.returnPassType);
        if ((row.attrs & (DN2CPP_MTHA_RETURN_BYREF | DN2CPP_MTHA_RETURN_POINTER))
            == DN2CPP_MTHA_RETURN_BYREF)
        {
            referent = row.returnPassType;
            if (referent == nullptr)
                dn2cpp_throw_invalid_operation();
        }
        obj = dn2cpp_invoke_receiver(row, obj, reflected);
        if (argc != row.paramCount)
            dn2cpp_throw_invoke_parameter_count();
        args = passArgs ? dn2cpp_invoke_pass_args(row.parameters, args, argc, wrapExceptions, &defaultCopyBack,
                            dn2cpp_invoke_declaring(row.declaringType, reflected))
                        : dn2cpp_invoke_check_args(mi, row.checkedTypes(), args, argc, &defaultCopyBack,
                            dn2cpp_invoke_declaring(row.declaringType, reflected));
        if (isStatic && (row.ilAttrs & DN2CPP_MA_ABSTRACT) != 0
            && (row.declaringType->flags & DN2CPP_TF_INTERFACE) != 0)
            dn2cpp_throw_invoke_static_abstract(wrapExceptions);
    }
    if (row.invoker == nullptr)
        dn2cpp_throw_invoke_unavailable(mi, reflected);
    // A virtual row runs the body a callvirt binds for the receiver: its class
    // vtable slot or its interface map slot. The row's thunk spells the declared
    // signature every body in the slot shares, and passes the receiver unadjusted,
    // since the adjustment below keys on the declaring type. A value type's row is
    // sealed, and a receiver without a vtable (a boxed value, a runtime-owned
    // handle) runs the row's own body: System.Object's and System.ValueType's rows
    // are metadata-answered and an enum declares no methods. A static or
    // non-virtual interface member runs its own body: .NET never dispatches it.
    // A closed generic virtual row has no slot; it runs through the dispatcher a
    // callvirt of the same instantiation calls, which takes the row's signature and
    // selects on the receiver's type, boxes included. Reachability cannot fill every
    // framework row's slots, so the receiver's slot or the dispatcher may hold a
    // trap for a body the image stripped, which reports itself through the entered
    // slot (dn2cpp_reflective_slot_check); an interface map miss stays its own loud
    // abort.
    void* fn = row.fnPtr;
    const Dn2CppTypeInfo* declaring = row.declaringType;
    const bool objectVirtualInvoker = row.invoker == reinterpret_cast<void*>(&dn2cpp_delegate_object_virtual_invoker)
        || row.invoker == reinterpret_cast<void*>(&dn2cpp_delegate_interface_invoker);
    if (obj != nullptr && row.vtableSlot >= 0 && !objectVirtualInvoker && (declaring->flags & DN2CPP_TF_PATCH) == 0)
    {
        if ((declaring->flags & DN2CPP_TF_INTERFACE) != 0)
        {
            if (!isStatic && (row.ilAttrs & DN2CPP_MA_VIRTUAL) != 0)
                fn = const_cast<void*>(dn2cpp_resolve_interface(obj->type, declaring)[row.vtableSlot]);
        }
        else if ((declaring->flags & DN2CPP_TF_VALUETYPE) == 0 && obj->type->vtable != nullptr)
            fn = const_cast<void*>(obj->type->vtable[row.vtableSlot]);
    }
    else if (obj != nullptr && row.gvmDispatcher != nullptr)
        fn = row.gvmDispatcher;
    if (fn == nullptr)
        dn2cpp_throw_invoke_unavailable(mi, reflected);
    Dn2CppObject* self = obj;
    if (!isStatic && obj != nullptr && (declaring->flags & DN2CPP_TF_VALUETYPE) != 0 && !objectVirtualInvoker)
        self = reinterpret_cast<Dn2CppObject*>(reinterpret_cast<char*>(obj) + sizeof(Dn2CppObject));
    if ((row.attrs & DN2CPP_MTHA_CONTEXTARG) != 0)
        self = dn2cpp_invoke_context_arg(declaring, reflected);
    if (invoke && (row.flags & Dn2CppInvokePlan::kUnallocatedBox) != 0)
        dn2cpp_throw_invoke_unallocated_box(mi, row, dn2cpp_invoke_declaring(declaring, reflected));
    Dn2CppObject* result;
    {
        Dn2CppReflectiveSlotScope entered(fn, obj != nullptr ? obj->type : nullptr, mi);
        result = dn2cpp_invoke_target(row.invoker, fn, self, args, row.returnType, wrapExceptions,
            &entered.slot);
    }
    if (!invoke)
        return result;
    result = (row.attrs & DN2CPP_MTHA_RETURN_POINTER) != 0
        ? dn2cpp_invoke_pointer_result(row.attrs, row.returnPassType, result, wrapExceptions)
        : referent != nullptr ? dn2cpp_invoke_deref_result(referent, result, wrapExceptions)
        : dn2cpp_invoke_box_result(row.returnType, result);
    if (passArgs)
        dn2cpp_invoke_write_back(args, callerArgs, argc);
    dn2cpp_invoke_write_defaults(row.parameters, args, callerArgs, defaultCopyBack, argc);
    return result;
}

// The plan a call through `mi` runs: its published plan, read in place, else
// `local`, which a plan for a delegate call fills without parameter types.
static const Dn2CppInvokePlan& dn2cpp_invoke_plan_of(Dn2CppMetadataHandle<Dn2CppMethodInfo> mi,
    bool withParamTypes, Dn2CppInvokePlan& local)
{
    std::atomic<const Dn2CppPublishedPlan*>& bucket = dn2cpp_invoke_plan_bucket(mi.identity());
    for (const Dn2CppPublishedPlan* n = bucket.load(std::memory_order_acquire); n != nullptr;
         n = n->next)
        if (n->identity == mi.identity())
            return n->plan;
    return dn2cpp_invoke_plan_miss(bucket, mi, withParamTypes, local);
}

// `reflected` is the type the member was obtained through, which places a template
// row on the instantiation it runs on.
static Dn2CppObject* dn2cpp_invoke_mi(Dn2CppMetadataHandle<Dn2CppMethodInfo> mi,
    Dn2CppObject* obj, Dn2CppObject** args, int32_t argc, bool wrapExceptions,
    const Dn2CppTypeInfo* reflected, Dn2CppInvokeMode mode = Dn2CppInvokeMode::Invoke)
{
    if (mi == nullptr)
        dn2cpp_throw_invalid_operation();
    const auto* declaring = dn2cpp_invoke_declaring(mi->declaringType, reflected);
    if (dn2cpp_type_contains_generic_parameters(declaring) || dn2cpp_type_metadata_only(declaring))
        dn2cpp_throw_invalid_operation();
    Dn2CppInvokePlan local;
    const Dn2CppInvokePlan& plan = dn2cpp_invoke_plan_of(mi, mode == Dn2CppInvokeMode::Invoke, local);
    // The plan carries the row's attrs, so only a metadata-answered row is read again.
    if ((plan.attrs & DN2CPP_MTHA_METAANSWER) != 0)
    {
        const Dn2CppMethodInfo* row = mi.native();
        if (const Dn2CppMetaMember* d = row != nullptr ? dn2cpp_invoke_answerer(mi, *row) : nullptr)
            return dn2cpp_invoke_answer(mi, *row, d, obj, args, argc, wrapExceptions, mode, reflected);
    }
    return dn2cpp_invoke_row(mi, plan, obj, args, argc, wrapExceptions, mode, reflected);
}

// MethodInfo.Invoke refuses an open definition view before receiver or argument
// validation, regardless of whether the image contains a compiled instantiation.
Dn2CppObject* dn2cpp_methodref_invoke(Dn2CppMethodRef* m, Dn2CppObject* obj, Dn2CppArrayRef* args, bool wrapExceptions)
{
    Dn2CppMetadataHandle<Dn2CppMethodInfo> mi = dn2cpp_methodref_require(m);
    if (dn2cpp_methodref_contains_generic_parameters(m) != 0
        || dn2cpp_type_metadata_only(mi->declaringType))
        dn2cpp_throw_reflection_fault(&dn2cpp_invalid_operation_exception_type,
            dn2cpp_sr_message(DN2CPP_SR_UNBOUND_GENERIC, nullptr, 0), 0x80131509u);
    return dn2cpp_invoke_mi(mi, obj, (args == nullptr) ? nullptr : args->data,
                            (args == nullptr) ? 0 : args->length, wrapExceptions, m->reflectedType);
}

// ---- reflection: CreateDelegate (the reflection -> delegate bridge) ----

// The bind node's header tag (identity only — never surfaced as a managed Type).
const Dn2CppTypeInfo dn2cpp_reflbind_type = { "<ReflectionDelegateBind>", nullptr, nullptr, nullptr, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (int32_t)sizeof(Dn2CppReflBind), 0, 0, 0, 0, 0, nullptr };

static thread_local const Dn2CppTypeInfo* t_null_receiver_context_type = nullptr;

const void* const* dn2cpp_null_receiver_rgctx(const Dn2CppTypeInfo* genericDef, const Dn2CppTypeInfo* source)
{
    const Dn2CppTypeInfo* type = t_null_receiver_context_type;
    t_null_receiver_context_type = nullptr;
    if (type == nullptr)
        dn2cpp_throw_null_reference();
    const void* const* context = source != nullptr
        ? dn2cpp_rgctx_template(type, source) : dn2cpp_rgctx(type, genericDef);
    if (context == nullptr)
        dn2cpp_throw_null_reference();
    return context;
}

struct Dn2CppNullReceiverContextScope
{
    const Dn2CppTypeInfo* previous;

    explicit Dn2CppNullReceiverContextScope(const Dn2CppTypeInfo* type)
        : previous(t_null_receiver_context_type)
    {
        t_null_receiver_context_type = type;
    }

    ~Dn2CppNullReceiverContextScope()
    {
        t_null_receiver_context_type = previous;
    }
};

// The boxed-invoker dispatch behind a dgrefl_* trampoline: same value-type
// receiver adjustment + invoker-thunk call as MethodInfo.Invoke, without its
// checks, the argument count included, since the bind proved the shapes and the
// trampoline boxes each argument exactly. The bind decided what the call faults
// with (Dn2CppReflBind::fault), so a call decodes no row. The bind's type places a
// template row's call on its clone, as the reflected type places Invoke's.
Dn2CppObject* dn2cpp_reflbind_invoke(Dn2CppReflBind* ctx, Dn2CppObject* self, Dn2CppObject** argv)
{
    if (ctx->fault == DN2CPP_DGBIND_FAULT_STATIC_VIRTUAL)
        dn2cpp_throw_static_virtual_entry_point();
    if (self == nullptr && ctx->fault == DN2CPP_DGBIND_FAULT_NULL_RECEIVER)
        dn2cpp_throw_null_reference();
    if (self == nullptr && ctx->fault == DN2CPP_DGBIND_FAULT_NULL_BODILESS)
        dn2cpp_throw_null_bound_bodiless();
    if (self == nullptr && ctx->fault == DN2CPP_DGBIND_FAULT_NULL_CONTEXT)
    {
        // The body reads its generic context off the receiver's type and loads the
        // receiver only to pass it on to such bodies, and the declaring type's own
        // slot holds the row.
        Dn2CppObject standIn{ ctx->declaring };
        return dn2cpp_invoke_mi(ctx->method, &standIn, argv, 0, false, ctx->declaring,
                                Dn2CppInvokeMode::Bound);
    }
    if (self == nullptr && ctx->fault == DN2CPP_DGBIND_FAULT_NULL_CTX_NULL)
    {
        Dn2CppNullReceiverContextScope context(ctx->declaring);
        return dn2cpp_invoke_mi(ctx->method, nullptr, argv, 0, false, ctx->declaring,
                                Dn2CppInvokeMode::Bound);
    }
    return dn2cpp_invoke_mi(ctx->method, self, argv, 0, false, ctx->declaring,
                            Dn2CppInvokeMode::Bound);
}

static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_delegate_invoke_row(const Dn2CppTypeInfo* ti)
{
    const auto reflection = ti->reflection();
    for (int32_t i = 0; i < reflection.methodCount; i++)
    {
        const auto mi = reflection.methods[i];
        if (std::strcmp(mi->name, "Invoke") == 0)
            return mi;
    }
    return nullptr;
}

// The row serves every call on the thread's last delegate type, so a repeated
// DynamicInvoke decodes no row to find it.
Dn2CppObject* dn2cpp_delegate_dynamic_invoke(Dn2CppObject* d, Dn2CppArrayRef* args)
{
    if (d == nullptr)
        dn2cpp_throw_null_reference();
    static thread_local const Dn2CppTypeInfo* lastType = nullptr;
    static thread_local Dn2CppMetadataHandle<Dn2CppMethodInfo> lastRow;
    if (lastType != d->type)
    {
        lastRow = dn2cpp_delegate_invoke_row(d->type);
        lastType = d->type;
    }
    return dn2cpp_invoke_mi(lastRow, d, args != nullptr ? args->data : nullptr,
        args != nullptr ? args->length : 0, true, d->type);
}

// Reference variance never converts a boxed value or a bound receiver.
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

// CLR by-value enum compatibility retains the underlying verifier type,
// including signedness. By-ref and function-pointer identities stay invariant.
static bool dn2cpp_dgbind_value_equivalent(const Dn2CppTypeInfo* from, const Dn2CppTypeInfo* to)
{
    if (from == to)
        return true;
    if (from == nullptr || to == nullptr || ((from->flags | to->flags) & DN2CPP_TF_ENUM) == 0)
        return false;
    return dn2cpp_enum_underlying_or_self(from) == dn2cpp_enum_underlying_or_self(to);
}

static bool dn2cpp_dgbind_delegate_type(const Dn2CppTypeInfo* type)
{
    // MulticastDelegate carries the delegate layout flag but is not a callable type.
    return (type->flags & DN2CPP_TF_DELEGATE) != 0
        && !(type->base != nullptr && std::strcmp(type->base->name, "System.Delegate") == 0);
}

static const Dn2CppTypeInfo* dn2cpp_dgbind_pointer_element(const Dn2CppTypeInfo* type)
{
    type = dn2cpp_enum_underlying_or_self(type);
    if (type == &dn2cpp_byte_type) return &dn2cpp_sbyte_type;
    if (type == &dn2cpp_uint16_type) return &dn2cpp_int16_type;
    if (type == &dn2cpp_uint32_type) return &dn2cpp_int32_type;
    if (type == &dn2cpp_uint64_type) return &dn2cpp_int64_type;
    if (type == &dn2cpp_uintptr_type) return &dn2cpp_intptr_type;
    return type;
}

// ABI shapes precede reference variance. By-ref and function-pointer locations
// are invariant; one-level unmanaged pointers admit primitive and reference casts.
static bool dn2cpp_dgbind_pass(const Dn2CppTypeInfo* from, int32_t fromKind,
    const Dn2CppTypeInfo* fromPass, const Dn2CppTypeInfo* to, int32_t toKind,
    const Dn2CppTypeInfo* toPass, bool relaxed, bool& unknown)
{
    constexpr int32_t shape = DN2CPP_PASS_BYREF | DN2CPP_PASS_POINTER | DN2CPP_PASS_FNPTR
        | DN2CPP_PASS_FNPTR_POINTEE | DN2CPP_PASS_BYREFLIKE | (0xff << DN2CPP_PASS_POINTER_DEPTH_SHIFT);
    if ((fromKind & shape) != (toKind & shape))
        return false;
    if (((fromKind | toKind) & DN2CPP_PASS_SIGNATURE_UNKNOWN) != 0
        || ((fromKind & DN2CPP_PASS_FNPTR) != 0
            && (fromPass == nullptr || toPass == nullptr
                || fromPass == &dn2cpp_void_type || toPass == &dn2cpp_void_type)))
    {
        // A known mismatch elsewhere can reject without resolving this signature.
        unknown = true;
        return true;
    }
    if ((fromKind & (DN2CPP_PASS_BYREF | DN2CPP_PASS_POINTER)) != 0)
    {
        if (fromPass == toPass)
            return true;
        if (relaxed && (fromKind & (DN2CPP_PASS_BYREF | DN2CPP_PASS_FNPTR
            | DN2CPP_PASS_FNPTR_POINTEE | (0xff << DN2CPP_PASS_POINTER_DEPTH_SHIFT))) == 0
            && fromPass != nullptr && toPass != nullptr)
        {
            return dn2cpp_dgbind_pointer_element(fromPass) == dn2cpp_dgbind_pointer_element(toPass)
                || dn2cpp_dgbind_widens(fromPass, toPass);
        }
        return false;
    }
    return dn2cpp_dgbind_value_equivalent(from, to)
        || (relaxed && dn2cpp_dgbind_widens(from, to));
}

// Binding identities share the public compound/array/generic interners. Names
// without a retained TI remain negative evidence only; never resolve them by name.
static const Dn2CppTypeInfo* dn2cpp_dgbind_signature_type(const Dn2CppBindingSignature* signature,
    const Dn2CppTypeInfo* declaring)
{
    if (signature == nullptr)
        return nullptr;
    const auto& sig = *signature;
    if (sig.kind == 0)
        return sig.type != nullptr && (sig.type->flags
            & (DN2CPP_TF_SHARED_CANON | DN2CPP_TF_GENERICPARAM | DN2CPP_TF_GENERICDEF)) == 0 ? sig.type : nullptr;
    if (sig.kind == 5)
    {
        if (declaring == nullptr || declaring->genericArgs == nullptr
            || sig.value < 0 || sig.value >= declaring->genericArgCount)
            return nullptr;
        Dn2CppBindingSignature actual{0, declaring->genericArgs[sig.value], 0, nullptr, 0, nullptr};
        return dn2cpp_dgbind_signature_type(&actual, nullptr);
    }
    if (sig.kind == 8 || sig.kind == 9 || (sig.kind == 4 && sig.type == nullptr))
        return nullptr;
    std::vector<const Dn2CppTypeInfo*> children;
    for (int32_t i = 0; i < sig.childCount; i++)
    {
        const auto* child = dn2cpp_dgbind_signature_type(&sig.children[i], declaring);
        if (child == nullptr)
            return nullptr;
        children.push_back(child);
    }
    if (sig.kind >= 1 && sig.kind <= 3)
        return dn2cpp_compound_type(sig.kind, sig.value, children.data(), sig.childCount);
    if ((sig.kind == 6 || sig.kind == 7) && sig.childCount == 1)
        return dn2cpp_array_ti(children[0], sig.kind == 6 ? 1 : sig.value, sig.kind == 7);
    if (sig.kind == 4)
    {
        std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
        return dn2cpp_constraint_instantiation(sig.type, children);
    }
    return nullptr;
}

static const Dn2CppTypeInfo* dn2cpp_dgbind_signature_pointee(
    const Dn2CppBindingSignature* signature, const Dn2CppTypeInfo* declaring,
    int32_t& kind, const Dn2CppTypeInfo* fallback)
{
    const auto* type = dn2cpp_dgbind_signature_type(signature, declaring);
    if (type == nullptr)
        return fallback;
    kind &= ~DN2CPP_PASS_SIGNATURE_UNKNOWN;
    if (dn2cpp_signature_kind(type) == 2)
        type = type->elementType;
    // Descriptor depth bits name the outer levels; excess levels remain in the
    // pointee identity, just as they do in the emitted ABI descriptors.
    int32_t levels = (kind >> DN2CPP_PASS_POINTER_DEPTH_SHIFT) & 0xff;
    if ((kind & DN2CPP_PASS_POINTER) != 0 && (kind & DN2CPP_PASS_FNPTR) == 0)
        for (int32_t i = 0; i <= levels && dn2cpp_signature_kind(type) == 1; i++)
            type = type->elementType;
    return type;
}

static bool dn2cpp_dgbind_parameter(const Dn2CppParamInfo& from,
    const Dn2CppParamInfo& to, bool relaxed, bool& unknown,
    const Dn2CppTypeInfo* fromDeclaring = nullptr, const Dn2CppTypeInfo* toDeclaring = nullptr)
{
    int32_t fromKind = from.passKind, toKind = to.passKind;
    const auto* fromPass = dn2cpp_dgbind_signature_pointee(from.reflectionSignature, fromDeclaring,
        fromKind, from.bindingPointeeType != nullptr ? from.bindingPointeeType : from.passType);
    const auto* toPass = dn2cpp_dgbind_signature_pointee(to.reflectionSignature, toDeclaring,
        toKind, to.bindingPointeeType != nullptr ? to.bindingPointeeType : to.passType);
    return dn2cpp_dgbind_pass(from.paramType, fromKind, fromPass,
        to.paramType, toKind, toPass, relaxed, unknown);
}

static bool dn2cpp_dgbind_return(const Dn2CppMethodInfo& from,
    const Dn2CppMethodInfo& to, bool relaxed, bool& unknown,
    const Dn2CppTypeInfo* fromDeclaring = nullptr, const Dn2CppTypeInfo* toDeclaring = nullptr)
{
    auto kind = [](const Dn2CppMethodInfo& row) {
        int32_t value = (row.attrs & DN2CPP_MTHA_RETURN_BYREF) != 0 ? DN2CPP_PASS_BYREF : 0;
        if ((row.attrs & DN2CPP_MTHA_RETURN_POINTER) != 0)
        {
            value |= DN2CPP_PASS_POINTER;
            value |= ((row.attrs >> DN2CPP_MTHA_RETURN_POINTER_DEPTH_SHIFT)
                & DN2CPP_MTHA_RETURN_POINTER_DEPTH_MASK) << DN2CPP_PASS_POINTER_DEPTH_SHIFT;
            if (row.returnPassType == nullptr)
                value |= DN2CPP_PASS_FNPTR;
            else if (row.returnSignatureType != nullptr && row.returnSignatureType != &dn2cpp_object_type)
                value |= DN2CPP_PASS_FNPTR_POINTEE;
        }
        if ((row.attrs & DN2CPP_MTHA_RETURN_BYREFLIKE) != 0)
            value |= DN2CPP_PASS_BYREFLIKE;
        if (row.returnSignatureType == &dn2cpp_void_type || row.returnSignatureType == &dn2cpp_object_type)
            value |= DN2CPP_PASS_SIGNATURE_UNKNOWN;
        return value;
    };
    int32_t fromKind = kind(from), toKind = kind(to);
    auto pointee = [](const Dn2CppMethodInfo& row, int32_t value) {
        return row.returnBindingPointeeType != nullptr ? row.returnBindingPointeeType
            : (value & DN2CPP_PASS_FNPTR) != 0 ? row.returnSignatureType : row.returnPassType;
    };
    const auto* fromPass = dn2cpp_dgbind_signature_pointee(from.returnReflectionSignature, fromDeclaring,
        fromKind, pointee(from, fromKind));
    const auto* toPass = dn2cpp_dgbind_signature_pointee(to.returnReflectionSignature, toDeclaring,
        toKind, pointee(to, toKind));
    return dn2cpp_dgbind_pass(from.returnType, fromKind, fromPass,
        to.returnType, toKind, toPass, relaxed, unknown);
}

static void dn2cpp_dgbind_require_signature(bool unknown)
{
    if (unknown)
        dn2cpp_throw_platform_not_supported(
            "CreateDelegate: the signature identity is not retained for this instantiation");
}

// Whether .NET binds `obj` where an instance of `to` is expected: a closed
// receiver, or a closed static method's first argument. The object is stored
// unconverted, so a boxed value binds wherever its box is an instance of `to` — an
// interface it implements, System.Enum, ValueType or Object. No other boxing
// conversion binds.
static bool dn2cpp_dgbind_instance_of(const Dn2CppObject* obj, const Dn2CppTypeInfo* to)
{
    const Dn2CppTypeInfo* from = obj->type;
    if (from == to || dn2cpp_dgbind_widens(from, to))
        return true;
    return (from->flags & DN2CPP_TF_VALUETYPE) != 0 && (to->flags & DN2CPP_TF_VALUETYPE) == 0
        && dn2cpp_typeinfo_assignable(from, to) != 0;
}

Dn2CppObject* dn2cpp_delegate_create(Dn2CppType* dt, Dn2CppObject* target,
                                     Dn2CppMethodRef* m, int32_t closedForm,
                                     int32_t throwOnFailure, bool fromDelegate)
{
    // Delegate.CreateDelegate names its arguments `type` and `method`; the
    // MethodInfo.CreateDelegate forms name `delegateType`, and their method is the
    // receiver, a null one being NullReferenceException.
    if (!fromDelegate)
        dn2cpp_methodref_require(m);
    const char* typeParam = fromDelegate ? "type" : "delegateType";
    if (dt == nullptr)
        dn2cpp_throw_argument_sr(&dn2cpp_argument_null_exception_type, DN2CPP_SR_ARGUMENT_NULL,
            typeParam, nullptr, 0, 0x80004003u);
    if (m == nullptr)
        dn2cpp_throw_argument_sr(&dn2cpp_argument_null_exception_type, DN2CPP_SR_ARGUMENT_NULL,
            "method", nullptr, 0, 0x80004003u);
    // A bind failure is .NET's ArgumentException, or null under the
    // throwOnBindFailure: false overloads.
    auto fail = [&]() -> Dn2CppObject* {
        if (throwOnFailure != 0)
            dn2cpp_throw_reflection_fault(&dn2cpp_argument_exception_type,
                dn2cpp_sr_message(DN2CPP_SR_DELEGATE_BIND, nullptr, 0), 0x80070057u);
        return nullptr;
    };
    const Dn2CppTypeInfo* dti = dt->typeInfo;
    if (!dn2cpp_dgbind_delegate_type(dti))
        dn2cpp_throw_argument_sr(&dn2cpp_argument_exception_type, DN2CPP_SR_MUST_BE_DELEGATE,
            typeParam, nullptr, 0, 0x80070057u);
    if (dn2cpp_type_contains_generic_parameters(dti))
        return fail();
    Dn2CppMetadataHandle<Dn2CppMethodInfo> mi = m->method;
    // A definition view has no invokable body of its own; .NET rejects binding
    // an open generic method with ArgumentException.
    if (dn2cpp_methodref_contains_generic_parameters(m) != 0
        || dn2cpp_type_metadata_only(mi->declaringType))
        return fail();
    // A native row is read in place; a packed one decodes once into the view.
    const auto rowView = mi.operator->();
    const Dn2CppMethodInfo& row = *rowView.operator->();
    Dn2CppMetadataHandle<Dn2CppMethodInfo> inv = dn2cpp_delegate_invoke_row(dti);
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
    // Binding mode: .NET's shape rules. Open static / closed instance are the
    // classic forms; a delegate one parameter LONGER than an instance method is
    // open-instance (its first argument is the receiver); a delegate one
    // parameter SHORTER than a static method is closed-static (the explicit
    // firstArgument becomes the method's first parameter). The explicit-target
    // overloads (closedForm) admit a null-bound closed-instance delegate.
    bool mStatic = (row.attrs & DN2CPP_MTHA_STATIC) != 0;
    int32_t dgArity = inv->paramCount;
    int32_t mode;
    if (mStatic)
    {
        if (dgArity == row.paramCount && target == nullptr)
            mode = DN2CPP_DGBIND_OPEN_STATIC;
        else if (dgArity == row.paramCount - 1 && closedForm != 0)
            mode = DN2CPP_DGBIND_CLOSED_STATIC;
        else
            return fail();
    }
    else
    {
        if (target != nullptr && dgArity == row.paramCount)
            mode = DN2CPP_DGBIND_CLOSED_INSTANCE;
        else if (target == nullptr && dgArity == row.paramCount + 1)
            mode = DN2CPP_DGBIND_OPEN_INSTANCE;
        else if (target == nullptr && dgArity == row.paramCount && closedForm != 0)
            mode = DN2CPP_DGBIND_CLOSED_INSTANCE; // null-bound; invoking it faults
        else
            return fail();
    }

    // A template level's row binds and runs on the clone level the MethodInfo was
    // obtained through, as Invoke's do.
    const Dn2CppTypeInfo* declTi = dn2cpp_invoke_declaring(row.declaringType, m->reflectedType);
    if (mode == DN2CPP_DGBIND_CLOSED_INSTANCE && target != nullptr
        && !dn2cpp_dgbind_instance_of(target, declTi))
        return fail();
    if (mode == DN2CPP_DGBIND_OPEN_INSTANCE)
    {
        // A value-type receiver would need a byref first argument (real .NET's
        // open-instance-over-struct shape) — not modeled.
        if ((declTi->flags & DN2CPP_TF_VALUETYPE) != 0)
            dn2cpp_throw_platform_not_supported(
                "CreateDelegate: an open-instance delegate over a value-type receiver is not supported");
        if (inv->parameters[0]->passKind != 0
            || !dn2cpp_dgbind_widens(inv->parameters[0]->paramType, declTi))
            return fail();
    }
    if (mode == DN2CPP_DGBIND_CLOSED_STATIC)
    {
        // .NET's first-argument binding stores the bound object unconverted, so
        // the method's first parameter must be a reference type (a value-typed
        // first parameter is ArgumentException in real .NET too).
        const Dn2CppTypeInfo* p0 = row.parameters[0]->paramType;
        if (row.parameters[0]->passKind != 0 || (p0->flags & DN2CPP_TF_VALUETYPE) != 0)
            return fail();
        if (target != nullptr && !dn2cpp_dgbind_instance_of(target, p0))
            return fail();
    }
    // Remaining parameters: delegate argument j feeds method parameter
    // (j - dgFirst) + mFirst; contravariant reference widening allowed.
    int32_t dgFirst = (mode == DN2CPP_DGBIND_OPEN_INSTANCE) ? 1 : 0;
    int32_t mFirst = (mode == DN2CPP_DGBIND_CLOSED_STATIC) ? 1 : 0;
    bool unknown = false;
    for (int32_t j = dgFirst; j < dgArity; j++)
        if (!dn2cpp_dgbind_parameter(*inv->parameters[j],
            *row.parameters[mFirst + (j - dgFirst)], true, unknown, dti, declTi))
            return fail();
    // Return: covariant reference widening from the method's to the delegate's.
    if (!dn2cpp_dgbind_return(row, *inv, true, unknown, declTi, dti))
        return fail();
    dn2cpp_dgbind_require_signature(unknown);
    // .NET binds a static virtual or abstract interface member only open, and a call
    // through that binding finds no entry point (dn2cpp_reflbind_invoke).
    bool staticVirtual = mStatic && (row.ilAttrs & DN2CPP_MA_VIRTUAL) != 0
        && (declTi->flags & DN2CPP_TF_INTERFACE) != 0;
    if (staticVirtual && mode == DN2CPP_DGBIND_CLOSED_STATIC)
        dn2cpp_throw_static_virtual_entry_point();
    // .NET refuses an open binding of a generic virtual method once its shape binds,
    // even a final one and under throwOnBindFailure: false.
    if (mode == DN2CPP_DGBIND_OPEN_INSTANCE && row.genericParamCount != 0 && (row.ilAttrs & DN2CPP_MA_VIRTUAL) != 0)
        dn2cpp_throw_reflection_fault(&dn2cpp_not_supported_exception_type, nullptr, 0x80131515u);
    // The shape binds; only now can the image refuse. A bodiless virtual row
    // (abstract or interface) binds the receiver's slot at each call, as
    // MethodInfo.Invoke does, and a generic virtual row its dispatcher. A boxed
    // value has no vtable, so a class row bound to one runs its own body. A binding
    // closed over null runs the row's own body, and a bodiless row faults when called
    // (dn2cpp_reflbind_invoke).
    bool nullBound = mode == DN2CPP_DGBIND_CLOSED_INSTANCE && target == nullptr;
    bool slotBound = !mStatic && !nullBound && row.vtableSlot >= 0 && (row.ilAttrs & DN2CPP_MA_VIRTUAL) != 0
        && ((declTi->flags & DN2CPP_TF_INTERFACE) != 0
            || ((declTi->flags & DN2CPP_TF_VALUETYPE) == 0
                && (target == nullptr || target->type->vtable != nullptr)));
    if (!slotBound && !mStatic && !nullBound && row.genericParamCount != 0)
        slotBound = dn2cpp_gvm_row_dispatch_of(row) != nullptr;
    bool bodiless = nullBound && (row.ilAttrs & DN2CPP_MA_ABSTRACT) != 0;
    // A metadata-answered row needs no body; dn2cpp_invoke_row answers it.
    bool answered = (row.attrs & DN2CPP_MTHA_METAANSWER) != 0;
    if (!staticVirtual && !bodiless && !answered
        && (row.invoker == nullptr || (row.fnPtr == nullptr && !slotBound)))
        dn2cpp_throw_invoke_unavailable(mi, m->reflectedType);

    auto* node = static_cast<Dn2CppReflBind*>(dn2cpp_alloc(sizeof(Dn2CppReflBind)));
    node->type = &dn2cpp_reflbind_type;
    node->method = mi;
    dn2cpp_gc_store_ref(&node->target, target);
    node->mode = mode;
    node->virtualRow = (row.ilAttrs & DN2CPP_MA_VIRTUAL) != 0 ? 1 : 0;
    node->slotKind = DN2CPP_DGBIND_SLOT_CLASS;
    node->slot = -1;
    if (node->virtualRow != 0 && mode == DN2CPP_DGBIND_CLOSED_INSTANCE && target != nullptr)
    {
        // An Object or ValueType row names Object's member, whose class slot every
        // type shares (Dn2CppItfImplSlots).
        if (answered)
        {
            const int32_t member = dn2cpp_meta_object_virtual_index(dn2cpp_meta_desc_of(mi));
            if (member >= 0)
            {
                node->slotKind = DN2CPP_DGBIND_SLOT_OBJECT;
                node->slot = member;
            }
        }
        else if (row.vtableSlot >= 0 && row.genericParamCount == 0)
        {
            if ((declTi->flags & DN2CPP_TF_INTERFACE) != 0)
            {
                node->slotKind = DN2CPP_DGBIND_SLOT_INTERFACE;
                node->slot = row.vtableSlot;
            }
            else if ((declTi->flags & DN2CPP_TF_VALUETYPE) == 0 && target->type->vtable != nullptr)
                node->slot = row.vtableSlot;
        }
    }
    node->parameters = row.parameters;
    // What .NET faults a call with before the body runs. A binding closed over a
    // receiver always calls with it. A null receiver runs the row's own body with a
    // null `this`, as .NET's call does, except where .NET dereferences it first: an
    // open binding of a virtual row dispatches on it. A binding closed over null
    // never dispatches, so a bodiless row there is bad IL. The own body runs only
    // where it tolerates the null receiver as .NET's does (DN2CPP_MTHA_NULLSAFE, and
    // the System.Object and System.ValueType rows the runtime answers) or reads only
    // its generic context off it (DN2CPP_MTHA_NULLCONTEXT), or keeps null while
    // reading the context separately (DN2CPP_MTHA_NULLCTX_NULL); any other faults as
    // .NET's dereference of the receiver would.
    node->declaring = declTi;
    if (staticVirtual)
        node->fault = DN2CPP_DGBIND_FAULT_STATIC_VIRTUAL;
    else if (mStatic || (mode == DN2CPP_DGBIND_CLOSED_INSTANCE && target != nullptr))
        node->fault = DN2CPP_DGBIND_FAULT_NONE;
    else if (mode == DN2CPP_DGBIND_OPEN_INSTANCE && (row.ilAttrs & DN2CPP_MA_VIRTUAL) != 0)
        node->fault = DN2CPP_DGBIND_FAULT_NULL_RECEIVER;
    else if ((row.ilAttrs & DN2CPP_MA_ABSTRACT) != 0)
        node->fault = DN2CPP_DGBIND_FAULT_NULL_BODILESS;
    else if ((row.attrs & (DN2CPP_MTHA_NULLSAFE | DN2CPP_MTHA_NULLCONTEXT | DN2CPP_MTHA_NULLCTX_NULL)) != 0
        && dn2cpp_null_lookups_hit(row.attrs, declTi))
        node->fault = DN2CPP_DGBIND_FAULT_NULL_RECEIVER;
    else if ((row.attrs & DN2CPP_MTHA_NULLSAFE) != 0
        || (answered && (declTi->flags & DN2CPP_TF_VALUETYPE) == 0))
        node->fault = DN2CPP_DGBIND_FAULT_NONE;
    else if ((row.attrs & DN2CPP_MTHA_NULLCONTEXT) != 0)
        node->fault = DN2CPP_DGBIND_FAULT_NULL_CONTEXT;
    else if ((row.attrs & DN2CPP_MTHA_NULLCTX_NULL) != 0)
        node->fault = DN2CPP_DGBIND_FAULT_NULL_CTX_NULL;
    else
        node->fault = DN2CPP_DGBIND_FAULT_NULL_RECEIVER;
    size_t sz = sizeof(Dn2CppDelegate);
    if (dti->instanceSize > 0 && static_cast<size_t>(dti->instanceSize) > sz)
        sz = static_cast<size_t>(dti->instanceSize);
    auto* dg = static_cast<Dn2CppDelegate*>(dn2cpp_alloc(sz));
    dg->type = dti;
    dn2cpp_gc_store_ref(&dg->target, reinterpret_cast<Dn2CppObject*>(node));
    dg->method = tramp;
    dg->prev = nullptr;
    return reinterpret_cast<Dn2CppObject*>(dg);
}

// Delegate name binding uses the host's CLR invariant uppercase mapping.
static char16_t dn2cpp_dgbind_upper(char16_t c)
{
#ifdef _WIN32
    wchar_t source = static_cast<wchar_t>(c);
    wchar_t mapped;
    if (LCMapStringEx(LOCALE_NAME_INVARIANT, LCMAP_UPPERCASE, &source, 1,
            &mapped, 1, nullptr, nullptr, 0) != 0)
        return static_cast<char16_t>(mapped);
    return c;
#else
    return dn2cpp_char_upper_invariant(c);
#endif
}

// The CLR passes the requested name through a NUL-terminated UTF-8 QCall string.
static bool dn2cpp_dgbind_name_matches(const char* candidate, Dn2CppString* name, int32_t ignoreCase)
{
    Dn2CppString* decoded = dn2cpp_string_from_utf8(candidate,
        static_cast<int32_t>(std::strlen(candidate)));
    int32_t length = 0;
    while (length < name->length && name->chars[length] != 0)
        length++;
    if (decoded->length != length)
        return false;
    for (int32_t i = 0; i < length; i++)
    {
        char16_t a = decoded->chars[i];
        char16_t b = name->chars[i];
        if (ignoreCase != 0 && a != b)
        {
            a = dn2cpp_dgbind_upper(a);
            b = dn2cpp_dgbind_upper(b);
        }
        if (a != b)
            return false;
    }
    return true;
}

static const Dn2CppBindingSignature* dn2cpp_dgbind_stand_in(const Dn2CppRuntimeTemplate& level,
    const Dn2CppTypeInfo* type)
{
    for (int32_t i = 0; i < level.bindingStandInCount; i++)
        if (level.bindingStandIns[i].type == type)
            return level.bindingStandIns[i].signature;
    return nullptr;
}

static bool dn2cpp_dgbind_binding_type_known(const Dn2CppTypeInfo* type)
{
    // Closed public generic TIs retain their definition and actual arguments;
    // their lazy Type-object cache does not determine their signature identity.
    return type != nullptr && (type->typeObject != nullptr
        || ((type->flags & (DN2CPP_TF_SHARED_CANON | DN2CPP_TF_GENERICDEF)) == 0
            && type->genericDef != nullptr && type->genericArgs != nullptr && type->genericArgCount > 0)
        || ((type->flags & (DN2CPP_TF_ARRAY | DN2CPP_TF_SHARED_CANON)) == DN2CPP_TF_ARRAY
            && type->arrayRank > 0 && dn2cpp_dgbind_binding_type_known(type->elementType)));
}

static Dn2CppBindingSignature dn2cpp_dgbind_actual_type(const Dn2CppTypeInfo* type)
{
    // A public nongeneric TI's CLR name proves inequality even while its lazy
    // Type-object cache is empty. Equal names do not prove type identity.
    if (type != nullptr && type->typeObject == nullptr && type->name != nullptr
        && (type->flags & (DN2CPP_TF_SHARED_CANON | DN2CPP_TF_GENERICDEF | DN2CPP_TF_ARRAY)) == 0
        && type->genericDef == nullptr && type->genericArgCount == 0
        && (type->base != nullptr
            || (type->flags & (DN2CPP_TF_VALUETYPE | DN2CPP_TF_INTERFACE)) != 0))
        return { 8, nullptr, 0, nullptr, 0, type->name };
    return { 0, type, 0, nullptr, 0, nullptr };
}

// Unknown leaves cannot establish inequality. These records affect candidate
// selection only; Pointer boxes and Invoke retain their original stand-ins.
static bool dn2cpp_dgbind_binding_mismatch(Dn2CppBindingSignature a, Dn2CppBindingSignature b,
    const Dn2CppTypeInfo& declaring)
{
    if (a.kind == 5)
    {
        if (a.value < 0 || a.value >= declaring.genericArgCount)
            return false;
        a = dn2cpp_dgbind_actual_type(declaring.genericArgs[a.value]);
    }
    if (a.kind == 0 && a.type != nullptr && a.type->typeObject == nullptr && a.name != nullptr)
        a.kind = 8;
    if (b.kind == 0 && b.type != nullptr && b.type->typeObject == nullptr && b.name != nullptr)
        b.kind = 8;
    // Array shape is public even when its element's Type-object cache is empty.
    auto arrayShape = [](const Dn2CppBindingSignature& value) {
        return value.kind == 0 && value.type != nullptr
            && (value.type->flags & (DN2CPP_TF_ARRAY | DN2CPP_TF_SHARED_CANON)) == DN2CPP_TF_ARRAY
            && value.type->arrayRank > 0;
    };
    auto nonArrayShape = [](const Dn2CppBindingSignature& value) {
        return (value.kind >= 1 && value.kind <= 4) || value.kind == 8
            || (value.kind == 0 && dn2cpp_dgbind_binding_type_known(value.type)
                && (value.type->flags & DN2CPP_TF_ARRAY) == 0);
    };
    if ((arrayShape(a) && nonArrayShape(b)) || (arrayShape(b) && nonArrayShape(a)))
        return true;
    if ((a.kind == 6 || a.kind == 7) && b.kind == 0
        && (arrayShape(b) || dn2cpp_dgbind_binding_type_known(b.type)))
    {
        if ((b.type->flags & DN2CPP_TF_ARRAY) == 0)
            return true;
        int32_t rank = a.kind == 6 ? 1 : a.value;
        if (b.type->arrayRank <= 0 || a.childCount != 1)
            return false;
        if (rank != b.type->arrayRank)
            return true;
        if (rank == 1 && (a.kind == 6) == dn2cpp_is_md_array(b.type))
            return true;
        // Equal shape still requires a known element identity.
        if (b.type->elementType == nullptr)
            return false;
        return dn2cpp_dgbind_binding_mismatch(a.children[0],
            dn2cpp_dgbind_actual_type(b.type->elementType), declaring);
    }
    if (a.kind == 0 && (b.kind == 6 || b.kind == 7)
        && (arrayShape(a) || dn2cpp_dgbind_binding_type_known(a.type)))
        return dn2cpp_dgbind_binding_mismatch(b, a, declaring);
    if ((a.kind == 0 && !dn2cpp_dgbind_binding_type_known(a.type))
        || (b.kind == 0 && !dn2cpp_dgbind_binding_type_known(b.type)))
        return false;
    if (a.kind == 8 || b.kind == 8)
    {
        // TI names and decoded nongeneric names are CLR FullNames. Equality of
        // names cannot prove identity across assemblies or absent metadata.
        const char* left = a.kind == 8 ? a.name : a.kind == 0 ? a.type->name : nullptr;
        const char* right = b.kind == 8 ? b.name : b.kind == 0 ? b.type->name : nullptr;
        if (left != nullptr && right != nullptr)
            return std::strcmp(left, right) != 0;
        int32_t other = a.kind == 8 ? b.kind : a.kind;
        return other == 1 || other == 2 || other == 3 || other == 4 || other == 6 || other == 7;
    }
    if (a.kind == 0 && b.kind == 0)
        return a.type != b.type;
    if (a.kind == 4 && b.kind == 0)
    {
        if (b.type->genericDef == nullptr || (a.type != nullptr && a.type != b.type->genericDef))
            return true;
        if (a.name != nullptr && b.type->genericDef->name != nullptr
            && std::strcmp(a.name, b.type->genericDef->name) != 0)
            return true;
        if (b.type->genericArgs == nullptr || a.childCount != b.type->genericArgCount)
            return false;
        for (int32_t i = 0; i < a.childCount; i++)
            if (dn2cpp_dgbind_binding_mismatch(a.children[i],
                dn2cpp_dgbind_actual_type(b.type->genericArgs[i]), declaring))
                return true;
        return false;
    }
    if (a.kind == 0 && b.kind == 4)
        return dn2cpp_dgbind_binding_mismatch(b, a, declaring);
    if (a.kind != b.kind)
        return true;
    if (a.kind == 3 && (a.value != b.value || a.childCount != b.childCount))
        return true;
    if (a.kind == 7 && a.value != b.value)
        return true;
    if (a.kind == 4)
    {
        if (a.type != nullptr && b.type != nullptr && a.type != b.type)
            return true;
        if (a.name != nullptr && b.name != nullptr && std::strcmp(a.name, b.name) != 0)
            return true;
        // Missing family TIs do not erase inequality of corresponding arguments.
    }
    if (a.childCount != b.childCount)
        return false;
    for (int32_t i = 0; i < a.childCount; i++)
        if (dn2cpp_dgbind_binding_mismatch(a.children[i], b.children[i], declaring))
            return true;
    return false;
}

// Unlike MethodInfo binding, name binding admits no reference variance.
// ParameterType alone erases by-ref and pointer shapes; their ABI descriptors
// must agree too. Ref and out differ only in parameter attributes.
static bool dn2cpp_dgbind_named_argument_mismatch(const Dn2CppMethodInfo& row,
    const Dn2CppMethodInfo& invoke, const Dn2CppTypeInfo* queried)
{
    const auto* declaring = dn2cpp_invoke_declaring(row.declaringType, queried);
    if ((declaring->flags & DN2CPP_TF_RUNTIME_SYNTH) == 0 || declaring->genericArgs == nullptr)
        return false;
    const auto* level = dn2cpp_runtime_template_by_ti(dn2cpp_runtime_template_of(declaring));
    if (level == nullptr)
        return false;
    for (int32_t i = 0; i < level->bindingTypeArgumentCount; i++)
    {
        const auto& binding = level->bindingTypeArguments[i];
        if (binding.methodToken != row.metadataToken
            || binding.typeArgument >= declaring->genericArgCount)
            continue;
        const Dn2CppTypeInfo* known = nullptr;
        if (binding.parameter == -1)
        {
            if ((row.returnSignatureType == &dn2cpp_object_type || row.returnSignatureType == &dn2cpp_void_type)
                && invoke.returnSignatureType != &dn2cpp_object_type && invoke.returnSignatureType != &dn2cpp_void_type)
                known = invoke.returnBindingPointeeType != nullptr ? invoke.returnBindingPointeeType
                    : invoke.returnPassType != nullptr ? invoke.returnPassType : invoke.returnSignatureType;
        }
        else if (binding.parameter >= 0 && binding.parameter < row.paramCount)
        {
            const auto a = row.parameters[binding.parameter].operator->();
            const auto b = invoke.parameters[binding.parameter].operator->();
            if ((a->passKind & DN2CPP_PASS_SIGNATURE_UNKNOWN) != 0
                && (b->passKind & DN2CPP_PASS_SIGNATURE_UNKNOWN) == 0)
                known = b->bindingPointeeType != nullptr ? b->bindingPointeeType : b->passType;
        }
        const auto* structure = dn2cpp_dgbind_stand_in(*level, known);
        int32_t depth = 0;
        while (structure != nullptr && structure->kind == 1 && structure->childCount == 1)
        {
            depth++;
            structure = structure->children;
        }
        if (structure != nullptr || (known != nullptr && known->typeObject != nullptr))
        {
            if (depth != binding.pointerDepth)
                return true;
        }
        if (binding.signature != nullptr)
        {
            if (structure != nullptr && dn2cpp_dgbind_binding_mismatch(*binding.signature, *structure, *declaring))
                return true;
            continue;
        }
        if (structure != nullptr)
        {
            const auto* leaf = structure;
            int32_t step = 0;
            for (; step < binding.pathCount; step++)
            {
                const auto& path = binding.path[step];
                if (dn2cpp_dgbind_binding_mismatch(
                    { 4, path.definition, 0, nullptr, 0, path.definition != nullptr ? path.definition->name : nullptr },
                    *leaf, *declaring))
                    return true;
                if (leaf->kind != 4 || leaf->children == nullptr
                    || path.argument < 0 || path.argument >= leaf->childCount)
                    break;
                leaf = leaf->children + path.argument;
            }
            if (step == binding.pathCount && binding.typeArgument >= 0
                && dn2cpp_dgbind_binding_mismatch(
                    { 5, nullptr, binding.typeArgument, nullptr, 0, nullptr }, *leaf, *declaring))
                return true;
            if (step == binding.pathCount && binding.typeArgument == -2
                && dn2cpp_dgbind_binding_mismatch(
                    { 4, binding.type, 0, nullptr, 0, binding.type != nullptr ? binding.type->name : nullptr },
                    *leaf, *declaring))
                return true;
            if (step == binding.pathCount && binding.typeArgument == -1
                && dn2cpp_dgbind_binding_mismatch(
                    dn2cpp_dgbind_actual_type(binding.type), *leaf, *declaring))
                return true;
        }
        if (structure != nullptr)
            known = structure->kind == 0 ? structure->type : nullptr;
        // An unrecorded stand-in cannot prove a CLR type inequality.
        int32_t step = 0;
        for (; step < binding.pathCount && known != nullptr && known->typeObject != nullptr; step++)
        {
            const auto& path = binding.path[step];
            if (known->genericDef != path.definition)
                return true;
            if (known->genericArgs == nullptr || path.argument < 0 || path.argument >= known->genericArgCount)
                break;
            known = known->genericArgs[path.argument];
        }
        if (step != binding.pathCount || known == nullptr || known->typeObject == nullptr)
            continue;
        if (binding.typeArgument >= 0 ? declaring->genericArgs[binding.typeArgument] != known
            : binding.typeArgument == -2 ? known->genericDef != binding.type : known != binding.type)
            return true;
    }
    return false;
}

static bool dn2cpp_dgbind_named_signature(const Dn2CppMethodInfo& row, const Dn2CppMethodInfo& invoke,
    const Dn2CppTypeInfo* queried, bool& skippedUnknown)
{
    if (row.genericParamCount != 0 || row.paramCount != invoke.paramCount)
        return false;
    bool unknown = false;
    const auto* declaring = dn2cpp_invoke_declaring(row.declaringType, queried);
    for (int32_t i = 0; i < row.paramCount; i++)
    {
        const auto a = row.parameters[i].operator->();
        const auto b = invoke.parameters[i].operator->();
        if (!dn2cpp_dgbind_parameter(*a.operator->(), *b.operator->(), false, unknown,
            declaring, invoke.declaringType))
            return false;
    }
    if (!dn2cpp_dgbind_return(row, invoke, false, unknown, declaring, invoke.declaringType))
        return false;
    if (unknown && dn2cpp_dgbind_named_argument_mismatch(row, invoke, queried))
    {
        skippedUnknown = true;
        return false;
    }
    dn2cpp_dgbind_require_signature(unknown);
    return true;
}

void dn2cpp_delegate_initialize_named(Dn2CppObject* receiver, Dn2CppObject* target,
    Dn2CppType* targetType, Dn2CppString* name, int32_t staticForm)
{
    if (receiver == nullptr)
        dn2cpp_throw_null_reference();
    auto* bound = reinterpret_cast<Dn2CppDelegate*>(dn2cpp_delegate_create_named(
        dn2cpp_get_type_from_handle(receiver->type), target, targetType, name,
        staticForm, staticForm != 0 ? 1 : 0, staticForm != 0 ? 0 : 1));
    if (bound == nullptr)
        return;
    auto* self = reinterpret_cast<Dn2CppDelegate*>(receiver);
    // CoreCLR can rebind invocation while preserving a stale multicast list.
    // Our uniform invocation chain cannot represent that hybrid state.
    if (self->prev != nullptr)
        dn2cpp_throw_platform_not_supported(
            "Named delegate constructors cannot reinitialize an existing multicast invocation list.");
    if (self->bindingSnapshotted.load(std::memory_order_relaxed))
        dn2cpp_throw_platform_not_supported(
            "Named delegate constructors cannot reinitialize a binding previously copied into a multicast invocation list.");
    dn2cpp_gc_store_ref(&self->target, bound->target);
    self->method = bound->method;
    self->identity = bound->identity;
    dn2cpp_gc_store_ref(&self->prev, static_cast<Dn2CppObject*>(nullptr));
    dn2cpp_gc_store_ref(&self->entry, static_cast<Dn2CppObject*>(nullptr));
    dn2cpp_gc_store_ref(&self->invocationCache, static_cast<Dn2CppDelegateInvocationCache*>(nullptr));
}

Dn2CppObject* dn2cpp_delegate_create_named(Dn2CppType* dt, Dn2CppObject* target,
    Dn2CppType* targetType, Dn2CppString* name, int32_t staticForm,
    int32_t ignoreCase, int32_t throwOnFailure)
{
    if (dt == nullptr)
        dn2cpp_throw_argument_null_param("type");
    if (staticForm != 0 ? targetType == nullptr : target == nullptr)
        dn2cpp_throw_argument_null_param("target");
    if (name == nullptr)
        dn2cpp_throw_argument_null_param("method");
    const Dn2CppTypeInfo* queried = staticForm != 0 ? targetType->typeInfo : target->type;
    if (staticForm != 0 && dn2cpp_type_contains_generic_parameters(queried))
        dn2cpp_throw_argument_sr(&dn2cpp_argument_exception_type, DN2CPP_SR_UNBOUND_GENERIC,
            "target", nullptr, 0, 0x80070057u);
    const Dn2CppTypeInfo* dti = dt->typeInfo;
    if (!dn2cpp_dgbind_delegate_type(dti))
        dn2cpp_throw_argument_sr(&dn2cpp_argument_exception_type, DN2CPP_SR_MUST_BE_DELEGATE,
            "type", nullptr, 0, 0x80070057u);
    auto fail = [&]() -> Dn2CppObject* {
        if (throwOnFailure != 0)
            dn2cpp_throw_reflection_fault(&dn2cpp_argument_exception_type,
                dn2cpp_sr_message(DN2CPP_SR_DELEGATE_BIND, nullptr, 0), 0x80070057u);
        return nullptr;
    };
    if (dn2cpp_type_contains_generic_parameters(dti))
        return fail();
    dn2cpp_require_metadata(dti);
    const auto invoke = dn2cpp_delegate_invoke_row(dti);
    if (invoke == nullptr)
        dn2cpp_throw_platform_not_supported(
            "CreateDelegate: the delegate type carries no reflected Invoke row in this image");
    const auto invokeView = invoke.operator->();
    bool skippedUnknown = false;
    auto matches = [&](const Dn2CppMethodInfo& row) {
        return ((row.attrs & DN2CPP_MTHA_STATIC) != 0) == (staticForm != 0)
            && dn2cpp_dgbind_name_matches(row.name, name, ignoreCase)
            && dn2cpp_dgbind_named_signature(row, *invokeView.operator->(), queried, skippedUnknown);
    };
    auto bind = [&](Dn2CppMetadataHandle<Dn2CppMethodInfo> method) {
        return dn2cpp_delegate_create(dt, target, dn2cpp_make_methodref(method, queried),
            staticForm == 0 ? 1 : 0, throwOnFailure, true);
    };
    auto constructor = [&](const Dn2CppTypeInfo* ti) {
        Dn2CppMetadataHandle<Dn2CppMethodInfo> selected;
        const auto reflection = ti->reflection();
        if (staticForm != 0)
        {
            if (reflection.initializer != nullptr && matches(*reflection.initializer.operator->().operator->()))
                return reflection.initializer;
        }
        else
            for (int32_t i = reflection.ctorCount - 1; i >= 0; i--)
                if (matches(*reflection.ctors[i].operator->().operator->()))
                    return reflection.ctors[i];
        return selected;
    };
    dn2cpp_require_metadata(queried);
    auto metadataMethod = [&](bool isVirtual) {
        Dn2CppMetadataHandle<Dn2CppMethodInfo> selected;
        for (int32_t k = 0; k < g_meta_member_count; k++)
        {
            const auto& descriptor = g_meta_members[k];
            if (!dn2cpp_dgbind_name_matches(descriptor.methodName, name, ignoreCase))
                continue;
            Dn2CppMethodCandidates candidates;
            Dn2CppString* canonicalName = dn2cpp_string_from_utf8(descriptor.methodName,
                static_cast<int32_t>(std::strlen(descriptor.methodName)));
            dn2cpp_meta_lookup(queried, canonicalName, 0, nullptr,
                DN2CPP_BF_PUBLIC | DN2CPP_BF_NONPUBLIC
                    | (staticForm != 0 ? DN2CPP_BF_STATIC | DN2CPP_BF_FLATTEN : DN2CPP_BF_INSTANCE), candidates);
            for (int32_t i = 0; i < candidates.size(); i++)
            {
                const auto method = candidates[i];
                const auto row = method.operator->();
                if (((row->ilAttrs & DN2CPP_MA_VIRTUAL) != 0) == isVirtual
                    && matches(*row.operator->()))
                    return method;
            }
        }
        return selected;
    };

    // The CLR checks the receiver's non-virtual methods in reverse declaration
    // order, its virtual slots (inherited ones included), then each base's
    // non-virtual methods. A compatible overload does not hide its base siblings.
    if (const auto method = constructor(queried))
        return bind(method);
    const auto own = queried->reflection();
    for (int32_t i = own.methodCount - 1; i >= 0; i--)
    {
        const auto method = own.methods[i];
        const auto row = method.operator->();
        if (((row->attrs & DN2CPP_MTHA_STATIC) != 0 || (row->ilAttrs & DN2CPP_MA_VIRTUAL) == 0)
            && matches(*row.operator->()))
            return bind(method);
    }
    if (staticForm == 0)
    {
        struct NamedSlot
        {
            int32_t slot;
            bool taken;
            Dn2CppMetadataHandle<Dn2CppMethodInfo> method;
        };
        Dn2CppWalkList<NamedSlot, 64> slots;
        Dn2CppMetadataHandle<Dn2CppMethodInfo> selected;
        int32_t selectedSlot = -1;
        for (const Dn2CppTypeInfo* ti = queried; ti != nullptr; ti = ti->base)
        {
            dn2cpp_require_metadata(ti);
            const auto reflection = ti->reflection();
            for (int32_t i = reflection.methodCount - 1; i >= 0; i--)
            {
                const auto method = reflection.methods[i];
                const auto row = method.operator->();
                if ((row->ilAttrs & DN2CPP_MA_VIRTUAL) == 0 || row->genericParamCount != 0
                    || (row->attrs & DN2CPP_MTHA_STATIC) != 0 || row->vtableSlot < 0)
                    continue;
                int32_t index = 0;
                while (index < slots.size() && slots[index].slot != row->vtableSlot)
                    index++;
                if (index == slots.size())
                    slots.push_back({ row->vtableSlot, false, {} });
                auto& slot = slots[index];
                if (!slot.taken)
                {
                    slot.taken = true;
                    slot.method = matches(*row.operator->()) ? method : Dn2CppMetadataHandle<Dn2CppMethodInfo>{};
                }
                // New-slot roots are visited in CLR virtual-slot order. A
                // MethodImpl can alias this chain to a base's C++ dispatch slot;
                // closing it leaves the base declaration available independently.
                if ((row->ilAttrs & DN2CPP_MA_NEWSLOT) != 0)
                {
                    if (slot.method != nullptr)
                        return bind(slot.method);
                    slot.taken = false;
                }
            }
        }
        // Runtime-owned Object slots have no emitted new-slot declaration.
        for (int32_t i = 0; i < slots.size(); i++)
            if (slots[i].taken && slots[i].method != nullptr && slots[i].slot > selectedSlot)
            {
                selected = slots[i].method;
                selectedSlot = slots[i].slot;
            }
        if (selected != nullptr)
            return bind(selected);
        selected = metadataMethod(true);
        if (selected != nullptr)
            return bind(selected);
    }
    for (const Dn2CppTypeInfo* ti = queried->base; ti != nullptr; ti = ti->base)
    {
        dn2cpp_require_metadata(ti);
        if (const auto method = constructor(ti))
            return bind(method);
        const auto reflection = ti->reflection();
        for (int32_t i = reflection.methodCount - 1; i >= 0; i--)
        {
            const auto method = reflection.methods[i];
            const auto row = method.operator->();
            if (((row->attrs & DN2CPP_MTHA_STATIC) != 0 || (row->ilAttrs & DN2CPP_MA_VIRTUAL) == 0)
                && matches(*row.operator->()))
                return bind(method);
        }
    }
    const auto metadata = metadataMethod(false);
    if (metadata != nullptr)
        return bind(metadata);
    // A proved mismatch permits later overloads, but does not make an otherwise
    // unresolved template signature a supported missing-method query.
    dn2cpp_dgbind_require_signature(skippedUnknown);
    return fail();
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

// Whether a row closes the instantiation whose arguments argAt(0..argc-1) name.
template<class ArgAt>
static bool dn2cpp_row_args_equal(const Dn2CppMethodInfo& row, int32_t argc, ArgAt argAt)
{
    if (row.genericParamCount != argc)
        return false;
    if (argc == 0)
        return true;
    if (row.genericArgs == nullptr)
        return false;
    for (int32_t i = 0; i < argc; i++)
        if (row.genericArgs[i] != argAt(i))
            return false;
    return true;
}

// The row compiled for definition `token` at the given arguments; token 0 names no
// definition, so it never matches.
template<class ArgAt>
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_find_method_instantiation(
    const Dn2CppTypeReflection& reflection, int32_t token, int32_t argc, ArgAt argAt)
{
    if (token == 0)
        return {};
    for (int32_t i = 0; i < reflection.methodCount; i++)
    {
        const auto handle = reflection.methods[i];
        const auto row = handle.operator->();
        if (row->metadataToken == token && dn2cpp_row_args_equal(*row.operator->(), argc, argAt))
            return handle;
    }
    return {};
}

Dn2CppObject* dn2cpp_delegate_object_virtual_invoker(void* fn, Dn2CppObject* receiver,
    Dn2CppObject** arguments, const Dn2CppTypeInfo*)
{
    if (fn == reinterpret_cast<void*>(&dn2cpp_object_tostring_virtual))
        return dn2cpp_meta_object_tostring(nullptr, receiver, arguments);
    if (fn == reinterpret_cast<void*>(&dn2cpp_object_equals_virtual))
        return dn2cpp_meta_object_equals(nullptr, receiver, arguments);
    return dn2cpp_meta_object_gethashcode(nullptr, receiver, arguments);
}

static const Dn2CppDelegateInterfaceTarget* g_delegate_interface_targets = nullptr;
static int32_t g_delegate_interface_target_count = 0;

void dn2cpp_set_delegate_interface_targets(const Dn2CppDelegateInterfaceTarget* rows, int32_t count)
{
    g_delegate_interface_targets = rows;
    g_delegate_interface_target_count = count;
}

static const Dn2CppInterfaceMethodTarget* dn2cpp_delegate_interface_identity(
    const Dn2CppTypeInfo* receiver, const Dn2CppTypeInfo* itf, int32_t slot)
{
    if (slot < 0)
        return nullptr;
    // Array variance shares an executable row, but the CLR helper instantiation
    // retains the requested closed interface's type argument.
    if ((receiver->flags & DN2CPP_TF_ARRAY) != 0)
        for (int32_t i = 0; i < g_delegate_interface_target_count; ++i)
        {
            const auto& candidate = g_delegate_interface_targets[i];
            if (candidate.receiverType == nullptr && candidate.interfaceType == itf && candidate.slot == slot)
                return &candidate.target;
        }
    const auto* entry = dn2cpp_dispatch_interface_row_of(receiver, itf);
    if (entry != nullptr && entry->methodTargets != nullptr && entry->methodTargets[slot].declaringType != nullptr)
        return &entry->methodTargets[slot];
    for (int32_t i = 0; i < g_delegate_interface_target_count; ++i)
    {
        const auto& candidate = g_delegate_interface_targets[i];
        if (candidate.receiverType == receiver && candidate.interfaceType == itf && candidate.slot == slot)
            return &candidate.target;
    }
    return nullptr;
}

Dn2CppObject* dn2cpp_delegate_interface_invoker(void* context, Dn2CppObject* receiver,
    Dn2CppObject** arguments, const Dn2CppTypeInfo* returnType)
{
    const auto* dispatch = static_cast<const Dn2CppDelegateInterfaceInvocation*>(context);
    void* fn = const_cast<void*>(dn2cpp_resolve_interface(receiver->type, dispatch->interfaceType)[dispatch->slot]);
    return reinterpret_cast<Dn2CppObject*(*)(void*, Dn2CppObject*, Dn2CppObject**, const Dn2CppTypeInfo*)>(dispatch->invoker)(
        fn, receiver, arguments, returnType);
}

static const Dn2CppDelegateMethodCatalog* g_delegate_method_catalog = nullptr;
static int32_t g_delegate_method_catalog_count = 0;

void dn2cpp_set_delegate_method_catalog(const Dn2CppDelegateMethodCatalog* rows, int32_t count)
{
    g_delegate_method_catalog = rows;
    g_delegate_method_catalog_count = count;
}

static Dn2CppMetadataTable<Dn2CppMethodInfo> dn2cpp_delegate_private_methods(
    const Dn2CppTypeInfo* owner, int32_t& count)
{
    for (int32_t i = 0; i < g_delegate_method_catalog_count; ++i)
        if (g_delegate_method_catalog[i].declaringType == owner)
        {
            count = g_delegate_method_catalog[i].methodCount;
            return g_delegate_method_catalog[i].methods;
        }
    count = 0;
    return {};
}

static int32_t dn2cpp_meta_emitted_token(const Dn2CppMetaMember* d, const Dn2CppTypeInfo* declaring)
{
    int32_t count;
    const auto methods = dn2cpp_delegate_private_methods(declaring, count);
    for (int32_t i = 0; i < count; ++i)
    {
        const Dn2CppMethodInfo row = *methods[i];
        if (std::strcmp(row.name, d->methodName) != 0 || row.paramCount != d->paramCount
            || row.genericParamCount != d->genericArity || row.returnType != d->retType
            || (row.attrs & DN2CPP_MTHA_STATIC) != (d->attrs & DN2CPP_MTHA_STATIC))
            continue;
        bool same = true;
        for (int32_t p = 0; p < row.paramCount; ++p)
            if (row.parameters[p]->paramType != d->params[p].paramType)
                same = false;
        if (same)
            return row.metadataToken;
    }
    return 0;
}

template<class ArgAt>
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_delegate_find_method(
    const Dn2CppTypeInfo* owner, int32_t token, int32_t argc, ArgAt argAt)
{
    dn2cpp_require_metadata(owner);
    if (const auto ordinary = dn2cpp_find_method_instantiation(owner->reflection(), token, argc, argAt))
        return ordinary;
    int32_t count;
    const auto methods = dn2cpp_delegate_private_methods(owner, count);
    for (int32_t i = 0; i < count; ++i)
    {
        const auto handle = methods[i];
        const Dn2CppMethodInfo row = *handle;
        if (row.metadataToken != token || !dn2cpp_row_args_equal(row, argc, argAt))
            continue;
        // Runtime descriptors and their public handles share one identity.
        for (int32_t j = 0; j < g_meta_member_count; ++j)
        {
            const auto* d = &g_meta_members[j];
            if (dn2cpp_meta_declares(owner, d) && std::strcmp(row.name, d->methodName) == 0
                && row.paramCount == d->paramCount && row.genericParamCount == d->genericArity)
                return dn2cpp_meta_row(d, owner, argc == 0 ? nullptr : row.genericArgs, argc);
        }
        return handle;
    }
    return {};
}

static bool dn2cpp_row_params_equal(const Dn2CppMethodInfo& a, const Dn2CppMethodInfo& b)
{
    if (a.paramCount != b.paramCount)
        return false;
    for (int32_t p = 0; p < a.paramCount; p++)
        if (a.parameters[p]->paramType != b.parameters[p]->paramType)
            return false;
    return true;
}

// `name` is `member` itself or an explicit implementation "Qualifier.member"; the
// qualifier is returned through `qualifierLength` (0 for the plain form).
static bool dn2cpp_names_member(const char* name, const char* member, size_t memberLength,
                                size_t* qualifierLength)
{
    size_t length = std::strlen(name);
    *qualifierLength = 0;
    if (length == memberLength)
        return std::memcmp(name, member, memberLength) == 0;
    if (length <= memberLength + 1 || name[length - memberLength - 1] != '.'
        || std::memcmp(name + length - memberLength, member, memberLength) != 0)
        return false;
    *qualifierLength = length - memberLength - 1;
    return true;
}

// Whether an explicit implementation's qualifier ("Ns.Outer<A>.IItf<B, C>") names
// `itf`: its last segment has the definition's simple name and as many top-level
// type arguments as the definition's own arity — the transpiler's own test.
static bool dn2cpp_qualifier_names(const char* qualifier, size_t qualifierLength,
                                   const Dn2CppTypeInfo* itf)
{
    const char* full = itf->genericDef != nullptr ? itf->genericDef->name : itf->name;
    if (full == nullptr)
        return false;
    const char* simple = full;
    for (const char* c = full; *c != '\0'; c++)
        if (*c == '.' || *c == '+')
            simple = c + 1;
    const size_t simpleLength = std::strcspn(simple, "`[");
    int32_t arity = 0;
    if (simple[simpleLength] == '`')
        for (const char* c = simple + simpleLength + 1; *c >= '0' && *c <= '9'; c++)
            arity = arity * 10 + (*c - '0');
    size_t segment = 0;
    int32_t depth = 0;
    for (size_t i = 0; i < qualifierLength; i++)
    {
        const char ch = qualifier[i];
        if (ch == '<' || ch == '(' || ch == '[')
            depth++;
        else if (ch == '>' || ch == ')' || ch == ']')
            depth--;
        else if (ch == '.' && depth == 0)
            segment = i + 1;
    }
    const char* last = qualifier + segment;
    const size_t lastLength = qualifierLength - segment;
    size_t nameLength = 0;
    while (nameLength < lastLength && last[nameLength] != '<')
        nameLength++;
    if (simpleLength == 0 || nameLength != simpleLength
        || std::memcmp(last, simple, simpleLength) != 0)
        return false;
    int32_t args = 0;
    if (nameLength < lastLength)
    {
        args = 1;
        depth = 0;
        for (size_t i = nameLength + 1; i < lastLength; i++)
        {
            const char ch = last[i];
            if (ch == '<' || ch == '(' || ch == '[')
                depth++;
            else if (ch == '>' || ch == ')' || ch == ']')
                depth--;
            else if (ch == ',' && depth == 0)
                args++;
        }
    }
    return args == arity;
}

// The class override of vtable slot `slot` bound for `receiver`, searched below
// `owner`. Slots are chain-consistent, so the first row on the slot is the
// override. A stripped level is passed only when its vtable provably inherits the
// slot (the strip keeps vtables); otherwise its missing rows could hide the answer.
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_delegate_class_virtual_target(
    const Dn2CppTypeInfo* receiver, const Dn2CppTypeInfo* owner, int32_t slot)
{
    for (const Dn2CppTypeInfo* ti = receiver; ti != nullptr && ti != owner; ti = ti->base)
    {
        if ((ti->flags & DN2CPP_TF_METADATA_STRIPPED) != 0)
        {
            if (ti->vtable != nullptr && ti->base != nullptr && ti->base->vtable != nullptr
                && ti->vtable[slot] == ti->base->vtable[slot])
                continue;
            dn2cpp_require_metadata(ti);
        }
        const auto reflection = ti->reflection();
        for (int32_t i = 0; i < reflection.methodCount; i++)
        {
            const auto handle = reflection.methods[i];
            if (handle->vtableSlot == slot)
                return handle;
        }
    }
    return {};
}

// The receiver's own level of a template level's definition: the clone level, or
// the image's type-info for a level the clone interned onto. Null when the receiver
// derives from no level of that definition.
static const Dn2CppTypeInfo* dn2cpp_clone_level(const Dn2CppTypeInfo* receiver, const Dn2CppTypeInfo* level)
{
    const Dn2CppTypeInfo* own = receiver;
    while (own != nullptr && own->genericDef != level->genericDef)
        own = own->base;
    return own;
}

// A template level's method row as the receiver's own level of that definition
// declares it: the clone's row, the image's row for an interned level, or an
// interned delta when the image's level has no such instantiation.
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_clone_level_method(
    const Dn2CppTypeInfo* receiver, const Dn2CppTypeInfo* level,
    Dn2CppMetadataHandle<Dn2CppMethodInfo> row, int32_t argc, const Dn2CppTypeInfo* const* args)
{
    const Dn2CppTypeInfo* own = dn2cpp_clone_level(receiver, level);
    if (own == nullptr || !row)
        return row;
    dn2cpp_require_metadata(own);
    if (const auto hit = dn2cpp_find_method_instantiation(own->reflection(), row->metadataToken,
            argc, [args](int32_t arg) { return args[arg]; }))
        return hit;
    struct LevelRow
    {
        Dn2CppMethodDelta delta;
        LevelRow* next;
    };
    static LevelRow* rows = nullptr;
    std::lock_guard<std::mutex> lock(g_synth_mtx);
    for (LevelRow* r = rows; r != nullptr; r = r->next)
        if (r->delta.original.identity() == row.identity() && r->delta.declaringType == own)
            return Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_raw(&r->delta);
    auto* r = new LevelRow{};
    r->delta.original = row;
    r->delta.declaringType = own;
    r->next = rows;
    rows = r;
    return Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_raw(&r->delta);
}

// The nearest level at or above `ti` the base image emitted; a patch type's base
// chain always reaches one.
static const Dn2CppTypeInfo* dn2cpp_aot_level(const Dn2CppTypeInfo* ti)
{
    while ((ti->flags & DN2CPP_TF_PATCH) != 0)
        ti = ti->base;
    return ti;
}

// The receiver a recorded target names for `receiver`, as the emitted dispatcher
// steps to it: a clone's template level, and for a generic virtual's cases, which
// a patch type never overrides, a patch type's nearest AOT ancestor. A patch
// type's own interface map may replace a plain interface slot's body.
static const Dn2CppTypeInfo* dn2cpp_recorded_receiver(const Dn2CppTypeInfo* receiver, bool gvmCase)
{
    if (gvmCase)
        receiver = dn2cpp_aot_level(receiver);
    return (receiver->flags & DN2CPP_TF_RUNTIME_SYNTH) != 0 ? dn2cpp_runtime_template_of(receiver) : receiver;
}

static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_object_virtual_target(
    const Dn2CppTypeInfo* receiver, const Dn2CppMetaMember* d, bool requireMetadata);

bool dn2cpp_delegate_selected_key(const Dn2CppDelegateMethodIdentity* identity,
    const Dn2CppTypeInfo* receiver, const Dn2CppTypeInfo*& owner, int32_t& token,
    int32_t* selectedArgCount, const Dn2CppTypeInfo* const** selectedArgs)
{
    if (selectedArgCount != nullptr)
        *selectedArgCount = identity->genericArgCount;
    if (selectedArgs != nullptr)
        *selectedArgs = identity->genericArgs;
    owner = identity->declaringType;
    token = identity->metadataToken;
    if (!identity->virtualBinding || receiver == nullptr)
    {
        if (owner != nullptr && receiver != nullptr)
            owner = dn2cpp_invoke_declaring(owner, receiver);
        return owner != nullptr;
    }
    if (owner != nullptr && (owner->flags & DN2CPP_TF_INTERFACE) != 0 && identity->interfaceSlot >= 0)
    {
        if (const auto* selected = dn2cpp_delegate_interface_identity(receiver, owner, identity->interfaceSlot))
        {
            owner = dn2cpp_invoke_declaring(selected->declaringType, receiver);
            token = selected->metadataToken;
            if (selected->method)
            {
                if (selectedArgCount != nullptr)
                    *selectedArgCount = selected->method->genericParamCount;
                if (selectedArgs != nullptr)
                    *selectedArgs = selected->method->genericArgs;
            }
            return owner != nullptr;
        }
        if ((receiver->flags & DN2CPP_TF_PATCH) != 0)
            return false;
    }
    const auto* recorded = dn2cpp_recorded_receiver(receiver, identity->genericArgCount != 0);
    for (int32_t i = 0; i < identity->selectedTargetCount; ++i)
    {
        const auto& target = identity->selectedTargets[i];
        if (target.receiverType != nullptr && target.receiverType == recorded)
        {
            owner = dn2cpp_invoke_declaring(target.declaringType, receiver);
            token = target.metadataToken;
            return owner != nullptr;
        }
    }
    if ((receiver->flags & DN2CPP_TF_PATCH) == 0)
        for (int32_t i = 0; i < identity->selectedTargetCount; ++i)
        {
            const auto& target = identity->selectedTargets[i];
            if (!target.receiverFamily)
                continue;
            bool matches = target.receiverType == nullptr && (receiver->flags & DN2CPP_TF_ARRAY) != 0;
            for (const auto* level = receiver; !matches && level != nullptr; level = level->base)
                matches = target.receiverType == level;
            if (matches)
            {
                owner = dn2cpp_invoke_declaring(target.declaringType, receiver);
                token = target.metadataToken;
                return owner != nullptr;
            }
        }
    // Runtime allocations need not have an emitted receiver case. Object-family
    // declaration tokens can still select an exact override from retained rows.
    if (owner != nullptr && token != 0 && (owner->flags & DN2CPP_TF_METADATA_STRIPPED) == 0)
        for (int32_t i = 0; i < g_meta_member_count; ++i)
        {
            const auto* descriptor = &g_meta_members[i];
            if ((descriptor->ilAttrs & DN2CPP_MA_VIRTUAL) == 0 || !dn2cpp_meta_declares(owner, descriptor)
                || (std::strcmp(descriptor->typeName, "System.Object") != 0
                    && std::strcmp(descriptor->typeName, "System.ValueType") != 0)
                || dn2cpp_meta_emitted_token(descriptor, owner) != token)
                continue;
            const auto selected = dn2cpp_object_virtual_target(receiver, descriptor, false);
            if (!selected)
                return false;
            owner = dn2cpp_invoke_declaring(selected->declaringType, receiver);
            token = selected->metadataToken;
            return owner != nullptr && token != 0;
        }
    return false;
}

// The emitter's selected method for an interface or GVM binding over `receiver`, or
// {} when no case names the receiver.
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_delegate_recorded_case(
    const Dn2CppTypeInfo* receiver, const Dn2CppDelegateMethodIdentity* identity)
{
    const Dn2CppTypeInfo* recorded = dn2cpp_recorded_receiver(receiver, identity->genericArgCount != 0);
    for (int32_t i = 0; i < identity->targetCount; i++)
    {
        const auto& target = identity->targets[i];
        if (target.receiverType != recorded)
            continue;
        dn2cpp_require_metadata(target.declaringType);
        const auto hit = dn2cpp_delegate_find_method(target.declaringType,
            target.metadataToken, identity->genericArgCount,
            [identity](int32_t arg) { return identity->genericArgs[arg]; });
        if ((target.declaringType->flags & DN2CPP_TF_RUNTIME_TEMPLATE) != 0)
            return dn2cpp_clone_level_method(receiver, target.declaringType, hit,
                identity->genericArgCount, identity->genericArgs);
        return hit;
    }
    return {};
}

// An unrecorded class GVM still needs metadata for each receiver level.
static void dn2cpp_require_unrecorded_levels(const Dn2CppTypeInfo* receiver, const Dn2CppTypeInfo* owner)
{
    if ((owner->flags & DN2CPP_TF_INTERFACE) == 0)
        for (const Dn2CppTypeInfo* ti = receiver; ti != nullptr && ti != owner; ti = ti->base)
            dn2cpp_require_metadata(ti);
}

// The recorded case, else {} once dn2cpp_require_unrecorded_levels passes.
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_delegate_recorded_target(
    const Dn2CppTypeInfo* receiver, const Dn2CppTypeInfo* owner,
    const Dn2CppDelegateMethodIdentity* identity)
{
    if (const auto hit = dn2cpp_delegate_recorded_case(receiver, identity))
        return hit;
    dn2cpp_require_unrecorded_levels(receiver, owner);
    return {};
}

// The emitted filler of slot `key` of `slotOwner` on instances of exactly `type`, or
// null when its name names the slot's member (Dn2CppRenamedSlotBody).
static const Dn2CppRenamedSlotBody* dn2cpp_renamed_slot_body_of(const Dn2CppTypeInfo* type,
    const Dn2CppTypeInfo* slotOwner, int32_t key)
{
    for (int32_t i = 0; i < dn2cpp_renamed_slot_body_count; i++)
    {
        const Dn2CppRenamedSlotBody& row = dn2cpp_renamed_slot_bodies[i];
        if (row.type == type && row.slotOwner == slotOwner && row.slotKey == key)
            return &row;
    }
    return nullptr;
}

// That filler's method row, or {} when there is none or its row was not emitted. A
// MakeGenericType clone shares its template's slots, so the template's rows name its
// fillers, each answering as the clone's own level of the filler's definition.
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_renamed_slot_method(const Dn2CppTypeInfo* type,
    const Dn2CppTypeInfo* slotOwner, int32_t key, int32_t slot)
{
    const Dn2CppTypeInfo* recorded = dn2cpp_recorded_receiver(type, false);
    if ((recorded->flags & DN2CPP_TF_PATCH) != 0)
    {
        // A patch can replace an interface body independently of its class slots.
        // Only inherited dispatch can use the nearest AOT level's renamed row.
        const Dn2CppTypeInfo* aot = dn2cpp_aot_level(recorded);
        const void** own = dn2cpp_try_resolve_interface(recorded, slotOwner);
        const void** inherited = dn2cpp_try_resolve_interface(aot, slotOwner);
        if (slot < 0 || own == nullptr || inherited == nullptr || own[slot] != inherited[slot])
            return {};
        recorded = dn2cpp_recorded_receiver(aot, false);
    }
    const Dn2CppRenamedSlotBody* row = dn2cpp_renamed_slot_body_of(recorded, slotOwner, key);
    if (row == nullptr)
        return {};
    dn2cpp_require_metadata(row->declaringType);
    const auto hit = dn2cpp_delegate_find_method(row->declaringType, row->metadataToken, 0,
        [](int32_t) -> const Dn2CppTypeInfo* { return nullptr; });
    if ((row->declaringType->flags & DN2CPP_TF_RUNTIME_TEMPLATE) != 0)
        return dn2cpp_clone_level_method(type, row->declaringType, hit, 0, nullptr);
    return hit;
}

static bool dn2cpp_interface_rows_contain(const Dn2CppTypeInfo* type, const Dn2CppTypeInfo* itf)
{
    int32_t count;
    const auto* entries = dn2cpp_type_interfaces(type, &count);
    for (int32_t i = 0; i < count; i++)
        if (entries[i].itf == itf)
            return true;
    return false;
}

// A derived interface's MethodImpl whose body a reference receiver's slot holds.
// Interface rows are transitive, so one flat pass over the receiver's rows finds
// every interface deriving from `owner`; only those need metadata. The answer is
// the candidate no other candidate's interface derives from.
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_delegate_derived_interface_target(
    const Dn2CppTypeInfo* receiver, const Dn2CppTypeInfo* owner,
    const Dn2CppMethodInfo& decl, void* bound)
{
    struct Candidate
    {
        const Dn2CppTypeInfo* itf;
        Dn2CppMetadataHandle<Dn2CppMethodInfo> method;
    };
    std::vector<Candidate> candidates;
    const size_t nameLength = std::strlen(decl.name);
    size_t qualifierLength = 0;
    int32_t count;
    const auto* entries = dn2cpp_type_interfaces(receiver, &count);
    for (int32_t i = 0; i < count; i++)
    {
        const Dn2CppTypeInfo* derived = entries[i].itf;
        if (derived == nullptr || derived == owner || (derived->flags & DN2CPP_TF_SHARED_CANON) != 0
            || !dn2cpp_interface_rows_contain(derived, owner))
            continue;
        dn2cpp_require_metadata(derived);
        const auto reflection = derived->reflection();
        for (int32_t j = 0; j < reflection.methodCount; j++)
        {
            const auto handle = reflection.methods[j];
            const auto row = handle.operator->();
            if (row->fnPtr == bound
                && dn2cpp_names_member(row->name, decl.name, nameLength, &qualifierLength)
                && qualifierLength != 0
                && dn2cpp_qualifier_names(row->name, qualifierLength, owner)
                && dn2cpp_row_params_equal(*row.operator->(), decl))
            {
                candidates.push_back({derived, handle});
                break;
            }
        }
    }
    Dn2CppMetadataHandle<Dn2CppMethodInfo> selected{};
    for (const auto& candidate : candidates)
    {
        bool shadowed = false;
        for (const auto& other : candidates)
            if (other.itf != candidate.itf && dn2cpp_interface_rows_contain(other.itf, candidate.itf))
            {
                shadowed = true;
                break;
            }
        if (shadowed)
            continue;
        if (selected)
            return {};
        selected = candidate.method;
    }
    return selected;
}

static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_delegate_interface_provenance(
    const Dn2CppTypeInfo* receiver, const Dn2CppTypeInfo* owner, int32_t slot,
    int32_t argc, const Dn2CppTypeInfo* const* args)
{
    const auto* selected = dn2cpp_delegate_interface_identity(receiver, owner, slot);
    if (selected == nullptr)
        return {};
    const auto& target = *selected;
    if (target.declaringType == nullptr)
        return {};
    dn2cpp_require_metadata(target.declaringType);
    if (target.method)
        return target.method;
    const auto hit = dn2cpp_delegate_find_method(target.declaringType, target.metadataToken, argc,
        [args](int32_t i) { return args[i]; });
    return (target.declaringType->flags & DN2CPP_TF_RUNTIME_TEMPLATE) != 0 && hit
        ? dn2cpp_clone_level_method(receiver, target.declaringType, hit, argc, args) : hit;
}

// The implementation an interface binding reached for a receiver without a
// recorded case, or {} to answer the declaration (Invoke re-dispatches an
// interface row through the receiver's table).
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_delegate_interface_target(
    const Dn2CppTypeInfo* receiver, const Dn2CppTypeInfo* owner,
    const Dn2CppMethodInfo& decl, const Dn2CppDelegateMethodIdentity* identity, void* bound)
{
    const auto argAt = [identity](int32_t i) { return identity->genericArgs[i]; };
    const bool gvm = decl.genericParamCount != 0;
    if (!gvm)
        if (const auto exact = dn2cpp_delegate_interface_provenance(receiver, owner, decl.vtableSlot,
                identity->genericArgCount, identity->genericArgs))
            return exact;
    // A variant instantiation's slot runs the row of the instantiation serving it.
    if (!gvm)
        if (const auto renamed = dn2cpp_renamed_slot_method(receiver,
                dn2cpp_dispatch_interface_of(receiver, owner), decl.metadataToken, decl.vtableSlot))
            return renamed;
    const size_t nameLength = std::strlen(decl.name);
    size_t qualifierLength = 0;
    // A reference receiver's slot holds the implementation's own symbol. The name
    // guard keeps a folded identical body (MSVC /OPT:ICF) from answering; parameter
    // types may differ from the declaration's under variance. Class bodies precede
    // interface overrides, which precede the declaration's own default body.
    if (!gvm && (receiver->flags & DN2CPP_TF_VALUETYPE) == 0)
    {
        for (const Dn2CppTypeInfo* ti = receiver; ti != nullptr; ti = ti->base)
        {
            dn2cpp_require_metadata(ti);
            const auto reflection = ti->reflection();
            for (int32_t i = 0; i < reflection.methodCount; i++)
            {
                const auto handle = reflection.methods[i];
                const auto row = handle.operator->();
                if ((row->attrs & DN2CPP_MTHA_STATIC) == 0 && row->fnPtr == bound
                    && row->paramCount == decl.paramCount
                    && dn2cpp_names_member(row->name, decl.name, nameLength, &qualifierLength))
                    return handle;
            }
        }
        if (const auto derived = dn2cpp_delegate_derived_interface_target(receiver, owner, decl, bound))
            return derived;
    }
    if (decl.fnPtr != nullptr && decl.fnPtr == bound)
        return {};
    // Value-type unboxing thunks, NFI-erasing thunks and generic-virtual dispatchers
    // hide the implementation's address: resolve by name. Interface rows are
    // transitive, so which class level lists the interface is invisible here; the
    // orders below are exact for one implementing level, and the emitter records
    // the selected body for the class receivers it allocates. Plain matches (1)
    // need a body; explicit ones (2) must name the owner.
    const auto kindOf = [&](const Dn2CppMethodInfo& row) -> int {
        if ((row.attrs & DN2CPP_MTHA_STATIC) != 0
            || !dn2cpp_names_member(row.name, decl.name, nameLength, &qualifierLength)
            || !dn2cpp_row_args_equal(row, identity->genericArgCount, argAt)
            || !dn2cpp_row_params_equal(row, decl))
            return 0;
        if (qualifierLength == 0)
            return row.fnPtr != nullptr ? 1 : 0;
        return dn2cpp_qualifier_names(row.name, qualifierLength, owner) ? 2 : 0;
    };
    // One level's candidate, explicit preferred; false when two explicit
    // candidates make the level ambiguous.
    const auto levelHit = [&](const Dn2CppTypeInfo* ti, bool explicitOnly,
                              Dn2CppMetadataHandle<Dn2CppMethodInfo>* hit) -> bool {
        const auto reflection = ti->reflection();
        Dn2CppMetadataHandle<Dn2CppMethodInfo> explicitHit{};
        Dn2CppMetadataHandle<Dn2CppMethodInfo> plainHit{};
        for (int32_t i = 0; i < reflection.methodCount; i++)
        {
            const auto handle = reflection.methods[i];
            const auto row = handle.operator->();
            int kind = kindOf(*row.operator->());
            if (kind == 2)
            {
                if (explicitHit)
                    return false;
                explicitHit = handle;
            }
            else if (kind == 1 && !explicitOnly && !plainHit)
                plainHit = handle;
        }
        *hit = explicitHit ? explicitHit : plainHit;
        return true;
    };
    Dn2CppMetadataHandle<Dn2CppMethodInfo> hit{};
    if (gvm)
    {
        // The first level with any implementation wins.
        for (const Dn2CppTypeInfo* ti = receiver; ti != nullptr; ti = ti->base)
        {
            dn2cpp_require_metadata(ti);
            if (!levelHit(ti, false, &hit))
                return {};
            if (hit)
                return hit;
        }
        return {};
    }
    // An explicit implementation anywhere in the chain beats a plain one.
    for (const Dn2CppTypeInfo* ti = receiver; ti != nullptr; ti = ti->base)
    {
        dn2cpp_require_metadata(ti);
        if (!levelHit(ti, true, &hit))
            return {};
        if (hit)
            return hit;
    }
    for (const Dn2CppTypeInfo* ti = receiver; ti != nullptr; ti = ti->base)
    {
        levelHit(ti, false, &hit);
        if (hit)
            return hit;
    }
    return {};
}

static const Dn2CppObjectVirtualInheritance* g_object_virtual_inheritance = nullptr;
static int32_t g_object_virtual_inheritance_count = 0;

void dn2cpp_set_object_virtual_inheritance(const Dn2CppObjectVirtualInheritance* rows, int32_t count)
{
    g_object_virtual_inheritance = rows;
    g_object_virtual_inheritance_count = count;
}

// The method a callvirt of Object or ValueType virtual `d` runs on a `receiver`: the
// most derived row overriding it, else the ValueType or Object row. A patch receiver
// runs its nearest AOT ancestor's, since a patch type overrides no Object virtual and
// copies its base's dispatch fields. A row whose level dn2cpp_object_slot_settles
// accepts is the answer, so no level above it is read. Short of that, null once a
// level dn2cpp_meta_level_passes refuses lies in between, unless raw metadata proves
// it inherits this virtual. A stripped level throws through
// dn2cpp_require_metadata when the answer needs its rows, and is passed when its
// dispatch field shows it inherits the body.
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_object_virtual_target(
    const Dn2CppTypeInfo* receiver, const Dn2CppMetaMember* d, bool requireMetadata = true)
{
    const Dn2CppMetaMember* root = std::strcmp(d->typeName, "System.Object") == 0
        ? d : dn2cpp_meta_object_counterpart(d);
    const Dn2CppTypeInfo* aot = dn2cpp_aot_level(receiver);
    if (!requireMetadata)
        if (const auto* renamed = dn2cpp_renamed_slot_body_of(dn2cpp_recorded_receiver(aot, false),
                &dn2cpp_object_type, dn2cpp_meta_object_virtual_index(root)))
        {
            const auto* owner = renamed->declaringType;
            const auto* own = (owner->flags & DN2CPP_TF_RUNTIME_TEMPLATE) != 0 ? dn2cpp_clone_level(aot, owner) : owner;
            if ((owner->flags & DN2CPP_TF_METADATA_STRIPPED) != 0
                || (own != nullptr && (own->flags & DN2CPP_TF_METADATA_STRIPPED) != 0))
                return {};
        }
    if (const auto renamed = dn2cpp_renamed_slot_method(aot, &dn2cpp_object_type,
            dn2cpp_meta_object_virtual_index(root), dn2cpp_meta_object_virtual_index(root)))
        return renamed;
    Dn2CppMetadataHandle<Dn2CppMethodInfo> found{};
    for (const Dn2CppTypeInfo* ti = aot; ti != nullptr; ti = dn2cpp_meta_next_level(ti))
    {
        // Equality must answer without executing a metadata refusal. An unknown
        // stripped level is not proven transparent by equal dispatch addresses.
        if (!requireMetadata && (ti->flags & DN2CPP_TF_METADATA_STRIPPED) != 0)
            return {};
        if ((ti->flags & DN2CPP_TF_METADATA_STRIPPED) == 0)
        {
            int32_t count;
            const auto methods = dn2cpp_delegate_private_methods(ti, count);
            for (int32_t i = 0; i < count; ++i)
            {
                const Dn2CppMethodInfo row = *methods[i];
                if (dn2cpp_meta_object_virtual_named(row) == root
                    && (row.ilAttrs & (DN2CPP_MA_NEWSLOT | DN2CPP_MA_ABSTRACT)) == 0)
                    return found ? found : methods[i];
            }
        }
        if (!dn2cpp_meta_level_passes(ti))
        {
            if ((ti->flags & DN2CPP_TF_METADATA_STRIPPED) == 0)
            {
                const int32_t slot = dn2cpp_meta_object_virtual_index(root);
                bool inherits = false;
                for (int32_t i = 0; i < g_object_virtual_inheritance_count; i++)
                    if (g_object_virtual_inheritance[i].type == ti && slot >= 0
                        && (g_object_virtual_inheritance[i].inheritedMask & (1 << slot)) != 0)
                        inherits = true;
                if (!inherits)
                    return {};
                continue;
            }
            if (found || dn2cpp_object_slot_overridden(ti, root) != 0)
                dn2cpp_require_metadata(ti);
            continue;
        }
        for (int32_t k = 0; k < g_meta_member_count; k++)
        {
            const Dn2CppMetaMember* own = &g_meta_members[k];
            if ((own->ilAttrs & DN2CPP_MA_VIRTUAL) != 0 && dn2cpp_meta_declares(ti, own)
                && dn2cpp_meta_same_member(own, d))
                return found ? found : dn2cpp_meta_row(own, ti, nullptr, 0);
        }
        // Rows below a new slot of the member override that slot, not this member.
        const auto reflection = ti->reflection();
        bool foundHere = false;
        for (int32_t i = 0; i < reflection.methodCount; i++)
        {
            const Dn2CppMethodInfo row = *reflection.methods[i];
            if (dn2cpp_meta_object_virtual_named(row) != root)
                continue;
            if ((row.ilAttrs & DN2CPP_MA_NEWSLOT) != 0)
            {
                found = {};
                foundHere = false;
            }
            else if (!found && (row.ilAttrs & DN2CPP_MA_ABSTRACT) == 0)
            {
                found = reflection.methods[i];
                foundHere = true;
            }
        }
        if (requireMetadata && foundHere && dn2cpp_object_slot_settles(ti, root, *found))
            return found;
    }
    return {};
}

// Object's address loads bind either its virtual dispatch or its own body.
static const Dn2CppMetaMember* dn2cpp_object_dispatch_member(const void* fn, bool virtualBinding)
{
    const char* name = virtualBinding
        ? fn == reinterpret_cast<const void*>(&dn2cpp_object_tostring_virtual) ? "ToString"
            : fn == reinterpret_cast<const void*>(&dn2cpp_object_equals_virtual) ? "Equals"
            : fn == reinterpret_cast<const void*>(&dn2cpp_object_gethashcode) ? "GetHashCode" : nullptr
        : fn == reinterpret_cast<const void*>(&dn2cpp_object_tostring_nonvirtual) ? "ToString"
            : fn == reinterpret_cast<const void*>(&dn2cpp_object_equals_nonvirtual) ? "Equals"
            : fn == reinterpret_cast<const void*>(&dn2cpp_object_hashcode) ? "GetHashCode" : nullptr;
    for (int32_t k = 0; name != nullptr && k < g_meta_member_count; k++)
    {
        const Dn2CppMetaMember* d = &g_meta_members[k];
        if ((d->ilAttrs & DN2CPP_MA_VIRTUAL) != 0 && std::strcmp(d->typeName, "System.Object") == 0
            && std::strcmp(d->methodName, name) == 0)
            return d;
    }
    return nullptr;
}

static std::atomic<Dn2CppPatchVirtualMethodResolver> g_patch_virtual_method_resolver{nullptr};

void dn2cpp_set_patch_virtual_method_resolver(Dn2CppPatchVirtualMethodResolver resolver)
{
    g_patch_virtual_method_resolver.store(resolver, std::memory_order_release);
}

// The method a reflection-bound delegate reports. A closed instance binding of a
// virtual row names the body its receiver's slot runs; an open binding names the
// row, as .NET's Delegate.Method does.
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_reflbind_method(const Dn2CppReflBind* bind)
{
    const Dn2CppMetadataHandle<Dn2CppMethodInfo> mi = bind->method;
    if (bind->mode != DN2CPP_DGBIND_CLOSED_INSTANCE || bind->target == nullptr)
        return mi;
    const Dn2CppMethodInfo decl = *mi;
    const Dn2CppTypeInfo* owner = decl.declaringType;
    const Dn2CppTypeInfo* receiver = bind->target->type;
    // An Object or ValueType virtual names the override its receiver runs, or null.
    if ((decl.attrs & DN2CPP_MTHA_METAANSWER) != 0)
    {
        const Dn2CppMetaMember* d = dn2cpp_meta_desc_of(mi);
        if (d == nullptr || !d->gated || (d->ilAttrs & DN2CPP_MA_VIRTUAL) == 0)
            return mi;
        return dn2cpp_object_virtual_target(receiver, d);
    }
    // A generic virtual row names the body its dispatcher selects for the receiver.
    // An emitted receiver without a recorded case runs the row's own body unless the
    // dispatcher strips an override (dn2cpp_reflbind_gvm_body); a patch or runtime
    // receiver needs metadata for each level.
    if (decl.genericParamCount != 0)
    {
        const Dn2CppGvmRowDispatch* dispatch = dn2cpp_gvm_row_dispatch_of(decl);
        if (dispatch == nullptr)
            return mi;
        if (const auto hit = dn2cpp_delegate_recorded_case(receiver, &dispatch->identity))
            return hit;
        if (dispatch->strips || (receiver->flags & (DN2CPP_TF_PATCH | DN2CPP_TF_RUNTIME_SYNTH)) != 0)
            dn2cpp_require_unrecorded_levels(receiver, owner);
        return mi;
    }
    if (decl.vtableSlot < 0)
        return mi;
    Dn2CppMetadataHandle<Dn2CppMethodInfo> hit{};
    if ((owner->flags & DN2CPP_TF_INTERFACE) != 0)
    {
        const void** slots = dn2cpp_try_resolve_interface(receiver, owner);
        if (slots == nullptr || (decl.ilAttrs & DN2CPP_MA_VIRTUAL) == 0)
            return mi;
        const Dn2CppDelegateMethodIdentity identity{ owner, decl.metadataToken, 0, nullptr, true, 0, nullptr };
        hit = dn2cpp_delegate_interface_target(receiver, owner, decl, &identity,
            const_cast<void*>(slots[decl.vtableSlot]));
    }
    else if ((owner->flags & DN2CPP_TF_PATCH) != 0)
    {
        const auto resolver = g_patch_virtual_method_resolver.load(std::memory_order_acquire);
        if (resolver == nullptr)
            dn2cpp_throw_platform_not_supported("Delegate.Method: patch virtual method identity is not available");
        hit = resolver(receiver, decl.vtableSlot, true);
    }
    else if ((owner->flags & DN2CPP_TF_VALUETYPE) == 0)
        hit = dn2cpp_delegate_class_virtual_target(receiver, owner, decl.vtableSlot);
    return hit ? hit : mi;
}

// Whether the member tables dn2cpp_reflbind_method reads for `receiver` are all
// present: --trim-reflection stripped none of its levels or their interfaces.
static bool dn2cpp_reflbind_receiver_described(const Dn2CppTypeInfo* receiver)
{
    for (const Dn2CppTypeInfo* ti = receiver; ti != nullptr; ti = ti->base)
    {
        if ((ti->flags & DN2CPP_TF_METADATA_STRIPPED) != 0)
            return false;
        int32_t count;
        const auto* entries = dn2cpp_type_interfaces(ti, &count);
        for (int32_t i = 0; i < count; i++)
            if ((entries[i].itf->flags & DN2CPP_TF_METADATA_STRIPPED) != 0)
                return false;
    }
    return true;
}

// The table a binding's slot indexes (Dn2CppReflBind::slotKind), null for a class
// vtable slot.
static const Dn2CppTypeInfo* dn2cpp_reflbind_slot_table(const Dn2CppReflBind* bind)
{
    if (bind->slotKind == DN2CPP_DGBIND_SLOT_OBJECT)
        return &dn2cpp_object_type;
    if (bind->slotKind == DN2CPP_DGBIND_SLOT_INTERFACE)
        return dn2cpp_dispatch_interface_of(bind->target->type, bind->declaring);
    return nullptr;
}

// The class vtable slot a closed binding runs on its receiver: the slot a class row
// names, or the one the emitted implementation slots map an interface slot or an
// Object member to. -1 when none answers.
static int32_t dn2cpp_reflbind_class_slot(const Dn2CppReflBind* bind)
{
    if (bind->slot < 0)
        return -1;
    if (bind->slotKind == DN2CPP_DGBIND_SLOT_CLASS)
        return bind->slot;
    // A filler whose name differs from the Object member it fills occupies its own
    // class slot as well, which bindings through its row name. A clone's vtable is
    // its template's.
    const bool objectSlot = bind->slotKind == DN2CPP_DGBIND_SLOT_OBJECT;
    if (objectSlot)
        if (const Dn2CppRenamedSlotBody* renamed = dn2cpp_renamed_slot_body_of(
                dn2cpp_recorded_receiver(bind->target->type, false), &dn2cpp_object_type, bind->slot);
            renamed != nullptr && renamed->classSlot >= 0)
            return renamed->classSlot;
    static std::once_flag& once = dn2cpp_never_destroyed<std::once_flag>();
    static std::unordered_map<const Dn2CppTypeInfo*, std::vector<const Dn2CppItfImplSlots*>>* byType;
    std::call_once(once, [] {
        byType = new std::unordered_map<const Dn2CppTypeInfo*, std::vector<const Dn2CppItfImplSlots*>>();
        for (int32_t i = 0; i < dn2cpp_itf_impl_slot_count; i++)
            (*byType)[dn2cpp_itf_impl_slots[i].type].push_back(&dn2cpp_itf_impl_slots[i]);
    });
    const auto found = byType->find(objectSlot ? nullptr : bind->target->type);
    if (found == byType->end())
        return -1;
    const Dn2CppTypeInfo* table = dn2cpp_reflbind_slot_table(bind);
    for (const Dn2CppItfImplSlots* row : found->second)
        if (row->itf == table)
            return bind->slot < row->count ? row->slots[bind->slot] : -1;
    return -1;
}

// The body a closed binding of generic virtual `row` runs on `receiver`, known
// without member metadata: the case the row's dispatcher records for the receiver,
// else the row's own body wherever a callvirt runs it. False when neither holds.
// A clone's case names its template level, which answers as the clone's own level
// of that definition, the declaring type of the clone's rows.
static bool dn2cpp_reflbind_gvm_body(const Dn2CppMethodInfo& row, const Dn2CppTypeInfo* receiver,
    const Dn2CppTypeInfo** owner, int32_t* token)
{
    *owner = row.declaringType;
    *token = row.metadataToken;
    if (!dn2cpp_gvm_row_dispatched(row))
        return true;
    const Dn2CppGvmRowDispatch* dispatch = dn2cpp_gvm_row_dispatch_of(row);
    if (dispatch == nullptr)
        return false;
    const Dn2CppTypeInfo* recorded = dn2cpp_recorded_receiver(receiver, true);
    for (int32_t i = 0; i < dispatch->identity.targetCount; i++)
        if (dispatch->identity.targets[i].receiverType == recorded)
        {
            const Dn2CppTypeInfo* declaring = dispatch->identity.targets[i].declaringType;
            const Dn2CppTypeInfo* own = (declaring->flags & DN2CPP_TF_RUNTIME_TEMPLATE) != 0
                ? dn2cpp_clone_level(receiver, declaring)
                : nullptr;
            *owner = own != nullptr ? own : declaring;
            *token = dispatch->identity.targets[i].metadataToken;
            return true;
        }
    return !dispatch->strips;
}

// Whether closed bindings of two generic virtual rows over one receiver run one
// method instantiation: 1 or 0, or -1 when a row is not a generic virtual one or
// its body is unknown.
static int32_t dn2cpp_reflbind_gvm_same_body(const Dn2CppReflBind* a, const Dn2CppReflBind* b)
{
    const Dn2CppMethodInfo rowA = *a->method;
    const Dn2CppMethodInfo rowB = *b->method;
    if (!dn2cpp_is_gvm_row(rowA) || !dn2cpp_is_gvm_row(rowB))
        return -1;
    const Dn2CppTypeInfo* ownerA;
    const Dn2CppTypeInfo* ownerB;
    int32_t tokenA;
    int32_t tokenB;
    if (!dn2cpp_reflbind_gvm_body(rowA, a->target->type, &ownerA, &tokenA)
        || !dn2cpp_reflbind_gvm_body(rowB, b->target->type, &ownerB, &tokenB))
        return -1;
    if (ownerA != ownerB || tokenA != tokenB || rowA.genericParamCount != rowB.genericParamCount)
        return 0;
    for (int32_t i = 0; i < rowA.genericParamCount; i++)
        if (rowA.genericArgs[i] != rowB.genericArgs[i])
            return 0;
    return 1;
}

bool dn2cpp_reflbind_selected_key(const Dn2CppReflBind* bind,
    const Dn2CppTypeInfo*& owner, int32_t& token, int32_t& genericArgCount,
    const Dn2CppTypeInfo* const*& genericArgs)
{
    Dn2CppMethodInfo row = *bind->method;
    const bool isStatic = (row.attrs & DN2CPP_MTHA_STATIC) != 0;
    if (bind->mode != (isStatic ? DN2CPP_DGBIND_OPEN_STATIC : DN2CPP_DGBIND_CLOSED_INSTANCE)
        || bind->fault != DN2CPP_DGBIND_FAULT_NONE)
        return false;
    owner = dn2cpp_invoke_declaring(row.declaringType, bind->declaring);
    token = row.metadataToken;
    genericArgCount = row.genericParamCount;
    genericArgs = row.genericArgs;
    if (genericArgCount != 0 && genericArgs == nullptr)
        return false;
    if (!isStatic && bind->virtualRow != 0 && bind->target != nullptr)
    {
        const auto* receiver = bind->target->type;
        if (bind->slotKind == DN2CPP_DGBIND_SLOT_OBJECT)
        {
            const auto* descriptor = dn2cpp_meta_desc_of(bind->method);
            const auto selected = descriptor != nullptr ? dn2cpp_object_virtual_target(receiver, descriptor, false)
                : Dn2CppMetadataHandle<Dn2CppMethodInfo>{};
            if (!selected)
                return false;
            owner = dn2cpp_invoke_declaring(selected->declaringType, receiver);
            token = selected->metadataToken;
            return owner != nullptr && token != 0;
        }
        if (bind->slotKind == DN2CPP_DGBIND_SLOT_INTERFACE && bind->slot >= 0)
        {
            Dn2CppDelegateMethodIdentity identity{};
            identity.declaringType = row.declaringType;
            identity.metadataToken = row.metadataToken;
            identity.virtualBinding = true;
            identity.interfaceSlot = bind->slot;
            return dn2cpp_delegate_selected_key(&identity, receiver, owner, token, &genericArgCount, &genericArgs);
        }
        if (dn2cpp_is_gvm_row(row))
            return dn2cpp_reflbind_gvm_body(row, receiver, &owner, &token);
        if (row.vtableSlot >= 0)
        {
            Dn2CppMetadataHandle<Dn2CppMethodInfo> selected;
            if ((row.declaringType->flags & DN2CPP_TF_PATCH) != 0)
            {
                const auto resolver = g_patch_virtual_method_resolver.load(std::memory_order_acquire);
                if (resolver == nullptr)
                    return false;
                selected = resolver(receiver, row.vtableSlot, false);
            }
            else
            {
                if (!dn2cpp_reflbind_receiver_described(receiver))
                    return false;
                selected = dn2cpp_reflbind_method(bind);
            }
            if (!selected)
                return false;
            owner = dn2cpp_invoke_declaring(selected->declaringType, receiver);
            token = selected->metadataToken;
        }
    }
    return owner != nullptr && token != 0;
}

// .NET binds a virtual row to its receiver's override, so a binding through the
// declaration and one through the override are one delegate, while different
// non-virtual rows are different methods. Slots and rows decide it, never body
// addresses: a linker folds identical bodies, shared generics run one body for
// several instantiations, and one trap fills every stripped slot of a signature.
// Bindings naming slots of one table bind one method exactly when they name one
// slot, which reads no metadata, and bindings whose slots map to the receiver's
// class slots compare those. Generic virtual rows compare the bodies their
// dispatchers select, which the dispatchers' cases state. Other pairs compare the
// rows they bind; where --trim-reflection stripped a table that comparison reads,
// they are distinct, since equality must answer.
bool dn2cpp_reflbind_same_method(const Dn2CppReflBind* a, const Dn2CppReflBind* b)
{
    if (a->mode != DN2CPP_DGBIND_CLOSED_INSTANCE || a->target == nullptr
        || a->virtualRow == 0 || b->virtualRow == 0)
        return false;
    if (a->slot >= 0 && b->slot >= 0 && a->slotKind == b->slotKind
        && (a->slotKind != DN2CPP_DGBIND_SLOT_INTERFACE
            || dn2cpp_reflbind_slot_table(a) == dn2cpp_reflbind_slot_table(b)))
        return a->slot == b->slot;
    const int32_t classSlotA = dn2cpp_reflbind_class_slot(a);
    const int32_t classSlotB = dn2cpp_reflbind_class_slot(b);
    if (classSlotA >= 0 && classSlotB >= 0)
        return classSlotA == classSlotB;
    if (a->slot < 0 && b->slot < 0)
        if (const int32_t same = dn2cpp_reflbind_gvm_same_body(a, b); same >= 0)
            return same != 0;
    if (((a->method->declaringType->flags | b->method->declaringType->flags) & DN2CPP_TF_PATCH) != 0)
    {
        const Dn2CppTypeInfo* ownerA;
        const Dn2CppTypeInfo* ownerB;
        int32_t tokenA, tokenB, argcA, argcB;
        const Dn2CppTypeInfo* const* argsA;
        const Dn2CppTypeInfo* const* argsB;
        if (!dn2cpp_reflbind_selected_key(a, ownerA, tokenA, argcA, argsA)
            || !dn2cpp_reflbind_selected_key(b, ownerB, tokenB, argcB, argsB)
            || ownerA != ownerB || tokenA != tokenB || argcA != argcB)
            return false;
        for (int32_t i = 0; i < argcA; i++)
            if (argsA[i] != argsB[i])
                return false;
        return true;
    }
    if (!dn2cpp_reflbind_receiver_described(a->target->type))
        return false;
    const Dn2CppMetadataHandle<Dn2CppMethodInfo> method = dn2cpp_reflbind_method(a);
    return method && method == dn2cpp_reflbind_method(b);
}

Dn2CppObject* dn2cpp_delegate_get_method(Dn2CppObject* d)
{
    if (d == nullptr)
        return nullptr;
    Dn2CppObject* t = reinterpret_cast<Dn2CppDelegate*>(d)->target;
    if (t != nullptr && t->type == &dn2cpp_reflbind_type)
    {
        const auto* bind = reinterpret_cast<Dn2CppReflBind*>(t);
        // .NET reads a generic type's instantiation off a closed binding's receiver,
        // so the method of one closed over null faults.
        if (bind->mode == DN2CPP_DGBIND_CLOSED_INSTANCE && bind->target == nullptr
            && dn2cpp_type_is_generic_type(bind->declaring) != 0)
            dn2cpp_throw_null_reference();
        const auto bound = dn2cpp_reflbind_method(bind);
        if (!bound)
            return nullptr;
        if (std::strcmp(bound->name, ".ctor") == 0 || std::strcmp(bound->name, ".cctor") == 0)
        {
            static constexpr char message[] = "Unable to cast object of type 'System.Reflection.RuntimeConstructorInfo' to type 'System.Reflection.MethodInfo'.";
            dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_invalid_cast_exception_type,
                dn2cpp_string_from_utf8(message, static_cast<int32_t>(sizeof message - 1)), nullptr));
        }
        // Declaring-normalized: .NET's delegate.Method is the declaring-typed
        // instance even when the delegate was created from a derived-reflected row. A
        // template row's declaring type is the clone level the binding runs on, which
        // a closed binding's receiver instantiates for every row it can name.
        const Dn2CppTypeInfo* through = bind->mode == DN2CPP_DGBIND_CLOSED_INSTANCE && bind->target != nullptr
            ? bind->target->type
            : bind->declaring;
        return reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(bound,
            dn2cpp_invoke_declaring(bound->declaringType, through)));
    }
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(d);
    // Runtime-created and interpreted delegates carry no static identity.
    const auto* identity = dg->identity;
    if (identity == nullptr || identity->declaringType == nullptr)
        return nullptr;
    const Dn2CppTypeInfo* owner = identity->declaringType;
    if (t != nullptr && identity->virtualBinding && (owner->flags & DN2CPP_TF_INTERFACE) != 0
        && identity->genericArgCount == 0 && identity->interfaceSlot >= 0)
        if (const auto exact = dn2cpp_delegate_interface_provenance(t->type, owner, identity->interfaceSlot, 0, nullptr))
            return reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(exact, nullptr));
    if (t != nullptr && identity->virtualBinding && (t->type->flags & DN2CPP_TF_PATCH) == 0)
    {
        const Dn2CppTypeInfo* selectedOwner;
        int32_t selectedToken;
        if (dn2cpp_delegate_selected_key(identity, t->type, selectedOwner, selectedToken))
            if (const auto selected = dn2cpp_delegate_find_method(selectedOwner, selectedToken,
                    identity->genericArgCount, [identity](int32_t i) { return identity->genericArgs[i]; }))
                return reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(selected, nullptr));
    }
    dn2cpp_require_metadata(owner);
    const auto declared = dn2cpp_delegate_find_method(owner,
        identity->metadataToken, identity->genericArgCount,
        [identity](int32_t i) { return identity->genericArgs[i]; });
    // Object dispatch also has runtime descriptors in images without a private row.
    if (!declared)
    {
        const Dn2CppMetaMember* d = owner == &dn2cpp_object_type && (!identity->virtualBinding || t != nullptr)
            ? dn2cpp_object_dispatch_member(dg->method, identity->virtualBinding) : nullptr;
        const auto hit = d == nullptr ? Dn2CppMetadataHandle<Dn2CppMethodInfo>{}
            : identity->virtualBinding ? dn2cpp_object_virtual_target(t->type, d)
            : dn2cpp_meta_row(d, &dn2cpp_object_type, nullptr, 0);
        return hit ? reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(hit, nullptr)) : nullptr;
    }
    if (identity->virtualBinding && t != nullptr && (declared->attrs & DN2CPP_MTHA_METAANSWER) != 0)
    {
        const auto* descriptor = dn2cpp_meta_desc_of(declared);
        if (descriptor != nullptr && (descriptor->ilAttrs & DN2CPP_MA_VIRTUAL) != 0)
        {
            const auto selected = dn2cpp_object_virtual_target(t->type, descriptor);
            return selected ? reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(selected, nullptr)) : nullptr;
        }
    }
    if (!identity->virtualBinding || t == nullptr)
    {
        auto selected = declared;
        if (t != nullptr && !identity->virtualBinding
            && (owner->flags & DN2CPP_TF_RUNTIME_TEMPLATE) != 0)
        {
            const Dn2CppMethodInfo row = *declared;
            if ((row.attrs & DN2CPP_MTHA_STATIC) == 0 && row.genericParamCount == 0)
                selected = dn2cpp_clone_level_method(t->type, owner, declared, 0, nullptr);
        }
        return reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(selected, nullptr));
    }
    // .NET names the override the binding resolved, so the declaration answers
    // only when no override binds.
    const Dn2CppMethodInfo decl = *declared;
    Dn2CppMetadataHandle<Dn2CppMethodInfo> hit{};
    if (decl.genericParamCount != 0 && decl.vtableSlot < 0)
        hit = dn2cpp_delegate_recorded_target(t->type, owner, identity);
    else if ((owner->flags & DN2CPP_TF_INTERFACE) != 0)
    {
        hit = dn2cpp_delegate_recorded_target(t->type, owner, identity);
        if (!hit)
            hit = dn2cpp_delegate_interface_target(t->type, owner, decl, identity, dg->method);
    }
    else if (decl.vtableSlot >= 0)
        hit = dn2cpp_delegate_class_virtual_target(t->type, owner, decl.vtableSlot);
    if (!hit && owner == &dn2cpp_memberinfo_type && dn2cpp_native_member_header(t->type)
        && std::strcmp(decl.name, "HasSameMetadataDefinitionAs") == 0)
        dn2cpp_throw_platform_not_supported("Delegate.Method: the runtime metadata-definition override is not retained");
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(hit ? hit : declared, nullptr));
}

// ---- reflection: constructors ----

// Allocates a fresh instance of mi->declaringType (a boxed payload for a value type),
// runs the ctor through its invoker thunk (instance, void return), and returns it.
static Dn2CppObject* dn2cpp_ctor_invoke_argv(Dn2CppMetadataHandle<Dn2CppMethodInfo> mi, Dn2CppObject** args, int32_t argc, bool wrapExceptions)
{
    Dn2CppInvokePlan local;
    const Dn2CppInvokePlan& plan = dn2cpp_invoke_plan_of(mi, true, local);
    if ((plan.declaringType->flags & DN2CPP_TF_GENERICDEF) != 0)
        dn2cpp_throw_member_access("Cannot create an instance of a type with unbound generic parameters.");
    if (dn2cpp_type_contains_generic_parameters(plan.declaringType) || dn2cpp_type_metadata_only(plan.declaringType))
        dn2cpp_throw_invalid_operation();
    // .NET refuses a by-ref-like type's constructor before it looks at the arguments.
    if ((plan.declaringType->flags & DN2CPP_TF_BYREFLIKE) != 0)
        dn2cpp_throw_reflection_fault(&dn2cpp_target_exception_type, nullptr, 0x80131603u);
    if (argc != plan.paramCount)
        dn2cpp_throw_invoke_parameter_count();
    Dn2CppObject** callerArgs = args;
    const bool passArgs = (plan.flags & Dn2CppInvokePlan::kPassArgs) != 0;
    Dn2CppArrayI4* defaultCopyBack = nullptr;
    args = passArgs ? dn2cpp_invoke_pass_args(plan.parameters, args, argc, wrapExceptions, &defaultCopyBack,
                        plan.declaringType)
                    : dn2cpp_invoke_check_args(mi, plan.checkedTypes(), args, argc, &defaultCopyBack,
                        plan.declaringType);
    // The image refuses only a call .NET would make, before anything is allocated.
    if (plan.invoker == nullptr || plan.fnPtr == nullptr)
        dn2cpp_throw_invoke_unavailable(mi, plan.declaringType);
    const Dn2CppTypeInfo* ti = plan.declaringType;
    if (dn2cpp_unallocated(ti))
    {
        const Dn2CppMethodInfo row = *mi;
        dn2cpp_throw_unallocated(ti, dn2cpp_unretained_subject(ti, row.display != nullptr
            ? dn2cpp_utf8_of(dn2cpp_metadata_string(row.display)) : std::string(".ctor")), "constructing it");
    }
    if ((plan.flags & Dn2CppInvokePlan::kUnallocatedBox) != 0)
        dn2cpp_throw_invoke_unallocated_box(mi, plan, ti);
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
    dn2cpp_invoke_target(plan.invoker, plan.fnPtr, self, args, nullptr, wrapExceptions);
    if (passArgs)
        dn2cpp_invoke_write_back(args, callerArgs, argc);
    dn2cpp_invoke_write_defaults(plan.parameters, args, callerArgs, defaultCopyBack, argc);
    return obj;
}

// Array.Initialize over a receiver whose element type only the object states. The
// element type's parameterless constructor row runs in place on every element, in
// storage order (row-major for an MD array), and what it throws propagates unwrapped.
// An element type that declares no such constructor leaves the array unchanged. A row
// without a body, or a declared constructor whose row the image omits, is a constructor
// this image did not compile, never one that does not exist.
void dn2cpp_array_initialize(Dn2CppObject* a)
{
    if (a == nullptr)
        dn2cpp_throw_null_reference();
    const Dn2CppTypeInfo* el = a->type != nullptr ? a->type->elementType : nullptr;
    if (el == nullptr || (el->flags & DN2CPP_TF_VALUETYPE) == 0)
        return;
    const auto reflection = el->reflection();
    for (int32_t i = 0; i < reflection.ctorCount; i++)
    {
        const auto row = reflection.ctors[i].operator->();
        if (row->paramCount != 0)
            continue;
        if (row->invoker == nullptr || row->fnPtr == nullptr)
            dn2cpp_throw_invoke_unavailable(reflection.ctors[i], el);
        char* data;
        int32_t size, count;
        if (dn2cpp_is_md_array(a->type))
        {
            auto* md = static_cast<Dn2CppMDArray*>(a);
            data = md->data;
            size = md->elemSize;
            count = dn2cpp_md_total_length(md);
        }
        else
        {
            auto* sz = static_cast<Dn2CppArrayN*>(a);
            data = sz->data;
            size = sz->elemSize;
            count = sz->length;
        }
        for (int32_t j = 0; j < count; j++)
            dn2cpp_invoke_target(row->invoker, row->fnPtr,
                reinterpret_cast<Dn2CppObject*>(data + static_cast<size_t>(j) * static_cast<size_t>(size)),
                nullptr, nullptr, false);
        return;
    }
    if ((reflection.activation & (DN2CPP_TA_OMITTED_PUBLIC_DEFAULT_CTOR | DN2CPP_TA_OMITTED_NONPUBLIC_DEFAULT_CTOR)) != 0)
        dn2cpp_throw_omitted_ctor(el, "Void .ctor()");
}

// Collects a type's own constructors (no base walk — ctors are never inherited,
// so the queried type IS the declaring type and the reflected-type stamp below
// is the same pointer normalization would pick; passed for query-path uniformity).
static int32_t dn2cpp_collect_ctors(const Dn2CppTypeInfo* ti, int32_t flags, Dn2CppObject** out)
{
    int32_t n = 0;
    const auto reflection = ti->reflection();
    for (int32_t i = 0; i < reflection.ctorCount; i++)
    {
        const auto ci = reflection.ctors[i];
        if (dn2cpp_member_matches(ci->attrs, flags, false))
        {
            if (out != nullptr)
                dn2cpp_gc_store_ref(&out[n],
                    reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(ci, ti)));
            n++;
        }
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
    dn2cpp_type_require(t);
    // Every Type.GetConstructor overload declares `types`: a null array or element is
    // rejected before any lookup.
    if (paramTypes == nullptr)
        dn2cpp_throw_argument_null_param("types");
    int32_t want = paramTypes->length;
    for (int32_t j = 0; j < want; j++)
        if (paramTypes->data[j] == nullptr)
            dn2cpp_throw_argument_null_param("types");
    dn2cpp_reflection_check_binder(binder);
    const Dn2CppTypeInfo* ti = t->typeInfo;
    const auto reflection = ti->reflection();
    for (int32_t i = 0; i < reflection.ctorCount; i++)
    {
        const auto ci = reflection.ctors[i];
        const auto row = ci.operator->();
        if (!dn2cpp_member_matches(row->attrs, bindingFlags, false) || row->paramCount != want)
            continue;
        bool match = true;
        for (int32_t j = 0; j < want; j++)
        {
            auto* pt = reinterpret_cast<Dn2CppType*>(paramTypes->data[j]);
            const auto parameter = *row->parameters[j];
            const auto* parameterType = (row->declaringType->flags & DN2CPP_TF_GENERICDEF) != 0 && row->definitionSignature != nullptr
                ? dn2cpp_method_definition_type(ci, "symbolic", parameter.paramType, ti, j)
                : dn2cpp_reflection_parameter_type(parameter, ti);
            if (parameterType != pt->typeInfo)
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

Dn2CppObject* dn2cpp_ctorref_invoke(Dn2CppMethodRef* c, Dn2CppArrayRef* args, bool wrapExceptions)
{
    return dn2cpp_ctor_invoke_argv(dn2cpp_methodref_require(c), args == nullptr ? nullptr : args->data,
        args == nullptr ? 0 : args->length, wrapExceptions);
}

[[noreturn]] static void dn2cpp_throw_missing_constructor(const Dn2CppTypeInfo* ti, bool bound);

// Activator.CreateInstance(Type[, bool nonPublic]): the parameterless form.
// Matches real .NET's exceptions: an abstract class or interface throws
// MissingMethodException, and so does a type whose only
// parameterless ctor is non-public when nonPublic wasn't requested — the
// parameterless CreateInstance(Type) binds PUBLIC ctors only. A value type
// with no explicit parameterless ctor yields the zero-initialized boxed value.
static Dn2CppObject* dn2cpp_activator_create_default(Dn2CppType* t, int32_t nonPublic,
    bool wrapExceptions, bool binderMessage)
{
    if (t == nullptr)
        dn2cpp_throw_argument_null_param("type");
    const Dn2CppTypeInfo* ti = t->typeInfo;
    if (dn2cpp_type_contains_generic_parameters(ti))
        dn2cpp_throw_sr0(&dn2cpp_argument_exception_type, DN2CPP_SR_TYPE_NOT_SUPPORTED);
    if (dn2cpp_type_metadata_only(ti))
        dn2cpp_throw_platform_not_supported("Reflection: the constraint type has no runtime layout");
    if ((ti->flags & (DN2CPP_TF_ABSTRACT | DN2CPP_TF_INTERFACE)) != 0)
        dn2cpp_throw_missing_method("Cannot create an instance of an abstract class or interface");
    // Whatever constructor it declares, a by-ref-like value is never boxed.
    if ((ti->flags & DN2CPP_TF_BYREFLIKE) != 0)
        dn2cpp_throw_invoke_not_supported("Cannot create boxed ByRef-like values.");
    const auto reflection = ti->reflection();
    bool hasDefaultConstructor = (reflection.activation &
        (DN2CPP_TA_OMITTED_PUBLIC_DEFAULT_CTOR | DN2CPP_TA_OMITTED_NONPUBLIC_DEFAULT_CTOR)) != 0;
    for (int32_t i = 0; i < reflection.ctorCount; i++)
    {
        const auto ci = reflection.ctors[i];
        const auto row = ci.operator->();
        hasDefaultConstructor |= row->paramCount == 0;
        if (row->paramCount == 0
            && (nonPublic != 0 || (row->attrs & DN2CPP_MTHA_PUBLIC) != 0))
            return dn2cpp_ctor_invoke_argv(ci, nullptr, 0, wrapExceptions);
    }
    // A row whose body the image did not retain refuses in dn2cpp_ctor_invoke_argv; a
    // visible parameterless constructor whose row the image omits refuses here.
    if ((reflection.activation & (nonPublic != 0
            ? DN2CPP_TA_OMITTED_PUBLIC_DEFAULT_CTOR | DN2CPP_TA_OMITTED_NONPUBLIC_DEFAULT_CTOR
            : DN2CPP_TA_OMITTED_PUBLIC_DEFAULT_CTOR)) != 0)
        dn2cpp_throw_omitted_ctor(ti, "Void .ctor()");
    // A value type with no explicit parameterless ctor: zero-initialized boxed value.
    if ((ti->flags & DN2CPP_TF_VALUETYPE) != 0)
    {
        if (dn2cpp_unallocated(ti))
            dn2cpp_throw_unallocated(ti, dn2cpp_unretained_subject(ti, "Void .ctor()"), "constructing it");
        auto* obj = static_cast<Dn2CppObject*>(
            dn2cpp_alloc(sizeof(Dn2CppObject) + static_cast<size_t>(ti->instanceSize)));
        obj->type = ti;
        return dn2cpp_rebox_from_nullable(obj);
    }
    if (binderMessage)
        dn2cpp_throw_missing_constructor(ti, false);
    dn2cpp_throw_missing_default_constructor(ti, hasDefaultConstructor);
}

[[noreturn]] void dn2cpp_throw_missing_default_constructor(const Dn2CppTypeInfo* ti,
    bool hasDefaultConstructor)
{
    Dn2CppString* typeName = ti->name != nullptr ? dn2cpp_type_tostring(ti) : nullptr;
    std::string name = typeName != nullptr ? dn2cpp_utf8_of(typeName) : "?";
    if (hasDefaultConstructor)
        dn2cpp_throw_missing_method(("No parameterless constructor defined for type '" + name + "'.").c_str());
    dn2cpp_throw_missing_method(("Cannot dynamically create an instance of type '" + name
        + "'. Reason: No parameterless constructor defined.").c_str());
}

Dn2CppObject* dn2cpp_activator_create_instance_wrapped(Dn2CppType* t, int32_t nonPublic, int32_t wrapExceptions)
{
    if (t == nullptr)
        dn2cpp_throw_argument_null_param("type");
    if (t->type != &dn2cpp_type_type)
        dn2cpp_throw_platform_not_supported("Activator: custom Type construction is not supported");
    return dn2cpp_activator_create_default(t, nonPublic, wrapExceptions != 0, false);
}

Dn2CppObject* dn2cpp_activator_create_instance_nonpublic(Dn2CppType* t, int32_t nonPublic)
{
    return dn2cpp_activator_create_default(t, nonPublic, true, false);
}

Dn2CppObject* dn2cpp_activator_create_instance(Dn2CppType* t)
{
    return dn2cpp_activator_create_instance_nonpublic(t, 0);
}

// ---- reflection: properties ----

static Dn2CppPropRef* dn2cpp_make_propref(Dn2CppMetadataHandle<Dn2CppPropInfo> p,
                                          const Dn2CppTypeInfo* reflected)
{
    if (reflected == nullptr)
        reflected = p->declaringType;
    std::lock_guard<std::mutex> lk(g_memberref_intern_mtx);
    Dn2CppPropRef*& slot = g_propref_intern[{ p.identity(), reflected }];
    if (slot == nullptr)
    {
        auto* r = static_cast<Dn2CppPropRef*>(dn2cpp_alloc_pinned(sizeof(Dn2CppPropRef)));
        r->type = &dn2cpp_propertyinfo_type;
        r->prop = p;
        r->reflectedType = reflected;
        const auto row = p.operator->();
        r->getter = row->getter;
        r->setter = row->setter;
        slot = r;
    }
    return slot;
}

static bool dn2cpp_props_sig_equal(Dn2CppMetadataHandle<Dn2CppPropInfo> a, Dn2CppMetadataHandle<Dn2CppPropInfo> b,
    const Dn2CppTypeInfo* reflected);

// Hide only matching names and signatures, preserving other indexer overloads
// and same-name properties whose return types differ.
static int32_t dn2cpp_collect_props(const Dn2CppTypeInfo* type, int32_t flags, Dn2CppObject** out)
{
    int32_t n = 0;
    Dn2CppWalkList<Dn2CppMetadataHandle<Dn2CppPropInfo>, 64> seen;
    for (const Dn2CppTypeInfo* ti = type; ti != nullptr; ti = dn2cpp_reflection_base(ti))
    {
        dn2cpp_require_metadata(ti);
        const auto reflection = ti->reflection();
        bool inherited = (ti != type);
        for (int32_t i = 0; i < reflection.propCount; i++)
        {
            const auto p = reflection.props[i];
            const auto row = p.operator->();
            if (!dn2cpp_member_matches(row->attrs, flags, inherited))
                continue;
            bool hidden = false;
            for (int32_t s = 0; s < seen.size() && !hidden; s++)
                hidden = std::strcmp(seen[s]->name, row->name) == 0 && dn2cpp_props_sig_equal(seen[s], p, type);
            if (hidden)
                continue;
            seen.push_back(p);
            if (out != nullptr)
                dn2cpp_gc_store_ref(&out[n],
                    reinterpret_cast<Dn2CppObject*>(dn2cpp_make_propref(p, type)));
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
static Dn2CppMetadataTable<Dn2CppParamInfo> dn2cpp_prop_index_parameters(
    const Dn2CppPropInfo* p, int32_t* count)
{
    const auto accessor = p->getter != nullptr ? p->getter : p->setter;
    if (accessor == nullptr)
    {
        *count = 0;
        return {};
    }
    const auto row = accessor.operator->();
    *count = p->getter != nullptr ? row->paramCount
        : (row->paramCount > 0 ? row->paramCount - 1 : 0);
    return row->parameters;
}

// Whether two property rows have the same reflection signature (property type +
// indexer parameter list) — the equality behind .NET's most-derived-wins rule
// for an overridden/`new`-redeclared property.
static const Dn2CppTypeInfo* dn2cpp_property_signature_type(Dn2CppMetadataHandle<Dn2CppPropInfo> property,
    const Dn2CppTypeInfo* reflected, int32_t parameter = -1)
{
    const auto accessor = property->getter != nullptr ? property->getter : property->setter;
    if (accessor != nullptr && dn2cpp_symbolic_method(*accessor))
        return dn2cpp_method_definition_type(accessor, "symbolic", property->propType,
            dn2cpp_invoke_declaring(property->declaringType, reflected), parameter >= 0 ? parameter
                : property->getter != nullptr ? -1 : accessor->paramCount - 1);
    return parameter < 0 ? property->propType : accessor->parameters[parameter]->paramType;
}

static bool dn2cpp_props_sig_equal(Dn2CppMetadataHandle<Dn2CppPropInfo> a, Dn2CppMetadataHandle<Dn2CppPropInfo> b,
    const Dn2CppTypeInfo* reflected)
{
    const auto rowA = a.operator->();
    const auto rowB = b.operator->();
    if (dn2cpp_property_signature_type(a, reflected) != dn2cpp_property_signature_type(b, reflected))
        return false;
    int32_t countA;
    int32_t countB;
    dn2cpp_prop_index_parameters(rowA.operator->(), &countA);
    dn2cpp_prop_index_parameters(rowB.operator->(), &countB);
    if (countA != countB)
        return false;
    for (int32_t i = 0; i < countA; i++)
        if (dn2cpp_property_signature_type(a, reflected, i) != dn2cpp_property_signature_type(b, reflected, i))
            return false;
    return true;
}

static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_prop_public_accessor(
    Dn2CppMetadataHandle<Dn2CppPropInfo> property)
{
    const auto getter = property->getter;
    if (getter != nullptr && (getter->attrs & DN2CPP_MTHA_PUBLIC) != 0)
        return getter;
    const auto setter = property->setter;
    if (setter != nullptr && (setter->attrs & DN2CPP_MTHA_PUBLIC) != 0)
        return setter;
    return {};
}

static bool dn2cpp_props_share_override_slot(Dn2CppMetadataHandle<Dn2CppPropInfo> first,
    Dn2CppMetadataHandle<Dn2CppPropInfo> second)
{
    if ((first->declaringType->flags & DN2CPP_TF_INTERFACE) != 0
        || (second->declaringType->flags & DN2CPP_TF_INTERFACE) != 0)
        return false;
    const auto a = dn2cpp_prop_public_accessor(first);
    const auto b = dn2cpp_prop_public_accessor(second);
    return a != nullptr && b != nullptr && a->vtableSlot >= 0
        && a->vtableSlot == b->vtableSlot;
}

Dn2CppPropRef* dn2cpp_type_get_property_full(Dn2CppType* t, Dn2CppString* name, int32_t bindingFlags,
                                             Dn2CppType* returnType, Dn2CppArrayRef* indexTypes,
                                             Dn2CppObject* binder, int32_t declared)
{
    dn2cpp_type_require(t);
    if (name == nullptr)
        dn2cpp_throw_argument_null_param("name");
    if ((declared & DN2CPP_LOOKUP_TYPES) != 0 && indexTypes == nullptr)
        dn2cpp_throw_argument_null_param("types");
    dn2cpp_reflection_check_binder(binder);
    // Collect candidates derived-first (flags + name + optional propType /
    // indexer-signature filters), then resolve like the method lookup: one
    // match wins; sig-equal matches (override / `new` redeclaration chains)
    // resolve to the most derived; anything else throws AmbiguousMatchException
    // (e.g. GetProperty("Item") over overloaded indexers), matching real .NET.
    Dn2CppWalkList<Dn2CppMetadataHandle<Dn2CppPropInfo>, 16> cands;
    for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr; ti = dn2cpp_reflection_base(ti))
    {
        dn2cpp_require_metadata(ti);
        const auto reflection = ti->reflection();
        bool inherited = (ti != t->typeInfo);
        for (int32_t i = 0; i < reflection.propCount; i++)
        {
            const auto p = reflection.props[i];
            const auto row = p.operator->();
            if (!dn2cpp_member_matches(row->attrs, bindingFlags, inherited)
                || !dn2cpp_member_name_matches(row->name, name, bindingFlags, false))
                continue;
            // The default binder rejects a null index type of any candidate of the
            // requested index count, before filtering on the property type.
            if (indexTypes != nullptr)
            {
                int32_t count;
                dn2cpp_prop_index_parameters(row.operator->(), &count);
                if (count != indexTypes->length)
                    continue;
                for (int32_t j = 0; j < indexTypes->length; j++)
                    if (indexTypes->data[j] == nullptr)
                        dn2cpp_throw_argument_null_param("indexes");
                bool match = true;
                for (int32_t j = 0; j < indexTypes->length; j++)
                {
                    auto* pt = reinterpret_cast<Dn2CppType*>(indexTypes->data[j]);
                    if (dn2cpp_property_signature_type(p, t->typeInfo, j) != pt->typeInfo) { match = false; break; }
                }
                if (!match)
                    continue;
            }
            if (returnType != nullptr && dn2cpp_property_signature_type(p, t->typeInfo) != returnType->typeInfo)
                continue;
            cands.push_back(p);
        }
        if (bindingFlags & DN2CPP_BF_DECLAREDONLY)
            break;
    }
    if (cands.size() == 0)
        return nullptr;
    Dn2CppWalkList<Dn2CppMetadataHandle<Dn2CppPropInfo>, 16> visible;
    for (int32_t i = 0; i < cands.size(); i++)
    {
        bool slotHidden = false;
        // A name-hidden row still carries the slot that hides a deeper override.
        for (int32_t j = 0; j < i && !slotHidden; j++)
            slotHidden = dn2cpp_props_share_override_slot(cands[j], cands[i]);
        if (slotHidden)
            continue;
        bool nameHidden = false;
        for (int32_t j = 0; j < visible.size() && !nameHidden; j++)
            if (visible[j]->declaringType != cands[i]->declaringType)
                nameHidden = std::strcmp(visible[j]->name, cands[i]->name) == 0
                    && dn2cpp_props_sig_equal(visible[j], cands[i], t->typeInfo);
        if (!nameHidden)
            visible.push_back(cands[i]);
    }
    if (visible.size() > 1)
        dn2cpp_throw_ambiguous_member(reinterpret_cast<Dn2CppObject*>(
            dn2cpp_make_propref(visible[0], t->typeInfo)));
    return dn2cpp_make_propref(visible[0], t->typeInfo);
}

Dn2CppPropRef* dn2cpp_type_get_property(Dn2CppType* t, Dn2CppString* name, int32_t bindingFlags)
{
    return dn2cpp_type_get_property_full(t, name, bindingFlags, nullptr, nullptr, nullptr, 0);
}

Dn2CppType* dn2cpp_propref_property_type(Dn2CppPropRef* p)
{
    return dn2cpp_get_type_from_handle(dn2cpp_property_signature_type(dn2cpp_propref_require(p), p->reflectedType));
}

static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_propref_visible_accessor(
    Dn2CppPropRef* p, bool setter)
{
    const auto property = dn2cpp_propref_require(p);
    const auto method = setter ? p->setter : p->getter;
    if (method != nullptr
        && p->reflectedType != dn2cpp_invoke_declaring(property->declaringType, p->reflectedType)
        && (method->attrs & DN2CPP_MTHA_PRIVATE) != 0)
        return {};
    return method;
}

static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_propref_required_accessor(
    Dn2CppPropRef* p, bool setter)
{
    const auto method = dn2cpp_propref_visible_accessor(p, setter);
    if (method == nullptr)
    {
        if ((setter ? p->setter : p->getter) == nullptr)
            dn2cpp_throw_invalid_operation();
        dn2cpp_throw_argument();
    }
    return method;
}

int32_t dn2cpp_propref_can_read(Dn2CppPropRef* p)
{
    return dn2cpp_propref_visible_accessor(p, false) != nullptr ? 1 : 0;
}

int32_t dn2cpp_propref_can_write(Dn2CppPropRef* p)
{
    return dn2cpp_propref_visible_accessor(p, true) != nullptr ? 1 : 0;
}

// PropertyInfo.GetGetMethod/GetSetMethod: wrap the accessor's method-table entry
// in a reflected handle; null when the property has no such accessor, or when it
// is non-public and nonPublic wasn't requested (the no-arg overloads pass 0).
Dn2CppMethodRef* dn2cpp_propref_accessor(Dn2CppPropRef* p, int32_t setter, int32_t nonPublic)
{
    const auto property = dn2cpp_propref_require(p);
    dn2cpp_require_metadata(property->declaringType);
    Dn2CppMetadataHandle<Dn2CppMethodInfo> m = dn2cpp_propref_visible_accessor(p, setter != 0);
    if (m == nullptr)
        return nullptr;
    if (nonPublic == 0 && (m->attrs & DN2CPP_MTHA_PUBLIC) == 0)
        return nullptr;
    // The accessor inherits the PROPERTY handle's reflected type, so
    // typeof(D).GetProperty("P").GetGetMethod() is the same instance as
    // typeof(D).GetMethod("get_P"), as on .NET.
    return dn2cpp_make_methodref(m, p->reflectedType);
}

Dn2CppArrayRef* dn2cpp_propref_get_accessors(Dn2CppPropRef* p, int32_t nonPublic)
{
    dn2cpp_require_metadata(dn2cpp_propref_require(p)->declaringType);
    Dn2CppMethodRef* getter = dn2cpp_propref_accessor(p, 0, nonPublic);
    Dn2CppMethodRef* setter = dn2cpp_propref_accessor(p, 1, nonPublic);
    auto* result = dn2cpp_newarr_ref((getter != nullptr ? 1 : 0) + (setter != nullptr ? 1 : 0));
    int32_t index = 0;
    if (getter != nullptr)
        dn2cpp_gc_store_ref(&result->data[index++], reinterpret_cast<Dn2CppObject*>(getter));
    if (setter != nullptr)
        dn2cpp_gc_store_ref(&result->data[index], reinterpret_cast<Dn2CppObject*>(setter));
    return result;
}

Dn2CppObject* dn2cpp_propref_get_value(Dn2CppPropRef* p, Dn2CppObject* obj)
{
    const auto getter = dn2cpp_propref_required_accessor(p, false);
    return dn2cpp_invoke_mi(getter, obj, nullptr, 0, true, p->reflectedType);
}

void dn2cpp_propref_set_value(Dn2CppPropRef* p, Dn2CppObject* obj, Dn2CppObject* value)
{
    const auto setter = dn2cpp_propref_required_accessor(p, true);
    Dn2CppObject* args[1] = { value };
    dn2cpp_invoke_mi(setter, obj, args, 1, true, p->reflectedType);
}

// PropertyInfo.GetIndexParameters(): the indexer parameters, read off the
// accessor rows exactly like dn2cpp_prop_index_parameters (the getter's parameters,
// or the setter's minus the trailing value). A non-indexed property yields an
// empty array, matching .NET.
Dn2CppArrayRef* dn2cpp_propref_get_index_parameters(Dn2CppPropRef* p)
{
    Dn2CppMetadataHandle<Dn2CppPropInfo> pi = dn2cpp_propref_require(p);
    const auto row = pi.operator->();
    const auto acc = row->getter != nullptr ? row->getter : row->setter;
    int32_t n;
    const auto parameters = dn2cpp_prop_index_parameters(row.operator->(), &n);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
    {
        auto* pr = static_cast<Dn2CppParamRef*>(dn2cpp_alloc(sizeof(Dn2CppParamRef)));
        pr->type = &dn2cpp_parameterinfo_type;
        pr->param = parameters[i];
        pr->position = i;
        pr->owner = acc;
        pr->ownerReflected = p->reflectedType;
        pr->ownerGenericDefView = 0;
        dn2cpp_gc_store_ref(&arr->data[i], reinterpret_cast<Dn2CppObject*>(pr));
    }
    return arr;
}

// PropertyInfo.GetValue(obj, object[] index) / SetValue(obj, value, object[]
// index): the indexed forms, routed through the accessor rows' boxed invokers.
// A null/empty index behaves like the plain forms; an index-count mismatch
// raises TargetParameterCountException. The BindingFlags/Binder/CultureInfo
// forms route here too (a non-null Binder is the AOT
// PlatformNotSupportedException; culture is a no-op like real .NET's default
// binder on the non-parse path).
Dn2CppObject* dn2cpp_propref_get_value_indexed(Dn2CppPropRef* p, Dn2CppObject* obj,
                                               Dn2CppArrayRef* index, Dn2CppObject* binder,
                                               bool wrapExceptions)
{
    dn2cpp_reflection_check_binder(binder);
    const auto getter = dn2cpp_propref_required_accessor(p, false);
    return dn2cpp_invoke_mi(getter, obj,
                            (index == nullptr) ? nullptr : index->data,
                            (index == nullptr) ? 0 : index->length, wrapExceptions, p->reflectedType);
}

void dn2cpp_propref_set_value_indexed(Dn2CppPropRef* p, Dn2CppObject* obj, Dn2CppObject* value,
                                      Dn2CppArrayRef* index, Dn2CppObject* binder,
                                      bool wrapExceptions)
{
    dn2cpp_reflection_check_binder(binder);
    const auto setter = dn2cpp_propref_required_accessor(p, true);
    int32_t n = (index == nullptr) ? 0 : index->length;
    // The setter's signature is (indices..., value): cap indexer arity at a
    // generous fixed bound rather than allocating (real indexers are tiny).
    if (n > 30)
        dn2cpp_throw_invoke_parameter_count();
    Dn2CppObject* args[31];
    for (int32_t i = 0; i < n; i++)
        args[i] = index->data[i];
    args[n] = value;
    dn2cpp_invoke_mi(setter, obj, args, n + 1, wrapExceptions, p->reflectedType);
}

// ---- reflection: events ----

static Dn2CppEventRef* dn2cpp_make_eventref(Dn2CppMetadataHandle<Dn2CppEventInfo> event,
    const Dn2CppTypeInfo* reflected)
{
    if (reflected == nullptr)
        reflected = event->declaringType;
    std::lock_guard<std::mutex> lock(g_memberref_intern_mtx);
    auto*& slot = g_eventref_intern[{ event.identity(), reflected }];
    if (slot == nullptr)
    {
        slot = static_cast<Dn2CppEventRef*>(dn2cpp_alloc_pinned(sizeof(Dn2CppEventRef)));
        slot->type = &dn2cpp_eventinfo_type;
        slot->event = event;
        slot->reflectedType = reflected;
    }
    return slot;
}

// A member name against a lookup's: ordinal, or folded to invariant upper case under
// BindingFlags.IgnoreCase. With `pattern` (GetMember, GetDefaultMembers), one trailing
// '*' matches every name it starts. A null name matches everything (GetMembers).
static bool dn2cpp_member_name_matches(const char* candidate, Dn2CppString* name, int32_t flags, bool pattern)
{
    if (name == nullptr)
        return true;
    char16_t local[128];
    std::vector<char16_t> spill;
    int32_t bytes = static_cast<int32_t>(std::strlen(candidate));
    char16_t* decoded = local;
    if (bytes > 128)
    {
        spill.resize(static_cast<size_t>(bytes));
        decoded = spill.data();
    }
    int32_t decodedLength = dn2cpp_utf8_to_utf16(candidate, bytes, decoded);
    int32_t length = name->length;
    bool prefix = pattern && length > 0 && name->chars[length - 1] == u'*';
    if (prefix)
        length--;
    if (decodedLength < length || (!prefix && decodedLength != length))
        return false;
    for (int32_t i = 0; i < length; i++)
    {
        char16_t a = decoded[i], b = name->chars[i];
        if ((flags & 1) != 0 && a != b)
        {
            a = dn2cpp_dgbind_upper(a);
            b = dn2cpp_dgbind_upper(b);
        }
        if (a != b)
            return false;
    }
    return true;
}

// Event hiding is by exact name, before BindingFlags filtering. Inherited
// private declarations do not participate in the visible name set.
static int32_t dn2cpp_collect_events(const Dn2CppTypeInfo* type, Dn2CppString* name,
    int32_t flags, Dn2CppObject** out, bool pattern = false,
    Dn2CppMetadataHandle<Dn2CppEventInfo>* first = nullptr)
{
    Dn2CppWalkList<const char*, 64> seen;
    int32_t count = 0;
    for (auto* ti = type; ti != nullptr; ti = dn2cpp_reflection_base(ti))
    {
        dn2cpp_require_metadata(ti);
        const auto reflection = ti->reflection();
        bool inherited = ti != type;
        for (int32_t i = 0; i < reflection.eventCount; i++)
        {
            const auto event = reflection.events[i];
            const auto row = event.operator->();
            if (inherited && (row->accessorAttrs & DN2CPP_MTHA_PRIVATE) != 0)
                continue;
            bool hidden = false;
            for (int32_t j = 0; j < seen.size(); j++)
                if (std::strcmp(seen[j], row->name) == 0) { hidden = true; break; }
            if (hidden)
                continue;
            seen.push_back(row->name);
            if (!dn2cpp_member_matches(row->accessorAttrs, flags, inherited))
                continue;
            if (!dn2cpp_member_name_matches(row->name, name, flags, pattern))
                continue;
            if (first != nullptr && count == 0)
                *first = event;
            if (out != nullptr)
                dn2cpp_gc_store_ref(&out[count], reinterpret_cast<Dn2CppObject*>(dn2cpp_make_eventref(event, type)));
            count++;
        }
        if ((flags & DN2CPP_BF_DECLAREDONLY) != 0)
            break;
    }
    return count;
}

Dn2CppArrayRef* dn2cpp_type_get_events(Dn2CppType* type, int32_t flags)
{
    dn2cpp_type_require(type);
    int32_t count = dn2cpp_collect_events(type->typeInfo, nullptr, flags, nullptr);
    auto* result = dn2cpp_newarr_ref(count);
    dn2cpp_collect_events(type->typeInfo, nullptr, flags, result->data);
    return result;
}

Dn2CppEventRef* dn2cpp_type_get_event(Dn2CppType* type, Dn2CppString* name, int32_t flags)
{
    dn2cpp_type_require(type);
    if (name == nullptr)
        dn2cpp_throw_argument_null_param("name");
    Dn2CppMetadataHandle<Dn2CppEventInfo> first;
    int32_t count = dn2cpp_collect_events(type->typeInfo, name, flags, nullptr, false, &first);
    if (count == 0)
        return nullptr;
    auto* result = dn2cpp_make_eventref(first, type->typeInfo);
    if (count > 1)
        dn2cpp_throw_ambiguous_member(reinterpret_cast<Dn2CppObject*>(result));
    return result;
}

Dn2CppType* dn2cpp_eventref_handler_type(Dn2CppEventRef* event)
{
    const auto row = dn2cpp_eventref_require(event);
    const auto* type = row->handlerRecipe != nullptr
        ? dn2cpp_symbolic_member_type(row->handlerRecipe, dn2cpp_invoke_declaring(row->declaringType, event->reflectedType))
        : row->handlerType;
    return dn2cpp_get_type_from_handle(type);
}

static Dn2CppMethodRef* dn2cpp_event_accessor(Dn2CppEventRef* event,
    Dn2CppMetadataHandle<Dn2CppMethodInfo> method, int32_t nonPublic)
{
    if (!method || (nonPublic == 0 && (method->attrs & DN2CPP_MTHA_PUBLIC) == 0))
        return nullptr;
    if (event->reflectedType != dn2cpp_invoke_declaring(event->event->declaringType, event->reflectedType)
        && (method->attrs & DN2CPP_MTHA_PRIVATE) != 0)
        return nullptr;
    return method->genericParamCount > 0 ? dn2cpp_make_methodref_defview(method, event->reflectedType)
        : dn2cpp_make_methodref(method, event->reflectedType);
}

Dn2CppMethodRef* dn2cpp_eventref_accessor(Dn2CppEventRef* event, int32_t accessor, int32_t nonPublic)
{
    const auto row = dn2cpp_eventref_require(event);
    dn2cpp_require_metadata(row->declaringType);
    return dn2cpp_event_accessor(event, accessor == 0 ? row->adder : accessor == 1 ? row->remover : row->raiser, nonPublic);
}

Dn2CppArrayRef* dn2cpp_eventref_other_methods(Dn2CppEventRef* event, int32_t nonPublic)
{
    const auto row = dn2cpp_eventref_require(event).operator->();
    dn2cpp_require_metadata(row->declaringType);
    int32_t count = 0;
    for (int32_t i = 0; i < row->otherCount; i++)
        if (dn2cpp_event_accessor(event, row->others[i], nonPublic) != nullptr)
            count++;
    auto* result = dn2cpp_newarr_ref(count);
    count = 0;
    for (int32_t i = 0; i < row->otherCount; i++)
        if (auto* method = dn2cpp_event_accessor(event, row->others[i], nonPublic))
            dn2cpp_gc_store_ref(&result->data[count++], reinterpret_cast<Dn2CppObject*>(method));
    return result;
}

int32_t dn2cpp_eventref_attributes(Dn2CppEventRef* event)
{
    return dn2cpp_eventref_require(event)->eventAttributes;
}

int32_t dn2cpp_eventref_is_multicast(Dn2CppEventRef* event)
{
    return (dn2cpp_eventref_handler_type(event)->typeInfo->flags & DN2CPP_TF_DELEGATE) != 0;
}

void dn2cpp_eventref_handler(Dn2CppEventRef* event, Dn2CppObject* target, Dn2CppObject* handler, int32_t remove)
{
    auto* method = dn2cpp_eventref_accessor(event, remove != 0 ? 1 : 0, 0);
    if (method == nullptr)
        dn2cpp_throw_invalid_operation();
    Dn2CppObject* args[] = { handler };
    dn2cpp_invoke_mi(method->method, target, args, 1, true, event->reflectedType);
}

// ---- reflection: mixed member lookup (GetMember/GetMembers/GetDefaultMembers) ----

// System.Reflection.MemberTypes bits.
#define DN2CPP_MT_CONSTRUCTOR 0x01
#define DN2CPP_MT_EVENT       0x02
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
        Dn2CppSeenSlots seen;
        Dn2CppGvmHiding gvm;
        gvm.reflected = type;
        for (const Dn2CppTypeInfo* ti = type; ti != nullptr; ti = dn2cpp_reflection_base(ti))
        {
            dn2cpp_require_metadata(ti);
            const auto reflection = ti->reflection();
            bool inherited = (ti != type);
            std::vector<int32_t> definitions;
            for (int32_t i = 0; i < reflection.methodCount; i++)
            {
                const auto mi = reflection.methods[i];
                const auto row = mi.operator->();
                if (!dn2cpp_member_matches(row->attrs, flags, inherited))
                    continue;
                if (row->genericParamCount > 0 && row->metadataToken != 0)
                {
                    bool duplicate = false;
                    for (int32_t token : definitions)
                        if (token == row->metadataToken) { duplicate = true; break; }
                    if (duplicate)
                        continue;
                    definitions.push_back(row->metadataToken);
                }
                bool hidden = row->vtableSlot >= 0 && seen.seen(row->vtableSlot);
                if (hidden || !dn2cpp_member_name_matches(row->name, name, flags, true))
                    continue;
                if (gvm.hides(mi, *row.operator->()))
                    continue;
                if (out != nullptr)
                    dn2cpp_gc_store_ref(&out[n],
                        reinterpret_cast<Dn2CppObject*>(row->genericParamCount > 0
                            ? dn2cpp_make_methodref_defview(mi, type) : dn2cpp_make_methodref(mi, type)));
                n++;
            }
            if (flags & DN2CPP_BF_DECLAREDONLY)
                break;
            gvm.next_type();
        }
    }
    if (memberTypes & DN2CPP_MT_CONSTRUCTOR)
    {
        const auto reflection = type->reflection();
        for (int32_t i = 0; i < reflection.ctorCount; i++)
        {
            const auto ci = reflection.ctors[i];
            const auto row = ci.operator->();
            if (!dn2cpp_member_matches(row->attrs, flags, false)
                || !dn2cpp_member_name_matches(row->name, name, flags, true))
                continue;
            if (out != nullptr)
                dn2cpp_gc_store_ref(&out[n],
                    reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(ci, type)));
            n++;
        }
    }
    if (memberTypes & DN2CPP_MT_PROPERTY)
    {
        // Hide by name + reflection signature (not by name alone): overloaded
        // indexers at one level all surface (GetMember("Item") reports every
        // overload, like real .NET), while an inherited property redeclared by
        // a derived level stays hidden by its most-derived row.
        Dn2CppWalkList<Dn2CppMetadataHandle<Dn2CppPropInfo>, 64> seenP;
        for (const Dn2CppTypeInfo* ti = type; ti != nullptr; ti = dn2cpp_reflection_base(ti))
        {
            dn2cpp_require_metadata(ti);
            const auto reflection = ti->reflection();
            bool inherited = (ti != type);
            for (int32_t i = 0; i < reflection.propCount; i++)
            {
                const auto p = reflection.props[i];
                const auto row = p.operator->();
                if (!dn2cpp_member_matches(row->attrs, flags, inherited))
                    continue;
                bool hidden = false;
                for (int32_t s = 0; s < seenP.size() && !hidden; s++)
                    hidden = std::strcmp(seenP[s]->name, row->name) == 0 && dn2cpp_props_sig_equal(seenP[s], p, type);
                if (hidden)
                    continue;
                seenP.push_back(p);
                if (!dn2cpp_member_name_matches(row->name, name, flags, true))
                    continue;
                if (out != nullptr)
                    dn2cpp_gc_store_ref(&out[n],
                        reinterpret_cast<Dn2CppObject*>(dn2cpp_make_propref(p, type)));
                n++;
            }
            if (flags & DN2CPP_BF_DECLAREDONLY)
                break;
        }
    }
    if (memberTypes & DN2CPP_MT_EVENT)
        n += dn2cpp_collect_events(type, name, flags, out != nullptr ? out + n : nullptr, true);
    if (memberTypes & DN2CPP_MT_FIELD)
        for (const Dn2CppTypeInfo* ti = type; ti != nullptr; ti = dn2cpp_reflection_base(ti))
        {
            dn2cpp_require_metadata(ti);
            const auto reflection = ti->reflection();
            bool inherited = (ti != type);
            for (int32_t i = 0; i < reflection.fieldCount; i++)
            {
                const auto field = reflection.fields[i];
                const auto row = field.operator->();
                if (!dn2cpp_field_matches(row.operator->(), flags, inherited)
                    || !dn2cpp_member_name_matches(row->name, name, flags, true))
                    continue;
                if (out != nullptr)
                    dn2cpp_gc_store_ref(&out[n], reinterpret_cast<Dn2CppObject*>(
                        dn2cpp_make_fieldref(field, type)));
                n++;
            }
            if (flags & DN2CPP_BF_DECLAREDONLY)
                break;
        }
    if ((memberTypes & (DN2CPP_MT_NESTEDTYPE | DN2CPP_MT_TYPEINFO)) != 0
        && (flags & DN2CPP_BF_PUBLIC) != 0)
    {
        const auto reflection = type->reflection();
        for (int32_t i = 0; i < reflection.nestedCount; i++)
        {
            const Dn2CppTypeInfo* nested = reflection.nestedTypes[i];
            // The nested table holds the public emitted set; its entries carry the
            // full CLR reflection name ("Ns.Outer+Inner"), and .NET's GetMember
            // contract matches the pattern against the simple-name tail.
            if (!dn2cpp_member_name_matches(dn2cpp_simple_type_name(nested->name), name, flags, true))
                continue;
            if (out != nullptr)
                dn2cpp_gc_store_ref(&out[n], reinterpret_cast<Dn2CppObject*>(
                    dn2cpp_get_type_from_handle(nested)));
            n++;
        }
    }
    return n;
}

Dn2CppArrayRef* dn2cpp_type_get_member(Dn2CppType* t, Dn2CppString* name,
                                       int32_t memberTypes, int32_t bindingFlags)
{
    dn2cpp_type_require(t);
    if (name == nullptr)
        dn2cpp_throw_argument_null_param("name");
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
    for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr && nm == nullptr; ti = dn2cpp_reflection_base(ti))
        nm = ti->reflection().defaultMemberName;
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
static bool dn2cpp_same_metadata_def(int32_t tokA, const char* assemblyA,
                                     int32_t tokB, const char* assemblyB)
{
    if (tokA == 0 || tokB == 0 || tokA != tokB)
        return false;
    const char* asmA = assemblyA != nullptr ? assemblyA : "System.Private.CoreLib";
    const char* asmB = assemblyB != nullptr ? assemblyB : "System.Private.CoreLib";
    return std::strcmp(asmA, asmB) == 0;
}

// Installed with the image's other dispatch tables, before managed initializers.
// Standalone runtime clients have no MemberInfo slot to dispatch.
int32_t dn2cpp_memberinfo_has_same_metadata_slot = -1;

void dn2cpp_set_memberinfo_has_same_metadata_slot(int32_t slot)
{
    dn2cpp_memberinfo_has_same_metadata_slot = slot;
}

// Registry entries and generic-definition links name canonical definitions, not
// constructed Type objects. Signature/formal types have separate identity rules.
static bool dn2cpp_recorded_type_definition(const Dn2CppTypeInfo* type)
{
    if (type == nullptr || (type->flags & (DN2CPP_TF_ARRAY | DN2CPP_TF_GENERICPARAM)) != 0
        || dn2cpp_signature_kind(type) != 0 || (type->genericDef != nullptr && type->genericDef != type))
        return false;
    if (type->reflection().metadataToken != 0)
        return true;
    for (int32_t i = 0; i < dn2cpp_type_registry_count; ++i)
        if (dn2cpp_type_registry[i].type == type)
            return true;
    return false;
}

[[noreturn]] static void dn2cpp_metadata_definition_unavailable()
{
    dn2cpp_throw_platform_not_supported("HasSameMetadataDefinitionAs: the member's definition identity is not retained");
}

static const Dn2CppTypeInfo* dn2cpp_metadata_type_definition(const Dn2CppTypeInfo* type)
{
    return type != nullptr && type->genericDef != nullptr ? type->genericDef : type;
}

static bool dn2cpp_recorded_formal_parameter(const Dn2CppTypeInfo* type)
{
    std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
    const auto found = g_formal_types.find(type);
    return found != g_formal_types.end() && found->second->parameter;
}

static bool dn2cpp_metadata_compound_type(const Dn2CppTypeInfo* type)
{
    const int32_t kind = dn2cpp_signature_kind(type);
    return (type->flags & DN2CPP_TF_ARRAY) != 0 || kind == 1 || kind == 2;
}

static const char* dn2cpp_metadata_definition_module(const Dn2CppTypeInfo* type)
{
    while (type != nullptr && dn2cpp_metadata_compound_type(type))
        type = type->elementType;
    {
        std::lock_guard<std::recursive_mutex> lock(g_formal_types_mtx);
        const auto formal = g_formal_types.find(type);
        if (formal != g_formal_types.end() && formal->second->parameter)
            type = formal->second->declaringType;
    }
    type = dn2cpp_metadata_type_definition(type);
    if (!dn2cpp_recorded_type_definition(type))
        dn2cpp_metadata_definition_unavailable();
    if (const char* module = type->reflection().assemblyName)
        return module;
    // Canonical identity alone proves no module. Only these exact native
    // CoreLib handles may use its implicit assembly; other missing modules refuse.
    if (type == &dn2cpp_object_type || type == &dn2cpp_string_type || type == &dn2cpp_void_type
        || type == &dn2cpp_bool_type || type == &dn2cpp_char_type || type == &dn2cpp_byte_type
        || type == &dn2cpp_sbyte_type || type == &dn2cpp_int16_type || type == &dn2cpp_uint16_type
        || type == &dn2cpp_int32_type || type == &dn2cpp_uint32_type || type == &dn2cpp_int64_type
        || type == &dn2cpp_uint64_type || type == &dn2cpp_intptr_type || type == &dn2cpp_uintptr_type
        || type == &dn2cpp_single_type || type == &dn2cpp_double_type)
        return "System.Private.CoreLib";
    dn2cpp_metadata_definition_unavailable();
}

static bool dn2cpp_same_recorded_metadata_def(int32_t tokenA, const Dn2CppTypeInfo* ownerA,
    int32_t tokenB, const Dn2CppTypeInfo* ownerB)
{
    if (tokenA == 0 || tokenA != tokenB)
        return false;
    // Callers proved these are canonical definitions; one owner has one module.
    return ownerA == ownerB || std::strcmp(dn2cpp_metadata_definition_module(ownerA),
        dn2cpp_metadata_definition_module(ownerB)) == 0;
}

static bool dn2cpp_same_type_metadata_definition(const Dn2CppTypeInfo* a, const Dn2CppTypeInfo* b)
{
    if (a == nullptr || b == nullptr)
        dn2cpp_metadata_definition_unavailable();
    const bool compoundA = dn2cpp_metadata_compound_type(a);
    const bool compoundB = dn2cpp_metadata_compound_type(b);
    if (compoundA || compoundB)
    {
        // CLR assigns every element-bearing RuntimeType the typed-nil TypeDef
        // token. The leaf's module, not array rank or element identity, separates it.
        const auto validLeaf = [](const Dn2CppTypeInfo* type) {
            while (type != nullptr && dn2cpp_metadata_compound_type(type))
                type = type->elementType;
            return type != nullptr && (dn2cpp_recorded_formal_parameter(type)
                || dn2cpp_recorded_type_definition(dn2cpp_metadata_type_definition(type)));
        };
        if (!validLeaf(a) || !validLeaf(b))
            dn2cpp_metadata_definition_unavailable();
        return compoundA && compoundB && std::strcmp(dn2cpp_metadata_definition_module(a),
            dn2cpp_metadata_definition_module(b)) == 0;
    }
    const bool formalA = dn2cpp_recorded_formal_parameter(a);
    const bool formalB = dn2cpp_recorded_formal_parameter(b);
    if (formalA || formalB)
    {
        if ((!formalA && !dn2cpp_recorded_type_definition(dn2cpp_metadata_type_definition(a)))
            || (!formalB && !dn2cpp_recorded_type_definition(dn2cpp_metadata_type_definition(b))))
            dn2cpp_metadata_definition_unavailable();
        // Published formal nodes are interned by typical owner/MethodDef/position.
        return formalA && formalB && a == b;
    }
    a = dn2cpp_metadata_type_definition(a);
    b = dn2cpp_metadata_type_definition(b);
    if (!dn2cpp_recorded_type_definition(a) || !dn2cpp_recorded_type_definition(b))
        dn2cpp_metadata_definition_unavailable();
    const auto ar = a->reflection();
    const auto br = b->reflection();
    if (ar.metadataToken != 0 && br.metadataToken != 0)
        return dn2cpp_same_recorded_metadata_def(ar.metadataToken, a, br.metadataToken, b);
    return a == b;
}

static bool dn2cpp_native_member_header(const Dn2CppTypeInfo* type)
{
    return type == &dn2cpp_type_type || dn2cpp_is_methodref_header(type)
        || type == &dn2cpp_fieldinfo_type || type == &dn2cpp_propertyinfo_type || type == &dn2cpp_eventinfo_type;
}

template<class Row>
static bool dn2cpp_recorded_builtin_member(Dn2CppMetadataHandle<Row> member,
    const Dn2CppTypeInfo* owner, Dn2CppMetadataTable<Row> rows, int32_t count)
{
    if (!dn2cpp_recorded_type_definition(owner) || owner->reflection().metadataToken != 0)
        return false;
    for (int32_t i = 0; i < count; ++i)
        if (rows[i] == member)
            return true;
    return false;
}

template<class Row>
static bool dn2cpp_same_member_metadata_definition(Dn2CppMetadataHandle<Row> a, Dn2CppMetadataHandle<Row> b,
    Dn2CppMetadataTable<Row> aTable, int32_t aCount, Dn2CppMetadataTable<Row> bTable, int32_t bCount)
{
    const Row ar = *a;
    const Row br = *b;
    const auto* ao = dn2cpp_metadata_type_definition(ar.declaringType);
    const auto* bo = dn2cpp_metadata_type_definition(br.declaringType);
    if (!dn2cpp_recorded_type_definition(ao) || !dn2cpp_recorded_type_definition(bo))
        dn2cpp_metadata_definition_unavailable();
    if (ar.metadataToken != 0 && br.metadataToken != 0)
        return dn2cpp_same_recorded_metadata_def(ar.metadataToken, ao, br.metadataToken, bo);
    const bool builtinA = dn2cpp_recorded_builtin_member(a, ar.declaringType, aTable, aCount);
    const bool builtinB = dn2cpp_recorded_builtin_member(b, br.declaringType, bTable, bCount);
    if ((ar.metadataToken == 0 && !builtinA) || (br.metadataToken == 0 && !builtinB))
        dn2cpp_metadata_definition_unavailable();
    if (!dn2cpp_same_type_metadata_definition(ao, bo))
        return false;
    if (builtinA && builtinB)
        return a == b;
    dn2cpp_metadata_definition_unavailable();
}

int32_t dn2cpp_memberinfo_has_same_metadata(Dn2CppObject* member, Dn2CppObject* other)
{
    dn2cpp_memberref_require(member);
    if (!dn2cpp_native_member_header(member->type))
    {
        const int32_t slot = dn2cpp_memberinfo_has_same_metadata_slot;
        if (slot >= 0 && member->type->vtable != nullptr)
        {
            const void* target = member->type->vtable[slot];
            if (target != nullptr && !dn2cpp_is_vcall_trap(target))
                return reinterpret_cast<int32_t (*)(Dn2CppObject*, Dn2CppObject*)>(const_cast<void*>(target))(member, other);
        }
        dn2cpp_throw_platform_not_supported("HasSameMetadataDefinitionAs: this reflection receiver has no retained virtual implementation");
    }
    if (other == nullptr)
        dn2cpp_throw_argument_null_param("other");
    // Runtime implementations accept only their own runtime member kind; in
    // particular a custom MemberInfo's virtual token/module getters are not read.
    if (member->type != other->type)
        return 0;
    if (member->type == &dn2cpp_type_type)
        return dn2cpp_same_type_metadata_definition(reinterpret_cast<Dn2CppType*>(member)->typeInfo,
            reinterpret_cast<Dn2CppType*>(other)->typeInfo);
    if (dn2cpp_is_methodref_header(member->type))
    {
        const auto a = dn2cpp_open_method_definition(reinterpret_cast<Dn2CppMethodRef*>(member)->method);
        const auto b = dn2cpp_open_method_definition(reinterpret_cast<Dn2CppMethodRef*>(other)->method);
        if (a == nullptr || b == nullptr)
            dn2cpp_metadata_definition_unavailable();
        const Dn2CppMethodInfo ar = *a;
        const Dn2CppMethodInfo br = *b;
        const auto* ao = dn2cpp_metadata_type_definition(ar.declaringType);
        const auto* bo = dn2cpp_metadata_type_definition(br.declaringType);
        if (!dn2cpp_recorded_type_definition(ao) || !dn2cpp_recorded_type_definition(bo))
            dn2cpp_metadata_definition_unavailable();
        if (ar.metadataToken != 0 && br.metadataToken != 0)
            return dn2cpp_same_recorded_metadata_def(ar.metadataToken, ao, br.metadataToken, bo);
        const auto at = ar.declaringType->reflection();
        const auto bt = br.declaringType->reflection();
        const bool constructor = member->type == &dn2cpp_constructorinfo_type;
        const auto* ad = dn2cpp_meta_desc_of(a);
        const auto* bd = dn2cpp_meta_desc_of(b);
        if (ad != nullptr || bd != nullptr)
        {
            const bool knownA = ar.metadataToken != 0 || ad != nullptr
                || dn2cpp_recorded_builtin_member(a, ar.declaringType,
                    constructor ? at.ctors : at.methods, constructor ? at.ctorCount : at.methodCount);
            const bool knownB = br.metadataToken != 0 || bd != nullptr
                || dn2cpp_recorded_builtin_member(b, br.declaringType,
                    constructor ? bt.ctors : bt.methods, constructor ? bt.ctorCount : bt.methodCount);
            if (!knownA || !knownB)
                dn2cpp_metadata_definition_unavailable();
            if (!dn2cpp_same_type_metadata_definition(ao, bo))
                return 0;
            if (ad == nullptr || bd == nullptr)
                dn2cpp_metadata_definition_unavailable();
            return ad == bd;
        }
        return dn2cpp_same_member_metadata_definition(a, b, constructor ? at.ctors : at.methods,
            constructor ? at.ctorCount : at.methodCount, constructor ? bt.ctors : bt.methods,
            constructor ? bt.ctorCount : bt.methodCount);
    }
    if (member->type == &dn2cpp_fieldinfo_type)
    {
        const auto a = reinterpret_cast<Dn2CppFieldRef*>(member)->field;
        const auto b = reinterpret_cast<Dn2CppFieldRef*>(other)->field;
        if (a == nullptr || b == nullptr || a->declaringType == nullptr || b->declaringType == nullptr)
            dn2cpp_metadata_definition_unavailable();
        const auto at = a->declaringType->reflection();
        const auto bt = b->declaringType->reflection();
        return dn2cpp_same_member_metadata_definition(a, b, at.fields, at.fieldCount, bt.fields, bt.fieldCount);
    }
    if (member->type == &dn2cpp_propertyinfo_type)
    {
        const auto a = reinterpret_cast<Dn2CppPropRef*>(member)->prop;
        const auto b = reinterpret_cast<Dn2CppPropRef*>(other)->prop;
        if (a == nullptr || b == nullptr || a->declaringType == nullptr || b->declaringType == nullptr)
            dn2cpp_metadata_definition_unavailable();
        const auto at = a->declaringType->reflection();
        const auto bt = b->declaringType->reflection();
        return dn2cpp_same_member_metadata_definition(a, b, at.props, at.propCount, bt.props, bt.propCount);
    }
    const auto a = reinterpret_cast<Dn2CppEventRef*>(member)->event;
    const auto b = reinterpret_cast<Dn2CppEventRef*>(other)->event;
    if (a == nullptr || b == nullptr || a->declaringType == nullptr || b->declaringType == nullptr)
        dn2cpp_metadata_definition_unavailable();
    const auto at = a->declaringType->reflection();
    const auto bt = b->declaringType->reflection();
    return dn2cpp_same_member_metadata_definition(a, b, at.events, at.eventCount, bt.events, bt.eventCount);
}

Dn2CppObject* dn2cpp_type_get_member_same_metadata(Dn2CppType* t, Dn2CppObject* member)
{
    dn2cpp_type_require(t);
    if (member == nullptr)
        dn2cpp_throw_argument_null_param("member");
    // A query path: the answer is a member OF `t`, so its reflected type is `t`
    // whatever base-chain level the row was found at, as on .NET.
    if (dn2cpp_is_methodref_header(member->type))
    {
        Dn2CppMetadataHandle<Dn2CppMethodInfo> m = reinterpret_cast<Dn2CppMethodRef*>(member)->method;
        const auto memberRow = m.operator->();
        const char* assembly = memberRow->declaringType != nullptr
            ? memberRow->declaringType->reflection().assemblyName : nullptr;
        for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr; ti = dn2cpp_reflection_base(ti))
        {
            dn2cpp_require_metadata(ti);
            const auto reflection = ti->reflection();
            for (int32_t i = 0; i < reflection.methodCount; i++)
            {
                const auto mi = reflection.methods[i];
                if (mi == m || dn2cpp_same_metadata_def(
                        mi->metadataToken, reflection.assemblyName, memberRow->metadataToken, assembly))
                    return reinterpret_cast<Dn2CppObject*>(
                        mi->genericParamCount > 0 ? dn2cpp_make_methodref_defview(mi, t->typeInfo)
                            : dn2cpp_make_methodref(mi, t->typeInfo));
            }
            for (int32_t i = 0; i < reflection.ctorCount; i++)
            {
                const auto ci = reflection.ctors[i];
                if (ci == m || dn2cpp_same_metadata_def(
                        ci->metadataToken, reflection.assemblyName, memberRow->metadataToken, assembly))
                    return reinterpret_cast<Dn2CppObject*>(
                        dn2cpp_make_methodref(ci, t->typeInfo));
            }
        }
    }
    else if (member->type == &dn2cpp_fieldinfo_type)
    {
        Dn2CppMetadataHandle<Dn2CppFieldInfo> f = reinterpret_cast<Dn2CppFieldRef*>(member)->field;
        const auto memberRow = f.operator->();
        const char* assembly = memberRow->declaringType != nullptr
            ? memberRow->declaringType->reflection().assemblyName : nullptr;
        for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr; ti = dn2cpp_reflection_base(ti))
        {
            dn2cpp_require_metadata(ti);
            const auto reflection = ti->reflection();
            for (int32_t i = 0; i < reflection.fieldCount; i++)
            {
                const auto fi = reflection.fields[i];
                if (fi == f || dn2cpp_same_metadata_def(
                        fi->metadataToken, reflection.assemblyName, memberRow->metadataToken, assembly))
                    return reinterpret_cast<Dn2CppObject*>(
                        dn2cpp_make_fieldref(fi, t->typeInfo));
            }
        }
    }
    else if (member->type == &dn2cpp_propertyinfo_type)
    {
        Dn2CppMetadataHandle<Dn2CppPropInfo> p = reinterpret_cast<Dn2CppPropRef*>(member)->prop;
        const auto memberRow = p.operator->();
        const char* assembly = memberRow->declaringType != nullptr
            ? memberRow->declaringType->reflection().assemblyName : nullptr;
        for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr; ti = dn2cpp_reflection_base(ti))
        {
            dn2cpp_require_metadata(ti);
            const auto reflection = ti->reflection();
            for (int32_t i = 0; i < reflection.propCount; i++)
            {
                const auto pi = reflection.props[i];
                if (pi == p || dn2cpp_same_metadata_def(
                        pi->metadataToken, reflection.assemblyName, memberRow->metadataToken, assembly))
                    return reinterpret_cast<Dn2CppObject*>(
                        dn2cpp_make_propref(pi, t->typeInfo));
            }
        }
    }
    else if (member->type == &dn2cpp_eventinfo_type)
    {
        const auto event = reinterpret_cast<Dn2CppEventRef*>(member)->event;
        const auto memberRow = event.operator->();
        const char* assembly = dn2cpp_ti_assembly_name(memberRow->declaringType);
        for (auto* ti = t->typeInfo; ti != nullptr; ti = dn2cpp_reflection_base(ti))
        {
            dn2cpp_require_metadata(ti);
            const auto reflection = ti->reflection();
            for (int32_t i = 0; i < reflection.eventCount; i++)
            {
                const auto candidate = reflection.events[i];
                if (candidate == event || dn2cpp_same_metadata_def(candidate->metadataToken,
                    reflection.assemblyName, memberRow->metadataToken, assembly))
                    return reinterpret_cast<Dn2CppObject*>(dn2cpp_make_eventref(candidate, t->typeInfo));
            }
        }
    }
    else if (member->type == &dn2cpp_type_type)
    {
        const Dn2CppTypeInfo* mi = reinterpret_cast<Dn2CppType*>(member)->typeInfo;
        const auto memberReflection = mi->reflection();
        const auto reflection = t->typeInfo->reflection();
        for (int32_t i = 0; i < reflection.nestedCount; i++)
        {
            const Dn2CppTypeInfo* nt = reflection.nestedTypes[i];
            const auto nestedReflection = nt->reflection();
            if (nt == mi || dn2cpp_same_metadata_def(
                    nestedReflection.metadataToken, nestedReflection.assemblyName,
                    memberReflection.metadataToken, memberReflection.assemblyName))
                return reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(nt));
        }
    }
    Dn2CppString* name = dn2cpp_memberinfo_name(member);
    dn2cpp_throw_argument_sr(&dn2cpp_argument_exception_type, DN2CPP_SR_MEMBER_INFO_NOT_FOUND,
        "member", &name, 1);
}

// ---- reflection: custom attributes ----

// The custom-attribute table of a reflected element handle, dispatched on the managed
// object header (a Type / FieldInfo / MethodInfo|ConstructorInfo / PropertyInfo /
// ParameterInfo). Returns nullptr/0 for an element with no reflected attributes.
static Dn2CppMetadataTable<Dn2CppAttrInfo> dn2cpp_member_custom_attrs(Dn2CppObject* m, int32_t* count)
{
    *count = 0;
    if (m != nullptr && m->type == &dn2cpp_runtime_module_type)
    {
        const char* name = dn2cpp_asm_unwrap(m);
        for (int32_t i = 0; i < dn2cpp_assembly_registry_count; i++)
            if (std::strcmp(dn2cpp_assembly_registry[i].name, name) == 0)
            {
                *count = dn2cpp_assembly_registry[i].moduleCustomAttrCount;
                return dn2cpp_assembly_registry[i].moduleCustomAttrs;
            }
        return nullptr;
    }
    dn2cpp_memberref_require(m);
    if (m->type == &dn2cpp_type_type)
    {
        const Dn2CppTypeInfo* ti = reinterpret_cast<Dn2CppType*>(m)->typeInfo;
        *count = ti->reflection().customAttrCount;
        return ti->reflection().customAttrs;
    }
    if (m->type == &dn2cpp_fieldinfo_type)
    {
        Dn2CppMetadataHandle<Dn2CppFieldInfo> f = reinterpret_cast<Dn2CppFieldRef*>(m)->field;
        *count = f->customAttrCount;
        return f->customAttrs;
    }
    if (dn2cpp_is_methodref_header(m->type))
    {
        Dn2CppMetadataHandle<Dn2CppMethodInfo> mi = reinterpret_cast<Dn2CppMethodRef*>(m)->method;
        if ((mi->declaringType->flags & DN2CPP_TF_PATCH) != 0)
            dn2cpp_throw_platform_not_supported("MethodInfo: patch custom attributes are not recorded in the image");
        *count = mi->customAttrCount;
        return mi->customAttrs;
    }
    if (m->type == &dn2cpp_propertyinfo_type)
    {
        Dn2CppMetadataHandle<Dn2CppPropInfo> p = reinterpret_cast<Dn2CppPropRef*>(m)->prop;
        *count = p->customAttrCount;
        return p->customAttrs;
    }
    if (m->type == &dn2cpp_eventinfo_type)
    {
        const auto event = reinterpret_cast<Dn2CppEventRef*>(m)->event;
        *count = event->customAttrCount;
        return event->customAttrs;
    }
    if (m->type == &dn2cpp_parameterinfo_type)
    {
        const auto* parameter = reinterpret_cast<Dn2CppParamRef*>(m);
        if (parameter->owner != nullptr
            && (parameter->owner->declaringType->flags & DN2CPP_TF_PATCH) != 0)
            dn2cpp_throw_platform_not_supported("ParameterInfo: patch custom attributes are not recorded in the image");
        Dn2CppMetadataHandle<Dn2CppParamInfo> p = parameter->param;
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

// The element an attribute read was asked of: `member`, or the assembly named
// `assembly` when `member` is null.
static std::string dn2cpp_attr_owner_text(Dn2CppObject* member, const char* assembly)
{
    if (member == nullptr)
        return std::string("assembly ") + (assembly != nullptr ? assembly : "?");
    if (member->type == &dn2cpp_runtime_module_type)
        return std::string("module ") + dn2cpp_asm_unwrap(member);
    if (member->type == &dn2cpp_type_type)
        return dn2cpp_utf8_of(dn2cpp_type_tostring(reinterpret_cast<Dn2CppType*>(member)->typeInfo));
    auto subject = [](const Dn2CppTypeInfo* owner, const char* name) {
        return dn2cpp_unretained_subject(owner, name != nullptr ? name : "?");
    };
    if (member->type == &dn2cpp_fieldinfo_type)
    {
        const auto f = reinterpret_cast<Dn2CppFieldRef*>(member)->field;
        return subject(f->declaringType, f->name);
    }
    if (dn2cpp_is_methodref_header(member->type))
    {
        const auto mi = reinterpret_cast<Dn2CppMethodRef*>(member)->method;
        return subject(mi->declaringType, mi->name);
    }
    if (member->type == &dn2cpp_propertyinfo_type)
    {
        const auto p = reinterpret_cast<Dn2CppPropRef*>(member)->prop;
        return subject(p->declaringType, p->name);
    }
    if (member->type == &dn2cpp_eventinfo_type)
    {
        const auto e = reinterpret_cast<Dn2CppEventRef*>(member)->event;
        return subject(e->declaringType, e->name);
    }
    if (member->type == &dn2cpp_parameterinfo_type)
    {
        const auto* parameter = reinterpret_cast<Dn2CppParamRef*>(member);
        const char* name = parameter->param->name;
        std::string text = std::string("parameter ") + (name != nullptr ? name : "?");
        if (parameter->owner != nullptr)
            text += " of " + subject(parameter->owner->declaringType, parameter->owner->name);
        return text;
    }
    return "?";
}

// Materializes an attribute table as an object[] (each entry a fresh instance via
// its create factory), filtered to `filter` when non-null. Shared by the member
// element forms and the assembly forms below; `member` and `assembly` name the
// element read (dn2cpp_attr_owner_text). A row without a factory is one whose
// constructor, or a named argument's setter, the image did not retain: the read refuses
// before it constructs anything rather than answer a set missing it.
static Dn2CppArrayRef* dn2cpp_attr_table_to_array(Dn2CppMetadataTable<Dn2CppAttrInfo> tab, int32_t count,
                                                  const Dn2CppTypeInfo* filter, Dn2CppObject* member,
                                                  const char* assembly)
{
    int32_t n = 0;
    for (int32_t i = 0; i < count; i++)
        if (filter == nullptr || dn2cpp_type_is_a(tab[i]->attrType, filter))
        {
            if (tab[i]->create == nullptr)
            {
                const Dn2CppAttrInfo row = *tab[i];
                if (row.unretainedSetter != nullptr)
                    dn2cpp_throw_unretained(row.unretainedSetterOwner,
                        dn2cpp_unretained_subject(row.unretainedSetterOwner, row.unretainedSetter),
                        "reflection found this attribute on " + dn2cpp_attr_owner_text(member, assembly)
                            + " but the setter of its named argument was not retained",
                        "method", row.unretainedSetter);
                const Dn2CppTypeInfo* attribute = tab[i]->attrType;
                dn2cpp_throw_unretained(attribute, dn2cpp_unretained_subject(attribute, ".ctor"),
                    "reflection found this attribute on " + dn2cpp_attr_owner_text(member, assembly)
                        + " but its constructor was not retained",
                    "method", ".ctor");
            }
            n++;
        }
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
        if (filter == nullptr || dn2cpp_type_is_a(tab[i]->attrType, filter))
            dn2cpp_gc_store_ref(&arr->data[k++], tab[i]->create());
    return arr;
}

static Dn2CppObject* dn2cpp_attr_table_to_single(Dn2CppMetadataTable<Dn2CppAttrInfo> tab, int32_t count,
                                               const Dn2CppTypeInfo* filter, Dn2CppObject* member,
                                               const char* assembly)
{
    // A later matching constructor's fault takes precedence over ambiguity.
    Dn2CppArrayRef* attributes = dn2cpp_attr_table_to_array(tab, count, filter, member, assembly);
    if (attributes->length > 1)
        dn2cpp_throw_ambiguous_attribute(attributes->data[0]);
    return attributes->length == 0 ? nullptr : attributes->data[0];
}

// Walk one override at a time: an intermediate override may carry attributes
// absent from both the queried method and GetBaseDefinition's root.
static Dn2CppMethodRef* dn2cpp_attribute_base_method(Dn2CppMethodRef* method)
{
    const auto row = *dn2cpp_methodref_require(method);
    if ((row.ilAttrs & DN2CPP_MA_VIRTUAL) == 0 || (row.ilAttrs & DN2CPP_MA_NEWSLOT) != 0
        || (row.declaringType->flags & DN2CPP_TF_INTERFACE) != 0)
        return nullptr;
    const auto* declaring = dn2cpp_invoke_declaring(row.declaringType, method->reflectedType);
    Dn2CppMethodRef* root = nullptr;
    for (const auto* type = dn2cpp_reflection_base(declaring); type != nullptr;
         type = dn2cpp_reflection_base(type))
    {
        dn2cpp_require_metadata(type);
        const auto reflection = type->reflection();
        for (int32_t i = 0; i < reflection.methodCount; i++)
        {
            const auto candidate = reflection.methods[i];
            const auto base = *candidate;
            if ((base.ilAttrs & DN2CPP_MA_VIRTUAL) == 0 || std::strcmp(base.name, row.name) != 0)
                continue;
            if (row.vtableSlot >= 0 && !dn2cpp_symbolic_method(row))
            {
                if (base.vtableSlot == row.vtableSlot)
                    return dn2cpp_make_methodref(candidate, type);
                continue;
            }
            auto* view = base.genericParamCount > 0
                ? dn2cpp_make_methodref_defview(candidate, type) : dn2cpp_make_methodref(candidate, type);
            if (root == nullptr)
                root = dn2cpp_methodref_get_base_definition(method);
            auto* candidateRoot = dn2cpp_methodref_get_base_definition(view);
            if (root->method == candidateRoot->method)
                return view;
        }
    }
    return nullptr;
}

static std::vector<Dn2CppAttrInfo> dn2cpp_inherited_attributes(Dn2CppObject* member,
                                                            const Dn2CppTypeInfo* filter,
                                                            bool firstOnly = false)
{
    std::vector<Dn2CppAttrInfo> selected;
    std::vector<const Dn2CppTypeInfo*> seen;
    bool declared = true;
    for (auto* level = member; level != nullptr;)
    {
        int32_t count;
        const auto table = dn2cpp_member_custom_attrs(level, &count);
        const auto previous = seen.size();
        for (int32_t i = 0; i < count; i++)
        {
            const auto row = *table[i];
            if (filter != nullptr && !dn2cpp_type_is_a(row.attrType, filter))
                continue;
            if (!declared && (row.usageFlags & 1) != 0)
                continue;
            bool duplicate = false;
            if (!declared && (row.usageFlags & 2) == 0)
                for (std::size_t j = 0; j < previous; j++)
                    if (seen[j] == row.attrType)
                        duplicate = true;
            if (!duplicate)
            {
                selected.push_back(row);
                if (firstOnly)
                    return selected;
                seen.push_back(row.attrType);
            }
        }
        declared = false;
        if (level->type == &dn2cpp_type_type)
        {
            const auto* base = dn2cpp_reflection_base(reinterpret_cast<Dn2CppType*>(level)->typeInfo);
            level = base == nullptr ? nullptr : reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(base));
        }
        else if (level->type == &dn2cpp_methodinfo_type)
            level = reinterpret_cast<Dn2CppObject*>(dn2cpp_attribute_base_method(reinterpret_cast<Dn2CppMethodRef*>(level)));
        else
            break;
    }
    return selected;
}

Dn2CppArrayRef* dn2cpp_get_custom_attributes(Dn2CppObject* member, Dn2CppType* attrType, int32_t inherit)
{
    int32_t count;
    Dn2CppMetadataTable<Dn2CppAttrInfo> tab = dn2cpp_member_custom_attrs(member, &count);
    const Dn2CppTypeInfo* filter = (attrType != nullptr) ? attrType->typeInfo : nullptr;
    if (inherit != 0)
    {
        const auto rows = dn2cpp_inherited_attributes(member, filter);
        return dn2cpp_attr_table_to_array(rows.data(), static_cast<int32_t>(rows.size()), filter, member, nullptr);
    }
    return dn2cpp_attr_table_to_array(tab, count, filter, member, nullptr);
}

// Typed-filter form: the caller always names an attribute type (GetCustomAttribute<T> /
// GetCustomAttributes(Type) / IsDefined(Type)). A null attrType here means the named type
// is not in the reachable set (its type-info was never emitted, so typeof() folded to a
// null token) — which, by tree-shaking, is exactly a type no reflected element carries.
// So a null filter matches *nothing* (not everything): the query is well-formed in .NET,
// the type simply isn't present on the element. This differs from the no-argument
// GetCustomAttributes() forms, where a null filter legitimately means "all".
Dn2CppArrayRef* dn2cpp_get_custom_attributes_typed(Dn2CppObject* member, Dn2CppType* attrType, int32_t inherit)
{
    if (attrType == nullptr)
        return dn2cpp_newarr_ref(0);
    return dn2cpp_get_custom_attributes(member, attrType, inherit);
}

Dn2CppObject* dn2cpp_get_custom_attribute(Dn2CppObject* member, Dn2CppType* attrType, int32_t inherit)
{
    if (attrType == nullptr)
        return nullptr; // named type absent from the reachable set -> no match
    const Dn2CppTypeInfo* filter = attrType->typeInfo;
    int32_t count;
    Dn2CppMetadataTable<Dn2CppAttrInfo> tab = dn2cpp_member_custom_attrs(member, &count);
    if (inherit != 0)
    {
        const auto rows = dn2cpp_inherited_attributes(member, filter);
        return dn2cpp_attr_table_to_single(rows.data(), static_cast<int32_t>(rows.size()), filter, member, nullptr);
    }
    return dn2cpp_attr_table_to_single(tab, count, filter, member, nullptr);
}

int32_t dn2cpp_is_defined(Dn2CppObject* member, Dn2CppType* attrType, int32_t inherit)
{
    if (attrType == nullptr)
        return 0; // named type absent from the reachable set -> not defined
    const Dn2CppTypeInfo* filter = attrType->typeInfo;
    if (inherit != 0)
        return dn2cpp_inherited_attributes(member, filter, true).empty() ? 0 : 1;
    int32_t count;
    Dn2CppMetadataTable<Dn2CppAttrInfo> tab = dn2cpp_member_custom_attrs(member, &count);
    for (int32_t i = 0; i < count; i++)
        if (dn2cpp_type_is_a(tab[i]->attrType, filter))
            return 1;
    return 0;
}

// ---- reflection: assembly-level custom attributes ----

// The assembly-registry attribute table for an Assembly handle (its simple name),
// resolved by name compare — the handle from Type.Assembly and the one from
// GetEntryAssembly may be distinct pointers to the same name. Null/unknown -> none.
static Dn2CppMetadataTable<Dn2CppAttrInfo> dn2cpp_assembly_custom_attrs(const char* name, int32_t* count)
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
    Dn2CppMetadataTable<Dn2CppAttrInfo> tab = dn2cpp_assembly_custom_attrs(name, &count);
    const Dn2CppTypeInfo* filter = (attrType != nullptr) ? attrType->typeInfo : nullptr;
    return dn2cpp_attr_table_to_array(tab, count, filter, nullptr, name);
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
    Dn2CppMetadataTable<Dn2CppAttrInfo> tab = dn2cpp_assembly_custom_attrs(name, &count);
    return dn2cpp_attr_table_to_single(tab, count, filter, nullptr, name);
}

int32_t dn2cpp_assembly_is_defined(const char* name, Dn2CppType* attrType)
{
    if (attrType == nullptr)
        return 0;
    const Dn2CppTypeInfo* filter = attrType->typeInfo;
    int32_t count;
    Dn2CppMetadataTable<Dn2CppAttrInfo> tab = dn2cpp_assembly_custom_attrs(name, &count);
    for (int32_t i = 0; i < count; i++)
        if (dn2cpp_type_is_a(tab[i]->attrType, filter))
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
    { "System.Reflection.Assembly", &dn2cpp_object_type, nullptr, nullptr, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, nullptr };
const Dn2CppTypeInfo dn2cpp_module_type =
    { "System.Reflection.Module", &dn2cpp_object_type, nullptr, nullptr, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, nullptr };
const Dn2CppTypeInfo dn2cpp_runtime_assembly_type =
    { "System.Reflection.RuntimeAssembly", &dn2cpp_assembly_type, nullptr, nullptr, dn2cpp_asm_box_tostring, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (int32_t)sizeof(Dn2CppAsmBox), 0, 0, 0, 0, 0, nullptr };
const Dn2CppTypeInfo dn2cpp_runtime_module_type =
    { "System.Reflection.RuntimeModule", &dn2cpp_module_type, nullptr, nullptr, dn2cpp_mod_box_tostring, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (int32_t)sizeof(Dn2CppAsmBox), 0, 0, 0, 0, 0, nullptr };

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
static std::mutex& g_asmBoxMutex = dn2cpp_never_destroyed<std::mutex>();

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
    bool scoped, Dn2CppString* resourceName, const Dn2CppTypeInfo* byteArrayType)
{
    // .NET: the (string) overload rejects a null name without naming a parameter; the
    // (Type, string) overload allows a null NAME when the type supplies the key and
    // otherwise names `type`. An empty composed key is "String cannot have zero length."
    if (resourceName == nullptr)
    {
        if (!scoped)
            dn2cpp_throw_sr0(&dn2cpp_argument_null_exception_type, DN2CPP_SR_ARGUMENT_NULL_STRING);
        if (scope == nullptr)
            dn2cpp_throw_argument_null_param("type");
    }
    const std::string key = dn2cpp_manifest_resource_key(scope, resourceName);
    if (key.empty())
        dn2cpp_throw_sr0(&dn2cpp_argument_exception_type, DN2CPP_SR_STRING_ZERO_LENGTH);
    const Dn2CppAssemblyRegEntry* e = dn2cpp_assembly_reg_find(name);
    const Dn2CppManifestResource* r = dn2cpp_manifest_resource_find(e, key);
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
        dn2cpp_throw_sr0(&dn2cpp_argument_null_exception_type, DN2CPP_SR_ARGUMENT_NULL_STRING);
    if (resourceName->length == 0)
        dn2cpp_throw_sr0(&dn2cpp_argument_exception_type, DN2CPP_SR_STRING_ZERO_LENGTH);
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
    if (full == nullptr)
        return nullptr;
    Dn2CppString* comma = dn2cpp_string_from_utf8(", ", 2);
    Dn2CppString* asmName = dn2cpp_assembly_display_name(dn2cpp_type_assembly_name(t));
    return dn2cpp_string_concat3(full, comma, asmName);
}

// Assembly.GetType(name[, throwOnError[, ignoreCase]]): the type-name registry
// restricted to the receiver assembly's own rows. An assembly suffix starts at
// a comma outside generic arguments and array ranks; ignoreCase folds ASCII case
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
        int32_t depth = 0;
        for (int32_t i = 0; i < name->length; i++)
        {
            if (name->chars[i] == u'[')
                depth++;
            else if (name->chars[i] == u']')
                depth--;
            else if (name->chars[i] == u',' && depth == 0)
            {
                len = i;
                break;
            }
        }
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
            return std::strcmp(dn2cpp_ti_assembly_name(ti), asmName) == 0;
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
// ArgumentNullException and empty -> ArgumentException (sentence `emptyKey`), both
// naming `paramName`; Load's blank name is a FileLoadException. Returns the registry's own
// name pointer — the canonical Assembly handle, so op_Equality's pointer-or-strcmp
// compare holds against Type.Assembly / GetEntryAssembly handles.
static const char* dn2cpp_assembly_lookup_by_name(Dn2CppString* name, const char* paramName,
    const char* emptyKey, bool strictName)
{
    if (name == nullptr)
        dn2cpp_throw_argument_null_param(paramName);
    if (name->length == 0)
        dn2cpp_throw_argument_param(emptyKey, paramName);
    int32_t b = 0, e = name->length;
    for (int32_t i = 0; i < name->length; i++)
        if (name->chars[i] == u',') { e = i; break; }
    while (b < e && name->chars[b] <= u' ') b++;
    while (e > b && name->chars[e - 1] <= u' ') e--;
    if (b == e)
    {
        if (strictName)
            dn2cpp_throw_of_msg(&dn2cpp_file_load_exception_type, "The given assembly name was invalid.");
        dn2cpp_throw_argument();
    }
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

const char* dn2cpp_assembly_load(Dn2CppString* name, const char* paramName)
{
    const char* found = dn2cpp_assembly_lookup_by_name(name, paramName, DN2CPP_SR_EMPTY_STRING, true);
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

Dn2CppString* dn2cpp_assembly_name_missing_message(Dn2CppString* fileName)
{
    Dn2CppString* args[] = { fileName };
    Dn2CppString* message = dn2cpp_sr_message(DN2CPP_SR_ASSEMBLY_NOT_FOUND, args, 1);
#ifdef _WIN32
    auto* newline = dn2cpp_string_literal(u"\r\n", 2);
#else
    auto* newline = dn2cpp_string_literal(u"\n", 1);
#endif
    return dn2cpp_string_concat2(message, newline);
}

const char* dn2cpp_assembly_load_partial(Dn2CppString* name)
{
    // The obsolete partial-name form reports a miss as null, never a throw.
    return dn2cpp_assembly_lookup_by_name(name, "partialName", DN2CPP_SR_STRING_ZERO_LENGTH, false);
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
static Dn2CppArrayRef* dn2cpp_attr_table_to_data_array(Dn2CppMetadataTable<Dn2CppAttrInfo> tab, int32_t count)
{
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(count);
    for (int32_t i = 0; i < count; i++)
    {
        auto* d = static_cast<Dn2CppAttrDataRef*>(dn2cpp_alloc(sizeof(Dn2CppAttrDataRef)));
        d->type = &dn2cpp_customattributedata_type;
        d->attr = tab[i];
        dn2cpp_gc_store_ref(&arr->data[i], reinterpret_cast<Dn2CppObject*>(d));
    }
    return arr;
}

Dn2CppArrayRef* dn2cpp_member_custom_attributes_data(Dn2CppObject* member)
{
    int32_t count;
    Dn2CppMetadataTable<Dn2CppAttrInfo> tab = dn2cpp_member_custom_attrs(member, &count);
    return dn2cpp_attr_table_to_data_array(tab, count);
}

Dn2CppArrayRef* dn2cpp_assembly_custom_attributes_data(const char* name)
{
    int32_t count;
    Dn2CppMetadataTable<Dn2CppAttrInfo> tab = dn2cpp_assembly_custom_attrs(name, &count);
    return dn2cpp_attr_table_to_data_array(tab, count);
}

Dn2CppType* dn2cpp_attrdata_attribute_type(Dn2CppAttrDataRef* d)
{
    return dn2cpp_get_type_from_handle(dn2cpp_attrdataref_require(d)->attrType);
}

Dn2CppString* dn2cpp_reflection_handle_tostring(Dn2CppObject* member)
{
    dn2cpp_memberref_require(member);
    std::string symbolic;
    bool resolved = false;
    if (dn2cpp_is_methodref_header(member->type))
    {
        auto* method = reinterpret_cast<Dn2CppMethodRef*>(member);
        const auto row = method->method;
        if (dn2cpp_symbolic_method(*row))
        {
            const auto* declaring = dn2cpp_invoke_declaring(row->declaringType, method->reflectedType);
            symbolic = dn2cpp_reflection_type_spelling(dn2cpp_method_definition_type(row, "symbolic", row->returnType, declaring))
                + " " + row->name;
            if (row->genericParamCount > 0)
            {
                symbolic += '[';
                for (int32_t i = 0; i < row->genericParamCount; i++)
                {
                    if (i != 0)
                        symbolic += ',';
                    symbolic += dn2cpp_reflection_type_spelling(row->genericArgs != nullptr ? row->genericArgs[i]
                        : dn2cpp_method_parameter_type(row, i), true);
                }
                symbolic += ']';
            }
            symbolic += '(';
            for (int32_t i = 0; i < row->paramCount; i++)
            {
                if (i != 0)
                    symbolic += ", ";
                const auto* type = dn2cpp_method_definition_type(row, "symbolic", row->parameters[i]->paramType, declaring, i);
                symbolic += dn2cpp_signature_kind(type) == 2
                    ? dn2cpp_reflection_type_spelling(type->elementType) + " ByRef" : dn2cpp_reflection_type_spelling(type);
            }
            symbolic += ')';
            resolved = true;
        }
    }
    else if (member->type == &dn2cpp_parameterinfo_type)
    {
        auto* parameter = reinterpret_cast<Dn2CppParamRef*>(member);
        if (parameter->owner != nullptr && dn2cpp_symbolic_method(*parameter->owner))
        {
            symbolic = dn2cpp_reflection_type_spelling(dn2cpp_paramref_parameter_type(parameter)->typeInfo);
            if (parameter->param->name != nullptr)
                symbolic += std::string(" ") + parameter->param->name;
            resolved = true;
        }
    }
    else if (member->type == &dn2cpp_fieldinfo_type)
    {
        auto* field = reinterpret_cast<Dn2CppFieldRef*>(member);
        if ((field->field->declaringType->flags & DN2CPP_TF_GENERICDEF) != 0)
        {
            symbolic = dn2cpp_reflection_type_spelling(dn2cpp_fieldref_field_type(field)->typeInfo) + " " + field->field->name;
            resolved = true;
        }
    }
    else if (member->type == &dn2cpp_propertyinfo_type)
    {
        auto* property = reinterpret_cast<Dn2CppPropRef*>(member);
        if ((property->prop->declaringType->flags & DN2CPP_TF_GENERICDEF) != 0)
        {
            symbolic = dn2cpp_reflection_type_spelling(dn2cpp_property_signature_type(property->prop, property->reflectedType))
                + " " + property->prop->name;
            int32_t count;
            dn2cpp_prop_index_parameters(property->prop.operator->().operator->(), &count);
            if (count > 0)
            {
                symbolic += " [";
                for (int32_t i = 0; i < count; i++)
                {
                    if (i != 0)
                        symbolic += ", ";
                    symbolic += dn2cpp_reflection_type_spelling(dn2cpp_property_signature_type(property->prop, property->reflectedType, i));
                }
                symbolic += ']';
            }
            resolved = true;
        }
    }
    else if (member->type == &dn2cpp_eventinfo_type)
    {
        auto* event = reinterpret_cast<Dn2CppEventRef*>(member);
        symbolic = dn2cpp_reflection_type_spelling(dn2cpp_eventref_handler_type(event)->typeInfo) + " " + event->event->name;
        resolved = true;
    }
    if (resolved)
        return dn2cpp_string_from_utf8(symbolic.c_str(), static_cast<int32_t>(symbolic.size()));
    const char* display = nullptr;
    if (member->type == &dn2cpp_fieldinfo_type)
        display = dn2cpp_fieldref_require(reinterpret_cast<Dn2CppFieldRef*>(member))->display;
    else if (dn2cpp_is_methodref_header(member->type))
    {
        auto* m = reinterpret_cast<Dn2CppMethodRef*>(member);
        Dn2CppMetadataHandle<Dn2CppMethodInfo> mi = dn2cpp_methodref_require(m);
        display = m->isGenericDefView != 0 && mi->genericDefinitionDisplay != nullptr
            ? mi->genericDefinitionDisplay : mi->display;
    }
    else if (member->type == &dn2cpp_propertyinfo_type)
        display = dn2cpp_propref_require(reinterpret_cast<Dn2CppPropRef*>(member))->display;
    else if (member->type == &dn2cpp_parameterinfo_type)
        display = dn2cpp_paramref_require(reinterpret_cast<Dn2CppParamRef*>(member))->param->display;
    else if (member->type == &dn2cpp_customattributedata_type)
        display = dn2cpp_attrdataref_require(reinterpret_cast<Dn2CppAttrDataRef*>(member))->display;
    if (display == nullptr)
        return dn2cpp_type_tostring(member->type);
    return dn2cpp_metadata_string(display);
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
    if ((ti->flags & DN2CPP_TF_ARRAY) != 0)
    {
        const Dn2CppTypeInfo* element = ti->elementType;
        while (element != nullptr && (element->flags & DN2CPP_TF_ARRAY) != 0)
            element = element->elementType;
        if (dn2cpp_signature_kind(element) != 0)
            return 0x2101; // CLR arrays: public, sealed, serializable.
    }
    // Presence test: emitted types always stamp their (nonzero) metadata token,
    // so a genuine all-zero TypeAttributes word (an internal class with a static
    // ctor) is still served exactly rather than falling to the synthesis.
    if (ti->reflection().ilAttrs != 0 || ti->reflection().metadataToken != 0)
        return static_cast<int32_t>(ti->reflection().ilAttrs);
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

// Synthetic definitions stamp enclosing visibility at emission. Other handles
// answer from their own visibility only; generic arguments are not consulted.
int32_t dn2cpp_type_is_visible(const Dn2CppTypeInfo* ti)
{
    const Dn2CppTypeInfo* signature = ti;
    while ((signature->flags & DN2CPP_TF_ARRAY) != 0 && signature->elementType != nullptr)
        signature = signature->elementType;
    if (dn2cpp_signature_kind(signature) != 0)
        return dn2cpp_signature_is_visible(signature);
    if ((ti->flags & DN2CPP_TF_HIDDEN_ENCLOSING) != 0)
        return 0;
    int32_t vis = dn2cpp_type_il_attrs(ti) & DN2CPP_TA_VISMASK;
    return (vis == DN2CPP_TA_PUBLIC || vis == DN2CPP_TA_NESTEDPUB) ? 1 : 0;
}

int32_t dn2cpp_type_is_nested_public(const Dn2CppTypeInfo* ti)
{
    return (dn2cpp_type_il_attrs(ti) & DN2CPP_TA_VISMASK) == DN2CPP_TA_NESTEDPUB ? 1 : 0;
}

// A function pointer has a signature, but no element type.
int32_t dn2cpp_type_has_element_type(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_ARRAY) != 0 || dn2cpp_signature_kind(ti) == 1
        || dn2cpp_signature_kind(ti) == 2 ? 1 : 0;
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
    if (dn2cpp_signature_kind(t->typeInfo) == 2)
        dn2cpp_throw_type_load();
    if ((t->typeInfo->flags & DN2CPP_TF_BYREFLIKE) != 0)
        dn2cpp_throw_type_load();
    if (dn2cpp_has_signature_array_element(t->typeInfo))
        return dn2cpp_get_type_from_handle(dn2cpp_array_ti(t->typeInfo, 1, false));
    if (dn2cpp_type_contains_generic_parameters(t->typeInfo))
        return dn2cpp_get_type_from_handle(dn2cpp_array_ti(t->typeInfo, 1, false));
    if ((t->typeInfo->flags & DN2CPP_TF_ARRAY) != 0)
        return dn2cpp_get_type_from_handle(dn2cpp_array_ti(t->typeInfo, 1));
    for (int32_t k = 0; k < dn2cpp_type_registry_count; k++)
    {
        const Dn2CppTypeInfo* cand = dn2cpp_type_registry[k].type;
        if ((cand->flags & DN2CPP_TF_ARRAY) != 0 && cand->elementType == t->typeInfo
            && dn2cpp_is_sz_array(cand))
            return dn2cpp_get_type_from_handle(cand);
    }
    dn2cpp_throw_not_supported();
}

Dn2CppType* dn2cpp_type_make_array_type_rank(Dn2CppType* t, int32_t rank)
{
    dn2cpp_type_require(t);
    if (rank <= 0)
        dn2cpp_throw_index_out_of_range();
    if (dn2cpp_has_signature_array_element(t->typeInfo) && rank > 32)
        dn2cpp_throw_type_load();
    if (dn2cpp_signature_kind(t->typeInfo) == 2)
        dn2cpp_throw_type_load();
    if ((t->typeInfo->flags & DN2CPP_TF_BYREFLIKE) != 0)
        dn2cpp_throw_type_load();
    if (dn2cpp_has_signature_array_element(t->typeInfo))
        return dn2cpp_get_type_from_handle(dn2cpp_array_ti(t->typeInfo, rank, true));
    if (dn2cpp_type_contains_generic_parameters(t->typeInfo))
    {
        if (rank > 32)
            dn2cpp_throw_type_load();
        return dn2cpp_get_type_from_handle(dn2cpp_array_ti(t->typeInfo, rank, true));
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
const Dn2CppTypeInfo* dn2cpp_find_array_ti_rank(const Dn2CppTypeInfo* elem, int32_t rank, bool nonSz)
{
    for (int32_t k = 0; k < dn2cpp_type_registry_count; k++)
    {
        const Dn2CppTypeInfo* cand = dn2cpp_type_registry[k].type;
        if ((cand->flags & DN2CPP_TF_ARRAY) != 0 && cand->elementType == elem
            && cand->arrayRank == rank
            && (rank != 1 || dn2cpp_is_md_array(cand) == nonSz))
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
        dn2cpp_throw_argument_param(DN2CPP_SR_MUST_BE_ENUM, "enumType");
    const Dn2CppTypeInfo* ti = t->typeInfo;
    const Dn2CppTypeInfo* u = ti->enumUnderlying != nullptr ? ti->enumUnderlying : &dn2cpp_int32_type;
    const auto reflection = ti->reflection();
    int32_t n = reflection.enumMemberCount;
    const Dn2CppTypeInfo* arrTi = dn2cpp_find_array_ti(u);
    if (u == &dn2cpp_int32_type || u == &dn2cpp_uint32_type)
    {
        Dn2CppArrayI4* a = arrTi != nullptr ? dn2cpp_newarr_i4_t(n, arrTi) : dn2cpp_newarr_i4(n);
        for (int32_t i = 0; i < n; i++)
            a->data[i] = static_cast<int32_t>(reflection.enumMembers[i]->value);
        return reinterpret_cast<Dn2CppObject*>(a);
    }
    int32_t sz = (u == &dn2cpp_byte_type || u == &dn2cpp_sbyte_type) ? 1
        : (u == &dn2cpp_int16_type || u == &dn2cpp_uint16_type) ? 2
        : 8;
    Dn2CppArrayN* a = arrTi != nullptr ? dn2cpp_newarr_n_t(n, sz, arrTi) : dn2cpp_newarr_n(n, sz);
    for (int32_t i = 0; i < n; i++)
    {
        int64_t v = reflection.enumMembers[i]->value;
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
        dn2cpp_throw_argument_null_param("filter");
    auto* d = reinterpret_cast<Dn2CppDelegate*>(filter);
    auto fn = reinterpret_cast<int32_t (*)(Dn2CppObject*, Dn2CppObject*, Dn2CppObject*)>(d->method);
    const Dn2CppTypeInfo* ti = t->typeInfo;
    dn2cpp_type_ensure_metadata_relations(ti);
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
            dn2cpp_gc_store_ref(&keep[n++], itf);
    });
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&arr->data[i], keep[i]);
    return arr;
}

// Type.ImplementInterface (internal BCL helper behind TypeInfo.IsAssignableFrom):
// interface-set membership over the base chain's own tables and relation rows.
int32_t dn2cpp_type_implement_interface(Dn2CppType* t, Dn2CppType* itf)
{
    dn2cpp_type_require(t);
    if (itf == nullptr)
        return 0;
    dn2cpp_type_ensure_metadata_relations(t->typeInfo);
    const Dn2CppTypeInfo* target = itf->typeInfo;
    for (const Dn2CppTypeInfo* ti = t->typeInfo; ti != nullptr; ti = ti->base)
    {
        int32_t count;
        const auto* entries = dn2cpp_type_interfaces(ti, &count);
        for (int32_t i = 0; i < count; i++)
            if (entries[i].itf == target)
                return 1;
        int32_t n;
        const Dn2CppInterfaceEntry* rel = dn2cpp_relation_interfaces(ti, &n);
        for (int32_t i = 0; i < n; i++)
            if (rel[i].itf == target)
                return 1;
    }
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
    if (m->type == &dn2cpp_eventinfo_type)
        return 0x02;
    if (m->type == &dn2cpp_type_type)
    {
        // Formal parameters have a declaring owner but remain TypeInfo members.
        const int32_t flags = reinterpret_cast<Dn2CppType*>(m)->typeInfo->flags;
        return (flags & (DN2CPP_TF_NESTED | DN2CPP_TF_GENERICPARAM)) == DN2CPP_TF_NESTED
            ? 0x80  // MemberTypes.NestedType
            : 0x20; // MemberTypes.TypeInfo
    }
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
    if (m->type == &dn2cpp_eventinfo_type)
        return reinterpret_cast<Dn2CppEventRef*>(m)->event->metadataToken;
    if (m->type == &dn2cpp_type_type)
        return reinterpret_cast<Dn2CppType*>(m)->typeInfo->reflection().metadataToken;
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
    else if (m->type == &dn2cpp_eventinfo_type)
        ti = reinterpret_cast<Dn2CppEventRef*>(m)->event->declaringType;
    else if (m->type == &dn2cpp_type_type)
        ti = reinterpret_cast<Dn2CppType*>(m)->typeInfo;
    return dn2cpp_ti_assembly_name(ti);
}

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

// ---- reflection: generic definitions and closed instantiations ----

int32_t dn2cpp_methodref_generic_parameter_count(Dn2CppMethodRef* m)
{
    return dn2cpp_methodref_require(m)->genericParamCount;
}

int32_t dn2cpp_methodref_is_generic_def(Dn2CppMethodRef* m)
{
    dn2cpp_methodref_require(m); // the null-handle guard; the flag lives on the handle
    return m->isGenericDefView;
}

int32_t dn2cpp_methodref_contains_generic_parameters(Dn2CppMethodRef* m)
{
    const auto mi = dn2cpp_methodref_require(m);
    if (m->isGenericDefView != 0 || dn2cpp_type_contains_generic_parameters(dn2cpp_invoke_declaring(mi->declaringType, m->reflectedType)))
        return 1;
    if (mi->genericArgs != nullptr)
        for (int32_t i = 0; i < mi->genericParamCount; i++)
            if (dn2cpp_type_contains_generic_parameters(mi->genericArgs[i]))
                return 1;
    return 0;
}

// Definitions expose formal parameters; compiled rows expose their closed arguments.
Dn2CppArrayRef* dn2cpp_methodref_get_generic_arguments(Dn2CppMethodRef* m)
{
    Dn2CppMetadataHandle<Dn2CppMethodInfo> mi = dn2cpp_methodref_require(m);
    const Dn2CppMethodInfo row = *mi;
    int32_t n = m->isGenericDefView != 0 || row.genericArgs != nullptr ? row.genericParamCount : 0;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(n);
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&arr->data[i],
            reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(m->isGenericDefView != 0
                ? dn2cpp_method_parameter_type(mi, i) : row.genericArgs[i])));
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
    return dn2cpp_make_methodref_defview(m->method,
        dn2cpp_invoke_declaring(m->method->declaringType, m->reflectedType));
}

// MethodInfo.MakeGenericMethod(Type[]): in-image resolution. Rows on the
// declaring type sharing the receiver's definition metadata token are the
// definition's compiled instantiations; match their closed arguments against
// the request. An instantiation the transpile never generated cannot be built
// at runtime (the IL2CPP AOT boundary, like MakeGenericType) — it throws a
// catchable PlatformNotSupportedException naming the missing instantiation.
Dn2CppMethodRef* dn2cpp_methodref_make_generic(Dn2CppMethodRef* m, Dn2CppArrayRef* types)
{
    Dn2CppMetadataHandle<Dn2CppMethodInfo> def = dn2cpp_methodref_require(m);
    if (types == nullptr)
        dn2cpp_throw_argument_null_param("methodInstantiation");
    if ((def->attrs & DN2CPP_MTHA_GENERIC) == 0 || m->isGenericDefView == 0)
        dn2cpp_throw_invalid_operation();
    int32_t argc = types->length;
    // .NET rejects a null element, without naming a parameter, before the arity.
    for (int32_t i = 0; i < argc; i++)
        if (types->data[i] == nullptr)
            dn2cpp_throw_argument_null();
    if (argc != def->genericParamCount)
        dn2cpp_throw_sr2(&dn2cpp_argument_exception_type, DN2CPP_SR_NOT_ENOUGH_GEN_ARGUMENTS,
            dn2cpp_format_int(argc, 4, nullptr),
            dn2cpp_format_int(def->genericParamCount, 4, nullptr));
    bool open = dn2cpp_type_contains_generic_parameters(dn2cpp_invoke_declaring(def->declaringType, m->reflectedType));
    std::vector<const Dn2CppTypeInfo*> arguments;
    for (int32_t i = 0; i < argc; i++)
    {
        const auto* type = dn2cpp_type_require(reinterpret_cast<Dn2CppType*>(types->data[i]));
        bool parameter = dn2cpp_type_contains_generic_parameters(type);
        if (!parameter && dn2cpp_type_metadata_only(type))
            dn2cpp_throw_platform_not_supported("Reflection: generic method argument has no runtime layout");
        open |= parameter;
        arguments.push_back(type);
    }
    for (int32_t i = 0; i < argc; i++)
        if (dn2cpp_signature_kind(reinterpret_cast<Dn2CppType*>(types->data[i])->typeInfo) != 0)
            dn2cpp_throw_argument_msg("A pointer, by-reference or function-pointer type cannot be a generic argument.");
    if (open)
        return dn2cpp_make_methodref(dn2cpp_open_method(def,
            dn2cpp_invoke_declaring(def->declaringType, m->reflectedType), arguments), m->reflectedType);
    // A metadata-answerable member has no compiled instantiations to resolve
    // against: its answer is a function of a concrete type argument, including
    // one no static call site named.
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
    if (const auto selected = dn2cpp_delegate_find_method(ti, def->metadataToken, argc,
            [&arguments](int32_t i) { return arguments[static_cast<size_t>(i)]; }))
        return dn2cpp_make_methodref(selected, m->reflectedType);
    std::string closed = def->name != nullptr ? def->name : "?";
    closed += "[";
    for (int32_t i = 0; i < argc; i++)
    {
        if (i > 0)
            closed += ",";
        closed += dn2cpp_utf8_of(dn2cpp_type_tostring(reinterpret_cast<Dn2CppType*>(types->data[i])->typeInfo));
    }
    dn2cpp_throw_unretained(ti, dn2cpp_unretained_subject(ti, closed + "]"), kDn2CppUnretainedClosedGeneric,
        nullptr, nullptr);
}

// The Object row the override chain through `mi` roots at: `mi` is a ValueType row, an
// emitted override dn2cpp_object_slot_settles accepts on its level, which roots there
// whatever the levels above declare, or one whose levels up to Object all pass
// dn2cpp_meta_level_passes, since a level without the rows could hold the new slot the
// chain really roots at. Null otherwise.
static Dn2CppMetadataHandle<Dn2CppMethodInfo> dn2cpp_meta_object_base(
    Dn2CppMetadataHandle<Dn2CppMethodInfo> mi, const Dn2CppMethodInfo& row)
{
    const Dn2CppMetaMember* d = nullptr;
    if ((row.attrs & DN2CPP_MTHA_METAANSWER) != 0)
    {
        const Dn2CppMetaMember* own = dn2cpp_meta_desc_of(mi);
        if (own != nullptr && (own->ilAttrs & DN2CPP_MA_VIRTUAL) != 0
            && (own->ilAttrs & DN2CPP_MA_NEWSLOT) == 0)
            d = dn2cpp_meta_object_counterpart(own);
    }
    else if ((row.ilAttrs & DN2CPP_MA_NEWSLOT) == 0)
        d = dn2cpp_meta_object_virtual_named(row);
    if (d == nullptr)
        return {};
    if (row.invoker == reinterpret_cast<const void*>(&dn2cpp_delegate_object_virtual_invoker))
        return dn2cpp_meta_row(d, &dn2cpp_object_type, nullptr, 0);
    if ((row.attrs & DN2CPP_MTHA_METAANSWER) == 0 && dn2cpp_object_slot_settles(row.declaringType, d, row))
        return dn2cpp_meta_row(d, &dn2cpp_object_type, nullptr, 0);
    for (const Dn2CppTypeInfo* ti = dn2cpp_meta_next_level(row.declaringType); ti != nullptr;
         ti = dn2cpp_meta_next_level(ti))
    {
        if (dn2cpp_meta_declares(ti, d))
            return dn2cpp_meta_row(d, ti, nullptr, 0);
        if (!dn2cpp_meta_level_passes(ti))
            return {};
        const auto reflection = ti->reflection();
        for (int32_t i = 0; i < reflection.methodCount; i++)
        {
            const Dn2CppMethodInfo level = *reflection.methods[i];
            if ((level.ilAttrs & DN2CPP_MA_NEWSLOT) != 0 && dn2cpp_meta_object_virtual_named(level) == d)
                return {};
        }
    }
    return {};
}

// MethodInfo.GetBaseDefinition: walk the base chain for the shallowest ancestor
// declaring a row on the same vtable slot (slot numbering is chain-consistent:
// a derived vtable extends its base, an override keeps the base's slot, and a
// `new` redeclaration gets a fresh slot — so slot equality is definition
// identity; the name check guards the interface-row slot numbering). A
// non-virtual method (slot -1) is its own base definition. A class's generic
// virtual row has no slot: its base definition is the generic method definition
// that introduces its override chain, as a definition view. A base whose member
// table was trimmed (unreached non-app-module rows) yields the deepest
// surviving declaration — best-effort, like the rest of the AOT surface.
Dn2CppMethodRef* dn2cpp_methodref_get_base_definition(Dn2CppMethodRef* m)
{
    Dn2CppMetadataHandle<Dn2CppMethodInfo> mi = dn2cpp_methodref_require(m);
    Dn2CppMethodInfo row = *mi;
    if (dn2cpp_symbolic_method(row) && (row.ilAttrs & DN2CPP_MA_VIRTUAL) != 0)
    {
        auto root = mi;
        const auto* declaring = dn2cpp_invoke_declaring(row.declaringType, m->reflectedType);
        const auto* rootType = declaring;
        for (const auto* type = dn2cpp_reflection_base(declaring);
             type != nullptr && (root->ilAttrs & DN2CPP_MA_NEWSLOT) == 0; type = dn2cpp_reflection_base(type))
        {
            dn2cpp_require_metadata(type);
            const auto reflection = type->reflection();
            for (int32_t i = 0; i < reflection.methodCount; i++)
            {
                const auto candidate = reflection.methods[i];
                if ((candidate->ilAttrs & DN2CPP_MA_VIRTUAL) != 0 && std::strcmp(candidate->name, row.name) == 0
                    && dn2cpp_symbolic_parameters_equal(mi, candidate, m->reflectedType, true))
                {
                    root = candidate;
                    rootType = type;
                    break;
                }
            }
        }
        if (const auto objectRoot = dn2cpp_meta_object_base(root, *root))
            return dn2cpp_make_methodref(objectRoot, nullptr);
        return row.genericParamCount > 0 ? dn2cpp_make_methodref_defview(root, rootType) : dn2cpp_make_methodref(root, rootType);
    }
    if (row.vtableSlot < 0)
    {
        if (!dn2cpp_is_gvm_row(row) || (row.declaringType->flags & DN2CPP_TF_INTERFACE) != 0)
        {
            const Dn2CppMetadataHandle<Dn2CppMethodInfo> root = dn2cpp_meta_object_base(mi, row);
            return root ? dn2cpp_make_methodref(root, nullptr) : m;
        }
        Dn2CppMetadataHandle<Dn2CppMethodInfo> root = mi;
        const Dn2CppGvmSlot slot = dn2cpp_gvm_slot(row);
        for (const Dn2CppTypeInfo* ti = dn2cpp_reflection_base(row.declaringType);
             ti != nullptr && dn2cpp_is_gvm_override(row); ti = dn2cpp_reflection_base(ti))
        {
            if (slot.known() && ti != slot.type)
                continue;
            dn2cpp_require_metadata(ti);
            const auto reflection = ti->reflection();
            for (int32_t i = 0; i < reflection.methodCount; i++)
            {
                const auto candidate = reflection.methods[i];
                const Dn2CppMethodInfo base = *candidate;
                if (base.vtableSlot < 0 && dn2cpp_is_gvm_row(base)
                    && (slot.known() ? base.metadataToken == slot.token : dn2cpp_gvm_same_signature(row, base)))
                {
                    root = candidate;
                    row = base;
                    break;
                }
            }
            if (slot.known())
                break;
        }
        return dn2cpp_make_methodref_defview(root, nullptr);
    }
    Dn2CppMetadataHandle<Dn2CppMethodInfo> best = mi;
    Dn2CppMethodInfo bestRow = row;
    for (const Dn2CppTypeInfo* ti = dn2cpp_reflection_base(row.declaringType); ti != nullptr; ti = dn2cpp_reflection_base(ti))
    {
        dn2cpp_require_metadata(ti);
        const auto reflection = ti->reflection();
        for (int32_t i = 0; i < reflection.methodCount; i++)
        {
            const auto candidate = reflection.methods[i];
            const Dn2CppMethodInfo base = *candidate;
            if (base.vtableSlot == row.vtableSlot && std::strcmp(base.name, row.name) == 0)
            {
                best = candidate;
                bestRow = base;
                break;
            }
        }
    }
    if (const Dn2CppMetadataHandle<Dn2CppMethodInfo> root = dn2cpp_meta_object_base(best, bestRow))
        return dn2cpp_make_methodref(root, nullptr);
    // Declaring-normalized (null): .NET's GetBaseDefinition answers the base declaring
    // type's own instance, so a derived-reflected receiver does not propagate.
    // `best == mi` short-circuits only when the receiver IS declaring-reflected — a
    // derived-reflected handle to a method declared on its own type re-mints the
    // declaring-typed one below, which is .NET's answer there too.
    return best == mi && m->reflectedType == row.declaringType
        ? m : dn2cpp_make_methodref(best, nullptr);
}

Dn2CppArrayRef* dn2cpp_methodref_get_parameter_types(Dn2CppMethodRef* m)
{
    const Dn2CppMethodInfo row = *dn2cpp_methodref_require(m);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(row.paramCount);
    for (int32_t i = 0; i < row.paramCount; i++)
    {
        const Dn2CppParamInfo parameter = *row.parameters[i];
        dn2cpp_gc_store_ref(&arr->data[i], reinterpret_cast<Dn2CppObject*>(
            dn2cpp_get_type_from_handle((m->isGenericDefView != 0 || (row.declaringType->flags & DN2CPP_TF_GENERICDEF) != 0) && parameter.genericDefinitionKey != nullptr
                ? dn2cpp_method_definition_type(m->method, parameter.genericDefinitionKey, parameter.paramType,
                    dn2cpp_invoke_declaring(row.declaringType, m->reflectedType), i)
                : dn2cpp_reflection_parameter_type(parameter, dn2cpp_invoke_declaring(row.declaringType, m->reflectedType)))));
    }
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
    return reinterpret_cast<Dn2CppObject*>(p->ownerGenericDefView != 0
        ? dn2cpp_make_methodref_defview(p->owner, p->ownerReflected)
        : dn2cpp_make_methodref(p->owner, p->ownerReflected));
}

int32_t dn2cpp_paramref_attributes(Dn2CppParamRef* p)
{
    return dn2cpp_paramref_require(p)->param->ilAttrs;
}

int32_t dn2cpp_paramref_has_default_value(Dn2CppParamRef* p)
{
    const auto parameter = dn2cpp_paramref_require(p)->param.operator->();
    return parameter->defaultValue != nullptr || (parameter->ilAttrs & 0x1000) != 0 ? 1 : 0;
}

int32_t dn2cpp_paramref_is_optional(Dn2CppParamRef* p)
{
    return (dn2cpp_paramref_require(p)->param->ilAttrs & 0x10) != 0 ? 1 : 0; // ParameterAttributes.Optional
}

Dn2CppArrayRef* dn2cpp_paramref_custom_modifiers(Dn2CppParamRef* p, int32_t required)
{
    Dn2CppMetadataHandle<Dn2CppParamInfo> pi = dn2cpp_paramref_require(p)->param;
    if (pi->customModifiersKnown == 0)
        dn2cpp_throw_platform_not_supported(
            "ParameterInfo.GetCustomModifiers: the signature modifiers are not recorded in the AOT image");
    const Dn2CppTypeInfo* const* modifiers = required
        ? pi->requiredCustomModifiers : pi->optionalCustomModifiers;
    int32_t count = required
        ? pi->requiredCustomModifierCount : pi->optionalCustomModifierCount;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(count);
    for (int32_t i = 0; i < count; i++)
        dn2cpp_gc_store_ref(&arr->data[i], reinterpret_cast<Dn2CppObject*>(
            dn2cpp_get_type_from_handle(modifiers[i])));
    return arr;
}

// CoreCLR's primitive MethodTable element type unwraps enums, but reports
// ordinary classes (including String/Object) as CLASS rather than signature codes.
static int32_t enum_cor_element_type(const Dn2CppTypeInfo* type)
{
    if (type == nullptr || dn2cpp_signature_kind(type) != 0
        || (type->flags & DN2CPP_TF_GENERICPARAM) != 0)
        dn2cpp_throw_not_supported_msg("Enum element type requires a runtime MethodTable type");
    if ((type->flags & DN2CPP_TF_ENUM) != 0)
        type = type->enumUnderlying != nullptr ? type->enumUnderlying : &dn2cpp_int32_type;
    int32_t primitive = dn2cpp_prim_code(type);
    if (primitive >= 0) return primitive + 2;
    if (type == &dn2cpp_void_type) return 0x01;
    if ((type->flags & DN2CPP_TF_VALUETYPE) != 0
        && type->name != nullptr && std::strcmp(type->name, "System.TypedReference") == 0) return 0x16;
    if (type == &dn2cpp_intptr_type) return 0x18;
    if (type == &dn2cpp_uintptr_type) return 0x19;
    if ((type->flags & DN2CPP_TF_ARRAY) != 0)
        return dn2cpp_is_md_array(type) ? 0x14 : 0x1D;
    return (type->flags & DN2CPP_TF_VALUETYPE) != 0 ? 0x11 : 0x12;
}

int32_t dn2cpp_enum_cor_element_type(Dn2CppObject* e)
{
    dn2cpp_memberref_require(e);
    return enum_cor_element_type(e->type);
}

int32_t dn2cpp_enum_type_cor_element_type(Dn2CppType* type)
{
    dn2cpp_type_require(type);
    return enum_cor_element_type(type->typeInfo);
}

// Enum's private primitive-type table has null holes and no SZARRAY entry.
// Unlike its public counterpart, this query does not validate an enum argument.
Dn2CppType* dn2cpp_enum_internal_get_underlying_type(Dn2CppType* type)
{
    int32_t code = dn2cpp_enum_type_cor_element_type(type);
    if (static_cast<uint32_t>(code) >= 26)
        dn2cpp_throw_index_out_of_range();
    if (!((code >= 2 && code <= 13) || code == 0x18 || code == 0x19))
        return nullptr;
    const Dn2CppTypeInfo* underlying = type->typeInfo;
    if ((underlying->flags & DN2CPP_TF_ENUM) != 0)
        underlying = underlying->enumUnderlying != nullptr ? underlying->enumUnderlying : &dn2cpp_int32_type;
    return dn2cpp_get_type_from_handle(underlying);
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
        dn2cpp_throw_argument_param(DN2CPP_SR_MUST_BE_TYPE, "type");
    if (size < 0)
        dn2cpp_throw_argument_out_of_range_value(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "size", size);
    return dn2cpp_alloc_pinned(static_cast<size_t>(size));
}

// ---- reflection: boxed-value conversion (Array.SetValue / Activator argument binding) ----
// The element<->box cores and the primitive-code/width queries below have
// external linkage (declared in dn2cpp_core.h): the non-generic System.Array
// GetValue/SetValue/CreateInstance surface, which lives in
// dn2cpp_system_array.cpp, reads element slots through them. They stay in
// this unit beside the Activator argument binder, which shares the
// Nullable/enum layout and numeric adaptation rules.

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

// Read the source box at its own width; target enum validation follows value validation.
static Dn2CppString* enum_get_name_boxed(Dn2CppType* t, Dn2CppObject* value)
{
    if (value == nullptr)
        dn2cpp_throw_argument_null_param("value");
    int32_t code;
    int64_t i;
    double r;
    if (!dn2cpp_read_boxed_num(value, &code, &i, &r)
        || code == DN2CPP_PC_BOOL || code == DN2CPP_PC_R4 || code == DN2CPP_PC_R8)
        dn2cpp_throw_argument_param(DN2CPP_SR_MUST_BE_ENUM_BASE_TYPE_OR_ENUM, "value");
    return dn2cpp_enum_get_name_u64(t, static_cast<uint64_t>(i));
}

Dn2CppString* dn2cpp_enum_get_name(Dn2CppType* t, Dn2CppObject* value)
{
    if (t == nullptr)
        dn2cpp_throw_argument_null_param("enumType");
    return enum_get_name_boxed(t, value);
}

Dn2CppString* dn2cpp_type_get_enum_name(Dn2CppType* t, Dn2CppObject* value)
{
    dn2cpp_type_require(t);
    return enum_get_name_boxed(t, value);
}

// An explicit call to the retained managed body validates value before using this.
Dn2CppString* dn2cpp_runtime_type_get_enum_name(Dn2CppType* t, Dn2CppObject* value)
{
    return enum_get_name_boxed(t, value);
}

// IsEnumDefined accepts a name, the exact underlying primitive, or the same enum.
// Validate before reading a payload; unlike GetEnumName, integer widths do not coerce.
static int32_t enum_is_defined_boxed(Dn2CppType* t, Dn2CppObject* value)
{
    if (value == nullptr)
        dn2cpp_throw_argument_null_param("value");
    const Dn2CppTypeInfo* ti = dn2cpp_enum_type_require(t);
    const Dn2CppTypeInfo* source = value->type;
    const auto reflection = ti->reflection();
    if (source == &dn2cpp_string_type)
    {
        auto* text = reinterpret_cast<Dn2CppString*>(value);
        int32_t size = dn2cpp_string_to_utf8(text, nullptr, 0);
        std::string name(static_cast<size_t>(size), '\0');
        dn2cpp_string_to_utf8(text, name.data(), size);
        // Literal field rows retain aliases, unlike the deduplicated value/name table.
        for (int32_t n = 0; n < reflection.fieldCount; n++)
        {
            const auto field = reflection.fields[n].operator->();
            if ((field->attrs & DN2CPP_FLDA_LITERAL) != 0 && name == field->name)
                return 1;
        }
        return 0;
    }
    if ((source->flags & DN2CPP_TF_ENUM) != 0)
    {
        if (source != ti)
            dn2cpp_throw_sr2(&dn2cpp_argument_exception_type, DN2CPP_SR_ENUM_AND_OBJECT_MUST_BE_SAME_TYPE,
                dn2cpp_type_tostring(source), dn2cpp_type_tostring(ti));
        source = source->enumUnderlying != nullptr ? source->enumUnderlying : &dn2cpp_int32_type;
    }
    int32_t code = dn2cpp_prim_code(source);
    if (code < DN2CPP_PC_CHAR || code > DN2CPP_PC_U8)
        dn2cpp_throw_sr0(&dn2cpp_invalid_operation_exception_type, DN2CPP_SR_UNKNOWN_ENUM_TYPE);
    const Dn2CppTypeInfo* underlying = ti->enumUnderlying != nullptr ? ti->enumUnderlying : &dn2cpp_int32_type;
    if (source != underlying)
        dn2cpp_throw_sr2(&dn2cpp_argument_exception_type, DN2CPP_SR_ENUM_UNDERLYING_AND_OBJECT_MUST_BE_SAME_TYPE,
            dn2cpp_type_tostring(source), dn2cpp_type_tostring(underlying));
    int64_t bits;
    double unused;
    dn2cpp_read_boxed_num(value, &code, &bits, &unused);
    int32_t width = dn2cpp_prim_storage_width(code);
    uint64_t mask = width == 8 ? UINT64_MAX : (uint64_t{1} << (width * 8)) - 1;
    uint64_t target = static_cast<uint64_t>(bits) & mask;
    for (int32_t n = 0; n < reflection.fieldCount; n++)
    {
        const auto field = reflection.fields[n].operator->();
        if ((field->attrs & DN2CPP_FLDA_LITERAL) != 0
            && (static_cast<uint64_t>(field->literalValue) & mask) == target)
            return 1;
    }
    return 0;
}

int32_t dn2cpp_enum_is_defined(Dn2CppType* t, Dn2CppObject* value)
{
    if (t == nullptr)
        dn2cpp_throw_argument_null_param("enumType");
    return enum_is_defined_boxed(t, value);
}

// Raw retained calls validate value before dereferencing this; callvirt checks at entry.
int32_t dn2cpp_runtime_type_is_enum_defined(Dn2CppType* t, Dn2CppObject* value)
{
    if (value == nullptr)
        dn2cpp_throw_argument_null_param("value");
    dn2cpp_type_require(t);
    return enum_is_defined_boxed(t, value);
}

int32_t dn2cpp_type_is_enum_defined(Dn2CppType* t, Dn2CppObject* value)
{
    dn2cpp_type_require(t);
    return enum_is_defined_boxed(t, value);
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
        dn2cpp_throw_argument_null_param("value");
    int32_t code;
    int64_t i;
    double r;
    if (!dn2cpp_read_boxed_num(value, &code, &i, &r)
        || code == DN2CPP_PC_R4 || code == DN2CPP_PC_R8)
        dn2cpp_throw_argument_param(DN2CPP_SR_MUST_BE_ENUM_BASE_TYPE_OR_ENUM, "value");
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

// The same detection as Nullable.GetUnderlyingType.
const Dn2CppTypeInfo* dn2cpp_nullable_underlying_ti(const Dn2CppTypeInfo* ti)
{
    if (ti != nullptr && ti->genericArgCount == 1 && ti->genericDef != nullptr
        && ti->genericArgs != nullptr && ti->genericDef->name != nullptr
        && std::strcmp(ti->genericDef->name, "System.Nullable`1") == 0)
        return ti->genericArgs[0];
    return nullptr;
}

// The transpiled Nullable<U> struct layout is { hasValue; U value; } with the
// value field at U's natural alignment. A primitive or enum U reports its code and
// storage width (alignment == width there). Any other U reports DN2CPP_PC_NONE and
// sits at the Nullable's extent less its own, since a struct's size is a multiple of
// its alignment. Returns false when an extent is unknown.
static bool dn2cpp_nullable_layout(const Dn2CppTypeInfo* nullable, const Dn2CppTypeInfo* u,
    int32_t* valueOffset, int32_t* width, int32_t* uCode)
{
    const Dn2CppTypeInfo* eff = u;
    if ((u->flags & DN2CPP_TF_ENUM) != 0)
        eff = u->enumUnderlying != nullptr ? u->enumUnderlying : &dn2cpp_int32_type;
    int32_t c = dn2cpp_prim_code(eff);
    if (c >= 0)
    {
        int32_t w = dn2cpp_prim_storage_width(c);
        *valueOffset = w; // hasValue byte, padded to the value's alignment
        *width = w;
        *uCode = c;
        return true;
    }
    if (((nullable->flags | u->flags) & DN2CPP_TF_LAYOUT_UNKNOWN) != 0
        || u->instanceSize <= 0 || nullable->instanceSize <= u->instanceSize)
        return false;
    *valueOffset = nullable->instanceSize - u->instanceSize;
    *width = u->instanceSize;
    *uCode = DN2CPP_PC_NONE;
    return true;
}

// Exact nullable payload packing is shared by raw boxes and reflection's byref
// cells. Numeric conversion here would alter signaling NaNs before copyback.
static void dn2cpp_nullable_store_payload(void* destination, int32_t size,
    int32_t offset, int32_t width, const void* payload)
{
    std::memset(destination, 0, static_cast<size_t>(size));
    if (payload != nullptr)
    {
        *static_cast<uint8_t*>(destination) = 1;
        dn2cpp_gc_memmove_refs(static_cast<char*>(destination) + offset, payload, static_cast<size_t>(width));
    }
    else
        dn2cpp_gc_write_barrier_if_heap(destination);
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
static int32_t dn2cpp_array_headerless_kind(const Dn2CppTypeInfo* elem)
{
    if (elem == nullptr || elem->name == nullptr)
        return 0;
    if (std::strcmp(elem->name, "System.Globalization.CultureInfo") == 0)
        return 1;
    if (std::strcmp(elem->name, "System.Globalization.NumberFormatInfo") == 0)
        return 2;
    if (std::strcmp(elem->name, "System.Globalization.TextInfo") == 0)
        return 3;
    if (std::strcmp(elem->name, "System.Reflection.Assembly") == 0)
        return 4;
    if (std::strcmp(elem->name, "System.Reflection.Module") == 0)
        return 5;
    return 0;
}

bool dn2cpp_array_has_raw_ref_element(const Dn2CppTypeInfo* elem)
{
    return dn2cpp_array_headerless_kind(elem) != 0;
}

Dn2CppObject* dn2cpp_byref_load_ref(const void* slot)
{
    auto* owner = dn2cpp_raw_md_owner(slot);
    if (owner != nullptr)
    {
        uintptr_t address = reinterpret_cast<uintptr_t>(slot);
        uintptr_t data = reinterpret_cast<uintptr_t>(owner->data);
        size_t cells = 1;
        for (int32_t i = 0; i < owner->rank; i++)
            cells *= static_cast<size_t>(owner->lengths[i]);
        if (owner->elemSize == sizeof(void*) && address >= data && (address - data) / sizeof(void*) < cells
            && (address - data) % sizeof(void*) == 0)
            return dn2cpp_array_load_ref(owner->type->elementType, slot, true);
    }
    return dn2cpp_array_load_ref(nullptr, slot, false);
}

Dn2CppObject* dn2cpp_array_load_ref(const Dn2CppTypeInfo* elem, const void* slot, bool rawReference)
{
    Dn2CppObject* value;
    std::memcpy(&value, slot, sizeof(value));
    if (!rawReference)
        return value;
    int32_t kind = dn2cpp_array_headerless_kind(elem);
    if (kind >= 1 && kind <= 3)
        return dn2cpp_nfi_wrap(reinterpret_cast<const Dn2CppNumberFormatInfo*>(value),
            kind == 1 ? DN2CPP_NFI_KIND_CULTURE : kind == 2 ? DN2CPP_NFI_KIND_NFI : DN2CPP_NFI_KIND_TEXTINFO);
    if (kind >= 4)
        return dn2cpp_asm_wrap(reinterpret_cast<const char*>(value),
            kind == 4 ? DN2CPP_ASM_KIND_ASSEMBLY : DN2CPP_ASM_KIND_MODULE);
    return value;
}

void dn2cpp_array_store_ref(const Dn2CppTypeInfo* elem, void* slot, Dn2CppObject* value, bool rawReference)
{
    if (rawReference)
    {
        int32_t kind = dn2cpp_array_headerless_kind(elem);
        if (kind >= 1 && kind <= 3)
            value = reinterpret_cast<Dn2CppObject*>(const_cast<Dn2CppNumberFormatInfo*>(dn2cpp_nfi_unwrap(value)));
        else if (kind >= 4)
            value = reinterpret_cast<Dn2CppObject*>(const_cast<char*>(dn2cpp_asm_unwrap(value)));
    }
    std::memcpy(slot, &value, sizeof(value));
    dn2cpp_gc_write_barrier_if_heap(slot);
}

void dn2cpp_array_store_boxed(Dn2CppObject* v, const Dn2CppTypeInfo* elem,
                              void* dst, int32_t elemSize, bool elemIsRef, bool rawReference)
{
    if (dn2cpp_signature_kind(elem) != 0)
        dn2cpp_throw_sr0(&dn2cpp_not_supported_exception_type, DN2CPP_SR_TYPE_NOT_SUPPORTED);
    if (elemIsRef)
    {
        if (v != nullptr
            && dn2cpp_type_is_assignable_from(dn2cpp_get_type_from_handle(elem),
                                              dn2cpp_get_type_from_handle(v->type)) == 0)
            dn2cpp_throw_array_store_mismatch();
        dn2cpp_array_store_ref(elem, dst, v, rawReference);
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
        if (!dn2cpp_nullable_layout(elem, u, &off, &w, &uc))
            dn2cpp_throw_platform_not_supported(
                "Array.SetValue into a Nullable<T> element whose T has no known layout is not supported");
        dn2cpp_nullable_store_payload(dst, elemSize, off, w, v + 1);
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
            dn2cpp_throw_sr0(&dn2cpp_argument_exception_type, DN2CPP_SR_PRIM_WIDEN);
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
                                       int32_t elemSize, bool elemIsRef, bool rawReference)
{
    if (dn2cpp_signature_kind(elem) != 0)
        dn2cpp_throw_sr0(&dn2cpp_not_supported_exception_type, DN2CPP_SR_TYPE_NOT_SUPPORTED);
    if (elemIsRef)
    {
        return dn2cpp_array_load_ref(elem, src, rawReference);
    }
    if (const Dn2CppTypeInfo* u = dn2cpp_nullable_underlying_ti(elem))
    {
        int32_t off, w, uc;
        if (!dn2cpp_nullable_layout(elem, u, &off, &w, &uc))
            dn2cpp_throw_platform_not_supported(
                "Boxing a Nullable<T> whose T has no known layout is not supported");
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

Dn2CppObject* dn2cpp_rebox_from_nullable(Dn2CppObject* value)
{
    if (value == nullptr || dn2cpp_nullable_underlying_ti(value->type) == nullptr)
        return value;
    return dn2cpp_array_box_element(value->type, value + 1, value->type->instanceSize, false);
}

// The payload is already validated as U; copying its packed bytes preserves
// floating-point bit patterns and managed references without binder conversions.
static Dn2CppObject* dn2cpp_nullable_box_payload(const Dn2CppTypeInfo* nullable,
    const Dn2CppTypeInfo* underlying, const void* payload)
{
    int32_t offset, width, code;
    if (!dn2cpp_nullable_layout(nullable, underlying, &offset, &width, &code))
        dn2cpp_throw_platform_not_supported(
            "Reflection: binding a Nullable<T> argument whose T has no known layout is not supported");
    int32_t size = nullable->instanceSize > 0 ? nullable->instanceSize : offset + width;
    auto* box = static_cast<Dn2CppObject*>(dn2cpp_alloc(sizeof(Dn2CppObject) + static_cast<size_t>(size)));
    box->type = nullable;
    dn2cpp_nullable_store_payload(box + 1, size, offset, width, payload);
    return box;
}

Dn2CppObject* dn2cpp_rebox_to_nullable(Dn2CppObject* value, Dn2CppType* type)
{
    const auto* nullable = type != nullptr ? type->typeInfo : nullptr;
    const auto* underlying = dn2cpp_nullable_underlying_ti(nullable);
    if (underlying == nullptr)
        dn2cpp_throw_invalid_operation_msg("Nullable reboxing requires a closed Nullable<T> runtime type.");
    return dn2cpp_nullable_box_payload(nullable, underlying,
        value != nullptr ? dn2cpp_unbox_nullable(value, underlying) : nullptr);
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

// Array.ConstrainedCopy's refusal of any pair that is not a plain move, .NET's own
// message.
[[noreturn]] static void dn2cpp_throw_constrained_copy_mismatch()
{
    const char* m = "Array.ConstrainedCopy will only work on array types that are provably compatible, "
                    "without any form of boxing, unboxing, widening, or casting of each array element.  "
                    "Change the array types (i.e., copy a Derived[] to a Base[]), or use a mitigation "
                    "strategy in the CER for Array.Copy's less powerful reliability contract, such as "
                    "cloning the array or throwing away the potentially corrupt destination array.";
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
    bool rawReference;
};

static Dn2CppArrCopyView dn2cpp_array_copy_view(Dn2CppObject* a)
{
    const Dn2CppTypeInfo* t = a->type;
    if (dn2cpp_is_md_array(t))
    {
        auto* md = reinterpret_cast<Dn2CppMDArray*>(a);
        const Dn2CppTypeInfo* el = t->elementType;
        bool isRef = dn2cpp_array_element_is_reference(el);
        return { md->data, static_cast<size_t>(md->elemSize), isRef, el, isRef && dn2cpp_array_headerless_kind(el) != 0 };
    }
    const Dn2CppTypeInfo* el = t != nullptr ? t->elementType : nullptr;
    bool isRef = el == nullptr ? (t == &dn2cpp_array_ref_type)
                               : dn2cpp_array_element_is_reference(el);
    if (isRef)
    {
        auto* r = static_cast<Dn2CppArrayRef*>(a);
        return { reinterpret_cast<char*>(r->data), sizeof(Dn2CppObject*), true, el, false };
    }
    bool isI4 = el == &dn2cpp_int32_type || el == &dn2cpp_uint32_type
        || (el != nullptr && (el->flags & DN2CPP_TF_ENUM) != 0
            && (el->enumUnderlying == &dn2cpp_int32_type
                || el->enumUnderlying == &dn2cpp_uint32_type));
    if (isI4)
    {
        auto* i = static_cast<Dn2CppArrayI4*>(a);
        return { reinterpret_cast<char*>(i->data), sizeof(int32_t), false, el, false };
    }
    auto* n = static_cast<Dn2CppArrayN*>(a);
    return { n->data, static_cast<size_t>(n->elemSize), false, el, false };
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
//     to the destination's, a per-element cast check when the reverse holds or
//     either element type is an interface;
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
                               Dn2CppObject* dst, int32_t dstIdx, int32_t len, bool reliable)
{
    // ConstrainedCopy checks this ahead of CopySlow, so its refusal replaces every other
    // one, the incompatible pair's included.
    auto refuse = [reliable]() {
        if (reliable)
            dn2cpp_throw_constrained_copy_mismatch();
        dn2cpp_throw_array_copy_mismatch();
    };
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
        refuse();
    }
    if (se == de && s.rawReference == d.rawReference)
    {
        dn2cpp_gc_memmove_refs(dp, sp, static_cast<size_t>(len) * s.stride);
        return;
    }
    if (dn2cpp_signature_kind(se) != 0 || dn2cpp_signature_kind(de) != 0)
    {
        // Array.Copy admits immediate pointer compatibility, independently of
        // the invariant array type-test rule. No boxed element is involved.
        if (dn2cpp_signature_kind(se) == 1 && dn2cpp_signature_kind(de) == 1
            && dn2cpp_typeinfo_assignable(se, de) != 0)
        {
            std::memmove(dp, sp, static_cast<size_t>(len) * s.stride);
            return;
        }
        refuse();
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
                if (reliable)
                    refuse();
                for (int32_t k = 0; k < len; k++)
                {
                    int64_t i;
                    double r;
                    dn2cpp_read_prim_storage(sp + static_cast<size_t>(k) * s.stride, cs, &i, &r);
                    dn2cpp_write_prim(dp + static_cast<size_t>(k) * d.stride, cd, cs, i, r);
                }
                return;
            }
            refuse();
        }
        // IntPtr/UIntPtr: one normalization class of their own, with no widening
        // row — nint[] <-> nuint[] copies raw; nint[] -> long[] refuses.
        if ((es == &dn2cpp_intptr_type || es == &dn2cpp_uintptr_type)
            && (ed == &dn2cpp_intptr_type || ed == &dn2cpp_uintptr_type))
        {
            dn2cpp_gc_memmove_refs(dp, sp, static_cast<size_t>(len) * s.stride);
            return;
        }
        refuse();
    }
    if (sv)
    {
        if (reliable)
            refuse();
        // Boxing copy: value elements into a compatible reference-element array.
        if (!dn2cpp_array_copy_box_ok(se, de))
            refuse();
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
        if (reliable)
            refuse();
        // Unboxing copy: exact element type per element (dn2cpp_array_store_boxed
        // is NOT reused for the primitive case — its SetValue contract widens,
        // and Copy's unbox arm does not); only the Nullable<U> element shares its
        // arm, whose exact-U-or-null rule IS the Copy rule.
        if (!dn2cpp_array_copy_box_ok(de, se))
            refuse();
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
    bool assignable = dn2cpp_typeinfo_assignable(se, de) != 0;
    if (assignable && s.rawReference == d.rawReference)
    {
        dn2cpp_gc_memmove_refs(dp, sp, static_cast<size_t>(len) * s.stride);
        return;
    }
    if (!assignable && (reliable || (dn2cpp_typeinfo_assignable(de, se) == 0
        && (se->flags & DN2CPP_TF_INTERFACE) == 0
        && (de->flags & DN2CPP_TF_INTERFACE) == 0)))
        refuse();
    for (int32_t k = 0; k < len; k++)
    {
        Dn2CppObject* v = dn2cpp_array_load_ref(se, sp + static_cast<size_t>(k) * s.stride, s.rawReference);
        if (v != nullptr && dn2cpp_typeinfo_assignable(v->type, de) == 0)
            dn2cpp_throw_array_copy_elem_cast();
        char* slot = dp + static_cast<size_t>(k) * d.stride;
        dn2cpp_array_store_ref(de, slot, v, d.rawReference);
    }
    return;
}

// ---- reflection: Activator.CreateInstance with constructor arguments ----

// DefaultBinder.CanChangePrimitive excludes ushort -> char; Invoke's
// CanPrimitiveWiden admits it. The remaining primitive pairs are shared.
static bool dn2cpp_binder_prim_widens(int32_t src, int32_t dst)
{
    return !(src == DN2CPP_PC_U2 && dst == DN2CPP_PC_CHAR)
        && dn2cpp_prim_widens(src, dst);
}

// Whether the boxed argument binds to a parameter of type `p` under the
// DefaultBinder rules: null matches EVERY parameter type
// (value types construct from default); a reference parameter checks
// assignability; a Nullable<U> parameter takes exactly a boxed U; an enum
// parameter its exact enum type; a primitive parameter admits the
// CanChangePrimitive conversions (an enum argument widening as its underlying).
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
    return ac >= 0 && dn2cpp_binder_prim_widens(ac, pc);
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
        return ac >= 0 && bc >= 0 && dn2cpp_binder_prim_widens(ac, bc);
    }
    return false;
}

// The type the DefaultBinder matches a parameter against: a by-ref parameter's
// referent, or the parameter type.
static const Dn2CppTypeInfo* dn2cpp_binder_param_type(const Dn2CppParamInfo& p)
{
    return (p.passKind & DN2CPP_PASS_BYREF) != 0 && p.passType != nullptr ? p.passType : p.paramType;
}

// Whether the boxed argument binds to the parameter. A pointer or by-ref-like
// parameter is assignable from no boxed value, so only null binds to it.
static bool dn2cpp_binder_param_matches(Dn2CppObject* a, const Dn2CppParamInfo& p)
{
    if (a != nullptr && (p.passKind & (DN2CPP_PASS_POINTER | DN2CPP_PASS_BYREFLIKE)) != 0)
        return false;
    return dn2cpp_binder_arg_matches(a, dn2cpp_binder_param_type(p));
}

// Whether candidate `x`'s whole parameter list is at least as specific as `y`'s and
// strictly more specific somewhere.
static bool dn2cpp_binder_dominates(Dn2CppMetadataHandle<Dn2CppMethodInfo> x,
    Dn2CppMetadataHandle<Dn2CppMethodInfo> y, int32_t argc)
{
    bool gtAny = false;
    for (int32_t j = 0; j < argc; j++)
    {
        const Dn2CppTypeInfo* px = dn2cpp_binder_param_type(*x->parameters[j]);
        const Dn2CppTypeInfo* py = dn2cpp_binder_param_type(*y->parameters[j]);
        if (!dn2cpp_binder_param_at_least_as_specific(px, py))
            return false;
        if (px != py)
            gtAny = true;
    }
    return gtAny;
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
        if (a == nullptr || a->type == u)
            return dn2cpp_nullable_box_payload(p, u, a != nullptr ? a + 1 : nullptr);
        int32_t off, w, uc;
        if (!dn2cpp_nullable_layout(p, u, &off, &w, &uc))
            dn2cpp_throw_platform_not_supported(
                "Reflection: binding a Nullable<T> argument whose T has no known layout is not supported");
        int32_t sz = p->instanceSize > 0 ? p->instanceSize : off + w;
        auto* box = static_cast<Dn2CppObject*>(dn2cpp_alloc(sizeof(Dn2CppObject) + static_cast<size_t>(sz)));
        box->type = p;
        if (a != nullptr)
        {
            char* value = reinterpret_cast<char*>(box + 1) + off;
            *reinterpret_cast<uint8_t*>(box + 1) = 1;
            if (uc < 0)
            {
                dn2cpp_gc_memmove_refs(value, a + 1, static_cast<size_t>(w));
            }
            else
            {
                int32_t sc;
                int64_t i;
                double r;
                dn2cpp_read_boxed_num(a, &sc, &i, &r);
                dn2cpp_write_prim(value, uc, sc, i, r);
            }
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
        case DN2CPP_PC_R4: case DN2CPP_PC_R8:
            // A UInt64 source converts as unsigned, like every other unsigned one.
            dn2cpp_write_prim(box + 1, pc, sc, i, r);
            break;
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

// A matching arity that rejects the arguments retains the binder's inner fault.
[[noreturn]] static void dn2cpp_throw_missing_constructor(const Dn2CppTypeInfo* ti, bool bound)
{
    static constexpr char member[] = "Member not found.";
    Dn2CppObject* inner = nullptr;
    if (bound)
    {
        inner = dn2cpp_exception_new(&dn2cpp_missing_method_exception_type,
            dn2cpp_string_from_utf8(member, static_cast<int32_t>(sizeof member - 1)), nullptr);
        reinterpret_cast<Dn2CppExceptionObject*>(inner)->hresult = static_cast<int32_t>(0x80131513u);
    }
    static constexpr char prefix[] = "Constructor on type '";
    static constexpr char suffix[] = "' not found.";
    Dn2CppString* name = dn2cpp_type_fullname(ti);
    if (name == nullptr)
    {
        const char* fallback = ti->name != nullptr ? ti->name : "?";
        name = dn2cpp_string_from_utf8(fallback, static_cast<int32_t>(std::strlen(fallback)));
    }
    Dn2CppString* message = dn2cpp_string_concat3(
        dn2cpp_string_from_utf8(prefix, static_cast<int32_t>(sizeof prefix - 1)),
        name,
        dn2cpp_string_from_utf8(suffix, static_cast<int32_t>(sizeof suffix - 1)));
    Dn2CppObject* e = dn2cpp_exception_new(&dn2cpp_missing_method_exception_type, message, inner);
    reinterpret_cast<Dn2CppExceptionObject*>(e)->hresult = static_cast<int32_t>(0x80131513u);
    dn2cpp_throw(e);
}

// Activator.CreateInstance(Type, object[] args[, BindingFlags, Binder,
// activationAttributes]): DefaultBinder-style constructor resolution matching real
// .NET — candidates filter on BindingFlags + arity + per-argument admissibility;
// several candidates resolve to the unique most-specific one (AmbiguousMatchException
// when none dominates, e.g. a null argument against (int) and (string) ctors); no
// candidate is MissingMethodException. A non-null Binder is the AOT
// PlatformNotSupportedException (the shared member-lookup posture); non-empty
// activation attributes throw PlatformNotSupportedException like real .NET. Flags
// with no lookup bit (BindingFlags & 0xFF) bind Public | Instance | CreateInstance.
Dn2CppObject* dn2cpp_activator_create_instance_args(Dn2CppType* t, Dn2CppArrayRef* args,
                                                    int32_t bindingFlags, Dn2CppObject* binder,
                                                    Dn2CppArrayRef* activationAttrs)
{
    if (t == nullptr)
        dn2cpp_throw_argument_null_param("type");
    if ((bindingFlags & 0xFF) == 0)
        bindingFlags |= DN2CPP_BF_PUBLIC | DN2CPP_BF_INSTANCE | 0x200;
    dn2cpp_reflection_check_binder(binder);
    if (activationAttrs != nullptr && activationAttrs->length > 0)
        dn2cpp_throw_platform_not_supported("Activation Attributes are not supported.");
    const Dn2CppTypeInfo* ti = t->typeInfo;
    int32_t argc = (args == nullptr) ? 0 : args->length;
    if (dn2cpp_type_contains_generic_parameters(ti))
        dn2cpp_throw_sr0(&dn2cpp_argument_exception_type, DN2CPP_SR_TYPE_NOT_SUPPORTED);
    if (dn2cpp_type_metadata_only(ti))
        dn2cpp_throw_platform_not_supported("Reflection: the constraint type has no runtime layout");
    const bool wrapExceptions = (bindingFlags & DN2CPP_BF_DO_NOT_WRAP_EXCEPTIONS) == 0;
    if (argc == 0)
        return dn2cpp_activator_create_default(
            t, (bindingFlags & DN2CPP_BF_NONPUBLIC) != 0 ? 1 : 0, wrapExceptions, true);
    if ((ti->flags & (DN2CPP_TF_ABSTRACT | DN2CPP_TF_INTERFACE)) != 0)
        dn2cpp_throw_missing_method("Cannot create an instance of an abstract class or interface");
    if (argc > 30)
        dn2cpp_throw_platform_not_supported("Activator: more than 30 constructor arguments");
    Dn2CppMethodCandidates cands;
    bool arityMatched = false;
    for (int32_t c = 0; c < ti->reflection().ctorCount; c++)
    {
        Dn2CppMetadataHandle<Dn2CppMethodInfo> ci = ti->reflection().ctors[c];
        if (!dn2cpp_member_matches(ci->attrs, bindingFlags, false) || ci->paramCount != argc)
            continue;
        arityMatched = true;
        bool ok = true;
        for (int32_t j = 0; j < argc && ok; j++)
            ok = dn2cpp_binder_param_matches(args->data[j], *ci->parameters[j]);
        if (ok)
            cands.push_back(ci);
    }
    const int32_t n = cands.size();
    if (n == 0)
        dn2cpp_throw_missing_constructor(ti, arityMatched);
    Dn2CppMetadataHandle<Dn2CppMethodInfo> best = nullptr;
    if (n == 1)
        best = cands[0];
    else
    {
        // The unique candidate that dominates every rival.
        for (int32_t x = 0; x < n && best == nullptr; x++)
        {
            bool dominates = true;
            for (int32_t y = 0; y < n && dominates; y++)
                if (x != y)
                    dominates = dn2cpp_binder_dominates(cands[x], cands[y], argc);
            if (dominates)
                best = cands[x];
        }
        // The message names the candidate .NET's pairwise walk ends on: the first,
        // replaced by each later one that dominates it.
        if (best == nullptr)
        {
            int32_t named = 0;
            for (int32_t y = 1; y < n; y++)
                if (dn2cpp_binder_dominates(cands[y], cands[named], argc))
                    named = y;
            dn2cpp_throw_ambiguous_member(reinterpret_cast<Dn2CppObject*>(dn2cpp_make_methodref(cands[named], ti)));
        }
    }
    // A parameter with DN2CPP_PASS_* bits takes its argument unconverted, since
    // the constructor call checks it, and a by-ref argument's copy is written back
    // into `args`.
    Dn2CppObject* adapted[30];
    bool byRef[30];
    const auto parameters = best->parameters;
    for (int32_t j = 0; j < argc; j++)
    {
        const auto param = parameters[j].operator->();
        byRef[j] = (param->passKind & DN2CPP_PASS_BYREF) != 0;
        adapted[j] = param->passKind != 0 ? args->data[j]
                                          : dn2cpp_binder_adapt_arg(args->data[j], param->paramType);
    }
    Dn2CppObject* instance = dn2cpp_ctor_invoke_argv(best, adapted, argc, wrapExceptions);
    for (int32_t j = 0; j < argc; j++)
        if (byRef[j])
            dn2cpp_gc_store_ref(&args->data[j], adapted[j]);
    return instance;
}

Dn2CppObject* dn2cpp_assembly_create_instance(const char* asmName, Dn2CppString* name,
    int32_t ignoreCase, int32_t bindingFlags, Dn2CppObject* binder, Dn2CppArrayRef* args,
    Dn2CppArrayRef* activationAttrs)
{
    Dn2CppType* t = dn2cpp_assembly_get_type(asmName, name, 0, ignoreCase);
    if (t == nullptr)
        return nullptr;
    return dn2cpp_activator_create_instance_args(t, args, bindingFlags, binder, activationAttrs);
}
