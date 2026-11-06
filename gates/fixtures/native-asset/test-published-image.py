#!/usr/bin/env python3
"""Exercise SDK-image reuse with text fixtures; no SDK or native execution."""
import os
from pathlib import Path
import re
import subprocess
import tempfile


GATES = Path(__file__).resolve().parents[2]
gate = (GATES / "build-and-run-native-asset-publish.sh").read_text(encoding="utf-8")
common = (GATES / "_common.sh").read_text(encoding="utf-8")


def function(source, name):
    match = re.search(r"^" + re.escape(name) + r"\(\) \{\n.*?^\}\n", source, re.M | re.S)
    assert match is not None, name
    return match.group()


assert not re.search(r"^\s*(?:if .* )?compile_console ", gate, re.M)
assert gate.count('publish_sdk_native_image "$GEN" "$PROJECT"') == 4
assert len(re.findall(r"\bdotnet publish \\\n", gate)) == 4

definitions = "\n".join([
    function(common, "stage_binary"),
    function(common, "stage_native_assets"),
    function(common, "publish_native_assets"),
    function(gate, "publish_sdk_native_image"),
])
probe = r'''
set -euo pipefail
DN2CPP_OS=linux
EXE_EXT=
out='generated output'
builddir="$PWD/$out/build"
first=11111111111111111111
second=22222222222222222222
third=33333333333333333333
cmake_build_app() { echo 'FAIL: unexpected native compile' >&2; return 1; }
compile_console() { echo 'FAIL: unexpected native compile' >&2; return 1; }
mkdir -p "$builddir"
printf 'dn2cpp_main_exit\n' > "$out/generated.cpp"

prepare_image() {
    local generation="$1" asset
    mkdir -p "$builddir/.dn2cpp-native/$generation"
    : > "$builddir/native-assets-publish.txt"
    for asset in shared.fixture dependency.fixture; do
        printf '%s:%s\n' "$generation" "$asset" > "$builddir/.dn2cpp-native/$generation/$asset"
        printf '%s\n' "$builddir/.dn2cpp-native/$generation/$asset" >> "$builddir/native-assets-publish.txt"
    done
    printf '%s\n' "$generation" > "$builddir/fixture-image"
}

assert_published_image() {
    local generation="$1" asset
    cmp "$builddir/fixture-image" "$out/fixture-image"
    [ "$(cat "$out/.native-generation-current.txt")" = "$generation" ]
    [ "$(cat "$out/.dn2cpp-native/$generation/.complete")" = "$generation" ]
    for asset in shared.fixture dependency.fixture; do
        cmp "$builddir/.dn2cpp-native/$generation/$asset" "$out/.dn2cpp-native/$generation/$asset"
    done
    [ ! -e "$out/.cmake" ]
}

prepare_image "$first"
publish_sdk_native_image "$out" fixture-image
assert_published_image "$first"
[ ! -e "$out/.native-generation-previous.txt" ]

prepare_image "$second"
if DN2CPP_TEST_NATIVE_ASSET_FAIL_AFTER=1 publish_sdk_native_image "$out" fixture-image \
        > copy-failure.log 2>&1; then
    echo 'FAIL: copy failure unexpectedly succeeded' >&2
    exit 1
fi
grep -q 'injected native-generation copy failure after 1 asset' copy-failure.log
[ "$(cat "$out/fixture-image")" = "$first" ]
[ "$(cat "$out/.native-generation-current.txt")" = "$first" ]
[ -f "$out/.dn2cpp-native/$first/.complete" ]
[ ! -e "$out/.dn2cpp-native/$second" ]
partials=("$out/.dn2cpp-native"/.new-*)
[ "${#partials[@]}" -eq 1 ] && [ -d "${partials[0]}" ]
[ ! -e "${partials[0]}/.complete" ]
[ "$(find "${partials[0]}" -type f | wc -l | tr -d ' ')" -eq 1 ]
publish_sdk_native_image "$out" fixture-image
assert_published_image "$second"
[ ! -e "${partials[0]}" ]
[ "$(cat "$out/.native-generation-previous.txt")" = "$first" ]
[ -f "$out/.dn2cpp-native/$first/.complete" ]

prepare_image "$third"
publish_sdk_native_image "$out" fixture-image
assert_published_image "$third"
[ ! -e "$out/.dn2cpp-native/$first" ]
[ -f "$out/.dn2cpp-native/$second/.complete" ]
[ "$(cat "$out/.native-generation-previous.txt")" = "$second" ]

printf 'plain return\n' > "$out/generated.cpp"
if publish_sdk_native_image "$out" fixture-image > exit-failure.log 2>&1; then
    echo 'FAIL: invalid generated main unexpectedly succeeded' >&2
    exit 1
fi
grep -q 'generated main does not exit through dn2cpp_main_exit' exit-failure.log
assert_published_image "$third"
printf 'OK: SDK-image reuse preserves shell publication and failure guards\n'
'''
with tempfile.TemporaryDirectory(prefix="dn2cpp-published-image-") as temporary:
    # Windows command-line limits can truncate the combined Bash source.
    script = Path(temporary) / "probe.sh"
    script.write_text(definitions + probe, encoding="utf-8", newline="\n")
    result = subprocess.run(
        [os.environ.get("DN2CPP_TEST_BASH", "bash"), script.as_posix()],
        cwd=temporary, capture_output=True, text=True, encoding="utf-8",
    )
    if result.returncode:
        raise AssertionError((result.returncode, result.stdout, result.stderr))
    print(result.stdout, end="")
