#!/usr/bin/env python3
"""SDK-free selection, binary reuse and failure checks for the short gates."""
import json
import os
from pathlib import Path
import re
import shlex
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET


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
        [ "${FAKE_PREPARE_CLI_FAIL:-0}" = 0 ] || return 23
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
    [ "$name" != SelfHostPrimSubset ] || : > "$dir/DefaultStructCtorLib.dll"
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


# Exercise the real fixture's source extraction, not only the mocked gate.
diagnostic_dir = root / "diagnostic-record-matches"
diagnostic_dir.mkdir()
diagnostic = subprocess.run([BASH, "gates/fixtures/diagnostic-record-matches.sh", sys.executable],
                            cwd=GATES.parent, env=ENV, capture_output=True)
(diagnostic_dir / "stdout.log").write_bytes(diagnostic.stdout)
(diagnostic_dir / "stderr.log").write_bytes(diagnostic.stderr)
(diagnostic_dir / "status").write_text(str(diagnostic.returncode) + "\n", encoding="utf-8")
results.append({"name": "diagnostic-record-matches", "status": diagnostic.returncode,
                "native": 0, "emit": 0})
check(diagnostic.returncode == 0, ("diagnostic-record-matches", diagnostic.stderr))
check(b"OK: actual diagnostic adapters retain payload CR" in diagnostic.stdout, diagnostic.stdout)


# Shared fixtures have one verification owner and one orchestrator build root.
gen2_owners = [path.name for path in GATES.glob("build-and-run-*.sh")
               if re.search(r"^\s*gen2_retained_boundary_gate\s", path.read_text(encoding="utf-8"), re.M)]
check(gen2_owners == ["build-and-run-selfhost-prim-subset.sh"], gen2_owners)
hotupdate = source("hotupdate-subset")
for obsolete in ("_gen2-gate.sh", "GEN2_GATE_INPUTS", "gen2-retained:"):
    check(obsolete not in hotupdate, f"hotupdate retains removed Gen2 ownership: {obsolete}")
orchestrator = (GATES / "run-all-gates.sh").read_text(encoding="utf-8")
ordered_text = re.search(r"^ORDERED_PROJECTS=\(\n(.*?)^\)", orchestrator, re.M | re.S)
check(ordered_text is not None, "missing ordered project roots")
ordered_projects = re.findall(r'^\s*"([^"]+[.]csproj)"\s*$', ordered_text[1], re.M)
check(ordered_projects and len(ordered_projects) == len(set(ordered_projects)), ordered_projects)
gen2_patch = "gates/fixtures/gen2-callback/interpreter/Patch/Gen2CallbackPatch.csproj"
gen2_base = "gates/fixtures/gen2-callback/interpreter/Base/Gen2CallbackBase.csproj"
check(ordered_projects.count(gen2_patch) == 1 and gen2_base not in ordered_projects, ordered_projects)
project = GATES.parent / gen2_patch
references = [(project.parent / reference.attrib["Include"]).resolve()
              for reference in ET.parse(project).iter("ProjectReference")]
check((GATES.parent / gen2_base).resolve() in references, references)


# Array observations are reused only inside the complete wrapper's cache region.
array_source = source("array-core")
array_helpers = {name: (GATES / name).read_text(encoding="utf-8")
                 for name in ("_headerless-reference-identity.sh", "_md-readonly-byrefs.sh")}

def array_script(source_text, headerless=None):
    script = source_text[:source_text.index("echo '== isolated framework and reflection boxes =='")]
    script = script.replace('source "$(dirname "$0")/_common.sh"', "")
    script = script.replace('source "$(dirname "$0")/_ildiet.sh"', "")
    for name, helper in array_helpers.items():
        script = script.replace("source gates/" + name,
                                headerless if name == "_headerless-reference-identity.sh" and headerless else helper)
    return script + "\nrecord FULL_ARRAY_PROFILE\n"

