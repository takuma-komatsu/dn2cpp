#!/usr/bin/env python3
"""Reject mutated companion responses before the transpiler builds its model."""
from pathlib import Path
import os
import shutil
import subprocess
import sys

cli, companion, probe, corelib, original, directory = (Path(value).resolve() for value in sys.argv[1:])
directory.mkdir(parents=True, exist_ok=True)
assert (original / "GraphApp.dll").is_file(), "create graph fixture inputs before checking the consumer"


def run(*arguments, env):
    return subprocess.run(["dotnet", "exec", *map(str, arguments)], capture_output=True,
                          env={**os.environ, **env})


# Matching malformed manifests isolate each validation from pointer consistency.
# A separate response-only mutation checks the pointer comparison itself.
proxy = directory / "protocol-proxy"
proxy.mkdir(parents=True, exist_ok=True)
assert cli.is_file(), "CLI payload is missing: " + str(cli)
cli_bin = cli.parent
for path in cli_bin.iterdir():
    if path.is_file() and path.suffix in (".dll", ".json"):
        shutil.copyfile(path, proxy / path.name)
proxy_diet = proxy / "ildiet"
proxy_diet.mkdir(parents=True, exist_ok=True)
for path in probe.parent.iterdir():
    if path.is_file() and path.suffix in (".dll", ".json"):
        shutil.copyfile(path, proxy_diet / path.name)
for extension in (".dll", ".runtimeconfig.json", ".deps.json"):
    shutil.copyfile(probe.with_suffix(extension), proxy_diet / ("ILDiet" + extension))
for host in ("ILDiet", "ILDiet.exe"):
    (proxy_diet / host).unlink(missing_ok=True)
diagnostics = {
    "version": "ILDiet returned an unsupported result version",
    "generation": "ILDiet returned an invalid generation",
    "hash": "ILDiet returned an invalid assembly hash: GraphApp",
    "contract": "ILDiet returned a different retention contract",
    "mode": "ILDiet returned an invalid assembly mode",
    "skipped-rewritten": "ILDiet returned an inconsistent assembly rewrite state",
    "fallback": "ILDiet returned an invalid fallback state",
    "copied-registry": "ILDiet returned an inconsistent assembly rewrite state",
    "duplicate-assembly": "ILDiet changed the number of assemblies or omitted counters",
    "counter-order": "ILDiet returned invalid counter names or order",
    "file-bytes": "ILDiet returned an invalid assembly hash: GraphApp",
    "pointer-mismatch": "ILDiet result does not match its published generation pointer",
}

def check_mutation(mutation):
    diagnostic = diagnostics[mutation]
    output = directory / ("consumer-" + mutation)
    # File-byte tampering leaves an invalid immutable generation after rejection.
    # Each case owns its output and must start clean on repeated gate runs.
    if output.exists():
        shutil.rmtree(output)
    options = ["--ildiet-skip-assembly", "GraphApp"] if mutation == "skipped-rewritten" else []
    result = run(proxy / "dn2cpp.dll", original / "GraphApp.dll", "-r", original / "GraphLib.dll",
                 "-r", corelib, "-o", output, *options,
                 env={"DN2CPP_TEST_ILDIET_COMPANION": str(companion), "DN2CPP_TEST_ILDIET_MUTATION": mutation})
    (directory / ("consumer-" + mutation + ".stdout.log")).write_bytes(result.stdout)
    (directory / ("consumer-" + mutation + ".stderr.log")).write_bytes(result.stderr)
    evidence = f"mutation={mutation}; exit={result.returncode}; stdout={result.stdout.decode('utf-8', errors='replace')}; stderr={result.stderr.decode('utf-8', errors='replace')}"
    assert ("protocol-fixture-mutated=" + mutation).encode() in result.stdout, "proxy mutation did not execute: " + evidence
    assert result.returncode != 0 and diagnostic.encode() in result.stderr, "consumer did not reject the intended protocol violation: " + evidence
    assert not list(output.glob("generated*")), "malformed publication reached C++ emission: " + evidence


for mutation in diagnostics:
    check_mutation(mutation)
print("graph-validation=consumer-" + ",".join(diagnostics), flush=True)
check_mutation("file-bytes")
print("graph-validation=consumer-repeat-file-bytes", flush=True)
