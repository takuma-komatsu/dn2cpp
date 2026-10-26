#!/usr/bin/env python3
"""Byte-preserving output assertions; CRLF equivalence is always opt-in.

Record operations treat LF as a separator and one final LF as a terminator,
not an extra empty record. Empty interior and extra trailing records survive.
Text assertions reject NUL. Byte witnesses inspect NUL-free records in arbitrary
byte containers; binary/encoding parity still requires exact byte comparison.
"""

import argparse
from collections import Counter
from pathlib import Path
import re
import sys


class OutputError(ValueError):
    pass


INFRASTRUCTURE_STATUSES = frozenset((124, 125, 126, 127, 130, 137, 143))


def normalize(data, policy="exact", terminal_newline="exact"):
    if policy not in ("exact", "crlf") or terminal_newline not in ("exact", "optional"):
        raise OutputError("unknown output policy")
    if (policy != "exact" or terminal_newline != "exact") and b"\0" in data:
        raise OutputError("NUL in semantic output; use exact byte comparison")
    if policy == "crlf":
        data = data.replace(b"\r\n", b"\n")
    if terminal_newline == "optional" and data.endswith(b"\n"):
        data = data[:-1]
    return data


def framed_records(data, policy="exact"):
    if policy not in ("exact", "crlf"):
        raise OutputError("unknown record framing policy")
    if policy == "crlf":
        data = data.replace(b"\r\n", b"\n")
    if not data:
        return []
    lines = data.split(b"\n")
    if data.endswith(b"\n"):
        lines.pop()
    return lines


def records(data, policy="exact"):
    if b"\0" in data:
        raise OutputError("NUL in text records; use byte-witness for execution markers")
    return framed_records(data, policy)


def write_view(source, destination, data):
    source, destination = Path(source), Path(destination)
    if (source.resolve() == destination.resolve()
            or (destination.exists() and source.samefile(destination))):
        raise OutputError("view destination must not overwrite raw input")
    destination.write_bytes(data)


def read(path):
    return Path(path).read_bytes()


def mismatch(actual, expected, subject):
    if actual != expected:
        limit = min(len(actual), len(expected))
        index = next((i for i in range(limit) if actual[i] != expected[i]), limit)
        raise OutputError(f"{subject} differs at index {index}; "
                          f"actual length {len(actual)}, expected length {len(expected)}; "
                          f"actual {actual[index:index + 80]!r}, "
                          f"expected {expected[index:index + 80]!r}")


def assert_witness(lines, witness, count):
    if b"\n" in witness or b"\0" in witness:
        raise OutputError("a witness must be one NUL-free record")
    found = lines.count(witness)
    if (found == 0 if count == "any" else found != count):
        raise OutputError(f"record {witness!r}: expected {count} occurrence(s), found {found}")


def assert_ordered_witnesses(lines, expected):
    """Require exact multiplicity and order, allowing unrelated records between."""
    if not expected:
        raise OutputError("witness list must not be empty")
    counts = Counter(expected)
    for witness, count in counts.items():
        assert_witness(lines, witness, count)
    mismatch([line for line in lines if line in counts], expected, "witness order")


def section(lines, start, end):
    if start == end:
        raise OutputError("section boundaries must be distinct")
    assert_witness(lines, start, 1)
    assert_witness(lines, end, 1)
    first, last = lines.index(start), lines.index(end)
    if first >= last:
        raise OutputError("section boundaries are out of order")
    return lines[first + 1:last]


def nonnegative(value):
    if value == "any":
        return value
    number = int(value)
    if number < 0:
        raise argparse.ArgumentTypeError("count must be nonnegative")
    return number


def omission_count(value):
    if value == "all":
        return value
    number = int(value)
    if number < 0:
        raise argparse.ArgumentTypeError("count must be nonnegative or all")
    return number


def abort_observation(prefix, diagnostic, witness=None, empty_stdout=False):
    evidence = (diagnostic,) if empty_stdout else (diagnostic, witness)
    if any(not value or '\n' in value or '\r' in value or '\0' in value
           for value in evidence):
        raise OutputError("abort evidence must name nonempty single records")
    receipt = read(prefix + ".status")
    if re.fullmatch(rb'(0|[1-9][0-9]*)\n', receipt) is None:
        raise OutputError(f"invalid exit receipt: {prefix}.status")
    stdout = read(prefix + ".stdout")
    if empty_stdout:
        mismatch(stdout, b"", "declared empty abort stdout")
    else:
        assert_witness(records(stdout, "crlf"), witness.encode('utf-8'), 1)
    diagnostics = records(read(prefix + ".stderr"), "crlf")
    if not diagnostics or diagnostics[0] != diagnostic.encode('utf-8'):
        raise OutputError(f"expected first abort diagnostic missing: {prefix}.stderr")
    return int(receipt), stdout