array_stubs = r'''
: > runtime/System.Collections.dll
array_record() { printf '%s\0' "$@" >> "$FAKE_ARRAY_ACTIONS"; printf '\0' >> "$FAKE_ARRAY_ACTIONS"; }
assert_output() { record "SOURCE_ASSERT:$*"; [ "$1" = "$2" ]; }
grep() {
    record "SOURCE_GREP:$*"
    if [[ "$*" == *Unsearched_Equals* ]]; then [ "${FAKE_ARRAY_FAIL:-}" = unsearched ]; return; fi
    if [ "${FAKE_ARRAY_FAIL:-}" = selected ] && [[ "$*" == *Selected_Equals* ]]; then return 1; fi
    if [ "${FAKE_ARRAY_FAIL:-}" = guard ] && [[ "$*" == *dn2cpp_array_search_equals* ]]; then return 1; fi
    return 0
}
awk() {
    record "SOURCE_AWK:$*"
    local expected
    for expected in 'shared SZ address identities' 'shared readonly MD reads' \
            'pending indirect references rooted' 'shared reference store' 'shared array allocation' \
            'shared multidimensional store' 'shared MD identities'; do
        if [[ "$*" == *"$expected"* ]]; then
            [ "${FAKE_ARRAY_FAIL:-}" != "$expected" ] || return 0
            printf '%s\n' "$expected"
            return
        fi
    done
    return 97
}
gate_capture() {
    record "CAPTURE:$1:${*:2}"
    array_record capture "$@"
    "$FAKE_PYTHON" "$FAKE_ARRAY_RUNNER" capture "$@"
}
gate_output() {
    record "ASSERT:$*"
    array_record output "$@"
    [ "${FAKE_ARRAY_DISCOVER:-0}" != 1 ] || return 0
    "$FAKE_PYTHON" "$FAKE_OUTPUT_HELPER" "$@"
}
gate_cache_check() {
    record "CACHE:$*"
    case "${FAKE_ARRAY_CACHE:-}" in
        all) return 0 ;;
        finite) [[ "$1" == *arrayfinitesearchonly* ]] ;;
        aggregate) [[ "$1" == *arraycore* ]] ;;
        *) return 1 ;;
    esac
}
if [ -n "${DN2CPP_SKIP_BUILD:-}" ]; then
    fake_build samples/dotnet/ArrayCore/ArrayCore.csproj
    fake_build samples/dotnet/ArrayCore/ArrayFiniteSearchOnly.csproj
    : > "$FAKE_LOG"
fi
'''
array_runner = root / "array-observation.py"
array_runner.write_text(r'''import json
import os
from pathlib import Path
import sys
_, prefix, *command = sys.argv[1:]
prefix = Path(prefix)
prefix.parent.mkdir(parents=True, exist_ok=True)
project = "finite" if "arrayfinitesearchonly" in prefix.as_posix() else "aggregate"
mode = command[-1] if project == "aggregate" and command[-1].startswith("before-") else ""
oracle = "dotnet" in command
records = json.loads(Path(os.environ["FAKE_ARRAY_FIXTURE"]).read_text())
data = records[project]
if mode and os.environ.get("FAKE_ARRAY_DISCOVER") != "1":
    marker = records["modes"][mode]
    data = data[:data.index(marker)]
status = int(os.environ.get("FAKE_ARRAY_WRAPPER_STATUS", "0")) if prefix.name in ("native", "oracle") else 0
failure = os.environ.get("FAKE_ARRAY_FAIL", "")
subject = os.environ.get("FAKE_ARRAY_SUBJECT", "aggregate")
active = project == subject and os.environ.get("FAKE_ARRAY_TARGET_SUFFIX", "") in prefix.as_posix()
if os.environ.get("FAKE_ARRAY_STAGE") == "argless": active = active and prefix.name == "argless-native"
if active:
    if mode and failure == "prefix" and mode == "before-default-comparison": data = ["wrong prefix"]
    if mode and failure == "prefix-status" and mode == "before-default-comparison": status = 23
    if not mode:
        if failure in ("missing-witness", "duplicate-witness"):
            marker = os.environ.get("FAKE_ARRAY_WITNESS") or ("finite local array searches end" if project == "finite" else "copy:0:0:-1:-1:-1 param=sourceArray")
            data = [line for line in data if line != marker] if failure == "missing-witness" else [*data, marker]
        if failure == "any-duplicate": data = [*data, "plain copied=handle:17:17"]
        if failure in ("prefix-marker-missing", "prefix-marker-duplicate"):
            marker = "== Array validation fields =="
            data = [line for line in data if line != marker] if failure == "prefix-marker-missing" else [*data, marker]
        if failure == "native-status" and not oracle or failure == "oracle-status" and oracle: status = 23
        if failure == "both-status": status = 2
raw = ("\n".join(data) + "\n").encode() if data else b""
if active and not mode and not oracle:
    raw += {"nul": b"\0", "cr": b"\r", "newline": b"\n"}.get(failure, b"")
    if failure == "capture-error": raise SystemExit(19)
for stream, data in (("stdout", raw), ("stderr", b"native diagnostic\n" if active and not oracle and failure == "stderr" else b""),
                     ("status", (str(status) + "\n").encode())):
    if active and not mode and not oracle and failure == "missing-" + stream: continue
    Path(str(prefix) + "." + stream).write_bytes(data)
''', encoding="utf-8")
array_fixture = root / "array-transcript.json"
array_fixture.write_text(json.dumps({"finite": ["mock output"], "aggregate": ["mock output"], "modes": {}}))
array_env = {"FAKE_PYTHON": os.sys.executable, "FAKE_OUTPUT_HELPER": str(GATES / "gate-output.py"),
             "FAKE_ARRAY_RUNNER": str(array_runner), "FAKE_ARRAY_FIXTURE": str(array_fixture)}


def array_run(name, script, extra=None):
    actions = root / (name + ".actions")
    actions.touch(exist_ok=False)
    result, calls = run(name, array_stubs + script,
                        {**array_env, "FAKE_ARRAY_ACTIONS": str(actions), **(extra or {})})
    observed = [entry.decode().split("\0") for entry in actions.read_bytes().split(b"\0\0") if entry]
    return result, calls, observed


def array_contract(actions):
    captures, witnesses, prefixes, patterns, data, modes = {}, [], [], [], {"finite": [], "aggregate": []}, {}
    for kind, *args in actions:
        if kind == "capture":
            captures[args[0] + ".stdout"] = args[1:]
            continue
        operation, actual, *tail = args
        project = "finite" if "arrayfinitesearchonly" in actual else "aggregate"
        if operation == "witness":
            count, policy = tail[tail.index("--count") + 1], tail[tail.index("--policy") + 1]
            for record in tail[:tail.index("--count")]:
                witnesses.append((project, record, count, policy))
                if record not in data[project]: data[project].append(record)
        elif operation == "prefix":
            marker = tail[tail.index("--before") + 1]
            argv = captures[tail[0]]
            mode = argv[-1]
            prefixes.append((project, marker, "oracle" if "dotnet" in argv else "native", mode))
            modes[mode] = marker
            if marker not in data[project]: data[project].append(marker)
        elif operation == "match-record":
            patterns.append((project, *tail))
            label = tail[0].split("=", 1)[0].removeprefix("^")
            data[project].append(label + "=System.TypeLoadException/80131522:'Type' from assembly 'Assembly' has too many dimensions.")
    return {"witnesses": sorted(witnesses), "prefixes": sorted(prefixes), "patterns": sorted(patterns)}, {**data, "modes": modes}

# Discover the declared records, not executable output. Real helper assertions
# below validate those synthetic bytes and deliberately corrupted observations.
array_prefix = array_script(array_source)
for full in ("0", "1"):
    name = "array-discover-" + full
    result, calls, actions = array_run(name, array_prefix, {"DN2CPP_ARRAY_FULL": full, "FAKE_ARRAY_DISCOVER": "1"})
    check(result.returncode == 0, (name, result.stderr))
    contract, transcript = array_contract(actions)
    (root / (name + ".contract.json")).write_text(json.dumps(contract, indent=2) + "\n")
    digest = __import__("hashlib").sha256(json.dumps(contract, sort_keys=True).encode()).hexdigest()
    # Pin record text/count/policy and prefix argv independently of batching.
    # The full-profile producer checks belong to the cached aggregate contract.
    expected = {"0": "806886c2fbe3db376925bd5972b2530cc7e26fa0a803099657f3530be9eeef72",
                "1": "27fa2b3ecbda837d805040cede8d0a16970ed5fd04308af8387ede0815c363e8"}[full]
    check(digest == expected, (name, digest, "witness/prefix contract changed"))
    if full == "1": array_fixture.write_text(json.dumps(transcript))

