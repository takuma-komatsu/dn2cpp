// dn2cpp_system_convert.cpp — System.Convert intrinsics.
//
// The transpiler intercepts System.Convert and binds it to these routines:
// numeric conversions with the BCL's range/round semantics, ChangeType against a
// runtime Type, the boxed IConvertible overloads, radix (base 2/8/10/16) integer
// parse/format, and Base64 encode/decode. Behavior matches real .NET.
#include "dn2cpp_core.h"

#include <array>
#include <cstdint>
#include <cstring>
#include <cmath>

// ---- Convert numeric conversions ----

// Convert.ToInt32(double)/ToInt64(double) round to the nearest integer, ties to
// even (banker's rounding) — std::nearbyint under the default FE_TONEAREST does
// exactly that. The range guards mirror the BCL (value must round into range, so
// the bound is MAX+0.5 / MIN-0.5); out-of-range traps like OverflowException.
int32_t dn2cpp_convert_r8_to_i32(double value)
{
    if (!(value >= -2147483648.5 && value < 2147483647.5))
        dn2cpp_overflow();
    return static_cast<int32_t>(std::nearbyint(value));
}

int64_t dn2cpp_convert_r8_to_i64(double value)
{
    // 9223372036854775807 is not exactly representable as a double; the BCL uses
    // the same < 9223372036854775808.0 (== 2^63) upper guard.
    if (!(value >= -9223372036854775808.0 && value < 9223372036854775808.0))
        dn2cpp_overflow();
    return static_cast<int64_t>(std::nearbyint(value));
}

// Convert.ToInt32(long): narrowing with an OverflowException range check.
int32_t dn2cpp_convert_i64_to_i32(int64_t value)
{
    if (value < -2147483648LL || value > 2147483647LL)
        dn2cpp_overflow();
    return static_cast<int32_t>(value);
}

// ---- Generalized checked narrowing (the sub-word / unsigned Convert.To* family) ----
// All raise a catchable OverflowException, matching the BCL.

// Integer source -> [lo, hi] target range.
int64_t dn2cpp_convert_i64_checked(int64_t value, int64_t lo, int64_t hi)
{
    if (value < lo || value > hi)
        dn2cpp_overflow();
    return value;
}

// UInt64 source -> a target whose range tops out at `hi` (<= Int64.MaxValue).
int64_t dn2cpp_convert_u64_checked(uint64_t value, uint64_t hi)
{
    if (value > hi)
        dn2cpp_overflow();
    return static_cast<int64_t>(value);
}

// Signed source -> UInt64: any negative overflows.
uint64_t dn2cpp_convert_i64_to_u64(int64_t value)
{
    if (value < 0)
        dn2cpp_overflow();
    return static_cast<uint64_t>(value);
}

// Double/float source -> integer target: round ties-to-even (banker's), then
// range-check the rounded integer — Convert.ToByte(254.5) == 254 but
// Convert.ToByte(255.5) (rounds to the even 256) overflows.
int64_t dn2cpp_convert_r8_checked(double value, int64_t lo, int64_t hi)
{
    if (!(value >= -9223372036854775808.0 && value < 9223372036854775808.0))
        dn2cpp_overflow();
    int64_t r = static_cast<int64_t>(std::nearbyint(value));
    if (r < lo || r > hi)
        dn2cpp_overflow();
    return r;
}

// Double/float source -> UInt64, same round-then-check semantics
// (Convert.ToUInt64(-0.5) == 0; -0.51 rounds to -1 and overflows).
uint64_t dn2cpp_convert_r8_to_u64(double value)
{
    if (!(value >= -0.5 && value < 18446744073709551616.0))
        dn2cpp_overflow();
    double r = std::nearbyint(value);
    if (r < 0.0)
        r = 0.0; // (-0.5, 0) rounds to -0.0
    return static_cast<uint64_t>(r);
}

// Decimal source -> integer target: banker's-round at 0 digits then range-check
// (Convert.ToInt16(2.5m) == 2, Convert.ToInt16(3.5m) == 4).
int64_t dn2cpp_convert_dec_checked(Dn2CppDecimal value, int64_t lo, int64_t hi)
{
    Dn2CppDecimal r = dn2cpp_decimal_round(value, 0, 0 /* ToEven */);
    if (dn2cpp_decimal_cmp(r, dn2cpp_decimal_from_i8(lo)) < 0
        || dn2cpp_decimal_cmp(r, dn2cpp_decimal_from_i8(hi)) > 0)
        dn2cpp_overflow();
    return dn2cpp_decimal_to_i8(r);
}

uint64_t dn2cpp_convert_dec_to_u64(Dn2CppDecimal value)
{
    Dn2CppDecimal r = dn2cpp_decimal_round(value, 0, 0 /* ToEven */);
    if (dn2cpp_decimal_cmp(r, dn2cpp_decimal_from_i4(0)) < 0
        || dn2cpp_decimal_cmp(r, dn2cpp_decimal_from_u8(0xFFFFFFFFFFFFFFFFull)) > 0)
        dn2cpp_overflow();
    return dn2cpp_decimal_to_u8(r);
}

// Convert.To*(string): unlike Parse, a null string yields the target's zero.
// Otherwise the NumberStyles engine parses at the target width with the
// default integer styles.
int64_t dn2cpp_convert_str_to_int(Dn2CppString* s, int32_t bits, int32_t isSigned)
{
    if (s == nullptr)
        return 0;
    return dn2cpp_integer_parse_chars(s->chars, s->length, 7 /* Integer */, nullptr, bits, isSigned);
}

// Convert.ToDecimal(string): null -> 0m, else decimal.Parse (NumberStyles.Number).
Dn2CppDecimal dn2cpp_convert_str_to_decimal(Dn2CppString* s)
{
    if (s == nullptr)
        return dn2cpp_decimal_from_i4(0);
    return dn2cpp_decimal_parse_styles_chars(s->chars, s->length, 111 /* Number */, nullptr);
}

// Convert.ToDateTime(string): null -> DateTime.MinValue, else the invariant
// DateTime.Parse machinery (catchable FormatException on bad input).
Dn2CppDateTime dn2cpp_convert_str_to_datetime(Dn2CppString* s)
{
    if (s == nullptr)
        return dn2cpp_datetime_pack(0, 0);
    Dn2CppDateTime r;
    if (!dn2cpp_datetime_try_parse(s, &r))
        dn2cpp_throw_format();
    return r;
}

