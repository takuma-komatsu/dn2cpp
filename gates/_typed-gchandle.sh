# Typed GCHandle call lowering, exact signatures, boxing and interface type identity.

typed_gchandle_assert_output() {
    local output="$1" label
    for label in default zero zero-boxed zero-wrong-closed new distinct-handles equality boxed wrong-equality roundtrip cross-type-token \
        disposed disposed-zero null enum-array-new enum-array-set culture culture-null culture-set text-object text-null text-set \
        assembly assembly-null assembly-set module-object module-null module-set constrained-default interface-default \
        constrained-equals generic-object-payload interface-equals interface-wrong-closed \
        constrained-dispose interface-dispose interface-copy-allocated \
        generic-object-string generic-object-array generic-object-culture; do
        local expected=True
        [ "$label" != default ] || expected=False
        gate_output witness "$output" "typed-$label:$expected" --count 1 --policy crlf || return 1
    done
    for label in copy-update:23 string:text action:1 array:9 array-enumeration:13 token-root:37; do
        gate_output witness "$output" "typed-$label" --count 1 --policy crlf || return 1
    done
    for label in default-get default-set disposed-get disposed-set constrained-disposed-get \
        interface-disposed-get interface-disposed-set; do
        gate_output witness "$output" "typed-$label:NullReferenceException:Object reference not set to an instance of an object." \
            --count 1 --policy crlf || return 1
    done
    gate_output witness "$output" '== Typed GC handles ==' --count 1 --policy crlf || return 1
    gate_output witness "$output" 'Typed GC handles end' --count 1 --policy crlf || return 1
}

typed_gchandle_assert_types() {
    local output="$1" label
    for label in disposable equatable wrong-closed object-disposable object-equatable open-field; do
        gate_output witness "$output" "typed-type-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output witness "$output" 'Typed GC handle types end' --count 1 --policy crlf || return 1
}

typed_gchandle_negative_cases() {
    local seed="$1" root corelib fixture mode path preprocessing expected status
    shift
    corelib=$(locate_corelib) || return 1
    build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    mkdir -p artifacts || return 1
    root=$(mktemp -d "artifacts/typed-gchandle-negative-${CONFIG}.XXXXXX") || return 1
    local cases=(ldftn-dispose ldvirtftn-equals-object call-dispose-return newobj-name)
    if [ "$#" -gt 0 ]; then
        cases=("$@")
    elif [ "$DN2CPP_MARSHAL_FULL" = 1 ]; then
        cases+=(ldftn-ctor ldftn-get-target ldftn-set-target ldftn-is-allocated \
            ldftn-from-intptr ldftn-to-intptr ldftn-equals-self ldftn-equals-object ldftn-hash \
            ldvirtftn-dispose call-get-target-return call-set-target-parameter call-ctor-parameter \
            call-dispose-static call-dispose-explicit-this call-dispose-vararg call-dispose-generic \
            call-dispose-parameter call-is-allocated-return call-from-intptr-return \
            call-from-intptr-parameter call-to-intptr-parameter call-equals-parameter \
            call-equals-other-closed call-hash-return newobj-return newobj-static \
            call-owner-class call-from-intptr-class call-to-intptr-class \
            call-dispose-modreq call-set-target-modopt call-owner-value-argument call-owner-enum-argument \
            constrained-dispose constrained-equals constrained-equatable-dispose \
            constrained-disposable-equals constrained-object-equals-handle)
    fi
    for mode in "${cases[@]}"; do
        path="$root/$mode/TypedGCHandleBasics.dll"
        gate_capture_ok "$root/$mode-generator" run_bounded dotnet "$fixture" \
            --typed-gchandle-negative "$seed" "$path" "$mode" || return 1
        gate_output witness "$root/$mode-generator.stdout" "typed-gchandle-negative:$mode" --count 1 --policy crlf || return 1
        case "$mode" in
            ldftn-*|ldvirtftn-*)
                expected="typed GCHandle<T> members are lowered only at call sites; taking a member's address is not supported, so call it from a lambda instead" ;;
            newobj-name) expected='typed GCHandle newobj requires a constructor' ;;
            call-owner-class|call-from-intptr-class|call-to-intptr-class|call-dispose-modreq|call-set-target-modopt)
                expected='typed GCHandle member signature is not modeled: invalid type encoding or modifier' ;;
            call-owner-value-argument|call-owner-enum-argument)
                expected='typed GCHandle requires a CoreLib definition and a reference-type argument' ;;
            constrained-equatable-dispose)
                expected='System.IEquatable_Ginst31_XSystem_002EPrivate_002ECoreLib58_XSystem_002ERuntime_002EInteropServices_002EGCHandle_00601_1_7_NStringEnd: no method Dispose/0' ;;
            constrained-*) expected='typed GCHandle call requires its exact declaring type' ;;
            *) expected='typed GCHandle member signature is not modeled' ;;
        esac
        for preprocessing in ildiet raw; do
            local flags=()
            [ "$preprocessing" != raw ] || flags+=(--no-ildiet)
            local out="$root/$mode/$preprocessing"
            gate_capture "$root/$mode-$preprocessing" invoke_cli "$path" -r "$corelib" \
                ${flags[@]+"${flags[@]}"} -o "$out" || return 1
            status=$(cat "$root/$mode-$preprocessing.status") || return 1
            cat "$root/$mode-$preprocessing.stdout" "$root/$mode-$preprocessing.stderr" \
                > "$root/$mode-$preprocessing.diagnostic" || return 1
            if [ "$status" != 2 ] || ! grep -Fq "$expected" "$root/$mode-$preprocessing.diagnostic"; then
                echo "FAIL: typed GCHandle $mode/$preprocessing expected exit 2: $expected" >&2
                cat "$root/$mode-$preprocessing.diagnostic" >&2
                return 1
            fi
            echo "typed-gchandle-refusal:$mode:$preprocessing:ok"
        done
    done
}

