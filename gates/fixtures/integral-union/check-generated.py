#!/usr/bin/env python3
"""Exact synthesized bodies: scalar memcpy loads only, never inactive union arms/padding."""
import argparse
import pathlib
import re

# Metadata order, byte offset, exact storage width, enum marker.
FIELDS = {
    "GuidMirror": [(0,"uint32_t",False),(4,"uint32_t",False),(4,"uint16_t",False),(6,"uint16_t",False),(8,"uint32_t",False),(8,"uint16_t",False),(8,"uint8_t",False),(10,"uint16_t",False),(12,"uint32_t",False),(16,"uint8_t",True)],
    "WidthAliases": [(0,"int8_t",False),(0,"uint8_t",False),(2,"int16_t",False),(2,"uint16_t",False),(4,"int32_t",False),(4,"uint32_t",False),(8,"int64_t",False),(8,"uint64_t",False),(8,"int64_t",True)],
    "SmallEnums": [(0,"int8_t",True),(1,"uint8_t",True),(2,"int16_t",True),(4,"uint16_t",True),(8,"int32_t",True),(12,"uint32_t",True)],
    "WideEnums": [(0,"int64_t",True),(8,"uint64_t",True)],
    "PackedIntegral": [(0,"uint8_t",False),(1,"int32_t",False)],
}

def normalize(body):
    return re.sub(r"\s+", "", body)

def declaration(kind, index):
    return f"[[maybe_unused]] {kind} t{index};"

def load(index, offset, kind, source):
    return f"std::memcpy(&t{index}, (const uint8_t*)({source}) + {offset}, sizeof({kind}));"

def eq_value(index, kind, enum):
    value = f"t{index}"
    if kind in ("int8_t","uint8_t","int16_t","uint16_t") or (enum and kind == "uint32_t"):
        value = f"((int32_t){value})"
    elif enum and kind == "uint64_t":
        value = f"((int64_t){value})"
    return value

def hash_value(index, kind, enum):
    value = f"t{index}"
    # Preserve the pre-existing enum hash policy; no numerical CLR parity promise.
    if kind in ("int8_t","uint8_t","int16_t","uint16_t") or (enum and kind != "int32_t"):
        return f"(int32_t)(((int32_t){value}))"
    if kind in ("int64_t", "uint64_t"):
        return f"(int32_t)((uint64_t)({value}) ^ ((uint64_t)({value}) >> 32))"
    return f"(int32_t)({value})"

def expected(fields, equality):
    lines = []
    if equality:
        for i, (_,kind,_) in enumerate(fields):
            lines.extend((declaration(kind,2*i), declaration(kind,2*i+1)))
        for i,(offset,kind,enum) in enumerate(fields):
            lines.extend((load(2*i,offset,kind,"a0"), load(2*i+1,offset,kind,"&(a1)")))
            left, right = eq_value(2*i,kind,enum), eq_value(2*i+1,kind,enum)
            lines.append(f"if (!((({left}) == ({right}) ? 1 : 0))) return 0;")
        lines.append("return 1;")
    else:
        lines.append(declaration("uint32_t",0))
        lines.extend(declaration(kind,i+1) for i,(_,kind,_) in enumerate(fields))
        lines.append("t0 = 0;")
        for i,(offset,kind,enum) in enumerate(fields):
            lines.append(load(i+1,offset,kind,"a0"))
            lines.append(f"t0 = t0 * 2773833001u + (uint32_t)({hash_value(i+1,kind,enum)});")
        lines.append("return (int32_t)t0;")
    return "\n".join(lines)

def verify(sources):
    found = {}
    for name, fields in FIELDS.items():
        for operation, suffix in (("Equals","vteq"),("GetHashCode","vthash")):
            args = f"t_IntegralUnionSubset_{name}* a0"
            if suffix == "vteq": args += f", t_IntegralUnionSubset_{name} a1"
            pattern = re.compile(r"^int32_t " + name + "_" + operation + r"_m\d+__" + suffix + r"\(" + re.escape(args) + r"\)\s*\n\{([^{}]*)\n\}", re.M)
            matches = [(path,m) for path,text in sources.items() for m in pattern.finditer(text)]
            assert len(matches) == 1, f"{name}.{operation}: expected exactly one whole body, got {len(matches)}"
            path, match = matches[0]
            wanted = expected(fields,suffix == "vteq")
            assert normalize(match.group(1)) == normalize(wanted), f"{name}.{operation}: wrong field width/offset/temp/use or extra/omitted operation in {path}"
            found[(name,operation)] = (path,match.group(1))
    return found

def self_test(sources, bodies):
    path,body = bodies[("GuidMirror","Equals")]
    mutations = {
        "padding-offset": body.replace(" + 0, sizeof(uint32_t)"," + 17, sizeof(uint32_t)",1),
        "wrong-width": body.replace("sizeof(uint32_t)","sizeof(uint64_t)",1),
        "wrong-temp-width": body.replace("uint32_t t0;","uint16_t t0;",1),
        "wrong-receiver": body.replace("(const uint8_t*)(a0)","(const uint8_t*)(&(a1))",1),
        "direct-union-read": body.replace("std::memcpy(&t0, (const uint8_t*)(a0) + 0, sizeof(uint32_t));","t0 = a0->f_A;",1),
        "missing-load": body.replace("std::memcpy(&t0, (const uint8_t*)(a0) + 0, sizeof(uint32_t));","",1),
        "whole-struct-memcmp": body.replace("return 1;","return std::memcmp(a0, &a1, sizeof(*a0)) == 0;",1),
        "skipped-comparison": body.replace("if (!(((t0) == (t1) ? 1 : 0))) return 0;","",1),
    }
    count = 0
    for label,mutated in mutations.items():
        assert mutated != body, label + ": mutation failed to target expected statement"
        changed = dict(sources); changed[path] = changed[path].replace(body,mutated,1)
        try: verify(changed)
        except AssertionError: count += 1
        else: raise AssertionError(label + ": checker accepted mutation")
    path,body = bodies[("PackedIntegral","GetHashCode")]
    changed = dict(sources); changed[path] = changed[path].replace(body,body.replace(" + 1, sizeof(int32_t)"," + 4, sizeof(int32_t)",1),1)
    try: verify(changed)
    except AssertionError: count += 1
    else: raise AssertionError("packed-hash-wrong-offset: checker accepted mutation")
    return count

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("directory",type=pathlib.Path)
    parser.add_argument("--self-test",action="store_true")
    args = parser.parse_args()
    sources = {str(p):p.read_text() for p in args.directory.glob("generated*.cpp")}
    bodies = verify(sources)
    print("integral-union-generated:10-exact-field-bodies")
    if args.self_test: print("integral-union-checker-rejected-mutations:"+str(self_test(sources,bodies)))

if __name__ == "__main__": main()
