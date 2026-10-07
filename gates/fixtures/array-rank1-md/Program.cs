using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
// C# cannot spell rank-one MD TypeSpecs; encode ARRAY rather than SZARRAY.
MetadataBuilder metadata = new();
metadata.AddModule(0, metadata.GetOrAddString("ArrayRankOneLibrary.dll"),
    metadata.GetOrAddGuid(new Guid("4ba7acb8-c891-4b7b-a7ad-a20b3329daa5")), default, default);
metadata.AddAssembly(metadata.GetOrAddString("ArrayRankOneLibrary"), new Version(1, 0, 0, 0),
    default, default, 0, AssemblyHashAlgorithm.None);
AssemblyName core = typeof(object).Assembly.GetName();
AssemblyReferenceHandle coreRef = metadata.AddAssemblyReference(metadata.GetOrAddString(core.Name!),
    core.Version!, default, metadata.GetOrAddBlob(core.GetPublicKeyToken()!), 0, default);
TypeReferenceHandle objectRef = metadata.AddTypeReference(coreRef,
    metadata.GetOrAddString("System"), metadata.GetOrAddString("Object"));
TypeReferenceHandle typeRef = metadata.AddTypeReference(coreRef,
    metadata.GetOrAddString("System"), metadata.GetOrAddString("Type"));
TypeReferenceHandle handleRef = metadata.AddTypeReference(coreRef,
    metadata.GetOrAddString("System"), metadata.GetOrAddString("RuntimeTypeHandle"));

BlobBuilder arraySignature = new();
new BlobEncoder(arraySignature).TypeSpecificationSignature().Array(out var element, out var shape);
element.Int32();
// An explicit lower bound preserves ARRAY identity through metadata writers.
shape.Shape(1, ImmutableArray<int>.Empty, ImmutableArray.Create(0));
TypeSpecificationHandle arraySpec = metadata.AddTypeSpecification(metadata.GetOrAddBlob(arraySignature));
BlobBuilder fieldSignature = new();
new BlobEncoder(fieldSignature).FieldSignature().Array(out element, out shape);
element.Int32();
shape.Shape(1, ImmutableArray<int>.Empty, ImmutableArray.Create(0));
FieldDefinitionHandle field = metadata.AddFieldDefinition(FieldAttributes.Public | FieldAttributes.Static,
    metadata.GetOrAddString("Value"), metadata.GetOrAddBlob(fieldSignature));

BlobBuilder getTypeSignature = new();
new BlobEncoder(getTypeSignature).MethodSignature().Parameters(1,
    result => result.Type().Type(typeRef, false),
    parameters => parameters.AddParameter().Type().Type(handleRef, true));
MemberReferenceHandle getType = metadata.AddMemberReference(typeRef,
    metadata.GetOrAddString(nameof(Type.GetTypeFromHandle)), metadata.GetOrAddBlob(getTypeSignature));
BlobBuilder instructions = new();
InstructionEncoder code = new(instructions);
code.OpCode(ILOpCode.Ldtoken);
code.Token(arraySpec);
code.Call(getType);
code.OpCode(ILOpCode.Ret);
BlobBuilder bodies = new();
int body = new MethodBodyStreamEncoder(bodies).AddMethodBody(code, maxStack: 1);
BlobBuilder methodSignature = new();
new BlobEncoder(methodSignature).MethodSignature().Parameters(0,
    result => result.Type().Type(typeRef, false), parameters => { });
MethodDefinitionHandle method = metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static,
    MethodImplAttributes.IL, metadata.GetOrAddString("ArrayType"), metadata.GetOrAddBlob(methodSignature),
    body, MetadataTokens.ParameterHandle(1));
metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"),
    default, field, method);
metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed,
    default, metadata.GetOrAddString("ArrayRankOneOwner"), objectRef, field, method);
ManagedPEBuilder pe = new(new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll),
    new MetadataRootBuilder(metadata), bodies, flags: CorFlags.ILOnly);
BlobBuilder image = new();
pe.Serialize(image);
byte[] bytes = image.ToArray();
Type emitted = Assembly.Load(bytes).GetType("ArrayRankOneOwner")!.GetField("Value")!.FieldType;
if (emitted.IsSZArray || emitted.GetArrayRank() != 1)
    throw new InvalidOperationException("The fixture must retain its rank-one MD TypeSpec.");
File.WriteAllBytes(args[0], bytes);