// Convert.ToBoolean(string): accepts "true"/"false" (case-insensitive, with
// optional surrounding whitespace), else FormatException — matching Boolean.Parse.
int32_t dn2cpp_convert_to_bool(Dn2CppString* s)
{
    if (s == nullptr)
        dn2cpp_throw_argument_null();
    int32_t i = 0, n = s->length;
    while (i < n && dn2cpp_is_ws(s->chars[i]))
        i++;
    while (n > i && dn2cpp_is_ws(s->chars[n - 1]))
        n--;
    int32_t len = n - i;
    auto ieq = [&](const char* lit, int32_t litLen) -> bool {
        if (len != litLen)
            return false;
        for (int32_t k = 0; k < litLen; k++)
        {
            char16_t c = s->chars[i + k];
            char16_t lc = (c >= u'A' && c <= u'Z') ? static_cast<char16_t>(c + 32) : c;
            if (lc != static_cast<char16_t>(lit[k]))
                return false;
        }
        return true;
    };
    if (ieq("true", 4))
        return 1;
    if (ieq("false", 5))
        return 0;
    dn2cpp_throw_format();
}

// ---- Convert.ChangeType(object, Type) ----

// Payload of a boxed value (just past the object header).
static inline const void* dn2cpp_box_payload(Dn2CppObject* v)
{
    return reinterpret_cast<const char*>(v) + sizeof(Dn2CppObject);
}

// Read a boxed primitive (or numeric string) as int64 — Convert.ChangeType's integer
// path. Floating sources are banker's-rounded; a string is parsed as a whole int64.
static int64_t dn2cpp_box_read_i64(Dn2CppObject* v)
{
    const Dn2CppTypeInfo* t = v->type;
    const void* p = dn2cpp_box_payload(v);
    if (t == &dn2cpp_bool_type || t == &dn2cpp_byte_type) return *static_cast<const uint8_t*>(p);
    if (t == &dn2cpp_sbyte_type) return *static_cast<const int8_t*>(p);
    if (t == &dn2cpp_int16_type) return *static_cast<const int16_t*>(p);
    if (t == &dn2cpp_uint16_type || t == &dn2cpp_char_type) return *static_cast<const uint16_t*>(p);
    if (t == &dn2cpp_int32_type) return *static_cast<const int32_t*>(p);
    if (t == &dn2cpp_uint32_type) return *static_cast<const uint32_t*>(p);
    if (t == &dn2cpp_int64_type) return *static_cast<const int64_t*>(p);
    if (t == &dn2cpp_uint64_type) return static_cast<int64_t>(*static_cast<const uint64_t*>(p));
    // IntPtr/UIntPtr: 8-byte payload, read at the matching signedness.
    if (t == &dn2cpp_intptr_type) return *static_cast<const intptr_t*>(p);
    if (t == &dn2cpp_uintptr_type) return static_cast<int64_t>(*static_cast<const uintptr_t*>(p));
    if (t == &dn2cpp_single_type) return dn2cpp_convert_r8_to_i64(*static_cast<const float*>(p));
    if (t == &dn2cpp_double_type) return dn2cpp_convert_r8_to_i64(*static_cast<const double*>(p));
    if (t == &dn2cpp_string_type) return dn2cpp_long_parse(reinterpret_cast<Dn2CppString*>(v));
    // A boxed Decimal narrows to integer with banker's rounding (matches Convert.ToInt*:
    // 2.5m -> 2, 3.5m -> 4), so round-to-even at 0 digits before truncating.
    if (t == &dn2cpp_decimal_type)
        return dn2cpp_decimal_to_i8(dn2cpp_decimal_round(*static_cast<const Dn2CppDecimal*>(p), 0, 0));
    // A boxed enum reads as its underlying value at the underlying's width and
    // signedness (as dn2cpp_object_gethashcode does). A 1/2/4-byte underlying rides
    // the box widened to int32, but a fixed int32 read would drop the high half of a
    // long/ulong enum and read a uint enum's high-bit values negative.
    if (t != nullptr && t->base == &dn2cpp_enum_type)
    {
        const Dn2CppTypeInfo* u = t->enumUnderlying;
        if (u == &dn2cpp_int64_type) return *static_cast<const int64_t*>(p);
        if (u == &dn2cpp_uint64_type) return static_cast<int64_t>(*static_cast<const uint64_t*>(p));
        if (u == &dn2cpp_uint32_type) return *static_cast<const uint32_t*>(p);
        return *static_cast<const int32_t*>(p);
    }
    dn2cpp_throw_invalid_operation();
}

// Read a boxed primitive (or numeric string) as double — Convert.ChangeType's floating /
// boolean path.
static double dn2cpp_box_read_f64(Dn2CppObject* v)
{
    const Dn2CppTypeInfo* t = v->type;
    const void* p = dn2cpp_box_payload(v);
    if (t == &dn2cpp_bool_type || t == &dn2cpp_byte_type) return *static_cast<const uint8_t*>(p);
    if (t == &dn2cpp_sbyte_type) return *static_cast<const int8_t*>(p);
    if (t == &dn2cpp_int16_type) return *static_cast<const int16_t*>(p);
    if (t == &dn2cpp_uint16_type || t == &dn2cpp_char_type) return *static_cast<const uint16_t*>(p);
    if (t == &dn2cpp_int32_type) return *static_cast<const int32_t*>(p);
    if (t == &dn2cpp_uint32_type) return static_cast<double>(*static_cast<const uint32_t*>(p));
    if (t == &dn2cpp_int64_type) return static_cast<double>(*static_cast<const int64_t*>(p));
    if (t == &dn2cpp_uint64_type) return static_cast<double>(*static_cast<const uint64_t*>(p));
    if (t == &dn2cpp_single_type) return *static_cast<const float*>(p);
    if (t == &dn2cpp_double_type) return *static_cast<const double*>(p);
    if (t == &dn2cpp_string_type) return dn2cpp_double_parse(reinterpret_cast<Dn2CppString*>(v));
    if (t == &dn2cpp_decimal_type) return dn2cpp_decimal_to_double(*static_cast<const Dn2CppDecimal*>(p));
    // Boxed enum: same width/signedness split as the i64 reader above.
    if (t != nullptr && t->base == &dn2cpp_enum_type)
    {
        const Dn2CppTypeInfo* u = t->enumUnderlying;
        if (u == &dn2cpp_int64_type) return static_cast<double>(*static_cast<const int64_t*>(p));
        if (u == &dn2cpp_uint64_type) return static_cast<double>(*static_cast<const uint64_t*>(p));
        if (u == &dn2cpp_uint32_type) return static_cast<double>(*static_cast<const uint32_t*>(p));
        return *static_cast<const int32_t*>(p);
    }
    dn2cpp_throw_invalid_operation();
}

