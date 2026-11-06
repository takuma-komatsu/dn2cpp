#!/usr/bin/env python3
"""SDK-free selection, binary reuse and failure checks for the short gates."""
import json
import os
from pathlib import Path
import subprocess
import tempfile


GATES = Path(__file__).resolve().parent
BASH = os.environ.get("DN2CPP_TEST_BASH", "bash")
ENV = {key: value for key, value in os.environ.items()
       if not key.startswith(("DN2CPP_", "GIT_")) and key not in ("HIGHWAY", "SCALAR", "IOS_SIM", "IOS_DEV")}
root = Path(tempfile.mkdtemp(prefix="dn2cpp-short-gates-"))
checks = 0
results = []


def check(condition, message):
    global checks
    if not condition:
        raise AssertionError((message, "evidence", str(root)))
    checks += 1


common = (GATES / "_common.sh").read_text(encoding="utf-8")
wrappers = common[common.index("_GATE_SCRATCH_DIRS=()"):
                  common.index("# wasm_corelib_diff_gate PROJECT")]
xasm = common[common.index("xasm_gate() {"):
              common.index("# ── Watchdogs")]
stubs = r'''
set -euo pipefail
CONFIG=Debug
TFM=net10.0
EXE_EXT=
DN2CPP_OS=linux
record() { printf '%s\n' "$*" >> "$FAKE_LOG"; }
locate_corelib() { printf '%s\n' "$PWD/runtime/System.Private.CoreLib.dll"; }
resolve_net10_corelib() { locate_corelib; }
_gate_cli_hash() { printf 'fake-cli'; }
prepare_cli() {
    if [ -z "${DN2CPP_CLI_DLL:-}" ]; then
        record PREPARE
        DN2CPP_CLI_DLL="$PWD/cli/dn2cpp.dll"
        export DN2CPP_CLI_DLL
        mkdir -p cli
        : > cli/dn2cpp.dll
        : > cli/Dn2Cpp.Runtime.dll
    fi
}
fake_build() {
    local dir="$(dirname "$1")/bin/$CONFIG/$TFM" name="$(basename "$1" .csproj)"
    record "BUILD:$1"
    mkdir -p "$dir"
    : > "$dir/$name.dll"
    : > "$dir/$name.runtimeconfig.json"
    : > "$dir/$name.deps.json"
    : > "$dir/CustomAsyncTaskLib.dll"
    : > "$dir/MiniCorlib.dll"
    : > "$dir/MultiAssemblyAlias.dll"
    [ "$name" != Dn2Cpp.Cli.Console ] || : > "$dir/dn2cpp-console.dll"
}
build_proj() { [ -z "${DN2CPP_SKIP_BUILD:-}" ] || return 0; fake_build "$1"; }
build_gate_proj() { fake_build "$1"; }
gate_python() { record "PYTHON:$*"; }
gate_skip() { echo "unexpected skip: $*" >&2; exit 99; }
invoke_cli() {
    prepare_cli
    local args="$*" app="$1" dir= arg symbol storage
    while [ "$#" -gt 0 ]; do
        if [ "$1" = -o ]; then dir="$2"; break; fi
        shift
    done
    record "EMIT:$dir:$args"
    if [ -n "${FAKE_EMIT_FAIL:-}" ] && [[ "$dir" == *"$FAKE_EMIT_FAIL"* ]]; then return 29; fi
    mkdir -p "$dir"
    case "$dir" in
        *-conflicting) echo 'Conflicting --reflection-metadata formats for shared generic definition'; return 2 ;;
        *-ambiguous) echo 'Ambiguous --reflection-metadata selector'; return 2 ;;
    esac
    : > "$dir/generated.cpp"
    : > "$dir/generated.h"
    if [[ "$app" == *CustomAsyncTask* ]]; then
        cat > "$dir/generated.h" <<'HEADER'
void Program_CompletedRef_TisCnRef_m1();
void Program_CompletedRef_TisString_m1()
{ rgctx_Program_CompletedRef_TisString_m1; }
void Program_CompletedRef_TisObject_m1()
{ rgctx_Program_CompletedRef_TisObject_m1; }
HEADER
        case "$dir" in
            *-real|*-exceed)
                for symbol in TaskPool Runner ITaskPoolNode AsyncCustomTaskMethodBuilder CustomTaskScheduler; do
                    echo "// CustomAsyncTaskLib.$symbol::Body" >> "$dir/generated.cpp"
                done ;;
            *-shared) echo '// SharedRoleSubset.SharedAwaiterTaskBuilder::Body' >> "$dir/generated.cpp" ;;
        esac
        echo 'inst 1 minst 1' >&2
        case "$dir" in
            *-exceed) echo 'adoption declined: CustomAsyncTaskLib.CustomTask::Yield' >&2 ;;
            *-shape) echo 'adoption declined: CustomAsyncTaskLib.CustomTask::GetAwaiter' >&2 ;;
            *-shared)
                echo "Awaiter::Probe transpiling CustomAsyncTaskLib's real IL" >&2
                echo "Awaiter::Probe transpiling CustomAsyncTaskTypeSharedExceed's real IL" >&2 ;;
        esac
    elif [[ "$app" == *MultiAssembly.dll ]]; then
        storage=native
        [[ "$dir" != *-first-packed && "$dir" != *-combined-packed ]] || storage=record
        echo "md_${storage}_refl_gendef_MetadataAssemblyCollision_Subject_1" >> "$dir/generated.cpp"
        for symbol in ti_MultiAssembly_MetadataExternalPolicy ti_MiniBcl_MetadataMiddle \
                gendef_MetadataCompressionCollision_SharedSubject_1; do
            storage=native
            [[ "$dir" != *-policy-packed && "$dir" != *-combined-packed ]] || storage=record
            echo "md_${storage}_refl_$symbol" >> "$dir/generated.cpp"
        done
        for symbol in ti_MultiAssembly_MetadataDerived ti_MetadataCompressionCollision_MarkedSubject \
                ti_MultiAssembly_MetadataScopeMarked ti_m2_MetadataCompressionCollision_SharedSubject_String; do
            echo "md_native_refl_$symbol" >> "$dir/generated.cpp"
        done
        for symbol in ti_MetadataCompressionCollision_UnmarkedSubject ti_MultiAssembly_MetadataScopeUnmarked \
                ti_m1_MetadataCompressionCollision_SharedSubject_Int32; do
            echo "md_record_refl_$symbol" >> "$dir/generated.cpp"
        done
        for symbol in 0 2; do
            for arg in Int32 String; do
                echo "ti_m${symbol}_AssemblyScopeCollision_Generic_$arg" >> "$dir/generated.h"
            done
        done
        cat >> "$dir/generated.h" <<'HEADER'
struct t_MiniBcl_LayoutBase__CnRef : Dn2CppObject
struct t_MiniBcl_LayoutBeh__CnRef
struct t_MiniBcl_LayoutLeaf
struct t_MiniBcl_LayoutAligned
t_MiniBcl_LayoutAligned f__aligned
struct alignas(8) t_MiniBcl_LayoutAligned
HEADER
    else
        printf '%s\n' 'const int32_t dn2cpp_type_bind_count = 0;' \
            'const Dn2CppTypeBind dn2cpp_type_binds[] = {};' >> "$dir/generated.cpp"
        echo 'vt_ExceptionMessageSubset_ExceptionVirtualMembers_ChangingMessage[] ChangingMessage_get_Message_m1' >> "$dir/generated.cpp"
    fi
    echo 'dn2cpp: 4 assemblies, mock'
}
compile_console() { record "NATIVE:$1:$2"; mkdir -p "$1"; }
run_bounded() {
    if [ "$1" = invoke_cli ]; then "$@"; else record "RUN:$*"; fi
}
gate_capture() {
    local prefix="$1" status=0
    shift
    record "CAPTURE:$prefix:$*"
    mkdir -p "$(dirname "$prefix")"
    [[ "$prefix" != *suspension-* ]] || status=134
    if [ -n "${FAKE_STATUS_FAIL:-}" ] && [[ "$prefix" == *"$FAKE_STATUS_FAIL"* ]]; then status=23; fi
    if [ -n "${FAKE_SNAPSHOT:-}" ]; then
        cp "$FAKE_SNAPSHOT" "$prefix.stdout"
        [ "${FAKE_TAINT:-0}" = 0 ] || printf '\nwrong\n' >> "$prefix.stdout"
    else
        printf 'mock output\n' > "$prefix.stdout"
    fi
    : > "$prefix.stderr"
    printf '%s\n' "$status" > "$prefix.status"
}
gate_capture_ok() {
    gate_capture "$@"
    [ "$(cat "$1.status")" = 0 ]
}
gate_output() {
    record "ASSERT:$*"
    if [ -n "${FAKE_ASSERT_FAIL:-}" ] && [[ "$*" == *"$FAKE_ASSERT_FAIL"* ]]; then return 31; fi
    if [ "$1" = status ]; then [ "$2" = "$3" ]; fi
    if [ "$1" = view ]; then
        printf '%s\n' 'enumerable snapshot count=2 first=True order=True' \
            'enumerable snapshot count=2 first=True order=True' \
            'enumerable snapshot count=2 first=True order=True' \
            'enumerable snapshot count=2 first=True order=True' \
            'enumerable snapshot count=2 first=True order=True' > "$3"
    fi
}
gate_cache_check() { record "CACHE:$*"; [ "${FAKE_CACHE_HIT:-0}" = 1 ]; }
gate_cache_hit_msg() { record HIT; }
gate_cache_commit() { record COMMIT; }
_gate_run_termination() { :; }
'''


