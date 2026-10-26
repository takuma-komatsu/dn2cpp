#!/usr/bin/env python3
"""Parse once-normalized ISA witness files using binary I/O, never text EOL rules."""
import argparse
from pathlib import Path
import re
import sys


def read_records(path):
    data = Path(path).read_bytes()
    # ISA labels/results have no CR or NUL payload. A surviving CR after the
    # caller's single CRLF normalization is malformed, not another terminator.
    if b'\r' in data or b'\0' in data:
        raise ValueError('malformed ISA output: residual CR or NUL')
    return data.split(b'\n')[:-1] if data.endswith(b'\n') else data.split(b'\n')


def contract(lines):
    active = False
    for line in lines:
        if line == b'== contract ==':
            active = True
        elif line.startswith(b'== '):
            active = False
        elif active:
            yield line


def violations(lines):
    supported = {}
    seen = set()
    active = False
    sections = 0
    for index, line in enumerate(lines):
        # Native N/NM concatenate `all` shards. A shard with no invalid
        # immediates ends in its contract rows, so the next shard's exact
        # adjacent preamble/header is framing, not a malformed token row.
        # Do not ignore arbitrary families= text or reset global evidence.
        if (active and line == b'families=all' and index + 1 < len(lines)
                and lines[index + 1] == b'== contract =='):
            active = False
            continue
        if line == b'== contract ==':
            active = True
            continue
        if line.startswith(b'== '):
            active = False
        if active:
            match = re.fullmatch(rb'([A-Za-z0-9.]+)=(True|False)', line)
            if match:
                supported[match[1]] = match[2]
            elif line:
                yield f'malformed contract row: {line!r}'
        match = re.fullmatch(rb'== invalid ([A-Za-z0-9.]+) ==', line)
        if not match:
            if line.startswith(b'== invalid '):
                yield f'malformed invalid-immediate header: {line!r}'
            continue
        row = match[1]
        name = row.decode('ascii')
        sections += 1
        if row in seen:
            yield f'duplicate invalid-immediate section: {name}'
        seen.add(row)
        if row not in supported:
            yield f'invalid-immediate section has no contract row: {name}'
        if index + 1 >= len(lines):
            yield f'invalid-immediate section has no result: {name}'
            continue
        result = lines[index + 1]
        want = b'ArgumentOutOfRangeException' if supported.get(row) == b'True' else b'PlatformNotSupportedException'
        if b'=' not in result or not result.rsplit(b'=', 1)[0]:
            yield f'malformed invalid-immediate result: {name}: {result!r}'
        got = result.rsplit(b'=', 1)[-1]
        if got != want:
            yield f'{name}: expected {want.decode()}, got {got!r}'
    if sections == 0:
        yield 'no invalid-immediate sections'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=('contract', 'violations', 'adjacent-count', 'plan-values', 'validate'))
    parser.add_argument('file')
    parser.add_argument('arguments', nargs='*')
    args = parser.parse_args()
    try:
        lines = read_records(args.file)
        if args.command == 'validate':
            pass
        elif args.command == 'plan-values':
            if len(args.arguments) != 1 or args.arguments[0] not in ('row', 'exercise', 'invalid'):
                raise ValueError('plan-values requires row, exercise or invalid')
            prefix = args.arguments[0].encode('ascii') + b'='
            values = sorted(line[len(prefix):] for line in lines if line.startswith(prefix))
            sys.stdout.buffer.write(b''.join(value + b'\n' for value in values))
        elif args.command == 'contract':
            rows = list(contract(lines))
            sys.stdout.buffer.write(b''.join(row + b'\n' for row in rows))
        elif args.command == 'violations':
            for diagnostic in violations(lines):
                print(diagnostic)
        else:
            if len(args.arguments) != 2:
                raise ValueError('adjacent-count requires HEADER RESULT')
            header, result = (value.encode('utf-8') for value in args.arguments)
            count = sum(line == header and index + 1 < len(lines) and lines[index + 1] == result
                        for index, line in enumerate(lines))
            sys.stdout.buffer.write(str(count).encode('ascii') + b'\n')
    except (OSError, ValueError) as error:
        # The violations API is a diagnostic-list API: errors must be nonempty,
        # even when a shell caller explicitly tests the resulting string.
        if args.command == 'violations':
            print(error)
        else:
            print(f'FAIL: {args.file}: {error}', file=sys.stderr)
            return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
