# BPI-FORMAT — Baked Patch Image binary specification (v1)

The distributable format for dn2cpp hot update.

## Purpose and invariants

A **Baked Patch Image (BPI)** is a binary produced by baking a managed DLL (a
patch) into an interpretation-optimized form **at build time**. The core
invariants:

1. **Position independence**: every cross-reference inside the blob is either
   a table index or a byte offset from the blob start. **No absolute pointers
   whatsoever** — independent of the mmap/whole-read address, no relocation.
2. **Zero-copy execution**: code/strings/metadata are referenced **in place**
   at runtime. No per-instruction / per-method allocation.
3. **Binding is one-shot and small**: load-time allocation is only the
   ImportBinding array (proportional to import count) + the patch types'
   `Dn2CppTypeInfo`/vtables (proportional to type count). Not proportional to
   code size.
4. **No token resolution, IL decoding, or signature parsing exists in the
   runtime.** The converter (`dn2cpp --emit-patch`) has done all of it; only
   the results are baked into the BPI.

## Conventions

- **Endianness**: all multi-byte integers are little-endian.
- **Alignment**: sections on 16-byte boundaries; records naturally aligned.
- **Reference encoding**: variable data (strings/blobs) via blob-relative
  `u32` offsets, structured references via `u32` table indices.
- **Tagged reference `EntityRef` (u32)**: the common encoding for
  type/method/field references.
  - bits 30–31 = tag: `0` = PatchEntity (index into a patch table) /
    `1` = Import (ImportTable index → resolved to an ImportBinding at load) /
    `2` = Primitive (low 30 bits = `PrimitiveTypeCode`) / `3` = reserved.
  - bits 0–29 = index or code.
  - Sentinel is `0xFFFFFFFF` (none).

## File layout

```
[ Header(64B) ][ SectionTable ][ Section 1 ][ Section 2 ] ... (16B aligned)
```

### Header (64 bytes, fixed)
| off | size | field | description |
|-----|------|-------|-------------|
| 0  | 8 | `magic` | `"DN2BPI\0\0"` |
| 8  | 4 | `formatVersion` | +1 on layout-incompatible change. The loader rejects an unknown major |
| 12 | 4 | `flags` | bit0 selects the register code format (§Register code format — the converter's default; 0 = the v1 stack encoding, forced by `--patch-stackcode`). Loaders reject unknown bits |
| 16 | 8 | `baseImageAbiHash` | hash of the base-image ABI contract (below). The loader checks it against the running base |
| 24 | 4 | `sectionCount` | |
| 28 | 4 | `sectionTableOff` | blob-relative |
| 32 | 4 | `patchTypeCount` | TypeTable record count (pre-tallied, for bind allocation) |
| 36 | 4 | `importCount` | ImportTable record count (for ImportBinding allocation) |
| 40 | 4 | `entryMethodIdx` | patch entry point (`0xFFFFFFFF` = none) |
| 44 | 4 | `patchVersion` | deployment ordering key (`--patch-version`; 0 = unversioned default). Directory loads apply BPIs in ascending order (§Deployment) |
| 48 | 16 | reserved | zero-filled |

Reserved header bytes are claimed front-to-back by additive fields whose zero
value must mean the pre-field behavior, so older BPIs (which zero-fill the
region) stay readable without a `formatVersion` bump.

### SectionTable entry (16 bytes, fixed)
| off | size | field |
|-----|------|-------|
| 0  | 4 | `kind` (enum below) |
| 4  | 4 | `offset` (blob-relative) |
| 8  | 4 | `length` (bytes) |
| 12 | 4 | `count` (record count; 0 for byte arenas) |

### Section kinds
| kind | name | contents |
|------|------|----------|
| 1 | StringPool | UTF-8 arena for names |
| 2 | ImportTable | external references bound to the base image |
| 3 | TypeTable | patch-defined types |
| 4 | FieldTable | field descriptions |
| 5 | MethodTable | method descriptions |
| 6 | CodeSection | pre-resolved bytecode |
| 7 | EHTable | exception regions |
| 8 | VtableDescTable | slot-override descriptions for patch types |
| 9 | LocalSigTable | per-method arg+local type-reference sequences |
| 10 | UserStringPool | UTF-16 arena for `ldstr` |
| 11 | SwitchTable | target-index sequences for `switch` |
| 12 | ItfDescTable | interface-implementation descriptions for patch types |

## Section details

### StringPool (1)
UTF-8 byte arena. **A name reference = a `u32` offset** pointing at
`{ u16 len; u8 utf8[len] }`. Holds type/method/field names and
signature-shape strings.

### UserStringPool (10)
UTF-16 arena. **An `ldstr` reference = a `u32` offset** pointing at
`{ u32 charLen; u16 utf16[charLen]; u16 0 }` (entries 4-byte-aligned; the
trailing NUL is not counted in `charLen`). The runtime constructs
`Dn2CppString`s pointing into this region (no copy; §Load) — the NUL
satisfies the `Dn2CppString.chars` termination contract.

### ImportTable (2) — `ImportRecord` (24 bytes)
| off | size | field | description |
|-----|------|-------|-------------|
| 0  | 4 | `kind` | 1=Type / 2=Method / 3=Field |
| 4  | 4 | `nameOff` | StringPool (CLR FullName for types — `Elem[]` for a synthetic SZArray record —, simple name for members; `.ctor` for constructors) |
| 8  | 4 | `declTypeImport` | Import index of a member's declaring type (`0xFFFFFFFF` for Type) |
| 12 | 4 | `sigShapeOff` | StringPool. Member overload discrimination + bridge ABI shape |
| 16 | 4 | `aux0` | Type = bit 0 marks a synthetic **SZArray type record** (below; 0 otherwise) / Method = parameter count (excluding `this`) in the low 16 bits, bit 16 = instance (a `this` slot precedes the args) / Field = 0 |
| 20 | 4 | `aux1` | Type(SZArray) = the element type's EntityRef / Method = LocalSigTable index of the import's `[return, params…]` EntityRef run (paramCount+1 entries; the return entry is `0xFFFFFFFF` for void) / Field = the field type's EntityRef |

At load, each ImportRecord resolves into **ImportBinding[i]** (§Load). The
`aux1` signature references drive the runtime's box/unbox marshalling across
the invoker/accessor-thunk boundary, so the binder never parses signature
strings.

**SZArray type records.** An array type on the patch surface (an array-typed
arg/local/field/import-signature slot, a `newarr`/`isinst`/`castclass` operand)
is encoded as a Type import with `aux0` bit 0 set, named by the CLR array name
(`System.Int32[]`, …), with `aux1` carrying the **element type's** EntityRef (a
Primitive code, a type Import, or a PatchEntity TypeTable index — so no new
EntityRef tag is consumed). The loader binds it by name against the type
registry first (a hit is the base image's precise per-element array type-info)
and otherwise constructs an array type-info from the resolved element **after
patch-type construction** (§Load step 5b), so patch-class elements resolve too.
Only single-dimension zero-based arrays over the fenced element kinds exist.

The v1 `sigShape` string (both here and in MethodTable) is the transpiler's
`SigKey` with the leading name removed: `(<paramTypes, comma-joined>):<ret>`
in `TypeDesc` rendering — e.g. `WriteLine(string)` is `(String):Void`.

**Nested base-image types are outside the type-import boundary.** The registry
keys a nested type by its CLR reflection name (`Ns.Outer+Inner`), but the
converter renders a nested TypeRef as its bare metadata simple name (`Inner`,
empty namespace), so a patch naming a nested base-image type — as a base class,
a slot type, or an array element — emits an import the loader cannot bind.

### TypeTable (3) — `TypeRecord` (32 bytes)
| off | size | field |
|-----|------|-------|
| 0  | 4 | `nameOff` |
| 4  | 4 | `baseRef` (EntityRef: Import = AOT base / PatchEntity = patch base — always a **smaller** TypeTable index, the converter bakes the table base-before-derived (patch-base-depth order) so the loader constructs in one pass / `0xFFFFFFFF` = object) |
| 8  | 4 | `flags` (mirror of `DN2CPP_TF_*` + interp-specific bits) |
| 12 | 4 | `itfDescOff` (into ItfDescTable — this type's interface-implementation run; `0xFFFFFFFF` = implements no base-image interface. Occupies the record's former, always-zero `instanceSize` word: the loader owns the numeric layout and computes the size against the live base type-info at load (§Load step 5), so no size is ever baked) |
| 16 | 4 | `fieldStart` / `fieldCount` (packed in high/low 16 bits) |
| 20 | 4 | `methodStart` |
| 24 | 4 | `methodCount` |
| 28 | 4 | `vtableDescOff` (into VtableDescTable; `0xFFFFFFFF` = no overrides) |

`flags` mirrors `DN2CPP_TF_*` in its low bits; the high bits are
interp-specific. **Bit 31 (`DN2CPP_BPI_TF_HAS_CCTOR`)** = the type declares a
static constructor, baked as the **first** entry of the type's MethodTable run
(`MethodTable[methodStart]`), so the runtime's lazy-cctor guard finds it
without a load-time name scan. The interpreter runs it lazily, exactly once —
before the first `ldsfld`/`stsfld` on one of the type's static fields and
before the first call into the type from outside it — with the per-type started
flag set **before** the initializer body runs, so re-entry from the running
initializer neither deadlocks nor re-runs; a throwing initializer propagates
the exception raw and never re-runs (the AOT accessor thunks' `__ensure`
semantics).

### FieldTable (4) — `FieldRecord` (20 bytes)
| off | size | field |
|-----|------|-------|
| 0  | 4 | `nameOff` |
| 4  | 4 | `typeRef` (EntityRef) |
| 8  | 4 | `offset` (instance fields: the field's **ordinal among its type's instance fields**, declaration order — never a byte offset; the loader assigns real offsets against the live base layout, §Load step 5. 0 for statics) |
| 12 | 4 | `flags` (bit0 = static, mirroring `DN2CPP_FLDA_STATIC`) |
| 16 | 4 | `staticSlot` (static-storage index for statics, `0xFFFFFFFF` for instance) |

`staticSlot` indices are assigned consecutively across the image (`0..N-1`,
type/declaration order); at load the runtime allocates one GC-scanned 8-byte
slot per index (§Load), so a static field value is a plain eval-stack-slot
copy at run time. Instance fields carry **no numeric layout** — the converter
never computes byte offsets or sizes (the C++ side owns layout, the same
ownership rule as `baseImageAbiHash`'s symbolic contract); the loader appends
each instance field after the live base type's `instanceSize` in declaration
order with natural alignment (4 for Int32/Boolean/Char/Single, 8 for
Int64/Double/references) and rounds the final size up to 8.

### MethodTable (5) — `MethodRecord` (40 bytes)
| off | size | field |
|-----|------|-------|
| 0  | 4 | `nameOff` |
| 4  | 4 | `declTypeIdx` (patch type index) |
| 8  | 4 | `sigShapeOff` (StringPool. Bridge ABI shape + overload discrimination) |
| 12 | 2 | `maxStack` |
| 14 | 2 | `localCount` |
| 16 | 4 | `argCount` (including this) |
| 20 | 4 | `localSigOff` (into LocalSigTable; start of the arg+local type-reference run) |
| 24 | 4 | `codeOff` (byte offset into CodeSection) |
| 28 | 4 | `codeInsnCount` |
| 32 | 4 | `ehStart` (EHTable index) |
| 36 | 2 | `ehCount` |
| 38 | 2 | `vtableSlot` (occupied slot; `0xFFFF` if non-virtual) |

### CodeSection (6) — `Insn` (12 bytes, fixed)
| off | size | field |
|-----|------|-------|
| 0 | 2 | `op` (internal opcode enum) |
| 2 | 2 | `pfx` (prefix bits: volatile/tail/unaligned; constrained puts the type reference in `b`) |
| 4 | 4 | `a` (primary operand) |
| 8 | 4 | `b` (secondary operand) |

**All operands are resolved at conversion time**:

- `ldc.i4`: `a` = constant. `ldc.i8`/`r8`: `a`/`b` = lo/hi of the 64 bits.
  `r4`: `a` = bit pattern.
- `ldloc`/`stloc`/`ldarg`/`starg`/`ldloca`/`ldarga`: `a` = slot index.
- `ldstr`: `a` = UserStringPool offset.
- branches `br`/`brtrue`/`beq`…: `a` = **target instruction index**
  (absolute, pre-normalized). Conditional branches carry the operand-kind
  hint (below) in `b`.
- `switch`: `a` = SwitchTable start index, `b` = count.
- `call`/`callvirt`/`newobj`/`ldftn`: `a` = method `EntityRef`,
  `b` = constrained type reference or 0. For a PatchEntity or Import
  `call`/`callvirt` target, `b` bit0 = the callee returns a value (the caller
  pushes the result); for a PatchEntity target, `b` bit1 = instance (arg0 is
  the receiver, null-checked by the interpreter). A `newobj` `a` is a `.ctor`
  Import (AOT type) or a PatchEntity `.ctor` MethodTable index (patch type); it
  always pushes the constructed reference.
  A `callvirt` whose bound method is virtual dispatches through the receiver's
  vtable slot (resolved at bind time) — on a patch instance with overrides that
  is the patched vtable copy, so it lands on the N2M trampoline and the
  interpreted override. `call` and a non-virtual `callvirt` invoke the bound
  pointer directly, so `base.Method()` from an override body lands on the base
  implementation, and a `.ctor` Import reached by `call` is an inheriting patch
  ctor's base-ctor chain (a chain to `System.Object::.ctor()` folds to `pop` at
  bake time). A `callvirt` on a *non-virtual* patch method canonicalizes to
  `call`; on a *virtual* patch method it stays `callvirt`, and the interpreter
  re-resolves the frozen slot against the receiver's live patch type (the
  VtableDesc chain walk, §VtableDescTable), so a patch-derived receiver lands
  on its own re-override.
