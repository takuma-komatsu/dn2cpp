#!/usr/bin/env bash
# System.Exception.get_Message + GetType, including TypeLoadException's public
# constructor messages without entering its VM-only lazy formatter.
# Runtime-created fault HResults match their types while constructor and explicit codes survive.
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
# UInt32 and Int32 bound messages retain their suffixes after a collection.
# Aggregate messages evaluate virtual inner getters lazily on each read.
# Enumerable aggregate constructors preserve collection and enumerator semantics.
# Derived aggregate constructors retain snapshots and cached read-only collections;
# user fields and virtual or non-virtual Message reads preserve the aggregate prefix.
# Reflective constructor reachability retains unused legacy aggregate serialization bodies.
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} samples/dotnet/ExceptionMessageSubset/OrdinaryReflectionArgumentSubset.cs"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|ordinary-reflection-arguments:${DN2CPP_BEFORE_ORDINARY_REFLECTION_ARGUMENTS:-}"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|before-array-shape-fields|before-runtime-hresult|before-lazy-aggregate-message|before-aggregate-enumerable|before-derived-aggregate|before-aggregate-serialization"

ancestry_app="gates/fixtures/runtime-exception-ancestry/bin/$CONFIG/$TFM/RuntimeExceptionAncestry.dll"
build_gate_proj gates/fixtures/runtime-exception-ancestry/RuntimeExceptionAncestry.csproj
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $ancestry_app ${ancestry_app%.dll}.runtimeconfig.json ${ancestry_app%.dll}.deps.json"
fields_app="gates/fixtures/runtime-argument-fields/bin/$CONFIG/$TFM/RuntimeArgumentFields.dll"
fallback_app="gates/fixtures/runtime-argument-fallback/bin/$CONFIG/$TFM/RuntimeArgumentFallback.dll"
fallback_bcl="$(dirname "$(resolve_net10_corelib)")/System.Collections.Concurrent.dll"
general_app="gates/fixtures/general-argument-fallback/bin/$CONFIG/$TFM/GeneralArgumentFallback.dll"
build_gate_proj gates/fixtures/runtime-argument-fields/RuntimeArgumentFields.csproj
build_gate_proj gates/fixtures/runtime-argument-fallback/RuntimeArgumentFallback.csproj
build_gate_proj gates/fixtures/general-argument-fallback/GeneralArgumentFallback.csproj
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $fields_app ${fields_app%.dll}.runtimeconfig.json ${fields_app%.dll}.deps.json $fallback_app ${fallback_app%.dll}.runtimeconfig.json ${fallback_app%.dll}.deps.json $fallback_bcl $general_app ${general_app%.dll}.runtimeconfig.json ${general_app%.dll}.deps.json"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|runtime-argument-fixtures|cli:$(_gate_cli_hash)"

