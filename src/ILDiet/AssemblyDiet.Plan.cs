using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private enum MethodDecision : byte { Remove, Stub, Body }
    private sealed class TypePlan
    {
        internal required TypeDefinition Type;
        internal required bool Keep;
        internal readonly List<TypePlan> Nested = new();
        internal readonly List<(MethodDefinition Method, MethodDecision Decision)> Methods = new();
        internal readonly List<PropertyDefinition> RemovedProperties = new();
        internal readonly List<EventDefinition> RemovedEvents = new();
    }

    private sealed class DietPlan
    {
        internal readonly List<TypePlan> Types = new();
        internal readonly List<(Mono.Cecil.Cil.Instruction Instruction, MethodDefinition Target, DietAssembly Assembly)> Redirects = new();
        internal readonly List<(CustomAttribute Attribute, int Index, CustomAttributeArgument Argument, DietAssembly Assembly)> Registrations = new();
        internal int RemovedTypes, RemovedMethods;
    }

    private long _plannedStubs, _plannedRemovedMethods;

    private DietPlan BuildPlan()
    {
        var plan = new DietPlan();
        foreach (var assembly in _assemblies)
            if (!assembly.Copy)
                foreach (var type in assembly.Assembly.MainModule.Types) plan.Types.Add(PlanType(type, plan));
        if (_fallback != "none") return plan;
        var active = _registryFactories.Values.Where(f => _methods.Contains(f.Registry)).ToList();
        foreach (var pair in _registryFactories)
        {
            var factory = pair.Value;
            if (!_methods.Contains(factory.Registry)) continue;
            if (KeepsRegistryFactory(factory.Method)) { _registryKept++; continue; }
            var ancestor = FindAncestorFactory(factory, active)
                ?? throw new InvalidOperationException("constructor registry has no retained compatible ancestor factory");
            if (!KeepsRegistryFactory(ancestor.Method))
                throw new InvalidOperationException("constructor registry redirect target has no retained body");
            plan.Redirects.Add((pair.Key, ancestor.Method, _byModule[factory.Registry.Module]));
            _registryRedirected++;
        }
        foreach (var attribute in _registrationAttributes)
            for (int i = 0; i < attribute.ConstructorArguments.Count; i++)
            {
                var argument = attribute.ConstructorArguments[i];
                if (!IsTypeArray(argument)) continue;
                var values = (CustomAttributeArgument[])argument.Value;
                var kept = (CustomAttributeArgument[])EffectiveAttributeArgument(attribute, argument).Value;
                _registryScriptsKept += kept.Length;
                _registryScriptsFiltered += values.Length - kept.Length;
                if (kept.Length == values.Length) continue;
                var owner = _assemblies.First(a => a.Assembly.HasCustomAttributes && a.Assembly.CustomAttributes.Contains(attribute));
                plan.Registrations.Add((attribute, i, new CustomAttributeArgument(argument.Type, kept), owner));
            }
        PlanRegistryDiagnostics();
        return plan;
    }

    private TypePlan PlanType(TypeDefinition type, DietPlan plan)
    {
        bool keep = _types.Contains(type) || type.Name == "<Module>";
        var item = new TypePlan { Type = type, Keep = keep };
        if (!keep)
        {
            plan.RemovedTypes += AllTypes(new[] { type }).Count();
            int count = AllTypes(new[] { type }).Sum(t => t.Methods.Count);
            plan.RemovedMethods += count;
            _plannedRemovedMethods += count;
            return item;
        }
        foreach (var nested in type.NestedTypes) item.Nested.Add(PlanType(nested, plan));
        foreach (var method in type.Methods)
        {
            var decision = _methods.Contains(method) || _formationBodies.Contains(method) ? MethodDecision.Body
                : _signatureMethods.Contains(method) && (!(method.IsConstructor && method.IsStatic)
                    || _attributeInitializerProviders.Contains(method)) ? MethodDecision.Stub
                : MethodDecision.Remove;
            item.Methods.Add((method, decision));
            if (decision == MethodDecision.Remove) { plan.RemovedMethods++; _plannedRemovedMethods++; }
            if (decision == MethodDecision.Stub && method.HasBody) _plannedStubs++;
        }
        foreach (var property in type.Properties)
            if (!_properties.Contains(property)) item.RemovedProperties.Add(property);
        foreach (var itemEvent in type.Events)
            if (!_events.Contains(itemEvent)) item.RemovedEvents.Add(itemEvent);
        return item;
    }

    private void ValidatePlan(DietPlan plan)
    {
        if (_fallback == "none") ValidateFormationBodies(plan);
        // Copied providers keep their rows too, even when a row references a
        // constructor or named member in an assembly that is being rewritten.
        if (_fallback == "none")
            foreach (var row in _selectedAttributeRowsInOrder)
                if (row.RetentionRequested && ResolveMember(row.Attribute.Constructor) is MethodDefinition constructor)
                    ValidateAttributeMetadataRow(row.Attribute, row.Provider, constructor);
        foreach (var assembly in _assemblies)
            if (!assembly.Copy)
            {
                ValidateAttributeConstructors(assembly.Assembly);
                ValidateSecurity(assembly.Assembly);
                ValidateAttributeConstructors(assembly.Assembly.MainModule);
                foreach (var type in AllTypes(assembly.Assembly.MainModule.Types))
                {
                    if (!_types.Contains(type)) continue;
                    ValidateKeptType(type.DeclaringType);
                    ValidateKeptType(type.BaseType);
                    foreach (var implementation in type.Interfaces)
                    {
                        ValidateKeptType(implementation.InterfaceType);
                        ValidateAttributeConstructors(implementation);
                    }
                    foreach (var parameter in type.GenericParameters) ValidateParameter(parameter);
                    foreach (var field in type.Fields)
                    {
                        ValidateKeptType(field.FieldType);
                        ValidateAttributeConstructors(field);
                        ValidateMarshal(field);
                    }
                    foreach (var property in type.Properties.Where(p => _properties.Contains(p)))
                    {
                        ValidateKeptType(property.PropertyType);
                        ValidateAttributeConstructors(property);
                        if (!_rowOnlyProperties.Contains(property)
                            && !new[] { property.GetMethod, property.SetMethod }.Concat(property.OtherMethods)
                                .Any(m => m is not null && KeepsMethod(m)))
                            throw new InvalidOperationException("ILDiet plan removed every retained property accessor");
                        foreach (var parameter in property.Parameters)
                        {
                            ValidateKeptType(parameter.ParameterType);
                            ValidateAttributeConstructors(parameter);
                            ValidateMarshal(parameter);
                        }
                    }
                    foreach (var item in type.Events.Where(e => _events.Contains(e)))
                    {
                        ValidateKeptType(item.EventType);
                        ValidateAttributeConstructors(item);
                        ValidateAccessors(new[] { item.AddMethod, item.RemoveMethod, item.InvokeMethod }.Concat(item.OtherMethods));
                    }
                    foreach (var method in type.Methods)
                    {
                        if (!KeepsMethod(method)) continue;
                        if (method.IsConstructor && method.IsStatic && !_methods.Contains(method)
                            && !_formationBodies.Contains(method) && !_attributeInitializerProviders.Contains(method))
                            throw new InvalidOperationException("ILDiet plan would stub a static initializer");
                        ValidateKeptType(method.ReturnType);
                        ValidateAttributeConstructors(method.MethodReturnType);
                        ValidateMarshal(method.MethodReturnType);
                        ValidateSecurity(method);
                        foreach (var parameter in method.Parameters)
                        {
                            ValidateKeptType(parameter.ParameterType);
                            ValidateAttributeConstructors(parameter);
                            ValidateMarshal(parameter);
                        }
                        foreach (var parameter in method.GenericParameters) ValidateParameter(parameter);
                        foreach (var overridden in method.Overrides)
                            if (ResolveMember(overridden) is MethodDefinition target
                                && IsStripped(target.DeclaringType) && !KeepsMethod(target))
                                throw new InvalidOperationException("ILDiet plan removed an override target");
                    }
                    ValidateAttributeConstructors(type);
                    ValidateSecurity(type);
                    foreach (var method in type.Methods.Where(KeepsMethod)) ValidateAttributeConstructors(method);
                }
            }
    }

    private void ValidateParameter(GenericParameter parameter)
    {
        ValidateAttributeConstructors(parameter);
        foreach (var constraint in parameter.Constraints)
        {
            ValidateKeptType(constraint.ConstraintType);
            ValidateAttributeConstructors(constraint);
        }
    }

    private void ValidateAccessors(IEnumerable<MethodDefinition?> accessors)
    {
        var methods = accessors.Where(m => m is not null).ToArray();
        if (methods.Length == 0 || methods.Any(m => !KeepsMethod(m!)))
            throw new InvalidOperationException("ILDiet plan removed a retained member accessor");
    }

    private void ValidateMarshal(IMarshalInfoProvider provider)
    {
        if (provider.HasMarshalInfo && provider.MarshalInfo is CustomMarshalInfo custom)
            ValidateKeptType(custom.ManagedType);
    }

    private void ValidateSecurity(ISecurityDeclarationProvider provider)
    {
        if (!provider.HasSecurityDeclarations) return;
        foreach (var declaration in provider.SecurityDeclarations)
            foreach (var attribute in declaration.SecurityAttributes) ValidateKeptType(attribute.AttributeType);
    }

    private void ValidateKeptType(TypeReference? reference)
    {
        if (reference is null || reference is GenericParameter) return;
        if (reference is GenericInstanceType generic)
            foreach (var argument in generic.GenericArguments) ValidateKeptType(argument);
        if (reference is IModifierType modifier) ValidateKeptType(modifier.ModifierType);
        if (reference is TypeSpecification specification) { ValidateKeptType(specification.ElementType); return; }
        if (Resolve(reference) is { } type && IsStripped(type) && !_types.Contains(type))
            throw new InvalidOperationException("ILDiet plan removed a required type: " + type.FullName);
    }

    private void ValidateAttributeConstructors(ICustomAttributeProvider provider)
    {
        if (!provider.HasCustomAttributes) return;
        foreach (var attribute in provider.CustomAttributes)
            if (ResolveMember(attribute.Constructor) is MethodDefinition method
                && !ValidateAttributeMetadataRow(attribute, provider, method)
                && !ValidateFormationAttribute(attribute, method)
                && IsStripped(method.DeclaringType) && !_methods.Contains(method) && !KeepsCutMethod(method))
                throw new InvalidOperationException("ILDiet plan removed an attribute constructor");
    }

    private void ApplyPlan(DietPlan plan)
    {
        foreach (var item in plan.Types) ApplyTypePlan(item);
        foreach (var redirect in plan.Redirects)
        {
            redirect.Instruction.Operand = redirect.Target;
            redirect.Assembly.RegistryRewritten = true;
        }
        foreach (var registration in plan.Registrations)
        {
            registration.Attribute.ConstructorArguments[registration.Index] = registration.Argument;
            registration.Assembly.RegistryRewritten = true;
        }
        _constructorRegistriesRewritten = _assemblies.Any(a => a.RegistryRewritten);
    }

    private void ApplyTypePlan(TypePlan item)
    {
        if (!item.Keep)
        {
            if (item.Type.DeclaringType is { } parent) parent.NestedTypes.Remove(item.Type);
            else item.Type.Module.Types.Remove(item.Type);
            return;
        }
        foreach (var nested in item.Nested) ApplyTypePlan(nested);
        foreach (var method in item.Methods)
        {
            if (method.Decision == MethodDecision.Remove) item.Type.Methods.Remove(method.Method);
            else if (method.Decision == MethodDecision.Stub && method.Method.HasBody) StubBody(method.Method);
        }
        foreach (var property in item.RemovedProperties) item.Type.Properties.Remove(property);
        foreach (var itemEvent in item.RemovedEvents) item.Type.Events.Remove(itemEvent);
        foreach (var property in item.Type.Properties)
        {
            if (property.GetMethod is not null && !KeepsMethod(property.GetMethod)) property.GetMethod = null;
            if (property.SetMethod is not null && !KeepsMethod(property.SetMethod)) property.SetMethod = null;
            for (int i = property.OtherMethods.Count - 1; i >= 0; i--)
                if (!KeepsMethod(property.OtherMethods[i])) property.OtherMethods.RemoveAt(i);
        }
    }
}