- `ldftn`: `a` = the method `EntityRef` a following delegate `newobj` binds — a
  PatchEntity MethodTable index or an Import naming a base-image **instance**
  method (a static one needs an adapter the interpreter has none of, so it is
  rejected). The interpreter pushes a small GC closure carrying the reference
  (§Delegate thunks); the receiver is captured into it at the `newobj`.
  `ldvirtftn` is a conversion-time rejection.
- `newobj` on a **delegate type**: `a` = the delegate type Import (kind Type,
  not a `.ctor` Method import — that is the discriminator the interpreter
  branches on). It pops the `ldftn` closure and the target below it and builds a
  `Dn2CppDelegate` (§Delegate thunks): a patch-method closure captures the
  target as the bound `this` and takes the delegate's pre-emitted thunk as
  `method`; a base-image-method closure becomes the natural AOT delegate
  (`method` = the method pointer, `target` = the receiver). The converter
  recognizes a delegate construction by its `.ctor(object, native int)`
  signature. A generic delegate resolves to its mangled registry name against
  the manifest's `instantiations` map; one the base image never emitted hits the
  missing-AOT-instantiation boundary (§Generics on the patch surface).
- `newobj` on an **exception type** (any base-image type whose chain reaches
  `System.Exception`): the `.ctor` Import binds to shape `kShapeExceptionNew`,
  mirroring the AOT newobj interception. The interpreter allocates the uniform
  message-carrying object (`dn2cpp_exception_new`, stamped with the derived
  type-info) and seeds `Message`/`innerException` positionally — the shapes
  `Compilation.ExceptionMessageArgIndex` / `ExceptionInnerArgIndex` recognize
  are `(string)`, `(string, Exception)`, `(TCode, string)`,
  `(TCode, string, Exception)`. If the base build reached a real, non-opaque
  ctor body for that shape (non-null reflection `fnPtr`), the interpreter then
  runs it through its invoker so a derived exception's own field writes land; an
  opaque base (`System.Exception`/`AggregateException`) or a never-reached ctor
  leaves `fnPtr` null and degrades to seed-only. A **patch** type may derive
  from a base-image exception: the loader lays its fields out after the base's
  `Dn2CppExceptionObject` message/inner/hresult/trace prefix (the intrinsic
  exception handles report `instanceSize` 0, so the append is floored at
  `sizeof(Dn2CppExceptionObject)`), and its interpreted ctor's `base(message)`
  is a `call` to the base exception ctor taking the same `kShapeExceptionNew`
  path.
- `callvirt` on a **delegate** `Invoke`: baked as an ordinary method-import
  `callvirt` (the import's declaring type is the base-image delegate). At bind
  the runtime sees the `DN2CPP_TF_DELEGATE` flag + the `Invoke` name and routes
  the dispatch through the delegate's pre-emitted invoke bridge (§Delegate
  thunks) instead of a reflection-metadata invoker. `+=`/`-=` (multicast
  `Delegate.Combine`/`Remove`) is a conversion-time rejection.
- `ret`: `a` = 1 when the method returns a value (pop and return the top slot),
  0 for void.
- `ldfld`/`stfld`/`ldflda`: `a` = field `EntityRef`. An AOT field's storage is
  reached through the `Dn2CppFieldInfo` accessors resolved into the
  ImportBinding; a PatchEntity ref is a patch instance field at its
  loader-assigned byte offset, with `b` = the declaring TypeTable index.
- `ldsfld`/`stsfld`: `a` = static-field `EntityRef`. For a PatchEntity ref
  `b` = the declaring TypeTable index, anchoring the lazy-cctor guard; the
  access is a plain slot copy against the image's static storage.
- `throw`: no operand. `rethrow`: `a` = the **method-relative EHTable index** of
  the enclosing catch clause (re-raises that handler's caught object; resolved
  at conversion time, so no runtime handler scan).
- `leave` (short form normalized): `a` = target instruction index. The eval
  stack empties; the interpreter runs the finally handler of every region left
  between the leave and its target, innermost-first. `endfinally`
  (= `endfault`): no operand.
- `castclass`/`isinst`: `a` = type `EntityRef` — an Import ref resolves to its
  bound base-image type (including a synthetic SZArray type import, decided by
  the runtime's array-covariance arm: reference elements covariant by element
  assignability, value elements exact), a PatchEntity ref to the
  loader-constructed patch type-info. Both feed the shared base-chain walk; a
  failed `castclass` raises the catchable `InvalidCastException` with the .NET
  message. `box`/`unbox.any`/`ldtoken`: `a` = type `EntityRef`; conversion-time
  rejections in v1.
- `newarr`: `a` = the SZArray type `EntityRef` (the resolved array type-info
  stamps the allocation header), `b` = the **element storage kind** (below),
  which selects the array representation (the same three layouts the AOT lane
  allocates, so arrays cross the AOT boundary unconverted). A negative length
  raises the catchable `OverflowException`.
- `ldelem`/`stelem`: `a` = the element storage kind — every typed/short form
  (`ldelem.i4`, `stelem.ref`, `ldelem <type>`, …) normalizes to this canonical
  pair at bake time, the kind derived from the opcode or the type-token operand.
  An out-of-bounds index raises the catchable `IndexOutOfRangeException`; a
  reference-element `stelem` performs **no covariance check** (AOT-lane parity;
  see the carve-outs). `ldlen`: no operand; the length pushes as an i32.
- arithmetic/comparison (`add`/`ceq`…): `a` = the **operand-kind hint** (below),
  decided by the converter from an abstract eval-stack simulation, eliminating
  runtime type inference. `b` = 0.
- conversions (`conv.i4`/`conv.u8`/`conv.r8`…): `a` = the **source** operand's
  kind hint; the opcode itself carries the target width and signedness.

**Operand-kind hints** (the abstract eval-stack type of the operand(s)):

| value | kind |
|-------|------|
| 0 | i32 (Int32/Boolean/Char eval-stack slots) |
| 1 | i64 |
| 2 | f32 |
| 3 | f64 |
| 4 | ref (object reference) |

Signedness is **not** part of the hint — the opcode itself (the `.un` forms,
`conv.u*`) carries it. i32 values are kept sign-extended in their 64-bit
slot; f32 values are kept widened to double (exact — narrowing back is
lossless), with f32-hinted operations rounding each result to float32.

**Element storage kinds** (the `newarr`/`ldelem`/`stelem` operand: the
element's byte width, the signedness of sub-int32 loads, and the array
representation — i4 = the int32 array layout, ref = the reference array
layout, everything else the packed element-sized layout):

| value | storage |
|-------|---------|
| 0 | i1 (1 byte, sign-extending load) |
| 1 | u1 (1 byte, zero-extending — Boolean) |
| 2 | i2 (2 bytes, sign-extending) |
| 3 | u2 (2 bytes, zero-extending — Char) |
| 4 | i4 (4 bytes; the int32 array layout) |
| 5 | i8 (8 bytes) |
| 6 | r4 (4 bytes, widened into the f64 slot) |
| 7 | r8 (8 bytes) |
| 8 | ref (pointer; the reference array layout) |

> The opcode enum is the **raw ECMA-335 opcode value** (u16; two-byte opcodes
> encode as `0xFExx`), so there is no separate enum to keep in sync with the
> converter. The converter normalizes short forms to their canonical long
> form (`ldc.i4.3` → `ldc.i4 a=3`, `br.s` → `br`, `ldarg.0` → `ldarg a=0`, …)
> so the interpreter switch handles only the long forms. This applies to the
> v1 stack encoding; `Header.flags` bit0 selects the register code format,
> which carries its own dense opcode enum (§Register code format).

### EHTable (7) — `EHRecord` (28 bytes)
`{ u32 kind(0=catch/1=filter/2=finally/3=fault); u32 tryStart; u32 tryEnd;
u32 handlerStart; u32 handlerEnd; u32 filterStart(or 0xFFFFFFFF);
u32 catchTypeRef(EntityRef or 0xFFFFFFFF) }`.
All ranges are **instruction indices**, half-open (`[start, end)`). One
method's records are consecutive (`MethodRecord.ehStart`/`ehCount`) in
metadata order — **innermost-first**, the order the interpreter's linear EH
scan relies on. `catchTypeRef` is an Import-tagged type `EntityRef` for catch
clauses (matched at run time by a base-chain walk of the thrown object's
type-info; `System.Exception`/`System.Object` are catch-all, the same
root-type rule as the AOT catch dispatch) and `0xFFFFFFFF` otherwise.
kind 1 (filter) is defined but outside the v1 converter fence.

Propagation is unified with AOT code: a managed throw rides the runtime's own
C++ exception (`dn2cpp_throw` / `Dn2CppException`) through interpreted and AOT
frames alike — each interpreted frame scans its EHTable records
innermost-first around the dispatch loop, and an unhandled exception continues
unwinding as the same C++ exception into the AOT caller.

