using System.Reflection.Metadata;
using PreserveKind = Dn2Cpp.PreservationReader.PreserveKind;
using PreservePolicy = Dn2Cpp.PreservationReader.PreservePolicy;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly IReadOnlyList<string> _projectRoots;
    private readonly IReadOnlyList<string> _linkXmlFiles;
    private readonly HashSet<string> _linkFeatures;
    private Dictionary<(int Module, TypeDefinitionHandle Type), PreservePolicy> _preservePolicies = new();
    private readonly HashSet<ClassInfo> _explicitReflectionKeep = new();
    private readonly HashSet<ClassInfo> _activatedConditionalPolicies = new();
    private bool _preservationSeedingActive;

    private void DiscoverPreservationPolicies()
    {
        var modules = new List<PreservationModule>();
        foreach (var module in Modules)
            modules.Add(new PreservationModule { Index = module.Index, AssemblyName = module.AssemblyName, Reader = module.Reader });
        _preservePolicies = new PreservationReader(modules, _projectRoots, _linkXmlFiles,
            _linkFeatures.ToArray()).Read();
    }

    private void SeedPreservedMembers()
    {
        _preservationSeedingActive = true;
        foreach (var cls in Classes.ToList())
            if (cls.MembersReady)
                ApplyPreservation(cls);
            else if (cls.ShapeReady)
                ApplyPreservationAfterShape(cls);
    }

    private void ApplyPreservation(ClassInfo cls)
    {
        // Policies key on the TypeDef handle, so an open shell (an unsubstituted
        // !!n spec from an empty-context overload decode) matches its definition's
        // policy — preserving it drags an open definition into the emit set. Each
        // closed instantiation gets the policy applied as it completes.
        if (ContainsGenericVar(cls))
            return;
        if (!_preservePolicies.TryGetValue((cls.Module.Index, cls.Handle), out var policy))
            return;
        bool conditional = _activatedConditionalPolicies.Contains(cls);
        PreserveKind kind = policy.Kind | (conditional ? policy.ConditionalKind : PreserveKind.None);
        bool delegateAll = cls.IsDelegate && kind != PreserveKind.None;
        if (kind != PreserveKind.None || policy.Fields.Count > 0 || policy.Methods.Count > 0
            || policy.Properties.Count > 0 || policy.Events.Count > 0
            || conditional && (policy.ConditionalFields.Count > 0 || policy.ConditionalMethods.Count > 0
                || policy.ConditionalProperties.Count > 0 || policy.ConditionalEvents.Count > 0))
        {
            NoteForceEmit(cls);
            _explicitReflectionKeep.Add(cls);
        }
        if ((kind & PreserveKind.Fields) != 0)
            foreach (var field in cls.Fields) PreserveField(field);
        else
            foreach (var field in cls.Fields)
                if (policy.Fields.Contains(field.Handle)
                    || conditional && policy.ConditionalFields.Contains(field.Handle)) PreserveField(field);
        bool ctorPreserved = false;
        foreach (var method in cls.Methods)
        {
            bool keep = delegateAll || (kind & PreserveKind.Methods) != 0
                || policy.Methods.Contains(method.Handle)
                || conditional && policy.ConditionalMethods.Contains(method.Handle)
                || (kind & PreserveKind.DefaultConstructor) != 0
                    && method.Name == ".ctor" && method.Signature.ParameterTypes.Length == 0;
            if (keep)
            {
                PreserveMethod(method);
                ctorPreserved |= method.Name == ".ctor" && !method.IsStatic && method.Rva != 0;
            }
        }
        // A preserved instance ctor declares "reflection constructs this" — the
        // instance then dispatches through vtable and interface slots, so the
        // class must cross the used-slot × allocated-type product. Reaching the
        // ctor alone leaves every slot of the minted instance a trap stub (on
        // wasm that trap is an unnamed call_indirect signature mismatch). The
        // constructor's caller dispatches the instance's USER-interface surface
        // next (a DI container's GetInterfaces → GetMethod → Invoke injection
        // pass), and that dispatch never records a used slot — it is a runtime
        // interface-table walk — so those impls are reached with it.
        if (ctorPreserved && !cls.IsValueType && !cls.IsAbstract && !cls.IsInterface
            && !cls.IsDelegate)
        {
            ReachAllocatedType(cls);
            ReachUserInterfaceImpls(cls);
        }
        foreach (var ph in policy.Properties)
            PreservePropertyType(cls, ph);
        foreach (var eh in policy.Events)
            PreserveEventType(cls, eh);
        if (conditional)
        {
            foreach (var ph in policy.ConditionalProperties) PreservePropertyType(cls, ph);
            foreach (var eh in policy.ConditionalEvents) PreserveEventType(cls, eh);
        }
    }

    private void PreservePropertyType(ClassInfo cls, PropertyDefinitionHandle handle)
    {
        var sig = cls.Module.Reader.GetPropertyDefinition(handle).DecodeSignature(SigProvider, cls.Context);
        NotePreservedType(sig.ReturnType);
        foreach (var type in sig.ParameterTypes) NotePreservedType(type);
    }

    private void PreserveEventType(ClassInfo cls, EventDefinitionHandle handle)
    {
        var ed = cls.Module.Reader.GetEventDefinition(handle);
        TypeDesc type = ed.Type.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeDescForDefinition(cls.Module, (TypeDefinitionHandle)ed.Type),
            HandleKind.TypeReference => ResolveTypeRef(cls.Module, (TypeReferenceHandle)ed.Type) ?? TypeDesc.MakeExternal("?"),
            HandleKind.TypeSpecification => cls.Module.Reader.GetTypeSpecification((TypeSpecificationHandle)ed.Type)
                .DecodeSignature(SigProvider, cls.Context),
            _ => TypeDesc.MakeExternal("?"),
        };
        NotePreservedType(type);
    }

    private void ApplyPreservationAfterShape(ClassInfo cls)
    {
        if (!_preservationSeedingActive
            || ContainsGenericVar(cls) // open shell — see ApplyPreservation
            || !_preservePolicies.TryGetValue((cls.Module.Index, cls.Handle), out var policy))
            return;
        if (cls.MembersReady)
        {
            ApplyPreservation(cls);
            return;
        }
        bool conditional = PreservedTypeWasOtherwiseUsed(cls);
        PreserveKind kind = policy.Kind | (conditional ? policy.ConditionalKind : PreserveKind.None);
        if ((kind & (PreserveKind.Methods | PreserveKind.DefaultConstructor)) != 0
            || policy.Methods.Count > 0 || conditional && policy.ConditionalMethods.Count > 0
            || cls.IsDelegate && kind != PreserveKind.None)
            CompleteMembers(cls);
        else
            ApplyPreservation(cls);
    }

    private bool PreservedTypeWasOtherwiseUsed(ClassInfo cls) =>
        ReferencedTypes.Contains(cls) || ForceEmittedClasses.Contains(cls)
        || IsAllocated(cls) || Reachable.Any(m => m.DeclaringClass == cls)
        || _reflectionRoots.Count > 0 && ReflectionRootMatching(cls) is not null;

    private bool ActivateConditionalPreservationPolicies()
    {
        if (!_preservationSeedingActive || _preservePolicies.Count == 0)
            return false;
        bool activated = false;
        foreach (var cls in Classes.ToList())
            if (!_activatedConditionalPolicies.Contains(cls)
                && _preservePolicies.TryGetValue((cls.Module.Index, cls.Handle), out var policy)
                && (policy.ConditionalKind != PreserveKind.None
                    || policy.ConditionalFields.Count > 0 || policy.ConditionalMethods.Count > 0
                    || policy.ConditionalProperties.Count > 0 || policy.ConditionalEvents.Count > 0)
                && PreservedTypeWasOtherwiseUsed(cls))
            {
                _activatedConditionalPolicies.Add(cls);
                if (cls.ShapeReady) ApplyPreservationAfterShape(cls);
                activated = true;
            }
        return activated;
    }

    private void ApplyPreservationToInstantiatedMethod(MethodInfo method)
    {
        if (!_preservationSeedingActive
            || !_preservePolicies.TryGetValue((method.DeclaringClass.Module.Index,
                method.DeclaringClass.Handle), out var policy))
            return;
        bool conditional = _activatedConditionalPolicies.Contains(method.DeclaringClass);
        PreserveKind kind = policy.Kind | (conditional ? policy.ConditionalKind : PreserveKind.None);
        if ((kind & PreserveKind.Methods) != 0 || policy.Methods.Contains(method.Handle)
            || conditional && policy.ConditionalMethods.Contains(method.Handle))
        {
            PreserveMethod(method);
            // Same allocation rule as ApplyPreservation: a preserved instance
            // ctor of a concrete reference instantiation is a late-bound
            // construction site.
            var cls = method.DeclaringClass;
            if (method.Name == ".ctor" && !method.IsStatic && method.Rva != 0
                && !cls.IsValueType && !cls.IsAbstract && !cls.IsInterface && !cls.IsDelegate)
                ReachAllocatedType(cls);
        }
    }

    private void PreserveField(FieldInfo field)
    {
        NoteForceEmit(field.DeclaringClass);
        NotePreservedType(field.Type);
        ReachCctor(field.DeclaringClass);
    }

    private void PreserveMethod(MethodInfo method)
    {
        NoteForceEmit(method.DeclaringClass);
        var sig = method.Signature;
        NotePreservedType(sig.ReturnType);
        foreach (var type in sig.ParameterTypes) NotePreservedType(type);
        Reach(method);
    }

    private void NotePreservedType(TypeDesc type)
    {
        switch (type.Kind)
        {
            case TypeKind.Class:
                NoteForceEmit(type.Class);
                if (type.Class is not null) _explicitReflectionKeep.Add(type.Class);
                break;
            case TypeKind.SZArray:
            case TypeKind.MDArray:
            case TypeKind.ByRef:
                if (type.Element is not null) NotePreservedType(type.Element);
                break;
            case TypeKind.ExternalGeneric:
                if (type.GenericArgs is not null)
                    foreach (var arg in type.GenericArgs) NotePreservedType(arg);
                break;
        }
        if (type.Kind == TypeKind.Class && type.Class is { } cls)
            foreach (var arg in cls.Context.TypeArgs) NotePreservedType(arg);
    }
}
