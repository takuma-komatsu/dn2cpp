#!/usr/bin/env python3
"""Collect frozen fallback profiles with one fresh CLR process per host locale."""

import argparse
import json
import os
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
TOOL = Path(__file__).resolve().parent


def inventory():
    hosts = {line for line in (TOOL / 'culture-candidates.txt').read_text().splitlines()
             if line and not line.startswith('#')}
    identifiers = (ROOT / 'runtime/core/intrinsics/dn2cpp_culture_identifiers.inc').read_text()
    extra = identifiers.split('kCultureIdentifierFormats', 1)[1].split('};', 1)[0]
    hosts.update(re.findall(r'^\s*\{\s*u"([^\"]+)"', extra, re.MULTILINE))
    # This variant is an undefined extlang in the host ICU locale too.
    hosts.discard('de-FOO')
    regions = {'', 'ZZ'}
    for host in hosts:
        regions.update(part.upper() for part in host.split('-')[1:]
                       if len(part) == 2 or (len(part) == 3 and part.isdigit()))
    hosts.update(['', 'en-US-POSIX'])
    return sorted(hosts), sorted(regions)


def cases(complete=False):
    _, regions = inventory()
    kinds = [line for line in (TOOL / 'numbering-systems.txt').read_text().splitlines()
             if line and not line.startswith('#')] + ['native', 'finance', 'traditio', 'default', 'foo', 'arab-foo', 'none']
    table = (ROOT / 'runtime/core/intrinsics/dn2cpp_culture_numbering.inc').read_text()
    types = table.split('kCultureNumberingTypes[]', 1)[1].split('};', 1)[0]
    families = {}
    for kind, family in re.findall(r'\{ u"([^\"]+)", (\d+) \}', types):
        families.setdefault(family, []).append(kind)
    representatives = ['latn' if 'latn' in members else members[0] for members in families.values()]
    names = []
    for region in regions:
        base = 'foo' + ('-' + region if region else '')
        names.append(base)
        selected = kinds if complete or region in ['', 'US', 'JP', 'CH'] else representatives
        names.extend(base + '-u-nu-' + kind for kind in selected)
    return names


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('dll', help='collect-fallback-data.dll built from the sibling C# file')
    parser.add_argument('output')
    parser.add_argument('--platform', choices=['apple', 'standard'], required=True)
    parser.add_argument('--icu-version', default='78.3')
    parser.add_argument('--icu-library-path', default='/opt/homebrew/opt/icu4c/lib')
    parser.add_argument('--complete', action='store_true', help='expand every numbering type in every region after a family split')
    args = parser.parse_args()
    hosts, _ = inventory()
    output = Path(args.output).resolve()
    output.mkdir(parents=True, exist_ok=True)
    inputs = output / 'cases.json'
    inputs.write_text(json.dumps({'names': cases(args.complete)}, indent=2) + '\n')
    for i, host in enumerate(hosts):
        env = dict(os.environ)
        locale = 'C' if host == 'en-US-POSIX' else host.replace('-', '_') + ('.UTF-8' if host else '')
        env['LC_ALL'] = env['LANG'] = locale
        env.pop('DOTNET_SYSTEM_GLOBALIZATION_INVARIANT', None)
        if args.platform == 'standard':
            env['DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU'] = args.icu_version
            env['DYLD_LIBRARY_PATH'] = args.icu_library_path
        else:
            env.pop('DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU', None)
            env.pop('DYLD_LIBRARY_PATH', None)
        target = output / ((host or 'root') + '.jsonl')
        subprocess.run(['dotnet', str(Path(args.dll).resolve()), str(target), str(inputs)], env=env, check=True)
        if i % 10 == 0 or i == len(hosts) - 1:
            print(args.platform, i + 1, '/', len(hosts), host or 'root', flush=True)


if __name__ == '__main__':
    main()
