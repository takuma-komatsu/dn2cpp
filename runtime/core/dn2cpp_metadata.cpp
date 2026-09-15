#include "dn2cpp_core.h"

namespace {
enum class Encoding : unsigned char { Pointer, Signed32, Unsigned32, Signed64 };
struct Field { std::size_t offset; Encoding encoding; };
struct Schema { const Field* fields; std::size_t count; std::size_t size; };
constexpr Field Dn2CppTypeReflection_fields[] = {
    { offsetof(Dn2CppTypeReflection, fields), Encoding::Pointer },
    { offsetof(Dn2CppTypeReflection, fieldCount), Encoding::Signed32 },
    { offsetof(Dn2CppTypeReflection, methods), Encoding::Pointer },
    { offsetof(Dn2CppTypeReflection, methodCount), Encoding::Signed32 },
    { offsetof(Dn2CppTypeReflection, ctors), Encoding::Pointer },
    { offsetof(Dn2CppTypeReflection, ctorCount), Encoding::Signed32 },
    { offsetof(Dn2CppTypeReflection, props), Encoding::Pointer },
    { offsetof(Dn2CppTypeReflection, propCount), Encoding::Signed32 },
    { offsetof(Dn2CppTypeReflection, customAttrs), Encoding::Pointer },
    { offsetof(Dn2CppTypeReflection, customAttrCount), Encoding::Signed32 },
    { offsetof(Dn2CppTypeReflection, enumMembers), Encoding::Pointer },
    { offsetof(Dn2CppTypeReflection, enumMemberCount), Encoding::Signed32 },
    { offsetof(Dn2CppTypeReflection, nestedTypes), Encoding::Pointer },
    { offsetof(Dn2CppTypeReflection, nestedCount), Encoding::Signed32 },
    { offsetof(Dn2CppTypeReflection, assemblyName), Encoding::Pointer },
    { offsetof(Dn2CppTypeReflection, ilAttrs), Encoding::Unsigned32 },
    { offsetof(Dn2CppTypeReflection, metadataToken), Encoding::Signed32 },
    { offsetof(Dn2CppTypeReflection, defaultMemberName), Encoding::Pointer },
    { offsetof(Dn2CppTypeReflection, marshalSize), Encoding::Signed32 },
    { offsetof(Dn2CppTypeReflection, eventSourceName), Encoding::Pointer },
    { offsetof(Dn2CppTypeReflection, eventSourceGuid), Encoding::Pointer },
    { offsetof(Dn2CppTypeReflection, genericParamNames), Encoding::Pointer },
};
constexpr Field Dn2CppFieldInfo_fields[] = {
    { offsetof(Dn2CppFieldInfo, name), Encoding::Pointer },
    { offsetof(Dn2CppFieldInfo, declaringType), Encoding::Pointer },
    { offsetof(Dn2CppFieldInfo, fieldType), Encoding::Pointer },
    { offsetof(Dn2CppFieldInfo, attrs), Encoding::Signed32 },
    { offsetof(Dn2CppFieldInfo, getter), Encoding::Pointer },
    { offsetof(Dn2CppFieldInfo, setter), Encoding::Pointer },
    { offsetof(Dn2CppFieldInfo, customAttrs), Encoding::Pointer },
    { offsetof(Dn2CppFieldInfo, customAttrCount), Encoding::Signed32 },
    { offsetof(Dn2CppFieldInfo, ilAttrs), Encoding::Signed32 },
    { offsetof(Dn2CppFieldInfo, metadataToken), Encoding::Signed32 },
    { offsetof(Dn2CppFieldInfo, literalValue), Encoding::Signed64 },
    { offsetof(Dn2CppFieldInfo, display), Encoding::Pointer },
};
constexpr Field Dn2CppMethodInfo_fields[] = {
    { offsetof(Dn2CppMethodInfo, name), Encoding::Pointer },
    { offsetof(Dn2CppMethodInfo, declaringType), Encoding::Pointer },
    { offsetof(Dn2CppMethodInfo, returnType), Encoding::Pointer },
    { offsetof(Dn2CppMethodInfo, parameters), Encoding::Pointer },
    { offsetof(Dn2CppMethodInfo, paramCount), Encoding::Signed32 },
    { offsetof(Dn2CppMethodInfo, attrs), Encoding::Signed32 },
    { offsetof(Dn2CppMethodInfo, vtableSlot), Encoding::Signed32 },
    { offsetof(Dn2CppMethodInfo, fnPtr), Encoding::Pointer },
    { offsetof(Dn2CppMethodInfo, invoker), Encoding::Pointer },
    { offsetof(Dn2CppMethodInfo, customAttrs), Encoding::Pointer },
    { offsetof(Dn2CppMethodInfo, customAttrCount), Encoding::Signed32 },
    { offsetof(Dn2CppMethodInfo, sigShape), Encoding::Pointer },
    { offsetof(Dn2CppMethodInfo, ilAttrs), Encoding::Signed32 },
    { offsetof(Dn2CppMethodInfo, ilImplAttrs), Encoding::Signed32 },
    { offsetof(Dn2CppMethodInfo, metadataToken), Encoding::Signed32 },
    { offsetof(Dn2CppMethodInfo, genericParamCount), Encoding::Signed32 },
    { offsetof(Dn2CppMethodInfo, genericArgs), Encoding::Pointer },
    { offsetof(Dn2CppMethodInfo, returnRequiredCustomModifiers), Encoding::Pointer },
    { offsetof(Dn2CppMethodInfo, returnRequiredCustomModifierCount), Encoding::Signed32 },
    { offsetof(Dn2CppMethodInfo, returnOptionalCustomModifiers), Encoding::Pointer },
    { offsetof(Dn2CppMethodInfo, returnOptionalCustomModifierCount), Encoding::Signed32 },
    { offsetof(Dn2CppMethodInfo, returnCustomModifiersKnown), Encoding::Signed32 },
    { offsetof(Dn2CppMethodInfo, display), Encoding::Pointer },
    { offsetof(Dn2CppMethodInfo, genericDefinitionDisplay), Encoding::Pointer },
    { offsetof(Dn2CppMethodInfo, returnDisplay), Encoding::Pointer },
    { offsetof(Dn2CppMethodInfo, genericDefinitionReturnDisplay), Encoding::Pointer },
};
constexpr Field Dn2CppParamInfo_fields[] = {
    { offsetof(Dn2CppParamInfo, paramType), Encoding::Pointer },
    { offsetof(Dn2CppParamInfo, name), Encoding::Pointer },
    { offsetof(Dn2CppParamInfo, customAttrs), Encoding::Pointer },
    { offsetof(Dn2CppParamInfo, customAttrCount), Encoding::Signed32 },
    { offsetof(Dn2CppParamInfo, ilAttrs), Encoding::Signed32 },
    { offsetof(Dn2CppParamInfo, requiredCustomModifiers), Encoding::Pointer },
    { offsetof(Dn2CppParamInfo, requiredCustomModifierCount), Encoding::Signed32 },
    { offsetof(Dn2CppParamInfo, optionalCustomModifiers), Encoding::Pointer },
    { offsetof(Dn2CppParamInfo, optionalCustomModifierCount), Encoding::Signed32 },
    { offsetof(Dn2CppParamInfo, customModifiersKnown), Encoding::Signed32 },
    { offsetof(Dn2CppParamInfo, display), Encoding::Pointer },
    { offsetof(Dn2CppParamInfo, genericDefinitionDisplay), Encoding::Pointer },
};
constexpr Field Dn2CppPropInfo_fields[] = {
    { offsetof(Dn2CppPropInfo, name), Encoding::Pointer },
    { offsetof(Dn2CppPropInfo, declaringType), Encoding::Pointer },
    { offsetof(Dn2CppPropInfo, propType), Encoding::Pointer },
    { offsetof(Dn2CppPropInfo, getter), Encoding::Pointer },
    { offsetof(Dn2CppPropInfo, setter), Encoding::Pointer },
    { offsetof(Dn2CppPropInfo, attrs), Encoding::Signed32 },
    { offsetof(Dn2CppPropInfo, customAttrs), Encoding::Pointer },
    { offsetof(Dn2CppPropInfo, customAttrCount), Encoding::Signed32 },
    { offsetof(Dn2CppPropInfo, metadataToken), Encoding::Signed32 },
    { offsetof(Dn2CppPropInfo, display), Encoding::Pointer },
};
constexpr Field Dn2CppAttrInfo_fields[] = {
    { offsetof(Dn2CppAttrInfo, attrType), Encoding::Pointer },
    { offsetof(Dn2CppAttrInfo, create), Encoding::Pointer },
    { offsetof(Dn2CppAttrInfo, display), Encoding::Pointer },
};
constexpr Field Dn2CppEnumMember_fields[] = {
    { offsetof(Dn2CppEnumMember, name), Encoding::Pointer },
    { offsetof(Dn2CppEnumMember, value), Encoding::Signed64 },
};
constexpr Schema schemas[] = {
    { Dn2CppTypeReflection_fields, sizeof(Dn2CppTypeReflection_fields) / sizeof(Field), sizeof(Dn2CppTypeReflection) },
    { Dn2CppFieldInfo_fields, sizeof(Dn2CppFieldInfo_fields) / sizeof(Field), sizeof(Dn2CppFieldInfo) },
    { Dn2CppMethodInfo_fields, sizeof(Dn2CppMethodInfo_fields) / sizeof(Field), sizeof(Dn2CppMethodInfo) },
    { Dn2CppParamInfo_fields, sizeof(Dn2CppParamInfo_fields) / sizeof(Field), sizeof(Dn2CppParamInfo) },
    { Dn2CppPropInfo_fields, sizeof(Dn2CppPropInfo_fields) / sizeof(Field), sizeof(Dn2CppPropInfo) },
    { Dn2CppAttrInfo_fields, sizeof(Dn2CppAttrInfo_fields) / sizeof(Field), sizeof(Dn2CppAttrInfo) },
    { Dn2CppEnumMember_fields, sizeof(Dn2CppEnumMember_fields) / sizeof(Field), sizeof(Dn2CppEnumMember) },
};

// Relative records and native rows can cross subobject boundaries. Resolve
// their addresses as checked integers rather than out-of-array pointer offsets.
const uint8_t* metadata_add(const void* base, uint64_t offset)
{
    uintptr_t address = reinterpret_cast<uintptr_t>(base);
    if (offset > UINTPTR_MAX - address)
        dn2cpp_throw_invalid_operation();
    return reinterpret_cast<const uint8_t*>(address + static_cast<uintptr_t>(offset));
}

const uint8_t* metadata_subtract(const void* base, uint64_t offset)
{
    uintptr_t address = reinterpret_cast<uintptr_t>(base);
    if (offset > address)
        dn2cpp_throw_invalid_operation();
    return reinterpret_cast<const uint8_t*>(address - static_cast<uintptr_t>(offset));
}

const uint8_t* metadata_index(const void* base, std::size_t stride, uint64_t index)
{
    if (stride != 0 && index > UINTPTR_MAX / stride)
        dn2cpp_throw_invalid_operation();
    return metadata_add(base, index * stride);
}

uint64_t read_unsigned(const uint8_t*& cursor, const uint8_t* end = nullptr)
{
    uint64_t result = 0;
    for (unsigned shift = 0; shift < 64; shift += 7)
    {
        if (end != nullptr && cursor == end)
            dn2cpp_throw_invalid_operation();
        uint8_t byte = *cursor++;
        if (shift == 63 && byte > 1)
            dn2cpp_throw_invalid_operation();
        result |= uint64_t(byte & 0x7f) << shift;
        if ((byte & 0x80) == 0)
            return result;
    }
    dn2cpp_throw_invalid_operation();
}

int32_t access_flags(int32_t attrs)
{
    int32_t result = (attrs & 0x10) != 0 ? DN2CPP_FLDA_STATIC : 0;
    if ((attrs & 7) == 6) result |= DN2CPP_FLDA_PUBLIC;
    if ((attrs & 7) == 1) result |= DN2CPP_FLDA_PRIVATE;
    return result;
}
}

