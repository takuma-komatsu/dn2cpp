# Vendored Boehm-Demers-Weiser GC (Unity fork)

This directory is a vendored subset of Unity Technologies' bdwgc fork. The
collector is compiled directly into the dn2cpp runtime.

## Version

- Repository: https://github.com/Unity-Technologies/bdwgc
- Branch: `unity-master`
- Commit: `4314cf4a2b1473ae8ec1aac7887af91d0fbe4d0c`
- Collector version: 7.7.0 development (`include/gc_version.h`)

## Local modifications

- `include/private/gc_atomic_ops.h` implements the `GC_BUILTIN_ATOMIC` subset
  with `_Interlocked*` intrinsics under real MSVC. Unity's GNU/Clang arm uses
  `__atomic_*`, which `cl.exe` does not provide.
- `include/private/gcconfig.h` keeps Emscripten stack scanning enabled and marks
  its direction; `os_dep.c` obtains its cold end from
  `emscripten_stack_get_base()`. The runtime link runs Binaryen's SpillPointers
  pass, making pointer locals visible in the linear-memory stack; Unity's
  `STACK_NOT_SCANNED` setting would leave those roots unmarked.

These modifications retain a notice at the source site as required by the
collector's license.

## Build

`extra/gc.c` is the single-file amalgamation compiled by
`runtime/CMakeLists.txt`. Native builds define `GC_THREADS`,
`GC_BUILTIN_ATOMIC`, and `NO_EXECUTE_PERMISSION`. Emscripten builds omit
`GC_THREADS` and additionally define `GC_DISABLE_INCREMENTAL` and `_GNU_SOURCE`.

The Emscripten fork configuration reports an empty static-data range. dn2cpp
registers its `dn2cpp_roots` linker section after `GC_INIT`; stack roots are
covered by the source modification above and the SpillPointers link pass.

## Vendored subset

Kept: every root `*.c`, `extra/gc.c`, the complete `include/` tree, and
`README.md`, `README.QUICK`, `AUTHORS`, and `ChangeLog`. Unity's amalgamation
requires its root `heapsections.c` and `vector_mlc.c` files.

Dropped: build-system files, tests, documentation outside the root readmes,
the cord implementation, C++ allocator sources, other `extra/` programs, and
assembly sources not included by `extra/gc.c`.

## License

[`LICENSE`](LICENSE) transcribes the `Copyright & Warranty` section of the
vendored fork's `README.md`. Source files retain their individual notices.

## Updating

Replace the same subset from a pinned Unity fork commit, reapply the local
modifications, update this provenance, and run the regression gates. The gate
cache hashes the contents of the complete vendored third-party tree.
