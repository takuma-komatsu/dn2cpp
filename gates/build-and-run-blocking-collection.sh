#!/usr/bin/env bash
# BlockingCollection<T> — a producer/consumer blocking queue on real threads. Sections:
# multi-producer/multi-consumer with CompleteAdding + blocking Take draining to an
# InvalidOperationException exit (totals match); single-thread FIFO order; CompleteAdding
# then drain + Take-throws / TryTake-false on completed-empty; bounded capacity (the
# capacity-2 queue blocks a 6-item producer until the consumer drains); TryTake/TryAdd
# timeouts; and reference (string) + 64-bit (long) + double element kinds. Every result
# is read after Join, so the output is deterministic and diffed exact vs real .NET.
# BlockingCollection<T> lives in System.Collections.Concurrent (not CoreLib), so that
# assembly is referenced alongside CoreLib.
# Receiver, capacity, timeout and completed-state argument validation.
# Disposal through direct and IDisposable routes.
# Closed nested generic names and namespaces, including disposal ObjectName.
# Array assembly lookup and MemberInfo/Type module ownership agree.
source "$(dirname "$0")/_common.sh"
message_app="gates/fixtures/blocking-disposal-message/bin/$CONFIG/$TFM/BlockingDisposalMessage.dll"
build_gate_proj gates/fixtures/blocking-disposal-message/BlockingDisposalMessage.csproj
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $message_app ${message_app%.dll}.runtimeconfig.json ${message_app%.dll}.deps.json"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|blocking-disposal-message|cli:$(_gate_cli_hash)"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|closed-nested-names-prefix-argv:before-closed-nested-names"
gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/BlockingCollectionSubset")
    native=$(strip_cr_win "$native")
    before=$(run_bounded dotnet "$_CG_APP" before-collection-disposal)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== dispose checks ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== dispose checks ==' \
        'dispose checks end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: BlockingCollectionSubset lifecycle witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-collection-validation)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== argument checks ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== argument checks ==' \
        'argument checks end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: BlockingCollectionSubset validation witness missing: $line" >&2; exit 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-closed-nested-names)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== closed nested reflection names ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    before=$(run_bounded "./$out/BlockingCollectionSubset" before-closed-nested-names)
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== closed nested reflection names ==' \
        'global Name=Program' 'global Namespace=<null>' \
        'plain Namespace=NestedTypeNameProbe' \
        'outer-reference Name=Outer`1' 'outer-value Name=Outer`1' \
        'nested-definition Name=Node' 'nested-definition Namespace=NestedTypeNameProbe' \
        'nested-reference Name=Node' 'nested-reference Namespace=NestedTypeNameProbe' \
        'nested-value Name=Node' 'nested-value Namespace=NestedTypeNameProbe' \
        'nested-generic Name=Inner`1' 'nested-generic-value Name=Inner`1' \
        'nested-deep Name=Leaf`1' 'nested-array-arguments Name=Inner`1' \
        'nested-array Name=Node[]' 'nested-array Namespace=NestedTypeNameProbe' \
        'nested-jagged-array Name=Node[][]' 'nested-jagged-array Namespace=NestedTypeNameProbe' \
        'nested-rank2-array Name=Node[,]' 'nested-rank2-array Namespace=NestedTypeNameProbe' \
        'nested-user-array-arguments Name=Inner`1' \
        'plain disposed type=ObjectDisposedException' \
        'outer-reference disposed type=ObjectDisposedException' \
        'nested-reference disposed type=ObjectDisposedException' \
        'nested-value disposed type=ObjectDisposedException' \
        'nested-generic disposed type=ObjectDisposedException' \
        'nested-generic-value disposed type=ObjectDisposedException' \
        'nested-deep disposed type=ObjectDisposedException' \
        'nested-array-arguments disposed type=ObjectDisposedException' \
        'nested-array disposed type=ObjectDisposedException' \
        'nested-jagged-array disposed type=ObjectDisposedException' \
        'nested-rank2-array disposed type=ObjectDisposedException' \
        'nested-user-array-arguments disposed type=ObjectDisposedException' \
        'closed nested reflection names end' \
        '== array assembly ownership ==' 'array assembly ownership end' \
        'plain-array assembly roundtrip=True' 'plain-array assembly explicit=True' \
        'plain-array assembly throwing=True' 'plain-array assembly ignore-case=True' \
        'plain-array member module=True' \
        'plain-jagged-array assembly roundtrip=True' 'plain-jagged-array assembly explicit=True' \
        'plain-jagged-array assembly throwing=True' 'plain-jagged-array assembly ignore-case=True' \
        'plain-jagged-array member module=True' \
        'plain-rank2-array assembly roundtrip=True' 'plain-rank2-array assembly explicit=True' \
        'plain-rank2-array assembly throwing=True' 'plain-rank2-array assembly ignore-case=True' \
        'plain-rank2-array member module=True' \
        'closed-generic assembly roundtrip=True' 'closed-generic assembly explicit=True' \
        'closed-generic assembly throwing=True' 'closed-generic member module=True'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: closed nested reflection name witness missing: $line" >&2; exit 1; }
    done
    local fixture="$out/message-only" expected actual
    invoke_cli "$message_app" -r "$_CG_CORELIB" -r "$(dirname "$_CG_CORELIB")/System.Collections.Concurrent.dll" -o "$fixture"
    compile_console "$fixture" BlockingDisposalMessage
    expected=$(run_bounded dotnet "$message_app")
    actual=$(run_bounded "./$fixture/BlockingDisposalMessage")
    actual=$(strip_cr_win "$actual")
    assert_output "$actual" "$(strip_cr_win "$expected")"
    if rg -q -w 'tibind_System_ObjectDisposedException' "$fixture" --glob '*.cpp'; then
        echo "FAIL: message-only fixture bound ObjectDisposedException fields" >&2
        exit 1
    fi
    grep -Fxq -- 'blocking disposal message end' <<< "$actual" \
        || { echo "FAIL: blocking disposal message witness missing" >&2; exit 1; }
}

corelib_diff_gate BlockingCollectionSubset System.Collections.Concurrent