for label, extra, images in (
        ("cold", {}, 2), ("empty-argv", {"DN2CPP_GATE_RUN_ARGS": ""}, 2),
        ("explicit-cli", {"DN2CPP_CLI_DLL": "provided/dn2cpp.dll"}, 2),
        ("suite", {"DN2CPP_SKIP_BUILD": "1"}, 2),
        ("suffix", {"DN2CPP_OUT_SUFFIX": "-probe", "HIGHWAY": "1", "SCALAR": "1"}, 2),
        ("full", {"DN2CPP_ARRAY_FULL": "1"}, 3),
        ("full-suite", {"DN2CPP_ARRAY_FULL": "1", "DN2CPP_SKIP_BUILD": "1", "DN2CPP_CLI_DLL": "provided/dn2cpp.dll"}, 3),
        ("full-suffix", {"DN2CPP_ARRAY_FULL": "1", "DN2CPP_OUT_SUFFIX": "-probe", "HIGHWAY": "1"}, 3),
        ("warm", {"FAKE_ARRAY_CACHE": "all"}, 0),
        ("warm-full", {"DN2CPP_ARRAY_FULL": "1", "FAKE_ARRAY_CACHE": "all", "DN2CPP_CLI_DLL": "provided/dn2cpp.dll"}, 0),
        ("finite-warm", {"FAKE_ARRAY_CACHE": "finite"}, 1),
        ("aggregate-warm", {"FAKE_ARRAY_CACHE": "aggregate"}, 1),
        ("any-duplicate", {"FAKE_ARRAY_FAIL": "any-duplicate"}, 2),
        ("native-diagnostic", {"FAKE_ARRAY_FAIL": "stderr"}, 2),
        ("finite-diagnostic", {"FAKE_ARRAY_FAIL": "stderr", "FAKE_ARRAY_SUBJECT": "finite"}, 2)):
    result, calls, actions = array_run("array-" + label, array_prefix, extra)
    check(result.returncode == 0, (label, result.stderr))
    check(calls.count("PREPARE") == (0 if extra.get("DN2CPP_CLI_DLL") else 1), (label, calls))
    native = [line for line in calls if line.startswith("NATIVE:")]
    captures = [args for kind, *args in actions if kind == "capture"]
    full_native = [args for args in captures if "dotnet" not in args and not args[-1].startswith("before-")]
    full_oracle = [args for args in captures if "dotnet" in args and not args[-1].startswith("before-")]
    check(len(native) == images and len(full_native) == images and len(full_oracle) == images, (label, native, captures))
    check(sum(line.startswith("EMIT:") for line in calls) == (3 if extra.get("DN2CPP_ARRAY_FULL") == "1" else 2), calls)
    check(sum(line.startswith("BUILD:") for line in calls) == (0 if extra.get("DN2CPP_SKIP_BUILD") else 3 if extra.get("DN2CPP_ARRAY_FULL") == "1" else 2), calls)
    check(calls.count("COMMIT") == images, calls)
    check(("FULL_ARRAY_PROFILE" in calls) == (extra.get("DN2CPP_ARRAY_FULL") == "1"), calls)
    if images == 0:
        check(not captures and not any(kind == "output" for kind, *_ in actions), (label, actions))
    cache = [line for line in calls if line.startswith("CACHE:") and "corelib_diff_gate|ArrayCore|" in line]
    check(len(cache) == 1 and "gates/_headerless-reference-identity.sh" in cache[0]
          and "gates/_md-readonly-byrefs.sh" in cache[0] and "array-full:" + extra.get("DN2CPP_ARRAY_FULL", "0") in cache[0], cache)
    witnesses = [args for kind, *args in actions if kind == "output" and args[0] == "witness"]
    check(len(witnesses) <= 55, (label, len(witnesses)))
    for args in witnesses:
        record_args = args[2:args.index("--count")]
        check(sum(len(arg) + 3 for arg in record_args) <= 4096, (label, args))
        check(args[-2:] == ["--policy", "crlf"], args)
    if label in ("cold", "full", "suffix"):
        contract, _ = array_contract(actions)
        check(len(contract["prefixes"]) == 34, (label, contract["prefixes"]))
        for args in witnesses:
            check(args[1].endswith("/native.stdout"), (label, "unverified observation", args))
    if label == "suffix":
        check(all("-hwy-scalar-probe/" in args[0] for args in captures), captures)

for full in ("0", "1"):
    result, calls, actions = array_run("array-cli-failure-" + full, array_prefix,
                                     {"DN2CPP_ARRAY_FULL": full, "FAKE_PREPARE_CLI_FAIL": "1"})
    check(result.returncode == 23 and calls == ["PREPARE"] and not actions, (full, result.stderr, calls, actions))

