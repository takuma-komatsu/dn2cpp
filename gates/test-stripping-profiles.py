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
prefix = driver_source[:driver_source.index("directory.mkdir(parents=True, exist_ok=True)")]
for suffix, valid, expected in [([], True, ["raw", "full"]), (["--representative"], True, ["full"]),
                                (["--with-medium"], False, None),
                                (["--representative", "--representative"], False, None)]:
    argv = [str(driver), "ildiet", "probe", "transpiler", "output", "cli", *suffix]
    parser = "import sys\nsys.argv = " + repr(argv) + "\n__file__ = " + repr(str(driver)) + "\n"
    result = subprocess.run([sys.executable, "-c", parser + prefix + "\nprint(__import__('json').dumps(modes))\n"],
                            cwd=REPO, env=ENV, capture_output=True, text=True, encoding="utf-8")
    check((result.returncode == 0) == valid, (suffix, result.returncode, result.stderr))
    if valid:
        check(json.loads(result.stdout) == expected, result.stdout)

preserve = (GATES / "build-and-run-preserve-control.sh").read_text(encoding="utf-8")
profile_start = preserve.index('case "${DN2CPP_PRESERVE_CONTROL_FULL:-0}" in')
profile_end = preserve.index('echo "preserve-control-profile=', profile_start)
profile = preserve[profile_start:profile_end]
section_start = preserve.index('echo "== Basic High/Medium retention and inert initializers =="')
section_end = preserve.index("\nif ! preserve_full; then", section_start)
section = preserve[section_start:section_end]
check(section_start < preserve.index("    exit 0", section_start), "basic Medium must run before the short-profile return")
stubs = r'''
set -euo pipefail
CONFIG=Debug TFM=net10.0 ILD_DLL=ildiet CLI_BIN=cli CLI_DLL=cli/dn2cpp.dll EXE_EXT=
build_gate_proj() { :; }
gate_python() { printf 'DRIVER:%s\n' "$*"; }
compile_console() { printf 'NATIVE:%s\n' "$1"; mkdir -p "$1"; }
gate_capture_ok() { :; }
gate_output() { :; }
run_bounded() { :; }
'''
with tempfile.TemporaryDirectory(prefix="dn2cpp-stripping-profile-test-") as temp:
    for value, modes in [("0", ("full",)), ("1", ("raw", "full"))]:
        expected = {f"emission-{fixture}-{axis}" for fixture in ("medium", "retained-equality", "forwarded-attribute") for axis in modes}
        expected |= {f"emission-intercept-{kind}-{level}-{axis}" for kind in ("retained", "called") for level in ("high", "medium") for axis in modes}
        result = shell(stubs + profile + section, temp, {"DN2CPP_PRESERVE_CONTROL_FULL": value})
        check(result.returncode == 0, (value, result.stderr))
        actual = [Path(line.split(":", 1)[1]).name for line in result.stdout.splitlines() if line.startswith("NATIVE:")]
        check(set(actual) == expected and len(actual) == len(expected), (value, actual, expected))
        commands = [line for line in result.stdout.splitlines() if line.startswith("DRIVER:")]
        check(len(commands) == 1 and ("--representative" in commands[0]) == (value == "0"), commands)

premerge = (GATES / "pre-merge.sh").read_text(encoding="utf-8")
force_start = premerge.index("# Full matrices are explicit development checks,")
force = premerge[force_start:premerge.index("\n\n", force_start)]
argv_match = re.search(r"^premerge_argv\(\) \{\n.*?^\}", premerge, re.M | re.S)
check(argv_match is not None, "actual premerge_argv missing")

profiles = {'array-core': 'DN2CPP_ARRAY_FULL', 'manifest-resources': 'DN2CPP_MANIFEST_RESOURCES_FULL', 'preserve-control': 'DN2CPP_PRESERVE_CONTROL_FULL', 'reflect-invoke': 'DN2CPP_REFLECT_INVOKE_FULL', 'reflect-types': 'DN2CPP_REFLECT_TYPES_FULL', 'transpiler-limits': 'DN2CPP_TRANSPILER_LIMITS_FULL', 'emit-order-stability': 'DN2CPP_EMIT_ORDER_FULL', 'marshal-pinning': 'DN2CPP_MARSHAL_FULL', 'http-get': 'DN2CPP_HTTP_FULL', 'hotupdate-subset': 'DN2CPP_HOTUPDATE_FULL', 'custom-async-task': 'DN2CPP_CUSTOM_ASYNC_TASK_FULL', 'exception-message-subset': 'DN2CPP_EXCEPTION_MESSAGE_FULL', 'multiassembly': 'DN2CPP_MULTIASSEMBLY_FULL', 'trim-reflection': 'DN2CPP_TRIM_REFLECTION_FULL', 'obfuscation': 'DN2CPP_OBFUSCATION_FULL', 'wasm-console': 'DN2CPP_WASM_CONSOLE_FULL', 'platform-isa-native': 'DN2CPP_PLATFORM_ISA_FULL', 'default-culture': 'DN2CPP_DEFAULT_CULTURE_FULL'}
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


for inherited in ("1", "bad"):
    source = ('set -euo pipefail\n' + force + '\nREPO=repo\nSKIP_GODOT=1\nSKIP_XCODE=1\n'
              'DN2CPP_INCLUDE_CULTURE=0\nDN2CPP_PREMERGE_NO_CAFFEINATE=1\n' + argv_match.group() +
              '\npremerge_argv Debug logs\nprintf "%s\\n" "${PREMERGE_ARGV[@]}"\n')
    result = shell(source, REPO, {flag: inherited for flag in profiles.values()})
    check(result.returncode == 0, result.stderr)
    values = result.stdout.splitlines()
    for flag in profiles.values():
        check(values.count(flag + "=0") == 1, (flag, values))
check("default High plus basic High/Medium retention and initializer regressions" in premerge,
      "receipt must describe actual stripping scope")
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

ast.parse((driver.parent / "intercept_retention.py").read_text(encoding="utf-8"))
check('validate_intercept_retention(probe, cli, original, directory, modes=modes)' in driver_source,
      "retained callable tests must receive the selected axes")
check('assert files == defaults, "explicit High changed generated C++"' in driver_source, "High byte comparison removed")
check('assert before.read_bytes() == (publications["high"] / before.name).read_bytes()' in driver_source,
      "High DLL comparison removed")
print(f"stripping-profile-controls: {checks} passed")
