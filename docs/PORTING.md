# PORTING.md — taking dn2cpp to a new target

What you have to know to bring dn2cpp up on an operating system, an ABI or a
device it does not already run on. Every seam named here is a file you can open,
and every count comes with the command that re-derives it.

Read `AGENTS.md` first (build, gates, the skip protocol) and `README.md` for the
architecture. `docs/ARCHITECTURE.md` §4-B is the authority on intrinsics;
`docs/PINVOKE-MARSHALLING.md` on P/Invoke shapes. This file is the authority on
**what is per-target and where it lives**.

---

## 1. What "a target" is, and what it is not

The transpiler is target-agnostic and stays that way. It consumes IL and emits
C++; nothing in `src/` branches on a host OS, and nothing may — the self-host
fixpoint (`gates/selfhost-emit.sh`) requires a successful transpile's output to be
a function of its inputs alone, which is also why `--trim-reflection` is a flag and
never an environment variable. A port therefore adds **no transpiler code** in the
normal case. The two exceptions are about which *managed* code the target's CoreLib
contains, not about the target itself, and both live in
`src/Dn2Cpp.Transpiler/Compilation.cs`:

- `Compilation.IsRuntimeProvidedPInvokeModule` — the allow-list of native modules
  whose `[DllImport]`s the runtime answers. A Windows-family target satisfies most
  of the .NET PAL by admitting the OS import libraries here rather than by writing
  C++ (§2.2).
- the intrinsic route for a P/Invoke module the new target's CoreLib flavour names
  and no existing one does (H6.2).

Everything else is the **C++ runtime under `runtime/`**. It has three per-target
surfaces, plus a fourth thing people forget: the files that branch on the target
*outside* the platform directory.

---

## 2. The four surfaces

### 2.1 The PAL seam — `runtime/core/platform/dn2cpp_pal.h`

The contract is one header. Each target supplies one implementation file under
`runtime/core/platform/<os>/`, and the intrinsics call only through it. The
header's preamble states what is deliberately *not* in the seam — stdio *file*
I/O, `std::chrono`, `std::tm` — because those already build everywhere, and a seam
that abstracts a portable facility is a seam somebody has to keep in sync for
nothing. Console output **is** in the seam, though it reads like it should not be.

**Do not start from this section and a blank file. Start from
`runtime/core/platform/reference/`** — a complete implementation of the seam in
portable C++17 that names no operating system, and that compiles, links and runs.
Copy the directory, add the CMake arm (§3.3), and replace bodies one at a time. The
`PAL_REFERENCE=1` axis of `gates/build-and-run-pal-reference.sh` keeps it working.

The seam declares **eighteen** functions and two enums. Re-derive the function count:

```bash
grep -cE '^[A-Za-z_].*\bdn2cpp_pal_[a-z_0-9]+\(' runtime/core/platform/dn2cpp_pal.h
```

Every implementation defines all eighteen; that parity is the point of the seam,
and section 1 of `gates/build-and-run-pal-reference.sh` enforces it by deriving the
required set from the header and diffing it against every `platform/*/`
implementation. An *extra* `dn2cpp_pal_*` definition fails too — a target growing a
seam member the header does not declare is the same drift the other way. (Counting
an implementation's set by hand needs a `grep -v '^static'`, or a file's local
helpers inflate it.)

**The MUST / MAY-DEGRADE classification lives in the header, not here.** Each
declaration in `dn2cpp_pal.h` carries a `// PAL-CONTRACT: MUST` or
`// PAL-CONTRACT: MAY-DEGRADE <sentinel>` line, that marker is the source of truth,
and `gates/build-and-run-doc-claims.sh` diffs it against the table below.

**Fifteen of the eighteen must answer truly. Exactly three have a documented
"unavailable" answer that a caller already handles**, and knowing which three is
the difference between a port that degrades and a port that lies:

| may degrade | the sentinel | who handles it |
|---|---|---|
| `dn2cpp_pal_executable_path` | returns `-1` | `runtime/core/intrinsics/dn2cpp_system_io.cpp` caches the failure; `Environment.ProcessPath` → null, `AppContext.BaseDirectory` → `""`. The wasm arm returns `-1` unconditionally: a wasm instance is not a process image on a file system. |
| `dn2cpp_pal_backtrace` | returns `0` | `runtime/core/dn2cpp_exceptions.cpp` stamps no trace and `Exception.StackTrace` stays null. The wasm arm returns `0` because `-fwasm-exceptions` exposes no unwinder and a release build carries no name section. |
| `dn2cpp_pal_default_locale_name` | returns `0` | The caller reads it as the invariant culture. The wasm and reference arms return `0` unconditionally — neither has a user to ask. dn2cpp models no ICU, so an invariant default is correct behaviour, merely not localised. |

Everything else — the five file-system calls, `getenv`, the three ANSI transforms,
the process-wide barrier, the two time conversions, the usable-size query, and the
two console entries — is a **correctness** obligation. Three are easy to implement
wrongly in a way that compiles:

