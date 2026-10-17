using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed class RawAsyncModule
{
    internal required int Index;
    internal required string AssemblyName;
    internal required MetadataReader Reader;
}

// Metadata-only classification is shared with retention. Its resolver returns
// original definitions and never substitutes or forms a generic instance.
internal sealed class RawAsyncAdoption
{
    private readonly IReadOnlyList<RawAsyncModule> _modules;
    private readonly Func<RawAsyncModule, TypeReferenceHandle, (RawAsyncModule Module, TypeDefinitionHandle Handle)?> _resolveType;
    private readonly Func<string, (RawAsyncModule Module, TypeDefinitionHandle Handle)?> _resolveSerialized;
    private readonly Func<MetadataReader, CustomAttribute, string?> _attributeName;
    private readonly Action<string>? _diagnostic;
    private readonly Action? _step;
    private readonly bool _captureEvidence;
    internal readonly List<AsyncAdoptionCandidate> OriginalCandidates = new();
    internal readonly List<DeclineWitness> DeclineWitnesses = new();
    internal readonly List<ContractReference> ContractReferences = new();
    internal readonly record struct ContractReference(RawAsyncModule Module, MemberReferenceHandle Reference,
        RawAsyncModule TargetModule, TypeDefinitionHandle Target);
    internal readonly record struct DeclineWitness(string AssemblyName, bool RejectsEveryRole);

    internal RawAsyncAdoption(IReadOnlyList<RawAsyncModule> modules,
        Func<RawAsyncModule, TypeReferenceHandle, (RawAsyncModule Module, TypeDefinitionHandle Handle)?> resolveType,
        Func<string, (RawAsyncModule Module, TypeDefinitionHandle Handle)?> resolveSerialized,
        Func<MetadataReader, CustomAttribute, string?> attributeName, Action<string>? diagnostic = null, Action? step = null, bool captureEvidence = false)
    {
        _modules = modules;
        _resolveType = resolveType;
        _resolveSerialized = resolveSerialized;
        _attributeName = attributeName;
        _diagnostic = diagnostic;
        _step = step;
        _captureEvidence = captureEvidence;
    }

    internal static IReadOnlyList<string> BuilderMethodNames => s_builderShape;

    internal List<AsyncAdoptionCandidate> Classify(IReadOnlyList<string> noAdopt)
    {
        var candidates = new List<AsyncAdoptionCandidate>();
        foreach (var module in _modules)
        {
            if (noAdopt.Any(name => string.Equals(module.AssemblyName, name, StringComparison.OrdinalIgnoreCase)))
                continue;
            var reader = module.Reader;
            foreach (var handle in reader.TypeDefinitions)
            {
                _step?.Invoke();
                var definition = reader.GetTypeDefinition(handle);
                if (AsyncMethodBuilderArg(reader, definition) is { } builder
                    && CollectAsyncTaskCandidate(module, handle, definition, builder) is { } candidate)
                    candidates.Add(candidate);
            }
        }
        if (_captureEvidence) OriginalCandidates.AddRange(candidates);
        if (candidates.Count == 0) return candidates;
        var declined = DeclineOutOfContractAdoptions(candidates);
        return candidates.Where(candidate => !declined.Contains(candidate.AssemblyName)).ToList();
    }

