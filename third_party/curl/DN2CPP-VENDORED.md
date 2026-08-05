# Vendored libcurl

This directory is a **vendored subset** of [curl](https://curl.se), built into
the dn2cpp runtime to back `System.Net.Http`'s transport
(`runtime/core/intrinsics/dn2cpp_http2_stream.cpp`, which lowers the managed
`HttpClient` stack to the `dn2cpp_http2_call_*` helpers declared in
`runtime/core/dn2cpp_core.h`).

**`http://` and `https://` alike, HTTP/1.1 and HTTP/2 alike.** TLS comes from the
vendored Mbed TLS (`third_party/mbedtls/`) with the vendored Mozilla bundle
(`third_party/cacert/cacert.pem`) as its trust anchors, and HTTP/2 framing from the
vendored nghttp2 (`third_party/nghttp2/`), so `libcurl_static` stays a
self-contained static archive with no OpenSSL and nothing found on the build host.
See "How it is built".

## Version

- **curl 8.21.0** (`include/curl/curlver.h`: `LIBCURL_VERSION "8.21.0"`).
- Upstream: https://github.com/curl/curl — official release tarball
  `https://github.com/curl/curl/releases/download/curl-8_21_0/curl-8.21.0.tar.gz`
  (sha256 `d9b327997999045a24cda50f3983e69e51c516bd8be6ef9842fc7f99135e33bb`,
  measured on the downloaded artifact). This is the current latest release as of
  this vendoring.

Unlike the zlib/brotli vendors, byte-exact wire parity with real .NET's
`SocketsHttpHandler` is not a goal: both are HTTP/1.1+ clients speaking the same
protocol, and the gates assert *observable* round-trip behaviour (status, body,
headers), never the exact bytes on the socket.

## How it is built

curl is **not** hand-globbed the way zlib/brotli are: it needs a generated
`curl_config.h` (built from `lib/curl_config-cmake.h.in` by ~200 platform-probe
tests) plus a large set of `-D` defines that only its own configure produces. So
`runtime/CMakeLists.txt`'s `DN2CPP_USE_CURL` arm pulls curl's own `CMakeLists.txt`
in with `add_subdirectory(... EXCLUDE_FROM_ALL)` and links the static-library
target it defines, **`libcurl_static`** (curl also aliases it `CURL::libcurl_static`;
the built archive is `libcurl.a`). The option is **ON by default**, like
`DN2CPP_USE_ZLIB`/`DN2CPP_USE_BROTLI`: `HttpClient` is BCL surface and the
transpiler's route to these helpers is unconditional, so a default that made it
fail to link was a landmine. It costs nothing to carry, because the transport
initializes lazily (`std::call_once` around `curl_global_init`, in
`dn2cpp_http2_stream.cpp`) — a program that issues no request names no symbol in that
TU, static-archive semantics leave its object unextracted, and curl, Mbed TLS and
the ~180 KB CA bundle are never linked at all (measured +0 bytes on the HelloWorld
console sample).

`-DDN2CPP_USE_CURL=OFF` is the supported opt-out: with it off this tree is never
configured and the `dn2cpp_http_*` symbols are undefined, so a program that reaches
the HTTP surface fails loudly at link — the same opt-out discipline as
`DN2CPP_USE_ZLIB`, and deliberately *not* what the Web lane does (there the option
is forced off by the platform and the transport's fallback arm answers at run time
instead; the two are told apart in `dn2cpp_http2_stream.cpp`'s header).

Before `add_subdirectory`, that arm force-sets curl's cache variables to pin the
one shape this project wants (a static library, nothing else):

- `BUILD_CURL_EXE OFF`, `BUILD_SHARED_LIBS OFF`, `BUILD_STATIC_LIBS ON`,
  `BUILD_TESTING OFF`, `BUILD_EXAMPLES OFF`, `BUILD_LIBCURL_DOCS OFF`,
  `BUILD_MISC_DOCS OFF`, `ENABLE_CURL_MANUAL OFF`, `CURL_DISABLE_INSTALL ON` —
  library only, no CLI/tests/examples/docs/install rules.
- `HTTP_ONLY ON` — HTTP is the sole protocol handler, and `https` is **not** one of
  the ones it disables: `lib/protocol.c` gates the https handler on `USE_SSL` alone.
  "HTTP only" and "TLS on" are orthogonal here, and both are wanted (curl configures
  with `Protocols: http https`).
- `CURL_ENABLE_SSL ON` + `CURL_USE_MBEDTLS ON` — TLS from the vendored Mbed TLS and
  from nothing else. Every other `CURL_USE_<backend>` (OpenSSL, Schannel, wolfSSL,
  GnuTLS, Rustls) is forced `OFF` explicitly rather than left to its default —
  `CURL_USE_OPENSSL`'s default is *not* off (curl computes it as "on unless another
  backend is selected"), and a `find_package(OpenSSL REQUIRED)` grabbing whatever the
  build host has is the exact host dependency this vendor exists to avoid.
  `CURL_CA_NATIVE OFF`, `USE_APPLE_SECTRUST OFF`, `CURL_CA_BUNDLE/CURL_CA_PATH none`
  keep the anchors the embedded bundle and only that — `auto` would bake the build
  machine's `/etc/ssl/cert.pem` path into `curl_config.h`. The full reasoning is at
  each `set()` in `runtime/CMakeLists.txt`'s arm.
- `CURL_USE_LIBSSH2 OFF`, `CURL_USE_LIBSSH OFF`, `CURL_USE_LIBPSL OFF`,
  `USE_LIBIDN2 OFF` — no optional transport/helper libraries pulled from the host.
- `USE_NGHTTP2 ON` — **HTTP/2, from the vendored `third_party/nghttp2/`** and, like
  the TLS backend, from nothing on the host. It is a requirement rather than an
  upgrade: gRPC is defined over HTTP/2, and the two pieces `Grpc.Net.Client` needs
  have no HTTP/1.1 spelling — stream multiplexing over one connection, and the
  trailing `HEADERS` frame carrying `grpc-status`. Both live in curl's `h2` filter
  (`lib/http2.c`). The handoff is the same shape as the Mbed TLS one — pre-seeded
  `NGHTTP2_INCLUDE_DIR`/`NGHTTP2_LIBRARY` plus a pre-created `CURL::nghttp2` ALIAS
  of the real target — and it is asserted rather than assumed: curl answers a failed
  `find_package(NGHTTP2 MODULE)` by silently setting `USE_NGHTTP2` back OFF, so
  `runtime/CMakeLists.txt` re-tests it after `add_subdirectory` and `FATAL_ERROR`s.
  Details in `third_party/nghttp2/DN2CPP-VENDORED.md`.
- `CURL_ZLIB OFF`, `CURL_BROTLI OFF`, `CURL_ZSTD OFF` — **content-encoding
  decompression off.** These default to **`AUTO`**, not `OFF`: left alone curl's
  `find_package` would locate the system zlib/brotli/zstd and add them
  (`ZLIB::ZLIB`, ...) to `libcurl_static`'s link interface, i.e. drag an external
  dependency into every binary that links the runtime. Forcing them off keeps the
  archive self-contained. (A later change can wire curl's `CURL_ZLIB` to the
  runtime's *own* vendored `third_party/zlib` if transparent gzip/deflate is
  wanted.)

On this host (macOS) curl's only link additions are Apple system frameworks
(`-framework CoreFoundation`/`CoreServices`/`SystemConfiguration`) and the
`Threads::Threads` imported target — the same `Threads::Threads` `dn2cpp_runtime`
already links and exports, so it introduces no new import requirement. The
frameworks ride through `export()` as plain `-framework` strings.

Confirmed to configure and build `libcurl_static` -> `libcurl.a` standalone with
exactly the cache set above (Apple clang, macOS arm64; also intended for Linux —
curl's CMake configure is portable) — reporting `Protocols: http https`,
`Enabled SSL backends: mbedTLS`, and `HTTP2` among its features, the three lines
that say the vendored backends were the ones it found. Re-confirmed on the two cross axes the
default-ON flip newly reaches, both of which configure and build curl+Mbed TLS
unchanged: the iOS-simulator SDK (`arm64;x86_64`, deployment target 16.3) and the
Android NDK (`arm64-v8a`, API 24). Emscripten is the one target that does not build
this tree at all — the option is forced off there (see `runtime/CMakeLists.txt`'s
Web-lane carve-out; a browser has no TCP socket layer, and curl's own
`find_package(Threads)` would not resolve on that arm either).

## What was kept / dropped

Kept — the minimal set curl's CMake build reads to configure and compile the
library:

- `lib/` — the entire library source tree (C sources + headers, the
  `curlx/ vauth/ vquic/ vssh/ vtls/` subdirectories, `Makefile.inc` — the
  authoritative source list curl's CMake transforms — and the config template
  `curl_config-cmake.h.in`). The pure-autotools `Makefile.am`/`Makefile.in`/
  `Makefile.soname` files, which the CMake build never reads, are dropped.
- `include/curl/*.h` — the public API headers (`curl.h`, `easy.h`, `multi.h`,
  `header.h`, ...). curl exposes `$<BUILD_INTERFACE:.../include>` on
  `libcurl_static`, which `export()` carries through, so consumers `#include
  <curl/curl.h>` with no extra include-dir wiring.
- `CMake/` — curl's CMake module path (`Utilities`, `Macros`, `OtherTests` +
  `CurlTests.c`, `PickyWarnings`, `CurlSymbolHiding`, the platform caches, the
  `Find*.cmake` modules). Kept whole; the `Find*` modules for the disabled
  backends are inert but harmless.
- `CMakeLists.txt` — curl's top-level build (byte-identical).
- `scripts/CMakeLists.txt` and `docs/CMakeLists.txt` — **only these two files**
  from `scripts/` and `docs/`, no other content. curl's top-level
  `add_subdirectory(scripts)` is unconditional and `add_subdirectory(docs)` runs
  whenever Perl is found, so both stub `CMakeLists.txt` files must exist for the
  configure to succeed — but under our options both are no-ops (everything inside
  is gated on `BUILD_CURL_EXE` / `BUILD_*_DOCS`, all off). This keeps every curl
  file byte-identical rather than patching the top-level `CMakeLists.txt` to drop
  the two `add_subdirectory` calls.
- `COPYING` — the license text (see "License").

Dropped: `src/` (the `curl` CLI), `tests/`, `docs/` (all but the stub
`CMakeLists.txt`), `scripts/` (all but the stub `CMakeLists.txt`), `m4/`,
`projects/`, and every root-level autotools/dev/CI file (`configure`,
`configure.ac`, `Makefile*`, `aclocal.m4`, `acinclude.m4`, `config.guess`,
`config.sub`, `compile`, `depcomp`, `install-sh`, `ltmain.sh`, `missing`,
`curl-config.in`, `libcurl.pc.in`, `Dockerfile`, `CHANGES.md`, `RELEASE-NOTES`,
`README`). The install/pkg-config machinery those root files feed is all inside
curl's `if(NOT CURL_DISABLE_INSTALL)` block, which our `CURL_DISABLE_INSTALL ON`
skips.

Note: curl 8.21.0 has **no `LICENSES/` directory** at the tarball root (only
`COPYING`), so there is none to vendor.

## Modifications to vendored source

**None.** Every vendored file is byte-identical to the 8.21.0 release tarball. The
build shape is imposed entirely through cache variables set by the dn2cpp
`CMakeLists.txt` before `add_subdirectory`, not by editing curl's sources — which
keeps updates and audits simple.

## License

curl's permissive `curl` license (SPDX-License-Identifier: `curl`, an
MIT/X-derivative) — see [`COPYING`](COPYING), copied verbatim from the release
tarball; every vendored source file also carries its own SPDX header.

## Updating to a new version

1. Download the new official release tarball and copy over the same subset above:
   `lib/` (drop `Makefile.am`/`Makefile.in`/`Makefile.soname`), `include/curl/*.h`,
   `CMake/`, top-level `CMakeLists.txt`, `scripts/CMakeLists.txt`,
   `docs/CMakeLists.txt`, `COPYING`.
2. Re-confirm the static-library target is still named `libcurl_static`
   (`CMakeLists.txt`: `set(LIB_STATIC "libcurl_static")`) and that the two
   `add_subdirectory(scripts|docs)` calls are still stub-only under our options; if
   curl restructures either, adjust the kept file set.
3. Re-confirm `curl_dependency_option`'s default is still `AUTO` (so `CURL_ZLIB`/
   `CURL_BROTLI`/`CURL_ZSTD` still need the explicit `OFF`), and that
   `CURL_USE_OPENSSL`'s default is still "on unless another backend is selected" (so
   it still needs the explicit `OFF` beside `CURL_USE_MBEDTLS ON`).
4. Re-confirm `CMake/FindNGHTTP2.cmake` still skips its host searches when
   `NGHTTP2_INCLUDE_DIR` and `NGHTTP2_LIBRARY` are pre-defined, still reads the
   version out of `${NGHTTP2_INCLUDE_DIR}/nghttp2/nghttp2ver.h`, and still guards
   its imported target with `if(NOT TARGET CURL::nghttp2)` — and that
   `lib/http2.c`'s `#error` floor is still one the vendored nghttp2 clears.
5. Configure + build `libcurl_static` standalone with the cache set in
   `runtime/CMakeLists.txt`'s `DN2CPP_USE_CURL` arm to confirm it still produces
   `libcurl.a` with `Protocols: http https`, `Enabled SSL backends: mbedTLS` and
   `HTTP2` in its feature list, then build a runtime with the option at its default
   and run `gates/build-and-run-http-get.sh` (sections 13-15 are the live TLS
   handshake).
6. Update the version/URL/sha256 above.