def run(name, script, extra=None):
    directory = root / name
    directory.mkdir()
    (directory / "runtime").mkdir()
    for filename in ("System.Private.CoreLib.dll", "System.Collections.Concurrent.dll"):
        (directory / "runtime" / filename).touch()
    log = directory / "calls.log"
    command = stubs + wrappers + xasm + script
    (directory / "script.sh").write_text(command, encoding="utf-8")
    result = subprocess.run([BASH, "script.sh"], cwd=directory,
                            env={**ENV, "FAKE_LOG": str(log), **(extra or {})},
                            capture_output=True, text=True, encoding="utf-8")
    (directory / "stdout.log").write_text(result.stdout, encoding="utf-8")
    (directory / "stderr.log").write_text(result.stderr, encoding="utf-8")
    (directory / "status").write_text(str(result.returncode) + "\n", encoding="utf-8")
    calls = log.read_text(encoding="utf-8").splitlines() if log.exists() else []
    results.append({"name": name, "status": result.returncode,
                    "native": sum(line.startswith("NATIVE:") for line in calls),
                    "emit": sum(line.startswith("EMIT:") for line in calls)})
    return result, calls


def source(gate):
    script = (GATES / ("build-and-run-" + gate + ".sh")).read_text(encoding="utf-8")
    return script.replace('source "$(dirname "$0")/_common.sh"', "", 1)


