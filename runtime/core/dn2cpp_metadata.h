#pragma once

#include <cstddef>
#include <cstdint>
#include <type_traits>

struct Dn2CppString;
struct Dn2CppTypeReflection;
struct Dn2CppFieldInfo;
struct Dn2CppMethodInfo;
struct Dn2CppParamInfo;
struct Dn2CppPropInfo;
struct Dn2CppAttrInfo;
struct Dn2CppEnumMember;

struct Dn2CppMetadataBlock
{
    const void* pointers;
    const char* const* displayTokens;
};
extern const Dn2CppMetadataBlock dn2cpp_metadata_blocks[];

enum class Dn2CppMetadataKind : unsigned char
{
    Type, Field, Method, Parameter, Property, Attribute, EnumMember
};
template<class Row> struct Dn2CppMetadataTraits;
#define DN2CPP_METADATA_KIND(Row, Kind) \
    template<> struct Dn2CppMetadataTraits<Row> \
    { static constexpr auto kind = Dn2CppMetadataKind::Kind; }
DN2CPP_METADATA_KIND(Dn2CppTypeReflection, Type);
DN2CPP_METADATA_KIND(Dn2CppFieldInfo, Field);
DN2CPP_METADATA_KIND(Dn2CppMethodInfo, Method);
DN2CPP_METADATA_KIND(Dn2CppParamInfo, Parameter);
DN2CPP_METADATA_KIND(Dn2CppPropInfo, Property);
DN2CPP_METADATA_KIND(Dn2CppAttrInfo, Attribute);
DN2CPP_METADATA_KIND(Dn2CppEnumMember, EnumMember);
#undef DN2CPP_METADATA_KIND

// Static records are even-aligned; the stored pointer is one byte past their
// start. Untagged pointers remain real native pointers for dynamically rooted rows.
// Records carry ULEB block id, padded byte length and presence bits, followed by
// schema-ordered ULEBs: signed values use zigzag, pointers use one-based pool ids.
// Block id UINT64_MAX inserts a backward offset before the presence mask, naming
// an adjacent block instead of the generated image's global block registry.
// Views decode on the stack; row identity always belongs to the original handle.
void dn2cpp_metadata_decode(void* destination, Dn2CppMetadataKind kind, const void* handle);
const void* dn2cpp_metadata_at(const void* table, Dn2CppMetadataKind kind, std::size_t stride, std::size_t index);

template<class Row> class Dn2CppMetadataHandle
{
    const void* data_ = nullptr;
    struct View
    {
        Row value{};
        explicit View(const void* data)
        {
            dn2cpp_metadata_decode(&value, Dn2CppMetadataTraits<Row>::kind, data);
        }
        const Row* operator->() const { return &value; }
    };
public:
    constexpr Dn2CppMetadataHandle() = default;
    constexpr Dn2CppMetadataHandle(const Row* data) : data_(data) {}
    static constexpr Dn2CppMetadataHandle from_static(const uint8_t* data)
    {
        Dn2CppMetadataHandle result;
        result.data_ = data + 1;
        return result;
    }
    static constexpr Dn2CppMetadataHandle from_raw(const void* data)
    {
        Dn2CppMetadataHandle result;
        result.data_ = data;
        return result;
    }
    constexpr const void* identity() const { return data_; }
    constexpr explicit operator bool() const { return data_ != nullptr; }
    View operator->() const { return View(data_); }
    Row operator*() const { return View(data_).value; }
    friend constexpr bool operator==(Dn2CppMetadataHandle a, Dn2CppMetadataHandle b)
    { return a.data_ == b.data_; }
    friend constexpr bool operator!=(Dn2CppMetadataHandle a, Dn2CppMetadataHandle b)
    { return a.data_ != b.data_; }
};

template<class Row> class Dn2CppMetadataTable
{
    const void* data_ = nullptr;
public:
    constexpr Dn2CppMetadataTable() = default;
    constexpr Dn2CppMetadataTable(const Row* data) : data_(data) {}
    static constexpr Dn2CppMetadataTable from_static(const uint8_t* data)
    {
        Dn2CppMetadataTable result;
        result.data_ = data + 1;
        return result;
    }
    static constexpr Dn2CppMetadataTable from_raw(const void* data)
    {
        Dn2CppMetadataTable result;
        result.data_ = data;
        return result;
    }
    constexpr const void* identity() const { return data_; }
    constexpr explicit operator bool() const { return data_ != nullptr; }
    Dn2CppMetadataHandle<Row> operator[](std::size_t index) const
    {
        return Dn2CppMetadataHandle<Row>::from_raw(dn2cpp_metadata_at(data_, Dn2CppMetadataTraits<Row>::kind, sizeof(Row), index));
    }
    friend constexpr bool operator==(Dn2CppMetadataTable a, Dn2CppMetadataTable b)
    { return a.data_ == b.data_; }
    friend constexpr bool operator!=(Dn2CppMetadataTable a, Dn2CppMetadataTable b)
    { return a.data_ != b.data_; }
};

static_assert(sizeof(Dn2CppMetadataHandle<Dn2CppMethodInfo>) == sizeof(void*));
static_assert(sizeof(Dn2CppMetadataTable<Dn2CppMethodInfo>) == sizeof(void*));
static_assert(std::is_trivially_copyable_v<Dn2CppMetadataHandle<Dn2CppMethodInfo>>);
static_assert(std::is_trivially_copyable_v<Dn2CppMetadataTable<Dn2CppMethodInfo>>);

// Display token streams begin with 0xff, which cannot begin valid UTF-8.
Dn2CppString* dn2cpp_metadata_string(const char* display);