for label, argv, extra, images in (
        ("prefix", "before-default-comparison", {}, 2),
        ("quoted", "'unrecognized argument'", {}, 2),
        ("whitespace", " ", {}, 2),
        ("suite", "before-default-comparison", {"DN2CPP_SKIP_BUILD": "1"}, 2),
        ("full", "before-default-comparison", {"DN2CPP_ARRAY_FULL": "1"}, 3),
        ("warm", "before-default-comparison", {"FAKE_ARRAY_CACHE": "all"}, 0),
        ("wrapper-failure-parity", "before-default-comparison", {"FAKE_ARRAY_WRAPPER_STATUS": "2"}, 2),
        ("native-diagnostic", "before-default-comparison", {"FAKE_ARRAY_FAIL": "stderr", "FAKE_ARRAY_STAGE": "argless"}, 2),
        ("finite-diagnostic", "before-default-comparison", {"FAKE_ARRAY_FAIL": "stderr", "FAKE_ARRAY_STAGE": "argless", "FAKE_ARRAY_SUBJECT": "finite"}, 2)):
    result, calls, actions = array_run("array-argv-" + label, array_prefix, {"DN2CPP_GATE_RUN_ARGS": argv, **extra})
    check(result.returncode == 0 and calls.count("COMMIT") == images, (label, result.stderr, calls))
    check(sum(line.startswith("NATIVE:") for line in calls) == images, (label, calls))
    argless = [args for kind, *args in actions if kind == "capture" and args[0].endswith("/argless-native")]
    check(len(argless) == images and all(len(args) == 3 and args[1] == "run_bounded" for args in argless), (label, argless))
    for kind, *args in actions:
        if kind == "output" and args[0] == "witness":
            check(args[1].endswith("/argless-native.stdout"), (label, args))
    for line in calls:
        if line.startswith("CACHE:"):
            check("runargs:" + argv in line and "gates/_headerless-reference-identity.sh" in line, (label, line))
    if label == "wrapper-failure-parity":
        check(calls.count("ASSERT:status 2 2") == images and "ASSERT:status 2 0" not in calls, calls)
    if label == "warm":
        check(not any(kind in ("capture", "output") for kind, *_ in actions), actions)

for subject in ("finite", "aggregate"):
    for failure in ("native-status", "oracle-status", "both-status", "missing-stdout", "missing-stderr", "missing-status",
                    "capture-error", "missing-witness", "duplicate-witness", "nul", "cr", "newline"):
        result, calls, actions = array_run("array-failure-" + subject + "-" + failure, array_prefix,
                                          {"FAKE_ARRAY_SUBJECT": subject, "FAKE_ARRAY_FAIL": failure})
        check(result.returncode != 0, (subject, failure, result.stderr))
        check(calls.count("COMMIT") == (0 if subject == "finite" else 1), (subject, failure, calls))
    for failure in ("native-status", "both-status", "missing-stdout", "missing-stderr", "missing-status",
                    "capture-error", "missing-witness", "duplicate-witness", "nul"):
        result, calls, actions = array_run("array-argless-failure-" + subject + "-" + failure, array_prefix,
                                          {"DN2CPP_GATE_RUN_ARGS": "before-default-comparison", "FAKE_ARRAY_WRAPPER_STATUS": "2",
                                           "FAKE_ARRAY_SUBJECT": subject, "FAKE_ARRAY_FAIL": failure, "FAKE_ARRAY_STAGE": "argless"})
        check(result.returncode != 0 and calls.count("COMMIT") == (0 if subject == "finite" else 1),
              (subject, failure, result.stderr, calls))
result, calls, _ = array_run("array-argv-infrastructure-status", array_prefix,
                             {"DN2CPP_GATE_RUN_ARGS": "before-default-comparison", "FAKE_ARRAY_WRAPPER_STATUS": "124"})
check(result.returncode != 0 and "COMMIT" not in calls, (result.stderr, calls))
for failure in ("prefix", "prefix-status", "prefix-marker-missing", "prefix-marker-duplicate", "selected", "unsearched", "guard", "shared SZ address identities", "shared readonly MD reads",
                "pending indirect references rooted", "shared reference store", "shared array allocation", "shared multidimensional store", "shared MD identities"):
    result, calls, _ = array_run("array-failure-" + failure.replace(" ", "-"), array_prefix, {"FAKE_ARRAY_FAIL": failure})
    check(result.returncode != 0, (failure, result.stderr))
    check(calls.count("COMMIT") == (0 if failure in ("selected", "unsearched") else 1), (failure, calls))

for label, extra, commits in (
        ("producer", {"FAKE_ARRAY_WITNESS": "array producer and validation order end"}, 2),
        ("raw-finite", {"FAKE_ARRAY_SUBJECT": "finite", "FAKE_ARRAY_TARGET_SUFFIX": "-no-ildiet"}, 1)):
    result, calls, _ = array_run("array-full-failure-" + label, array_prefix,
                                {"DN2CPP_ARRAY_FULL": "1", "FAKE_ARRAY_FAIL": "missing-witness", **extra})
    check(result.returncode != 0 and calls.count("COMMIT") == commits, (label, result.stderr, calls))

batch_helper = array_source[array_source.index("array_core_witnesses() {"):
                            array_source.index("# Prefix runs retain their own evidence;")]
