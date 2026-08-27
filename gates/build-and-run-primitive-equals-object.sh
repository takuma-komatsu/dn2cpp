#!/usr/bin/env bash
# Int32/Int64/Int16/Byte/Char/Boolean.Equals(object): the boxed-argument overload
# the integer primitives share, reached by value-object wrappers whose
# Equals(object) forwards to the wrapped primitive's. Real .NET is the oracle for
# the same-type-box-only rule (no cross-type numeric equality, null is false).
source "$(dirname "$0")/_common.sh"

corelib_diff_gate PrimitiveEqualsObject
