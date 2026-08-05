#!/usr/bin/env bash
# Cross-assembly generic-method pipeline: an app calls generic methods declared
# on a non-generic type in a *separate* library assembly (Lib.Echo<T>). Each call
# is a MethodSpec over a MemberRef with a TypeReference parent, instantiated in
# the library's module. app + library DLL -> cross-assembly transpile -> native
# binary -> run. The three core gates
# (sample/multiassembly/godot) remain the regression set.
source "$(dirname "$0")/_common.sh"

xasm_gate XAsmGenericMethod XGenericMethodLib.dll artifacts/xasmgenericmethod
