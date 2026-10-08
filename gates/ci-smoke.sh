#!/usr/bin/env bash
# CI profiles share the fast bootstrap; fixed primitive partitions cover the
# same allowlist without extending a hosted job budget. The suite is unchanged.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd -P)"
PROFILE=""
PARTITION=""
LIST=0
SELF_TEST=0
usage() {
    echo "usage: $0 --profile primitives|suite [--partition core|limits] [--list] | --self-test" >&2
    exit 2
}
while [ "$#" -gt 0 ]; do
    case "$1" in
        --profile)
            [ "$#" -ge 2 ] && [ -z "$PROFILE" ] || usage
            PROFILE="$2"; shift 2 ;;
        --partition)
            [ "$#" -ge 2 ] && [ -z "$PARTITION" ] || usage
            case "$2" in core|limits) PARTITION="$2" ;; *) usage ;; esac
            shift 2 ;;
        --list) [ "$LIST" -eq 0 ] || usage; LIST=1; shift ;;
        --self-test) [ "$SELF_TEST" -eq 0 ] || usage; SELF_TEST=1; shift ;;
        *) usage ;;
    esac
done

PRIMITIVES=(
    sample multiassembly lang-versions
    boxing-primitives convert-parse enum-ops decimal-ops math-subset
    array-core string-core span-ops unsafe-general list-collections dict-collections
    shared-generics async-core async-combinators custom-async-task thread-spawn
    gc-write-barrier external-ref-barrier
    file-real filesystem-enum env-subset threading-primitives sync-primitives
    thread-local timer mmap-file pinvoke-native
    console-io datetime threadpool transpiler-limits
)

self_test() (
    local fixture name case_name rc expected test_os success_case partition label failure_gate
    local full core limits combined logdir
    local runner_args
    fixture=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp-ci-smoke-test.XXXXXX")
    trap 'rm -rf "$fixture"' EXIT
    mkdir -p "$fixture/gates" "$fixture/bin" "$fixture/runtime/10.0.0"
    cp "$SCRIPT_DIR/ci-smoke.sh" "$SCRIPT_DIR/_common.sh" "$fixture/gates/"
    touch "$fixture/runtime/10.0.0/System.Private.CoreLib.dll"
    printf '#!/usr/bin/env bash\n[ "$1" = 65001 ]\n' > "$fixture/bin/chcp.com"
    chmod +x "$fixture/bin/chcp.com"
    cat > "$fixture/bin/dotnet" <<'DOTNET'
#!/usr/bin/env bash
set -euo pipefail
[ "$CONFIG" = "$CI_TEST_CONFIG" ]
[ "$DN2CPP_GATE_CACHE" = 0 ] && [ "$DN2CPP_REQUIRE_ALL" = 1 ]
[ "${DN2CPP_SKIP_BUILD+x}" != x ]
case "$1" in
    build)
        printf 'build\n' >> "$CI_TEST_ORDER"
        case "$CI_TEST_CASE" in build-fail|windows-build-fail) exit 9 ;; esac
        mkdir -p "$(dirname "$DN2CPP_CLI_DLL")"
        rm -f "$DN2CPP_CLI_DLL"
        [ "$CI_TEST_CASE" = missing-cli ] || touch "$DN2CPP_CLI_DLL"
        ;;
    build-server)
        [ "$2" = shutdown ]
        printf 'shutdown\n' >> "$CI_TEST_ORDER"
        [ "$CI_TEST_CASE" != windows-build-fail ] || exit 8
        ;;
    --list-runtimes)
        printf 'corelib\n' >> "$CI_TEST_ORDER"
        [ "$CI_TEST_CASE" != missing-corelib ] || exit 0
        printf 'Microsoft.NETCore.App 10.0.0 [%s/runtime]\n' "$PWD"
        ;;
    */HelloWorld.dll)
        if [ "$CI_TEST_CASE" = sample-crlf ]; then
            printf 'sample output: 日本語\r\n'
        else
            printf 'sample output: 日本語\n'
        fi
        [ "$CI_TEST_CASE" != sample-status ] || exit 4
        ;;
    *) exit 8 ;;
esac
DOTNET
    chmod +x "$fixture/bin/dotnet"
    cat > "$fixture/gates/stub.sh" <<'GATE'
