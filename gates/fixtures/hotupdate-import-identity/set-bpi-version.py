import pathlib
import struct
import sys

source = pathlib.Path(sys.argv[1]).read_bytes()
if len(source) < 64 or source[:8] != b"DN2BPI\0\0":
    raise SystemExit("expected a complete BPI header")
if struct.unpack_from("<I", source, 8)[0] != 2:
    raise SystemExit("expected current BPI format before version mutation")
version = int(sys.argv[3])
if version == 2 or not 0 <= version <= 0xFFFFFFFF:
    raise SystemExit("negative fixture must name another format")
image = bytearray(source)
struct.pack_into("<I", image, 8, version)
pathlib.Path(sys.argv[2]).write_bytes(image)