### VtableDescTable (8)
`TypeRecord.vtableDescOff` points at
`{ u32 vtableLen; u32 overrideCount; SlotEntry[overrideCount] }`,
`SlotEntry = { u32 slot; u32 patchMethodIdx }`. `vtableLen` is the **nearest
AOT ancestor** type's total vtable slot count (baked from the manifest's slot
list — the live `Dn2CppTypeInfo` carries no vtable length; patch types declare
no new slots, so the ancestor owns every occupiable slot at every patch-chain
depth), and each entry pairs a frozen base slot with the overriding patch
method. At load, the base vtable — the AOT base's, or a patch base's
already-patched copy — is copied at `vtableLen` entries and each listed slot is
overwritten with the **N2M trampoline** registered for `(slot, the method's
sigShape)` (§N2M trampolines); types without overrides (`vtableDescOff` =
`0xFFFFFFFF`) keep sharing the base vtable, so a patch-derived type without
re-overrides inherits its patch base's patched slots. Each entry's method must
belong to the owning type and carry the matching `MethodRecord.vtableSlot`.
Slot → method resolution at dispatch time walks the receiver's patch base
chain (its own VtableDesc first, then each patch ancestor's), so the deepest
re-override wins.

**Slot freeze.** The converter resolves an override to its slot by matching the
method's SigKey against the AOT ancestor's slot list in the `base-abi.json`
manifest — never by recomputing the base vtable — so the baked numbers are the
same V lines the ABI hash freezes, and a base rebuild that moves slots changes
the hash and mechanically rejects the stale BPI. New virtual slot declarations
(newslot) and overrides of ToString/GetHashCode/Equals/Finalize (dispatched
through dedicated type-info entries, not vtable slots) are conversion-time
rejections.

### ItfDescTable (12)
`TypeRecord.itfDescOff` points at
`{ u32 itfCount; ItfEntry[itfCount] }`,
`ItfEntry = { u32 itfImport; u32 fullSlotCount; u32 implCount; SlotImpl[implCount] }`,
`SlotImpl = { u32 slot; u32 patchMethodIdx }`. It describes the base-image
interfaces a patch type implements. `itfImport` is an **Import**-tagged type
`EntityRef` to the interface (bound to the live interface type-info at load);
`fullSlotCount` is the interface's total method-slot count (from the manifest's
I lines, §baseImageAbiHash); and each `SlotImpl` pairs an interface method slot
with the patch method (a MethodTable index) that implements it. The converter
resolves a slot's implementer by explicit `.override` (a MethodImpl targeting
the interface method) or by name+SigKey match against the type or its patch
base chain; a slot left to an AOT-base inheritance carries no `SlotImpl` (the
loader fills it from the base's interface map).

At load (§Load step 5) a type with an ItfDesc gets an **extended interface map**
instead of sharing the base's: for each interface, a fresh slots array is seeded
from the base type's implementation of that interface (`fullSlotCount` entries,
or zero-filled when the base does not implement it) and each `SlotImpl` slot is
overwritten with the **N2M interface trampoline** registered for
`(interface type-info, slot)` (§N2M trampolines). Interfaces the type inherits
unchanged keep sharing the base entries. Slot → method resolution at dispatch
walks the receiver's patch base chain (its own ItfDesc first, then each patch
ancestor's) like the VtableDesc chain, so a patch-derived re-implementation
wins.

Implementation methods that are new virtual slots occupy **no class vtable
slot** (`MethodRecord.vtableSlot` = `0xFFFF`) — they are reached through the
interface map — while an interface method that also overrides a base class
virtual keeps its VtableDesc entry. Generic interfaces, interfaces absent from
the base image, and interface **declarations** inside the patch are
conversion-time rejections.

### LocalSigTable (9)
A flat array of `EntityRef`s. `argCount+localCount` entries starting at
`MethodRecord.localSigOff`. Used for value-type/reference classification of
eval/local slots (box/unbox, GC scanning). The arena also holds each method
import's `[return, params…]` signature run (referenced from
`ImportRecord.aux1`).

### SwitchTable (11)
A flat array of `u32` (instruction indices). Referenced by a `switch`
instruction as `(a = start, b = count)`.

## Register code format (`Header.flags` bit0)

`Header.flags` bit0 = 1 selects the **register interpretation of the
CodeSection**. Nothing else about the file changes: the same 12-byte
instruction record size, the same section kinds and layout, the same encoding
of every other section, and no `formatVersion` bump — the bit reinterprets
CodeSection records only. A loader that does not support the bit rejects the
file (the directory loader skips it): loaders enforce a supported-flags mask,
so an unknown bit is never silently run through the wrong dispatch loop.

> This section is the normative specification for the register format. The C++
> X-macro opcode list (`runtime/core/dn2cpp_interp_regops.h`) and its C# mirror
> (`src/Dn2Cpp.Transpiler/HotUpdate/RegOps.cs`) must match the opcode table
> below, value for value. Converter emission of the format is the default
> (`--emit-patch` bakes register code; `--patch-stackcode` forces the v1 stack
> encoding).

### Instruction record (12 bytes, fixed)

| off | size | field | description |
|-----|------|-------|-------------|
| 0 | 2 | `op` | dense register-opcode enum value (normative table below; 0 = `R_INVALID`, traps) |
| 2 | 2 | `regs` | two register operands: low byte = `r0`, high byte = `r1`; `0xFF` = unused |
| 4 | 4 | `a` | payload, or a third register `r2` in its low byte (class C3) |
| 8 | 4 | `b` | payload |

The v1 `pfx` field **does not exist** in this format — v1 wrote it as 0 and
never read it; any prefix semantics that ever matter become distinct opcodes
(`tail.` remains unsupported).

### Register model

One flat frame array of interpreter slots:

```
regs[0 .. argCount)            args   (copied in at entry)
regs[argCount .. slotCount)    locals (zero-initialized)
regs[slotCount + d]            the eval temp at abstract stack depth d
```