const void* dn2cpp_metadata_at(const void* table, Dn2CppMetadataKind kind,
    std::size_t stride, std::size_t index)
{
    if ((reinterpret_cast<uintptr_t>(table) & 1) == 0)
    {
        if (kind == Dn2CppMetadataKind::Method && table != nullptr
            && dn2cpp_metadata_is_method_delta(table))
            stride = sizeof(Dn2CppMethodDelta);
        if (table == nullptr && index != 0)
            dn2cpp_throw_invalid_operation();
        return metadata_index(table, stride, index);
    }
    auto* record = metadata_subtract(table, 1);
    for (std::size_t i = 0; i < index; i++)
    {
        auto* cursor = record;
        read_unsigned(cursor);
        uint64_t length = read_unsigned(cursor);
        if (length < static_cast<uint64_t>(cursor - record) || (length & 1) != 0)
            dn2cpp_throw_invalid_operation();
        record = metadata_add(record, length);
    }
    return metadata_add(record, 1);
}

bool dn2cpp_metadata_is_image_method(const void* handle)
{
    if ((reinterpret_cast<uintptr_t>(handle) & 1) == 0)
        return false;
    const auto* record = metadata_subtract(handle, 1);
    auto* cursor = record;
    uint64_t block = read_unsigned(cursor);
    if (block >= dn2cpp_metadata_block_count)
        return false;
    const auto& image = dn2cpp_metadata_blocks[block];
    uintptr_t offset = reinterpret_cast<uintptr_t>(record)
        - reinterpret_cast<uintptr_t>(image.methodRecords);
    if (image.methodRecords == nullptr || offset >= image.methodRecordsSize
        || static_cast<uint64_t>(cursor - record) >= image.methodRecordsSize - offset)
        return false;
    uint64_t length = read_unsigned(cursor,
        metadata_add(image.methodRecords, image.methodRecordsSize));
    return length >= static_cast<uint64_t>(cursor - record)
        && (length & 1) == 0 && length <= image.methodRecordsSize - offset;
}

