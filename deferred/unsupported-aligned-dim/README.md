# Deferred unsupported-vector exception ordering

This source is outside the transpiler and fixture projects and is not run by
the active gates. It is retained for reference, not as an optional supported
feature or a passing test suite.

The deferred behavior preserves CLR exception ordering for retained
`ISimdVector<TSelf,T>.LoadAligned` and `StoreAligned` defaults over
`System.Numerics.Vector<decimal>` and fixed-width vectors over `char`, `Int128`
or `UInt128`. It does not implement memory operations for those elements.
The active compiler rejects unsupported aligned-memory paths through its
existing numeric-only alignment boundary instead of retaining these defaults.

Supported numeric vectors keep their existing managed alignment checks and
memory operations. The ordinary alignment gates, unsupported-element refusals,
helper checks, raw and rewritten inputs, and shared and unshared axes remain
active. Vector value methods and angle conversions are separate from this
deferred behavior.

The archived production files, fixtures and checkers are preserved from
`4fd16bce63f2aadfae3a26f45654eb37ab5df89f`. The runtime probe was extracted
unchanged from that revision's alignment probe. Their old paths and project
dependencies are intentionally not rewired here. Reintroducing the feature
requires reviewing its compiler integration and completing its validation;
the archived authority verification is incomplete. Removing it from the
supported scope does not establish a pass for that verification or for any
earlier revision.
