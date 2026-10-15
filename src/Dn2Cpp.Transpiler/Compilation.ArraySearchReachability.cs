namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly HashSet<TypeDesc> _arraySearchSeenTypes = new();
    private readonly HashSet<ClassInfo> _arraySearchCandidateClasses = new();
    private IReadOnlyList<TypeDesc> _arraySearchSelectedTypes = Array.Empty<TypeDesc>();
    private int _arraySearchCandidateVersion;
    private int _arraySearchSelectedVersion = -1;
    private int _arraySearchArrayTypesScanned;
    private int _arraySearchMdTypesScanned;
    private int _arraySearchAllocatedTypesScanned;
    private bool _arraySearchRuntimeFactoryUsed;
    private bool _runtimeHandleBoxUsed;
    internal bool NeedsConservativeArraySearchClosure =>
        _nonGenericArrayEqualityDispatched || _runtimeHandleBoxUsed;

    internal void AddConservativeArraySearchType(TypeDesc? type)
    {
        if (type is null || !_arraySearchSeenTypes.Add(type)
            || ContainsCanonPlaceholder(type) || ContainsGenericVar(type) || ContainsCanonAny(type))
            return;
        if (type.Kind == TypeKind.Class && type.Class is { } cls)
        {
            if (cls.Context.TypeArgs.Length != cls.GenericArity)
                return;
            if (_arraySearchCandidateClasses.Add(cls))
                _arraySearchCandidateVersion++;
            foreach (var argument in cls.Context.TypeArgs)
                AddConservativeArraySearchType(argument);
        }
        else if (type.Element is { } element)
            AddConservativeArraySearchType(element);
    }

    internal void AddConservativeArraySearchLayouts(IEnumerable<ClassInfo> layouts)
    {
        if (!NeedsConservativeArraySearchClosure
            || (!_arraySearchRuntimeFactoryUsed && !_runtimeHandleBoxUsed))
            return;
        // Runtime factories can select emitted metadata, not every held definition.
        // Layout closure includes types first named by reflected signatures and fields.
        foreach (var cls in layouts)
            if (!_arraySearchCandidateClasses.Contains(cls))
                AddConservativeArraySearchType(TypeDesc.MakeClass(cls));
    }

    private void CollectConservativeArraySearchTypes()
    {
        if (_arraySearchArrayTypesScanned != ArrayElementTypes.Count)
        {
            _arraySearchArrayTypesScanned = ArrayElementTypes.Count;
            foreach (var element in ArrayElementTypes.Values.ToList())
                AddConservativeArraySearchType(element);
        }
        if (_arraySearchMdTypesScanned != MdArrayTypes.Count)
        {
            _arraySearchMdTypesScanned = MdArrayTypes.Count;
            foreach (var type in MdArrayTypes.Values.ToList())
                AddConservativeArraySearchType(type.Element);
        }
        if (_arraySearchAllocatedTypesScanned != _allocatedRefTypes.Count)
        {
            _arraySearchAllocatedTypesScanned = _allocatedRefTypes.Count;
            foreach (var cls in _allocatedRefTypes.ToList())
                AddConservativeArraySearchType(TypeDesc.MakeClass(cls));
        }
    }

    internal IReadOnlyList<TypeDesc> SelectedArraySearchElements()
    {
        CollectConservativeArraySearchTypes();
        if (_arraySearchSelectedVersion == _arraySearchCandidateVersion)
            return _arraySearchSelectedTypes;
        var selected = new List<TypeDesc>();
        foreach (var cls in _arraySearchCandidateClasses.ToList())
            if (EnsureShape(cls) is { IsValueType: true, IsByRefLike: false }
                && cls.Context.TypeArgs.Length == cls.GenericArity
                && !ContainsCanonPlaceholder(cls) && !ContainsGenericVar(cls)
                && !cls.Context.TypeArgs.Any(ContainsCanonAny))
                selected.Add(TypeDesc.MakeClass(cls));
        selected.Sort((left, right) => ClassInfo.CompareByOrder(left.Class!, right.Class!));
        _arraySearchSelectedTypes = selected;
        _arraySearchSelectedVersion = _arraySearchCandidateVersion;
        return _arraySearchSelectedTypes;
    }

    internal IReadOnlyList<TypeDesc> SelectedRuntimeHandleBoxes() =>
        _runtimeHandleBoxUsed ? SelectedArraySearchElements() : Array.Empty<TypeDesc>();

    internal void NoteArraySearchRuntimeFactory() => _arraySearchRuntimeFactoryUsed = true;
    internal void NoteRuntimeHandleBox() => _runtimeHandleBoxUsed = true;
}