// Format a boxed primitive as its .NET string (Convert.ChangeType(.., typeof(string))).
static Dn2CppString* dn2cpp_box_to_string(Dn2CppObject* v)
{
    const Dn2CppTypeInfo* t = v->type;
    if (t == &dn2cpp_string_type) return reinterpret_cast<Dn2CppString*>(v);
    if (t == &dn2cpp_bool_type) return dn2cpp_bool_to_string(static_cast<int32_t>(dn2cpp_box_read_i64(v)));
    if (t == &dn2cpp_single_type) return dn2cpp_float_to_string(static_cast<float>(dn2cpp_box_read_f64(v)));
    if (t == &dn2cpp_double_type) return dn2cpp_double_to_string(dn2cpp_box_read_f64(v));
    if (t == &dn2cpp_char_type) return dn2cpp_char_to_string(static_cast<char16_t>(dn2cpp_box_read_i64(v)));
    if (t == &dn2cpp_decimal_type) return dn2cpp_decimal_to_string(*static_cast<const Dn2CppDecimal*>(dn2cpp_box_payload(v)));
    // A boxed enum stringifies to its member name (Convert.ToString(enum) == enum.ToString()),
    // recovered from the per-enum (name, value) table; an undefined value falls back to the
    // underlying number (flag combinations stay a carve-out).
    if (t != nullptr && t->base == &dn2cpp_enum_type)
    {
        // Model width, as dn2cpp_object_tostring's twin arm reads it: a 64-bit-underlying
        // enum's box carries 8 bytes, and an int32 read names the truncated member.
        bool wide = t->enumUnderlying == &dn2cpp_int64_type || t->enumUnderlying == &dn2cpp_uint64_type;
        const void* p = dn2cpp_box_payload(v);
        int64_t ev = wide ? *static_cast<const int64_t*>(p) : *static_cast<const int32_t*>(p);
        for (int32_t i = 0; i < t->enumMemberCount; i++)
        {
            int64_t mv = t->enumMembers[i].value;
            if (wide ? mv == ev : static_cast<int32_t>(mv) == static_cast<int32_t>(ev))
                return dn2cpp_string_from_utf8(t->enumMembers[i].name,
                    static_cast<int32_t>(std::strlen(t->enumMembers[i].name)));
        }
        return wide ? dn2cpp_long_to_string(ev) : dn2cpp_int_to_string(static_cast<int32_t>(ev));
    }
    // UInt64 / UIntPtr must format unsigned (their bit pattern can exceed Int64.MaxValue).
    if (t == &dn2cpp_uint64_type || t == &dn2cpp_uintptr_type)
        return dn2cpp_format_uint(*static_cast<const uint64_t*>(dn2cpp_box_payload(v)), 8, nullptr);
    // every remaining integral width (incl. signed IntPtr) formats from int64
    return dn2cpp_long_to_string(dn2cpp_box_read_i64(v));
}

// Convert.ChangeType(value, targetType): convert a boxed value to targetType via the
// IConvertible matrix (the common practical subset — primitives + string, the shapes a
// deserializer hits). A null value passes through (matches .NET for reference/Nullable
// targets); an unsupported source/target throws InvalidOperation (standing in for
// InvalidCastException). String<->primitive uses the same parse/format helpers as the
// statically-typed Convert.To* intrinsics, so results are identical.

static Dn2CppObject* dn2cpp_change_type_to(Dn2CppObject* value, const Dn2CppTypeInfo* tt)
{
    if (value == nullptr || tt == nullptr)
        return nullptr;
    if (value->type == tt)
        return value; // already the target type
    if (tt == &dn2cpp_string_type)
        return reinterpret_cast<Dn2CppObject*>(dn2cpp_box_to_string(value));
    if (tt == &dn2cpp_decimal_type)
    {
        // A string parses; a floating source keeps its fraction (2.5 -> 2.5m); every
        // other (integral / bool / enum) source converts through its integer value.
        Dn2CppDecimal d = (value->type == &dn2cpp_string_type)
            ? dn2cpp_decimal_parse(reinterpret_cast<Dn2CppString*>(value))
            : (value->type == &dn2cpp_single_type || value->type == &dn2cpp_double_type)
                ? dn2cpp_decimal_from_double(dn2cpp_box_read_f64(value))
                : dn2cpp_decimal_from_i8(dn2cpp_box_read_i64(value));
        return dn2cpp_box(tt, &d, sizeof(d));
    }
    if (tt == &dn2cpp_datetime_type)
    {
        // Only a string source converts (IConvertible.ToDateTime, via the
        // invariant DateTime.Parse machinery). A DateTime source is the identity case
        // above; every numeric source throws InvalidCast in real .NET.
        if (value->type != &dn2cpp_string_type)
            dn2cpp_throw_invalid_operation();
        Dn2CppDateTime dt = dn2cpp_datetime_parse(reinterpret_cast<Dn2CppString*>(value));
        return dn2cpp_box(tt, &dt, sizeof(dt));
    }
    if (tt == &dn2cpp_bool_type)
    {
        uint8_t b = (value->type == &dn2cpp_string_type)
            ? static_cast<uint8_t>(dn2cpp_convert_to_bool(reinterpret_cast<Dn2CppString*>(value)))
            : static_cast<uint8_t>(dn2cpp_box_read_f64(value) != 0.0 ? 1 : 0);
        return dn2cpp_box(tt, &b, sizeof(b));
    }
    if (tt == &dn2cpp_double_type)
    {
        double d = dn2cpp_box_read_f64(value);
        return dn2cpp_box(tt, &d, sizeof(d));
    }
    if (tt == &dn2cpp_single_type)
    {
        float f = static_cast<float>(dn2cpp_box_read_f64(value));
        return dn2cpp_box(tt, &f, sizeof(f));
    }
    if (tt == &dn2cpp_int64_type)
    {
        int64_t i = dn2cpp_box_read_i64(value);
        return dn2cpp_box(tt, &i, sizeof(i));
    }
    if (tt == &dn2cpp_uint64_type)
    {
        // A string source parses unsigned (so values > Int64.MaxValue round-trip);
        // every other source reads through its integer value, reinterpreted unsigned.
        uint64_t i = (value->type == &dn2cpp_string_type)
            ? static_cast<uint64_t>(dn2cpp_convert_from_base_any(
                  reinterpret_cast<Dn2CppString*>(value), 10, 64, 1))
            : static_cast<uint64_t>(dn2cpp_box_read_i64(value));
        return dn2cpp_box(tt, &i, sizeof(i));
    }
    if (tt == &dn2cpp_uint32_type)
    {
        uint32_t i = static_cast<uint32_t>(dn2cpp_box_read_i64(value));
        return dn2cpp_box(tt, &i, sizeof(i));
    }
    if (tt == &dn2cpp_int32_type)
    {
        int32_t i = (value->type == &dn2cpp_string_type)
            ? dn2cpp_int_parse(reinterpret_cast<Dn2CppString*>(value))
            : dn2cpp_convert_i64_to_i32(dn2cpp_box_read_i64(value));
        return dn2cpp_box(tt, &i, sizeof(i));
    }
    if (tt == &dn2cpp_int16_type)
    {
        int16_t i = static_cast<int16_t>(dn2cpp_box_read_i64(value));
        return dn2cpp_box(tt, &i, sizeof(i));
    }
    if (tt == &dn2cpp_uint16_type)
    {
        uint16_t i = static_cast<uint16_t>(dn2cpp_box_read_i64(value));
        return dn2cpp_box(tt, &i, sizeof(i));
    }
    if (tt == &dn2cpp_byte_type)
    {
        uint8_t i = static_cast<uint8_t>(dn2cpp_box_read_i64(value));
        return dn2cpp_box(tt, &i, sizeof(i));
    }
    if (tt == &dn2cpp_sbyte_type)
    {
        int8_t i = static_cast<int8_t>(dn2cpp_box_read_i64(value));
        return dn2cpp_box(tt, &i, sizeof(i));
    }
    if (tt == &dn2cpp_char_type)
    {
        uint16_t i = static_cast<uint16_t>(dn2cpp_box_read_i64(value));
        return dn2cpp_box(tt, &i, sizeof(i));
    }
    dn2cpp_throw_invalid_operation(); // unsupported target type (incl. enum target, matching .NET)
}