#!/usr/bin/env bash
set -euo pipefail
[ "$CONFIG" = "$CI_TEST_CONFIG" ]
[ "$DN2CPP_GATE_CACHE" = 0 ] && [ "$DN2CPP_REQUIRE_ALL" = 1 ]
[ "${DN2CPP_SKIP_BUILD+x}" != x ]
[ -f "$DN2CPP_CLI_DLL" ] && [ -f "$DN2CPP_CORELIB" ]
name=${0##*/}; name=${name#build-and-run-}; name=${name%.sh}
printf '%s\n' "$name" >> "$CI_TEST_ORDER"
if [ "$name" = "${CI_TEST_FAILURE_GATE:-multiassembly}" ]; then
    case "$CI_TEST_CASE" in
        fail) echo 'fixture failure'; exit 3 ;;
        exit77) exit 77 ;;
        skip) echo 'SKIP: fixture' ;;
        partial) echo 'PARTIAL: fixture' ;;
        expected-partial) echo 'EXPECTED PARTIAL: fixture' ;;
        cached) echo 'OK (cached green: fixture)' ;;
        cached-partial) echo 'OK (cached partial: fixture)' ;;
        watchdog) sleep 30 ;;
    esac
fi
if [ "$name" = sample ]; then
    mkdir -p "samples/dotnet/HelloWorld/bin/$CONFIG/$TFM" artifacts/console
    touch "samples/dotnet/HelloWorld/bin/$CONFIG/$TFM/HelloWorld.dll"
    cat > "artifacts/console/HelloWorld$EXE_EXT" <<'NATIVE'
#!/usr/bin/env bash
if [ "$CI_TEST_CASE" = sample-output ]; then
    printf 'different output\n'
else
    printf 'sample output: 日本語\n'
fi
NATIVE
    chmod +x "artifacts/console/HelloWorld$EXE_EXT"
fi
GATE
    for name in "${PRIMITIVES[@]}"; do
        cp "$fixture/gates/stub.sh" "$fixture/gates/build-and-run-$name.sh"
    done
    cat > "$fixture/gates/run-all-gates.sh" <<'SUITE'
