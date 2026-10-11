extern alias metadataAlias;
extern alias foreignConstraintAlias;

using System;
using System.Globalization;
using System.Reflection;
using TrimReflectLib;

internal static class FormalConstraintsOnlyProgram
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length > 0 && (args[0] == "generic-definition-relations-cold" || args[0] == "generic-definition-relations-warm"))
        {
            ReflectGenericMethodSubset.Program.RunGenericDefinitionRelations(args[0] == "generic-definition-relations-warm");
            return;
        }
        if (args.Length > 0 && args[0] == "generic-definition-members")
        {
            ReflectGenericMethodSubset.Program.RunGenericDefinitionMembers();
            return;
        }
        if (args.Length > 0 && args[0] == "open-generic-signatures")
        {
            ReflectGenericMethodSubset.Program.RunOpenSignatures();
            return;
        }
        if (args.Length > 0 && args[0] == "unloaded-open-signatures")
        {
            MethodInfo definition = typeof(ReflectGenericMethodSubset.DefinitionClass).GetMethod("Echo")!;
            Type formal = typeof(ForeignConstraint).GetMethod("M")!.GetGenericArguments()[0];
            Type shape = typeof(System.Collections.Generic.List<>).MakeGenericType(formal.MakeArrayType());
            MethodInfo method = definition.MakeGenericMethod(shape);
            Console.WriteLine("unloaded open signature identity: " + ReferenceEquals(method.ReturnType, shape) + "/"
                + ReferenceEquals(shape.GetGenericArguments()[0].GetElementType(), formal) + "/"
                + ReferenceEquals(method.GetGenericMethodDefinition(), definition));
            Console.WriteLine("unloaded open signature shape: " + shape.ContainsGenericParameters + "/"
                + shape.IsConstructedGenericType + "/" + (shape.FullName is null));
            return;
        }
        if (args.Length > 0 && args[0] == "formal-interface-order")
        {
            Type missing = typeof(ForeignConstraint).GetMethod("M")!.GetGenericArguments()[0];
            Console.WriteLine("constraint interface filter-null: " + Fault(() => missing.FindInterfaces(null!, null)));
            Type parameter = typeof(ReflectGenericMethodSubset.DefinitionSignatureOwner).GetMethod("InterfaceConstrained")!
                .GetGenericArguments()[0];
            TypeFilter all = static (type, criteria) => true;
            int before = parameter.FindInterfaces(all, null).Length;
            int relations = parameter.GetInterfaces().Length;
            int after = parameter.FindInterfaces(all, null).Length;
            Console.WriteLine("constraint filtered interfaces: " + before + "/" + relations + "/" + after);
            return;
        }
        if (args.Length > 0 && args[0] == "unloaded-self-assignability")
        {
            Type parameter = typeof(ForeignConstraint).GetMethod("M")!.GetGenericArguments()[0];
            Console.WriteLine("unloaded self assignability: " + Query(() => parameter.IsAssignableFrom(parameter))
                + "/" + Query(() => parameter.IsAssignableTo(parameter)));
            Console.WriteLine("unloaded object assignability: " + Query(() => typeof(object).IsAssignableFrom(parameter))
                + "/" + Query(() => parameter.IsAssignableTo(typeof(object))));
            Console.WriteLine("unloaded instance boundary: " + Query(() => parameter.IsInstanceOfType(new object())));
            Console.WriteLine("unloaded subclass boundary: " + Query(() => parameter.IsSubclassOf(parameter))
                + "/" + Query(() => parameter.IsSubclassOf(typeof(object))));
            Console.WriteLine("unloaded empty interface: " + Query(() => parameter.GetInterface("") is null));
            const BindingFlags declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            Console.WriteLine("unloaded declared members: " + Query(() => parameter.GetMethods(declared).Length == 0)
                + "/" + Query(() => parameter.GetFields(declared).Length == 0)
                + "/" + Query(() => parameter.GetProperties(declared).Length == 0));
            return;
        }
        if (args.Length > 0 && args[0] == "retained-formal-members")
        {
            Type parameter = typeof(RetainedConstraint).GetMethod("M")!.GetGenericArguments()[0];
            Console.WriteLine("retained inherited members: "
                + ReferenceEquals(parameter.GetField("Value")!.DeclaringType, typeof(RetainedConstraintBase)) + "/"
                + ReferenceEquals(parameter.GetProperty("Property")!.DeclaringType, typeof(RetainedConstraintBase)) + "/"
                + ReferenceEquals(parameter.GetMethod("Method")!.DeclaringType, typeof(RetainedConstraintBase)));
            Console.WriteLine("retained member counts: " + parameter.GetFields().Length + "/"
                + parameter.GetProperties().Length + "/" + parameter.GetMethods().Length);
            return;
        }
        if (args.Length > 0 && args[0] == "nested-assignability")
        {
            Type parameter = typeof(ReflectGenericMethodSubset.ConstraintOwner<ReflectGenericMethodSubset.ConstraintRoot>)
                .GetMethod("Composed")!.GetGenericArguments()[0];
            Type shape = parameter.GetGenericParameterConstraints()[1].GetGenericArguments()[0];
            bool before = typeof(ReflectGenericMethodSubset.ConstraintRoot[]).IsAssignableFrom(shape);
            bool element = ReferenceEquals(shape.GetElementType()!.BaseType, typeof(ReflectGenericMethodSubset.ConstraintRoot));
            bool after = typeof(ReflectGenericMethodSubset.ConstraintRoot[]).IsAssignableFrom(shape);
            Console.WriteLine("constraint nested assignability: " + before + "/" + element + "/" + after);
            return;
        }
        if (args.Length > 0 && args[0] == "unloaded-scoped-constraints")
        {
            Type[] parameters = typeof(ForeignConstraintShadow).GetMethod("M")!.GetGenericArguments();
            Console.WriteLine("unloaded scoped identity: " + parameters[0].Name + "/" + parameters[1].Name);
            for (int i = 0; i < 2; i++)
            {
                Console.WriteLine("unloaded scoped leaf: " + Fault(() => parameters[0].GetGenericParameterConstraints()));
                Console.WriteLine("unloaded scoped generic: " + Fault(() => parameters[1].GetGenericParameterConstraints()));
            }
            Type[] again = typeof(ForeignConstraintShadow).GetMethod("M")!.GetGenericArguments();
            Console.WriteLine("unloaded scoped stable: " + ReferenceEquals(parameters[0], again[0]) + "/" + ReferenceEquals(parameters[1], again[1]));
            return;
        }
        if (args.Length > 0 && args[0] == "scoped-constraint-targets")
        {
            Type[] parameters = typeof(ForeignConstraintShadow).GetMethod("M")!.GetGenericArguments();
            Console.WriteLine("scoped constraint targets: "
                + ReferenceEquals(parameters[0].GetGenericParameterConstraints()[0], typeof(ConstraintTwin.I)) + "/"
                + ReferenceEquals(parameters[1].GetGenericParameterConstraints()[0], typeof(ConstraintTwin.Base<int>)));
            return;
        }
        if (args.Length > 0 && args[0] == "nested-constraint")
        {
            ReflectGenericMethodSubset.Program.RunNestedConstraint();
            return;
        }
        if (args.Length > 0 && args[0] == "unloaded-method-identity")
        {
            Type parameter = typeof(ForeignConstraint).GetMethod("M")!.GetGenericArguments()[0];
            Console.WriteLine("unloaded method identity: " + parameter.Name + "/" + parameter.GenericParameterAttributes
                + "/" + ReferenceEquals(parameter, typeof(ForeignConstraint).GetMethod("M")!.GetGenericArguments()[0]));
            return;
        }
        if (args.Length > 0 && args[0] == "unloaded-relations")
        {
            Type parameter = typeof(ForeignConstraint).GetMethod("M")!.GetGenericArguments()[0];
            for (int i = 0; i < 2; i++)
                Console.WriteLine("unloaded constraint query: " + Fault(() => parameter.GetGenericParameterConstraints()));
            Console.WriteLine("unloaded constraint base: " + Fault(() => parameter.BaseType));
            Console.WriteLine("unloaded constraint interfaces: " + Fault(() => parameter.GetInterfaces()));
            return;
        }
        if (args.Length > 0 && args[0] == "constraint-scope")
        {
            FormalConstraintScope.Host.M<FormalConstraintScope.Base>();
            metadataAlias::FormalConstraintScope.Host.M<metadataAlias::FormalConstraintScope.Base>();
            Type app = typeof(FormalConstraintScope.Host).GetMethod("M")!.GetGenericArguments()[0];
            Type library = typeof(metadataAlias::FormalConstraintScope.Host).GetMethod("M")!.GetGenericArguments()[0];
            Console.WriteLine("constraint scope: " + ReferenceEquals(app.BaseType, typeof(FormalConstraintScope.Base))
                + "/" + ReferenceEquals(library.BaseType, typeof(metadataAlias::FormalConstraintScope.Base))
                + "/" + ReferenceEquals(app.GetInterfaces()[0], typeof(FormalConstraintScope.IApplication))
                + "/" + ReferenceEquals(library.GetInterfaces()[0], typeof(metadataAlias::FormalConstraintScope.ILibrary)));
            return;
        }
        if (args.Length > 0 && args[0] == "stripped-formal-constraints")
        {
            Type parameter = Factory.MakeBox().GetType().GetGenericTypeDefinition().GetGenericArguments()[0];
            for (int i = 0; i < 2; i++)
                Console.WriteLine("stripped constraint query: " + Fault(() => parameter.GetGenericParameterConstraints()));
            Console.WriteLine("stripped constraint base: " + Fault(() => parameter.BaseType));
            Console.WriteLine("stripped constraint interfaces: " + Fault(() => parameter.GetInterfaces()));
            return;
        }
        ReflectGenericMethodSubset.Program.RunConstraintMetadata();
    }

    private static string Fault(Func<object?> action)
    {
        try
        {
            action();
            return "none";
        }
        catch (Exception exception)
        {
            return exception.GetType().Name;
        }
    }

    private static string Query(Func<bool> action)
    {
        try
        {
            return action().ToString();
        }
        catch (Exception exception)
        {
            return exception.GetType().Name;
        }
    }
}

internal static class ForeignConstraint
{
    public static void M<U>() where U : foreignConstraintAlias::Ext.LayoutBase<int> { }
}

internal static class ForeignConstraintShadow
{
    public static void M<U, V>() where U : foreignConstraintAlias::ConstraintTwin.I
        where V : foreignConstraintAlias::ConstraintTwin.Base<int> { }
}

internal class RetainedConstraintBase
{
    public int Value = 0;
    public int Property { get; set; }
    public string Method() => "member";
}

internal static class RetainedConstraint
{
    public static void M<U>() where U : RetainedConstraintBase { }
}

namespace ConstraintTwin
{
    internal interface I { }
    internal class Base<T> { }
}

namespace FormalConstraintScope
{
    internal interface IApplication { }
    internal class Base : IApplication { }
    internal static class Host
    {
        public static void M<T>() where T : Base { }
    }
}
