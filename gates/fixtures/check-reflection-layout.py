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
    attributed_array = False
    declaration = re.compile(r"\b(md_(?:native|record)_\w+)\s*\[\s*\]\s*=")
    method_table = re.compile(r"md_(native|record)_methtab_(ReflectMetadataLayoutSubset_(?:NativeBase|PackedBase)|ReflectMetadataCompressionSubset_Direct)\[\] = \{")
    for path in output.glob("generated*.cpp"):
        native_owner = None
        with path.open(encoding="utf-8") as stream:
            for line in stream:
                if line.startswith("const Dn2CppTypeInfo ti_arr_ReflectMetadataCompressionSubset_Direct ="):
                    if not line.rstrip().endswith(", nullptr };"):
                        raise ValueError("an element attribute must not create array reflection metadata")
                    attributed_array = True
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
    for name, native in (("ReflectMetadataLayoutSubset_NativeBase", not overridden),
                         ("ReflectMetadataLayoutSubset_PackedBase", overridden),
                         ("ReflectMetadataCompressionSubset_Direct", not overridden)):
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
    for name, native, tables in (
            ("Direct", not overridden, ("fldtab", "methtab", "ctortab", "proptab", "attrtab")),
            ("Middle", True, ("ctortab",)),
            ("Descendant", True, ("ctortab",)),
            ("Custom", True, ("fldtab", "attrtab")),
            ("ValueSubject", True, ("fldtab",)),
            ("EnumSubject", True, ("fldtab", "enummembers")),
            ("DelegateSubject", True, ("methtab",)),
            ("ISubject", True, ("methtab",)),
            ("Implementation", False, ("methtab",)),
            ("Generic_Int32", True, ("fldtab", "methtab", "ctortab")),
            ("Generic_String", not overridden, ("fldtab", "methtab", "ctortab")),
            ("Generic_ReflectMetadataCompressionSubset_Argument", True, ("fldtab", "methtab", "ctortab")),
            ("GenericDescendant", True, ("ctortab",)),
            ("Argument", False, ("fldtab",)),
            ("PlainGeneric_ReflectMetadataCompressionSubset_Direct", False, ("fldtab", "ctortab")),
            ("Outer", True, ("fldtab",)),
            ("Outer_Nested", False, ("fldtab",)),
            ("Container", False, ("fldtab",)),
            ("Container_Nested", True, ("fldtab",)),
            ("CliBase", overridden, ("fldtab",)),
            ("CliDescendant", False, ("ctortab",)),
            ("Other_Subject", False, ("fldtab", "attrtab"))):
        require("refl_ti_ReflectMetadataCompressionSubset_" + name, native)
        for table in tables:
            require(table + "_ReflectMetadataCompressionSubset_" + name, native)
    require("refl_gendef_ReflectMetadataCompressionSubset_Generic_1", True)
    require("refl_gendef_ReflectMetadataCompressionSubset_PlainGeneric_1", False)
    if not attributed_array:
        raise ValueError("attributed array element case was not emitted")
    member_attributes = [name for name in declarations
                         if re.match(r"md_(?:native|record)_attrtab_(?:(?:methtab|ctortab)_)?ReflectMetadataCompressionSubset_Direct_", name)]
    if len(member_attributes) < 5:
        raise ValueError("direct subject must retain field, property, constructor, method, and parameter attributes")
    for name in member_attributes:
        require(re.sub(r"^md_(?:native|record)_", "", name), not overridden)
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
