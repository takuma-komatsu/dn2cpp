using Mono.Cecil;
using Mono.Cecil.Cil;

static class GraphUncertaintyValidation
{
    static readonly string[] Families = { "lookup-name", "lookup-flags", "base", "generic-definition", "array", "element", "accessor", "array-rank", "object", "type-name", "interface", "object-generic", "box-class", "box-value", "field-array", "field-nested", "field-bound-array", "field-bound-nested", "method-array", "canonical-array", "canonical-made-array", "canonical-generic", "canonical-definition", "contextual-method-array", "contextual-field-array", "contextual-own-array", "template-array", "template-name-array", "template-base", "template-default-members" };
    static readonly string[] ViewFamilies = { "tuple-fixed", "tuple-index", "tuple-owner", "view-field", "view-property", "view-parameter", "view-accessor", "template-view-return", "template-view-parameter", "interface-open-enumeration", "interface-open-named", "interface-fixed-enumeration", "interface-fixed-named" };
    static readonly string[] BoundFamilies = { "tuple-object", "tuple-method-closure", "tuple-method-owner" };
    static readonly string[] OriginFamilies = { "boxed-vector", "null-vector", "definition-element", "primitive-loadset", "modifier-loadset" };

    internal static bool Run(string[] args)
    {
        if (args.Length == 4 && args[0] == "--create-uncertainty-fixtures")
        {
            Parse(args[3]); Create(args[1], args[2], args[3]);
            Console.WriteLine("fixture-created=reflection-uncertainty mode=" + args[3]);
            return true;
        }
        if (args.Length == 4 && args[0] == "--check-uncertainty-fixtures")
        {
            Parse(args[2]); Require(args[3] is "original" or "published", "unknown uncertainty fixture state");
            Check(args[1], args[2], args[3]);
            return true;
        }
        return false;
    }

    static (string Family, bool Unknown) Parse(string mode)
    {
        int separator = mode.LastIndexOf('-');
        Require(separator > 0, "unknown uncertainty fixture mode");
        string family = mode[..separator]; string variant = mode[(separator + 1)..];
        Require((Families.Contains(family) || ViewFamilies.Contains(family) || BoundFamilies.Contains(family) || OriginFamilies.Contains(family)) && (variant is "unknown" or "complete"), "unknown uncertainty fixture mode");
        Require(family != "box-value" || variant == "complete", "value boxing is an exact-owner control");
        Require(!family.StartsWith("field-bound-", StringComparison.Ordinal) || variant == "complete", "closed generic owners are complete-owner controls");
        Require(family != "tuple-owner" && !family.StartsWith("interface-fixed-", StringComparison.Ordinal) || variant == "complete", "direct template and fixed interface owners are complete controls");
        Require(family != "tuple-method-owner" || variant == "complete", "direct template method attributes are a complete control");
        Require(family != "null-vector" || variant == "complete", "null argument arrays are complete controls");
        return (family, variant == "unknown");
    }

