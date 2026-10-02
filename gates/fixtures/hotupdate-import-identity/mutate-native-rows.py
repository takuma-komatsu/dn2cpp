import ast
import pathlib
import re
import sys

root = pathlib.Path(sys.argv[1])
axis = sys.argv[2]
files = list(root.glob("generated*.cpp")) + list(root.glob("generated*.h"))
texts = {path: path.read_text() for path in files}
pool_matches = []
for path, text in texts.items():
    pool_matches.extend(re.finditer(r"extern const char md_name_pool\[\] =\s*(.*?)\n;", text, re.S))
if len(pool_matches) != 1:
    raise SystemExit("expected one generated metadata name pool")
pool = b"".join(ast.literal_eval(token).encode("utf-8") for token in
                re.findall(r'"(?:[^"\\]|\\.)*"', pool_matches[0].group(1)))
names = {}
for text in texts.values():
    for match in re.finditer(r"md_name_(\d+) = md_name_pool \+ (\d+);", text):
        start = int(match.group(2))
        end = pool.index(b"\0", start) if b"\0" in pool[start:] else len(pool)
        names["md_name_" + match.group(1)] = pool[start:end].decode("utf-8")


def fields_of(row):
    fields = []
    start = 0
    depth = 0
    for index, char in enumerate(row):
        if char in "(<{":
            depth += 1
        elif char in ")>}":
            depth -= 1
        elif char == "," and depth == 0:
            fields.append(row[start:index].strip())
            start = index + 1
    fields.append(row[start:].strip())
    return fields


changed = 0
expected = 2 if axis == "legacy-ambiguous" else 1
for path in root.glob("generated*.cpp"):
    text = path.read_text()
    def table(match):
        global changed
        def row(match_row):
            global changed
            fields = fields_of(match_row.group(1))
            if len(fields) < 12:
                raise SystemExit("native MethodInfo row does not include sigShape")
            method = names.get(fields[0])
            shape = names.get(fields[11])
            if axis == "duplicate" and method == "TypeName" and shape == "<String>():String":
                candidates = [symbol for symbol, value in names.items() if value == "<Int32>():String"]
                if len(candidates) != 1:
                    raise SystemExit("expected one Int32 import shape name")
                fields[11] = candidates[0]
            elif axis == "missing" and method == "TypeName" and shape == "<Int32>():String":
                fields[11] = '"__missing_closed_instantiation__"'
            elif axis == "legacy-single" and method == "Name" and shape == "():Int32":
                fields[11] = "nullptr"
            elif axis == "legacy-ambiguous" and method == "Pair" and shape in ("<Int32>():Int32", "<String>():Int32"):
                fields[11] = "nullptr"
            else:
                return match_row.group(0)
            changed += 1
            return "    { " + ", ".join(fields) + " },"
        body = re.sub(r"^    \{ (.*?) \},$", row, match.group(2), flags=re.M)
        return match.group(1) + body + match.group(3)
    text = re.sub(r"((?:static|extern) const Dn2CppMethodInfo \w+\[\] = \{\n)(.*?)(^\};)", table, text, flags=re.M | re.S)
    if text != path.read_text():
        path.write_text(text)
if changed != expected:
    raise SystemExit("native row mutation count mismatch: " + str(changed) + " vs " + str(expected))
print("native sigShape mutation:" + axis + ":" + str(changed))