typed_gchandle_assert_collections() {
    LC_ALL=C awk '
        { sub(/\r$/, "") }
        /^collections \(GC_no\): [0-9]+$/ { rows++; collections = $3 }
        END {
            if (rows != 1 || collections < 2) {
                print "FAIL: typed GCHandle token-root retention needs at least two observed collections" > "/dev/stderr"
                exit 1
            }
        }
    ' "$1"
}

typed_gchandle_retention() {
    local fixture=gates/fixtures/typed-gchandle core_root level
    build_gate_proj "$fixture/retention/TypedGCHandleRetention.csproj"
    build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
    core_root=$(mktemp -d "artifacts/typed-gchandle-core-${CONFIG}.XXXXXX")
    gate_capture_ok "$core_root/generator" run_bounded dotnet \
        "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --typed-gchandle-core "$core_root/System.Private.CoreLib.dll"
    gate_output witness "$core_root/generator.stdout" 'typed-gchandle-core:written' --count 1 --policy crlf
    # Real CoreLib Medium retention reaches unrelated unmodeled globalization.
    # The generated declaration fixture isolates typed handles; its raw bodies
    # reference a forbidden InternalCall so retaining them cannot pass silently.
    echo 'typed-gchandle Medium coverage: generated CoreLib declarations; real CoreLib Medium is not covered'
    for level in high medium; do
        (
            local _CG_CORELIB_IN="$PWD/$core_root/System.Private.CoreLib.dll"
            DN2CPP_SAMPLE_PROJECT_DIR="$fixture/retention"
            DN2CPP_OUT_SUFFIX="${DN2CPP_OUT_SUFFIX:-}-$level"
            export DN2CPP_GC_STATS=1
            local DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|typed-retention-gc-floor:2"
            corelib_diff_gate TypedGCHandleRetention --stripping-level "$level"
            # The saved observation remains mandatory on a gate-cache hit.
            typed_gchandle_assert_collections "$_CG_OUT/native.stderr"
        )
    done
}

typed_gchandle_regressions() {
    local fixture=gates/fixtures/typed-gchandle
    build_gate_proj "$fixture/basics/TypedGCHandleBasics.csproj" || return 1
    typed_gchandle_negative_cases "$fixture/basics/bin/$CONFIG/$TFM/TypedGCHandleBasics.dll" || return 1
    build_gate_proj "$fixture/type-only/TypedGCHandleTypeOnly.csproj" || return 1
    (
        DN2CPP_SAMPLE_PROJECT_DIR="$fixture/type-only"
        DN2CPP_OUT_SUFFIX="${DN2CPP_OUT_SUFFIX:-}-high"
        corelib_diff_gate TypedGCHandleTypeOnly --stripping-level high
    )
    if [ "$DN2CPP_MARSHAL_FULL" = 1 ]; then
        (
            DN2CPP_SAMPLE_PROJECT_DIR="$fixture/basics"
            DN2CPP_OUT_SUFFIX="${DN2CPP_OUT_SUFFIX:-}-high"
            corelib_diff_gate TypedGCHandleBasics --stripping-level high
        )
        typed_gchandle_retention
    fi
}
