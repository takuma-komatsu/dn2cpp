#!/usr/bin/env python3
"""CLI preparation and subprocess reuse without an installed .NET SDK."""
import os
from pathlib import Path
import subprocess
import tempfile

GATES = Path(__file__).resolve().parent
BASH = os.environ.get("DN2CPP_TEST_BASH", "bash")
source = (GATES / "_common.sh").read_text(encoding="utf-8")
start = source.index("prepare_cli() {")
helpers = source[start:source.index("# build_proj CSPROJ", start)]
checks = 0


def check(condition, message):
    global checks
    assert condition, message
    checks += 1


with tempfile.TemporaryDirectory(prefix="dn2cpp-cli-test-") as temp:
    root = Path(temp)
    fake = root / "dotnet"
    fake.write_text('''#!/usr/bin/env bash
set -eu
printf '%s\\n' "$*" >> "$FAKE_LOG"
case "$1" in
    build)
        [ "$FAKE_MODE" != fail ] || exit 37
        if [ "$FAKE_MODE" != missing ]; then
            mkdir -p "src/Dn2Cpp.Cli/bin/$CONFIG/$TFM"
            : > "src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll"
        fi ;;
    exec) exit "${FAKE_EXEC_STATUS:-0}" ;;
    *) exit 99 ;;
esac
''', encoding="utf-8")
    fake.chmod(0o755)
    scenarios = [
        ("subshells", "ok", '''prepare_cli
( invoke_cli first )
( invoke_cli second )
invoke_cli third | cat
''', 0, 1, 3),
        ("lazy", "ok", "invoke_cli first\ninvoke_cli second\n", 0, 1, 2),
        ("provided", "ok", '''DN2CPP_CLI_DLL="$PWD/provided path/custom.dll"
prepare_cli
[ "$(bash -c 'printf %s "$DN2CPP_CLI_DLL"')" = "$DN2CPP_CLI_DLL" ]
( invoke_cli first )
( invoke_cli second )
''', 0, 0, 2),
        ("failed-prepare", "fail", "prepare_cli\ninvoke_cli never\n", 1, 1, 0),
        ("failed-lazy", "fail", "invoke_cli never\n", 1, 1, 0),
        ("missing-prepare", "missing", "prepare_cli\ninvoke_cli never\n", 1, 1, 0),
        ("missing-lazy", "missing", "invoke_cli never\n", 1, 1, 0),
        ("exec-status", "ok", "FAKE_EXEC_STATUS=23 invoke_cli fails\n", 23, 1, 1),
    ]
    for name, mode, script, status, builds, executions in scenarios:
        directory = root / name
        directory.mkdir()
        log = directory / "calls.log"
        env = dict(os.environ, PATH=str(root) + os.pathsep + os.environ["PATH"],
                   FAKE_LOG=str(log), FAKE_MODE=mode)
        env.pop("DN2CPP_CLI_DLL", None)
        env.pop("FAKE_EXEC_STATUS", None)
        # Keep extracted UTF-8 shell source out of locale-encoded argv.
        command = "set -euo pipefail\nexport CONFIG=Debug TFM=net10.0\n" + helpers + script
        (directory / "script.sh").write_bytes(command.encode("utf-8"))
        result = subprocess.run([BASH, "script.sh"],
                                cwd=directory, env=env, capture_output=True, text=True, encoding="utf-8")
        calls = log.read_text(encoding="utf-8").splitlines() if log.exists() else []
        check(result.returncode == status, (name, result.returncode, result.stderr))
        check(sum(line.startswith("build ") for line in calls) == builds, (name, calls))
        check(sum(line.startswith("exec ") for line in calls) == executions, (name, calls))
        if mode == "missing":
            check("CLI build produced no" in result.stderr, (name, result.stderr))
        if name == "provided":
            check(all("provided path/custom.dll" in line for line in calls), calls)

for gate in ("memorypack", "mastermemory", "messagepack", "grpc-unary", "magiconion-client", "magiconion-yetanotherhttphandler",
             "r3", "zlinq", "zstring", "messagepipe", "yetanotherhttphandler", "unitask"):
    wrapper = (GATES / ("build-and-run-" + gate + ".sh")).read_text(encoding="utf-8")
    check(wrapper.count("\nprepare_cli\n") == 1, (gate, "prepare once in parent shell"))
    check(wrapper.index("\nprepare_cli\n") < wrapper.index("invoke_cli "), gate)
    check("build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj" not in wrapper, gate)

print(f"invoke-cli-preparation: {checks} checks passed")
