using Mono.Cecil;

internal static class DateTimeFixtureDeclaration
{
    public static bool IsCoreLibInstanceMethod(AssemblyDefinition core, MethodDefinition method, string ownerName, string name) =>
        IsCoreLibMethod(core, method, ownerName, name, valueTypeOwner: false, hasThis: true);

    public static bool IsCoreLibMethod(AssemblyDefinition core, MethodDefinition method, string ownerName, string name, bool valueTypeOwner, bool hasThis)
    {
        var module = core.MainModule;
        var owner = module.GetType(ownerName);
        // A MethodDef row number is not a declaration identity across runtime builds.
        return core.Name.FullName == "System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"
            && module.Name == "System.Private.CoreLib.dll" && ReferenceEquals(module.Assembly, core)
            && owner is not null && !owner.IsNested && !owner.IsInterface && !owner.IsEnum && owner.IsValueType == valueTypeOwner && !owner.HasGenericParameters
            && ReferenceEquals(method.Module, module) && ReferenceEquals(method.DeclaringType, owner) && owner.Methods.Contains(method) && method.Name == name
            && method.HasThis == hasThis && !method.ExplicitThis && method.CallingConvention == MethodCallingConvention.Default
            && IsCoreType(method.ReturnType, module) && method.Parameters.All(parameter => IsCoreType(parameter.ParameterType, module));
    }

    private static bool IsCoreType(TypeReference type, ModuleDefinition module)
    {
        if (type is ByReferenceType byReference) return IsCoreType(byReference.ElementType, module);
        if (type is ArrayType array) return array.IsVector && IsCoreType(array.ElementType, module);
        return type is TypeDefinition definition && ReferenceEquals(definition.Module, module)
            && ReferenceEquals(module.GetType(definition.FullName), definition) && !definition.HasGenericParameters;
    }
}
