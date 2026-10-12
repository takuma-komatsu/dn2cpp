#include "dn2cpp_core.h"
#include "dn2cpp_metadata_native.h"
#include "dn2cpp_metadata.cpp"
#include <array>
#include <cstdio>
#include <cstdlib>
#include <initializer_list>
#include <limits>
#include <utility>

namespace {
void require(bool condition, const char* invariant)
{
    if (!condition)
    {
        std::fprintf(stderr, "metadata codec: %s\n", invariant);
        std::exit(1);
    }
}

template<class Action> void require_rejected(Action action, const char* invariant)
{
    bool rejected = false;
    try { action(); }
    catch (Dn2CppException& ex)
    {
        rejected = ex.obj != nullptr && ex.obj->type == &dn2cpp_invalid_operation_exception_type;
        dn2cpp_exc_inflight_pop(ex.obj);
    }
    require(rejected, invariant);
}

struct Record
{
    alignas(2) std::array<uint8_t, 512> bytes{};
    std::size_t length = 0;
};

void append_unsigned(Record& record, uint64_t value)
{
    do
    {
        uint8_t byte = static_cast<uint8_t>(value & 127);
        value >>= 7;
        record.bytes[record.length++] = byte | (value != 0 ? 128 : 0);
    } while (value != 0);
}

Record record(uint64_t block, uint64_t presence,
    std::initializer_list<uint64_t> values, std::size_t padded_length = 0)
{
    Record payload;
    append_unsigned(payload, presence);
    for (uint64_t value : values)
        append_unsigned(payload, value);
    std::size_t total = padded_length;
    for (;;)
    {
        Record result;
        append_unsigned(result, block);
        append_unsigned(result, total);
        std::size_t required = (result.length + payload.length + 1) & ~std::size_t(1);
        if (total < required)
        {
            total = required;
            continue;
        }
        require(total <= result.bytes.size() && (total & 1) == 0, "record extent");
        std::memcpy(result.bytes.data() + result.length, payload.bytes.data(), payload.length);
        result.length = total;
        return result;
    }
}

const Dn2CppTypeInfo original_type{};
const Dn2CppTypeInfo substituted_type{};
constexpr char empty[] = "";
constexpr char unicode[] = "共有接尾辞_Ω_𐐀";
const void* const pointers[] = { &original_type, empty, unicode, &substituted_type };
const char* const display_tokens[] = { "共有", "接尾辞", "_Ω_𐐀" };
const Record image_method = record(0, 0, {});
const Record image_ctor = record(0, 0, {});
const Record overrun_ctor = record(1, 0, {});

const Dn2CppBindingSignature field_signature{ 6 };

Dn2CppObject* identity_getter(Dn2CppObject* object) { return object; }

DN2CPP_NATIVE_FIELDS(local_fields,
    { unicode, &original_type, &original_type, DN2CPP_FLDA_PUBLIC, identity_getter,
        nullptr, nullptr, 0, 0x6, INT32_MAX, INT64_MIN, unicode, identity_getter, &field_signature },
    { empty, &original_type, nullptr, 0, nullptr, nullptr, nullptr, 0, 0x6 });
DN2CPP_NATIVE_METADATA_STORAGE(local_fields);
constexpr auto packed_local_fields = Dn2CppMetadataTable<Dn2CppFieldInfo>::from_static(local_fields_storage.records.data());
DN2CPP_NATIVE_TYPE_REFLECTION(local_type, packed_local_fields, 2);
static constexpr Dn2CppMethodInfo event_methods_rows[] = { {} };
alignas(2) constexpr uint8_t event_method_record[] = { 0, 4, 0, 0 };
constexpr auto packed_event_method = Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_static(event_method_record);
static constexpr Dn2CppMetadataHandle<Dn2CppMethodInfo> event_others[] = { packed_event_method, &event_methods_rows[0] };
static constexpr Dn2CppEventInfo event_rows[] = {
    { unicode, &original_type, &substituted_type, packed_event_method, &event_methods_rows[0],
        packed_event_method, event_others, 2, DN2CPP_MTHA_PUBLIC, 0x200, nullptr, 0, 0x14000001, unicode, &field_signature },
};
DN2CPP_NATIVE_METADATA_STORAGE(event);
constexpr auto packed_event = Dn2CppMetadataHandle<Dn2CppEventInfo>::from_static(event_storage.records.data());
const Dn2CppMethodInfo initializer_row{};
const Dn2CppGenericTypeMetadata generic_type_metadata{};
DN2CPP_NATIVE_TYPE_REFLECTION(initializer_type,
    nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0,
    nullptr, 0, nullptr, 0, 0, nullptr, 0, nullptr, nullptr, nullptr, &initializer_row, &generic_type_metadata, Dn2CppMetadataTable<Dn2CppEventInfo>::from_static(event_storage.records.data()), 1);
DN2CPP_NATIVE_METADATA_STORAGE(initializer_type);
constexpr auto packed_initializer_type = Dn2CppMetadataHandle<Dn2CppTypeReflection>::from_static(initializer_type_storage.records.data());
DN2CPP_NATIVE_METADATA_STORAGE(local_type);
constexpr auto packed_local_type = Dn2CppMetadataHandle<Dn2CppTypeReflection>::from_static(local_type_storage.records.data());

const Dn2CppTypeInfo invocation_owner_def{ "InvokeDiagnostics.Owner`1" };
const Dn2CppTypeInfo* const invocation_owner_args[] = { &dn2cpp_int32_type };
const Dn2CppTypeInfo* const invocation_method_args[] = { &dn2cpp_int64_type };
const Dn2CppTypeInfo invocation_owner = [] {
    Dn2CppTypeInfo type{ "Internal_Mangled_Owner_Int32", &dn2cpp_object_type };
    type.genericDef = &invocation_owner_def;
    type.genericArgCount = 1;
    type.genericArgs = invocation_owner_args;
    return type;
}();

Dn2CppObject* invocation_target()
{
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_string_from_utf8("available", 9));
}

Dn2CppObject* invocation_thunk(void* fn, Dn2CppObject*, Dn2CppObject**, const Dn2CppTypeInfo*)
{
    return reinterpret_cast<Dn2CppObject* (*)()>(fn)();
}