Dn2CppObject* dn2cpp_convert_change_type(Dn2CppObject* value, Dn2CppType* targetType)
{
    if (value == nullptr || targetType == nullptr)
        return nullptr;
    return dn2cpp_change_type_to(value, targetType->typeInfo);
}

// Whether a type implements System.IConvertible. The interface set is walked at
// every level of the base chain, not just on the entry type: an implementation
// inherited from a base is still an implementation, and stopping at the entry type
// would report a deriving type as non-convertible.
static bool dn2cpp_implements_iconvertible(const Dn2CppTypeInfo* ti)
{
    for (; ti != nullptr; ti = ti->base)
        for (int32_t i = 0; i < ti->interfaceCount; i++)
        {
            const Dn2CppTypeInfo* itf = ti->interfaces[i].itf;
            if (itf != nullptr && itf->name != nullptr
                && std::strcmp(itf->name, "System.IConvertible") == 0)
                return true;
        }
    return false;
}

int32_t dn2cpp_convert_get_type_code(Dn2CppObject* value)
{
    if (value == nullptr)
        return 0; // TypeCode.Empty
    int32_t code = dn2cpp_type_get_type_code(value->type);
    if (code != 1)
        return code; // provably what this type's IConvertible.GetTypeCode() returns
    // TypeCode.Object from the type. Real .NET agrees unless the value implements
    // IConvertible, in which case it returns whatever that implementation says — a
    // body this lowering cannot ask. Say so rather than answer Object, which would
    // be a plausible wrong number carrying no diagnostic.
    if (dn2cpp_implements_iconvertible(value->type))
    {
        char msg[512];
        std::snprintf(msg, sizeof(msg),
            "Convert.GetTypeCode: '%s' implements IConvertible, whose GetTypeCode() dn2cpp "
            "cannot dispatch; only values whose TypeCode follows from the type itself (the "
            "primitives, String, Decimal, DateTime, DBNull and enums) are supported",
            value->type != nullptr && value->type->name != nullptr ? value->type->name : "?");
        dn2cpp_throw_platform_not_supported(msg);
    }
    return 1; // TypeCode.Object — real .NET's answer for a non-IConvertible value
}

// Map a System.TypeCode (the integer the enum boxes to) to the modeled boxed type-info,
// or nullptr for a code with no boxed representation (Empty/Object/DBNull).
static const Dn2CppTypeInfo* dn2cpp_type_info_for_code(int32_t code)
{
    switch (code)
    {
        case 16: return &dn2cpp_datetime_type; // DateTime
        case 3:  return &dn2cpp_bool_type;    // Boolean
        case 4:  return &dn2cpp_char_type;    // Char
        case 5:  return &dn2cpp_sbyte_type;   // SByte
        case 6:  return &dn2cpp_byte_type;    // Byte
        case 7:  return &dn2cpp_int16_type;   // Int16
        case 8:  return &dn2cpp_uint16_type;  // UInt16
        case 9:  return &dn2cpp_int32_type;   // Int32
        case 10: return &dn2cpp_uint32_type;  // UInt32
        case 11: return &dn2cpp_int64_type;   // Int64
        case 12: return &dn2cpp_uint64_type;  // UInt64
        case 13: return &dn2cpp_single_type;  // Single
        case 14: return &dn2cpp_double_type;  // Double
        case 15: return &dn2cpp_decimal_type; // Decimal
        case 18: return &dn2cpp_string_type;  // String
        default: return nullptr;
    }
}

Dn2CppObject* dn2cpp_convert_change_type_code(Dn2CppObject* value, int32_t typeCode)
{
    if (value == nullptr)
        return nullptr;
    const Dn2CppTypeInfo* tt = dn2cpp_type_info_for_code(typeCode);
    if (tt == nullptr)
        dn2cpp_throw_invalid_operation(); // unsupported TypeCode target
    return dn2cpp_change_type_to(value, tt);
}

// ---- Convert.To*(object, IFormatProvider) — boxed-source IConvertible overloads ----
// dn2cpp's own attribute-value rendering (CppEmitter.RenderAttrValue /
// RenderPrimitiveAttrLiteral) calls these object overloads. They read the boxed source
// through the same matrix as Convert.ChangeType (box_read_i64/f64: integral widen, a
// floating source banker's-rounds to integer, a numeric string parses). The provider is
// InvariantCulture everywhere in dn2cpp and is ignored. A null source yields the target's
// zero, matching Convert.To*((object)null). The caller's emit applies the final
// width/signedness cast (e.g. (uint32_t) for ToUInt32).
int64_t dn2cpp_convert_obj_to_i64(Dn2CppObject* v)
{
    return v != nullptr ? dn2cpp_box_read_i64(v) : 0;
}

double dn2cpp_convert_obj_to_f64(Dn2CppObject* v)
{
    return v != nullptr ? dn2cpp_box_read_f64(v) : 0.0;
}

int32_t dn2cpp_convert_obj_to_bool(Dn2CppObject* v)
{
    if (v == nullptr)
        return 0;
    if (v->type == &dn2cpp_string_type)
        return dn2cpp_convert_to_bool(reinterpret_cast<Dn2CppString*>(v));
    return dn2cpp_box_read_f64(v) != 0.0 ? 1 : 0;
}