batch_records = [f'quoted "record" \\ {index} ' + "\u03b1" * 80 for index in range(120)]
for label, records, needles, count, success in (
        ("split", batch_records, batch_records, "1", True),
        ("missing", ["first"], ["first", "missing"], "1", False),
        ("duplicate", ["first", "first"], ["first"], "1", False),
        ("any", ["first", "first"], ["first"], "any", True),
        ("empty", ["first"], [], "1", False),
        ("oversize", ["x" * 5000], ["x" * 5000], "1", False)):
    script = batch_helper + "\nprintf '%s\\n' " + " ".join(map(shlex.quote, records)) + " > batch.stdout\n"
    script += "array_core_witnesses batch.stdout " + count + " " + " ".join(map(shlex.quote, needles)) + "\n"
    result, _, actions = array_run("array-batch-" + label, script)
    check((result.returncode == 0) == success, (label, result.stderr))
    batches = [args for kind, *args in actions if kind == "output"]
    if label == "split": check(len(batches) > 1, batches)
    if label in ("empty", "oversize"): check(not batches, batches)
    for args in batches:
        command = subprocess.list2cmdline([os.sys.executable, str(GATES / "gate-output.py"), *args])
        check(len(command.encode("utf-16-le")) // 2 < 12000, (label, "Windows argv too large"))
        check(args[-4:] == ["--count", count, "--policy", "crlf"], args)

# Shared helper callers retain fresh captures unless an explicit observation is
# provided. Reuse checks all receipts even when called inside a shell condition.
headerless = array_helpers["_headerless-reference-identity.sh"]
headerless_setup = r'''
mkdir -p artifacts/arraycore
_CG_APP=ArrayCore.dll
printf '%s\n' 0 > artifacts/arraycore/reused.status
: > artifacts/arraycore/reused.stderr
"$FAKE_PYTHON" "$FAKE_ARRAY_RUNNER" capture artifacts/arraycore/reused run_bounded ArrayCore
case "${FAKE_HEADERLESS_FAIL:-}" in
    status) printf '%s\n' 23 > artifacts/arraycore/reused.status ;;
    stdout|stderr|missing-status) stream=${FAKE_HEADERLESS_FAIL#missing-}; mv "artifacts/arraycore/reused.$stream" "artifacts/arraycore/saved.$stream" ;;
esac
'''
for reuse in (False, True):
    for failure in ("", "status", "stdout", "stderr", "missing-status") if reuse else ("",):
        invocation = ('headerless_reference_prefix_asserts artifacts/arraycore ArrayCore before-headerless-md-identity '
                      "'== headerless multidimensional reference identity ==' 'headerless multidimensional reference identity end'")
        if reuse: invocation += " artifacts/arraycore/reused"
        script = headerless + headerless_setup + "\nif " + invocation + "; then record HELPER_OK; else exit 27; fi\n"
        result, calls, actions = array_run("array-headerless-" + str(reuse) + "-" + failure, script,
                                          {"FAKE_HEADERLESS_FAIL": failure})
        check((result.returncode == 0) == (not failure), (reuse, failure, result.stderr))
        captures = [args for kind, *args in actions if kind == "capture"]
        check(len(captures) == (0 if failure else 2 if reuse else 3), (reuse, failure, captures))

for project in ("ReflectInvoke", "StringBuild"):
    script = headerless + headerless_setup + (
        "\nheaderless_reference_prefix_asserts artifacts/arraycore " + project + " before-headerless-md-identity "
        "'== headerless multidimensional reference identity ==' 'headerless multidimensional reference identity end'\n")
    result, _, actions = array_run("array-headerless-default-" + project, script)
    captures = [args for kind, *args in actions if kind == "capture"]
    check(result.returncode == 0 and len(captures) == 3, (project, result.stderr, captures))
    check(captures[0][-1] == "artifacts/arraycore/" + project, captures)
for failure in ("status", "stderr", "missing-status"):
    script = headerless + headerless_setup + (
        "\nif headerless_md_reference_asserts artifacts/arraycore artifacts/arraycore/reused; "
        "then record HELPER_OK; else exit 27; fi\n")
    result, _, actions = array_run("array-headerless-forward-" + failure, script, {"FAKE_HEADERLESS_FAIL": failure})
    check(result.returncode == 27 and not any(kind == "capture" for kind, *_ in actions), (failure, result.stderr, actions))


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
mkdir -p artifacts/selfhost-prim-routes-Debug/previous-run
printf 'retained route evidence\n' > artifacts/selfhost-prim-routes-Debug/previous-run/native.stdout
for reference in System.Linq System.Diagnostics.TraceSource System.Collections.Specialized \
        System.Security.Claims System.Runtime System.Console; do
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
    local args="$*" dir= name= diagnostic= suffix body leaf query collect wait backedge refusal="${FAKE_GC_REFUSAL:-}"
    local argv=("$@")
    while [ "$#" -gt 0 ]; do
        if [ "$1" = -o ]; then dir="$2"; break; fi
        shift
    done
    case "$dir" in
        artifacts/selfhost-prim-routes-*/*-out)
            name="${dir##*/}"
            name="${name%-out}"
            refusal="${FAKE_ROUTE_REFUSAL:-}"
            case "$name" in
                pointer-memberref)
                    diagnostic='RuntimeTypeIsPointerSubset.IsPointerChecks.Address: RuntimeType pointer query requires its exact MemberRef signature [chain: Program.Main]' ;;
                pointer-call-raw)
                    diagnostic='RuntimeTypeIsPointerSubset.IsPointerChecks.Direct: RuntimeType pointer query requires its exact MemberRef signature [chain: Program.Main]' ;;
                principal-missing)
                    diagnostic='default principal requires the System.Security.Claims implementation assembly' ;;
                principal-factory)
                    diagnostic='default principal requires the exact managed GenericPrincipal.GetDefaultInstance factory' ;;
                typed-gchandle-address)
                    diagnostic="Entry.Main: System.Runtime.InteropServices.GCHandle_Object::ToIntPtr: typed GCHandle<T> members are lowered only at call sites; taking a member's address is not supported, so call it from a lambda instead [chain: Entry.Main]" ;;
                *) return 97 ;;
            esac
            ;;
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
        [ "$refusal" != "$name:accepted" ] || return 0
        [ "$refusal" != "$name:diagnostic" ] || diagnostic='wrong diagnostic'
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
primitive_stdout() {
    printf '%s\n' 'mock output' 'Nullable reboxing contract end' 'Activator default construction end'
}
gc_stdout() {
    primitive_stdout
    printf '%s\n' '== GC collection overloads ==' \
        'GC collection overloads end' 'GC logical generations end' \
        'GC memory query entry points end' 'GC notification validation end' \
        'GC pause duration end' 'GC finalizer worker contract end' 'Buffer reference moves end'
}
gate_capture() {
    local prefix="$1" status=0 label mode captured_status
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
        */runtime-field-fixture|*/runtime-type-fixture)
            captured_status=$(cat "$prefix.status")
            [ "$2" = run_bounded ] && [ "$3" = dotnet ] || return 97
            dotnet "${@:4}" > "$prefix.stdout" 2> "$prefix.stderr" || status=$?
            [ "$status" != 0 ] || status=$captured_status
            printf '%s\n' "$status" > "$prefix.status" ;;
        */runtime-type-oracle|*/runtime-type-native)
            [ "${@: -1}" = runtime-type-routes ] || return 97
            if [[ "$prefix" == */runtime-type-oracle ]]; then
                [ "$4" = samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/SelfHostPrimSubset.dll ] || return 97
                [ ! -s "$4" ] || return 97
            fi
            printf 'Runtime is-pointer routes end\n' > "$prefix.stdout" ;;
        */runtime-field-boundary)
            [ "${@: -1}" = native-boundary ] || return 97
            printf '%s\n' 'field-handle-GetAttributes-direct:PlatformNotSupportedException:System.RuntimeFieldHandle.GetAttributes requires CoreCLR field descriptors. Use FieldInfo instead.' > "$prefix.stdout" ;;
        */selfhostprimsubset*/native|*/selfhostprimsubset*/oracle|*/buffer-unrewritten)
            gc_stdout > "$prefix.stdout"
            if [ "${FAKE_RUNTIME_TAINT:-}" = prefix ] && [[ "$prefix" == */native ]]; then
                printf 'wrong prefix\n' >> "$prefix.stdout"
            fi
            printf '%s\n' '== Runtime method enumeration contract ==' \
                'Runtime method enumeration contract end' 'Runtime type handle accessors end' \
                'Principal public neighbor end' 'known-constant-public-searches:True' \
                'known-constant-search-values:70:70:0:-1:-1' >> "$prefix.stdout"
            if [ "${FAKE_RUNTIME_TAINT:-}" != tail ] || [[ "$prefix" != */native ]]; then
                printf '%s\n' 'Known constant public searches end' >> "$prefix.stdout"
            fi ;;
        */gc-prefix) primitive_stdout > "$prefix.stdout" ;;
        */runtime-prefix) gc_stdout > "$prefix.stdout" ;;
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
    if [ -n "${FAKE_ROUTE_TAINT:-}" ] && [[ "$prefix" == */"$FAKE_ROUTE_TAINT" ]]; then
        printf 'wrong output\n' > "$prefix.stdout"
    fi
    if [ -n "${FAKE_ROUTE_STDERR:-}" ] && [[ "$prefix" == */"$FAKE_ROUTE_STDERR" ]]; then
        printf 'unexpected stderr\n' > "$prefix.stderr"
    fi
    if [[ "$prefix" == */runtime-type-native ]]; then
        case "${FAKE_ROUTE_BYTES:-}" in
            nul) printf '\000' >> "$prefix.stdout" ;;
            cr) printf '\r' >> "$prefix.stdout" ;;
            newline) printf '\n' >> "$prefix.stdout" ;;
        esac
    fi
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
        artifacts/selfhost-prim-routes-*/*:*|*/runtime-type-*:*|*/runtime-field-*:*|*/gc-*:*|*/buffer-*:*|*/gen2-retained.*:*|*:witness*'GC collection overloads end'*|*:witness*'Runtime method enumeration contract end'*|*:prefix*'/gc-prefix.stdout'*|*:prefix*'/runtime-prefix.stdout'*)
            "$FAKE_PYTHON" "$FAKE_OUTPUT_HELPER" "$@" ;;
    esac
}
git() {
    [ "$*" = 'ls-files -co --exclude-standard -- gates/fixtures/ldftn-local gates/fixtures/default-principal' ] || return 97
    printf '%s\n' gates/fixtures/ldftn-local/RuntimeTypeIsPointerFixture.cs \
        gates/fixtures/ldftn-local/RuntimeFieldHandleFixture.cs \
        gates/fixtures/ldftn-local/DefaultPrincipalFixture.cs \
        gates/fixtures/default-principal/ImplicitPrincipalProbe.csproj
}
dotnet() {
    record "FIXTURE:$*"
    [ "$1" = "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" ] && [ -f "$1" ] || return 97
    [ "${FAKE_ROUTE_FIXTURE_FAIL:-}" != "$2" ] || return 23
    case "$2" in
        --runtime-field-handle|--runtime-type-is-pointer-aggregate|\
        --runtime-type-is-pointer-negative-address-return|--runtime-type-is-pointer-negative-call-class-return|\
        --default-principal)
            [ "$#" = 4 ] && [ -f "$3" ] && [ -f "$4" ] || return 97
            if [ "$2" = --runtime-field-handle ]; then
                [[ "$3" == */app/SelfHostPrimSubset.dll ]] || return 97
                printf 'private-field\n' >> "$3"
                printf 'field-handle-sites=GetAttributes-direct\n'
            elif [ "$2" = --runtime-type-is-pointer-aggregate ]; then
                [[ "$3" == */app/SelfHostPrimSubset.dll ]] || return 97
                grep -Fxq private-field "$3" || return 97
                printf 'private-pointer\n' >> "$3"
                printf 'runtime-type-is-pointer-entry:Program.Main\n'
            fi ;;
        --default-principal-target-copy)
            [ "$#" = 5 ] && [ -f "$3" ] && [ "$5" = wrong-return ] || return 97
            cp "$3" "$4" ;;
        *) return 97 ;;
    esac
}
gate_cache_check() {
    record "CACHE:$*"
    if [[ "$1" == artifacts/selfhost-prim-routes-* ]]; then
        [ "${FAKE_ROUTES_CACHE_HIT:-${FAKE_CACHE_HIT:-0}}" = 1 ]
    else
        [ "${FAKE_CACHE_HIT:-0}" = 1 ]
    fi
}
if [ -n "${DN2CPP_SKIP_BUILD:-}" ]; then
    fake_build samples/dotnet/SelfHostPrimSubset/SelfHostPrimSubset.csproj
    fake_build gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
    fake_build gates/fixtures/gen2-callback/interpreter/Patch/Gen2CallbackPatch.csproj
    prepare_gen2_projects
    if [ "${FAKE_MISSING_LDFTN:-0}" = 1 ]; then
        mv gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll fixture-unavailable.dll
    fi
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
        ("cold", {}, (7, 4, 5)),
        ("suite", {"DN2CPP_SKIP_BUILD": "1"}, (7, 4, 2)),
        ("suffix", {"DN2CPP_OUT_SUFFIX": "-probe", "HIGHWAY": "1"}, (7, 4, 5)),
        ("scalar", {"SCALAR": "1"}, (7, 4, 5)),
        ("routes-warm", {"FAKE_ROUTES_CACHE_HIT": "1"}, (7, 4, 4)),
        ("aggregate-warm", {"FAKE_CACHE_HIT": "1", "FAKE_ROUTES_CACHE_HIT": "0"}, (2, 1, 4)),
        ("warm", {"FAKE_CACHE_HIT": "1"}, (2, 1, 3))):
    result, calls = run("selfhost-" + label, selfhost_stubs + selfhost, {**selfhost_env, **extra})
    check(result.returncode == 0, (label, result.returncode, result.stderr))
    work = root / ("selfhost-" + label)
    check((work / "artifacts/selfhost-prim-routes-Debug/previous-run/native.stdout").read_bytes()
          == b"retained route evidence\n", label)
    check((work / "samples/dotnet/SelfHostPrimSubset/bin/Debug/net10.0/SelfHostPrimSubset.dll").read_bytes()
          == b"", (label, "shared build output was rewritten"))
    emit = [line for line in calls if line.startswith("EMIT:")]
    native = [line for line in calls if line.startswith("NATIVE:")]
    builds = [line for line in calls if line.startswith("BUILD:")]
    captures = [line for line in calls if line.startswith("CAPTURE:")]
    check((len(emit), len(native), len(builds)) == expected, (label, emit, native, builds))
    check(sum(line == "PREPARE" for line in calls) == 1, calls)
    check(not any("DelegateCallableBodies.csproj" in line for line in calls), calls)
    check(any("/app/SelfHostPrimSubset.dll -r " in line for line in emit), emit)
    check(any("System.Security.Claims.dll" in line and "DefaultStructCtorLib.dll" in line
              for line in emit), emit)
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
    aggregate_cache = [line for line in cache if not line.startswith("CACHE:artifacts/selfhost-prim-routes-")]
    check(len(aggregate_cache) == 1, aggregate_cache)
    for dependency in ("RuntimeTypeIsPointerFixture.cs", "RuntimeFieldHandleFixture.cs",
                       "runtime-type-routes:aggregate,public-wrapper-oracle,fieldhandle-boundary",
                       "runtime-type-oracle.stdout", "runtime-type-oracle.stderr"):
        check(dependency in aggregate_cache[0], (label, dependency, aggregate_cache))
    route_oracle = [line for line in captures if "/runtime-type-oracle:" in line]
    check(len(route_oracle) == 1 and route_oracle[0].endswith(
        "run_bounded dotnet " + raw_base + ".dll runtime-type-routes"), route_oracle)
    field_fixture = next(line for line in captures if " --runtime-field-handle " in line)
    route_fixture = next(line for line in captures if " --runtime-type-is-pointer-aggregate " in line)
    aggregate_emit = next(line for line in emit if "/app/SelfHostPrimSubset.dll -r " in line)
    check(calls.index(route_oracle[0]) < calls.index(delegate_fixture) < calls.index(buffer_fixture)
          < calls.index(field_fixture) < calls.index(route_fixture) < calls.index(aggregate_emit), captures)
    check(sum("LdftnLocalFixture.csproj" in line for line in builds) == (0 if label == "suite" else 1), builds)
    route_cache = [line for line in cache if line.startswith("CACHE:artifacts/selfhost-prim-routes-")]
    check(len(route_cache) == 1, route_cache)
    for dependency in ("flags:--auto-ref,--no-ildiet", "DefaultStructCtorLib.dll", "RuntimeTypeIsPointerFixture.cs",
                       "RuntimeFieldHandleFixture.cs", "DefaultPrincipalFixture.cs", "ImplicitPrincipalProbe.csproj"):
        check(dependency in route_cache[0], (label, dependency, route_cache))
    route_native = [line for line in native if line.endswith(":RuntimeTypeRoutes")]
    check(not route_native and not any("selfhost-prim-routes-" in line for line in emit), (native, emit))
    route_refusals = [line for line in calls if line.startswith("REJECT:") and "artifacts/selfhost-prim-routes-" in line]
    fixtures = [line.split()[1] for line in calls if line.startswith("FIXTURE:")]
    positive_fixtures = ["--runtime-field-handle", "--runtime-type-is-pointer-aggregate"]
    if label in ("warm", "routes-warm"):
        check(not route_refusals and fixtures == positive_fixtures, calls)
        check(calls.count("HIT") == (2 if label == "warm" else 1), calls)
    else:
        check([line.split(":")[1] for line in route_refusals] == ["pointer-memberref", "pointer-call-raw",
              "principal-missing", "principal-factory", "typed-gchandle-address"], route_refusals)
        check(" --auto-ref " in route_refusals[0] and "--no-ildiet" not in route_refusals[0], route_refusals)
        check(" --auto-ref --no-ildiet " in route_refusals[1], route_refusals)
        check(" --auto-ref " not in route_refusals[2] and "System.Runtime.dll" in route_refusals[2]
              and "System.Console.dll" in route_refusals[2] and "System.Security.Claims.dll" not in route_refusals[2], route_refusals)
        check("/claims/copy/System.Security.Claims.dll" in route_refusals[3], route_refusals)
        check("gates/fixtures/default-principal/bin/Debug/net10.0/ImplicitPrincipalProbe.dll --auto-ref" in route_refusals[4], route_refusals)
        check(sum(line.startswith("ASSERT:match-record artifacts/selfhost-prim-routes-") for line in calls) == 5, calls)
        check(fixtures == positive_fixtures + [
              "--runtime-type-is-pointer-negative-address-return", "--runtime-type-is-pointer-negative-call-class-return",
              "--default-principal", "--default-principal-target-copy"], fixtures)
    check(calls.count("COMMIT") == (0 if label == "warm" else 1 if label in ("routes-warm", "aggregate-warm") else 2), calls)
    if label in ("warm", "aggregate-warm"):
        check("HIT" in calls, calls)
        check(not any("delegate-refusals " in line for line in calls if line.startswith("CAPTURE:")), calls)
        check(not any("gen2-retained" in line for line in native + emit), calls)
        check(not any("/runtime-type-native:" in line or "/runtime-field-boundary:" in line for line in captures), captures)
        continue
    target = next(line.split(":")[1] for line in native if line.endswith(":SelfHostPrimSubset"))
    check(any(line.endswith(f"{target}/SelfHostPrimSubset runtime-type-routes") for line in captures), captures)
    check(any(line.endswith(f"{target}/SelfHostPrimSubset native-boundary") for line in captures), captures)
    for stream in ("stdout", "stderr"):
        check(f"ASSERT:compare {target}/runtime-type-oracle.{stream} {target}/runtime-type-native.{stream}" in calls, calls)
    check(any(line.startswith(f"ASSERT:byte-witness {target}/runtime-field-boundary.stdout ")
              and "CoreCLR field descriptors" in line for line in calls), calls)
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
    check(any(line.endswith(f"{target}/app/SelfHostPrimSubset.dll before-runtime-primitives")
              for line in captures), captures)
    check(f"ASSERT:prefix {target}/native.stdout {target}/runtime-prefix.stdout --before == Runtime method enumeration contract == --policy crlf" in calls, calls)
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
    refusals = [line for line in calls if line.startswith("REJECT:") and "artifacts/selfhost-prim-routes-" not in line]
    check([line.split(":")[1] for line in refusals] == ["buffer-negative-internal", "buffer-negative-batch",
          "gc-missing-corelib-collect", "gc-missing-corelib-memory", "gen2-newobj", "gen2-memberref"], refusals)
    check(sum(line == "ASSERT:status 2 2" for line in calls) == (6 if label == "routes-warm" else 11), calls)
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
        ("retained-status", {"FAKE_STATUS_FAIL": "retained-only/native"}),
        ("runtime-prefix-status", {"FAKE_STATUS_FAIL": "/runtime-prefix"}),
        ("runtime-prefix-bytes", {"FAKE_GC_TAINT": "/runtime-prefix"}),
        ("runtime-native-prefix", {"FAKE_RUNTIME_TAINT": "prefix"}),
        ("runtime-tail", {"FAKE_RUNTIME_TAINT": "tail"})):
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

