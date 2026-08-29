# gen-isa-map

Generates the hardware-intrinsics family contract from the public surface of
`System.Private.CoreLib`: which `System.Runtime.Intrinsics.{X86,Arm,Wasm}` families
exist, the `IsSupported` token of each, the C++ helper every method lowers to, and
whether a family is lowered at all. Everything it writes is checked in; the generator
is a manual aid, never a build step.

## Invocation

Run from the repository root:

```bash
dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll> --check
```

`--check` regenerates in memory and exits non-zero listing every checked-in output that
differs, is missing, or is stale; it compares with line endings read as LF, so a CRLF
checkout does not fail it. `--root <dir>` points at a repository other than the
current directory. The CoreLib is read as metadata only, so either the x64 or the arm64
flavour of the pinned version gives the same outputs.

Outputs:

| path | content |
|------|---------|
| `runtime/core/isa/dn2cpp_isa_tokens.g.h` | one `IsSupported` token per family |
| `runtime/core/isa/dn2cpp_isa_families.g.h` | includes every generated family header |
| `runtime/core/isa/dn2cpp_isa_manifest.txt` | every lowered helper name, sorted |
| `runtime/core/isa/<arch>/dn2cpp_isa_<arch>_<family>.h` | helpers of one family with map rows |
| `src/Dn2Cpp.Transpiler/CoreIntrinsics.PlatformIsa.g.cs` | the family table the transpiler reads |
| `samples/dotnet/PlatformIsaProbe/Exercises.g.cs` | the probe's exercise of every generated-lowered family |

Inputs: `families.csv` (the family set, feature bits and landing order),
`runtime/core/dn2cpp_cpu_features.h` (the feature bits and what each implies) and
`map/<arch>/<family>.map` or `map/<arch>/<family>/*.map` (the lowering of each method). The
generator fails when the csv's family set differs from CoreLib's, naming both differences,
and when the csv names a bit the header does not define.

## Naming contract

- **Qualified name**: `Namespace.Type`, nested types joined by `+`
  (`System.Runtime.Intrinsics.X86.Lzcnt+X64`).
- **Token**: `DN2CPP_ISA_` + qualified name without the `System.Runtime.Intrinsics.`
  prefix, `.` and `+` as `_`, CLR casing kept (`DN2CPP_ISA_X86_Lzcnt_X64`,
  `DN2CPP_ISA_Arm_AdvSimd_Arm64`, `DN2CPP_ISA_Wasm_PackedSimd`).
- **Display name** (gates, samples): qualified name without the prefix, `+` as `.`
  (`X86.Lzcnt.X64`).
- **Helper**: `dn2cpp_isa_<arch>_<typepath>_<method>[_<argsig>]`, all lowercase; `<arch>` is
  `x86`, `arm` or `wasm`; `<typepath>` is the type path below the arch namespace with `+`
  as `_` (`sse2_x64`, `avx512f_vl`, `advsimd_arm64`); `<argsig>` joins one code per
  parameter with `_` and is omitted for a parameterless method. Return types are never
  encoded.

Argument codes:

| C# type | code |
|---------|------|
| `sbyte` `byte` `short` `ushort` `int` `uint` `long` `ulong` | `i8` `u8` `i16` `u16` `i32` `u32` `i64` `u64` |
| `float` `double` `nint` `nuint` `bool` `char` | `f32` `f64` `nint` `nuint` `bool` `char` |
| `Vector64<T>` … `Vector512<T>` | `v64` … `v512` followed by the element code (`v128i32`) |
| `T*` / `void*` | `p` + code (`pu8`) / `pvoid` |
| `ref`/`out`/`in T` | `r` + code |
| enum | code of the underlying type (`FloatComparisonMode` is `u8`) |
| homogeneous `(VectorN<T>, …)` parameter | `t` + arity + item code (`t2v64i8`) |

Anything else fails the run, naming the member: the surface is finite and a new shape is
a contract change.

Examples: `Sse2.Add(Vector128<int>, Vector128<int>)` is
`dn2cpp_isa_x86_sse2_add_v128i32_v128i32`; `Lzcnt.X64.LeadingZeroCount(ulong)` is
`dn2cpp_isa_x86_lzcnt_x64_leadingzerocount_u64`; `X86Base.Pause()` is
`dn2cpp_isa_x86_x86base_pause`.

C++ helper signature: vector parameters `const Dn2CppVector<N>&`, vector returns by value,
scalars and pointers by value, `ref` parameters as pointers; a `ValueTuple` return becomes
a `void` return with trailing out-pointers `item1, item2, …` in tuple order. A tuple
parameter is passed as its items, `a<k>_1, a<k>_2, …`.

## Map grammar

