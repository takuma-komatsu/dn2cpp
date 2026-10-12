"""Follow packed reflection edges without rooting an attribute read in managed IL."""

import pathlib
import re
import sys


def unique(pattern, text, subject):
    matches = re.findall(pattern, text, re.M | re.S)
    if len(matches) != 1:
        raise SystemExit("FAIL: unread attribute chain has no unique " + subject)
    return matches[0]


out = pathlib.Path(sys.argv[1])
source = "\n".join(path.read_text(encoding="utf-8") for path in sorted(out.glob("generated*.cpp")))
schema_source = pathlib.Path("runtime/core/dn2cpp_metadata.cpp").read_text(encoding="utf-8")
blocks = unique(r"const Dn2CppMetadataBlock dn2cpp_metadata_blocks\[\] = \{(.*?)\n\};",
                source, "metadata block registry")
pools = re.findall(r"\{\s*(md_ptr_[0-9]+),", blocks)


def decode(record, kind):
    name, offset = unique(r"(md_record_\w+) \+ ([0-9]+) \+ 1", record, "packed handle")
    initializer = unique(r"\buint8_t " + re.escape(name) + r"\[\] = \{([^}]*)\};",
                         source, "record " + name)
    data = bytes(int(value.strip()) for value in initializer.split(",") if value.strip())
    start = cursor = int(offset)

    def unsigned():
        nonlocal cursor
        value = shift = 0
        while True:
            if cursor >= len(data) or shift >= 64:
                raise SystemExit("FAIL: invalid packed attribute-chain record")
            byte = data[cursor]
            cursor += 1
            value |= (byte & 127) << shift
            if byte < 128:
                return value
            shift += 7

    block, length, presence = unsigned(), unsigned(), unsigned()
    if block >= len(pools) or length % 2 or start + length > len(data):
        raise SystemExit("FAIL: invalid attribute-chain metadata block or length")
    pool = unique(r"\bconst void\* const " + pools[block] + r"\[\] = \{([^\n]*)\};",
                  source, "pointer pool")
    pointers = pool.split(", ")
    fields = unique(r"constexpr Field Dn2Cpp" + kind + r"_fields\[\] = \{(.*?)\n\};",
                    schema_source, "schema")
    fields = re.findall(r"offsetof\([^,]+, (\w+)\), Encoding::(\w+)", fields)
    if presence >> len(fields):
        raise SystemExit("FAIL: invalid attribute-chain presence mask")
    result = {}
    for index, (field, encoding) in enumerate(fields):
        value = unsigned() if presence & (1 << index) else 0
        if encoding == "Pointer":
            if value > len(pointers):
                raise SystemExit("FAIL: invalid attribute-chain pointer index")
            result[field] = pointers[value - 1] if value else "nullptr"
        elif encoding.startswith("Signed"):
            result[field] = (value >> 1) ^ -(value & 1)
        else:
            result[field] = value
    if cursor > start + length:
        raise SystemExit("FAIL: attribute-chain record exceeds its length")
    return result


closed = unique(r"^const Dn2CppTypeInfo ti_ReflectAttrUnread_ChainSecond_Int32 = ([^\n]+);$",
                source, "closed owner TI")
reflection = decode(closed, "TypeReflection")
if reflection["customAttrCount"] != 1:
    raise SystemExit("FAIL: closed chain owner lost its attribute row")
attribute = decode(reflection["customAttrs"], "AttrInfo")
factory = unique(r"\b(attrcreate_[0-9]+)\b", attribute["create"], "attribute factory")
body = unique(r"^Dn2CppObject\* " + factory + r"\(\)\n\{(.*?)^\}", source, "factory body")
array_handle = unique(r"dn2cpp_get_type_from_handle\(&(ti_arr_\w+)\)",
                      body, "deepest array handle in attribute factory")
array = unique(r"^const Dn2CppTypeInfo " + re.escape(array_handle) + r" = ([^\n]+);$",
               source, "deepest array TI")
if not re.search(r"&ti_ReflectAttrUnread_ChainLast_Int32\b", array):
    raise SystemExit("FAIL: deepest array lost its element identity")
unique(r"^const Dn2CppTypeInfo ti_ReflectAttrUnread_ChainLast_Int32 = ([^\n]+);$",
       source, "deepest element TI")
print("OK (unread attribute chain: closed metadata -> interned row -> deepest array handle)")
