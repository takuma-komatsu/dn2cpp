# P/Invoke marshalling — the supported surface

The transpiler's `[DllImport]` lowering (`MethodCompiler.Newobj.cs`,
`EmitPInvokeCall`, whose shape gate is `CppTypes.PInvokeNativeType`) marshals the
shapes below. Each row is a feature of the low-level interop surface, and the
native side of every one of them is exercised by
`samples/native/dn2cpptest/dn2cpptest.c`.

**The last column attributes each row to a bucket and a section, never to a gate
script**: a gate name is what a fold retires, while the section name is what a fold
carries over. Every section named below is a `.cs` file of the one
`samples/dotnet/PInvokeNative` bucket, and **two** gates drive that same program:
`gates/build-and-run-pinvoke-native.sh` on the host (exact diff against real .NET as
the oracle, probe-confirmed where the Notes say so) and
`gates/build-and-run-pinvoke-wasm.sh` over the identical section set on the wasm32
axis. Fold or rename either gate and this column is still right.

**The attribution is itself checked.** `gates/build-and-run-doc-claims.sh` asserts
that the sections this file names and the sections the bucket holds are the same
set in both directions, that each one is driven by the bucket's `Program.cs` — a
section nothing calls would let a row attribute a claim to dead code — and that
every path cited here exists. What it cannot check is whether a row's *Notes* still
describe what the named section asserts: that is a reading. So the check buys one
thing precisely — a row can no longer point at a section that is gone — and reading
the code beside a row remains the only way to find a row that points at the right
section and says the wrong thing about it.

