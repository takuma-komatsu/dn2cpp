"""Compare emitted bytes after validating output-relative publication paths."""
from pathlib import Path
import re
import sys
from tempfile import TemporaryDirectory
import xml.etree.ElementTree as ET
from xml.parsers import expat
from xml.sax.saxutils import escape

from ildiet_manifest import fnv1a, manifest


def normalized_result(output):
    output = output.resolve()
    root, _ = manifest(output)
    content = (output / "ildiet-result.xml").read_bytes()
    parser = expat.ParserCreate()
    edits = []
    depth = 0

    def start(name, attributes):
        nonlocal depth
        fields = ("input", "preservation") if depth == 0 and name == "ildietResult" else (
            ("path",) if depth == 1 and name == "reference" else ())
        if fields:
            offset = parser.CurrentByteIndex
            tag = re.match(rb'''<(?:[^"'<>]|"[^"]*"|'[^']*')*>''', content[offset:])
            assert tag is not None, "invalid publication start tag"
            for attribute in re.finditer(rb'''([^\s=<>/'"]+)\s*=\s*(["'])(.*?)\2''', tag[0], re.S):
                key = attribute[1].decode("utf-8")
                if key in fields:
                    path = Path(attributes[key])
                    assert path.is_absolute(), "publication path is not absolute: " + str(path)
                    relative = path.resolve().relative_to(output).as_posix()
                    value = escape(relative, {'"': "&quot;", "'": "&apos;"}).encode("utf-8")
                    edits.append((offset + attribute.start(3), offset + attribute.end(3), value))
        depth += 1

    def end(_):
        nonlocal depth
        depth -= 1

    parser.StartElementHandler = start
    parser.EndElementHandler = end
    parser.Parse(content, True)
    assert len(edits) == 2 + len(root.findall("reference")), "incomplete publication path normalization"
    # Preserve XML formatting, ordering and every byte outside the path values.
    for begin, end, value in reversed(edits):
        content = content[:begin] + value + content[end:]
    return content


def inventory(output):
    result = {}
    for path in output.rglob("*"):
        assert not path.is_symlink(), "unexpected output symlink: " + str(path)
        result[path.relative_to(output)] = path.is_dir()
    return result


def same_bytes(left, right):
    with left.open("rb") as first, right.open("rb") as second:
        while True:
            a, b = first.read(1024 * 1024), second.read(1024 * 1024)
            if a != b:
                return False
            if not a:
                return True


def compare(left, right):
    result = Path("ildiet/ildiet-result.xml")
    first, second = normalized_result(left / "ildiet"), normalized_result(right / "ildiet")
    a, b = inventory(left), inventory(right)
    assert a == b, "output inventory differs: " + str(sorted(set(a.items()) ^ set(b.items())))
    for name, directory in sorted(a.items()):
        if not directory:
            equal = first == second if name == result else same_bytes(left / name, right / name)
            assert equal, "output bytes differ: " + str(name)


def self_test():
    def fixture(output):
        generation = output / "ildiet/g-0000000000000001"
        generation.mkdir(parents=True)
        (output / "generated.cpp").write_bytes(b"generated body\n")
        (output / "reflection-unrecognized.tsv").write_bytes(b"reflection report\n")
        (generation / "preservation.xml").write_bytes(b"<preservation />\n")
        root = ET.Element("ildietResult", version="2", generation=generation.name,
                          input=str(generation / "App.dll"), preservation=str(generation / "preservation.xml"),
                          fallback="none", contract="0000000000000000", cutsValidated="true",
                          constructorRegistriesRewritten="false")
        for name in ("RefA", "RefB"):
            ET.SubElement(root, "reference", path=str(generation / (name + ".dll")))
        for name in ("App", "RefA", "RefB"):
            body = name.encode("utf-8")
            (generation / (name + ".dll")).write_bytes(body)
            ET.SubElement(root, "assembly", name=name, mode="rewritten", hash=fnv1a(body),
                          registryRewritten="false")
        ET.SubElement(root, "counters", work="1")
        result = output / "ildiet/ildiet-result.xml"
        ET.ElementTree(root).write(result, encoding="utf-8")
        return result, generation

    cases = ("malformed", "generation-escape", "hash", "counter", "contract", "reference-order",
             "assembly-body", "preservation", "generated-body", "report", "extra-file", "xml-format")
    with TemporaryDirectory(prefix="dn2cpp-emit-compare-") as temporary:
        base = Path(temporary).resolve()
        for case in ("relocation", *cases):
            left, right = base / case / "left & path", base / case / "longer right ' path"
            fixture(left)
            result, generation = fixture(right)
            root = ET.parse(result).getroot()
            if case == "malformed":
                result.write_bytes(b"<ildietResult")
            elif case == "generation-escape":
                root.set("input", str(left / "ildiet" / generation.name / "App.dll"))
            elif case == "hash":
                (generation / "App.dll").write_bytes(b"changed")
            elif case == "counter":
                root.find("counters").set("work", "2")
            elif case == "contract":
                root.set("contract", "1111111111111111")
            elif case == "reference-order":
                refs = root.findall("reference")
                root.remove(refs[1])
                root.insert(0, refs[1])
            elif case == "assembly-body":
                body = b"different body with a valid publication hash"
                (generation / "App.dll").write_bytes(body)
                root.find("assembly").set("hash", fnv1a(body))
            elif case == "preservation":
                (generation / "preservation.xml").write_bytes(b"<preservation changed='true' />\n")
            elif case == "generated-body":
                (right / "generated.cpp").write_bytes(b"changed generated body\n")
            elif case == "report":
                (right / "reflection-unrecognized.tsv").write_bytes(b"changed report\n")
            elif case == "extra-file":
                (right / "extra.txt").write_bytes(b"extra")
            elif case == "xml-format":
                result.write_bytes(result.read_bytes() + b"\n")
            if case in ("generation-escape", "counter", "contract", "reference-order", "assembly-body"):
                ET.ElementTree(root).write(result, encoding="utf-8")
            if case == "relocation":
                compare(left, right)
            else:
                try:
                    compare(left, right)
                except (AssertionError, ET.ParseError):
                    pass
                else:
                    raise AssertionError("comparison accepted mutation: " + case)
    print("OK: publication relocation and comparison negative controls")


if __name__ == "__main__":
    try:
        if sys.argv[1:] == ["--self-test"]:
            self_test()
        else:
            assert len(sys.argv) == 3, "usage: compare-emit-output.py LEFT RIGHT"
            compare(Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve())
    except (AssertionError, OSError, ValueError, ET.ParseError, expat.ExpatError) as error:
        print("FAIL: " + str(error), file=sys.stderr)
        sys.exit(1)