constexpr char invocation_display[] = "System.String Diagnostic[Int64]()";
const void* const invocation_pointers[] = { "Diagnostic", &invocation_owner, &dn2cpp_string_type,
    reinterpret_cast<const void*>(&invocation_target), reinterpret_cast<const void*>(&invocation_thunk),
    invocation_method_args, invocation_display };

struct InvocationFault
{
    Dn2CppString* message = nullptr;
    bool described = false;
};

constexpr int32_t invocation_attrs = DN2CPP_MTHA_STATIC | DN2CPP_MTHA_PUBLIC | DN2CPP_MTHA_GENERIC;

Dn2CppMethodInfo invocation_row(int kind)
{
    Dn2CppMethodInfo row{};
    row.name = "Diagnostic";
    row.declaringType = &invocation_owner;
    row.returnType = &dn2cpp_string_type;
    row.attrs = invocation_attrs;
    row.vtableSlot = -1;
    row.fnPtr = kind == 1 ? nullptr : reinterpret_cast<void*>(&invocation_target);
    row.invoker = kind == 0 ? nullptr : reinterpret_cast<void*>(&invocation_thunk);
    row.ilAttrs = 0x16;
    row.genericParamCount = 1;
    row.genericArgs = invocation_method_args;
    row.display = invocation_display;
    return row;
}

template<class Action> InvocationFault invocation_fault(Action action)
{
    try { action(); }
    catch (Dn2CppException& ex)
    {
        Dn2CppString* message = dn2cpp_exception_message(ex.obj);
        auto contains = [message](const char* text) {
            return dn2cpp_str_contains_cmp(message,
                dn2cpp_string_from_utf8(text, static_cast<int32_t>(std::strlen(text))), 4) != 0;
        };
        bool described = ex.obj->type == &dn2cpp_platform_not_supported_exception_type
            && dn2cpp_exception_hresult(ex.obj) == static_cast<int32_t>(0x80131539u)
            && reinterpret_cast<Dn2CppExceptionObject*>(ex.obj)->inner == nullptr
            && dn2cpp_str_contains_cmp(message, dn2cpp_type_tostring(&invocation_owner), 4) != 0
            && contains("Diagnostic") && contains("Int64")
            && contains("the target method's body was not compiled into this image");
        if (!described)
            std::fprintf(stderr, "metadata invocation refusal: %s/%08X, complete diagnosis=%d\n",
                ex.obj->type->name, static_cast<uint32_t>(dn2cpp_exception_hresult(ex.obj)), described);
        dn2cpp_exc_inflight_pop(ex.obj);
        return { message, described };
    }
    return {};
}

void check_invocation_rows()
{
    std::puts("metadata missing invocation diagnostics begin");
    bool all_described = true;
    // Native invocation plans retain row identity; each shape needs an immutable row.
    static const Dn2CppMethodInfo rows[] = { invocation_row(0), invocation_row(1), invocation_row(2) };
    constexpr uint64_t presence = (1ULL << 0) | (1ULL << 1) | (1ULL << 2) | (1ULL << 5)
        | (1ULL << 6) | (1ULL << 12) | (1ULL << 15) | (1ULL << 16) | (1ULL << 22);
    for (bool packed : { false, true })
    {
        // A callable row is the control for both missing-pointer paths.
        for (int kind : { 2, 0, 1 })
        {
            Record bytes = kind == 0
                ? record(2, presence | (1ULL << 7), { 1, 2, 3, invocation_attrs * 2, 1, 4, 0x16 * 2, 2, 6, 7 })
                : kind == 1
                ? record(2, presence | (1ULL << 8), { 1, 2, 3, invocation_attrs * 2, 1, 5, 0x16 * 2, 2, 6, 7 })
                : record(2, presence | (1ULL << 7) | (1ULL << 8),
                    { 1, 2, 3, invocation_attrs * 2, 1, 4, 5, 0x16 * 2, 2, 6, 7 });
            Dn2CppMetadataHandle<Dn2CppMethodInfo> handle = packed
                ? Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_static(bytes.bytes.data()) : &rows[kind];
            Dn2CppMethodRef method{};
            method.type = &dn2cpp_methodinfo_type;
            method.method = handle;
            method.reflectedType = &invocation_owner;
            Dn2CppReflBind bind{};
            bind.type = &dn2cpp_reflbind_type;
            bind.method = handle;
            bind.declaring = &invocation_owner;
            bind.mode = DN2CPP_DGBIND_OPEN_STATIC;
            auto invoke = [&] { return dn2cpp_methodref_invoke(&method, nullptr, nullptr); };
            auto unwrapped = [&] { return dn2cpp_methodref_invoke(&method, nullptr, nullptr, false); };
            auto bound = [&] { return dn2cpp_reflbind_invoke(&bind, nullptr, nullptr); };
            bool count_rejected = false;
            try { (void)dn2cpp_methodref_invoke(&method, nullptr, dn2cpp_newarr_ref(1)); }
            catch (Dn2CppException& ex)
            {
                count_rejected = ex.obj->type == &dn2cpp_target_parameter_count_exception_type;
                dn2cpp_exc_inflight_pop(ex.obj);
            }
            require(count_rejected, "argument count precedes a missing body or invoker");
            if (kind == 2)
            {
                for (Dn2CppObject* value : { invoke(), unwrapped(), bound() })
                    require(dn2cpp_string_equals(reinterpret_cast<Dn2CppString*>(value),
                        dn2cpp_string_from_utf8("available", 9)) != 0,
                        "native and packed callable rows still run through every entry");
                continue;
            }
            std::printf("metadata missing %s %s\n", packed ? "packed" : "native",
                kind == 0 ? "invoker" : "body");
            InvocationFault first = invocation_fault(invoke);
            InvocationFault second = invocation_fault(unwrapped);
            InvocationFault third = invocation_fault(bound);
            all_described &= first.described && second.described && third.described
                && dn2cpp_string_equals(first.message, second.message) != 0
                && dn2cpp_string_equals(first.message, third.message) != 0;
        }
    }
    require(all_described, "every missing-row entry shares a complete platform refusal");
    std::puts("metadata missing invocation diagnostics OK");
}

Dn2CppString* default_kind(Dn2CppObject*) { return dn2cpp_string_from_utf8("default", 7); }
Dn2CppString* derived_kind(Dn2CppObject*) { return dn2cpp_string_from_utf8("derived", 7); }