`map/<arch>/<family>.map`, named by the helper type path (`map/x86/sse2_x64.map`), or a
directory `map/<arch>/<family>/` of `*.map` files read in ordinal name order
(`map/arm/advsimd/arithmetic.map`, `…/shift.map`), so a large family is written by topic.
A method with rows in two files is an error, and `target =` may appear in any file of the
directory as long as every occurrence agrees. A family with no feature bits (Sve, Sve2) may
not have a map.

```
# comment
target = sse4.2
Method(argcodes) = <C expression using $0, $1, ...>
Method(v128{T},v128{T}) = _mm_add_{epi}($0,$1) for T in i8,u8,i16
Add(v{W}{T},v{W}{T}) = vadd{q}_{neon}($0, $1) for W in 64,128 for T in i8,u8,i16,u16,i32,u32,f32
ShiftRightLogical(v{W}{T},u8) @imm[1..{bits}] = vshr{q}_n_{uT}($0:{uT}, $1) for W in 64,128 for T in i8,u8
Extract(v{W}{T},u8) @imm[0..{N}) = vget{q}_lane_{neon}($0, $1) for W in 64,128 for T in i8,u8
InsertSelectedScalar(v128{T},u8,v64{T},u8) @imm$1[0..{N128}) @imm$3[0..{N64}) = vcopyq_lane_{neon}($0, $1, $2, $3) for T in i8,u8
Store(p{T},t2v64{T}) = vst1_{neon}_x2($0, $1.*) for T in i8,u8
Load2xVector64(p{T}) = dn2cpp_isa_scatter(vld1_{neon}_x2($0), &$r*) for T in i8,u8
Method(v128f32,u8) @imm8 = _mm_shuffle_ps($0,$0,$1)
Method(v128f32,v128f32) @target("sse4.1") @throws = ...
```

- An exact row names the method and its argument codes exactly as in the helper name.
- A pattern row repeats for every width after `for W in` (64, 128, 256, 512) and every
  element code after `for T in`; the placeholders are replaced in the codes, the annotations
  and the expression alike. A row with element placeholders needs the `T` clause and one with
  width placeholders the `W` clause, and a clause nothing uses is an error. The placeholders:

  | placeholder | reads | spelling |
  |-------------|-------|----------|
  | `{T}` | T | the element code (`i8`) |
  | `{W}` | W | the width (`128`) |
  | `{q}` | W | `` for 64, `q` for wider: the NEON quad-register suffix |
  | `{mm}` `{m}` | W | the SSE/AVX/AVX-512 intrinsic prefix and register type at that width (`_mm` / `_mm256` / `_mm512`, `__m128` / `__m256` / `__m512`; `{m}i` and `{m}d` are the integer and double types) |
  | `{N}` | W, T | the lane count (`v128i8` → `16`) |
  | `{N64}` `{N128}` | T | the lane count at that width |
  | `{bits}` | T | the element width in bits |
  | `{neon}` | T | the ACLE type suffix (`s8`, `u8`, `f32`) |
  | `{ntype}` | W, T | the NEON vector type (`int8x16_t`) |
  | `{uT}` `{sT}` | T | the same-width unsigned / signed element code (`f32` → `u32` / `i32`) |
  | `{wT}` `{nT}` `{unT}` | T | the element of twice / half the width, and the unsigned one of half the width (`i16` → `i32` / `i8` / `u8`) |
  | `{wneon}` `{nneon}` | T | the ACLE suffix of the twice- / half-width element |
  | `{hbits}` | T | half the element width in bits: a narrowing shift's range |
  | `{bhsd}` | T | the scalar-register letter of a one-lane intrinsic (`vqrdmlahh_s16`) |
  | `{epi}` `{epu}` `{ep}` | T | the SSE integer suffix in the signed spelling, the unsigned spelling, and the element's own (`_mm_add_epi8` serves both byte overloads; `_mm_max_epu8` one; `_mm_cvtepi8_epi16` / `_mm_cvtepu8_epi16` each their own) |
  | `{ps\|pd}` `{ss\|sd}` | T | the SSE packed / scalar float suffix |
  | `{lane}` | T | the wasm lane shape in the element's signedness (`u8x16`; a native-integer lane is `i32x4` / `u32x4`) |
  | `{slane}` `{ulane}` | T | the signed / unsigned wasm lane shape whatever the element's (`wasm_i8x16_add` serves both `sbyte` and `byte`) |
  | `{wlane}` `{swlane}` `{uwlane}` | T | the twice-width lane shape in the element's, the signed and the unsigned spelling (`i8` → `i16x8`; `u8` → `u16x8` / `i16x8` / `u16x8`) |
  | `{cs}` `{scs}` `{ucs}` | T | the C# element type and its same-width signed / unsigned type (`byte` / `sbyte` / `byte`), for the casts a `@ref` expression needs |
  | `{ncs}` | T | the C# type of the element of half the width (`int` → `short`), the zero operand a `@ref` narrows with |

  A missing table entry (a float asked for `{epi}`) is an error, never a guess. `nint` and
  `nuint` are vector elements only in the wasm family: their lanes are spelled as 32 bits,
  which holds on wasm32 alone, and the generator refuses such a vector elsewhere.
