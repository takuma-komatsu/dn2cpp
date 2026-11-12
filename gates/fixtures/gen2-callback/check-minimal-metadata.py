"""Reference-only Gen2 metadata must retain the permanent allocation refusal."""
from pathlib import Path
import re
import sys


def require(condition, message):
    if not condition:
        raise AssertionError(message)


output = Path(sys.argv[1])
text = "\n".join(path.read_text() for path in [output / "generated.h", *sorted(output.glob("generated*.cpp"))])
symbol = "ti_System_Gen2GcCallback"
rows = re.findall(r"^const Dn2CppTypeInfo " + symbol + r" = \{ ([^\n]+) \};$", text, re.MULTILINE)
require(len(rows) == 1, "expected one Gen2 type-info definition")
hot = [value.strip() for value in rows[0].split(",")]
require(len(hot) == 22, "unexpected Gen2 type-info schema")
require(hot[15] == "0", "Gen2 was emitted with a full instance layout instead of reference-only metadata")
require(hot[2] == hot[11] == hot[13] == "nullptr", "reference-only Gen2 must have no vtable, finalizer or full type object")
require(hot[21] == "Dn2CppMetadataHandle<Dn2CppTypeReflection>::from_raw(md_native_refl_" + symbol + " + 0)",
        "Gen2 type info does not reference its own native allocation metadata")
cold_rows = re.findall(r"static const Dn2CppTypeReflection md_native_refl_" + symbol
                      + r"\[\] = \{\s*\{ ([^\n]+) \},\s*\};", text)
require(len(cold_rows) == 1, "expected one Gen2 reflection row")
cold = [value.strip() for value in cold_rows[0].split(",")]
require(len(cold) == 27, "unexpected Gen2 reflection metadata schema")
require(cold[0:6] == ["0", "0LL", "0", "0LL", "0", "0LL"],
        "reference-only Gen2 must have no field, method or constructor tables")
require(cold[26] == "8LL", "reference-only Gen2 lost DN2CPP_TA_ALLOCATION_UNSUPPORTED")
require(not re.search(r"\bstruct t_System_Gen2GcCallback(?:\s*:\s*[^\n{]+)?\s*\{", text),
        "reference-only Gen2 unexpectedly has an allocated class layout")
print("gen2-reference-only-allocation-bit:True")
