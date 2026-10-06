#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

internal static class FormalGlobalOwner
{
    public static U Echo<U>(U value) => value;
}

namespace ReflectGenericMethodSubset
{
    // SUBJECT: the generic-method reflection surface — MakeGenericMethod resolves
    // within the instantiations the program reaches STATICALLY (each explicit call
    // below is what puts the closed row in the image), GetGenericMethodDefinition
    // round-trips by definition identity, GetBaseDefinition walks virtual slots.
    // The intentional AOT divergences are frozen in the reflect-types bucket.
    static class Helper
    {
        public static string Describe<T>(T value) => typeof(T).Name + ":" + value;
        public static int TwoArg<T1, T2>(int seed) => seed + typeof(T1).Name.Length + typeof(T2).Name.Length;
        public static int Plain(int x) => x + 1;
    }

    class Box
    {
        public string Tag<T>(T v) => typeof(T).Name + "=" + v;
    }

    class DefinitionClass
    {
        public static string Stamp<U>(int seed) => typeof(U).Name + ":" + seed;
        public static U Echo<U>(U value) => value;
        public static string Never<U>(int seed) => "never:" + typeof(U).Name + ":" + seed;
        private static string Hidden<U>(int seed) => "hidden:" + typeof(U).Name + ":" + seed;
        public static string RootHidden() => Hidden<int>(3);
        public static string Select(int seed) => "plain:" + seed;
        public static string Select<U>(int seed) => typeof(U).Name + ":" + seed;
        public static string Select<U, V>(string value) => typeof(U).Name + "/" + typeof(V).Name + ":" + value;
    }

    class DefinitionChild : DefinitionClass
    {
    }

    class MixedDefinitionOwner
    {
        public static U Never<U>(U value) => value;
        public static U Echo<U>(U value) => value;
    }

    interface IDefinitionConstraint
    {
    }

    class DefinitionConstraintBase
    {
    }

    class DefinitionClassificationOwner
    {
        public static U Free<U>(U value) => value;
        public static U Value<U>(U value) where U : struct => value;
        public static U Reference<U>(U value) where U : class => value;

        public class Nested
        {
        }
    }

    class DefinitionSignatureHolder<T>
    {
        public T? Value = default;
    }

    class DefinitionSignatureClosureOwner
    {
        public static DefinitionSignatureHolder<int> Pick<U>(DefinitionSignatureHolder<int> values, U fallback) => values;
        public static Queue<int> QueuePick<U>(Queue<int> values, U fallback) => values;
    }

    class DefinitionSignatureOwner
    {
        public static U[] ArrayShape<U>(U[] value) => value;
        public static U RefShape<U>(ref U value) => value;
        public static List<U> ListShape<U>(List<U> value) => value;
        public static IEnumerable<U> EnumerableShape<U>(IEnumerable<U> value) => value;
        public static Func<U, bool> FunctionShape<U>(Func<U, bool> value) => value;
        public static Task<U> TaskShape<U>(Task<U> value) => value;
        public static U Unconstrained<U>(U value) => value;
        public static U ValueConstrained<U>(U value) where U : struct => value;
        public static U InterfaceConstrained<U>(U value) where U : IDefinitionConstraint => value;
        public static U ReferenceConstrained<U>(U value) where U : class => value;
        public static U BaseConstrained<U>(U value) where U : DefinitionConstraintBase => value;
    }

    struct DefinitionStruct
    {
        public string Stamp<U>(int seed) => typeof(U).Name + ":" + seed;
    }

    class DefinitionOwner<T>
    {
        public string Stamp<U>(int seed) => typeof(T).Name + "/" + typeof(U).Name + ":" + seed;
    }

    class DefinitionTemplate<T>
    {
        public string Kind() => typeof(T).Name;
        public string Never<U>(int seed) => typeof(T).Name + ":" + seed;
    }

    class BaseV
    {
        public virtual string V() => "BaseV.V";
        public virtual string W() => "BaseV.W";
        public string NV() => "BaseV.NV";
    }

    class MidV : BaseV
    {
        public override string V() => "MidV.V";
    }

    class DerV : MidV
    {
        public override string V() => "DerV.V";
        public new virtual string W() => "DerV.W";
    }

    static class Program
    {
        private const BindingFlags DefinitionFlags = BindingFlags.Public | BindingFlags.Instance
            | BindingFlags.Static | BindingFlags.DeclaredOnly;