- `$k` is parameter `k`; a vector arrives converted to the native register type through
  `dn2cpp_isa_bits<...>`, and `$k:u8` converts it to the same-width vector of another element
  instead (a bitwise operation on floats, a table index read as bytes). A vector result is
  wrapped back through `dn2cpp_isa_vec<...>`, whatever type the intrinsic returned.
  `$k.j` is item `j` of a tuple parameter and `$k.*` the whole tuple as the NEON
  multi-register aggregate (`int8x16x2_t`, parenthesized); `$r1`, `$r2`, … are the
  dereferenced out-pointers of a tuple return, `&$r1`, `&$r2`, … the out-pointers themselves
  and `&$r*` all of them in order (the argument list `dn2cpp_isa_scatter` takes).
- `@imm8` marks the last parameter as a byte immediate with every value valid;
  `@imm[lo..hi)` and `@imm[lo..hi]` (exclusive / inclusive top) give the encodable range —
  a lane index `[0..{N})`, a left shift `[0..{bits})`, a right shift `[1..{bits}]` — for a
  count that is a power of two up to 256. `@immwrap[0..{N})` instead accepts every byte and
  masks an instruction-encoded lane index to that power-of-two count. `@imm$k[...]` names
  parameter `k` instead of the last, and two such annotations make a two-immediate helper.
  `@imm{1,2,4,8}` lists the
  valid values instead (a gather scale), one switch case each, and is dispatched alone. The
  generated exercise normally uses the middle valid value; `@exerciseimm=0` selects another
  valid value when that midpoint has unstable oracle semantics, and `@exerciseimm$k=0` names
  one of two immediate parameters. The
  body becomes `DN2CPP_ISA_IMM8_SWITCH`, `DN2CPP_ISA_IMM_RANGE_SWITCH`,
  `DN2CPP_ISA_IMM_WRAP_SWITCH`, `DN2CPP_ISA_IMM_RANGE_SWITCH2` or the listed cases, and the
  expression names the
  constants `DN2CPP_IMM` and `DN2CPP_IMM2` (the `$k` of the immediate is rewritten). A value
  outside a constrained range or list throws ArgumentOutOfRangeException, the check .NET
  inserts for a non-constant immediate; `@immwrap` has no out-of-range byte. A
  `FloatRoundingMode` is such a list: .NET accepts 0 through 11
  and the JIT encodes the low two bits as the rounding control with exceptions suppressed, so
  the row lists those twelve values and the expression hands the intrinsic `($k & 3) | 8`, the
  only spelling the compiler's `_round` intrinsics accept. An immediate whose compiler intrinsic accepts fewer bits
  than .NET's byte (`_mm256_extractf128_ps` reads one) is masked in the expression
  (`DN2CPP_IMM & 1`), as the hardware ignores the rest. `@target("...")` overrides the
  family-level `target =`. `@opaque` passes non-immediate exercise operands through the
  probe's no-inline identity function so the .NET oracle executes the instruction instead
  of folding constant vectors. `@throws` documents a faulting intrinsic and changes nothing.
- `derive = X86.Avx512F.VL, X86.Avx512BW.VL, …` gives the family, for every method it shares
  with a listed family (same name and argument codes), that family's row under this file's
  `target =`; the first listed source wins where two define a method, an explicit row in the
  same file wins over every source, and a method no source covers is an error rather than an
  unmapped method, so a surface addition cannot hide behind the derivation. A source must be
  mapped directly (not derive in turn) and a source row's `@target` is not carried. .NET 10's
  `Avx10v1` is the AVX-512 VL and scalar surfaces under one token, which is what the directive
  is for: copied rows would drift when a source row is fixed, and `--check` could not see it.
- `@ref(<C# expression>)` names a portable `System.Runtime.Intrinsics` computation of the
  same result over `$0`, `$1`, … (`Add(v128{T},v128{T}) @ref(Vector128.Add($0, $1)) = …`;
  a pointer operand is dereferenced as `*$0`, an immediate is its literal). The generated
  exercise binds the operands to locals, evaluates both, and prints ` ref=OK` or
  ` ref=MISMATCH(<reference>)` after the helper's bytes. The portable layer is lowered by an
  independent implementation, so this is the second check on a family whose gate output is
  frozen because no host .NET can answer for it (PackedSimd). Give it to every method with
  a clean equivalent and to nothing else: a reference that re-derives the instruction from
  scratch would only restate the map row.
