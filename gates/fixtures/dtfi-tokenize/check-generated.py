"""Whole refusal body is inert argument moves, ordered void uses, throw, unreachable bool."""
import argparse,re
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('output',type=Path);p.add_argument('--mode',choices=['all','reflection','minimal'],default='all');a=p.parse_args()
paths=list(a.output.glob('generated*.cpp'))+[a.output/'generated.h']
text='\n'.join(p.read_text() for p in paths if p.exists())
def definitions(name):
    result=[]
    signature=r't_System_Globalization_DateTimeFormatInfo\* a0, int32_t a1, int32_t\* a2, int32_t\* a3, t_System___DTString\* a4'
    for m in re.finditer(r'^(?:inline )?(?:uint8_t|int32_t|bool) '+name+r'_m\d+\('+signature+r'\)\n\{',text,re.M):
        end=m.end();start=end;depth=1
        while depth:
            depth+=(text[end]=='{')-(text[end]=='}');end+=1
        result.append(text[start:end-1])
    return result
name='Program_Direct' if a.mode=='minimal' else 'DateTimeFormatInfo_Tokenize'
bodies=definitions(name);assert len(bodies)==1,(name,len(bodies))
throw='dn2cpp_throw_platform_not_supported("DateTimeFormatInfo token-table parsing is not supported");'
def check_body(body, virtual=False):
    lines=[line.strip() for line in body.splitlines() if line.strip()]
    assert lines.count(throw)==1
    values={f'a{i}': f'a{i}' for i in range(5)}
    declared=set();uses=[];thrown=False;returned=False;receiver=False;nullchecks=0
    for line in lines:
        if line==throw:
            assert uses==['a1','a2','a3','a4'] and receiver
            assert nullchecks==int(virtual)
            thrown=True;continue
        m=re.fullmatch(r'\[\[maybe_unused\]\] [\w *]+ (\w+);',line)
        if m:
            assert not thrown and m[1] not in declared
            declared.add(m[1]);continue
        m=re.fullmatch(r'(\w+) = (a[0-4]|t\d+|0);',line)
        if m:
            assert m[1] in declared
            value='0' if m[2]=='0' else values[m[2]]
            assert (value=='0')==thrown
            values[m[1]]=value
            receiver |= value=='a0'
            continue
        m=re.fullmatch(r'\(void\)\((a[0-4]|t\d+)\);',line)
        if m:
            assert not thrown and not nullchecks
            uses.append(values[m[1]]);continue
        m=re.fullmatch(r'dn2cpp_null_check\((t\d+|a0)\);',line)
        if m:
            assert virtual and not thrown and values[m[1]]=='a0'
            assert uses==['a1','a2','a3','a4']
            nullchecks+=1;continue
        m=re.fullmatch(r'return (0|t\d+);',line)
        if m:
            assert thrown and (m[1]=='0' or values[m[1]]=='0') and not returned
            returned=True;continue
        raise AssertionError('unexpected payload access/control flow: '+line)
    assert uses==['a1','a2','a3','a4'] and returned
check_body(bodies[0])
if a.mode=='all':
    virtual=definitions('Program_Virtual');assert len(virtual)==1
    check_body(virtual[0],virtual=True)
if a.mode in ('minimal','reflection'):
    assert not re.search(r'Program_(Pointer|Address|DelegateRoute|Producer|VirtualProducer)_m\d+',text)
if a.mode=='minimal':assert not re.search(r'Program_(Virtual|Metadata|RunReflectionChecks)_m\d+',text)
if a.mode=='reflection':assert not re.search(r'Program_(Direct|Virtual)_m\d+',text)
print('dtfi-tokenize-generated:payload-free-unconditional-refusal')