with `tempBase = slotCount = argCount + localCount`. The converter's abstract
eval-stack simulation (the same one that decides v1's operand-kind hints)
makes the depth **statically known at every instruction** and enforces
identical stack shapes at merge points, so `reg = tempBase + depth` is a pure
function of the program point: **no phi nodes, ever**. Registerization is a
renaming, not an optimization problem.

- **Limits**: `argCount + localCount` ≤ 64 and the eval-temp region ≤ 64, so a
  frame never exceeds **128 registers** and register operands are u8
  (`0xFF` = unused). Both limits are converter-enforced (a method over either
  is a conversion-time rejection).
- **Method-record fields keep their meaning**; under the register format
  `maxStack` records the **simulated maximum eval depth** — the register
  verifier bounds temp operands against it.
- **Call windows**: an operation consuming *n* values at depth *d* consumes
  regs `[tempBase+d-n, tempBase+d)` — always contiguous, so a call passes
  `&regs[windowBase]` **zero-copy** and the return value lands in
  `regs[windowBase]`. The native↔interpreted ABI
  (`Dn2CppInterpSlot* args + argCount`) is format-agnostic and unchanged.

### Operand classes

Every opcode belongs to one of four classes; the class fixes which record
fields are registers and which are payloads, driving both decode and the
load-time verifier:

| class | registers | payloads |
|-------|-----------|----------|
| C0 | none | `a` (branch target / EH index / unused) |
| C1 | `r0` | `a`, `b` |
| C2 | `r0`, `r1` | `a`, `b` |
| C3 | `r0`, `r1`, `r2` (= `a & 0xFF`) | `b` |

(`R_EXT`, the reserved extension record, is class C0 but carries both `a` and
`b` as payloads; the verifier rejects it in this version.)

### Opcode table (normative)

Values are assigned **sequentially in table order starting at 0** —
160 opcodes, values 0–159. The set covers exactly the v1 interpreted surface:
IL the converter fences (`box`/`unbox.any`, `switch`, …) has no register
opcode, the same way it has no stack-interpreter arm.

**R_INVALID and data movement (0–1).** `R_MOV` is the one data-movement op:
`ldarg`/`ldloc`/`starg`/`stloc`/`dup` all become register copies (or nothing
at all when the simulation folds them away); `nop` and `pop` emit no record.

| value | name | class | operands / semantics |
|-------|------|-------|----------------------|
| 0 | `R_INVALID` | C0 | traps if executed (never emitted) |
| 1 | `R_MOV` | C2 | `r0` = dst, `r1` = src: `r0 ← r1` (whole 8-byte slot) |

**Constants (2–7).** `r0` = dst throughout. Slot invariants are v1's: i32
values sign-extended in the 64-bit slot, f32 values widened to double.

| value | name | class | operands / semantics |
|-------|------|-------|----------------------|
| 2 | `R_LDNULL` | C1 | `r0 ← null` |
| 3 | `R_LDC_I32` | C1 | `a` = value (sign-extended into the slot) |
| 4 | `R_LDC_I64` | C1 | `a`/`b` = lo/hi of the 64 bits |
| 5 | `R_LDC_R4` | C1 | `a` = float bit pattern (stored widened) |
| 6 | `R_LDC_R8` | C1 | `a`/`b` = lo/hi of the double bit pattern |
| 7 | `R_LDSTR` | C1 | `a` = UserStringPool offset |

**Binary arithmetic (8–43).** All C3: `r0` = dst, `r1` = src1, `r2` = src2.
Div/rem semantics are identical to v1 (`DivideByZeroException` on a zero
divisor; signed `INT_MIN / -1` behavior; float rem = `fmod`). The `_F32` forms
compute in float precision and store widened. Shift amounts mask to the
operand width.

| value | name | class | operands / semantics |
|-------|------|-------|----------------------|
| 8 | `R_ADD_I32` | C3 | `r0 ← r1 + r2` (i32) |
| 9 | `R_ADD_I64` | C3 | `r0 ← r1 + r2` (i64) |
| 10 | `R_SUB_I32` | C3 | `r0 ← r1 - r2` (i32) |
| 11 | `R_SUB_I64` | C3 | `r0 ← r1 - r2` (i64) |
| 12 | `R_MUL_I32` | C3 | `r0 ← r1 * r2` (i32) |
| 13 | `R_MUL_I64` | C3 | `r0 ← r1 * r2` (i64) |
| 14 | `R_DIV_I32` | C3 | `r0 ← r1 / r2` (i32, signed) |
| 15 | `R_DIV_I64` | C3 | `r0 ← r1 / r2` (i64, signed) |
| 16 | `R_DIV_UN_I32` | C3 | `r0 ← r1 / r2` (u32) |
| 17 | `R_DIV_UN_I64` | C3 | `r0 ← r1 / r2` (u64) |
| 18 | `R_REM_I32` | C3 | `r0 ← r1 % r2` (i32, signed) |
| 19 | `R_REM_I64` | C3 | `r0 ← r1 % r2` (i64, signed) |
| 20 | `R_REM_UN_I32` | C3 | `r0 ← r1 % r2` (u32) |
| 21 | `R_REM_UN_I64` | C3 | `r0 ← r1 % r2` (u64) |
| 22 | `R_AND_I32` | C3 | `r0 ← r1 & r2` (i32) |
| 23 | `R_AND_I64` | C3 | `r0 ← r1 & r2` (i64) |
| 24 | `R_OR_I32` | C3 | `r0 ← r1 \| r2` (i32) |
| 25 | `R_OR_I64` | C3 | `r0 ← r1 \| r2` (i64) |
| 26 | `R_XOR_I32` | C3 | `r0 ← r1 ^ r2` (i32) |
| 27 | `R_XOR_I64` | C3 | `r0 ← r1 ^ r2` (i64) |
| 28 | `R_ADD_F32` | C3 | `r0 ← r1 + r2` (f32, stored widened) |
| 29 | `R_ADD_F64` | C3 | `r0 ← r1 + r2` (f64) |
| 30 | `R_SUB_F32` | C3 | `r0 ← r1 - r2` (f32, stored widened) |
| 31 | `R_SUB_F64` | C3 | `r0 ← r1 - r2` (f64) |
| 32 | `R_MUL_F32` | C3 | `r0 ← r1 * r2` (f32, stored widened) |
| 33 | `R_MUL_F64` | C3 | `r0 ← r1 * r2` (f64) |
| 34 | `R_DIV_F32` | C3 | `r0 ← r1 / r2` (f32, stored widened) |
| 35 | `R_DIV_F64` | C3 | `r0 ← r1 / r2` (f64) |
| 36 | `R_REM_F32` | C3 | `r0 ← fmod(r1, r2)` (f32, stored widened) |
| 37 | `R_REM_F64` | C3 | `r0 ← fmod(r1, r2)` (f64) |
| 38 | `R_SHL_I32` | C3 | `r0 ← r1 << r2` (i32) |
| 39 | `R_SHL_I64` | C3 | `r0 ← r1 << r2` (i64) |
| 40 | `R_SHR_I32` | C3 | `r0 ← r1 >> r2` (i32, arithmetic) |
| 41 | `R_SHR_I64` | C3 | `r0 ← r1 >> r2` (i64, arithmetic) |
| 42 | `R_SHR_UN_I32` | C3 | `r0 ← r1 >> r2` (u32, logical) |
| 43 | `R_SHR_UN_I64` | C3 | `r0 ← r1 >> r2` (u64, logical) |

**Unary (44–49).** All C2: `r0` = dst, `r1` = src.

| value | name | class | operands / semantics |
|-------|------|-------|----------------------|
| 44 | `R_NEG_I32` | C2 | `r0 ← -r1` (i32) |
| 45 | `R_NEG_I64` | C2 | `r0 ← -r1` (i64) |
| 46 | `R_NEG_F32` | C2 | `r0 ← -r1` (f32, stored widened) |
| 47 | `R_NEG_F64` | C2 | `r0 ← -r1` (f64) |
| 48 | `R_NOT_I32` | C2 | `r0 ← ~r1` (i32) |
| 49 | `R_NOT_I64` | C2 | `r0 ← ~r1` (i64) |

**Conversions (50–67).** All C2: `r0` = dst, `r1` = src. Semantics are
identical to v1's `conv.*` arms; the target width and signedness ride in the
opcode, the source kind is baked in at conversion time (the v1 hint, promoted
to the opcode). `_FROM_F` covers **both** float widths — the slot holds a
widened double either way — which is why the group is 6×3, not 6×4.

| value | name | class | operands / semantics |
|-------|------|-------|----------------------|
| 50 | `R_CONV_I4_FROM_I32` | C2 | `conv.i4`, i32 source |
| 51 | `R_CONV_I4_FROM_I64` | C2 | `conv.i4`, i64 source |
| 52 | `R_CONV_I4_FROM_F` | C2 | `conv.i4`, float source |
| 53 | `R_CONV_U4_FROM_I32` | C2 | `conv.u4`, i32 source |
| 54 | `R_CONV_U4_FROM_I64` | C2 | `conv.u4`, i64 source |
| 55 | `R_CONV_U4_FROM_F` | C2 | `conv.u4`, float source |
| 56 | `R_CONV_I8_FROM_I32` | C2 | `conv.i8`, i32 source (sign-extends) |
| 57 | `R_CONV_I8_FROM_I64` | C2 | `conv.i8`, i64 source |
| 58 | `R_CONV_I8_FROM_F` | C2 | `conv.i8`, float source |
| 59 | `R_CONV_U8_FROM_I32` | C2 | `conv.u8`, i32 source (zero-extends) |
| 60 | `R_CONV_U8_FROM_I64` | C2 | `conv.u8`, i64 source |
| 61 | `R_CONV_U8_FROM_F` | C2 | `conv.u8`, float source |
| 62 | `R_CONV_R4_FROM_I32` | C2 | `conv.r4`, i32 source (rounds to float32, stored widened) |
| 63 | `R_CONV_R4_FROM_I64` | C2 | `conv.r4`, i64 source (rounds to float32, stored widened) |
| 64 | `R_CONV_R4_FROM_F` | C2 | `conv.r4`, float source (rounds to float32, stored widened) |
| 65 | `R_CONV_R8_FROM_I32` | C2 | `conv.r8`, i32 source |
| 66 | `R_CONV_R8_FROM_I64` | C2 | `conv.r8`, i64 source |
| 67 | `R_CONV_R8_FROM_F` | C2 | `conv.r8`, float source |

**Comparisons (68–89).** All C3: `r0` = dst, `r1`/`r2` = operands; the result
is an i32 0/1. Float `_UN` forms carry v1's unordered semantics.
`R_CGT_UN_REF` is the canonical non-null idiom (`cgt.un` on references).

| value | name | class | operands / semantics |
|-------|------|-------|----------------------|
| 68 | `R_CEQ_I32` | C3 | `r0 ← (r1 == r2)` (i32) |
| 69 | `R_CEQ_I64` | C3 | `r0 ← (r1 == r2)` (i64) |
| 70 | `R_CEQ_F32` | C3 | `r0 ← (r1 == r2)` (f32) |
| 71 | `R_CEQ_F64` | C3 | `r0 ← (r1 == r2)` (f64) |
| 72 | `R_CGT_I32` | C3 | `r0 ← (r1 > r2)` (i32, signed) |
| 73 | `R_CGT_I64` | C3 | `r0 ← (r1 > r2)` (i64, signed) |
| 74 | `R_CGT_F32` | C3 | `r0 ← (r1 > r2)` (f32, ordered) |
| 75 | `R_CGT_F64` | C3 | `r0 ← (r1 > r2)` (f64, ordered) |
| 76 | `R_CGT_UN_I32` | C3 | `r0 ← (r1 > r2)` (u32) |
| 77 | `R_CGT_UN_I64` | C3 | `r0 ← (r1 > r2)` (u64) |
| 78 | `R_CGT_UN_F32` | C3 | `r0 ← (r1 > r2)` (f32, unordered) |
| 79 | `R_CGT_UN_F64` | C3 | `r0 ← (r1 > r2)` (f64, unordered) |
| 80 | `R_CLT_I32` | C3 | `r0 ← (r1 < r2)` (i32, signed) |
| 81 | `R_CLT_I64` | C3 | `r0 ← (r1 < r2)` (i64, signed) |
| 82 | `R_CLT_F32` | C3 | `r0 ← (r1 < r2)` (f32, ordered) |
| 83 | `R_CLT_F64` | C3 | `r0 ← (r1 < r2)` (f64, ordered) |
| 84 | `R_CLT_UN_I32` | C3 | `r0 ← (r1 < r2)` (u32) |
| 85 | `R_CLT_UN_I64` | C3 | `r0 ← (r1 < r2)` (u64) |
| 86 | `R_CLT_UN_F32` | C3 | `r0 ← (r1 < r2)` (f32, unordered) |
| 87 | `R_CLT_UN_F64` | C3 | `r0 ← (r1 < r2)` (f64, unordered) |
| 88 | `R_CEQ_REF` | C3 | `r0 ← (r1 == r2)` (reference identity) |
| 89 | `R_CGT_UN_REF` | C3 | `r0 ← (r1 != r2)` (refs; the `!= null` idiom) |

**Branches (90–138).** The target is `a` = the **absolute instruction index in
the register stream**. `R_BRTRUE_*`/`R_BRFALSE_*` are C1 (`r0` = the tested
operand); compare-branches are C2 (`r0`, `r1` = operands). Float
compare-branches keep v1's unordered-comparison semantics (the `_UN` forms
take the branch on an unordered pair; the ordered forms fall through).

| value | name | class | operands / semantics |
|-------|------|-------|----------------------|
| 90 | `R_BR` | C0 | unconditional; `a` = target |
| 91 | `R_BRTRUE_I32` | C1 | branch if `r0 != 0` (i32) |
| 92 | `R_BRTRUE_I64` | C1 | branch if `r0 != 0` (i64) |
| 93 | `R_BRTRUE_REF` | C1 | branch if `r0 != null` |
| 94 | `R_BRFALSE_I32` | C1 | branch if `r0 == 0` (i32) |
| 95 | `R_BRFALSE_I64` | C1 | branch if `r0 == 0` (i64) |
| 96 | `R_BRFALSE_REF` | C1 | branch if `r0 == null` |
| 97 | `R_BEQ_I32` | C2 | branch if `r0 == r1` (i32) |
| 98 | `R_BEQ_I64` | C2 | branch if `r0 == r1` (i64) |
| 99 | `R_BEQ_F32` | C2 | branch if `r0 == r1` (f32) |
| 100 | `R_BEQ_F64` | C2 | branch if `r0 == r1` (f64) |
| 101 | `R_BNE_UN_I32` | C2 | branch if `r0 != r1` (i32) |
| 102 | `R_BNE_UN_I64` | C2 | branch if `r0 != r1` (i64) |
| 103 | `R_BNE_UN_F32` | C2 | branch if `r0 != r1` (f32, unordered) |
| 104 | `R_BNE_UN_F64` | C2 | branch if `r0 != r1` (f64, unordered) |
| 105 | `R_BGE_I32` | C2 | branch if `r0 >= r1` (i32, signed) |
| 106 | `R_BGE_I64` | C2 | branch if `r0 >= r1` (i64, signed) |
| 107 | `R_BGE_F32` | C2 | branch if `r0 >= r1` (f32, ordered) |
| 108 | `R_BGE_F64` | C2 | branch if `r0 >= r1` (f64, ordered) |
| 109 | `R_BGT_I32` | C2 | branch if `r0 > r1` (i32, signed) |
| 110 | `R_BGT_I64` | C2 | branch if `r0 > r1` (i64, signed) |
| 111 | `R_BGT_F32` | C2 | branch if `r0 > r1` (f32, ordered) |
| 112 | `R_BGT_F64` | C2 | branch if `r0 > r1` (f64, ordered) |
| 113 | `R_BLE_I32` | C2 | branch if `r0 <= r1` (i32, signed) |
| 114 | `R_BLE_I64` | C2 | branch if `r0 <= r1` (i64, signed) |
| 115 | `R_BLE_F32` | C2 | branch if `r0 <= r1` (f32, ordered) |
| 116 | `R_BLE_F64` | C2 | branch if `r0 <= r1` (f64, ordered) |
| 117 | `R_BLT_I32` | C2 | branch if `r0 < r1` (i32, signed) |
| 118 | `R_BLT_I64` | C2 | branch if `r0 < r1` (i64, signed) |
| 119 | `R_BLT_F32` | C2 | branch if `r0 < r1` (f32, ordered) |
| 120 | `R_BLT_F64` | C2 | branch if `r0 < r1` (f64, ordered) |
| 121 | `R_BGE_UN_I32` | C2 | branch if `r0 >= r1` (u32) |
| 122 | `R_BGE_UN_I64` | C2 | branch if `r0 >= r1` (u64) |
| 123 | `R_BGE_UN_F32` | C2 | branch if `r0 >= r1` (f32, unordered) |
| 124 | `R_BGE_UN_F64` | C2 | branch if `r0 >= r1` (f64, unordered) |
| 125 | `R_BGT_UN_I32` | C2 | branch if `r0 > r1` (u32) |
| 126 | `R_BGT_UN_I64` | C2 | branch if `r0 > r1` (u64) |
| 127 | `R_BGT_UN_F32` | C2 | branch if `r0 > r1` (f32, unordered) |
| 128 | `R_BGT_UN_F64` | C2 | branch if `r0 > r1` (f64, unordered) |
| 129 | `R_BLE_UN_I32` | C2 | branch if `r0 <= r1` (u32) |
| 130 | `R_BLE_UN_I64` | C2 | branch if `r0 <= r1` (u64) |
| 131 | `R_BLE_UN_F32` | C2 | branch if `r0 <= r1` (f32, unordered) |
| 132 | `R_BLE_UN_F64` | C2 | branch if `r0 <= r1` (f64, unordered) |
| 133 | `R_BLT_UN_I32` | C2 | branch if `r0 < r1` (u32) |
| 134 | `R_BLT_UN_I64` | C2 | branch if `r0 < r1` (u64) |
| 135 | `R_BLT_UN_F32` | C2 | branch if `r0 < r1` (f32, unordered) |
| 136 | `R_BLT_UN_F64` | C2 | branch if `r0 < r1` (f64, unordered) |
| 137 | `R_BEQ_REF` | C2 | branch if `r0 == r1` (reference identity) |
| 138 | `R_BNE_UN_REF` | C2 | branch if `r0 != r1` (reference identity) |