- A scalar result that .NET returns in lane 0 of a Vector64 (the `Across` reductions,
  SHA1H, the one-lane RDM forms) goes through `dn2cpp_isa_lane0<8>(...)`, which zeroes the
  other lanes as the register write does.
- A family whose only public static member is `IsSupported` (`X86Serialize.X64`) has no map
  file: it is covered vacuously and lowered with its enclosing family.

Every helper is emitted under the arch's `#if` — `DN2CPP_TARGET_X64`, `DN2CPP_TARGET_ARM64`,
or `DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)` — and any compiler-intrinsic
capability its family requires. The real body first tests the family's `IsSupported` token
through `dn2cpp_isa_require` (a call made while the token is false throws
PlatformNotSupportedException, as in .NET), and the `#else` is a `[[noreturn]]` stub calling
`dn2cpp_isa_not_lowered`, so a foreign-arch or compiler-unavailable dead arm in generated
code still compiles. The same compiler capability is part of the token, preserving the
contract that a true getter always has a real helper body. wasm SIMD is a property of the
whole module (the CMake option `DN2CPP_WASM_SIMD` adds `-msimd128` everywhere), so a default
wasm build compiles the stubs and its detector answers false. The macros come from
`runtime/core/dn2cpp_cpu_features.h` and `runtime/core/isa/dn2cpp_isa_common.h`.

## Lowered is derived

A family is lowered when every public static method it declares has a map row and every
family it implies is lowered too. The implied set comes from the feature bits: the families
whose bits lie in the implication closure of its own (`families.csv` gives the bits,
`runtime/core/dn2cpp_cpu_features.h`'s `DN2CPP_CPU_FEATURE_TABLE` parents give the
implications, and the generator reads both). That covers the enclosing type — a nested type
carries its enclosing type's bits, so `Sse3.X64` cannot be lowered before `Sse3`, nor `Sse3`
before `Sse3.X64` — and .NET's instruction-set implications, so `Dp` waits for `AdvSimd` and
`Popcnt` for `Sse42`: otherwise `Dp.IsSupported` could answer true while
`AdvSimd.IsSupported` is the constant 0, a state .NET never has, and BCL code guarded by `Dp`
would reach `AdvSimd` calls that throw. A family with no methods of its own is vacuously
covered, a family with a `derive` list is covered by its sources' rows, and a family with no
feature bits is never lowered. Nothing edits the `lowered`
column of `CoreIntrinsics.PlatformIsa.g.cs` by hand; the transpiler checks the
enclosing-before-nested rule against the table it reads, and
`gates/build-and-run-platform-isa-surface.sh` re-derives the implication rule from the two
sources.

## Generated exercises

`samples/dotnet/PlatformIsaProbe/Exercises.g.cs` exercises every lowered family that has no
hand-written exercise (the scalar families keep theirs in `X86Sections` / `ArmSections`):
each mapped method is called once with fixed inputs — vectors from a per-operand, per-lane
pattern, immediates at the middle of their range or list, pointers into a filled 64-byte-aligned
64-byte stack buffer (the aligned 512-bit loads and stores fault below that alignment, and no
load, store, masked or compressed form reaches past 64 bytes) whose bytes are printed
after a store, a gather's index lanes folded so every lane stays inside that buffer — and
prints `Method(argcodes)=<hex bytes>`; nested types run behind their own `IsSupported`. One
immediate method per family also supplies an out-of-contract range or list value. Those calls
live in a separate generated registry and run only under the probe's explicit
`--invalid-immediates` mode: a supported token must produce `ArgumentOutOfRangeException`,
while a false token must produce `PlatformNotSupportedException` before the range check. The
ordinary managed oracle receives only valid immediates and wrapping boundaries, because an
out-of-contract value is not a stable JIT execution contract. Real .NET is the oracle for the
other x86 and Arm output: the native gates diff it byte for byte, so no reference value is
computed. PackedSimd has no such oracle — no host .NET answers true for
it — so `gates/build-and-run-platform-isa-wasm.sh` diffs its output against the frozen
`gates/expected/platform-isa-wasm-simd.txt`, and every row with a `@ref` prints its
portable cross-check beside the bytes; the gate refuses a `MISMATCH`. The registration
(`Exercises.RegisterX86/Arm/Wasm`) is called from each `*Sections` class.

## Previewing a partial map

`--lowered-preview Arm.AdvSimd,Arm.AdvSimd.Arm64` treats the named families as covered: their
unmapped methods get throwing native stubs and every output carries a PREVIEW banner. Run it
on a scratch copy of the tree (never `--check`) to build and run the native gate against real
.NET while a family's map is still being written. On an Apple silicon Mac,
`tools/platform-isa-rosetta.sh --preview <families>` regenerates the working tree with the
preview, runs the probe as an x86-64 binary under Rosetta 2 (the `@ref` cross-checks stand in
for the absent x86 .NET oracle), and regenerates without the preview on exit.
