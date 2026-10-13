#!/usr/bin/env python3
"""Deduplicate CLR numbering snapshots; inputs come from collect-numbering-data.cs."""

import argparse
import json
from pathlib import Path


STRINGS = [
    "NumberDecimalSeparator", "NumberGroupSeparator",
    "CurrencyDecimalSeparator", "CurrencyGroupSeparator",
    "PercentDecimalSeparator", "PercentGroupSeparator",
    "NegativeSign", "PositiveSign", "NaNSymbol", "PositiveInfinitySymbol",
    "NegativeInfinitySymbol", "PercentSymbol", "PerMilleSymbol",
]
GROUPS = ["NumberGroupSizes", "CurrencyGroupSizes", "PercentGroupSizes"]
NUMBERS = [
    "NumberDecimalDigits", "CurrencyDecimalDigits", "PercentDecimalDigits",
    "NumberNegativePattern", "CurrencyPositivePattern", "CurrencyNegativePattern",
    "PercentPositivePattern", "PercentNegativePattern",
]
FIELDS = STRINGS + GROUPS + NUMBERS
ALIASES = ["native", "finance", "traditio", "default"]
UNKNOWN = "foo"


def signature(value):
    return json.dumps(value, ensure_ascii=True, sort_keys=True, separators=(",", ":"))


def literal(value):
    escaped = ""
    for char in value:
        code = ord(char)
        if char in '\\"':
            escaped += "\\" + char
        elif 32 <= code < 127:
            escaped += char
        elif code <= 0xffff:
            escaped += "\\u%04x" % code
        else:
            raise ValueError("numeric symbol outside the BMP: " + repr(value))
    return 'u"' + escaped + '"'


