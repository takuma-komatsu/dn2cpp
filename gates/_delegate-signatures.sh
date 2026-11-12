#!/usr/bin/env bash
# Delegate declaration, raw resolver and ILDiet preflight boundaries without emission.

delegate_signature_regressions() {
    local root corelib fixture mode result
    corelib=$(locate_corelib) || return 1
    fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    if [ -z "${DN2CPP_CLI_DLL:-}" ] || [ ! -f "$fixture" ]; then
        echo 'FAIL: delegate signature boundaries require the caller-prepared CLI and LdftnLocalFixture' >&2
        return 1
    fi
    mkdir -p artifacts || return 1
    root=$(mktemp -d "artifacts/delegate-signatures-${CONFIG}.XXXXXX") || return 1
    echo "delegate-signature-artifacts:$root"
    gate_capture_ok "$root/boundaries" run_bounded dotnet "$fixture" --delegate-signatures \
        "$DN2CPP_CLI_DLL" "$corelib" "$root/fixtures" || return 1
    local witnesses=('delegate-declaration-contract:ok' 'delegate-signature-preflight:trailing-call:refused'
        'delegate-signature-model:one:reachable-zero:emission-none' 'delegate-signature-boundaries:ok')
    for mode in static nonvirtual native bodyless explicit-this wrong-return sealed-owner wrong-base; do
        witnesses+=("delegate-declaration-refusal:$mode")
    done
    for mode in control return-call parameter-address modifier-call header-address \
        trailing-call fake-owner-address owner-typespec-call owner-modifier-address; do
        result=refused
        [ "$mode" != control ] || result=accepted
        witnesses+=("delegate-signature-resolver:$mode:raw:$result")
        [ "$mode" != trailing-call ] || continue
        [ "$mode" != owner-typespec-call ] || result=accepted
        witnesses+=("delegate-signature-preflight:$mode:rewritten" "delegate-signature-resolver:$mode:rewritten:$result")
    done
    gate_output byte-witness "$root/boundaries.stdout" "${witnesses[@]}" --count 1 --policy crlf || return 1
    echo 'delegate-signature-boundaries:ok'
}

if [ "${BASH_SOURCE[0]}" = "$0" ]; then
    source "$(dirname "$0")/_common.sh"
    prepare_cli
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
    delegate_signature_regressions
fi
