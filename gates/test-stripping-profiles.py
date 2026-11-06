#!/usr/bin/env python3
"""Finite selection tests only: no SDK, transpilation, or native execution."""
import ast
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile

GATES = Path(__file__).resolve().parent
REPO = GATES.parent
ENV = {key: value for key, value in os.environ.items() if not key.startswith(("GIT_", "DN2CPP_"))}
BASH = os.environ.get("DN2CPP_TEST_BASH", "bash")
checks = 0


def check(condition, message):
    global checks
    if not condition:
        raise AssertionError(message)
    checks += 1


def shell(source, cwd, extra=None):
    return subprocess.run([BASH, "-c", source], cwd=cwd,
                          env={**ENV, **(extra or {})}, capture_output=True, text=True, encoding="utf-8")


driver = GATES / "fixtures/ildiet-metadata-validation/check-stripping.py"
driver_source = driver.read_text(encoding="utf-8")
# Execute the actual argument/profile parser, ending before its first mkdir.
prefix = driver_source[:driver_source.index("directory.mkdir(parents=True, exist_ok=True)")]
parser = "import sys\nsys.argv = " + "ARGS" + "\n__file__ = " + repr(str(driver)) + "\n"
parser_tail = "\nprint(__import__('json').dumps({'levels': levels, 'names': names, 'extended': extended_levels, 'modes': modes}))\n"
for suffix, valid, extended, modes in [
        ([], True, ["high"], ["raw", "full"]),
        (["--with-medium"], True, ["high", "medium"], ["raw", "full"]),
        (["--representative"], True, ["high"], ["full"]),
        (["--medium"], False, None, None),
        (["--with-medium", "--with-medium"], False, None, None),
        (["--with-medium", "--representative"], False, None, None)]:
    for cli in ([], ["cli"]):
        argv = [str(driver), "ildiet", "probe", "transpiler", "output", *cli, *suffix]
        result = subprocess.run([sys.executable, "-c", parser.replace("ARGS", repr(argv)) + prefix + parser_tail],
                                cwd=REPO, env=ENV, capture_output=True, text=True, encoding="utf-8")
        check((result.returncode == 0) == valid, (suffix, cli, result.returncode, result.stderr))
        if valid:
            data = json.loads(result.stdout)
            check(data["levels"] == ["high", "medium"] and data["names"] == ["default", "high", "medium"], data)
            check(data["extended"] == extended and data["modes"] == modes, data)


preserve = (GATES / "build-and-run-preserve-control.sh").read_text(encoding="utf-8")
profile_start = preserve.index('case "${DN2CPP_PRESERVE_CONTROL_FULL:-0}" in')
profile_end = preserve.index('echo "preserve-control-stripping=basic:high,medium extended:$stripping_levels"', profile_start)
profile_end = preserve.index("\n", profile_end) + 1
profile = preserve[profile_start:profile_end]
section_start = preserve.index('echo "== Basic High/Medium retention and inert initializers =="')
section_end = preserve.index('\nif ! preserve_full; then', section_start)
basic_section = preserve[section_start:section_end]
extended_start = preserve.index('for fixture in retained called; do', section_end)
extended_end = preserve.index('gate_python gates/fixtures/ildiet-metadata-validation/check.py', extended_start)
section = basic_section + preserve[extended_start:extended_end]
check(section_start < preserve.index("    exit 0", section_start), "basic Medium must precede representative return")
stubs = r'''
set -euo pipefail
CONFIG=Debug
TFM=net10.0
ILD_DLL=ildiet
CLI_BIN=cli
CLI_DLL=cli/dn2cpp.dll
EXE_EXT=
build_gate_proj() { :; }
gate_python() { printf 'DRIVER:%s\n' "$*"; }
compile_console() { printf 'NATIVE:%s\n' "$1"; mkdir -p "$1"; }
gate_capture_ok() { :; }
gate_output() { :; }
run_bounded() { :; }
'''
expected_high = {
    f"emission-{family}-{kind}-high-{axis}"
    for family in ("intercept", "buffer-moves", "gc-memory", "gc-generations")
    for kind in ("retained", "called") for axis in ("raw", "full")
}
expected_high |= {f"emission-{family}-managed-{axis}-high"
                  for family in ("globalization", "kernel32") for axis in ("raw", "full")}