**Object / call (139–152).** Field, cast, and array payloads (`a`/`b`) mean
exactly what they mean in v1; the element-storage-kind payload sub-dispatch is
retained.

| value | name | class | operands / semantics |
|-------|------|-------|----------------------|
| 139 | `R_CALL` | C1 | `r0` = window base; `a` = method `EntityRef`; `b` bit0 = hasResult (the result lands in `regs[r0]`), bit1 = instance-null-check — exactly v1's meanings. The consumed count comes from the callee's method record / import binding, as in v1 |
| 140 | `R_CALLVIRT` | C1 | as `R_CALL`, with v1's virtual/slot re-resolution semantics. A delegate-`Invoke` window is `[dg, args...]` |
| 141 | `R_NEWOBJ` | C1 | as `R_CALL`; the constructed reference lands in `regs[r0]`. A delegate-construction window is `[target, ftn]` |
| 142 | `R_LDFTN` | C1 | `r0` = dst; `a` = method `EntityRef` (pushes the delegate-method closure, as v1) |
| 143 | `R_LDFLD` | C2 | `r0` = dst, `r1` = obj; `a`/`b` = v1's field operands |
| 144 | `R_STFLD` | C2 | `r0` = obj, `r1` = value; `a`/`b` = v1's field operands |
| 145 | `R_LDSFLD` | C1 | `r0` = dst; `a`/`b` = v1's static-field operands |
| 146 | `R_STSFLD` | C1 | `r0` = value; `a`/`b` = v1's static-field operands |
| 147 | `R_CASTCLASS` | C2 | `r0` = dst, `r1` = src; `a` = type `EntityRef` (v1 semantics, incl. the catchable `InvalidCastException`) |
| 148 | `R_ISINST` | C2 | `r0` = dst, `r1` = src; `a` = type `EntityRef` |
| 149 | `R_NEWARR` | C2 | `r0` = dst, `r1` = length; `a` = SZArray type `EntityRef`, `b` = element storage kind — as v1 (incl. the negative-length `OverflowException`) |
| 150 | `R_LDLEN` | C2 | `r0` = dst, `r1` = array (length as i32) |
| 151 | `R_LDELEM` | C3 | `r0` = dst, `r1` = array, `r2` = index; `b` = element storage kind |
| 152 | `R_STELEM` | C3 | `r0` = array, `r1` = index, `r2` = value; `b` = element storage kind |

**Control / EH (153–158).**

| value | name | class | operands / semantics |
|-------|------|-------|----------------------|
| 153 | `R_RET_VOID` | C0 | return, no value |
| 154 | `R_RET` | C1 | return `r0` |
| 155 | `R_LEAVE` | C0 | `a` = target; v1's leave machinery (§Exception handling below) |
| 156 | `R_ENDFINALLY` | C0 | end finally/fault, as v1 |
| 157 | `R_THROW` | C1 | throw `r0` |
| 158 | `R_RETHROW` | C0 | `a` = the enclosing catch's method-relative EHTable index, as v1 |

**Reserved (159).**

| value | name | class | operands / semantics |
|-------|------|-------|----------------------|
| 159 | `R_EXT` | C0 | immediate-extension record: `a`/`b` extend the **following** instruction — defined for future ops needing three registers plus two full u32 payloads. Unused; the verifier rejects it |

### Exception handling

- The EHTable record layout is unchanged; all instruction indices — try and
  handler ranges, `R_LEAVE` targets, `R_RETHROW`'s clause index — refer to the
  **register-form stream**.
- **Catch handler entry**: the exception object is written to `regs[tempBase]`
  — a catch's entry depth is 1, so the handler body reads it as the depth-0
  temp. **Finally/fault entry writes no register** (entry depth 0).
- The `R_LEAVE`/`R_ENDFINALLY` machinery behaves as v1 (every finally between
  the leave and its target runs, innermost-first); v1's "the eval stack
  empties" is vacuous here — temps above the target's depth are dead by
  construction. Filter clauses remain unsupported.

### Load-time verification

A register-format image gets **one linear verification scan per method**,
after import binding:

- every register operand `< slotCount + maxStack` (`maxStack` = the simulated
  maximum eval depth, §Register model);
- every branch/leave target `< codeInsnCount`;
- every `R_RETHROW` EH index `< ehCount`;
- every `R_CALL`/`R_CALLVIRT`/`R_NEWOBJ` window:
  `windowBase + consumedCount <= slotCount + maxStack`, with the consumed
  count taken from the callee's method record / import binding (the same
  metadata dispatch uses);
- `R_EXT` and any opcode value past the end of the table reject the load
  (`R_INVALID` verifies — it has no operands — and traps if executed).

The dispatch loop then trusts every operand unchecked: all bounds checking
happens once, outside the hot loop.

## Definition of baseImageAbiHash

A 64-bit FNV-1a hash (offset basis `0xcbf29ce484222325`, prime
`0x100000001b3`) over the UTF-8 bytes of the base image's **canonical ABI
contract** (below). The contract covers every type the base image emits under
`--hotupdate-base`.

**Canonical serialization v1 is symbolic, not numeric.** The emitter never
computes numeric layout in C# (`instanceSize` is emitted as a C++ `sizeof`
expression; the C++ compiler owns layout), so the contract hashes the layout
*inputs* instead: any base change that can move a real offset/size/slot
necessarily changes one of the serialized lines. Numeric binding (field
offsets, method pointers) always happens in the runtime loader against the
live `Dn2CppTypeInfo` tables, never from the manifest.

One leading policy line, then per emitted type, ordered by ordinal comparison
of the type's canonical name (FullName, prefixed by the enclosing-type
qualifier for nested types and suffixed with `[typeArgs]` for a closed generic
specialization), the following `\n`-terminated lines:

```
P canon=<N> layout=<N>
T <canonicalName> : <base canonicalName chain, comma-joined>
L vt=<0|1> expl=<0|1> pack=<N> size=<N> inline=<N> enum=<0|1>
F <declIndex> <name> <type> static=<0|1> exploff=<N> byval=<N>
V <slot> <SigKey>
I <slot> <SigKey>
D <SigKey>
```

- `P` line: the emitter **policies** under which the base image was emitted.
  `canon=` is the generics-canonicalization policy —
  `AbiContract.CanonPolicyVersion` (currently 1) when canonical shared generics
  are enabled (the default), `0` for a `--no-shared-generics` build. Sharing
  changes which concrete function a bound symbol resolves to without moving any
  symbolic line. `layout=` is the field-layout policy —
  `AbiContract.LayoutPolicyVersion` (currently 6) — bumped by any change that
  moves real field offsets/sizes while changing no symbolic `F`/`L`/`V` line
  (field-width narrowing, growth of the runtime `Dn2CppExceptionObject` prefix,
  a repack of a runtime-owned struct). It applies to both sharing modes, so
  bumping `canon=` alone cannot carry it (a non-shared base stamps `canon=0`
  regardless of version).
- `F` lines: instance and static fields in declaration order; `<type>` is the
  transpiler's `TypeDesc` rendering (`Int32`, `System.String`, `T[]`, …).
- `V` lines: every vtable slot, with the slot owner's `SigKey`
  (`Name(paramTypes):retType`).
- `I` lines: an **interface**'s method slots in slot (declaration) order, each
  with the method's `SigKey`, freezing the interface's slot layout the way `V`
  lines freeze a class vtable. Only interface types emit them.
- `D` lines: a **delegate**'s `Invoke` method `SigKey`. It is neither a `V` nor
  an `I` slot, so without this line a base change to a delegate's Invoke shape
  would move no hashed line (§Delegate thunks). Only delegate types emit it.

The sidecar manifest `base-abi.json` (co-produced by the `--hotupdate-base`
build, consumed by `--emit-patch`) is:

```json
{ "contractVersion": 1, "hash": "0x<16 hex digits>",
  "vtables": {
    "<canonicalName>": ["<slot 0 SigKey>", "<slot 1 SigKey>", ...]
  },
  "interfaces": {
    "<interface canonicalName>": ["<slot 0 SigKey>", "<slot 1 SigKey>", ...]
  },
  "instantiations": {
    "<InstantiationKey>": "<mangled registry name>"
  },
  "defaultRefs": {
    "<shim simple name>": "<DefaultRefOutcome>"
  }
}
```

`vtables` carries the vtable layout of every emitted class that has a live
vtable (the same per-slot SigKeys as the contract's V lines, so the manifest
cannot drift from the hash): the converter resolves a patch override to its
frozen slot index here, without loading the base assembly, and bakes the
list's length as the VtableDescTable's `vtableLen`. `interfaces` mirrors that
for every emitted interface (the same per-slot SigKeys as the I lines): the
converter resolves a patch interface-implementation to its frozen slot and
bakes the list's length as the ItfDesc entry's `fullSlotCount`.
`instantiations` maps every emitted **closed generic reference-type**
instantiation's converter-computable lookup key to its mangled registry name
(§Generics on the patch surface).

`defaultRefs` records what the base build's **conditional default-reference**
pass decided for each shipped shim (`DnZlib`, `DnBrotli`, `DnHttp`) — the
`Compilation.DefaultRefOutcome` name: `Injected`, `AlreadyLoaded`,
`TriggerAbsent`, `Suppressed`, or `NotFound`. A base build injects a shim
whenever a trigger assembly is in its reference closure; the patch converter
never injects. That asymmetry is benign (a patch reaches the base only through
name-based imports), so `defaultRefs` is deliberately **not hashed** — a shim
actually *reached* emits types that enter the contract as ordinary
`T`/`L`/`F`/`V` lines anyway. The converter uses it to refuse the **reverse**
direction: a `-r` naming a shim whose recorded outcome is not `Injected` or
`AlreadyLoaded` means the base does not carry it, so names resolved against it
would bake into imports that bind against nothing.
`PatchConverter.CheckDefaultRefSymmetry` rejects that at bake, naming the shim
and the base's verdict, and runs **before** the reference set is loaded. A
sidecar written before this key existed carries no `defaultRefs` object and
asserts nothing.

