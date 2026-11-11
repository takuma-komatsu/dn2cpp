#!/usr/bin/env python3
"""Keep primitive BigMul callable when retention, rather than a call, roots it."""
from pathlib import Path
import re
import sys

root = Path(sys.argv[1])
files = sorted(root.glob("generated*.cpp")) + [root / "generated.h"]
text = "\n".join(path.read_text(encoding="utf-8") for path in files)
for owner in ("Int32", "UInt32", "Int64", "UInt64", "IntPtr", "UIntPtr"):
    # Inline bodies live in the header; a declaration alone is not executable.
    pattern = rf"\b{owner}_BigMul_\w+\([^;{{}}]*\)\s*\{{"
    count = len(re.findall(pattern, text))
    if count != 1:
        raise SystemExit(f"{owner}.BigMul: expected one callable body, found {count}")
print("primitive BigMul retention-only bodies OK")
