using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ILOpCode = System.Reflection.Metadata.ILOpCode;

namespace Dn2Cpp;

internal enum ArraySearchValueKind
{
    RuntimeType,
    TypeArrayElement,
    GenericOwner,
    ArrayElement,
    ArrayRuntimeType,
    ArrayRuntimeElement,
    BoxedValue,
    ObjectType,
}

internal enum ArraySearchFlowKind
{
    Identity,
    ElementType,
    ArrayTypeElement,
    GenericArguments,
    FieldType,
    PropertyType,
    MethodReturnType,
    RuntimeTypeToArrayElement,
    ArrayTypeToArrayElement,
    BoxedToArrayElement,
    ObjectTypeToRuntimeType,
    ArrayTypeFromElement,
    ArrayTypeCancellation,
    FieldTypeToBoxedValue,
    RuntimeTypeToBoxedValue,
    ReferenceSlotBoxValue,
    TypeArrayElementToRuntimeType,
    GenericArgumentAt,
    TypeArrayStoredAt,
    BoxedArrayElement,
    ArrayCloneElements,
    ArrayCopiedElements,
}

internal sealed class ArraySearchOrigin
{
    private static readonly IReadOnlyDictionary<ArraySearchValueKind,
        Dictionary<string, TypeDesc>> EmptyTypeValues =
        new Dictionary<ArraySearchValueKind, Dictionary<string, TypeDesc>>();
    private List<(ArraySearchOrigin Source, ArraySearchFlowKind Kind, string? Name)>? _inputs;
    private Dictionary<ArraySearchValueKind, Dictionary<string, TypeDesc>>? _seeds;
    private Dictionary<ArraySearchValueKind, Dictionary<string, TypeDesc>>? _values;
    internal IReadOnlyList<(ArraySearchOrigin Source, ArraySearchFlowKind Kind, string? Name)> Inputs =>
        _inputs is { } inputs ? inputs
            : Array.Empty<(ArraySearchOrigin, ArraySearchFlowKind, string?)>();
    internal List<(ArraySearchOrigin Source, ArraySearchFlowKind Kind, string? Name)> MutableInputs =>
        _inputs ??= new();
    internal IReadOnlyDictionary<ArraySearchValueKind, Dictionary<string, TypeDesc>> Seeds =>
        _seeds ?? EmptyTypeValues;
    internal Dictionary<ArraySearchValueKind, Dictionary<string, TypeDesc>> MutableSeeds =>
        _seeds ??= new();
    internal void ShareSeedsFrom(ArraySearchOrigin source) => _seeds = source._seeds;
    internal IReadOnlyDictionary<ArraySearchValueKind, Dictionary<string, TypeDesc>> Values =>
        _values ?? EmptyTypeValues;
    internal Dictionary<ArraySearchValueKind, Dictionary<string, TypeDesc>> MutableValues =>
        _values ??= new();
    internal void ClearValues() => _values?.Clear();
    internal Dictionary<int, Dictionary<string, TypeDesc>>? IndexedTypeValues;
    internal bool Unknown;
    internal MethodInfo? ParameterMethod;
    internal int ParameterIndex;
    internal ArraySearchCall? Call;
    internal ArraySearchOrigin?[]? CallActuals;
    internal ArraySearchFrame? CallFrame;
    internal HashSet<MethodInfo>? ProcessedCallTargets;
    internal int ByRefCallParameter = -1;
    internal ArraySearchOrigin? ByRefCallFallback;
    internal (FieldInfo Field, ArraySearchOrigin Receiver, MethodInfo Owner,
        int Offset, bool StraightLine)? FieldRead;
    internal ArraySearchFrame? FieldReadFrame;
    internal (ArraySearchOrigin Array, int? Index, MethodInfo Owner,
        int Offset, bool StraightLine)? ElementRead;
    internal ArraySearchFrame? ElementReadFrame;
    internal (ArraySearchOrigin Handle, ArraySearchOrigin? Receiver, MethodInfo Owner,
        int Offset, bool StraightLine)? ReflectedFieldRead;
    internal ArraySearchFrame? ReflectedFieldReadFrame;
    internal ArraySearchOrigin? LookupParameterTypes;
    internal ArraySearchOrigin? RuntimeBoxHandle;
    internal int LookupOffset = -1;
    internal ArraySearchFrame? LookupFrame;
    internal (FieldInfo Field, MethodInfo Owner, int Offset, bool StraightLine)? StaticFieldRead;
    internal ArraySearchFrame? StaticFieldReadFrame;
    internal bool ArrayAllocation;
    internal int? ArrayAllocationOffset;
    internal MethodInfo? ArrayAllocationMethod;
    internal bool ObjectAllocation;
    internal bool FieldAddress;
    internal int? TypeArrayLength;
    internal ArraySearchFrame? AllocationFrame;
    internal List<(int Index, ArraySearchOrigin Value, int Offset,
        ArraySearchFrame Frame)>? TypeArrayStoreEvents;
    internal Dictionary<int, TypeDesc>? TypeArrayItems;
    internal Dictionary<int, int>? TypeArrayLastStores;
    internal HashSet<int>? TypeArrayAmbiguousSlots;
    internal bool TypeArrayUnknownIndexStore;
}

internal sealed record ArraySearchCall(MethodInfo Owner, MethodInfo Method,
    ArraySearchOrigin?[] Arguments, bool Virtual, int Offset, bool StraightLine);
internal sealed record ArraySearchStore(MethodInfo Method, ArraySearchOrigin Array,
    ArraySearchOrigin Value, bool TypeArray, int? Index, int Offset, bool StraightLine);
internal sealed record ArraySearchClonedElementStore(ArraySearchOrigin Array,
    ArraySearchOrigin Value, bool TypeArray, int? Index, int Offset,
    bool StraightLine, ArraySearchFrame Frame);
internal sealed record ArraySearchWriteEffect(ArraySearchOrigin Target,
    ArraySearchOrigin Value, ArraySearchFlowKind Flow, int Offset, bool StraightLine);
internal sealed record ArraySearchFrame(MethodInfo Method, ArraySearchOrigin?[] Arguments,
    int Depth, ArraySearchFrame? Parent = null, int ParentCallOffset = -1,
    bool ParentCallStraightLine = false);
internal sealed record ArraySearchClonedFieldStore(FieldInfo Field, ArraySearchOrigin Receiver,
    ArraySearchOrigin Value, int Offset, bool StraightLine, ArraySearchFrame Frame);
internal sealed record ArraySearchClonedStaticFieldStore(FieldInfo Field, ArraySearchOrigin Value,
    int Offset, bool StraightLine, ArraySearchFrame Frame);
internal sealed record ArraySearchClonedReflectedFieldStore(ArraySearchOrigin Handle,
    ArraySearchOrigin? Receiver, ArraySearchOrigin Value, int Offset, bool StraightLine,
    ArraySearchFrame Frame);
internal sealed record ArraySearchFieldValueCandidate(ArraySearchOrigin Value,
    ArraySearchOrigin? Receiver, int Offset, bool StraightLine, ArraySearchFrame Frame);

internal sealed partial class Compilation
{
    private readonly ArraySearchOrigin _inactiveArraySearchOrigin = new();
    internal ArraySearchOrigin InactiveArraySearchOrigin => _inactiveArraySearchOrigin;
    private readonly List<(MethodInfo Owner, ArraySearchOrigin Origin)> _arraySearchOperands = new();
    private readonly Dictionary<MethodInfo, List<ArraySearchOrigin>> _arraySearchCalls = new();
    private readonly Dictionary<MethodInfo, List<ArraySearchStore>> _arraySearchStores = new();
    private readonly Dictionary<MethodInfo, List<ArraySearchWriteEffect>> _arraySearchWriteEffects = new();
    private readonly Dictionary<MethodInfo, ArraySearchOrigin> _arraySearchReturns = new();
    private readonly Dictionary<(MethodInfo Method, int Index), ArraySearchOrigin> _arraySearchParameters = new();
    private readonly HashSet<(ArraySearchOrigin Target, ArraySearchOrigin Source, ArraySearchFlowKind Kind, string? Name)> _arraySearchEdges = new();
    private readonly Dictionary<MethodInfo, int?> _arraySearchAddressTakenSlots = new();
    private readonly Dictionary<(MethodInfo Method, int Parameter), int?> _arraySearchByRefWriteOffsets = new();
    private readonly Dictionary<(MethodInfo Method, int Parameter), int?> _arraySearchStobjValueParameters = new();
    private readonly Dictionary<MethodInfo, Dictionary<int, (int Offset, int? DirectCall)>>
        _arraySearchDirectLocalAddressCalls = new();
    private readonly Dictionary<MethodInfo, Dictionary<int, int>>
        _arraySearchPointerBelowValueCalls = new();
    private readonly Dictionary<MethodInfo, int[]> _arraySearchEscapeOffsets = new();
    private readonly HashSet<(MethodInfo Method, int Offset)> _arraySearchArrayEscapeStores = new();
    private readonly Dictionary<MethodInfo, int[]> _arraySearchCallBarrierOffsets = new();
    private readonly Dictionary<MethodInfo, bool> _arraySearchForwardOnlyMethods = new();
    private readonly Dictionary<(MethodInfo Owner, int Offset, MethodInfo Target), int> _arraySearchProvenByRefCalls = new();
    private readonly Dictionary<(string Element, int Rank), TypeDesc> _arraySearchConstructedArrayTypes = new();
    private readonly HashSet<string> _arraySearchUnboundedArrayTypes = new(StringComparer.Ordinal);
    private int _arraySearchArrayGrowthEdges;
    private int _arraySearchArrayDepthLimit;
    private HashSet<MethodInfo>? _arraySearchExpandedCallOwners;
    private Dictionary<MethodInfo, List<ArraySearchFrame>>? _arraySearchPureFrames;
    private Dictionary<MethodInfo, bool>? _arraySearchPureReturnCache;
    private int _arraySearchGraphEpoch;
    private int _arraySearchSelectedEpoch = -1;
    private int _arraySearchSelectedClassCount;
    private int _arraySearchSelectedAllocatedCount;
    private int _arraySearchSelectedShapeCount;
    private int _arraySearchSelectedMemberCount;
    private int _arraySearchSelectedMethodInstanceCount;
    private IReadOnlyList<TypeDesc>? _arraySearchSelectedTypes;
    private IReadOnlyList<TypeDesc> _runtimeHandleBoxSelectedTypes = Array.Empty<TypeDesc>();
    private bool _runtimeHandleBoxUsed;
    private HashSet<ArraySearchOrigin>? _arraySearchRelevantOrigins;
    private bool TrackArraySearchOrigins => Phase == EmitPhase.Emission;

    internal void MarkArraySearchDirty() => _arraySearchGraphEpoch++;

    internal ArraySearchOrigin NewArraySearchOrigin()
    {
        if (!TrackArraySearchOrigins)
            return _inactiveArraySearchOrigin;
        return new ArraySearchOrigin();
    }

    internal ArraySearchOrigin SeedArraySearchOrigin(ArraySearchValueKind kind, TypeDesc? type)
    {
        var origin = NewArraySearchOrigin();
        if (!TrackArraySearchOrigins)
            return origin;
        if (type is not null && !ContainsCanonPlaceholder(type) && !ContainsGenericVar(type))
            origin.MutableSeeds[kind] = new Dictionary<string, TypeDesc>(StringComparer.Ordinal)
            {
                [IdentityMangle(type)] = type,
            };
        else
            origin.Unknown = true;
        MarkArraySearchDirty();
        return origin;
    }

    internal void NoteArraySearchTypeArrayStore(MethodInfo method, ArraySearchOrigin? origin,
        int? index, TypeDesc? type, int offset, bool straightLine)
    {
        if (!TrackArraySearchOrigins || origin is null)
            return;
        MarkArraySearchDirty();
        var visited = new HashSet<ArraySearchOrigin>();
        void Visit(ArraySearchOrigin current)
        {
            if (!visited.Add(current))
                return;
            if (current.TypeArrayLength is { } length)
            {
                if (index is not { } slot || slot < 0 || slot >= length)
                    current.TypeArrayUnknownIndexStore = true;
                else
                {
                    var items = current.TypeArrayItems ??= new Dictionary<int, TypeDesc>();
                    var lastStores = current.TypeArrayLastStores ??= new Dictionary<int, int>();
                    bool replacesPrior = straightLine && ArraySearchFreshArray(origin) is { } allocation
                        && ReferenceEquals(allocation, current)
                        && lastStores.TryGetValue(slot, out int priorOffset)
                        && ArraySearchNoEscapeBetween(method,
                            allocation.ArrayAllocationOffset ?? priorOffset, offset);
                    if (replacesPrior)
                    {
                        current.TypeArrayAmbiguousSlots?.Remove(slot);
                        if (type is null)
                        {
                            items.Remove(slot);
                            (current.TypeArrayAmbiguousSlots ??= new HashSet<int>()).Add(slot);
                        }
                        else
                            items[slot] = type;
                    }
                    else if (type is null || items.TryGetValue(slot, out var previous)
                        && IdentityMangle(previous) != IdentityMangle(type))
                        (current.TypeArrayAmbiguousSlots ??= new HashSet<int>()).Add(slot);
                    else
                        items[slot] = type;
                    lastStores[slot] = offset;
                }
            }
            foreach (var input in current.Inputs)
                if (input.Kind == ArraySearchFlowKind.Identity)
                    Visit(input.Source);
        }
        Visit(origin);
    }

    internal IReadOnlyList<string>? KnownArraySearchTypeArray(ArraySearchOrigin? origin)
    {
        if (!TrackArraySearchOrigins || origin is null)
            return null;
        var allocations = new List<ArraySearchOrigin>();
        var visited = new HashSet<ArraySearchOrigin>();
        void Visit(ArraySearchOrigin current)
        {
            if (!visited.Add(current))
                return;
            if (current.TypeArrayLength is not null)
                allocations.Add(current);
            foreach (var input in current.Inputs)
                if (input.Kind == ArraySearchFlowKind.Identity)
                    Visit(input.Source);
        }
        Visit(origin);
        if (allocations.Count != 1 || allocations[0].TypeArrayUnknownIndexStore
            || allocations[0].TypeArrayAmbiguousSlots is { Count: > 0 }
            || allocations[0].TypeArrayLength is not { } length
            || (allocations[0].TypeArrayItems?.Count ?? 0) != length)
            return null;
        var result = new List<string>();
        for (int i = 0; i < length; i++)
        {
            if (!allocations[0].TypeArrayItems!.TryGetValue(i, out var type))
                return null;
            result.Add(IdentityMangle(type));
        }
        return result;
    }

    internal void AddArraySearchSeed(ArraySearchOrigin origin, ArraySearchValueKind kind, TypeDesc? type)
    {
        if (!TrackArraySearchOrigins || type is null || ContainsCanonPlaceholder(type)
            || ContainsGenericVar(type))
            return;
        if (!origin.Seeds.TryGetValue(kind, out var seeds))
            origin.MutableSeeds.Add(kind, seeds = new Dictionary<string, TypeDesc>(StringComparer.Ordinal));
        if (seeds.TryAdd(IdentityMangle(type), type))
            MarkArraySearchDirty();
    }

    internal ArraySearchOrigin ArraySearchCallOrigin(MethodInfo owner, MethodInfo method,
        ArraySearchOrigin?[] arguments, bool virtualCall, int offset, bool straightLine)
    {
        var origin = NewArraySearchOrigin();
        if (TrackArraySearchOrigins)
        {
            origin.Call = new ArraySearchCall(owner, method, arguments, virtualCall,
                offset, straightLine);
            if (!_arraySearchCalls.TryGetValue(owner, out var calls))
                _arraySearchCalls.Add(owner, calls = new List<ArraySearchOrigin>());
            calls.Add(origin);
            MarkArraySearchDirty();
        }
        return origin;
    }

    internal ArraySearchOrigin ArraySearchByRefCallOrigin(ArraySearchOrigin callOrigin,
        MethodInfo owner, MethodInfo method, ArraySearchOrigin previous,
        int local, int parameter, int offset)
    {
        var target = method.Emittable;
        if (!TrackArraySearchOrigins || callOrigin.Call is not { } call
            || !ArraySearchCanVersionByRefCall(owner, target, call.Arguments,
                local, parameter, offset))
            return previous;
        callOrigin.ByRefCallParameter = parameter;
        callOrigin.ByRefCallFallback = previous;
        _arraySearchProvenByRefCalls[(owner, offset, target)] = parameter;
        return callOrigin;
    }