| Feature | Managed shape | Native shape | Notes | Asserted by (`PInvokeNative` section) |
|---|---|---|---|---|
| Marshalling envelope | blittable scalars (integer/float, IntPtr/UIntPtr, pointers, blittable-underlying enums) | precise native-ABI-width cast | The core envelope over system libc. | `PInvokeLibcSubset` (the libc arm, which binds no custom library), `PInvokeCustomLibSubset` |
| Non-system library linking | `[DllImport("mylib")]` | the library's `extern "C"` symbol | Only adds the linking path; marshalling is the same envelope. | `PInvokeCustomLibSubset` |
| Default/Ansi strings | `string` under default/Ansi/Auto CharSet | NUL-terminated buffer — host code page on Windows, UTF-8 on Unix | Managed→native in; a `string` return is decoded and its buffer freed (matching .NET's default marshaller). | `PInvokeStringSubset` |
| Unicode strings | `string` under `CharSet.Unicode` | NUL-terminated UTF-16 (`LPWStr`) | Same in/return contract as the Ansi row. | `PInvokeWideStringSubset` |
| Blittable arrays | `int[]`, `byte[]`, … | pointer to element 0 (pinned, no copy) | `[In]` reads and `[In,Out]` write-backs both hit the managed buffer; null array → null pointer. | `PInvokeArraySubset` |
| Blittable struct by value | a blittable value struct | passed/returned by value as its C++ layout | Sub-word (`byte`/`ushort`) fields keep their real storage width, and a single-pointer-field struct return flattens to one pointer register (the singleton-aggregate ABI). | `PInvokeStructSubset`, `PInvokeSubWordStructSubset`, `PInvokeSingleFieldStructSubset` |
| bool | `System.Boolean` | 4-byte Win32 `BOOL` | Any non-zero return normalizes to managed `true` (.NET's rule). `[MarshalAs(U1/I1)]` forces the 1-byte width. | `PInvokeBoolSubset`; the `U1`/`I1` widths in `PInvokeMarshalAsSubset` |
| SetLastError | `SetLastError = true` | errno (POSIX) / `GetLastError()` (Windows) | Captured immediately after the call into the per-thread slot `Marshal.GetLastWin32Error` / `GetLastPInvokeError` read. | `PInvokeLastErrorSubset`; the `GetLastPInvokeError` spelling also in `RuntimePrimSubset` |
| Scalar char | `System.Char` | 1 byte (default/Ansi: first UTF-8 byte; a 0x80–0xFF return decodes to U+FFFD) or 2 bytes (`CharSet.Unicode`: the raw UTF-16 code unit) | Chosen by the method CharSet. | `PInvokeCharSubset` |
| Unicode char arrays | `char[]` under `CharSet.Unicode` | the packed `char16_t` data pointer (blittable) | Rides the blittable-array row; write-backs visible (probe-confirmed). | `PInvokeWideCharArraySubset` |
| StringBuilder buffers | `System.Text.StringBuilder` | caller-allocated `[In,Out]` buffer holding the current content | Read back into the builder after the call. The native buffer is sized for `sb.Capacity`, so the section pins dn2cpp's `Capacity` model to real .NET too. | `PInvokeStringBuilderSubset` |
| Byref/[Out] strings | `out string` / `ref string` | `&tmp` (a `void*` slot the native writes an allocated string through) | Decoded back into the ByRef slot after the call; `out` seeds no input buffer, `ref` seeds the marshalled current value. | `PInvokeByRefStringSubset` |
| [MarshalAs] overrides + calling conventions | per-parameter/return `[MarshalAs]`: `LPStr`/`LPUTF8Str`/`LPWStr` on strings, `U1`/`I1` on bool, `LPArray` on blittable arrays | — | On the Unix target ABI every `CallingConvention` collapses to the one platform C convention (the x86 register/cleanup distinctions are 32-bit-Windows-only). | `PInvokeMarshalAsSubset`; the conventions in `PInvokeCallConvSubset` |
| Ansi char arrays | `char[]` under default/Ansi CharSet | NUL-terminated UTF-8 buffer (length diverges from the array's) | Encoded by copy; decoded back only when `[Out]`/`[In,Out]` — the default direction is `[In]`, as real .NET marshals it (probe-confirmed). | `PInvokeAnsiCharArraySubset` |
| Byref blittables | `ref`/`out`/`in` of a blittable type | address of the pinned managed storage (no copy) | Write-backs are automatically visible; no post-call decode. `ref bool`/`ref char` stay a carve-out (their marshalled width differs from the managed storage). | `PInvokeByRefBlittableSubset`; the blittable-`ref struct` shape in `PInvokeVariantByRefSubset` |
| String arrays | `string[]` | `char**` / `char16_t**` (per-slot NUL-terminated buffers; null element → null pointer) | Default direction `[In]`. | `PInvokeStringArraySubset` |
| String-array write-back | `string[]` marked `[In,Out]`/`[Out]` | same pointer array, slots re-read after the call | Only a slot the native REPLACED (pointer differs from the pre-call snapshot) is freed — native heap ownership, matching .NET's CoTaskMemFree-on-Unix rule. | `PInvokeStringArraySubset` |
| Blittable struct arrays | `Point[]`-style arrays of blittable structs | fresh native buffer, `length * sizeof` packed copy | BY COPY with direction semantics (probe-confirmed): copy in unless `[Out]`-only, copy back only when `[Out]`/`[In,Out]` — unlike primitive arrays, which pin. | `PInvokeStructArraySubset` |
| Explicit-layout (union) structs | `[StructLayout(LayoutKind.Explicit)]` with overlapping `[FieldOffset]` arms | the byte-identical C union | Blittable whenever every arm is (overlapping scalars, unmanaged pointers, value structs), so it crosses by value, by return and byref with no marshaller — the sections write one arm and read a *different* one across the boundary, so the exact diff pins the punning. An explicit-layout field without `[FieldOffset]` is a loud carve-out (`CppEmitter`). | `PInvokeExplicitUnionSubset`; nested inside a `ref struct` in `PInvokeVariantByRefSubset` |
| Non-blittable marshalable structs | a value struct with `string` / `bool` / nested-blittable fields | a per-struct native layout `tn_<Name>` (`CppTypes.MarshalStructName`) filled field by field | By value (`[In]`) or byref (`[In]`/`[In,Out]`/`[Out]`): a string field encodes to/from a NUL-terminated buffer under the struct's CharSet, a bool to/from the 4-byte BOOL. A byref string field's write-back frees a buffer only when the native REPLACED the `[In]` (GC) one. **Return position is a carve-out.** A field `[MarshalAs]` the marshaller does not implement (`ByValTStr`, a bool width override, a string encoding override, a width-MISMATCHED descriptor on a blittable-typed field) refuses at transpile naming the field (`CppTypes.StructFieldDescriptorSupported`, asked by `IsBlittableStruct` too). Only the exact no-ops (`Bool` on a bool, `Struct` on a struct, a width descriptor naming the field's own width) pass. | `PInvokeMarshalStructSubset` |
| Inline fixed-length array fields | `[MarshalAs(UnmanagedType.ByValArray, SizeConst = N)]` on a *struct field* | `<elem> f[N]` embedded inline in the native struct | Blittable scalar/enum elements only. Copy-in fills the inline slots (a null array zeroes them, a longer array truncates, a shorter one throws `ArgumentException`); copy-back allocates a fresh N-element managed array. Descriptor decoded by `Compilation.ReadByValArraySize`. | `PInvokeByValArraySubset` |
| Delegate callbacks | a delegate parameter | a native function pointer | Parked in the per-delegate-type, GC-rooted **thunk pool** and passed as the stable slot-thunk address (`CppEmitter.EmitMarshalFnPtrThunks`). That is the only delegate→fnptr path: the native may STORE the pointer and invoke it long after the call returns, which a synchronous thread-local slot could not serve. Pool is 8 live delegates per type; exhaustion traps loudly. | `PInvokeCallbackSubset`; the stored/deferred lifetime in `PInvokeStoredCallbackSubset` |
| Explicit delegate ⇄ pointer conversion | `Marshal.GetFunctionPointerForDelegate<T>` / `GetDelegateForFunctionPointer<T>` | the same pool slot's thunk address / a fresh delegate over a managed-ABI forwarder | One delegate instance always yields one pointer and a thunk pointer round-trips back to the *original* instance (.NET's identity guarantees); a raw native pointer rehydrates as a forwarder-backed delegate. | `MarshalFnPtrSubset` |
| Address-taken imports | a delegate over a `[DllImport]` method group, or `delegate*` via `&Method` | a synthesized function wrapping the extern call | The body is synthesized from the same `EmitPInvokeCall` lowering a direct call uses (`MethodCompiler.CompilePInvokeWrapper`), so marshalling is identical; an import outside this table's surface fails loudly when its address is taken. | `PInvokeFtnDelegateSubset` |
| Marshalled layout (`Marshal.SizeOf` / `OffsetOf`) | any type these are asked about | the unmanaged size and field offsets real .NET reports | Computed by `Compilation.MarshalLayout.cs`, a model SEPARATE from the C++ representation layout: a `bool` field is 4 bytes and aligned 4, a `char` follows the declaring type's CharSet (1 Ansi / 2 Unicode, and `Auto` is Ansi on POSIX), a `string` is a pointer, `ByValArray`/`ByValTStr` inline their extent, `Pack` caps each alignment, `Size` floors the total (and is never rounded up to the alignment; the shapes the C++ representation cannot express are refused at emission instead, asserted by the marshal-pinning gate's negative arm), and a `[StructLayout(Sequential)]` **class** answers with its base chain's fields first and no object header. A pointer-shaped extent is read at BOTH pointer widths and the pair folded by `PointerWidth.Model` into a `sizeof(void*)` expression, so the answer is right on wasm32 while the transpiler never asks what the target is (the contract is stated at the top of `Compilation.MarshalLayout.cs`). `System.DateTime` is the ONE auto-layout type .NET marshals as a field (8/8, a CoreCLR special case); every other auto-layout type is refused in both positions, as .NET refuses it. An **object** field is declined rather than refused — real .NET sizes a COM interface pointer on Windows and refuses on POSIX, and a verdict decided at transpile time cannot split by host — so it throws `PlatformNotSupportedException`, asserted by the reflect-types gate's marshal-verdict section. The emitted `tn_<Name>` carries unguarded `static_assert`s comparing `sizeof`/`offsetof` against the model, so the C++ compiler checks the arithmetic on every build — and the wasm link checks it at 32-bit width, per struct. `PtrToStructure`/`StructureToPtr` deliberately do NOT ride this — they copy bytes, so they still require representation == marshalled form. | `PInvokeMarshalLayoutSubset` |
| Reverse calls (`[UnmanagedCallersOnly]`) | `&Method` on an `[UnmanagedCallersOnly]` static — no delegate object, no thunk | the raw method address, invoked through a plain C function pointer | `delegate* unmanaged[Cdecl]` fields of a struct installed into a native once and dispatched through on LATER calls (the vtable idiom) ride the same lowering. An `EntryPoint` name additionally exports the C symbol (`CppEmitter`, which also rejects the attribute on an instance method and a duplicate entry point). | `PInvokeUcoReverseSubset`, `PInvokeVtblStructSubset` |

Everything outside the table — object marshalling, array returns, a
**non-blittable struct in return position**, and the
`COM`/`BStr`/`ByValArray`/`SafeArray`/`FunctionPtr` `[MarshalAs]` kinds **on a
parameter or return** — is a precise `NotSupportedException` at transpile time,
never a silent default. Two narrowings of that sentence: a non-blittable struct *is*
marshalled in parameter and byref position (its own row above), and `ByValArray` is
a carve-out only as a parameter/return `[MarshalAs]` — as a struct-field descriptor
it is a supported row.

Three further sections of the same bucket assert a P/Invoke **mechanism** rather
than a marshalling shape, so they have no row: `PInvokeCrossAsmSubset` (imports
declared by a *referenced* assembly, admitted only by `--pinvoke-module <name>`;
the native gate's step 5 asserts the flagless transpile still refuses them),
`NativeImplSubset` (`[Dn2Cpp.Runtime.NativeImplementation]` substituting transpiled
C# for an import at transpile time, so the library is never referenced or linked),
and `PInvokeStackallocUtf8Subset` (a `stackalloc` UTF-8 buffer passed as a raw
`byte*` — no marshaller runs at all; it rides the envelope row's pointer case).

A further section is in this bucket for the TARGET the bucket is built for rather
than for P/Invoke at all: `SequentialSizeSubset` measures the size of a
`[StructLayout(Size = N)]` value type whose fields end on a pointer. The emitted
body fixes that size, so it can only be wrong on a 32-bit target, and
`gates/build-and-run-pinvoke-wasm.sh` is the only gate that builds this program for
one — the same reason the size checks in `PInvokeExplicitUnionSubset` are here.
