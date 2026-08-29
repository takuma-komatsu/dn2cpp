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

Inputs: `families.csv` (the family set, feature bits and landing order),
`runtime/core/dn2cpp_cpu_features.h` (the feature bits and what each implies) and
`map/<arch>/<family>.map` (the lowering of each method). The generator fails when the csv's
family set differs from CoreLib's, naming both differences, and when the csv names a bit
the header does not define.

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

`map/<arch>/<family>.map`, one file per family, named by the helper type path
(`map/x86/sse2_x64.map`). A family with no feature bits (Sve, Sve2) may not have one.

```
# comment
target = sse4.2
Method(argcodes) = <C expression using $0, $1, ...>
Method(v128{T},v128{T}) = _mm_add_{epi}($0,$1) for T in i8,u8,i16
Method(v128f32,u8) @imm8 = _mm_shuffle_ps($0,$0,$1)
Method(v128f32,v128f32) @target("sse4.1") @throws = ...
```

- An exact row names the method and its argument codes exactly as in the helper name.
- A pattern row repeats for every element code after `for T in`; `{T}` is replaced in the
  codes and the expression, and `{epi}` (`i8`→`epi8`, …), `{ps|pd}` (`f32`→`ps`,
  `f64`→`pd`), `{neon}` (`i8`→`s8`, `u8`→`u8`, …) and `{lane}` (`i8`→`i8x16`, …) are
  spelled per element. A missing table entry is an error, never a guess.
- `$k` is parameter `k`; a vector arrives converted to the native register type through
  `dn2cpp_isa_bits<...>`, and a vector result is wrapped back through `dn2cpp_isa_vec<...>`.
  `$k.j` is item `j` of a tuple parameter; `$r1`, `$r2`, … are the dereferenced out-pointers
  of a tuple return and `&$r1`, `&$r2`, … the out-pointers themselves.
- `@imm8` and `@imm[0..N)` (N in 2, 4, 8, 16, 32, 64) mark the last parameter as an
  immediate; the body becomes `DN2CPP_ISA_IMM8_SWITCH` or `DN2CPP_ISA_IMM_SWITCH_N` and
  the expression names the constant `DN2CPP_IMM`. `@target("...")` overrides the
  family-level `target =`.
  `@throws` documents a faulting intrinsic and changes nothing.
- A family whose only public static member is `IsSupported` (`X86Serialize.X64`) has no map
  file: it is covered vacuously and lowered with its enclosing family.

Every helper is emitted under `#if DN2CPP_TARGET_<ARCH>` with its body, which first tests
the family's `IsSupported` token through `dn2cpp_isa_require` (a call made while the token
is false throws PlatformNotSupportedException, as in .NET), and under `#else` as a
`[[noreturn]]` stub calling `dn2cpp_isa_not_lowered`, so a foreign-arch dead arm in
generated code still compiles. The macros come from `runtime/core/isa/dn2cpp_isa_common.h`.

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
covered, and a family with no feature bits is never lowered. Nothing edits the `lowered`
column of `CoreIntrinsics.PlatformIsa.g.cs` by hand; the transpiler checks the
enclosing-before-nested rule against the table it reads, and
`gates/build-and-run-platform-isa-surface.sh` re-derives the implication rule from the two
sources.