expected_high |= {f"emission-kernel32-primitive-{axis}-high" for axis in ("raw", "full")}
expected_medium = {f"emission-{fixture}-{axis}"
                   for fixture in ("medium", "retained-equality", "forwarded-attribute") for axis in ("raw", "full")}
expected_medium |= {name.replace("-high-", "-medium-") for name in expected_high if "-high-" in name}
expected_medium |= {f"emission-{family}-{kind}-{axis}"
                   for family, kinds in (("globalization", ("retained", "called", "alias", "managed")),
                                         ("kernel32", ("retained", "called", "managed", "other-owner")))
                   for kind in kinds for axis in ("raw", "full")}
expected_medium |= {f"emission-kernel32-primitive-{axis}-medium" for axis in ("raw", "full")}
expected_basic = {f"emission-{fixture}-full" for fixture in ("medium", "retained-equality", "forwarded-attribute")}
expected_basic |= {f"emission-intercept-{kind}-{level}-full" for kind in ("retained", "called") for level in ("high", "medium")}
expected_high |= {name for name in expected_medium if name.startswith(("emission-medium-", "emission-retained-equality-", "emission-forwarded-attribute-", "emission-intercept-"))}
with tempfile.TemporaryDirectory(prefix="dn2cpp-stripping-profile-test-") as temp:
    result = shell(stubs + profile + basic_section, temp,
                   {"DN2CPP_PRESERVE_CONTROL_FULL": "0", "DN2CPP_PRESERVE_CONTROL_MEDIUM": "0"})
    check(result.returncode == 0, result.stderr)
    actual = [Path(line.split(":", 1)[1]).name for line in result.stdout.splitlines() if line.startswith("NATIVE:")]
    check(set(actual) == expected_basic and len(actual) == len(expected_basic), actual)
    check("--representative" in result.stdout, result.stdout)
    for value, expected in [("0", expected_high), ("1", expected_high | expected_medium)]:
        result = shell(stubs + profile + section, temp, {"DN2CPP_PRESERVE_CONTROL_FULL": "1", "DN2CPP_PRESERVE_CONTROL_MEDIUM": value})
        check(result.returncode == 0, (value, result.stderr))
        actual = [Path(line.split(":", 1)[1]).name for line in result.stdout.splitlines() if line.startswith("NATIVE:")]
        check(set(actual) == expected and len(actual) == len(expected), (value, actual, expected))
        commands = [line for line in result.stdout.splitlines() if line.startswith("DRIVER:")]
        check(len(commands) == 1 and ("--with-medium" in commands[0]) == (value == "1"), commands)
    for bad in ("true", "2", " ", "high medium"):
        result = shell(stubs + profile + section, temp, {"DN2CPP_PRESERVE_CONTROL_FULL": "1", "DN2CPP_PRESERVE_CONTROL_MEDIUM": bad})
        check(result.returncode != 0 and "DRIVER:" not in result.stdout and "NATIVE:" not in result.stdout, bad)

    original = Path(temp) / "samples/dotnet/SelfHostPrimSubset/bin/Debug/net10.0"
    original.mkdir(parents=True)
    for name in ("SelfHostPrimSubset.dll", "DefaultStructCtorLib.dll", "SelfHostPrimSubset.runtimeconfig.json"):
        (original / name).write_text("data-only fixture, never executed\n", encoding="utf-8")
    known_stubs = r'''
set -euo pipefail
CONFIG=Debug
TFM=net10.0
EXE_EXT=
WASM=0
DN2CPP_KNOWN_CONSTANT_MODES='all reflection'
DN2CPP_KNOWN_CONSTANT_AXES='raw diet'
source "$KNOWN_HELPER"
resolve_net10_corelib() { printf 'corelib\n'; }
search_values_shuffle_gate() { printf 'SHUFFLE\n'; }
known_constant_asserts() { :; }
known_constant_emitted_asserts() { :; }
dotnet() { :; }
gate_capture_ok() { mkdir -p "$(dirname "$1")"; : > "$1.stdout"; }
gate_output() { :; }
compile_console() { :; }
invoke_cli() { printf 'CLI:%s\n' "$*"; }
known_constant_routes_gate "$CASE_ROOT"
'''
    for index, (value, expected) in enumerate([(None, ["high"]), ("", ["high"]), ("High Medium", ["high", "medium"]),
                                                ("Medium", ["medium"]), ("low", None), (" ", None),
                                                ("\n", None), ("*", None), ("High *", None),
                                                ("High\nMedium", ["high", "medium"]),
                                                ("mEdIuM HiGh", ["medium", "high"])]):
        extra = {"KNOWN_HELPER": (GATES / "_known-constant.sh").as_posix(), "CASE_ROOT": (Path(temp) / ("known-" + str(index))).as_posix()}
        source = known_stubs
        if value is None:
            source = "unset DN2CPP_KNOWN_CONSTANT_LEVELS\n" + source
        else:
            extra["DN2CPP_KNOWN_CONSTANT_LEVELS"] = value
        result = shell(source, temp, extra)
        if expected is None:
            check(result.returncode != 0 and "CLI:" not in result.stdout and "SHUFFLE" not in result.stdout, (value, result.stdout))
        else:
            check(result.returncode == 0, (value, result.stderr))
            actual = re.findall(r"--stripping-level (\w+)", result.stdout)
            check(sorted(actual) == sorted(expected * 4), (value, actual))