- **`dn2cpp_pal_unlink` / `dn2cpp_pal_mkdir` must preserve `errno`.** `ENOENT` is
  how "delete a missing file" becomes a no-op and `EEXIST` is how
  `Directory.CreateDirectory` is idempotent. Collapse failure to `-1` and clear
  `errno` and both become exceptions.
- **`dn2cpp_pal_membarrier_processwide` has no degradation handling at its
  caller.** Implementations may weaken (Linux falls from `membarrier(2)` to an
  mprotect-IPI bounce to a plain seq_cst fence; wasm is a plain fence), but the
  caller in `runtime/core/intrinsics/dn2cpp_system_threading.cpp` has no arm for
  "this did nothing". A no-op implementation is a silent memory-model bug.
- **The three ANSI functions are a per-OS transform, not a formality.** POSIX and
  wasm delegate to the runtime's UTF-8 codec cores; Windows goes through
  `WideCharToMultiByte`/`MultiByteToWideChar` at `CP_ACP`. Getting this wrong does
  not crash — it silently changes what a P/Invoke marshals (H5).

`dn2cpp_pal_malloc_usable_size` has no caller at all on Windows, where the
aligned-realloc path takes `_aligned_realloc`. Implement it anyway: the seam is a
parity contract, and the next caller will not check which targets were exempt.

### 2.2 The .NET PAL surface — `SystemNative_*`

`runtime/core/platform/posix/dn2cpp_system_native.cpp` reimplements the part of
dotnet/runtime's `libSystem.Native` that the real CoreLib's Unix flavour
P/Invokes, plus the two Darwin bridges `dn2cpp_setattrlist` /
`dn2cpp_fsetattrlist`. Count the entry points:

```bash
grep -oE '^[A-Za-z_][A-Za-z_0-9 ]*[ *]+SystemNative_[A-Za-z_0-9]+' \
    runtime/core/platform/posix/dn2cpp_system_native.cpp \
  | grep -oE 'SystemNative_[A-Za-z_0-9]+' | sort -u | wc -l
```

(`grep -c 'extern "C"'` is the wrong question — one `extern "C" {` block wraps
nearly all of it.) The file's own `// ==== … ====` banners group them:
errno/host name, memory, file descriptors, `stat`, paths and links, directory
enumeration and mode/times/ownership, the Darwin `setattrlist` bridge,
copy/fs-type/clocks/CSPRNG, the Darwin special-folder search paths, process and
user identity, and the low-level monitor.

**This surface is a POSIX-family obligation, not a universal one.**

- **Windows has no `system_native` file at all.** The win-x64 CoreLib's Interop
  layer P/Invokes `kernel32`, `ntdll`, `ole32`, `ws2_32`, `iphlpapi`, `advapi32`
  directly in already-blittable shapes, so those calls direct-link once the
  modules are admitted to `Compilation.IsRuntimeProvidedPInvokeModule`. The port
  is a line in the transpiler and a `target_link_libraries` row.
- **wasm defines 29.** They live in
  `runtime/core/platform/wasm/dn2cpp_system_native_wasm.cpp`, in three groups.
  The **non-file** entries are there each for their own reason:
  `SystemNative_GetCryptographicallySecureRandomBytes`, whose caller is not a
  P/Invoke at all; `SystemNative_SysLog` and `SystemNative_Write`, the two sinks
  of `DebugProvider.WriteCore`, hence reached by any game that logs;
  `SystemNative_Malloc` and `SystemNative_Free`, which the intercepted
  `Marshal`/`NativeMemory` surface never reaches but the BSTR allocators and the
  in-CoreLib marshaller stubs do; and the `pal_time.c` clocks behind
  `Stopwatch.GetTimestamp` and `Environment.TickCount64`. Both user calls resolve
  from MemberReferences to real CoreLib bodies and reach P/Invokes; and
  `SystemNative_InterfaceNameToIndex`, whose named IPv6-scope caller gets zero
  because a browser exposes no host interface namespace. An in-CoreLib
  MethodDefinition call to `TickCount64` can instead take the
  `dn2cpp_tickcount64` intrinsic. The timestamp-resolution entry has no
  counterpart here because `Stopwatch.Frequency` is a constant on this CoreLib.

  The **error** entries — the `errno` accessors and the two PAL/platform code
  converters — ride in behind any of the others and behind every PAL failure the
  BCL turns into an exception, with `SystemNative_StrErrorR` formatting it.

  The **file-I/O closure** runs against Emscripten's MEMFS: `FileStream`,
  `SafeFileHandle` and the `File.*` surface all work, and the files are ephemeral
  — they live in the page's memory and are gone when it unloads. It is not
  optional coverage, because a game does not opt into it: `Trace` with a
  `DefaultTraceListener` log file goes `File.AppendAllText` → `SafeFileHandle` →
  the whole closure. Two entries degrade rather than fail, both where .NET treats
  the call as a hint: `SystemNative_PosixFAdvise` is advisory, and
  `SystemNative_FAllocate` does nothing and reports success because MEMFS has no
  size-preserving preallocation primitive — and a preallocation that changed the
  file's LENGTH would make it read back as zeros nobody wrote.

  What is still absent is absent for the ordinary reason — nothing here calls it.
  Lowering a P/Invoke is target-neutral, so excluding the POSIX file excludes the
  definition and leaves its caller; and absence means something weaker here than
  in an executable link, because on a side module an undefined symbol is a wasm
  IMPORT that throws only when first called. That is why the export gates assert
  the drop-in's whole import closure rather than trusting the link, and why
  `gates/build-and-run-wasm-console.sh`'s `PalSurface` bucket must keep REACHING
  every entry this file defines.