def load(path, allow_bare_rejection=False):
    data = {}
    for line in Path(path).read_text().splitlines():
        row = json.loads(line)
        if "oracle" in row:
            continue
        if "error" in row and (not allow_bare_rejection or "-u-nu-" in row["input"]):
            raise ValueError("oracle rejected " + row["input"])
        if "-u-nu-" not in row["input"]:
            continue
        base, kind = row["input"].split("-u-nu-", 1)
        profile = {field: row[field] for field in FIELDS}
        profile["EmptyCurrency"] = row["CurrencySymbol"] == ""
        for field in GROUPS:
            if not 1 <= len(profile[field]) <= 4:
                raise ValueError("unrepresentable group array: " + row["input"])
        data[base, kind] = profile
    return data


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--windows", action="store_true",
                        help="generate Windows data from one snapshot and an output path")
    parser.add_argument("paths", nargs="+", metavar="PATH")
    args = parser.parse_args()
    if len(args.paths) != (2 if args.windows else 3):
        parser.error("expected Windows snapshot/output or Apple/standard snapshots/output")
    paths = [args.paths[0], args.paths[0]] if args.windows else args.paths[:2]
    root = Path(__file__).resolve().parent
    kinds = [line for line in (root / "numbering-systems.txt").read_text().splitlines()
             if line and not line.startswith("#")] + ALIASES + [UNKNOWN]
    sources = [load(path, allow_bare_rejection=args.windows) for path in paths]
    currencies = []
    currency_sources = []
    for path in paths:
        values = {}
        symbols = {}
        for line in Path(path).read_text().splitlines():
            row = json.loads(line)
            if "input" in row and "-u-nu-" in row["input"]:
                base, kind = row["input"].split("-u-nu-", 1)
                symbols[base, kind] = row["CurrencySymbol"]
                if kind == "latn":
                    values[base] = row["CurrencySymbol"]
        currencies.append(values)
        currency_sources.append(symbols)
    bases = sorted({base for base, _ in sources[0]})
    # This variant becomes an undefined language after ICU extlang conversion.
    bases = [base for base in bases if base != "de-FOO"]
    if {base for base, _ in sources[1] if base != "de-FOO"} != set(bases):
        raise ValueError("platform snapshots have different locale sets")
    for platform, source in enumerate(sources):
        for base in bases:
            for kind in kinds:
                if (base, kind) not in source:
                    raise ValueError("missing numbering data: " + base + "-u-nu-" + kind)
            for (locale, kind), profile in source.items():
                if locale != base or profile["EmptyCurrency"]:
                    continue
                # The stored locale currency must represent numeric aliases too.
                if currency_sources[platform][base, kind] != currencies[platform][base]:
                    raise ValueError("numbering type changes currency: " + base + "-u-nu-" + kind)
    families = []
    family_signatures = []
    for kind in kinds:
        vector = signature([[source[base, kind] for base in bases] for source in sources])
        if vector not in family_signatures:
            family_signatures.append(vector)
            families.append([])
        families[family_signatures.index(vector)].append(kind)
    profiles = sorted({signature(source[base, family[0]])
                       for source in sources for base in bases for family in families})
    if len(profiles) > 256:
        raise ValueError("profile indices exceed uint8_t")
    vectors = sorted({tuple(profiles.index(signature(source[base, family[0]]))
                            for family in families)
                      for source in sources for base in bases})
    if len(vectors) > 256:
        raise ValueError("locale-vector indices exceed uint8_t")
    provenance = []
    for path in paths:
        first = json.loads(Path(path).read_text().splitlines()[0])
        provenance.append(first.get("oracle", "unrecorded oracle") + "; ICU "
                          + str(first.get("icu", "unknown")) + "; " + first.get("host", "unknown host"))
    out = [
        "// GENERATED FILE — do not edit by hand.",
        "// Numeric symbols and patterns are a frozen CLR/ICU snapshot.",
    ]
    if args.windows:
        out += ["// Windows oracle: " + provenance[0],
                "// Regenerate with collect-numbering-data.cs on Windows, then",
                "// gen-numbering-table.py --windows snapshot.jsonl output.inc."]
    else:
        out += ["// Apple oracle: " + provenance[0],
                "// Standard oracle: " + provenance[1],
        "// Regenerate with tools/gen-culture-table/collect-numbering-data.cs on",
        "// Apple ICU and standard ICU, then gen-numbering-table.py with both JSONL files."]
    out += [
        "// Currency symbols are stored per locale; empty numeric profiles override them.",
        "// NativeDigits and DigitSubstitution do not participate in this numeric model.",
        "static const Dn2CppCultureNumericData kCultureNumericProfiles[] = {",
    ]
    for item in profiles:
        profile = json.loads(item)
        values = [literal(profile[field]) for field in STRINGS]
        for field in GROUPS:
            sizes = profile[field]
            values.append("{ { " + ", ".join(str(n) for n in sizes)
                          + " }, " + str(len(sizes)) + " }")
        values += [str(profile[field]) for field in NUMBERS]
        values.append("true" if profile["EmptyCurrency"] else "false")
        out.append("    { " + ", ".join(values) + " },")
    out += ["};", "static const uint8_t kCultureNumericVectors[][" + str(len(families)) + "] = {"]
    out += ["    { " + ", ".join(str(i) for i in vector) + " }," for vector in vectors]
    out += ["};", "struct Dn2CppCultureNumberingType { const char16_t* name; uint8_t family; };",
            "static const Dn2CppCultureNumberingType kCultureNumberingTypes[] = {"]
    for kind in sorted(kinds):
        family = next(i for i, members in enumerate(families) if kind in members)
        out.append("    { " + literal(kind) + ", " + str(family) + " },")
    unknown_family = next(i for i, members in enumerate(families) if UNKNOWN in members)
    algorithmic_family = next(i for i, members in enumerate(families) if "roman" in members)
    # Runtime fast paths use these profiles without consulting a locale vector.
    for family in [unknown_family, algorithmic_family]:
        values = {signature(source[base, kind]) for source in sources
                  for base in bases for kind in families[family]}
        if len(values) != 1:
            raise ValueError("global numbering family needs locale-specific profiles")
    for source in sources:
        for base in bases:
            for kind in ["arab-foo", "none"]:
                if source[base, kind] != source[base, UNKNOWN]:
                    raise ValueError("unknown numbering values disagree: " + base + "-u-nu-" + kind)
    algorithmic_profile = profiles.index(signature(sources[0][bases[0], "roman"]))
    unknown_profile = profiles.index(signature(sources[0][bases[0], UNKNOWN]))
    out += ["};", f"static const uint8_t kNumberingUnknownFamily = {unknown_family};",
            f"static const uint8_t kNumberingAlgorithmicFamily = {algorithmic_family};",
            f"static const uint8_t kNumberingUnknownProfile = {unknown_profile};",
            f"static const uint8_t kNumberingAlgorithmicProfile = {algorithmic_profile};",
            "struct Dn2CppCultureNumberingLocale { const char16_t* name; uint8_t vector; const char16_t* currency; };",
            "static const Dn2CppCultureNumberingLocale kCultureNumberingLocales[] = {"]
    for base in bases:
        ids = [vectors.index(tuple(profiles.index(signature(source[base, family[0]]))
                                   for family in families)) for source in sources]
        row = lambda platform: "    { " + literal(base) + ", " + str(ids[platform]) + ", " + literal(currencies[platform][base]) + " },"
        if row(0) == row(1):
            out.append(row(0))
        else:
            out += ["#if defined(__APPLE__)", row(0), "#else", row(1), "#endif"]
    out += ["};", ""]
    Path(args.paths[-1]).write_text("\n".join(out), encoding="utf-8")
    print(f"families={len(families)}, profiles={len(profiles)}, vectors={len(vectors)}")


if __name__ == "__main__":
    main()