profiles = [
    ("custom-async-task", "DN2CPP_CUSTOM_ASYNC_TASK_FULL", (3, 3), (5, 5)),
    ("exception-message-subset", "DN2CPP_EXCEPTION_MESSAGE_FULL", (3, 3), (5, 5)),
    ("multiassembly", "DN2CPP_MULTIASSEMBLY_FULL", (3, 5), (4, 9)),
]
for gate, selector, normal, full in profiles:
    script = source(gate)
    for label, value, expected in (("default", "0", normal), ("0", "0", normal), ("1", "1", full)):
        result, calls = run(gate + "-" + label, script, {} if label == "default" else {selector: value})
        check(result.returncode == 0, (gate, value, result.stderr))
        native = [line for line in calls if line.startswith("NATIVE:")]
        emit = [line for line in calls if line.startswith("EMIT:")]
        check((len(native), len(emit)) == expected, (gate, value, native, emit))
        check(("skipped (not selected); not counted as passed" in result.stdout) == (value == "0"), gate)
        cache = [line for line in calls if line.startswith("CACHE:")]
        check(cache and any(("full:" + value) in line for line in cache), (gate, value, cache))
        check(sum(line == "PREPARE" for line in calls) == 1, (gate, calls))
        if gate == "custom-async-task":
            check(sum(line.startswith("ASSERT:status ") for line in calls) == expected[0], calls)
            check(any("--no-adopt-async CustomAsyncTaskLib" in line for line in emit), emit)
            check(any("customasynctask-exceed:" in line for line in emit), emit)
            if value == "0":
                check(all("ShapeExceed" not in line and "SharedExceed" not in line for line in cache), cache)
        elif gate == "exception-message-subset":
            check(any("RuntimeArgumentFields:" in line for line in native), native)
            check(any("RuntimeArgumentFallback:" in line for line in native), native)
            check(any("runtime exception ancestry end" in line for line in calls), calls)
            if value == "0":
                check(all("RuntimeExceptionAncestry.dll" not in line and "GeneralArgumentFallback.dll" not in line
                          for line in cache), cache)
        else:
            check(sum(line.startswith("BUILD:") for line in calls) == 1, calls)
            for target in ("multiasm/enum-full", "multiasm-inference/enum-full"):
                check(any(target in line for line in calls if line.startswith("CAPTURE:")), target)
            if value == "0":
                combined = [line for line in emit if "combined-packed:" in line]
                check(len(combined) == 1 and combined[0].count("--reflection-metadata") == 4, combined)
            else:
                check(any("multiasm-shimcopy." in line for line in emit), emit)
    for value in ("true", "2", " "):
        result, calls = run(gate + "-invalid-" + repr(value), script, {selector: value})
        check(result.returncode != 0 and selector + " must be 0 or 1" in result.stderr, (gate, result.stderr))
        check(not calls, (gate, calls))