So the first question of a port is not "how do I write these functions" but
**"which CoreLib flavour will this target's programs be transpiled from, and what
does *it* P/Invoke?"** Answer that first; it decides whether you are writing a
POSIX PAL, admitting import libraries, or a third thing.

### 2.3 The memory-mapped-file seam

Seven entry points, declared in `runtime/core/dn2cpp_core.h`, implemented twice:

```bash
for f in posix/dn2cpp_mmap_posix windows/dn2cpp_mmap_windows; do
    echo "-- $f"
    grep -oE '^[A-Za-z_].*\bdn2cpp_mmap_[a-z_]+\(' runtime/core/platform/$f.cpp \
      | grep -oE 'dn2cpp_mmap_[a-z_]+' | sort -u
done
```

The entry-point sets are identical; the Windows file carries one extra *file-local*
helper (`dn2cpp_mmap_allocation_granularity`) because `MapViewOfFile` demands 64
KiB allocation-granularity alignment where POSIX `mmap` wants only page alignment —
a real difference in the offset arithmetic a porter has to reproduce.

There is no wasm variant: `runtime/CMakeLists.txt` globs `platform/wasm/*.cpp` and
then explicitly appends `runtime/core/platform/posix/dn2cpp_mmap_posix.cpp`,
because Emscripten's musl serves `open`/`fstat`/`mmap`/`msync` against MEMFS.
**Reusing one POSIX TU on a non-POSIX-glob target is a supported move** and the
cheapest kind of port; it needs one `list(APPEND)` and a comment saying why.

### 2.4 The seam is not the only place

Files outside `runtime/core/platform/` also branch on the target. Enumerate them:

```bash
grep -rlE '__APPLE__|__linux__|__ANDROID__|_WIN32|_MSC_VER|__EMSCRIPTEN__|TARGET_OS_IPHONE|DN2CPP_TARGET_(X64|ARM64|WASM32|OTHER)' \
    runtime/ | grep -v '^runtime/core/platform/' | sort
```

The distribution is the useful part:

- `runtime/core/dn2cpp_gc.cpp` is the heaviest and the one a port is most likely to
  have to touch: stack bounds, thread registration, aligned allocation, the
  incremental-mode decision, the wasm static-root registration.
- `runtime/core/intrinsics/dn2cpp_system_io.cpp` is the largest single-macro
  concentration (`_WIN32`) — path separators and drive letters.
- The `_MSC_VER` hits are **compiler flavour, not OS flavour**, and are the
  cheapest class: no 128-bit integer type, no `<dirent.h>`,
  `__builtin_*_overflow` needing overloads, `_aligned_malloc`'s reversed argument
  order. A new clang/gcc-family target inherits none of them.

Budget for this. A port that plans only for `platform/<os>/` has planned for about
half of it.

### 2.5 CPU feature detection

`runtime/core/dn2cpp_cpu_features.h` derives exactly one of `DN2CPP_TARGET_X64`,
`DN2CPP_TARGET_ARM64`, `DN2CPP_TARGET_WASM32` (else `DN2CPP_TARGET_OTHER`) from
the compiler's architecture macros, and
`runtime/core/intrinsics/dn2cpp_cpu_features.cpp` supplies the detection behind
every `System.Runtime.Intrinsics.X86` / `.Arm` / `.Wasm` `IsSupported`: CPUID
with the OSXSAVE and XGETBV checks the AVX families need on x86-64,
`getauxval(AT_HWCAP)` on Linux arm64, `sysctlbyname("hw.optional.arm.FEAT_*")`
on Darwin arm64, `IsProcessorFeaturePresent` on Windows. A target that supplies
none of these is not broken — every family answers false and the BCL's software
fallback runs — but it has left the hardware paths on the table.

Detection is a **run-time** fact, never a build-time one. The binary that runs
under Rosetta on an iOS-simulator lane was compiled for x86-64 and executes on an
arm64 machine whose CPUID emulation exposes a feature set no shipping CPU has;
a build-time answer would be wrong on either side of that line. The same rule
gives the tests their handle: `DN2CPP_CPU_FEATURES` intersects the detected set
and never widens it, so `none` is the software-fallback build of the same
binary.

---

## 3. The CMake side

`runtime/CMakeLists.txt` is the entire build — no per-directory `CMakeLists.txt`
under `runtime/core`, `runtime/godot` or `runtime/dotnetmodule`, no `find_package`
other than `Threads`, and the only `add_subdirectory` is vendored curl. The tree
contains **no CMake toolchain files**; cross-compilation is caller-driven from
`gates/_common.sh`, which passes the NDK's own `android.toolchain.cmake` for
Android, four plain cache variables for iOS, and swaps the configure command to
`emcmake cmake` for wasm.

