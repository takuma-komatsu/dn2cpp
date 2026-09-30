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
#
# ConstBodyNullFaultSubset is the callvirt null check at a call site folded to a
# constant: a non-virtual callee whose body is `ldc; ret` never touches its
# receiver, so without the check kept at the fold a null receiver answers the
# constant instead of raising NullReferenceException.
#
# Runtime-raised argument fields and the shared-source whitespace guard are compared
# with .NET. Separate fixtures cover layouts without explicit exception constructors
# and Message-only fallback, including NUL and unpaired UTF-16 surrogates.
source "$(dirname "$0")/_common.sh"

fields_app="gates/fixtures/runtime-argument-fields/bin/$CONFIG/$TFM/RuntimeArgumentFields.dll"
fallback_app="gates/fixtures/runtime-argument-fallback/bin/$CONFIG/$TFM/RuntimeArgumentFallback.dll"
build_gate_proj gates/fixtures/runtime-argument-fields/RuntimeArgumentFields.csproj
build_gate_proj gates/fixtures/runtime-argument-fallback/RuntimeArgumentFallback.csproj
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $fields_app ${fields_app%.dll}.runtimeconfig.json ${fields_app%.dll}.deps.json $fallback_app ${fallback_app%.dll}.runtimeconfig.json ${fallback_app%.dll}.deps.json"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|runtime-argument-fixtures|cli:$(_gate_cli_hash)"

gate_extra_asserts() {
    local out="$1" native before prefix line app name fixture expected actual
    native=$(run_bounded "./$out/ExceptionMessageSubset")
    native=$(strip_cr_win "$native")
    before=$(dotnet "$_CG_APP" before-runtime-fields)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^-- runtime-raised argument fields --$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '-- runtime-raised argument fields --' \
        'dictionary null key: ArgumentNullException param=key' \
        'builder repeat: ArgumentOutOfRangeException param=repeatCount actual=-1 actual-type=Int32' \
        'builder window null: ArgumentNullException param=value' \
        '-- runtime-raised parameter names --' \
        'polyfill blank NUL message: ArgumentException param=Argument is whitespace' \
        'polyfill blank surrogate message: ArgumentException param=Argument is whitespace'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: runtime argument witness missing: $line" >&2; exit 1; }
    done
    for app in "$fields_app" "$fallback_app"; do
        name=$(basename "${app%.dll}")
        fixture="$out/$name"
        invoke_cli "$app" -r "$_CG_CORELIB" -o "$fixture"
        compile_console "$fixture" "$name"
        expected=$(run_bounded dotnet "$app")
        actual=$(run_bounded "./$fixture/$name")
        actual=$(strip_cr_win "$actual")
        assert_output "$actual" "$(strip_cr_win "$expected")"
        if [ "$app" = "$fields_app" ]; then
            for line in '-- argument fields without constructors --' \
                'substring negative start: ArgumentOutOfRangeException:startIndex' \
                'actual=-1:Int32' \
                'builder count: ArgumentOutOfRangeException:repeatCount' \
                'join null: ArgumentNullException:value' \
                'dictionary null: ArgumentNullException:key' \
                'ilist null: ArgumentNullException:item'; do
                grep -Fxq -- "$line" <<< "$actual" \
                    || { echo "FAIL: constructor-free argument witness missing: $line" >&2; exit 1; }
            done
        else
            grep -Fxq 'const int32_t dn2cpp_type_bind_count = 0;' "$fixture/generated.cpp" \
                || { echo 'FAIL: fallback fixture reached a managed exception layout' >&2; exit 1; }
            for line in '-- argument Message fallback --' \
                "Value cannot be null. (Parameter 'a<nul>b')" \
                "Value cannot be null. (Parameter 'a<sur>b')" \
                "a<nul>b (Parameter 'Argument is whitespace')" \
                "a<sur>b (Parameter 'Argument is whitespace')"; do
                grep -Fxq -- "$line" <<< "$actual" \
                    || { echo "FAIL: fallback argument witness missing: $line" >&2; exit 1; }
            done
        fi
    done
}

corelib_diff_gate ExceptionMessageSubset
