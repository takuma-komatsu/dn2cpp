#include "dn2cpp_core.h"
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <type_traits>

static_assert(std::is_same<decltype(&dn2cpp_decimal_divmod1e9), uint32_t (*)(Dn2CppDecimal*)>::value);
struct GuardedDecimal {
    uint64_t before;
    Dn2CppDecimal neighbors[3];
    uint64_t after;
};

static int check_decimal_storage()
{
    const uint32_t flags[] = {0u, 0x80000000u, 0x001c0000u, 0x801c0000u, 0xffffffffu, 0x7f010001u, 0x001d0000u};
    for (uint32_t flag : flags) {
        GuardedDecimal storage;
        std::memset(&storage, 0xA5, sizeof(storage));
        auto* value = &storage.neighbors[1];
        value->_flags = flag; value->_hi32 = 0xffffffffu; value->_lo64 = 0xffffffffffffffffull;
        GuardedDecimal expected = storage;
        expected.neighbors[1]._hi32 = 4u;
        expected.neighbors[1]._lo64 = (uint64_t(1266874889u) << 32) | 3047500985u;
        auto* alias = value;
        uint32_t result = dn2cpp_decimal_divmod1e9(value);
        if (result != 543950335u || alias->_flags != flag || std::memcmp(&storage, &expected, sizeof(storage)) != 0)
            std::abort();
        value->_hi32 = 0; value->_lo64 = 0;
        expected = storage;
        if (dn2cpp_decimal_divmod1e9(value) != 0 || std::memcmp(&storage, &expected, sizeof(storage)) != 0)
            std::abort();
    }
    std::fputs("decimal divmod native storage/ABI/canaries passed\n", stderr);
    return 0;
}
static const int decimal_storage_checked = check_decimal_storage();
