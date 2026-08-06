# Licence texts a shipped bundle carries

The toolchain bundle ships third-party binaries. Most of them carry their own
licence text inside the archive they were unpacked from, and that text is staged
with them. This directory holds the texts of the ones that do **not** — a file
here exists because there is nowhere else the obligation could be discharged
from.

Same convention as `third_party/*/DN2CPP-VENDORED.md`: the upstream URL, the
revision and the sha256 are written down here, because a licence file with no
provenance cannot be checked against upstream on a refresh.

A file is named `<project>-<the upstream file's own name>`, lowercased to `.txt`
so the directory reads as one kind of thing. `gates/build-and-run-doc-claims.sh`
checks every `.txt` here against the sha256 recorded for it below, which is what
makes a recorded hash worth reading.

## `ninja-COPYING.txt` — ninja

- **Apache License 2.0**, sha256
  `eb7e9ab9690124c5c9f42bdc81383d886a3dede26345b6ed15bbad7caf81f7ea`.
- Fetched from
  `https://raw.githubusercontent.com/ninja-build/ninja/v1.13.2/COPYING` — the
  `COPYING` at the **tag `gates/expected/buildtools-pin.txt` pins**, so the text
  and the binary come from one commit.
- **Verbatim.** No modifications.

Ninja's release zip (`ninja-mac.zip` and its siblings) holds the executable and
nothing else — no licence, no notice. Apache-2.0 §4(a) obliges a redistributor to
give recipients a copy of the licence, so shipping the binary without this file
would be a breach; vendoring it here is the whole remedy.

**cmake needs nothing vendored.** Its archives carry `doc/cmake/LICENSE.rst`
(cmake's own BSD-3-Clause) and one `COPYING`/`LICENSE`/`NOTICE` per bundled
library under `doc/cmake/cm*/`, which a bundle's keep list has to name — a trim
that drops them puts cmake in the same position ninja is in here.

## `llvm-LICENSE.txt` — LLVM (clang, lld, the `llvm-*` tools, `lib/clang`)

- **Apache License 2.0 with LLVM Exceptions**, plus the section naming the third
  party and legacy-licensed code the project still carries. sha256
  `8d85c1057d742e597985c7d4e6320b015a9139385cff4cbae06ffc0ebe89afee`.
- Fetched from
  `https://raw.githubusercontent.com/llvm/llvm-project/abd3b3a1445b5a8eeffae5c3912883faa9287fb7/LICENSE.TXT`.
  The repository's `llvm/LICENSE.TXT` is byte-identical at that commit.
- **Verbatim.** No modifications.

## `binaryen-LICENSE.txt` — Binaryen (the `wasm-*` tools)

- **Apache License 2.0**, with a closing note that the vendored FP16 is MIT.
  **No LLVM exception** — Binaryen's text is the plain licence, unlike LLVM's.
  sha256 `64c0d02491e16eced74826440ecd2bcf7722d37fa586faf457b2494293afbffe`.
- Fetched from
  `https://raw.githubusercontent.com/WebAssembly/binaryen/55dff6b2430aee4fee7e5cf748af07222b079742/LICENSE`.
- **Verbatim.** No modifications.

### Why both are vendored, and why a commit rather than a tag

The archive `gates/expected/emsdk-pin.txt` fetches holds no licence file for
either: every text in it sits under `emscripten/`, and `bin/`, `lib/clang` and
`node/` carry none. So no keep list can reach one, and the trim cannot be blamed
— the two here are the bundle's only copy of those terms.

Neither component is built at a release tag. The Emscripten SDK build pins a
development revision of each, so the provenance is a commit, and the only
trustworthy way to recover it is to **run the staged binary** — the version
string carries the revision:

```sh
emsdk/bin/clang --version     # clang version 24.0.0git (… <llvm commit>)
emsdk/bin/wasm-opt --version  # wasm-opt version <n> (version_<n>-<k>-g<binaryen commit>)
```

`wasm-opt` prints an abbreviated commit; resolve it against
`WebAssembly/binaryen` before fetching. Both revisions above were read this way
off the SDK the pin's `release_hash`
`dbd755b5da399329c2576f6e3dfa7f419f5d8409` unpacks to.

## Updating

A file here is pinned to the revision beside it, so it moves when the pin that
governs it moves — `gates/expected/buildtools-pin.txt` for ninja, and
`gates/expected/emsdk-pin.txt` for LLVM and Binaryen. Re-fetch the same path at
the new revision, record the new sha256 above, and diff the two texts — a licence
that *changed* is a fact about the project worth reading before the version bump
lands.

For LLVM and Binaryen the revision is not in the pin file and cannot be read off
it: re-measure it from the newly unpacked SDK with the two commands above.
`gates/build-and-run-doc-claims.sh` fails while this file still names the old
`release_hash`, which is what makes the re-measure happen.
