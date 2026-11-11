"""Check alignment guards and constrained element-support emission, not runtime behavior."""
from pathlib import Path
import re
import sys


root = Path(sys.argv[1])
header = (root / "generated.h").read_text(encoding="utf-8")
source = header + "\n" + "\n".join(path.read_text(encoding="utf-8") for path in root.glob("generated*.cpp"))
functions = list(re.finditer(
    r"^// (?P<owner>[^\n]+)::(?P<method>[^\n]+)\n"
    r"[^\n;{}]+\b(?P<name>\w+)\([^\n]*\)\n\{(?P<body>.*?)^\}",
    source, re.M | re.S))
definitions = {item["name"]: item for item in functions}
loads = [item for item in functions
         if item["owner"].startswith("System.Runtime.Intrinsics.ISimdVector")
         and item["method"] == "LoadAligned"]
assert len(loads) == 5, ("missing default LoadAligned bodies", len(loads))
main = [item for item in functions if item["owner"] == "Fixture.Program" and item["method"] == "Main"]
assert len(main) == 1, "missing fixture entry point"
for item in loads:
    name, body = item["name"], item["body"]
    assert re.search(r"\b" + re.escape(name) + r"\s*\(", main[0]["body"]), ("unreached default body", name)
    calls = re.findall(r"\b(\w*(?:get_Alignment|ThrowAccessViolationException)\w*)\s*\(", body)
    assert any("get_Alignment" in call for call in calls), ("missing concrete alignment call", name)
    assert any("ThrowAccessViolationException" in call for call in calls), ("missing misalignment guard", name)
    assert "dn2cpp_rem_unsigned" in body and "if (" in body, ("missing alignment branch", name)
    assert "dn2cpp_vec_load<" in body, ("missing guarded memory load", name)
    for target in [name, *calls]:
        assert target in definitions, ("missing callable managed definition", target)
        assert re.search(r"\b" + re.escape(target) + r"\s*\(", header), ("missing callable declaration", target)
        if "ThrowAccessViolationException" in target:
            assert "dn2cpp_throw" in definitions[target]["body"], "missing managed exception throw"
print("vector alignment default bodies and callable guards emitted")

# Follow the returned temporary through ordinary managed bridges as well as a
# direct lowering. Every leaf must be a backend-independent support constant.
def support_constant(item, seen=()):
    assert item["name"] not in seen, ("recursive support bridge", item["name"])
    body = item["body"]
    assert "DN2CPP_SIMD_HW_ACCEL" not in body, ("element support depends on backend", item["name"])
    returned = re.findall(r"\breturn\s+(t\d+);", body)
    assert len(returned) == 1, ("unexpected support return", item["name"], body)
    assert not re.search(r"\b(?:if|goto)\b", body), ("unexpected support branch", item["name"])

    def assignment(temporary):
        values = re.findall(r"\b" + re.escape(temporary) + r"\s*=\s*([^;]+);", body)
        assert len(values) == 1, ("unexpected support assignment", item["name"], temporary, body)
        return values[0]

    value = assignment(returned[0])
    if value in ("0", "1"):
        return value
    comparison = re.fullmatch(r"dn2cpp_type_equals\((t\d+), (t\d+)\)", value)
    if comparison:
        # A retained enum getter may keep its final typeof(T) equality. These
        # interned type handles have exact identity, even when storage matches.
        def type_handle(temporary):
            handle = re.fullmatch(r"dn2cpp_get_type_from_handle\((t\d+)\)", assignment(temporary))
            assert handle, ("unexpected support type lookup", item["name"], temporary)
            symbol = assignment(handle[1])
            assert re.fullmatch(r"&(?:ti_Fixture_UnsupportedVectorEnum|dn2cpp_(?:u?int(?:8|16|32|64|ptr)|float|double)_type)", symbol), ("unexpected support type", symbol)
            return symbol
        return "1" if type_handle(comparison[1]) == type_handle(comparison[2]) else "0"
    bridge = re.fullmatch(r"(\w+)\(\)", value)
    assert bridge and bridge[1] in definitions, ("missing support bridge", item["name"], value)
    return support_constant(definitions[bridge[1]], (*seen, item["name"]))


for family in ("Vector64", "Vector128", "Vector256", "Vector512", "Vector"):
    for element, expected in (("Int32", "1"), ("Char", "0"), ("UnsupportedVectorEnum", "0")):
        method = "ContractSupport_" + family + "_" + element
        matches = [item for item in functions if item["owner"] == "Fixture.Program" and item["method"] == method]
        assert len(matches) == 1, ("missing constrained support body", method)
        item = matches[0]
        assert re.search(r"\b" + re.escape(item["name"]) + r"\s*\(", main[0]["body"]), ("unreached support body", method)
        assert support_constant(item) == expected, ("wrong element support", method)
print("constrained vector support is true for int and false for char/enum in every family")
