# PORTING.md — how DnBrotli is ported from C

Conventions for porting `third_party/brotli/` (v1.1.0, byte-identical vendor) to C#. The goal is
**line-level diffability against the C source**: a reviewer holding `c/dec/decode.c` next to
`Dec/DecoderEngine.cs` should walk them in lockstep. These rules bind every engine file; the thin
public layers (`Brotli.cs`, `Streams/`) use normal BCL idioms instead.

## Source mapping and naming

- One C# file per C translation unit, headed by a comment naming it
  (`// Port of c/dec/bit_reader.{h,c}.`).
- C functions become `internal static` methods keeping the **exact C name**
  (`BrotliWarmupBitReader`, `SafeReadBits`); C macros become
  `[MethodImpl(MethodImplOptions.AggressiveInlining)]` static methods of the same name; constants
  keep C names (`BROTLI_HUFFMAN_MAX_CODE_LENGTH`) as `const` members of the owning class.
- Struct fields keep the C spelling verbatim, snake_case and trailing underscores included
  (`ringbuffer_size`, `bit_pos_`) — diffability beats .editorconfig naming here, as in DnZlib's
  `z_stream`.
- Port the C comments that explain *why*; drop banner noise.

## State and memory

- Engine state lives in **unmanaged memory**, mirroring the C structs field-for-field as
  `unsafe struct`s allocated with `NativeMemory.AllocZeroed` and freed in the matching
  `*DestroyInstance` / cleanup path. Never a managed class, never a GC reference inside a native
  struct (the DnZlib rule); `BROTLI_ALLOC`/`BROTLI_FREE` map 1:1 to `NativeMemory.Alloc`/`Free`
  with identical lifetimes.
- Embedded C arrays of primitives become `fixed` buffers; embedded arrays of small structs (e.g.
  `HuffmanCode` scratch tables) become sibling `fixed` buffers of the flattened primitive where
  possible, else nested structs. No `[InlineArray]` (unverified under the transpiler).
- Struct layouts are .NET's packed layout under both runtimes — dn2cpp emits every value type's
  fields at their real storage width, so `sizeof(HuffmanCode)` is 4 (`HuffmanTree` 8,
  `CmdLutElement` 8) either way. `sizeof`-based byte math, `memcpy`-as-image blob decoding and
  16-bit stores through sub-word field pointers (`&cmd->cmd_prefix_` as `ushort*`) may assume it.
- `BROTLI_BOOL` is `int` (0/1) at every ABI-shaped surface; `bool` only for genuinely local logic.
  `size_t` and `brotli_reg_t` are `nuint` (64-bit on every dn2cpp target); pointer arithmetic
  stays pointer arithmetic.

## Control flow

`decode.c`-style resumable state machines keep their `switch`/`goto` structure: C fallthrough
becomes `goto case`, `goto saveStateAndReturn` becomes a C# label. Do not "clean up" the state
machine, and port loops and branch shapes as-is even where LINQ or a helper would be shorter —
behavior-affecting cleverness is a porting bug.

## Data tables

- Big binary tables (≥ a few KiB) are generated `.g.cs` files: byte-element `ReadOnlySpan<byte>`
  collection-expression properties (RVA static data), multi-byte values stored **little-endian**
  and decoded once at static init into `NativeMemory` for pointer-stable hot-path access
  (`BinaryPrimitives.ReadUInt16LittleEndian` etc.). `StaticDataTests` asserts a SHA-256 per blob.
- Small tables (≤ ~2 KiB) are hand-inlined next to their users, matching the C initializer layout.

## Platform / intrinsics

- `platform.h` load/store macros → `Unsafe.ReadUnaligned<T>` / `Unsafe.WriteUnaligned<T>`
  (little-endian is assumed and asserted at init; every dn2cpp target is LE).
  `BROTLI_TZCNT64`/`Log2Floor` → `System.Numerics.BitOperations`.
- SIMD policy (the DnZlib rule): architecture-generic `Vector128<T>` only, every vector path gated
  on `Vector128.IsHardwareAccelerated` with an **independently correct** scalar fallback — under
  dn2cpp's default backend the flag is false and only the scalar path runs. The decoder uses no
  SIMD at all; copies go through `Buffer.MemoryCopy` / `Unsafe.CopyBlockUnaligned`, never a
  hand-rolled vector overlap copy.
- No `double` in the decoder. Encoder cost models use `double` exactly as the C does — expression
  shapes must match the C source so FP results are stable.

## Determinism discipline

Compressed output is never compared or printed byte-wise against native brotli in gates or samples
(implementation-defined); round-trip facts only. In-repo xUnit tests MAY assert sizes and golden
hashes of DnBrotli's own output — they pin DnBrotli's determinism, not native equivalence.