premerge = (GATES / "pre-merge.sh").read_text(encoding="utf-8")
force_start = premerge.index("export DN2CPP_KNOWN_CONSTANT_LEVELS=High\n")
force = premerge[force_start:premerge.index("\n\n", force_start)]
argv_match = re.search(r"^premerge_argv\(\) \{\n.*?^\}", premerge, re.M | re.S)
check(argv_match is not None, "actual premerge_argv missing")
for inherited in ({"DN2CPP_KNOWN_CONSTANT_LEVELS": "Medium", "DN2CPP_PRESERVE_CONTROL_MEDIUM": "1"},
                  {"DN2CPP_KNOWN_CONSTANT_LEVELS": "bad", "DN2CPP_PRESERVE_CONTROL_MEDIUM": "bad"}):
    source = ('set -euo pipefail\n' + force + '\nREPO=repo\nSKIP_GODOT=1\nSKIP_XCODE=1\n'
              'DN2CPP_INCLUDE_CULTURE=0\nDN2CPP_PREMERGE_NO_CAFFEINATE=1\n' + argv_match.group() +
              '\npremerge_argv Debug logs\nprintf "%s\\n" "${PREMERGE_ARGV[@]}"\n')
    result = shell(source, REPO, inherited)
    check(result.returncode == 0, result.stderr)
    values = result.stdout.splitlines()
    check(values.count("DN2CPP_KNOWN_CONSTANT_LEVELS=High") == 1 and values.count("DN2CPP_PRESERVE_CONTROL_MEDIUM=0") == 1, values)
check("printf 'stripping: default High plus basic High/Medium retention; extended Medium API matrices not selected\\n'" in premerge,
      "receipt omits actual basic High/Medium scope")
check('note "stripping: default High plus basic High/Medium retention; extended Medium API matrices not selected."' in premerge,
      "plan omits actual basic High/Medium scope")

