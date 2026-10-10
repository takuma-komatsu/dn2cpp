#!/usr/bin/env python3
"""Model identical-code folding without relying on a platform linker option."""
import pathlib
import re
import sys

out = pathlib.Path(sys.argv[1])
paths = sorted(out.glob("generated*.cpp")) + [out / "generated.h"]
sources = {p: p.read_text(encoding="utf-8") for p in paths}
pattern = re.compile(
    r"// DelegateVirtualIdentitySubset\.[^\n]+\n"
    r"(?:inline )?int32_t (\w+)\((t_DelegateVirtualIdentitySubset_\w+\* a0)\)\n"
    r"\{\n(.*?)\n\}", re.S)
groups = {}
definitions = []
for source in sources.values():
    for match in pattern.finditer(source):
        name, signature, body = match.groups()
        if body.strip() != "[[maybe_unused]] int32_t t0;\n    t0 = 7;\n    return t0;":
            continue
        # Every accepted body ignores its single pointer argument. Its ABI and
        # instructions are identical even when the receiver's C++ name differs.
        groups.setdefault(body, []).append(name)
        definitions.append(match.group(0))
aliases = {}
for names in groups.values():
    for name in names[1:]:
        aliases[name] = names[0]
if len(aliases) < 6:
    raise SystemExit("missing identical fixture bodies")
# String's two enumerator implementations allocate and initialize the same
# CharEnumerator. Their only spelling difference is the returned interface cast.
string_enumerator = re.compile(
    r"inline t_System_Collections_\w+\* "
    r"(String_System_Collections_\w+_GetEnumerator_m\d+)\(Dn2CppString\* a0\)\n"
    r"\{(.*?)\n\}", re.S)
string_methods = [match for source in sources.values()
                  for match in string_enumerator.finditer(source)]
if len(string_methods) != 2:
    raise SystemExit("missing String enumerator bodies")
normalize_string = lambda body: re.sub(r"t_System_Collections_\w+\*", "void*", body)
if normalize_string(string_methods[0][2]) != normalize_string(string_methods[1][2]):
    raise SystemExit("String enumerator bodies are not identical")
aliases[string_methods[1][1]] = string_methods[0][1]
definitions.extend(match.group(0) for match in string_methods)
# Array forwarding thunks differ only in the interface cast and in which of
# these byte-identical cursor factories they call.
array_factory = re.compile(
    r"inline t_System_Collections_\w+\* "
    r"(SZArrayEnumerable_1_(?:System_Collections_IEnumerable_)?GetEnumerator_m\d+)"
    r"\(t_Dn2Cpp_Runtime_SZArrayEnumerable_\w+\* a0\)\n\{(.*?)\n\}", re.S)
factory_groups = {}
for source in sources.values():
    for match in array_factory.finditer(source):
        factory_groups.setdefault(normalize_string(match[2]), []).append(match[1])
        definitions.append(match.group(0))
factory_aliases = {}
for names in factory_groups.values():
    for name in names[1:]:
        factory_aliases[name] = names[0]
if not factory_aliases:
    raise SystemExit("missing identical array cursor factories")
array_thunk = re.compile(r"static t_System_Collections_\w+\* (arrthunk_\w+)\([^\n]+\) \{(.*?)\n\}", re.S)
thunk_groups = {}
for path, source in sources.items():
    for match in array_thunk.finditer(source):
        body = normalize_string(match[2])
        for name, target in factory_aliases.items():
            body = re.sub(r"\b" + name + r"\b", target, body)
        if not any(target + "(" in body for target in factory_aliases.values()):
            continue
        thunk_groups.setdefault((path, body), []).append(match[1])
        definitions.append(match.group(0))
for names in thunk_groups.values():
    for name in names[1:]:
        aliases[name] = names[0]
if not any(name.startswith("arrthunk_") for name in aliases):
    raise SystemExit("missing identical array forwarding thunks")
# Wrappers fold too when their only callee has an identical body. Keep the
# unboxing offset; only receiver type spellings and proven callees normalize.
wrapper = re.compile(r"(?:static )?int32_t ((?:ubthunk_DelegateVirtualIdentitySubset_|dn2cpp_gvm_Generic_1_)\w+)\([^\n]+\)\s*\{([^{}]+)\}")
wrapper_groups = {}
for path, source in sources.items():
    for match in wrapper.finditer(source):
        name, body = match.groups()
        if "if (" in body or "return (int32_t)" not in body:
            continue
        normalized = body
        for folded, target in aliases.items():
            normalized = re.sub(r"\b" + folded + r"\b", target, normalized)
        if not any(target + "(" in normalized for target in aliases.values()):
            continue
        normalized = re.sub(r"t_DelegateVirtualIdentitySubset_\w+", "void", normalized)
        normalized = " ".join(normalized.split())
        scope = path if name.startswith("ubthunk_") else None
        wrapper_groups.setdefault((scope, normalized), []).append(name)
        definitions.append(match.group(0))
