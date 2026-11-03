# ArrayNativeLength: finite contract witness

These finite fixtures supplement, and do not replace, the unchanged themed gate.
Build, metadata guard, emission, and runtime evidence are recorded independently.
No acceptance change or native-pass claim follows from the fixture source.

## Owned additive selection

Copy only `files/` relative paths into the composed source selection. Apply the two
`Program.cs` dispatcher lines from `integration.patch` additively; the two component
patches share the same frozen-base context, so compose their insertions rather than
replacing `Program.cs` with either draft. The ldftn-local SDK project includes new
`.cs` helpers automatically; no project edit is required.

Frozen base: `4e0e0d9a48fda8dc55cc808f081197dfaf186fde`. No existing production source,
seal, sample, gate, or test was changed. Original `ArrayCore` acceptance stays intact.
The runtime witness is standalone under `gates/fixtures/array-native-length-runtime`, because
accessing the genuine nonpublic leaf through direct/calli IL requires rewriting.
It does not remove, truncate, replace, or bypass any themed-bucket section.

## Coverage and boundaries

* Original .13 PE declaration/raw flags/RVA/owner; cold-twice purity and no class
  growth; cold and warm ownership/flags/context/RVA/pinvoke near misses.
* All single-byte raw signature mutations, all truncations, trailing data, and
  structured malformed/modified/alias controls remain metadata only.
* Warm decoded disagreements, primitive aliases, byrefs/pointers/arrays and canon
  placeholders cannot acquire the exact row.
* Exact MemberRef identity through ResolveCallTarget, TryResolveMemberRefMethod,
  ResolveMemberRefMethod, and ResolveFunctionMemberRef; original MethodDef identity; generic caller
  substitution cannot rescue a VAR/MVAR near miss.
* Fresh direct, address/calli and reflection-only target-root models establish
  replacement reachability, not intrinsic-wrapper registration or original IL
  scanning. The reflection-only caller has no target reference. Retained opaque
  row eligibility is tested separately from runtime named lookup.
* `RuntimeTypeInfoSymbol(ClassInfo)` must be null for the leaf owner: the normal
  generated owner type-info, not a new runtime hook, is the attachment route.
* Separate `reflection-only` runtime input has its direct/calli/oracle methods
  physically removed. The GCHandle case also forbids TryGetTarget/SetTarget in
  that input; those real consumers belong only in its direct/calli witness.
* `check-array-native-length-attachment.py` reads default packed or native metadata output
  and follows the real function row and invoker through the owner's cold metadata
  to its generated hot type-info and canonical type object. It checks a genuine
  replacement body, row ABI/flags, and nonempty attached method count. This is
  structural evidence only; CLR/native stdout parity is still required.

The raw metadata reference-holder assembly is never executed. No invalid or
arbitrary handle pointer, freed-copy probe, or historical UNKNOWN fixture is used.
Named lookup is **not presumed unsupported or passed**: the separate source and
emitted-attachment checks are designed to establish it with the later runtime run.

## Later verification recipe (not run)

The parent owns the composed CLI and any authorization to run. Use that exact
built source identity and the already-provisioned .13 SDK; keep new output/log paths
and preserve every input/result. With `FIXTURE` the built `LdftnLocalFixture.dll`,
`TRANSPILER` its matching `Dn2Cpp.Transpiler.dll`, and `CORE` one original PE:

```sh
dotnet "$FIXTURE" --array-native-length-guards "$TRANSPILER" "$CORE" "$NEW_MODEL_DLL"
```

Run that metadata/model helper once per original Linux, win-x64, osx-x64 and
osx-arm64 .13 CoreLib. All four executions are **host helpers reading PE metadata**,
never execution of foreign assemblies. The exact supplied originals are:

* `$W/held-field-tools-astra-v1/dotnet/shared/Microsoft.NETCore.App/10.0.13/System.Private.CoreLib.dll`
* `$W/datetime-platform-guard-astra-v1/corelibs/win-x64/System.Private.CoreLib.dll`
* `$W/datetime-platform-guard-astra-v1/corelibs/osx-x64/System.Private.CoreLib.dll`
* `$W/datetime-platform-guard-astra-v1/corelibs/osx-arm64/System.Private.CoreLib.dll`

Build `ArrayNativeLengthWitness.csproj` only after authorization. Run its original DLL on the
Linux .13 CLR with no argument for the direct/calli oracle and with `reflection-only`
for the named oracle, recording full stdout and exit status. Generate separate new
native inputs from that original Linux witness, never overwriting it:

```sh
dotnet "$FIXTURE" --array-native-length-fixture "$WITNESS_DLL" "$LINUX_CORE" "$DIRECT_INPUT" direct-calli
dotnet "$FIXTURE" --array-native-length-fixture "$WITNESS_DLL" "$LINUX_CORE" "$NAMED_INPUT" reflection-only
```

For **each** generated input, run the parent's normal finite raw and ILDiet paths
through the composed CLI (four emissions/native binaries total). Raw uses
`--no-ildiet`; Diet must use actual ILDiet, not a renamed raw image. Preserve the
original strict compiler/gate acceptance; do not substitute `--measure`, suppress
an error, or execute a metadata-negative holder. Verify each native exit status is
zero and diff its complete stdout byte-for-byte against the matching original CLR
oracle. Authenticate `UnusedDietSentinel` in the original compiler input; the actual Diet
image may remove it or retain exactly the existing raw ILDiet marker on that same
declaration. The proof helper rejects restored live bodies, changed markers, and
wrong sentinel identities using retained metadata-only files. Those negative DLLs
are never compiler inputs or executed programs.

Run the attachment checker against **both reflection-only emissions**:

```sh
python gates/fixtures/ldftn-local/check-array-native-length-attachment.py "$NAMED_GENERATED_DIR"
```

Keep emitted structural results, compiler evidence, build results and runtime
output distinct. The helpers are not a native pass, the focused witnesses are not
a full `ArrayCore` aggregate, and neither component alone is a Medium/premerge pass.
The parent must also retain/run the existing requested themed acceptance unchanged.

The native rewrite uses the supported static callsite ABI with the explicit
receiver as parameter zero for instance ldftn targets. The CLR oracle retains
genuine instance HasThis semantics. This tests the callable receiver function
ABI, not support for native HasThis callsite encoding.
