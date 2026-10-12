#!/usr/bin/env python3
"""Deduplicate frozen undefined-language numeric fallback snapshots."""

import argparse
import importlib.util
import json
from pathlib import Path

here = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("numbering", here / "gen-numbering-table.py")
numbering = importlib.util.module_from_spec(spec)
spec.loader.exec_module(numbering)
matrix_spec = importlib.util.spec_from_file_location("fallback_matrix", here / "collect-fallback-matrix.py")
matrix = importlib.util.module_from_spec(matrix_spec)
matrix_spec.loader.exec_module(matrix)
FIELDS = numbering.FIELDS
signature = numbering.signature
literal = numbering.literal


def load(folder):
    hosts, languages, provenance = {}, None, None
    for path in sorted(Path(folder).glob("*.jsonl")):
        host = "" if path.stem == "root" else path.stem
        rows = {}
        for line in path.read_text().splitlines():
            row = json.loads(line)
            if "oracle" in row:
                if provenance is not None and provenance != row:
                    raise ValueError("mixed oracle versions: " + str(path))
                provenance = row
            elif "resourceLanguages" in row:
                current = row["resourceLanguages"]
                if languages is not None and languages != current:
                    raise ValueError("resource inventory depends on host: " + str(path))
                languages = current
            else:
                rows[row["input"]] = row
        hosts[host] = rows
    if not hosts or languages is None:
        raise ValueError("empty platform snapshot: " + str(folder))
    return hosts, languages, provenance


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("apple")
    parser.add_argument("standard")
    parser.add_argument("output")
    args = parser.parse_args()
    sources = [load(args.apple), load(args.standard)]
    hosts = sorted(sources[0][0])
    if set(hosts) != set(sources[1][0]):
        raise ValueError("platform host inventories differ")
    expected_hosts, expected_regions = matrix.inventory()
    if hosts != expected_hosts:
        raise ValueError("host snapshots do not match the frozen candidate inventory")
    kinds = [line for line in (here / "numbering-systems.txt").read_text().splitlines()
             if line and not line.startswith("#")] + numbering.ALIASES + ["foo", "arab-foo", "none"]
    controls = ["", "US", "JP", "CH"]
    families, keys = [], []
    for kind in kinds:
        # Identity differs by numbering type; only numeric data define a family.
        key = signature([[{field: row[field] for field in FIELDS + ["CurrencySymbol"]}
                          for host in hosts for region in controls
                          for row in [source[0][host]["foo" + ("-" + region if region else "") + "-u-nu-" + kind]]]
                         for source in sources])
        if key not in keys:
            keys.append(key)
            families.append([])
        families[keys.index(key)].append(kind)
    if len(families) >= 256:
        raise ValueError("numbering family indices exceed uint8_t")
    regions = sorted({name[4:] if name.startswith("foo-") else ""
                      for name in sources[0][0][hosts[0]] if "-u-nu-" not in name})
    if regions != expected_regions:
        raise ValueError("region snapshots do not match the frozen candidate inventory")
    for source, _, _ in sources:
        for host, rows in source.items():
            actual_regions = sorted({name[4:] if name.startswith("foo-") else ""
                                     for name in rows if "-u-nu-" not in name})
            if actual_regions != expected_regions:
                raise ValueError("missing region snapshot: " + host)
    profiles, currencies, vectors, entries = set(), set(), set(), []
    for source, _, _ in sources:
        platform = []
        for host in hosts:
            regional = []
            for region in regions:
                base = "foo" + ("-" + region if region else "")
                row = source[host][base]
                currency = row["CurrencySymbol"]
                currencies.add(currency)
                vector = []
                for family in [None] + families:
                    if family is None:
                        item = row
                    else:
                        observed = [source[host].get(base + "-u-nu-" + kind) for kind in family]
                        observed = [item for item in observed if item is not None]
                        if not observed:
                            raise ValueError("numbering family split requires more samples: " + host + "/" + base + "/" + str(family))
                        item = observed[0]
                        if any(any(other[field] != item[field] for field in FIELDS + ["CurrencySymbol"])
                               for other in observed[1:]):
                            raise ValueError("numbering family depends on region: " + host + "/" + base)
                    if item["CurrencySymbol"] and item["CurrencySymbol"] != currency:
                        raise ValueError("numbering changes nonempty currency: " + host + "/" + item["input"])
                    profile = {field: item[field] for field in FIELDS}
                    profile["EmptyCurrency"] = not item["CurrencySymbol"]
                    for field in numbering.GROUPS:
                        if not 1 <= len(profile[field]) <= 4:
                            raise ValueError("unrepresentable group array: " + item["input"])
                    key = signature(profile)
                    profiles.add(key)
                    vector.append(key)
                vector = tuple(vector)
                vectors.add(vector)
                regional.append((vector, currency))
            platform.append(regional)
        entries.append(platform)
    profiles, currencies, vectors = sorted(profiles), sorted(currencies), sorted(vectors)
    if any(len(values) > 65535 for values in [profiles, currencies, vectors]):
        raise ValueError("fallback indices exceed uint16_t")
    profile_index = {value: i for i, value in enumerate(profiles)}
    vector_index = {value: i for i, value in enumerate(vectors)}
    currency_index = {value: i for i, value in enumerate(currencies)}
    out = ["// GENERATED FILE — do not edit by hand.",
           "// Undefined-language numeric resources are a frozen CLR/ICU snapshot.",
           "// Regenerate with collect-fallback-data.cs, collect-fallback-matrix.py",
           "// and gen-fallback-table.py in tools/gen-culture-table/.",
           "// Host and region inventories follow the modeled culture candidates.",
           "// NativeDigits and DigitSubstitution are outside this numeric model."]
    for label, (_, _, provenance) in zip(["Apple", "Standard"], sources):
        out.append("// " + label + " oracle: " + provenance["oracle"] + "; ICU " + provenance["icu"] + "; " + provenance["host"])
    out += ["static const Dn2CppCultureNumericData kCultureFallbackProfiles[] = {"]
    for key in profiles:
        profile = json.loads(key)
        values = [literal(profile[field]) for field in numbering.STRINGS]
        for field in numbering.GROUPS:
            sizes = profile[field]
            values.append("{ { " + ", ".join(str(n) for n in sizes) + " }, " + str(len(sizes)) + " }")
        values += [str(profile[field]) for field in numbering.NUMBERS]
        values.append("true" if profile["EmptyCurrency"] else "false")
        out.append("    { " + ", ".join(values) + " },")
    out += ["};", "static const uint16_t kCultureFallbackVectors[][" + str(len(families) + 1) + "] = {"]
    out += ["    { " + ", ".join(str(profile_index[key]) for key in vector) + " }," for vector in vectors]
    out += ["};", "static const Dn2CppCultureNumberingType kCultureFallbackTypes[] = {"]
    for kind in sorted(kinds):
        family = next(i for i, values in enumerate(families) if kind in values)
        out.append("    { " + literal(kind) + ", " + str(family + 1) + " },")
    unknown = next(i + 1 for i, values in enumerate(families) if "foo" in values)
    out += ["};", "static constexpr uint8_t kFallbackUnknownFamily = " + str(unknown) + ";",
            "static const char16_t* const kCultureFallbackHosts[] = {"]
    out += ["    " + literal(host) + "," for host in hosts]
    out += ["};", "static const char16_t* const kCultureFallbackRegions[] = {"]
    out += ["    " + literal(region) + "," for region in regions]
    out += ["};", "static const char16_t* const kCultureFallbackCurrencies[] = {"]
    out += ["    " + literal(currency) + "," for currency in currencies]
    out += ["};", "struct Dn2CppCultureFallbackEntry { uint16_t vector; uint16_t currency; };",
            "static const Dn2CppCultureFallbackEntry kCultureFallbackEntries[][" + str(len(regions)) + "] = {"]
    for platform, data in enumerate(entries):
        out.append("#if defined(__APPLE__)" if platform == 0 else "#else")
        for host, regional in zip(hosts, data):
            out.append("    // " + (host or "root"))
            out.append("    { " + ", ".join("{ " + str(vector_index[vector]) + ", " + str(currency_index[currency]) + " }"
                                            for vector, currency in regional) + " },")
    out += ["#endif", "};", "static const char16_t* const kCultureResourceLanguages[] = {"]
    for platform, (_, languages, _) in enumerate(sources):
        out.append("#if defined(__APPLE__)" if platform == 0 else "#else")
        out += ["    " + literal(language) + "," for language in languages]
    out += ["#endif", "};"]
    Path(args.output).write_text("\n".join(out) + "\n")
    print("profiles", len(profiles), "vectors", len(vectors), "currencies", len(currencies), "families", len(families),
          "bytes", Path(args.output).stat().st_size)


if __name__ == "__main__":
    main()