    internal ArraySearchOrigin ArraySearchByRefCallOrigin(MethodInfo owner,
        MethodInfo method, ArraySearchOrigin?[] arguments, ArraySearchOrigin previous,
        int local, int parameter, int offset)
    {
        var target = method.Emittable;
        if (!TrackArraySearchOrigins
            || !ArraySearchCanVersionByRefCall(owner, target, arguments,
                local, parameter, offset))
            return previous;
        var callOrigin = ArraySearchCallOrigin(owner, method, arguments, false, offset, true);
        callOrigin.ByRefCallParameter = parameter;
        callOrigin.ByRefCallFallback = previous;
        _arraySearchProvenByRefCalls[(owner, offset, target)] = parameter;
        return callOrigin;
    }

    private bool ArraySearchCanVersionByRefCall(MethodInfo owner, MethodInfo method,
        ArraySearchOrigin?[] arguments, int local, int parameter, int offset)
    {
        if (ArraySearchDefiniteByRefWriteOffset(method, parameter) is not null)
            return ArraySearchSingleLocalAddress(owner, local, offset);
        if (ArraySearchStobjValueParameter(method, parameter) is not { } valueParameter
            || valueParameter >= arguments.Length || arguments[valueParameter] is not { } value
            || ArraySearchFreshArray(value) is not { ArrayAllocationOffset: { } allocationOffset } allocation
            || !ReferenceEquals(value, allocation)
            || !ReferenceEquals(allocation.ArrayAllocationMethod, owner)
            || allocationOffset >= offset
            || !allocation.Seeds.ContainsKey(ArraySearchValueKind.ArrayElement)
            || !ArraySearchNoEscapeBetween(owner, allocationOffset, offset))
            return false;
        return ArraySearchPointerBelowValueAtCall(owner, local, offset);
    }

    private bool ArraySearchSingleLocalAddress(MethodInfo method, int local, int callOffset)
    {
        if (!_arraySearchDirectLocalAddressCalls.TryGetValue(method, out var calls))
        {
            calls = new Dictionary<int, (int Offset, int? DirectCall)>();
            if (method.Rva != 0)
            {
                var instructions = ILDecoder.Decode(method.Module.PE
                    .GetMethodBody(method.Rva).GetILBytes()!.ToImmutableArrayCompat());
                for (int i = 0; i < instructions.Count; i++)
                {
                    var instruction = instructions[i];
                    if (instruction.OpCode is not (ILOpCode.Ldloca or ILOpCode.Ldloca_s))
                        continue;
                    int slot = (int)instruction.Operand;
                    int? nextCall = i + 1 < instructions.Count
                        && instructions[i + 1].OpCode == ILOpCode.Call
                        ? instructions[i + 1].Offset : null;
                    if (!calls.TryAdd(slot, (instruction.Offset, nextCall)))
                        calls[slot] = (-1, null);
                }
            }
            _arraySearchDirectLocalAddressCalls.Add(method, calls);
        }
        return calls.TryGetValue(local, out var address) && address.DirectCall == callOffset;
    }

    private bool ArraySearchPointerBelowValueAtCall(MethodInfo method, int local, int callOffset)
    {
        _ = ArraySearchSingleLocalAddress(method, local, callOffset);
        if (!_arraySearchPointerBelowValueCalls.TryGetValue(method, out var calls))
        {
            calls = new Dictionary<int, int>();
            int activeLocal = -1;
            int depth = 0;
            foreach (var instruction in ILDecoder.Decode(method.Module.PE
                .GetMethodBody(method.Rva).GetILBytes()!.ToImmutableArrayCompat()))
            {
                if (activeLocal < 0)
                {
                    if (instruction.OpCode is ILOpCode.Ldloca or ILOpCode.Ldloca_s
                        && _arraySearchDirectLocalAddressCalls[method]
                            .TryGetValue((int)instruction.Operand, out var address)
                        && address.Offset == instruction.Offset)
                    {
                        activeLocal = (int)instruction.Operand;
                        depth = 1;
                    }
                    continue;
                }
                bool valid = true;
                switch (instruction.OpCode)
                {
                    case ILOpCode.Call:
                        if (depth == 2)
                            calls.Add(instruction.Offset, activeLocal);
                        valid = false;
                        break;
                    case ILOpCode.Ldc_i4_m1: case ILOpCode.Ldc_i4_0:
                    case ILOpCode.Ldc_i4_1: case ILOpCode.Ldc_i4_2:
                    case ILOpCode.Ldc_i4_3: case ILOpCode.Ldc_i4_4:
                    case ILOpCode.Ldc_i4_5: case ILOpCode.Ldc_i4_6:
                    case ILOpCode.Ldc_i4_7: case ILOpCode.Ldc_i4_8:
                    case ILOpCode.Ldc_i4_s: case ILOpCode.Ldc_i4:
                    case ILOpCode.Ldnull: case ILOpCode.Ldstr:
                    case ILOpCode.Ldloc_0: case ILOpCode.Ldloc_1:
                    case ILOpCode.Ldloc_2: case ILOpCode.Ldloc_3:
                    case ILOpCode.Ldloc_s: case ILOpCode.Ldloc:
                    case ILOpCode.Ldloca_s: case ILOpCode.Ldloca:
                        depth++;
                        break;
                    case ILOpCode.Newarr:
                        valid = depth >= 2;
                        break;
                    case ILOpCode.Dup:
                        valid = depth >= 2;
                        depth++;
                        break;
                    case ILOpCode.Stelem_ref:
                        valid = depth >= 4;
                        depth -= 3;
                        break;
                    case ILOpCode.Initobj:
                    case ILOpCode.Stloc_0: case ILOpCode.Stloc_1:
                    case ILOpCode.Stloc_2: case ILOpCode.Stloc_3:
                    case ILOpCode.Stloc_s: case ILOpCode.Stloc:
                        valid = depth >= 2;
                        depth--;
                        break;
                    case ILOpCode.Box: case ILOpCode.Castclass: case ILOpCode.Isinst:
                        valid = depth >= 2;
                        break;
                    default:
                        valid = false;
                        break;
                }
                if (!valid)
                    activeLocal = -1;
            }
            _arraySearchPointerBelowValueCalls.Add(method, calls);
        }
        return calls.TryGetValue(callOffset, out int callLocal) && callLocal == local;
    }

    private int? ArraySearchStobjValueParameter(MethodInfo method, int parameter)
    {
        if (_arraySearchStobjValueParameters.TryGetValue((method, parameter), out var cached))
            return cached;
        int? result = null;
        if (method.IsStatic && parameter == 0 && method.Rva != 0
            && method.Signature.ParameterTypes.Length == 2
            && method.Signature.ParameterTypes[0].Kind == TypeKind.ByRef
            && method.Signature.ParameterTypes[1].Kind != TypeKind.ByRef
            && method.Module.PE.GetMethodBody(method.Rva).ExceptionRegions.Length == 0)
        {
            var instructions = ILDecoder.Decode(method.Module.PE.GetMethodBody(method.Rva)
                .GetILBytes()!.ToImmutableArrayCompat())
                .Where(instruction => instruction.OpCode != ILOpCode.Nop).ToArray();
            if (instructions.Length == 4 && ArraySearchLoadsArgument(instructions[0], 0)
                && ArraySearchLoadsArgument(instructions[1], 1)
                && instructions[2].OpCode == ILOpCode.Stobj
                && instructions[3].OpCode == ILOpCode.Ret)
                result = 1;
        }
        _arraySearchStobjValueParameters.Add((method, parameter), result);
        return result;
    }

    private ArraySearchOrigin? ArraySearchDefiniteByRefWrite(MethodInfo method, int parameter)
    {
        int? offset = ArraySearchDefiniteByRefWriteOffset(method, parameter);
        int? valueParameter = ArraySearchStobjValueParameter(method, parameter);
        if (offset is null && valueParameter is null
            || !_arraySearchWriteEffects.TryGetValue(method, out var effects)
            || effects.Count != 1
            || effects[0] is not { Flow: ArraySearchFlowKind.Identity,
                Target: { ParameterMethod: { } owner } }
            || !ReferenceEquals(owner, method)
            || effects[0].Target.ParameterIndex != parameter)
            return null;
        if (valueParameter is { } valueIndex)
            return effects[0].Value.ParameterMethod is { } valueOwner
                && ReferenceEquals(valueOwner, method)
                && effects[0].Value.ParameterIndex == valueIndex
                ? effects[0].Value : null;
        if (offset is not { } storeOffset || effects[0].Offset != storeOffset
            || ArraySearchFreshArray(effects[0].Value) is not { } allocation
            || !allocation.Seeds.ContainsKey(ArraySearchValueKind.ArrayElement))
            return null;
        return effects[0].Value;
    }

    private int? ArraySearchDefiniteByRefWriteOffset(MethodInfo method, int parameter)
    {
        if (_arraySearchByRefWriteOffsets.TryGetValue((method, parameter), out var cached))
            return cached;
        int? result = ArraySearchDefiniteByRefWriteOffsetCore(method, parameter);
        _arraySearchByRefWriteOffsets.Add((method, parameter), result);
        return result;
    }

    private static bool ArraySearchLoadsArgument(Instruction instruction, int parameter) =>
        instruction.OpCode switch
        {
            ILOpCode.Ldarg_0 => parameter == 0,
            ILOpCode.Ldarg_1 => parameter == 1,
            ILOpCode.Ldarg_2 => parameter == 2,
            ILOpCode.Ldarg_3 => parameter == 3,
            ILOpCode.Ldarg_s or ILOpCode.Ldarg => (int)instruction.Operand == parameter,
            _ => false,
        };

    private int? ArraySearchDefiniteByRefWriteOffsetCore(MethodInfo method, int parameter)
    {
        if (!method.IsStatic || parameter != method.Signature.ParameterTypes.Length - 1
            || method.Signature.ParameterTypes[parameter].Kind != TypeKind.ByRef
            || method.Rva == 0
            || method.Module.PE.GetMethodBody(method.Rva).ExceptionRegions.Length != 0)
            return null;
        var instructions = ILDecoder.Decode(method.Module.PE.GetMethodBody(method.Rva)
            .GetILBytes()!.ToImmutableArrayCompat())
            .Where(instruction => instruction.OpCode != ILOpCode.Nop)
            .ToArray();
        if (instructions.Length < 4 || !ArraySearchLoadsArgument(instructions[0], parameter)
            || instructions[^1].OpCode != ILOpCode.Ret)
            return null;
        int store = Array.FindIndex(instructions,
            instruction => instruction.OpCode == ILOpCode.Stind_ref);
        if (store < 2 || store >= instructions.Length - 1)
            return null;
        int depth = 1;
        for (int i = 1; i < store; i++)
        {
            var instruction = instructions[i];
            switch (instruction.OpCode)
            {
                case ILOpCode.Ldc_i4_m1: case ILOpCode.Ldc_i4_0:
                case ILOpCode.Ldc_i4_1: case ILOpCode.Ldc_i4_2:
                case ILOpCode.Ldc_i4_3: case ILOpCode.Ldc_i4_4:
                case ILOpCode.Ldc_i4_5: case ILOpCode.Ldc_i4_6:
                case ILOpCode.Ldc_i4_7: case ILOpCode.Ldc_i4_8:
                case ILOpCode.Ldc_i4_s: case ILOpCode.Ldc_i4:
                case ILOpCode.Ldnull: case ILOpCode.Ldstr:
                case ILOpCode.Ldloc_0: case ILOpCode.Ldloc_1:
                case ILOpCode.Ldloc_2: case ILOpCode.Ldloc_3:
                case ILOpCode.Ldloc_s: case ILOpCode.Ldloc:
                case ILOpCode.Ldloca_s: case ILOpCode.Ldloca:
                    depth++;
                    break;
                case ILOpCode.Newarr:
                    if (depth < 2)
                        return null;
                    break;
                case ILOpCode.Dup:
                    if (depth < 2)
                        return null;
                    depth++;
                    break;
                case ILOpCode.Newobj:
                {
                    var ctor = System.Reflection.Metadata.Ecma335.MetadataTokens
                        .EntityHandle(instruction.Token);
                    int parameters = ctor.Kind switch
                    {
                        HandleKind.MethodDefinition => method.Module.Reader
                            .GetMethodDefinition((MethodDefinitionHandle)ctor)
                            .DecodeSignature(SigProvider, method.Context).ParameterTypes.Length,
                        HandleKind.MemberReference => method.Module.Reader
                            .GetMemberReference((MemberReferenceHandle)ctor)
                            .DecodeMethodSignature(SigProvider, method.Context).ParameterTypes.Length,
                        _ => -1,
                    };
                    if (parameters < 0 || depth <= parameters)
                        return null;
                    depth += 1 - parameters;
                    break;
                }
                case ILOpCode.Stelem_ref:
                    if (depth < 4)
                        return null;
                    depth -= 3;
                    break;
                case ILOpCode.Initobj:
                case ILOpCode.Stloc_0: case ILOpCode.Stloc_1:
                case ILOpCode.Stloc_2: case ILOpCode.Stloc_3:
                case ILOpCode.Stloc_s: case ILOpCode.Stloc:
                    if (depth < 2)
                        return null;
                    depth--;
                    break;
                case ILOpCode.Box: case ILOpCode.Castclass: case ILOpCode.Isinst:
                    if (depth < 2)
                        return null;
                    break;
                default:
                    return null;
            }
        }
        if (depth != 2)
            return null;
        depth = 0;
        for (int i = store + 1; i < instructions.Length - 1; i++)
            switch (instructions[i].OpCode)
            {
                case ILOpCode.Ldc_i4_m1: case ILOpCode.Ldc_i4_0:
                case ILOpCode.Ldc_i4_1: case ILOpCode.Ldc_i4_2:
                case ILOpCode.Ldc_i4_3: case ILOpCode.Ldc_i4_4:
                case ILOpCode.Ldc_i4_5: case ILOpCode.Ldc_i4_6:
                case ILOpCode.Ldc_i4_7: case ILOpCode.Ldc_i4_8:
                case ILOpCode.Ldc_i4_s: case ILOpCode.Ldc_i4:
                case ILOpCode.Ldnull: case ILOpCode.Ldstr:
                case ILOpCode.Ldloc_0: case ILOpCode.Ldloc_1:
                case ILOpCode.Ldloc_2: case ILOpCode.Ldloc_3:
                case ILOpCode.Ldloc_s: case ILOpCode.Ldloc:
                case ILOpCode.Ldarg_0: case ILOpCode.Ldarg_1:
                case ILOpCode.Ldarg_2: case ILOpCode.Ldarg_3:
                case ILOpCode.Ldarg_s: case ILOpCode.Ldarg:
                    depth++;
                    break;
                default:
                    return null;
            }
        return depth == (method.Signature.ReturnType.IsVoid ? 0 : 1)
            ? instructions[store].Offset : null;
    }

    internal void NoteArraySearchStore(MethodInfo method, ArraySearchOrigin? array,
        ArraySearchOrigin? value, bool typeArray, int? index, int offset, bool straightLine,
        bool arrayValue = false)
    {
        if (TrackArraySearchOrigins
            && (arrayValue || value is not null && ArraySearchFreshArray(value) is not null)
            && _arraySearchArrayEscapeStores.Add((method, offset)))
            _arraySearchEscapeOffsets.Remove(method);
        if (TrackArraySearchOrigins && array is not null && value is not null)
        {
            if (!_arraySearchStores.TryGetValue(method, out var stores))
                _arraySearchStores.Add(method, stores = new List<ArraySearchStore>());
            if (straightLine && index is not null && ArraySearchFreshArray(array) is { } allocation)
                stores.RemoveAll(previous => previous.StraightLine && previous.Index == index
                    && previous.TypeArray == typeArray && previous.Offset < offset
                    && ReferenceEquals(ArraySearchFreshArray(previous.Array), allocation)
                    && ArraySearchNoEscapeBetween(method,
                        allocation.ArrayAllocationOffset ?? previous.Offset, offset));
            stores.Add(new ArraySearchStore(method, array, value, typeArray, index,
                offset, straightLine));
            MarkArraySearchDirty();
        }
    }