int32_t dn2cpp_convert_obj_to_char(Dn2CppObject* v)
{
    // Char rides the I4 stack slot; the boxed source's low 16 bits are the code unit
    // (matches Convert.ChangeType(.., typeof(char)) for a boxed char/integer source).
    return v != nullptr ? static_cast<int32_t>(static_cast<uint16_t>(dn2cpp_box_read_i64(v))) : 0;
}

// Convert.ToString(object[, IFormatProvider]): the boxed value's virtual ToString,
// through the shared dn2cpp_object_tostring dispatch (tostring slot for an overridden
// body, the primitive formatters for a boxed primitive). A null source yields ""
// (string.Empty — real .NET's Convert.ToString((object)null); the (string)null
// identity overload stays null and never comes here), which is exactly what the
// shared dispatch answers for null.
Dn2CppString* dn2cpp_convert_obj_to_string(Dn2CppObject* v)
{
    return dn2cpp_object_tostring(v);
}

// Convert.ToDecimal(object): boxed IConvertible dispatch (null -> 0m).
Dn2CppDecimal dn2cpp_convert_obj_to_decimal(Dn2CppObject* v)
{
    if (v == nullptr)
        return dn2cpp_decimal_from_i4(0);
    if (v->type == &dn2cpp_decimal_type)
        return *static_cast<const Dn2CppDecimal*>(dn2cpp_box_payload(v));
    if (v->type == &dn2cpp_string_type)
        return dn2cpp_convert_str_to_decimal(reinterpret_cast<Dn2CppString*>(v));
    if (v->type == &dn2cpp_single_type || v->type == &dn2cpp_double_type)
        return dn2cpp_decimal_from_double(dn2cpp_box_read_f64(v));
    if (v->type == &dn2cpp_uint64_type)
        return dn2cpp_decimal_from_u8(*static_cast<const uint64_t*>(dn2cpp_box_payload(v)));
    return dn2cpp_decimal_from_i8(dn2cpp_box_read_i64(v));
}

// Convert.ToDateTime(object): boxed DateTime passes through, a string parses,
// null -> DateTime.MinValue; any other source is invalid (like the BCL's
// InvalidCastException).
Dn2CppDateTime dn2cpp_convert_obj_to_datetime(Dn2CppObject* v)
{
    if (v == nullptr)
        return dn2cpp_datetime_pack(0, 0);
    if (v->type == &dn2cpp_datetime_type)
        return *static_cast<const Dn2CppDateTime*>(dn2cpp_box_payload(v));
    if (v->type == &dn2cpp_string_type)
        return dn2cpp_convert_str_to_datetime(reinterpret_cast<Dn2CppString*>(v));
    dn2cpp_throw_invalid_operation();
}

// ---- Convert radix overloads ----

// Format the unsigned bit pattern `u` in base `base` (2/8/16), lowercase digits.
template <typename U>
static Dn2CppString* dn2cpp_format_unsigned_base(U u, int32_t base)
{
    if (u == 0)
    {
        char16_t* zbuf;
        Dn2CppString* z = dn2cpp_string_alloc(&zbuf, 1);
        zbuf[0] = u'0';
        return z;
    }
    const char16_t* digits = u"0123456789abcdef";
    char16_t tmp[64];
    int32_t n = 0;
    U b = static_cast<U>(base);
    while (u != 0)
    {
        tmp[n++] = digits[static_cast<int32_t>(u % b)];
        u /= b;
    }
    char16_t* buf;
    Dn2CppString* r = dn2cpp_string_alloc(&buf, n);
    for (int32_t i = 0; i < n; i++)
        buf[i] = tmp[n - 1 - i];
    return r;
}

Dn2CppString* dn2cpp_convert_to_string_base_i32(int32_t value, int32_t toBase)
{
    if (toBase == 10)
        return dn2cpp_int_to_string(value);
    if (toBase != 2 && toBase != 8 && toBase != 16)
        dn2cpp_throw_argument();
    return dn2cpp_format_unsigned_base<uint32_t>(static_cast<uint32_t>(value), toBase);
}

Dn2CppString* dn2cpp_convert_to_string_base_i64(int64_t value, int32_t toBase)
{
    if (toBase == 10)
        return dn2cpp_long_to_string(value);
    if (toBase != 2 && toBase != 8 && toBase != 16)
        dn2cpp_throw_argument();
    return dn2cpp_format_unsigned_base<uint64_t>(static_cast<uint64_t>(value), toBase);
}

// The ParseNumbers.StringToInt/StringToLong model backing every
// Convert.To*(string, fromBase) overload (the BCL parses these "tight": no
// whitespace tolerated). Rules, matched to real .NET:
//  - the base must be 2/8/10/16, validated first (a catchable
//    ArgumentException even for a null string);
//  - a null string is 0; an empty one a catchable
//    ArgumentOutOfRangeException;
//  - a leading '+' is always allowed; '-' only in base 10 (else
//    ArgumentException), and only for a signed target — an unsigned target
//    overflows on any '-', even "-0";
//  - base 16 accepts an "0x"/"0X" prefix;
//  - non-decimal bases parse the raw bit pattern bounded by the target width
//    ("FF" -> byte 255 / sbyte -1, sign-extended below); base 10 range-checks
//    against the target's signed/unsigned range;
//  - a non-digit (or sign/prefix with nothing after it) is a catchable
//    FormatException, out-of-range accumulation a catchable
//    OverflowException.
int64_t dn2cpp_convert_from_base_any(Dn2CppString* s, int32_t fromBase, int32_t bits,
                                     int32_t isUnsigned)
{
    if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
        dn2cpp_throw_argument();
    if (s == nullptr)
        return 0;
    if (s->length == 0)
        dn2cpp_throw_argument_out_of_range();
    int32_t i = 0;
    int neg = 0;
    if (s->chars[0] == u'+')
    {
        i++;
    }
    else if (s->chars[0] == u'-')
    {
        if (fromBase != 10)
            dn2cpp_throw_argument();
        if (isUnsigned)
            dn2cpp_overflow();
        neg = 1;
        i++;
    }
    if (fromBase == 16 && i + 1 < s->length && s->chars[i] == u'0'
        && (s->chars[i + 1] == u'x' || s->chars[i + 1] == u'X'))
        i += 2;
    if (i >= s->length)
        dn2cpp_throw_format();
    uint64_t limit;
    if (fromBase == 10 && !isUnsigned)
        limit = (bits == 64 ? (1ull << 63) : (1ull << (bits - 1))) - (neg ? 0 : 1);
    else
        limit = bits == 64 ? ~0ull : ((1ull << bits) - 1);
    uint64_t b = static_cast<uint64_t>(fromBase);
    uint64_t acc = 0;
    for (; i < s->length; i++)
    {
        char16_t c = s->chars[i];
        uint32_t d;
        if (c >= u'0' && c <= u'9')
            d = static_cast<uint32_t>(c - u'0');
        else if (c >= u'a' && c <= u'f')
            d = static_cast<uint32_t>(10 + (c - u'a'));
        else if (c >= u'A' && c <= u'F')
            d = static_cast<uint32_t>(10 + (c - u'A'));
        else
            dn2cpp_throw_format();
        if (d >= static_cast<uint32_t>(fromBase))
            dn2cpp_throw_format();
        if (acc > (limit - d) / b)
            dn2cpp_overflow();
        acc = acc * b + d;
    }
    if (neg)
        return static_cast<int64_t>(0 - acc); // unsigned negation: INT64_MIN-safe
    if (!isUnsigned && fromBase != 10 && bits < 64 && (acc & (1ull << (bits - 1))) != 0)
        return static_cast<int64_t>(acc | ~((1ull << bits) - 1)); // sign-extend the bit pattern
    return static_cast<int64_t>(acc);
}

