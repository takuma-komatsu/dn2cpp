# Vendored Zstandard

The `dn2cpp_zstd` static library provides the `ZSTD_*` and `ZDICT_*`
symbols imported by .NET's `System.IO.Compression`. Its source matches
[the zstd tree in dotnet/runtime v11.0.0](https://github.com/dotnet/runtime/tree/v11.0.0/src/native/external/zstd).

## Source and updates

Copy the public headers `lib/{zstd,zdict,zstd_errors}.h`, all `.c` and
`.h` files in `lib/{common,compress,decompress,dictBuilder}/`, and
`LICENSE` from the pinned runtime tree. Keep the files byte-identical,
including embedded license notices. When updating the runtime pin, check
its [version record](https://github.com/dotnet/runtime/blob/v11.0.0/src/native/external/zstd-version.txt)
and [build settings](https://github.com/dotnet/runtime/blob/v11.0.0/src/native/external/zstd.cmake).
The runtime carries upstream fixes on top of the
[v1.5.7 release](https://github.com/facebook/zstd/releases/tag/v1.5.7).

`runtime/CMakeLists.txt` builds the portable C decoder, encoder and
dictionary trainer. Legacy frame support and multithreaded compression
are disabled as in .NET. Android uses the portable sort implementation
because the NDK's `qsort_r` availability varies by API level.

## License

dn2cpp uses Zstandard under the upstream [BSD 3-Clause license](LICENSE).
Distribute [the component notices](THIRD-PARTY-NOTICES.txt) with the license.
They include the xxHash and threading credits and the dictionary builder's
[libdivsufsort MIT notice](lib/dictBuilder/divsufsort.c).