for gate, extra in (
        ("custom-async-task", {"FAKE_STATUS_FAIL": "customasynctask-exceed/native"}),
        ("custom-async-task", {"FAKE_EMIT_FAIL": "customasynctask-real"}),
        ("custom-async-task", {"DN2CPP_CUSTOM_ASYNC_TASK_FULL": "1", "FAKE_STATUS_FAIL": "customasynctask-shared/native"}),
        ("custom-async-task", {"DN2CPP_CUSTOM_ASYNC_TASK_FULL": "1", "FAKE_EMIT_FAIL": "customasynctask-shape"}),
        ("exception-message-subset", {"FAKE_STATUS_FAIL": "RuntimeArgumentFallback/assert-actual-19"}),
        ("exception-message-subset", {"FAKE_ASSERT_FAIL": "bound Message fallback end"}),
        ("exception-message-subset", {"DN2CPP_EXCEPTION_MESSAGE_FULL": "1", "FAKE_STATUS_FAIL": "assert-actual-14"}),
        ("exception-message-subset", {"DN2CPP_EXCEPTION_MESSAGE_FULL": "1", "FAKE_ASSERT_FAIL": "culture Message fallback end"}),
        ("multiassembly", {"FAKE_ASSERT_FAIL": "combined-packed/metadata-assembly.raw.stdout"}),
        ("multiassembly", {"FAKE_EMIT_FAIL": "metadata-conflicting"})):
    result, calls = run(gate + "-failure-" + str(len(results)), source(gate), extra)
    check(result.returncode != 0, (gate, extra))
    if gate != "multiassembly":
        check("COMMIT" not in calls, (gate, extra, calls))