    internal ArraySearchOrigin ArraySearchRuntimeBoxOrigin(MethodInfo owner, ArraySearchOrigin? handle,
        ArraySearchOrigin? slot)
    {
        var origin = TransformArraySearchOrigin(handle, ArraySearchFlowKind.RuntimeTypeToBoxedValue);
        if (TrackArraySearchOrigins)
        {
            _runtimeHandleBoxUsed = true;
            origin.RuntimeBoxHandle = handle;
            LinkArraySearchOrigin(origin, slot, ArraySearchFlowKind.ReferenceSlotBoxValue);
            NoteArraySearchOperand(owner, origin);
        }
        return origin;
    }

    internal ArraySearchOrigin ArraySearchElementOrigin(MethodInfo method,
        ArraySearchOrigin? array, int? index, int offset, bool straightLine,
        ArraySearchFlowKind fallback)
    {
        var origin = TransformArraySearchOrigin(array, fallback,
            index?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (TrackArraySearchOrigins && array is not null)
            origin.ElementRead = (array, index, method, offset, straightLine);
        return origin;
    }

    internal ArraySearchOrigin? ArraySearchStoredElement(MethodInfo method,
        ArraySearchOrigin? array, int? index, int offset, bool straightLine)
    {
        if (!TrackArraySearchOrigins || !straightLine || array is null || index is null
            || !_arraySearchStores.TryGetValue(method, out var stores))
            return null;
        var allocation = ArraySearchFreshArray(array);
        if (allocation is null)
            return null;
        ArraySearchStore? selected = null;
        foreach (var store in stores)
        {
            if (!store.StraightLine || store.Offset >= offset
                || !ReferenceEquals(ArraySearchFreshArray(store.Array), allocation))
                continue;
            if (store.Index is null)
                selected = null;
            else if (store.Index == index)
                selected = store;
        }
        return selected is not null && ArraySearchNoEscapeBetween(method,
            allocation.ArrayAllocationOffset ?? selected.Offset, offset)
            ? selected.Value : null;
    }

    private static ArraySearchOrigin? ArraySearchFreshArray(ArraySearchOrigin origin)
    {
        var seen = new HashSet<ArraySearchOrigin>();
        ArraySearchOrigin? allocation = null;
        bool Visit(ArraySearchOrigin current)
        {
            if (!seen.Add(current))
                return true;
            if (current.Unknown || current.ParameterMethod is not null || current.Call is not null
                || current.FieldRead is not null || current.StaticFieldRead is not null)
                return false;
            if (current.ArrayAllocation)
            {
                if (current.Seeds.Count == 0 || allocation is not null
                    && !ReferenceEquals(allocation, current))
                    return false;
                allocation = current;
            }
            return current.Inputs.All(input => input.Kind is ArraySearchFlowKind.TypeArrayStoredAt
                    or ArraySearchFlowKind.BoxedToArrayElement
                || current.ArrayAllocation && current.Seeds.ContainsKey(ArraySearchValueKind.ArrayElement)
                    && input.Kind == ArraySearchFlowKind.RuntimeTypeToArrayElement
                || input.Kind == ArraySearchFlowKind.Identity && Visit(input.Source));
        }
        return Visit(origin) ? allocation : null;
    }

    private bool ArraySearchNoEscapeBetween(MethodInfo method, int from, int to)
    {
        if (method.Rva == 0)
            return false;
        if (!_arraySearchEscapeOffsets.TryGetValue(method, out var offsets))
        {
            var instructions = ILDecoder.Decode(method.Module.PE
                .GetMethodBody(method.Rva).GetILBytes()!.ToImmutableArrayCompat());
            var unsafeOffsets = new List<int>();
            for (int i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];
                if (instruction.OpCode == ILOpCode.Initobj && i > 0
                    && (instructions[i - 1].OpCode is ILOpCode.Ldloca or ILOpCode.Ldloca_s))
                    continue;
                if ((instruction.OpCode is System.Reflection.Metadata.ILOpCode.Call
                        or System.Reflection.Metadata.ILOpCode.Callvirt)
                    && !ArraySearchPureNonEscapingCall(method, instruction.Token))
                    unsafeOffsets.Add(instruction.Offset);
                if (_arraySearchArrayEscapeStores.Contains((method, instruction.Offset)))
                    unsafeOffsets.Add(instruction.Offset);
                if (instruction.OpCode is System.Reflection.Metadata.ILOpCode.Calli
                    or System.Reflection.Metadata.ILOpCode.Newobj
                    or System.Reflection.Metadata.ILOpCode.Stfld
                    or System.Reflection.Metadata.ILOpCode.Stsfld
                    or System.Reflection.Metadata.ILOpCode.Stind_ref
                    or System.Reflection.Metadata.ILOpCode.Stobj
                    or System.Reflection.Metadata.ILOpCode.Cpobj
                    or System.Reflection.Metadata.ILOpCode.Initobj
                    or System.Reflection.Metadata.ILOpCode.Cpblk
                    or System.Reflection.Metadata.ILOpCode.Initblk)
                    unsafeOffsets.Add(instruction.Offset);
            }
            offsets = unsafeOffsets.ToArray();
            _arraySearchEscapeOffsets.Add(method, offsets);
        }
        int next = Array.BinarySearch(offsets, from + 1);
        if (next < 0)
            next = ~next;
        return next == offsets.Length || offsets[next] >= to;
    }

    private bool ArraySearchPureNonEscapingCall(MethodInfo owner, int token)
    {
        var handle = System.Reflection.Metadata.Ecma335.MetadataTokens.EntityHandle(token);
        if (handle.Kind == HandleKind.MethodDefinition)
        {
            var method = owner.Module.Reader.GetMethodDefinition((MethodDefinitionHandle)handle);
            return (method.Attributes & System.Reflection.MethodAttributes.Static) != 0
                && method.DecodeSignature(SigProvider, owner.Context).ParameterTypes.Length == 0;
        }
        if (handle.Kind == HandleKind.MemberReference)
        {
            var member = owner.Module.Reader.GetMemberReference((MemberReferenceHandle)handle);
            var signature = member.DecodeMethodSignature(SigProvider, owner.Context);
            return !signature.Header.IsInstance && signature.ParameterTypes.Length == 0;
        }
        return false;
    }

    internal ArraySearchOrigin TransformArraySearchOrigin(ArraySearchOrigin? source,
        ArraySearchFlowKind kind, string? name = null)
    {
        var result = NewArraySearchOrigin();
        LinkArraySearchOrigin(result, source, kind, name);
        return result;
    }

    internal void LinkArraySearchOrigin(ArraySearchOrigin target, ArraySearchOrigin? source,
        ArraySearchFlowKind kind = ArraySearchFlowKind.Identity, string? name = null)
    {
        if (!TrackArraySearchOrigins || ReferenceEquals(target, _inactiveArraySearchOrigin)
            || ReferenceEquals(source, _inactiveArraySearchOrigin))
            return;
        if (source is null)
        {
            if (!target.Unknown)
            {
                target.Unknown = true;
                if (_arraySearchRelevantOrigins is null
                    || _arraySearchRelevantOrigins.Contains(target))
                    MarkArraySearchDirty();
            }
            return;
        }
        if (_arraySearchEdges.Add((target, source, kind, name)))
        {
            target.MutableInputs.Add((source, kind, name));
            if (_arraySearchRelevantOrigins is null
                || _arraySearchRelevantOrigins.Contains(target))
                MarkArraySearchDirty();
        }
    }

    internal void NoteArraySearchWriteEffect(MethodInfo method, ArraySearchOrigin? target,
        ArraySearchOrigin? value, ArraySearchFlowKind flow, int offset = -1,
        bool straightLine = false)
    {
        if (!TrackArraySearchOrigins || target is null || value is null)
            return;
        if (!_arraySearchWriteEffects.TryGetValue(method, out var effects))
            _arraySearchWriteEffects.Add(method, effects = new List<ArraySearchWriteEffect>());
        effects.Add(new ArraySearchWriteEffect(target, value, flow, offset, straightLine));
        MarkArraySearchDirty();
    }

    private bool ArraySearchHasAddressTakenSlotBefore(MethodInfo method, int offset)
    {
        if (method.Rva == 0)
            return true;
        if (!_arraySearchAddressTakenSlots.TryGetValue(method, out int? first))
        {
            foreach (var instruction in ILDecoder.Decode(method.Module.PE
                .GetMethodBody(method.Rva).GetILBytes()!.ToImmutableArrayCompat()))
                if (instruction.OpCode is ILOpCode.Ldloca or ILOpCode.Ldloca_s
                    or ILOpCode.Ldarga or ILOpCode.Ldarga_s)
                {
                    first = instruction.Offset;
                    break;
                }
            _arraySearchAddressTakenSlots.Add(method, first);
        }
        return first is { } addressOffset && addressOffset < offset;
    }

    private ArraySearchOrigin? ArraySearchStableFreshArray(ArraySearchOrigin? origin,
        MethodInfo method, int offset, bool straightLine)
    {
        if (straightLine && origin is not null
            && ArraySearchFreshArray(origin) is { ArrayAllocationOffset: { } allocationOffset } allocation
            && ReferenceEquals(allocation.ArrayAllocationMethod, method)
            && allocationOffset < offset && !ArraySearchHasAddressTakenSlotBefore(method, offset)
            && ArraySearchNoEscapeBetween(method, allocationOffset, offset))
            return allocation;
        return null;
    }

    internal int? ArraySearchStableFreshLength(ArraySearchOrigin? origin,
        MethodInfo method, int offset, bool straightLine)
    {
        if (straightLine && origin is not null
            && ArraySearchFreshArray(origin) is { ArrayAllocationOffset: { } allocationOffset } allocation
            && ReferenceEquals(allocation.ArrayAllocationMethod, method)
            && allocationOffset < offset && !ArraySearchHasAddressTakenSlotBefore(method, offset))
            return allocation.TypeArrayLength;
        return null;
    }

    private TypeDesc? ArraySearchExactCopyElement(ArraySearchOrigin? origin,
        TypeDesc? staticType, MethodInfo method, int offset, bool straightLine)
    {
        if (ArraySearchStableFreshArray(origin, method, offset, straightLine) is { } allocation
            && allocation.Seeds.TryGetValue(ArraySearchValueKind.ArrayElement, out var elements)
            && elements.Count == 1)
        {
            var freshElement = elements.Values.First();
            return freshElement;
        }
        if (staticType is not { Kind: TypeKind.SZArray or TypeKind.MDArray,
            Element: { } element })
            return null;
        // A covariant reference array may hold a narrower runtime element.
        if (ArraySearchValueElement(element)
            || element.IsString
            || element is { Kind: TypeKind.Class, Class: { IsSealed: true } })
            return element;
        return null;
    }

    private static bool ArraySearchValueElement(TypeDesc element) =>
        element.Kind == TypeKind.Primitive && element.Primitive is not
            (System.Reflection.Metadata.PrimitiveTypeCode.Object
                or System.Reflection.Metadata.PrimitiveTypeCode.String
                or System.Reflection.Metadata.PrimitiveTypeCode.Void)
        || element is { Kind: TypeKind.Class, Class: { IsValueType: true } };

    private static bool ArraySearchReferenceElement(TypeDesc element) =>
        element.IsObject || element.IsString
        || element is { Kind: TypeKind.External,
            ExternalName: "System.ValueType" or "System.Enum" }
        || element.Kind is TypeKind.SZArray or TypeKind.MDArray
        || element is { Kind: TypeKind.Class, Class: { IsValueType: false } };

    private static bool ArraySearchNonPrimitiveStruct(TypeDesc element) =>
        element is { Kind: TypeKind.Class,
            Class: { IsValueType: true, IsEnum: false } type }
        && type.FullName is not ("System.IntPtr" or "System.UIntPtr");

    private static bool ArraySearchCopyCannotStore(TypeDesc source, TypeDesc destination,
        bool reliable)
    {
        if (IdentityMangle(source) == IdentityMangle(destination))
            return false;
        bool sourceValue = ArraySearchValueElement(source);
        bool destinationValue = ArraySearchValueElement(destination);
        if (destinationValue && ArraySearchNonPrimitiveStruct(source)
            || sourceValue && ArraySearchNonPrimitiveStruct(destination))
            return true;
        if (reliable)
        {
            if (sourceValue && ArraySearchReferenceElement(destination)
                || destinationValue && ArraySearchReferenceElement(source))
                return true;
            // A reliable reference copy cannot cast elements down to a sealed type.
            if (destination.IsString
                || destination is { Kind: TypeKind.Class,
                    Class: { IsValueType: false, IsSealed: true } })
                return ArraySearchReferenceElement(source);
        }
        if (sourceValue && (destination.IsString
            || destination.Kind is TypeKind.SZArray or TypeKind.MDArray
            || destination is { Kind: TypeKind.Class,
                Class: { IsValueType: false, IsInterface: false } target }
                && target.FullName is not ("System.Object" or "System.ValueType" or "System.Enum")))
            return true;
        if (sourceValue && destination is { Kind: TypeKind.Class,
                Class: { FullName: "System.Enum" } }
            && source is not { Kind: TypeKind.Class, Class: { IsEnum: true } })
            return true;
        return false;
    }

    private static int? ArraySearchStaticRank(TypeDesc? type) => type?.Kind switch
    {
        TypeKind.SZArray => 1,
        TypeKind.MDArray => type.Rank,
        _ => null,
    };

    internal void NoteArraySearchCopiedElements(MethodInfo method, ArraySearchOrigin? source,
        ArraySearchOrigin? destination, TypeDesc? sourceStaticType,
        TypeDesc? destinationStaticType, bool reliable = false,
        bool zeroElements = false, int offset = -1, bool straightLine = false)
    {
        if (!TrackArraySearchOrigins || source is null || destination is null || zeroElements
            || ArraySearchStaticRank(sourceStaticType) is { } sourceRank
                && ArraySearchStaticRank(destinationStaticType) is { } destinationRank
                && sourceRank != destinationRank)
            return;
        TypeDesc? sourceElement = ArraySearchExactCopyElement(source, sourceStaticType,
            method, offset, straightLine);
        TypeDesc? destinationElement = ArraySearchExactCopyElement(destination,
            destinationStaticType, method, offset, straightLine);
        if (reliable && (sourceElement is not null && ArraySearchValueElement(sourceElement)
                && destinationStaticType is { Kind: TypeKind.SZArray or TypeKind.MDArray,
                    Element: { } declaredDestination }
                && ArraySearchReferenceElement(declaredDestination)
            || destinationElement is not null && ArraySearchValueElement(destinationElement)
                && sourceStaticType is { Kind: TypeKind.SZArray or TypeKind.MDArray,
                    Element: { } declaredSource }
                && ArraySearchReferenceElement(declaredSource)))
            return;
        if (sourceElement is not null && destinationElement is not null
            && ArraySearchCopyCannotStore(sourceElement, destinationElement, reliable))
            return;
        NoteArraySearchWriteEffect(method, destination, source,
            ArraySearchFlowKind.ArrayCopiedElements);
    }

    private TypeDesc ArraySearchConstructedArrayType(TypeDesc element, int rank)
    {
        // A repeated construction edge adds reference wrappers above this finite prefix.
        if (ArraySearchArrayDepth(element) >= _arraySearchArrayDepthLimit)
        {
            _arraySearchUnboundedArrayTypes.Add(IdentityMangle(element));
            return element;
        }
        var key = (IdentityMangle(element), rank);
        if (!_arraySearchConstructedArrayTypes.TryGetValue(key, out var arrayType))
        {
            arrayType = rank == 1 ? TypeDesc.MakeSZArray(element) : TypeDesc.MakeMDArray(element, rank);
            _arraySearchConstructedArrayTypes.Add(key, arrayType);
        }
        if (_arraySearchUnboundedArrayTypes.Contains(key.Item1))
            _arraySearchUnboundedArrayTypes.Add(IdentityMangle(arrayType));
        return arrayType;
    }

    private static int ArraySearchArrayDepth(TypeDesc type)
    {
        int depth = 0;
        while (type is { Kind: TypeKind.SZArray or TypeKind.MDArray, Element: { } element })
        {
            depth++;
            type = element;
        }
        return depth;
    }