## Generics on the patch surface

The base image monomorphizes every reachable closed generic instantiation
(whole-program specialization): `List<int>`, `Func<int,int>`, a user `Box<int>`
are each an ordinary emitted type with a mangled name (`List_Int32`,
`Box_Int32`), the generic-ness fully erased. A patch may **use** an
instantiation the base carries — as a local/arg/field/return, a `newobj`, and
instance/static member calls on it — and bind a method into a generic delegate;
the runtime needs no generic machinery, because a monomorphized instantiation
is a plain type bound by its registry name whose members match by
`(name, arity)` on the specialized type.

The converter never loads the base assembly, so it cannot compute the mangled
name; it computes only a **lookup key** and reads the mangled name back from
the manifest's `instantiations` map:

- **InstantiationKey** = the arity-stripped open-definition full name, then the
  closed type arguments rendered by `TypeDesc.ToString`, e.g.
  `System.Collections.Generic.List[Int32]`, `Box[String]`,
  `System.Func[Int32,Int32]`. Both sides compute it from metadata alone (the
  base build from the emitted specialization, the converter from the patch
  IL's TypeSpec). The map's value is the mangled registry name the type-name
  registry is keyed by (`List_Int32`), which the base owns.
- A member reference on a generic instantiation has a **TypeSpecification**
  parent; the converter resolves it to the mangled name and decodes the member
  signature under the instantiation's type arguments, so `List<int>.Add(!0)`
  bakes as `Add(Int32)`.
- Supported surface: instantiations whose type arguments are all
  **non-generic** (a primitive, String, or a non-generic reference type) — the
  key renders identically on both sides. A nested closed-generic argument
  (`List<List<int>>`) would embed the base's mangled inner name the converter
  cannot reproduce and is a converter-side rejection; generic **value-type**
  instantiations (`Nullable<int>`, `KeyValuePair<…>`) are not in the map
  (reference types only) and hit the boundary below; a generic **method** is
  §Generic methods below.

**Missing-AOT-instantiation boundary.** An instantiation the base image never
emitted is absent from the `instantiations` map, so the converter rejects it
with `NotSupportedException` (`--emit-patch` exit 2) — HybridCLR's rule that a
generic instantiation absent from the AOT image is unavailable. The escape
hatch is a **`hotupdate-refs.txt`** root file: a `--hotupdate-base` build reads
it via `--hotupdate-refs <file>` (one root per line, CLR type names, `#`
comments), resolves each open definition by name+arity and its arguments,
instantiates the specialization, and **reaches every one of its methods** (the
base cannot know which members the patch will call), so the whole
instantiation is emitted and listed. A generic type the base uses itself needs
no root; one only the patch uses does.

### Generic methods

A patch may also call a closed **generic method** on a base-image type
(`Counter.Echo<int>`, `Enumerable.Select<int,string>`), reached through a
`MethodSpecification` IL operand. The base monomorphizes each instantiation and
emits **every instantiation into the declaring type's reflection method table
under the one plain name** (`Dn2CppMethodInfo::name` = `Echo`, no suffix), so
`Echo<int>` and `Echo<string>` are two `Echo` rows differing only in signature.
A generic method therefore needs no `instantiations` map entry — the declaring
type binds normally — but the loader's `(name, arity, staticness)` match cannot
tell two same-named instantiations apart. The discriminator is the **`sigShape`**
(§ImportTable / §MethodTable): a `--hotupdate-base` build stamps every reflected
method row with its shape string (`Dn2CppMethodInfo::sigShape`; null in a normal
build — the loader is its only reader), and the converter bakes the import's
`sigShape` from the **substituted** signature (decoding the
`MethodSpecification`'s type arguments and resolving the closed method under a
method-arg generic context, so `Echo<int>`'s `!!0`→`Int32` gives
`(Int32):Int32`). One shared routine renders both strings, so they agree
byte-for-byte and the loader binds each import to its exact instantiation
(§Load step 3).

**Missing-AOT-instantiation boundary (methods).** Unlike a generic type, the
converter cannot see the base's emitted method table, so it does not reject a
missing method instantiation at bake time; instead the loader's method-import
bind is the boundary — a `sigShape` no reflected row carries is a loud
*unresolved method import* at load, never a silent bind onto a sibling
instantiation. A generic method the base program never calls itself is
force-emitted with a **generic-method root** in `hotupdate-refs.txt`, written
`DeclType::Method[arg,...]` (the `::` distinguishing it from a generic-type
root): the `--hotupdate-base` build resolves the declaring class, instantiates
every matching-arity `Method` template at the arguments, and reaches it. The
declaring type must be non-generic; a generic method **on** a generic type,
nested/value-type method-type arguments, and generic **patch** methods are not
supported.

## N2M trampolines (interpreted virtual overrides)

An AOT `callvirt` site loads `receiver->type->vtable[slot]` as a raw function
pointer and calls it with the slot's AOT C++ signature, so pointing a slot at an
interpreted method needs a native bridge with exactly that ABI. A
`--hotupdate-base` build pre-emits one **N2M trampoline per distinct (slot,
signature shape) pair** over the emitted classes' vtables — a set known and
finite at base-build time (the contract's V lines), with the slot number safely
baked in under the slot-freeze rule. Each trampoline (receiver typed as the base
object pointer) packs its arguments into interpreter slots (i32 sign-extended,
f32 widened, references as-is; the array is a native-stack local, so the
conservative collector sees the references) and forwards to
`dn2cpp_interp_vcall(slot, args, argCount)`, converting the result slot back to
the AOT return type.

The build also emits a registration table (`dn2cpp_n2m_trampolines[]` /
`dn2cpp_n2m_trampoline_count`, rows of `{ slot, sigShape, fnPtr }`): the loader
looks each VtableDesc override up by `(slot, the patch method's sigShape)` and
installs the trampoline into the copied vtable. A missing row fails the load
loudly; slots whose signature falls outside the bridged marshalling shapes
(by-value structs, byrefs, unsigned/native ints, …) get no trampoline, and the
converter's method fence rejects such override shapes at bake time.

**Interface trampolines.** A `--hotupdate-base` build pre-emits one **N2M
interface trampoline per (emitted interface, method slot)** with a bridgeable
signature, plus a second registration table (`dn2cpp_n2m_itf_trampolines[]` /
`dn2cpp_n2m_itf_trampoline_count`, rows of `{ itf, slot, fnPtr }`). An interface
method's identity is `(interface, slot)` — the interface pointer disambiguates
two interfaces sharing a slot number — so both are baked into the trampoline
body's `dn2cpp_interp_itfcall(&itf, slot, args, argCount)` call and matched in
the table. The loader installs a trampoline into a patch type's interface slots
array by `(interface type-info, slot)` (§ItfDescTable); `dn2cpp_interp_itfcall`
recovers the patch context from the receiver and resolves `(interface, slot)`
against the receiver's ItfDesc chain to the implementing patch method. An
interface-typed `callvirt` in interpreted code resolves the receiver's slot
through `dn2cpp_resolve_interface` and calls it through the interface method's
invoker thunk (emitted signature-only for interface methods in a
`--hotupdate-base` build).

`dn2cpp_interp_vcall` recovers the patch context from the receiver: the loader
constructs every patch type-info as an extended record (the `Dn2CppTypeInfo`
first, then the owning image + TypeTable index + a magic stamp), invisible to
AOT code, and only patch-type vtable copies ever hold trampolines, so the
downcast is sound by construction. The slot maps to the overriding method by
walking the receiver type's VtableDesc chain (§VtableDescTable) and runs as an
ordinary interpreted frame; an exception it throws unwinds through the
trampoline and the AOT caller frame on the runtime's one exception machine.
Native/interpreted mutual recursion through trampolines consumes native stack
per hop (the interpreter's own depth guard resets at each native boundary).

`CppEmitter` computes the ABI hash during a `--hotupdate-base` build and emits
the constant `dn2cpp_base_image_abi_hash` into the base binary. The converter
stamps the manifest hash into each BPI. The loader checks
`bpi.header.baseImageAbiHash == dn2cpp_base_image_abi_hash`; a mismatch throws
`NotSupportedException`.

## Delegate thunks (interpreted delegate targets)

A delegate is the uniform runtime object `Dn2CppDelegate { target, method,
prev }` (`prev` = the multicast chain, always null on the patch surface —
single-target only). An `Invoke` calls `method(target, args…)` with the
delegate's Invoke C++ ABI (the target is always the first argument; an
instance method's real address takes it as `this`, a static one goes through
an adapter that drops it). A patch method has neither a native address nor an
adapter, so binding one into a base-image delegate needs a native bridge — one
per **bridgeable emitted delegate type** (its Invoke return + parameters all
inside the marshalling surface), pre-emitted by a `--hotupdate-base` build in
two type-info-keyed registration tables (a delegate's Invoke signature is
neither a vtable nor an interface slot, so the type-info pointer is the key):

- `dn2cpp_n2m_delegate_thunks[]` (rows of `{ dg, fn }`): the `method` a
  patch-bound delegate carries. `fn` is a native function with the delegate's
  Invoke C++ ABI and a leading `Dn2CppObject*` target (the **interpreter
  closure**); it packs the invoke arguments into interpreter slots (i32
  sign-extended, f32 widened, references as-is) and forwards into
  `dn2cpp_interp_dgcall(closure, args, argCount)`, converting the result slot
  back to the AOT return type.
- `dn2cpp_dg_invoke_bridges[]` (rows of `{ dg, fn }`): the reverse, for an
  interpreted `Invoke`. `fn` takes the delegate + the raw argument slots,
  unpacks them into the Invoke C++ ABI, and calls the delegate's own emitted
  multicast invoker `dginvoke_<T>` (each `method` landing on a thunk or an AOT
  method), then packs the result back into a slot.

The **interpreter closure** is a loader-built GC object (a `Dn2CppObject`
header + the owning image, the patch MethodTable index, the captured receiver,
and a magic stamp). `ldftn` on a patch method builds it (receiver null); the
delegate `newobj` captures the target into it and installs the delegate's
thunk as `method` with the closure as `target`. `dn2cpp_interp_dgcall`
recovers `(image, methodIdx, boundThis)` from the closure, prepends the bound
`this` for an instance patch method (otherwise the frame is the invoke
arguments alone), and runs the method as an ordinary interpreted frame. An
`ldftn` on a base-image instance method instead builds an "AOT" closure whose
method pointer becomes the delegate's `method` directly (the natural AOT
delegate, no interpreter hop), so a delegate wrapping an AOT method is bound
and invoked without the tables.

The construction site is where every type statement is enforced, because once
the closure's `method` is a raw pointer the interpreter has no further
interceptable point:

- **A non-null captured receiver must be an instance of the patch method's
  declaring patch type**, by the same `dn2cpp_typeinfo_assignable` rule the
  import side uses; a captured receiver reaching a **static** frame is refused
  outright (arity is all a MethodTable record says about staticness, so the two
  disagreeing means the thunk packed one argument too many and the frame's
  first slot holds register garbage).
- **A null captured target for an instance method is refused** with real
  .NET's `ArgumentException` at construction. For an AOT closure the refusal is
  unconditional (`ldftn` rejects a static base-image target, so a null can mean
  nothing else). For a **patch** closure a null target is legitimate — it is how
  the format encodes a *static* patch method — and instance-ness is derived by
  the arity comparison `dn2cpp_interp_dgcall` performs at invoke time: the
  patch method's `argCount` includes `this`, so it exceeds the delegate's
  `Invoke` parameter count by one. That parameter count is read from the bound
  delegate type-info's method table, and a table that cannot answer
  **degrades** rather than refusing — an absent `Invoke` row is not evidence of
  a static method.
- **An AOT closure's `newobj` tests the captured target's SIGNATURE against the
  delegate's `Invoke`**: arity, plus one position rule — a value type must be
  exact (its C++ representation is its own), two reference types go through
  `dn2cpp_typeinfo_assignable` (the rule `isinst` runs, since a method group
  conversion is legal under variance and every reference is one pointer). The
  comparison material is live metadata — the delegate type-info's own `Invoke`
  row and the base-image method row a metadata bind resolved — never the `D`
  contract line, which is a hash input the loader never sees. An **absent**
  `Invoke` row is a refusal here (a delegate with no `Invoke` row is
  uninvokable by anything), where the null-`this` test above degrades on the
  same absence. An intrinsic-table capture carries no method row and needs
  none: `kShapeRefRetObj` is the one shape `ldftn` may take out of that table
  and its C++ signature is exactly `Dn2CppObject* (*)(Dn2CppObject*)`.
- **An AOT closure's `newobj` also tests the captured target against the
  import's declared receiver type**; a failure is the same
  `NotSupportedException` a dispatch loop's call arm raises. `ldftn` is a
  second mouth onto every bound import's function pointer, so the call arms'
  receiver statements would otherwise be reachable around. `ldftn` additionally
  rejects a binding whose call shape holds no plain instance-method pointer: a
  delegate-Invoke import's `fnPtr` is an M2N bridge and an exception-ctor
  import's is a constructor, neither callable through an Invoke thunk.

The **D contract line** (`D <Invoke SigKey>`, per emitted delegate type)
freezes the Invoke signature into `baseImageAbiHash` — without it a base change
to a delegate's Invoke shape would move no hashed line and a stale patch bound
against the old shape would be silently accepted.

Generic delegates (`Func<>`/`Action<>`) are supported when the base image
carries the instantiation (§Generics on the patch surface) and otherwise hit
the missing-AOT-instantiation boundary; multicast (`Delegate.Combine`/`Remove`,
i.e. `+=`/`-=`) is a conversion-time rejection.

## Load & bind (runtime)

`dn2cpp_patch_load(const void* blob, size_t len) -> Dn2CppInterpImage*`:

1. Validate `magic`/`formatVersion`. Check `baseImageAbiHash` (mismatch
   throws).
2. Pin-allocate a `Dn2CppInterpImage` holding `blob` as-is (**code, strings,
   and metadata are referenced in place — no copies**).
3. **Bind pass** (the only proportional allocation): pin-allocate
   `ImportBinding[importCount]` and resolve each `ImportRecord`:
   - Type → the type-name registry (may stay null for a pure intrinsic anchor
     like `System.Console`; a member bind against a null type throws). An
     SZArray record whose CLR array name hits the registry binds the base
     image's precise per-element array type-info here; a miss defers to step 5b
     (never a bind failure — an array value is always a plain reference).
   - Method → first the runtime's **intrinsic import table** (exact declaring
     type + name + staticness + `sigShape`): the hand-curated surface for
     intrinsic types with no emitted bodies — the `Console.WriteLine`
     overloads, the `Object`/`Type`/`Exception` reflection-lite accessors, and
     the string-only `String.Concat` overloads Roslyn emits for `+` chains and
     string-holed interpolation (2/3/4 operands plus the params `string[]`
     fallback; `Concat(object, …)` shapes stay outside, and value-typed
     interpolation holes are a bake-time converter rejection).
     Otherwise `{ fnPtr, invoker, vtableSlot, returnType }` from the declaring
     type's reflection metadata — `.ctor` against the type's own `ctors` table,
     everything else against the `methods` tables up the base chain — matched
     by name + parameter count + staticness, then disambiguated by full
     `sigShape` (§Generic methods). A same-`(name, arity, staticness)` set with
     no `sigShape` match is unresolved, never a silent bind onto a sibling;
     genuine ambiguity survives only among legacy unshaped rows. The `aux1`
     signature run is decoded into per-value marshal descriptors (scalars
     box/unbox across the invoker-thunk boundary; references pass through).

     **Receiver and argument typing.** Every bind above matches by NAME, so a
     re-labelled import would otherwise answer a call site with a different
     base-image method; the call arms therefore type-test before dispatching,
     and a failure is `NotSupportedException`. An intrinsic row on the instance
     reference-return shape carries the **receiver type** its declaring type
     demands; a row on one of the three **reference-argument** shapes carries
     the same statement **per parameter position** (the shapes are not
     homogeneous — the single-string shape covers both `Type.GetType(string)`
     and `String.Concat(string[])`, and the second casts that parameter
     straight to an array header). The scalar shapes carry nothing. A `null`
     argument satisfies every kind, matching the helpers. A metadata bind needs
     no receiver *kind* — it holds the declaring type-info, and the call arms
     test the receiver against it with the rule `isinst` runs. **None of this
     is part of the format**: it is a property of the interpreter's own row
     table, so it moves no version and every existing `.bpi` binds unchanged.
   - Field → the declaring type's `Dn2CppFieldInfo` entry matched by name up
     the base chain; its accessor thunks carry the instance offset / static
     address / lazy-cctor guard, so the binding stores no separate location.