async_script = source("async-combinators")
check("corelib_freeze_gate" not in async_script, "async must reuse the aggregate")
for label, extra, expected in (
        ("cold", {}, 0), ("suffix", {"DN2CPP_OUT_SUFFIX": "-probe", "HIGHWAY": "1"}, 0),
        ("status", {"FAKE_STATUS_FAIL": "native-deadlines"}, 1),
        ("compare", {"FAKE_ASSERT_FAIL": "native-deadlines.stdout"}, 31),
        ("warm", {"FAKE_CACHE_HIT": "1"}, 0)):
    result, calls = run("async-" + label, async_script, extra)
    check(result.returncode == expected, (label, result.returncode, result.stderr))
    native = [line for line in calls if line.startswith("NATIVE:")]
    check(len(native) == (0 if label == "warm" else 1 if expected else 2), (label, native))
    cache = next(line for line in calls if line.startswith("CACHE:"))
    check("async-delay-deadlines.txt" in cache and "shared-binary:native-delay-deadlines" in cache, cache)
    if label == "warm":
        check("HIT" in calls and not any(line.startswith("CAPTURE:") for line in calls), calls)
    elif expected:
        check("COMMIT" not in calls, calls)
    else:
        capture = next(line for line in calls if line.startswith("CAPTURE:") and "native-delay-deadlines" in line)
        target = native[0].split(":")[1]
        check(f"./{target}/AsyncCombinators native-delay-deadlines" in capture, capture)
        check(calls.index(capture) < calls.index("COMMIT"), calls)

culture = source("default-culture")
check("corelib_freeze_gate" not in culture, "culture must reuse the aggregate")
check(culture.count('\n_corelib_gate_core "$PROJECT" "$out"') == 1, "culture emits once")
check(culture.count('\ncompile_console "$out" "$PROJECT"') == 1, "culture compiles once")
check("shared-binary:culture-zz-currency" in culture, "culture cache context includes frozen run")
frozen = culture[culture.index('echo "-- frozen unknown-region currency:') :]
for platform in ("posix", "windows"):
    snapshot = GATES / "expected" / ("default-culture-zz-" + platform + ".txt")
    for label, extra in (("ok", {}), ("status", {"FAKE_STATUS_FAIL": "zz-frozen"}),
                         ("bytes", {"FAKE_TAINT": "1"})):
        script = '\nPROJECT=DefaultCulture\nout=artifacts/defaultculture\n'
        script += "zz_snapshot=" + repr(str(snapshot)) + "\n"
        script += 'gate_output() { record "ASSERT:$*"; "$FAKE_PYTHON" "$FAKE_OUTPUT_HELPER" "$@"; }\n'
        result, calls = run("culture-" + platform + "-" + label, script + frozen,
                            {"FAKE_SNAPSHOT": str(snapshot), "FAKE_PYTHON": os.sys.executable,
                             "FAKE_OUTPUT_HELPER": str(GATES / "gate-output.py"), **extra})
        check((result.returncode == 0) == (label == "ok"), (platform, label, result.stderr))
        check(("COMMIT" in calls) == (label == "ok"), calls)
        capture = next(line for line in calls if line.startswith("CAPTURE:"))
        check("env LC_ALL=en_US.UTF-8 LANG=en_US.UTF-8 ./artifacts/defaultculture/DefaultCulture culture-zz-currency" in capture,
              capture)

