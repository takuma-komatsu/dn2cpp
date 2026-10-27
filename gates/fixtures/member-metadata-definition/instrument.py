#!/usr/bin/env python3
"""Run the native-only incomplete-metadata contract in an artifact copy."""
from pathlib import Path
import sys
source = Path(sys.argv[1]) / "generated.cpp"
text = source.read_text(encoding="utf-8")
fixture = Path(__file__).with_name("native-contract.inc").read_text(encoding="utf-8")
main = text.index("int main(")
start = text.rfind("#if", 0, main)
assert start >= 0
text = text[:start] + fixture + "\n" + text[start:]
entry = text.index("    try {", text.index("int main("))
witness = "    metadata_definition_native_contract();\n    dn2cpp_main_exit(0);\n    return 0;\n"
source.write_text(text[:entry] + witness + text[entry:], encoding="utf-8")
