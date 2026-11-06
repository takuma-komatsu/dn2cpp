#!/usr/bin/env python3
"""Prevent the tiny runtime driver's shared helper subset drifting from VectorProbe."""
from pathlib import Path
root = Path(__file__).resolve().parents[3]
source = (root / 'samples/dotnet/VectorProbe/UnsupportedValueSemantics.cs').read_text()
helper = Path(__file__).with_name('Helpers.cs').read_text()
for begin, end in (
    ('internal enum VectorUnsupportedEnum', '// Inline value storage'),
    ('    private static string VectorUnsupportedEffects', '    private static Vector<T> VectorUnsupportedOperand'),
    ('    private static object VectorUnsupportedObjectOperand', '    private static void VectorUnsupportedTest<T>'),
):
    assert source[source.index(begin):source.index(end)] in helper, ('runtime helper drift', begin)
project = Path(__file__).with_name('FixedUnsupportedRuntime.csproj').read_text()
assert 'Include="../../../samples/dotnet/VectorProbe/FixedUnsupportedValueSemantics.cs"' in project
print('focused runtime links exact sample and preserves its shared helper subset')