### 3.1 Detection is by CMake's own variables

There is no `DN2CPP_TARGET` enum. Every branch keys off `MSVC`, `EMSCRIPTEN`,
`WIN32`, `APPLE`, or `CMAKE_SYSTEM_NAME STREQUAL "iOS"`. **There is no `ANDROID`
branch and no Linux branch anywhere in the file** — both go entirely through the
`else()` arms. That is the target to aim for: iOS costs one line
(`set(CMAKE_MACOSX_BUNDLE OFF)`, so console executables stay plain Mach-O rather
than app bundles), Android costs zero.

### 3.2 The options

The knobs are `option()`s plus a handful of cached `set()`s, all `DN2CPP_`-prefixed:

```bash
grep -nE '^\s*(option|set)\(DN2CPP_[A-Za-z_]+' runtime/CMakeLists.txt
```

The ones a port has to think about:

| option | default | why a port cares |
|---|---|---|
| `DN2CPP_USE_GC` | `ON` | OFF is the calloc fallback — the supported escape hatch for a target with no working conservative collector. |
| `DN2CPP_USE_CURL` | `ON` | The one option the file **overrides rather than obeys**, and only on Emscripten (H8). |
| `DN2CPP_USE_ZLIB` / `_BROTLI` / `_HIGHWAY` | `ON` | Vendored; Highway is header-only and adds no target. |
| `DN2CPP_HIDDEN_VISIBILITY` | `ON` | Load-bearing far beyond binary size on any target where an export is a DCE root (H9.2). |
| `DN2CPP_DEAD_STRIP` / `DN2CPP_STRIP` | `ON` | Each has three per-linker flavours; a new linker needs an arm in both. |
| `DN2CPP_MAX_STACK_FRAME` | `4096` | The runtime's per-function stack-frame ceiling, as `-Werror=frame-larger-than`. Applied to the runtime's own targets, never to the generated app (whose frames are a function of the input program) and never to vendored third-party ones. Raise it only as a decision about the smallest thread stack dn2cpp intends to run on; MSVC has no equivalent flag, so the ceiling is absent there. |
| `DN2CPP_PAL_REFERENCE` | `OFF` | Swaps the host's PAL implementation for `runtime/core/platform/reference/`. A porting-contract assertion, not a shipping configuration — set only by `gates/build-and-run-pal-reference.sh`. |

One asymmetry that is invisible from the file: `DN2CPP_APP_LINK_FLAGS` is declared,
but **`DN2CPP_APP_LINK_LIBS` is not** — it exists only as a `-D` the gates pass, and
it must ride `LINK_LIBRARIES` rather than `LINK_FLAGS` because link *options* land
before the object files on the link line.

### 3.3 The edits a new target needs

In `runtime/CMakeLists.txt`, in rough order of "silently wrong if skipped":

1. **The PAL source selection.** Three arms — `EMSCRIPTEN` / `WIN32` / `else()` —
   and the `else()` is a **fallthrough to POSIX, not an error**. A target that is
   neither and is not POSIX gets `platform/posix/*.cpp` and fails at compile time on
   missing headers with nothing naming the cause. A new non-POSIX target also needs
   a new `runtime/core/platform/<os>/` directory.
2. **The `dn2cpp_gc` defines, in two places that must agree**: the per-target arm on
   the `dn2cpp_gc` target, and the `GC_THREADS` PUBLIC define on `dn2cpp_runtime`.
   Disagree and consumers' `<gc.h>` describes a different collector than was built.
3. **The threads decision** — the `else()` that does
   `find_package(Threads REQUIRED)`. A threadless target must take the other path,
   and must then also force `DN2CPP_USE_CURL` off: curl's own CMakeLists appends
   `Threads::Threads` to the static library's link interface, that name is written
   into the exported `dn2cpp-targets.cmake`, and the *per-app* configure then dies
   at generate time on an unresolvable CMake target with nothing in the message
   naming your platform.
4. **Platform link libraries** (`-framework CoreFoundation Foundation objc` on
   Apple, `ws2_32` on Windows), `PUBLIC` so the app's generated TUs inherit them.
5. **Dead-strip and strip flavours**, plus the `-ffunction-sections -fdata-sections`
   prerequisite, plus any code-signing pairing (on Apple, `strip` invalidates the
   arm64 ad-hoc signature, so the re-sign is a POST_BUILD step *after* the strip).
6. **The app shapes** — executable, GDExtension shared library, plain shared
   library, mono-module — if the target's shared-library model differs (side module,
   bundle, `.aar`), including any `PREFIX`/`SUFFIX` pinning.

Outside `runtime/`, `gates/_common.sh` needs a matching set: a build directory for
the new axis, a `_<target>_toolchain_args()` emitting one cache entry per line, an
arm in the runtime-configure dispatch **and** the identical arm in `cmake_build_app`
(the two configures must pass the same toolchain args or the app links against a
runtime built for something else), and the new axis appended to the guard that
excludes `-DCMAKE_CXX_COMPILER` on cross axes.

---

## 4. The gate side