    private static string TypeFullName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var type = reader.GetTypeDefinition(handle);
        string ns = reader.GetString(type.Namespace), name = reader.GetString(type.Name);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    private static string TypeRefFullName(MetadataReader reader, TypeReferenceHandle handle)
    {
        var type = reader.GetTypeReference(handle);
        string ns = reader.GetString(type.Namespace), name = reader.GetString(type.Name);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    /// <summary>The task-like role, the builder role and the awaiter role of one would-be
    /// adoption — each with its own member contract in the pre-scan.</summary>
    internal enum AdoptedRole { TaskType, Builder, Awaiter }

    /// <summary>The ordered roles of one candidate, before assembly-wide decline.</summary>
    internal sealed class AsyncAdoptionCandidate
    {
        // Decline granularity is the ASSEMBLY, matching --no-adopt-async, and for the same
        // reason: sibling task types' promises interlock, so half-adopting one is unsound.
        public required string AssemblyName;
        public required bool IsStruct;
        public required int TaskArity;
        public required string TaskSignature;
        public required string BuilderSignature;
        public string? AwaiterSignature;
        public required List<(RawAsyncModule Module, TypeDefinitionHandle Handle,
            (string Cpp, string Key) Mapping, AdoptedRole Role)> Rows;
    }

    private static string OpenTypeSignature(MetadataReader reader, TypeDefinitionHandle handle)
    {
        string name = RawSignatureProvider.TypeDefinitionName(reader, handle);
        int arity = reader.GetTypeDefinition(handle).GetGenericParameters().Count;
        if (arity == 0)
            return name;
        string result = name + "<";
        for (int i = 0; i < arity; i++)
        {
            if (i != 0)
                result += ",";
            result += $"!{i}";
        }
        return result + ">";
    }

    /// <summary>Collects one task-like type, its builder and its awaiter as an adoption
    /// candidate, or nothing if any of the three does not fit the model. The commit loop
    /// TryAdds the rows in this order, so a type claimed in one role keeps it (a
    /// fire-and-forget task type — UniTaskVoid's shape — whose GetAwaiter returns itself
    /// stays the TASK, and awaiting it then fails loud, which is what real .NET does
    /// too).</summary>
    private AsyncAdoptionCandidate? CollectAsyncTaskCandidate(
        RawAsyncModule module, TypeDefinitionHandle tdh, TypeDefinition td, string builderName)
    {
        var reader = module.Reader;
        string taskName = DefFullName(reader, td);

        // The BCL's own task-like types (Task<T> and ValueTask/ValueTask<T> all carry a
        // type-level [AsyncMethodBuilder]) are already modeled by the static maps.
        if (CoreIntrinsicTypeMaps.IntrinsicGenericCppType(taskName) is not null
            || CoreIntrinsicTypeMaps.IsIntrinsicType(taskName))
            return null;
        // Arity 0 (async GDTask) or 1 (async GDTask<T>) — the only shapes the Task-family
        // runtime structs model.
        if (td.GetGenericParameters().Count > 1)
            return null;

        if (_resolveSerialized(builderName) is not { } b)
            return null; // the builder's assembly is not loaded: nothing to adopt it to
        var btd = b.Module.Reader.GetTypeDefinition(b.Handle);
        // The builder is embedded BY VALUE in the state machine (Dn2CppAsyncBuilder), and
        // it must carry the compiler-required shape or the intrinsic cases have nothing to
        // answer. (Start / AwaitOnCompleted / AwaitUnsafeOnCompleted are generic and route
        // through TranslateAsyncGenericIntrinsic, which never consults the dispatch key.)
        if (!IsValueTypeDef(b.Module.Reader, btd))
            return null;
        foreach (string required in s_builderShape)
            if (!HasMethodNamed(b.Module.Reader, btd, required))
                return null;

        // A struct task type is modeled on ValueTask, a reference one on Task. The kind
        // drives all three keys TOGETHER: get_Task's C++ shape differs between the two
        // builder switches (a Dn2CppTask* by reference vs a Dn2CppTaskAwaiter by value),
        // so a mismatched pair would not even compile. The struct arm is also the
        // semantically right one — a zeroed `default(GDTask)` is a null task pointer, and
        // dn2cpp_vtask normalizes null to a pre-completed sentinel, which is exactly what
        // `default(UniTask)` means; the Task arm's raw ->task->status read would fault.
        bool isStruct = IsValueTypeDef(reader, td);
        var rows = new List<(RawAsyncModule, TypeDefinitionHandle, (string, string), AdoptedRole)>(3)
        {
            (module, tdh, isStruct
                ? ("Dn2CppTaskAwaiter", "System.Threading.Tasks.ValueTask")
                : ("Dn2CppTask*", "System.Threading.Tasks.Task"), AdoptedRole.TaskType),
            (b.Module, b.Handle, ("Dn2CppAsyncBuilder", isStruct
                ? "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder"
                : "System.Runtime.CompilerServices.AsyncTaskMethodBuilder"), AdoptedRole.Builder),
        };
        var candidate = new AsyncAdoptionCandidate
        {
            AssemblyName = module.AssemblyName,
            IsStruct = isStruct,
            TaskArity = td.GetGenericParameters().Count,
            TaskSignature = OpenTypeSignature(reader, tdh),
            BuilderSignature = OpenTypeSignature(b.Module.Reader, b.Handle),
            Rows = rows,
        };

        // The awaiter, discovered structurally: what GetAwaiter() returns. Optional — a
        // fire-and-forget task type has none — and skipped when it is an interface, which
        // the concrete Dn2CppTaskAwaiter value struct cannot back.
        if (AwaiterOf(module, td) is not { } a)
            return candidate;
        var atd = a.Module.Reader.GetTypeDefinition(a.Handle);
        if ((atd.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface)
            return candidate;
        candidate.AwaiterSignature = OpenTypeSignature(a.Module.Reader, a.Handle);
        rows.Add((a.Module, a.Handle, ("Dn2CppTaskAwaiter", isStruct
            ? "System.Runtime.CompilerServices.ValueTaskAwaiter"
            : "System.Runtime.CompilerServices.TaskAwaiter"), AdoptedRole.Awaiter));
        return candidate;
    }

    /// <summary>The members the C# compiler requires of an async method builder, and which
    /// the Task-family intrinsic switches answer by the same names. Start /
    /// AwaitOnCompleted / AwaitUnsafeOnCompleted are generic and handled separately.</summary>
    private static readonly string[] s_builderShape =
        { "Create", "Start", "SetStateMachine", "SetResult", "SetException", "get_Task" };

    /// <summary>The one task-type FIELD an adopted type models: the pre-completed constant
    /// (MethodCompiler.TryIntrinsicStaticField). Builders and awaiters model no field.</summary>
    private const string AdoptedTaskFieldContract = "CompletedTask";

    /// <summary>Raw signature decoding does not form model types.</summary>
    private readonly record struct AsyncMemberShape(bool IsField, bool IsInstance,
        int GenericArity, string ReturnType, ImmutableArray<string> Parameters);

    private static bool IsAdoptedMember(
        AsyncAdoptionCandidate candidate, AdoptedRole role, string name, AsyncMemberShape s)
    {
        if (s.IsField)
            return role == AdoptedRole.TaskType && name == AdoptedTaskFieldContract
                && !s.IsInstance && s.ReturnType == candidate.TaskSignature;
        bool Method(bool instance, int arity, int parameters) =>
            s.IsInstance == instance && s.GenericArity == arity && s.Parameters.Length == parameters;
        bool Returns(string type) => s.ReturnType == type;
        bool Parameter(int index, string type) => s.Parameters[index] == type;
        string configured = candidate.IsStruct
            ? "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable"
            : "System.Runtime.CompilerServices.ConfiguredTaskAwaitable";
        if (candidate.TaskArity != 0)
            configured += "`1<!0>";
        string task = candidate.TaskArity == 0
            ? "System.Threading.Tasks.Task"
            : "System.Threading.Tasks.Task`1<!0>";

        return role switch
        {
            AdoptedRole.TaskType => name switch
            {
                "GetAwaiter" => Method(true, 0, 0)
                    && candidate.AwaiterSignature is { } awaiter && Returns(awaiter),
                "get_Result" => candidate.TaskArity == 1 && Method(true, 0, 0) && Returns("!0"),
                "AsTask" => Method(true, 0, 0) && Returns(task),
                "ConfigureAwait" => Method(true, 0, 1) && Parameter(0, "System.Boolean")
                    && Returns(configured),
                "get_IsCompleted" or "get_IsCompletedSuccessfully" =>
                    Method(true, 0, 0) && Returns("System.Boolean"),
                "get_CompletedTask" => Method(false, 0, 0) && Returns(candidate.TaskSignature),
                "FromCanceled" => Method(false, 0, 1)
                    && Parameter(0, "System.Threading.CancellationToken")
                    && Returns(candidate.TaskSignature),
                "FromException" => Method(false, 0, 1)
                    && Parameter(0, "System.Exception") && Returns(candidate.TaskSignature),
                _ => false,
            },
            AdoptedRole.Builder => name switch
            {
                "Create" => Method(false, 0, 0) && Returns(candidate.BuilderSignature),
                "Start" => Method(true, 1, 1) && Parameter(0, "!!0&")
                    && Returns("System.Void"),
                "SetStateMachine" => Method(true, 0, 1)
                    && Parameter(0, "System.Runtime.CompilerServices.IAsyncStateMachine")
                    && Returns("System.Void"),
                "SetResult" => Returns("System.Void") && (candidate.TaskArity == 0
                    ? Method(true, 0, 0)
                    : Method(true, 0, 1) && Parameter(0, "!0")),
                "SetException" => Method(true, 0, 1) && Parameter(0, "System.Exception")
                    && Returns("System.Void"),
                "get_Task" => Method(true, 0, 0) && Returns(candidate.TaskSignature),
                "AwaitOnCompleted" or "AwaitUnsafeOnCompleted" => Method(true, 2, 2)
                    && Parameter(0, "!!0&") && Parameter(1, "!!1&")
                    && Returns("System.Void"),
                _ => false,
            },
            _ => name switch
            {
                "GetAwaiter" => Method(true, 0, 0)
                    && candidate.AwaiterSignature is { } awaiter && Returns(awaiter),
                "GetResult" => Method(true, 0, 0)
                    && Returns(candidate.TaskArity == 0 ? "System.Void" : "!0"),
                "get_IsCompleted" => Method(true, 0, 0) && Returns("System.Boolean"),
                "OnCompleted" or "UnsafeOnCompleted" => Method(true, 0, 1)
                    && Parameter(0, "System.Action") && Returns("System.Void"),
                _ => false,
            },
        };
    }

    private static AsyncMemberShape? ReadAsyncMemberShape(MetadataReader reader, MemberReference mr,
        RawAsyncModule targetModule, TypeDefinitionHandle targetHandle)
    {
        try
        {
            var header = reader.GetBlobReader(mr.Signature).ReadSignatureHeader();
            if (header.Kind == SignatureKind.Field)
            {
                string fieldType = mr.DecodeFieldSignature(RawSignatureProvider.Instance, null);
                var targetReader = targetModule.Reader;
                string member = reader.GetString(mr.Name);
                foreach (var fieldHandle in targetReader.GetTypeDefinition(targetHandle).GetFields())
                {
                    var field = targetReader.GetFieldDefinition(fieldHandle);
                    if (targetReader.GetString(field.Name) != member
                        || field.DecodeSignature(RawSignatureProvider.Instance, null) != fieldType)
                        continue;
                    bool isInstance = (field.Attributes & FieldAttributes.Static) == 0;
                    return new AsyncMemberShape(true, isInstance, 0, fieldType, []);
                }
                return null;
            }
            if (header.Kind != SignatureKind.Method)
                return null;
            var signature = mr.DecodeMethodSignature(RawSignatureProvider.Instance, null);
            return new AsyncMemberShape(false, header.IsInstance,
                signature.GenericParameterCount, signature.ReturnType, signature.ParameterTypes);
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    /// <summary>The adoption pre-scan: walks every module's MemberReference table for a
    /// cross-assembly reference to a candidate's task/builder/awaiter outside that role's
    /// contract, and returns the assembly simple names to decline (their real IL then
    /// transpiles, exactly as --no-adopt-async would arrange). Signature shapes decode
    /// through raw metadata only and cannot form generic specializations.
    /// Over-detection (a dead MemberRef declining a
    /// live adoption) is safe: declined IL is merely transpiled for real. A same-module
    /// reference is skipped — same-assembly out-of-contract calls are MethodDefs or
    /// same-module MemberRefs and stay the emitter's fail-loud hole.</summary>
    private HashSet<string> DeclineOutOfContractAdoptions(List<AsyncAdoptionCandidate> candidates)
    {
        // The role/mapping still follows first-claim wins. Ownership does not: one shared
        // builder or awaiter makes every task assembly depend on the same contract.
        var byHandle = new Dictionary<(int, TypeDefinitionHandle),
            (AdoptedRole Role, List<AsyncAdoptionCandidate> Candidates)>();
        foreach (var cand in candidates)
            foreach (var row in cand.Rows)
            {
                var key = (row.Module.Index, row.Handle);
                if (!byHandle.TryGetValue(key, out var target))
                {
                    target = (row.Role, new List<AsyncAdoptionCandidate>());
                    byHandle.Add(key, target);
                }
                if (!target.Candidates.Contains(cand))
                    target.Candidates.Add(cand);
            }
        // Candidate (namespace, name) pairs: the allocation-free prefilter a TypeReference
        // parent must pass before the (allocating) full resolve is paid for.
        var names = new List<(string Ns, string Name)>();
        foreach (var cand in candidates)
            foreach (var row in cand.Rows)
            {
                var td = row.Module.Reader.GetTypeDefinition(row.Handle);
                var key = (row.Module.Reader.GetString(td.Namespace), row.Module.Reader.GetString(td.Name));
                if (!names.Contains(key))
                    names.Add(key);
            }

        var declined = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in _modules)
        {
            var reader = module.Reader;
            foreach (var mrh in reader.MemberReferences)
            {
                _step?.Invoke();
                var mr = reader.GetMemberReference(mrh);
                EntityHandle parent = mr.Parent;
                // A generic candidate is referenced through a TypeSpec (GENERICINST
                // (CLASS|VALUETYPE) TypeDefOrRefOrSpec ...): peel it to the open template
                // handle the registry keys on, exactly as AwaiterOf reads a return blob.
                if (parent.Kind == HandleKind.TypeSpecification)
                {
                    try
                    {
                        var blob = reader.GetBlobReader(
                            reader.GetTypeSpecification((TypeSpecificationHandle)parent).Signature);
                        var tc = blob.ReadSignatureTypeCode();
                        if (tc == SignatureTypeCode.GenericTypeInstance)
                            tc = blob.ReadSignatureTypeCode();
                        if (tc != SignatureTypeCode.TypeHandle)
                            continue;
                        parent = blob.ReadTypeHandle();
                    }
                    catch (BadImageFormatException)
                    {
                        continue; // an unreadable TypeSpec references no candidate
                    }
                }
                // A TypeDefinition parent (bare or inside the TypeSpec) is a same-module
                // reference; every other kind (MethodDef vararg site, ModuleRef) is not a
                // type member reference at all.
                if (parent.Kind != HandleKind.TypeReference)
                    continue;
                var tr = reader.GetTypeReference((TypeReferenceHandle)parent);
                bool maybe = false;
                foreach (var (ns, nm) in names)
                    if (reader.StringComparer.Equals(tr.Name, nm)
                        && reader.StringComparer.Equals(tr.Namespace, ns))
                    {
                        maybe = true;
                        break;
                    }
                if (!maybe)
                    continue;
                var resolved = _resolveType(module, (TypeReferenceHandle)parent);
                var m = resolved?.Module;
                var h = resolved?.Handle ?? default;
                if (m is null || m.Index == module.Index
                    || !byHandle.TryGetValue((m.Index, h), out var target))
                    continue;
                if (_captureEvidence)
                {
                    _step?.Invoke();
                    ContractReferences.Add(new(module, mrh, m, h));
                }
                string member = reader.GetString(mr.Name);
                var shape = ReadAsyncMemberShape(reader, mr, m, h);
                bool inContract = shape is not null;
                if (shape is { } memberShape)
                    foreach (var candidate in target.Candidates)
                        if (!IsAdoptedMember(candidate, target.Role, member, memberShape))
                        {
                            inContract = false;
                            break;
                        }
                if (inContract)
                    continue;
                // stderr only: the notice must not perturb the generated bytes.
                foreach (var candidate in target.Candidates)
                {
                    string assemblyName = candidate.AssemblyName;
                    if (_captureEvidence)
                    {
                        _step?.Invoke();
                        bool rejectsEveryRole = shape is not { } originalShape
                            || !IsAdoptedMember(candidate, AdoptedRole.TaskType, member, originalShape)
                                && !IsAdoptedMember(candidate, AdoptedRole.Builder, member, originalShape)
                                && !IsAdoptedMember(candidate, AdoptedRole.Awaiter, member, originalShape);
                        DeclineWitnesses.Add(new(assemblyName, rejectsEveryRole));
                    }
                    if (declined.Add(assemblyName))
                        _diagnostic?.Invoke(
                            $"dn2cpp: adoption declined: {DefFullName(m.Reader, m.Reader.GetTypeDefinition(h))}"
                            + $"::{member} is outside the mapped contract; transpiling "
                            + $"{assemblyName}'s real IL");
                }
            }
        }
        return declined;
    }

    /// <summary>The assembly-qualified builder type name in a type-level
    /// <c>[AsyncMethodBuilder(typeof(B))]</c>, or null. Read straight off the value blob
    /// (ECMA-335 II.23.3: a 0x0001 prolog, then the SerString type name) exactly as
    /// the primary does — DecodeCustomAttributes drops a CoreLib
    /// attribute like AsyncMethodBuilderAttribute unless user code typeof-names it, and
    /// this read must work regardless. METHOD-level
    /// <c>[AsyncMethodBuilder]</c> (the BCL's own PoolingAsyncValueTaskMethodBuilder
    /// overrides on the async Stream paths) is deliberately not read: it swaps the builder
    /// for one method, it does not make a type task-like.</summary>
    private string? AsyncMethodBuilderArg(MetadataReader reader, TypeDefinition td)
    {
        foreach (var cah in td.GetCustomAttributes())
        {
            var ca = reader.GetCustomAttribute(cah);
            if (_attributeName(reader, ca) != "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute")
                continue;
            try
            {
                var br = reader.GetBlobReader(ca.Value);
                if (br.Length >= 2 && br.ReadUInt16() == 0x0001
                    && br.ReadSerializedString() is { Length: > 0 } name)
                    return name;
            }
            catch (BadImageFormatException)
            {
                // An undecodable blob adopts nothing.
            }
        }
        return null;
    }

    /// <summary>The awaiter a task-like type's parameterless <c>GetAwaiter()</c> returns,
    /// as a (module, TypeDef) — read off the signature BLOB rather than decoded, because
    /// for a generic task the return type is <c>GDTask&lt;!0&gt;.Awaiter</c> and decoding
    /// that here (before Pass 2) would instantiate a bogus open specialization. The return
    /// shape is <c>[GENERICINST] (CLASS|VALUETYPE) TypeDefOrRefOrSpec</c>:
    /// SignatureTypeCode.TypeHandle collapses CLASS and VALUETYPE, and for a GENERICINST
    /// the handle names the open template — which is what the registry keys on. Null when
    /// there is no such method, or its return is not a plain (possibly generic) type.</summary>
    private (RawAsyncModule Module, TypeDefinitionHandle Handle)? AwaiterOf(RawAsyncModule module, TypeDefinition td)
    {
        var reader = module.Reader;
        foreach (var mdh in td.GetMethods())
        {
            var md = reader.GetMethodDefinition(mdh);
            if (reader.GetString(md.Name) != "GetAwaiter")
                continue;
            EntityHandle ret;
            try
            {
                var blob = reader.GetBlobReader(md.Signature);
                var hdr = blob.ReadSignatureHeader();
                if (hdr.IsGeneric)
                    blob.ReadCompressedInteger();       // method generic-param count
                if (blob.ReadCompressedInteger() != 0)  // parameter count: must be 0
                    continue;
                var tc = blob.ReadSignatureTypeCode();
                if (tc == SignatureTypeCode.GenericTypeInstance)
                    tc = blob.ReadSignatureTypeCode();
                if (tc != SignatureTypeCode.TypeHandle)
                    continue;
                ret = blob.ReadTypeHandle();
            }
            catch (BadImageFormatException)
            {
                continue;
            }
            switch (ret.Kind)
            {
                case HandleKind.TypeDefinition:
                    return (module, (TypeDefinitionHandle)ret);
                case HandleKind.TypeReference:
                    return _resolveType(module, (TypeReferenceHandle)ret);
            }
        }
        return null;
    }

    /// <summary>Whether a TypeDef's base is System.ValueType (a struct), from raw metadata
    /// before the primary scans base types.</summary>
    private static bool IsValueTypeDef(MetadataReader reader, TypeDefinition td) => td.BaseType switch
    {
        { IsNil: false, Kind: HandleKind.TypeDefinition } b =>
            TypeFullName(reader, (TypeDefinitionHandle)b) == "System.ValueType",
        { IsNil: false, Kind: HandleKind.TypeReference } b =>
            TypeRefFullName(reader, (TypeReferenceHandle)b) == "System.ValueType",
        _ => false,
    };

    private static bool HasMethodNamed(MetadataReader reader, TypeDefinition td, string name)
    {
        foreach (var mdh in td.GetMethods())
            if (reader.GetString(reader.GetMethodDefinition(mdh).Name) == name)
                return true;
        return false;
    }

    /// <summary>A TypeDef's arity-stripped full name (<c>GDTask`1</c> -> "GodotTask.GDTask"),
    /// the key the intrinsic maps use.</summary>
    private static string DefFullName(MetadataReader reader, TypeDefinition td)
    {
        string ns = reader.GetString(td.Namespace);
        string nm = reader.GetString(td.Name);
        int tick = nm.IndexOf('`');
        if (tick >= 0) nm = nm[..tick];
        return string.IsNullOrEmpty(ns) ? nm : ns + "." + nm;
    }

}