# Every expensive profile rejects malformed selection and stays opt-in in pre-merge.
profiles = {
    "datetime": "DN2CPP_DATETIME_FULL", "array-core": "DN2CPP_ARRAY_FULL",
    "manifest-resources": "DN2CPP_MANIFEST_RESOURCES_FULL",
    "preserve-control": "DN2CPP_PRESERVE_CONTROL_FULL",
    "reflect-invoke": "DN2CPP_REFLECT_INVOKE_FULL", "reflect-types": "DN2CPP_REFLECT_TYPES_FULL",
    "transpiler-limits": "DN2CPP_TRANSPILER_LIMITS_FULL", "emit-order-stability": "DN2CPP_EMIT_ORDER_FULL",
    "math-subset": "DN2CPP_MATH_FULL", "marshal-pinning": "DN2CPP_MARSHAL_FULL",
    "http-get": "DN2CPP_HTTP_FULL", "vector-probe": "DN2CPP_VECTOR_FULL",
    "selfhost-prim-subset": "DN2CPP_SELFHOST_PRIM_FULL", "vector-value-wasm": "DN2CPP_VECTOR_FULL",
    "hotupdate-subset": "DN2CPP_HOTUPDATE_FULL", "threading-primitives": "DN2CPP_THREADING_FULL",
    "string-core": "DN2CPP_STRING_FULL", "string-interp": "DN2CPP_STRING_INTERP_FULL",
    "decimal-ops": "DN2CPP_DECIMAL_FULL", "dict-collections": "DN2CPP_DICT_FULL",
    "custom-async-task": "DN2CPP_CUSTOM_ASYNC_TASK_FULL",
    "exception-message-subset": "DN2CPP_EXCEPTION_MESSAGE_FULL",
    "multiassembly": "DN2CPP_MULTIASSEMBLY_FULL",
    "trim-reflection": "DN2CPP_TRIM_REFLECTION_FULL",
    "obfuscation": "DN2CPP_OBFUSCATION_FULL",
    "wasm-console": "DN2CPP_WASM_CONSOLE_FULL",
    "platform-isa-native": "DN2CPP_PLATFORM_ISA_FULL",
    "default-culture": "DN2CPP_DEFAULT_CULTURE_FULL",
}
for gate, flag in profiles.items():
    source = (GATES / ("build-and-run-" + gate + ".sh")).read_text(encoding="utf-8")
    if gate == "platform-isa-native":
        source += (GATES / "_platform_isa.sh").read_text(encoding="utf-8")
    start = source.index('case "${' + flag + ':-0}" in')
    selection = source[start:source.index("esac", start) + len("esac")]
    for value, valid in [(None, True), ("0", True), ("1", True), ("true", False), ("2", False), (" ", False)]:
        setup = "unset " + flag + "\n" if value is None else ""
        result = shell("set -euo pipefail\n" + setup + selection, REPO, {} if value is None else {flag: value})
        check((result.returncode == 0) == valid, (gate, value, result.stderr))
    check("export " + flag + "=0" in force.splitlines(), (gate, "pre-merge did not pin profile"))
    check('"' + flag + '=0"' in argv_match.group(), (gate, "child profile not pinned"))
    check("skipped (not selected)" in source, (gate, "unselected coverage not reported"))

for full, medium, valid in [("0", "0", True), ("0", "1", False), ("1", "1", True)]:
    result = shell("set -euo pipefail\n" + profile, REPO,
                   {"DN2CPP_PRESERVE_CONTROL_FULL": full, "DN2CPP_PRESERVE_CONTROL_MEDIUM": medium})
    check((result.returncode == 0) == valid, (full, medium, result.stderr))

# Compare the actual representative emit-order branch with lightweight callbacks.
emit_source = (GATES / "build-and-run-emit-order-stability.sh").read_text(encoding="utf-8")
emit_start = emit_source.index('if [ "$DN2CPP_EMIT_ORDER_FULL" = 0 ]; then')
emit_end = emit_source.index("\n# project |", emit_start)
with tempfile.TemporaryDirectory(prefix="dn2cpp-emit-profile-test-") as temp:
    (Path(temp) / "System.Collections.dll").touch()
    emit_stubs = r'''
set -euo pipefail
CONFIG=Debug TFM=net10.0 OUT=artifacts/emit-test corelib=corelib bcl="$PWD"
build_proj() { :; }
_gate_cli_hash() { printf cli; }
gate_cache_check() { return 1; }
invoke_cli() { printf 'EMIT:%s\n' "$*"; }
native_path() { printf '%s\n' "$1"; }
compare_outputs() { printf 'COMPARE:%s\n' "$*"; }
gate_cache_commit() { printf 'COMMIT\n'; }
'''
    for value, expected in [("0", 3), ("1", 0)]:
        result = shell(emit_stubs + emit_source[emit_start:emit_end], temp, {"DN2CPP_EMIT_ORDER_FULL": value})
        check(result.returncode == 0, result.stderr)
        check(sum(line.startswith("EMIT:") for line in result.stdout.splitlines()) == expected, result.stdout)
        check(sum(line.startswith("COMPARE:") for line in result.stdout.splitlines()) == (2 if expected else 0), result.stdout)