void check_unrecorded_interface_guard()
{
    std::puts("metadata unrecorded interface guard begin");
    static Dn2CppTypeInfo declaration{ "MetadataGuard.IDefaultKind" };
    static Dn2CppTypeInfo derived{ "MetadataGuard.ISelectedKind" };
    static const Dn2CppMethodInfo declaration_row = [] {
        Dn2CppMethodInfo row{};
        row.name = "Kind";
        row.declaringType = &declaration;
        row.returnType = &dn2cpp_string_type;
        row.attrs = DN2CPP_MTHA_PUBLIC;
        row.ilAttrs = DN2CPP_MA_VIRTUAL;
        row.vtableSlot = 0;
        row.metadataToken = 0x06000001;
        row.fnPtr = reinterpret_cast<void*>(&default_kind);
        return row;
    }();
    static const Dn2CppMethodInfo derived_row = [] {
        Dn2CppMethodInfo row = declaration_row;
        row.name = "MetadataGuard.IDefaultKind.Kind";
        row.declaringType = &derived;
        row.metadataToken = 0x06000002;
        row.fnPtr = reinterpret_cast<void*>(&derived_kind);
        return row;
    }();
    static const Dn2CppTypeReflection declaration_reflection = [] {
        Dn2CppTypeReflection reflection{};
        reflection.methods = &declaration_row;
        reflection.methodCount = 1;
        return reflection;
    }();
    static const Dn2CppTypeReflection derived_reflection = [] {
        Dn2CppTypeReflection reflection{};
        reflection.methods = &derived_row;
        reflection.methodCount = 1;
        return reflection;
    }();
    static const void* default_slots[] = { reinterpret_cast<void*>(&default_kind) };
    static const void* derived_slots[] = { reinterpret_cast<void*>(&derived_kind) };
    static const Dn2CppInterfaceEntry derived_interfaces[] = { { &declaration, nullptr } };
    static const Dn2CppInterfaceEntry default_interfaces[] = { { &declaration, default_slots } };
    static const Dn2CppInterfaceEntry selected_interfaces[] = {
        { &declaration, derived_slots }, { &derived, nullptr }
    };
    declaration.flags = DN2CPP_TF_INTERFACE;
    declaration.reflectionData = &declaration_reflection;
    derived.flags = DN2CPP_TF_INTERFACE;
    derived.interfaces = derived_interfaces;
    derived.interfaceCount = 1;
    derived.reflectionData = &derived_reflection;
    static Dn2CppTypeInfo receiver{ "MetadataGuard.UnrecordedReceiver" };
    receiver.base = &dn2cpp_object_type;
    receiver.interfaces = default_interfaces;
    receiver.interfaceCount = 1;
    static Dn2CppObject object{ &receiver };
    static const Dn2CppDelegateMethodIdentity identity{ &declaration, 0x06000001, 0, nullptr, true };
    Dn2CppDelegate delegate{};
    delegate.type = &dn2cpp_object_type;
    delegate.target = &object;
    delegate.identity = &identity;
    delegate.method = reinterpret_cast<void*>(&default_kind);
    auto* method = reinterpret_cast<Dn2CppMethodRef*>(dn2cpp_delegate_get_method(&delegate));
    require(method != nullptr && method->method == &declaration_row,
        "an unrecorded receiver with the declaration default answers its declaration");
    receiver.interfaces = selected_interfaces;
    receiver.interfaceCount = 2;
    delegate.method = reinterpret_cast<void*>(&derived_kind);
    method = reinterpret_cast<Dn2CppMethodRef*>(dn2cpp_delegate_get_method(&delegate));
    require(method != nullptr && method->method == &derived_row,
        "an unrecorded receiver selects a described derived-interface override");
    derived.flags |= DN2CPP_TF_METADATA_STRIPPED;
    derived.reflectionData = nullptr;
    bool rejected = false;
    try { (void)dn2cpp_delegate_get_method(&delegate); }
    catch (Dn2CppException& exception)
    {
        auto* message = dn2cpp_exception_message(exception.obj);
        rejected = exception.obj->type == &dn2cpp_platform_not_supported_exception_type
            && dn2cpp_str_contains_cmp(message, dn2cpp_string_from_utf8(derived.name,
                static_cast<int32_t>(std::strlen(derived.name))), 4) != 0
            && dn2cpp_str_contains_cmp(message, dn2cpp_string_from_utf8("--reflection-root", 17), 4) != 0
            && dn2cpp_str_contains_cmp(message, dn2cpp_string_from_utf8("--trim-reflection", 17), 4) != 0;
        dn2cpp_exc_inflight_pop(exception.obj);
    }
    require(rejected, "an unknown stripped derived-interface selection refuses with its owner and remedies");
    std::puts("metadata unrecorded interface guard OK");
}

