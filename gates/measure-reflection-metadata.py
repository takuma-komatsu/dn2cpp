#!/usr/bin/env python3
"""Measure a linked Mach-O image or compare reflection allocation CSV captures."""

import argparse
import collections
import csv
import hashlib
import json
import pathlib
import re
import struct
import subprocess
import sys


def allocation_rows(path):
    result = {}
    context = {}
    with open(path, newline="", encoding="utf-8") as stream:
        for row in csv.reader(stream):
            if row and row[0] == "reflection-measure":
                if len(row) != 6 or row[1] in result:
                    raise ValueError("invalid or duplicate operation in " + str(path))
                result[row[1]] = dict(zip(
                    ("first_bytes", "repeated_bytes", "first_ticks", "repeated_ticks"),
                    map(int, row[2:])))
            elif row and row[0] in ("reflection-measure-frequency", "reflection-measure-iterations",
                                   "reflection-measure-managed-held-before", "reflection-measure-managed-held-after"):
                context[row[0].removeprefix("reflection-measure-")] = int(row[1])
    if not result:
        raise ValueError("no reflection measurements in " + str(path))
    if context.get("frequency", 0) <= 0 or context.get("iterations", 0) <= 0:
        raise ValueError("missing measurement clock or iteration count")
    return result, context


def compare_allocations(before, after):
    (old, old_context), (new, new_context) = allocation_rows(before), allocation_rows(after)
    if old.keys() != new.keys():
        raise ValueError("operation sets differ")
    if old_context["iterations"] != new_context["iterations"]:
        raise ValueError("iteration counts differ")
    increases = []
    timing = {}
    for operation in old:
        for phase in ("first_bytes", "repeated_bytes"):
            if new[operation][phase] > old[operation][phase]:
                increases.append({"operation": operation, "phase": phase,
                                  "before": old[operation][phase],
                                  "after": new[operation][phase]})
        timing[operation] = {}
        for phase in ("first_ticks", "repeated_ticks"):
            before_seconds = old[operation][phase] / old_context["frequency"]
            after_seconds = new[operation][phase] / new_context["frequency"]
            timing[operation][phase.removesuffix("_ticks")] = {
                "before_seconds": before_seconds, "after_seconds": after_seconds,
                "ratio": after_seconds / before_seconds if before_seconds else None}
    return {"before": old, "after": new, "before_context": old_context,
            "after_context": new_context, "timing": timing, "allocation_increases": increases}


def check_allocation_limits(limits_path, capture_path):
    captured, context = allocation_rows(capture_path)
    limits = {}
    with open(limits_path, newline="", encoding="utf-8") as stream:
        for row in csv.DictReader(stream):
            limits[row["operation"]] = {"first_bytes": int(row["first_bytes"]),
                                        "repeated_bytes": int(row["repeated_bytes"])}
    if limits.keys() != captured.keys() or context["iterations"] != 1000:
        raise ValueError("allocation budget operation set or iteration count differs")
    increases = []
    for operation, budget in limits.items():
        for phase, maximum in budget.items():
            if captured[operation][phase] > maximum:
                increases.append({"operation": operation, "phase": phase,
                                  "maximum": maximum, "actual": captured[operation][phase]})
    return {"allocation_increases": increases, "operations_checked": len(limits)}


def category(name):
    name = re.sub(r"^_+ZL?\d+", "", name).lstrip("_")
    if name.startswith("ti_") or (name.startswith("dn2cpp_") and name.endswith("_type")):
        return "type_info"
    if name.startswith("ty_") or name.endswith("_type_obj"):
        return "type_wrappers"
    if name.startswith("dn2cpp_metadata_blocks"):
        return "block_tables"
    builtin_fields = name.startswith(("dn2cpp_ownflds_", "dn2cpp_primflds_"))
    if name.endswith("_storage") and (builtin_fields or "_reflection_storage" in name):
        return "runtime_local_metadata_blocks_pointers_and_records"
    if builtin_fields:
        return "reflection_records"
    if name.startswith("md_display_tokens"):
        return "display_dictionary_pointers"
    if name.startswith(("genargpool_", "mgargs_", "custommods_")):
        return "shared_auxiliary_pointer_arrays"
    for prefix, label in (("md_record_", "packed_records"), ("md_ptr_", "pointer_pools"),
                          ("md_name_", "name_pools"), ("md_display_", "display_pools"),
                          ("md_block_", "block_tables")):
        if name.startswith(prefix):
            return label
    if name.startswith("dn2cpp_metadata_") and not name.startswith("dn2cpp_metadata_blocks"):
        return "metadata_decoder_code"
    if name.startswith(("fldtab_", "proptab_", "methtab_", "ctortab_", "enummembers_",
                        "paramtab_", "parmpool_", "params_", "attrtab_", "catab_")):
        return "reflection_records"
    if "metadata" in name or name.startswith(("refl_", "md_")):
        return "metadata_other"
    return "other"


