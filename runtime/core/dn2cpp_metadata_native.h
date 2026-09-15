#pragma once

#include "dn2cpp_core.h"
#include <array>
#include <utility>

// Adjacent local blocks keep each record's pointer dependencies together without
// rooting metadata or accessor thunks belonging to unrelated blocks.
namespace dn2cpp_native_metadata {
using Getter = Dn2CppObject* (*)(Dn2CppObject*);
using Setter = void (*)(Dn2CppObject*, Dn2CppObject*);
union Pointer
{
    const void* object;
    Getter getter;
    Setter setter;
    constexpr Pointer() : object(nullptr) {}
    constexpr Pointer(const void* p) : object(p) {}
    constexpr Pointer(Getter p) : getter(p) {}
    constexpr Pointer(Setter p) : setter(p) {}
};
static_assert(sizeof(Pointer) == sizeof(void*));

struct Source
{
    const void* object = nullptr;
    Getter getter = nullptr;
    Setter setter = nullptr;
    const char* text = nullptr;
    unsigned kind = 0;
    constexpr Source() = default;
    constexpr Source(const void* p) : object(p) {}
    constexpr Source(const char* p) : text(p), kind(3) {}
    constexpr Source(Getter p) : getter(p), kind(1) {}
    constexpr Source(Setter p) : setter(p), kind(2) {}
    constexpr bool empty() const
    { return object == nullptr && getter == nullptr && setter == nullptr && text == nullptr; }
    constexpr bool operator==(const Source& other) const
    {
        if (kind != other.kind)
            return false;
        if (kind == 3)
        {
            if (text == nullptr || other.text == nullptr)
                return text == nullptr && other.text == nullptr;
            std::size_t i = 0;
            while (text[i] != 0 && text[i] == other.text[i]) i++;
            return text[i] == other.text[i];
        }
        return object == other.object && getter == other.getter && setter == other.setter;
    }
    constexpr Pointer pointer() const
    {
        return kind == 1 ? Pointer(getter) : kind == 2 ? Pointer(setter)
            : kind == 3 ? Pointer(static_cast<const void*>(text)) : Pointer(object);
    }
};

struct Value
{
    uint64_t scalar = 0;
    Source pointer{};
    bool isPointer = false;
    constexpr Value() = default;
    constexpr Value(int64_t value)
        : scalar(value < 0 ? ((~uint64_t(value)) << 1) | 1 : uint64_t(value) << 1) {}
    constexpr Value(uint32_t value) : scalar(value) {}
    constexpr Value(Source value) : pointer(value), isPointer(true) {}
    constexpr bool present() const { return isPointer ? !pointer.empty() : scalar != 0; }
};
constexpr Value pointer(const char* p) { return Value(Source(p)); }
constexpr Value pointer(const void* p) { return Value(Source(p)); }
constexpr Value scalar(int64_t value) { return Value(value); }

constexpr std::size_t field_count(const Dn2CppFieldInfo&) { return 12; }
constexpr Value value(const Dn2CppFieldInfo& row, std::size_t field)
{
    switch (field)
    {
        case 0: return pointer(row.name);
        case 1: return pointer(row.declaringType);
        case 2: return pointer(row.fieldType);
        case 3: return scalar(row.attrs);
        case 4: return Value(Source(row.getter));
        case 5: return Value(Source(row.setter));
        case 6: return pointer(row.customAttrs.identity());
        case 7: return scalar(row.customAttrCount);
        case 8: return scalar(row.ilAttrs);
        case 9: return scalar(row.metadataToken);
        case 10: return scalar(row.literalValue);
        case 11: return pointer(row.display);
    }
    return {};
}
constexpr std::size_t field_count(const Dn2CppTypeReflection&) { return 22; }
constexpr Value value(const Dn2CppTypeReflection& row, std::size_t field)
{
    switch (field)
    {
        case 0: return pointer(row.fields.identity());
        case 1: return scalar(row.fieldCount);
        case 2: return pointer(row.methods.identity());
        case 3: return scalar(row.methodCount);
        case 4: return pointer(row.ctors.identity());
        case 5: return scalar(row.ctorCount);
        case 6: return pointer(row.props.identity());
        case 7: return scalar(row.propCount);
        case 8: return pointer(row.customAttrs.identity());
        case 9: return scalar(row.customAttrCount);
        case 10: return pointer(row.enumMembers.identity());
        case 11: return scalar(row.enumMemberCount);
        case 12: return pointer(row.nestedTypes);
        case 13: return scalar(row.nestedCount);
        case 14: return pointer(row.assemblyName);
        case 15: return Value(row.ilAttrs);
        case 16: return scalar(row.metadataToken);
        case 17: return pointer(row.defaultMemberName);
        case 18: return scalar(row.marshalSize);
        case 19: return pointer(row.eventSourceName);
        case 20: return pointer(row.eventSourceGuid);
        case 21: return pointer(row.genericParamNames);
    }
    return {};
}

constexpr bool present(const Dn2CppTypeReflection& row, std::size_t field)
{
    return value(row, field).present();
}
constexpr bool present(const Dn2CppFieldInfo& row, std::size_t field)
{
    if (field != 3)
        return value(row, field).present();
    int32_t derived = (row.ilAttrs & 0x10) != 0 ? DN2CPP_FLDA_STATIC : 0;
    if ((row.ilAttrs & 7) == 6) derived |= DN2CPP_FLDA_PUBLIC;
    if ((row.ilAttrs & 7) == 1) derived |= DN2CPP_FLDA_PRIVATE;
    if ((row.ilAttrs & 0x20) != 0) derived |= DN2CPP_FLDA_INITONLY;
    if ((row.ilAttrs & 0x40) != 0) derived |= DN2CPP_FLDA_LITERAL;
    return row.attrs != derived;
}

template<std::size_t Capacity> struct Sources
{
    std::array<Source, Capacity> entries{};
    std::size_t count = 0;
    constexpr std::size_t index(Source source) const
    {
        for (std::size_t i = 0; i < count; i++)
            if (entries[i] == source)
                return i + 1;
        return 0;
    }
};

template<auto& Rows> constexpr auto make_sources()
{
    constexpr auto rowCount = sizeof(Rows) / sizeof(Rows[0]);
    Sources<rowCount * field_count(Rows[0])> result;
    for (std::size_t r = 0; r < rowCount; r++)
        for (std::size_t f = 0; f < field_count(Rows[r]); f++)
        {
            auto v = value(Rows[r], f);
            if (v.isPointer && v.present() && result.index(v.pointer) == 0)
                result.entries[result.count++] = v.pointer;
        }
    return result;
}
template<auto& Rows> inline constexpr auto sources = make_sources<Rows>();

template<std::size_t Count> struct PointerArray
{
    Pointer values[Count != 0 ? Count : 1];
};

template<std::size_t Pointers, std::size_t Bytes> struct Block
{
    Dn2CppMetadataBlock block;
    PointerArray<Pointers> pointers;
    alignas(2) std::array<uint8_t, Bytes> records;
};

constexpr std::size_t unsigned_size(uint64_t value)
{
    std::size_t size = 1;
    while (value >= 128) { value >>= 7; size++; }
    return size;
}
template<auto& Rows> constexpr uint64_t presence(std::size_t row)
{
    uint64_t bits = 0;
    for (std::size_t f = 0; f < field_count(Rows[row]); f++)
        if (present(Rows[row], f))
            bits |= uint64_t(1) << f;
    return bits;
}
template<auto& Rows> constexpr std::size_t record_size(std::size_t row, std::size_t offset)
{
    std::size_t size = 10 + unsigned_size(offset) + unsigned_size(presence<Rows>(row));
    for (std::size_t f = 0; f < field_count(Rows[row]); f++)
    {
        auto v = value(Rows[row], f);
        if (present(Rows[row], f))
            size += unsigned_size(v.isPointer ? sources<Rows>.index(v.pointer) : v.scalar);
    }
    std::size_t total = size + 1;
    while (true)
    {
        std::size_t next = (size + unsigned_size(total) + 1) & ~std::size_t(1);
        if (next == total)
            return total;
        total = next;
    }
}
template<auto& Rows> constexpr std::size_t byte_count()
{
    using Storage = Block<sources<Rows>.count, 1>;
    constexpr std::size_t base = offsetof(Storage, records);
    std::size_t bytes = 0;
    for (std::size_t r = 0; r < sizeof(Rows) / sizeof(Rows[0]); r++)
        bytes += record_size<Rows>(r, base + bytes);
    return bytes;
}
template<auto& Rows> using Storage = Block<sources<Rows>.count, byte_count<Rows>()>;

template<auto& Rows, std::size_t... I> constexpr auto make_pointers(std::index_sequence<I...>)
{
    return PointerArray<sizeof...(I)>{ { sources<Rows>.entries[I].pointer()... } };
}
template<std::size_t Size> constexpr void write_unsigned(
    std::array<uint8_t, Size>& bytes, std::size_t& cursor, uint64_t value)
{
    do
    {
        uint8_t byte = static_cast<uint8_t>(value & 127);
        value >>= 7;
        bytes[cursor++] = byte | (value != 0 ? 128 : 0);
    } while (value != 0);
}
template<auto& Rows> constexpr auto make_bytes()
{
    using Local = Storage<Rows>;
    constexpr std::size_t base = offsetof(Local, records);
    std::array<uint8_t, byte_count<Rows>()> bytes{};
    std::size_t cursor = 0;
    for (std::size_t r = 0; r < sizeof(Rows) / sizeof(Rows[0]); r++)
    {
        std::size_t start = cursor;
        std::size_t size = record_size<Rows>(r, base + start);
        write_unsigned(bytes, cursor, UINT64_MAX);
        write_unsigned(bytes, cursor, size);
        write_unsigned(bytes, cursor, base + start);
        write_unsigned(bytes, cursor, presence<Rows>(r));
        for (std::size_t f = 0; f < field_count(Rows[r]); f++)
        {
            auto v = value(Rows[r], f);
            if (present(Rows[r], f))
                write_unsigned(bytes, cursor, v.isPointer ? sources<Rows>.index(v.pointer) : v.scalar);
        }
        cursor = start + size;
    }
    return bytes;
}
}

#define DN2CPP_NATIVE_METADATA_STORAGE(Name) \
    static constexpr dn2cpp_native_metadata::Storage<Name##_rows> Name##_storage = { \
        { Name##_storage.pointers.values, nullptr }, \
        dn2cpp_native_metadata::make_pointers<Name##_rows>( \
            std::make_index_sequence<dn2cpp_native_metadata::sources<Name##_rows>.count>{}), \
        dn2cpp_native_metadata::make_bytes<Name##_rows>() }

// Shared runtime metadata stays native across application emission policies.
#define DN2CPP_NATIVE_FIELDS(Name, ...) \
    static constexpr Dn2CppFieldInfo Name##_rows[] = { __VA_ARGS__ }; \
    static constexpr Dn2CppMetadataTable<Dn2CppFieldInfo> Name{ Name##_rows }

#define DN2CPP_NATIVE_TYPE_REFLECTION(Name, ...) \
    static constexpr Dn2CppTypeReflection Name##_rows[] = { { __VA_ARGS__ } }; \
    static constexpr Dn2CppMetadataHandle<Dn2CppTypeReflection> Name{ Name##_rows }
