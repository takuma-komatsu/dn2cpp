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
fake_invoke_cli() {
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
invoke_cli() { fake_invoke_cli "$@"; }
compile_console() { record "NATIVE:$1:$2"; mkdir -p "$1"; }
run_bounded() {
    if [ "$1" = invoke_cli ]; then "$@"; else record "RUN:$*"; fi
}
fake_gate_capture() {
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
gate_capture() { fake_gate_capture "$@"; }
gate_capture_ok() {
    gate_capture "$@"
    [ "$(cat "$1.status")" = 0 ]
}
fake_gate_output() {
    record "ASSERT:$*"
    if [ -n "${FAKE_ASSERT_FAIL:-}" ] && [[ "$*" == *"$FAKE_ASSERT_FAIL"* ]]; then return 31; fi
    if [ "$1" = status ]; then [ "$2" = "$3" ] || return 1; fi
    if [ "$1" = view ]; then
        printf '%s\n' 'enumerable snapshot count=2 first=True order=True' \
            'enumerable snapshot count=2 first=True order=True' \
            'enumerable snapshot count=2 first=True order=True' \
            'enumerable snapshot count=2 first=True order=True' \
            'enumerable snapshot count=2 first=True order=True' > "$3"
    fi
}
gate_output() { fake_gate_output "$@"; }
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
culture_stubs = r'''
DN2CPP_OS=${FAKE_CULTURE_OS:-macos}
CMAKE=fake_culture_cmake
fake_culture_cmake() { record "CMAKE:$*"; }
default_culture_run() {
    local result="$1" side="$2" path
    shift 2
    record "CULTURE_RUN:$side:$*"
    if [ -n "${FAKE_CULTURE_RUN_FAIL:-}" ] && [[ "$side:$*" == *"$FAKE_CULTURE_RUN_FAIL"* ]]; then return 23; fi
    CULTURE_SEQUENCE=$((${CULTURE_SEQUENCE:-0} + 1))
    path="$PWD/culture-$CULTURE_SEQUENCE.stdout"
    printf -v "$result" '%s' "$path"
    if [[ "$*" == *culture-user-lcid* ]]; then
        printf '1033\n' > "$path"
    else
        printf 'Name=ja-JP\n' > "$path"
    fi
}
gate_output() {
    record "ASSERT:$*"
    if [ -n "${FAKE_ASSERT_FAIL:-}" ] && [[ "$*" == *"$FAKE_ASSERT_FAIL"* ]]; then return 31; fi
    if [ "$1" = view ]; then cp "$2" "$3"; fi
}
gate_assert_text() { record "ASSERT_TEXT:$*"; }
'''
culture_env = culture[culture.index("run_locale_env() {"):
                      culture.index("default_culture_diagnostics_self_test() {")]
culture_profile = culture[culture.index('case "${DN2CPP_DEFAULT_CULTURE_FULL:-0}" in'):]
culture_script = culture_stubs + culture_env + culture_profile
for platform, value, expected in (("macos", None, 21), ("macos", "0", 21), ("macos", "1", 1413),
                                  ("linux", "1", 1411), ("windows", "0", 27), ("windows", "1", 1523)):
    extra = {"FAKE_CULTURE_OS": platform}
    if value is not None:
        extra["DN2CPP_DEFAULT_CULTURE_FULL"] = value
    result, calls = run("culture-profile-" + platform + "-" + str(value), culture_script, extra)
    check(result.returncode == 0, (platform, value, result.stderr))
    check(sum(line.startswith("NATIVE:") for line in calls) == 1, (platform, value, "native builds"))
    check(sum(line.startswith("EMIT:") for line in calls) == 1, (platform, value, "transpiles"))
    runs = [line for line in calls if line.startswith(("CULTURE_RUN:", "CAPTURE:"))]
    check(len(runs) == expected, (platform, value, len(runs), expected))
    full = value == "1"
    check(any(" culture-fallback " in line for line in runs) == full, (platform, value, "isolated fallbacks"))
    check(any(" culture-user-numbers " in line for line in runs) == (full and platform == "windows"),
          (platform, value, "Windows constructor order"))
    check(any(line.startswith("CMAKE:") for line in calls) == (full and platform != "windows"),
          (platform, value, "libc fixture"))
    check(any("LANG=zz_ZZ.UTF-8" in line for line in runs), (platform, value, "ZZ environment"))
    check(any("culture-zz-currency" in line for line in runs), (platform, value, "frozen currency"))
    check(any(("full:" + (value or "0")) in line for line in calls if line.startswith("CACHE:")),
          (platform, value, "cache profile"))
    check(("skipped (not selected)" in result.stdout) == (not full), (platform, value, result.stdout))
    check("COMMIT" in calls, (platform, value, "cache receipt"))
for label, extra, expected in (
        ("override", {"DN2CPP_CULTURE_LOCALES": "ja_JP.UTF-8"}, 0),
        ("warm", {"FAKE_CACHE_HIT": "1"}, 0),
        ("invalid", {"DN2CPP_DEFAULT_CULTURE_FULL": "bad"}, 1),
        ("run-failure", {"FAKE_CULTURE_RUN_FAIL": "LANG=zz_ZZ.UTF-8"}, 23),
        ("witness-failure", {"FAKE_ASSERT_FAIL": "default culture identities end"}, 1)):
    result, calls = run("culture-profile-" + label, culture_script, extra)
    check(result.returncode == expected, (label, result.returncode, result.stderr))
    if expected:
        check("COMMIT" not in calls, (label, "failed validation cached"))
    if label == "override":
        check(sum(line.startswith(("CULTURE_RUN:", "CAPTURE:")) for line in calls) == 17,
              (label, "locale override ignored"))
    elif label == "warm":
        check("HIT" in calls and not any(line.startswith("NATIVE:") for line in calls), (label, calls))
        check("skipped (not selected)" in result.stdout, (label, "warm profile note missing"))
    elif label == "invalid":
        check(not any(line.startswith(("EMIT:", "NATIVE:", "CULTURE_RUN:")) for line in calls),
              (label, calls))
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
    local frontend="$1" args dir= jobs= no_metadata=0 rejected= token fp=strict
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
            --fp-mode)
                case "${1:-}" in
                    strict|precise) fp="$1"; shift ;;
                    '') rejected=--fp-mode ;;
                    *) rejected="$1"; shift ;;
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
    printf '%s\n' "$fp" > "$dir/generated-fp-mode.txt"
    if [ "$no_metadata" = 0 ] || [ "${FAKE_METADATA_FAIL:-}" = "$frontend:packed" ]; then
        echo 'md_record_subject[] = {};' > "$dir/generated.cpp"
    elif [ "${FAKE_METADATA_FAIL:-}" = "$frontend:missing" ]; then
        echo '// no metadata' > "$dir/generated.cpp"
    else
        echo 'md_native_subject[] = {};' > "$dir/generated.cpp"
    fi
}
_gate_surface_lines() { cat "$1/generated-fp-mode.txt"; }
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
check(len(accepted) == 7 and len(rejected) == 18, (accepted, rejected))
check(sum(line.startswith("EMIT:") for line in calls) == 7, calls)
check(not any(line.startswith("NATIVE:") for line in calls), calls)
for frontend in ("full", "console"):
    for jobs in ("1", "2"):
        matching = [line for line in accepted if line.startswith("ACCEPT:" + frontend + ":") and "--jobs " + jobs in line]
        check(len(matching) == 1, (frontend, jobs, accepted))
        check(("--no-metadata-compression" in matching[0]) == (jobs == "1"), matching)
    for token in ("--jobs", "0", "-2", "many", "--fp-mode", "fast", "presice"):
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