void check_object_virtual_equality_guard()
{
    std::puts("metadata Object virtual equality guard begin");
    auto* root = dn2cpp_type_get_method(dn2cpp_get_type_from_handle(&dn2cpp_object_type),
        dn2cpp_string_from_utf8("ToString", 8), 20);
    require(root != nullptr, "Object virtual equality has its declaration descriptor");
    static Dn2CppTypeInfo described{ "MetadataEquality.Described" };
    static const Dn2CppMethodInfo override_row = [] {
        Dn2CppMethodInfo row{};
        row.name = "ToString";
        row.declaringType = &described;
        row.returnType = &dn2cpp_string_type;
        row.attrs = DN2CPP_MTHA_PUBLIC;
        row.ilAttrs = DN2CPP_MA_VIRTUAL;
        row.metadataToken = 0x06000003;
        return row;
    }();
    static const Dn2CppTypeReflection described_reflection = [] {
        Dn2CppTypeReflection reflection{};
        reflection.methods = &override_row;
        reflection.methodCount = 1;
        return reflection;
    }();
    described.base = &dn2cpp_object_type;
    described.tostring = dn2cpp_object_type.tostring;
    described.flags = DN2CPP_TF_OBJECT_MEMBER_ROWS;
    described.reflectionData = &described_reflection;
    Dn2CppObject receiver{ &described };
    Dn2CppReflBind binding{};
    binding.method = root->method;
    binding.declaring = &dn2cpp_object_type;
    binding.target = &receiver;
    binding.mode = DN2CPP_DGBIND_CLOSED_INSTANCE;
    binding.virtualRow = 1;
    binding.slotKind = DN2CPP_DGBIND_SLOT_OBJECT;
    binding.slot = 0;
    const Dn2CppTypeInfo* owner;
    int32_t token, argc;
    const Dn2CppTypeInfo* const* args;
    require(dn2cpp_reflbind_selected_key(&binding, owner, token, argc, args)
        && owner == &described && token == override_row.metadataToken && argc == 0,
        "Object virtual equality selects an override from rows without a distinct body address");
    static Dn2CppTypeInfo catalog_base{ "MetadataEquality.CatalogBase" };
    static const Dn2CppMethodInfo base_row = [] {
        Dn2CppMethodInfo row = override_row;
        row.declaringType = &catalog_base;
        row.metadataToken = 0x06000004;
        return row;
    }();
    static const Dn2CppDelegateMethodCatalog catalog[] = {
        { &catalog_base, &base_row, 1 }
    };
    catalog_base.base = &dn2cpp_object_type;
    catalog_base.tostring = dn2cpp_object_type.tostring;
    described.base = &catalog_base;
    dn2cpp_set_delegate_method_catalog(catalog, 1);
    require(dn2cpp_reflbind_selected_key(&binding, owner, token, argc, args)
        && owner == &described && token == override_row.metadataToken,
        "a private ancestor row preserves the most derived overriding row");
    dn2cpp_set_delegate_method_catalog(nullptr, 0);
    static Dn2CppTypeInfo opaque{ "MetadataEquality.Opaque" };
    opaque.base = &dn2cpp_object_type;
    opaque.tostring = dn2cpp_object_type.tostring;
    receiver.type = &opaque;
    require(!dn2cpp_reflbind_selected_key(&binding, owner, token, argc, args),
        "Object virtual equality declines an opaque unknown level without throwing");
    described.flags |= DN2CPP_TF_METADATA_STRIPPED;
    receiver.type = &described;
    require(!dn2cpp_reflbind_selected_key(&binding, owner, token, argc, args),
        "Object virtual equality declines a stripped unknown level without throwing");
    std::puts("metadata Object virtual equality guard OK");
}
}

const Dn2CppMetadataBlock dn2cpp_metadata_blocks[129] = {
    { pointers, display_tokens, image_method.bytes.data(), image_method.length,
        image_ctor.bytes.data(), image_ctor.length },
    { pointers, nullptr, nullptr, 0, overrun_ctor.bytes.data(), overrun_ctor.length - 2 },
    { invocation_pointers, nullptr, nullptr, 0, nullptr, 0 }
};
const std::size_t dn2cpp_metadata_block_count = 129;
// This native probe supplies the empty generated-image tables required by the runtime.
const Dn2CppTypeRegEntry dn2cpp_type_registry[] = { {} };
const int32_t dn2cpp_type_registry_count = 0;
const Dn2CppTypeBind dn2cpp_type_binds[] = { {} };
const int32_t dn2cpp_type_bind_count = 0;
const Dn2CppAssemblyRegEntry dn2cpp_assembly_registry[] = { {} };
const int32_t dn2cpp_assembly_registry_count = 0;
const Dn2CppDelegateReflEntry dn2cpp_delegate_refl_registry[] = { {} };
const int32_t dn2cpp_delegate_refl_registry_count = 0;
const Dn2CppGvmRowDispatch dn2cpp_gvm_row_dispatch[] = { {} };
const int32_t dn2cpp_gvm_row_dispatch_count = 0;
const Dn2CppItfImplSlots dn2cpp_itf_impl_slots[] = { {} };
const int32_t dn2cpp_itf_impl_slot_count = 0;
const Dn2CppRenamedSlotBody dn2cpp_renamed_slot_bodies[] = { {} };
const int32_t dn2cpp_renamed_slot_body_count = 0;
const Dn2CppBclMessage dn2cpp_bcl_messages[] = { { nullptr, nullptr } };
const int32_t dn2cpp_bcl_message_count = 0;
const int32_t dn2cpp_exception_get_message_slot = -1;
bool dn2cpp_argument_exception_store(Dn2CppObject*, Dn2CppString*, Dn2CppObject*) { return false; }
bool dn2cpp_object_disposed_exception_store(Dn2CppObject*, Dn2CppString*) { return false; }
const Dn2CppRuntimeTemplate* const dn2cpp_runtime_templates = nullptr;
const int32_t dn2cpp_runtime_template_count = 0;

