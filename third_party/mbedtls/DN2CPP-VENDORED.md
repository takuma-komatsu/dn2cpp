# Vendored Mbed TLS

This directory is a **vendored subset** of [Mbed TLS](https://github.com/Mbed-TLS/mbedtls),
the TLS backend for the vendored libcurl (`third_party/curl/`, see
`third_party/curl/DN2CPP-VENDORED.md`) — i.e. what backs `CURL_ENABLE_SSL ON` +
`CURL_USE_MBEDTLS ON` and so makes the transport behind `System.Net.Http`
`https://`-capable. Both it and curl are built by default
(`DN2CPP_USE_CURL` is ON), and neither is linked into a program that issues no
HTTP request — the transport's lazy init leaves the whole subtree unextracted from
the static archive.

A **vendored** TLS backend rather than a platform one (Schannel on Windows,
Secure Transport on Apple, a system OpenSSL on Linux) because dn2cpp ships one
transport across console, GDExtension, Android, iOS and wasm: a vendored
backend is the only choice that is the *same* backend everywhere, needs no
`find_package` against a host that may not have one, and compiles under
Emscripten alongside everything else in `third_party/`. This directory is
**source only** — see "How it is built".

## Version

- **Mbed TLS 3.6.7** (`include/mbedtls/build_info.h`:
  `MBEDTLS_VERSION_STRING "3.6.7"`, `MBEDTLS_VERSION_NUMBER 0x03060700`).
- Upstream: https://github.com/Mbed-TLS/mbedtls — official release **asset**
  `https://github.com/Mbed-TLS/mbedtls/releases/download/mbedtls-3.6.7/mbedtls-3.6.7.tar.bz2`
  (sha256 `a7e8bcbec0e6f761b4af24f25677626b35f762f68eef79c08677a363212d11f6`,
  measured on the downloaded artifact and matching the checksum on the release
  page). Released 2026-07-07; the current 3.6 LTS point release as of this
  vendoring.
- **The release asset, not GitHub's auto-generated source tarball.** The
  auto-generated one is a plain `git archive` of the tag: it has no `framework/`
  (a submodule) and, more importantly here, none of the **generated** sources
  that upstream keeps out of the repository and ships pre-generated in the
  release asset. Five of those are in the kept set below and the library does not
  build without them; a git checkout would need `scripts/` plus Python to
  regenerate them, which is exactly the configure step this vendor exists to
  avoid.

**3.6 LTS, not 4.x.** Two independent reasons:

- **4.x is a different file set, not a newer version of this one.** Mbed TLS 4.0
  moved the cryptography half out into the separate
  [TF-PSA-Crypto](https://github.com/Mbed-TLS/TF-PSA-Crypto) project, pulled back
  in as a submodule: the crypto sources no longer live under `library/`, and the
  PSA configuration header moves to `tf-psa-crypto/include/psa/crypto_config.h`.
  Vendoring 4.x means vendoring two trees and re-deriving the kept set, which is
  a separate piece of work from a version bump.
- **curl 8.21.0's floor is 3.2.0**, and its mbedTLS backend still carries
  `MBEDTLS_VERSION_NUMBER < 0x04000000` arms — `lib/vtls/mbedtls.c` uses the
  legacy `mbedtls_ctr_drbg_*`/`mbedtls_entropy_*` seeding on 3.x and a different
  path on 4.x. 3.6.x is the well-trodden combination.

Upstream's `BRANCHES.md` puts 3.6 LTS support at March 2027 (the 4.1 LTS, March
2026, runs to March 2029) — so this vendor has a supported upstream for the
lifetime of the current curl vendor, and the 4.x migration is a scheduled piece
of work rather than an urgent one.

## How it is built

Mbed TLS is vendored **hand-globbed**, the `third_party/zlib` /
`third_party/brotli` shape rather than the `third_party/curl` one: no upstream
build system is vendored (`CMakeLists.txt`, `Makefile`, `library/CMakeLists.txt`,
`library/Makefile`, `include/CMakeLists.txt` are all dropped) and no configure
step runs. The default configuration compiles as shipped — the whole point of
`include/mbedtls/mbedtls_config.h` being a plain header with the choices already
made in it.

The target that compiles this tree, and the wiring that points curl's mbedTLS
backend at it, belong in `runtime/CMakeLists.txt` beside the `dn2cpp_zlib` /
`dn2cpp_brotli` / `DN2CPP_USE_CURL` arms; this directory carries source only.
The shape that was confirmed to work is a single glob of `library/*.c` with
`include/` as the public include directory:

```sh
clang -c -std=c11 -O2 -fPIC -w -Ithird_party/mbedtls/include \
    third_party/mbedtls/library/<file>.c -o <file>.o
```

- **No `-D` defines are needed**, and none were used: all 109 `library/*.c`
  compile to objects with zero errors under Apple clang (macOS arm64) and,
  separately, under `emcc` (Emscripten, `wasm32`) — the wasm arm matters because
  `library/entropy_poll.c` `#error`s on a platform that is neither Unix-like nor
  Windows, and Emscripten defines `__unix__`, so it takes the `/dev/urandom`
  path rather than the `#error`.
- **`-w`** — third-party source, suppress its warnings (the `dn2cpp_gc` /
  `dn2cpp_zlib` / `dn2cpp_brotli` convention).
- `-fPIC` comes from the project's `CMAKE_POSITION_INDEPENDENT_CODE ON`; passed
  explicitly here only because this flag list documents a standalone manual
  compile.
- Upstream splits the same sources into three archives (`libmbedcrypto`,
  `libmbedx509`, `libmbedtls`) with an explicit per-archive file list. That split
  buys nothing here — everything links into one static runtime and the three are
  a strict dependency chain — so one glob over `library/*.c` is both simpler and
  immune to upstream moving a file between the lists.

### What the default configuration gives curl (verified, not assumed)

curl's `lib/vtls/mbedtls.c` reads the configuration it was compiled against, so
these defaults are part of this vendor's contract. All are as shipped in
`include/mbedtls/mbedtls_config.h`; **no config header was modified** (see
"Modifications to vendored source"):

| symbol | default | why it matters |
|--------|---------|----------------|
| `MBEDTLS_CTR_DRBG_C` | **on** | curl `#error`s without it on 3.x (`mbedtls.c`: `"MBEDTLS_CTR_DRBG_C is required for mbedTLS 3.x."`) |
| `MBEDTLS_SSL_PROTO_TLS1_3` | **on** | curl branches on it; with it off the backend silently caps at TLS 1.2 |
| `MBEDTLS_SSL_PROTO_TLS1_2` | on | the other half of the version range |
| `MBEDTLS_PSA_CRYPTO_C` | on | curl calls `psa_crypto_init`, `psa_generate_random`, `psa_hash_compute` on every connection |
| `MBEDTLS_PSA_CRYPTO_CONFIG` | **off** | so `include/psa/crypto_config.h` is inert — PSA mechanisms follow the legacy `MBEDTLS_*_C` switches. The header must still exist: curl's `mbedtls.c` includes `<psa/crypto_config.h>` unconditionally |
| `MBEDTLS_X509_CRT_PARSE_C`, `MBEDTLS_PEM_PARSE_C`, `MBEDTLS_FS_IO` | on | CA bundle loading, from memory and from a file |
| `MBEDTLS_SSL_ALPN`, `MBEDTLS_SSL_SERVER_NAME_INDICATION`, `MBEDTLS_SSL_SESSION_TICKETS` | on | ALPN (HTTP/1.1 negotiation), SNI, session resumption — curl configures all three |
| `MBEDTLS_ENTROPY_C` + platform entropy | on | `MBEDTLS_NO_PLATFORM_ENTROPY` is off, so `entropy_poll.c` uses the OS source |
| `MBEDTLS_THREADING_C` | **off** | Mbed TLS does **not** serialise access to a shared context. curl's own mbedTLS backend is written against this default; it matters if anything else in the runtime ever shares an `mbedtls_ssl_context` across threads |
| `MBEDTLS_ECDH_VARIANT_EVEREST_ENABLED`, `MBEDTLS_PSA_P256M_DRIVER_ENABLED` | **off** | the two options that would require the dropped `3rdparty/` tree (see below) |

## What was kept / dropped

Kept (272 files, ~7.3 MiB):

- **`library/*.c` (109 files)** — the whole library. Unlike zlib, no per-entry-point
  cut is available: the TLS, X.509 and crypto halves are a dependency chain and
  every `.c` is compiled by upstream's own build, so this is the minimal
  buildable unit. Unused parts (DTLS, the server side, the ciphers curl's
  ciphersuite list never selects) cost object-file size in the static archive;
  the lever for shrinking them is the configuration (turning `MBEDTLS_*` options
  off), not a smaller glob.
- **`library/*.h` (65 files)** — the internal headers those sources include by
  relative path (`"common.h"`, `"ssl_misc.h"`, `"psa_crypto_core.h"`, ...); not a
  public API, but not separable from the `.c` files either.
- **`include/mbedtls/*.h` (74 files)** — the public API plus the configuration
  headers: `mbedtls_config.h` (the compile-time feature set), `build_info.h` (the
  version macros, and the file that pulls the config in), and the
  `config_adjust_*.h`/`check_config.h` consistency layer that must accompany them.
- **`include/psa/*.h` (23 files)** — the PSA Crypto API surface, including
  `psa/crypto_config.h`. Required even though `MBEDTLS_PSA_CRYPTO_CONFIG` is off:
  the library's own sources include this tree, and curl's backend includes
  `<psa/crypto_config.h>` unconditionally.
- **`LICENSE`** — the license text (see "License").

Five of the kept `library/` files are **generated** and exist only in the release
asset, never in a git checkout — upstream names them in `library/.gitignore`,
commented out inside a `START_COMMENTED_GENERATED_FILES` block so the release
tarball can carry them:

- `error.c` — the `mbedtls_strerror` table, generated from every module's error codes,
- `version_features.c` — the `mbedtls_version_check_feature` table, generated from the config,
- `ssl_debug_helpers_generated.c` — the handshake-state name tables,
- `psa_crypto_driver_wrappers.h` and `psa_crypto_driver_wrappers_no_static.c` —
  the PSA driver dispatch layer, generated from the driver JSON descriptions.

They are kept because the glob does not link without them, and their being
release-only is the reason this vendor is sourced from the release asset.

Dropped:

- **`tests/`, `framework/`, `programs/`** — the test suites, the test framework
  (a git submodule, absent from a plain source tarball anyway) and the sample
  programs. Nothing in the kept set names them; the two `framework/` mentions in
  `library/threading.c` / `include/mbedtls/threading.h` are prose in comments.
- **`3rdparty/`** — the two optional alternative implementations: Everest
  (a formally verified Curve25519) and p256-m (a compact P-256). Both are gated
  on config options that are **off** in the default config
  (`MBEDTLS_ECDH_VARIANT_EVEREST_ENABLED`, `MBEDTLS_PSA_P256M_DRIVER_ENABLED`),
  and every reference to them from the kept tree — `include/mbedtls/ecdh.h`'s
  `#include "everest/everest.h"`, `library/psa_crypto_driver_wrappers*`'s
  `#include "../3rdparty/p256-m/p256-m_driver_entrypoints.h"` — sits behind those
  guards. Turning either option on means vendoring `3rdparty/` too.
- **`configs/`** — upstream's alternative pre-made configuration headers
  (`config-suite-b.h`, `config-symmetric-only.h`, ...), selected by defining
  `MBEDTLS_CONFIG_FILE`. This vendor uses the default `mbedtls_config.h` as
  shipped, so none of them is read; a future decision to trim the feature set
  should prefer compile definitions or an explicit dn2cpp-owned config header
  over resurrecting one of these.
- **`docs/`, `doxygen/`, `scripts/`, `ChangeLog`, `ChangeLog.d/`, `pkgconfig/`,
  `visualc/`, `cmake/`, `.github/`** and the root-level build/dev files
  (`CMakeLists.txt`, `Makefile`, `library/CMakeLists.txt`, `library/Makefile`,
  `include/CMakeLists.txt`, `DartConfiguration.tcl`, `BRANCHES.md`, `BUGS.md`,
  `CONTRIBUTING.md`, `README.md`, `SECURITY.md`, `SUPPORT.md`, `dco.txt`), and
  every dotfile at the root and in the kept directories (`.gitattributes`,
  `.gitmodules`, `.globalrc`, `.mypy.ini`, `.pylintrc`, `.readthedocs.yaml`,
  `.travis.yml`, `.uncrustify.cfg`, and the `.gitignore` files in `/`,
  `library/` and `include/` — the `library/` one is quoted above rather than
  vendored).

## Modifications to vendored source

**None.** Every vendored file is byte-identical to the 3.6.7 release asset
(verified with a full `diff -rq` of the copied subset against the extracted
tarball). In particular **the configuration headers are unmodified**:
`include/mbedtls/mbedtls_config.h` and `include/psa/crypto_config.h` are as
shipped, so the defaults tabulated above are upstream's own. Any feature-set
change dn2cpp wants should arrive as compile definitions or an explicitly named
config file from `runtime/CMakeLists.txt`, keeping this tree a clean copy the way
the curl and brotli vendors are.

## License

Mbed TLS is dual-licensed **Apache-2.0 OR GPL-2.0-or-later**, the choice being
the user's. **dn2cpp takes it under Apache-2.0** — every other vendored tree here
(zlib, brotli, curl, bdwgc, highway) is permissive, and a GPL-2.0 election would
change the license of anything a dn2cpp-built binary links.

[`LICENSE`](LICENSE) is copied verbatim from the release asset and contains the
full text of *both* licenses (upstream ships one file with both); the Apache-2.0
election above is the operative one for this repository. Every vendored `.c`/`.h`
carries `SPDX-License-Identifier: Apache-2.0 OR GPL-2.0-or-later` in its own
header (verified: no kept source file lacks it).

## Updating to a new version

1. Download the new **release asset** — `mbedtls-<version>.tar.bz2` from the
   releases page, *not* the auto-generated source tarball (see "Version"), and
   verify its sha256 against the checksum published on the release page.
2. Copy over the same subset: `library/*.c`, `library/*.h`,
   `include/mbedtls/*.h`, `include/psa/*.h`, `LICENSE`. Nothing else — no
   `CMakeLists.txt`, no `Makefile`.
3. **Re-derive the source set rather than assuming it.** Diff the new
   `library/` file list against the vendored one: upstream moves code between
   files across releases, and this vendor is a glob, so an added file is picked
   up only after re-running cmake and a removed one leaves a stale copy behind.
   Re-read `library/.gitignore`'s `START_COMMENTED_GENERATED_FILES` block and
   confirm every file it names is present in the asset — that list is upstream's
   own statement of which sources are generated, and it changes.
4. **Re-check the configuration defaults** in the table under "How it is built"
   by reading the new `include/mbedtls/mbedtls_config.h`, not by trusting this
   file. At minimum: `MBEDTLS_CTR_DRBG_C` (curl `#error`s without it),
   `MBEDTLS_SSL_PROTO_TLS1_3` (silently caps the version range), and
   `MBEDTLS_PSA_CRYPTO_C`/`MBEDTLS_PSA_CRYPTO_CONFIG`. If
   `MBEDTLS_ECDH_VARIANT_EVEREST_ENABLED` or `MBEDTLS_PSA_P256M_DRIVER_ENABLED`
   ever becomes default-on, `3rdparty/` has to be vendored with it or the build
   breaks at the `#include`.
5. **Re-check curl's requirements against the vendored curl**, both directions:
   the floor in `third_party/curl/CMakeLists.txt`
   (`if(MBEDTLS_VERSION VERSION_LESS 3.2.0)`) and the version arms in
   `third_party/curl/lib/vtls/mbedtls.c` (`0x03020000`, `0x03060100`,
   `0x04000000`). Crossing 4.0 is not an update — it is a re-vendor (see
   "Version").
6. Re-run the standalone smoke compile of every `library/*.c` — host clang **and**
   `emcc` — before trusting the new tree to still need no `-D` flags, then build
   a runtime and an HTTPS gate against it.
7. Update the version/URL/sha256/file-count above.