for names in wrapper_groups.values():
    for name in names[1:]:
        aliases[name] = names[0]
aliases = {name: target for name, target in aliases.items()
           if any(re.search(r"&" + name + r"\b", source) for source in sources.values())}
originals = sources.copy()
counts = {name: 0 for name in aliases}
for path, source in sources.items():
    for name, target in aliases.items():
        source, count = re.subn(r"&" + name + r"\b", "&" + target, source)
        counts[name] += count
    sources[path] = source
if any(count == 0 for count in counts.values()):
    raise SystemExit("a folded fixture method has no address reference")
# The complete generated sources may differ only at function-address loads;
# this checks all metadata and selected-key initializers, not just declarations.
address = re.compile(r"&(?:" + "|".join(re.escape(n) for n in set(aliases) | set(aliases.values())) + r")\b")
for path in sources:
    if address.sub("&folded_fixture_body", originals[path]) != address.sub("&folded_fixture_body", sources[path]):
        raise SystemExit("folding changed more than function-address references")
joined = "\n".join(sources.values())
if any(definition not in joined for definition in definitions):
    raise SystemExit("folding changed a method definition")
checks = []
for source in sources.values():
    for ti, entries in re.findall(r"static const void\* vt_(DelegateVirtualIdentitySubset_\w+)\[\] = \{ ([^\n]+) \};", source):
        slots = re.findall(r"&([A-Za-z0-9_]+)", entries)
        repeated = {}
        for slot, name in enumerate(slots):
            if name in aliases.values():
                if name in repeated:
                    i = len(checks)
                    checks += [f"    const void* volatile fold_a{i} = ti_{ti}.vtable[{repeated[name]}];",
                               f"    const void* volatile fold_b{i} = ti_{ti}.vtable[{slot}];",
                               f"    if (fold_a{i} != fold_b{i}) std::abort();"]
                else:
                    repeated[name] = slot
if len(checks) < 9:
    raise SystemExit("missing folded virtual-slot collisions")
checks += ["    const void* volatile box_a = dn2cpp_resolve_interface(&ti_DelegateVirtualIdentitySubset_Boxed, &ti_DelegateVirtualIdentitySubset_IFirst)[0];",
           "    const void* volatile box_b = dn2cpp_resolve_interface(&ti_DelegateVirtualIdentitySubset_Boxed, &ti_DelegateVirtualIdentitySubset_IAlias)[0];",
           "    if (box_a != box_b) std::abort();"]
string_tables = []
for method in string_methods:
    tables = [table for source in originals.values()
              for table in re.findall(r"static const void\* (str_itf_\d+)\[\] = \{ \(const void\*\)&"
                                      + re.escape(method[1]) + r" \};", source)]
    if len(tables) != 1:
        raise SystemExit("missing String dispatch address")
    string_tables.extend(tables)
checks += [f"    const void* volatile string_a = {string_tables[0]}[0];",
           f"    const void* volatile string_b = {string_tables[1]}[0];",
           "    if (string_a != string_b) std::abort();"]
checks += ["    const void* volatile array_a = dn2cpp_resolve_interface(&ti_arr_String, &ti_System_Collections_IEnumerable)[0];",
           "    const void* volatile array_b = dn2cpp_resolve_interface(&ti_arr_String, &ti_System_Collections_Generic_IEnumerable_String)[0];",
           "    if (array_a != array_b) std::abort();"]
checks.append(f'    std::fputs("delegate folded addresses: {len(checks) // 3}\\n", stderr);')
main = out / "generated.cpp"
sources[main] = '#include <cstdio>\n#include <cstdlib>\n' + sources[main]
sources[main], count = re.subn(r"(    dn2cpp_runtime_init\(\);\n)",
    lambda m: m.group(0) + "\n" + "\n".join(checks), sources[main])
if count != 1:
    raise SystemExit("missing native main")
for path, source in sources.items():
    path.write_text(source, encoding="utf-8")
print(f"folded delegate methods: {len(aliases)}; address references: {sum(counts.values())}")