    static void Create(string directory, string corelib, string mode)
    {
        var (family, unknown) = Parse(mode); Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)), "GraphApp", ModuleKind.Console);
        using var library = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)), "GraphLib", ModuleKind.Dll);
        var module = app.MainModule; var lib = library.MainModule;
        TypeReference Core(ModuleDefinition target, string name) => target.ImportReference(core.MainModule.GetType("System." + name));
        MethodReference Api(ModuleDefinition target, string owner, string name, params string[] parameters) => target.ImportReference(core.MainModule.GetType("System." + owner).Methods.Single(m => m.IsPublic && m.Name == name
            && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters)));
        TypeDefinition Type(ModuleDefinition target, string name, TypeReference? parent = null)
        {
            var type = new TypeDefinition("Graph", name, TypeAttributes.Public | TypeAttributes.Class, parent ?? Core(target, "Object")); target.Types.Add(type); return type;
        }
        MethodDefinition Method(TypeDefinition owner, string name, TypeReference? result = null, MethodAttributes flags = MethodAttributes.Public | MethodAttributes.Static)
        {
            var method = new MethodDefinition(name, flags, result ?? Core(owner.Module, "Void")); owner.Methods.Add(method); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); return method;
        }
        MethodDefinition Dependency(string name) => Method(Type(lib, name), "Touch");
        var marker = Type(lib, "UncertaintyMarkerAttribute", Core(lib, "Attribute"));
        var markerCtor = Method(marker, ".ctor", flags: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        markerCtor.Parameters.Add(new ParameterDefinition(Core(lib, "Int32"))); markerCtor.Body.Instructions.Clear();
        markerCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); markerCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, lib.ImportReference(core.MainModule.GetType("System.Attribute").Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0))));
        markerCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Dependency("UncertaintyCtorDependency"))); markerCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var setter = Method(marker, "set_Label", flags: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig);
        setter.Parameters.Add(new ParameterDefinition(Core(lib, "String"))); setter.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Dependency("UncertaintySetterDependency")));
        var getter = Method(marker, "get_Label", Core(lib, "String"), MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig);
        getter.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldstr, "label"));
        marker.Properties.Add(new PropertyDefinition("Label", PropertyAttributes.None, Core(lib, "String")) { GetMethod = getter, SetMethod = setter });
        var host = Type(lib, "UncertaintyHost"); var touch = Method(host, "Touch"); var annotated = Method(host, "Annotated");
        annotated.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Dependency("UncertaintyAnnotatedDependency")));
        var attribute = new CustomAttribute(markerCtor); attribute.ConstructorArguments.Add(new CustomAttributeArgument(Core(lib, "Int32"), 1));
        attribute.Properties.Add(new CustomAttributeNamedArgument("Label", new CustomAttributeArgument(Core(lib, "String"), "selected"))); annotated.CustomAttributes.Add(attribute);
        var known = Type(module, "KnownOwner");
        if (family == "interface")
        {
            var contract = new TypeDefinition("Graph", "IÅ", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract, null); module.Types.Add(contract);
            var abstractMethodFlags = MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            contract.Methods.Add(new MethodDefinition("Available", abstractMethodFlags, Core(module, "Void")));
            known.IsAbstract = true; known.Methods.Add(new MethodDefinition("Available", abstractMethodFlags, Core(module, "Void")));
            known.Interfaces.Add(new InterfaceImplementation(contract));
        }
        else Method(known, "Available");
        var derived = Type(module, "KnownDerived", known);
        var generic = Type(module, "KnownGeneric`1"); generic.GenericParameters.Add(new GenericParameter("T", generic));
        var closed = new GenericInstanceType(generic); closed.GenericArguments.Add(Core(module, "Int32"));
        var propertyOwner = Type(module, "KnownPropertyOwner"); var value = Method(propertyOwner, "get_Value", Core(module, "Int32"), MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName);
        value.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_0)); propertyOwner.Properties.Add(new PropertyDefinition("Value", PropertyAttributes.None, Core(module, "Int32")) { GetMethod = value });
        var driver = Type(module, family == "contextual-own-array" ? "UncertaintyDriver`1" : "UncertaintyDriver");
        if (family == "contextual-own-array") driver.GenericParameters.Add(new GenericParameter("T", driver));
        var helper = Method(driver, "Read"); helper.Body.Instructions.Clear();
        var entry = Type(module, "Entry"); var main = Method(entry, "Main"); app.EntryPoint = main; main.Body.Instructions.Clear();
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, module.ImportReference(touch)));
        void TypeValue(MethodDefinition method, TypeReference owner)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, owner)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(module, "Type", "GetTypeFromHandle", "System.RuntimeTypeHandle")));
        }
        void PropertyValue(MethodDefinition method)
        {
            TypeValue(method, propertyOwner); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Value")); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 24));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "GetProperty", "System.String", "System.Reflection.BindingFlags")));
        }
        TypeDefinition EmptyOwner(string name)
        {
            var owner = Type(module, name); owner.GenericParameters.Add(new GenericParameter("T", owner));
            var ctor = Method(owner, ".ctor", flags: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            ctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldarg_0)); ctor.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Call, Api(module, "Object", ".ctor"))); return owner;
        }
        void MadeOwner(TypeDefinition owner, TypeReference argument)
        {
            TypeValue(helper, owner); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(module, "Type")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); TypeValue(helper, argument); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "MakeGenericType", "System.Type[]")));
        }
        void KnownSource(MethodDefinition method)
        {
            if (family == "accessor") PropertyValue(method);
            else TypeValue(method, family == "base" ? derived : family == "generic-definition" ? closed : family == "element" ? new ArrayType(known) : known);
        }
        bool canonical = family.StartsWith("canonical-", StringComparison.Ordinal);
        bool contextual = family.StartsWith("contextual-", StringComparison.Ordinal);
        bool template = family.StartsWith("template-", StringComparison.Ordinal);
        bool tuple = family.StartsWith("tuple-", StringComparison.Ordinal);
        bool view = ViewFamilies.Contains(family);
        bool boundFixture = BoundFamilies.Contains(family);
        bool origin = OriginFamilies.Contains(family);
        bool mixedProjection = family is "base" or "generic-definition" or "array" or "element" or "accessor";
        if (family is "primitive-loadset" or "modifier-loadset")
        {
            TypeReference signature = Core(module, "Int32");
            if (family == "modifier-loadset") signature = new RequiredModifierType(Core(module, "Runtime.CompilerServices.IsVolatile"), signature);
            known.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, signature));
            TypeValue(helper, known); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Value"));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "GetField", "System.String")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Reflection.FieldInfo", "get_FieldType")));
        }
        else if (family == "definition-element")
        {
            var owner = EmptyOwner("DefinitionTupleOwner`1"); var argument = EmptyOwner("DefinitionArgument`1");
            TypeReference element = argument;
            if (!unknown)
            {
                var instance = new GenericInstanceType(argument); instance.GenericArguments.Add(Core(module, "Int32")); element = instance;
            }
            MadeOwner(owner, element);
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "GetGenericArguments")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
        }
        else if (origin)
        {
            var owner = EmptyOwner("VectorOwner`1"); TypeValue(helper, owner);
            if (family == "null-vector") helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            else
            {
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(module, "Type")));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); TypeValue(helper, Core(module, "String")); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
                if (unknown) helper.Body.Instructions.Add(Instruction.Create(OpCodes.Box, new ArrayType(Core(module, "Type"))));
            }
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "MakeGenericType", "System.Type[]")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "GetGenericArguments")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "MakeArrayType")));
        }
        else if (family == "tuple-object")
        {
            var owner = EmptyOwner("ObjectTupleOwner`1");
            TypeValue(helper, owner); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 1)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(module, "Type")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
            TypeValue(helper, Core(module, "String"));
            if (unknown) helper.Body.Instructions.Add(Instruction.Create(OpCodes.Box, Core(module, "Type")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "MakeGenericType", "System.Type[]")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "GetGenericArguments")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "MakeArrayType")));
        }
        else if (boundFixture)
        {
            var owner = EmptyOwner("MethodTupleOwner`1"); var genericMethod = Method(owner, "G"); genericMethod.GenericParameters.Add(new GenericParameter("U", genericMethod));
            var parameter = new GenericParameter("T", helper); helper.GenericParameters.Add(parameter);
            MadeOwner(owner, new ArrayType(unknown || family == "tuple-method-owner" ? parameter : Core(module, "String")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "G")); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "GetMethod", "System.String")));
            if (family == "tuple-method-closure")
            {
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(module, "Type")));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); TypeValue(helper, Core(module, "Int32")); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Reflection.MethodInfo", "MakeGenericMethod", "System.Type[]")));
            }
        }
        else if (tuple)
        {
            var owner = EmptyOwner("TupleOwner`1"); var parameter = new GenericParameter("T", helper); helper.GenericParameters.Add(parameter);
            MadeOwner(owner, new ArrayType(unknown || family == "tuple-owner" ? parameter : Core(module, "String")));
            if (family != "tuple-owner")
            {
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "GetGenericArguments")));
                if (family == "tuple-index")
                {
                    helper.Parameters.Add(new ParameterDefinition(Core(module, "Int32"))); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                }
                else helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
            }
        }
        else if (family.StartsWith("view-", StringComparison.Ordinal) || family.StartsWith("template-view-", StringComparison.Ordinal))
        {
            bool made = family.StartsWith("template-view-", StringComparison.Ordinal);
            var wrapper = EmptyOwner("ViewWrapper`1");
            var owner = made ? EmptyOwner("ViewOwner`1") : Type(module, unknown ? "ViewOpen`1" : "ViewClosed");
            if (!made && unknown) owner.GenericParameters.Add(new GenericParameter("T", owner));
            TypeReference memberType;
            if (made)
            {
                var array = new ArrayType(owner.GenericParameters[0]); memberType = array;
                if (unknown) { var instance = new GenericInstanceType(wrapper); instance.GenericArguments.Add(array); memberType = instance; }
            }
            else { var instance = new GenericInstanceType(wrapper); instance.GenericArguments.Add(Core(module, "Int32")); memberType = instance; }
            bool parameterView = family is "view-parameter" or "template-view-parameter";
            bool propertyView = family is "view-property" or "view-accessor";
            if (family == "view-field") owner.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, memberType));
            else if (propertyView)
            {
                var accessor = Method(owner, "get_Value", memberType, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName);
                accessor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldnull)); owner.Properties.Add(new PropertyDefinition("Value", PropertyAttributes.None, memberType) { GetMethod = accessor });
            }
            else
            {
                var method = Method(owner, "Project", parameterView ? Core(module, "Void") : memberType);
                if (parameterView) method.Parameters.Add(new ParameterDefinition(memberType)); else method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldnull));
            }
            if (made && !unknown)
            {
                var compiled = new GenericInstanceType(owner); compiled.GenericArguments.Add(Core(module, "String"));
                TypeValue(main, compiled); main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            }
            if (made) MadeOwner(owner, Core(module, "String")); else TypeValue(helper, owner);
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, family == "view-field" || propertyView ? "Value" : "Project"));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", family == "view-field" ? "GetField" : propertyView ? "GetProperty" : "GetMethod", "System.String")));
            if (family == "view-field") helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Reflection.FieldInfo", "get_FieldType")));
            else if (family == "view-property") helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Reflection.PropertyInfo", "get_PropertyType")));
            else
            {
                if (family == "view-accessor") helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Reflection.PropertyInfo", "GetGetMethod")));
                if (parameterView)
                {
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Reflection.MethodBase", "GetParameters")));
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Reflection.ParameterInfo", "get_ParameterType")));
                }
                else helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Reflection.MethodInfo", "get_ReturnType")));
            }
        }
        else if (view)
        {
            bool fixedInterface = family.StartsWith("interface-fixed-", StringComparison.Ordinal);
            var contract = new TypeDefinition("Graph", "IView`1", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract, null);
            contract.GenericParameters.Add(new GenericParameter("T", contract)); module.Types.Add(contract);
            var owner = Type(module, "InterfaceOpen`1"); owner.IsAbstract = true; owner.GenericParameters.Add(new GenericParameter("T", owner));
            var instance = new GenericInstanceType(contract); instance.GenericArguments.Add(fixedInterface ? Core(module, "String") : owner.GenericParameters[0]); owner.Interfaces.Add(new InterfaceImplementation(instance));
            TypeReference query = owner;
            if (!unknown && !fixedInterface) { var closedOwner = new GenericInstanceType(owner); closedOwner.GenericArguments.Add(Core(module, "String")); query = closedOwner; }
            TypeValue(helper, query);
            if (family.EndsWith("named", StringComparison.Ordinal))
            {
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "IView`1")); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "GetInterface", "System.String")));
            }
            else
            {
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "GetInterfaces")));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
            }
        }
        else if (contextual)
        {
            GenericParameter callerParameter;
            if (family == "contextual-own-array") callerParameter = driver.GenericParameters[0];
            else { callerParameter = new GenericParameter("T", helper); helper.GenericParameters.Add(callerParameter); }
            TypeReference argument = unknown ? callerParameter : Core(module, "String");
            if (family == "contextual-field-array")
            {
                var owner = Type(module, "ContextFieldOwner`1"); var parameter = new GenericParameter("U", owner); owner.GenericParameters.Add(parameter);
                var field = new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, new ArrayType(parameter)); owner.Fields.Add(field);
                var instance = new GenericInstanceType(owner); instance.GenericArguments.Add(argument);
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, new FieldReference(field.Name, field.FieldType, instance)));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, instance));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(module, "Reflection.FieldInfo", "GetFieldFromHandle", "System.RuntimeFieldHandle", "System.RuntimeTypeHandle")));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Reflection.FieldInfo", "get_FieldType")));
            }
            else
            {
                var owner = family == "contextual-own-array" ? driver : Type(module, "ContextMethodOwner");
                var method = Method(owner, "ReturnArray");
                GenericParameter parameter;
                if (family == "contextual-own-array") parameter = callerParameter;
                else { parameter = new GenericParameter("U", method); method.GenericParameters.Add(parameter); }
                method.ReturnType = new ArrayType(family == "contextual-own-array" ? argument : parameter);
                method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldnull));
                MethodReference token = method;
                if (family == "contextual-method-array")
                {
                    var instance = new GenericInstanceMethod(method); instance.GenericArguments.Add(argument); token = instance;
                }
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, token));
                if (family == "contextual-own-array")
                {
                    var instance = new GenericInstanceType(driver); instance.GenericArguments.Add(callerParameter);
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, instance));
                }
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Call, family == "contextual-own-array"
                    ? Api(module, "Reflection.MethodBase", "GetMethodFromHandle", "System.RuntimeMethodHandle", "System.RuntimeTypeHandle")
                    : Api(module, "Reflection.MethodBase", "GetMethodFromHandle", "System.RuntimeMethodHandle")));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Castclass, Core(module, "Reflection.MethodInfo")));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Reflection.MethodInfo", "get_ReturnType")));
            }
        }
        else if (template)
        {
            var owner = Type(module, "EmptyGeneric`1"); owner.GenericParameters.Add(new GenericParameter("T", owner));
            MethodDefinition Constructor(TypeDefinition type, MethodReference parent)
            {
                var ctor = Method(type, ".ctor", flags: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
                ctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldarg_0)); ctor.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Call, parent)); return ctor;
            }
            Constructor(owner, Api(module, "Object", ".ctor"));
            if (family == "template-base")
            {
                var child = Type(module, "EmptyDerived`1"); var parameter = new GenericParameter("T", child); child.GenericParameters.Add(parameter);
                var parent = new GenericInstanceType(owner); parent.GenericArguments.Add(parameter); child.BaseType = parent;
                Constructor(child, new MethodReference(".ctor", Core(module, "Void"), parent) { HasThis = true }); owner = child;
            }
            if (family == "template-default-members")
            {
                Method(owner, "Available");
                var defaultMember = new CustomAttribute(Api(module, "Reflection.DefaultMemberAttribute", ".ctor", "System.String"));
                defaultMember.ConstructorArguments.Add(new CustomAttributeArgument(Core(module, "String"), "Available")); owner.CustomAttributes.Add(defaultMember);
            }
            if (!unknown)
            {
                var instance = new GenericInstanceType(owner); instance.GenericArguments.Add(Core(module, "String")); TypeValue(helper, instance);
            }
            else if (family == "template-name-array")
            {
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Graph.EmptyGeneric`1[[System.String, " + core.Name.Name + "]], GraphApp"));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(module, "Type", "GetType", "System.String")));
            }
            else
            {
                TypeValue(helper, owner); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(module, "Type")));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); TypeValue(helper, Core(module, "String")); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "MakeGenericType", "System.Type[]")));
            }
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, family == "template-base" ? Api(module, "Type", "get_BaseType")
                : family == "template-default-members" ? Api(module, "Type", "GetDefaultMembers") : Api(module, "Type", "MakeArrayType")));
            if (family == "template-default-members")
            {
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
            }
        }
        else if (canonical)
        {
            var parameter = new GenericParameter("T", helper); helper.GenericParameters.Add(parameter);
            TypeReference argument = unknown ? parameter : Core(module, "String");
            if (family == "canonical-array") TypeValue(helper, new ArrayType(argument));
            else if (family == "canonical-made-array")
            {
                TypeValue(helper, argument); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "MakeArrayType")));
            }
            else
            {
                var instance = new GenericInstanceType(generic); instance.GenericArguments.Add(argument); TypeValue(helper, instance);
                if (family == "canonical-definition") helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "GetGenericTypeDefinition")));
            }
        }
        else if (mixedProjection)
        {
            if (unknown)
            {
                var input = Core(module, family == "accessor" ? "Reflection.PropertyInfo" : "Type");
                helper.Parameters.Add(new ParameterDefinition(Core(module, "Boolean"))); helper.Parameters.Add(new ParameterDefinition(input));
                helper.Body.InitLocals = true; helper.Body.Variables.Add(new VariableDefinition(input));
                var knownBranch = Instruction.Create(OpCodes.Nop); var join = Instruction.Create(OpCodes.Nop);
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Brtrue, knownBranch));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Br, join));
                helper.Body.Instructions.Add(knownBranch); KnownSource(helper); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0)); helper.Body.Instructions.Add(join); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); KnownSource(main);
            }
            else KnownSource(helper);
            var projection = family switch
            {
                "base" => Api(module, "Type", "get_BaseType"),
                "generic-definition" => Api(module, "Type", "GetGenericTypeDefinition"),
                "array" => Api(module, "Type", "MakeArrayType"),
                "element" => Api(module, "Type", "GetElementType"),
                _ => Api(module, "Reflection.PropertyInfo", "GetGetMethod"),
            };
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, projection));
        }
        else if (family is "lookup-name" or "lookup-flags")
        {
            TypeValue(helper, known);
            if (unknown)
            {
                helper.Parameters.Add(new ParameterDefinition(Core(module, family == "lookup-name" ? "String" : "Reflection.BindingFlags")));
                if (family == "lookup-flags") helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Available"));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                main.Body.Instructions.Add(family == "lookup-name" ? Instruction.Create(OpCodes.Ldstr, "Available") : Instruction.Create(OpCodes.Ldc_I4, 24));
            }
            else
            {
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Available"));
                if (family == "lookup-flags") helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 24));
            }
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, family == "lookup-name" ? Api(module, "Type", "GetMethod", "System.String")
                : Api(module, "Type", "GetMethod", "System.String", "System.Reflection.BindingFlags")));
        }
        else if (family == "array-rank")
        {
            TypeValue(helper, known);
            if (unknown)
            {
                helper.Parameters.Add(new ParameterDefinition(Core(module, "Int32"))); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_2));
            }
            else helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_2));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "MakeArrayType", "System.Int32")));
        }
        else if (family == "object")
        {
            TypeValue(helper, known);
            if (unknown) helper.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, Api(module, "Reflection.TypeDelegator", ".ctor", "System.Type")));
        }
        else if (family == "object-generic")
        {
            TypeValue(helper, generic);
            if (unknown) helper.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, Api(module, "Reflection.TypeDelegator", ".ctor", "System.Type")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(module, "Type")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); TypeValue(helper, Core(module, "Int32")); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "MakeGenericType", "System.Type[]")));
        }
        else if (family is "box-class" or "box-value")
        {
            bool valueType = family == "box-value";
            var boxOwner = Type(module, "KnownBox`1", valueType ? Core(module, "ValueType") : null);
            boxOwner.GenericParameters.Add(new GenericParameter("T", boxOwner)); boxOwner.IsSealed = valueType || !unknown;
            if (valueType) boxOwner.IsSequentialLayout = true;
            var boxType = new GenericInstanceType(boxOwner); boxType.GenericArguments.Add(Core(module, "Int32"));
            if (valueType)
            {
                var field = new FieldDefinition("DefaultBox", FieldAttributes.Public | FieldAttributes.Static, boxType); driver.Fields.Add(field);
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, field));
            }
            else
            {
                var ctor = Method(boxOwner, ".ctor", flags: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
                ctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldarg_0)); ctor.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Call, module.ImportReference(core.MainModule.GetType("System.Object").Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0))));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, new MethodReference(".ctor", Core(module, "Void"), boxType) { HasThis = true }));
            }
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Box, boxType)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Object", "GetType")));
        }
        else if (family is "field-array" or "field-nested" or "field-bound-array" or "field-bound-nested")
        {
            bool bound = family.StartsWith("field-bound-", StringComparison.Ordinal);
            var owner = Type(module, unknown || bound ? "OpenOwner`1" : "ClosedOwner"); TypeReference element = Core(module, "Int32");
            if (unknown || bound)
            {
                var parameter = new GenericParameter("T", owner); owner.GenericParameters.Add(parameter); element = parameter;
            }
            TypeReference fieldType = new ArrayType(element);
            if (family.EndsWith("nested", StringComparison.Ordinal))
            {
                var wrapper = Type(module, "KnownWrapper`1"); wrapper.GenericParameters.Add(new GenericParameter("T", wrapper));
                var instance = new GenericInstanceType(wrapper); instance.GenericArguments.Add(fieldType); fieldType = new ArrayType(instance);
            }
            owner.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, fieldType));
            TypeReference queryOwner = owner;
            if (bound)
            {
                var instance = new GenericInstanceType(owner); instance.GenericArguments.Add(Core(module, "Int32")); queryOwner = instance;
            }
            TypeValue(helper, queryOwner); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Value"));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "GetField", "System.String")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Reflection.FieldInfo", "get_FieldType")));
        }
        else if (family == "method-array")
        {
            var owner = Type(module, "KnownMethodOwner"); var method = Method(owner, "ReturnArray");
            var parameter = new GenericParameter("T", method); method.GenericParameters.Add(parameter); method.ReturnType = new ArrayType(parameter);
            method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldnull));
            MethodReference token = method;
            if (!unknown)
            {
                var instance = new GenericInstanceMethod(method); instance.GenericArguments.Add(Core(module, "Int32")); token = instance;
            }
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, token));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(module, "Reflection.MethodBase", "GetMethodFromHandle", "System.RuntimeMethodHandle")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Castclass, Core(module, "Reflection.MethodInfo")));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Reflection.MethodInfo", "get_ReturnType")));
        }
        else if (family == "type-name")
        {
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, unknown ? "Graph.KnownOwner" : "Graph.KnownOwner, GraphApp"));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); helper.Body.Instructions.Add(Instruction.Create(unknown ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(module, "Type", "GetType", "System.String", "System.Boolean", "System.Boolean")));
        }
        else
        {
            TypeValue(helper, known); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Graph.IÅ"));
            helper.Body.Instructions.Add(Instruction.Create(unknown ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Type", "GetInterface", "System.String", "System.Boolean")));
        }
        TypeValue(helper, module.ImportReference(marker)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(module, "Reflection.MemberInfo", "GetCustomAttributes", "System.Type", "System.Boolean")));
        helper.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        MethodReference call = helper;
        if (canonical || tuple && (!boundFixture || family is "tuple-method-closure" or "tuple-method-owner") || contextual && family != "contextual-own-array")
        {
            var instance = new GenericInstanceMethod(helper); instance.GenericArguments.Add(Core(module, "String")); call = instance;
        }
        else if (family == "contextual-own-array")
        {
            var instance = new GenericInstanceType(driver); instance.GenericArguments.Add(Core(module, "String"));
            call = new MethodReference(helper.Name, helper.ReturnType, instance) { HasThis = false };
        }
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, call)); main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        Require(module.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).All(i => i.Operand is not MethodReference method
            || method.DeclaringType.FullName != marker.FullName && !(method.DeclaringType.FullName == host.FullName && method.Name == "Annotated")), "uncertainty fixture directly selected an attribute provider or constructor");
        app.Write(Path.Combine(directory, "GraphApp.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        library.Write(Path.Combine(directory, "GraphLib.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    static void Check(string directory, string mode, string state)
    {
        var (family, unknown) = Parse(mode);
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        using var library = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        var module = library.MainModule; var own = app.MainModule;
        MethodDefinition? Find(string owner, string name) => module.GetType("Graph." + owner)?.Methods.SingleOrDefault(m => m.Name == name);
        void Body(string owner, string name) => Require(Find(owner, name) is { HasBody: true } method && method.Body.Instructions.Count != 0 && !Stub(method), "uncertainty attribute target body was lost: " + owner + "::" + name);
        void Unexecuted(string owner, string name) => Require(Find(owner, name) is not { } method || method.HasBody && Stub(method), "uncertainty attribute filter executed an unrelated body: " + owner + "::" + name);
        var known = own.GetType("Graph.KnownOwner"); var available = known?.Methods.SingleOrDefault(m => m.Name == "Available");
        bool canonical = family.StartsWith("canonical-", StringComparison.Ordinal);
        bool contextual = family.StartsWith("contextual-", StringComparison.Ordinal);
        bool template = family.StartsWith("template-", StringComparison.Ordinal);
        bool view = ViewFamilies.Contains(family);
        bool boundFixture = BoundFamilies.Contains(family);
        bool origin = OriginFamilies.Contains(family);
        bool composed = canonical || contextual || template || view || boundFixture || origin || family is "object-generic" or "box-class" or "box-value" or "field-array" or "field-nested" or "field-bound-array" or "field-bound-nested" or "method-array";
        if (!composed && family is not ("generic-definition" or "accessor"))
            Require(available is not null && known!.CustomAttributes.Count == 0 && available.CustomAttributes.Count == 0, "uncertainty fixture has no concrete unannotated lookup candidate");
        if (family == "base") Require(own.GetType("Graph.KnownDerived")?.BaseType.FullName == known!.FullName, "uncertainty fixture has no concrete base candidate");
        if (family == "generic-definition") Require(own.GetType("Graph.KnownGeneric`1") is { GenericParameters.Count: 1, CustomAttributes.Count: 0 }, "uncertainty fixture has no concrete generic definition candidate");
        if (family == "interface") Require(known!.Interfaces.Any(i => i.InterfaceType.FullName == "Graph.IÅ")
            && known.IsAbstract && !known.IsInterface && available is { IsAbstract: true, IsVirtual: true, IsStatic: false, HasThis: true, HasBody: false, ReturnType.FullName: "System.Void", Parameters.Count: 0 }
            && own.GetType("Graph.IÅ") is { IsInterface: true, CustomAttributes.Count: 0 } contract
            && contract.Methods.SingleOrDefault(m => m.Name == "Available") is { IsAbstract: true, IsVirtual: true, IsStatic: false, HasThis: true, HasBody: false, ReturnType.FullName: "System.Void", Parameters.Count: 0 }, "uncertainty fixture has no abstract non-ASCII interface candidate");
        if (family == "accessor")
        {
            var property = own.GetType("Graph.KnownPropertyOwner")?.Properties.SingleOrDefault(p => p.Name == "Value");
            Require(property?.GetMethod is not null && property.GetMethod.CustomAttributes.Count == 0, "uncertainty fixture has no concrete unannotated accessor candidate");
            if (state == "published") Require(property!.GetMethod!.HasBody && Stub(property.GetMethod), "attribute projection executed its known getter");
        }
        var helper = own.GetType(family == "contextual-own-array" ? "Graph.UncertaintyDriver`1" : "Graph.UncertaintyDriver").Methods.Single(m => m.Name == "Read");
        if (origin) CheckOriginFixture(app, library, family, unknown, state, helper);
        else if (boundFixture) CheckBoundFixture(app, library, family, unknown, state, helper);
        else if (view) CheckViewFixture(app, family, unknown, state, helper);
        else if (contextual)
        {
            bool ownClass = family == "contextual-own-array";
            Require(helper is { IsStatic: true, HasBody: true, Parameters.Count: 0 }
                && helper.GenericParameters.Count == (ownClass ? 0 : 1) && helper.DeclaringType.GenericParameters.Count == (ownClass ? 1 : 0), "contextual token fixture changed its caller generic owner");
            var calls = own.EntryPoint.Body.Instructions.Select(i => i.Operand).OfType<MethodReference>().Where(m => m.Name == "Read").ToArray();
            Require(calls.Length == 1 && (ownClass ? calls[0].DeclaringType is GenericInstanceType callerOwner && callerOwner.ElementType.FullName == helper.DeclaringType.FullName
                    && callerOwner.GenericArguments.Count == 1 && callerOwner.GenericArguments[0].FullName == "System.String"
                : calls[0] is GenericInstanceMethod call && call.ElementMethod.FullName == helper.FullName
                    && call.GenericArguments.Count == 1 && call.GenericArguments[0].FullName == "System.String"), "contextual token fixture lost its exact String caller instantiation");
            bool CallerArgument(TypeReference argument) => unknown ? argument is GenericParameter parameter && parameter.Position == 0
                    && ReferenceEquals(parameter.Owner, ownClass ? (IGenericParameterProvider)helper.DeclaringType : helper)
                : argument.FullName == "System.String";
            if (family == "contextual-field-array")
            {
                var owner = own.GetType("Graph.ContextFieldOwner`1"); var field = owner?.Fields.SingleOrDefault(f => f.Name == "Value");
                Require(field is { IsStatic: true, FieldType: ArrayType array } && array.ElementType is GenericParameter parameter
                    && parameter.Position == 0 && ReferenceEquals(parameter.Owner, owner), "contextual field token lost its original type-formal array signature");
                var tokens = helper.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is FieldReference).Select(i => (FieldReference)i.Operand).ToArray();
                Require(tokens.Length == 1 && tokens[0] is not FieldDefinition && tokens[0].Name == "Value"
                    && tokens[0].DeclaringType is GenericInstanceType instance && instance.ElementType.FullName == owner!.FullName
                    && instance.GenericArguments.Count == 1 && CallerArgument(instance.GenericArguments[0]), "contextual field token lost its MemberRef caller context");
                Require(helper.Body.Instructions.Any(i => i.Operand is MethodReference method && method.DeclaringType.FullName == "System.Reflection.FieldInfo"
                        && method.Name == "GetFieldFromHandle" && method.Parameters.Count == 2)
                    && helper.Body.Instructions.Any(i => i.Operand is MethodReference method && method.Name == "get_FieldType"), "contextual field token lost its handle-to-signature route");
            }
            else
            {
                var owner = ownClass ? helper.DeclaringType : own.GetType("Graph.ContextMethodOwner"); var method = owner.Methods.Single(m => m.Name == "ReturnArray");
                Require(method.ReturnType is ArrayType array && (ownClass ? CallerArgument(array.ElementType)
                        : array.ElementType is GenericParameter parameter && parameter.Position == 0 && ReferenceEquals(parameter.Owner, method))
                    && method.GenericParameters.Count == (ownClass ? 0 : 1), "contextual method token lost its original array-return signature");
                var tokens = helper.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is MethodReference).Select(i => (MethodReference)i.Operand).ToArray();
                Require(tokens.Length == 1 && (ownClass ? tokens[0] is MethodDefinition definition && ReferenceEquals(definition, method)
                    : tokens[0] is GenericInstanceMethod instance && instance.ElementMethod.FullName == method.FullName
                        && instance.GenericArguments.Count == 1 && CallerArgument(instance.GenericArguments[0])), "contextual method token lost its raw own definition or caller-bound MethodSpec");
                Require(helper.Body.Instructions.Any(i => i.Operand is MethodReference target && target.DeclaringType.FullName == "System.Reflection.MethodBase"
                        && target.Name == "GetMethodFromHandle" && target.Parameters.Count == (ownClass ? 2 : 1))
                    && helper.Body.Instructions.Any(i => i.Operand is MethodReference target && target.DeclaringType.FullName == "System.Reflection.MethodInfo" && target.Name == "get_ReturnType"), "contextual method token lost its handle-to-signature route");
                Require(method.HasBody && (method.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldnull, Code.Ret })
                    || state == "published" && Stub(method)), "contextual method token changed its dependency-free original body or metadata stub");
                Require(own.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions)
                    .Where(i => i.Operand is MethodReference target && target.Name == method.Name && target.DeclaringType.GetElementType().FullName == owner.FullName)
                    .All(i => i.OpCode == OpCodes.Ldtoken), "contextual method token gained an executable call to its signature provider");
            }
        }
        else if (template)
        {
            var owner = own.GetType(family == "template-base" ? "Graph.EmptyDerived`1" : "Graph.EmptyGeneric`1");
            Require(owner is { IsAbstract: false, IsInterface: false, GenericParameters.Count: 1, Fields.Count: 0, Interfaces.Count: 0 }
                && !owner.Methods.Any(m => m.Name == ".cctor") && !owner.CustomAttributes.Any(a => a.AttributeType.FullName == "Graph.UncertaintyMarkerAttribute"), "template fixture lost its eligible unannotated generic definition");
            if (state == "original")
                Require(own.GetTypes().Where(t => t.Name is "EmptyGeneric`1" or "EmptyDerived`1").All(t => !t.IsNested && !t.IsAbstract && !t.IsInterface
                    && t.GenericParameters.Count == 1 && !t.GenericParameters[0].HasConstraints && t.Fields.Count == 0 && t.Interfaces.Count == 0
                    && !t.Methods.Any(m => m.Name == ".cctor") && t.Methods.Single(m => m.Name == ".ctor") is { HasThis: true, Parameters.Count: 0, HasBody: true } ctor
                    && ctor.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldarg_0, Code.Call, Code.Ret })), "template fixture changed its original eligible constructor or hierarchy shape");
            Require(!helper.HasGenericParameters && !helper.DeclaringType.HasGenericParameters && helper.Parameters.Count == 0
                && own.EntryPoint.Body.Instructions.Count(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference method && method.FullName == helper.FullName) == 1, "template fixture changed its nongeneric reached helper");
            var tokens = helper.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference).Select(i => (TypeReference)i.Operand).ToArray();
            if (unknown)
            {
                Require(!own.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions)
                    .Any(i => i.Operand is GenericInstanceType instance && instance.ElementType.FullName == owner!.FullName
                        && instance.GenericArguments.Any(a => a.FullName == "System.String")), "template fixture has an independent original String instantiation");
                if (family == "template-name-array")
                    Require(helper.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldstr && i.Operand is string name
                        && name.StartsWith("Graph.EmptyGeneric`1[[System.String, ", StringComparison.Ordinal) && name.EndsWith("]], GraphApp", StringComparison.Ordinal)), "template name fixture lost its qualified generic name without inline modifiers");
                else
                    Require(tokens.Any(t => t.FullName == owner!.FullName) && tokens.Any(t => t.FullName == "System.String")
                        && helper.Body.Instructions.Count(i => i.Operand is MethodReference method && method.Name == "MakeGenericType") == 1
                        && helper.Body.Instructions.Any(i => i.OpCode == OpCodes.Newarr && i.Operand is TypeReference type && type.FullName == "System.Type")
                        && helper.Body.Instructions.Count(i => i.OpCode == OpCodes.Stelem_Ref) == 1, "template fixture lost its exact String construction vector");
            }
            else
                Require(tokens.Count(t => t is GenericInstanceType instance && instance.ElementType.FullName == owner!.FullName
                        && instance.GenericArguments.Count == 1 && instance.GenericArguments[0].FullName == "System.String") == 1
                    && !helper.Body.Instructions.Any(i => i.Operand is MethodReference method && method.Name is "MakeGenericType" or "GetType"), "template control lost its direct original closed type token");
            string operation = family == "template-base" ? "get_BaseType" : family == "template-default-members" ? "GetDefaultMembers" : "MakeArrayType";
            Require(helper.Body.Instructions.Count(i => i.Operand is MethodReference method && method.DeclaringType.FullName == "System.Type" && method.Name == operation) == 1, "template fixture changed its structural consumer transform");
            if (family == "template-base")
                Require(owner!.BaseType is GenericInstanceType parent && parent.ElementType.FullName == "Graph.EmptyGeneric`1"
                    && parent.GenericArguments.Single() is GenericParameter parameter && ReferenceEquals(parameter.Owner, owner), "template base fixture lost its injective original parameter projection");
            if (family == "template-default-members")
            {
                Require(owner!.CustomAttributes.SingleOrDefault()?.AttributeType.FullName == "System.Reflection.DefaultMemberAttribute"
                    && owner.CustomAttributes.Single().ConstructorArguments.Single().Value is "Available"
                    && owner.Methods.SingleOrDefault(m => m.Name == "Available") is { } selected && (state == "original" || Stub(selected))
                    && helper.Body.Instructions.Count(i => i.OpCode == OpCodes.Ldelem_Ref) == 1, "template default-member fixture lost its known metadata-only member control");
            }
        }
        else if (canonical)
        {
            Require(helper is { IsStatic: true, HasBody: true, GenericParameters.Count: 1, Parameters.Count: 0 }
                && !helper.DeclaringType.HasGenericParameters, "canonical fixture lost its shareable generic method definition");
            var calls = own.EntryPoint.Body.Instructions.Where(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference method
                && method.Name == "Read" && method.DeclaringType.FullName == helper.DeclaringType.FullName).Select(i => (MethodReference)i.Operand).ToArray();
            Require(calls.Length == 1 && calls[0] is GenericInstanceMethod call && call.ElementMethod.FullName == helper.FullName
                && call.GenericArguments.Count == 1 && call.GenericArguments[0].FullName == "System.String", "canonical fixture lost its exact Read<String> MethodSpec");
            var tokens = helper.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference type
                && type.FullName != "Graph.UncertaintyMarkerAttribute").Select(i => (TypeReference)i.Operand).ToArray();
            Require(tokens.Length == 1, "canonical fixture has an independent type owner witness");
            TypeReference argument = tokens[0];
            if (family == "canonical-array")
            {
                Require(argument is ArrayType { IsVector: true }, "canonical fixture lost its composed array token"); argument = ((ArrayType)argument).ElementType;
            }
            else if (family is "canonical-generic" or "canonical-definition")
            {
                Require(argument is GenericInstanceType instance && instance.ElementType.FullName == "Graph.KnownGeneric`1" && instance.GenericArguments.Count == 1
                    && own.GetType("Graph.KnownGeneric`1") is { GenericParameters.Count: 1, CustomAttributes.Count: 0 }, "canonical fixture lost its composed unannotated generic candidate");
                argument = ((GenericInstanceType)argument).GenericArguments[0];
            }
            Require(unknown ? argument is GenericParameter parameter && parameter.Position == 0 && ReferenceEquals(parameter.Owner, helper)
                : argument.FullName == "System.String", "canonical fixture changed its body-context formal or complete token");
            Require(helper.Body.Instructions.Count(i => i.Operand is MethodReference method && method.DeclaringType.FullName == "System.Type" && method.Name == "MakeArrayType")
                    == (family == "canonical-made-array" ? 1 : 0)
                && helper.Body.Instructions.Count(i => i.Operand is MethodReference method && method.DeclaringType.FullName == "System.Type" && method.Name == "GetGenericTypeDefinition")
                    == (family == "canonical-definition" ? 1 : 0), "canonical fixture changed its owner transform");
        }
        else if (family == "object-generic")
        {
            Require(own.GetType("Graph.KnownGeneric`1") is { GenericParameters.Count: 1, CustomAttributes.Count: 0 }
                && helper.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference type && type.FullName == "Graph.KnownGeneric`1")
                && helper.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference type && type.FullName == "System.Int32")
                && helper.Body.Instructions.Any(i => i.Operand is MethodReference method && method.DeclaringType.FullName == "System.Type" && method.Name == "MakeGenericType"), "generic object fixture lost its concrete definition candidate");
            Require(helper.Body.Instructions.Any(i => i.OpCode == OpCodes.Newobj && i.Operand is MethodReference method && method.DeclaringType.FullName == "System.Reflection.TypeDelegator") == unknown, "generic object fixture changed its owner classification");
        }
        else if (family is "box-class" or "box-value")
        {
            var owner = own.GetType("Graph.KnownBox`1");
            Require(owner is { GenericParameters.Count: 1, CustomAttributes.Count: 0 } && owner.IsValueType == (family == "box-value")
                && owner.IsSealed == (family == "box-value" || !unknown), "boxing fixture changed its exact runtime type classification");
            Require(helper.Body.Instructions.Count(i => i.OpCode == OpCodes.Box && i.Operand is GenericInstanceType type && type.ElementType.FullName == "Graph.KnownBox`1"
                && type.GenericArguments.Count == 1 && type.GenericArguments[0].FullName == "System.Int32") == 1
                && helper.Body.Instructions.Any(i => i.OpCode == OpCodes.Callvirt && i.Operand is MethodReference method && method.DeclaringType.FullName == "System.Object" && method.Name == "GetType"), "boxing fixture lost its inexact-box GetType token");
            if (family == "box-class") Require(owner!.Methods.Single(m => m.Name == ".ctor") is { HasBody: true } ctor && !Stub(ctor), "boxing fixture lost its ordinary construction body");
        }
        else if (family is "field-array" or "field-nested" or "field-bound-array" or "field-bound-nested")
        {
            bool bound = family.StartsWith("field-bound-", StringComparison.Ordinal); bool formal = unknown || bound;
            var owner = own.GetType(formal ? "Graph.OpenOwner`1" : "Graph.ClosedOwner"); var field = owner?.Fields.SingleOrDefault(f => f.Name == "Value");
            bool ContainsVariable(TypeReference type) => type is GenericParameter || type is GenericInstanceType instance && instance.GenericArguments.Any(ContainsVariable)
                || type is TypeSpecification specification && type is not GenericInstanceType && ContainsVariable(specification.ElementType);
            Require(field is { IsPublic: true, IsStatic: true, FieldType: ArrayType } && owner!.HasGenericParameters == formal
                && ContainsVariable(field.FieldType) == formal, "field projection fixture changed its nested generic-variable signature");
            var element = ((ArrayType)field!.FieldType).ElementType;
            if (family.EndsWith("nested", StringComparison.Ordinal))
            {
                Require(element is GenericInstanceType wrapper && wrapper.ElementType.FullName == "Graph.KnownWrapper`1"
                    && wrapper.GenericArguments.Count == 1 && wrapper.GenericArguments[0] is ArrayType, "field projection lost its nested wrapper shape");
                element = ((ArrayType)((GenericInstanceType)element).GenericArguments[0]).ElementType;
            }
            Require(formal ? element is GenericParameter parameter && parameter.Position == 0 && ReferenceEquals(parameter.Owner, owner)
                : element.FullName == "System.Int32", "field projection changed its original open or closed element");
            Require(helper.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference type
                    && (bound ? type is GenericInstanceType instance && instance.ElementType.FullName == owner!.FullName
                            && instance.GenericArguments.Count == 1 && instance.GenericArguments[0].FullName == "System.Int32"
                        : type.FullName == owner!.FullName))
                && helper.Body.Instructions.Any(i => i.Operand is MethodReference method && method.DeclaringType.FullName == "System.Reflection.FieldInfo" && method.Name == "get_FieldType"), "field projection lost its concrete owner or signature conversion");
        }
        else if (family == "method-array")
        {
            var owner = own.GetType("Graph.KnownMethodOwner"); var method = owner?.Methods.SingleOrDefault(m => m.Name == "ReturnArray");
            Require(owner is { HasGenericParameters: false } && method is { GenericParameters.Count: 1, ReturnType: ArrayType array }
                && array.ElementType is GenericParameter parameter && parameter.Position == 0 && ReferenceEquals(parameter.Owner, method), "method return projection lost its original method-formal array signature");
            var tokens = helper.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is MethodReference).Select(i => (MethodReference)i.Operand).ToArray();
            Require(tokens.Length == 1 && (unknown ? tokens[0] is MethodDefinition definition && ReferenceEquals(definition, method)
                : tokens[0] is GenericInstanceMethod instance && instance.ElementMethod.FullName == method!.FullName
                    && instance.GenericArguments.Count == 1 && instance.GenericArguments[0].FullName == "System.Int32"), "method return projection changed its raw definition or closed MethodSpec token");
            Require(helper.Body.Instructions.Any(i => i.Operand is MethodReference target && target.DeclaringType.FullName == "System.Reflection.MethodBase"
                    && target.Name == "GetMethodFromHandle" && target.Parameters.Count == 1)
                && helper.Body.Instructions.Any(i => i.Operand is MethodReference target && target.DeclaringType.FullName == "System.Reflection.MethodInfo" && target.Name == "get_ReturnType"), "method return projection lost its recognized handle-to-signature path");
            Require(method is { HasBody: true } && (state == "published" && Stub(method) || method.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldnull, Code.Ret })), "method return projection introduced an executable body dependency");
        }
        else if (family == "type-name")
            Require(helper.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldstr && (string)i.Operand == (unknown ? "Graph.KnownOwner" : "Graph.KnownOwner, GraphApp")), "uncertainty fixture lost its concrete type-name candidate");
        else
        {
            string source = family == "base" ? "Graph.KnownDerived" : family == "generic-definition" ? "Graph.KnownGeneric`1" : family == "accessor" ? "Graph.KnownPropertyOwner" : "Graph.KnownOwner";
            Require(helper.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference type && type.GetElementType().FullName == source), "uncertainty projection lost its concrete source alternative");
        }
        if (unknown && (family is "base" or "generic-definition" or "array" or "element" or "accessor"))
            Require(helper.Parameters.Count == 2 && helper.Body.Instructions.Count(i => i.OpCode == OpCodes.Brtrue) == 1
                && helper.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldarg_1), "uncertainty projection lost its unknown source alternative");
        Require(helper.Body.Instructions.Count(i => i.Operand is MethodReference method && method.Name == "GetCustomAttributes") == 1
            && own.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).Count(i => i.Operand is MethodReference method && method.Name == "GetCustomAttributes") == 1, "uncertainty fixture has an independent attribute capability witness");
        Body("UncertaintyHost", "Touch");
        if (state == "original")
        {
            var provider = Find("UncertaintyHost", "Annotated");
            Require(provider?.CustomAttributes.SingleOrDefault() is { } attribute && attribute.AttributeType.FullName == "Graph.UncertaintyMarkerAttribute"
                && attribute.ConstructorArguments.Count == 1 && attribute.Properties.Single().Name == "Label", "uncertainty fixture lost its original attribute provider");
            Console.WriteLine("graph-uncertainty=original:" + mode + ":known-candidate-present");
            return;
        }
        bool memberMetadataClosure = unknown && (family is "accessor" or "object" or "object-generic");
        if (memberMetadataClosure)
        {
            // Widened member metadata keeps ordinary attribute IL without authorizing primary execution.
            Body("UncertaintyMarkerAttribute", ".ctor"); Body("UncertaintyMarkerAttribute", "set_Label"); Body("UncertaintyMarkerAttribute", "get_Label");
            Require(module.GetType("Graph.UncertaintyCtorDependency") is not null && module.GetType("Graph.UncertaintySetterDependency") is not null,
                "widened member metadata lost its ordinary attribute dependencies");
        }
        else
        {
            Unexecuted("UncertaintyMarkerAttribute", ".ctor"); Unexecuted("UncertaintyMarkerAttribute", "set_Label");
            Require(module.GetType("Graph.UncertaintyCtorDependency") is null && module.GetType("Graph.UncertaintySetterDependency") is null,
                "unknown attribute owner retained unrelated constructor or setter dependencies");
        }
        if (unknown)
            Require(Find("UncertaintyHost", "Annotated")?.CustomAttributes.Any(a => a.AttributeType.FullName == "Graph.UncertaintyMarkerAttribute") == true,
                "independently reached host lost its original attribute identity row");
        Unexecuted("UncertaintyHost", "Annotated"); Require(module.GetType("Graph.UncertaintyAnnotatedDependency") is null, "attribute instance selection executed its provider body");
        Require(family == "interface" || available is null || available.HasBody && Stub(available), "attribute lookup executed its known candidate method");
        Console.WriteLine("graph-uncertainty=published:" + mode + (memberMetadataClosure ? ":metadata-member-closure" : unknown ? ":unknown-no-body" : ":metadata-only"));
    }

    static void CheckViewFixture(AssemblyDefinition app, string family, bool unknown, string state, MethodDefinition helper)
    {
        var own = app.MainModule; var instructions = helper.Body.Instructions;
        bool Calls(string owner, string name) => instructions.Any(i => i.Operand is MethodReference method && method.DeclaringType.FullName == owner && method.Name == name);
        var tokens = instructions.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference).Select(i => (TypeReference)i.Operand).ToArray();
        bool IsWrapper(TypeReference type) => type is GenericInstanceType instance && (instance.ElementType.FullName == "Graph.ViewWrapper`1" || instance.GenericArguments.Any(IsWrapper))
            || type is TypeSpecification specification && type is not GenericInstanceType && IsWrapper(specification.ElementType);
        void TemplateShape(TypeDefinition owner)
        {
            Require(!owner.IsNested && !owner.IsAbstract && !owner.IsInterface && owner.GenericParameters.Count == 1
                && !owner.GenericParameters[0].HasConstraints && owner.Fields.Count == 0 && owner.Interfaces.Count == 0
                && !owner.Methods.Any(m => m.Name == ".cctor"), "metadata fixture changed its eligible template shape");
            if (state == "original")
                Require(owner.Methods.Single(m => m.Name == ".ctor") is { HasThis: true, Parameters.Count: 0, HasBody: true } ctor
                    && ctor.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldarg_0, Code.Call, Code.Ret }), "metadata fixture changed its original trivial template constructor");
        }
        Require(!own.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).Any(m => m.Body.Variables.Any(v => IsWrapper(v.VariableType))
            || m.Body.Instructions.Any(i => i.Operand switch
            {
                TypeReference type => IsWrapper(type),
                MethodReference target => IsWrapper(target.DeclaringType) || target is GenericInstanceMethod method && method.GenericArguments.Any(IsWrapper),
                FieldReference field => IsWrapper(field.FieldType),
                _ => false,
            })), "metadata view fixture has an independent executable wrapper context");
        if (family.StartsWith("tuple-", StringComparison.Ordinal))
        {
            Require(helper.GenericParameters.Count == 1 && !helper.DeclaringType.HasGenericParameters && helper.Parameters.Count == (family == "tuple-index" ? 1 : 0), "template tuple fixture changed its shared caller definition");
            var calls = own.EntryPoint.Body.Instructions.Where(i => i.Operand is MethodReference method && method.Name == "Read").Select(i => (MethodReference)i.Operand).ToArray();
            Require(calls.Length == 1 && calls[0] is GenericInstanceMethod call && call.ElementMethod.FullName == helper.FullName
                && call.GenericArguments.Count == 1 && call.GenericArguments[0].FullName == "System.String", "template tuple fixture lost its exact Read<String> MethodSpec");
            var owner = own.GetType("Graph.TupleOwner`1");
            Require(owner is not null, "template tuple fixture lost its original owner"); TemplateShape(owner!);
            Require(owner is { GenericParameters.Count: 1, Fields.Count: 0, Interfaces.Count: 0 } && !owner.Methods.Any(m => m.Name == ".cctor")
                && tokens.Count(t => t.FullName == owner.FullName) == 1 && !tokens.OfType<GenericInstanceType>().Any(t => t.ElementType.FullName == owner.FullName), "template tuple fixture lost its original eligible definition-only owner");
            var arrays = tokens.OfType<ArrayType>().ToArray();
            Require(arrays.Length == 1 && arrays[0].IsVector && (unknown || family == "tuple-owner"
                ? arrays[0].ElementType is GenericParameter tupleParameter && tupleParameter.Position == 0 && ReferenceEquals(tupleParameter.Owner, helper)
                : arrays[0].ElementType.FullName == "System.String"), "template tuple fixture changed its symbolic or known array argument");
            Require(Calls("System.Type", "MakeGenericType") && instructions.Count(i => i.OpCode == OpCodes.Newarr) == 1
                && instructions.Count(i => i.OpCode == OpCodes.Stelem_Ref) == 1, "template tuple fixture lost its one-element argument vector");
            Require(Calls("System.Type", "GetGenericArguments") == (family != "tuple-owner")
                && instructions.Count(i => i.OpCode == OpCodes.Ldelem_Ref) == (family == "tuple-owner" ? 0 : 1), "template tuple fixture changed its direct owner or argument projection");
            if (family != "tuple-owner")
            {
                var element = instructions.Single(i => i.OpCode == OpCodes.Ldelem_Ref);
                Require(element.Previous.OpCode == (family == "tuple-index" ? OpCodes.Ldarg_0 : OpCodes.Ldc_I4_0), "template tuple fixture changed its fixed or unknown index summary path");
            }
            return;
        }
        Require(!helper.HasGenericParameters && !helper.DeclaringType.HasGenericParameters && helper.Parameters.Count == 0, "metadata view fixture changed its nongeneric reader");
        if (family.StartsWith("interface-", StringComparison.Ordinal))
        {
            bool fixedInterface = family.StartsWith("interface-fixed-", StringComparison.Ordinal);
            var owner = own.GetType("Graph.InterfaceOpen`1"); var contract = own.GetType("Graph.IView`1");
            Require(owner is { IsAbstract: true, GenericParameters.Count: 1, Methods.Count: 0, Interfaces.Count: 1 }
                && contract is { IsInterface: true, IsAbstract: true, GenericParameters.Count: 1, Methods.Count: 0, CustomAttributes.Count: 0 }
                && owner.Interfaces[0].InterfaceType is GenericInstanceType interfaceInstance && interfaceInstance.ElementType.FullName == contract.FullName
                && interfaceInstance.GenericArguments.Count == 1 && (fixedInterface ? interfaceInstance.GenericArguments[0].FullName == "System.String"
                    : interfaceInstance.GenericArguments[0] is GenericParameter interfaceParameter && ReferenceEquals(interfaceParameter.Owner, owner)), "interface fixture changed its dependent or fixed original edge");
            Require(tokens.Any(t => !unknown && !fixedInterface ? t is GenericInstanceType closed && closed.ElementType.FullName == owner!.FullName
                    && closed.GenericArguments.Count == 1 && closed.GenericArguments[0].FullName == "System.String" : t is TypeDefinition && t.FullName == owner!.FullName), "interface fixture lost its original open or directly compiled closed query");
            bool named = family.EndsWith("named", StringComparison.Ordinal);
            Require(Calls("System.Type", named ? "GetInterface" : "GetInterfaces") && instructions.Count(i => i.OpCode == OpCodes.Ldelem_Ref) == (named ? 0 : 1)
                && (!named || instructions.Any(i => i.OpCode == OpCodes.Ldstr && (string)i.Operand == "IView`1")), "interface fixture changed its ASCII named or enumerated selection");
            return;
        }
        bool made = family.StartsWith("template-view-", StringComparison.Ordinal);
        var viewOwner = own.GetType(made ? "Graph.ViewOwner`1" : unknown ? "Graph.ViewOpen`1" : "Graph.ViewClosed");
        var wrapper = own.GetType("Graph.ViewWrapper`1");
        Require(viewOwner is not null && viewOwner.HasGenericParameters == (made || unknown)
            && (made && !unknown && state == "published" || wrapper is { GenericParameters.Count: 1, Fields.Count: 0, Interfaces.Count: 0, CustomAttributes.Count: 0 }), "metadata view fixture lost its original owner or nested wrapper definition");
        bool parameterView = family is "view-parameter" or "template-view-parameter";
        bool propertyView = family is "view-property" or "view-accessor";
        TypeReference signature;
        if (family == "view-field") signature = viewOwner!.Fields.Single(f => f.Name == "Value").FieldType;
        else if (propertyView) signature = viewOwner!.Properties.Single(p => p.Name == "Value").PropertyType;
        else
        {
            var method = viewOwner!.Methods.Single(m => m.Name == "Project"); signature = parameterView ? method.Parameters.Single().ParameterType : method.ReturnType;
        }
        if (made)
        {
            TemplateShape(viewOwner!);
            if (unknown)
            {
                Require(signature is GenericInstanceType viewInstance && viewInstance.ElementType.FullName == wrapper!.FullName && viewInstance.GenericArguments.Count == 1, "template signature lost its nested metadata wrapper");
                signature = ((GenericInstanceType)signature).GenericArguments[0];
            }
            Require(signature is ArrayType signatureArray && signatureArray.ElementType is GenericParameter signatureParameter && ReferenceEquals(signatureParameter.Owner, viewOwner), "template signature lost its original plain or nested formal array");
            Require(tokens.Count(t => t.FullName == viewOwner!.FullName) == 1 && tokens.Any(t => t.FullName == "System.String")
                && !tokens.OfType<GenericInstanceType>().Any(t => t.ElementType.FullName == viewOwner!.FullName)
                && Calls("System.Type", "MakeGenericType"), "template signature fixture changed its helper's known String formation vector");
            var compiled = own.EntryPoint.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is GenericInstanceType instance
                && instance.ElementType.FullName == viewOwner!.FullName).ToArray();
            Require(compiled.Length == (unknown ? 0 : 1), "template signature fixture changed its absent or explicit compiled-tuple control");
            if (!unknown)
                Require(((GenericInstanceType)compiled[0].Operand).GenericArguments.Single().FullName == "System.String"
                    && compiled[0].Next is { Operand: MethodReference target } && target.DeclaringType.FullName == "System.Type" && target.Name == "GetTypeFromHandle"
                    && compiled[0].Next.Next.OpCode == OpCodes.Pop
                    && own.EntryPoint.Body.Instructions.IndexOf(compiled[0]) < own.EntryPoint.Body.Instructions.ToList().FindIndex(i => i.Operand is MethodReference method && method.FullName == helper.FullName), "template signature control lost its concrete handle receipt before the unchanged helper");
        }
        else
        {
            Require(signature is GenericInstanceType fixedWrapper && fixedWrapper.ElementType.FullName == wrapper!.FullName && fixedWrapper.GenericArguments.Count == 1
                && fixedWrapper.GenericArguments[0].FullName == "System.Int32" && tokens.Count(t => t.FullName == viewOwner!.FullName) == 1
                && !Calls("System.Type", "MakeGenericType"), "definition signature fixture changed its fixed Wrapper<Int32> or compiled owner control");
        }
        Require(Calls("System.Type", family == "view-field" ? "GetField" : propertyView ? "GetProperty" : "GetMethod")
            && Calls(family == "view-field" ? "System.Reflection.FieldInfo" : family == "view-property" ? "System.Reflection.PropertyInfo"
                : parameterView ? "System.Reflection.ParameterInfo" : "System.Reflection.MethodInfo",
                family == "view-field" ? "get_FieldType" : family == "view-property" ? "get_PropertyType" : parameterView ? "get_ParameterType" : "get_ReturnType")
            && (family != "view-accessor" || Calls("System.Reflection.PropertyInfo", "GetGetMethod"))
            && (!parameterView || Calls("System.Reflection.MethodBase", "GetParameters")), "metadata view fixture changed its member signature or accessor projection");
        foreach (var method in viewOwner!.Methods.Where(m => !m.IsConstructor))
            Require(method.HasBody && (state == "published" && Stub(method) || method.Body.Instructions.Select(i => i.OpCode.Code)
                .SequenceEqual(method.ReturnType.FullName == "System.Void" ? new[] { Code.Ret } : new[] { Code.Ldnull, Code.Ret })), "metadata view fixture changed its dependency-free signature provider body");
        Require(!own.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions)
            .Any(i => i.Operand is MethodReference target && target.DeclaringType.GetElementType().FullName == viewOwner.FullName && !target.Name.StartsWith(".", StringComparison.Ordinal)), "metadata view fixture directly calls or binds its signature provider");
    }

    static void CheckBoundFixture(AssemblyDefinition app, AssemblyDefinition library, string family, bool unknown, string state, MethodDefinition helper)
    {
        var own = app.MainModule; var instructions = helper.Body.Instructions;
        int Calls(string owner, string name) => instructions.Count(i => i.Operand is MethodReference method && method.DeclaringType.FullName == owner && method.Name == name);
        var tokens = instructions.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference).Select(i => (TypeReference)i.Operand).ToArray();
        if (family == "tuple-object")
        {
            const int arity = 1;
            var owner = own.GetType("Graph.ObjectTupleOwner`1");
            Require(owner is { Fields.Count: 0, Interfaces.Count: 0, IsAbstract: false, IsInterface: false } && owner.GenericParameters.Count == arity
                && owner.GenericParameters.All(p => !p.HasConstraints) && !owner.Methods.Any(m => m.Name == ".cctor") && !tokens.OfType<GenericInstanceType>().Any(t => t.ElementType.FullName == owner.FullName)
                && tokens.Count(t => t.FullName == owner.FullName) == 1 && !helper.HasGenericParameters && helper.Parameters.Count == 0, "tuple knownness fixture changed its eligible original definition or caller");
            Require(Calls("System.Type", "MakeGenericType") == 1 && Calls("System.Type", "GetGenericArguments") == 1 && Calls("System.Type", "MakeArrayType") == 1
                && instructions.Count(i => i.OpCode == OpCodes.Newarr) == 1 && instructions.Count(i => i.OpCode == OpCodes.Stelem_Ref) == arity
                && instructions.Count(i => i.OpCode == OpCodes.Ldelem_Ref && i.Previous.OpCode == OpCodes.Ldc_I4_0) == 1, "tuple knownness fixture lost its vector, fixed generic argument, or array transform");
            var allocation = instructions.Single(i => i.OpCode == OpCodes.Newarr);
            Require(allocation.Operand is TypeReference element && element.FullName == "System.Type" && allocation.Previous.Operand is int length && length == arity
                && instructions.Where(i => i.OpCode == OpCodes.Dup).Select(i => i.Next.Operand is int position ? position : -1).SequenceEqual(Enumerable.Range(0, arity))
                && tokens.All(t => t.FullName == owner!.FullName || t.FullName is "System.String" or "System.Object" or "Graph.UncertaintyMarkerAttribute"), "tuple knownness fixture changed its complete vector shape or introduced another type source");
            var boxes = instructions.Where(i => i.OpCode == OpCodes.Box).ToArray();
            Require(boxes.Length == (unknown ? 1 : 0) && boxes.All(i => i.Operand is TypeReference type && type.FullName == "System.Type"
                    && i.Previous.Operand is MethodReference method && method.DeclaringType.FullName == "System.Type" && method.Name == "GetTypeFromHandle"
                    && i.Previous.Previous is { OpCode.Code: Code.Ldtoken, Operand: TypeReference source } && source.FullName == "System.String"
                    && i.Next.OpCode == OpCodes.Stelem_Ref)
                && !instructions.Any(i => i.OpCode == OpCodes.Newobj) && tokens.Count(t => t.FullName == "System.String") == 1, "tuple object fixture lost its ExactObject-producing reference box or direct Type control");
            return;
        }
        var methodOwner = own.GetType("Graph.MethodTupleOwner`1"); var genericMethod = methodOwner?.Methods.SingleOrDefault(m => m.Name == "G");
        Require(methodOwner is { GenericParameters.Count: 1, Fields.Count: 0, Interfaces.Count: 0, IsAbstract: false, IsInterface: false } && !methodOwner.GenericParameters[0].HasConstraints && !methodOwner.Methods.Any(m => m.Name == ".cctor")
            && genericMethod is { IsStatic: true, GenericParameters.Count: 1, Parameters.Count: 0, ReturnType.FullName: "System.Void", HasBody: true } && !genericMethod.GenericParameters[0].HasConstraints
            && (state == "published" && Stub(genericMethod) || genericMethod.Body.Instructions.Count == 1 && genericMethod.Body.Instructions[0].OpCode == OpCodes.Ret), "template generic-method fixture lost its fixed signature or dependency-free original body");
        var call = own.EntryPoint.Body.Instructions.Select(i => i.Operand).OfType<GenericInstanceMethod>().SingleOrDefault();
        Require(helper.GenericParameters.Count == 1 && helper.Parameters.Count == 0 && call is not null && call.ElementMethod.FullName == helper.FullName
            && call.GenericArguments.Single().FullName == "System.String", "template generic-method fixture lost its exact shared Read<String> call");
        var array = tokens.OfType<ArrayType>().SingleOrDefault();
        Require(array is not null && (unknown || family == "tuple-method-owner" ? array.ElementType is GenericParameter parameter && ReferenceEquals(parameter.Owner, helper)
            : array.ElementType.FullName == "System.String"), "template generic-method fixture changed its uncertain or known array tuple");
        bool close = family == "tuple-method-closure";
        Require(Calls("System.Type", "MakeGenericType") == 1 && Calls("System.Type", "GetMethod") == 1
            && Calls("System.Reflection.MethodInfo", "MakeGenericMethod") == (close ? 1 : 0)
            && instructions.Count(i => i.OpCode == OpCodes.Newarr) == (close ? 2 : 1) && instructions.Count(i => i.OpCode == OpCodes.Stelem_Ref) == (close ? 2 : 1)
            && (!close || tokens.Count(t => t.FullName == "System.Int32") == 1)
            && instructions.Any(i => i.OpCode == OpCodes.Ldstr && (string)i.Operand == "G")
            && !own.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions)
                .Any(i => i.Operand is MethodReference target && target.DeclaringType.GetElementType().FullName == methodOwner!.FullName && target.Name == "G"), "template generic-method fixture changed its closure boundary or directly called the generic target");
    }

    static void CheckOriginFixture(AssemblyDefinition app, AssemblyDefinition library, string family, bool unknown, string state, MethodDefinition helper)
    {
        var own = app.MainModule; var instructions = helper.Body.Instructions;
        int Calls(string owner, string name) => instructions.Count(i => i.Operand is MethodReference method && method.DeclaringType.FullName == owner && method.Name == name);
        var tokens = instructions.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference).Select(i => (TypeReference)i.Operand).ToArray();
        Require(!helper.HasGenericParameters && helper.Parameters.Count == 0 && !instructions.Any(i => i.OpCode == OpCodes.Newobj), "formation-origin fixture gained a constructed receiver or generic caller");
        if (family is "primitive-loadset" or "modifier-loadset")
        {
            var owner = own.GetType("Graph.KnownOwner"); var field = owner?.Fields.SingleOrDefault();
            Require(owner is { HasGenericParameters: false, CustomAttributes.Count: 0 } && field is { Name: "Value", IsPublic: true, IsStatic: true, IsLiteral: false, CustomAttributes.Count: 0 }
                && (family == "modifier-loadset" ? field.FieldType is RequiredModifierType { ModifierType.FullName: "System.Runtime.CompilerServices.IsVolatile", ElementType.MetadataType: MetadataType.Int32, ElementType.FullName: "System.Int32" }
                    : field.FieldType.MetadataType == MetadataType.Int32 && field.FieldType.FullName == "System.Int32"), "primitive-loadset fixture lost its concrete original primitive or volatile field signature");
            Require(instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldtoken, Code.Call, Code.Ldstr, Code.Callvirt, Code.Callvirt,
                    Code.Ldtoken, Code.Call, Code.Ldc_I4_0, Code.Callvirt, Code.Pop, Code.Ret })
                && tokens.Length == 2 && ReferenceEquals(tokens[0], owner) && tokens[1].FullName == "Graph.UncertaintyMarkerAttribute"
                && instructions[2].Operand is "Value" && Calls("System.Type", "GetField") == 1 && Calls("System.Reflection.FieldInfo", "get_FieldType") == 1,
                "primitive-loadset fixture changed its recognized field-to-attribute pipeline");
            var operands = own.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).ToArray();
            Require(!operands.Any(i => i.Operand is FieldReference || i.Operand is GenericInstanceType
                    || i.Operand is TypeReference type && type.FullName == "System.Int32"
                    || i.Operand is MethodReference method && (method.DeclaringType.FullName == "Graph.UncertaintyMarkerAttribute"
                        || method.DeclaringType.FullName == "Graph.UncertaintyHost" && method.Name != "Touch")),
                "primitive-loadset fixture gained a primitive token, value read, opaque sink, or attribute body root");
            Require(own.EntryPoint.Body.Instructions.Where(i => i.OpCode == OpCodes.Call).Select(i => ((MethodReference)i.Operand).FullName)
                .SequenceEqual(new[] { "System.Void Graph.UncertaintyHost::Touch()", helper.FullName }), "primitive-loadset fixture lost its retained host and nongeneric helper call");
            return;
        }
        if (family == "definition-element")
        {
            var owner = own.GetType("Graph.DefinitionTupleOwner`1"); var argument = own.GetType("Graph.DefinitionArgument`1");
            foreach (var type in new[] { owner, argument })
            {
                Require(type is { GenericParameters.Count: 1, Fields.Count: 0, Interfaces.Count: 0, CustomAttributes.Count: 0, IsAbstract: false, IsInterface: false, IsNested: false }
                    && !type.GenericParameters[0].HasConstraints && type.BaseType.FullName == "System.Object"
                    && type.Methods.All(m => m.IsConstructor && !m.IsStatic), "definition-element fixture changed an eligible unannotated generic definition");
                if (state == "original")
                    Require(type!.Methods.Count == 1 && type.Methods[0] is { HasThis: true, Parameters.Count: 0, HasBody: true } ctor
                        && ctor.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldarg_0, Code.Call, Code.Ret })
                        && ctor.Body.Instructions[1].Operand is MethodReference baseCtor && baseCtor.DeclaringType.FullName == "System.Object" && baseCtor.Name == ".ctor",
                        "definition-element fixture lost its trivial original constructor");
            }
            Require(instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldtoken, Code.Call, Code.Ldc_I4_1, Code.Newarr,
                    Code.Dup, Code.Ldc_I4_0, Code.Ldtoken, Code.Call, Code.Stelem_Ref, Code.Callvirt, Code.Callvirt,
                    Code.Ldc_I4_0, Code.Ldelem_Ref, Code.Ldtoken, Code.Call, Code.Ldc_I4_0, Code.Callvirt, Code.Pop, Code.Ret })
                && instructions[3].Operand is TypeReference arrayElement && arrayElement.FullName == "System.Type"
                && tokens.Length == 3 && tokens[0] is TypeDefinition definition && ReferenceEquals(definition, owner)
                && (unknown ? tokens[1] is TypeDefinition nominal && ReferenceEquals(nominal, argument)
                    : tokens[1] is GenericInstanceType instance && instance.ElementType.FullName == argument!.FullName
                        && instance.GenericArguments.Count == 1 && instance.GenericArguments[0].FullName == "System.Int32")
                && tokens[2].FullName == "Graph.UncertaintyMarkerAttribute", "definition-element fixture lost its nominal definition or exact closed argument vector");
            Require(Calls("System.Type", "MakeGenericType") == 1 && Calls("System.Type", "GetGenericArguments") == 1 && Calls("System.Type", "MakeArrayType") == 0
                && instructions[9].Operand is MethodReference made && made.Name == "MakeGenericType"
                && instructions[10].Operand is MethodReference extracted && extracted.Name == "GetGenericArguments",
                "definition-element fixture changed its direct fixed-index extraction");
            var operands = own.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).ToArray();
            Require(!operands.Any(i => i.Operand is GenericInstanceType type && type.ElementType.FullName == owner!.FullName)
                && !operands.Any(i => i.Operand is MethodReference method && (method.DeclaringType.GetElementType().FullName == owner!.FullName
                    || method.DeclaringType.GetElementType().FullName == argument!.FullName || method.DeclaringType.FullName == "Graph.UncertaintyMarkerAttribute"))
                && operands.Count(i => i.Operand is GenericInstanceType type && type.ElementType.FullName == argument!.FullName) == (unknown ? 0 : 1),
                "definition-element fixture gained an independently constructed owner, argument, or attribute");
            Require(own.EntryPoint.Body.Instructions.Where(i => i.OpCode == OpCodes.Call).Select(i => ((MethodReference)i.Operand).FullName)
                .SequenceEqual(new[] { "System.Void Graph.UncertaintyHost::Touch()", helper.FullName }), "definition-element fixture lost its retained host and nongeneric helper call");
            return;
        }
        var vectorOwner = own.GetType("Graph.VectorOwner`1");
        Require(vectorOwner is { GenericParameters.Count: 1, Fields.Count: 0, Interfaces.Count: 0, IsAbstract: false, IsInterface: false }
            && !vectorOwner.GenericParameters[0].HasConstraints && !vectorOwner.Methods.Any(m => m.Name == ".cctor") && tokens.Count(t => t.FullName == vectorOwner.FullName) == 1
            && !tokens.OfType<GenericInstanceType>().Any(t => t.ElementType.FullName == vectorOwner.FullName), "whole-vector fixture changed its eligible original definition-only owner");
        Require(Calls("System.Type", "MakeGenericType") == 1 && Calls("System.Type", "GetGenericArguments") == 1 && Calls("System.Type", "MakeArrayType") == 1
            && instructions.Count(i => i.OpCode == OpCodes.Ldelem_Ref && i.Previous.OpCode == OpCodes.Ldc_I4_0) == 1, "whole-vector fixture lost its fixed generic argument and array transform");
        var formation = instructions.Single(i => i.Operand is MethodReference method && method.DeclaringType.FullName == "System.Type" && method.Name == "MakeGenericType");
        if (family == "null-vector")
            Require(formation.Previous.OpCode == OpCodes.Ldnull && instructions.Count(i => i.OpCode == OpCodes.Ldnull) == 1
                && !instructions.Any(i => i.OpCode == OpCodes.Newarr || i.OpCode == OpCodes.Stelem_Ref || i.OpCode == OpCodes.Box), "null-vector control no longer supplies a null array rather than a null element");
        else
        {
            var allocation = instructions.SingleOrDefault(i => i.OpCode == OpCodes.Newarr);
            Require(allocation is { Operand: TypeReference type } && type.FullName == "System.Type" && allocation.Previous.OpCode == OpCodes.Ldc_I4_1
                && instructions.Count(i => i.OpCode == OpCodes.Stelem_Ref) == 1 && instructions.Count(i => i.OpCode == OpCodes.Dup) == 1
                && instructions.Single(i => i.OpCode == OpCodes.Dup).Next.OpCode == OpCodes.Ldc_I4_0
                && tokens.Count(t => t.FullName == "System.String") == 1, "whole-vector fixture changed its exact one-position known Type array");
            var boxes = instructions.Where(i => i.OpCode == OpCodes.Box).ToArray();
            Require(boxes.Length == (unknown ? 1 : 0) && (unknown ? boxes[0].Operand is ArrayType { IsVector: true, ElementType.FullName: "System.Type" }
                    && boxes[0].Previous.OpCode == OpCodes.Stelem_Ref && ReferenceEquals(boxes[0].Next, formation)
                : formation.Previous.OpCode == OpCodes.Stelem_Ref), "whole-vector fixture lost its ExactObject-producing array reference box or direct-vector control");
        }
    }

    static bool Stub(MethodDefinition method) => method.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw });
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
