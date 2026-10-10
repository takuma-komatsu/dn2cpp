using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;

namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private void EmitTypeInfos(CppOutput o) => new TypeMetadataEmitter(this, o).Emit();

    private readonly Dictionary<ClassInfo, List<MethodInfo>> _reflectionDefinitions = new();
    private readonly HashSet<MethodInfo> _metadataOnlyDefinitions = new();
    private readonly HashSet<MethodInfo> _unsupportedDefinitions = new();
    private readonly HashSet<MethodInfo> _describedDefinitions = new();

    // An added definition without a decodable signature contributes no row.
    private bool CanDescribeReflectionDefinition(MethodInfo method)
    {
        if (!_metadataOnlyDefinitions.Contains(method))
            return true;
        if (_unsupportedDefinitions.Contains(method))
            return false;
        if (_describedDefinitions.Contains(method))
            return true;
        try
        {
            // Reject missing layout dependencies before decoding can publish partial shapes.
            new ReflectionDefinitionLayout(this).Check(method);
            method.EnsureSignature();
            _describedDefinitions.Add(method);
            return true;
        }
        catch (NotSupportedException e) when (!Compilation.IsMustEscape(e))
        {
            _unsupportedDefinitions.Add(method);
            return false;
        }
    }

    private sealed class ReflectionLayoutType
    {
        internal Module? Module;
        internal TypeDefinitionHandle Handle;
        internal ReflectionLayoutType[] Arguments = Array.Empty<ReflectionLayoutType>();
        internal PrimitiveTypeCode? Primitive;
        internal ReflectionLayoutType? Element;
        internal TypeKind ElementKind;
        internal int Rank;
        internal bool Open;
        internal string? ExternalName;
        internal bool Intrinsic;
        internal Dictionary<int, int>? Origins;
    }

    private sealed class ReflectionLayoutFrame
    {
        internal readonly int[] Symbols;
        internal readonly bool[] Edges;
        internal readonly bool[] Positive;

        internal ReflectionLayoutFrame(int[] symbols)
        {
            Symbols = symbols;
            Edges = new bool[symbols.Length * symbols.Length];
            Positive = new bool[Edges.Length];
        }
    }

    // This dependency probe reads blobs without instantiating or completing model classes.
    private sealed class ReflectionDefinitionLayout : ISignatureTypeProvider<ReflectionLayoutType, ReflectionLayoutType[]>
    {
        private readonly CppEmitter _e;
        private readonly Compilation _c;
        private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
        private readonly Dictionary<(Module, TypeDefinitionHandle), ReflectionLayoutFrame> _active = new();
        private readonly HashSet<string> _shapeSeen = new(StringComparer.Ordinal);
        private readonly HashSet<(Module, TypeDefinitionHandle)> _shapeActive = new();
        private readonly List<ReflectionLayoutType> _instances = new();
        private readonly List<ReflectionLayoutType> _layouts = new();
        private int _originSequence;
        private bool _checkingLayouts;
        private static readonly ReflectionLayoutType Open = new() { Open = true };
        private static readonly ReflectionLayoutType Leaf = new();

        internal ReflectionDefinitionLayout(CppEmitter emitter)
        {
            _e = emitter;
            _c = emitter._c;
        }

        internal void Check(MethodInfo method)
        {
            var arguments = method.Context.TypeArgs.Select(FromType).ToArray();
            var signature = method.Module.Reader.GetMethodDefinition(method.Handle)
                .DecodeSignature(this, arguments);
            int instanceCount = _instances.Count;
            _checkingLayouts = true;
            Check(signature.ReturnType);
            foreach (var parameter in signature.ParameterTypes)
                Check(parameter);
            // A closed app specialization also joins the next all-app layout seed.
            for (int i = 0; i < instanceCount; i++)
                CheckAppInstance(_instances[i]);
            // Completion decodes comparator and MethodImpl signatures before rendering.
            // Only the original layouts contribute these asks; newly named owners do not recurse.
            var completionLayouts = _layouts.ToList();
            foreach (var layout in completionLayouts)
            {
                CheckVirtualSignatures(layout, renderSlots: false);
                CheckMethodImplSignatures(layout);
            }
            // Newly emitted owners keep ordinary rows too; their signatures need
            // shape checks, never another signature-layout fixpoint.
            var layouts = _layouts.ToList();
            _checkingLayouts = false;
            foreach (var layout in layouts)
                CheckMemberSignatures(layout);
        }

        private void CheckMemberSignatures(ReflectionLayoutType type)
        {
            var module = type.Module!;
            var reader = module.Reader;
            var definition = reader.GetTypeDefinition(type.Handle);
            var existing = _c.Classes.FirstOrDefault(c => c.Module == module && c.Handle == type.Handle
                && c.Context.TypeArgs.Length == type.Arguments.Length
                && c.Context.TypeArgs.Select(FromType).Select(Key).SequenceEqual(type.Arguments.Select(Key)));
            if (existing is not null && (_e.IsOpaque(existing) || existing.IsEnum))
                return;
            CheckVirtualSignatures(type, RendersVirtualSlots(type, existing));
            CheckInterfaceSignatures(type, existing);
            var accessors = new HashSet<MethodDefinitionHandle>();
            foreach (var handle in definition.GetProperties())
            {
                var property = reader.GetPropertyDefinition(handle).GetAccessors();
                if (!property.Getter.IsNil) accessors.Add(property.Getter);
                if (!property.Setter.IsNil) accessors.Add(property.Setter);
            }
            foreach (var handle in definition.GetMethods())
            {
                var method = reader.GetMethodDefinition(handle);
                // Added generic definitions have their own omission decision.
                if (method.GetGenericParameters().Count != 0)
                    continue;
                string name = reader.GetString(method.Name);
                bool ctor = name == ".ctor" && (method.Attributes & System.Reflection.MethodAttributes.Static) == 0;
                if (!ctor && (name is ".ctor" or ".cctor"
                    || !_c.KeepsReflectionMetadata(existing)))
                    continue;
                if (module != _c.AppModule && !_e._hotUpdateBase && method.RelativeVirtualAddress != 0
                    && !accessors.Contains(handle))
                {
                    var model = existing is { MembersReady: true }
                        ? existing.Methods.FirstOrDefault(m => m.Handle == handle) : null;
                    if (model is null || !_c.Reachable.Contains(model) && !_c.KeepsUnreachedRow(model))
                        continue;
                }
                CheckSignature(module, handle, type.Arguments);
            }
        }

        private void CheckSignature(Module module, MethodDefinitionHandle handle, ReflectionLayoutType[] arguments)
        {
            var signature = module.Reader.GetMethodDefinition(handle).DecodeSignature(this, arguments);
            CheckShape(signature.ReturnType);
            foreach (var parameter in signature.ParameterTypes)
                CheckShape(parameter);
        }

        private void CheckMethodImplSignatures(ReflectionLayoutType type)
        {
            var module = type.Module!;
            var reader = module.Reader;
            foreach (var handle in reader.GetTypeDefinition(type.Handle).GetMethodImplementations())
            {
                var method = reader.GetMethodImplementation(handle);
                bool genericDeclaration = method.MethodDeclaration.Kind switch
                {
                    HandleKind.MemberReference => reader.GetBlobReader(reader.GetMemberReference(
                        (MemberReferenceHandle)method.MethodDeclaration).Signature).ReadSignatureHeader().IsGeneric,
                    HandleKind.MethodDefinition => reader.GetMethodDefinition(
                        (MethodDefinitionHandle)method.MethodDeclaration).GetGenericParameters().Count != 0,
                    _ => false,
                };
                if (genericDeclaration)
                    continue;
                if (method.MethodDeclaration.Kind == HandleKind.MemberReference)
                {
                    var declaration = reader.GetMemberReference((MemberReferenceHandle)method.MethodDeclaration);
                    var parent = TypeFromHandle(module, declaration.Parent, type.Arguments);
                    if (declaration.Parent.Kind == HandleKind.TypeReference && parent.Module is null)
                        continue;
                    string? parentName = parent.Module is { } owner && !parent.Handle.IsNil
                        ? RawSignatureProvider.TypeDefinitionName(owner.Reader, parent.Handle) : parent.ExternalName;
                    if (parentName == "System.Object" && reader.GetString(declaration.Name) == "Finalize")
                        continue;
                }
                CheckMethodImplReference(module, method.MethodBody, type.Arguments);
                CheckMethodImplReference(module, method.MethodDeclaration, type.Arguments);
            }
        }

        private void CheckMethodImplReference(Module module, EntityHandle handle, ReflectionLayoutType[] arguments)
        {
            // A MethodDef resolves by identity; only MemberRef resolution asks signatures.
            if (handle.Kind != HandleKind.MemberReference)
                return;
            var reader = module.Reader;
            var member = reader.GetMemberReference((MemberReferenceHandle)handle);
            var parent = TypeFromHandle(module, member.Parent, arguments);
            if (parent.Module is not { } owner || parent.Handle.IsNil)
                return;
            var signature = member.DecodeMethodSignature(this, parent.Arguments);
            CheckShape(signature.ReturnType);
            foreach (var parameter in signature.ParameterTypes)
                CheckShape(parameter);
            string name = reader.GetString(member.Name);
            foreach (var candidate in owner.Reader.GetTypeDefinition(parent.Handle).GetMethods())
            {
                var definition = owner.Reader.GetMethodDefinition(candidate);
                if (definition.GetGenericParameters().Count == 0 && owner.Reader.GetString(definition.Name) == name)
                    CheckSignature(owner, candidate, parent.Arguments);
            }
        }

        private void CheckInterfaceSignatures(ReflectionLayoutType type, ClassInfo? existing)
        {
            if (existing is not null)
            {
                if (existing.IsEnum || _e.SkipsCanonicalMetadata(existing) || _e.IsIntrinsicShaped(existing)
                    || existing.IsInterface || existing.IsAbstract || _e.IsOpaqueShell(existing)
                    || existing.IsValueType && !_c.IsAllocated(existing))
                    return;
            }
            else
            {
                var typeModule = type.Module!;
                var definition = typeModule.Reader.GetTypeDefinition(type.Handle);
                if ((definition.Attributes & (System.Reflection.TypeAttributes.Interface | System.Reflection.TypeAttributes.Abstract)) != 0)
                    return;
                if (!definition.BaseType.IsNil)
                {
                    var baseType = TypeFromHandle(typeModule, definition.BaseType, type.Arguments);
                    string? baseName = baseType.Module is { } module && !baseType.Handle.IsNil
                        ? RawSignatureProvider.TypeDefinitionName(module.Reader, baseType.Handle) : baseType.ExternalName;
                    // An unpublished value type cannot have joined the allocated set.
                    if (baseName is "System.ValueType" or "System.Enum")
                        return;
                }
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var levels = new Queue<ReflectionLayoutType>();
            levels.Enqueue(type);
            while (levels.Count > 0)
            {
                var level = levels.Dequeue();
                if (level.Module is not { } module || level.Handle.IsNil || level.Intrinsic || !seen.Add(Key(level)))
                    continue;
                var reader = module.Reader;
                var definition = reader.GetTypeDefinition(level.Handle);
                string name = RawSignatureProvider.TypeDefinitionName(reader, level.Handle);
                int arity = name.IndexOf('`');
                if (CoreIntrinsics.IsIntrinsicType(arity < 0 ? name : name[..arity]))
                    continue;
                if ((definition.Attributes & System.Reflection.TypeAttributes.Interface) != 0)
                {
                    // Dispatch traps type every interface slot, including unreached default bodies.
                    foreach (var handle in definition.GetMethods())
                        if (reader.GetMethodDefinition(handle).GetGenericParameters().Count == 0)
                            CheckSignature(module, handle, level.Arguments);
                }
                else if (!definition.BaseType.IsNil)
                    levels.Enqueue(TypeFromHandle(module, definition.BaseType, level.Arguments));
                foreach (var handle in definition.GetInterfaceImplementations())
                    levels.Enqueue(TypeFromHandle(module, reader.GetInterfaceImplementation(handle).Interface, level.Arguments));
            }
        }

        private bool RendersVirtualSlots(ReflectionLayoutType type, ClassInfo? existing)
        {
            if (existing is not null)
                return !existing.IsValueType && !existing.IsEnum && !existing.IsInterface
                    && !existing.IsDelegate && !_e.IsOpaque(existing) && !_e.SkipsCanonicalMetadata(existing);
            var typeModule = type.Module!;
            var reader = typeModule.Reader;
            var definition = reader.GetTypeDefinition(type.Handle);
            if ((definition.Attributes & System.Reflection.TypeAttributes.Interface) != 0)
                return false;
            if (definition.BaseType.IsNil)
                return true;
            var baseType = TypeFromHandle(typeModule, definition.BaseType, type.Arguments);
            string? baseName = baseType.Module is { } module && !baseType.Handle.IsNil
                ? RawSignatureProvider.TypeDefinitionName(module.Reader, baseType.Handle) : baseType.ExternalName;
            return baseName is not ("System.ValueType" or "System.Enum" or "System.MulticastDelegate" or "System.Delegate");
        }

        private void CheckVirtualSignatures(ReflectionLayoutType type, bool renderSlots)
        {
            var module = type.Module!;
            var reader = module.Reader;
            var definition = reader.GetTypeDefinition(type.Handle);
            if ((definition.Attributes & System.Reflection.TypeAttributes.Interface) != 0 || definition.BaseType.IsNil)
                return;
            var methods = new Dictionary<MethodDefinitionHandle, string>();
            foreach (var handle in definition.GetMethods())
            {
                var method = reader.GetMethodDefinition(handle);
                if (method.GetGenericParameters().Count != 0
                    || (method.Attributes & System.Reflection.MethodAttributes.Virtual) == 0)
                    continue;
                // Unreached slots still need a typed trap, independently of reflection retention.
                if (renderSlots)
                    CheckSignature(module, handle, type.Arguments);
                if ((method.Attributes & System.Reflection.MethodAttributes.NewSlot) == 0)
                    methods.Add(handle, reader.GetString(method.Name));
            }
            if (methods.Count == 0)
                return;
            var names = methods.Values.ToHashSet(StringComparer.Ordinal);
            var matches = new HashSet<string>(StringComparer.Ordinal);
            var seen = new HashSet<(Module, TypeDefinitionHandle)>();
            var level = TypeFromHandle(module, definition.BaseType, type.Arguments);
            while (level.Module is { } baseModule && !level.Handle.IsNil && seen.Add((baseModule, level.Handle)))
            {
                var baseReader = baseModule.Reader;
                var baseDefinition = baseReader.GetTypeDefinition(level.Handle);
                foreach (var handle in baseDefinition.GetMethods())
                {
                    var method = baseReader.GetMethodDefinition(handle);
                    string name = baseReader.GetString(method.Name);
                    if (method.GetGenericParameters().Count == 0
                        && (method.Attributes & System.Reflection.MethodAttributes.Virtual) != 0 && names.Contains(name))
                    {
                        // Vtable completion asks same-name SigKeys even when the
                        // corresponding reflection body row will be trimmed.
                        CheckSignature(baseModule, handle, level.Arguments);
                        matches.Add(name);
                    }
                }
                if (baseDefinition.BaseType.IsNil)
                    break;
                level = TypeFromHandle(baseModule, baseDefinition.BaseType, level.Arguments);
            }
            foreach (var method in methods)
                if (matches.Contains(method.Value))
                    CheckSignature(module, method.Key, type.Arguments);
        }

        private ReflectionLayoutType FromType(TypeDesc type) => type.Kind switch
        {
            TypeKind.Class => new()
            {
                Module = type.Class!.Module, Handle = type.Class.Handle,
                Arguments = type.Class.Context.TypeArgs.Select(FromType).ToArray(),
                Open = Compilation.ContainsGenericVar(type) || Compilation.ContainsCanonPlaceholder(type),
                Intrinsic = type.Class.IntrinsicCppName is not null,
            },
            TypeKind.Template => new() { Module = type.TemplateModule, Handle = type.TemplateHandle },
            TypeKind.Primitive => new() { Primitive = type.Primitive, Open = type.IsCanonPlaceholder },
            TypeKind.GenericVar => Open,
            TypeKind.SZArray or TypeKind.MDArray or TypeKind.ByRef or TypeKind.Pointer =>
                Wrap(FromType(type.Element!), type.Kind, type.Rank),
            _ => new() { ExternalName = type.ExternalName },
        };

        private static string Key(ReflectionLayoutType type)
        {
            if (type.Element is { } element)
                return type.ElementKind + ":" + type.Rank + "[" + Key(element) + "]";
            if (type.Module is not { } module)
                return type.Primitive is { } primitive ? (type.Open ? "!p" : "p") + (int)primitive
                    : type.Open ? "!" : "e" + type.ExternalName;
            return module.Index + ":" + System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(type.Handle)
                + "[" + string.Join(",", type.Arguments.Select(Key)) + "]";
        }

        private static string OriginKey(ReflectionLayoutType type)
        {
            string origins = type.Origins is null ? "" : string.Join(",", type.Origins.OrderBy(p => p.Key)
                .Select(p => p.Key + ":" + p.Value));
            return origins + "[" + (type.Element is { } element ? OriginKey(element)
                : string.Join(";", type.Arguments.Select(OriginKey))) + "]";
        }

        private void CheckShape(ReflectionLayoutType type)
        {
            if (type.Element is { } element)
            {
                CheckShape(element);
                return;
            }
            if (type.Module is not { } module || type.Handle.IsNil || type.Intrinsic)
                return;
            var identity = (module, type.Handle);
            if (_shapeActive.Contains(identity))
            {
                foreach (var argument in type.Arguments)
                    CheckShape(argument);
                return;
            }
            if (!_shapeSeen.Add(Key(type)))
                return;
            _shapeActive.Add(identity);
            try
            {
                var reader = module.Reader;
                var definition = reader.GetTypeDefinition(type.Handle);
                if (!definition.BaseType.IsNil)
                    CheckShape(TypeFromHandle(module, definition.BaseType, type.Arguments));
                foreach (var handle in definition.GetInterfaceImplementations())
                    CheckShape(TypeFromHandle(module, reader.GetInterfaceImplementation(handle).Interface, type.Arguments));
            }
            finally
            {
                _shapeActive.Remove(identity);
            }
        }

        private void CheckAppInstance(ReflectionLayoutType type)
        {
            if (type.Module == _c.AppModule && !type.Open)
                Check(type);
        }

        private void Check(ReflectionLayoutType type)
        {
            if (type.Element is { } element)
            {
                Check(element);
                return;
            }
            if (type.Module is not { } module || type.Handle.IsNil || type.Intrinsic)
                return;
            if (type.Open)
            {
                CheckShape(type);
                return;
            }
            var identity = (module, type.Handle);
            if (_active.TryGetValue(identity, out var origins))
            {
                if (ExpandsCycle(type.Arguments, origins))
                    throw new NotSupportedException("Self-expanding reflection definition layout");
                foreach (var argument in type.Arguments)
                    Check(argument);
                return;
            }
            // Equal actual arguments can still carry different formal dependencies.
            if (!_seen.Add(Key(type) + "|" + OriginKey(type)))
                return;
            var symbols = new int[type.Arguments.Length];
            var arguments = new ReflectionLayoutType[type.Arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
            {
                symbols[i] = _originSequence++;
                arguments[i] = WithOrigin(type.Arguments[i], symbols[i]);
            }
            _active.Add(identity, new ReflectionLayoutFrame(symbols));
            try
            {
                var reader = module.Reader;
                var definition = reader.GetTypeDefinition(type.Handle);
                if (!definition.BaseType.IsNil)
                    Check(TypeFromHandle(module, definition.BaseType, arguments));
                foreach (var handle in definition.GetInterfaceImplementations())
                    Check(TypeFromHandle(module, reader.GetInterfaceImplementation(handle).Interface, arguments));
                string fullName = RawSignatureProvider.TypeDefinitionName(reader, type.Handle);
                int arity = fullName.IndexOf('`');
                if (CoreIntrinsics.IsIntrinsicType(arity < 0 ? fullName : fullName[..arity]))
                    return;
                _layouts.Add(type);
                foreach (var handle in definition.GetFields())
                    Check(reader.GetFieldDefinition(handle).DecodeSignature(this, arguments));
            }
            finally
            {
                _active.Remove(identity);
            }
        }

        private static ReflectionLayoutType WithOrigin(ReflectionLayoutType type, int symbol)
        {
            var origins = type.Origins is null ? new Dictionary<int, int>() : new Dictionary<int, int>(type.Origins);
            origins[symbol] = 0;
            return new ReflectionLayoutType
            {
                Module = type.Module, Handle = type.Handle, Arguments = type.Arguments,
                Primitive = type.Primitive, Element = type.Element, ElementKind = type.ElementKind,
                Rank = type.Rank, Open = type.Open, ExternalName = type.ExternalName,
                Intrinsic = type.Intrinsic, Origins = origins,
            };
        }

        private static Dictionary<int, int>? NestedOrigins(IEnumerable<ReflectionLayoutType> arguments)
        {
            Dictionary<int, int>? result = null;
            foreach (var argument in arguments)
                if (argument.Origins is { } origins)
                    foreach (var pair in origins)
                    {
                        result ??= new Dictionary<int, int>();
                        int depth = pair.Value + 1;
                        if (!result.TryGetValue(pair.Key, out int previous) || depth > previous)
                            result[pair.Key] = depth;
                    }
            return result;
        }

        // Paths through different fields compose. Positive nesting grows only
        // on a cycle; constants, permutations and converging resets do not.
        private static bool ExpandsCycle(ReflectionLayoutType[] arguments, ReflectionLayoutFrame frame)
        {
            var symbols = frame.Symbols;
            int count = symbols.Length;
            var edges = frame.Edges;
            var positive = frame.Positive;
            for (int i = 0; i < Math.Min(count, arguments.Length); i++)
                if (arguments[i].Origins is { } origins)
                    for (int j = 0; j < count; j++)
                        if (origins.TryGetValue(symbols[j], out int depth))
                        {
                            edges[i * count + j] = true;
                            positive[i * count + j] |= depth > 0;
                        }
            for (int k = 0; k < count; k++)
                for (int i = 0; i < count; i++)
                    for (int j = 0; j < count; j++)
                        edges[i * count + j] |= edges[i * count + k] && edges[k * count + j];
            for (int i = 0; i < count; i++)
                for (int j = 0; j < count; j++)
                    if (positive[i * count + j] && edges[j * count + i])
                        return true;
            return false;
        }

        private ReflectionLayoutType TypeFromHandle(Module module, EntityHandle handle, ReflectionLayoutType[] arguments) => handle.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeFromDefinition(module.Reader, (TypeDefinitionHandle)handle, 0),
            HandleKind.TypeReference => GetTypeFromReference(module.Reader, (TypeReferenceHandle)handle, 0),
            HandleKind.TypeSpecification => GetTypeFromSpecification(module.Reader, arguments, (TypeSpecificationHandle)handle, 0),
            _ => throw new NotSupportedException("Unsupported reflection definition layout dependency"),
        };

        public ReflectionLayoutType GetGenericInstantiation(ReflectionLayoutType genericType, ImmutableArray<ReflectionLayoutType> typeArguments)
        {
            if (genericType.Module is not { } module || genericType.Handle.IsNil)
                throw new NotSupportedException($"Generic instantiation of {genericType.ExternalName} is not supported yet (external generic types)");
            var reader = module.Reader;
            var definition = reader.GetTypeDefinition(genericType.Handle);
            string name = reader.GetString(definition.Name);
            int arity = name.IndexOf('`');
            string ns = reader.GetString(definition.Namespace);
            string fullName = (ns.Length == 0 ? "" : ns + ".") + (arity < 0 ? name : name[..arity]);
            var arguments = typeArguments.ToArray();
            var instance = new ReflectionLayoutType
            {
                Module = module, Handle = genericType.Handle, Arguments = arguments,
                Open = arguments.Any(a => a.Open),
                Origins = NestedOrigins(arguments),
                Intrinsic = (CoreIntrinsics.IntrinsicGenericCppType(fullName, arguments.Select(a =>
                    a.Primitive is { } primitive ? TypeDesc.MakePrimitive(primitive)
                        : TypeDesc.MakeGenericVar(0, isMethod: false)).ToArray())
                    ?? _c.AdoptedAsyncCpp(module, genericType.Handle)) is not null,
            };
            _instances.Add(instance);
            // Canonical signature decoding drains every specialization's shape,
            // including arguments hidden by an intrinsic or function-pointer result.
            CheckShape(instance);
            if (_checkingLayouts)
                CheckAppInstance(instance);
            return instance;
        }

        public ReflectionLayoutType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
            FromType(_c.GetTypeDescForDefinition(_c.ModuleOf(reader), handle));

        public ReflectionLayoutType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
            _c.ResolveTypeRef(_c.ModuleOf(reader), handle) is { } resolved ? FromType(resolved)
                : new() { ExternalName = RawSignatureProvider.TypeReferenceName(reader, handle) };

        public ReflectionLayoutType GetTypeFromSpecification(MetadataReader reader, ReflectionLayoutType[] genericContext,
            TypeSpecificationHandle handle, byte rawTypeKind) => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
        public ReflectionLayoutType GetGenericTypeParameter(ReflectionLayoutType[] genericContext, int index) =>
            index < genericContext.Length ? genericContext[index] : Open;
        public ReflectionLayoutType GetGenericMethodParameter(ReflectionLayoutType[] genericContext, int index) => Open;
        public ReflectionLayoutType GetPrimitiveType(PrimitiveTypeCode typeCode) => new() { Primitive = typeCode };
        private static ReflectionLayoutType Wrap(ReflectionLayoutType element, TypeKind kind, int rank = 0) =>
            new() { Element = element, ElementKind = kind, Rank = rank, Open = element.Open,
                Origins = NestedOrigins(new[] { element }) };
        public ReflectionLayoutType GetArrayType(ReflectionLayoutType elementType, ArrayShape shape) => Wrap(elementType, TypeKind.MDArray, shape.Rank);
        public ReflectionLayoutType GetSZArrayType(ReflectionLayoutType elementType) => Wrap(elementType, TypeKind.SZArray);
        public ReflectionLayoutType GetByReferenceType(ReflectionLayoutType elementType) => Wrap(elementType, TypeKind.ByRef);
        public ReflectionLayoutType GetPointerType(ReflectionLayoutType elementType) => Wrap(elementType, TypeKind.Pointer);
        public ReflectionLayoutType GetFunctionPointerType(MethodSignature<ReflectionLayoutType> signature) => Leaf;
        public ReflectionLayoutType GetModifiedType(ReflectionLayoutType modifier, ReflectionLayoutType unmodifiedType, bool isRequired) => unmodifiedType;
        public ReflectionLayoutType GetPinnedType(ReflectionLayoutType elementType) => elementType;
    }

    // Cache only raw definitions: planning may still add closed rows to cls.Methods.
    private List<MethodInfo> ReflectionMethods(ClassInfo cls)
    {
        if (!_reflectionDefinitions.TryGetValue(cls, out var definitions))
        {
            definitions = new List<MethodInfo>();
            if (!cls.Handle.IsNil)
            {
                var reader = cls.Module.Reader;
                foreach (var handle in reader.GetTypeDefinition(cls.Handle).GetMethods())
                {
                    var definition = reader.GetMethodDefinition(handle);
                    if (definition.GetGenericParameters().Count == 0)
                        continue;
                    var method = new MethodInfo
                    {
                        DeclaringClass = cls, Name = reader.GetString(definition.Name),
                        Module = cls.Module, Handle = handle, Context = cls.Context,
                        Attributes = definition.Attributes, ImplAttributes = definition.ImplAttributes,
                        Rva = definition.RelativeVirtualAddress,
                    };
                    _metadataOnlyDefinitions.Add(method);
                    definitions.Add(method);
                }
            }
            _reflectionDefinitions[cls] = definitions;
        }
        var methods = cls.Methods.Where(m => !(_c.SharedGenericsEnabled
            && m.Context.MethodArgs.Any(Compilation.ContainsCanonPlaceholder))).ToList();
        var seen = methods.Select(m => m.Handle).ToHashSet();
        foreach (var definition in definitions)
            if (seen.Add(definition.Handle))
                methods.Add(definition);
        return methods;
    }

    private bool KeepsReflectionMethodRow(ClassInfo cls, MethodInfo method,
        HashSet<MethodDefinitionHandle> propertyAccessors)
    {
        bool ctor = method.Name == ".ctor" && !method.IsStatic;
        bool initializer = method.Name == ".cctor" && method.IsStatic && _c.NamedDelegateBindingUsed;
        if (!ctor && (!_c.KeepsReflectionMetadata(cls)
            || (!initializer && (method.Name == ".ctor" || method.Name == ".cctor"))
            || (_c.SharedGenericsEnabled && method.Context.MethodArgs.Any(Compilation.ContainsCanonPlaceholder))))
            return false;
        if (cls.Module != _c.AppModule && !_hotUpdateBase && method.Rva != 0
            && !_c.Reachable.Contains(method) && !_c.KeepsUnreachedRow(method)
            && !propertyAccessors.Contains(method.Handle))
            return false;
        return CanDescribeReflectionDefinition(method);
    }

    private static HashSet<MethodDefinitionHandle> PropertyAccessorHandles(ClassInfo cls)
    {
        // Accessor descriptions survive trimming so visible properties retain their accessors.
        var result = new HashSet<MethodDefinitionHandle>();
        if (cls.Handle.IsNil)
            return result;
        var reader = cls.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(cls.Handle).GetProperties())
        {
            var accessors = reader.GetPropertyDefinition(handle).GetAccessors();
            if (!accessors.Getter.IsNil) result.Add(accessors.Getter);
            if (!accessors.Setter.IsNil) result.Add(accessors.Setter);
        }
        return result;
    }

    /// <summary>Renders the type-metadata section of the emission: the enum and
    /// generic-open-definition type-infos, the shared-generics rgctx tables, and
    /// every class's metadata block (vtable, interface dispatch tables, reflection
    /// field/method/ctor/property tables, attribute factories, invoker thunks,
    /// ti_/ty_ definitions). One instance serves one emission: pool sequence
    /// numbers land in output bytes, so the class-major rendering order and the
    /// monotonic per-kind counters are part of the output contract (see the
    /// class-major loop in <see cref="Emit"/>).</summary>
    private sealed class TypeMetadataEmitter
    {
        private readonly CppEmitter _e;
        private readonly Compilation _c;
        private readonly CppOutput _o;
        // The active metadata sink: _o.Data for the shared sections; the
        // class-major loop in Emit points it at a per-class scratch block while
        // the renderers run, then back (see the loop comment there).
        private StringBuilder _sb;

        // The grouped instantiations whose rgctx_ table was emitted (shared
        // generics), read by RenderTypeInfo to wire the type-info's trailing
        // rgctx member.
        private readonly HashSet<ClassInfo> _rgctxTableSyms = new();
        // ---- per-class metadata state, shared by the renderers below ----
        // Interface dispatch tables emitted for the current class, consumed by its
        // type-info initializer within the same block.
        private readonly Dictionary<ClassInfo, List<ClassInfo>> _itfTables = new();
        // Emitted row count per class — the real rows plus any canonical alias
        // rows (shared generics), which the type-info's interfaceCount covers.
        private readonly Dictionary<ClassInfo, int> _itfRowCounts = new();
        // The class's interface-entry table symbol — a pooled itfspool_ array
        // (see RenderItfTables), consumed by the same class's type-info
        // initializer within the same rendered block (or, when the table
        // deduplicated globally, resolved through its generated.h extern).
        private readonly Dictionary<ClassInfo, string> _itfTabSyms = new();
        private readonly Dictionary<ClassInfo, (string Expr, int Count)> _fieldTabs = new();
        private readonly Dictionary<ClassInfo, (string Expr, int Count)> _methodTabs = new();
        private readonly Dictionary<ClassInfo, (string Expr, int Count)> _ctorTabs = new();
        private readonly Dictionary<ClassInfo, string> _initializerRows = new();
        private readonly Dictionary<ClassInfo, (string Expr, int Count)> _propTabs = new();
        // The methods under an Object member name that got a method row, per class, for
        // the class's DN2CPP_TF_OBJECT_MEMBER_ROWS decision (CarriesObjectMemberRows).
        private readonly Dictionary<ClassInfo, HashSet<MethodDefinitionHandle>> _objectMemberRows = new();
        // A method's address within its emitted table ("&methtab_X[k]"), so a property's
        // accessor can reference its method-table entry.
        private readonly Dictionary<MethodInfo, string> _memberAddr = new();
        // Signature-deduplicated invoker thunks: keyed by C++ ABI shape so
        // every member with the same (static?, param/return ABI) shares one thunk.
        // The thunks are file-local (static), so the dedup scope is one metadata
        // chunk TU: the set is cleared whenever the class-major loop below rolls to
        // a fresh chunk, making each chunk define every thunk it references
        // (duplicate static definitions across TUs are legal and deterministic).
        private readonly HashSet<string> _invokerThunks = new(System.StringComparer.Ordinal);
        // The CppNames of the delegate classes given a multicast invoker (dginvoke_*).
        // Membership only: a hash set's order must not reach the output.
        private HashSet<string>? _delegateInvokerNames;
        // Content-deduplicated parameter tables, keyed by initializer text. Unlike
        // the invoker thunks these pools are GLOBAL (whole-emission, surviving chunk
        // rolls): the first chunk to mint an initializer defines the array with
        // external linkage and declares it in _o.Header, every later chunk references
        // it by name. Sound because pool naming is deterministic (one monotonic
        // counter per kind, claimed in TopoOrder first-encounter order, so definition
        // sites reproduce byte-identically — the self-host fixpoint relies on it) and
        // every symbol a shareable initializer names resolves globally; an initializer
        // naming a file-local symbol (ubthunk_, attribute factories) embeds its
        // uniquely named owner, so it is unique by construction.
        private readonly Dictionary<string, string> _parmPools = new(System.StringComparer.Ordinal);
        private int _parmPoolSeq;
        // Same pattern for interface slot tables (itfpool_) and generic-argument
        // vectors (genargpool_). Both are address-agnostic: interface dispatch
        // compares Dn2CppInterfaceEntry.itf and only indexes .slots, and every
        // genericArgs consumer (MakeGenericType candidate matching,
        // GetGenericArguments, array-element variance) reads the elements — no
        // runtime path keys on either array's address, so byte-identical
        // initializers can share one external-linkage array across the emission.
        private readonly Dictionary<string, string> _itfPools = new(System.StringComparer.Ordinal);
        private int _itfPoolSeq;
        private readonly Dictionary<string, string> _genargPools = new(System.StringComparer.Ordinal);
        private int _genargPoolSeq;
        // Interface-entry tables (itfspool_) intern the same way — rows pair &ti_
        // handles with the pooled slot tables above, and table address identity is
        // not load-bearing (the hot-update loader itself aliases whole tables via
        // ti->interfaces = base->interfaces). The global itfpool_ names feed this
        // pool's keys, so cross-chunk slot sharing is what makes entry tables
        // comparable across chunks at all.
        private readonly Dictionary<string, string> _itfsPools = new(System.StringComparer.Ordinal);
        private int _itfsPoolSeq;
        // Per-slot named trap stubs (slotmiss_). A dispatch slot whose return type is a
        // by-value struct cannot read its receiver out of argument 0 (an indirect return
        // spends that register on the caller's hidden result buffer), so the shared
        // receiver-reading traps would report "(unknown)"; the slot's descriptor is baked
        // into a stub instead. The ambiguous-slot stubs (slotambig_) share the map.
        // Deduplicated per chunk on (reporter, descriptor) text — the file-local statics
        // reset on a chunk roll like the pools above — while the sequence counter stays
        // monotonic across the emission so names are unique and deterministic.
        private readonly Dictionary<string, string> _slotStubs = new(System.StringComparer.Ordinal);
        private int _slotStubSeq;
        private readonly List<string> _ambiguousBindings = new();
        // Per-row invoker trap stubs (invmiss_), the reflection-invoker sibling of the
        // slotmiss_ map above: a member-table row whose invoker thunk cannot be
        // materialized (InvokerThunkBlocker — its signature names a by-value struct
        // the emit set never declared) gets a stub with the invoker ABI that raises
        // the catchable NotSupportedException naming the method and the missing type,
        // so the row stays bindable by name (the hot-update loader requires a non-null
        // invoker) and the failure moves from the C++ compile to the actual invoke.
        // Same scoping rules as _slotStubs: per-chunk map (file-local statics),
        // sequence counter monotonic across the emission.
        private readonly Dictionary<string, string> _invokerMissStubs = new(System.StringComparer.Ordinal);
        private int _invokerMissSeq;
        // The C++ struct names the layout emission declares (every emitted class's
        // CppStructName — the set EmitStructs defines), computed once on first use:
        // this emitter runs strictly after the emit set is final (see the class doc),
        // so the snapshot cannot go stale. Consumed by the InvokerThunkBlocker test
        // at the two invoker-thunk call sites in BuildMemberTable.
        private HashSet<string>? _declaredStructs;
        private IReadOnlySet<string> DeclaredStructs =>
            _declaredStructs ??= _e.EmittedClasses.Select(c => c.CppStructName)
                .ToHashSet(System.StringComparer.Ordinal);
        // The enums whose ti_ is actually emitted — snapshotted by Emit once the
        // enum section has run (see the assignment there).
        private HashSet<ClassInfo> _emittedEnums = new();
        private readonly Dictionary<MethodInfo, ModifierSignature?> _modifierSignatures = new();

        private sealed class ModifiedType
        {
            internal TypeDesc Type { get; }
            internal TypeDesc[] Required { get; }
            internal TypeDesc[] Optional { get; }

            internal ModifiedType(TypeDesc type, TypeDesc[]? required = null, TypeDesc[]? optional = null)
            {
                Type = type;
                Required = required ?? Array.Empty<TypeDesc>();
                Optional = optional ?? Array.Empty<TypeDesc>();
            }
        }

        private sealed class ModifierSignature
        {
            internal ModifiedType ReturnType { get; }
            internal System.Collections.Immutable.ImmutableArray<ModifiedType> ParameterTypes { get; }

            internal ModifierSignature(MethodSignature<ModifiedType> signature)
            {
                ReturnType = signature.ReturnType;
                ParameterTypes = signature.ParameterTypes;
            }
        }

        /// <summary>Signature decoder that keeps the custom modifiers which the main
        /// TypeDesc decoder deliberately erases. A modifier belongs to the signature
        /// layer it wraps; composing an array/pointer/generic type therefore does not
        /// promote a nested element modifier to the parameter itself.</summary>
        private sealed class ModifierSignatureProvider : ISignatureTypeProvider<ModifiedType, object?>
        {
            private readonly SignatureProvider _types;

            internal ModifierSignatureProvider(SignatureProvider types) => _types = types;

            private static ModifiedType Plain(TypeDesc type) => new(type);

            public ModifiedType GetPrimitiveType(PrimitiveTypeCode typeCode) => Plain(_types.GetPrimitiveType(typeCode));
            public ModifiedType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
                Plain(_types.GetTypeFromDefinition(reader, handle, rawTypeKind));
            public ModifiedType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
                Plain(_types.GetTypeFromReference(reader, handle, rawTypeKind));
            public ModifiedType GetSZArrayType(ModifiedType elementType) => Plain(_types.GetSZArrayType(elementType.Type));
            public ModifiedType GetArrayType(ModifiedType elementType, ArrayShape shape) => Plain(_types.GetArrayType(elementType.Type, shape));
            public ModifiedType GetByReferenceType(ModifiedType elementType) => Plain(_types.GetByReferenceType(elementType.Type));
            public ModifiedType GetPointerType(ModifiedType elementType) => Plain(_types.GetPointerType(elementType.Type));
            public ModifiedType GetFunctionPointerType(MethodSignature<ModifiedType> signature) => Plain(TypeDesc.MakeFunctionPointer());
            public ModifiedType GetGenericInstantiation(ModifiedType genericType,
                System.Collections.Immutable.ImmutableArray<ModifiedType> typeArguments)
            {
                var args = new TypeDesc[typeArguments.Length];
                for (int i = 0; i < args.Length; i++) args[i] = typeArguments[i].Type;
                return Plain(_types.GetGenericInstantiation(genericType.Type,
                    System.Collections.Immutable.ImmutableArray.Create(args)));
            }
            public ModifiedType GetGenericMethodParameter(object? genericContext, int index) =>
                Plain(_types.GetGenericMethodParameter(genericContext, index));
            public ModifiedType GetGenericTypeParameter(object? genericContext, int index) =>
                Plain(_types.GetGenericTypeParameter(genericContext, index));
            public ModifiedType GetModifiedType(ModifiedType modifier, ModifiedType unmodifiedType, bool isRequired)
            {
                var prior = isRequired ? unmodifiedType.Required : unmodifiedType.Optional;
                var next = new TypeDesc[prior.Length + 1];
                next[0] = modifier.Type;
                Array.Copy(prior, 0, next, 1, prior.Length);
                return isRequired
                    ? new ModifiedType(unmodifiedType.Type, next, unmodifiedType.Optional)
                    : new ModifiedType(unmodifiedType.Type, unmodifiedType.Required, next);
            }
            public ModifiedType GetPinnedType(ModifiedType elementType) => Plain(_types.GetPinnedType(elementType.Type));
            public ModifiedType GetTypeFromSpecification(MetadataReader reader, object? genericContext,
                TypeSpecificationHandle handle, byte rawTypeKind) =>
                reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
        }

        /// <summary>The function pointer type that <paramref name="type"/>, parameter
        /// <paramref name="parameter"/> of <paramref name="method"/> (-1 for its return),
        /// is or ends at through pointer and by-ref levels, spelled from the metadata
        /// signature; null for any other type or an undecodable signature. Open
        /// spelling remains Invoke's descriptor, but cannot identify a binding.</summary>
        private (string Key, string Name, bool Known, BindingSignature Binding)? FunctionPointerSpelling(MethodInfo method, int parameter, TypeDesc type)
        {
            var t = type;
            while (t is { Kind: TypeKind.ByRef or TypeKind.Pointer, IsFunctionPointer: false, Element: { } element })
                t = element;
            if (!t.IsFunctionPointer)
                return null;
            var signature = _e.ReflectionSignature(method);
            if (signature is not { } decoded)
                return null;
            var spelled = parameter < 0 ? decoded.ReturnType
                : parameter < decoded.ParameterTypes.Length ? decoded.ParameterTypes[parameter] : null;
            if (spelled?.FunctionPointer is not { } f)
                return null;
            if (f.Open)
                _e.NoteBindingFunctionPointer(method, parameter, type, f.Binding);
            return (f.Key, f.Name, !f.Open, f.Binding);
        }

        private (string Key, string Name, bool Known, BindingSignature Binding)? FunctionPointerSpelling(ClassInfo cls,
            FieldDefinitionHandle handle, TypeDesc type)
        {
            while (type is { Kind: TypeKind.Pointer, IsFunctionPointer: false, Element: { } element })
                type = element;
            if (!type.IsFunctionPointer || handle.IsNil)
                return null;
            try
            {
                var field = cls.Module.Reader.GetFieldDefinition(handle);
                var spelled = field.DecodeSignature(new FunctionPointerSpellingProvider(_e, _c.SigProvider), cls.Context);
                return spelled.FunctionPointer is { } f ? (f.Key, f.Name, !f.Open, f.Binding) : null;
            }
            catch (Exception e) when (!Compilation.IsMustEscape(e))
            {
                return null;
            }
        }

        private ModifierSignature? CustomModifiers(MethodInfo method)
        {
            if (_modifierSignatures.TryGetValue(method, out var cached))
                return cached;
            try
            {
                var md = method.Module.Reader.GetMethodDefinition(method.Handle);
                cached = new ModifierSignature(md.DecodeSignature(
                    new ModifierSignatureProvider(_c.SigProvider), method.Context));
            }
            catch (Exception e) when (!Compilation.IsMustEscape(e))
            {
                cached = null;
            }
            _modifierSignatures[method] = cached;
            return cached;
        }

        private void NoteModifierTypes(ModifiedType type)
        {
            foreach (var modifier in type.Required)
                if (modifier.Class is { } cls) _c.NoteReferencedType(cls);
            foreach (var modifier in type.Optional)
                if (modifier.Class is { } cls) _c.NoteReferencedType(cls);
        }

        private (string Expr, int Count) RenderCustomModifiers(TypeDesc[] modifiers)
        {
            if (modifiers.Length == 0)
                return ("nullptr", 0);
            var rows = new string[modifiers.Length];
            for (int i = 0; i < rows.Length; i++)
                rows[i] = _e.MemberTypeInfoExpr(modifiers[i], _emittedEnums);
            return (TypeArgumentVector(string.Join(", ", rows)), rows.Length);
        }

        internal TypeMetadataEmitter(CppEmitter e, CppOutput o)
        {
            _e = e;
            _c = e._c;
            _o = o;
            _sb = o.Data;
        }

        // Whether a row can be entered late-bound, as no compiled call site names it:
        // reflection invokes or binds rows, and a hot-update patch imports them.
        private bool RowsEnteredLateBound
            => _c.ReflectionInvokeUsed || _c.NeedsReflectionDelegateBind || _e._hotUpdateBase;

        /// <summary>Append an open generic DEFINITION's kind flags — the bits behind
        /// <c>Type.IsValueType</c>/<c>IsInterface</c>/<c>IsAbstract</c>/<c>IsClass</c>/
        /// <c>IsSealed</c> — to <paramref name="flagBits"/>. A <c>gendef_</c> handle is what
        /// a bare <c>typeof(Def&lt;&gt;)</c> reflects over, so both emit routes (natural off
        /// a closed instantiation, synthetic for a def with no close) must carry them or the
        /// definition reads as a non-sealed reference class. Interface ⟹ abstract, mirroring
        /// the closed-type flag rule.</summary>
        private static void AddGenericDefKindFlags(List<string> flagBits, bool isValueType, bool isInterface, bool isAbstract, bool isSealed, bool isDelegate)
        {
            if (isValueType)
                flagBits.Add("DN2CPP_TF_VALUETYPE");
            if (isInterface)
            {
                flagBits.Add("DN2CPP_TF_INTERFACE");
                flagBits.Add("DN2CPP_TF_ABSTRACT");
            }
            else if (isAbstract)
                flagBits.Add("DN2CPP_TF_ABSTRACT");
            if (isSealed)
                flagBits.Add("DN2CPP_TF_SEALED");
            if (isDelegate)
                flagBits.Add("DN2CPP_TF_DELEGATE");
        }

        /// <summary>Whether <paramref name="defName"/> is one of the five generic collection
        /// interfaces an SZArray implements over its element. The bit rides the DEFINITION
        /// handle (one stamp covers every close), and <c>dn2cpp_isinst</c> reads it off a
        /// closed target's genericDef to answer <c>(array) is IList&lt;T&gt;</c> — with
        /// array-element covariance on the type argument — independent of the lazy dispatch
        /// map. Asked at BOTH gendef mint sites, because a definition first named by a
        /// relation-only array row mints its handle through the minimal-type-info route,
        /// where a missing bit is a type test that silently answers false.</summary>
        private static bool IsArrayGenericItfDef(string defName) =>
            defName is "System.Collections.Generic.IEnumerable`1"
                or "System.Collections.Generic.ICollection`1"
                or "System.Collections.Generic.IList`1"
                or "System.Collections.Generic.IReadOnlyList`1"
                or "System.Collections.Generic.IReadOnlyCollection`1";

        /// <summary>The SUBSTITUTION-INVARIANT relations a <c>gendef_</c> shell carries: its
        /// nearest ancestor naming no type parameter as the base pointer, and one
        /// nullptr-slot row per such interface in the definition's transitive closure — the
        /// same relation-only shape <see cref="RenderItfTables"/> gives an interface or an
        /// abstract class.
        ///
        /// <para>Substitution cannot touch a type that names no type parameter, so this set —
        /// non-generic ancestry plus closed fixed-argument entries like <c>B&lt;int&gt;</c> —
        /// is the same for the definition and for every close of it. That identity is the
        /// whole invariant: it makes <c>typeof(IMarker).IsAssignableFrom(typeof(Box&lt;&gt;))</c>
        /// answer True with nothing minted HERE — the reads this side makes are lookup-only,
        /// and a closed entry a typeof-only program never instantiates was minted upstream by
        /// the wiring pass — while every relation spelled in the definition's own parameters
        /// stays False by construction, because no row here can name one.</para>
        ///
        /// <para>Rows are filtered by the definedness test rather than force-emitted, exactly
        /// as RenderItfTables filters its own, so the answer degrades to a SUBSET of .NET's
        /// the same monotone way. A definition no close exists of has its ancestry minted and
        /// forced into the emit set upstream (Compilation.WireTypeofOpenGenericDefAncestry).
        /// The base of a value type or interface stays null, matching the closed-type
        /// rule.</para>
        /// </summary>
        private (string Base, string Itfs, int ItfCount) GenericDefRelations(
            string defSym, ClassInfo? nonGenericBase, List<ClassInfo> itfs, bool isValueType, bool isInterface)
        {
            string baseExpr =
                isValueType || isInterface ? "nullptr"
                : nonGenericBase is { } b && _e.TypeInfoSymbolDefined(b.CppTypeInfoName)
                    ? _e.TypeInfoRef(b, "generic-definition nearest invariant base")
                    : "&dn2cpp_object_type";
            var rows = itfs
                .Where(i => _e.RelationRowDefined(i))
                .Select(i => $"{{ {_e.TypeInfoRef(i, "generic-definition relation row")}, nullptr }}")
                .ToList();
            if (rows.Count == 0)
                return (baseExpr, "nullptr", 0);
            // File-local, defined immediately before the handle whose initializer is its
            // only reader; the gendef sections all render into the primary metadata TU.
            string tab = "gendefitfs_" + defSym;
            _sb.AppendLine($"static const Dn2CppInterfaceEntry {tab}[] = {{ {string.Join(", ", rows)} }};");
            return (baseExpr, tab, rows.Count);
        }

        /// <summary>Record that this emission defines the type-info symbol
        /// <paramref name="sym"/> — the defined half of
        /// <see cref="CppEmitter.AssertNamedTypeInfosDefined"/>. Filtered to <c>ti_</c>
        /// symbols (a runtime-raised exception's <c>tibind_</c> or a shared handle is not a
        /// per-type <c>ti_</c> a body ever names), so the set holds exactly what the named
        /// half can hold.</summary>
        private void NoteDefinedTypeInfo(string sym)
        {
            if (sym.StartsWith("ti_", System.StringComparison.Ordinal))
                _e._definedTypeInfoSyms.Add(sym);
        }

        /// <summary>The intrinsic-modeled classes / interfaces a lowering named by their own
        /// <c>&amp;ti_&lt;T&gt;</c> handle — the generic Class arm of
        /// <see cref="MethodCompiler.TypeInfoExprOf"/>, reached by a
        /// <c>typeof</c>/<c>isinst</c>/<c>castclass</c>/<c>box</c>/array-covariance site — that
        /// no other pass emits. A non-intrinsic referenced reference type is pulled into the
        /// emit set opaquely by <see cref="CppEmitter.Emit"/>; an intrinsic one is skipped
        /// there, so its <c>ti_</c> would be undeclared at link time. The predicate is
        /// decode-free (names, flags and <see cref="ClassInfo.CppTypeInfoName"/> only) and
        /// selects exactly: a type whose body-path type-info expression IS its own <c>ti_</c>
        /// (not a shared runtime handle like Decimal/Task/an exception) yet which nothing
        /// emits. Enums are excluded (emitted by their own loop).</summary>
        private List<ClassInfo> ReferencedIntrinsicTypeInfos()
        {
            var topo = _e.TopoOrder().ToHashSet();
            bool Eligible(ClassInfo c) => !c.IsEnum
                && !_e._emit.Contains(c)
                && !topo.Contains(c)
                // A runtime-OWNED type's ti_ is the runtime's own definition —
                // minting a minimal one here would be the second identity this
                // table exists to prevent. Not subsumed by the TypeInfoExprOf
                // comparison below, which holds for every class since the Class
                // arm answers "&" + CppTypeInfoName.
                && !CoreIntrinsics.RuntimeOwnsTypeInfo(c)
                && MethodCompiler.TypeInfoExprOf(TypeDesc.MakeClass(c)) == "&" + c.CppTypeInfoName;
            var result = _c.ReferencedTypes.Where(Eligible).ToHashSet();
            var pending = result.ToList();
            for (int i = 0; i < pending.Count; i++)
            {
                foreach (var arg in pending[i].Context.TypeArgs)
                {
                    if (arg is { Kind: TypeKind.Class, Class: { } nested }
                        && Eligible(nested) && result.Add(nested))
                        pending.Add(nested);
                }
            }
            return result.OrderBy(c => c.CppName, System.StringComparer.Ordinal).ToList();
        }

        /// <summary>One type the reflection tables will name by its type-info: a member's
        /// type, a closed generic argument, or an array member's element. An enum is noted
        /// referenced so its own <c>ti_</c> is emitted by the referenced-enum loops below:
        /// <see cref="CppEmitter.FieldTypeInfoExpr"/> degrades an enum without one to
        /// System.Object, and an enum only a reflected row names has no token site to have
        /// noted it. An SZArray records its element (<see cref="Compilation.NoteArrayElementType"/>
        /// — the same route every <c>newarr</c>/<c>typeof(T[])</c> site takes) so the precise
        /// <c>ti_arr_&lt;elem&gt;</c> handle the SZArray arm names is forward-declared and
        /// emitted. Element kinds mirror the <c>ldtoken typeof(T[])</c> arm, a cross-assembly
        /// name is promoted as FieldTypeInfoExpr promotes it. Additional definitions
        /// record concrete intrinsic signature identities for the minimal type-info pass.</summary>
        private void NoteReflectedType(TypeDesc t, bool definitionSignature = false)
        {
            // A by-ref or pointer member names its element (CppEmitter.ReflectionPass).
            if (t is { Kind: TypeKind.ByRef or TypeKind.Pointer, Element: { } element })
            {
                NoteReflectedType(element, definitionSignature);
                return;
            }
            if (t.Kind == TypeKind.External && _c.ResolveExternalClass(t) is { } xc)
                t = TypeDesc.MakeClass(xc);
            if (definitionSignature && t is { Kind: TypeKind.Class, Class.IntrinsicCppName: not null }
                && !Compilation.ContainsGenericVar(t) && !Compilation.ContainsCanonPlaceholder(t))
            {
                // Added definitions need intrinsic signature identities, not managed layouts.
                _c.NoteTypeIdentityClosure(t, keepSeed: false);
                return;
            }
            if (t is { Kind: TypeKind.Class, Class: { IsEnum: true } en })
            {
                if (!Compilation.ContainsCanonPlaceholder(t))
                    _c.NoteReferencedType(en);
                return;
            }
            if (t is not { Kind: TypeKind.SZArray, Element: { Kind: TypeKind.Primitive or TypeKind.Class or TypeKind.External or TypeKind.SZArray or TypeKind.MDArray } el })
                return;
            _c.NoteArrayElementType(el);
            NoteReflectedType(el, definitionSignature);
        }

        /// <summary>Pre-notes every type the reflection tables name by its type-info
        /// (<see cref="NoteReflectedType"/>) — field types (<see cref="RenderFieldTable"/>),
        /// method/ctor return, parameter and generic-argument types
        /// (<see cref="BuildMemberTable"/> via <see cref="RenderMemberTables"/>), property
        /// types (<see cref="BuildPropTable"/>) and the closed generic-argument vectors
        /// (<see cref="RenderTypeInfo"/>) — by mirroring those emitters' class/member
        /// filters, so it reads exactly the member types they read (the trim rule is applied
        /// before any signature is touched, like BuildMemberTable's row loop). MUST run
        /// before the forward-declaration loops in <see cref="Emit"/>: the ti_arr_/enum-ti_
        /// externs — and with them the declared-handle record
        /// <see cref="CppEmitter.ArrayTypeInfoDeclared"/> answers from — are taken there, and
        /// a type noted later can no longer be named by a member row. Each mirrored emitter
        /// names this method at its guard.</summary>
        private void NoteReflectedMemberTypes()
        {
            foreach (var cls in _e.TopoOrder())
            {
                if (cls.IsEnum)
                    continue;
                // RenderFieldTable's guard.
                if (cls.Fields.Count > 0 && !_e.IsCanonicalWorld(cls) && _c.KeepsReflectionMetadata(cls))
                    foreach (var f in cls.Fields)
                        NoteReflectedType(f.Type);
                // RenderTypeInfo's generic-argument vector gate (its _genericDefSyms
                // lookup succeeds exactly when GenericDefInfo is non-null).
                if (!_e.IsCanonicalWorld(cls) && !_e.SkipsCanonicalMetadata(cls)
                    && _e.GenericDefInfo(cls) is not null)
                    foreach (var a in cls.Context.TypeArgs)
                        NoteReflectedType(a);
                // RenderMemberTables' guard + BuildMemberTable's row trim.
                if (_e.IsOpaque(cls) || _e.IsCanonicalWorld(cls))
                    continue;
                bool keepRefl = _c.KeepsReflectionMetadata(cls);
                var propertyAccessors = PropertyAccessorHandles(cls);
                var accessorRows = new HashSet<MethodDefinitionHandle>();
                foreach (var m in _e.ReflectionMethods(cls))
                {
                    bool methodRow = m.Name != ".ctor" && m.Name != ".cctor"
                        && !(_c.SharedGenericsEnabled
                            && m.Context.MethodArgs.Any(Compilation.ContainsCanonPlaceholder));
                    if (methodRow)
                        accessorRows.Add(m.Handle);
                    if (!_e.KeepsReflectionMethodRow(cls, m, propertyAccessors))
                        continue;
                    bool definitionOnly = m.Signature.GenericParameterCount > 0 && m.Context.MethodArgs.Length == 0;
                    var decodedSignature = HasSignatureShape(m.Signature.ReturnType) || m.Signature.ParameterTypes.Any(HasSignatureShape)
                        ? _e.ReflectionSignature(m) : null;
                    void NoteSignatureType(TypeDesc type, BindingSignature? binding)
                    {
                        if (definitionOnly && (Compilation.ContainsGenericVar(type) || Compilation.ContainsCanonPlaceholder(type)))
                            return;
                        bool addedDefinition = definitionOnly && _e._metadataOnlyDefinitions.Contains(m);
                        NoteReflectedType(type, addedDefinition);
                        if (HasSignatureShape(type) && binding is not null)
                            _e.NoteReflectionSignatureLeaves(binding, leaf => NoteReflectedType(leaf, addedDefinition));
                    }
                    NoteSignatureType(m.Signature.ReturnType, decodedSignature?.ReturnType.Binding);
                    for (int i = 0; i < m.Signature.ParameterTypes.Length; i++)
                        NoteSignatureType(m.Signature.ParameterTypes[i], decodedSignature?.ParameterTypes[i].Binding);
                    foreach (var ga in m.Context.MethodArgs)
                        NoteReflectedType(ga);
                    if (CustomModifiers(m) is { } modifiers)
                    {
                        NoteModifierTypes(modifiers.ReturnType);
                        foreach (var p in modifiers.ParameterTypes)
                            NoteModifierTypes(p);
                    }
                }
                // BuildPropTable's rows: a property renders (with its decoded type) when
                // either accessor is in the type's method list; accessor descriptions
                // survive trimming, and the property type is noted by the same condition.
                if (!keepRefl)
                    continue;
                try
                {
                    var reader = cls.Module.Reader;
                    var td = reader.GetTypeDefinition(cls.Handle);
                    foreach (var ph in td.GetProperties())
                    {
                        var acc = reader.GetPropertyDefinition(ph).GetAccessors();
                        if ((acc.Getter.IsNil || !accessorRows.Contains(acc.Getter))
                            && (acc.Setter.IsNil || !accessorRows.Contains(acc.Setter)))
                            continue;
                        NoteReflectedType(
                            reader.GetPropertyDefinition(ph).DecodeSignature(_c.SigProvider, cls.Context).ReturnType);
                    }
                }
                catch (Exception e) when (!Compilation.IsMustEscape(e))
                { /* no decodable property metadata — BuildPropTable yields no rows either */ }
            }
            // EmitReferencedIntrinsicTypeInfos' generic-argument vectors: those rows are
            // outside the emit set, so the walk above never reaches them.
            foreach (var cls in _e._referencedIntrinsicTis)
                if (_e.GenericDefInfo(cls) is not null)
                    foreach (var a in cls.Context.TypeArgs)
                        NoteReflectedType(a);
        }

        /// <summary>The pooled <c>genargpool_</c> symbol for <paramref name="cls"/>'s closed
        /// type arguments. Byte-identical vectors are interned across the whole emission
        /// (first definition wins; explicit <c>extern</c> because a namespace-scope const
        /// array defaults to internal linkage) — every runtime consumer reads the elements,
        /// never the array's address.</summary>
        private string GenericArgVector(ClassInfo cls)
        {
            string gaInit = string.Join(", ", cls.Context.TypeArgs.Select(a => _e.FieldTypeInfoExpr(a, _emittedEnums)));
            return TypeArgumentVector(gaInit);
        }

        private string TypeArgumentVector(string gaInit)
        {
            if (_genargPools.TryGetValue(gaInit, out var pooled))
                return pooled;
            string arr = $"genargpool_{_genargPoolSeq++}";
            _sb.AppendLine($"extern const Dn2CppTypeInfo* const {arr}[] = {{ {gaInit} }};");
            _o.Header.AppendLine($"extern const Dn2CppTypeInfo* const {arr}[];");
            _genargPools[gaInit] = arr;
            return arr;
        }

        /// <summary>The minimal type-infos of the classes <see cref="ReferencedIntrinsicTypeInfos"/>
        /// forward-declared: name, base chain (a reference type chains to System.Object; a
        /// value type or interface roots at null), runtime-struct size where one exists, and
        /// the flag bits <c>dn2cpp_isinst</c>/
        /// <c>dn2cpp_type_is_*</c> read. Relation-only interface rows may be present; calls
        /// are intrinsic-dispatched without vtables or member/interface dispatch tables.
        /// No ty_ companion, so typeof interns lazily. An
        /// intrinsic value that can be boxed wires the object virtuals its runtime payload
        /// can answer without a managed body.
        ///
        /// A CLOSED intrinsic generic also carries genericDef + genericArgs, minting the
        /// family's open-definition handle when no emitted instantiation did: without them
        /// Type.Name/ToString/FullName have nothing to normalize or compose from. Hence the
        /// position — after the gendef loops and after <c>_emittedEnums</c>.</summary>
        private void EmitReferencedIntrinsicTypeInfos()
        {
            foreach (var c in _e._referencedIntrinsicTis)
            {
                if (_e.GenericDefInfo(c) is not { } gi || _e._genericDefSyms.ContainsKey(gi.DefName))
                    continue;
                string defBase = CppNaming.GenericDefinitionStem(gi.DefName);
                string defSym = "gendef_" + defBase;
                _e._genericDefSyms[gi.DefName] = defSym;
                var defFlagBits = new List<string> { "DN2CPP_TF_GENERICDEF" };
                AddGenericDefKindFlags(defFlagBits, c.IsValueType, c.IsInterface, c.IsAbstract, c.IsSealed, c.IsDelegate);
                if (IsArrayGenericItfDef(gi.DefName))
                    defFlagBits.Add("DN2CPP_TF_ARRAY_GEN_ITF");
                string defFlags = "(" + string.Join(" | ", defFlagBits) + ")";
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo {defSym};");
                _sb.AppendLine($"extern const Dn2CppType ty_{defSym};");
                var defAnc = _c.OpenGenericDefAncestry(c.Module, c.Handle);
                var (dfBase, dfItfs, dfItfCount) =
                    GenericDefRelations(defBase, defAnc.Base, defAnc.Interfaces, c.IsValueType, c.IsInterface);
                _e.EmitTypeInfo(_sb, defSym, new TypeMetadata {
                    Native = _c.UsesNativeReflectionMetadata(gi.DefName),
                    Name = gi.DefName, Base = dfBase, Interfaces = dfItfs, InterfaceCount = dfItfCount,
                    Flags = defFlags, GenericDef = "&" + defSym, TypeObject = "&ty_" + defSym,
                    GenericParamNames = Compilation.GenericParamNames(c.Module, c.Handle),
                    AssemblyName = string.IsNullOrEmpty(c.Module.AssemblyName) ? null : c.Module.AssemblyName,
                });
                _sb.AppendLine($"const Dn2CppType ty_{defSym} = {{ {{ &dn2cpp_type_type }}, &{defSym} }};");
            }
            foreach (var c in _e._referencedIntrinsicTis)
            {
                bool isTask = _c.GenericDefFullName(c) == "System.Threading.Tasks.Task";
                string interfaces = "nullptr";
                int interfaceCount = 0;
                if (isTask)
                {
                    // Intrinsic shapes omit managed ancestry; the definition's invariant
                    // metadata supplies Task's inherited relation rows without decoding its fields.
                    var ancestry = _c.OpenGenericDefAncestry(c.Module, c.Handle);
                    (_, interfaces, interfaceCount) = GenericDefRelations(c.CppName,
                        ancestry.Base, ancestry.Interfaces, c.IsValueType, c.IsInterface);
                }
                string baseExpr = c.IsValueType || c.IsInterface ? "nullptr"
                    : isTask
                        ? "&dn2cpp_task_type"
                        : "&dn2cpp_object_type";
                string size = c.IntrinsicCppName == "Dn2CppTask*"
                    ? "(int32_t)sizeof(Dn2CppTask)"
                    : c.IntrinsicCppName == "Dn2CppTaskCompletionSource*"
                        ? "(int32_t)sizeof(Dn2CppTaskCompletionSource)"
                        : c.IsValueType && c.IntrinsicCppName is not null
                            ? $"(int32_t)sizeof({CppTypes.Of(TypeDesc.MakeClass(c))})"
                            : "0";
                var flagBits = new List<string>();
                if (c.IsValueType)
                    flagBits.Add("DN2CPP_TF_VALUETYPE");
                if (c.IsInterface)
                {
                    flagBits.Add("DN2CPP_TF_INTERFACE");
                    flagBits.Add("DN2CPP_TF_ABSTRACT");
                }
                else if (c.IsAbstract)
                    flagBits.Add("DN2CPP_TF_ABSTRACT");
                if (c.IsSealed)
                    flagBits.Add("DN2CPP_TF_SEALED");
                string flags = flagBits.Count == 0 ? "0" : string.Join(" | ", flagBits);
                string toString = _c.GenericDefFullName(c) == "System.Threading.Tasks.ValueTask"
                    && c.Context.TypeArgs.Length == 1
                        ? "&dn2cpp_valuetask_box_tostring"
                    : c.IntrinsicCppName is "Dn2CppVector64" or "Dn2CppVector128"
                        or "Dn2CppVector256" or "Dn2CppVector512" or "Dn2CppVectorT"
                        && c.Context.TypeArgs.Length == 1
                        ? $"&dn2cpp_vec_box_tostring<{CppTypes.StorageOf(c.Context.TypeArgs[0])}, "
                            + $"{MethodCompiler.VecWidthBytes(c.IntrinsicCppName)}, "
                            + $"{(c.IntrinsicCppName == "Dn2CppVectorT" ? 1 : 0)}>"
                        : "nullptr";
                string getHash = "nullptr";
                string equals = "nullptr";
                if (MethodCompiler.IsCancellationTokenValue(TypeDesc.MakeClass(c)))
                {
                    string ghThunk = $"ghthunk_{c.CppName}";
                    _sb.AppendLine($"static int32_t {ghThunk}(Dn2CppObject* o) {{ return (int32_t)(uintptr_t)(((Dn2CppCancelToken*)(o + 1))->source); }}");
                    getHash = $"&{ghThunk}";
                    string eqThunk = $"eqthunk_{c.CppName}";
                    _sb.AppendLine($"static int32_t {eqThunk}(Dn2CppObject* a, Dn2CppObject* b) {{ return (b && b->type == a->type && ((Dn2CppCancelToken*)(a + 1))->source == ((Dn2CppCancelToken*)(b + 1))->source) ? 1 : 0; }}");
                    equals = $"&{eqThunk}";
                }
                string genericDef = "nullptr", genericArgs = "nullptr";
                int genericCount = 0;
                if (_e.GenericDefInfo(c) is { } gi && _e._genericDefSyms.TryGetValue(gi.DefName, out var defSym))
                {
                    genericDef = "&" + defSym;
                    genericArgs = GenericArgVector(c);
                    genericCount = c.Context.TypeArgs.Length;
                }
                string displayName = c.IntrinsicCppName == "Dn2CppTaskAwaiter"
                    && c.Context.TypeArgs.Length > 0 && IsNestedType(c)
                        ? _e.ClosedNestedTypeDisplay(c)
                        : Compilation.ReflectionTypeName(c);
                _e.EmitTypeInfo(_sb, c.CppTypeInfoName, new TypeMetadata {
                    Native = _c.UsesNativeReflectionMetadata(c),
                    Name = displayName, Base = baseExpr, InstanceSize = size,
                    Interfaces = interfaces, InterfaceCount = interfaceCount,
                    ToStringFn = toString, HashFn = getHash, EqualsFn = equals, Flags = flags,
                    GenericDef = genericDef, GenericArgs = genericArgs, GenericArgCount = genericCount,
                });
            }
        }

        internal void Emit()
        {
            _c.FreezeReflectionMetadataSelection();
            // Seed the note pass with identities already named by bodies; reflected
            // intrinsic types and modifiers can add identities before final declarations.
            _e._referencedIntrinsicTis = ReferencedIntrinsicTypeInfos();

            // Before anything is forward-declared: note every array and enum the
            // reflection tables below will type a member or a generic argument with, so its
            // precise ti_arr_ or enum ti_ is declared and emitted like a body-noted one.
            NoteReflectedMemberTypes();

            // The note pass also discovers custom-modifier types. Unlike member
            // parameter types, those are erased by the main TypeDesc signature and
            // can first become referenced here; refresh the minimal type-info set
            // before its forward declarations are emitted.
            _e._referencedIntrinsicTis = ReferencedIntrinsicTypeInfos();

            // The noted-array set is final HERE, so this is where the relation-only generic
            // interface rows are planted: an element the pass above just discovered is
            // invisible to every fixpoint round, and its array would report a short
            // interface list. The minted interfaces are named by nothing else, so they join
            // the minimal-type-info set — re-deriving keeps that list's CppName order
            // rather than appending to it.
            if (_c.PlantUnmappedArrayGenericItfRows().Count > 0)
                _e._referencedIntrinsicTis = ReferencedIntrinsicTypeInfos();

            // Open definitions share one emitted handle per CLR name. Collect all
            // physical owners before selecting its format, including later chunks.
            foreach (var cls in _e.TopoOrder().Where(c => !c.IsEnum && !_e.IsCanonicalWorld(c))
                .Concat(_e._referencedIntrinsicTis))
                if (_e.GenericDefInfo(cls) is { } definition)
                    _c.NoteEmittedReflectionDefinition(definition.DefName, cls.Module);
            _c.PrepareReflectionDefinitionFormats();

            // Type-info handles for classes, referenced enums, and per-element arrays are named
            // by method bodies in any TU (typeof / isinst / cast / typed array creation), so
            // each is forward-declared with external linkage in the header. The definitions
            // land in per-class metadata blocks that overflow into metadata-only chunk TUs
            // (see the class-major loop below); all the file-local tables/thunks a class's
            // type-info references are co-located inside its block.
            _o.Header.AppendLine("// ---- type metadata forward declarations ----");
            // Each ti_ has a ty_ companion: the interned System.Type object baked into
            // the type-info's typeObject member, making typeof/GetType a lock-free load.
            // Iterate TopoOrder(), not EmittedClasses: a tree-shaken abstract base (no
            // reachable body, so absent from the emit set) is still pulled into the
            // topo order as an emitted subclass's base, and its ti_/ty_ definition is
            // emitted by the definition loop below — so it needs a matching forward
            // declaration here, or a field table naming &ti_base fails to compile.
            foreach (var cls in _e.TopoOrder().Where(c => !c.IsEnum && !_e.SkipsCanonicalMetadata(c)
                                                          && !CoreIntrinsics.RuntimeOwnsTypeInfo(c)))
            {
                // CppTypeInfoDefName, not CppTypeInfoName: a runtime-raised exception type's
                // handle is declared by dn2cpp.h (and is mutable — the startup bind writes it),
                // so re-declaring it here as `extern const` would conflict. Its emitted
                // metadata carries the tibind_ name instead. A runtime-OWNED type is skipped
                // outright: the runtime defines both its handle and its Type object, and a
                // second definition here would be a second identity for one CLR type.
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo {cls.CppTypeInfoDefName};");
                _o.Header.AppendLine($"extern const Dn2CppType ty_{cls.CppName};");
                NoteDefinedTypeInfo(cls.CppTypeInfoDefName);
            }
            foreach (var en in _c.ReferencedTypes.Where(c => c.IsEnum).OrderBy(c => c.CppName, System.StringComparer.Ordinal))
            {
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo {en.CppTypeInfoName};");
                _o.Header.AppendLine($"extern const Dn2CppType ty_{en.CppName};");
                NoteDefinedTypeInfo(en.CppTypeInfoName);
            }
            foreach (var kv in _c.ArrayElementTypes.OrderBy(kv => kv.Key, System.StringComparer.Ordinal))
            {
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo ti_arr_{kv.Key};");
                _o.Header.AppendLine($"extern const Dn2CppType ty_arr_{kv.Key};");
                NoteDefinedTypeInfo("ti_arr_" + kv.Key);
            }
            // Static MD-array handles: named by constructed type identities, nested array
            // elementType rows and the registry; defined by EmitArrayTypeInfos.
            foreach (var kv in _c.MdArrayTypes.OrderBy(kv => kv.Key, System.StringComparer.Ordinal))
            {
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo ti_md_{kv.Key};");
                _o.Header.AppendLine($"extern const Dn2CppType ty_md_{kv.Key};");
                NoteDefinedTypeInfo("ti_md_" + kv.Key);
            }
            // NoteDefinedTypeInfo above IS the declared-handle snapshot the two array-naming
            // mouths key on (CppEmitter.ArrayTypeInfoDeclared): an element noted after this
            // point (an attribute decode inside the class-major loop) still gets its ti_arr_
            // definition from EmitArrayTypeInfos, but no header extern — so member types must
            // never name it, and it is correctly absent from the recorded set.
            // Referenced intrinsic type-infos: a `typeof`/`isinst`/`castclass`/`box`/array-
            // covariance lowering names an intrinsic-modeled type's own `&ti_<T>` (StringBuilder,
            // CultureInfo, the SIMD vectors, IFormatProvider, …), but the opaque referenced-type
            // pass in CppEmitter.Emit skips intrinsic classes — so nothing else would emit that
            // symbol and the C++ link would fail on an undeclared identifier. (Forward-declared
            // here; defined by EmitReferencedIntrinsicTypeInfos, once the gendef handles exist.)
            foreach (var c in _e._referencedIntrinsicTis)
            {
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo {c.CppTypeInfoName};");
                NoteDefinedTypeInfo(c.CppTypeInfoName);
            }
            _o.Header.AppendLine();
            // The declaration block is complete: nothing below adds a ti_ definition that a
            // reference could be checked against, so from here on CppEmitter.TypeInfoRef can
            // answer definedness — and every emitter-side &ti_ mouth from here to the end of
            // emission goes through it.
            _e._typeInfoSymsFinal = true;

            _sb.AppendLine("// ---- type metadata ----");

            // Per-enum type-info: an enum referenced as a type token (isinst / box / cast)
            // gets a distinct identity so `o is SomeEnum` discriminates one enum from
            // another and from int. Modeled as int32 with the shared System.Enum base,
            // which the runtime keys on to format/hash the payload as its underlying value.
            // Emitted from ReferencedTypes (enums carry no layout, so they are not in the
            // emit/struct set), ordered for stable output. The `tostring` slot is wired to
            // a per-enum name-formatting function so a boxed enum's Object.ToString yields
            // the member name, not the number — its body references string literals and so
            // is emitted after the literal table; only the declaration goes here.
            foreach (var en in _c.ReferencedTypes.Where(c => c.IsEnum).OrderBy(c => c.CppName, System.StringComparer.Ordinal))
            {
                string toStr = "nullptr";
                _e._metadataNativeRows = _c.UsesNativeReflectionMetadata(en);
                if (MethodCompiler.EnumToStringFn(en, $"enumtostr_{en.CppName}", _e._literals) is { } body)
                {
                    _sb.AppendLine($"static Dn2CppString* enumtostr_{en.CppName}(Dn2CppObject*);");
                    _e._enumToStringFns.Append(body);
                    toStr = $"&enumtostr_{en.CppName}";
                }
                // Enum runtime metadata: the underlying primitive's handle + the
                // (name, value) member table pre-sorted by unsigned underlying magnitude
                // (matching .NET's Enum.GetNames/GetValues order). Backs the non-generic
                // Enum.GetNames/GetName/IsDefined/Parse(Type, …) + GetEnumUnderlyingType.
                string underlying = MethodCompiler.TypeInfoExprOf(TypeDesc.MakePrimitive(en.EnumUnderlying)) ?? "&dn2cpp_int32_type";
                var ordered = SortedEnumMembers(en);
                string membersExpr = "nullptr";
                if (ordered.Count > 0)
                {
                    string tab = $"enummembers_{en.CppName}";
                    // Member names are C# identifiers (ASCII letters/digits/underscore), so
                    // they inline safely as C string literals — no escaping/literal pool.
                    // Values render in hex, as the field-row site below does: a decimal
                    // long.MinValue has no C++ literal form (`-9223372036854775808LL` is a
                    // negation of an unsigned literal, which C++11-narrows and fails to compile).
                    var rows = ordered.Select(m => new MetadataRow(new[] { MetadataValue.Text(m.name), MetadataValue.Signed(m.value) })).ToList();
                    _e.EmitMetadataTable(_sb, "Dn2CppEnumMember", tab, rows);
                    membersExpr = tab;
                }
                // The reflection field surface (value__ + one literal row per
                // member), backing Type.GetField(s) like any class's fldtab_.
                var (fldExpr, fldCount) = RenderEnumFieldTable(en);
                // Trailing 0-fills for methods/ctors/props/customAttrs/generic, then
                // the enum metadata (enumUnderlying, enumMembers, enumMemberCount).
                string enFlags = "(DN2CPP_TF_ENUM | DN2CPP_TF_VALUETYPE | DN2CPP_TF_SEALED"
                    + (IsNestedType(en) ? " | DN2CPP_TF_NESTED" : "")
                    + (EnumHasFlagsAttribute(en) ? " | DN2CPP_TF_FLAGS" : "")
                    + (CarriesObjectMemberRows(en) ? " | DN2CPP_TF_OBJECT_MEMBER_ROWS" : "") + ")";
                var (enIlAttrs, enToken) = TypeIlMeta(en);
                // instanceSize is the enum's MODEL width — int32, or int64 for a long/ulong
                // underlying — not its CLR underlying width: every reader of this field
                // sizes a BOX from it, and a box carries the model payload. The underlying
                // width is enumUnderlying's answer (dn2cpp_layout_size backs
                // Unsafe.SizeOf<E> from there).
                string enSize = MethodCompiler.IsWideEnum(en) ? "(int32_t)sizeof(int64_t)" : "(int32_t)sizeof(int32_t)";
                _e.EmitTypeInfo(_sb, en.CppTypeInfoName, new TypeMetadata {
                    Native = _e._metadataNativeRows,
                    Name = Compilation.ReflectionTypeName(en), Base = "&dn2cpp_enum_type",
                    InstanceSize = enSize, ToStringFn = toStr, Flags = enFlags, Fields = fldExpr, FieldCount = fldCount,
                    EnumUnderlying = underlying, EnumMembers = membersExpr, EnumMemberCount = ordered.Count,
                    AssemblyName = string.IsNullOrEmpty(en.Module.AssemblyName) ? null : en.Module.AssemblyName, TypeObject = "&ty_" + en.CppName, IlAttrs = unchecked((uint)enIlAttrs), MetadataToken = enToken,
                });
                _sb.AppendLine($"const Dn2CppType ty_{en.CppName} = {{ {{ &dn2cpp_type_type }}, {_e.TypeInfoRef(en, "enum ty_ companion")} }};");
            }
            _e._metadataNativeRows = false;

            // Generic open-definition type-infos: one synthetic Dn2CppTypeInfo per
            // distinct closed-generic definition, so GetGenericTypeDefinition() returns a
            // stable shared handle (two List<T> closes' definitions compare equal) and
            // MakeGenericType has something to key the closed instantiations on. Self-
            // referencing genericDef (a const's own address is known in its initializer);
            // carries DN2CPP_TF_GENERICDEF. Emitted before the closed type-infos that name it.
            foreach (var cls in _e.TopoOrder().Where(c => !c.IsEnum && !_e.IsCanonicalWorld(c)))
            {
                if (_e.GenericDefInfo(cls) is not { } gi || _e._genericDefSyms.ContainsKey(gi.DefName))
                    continue;
                string symBase = CppNaming.GenericDefinitionStem(gi.DefName);
                string sym = "gendef_" + symBase;
                _e._genericDefSyms[gi.DefName] = sym;
                // Generic variance rides the definition handle, which is the only place it can:
                // it is a property of the type PARAMETERS, so every closed instantiation reads
                // it from here. `varianceMask` carries the general answer (per parameter, either
                // direction, any arity — IComparer<in T>, IGrouping<out K, out E>); the older
                // DN2CPP_TF_COVARIANT bit still marks the single-parameter covariant defs it
                // always marked, because a base image emitted before the mask existed keeps
                // matching through it.
                int varMask = _c.GenericVarianceMask(cls);
                var flagBits = new List<string> { "DN2CPP_TF_GENERICDEF" };
                // The definition's KIND bits — invariant across every close, so read off
                // this closed instantiation. A bare typeof(Def<>) reflects over the gendef
                // handle, so IsInterface/IsAbstract/IsClass/IsSealed/IsValueType must answer
                // as real .NET does. An interface is also abstract, mirroring the
                // closed-type flag rule above.
                AddGenericDefKindFlags(flagBits, cls.IsValueType, cls.IsInterface, cls.IsAbstract, cls.IsSealed, cls.IsDelegate);
                if (varMask != 0)
                    flagBits.Add("DN2CPP_TF_VARIANT");
                if (_e.IsCovariantGenericDef(cls))
                    flagBits.Add("DN2CPP_TF_COVARIANT");
                if (IsArrayGenericItfDef(gi.DefName))
                    flagBits.Add("DN2CPP_TF_ARRAY_GEN_ITF");
                string defFlags = flagBits.Count == 1 ? flagBits[0] : "(" + string.Join(" | ", flagBits) + ")";
                // name, base, instanceSize, vtable, interfaces, interfaceCount, tostring,
                // gethashcode, equals, flags, fields, fieldCount, methods, methodCount,
                // ctors, ctorCount, props, propCount, customAttrs, customAttrCount,
                // genericDef(self), genericArgs, genericArgCount.
                // External linkage always: a closed instantiation's type-info names the
                // definition handle from its metadata chunk TU (and with shared generics
                // on, a shared body's rgctx walk anchors on it from any body TU).
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo {sym};");
                // The ty_ companion keeps its extern declaration next to the definition
                // (only the def's own initializer needs the companion's address).
                _sb.AppendLine($"extern const Dn2CppType ty_{sym};");
                // The definition's TypeDef is this closed instantiation's own handle, so the
                // argument-free ancestry reads off the same metadata a synthetic def's does.
                var anc = _c.OpenGenericDefAncestry(cls.Module, cls.Handle);
                var (relBase, relItfs, relItfCount) =
                    GenericDefRelations(symBase, anc.Base, anc.Interfaces, cls.IsValueType, cls.IsInterface);
                // A variant def spells the members between typeObject and varianceMask that the
                // trailing 0-fill convention would otherwise leave implicit (formatspec, ilAttrs,
                // metadataToken, defaultMemberName); an invariant one stops at typeObject exactly
                // as before, so its text is unchanged.
                _e.EmitTypeInfo(_sb, sym, new TypeMetadata {
                    Native = _c.UsesNativeReflectionMetadata(gi.DefName),
                    Name = gi.DefName, Base = relBase, Interfaces = relItfs, InterfaceCount = relItfCount,
                    Flags = defFlags, GenericDef = "&" + sym, TypeObject = "&ty_" + sym, VarianceMask = varMask,
                    GenericParamNames = Compilation.GenericParamNames(cls.Module, cls.Handle),
                    AssemblyName = string.IsNullOrEmpty(cls.Module.AssemblyName) ? null : cls.Module.AssemblyName,
                });
                _sb.AppendLine($"const Dn2CppType ty_{sym} = {{ {{ &dn2cpp_type_type }}, &{sym} }};");
            }

            // Synthetic open-definition handles for a bare typeof(Def<>) no emitted closed
            // instantiation minted a gendef for above (that loop keys on TopoOrder's emitted
            // classes; an open definition names no ClassInfo). A body that lowered such a
            // typeof named &gendef_<sym> (recorded in TypeofOpenGenericDefs), so the symbol
            // MUST be defined or the C++ link fails on it (cut ⟹ route). Name +
            // self-referencing genericDef + the KIND bits resolved at the token site + the
            // argument-free relations (GenericDefRelations, whose ancestry this lane forced
            // into the emit set): with no closed instantiation there is nothing to
            // covariance-match or array-interface-test against (both bits are only ever read
            // off a CLOSED target's genericDef), and MakeGenericType finding no registry
            // candidate throws, matching IL2CPP. GenericDefKind.Unknown — a definition no
            // loaded module declares — keeps the minimal flags, a best-effort degrade.
            foreach (var (defName, def) in _c.TypeofOpenGenericDefs.OrderBy(kv => kv.Key, System.StringComparer.Ordinal))
            {
                if (_e._genericDefSyms.ContainsKey(defName))
                    continue;
                string symBase = CppNaming.GenericDefinitionStem(defName);
                string sym = "gendef_" + symBase;
                _e._genericDefSyms[defName] = sym;
                var flagBits = new List<string> { "DN2CPP_TF_GENERICDEF" };
                bool synthValueType = (def.Kind & GenericDefKind.ValueType) != 0;
                bool synthInterface = (def.Kind & GenericDefKind.Interface) != 0;
                AddGenericDefKindFlags(flagBits,
                    synthValueType,
                    synthInterface,
                    (def.Kind & GenericDefKind.Abstract) != 0,
                    (def.Kind & GenericDefKind.Sealed) != 0,
                    (def.Kind & GenericDefKind.Delegate) != 0);
                uint synthAttrs = 0;
                int synthToken = 0;
                string? synthAssembly = null;
                if ((def.Kind & GenericDefKind.Nested) != 0)
                {
                    flagBits.Add("DN2CPP_TF_NESTED");
                    if (_c.OpenGenericDefHandleByName(defName) is { } definition)
                    {
                        synthAttrs = (uint)definition.Module.Reader.GetTypeDefinition(definition.Handle).Attributes;
                        synthToken = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(definition.Handle);
                        synthAssembly = definition.Module.AssemblyName;
                    }
                }
                if ((def.Kind & GenericDefKind.ByRefLike) != 0)
                    flagBits.Add("DN2CPP_TF_BYREFLIKE");
                if ((def.Kind & GenericDefKind.HiddenEnclosing) != 0)
                    flagBits.Add("DN2CPP_TF_HIDDEN_ENCLOSING");
                string defFlags = flagBits.Count == 1 ? flagBits[0] : "(" + string.Join(" | ", flagBits) + ")";
                _o.Header.AppendLine($"extern const Dn2CppTypeInfo {sym};");
                _sb.AppendLine($"extern const Dn2CppType ty_{sym};");
                var synthAnc = _c.OpenGenericDefAncestryByName(defName);
                var (synthBase, synthItfs, synthItfCount) = GenericDefRelations(
                    symBase, synthAnc.Base, synthAnc.Interfaces, synthValueType, synthInterface);
                _e.EmitTypeInfo(_sb, sym, new TypeMetadata {
                    Native = _c.UsesNativeReflectionMetadata(defName),
                    Name = defName, Base = synthBase, Interfaces = synthItfs, InterfaceCount = synthItfCount,
                    Flags = defFlags, GenericDef = "&" + sym, TypeObject = "&ty_" + sym, GenericParamNames = def.ParamNames,
                    IlAttrs = synthAttrs, MetadataToken = synthToken, AssemblyName = synthAssembly,
                });
                _sb.AppendLine($"const Dn2CppType ty_{sym} = {{ {{ &dn2cpp_type_type }}, &{sym} }};");
            }

            // Rgctx tables (shared generics): one per grouped real instantiation
            // whose canonical owner registered slots — the per-instantiation values
            // the shared bodies read (wired into the type-info's trailing rgctx
            // member below, and passed directly by the hidden-parameter forwarders).
            if (_c.SharedGenericsEnabled)
            {
                foreach (var (user, entries) in _c.RgctxTables())
                {
                    foreach (var e in entries)
                        _e.RequireRenderedTypeInfoDefined(e, "rgctx type slot", user);
                    _o.Header.AppendLine($"extern const void* const rgctx_{user.CppName}[];");
                    _sb.AppendLine($"const void* const rgctx_{user.CppName}[] = {{ "
                        + string.Join(", ", entries.Select(e => $"(const void*)({e})")) + " };");
                    _rgctxTableSyms.Add(user);
                }
                // Method-dimension tables: one per real generic-method
                // instantiation bound to a context-using shared body. Never wired
                // into a type-info (method context is not receiver-derivable) —
                // only the instantiation's forwarder and forwarding slot entries
                // name them.
                int methodTables = 0;
                foreach (var (user, entries) in _c.RgctxMethodTables())
                {
                    foreach (var e in entries)
                        _e.RequireRenderedTypeInfoDefined(e, "rgctx method-dimension type slot",
                            user.DeclaringClass);
                    _o.Header.AppendLine($"extern const void* const rgctx_{user.CppName}[];");
                    _sb.AppendLine($"const void* const rgctx_{user.CppName}[] = {{ "
                        + string.Join(", ", entries.Select(e => $"(const void*)({e})")) + " };");
                    methodTables++;
                }
                _c.RgctxTableCount = _rgctxTableSyms.Count + methodTables;
                _c.RgctxSlotCount = _c.RgctxSlotTotal();
            }

            // The enums whose ti_ is actually emitted (from ReferencedTypes) — a field of
            // any other (un-emitted, opaque) class/enum type must not name a dangling ti_.
            _emittedEnums = _c.ReferencedTypes.Where(c => c.IsEnum).ToHashSet();

            EmitReferencedIntrinsicTypeInfos();

            // Class-major metadata emission: each class's complete metadata — vtable,
            // interface slot tables + unboxing thunks, field table + accessor thunks,
            // method/ctor/param/property tables + attribute factories + invoker thunks, and
            // finally its ti_/ty_ definitions — renders into one scratch block appended
            // whole, so it overflows into metadata-only chunk TUs and is never split across
            // a chunk boundary. Every file-local symbol a block references is defined inside
            // that block; cross-class references resolve through the header externs. Two
            // dedup scopes:
            //
            // - PER-CHUNK: the invoker-thunk set and the slotmiss_/invmiss_ stub maps. They
            //   name functions, whose duplicate `static` definitions across TUs are legal
            //   and deterministic, so each chunk simply (re)defines what it references.
            //   Reset BEFORE rendering whenever the upcoming append opens a fresh chunk —
            //   sound because MetadataWillRoll ignores the incoming block's size, so the
            //   verdict cannot change between this check and the append.
            // - GLOBAL (whole emission): the intern pools (parmpool_/itfpool_/itfspool_/
            //   genargpool_), which deliberately survive the chunk roll below — duplicate
            //   DATA would be duplicate bytes in the binary. Soundness argument at the
            //   pool-map declarations above.
            //
            // Rendering order (TopoOrder) and the roll rule are deterministic, so chunk
            // boundaries — and therefore the whole output — reproduce byte-identically (the
            // self-host fixpoint relies on it). Rendering also interns string literals and
            // attr-factory sequence numbers in class-major order; those indices are
            // emission-order-significant but carry no cross-section invariant.
            var block = new StringBuilder();
            foreach (var cls in _e.TopoOrder())
            {
                if (cls.IsEnum)
                    continue;
                // A runtime-OWNED type contributes no metadata block at all: the runtime
                // defines its type-info, its Type object and its tables, and every reference
                // goes there. Skipping the WHOLE block rather than just the type-info is
                // what keeps the vtable/field/member tables from being emitted as statics
                // nothing names — for the primitives, which are not opaque, those are real
                // tables.
                if (CoreIntrinsics.RuntimeOwnsTypeInfo(cls))
                    continue;
                block.Clear();
                if (_o.MetadataWillRoll)
                {
                    _invokerThunks.Clear();
                    _slotStubs.Clear();
                    _invokerMissStubs.Clear();
                }
                _sb = block;
                _e.BeginMetadataBlock(_c.UsesNativeReflectionMetadata(cls));
                RenderVtable(cls);
                RenderItfTables(cls);
                RenderFieldTable(cls);
                RenderMemberTables(cls);
                RenderTypeInfo(cls);
                _e.EndMetadataBlock(block);
                _sb = _o.Data;
                if (block.Length > 0)
                    _o.AppendMetadata(block.ToString());
            }

            _e.EmitArrayTypeInfos(_sb, _emittedEnums);
            _e.EmitMdArrayItfMap(_sb);
            _e.EmitArrayNongenericItfRows(_sb);
            _e.EmitStringInterfaceMap(_sb);
            _e.EmitEnumInterfaceMap(_sb);
            _e.EmitIntrinsicInterfaceMaps(_sb);
            _e.EmitRelationRows(_sb);
            _e.NoteRuntimeHandleBases();
            _sb.AppendLine();
            EmitDelegateIdentities();
            EmitGvmRowDispatch();
            EmitAmbiguousBindings();

            // Last statement of the emission, and it has to be: this object is unreferenced
            // the moment it returns (EmitTypeInfos keeps no field), so a census anywhere
            // later would report the pools as free rather than as big.
            Census();
        }

        /// <summary>The delegate method identities the shipped bodies named, spelled as the
        /// method rows spell their declaring type and generic arguments so the runtime can
        /// match a row by pointer identity. An owner with no defined type-info has no rows
        /// either, and answers through a null declaring type.</summary>
        private void EmitDelegateIdentities()
        {
            var identities = _c.FreezeDelegateIdentities();
            if (identities.Count == 0)
                return;
            var gvms = _c.UsedGvms.ToDictionary(g => g.Gvm.CppName, System.StringComparer.Ordinal);
            var enumClass = _c.EnumInterfaces?.EnumClass;
            if (enumClass is null)
                foreach (var (_, method, _) in identities)
                    if (method.DeclaringClass.FullName == "System.Enum")
                    {
                        enumClass = method.DeclaringClass;
                        break;
                    }
            _sb.AppendLine("// ---- delegate method identities ----");
            foreach (var (sym, m, isVirtual) in identities)
            {
                var owner = m.DeclaringClass;
                string ownerExpr = _e.TypeInfoSymbolDefined(owner.CppTypeInfoName)
                    ? _e.TypeInfoRef(owner, "delegate method identity's declaring type")
                    : "nullptr";
                var margs = m.Context.MethodArgs;
                string argsExpr = "nullptr";
                if (margs.Length > 0)
                {
                    var gargs = new List<string>(margs.Length);
                    foreach (var ga in margs)
                        gargs.Add(_e.MemberTypeInfoExpr(ga, _emittedEnums));
                    argsExpr = TypeArgumentVector(string.Join(", ", gargs));
                }
                int token = DelegateIdentityToken(m);
                string targetsExpr = "nullptr";
                int targetCount = 0;
                if (isVirtual && gvms.TryGetValue(m.CppName, out var disp))
                {
                    targetCount = EmitGvmTargets(disp, sym + "_gvm_targets");
                    if (targetCount > 0)
                        targetsExpr = sym + "_gvm_targets";
                }
                else if (isVirtual && owner.IsInterface)
                {
                    // A declaration's own default body is recorded too, so the
                    // runtime can tell an emitted receiver from one it must
                    // resolve without this table. A clone resolves without it:
                    // its interface slots hold the bodies its own rows name.
                    var targets = new List<(ClassInfo Receiver, MethodInfo Target)>();
                    foreach (var receiver in _c.AllocatedRefTypes.ToList())
                    {
                        if (receiver.IsInterface || !_c.ImplementsInterface(receiver, owner)
                            || _e.SkipsCanonicalMetadata(receiver)
                            || _e.IsRuntimeTemplateLevel(receiver)
                            || !_e.TypeInfoSymbolDefined(receiver.CppTypeInfoName))
                            continue;
                        var target = _c.ResolveItfImplOrNull(receiver, m);
                        if (target is not null
                            && _c.Reachable.Contains(target)
                            && _e.TypeInfoSymbolDefined(target.DeclaringClass.CppTypeInfoName))
                            targets.Add((receiver, target));
                    }
                    if (targets.Count > 0)
                    {
                        targetsExpr = sym + "_interface_targets";
                        targetCount = targets.Count;
                        _sb.AppendLine($"static const Dn2CppDelegateMethodTarget {targetsExpr}[] = {{");
                        foreach (var (receiver, target) in targets.OrderBy(t => t.Receiver.CppName,
                            System.StringComparer.Ordinal))
                        {
                            string receiverExpr = _e.TypeInfoRef(receiver, "delegate interface receiver");
                            string targetExpr = _e.TypeInfoRef(target.DeclaringClass, "delegate interface target");
                            int targetToken = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(target.Handle);
                            _sb.AppendLine($"    {{ {receiverExpr}, {targetExpr}, {targetToken} }},");
                        }
                        _sb.AppendLine("};");
                    }
                }
                string selectedTargetsExpr = "nullptr";
                int selectedTargetCount = 0;
                if (isVirtual)
                {
                    // Dispatch identity survives reflection trimming and code folding.
                    // Record the selected declaration, including a default body and a
                    // runtime template receiver, independently of its member rows.
                    var targets = new List<(ClassInfo? Receiver, MethodInfo Target, bool Family)>();
                    var receivers = _c.AllocatedRefTypes.ToList();
                    if (_c.StringInterfaces is { } stringInterfaces)
                        receivers.Add(stringInterfaces.StringClass);
                    foreach (var receiver in receivers)
                    {
                        if (receiver.IsInterface || _e.SkipsCanonicalMetadata(receiver)
                            || !_e.TypeInfoSymbolDefined(receiver.CppTypeInfoName))
                            continue;
                        MethodInfo? target = null;
                        if (gvms.TryGetValue(m.CppName, out var selectedDisp)
                            && selectedDisp.Cases.TryGetValue(receiver, out var selectedGvm))
                            target = selectedGvm;
                        else if (Compilation.IsGvmCall(m)
                            && (owner.IsInterface ? _c.ImplementsInterface(receiver, owner)
                                : Compilation.DerivesFromOrIs(receiver, owner)))
                            target = m;
                        else if (owner.IsInterface && _itfTables.TryGetValue(receiver, out var interfaces))
                            target = _c.DelegateInterfaceTarget(receiver, m, interfaces);
                        else if (owner.IsInterface && _c.StringInterfaces is { } si
                            && receiver == si.StringClass)
                            target = _c.DelegateInterfaceTarget(receiver, m,
                                si.Dispatches.Where(d => _e._emit.Contains(d.Itf)).Select(d => d.Itf).ToList());
                        else if (owner.FullName == "System.Object")
                            target = m.Name switch
                            {
                                "ToString" => Compilation.EffectiveToString(receiver) ?? m,
                                "Equals" => Compilation.EffectiveEquals(receiver) ?? m,
                                "GetHashCode" => Compilation.EffectiveGetHashCode(receiver) ?? m,
                                _ => null,
                            };
                        else if (Compilation.DerivesFromOrIs(receiver, owner)
                            && m.VtableSlot >= 0 && m.VtableSlot < receiver.Vtable.Count)
                            target = receiver.Vtable[m.VtableSlot];
                        if (target is not null
                            && _e.TypeInfoSymbolDefined(target.DeclaringClass.CppTypeInfoName))
                            targets.Add((receiver, target, false));
                    }
                    if (enumClass is not null)
                    {
                        MethodInfo? enumTarget = owner.IsInterface && _c.EnumInterfaces is { } ei
                            ? _c.DelegateInterfaceTarget(enumClass, m,
                                ei.Dispatches.Where(d => _e._emit.Contains(d.Itf)).Select(d => d.Itf).ToList())
                            : owner.FullName == "System.Object" ? m.Name switch
                            {
                                "ToString" => Compilation.EffectiveToString(enumClass),
                                "Equals" => Compilation.EffectiveEquals(enumClass),
                                "GetHashCode" => Compilation.EffectiveGetHashCode(enumClass),
                                _ => null,
                            } : null;
                        if (enumTarget is not null
                            && _e.TypeInfoSymbolDefined(enumTarget.DeclaringClass.CppTypeInfoName))
                            targets.Add((enumClass, enumTarget, true));
                    }
                    foreach (var info in _c.IntrinsicInterfaces)
                        if (info.Itf == owner && info.SlotDecl == m
                            && info.Receiver is { } intrinsicReceiver && info.Target is { } intrinsicTarget
                            && _e.TypeInfoSymbolDefined(intrinsicTarget.DeclaringClass.CppTypeInfoName))
                            targets.Add((intrinsicReceiver, intrinsicTarget, true));
                    if (owner.IsInterface && _c.ArrayDispatchClass is { } arrayClass)
                    {
                        MethodInfo? arrayTarget = null;
                        if (owner.Context.TypeArgs.Length == 0)
                            arrayTarget = _c.ResolveItfImplOrNull(arrayClass, m);
                        else if (owner.Context.TypeArgs is [{ } element]
                            && _c.ArrayEnumerableElementTypes.TryGetValue(Compilation.ArrayElemMangle(element), out var arrayMap)
                            && arrayMap.Dispatches.Any(d => d.Itf == owner))
                        {
                            // SZArrayHelper selects the requested T, even when the native
                            // variant table uses the receiver element's shared body. The
                            // existing closed wrapper is its private identity namespace.
                            arrayTarget = _c.ResolveItfImplOrNull(arrayMap.Szae, m);
                        }
                        if (arrayTarget is not null
                            && _e.TypeInfoSymbolDefined(arrayTarget.DeclaringClass.CppTypeInfoName))
                            targets.Add((null, arrayTarget, true));
                    }
                    selectedTargetCount = EmitDelegateTargets(targets, sym + "_selected_targets");
                    if (selectedTargetCount > 0)
                        selectedTargetsExpr = sym + "_selected_targets";
                }
                _o.Header.AppendLine($"extern const Dn2CppDelegateMethodIdentity {sym};");
                _sb.AppendLine($"extern const Dn2CppDelegateMethodIdentity {sym} = "
                    + $"{{ {ownerExpr}, {token}, {margs.Length}, {argsExpr}, {(isVirtual ? "true" : "false")}, "
                    + $"{targetCount}, {targetsExpr}, {selectedTargetCount}, {selectedTargetsExpr} }};");
            }
            _sb.AppendLine();
        }

        /// <summary>Emits a dispatcher's branches as the delegate method targets
        /// <paramref name="symbol"/> names, template levels included: the runtime looks a
        /// clone up by its template level. Returns how many; none emits no array.</summary>
        private int EmitGvmTargets(Compilation.GvmDispatch disp, string symbol)
        {
            var targets = disp.Cases
                .Where(kv => kv.Value != disp.Gvm && _c.Reachable.Contains(kv.Value)
                    && !_e.SkipsCanonicalMetadata(kv.Key)
                    && _e.TypeInfoSymbolDefined(kv.Value.DeclaringClass.CppTypeInfoName))
                .OrderBy(kv => kv.Key.CppName, System.StringComparer.Ordinal)
                .ToList();
            if (targets.Count == 0)
                return 0;
            _sb.AppendLine($"static const Dn2CppDelegateMethodTarget {symbol}[] = {{");
            foreach (var (receiver, target) in targets)
            {
                string receiverExpr = _e.TypeInfoRef(receiver, "delegate GVM receiver");
                string targetExpr = _e.TypeInfoRef(target.DeclaringClass, "delegate GVM target");
                int targetToken = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(target.Handle);
                _sb.AppendLine($"    {{ {receiverExpr}, {targetExpr}, {targetToken} }},");
            }
            _sb.AppendLine("};");
            return targets.Count;
        }

        private int EmitDelegateTargets(List<(ClassInfo? Receiver, MethodInfo Target, bool Family)> targets, string symbol)
        {
            if (targets.Count == 0)
                return 0;
            _sb.AppendLine($"static const Dn2CppDelegateMethodTarget {symbol}[] = {{");
            foreach (var (receiver, target, family) in targets.OrderBy(t => t.Receiver?.CppName ?? "",
                System.StringComparer.Ordinal))
            {
                string receiverExpr = receiver is not null
                    ? _e.TypeInfoRef(receiver, "delegate dispatch receiver") : "nullptr";
                string targetExpr = _e.TypeInfoRef(target.DeclaringClass, "delegate dispatch target");
                int targetToken = DelegateIdentityToken(target);
                _sb.AppendLine($"    {{ {receiverExpr}, {targetExpr}, {targetToken}, {(family ? "true" : "false")} }},");
            }
            _sb.AppendLine("};");
            return targets.Count;
        }

        private static int DelegateIdentityToken(MethodInfo method) => method.Handle.IsNil
            ? method.Name switch { "ToString" => -1, "Equals" => -2, "GetHashCode" => -3, _ => 0 }
            : System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(method.Handle);

        /// <summary>The dispatcher a generic virtual method row runs through when
        /// reflection or a hot-update import enters it (<c>dn2cpp_gvm_row_dispatch</c>),
        /// keyed by the row's declaring type, token and generic arguments, spelled as the
        /// row spells them, with the targets <c>Delegate.Method</c> reports and whether it
        /// strips. Sorted by token, then owner and method, so the runtime bisects on the
        /// token. A final row or a sealed class's row runs its own body. The symbols always
        /// link; entries emit only when reflection can enter a row or the image is a
        /// <c>--hotupdate-base</c> one.</summary>
        private void EmitGvmRowDispatch()
        {
            var rows = new List<Compilation.GvmDispatch>();
            if (RowsEnteredLateBound)
                foreach (var disp in _c.UsedGvms)
                {
                    var gvm = disp.Gvm;
                    if (_memberAddr.ContainsKey(gvm) && (gvm.Attributes & System.Reflection.MethodAttributes.Final) == 0
                        && !gvm.DeclaringClass.IsSealed && _e.EmitsGvmDispatcher(disp))
                        rows.Add(disp);
                }
            var entries = new List<string>(rows.Count);
            foreach (var disp in rows
                         .OrderBy(d => System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(d.Gvm.Handle))
                         .ThenBy(d => d.Gvm.DeclaringClass.CppName, System.StringComparer.Ordinal)
                         .ThenBy(d => d.Gvm.CppName, System.StringComparer.Ordinal))
            {
                var gvm = disp.Gvm;
                string name = Compilation.GvmDispatchName(gvm);
                string targetsExpr = "gvmrow_targets_" + entries.Count;
                int targetCount = EmitGvmTargets(disp, targetsExpr);
                var gargs = new List<string>(gvm.Context.MethodArgs.Length);
                foreach (var ga in gvm.Context.MethodArgs)
                    gargs.Add(_e.MemberTypeInfoExpr(ga, _emittedEnums));
                int token = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(gvm.Handle);
                entries.Add($"    {{ {{ {_e.TypeInfoRef(gvm.DeclaringClass, "generic virtual row dispatch owner")}, "
                    + $"{token}, {gargs.Count}, {TypeArgumentVector(string.Join(", ", gargs))}, true, "
                    + $"{targetCount}, {(targetCount > 0 ? targetsExpr : "nullptr")} }}, (void*)&{name}, "
                    + $"{(disp.Strips ? "true" : "false")} }},");
                _e._reflectedGvmDispatchers.Add(name);
            }
            if (entries.Count == 0)
                entries.Add("    { { nullptr, 0, 0, nullptr, false, 0, nullptr }, nullptr, false },");
            _sb.AppendLine("// ---- generic virtual row dispatch ----");
            _sb.AppendLine("extern const Dn2CppGvmRowDispatch dn2cpp_gvm_row_dispatch[] = {");
            foreach (var entry in entries)
                _sb.AppendLine(entry);
            _sb.AppendLine("};");
            _sb.AppendLine($"extern const int32_t dn2cpp_gvm_row_dispatch_count = {rows.Count};");
            _sb.AppendLine();
        }

        /// <summary>Hands the emitter's own tables to <see cref="EmitCensus"/>. Gated at
        /// the call site rather than inside it: summing the lengths of ~40,000 pooled
        /// initializer strings is real work, and a diagnostic that costs what it measures
        /// on every transpile is worse than no diagnostic.</summary>
        private void Census()
        {
            if (!EmitCensus.Enabled)
                return;
            long parmChars = 0;
            foreach (var kv in _parmPools)
                parmChars += kv.Key.Length + kv.Value.Length;
            long itfChars = 0;
            foreach (var kv in _itfPools)
                itfChars += kv.Key.Length + kv.Value.Length;
            long genargChars = 0;
            foreach (var kv in _genargPools)
                genargChars += kv.Key.Length + kv.Value.Length;
            long itfsChars = 0;
            foreach (var kv in _itfsPools)
                itfsChars += kv.Key.Length + kv.Value.Length;
            long memberAddrChars = 0;
            foreach (var kv in _memberAddr)
                memberAddrChars += kv.Value.Length;
            long tabChars = 0;
            foreach (var kv in _fieldTabs)
                tabChars += kv.Value.Expr.Length;
            foreach (var kv in _methodTabs)
                tabChars += kv.Value.Expr.Length;
            foreach (var kv in _ctorTabs)
                tabChars += kv.Value.Expr.Length;
            foreach (var kv in _propTabs)
                tabChars += kv.Value.Expr.Length;
            long itfSymChars = 0;
            foreach (var kv in _itfTabSyms)
                itfSymChars += kv.Value.Length;
            long stubChars = 0;
            foreach (var kv in _slotStubs)
                stubChars += kv.Key.Length + kv.Value.Length;
            foreach (var kv in _invokerMissStubs)
                stubChars += kv.Key.Length + kv.Value.Length;
            long thunkChars = 0;
            foreach (var k in _invokerThunks)
                thunkChars += k.Length;
            EmitCensus.ReportMetadata(
                _parmPools.Count, parmChars, _itfPools.Count, itfChars,
                _genargPools.Count, genargChars, _itfsPools.Count, itfsChars,
                _memberAddr.Count, memberAddrChars,
                _fieldTabs.Count + _methodTabs.Count + _ctorTabs.Count + _propTabs.Count, tabChars,
                _itfTabSyms.Count, itfSymChars,
                _slotStubs.Count + _invokerMissStubs.Count, stubChars,
                _invokerThunks.Count, thunkChars);
        }

        // Emit (once per chunk) a stub that aborts with `desc` baked in and return its
        // symbol, for a slot whose receiver the shared traps cannot read. `reporter` is
        // the runtime entry point (dn2cpp_itf_slot_missing_named / _vcall_unimplemented_named).
        // The stub carries the slot's exact C++ signature where it renders
        // (CppEmitter.NamedSlotMissStubDef): a wasm call_indirect checks the callee's
        // type immediate, so the historical void() form dies there as an anonymous
        // signature-mismatch trap before the named abort can run.
        private string SlotMissStub(string reporter, string desc, MethodInfo decl) =>
            PooledSlotStub("slotmiss_", reporter + "\0" + desc, decl,
                name => _e.NamedSlotMissStubDef(name, reporter, desc, decl));

        // A slot whose sibling interface overrides leave no most specific body:
        // the stub throws .NET's AmbiguousImplementationException with its message.
        // A MakeGenericType instantiation copies its runtime template's table, so
        // a template's stub reads the receiver's type name where it has a receiver.
        private string AmbiguousSlotStub(ClassInfo cls, ClassInfo itf, MethodInfo decl)
        {
            string? self = _e.IsRuntimeTemplateLevel(cls) && _e.SlotTrapShape(decl) is not null ? "self" : null;
            string body = AmbiguousImplementationThrow(cls, itf, decl, self);
            return PooledSlotStub("slotambig_", body, decl,
                name =>
                {
                    // The checker lives beside its chunk-local stub. Binding compares
                    // addresses without calling a target with the slot's arbitrary ABI.
                    string check = "bind_" + name;
                    _ambiguousBindings.Add(check);
                    _o.Header.AppendLine($"void {check}(const void* target, void* self);");
                    return _e.SlotStubDef(name, body, decl, self) + "\n"
                        + $"void {check}(const void* target, [[maybe_unused]] void* self) "
                        + $"{{ if (target == (const void*)&{name}) {body} }}";
                });
        }

        private void EmitAmbiguousBindings()
        {
            _o.Header.AppendLine("void* dn2cpp_bind_interface_slot(const void* target, void* self);");
            _sb.AppendLine("void* dn2cpp_bind_interface_slot(const void* target, [[maybe_unused]] void* self)");
            _sb.AppendLine("{");
            foreach (string check in _ambiguousBindings)
                _sb.AppendLine($"    {check}(target, self);");
            _sb.AppendLine("    return const_cast<void*>(target);");
            _sb.AppendLine("}");
        }

        private string PooledSlotStub(string prefix, string text, MethodInfo decl, Func<string, string> define)
        {
            string key = _e.SlotStubKey(text, decl);
            if (!_slotStubs.TryGetValue(key, out var name))
            {
                name = prefix + _slotStubSeq++;
                _sb.AppendLine(define(name));
                _slotStubs[key] = name;
            }
            return name;
        }

        // Emit (once per chunk) the invoker-ABI trap stub standing in for a member-table
        // row's invoker thunk that cannot be materialized (see _invokerMissStubs). Unlike a
        // slotmiss_ stub this one is ENTERED through its real signature — the interpreter
        // and MethodBase.Invoke both call invokers with the shared (fn, self, args, retType)
        // ABI — so it carries that exact C++ signature and raises the CATCHABLE
        // NotSupportedException instead of aborting: the caller is a program that asked to
        // invoke a member, not a corrupted dispatch table, and the diagnosis has to reach
        // it. Deduplicated per chunk on the descriptor text; the sequence counter stays
        // monotonic across the emission.
        private string InvokerMissStub(ClassInfo cls, MethodInfo m, string blockedType)
        {
            // ASCII only: the text is a runtime exception message (terminals, logs,
            // frozen gate transcripts), not a source comment.
            string desc = $"{cls.FullName}.{m.Name}: no reflection invoker in this image "
                + $"(the signature names {blockedType}, whose layout was never emitted)";
            if (!_invokerMissStubs.TryGetValue(desc, out var name))
            {
                name = $"invmiss_{_invokerMissSeq++}";
                string lit = desc.Replace("\\", "\\\\").Replace("\"", "\\\"");
                _sb.AppendLine($"static Dn2CppObject* {name}([[maybe_unused]] void* fn, "
                    + "[[maybe_unused]] Dn2CppObject* self, [[maybe_unused]] Dn2CppObject** args, "
                    + $"[[maybe_unused]] const Dn2CppTypeInfo* retType) {{ dn2cpp_throw_invoker_missing(\"{lit}\", (const void*)&{name}); }}");
                _invokerMissStubs[desc] = name;
            }
            return name;
        }

        // Vtable: the class's virtual dispatch table, file-local — referenced only
        // by the class's own type-info initializer, within the same block.
        private void RenderVtable(ClassInfo cls)
        {
            if (cls.IsValueType || cls.IsEnum || cls.IsInterface || cls.IsDelegate || _e.IsOpaque(cls)
                || _e.SkipsCanonicalMetadata(cls))
                return;
            // A slot with no reached body is a slot nothing dispatches, but it holds a TRAP
            // rather than a null pointer: reaching one means the reachability model was
            // wrong about the slot and has to say which type and method it was wrong about,
            // where a null slot is a call through address 0 naming nothing. A trap must
            // also carry the slot's exact C++ signature or wasm's call_indirect type check
            // kills it anonymously before it can report (SlotTrapThunk). The thunk recovers
            // the name at run time from the receiver's method table, and one inline thunk
            // per ABI shape keeps vtable initializers internable. A struct-returning
            // virtual instead bakes its descriptor into a per-slot stub — exact even on a
            // metadata-stripped image, where the thunk's scan finds no method rows.
            var owners = cls.SlotOwners;
            var entries = cls.Vtable.Select((m, i) =>
            {
                if (m is not null && _c.Reachable.Contains(m))
                    return $"(const void*)&{m.Emittable.CppName}";
                // An abstract slot has a null impl but its OWNER still declares the
                // signature — the trap must carry it either way.
                var decl = m ?? (i < owners.Count ? owners[i] : null);
                if (decl is not null && !ReceiverIsFirstArg(decl.Signature.ReturnType))
                    return $"(const void*)&{SlotMissStub("dn2cpp_vcall_unimplemented_named", $"{cls.FullName}.{decl.Name}", decl)}";
                if (decl is not null && _e.SlotTrapThunk(decl, vcall: true) is { } trap)
                    return $"(const void*)&{trap}";
                return "(const void*)&dn2cpp_vcall_unimplemented";
            }).ToList();
            if (entries.Count == 0)
                entries.Add("(const void*)&dn2cpp_vcall_unimplemented");
            _sb.AppendLine($"[[maybe_unused]] static const void* {cls.CppVtableName}[] = {{ {string.Join(", ", entries)} }};");
        }

        // Interface dispatch tables: one per (concrete class, interface),
        // resolved against the class itself so overrides bind correctly.
        private void RenderItfTables(ClassInfo cls)
        {
            // Reference types always get dispatch tables; a value type only when it is
            // boxed (a never-boxed one is dispatched solely via direct constrained calls).
            // An interface or abstract class gets a RELATION-ONLY table (nullptr slots; see
            // the relationOnly branch below): the `.itf`-only readers
            // (dn2cpp_type_is_assignable_from, dn2cpp_type_get_interfaces, dn2cpp_isinst,
            // dn2cpp_array_elem_assignable) need the rows, or IsAssignableFrom over two
            // interfaces silently answers False. The slot-resolving walkers skip
            // nullptr-slot rows, so a relation row on an abstract base can never shadow a
            // concrete ancestor's real slot table.
            if (cls.IsEnum || _e.SkipsCanonicalMetadata(cls))
                return;
            // An intrinsic-shaped class's members are intrinsic-dispatched, so its own table
            // could carry no slot. One whose emitted ti_ is its only type-info states its CLR
            // relations through the side table instead, where a dispatch they admit but no
            // map serves is catchable (CppEmitter.EmitRelationRows).
            if (_e.IsIntrinsicShaped(cls))
            {
                if (CoreIntrinsics.RuntimeTypeInfoSymbol(cls) is null)
                    _e._intrinsicShellRelations.Add(cls);
                return;
            }
            // Three kinds get a relation-only table rather than none, each otherwise a
            // silent False out of IsAssignableFrom / an empty GetInterfaces():
            //
            // - a NEVER-BOXED value type: no instance to dispatch on, so the slots would
            //   resolve nothing — and the slot half is where the cost lives, since a value
            //   type's rows need one unboxing THUNK per interface method (code, not data).
            // - an OPAQUE shell, reached by a type token alone: nothing allocates it, so
            //   nothing dispatches through it, and its rows stay filtered by the emit set
            //   below so the table drags in nothing. That filter is why the answer can be a
            //   SUBSET of .NET's — an interface no other reachable type mentions has no
            //   type-info to point at — monotone in the right direction (every row present
            //   is a true relation), the same unsoundness the AOT reflection model carries.
            // - an ABSTRACT class or an INTERFACE.
            bool relationOnly = cls.IsInterface || cls.IsAbstract || _e.IsOpaqueShell(cls)
                || (cls.IsValueType && !_c.IsAllocated(cls));
            var transitive = new List<ClassInfo>();
            for (var c = cls; c is not null; c = c.BaseClass)
                foreach (var itf in c.Interfaces)
                    // Skip interfaces inherited from an opaque intrinsic base
                    // (e.g. Exception : ISerializable) — they are not in the emit
                    // set, so their dispatch tables/type-info aren't materialized
                    // and we never dispatch through them.
                    if (_e._emit.Contains(itf) && !transitive.Contains(itf))
                        transitive.Add(itf);
            if (transitive.Count == 0)
                return;
            _itfTables[cls] = transitive;

            // Relation-only table: rows carry the interface type-infos, slots stay nullptr —
            // implementation resolution against a type that can have no instance is
            // meaningless (an interface) or moot (an abstract class, whose every dispatch
            // resolves against the concrete receiver's own table, a superset of this one).
            // Canonical alias rows are deliberately NOT appended: they serve a shared body's
            // dispatch against a canonical handle, and a relation table serves no dispatch —
            // adding them would leak alias type-infos into the reflection-visible interface
            // list of a type nothing instantiates.
            if (relationOnly)
            {
                var relEntries = transitive
                    .Select(itf => $"{{ {_e.TypeInfoRef(itf, "interface relation-only table row")}, nullptr }}")
                    .ToList();
                _itfRowCounts[cls] = relEntries.Count;
                InternItfEntryTable(cls, string.Join(", ", relEntries));
                return;
            }

            // The class's slot-table symbol per interface — a pooled itfpool_
            // array (see the intern below), shared with any earlier table in
            // the emission whose initializer is byte-identical.
            var slotSyms = new Dictionary<ClassInfo, string>();
            // A receiver whose member rows --trim-reflection strips states which class slot
            // implements each slot of a kept interface, which is what reflection-bound
            // delegates over it compare (Dn2CppItfImplSlots); a stripped interface has no
            // rows to bind.
            bool recordImplSlots = _c.NeedsReflectionDelegateBind && !_c.KeepsReflectionMetadata(cls);
            bool recordRenamed = RecordsRenamedSlotBodies;
            foreach (var itf in transitive)
            {
                var slots = new List<string>();
                var ims = itf.Methods.ToList();
                int[]? implSlots = recordImplSlots && _c.KeepsReflectionMetadata(itf)
                    ? Enumerable.Repeat(-1, ims.Count).ToArray()
                    : null;
                for (int i = 0; i < ims.Count; i++)
                {
                    var im = ims[i];
                    // Null when the class has no implementation for the interface method —
                    // e.g. a boxed primitive's corlib ClassInfo gets a full interface table
                    // but IntPtr has no concrete IBinaryInteger.DivRem.
                    var impl = _c.ResolveItfImplOrNull(cls, im, out bool ambiguous);
                    if (recordRenamed && !ambiguous && impl is { IsStatic: false } && !impl.DeclaringClass.IsInterface
                        && im.IsVirtual && im.Signature.GenericParameterCount == 0
                        && RenamesSlot(impl.Name, im.Name) && _c.Reachable.Contains(impl))
                        _e._renamedSlotRows.Add((cls, itf, System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(im.Handle), impl));
                    if (implSlots is not null && !ambiguous && impl is { VtableSlot: >= 0 }
                        && impl.VtableSlot < cls.Vtable.Count && cls.Vtable[impl.VtableSlot] == impl)
                        implSlots[i] = impl.VtableSlot;
                    if (ambiguous)
                    {
                        slots.Add($"(const void*)&{AmbiguousSlotStub(cls, itf, im)}");
                        continue;
                    }
                    // A slot with no resolvable impl, or whose impl is unreachable, is never
                    // dispatched through — degrade it to a TRAP, not a null pointer.
                    // "Never dispatched" is a claim about the reachability closure, and when
                    // that claim is wrong (the runtime resolves a variance-satisfied row the
                    // closure never filled) a null slot is a call through 0x0: a SIGSEGV
                    // naming nothing.
                    //
                    // Which trap depends on the slot's RETURN SHAPE. A slot is entered
                    // through a pointer cast to the dispatch site's signature, whose first
                    // parameter is the receiver, so the shared trap can read the receiver's
                    // type name out of argument 0 — unless the return type is a struct
                    // returned INDIRECTLY, where the ABI spends that register on the hidden
                    // result pointer and argument 0 is a raw buffer. Real slots return such
                    // structs (IEnumerator<KeyValuePair<K,V>>.Current, ValueTuple, Decimal,
                    // Nullable<T>), so anything not void, scalar or pointer takes the
                    // anonymous trap: it loses the name, never the abort. Both traps are ONE
                    // shared symbol, so nothing is emitted per class and the pooled slot
                    // tables intern unchanged.
                    if (impl is null || !_c.Reachable.Contains(impl))
                    {
                        // Receiver-readable slot: a per-signature thunk (SlotTrapThunk)
                        // enters the shared receiver-reading trap through the slot's own
                        // C++ signature — wasm's call_indirect rejects a mismatched one
                        // before it can report. Struct-returning slot: bake the (class,
                        // interface, method) descriptor into a per-slot stub instead —
                        // exact even where the receiver's metadata is stripped.
                        slots.Add(ReceiverIsFirstArg(im.Signature.ReturnType)
                            ? $"(const void*)&{_e.SlotTrapThunk(im, vcall: false) ?? "dn2cpp_itf_slot_missing"}"
                            : $"(const void*)&{SlotMissStub("dn2cpp_itf_slot_missing_named", $"{cls.FullName}::{itf.FullName}.{im.Name}", im)}");
                        continue;
                    }
                    // A reference-type impl receives the object pointer directly.
                    // A value-type body needs the unboxed payload; an interface
                    // default body still receives the box. The slot thunk chooses
                    // the receiver representation for the selected body.
                    if (!cls.IsValueType)
                    {
                        // Usually the implementation symbol goes straight into the
                        // slot; a concrete implementer of an interface whose CANONICAL
                        // form erases a headerless intrinsic parameter needs an
                        // unwrapping thunk instead (NfiErasedSlotThunk explains why the
                        // caller's wrap is right for the shared body and wrong here).
                        slots.Add("(const void*)&"
                            + (_e.NfiErasedSlotThunk(_sb, cls, itf, im, impl, i)
                               ?? impl.Emittable.CppName));
                        continue;
                    }
                    string thunk = $"ubthunk_{cls.CppName}_{itf.CppName}_{i}";
                    EmitUnboxingThunk(_sb, thunk, cls, im, impl);
                    slots.Add($"(const void*)&{thunk}");
                }
                // A zero-length array is a GNU/clang extension MSVC rejects (C2466);
                // this happens for a marker interface with no methods of its own
                // (e.g. IUnsignedNumber<T>, or a user interface like IAnimal). The
                // table is never indexed in that case (there is no method to dispatch
                // through it), so a single dummy nullptr element is a behavior-neutral
                // portable stand-in.
                string slotsInit = slots.Count > 0
                    ? string.Join(", ", slots)
                    : "nullptr /* unused: 0-method interface */";
                // Intern byte-identical slot tables across the whole emission
                // (first definition wins; see the pool-map declarations above).
                // The array's element type is const but the array itself is not,
                // so the plain namespace-scope definition already has external
                // linkage — no `extern` keyword (clang's -Wextern-initializer
                // fires on an extern non-const definition). A value-type table
                // embeds this class's unboxing-thunk symbols, so it is unique by
                // construction and simply claims a pool entry of its own.
                if (_itfPools.TryGetValue(slotsInit, out var pooled))
                {
                    slotSyms[itf] = pooled;
                }
                else
                {
                    string tn = $"itfpool_{_itfPoolSeq++}";
                    _sb.AppendLine($"const void* {tn}[] = {{ {slotsInit} }};");
                    _o.Header.AppendLine($"extern const void* {tn}[];");
                    _itfPools[slotsInit] = tn;
                    slotSyms[itf] = tn;
                }
                // A missing row already reads as no class slot.
                if (implSlots is not null && implSlots.Any(slot => slot >= 0))
                    _e._itfImplSlotRows.Add((cls, itf, implSlots));
            }
            var entries = transitive.Select(itf => $"{{ {_e.TypeInfoRef(itf, "interface-dispatch table row")}, {slotSyms[itf]} }}").ToList();
            // Canonical alias rows: for each implemented closed generic
            // interface whose canonical form differs, add a row carrying the
            // canonical interface's type-info over the SAME slot table, so a
            // shared body's dn2cpp_resolve_interface against the canonical
            // handle (e.g. ti_IEqualityComparer_$CnInt32) dispatches on this
            // receiver — including a user comparer implementing
            // IEqualityComparer<SomeEnum>. First row wins when two real
            // interfaces collapse onto one canonical form (deterministic; such
            // a dispatch is inherently ambiguous and the colliding class is
            // vtable-shape-dropped from sharing anyway).
            if (_c.SharedGenericsEnabled)
            {
                var aliased = new HashSet<ClassInfo>(transitive);
                foreach (var itf in transitive)
                    if (_c.CanonicalInterfaceOf(itf) is { } citf && aliased.Add(citf))
                        entries.Add($"{{ {_e.TypeInfoRef(citf, "interface-dispatch table (canonical alias row)")}, {slotSyms[itf]} }}");
            }
            _itfRowCounts[cls] = entries.Count;
            InternItfEntryTable(cls, string.Join(", ", entries));
        }

        // Intern byte-identical interface-entry tables across the whole
        // emission, same first-definition-wins rule as the slot-table pool
        // above. A const array defaults to internal linkage at namespace
        // scope, so the definition needs the explicit `extern`.
        private void InternItfEntryTable(ClassInfo cls, string itfsInit)
        {
            if (_itfsPools.TryGetValue(itfsInit, out var pooledTab))
            {
                _itfTabSyms[cls] = pooledTab;
            }
            else
            {
                string tabName = $"itfspool_{_itfsPoolSeq++}";
                _sb.AppendLine($"extern const Dn2CppInterfaceEntry {tabName}[] = {{ {itfsInit} }};");
                _o.Header.AppendLine($"extern const Dn2CppInterfaceEntry {tabName}[];");
                _itfsPools[itfsInit] = tabName;
                _itfTabSyms[cls] = tabName;
            }
        }

        // Enum reflection field table, matching real .NET: one public instance `value__`
        // row (typed at the underlying primitive) plus one public static literal row per
        // member (typed at the enum itself), in metadata declaration order. Reuses the
        // fldtab_ machinery — a literal row has no storage to thunk-read, so it carries its
        // constant in the trailing literalValue member and the runtime getter boxes it;
        // value__ gets a real getter thunk boxing the receiver's payload at the underlying
        // type. Rows are read straight off the metadata (no ClassInfo.Fields/FieldInfo.Type
        // decode), so alias members — two names, one value, deduped out of enummembers_ —
        // keep their own rows like real .NET.
        //
        // --trim-reflection keeps these rows for every enum emitted: they are an enum's
        // whole member surface, and an enum first noted by a reflected member row is noted
        // after the keep set is final, where stripping would answer an empty GetFields.
        private (string Expr, int Count) RenderEnumFieldTable(ClassInfo en)
        {
            if (en.Handle.IsNil)
                return ("nullptr", 0);
            var rows = new List<MetadataRow>();
            try
            {
                var reader = en.Module.Reader;
                var td = reader.GetTypeDefinition(en.Handle);
                string underlyingTi = MethodCompiler.TypeInfoExprOf(
                    TypeDesc.MakePrimitive(en.EnumUnderlying)) ?? "&dn2cpp_int32_type";
                bool userFldCls = _c.IsUserModule(en.Module);
                foreach (var fdh in td.GetFields())
                {
                    var fd = reader.GetFieldDefinition(fdh);
                    string fname = reader.GetString(fd.Name);
                    bool literal = (fd.Attributes & System.Reflection.FieldAttributes.Literal) != 0;
                    bool isStatic = (fd.Attributes & System.Reflection.FieldAttributes.Static) != 0;
                    var access = fd.Attributes & System.Reflection.FieldAttributes.FieldAccessMask;
                    int attrs = MetadataFieldAttrs((int)fd.Attributes);
                    // A literal member's constant, at full 64-bit width (rendered in
                    // hex so long.MinValue needs no unary minus). The int32
                    // truncation EnumMembers applies for the model does not apply
                    // here — the runtime boxes at the enum's own model width.
                    long lv = 0;
                    string get = "nullptr";
                    string set = "nullptr";
                    string ftInfo;
                    if (literal)
                    {
                        ftInfo = _e.TypeInfoRef(en, "enum literal field row's field type");
                        var ch = fd.GetDefaultValue();
                        if (!ch.IsNil)
                        {
                            var constant = reader.GetConstant(ch);
                            lv = ReadConstantBits(constant.TypeCode, reader.GetBlobReader(constant.Value));
                        }
                    }
                    else if (!isStatic)
                    {
                        // value__: box the receiver's payload as the underlying
                        // primitive (a boxed Tier answers a boxed Byte, like .NET).
                        // The payload is stored at the enum's MODEL width (int32,
                        // int64 for 64-bit underlyings — CppTypes.Of), read at that
                        // width and narrowed to the underlying type for the box.
                        ftInfo = underlyingTi;
                        string readT = en.EnumUnderlying is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
                            ? "int64_t" : "int32_t";
                        string underT = CppTypes.Of(TypeDesc.MakePrimitive(en.EnumUnderlying));
                        string gname = $"fldget_{en.CppName}_{fname}";
                        _sb.AppendLine($"static Dn2CppObject* {gname}(Dn2CppObject* o) {{ "
                            + $"{underT} v = ({underT})*({readT}*)((Dn2CppObject*)o + 1); "
                            + $"return dn2cpp_box({ftInfo}, &v, sizeof({underT})); }}");
                        get = $"&{gname}";
                        // SetValue stores into the boxed receiver; the dispatcher has
                        // already converted the value to a box of the underlying type.
                        string sname = $"fldset_{en.CppName}_{fname}";
                        _sb.AppendLine($"static void {sname}(Dn2CppObject* o, Dn2CppObject* val) {{ "
                            + $"*({readT}*)((Dn2CppObject*)o + 1) = ({readT})*({underT}*)((char*)val + sizeof(Dn2CppObject)); }}");
                        set = $"&{sname}";
                    }
                    else
                    {
                        // A non-literal static field on an enum type does not occur
                        // in C#; report the row (typed at the underlying) with null
                        // thunks rather than invent storage for it.
                        ftInfo = underlyingTi;
                    }
                    (string Expr, int Count) ca = ("nullptr", 0);
                    if (userFldCls)
                        ca = _e.BuildAttrTable(_sb, $"{en.CppName}_fld{rows.Count}", en.Module,
                            fd.GetCustomAttributes());
                    int fldToken = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(fdh);
                    string fieldDisplayType = literal
                        ? _e.ReflectionSignatureType(TypeDesc.MakeClass(en))
                        : _e.ReflectionSignatureType(TypeDesc.MakePrimitive(en.EnumUnderlying));
                    rows.Add(new MetadataRow(new[] {
                        MetadataValue.Text(fname), MetadataValue.Ref(_e.TypeInfoRef(en, "enum field row's declaring type")), MetadataValue.Ref(ftInfo),
                        MetadataValue.ExplicitSigned(attrs), MetadataValue.Ref(get), MetadataValue.Ref(set),
                        MetadataValue.Ref(ca.Expr), MetadataValue.Signed(ca.Count), MetadataValue.Signed((int)fd.Attributes), MetadataValue.Signed(fldToken),
                        MetadataValue.Signed(lv), MetadataValue.Display(fieldDisplayType + " " + fname),
                    }));
                }
            }
            catch (System.Exception e) when (!Compilation.IsMustEscape(e))
            {
                // An enum without decodable field metadata reports no fields.
                return ("nullptr", 0);
            }
            if (rows.Count == 0)
                return ("nullptr", 0);
            _e.EmitMetadataTable(_sb, "Dn2CppFieldInfo", $"fldtab_{en.CppName}", rows);
            return ($"fldtab_{en.CppName}", rows.Count);
        }

        // A constant's bits: integers sign- or zero-extended, a float's or double's IEEE
        // bits, so NaN payloads and -0 survive.
        private static long ReadConstantBits(ConstantTypeCode code, BlobReader blob) => code switch
        {
            ConstantTypeCode.Boolean => blob.ReadBoolean() ? 1 : 0,
            ConstantTypeCode.Char => blob.ReadChar(),
            ConstantTypeCode.SByte => blob.ReadSByte(),
            ConstantTypeCode.Byte => blob.ReadByte(),
            ConstantTypeCode.Int16 => blob.ReadInt16(),
            ConstantTypeCode.UInt16 => blob.ReadUInt16(),
            ConstantTypeCode.Int32 or ConstantTypeCode.Single => blob.ReadInt32(),
            ConstantTypeCode.UInt32 => blob.ReadUInt32(),
            ConstantTypeCode.Int64 or ConstantTypeCode.Double => blob.ReadInt64(),
            ConstantTypeCode.UInt64 => unchecked((long)blob.ReadUInt64()),
            _ => 0,
        };

        private static PrimitiveTypeCode? ConstantPrimitive(ConstantTypeCode code) => code switch
        {
            ConstantTypeCode.Boolean => PrimitiveTypeCode.Boolean,
            ConstantTypeCode.Char => PrimitiveTypeCode.Char,
            ConstantTypeCode.SByte => PrimitiveTypeCode.SByte,
            ConstantTypeCode.Byte => PrimitiveTypeCode.Byte,
            ConstantTypeCode.Int16 => PrimitiveTypeCode.Int16,
            ConstantTypeCode.UInt16 => PrimitiveTypeCode.UInt16,
            ConstantTypeCode.Int32 => PrimitiveTypeCode.Int32,
            ConstantTypeCode.UInt32 => PrimitiveTypeCode.UInt32,
            ConstantTypeCode.Int64 => PrimitiveTypeCode.Int64,
            ConstantTypeCode.UInt64 => PrimitiveTypeCode.UInt64,
            ConstantTypeCode.Single => PrimitiveTypeCode.Single,
            ConstantTypeCode.Double => PrimitiveTypeCode.Double,
            _ => null,
        };

        // A literal row answers GetValue from its constant. A constant encoded at the
        // field's own primitive or enum type rides in literalValue as bits the runtime
        // boxes at the field type, and a null constant leaves the row empty. A string,
        // or a constant encoded at another type (C# stores an nint constant as an int),
        // boxes in a getter thunk instead.
        private (string Get, long Bits) RenderLiteral(ClassInfo cls, FieldInfo f,
            FieldDefinitionHandle handle, string fieldTypeInfo)
        {
            var reader = cls.Module.Reader;
            var constantHandle = reader.GetFieldDefinition(handle).GetDefaultValue();
            if (constantHandle.IsNil)
                return ("nullptr", 0);
            var constant = reader.GetConstant(constantHandle);
            var blob = reader.GetBlobReader(constant.Value);
            string body;
            if (constant.TypeCode == ConstantTypeCode.String)
            {
                body = $"return (Dn2CppObject*){_e._literals.GetOrAdd(blob.ReadUTF16(blob.Length))};";
            }
            else if (ConstantPrimitive(constant.TypeCode) is { } primitive)
            {
                long bits = ReadConstantBits(constant.TypeCode, blob);
                bool enumField = f.Type is { Kind: TypeKind.Class, Class.IsEnum: true }
                    && fieldTypeInfo != "&dn2cpp_object_type"
                    && primitive is not (PrimitiveTypeCode.Single or PrimitiveTypeCode.Double);
                if (enumField || (f.Type.Kind == TypeKind.Primitive && f.Type.Primitive == primitive))
                    return ("nullptr", bits);
                bool wide = primitive is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
                    or PrimitiveTypeCode.Double;
                string value = wide
                    ? $"int64_t v = (int64_t)0x{unchecked((ulong)bits):x}ULL;"
                    : $"int32_t v = (int32_t)0x{unchecked((uint)bits):x}u;";
                string boxType = MethodCompiler.TypeInfoExprOf(TypeDesc.MakePrimitive(primitive))!;
                body = $"{value} return dn2cpp_box({boxType}, &v, sizeof(v));";
            }
            else
            {
                return ("nullptr", 0);
            }
            string name = $"fldget_{cls.CppName}_{f.CppName}";
            _sb.AppendLine($"static Dn2CppObject* {name}(Dn2CppObject*) {{ {body} }}");
            return ($"&{name}", 0);
        }

        private string RenderParameterDefault(MethodInfo method, ParameterHandle handle,
            TypeDesc type, string name)
        {
            var reader = method.Module.Reader;
            var parameter = reader.GetParameter(handle);
            string? body = null;
            var constantHandle = parameter.GetDefaultValue();
            if (!constantHandle.IsNil)
            {
                var constant = reader.GetConstant(constantHandle);
                var blob = reader.GetBlobReader(constant.Value);
                if (constant.TypeCode == ConstantTypeCode.String)
                    body = $"return (Dn2CppObject*){_e._literals.GetOrAdd(blob.ReadUTF16(blob.Length))};";
                else if (constant.TypeCode == ConstantTypeCode.NullReference)
                    body = "return nullptr;";
                else if (ConstantPrimitive(constant.TypeCode) is { } primitive)
                {
                    long bits = ReadConstantBits(constant.TypeCode, blob);
                    if (type.Kind == TypeKind.ByRef)
                        type = type.Element!;
                    type = _c.NullableUnderlying(type) ?? type;
                    bool isEnum = type is { Kind: TypeKind.Class, Class.IsEnum: true };
                    string boxType = isEnum ? _e.MemberTypeInfoExpr(type, _emittedEnums)
                        : MethodCompiler.TypeInfoExprOf(TypeDesc.MakePrimitive(primitive))!;
                    bool wide = primitive is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.Double;
                    string value = wide ? $"int64_t v = (int64_t)0x{unchecked((ulong)bits):x}ULL;"
                        : $"int32_t v = (int32_t)0x{unchecked((uint)bits):x}u;";
                    body = $"{value} return dn2cpp_box({boxType}, &v, sizeof(v));";
                }
            }
            else
            {
                foreach (var cah in parameter.GetCustomAttributes())
                {
                    var attribute = reader.GetCustomAttribute(cah);
                    string? attributeName = Compilation.AttributeTypeName(reader, attribute);
                    if (attributeName is not ("System.Runtime.CompilerServices.DecimalConstantAttribute"
                        or "System.Runtime.CompilerServices.DateTimeConstantAttribute"))
                        continue;
                    var blob = reader.GetBlobReader(attribute.Value);
                    if (blob.ReadUInt16() != 1)
                        throw new NotSupportedException("An optional parameter constant has an invalid attribute prolog.");
                    if (attributeName == "System.Runtime.CompilerServices.DateTimeConstantAttribute")
                    {
                        long ticks = blob.ReadInt64();
                        body = $"Dn2CppDateTime v = dn2cpp_datetime_from_ticks((int64_t)0x{unchecked((ulong)ticks):x}ULL, 0); "
                            + "return dn2cpp_box(&dn2cpp_datetime_type, &v, sizeof(v));";
                    }
                    else
                    {
                        int scale = blob.ReadByte(), sign = blob.ReadByte();
                        uint hi = blob.ReadUInt32(), mid = blob.ReadUInt32(), lo = blob.ReadUInt32();
                        body = $"Dn2CppDecimal v = dn2cpp_decimal_from_parts((int32_t)0x{lo:x}u, (int32_t)0x{mid:x}u, "
                            + $"(int32_t)0x{hi:x}u, {(sign != 0 ? 1 : 0)}, {scale}); return dn2cpp_box(&dn2cpp_decimal_type, &v, sizeof(v));";
                    }
                    break;
                }
            }
            if (body is null)
                return "nullptr";
            _sb.AppendLine($"static Dn2CppObject* {name}() {{ {body} }}");
            return "&" + name;
        }

        // Per-type reflection field tables: one Dn2CppFieldInfo[] per type that
        // declares fields, referenced by the type-info's fields/fieldCount below. Emitted
        // before the type-info definition so the initializer can name it. Each entry
        // carries the field's name, declaring/field type-info, and accessibility bits.
        private void RenderFieldTable(ClassInfo cls)
        {
            // Guard mirrored by NoteReflectedMemberTypes (which pre-notes the
            // array and enum field types this table names) — keep the two in step.
            if (cls.IsEnum || cls.Fields.Count == 0 || _e.IsCanonicalWorld(cls)
                || !_c.KeepsReflectionMetadata(cls))
                return;
            // Field custom-attribute + token lookup: map field name ->
            // FieldDefinitionHandle. Collected for every module (the metadata token is
            // emitted on BCL rows too); the attribute factories below stay gated to
            // user-module (app + referenced library) non-opaque classes.
            bool userFldCls = _c.IsUserModule(cls.Module) && !_e.IsOpaque(cls);
            var fieldHandles = new Dictionary<string, FieldDefinitionHandle>();
            try
            {
                var reader = cls.Module.Reader;
                var td = reader.GetTypeDefinition(cls.Handle);
                foreach (var fh in td.GetFields())
                    fieldHandles[reader.GetString(reader.GetFieldDefinition(fh).Name)] = fh;
            }
            catch (Exception e) when (!Compilation.IsMustEscape(e))
            { /* no decodable field metadata */ }
            var rows = new List<MetadataRow>();
            foreach (var f in cls.Fields)
            {
                int attrs = MetadataFieldAttrs((int)f.Attributes);
                string ftInfo = _e.FieldTypeInfoExpr(f.Type, _emittedEnums);
                // GetValue/SetValue accessor thunks, emitted only when the
                // field is a real member of the emitted layout: a non-literal field of
                // a normally-emitted struct. Delegates (synthetic f_target/f_method
                // layout) and intrinsic-/opaque-layout types don't carry their metadata
                // fields, so their thunks would not compile — leave those null (reflected
                // GetValue/SetValue then throws). A value-type field is boxed/unboxed via
                // its field type-info; a reference field passes the object pointer
                // through; a value-type receiver's payload is at o+1.
                // An [InlineArray] struct lays its single field out as a C array
                // (f_name[N]), which is neither copyable nor assignable — skip its thunks.
                (string get, string set) = ("nullptr", "nullptr");
                string valueCheck = "nullptr";
                long literalBits = 0;
                if (f.IsLiteral)
                {
                    if (fieldHandles.TryGetValue(f.Name, out var literalHandle))
                        (get, literalBits) = RenderLiteral(cls, f, literalHandle, ftInfo);
                }
                else if (!_e.IsOpaque(cls) && !cls.IsDelegate
                    && cls.IntrinsicCppName is null && cls.InlineArrayLength <= 0)
                {
                    string cppT = CppTypes.Of(f.Type);
                    bool isRef = cppT.EndsWith("*");
                    // The spelling the member is actually declared with: a
                    // shared struct layout carries the canonical owner's erased
                    // field types, so the setter must cast to that spelling
                    // (statics stay per-real and keep the real spelling).
                    string memberT = cppT;
                    if (!f.IsStatic && _c.SharedGenericsEnabled
                        && cls.SharedOwner is { } layoutOwner
                        && layoutOwner.Fields.FirstOrDefault(x => x.Name == f.Name) is { } ownerFld)
                        memberT = CppTypes.Of(ownerFld.Type);
                    string access = f.IsStatic
                        ? f.CppStaticAccess
                        : (cls.IsValueType
                            ? $"(({cls.CppStructName}*)((Dn2CppObject*)o + 1))->{f.CppName}"
                            : $"(({cls.CppStructName}*)o)->{f.CppName}");
                    string getName = $"fldget_{cls.CppName}_{f.CppName}";
                    string setName = $"fldset_{cls.CppName}_{f.CppName}";
                    var pointerSpelling = f.Type.Kind == TypeKind.Pointer
                        ? FunctionPointerSpelling(cls, fieldHandles.GetValueOrDefault(f.Name), f.Type) : null;
                    var pointerPass = f.Type.Kind == TypeKind.Pointer
                        ? _e.ReflectionPass(f.Type, _emittedEnums, pointerSpelling)
                        : (Kind: 0, Type: "nullptr");
                    bool isPointer = (pointerPass.Kind & PassPointer) != 0;
                    int pointerDepth = ((pointerPass.Kind >> PassPointerDepthShift) & 0xFF) + 1;
                    string pointee = (pointerPass.Kind & PassFunctionPointer) != 0 ? "nullptr" : pointerPass.Type;
                    // A reflected static-field access must run the declaring type's
                    // .cctor first (.NET's lazy-initialization guarantee): unlike a
                    // compiled use site, this thunk has no emitted first-use guard,
                    // and under a deferred startup pass (the Godot mono-module
                    // backend) it can otherwise observe a not-yet-initialized static.
                    string ensure = "";
                    if (f.IsStatic && cls.StaticCctor is { } fldCc)
                    {
                        _c.NoteCctorEnsure(fldCc);
                        ensure = $"{fldCc.CppName}__ensure(); ";
                    }
                    // A headerless-typed field (the NFI trio, Assembly/Module — the
                    // headerless intrinsic pointers): a reflected GetValue is an
                    // escape into `object`, so it wraps like every other escape
                    // (MethodCompiler.Cast), and SetValue unwraps symmetrically.
                    // Keyed on the DECLARED member spelling: a shared-layout field
                    // (memberT erased to Dn2CppObject*) already holds the
                    // managed-object form, so it takes the plain reference path.
                    bool isHeaderless = MethodCompiler.IsHeaderlessWrapCpp(memberT);
                    string getBody = ensure + (isPointer
                        ? $"return dn2cpp_invoke_box_pointer((void*)({access}), {pointee}, {pointerDepth});"
                        : isHeaderless
                        ? $"return {MethodCompiler.HeaderlessWrapExpr(access, memberT, f.Type)};"
                        : isRef
                        ? $"return (Dn2CppObject*)({access});"
                        : $"{cppT} v = {access}; return dn2cpp_box({ftInfo}, &v, sizeof({cppT}));");
                    // A null value stores the default. The dispatcher converts null
                    // through the row's field type, which reads Object for a value type
                    // the image emits no type-info for.
                    string setBody = ensure + (isHeaderless
                        ? $"{access} = {MethodCompiler.HeaderlessUnwrapExpr("val", memberT)};"
                        : isRef
                        ? $"{access} = ({memberT})val;"
                        : $"{access} = val == nullptr ? {cppT}{{}} : *({cppT}*)((char*)val + sizeof(Dn2CppObject));");
                    if (f.Type.ContainsGcReferences())
                    {
                        if (f.IsStatic)
                        {
                            if (f.IsGcRootedThreadStatic)
                                setBody += $" dn2cpp_gc_write_barrier((void*)&({access}));";
                        }
                        else
                        {
                            setBody += " dn2cpp_gc_write_barrier((void*)o);";
                        }
                    }
                    _sb.AppendLine($"static Dn2CppObject* {getName}([[maybe_unused]] Dn2CppObject* o) {{ {getBody} }}");
                    _sb.AppendLine($"static void {setName}([[maybe_unused]] Dn2CppObject* o, [[maybe_unused]] Dn2CppObject* val) {{ {setBody} }}");
                    if (isPointer)
                    {
                        string checkName = $"fldcheck_{cls.CppName}_{f.CppName}";
                        // A static initonly function pointer accepts null during value
                        // validation; the dispatcher then refuses its store.
                        string nullCheck = f.IsStatic && (f.Attributes & System.Reflection.FieldAttributes.InitOnly) != 0
                            && (pointerPass.Kind & PassFunctionPointer) != 0 ? "if (val == nullptr) return nullptr; " : "";
                        string signature = _e.EmitBindingSignature(_sb,
                            _e.QuerySignature(PointerQuerySignature(f.Type, pointerSpelling?.Binding)), query: true);
                        _sb.AppendLine($"static Dn2CppObject* {checkName}(Dn2CppObject* val) {{ {nullCheck}return dn2cpp_field_pointer_value(val, {pointerPass.Kind}, {pointerPass.Type}, {signature}); }}");
                        valueCheck = $"&{checkName}";
                    }
                    (get, set) = ($"&{getName}", $"&{setName}");
                }
                (string Expr, int Count) ca = ("nullptr", 0);
                int fldToken = 0;
                if (fieldHandles.TryGetValue(f.Name, out var fdh))
                {
                    if (userFldCls)
                        ca = _e.BuildAttrTable(_sb, $"{cls.CppName}_fld{rows.Count}", cls.Module,
                            cls.Module.Reader.GetFieldDefinition(fdh).GetCustomAttributes());
                    fldToken = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(fdh);
                }
                rows.Add(new MetadataRow(new[] {
                    MetadataValue.Text(f.Name), MetadataValue.Ref(_e.TypeInfoRef(cls, "field row's declaring type")), MetadataValue.Ref(ftInfo),
                    MetadataValue.ExplicitSigned(attrs), MetadataValue.Ref(get), MetadataValue.Ref(set), MetadataValue.Ref(ca.Expr), MetadataValue.Signed(ca.Count),
                    MetadataValue.Signed((int)f.Attributes), MetadataValue.Signed(fldToken), MetadataValue.Signed(literalBits),
                    MetadataValue.Display(_e.ReflectionSignatureType(f.Type) + " " + f.Name),
                    MetadataValue.Ref(valueCheck),
                }));
            }
            _e.EmitMetadataTable(_sb, "Dn2CppFieldInfo", $"fldtab_{cls.CppName}", rows);
            _fieldTabs[cls] = ($"fldtab_{cls.CppName}", rows.Count);
        }

        // Builds one Dn2CppMethodInfo[] table (named "<prefix>_<cls>") for the given
        // members, emitting their parmtab_ arrays and invoker thunks; returns null for
        // an empty list. Shared by the method and constructor tables.
        private (string Expr, int Count)? BuildMemberTable(ClassInfo cls, List<MethodInfo> members, string prefix)
        {
            if (members.Count == 0)
                return null;
            string tab = $"{prefix}_{cls.CppName}";
            var rows = new List<MetadataRow>();
            bool appCls = cls.Module == _c.AppModule && !_e.IsOpaque(cls);
            // Attribute factories cover user-module (app + referenced library) non-opaque
            // classes, so a library class's method/param attributes are emitted; the
            // row-trim rule below stays app-module-keyed (a library's *own* unreached
            // rows are still trimmed — this only widens which classes get attribute tables).
            bool attrCls = _c.IsUserModule(cls.Module) && !_e.IsOpaque(cls);
            // A reference assembly's unreached members are reflected but never callable:
            // their fnPtr/invoker are null below, so Invoke throws and the row is nothing
            // but GetMethods() bloat. Property accessors still need descriptions so
            // a visible property never loses its getter/setter. An app module keeps every declared member,
            // so GetMethods() over the user's own types is unchanged, mirroring how a
            // program that reflects-and-invokes force-reaches app-module bodies only. A
            // hot-update base keeps everything: the interpreter binds a patch's imports by
            // walking these tables.
            // (Row filter mirrored by NoteReflectedMemberTypes — keep in step.)
            bool trim = !appCls && !_e._hotUpdateBase;
            var propertyAccessors = PropertyAccessorHandles(cls);
            foreach (var m in members)
            {
                int genericCount = m.Handle.IsNil ? 0
                    : m.Module.Reader.GetMethodDefinition(m.Handle).GetGenericParameters().Count;
                bool definitionOnly = genericCount > 0 && m.Context.MethodArgs.Length == 0;
                // Rva == 0 is a bodiless declaration -- an interface or abstract slot,
                // which is never reached (dispatch reaches the impl) yet must stay
                // visible, or the type's GetMethods() would come back empty.
                if (trim && m.Rva != 0 && !_c.Reachable.Contains(m) && !_c.KeepsUnreachedRow(m)
                    && !propertyAccessors.Contains(m.Handle))
                    continue;
                _memberAddr[m] = rows.Count.ToString();
                if (prefix == "methtab" && !m.Handle.IsNil && CoreIntrinsics.IsObjectMemberRowName(m.Name))
                {
                    if (!_objectMemberRows.TryGetValue(cls, out var named))
                        _objectMemberRows[cls] = named = new HashSet<MethodDefinitionHandle>();
                    named.Add(m.Handle);
                }
                int attrs = MetadataMemberAttrs((int)m.Attributes)
                    | (((int)m.Attributes & 0x800) != 0 ? 0x20 : 0)
                    | (genericCount > 0 ? 0x40 : 0);
                // Method + parameter custom attributes: the MethodDefinition's own
                // attributes, and each Parameter's, for an app-module non-opaque class.
                // Parameter handles are collected for every module: the raw
                // ParameterAttributes word (IsOptional/HasDefault) is emitted on BCL
                // rows too, unlike the attribute factories.
                (string Expr, int Count) mca = ("nullptr", 0);
                var paramHandles = new Dictionary<int, ParameterHandle>();
                try
                {
                    var rdr = m.Module.Reader;
                    var md = rdr.GetMethodDefinition(m.Handle);
                    if (attrCls)
                        mca = _e.BuildAttrTable(_sb, $"{prefix}_{cls.CppName}_{rows.Count}", m.Module, md.GetCustomAttributes());
                    foreach (var pph in md.GetParameters())
                    {
                        var par = rdr.GetParameter(pph);
                        if (par.SequenceNumber >= 1)
                            paramHandles[par.SequenceNumber - 1] = pph;
                    }
                }
                catch (Exception e) when (!Compilation.IsMustEscape(e))
                { /* no decodable method metadata */ }
                var ps = m.Signature.ParameterTypes;
                if (!definitionOnly)
                    _e.NoteBindingTypeArguments(cls, m, m.Signature.ReturnType, ps);
                var names = ParamNames(m, ps.Length);
                var genericDefinition = _e.ReflectionGenericMethodDefinition(m, names);
                var modifierSig = CustomModifiers(m);
                bool modifiersKnown = modifierSig is not null
                    && modifierSig.ParameterTypes.Length == ps.Length;
                string paramsExpr = "nullptr";
                if (ps.Length > 0)
                {
                    var prows = new List<MetadataRow>();
                    for (int i = 0; i < ps.Length; i++)
                    {
                        string ptInfo = definitionOnly && (Compilation.ContainsGenericVar(ps[i]) || Compilation.ContainsCanonPlaceholder(ps[i]))
                            ? "&dn2cpp_object_type" : _e.ReflectionAbiTypeInfoExpr(ps[i], _emittedEnums);

                        (string Expr, int Count) pca = ("nullptr", 0);
                        int pAttrs = 0;
                        string defaultValue = "nullptr";
                        if (paramHandles.TryGetValue(i, out var pph))
                        {
                            if (attrCls)
                                pca = _e.BuildAttrTable(_sb, $"{prefix}_{cls.CppName}_{rows.Count}_p{i}", m.Module,
                                    m.Module.Reader.GetParameter(pph).GetCustomAttributes());
                            pAttrs = (int)m.Module.Reader.GetParameter(pph).Attributes;
                            defaultValue = RenderParameterDefault(m, pph, ps[i],
                                $"{prefix}_{cls.CppName}_{rows.Count}_p{i}_default");
                        }
                        (string Expr, int Count) req = ("nullptr", 0);
                        (string Expr, int Count) opt = ("nullptr", 0);
                        if (modifiersKnown)
                        {
                            req = RenderCustomModifiers(modifierSig!.ParameterTypes[i].Required);
                            opt = RenderCustomModifiers(modifierSig.ParameterTypes[i].Optional);
                        }
                        string pdisplay = _e.ReflectionSignatureType(ps[i])
                            + (names[i] is { Length: > 0 } pName ? " " + pName : "");
                        var pass = definitionOnly ? (Kind: 0, Type: "nullptr")
                            : _e.ReflectionPass(ps[i], _emittedEnums, FunctionPointerSpelling(m, i, ps[i]));

                        prows.Add(new MetadataRow(new[] {
                            MetadataValue.Ref(ptInfo), MetadataValue.Text(names[i] is { Length: > 0 } parameterName ? parameterName : null),
                            MetadataValue.Ref(pca.Expr), MetadataValue.Signed(pca.Count), MetadataValue.Signed(pAttrs),
                            MetadataValue.Ref(req.Expr), MetadataValue.Signed(req.Count), MetadataValue.Ref(opt.Expr), MetadataValue.Signed(opt.Count),
                            MetadataValue.Signed(modifiersKnown ? 1 : 0), MetadataValue.Display(pdisplay), MetadataValue.Display(genericDefinition is { } pd ? pd.Parameters[i] : null),
                            MetadataValue.Text(genericDefinition is { } pk ? pk.ParameterKeys[i] : null),
                            MetadataValue.Signed(pass.Kind), MetadataValue.Ref(pass.Type),
                            MetadataValue.Ref(defaultValue),
                            MetadataValue.Ref(definitionOnly ? "nullptr" : _e.ReflectionBindingPointee(ps[i], _emittedEnums)),
                            MetadataValue.Ref(_e.EmitReflectionSignature(_sb, m, i)),
                        }));
                    }
                    // Intern byte-identical parameter tables across the whole
                    // emission (first definition wins; explicit `extern` because
                    // a namespace-scope const array defaults to internal
                    // linkage). Rows that reference a per-member attr table
                    // embed that unique symbol in the initializer text, so they
                    // never collide and simply get a pool entry of their own.
                    string init = _e.MetadataTableKey(prows);
                    if (_parmPools.TryGetValue(init, out var pooled))
                    {
                        paramsExpr = pooled;
                    }
                    else
                    {
                        string ptName = $"parmpool_{_parmPoolSeq++}";
                        _e.EmitMetadataTable(_sb, "Dn2CppParamInfo", ptName, prows, true);
                        _parmPools[init] = ptName;
                        paramsExpr = ptName;
                    }
                }
                // fnPtr + invoker: the emitted member's function pointer for Invoke
                // and its signature-deduplicated invoker thunk; both null when
                // the body was not reached/emitted (same guard as the ToString slot).
                // A skipped method with a synthesized engine-call body (hot-update
                // base) counts as emitted: the wrapper carries the method's normal
                // name and signature, so the standard wiring applies.
                string fnPtr = "nullptr";
                string invoker = "nullptr";
                // A delegate's Invoke is supplied by the runtime: once MethodInfo.Invoke or
                // Delegate.DynamicInvoke is in use, its row runs the type's multicast
                // invoker (dginvoke_*), which takes the delegate as an instance body takes
                // its receiver.
                bool delegateInvoke = !definitionOnly && cls.IsDelegate && m.Name == "Invoke" && !m.IsStatic
                    && (_c.NeedsDelegateInvokeRows || _c.ReflectionInvokeUsed)
                    && (_delegateInvokerNames ??= _e._delegateInvokerClasses
                        .Select(d => d.CppName).ToHashSet(System.StringComparer.Ordinal)).Contains(cls.CppName);
                if (delegateInvoke)
                {
                    fnPtr = $"(void*)&dginvoke_{cls.CppName}";
                    invoker = "(void*)&" + (InvokerThunkBlocker(m, DeclaredStructs) is { } blocked
                        ? InvokerMissStub(cls, m, blocked)
                        : _e.EmitInvokerThunk(_sb, m, _invokerThunks));
                }
                // Hot-update base build: a placeholder-bodied intrinsic surface
                // (the backend's call intrinsics own every call site; the emitted
                // body is dead `return default` code) is excluded from the wiring,
                // so a patch import fails at load as a standard unresolved import
                // instead of silently binding the placeholder. Gated on
                // --hotupdate-base so normal builds stay byte-identical.
                else if (!definitionOnly && _c.Reachable.Contains(m)
                    && (!_e._backend.ShouldSkipMethodBody(m.DeclaringClass, m) || _e._syntheticBodies.Contains(m))
                    && !(_e._hotUpdateBase && _e._backend.HasPlaceholderBody(m.DeclaringClass, m)))
                {
                    fnPtr = $"(void*)&{m.Emittable.CppName}";
                    // A template level's static body is the canonical one, which takes
                    // the generic context no forwarder of a clone could append.
                    bool contextArg = m.IsStatic && m.Emittable.RgctxParam && _e.IsRuntimeTemplateLevel(cls);
                    if (contextArg)
                        attrs |= ContextArgRow;
                    // The blocker can never fire here — a reached method's emitted body
                    // names the same CppTypes renderings in its own prototype, so they
                    // are declared (see InvokerThunkBlocker) — but the invariant "an
                    // invoker thunk is emitted only when its C++ compiles; otherwise
                    // the row's invoker traps" is asked at every mouth, not assumed.
                    invoker = "(void*)&" + (InvokerThunkBlocker(m, DeclaredStructs) is { } blocked
                        ? InvokerMissStub(cls, m, blocked)
                        : _e.EmitInvokerThunk(_sb, m, _invokerThunks, contextArg));
                }
                // A bodyless interface or abstract class row needs a signature-only
                // invoker only when a late-bound consumer resolves the receiver's slot
                // and calls its concrete body through that ABI. Hot-update base requests
                // an invoker for every callable interface row because its interpreter walks these tables.
                else if (!definitionOnly && (cls.IsInterface
                    ? _e._hotUpdateBase || (RowsEnteredLateBound && !m.IsStatic && m.IsVirtual)
                    : m.IsAbstract && RowsEnteredLateBound))
                {
                    try
                    {
                        // A signature-only row is kept whatever reachability says, so —
                        // unlike the reached arm above — its signature can name a by-value
                        // struct NOTHING in the image ever laid out (an uninstantiated
                        // specialization, an intrinsic-represented value type such as
                        // Span<char>), and a thunk over it is C++ that does not compile.
                        // Such a row gets the trapping stub instead: it stays bindable by
                        // name, and the actual invoke raises the catchable diagnosis.
                        invoker = "(void*)&" + (InvokerThunkBlocker(m, DeclaredStructs) is { } blocked
                            ? InvokerMissStub(cls, m, blocked)
                            : _e.EmitInvokerThunk(_sb, m, _invokerThunks));
                    }
                    // InstantiationBoundException IS a NotSupportedException, so the type
                    // alone does not filter out the must-escape set (Compilation.IsMustEscape).
                    catch (NotSupportedException e) when (!Compilation.IsMustEscape(e))
                    {
                        // A signature CppTypes cannot render stays un-invokable.
                    }
                }
                // The null-receiver run modes and a template level's
                // DN2CPP_MTHA_NULLLOOKUP_* masks: only a CreateDelegate binding called
                // with a null receiver (closed over null, or open over a non-virtual row)
                // enters an instance body without one. A multicast invoker reads its
                // receiver's chain, so such a binding of a delegate's Invoke faults.
                if (fnPtr != "nullptr" && !m.IsStatic && !delegateInvoke && _c.NeedsReflectionDelegateBind)
                    attrs |= _e.NullReceiverAttrs(cls, m);
                string retInfo = definitionOnly && (Compilation.ContainsGenericVar(m.Signature.ReturnType)
                    || Compilation.ContainsCanonPlaceholder(m.Signature.ReturnType))
                    ? "&dn2cpp_object_type" : _e.ReflectionAbiTypeInfoExpr(m.Signature.ReturnType, _emittedEnums);
                var retPass = definitionOnly ? (Attrs: 0, Referent: "nullptr", Signature: "nullptr")
                    : _e.ReflectionReturnPass(m.Signature.ReturnType, _emittedEnums,
                        FunctionPointerSpelling(m, -1, m.Signature.ReturnType));
                attrs |= retPass.Attrs;
                // sigShape: the method-import overload discriminator, baked only in
                // a --hotupdate-base build (the hot-update loader is the sole reader;
                // normal builds keep it null). It lets the loader tell same-(name,
                // arity, static) methods apart — chiefly a generic method's several
                // instantiations, all emitted under one name, which it tells apart
                // by type arguments (AbiContract.ImportShape).

                // Trailing raw ECMA words + token: MethodAttributes, MethodImplAttributes,
                // and the method's metadata token (MemberInfo.MetadataToken).
                int mdToken = m.Handle.IsNil ? 0 : System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(m.Handle);
                // Closed generic-method instantiation: bake the row's type arguments
                // so MakeGenericMethod resolves in-image (rows sharing the definition
                // token match argument-wise) and GetGenericArguments answers exactly.
                string genArgsExpr = "nullptr";
                if (m.Context.MethodArgs.Length > 0)
                {
                    var gargs = new List<string>(m.Context.MethodArgs.Length);
                    foreach (var ga in m.Context.MethodArgs)
                        gargs.Add(_e.MemberTypeInfoExpr(ga, _emittedEnums));
                    genArgsExpr = TypeArgumentVector(string.Join(", ", gargs));
                }
                (string Expr, int Count) retReq = ("nullptr", 0);
                (string Expr, int Count) retOpt = ("nullptr", 0);
                if (modifiersKnown)
                {
                    retReq = RenderCustomModifiers(modifierSig!.ReturnType.Required);
                    retOpt = RenderCustomModifiers(modifierSig.ReturnType.Optional);
                }
                // A class generic virtual override names the new slot its chain starts at,
                // which closed signatures cannot tell from a sibling slot: the base steps
                // to the declaring type and the definition's token.
                int gvmRootDepth = 0;
                int gvmRootToken = 0;
                if (_c.GvmChainRootOf(m) is { } gvmRoot)
                {
                    int depth = 0;
                    for (var b = cls; b is not null; b = b.BaseClass, depth++)
                        if (b == gvmRoot.DeclaringClass)
                        {
                            gvmRootDepth = depth;
                            gvmRootToken = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(gvmRoot.Handle);
                            break;
                        }
                }

                rows.Add(new MetadataRow(new[] {
                    MetadataValue.Text(m.Name), MetadataValue.Ref(_e.TypeInfoRef(cls, "method/ctor row's declaring type")),
                    MetadataValue.Ref(retInfo), MetadataValue.Ref(paramsExpr), MetadataValue.Signed(ps.Length), MetadataValue.ExplicitSigned(attrs),
                    MetadataValue.Signed(m.VtableSlot), MetadataValue.Ref(fnPtr), MetadataValue.Ref(invoker),
                    MetadataValue.Ref(mca.Expr), MetadataValue.Signed(mca.Count), MetadataValue.Text(_e._hotUpdateBase && !definitionOnly ? AbiContract.ImportShape(m.Signature, m.Context.MethodArgs) : null),
                    MetadataValue.Signed((int)m.Attributes), MetadataValue.Signed((int)m.ImplAttributes), MetadataValue.Signed(mdToken),
                    MetadataValue.Signed(genericCount), MetadataValue.Ref(genArgsExpr),
                    MetadataValue.Ref(retReq.Expr), MetadataValue.Signed(retReq.Count), MetadataValue.Ref(retOpt.Expr), MetadataValue.Signed(retOpt.Count),
                    MetadataValue.Signed(modifiersKnown ? 1 : 0), MetadataValue.Display(_e.ReflectionMethodDisplay(m)), MetadataValue.Display(genericDefinition is { } methodDisplayParts ? methodDisplayParts.Method : null),
                    MetadataValue.Display(_e.ReflectionSignatureType(m.Signature.ReturnType)), MetadataValue.Display(genericDefinition is { } rd ? rd.Return : null),
                    MetadataValue.Text(genericDefinition is { } rk ? rk.ReturnKey : null),
                    MetadataValue.Signed(gvmRootDepth), MetadataValue.Signed(gvmRootToken),
                    MetadataValue.Ref(retPass.Referent),
                    MetadataValue.Ref(RenderGenericParameters(m)),
                    MetadataValue.Ref(retPass.Signature),
                    MetadataValue.Ref(definitionOnly ? "nullptr" : _e.ReflectionBindingPointee(m.Signature.ReturnType, _emittedEnums)),
                    MetadataValue.Ref(_e.EmitReflectionSignature(_sb, m, -1)),
                }));
            }
            // The trim can empty a table the member list did not. A zero-length array is
            // ill-formed, and no _memberAddr entry survives to point into it, so report
            // "no table" — the ti_ then carries nullptr/0, as for a type with no members.
            if (rows.Count == 0)
                return null;
            _e.EmitMetadataTable(_sb, "Dn2CppMethodInfo", tab, rows);
            foreach (var m in members)
                if (_memberAddr.TryGetValue(m, out var index) && int.TryParse(index, out int rowIndex))
                    _memberAddr[m] = _e.MetadataRowAddress(tab, rowIndex);
            return (tab, rows.Count);
        }

        private readonly HashSet<string> _genericParameterTables = new(System.StringComparer.Ordinal);

        private string RenderGenericParameters(MethodInfo m)
        {
            if (m.Handle.IsNil)
                return "nullptr";
            var reader = m.Module.Reader;
            var handles = reader.GetMethodDefinition(m.Handle).GetGenericParameters();
            if (handles.Count == 0)
                return "nullptr";
            string symbol = "methodparams_" + m.Module.Index + "_"
                + System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(m.Handle);
            if (_genericParameterTables.Add(symbol))
            {
                var entries = new string[handles.Count];
                foreach (var handle in handles)
                {
                    var parameter = reader.GetGenericParameter(handle);
                    entries[parameter.Index] = "{ \"" + CLiteral(reader.GetString(parameter.Name))
                        + "\", " + (int)parameter.Attributes + " }";
                }
                _e._metadataHeader.AppendLine($"extern const Dn2CppMethodGenericParameter {symbol}[{handles.Count}];");
                _sb.AppendLine($"extern const Dn2CppMethodGenericParameter {symbol}[] = {{ {string.Join(", ", entries)} }};");
            }
            return symbol;
        }

        // Accessors refer to retained method-table descriptions; unreachable
        // bodies remain uncallable. Row conditions mirror NoteReflectedMemberTypes.
        private (string Expr, int Count)? BuildPropTable(ClassInfo cls, List<MethodInfo> methods)
        {
            var byHandle = new Dictionary<MethodDefinitionHandle, MethodInfo>();
            foreach (var m in methods)
                byHandle.TryAdd(m.Handle, m);
            var rows = new List<MetadataRow>();
            try
            {
                var reader = cls.Module.Reader;
                var td = reader.GetTypeDefinition(cls.Handle);
                foreach (var ph in td.GetProperties())
                {
                    var pd = reader.GetPropertyDefinition(ph);
                    var acc = pd.GetAccessors();
                    MethodInfo? getter = null, setter = null;
                    if (!acc.Getter.IsNil)
                        byHandle.TryGetValue(acc.Getter, out getter);
                    if (!acc.Setter.IsNil)
                        byHandle.TryGetValue(acc.Setter, out setter);
                    if (getter is null && setter is null)
                        continue;
                    string name = reader.GetString(pd.Name);
                    var psig = pd.DecodeSignature(_c.SigProvider, cls.Context);
                    string propType = _e.MemberTypeInfoExpr(psig.ReturnType, _emittedEnums);
                    string getterExpr = getter is not null && _memberAddr.TryGetValue(getter, out var ga) ? ga : "nullptr";
                    string setterExpr = setter is not null && _memberAddr.TryGetValue(setter, out var sa) ? sa : "nullptr";
                    // Accessibility for BindingFlags: public if either accessor is public;
                    // static if the accessors are static (an instance/static property is
                    // uniform across its accessors).
                    var anyAcc = getter ?? setter!;
                    bool pub = (getter?.IsPublic ?? false) || (setter?.IsPublic ?? false);
                    int attrs = (anyAcc.IsStatic ? 1 : 0) | (pub ? 2 : 4);
                    // Property custom attributes: user-module (app + referenced library)
                    // non-opaque classes.
                    (string Expr, int Count) pca = ("nullptr", 0);
                    if (_c.IsUserModule(cls.Module) && !_e.IsOpaque(cls))
                        pca = _e.BuildAttrTable(_sb, $"{cls.CppName}_prp{rows.Count}", cls.Module, pd.GetCustomAttributes());
                    int prpToken = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(ph);
                    IReadOnlyList<TypeDesc> indexParams = getter is not null
                        ? getter.Signature.ParameterTypes
                        : setter!.Signature.ParameterTypes.Take(setter.Signature.ParameterTypes.Length - 1).ToArray();
                    string display = _e.ReflectionPropertyDisplay(name, psig.ReturnType, indexParams);
                    rows.Add(new MetadataRow(new[] {
                        MetadataValue.Text(name), MetadataValue.Ref(_e.TypeInfoRef(cls, "property row's declaring type")), MetadataValue.Ref(propType),
                        MetadataValue.Ref(getterExpr), MetadataValue.Ref(setterExpr), MetadataValue.ExplicitSigned(attrs),
                        MetadataValue.Ref(pca.Expr), MetadataValue.Signed(pca.Count), MetadataValue.Signed(prpToken), MetadataValue.Display(display),
                    }));
                }
            }
            catch (Exception e) when (!Compilation.IsMustEscape(e))
            {
                // A metadata shape we can't decode just yields no properties.
            }
            if (rows.Count == 0)
                return null;
            string tab = $"proptab_{cls.CppName}";
            _e.EmitMetadataTable(_sb, "Dn2CppPropInfo", tab, rows);
            return (tab, rows.Count);
        }

        // Per-type reflection member tables: one Dn2CppMethodInfo[] of
        // non-ctor methods (methtab_) and one of instance constructors (ctortab_) per
        // type, plus a Dn2CppParamInfo[] per member that has parameters, and one invoker
        // thunk per distinct C++ ABI shape. Emitted before the type-info
        // definition so the initializer can name them. GetMethods excludes ctors and
        // GetConstructors excludes methods, matching .NET's split.
        private void RenderMemberTables(ClassInfo cls)
        {
            // Skip opaque/intrinsic types (System.Object/ValueType/Exception/…): their
            // members are intrinsic-dispatched and their ti_ sits in user types' base
            // chains, so emitting a table here would leak Object.ToString/Equals/etc.
            // into GetMethods, which unlike real .NET lists no Object member; a named
            // lookup answers those from the runtime's rows (dn2cpp_meta_lookup).
            if (cls.IsEnum || _e.IsOpaque(cls) || _e.IsCanonicalWorld(cls))
                return;
            // Dedupe by CppName: a class can list the same method twice (e.g. an
            // interface-impl entry shares the method's handle), which would both emit a
            // duplicate GetMethods row and collide the per-method parmtab_ array name.
            // A generic-method instantiation over canonical placeholders is not a
            // real member (no runtime Type exposes it, and its parameter table
            // would name never-emitted canonical-world type-infos) — skip it.
            var seenMethod = new HashSet<string>(System.StringComparer.Ordinal);
            bool keepRefl = _c.KeepsReflectionMetadata(cls);
            var propertyAccessors = PropertyAccessorHandles(cls);
            var methods = !keepRefl ? new List<MethodInfo>() : _e.ReflectionMethods(cls)
                .Where(m => m.Name != ".ctor" && m.Name != ".cctor"
                    && _e.KeepsReflectionMethodRow(cls, m, propertyAccessors)
                    && seenMethod.Add(m.CppName))
                .ToList();
            // Public constructor lookup keeps its instance-only table; named binding
            // describes the static initializer separately without exposing a MethodInfo.
            var seenCtor = new HashSet<string>(System.StringComparer.Ordinal);
            var ctors = cls.Methods
                .Where(m => m.Name == ".ctor" && !m.IsStatic && seenCtor.Add(m.CppName))
                .ToList();
            // --trim-reflection strips the METHOD and PROPERTY tables but keeps the
            // CONSTRUCTOR table: constructors are the one member family a reachable
            // cross-assembly caller resolves against a type chosen at RUN time (GodotSharp's
            // RuntimeTypeConversionHelper calls Type.GetConstructor on a type it picks out
            // of a Variant), i.e. exactly the undecidable case the keep-set cannot see, and
            // ctor rows are a small fraction of the relocation budget the other three tables
            // are. Constructors are never inherited, so nothing base-walks them and the kept
            // table is self-contained.
            if (keepRefl && BuildMemberTable(cls, methods, "methtab") is { } mt)
                _methodTabs[cls] = mt;
            if (BuildMemberTable(cls, ctors, "ctortab") is { } ct)
                _ctorTabs[cls] = ct;
            if (keepRefl && _c.NamedDelegateBindingUsed && cls.StaticCctor is { } initializer
                && _e.KeepsReflectionMethodRow(cls, initializer, propertyAccessors)
                && BuildMemberTable(cls, new List<MethodInfo> { initializer }, "initrow") is { } init)
                _initializerRows[cls] = _e.MetadataRowAddress(init.Expr, 0);
            // Property table: read the type's PropertyDefs from metadata, map each
            // accessor to its method-table entry (so PropertyInfo.GetValue/SetValue and
            // CanRead/CanWrite work), and emit a Dn2CppPropInfo[]. An accessor the
            // method table trimmed away has no _memberAddr entry and lands on the same
            // null-accessor path as a property that never had one. A stripped class emits no
            // property table either — and could not: Dn2CppPropInfo::getter/setter are
            // literally `&methtab_X[k]`, so the property table cannot outlive the method
            // table it points into.
            if (keepRefl && BuildPropTable(cls, methods) is { } pt)
                _propTabs[cls] = pt;
        }

        /// <summary>Whether every method <paramref name="cls"/> declares under a name
        /// <see cref="CoreIntrinsics.IsObjectMemberRowName"/> accepts got a method row, so
        /// the runtime may answer an Object or ValueType row through this level without
        /// passing over an override it has no row for. Reads only the raw TypeDef, so it
        /// decodes nothing; a class whose tables are stripped never qualifies.</summary>
        private bool CarriesObjectMemberRows(ClassInfo cls)
        {
            _objectMemberRows.Remove(cls, out var rows);
            if (cls.Handle.IsNil || !_c.KeepsReflectionMetadata(cls))
                return false;
            try
            {
                var reader = cls.Module.Reader;
                foreach (var handle in reader.GetTypeDefinition(cls.Handle).GetMethods())
                {
                    if (CoreIntrinsics.IsObjectMemberRowName(reader.GetString(reader.GetMethodDefinition(handle).Name))
                        && rows?.Contains(handle) != true)
                        return false;
                }
                return true;
            }
            catch (Exception e) when (!Compilation.IsMustEscape(e))
            {
                return false;
            }
        }

        // The class's type-info + interned Type object: the externally-visible
        // ti_/ty_ definitions closing its metadata block, referencing the
        // block's own file-local vtable/tables/thunks and (across TUs) only
        // header-declared symbols.
        /// <summary>Whether reflection-bound delegates or <c>Delegate.Method</c> can ask which
        /// method fills a slot, so <c>dn2cpp_renamed_slot_bodies</c> lists the fillers a name
        /// match cannot find.</summary>
        private bool RecordsRenamedSlotBodies => _c.NeedsReflectionDelegateBind || _c.ReadsDelegateMethod;

        /// <summary>Whether a filler named <paramref name="filler"/> is one the runtime's name
        /// match misses for member <paramref name="member"/>: neither the member's name nor
        /// an explicit implementation's qualified one.</summary>
        private static bool RenamesSlot(string filler, string member) =>
            filler != member && !filler.EndsWith("." + member, System.StringComparison.Ordinal);

        private static readonly string[] ObjectVirtualNames = { "ToString", "Equals", "GetHashCode", "Finalize" };

        /// <summary>Records the bodies <paramref name="cls"/>'s vtable runs for Object's virtual
        /// ToString, Equals, GetHashCode and Finalize, in the order
        /// <c>dn2cpp_renamed_slot_bodies</c> indexes them, where the name differs.</summary>
        private void NoteRenamedObjectFillers(ClassInfo cls)
        {
            MethodInfo?[] fillers =
            {
                Compilation.EffectiveToString(cls), Compilation.EffectiveEquals(cls),
                Compilation.EffectiveGetHashCode(cls), Compilation.EffectiveFinalize(cls),
            };
            for (int i = 0; i < fillers.Length; i++)
                if (fillers[i] is { } filler && RenamesSlot(filler.Name, ObjectVirtualNames[i])
                    && _c.Reachable.Contains(filler))
                    _e._renamedSlotRows.Add((cls, null, i, filler));
        }

        private void RenderTypeInfo(ClassInfo cls)
        {
            if (cls.IsEnum || _e.SkipsCanonicalMetadata(cls))
                return;
            // A base whose type-info the C++ runtime defines — System.Object, the exception
            // set the traps raise, System.Enum — chains to THAT handle: otherwise an object
            // the runtime allocated and one the generated code allocated are different types
            // to `is`/catch/typeof, and `typeof(MyClass).BaseType == typeof(object)` is
            // false. Such a base always chains, even from an OPAQUE class — the runtime's
            // handle is defined unconditionally, so there is nothing to be missing. An
            // opaque (referenced-only) class keeps a plain emitted base too when that base's
            // type-info is itself emitted (the opaque pass pulls base chains in), so isinst
            // / IsAssignableFrom over a typeof-only type still walks the real hierarchy.
            string baseExpr = !cls.IsValueType && cls.BaseClass is { } bc
                    && (CoreIntrinsics.RuntimeTypeInfoSymbol(bc) is not null
                        || !_e.IsOpaque(cls)
                        || (_e._emit.Contains(bc) && !bc.IsEnum && !_e.SkipsCanonicalMetadata(bc)))
                ? _e.TypeInfoRef(bc, "base type-info of an emitted class")
                : !cls.IsValueType && cls.BaseClass is null && Compilation.HasRuntimeObjectBase(cls)
                    ? "&dn2cpp_object_type"
                // An UNLOADED BCL exception base (a corelib-less `MyEx : SystemException`):
                // no ClassInfo exists to chain through, so chain straight to the shared
                // runtime handle — the per-type one when the runtime raises that base, else
                // the root Exception handle. This is what makes `is Exception` / catch-walks
                // and the hot-update loader's type_is_exception see an exception where the
                // absent base would otherwise cut the chain at nullptr.
                : !cls.IsValueType && cls.BaseClass is null
                        && cls.ExternalBaseName is { } extBase
                        && CoreIntrinsics.IsExternalBclExceptionName(extBase)
                    ? CoreIntrinsics.RuntimeExceptionTypeInfo(extBase) ?? "&dn2cpp_exception_type"
                    : "nullptr";
            string vt = cls.IsValueType || cls.IsInterface || cls.IsDelegate || _e.IsOpaque(cls) ? "nullptr" : cls.CppVtableName;
            bool hasInterfaces = _itfTables.ContainsKey(cls);
            string itfs = hasInterfaces ? _itfTabSyms[cls] : "nullptr";
            int interfaceCount = hasInterfaces ? _itfRowCounts[cls] : 0;
            if (RecordsRenamedSlotBodies && !cls.IsInterface)
                NoteRenamedObjectFillers(cls);
            // The overridden ToString dispatched for an instance of this type, cast
            // to the Object-receiver signature (the override accesses fields via the
            // concrete layout). Null when the type does not override ToString.
            // Only wire the override when its body is actually emitted — same
            // predicate as the method-body loop, so a reachable-but-skipped shim
            // method (e.g. a Godot engine ToString whose body is bridged) is not
            // referenced. A boxed value type's override expects the unboxed
            // payload (box + 1), so it is reached through an adjusting thunk that
            // offsets past the box header — this is what lets dn2cpp_object_tostring
            // format a boxed struct (Console.WriteLine(tuple) / string.Format / a
            // concat hole) via its own ToString rather than the type name.
            string toStr = "nullptr";
            if (Compilation.EffectiveToString(cls) is { } ts
                && _c.Reachable.Contains(ts)
                && !_e._backend.ShouldSkipMethodBody(ts.DeclaringClass, ts))
            {
                if (!cls.IsValueType)
                    toStr = $"(Dn2CppString*(*)(Dn2CppObject*))&{ts.Emittable.CppName}";
                else
                {
                    string thunk = $"tsthunk_{cls.CppName}";
                    _sb.AppendLine($"static Dn2CppString* {thunk}(Dn2CppObject* o) {{ return {ts.Emittable.CppName}(({cls.CppStructName}*)(o + 1)); }}");
                    toStr = $"&{thunk}";
                }
            }
            else if (EventSourceIdentity(cls) is not null)
            {
                // EventSource itself is opaque/intrinsic, so its override has no emitted
                // method body to wire. Every provider still carries modeled Name/Guid;
                // give its dynamic type the standard EventSource display thunk.
                toStr = "&dn2cpp_eventsource_tostring";
            }
            // GetHashCode / Equals(object) overrides wired into the type-info slots
            // so dn2cpp_object_gethashcode / _equals dispatch them. Same shape
            // as the ToString slot: a reference type's impl receives the object
            // pointer directly; a boxed value type's impl expects the unboxed payload
            // (box + 1), so it is reached through an adjusting thunk. The override's
            // Equals(object) takes the boxed `other` as-is (it isinst-checks/unboxes
            // internally), so only the receiver needs adjusting.
            //
            // A value type that overrides NEITHER must still not leave the slots null: null
            // means "reference equality / identity hash", which for two boxes of the same
            // value is false and two different numbers — silently. The minted field walk
            // (Compilation.SynthesizedValueEquals) fills the slot when reachability wired
            // one, which it does for a struct boxed in a program that compares objects at
            // all, so nothing else pays.
            string getHash = "nullptr";
            string equals = "nullptr";
            if (MethodCompiler.IntrinsicPointerValueType(TypeDesc.MakeClass(cls)) is { } ip)
            {
                string ghThunk = $"ghthunk_{cls.CppName}";
                _sb.AppendLine($"static int32_t {ghThunk}(Dn2CppObject* o) {{ {ip} v = *({ip}*)(o + 1); return (int32_t)((uint64_t)v ^ ((uint64_t)v >> 32)); }}");
                getHash = $"&{ghThunk}";
                string eqThunk = $"eqthunk_{cls.CppName}";
                _sb.AppendLine($"static int32_t {eqThunk}(Dn2CppObject* a, Dn2CppObject* b) {{ return (b && b->type == a->type && *({ip}*)(a + 1) == *({ip}*)(b + 1)) ? 1 : 0; }}");
                equals = $"&{eqThunk}";
            }
            else if ((Compilation.EffectiveGetHashCode(cls) ?? _c.ReachedSynthesizedValueHash(cls)) is { } gh
                && _c.Reachable.Contains(gh)
                && !_e._backend.ShouldSkipMethodBody(gh.DeclaringClass, gh))
            {
                if (!cls.IsValueType)
                    getHash = $"(int32_t(*)(Dn2CppObject*))&{gh.Emittable.CppName}";
                else
                {
                    // Both shapes take the unboxed payload, so one thunk serves the real
                    // override and the minted walk alike.
                    string thunk = $"ghthunk_{cls.CppName}";
                    _sb.AppendLine($"static int32_t {thunk}(Dn2CppObject* o) {{ return {gh.Emittable.CppName}(({cls.CppStructName}*)(o + 1)); }}");
                    getHash = $"&{thunk}";
                }
            }
            var synEq = _c.ReachedSynthesizedValueEquals(cls);
            if (equals == "nullptr" && (Compilation.EffectiveEquals(cls) ?? synEq) is { } eq
                && _c.Reachable.Contains(eq)
                && !_e._backend.ShouldSkipMethodBody(eq.DeclaringClass, eq))
            {
                if (!cls.IsValueType)
                    equals = $"(int32_t(*)(Dn2CppObject*,Dn2CppObject*))&{eq.Emittable.CppName}";
                else
                {
                    string thunk = $"eqthunk_{cls.CppName}";
                    // The minted walk is TYPED (its callers all hold two T's; boxing them to
                    // re-enter an object-taking signature would allocate on every probe), so
                    // the boxed entry is where the type check and the unbox live — the two
                    // lines a record's Equals(object) is. `b->type == a->type` rather than a
                    // baked &ti_: identical here (the receiver IS this class), and it keeps
                    // the thunk independent of which type-info symbol it sits next to.
                    _sb.AppendLine(ReferenceEquals(eq, synEq)
                        ? $"static int32_t {thunk}(Dn2CppObject* a, Dn2CppObject* b) {{ return (b && b->type == a->type) ? {eq.Emittable.CppName}(({cls.CppStructName}*)(a + 1), *({cls.CppStructName}*)(b + 1)) : 0; }}"
                        : $"static int32_t {thunk}(Dn2CppObject* a, Dn2CppObject* b) {{ return {eq.Emittable.CppName}(({cls.CppStructName}*)(a + 1), b); }}");
                    equals = $"&{thunk}";
                }
            }
            // The overridden Finalize() dispatched for an instance of this type
            // (what a C# ~T() destructor compiles down to), or null when the type
            // does not override Object.Finalize. Read by MethodCompiler.Newobj.cs
            // to decide whether allocating this type must call
            // dn2cpp_register_finalizer.
            // Finalizers are class-only in C# (no ~T() on a struct), so unlike
            // ToString/GetHashCode/Equals there is no boxed-value-type thunk case.
            string finalize = "nullptr";
            if (Compilation.EffectiveFinalize(cls) is { } fin
                && _c.Reachable.Contains(fin)
                && !_e._backend.ShouldSkipMethodBody(fin.DeclaringClass, fin))
            {
                finalize = $"(void(*)(Dn2CppObject*))&{fin.Emittable.CppName}";
            }
            // Boolean Type properties packed into flags so the dn2cpp_type_is_*
            // helpers answer for any instance (typeof or GetType()), not just static
            // folds. Enums are handled in their own loop above; here a class
            // marks ValueType/Interface/Abstract. In .NET an interface is also
            // abstract (typeof(IFoo).IsAbstract == true), so set both bits for one.
            var flagBits = new List<string>();
            if (cls.IsValueType)
                flagBits.Add("DN2CPP_TF_VALUETYPE");
            if (cls.IsInterface)
            {
                flagBits.Add("DN2CPP_TF_INTERFACE");
                flagBits.Add("DN2CPP_TF_ABSTRACT");
            }
            else if (cls.IsAbstract)
                flagBits.Add("DN2CPP_TF_ABSTRACT");
            // IsSealed = the metadata Sealed bit: value types, enums, delegates, sealed
            // classes and static (abstract+sealed) classes carry it; interfaces and
            // open/abstract classes do not. (Enums are emitted in their own loop above.)
            if (cls.IsSealed)
                flagBits.Add("DN2CPP_TF_SEALED");
            // IsByRefLike = ref struct (Span<T> and user ref structs).
            if (cls.IsByRefLike)
                flagBits.Add("DN2CPP_TF_BYREFLIKE");
            // IsNested = declared inside another type (metadata DeclaringType).
            if (IsNestedType(cls))
                flagBits.Add("DN2CPP_TF_NESTED");
            // Delegates: lets dn2cpp_object_gethashcode/_equals recognize a
            // delegate instance and dispatch the chain-aware delegate hash/
            // equality (delegates never wire the gethashcode/equals slots — see
            // the delegate carve-out in Compilation's override reach).
            if (cls.IsDelegate)
                flagBits.Add("DN2CPP_TF_DELEGATE");
            // --trim-reflection removed this type's field/method/property tables. The bit is
            // what keeps the strip HONEST: a stripped type and a genuinely member-less one
            // are indistinguishable from the tables alone (nullptr, 0), so without it
            // GetMethods() would answer an empty array instead of throwing. Constructors are
            // kept, so the bit does not speak for GetConstructor/Activator.
            if (!_c.KeepsReflectionMetadata(cls))
                flagBits.Add("DN2CPP_TF_METADATA_STRIPPED");
            if (CarriesObjectMemberRows(cls))
                flagBits.Add("DN2CPP_TF_OBJECT_MEMBER_ROWS");
            // Abstract System.Array base. Array type-infos (ti_arr_<T> and the runtime's
            // built-in / dynamically-built handles) carry base=nullptr, so `(array) is
            // Array` / castclass reaches nothing on the base chain; this bit lets
            // dn2cpp_isinst recognize an abstract-Array target the way it recognizes
            // System.Object through the shared runtime handle.
            if (cls.FullName == "System.Array")
                flagBits.Add("DN2CPP_TF_SYSTEM_ARRAY");
            // The six NON-generic interfaces every array implements. Stamping the bit lets
            // dn2cpp_isinst answer `(array) is IList` / `is ICloneable` from the array source
            // + this target alone, independent of whether the lazy SZArray dispatch map was
            // wired — an un-enumerated array's interface table is empty, so the test would
            // silently answer False. The map still drives the actual dispatch when a visible
            // call site wires it.
            if (cls.FullName is "System.Collections.IEnumerable"
                or "System.Collections.ICollection"
                or "System.Collections.IList"
                or "System.ICloneable"
                or "System.Collections.IStructuralComparable"
                or "System.Collections.IStructuralEquatable")
                flagBits.Add("DN2CPP_TF_ARRAY_ITF");
            // The SZArrayEnumerable<object> wrapper the shared reference-element array
            // fallback table forwards into: its closed-generic interface rows
            // are element-erased (reference elements share one C++ layout), so the
            // runtime's dispatch walk may serve any all-reference-argument
            // instantiation of the same definition from them — the enumerator a
            // fallback GetEnumerator hands back is dispatched at the caller's element
            // typing (IEnumerator<Attribute>). Dispatch only; type tests never read it.
            if (_c.IsRefErasedItfWrapper(cls))
                flagBits.Add("DN2CPP_TF_REF_ERASED_ITF");
            // An opaque value-type shell whose CLR layout extent the model could not
            // compute: the shell stayed empty, so the instanceSize about to be stamped
            // from sizeof() reads 1 — the same number a genuinely field-less struct
            // stamps. The bit is what separates the two, so the size readers can throw
            // naming the type instead of handing managed code a 1 (see
            // CppEmitter._layoutUnknown, which the shell-pad site populates).
            if (_e.HasUnknownLayoutExtent(cls))
                flagBits.Add("DN2CPP_TF_LAYOUT_UNKNOWN");
            // The marshalling verdict, asked of EVERY emitted class, reference types
            // included: the runtime's "not a value type" arm refuses them, and for a class
            // carrying an explicit [StructLayout(Sequential)] that refusal would be a false
            // claim about .NET, which measures one. The classifier answers INEXACT for
            // exactly those, so the runtime raises the declared PlatformNotSupportedException
            // instead — hence the INEXACT test sits AHEAD of the value-type test in
            // dn2cpp_marshal_require_size. A closed generic and a primitive are answered
            // before any of these bits, so their stamps are inert.
            //
            // NOT_MARSHALABLE is the marshalled-layout model's verdict, because that model
            // walks the same rules .NET does and so can tell "auto-layout / a field with no
            // unmanaged form" from "a shape dn2cpp declines to place". MARSHAL_INEXACT has
            // its own source and a different meaning — "instanceSize is not the marshalled
            // size", what the COPY paths need — so a struct with a bool field is both: it
            // carries a marshalled size AND is inexact, i.e. Marshal.SizeOf answers 4 while
            // PtrToStructure still refuses.
            switch (_c.TopLevelMarshalExtent(cls).Verdict)
            {
                case MarshalSizeVerdict.Refused:
                    flagBits.Add("DN2CPP_TF_NOT_MARSHALABLE");
                    break;
            }
            if (_c.MarshalVerdictOf(cls) != MarshalVerdict.Exact)
                flagBits.Add("DN2CPP_TF_MARSHAL_INEXACT");
            // A canonical-world type-info (shared generics): not a CLR type, and in practice
            // always a canonical INTERFACE, since SkipsCanonicalMetadata keeps every other
            // canonical class's type-info from emitting at all. It is reachable from managed
            // code through exactly one door — the canonical ALIAS rows the interface tables
            // carry over the real rows' slots — and the bit is what lets the readers that
            // hand a row OUTWARD as a managed Type (Type.GetInterfaces / FindInterfaces)
            // drop it while the dispatch and type-test walks keep it.
            if (_e.IsCanonicalWorld(cls))
                flagBits.Add("DN2CPP_TF_SHARED_CANON");
            // A runtime-instantiation template level: canonical (not a CLR type,
            // never handed outward) yet fully rendered, plus the bit the runtime
            // MakeGenericType fallback keys its clone sources on. The clone strips
            // both bits.
            else if (_e.IsRuntimeTemplateLevel(cls))
            {
                flagBits.Add("DN2CPP_TF_SHARED_CANON");
                flagBits.Add("DN2CPP_TF_RUNTIME_TEMPLATE");
            }
            string flags = flagBits.Count == 0 ? "0" : string.Join(" | ", flagBits);
            var (fieldsExpr, fieldCount) = _fieldTabs.TryGetValue(cls, out var ftv) ? (ftv.Expr, ftv.Count) : ("nullptr", 0);
            var (methodsExpr, methodCount) = _methodTabs.TryGetValue(cls, out var mtv) ? (mtv.Expr, mtv.Count) : ("nullptr", 0);
            var (ctorsExpr, ctorCount) = _ctorTabs.TryGetValue(cls, out var ctv) ? (ctv.Expr, ctv.Count) : ("nullptr", 0);
            var (propsExpr, propCount) = _propTabs.TryGetValue(cls, out var ptv) ? (ptv.Expr, ptv.Count) : ("nullptr", 0);
            // Type-level custom attributes: user-module (app + referenced library)
            // non-opaque classes, so an attribute on a library-declared class is emitted.
            (string Expr, int Count) typeAttrs = ("nullptr", 0);
            if (_c.IsUserModule(cls.Module) && !_e.IsOpaque(cls))
                try
                {
                    var td = cls.Module.Reader.GetTypeDefinition(cls.Handle);
                    typeAttrs = _e.BuildAttrTable(_sb, cls.CppName, cls.Module, td.GetCustomAttributes());
                }
                catch (Exception e) when (!Compilation.IsMustEscape(e))
                { /* no decodable type metadata -> no attributes */ }
            // Generic reflection: a closed instantiation points at its shared
            // open-definition handle and lists its closed type arguments. Non-generic
            // types leave these 0 (trailing-member convention). (Gate mirrored by
            // NoteReflectedMemberTypes for array and enum type arguments.)
            string genDefExpr = "nullptr", genArgsExpr = "nullptr";
            int genArgCount = 0;
            if (!_e.IsCanonicalWorld(cls)
                && _e.GenericDefInfo(cls) is { } gdi && _e._genericDefSyms.TryGetValue(gdi.DefName, out var defSym))
            {
                genDefExpr = "&" + defSym;
                // A template's placeholder arguments have no handles to list; the
                // runtime clone stamps the real argument vector (the genericDef is
                // still wired — the receiver-derived rgctx walk anchors on it).
                if (!_e.IsRuntimeTemplateLevel(cls))
                {
                    genArgsExpr = GenericArgVector(cls);
                    genArgCount = cls.Context.TypeArgs.Length;
                }
            }
            // Nested-type table: the type's public nested types. The enum metadata
            // members (24-26) are 0 for a class — spelled out so the nested fields land in
            // the right positional slots.
            var nested = _e.PublicNestedTypes(cls, _emittedEnums);
            string nestedExpr = "nullptr";
            if (nested.Count > 0)
            {
                string arr = "nested_" + cls.CppName;
                _sb.AppendLine($"static const Dn2CppTypeInfo* const {arr}[] = {{ {string.Join(", ", nested.Select(n => _e.TypeInfoRef(n, "nested-type table row")))} }};");
                nestedExpr = arr;
            }
            // Trailing rgctx member: the grouped instantiation's slot table
            // (see the rgctx tables above); every other type leaves it null.
            string rgctxExpr = _rgctxTableSyms.Contains(cls) ? $"rgctx_{cls.CppName}" : "nullptr";
            // The members after typeObject are formatspec (never wired for an
            // emitted class), the raw ECMA TypeAttributes word + metadata token,
            // and the [DefaultMember]-declared member name (GetDefaultMembers).
            var (tIlAttrs, tToken) = TypeIlMeta(cls);
            _e.EmitTypeInfo(_sb, cls.CppTypeInfoDefName, new TypeMetadata {
                Native = _e._metadataNativeRows,
                Name = Compilation.ReflectionTypeName(cls), Base = baseExpr,
                InstanceSize = $"(int32_t)sizeof({cls.CppStructName})", Vtable = vt, Interfaces = itfs, InterfaceCount = interfaceCount,
                ToStringFn = toStr, HashFn = getHash, EqualsFn = equals, Flags = flags,
                Fields = fieldsExpr, FieldCount = fieldCount, Methods = methodsExpr, MethodCount = methodCount,
                Ctors = ctorsExpr, CtorCount = ctorCount, Props = propsExpr, PropCount = propCount,
                Initializer = _initializerRows.GetValueOrDefault(cls, "nullptr"),
                CustomAttrs = typeAttrs.Expr, CustomAttrCount = typeAttrs.Count, GenericDef = genDefExpr,
                GenericArgs = genArgsExpr, GenericArgCount = genArgCount, NestedTypes = nestedExpr, NestedCount = nested.Count,
                AssemblyName = string.IsNullOrEmpty(cls.Module.AssemblyName) ? null : cls.Module.AssemblyName, FinalizeFn = finalize, Rgctx = rgctxExpr, TypeObject = "&ty_" + cls.CppName,
                IlAttrs = unchecked((uint)tIlAttrs), MetadataToken = tToken, DefaultMemberName = DefaultMemberName(cls), MarshalSize = _c.MarshalStampSize(cls),
                EventSourceName = EventSourceIdentity(cls)?.Name, EventSourceGuid = EventSourceIdentity(cls)?.Guid,
            });
            _sb.AppendLine($"const Dn2CppType ty_{cls.CppName} = {{ {{ &dn2cpp_type_type }}, {_e.TypeInfoRef(cls, "class ty_ companion")} }};");
            // A bind-kind type whose layout/vtable/metadata this emission knows: its handle
            // is the runtime's, so the metadata just rendered has to be copied INTO that
            // handle at startup. An OPAQUE one (System.Exception, System.AggregateException
            // — intrinsic, no fields, no vtable, and a runtime object layout the emitted
            // struct does not describe) is left alone: the runtime's own stub IS its truth.
            if (CoreIntrinsics.RuntimeTypeInfoSymbol(cls) is not null && !_e.IsOpaque(cls))
                _e._typeBinds.Add(cls);
        }
    }
}
