# Licence texts a shipped bundle carries

The toolchain bundle ships third-party binaries. Most of them carry their own
licence text inside the archive they were unpacked from, and that text is staged
with them. This directory holds the texts of the ones that do **not** — a file
here exists because there is nowhere else the obligation could be discharged
from.

Same convention as `third_party/*/DN2CPP-VENDORED.md`: the upstream URL, the tag
and the sha256 are written down here, because a licence file with no provenance
cannot be checked against upstream on a refresh.

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

## Updating

A file here is pinned to the tag beside it, so it moves when
`gates/expected/buildtools-pin.txt` moves: re-fetch the same path at the new tag,
record the new sha256 above, and diff the two texts — a licence that *changed* is
a fact about the project worth reading before the version bump lands.
