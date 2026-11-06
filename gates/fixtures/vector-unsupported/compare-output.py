#!/usr/bin/env python3
"""Independent unsupported-value outcomes; never learn expectations from generated code."""
from pathlib import Path
import argparse
import hashlib
import json
import re

# Count must throw before any numeric lane access. Constructors only pin existing
# exception ordering; these expectations do not claim new constructor support.
cases = [
    ("default-init", "result=False", ""),
    ("IsSupported", "result=False", ""),
    ("Count", "NotSupportedException", ""),
    ("typed", "NotSupportedException", ""),
    ("hash", "NotSupportedException", ""),
    ("object-null", "result=False", ""),
    ("object-wrong", "result=False", ""),
    ("object-other-vector", "result=False", ""),
    ("object-same-vector", "NotSupportedException", ""),
    ("boxed-null", "result=False", ""),
    ("boxed-wrong", "result=False", ""),
    ("boxed-same", "NotSupportedException", ""),
    ("boxed-distinct-same-type", "NotSupportedException", ""),
    ("boxed-hash", "NotSupportedException", ""),
    ("interface", "NotSupportedException", ""),
    ("constrained", "NotSupportedException", ""),
    ("comparer-equal", "NotSupportedException", ""),
    ("comparer-hash", "NotSupportedException", ""),
    ("typed-address", "NotSupportedException", ""),
    ("object-address-null", "result=False", ""),
    ("object-address-same", "NotSupportedException", ""),
    ("hash-address", "NotSupportedException", ""),
    ("effects-typed", "NotSupportedException", "RA"),
    ("effects-typed-argument-throws", "InvalidOperationException", "RA"),
    ("effects-typed-receiver-throws", "InvalidOperationException", "R"),
    ("effects-object-null", "result=False", "RA"),
    ("effects-object-same", "NotSupportedException", "RA"),
    ("effects-object-argument-throws", "InvalidOperationException", "RA"),
    ("effects-hash", "NotSupportedException", "R"),
    ("array-null", "NullReferenceException", ""),
    ("array-empty", "NotSupportedException", ""),
    ("array-null-negative", "ArgumentOutOfRangeException", ""),
    ("array-null-zero", "NullReferenceException", ""),
    ("array-empty-negative", "ArgumentOutOfRangeException", ""),
    ("array-empty-zero", "NotSupportedException", ""),
    ("readonly-span-empty", "NotSupportedException", ""),
    ("span-empty", "NotSupportedException", ""),
    ("readonly-span-nonempty", "NotSupportedException", ""),
]
expected = ["-- vector unsupported semantics --"]
for element in ("decimal", "enum", "struct", "ref-containing-struct"):
    for name, outcome, effects in cases:
        if not outcome.startswith("result="):
            outcome = "exception=System." + outcome
        expected.append(f"{element}.{name}|{outcome}|effects={effects}")
for element in ("enum", "int", "uint"):
    for storage in ("direct", "nested", "base"):
        for name in ("supported", "count-positive", "typed", "hash-match", "object-null", "object-same"):
            if name == "object-null" or (element == "enum" and name == "supported"):
                outcome = "result=False"
            elif element == "enum":
                outcome = "exception=System.NotSupportedException"
            else:
                outcome = "result=True"
            expected.append(f"storage.{element}.{storage}.{name}|{outcome}|effects=")
for element in ("enum", "int"):
    expected.append(f"storage.scalar-{element}|result=True|effects=")
for element in ("enum", "int", "uint"):
    for route in ("value", "byref", "delegate"):
        for name in ("marker", "box-identity", "supported", "count-positive", "typed", "hash-match", "object-null", "object-same"):
            if name == "object-null" or (element == "enum" and name == "supported"):
                outcome = "result=False"
            elif element == "enum" and name not in ("marker", "box-identity"):
                outcome = "exception=System.NotSupportedException"
            else:
                outcome = "result=True"
            expected.append(f"abi.{element}.{route}.{name}|{outcome}|effects=")
for element in ("enum", "int"):
    expected.append(f"abi.scalar-{element}|result=True|effects=")