        internal static void RunDefinitionLookups()
        {
            Console.WriteLine("== generic method definitions ==");
            Console.WriteLine("definition roots class=" + DefinitionClass.Stamp<int>(1) + "/"
                + DefinitionClass.Stamp<string>(2) + " echo=" + DefinitionClass.Echo<int>(7) + "/"
                + DefinitionClass.Echo<string>("echo") + " private=" + DefinitionClass.RootHidden());
            var value = new DefinitionStruct();
            Console.WriteLine("definition roots struct=" + value.Stamp<int>(1) + "/" + value.Stamp<string>(2));
            var numberOwner = new DefinitionOwner<int>();
            var textOwner = new DefinitionOwner<string>();
            Console.WriteLine("definition roots owners=" + numberOwner.Stamp<int>(1) + "/"
                + numberOwner.Stamp<string>(2) + "/" + textOwner.Stamp<int>(3) + "/"
                + textOwner.Stamp<string>(4));
            Console.WriteLine("definition roots overloads=" + DefinitionClass.Select(5) + "/"
                + DefinitionClass.Select<int>(6) + "/" + DefinitionClass.Select<int, string>("two"));

            CheckDefinition("class", typeof(DefinitionClass), "Stamp", DefinitionFlags,
                null, typeof(int), typeof(string), 9);
            CheckDefinition("struct", typeof(DefinitionStruct), "Stamp", DefinitionFlags,
                value, typeof(int), typeof(string), 9);
            CheckDefinition("owner-int", typeof(DefinitionOwner<int>), "Stamp", DefinitionFlags,
                numberOwner, typeof(int), typeof(string), 9);
            CheckDefinition("owner-string", typeof(DefinitionOwner<string>), "Stamp", DefinitionFlags,
                textOwner, typeof(int), typeof(string), 9);
            CheckDefinition("inherited", typeof(DefinitionChild), "Stamp",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
                null, typeof(int), typeof(string), 9);
            CheckDefinition("echo", typeof(DefinitionClass), "Echo", DefinitionFlags,
                null, typeof(int), typeof(string), 11);
            CheckDefinition("private", typeof(DefinitionClass), "Hidden",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly,
                null, typeof(int), typeof(int), 9);
            Console.WriteLine("definition private public lookup="
                + (typeof(DefinitionClass).GetMethod("Hidden") is not null));
            CheckDefinition("unreached", typeof(DefinitionClass), "Never", DefinitionFlags,
                null, null, null, 9);
            Type template = typeof(DefinitionTemplate<>).MakeGenericType(typeof(long));
            object receiver = Activator.CreateInstance(template)!;
            CheckDefinition("template-unreached", template, "Never", DefinitionFlags,
                receiver, null, null, 9);
            CheckDefinitionOverloads();

            MethodInfo? stamp = typeof(DefinitionClass).GetMethod("Stamp");
            MethodInfo? echo = typeof(DefinitionClass).GetMethod("Echo");
            if (stamp is not null && echo is not null)
            {
                Console.WriteLine("definition parameter methods distinct="
                    + !ReferenceEquals(stamp.GetGenericArguments()[0], echo.GetGenericArguments()[0]));
                Type parameter = stamp.GetGenericArguments()[0];
                Type inheritedParameter = typeof(DefinitionChild).GetMethod("Stamp",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!
                    .GetGenericArguments()[0];
                Console.WriteLine("definition parameter inherited: reference="
                    + ReferenceEquals(parameter, inheritedParameter) + " operator=" + (parameter == inheritedParameter));
                Type numberParameter = typeof(DefinitionOwner<int>).GetMethod("Stamp")!.GetGenericArguments()[0];
                Type textParameter = typeof(DefinitionOwner<string>).GetMethod("Stamp")!.GetGenericArguments()[0];
                Console.WriteLine("definition parameter owners: reference="
                    + ReferenceEquals(numberParameter, textParameter) + " operator=" + (numberParameter == textParameter));
            }
            Console.WriteLine("generic method definitions end");
        }

        private static void CheckDefinition(string label, Type owner, string name, BindingFlags flags,
            object? receiver, Type? known, Type? other, object value)
        {
            string prefix = "definition " + label;
            MethodInfo? method = owner.GetMethod(name, flags);
            Console.WriteLine(prefix + " found=" + (method is not null));
            if (method is null)
                return;
            try
            {
                int count = 0;
                bool allDefinitions = true;
                bool same = false;
                foreach (MethodInfo candidate in owner.GetMethods(flags))
                {
                    if (candidate.Name != name)
                        continue;
                    count++;
                    allDefinitions &= candidate.IsGenericMethodDefinition;
                    same |= ReferenceEquals(candidate, method);
                }
                Console.WriteLine(prefix + " list: count=" + count + " definitions=" + allDefinitions
                    + " identity=" + same);
                Console.WriteLine(prefix + " flags: generic=" + method.IsGenericMethod
                    + " definition=" + method.IsGenericMethodDefinition
                    + " contains=" + method.ContainsGenericParameters);
                Type[] args = method.GetGenericArguments();
                for (int i = 0; i < args.Length; i++)
                    Console.WriteLine(prefix + " parameter " + i + ": name=" + args[i].Name
                        + " generic=" + args[i].IsGenericParameter + " contains=" + args[i].ContainsGenericParameters
                        + " identity=" + ReferenceEquals(args[i], method.GetGenericArguments()[i]));
                Console.WriteLine(prefix + " declaring=" + (method.DeclaringType == owner)
                    + " reflected=" + (method.ReflectedType == owner));
                Console.WriteLine(prefix + " open invoke="
                    + DefinitionFault(() => method.Invoke(receiver, new[] { value })) + "/"
                    + DefinitionFault(() => method.Invoke(null, null)) + "/"
                    + DefinitionFault(() => method.Invoke(receiver, new object[] { "wrong" })));
                if (label == "echo" && args.Length == 1)
                    Console.WriteLine(prefix + " signature: return=" + ReferenceEquals(method.ReturnType, args[0])
                        + " parameter=" + ReferenceEquals(method.GetParameters()[0].ParameterType, args[0]));
                if (known is not null && other is not null)
                {
                    MethodInfo made = method.MakeGenericMethod(known);
                    MethodInfo another = method.MakeGenericMethod(other);
                    Console.WriteLine(prefix + " closed: definition=" + made.IsGenericMethodDefinition
                        + " contains=" + made.ContainsGenericParameters
                        + " argument=" + made.GetGenericArguments()[0].Name
                        + " result=" + made.Invoke(receiver, new[] { value }));
                    MethodInfo firstDefinition = made.GetGenericMethodDefinition();
                    MethodInfo secondDefinition = another.GetGenericMethodDefinition();
                    Console.WriteLine(prefix + " canonical: operator=" + (firstDefinition == secondDefinition)
                        + " equals=" + firstDefinition.Equals(secondDefinition)
                        + " reference=" + ReferenceEquals(firstDefinition, secondDefinition)
                        + " lookup=" + ReferenceEquals(method, firstDefinition)
                        + " self=" + ReferenceEquals(method, method.GetGenericMethodDefinition()));
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(prefix + " fault=" + error.GetType().Name);
            }
        }

        private static string DefinitionFault(Func<object?> action)
        {
            try
            {
                action();
                return "none";
            }
            catch (Exception error)
            {
                return error.GetType().Name;
            }
        }

        private static void CheckDefinitionOverloads()
        {
            Type owner = typeof(DefinitionClass);
            Console.WriteLine("definition overload name=" + DefinitionFault(() => owner.GetMethod("Select")));
            foreach (int arity in new[] { 0, 1, 2 })
            {
                Type[] types = arity == 2 ? new[] { typeof(string) } : new[] { typeof(int) };
                MethodInfo? method = owner.GetMethod("Select", arity, types);
                Console.WriteLine("definition overload arity=" + arity + " found=" + (method is not null)
                    + " definition=" + (method?.IsGenericMethodDefinition == true)
                    + " parameters=" + (method?.GetGenericArguments().Length ?? -1));
            }
        }

        internal static void RunDefinitionBoundaries()
        {
            Console.WriteLine("== generic method AOT instantiations ==");
            DefinitionBoundary("unreached", typeof(DefinitionClass), "Never", null, typeof(int), 8);
            DefinitionBoundary("missing-argument", typeof(DefinitionClass), "Stamp", null, typeof(bool), 8);
            Type template = typeof(DefinitionTemplate<>).MakeGenericType(typeof(long));
            DefinitionBoundary("template-unreached", template, "Never", Activator.CreateInstance(template),
                typeof(string), 8);
            Console.WriteLine("generic method AOT instantiations end");
        }

        private static void DefinitionBoundary(string label, Type owner, string name, object? receiver,
            Type argument, object value)
        {
            MethodInfo? method = owner.GetMethod(name);
            Console.WriteLine("definition boundary " + label + " found=" + (method is not null));
            try
            {
                MethodInfo made = method!.MakeGenericMethod(argument);
                Console.WriteLine("definition boundary " + label + " result=" + made.Invoke(receiver, new[] { value }));
            }
            catch (Exception error)
            {
                Console.WriteLine("definition boundary " + label + " fault=" + error.GetType().Name);
            }
        }

        internal static void RunFormalParameters()
        {
            Console.WriteLine("== formal method parameters ==");
            MethodInfo method = typeof(DefinitionClass).GetMethod("Echo")!;
            Type parameter = method.GetGenericArguments()[0];
            Console.WriteLine("formal names: name=" + parameter.Name + " full=" + (parameter.FullName ?? "<null>")
                + " qualified=" + (parameter.AssemblyQualifiedName ?? "<null>"));
            Console.WriteLine("formal namespaces: parameter=" + (parameter.Namespace ?? "<null>")
                + " declaring=" + (method.DeclaringType!.Namespace ?? "<null>"));
            Console.WriteLine("formal assembly owner="
                + (parameter.Assembly.GetName().Name == method.DeclaringType!.Assembly.GetName().Name));
            Type ownerParameter = typeof(DefinitionOwner<int>).GetMethod("Stamp")!.GetGenericArguments()[0];
            Type inheritedParameter = typeof(DefinitionChild).GetMethod("Stamp",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!.GetGenericArguments()[0];
            Console.WriteLine("formal assembly closed-owner="
                + (ownerParameter.Assembly.GetName().Name == typeof(DefinitionOwner<int>).Assembly.GetName().Name)
                + " inherited="
                + (inheritedParameter.Assembly.GetName().Name == typeof(DefinitionClass).Assembly.GetName().Name));
            Console.WriteLine("formal activation=" + DefinitionFault(() => Activator.CreateInstance(parameter)));
            Console.WriteLine("formal array allocation=" + DefinitionFault(() => Array.CreateInstance(parameter, 1)));
            Console.WriteLine("formal uninitialized=" + DefinitionFault(() => RuntimeHelpers.GetUninitializedObject(parameter)));

            MethodInfo? found = typeof(DefinitionClass).GetMethod("Echo", new[] { parameter });
            Console.WriteLine("formal signature: found=" + (found is not null)
                + " reference=" + ReferenceEquals(found, method));
            Type foreign = typeof(DefinitionClass).GetMethod("Never")!.GetGenericArguments()[0];
            Console.WriteLine("formal foreign signature found="
                + (typeof(DefinitionClass).GetMethod("Echo", new[] { foreign }) is not null));
            CheckMappedDefinition("definition", method, method);
            CheckMappedDefinition("closed", method.MakeGenericMethod(typeof(int)), method);
            Type globalParameter = typeof(global::FormalGlobalOwner).GetMethod("Echo")!.GetGenericArguments()[0];
            Console.WriteLine("formal global names: namespace=" + (globalParameter.Namespace ?? "<null>")
                + " name=" + globalParameter.Name);
            Console.WriteLine("formal method parameters end");
        }

        private static void CheckMappedDefinition(string label, MethodInfo source, MethodInfo definition)
        {
            MethodInfo mapped = (MethodInfo)typeof(DefinitionClass).GetMemberWithSameMetadataDefinitionAs(source);
            Console.WriteLine("formal mapped " + label + ": definition=" + mapped.IsGenericMethodDefinition
                + " reference=" + ReferenceEquals(mapped, definition) + " operator=" + (mapped == definition));
        }

        internal static void RunFormalTypeBoundaries()
        {
            Console.WriteLine("== formal type compositions ==");
            Type parameter = typeof(DefinitionClass).GetMethod("Never")!.GetGenericArguments()[0];
            FormalComposition("generic", () => typeof(DefinitionTemplate<>).MakeGenericType(parameter), true);
            FormalComposition("array", () => parameter.MakeArrayType(), false);
            FormalComposition("rank2-array", () => parameter.MakeArrayType(2), false);
            Console.WriteLine("formal type compositions end");
            Console.WriteLine("== formal method compositions ==");
            FormalMethodComposition("metadata-answer", typeof(Unsafe).GetMethod("SizeOf")!, parameter, null);
            FormalMethodComposition("compiled", typeof(DefinitionClass).GetMethod("Echo")!, parameter,
                new object[] { 7 });
            Console.WriteLine("formal method compositions end");
        }

        private static void FormalMethodComposition(string label, MethodInfo definition, Type parameter, object[]? args)
        {
            try
            {
                MethodInfo composed = definition.MakeGenericMethod(parameter);
                Console.WriteLine("formal method composition " + label + ": definition="
                    + composed.IsGenericMethodDefinition + " contains=" + composed.ContainsGenericParameters);
                Console.WriteLine("formal method composition " + label + " invoke="
                    + DefinitionFault(() => composed.Invoke(null, args)));
            }
            catch (Exception error)
            {
                Console.WriteLine("formal method composition " + label + " fault=" + error.GetType().Name);
            }
        }

        private static void FormalComposition(string label, Func<Type> action, bool activation)
        {
            try
            {
                Type composed = action();
                Console.WriteLine("formal composition " + label + " contains=" + composed.ContainsGenericParameters);
                if (activation)
                    Console.WriteLine("formal composition " + label + " activation="
                        + DefinitionFault(() => Activator.CreateInstance(composed)));
            }
            catch (Exception error)
            {
                Console.WriteLine("formal composition " + label + " fault=" + error.GetType().Name);
            }
        }

        internal static void RunMixedDefinitionLookups()
        {
            Console.WriteLine("== mixed generic method definitions ==");
            Console.WriteLine("mixed roots=" + MixedDefinitionOwner.Echo<int>(7) + "/"
                + MixedDefinitionOwner.Echo<string>("text"));
            foreach (string name in new[] { "Never", "Echo" })
            {
                MethodInfo lookup = typeof(MixedDefinitionOwner).GetMethod(name)!;
                int count = 0;
                foreach (MethodInfo method in typeof(MixedDefinitionOwner).GetMethods())
                    if (method.Name == name)
                        count++;
                Console.WriteLine("mixed " + name + " GetMethods count=" + count);
                CheckMixedMembers(name, "GetMember", lookup, typeof(MixedDefinitionOwner).GetMember(name));
                CheckMixedMembers(name, "GetMembers", lookup, typeof(MixedDefinitionOwner).GetMembers());
            }
            CheckFormalDeclaring("own", typeof(DefinitionClass).GetMethod("Echo")!, typeof(DefinitionClass));
            CheckFormalDeclaring("owner-int", typeof(DefinitionOwner<int>).GetMethod("Stamp")!, typeof(DefinitionOwner<>));
            CheckFormalDeclaring("owner-string", typeof(DefinitionOwner<string>).GetMethod("Stamp")!, typeof(DefinitionOwner<>));
            CheckFormalDeclaring("inherited", typeof(DefinitionChild).GetMethod("Stamp",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!, typeof(DefinitionClass));
            Console.WriteLine("mixed generic method definitions end");
        }

        private static void CheckMixedMembers(string name, string route, MethodInfo lookup, MemberInfo[] members)
        {
            string prefix = "mixed " + name + " " + route;
            int count = 0;
            foreach (MemberInfo member in members)
            {
                if (member is not MethodInfo method || method.Name != name)
                    continue;
                count++;
                Type[] parameters = method.GetGenericArguments();
                Console.WriteLine(prefix + " flags: generic=" + method.IsGenericMethod
                    + " definition=" + method.IsGenericMethodDefinition + " contains=" + method.ContainsGenericParameters);
                Console.WriteLine(prefix + " arguments=" + parameters.Length
                    + " name=" + (parameters.Length > 0 ? parameters[0].Name : "<none>"));
                Console.WriteLine(prefix + " identity: reference=" + ReferenceEquals(method, lookup)
                    + " operator=" + (method == lookup) + " equals=" + method.Equals(lookup));
                Type? parameter = parameters.Length > 0 ? parameters[0] : null;
                Console.WriteLine(prefix + " signature: return=" + ReferenceEquals(method.ReturnType, parameter)
                    + " parameter=" + ReferenceEquals(method.GetParameters()[0].ParameterType, parameter)
                    + " return-parameter=" + ReferenceEquals(method.ReturnParameter.ParameterType, parameter));
                Console.WriteLine(prefix + " invoke=" + DefinitionFault(() => method.Invoke(null, new object[] { 7 })));
            }
            Console.WriteLine(prefix + " count=" + count);
        }

        private static void CheckFormalDeclaring(string label, MethodInfo method, Type expected)
        {
            Type? declaring = method.GetGenericArguments()[0].DeclaringType;
            Console.WriteLine("formal declaring " + label + ": reference=" + ReferenceEquals(declaring, expected)
                + " operator=" + (declaring == expected) + " method-owner=" + (declaring == method.DeclaringType));
        }

        internal static void RunFormalReflectedOwners()
        {
            Console.WriteLine("== formal parameter reflected owners ==");
            CheckFormalReflected("own", typeof(DefinitionClass).GetMethod("Echo")!,
                typeof(DefinitionClass), typeof(DefinitionClass));
            CheckFormalReflected("owner-int", typeof(DefinitionOwner<int>).GetMethod("Stamp")!,
                typeof(DefinitionOwner<>), typeof(DefinitionOwner<int>));
            CheckFormalReflected("owner-string", typeof(DefinitionOwner<string>).GetMethod("Stamp")!,
                typeof(DefinitionOwner<>), typeof(DefinitionOwner<string>));
            CheckFormalReflected("inherited", typeof(DefinitionChild).GetMethod("Stamp",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!,
                typeof(DefinitionClass), typeof(DefinitionChild));
            CheckFormalReflected("global", typeof(global::FormalGlobalOwner).GetMethod("Echo")!,
                typeof(global::FormalGlobalOwner), typeof(global::FormalGlobalOwner));
            Console.WriteLine("formal parameter reflected owners end");
        }

        private static void CheckFormalReflected(string label, MethodInfo method, Type expected, Type lookupOwner)
        {
            Type parameter = method.GetGenericArguments()[0];
            Type? declaring = parameter.DeclaringType;
            Type? reflected = parameter.ReflectedType;
            Console.WriteLine("formal reflected " + label + ": declaring-reference=" + ReferenceEquals(declaring, expected)
                + " declaring-operator=" + (declaring == expected)
                + " reflected-reference=" + ReferenceEquals(reflected, expected)
                + " reflected-operator=" + (reflected == expected)
                + " same-reference=" + ReferenceEquals(declaring, reflected) + " same-operator=" + (declaring == reflected));
            Console.WriteLine("formal reflected " + label + " lookup: expected=" + ReferenceEquals(method.ReflectedType, lookupOwner)
                + " parameter-same=" + ReferenceEquals(reflected, method.ReflectedType));
        }

        internal static void RunFormalClassification()
        {
            Console.WriteLine("== formal method parameter classification ==");
            foreach (string name in new[] { "Free", "Value", "Reference" })
            {
                MethodInfo lookup = typeof(DefinitionClassificationOwner).GetMethod(name)!;
                CheckFormalClassification(name + " GetMethod", lookup.GetGenericArguments()[0]);
                int count = 0;
                foreach (MethodInfo method in typeof(DefinitionClassificationOwner).GetMethods())
                {
                    if (method.Name != name)
                        continue;
                    count++;
                    CheckFormalClassification(name + " GetMethods", method.GetGenericArguments()[0]);
                }
                Console.WriteLine("formal classification " + name + " GetMethods count=" + count);
                if (name == "Value")
                {
                    Type parameter = lookup.GetGenericArguments()[0];
                    Console.WriteLine("formal classification Value allocation: activation="
                        + DefinitionFault(() => Activator.CreateInstance(parameter))
                        + " array=" + DefinitionFault(() => Array.CreateInstance(parameter, 1))
                        + " uninitialized=" + DefinitionFault(() => RuntimeHelpers.GetUninitializedObject(parameter)));
                }
            }
            Console.WriteLine("formal method parameter classification end");
        }

        internal static void RunMetadataFormalAttributes()
        {
            Console.WriteLine("== metadata formal parameter attributes ==");
            CheckMetadataFormalAttributes("metadata", typeof(Unsafe).GetMethod("SizeOf")!);
            CheckMetadataFormalAttributes("ordinary", typeof(DefinitionClass).GetMethod("Echo")!);
            Console.WriteLine("metadata formal parameter attributes end");
        }

        private static void CheckMetadataFormalAttributes(string label, MethodInfo method)
        {
            Type parameter = method.GetGenericArguments()[0];
            Console.WriteLine("metadata formal " + label + ": name=" + parameter.Name
                + " attributes=" + (int)parameter.GenericParameterAttributes
                + " definition=" + method.IsGenericMethodDefinition + " contains=" + parameter.ContainsGenericParameters
                + " nested=" + parameter.IsNested + " value=" + parameter.IsValueType + " class=" + parameter.IsClass);
            Type definition = method.GetGenericMethodDefinition().GetGenericArguments()[0];
            Console.WriteLine("metadata formal " + label + " definition: attributes=" + (int)definition.GenericParameterAttributes
                + " canonical=" + ReferenceEquals(definition, parameter));
            Type remade = method.MakeGenericMethod(typeof(int)).GetGenericMethodDefinition().GetGenericArguments()[0];
            Console.WriteLine("metadata formal " + label + " closed definition: attributes=" + (int)remade.GenericParameterAttributes
                + " canonical=" + ReferenceEquals(remade, parameter));
        }

        internal static void RunFormalMemberTypes()
        {
            Console.WriteLine("== formal parameter member types ==");
            CheckFormalMemberType("free", typeof(DefinitionClassificationOwner).GetMethod("Free")!.GetGenericArguments()[0]);
            CheckFormalMemberType("value", typeof(DefinitionClassificationOwner).GetMethod("Value")!.GetGenericArguments()[0]);
            CheckFormalMemberType("reference", typeof(DefinitionClassificationOwner).GetMethod("Reference")!.GetGenericArguments()[0]);
            CheckFormalMemberType("metadata", typeof(Unsafe).GetMethod("SizeOf")!.GetGenericArguments()[0]);
            CheckFormalMemberType("nested", typeof(DefinitionClassificationOwner.Nested));
            CheckFormalMemberType("top-level", typeof(DefinitionClassificationOwner));
            Console.WriteLine("formal parameter member types end");
        }

        private static void CheckFormalMemberType(string label, Type type)
        {
            Console.WriteLine("formal member " + label + ": type=" + (int)type.MemberType
                + " memberinfo=" + (int)((MemberInfo)type).MemberType + " nested=" + type.IsNested);
        }

        internal static void RunDefinitionSignatureClosure()
        {
            Console.WriteLine("== definition signature closure ==");
            foreach (string name in new[] { "Pick", "QueuePick" })
            {
                MethodInfo? lookup = typeof(DefinitionSignatureClosureOwner).GetMethod(name);
                if (lookup is not null)
                    CheckDefinitionSignatureClosure(name + " GetMethod", lookup);
                else
                    Console.WriteLine("signature closure " + name + " GetMethod missing");
                int count = 0;
                foreach (MethodInfo method in typeof(DefinitionSignatureClosureOwner).GetMethods())
                {
                    if (method.Name != name)
                        continue;
                    count++;
                    CheckDefinitionSignatureClosure(name + " GetMethods", method);
                }
                Console.WriteLine("signature closure " + name + " GetMethods count=" + count);
            }
            Console.WriteLine("definition signature closure end");
        }

        private static void CheckDefinitionSignatureClosure(string label, MethodInfo method)
        {
            Console.WriteLine("signature closure " + label + " definition=" + method.IsGenericMethodDefinition);
            CheckClosedSignatureType(label + " return", () => method.ReturnType);
            CheckClosedSignatureType(label + " parameter", () => method.GetParameters()[0].ParameterType);
            CheckClosedSignatureType(label + " return-parameter", () => method.ReturnParameter.ParameterType);
            Console.WriteLine("signature closure " + label + " identity: parameter="
                + ReferenceEquals(method.ReturnType, method.GetParameters()[0].ParameterType)
                + " return-parameter=" + ReferenceEquals(method.ReturnType, method.ReturnParameter.ParameterType));
        }

        private static void CheckClosedSignatureType(string label, Func<Type> action)
        {
            try
            {
                Type type = action();
                Console.WriteLine("signature closure " + label + ": name=" + type.Name
                    + " full=" + (type.FullName ?? "<null>") + " generic=" + type.IsGenericType);
            }
            catch (Exception error)
            {
                Console.WriteLine("signature closure " + label + " fault=" + error.GetType().Name);
            }
        }

        private static void CheckFormalClassification(string label, Type parameter)
        {
            Console.WriteLine("formal classification " + label + ": nested=" + parameter.IsNested
                + " value=" + parameter.IsValueType + " class=" + parameter.IsClass
                + " base=" + parameter.BaseType?.FullName + " interfaces=" + parameter.GetInterfaces().Length);
        }

        internal static void RunSignatureBoundaries()
        {
            Console.WriteLine("== generic signature type boundaries ==");
            int value = 7;
            var list = new List<int> { 7 };
            Func<int, bool> function = DefinitionPositive;
            Task<int> task = Task.FromResult(7);
            Console.WriteLine("signature roots: array=" + DefinitionSignatureOwner.ArrayShape<int>(new[] { 7 }).Length
                + " byref=" + DefinitionSignatureOwner.RefShape<int>(ref value)
                + " list=" + DefinitionSignatureOwner.ListShape<int>(list).Count
                + " enumerable=" + ReferenceEquals(DefinitionSignatureOwner.EnumerableShape<int>(list), list)
                + " function=" + ReferenceEquals(DefinitionSignatureOwner.FunctionShape<int>(function), function)
                + " task=" + ReferenceEquals(DefinitionSignatureOwner.TaskShape<int>(task), task));
            foreach (string name in new[] { "ArrayShape", "RefShape", "ListShape", "EnumerableShape", "FunctionShape", "TaskShape" })
            {
                MethodInfo lookup = typeof(DefinitionSignatureOwner).GetMethod(name)!;
                CheckSignatureTypes(name + " GetMethod", lookup);
                int count = 0;
                foreach (MethodInfo method in typeof(DefinitionSignatureOwner).GetMethods())
                {
                    if (method.Name != name)
                        continue;
                    count++;
                    CheckSignatureTypes(name + " GetMethods", method);
                }
                Console.WriteLine("signature " + name + " GetMethods count=" + count);
                MethodInfo closed = lookup.MakeGenericMethod(typeof(int));
                CheckSignatureTypes(name + " closed", closed);
                if (name == "RefShape")
                {
                    object[] args = { 7 };
                    Console.WriteLine("signature closed byref invoke=" + closed.Invoke(null, args) + " value=" + args[0]);
                }
            }
            foreach (string name in new[] { "Unconstrained", "ValueConstrained", "InterfaceConstrained", "ReferenceConstrained", "BaseConstrained" })
            {
                try
                {
                    Type parameter = typeof(DefinitionSignatureOwner).GetMethod(name)!.GetGenericArguments()[0];
                    Type[] constraints = parameter.GetGenericParameterConstraints();
                    Console.WriteLine("signature constraints " + name + " count=" + constraints.Length);
                    foreach (Type constraint in constraints)
                        Console.WriteLine("signature constraints " + name + " type=" + constraint);
                }
                catch (Exception error)
                {
                    Console.WriteLine("signature constraints " + name + " fault=" + error.GetType().Name);
                }
            }
            foreach (string name in new[] { "BaseConstrained", "InterfaceConstrained" })
            {
                Type parameter = typeof(DefinitionSignatureOwner).GetMethod(name)!.GetGenericArguments()[0];
                Console.WriteLine("signature constrained " + name + " base=" + parameter.BaseType?.FullName);
                Type[] interfaces = parameter.GetInterfaces();
                Console.WriteLine("signature constrained " + name + " interfaces=" + interfaces.Length);
                foreach (Type type in interfaces)
                    Console.WriteLine("signature constrained " + name + " interface=" + type.FullName);
            }
            Console.WriteLine("generic signature type boundaries end");
        }

        private static bool DefinitionPositive(int value) => value > 0;

        private static void CheckSignatureTypes(string label, MethodInfo method)
        {
            Console.WriteLine("signature " + label + " definition=" + method.IsGenericMethodDefinition
                + " contains=" + method.ContainsGenericParameters);
            Console.WriteLine("signature " + label + " return=" + DefinitionTypeResult(() => method.ReturnType));
            Console.WriteLine("signature " + label + " parameter=" + DefinitionTypeResult(() => method.GetParameters()[0].ParameterType));
            Console.WriteLine("signature " + label + " return-parameter=" + DefinitionTypeResult(() => method.ReturnParameter.ParameterType));
        }

        private static string DefinitionTypeResult(Func<Type> action)
        {
            try
            {
                Type type = action();
                return "type:" + type + "|parameter=" + type.IsGenericParameter + "|contains=" + type.ContainsGenericParameters;
            }
            catch (Exception error)
            {
                return "fault:" + error.GetType().Name;
            }
        }

        internal static void Run()
        {
            // Put the closed instantiations in the image (the AOT resolution set).
            Console.WriteLine($"direct => {Helper.Describe<int>(42)}");
            Console.WriteLine($"direct2 => {Helper.TwoArg<int, string>(10)}");
            Console.WriteLine($"direct3 => {new Box().Tag<double>(1.5)}");

            MethodInfo describe = typeof(Helper).GetMethod("Describe")!;
            Console.WriteLine($"gm-isgeneric => {describe.IsGenericMethod}");

            // MakeGenericMethod over an in-image instantiation, then invoke it.
            MethodInfo closed = describe.MakeGenericMethod(typeof(int));
            Console.WriteLine($"gm-make => {closed.Invoke(null, new object[] { 7 })}");
            Console.WriteLine($"gm-make-args => {closed.GetGenericArguments()[0].Name}");
            Console.WriteLine($"gm-make-isdef => {closed.IsGenericMethodDefinition}");

            // Definition round-trip: the definitions reached from two different
            // handles of the same method compare equal.
            MethodInfo def1 = describe.GetGenericMethodDefinition();
            MethodInfo def2 = closed.GetGenericMethodDefinition();
            Console.WriteLine($"gm-def-eq => {def1 == def2}");
            Console.WriteLine($"gm-def-isdef => {def1.IsGenericMethodDefinition}");
            Console.WriteLine($"gm-def-name => {def1.Name}");

            // MakeGenericMethod on the definition view re-resolves in-image.
            MethodInfo remade = def1.MakeGenericMethod(typeof(int));
            Console.WriteLine($"gm-remake => {remade.Invoke(null, new object[] { 9 })}");

            // Two-parameter arity + wrong-arity ArgumentException.
            MethodInfo two = typeof(Helper).GetMethod("TwoArg")!;
            MethodInfo twoClosed = two.MakeGenericMethod(typeof(int), typeof(string));
            Console.WriteLine($"gm-two => {twoClosed.Invoke(null, new object[] { 100 })}");
            try
            {
                two.MakeGenericMethod(typeof(int));
                Console.WriteLine("gm-arity => no exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("gm-arity => ArgumentException");
            }

            // Non-generic receivers reject the generic-method surface.
            MethodInfo plain = typeof(Helper).GetMethod("Plain")!;
            Console.WriteLine($"gm-plain-isgeneric => {plain.IsGenericMethod}");
            try
            {
                plain.MakeGenericMethod(typeof(int));
                Console.WriteLine("gm-plain-make => no exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("gm-plain-make => InvalidOperationException");
            }
            try
            {
                plain.GetGenericMethodDefinition();
                Console.WriteLine("gm-plain-def => no exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("gm-plain-def => InvalidOperationException");
            }
            Console.WriteLine($"gm-plain-args => {plain.GetGenericArguments().Length}");

            // Instance generic method through the same route.
            MethodInfo tag = typeof(Box).GetMethod("Tag")!.MakeGenericMethod(typeof(double));
            Console.WriteLine($"gm-instance => {tag.Invoke(new Box(), new object[] { 2.5 })}");

            // GetBaseDefinition: override chains resolve to the root declaration,
            // a `new` slot roots at its own declaration, non-virtuals are their
            // own base definition.
            MethodInfo derV = typeof(DerV).GetMethod("V")!;
            Console.WriteLine($"basedef-override => {derV.GetBaseDefinition().DeclaringType!.Name}");
            MethodInfo midV = typeof(MidV).GetMethod("V")!;
            Console.WriteLine($"basedef-mid => {midV.GetBaseDefinition().DeclaringType!.Name}");
            Console.WriteLine($"basedef-eq => {derV.GetBaseDefinition() == typeof(BaseV).GetMethod("V")}");
            MethodInfo derW = typeof(DerV).GetMethod("W",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
            Console.WriteLine($"basedef-new => {derW.GetBaseDefinition().DeclaringType!.Name}");
            MethodInfo baseW = typeof(BaseV).GetMethod("W")!;
            Console.WriteLine($"basedef-root => {baseW.GetBaseDefinition().DeclaringType!.Name}");
            MethodInfo nv = typeof(DerV).GetMethod("NV")!;
            Console.WriteLine($"basedef-nonvirtual => {nv.GetBaseDefinition().DeclaringType!.Name}");
        }
    }
}
