using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

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
    private bool _unknownArraySearchReceiver;
    private readonly HashSet<TypeDesc> _finiteArraySearchElements = new();
    internal bool NeedsConservativeArraySearchClosure =>
        _unknownArraySearchReceiver || _runtimeHandleBoxUsed;

    private void NoteNonGenericArraySearch(MethodInfo method, int offset)
    {
        _nonGenericArrayEqualityDispatched = true;
        if (_unknownArraySearchReceiver)
            return;
        if (method.Module.ReflectionSummaries.TryGetValue(method.Handle, out var summary))
            foreach (var site in summary.ArraySearchSites)
                if (site.Offset == offset && site.Types.IsKnown)
                {
                    var elements = new List<TypeDesc>();
                    foreach (int id in site.Types.Alternatives!)
                    {
                        var term = summary.Terms[id];
                        if (term.Kind != TermKind.ArrayOf || term.Children.Length != 1)
                            break;
                        var child = summary.ValueOf(term.Children[0]);
                        if (!child.IsKnown || child.Count != 1)
                            break;
                        var elementTerm = summary.Terms[child.Alternatives![0]];
                        if (elementTerm.Kind != TermKind.TypeToken)
                            break;
                        var element = ResolveTypeTokenForScan(method.Module,
                            SRME.EntityHandle(elementTerm.Token), method.Context);
                        if (element is null || ContainsGenericVar(element) || ContainsCanonAny(element)
                            || ContainsCanonPlaceholder(element))
                            break;
                        // Reference arrays may contain boxes whose runtime types the
                        // local array identity does not constrain.
                        bool value = element.Kind == TypeKind.Primitive
                            ? element.Primitive is not (PrimitiveTypeCode.Object or PrimitiveTypeCode.String
                                or PrimitiveTypeCode.Void)
                            : element.Kind == TypeKind.Class && EnsureShape(element.Class!).IsValueType;
                        if (!value)
                            break;
                        elements.Add(element);
                    }
                    if (elements.Count == site.Types.Count)
                    {
                        foreach (var element in elements)
                            if (element.Kind == TypeKind.Class)
                                _finiteArraySearchElements.Add(element);
                        return;
                    }
                    break;
                }
        _unknownArraySearchReceiver = true;
    }

    private IReadOnlyList<TypeDesc> ArraySearchEqualityElements()
    {
        if (_unknownArraySearchReceiver)
            return SelectedArraySearchElements();
        var elements = _finiteArraySearchElements.ToList();
        elements.Sort((left, right) => ClassInfo.CompareByOrder(left.Class!, right.Class!));
        return elements;
    }

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
