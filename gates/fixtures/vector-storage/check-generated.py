"""Verify the alignment boundary even on CPUs that tolerate unaligned loads."""
from pathlib import Path
import re
import sys

text = '\n'.join(path.read_text() for path in Path(sys.argv[1]).glob('generated*.cpp'))
functions = re.findall(r'^([^\n;{}]+\b(\w+)\([^\n]*\))\n\{(.*?)^\}', text, re.M | re.S)
unboxes = 0
indirect = False
mutation = False
for declaration, name, body in functions:
    if 'StorageUnbox_' in name and 'dn2cpp_unbox' in body:
        assert 'std::memcpy' in body, ('box payload must use an unaligned copy', name)
        assert not re.search(r'\*\(\([^\n]*\*\)dn2cpp_unbox', body), name
        unboxes += 1
    if 'StorageIndirect_' in name:
        assert body.count('std::memcpy') >= 3, 'indirect store/load/init must copy bytes'
        indirect = True
    if 'BoxedStorage_Change_' in name:
        assert not re.search(r'->f_(?:Value|Marker|Prefix|Tag)\b', body), 'boxed storage must not form aligned field lvalues'
        for field in ('Value', 'Marker', 'Prefix', 'Tag'):
            assert re.search(r'offsetof\([^,]+, f_' + field + r'\)', body), field
        mutation = True
assert unboxes >= 3 and indirect and mutation, (unboxes, indirect, mutation)
print('vector storage generated copies verified')