int main()
{
    static_assert(sizeof(Dn2CppMetadataHandle<Dn2CppFieldInfo>) == sizeof(void*));
    static_assert(offsetof(Dn2CppMethodInfo, definitionSignature) + sizeof(void*) == sizeof(Dn2CppMethodInfo));
    auto event = packed_event.operator->();
    require(event->name == unicode && event->declaringType == &original_type
        && event->handlerType == &substituted_type && event->handlerRecipe == &field_signature
        && event->eventAttributes == 0x200 && event->metadataToken == 0x14000001,
        "event schema preserves handler identity, symbolic recipes and event attributes");
    require(event->adder.identity() == packed_event_method.identity()
        && event->remover.identity() == &event_methods_rows[0]
        && event->raiser.identity() == packed_event_method.identity()
        && event->otherCount == 2 && event->others[0].identity() == packed_event_method.identity()
        && event->others[1].identity() == &event_methods_rows[0],
        "event accessors preserve original packed and native method handles");
    require(event_rows[0].adder.identity() == packed_event_method.identity()
        && packed_initializer_type->eventCount == 1
        && packed_initializer_type->events[0].identity() == packed_event.identity(),
        "native event owners retain packed accessor identity and cold type rows retain events");
    require(packed_initializer_type->initializer.identity() == &initializer_row,
        "packed cold rows retain a binding-only initializer handle");
    require(packed_initializer_type->genericMetadata == &generic_type_metadata,
        "native packing and cold decoding preserve the generic descriptor pointer");
    static const Dn2CppTypeInfo constraint_interface = [] {
        Dn2CppTypeInfo ti{};
        ti.name = "Codec.IConstraint";
        ti.flags = DN2CPP_TF_INTERFACE;
        return ti;
    }();
    static const Dn2CppBindingSignature interface_constraint{8, &constraint_interface};
    static const Dn2CppMethodGenericParameter interface_parameter{"W", 0, &interface_constraint, 1};
    static Dn2CppMethodInfo interface_method{};
    interface_method.name = "InterfaceConstraint";
    interface_method.declaringType = &original_type;
    interface_method.attrs = DN2CPP_MTHA_GENERIC;
    interface_method.genericParamCount = 1;
    interface_method.genericParameters = &interface_parameter;
    Dn2CppMethodRef interface_reference{};
    interface_reference.type = &dn2cpp_methodinfo_type;
    interface_reference.method = &interface_method;
    interface_reference.isGenericDefView = 1;
    auto* interface_formal = reinterpret_cast<Dn2CppType*>(dn2cpp_methodref_get_generic_arguments(&interface_reference)->data[0]);
    require(dn2cpp_type_implement_interface(interface_formal, dn2cpp_get_type_from_handle(&constraint_interface)) != 0,
        "internal interface queries complete cold formal relations before their first read");
    static const Dn2CppBindingSignature missing_constraint{8, nullptr, 0, nullptr, 0, "Missing.Constraint.Type"};
    static const Dn2CppMethodGenericParameter failed_parameter{"U", 0, &missing_constraint, 1};
    static Dn2CppMethodInfo failed_method{};
    failed_method.name = "FailedConstraint";
    failed_method.declaringType = &original_type;
    failed_method.attrs = DN2CPP_MTHA_GENERIC;
    failed_method.genericParamCount = 1;
    failed_method.genericParameters = &failed_parameter;
    Dn2CppMethodRef failed_reference{};
    failed_reference.type = &dn2cpp_methodinfo_type;
    failed_reference.method = &failed_method;
    failed_reference.reflectedType = &original_type;
    failed_reference.isGenericDefView = 1;
    auto* failed_formal = reinterpret_cast<Dn2CppType*>(dn2cpp_methodref_get_generic_arguments(&failed_reference)->data[0]);
    require(std::strcmp(failed_formal->typeInfo->name, "U") == 0
        && dn2cpp_type_generic_parameter_attributes(failed_formal->typeInfo) == 0,
        "unresolved constraints cannot prevent formal identity queries");
    auto* failed_declaring = dn2cpp_type_generic_parameter_declaring_method(failed_formal->typeInfo);
    require(failed_declaring->method.identity() == &failed_method && failed_declaring->isGenericDefView != 0,
        "formal declaring methods preserve token-zero row identity without resolving constraints");
    for (int attempt = 0; attempt < 2; attempt++)
    {
        bool refused = false;
        try { (void)dn2cpp_type_get_generic_parameter_constraints(failed_formal); }
        catch (Dn2CppException& ex)
        {
            refused = ex.obj != nullptr && ex.obj->type == &dn2cpp_platform_not_supported_exception_type;
            dn2cpp_exc_inflight_pop(ex.obj);
        }
        require(refused, "failed constraint initialization must not publish partial answers on retry");
        require(dn2cpp_methodref_get_generic_arguments(&failed_reference)->data[0] == reinterpret_cast<Dn2CppObject*>(failed_formal),
            "a failed relation query preserves the formal identity");
        require(dn2cpp_type_generic_parameter_declaring_method(failed_formal->typeInfo) == failed_declaring,
            "failed constraint completion cannot change a formal declaring method");
    }
    static const Dn2CppGenericTypeMetadata failed_owner_metadata{&failed_parameter, 1, nullptr, nullptr, 0, true};
    static const Dn2CppTypeReflection failed_owner_reflection = [] {
        Dn2CppTypeReflection row{};
        row.genericMetadata = &failed_owner_metadata;
        return row;
    }();
    static const Dn2CppTypeInfo failed_owner = [] {
        Dn2CppTypeInfo ti{};
        ti.name = "FailedOwner`1";
        ti.flags = DN2CPP_TF_GENERICDEF;
        ti.reflectionData = &failed_owner_reflection;
        return ti;
    }();
    static const Dn2CppMethodGenericParameter unconstrained_parameter{"V", 0};
    static Dn2CppMethodInfo owner_method = failed_method;
    owner_method.declaringType = &failed_owner;
    owner_method.genericParameters = &unconstrained_parameter;
    Dn2CppMethodRef owner_reference = failed_reference;
    owner_reference.method = &owner_method;
    auto* owner_formal = reinterpret_cast<Dn2CppType*>(dn2cpp_type_get_generic_arguments(dn2cpp_get_type_from_handle(&failed_owner))->data[0]);
    Dn2CppObject* method_formal = dn2cpp_methodref_get_generic_arguments(&owner_reference)->data[0];
    auto* owner_declaring = dn2cpp_type_generic_parameter_declaring_method(reinterpret_cast<Dn2CppType*>(method_formal)->typeInfo);
    require(owner_declaring->method.identity() == &owner_method && owner_declaring->reflectedType == &failed_owner,
        "a formal declaring method retains its typical owner without resolving owner constraints");
    for (int attempt = 0; attempt < 2; attempt++)
    {
        bool refused = false;
        try { (void)dn2cpp_type_base_type(owner_formal); }
        catch (Dn2CppException& ex)
        {
            refused = ex.obj != nullptr && ex.obj->type == &dn2cpp_platform_not_supported_exception_type;
            dn2cpp_exc_inflight_pop(ex.obj);
        }
        require(refused && dn2cpp_methodref_get_generic_arguments(&owner_reference)->data[0] == method_formal,
            "unresolved owner relations cannot leave a method parameter context partially initialized");
        require(dn2cpp_type_get_generic_parameter_constraints(reinterpret_cast<Dn2CppType*>(method_formal))->length == 0,
            "an unconstrained method formal does not require its owner's unresolved constraints");
        require(dn2cpp_type_generic_parameter_declaring_method(reinterpret_cast<Dn2CppType*>(method_formal)->typeInfo) == owner_declaring,
            "failed owner completion cannot change a formal declaring method");
    }
    static const Dn2CppTypeInfo layoutless_definition = [] {
        Dn2CppTypeInfo ti{};
        ti.name = "MetadataOnly`1";
        ti.flags = DN2CPP_TF_GENERICDEF;
        return ti;
    }();
    static const Dn2CppBindingSignature closed_child{0, &dn2cpp_int32_type};
    static const Dn2CppBindingSignature closed_constraint{4, &layoutless_definition, 0, &closed_child, 1};
    static const Dn2CppMethodGenericParameter closed_parameter{"U", 0, &closed_constraint, 1};
    static Dn2CppMethodInfo closed_method = failed_method;
    closed_method.genericParameters = &closed_parameter;
    Dn2CppMethodRef closed_reference = failed_reference;
    closed_reference.method = &closed_method;
    auto* closed_formal = reinterpret_cast<Dn2CppType*>(dn2cpp_methodref_get_generic_arguments(&closed_reference)->data[0]);
    auto* closed = reinterpret_cast<Dn2CppType*>(dn2cpp_type_get_generic_parameter_constraints(closed_formal)->data[0]);
    require(!dn2cpp_type_contains_generic_parameters(closed->typeInfo) && dn2cpp_type_metadata_only(closed->typeInfo),
        "a closed constraint recipe without an AOT layout remains metadata-only");
    for (int action = 0; action < 5; action++)
    {
        bool refused = false;
        try
        {
            int32_t length = 1;
            if (action == 0) (void)dn2cpp_box_by_handle(closed->typeInfo, nullptr);
            if (action == 1) (void)dn2cpp_get_uninitialized_object(closed);
            if (action == 2) (void)dn2cpp_activator_create_instance(closed);
            if (action == 3) (void)dn2cpp_array_create_instance(closed, &length, 1);
            if (action == 4)
            {
                auto* arguments = dn2cpp_newarr_ref(1);
                arguments->data[0] = reinterpret_cast<Dn2CppObject*>(closed);
                (void)dn2cpp_type_make_generic(dn2cpp_get_type_from_handle(&layoutless_definition), arguments);
            }
        }
        catch (Dn2CppException& ex)
        {
            refused = ex.obj != nullptr && ex.obj->type == (action == 3
                ? &dn2cpp_not_supported_exception_type : &dn2cpp_platform_not_supported_exception_type);
            dn2cpp_exc_inflight_pop(ex.obj);
        }
        require(refused, "closed metadata-only constraints cannot enter allocation, boxing or executable generic composition");
    }
    auto minimum = record(0, 2, { UINT64_MAX });
    auto minimum_handle = Dn2CppMetadataHandle<Dn2CppEnumMember>::from_static(minimum.bytes.data());
    require(minimum_handle->value == INT64_MIN, "ten-byte unsigned integer preserves signed minimum");
    require(minimum_handle.identity() == minimum.bytes.data() + 1, "static identity is tagged record address");
    require(minimum_handle->name == nullptr, "absent pointer remains null");
    auto maximum = record(0, 2, { UINT64_MAX - 1 });
    require(Dn2CppMetadataHandle<Dn2CppEnumMember>::from_static(maximum.bytes.data())->value == INT64_MAX,
        "signed maximum");
    auto negative = record(0, 2, { 1 });
    require(Dn2CppMetadataHandle<Dn2CppEnumMember>::from_static(negative.bytes.data())->value == -1,
        "zigzag negative one");

    auto field = record(0, (1ULL << 3) | (1ULL << 7) | (1ULL << 9),
        { UINT32_MAX, UINT32_MAX - 1, 1 });
    auto decoded_field = *Dn2CppMetadataHandle<Dn2CppFieldInfo>::from_static(field.bytes.data());
    require(decoded_field.attrs == INT32_MIN && decoded_field.customAttrCount == INT32_MAX
        && decoded_field.metadataToken == -1, "signed 32-bit boundaries remain full width");
    auto type = record(0, 1ULL << 15, { UINT32_MAX });
    require(Dn2CppMetadataHandle<Dn2CppTypeReflection>::from_static(type.bytes.data())->ilAttrs == UINT32_MAX,
        "unsigned 32-bit attributes remain full width");
    auto derived_flags = record(0, 1ULL << 8, { 0x76 * 2 });
    require(Dn2CppMetadataHandle<Dn2CppFieldInfo>::from_static(derived_flags.bytes.data())->attrs
        == (DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_INITONLY | DN2CPP_FLDA_LITERAL),
        "omitted flags derive from recorded ECMA attributes");
    auto explicit_flags = record(0, (1ULL << 3) | (1ULL << 8), { 0, 0x76 * 2 });
    require(Dn2CppMetadataHandle<Dn2CppFieldInfo>::from_static(explicit_flags.bytes.data())->attrs == 0,
        "explicit zero flags remain distinct from omitted flags");

    auto unknown = record(0, 0, {});
    auto known = record(0, (1ULL << 0) | (1ULL << 1) | (1ULL << 9), { 1, 2, 2 });
    auto parameter = *Dn2CppMetadataHandle<Dn2CppParamInfo>::from_static(known.bytes.data());
    require(parameter.paramType == &original_type && parameter.name == empty && parameter.name[0] == 0,
        "pooled pointers and empty names preserve identity");
    require(parameter.requiredCustomModifiers == nullptr && parameter.requiredCustomModifierCount == 0
        && parameter.customModifiersKnown == 1, "known empty modifiers");
    require(Dn2CppMetadataHandle<Dn2CppParamInfo>::from_static(unknown.bytes.data())->customModifiersKnown == 0,
        "unknown modifiers remain distinct from empty");
    auto pass_record = record(0, (1ULL << 12) | (1ULL << 13) | (1ULL << 14),
        { 3, DN2CPP_PASS_BYREF * 2, 1 });
    auto pass = *Dn2CppMetadataHandle<Dn2CppParamInfo>::from_static(pass_record.bytes.data());
    require(pass.genericDefinitionKey == unicode && pass.passKind == DN2CPP_PASS_BYREF
        && pass.passType == &original_type, "parameter invocation descriptors preserve their identity");
    auto dispatch_record = record(0, (1ULL << 26) | (1ULL << 27) | (1ULL << 28) | (1ULL << 29),
        { 3, 4, 0x06000001 * 2, 1 });
    auto dispatch = *Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_static(dispatch_record.bytes.data());
    require(dispatch.genericDefinitionReturnKey == unicode && dispatch.gvmRootDepth == 2
        && dispatch.gvmRootToken == 0x06000001 && dispatch.returnPassType == &original_type,
        "method invocation descriptors preserve generic roots and return types");
    auto function_record = record(0, (1ULL << 5) | (1ULL << 31),
        { DN2CPP_MTHA_RETURN_POINTER * 2, 1 });
    auto function = *Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_static(function_record.bytes.data());
    require(function.returnSignatureType == &original_type && function.returnPassType == nullptr,
        "function-pointer identity remains separate from Invoke's IntPtr boxing marker");
    auto pointer_parameter_record = record(0, (1ULL << 14) | (1ULL << 16), { 1, 4 });
    auto pointer_parameter = *Dn2CppMetadataHandle<Dn2CppParamInfo>::from_static(pointer_parameter_record.bytes.data());
    require(pointer_parameter.passType == &original_type && pointer_parameter.bindingPointeeType == pointers[3],
        "pointer binding identity remains separate from parameter marshalling");
    auto pointer_return_record = record(0, (1ULL << 29) | (1ULL << 32), { 1, 4 });
    auto pointer_return = *Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_static(pointer_return_record.bytes.data());
    require(pointer_return.returnPassType == &original_type && pointer_return.returnBindingPointeeType == pointers[3]
        && pointer_return.returnSignatureType == nullptr,
        "pointer return binding identity crosses the high presence-mask bit without changing boxing");
    auto query_parameter_record = record(0, (1ULL << 14) | (1ULL << 17), { 1, 4 });
    auto query_parameter = *Dn2CppMetadataHandle<Dn2CppParamInfo>::from_static(query_parameter_record.bytes.data());
    require(query_parameter.passType == &original_type && query_parameter.reflectionSignature == pointers[3],
        "query parameter AST relocates independently of Invoke pass type");
    auto query_return_record = record(0, (1ULL << 29) | (1ULL << 33), { 1, 4 });
    auto query_return = *Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_static(query_return_record.bytes.data());
    require(query_return.returnPassType == &original_type && query_return.returnReflectionSignature == pointers[3],
        "query return AST relocates above the high presence-mask bit");
    auto definition_signature_record = record(0, (1ULL << 29) | (1ULL << 34), { 1, 4 });
    auto definition_signature = *Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_static(definition_signature_record.bytes.data());
    require(definition_signature.returnPassType == &original_type && definition_signature.definitionSignature == pointers[3],
        "symbolic MethodDef query recipes relocate independently of Invoke pass types");
    auto named = record(0, 1, { 3 });
    require(Dn2CppMetadataHandle<Dn2CppEnumMember>::from_static(named.bytes.data())->name == unicode,
        "Unicode names remain exact pooled bytes");

    for (std::size_t padded : { std::size_t(126), std::size_t(128), std::size_t(130) })
    {
        auto first = record(127, 2, { 2 }, padded);
        auto second = record(128, 2, { 4 });
        std::memcpy(first.bytes.data() + first.length, second.bytes.data(), second.length);
        auto table = Dn2CppMetadataTable<Dn2CppEnumMember>::from_static(first.bytes.data());
        require(table[0]->value == 1 && table[1]->value == 2, "block and record length varint transitions");
        require(table[1].identity() == first.bytes.data() + padded + 1, "record iteration uses full extent");
    }

    Dn2CppEnumMember native_rows[] = { { empty, INT64_MIN }, { unicode, INT64_MAX } };
    Dn2CppMetadataTable<Dn2CppEnumMember> native_table = native_rows;
    require(native_table[1].identity() == &native_rows[1] && native_table[1]->value == INT64_MAX,
        "native table identity and stride");
    Dn2CppMetadataHandle<Dn2CppEnumMember> native_handle = &native_rows[0];
    require(native_handle.identity() == &native_rows[0] && native_handle->value == INT64_MIN,
        "native handle remains untagged");
    auto native_view = native_handle.operator->();
    native_rows[0].value = 31;
    require(native_handle.native() == &native_rows[0] && native_view->value == 31,
        "native views read the original row without a decoded snapshot");
    auto decoded_view = minimum_handle.operator->();
    auto copied_view = decoded_view;
    auto moved_view = std::move(copied_view);
    require(minimum_handle.native() == nullptr && moved_view->value == INT64_MIN
        && moved_view.operator->() != decoded_view.operator->(),
        "copied and moved packed views own independent decoded storage");
    require(Dn2CppMetadataHandle<Dn2CppMethodInfo>{}.native() == nullptr,
        "null method handle has no native row");
    require(dn2cpp_metadata_at(nullptr, Dn2CppMetadataKind::EnumMember,
        sizeof(Dn2CppEnumMember), 0) == nullptr, "empty native table needs no pointer arithmetic");
    require_rejected([&] {
        dn2cpp_metadata_at(native_rows, Dn2CppMetadataKind::EnumMember,
            sizeof(Dn2CppEnumMember), SIZE_MAX);
    }, "native table byte extent cannot overflow the address width");
    require_rejected([] {
        metadata_add(reinterpret_cast<const void*>(UINTPTR_MAX - 1), 2);
    }, "relative addition rejects address overflow before pointer reconstruction");
    require_rejected([] {
        metadata_subtract(reinterpret_cast<const void*>(uintptr_t(1)), 2);
    }, "local block subtraction rejects address underflow");

    Record oversized;
    append_unsigned(oversized, 0);
    append_unsigned(oversized, UINT64_MAX - 1);
    require_rejected([&] {
        (void)*Dn2CppMetadataHandle<Dn2CppEnumMember>::from_static(oversized.bytes.data());
    }, "record extent cannot overflow a native address on either pointer width");
    auto invalid_block = record(dn2cpp_metadata_block_count, 1, { 1 });
    require_rejected([&] {
        (void)*Dn2CppMetadataHandle<Dn2CppEnumMember>::from_static(invalid_block.bytes.data());
    }, "record block index must name a registered block before reading its pointer pool");
    auto invalid_pool_index = record(0, 1, { UINT64_MAX });
    require_rejected([&] {
        (void)*Dn2CppMetadataHandle<Dn2CppEnumMember>::from_static(invalid_pool_index.bytes.data());
    }, "pointer pool indices cannot wrap a byte offset");
    auto truncated = record(0, 2, { 1 });
    truncated.bytes[truncated.length - 1] = 0x80;
    require_rejected([&] {
        (void)*Dn2CppMetadataHandle<Dn2CppEnumMember>::from_static(truncated.bytes.data());
    }, "a continued payload integer cannot read beyond its record");

    Dn2CppMethodInfo original{};
    original.declaringType = &original_type;
    original.name = unicode;
    original.metadataToken = INT32_MAX;
    original.returnPassType = &original_type;
    original.returnBindingPointeeType = &substituted_type;
    original.returnReflectionSignature = reinterpret_cast<const Dn2CppBindingSignature*>(&substituted_type);
    original.definitionSignature = reinterpret_cast<const Dn2CppMethodDefinitionSignature*>(&substituted_type);
    Dn2CppMethodDelta deltas[] = { { 1, &original, &substituted_type }, { 1, &original, &original_type } };
    auto delta = Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_raw(&deltas[0]);
    require(delta.native() == nullptr, "a method delta is decoded instead of treated as a native row");
    require(delta->declaringType == &substituted_type && delta->name == unicode
        && delta->metadataToken == INT32_MAX && original.declaringType == &original_type
        && delta->returnPassType == &original_type && delta->returnBindingPointeeType == &substituted_type
        && delta->returnReflectionSignature == original.returnReflectionSignature
        && delta->definitionSignature == original.definitionSignature,
        "constructor delta preserves original metadata and changes only declaring type");
    require(dn2cpp_metadata_at(deltas, Dn2CppMetadataKind::Method, sizeof(Dn2CppMethodInfo), 1) == &deltas[1],
        "constructor delta table uses descriptor stride");
    auto image_method_handle = Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_static(image_method.bytes.data());
    auto transient_method = record(0, 0, {});
    auto transient_method_handle = Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_static(transient_method.bytes.data());
    require(dn2cpp_metadata_is_image_method(image_method_handle.identity()),
        "registered image method records may enter the invocation cache");
    require(!dn2cpp_metadata_is_image_method(transient_method_handle.identity())
        && !dn2cpp_metadata_is_image_method(delta.identity())
        && !dn2cpp_metadata_is_image_method(&original)
        && !dn2cpp_metadata_is_image_method(packed_local_fields[0].identity())
        && !dn2cpp_metadata_is_image_method(nullptr),
        "stack records, deltas, local blocks, native rows and null cannot enter the invocation cache");
    require(dn2cpp_metadata_is_image_method(
            Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_static(image_ctor.bytes.data()).identity()),
        "registered image constructor records may enter the invocation cache");
    require(!dn2cpp_metadata_is_image_method(
            Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_static(overrun_ctor.bytes.data()).identity()),
        "a record overrunning its registered extent cannot enter the invocation cache");
    require(local_type.native() == local_type_rows && local_fields[0].native() == local_fields_rows,
        "runtime-owned metadata macros retain native rows");
    auto builtin = *packed_local_type;
    require(builtin.fieldCount == 2 && builtin.fields == packed_local_fields,
        "local block retains its tagged field-table identity");
    require(builtin.fields[0]->name == unicode && builtin.fields[0]->declaringType == &original_type
        && builtin.fields[0]->literalValue == INT64_MIN && builtin.fields[0]->metadataToken == INT32_MAX,
        "local block preserves pooled pointers and integer boundaries");
    Dn2CppObject object{ &original_type };
    require(builtin.fields[0]->getter(&object) == &object, "local function pointers remain callable");
    require(builtin.fields[0]->valueCheck(&object) == &object
        && builtin.fields[1]->valueCheck == nullptr,
        "local value checks remain callable and omitted checks remain null");
    require(builtin.fields[0]->reflectionSignature == &field_signature
        && builtin.fields[1]->reflectionSignature == nullptr,
        "local field signature descriptors preserve identity and omitted columns");
    bool pointer_refused = false;
    try { (void)dn2cpp_invoke_box_pointer(nullptr, &dn2cpp_void_type, 1); }
    catch (Dn2CppException& ex)
    {
        pointer_refused = ex.obj != nullptr && ex.obj->type == &dn2cpp_not_supported_exception_type;
        dn2cpp_exc_inflight_pop(ex.obj);
    }
    require(pointer_refused, "an image without Pointer refuses boxing with a managed exception");
    require(builtin.fields[1]->name == empty && builtin.fields[1]->attrs == 0
        && builtin.fields[1]->ilAttrs == 0x6, "local block preserves empty names and explicit zero flags");

    auto malformed = record(0, 1ULL << 3, { UINT64_MAX });
    require_rejected([&] {
        (void)*Dn2CppMetadataHandle<Dn2CppFieldInfo>::from_static(malformed.bytes.data());
    }, "out-of-range 32-bit values are rejected rather than truncated");
    Record invalid_display_block;
    invalid_display_block.bytes[invalid_display_block.length++] = 0xff;
    append_unsigned(invalid_display_block, dn2cpp_metadata_block_count);
    append_unsigned(invalid_display_block, 1);
    append_unsigned(invalid_display_block, 0);
    require_rejected([&] {
        dn2cpp_metadata_string(reinterpret_cast<const char*>(invalid_display_block.bytes.data()));
    }, "display block index must name a registered block before reading its token pool");
    const char tokenized[] = { char(0xff), 0, 3, 0, 1, 2 };
    int64_t before = dn2cpp_gc_allocated_bytes_current_thread();
    Dn2CppString* plain_display = dn2cpp_metadata_string(unicode);
    int64_t plain_bytes = dn2cpp_gc_allocated_bytes_current_thread() - before;
    before = dn2cpp_gc_allocated_bytes_current_thread();
    Dn2CppString* packed_display = dn2cpp_metadata_string(tokenized);
    int64_t packed_bytes = dn2cpp_gc_allocated_bytes_current_thread() - before;
    require(plain_display->length == packed_display->length
        && std::memcmp(plain_display->chars, packed_display->chars,
            plain_display->length * sizeof(char16_t)) == 0,
        "token display reconstructs exact UTF-16 including surrogate pairs");
    require(plain_bytes == packed_bytes, "token display only allocates the final managed string");
    std::puts("metadata codec boundaries OK");
    bool invoker_rejected = false;
    try { dn2cpp_throw_invoker_missing("missing metadata invoker", &invoker_rejected); }
    catch (Dn2CppInvokerMissing& ex)
    {
        invoker_rejected = ex.obj != nullptr && ex.call == &invoker_rejected
            && ex.obj->type == &dn2cpp_not_supported_exception_type
            && dn2cpp_exception_hresult(ex.obj) == static_cast<int32_t>(0x80131515u);
        dn2cpp_exc_inflight_pop(ex.obj);
    }
    require(invoker_rejected, "missing invoker carries the NotSupportedException HResult");
    std::puts("metadata invoker refusal fields OK");
    check_invocation_rows();
    check_unrecorded_interface_guard();
    check_object_virtual_equality_guard();
    std::fflush(stdout);
    dn2cpp_main_exit(0);
    return 0;
}
