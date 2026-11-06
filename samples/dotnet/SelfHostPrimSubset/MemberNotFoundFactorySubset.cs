using System;
using System.Globalization;
using System.Reflection;

namespace MemberNotFoundFactorySubset;

internal static class Program
{
    private sealed class Source
    {
        public Source() { }
        public int Field = 1;
        public int Value { get; set; }
        public event Action Changed { add { } remove { } }
        public void Method() { }
        public sealed class Nested { }
    }

    private sealed class Target { }

    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
    private const string FactoryName = "CreateGetMemberWithSameMetadataDefinitionAsNotFoundException";
    private static readonly string[] s_kinds = { "method", "field", "property", "event", "constructor", "type" };
    private static readonly string[] s_messages = new string[6];

    private static MemberInfo[] Members() => new MemberInfo[]
    {
        typeof(Source).GetMethod("Method"),
        typeof(Source).GetField("Field"),
        typeof(Source).GetProperty("Value"),
        typeof(Source).GetEvent("Changed"),
        typeof(Source).GetConstructor(Type.EmptyTypes),
        typeof(Source.Nested),
    };

    private static void Check(string label, bool success)
    {
        Console.WriteLine("member-not-found-factory-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static bool IsMemberError(Exception error) => error is ArgumentException argument
        && error.GetType() == typeof(ArgumentException) && error.HResult == unchecked((int)0x80070057)
        && argument.ParamName == "member" && error.InnerException is null;

    private static Exception Error(Action call)
    {
        try { call(); }
        catch (Exception error) { return error; }
        return null;
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Managed member-not-found factory ==");
        MethodInfo factory = typeof(Type).GetMethod(FactoryName, Flags, new[] { typeof(MemberInfo) });
        ParameterInfo parameter = factory.GetParameters()[0];
        Check("shape", factory.IsFamilyAndAssembly && factory.IsStatic && !factory.IsGenericMethod
            && factory.ReturnType == typeof(ArgumentException) && !parameter.IsOptional && !parameter.HasDefaultValue
            && (factory.Attributes & MethodAttributes.PinvokeImpl) == 0
            && (factory.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) == 0);
        Check("lookup-identity", ReferenceEquals(factory,
            typeof(Type).GetMethod(FactoryName, Flags, new[] { typeof(MemberInfo) }))
            && factory.DeclaringType == typeof(Type) && factory.ReflectedType == typeof(Type));
        Check("lookup-public", typeof(Type).GetMethod(FactoryName, BindingFlags.Public | BindingFlags.Static,
            new[] { typeof(MemberInfo) }) is null);
        Check("lookup-instance", typeof(Type).GetMethod(FactoryName, BindingFlags.NonPublic | BindingFlags.Instance,
            new[] { typeof(MemberInfo) }) is null);
        Check("lookup-parameter", typeof(Type).GetMethod(FactoryName, Flags, new[] { typeof(object) }) is null);
        Check("lookup-generic-arity", typeof(Type).GetMethod(FactoryName, 1, Flags, new[] { typeof(MemberInfo) }) is null);
        Check("lookup-ignore-case", ReferenceEquals(factory, typeof(Type).GetMethod(
            "creategetmemberwithsamemetadatadefinitionasnotfoundexception", Flags | BindingFlags.IgnoreCase,
            new[] { typeof(MemberInfo) })));
        Check("lookup-owner", typeof(MemberInfo).GetMethod(FactoryName, Flags, new[] { typeof(MemberInfo) }) is null);
        Check("type-identity", typeof(Type).FullName == "System.Type" && typeof(Type).Name == "Type"
            && typeof(Type).BaseType == typeof(MemberInfo)
            && Type.GetTypeFromHandle(typeof(Type).TypeHandle) == typeof(Type));
        MemberInfo[] members = Members();
        for (int i = 0; i < members.Length; i++)
        {
            MemberInfo member = members[i];
            Exception error = Error(() => typeof(Target).GetMemberWithSameMetadataDefinitionAs(member));
            s_messages[i] = error.Message;
            Check("public-" + s_kinds[i], IsMemberError(error)
                && error.Message == "A MemberInfo that matches '" + member.Name + "' could not be found. (Parameter 'member')");
        }
        MemberInfo found = typeof(Source).GetMemberWithSameMetadataDefinitionAs(members[0]);
        Check("public-success", found.Name == "Method" && found.MemberType == MemberTypes.Method);
        Exception nullError = Error(() => typeof(Target).GetMemberWithSameMetadataDefinitionAs(null));
        Check("public-null", nullError is ArgumentNullException argument && argument.ParamName == "member"
            && nullError.HResult == unchecked((int)0x80004003) && nullError.InnerException is null);
        Console.WriteLine("Managed member-not-found factory contract end");
    }

    // The fixture supplies exact CoreLib call/calli sites; these entry points are native-only.
    private static ArgumentException Direct(MemberInfo member) => throw new InvalidOperationException("fixture missing");
    private static ArgumentException Pointer(MemberInfo member) => throw new InvalidOperationException("fixture missing");
    private static ArgumentException Reflection(MemberInfo member) =>
        (ArgumentException)typeof(Type).GetMethod(FactoryName, Flags, new[] { typeof(MemberInfo) })
            .Invoke(null, new object[] { member });

    private static bool Returned(ArgumentException error, string message) =>
        error is not null && IsMemberError(error) && error.Message == message && error.StackTrace is null;

    private static Exception Unwrap(Exception error, bool wrapped) =>
        wrapped ? error is TargetInvocationException invocation ? invocation.InnerException : null
            : error is TargetInvocationException ? null : error;

    private static void RunRoute(string route, Func<MemberInfo, ArgumentException> factory)
    {
        MemberInfo[] members = Members();
        for (int i = 0; i < members.Length; i++)
        {
            MemberInfo member = members[i];
            ArgumentException result = factory(member);
            string label = route + "-" + s_kinds[i];
            Check(label + "-return", Returned(result, s_messages[i]));
            Check(label + "-fresh", !ReferenceEquals(result, factory(member)));
            Check(label + "-caller-throw", ReferenceEquals(Error(() => throw result), result));
        }
        Check(route + "-null", Unwrap(Error(() => factory(null)), route == "reflection") is NullReferenceException);
    }

    private static void RunDelegateChecks()
    {
        MethodInfo factory = typeof(Type).GetMethod(FactoryName, Flags, new[] { typeof(MemberInfo) });
        var bound = (Func<MemberInfo, ArgumentException>)factory.CreateDelegate(typeof(Func<MemberInfo, ArgumentException>));
        Check("delegate-method", ReferenceEquals(bound.Method, factory) && bound.Target is null);
        MemberInfo member = typeof(Source).GetMethod("Method");
        ArgumentException result = bound(member);
        Check("delegate-return", Returned(result, s_messages[0]));
        Check("delegate-fresh", !ReferenceEquals(result, bound(member)));
    }

    internal static void RunArtifactChecks(string[] args)
    {
        Run();
        RunRoute("direct", Direct);
        RunRoute("pointer", Pointer);
        RunRoute("reflection", Reflection);
        RunDelegateChecks();
        Console.WriteLine("Managed member-not-found factory artifact end");
    }

    internal static void RunReflectionChecks(string[] args)
    {
        Run();
        RunRoute("reflection", Reflection);
        Console.WriteLine("Managed member-not-found factory artifact end");
    }
}
