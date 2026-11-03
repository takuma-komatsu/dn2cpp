#!/usr/bin/env python3
"""Check a reflection-only emission's body -> method row -> type-info attachment.

Reads generated C++ only. Supports the frozen emitter's native and packed rows;
this is structural evidence, never a replacement for the native/CLR output diff.
"""
import argparse
import json
import re
from pathlib import Path

LABEL = "gchandle-check-uninitialized"
OWNER_SUFFIX = "System_Runtime_InteropServices_GCHandle"
FUNCTION = re.compile(r"\b(GCHandle_CheckUninitialized_m\d+)\b")
EXPECTED_ATTRIBUTES = 0x0093
EXPECTED_IMPL = 0x0100
EXPECTED_PARAMETERS = 1
REQUIRED_BODY = re.compile(r"if\s*\(a0\s*==\s*0\)\s*dn2cpp_throw_null_reference\(\);\s*return;")


def require(condition, message):
    if not condition:
        raise SystemExit(LABEL + ": " + message)


def split_fields(text):
    values, start, depth, quoted, escaped = [], 0, 0, False, False
    for index, char in enumerate(text):
        if quoted:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                quoted = False
            continue
        if char == '"':
            quoted = True
        elif char in "([{<":
            depth += 1
        elif char in ")]}>":
            depth -= 1
        elif char == "," and depth == 0:
            values.append(text[start:index].strip())
            start = index + 1
    tail = text[start:].strip()
    if tail:
        values.append(tail)
    return values


def varuint(data, index):
    result, shift = 0, 0
    while True:
        require(index < len(data) and shift < 70, "invalid packed metadata varint")
        value = data[index]
        index += 1
        result |= (value & 127) << shift
        if not value & 128:
            return result, index
        shift += 7


def packed_rows(data):
    start, rows = 0, []
    while start < len(data):
        block, index = varuint(data, start)
        size, index = varuint(data, index)
        mask, index = varuint(data, index)
        require(size > 0 and not size % 2 and start + size <= len(data), "invalid packed row extent")
        fields = {}
        for field in range(64):
            if mask & (1 << field):
                fields[field], index = varuint(data, index)
        require(index <= start + size and not any(data[index:start + size]), "invalid packed row padding")
        rows.append({"block": block, "offset": start, "fields": fields, "packed": True})
        start += size
    return rows


def native_rows(text):
    result = []
    for row in split_fields(text):
        require(row.startswith("{") and row.endswith("}"), "invalid native row initializer")
        result.append({"fields": dict(enumerate(split_fields(row[1:-1]))), "packed": False})
    return result


def number(row, field):
    value = row["fields"].get(field, 0 if row["packed"] else "0")
    if row["packed"]:
        return (value >> 1) ^ -(value & 1)
    match = re.fullmatch(r"(-?\d+)(?:ULL|LL|UL|L|U)?", value)
    require(match is not None, "non-numeric native metadata field")
    return int(match[1])


def named_invoker_bodies(text, path):
    for match in re.finditer(r"\bvoid (Program_Named_m\d+)\(intptr_t a0\)\s*\{", text):
        depth, end = 1, match.end()
        while end < len(text) and depth:
            if text[end] == "{":
                depth += 1
            elif text[end] == "}":
                depth -= 1
            end += 1
        require(depth == 0, "unterminated named invoker body")
        yield {"function": match[1], "file": str(path), "body": text[match.end():end - 1]}