# A rewrite hook must use the built input without publishing it to other gates.
prepare_app = r'''
gate_prepare_app() {
    record "PREPARE_APP:$1:$2:$_CG_APP:$_CG_CORELIB"
    [ -f "$_CG_APP" ]
    [ "${FAKE_PREPARE_FAIL:-0}" = 0 ] || return 23
    mkdir -p "$2/private"
    printf 'rewritten\n' > "$2/private/$1.dll"
    _CG_APP="$2/private/$1.dll"
}
_corelib_gate_core PreparedApp artifacts/prepared System.Collections.Concurrent --no-ildiet
test ! -s samples/dotnet/PreparedApp/bin/$CONFIG/$TFM/PreparedApp.dll
'''
for label, extra, status in (("ok", {}, 0), ("failure", {"FAKE_PREPARE_FAIL": "1"}, 23)):
    result, calls = run("prepare-app-" + label, prepare_app, extra)
    check(result.returncode == status, (label, result.returncode, result.stderr))
    check(sum(line.startswith("PREPARE_APP:") for line in calls) == 1, calls)
    emit = [line for line in calls if line.startswith("EMIT:")]
    check(len(emit) == (1 if status == 0 else 0), calls)
    if not status:
        check("artifacts/prepared/private/PreparedApp.dll -r " in emit[0], emit)
        check("System.Collections.Concurrent.dll --no-ildiet" in emit[0], emit)

selfhost = source("selfhost-prim-subset").replace(
    'source "$(dirname "$0")/_delegate-callable-bodies.sh"',
    (GATES / "_delegate-callable-bodies.sh").read_text(encoding="utf-8"), 1)