def image_sizes(path):
    data = path.read_bytes()
    if len(data) < 32 or struct.unpack_from("<I", data)[0] != 0xFEEDFACF:
        raise ValueError("expected a thin little-endian 64-bit Mach-O image: " + str(path))
    commands = struct.unpack_from("<I", data, 16)[0]
    offset = 32
    sections = []
    segments = {}
    fixups = {}
    symbols = None
    for _ in range(commands):
        command, length = struct.unpack_from("<II", data, offset)
        if length < 8 or offset + length > len(data):
            raise ValueError("invalid load command")
        if command == 0x19:
            name = data[offset + 8:offset + 24].split(b"\0")[0].decode()
            address, virtual_size, file_offset, file_size = struct.unpack_from("<QQQQ", data, offset + 24)
            segments[name] = {"file_bytes": file_size, "virtual_bytes": virtual_size}
            count = struct.unpack_from("<I", data, offset + 64)[0]
            for index in range(count):
                base = offset + 72 + index * 80
                section = data[base:base + 16].split(b"\0")[0].decode()
                section_address, size = struct.unpack_from("<QQ", data, base + 32)
                sections.append({"name": name + "," + section,
                                 "address": section_address, "bytes": size})
        elif command == 2:
            symbols = struct.unpack_from("<IIII", data, offset + 8)
        elif command == 0x80000034:
            fixups["chained_fixups_bytes"] = struct.unpack_from("<I", data, offset + 12)[0]
        elif command in (0x22, 0x80000022):
            fields = struct.unpack_from("<10I", data, offset + 8)
            for index, name in enumerate(("rebase", "bind", "weak_bind", "lazy_bind", "export")):
                fixups[name + "_bytes"] = fields[index * 2 + 1]
        offset += length

    # Mach-O nlist has no symbol sizes. Attribute the interval up to the next
    # symbol in its section, including padding; aliases share the same interval.
    by_section = collections.defaultdict(lambda: collections.defaultdict(set))
    if symbols:
        symbol_offset, count, string_offset, string_size = symbols
        for index in range(count):
            string_index, kind, section, description, address = struct.unpack_from(
                "<IBBHQ", data, symbol_offset + index * 16)
            if kind & 0xE0 or (kind & 0x0E) != 0x0E or not 0 < section <= len(sections):
                continue
            if string_index >= string_size:
                raise ValueError("symbol string outside table")
            start = string_offset + string_index
            end = data.index(b"\0", start, string_offset + string_size)
            name = data[start:end].decode("utf-8", errors="replace")
            by_section[section][address].add(category(name))
    spans = collections.Counter()
    for section, entries in by_section.items():
        addresses = sorted(entries)
        limit = sections[section - 1]["address"] + sections[section - 1]["bytes"]
        for index, address in enumerate(addresses):
            end = addresses[index + 1] if index + 1 < len(addresses) else limit
            if end < address or end > limit:
                raise ValueError("symbol outside section")
            categories = entries[address]
            label = next(iter(categories)) if len(categories) == 1 else "aliased_categories"
            spans[label] += end - address
    return {"path": str(path), "sha256": hashlib.sha256(data).hexdigest(),
            "file_bytes": len(data), "segments": segments, "fixup_payloads": fixups,
            "sections": {item["name"]: item["bytes"] for item in sections},
            "metadata_symbol_attribution_available": "type_info" in spans,
            "symbol_spans_including_padding": dict(spans)}


def run_measurement(path):
    import os
    import time
    if sys.platform != "darwin":
        raise ValueError("--run requires macOS /usr/bin/time -l")
    runs = {}
    for mode in ("normal", "reflection"):
        environment = dict(os.environ)
        environment.pop("DN2CPP_REFLECTION_MEASURE", None)
        if mode == "reflection":
            environment["DN2CPP_REFLECTION_MEASURE"] = "1"
        started = time.perf_counter()
        run = subprocess.run(["/usr/bin/time", "-l", str(path.resolve())],
                             env=environment, capture_output=True, text=True, check=True)
        match = re.search(r"(\d+)\s+maximum resident set size", run.stderr)
        if match is None:
            raise ValueError("time did not report peak RSS")
        runs[mode] = {"wall_seconds_including_launch": time.perf_counter() - started,
                      "peak_rss_bytes": int(match.group(1)), "stdout": run.stdout}
    return runs


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("images", nargs="*", type=pathlib.Path)
    parser.add_argument("--compare-allocations", nargs=2, metavar=("BEFORE_CSV", "AFTER_CSV"))
    parser.add_argument("--run", type=pathlib.Path, help="capture normal/reflection timing and peak RSS")
    parser.add_argument("--check-allocation-limits", nargs=2, metavar=("LIMITS_CSV", "CAPTURE_CSV"))
    args = parser.parse_args()
    if not args.images and not args.compare_allocations and not args.run and not args.check_allocation_limits:
        parser.error("supply images or --compare-allocations")
    result = {"images": [image_sizes(path) for path in args.images]}
    if args.run:
        result["runs"] = run_measurement(args.run)
    failed = False
    if args.check_allocation_limits:
        comparison = check_allocation_limits(*args.check_allocation_limits)
        result["allocation_limits"] = comparison
        failed = bool(comparison["allocation_increases"])
    if args.compare_allocations:
        comparison = compare_allocations(*args.compare_allocations)
        result["reflection"] = comparison
        failed = failed or bool(comparison["allocation_increases"])
    print(json.dumps(result, indent=2))
    return int(failed)


if __name__ == "__main__":
    sys.exit(main())