def lazy_type_path(named, owner):
    require(len(named) == 1, "expected one isolated named invocation body")
    entry = named[0]
    body = entry["body"]
    loads = list(re.finditer(r"\b(t\d+)\s*=\s*&" + re.escape(owner) + r"\s*;", body))
    require(len(loads) == 1, "canonical lazy Type path does not use the generated owner type-info")
    load = loads[0]
    conversions = list(re.finditer(r"\b(t\d+)\s*=\s*dn2cpp_get_type_from_handle\(" + re.escape(load[1]) + r"\);", body))
    require(len(conversions) == 1 and conversions[0].start() > load.end(), "missing canonical lazy Type conversion")
    conversion = conversions[0]
    require(not re.search(r"\b" + re.escape(load[1]) + r"\s*=", body[load.end():conversion.start()]),
            "lazy Type handle overwritten before conversion")
    lookups = list(re.finditer(r"\b(t\d+)\s*=\s*\(\(Dn2CppObject\*\)dn2cpp_type_get_method_full\("
                              + re.escape(conversion[1]) + r",[^;]+;", body))
    require(len(lookups) == 1 and lookups[0].start() > conversion.end(), "named lookup does not use the canonical lazy Type")
    lookup = lookups[0]
    require(not re.search(r"\b" + re.escape(conversion[1]) + r"\s*=", body[conversion.end():lookup.start()]),
            "lazy Type object overwritten before lookup")
    aliases = list(re.finditer(r"\b(bs\d+_Ref)\s*=\s*" + re.escape(lookup[1]) + r";", body))
    require(len(aliases) == 1 and aliases[0].start() > lookup.end(), "named method result has no invocation alias")
    alias = aliases[0]
    require(not re.search(r"\b" + re.escape(lookup[1]) + r"\s*=", body[lookup.end():alias.start()]),
            "named lookup result overwritten before invocation alias")
    invocation = re.search(r"dn2cpp_methodref_invoke\(\(Dn2CppMethodRef\*\)\(" + re.escape(alias[1]) + r"\),", body[alias.end():])
    require(invocation is not None, "named result is not actually invoked")
    require(not re.search(r"\b" + re.escape(alias[1]) + r"\s*=", body[alias.end():alias.end() + invocation.start()]),
            "named method result overwritten before invocation")
    return {"kind": "lazy", "function": entry["function"], "file": entry["file"],
            "owner_variable": load[1], "type_variable": conversion[1], "lookup_variable": lookup[1],
            "invocation_alias": alias[1]}