for helper in ("_gc-collector-contracts.sh", "_gen2-gate.sh"):
    selfhost = selfhost.replace('source "$(dirname "$0")/' + helper + '"',
                                (GATES / helper).read_text(encoding="utf-8"), 1)
signature_helper = (GATES / "_delegate-signatures.sh").read_text(encoding="utf-8")
signature_helper = signature_helper[:signature_helper.index('\nif [ "${BASH_SOURCE[0]}" = "$0" ]; then')]
selfhost = selfhost.replace('source "$(dirname "$0")/_delegate-signatures.sh"',
                            signature_helper, 1)
selfhost_stubs = r'''
for reference in System.Linq System.Diagnostics.TraceSource System.Collections.Specialized; do
    : > "runtime/$reference.dll"
done
prepare_gen2_projects() {
    local base=gates/fixtures/gen2-callback/interpreter/Base/bin/$CONFIG/$TFM
    mkdir -p "$base"
    : > "$base/Gen2CallbackBase.dll"
}
build_proj() {
    [ -z "${DN2CPP_SKIP_BUILD:-}" ] || return 0
    fake_build "$1"
    [[ "$1" != */Gen2CallbackPatch.csproj ]] || prepare_gen2_projects
}
gate_add_exit_hook() { :; }
run_bounded() {
    record "RUN:$*"
    printf '%s\n' 'child exit 23' 'launch failure caught' 'NUL argument rejected' 'tool process complete'
    printf 'child stderr\n' >&2
}
gate_python() {
    record "PYTHON:$*"
    if [[ "$1" == *check-delegate-callable-bodies.py ]]; then
        [ "${FAKE_RETAINED_CHECK_FAIL:-0}" = 0 ] || return 37
    else
        "$FAKE_PYTHON" "$FAKE_GATES/${1#gates/}" "${@:2}"
    fi
}
# Missing CoreLib and malformed private inputs refuse; positive emissions retain
# their command/image count, including the two BPI encodings and metadata-only TI.
invoke_cli() {
    local args="$*" dir= name= diagnostic= suffix body leaf query collect wait backedge
    local argv=("$@")
    while [ "$#" -gt 0 ]; do
        if [ "$1" = -o ]; then dir="$2"; break; fi
        shift
    done
    case "$dir" in
        */buffer-negative-internal/emitted|*/buffer-negative-batch/emitted)
            name=${dir%/emitted}; name=${name##*/}
            suffix=Internal
            [[ "$name" != *batch ]] || suffix=Batch
            diagnostic="System.Buffer::BulkMoveWithWriteBarrier$suffix(ref Byte, ref Byte, Int32) is unsupported" ;;
        */gc-missing-corelib.*/collect/emitted|*/gc-missing-corelib.*/memory/emitted)
            name=${dir%/emitted}; name=${name##*/}
            suffix=Collect
            [[ "$name" != memory ]] || suffix=GetTotalMemory
            name=gc-missing-corelib-$name
            diagnostic="System.GC.$suffix requires CoreLib IL; use --auto-ref or -r <path/to/System.Private.CoreLib.dll>." ;;
        */gen2-retained.*/newobj)
            name=gen2-newobj
            diagnostic='newobj System.Gen2GcCallback — native allocation of Gen2 GC notification objects is not supported.' ;;
        */gen2-retained.*/memberref)
            name=gen2-memberref
            diagnostic='Gen2GcCallback requires its exact MemberRef signature' ;;
    esac
    if [ -n "$name" ]; then
        record "REJECT:$name:$args"
        [ "${FAKE_GC_REFUSAL:-}" != "$name:accepted" ] || return 0
        [ "${FAKE_GC_REFUSAL:-}" != "$name:diagnostic" ] || diagnostic='wrong diagnostic'
        printf 'error: %s\n' "$diagnostic" >&2
        return 2
    fi
    fake_invoke_cli "${argv[@]}" || return $?
    if [[ "$dir" == */gen2-retained.*/minimal ]]; then
        "$FAKE_PYTHON" "$FAKE_MINIMAL_WRITER" "$dir" "${FAKE_MINIMAL_FAIL:-}"
    elif [[ "${argv[0]}" == */app/SelfHostPrimSubset.dll ]]; then
        for suffix in Internal Batch; do
            body='dn2cpp_gc_memmove_refs(dst, src, size);'
            [ "${FAKE_BUFFER_BODY_FAIL:-}" != "$suffix" ] || body='std::memmove(dst, src, size);'
            printf 'void Program_Move%s_m1() {\n    %s\n}\n' "$suffix" "$body" >> "$dir/generated.cpp"
        done
        leaf='dn2cpp_gc_get_total_memory(0)'
        query="$leaf"
        collect='GC_Collect_m1();'
        wait='dn2cpp_gc_wait_for_pending_finalizers();'
        backedge='goto IL_0010;'
        case "${FAKE_GC_MEMORY_FAIL:-}" in
            leaf) leaf=0 ;;
            query) query=0 ;;
            collect) collect= ;;
            wait) wait= ;;
            backedge) backedge= ;;
        esac
        printf 'int64_t GC_GetTotalMemory_m1() {\n    return %s;\n}\n' "$leaf" >> "$dir/generated.cpp"
        printf 'int64_t GC_GetTotalMemory_m2(int32_t a0) {\n    auto before = dn2cpp_gc_get_total_memory(0);\nIL_0010:\n    %s\n    %s\n    auto after = %s;\n    %s\n}\n' \
            "$collect" "$wait" "$query" "$backedge" >> "$dir/generated.cpp"
    fi
}
gc_stdout() {
    printf '%s\n' 'mock output' '== GC collection overloads ==' \
        'GC collection overloads end' 'GC logical generations end' \
        'GC memory query entry points end' 'GC notification validation end' \
        'GC pause duration end' 'GC finalizer worker contract end' 'Buffer reference moves end'
}
gate_capture() {
    local prefix="$1" status=0 label mode
    if [ "$2" = invoke_cli ]; then
        shift
        record "CAPTURE:$prefix:$*"
        mkdir -p "$(dirname "$prefix")"
        "$@" > "$prefix.stdout" 2> "$prefix.stderr" || status=$?
        printf '%s\n' "$status" > "$prefix.status"
        return
    fi
    fake_gate_capture "$@"
    case "$prefix" in
        */selfhostprimsubset*/native|*/selfhostprimsubset*/oracle|*/buffer-unrewritten) gc_stdout > "$prefix.stdout" ;;
        */buffer-fixture)
            printf '%s\n' 'buffer-move-fixture:Internal:ref-byte,ref-byte,nuint' \
                'buffer-move-fixture:Batch:ref-byte,ref-byte,nuint' > "$prefix.stdout" ;;
        */gc-missing-corelib.*/collect/fixture)
            printf '%s\n' 'gc-missing-corelib-fixture:collect:System.Void System.GC::Collect()' > "$prefix.stdout" ;;
        */gc-missing-corelib.*/memory/fixture)
            printf '%s\n' 'gc-missing-corelib-fixture:memory:System.Int64 System.GC::GetTotalMemory(System.Boolean)' > "$prefix.stdout" ;;
        */gc-boundary)
            : > "$prefix.stdout"
            for label in fullgc-register-direct fullgc-register-delegate fullgc-register-pointer fullgc-cancel-direct fullgc-cancel-delegate; do
                printf '%s\n' "$label:InvalidOperationException::This API is not available when the concurrent GC is enabled." >> "$prefix.stdout"
            done
            for label in fullgc-approach-default fullgc-complete-default fullgc-approach-zero fullgc-complete-positive fullgc-approach-pointer fullgc-approach-time fullgc-complete-time fullgc-approach-fraction fullgc-complete-upper; do
                printf '%s\n' "$label:NotApplicable" >> "$prefix.stdout"
            done
            printf '%s\n' 'GC notification unavailable end' 'gc-max-generation:0' \
                'gc-worker-refused:System.GC.RunFinalizers is a CoreCLR-owned worker entry point; dn2cpp owns finalization. Use GC.WaitForPendingFinalizers instead.' \
                'GC finalizer worker refusal end' >> "$prefix.stdout" ;;
        */gen2-retained.*/fixture-*)
            mode=${prefix##*/fixture-}
            printf 'gen2-fixture:%s\n' "$mode" > "$prefix.stdout" ;;
        */gen2-retained.*/native-register|*/gen2-retained.*/native-stack)
            printf '%s\n' gen2-ctor0-reflection:True gen2-ctor1-reflection:True gen2-uninitialized:True \
                gen2-final-metadata:True gen2-final-reflection-null-target:True \
                gen2-callbacks-untouched-reflection:True gen2-interp-base-ready \
                gen2-interp-ctor0:True gen2-interp-ordinary:True gen2-interp-end \
                > "$prefix.stdout" ;;
    esac
    if [ -n "${FAKE_GC_TAINT:-}" ] && [[ "$prefix" == *"$FAKE_GC_TAINT" ]]; then
        printf 'wrong output\n' > "$prefix.stdout"
    fi
    if [ -n "${FAKE_GC_STDERR:-}" ] && [[ "$prefix" == *"$FAKE_GC_STDERR" ]]; then
        printf 'unexpected stderr\n' > "$prefix.stderr"
    fi
}
# Run the real contracts on the new fixture/boundary transcripts, prefix and
# refusal diagnostics; a changed byte must fail, not merely call an assertion.
gate_output() {
    fake_gate_output "$@" || return $?
    case "${2:-}:$*" in
        */gc-*:*|*/buffer-*:*|*/gen2-retained.*:*|*:witness*'GC collection overloads end'*|*:prefix*'/gc-prefix.stdout'*)
            "$FAKE_PYTHON" "$FAKE_OUTPUT_HELPER" "$@" ;;
    esac
}
if [ -n "${DN2CPP_SKIP_BUILD:-}" ]; then
    fake_build samples/dotnet/SelfHostPrimSubset/SelfHostPrimSubset.csproj
    fake_build gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
    fake_build gates/fixtures/gen2-callback/interpreter/Patch/Gen2CallbackPatch.csproj
    prepare_gen2_projects
    : > "$FAKE_LOG"
fi
'''
selfhost_stubs += common[common.index("gate_assert_text() {"):
                         common.index("# assert_output ACTUAL EXPECTED")]