# Representative ArrayFiniteSearch still verifies isolation with one native image.
array_source = (GATES / "build-and-run-array-core.sh").read_text(encoding="utf-8")
array_start = array_source.index('(\n    DN2CPP_SAMPLE_PROJECT_DIR=samples/dotnet/ArrayCore')
array_end = array_source.index('\ncorelib_diff_gate ArrayCore System.Collections', array_start)
array_branch = array_source[array_start:array_end] + "\ncorelib_diff_gate ArrayCore System.Collections\n"
for value, expected in [("0", 2), ("1", 3)]:
    result = shell("set -euo pipefail\ncorelib_diff_gate() { printf 'BUILD:%s\\n' \"$*\"; }\n" + array_branch,
                   REPO, {"DN2CPP_ARRAY_FULL": value})
    check(result.returncode == 0, result.stderr)
    check(sum(line.startswith("BUILD:") for line in result.stdout.splitlines()) == expected, result.stdout)

# A default run never calls the DateTime private fixture matrix.
datetime_source = (GATES / "build-and-run-datetime.sh").read_text(encoding="utf-8")
datetime_start = datetime_source.index('    if [ "$DN2CPP_DATETIME_FULL" = 1 ]; then',
                                       datetime_source.index('gate_extra_asserts()'))
datetime_end = datetime_source.index('\n    fi', datetime_start) + len('\n    fi')
for value, expected in [("0", False), ("1", True)]:
    result = shell("set -euo pipefail\nout=out\ndatetime_family_focused_gate() { printf 'DETAILS\\n'; }\n" +
                   datetime_source[datetime_start:datetime_end], REPO, {"DN2CPP_DATETIME_FULL": value})
    check(result.returncode == 0 and ("DETAILS" in result.stdout) == expected, result.stderr)

# The full High profile retains the address-only regressions, and every helper
# receives the explicit selection rather than falling back to its full profile.
for name in ("intercept_retention", "buffer_moves", "globalization_retention", "gc_memory", "gc_generations"):
    ast.parse((driver.parent / (name + ".py")).read_text(encoding="utf-8"))
check("validate_intercept_retention(probe, cli, original, directory, levels=levels, modes=modes)" in driver_source,
      "basic callable intercept checks must use both levels and the selected axes")
for function in ("validate_buffer_moves", "validate_globalization", "validate_kernel32",
                 "validate_gc_memory", "validate_gc_generations"):
    check(function + "(probe, cli, original, directory, levels=extended_levels)" in driver_source, function)
check('assert files == defaults, "explicit High changed generated C++"' in driver_source, "High byte comparison removed")
check('assert before.read_bytes() == (publications["high"] / before.name).read_bytes()' in driver_source,
      "High DLL comparison removed")
# Layering base regressions must preserve the optional full vector callback and
# propagate its failure before recording later regression assertions.
for status in (0, 17):
    code = r'''
set -euo pipefail
_CG_APP=app
log=""
gate_extra_asserts() { printf 'PROFILE\n'; return "$PROFILE_STATUS"; }
source "$VECTOR_HELPER"
vector_storage_asserts() { printf 'STORAGE\n'; }
gate_capture_ok() { :; }
gate_output() { :; }
gate_extra_asserts output
'''
    result = shell(code, REPO, {"VECTOR_HELPER": str(GATES / "_vector-regression-checks.sh"), "PROFILE_STATUS": str(status)})
    check(result.returncode == status, (status, result.stderr))
    check(result.stdout.splitlines() == (["PROFILE", "STORAGE"] if status == 0 else ["PROFILE"]), result.stdout)
code = code.replace("return \"$PROFILE_STATUS\";", "false; printf 'UNREACHABLE\\n';")
result = shell(code, REPO, {"VECTOR_HELPER": str(GATES / "_vector-regression-checks.sh"), "PROFILE_STATUS": "0"})
check(result.returncode != 0 and result.stdout.splitlines() == ["PROFILE"],
      ("a preceding profile assertion must retain errexit", result.stdout, result.stderr))
print(f"stripping-profile-controls: {checks} passed; basic default {len(expected_basic)}, full High {len(expected_high)}, full with Medium {len(expected_high | expected_medium)} native selections")
