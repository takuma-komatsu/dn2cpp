# Vendored nghttp2

This directory is a **vendored subset** of [nghttp2](https://nghttp2.org), the
HTTP/2 framing/HPACK library, compiled into the dn2cpp runtime as the backend of
the vendored libcurl's HTTP/2 support (`third_party/curl/lib/http2.c`, curl's `h2`
connection filter). It is **not** reached from managed code and defines no
`dn2cpp_*` or P/Invoke symbol: its only consumer is curl, and curl's only consumer
is `runtime/core/intrinsics/dn2cpp_http2_stream.cpp`.

**Why it is here at all.** ALPN offers `h2` only when a backend for it is linked,
and a server that is not offered `h2` speaks HTTP/1.1 over the same TLS — so for
plain `HttpClient` traffic this library is an optimization nobody asked for. What
asks for it is **gRPC**: gRPC is *defined* over HTTP/2, `Grpc.Net.Client` speaks
nothing else, and the two pieces a unary call needs have no HTTP/1.1 spelling at
all — one long-lived connection multiplexing concurrent streams, and the trailing
`HEADERS` frame carrying `grpc-status`. Both live in curl's `h2` filter, i.e. in
this library. See the `USE_NGHTTP2` block in `runtime/CMakeLists.txt`.

## Version

- **nghttp2 1.70.0** (`lib/includes/nghttp2/nghttp2ver.h`: `NGHTTP2_VERSION
  "1.70.0"`, `NGHTTP2_VERSION_NUM 0x014600`).
- Upstream: https://github.com/nghttp2/nghttp2 — official release tarball
  `https://github.com/nghttp2/nghttp2/releases/download/v1.70.0/nghttp2-1.70.0.tar.xz`
  (sha256 `e05cb1388eaca3830aded4ccf20044b6e1ac1a61411dcca11b0437c4285c8bc2`,
  measured on the downloaded artifact and cross-checked against the `checksums.txt`
  asset of the same release). Current latest release as of this vendoring
  (`gh api repos/nghttp2/nghttp2/releases/latest` -> `v1.70.0`).
- **The floor is curl's, and curl states it as a hard error, not a preference**:
  `third_party/curl/lib/http2.c` opens with `#if NGHTTP2_VERSION_NUM < 0x010f00 /
  #error "nghttp2 1.15.0 or greater required"`. Any version this directory holds
  must clear that, and a downgrade below it fails the C preprocessor rather than
  producing a working-looking build.

The **release tarball**, not a git tag archive: the git tree ships
`lib/includes/nghttp2/nghttp2ver.h.in` only, and the release tarball ships that
header already instantiated — which is what keeps "modifications: none" below true
(see "Modifications to vendored source").

Wire-level parity with real .NET's `SocketsHttpHandler` is not a goal, for the same
reason it is not one for curl itself (`third_party/curl/DN2CPP-VENDORED.md`): both
are HTTP/2 implementations of the same RFC, and the gates assert *observable*
behaviour, never the frames on the socket.

## How it is built

Hand-globbed into one static library, **not** driven through nghttp2's own
CMakeLists — the `dn2cpp_zlib` / `dn2cpp_brotli` / `dn2cpp_mbedtls` pattern rather
than the `add_subdirectory` one curl needs. The reason is the same as Mbed TLS's:
nghttp2's build exists to produce an installable shared+static pair with an export
set, a pkg-config file and a generated `nghttp2ver.h`, none of which this tree
wants, and every choice its configure makes is expressible as the handful of `-D`
flags below. `runtime/CMakeLists.txt`'s `dn2cpp_nghttp2` target
`file(GLOB ...)`s every `third_party/nghttp2/lib/*.c` with
`lib/includes/` as the public include dir (the sources include their own API
headers as `<nghttp2/nghttp2.h>`).

Confirmed compile flags (Apple clang, macOS arm64; also intended for Linux — plain
POSIX C, no configure step):