minimal_writer = root / "write-minimal.py"
minimal_writer.write_text('''from pathlib import Path
import sys
output, failure = Path(sys.argv[1]), sys.argv[2]
hot = ["nullptr"] * 22
hot[15] = "0" if failure != "layout" else "24"
hot[21] = "Dn2CppMetadataHandle<Dn2CppTypeReflection>::from_raw(md_native_refl_ti_System_Gen2GcCallback + 0)"
cold = ["0", "0LL"] * 13 + ["8LL" if failure != "bit" else "0LL"]
if failure == "ctor": cold[4] = "ctors"
text = "const Dn2CppTypeInfo ti_System_Gen2GcCallback = { " + ", ".join(hot) + " };\\n"
text += "static const Dn2CppTypeReflection md_native_refl_ti_System_Gen2GcCallback[] = {\\n{ " + ", ".join(cold) + " },\\n};\\n"
(output / "generated.cpp").write_text(text)
''', encoding="utf-8")
selfhost_env = {"FAKE_PYTHON": os.sys.executable, "FAKE_OUTPUT_HELPER": str(GATES / "gate-output.py"),
                "FAKE_GATES": str(GATES), "FAKE_MINIMAL_WRITER": str(minimal_writer)}
for label, extra, expected in (
        ("cold", {}, (7, 4, 4)),
        ("suite", {"DN2CPP_SKIP_BUILD": "1"}, (7, 4, 1)),
        ("suffix", {"DN2CPP_OUT_SUFFIX": "-probe", "HIGHWAY": "1"}, (7, 4, 4)),
        ("warm", {"FAKE_CACHE_HIT": "1"}, (2, 1, 3))):
    result, calls = run("selfhost-" + label, selfhost_stubs + selfhost, {**selfhost_env, **extra})
    check(result.returncode == 0, (label, result.returncode, result.stderr))
    emit = [line for line in calls if line.startswith("EMIT:")]
    native = [line for line in calls if line.startswith("NATIVE:")]
    builds = [line for line in calls if line.startswith("BUILD:")]
    captures = [line for line in calls if line.startswith("CAPTURE:")]
    check((len(emit), len(native), len(builds)) == expected, (label, emit, native, builds))
    check(sum(line == "PREPARE" for line in calls) == 1, calls)
    check(not any("DelegateCallableBodies.csproj" in line for line in calls), calls)
    check(any("/app/SelfHostPrimSubset.dll -r " in line for line in emit), emit)
    cache = [line for line in calls if line.startswith("CACHE:")]
    check(any("delegate-callable-refusals.txt" in line and "keep-delegate-callable-bodies.xml" in line
              and "delegate-callable:reflection,call,calli,callvirt,ldvirtftn" in line for line in cache), cache)
    for dependency in ("_gc-collector-contracts.sh", "check-gc-collector-bodies.py", "BufferReferenceMoveFixture.cs",
                       "GcCoreLibInputFixture.cs", "gc-corelib-input:collect,memory",
                       "_gen2-gate.sh", "Gen2CallbackFixture.cs", "check-minimal-metadata.py",
                       "gc-collector:il-wrappers,finalizer-refusal", "buffer-reference-moves:internal,batch,overlap,barrier,wrong-count,standalone-clr",
                       "gen2:reflection,register,stack,reference-only,newobj,memberref"):
        check(any(dependency in line for line in cache), (label, dependency, cache))
    raw_base = "samples/dotnet/SelfHostPrimSubset/bin/Debug/net10.0/SelfHostPrimSubset"
    raw = [line for line in captures if "/buffer-unrewritten:" in line]
    check(len(raw) == 1 and raw[0].endswith("run_bounded dotnet " + raw_base + ".dll"), raw)
    delegate_fixture = next(line for line in captures if " --delegate-callable-bodies " in line)
    buffer_fixture = next(line for line in captures if " --buffer-reference-moves " in line)
    check(calls.index(raw[0]) < calls.index(delegate_fixture) < calls.index(buffer_fixture), captures)
    for suffix in (".dll", ".runtimeconfig.json", ".deps.json"):
        check(any(raw_base + suffix in line for line in cache), (label, suffix, cache))
    check(any(raw[0].split(":")[1] + ".stdout" in line for line in cache), (label, raw, cache))
    if label == "warm":
        check("HIT" in calls and "COMMIT" not in calls, calls)
        check(not any("delegate-refusals " in line for line in calls if line.startswith("CAPTURE:")), calls)
        check(not any("gen2-retained" in line for line in native + emit), calls)
        check(not any(line.startswith("REJECT:") for line in calls), calls)
        continue
    target = next(line.split(":")[1] for line in native if line.endswith(":SelfHostPrimSubset"))
    check(f"ASSERT:compare {target}/buffer-unrewritten.stdout {target}/oracle.stdout" in calls, calls)
    for route in ("reflection", "call", "calli", "callvirt", "ldvirtftn"):
        for mode in ("delegate-callable", "delegate-refusals"):
            check(any(line.endswith(f"{target}/SelfHostPrimSubset {mode} {route}") for line in captures),
                  (label, route, mode, captures))
        check(any(line.endswith(f"{target}/app/SelfHostPrimSubset.dll delegate-callable {route}")
                  for line in captures), (label, route, captures))
    check(any("--delegate-retained-only" in line and "/retained-app/SelfHostPrimSubset.dll " in line
              for line in captures), captures)
    check(sum("--link-xml samples/dotnet/SelfHostPrimSubset/keep-delegate-callable-bodies.xml" in line
              for line in emit) == 1, emit)
    check(any(" before-nullable-reboxing" in line for line in captures), captures)
    check(sum(" delegate-callable before-virtual-delegates" in line for line in captures) == 2, captures)
    check("COMMIT" in calls and any(" --delegate-signatures " in line for line in captures), calls)
    check(any(line.endswith(f"{target}/SelfHostPrimSubset gc-native-boundary") for line in captures), captures)
    check(any(line.endswith(f"{target}/app/SelfHostPrimSubset.dll before-gc-contracts") for line in captures), captures)
    check(any("--buffer-reference-moves " in line and f"{target}/app/SelfHostPrimSubset.dll " in line
              for line in captures), captures)
    check(sum("--buffer-reference-moves-negative-" in line for line in captures) == 2, captures)
    gen2_native = [line for line in native if line.endswith(":Gen2CallbackBase")]
    check(len(gen2_native) == 1, gen2_native)
    gen2_base = gen2_native[0].split(":")[1]
    for codec in ("register", "stack"):
        patch = [line for line in emit if f"/patch-{codec}:" in line]
        check(len(patch) == 1 and ":--emit-patch " in patch[0], patch)
        check(("--patch-stackcode" in patch[0]) == (codec == "stack"), patch)
        check(f"--base-abi {gen2_base}/base-abi.json" in patch[0], patch)
        check(any(f"{gen2_base}/Gen2CallbackBase " in line and f"/patch-{codec}/Gen2CallbackPatch.bpi" in line
                  for line in captures), captures)
    minimal = [line for line in emit if "/minimal:" in line]
    check(len(minimal) == 1 and "--reflection-metadata System.Gen2GcCallback=native" in minimal[0], minimal)
    check(not any("/minimal:" in line for line in native), native)
    check(not any("run_bounded dotnet " in line and "/input/" in line
                  and "LdftnLocalFixture.dll" not in line for line in captures), captures)
    refusals = [line for line in calls if line.startswith("REJECT:")]
    check([line.split(":")[1] for line in refusals] == ["buffer-negative-internal", "buffer-negative-batch",
          "gc-missing-corelib-collect", "gc-missing-corelib-memory", "gen2-newobj", "gen2-memberref"], refusals)
    check(sum(line == "ASSERT:status 2 2" for line in calls) == 6, calls)
    for mode in ("collect", "memory"):
        missing = [line for line in refusals if line.startswith(f"REJECT:gc-missing-corelib-{mode}:")]
        check(len(missing) == 1 and f"/{mode}/SelfHostPrimSubset.dll --no-ildiet -o " in missing[0], missing)
        check(" -r " not in missing[0] and " --auto-ref" not in missing[0] and " --no-default-ref" not in missing[0], missing)
        check(sum(f" --gc-missing-corelib-{mode} " in line for line in captures) == 1, captures)
    check(sum("check-minimal-metadata.py" in line for line in calls if line.startswith("PYTHON:")) == 1, calls)
    check(sum("check-gc-collector-bodies.py" in line for line in calls if line.startswith("PYTHON:")) == 1, calls)
    check(sum("Gen2CallbackPatch.csproj" in line for line in builds) == (0 if label == "suite" else 1), builds)

