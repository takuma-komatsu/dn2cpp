#!/usr/bin/env python3
"""Check native ISA image selection and run routing without an SDK or compiler."""
from collections import Counter
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

GATES = Path(__file__).resolve().parent
REPO = GATES.parent
BASH = os.environ.get("DN2CPP_TEST_BASH", "bash")
ENV = {key: value for key, value in os.environ.items()
       if not key.startswith(("DN2CPP_", "GIT_"))}
source = (GATES / "_platform_isa.sh").read_text(encoding="utf-8")
project = ET.parse(REPO / "samples/dotnet/PlatformIsaProbe/PlatformIsaProbe.csproj").getroot()
root = Path(tempfile.mkdtemp(prefix="dn2cpp-isa-profile-"))
checks = 0


def check(condition, detail):
    global checks
    if not condition:
        raise AssertionError((detail, "evidence", str(root)))
    checks += 1


def between(start, end):
    first = source.index(start)
    return source[first:source.index(end, first)]


def shell(script, full, directory):
    # Keep extracted UTF-8 shell source out of locale-encoded argv.
    (directory / "script.sh").write_bytes(script.encode("utf-8"))
    return subprocess.run([BASH, "script.sh"], cwd=directory,
                          env={**ENV, "DN2CPP_PLATFORM_ISA_FULL": str(full),
                               "ISA_SOURCE": str(GATES / "_platform_isa.sh"),
                               "PLAN": str(directory / "plan.log")},
                          capture_output=True, text=True, encoding="utf-8")


# Execute the actual profile branch rather than maintaining another default.
profile = between('    case "${DN2CPP_PLATFORM_ISA_FULL:-0}" in',
                  '    _platform_isa_witness_normalization_self_check')
full_shards = ["x86-base", "x86-avx", "x86-avx512", "x86-avx10", "arm"]
for full, expected in [(0, ["native"]), (1, full_shards)]:
    result = shell('set -euo pipefail\nprofile() {\n' + profile +
                   '\nprintf "SELECT:%s\\n" "$PLATFORM_ISA_NATIVE_SHARDS"\n}\nprofile\n', full, root)
    check(result.returncode == 0, result.stderr)
    selected = [line.removeprefix("SELECT:").split() for line in result.stdout.splitlines()
                if line.startswith("SELECT:")]
    check(selected == [expected], (full, selected))


def definitions(shard):
    for group in project.findall("PropertyGroup"):
        if group.get("Condition") == f"'$(PlatformIsaShard)' == '{shard}'":
            return set(group.findtext("DefineConstants").split(";")[1:])
    raise AssertionError(shard)


native_definitions = definitions("native")
check(native_definitions == set().union(*(definitions(shard) for shard in full_shards)),
      "native definitions are not the exact full native union")
check("PLATFORM_ISA_SHARD_WASM" not in native_definitions and
      "PLATFORM_ISA_SHARD_ALL" not in native_definitions, "Wasm roots entered the native image")


def selected_lines(text, symbols):
    active = True
    stack = []
    selected = []
    for line in text.splitlines():
        directive = line.strip()
        if directive.startswith("#if "):
            expression = directive[4:]
            if not re.fullmatch(r"[A-Z0-9_]+(?: \|\| [A-Z0-9_]+)*", expression):
                raise AssertionError(("unhandled preprocessor expression", expression))
            condition = any(name in symbols for name in expression.split(" || "))
            stack.append((active, condition))
            active = active and condition
        elif directive == "#else":
            outer, condition = stack[-1]
            active = outer and not condition
        elif directive == "#endif":
            active = stack.pop()[0]
        elif active:
            selected.append(line)
    check(not stack, "unclosed conditional")
    return selected