expected.extend([
    "abi.body-enum-supported|result=False|effects=",
    "abi.body-enum-count|exception=System.NotSupportedException|effects=",
    "abi.body-int-supported|result=True|effects=",
    "abi.body-int-count|result=True|effects=",
])
for element in ("int128", "uint128"):
    for name in ("Count", "typed", "object-null", "hash"):
        outcome = "result=False" if name == "object-null" else "exception=System.NotSupportedException"
        expected.append(f"{element}.{name}|{outcome}|effects=")
for element in ("decimal", "enum", "struct", "ref-containing-struct"):
    expected.append(f"{element}.interface-address|exception=System.NotSupportedException|effects=")
expected.append("vector unsupported end")
parser = argparse.ArgumentParser()
parser.add_argument('output', type=Path)
parser.add_argument('--backend', choices=('exact', 'hw0'), default='exact')
parser.add_argument('--oracle', type=Path)
parser.add_argument('--hardware-evidence', type=Path)
parser.add_argument('--difference-receipt', type=Path)
args = parser.parse_args()
actual = args.output.read_text().splitlines()

def require_equal(actual_lines, expected_lines):
    assert actual_lines == expected_lines, next(((i, a, b) for i, (a, b) in enumerate(zip(actual_lines, expected_lines)) if a != b), ("length", len(actual_lines), len(expected_lines)))

if args.oracle is not None:
    require_equal(args.oracle.read_text().splitlines(), expected)
if args.backend == 'exact':
    require_equal(actual, expected)
    print(f"vector unsupported independent outcomes passed: {len(expected) - 2} records")
else:
    assert args.oracle and args.hardware_evidence and args.difference_receipt, 'hw0 requires oracle, actual hardware evidence, and a difference receipt'
    evidence = args.hardware_evidence.read_text().splitlines()
    witnesses = [line for line in evidence if line.startswith('vector value actual tuple equality/hash passed ')]
    assert len(witnesses) == 1 and re.fullmatch(r'vector value actual tuple equality/hash passed hw=0 pointer=(4|8)', witnesses[0]), ('actual hw0 witness required', witnesses)
    numeric_ids = {
        'storage.int.direct.supported', 'storage.int.nested.supported', 'storage.int.base.supported',
        'storage.uint.direct.supported', 'storage.uint.nested.supported', 'storage.uint.base.supported',
        'abi.int.value.supported', 'abi.int.byref.supported', 'abi.int.delegate.supported',
        'abi.uint.value.supported', 'abi.uint.byref.supported', 'abi.uint.delegate.supported',
        'abi.body-int-supported',
    }
    backend_expected = []
    selected = set()
    for line in expected:
        identity = line.split('|', 1)[0]
        if identity in numeric_ids:
            assert line == identity + '|result=True|effects=', line
            backend_expected.append(identity + '|result=False|effects=')
            selected.add(identity)
        else:
            backend_expected.append(line)
    assert selected == numeric_ids and len(numeric_ids) == 13
    require_equal(actual, backend_expected)
    differences = [{'id': left.split('|', 1)[0], 'oracle': left, 'target': right}
                   for left, right in zip(expected, actual) if left != right]
    assert len(differences) == 13 and {x['id'] for x in differences} == numeric_ids
    assert len(expected) - 2 - len(differences) == 285
    assert not args.difference_receipt.exists(), ('refusing receipt overwrite', args.difference_receipt)
    def identity(path):
        return {'path': str(path.resolve()), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
    receipt = {'whole_output_parity': False, 'exact_parity_records': 285,
               'numeric_backend_specific_records': 13, 'backend': 'hw0',
               'oracle': identity(args.oracle), 'target': identity(args.output),
               'hardware_evidence': identity(args.hardware_evidence), 'hardware_witness': witnesses[0],
               'differences': differences}
    args.difference_receipt.write_text(json.dumps(receipt, indent=2) + '\n')
    print('vector unsupported: 285 exact parity records; 13 explicit numeric IsSupported hw0 differences verified (not 298-record parity)')