for label, extra, native_count in (
        ("field-fixture", {"FAKE_ROUTE_FIXTURE_FAIL": "--runtime-field-handle"}, 0),
        ("field-fixture-status", {"FAKE_STATUS_FAIL": "/runtime-field-fixture"}, 0),
        ("field-fixture-bytes", {"FAKE_ROUTE_TAINT": "runtime-field-fixture"}, 0),
        ("field-fixture-stderr", {"FAKE_ROUTE_STDERR": "runtime-field-fixture"}, 0),
        ("fixture", {"FAKE_ROUTE_FIXTURE_FAIL": "--runtime-type-is-pointer-aggregate"}, 0),
        ("fixture-status", {"FAKE_STATUS_FAIL": "/runtime-type-fixture"}, 0),
        ("fixture-bytes", {"FAKE_ROUTE_TAINT": "runtime-type-fixture"}, 0),
        ("fixture-stderr", {"FAKE_ROUTE_STDERR": "runtime-type-fixture"}, 0),
        ("oracle-status", {"FAKE_STATUS_FAIL": "/runtime-type-oracle"}, 0),
        ("oracle-status-warm", {"FAKE_STATUS_FAIL": "/runtime-type-oracle", "FAKE_CACHE_HIT": "1"}, 0),
        ("suite-missing-fixture", {"DN2CPP_SKIP_BUILD": "1", "FAKE_MISSING_LDFTN": "1"}, 0),
        ("emit", {"FAKE_EMIT_FAIL": "artifacts/selfhostprimsubset"}, 0),
        ("native-status", {"FAKE_STATUS_FAIL": "/runtime-type-native"}, 3),
        ("parity-bytes", {"FAKE_ROUTE_TAINT": "runtime-type-oracle"}, 3),
        ("parity-stderr", {"FAKE_ROUTE_STDERR": "runtime-type-native"}, 3),
        ("parity-nul", {"FAKE_ROUTE_BYTES": "nul"}, 3),
        ("parity-cr", {"FAKE_ROUTE_BYTES": "cr"}, 3),
        ("parity-newline", {"FAKE_ROUTE_BYTES": "newline"}, 3),
        ("witness", {"FAKE_ROUTE_TAINT": "runtime-type-native"}, 3),
        ("boundary-status", {"FAKE_STATUS_FAIL": "/runtime-field-boundary"}, 3),
        ("boundary-bytes", {"FAKE_ROUTE_TAINT": "runtime-field-boundary"}, 3)):
    result, calls = run("selfhost-routes-failure-" + label, selfhost_stubs + selfhost,
                        {**selfhost_env, **extra})
    check(result.returncode != 0, (label, result.stderr))
    check("COMMIT" not in calls and not any(line.startswith("REJECT:") and "artifacts/selfhost-prim-routes-" in line for line in calls), (label, calls))
    native = [line for line in calls if line.startswith("NATIVE:")]
    check(len(native) == native_count, (label, native))
    if label.startswith("oracle-status"):
        check("HIT" not in calls, calls)

for name in ("pointer-memberref", "pointer-call-raw", "principal-missing", "principal-factory", "typed-gchandle-address"):
    for failure in ("accepted", "diagnostic"):
        result, calls = run("selfhost-routes-" + name + "-" + failure, selfhost_stubs + selfhost,
                            {**selfhost_env, "FAKE_ROUTE_REFUSAL": name + ":" + failure})
        check(result.returncode != 0 and calls.count("COMMIT") == 1, (name, failure, calls))
        check(sum(line.startswith("NATIVE:") for line in calls) == 4, calls)
        rejected = [line.split(":")[1] for line in calls if line.startswith("REJECT:")]
        check(rejected[-1] == name, (name, failure, rejected))

(root / "results.json").write_text(json.dumps(results, indent=2) + "\n", encoding="utf-8")
print(f"short-gate-builds: {checks} checks passed; evidence: {root}")
