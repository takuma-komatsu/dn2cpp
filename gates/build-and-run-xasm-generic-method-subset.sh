#!/usr/bin/env bash
# Cross-assembly generic-method pipeline: an app calls generic methods declared
# on a non-generic type in a *separate* library assembly (Lib.Echo<T>). Each call
# is a MethodSpec over a MemberRef with a TypeReference parent, instantiated in
# the library's module. app + library DLL -> cross-assembly transpile -> native
# binary -> run. The three core gates
# (sample/multiassembly/godot) remain the regression set.
source "$(dirname "$0")/_common.sh"

xasm_gate XAsmGenericMethod XGenericMethodLib.dll artifacts/xasmgenericmethod

generated=artifacts/xasmgenericmethod/generated.h
for symbol in \
    'Program_Main_m[0-9]+' \
    'Lib_Echo_Tis[A-Za-z0-9_]+_m[0-9]+' \
    'Lib_Echo_TisPoint_m[0-9]+' \
    'Lib_PairTag_Tis[A-Za-z0-9_]+_TisBoolean_m[0-9]+' \
    'NamingProbe_1__ctor_m[0-9]+' \
    'NamingProbe_1__cctor_m[0-9]+' \
    'NamingProbe_1_get_Value_m[0-9]+' \
    'NamingProbe_1_op_Addition_m[0-9]+' \
    'NamingProbe_1_XAsmGenericMethod_Program_INamingProbe_Mark_m[0-9]+' \
    'NamingProbe_1_MUE9thod_m[0-9]+' \
    'Program_U3CMainU3Eg__GeneratedU7C[0-9]+_[0-9]+_m[0-9]+'; do
    grep -qE "$symbol" "$generated" || {
        echo "FAIL: IL2CPP-style method name missing: $symbol" >&2
        exit 1
    }
done

cctor=$(grep -oE 'NamingProbe_1__cctor_m[0-9]+' "$generated" | sort -u)
grep -q "${cctor}__ensure" "$generated" || {
    echo "FAIL: cctor ensure helper does not retain the method base symbol" >&2
    exit 1
}

collision_symbols=$(grep -oE 'CollisionProbe_Run_m[0-9]+' "$generated" | sort -u)
if [ "$(grep -c . <<<"$collision_symbols")" -ne 2 ]; then
    echo "FAIL: cross-module same-simple-name methods do not have two unique symbols" >&2
    echo "$collision_symbols" >&2
    exit 1
fi

if grep -qE '(m_XAsmGenericMethod|XAsmGenericMethod_Program_Main_m|Program_NamingProbe_[A-Za-z0-9_]*_m|TisXAsmGenericMethod_)' "$generated"; then
    echo "FAIL: method readable stems still carry a namespace, enclosing type, or m_ prefix" >&2
    exit 1
fi
echo "IL2CPP-style method names: OK"
