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
ENV = {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}
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
parser_tail = "\nprint(__import__('json').dumps({'levels': levels, 'names': names, 'medium': include_medium}))\n"
for suffix, valid, expected in [([], True, ["high"]), (["--with-medium"], True, ["high", "medium"]),
                                (["--medium"], False, None), (["--with-medium", "--with-medium"], False, None)]:
    argv = [str(driver), "ildiet", "probe", "transpiler", "output", "cli", *suffix]
    result = subprocess.run([sys.executable, "-c", parser.replace("ARGS", repr(argv)) + prefix + parser_tail],
                            cwd=REPO, env=ENV, capture_output=True, text=True, encoding="utf-8")
    check((result.returncode == 0) == valid, (suffix, result.returncode, result.stderr))
    if valid:
        data = json.loads(result.stdout)
        check(data["levels"] == expected and data["names"] == ["default", *expected], data)
for suffix in ([], ["--with-medium"]):
    argv = [str(driver), "ildiet", "probe", "transpiler", "output", *suffix]
    result = subprocess.run([sys.executable, "-c", parser.replace("ARGS", repr(argv)) + prefix + parser_tail],
                            cwd=REPO, env=ENV, capture_output=True, text=True, encoding="utf-8")
    check(result.returncode == 0, (suffix, result.stderr))
    check(json.loads(result.stdout)["levels"] == (["high", "medium"] if suffix else ["high"]), result.stdout)


preserve = (GATES / "build-and-run-preserve-control.sh").read_text(encoding="utf-8")
profile_start = preserve.index('case "${DN2CPP_PRESERVE_CONTROL_MEDIUM:-0}" in')
profile_end = preserve.index('echo "preserve-control-stripping=$stripping_levels"', profile_start)
profile_end = preserve.index("\n", profile_end) + 1
profile = preserve[profile_start:profile_end]
section_start = preserve.index('echo "== Selected stripping levels preserve static closure')
section_end = preserve.index('# shellcheck disable=SC2086 -- resolve_python may answer `py -3`.\n$PYTHON gates/fixtures/ildiet-metadata-validation/check.py', section_start)
section = preserve[section_start:section_end]
stubs = r'''
set -euo pipefail
CONFIG=Debug
TFM=net10.0
PYTHON=python_stub
ILD_DLL=ildiet
CLI_BIN=cli
CLI_DLL=cli/dn2cpp.dll
EXE_EXT=
python_stub() { printf 'DRIVER:%s\n' "$*"; }
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
with tempfile.TemporaryDirectory(prefix="dn2cpp-stripping-profile-test-") as temp:
    for value, expected in [("0", expected_high), ("1", expected_high | expected_medium)]:
        result = shell(stubs + profile + section, temp, {"DN2CPP_PRESERVE_CONTROL_MEDIUM": value})
        check(result.returncode == 0, (value, result.stderr))
        actual = [Path(line.split(":", 1)[1]).name for line in result.stdout.splitlines() if line.startswith("NATIVE:")]
        check(set(actual) == expected and len(actual) == len(expected), (value, actual, expected))
        commands = [line for line in result.stdout.splitlines() if line.startswith("DRIVER:")]
        check(len(commands) == 1 and ("--with-medium" in commands[0]) == (value == "1"), commands)
    for bad in ("true", "2", " ", "high medium"):
        result = shell(stubs + profile + section, temp, {"DN2CPP_PRESERVE_CONTROL_MEDIUM": bad})
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
check("printf 'stripping: High only; optional Medium checks not selected or counted as passed\\n'" in premerge,
      "receipt omits actual High-only scope")
check('note "stripping: High only; optional Medium checks are not selected or counted as passed."' in premerge,
      "plan omits actual High-only scope")

# Normal High still runs the old address-only regressions, and every helper
# receives the explicit selection rather than falling back to its full profile.
for name in ("intercept_retention", "buffer_moves", "globalization_retention", "gc_memory", "gc_generations"):
    ast.parse((driver.parent / (name + ".py")).read_text(encoding="utf-8"))
for function in ("validate_intercept_retention", "validate_buffer_moves", "validate_globalization", "validate_kernel32",
                 "validate_gc_memory", "validate_gc_generations"):
    check(function + "(probe, cli, original, directory, levels=levels)" in driver_source, function)
check('assert files == defaults, "explicit High changed generated C++"' in driver_source, "High byte comparison removed")
check('assert before.read_bytes() == (publications["high"] / before.name).read_bytes()' in driver_source,
      "High DLL comparison removed")
print(f"stripping-profile-controls: {checks} passed; High 22 native selections, optional full 62")