4. **Static storage** (one-shot, proportional to the patch's static-field and
   type counts): one zeroed, **GC-scanned** 8-byte slot per FieldTable
   `staticSlot` (reference slots must stay collector-visible; the array hangs
   off the image, which the append-only image chain roots), plus one lazy-cctor
   started flag per patch type. A `DN2CPP_BPI_TF_HAS_CCTOR` type whose
   `MethodTable[methodStart]` is not its own parameterless method is rejected.
5. **Patch-type construction** (one-shot, proportional to type + field count):
   pin-construct a real `Dn2CppTypeInfo` from each `TypeRecord` — as the
   extended interpreter record (§N2M trampolines), so a trampoline can route
   the receiver back to its image. The base is the resolved `baseRef` type: an
   Import ref binds an AOT base-image class (value types, interfaces,
   sealed/array/delegate bases, and exception-derived bases are rejected), a
   PatchEntity ref an already-constructed patch type of the same image (the
   TypeTable's base-before-derived order guarantees it — a forward patch base
   fails the load), `0xFFFFFFFF` is `System.Object`. **The instance layout is
   computed here, never by the converter**: each instance field of the type's
   FieldTable run is appended after the live base type-info's `instanceSize` —
   for a patch base, the size just computed for it — in declaration order with
   natural alignment (4/8 bytes by field kind), the final size rounds up to 8,
   and the per-field byte offsets + storage kinds live on the image. A type
   with a `VtableDesc` gets a **copy** of the base vtable (`vtableLen` entries;
   for a patch base that copy starts from the base's own patched copy) with
   each overridden slot rewritten to its N2M trampoline; a type without
   overrides shares the base vtable as-is. A type with an ItfDesc run
   (§ItfDescTable) gets an **extended interface map**; a type without one
   shares the base's interface map. The tostring/gethashcode/equals/finalize
   entries are always shared from the base (overriding those is fenced at
   conversion).

   **5b. Array-type binding**: each still-unbound SZArray type record
   (§ImportTable) resolves its element EntityRef — a Primitive code to the
   built-in primitive/String type-info, an Import to its bound base type, a
   PatchEntity to the type-info step 5 just constructed — and constructs an
   array type-info of the same shape the AOT emitter bakes (`ARRAY|SEALED`
   flags, the element type-info, rank 1, base-less), so the shared type tests
   and array allocators treat it exactly like an emitted per-element handle.
   Constructed array type-infos stay per-image and are **not** registered (name
   lookups resolve only base-emitted array types) and carry no SZArray
   interface-dispatch map (see the carve-outs).
6. **Registry registration**: after the whole construction loop (a load that
   fails partway publishes nothing), every patch type-info is added to the
   type-name registry's **dynamic side-chain** (`dn2cpp_type_registry_add`),
   keyed by CLR FullName. Lookups (`Type.GetType(string)` from AOT and
   interpreted code, later loads' bind passes) scan the static registry first —
   a built-in type can never be shadowed — then the chain newest-first:
   **re-loading a same-named patch type makes the newest registration the
   visible one**, while instances of older loads keep their original type-info
   (append-only, no unloading). `GetType()`/`isinst`/`castclass`/catch matching
   need no registration — they read object headers and walk base chains.
   Enumerating surfaces (`Assembly.GetTypes`, the `MakeGenericType` scan) stay
   static-table-only — the runtime-instantiation-template fallback behind that
   scan interns on its own chain and never reads a patch table.
7. Return the `Dn2CppInterpImage*`. **Execution proceeds directly from the
   blob** — instructions from `CodeSection`, strings from `UserStringPool`,
   type/method references via `EntityRef` → (PatchEntity = patch tables /
   Import = `ImportBinding`).

## Deployment (discovery & versioning)

The managed surface is `Dn2Cpp.Runtime.HotUpdate`, three intrinsic drivers
over the same load pipeline (`runtime/core/dn2cpp_interp.cpp`):

| C# | runtime | contract |
|----|---------|----------|
| `Run(string bpiPath)` | `dn2cpp_hotupdate_run` | load one BPI and run its entry method; **entry required** (`entryMethodIdx == 0xFFFFFFFF` throws) |
| `Load(string bpiPath)` | `dn2cpp_hotupdate_load` | load one BPI; **entry optional** — run only if present (a registration-only image is a valid deployment unit) |
| `LoadDirectory(string dirPath)` | `dn2cpp_hotupdate_load_dir` | apply a deployment directory; returns the number of BPIs loaded |

`LoadDirectory` is the deployment flow:

- **Discovery**: enumerate the directory's regular files named `*.bpi`
  (byte-wise, lowercase); everything else is ignored. A missing or unopenable
  directory is the normal fresh-install state and loads nothing (returns 0).
- **Header pre-validation, skip on staleness**: each candidate is whole-read
  once and its 64-byte header checked — length, `magic`, `formatVersion`,
  `baseImageAbiHash`. A failure skips the file with a one-line stderr
  diagnostic and the load continues: patches baked against a previous base
  build are the routine post-base-update deployment state and must never block
  startup (stdout is untouched — it belongs to the program).
- **Ordering**: survivors are applied in ascending (`patchVersion`, byte-wise
  filename) order — never directory enumeration order. The highest version
  therefore registers last, and with the registry's append-only
  newest-registration-wins chain (§Load step 6), its same-name types are the
  ones name lookups resolve to.
- **Per-BPI atomicity**: past the header pre-validation, a failure is
  corruption or a converter bug — not staleness — and throws exactly like a
  single-file load. A mid-directory throw leaves the earlier BPIs published
  (each `dn2cpp_patch_load` registers nothing until its image fully
  constructs, and loading is append-only).
- Each loaded BPI's entry method runs if present (entry-optional, as with
  `Load`).

### Godot (GDExtension)

A GDExtension game deploys through the same three drivers, with one build
requirement: the shared library must be transpiled with `--gdextension` **and**
`--hotupdate-base` together — the interpreter TU resolves
`dn2cpp_base_image_abi_hash` / `dn2cpp_n2m_trampolines` from the generated
code, so a loader call in a non-`--hotupdate-base` image fails to link.
Calling the drivers from `_Ready` onward is safe: the GDExtension init has
already run the runtime init + static constructors on the main thread. Paths
are **OS filesystem paths** — `res://` / `user://` virtual paths are never
resolved inside `System.IO`, here or anywhere else in the Godot lane (a
permanent non-goal; see the `System.IO` rule in `AGENTS.md`). Convert first
with `ProjectSettings.GlobalizePath(...)`, or take the writable root from
`OS.GetUserDataDir()`.

Engine-virtual dispatch reaches patch overrides: in a `--hotupdate-base`
GDExtension image the per-class virtual trampolines (`_ready`/`_process`/…)
make their inner call through the receiver's vtable slot — the same frozen slots
the patch loader rewrites with N2M trampolines. Node-derived patch classes are
instantiable from patch code: the interpreter's `newobj` consults an allocation
hook the Godot layer registers at extension init
(`dn2cpp_interp_set_alloc_hook`; the hook variable lives in the core runtime TU,
so registering it never links the interpreter into non-hotupdate programs),
which backs the instance with an engine object constructed under the nearest
ClassDB-registered AOT ancestor's class name (`object_set_instance` binds it
there; `init_ref` for the RefCounted family) — the engine sees the instance as
that ancestor class, the dn2cpp side sees the patch type-info, whose vtable copy
carries the interpreted overrides.

