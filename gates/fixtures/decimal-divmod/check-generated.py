"""Assert the direct/address/retained routes and attach a callable ABI witness."""
from pathlib import Path
import re
import sys

out = Path(sys.argv[1])
text = '\n'.join(p.read_text() for p in sorted(out.glob('generated*.cpp')))
assert 'dn2cpp_decimal_divmod1e9(' in text
assert not re.search(r'(?:AsMutable|DecCalc)_DecDivMod1E9', text)
header = (out / 'generated.h').read_text()
text += '\n' + header
retained = sorted(set(re.findall(r'uint32_t (DecimalDivModSubset_RetainedOnly_\w+)\(Dn2CppDecimal\* a0\)', header)))
assert len(retained) == 1, retained
symbol = retained[0]
assert re.search(r'uint32_t ' + symbol + r'\([^)]*\)\s*\{[^}]*dn2cpp_decimal_divmod1e9\(', text)
source = (Path(__file__).parent / 'native-storage.cpp').read_text()
source += '''
#include "generated.h"
static int check_retained_decimal_divmod()
{
    static_assert(std::is_same<decltype(&SYMBOL), uint32_t (*)(Dn2CppDecimal*)>::value);
    Dn2CppDecimal value;
    value._flags = 0x801c0000u; value._hi32 = 0xffffffffu; value._lo64 = 0xffffffffffffffffull;
    uint32_t remainder = SYMBOL(&value);
    if (remainder != 543950335u || value._flags != 0x801c0000u || value._hi32 != 4u
        || value._lo64 != ((uint64_t(1266874889u) << 32) | 3047500985u)) std::abort();
    std::fputs("decimal divmod retained ABI passed\\n", stderr);
    return 0;
}
static const int decimal_retained_checked = check_retained_decimal_divmod();
'''.replace('SYMBOL', symbol)
(out / 'generated_decimal_storage_probe.cpp').write_text(source)
print('decimal divmod retained callable: ' + symbol)
