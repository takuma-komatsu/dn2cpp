import pathlib
import re
import struct
import sys

source = pathlib.Path(sys.argv[1]).read_bytes()
if len(source) < 64 or source[:8] != b"DN2BPI\0\0":
    raise SystemExit("expected a complete BPI header")
current = int(re.search(r"^#define DN2CPP_BPI_VERSION ([0-9]+)u$",
    pathlib.Path("runtime/core/dn2cpp_interp.h").read_text(), re.M).group(1))
if struct.unpack_from("<I", source, 8)[0] != current:
    raise SystemExit("expected current BPI format before version mutation")
version = int(sys.argv[3])
if version == current or not 0 <= version <= 0xFFFFFFFF:
    raise SystemExit("negative fixture must name another format")
image = bytearray(source)
struct.pack_into("<I", image, 8, version)
pathlib.Path(sys.argv[2]).write_bytes(image)