int32_t dn2cpp_convert_from_base_i32(Dn2CppString* s, int32_t fromBase)
{
    return static_cast<int32_t>(dn2cpp_convert_from_base_any(s, fromBase, 32, 0));
}

int64_t dn2cpp_convert_from_base_i64(Dn2CppString* s, int32_t fromBase)
{
    return dn2cpp_convert_from_base_any(s, fromBase, 64, 0);
}

// ---- Convert.To/FromBase64String ----

static int32_t dn2cpp_base64_decode_digit(char16_t c)
{
    // O(1) ASCII lookup in place of the range-compare chain. The 128-entry table
    // maps each base64 alphabet char to its 6-bit value and everything else
    // (including code units >= 0x80) to -1; it is built once from the encode
    // alphabet on first use.
    static const std::array<int8_t, 128> table = [] {
        std::array<int8_t, 128> t{};
        t.fill(-1);
        const char* alpha =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        for (int8_t i = 0; alpha[i] != '\0'; i++)
            t[static_cast<unsigned char>(alpha[i])] = i;
        return t;
    }();
    return c < 128 ? table[c] : -1;
}

static bool dn2cpp_base64_is_ws(char16_t c)
{
    return c == u' ' || c == u'\t' || c == u'\n' || c == u'\r';
}

// Base64FormattingOptions: only None (0) and InsertLineBreaks (1) exist; anything
// else is a catchable ArgumentException like the BCL.
static void dn2cpp_base64_check_options(int32_t options)
{
    if (options != 0 && options != 1)
        dn2cpp_throw_argument();
}

// Encoded char count: 4 chars per 3-byte group, plus a CRLF after every full
// 76-char line except a trailing one (57 input bytes encode to exactly 76 chars
// with no break; 58 bytes get one CRLF before the final group).
static int32_t dn2cpp_base64_encoded_len(int32_t n, bool breaks)
{
    int64_t len = (static_cast<int64_t>(n) + 2) / 3 * 4;
    if (breaks && len > 0)
        len += (len - 1) / 76 * 2;
    return static_cast<int32_t>(len);
}

// Encode n bytes (strided source, so a non-packed byte[] still reads right) into
// `out`; returns the chars written (== dn2cpp_base64_encoded_len(n, breaks)).
static int32_t dn2cpp_base64_encode_core(const char* base, size_t stride, int32_t n,
                                         char16_t* out, bool breaks)
{
    static const char16_t* tbl =
        u"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    int32_t w = 0, lineChars = 0;
    for (int32_t i = 0; i < n; i += 3)
    {
        if (breaks && lineChars == 76)
        {
            out[w++] = u'\r';
            out[w++] = u'\n';
            lineChars = 0;
        }
        uint32_t b0 = static_cast<uint8_t>(base[static_cast<size_t>(i) * stride]);
        uint32_t b1 = (i + 1 < n) ? static_cast<uint8_t>(base[static_cast<size_t>(i + 1) * stride]) : 0u;
        uint32_t b2 = (i + 2 < n) ? static_cast<uint8_t>(base[static_cast<size_t>(i + 2) * stride]) : 0u;
        uint32_t triple = (b0 << 16) | (b1 << 8) | b2;
        out[w++] = tbl[(triple >> 18) & 0x3Fu];
        out[w++] = tbl[(triple >> 12) & 0x3Fu];
        out[w++] = (i + 1 < n) ? tbl[(triple >> 6) & 0x3Fu] : u'=';
        out[w++] = (i + 2 < n) ? tbl[triple & 0x3Fu] : u'=';
        lineChars += 4;
    }
    return w;
}

// Encode a validated slice of a byte[] into a fresh string.
static Dn2CppString* dn2cpp_base64_encode_alloc(const char* base, size_t stride, int32_t n,
                                                int32_t options)
{
    bool breaks = (options & 1) != 0;
    char16_t* buf;
    Dn2CppString* r = dn2cpp_string_alloc(&buf, dn2cpp_base64_encoded_len(n, breaks));
    dn2cpp_base64_encode_core(base, stride, n, buf, breaks);
    return r;
}

Dn2CppString* dn2cpp_convert_to_base64(Dn2CppArrayN* inArray, int32_t options)
{
    if (inArray == nullptr)
        dn2cpp_throw_argument_null();
    dn2cpp_base64_check_options(options);
    return dn2cpp_base64_encode_alloc(inArray->data, static_cast<size_t>(inArray->elemSize),
        inArray->length, options);
}

// ToBase64String(byte[], int offset, int length[, options]) — catchable
// ArgumentNull / ArgumentOutOfRange validation like the BCL.
Dn2CppString* dn2cpp_convert_to_base64_offset(Dn2CppArrayN* inArray, int32_t offset,
                                              int32_t length, int32_t options)
{
    if (inArray == nullptr)
        dn2cpp_throw_argument_null();
    dn2cpp_base64_check_options(options);
    if (offset < 0 || length < 0 || offset > inArray->length - length)
        dn2cpp_throw_argument_out_of_range();
    size_t stride = static_cast<size_t>(inArray->elemSize);
    return dn2cpp_base64_encode_alloc(inArray->data + static_cast<size_t>(offset) * stride,
        stride, length, options);
}

// ToBase64String(ReadOnlySpan<byte>[, options]) — contiguous-bytes form.
Dn2CppString* dn2cpp_convert_to_base64_raw(const uint8_t* data, int32_t n, int32_t options)
{
    dn2cpp_base64_check_options(options);
    return dn2cpp_base64_encode_alloc(reinterpret_cast<const char*>(data), 1, n, options);
}