A new target gets gates, and the gates have to be honest about what they did not
run. `AGENTS.md`'s rules apply unchanged; what a porter needs is the mapping from
"my toolchain is not installed" to the right primitive.

| situation | primitive | mode behaviour |
|---|---|---|
| the toolchain/SDK/device is absent | `gate_skip REASON` (exit 77) | counted separately, named with its reason; **a failure under `DN2CPP_REQUIRE_ALL=1`** |
| the gate ran and asserted, but one optional section could not | `gate_partial REASON` | passes, prints `PARTIAL:`, cached as partial; a failure under `REQUIRE_ALL` |
| a section is barred by a **structural, permanent** limit whose surface another named run asserts for real | `gate_expected_partial REASON` | passes in every mode, printed and listed in the summary |
| a maintenance condition, no coverage implication | `gate_warn REASON` | passes in every mode; no cache latch |

Four things a new target's gates get wrong most often:

- **`echo SKIP; exit 0` is banned and the runner enforces it** — it fails any gate
  that exits 0 having printed a line beginning with `SKIP`. That pattern is how a
  whole lane once shipped red on `main` inside an all-green suite.
- **A skip's stated remedy must actually work on the host it is printed to.** A skip
  whose remedy does not work reads as "this box is under-provisioned" when it means
  "this lane has no port to your OS".
- **`gate_expected_partial` is not the tool for "the SDK is not installed here".**
  That is transient and stays a `gate_skip`. The bar is that *no configuration of
  either side* can close the hole, and that bar is easy to overstate (H5.2).
- **Prefer widening an existing themed bucket to creating a new gate.** A genuinely
  new target is one of the few legitimate reasons for a new bucket.

---

## 5. Known porting hazards

Grouped by the *mistake*, not by the platform: the platform-specific form is the
accident and the class recurs.

### H1. The louder targets hide the quieter ones

- **A dead-stripped tail is never missed.** An executable link drops unreferenced
  code, so a missing PAL entry point can go unnoticed; in a wasm **side module** a
  missing symbol is not a link error at all — it becomes an import, `dlopen` and
  `dlsym` succeed, and the first *call* throws `TypeError: resolved is not a
  function`. Assert the import surface statically.
- **An axis that has only ever skipped has never run.** Acquire the toolchain
  before declaring an axis supported.
- **A missing runtime dependency names neither the library nor the import**, and
  cannot reproduce on a machine that can build the export. Linking the dynamic MSVC
  CRT is the example: set `CMAKE_MSVC_RUNTIME_LIBRARY` at *directory* scope so every
  object flips together and no link mixes CRT models.

Rule: **assume the target you can debug is masking the target you cannot.**

### H2. A libc is not its family

`__GLIBC__` is not "is Linux" and its absence is not "is BSD" — bionic defines
neither `__GLIBC__` nor the BSD macros, yet ships the GNU-style
`char* strerror_r(...)`. Test for the libc you mean, never for the one you assume
implies it.

### H3. An API-level floor is part of the target

- A symbol can be in the headers and absent from the sysroot: `getentropy` and
  `std::aligned_alloc` are bionic-only from API 28 while the project floor is
  `android-24`, so the `__ANDROID__` arms use `arc4random_buf` and
  `::posix_memalign` (whose blocks release with plain `free()`).
- The iOS deployment target is not a preference: it is the floor Apple's libc++ puts
  under the floating-point `std::to_chars` overloads the formatting core uses. Both
  floors live in `gates/_common.sh`.

### H4. Allocator families do not mix, and alignment has a floor

- **Every POSIX aligned allocator refuses alignment below `sizeof(void*)`** while
  `NativeMemory.AlignedAlloc(n, 1)` is legal in .NET, so the clamp lives in the
  generic path of `runtime/core/dn2cpp_gc.cpp`. The size must then round to the
  *effective* alignment, and the MSVC `_aligned_realloc` arm needs the same clamp as
  a correctness requirement — it must be handed the alignment `_aligned_malloc` used.
- **Mixing allocator families corrupts the heap.** MSVC has no `std::aligned_alloc`;
  `_aligned_malloc` takes `(size, alignment)` — the reversed order — and its blocks
  must go to `_aligned_free` / `_aligned_realloc`.

### H5. The narrow encoding is a per-OS transform, and so is the oracle's

- **`Encoding.Default` reports UTF-8 on every OS and that is not what marshalling
  does**: default Ansi P/Invoke marshalling is the host's narrow encoding — `CP_ACP`
  with best-fit substitution on Windows, UTF-8 on POSIX — so a UTF-8 reading of
  `new string(sbyte*)` corrupts non-ASCII text with no diagnostic. Hence three ANSI
  seam functions, and the Windows arm's save/restore of `GetLastError()` around
  `MultiByteToWideChar`.
- **The oracle has a host too.** An ANSI code page comes from a process image's
  Win32 manifest, so an oracle can often be moved when the subject cannot — which is
  why "structural" is a high bar. Read the ACP from the registry, not from `chcp`,
  which reports the *console* code page.
