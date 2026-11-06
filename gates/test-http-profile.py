#!/usr/bin/env python3
"""HTTP driver selection and cache completion without an SDK or network."""
import os
from pathlib import Path
import re
import subprocess
import tempfile
import xml.etree.ElementTree as ET

GATES = Path(__file__).resolve().parent
REPO = GATES.parent
BASH = os.environ.get("DN2CPP_TEST_BASH", "bash")
source = (GATES / "build-and-run-http-get.sh").read_text(encoding="utf-8")
checks = 0


def check(condition, detail):
    global checks
    assert condition, detail
    checks += 1


def between(start, end):
    first = source.index(start)
    return source[first:source.index(end, first)]


# Execute the real selection/build/cache branches, replacing server setup and
# assertions with the actual capture calls, whose callbacks never execute them.
build = between('echo "== 2/17 ', '\nif [ "$DN2CPP_HTTP_FULL" = 1 ]; then\necho "== 3/17 ')
live = between('echo "== 8/17 ', '\nif [ "$DN2CPP_HTTP_FULL" = 1 ]; then\necho "== 16/17 ')
regions = [
    ('echo "== 9/17 ', 'if [ "$DN2CPP_HTTP_FULL" = 1 ]; then gate_cache_commit; fi\nfi   # real arm'),
    ('echo "== 12/17 ', 'if [ "$DN2CPP_HTTP_FULL" = 1 ]; then gate_cache_commit; fi\nfi   # beyond arm'),
    ('# The trust anchors, read at the LINKER', 'gate_cache_commit\nfi   # tls arm'),
]
for start, end in regions:
    first = live.index(start)
    last = live.index(end, first)
    captures = re.findall(r'^gate_capture_ok [^\n]*(?:\\\n[^\n]*)? \|\| \{', live[first:last], re.M)
    expected = 3 if 'trust anchors' in start else 2
    check(len(captures) == expected, (start, captures))
    live = live[:first] + '\n'.join(capture[:-len(' || {')] for capture in captures) + '\n' + live[last:]

stubs = r'''
set -euo pipefail
CONFIG=Debug TFM=net10.0 corelib=corelib nethttp=nethttp
base=http://get ebase=http://beyond tbase=https://tls tlsca=ca.pem tlsdir=tls-evidence
build_proj() {
    printf 'PROJECT:%s\n' "$1"
    local dir name
    dir="$(dirname "$1")/bin/$CONFIG/$TFM"
    name="$(basename "$1" .csproj)"
    mkdir -p "$dir"
    : > "$dir/$name.dll"
}
invoke_cli() { printf 'EMIT:%s\n' "$*"; }
compile_console() {
    printf 'NATIVE:%s/%s\n' "$1" "$2"
    : > "$1/$2$EXE_EXT"
    chmod +x "$1/$2$EXE_EXT"
}
gate_cache_check() { printf 'CACHE:%s\n' "$*"; return "$CACHE_STATUS"; }
gate_cache_hit_msg() { printf 'HIT\n'; }
gate_cache_commit() { printf 'COMMIT\n'; }
gate_capture_ok() {
    printf 'CAPTURE:%s\n' "$*"
    [ "$1" != "$FAIL_CAPTURE" ] || return 37
}
'''

with tempfile.TemporaryDirectory(prefix="dn2cpp-http-profile-test-") as temp:
    # Keep extracted UTF-8 shell source out of locale-encoded argv.
    (Path(temp) / "script.sh").write_bytes((stubs + build + live).encode("utf-8"))
    for full in ("0", "1"):
        for cache in ("0", "1"):
            for extension in ("", ".exe"):
                directory = Path(temp) / (full + cache + (extension or "unix"))
                directory.mkdir()
                env = dict(os.environ, DN2CPP_HTTP_FULL=full, CACHE_STATUS=cache,
                           EXE_EXT=extension, FAIL_CAPTURE="")
                result = subprocess.run([BASH, "../script.sh"], cwd=directory,
                                        env=env, capture_output=True, text=True, encoding="utf-8")
                check(result.returncode == 0, (full, cache, extension, result.stderr))
                lines = result.stdout.splitlines()
                count = lambda prefix: sum(line.startswith(prefix) for line in lines)
                images = 1 if full == "0" else 3
                check(count("EMIT:") == images, result.stdout)
                check(count("CACHE:") == images, result.stdout)
                check(count("NATIVE:") == (images if cache == "1" else 0), result.stdout)
                check(count("COMMIT") == (images if cache == "1" else 0), result.stdout)
                check(count("CAPTURE:") == (7 if cache == "1" else 0), result.stdout)
                projects = [line for line in lines if line.startswith("PROJECT:")]
                check(len(projects) == (2 if full == "0" else 5), projects)
                if cache == "1":
                    calls = [line for line in lines if line.startswith("CAPTURE:")]
                    if full == "0":
                        for call, mode in zip(calls, ("get", "get", "beyond", "beyond", "tls", "tls", "tls")):
                            check(" " + mode + " " in call, call)
                        check(lines.index("COMMIT") > lines.index(calls[-1]), result.stdout)
                    else:
                        check(all("HttpTransport" not in call and "/httptransport" not in call for call in calls), calls)

    for failure in ("artifacts/httptransport-native/native", "artifacts/httptransport-native/beyond/native",
                    "tls-evidence/trusted", "tls-evidence/untrusted", "tls-evidence/untrusted-oracle"):
        directory = Path(temp) / ("fail-" + failure.replace("/", "-"))
        directory.mkdir()
        env = dict(os.environ, DN2CPP_HTTP_FULL="0", CACHE_STATUS="1", EXE_EXT="", FAIL_CAPTURE=failure)
        result = subprocess.run([BASH, "../script.sh"], cwd=directory, env=env,
                                capture_output=True, text=True, encoding="utf-8")
        check(result.returncode == 37, (failure, result.returncode, result.stderr))
        check("COMMIT" not in result.stdout, (failure, result.stdout))

project = REPO / "samples/dotnet/HttpTransport/HttpTransport.csproj"
aggregate = ET.parse(project).getroot()
check(aggregate.findtext("PropertyGroup/StartupObject") == "HttpTransport.Program", "aggregate startup")
check(not aggregate.findall(".//ProjectReference"), "aggregate must remain independent in sample prebuild")
links = {item.get("Include") for item in aggregate.findall("ItemGroup/Compile")}
for name in ("HttpReal", "HttpBeyondGet", "HttpsTls"):
    check("../" + name + "/Program.cs" in links, name)
    leaf = ET.parse(REPO / ("samples/dotnet/" + name + "/" + name + ".csproj")).getroot()
    for property in leaf.find("PropertyGroup"):
        check(aggregate.findtext("PropertyGroup/" + property.tag) == property.text, (name, property.tag))

print(f"http-profile-controls: {checks} checks passed; aggregate 1 emit/native build, isolated live drivers 3")