void dn2cpp_metadata_decode(void* destination, Dn2CppMetadataKind kind, const void* handle)
{
    const Schema& schema = schemas[static_cast<unsigned>(kind)];
    if (handle == nullptr)
    {
        std::memset(destination, 0, schema.size);
        return;
    }
    if ((reinterpret_cast<uintptr_t>(handle) & 1) == 0)
    {
        if (kind == Dn2CppMetadataKind::Method
            && dn2cpp_metadata_is_method_delta(handle))
        {
            const auto* delta = static_cast<const Dn2CppMethodDelta*>(handle);
            dn2cpp_metadata_decode(destination, kind, delta->original.identity());
            static_cast<Dn2CppMethodInfo*>(destination)->declaringType = delta->declaringType;
            return;
        }
        std::memcpy(destination, handle, schema.size);
        return;
    }
    std::memset(destination, 0, schema.size);
    const auto* record = metadata_subtract(handle, 1);
    auto* cursor = record;
    uint64_t block = read_unsigned(cursor);
    uint64_t length = read_unsigned(cursor);
    if (length < static_cast<uint64_t>(cursor - record) || (length & 1) != 0)
        dn2cpp_throw_invalid_operation();
    const auto* end = metadata_add(record, length);
    const Dn2CppMetadataBlock* localBlock = nullptr;
    if (block == UINT64_MAX)
    {
        uint64_t backwards = read_unsigned(cursor, end);
        localBlock = reinterpret_cast<const Dn2CppMetadataBlock*>(metadata_subtract(record, backwards));
    }
    else if (block >= dn2cpp_metadata_block_count)
        dn2cpp_throw_invalid_operation();
    uint64_t presence = read_unsigned(cursor, end);
    if (schema.count < 64 && (presence >> schema.count) != 0)
        dn2cpp_throw_invalid_operation();
    for (std::size_t i = 0; i < schema.count; i++)
    {
        if ((presence & (uint64_t(1) << i)) == 0)
            continue;
        uint64_t value = read_unsigned(cursor, end);
        auto* field = static_cast<uint8_t*>(destination) + schema.fields[i].offset;
        switch (schema.fields[i].encoding)
        {
            case Encoding::Pointer:
            {
                const void* pointer = nullptr;
                if (value != 0)
                {
                    const auto& source = localBlock != nullptr ? *localBlock : dn2cpp_metadata_blocks[block];
                    const auto* slot = metadata_index(source.pointers, sizeof(void*), value - 1);
                    std::memcpy(&pointer, slot, sizeof(pointer));
                }
                std::memcpy(field, &pointer, sizeof(pointer));
                break;
            }
            case Encoding::Signed32:
            {
                if (value > UINT32_MAX)
                    dn2cpp_throw_invalid_operation();
                uint32_t bits = static_cast<uint32_t>((value >> 1) ^ (uint64_t(0) - (value & 1)));
                std::memcpy(field, &bits, sizeof(bits));
                break;
            }
            case Encoding::Unsigned32:
            {
                if (value > UINT32_MAX)
                    dn2cpp_throw_invalid_operation();
                uint32_t bits = static_cast<uint32_t>(value);
                std::memcpy(field, &bits, sizeof(bits));
                break;
            }
            case Encoding::Signed64:
            {
                uint64_t bits = (value >> 1) ^ (uint64_t(0) - (value & 1));
                std::memcpy(field, &bits, sizeof(bits));
                break;
            }
        }
    }
    if (kind == Dn2CppMetadataKind::Field && (presence & (uint64_t(1) << 3)) == 0)
    {
        auto* field = static_cast<Dn2CppFieldInfo*>(destination);
        field->attrs = access_flags(field->ilAttrs)
            | ((field->ilAttrs & 0x20) != 0 ? DN2CPP_FLDA_INITONLY : 0)
            | ((field->ilAttrs & 0x40) != 0 ? DN2CPP_FLDA_LITERAL : 0);
    }
    if (kind == Dn2CppMetadataKind::Method && (presence & (uint64_t(1) << 5)) == 0)
    {
        auto* method = static_cast<Dn2CppMethodInfo*>(destination);
        method->attrs = access_flags(method->ilAttrs)
            | ((method->ilAttrs & 0x800) != 0 ? DN2CPP_MTHA_SPECIALNAME : 0)
            | (method->genericParamCount != 0 ? DN2CPP_MTHA_GENERIC : 0);
    }
}