- **The host's locale leaks into the oracle generally**: any gate diffing against
  real .NET must pin `CultureInfo.InvariantCulture`, since only the oracle has a
  culture (`gates/verify-culture-invariance.sh` is the proof harness). Platforms
  disagree in their CLDR data, hence the per-OS branches in
  `runtime/core/intrinsics/dn2cpp_system_globalization.cpp`.

### H6. The CoreLib flavour is part of the target

- **Cross-compiling with the host's CoreLib links the host's OS libraries** — an
  Android or Web build from a Windows host dies on `-lkernel32` or a missing
  `ole32`, naming a missing library rather than the real cause.
  `locate_corelib_cross_posix` in `gates/_common.sh` resolves a POSIX CoreLib, and
  `dist/package-toolchain.sh` stages a second reference closure, where an absent
  runtime pack is a hard error rather than a skip.
- **A different CoreLib flavour P/Invokes a different native module** (the Linux
  cryptography assembly wants `CryptoNative_*` where macOS wants the Apple module),
  so before writing C++ for a new OS, diff the target RID's CoreLib P/Invoke surface
  against the one you already serve. The pattern to copy is
  `runtime/core/intrinsics/dn2cpp_openssl_crypto_digest.cpp` beside
  `runtime/core/intrinsics/dn2cpp_apple_crypto_digest.cpp`, both over the shared
  cores in `runtime/core/intrinsics/dn2cpp_hash_cores.h` with no vendor library
  linked.
- **A per-directory glob is not a per-OS filter**: a file under `platform/posix/`
  compiles on every POSIX target, so OS-specific content either sits inside an
  `#if`, or is written to be structurally self-guarding — one symbol forwarding to a
  portable entry point, forward-declared rather than including an SDK header — so it
  merely dead-strips elsewhere.
- **On a Windows host `wasm_corelib_diff_gate` transpiles the linux-x64 runtime
  pack's CoreLib**, resolved through `locate_corelib_cross_posix`, so the axis
  sees the POSIX `SystemNative_*` surface the wasm PAL implements instead of the
  host's kernel32 imports.

### H7. The collector's assumptions are per-target, and they fail silently

Every item here produces a wrong answer or a crash, never a build error.

- **A stale thread-table entry aborts the collector — on Linux only**, because
  `pthread_stop_world` signals an entry that never acks while `darwin_stop_world`
  enumerates live threads from the kernel. Unregister with a `thread_local` RAII
  guard.
- **The incremental collector mprotects the heap, and the kernel does not trap into
  its handler**, so `::pread` into a managed `byte[]` returns `EFAULT` and surfaces
  as an `IOException`; kernel writes are bounced through memory the collector never
  touches. A user `[DllImport]` with an `[Out] byte[]` filled by a syscall keeps the
  exposure and cannot be fixed from the runtime.
- **A target with no page-protection VDB must force incremental mode off in the
  runtime, not only in the build** (`runtime/core/dn2cpp_gc.cpp`).
- **A target whose static data segment the collector cannot see needs explicit
  roots**: such globals go into a named `dn2cpp_roots` section registered at init via
  the linker-synthesized `__start_`/`__stop_` symbols, strong references on purpose
  so losing the section is a link error rather than silent premature collection.
- **Conservative scanning needs the pointers to be in memory.** On wasm, Binaryen's
  SpillPointers pass spills pointer-typed locals but finds `__stack_pointer` by name,
  and emcc strips the name section before invoking wasm-opt — so it needs `-g2`.
- **A threadless target may not define `GC_THREADS`** (bdwgc `#error`s under it on
  Emscripten), the define must stay off the PUBLIC interface too, and finalizers then
  drain manually.

If none of this can be made to work, `-DDN2CPP_USE_GC=OFF` (`DN2CPP_NO_GC=1` from
the gates) drops to the calloc fallback — a supported configuration, not a debugging
aid.

### H8. The build leaks the *host* into the artifact

The vendored TLS stack is the worst offender, and every trap is written out at its
site in `runtime/CMakeLists.txt`: `CURL_ENABLE_SSL=ON` silently re-defaults
`CURL_USE_OPENSSL=ON` and would link the build host's OpenSSL; `CURL_CA_BUNDLE`
defaults to `"auto"` and bakes a host path into curl's config header (the tree pins
`none` and passes the vendored Mozilla bundle as `CURLOPT_CAINFO_BLOB`, a blob being
what works inside an APK, an `.ipa` and a wasm module alike);
`CURL_ZLIB`/`CURL_BROTLI`/`CURL_ZSTD` default to `AUTO` and find system copies;
curl's `check_function_exists(mbedtls_des_crypt_ecb)` kills the configure, so the
result is pre-seeded; and mbedTLS must inherit hidden visibility, or a GDExtension
loaded into a host process with its own mbedTLS lets the loader cross-bind
`mbedtls_*`.

The Emscripten carve-out is the model for a target that cannot have a transport:
`DN2CPP_USE_CURL` is forced off with a `set()` **without `CACHE`** (shadowing the
cache entry for every scope that reads it, including the per-app configure), while
the `dn2cpp_http2_call_*` symbols stay **defined** by the Emscripten fallback arm of
`runtime/core/intrinsics/dn2cpp_http2_stream.cpp` and answer every call with a
sentence naming the platform, which becomes a catchable `HttpRequestException`. Two
meanings are kept apart on purpose: "the target cannot have it" degrades at run time
so the diagnosis reaches the player, while `-DDN2CPP_USE_CURL=OFF` fails loudly at
link because somebody declined it and can turn it back on.

