#!/usr/bin/env python3
"""Record the actual driver request without starting a companion."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import sys
import xml.etree.ElementTree as ET


def capture():
    assert sys.argv[1::2] == ['--request', '--result'] and len(sys.argv) == 5
    request = Path(sys.argv[2])
    destination = Path(os.environ['CUT_CAPTURE_DIR'])
    assert request.is_absolute() and destination.is_absolute()
    destination.mkdir(parents=True, exist_ok=False)
    shutil.copyfile(request, destination / 'request.xml')
    root = ET.parse(request).getroot()
    paths = [Path(root.attrib['input']), *(Path(n.attrib['path']) for n in root.findall('reference'))]
    rows = []
    for path in paths:
        assert path.is_absolute() and path.is_file() and not path.is_symlink()
        rows.append({'path': str(path), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
    with (destination / 'capture.json').open('x') as stream:
        json.dump({'kind': 'cut-request-only', 'companion_started': False, 'exit_code': 86,
                   'argv': sys.argv, 'inputs': rows}, stream, indent=2, sort_keys=True)
    return 86


if __name__ == '__main__':
    raise SystemExit(capture())