gate_extra_asserts() {
    local out="$1" native before prefix line app name fixture expected actual
    native=$(run_bounded "./$out/ExceptionMessageSubset")
    native=$(strip_cr_win "$native")
    prefix=$(awk '/^== aggregate legacy serialization reachability ==$/ { exit } { print }' <<< "$native")
    before=$(run_bounded dotnet "$_CG_APP" before-aggregate-serialization)
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded "./$out/ExceptionMessageSubset$EXE_EXT" before-aggregate-serialization)
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== aggregate legacy serialization reachability ==' \
        'legacy aggregate message=legacy (inner)' \
        'legacy aggregate inner=True/fields=53/normal' \
        'legacy serialization calls=0' \
        'aggregate legacy serialization reachability end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: aggregate serialization reachability witness missing: $line" >&2; return 1; }
    done
    prefix=$(awk '/^== derived aggregate constructors and getters ==$/ { exit } { print }' <<< "$native")
    before=$(run_bounded dotnet "$_CG_APP" before-derived-aggregate)
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded "./$out/ExceptionMessageSubset$EXE_EXT" before-derived-aggregate)
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== derived aggregate constructors and getters ==' \
        'derived ctor default fields=37/ctor' \
        'derived ctor message array count=2 first=True order=True cached=True' \
        'derived ctor list snapshot count=2 first=True order=True cached=True' \
        'derived custom sequence trace=Get/Move/Current/Move/Current/Move/Dispose/' \
        'derived custom collection trace=Count/Copy/' \
        'derived rewritten fields=41/after' \
        'derived read-only=True' 'derived read-only add=NotSupportedException' 'derived read-only count=2' \
        'derived null array=ArgumentNullException param=innerExceptions' \
        'derived null single=ArgumentNullException param=innerException' \
        'derived constructed reads=0/0' \
        'derived first message=lazy (derived-left:1) (derived-right:1)' \
        'derived second reads=2/2' 'derived nonvirtual base reads=3/3' \
        'derived own constructed reads=0/0/0' \
        'derived own virtual message=own/own base (own-left:1) (own-right:1)' \
        'derived own getter reads=1' 'derived own fault getter threw=InvalidOperationException:own getter' \
        'derived own fault getter reads=2' \
        'derived further fields=73/further' 'derived further level=9 constructed reads=0/0' \
        'derived inner fault reads=1/0' 'derived base inner fault reads=2/0' \
        'derived caught identity=True first=True' \
        'derived aggregate constructors and getters end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: derived aggregate witness missing: $line" >&2; return 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-aggregate-enumerable)
    prefix=$(awk '/^== aggregate enumerable constructors ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== aggregate enumerable constructors ==' \
        'enumerable constructed reads=0/0' \
        'custom sequence trace=Get/Move/Current/Move/Current/Move/Dispose/' \
        'collection trace=Count/Copy/' \
        'enumerable first message=One or more errors occurred. (first:1) (second:1)' \
        'enumerable second message=One or more errors occurred. (first:2) (second:2)' \
        'enumerable custom message=custom (first:3) (second:3)' \
        'ctor empty enumerable=One or more errors occurred. count=0' \
        'ctor empty custom sequence= count=0' \
        'enumerable null=ArgumentNullException param=innerExceptions' \
        'enumerable custom null=ArgumentNullException param=innerExceptions' \
        'enumerable null element=ArgumentException param=' \
        'enumerable null element trace=Get/Move/Current/Move/Current/Move/Dispose/ reads=3' \
        'sequence Get fault=sequence fault original=True dispose=False' \
        'sequence Get trace=Get/' \
        'sequence Move fault=sequence fault original=True dispose=False' \
        'sequence Move trace=Get/Move/Dispose/' \
        'sequence Current fault=sequence fault original=True dispose=False' \
        'sequence Current trace=Get/Move/Current/Dispose/' \
        'sequence Dispose fault=dispose fault original=False dispose=True' \
        'sequence Dispose trace=Get/Move/Current/Move/Dispose/' \
        'sequence Current+Dispose fault=dispose fault original=False dispose=True' \
        'sequence Current+Dispose trace=Get/Move/Current/Dispose/' \
        'aggregate enumerable constructors end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: aggregate enumerable witness missing: $line" >&2; return 1; }
    done
    assert_output "$(grep -Fc 'enumerable snapshot count=2 first=True order=True' <<< "$native")" '5'
    before=$(run_bounded dotnet "$_CG_APP" before-lazy-aggregate-message)
    prefix=$(awk '/^== lazy aggregate Message ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== lazy aggregate Message ==' \
        'pair constructed reads=0/0 first=True order=True' \
        'pair first message=One or more errors occurred. (left:1) (right:1)' \
        'pair second message=One or more errors occurred. (left:2) (right:2)' \
        'ordinary stored=stored reads=2' \
        'throwing constructed reads=0/0 first=True order=True' \
        'throwing first getter threw=InvalidOperationException:message getter' \
        'throwing second reads=2/0' \
        'nested constructed reads=0 identity=True' \
        'nested second message=outer (One or more errors occurred. (nested:2)) (tail)' \
        'ctor snapshot=One or more errors occurred. (original) first=True element=True' \
        'ctor null array=ArgumentNullException param=innerExceptions' \
        'ctor null element=ArgumentException param=' \
        'ctor null single=ArgumentNullException param=innerException' \
        'aggregate UTF16=0062 0061 0073 0065 DFFF 0020 0028 0078 0000 D800 0079 0029' \
        'derived Message=derived/derived base base=derived base' \
        'lazy aggregate Message end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: lazy aggregate Message witness missing: $line" >&2; return 1; }
    done
    grep -Eq '^.*vt_ExceptionMessageSubset_ExceptionVirtualMembers_ChangingMessage\[\].*ChangingMessage_get_Message_m[0-9]+' "$out"/generated*.cpp \
        || { echo 'FAIL: the side-effecting Message override was not installed' >&2; return 1; }
    before=$(DN2CPP_BEFORE_ORDINARY_REFLECTION_ARGUMENTS=1 run_bounded "./$out/ExceptionMessageSubset$EXE_EXT")
    prefix=$(awk '/^-- ordinary reflection argument fields --$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '-- ordinary reflection argument fields --' \
        'Type.GetEnumUnderlyingType null receiver: NullReferenceException' \
        'Type.GetEnumNames null receiver: NullReferenceException' \
        'Type.GetEnumValuesAsUnderlyingType null receiver: NullReferenceException' \
        'ordinary reflection argument fields end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: ordinary reflection argument witness missing: $line" >&2; return 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-runtime-hresult)
    prefix=$(awk '/^== runtime exception HResult ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== runtime exception HResult ==' \
        'null receiver: NullReferenceException/80004003' 'throw null: NullReferenceException/80004003' \
        'array index: IndexOutOfRangeException/80131508' 'cast: InvalidCastException/80004002' \
        'unbox: InvalidCastException/80004002' 'checked conversion: OverflowException/80131516' \
        'checked addition: OverflowException/80131516' 'integer division: DivideByZeroException/80020012' \
        'integer remainder: DivideByZeroException/80020012' 'integer format: FormatException/80131537' \
        'parse null: ArgumentNullException/80004003' 'substring range: ArgumentOutOfRangeException/80131502' \
        'duplicate key: ArgumentException/80070057' 'missing key: KeyNotFoundException/80131577' \
        'read-only collection: NotSupportedException/80131515' 'changed collection: InvalidOperationException/80131509' \
        'disposed stream: ObjectDisposedException/80131622' 'missing constructor: MissingMethodException/80131513' \
        'array rank: RankException/80131517' 'array element type: ArrayTypeMismatchException/80131503' \
        'constructed null: NullReferenceException/80004003' 'constructed index: IndexOutOfRangeException/80131508' \
        'constructed cast: InvalidCastException/80004002' 'constructed overflow: OverflowException/80131516' \
        'constructed division: DivideByZeroException/80020012' 'constructed format: FormatException/80131537' \
        'constructed base: Exception/80131500' 'custom constructor code: CustomCode/81234567' \
        'custom argument: CustomArgument/80070057' 'aggregate default: AggregateException/80131500' \
        'assigned thrown code: Exception/0000007B' 'explicit HRESULT: COMException/81234567' \
        'runtime exception HResult end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: runtime HResult witness missing: $line" >&2; return 1; }
    done
    before=$(run_bounded "./$out/ExceptionMessageSubset" before-runtime-exception-chains)
    prefix=$(awk '/^== runtime exception ancestry ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    grep -Fxq 'runtime exception ancestry end' <<< "$native" \
        || { echo 'FAIL: runtime exception ancestry section did not run' >&2; exit 1; }
    fixture="$out/RuntimeExceptionAncestry"
    DN2CPP_STRICT_COMPLETION=1 invoke_cli "$ancestry_app" -r "$_CG_CORELIB" -o "$fixture"
    compile_console "$fixture" RuntimeExceptionAncestry
    expected=$(run_bounded dotnet "$ancestry_app")
    actual=$(run_bounded "./$fixture/RuntimeExceptionAncestry")
    actual=$(strip_cr_win "$actual")
    assert_output "$actual" "$(strip_cr_win "$expected")"
    for line in 'array index: IndexOutOfRangeException > SystemException > Exception > Object' \
        'no parameterless ctor: MissingMethodException > MissingMemberException > MemberAccessException > SystemException > Exception > Object' \
        'HRESULT E_FAIL: COMException > ExternalException > SystemException > Exception > Object'; do
        grep -Fxq -- "$line" <<< "$actual" \
            || { echo "FAIL: runtime exception chain missing: $line" >&2; exit 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-general-argument-fields)
    prefix=$(awk '/^-- general BCL argument fields --$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '-- general BCL argument fields --' \
        'console null array pair=[<><>]' \
        'general BCL argument fields end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: general BCL argument witness missing: $line" >&2; exit 1; }
    done
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
    local array_before array_prefix
    array_before=$(run_bounded "$out/ExceptionMessageSubset$EXE_EXT" before-array-shape-fields)
    array_prefix=$(awk '/^-- runtime Array argument fields --$/ { exit } { print }' <<< "$native")
    assert_output "$array_prefix" "$(strip_cr_win "$array_before")"
    for line in '-- runtime Array argument fields --' \
            'array-createinstance-length2: ArgumentOutOfRangeException param=length2 actual=-1 actual-type=Int32' \
            'array-getvalue-null-indices: ArgumentNullException param=indices' \
            'runtime Array argument fields end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: runtime Array fields witness missing: $line" >&2; exit 1; }
    done
    for app in "$fields_app" "$fallback_app" "$general_app"; do
        name=$(basename "${app%.dll}")
        fixture="$out/$name"
        if [ "$name" = GeneralArgumentFallback ]; then
            invoke_cli "$app" -r "$_CG_CORELIB" -r "$fallback_bcl" -o "$fixture"
        else
            invoke_cli "$app" -r "$_CG_CORELIB" -o "$fixture"
        fi
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
        elif [ "$name" = GeneralArgumentFallback ]; then
            local binds
            binds=$(grep '^const Dn2CppTypeBind dn2cpp_type_binds' "$fixture/generated.cpp")
            if [[ "$binds" == *'&dn2cpp_argument_exception_type'* ||
                "$binds" == *'&dn2cpp_argument_null_exception_type'* ||
                "$binds" == *'&dn2cpp_argument_out_of_range_exception_type'* ]]; then
                echo 'FAIL: general Message-only fixture bound an argument exception layout' >&2
                exit 1
            fi
            grep -Fxq 'general BCL Message fallback end' <<< "$actual" \
                || { echo 'FAIL: general Message-only fixture did not run' >&2; exit 1; }
            local decimal_prefix decimal_before
            decimal_prefix=$(awk '/^-- decimal parse Message fallback --$/ { exit } { print }' <<< "$actual")
            decimal_before=$(run_bounded dotnet "$app" before-decimal-fault-text)
            assert_output "$decimal_prefix" "$(strip_cr_win "$decimal_before")"
            grep -Fxq 'decimal parse Message fallback end' <<< "$actual" \
                || { echo 'FAIL: decimal Message-only faults did not run' >&2; exit 1; }
        else
            grep -Fxq 'const int32_t dn2cpp_type_bind_count = 0;' "$fixture/generated.cpp" \
                || { echo 'FAIL: fallback fixture reached a managed exception layout' >&2; exit 1; }
            for line in '-- argument Message fallback --' \
                "Value cannot be null. (Parameter 'a<nul>b')" \
                "Value cannot be null. (Parameter 'a<sur>b')" \
                "a<nul>b (Parameter 'Argument is whitespace')" \
                "a<sur>b (Parameter 'Argument is whitespace')" \
                '-- bound Message fallback --' 'bound Message fallback end' \
                "insert unsigned: startIndex ('4294967295') must be less than or equal to '3'. (Parameter 'startIndex')|Actual value was 4294967295." \
                "array wrapped room: startIndex ('0') must be less than or equal to '-2147483645'. (Parameter 'startIndex')|Actual value was 0."; do
                grep -Fxq -- "$line" <<< "$actual" \
                    || { echo "FAIL: fallback argument witness missing: $line" >&2; exit 1; }
            done
        fi
    done
}

corelib_diff_gate ExceptionMessageSubset
