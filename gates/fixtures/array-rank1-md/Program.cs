using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Linq;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
if (args[0] == "--inspect")
{
    Type owner = Assembly.Load(File.ReadAllBytes(args[1])).GetType("ArrayRankOneOwner")!;
    foreach (var item in owner.GetFields().OrderBy(item => item.Name, StringComparer.Ordinal))
        Console.WriteLine($"field {item.Name}={Display(item.FieldType)};optional={string.Join(",", item.GetOptionalCustomModifiers().Select(Display))}");
    foreach (var item in owner.GetProperties().OrderBy(item => item.Name, StringComparer.Ordinal))
        Console.WriteLine($"property {item.Name}={Display(item.PropertyType)}({string.Join(",", item.GetIndexParameters().Select(parameter => Display(parameter.ParameterType)))})");
    foreach (var item in owner.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .OrderBy(item => item.Name, StringComparer.Ordinal))
    {
        Console.WriteLine($"method {item.Name}={Display(item.ReturnType)}({string.Join(",", item.GetParameters().Select(parameter => Display(parameter.ParameterType)))})");
        foreach (var local in item.GetMethodBody()!.LocalVariables)
            Console.WriteLine($"local {item.Name}/{local.LocalIndex}={Display(local.LocalType)}/{local.IsPinned}");
        if (item.Name.EndsWith("Type", StringComparison.Ordinal) && !item.ContainsGenericParameters)
            Console.WriteLine($"token {item.Name}={Display((Type)item.Invoke(null, null)!)}");
    }
    return;
}
bool metadataOnly = args.Length == 2 && args[1] == "--metadata-only";
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
byte[] unsized = { 0x14, 0x08, 1, 0, 0 };
byte[] sized = { 0x14, 0x08, 1, 1, 5, 0 };
byte[] vector = { 0x1d, 0x08 };
byte[] vectorOfUnsized = { 0x1d, 0x14, 0x08, 1, 0, 0 };
byte[] unsizedOfVector = { 0x14, 0x1d, 0x08, 1, 0, 0 };
byte[] unsizedOfUnsized = { 0x14, 0x14, 0x08, 1, 0, 0, 1, 0, 0 };
byte[] unsizedOfRectangle = { 0x14, 0x14, 0x08, 2, 0, 0, 1, 0, 0 };
byte[] unsizedOfUnsizedOfUnsized = { 0x14, 0x14, 0x14, 0x08, 1, 0, 0, 1, 0, 0, 1, 0, 0 };
var arrays = new[] {
    ("Unsized", unsized), ("Sized", sized), ("Vector", vector), ("VectorOfUnsized", vectorOfUnsized),
    ("UnsizedOfVector", unsizedOfVector), ("UnsizedOfUnsized", unsizedOfUnsized),
    ("UnsizedOfRectangle", unsizedOfRectangle), ("UnsizedOfUnsizedOfUnsized", unsizedOfUnsizedOfUnsized) };
foreach (var (name, signature) in arrays)
{
    BlobBuilder signatureBlob = new();
    signatureBlob.WriteByte(0x06);
    signatureBlob.WriteBytes(signature);
    metadata.AddFieldDefinition(FieldAttributes.Public | FieldAttributes.Static,
        metadata.GetOrAddString(name), metadata.GetOrAddBlob(signatureBlob));
}
if (metadataOnly)
{
    TypeReferenceHandle listRef = metadata.AddTypeReference(coreRef,
        metadata.GetOrAddString("System.Collections.Generic"), metadata.GetOrAddString("List`1"));
    BlobBuilder genericSignature = new();
    new BlobEncoder(genericSignature).FieldSignature().GenericInstantiation(listRef, 1, false)
        .AddArgument().Array(out element, out shape);
    element.Int32();
    shape.Shape(1, ImmutableArray<int>.Empty, ImmutableArray<int>.Empty);
    metadata.AddFieldDefinition(FieldAttributes.Public | FieldAttributes.Static,
        metadata.GetOrAddString("GenericUnsized"), metadata.GetOrAddBlob(genericSignature));

    BlobBuilder modifiedSignature = new();
    var modified = new BlobEncoder(modifiedSignature).FieldSignature();
    modified.CustomModifiers().AddModifier(objectRef, true);
    modified.Array(out element, out shape);
    element.Int32();
    shape.Shape(1, ImmutableArray<int>.Empty, ImmutableArray<int>.Empty);
    metadata.AddFieldDefinition(FieldAttributes.Public | FieldAttributes.Static,
        metadata.GetOrAddString("ModifiedUnsized"), metadata.GetOrAddBlob(modifiedSignature));

    BlobBuilder pointerSignature = new();
    pointerSignature.WriteBytes(new byte[] { 0x06, 0x0f });
    pointerSignature.WriteBytes(unsized);
    metadata.AddFieldDefinition(FieldAttributes.Public | FieldAttributes.Static,
        metadata.GetOrAddString("PointerUnsized"), metadata.GetOrAddBlob(pointerSignature));

    BlobBuilder functionSignature = new();
    functionSignature.WriteBytes(new byte[] { 0x06, 0x1b, 0, 1 });
    functionSignature.WriteBytes(unsized);
    functionSignature.WriteByte(0x10);
    functionSignature.WriteBytes(vectorOfUnsized);
    metadata.AddFieldDefinition(FieldAttributes.Public | FieldAttributes.Static,
        metadata.GetOrAddString("FunctionUnsized"), metadata.GetOrAddBlob(functionSignature));
}

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
MethodBodyStreamEncoder bodyEncoder = new(bodies);
int body = bodyEncoder.AddMethodBody(code, maxStack: 1);
BlobBuilder methodSignature = new();
new BlobEncoder(methodSignature).MethodSignature().Parameters(0,
    result => result.Type().Type(typeRef, false), parameters => { });
