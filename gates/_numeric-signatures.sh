# Exact BigMul and checked-wide-integer metadata: controls and refusal diagnostics.

numeric_signature_regressions() {
    local family="${1:-all}" root corelib fixture mode preprocessing expected status
    local cases=()
    case "$family" in bigmul|checked|all) ;; *) echo "FAIL: unknown numeric signature family: $family" >&2; return 1 ;; esac
    if [ "$family" != checked ]; then
        cases+=(bigmul-control bigmul-int-return-call bigmul-int-return-address \
            bigmul-native-out-call bigmul-native-out-address \
            bigmul-int-owner-modifier-call bigmul-int-owner-modifier-address \
            bigmul-int-owner-typespec-call bigmul-int-owner-typespec-address)
    fi
    if [ "$family" != bigmul ]; then
        cases+=(int128-control uint128-control \
            int128-member-return-type int128-member-generic-arity int128-methodspec-arity int128-methodspec-empty \
            int128-interface-owner-arity int128-constrained-wrong-target \
            int128-methodspec-modifier int128-methodspec-pinned \
            int128-owner-modifier int128-interface-owner-modifier \
            int128-constrained-modifier int128-constrained-pinned \
            uint128-member-return-type uint128-member-generic-arity uint128-methodspec-arity \
            uint128-constrained-wrong-target uint128-methodspec-modifier uint128-methodspec-pinned \
            uint128-owner-modifier uint128-interface-owner-arity uint128-interface-owner-modifier \
            uint128-constrained-modifier uint128-constrained-pinned)
    fi
    corelib=$(locate_corelib) || return 1
    build_gate_proj gates/fixtures/numeric-signatures/NumericSignatures.csproj || return 1
    fixture="gates/fixtures/numeric-signatures/bin/$CONFIG/$TFM/NumericSignatures.dll"
    mkdir -p artifacts || return 1
    root=$(mktemp -d "artifacts/numeric-signatures-$family-${CONFIG}.XXXXXX") || return 1
    echo "numeric-signature-artifacts:$root"
    for mode in "${cases[@]}"; do
        local input="$root/$mode/NumericSignatures.dll"
        gate_capture_ok "$root/$mode/generator" run_bounded dotnet "$fixture" "$mode" "$input" || return 1
        gate_output witness "$root/$mode/generator.stdout" "numeric-signature-fixture:$mode" --count 1 --policy crlf || return 1
        case "$mode" in
            *-control)
                expected=
                gate_capture_ok "$root/$mode/clr" run_bounded dotnet exec \
                    --runtimeconfig "${fixture%.dll}.runtimeconfig.json" "$input" || return 1 ;;
            bigmul-int-owner-typespec-*)
                expected='Primitive BigMul requires its unmodified named CoreLib owner'
                gate_capture_ok "$root/$mode/clr" run_bounded dotnet exec \
                    --runtimeconfig "${fixture%.dll}.runtimeconfig.json" "$input" || return 1 ;;
            bigmul-int-owner-*) expected='Primitive BigMul requires its unmodified named CoreLib owner' ;;
            bigmul-*) expected='Primitive BigMul requires its exact CoreLib reference and declaration' ;;
            int128-member-*) expected='Int128.CreateChecked requires its exact MethodSpec MemberRef signature' ;;
            int128-methodspec-arity) expected='System.Int128: no generic method CreateChecked<2>/1' ;;
            int128-methodspec-empty) expected='NumericSignatures.Program.Main: Cannot decode MethodSpec 0x2b000001:' ;;
            int128-methodspec-*) expected='Int128.CreateChecked requires its exact closed integer MethodSpec signature' ;;
            int128-owner-modifier|int128-interface-owner-modifier) expected='Int128.CreateChecked requires an unmodified exact owner TypeSpec' ;;
            *-interface-owner-arity) expected='INumberBase.CreateChecked requires exactly one owner type argument' ;;
            int128-constrained-wrong-target) expected='Int128.CreateChecked requires its exact constrained interface signature' ;;
            int128-constrained-*) expected='Int128.CreateChecked requires an unmodified constrained TypeSpec' ;;
            uint128-member-*) expected='UInt128.CreateChecked requires its exact MethodSpec MemberRef signature' ;;
            uint128-methodspec-arity) expected='System.UInt128: no generic method CreateChecked<2>/1' ;;
            uint128-methodspec-*) expected='UInt128.CreateChecked requires its exact closed integer MethodSpec signature' ;;
            uint128-owner-modifier|uint128-interface-owner-modifier) expected='UInt128.CreateChecked requires an unmodified exact owner TypeSpec' ;;
            uint128-constrained-wrong-target) expected='UInt128.CreateChecked requires its exact constrained interface signature' ;;
            uint128-constrained-*) expected='UInt128.CreateChecked requires an unmodified constrained TypeSpec' ;;
            *) echo "FAIL: unclassified numeric signature fixture: $mode" >&2; return 1 ;;
        esac
        for preprocessing in raw ildiet; do
            local flags=() prefix="$root/$mode/$preprocessing" required="$expected"
            [ "$preprocessing" != raw ] || flags+=(--no-ildiet)
            # ILDiet canonicalizes an unmodified primitive TypeSpec into the
            # named TypeRef accepted by the exact BigMul resolver.
            if [[ "$mode" = bigmul-int-owner-typespec-* ]] && [ "$preprocessing" = ildiet ]; then required=; fi
            if [ "$mode" = int128-methodspec-empty ] && [ "$preprocessing" = ildiet ]; then required='ILDiet failed with exit code 1'; fi
            gate_capture "$prefix/cli" invoke_cli "$input" -r "$corelib" \
                ${flags[@]+"${flags[@]}"} -o "$prefix/emitted" || return 1
            status=$(cat "$prefix/cli.status") || return 1
            if [ -z "$required" ]; then
                gate_output status "$status" 0 || { cat "$prefix/cli.stderr" >&2; return 1; }
                if [[ "$mode" = bigmul-int-owner-typespec-* ]]; then
                    gate_capture_ok "$prefix/normalized" run_bounded dotnet "$fixture" --check-bigmul-normalized \
                        "$mode" "$prefix/emitted/ildiet/ildiet-result.xml" || return 1
                fi
                gate_python gates/fixtures/numeric-signatures/check-emission.py "$mode" "$prefix/emitted" || return 1
                echo "numeric-signature-control:$mode:$preprocessing:ok"
            else
                cat "$prefix/cli.stdout" "$prefix/cli.stderr" > "$prefix/diagnostic" || return 1
                if [ "$status" != 2 ] || ! grep -Fq "$required" "$prefix/diagnostic"; then
                    echo "FAIL: numeric signature $mode/$preprocessing expected exit 2: $required" >&2
                    cat "$prefix/diagnostic" >&2
                    return 1
                fi
                echo "numeric-signature-refusal:$mode:$preprocessing:ok"
            fi
        done
    done
}
