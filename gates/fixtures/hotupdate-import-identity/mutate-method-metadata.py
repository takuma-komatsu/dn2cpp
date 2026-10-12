import pathlib
import struct
import sys

source, destination, mutation = sys.argv[1:]
image = bytearray(pathlib.Path(source).read_bytes())
section_count, section_start = struct.unpack_from("<II", image, 24)
sections = {}
for index in range(section_count):
    entry = section_start + index * 16
    kind, start, size, count = struct.unpack_from("<IIII", image, entry)
    sections[kind] = (entry, start, size, count)
if 13 not in sections or 12 not in sections:
    raise SystemExit("expected interpreted interface method metadata")
entry, start, size, count = sections[13]
if mutation == "extent":
    struct.pack_into("<I", image, entry + 12, count + 1)
elif mutation == "parameter-count":
    struct.pack_into("<I", image, start + 32, 0xFFFFFFFF)
elif mutation == "return-type":
    struct.pack_into("<I", image, start + 16, 0x8000000E)
elif mutation == "ownership":
    _, method_start, _, _ = sections[5]
    first_method = struct.unpack_from("<I", image, start)[0]
    first_owner = struct.unpack_from("<I", image, method_start + first_method * 40 + 4)[0]
    replacement = None
    for index in range(count):
        candidate = struct.unpack_from("<I", image, start + index * 36)[0]
        owner = struct.unpack_from("<I", image, method_start + candidate * 40 + 4)[0]
        if owner != first_owner:
            replacement = candidate
            break
    if replacement is None:
        raise SystemExit("expected implementations on distinct patch owners")
    _, interface_start, interface_size, _ = sections[12]
    cursor = interface_start
    end = cursor + interface_size
    changed = False
    while cursor < end:
        interface_count = struct.unpack_from("<I", image, cursor)[0]
        cursor += 4
        for _ in range(interface_count):
            _, _, implementation_count = struct.unpack_from("<III", image, cursor)
            cursor += 12
            for _ in range(implementation_count):
                method = struct.unpack_from("<I", image, cursor + 4)[0]
                if method == first_method:
                    struct.pack_into("<I", image, cursor + 4, replacement)
                    changed = True
                cursor += 8
    if not changed:
        raise SystemExit("missing interface slot using the selected implementation")
else:
    raise SystemExit("unknown metadata mutation")
pathlib.Path(destination).write_bytes(image)
