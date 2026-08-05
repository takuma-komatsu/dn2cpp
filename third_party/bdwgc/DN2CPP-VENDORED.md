# Vendored Boehm-Demers-Weiser GC (bdw-gc)

This directory is a **vendored subset** of the Boehm-Demers-Weiser conservative
garbage collector, compiled directly into the dn2cpp runtime (IL2CPP-style): the
collector is built from source as part of the normal build.

## Version

- **bdw-gc 8.2.8** (`include/gc_version.h`: 8.2.8).
- Upstream: https://github.com/bdwgc/bdwgc — release tarball
  `https://github.com/bdwgc/bdwgc/releases/download/v8.2.8/gc-8.2.8.tar.gz`.

## How it is built

The collector is built as a **single-file amalgamation**: `extra/gc.c` `#include`s
every root `*.c`. The build (`gates/_common.sh::_cache_gc_obj`) compiles just that
one translation unit into a cached PIC object and links it with the rest of the
runtime. The runtime's `#include <gc.h>` resolves against `include/` (added as
`-Ithird_party/bdwgc/include`).

Confirmed compile flags (Apple clang, macOS arm64; also intended for Linux):

```sh
clang -c -x c -std=c11 -O2 -fPIC -w \
    -DGC_THREADS -DGC_BUILTIN_ATOMIC -DNO_EXECUTE_PERMISSION \
    -Ithird_party/bdwgc/include third_party/bdwgc/extra/gc.c -o bdwgc.o
```

- **`-DGC_THREADS`** — thread-aware build (ahead of managed-thread support; the
  runtime currently runs single-threaded, which a thread-aware GC handles fine).
- **`-DGC_BUILTIN_ATOMIC`** — use the compiler's `__atomic_*` builtins instead of
  libatomic_ops, so **libatomic_ops is not vendored**.
- **`-DNO_EXECUTE_PERMISSION`** — do not request executable heap pages.
- **`-w`** — third-party source; on macOS 13+ it warns about the deprecated
  `get_etext`/`get_end` (used for the data-segment bounds) — harmless, suppressed.

> Linker note (macOS, thread-aware build): linking the object emits a few
> `REFERENCED_DYNAMICALLY flag on symbol '_catch_exception_raise*' is deprecated`
> warnings. These are the Mach exception handlers used to stop threads for a
> collection — required for `GC_THREADS` on Darwin, harmless.

## The wasm (Emscripten) build

The wasm axis compiles this **same unpatched tree** — no source patch — with a
different flag set (`runtime/CMakeLists.txt`, the `EMSCRIPTEN` arm of the GC
target):

```sh
emcc -c ... -DGC_BUILTIN_ATOMIC -DNO_EXECUTE_PERMISSION \
    -DGC_DISABLE_INCREMENTAL -D_GNU_SOURCE \
    -Ithird_party/bdwgc/include third_party/bdwgc/extra/gc.c -o bdwgc.o
```

- **No `GC_THREADS`** — bdwgc's `EMSCRIPTEN` arm `#error`s under it, and the wasm
  build is single-threaded anyway. (Native still passes `-DGC_THREADS`.)
- **`-DGC_DISABLE_INCREMENTAL`** — there is no mprotect/signal dirty-bit VDB under
  Emscripten, so bdwgc's incremental mode would fall back to `DEFAULT_VDB` (every
  page always dirty). Incremental is also forced off at runtime under
  `__EMSCRIPTEN__`.
- **`-D_GNU_SOURCE`** — emscripten's `unistd.h` hides the `sbrk()` declaration
  behind it, and `os_dep.c`'s EMSCRIPTEN arm calls `sbrk()`; without it `gc.c`
  does not compile under emcc.
- `-DGC_BUILTIN_ATOMIC` / `-DNO_EXECUTE_PERMISSION` are as on native.

**Static-data roots come from a named section, not from the collector's data-segment
bounds.** bdwgc's `EMSCRIPTEN` arm registers an **empty** static-data range
(`gcconfig.h` `DATASTART == DATAEND`; upstream master is the same), so the collector
would never scan the module's globals. dn2cpp does not patch that; instead the runtime
`GC_add_roots`es the linker-encapsulated `[__start_dn2cpp_roots, __stop_dn2cpp_roots)`
range once after `GC_INIT`, and every GC-pointer-holding data-segment global is placed
in that `dn2cpp_roots` section (the emitter stamps `DN2CPP_GC_STATIC_ROOT` on GC-ref
statics; runtime root heads are hand-annotated).

**Stack scanning depends on SpillPointers.** A pointer live only in a wasm local is
invisible to a linear-memory scanner, so the runtime link runs Binaryen's SpillPointers
pass (`-sBINARYEN_EXTRA_PASSES=--spill-pointers`) to spill pointer-typed locals to
memory before a collection can see them. Without it the conservative scan is unsound on
wasm.

## What was kept / dropped

Kept: every root `*.c` (all are pulled in by `extra/gc.c`), `extra/gc.c`, the
whole `include/` tree (public + `private/` headers), and `README.md` /
`README.QUICK` / `AUTHORS` / `ChangeLog` for the license/attribution notices
(the permissive bdw-gc license is also retained in every source-file header).

Dropped (not needed for the amalgamation): the autotools/CMake build system,
`tests/`, `doc/`, the `cord/` string library, the C++ allocator variants
(`gc_cpp.*`, `gc_badalc.*`), other `extra/` platform stubs, and
`ia64_save_regs_in_stack.s`.

## License

The upstream permissive license is transcribed in [`LICENSE`](LICENSE) (a
verbatim copy of the "Copyright & Warranty" section of the upstream
`README.md`); every vendored source file also retains it in its header.

## Updating to a new bdw-gc version

1. Download the new release tarball and replace this tree with the same subset
   (root `*.c`, `extra/gc.c`, `include/`, the license/attribution files).
2. Re-run `./gates/run-all-gates.sh` — the cache key hashes every vendored
   `*.c`/`*.h`, so the GC object rebuilds automatically.
3. Update the version above and the confirmed flags if upstream changes them.
