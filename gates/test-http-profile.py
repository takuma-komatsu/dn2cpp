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


# Keep the driver selection, capture calls, curl-less link and cache boundaries.
build = between('echo "== 2/16 ', 'echo "== 3/16 ') + "fi\n"
live = source[source.index('echo "== 8/16 '):]
regions = [
    ('echo "== 9/16 ', 'echo "== 11/16 '),
    ('echo "== 12/16 ', 'echo "== 13/16 '),
    ('echo "== 13/16 ', 'if [ "$DN2CPP_HTTP_FULL" = 1 ]; then\necho "== 16/16 '),
]
for (start, end), expected in zip(regions, (2, 2, 3)):
    first = live.index(start)
    last = live.index(end, first)
    captures = re.findall(r'^gate_capture_ok [^\n]*(?:\\\n[^\n]*)? \|\| \{', live[first:last], re.M)
    check(len(captures) == expected, (start, captures))
    live = live[:first] + '\n'.join(capture[:-len(' || {')] for capture in captures) + '\n' + live[last:]

stubs = r'''
set -euo pipefail
CONFIG=Debug TFM=net11.0 corelib=corelib nethttp=nethttp
DN2CPP_CLI_DLL="cli payload/dn2cpp.dll"
mkdir -p "cli payload" runtime-nocurl
: > "cli payload/DnHttp.dll"
DN2CPP_OS=linux
base=http://get ebase=http://beyond tbase=https://tls tlsca=ca.pem tlsdir=tls-evidence
build_proj() {
    printf 'PROJECT:%s\n' "$1"
    local dir name
    dir="$(dirname "$1")/bin/$CONFIG/$TFM"
    name="$(basename "$1" .csproj)"
    mkdir -p "$dir"
    : > "$dir/$name.dll"
}
invoke_cli() {
    printf 'EMIT:%s\n' "$*"
    : > "${!#}/generated.cpp"
}
compile_console() {
    printf 'NATIVE:%s/%s\n' "$1" "$2"
    : > "$1/$2$EXE_EXT"
    chmod +x "$1/$2$EXE_EXT"
}
gate_cache_check() { printf 'CACHE:%s\n' "$*"; return "$CACHE_STATUS"; }
gate_cache_hit_msg() { printf 'HIT\n'; }
gate_cache_commit() { printf 'COMMIT\n'; }
ensure_cmake_runtime() { printf '%s\n' "$PWD/runtime-nocurl/dn2cpp-targets.cmake"; }
dump_object_symbols_defined() { :; }
cmake_build_app() {
    [ "$DN2CPP_NO_CURL" = 1 ] && [ -f "$1/generated.cpp" ] || return 1
    printf '%s\n' "$1" >> nocurl-calls
    [ "$LINK_RESULT" != success ] || return 0
    printf 'undefined symbol: %s\n' "$LINK_RESULT"
    return 1
}
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
                           EXE_EXT=extension, FAIL_CAPTURE="", LINK_RESULT="dn2cpp_http2_call_open")
                result = subprocess.run([BASH, "../script.sh"], cwd=directory,
                                        env=env, capture_output=True, text=True, encoding="utf-8")
                check(result.returncode == 0, (full, cache, extension, result.stderr))
                lines = result.stdout.splitlines()
                count = lambda prefix: sum(line.startswith(prefix) for line in lines)
                check(count("EMIT:") == 1 and count("CACHE:") == 1, result.stdout)
                emit = next(line for line in lines if line.startswith("EMIT:"))
                key = next(line for line in lines if line.startswith("CACHE:"))
                explicit_shim = "internal/DnHttp/src/DnHttp/bin/Debug/net11.0/DnHttp.dll"
                check((" -r " + explicit_shim in emit) == (full == "0"), emit)
                shim = explicit_shim if full == "0" else "cli payload/DnHttp.dll"
                mode = "explicit" if full == "0" else "default"
                check("|dnhttp:" + mode + "|" in key and " " + shim + " " in key, key)
                check(count("NATIVE:") == (1 if cache == "1" else 0), result.stdout)
                check(count("COMMIT") == (1 if cache == "1" else 0), result.stdout)
                check(count("CAPTURE:") == (7 if cache == "1" else 0), result.stdout)
                projects = [line for line in lines if line.startswith("PROJECT:")]
                check(len(projects) == (2 if full == "0" else 3), projects)
                nocurl = directory / "nocurl-calls"
                if full == "1" and cache == "1":
                    check(nocurl.read_text().splitlines() == ["artifacts/httptransport-native"], nocurl)
                else:
                    check(not nocurl.exists(), nocurl)
                if cache == "1":
                    calls = [line for line in lines if line.startswith("CAPTURE:")]
                    for call, mode in zip(calls, ("get", "get", "beyond", "beyond", "tls", "tls", "tls")):
                        check(" " + mode + " " in call, call)
                    check(lines.index("COMMIT") > lines.index(calls[-1]), result.stdout)

    for failure in ("artifacts/httptransport-native/native", "artifacts/httptransport-native/beyond/native",
                    "tls-evidence/trusted", "tls-evidence/untrusted", "tls-evidence/untrusted-oracle"):
        directory = Path(temp) / ("fail-" + failure.replace("/", "-"))
        directory.mkdir()
        env = dict(os.environ, DN2CPP_HTTP_FULL="1", CACHE_STATUS="1", EXE_EXT="", FAIL_CAPTURE=failure)
        result = subprocess.run([BASH, "../script.sh"], cwd=directory, env=env,
                                capture_output=True, text=True, encoding="utf-8")
        check(result.returncode == 37, (failure, result.returncode, result.stderr))
        check("COMMIT" not in result.stdout, (failure, result.stdout))

    for link_result in ("success", "unrelated_symbol"):
        directory = Path(temp) / link_result
        directory.mkdir()
        env = dict(os.environ, DN2CPP_HTTP_FULL="1", CACHE_STATUS="1", EXE_EXT="",
                   FAIL_CAPTURE="", LINK_RESULT=link_result)
        result = subprocess.run([BASH, "../script.sh"], cwd=directory, env=env,
                                capture_output=True, text=True, encoding="utf-8")
        check(result.returncode != 0 and "COMMIT" not in result.stdout, (link_result, result.stdout))

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

print(f"http-profile-controls: {checks} checks passed")
