#!/usr/bin/env bash
# Consolidated Unsafe/MemoryMarshal arbitrary-type gate: proves the transpiler's
# System.Runtime.CompilerServices.Unsafe and System.Runtime.InteropServices.MemoryMarshal
# intrinsic interceptions are correct over arbitrary element/struct types and arbitrary
# memory backings (stackalloc, heap arrays, struct fields), not just int. Covers
# Unsafe.As / AsRef / Add / Subtract / ByteOffset / SizeOf / AreSame / NullRef /
# IsNullRef / Read / Write / ReadUnaligned / WriteUnaligned / CopyBlock / InitBlock
# / Unbox (mutate a box's payload in place through the returned ref),
# and MemoryMarshal.Cast / AsBytes / GetReference / GetArrayDataReference / Read /
# Write / CreateSpan / CreateReadOnlySpan over multi-field structs, double/long, and
# byte-backed reinterprets. Also proves [StructLayout] layout control:
# LayoutKind.Explicit unions (all-offset-0 and disjoint offsets), explicit Size,
# Pack=1/Pack=8 packing, and an explicit union nested in a packed sequential
# struct (the godot_variant shape), checked by sizeof / byte punning / AsBytes.
# Reinterpret byte order is host-endian — the gate diffs the
# transpiled binary against real .NET on the same machine, so they agree.
# Typed references retain their exact declared type and writable location,
# including shared generic arguments and invalid refanyval casts.
# Diffed exact vs real .NET.
source "$(dirname "$0")/_common.sh"

gate_capture_diff_files() {
    local output="$3" tail="$1/assert-typedref-tail.stdout"
    gate_output byte-witness "$output" 'typedref complete' --policy crlf || return 1
    gate_output select "$output" "$tail" --from 'typedref complete' --policy crlf || return 1
    gate_assert_text "$tail" 'typedref complete'
}

gate_extra_asserts() {
    local python
    python="$(resolve_python)"
    # shellcheck disable=SC2086 -- resolve_python may return `py -3`.
    $python - "$1" <<'PY'
from pathlib import Path
import re
import sys

source = "\n".join(p.read_text() for p in Path(sys.argv[1]).glob("generated*.cpp"))
body = re.search(r"// UnsafeGeneral.TypedReferenceSubset.Program::HeapReference\n.*?\n}", source, re.S)
assert body, "Missing typed reference GC witness"
reference = re.search(r"(\w+) = Dn2CppTypedReference\{", body[0])
assert reference, "Missing interior typed reference"
assert re.search(r"= Program_Collect_m\d+\(\);\s+DN2CPP_WEB_GC_LIVENESS\(\(" +
                 re.escape(reference[1]) + r"\)\.f__value\);", body[0]), \
    "Pending typed reference must survive the collecting argument call on Web"
PY
}

corelib_diff_gate UnsafeGeneral
