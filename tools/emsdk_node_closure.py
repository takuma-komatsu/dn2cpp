#!/usr/bin/env python3
"""The npm packages an Emscripten SDK needs at RUN time.

emcc loads a handful of node packages while linking (acorn, and whatever a
declined option would reach for); the other ~190 under node_modules are
devDependencies of emscripten's own repository and never open. Printing the
runtime closure rather than hand-listing it is what makes an upstream
dependency change visible: dist/package-toolchain.sh keeps exactly this set,
so a package added upstream is either shipped or named in dist/emsdk-trim.txt.

Usage: emsdk_node_closure.py [--drop NAME]... EMSCRIPTEN_DIR

--drop declines a runtime dependency; anything reachable only through it drops
with it. Printing sorted package directory names, one per line, relative to
node_modules/.
"""

import json
import os
import sys


def read_manifest(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def resolve(root, name):
    """The directory node would load NAME from, or None. Flat layout only —
    npm hoists, and a nested copy is a duplicate this bundle would ship twice."""
    path = os.path.join(root, "node_modules", name)
    return path if os.path.isfile(os.path.join(path, "package.json")) else None


def closure(emscripten_dir, dropped):
    manifest = read_manifest(os.path.join(emscripten_dir, "package.json"))
    # devDependencies are the repository's own lint/test/bundle chain. An
    # optionalDependency counts only when it is installed: the closure-compiler
    # families are one platform's binary each.
    queue = [n for n in manifest.get("dependencies", {}) if n not in dropped]
    seen = set()
    missing = []
    while queue:
        name = queue.pop()
        if name in seen or name in dropped:
            continue
        seen.add(name)
        pkg = resolve(emscripten_dir, name)
        if pkg is None:
            missing.append(name)
            continue
        sub = read_manifest(os.path.join(pkg, "package.json"))
        for key in ("dependencies", "optionalDependencies", "peerDependencies"):
            for dep in sub.get(key, {}):
                if dep not in seen and resolve(emscripten_dir, dep) is not None:
                    queue.append(dep)
    return sorted(seen), missing


def main(argv):
    dropped = set()
    args = []
    i = 0
    while i < len(argv):
        if argv[i] == "--drop":
            dropped.add(argv[i + 1])
            i += 2
        else:
            args.append(argv[i])
            i += 1
    if len(args) != 1:
        sys.stderr.write(__doc__)
        return 2
    names, missing = closure(args[0], dropped)
    if missing:
        sys.stderr.write(
            "error: emscripten/package.json depends on packages node_modules does not "
            "carry: %s\n" % " ".join(sorted(missing)))
        return 1
    for name in names:
        print(name)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