def check_lookup_overwrite_negative():
    owner = "ti_System_Runtime_InteropServices_GCHandle"
    body = ("t0 = &" + owner + ";\n"
            "t1 = dn2cpp_get_type_from_handle(t0);\n"
            "t2 = ((Dn2CppObject*)dn2cpp_type_get_method_full(t1, nullptr, -1, nullptr, 40, 0, nullptr, DN2CPP_LOOKUP_TYPES));\n"
            "bs0_Ref = t2;\n"
            "dn2cpp_methodref_invoke((Dn2CppMethodRef*)(bs0_Ref), nullptr, nullptr);\n")
    entry = {"function": "Program_Named_m1", "file": "data-only-checker-control", "body": body}
    lazy_type_path([entry], owner)
    entry["body"] = body.replace("bs0_Ref = t2;", "t2 = unrelated_method;\nbs0_Ref = t2;")
    rejected = False
    try:
        lazy_type_path([entry], owner)
    except SystemExit as error:
        rejected = str(error) == LABEL + ": named lookup result overwritten before invocation alias"
    require(rejected, "lookup-overwrite negative was not rejected at the exact boundary")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("generated", type=Path)
    args = parser.parse_args()
    check_lookup_overwrite_negative()
    paths = sorted(path for path in args.generated.rglob("*") if path.suffix in (".cpp", ".h"))
    require(paths, "no generated C++ files")
    pointers, tables, typeinfos, bodies, named = {}, {}, {}, {}, []
    for path in paths:
        text = path.read_text()
        for match in re.finditer(r"const void\* const md_ptr_(\d+)\[\]\s*=\s*\{(.*?)\};", text, re.S):
            pointers[int(match[1])] = split_fields(match[2])
        for match in re.finditer(r"const uint8_t (md_record_(?:methtab_|refl_ti_)\w+)\[\]\s*=\s*\{([^}]+)\};", text, re.S):
            values = [int(value.strip()) for value in match[2].split(",") if value.strip()]
            require(all(0 <= value < 256 for value in values), "invalid packed byte initializer")
            tables[match[1]] = packed_rows(values)
        for match in re.finditer(r"const (?:Dn2CppMethodInfo|Dn2CppTypeReflection) (md_native_(?:methtab_|refl_ti_)\w+)\[\]\s*=\s*\{(.*?)\};", text, re.S):
            tables[match[1]] = native_rows(match[2])
        for match in re.finditer(r"const Dn2CppTypeInfo (ti_\w+)\s*=\s*\{(.*?)\};", text, re.S):
            typeinfos[match[1]] = split_fields(match[2])
        named.extend(named_invoker_bodies(text, path))
        # Both leaf replacements have a flat body; the complete brace body is checked.
        for match in re.finditer(r"\b(GCHandle_CheckUninitialized_m\d+)\s*\([^;{}]*\)\s*\{([^{}]*)\}", text, re.S):
            bodies[match[1]] = {"body": match[2], "file": str(path)}

    def pointer(row, field):
        value = row["fields"].get(field, 0 if row["packed"] else "nullptr")
        if not row["packed"]:
            return value
        if value == 0:
            return "nullptr"
        block = pointers.get(row["block"])
        require(block is not None and 0 < value <= len(block), "missing packed pointer index")
        return block[value - 1]

    hits = []
    for table, rows in tables.items():
        if "methtab_" not in table:
            continue
        for index, row in enumerate(rows):
            match = FUNCTION.search(pointer(row, 7))
            if match:
                hits.append((table, index, row, match[1]))
    require(len(hits) == 1, "expected exactly one callable target row, found " + str(len(hits)))
    table, index, row, function = hits[0]
    require(function in bodies and REQUIRED_BODY.search(bodies[function]["body"]), "missing genuine replacement body")
    require(number(row, 4) == EXPECTED_PARAMETERS and number(row, 12) == EXPECTED_ATTRIBUTES
            and number(row, 13) == EXPECTED_IMPL, "target row ABI/flags disagreement")
    invoker = pointer(row, 8)
    require("&" in invoker and "nullptr" not in invoker, "target row has no invoker")
    owner_match = re.search(r"&\s*(ti_\w+)\b", pointer(row, 1))
    require(owner_match is not None, "target row has no generated owner type-info")
    owner = owner_match[1]
    require(owner.endswith(OWNER_SUFFIX), "unexpected target owner")
    require(owner in typeinfos and len(typeinfos[owner]) == 22, "missing owner hot type-info")
    cold_pointer = typeinfos[owner][21]
    cold_match = re.search(r"(md_(?:record|native)_refl_ti_\w+)\s*\+\s*0\b", cold_pointer)
    require(cold_match is not None and "from_raw" in cold_pointer, "owner type-info lacks attached cold metadata")
    cold_name = cold_match[1]
    require(cold_name in tables and len(tables[cold_name]) == 1, "missing attached cold record")
    cold = tables[cold_name][0]
    method_table = re.sub(r"^md_(?:record|native)_", "", table)
    require(re.search(r"\b(?:md_(?:record|native)_)?" + re.escape(method_table) + r"\b", pointer(cold, 2)),
            "owner cold record does not attach the target method table")
    require(number(cold, 3) == len(tables[table]) and number(cold, 3) > index, "attached method count does not include target row")
    require(typeinfos[owner][13] == "nullptr", "minimal owner no longer uses lazy Type interning")
    canonical_path = lazy_type_path(named, owner)
    print(json.dumps({"component": LABEL, "evidence": "emitted-attachment-only", "owner": owner,
                      "table": table, "row_index": index, "function": function, "invoker": invoker,
                      "cold_record": cold_name, "type_object": "lazy:" + owner, "canonical_path": canonical_path,
                      "body_file": bodies[function]["file"],
                      "data_only_checker_control": "lookup-result-overwrite-rejected",
                      "native_runtime_pass": False}, indent=2, sort_keys=True))


if __name__ == "__main__":
    main()
