# Vendored Boehm-Demers-Weiser GC (upstream, dev-only alternate backend)

This tree is the **pre-fork upstream bdw-gc snapshot**, kept as a
development-only alternate GC backend. The production backend is the Unity
fork in `third_party/bdwgc/` (see its `DN2CPP-VENDORED.md`); this tree is
selected instead with the CMake option `-DDN2CPP_GC_BACKEND=upstream`, is not
shipped in the CLI package, and exists for local behavioral comparison against
plain upstream and for fallback verification when a fork-origin bug is
suspected.

## Version

- **bdw-gc 8.2.8** (`include/gc_version.h`: 8.2.8).
- Upstream: https://github.com/bdwgc/bdwgc — release tarball
  `https://github.com/bdwgc/bdwgc/releases/download/v8.2.8/gc-8.2.8.tar.gz`.

## Local modifications

- `include/private/gc_atomic_ops.h` adds a `#if defined(_MSC_VER) &&
  !defined(__clang__)` arm implementing the `GC_BUILTIN_ATOMIC` subset with
  `_Interlocked*` intrinsics: real MSVC (`cl.exe`) has no `__atomic_*` /
  `__GCC_ATOMIC_*` builtins, which the GNU/Clang arm below it relies on.
  Every other file in this tree is byte-identical to the 8.2.8 release
  tarball.

## How it is built

The collector is built as a **single-file amalgamation**: `extra/gc.c` `#include`s
every root `*.c`. The runtime's `#include <gc.h>` resolves against `include/`
(added as `-Ithird_party/bdwgc-upstream/include`).

Confirmed compile flags (Apple clang, macOS arm64; also intended for Linux):

```sh
clang -c -x c -std=c11 -O2 -fPIC -w \
    -DGC_THREADS -DGC_BUILTIN_ATOMIC -DNO_EXECUTE_PERMISSION \
    -Ithird_party/bdwgc-upstream/include third_party/bdwgc-upstream/extra/gc.c -o bdwgc.o
```

- **`-DGC_THREADS`** — thread-aware build (ahead of managed-thread support; the
  runtime currently runs single-threaded, which a thread-aware GC handles fine).
- **`-DGC_BUILTIN_ATOMIC`** — use the compiler's `__atomic_*` builtins (or the
  MSVC arm above) instead of libatomic_ops, so **libatomic_ops is not vendored**.
- **`-DNO_EXECUTE_PERMISSION`** — do not request executable heap pages.
- **`-w`** — third-party source; on macOS 13+ it warns about the deprecated
  `get_etext`/`get_end` (used for the data-segment bounds) — harmless, suppressed.

> Linker note (macOS, thread-aware build): linking the object emits a few
> `REFERENCED_DYNAMICALLY flag on symbol '_catch_exception_raise*' is deprecated`
> warnings. These are the Mach exception handlers used to stop threads for a
> collection — required for `GC_THREADS` on Darwin, harmless.

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
2. Reapply the MSVC atomics modification above.
3. Update the version and confirmed flags if upstream changes them.
