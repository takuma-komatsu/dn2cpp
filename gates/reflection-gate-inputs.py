#!/usr/bin/env python3
"""Check reflection witnesses and identify deterministic CLR inputs."""

from collections import Counter
import hashlib
import json
import os
from pathlib import Path
import sys


def check_lines(source, count, expected, prefixes=False):
    data = sys.stdin.buffer.read() if source == "-" else Path(source).read_bytes()
    # MSYS grep reads Windows text output with CRLF translated to LF.
    if os.name == "nt":
        data = data.replace(b"\r\n", b"\n")
    lines = data.split(b"\n") if data else []
    if data.endswith(b"\n"):
        lines.pop()
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
                   or key in ("LANG", "DN2CPP_REFLECTION_MEASURE", "DN2CPP_STRIPPED_OVERRIDES")}
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
        expected = sys.stdin.buffer.read().decode("utf-8").splitlines()
        return check_lines(source, count, expected)
    if mode == "oracle-key":
        app, *args = arguments
        return oracle_key(app, args)
    raise ValueError("unknown reflection gate operation: " + mode)


if __name__ == "__main__":
    sys.exit(main())
