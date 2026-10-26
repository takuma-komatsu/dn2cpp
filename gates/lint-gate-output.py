#!/usr/bin/env python3
"""Reject new legacy newline filters and binary-witness spellings in gate scripts."""

import argparse
from collections import Counter
from pathlib import Path
import re
import subprocess
import sys


LEGACY = re.compile(r"\bstrip_cr_win(?:_file)?\b(?!\s*\(\s*\))")
DELETE = re.compile(r"\btr\s+(?:-[A-Za-z]*d[A-Za-z]*|--delete)\s+(\$?'[^']*'|\"[^\"]*\"|[^\s;|]+)")
CR = re.compile(r"\\(?:r|0?15|x0[dD])|\r")
BINARY_ALIAS = re.compile(r"\bgate_output\s+witness\b[^\n]*--binary-container\b")


def violations(source):
    """Keep exact source lines as the ratchet unit; comments are not call sites."""
    result = Counter()
    for line in source.splitlines():
        if line.lstrip().startswith('#'):
            continue
        if LEGACY.search(line) or BINARY_ALIAS.search(line) or any(CR.search(m[1]) for m in DELETE.finditer(line)):
            result[line.strip()] += 1
    return result


def git(*args):
    return subprocess.check_output(['git', *args])


def snapshot(revision):
    paths = git('ls-tree', '-r', '--name-only', '-z', revision, '--', 'gates').split(b'\0')
    return {
        path.decode(): git('show', f'{revision}:{path.decode()}').decode('utf-8')
        for path in paths if path.endswith(b'.sh')
    }


def working_snapshot():
    paths = git('ls-files', '-z', '--cached', '--others', '--exclude-standard', '--', 'gates').split(b'\0')
    return {
        path.decode(): Path(path.decode()).read_text(encoding='utf-8')
        for path in paths if path.endswith(b'.sh') and Path(path.decode()).is_file()
    }


def introduced(before, after):
    return {
        path: additions
        for path, source in after.items()
        if (additions := violations(source) - violations(before.get(path, '')))
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base', required=True, help='review base revision; no implicit or fail-open baseline')
    parser.add_argument('--head', help='revision to inspect; defaults to tracked and untracked working files')
    args = parser.parse_args()
    try:
        before = snapshot(args.base)
        after = snapshot(args.head) if args.head else working_snapshot()
        findings = introduced(before, after)
    except (OSError, UnicodeError, subprocess.CalledProcessError) as error:
        print(f'FAIL: cannot inspect gate output policy: {error}', file=sys.stderr)
        return 2
    for path, lines in sorted(findings.items()):
        for line, count in lines.items():
            print(f'{path}: new legacy output-policy use ({count} occurrence(s)): {line}', file=sys.stderr)
    if findings:
        print('Use raw gate_capture files and explicit gate_output policies; see docs/GATE-OUTPUT.md.', file=sys.stderr)
        return 1
    print('OK: no new legacy gate output-policy uses')
    return 0


if __name__ == '__main__':
    sys.exit(main())