```sh
clang -c -std=c11 -O2 -fPIC -w -Ithird_party/nghttp2/lib \
    -Ithird_party/nghttp2/lib/includes -DNGHTTP2_STATICLIB \
    -DHAVE_ARPA_INET_H -DHAVE_NETINET_IN_H \
    -DHAVE_CLOCK_GETTIME -DHAVE_DECL_CLOCK_MONOTONIC=1 \
    third_party/nghttp2/lib/<file>.c -o <file>.o
```

- **No `config.h` is needed, and none is supplied.** Every `#include <config.h>` in
  the tree sits behind `#ifdef HAVE_CONFIG_H`, which is never defined here; the
  `HAVE_*` symbols the sources actually consult are passed as target compile
  definitions instead of by patching sources or synthesizing a config header.
  Confirmed via a standalone smoke compile of all 26 vendored `.c` files before
  committing to this file set.
- **`NGHTTP2_STATICLIB` is `PUBLIC`** — set both when compiling this library and
  when compiling curl's TUs that include `<nghttp2/nghttp2.h>`, so both sides see
  the same declarations. It is also what makes hidden visibility work (below).
- **Two of the `HAVE_*` defines change generated code, not merely an include**,
  which is why they are set rather than left at their silent, working-looking
  defaults:
  - `HAVE_ARPA_INET_H` / `HAVE_NETINET_IN_H` gate `lib/nghttp2_net.h`'s only two
    includes, and `nghttp2_helper.c` / `nghttp2_hd_huffman.c` call
    `htonl`/`htons`/`ntohl`/`ntohs`. On Darwin those arrive as macros
    *transitively* — `nghttp2.h` includes `<sys/types.h>`, which reaches
    `machine/endian.h`, which `#define`s them (verified: a compile with
    `-Werror=implicit-function-declaration` and neither define succeeds on this
    host, and `clang -E -dM` shows `#define htonl(x) __DARWIN_OSSwapInt32(x)`).
    On glibc the declarations live in `<arpa/inet.h>` and nowhere else. So
    omitting these compiles clean on macOS and breaks on Linux — measured both
    ways here before writing it down.
  - `HAVE_CLOCK_GETTIME` + `HAVE_DECL_CLOCK_MONOTONIC` select
    `nghttp2_time.c`'s `CLOCK_MONOTONIC` arm. Omitting them selects the
    `time(NULL)` fallback, which compiles and runs and is a **wall** clock:
    `nghttp2_ratelim`'s token bucket would then jump on an NTP step.
    `HAVE_DECL_CLOCK_MONOTONIC` is tested **by value**
    (`#elif defined(HAVE_CLOCK_GETTIME) && HAVE_DECL_CLOCK_MONOTONIC`), so it
    must be `=1`, not merely defined.

  Both are `PRIVATE` and guarded `if(NOT WIN32)`; the Windows arm passes
  `HAVE_WINDOWS_H HAVE_GETTICKCOUNT64` instead, which is `nghttp2_time.c`'s
  `GetTickCount64()` arm (Vista+). Emscripten never reaches this code at all —
  `DN2CPP_USE_CURL` is forced off there.
- **`BUILDING_NGHTTP2`, which upstream's own build defines, is deliberately NOT
  defined.** Under `NGHTTP2_STATICLIB` its only remaining effect is
  `#undef NGHTTP2_NO_SSIZE_T`, and nothing here defines that symbol; leaving it out
  means no arm of `nghttp2.h` can select default visibility even if the
  `NGHTTP2_STATICLIB` define were ever lost, which is the fail-safe direction.