### H9. The toolchain fails quietly

- **`emcc -shared` implies `SIDE_MODULE=1`, which implies `LINKABLE=1`** — DCE off,
  `-sEXPORTED_FUNCTIONS` a no-op, every unresolved symbol turned into an import
  rather than a link error. Always `SIDE_MODULE=2` with an explicit export list, and
  assert `__wasm_call_ctors` (deliberately not exported), without which every C++
  static constructor silently never runs.
- **Visibility is a size *and* a feasibility question**: `wasm-ld` exports every
  non-hidden global of a `-shared` link, every export is a DCE root, and
  `__wasm_apply_data_relocs` is one function against V8's 7,654,321-byte
  single-function ceiling.
- **A build system can degrade a shared library to a static archive without saying
  so** (Emscripten's `Platform/Emscripten.cmake` sets
  `TARGET_SUPPORTS_SHARED_LIBS=FALSE` on some CMake bands), so the guard in
  `runtime/CMakeLists.txt` reads the property rather than re-deriving the band.
- **A flag the toolchain silently ignores is one nobody notices is misplaced**:
  `-sEXIT_RUNTIME`, `-sSTACK_SIZE` and `-sALLOW_MEMORY_GROWTH` belong to the *main*
  module, and emcc ignores rather than rejects them on a side module.
- **Shell and path handling on a Windows host**: MSYS converts a path handed to a
  native program as an argument and nothing else, so a path buried inside a longer
  argument arrives verbatim — route such sites through `native_path` in
  `gates/_common.sh`. MSYS's `ln -s` copies rather than links, and Windows `dotnet`
  emits CRLF, so a `read -r` keeps the `\r` and an obviously-correct match never
  fires.
- **A swallowed diagnostic is worse than any of them**: ninja funnels each edge's
  diagnostics to its own stdout, so a wrapper redirecting to `/dev/null` makes every
  compile and link failure silent under `set -e`.

### H10. The device is not the desktop

Generalisable to any target you cannot drive from a shell:

- **A packaged mobile app has no CLI**, so Godot's `--quit-after N` cannot reach it;
  the mobile gates ship a scene script that quits after a fixed frame count on the
  `ios`/`android` feature tags. Never end a Godot run with a signal — the logger's
  buffer is lost and a healthy run looks like a silent stall.
- **Console output may reach neither the launch pty nor the system log** (`GD.Print`
  on iOS reaches neither), so print markers through the runtime's plain-stdout
  `Console` path.
- **Launch is load-sensitive**: under a saturated machine an OS launch watchdog kills
  the app before the engine finishes starting while the same build passes solo. Retry
  with a bound, stopping as soon as the init marker appears.
- **The vendor's own artifacts can be the blocker** — Godot's official iOS export
  template ships an x86_64-only simulator slice against arm64-only modern runtimes,
  which `gates/setup-ios-sim-template.sh` repairs by rebuilding from source.
- **Export toolchains refuse for reasons that are not yours**: the Android export
  refusal fires in the editor's `can_export` before any dn2cpp step runs, keyed on the
  editor's own JDK setting rather than on `PATH` — and a gate that discards its export
  log reports only "produced no APK" while the engine had named the remedy.

---

## 6. Linux, specifically

Linux is a supported target with no CMake branch of its own: it takes the POSIX
PAL, `find_package(Threads)`, `--gc-sections` and `strip` through the `else()`
arms, and the only mention of it in `runtime/CMakeLists.txt` is prose.

**What is Linux-specific in the runtime**, all cited in §5: the OpenSSL crypto PAL
(H6.2), the `membarrier(2)` fast path with its mprotect-IPI fallback,
`malloc_usable_size` against macOS's `malloc_size`, `/proc/self/exe` against
`_NSGetExecutablePath`, the `st_ctim`/`st_ctimespec` field-name split, the CLDR
divergences (H5.3), and the stale-thread-table abort (H7). Distro libc churn is a
standing maintenance cost of the target, not a one-time port cost — a glibc release
that stopped pulling `clock_gettime` in transitively through `<sys/time.h>` broke
the POSIX PAL's compile until an explicit `<ctime>` was added.

**CI is not the merge gate and cannot be made into one.**
The hosted smoke workflows (`.github/workflows/linux-smoke.yml`,
`.github/workflows/windows-smoke.yml`, `.github/workflows/macos-smoke.yml`) run
the console lane end to end and the `SKIP_GODOT=1` suite in normal mode,
fetching a public Godot build only to dump `extension_api.json` (the runner's
pre-build needs it even when the Godot phase never runs, because it compiles the
`GodotSharpShim`). Four lanes cannot run on a hosted runner, and none of the four
is a provisioning problem somebody could solve: the editor-export gates need a
scons-built dn2cpp *fork* of the Godot editor, which is hours of build a hosted
runner discards every time; the CRI ADX LE gates need a proprietary package and
a private sample repository, where the licence is the obstacle rather than the
automation; the godot-dotnet gates need a mono editor at a pinned commit; and the
iOS gates need Xcode plus a locally repaired export template. `DN2CPP_REQUIRE_ALL=1`
turns every one of those skips into a failure, so a hosted runner is red under it by
construction. What CI buys is the one thing no merge-gate run has covered — every
merge-gate run to date has been on macOS — namely that the tree still builds and
transpiles on Linux. Passing it is not permission to merge.

