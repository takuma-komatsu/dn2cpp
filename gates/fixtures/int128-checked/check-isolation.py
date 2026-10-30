#!/usr/bin/env python3
"""Confirm isolation in emitted bodies, rather than trusting constant branch folding."""
import pathlib
import re
import sys
root, route = pathlib.Path(sys.argv[1]), sys.argv[2]
text = '\n'.join(p.read_text() for p in sorted(root.glob('generated*.*')))
found = set(re.findall(r'\bProgram_(Direct|Constrained|ViaConstraint|Address|Pointer|Route)_\w*\([^;{}]*\)\s*\{', text))
allowed = {'DIRECT': {'Direct'}, 'CONSTRAINED': {'Constrained', 'ViaConstraint'},
           'ADDRESS': {'Address'}, 'CALLI': {'Pointer'}, 'DISCARD': set()}
if route not in allowed or found != allowed[route]:
    raise SystemExit(f'wrong isolated bodies for {route}: {sorted(found)}')
if route == 'DISCARD' and ('struct t_System_Int128' not in text or '.f__lower' not in text or '.f__upper' not in text):
    raise SystemExit('discard has no independent named Int128 value layout')
print('int128-checked-isolation:' + route + ':' + ','.join(sorted(found)))