for label, extra in (
        ("fixture-status", {"FAKE_STATUS_FAIL": "/fixture"}),
        ("fixture-witness", {"FAKE_ASSERT_FAIL": "CallDelegateEquals=call:"}),
        ("parity-status", {"FAKE_STATUS_FAIL": "delegate-reflection-native"}),
        ("parity-bytes", {"FAKE_ASSERT_FAIL": "compare artifacts/selfhostprimsubset/delegate-calli-native.stdout"}),
        ("refusals", {"FAKE_ASSERT_FAIL": "compare artifacts/selfhostprimsubset/delegate-refusals-ldvirtftn.stdout"}),
        ("retained-fixture", {"FAKE_STATUS_FAIL": "retained-fixture"}),
        ("retained-emit", {"FAKE_EMIT_FAIL": "retained-only"}),
        ("retained-check", {"FAKE_RETAINED_CHECK_FAIL": "1"}),
        ("retained-status", {"FAKE_STATUS_FAIL": "retained-only/native"})):
    result, calls = run("selfhost-failure-" + label, selfhost_stubs + selfhost, {**selfhost_env, **extra})
    check(result.returncode != 0, (label, result.stderr))
    check("COMMIT" not in calls and not any(" --delegate-signatures " in line for line in calls), (label, calls))
    if label.startswith("fixture-"):
        check(not any(line.startswith(("EMIT:", "NATIVE:")) for line in calls), (label, calls))

