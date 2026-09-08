# ILDiet

ILDiet removes unreachable managed type and method definitions before dn2cpp
constructs its transpilation model. It uses Mono.Cecil to write new assemblies;
input files are never modified.

```sh
dotnet run --project src/ILDiet -c Release -- \
    App.dll -r Library.dll -r System.Private.CoreLib.dll \
    --link-xml link.xml -o stripped

dn2cpp stripped/App.dll -r stripped/Library.dll \
    -r stripped/System.Private.CoreLib.dll \
    --no-ildiet --link-xml stripped/preservation.xml -o generated
```

Pass the complete managed reference set with repeatable `-r` options. ILDiet
does not resolve assemblies from the machine's installed frameworks or a NuGet
cache. dn2cpp's automatic preprocessing resolves its reference set before
invoking ILDiet and passes every output assembly to the transpiler.

`--link-xml` accepts an explicit Unity-format descriptor. Repeatable
`--project-root` searches for files named exactly `link.xml`, excluding `bin`,
`obj`, `.godot`, and `.git`. `--link-feature` enables `com`, `sre`, or `remoting`.
Descriptors and `PreserveAttribute` use dn2cpp's shared preservation reader.
The generated `preservation.xml` carries applicable descriptor rules into a
subsequent manual transpile; include it even when the required methods already
exist in the stripped DLLs.

Executable assemblies retain their entry point and application static
initializers. Non-framework module initializers, native exports, managed native
implementation adapters, and explicit preservation are roots. A library without
an entry point retains its public application surface. Public methods of an
executable are otherwise eligible for removal. Reflection selected only through
runtime strings requires a descriptor or `PreserveAttribute`.

The graph preserves live field layouts, virtual and interface implementations,
generic signatures and constraints, attributes, delegates, and referenced IL
operands. It does not specialize generics or perform dn2cpp intrinsic lowering.
Framework assemblies, GodotSharp, and dn2cpp runtime/codec/HTTP shims are copied
unchanged. They contain runtime dependencies that are introduced during later
transpilation. dn2cpp also requests copies for hot-update base builds.
When a `--cut` selector names a closed generic specialization, ILDiet reports
that it is copying the complete load set and leaves validation to dn2cpp's
original model. Whether that specialization exists depends on model discovery;
an invalid selector still fails before C++ emission. Ordinary non-generic cut
targets are validated on the original metadata and remain eligible for stripping.

Output goes into a dedicated directory. An existing nonempty directory is
accepted only when it carries ILDiet's ownership marker. A failed write leaves
the previous output intact. Rewritten assemblies retain their assembly identity
and resources, have deterministic metadata, and omit PDBs and strong-name
signatures; they are dn2cpp inputs rather than signed distribution assemblies.

The integration protocol is `--request <request.xml> --result <result.xml>`.
The request contains the resolved input/reference paths, descriptor paths,
features, optional full-type roots, and an optional copy-all setting. The result
contains the ordered output assembly paths, effective preservation file, and
whether cut selectors were validated before stripping.
ILDiet runs as a companion process; its Cecil dependency is never transpiled
into the native dn2cpp executable.
