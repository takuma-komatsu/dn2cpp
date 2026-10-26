#!/usr/bin/env python3
"""Check reflection witnesses and identify deterministic CLR inputs."""

from collections import Counter
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import sys


_spec = importlib.util.spec_from_file_location("gate_output", Path(__file__).with_name("gate-output.py"))
_output = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_output)


def check_lines(source, count, expected, prefixes=False):
    data = sys.stdin.buffer.read() if source == "-" else Path(source).read_bytes()
    try:
        lines = _output.records(data, policy="crlf")
    except ValueError as error:
        print(f"FAIL: {source}: {error}", file=sys.stderr)
        return 1
    counts = Counter(lines)
    failed = False
    for line in expected:
        encoded = line.encode("utf-8")
        actual = (sum(occurrences for text, occurrences in counts.items() if text.startswith(encoded))
                  if prefixes else counts[encoded])
        if (actual == 0 if count == "any" else actual != int(count)):
            print(f"FAIL: {source}: expected {count} occurrence(s) of {line!r}, found {actual}",
                  file=sys.stderr)
            failed = True
    return int(failed)


def oracle_key(app, arguments):
    app = Path(app)
    digest = hashlib.sha256()
    # Native emission flags do not change the CLR input; compile-time variants do.
    environment = {key: value for key, value in os.environ.items()
                   if key.upper().startswith(("DOTNET_", "COMPLUS_", "DN2CPP_BEFORE_", "LC_"))
                   or key in ("LANG", "DN2CPP_REFLECTION_MEASURE", "DN2CPP_STRIPPED_OVERRIDES", "DN2CPP_INHERITED_GVM_REMEDY")}
    digest.update(json.dumps([app.name, arguments, environment], sort_keys=True).encode("utf-8"))
    for path in sorted(app.parent.iterdir(), key=lambda p: p.name):
        if path.is_file() and (path.suffix.lower() == ".dll"
                               or path.name.endswith((".runtimeconfig.json", ".deps.json"))):
            digest.update(path.name.encode("utf-8") + b"\0")
            digest.update(hashlib.sha256(path.read_bytes()).digest())
    print(digest.hexdigest())
    return 0


def main():
    mode, *arguments = sys.argv[1:]
    if mode in ("lines", "prefixes"):
        source, count, *expected = arguments
        return check_lines(source, count, expected, prefixes=mode == "prefixes")
    if mode == "witnesses":
        source, count = arguments
        expected = [record.decode("utf-8") for record in
                    _output.records(sys.stdin.buffer.read(), policy="crlf")]
        return check_lines(source, count, expected)
    if mode == "oracle-key":
        app, *args = arguments
        return oracle_key(app, args)
    raise ValueError("unknown reflection gate operation: " + mode)


if __name__ == "__main__":
    sys.exit(main())