for label, extra in (("signature-status", {"FAKE_STATUS_FAIL": "/boundaries"}),
                     ("signature-witness", {"FAKE_ASSERT_FAIL": "delegate-signature-boundaries:ok"})):
    result, calls = run("selfhost-failure-" + label, selfhost_stubs + selfhost, {**selfhost_env, **extra})
    check(result.returncode != 0, (label, result.stderr))
    check(sum(line.startswith("EMIT:") for line in calls) == 7, calls)
    check(sum(line.startswith("NATIVE:") for line in calls) == 4, calls)

for label, extra in (
        ("raw-status", {"FAKE_STATUS_FAIL": "/buffer-unrewritten"}),
        ("raw-status-warm", {"FAKE_STATUS_FAIL": "/buffer-unrewritten", "FAKE_CACHE_HIT": "1"}),
        ("raw-bytes", {"FAKE_GC_TAINT": "/buffer-unrewritten"}),
        ("buffer-fixture-status", {"FAKE_STATUS_FAIL": "/buffer-fixture"}),
        ("buffer-fixture-bytes", {"FAKE_GC_TAINT": "/buffer-fixture"}),
        ("gc-tail", {"FAKE_GC_TAINT": "selfhostprimsubset/native"}),
        ("gc-prefix-status", {"FAKE_STATUS_FAIL": "/gc-prefix"}),
        ("gc-prefix-bytes", {"FAKE_GC_TAINT": "/gc-prefix"}),
        ("gc-boundary-status", {"FAKE_STATUS_FAIL": "/gc-boundary"}),
        ("gc-boundary-bytes", {"FAKE_GC_TAINT": "/gc-boundary"}),
        ("buffer-internal-body", {"FAKE_BUFFER_BODY_FAIL": "Internal"}),
        ("buffer-batch-body", {"FAKE_BUFFER_BODY_FAIL": "Batch"}),
        ("gc-memory-leaf", {"FAKE_GC_MEMORY_FAIL": "leaf"}),
        ("gc-memory-query", {"FAKE_GC_MEMORY_FAIL": "query"}),
        ("gc-memory-collect", {"FAKE_GC_MEMORY_FAIL": "collect"}),
        ("gc-memory-wait", {"FAKE_GC_MEMORY_FAIL": "wait"}),
        ("gc-memory-backedge", {"FAKE_GC_MEMORY_FAIL": "backedge"}),
        ("buffer-internal-fixture", {"FAKE_STATUS_FAIL": "/buffer-negative-internal/fixture"}),
        ("buffer-batch-fixture", {"FAKE_STATUS_FAIL": "/buffer-negative-batch/fixture"}),
        ("gen2-base-emit", {"FAKE_EMIT_FAIL": "/base"}),
        ("gen2-register-emit", {"FAKE_EMIT_FAIL": "/patch-register"}),
        ("gen2-stack-emit", {"FAKE_EMIT_FAIL": "/patch-stack"}),
        ("gen2-codec-bytes", {"FAKE_ASSERT_FAIL": "/native-register.stdout artifacts/"}),
        ("gen2-codec-status", {"FAKE_ASSERT_FAIL": "/native-register.status"}),
        ("gen2-minimal-emit", {"FAKE_EMIT_FAIL": "/minimal"}),
        ("gen2-minimal-bit", {"FAKE_MINIMAL_FAIL": "bit"}),
        ("gen2-minimal-layout", {"FAKE_MINIMAL_FAIL": "layout"}),
        ("gen2-minimal-ctor", {"FAKE_MINIMAL_FAIL": "ctor"})):
    result, calls = run("selfhost-gc-failure-" + label, selfhost_stubs + selfhost, {**selfhost_env, **extra})
    check(result.returncode != 0, (label, result.stderr))
    check("COMMIT" not in calls and not any(" --delegate-signatures " in line for line in calls), (label, calls))
    if label.startswith("raw-status"):
        check("HIT" not in calls and not any(line.startswith(("EMIT:", "NATIVE:")) for line in calls), calls)