def assert_abort_status(code, host):
    if code in (0, 1):
        raise OutputError(f"expected abnormal exit, got {code}")
    # A Windows/MSYS std::abort result can collide with the launch-failure code.
    # This exception is reached only after the declared program evidence passes.
    if code in INFRASTRUCTURE_STATUSES and not (code == 127 and host == "windows"):
        raise OutputError(f"infrastructure exit status: {code}")


def parser():
    root = argparse.ArgumentParser(description=__doc__)
    commands = root.add_subparsers(dest="command", required=True)
    for name in ("compare", "records", "prefix", "suffix", "section", "witnesses"):
        command = commands.add_parser(name)
        command.add_argument("actual")
        command.add_argument("expected")
        command.add_argument("--policy", choices=("exact", "crlf"), default="exact")
        if name == "compare":
            command.add_argument("--terminal-newline", choices=("exact", "optional"), default="exact")
        if name == "prefix":
            command.add_argument("--before")
        if name == "suffix":
            command.add_argument("--after")
        if name == "section":
            command.add_argument("--start", required=True)
            command.add_argument("--end", required=True)
    for name in ("byte-prefix", "byte-suffix"):
        command = commands.add_parser(name)
        command.add_argument("actual")
        command.add_argument("expected")
    for name in ("witness", "byte-witness"):
        command = commands.add_parser(name)
        command.add_argument("actual")
        command.add_argument("record", nargs="+")
        command.set_defaults(binary_container=name == "byte-witness")
        if name == "witness":
            command.add_argument("--binary-container", action="store_true")
        command.add_argument("--count", type=nonnegative, default=1)
        command.add_argument("--policy", choices=("exact", "crlf"), default="exact")
    command = commands.add_parser("match-record")
    command.add_argument("actual")
    command.add_argument("pattern")
    command.add_argument("--count", type=nonnegative, default=1)
    command.add_argument("--policy", choices=("exact", "crlf"), default="exact")
    command = commands.add_parser("view")
    command.add_argument("actual")
    command.add_argument("output")
    command.add_argument("--policy", choices=("crlf",), required=True)
    command = commands.add_parser("select")
    command.add_argument("actual")
    command.add_argument("output")
    command.add_argument("--policy", choices=("exact", "crlf"), required=True)
    selection = command.add_mutually_exclusive_group()
    selection.add_argument("--before")
    selection.add_argument("--before-prefix")
    selection.add_argument("--first-record", action="store_true")
    selection.add_argument("--from", dest="from_record")
    command.add_argument("--through")
    selection.add_argument("--exclude-start")
    command.add_argument("--exclude-end")
    command = commands.add_parser("omit-prefix")
    command.add_argument("actual")
    command.add_argument("output")
    command.add_argument("--prefix", action="append", required=True)
    command.add_argument("--count", type=omission_count, default=1)
    command.add_argument("--policy", choices=("exact", "crlf"), required=True)
    command = commands.add_parser("status")
    command.add_argument("actual", type=int)
    command.add_argument("expected", type=int)
    command.add_argument("--policy", choices=("exact", "success-class"), default="exact")
    command = commands.add_parser("abort-parity")
    command.add_argument("actual")
    command.add_argument("expected")
    command.add_argument("--host", choices=("windows", "linux", "macos"), required=True)
    command.add_argument("--witness", required=True)
    command.add_argument("--diagnostic", required=True)
    command.add_argument("--oracle-diagnostic")
    command = commands.add_parser("wasm-abort")
    command.add_argument("actual")
    command.add_argument("expected")
    command.add_argument("--host", choices=("windows", "linux", "macos"), required=True)
    command.add_argument("--witness", required=True)
    command.add_argument("--diagnostic", required=True)
    command.add_argument("--oracle-diagnostic", required=True)
    command = commands.add_parser("native-abort")
    command.add_argument("actual")
    command.add_argument("--host", choices=("windows", "linux", "macos"), required=True)
    command.add_argument("--diagnostic", required=True)
    evidence = command.add_mutually_exclusive_group(required=True)
    evidence.add_argument("--witness")
    evidence.add_argument("--empty-stdout", action="store_true")
    return root