#!/usr/bin/env bash
set -euo pipefail
[ "$SKIP_GODOT" = 1 ]
[ "$CONFIG" = Sentinel ] && [ "$DN2CPP_GATE_CACHE" = Sentinel ]
[ "$DN2CPP_REQUIRE_ALL" = Sentinel ] && [ "$DN2CPP_SKIP_BUILD" = Sentinel ]
printf 'suite forwarded\n'
exit 23
SUITE

    full=$(bash "$fixture/gates/ci-smoke.sh" --profile primitives --list)
    core=$(bash "$fixture/gates/ci-smoke.sh" --profile primitives --partition core --list)
    limits=$(bash "$fixture/gates/ci-smoke.sh" --profile primitives --partition limits --list)
    [ "$full" = "$(printf '%s\n' "${PRIMITIVES[@]}")" ]
    [ "$limits" = transpiler-limits ]
    [ "$core" = "$(sed '/^transpiler-limits$/d' <<< "$full")" ]
    combined=$(printf '%s\n%s\n' "$core" "$limits" | LC_ALL=C sort)
    [ "$combined" = "$(LC_ALL=C sort <<< "$full")" ]
    [ -z "$(uniq -d <<< "$combined")" ]
    echo 'OK: primitive partitions are disjoint and cover the exact full allowlist'

    for partition in '' core limits; do
        runner_args=(--profile primitives)
        label=${partition:-full}
        [ -z "$partition" ] || runner_args+=(--partition "$partition")
        for test_os in linux windows; do
            success_case=success
            [ "$test_os" != windows ] || success_case=sample-crlf
            expected=$(bash "$fixture/gates/ci-smoke.sh" "${runner_args[@]}" --list)
            logdir="$fixture/logs-$test_os-$label"
            rc=0
            PATH="$fixture/bin:$PATH" CONFIG=Sentinel DN2CPP_SKIP_BUILD=1 \
                DN2CPP_GATE_CACHE=1 DN2CPP_REQUIRE_ALL=0 DN2CPP_CORELIB= DN2CPP_OS="$test_os" \
                CMAKE_CXX_COMPILER=g++ CI_TEST_CONFIG=Debug \
                CI_TEST_CASE="$success_case" CI_TEST_ORDER="$fixture/order" LOGDIR="$logdir" \
                bash "$fixture/gates/ci-smoke.sh" "${runner_args[@]}" \
                > "$fixture/output" 2>&1 || rc=$?
            [ "$rc" -eq 0 ] || { cat "$fixture/output" >&2; exit 1; }
            if [ "$test_os" = windows ]; then
                expected=$(printf 'build\nshutdown\ncorelib\n%s\n' "$expected")
            else
                expected=$(printf 'build\ncorelib\n%s\n' "$expected")
            fi
            [ "$(cat "$fixture/order")" = "$expected" ]
            if [ "$partition" != limits ]; then
                [ -s "$logdir/sample-dotnet.stdout" ]
                [ -s "$logdir/sample-native.stdout" ]
                if [ "$test_os" = windows ]; then
                    ! cmp -s "$logdir/sample-dotnet.raw.stdout" "$logdir/sample-native.raw.stdout"
                fi
            else
                [ ! -e "$logdir/sample.log" ]
            fi
            [ -s "$logdir/summary.md" ] && [ ! -s "$logdir/_failures.txt" ]
            rm -f "$fixture/order"
            echo "OK: $test_os $label uses strict Debug/bootstrap and exactly its selected gates"
        done

        failure_gate=multiassembly
        [ "$partition" != limits ] || failure_gate=transpiler-limits
        for case_name in fail exit77 skip partial expected-partial cached cached-partial \
                sample-output sample-status build-fail windows-build-fail missing-cli missing-corelib watchdog missing-gate; do
            case "$partition:$case_name" in limits:sample-output|limits:sample-status) continue ;; esac
            rm -f "$fixture/order"
            test_os=linux
            [ "$case_name" != windows-build-fail ] || test_os=windows
            logdir="$fixture/logs-$label-$case_name"
            if [ "$case_name" = missing-gate ]; then
                rm "$fixture/gates/build-and-run-$failure_gate.sh"
            fi
            rc=0
            PATH="$fixture/bin:$PATH" DN2CPP_CORELIB= DN2CPP_OS="$test_os" CI_TEST_CONFIG=Debug \
                CMAKE_CXX_COMPILER=g++ CI_TEST_FAILURE_GATE="$failure_gate" \
                CI_TEST_CASE="$case_name" CI_TEST_ORDER="$fixture/order" \
                DN2CPP_GATE_WATCHDOG_SECS=$([ "$case_name" = watchdog ] && echo 1 || echo 30) \
                LOGDIR="$logdir" \
                bash "$fixture/gates/ci-smoke.sh" "${runner_args[@]}" \
                > "$fixture/output" 2>&1 || rc=$?
            [ "$rc" -ne 0 ] || { echo "FAIL: self-test accepted $label $case_name" >&2; exit 1; }
            [ -s "$logdir/_failures.txt" ] && [ -s "$logdir/summary.md" ]
            case "$case_name" in
                build-fail|windows-build-fail|missing-cli|missing-corelib|missing-gate)
                    [ -s "$logdir/bootstrap.log" ]
                    [ ! -e "$logdir/$failure_gate.log" ] ;;
                sample-output|sample-status)
                    [ -s "$logdir/sample.log" ]
                    [ ! -e "$logdir/multiassembly.log" ] ;;
                *)
                    [ -s "$logdir/$failure_gate.log" ]
                    [ ! -e "$logdir/lang-versions.log" ] ;;
            esac
            if [ "$case_name" = watchdog ]; then
                grep -q '^WATCHDOG:' "$logdir/$failure_gate.log"
            fi
            if [ "$case_name" = windows-build-fail ]; then
                grep -q '^FAIL: bootstrap exited 9$' "$logdir/bootstrap.log"
                [ "$(cat "$fixture/order")" = "$(printf 'build\nshutdown\n')" ]
            fi
            if [ "$case_name" = missing-gate ]; then
                cp "$fixture/gates/stub.sh" "$fixture/gates/build-and-run-$failure_gate.sh"
            fi
            echo "OK: $label rejects $case_name and records a failed step"
        done
    done
    for name in '' '--profile unknown' '--profile console' '--profile' '--list' \
            '--profile primitives --profile primitives' '--profile suite --list' \
            '--self-test --profile primitives' '--partition' '--partition other' \
            '--partition limits --partition core' '--partition limits --list' \
            '--profile suite --partition limits' '--self-test --partition core'; do
        rc=0
        bash "$fixture/gates/ci-smoke.sh" $name > "$fixture/output" 2>&1 || rc=$?
        [ "$rc" -eq 2 ]
    done
    rc=0
    CONFIG=Sentinel DN2CPP_GATE_CACHE=Sentinel DN2CPP_REQUIRE_ALL=Sentinel \
        DN2CPP_SKIP_BUILD=Sentinel bash "$fixture/gates/ci-smoke.sh" --profile suite \
        > "$fixture/output" 2>&1 || rc=$?
    [ "$rc" -eq 23 ] && grep -qx 'suite forwarded' "$fixture/output"
    echo 'OK: CI smoke runner self-test'
)