// ToBase64CharArray(byte[], int, int, char[], int[, options]) — returns the
// chars written. The destination check needs the encoded length, so it runs
// after the source-slice check (same order as the BCL).
int32_t dn2cpp_convert_to_base64_chararray(Dn2CppArrayN* inArray, int32_t offsetIn,
                                           int32_t length, Dn2CppArrayN* outArray,
                                           int32_t offsetOut, int32_t options)
{
    if (inArray == nullptr || outArray == nullptr)
        dn2cpp_throw_argument_null();
    dn2cpp_base64_check_options(options);
    if (offsetIn < 0 || length < 0 || offsetOut < 0
        || offsetIn > inArray->length - length)
        dn2cpp_throw_argument_out_of_range();
    bool breaks = (options & 1) != 0;
    int32_t need = dn2cpp_base64_encoded_len(length, breaks);
    if (offsetOut > outArray->length - need)
        dn2cpp_throw_argument_out_of_range();
    size_t inStride = static_cast<size_t>(inArray->elemSize);
    const char* src = inArray->data + static_cast<size_t>(offsetIn) * inStride;
    // char[] is packed UTF-16 (elemSize 2): encode straight into the slot.
    return dn2cpp_base64_encode_core(src, inStride, length,
        reinterpret_cast<char16_t*>(outArray->data) + offsetOut, breaks);
}

// TryToBase64Chars(ReadOnlySpan<byte>, Span<char>, out int[, options]): a
// too-short destination reports false with 0 written and the buffer untouched.
int32_t dn2cpp_convert_try_to_base64(const uint8_t* data, int32_t n, char16_t* dest,
                                     int32_t destLen, int32_t options, int32_t* written)
{
    dn2cpp_base64_check_options(options);
    bool breaks = (options & 1) != 0;
    if (dn2cpp_base64_encoded_len(n, breaks) > destLen)
    {
        *written = 0;
        return 0;
    }
    *written = dn2cpp_base64_encode_core(reinterpret_cast<const char*>(data), 1, n, dest, breaks);
    return 1;
}

// Decode core, streaming 4-unit quanta (whitespace skipped anywhere; '=' only
// in the final quantum's last two slots; a completed padded quantum must end the
// data). Each complete quantum's bytes are written atomically, so on a
// too-short destination the earlier quanta are already in the buffer but
// *written reports 0 — exactly the BCL's Try* semantics. `dest == nullptr`
// counts without writing (the sizing pass for the throwing forms; destLen is
// ignored). Returns 1 ok / 0 invalid-or-short.
static int32_t dn2cpp_base64_decode_core(const char16_t* p, int32_t n, uint8_t* dest,
                                         int32_t destLen, int32_t* written)
{
    int32_t w = 0;
    uint32_t acc = 0;
    int32_t dataInQ = 0, padsInQ = 0;
    bool done = false; // a padded quantum completed; only whitespace may follow
    for (int32_t i = 0; i < n; i++)
    {
        char16_t c = p[i];
        if (dn2cpp_base64_is_ws(c))
            continue;
        if (done)
            goto fail;
        if (c == u'=')
        {
            // Padding is only valid in the last two slots of a quantum.
            padsInQ++;
            if (padsInQ > 2 || dataInQ < 2)
                goto fail;
        }
        else
        {
            int32_t d = dn2cpp_base64_decode_digit(c);
            if (d < 0 || padsInQ > 0)
                goto fail;
            acc = (acc << 6) | static_cast<uint32_t>(d);
            dataInQ++;
        }
        if (dataInQ + padsInQ == 4)
        {
            int32_t bytes = 3 - padsInQ;
            uint32_t v = acc << (6 * padsInQ);
            if (dest != nullptr)
            {
                if (w + bytes > destLen)
                    goto fail;
                dest[w] = static_cast<uint8_t>(v >> 16);
                if (bytes > 1)
                    dest[w + 1] = static_cast<uint8_t>(v >> 8);
                if (bytes > 2)
                    dest[w + 2] = static_cast<uint8_t>(v);
            }
            w += bytes;
            if (padsInQ > 0)
                done = true;
            acc = 0;
            dataInQ = 0;
            padsInQ = 0;
        }
    }
    if (dataInQ + padsInQ != 0)
        goto fail; // trailing partial quantum
    *written = w;
    return 1;
fail:
    *written = 0;
    return 0;
}

// Validate + size + decode into a fresh packed byte[]; FormatException on
// invalid input (the throwing FromBase64* forms).
static Dn2CppArrayN* dn2cpp_base64_decode_alloc(const char16_t* p, int32_t n,
                                                const Dn2CppTypeInfo* ti)
{
    int32_t outLen;
    if (!dn2cpp_base64_decode_core(p, n, nullptr, 0, &outLen))
        dn2cpp_throw_format();
    Dn2CppArrayN* out = dn2cpp_newarr_n_t(outLen, static_cast<int32_t>(sizeof(uint8_t)), ti);
    dn2cpp_base64_decode_core(p, n, reinterpret_cast<uint8_t*>(out->data), outLen, &outLen);
    return out;
}

Dn2CppArrayN* dn2cpp_convert_from_base64(Dn2CppString* s, const Dn2CppTypeInfo* ti)
{
    if (s == nullptr)
        dn2cpp_throw_argument_null();
    return dn2cpp_base64_decode_alloc(s->chars, s->length, ti);
}

// FromBase64CharArray(char[], int offset, int length) — catchable ArgumentNull /
// ArgumentOutOfRange validation, then the same decode as FromBase64String.
Dn2CppArrayN* dn2cpp_convert_from_base64_chararray(Dn2CppArrayN* inArray, int32_t offset,
                                                   int32_t length, const Dn2CppTypeInfo* ti)
{
    if (inArray == nullptr)
        dn2cpp_throw_argument_null();
    if (offset < 0 || length < 0 || offset > inArray->length - length)
        dn2cpp_throw_argument_out_of_range();
    // char[] is packed UTF-16 (elemSize 2).
    return dn2cpp_base64_decode_alloc(
        reinterpret_cast<const char16_t*>(inArray->data) + offset, length, ti);
}

int32_t dn2cpp_convert_try_from_base64(const char16_t* p, int32_t n, uint8_t* dest,
                                       int32_t destLen, int32_t* written)
{
    return dn2cpp_base64_decode_core(p, n, dest, destLen, written);
}

int32_t dn2cpp_convert_try_from_base64_str(Dn2CppString* s, uint8_t* dest, int32_t destLen,
                                           int32_t* written)
{
    if (s == nullptr)
        dn2cpp_throw_argument_null();
    return dn2cpp_base64_decode_core(s->chars, s->length, dest, destLen, written);
}

