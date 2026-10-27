using System;
using System.Globalization;
using System.Reflection;

namespace AmbiguousMatchFactorySubset;

internal interface IMarker<T> { }
internal sealed class BothMarkers : IMarker<int>, IMarker<string> { }

internal static class Program
{
    private sealed class Overloads
    {
        public void Pick(int value) { }
        public void Pick(string value) { }
        [Probe, Probe] public void Tagged() { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    private sealed class ProbeAttribute : Attribute
    {
        public override string ToString()
        {
            s_formatCalls++;
            if (s_formatMode == 2) throw s_formatError;
            return s_formatMode == 1 ? null : "attribute " + 1.5.ToString("F1");
        }
    }

    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
    private static int s_formatMode;
    private static int s_formatCalls;
    private static readonly InvalidOperationException s_formatError = new InvalidOperationException("attribute text failed");
    private static string s_memberMessage;
    private static string s_typeMessage;
    private static string s_attributeMessage;
    private static string s_emptyAttributeMessage;

    private static void Check(string label, bool success)
    {
        Console.WriteLine("ambiguous-factory-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static bool IsAmbiguous(Exception error) => error.GetType() == typeof(AmbiguousMatchException)
        && error.HResult == unchecked((int)0x8000211D) && error.InnerException is null;

    private static Exception Error(Action call)
    {
        try { call(); }
        catch (Exception error) { return error; }
        return null;
    }

    private static Exception PublicAttributeError() => Error(() => Attribute.GetCustomAttribute(
        typeof(Overloads).GetMethod("Tagged"), typeof(ProbeAttribute)));

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Managed ambiguous-match factories ==");
        Type helper = Type.GetType("System.ThrowHelper");
        foreach (Type parameter in new[] { typeof(MemberInfo), typeof(Attribute) })
        {
            MethodInfo factory = helper.GetMethod("GetAmbiguousMatchException", Flags, new[] { parameter });
            Check(parameter.Name + "-shape", factory.IsAssembly && factory.IsStatic && !factory.IsGenericMethod
                && factory.ReturnType == typeof(AmbiguousMatchException)
                && (factory.Attributes & MethodAttributes.PinvokeImpl) == 0
                && (factory.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) == 0);
        }
        MethodInfo member = typeof(Overloads).GetMethod("Pick", new[] { typeof(int) });
        Exception error = Error(() => typeof(Overloads).GetMethod("Pick"));
        s_memberMessage = error.Message;
        Check("public-member", IsAmbiguous(error)
            && error.Message == "Ambiguous match found for '" + member.DeclaringType + " " + member + "'.");
        error = Error(() => typeof(BothMarkers).GetInterface("IMarker`1"));
        s_typeMessage = error.Message;
        Check("public-null-declaring-type", IsAmbiguous(error)
            && error.Message == "Ambiguous match found for ' " + typeof(IMarker<int>) + "'.");
        s_formatMode = 0;
        s_formatCalls = 0;
        error = PublicAttributeError();
        s_attributeMessage = error.Message;
        Check("public-attribute", IsAmbiguous(error) && s_formatCalls == 1
            && error.Message == "Multiple custom attributes of the same type 'attribute 1.5' found.");
        s_formatMode = 1;
        s_formatCalls = 0;
        error = PublicAttributeError();
        s_emptyAttributeMessage = error.Message;
        Check("public-null-text", IsAmbiguous(error) && s_formatCalls == 1
            && error.Message == "Multiple custom attributes of the same type '' found.");
        s_formatMode = 2;
        s_formatCalls = 0;
        error = PublicAttributeError();
        Check("public-text-fault", ReferenceEquals(error, s_formatError) && s_formatCalls == 1);
        s_formatMode = 0;
        Console.WriteLine("Managed ambiguous-match factories contract end");
    }

    // The fixture supplies exact CoreLib call/calli sites; these entry points are native-only.
    private static AmbiguousMatchException MemberDirect(MemberInfo member) => throw new InvalidOperationException("fixture missing");
    private static AmbiguousMatchException MemberPointer(MemberInfo member) => throw new InvalidOperationException("fixture missing");
    private static AmbiguousMatchException AttributeDirect(Attribute attribute) => throw new InvalidOperationException("fixture missing");
    private static AmbiguousMatchException AttributePointer(Attribute attribute) => throw new InvalidOperationException("fixture missing");
    private static AmbiguousMatchException MemberReflection(MemberInfo member) =>
        (AmbiguousMatchException)Type.GetType("System.ThrowHelper").GetMethod("GetAmbiguousMatchException", Flags,
            new[] { typeof(MemberInfo) }).Invoke(null, new object[] { member });
    private static AmbiguousMatchException AttributeReflection(Attribute attribute) =>
        (AmbiguousMatchException)Type.GetType("System.ThrowHelper").GetMethod("GetAmbiguousMatchException", Flags,
            new[] { typeof(Attribute) }).Invoke(null, new object[] { attribute });

    private static bool Returned(AmbiguousMatchException error, string message) =>
        error is not null && IsAmbiguous(error) && error.Message == message && error.StackTrace is null;

    private static Exception Unwrap(Exception error, bool wrapped) =>
        wrapped ? error is TargetInvocationException invocation ? invocation.InnerException : null
            : error is TargetInvocationException ? null : error;

    private static void RunRoute(string route, Func<MemberInfo, AmbiguousMatchException> memberFactory,
        Func<Attribute, AmbiguousMatchException> attributeFactory)
    {
        bool wrapped = route == "reflection";
        MemberInfo member = typeof(Overloads).GetMethod("Pick", new[] { typeof(int) });
        AmbiguousMatchException result = memberFactory(member);
        Check(route + "-member-return", Returned(result, s_memberMessage));
        Check(route + "-member-fresh", !ReferenceEquals(result, memberFactory(member)));
        Check(route + "-type-return", Returned(memberFactory(typeof(IMarker<int>)), s_typeMessage));
        Exception error = Error(() => memberFactory(null));
        Check(route + "-member-null", Unwrap(error, wrapped) is NullReferenceException);
        Check(route + "-caller-throw", ReferenceEquals(Error(() => throw result), result));
        s_formatMode = 0;
        s_formatCalls = 0;
        result = attributeFactory(new ProbeAttribute());
        Check(route + "-attribute-return", Returned(result, s_attributeMessage) && s_formatCalls == 1);
        Check(route + "-attribute-fresh", !ReferenceEquals(result, attributeFactory(new ProbeAttribute())) && s_formatCalls == 2);
        s_formatCalls = 0;
        Check(route + "-attribute-null", Returned(attributeFactory(null), s_emptyAttributeMessage) && s_formatCalls == 0);
        s_formatMode = 1;
        s_formatCalls = 0;
        result = attributeFactory(new ProbeAttribute());
        Check(route + "-attribute-null-text", Returned(result, s_emptyAttributeMessage) && s_formatCalls == 1);
        s_formatMode = 2;
        s_formatCalls = 0;
        error = Error(() => attributeFactory(new ProbeAttribute()));
        Check(route + "-attribute-text-fault", ReferenceEquals(Unwrap(error, wrapped), s_formatError) && s_formatCalls == 1);
        s_formatMode = 0;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        try
        {
            s_formatCalls = 0;
            result = attributeFactory(new ProbeAttribute());
            Check(route + "-attribute-culture", Returned(result,
                "Multiple custom attributes of the same type 'attribute 1,5' found.") && s_formatCalls == 1);
        }
        finally { CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; }
    }

    internal static void RunArtifactChecks(string[] args)
    {
        Run();
        RunRoute("direct", MemberDirect, AttributeDirect);
        RunRoute("pointer", MemberPointer, AttributePointer);
        RunRoute("reflection", MemberReflection, AttributeReflection);
        Console.WriteLine("Managed ambiguous-match factories artifact end");
    }

    internal static void RunReflectionChecks(string[] args)
    {
        Run();
        RunRoute("reflection", MemberReflection, AttributeReflection);
        Console.WriteLine("Managed ambiguous-match factories artifact end");
    }
}