for witness in ("fullgc-register-direct", "fullgc-register-delegate", "fullgc-register-pointer",
                "fullgc-cancel-direct", "fullgc-cancel-delegate", "fullgc-approach-default",
                "fullgc-complete-default", "fullgc-approach-zero", "fullgc-complete-positive",
                "fullgc-approach-pointer", "fullgc-approach-time", "fullgc-complete-time",
                "fullgc-approach-fraction", "fullgc-complete-upper", "gc-max-generation:0", "gc-worker-refused:"):
    result, calls = run("selfhost-gc-witness-" + witness.replace(":", "-"), selfhost_stubs + selfhost,
                        {**selfhost_env, "FAKE_ASSERT_FAIL": witness})
    check(result.returncode != 0 and "COMMIT" not in calls, (witness, calls))

for mode in ("base", "patch", "minimal", "newobj", "memberref"):
    for failure, variable in (("status", "FAKE_STATUS_FAIL"), ("bytes", "FAKE_GC_TAINT"), ("stderr", "FAKE_GC_STDERR")):
        result, calls = run("selfhost-gen2-fixture-" + mode + "-" + failure, selfhost_stubs + selfhost,
                            {**selfhost_env, variable: "/fixture-" + mode})
        check(result.returncode != 0 and "COMMIT" not in calls, (mode, failure, calls))
        check(not any(line.endswith(":Gen2CallbackBase") for line in calls if line.startswith("NATIVE:")), calls)