// Contiguous-bytes core (the ReadOnlySpan<byte> overload lowers here directly).
Dn2CppString* dn2cpp_convert_to_hex_raw(const uint8_t* data, int32_t n, bool lower)
{
    const char16_t* tbl = lower ? u"0123456789abcdef" : u"0123456789ABCDEF";
    char16_t* buf;
    Dn2CppString* r = dn2cpp_string_alloc(&buf, n * 2);
    for (int32_t i = 0; i < n; i++)
    {
        uint8_t b = data[i];
        buf[i * 2] = tbl[b >> 4];
        buf[i * 2 + 1] = tbl[b & 0x0F];
    }
    return r;
}

Dn2CppString* dn2cpp_convert_to_hex(Dn2CppArrayN* inArray, bool lower)
{
    if (inArray == nullptr)
        dn2cpp_throw_argument_null();
    size_t stride = static_cast<size_t>(inArray->elemSize);
    if (stride == 1)
        return dn2cpp_convert_to_hex_raw(reinterpret_cast<const uint8_t*>(inArray->data), inArray->length, lower);
    const char16_t* tbl = lower ? u"0123456789abcdef" : u"0123456789ABCDEF";
    int32_t n = inArray->length;
    const char* base = inArray->data;
    char16_t* buf;
    Dn2CppString* r = dn2cpp_string_alloc(&buf, n * 2);
    for (int32_t i = 0; i < n; i++)
    {
        uint8_t b = static_cast<uint8_t>(base[static_cast<size_t>(i) * stride]);
        buf[i * 2] = tbl[b >> 4];
        buf[i * 2 + 1] = tbl[b & 0x0F];
    }
    return r;
}

// ToHexString/ToHexStringLower(byte[], int, int) — validate like real .NET
// (null -> ArgumentNullException, negative/overrunning offset/count ->
// ArgumentOutOfRangeException, both catchable), then encode the slice.
Dn2CppString* dn2cpp_convert_to_hex_offset(Dn2CppArrayN* inArray, int32_t offset, int32_t count, bool lower)
{
    if (inArray == nullptr)
        dn2cpp_throw_argument_null();
    if (static_cast<uint32_t>(offset) > static_cast<uint32_t>(inArray->length)
        || static_cast<uint32_t>(count) > static_cast<uint32_t>(inArray->length - offset))
        dn2cpp_throw_argument_out_of_range();
    size_t stride = static_cast<size_t>(inArray->elemSize);
    if (stride == 1)
        return dn2cpp_convert_to_hex_raw(
            reinterpret_cast<const uint8_t*>(inArray->data) + offset, count, lower);
    const char16_t* tbl = lower ? u"0123456789abcdef" : u"0123456789ABCDEF";
    const char* base = inArray->data + static_cast<size_t>(offset) * stride;
    char16_t* buf;
    Dn2CppString* r = dn2cpp_string_alloc(&buf, count * 2);
    for (int32_t i = 0; i < count; i++)
    {
        uint8_t b = static_cast<uint8_t>(base[static_cast<size_t>(i) * stride]);
        buf[i * 2] = tbl[b >> 4];
        buf[i * 2 + 1] = tbl[b & 0x0F];
    }
    return r;
}

static int32_t dn2cpp_hex_decode_digit(char16_t c)
{
    if (c >= u'0' && c <= u'9')
        return c - u'0';
    if (c >= u'A' && c <= u'F')
        return c - u'A' + 10;
    if (c >= u'a' && c <= u'f')
        return c - u'a' + 10;
    return -1;
}

Dn2CppArrayN* dn2cpp_convert_from_hex(Dn2CppString* s, const Dn2CppTypeInfo* ti)
{
    if (s == nullptr)
        dn2cpp_throw_argument_null();
    if (s->length % 2 != 0)
        dn2cpp_throw_format();
    int32_t outLen = s->length / 2;
    Dn2CppArrayN* out = dn2cpp_newarr_n_t(outLen, static_cast<int32_t>(sizeof(uint8_t)), ti);
    char* od = out->data;
    size_t stride = static_cast<size_t>(out->elemSize);
    for (int32_t i = 0; i < outLen; i++)
    {
        int32_t hi = dn2cpp_hex_decode_digit(s->chars[i * 2]);
        int32_t lo = dn2cpp_hex_decode_digit(s->chars[i * 2 + 1]);
        if (hi < 0 || lo < 0)
            dn2cpp_throw_format();
        *reinterpret_cast<uint8_t*>(od + static_cast<size_t>(i) * stride) =
            static_cast<uint8_t>((hi << 4) | lo);
    }
    return out;
}

// FromHexString(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsConsumed,
// out int bytesWritten) -> OperationStatus. Replicates the BCL's outer sizing decision
// (DestinationTooSmall when fewer bytes than whole pairs, NeedMoreData on an odd trailing
// nibble) and HexConverter.TryDecodeFromUtf16's inner loop — including the quirk that a
// bad LOW nibble advances charsConsumed past the already-valid HIGH nibble, so an invalid
// pair whose low char is bad reports an odd charsConsumed. An invalid char anywhere in the
// decoded region overrides the sizing verdict with InvalidData, matching .NET.
int32_t dn2cpp_convert_from_hex_span(const char16_t* chars, int32_t charsLen,
                                     uint8_t* bytes, int32_t bytesLen,
                                     int32_t* charsConsumed, int32_t* bytesWritten)
{
    int32_t quotient = charsLen / 2;
    int32_t remainder = charsLen % 2;
    int32_t destPairs;
    int32_t status; // 0 Done, 1 DestinationTooSmall, 2 NeedMoreData, 3 InvalidData
    if (bytesLen < quotient) { destPairs = bytesLen; status = 1; }
    else if (remainder != 0) { destPairs = quotient; status = 2; }
    else                     { destPairs = quotient; status = 0; }

    int32_t i = 0, j = 0, byteHi = 0, byteLo = 0;
    while (j < destPairs)
    {
        int32_t dh = dn2cpp_hex_decode_digit(chars[i]);
        int32_t dl = dn2cpp_hex_decode_digit(chars[i + 1]);
        byteHi = dh < 0 ? 0xFF : dh;
        byteLo = dl < 0 ? 0xFF : dl;
        if ((byteHi | byteLo) == 0xFF)
            break;
        bytes[j++] = static_cast<uint8_t>((byteHi << 4) | byteLo);
        i += 2;
    }
    if (byteLo == 0xFF)
        i++;
    if (j < destPairs) // the loop broke early on an invalid char
    {
        status = 3;
        *charsConsumed = i;
        *bytesWritten = i / 2;
    }
    else
    {
        *charsConsumed = i;
        *bytesWritten = j;
    }
    return status;
}