---

## 7. A checklist for a new target

Ordered so that the things which fail *silently* come before the things which fail
loudly.

1. **Decide which CoreLib flavour the target's programs will be transpiled from**,
   and diff its P/Invoke module set against what the runtime already answers (§2.2,
   H6). This decides the size of the whole port.
2. **Create `runtime/core/platform/<os>/` and implement the whole PAL seam.**
   Degrade only the three functions §2.1 names, and only to their documented
   sentinels.
3. **Add the PAL selection arm in `runtime/CMakeLists.txt`** — the `else()` is a
   fallthrough to POSIX, not an error (§3.3).
4. **Decide the GC story**: threads, incremental mode, static-data roots, stack
   scanning. Get it wrong and nothing fails to build (H7). `-DDN2CPP_USE_GC=OFF` is
   the supported retreat.
5. **Decide the transport story**: if the target has no sockets, copy the Emscripten
   shape in *both* files — force the option off without `CACHE`, and define the HTTP
   surface so it answers rather than fails to link (H8).
6. **Wire `gates/_common.sh`**: build dir, toolchain args, and the *same* args in
   both the runtime configure and `cmake_build_app` (§3.3).
7. **Decide the CPU-feature story** (§2.5): a detection arm in
   `runtime/core/intrinsics/dn2cpp_cpu_features.cpp` for the target's OS, or the
   documented all-false answer. Prove it with `DN2CPP_CPU_FEATURES_DIAG=1`, whose
   one stderr line names the detected set, before trusting any `IsSupported`.
8. **Write the gate, and make its skip honest**: `gate_skip` with a reason whose
   stated remedy actually works on the host that will read it (§4).
9. **Then install the toolchain and run it.** An axis that has only ever skipped has
   never run (H1.2).

---

## 8. The console-port contract

A `platform/<console>/` directory plus a toolchain file should port with zero core
changes. Four in-repo things make that possible:

| the piece | where it lives | what asserts it |
|---|---|---|
| the **stub** | `runtime/core/platform/reference/` — a complete implementation of the seam in portable C++17 that names no operating system. Copy the directory; do not start from §2.1 and an empty file. | `gates/build-and-run-pal-reference.sh` §3–4: the whole runtime builds with the host PAL swapped out, and a transpiled program runs on it and matches real .NET |
| the **calloc GC path** | `-DDN2CPP_USE_GC=OFF` | `gates/build-and-run-pal-reference.sh` §6, the suite's only build of that configuration |
| the **stdout hook** | `dn2cpp_pal_console_write` / `dn2cpp_pal_console_flush` in the seam. Every console byte the runtime emits leaves through them, so a target with no stdout implements two functions and touches no core file. | `gates/build-and-run-pal-reference.sh` §5, which installs a sink through the reference target's hook and diffs the captured bytes against the exact text the console family emits |
| **stack→heap for large buffers** | `DN2CPP_MAX_STACK_FRAME` (default 4096) arms `-Werror=frame-larger-than` on the runtime's own targets; the functions that exceeded it are on the heap | the flag is a build error, and §3 of the same gate asserts the flag actually reached the compile line |

Three rules that fell out of building it:

- **A contract stated only in prose is not a contract.** The seam's membership is
  derivable from the header's markers and checked by a gate, because prose about it
  had already gone stale.
- **A stack-buffer audit cannot be done by reading source on one machine.** The same
  text costs ~2 KiB of frame on macOS and ~4 KiB on Linux where `PATH_MAX` differs.
  Measure with the compiler (`-Wframe-larger-than=`), per target, or do not claim to
  have measured.
- **An escape hatch nothing exercises is not an escape hatch**; it is an untested
  code path with a flag. `DN2CPP_NO_GC=1` reconfigures the *shared* runtime build
  dir, so it needs an axis of its own rather than an ad-hoc run.

**What a real console port still owns**, and why none of it could be done here:

- the `platform/<target>/` implementation and its toolchain file — the NDA-bound
  part, and the reason this guide stays generic;
- §2.2's question, the larger half of any non-POSIX port: *which CoreLib flavour
  will the target's programs be transpiled from, and what does it P/Invoke?* The
  reference target answers §2.1's seam and deliberately keeps the host's
  `SystemNative_*` and mmap TUs, so a green there says nothing about this;
- the GC decision (H7) — stack bounds, thread registration, static-data roots;
- `dn2cpp_scrub_stack`'s 16 KiB band, the tree's one deliberate large frame. On a
  target whose thread stacks are smaller than that, this call *is* the overflow. It
  is exempted from the ceiling at its site, with the trade written out there.