# Table entries and exercise registrations must be identical to the old union.
for filename in ("Program.cs", "X86Sections.cs", "ArmSections.cs", "WasmSections.cs", "Exercises.g.cs"):
    text = (REPO / "samples/dotnet/PlatformIsaProbe" / filename).read_text(encoding="utf-8")
    native = selected_lines(text, native_definitions)
    parts = [selected_lines(text, definitions(shard)) for shard in full_shards]
    check(set(native) == set().union(*map(set, parts)), (filename, "source union differs"))
    # Unconditional source appears in every shard. Count only selected registration
    # lines, whose guarded multiplicity must not grow when symbols are combined.
    pattern = r'^\s*(?:new\("(?:X86|Arm|Wasm)",|(?:exercises|nestedProbes)\.Add\()'
    registrations = [line for line in native if re.match(pattern, line)]
    combined = [line for part in parts for line in part if re.match(pattern, line)]
    check(Counter(registrations) == Counter(combined), (filename, "registration multiplicity differs"))
    check(not any('"Wasm"' in line or '"Wasm.' in line for line in registrations),
          (filename, "native image registered Wasm"))


# Run the actual shard filter and both execution adapters with no-op processes.
# Arguments, attempt prefixes and per-architecture output paths remain observable.
select_shards = between('    local shard_names=()', '    local p_shard=')
run_adapters = between('    _pi_run() {', '    # _pi_require_count')
stubs = r'''
set -euo pipefail
source "$ISA_SOURCE"
gate_output() {
    if [ "$1" = view ]; then cp "$2" "$3"; fi
    return 0
}
platform_isa_output() { :; }
gate_run_diag_once() { echo 'unexpected diagnostic failure' >&2; return 1; }
_platform_isa_run_native_shard() {
    printf 'NATIVE|%s|%s|%s\n' "$1" "$2" "${*:7}" >>"$PLAN"
    local label="$1" shard="$2" accepted="$5" selection="$8" row
    if [ "$label" = N ] || [ "$label" = NM ]; then
        local supported=True result=ArgumentOutOfRangeException
        if [ "$label" = NM ]; then supported=False; result=PlatformNotSupportedException; fi
        case "$shard" in
            native) row="$selection.AdvSimd"; [ "$selection" != X86 ] || row=X86.Avx ;;
            x86-base) row=X86.X86Base ;;
            x86-avx) row=X86.Avx ;;
            x86-avx512) row=X86.Avx512F ;;
            x86-avx10) row=X86.Avx10v2 ;;
            arm) row=Arm.AdvSimd ;;
            *) return 1 ;;
        esac
        printf 'families=%s\n== contract ==\n%s=%s\n' "$selection" "$row" "$supported" >>"$accepted"
        # The empty base shard leaves the parser inside its contract at the
        # next shard's preamble. This is a real full-profile framing boundary.
        if [ "$shard" != x86-base ]; then
            printf '== invalid %s ==\nprobe=%s\n' "$row" "$result" >>"$accepted"
        fi
    fi
    _PI_SHARD_CODE=0
}
gate_capture() {
    local prefix="$1"
    printf 'ORACLE|%s|%s\n' "$prefix" "${*:2}" >>"$PLAN"
    : >"$prefix.stdout"
    : >"$prefix.stderr"
    printf '0\n' >"$prefix.status"
}
'''
setup = r'''
    PLATFORM_ISA_BUILT_SHARDS=()
    PLATFORM_ISA_BUILT_ARCHES=()
    PLATFORM_ISA_BUILT_OUTS=()
    PLATFORM_ISA_BUILT_APPS=()
    for shard in $PLATFORM_ISA_NATIVE_SHARDS; do
        PLATFORM_ISA_BUILT_SHARDS+=("$shard")
        PLATFORM_ISA_BUILT_ARCHES+=("$(platform_isa_shard_arches "$shard")")
        PLATFORM_ISA_BUILT_OUTS+=("out-$shard")
        PLATFORM_ISA_BUILT_APPS+=("$shard.dll")
    done
'''
runs = r'''
    local p_shard="${shard_names[0]}" log_dir="$PWD/logs-$arch"
    mkdir -p "$log_dir"
    _PI_RUN_LABELS=() _PI_RUN_CODES=() _PI_RUN_ORACLE_CODES=()
    _PI_RUN_OUTS=() _PI_RUN_ORACLE_OUTS=() _PI_RUN_LOGS=() _PI_RUN_ORACLE_LOGS=()
    _pi_run F unused "" ""
    _pi_run S unused "" ""
    _pi_run M unused "DN2CPP_CPU_FEATURES=none" "DOTNET_EnableHWIntrinsic=0"
    _pi_run P "$isa.PartialWitnesses" "" ""
    if [ "$arch" = x86 ]; then _pi_run O X86.Avx10v2 "DN2CPP_ENABLE_AVX10V2=1" ""; fi
    _pi_run_native N unused ""
    _pi_run_native NM unused "DN2CPP_CPU_FEATURES=none"
'''
for full in (0, 1):
    for arch, isa in (("x86", "X86"), ("arm", "Arm")):
        directory = root / f"profile-{full}-{arch}"
        directory.mkdir()
        script = stubs + 'exercise() {\nlocal arch="$1" isa="$2"\n' + profile + setup
        script += select_shards + run_adapters + runs + '\n}\nexercise ' + arch + ' ' + isa + '\n'
        result = shell(script, full, directory)
        (directory / "stdout.log").write_text(result.stdout, encoding="utf-8")
        (directory / "stderr.log").write_text(result.stderr, encoding="utf-8")
        check(result.returncode == 0, (full, arch, result.stderr))
        records = (directory / "plan.log").read_text(encoding="utf-8").splitlines()
        native = [row.split("|", 3)[1:] for row in records if row.startswith("NATIVE|")]
        selected = ["native"] if full == 0 else (full_shards[:4] if arch == "x86" else ["arm"])
        for label in ("F", "S", "M", "N", "NM", "P") + (("O",) if arch == "x86" else ()):
            calls = [row for row in native if row[0] == label]
            expected = selected
            if label == "P":
                expected = selected[:1]
            elif label == "O":
                expected = ["native" if full == 0 else "x86-avx10"]
            check([row[1] for row in calls] == expected, (full, arch, label, calls))
            for _, shard, command in calls:
                selection = isa if shard == "native" else "all"
                if label == "P":
                    selection = isa + ".PartialWitnesses"
                elif label == "O":
                    selection = "X86.Avx10v2"
                flag = " --contract-only" if label in ("F", "M") else " --invalid-immediates" if label in ("N", "NM") else ""
                check(command == f"./out-{shard}/PlatformIsaProbe {selection}{flag}", (full, arch, label, command))
                if label not in ("N", "NM"):
                    check(any(f"dotnet {shard}.dll {selection}{flag}" in row for row in records if row.startswith("ORACLE|")),
                          (full, arch, label, "oracle selection differs"))
        check(all("/logs-" + arch + "/" in row for row in records if row.startswith("ORACLE|")),
              (full, arch, "architecture logs overlap"))

        for label in ("N", "NM"):
            output = directory / f"logs-{arch}/{label}.out"
            parsed = subprocess.run([sys.executable, str(GATES / "platform-isa-output.py"), "violations", str(output)],
                                    capture_output=True, text=True, encoding="utf-8")
            check(parsed.returncode == 0 and parsed.stdout == "", (full, arch, label, parsed.stdout, parsed.stderr))
            if full == 1 and arch == "x86":
                text = output.read_text(encoding="utf-8")
                supported = "True" if label == "N" else "False"
                boundary = f"X86.X86Base={supported}\nfamilies=all\n== contract ==\n"
                check(boundary in text, (label, "missing empty-base-shard boundary"))
                malformed = directory / f"{label}-malformed-boundary.out"
                malformed.write_text(text.replace(boundary, boundary.replace("families=all", "families=X86"), 1),
                                     encoding="utf-8")
                rejected = subprocess.run([sys.executable, str(GATES / "platform-isa-output.py"), "violations", str(malformed)],
                                          capture_output=True, text=True, encoding="utf-8")
                check(rejected.returncode == 0 and "malformed contract row: b'families=X86'" in rejected.stdout,
                      (label, "parser accepted a malformed concatenated boundary", rejected.stdout, rejected.stderr))

check('p_prefix="${shard_outs[$i]}/partial-plan-$arch"' in source, "partial plans overwrite the other architecture")
check('|selection:$isa|' in source and '|shards:$PLATFORM_ISA_NATIVE_SHARDS|' in source,
      "cache context does not distinguish the execution plan")
print(f"platform-isa-profile: {checks} passed; no SDK, transpiler or native execution; evidence: {root}")