for codec in ("register", "stack"):
    for failure, variable in (("status", "FAKE_STATUS_FAIL"), ("bytes", "FAKE_GC_TAINT"), ("stderr", "FAKE_GC_STDERR")):
        result, calls = run("selfhost-gen2-native-" + codec + "-" + failure, selfhost_stubs + selfhost,
                            {**selfhost_env, variable: "/native-" + codec})
        check(result.returncode != 0 and "COMMIT" not in calls, (codec, failure, calls))
        check(not any("/minimal:" in line for line in calls if line.startswith("EMIT:")), calls)

for mode in ("collect", "memory"):
    for failure, variable in (("status", "FAKE_STATUS_FAIL"), ("bytes", "FAKE_GC_TAINT"), ("stderr", "FAKE_GC_STDERR")):
        result, calls = run("selfhost-gc-input-fixture-" + mode + "-" + failure, selfhost_stubs + selfhost,
                            {**selfhost_env, variable: "/" + mode + "/fixture"})
        check(result.returncode != 0 and "COMMIT" not in calls, (mode, failure, calls))
        check(not any(line.startswith("REJECT:gc-missing-corelib-" + mode + ":") for line in calls), calls)

for name in ("buffer-negative-internal", "buffer-negative-batch", "gc-missing-corelib-collect",
             "gc-missing-corelib-memory", "gen2-newobj", "gen2-memberref"):
    for failure in ("accepted", "diagnostic"):
        result, calls = run("selfhost-refusal-" + name + "-" + failure, selfhost_stubs + selfhost,
                            {**selfhost_env, "FAKE_GC_REFUSAL": name + ":" + failure})
        check(result.returncode != 0 and "COMMIT" not in calls, (name, failure, calls))
        check(any(line.startswith("REJECT:" + name + ":") for line in calls), calls)

(root / "results.json").write_text(json.dumps(results, indent=2) + "\n", encoding="utf-8")
print(f"short-gate-builds: {checks} checks passed; evidence: {root}")
