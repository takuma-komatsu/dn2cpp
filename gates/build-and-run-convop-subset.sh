#!/usr/bin/env bash
# Return-type-overloaded conversion operators across an assembly boundary:
# op_Implicit/op_Explicit overloads sharing the same parameter type but differing
# only by return type must each bind to their own overload (not the first-declared
# one). The conversions live in a separate library (ConvOpLib.dll, via -r).
source "$(dirname "$0")/_common.sh"

xasm_gate ConvOpSubset ConvOpLib.dll artifacts/convop
