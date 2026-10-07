import pathlib
import re
import sys

root = pathlib.Path(sys.argv[1])
owners = {"HotUpdateBase_UnavailableAggregateProbe", "HotUpdateBase_UnavailableOrdinaryProbe"}
changes = {}
seen = set()
pattern = re.compile(r"((?:static|extern) const Dn2CppMethodInfo md_native_ctortab_(\w+)\[\] = \{\n)(.*?)(^\};)", re.M | re.S)
for path in sorted(root.glob("generated*.cpp")):
    original = path.read_text(encoding="utf-8")

    def table(match):
        owner = match.group(2)
        if owner not in owners:
            return match.group(0)
        if owner in seen:
            raise SystemExit("duplicate unavailable constructor table: " + owner)
        rows = list(re.finditer(r"^    \{ (.*?) \},$", match.group(3), re.M))
        if len(rows) != 1:
            raise SystemExit("expected one native constructor row: " + owner)
        row = rows[0]
        fields = row.group(1).split(",")
        if len(fields) != 33 or fields[1].strip() != "&ti_" + owner or fields[30].strip() != "0" or fields[31].strip() != "0" or fields[32].strip() != "0":
            raise SystemExit("unavailable constructor row layout changed: " + owner)
        body = fields[7].strip()
        invoker = fields[8].strip()
        constructor = owner[len("HotUpdateBase_"):] + "__ctor_m"
        if not re.fullmatch(r"\(void\*\)&" + constructor + r"\d+", body):
            raise SystemExit("expected reached constructor body: " + owner)
        if not re.fullmatch(r"\(void\*\)&inv_\w+", invoker):
            raise SystemExit("expected reached constructor invoker: " + owner)
        # Change only the two pointer fields; keep row identity and signature intact.
        fields[7] = " nullptr"
        fields[8] = " nullptr"
        new_row = "    { " + ",".join(fields) + " },"
        block = match.group(3)
        seen.add(owner)
        return match.group(1) + block[:row.start()] + new_row + block[row.end():] + match.group(4)

    updated = pattern.sub(table, original)
    if updated != original:
        changes[path] = updated
if seen != owners:
    raise SystemExit("missing native unavailable constructor tables: " + ",".join(sorted(owners - seen)))
for path, updated in changes.items():
    path.write_text(updated, encoding="utf-8", newline="\n")
print("native aggregate/ordinary constructor bodies unavailable:2")