cli_stubs = r'''
fake_frontend() {
    local frontend="$1" args dir= jobs= no_metadata=0 rejected= token
    shift
    args="$*"
    while [ "$#" -gt 0 ]; do
        token="$1"; shift
        case "$token" in
            -o) dir="$1"; shift ;;
            -r) shift ;;
            --jobs)
                jobs="${1:-}"
                case "$jobs" in
                    1|2) shift ;;
                    '') rejected=--jobs ;;
                    *) rejected="$jobs"; shift ;;
                esac ;;
            --no-default-ref)
                [ "$1" = DnZlib ] || rejected="$1"
                shift ;;
            --no-metadata-compression) no_metadata=1 ;;
            --bogus-flag|--tirm-reflection|extra.dll) rejected="$token" ;;
        esac
    done
    if [ -n "$rejected" ]; then
        record "REJECT:$frontend:$rejected"
        if [ "${FAKE_REJECT_MODE:-}" = accepted ]; then return 0; fi
        if [ "${FAKE_REJECT_MODE:-}" = diagnostic ]; then
            echo 'wrong diagnostic' >&2
        else
            echo "invalid argument $rejected" >&2
        fi
        return 2
    fi
    record "ACCEPT:$frontend:$args"
    [ "${FAKE_ACCEPT_FAIL:-}" != "$frontend:$jobs" ] || return 29
    record "EMIT:$dir:$frontend:$args"
    mkdir -p "$dir"
    if [ "$no_metadata" = 0 ] || [ "${FAKE_METADATA_FAIL:-}" = "$frontend:packed" ]; then
        echo 'md_record_subject[] = {};' > "$dir/generated.cpp"
    elif [ "${FAKE_METADATA_FAIL:-}" = "$frontend:missing" ]; then
        echo '// no metadata' > "$dir/generated.cpp"
    else
        echo 'md_native_subject[] = {};' > "$dir/generated.cpp"
    fi
}
invoke_cli() { prepare_cli; fake_frontend full "$@"; }
dotnet() {
    [ "$1" = exec ] && [[ "$2" == *dn2cpp-console.dll ]] || return 97
    shift 2
    fake_frontend console "$@"
}
'''
cli = cli_stubs + source("cli-args")
result, calls = run("cli-args", cli)
check(result.returncode == 0, result.stderr)
accepted = [line for line in calls if line.startswith("ACCEPT:")]
rejected = [line for line in calls if line.startswith("REJECT:")]
check(len(accepted) == 4 and len(rejected) == 12, (accepted, rejected))
check(sum(line.startswith("EMIT:") for line in calls) == 4, calls)
check(not any(line.startswith("NATIVE:") for line in calls), calls)
for frontend in ("full", "console"):
    for jobs in ("1", "2"):
        matching = [line for line in accepted if line.startswith("ACCEPT:" + frontend + ":") and "--jobs " + jobs in line]
        check(len(matching) == 1, (frontend, jobs, accepted))
        check(("--no-metadata-compression" in matching[0]) == (jobs == "1"), matching)
    for token in ("--jobs", "0", "-2", "many"):
        check("REJECT:" + frontend + ":" + token in rejected, (frontend, token, rejected))
check(any("--no-default-ref DnZlib" in line for line in accepted), accepted)
for token in ("--bogus-flag", "--tirm-reflection", "extra.dll", "DnBogus"):
    check("REJECT:full:" + token in rejected, (token, rejected))
check(any("combined-acceptance" in line for line in calls if line.startswith("CACHE:")), calls)
check(sum(line == "PREPARE" for line in calls) == 1, calls)
check("COMMIT" in calls, calls)
for frontend in ("full", "console"):
    for failure in ("packed", "missing"):
        result, calls = run("cli-metadata-" + frontend + "-" + failure, cli,
                            {"FAKE_METADATA_FAIL": frontend + ":" + failure})
        check(result.returncode != 0, (frontend, failure))
        check("COMMIT" not in calls, (frontend, failure, calls))
        check("FAIL: " + frontend + " emitted " in result.stderr, result.stderr)
    result, calls = run("cli-accept-fail-" + frontend, cli, {"FAKE_ACCEPT_FAIL": frontend + ":2"})
    check(result.returncode != 0 and "COMMIT" not in calls, (frontend, calls))
for failure in ("accepted", "diagnostic"):
    result, calls = run("cli-reject-" + failure, cli, {"FAKE_REJECT_MODE": failure})
    check(result.returncode != 0 and "COMMIT" not in calls, (failure, calls))
result, calls = run("cli-args-warm", cli, {"FAKE_CACHE_HIT": "1"})
check(result.returncode == 0 and "HIT" in calls, calls)
check(not any(line.startswith(("ACCEPT:", "REJECT:", "EMIT:")) for line in calls), calls)

(root / "results.json").write_text(json.dumps(results, indent=2) + "\n", encoding="utf-8")
print(f"short-gate-builds: {checks} checks passed; evidence: {root}")