if [ "$SELF_TEST" -eq 1 ]; then
    [ -z "$PROFILE" ] && [ -z "$PARTITION" ] && [ "$LIST" -eq 0 ] || usage
    self_test
    exit 0
fi
case "$PROFILE" in
    primitives)
        GATES=()
        for name in "${PRIMITIVES[@]}"; do
            case "$PARTITION:$name" in
                core:transpiler-limits) continue ;;
                limits:*) [ "$name" = transpiler-limits ] || continue ;;
            esac
            GATES+=("$name")
        done
        CONFIG=Debug ;;
    suite)
        [ "$LIST" -eq 0 ] && [ -z "$PARTITION" ] || usage
        export SKIP_GODOT=1
        exec bash "$SCRIPT_DIR/run-all-gates.sh" ;;
    *) usage ;;
esac
if [ "$LIST" -eq 1 ]; then
    printf '%s\n' "${GATES[@]}"
    exit 0
fi

source "$SCRIPT_DIR/_common.sh"
export CONFIG TFM EXE_EXT DN2CPP_GATE_CACHE=0 DN2CPP_REQUIRE_ALL=1
unset DN2CPP_SKIP_BUILD
export DN2CPP_CLI_DLL="$PWD/src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll"
LOGDIR=${LOGDIR:-/tmp/dn2cpp-ci-smoke}
mkdir -p "$LOGDIR"
LOGDIR="$(cd "$LOGDIR" && pwd -P)"
: > "$LOGDIR/_timings.txt"
: > "$LOGDIR/_failures.txt"
rm -f "$LOGDIR/bootstrap.log" "$LOGDIR/summary.md" "$LOGDIR/_corelib.txt" \
    "$LOGDIR"/sample-dotnet.* "$LOGDIR"/sample-native.*
for name in "${GATES[@]}"; do rm -f "$LOGDIR/$name.log"; done
START=$SECONDS

summary() {
    local name seconds verdict
    {
        printf '# CI smoke: %s\n\n' "$PROFILE"
        printf 'Configuration: %s; elapsed: %ss. Logs: `%s`.\n\n' \
            "$CONFIG" "$((SECONDS - START))" "$LOGDIR"
        [ -z "$PARTITION" ] || printf 'Partition: `%s`.\n\n' "$PARTITION"
        printf '| Step | Seconds | Result |\n| --- | ---: | --- |\n'
        while read -r name seconds verdict; do
            printf '| %s | %s | %s |\n' "$name" "$seconds" "$verdict"
        done < "$LOGDIR/_timings.txt"
    } > "$LOGDIR/summary.md"
}

run_step() {
    local name="$1" start=$SECONDS rc=0
    shift
    run_with_watchdog "${DN2CPP_GATE_WATCHDOG_SECS:-600}" "$@" \
        > "$LOGDIR/$name.log" 2>&1 || rc=$?
    if [ "$rc" -eq 0 ] && grep -Eq \
            '^(SKIP|PARTIAL:|EXPECTED PARTIAL:)|cached (green|partial)' "$LOGDIR/$name.log"; then
        echo 'FAIL: CI smoke requires uncached, complete execution' >> "$LOGDIR/$name.log"
        rc=1
    fi
    if [ "$rc" -ne 0 ]; then
        printf 'FAIL: %s exited %s\n' "$name" "$rc" >> "$LOGDIR/$name.log"
        printf '%s %s failed\n' "$name" "$((SECONDS - start))" >> "$LOGDIR/_timings.txt"
        printf '%s\n' "$name" >> "$LOGDIR/_failures.txt"
        cat "$LOGDIR/$name.log" >&2
        printf 'FAIL: %s (exit %s); log: %s/%s.log\n' "$name" "$rc" "$LOGDIR" "$name" >&2
        summary
        return 1
    fi
    printf '%s %s ran\n' "$name" "$((SECONDS - start))" >> "$LOGDIR/_timings.txt"
    printf 'PASS: %-24s %ss\n' "$name" "$((SECONDS - start))"
    return 0
}

