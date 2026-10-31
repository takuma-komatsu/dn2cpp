# Vendored brotli

This directory contains the C library from google/brotli. The dn2cpp runtime
links its public API directly to back `System.IO.Compression.Brotli` when the
managed DnBrotli replacement is disabled. The BCL's P/Invoke entry points name
`BrotliDecoder*` and `BrotliEncoder*` exports, so no native shim is needed.

## Version

- **brotli 1.2.1** (`c/common/version.h`).
- Upstream: https://github.com/google/brotli
- Tag commit: `db24a98aed38a16de78cc05ca15cdd8dc2a0d92d`.
- Release archive: https://github.com/google/brotli/archive/refs/tags/v1.2.1.tar.gz
- Archive SHA-256:
  `98d35d11a78dc453aebb7d10c3748f3f73ff78c8267c0dc697b8631f67bf7e8f`.

Every vendored source file and `LICENSE` is byte-identical to the release
archive. `DN2CPP-VENDORED.md` is the local provenance record.

The native library and the managed port under `internal/DnBrotli/` must both
receive applicable upstream fixes. Updating this tree alone does not update
the managed codec used by default.

Compressed output is implementation- and version-dependent. Gates compare
round-trip and decompression behavior against .NET; they do not require the
same Brotli version or byte-identical compressed output.

## Build and retained files

The complete `c/common/`, `c/dec/`, `c/enc/`, and `c/include/brotli/` trees
and the upstream `LICENSE` are retained, including generated `*_inc.h`
tables. Other upstream directories and root build files are omitted.

`runtime/CMakeLists.txt` builds the `dn2cpp_brotli` static library with CMake
and Ninja. It globs `.c` files in `c/common/`, `c/dec/`, and `c/enc/`, and
exposes `c/include/` as the public include directory. Relative upstream
includes remain intact. Position-independent code follows the runtime build;
third-party warnings are suppressed.

The build uses the default `BROTLI_STATIC_INIT_NONE` configuration and its
precomputed tables. The retained `c/enc/static_init_lazy.cc` belongs to the
optional upstream lazy-initialization configuration and is not compiled.
No additional build definitions are required.

## Updating

1. Download a stable upstream release archive and verify its tag commit and
   SHA-256. Replace the complete retained trees and `LICENSE`, removing files
   absent from the new release. Preserve this provenance record.
2. Review the upstream changes against the managed port in `internal/DnBrotli/`,
   including decoder error propagation, encoder behavior, and generated tables.
   Update the managed version APIs and attribution with the port.
3. Run the compression gates through their individual wrappers to verify
   native and managed round trips, malformed-stream rejection, and codec
   symbol selection. Reconfigure CMake so newly added translation units are
   included.
4. Update the version, tag commit, archive URL, and SHA-256 above. Verify the
   retained file set and every file's bytes against the archive.

## License

MIT (see [`LICENSE`](LICENSE), copied verbatim from the release archive).