- **Hidden visibility, inherited and load-bearing.** The target picks up the
  directory-scoped `-fvisibility=hidden` (`runtime/CMakeLists.txt`'s
  `DN2CPP_HIDDEN_VISIBILITY` block), and with `NGHTTP2_STATICLIB` set
  `NGHTTP2_EXTERN` expands to *nothing* (`nghttp2.h`'s first arm), so every
  `nghttp2_*` definition takes that directory default. Verified with
  `nm -m`: `_nghttp2_session_send` and its siblings are `private external` in
  `libdn2cpp_nghttp2.a`, exactly as `libdn2cpp_mbedtls.a`'s `mbedtls_*` are. This is
  the same isolation argument as Mbed TLS's — a dn2cpp GDExtension/mono-module
  library loaded into a host process that has its own nghttp2 must not have its
  calls bound to the other image's definitions. Do not exempt this target, and do
  not drop `NGHTTP2_STATICLIB`: without it the `BUILDING_NGHTTP2` arm would stamp
  `visibility("default")` back onto the whole API.
- **`-w`** — third-party source, suppress its warnings (via the shared
  `_dn2cpp_compile_options_silent` helper, matching `dn2cpp_zlib` /
  `dn2cpp_brotli` / `dn2cpp_mbedtls`).
- `-fPIC` comes from the project's `CMAKE_POSITION_INDEPENDENT_CODE ON`; it is
  listed above only because that flag list documents a standalone manual compile.

### How curl is handed this library

curl's `CMake/FindNGHTTP2.cmake` skips every host search (pkg-config, a `nghttp2`
CONFIG package, `find_path`/`find_library`) when `NGHTTP2_INCLUDE_DIR` and
`NGHTTP2_LIBRARY` are already defined, and defines the imported target
`CURL::nghttp2` only `if(NOT TARGET CURL::nghttp2)`. Both hooks are used, exactly
as for Mbed TLS:

- `NGHTTP2_LIBRARY` is pre-seeded with the **target name** `dn2cpp_nghttp2` — the
  module only tests it for non-emptiness, and never builds a link interface out of
  it because `CURL::nghttp2` already exists.
- `NGHTTP2_INCLUDE_DIR` must be a real path: the module reads `NGHTTP2_VERSION` out
  of `${NGHTTP2_INCLUDE_DIR}/nghttp2/nghttp2ver.h`.
- `CURL::nghttp2` is pre-created as an **ALIAS** of `dn2cpp_nghttp2`, so the name
  curl writes into the static library's `$<LINK_ONLY:...>` interface — and thus into
  the `dn2cpp-targets.cmake` the runtime `export()`s — resolves to a target that *is*
  in the export set (it lands as `dn2cpp::dn2cpp_nghttp2`). An INTERFACE IMPORTED
  target belongs to no export set, so without the alias a per-app configure that
  imports the runtime would fail at generate time on an unresolvable
  `CURL::nghttp2`.

`USE_NGHTTP2 ON` is a *request*: curl answers a failed `find_package` by silently
setting it back OFF, so `runtime/CMakeLists.txt` re-tests it after
`add_subdirectory` and `FATAL_ERROR`s. A configure that reaches the end therefore
reports `Features: ... HTTP2 ...` by construction.

## What was kept / dropped

Kept — the complete C library and its public headers, and nothing else. The `.c`
set is not a hand-picked subset: it is **exactly** upstream `lib/CMakeLists.txt`'s
`NGHTTP2_SOURCES` list (compare the two if the tree is ever updated), because
nghttp2's session/frame/HPACK machinery is one internally entangled unit with no
per-entry-point cut available.

- `lib/*.c` — 26 files (the 25 `nghttp2_*.c` plus `sfparse.c`, the structured-fields
  parser `nghttp2_http.c` uses for the priority header).
- `lib/*.h` — 26 private headers.
- `lib/includes/nghttp2/nghttp2.h`, `lib/includes/nghttp2/nghttp2ver.h` — the public
  API surface curl includes.
- `COPYING` — the license text (see "License").

That is **55 vendored files** (26 + 26 + 2 + 1); `find third_party/nghttp2 -type f`
reports 56, the extra one being this document.