    private static int ArraySearchKnownArrayDepth(TypeDesc type)
    {
        int depth = ArraySearchArrayDepth(type);
        while (type is { Kind: TypeKind.SZArray or TypeKind.MDArray, Element: { } element })
            type = element;
        if (type is { Kind: TypeKind.Class, Class: { } cls })
            foreach (var argument in cls.Context.TypeArgs)
                depth = Math.Max(depth, ArraySearchKnownArrayDepth(argument));
        return depth;
    }

    private TypeDesc NoteArraySearchDeclaredArrayDepth(TypeDesc type)
    {
        _arraySearchArrayDepthLimit = Math.Max(_arraySearchArrayDepthLimit,
            ArraySearchKnownArrayDepth(type) + _arraySearchArrayGrowthEdges);
        return type;
    }

    internal ArraySearchOrigin ArraySearchReturn(MethodInfo method)
    {
        if (!TrackArraySearchOrigins)
            return _inactiveArraySearchOrigin;
        if (!_arraySearchReturns.TryGetValue(method, out var origin))
            _arraySearchReturns.Add(method, origin = NewArraySearchOrigin());
        return origin;
    }

    internal ArraySearchOrigin ArraySearchParameter(MethodInfo method, int index)
    {
        if (!TrackArraySearchOrigins)
            return _inactiveArraySearchOrigin;
        if (!_arraySearchParameters.TryGetValue((method, index), out var origin))
        {
            origin = NewArraySearchOrigin();
            origin.ParameterMethod = method;
            origin.ParameterIndex = index;
            _arraySearchParameters.Add((method, index), origin);
        }
        return origin;
    }

    internal void NoteArraySearchOperand(MethodInfo owner, ArraySearchOrigin? origin)
    {
        if (!TrackArraySearchOrigins)
            return;
        if (origin is not null && !_arraySearchOperands.Any(x => ReferenceEquals(x.Owner, owner)
            && ReferenceEquals(x.Origin, origin)))
        {
            _arraySearchOperands.Add((owner, origin));
            MarkArraySearchDirty();
        }
    }

    internal ArraySearchOrigin ArraySearchReflectedFieldValue(ArraySearchOrigin? handle,
        ArraySearchOrigin? receiver, MethodInfo owner, int offset, bool straightLine)
    {
        var origin = TransformArraySearchOrigin(handle, ArraySearchFlowKind.FieldTypeToBoxedValue);
        if (TrackArraySearchOrigins && handle is not null)
            origin.ReflectedFieldRead = (handle, receiver, owner, offset, straightLine);
        return origin;
    }

    private static bool ArraySearchSubclassOf(ClassInfo c, ClassInfo baseClass)
    {
        for (ClassInfo? current = c; current is not null; current = current.BaseClass)
            if (ReferenceEquals(current, baseClass))
                return true;
        return false;
    }

    internal static string ArraySearchSelector(string? name, int? flags,
        bool zeroParameters = false, IReadOnlyList<string>? parameterTypes = null,
        int? genericCount = null) =>
        (name ?? "\u0001") + "\0" + (flags?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "?")
            + "\0" + (zeroParameters ? "1" : "0")
            + "\0" + (parameterTypes is null ? "?" : string.Join('\u0002', parameterTypes))
            + "\0" + (genericCount?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "?");

    private static bool ArraySearchMemberMatches(string memberName, string? name, int? flags,
        bool isPublic, bool isStatic, bool isPrivate, bool inherited)
    {
        if (name is not null && !string.Equals(memberName, name,
                flags is { } f && (f & 1) != 0
                    ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            return false;
        if (inherited && (isPrivate || flags is { } d && (d & 2) != 0))
            return false;
        if (inherited && isStatic && flags is { } staticFlags && (staticFlags & 64) == 0)
            return false;
        if (flags is not { } mask)
            return true;
        return (isPublic ? (mask & 16) != 0 : (mask & 32) != 0)
            && (isStatic ? (mask & 8) != 0 : (mask & 4) != 0);
    }

    private IEnumerable<TypeDesc> ArraySearchMemberTypes(TypeDesc owner, string? selector,
        ArraySearchFlowKind kind, IReadOnlyList<HashSet<string>>? dynamicParameters = null,
        IReadOnlyList<string[]>? signatureTuples = null)
    {
        if (owner is not { Kind: TypeKind.Class, Class: { } cls })
            yield break;
        string[] parts = selector?.Split('\0') ?? Array.Empty<string>();
        string? name = parts.Length > 0 && parts[0] != "\u0001" ? parts[0] : null;
        int? flags = parts.Length > 1 && int.TryParse(parts[1], out int parsedFlags)
            ? parsedFlags : null;
        bool zeroParameters = parts.Length > 2 && parts[2] == "1";
        string[]? parameterTypes = parts.Length > 3 && parts[3] != "?"
            ? parts[3].Length == 0 ? Array.Empty<string>() : parts[3].Split('\u0002') : null;
        int? genericCount = parts.Length > 4 && int.TryParse(parts[4], out int parsedCount)
            ? parsedCount : null;
        var selectedRows = new HashSet<string>(StringComparer.Ordinal);
        for (ClassInfo? current = cls; current is not null; current = current.BaseClass)
        {
            if (kind == ArraySearchFlowKind.FieldType)
            {
                foreach (var field in current.Fields)
                    if (ArraySearchMemberMatches(field.Name, name, flags,
                        field.IsPublic, field.IsStatic, field.IsPrivate, !ReferenceEquals(current, cls))
                        && selectedRows.Add(field.Name))
                        yield return NoteArraySearchDeclaredArrayDepth(field.Type);
                continue;
            }
            if (kind == ArraySearchFlowKind.PropertyType)
            {
                var reader = current.Module.Reader;
                var type = reader.GetTypeDefinition(current.Handle);
                foreach (var handle in type.GetProperties())
                {
                    var property = reader.GetPropertyDefinition(handle);
                    var accessors = property.GetAccessors();
                    if (accessors.Getter.IsNil && accessors.Setter.IsNil)
                        continue;
                    var getterAttrs = accessors.Getter.IsNil ? (System.Reflection.MethodAttributes)0
                        : reader.GetMethodDefinition(accessors.Getter).Attributes;
                    var setterAttrs = accessors.Setter.IsNil ? (System.Reflection.MethodAttributes)0
                        : reader.GetMethodDefinition(accessors.Setter).Attributes;
                    bool getterPublic = !accessors.Getter.IsNil && (getterAttrs
                        & System.Reflection.MethodAttributes.MemberAccessMask)
                        == System.Reflection.MethodAttributes.Public;
                    bool setterPublic = !accessors.Setter.IsNil && (setterAttrs
                        & System.Reflection.MethodAttributes.MemberAccessMask)
                        == System.Reflection.MethodAttributes.Public;
                    bool getterPrivate = accessors.Getter.IsNil || (getterAttrs
                        & System.Reflection.MethodAttributes.MemberAccessMask)
                        == System.Reflection.MethodAttributes.Private;
                    bool setterPrivate = accessors.Setter.IsNil || (setterAttrs
                        & System.Reflection.MethodAttributes.MemberAccessMask)
                        == System.Reflection.MethodAttributes.Private;
                    var propertySignature = property.DecodeSignature(SigProvider, current.Context);
                    if (ArraySearchMemberMatches(reader.GetString(property.Name), name, flags,
                        getterPublic || setterPublic,
                        ((getterAttrs | setterAttrs) & System.Reflection.MethodAttributes.Static) != 0,
                        getterPrivate && setterPrivate,
                        !ReferenceEquals(current, cls))
                        && (parameterTypes is null || propertySignature.ParameterTypes.Length == parameterTypes.Length
                            && propertySignature.ParameterTypes.Select(IdentityMangle)
                                .SequenceEqual(parameterTypes, StringComparer.Ordinal))
                        && selectedRows.Add(reader.GetString(property.Name) + "\0" + string.Join('\0',
                            propertySignature.ParameterTypes.Select(IdentityMangle))))
                        yield return NoteArraySearchDeclaredArrayDepth(propertySignature.ReturnType);
                }
                continue;
            }
            foreach (var method in current.Methods)
                if (ArraySearchMemberMatches(method.Name, name, flags,
                        method.IsPublic, method.IsStatic, method.IsPrivate, !ReferenceEquals(current, cls))
                    && (!zeroParameters || method.Signature.ParameterTypes.Length == 0)
                    && (genericCount is null || method.Signature.GenericParameterCount == genericCount)
                    && (parameterTypes is null || method.Signature.ParameterTypes.Length == parameterTypes.Length
                        && method.Signature.ParameterTypes.Select(IdentityMangle)
                            .SequenceEqual(parameterTypes, StringComparer.Ordinal))
                    && (signatureTuples is not null
                        ? signatureTuples.Any(tuple => method.Signature.ParameterTypes.Length == tuple.Length
                            && method.Signature.ParameterTypes.Select(IdentityMangle)
                                .SequenceEqual(tuple, StringComparer.Ordinal))
                        : dynamicParameters is null
                        || method.Signature.ParameterTypes.Length == dynamicParameters.Count
                            && method.Signature.ParameterTypes.Select(IdentityMangle)
                                .Where((identity, index) => dynamicParameters[index].Contains(identity))
                                .Count() == dynamicParameters.Count)
                    && selectedRows.Add(method.Name + "\0"
                        + method.Signature.GenericParameterCount.ToString(
                            System.Globalization.CultureInfo.InvariantCulture) + "\0" + string.Join('\0',
                        method.Signature.ParameterTypes.Select(IdentityMangle))))
                    yield return NoteArraySearchDeclaredArrayDepth(method.Signature.ReturnType);
        }
    }

    private static IReadOnlyList<HashSet<string>>? ArraySearchSignatureValues(ArraySearchOrigin origin)
    {
        if (ArraySearchFreshArray(origin) is not { TypeArrayLength: { } length })
            return null;
        var result = new List<HashSet<string>>(length);
        for (int index = 0; index < length; index++)
        {
            if (origin.IndexedTypeValues is null
                || !origin.IndexedTypeValues.TryGetValue(index, out var values)
                || values.Count == 0)
                return null;
            result.Add(new HashSet<string>(values.Keys, StringComparer.Ordinal));
        }
        return result;
    }

    private IReadOnlyList<string[]>? ArraySearchSignatureTuples(ArraySearchOrigin origin,
        ArraySearchFrame? frame, int lookupOffset)
    {
        if (frame is null || lookupOffset < 0
            || ArraySearchFreshArray(origin) is not { TypeArrayLength: { } length,
                ArrayAllocationOffset: { } allocationOffset } allocation
            || length > 8 || !ReferenceEquals(allocation.AllocationFrame, frame)
            || frame.Method.Rva == 0)
            return null;
        var body = frame.Method.Module.PE.GetMethodBody(frame.Method.Rva);
        if (body.ExceptionRegions.Length != 0)
            return null;
        var instructions = ILDecoder.Decode(body.GetILBytes()!.ToImmutableArrayCompat());
        var byOffset = instructions.ToDictionary(instruction => instruction.Offset);
        if (!byOffset.TryGetValue(allocationOffset, out var allocationInstruction)
            || !byOffset.ContainsKey(lookupOffset))
            return null;
        if (allocation.TypeArrayStoreEvents is not { } events)
            return null;
        var stores = events
            .Where(store => ReferenceEquals(store.Frame, frame)
                && store.Offset > allocationOffset && store.Offset < lookupOffset).ToList();
        if (stores.Count == 0 || stores.Any(store => store.Index < 0 || store.Index >= length)
            || stores.GroupBy(store => store.Offset).Any(group => group.Count() != 1))
            return null;
        var byStoreOffset = stores.ToDictionary(store => store.Offset);
        var pending = new Queue<(int Offset, string?[] Types)>();
        pending.Enqueue((allocationInstruction.NextOffset, new string?[length]));
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var tuples = new Dictionary<string, string[]>(StringComparer.Ordinal);
        while (pending.Count > 0)
        {
            var (offset, types) = pending.Dequeue();
            if (offset < allocationOffset || !byOffset.TryGetValue(offset, out var instruction))
                return null;
            string stateKey = offset.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "\0" + string.Join('\0', types);
            if (!visited.Add(stateKey))
                continue;
            if (visited.Count > 512)
                return null;
            if (offset == lookupOffset)
            {
                if (types.Any(type => type is null))
                    return null;
                var tuple = types.Select(type => type!).ToArray();
                tuples.TryAdd(string.Join('\0', tuple), tuple);
                continue;
            }
            if (offset > lookupOffset)
                return null;
            if (instruction.OpCode == System.Reflection.Metadata.ILOpCode.Stelem_ref)
            {
                if (!byStoreOffset.TryGetValue(offset, out var store)
                    || !store.Value.Values.TryGetValue(ArraySearchValueKind.RuntimeType, out var values)
                    || values.Count != 1)
                    return null;
                types = (string?[])types.Clone();
                types[store.Index] = values.Keys.First();
            }
            else if (instruction.OpCode == System.Reflection.Metadata.ILOpCode.Call)
            {
                if (!ArraySearchPureTypeHandleCall(frame.Method.Module.Reader, instruction.Token))
                    return null;
            }
            else if (instruction.OpCode is System.Reflection.Metadata.ILOpCode.Callvirt
                or System.Reflection.Metadata.ILOpCode.Calli
                or System.Reflection.Metadata.ILOpCode.Newobj
                or System.Reflection.Metadata.ILOpCode.Stobj
                or System.Reflection.Metadata.ILOpCode.Initobj
                or System.Reflection.Metadata.ILOpCode.Cpobj
                or System.Reflection.Metadata.ILOpCode.Cpblk
                or System.Reflection.Metadata.ILOpCode.Initblk
                or System.Reflection.Metadata.ILOpCode.Ldelema
                || instruction.OpCode.ToString().StartsWith("Stind", StringComparison.Ordinal))
                return null;
            if (instruction.SwitchTargets is { } switchTargets)
            {
                foreach (int target in switchTargets)
                    pending.Enqueue((target, types));
                pending.Enqueue((instruction.NextOffset, types));
            }
            else if (ILDecoder.IsBranch(instruction.OpCode))
            {
                pending.Enqueue(((int)instruction.Operand, types));
                if (!ILDecoder.IsUnconditionalTransfer(instruction.OpCode))
                    pending.Enqueue((instruction.NextOffset, types));
            }
            else if (instruction.OpCode is not (System.Reflection.Metadata.ILOpCode.Ret
                or System.Reflection.Metadata.ILOpCode.Throw
                or System.Reflection.Metadata.ILOpCode.Rethrow))
                pending.Enqueue((instruction.NextOffset, types));
        }
        return tuples.Count == 0 ? null : tuples.Values.ToList();
    }

    private IEnumerable<FieldInfo> ArraySearchMemberFields(TypeDesc owner, string? selector)
    {
        if (owner is not { Kind: TypeKind.Class, Class: { } cls })
            yield break;
        string[] parts = selector?.Split('\0') ?? Array.Empty<string>();
        string? name = parts.Length > 0 && parts[0] != "\u0001" ? parts[0] : null;
        int? flags = parts.Length > 1 && int.TryParse(parts[1], out int parsedFlags)
            ? parsedFlags : null;
        var selectedNames = new HashSet<string>(StringComparer.Ordinal);
        for (ClassInfo? current = cls; current is not null; current = current.BaseClass)
            foreach (var field in current.Fields)
                if (ArraySearchMemberMatches(field.Name, name, flags,
                    field.IsPublic, field.IsStatic, field.IsPrivate, !ReferenceEquals(current, cls))
                    && selectedNames.Add(field.Name))
                    yield return field;
    }

    private IEnumerable<FieldInfo> ArraySearchReflectedFields(ArraySearchOrigin handle)
    {
        var visited = new HashSet<ArraySearchOrigin>();
        var pending = new Stack<ArraySearchOrigin>();
        pending.Push(handle);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!visited.Add(current))
                continue;
            foreach (var (source, kind, selector) in current.Inputs)
            {
                if (kind == ArraySearchFlowKind.Identity)
                    pending.Push(source);
                else if (kind == ArraySearchFlowKind.FieldType
                    && source.Values.TryGetValue(ArraySearchValueKind.RuntimeType, out var owners))
                    foreach (var owner in owners.Values)
                        foreach (var field in ArraySearchMemberFields(owner, selector))
                            yield return field;
            }
        }
    }

