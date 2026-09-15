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
const void* const pointers[] = { &original_type, empty, unicode };
const char* const display_tokens[] = { "共有", "接尾辞", "_Ω_𐐀" };
const Record image_method = record(0, 0, {});

Dn2CppObject* identity_getter(Dn2CppObject* object) { return object; }

DN2CPP_NATIVE_FIELDS(local_fields,
    { unicode, &original_type, &original_type, DN2CPP_FLDA_PUBLIC, identity_getter,
        nullptr, nullptr, 0, 0x6, INT32_MAX, INT64_MIN, unicode },
    { empty, &original_type, nullptr, 0, nullptr, nullptr, nullptr, 0, 0x6 });
DN2CPP_NATIVE_METADATA_STORAGE(local_fields);
constexpr auto packed_local_fields = Dn2CppMetadataTable<Dn2CppFieldInfo>::from_static(local_fields_storage.records.data());
DN2CPP_NATIVE_TYPE_REFLECTION(local_type, packed_local_fields, 2);
DN2CPP_NATIVE_METADATA_STORAGE(local_type);
constexpr auto packed_local_type = Dn2CppMetadataHandle<Dn2CppTypeReflection>::from_static(local_type_storage.records.data());
}

const Dn2CppMetadataBlock dn2cpp_metadata_blocks[129] = {
    { pointers, display_tokens, image_method.bytes.data(), image_method.length }
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
const Dn2CppBclMessage dn2cpp_bcl_messages[] = { { nullptr, nullptr } };
const int32_t dn2cpp_bcl_message_count = 0;
const int32_t dn2cpp_exception_get_message_slot = -1;
const Dn2CppRuntimeTemplate* const dn2cpp_runtime_templates = nullptr;
const int32_t dn2cpp_runtime_template_count = 0;

int main()
{
    static_assert(sizeof(Dn2CppMetadataHandle<Dn2CppFieldInfo>) == sizeof(void*));
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
    Dn2CppMethodDelta deltas[] = { { 1, &original, &substituted_type }, { 1, &original, &original_type } };
    auto delta = Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_raw(&deltas[0]);
    require(delta.native() == nullptr, "a method delta is decoded instead of treated as a native row");
    require(delta->declaringType == &substituted_type && delta->name == unicode
        && delta->metadataToken == INT32_MAX && original.declaringType == &original_type,
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
    std::fflush(stdout);
    dn2cpp_main_exit(0);
    return 0;
}