Interpreted patch code can call the engine-shim surface: a hot-update base
synthesizes a real, invoker-compatible body for every **reachable** skipped
engine-shim method (through the same call lowering an AOT call site gets —
identical ptrcall / marshalling / RefCounted-ownership semantics) and wires it
into the method's reflection fnPtr/invoker rows, so a patch method import
against `Godot.GD` or a `Godot.Object`-derived shim class binds through the
ordinary §Load step-3 path. Fences on that surface:

- only marshalling shapes the Godot call lowering models get a wrapper (an
  unlowerable shape stays bodyless — an unresolved import at load, the same
  missing-AOT boundary as elsewhere);
- only methods the base image **reaches** are wrapped — an engine method only
  patch code calls must be rooted in `hotupdate-refs.txt` (the plain
  `DeclType::Method` root form);
- builtin value-type instance methods (Vector3, Color, …) and the `GD` utility
  class beyond `Print` are outside it (and `GD.Print`'s `params Variant[]`
  signature needs value-type arrays, which the converter fences anyway);
- the container wrappers (`Godot.Collections`) and `Mathf` are not
  skipped-body shims — their `return default` placeholder bodies are emitted
  (dead code: every call site is lowered inline) but a hot-update base excludes
  them from the reflection fnPtr/invoker wiring
  (`IEmitBackend.HasPlaceholderBody`), so a patch import against them is the
  standard unresolved-import load failure rather than a silent bind onto a body
  that computes nothing;
- patch types are not ClassDB-visible (registration happens once at extension
  init and unload/re-registration semantics are unresolved — GDScript `.new()`
  / `.tscn` cannot name a patch class; a patch instance lives under its base's
  registered class).

`gates/build-and-run-hotupdate-godot.sh` exercises the whole flow.

## Versioning and compatibility

- `formatVersion`: +1 on layout-incompatible change; the loader rejects a
  major mismatch.
- `flags`: loaders enforce a supported-flags mask and reject an unknown bit.
- `baseImageAbiHash`: independently of format compatibility, guarantees
  **base-binary compatibility** — a base rebuild that changes layouts/slots
  automatically rejects stale BPIs.
- `patchVersion` (`--patch-version <n>`, default 0 = unversioned): the
  **deployment ordering key** within one compatible base — it decides the apply
  order of a directory load (ascending; filename breaks ties), so the newest
  patch's registrations win. It is deliberately not a compatibility gate:
  staleness is `baseImageAbiHash`'s job.

## Conversion surface (the patch fence)

What the converter (`src/Dn2Cpp.Transpiler/HotUpdate/PatchConverter.cs`, the
`--emit-patch` driver) accepts; everything outside this fence throws
`NotSupportedException` at conversion.

**Types.** Patch types are plain classes deriving from `System.Object`, from an
AOT base-image class, or from another patch class of the same image (the
TypeTable bakes base-before-derived so the loader constructs in one pass),
carrying static and instance fields of scalar
(Int32/Int64/Single/Double/Boolean/Char), String, AOT-reference, patch-class,
or SZArray-over-those type.

**Methods.** Static methods, instance constructors, plain instance methods, or
overrides of base-image virtual methods (including a patch-derived re-override
of a patch base's override — the same frozen slot), all over the same type
fence for parameters, locals, and returns (void included). An override resolves
to its frozen base vtable slot through the `base-abi.json` manifest (keyed by
the nearest AOT ancestor) and bakes into the type's VtableDesc run. New virtual
slot declarations (newslot) and the object overrides
(ToString/GetHashCode/Equals/Finalize — dedicated type-info entries, not vtable
slots) are rejected.

**Fields and initializers.** Static fields bake into the FieldTable with
image-consecutive staticSlot indices; instance fields bake with their per-type
declaration ordinal only — byte offsets and instance sizes are assigned by the
runtime loader against the live base layout (§Load & bind). A `.cctor` bakes as
the type's first MethodTable entry (the has-cctor type flag), run lazily by the
interpreter; a `.ctor` bakes as a normal instance method whose body chains into
the AOT base constructor through an instance-call import (a chain to
`System.Object::.ctor()` folds to dropping the receiver).

**Bodies.** Constants, argument/local access, branches,
arithmetic/comparison/conversion, intra-patch calls (static and instance,
`newobj` on patch types), patch field access, AOT-type access through import
bindings — external static and instance calls (scalar/string/AOT-reference
arguments and returns), `newobj` on AOT reference types, and
`ldfld`/`stfld`/`ldsfld`/`stsfld` via field imports — and exception handling:
try/catch/finally/fault regions baked into EHTable records with
`throw`/`rethrow`/`leave`/`endfinally` (a `newobj` on an exception type binds
to the runtime's `dn2cpp_exception_new` interception — the uniform
message/inner allocation; a patched body constructing a BCL exception that
declares fields of its own reads them back null, the AOT path having since
grown a real ctor call the interpreter does not make). Plus reference-type
`isinst`/`castclass` over patch, AOT class, and SZArray targets, and
single-dimension zero-based arrays of the fenced element kinds:
`newarr`/`ldlen`/`ldelem*`/`stelem*` (typed and short forms normalized to the
canonical `ldelem`/`stelem` with a baked element storage kind), with an array
type encoded as a synthetic SZArray type import carrying the element's
EntityRef.

**Outside the fence** (conversion-time `NotSupportedException`): value-type
instance access (constrained/boxed `this`), boxing (`box`/`unbox.any`),
`ldtoken`/typeof, address-taking (`ldflda`/`ldelema`), jagged/
multi-dimensional arrays, filter clauses (`catch ... when`), and generic
members beyond the closed base-image instantiations of §Generics on the patch
surface — see also the carve-out list below.

## v1 carve-outs (known boundaries)

- **No unloading** (append-only loading; avoids stale patch-type instances).
  Same-name re-loads follow §Load step 6: the newest registration wins in name
  lookups, old instances keep their old type-info.
- **Generic instantiations absent from the AOT image**: without a
  pre-reference in the base (`hotupdate-refs.txt`),
  `dn2cpp_throw_not_supported` (the same AOT boundary as HybridCLR).
- **Inter-patch dependencies**: references are limited to the base image + the
  same BPI (a multi-BPI dependency graph is future work). Patch bases in
  particular must live in the same BPI.
- **Type-system boundaries** (each a conversion-time rejection with a clear
  message unless noted): boxing (`box`/`unbox.any` — and with them value-type
  type tests), `ldtoken`/`typeof` (patch code reaches a `Type` via `GetType()`
  or `Type.GetType(string)` instead), new virtual slots (newslot) and
  ToString/GetHashCode/Equals/Finalize overrides (dedicated type-info entries,
  not vtable slots), exception-derived patch bases — and with them patch types
  in `catch` clauses (C# only catches Exception-derived types) — and filter
  clauses. Closed generic base-image **types** on the patch surface are
  supported (§Generics on the patch surface); generic **methods** on generic
  types, generic patch types, and nested/value-type generic arguments are not.
  Patch types are also not enumerated by `Assembly.GetTypes` (registration is
  name-lookup-only).
- **SZArray carve-outs**: jagged (`T[][]`) and multi-dimensional (`T[,]`)
  arrays, `ldelema` (element address-taking), and value-type elements outside
  the six fenced scalars are conversion-time rejections. A reference-element
  `stelem` performs **no covariance check** — storing a mismatched element into
  a covariantly-viewed array silently succeeds instead of raising
  `ArrayTypeMismatchException`, matching the AOT lane's stelem helpers (one
  behavior across both lanes; `isinst`/`castclass` on array types do check
  element assignability). An array type the base image never emitted
  (patch-class elements always) gets a loader-constructed type-info:
  functionally identical for allocation/access/casts/GetType, but per-image (a
  later load constructs its own — `Type` identity is not shared), not
  resolvable via `Type.GetType(string)`, and without an SZArray
  interface-dispatch map (it cannot be viewed as `IEnumerable<T>`/`IList<T>`
  from AOT code — the missing-AOT-instantiation boundary, surfaced as the
  runtime's interface-dispatch failure rather than a load error).
- **Interface carve-outs**: a patch type may implement a **non-generic
  base-image interface** (present in the manifest) with implicit or explicit
  (`.override`) methods; a patch instance dispatches to the interpreted
  implementation from AOT and interpreted interface-typed callers alike, and
  `isinst`/`as` answer the interface. Conversion-time rejections: declaring an
  interface inside the patch, implementing a generic (or patch-declared)
  interface, and an implementation method whose signature falls outside the
  bridged marshalling shapes (no interface trampoline). A patch type that
  *re-implements* an interface its AOT base already implements is supported
  when it declares the interface itself (a fresh interface entry); overriding
  an interface method purely by inheritance (without re-declaring the interface
  on the patch type) leaves the inherited AOT interface slot in place and is an
  unexercised boundary.
- **Delegate carve-outs**: a patch may bind a method — a patch static method, a
  patch instance method (its receiver captured as the closure's bound `this`),
  or a base-image **instance** method — into a base-image delegate type, invoke
  it from patch code, and hand it across the AOT boundary; delegates flow
  through array/field/arg/return slots as ordinary references. A **generic**
  delegate is supported when the base image carries the instantiation
  (`Func<int,int>`, a user `Mapper<int>` — resolved to its mangled name against
  the manifest); one the base never emitted hits the missing-AOT-instantiation
  boundary unless pre-referenced in `hotupdate-refs.txt`. Conversion-time
  rejections: a **multicast** delegate (`Delegate.Combine`/`Remove`, i.e.
  `+=`/`-=` — only single-target delegates are baked), `ldvirtftn` (a delegate
  over a virtual method), and binding a base-image **static** method (it needs
  the shuffle adapter the interpreter has no symbol for — use an instance
  method). A delegate whose `Invoke` signature falls outside the bridged
  marshalling surface has no thunk/bridge and fails loudly at load, like an
  out-of-surface interface method.