    private IEnumerable<(ArraySearchValueKind Kind, TypeDesc Type)> ArraySearchTransform(
        ArraySearchFlowKind flow, ArraySearchValueKind kind, TypeDesc type, string? name)
    {
        switch (flow)
        {
            case ArraySearchFlowKind.Identity:
                yield return (kind, type);
                break;
            case ArraySearchFlowKind.ElementType when kind == ArraySearchValueKind.ArrayRuntimeElement:
                yield return (ArraySearchValueKind.RuntimeType, type);
                break;
            case ArraySearchFlowKind.ArrayTypeToArrayElement when kind == ArraySearchValueKind.ArrayRuntimeElement:
                yield return (ArraySearchValueKind.ArrayElement, type);
                yield return (ArraySearchValueKind.ArrayRuntimeElement, type);
                break;
            case ArraySearchFlowKind.ElementType when kind == ArraySearchValueKind.RuntimeType:
            case ArraySearchFlowKind.ArrayTypeElement when kind == ArraySearchValueKind.RuntimeType:
                if (type is { Kind: TypeKind.SZArray or TypeKind.MDArray, Element: { } element })
                {
                    // An unbounded prefix can still have wrappers after an element read.
                    if (_arraySearchUnboundedArrayTypes.Contains(IdentityMangle(type)))
                        yield return (ArraySearchValueKind.RuntimeType, type);
                    yield return (ArraySearchValueKind.RuntimeType, element);
                }
                break;
            case ArraySearchFlowKind.GenericArguments when kind == ArraySearchValueKind.RuntimeType:
                yield return (ArraySearchValueKind.GenericOwner, type);
                break;
            case ArraySearchFlowKind.GenericArgumentAt when kind == ArraySearchValueKind.GenericOwner:
                if (type is { Kind: TypeKind.Class, Class: { } generic })
                {
                    int? index = int.TryParse(name, out int parsed) ? parsed : null;
                    for (int i = 0; i < generic.Context.TypeArgs.Length; i++)
                        if (index is null || index == i)
                            yield return (ArraySearchValueKind.RuntimeType, generic.Context.TypeArgs[i]);
                }
                break;
            case ArraySearchFlowKind.FieldType when kind == ArraySearchValueKind.RuntimeType:
            case ArraySearchFlowKind.PropertyType when kind == ArraySearchValueKind.RuntimeType:
            case ArraySearchFlowKind.MethodReturnType when kind == ArraySearchValueKind.RuntimeType:
                foreach (var memberType in ArraySearchMemberTypes(type, name, flow))
                    yield return (ArraySearchValueKind.RuntimeType, memberType);
                break;
            case ArraySearchFlowKind.RuntimeTypeToArrayElement when kind == ArraySearchValueKind.RuntimeType:
                yield return (ArraySearchValueKind.ArrayElement, type);
                if (int.TryParse(name, out int rank) && rank > 0)
                    yield return (ArraySearchValueKind.ArrayRuntimeType,
                        ArraySearchConstructedArrayType(type, rank));
                else
                    yield return (ArraySearchValueKind.ArrayRuntimeElement, type);
                break;
            case ArraySearchFlowKind.ArrayTypeToArrayElement when kind == ArraySearchValueKind.RuntimeType:
                if (type is { Kind: TypeKind.SZArray or TypeKind.MDArray, Element: { } arrayElement })
                {
                    if (_arraySearchUnboundedArrayTypes.Contains(IdentityMangle(type)))
                        yield return (ArraySearchValueKind.ArrayElement, type);
                    yield return (ArraySearchValueKind.ArrayElement, arrayElement);
                    yield return (ArraySearchValueKind.ArrayRuntimeType, type);
                }
                break;
            case ArraySearchFlowKind.BoxedToArrayElement when kind == ArraySearchValueKind.BoxedValue:
            case ArraySearchFlowKind.BoxedToArrayElement when kind == ArraySearchValueKind.ObjectType:
            case ArraySearchFlowKind.BoxedToArrayElement when kind == ArraySearchValueKind.ArrayRuntimeType:
                yield return (ArraySearchValueKind.ArrayElement, type);
                break;
            case ArraySearchFlowKind.ObjectTypeToRuntimeType when kind == ArraySearchValueKind.ObjectType:
                yield return (ArraySearchValueKind.RuntimeType, type);
                break;
            case ArraySearchFlowKind.ObjectTypeToRuntimeType when kind == ArraySearchValueKind.ArrayRuntimeType:
                yield return (ArraySearchValueKind.RuntimeType, type);
                break;
            case ArraySearchFlowKind.ObjectTypeToRuntimeType when kind == ArraySearchValueKind.ArrayRuntimeElement:
                yield return (ArraySearchValueKind.ArrayRuntimeElement, type);
                break;
            case ArraySearchFlowKind.ArrayTypeFromElement when kind == ArraySearchValueKind.RuntimeType:
                yield return (ArraySearchValueKind.RuntimeType,
                    ArraySearchConstructedArrayType(type, 1));
                break;
            case ArraySearchFlowKind.ArrayTypeCancellation when kind is
                ArraySearchValueKind.RuntimeType or ArraySearchValueKind.ArrayRuntimeElement:
                yield return (kind, type);
                break;
            case ArraySearchFlowKind.FieldTypeToBoxedValue when kind == ArraySearchValueKind.RuntimeType:
                yield return (ArraySearchValueKind.BoxedValue, type);
                break;
            case ArraySearchFlowKind.RuntimeTypeToBoxedValue when kind == ArraySearchValueKind.RuntimeType:
                var boxedType = NullableUnderlying(type) ?? type;
                yield return (ArraySearchValueKind.BoxedValue, boxedType);
                yield return (ArraySearchValueKind.ObjectType, boxedType);
                break;
            case ArraySearchFlowKind.ReferenceSlotBoxValue when kind is ArraySearchValueKind.BoxedValue
                or ArraySearchValueKind.ObjectType or ArraySearchValueKind.ArrayRuntimeType
                or ArraySearchValueKind.ArrayRuntimeElement:
                yield return (kind, type);
                break;
            case ArraySearchFlowKind.TypeArrayElementToRuntimeType when kind == ArraySearchValueKind.TypeArrayElement:
                yield return (ArraySearchValueKind.RuntimeType, type);
                break;
            case ArraySearchFlowKind.BoxedArrayElement when kind == ArraySearchValueKind.ArrayElement:
                yield return (ArraySearchValueKind.BoxedValue, type);
                yield return (ArraySearchValueKind.ObjectType, type);
                break;
            case ArraySearchFlowKind.ArrayCloneElements when kind == ArraySearchValueKind.ArrayElement:
            case ArraySearchFlowKind.ArrayCopiedElements when kind == ArraySearchValueKind.ArrayElement:
                yield return (ArraySearchValueKind.ArrayElement, type);
                break;
            case ArraySearchFlowKind.ArrayCloneElements when kind is ArraySearchValueKind.ArrayRuntimeType
                or ArraySearchValueKind.ArrayRuntimeElement:
                yield return (kind, type);
                break;
        }
    }

    private static bool ArraySearchReferenceType(TypeDesc type)
    {
        return (type.Kind is TypeKind.Class or TypeKind.External or TypeKind.SZArray or TypeKind.MDArray)
                && CppTypes.KindOf(type) == StackKind.Ref
            || type.Kind == TypeKind.Primitive
                && type.Primitive is PrimitiveTypeCode.String or PrimitiveTypeCode.Object;
    }

    private static bool ArraySearchBoxReturnsReference(ArraySearchOrigin origin)
    {
        return origin.RuntimeBoxHandle is { } handle
            && handle.Values.TryGetValue(ArraySearchValueKind.RuntimeType, out var types)
            && types.Values.Any(ArraySearchReferenceType);
    }