MethodDefinitionHandle method = metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static,
    MethodImplAttributes.IL, metadata.GetOrAddString("ArrayType"), metadata.GetOrAddBlob(methodSignature),
    body, MetadataTokens.ParameterHandle(1));
foreach (var (name, signature) in arrays)
{
    TypeSpecificationHandle token = metadata.AddTypeSpecification(metadata.GetOrAddBlob(signature));
    BlobBuilder echoSignature = new();
    echoSignature.WriteBytes(new byte[] { 0, 1 });
    echoSignature.WriteBytes(signature);
    echoSignature.WriteBytes(signature);
    MemberReferenceHandle echoReference = metadata.AddMemberReference(MetadataTokens.TypeDefinitionHandle(2),
        metadata.GetOrAddString("Echo" + name), metadata.GetOrAddBlob(echoSignature));
    BlobBuilder tokenInstructions = new();
    InstructionEncoder tokenCode = new(tokenInstructions);
    tokenCode.OpCode(ILOpCode.Ldnull);
    tokenCode.Call(echoReference);
    tokenCode.OpCode(ILOpCode.Pop);
    tokenCode.OpCode(ILOpCode.Ldtoken);
    tokenCode.Token(token);
    tokenCode.Call(getType);
    tokenCode.OpCode(ILOpCode.Ret);
    int tokenBody = bodyEncoder.AddMethodBody(tokenCode, maxStack: 1);
    metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static, MethodImplAttributes.IL,
        metadata.GetOrAddString(name + "Type"), metadata.GetOrAddBlob(methodSignature),
        tokenBody, MetadataTokens.ParameterHandle(1));

    BlobBuilder echoInstructions = new();
    InstructionEncoder echoCode = new(echoInstructions);
    echoCode.OpCode(ILOpCode.Ldarg_0);
    echoCode.OpCode(ILOpCode.Stloc_0);
    echoCode.OpCode(ILOpCode.Ldloc_0);
    echoCode.OpCode(ILOpCode.Ret);
    BlobBuilder localSignature = new();
    localSignature.WriteBytes(new byte[] { 0x07, 1 });
    if (metadataOnly) localSignature.WriteByte(0x45);
    localSignature.WriteBytes(signature);
    StandaloneSignatureHandle locals = metadata.AddStandaloneSignature(metadata.GetOrAddBlob(localSignature));
    int echoBody = bodyEncoder.AddMethodBody(echoCode, maxStack: 1, localVariablesSignature: locals,
        attributes: MethodBodyAttributes.InitLocals);
    metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static, MethodImplAttributes.IL,
        metadata.GetOrAddString("Echo" + name), metadata.GetOrAddBlob(echoSignature),
        echoBody, MetadataTokens.ParameterHandle(1));

    if (name == "UnsizedOfUnsized")
    {
        BlobBuilder testInstructions = new();
        InstructionEncoder testCode = new(testInstructions);
        testCode.OpCode(ILOpCode.Ldarg_0);
        testCode.OpCode(ILOpCode.Isinst);
        testCode.Token(token);
        testCode.OpCode(ILOpCode.Ldnull);
        testCode.OpCode(ILOpCode.Cgt_un);
        testCode.OpCode(ILOpCode.Ret);
        int testBody = bodyEncoder.AddMethodBody(testCode, maxStack: 2);
        metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static, MethodImplAttributes.IL,
            metadata.GetOrAddString("IsUnsizedOfUnsized"),
            metadata.GetOrAddBlob(new byte[] { 0, 1, 0x02, 0x1c }),
            testBody, MetadataTokens.ParameterHandle(1));

        BlobBuilder castInstructions = new();
        InstructionEncoder castCode = new(castInstructions);
        castCode.OpCode(ILOpCode.Ldarg_0);
        castCode.OpCode(ILOpCode.Castclass);
        castCode.Token(token);
        castCode.OpCode(ILOpCode.Ret);
        int castBody = bodyEncoder.AddMethodBody(castCode, maxStack: 1);
        metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static, MethodImplAttributes.IL,
            metadata.GetOrAddString("CastUnsizedOfUnsized"),
            metadata.GetOrAddBlob(new byte[] { 0, 1, 0x1c, 0x1c }),
            castBody, MetadataTokens.ParameterHandle(1));
    }
}
if (metadataOnly)
{
    BlobBuilder getterSignature = new();
    getterSignature.WriteBytes(new byte[] { 0, 1 });
    getterSignature.WriteBytes(unsized);
    getterSignature.WriteBytes(unsized);
    BlobBuilder getterInstructions = new();
    InstructionEncoder getterCode = new(getterInstructions);
    getterCode.OpCode(ILOpCode.Ldarg_0);
    getterCode.OpCode(ILOpCode.Ret);
    int getterBody = bodyEncoder.AddMethodBody(getterCode, maxStack: 1);
    MethodDefinitionHandle getter = metadata.AddMethodDefinition(
        MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
        MethodImplAttributes.IL, metadata.GetOrAddString("get_IndexedUnsized"),
        metadata.GetOrAddBlob(getterSignature), getterBody, MetadataTokens.ParameterHandle(1));
    BlobBuilder propertySignature = new();
    propertySignature.WriteBytes(new byte[] { 0x08, 1 });
    propertySignature.WriteBytes(unsized);
    propertySignature.WriteBytes(unsized);
    PropertyDefinitionHandle property = metadata.AddProperty(PropertyAttributes.None,
        metadata.GetOrAddString("IndexedUnsized"), metadata.GetOrAddBlob(propertySignature));
    metadata.AddPropertyMap(MetadataTokens.TypeDefinitionHandle(2), property);
    metadata.AddMethodSemantics(property, MethodSemanticsAttributes.Getter, getter);

    TypeSpecificationHandle genericType = metadata.AddTypeSpecification(metadata.GetOrAddBlob(new byte[] { 0x1e, 0 }));
    BlobBuilder genericInstructions = new();
    InstructionEncoder genericCode = new(genericInstructions);
    genericCode.OpCode(ILOpCode.Ldtoken);
    genericCode.Token(genericType);
    genericCode.Call(getType);
    genericCode.OpCode(ILOpCode.Ret);
    int genericBody = bodyEncoder.AddMethodBody(genericCode, maxStack: 1);
    BlobBuilder genericMethodSignature = new();
    new BlobEncoder(genericMethodSignature).MethodSignature(genericParameterCount: 1).Parameters(0,
        result => result.Type().Type(typeRef, false), parameters => { });
    MethodDefinitionHandle genericMethod = metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static,
        MethodImplAttributes.IL, metadata.GetOrAddString("GenericArrayType"),
        metadata.GetOrAddBlob(genericMethodSignature), genericBody, MetadataTokens.ParameterHandle(1));
    metadata.AddGenericParameter(genericMethod, GenericParameterAttributes.None, metadata.GetOrAddString("T"), 0);
    BlobBuilder genericArguments = new();
    genericArguments.WriteBytes(new byte[] { 0x0a, 1 });
    genericArguments.WriteBytes(unsized);
    MethodSpecificationHandle specialized = metadata.AddMethodSpecification(genericMethod, metadata.GetOrAddBlob(genericArguments));
    BlobBuilder specializedInstructions = new();
    InstructionEncoder specializedCode = new(specializedInstructions);
    specializedCode.Call(specialized);
    specializedCode.OpCode(ILOpCode.Ret);
    int specializedBody = bodyEncoder.AddMethodBody(specializedCode, maxStack: 1);
    metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static, MethodImplAttributes.IL,
        metadata.GetOrAddString("MethodSpecType"), metadata.GetOrAddBlob(methodSignature),
        specializedBody, MetadataTokens.ParameterHandle(1));
}
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

static string Display(Type type)
{
    if (type.IsArray) return Display(type.GetElementType()!) + (type.IsSZArray ? "[]" : "[*]");
    if (type.IsPointer) return Display(type.GetElementType()!) + "*";
    if (type.IsByRef) return Display(type.GetElementType()!) + "&";
    if (type.IsFunctionPointer)
        return "fn(" + string.Join(",", type.GetFunctionPointerParameterTypes().Select(Display)) + ")->"
            + Display(type.GetFunctionPointerReturnType());
    if (type.IsGenericType)
        return type.GetGenericTypeDefinition().FullName + "<" + string.Join(",", type.GenericTypeArguments.Select(Display)) + ">";
    return type.FullName!;
}