bootstrap() {
    local name corelib build_rc=0
    for name in "${GATES[@]}"; do
        [ -f "$SCRIPT_DIR/build-and-run-$name.sh" ] || {
            echo "FAIL: missing gate: $name" >&2
            return 1
        }
    done
    build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj || build_rc=$?
    if [ "$DN2CPP_OS" = windows ]; then dotnet build-server shutdown || true; fi
    [ "$build_rc" -eq 0 ] || return "$build_rc"
    [ -f "$DN2CPP_CLI_DLL" ] || { echo "FAIL: missing CLI: $DN2CPP_CLI_DLL" >&2; return 1; }
    corelib=$(locate_corelib) || return $?
    [ -f "$corelib" ] || { echo "FAIL: missing CoreLib: $corelib" >&2; return 1; }
    printf '%s\n' "$corelib" > "$LOGDIR/_corelib.txt"
    return 0
}

sample_parity() {
    local app="samples/dotnet/HelloWorld/bin/$CONFIG/$TFM/HelloWorld.dll"
    local native="artifacts/console/HelloWorld$EXE_EXT" dotnet_rc=0 native_rc=0
    [ -f "$app" ] && [ -x "$native" ] || {
        echo 'FAIL: sample parity requires the managed and native binaries' >&2
        return 1
    }
    run_bounded dotnet "$app" > "$LOGDIR/sample-dotnet.raw.stdout" \
        2> "$LOGDIR/sample-dotnet.stderr" || dotnet_rc=$?
    run_bounded "./$native" > "$LOGDIR/sample-native.raw.stdout" \
        2> "$LOGDIR/sample-native.stderr" || native_rc=$?
    printf '%s\n' "$dotnet_rc" > "$LOGDIR/sample-dotnet.exit"
    printf '%s\n' "$native_rc" > "$LOGDIR/sample-native.exit"
    if [ "$dotnet_rc" -ne "$native_rc" ] || [ "$dotnet_rc" -ne 0 ]; then
        cat "$LOGDIR/sample-dotnet.stderr" "$LOGDIR/sample-native.stderr" >&2
        echo "FAIL: sample exit status: .NET=$dotnet_rc native=$native_rc" >&2
        return 1
    fi
    strip_cr_win_file "$LOGDIR/sample-dotnet.raw.stdout" > "$LOGDIR/sample-dotnet.stdout" || return $?
    strip_cr_win_file "$LOGDIR/sample-native.raw.stdout" > "$LOGDIR/sample-native.stdout" || return $?
    if ! cmp -s "$LOGDIR/sample-dotnet.stdout" "$LOGDIR/sample-native.stdout"; then
        diff -u "$LOGDIR/sample-dotnet.stdout" "$LOGDIR/sample-native.stdout" >&2 || true
        echo 'FAIL: sample stdout differs from .NET' >&2
        return 1
    fi
    echo 'OK: sample .NET/native stdout and exit status match'
    return 0
}

run_fast_gate() {
    bash "$SCRIPT_DIR/build-and-run-$1.sh" || return $?
    if [ "$1" = sample ]; then sample_parity || return $?; fi
    return 0
}

run_step bootstrap bootstrap || exit 1
DN2CPP_CORELIB=$(cat "$LOGDIR/_corelib.txt")
export DN2CPP_CORELIB
for name in "${GATES[@]}"; do
    run_step "$name" run_fast_gate "$name" || exit 1
done
summary
printf 'PASS: %s profile; %s gates; %ss; logs: %s\n' \
    "$PROFILE" "${#GATES[@]}" "$((SECONDS - START))" "$LOGDIR"