    private void EvaluateArraySearchValues(IEnumerable<ArraySearchOrigin> roots)
    {
        var visited = new HashSet<ArraySearchOrigin>();
        var active = new List<ArraySearchOrigin>();
        var arrayTypeSources = new Dictionary<ArraySearchOrigin, ArraySearchOrigin[]>();
        ArraySearchOrigin[] ArrayTypeSources(ArraySearchOrigin source)
        {
            if (arrayTypeSources.TryGetValue(source, out var cached))
                return cached;
            var found = new HashSet<ArraySearchOrigin>();
            var seen = new HashSet<ArraySearchOrigin>();
            var pending = new Stack<ArraySearchOrigin>();
            pending.Push(source);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (!seen.Add(current))
                    continue;
                foreach (var input in current.Inputs)
                    if (input.Kind == ArraySearchFlowKind.ArrayTypeFromElement)
                        found.Add(input.Source);
                    else if (input.Kind is ArraySearchFlowKind.Identity
                        or ArraySearchFlowKind.ArrayTypeCancellation)
                        pending.Push(input.Source);
            }
            cached = found.ToArray();
            arrayTypeSources.Add(source, cached);
            return cached;
        }
        void Visit(ArraySearchOrigin origin)
        {
            if (!visited.Add(origin))
                return;
            foreach (var input in origin.Inputs)
                Visit(input.Source);
            if (origin.LookupParameterTypes is { } lookupTypes)
                Visit(lookupTypes);
            active.Add(origin);
        }
        foreach (var root in roots)
            Visit(root);
        _arraySearchUnboundedArrayTypes.Clear();
        _arraySearchArrayGrowthEdges = 0;
        int knownArrayDepth = 0;
        foreach (var origin in active)
        {
            foreach (var input in origin.Inputs)
                if (input.Kind is ArraySearchFlowKind.ArrayTypeFromElement
                    or ArraySearchFlowKind.RuntimeTypeToArrayElement)
                    _arraySearchArrayGrowthEdges++;
            foreach (var seeds in origin.Seeds.Values)
                foreach (var type in seeds.Values)
                    knownArrayDepth = Math.Max(knownArrayDepth, ArraySearchKnownArrayDepth(type));
        }
        // A path without a repeated construction edge cannot exceed this extent.
        _arraySearchArrayDepthLimit = knownArrayDepth + Math.Max(1, _arraySearchArrayGrowthEdges);
        var resolvedElementReads = new HashSet<ArraySearchOrigin>();
        var resolvedReferenceElementReads = new HashSet<ArraySearchOrigin>();
        foreach (var origin in active)
            if (origin.ElementRead is { } element && origin.ElementReadFrame is not null)
            {
                var sources = ArraySearchIdentitySources(element.Array);
                // Reference slots remain null until a temporally selected store supplies a value.
                if (!sources.Open && sources.Allocations.Count > 0
                    && sources.Allocations.All(allocation => allocation.ArrayAllocation))
                {
                    resolvedElementReads.Add(origin);
                    if (sources.Allocations.All(allocation =>
                        allocation.Seeds.TryGetValue(ArraySearchValueKind.ArrayElement, out var declared)
                        && declared.Count > 0 && declared.Values.All(ArraySearchReferenceType)))
                        resolvedReferenceElementReads.Add(origin);
                }
            }
        bool addedArrayTypeEdge;
        do
        {
            addedArrayTypeEdge = false;
            arrayTypeSources.Clear();
            foreach (var origin in active)
                for (int i = 0; i < origin.Inputs.Count; i++)
                {
                    var input = origin.Inputs[i];
                    if (input.Kind != ArraySearchFlowKind.ElementType)
                        continue;
                    foreach (var source in ArrayTypeSources(input.Source))
                        addedArrayTypeEdge |= AddArraySearchCloneEdge(origin, source,
                            ArraySearchFlowKind.ArrayTypeCancellation);
                }
        } while (addedArrayTypeEdge);
        foreach (var origin in active)
        {
            origin.ClearValues();
            origin.IndexedTypeValues?.Clear();
            foreach (var (kind, seeds) in origin.Seeds)
                origin.MutableValues[kind] = new Dictionary<string, TypeDesc>(seeds, StringComparer.Ordinal);
        }
        static bool AddValue(Dictionary<string, TypeDesc> target, TypeDesc type)
        {
            string identity = IdentityMangle(type);
            return target.TryAdd(identity, type);
        }
        for (int lookupStage = 0; lookupStage < 2; lookupStage++)
        {
            bool changed;
            do
            {
                changed = false;
                int depthLimitBefore = _arraySearchArrayDepthLimit;
                int unboundedTypesBefore = _arraySearchUnboundedArrayTypes.Count;
            foreach (var origin in active)
            {
                foreach (var input in origin.Inputs)
                {
                    if (input.Kind == ArraySearchFlowKind.ReferenceSlotBoxValue
                        && !ArraySearchBoxReturnsReference(origin))
                        continue;
                    // Reference-slot contents come from the stores selected at this read, not later writes.
                    if (input.Kind == ArraySearchFlowKind.BoxedArrayElement
                        && resolvedReferenceElementReads.Contains(origin))
                        continue;
                    if (input.Kind == ArraySearchFlowKind.Identity
                        && input.Source.IndexedTypeValues is { } sourceIndexed)
                        foreach (var (index, values) in sourceIndexed)
                        {
                            var indexed = origin.IndexedTypeValues ??= new();
                            if (!indexed.TryGetValue(index, out var target))
                                indexed.Add(index,
                                    target = new Dictionary<string, TypeDesc>(StringComparer.Ordinal));
                            foreach (var type in values.Values)
                                changed |= AddValue(target, type);
                        }
                    if (input.Kind == ArraySearchFlowKind.TypeArrayStoredAt
                        && input.Source.Values.TryGetValue(ArraySearchValueKind.RuntimeType, out var storedTypes))
                    {
                        int index = int.TryParse(input.Name, out int parsed) ? parsed : -1;
                        var indexed = origin.IndexedTypeValues ??= new();
                        if (!indexed.TryGetValue(index, out var target))
                            indexed.Add(index,
                                target = new Dictionary<string, TypeDesc>(StringComparer.Ordinal));
                        foreach (var type in storedTypes.Values)
                            changed |= AddValue(target, type);
                    }
                    if (input.Kind == ArraySearchFlowKind.GenericArgumentAt
                        && !resolvedElementReads.Contains(origin)
                        && input.Source.IndexedTypeValues is { } genericIndexed)
                    {
                        int? requested = int.TryParse(input.Name, out int parsed) ? parsed : null;
                        foreach (var (index, values) in genericIndexed)
                            if (requested is null || index < 0 || requested == index)
                            {
                                if (!origin.Values.TryGetValue(ArraySearchValueKind.RuntimeType, out var target))
                                    origin.MutableValues.Add(ArraySearchValueKind.RuntimeType,
                                        target = new Dictionary<string, TypeDesc>(StringComparer.Ordinal));
                                foreach (var type in values.Values)
                                    changed |= AddValue(target, type);
                            }
                    }
                    foreach (var (kind, values) in input.Source.Values)
                        foreach (var type in values.Values)
                        {
                            IEnumerable<(ArraySearchValueKind Kind, TypeDesc Type)> transformed;
                            if (input.Kind == ArraySearchFlowKind.MethodReturnType
                                && kind == ArraySearchValueKind.RuntimeType
                                && origin.LookupParameterTypes is { } lookupTypes)
                            {
                                var signature = ArraySearchSignatureValues(lookupTypes);
                                if (signature is null && lookupStage == 0)
                                    continue;
                                var tuples = signature is null ? null
                                    : ArraySearchSignatureTuples(lookupTypes,
                                        origin.LookupFrame, origin.LookupOffset);
                                transformed = ArraySearchMemberTypes(type, input.Name,
                                    ArraySearchFlowKind.MethodReturnType, signature, tuples)
                                    .Select(memberType => (ArraySearchValueKind.RuntimeType, memberType));
                            }
                            else
                                transformed = ArraySearchTransform(input.Kind, kind, type, input.Name);
                            foreach (var (resultKind, resultType) in transformed)
                            {
                                if (!origin.Values.TryGetValue(resultKind, out var target))
                                    origin.MutableValues.Add(resultKind, target = new Dictionary<string, TypeDesc>(StringComparer.Ordinal));
                                changed |= AddValue(target, resultType);
                            }
                        }
                }
            }
                changed |= depthLimitBefore != _arraySearchArrayDepthLimit
                    || unboundedTypesBefore != _arraySearchUnboundedArrayTypes.Count;
            } while (changed);
        }
    }

    private IEnumerable<MethodInfo> ArraySearchCallTargets(ArraySearchCall call,
        ArraySearchOrigin?[] actuals)
    {
        if (!call.Virtual)
        {
            yield return call.Method.Emittable;
            yield break;
        }
        if (actuals.Length == 0 || actuals[0] is not { } receiver)
            yield break;
        if (!receiver.Values.TryGetValue(ArraySearchValueKind.ObjectType, out var types))
            yield break;
        var methods = new HashSet<MethodInfo>();
        foreach (var type in types.Values.OrderBy(IdentityMangle, StringComparer.Ordinal))
        {
            if (type is not { Kind: TypeKind.Class, Class: { } cls })
                continue;
            MethodInfo? target = null;
            if (call.Method.DeclaringClass.IsInterface)
                target = ResolveItfImplOrNull(cls, call.Method);
            else if (ArraySearchSubclassOf(cls, call.Method.DeclaringClass)
                && call.Method.VtableSlot >= 0 && call.Method.VtableSlot < cls.Vtable.Count)
                target = cls.Vtable[call.Method.VtableSlot];
            if (target is not null && methods.Add(target))
                yield return target;
        }
    }

    private bool ArraySearchPureTypeReturn(MethodInfo method, HashSet<MethodInfo> active)
    {
        if (_arraySearchPureReturnCache!.TryGetValue(method, out bool cached))
            return cached;
        var returned = method.Signature.ReturnType;
        if (!method.IsStatic || method.Rva == 0
            || ContainsCanonPlaceholder(method.DeclaringClass)
            || method.Context.MethodArgs.Any(ContainsCanonPlaceholder)
            || !(returned.Kind == TypeKind.External && returned.ExternalName == "System.Type"
                || returned.Kind == TypeKind.Class && returned.Class?.FullName == "System.Type")
            || !active.Add(method)
            || _arraySearchStores.ContainsKey(method)
            || _arraySearchWriteEffects.ContainsKey(method)
            || _arraySearchInstanceFieldStores.ContainsKey(method)
            || _arraySearchStaticFieldStores.ContainsKey(method)
            || _arraySearchReflectedFieldStores.ContainsKey(method))
            return false;
        bool pure = true;
        foreach (var instruction in ILDecoder.Decode(method.Module.PE
            .GetMethodBody(method.Rva).GetILBytes()!.ToImmutableArrayCompat()))
        {
            string op = instruction.OpCode.ToString();
            if (instruction.OpCode == System.Reflection.Metadata.ILOpCode.Call)
            {
                if (ArraySearchPureTypeHandleCall(method.Module.Reader, instruction.Token))
                    continue;
                if (!_arraySearchCalls.TryGetValue(method, out var calls)
                    || !calls.Any(origin => origin.Call is { } call
                        && call.Offset == instruction.Offset
                        && ArraySearchPureTypeReturn(call.Method, active)))
                {
                    pure = false;
                    break;
                }
            }
            else if (instruction.OpCode is System.Reflection.Metadata.ILOpCode.Callvirt
                or System.Reflection.Metadata.ILOpCode.Calli
                or System.Reflection.Metadata.ILOpCode.Newobj
                or System.Reflection.Metadata.ILOpCode.Newarr
                or System.Reflection.Metadata.ILOpCode.Box
                or System.Reflection.Metadata.ILOpCode.Unbox
                or System.Reflection.Metadata.ILOpCode.Unbox_any
                || op.StartsWith("Ldfld", StringComparison.Ordinal)
                || op.StartsWith("Ldsfld", StringComparison.Ordinal)
                || op.StartsWith("Stfld", StringComparison.Ordinal)
                || op.StartsWith("Stsfld", StringComparison.Ordinal)
                || op.StartsWith("Ldelem", StringComparison.Ordinal)
                || op.StartsWith("Stelem", StringComparison.Ordinal)
                || op.StartsWith("Ldind", StringComparison.Ordinal)
                || op.StartsWith("Stind", StringComparison.Ordinal)
                || op is "Ldarga" or "Ldarga_s" or "Ldloca" or "Ldloca_s"
                    or "Ldobj" or "Stobj" or "Initobj" or "Cpobj"
                    or "Cpblk" or "Initblk" or "Localloc")
            {
                pure = false;
                break;
            }
        }
        active.Remove(method);
        _arraySearchPureReturnCache.Add(method, pure);
        return pure;
    }

    private static bool ArraySearchPureTypeHandleCall(MetadataReader reader, int token)
    {
        var handle = System.Reflection.Metadata.Ecma335.MetadataTokens.EntityHandle(token);
        if (handle.Kind != HandleKind.MemberReference)
            return false;
        var member = reader.GetMemberReference((MemberReferenceHandle)handle);
        if (reader.GetString(member.Name) != "GetTypeFromHandle"
            || member.Parent.Kind != HandleKind.TypeReference)
            return false;
        var type = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
        return reader.GetString(type.Namespace) == "System"
            && reader.GetString(type.Name) == "Type";
    }

    private static string? ArraySearchImmutableTypeLiteral(ArraySearchOrigin? origin)
    {
        if (origin is null || origin.Unknown || origin.Inputs.Count != 0
            || origin.Call is not null || origin.ParameterMethod is not null
            || origin.FieldRead is not null || origin.ReflectedFieldRead is not null
            || origin.StaticFieldRead is not null || origin.LookupParameterTypes is not null
            || origin.Seeds.Count != 1
            || !origin.Seeds.TryGetValue(ArraySearchValueKind.RuntimeType, out var types)
            || types.Count != 1)
            return null;
        return types.Keys.First();
    }

    private static bool ArraySearchSameActuals(ArraySearchOrigin?[] left,
        ArraySearchOrigin?[] right)
    {
        if (left.Length != right.Length)
            return false;
        for (int index = 0; index < left.Length; index++)
            if (!ReferenceEquals(left[index], right[index]))
            {
                string? leftType = ArraySearchImmutableTypeLiteral(left[index]);
                if (leftType is null || leftType != ArraySearchImmutableTypeLiteral(right[index]))
                    return false;
            }
        return true;
    }

    private static bool AddArraySearchCloneEdge(ArraySearchOrigin target, ArraySearchOrigin source,
        ArraySearchFlowKind kind = ArraySearchFlowKind.Identity, string? name = null)
    {
        foreach (var edge in target.Inputs)
            if (ReferenceEquals(edge.Source, source) && edge.Kind == kind && edge.Name == name)
                return false;
        target.MutableInputs.Add((source, kind, name));
        return true;
    }

    private bool LinkArraySearchCallTarget(ArraySearchOrigin copy, MethodInfo target,
        ArraySearchOrigin?[] actuals, ArraySearchFrame frame,
        Dictionary<(ArraySearchOrigin Origin, ArraySearchFrame Frame), ArraySearchOrigin> cache,
        HashSet<ArraySearchFrame> processed,
        List<ArraySearchOrigin> fieldReads,
        List<ArraySearchClonedFieldStore> fieldStores,
        List<ArraySearchOrigin> staticReads,
        List<ArraySearchClonedStaticFieldStore> staticStores,
        List<ArraySearchClonedReflectedFieldStore> reflectedStores)
    {
        if (!(copy.ProcessedCallTargets ??= new HashSet<MethodInfo>()).Add(target))
            return false;
        for (ArraySearchFrame? ancestor = frame; ancestor is not null; ancestor = ancestor.Parent)
            if (ReferenceEquals(ancestor.Method, target))
            {
                copy.Unknown = true;
                return true;
            }
        ArraySearchFrame? calleeFrame = null;
        bool share = ArraySearchPureTypeReturn(target, new HashSet<MethodInfo>());
        if (share && _arraySearchPureFrames!.TryGetValue(target, out var frames))
            calleeFrame = frames.FirstOrDefault(existing =>
                ArraySearchSameActuals(existing.Arguments, actuals));
        bool freshFrame = calleeFrame is null;
        calleeFrame ??= new ArraySearchFrame(target, actuals, frame.Depth + 1, frame,
            copy.Call!.Offset, copy.Call.StraightLine);
        if (share && freshFrame)
        {
            if (!_arraySearchPureFrames!.TryGetValue(target, out frames))
                _arraySearchPureFrames.Add(target, frames = new List<ArraySearchFrame>());
            frames.Add(calleeFrame);
        }
        ProcessArraySearchMethod(calleeFrame, cache, processed, fieldReads, fieldStores,
            staticReads, staticStores, reflectedStores);
        if (copy.ByRefCallParameter >= 0)
        {
            if (ArraySearchDefiniteByRefWrite(target, copy.ByRefCallParameter) is { } written)
                AddArraySearchCloneEdge(copy,
                    CloneArraySearchOrigin(written, calleeFrame, cache, processed,
                        fieldReads, fieldStores, staticReads, staticStores, reflectedStores));
            else if (copy.ByRefCallFallback is { } previous)
                AddArraySearchCloneEdge(copy, previous);
            return true;
        }
        if (_arraySearchReturns.TryGetValue(target, out var returned))
            AddArraySearchCloneEdge(copy,
                CloneArraySearchOrigin(returned, calleeFrame, cache, processed, fieldReads, fieldStores,
                    staticReads, staticStores, reflectedStores));
        else
            copy.Unknown = true;
        return true;
    }

    private ArraySearchOrigin CloneArraySearchOrigin(ArraySearchOrigin original, ArraySearchFrame frame,
        Dictionary<(ArraySearchOrigin Origin, ArraySearchFrame Frame), ArraySearchOrigin> cache,
        HashSet<ArraySearchFrame> processed,
        List<ArraySearchOrigin> fieldReads,
        List<ArraySearchClonedFieldStore> fieldStores,
        List<ArraySearchOrigin> staticReads,
        List<ArraySearchClonedStaticFieldStore> staticStores,
        List<ArraySearchClonedReflectedFieldStore> reflectedStores)
    {
        if (original.ParameterMethod is { } parameterMethod)
        {
            if (ReferenceEquals(parameterMethod, frame.Method)
                && original.ParameterIndex < frame.Arguments.Length
                && frame.Arguments[original.ParameterIndex] is { } actual)
                return actual;
            if (!cache.TryGetValue((original, frame), out var unbound))
                cache.Add((original, frame), unbound = new ArraySearchOrigin { Unknown = true });
            return unbound;
        }
        if (cache.TryGetValue((original, frame), out var existing))
            return existing;
        var copy = new ArraySearchOrigin { Unknown = original.Unknown,
            ByRefCallParameter = original.ByRefCallParameter,
            ArrayAllocation = original.ArrayAllocation,
            ArrayAllocationOffset = original.ArrayAllocationOffset,
            ArrayAllocationMethod = original.ArrayAllocationMethod,
            AllocationFrame = original.ArrayAllocation ? frame : null,
            ObjectAllocation = original.ObjectAllocation,
            FieldAddress = original.FieldAddress,
            TypeArrayLength = original.TypeArrayLength,
            LookupOffset = original.LookupOffset,
            LookupFrame = original.LookupOffset >= 0 ? frame : null };
        cache.Add((original, frame), copy);
        if (original.ByRefCallFallback is { } fallback)
            copy.ByRefCallFallback = CloneArraySearchOrigin(fallback, frame, cache,
                processed, fieldReads, fieldStores, staticReads, staticStores, reflectedStores);
        copy.ShareSeedsFrom(original);
        if (original.FieldRead is { } fieldRead)
        {
            copy.FieldRead = (fieldRead.Field,
                CloneArraySearchOrigin(fieldRead.Receiver, frame, cache, processed, fieldReads, fieldStores,
                    staticReads, staticStores, reflectedStores),
                fieldRead.Owner, fieldRead.Offset, fieldRead.StraightLine);
            copy.FieldReadFrame = frame;
            fieldReads.Add(copy);
        }
        if (original.ReflectedFieldRead is { } reflectedField)
        {
            copy.ReflectedFieldRead = (
                CloneArraySearchOrigin(reflectedField.Handle, frame, cache, processed,
                    fieldReads, fieldStores, staticReads, staticStores, reflectedStores),
                reflectedField.Receiver is { } receiver
                    ? CloneArraySearchOrigin(receiver, frame, cache, processed,
                        fieldReads, fieldStores, staticReads, staticStores, reflectedStores) : null,
                reflectedField.Owner, reflectedField.Offset, reflectedField.StraightLine);
            copy.ReflectedFieldReadFrame = frame;
            fieldReads.Add(copy);
        }
        if (original.ElementRead is { } elementRead)
        {
            copy.ElementRead = (
                CloneArraySearchOrigin(elementRead.Array, frame, cache, processed,
                    fieldReads, fieldStores, staticReads, staticStores, reflectedStores),
                elementRead.Index, elementRead.Owner, elementRead.Offset, elementRead.StraightLine);
            copy.ElementReadFrame = frame;
        }
        if (original.RuntimeBoxHandle is { } boxHandle)
            copy.RuntimeBoxHandle = CloneArraySearchOrigin(boxHandle, frame, cache, processed,
                fieldReads, fieldStores, staticReads, staticStores, reflectedStores);
        if (original.LookupParameterTypes is { } lookupTypes)
            copy.LookupParameterTypes = CloneArraySearchOrigin(lookupTypes, frame, cache, processed,
                fieldReads, fieldStores, staticReads, staticStores, reflectedStores);
        if (original.StaticFieldRead is { } staticRead)
        {
            copy.StaticFieldRead = staticRead;
            copy.StaticFieldReadFrame = frame;
            staticReads.Add(copy);
        }
        if (original.Call is { } call)
        {
            var actuals = new ArraySearchOrigin?[call.Arguments.Length];
            for (int i = 0; i < actuals.Length; i++)
                if (call.Arguments[i] is { } argument)
                    actuals[i] = CloneArraySearchOrigin(argument, frame, cache, processed, fieldReads, fieldStores,
                        staticReads, staticStores, reflectedStores);
            copy.Call = call;
            copy.CallActuals = actuals;
            copy.CallFrame = frame;
            if (!call.Virtual)
                foreach (var target in ArraySearchCallTargets(call, actuals))
                    LinkArraySearchCallTarget(copy, target, actuals, frame, cache, processed,
                        fieldReads, fieldStores, staticReads, staticStores, reflectedStores);
            return copy;
        }
        foreach (var (source, kind, name) in original.Inputs)
            AddArraySearchCloneEdge(copy,
                CloneArraySearchOrigin(source, frame, cache, processed, fieldReads, fieldStores,
                    staticReads, staticStores, reflectedStores), kind, name);
        return copy;
    }

    private bool ApplyArraySearchStore(ArraySearchOrigin array, ArraySearchOrigin value,
        bool typeArray, int? index, int offset, ArraySearchFrame frame)
    {
        bool changed = false;
        var visited = new HashSet<ArraySearchOrigin>();
        void Attach(ArraySearchOrigin target)
        {
            if (!visited.Add(target))
                return;
            if (typeArray && index is { } slot && target.ArrayAllocation)
            {
                var events = target.TypeArrayStoreEvents ??= new();
                if (!events.Any(store => store.Index == slot && store.Offset == offset
                    && ReferenceEquals(store.Value, value) && ReferenceEquals(store.Frame, frame)))
                    events.Add((slot, value, offset, frame));
            }
            changed |= AddArraySearchCloneEdge(target, value,
                typeArray ? ArraySearchFlowKind.TypeArrayStoredAt : ArraySearchFlowKind.BoxedToArrayElement,
                index?.ToString(System.Globalization.CultureInfo.InvariantCulture));
            foreach (var (source, kind, _) in target.Inputs.ToList())
                if (kind == ArraySearchFlowKind.Identity)
                    Attach(source);
        }
        Attach(array);
        return changed;
    }

    private static (HashSet<ArraySearchOrigin> Sources, HashSet<ArraySearchOrigin> Allocations,
        bool Open) ArraySearchIdentitySources(ArraySearchOrigin origin)
    {
        var sources = new HashSet<ArraySearchOrigin>();
        var allocations = new HashSet<ArraySearchOrigin>();
        bool open = false;
        var pending = new Stack<ArraySearchOrigin>();
        pending.Push(origin);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!sources.Add(current))
                continue;
            if (current.ObjectAllocation || current.ArrayAllocation)
            {
                allocations.Add(current);
                continue;
            }
            bool hasIdentityInput = false;
            foreach (var input in current.Inputs)
                if (input.Kind == ArraySearchFlowKind.Identity)
                {
                    pending.Push(input.Source);
                    hasIdentityInput = true;
                }
            if (current.Unknown || !hasIdentityInput)
                open = true;
        }
        return (sources, allocations, open);
    }

    private static bool ArraySearchMayAlias(ArraySearchOrigin first, ArraySearchOrigin second,
        Dictionary<ArraySearchOrigin, (HashSet<ArraySearchOrigin> Sources,
            HashSet<ArraySearchOrigin> Allocations, bool Open)> summaries)
    {
        if (ReferenceEquals(first, second))
            return true;
        if (!summaries.TryGetValue(first, out var left))
            summaries.Add(first, left = ArraySearchIdentitySources(first));
        if (!summaries.TryGetValue(second, out var right))
            summaries.Add(second, right = ArraySearchIdentitySources(second));
        if (left.Sources.Overlaps(right.Sources))
            return true;
        return left.Open || right.Open || left.Allocations.Overlaps(right.Allocations);
    }

    private static bool ArraySearchMustAlias(ArraySearchOrigin first, ArraySearchOrigin second)
    {
        if (ReferenceEquals(first, second))
            return true;
        if (first.StaticFieldRead is { } leftField
            && second.StaticFieldRead is { } rightField
            && ReferenceEquals(leftField.Field, rightField.Field)
            && (leftField.Field.Attributes & System.Reflection.FieldAttributes.InitOnly) != 0)
            return true;
        var left = ArraySearchIdentitySources(first);
        var right = ArraySearchIdentitySources(second);
        return !left.Open && !right.Open && left.Allocations.Count == 1
            && right.Allocations.Count == 1 && left.Allocations.Overlaps(right.Allocations);
    }

    private bool LinkArraySearchFieldValues(ArraySearchOrigin read, MethodInfo owner,
        int offset, bool straightLine, ArraySearchFrame? frame,
        List<ArraySearchFieldValueCandidate> matching)
    {
        ArraySearchOrigin? receiver = read.FieldRead?.Receiver
            ?? read.ReflectedFieldRead?.Receiver ?? read.ElementRead?.Array;
        ArraySearchFieldValueCandidate? lastLocal = null;
        if (straightLine && owner.Rva != 0)
            foreach (var store in matching)
                if (store.StraightLine && ReferenceEquals(frame, store.Frame)
                    && store.Offset < offset
                    && (lastLocal is null || store.Offset > lastLocal.Offset)
                    && (receiver is null && store.Receiver is null
                        || receiver is not null && store.Receiver is { } storedReceiver
                            && ArraySearchMustAlias(receiver, storedReceiver))
                    && ArraySearchNoCallBetween(owner, store.Offset, offset))
                    lastLocal = store;
        if (lastLocal is not null)
            return AddArraySearchCloneEdge(read, lastLocal.Value);
        bool changed = false;
        foreach (var store in matching)
            if (!ArraySearchDefinitelyAfter(store.Frame, store.Offset, store.StraightLine,
                    frame, offset, straightLine)
                && !matching.Any(later => !ReferenceEquals(frame, store.Frame)
                && ReferenceEquals(store.Frame, later.Frame)
                && later.Offset > store.Offset && later.StraightLine && store.StraightLine
                && (store.Receiver is null && later.Receiver is null
                    || store.Receiver is { } a && later.Receiver is { } b
                        && ArraySearchMustAlias(a, b))
                && ArraySearchNoCallBetween(store.Frame.Method, store.Offset, later.Offset)))
                changed |= AddArraySearchCloneEdge(read, store.Value);
        return changed;
    }

    private bool LinkArraySearchElementValues(ArraySearchOrigin read,
        List<ArraySearchClonedElementStore> matching)
    {
        var element = read.ElementRead!.Value;
        ArraySearchClonedElementStore? lastLocal = null;
        if (element.StraightLine && element.Index is not null)
            foreach (var store in matching)
                if (store.StraightLine && store.Index == element.Index
                    && ReferenceEquals(read.ElementReadFrame, store.Frame)
                    && store.Offset < element.Offset
                    && (lastLocal is null || store.Offset > lastLocal.Offset)
                    && ArraySearchMustAlias(element.Array, store.Array)
                    && ArraySearchNoCallBetween(element.Owner, store.Offset, element.Offset))
                    lastLocal = store;
        if (lastLocal is not null && !matching.Any(store =>
                ReferenceEquals(store.Frame, read.ElementReadFrame)
                && store.Offset > lastLocal.Offset && store.Offset < element.Offset))
            return AddArraySearchCloneEdge(read, lastLocal.Value);
        bool changed = false;
        foreach (var store in matching)
            if (!ArraySearchDefinitelyAfter(store.Frame, store.Offset, store.StraightLine,
                    read.ElementReadFrame, element.Offset, element.StraightLine)
                && !matching.Any(later => store.Index is not null && later.Index == store.Index
                    && ReferenceEquals(store.Frame, later.Frame)
                    && later.Offset > store.Offset && later.StraightLine && store.StraightLine
                    && ArraySearchMustAlias(store.Array, later.Array)
                    && !ArraySearchDefinitelyAfter(later.Frame, later.Offset, later.StraightLine,
                        read.ElementReadFrame, element.Offset, element.StraightLine)
                    && ArraySearchNoCallBetween(store.Frame.Method, store.Offset, later.Offset)))
                changed |= AddArraySearchCloneEdge(read, store.Value);
        return changed;
    }

    private bool ArraySearchDefinitelyAfter(ArraySearchFrame storeFrame, int storeOffset,
        bool storeStraightLine, ArraySearchFrame? readFrame, int readOffset,
        bool readStraightLine)
    {
        if (ReferenceEquals(storeFrame, readFrame) && storeOffset > readOffset
            && ArraySearchForwardOnlyFrame(storeFrame))
            return true;
        if (!storeStraightLine || !readStraightLine || readFrame is null)
            return false;
        for (ArraySearchFrame? read = readFrame; read is not null;)
        {
            int comparableOffset = storeOffset;
            for (ArraySearchFrame? store = storeFrame; store is not null;)
            {
                if (ReferenceEquals(store, read))
                    return comparableOffset > readOffset;
                if (!store.ParentCallStraightLine)
                    break;
                comparableOffset = store.ParentCallOffset;
                store = store.Parent;
            }
            if (!read.ParentCallStraightLine)
                break;
            readOffset = read.ParentCallOffset;
            read = read.Parent;
        }
        return false;
    }

    private bool ArraySearchForwardOnlyFrame(ArraySearchFrame frame)
    {
        // A repeated caller invocation can expose an earlier iteration's later store.
        for (ArraySearchFrame? current = frame; current is not null; current = current.Parent)
            if (!ArraySearchForwardOnly(current.Method))
                return false;
        return true;
    }

    private bool ArraySearchForwardOnly(MethodInfo method)
    {
        if (_arraySearchForwardOnlyMethods.TryGetValue(method, out bool cached))
            return cached;
        bool forwardOnly = method.Rva != 0;
        if (forwardOnly)
        {
            var body = method.Module.PE.GetMethodBody(method.Rva);
            // Finally and filters can revisit earlier IL; ordinary catch handlers must advance.
            foreach (var region in body.ExceptionRegions)
                if (region.Kind != ExceptionRegionKind.Catch
                    || region.HandlerOffset < region.TryOffset + region.TryLength)
                    forwardOnly = false;
            foreach (var instruction in ILDecoder.Decode(body.GetILBytes()!.ToImmutableArrayCompat()))
                if (ILDecoder.IsBranch(instruction.OpCode)
                        && instruction.Operand <= instruction.Offset
                    || instruction.SwitchTargets is { } targets
                        && targets.Any(target => target <= instruction.Offset))
                    forwardOnly = false;
        }
        _arraySearchForwardOnlyMethods.Add(method, forwardOnly);
        return forwardOnly;
    }

    private bool ArraySearchNoCallBetween(MethodInfo owner, int from, int to)
    {
        if (owner.Rva == 0)
            return false;
        if (!_arraySearchCallBarrierOffsets.TryGetValue(owner, out var offsets))
        {
            var barriers = new List<int>();
            foreach (var instruction in ILDecoder.Decode(owner.Module.PE
                .GetMethodBody(owner.Rva).GetILBytes()!.ToImmutableArrayCompat()))
                if (instruction.OpCode is ILOpCode.Call or ILOpCode.Callvirt
                    or ILOpCode.Calli or ILOpCode.Newobj
                    && !ArraySearchPureLengthRead(owner.Module.Reader, instruction.Token)
                    && !ArraySearchPureFieldLookup(owner.Module.Reader, instruction.Token))
                    barriers.Add(instruction.Offset);
            offsets = barriers.ToArray();
            _arraySearchCallBarrierOffsets.Add(owner, offsets);
        }
        int next = Array.BinarySearch(offsets, from + 1);
        if (next < 0)
            next = ~next;
        return next == offsets.Length || offsets[next] >= to;
    }

    private static bool ArraySearchPureLengthRead(MetadataReader reader, int token)
    {
        var handle = System.Reflection.Metadata.Ecma335.MetadataTokens.EntityHandle(token);
        if (handle.Kind != HandleKind.MemberReference)
            return false;
        var member = reader.GetMemberReference((MemberReferenceHandle)handle);
        if (reader.GetString(member.Name) != "get_Length")
            return false;
        if (member.Parent.Kind == HandleKind.TypeReference)
        {
            var type = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
            return reader.GetString(type.Namespace) == "System"
                && reader.GetString(type.Name) == "Array";
        }
        if (member.Parent.Kind == HandleKind.TypeDefinition)
        {
            var type = reader.GetTypeDefinition((TypeDefinitionHandle)member.Parent);
            return reader.GetString(type.Namespace) == "System"
                && reader.GetString(type.Name) == "Array";
        }
        return false;
    }

    private static bool ArraySearchPureFieldLookup(MetadataReader reader, int token)
    {
        var handle = System.Reflection.Metadata.Ecma335.MetadataTokens.EntityHandle(token);
        if (handle.Kind != HandleKind.MemberReference)
            return false;
        var member = reader.GetMemberReference((MemberReferenceHandle)handle);
        string name = reader.GetString(member.Name);
        if (member.Parent.Kind != HandleKind.TypeReference)
            return false;
        var type = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
        string ns = reader.GetString(type.Namespace);
        string owner = reader.GetString(type.Name);
        return name == "GetTypeFromHandle" && ns == "System" && owner == "Type"
            || name == "GetField" && ns == "System" && owner == "Type"
            || name == "GetValue" && ns == "System.Reflection" && owner == "FieldInfo";
    }

    private static void RegisterArraySearchIndirectFieldStore(ArraySearchOrigin target,
        ArraySearchOrigin value, int offset, bool straightLine, ArraySearchFrame frame,
        List<ArraySearchClonedFieldStore> fieldStores,
        List<ArraySearchClonedStaticFieldStore> staticStores)
    {
        var visited = new HashSet<ArraySearchOrigin>();
        void Visit(ArraySearchOrigin current)
        {
            if (!visited.Add(current))
                return;
            if (current.FieldAddress && current.FieldRead is { } field)
                fieldStores.Add(new ArraySearchClonedFieldStore(field.Field,
                    field.Receiver, value, offset, straightLine, frame));
            if (current.FieldAddress && current.StaticFieldRead is { } staticField)
                staticStores.Add(new ArraySearchClonedStaticFieldStore(staticField.Field,
                    value, offset, straightLine, frame));
            foreach (var input in current.Inputs)
                if (input.Kind == ArraySearchFlowKind.Identity)
                    Visit(input.Source);
        }
        Visit(target);
    }

    private void ProcessArraySearchMethod(ArraySearchFrame frame,
        Dictionary<(ArraySearchOrigin Origin, ArraySearchFrame Frame), ArraySearchOrigin> cache,
        HashSet<ArraySearchFrame> processed,
        List<ArraySearchOrigin> fieldReads,
        List<ArraySearchClonedFieldStore> fieldStores,
        List<ArraySearchOrigin> staticReads,
        List<ArraySearchClonedStaticFieldStore> staticStores,
        List<ArraySearchClonedReflectedFieldStore> reflectedStores)
    {
        if (!processed.Add(frame))
            return;
        if (_arraySearchStores.TryGetValue(frame.Method, out var stores))
            foreach (var store in stores.ToList())
                ApplyArraySearchStore(
                    CloneArraySearchOrigin(store.Array, frame, cache, processed, fieldReads, fieldStores,
                        staticReads, staticStores, reflectedStores),
                    CloneArraySearchOrigin(store.Value, frame, cache, processed, fieldReads, fieldStores,
                        staticReads, staticStores, reflectedStores),
                    store.TypeArray, store.Index, store.Offset, frame);
        if (_arraySearchWriteEffects.TryGetValue(frame.Method, out var effects))
            foreach (var effect in effects)
            {
                if (frame.Parent is { } caller
                    && _arraySearchProvenByRefCalls.TryGetValue((caller.Method,
                        frame.ParentCallOffset, frame.Method), out int parameter)
                    && ReferenceEquals(ArraySearchDefiniteByRefWrite(frame.Method, parameter), effect.Value))
                    continue;
                var target = CloneArraySearchOrigin(effect.Target, frame, cache, processed,
                    fieldReads, fieldStores, staticReads, staticStores, reflectedStores);
                var value = CloneArraySearchOrigin(effect.Value, frame, cache, processed,
                    fieldReads, fieldStores, staticReads, staticStores, reflectedStores);
                if (effect.Flow == ArraySearchFlowKind.Identity)
                    RegisterArraySearchIndirectFieldStore(target, value, effect.Offset,
                        effect.StraightLine, frame, fieldStores, staticStores);
                AddArraySearchCloneEdge(target, value, effect.Flow);
            }
        if (_arraySearchInstanceFieldStores.TryGetValue(frame.Method, out var instanceStores))
            foreach (var store in instanceStores)
                fieldStores.Add(new ArraySearchClonedFieldStore(store.Field,
                    CloneArraySearchOrigin(store.Receiver, frame, cache, processed, fieldReads, fieldStores,
                        staticReads, staticStores, reflectedStores),
                    CloneArraySearchOrigin(store.Value, frame, cache, processed, fieldReads, fieldStores,
                        staticReads, staticStores, reflectedStores),
                    store.Offset, store.StraightLine, frame));
        if (_arraySearchStaticFieldStores.TryGetValue(frame.Method, out var fieldWrites))
            foreach (var store in fieldWrites)
                staticStores.Add(new ArraySearchClonedStaticFieldStore(store.Field,
                    CloneArraySearchOrigin(store.Value, frame, cache, processed, fieldReads, fieldStores,
                        staticReads, staticStores, reflectedStores),
                    store.Offset, store.StraightLine, frame));
        if (_arraySearchReflectedFieldStores.TryGetValue(frame.Method, out var reflectionWrites))
            foreach (var store in reflectionWrites)
                reflectedStores.Add(new ArraySearchClonedReflectedFieldStore(
                    CloneArraySearchOrigin(store.Handle, frame, cache, processed, fieldReads, fieldStores,
                        staticReads, staticStores, reflectedStores),
                    store.Receiver is { } receiver
                        ? CloneArraySearchOrigin(receiver, frame, cache, processed, fieldReads, fieldStores,
                            staticReads, staticStores, reflectedStores) : null,
                    CloneArraySearchOrigin(store.Value, frame, cache, processed, fieldReads, fieldStores,
                        staticReads, staticStores, reflectedStores),
                    store.Offset, store.StraightLine, frame));
        if (_arraySearchCalls.TryGetValue(frame.Method, out var calls))
            foreach (var call in calls.ToList())
                if (call.Call is { } target
                    && (_arraySearchExpandedCallOwners?.Contains(target.Method) == true
                        || _arraySearchExpandedCallOwners?.Contains(target.Method.Emittable) == true
                        || target.Virtual))
                    CloneArraySearchOrigin(call, frame, cache, processed, fieldReads, fieldStores,
                        staticReads, staticStores, reflectedStores);
    }

    internal IReadOnlyList<TypeDesc> SelectedArraySearchElements()
    {
        if (!TrackArraySearchOrigins || _arraySearchOperands.Count == 0)
            return Array.Empty<TypeDesc>();
        // Emitted origins and field stores advance the epoch. Shape/member completion,
        // method instantiation and allocation can change rows or virtual targets.
        if (_arraySearchSelectedTypes is not null
            && _arraySearchSelectedEpoch == _arraySearchGraphEpoch
            && _arraySearchSelectedClassCount == Classes.Count
            && _arraySearchSelectedAllocatedCount == _allocatedRefTypes.Count
            && _arraySearchSelectedShapeCount == Classes.Count(c => c.ShapeCompleted)
            && _arraySearchSelectedMemberCount == _membersCompletedOrder.Count
            && _arraySearchSelectedMethodInstanceCount == _methodInstanceOrder.Count)
            return _arraySearchSelectedTypes;
        var callersByTarget = new Dictionary<MethodInfo, HashSet<MethodInfo>>();
        var virtualCallersBySignature = new Dictionary<(string Name, int Arity), HashSet<MethodInfo>>();
        static void AddCaller<TKey>(Dictionary<TKey, HashSet<MethodInfo>> index,
            TKey key, MethodInfo caller) where TKey : notnull
        {
            if (!index.TryGetValue(key, out var callers))
                index.Add(key, callers = new HashSet<MethodInfo>());
            callers.Add(caller);
        }
        foreach (var (caller, calls) in _arraySearchCalls)
            foreach (var origin in calls)
                if (origin.Call is { } call)
                {
                    AddCaller(callersByTarget, call.Method, caller);
                    AddCaller(callersByTarget, call.Method.Emittable, caller);
                    if (call.Virtual)
                        AddCaller(virtualCallersBySignature,
                            (call.Method.Name, call.Method.Signature.ParameterTypes.Length), caller);
                }
        void AddCallers(HashSet<MethodInfo> methods)
        {
            var pending = new Queue<MethodInfo>(methods);
            while (pending.TryDequeue(out var target))
            {
                if (callersByTarget.TryGetValue(target, out var direct))
                    foreach (var caller in direct)
                        if (methods.Add(caller))
                            pending.Enqueue(caller);
                if (virtualCallersBySignature.TryGetValue(
                    (target.Name, target.Signature.ParameterTypes.Length), out var virtuals))
                    foreach (var caller in virtuals)
                        if (methods.Add(caller))
                            pending.Enqueue(caller);
            }
        }
        var expandedCalls = new HashSet<MethodInfo>(_arraySearchOperands.Select(entry => entry.Owner));
        expandedCalls.UnionWith(_arraySearchStores.Keys);
        expandedCalls.UnionWith(_arraySearchWriteEffects.Keys);
        expandedCalls.UnionWith(_arraySearchInstanceFieldStores.Keys);
        expandedCalls.UnionWith(_arraySearchStaticFieldStores.Keys);
        expandedCalls.UnionWith(_arraySearchReflectedFieldStores.Keys);
        AddCallers(expandedCalls);
        _arraySearchExpandedCallOwners = expandedCalls;
        _arraySearchPureFrames = new Dictionary<MethodInfo, List<ArraySearchFrame>>();
        _arraySearchPureReturnCache = new Dictionary<MethodInfo, bool>();
        var cache = new Dictionary<(ArraySearchOrigin Origin, ArraySearchFrame Frame), ArraySearchOrigin>();
        var processed = new HashSet<ArraySearchFrame>();
        var roots = new List<ArraySearchOrigin>();
        var fieldReads = new List<ArraySearchOrigin>();
        var fieldStores = new List<ArraySearchClonedFieldStore>();
        var staticReads = new List<ArraySearchOrigin>();
        var staticStores = new List<ArraySearchClonedStaticFieldStore>();
        var reflectedStores = new List<ArraySearchClonedReflectedFieldStore>();
        var rooted = new HashSet<(ArraySearchOrigin Operand, ArraySearchFrame Frame)>();
        var rootFrames = new Dictionary<MethodInfo, ArraySearchFrame>();
        // Search and runtime-box roots use each invocation's parameter frame.
        var needed = new HashSet<MethodInfo>(_arraySearchOperands.Select(entry => entry.Owner));
        bool added;
        bool repeat;
        do
        {
            AddCallers(needed);
            int beforeFrames = processed.Count;
            foreach (var owner in needed.ToList())
            {
                if (!rootFrames.TryGetValue(owner, out var frame))
                    rootFrames.Add(owner, frame = new ArraySearchFrame(owner,
                        Array.Empty<ArraySearchOrigin?>(), 0));
                ProcessArraySearchMethod(frame, cache, processed, fieldReads, fieldStores,
                    staticReads, staticStores, reflectedStores);
            }
            foreach (var frame in processed.ToList())
                foreach (var (owner, operand) in _arraySearchOperands)
                    if (ReferenceEquals(owner, frame.Method) && rooted.Add((operand, frame)))
                        roots.Add(CloneArraySearchOrigin(operand, frame, cache, processed,
                            fieldReads, fieldStores, staticReads, staticStores, reflectedStores));
            var reflectedReads = fieldReads.Where(read => read.ReflectedFieldRead is not null).ToList();
            if (reflectedReads.Count > 0 || reflectedStores.Count > 0)
                EvaluateArraySearchValues(reflectedReads.Select(read => read.ReflectedFieldRead!.Value.Handle)
                    .Concat(reflectedStores.Select(store => store.Handle)));
            var reflectedFields = new Dictionary<ArraySearchOrigin, List<FieldInfo>>();
            foreach (var read in reflectedReads)
            {
                var fields = ArraySearchReflectedFields(read.ReflectedFieldRead!.Value.Handle)
                    .Distinct().ToList();
                reflectedFields.Add(read, fields);
            }
            var reflectedStoreFields = new Dictionary<ArraySearchClonedReflectedFieldStore, List<FieldInfo>>();
            foreach (var store in reflectedStores)
                reflectedStoreFields.Add(store, ArraySearchReflectedFields(store.Handle)
                    .Distinct().ToList());
            var selectedFields = new HashSet<FieldInfo>(fieldReads
                .Where(read => read.FieldRead is not null)
                .Select(read => read.FieldRead!.Value.Field));
            foreach (var fields in reflectedFields.Values)
                selectedFields.UnionWith(fields.Where(field => !field.IsStatic));
            added = false;
            foreach (var (owner, stores) in _arraySearchInstanceFieldStores)
                if (stores.Any(store => selectedFields.Contains(store.Field)))
                    added |= needed.Add(owner);
            if (selectedFields.Count > 0 || reflectedReads.Count > 0)
                foreach (var owner in _arraySearchReflectedFieldStores.Keys)
                    added |= needed.Add(owner);
            var selectedStaticFields = new HashSet<FieldInfo>(staticReads
                .Where(read => read.StaticFieldRead is not null)
                .Select(read => read.StaticFieldRead!.Value.Field));
            foreach (var fields in reflectedFields.Values)
                selectedStaticFields.UnionWith(fields.Where(field => field.IsStatic));
            foreach (var (owner, stores) in _arraySearchStaticFieldStores)
                if (stores.Any(store => selectedStaticFields.Contains(store.Field)))
                    added |= needed.Add(owner);
            var fieldStoresByField = new Dictionary<FieldInfo, List<ArraySearchClonedFieldStore>>();
            foreach (var store in fieldStores)
            {
                if (!fieldStoresByField.TryGetValue(store.Field, out var stores))
                    fieldStoresByField.Add(store.Field, stores = new List<ArraySearchClonedFieldStore>());
                stores.Add(store);
            }
            var staticStoresByField = new Dictionary<FieldInfo, List<ArraySearchClonedStaticFieldStore>>();
            foreach (var store in staticStores)
            {
                if (!staticStoresByField.TryGetValue(store.Field, out var stores))
                    staticStoresByField.Add(store.Field, stores = new List<ArraySearchClonedStaticFieldStore>());
                stores.Add(store);
            }
            bool newFieldEdges = false;
            var referenceBoxes = cache.Values.Where(origin => origin.RuntimeBoxHandle is not null).ToList();
            EvaluateArraySearchValues(referenceBoxes.Select(origin => origin.RuntimeBoxHandle!));
            // Reference handles return the slot object, including its mutable array identity.
            foreach (var origin in referenceBoxes)
                if (ArraySearchBoxReturnsReference(origin))
                    foreach (var input in origin.Inputs.ToList())
                        if (input.Kind == ArraySearchFlowKind.ReferenceSlotBoxValue)
                            newFieldEdges |= AddArraySearchCloneEdge(origin, input.Source);
            var aliasSources = new Dictionary<ArraySearchOrigin,
                (HashSet<ArraySearchOrigin> Sources, HashSet<ArraySearchOrigin> Allocations, bool Open)>();
            foreach (var read in fieldReads)
                if (read.FieldRead is { } fieldRead)
                {
                    var matching = new List<ArraySearchFieldValueCandidate>();
                    if (fieldStoresByField.TryGetValue(fieldRead.Field, out var stores))
                        foreach (var store in stores)
                            if (ArraySearchMayAlias(fieldRead.Receiver, store.Receiver, aliasSources))
                                matching.Add(new ArraySearchFieldValueCandidate(store.Value, store.Receiver,
                                    store.Offset, store.StraightLine, store.Frame));
                    foreach (var (store, fields) in reflectedStoreFields)
                        if (fields.Contains(fieldRead.Field) && store.Receiver is { } receiver
                            && ArraySearchMayAlias(fieldRead.Receiver, receiver, aliasSources))
                            matching.Add(new ArraySearchFieldValueCandidate(store.Value, receiver,
                                store.Offset, store.StraightLine, store.Frame));
                    bool changedRead = LinkArraySearchFieldValues(read, fieldRead.Owner, fieldRead.Offset,
                        fieldRead.StraightLine, read.FieldReadFrame, matching);
                    newFieldEdges |= changedRead;
                    if (changedRead)
                        aliasSources.Clear();
                }
            foreach (var read in staticReads)
                if (read.StaticFieldRead is { } staticRead)
                {
                    var matching = new List<ArraySearchFieldValueCandidate>();
                    if (staticStoresByField.TryGetValue(staticRead.Field, out var stores))
                        foreach (var store in stores)
                            matching.Add(new ArraySearchFieldValueCandidate(store.Value, null,
                                store.Offset, store.StraightLine, store.Frame));
                    foreach (var (store, fields) in reflectedStoreFields)
                        if (fields.Contains(staticRead.Field))
                            matching.Add(new ArraySearchFieldValueCandidate(store.Value, null,
                                store.Offset, store.StraightLine, store.Frame));
                    bool changedRead = LinkArraySearchFieldValues(read, staticRead.Owner, staticRead.Offset,
                        staticRead.StraightLine, read.StaticFieldReadFrame, matching);
                    newFieldEdges |= changedRead;
                    if (changedRead)
                        aliasSources.Clear();
                }
            foreach (var (read, fields) in reflectedFields)
                if (read.ReflectedFieldRead is { } reflectedRead)
                    foreach (var field in fields)
                    {
                        var matching = new List<ArraySearchFieldValueCandidate>();
                        if (field.IsStatic)
                        {
                            if (staticStoresByField.TryGetValue(field, out var stores))
                                foreach (var store in stores)
                                    matching.Add(new ArraySearchFieldValueCandidate(store.Value, null,
                                        store.Offset, store.StraightLine, store.Frame));
                        }
                        else if (reflectedRead.Receiver is { } receiver)
                        {
                            if (fieldStoresByField.TryGetValue(field, out var stores))
                                foreach (var store in stores)
                                    if (ArraySearchMayAlias(receiver, store.Receiver, aliasSources))
                                        matching.Add(new ArraySearchFieldValueCandidate(store.Value,
                                            store.Receiver, store.Offset, store.StraightLine, store.Frame));
                        }
                        foreach (var (store, storedFields) in reflectedStoreFields)
                            if (storedFields.Contains(field)
                                && (field.IsStatic || reflectedRead.Receiver is { } readReceiver
                                    && store.Receiver is { } storedReceiver
                                    && ArraySearchMayAlias(readReceiver, storedReceiver, aliasSources)))
                                matching.Add(new ArraySearchFieldValueCandidate(store.Value,
                                    store.Receiver, store.Offset, store.StraightLine, store.Frame));
                        bool changedRead = LinkArraySearchFieldValues(read, reflectedRead.Owner,
                            reflectedRead.Offset, reflectedRead.StraightLine,
                            read.ReflectedFieldReadFrame, matching);
                        newFieldEdges |= changedRead;
                        if (changedRead)
                            aliasSources.Clear();
                    }
            var elementStores = new List<ArraySearchClonedElementStore>();
            foreach (var frame in processed.ToList())
                if (_arraySearchStores.TryGetValue(frame.Method, out var stores))
                    foreach (var store in stores.ToList())
                        elementStores.Add(new ArraySearchClonedElementStore(
                            CloneArraySearchOrigin(store.Array, frame, cache, processed,
                                fieldReads, fieldStores, staticReads, staticStores, reflectedStores),
                            CloneArraySearchOrigin(store.Value, frame, cache, processed,
                                fieldReads, fieldStores, staticReads, staticStores, reflectedStores),
                            store.TypeArray, store.Index, store.Offset, store.StraightLine, frame));
            foreach (var read in cache.Values.ToList())
                if (read.ElementRead is { } elementRead)
                {
                    var matching = new List<ArraySearchClonedElementStore>();
                    foreach (var store in elementStores)
                        if ((elementRead.Index is null || store.Index is null
                                || elementRead.Index == store.Index)
                            && ArraySearchIdentitySources(elementRead.Array).Allocations.Count > 0
                            && ArraySearchIdentitySources(store.Array).Allocations.Count > 0
                            && ArraySearchMayAlias(elementRead.Array, store.Array, aliasSources))
                            matching.Add(store);
                    bool changedRead = LinkArraySearchElementValues(read, matching);
                    newFieldEdges |= changedRead;
                    if (changedRead)
                        aliasSources.Clear();
                }
            foreach (var store in elementStores)
                newFieldEdges |= ApplyArraySearchStore(store.Array, store.Value,
                    store.TypeArray, store.Index, store.Offset, store.Frame);
            bool newTargets = false;
            var virtualCalls = cache.Values.Where(copy => copy.Call is { Virtual: true }
                && copy.CallActuals is { Length: > 0 } actuals && actuals[0] is not null
                && copy.CallFrame is not null).ToList();
            EvaluateArraySearchValues(virtualCalls.Select(copy => copy.CallActuals![0]!));
            foreach (var copy in virtualCalls)
                if (copy.Call is { Virtual: true } call && copy.CallActuals is { } actuals
                    && copy.CallFrame is { } frame)
                    foreach (var target in ArraySearchCallTargets(call, actuals))
                        newTargets |= LinkArraySearchCallTarget(copy, target, actuals, frame,
                            cache, processed, fieldReads, fieldStores, staticReads, staticStores,
                            reflectedStores);
            repeat = added || processed.Count != beforeFrames || newFieldEdges || newTargets;
        } while (repeat);
        EvaluateArraySearchValues(roots);
        var selected = new Dictionary<string, TypeDesc>(StringComparer.Ordinal);
        var boxes = new Dictionary<string, TypeDesc>(StringComparer.Ordinal);
        foreach (var operand in roots)
        {
            if (operand.Values.TryGetValue(ArraySearchValueKind.ArrayElement, out var elements))
                foreach (var (identity, type) in elements)
                    selected.TryAdd(identity, type);
            if (operand.RuntimeBoxHandle is not null
                && operand.Values.TryGetValue(ArraySearchValueKind.BoxedValue, out var boxed))
                foreach (var (identity, type) in boxed)
                    boxes.TryAdd(identity, type);
        }
        _arraySearchSelectedTypes = selected.OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => entry.Value).ToList();
        _runtimeHandleBoxSelectedTypes = boxes.OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => entry.Value).ToList();
        _arraySearchRelevantOrigins = cache.Keys.Select(key => key.Origin).ToHashSet();
        _arraySearchSelectedEpoch = _arraySearchGraphEpoch;
        _arraySearchSelectedClassCount = Classes.Count;
        _arraySearchSelectedAllocatedCount = _allocatedRefTypes.Count;
        _arraySearchSelectedShapeCount = Classes.Count(c => c.ShapeCompleted);
        _arraySearchSelectedMemberCount = _membersCompletedOrder.Count;
        _arraySearchSelectedMethodInstanceCount = _methodInstanceOrder.Count;
        return _arraySearchSelectedTypes;
    }

    internal IReadOnlyList<TypeDesc> SelectedRuntimeHandleBoxes()
    {
        if (!_runtimeHandleBoxUsed || !TrackArraySearchOrigins)
            return Array.Empty<TypeDesc>();
        SelectedArraySearchElements();
        return _runtimeHandleBoxSelectedTypes;
    }
}
