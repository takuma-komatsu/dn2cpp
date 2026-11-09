#!/usr/bin/env python3
"""Check the effective floating-point options on every generated translation unit."""
import argparse
import json
from pathlib import Path
import re

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("build_dir", type=Path)
parser.add_argument("mode", choices=("strict", "precise"))
parser.add_argument("--require-fast", action="store_true")
args = parser.parse_args()

commands = json.loads((args.build_dir / "compile_commands.json").read_text(encoding="utf-8"))
assert commands, "No native compile commands"
fast_seen = False
for entry in commands:
    command = entry["command"]
    fast = Path(entry["file"]).name == "generated_hot_fast.cpp"
    fast_seen |= fast
    modes = re.findall(r"(?<!\S)/fp:(\w+)(?!\S)", command)
    if re.search(r"(?<!\S)/bigobj(?!\S)", command):
        expected = [args.mode, "fast"] if fast else [args.mode]
        assert modes == expected, (entry["file"], expected, modes)
    else:
        assert not modes, (entry["file"], modes)
if args.require_fast:
    assert fast_seen, "FastMath translation unit was not compiled"
print(f"OK: native floating-point mode {args.mode}; FastMath precedence preserved")