Dn2CppString* dn2cpp_metadata_string(const char* display)
{
    const auto* start = reinterpret_cast<const uint8_t*>(display);
    if (*start != 0xff)
    {
        int32_t bytes = dn2cpp_string_checked_length(std::strlen(display));
        int32_t length = dn2cpp_utf8_to_utf16(display, bytes, nullptr);
        char16_t* buffer;
        auto* result = dn2cpp_string_alloc(&buffer, length);
        dn2cpp_utf8_to_utf16(display, bytes, buffer);
        return result;
    }
    auto* cursor = start + 1;
    uint64_t block = read_unsigned(cursor);
    if (block >= dn2cpp_metadata_block_count)
        dn2cpp_throw_invalid_operation();
    uint64_t count = read_unsigned(cursor);
    const auto* tokens = cursor;
    int64_t length = 0;
    for (uint64_t i = 0; i < count; i++)
    {
        const char* token = dn2cpp_metadata_blocks[block].displayTokens[read_unsigned(cursor)];
        int32_t bytes = dn2cpp_string_checked_length(std::strlen(token));
        length += dn2cpp_utf8_to_utf16(token, bytes, nullptr);
        dn2cpp_string_checked_length(length);
    }
    char16_t* buffer;
    auto* result = dn2cpp_string_alloc(&buffer, static_cast<int32_t>(length));
    cursor = tokens;
    int32_t written = 0;
    for (uint64_t i = 0; i < count; i++)
    {
        const char* token = dn2cpp_metadata_blocks[block].displayTokens[read_unsigned(cursor)];
        int32_t bytes = static_cast<int32_t>(std::strlen(token));
        written += dn2cpp_utf8_to_utf16(token, bytes, buffer + written);
    }
    return result;
}
