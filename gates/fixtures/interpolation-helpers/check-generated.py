#!/usr/bin/env python3
"""Assert selected helpers remain unique real bodies on an opaque intrinsic owner."""
import argparse
from pathlib import Path
import re
p=argparse.ArgumentParser()
p.add_argument('output', type=Path)
p.add_argument('--route', default='all')
a=p.parse_args()
s='\n'.join(x.read_text() for x in sorted(a.output.glob('generated*.cpp')))
h=(a.output/'generated.h').read_text()
s+='\n'+h
for name in ['HasCustomFormatter','GetDefaultLength']:
    if a.route == 'public' and name == 'GetDefaultLength':
        continue
    matches=list(re.finditer(r'^(?:inline )?int32_t DefaultInterpolatedStringHandler_'+name+r'_m\d+\([^\n]*\)\n\{',s,re.M))
    assert len(matches)==1,(name,'unique definition',len(matches))
    begin=end=matches[0].end();depth=1
    while depth:
        depth+=(s[end]=='{')-(s[end]=='}');end+=1
    body=s[begin:end-1]
    assert 'throw new' not in body and 'not_supported' not in body
    if name=='HasCustomFormatter':
        assert 'dn2cpp_null_check' in body
        assert 'dn2cpp_format_provider_get_format' in body
        assert 'dn2cpp_cultureinfo_type' in body and 'ICustomFormatter' in body
        assert 'dn2cpp_isinst' not in body
    else:
        assert '11' in body and '256' in body and 'a0' in body and 'a1' in body
    print('interpolation-helper-real-il:'+name)
assert not re.search(r'DefaultInterpolatedStringHandler_(?:AppendCustomFormatter|Grow|Clear|GetSpan)_m\d+\(',s)
if a.route in ['reflection','retained','public']:
    assert not re.search(r'Program_(?:Direct|DirectLength|Address|AddressLength)_m\d+\(',h), 'alternate roots retained'
if a.route=='address':
    assert not re.search(r'Program_(?:Direct|DirectLength|Reflected|ReflectedLength)_m\d+\(',h), 'address route not isolated'
if a.route=='direct':
    assert not re.search(r'Program_(?:Address|AddressLength|Reflected|ReflectedLength)_m\d+\(',h), 'direct route not isolated'
if a.route in ['direct','address','reflection']:
    assert 'Provider_GetFormat_' in s, 'helper-only provider callback lost'
    assert not re.search(r'(?:Program___GateEntry|TryWriteInterpolatedStringHandler_AppendFormatted)_.*\(',h), 'numeric-format-independent closure lost'

if a.route in ['reflection','retained','all']:
    identity=re.search(r'const Dn2CppTypeInfo ti_System_Runtime_CompilerServices_DefaultInterpolatedStringHandler = ([^\n]+)',s)
    assert identity and 'DN2CPP_TF_BYREFLIKE' in identity.group(1), 'actual byref-like flag lost'
    assert re.search(r'interpolationhelpertab_System_Runtime_CompilerServices_DefaultInterpolatedStringHandler, 2LL',s), 'exact helper rows missing'