def run(args):
    if args.command == "wasm-abort":
        actual, actual_stdout = abort_observation(args.actual, args.diagnostic, args.witness)
        expected, expected_stdout = abort_observation(args.expected, args.oracle_diagnostic, args.witness)
        mismatch(normalize(actual_stdout, "crlf"), normalize(expected_stdout, "crlf"), "abort stdout bytes")
        if actual != 1:
            raise OutputError(f"expected Node abort status 1, got {actual}")
        assert_abort_status(expected, args.host)
        return
    if args.command == "abort-parity":
        actual, actual_stdout = abort_observation(args.actual, args.diagnostic, args.witness)
        expected, expected_stdout = abort_observation(args.expected, args.oracle_diagnostic if args.oracle_diagnostic is not None else args.diagnostic, args.witness)
        mismatch(actual_stdout, expected_stdout, "abort stdout bytes")
        if actual != expected:
            raise OutputError(f"expected equal abnormal exits, got {actual} vs {expected}")
        assert_abort_status(actual, args.host)
        return
    if args.command == "native-abort":
        code, _ = abort_observation(args.actual, args.diagnostic, args.witness, args.empty_stdout)
        assert_abort_status(code, args.host)
        return
    if args.command == "status":
        # Shell launch failures, watchdogs and termination signals are not program
        # observations. Equal infrastructure failures must never cache as parity.
        if args.actual in INFRASTRUCTURE_STATUSES or args.expected in INFRASTRUCTURE_STATUSES:
            raise OutputError(f"infrastructure exit status: {args.actual} vs {args.expected}")
        if args.policy == "success-class":
            if (args.actual == 0) != (args.expected == 0):
                raise OutputError(f"exit success class differs: {args.actual} vs {args.expected}")
        elif args.actual != args.expected:
            raise OutputError(f"exit status {args.actual}, expected {args.expected}")
        return
    if args.command in ("byte-prefix", "byte-suffix"):
        actual, expected = read(args.actual), read(args.expected)
        selected = actual[:len(expected)] if args.command == "byte-prefix" else (actual[-len(expected):] if expected else b"")
        mismatch(selected, expected, "output bytes")
        return
    if args.command == "view":
        write_view(args.actual, args.output, normalize(read(args.actual), args.policy))
        return
    if args.command == "omit-prefix":
        data = read(args.actual)
        raw = data.split(b"\n") if data else []
        terminated = data.endswith(b"\n")
        if terminated:
            raw.pop()
        matching = [line[:-1] if args.policy == "crlf" and line.endswith(b"\r")
                    and (index < len(raw) - 1 or terminated) else line
                    for index, line in enumerate(raw)]
        prefixes = [prefix.encode("utf-8") for prefix in args.prefix]
        if any(not prefix or b"\n" in prefix or b"\0" in prefix for prefix in prefixes):
            raise OutputError("prefixes must be nonempty NUL-free records and count nonnegative")
        omitted = set()
        for prefix in prefixes:
            indices = [index for index, line in enumerate(matching) if line.startswith(prefix)]
            if args.count != "all" and len(indices) != args.count:
                raise OutputError(f"prefix {prefix!r}: expected {args.count} occurrence(s), found {len(indices)}")
            omitted.update(indices)
        selected = b"".join(line + (b"\n" if index < len(raw) - 1 or terminated else b"")
                            for index, line in enumerate(raw) if index not in omitted)
        write_view(args.actual, args.output, selected)
        return
    if args.command == "select":
        if args.first_record:
            if args.through is not None or args.exclude_end is not None:
                raise OutputError("--first-record cannot combine with other boundaries")
            data = read(args.actual)
            if not data:
                raise OutputError("first record is missing")
            end = data.find(b"\n")
            write_view(args.actual, args.output, data if end < 0 else data[:end + 1])
            return
        if not any(value is not None for value in (args.before, args.before_prefix, args.from_record, args.through, args.exclude_start)):
            raise OutputError("select requires --before, --before-prefix, --from, --through or --exclude-start")
        if args.through is not None and (args.before is not None or args.before_prefix is not None or args.exclude_start is not None):
            raise OutputError("--through can only combine with --from")
        data = read(args.actual)
        # Selection preserves raw bytes, including binary records outside the
        # marker. Only downstream text assertions require NUL-free content.
        matching = data.replace(b"\r\n", b"\n") if args.policy == "crlf" else data
        lines = matching.split(b"\n") if matching else []
        if matching.endswith(b"\n"):
            lines.pop()
        if args.exclude_start is not None:
            if args.exclude_end is None:
                raise OutputError("--exclude-start requires --exclude-end")
            start, end = args.exclude_start.encode("utf-8"), args.exclude_end.encode("utf-8")
            section(lines, start, end)
            first, last = lines.index(start), lines.index(end)
            raw = data.split(b"\n")
            left = sum(len(line) + 1 for line in raw[:first])
            right = min(len(data), sum(len(line) + 1 for line in raw[:last + 1]))
            selected = data[:left] + data[right:]
        else:
            if args.exclude_end is not None:
                raise OutputError("--exclude-end requires --exclude-start")
            if args.before_prefix is not None:
                prefix = args.before_prefix.encode("utf-8")
                if not prefix or b"\n" in prefix or b"\0" in prefix:
                    raise OutputError("boundary prefix must be nonempty and NUL-free without LF")
                indices = [i for i, line in enumerate(lines) if line.startswith(prefix)]
                if len(indices) != 1:
                    raise OutputError(f"boundary prefix {prefix!r}: expected one record, found {len(indices)}")
                index = indices[0]
            else:
                marker = (args.before if args.before is not None else
                          (args.from_record if args.from_record is not None else args.through)).encode("utf-8")
                assert_witness(lines, marker, 1)
                index = lines.index(marker)
            # Marker matching never rewrites the evidence. Applying CRLF
            # equivalence again would consume a lone CR in CR-CR-LF.
            raw = data.split(b"\n")
            offset = sum(len(line) + 1 for line in raw[:index])
            if args.through is not None:
                end = args.through.encode("utf-8")
                assert_witness(lines, end, 1)
                last = lines.index(end)
                if last < index:
                    raise OutputError("selection boundaries are out of order")
                right = min(len(data), sum(len(line) + 1 for line in raw[:last + 1]))
                selected = data[offset if args.from_record is not None else 0:right]
            else:
                selected = data[:offset] if args.before is not None or args.before_prefix is not None else data[offset:]
        write_view(args.actual, args.output, selected)
        return
    if args.command == "compare":
        mismatch(normalize(read(args.actual), args.policy, args.terminal_newline),
                 normalize(read(args.expected), args.policy, args.terminal_newline), "output bytes")
        return
    if args.command == "match-record":
        try:
            pattern = re.compile(args.pattern.encode("utf-8"))
        except re.error as error:
            raise OutputError(f"invalid byte-record pattern: {error}") from error
        count = sum(pattern.fullmatch(line) is not None for line in records(read(args.actual), args.policy))
        if (args.count == "any" and count == 0) or (args.count != "any" and count != args.count):
            raise OutputError(f"record pattern {args.pattern!r}: expected {args.count}, found {count}")
        return
    if args.command in ("witness", "byte-witness") and args.binary_container:
        # Only NUL-free whole-record needles are allowed. Unrelated binary
        # records remain intact and cannot match their NUL-deleted spelling.
        actual = framed_records(read(args.actual), args.policy)
    else:
        actual = records(read(args.actual), args.policy)
    if args.command in ("witness", "byte-witness"):
        for record in args.record:
            assert_witness(actual, record.encode("utf-8"), args.count)
        return
    expected = records(read(args.expected), args.policy)
    if args.command == "witnesses":
        assert_ordered_witnesses(actual, expected)
        return
    if args.command == "section":
        actual = section(actual, args.start.encode("utf-8"), args.end.encode("utf-8"))
    elif args.command == "prefix":
        if args.before is not None:
            marker = args.before.encode("utf-8")
            assert_witness(actual, marker, 1)
            actual = actual[:actual.index(marker)]
        else:
            actual = actual[:len(expected)]
    elif args.command == "suffix":
        if args.after is not None:
            marker = args.after.encode("utf-8")
            assert_witness(actual, marker, 1)
            actual = actual[actual.index(marker) + 1:]
        else:
            actual = actual[-len(expected):] if expected else []
    mismatch(actual, expected, "output records")


def main(argv=None):
    args = parser().parse_args(argv)
    try:
        run(args)
    except (OSError, OutputError) as error:
        paths = []
        for name in ("actual", "expected", "output"):
            value = getattr(args, name, None)
            if isinstance(value, str):
                paths.append(f"{name}={value!r}")
        context = " (" + ", ".join(paths) + ")" if paths else ""
        print(f"FAIL{context}: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
