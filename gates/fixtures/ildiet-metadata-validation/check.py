#!/usr/bin/env python3
"""Check rewritten metadata, preserved payloads, and an entirely dead reference."""
import hashlib
from pathlib import Path
import subprocess
import sys

companion, probe, app, library, corelib, directory = map(Path, sys.argv[1:])
directory.mkdir(parents=True, exist_ok=True)
dead = directory / "ILDietDeadReference.dll"
signed = companion.parent / "Mono.Cecil.dll"


def run(*arguments):
    subprocess.run(["dotnet", "exec", *map(str, arguments)], check=True)


run(probe, "--create-dead", dead)
resolver_fixtures = directory / "resolver-fixtures"
run(probe, "--create-resolver-fixtures", resolver_fixtures)
inputs = [app, library, corelib, signed, dead]
before = {path: hashlib.sha256(path.read_bytes()).digest() for path in inputs}
for name in ("first", "second"):
    run(companion, app, "-r", library, "-r", corelib, "-r", signed,
        "-r", dead, "-o", directory / name)
first = directory / "first"
run(probe, app, first / app.name)
run(probe, library, first / library.name)
run(probe, signed, first / signed.name, "signed")
run(probe, dead, first / dead.name, "empty", "resources")
for path, digest in before.items():
    assert hashlib.sha256(path.read_bytes()).digest() == digest, f"input changed: {path}"
assert sorted(path.name for path in first.iterdir()) == sorted(path.name for path in (directory / "second").iterdir())
for path in first.iterdir():
    assert path.read_bytes() == (directory / "second" / path.name).read_bytes(), f"nondeterministic output: {path.name}"
assert (first / corelib.name).read_bytes() == corelib.read_bytes(), "protected CoreLib was rewritten"


def resolver_case(name, references, selected):
    output = directory / name
    resolver_app = resolver_fixtures / "ResolverApp.dll"
    command = [companion, resolver_app]
    for reference in references:
        command.extend(("-r", resolver_fixtures / reference))
    command.extend(("-o", output))
    run(*command)
    run(probe, "--check-resolver", output, selected)


resolver_case("resolver-first", ("ResolverFirst.dll", "ResolverSecond.dll"), "ResolverFirst")
resolver_case("resolver-second", ("ResolverSecond.dll", "ResolverFirst.dll"), "ResolverSecond")
print("metadata-validation=resources,identity,empty-reference,token-integrity,determinism,resolver-order,nested-resolver,missing-type")
