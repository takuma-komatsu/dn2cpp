#!/usr/bin/env bash
# System.Exception.get_Message + GetType, including TypeLoadException's public
# constructor messages without entering its VM-only lazy formatter.
# dn2cpp's own catch+inspect code reads a caught exception's .Message and
# .GetType().Name to build a wrapped exception (MethodCompiler.Compile /
# Compilation.ScanBodyForGenerics) or a measure-gap record (MeasureGap.From), so
# before this slice ex.Message / ex.GetType() had no intrinsic mapping (5 self-host
# gaps: 2 own-code get_Message + 1 own-code GetType + 2 BCL *Exception.ToString
# GetType). An Exception-derived newobj is now intercepted to a message-carrying
# object (dn2cpp_exception_new) whose Dn2CppString* message slot get_Message reads;
# GetType reads the object header (== Object.GetType). The exception type-info + base
# chain still emit (the real ctor stays reached, only its message-dropping output is
# unused), so catch/throw/filter are unchanged. Output (Message, GetType().Name /
# .FullName across with-message / empty / (string,Exception) / base-default / null /
# typed catch / catch(Exception) / nested) diffs exact vs real .NET. CoreLib only.
# HResult storage is real (Exception.HResult get/set + GetBaseException), which also
# makes a parameterless Argument*/FileNotFound* exception's per-type default Message
# exact — its get_Message override picks the resource default via the real
# `HResult == COR_E_*` probe. All exercised below and diffed exact.
#
# Consolidated bucket — one sample project, one .cs per section, driven in order
# by samples/dotnet/ExceptionMessageSubset/Program.cs. Its last two sections are
# NOT about messages or type names, and this is the only place that says so; a
# later reader pruning the bucket by its heading would delete the suite's only
# desktop coverage of two EH region kinds:
#   * FaultSubset — a `fault` region. A C# try/finally inside a `yield` iterator
#     is lowered into the state machine's MoveNext as `try { } fault { }`, so an
#     exception unwinding through MoveNext runs the finally and re-raises, while
#     the normal path runs it through Dispose instead.
#   * FilterSubset — `filter`/`endfilter`. `catch (E) when (cond)` becomes an IL
#     region yielding 1 (run this handler) or 0 (continue the search); handlers
#     are tried in order, and a false filter on an inner try must let the
#     exception reach the outer one.
# Both arrived as `corelib_subset_gate` calls passing NO expected string at all,
# i.e. they only asserted exit 0; here they gain real .NET as an oracle.
#
# The third EH-region gate, build-and-run-nested-finally-subset.sh, deliberately
# did NOT fold: build-and-run-ios-sim-console.sh and build-and-run-wasm-console.sh
# each re-transpile NestedFinallySubset as their cross-compile EH probe, so the
# project has to keep existing.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate ExceptionMessageSubset
