#!/usr/bin/env python3
"""Assert exact per-type metadata representation in the reflection parity image."""

import pathlib
import re
import sys


def main():
    output = pathlib.Path(sys.argv[1])
    overridden = sys.argv[2] == "overrides"
    uncompressed = sys.argv[2] == "uncompressed"
    declarations = set()
    native_parameters = {}
    packed_blocks = {}
    pointer_parameters = {}
    declaration = re.compile(r"\b(md_(?:native|record)_\w+)\s*\[\s*\]\s*=")
    method_table = re.compile(r"md_(native|record)_methtab_ReflectMetadataLayoutSubset_(NativeBase|PackedBase)\[\] = \{")
    for path in output.glob("generated*.cpp"):
        native_owner = None
        with path.open(encoding="utf-8") as stream:
            for line in stream:
                declarations.update(declaration.findall(line))
                match = method_table.search(line)
                if match:
                    layout, owner = match.groups()
                    if layout == "native":
                        native_owner = owner
                        native_parameters[owner] = set()
                    else:
                        encoded = list(map(int, line[match.end():].split("}", 1)[0].strip(" ,").split(",")))
                        block = 0
                        for shift, byte in enumerate(encoded):
                            block |= (byte & 127) << (shift * 7)
                            if byte < 128:
                                break
                        packed_blocks[owner] = block
                if native_owner is not None:
                    native_parameters[native_owner].update(re.findall(r"\bparmpool_\d+\b", line))
                    if line.strip() == "};":
                        native_owner = None
                pool = re.search(r"\bmd_ptr_(\d+)\[\] =", line)
                if pool:
                    pointer_parameters[int(pool[1])] = set(re.findall(r"\bmd_(?:native|record)_parmpool_\d+\b", line))

    def require(table, native):
        native = native or uncompressed
        wanted = "md_" + ("native_" if native else "record_") + table
        unwanted = "md_" + ("record_" if native else "native_") + table
        if wanted not in declarations or unwanted in declarations:
            raise ValueError("expected only " + wanted)

    def subject(name, native, tables):
        require("refl_ti_ReflectMetadataLayoutSubset_" + name, native)
        for table in tables:
            require(table + "_ReflectMetadataLayoutSubset_" + name, native)

    subject("NativeBase", not overridden, ("fldtab", "methtab", "ctortab", "proptab"))
    subject("PackedBase", overridden, ("fldtab", "methtab", "ctortab", "proptab"))
    require("attrtab_ReflectMetadataLayoutSubset_NativeBase", not overridden)
    require("attrtab_ReflectMetadataLayoutSubset_PackedBase", overridden)
    for name, native in (("NativeBase", not overridden), ("PackedBase", overridden)):
        if native or uncompressed:
            parameters = native_parameters.get(name, set())
            for table in parameters:
                require(table, True)
        else:
            parameters = pointer_parameters.get(packed_blocks.get(name), set())
            if any(not table.startswith("md_record_") for table in parameters):
                raise ValueError("packed methods borrow native parameter rows: " + name)
        if not parameters:
            raise ValueError("no parameter rows checked for " + name)
    subject("NativeDerived", True, ("ctortab",))
    subject("PackedDerived", False, ("ctortab",))
    subject("Generic_Int32", True, ("fldtab", "methtab", "ctortab"))
    subject("Generic_String", not overridden, ("fldtab", "methtab", "ctortab"))
    subject("Generic_Object", overridden, ("fldtab", "methtab", "ctortab"))
    require("refl_gendef_ReflectMetadataLayoutSubset_Generic_1", True)
    subject("GenericToken", True, ("fldtab",))
    subject("NativeArrayElement", False, ("fldtab",))
    subject("PackedArrayElement", False, ("fldtab",))
    subject("IRead", False, ("methtab",))
    for argument in ("Boolean", "Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64"):
        subject("InvokeFamily_" + argument, False, ("methtab",))
    for name, native in (("Subject", True), ("Packed", False)):
        for table in ("refl_ti", "fldtab", "methtab"):
            require(table + "_ReflectMetadataMeasureSubset_" + name, native)
    for name, native in (("SignedBoundary", False), ("UnsignedBoundary", True)):
        for table in ("refl_ti", "fldtab", "enummembers"):
            require(table + "_ReflectMetadataPreservationSubset_" + name, native)
    if uncompressed:
        packed = sorted(name for name in declarations if name.startswith("md_record_"))
        if packed:
            raise ValueError("compression disabled but packed metadata remains: " + ", ".join(packed))
        for path in output.glob("generated*.cpp"):
            with path.open(encoding="utf-8") as stream:
                for line in stream:
                    if re.search(r"\bmd_display_(?:tokens|\d+)\[\]\s*=\s*\{", line):
                        raise ValueError("compression disabled but encoded display metadata remains")
    print("reflection metadata type layouts OK (" + sys.argv[2] + ")")


if __name__ == "__main__":
    main()