Dropped: `src/` (the `nghttp2`/`nghttpd`/`nghttpx`/`h2load` C++ programs),
`tests/`, `integration-tests/`, `examples/`, `contrib/`, `bpf/`, `doc/`,
`third-party/` (nghttp2's own vendored deps — llhttp, mruby, urlparse — reached only
by `src/`), `m4/`, `cmake/`, and every build-system and doc file at both the root and
inside `lib/`: `CMakeLists.txt`, `CMakeOptions.txt`, `cmakeconfig.h.in`,
`config.h.in`, `configure`, `configure.ac`, `aclocal.m4`, `Makefile.am`,
`Makefile.in`, `Makefile.msvc`, `compile`, `config.guess`, `config.sub`, `depcomp`,
`install-sh`, `ltmain.sh`, `missing`, `test-driver`, `lib/config.cmake.in`,
`lib/libnghttp2.pc.in`, `lib/version.rc.in`, `lib/includes/CMakeLists.txt`,
`lib/includes/Makefile.{am,in}`, `lib/includes/nghttp2/Makefile.{am,in}`,
`android-config`, `android-env`, `Dockerfile.android`, `nghttpx.conf.sample`,
`proxy.pac.sample`, `AUTHORS`, `ChangeLog`, `INSTALL`, `NEWS`, `README`,
`README.rst`.

Also dropped: **`lib/includes/nghttp2/nghttp2ver.h.in`**, the autoconf template. The
release tarball ships the instantiated `nghttp2ver.h` beside it, so the shipped
header is vendored as-is and the template is dead weight — see below.

## Modifications to vendored source

**None.** Every vendored file is byte-identical to the 1.70.0 release tarball, and
**no header was hand-generated**: `lib/includes/nghttp2/nghttp2ver.h` comes from the
tarball already instantiated (`NGHTTP2_VERSION "1.70.0"` /
`NGHTTP2_VERSION_NUM 0x014600`), which is the whole reason the *release* tarball is
the source rather than a git tag archive. The configless build's needs are met
entirely by target compile definitions (see "How it is built"), never by editing a
source file.

## License

MIT (see [`COPYING`](COPYING), copied verbatim from the release tarball); every
vendored source file also retains its own MIT notice in its header comment.

## Updating to a new version

1. Take the new official **release** tarball (not a git tag archive — see "Version")
   and copy over the same subset: `lib/*.c`, `lib/*.h`,
   `lib/includes/nghttp2/nghttp2.h`, `lib/includes/nghttp2/nghttp2ver.h`, `COPYING`.
   Confirm the new `nghttp2ver.h` is instantiated and not a `@PACKAGE_VERSION@`
   template.
2. Re-derive the `.c` set from the new `lib/CMakeLists.txt`'s `NGHTTP2_SOURCES` and
   confirm it still equals `lib/*.c` — if upstream adds a source the glob does not
   want, or wants one the glob does not see, this file's "kept" claim above is what
   has to change.
3. Re-check the `HAVE_*` surface (`grep -rho 'HAVE_[A-Z0-9_]*' lib/`) against the
   defines in `runtime/CMakeLists.txt`'s `dn2cpp_nghttp2` block: a newly-consulted
   `HAVE_*` left undefined silently selects a fallback arm, which is how a wall
   clock or a missing `htonl` gets in.
4. Re-run a standalone smoke compile of every `.c` file (see "How it is built"),
   then configure a runtime and confirm curl still prints
   `Features: ... HTTP2 ...` — the `FATAL_ERROR` after curl's `add_subdirectory`
   makes a broken handoff loud, and `NGHTTP2_VERSION` in the build dir's
   `CMakeCache.txt` shows which version the module actually resolved. Finally run
   `gates/build-and-run-http-get.sh` (sections 13-15 are the live TLS handshake).
5. Confirm the new version still clears curl's `#error` floor in
   `third_party/curl/lib/http2.c`.
6. Update the version/URL/sha256 above.
