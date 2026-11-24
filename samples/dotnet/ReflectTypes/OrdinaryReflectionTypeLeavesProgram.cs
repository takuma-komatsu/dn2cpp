using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
namespace OrdinaryReflectionTypeLeaves;
static class Program
{
    private sealed class AnnotatedFields<T, U> where T : class where U : class?
    {
        public string? Optional = null;
        [AllowNull] public T Required = default!;
        public U Loose = default!;
    }

    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== ordinary reflection type leaves ==");
        AttributeTypePropertySubset.Program.Run();
        ReflectAttrBoxedSubset.Program.Run();
        ReflectRuntimeTypeParitySubset.Program.Run();
        ReflectAssemblyErrorSubset.Program.Run();
        Console.WriteLine("ordinary reflection type leaves end");
        if (args.Length > 0 && args[0] == "before-attribute-chain")
            return;
        AttributeTypePropertySubset.ChainedTypes.Run();
        if (args.Length > 0 && args[0] == "before-nested-generic-names")
            return;
        NestedGenericTypeNameSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-property-accessors")
            return;
        PropertyAccessorRowsSubset.Run();
        if (args.Length > 0 && args[0] == "before-attribute-display-code-units")
            return;
        ReflectAttrBoxedSubset.Program.RunSurrogateDisplays();
        if (args.Length > 0 && args[0] == "before-exception-object-members")
            return;
        ReflectRuntimeTypeParitySubset.Program.RunExceptionMembers();
        if (args.Length > 0 && args[0] == "before-assembly-name-validation")
            return;
        ReflectAssemblyErrorSubset.Program.RunAssemblyNameValidation();
        if (args.Length > 0 && args[0] == "before-attribute-construction-order")
            return;
        AttributeConstructionOrderSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-array-object-types")
            return;
        ReflectRuntimeTypeParitySubset.Program.RunArrayObjectTypes();
        if (args.Length > 0 && args[0] == "before-method-attribute-inheritance")
            return;
        MethodAttributeInheritanceSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-metadata-definition-identity")
            return;
        MetadataDefinitionSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-collectibility")
            return;
        CollectibilitySubset.Program.Run();
        ReflectRuntimeTypeParitySubset.Program.RunAssemblyReferences();
        ReflectAttrBoxedSubset.Program.RunConstructorArguments();
        var context = new NullabilityInfoContext();
        var fields = typeof(AnnotatedFields<string, string>);
        var optional = context.Create(fields.GetField(nameof(AnnotatedFields<string, string>.Optional))!);
        var required = context.Create(fields.GetField(nameof(AnnotatedFields<string, string>.Required))!);
        var loose = context.Create(fields.GetField(nameof(AnnotatedFields<string, string>.Loose))!);
        Console.WriteLine("field nullability=" + optional.WriteState + "/" + required.ReadState + "/" + required.WriteState + "/" + loose.WriteState);
    }
}
