using System;
using System.Globalization;
using System.Reflection;

namespace MetadataDefinitionRoutes;

public static class Program
{
    // The artifact fixture supplies call and bound-instance ldftn sites to the public base method.
    public static bool Direct(MemberInfo receiver, MemberInfo other) =>
        throw new InvalidOperationException("metadata-definition fixture missing");
    public static Func<MemberInfo, bool> Address(MemberInfo receiver) => receiver.HasSameMetadataDefinitionAs;

    private static void Check(string label, bool success)
    {
        Console.WriteLine("metadata-definition-route-" + label + ":" + success);
        if (!success)
            throw new InvalidOperationException(label);
    }

    private static Exception? Error(Action action)
    {
        try { action(); }
        catch (Exception error) { return error; }
        return null;
    }

    private static bool IsBaseError(Exception? error) => error is NotImplementedException
        && error.GetType() == typeof(NotImplementedException) && error.HResult == unchecked((int)0x80004001)
        && error.InnerException is null;

    public static void RunExactRoutes()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== member metadata definition exact routes ==");
        MemberInfo member = typeof(int);
        Check("direct", IsBaseError(Error(() => Direct(member, member))));
        Check("direct-other-null", IsBaseError(Error(() => Direct(member, null!))));
        Check("direct-receiver-null", IsBaseError(Error(() => Direct(null!, member))));
        Check("direct-both-null", IsBaseError(Error(() => Direct(null!, null!))));
        Func<MemberInfo, bool> bound = Address(member);
        Check("address", IsBaseError(Error(() => bound(member))));
        Check("address-other-null", IsBaseError(Error(() => bound(null!))));
        Check("address-receiver-null", Error(() => Address(null!)) is ArgumentException nullTarget
            && nullTarget.GetType() == typeof(ArgumentException) && nullTarget.ParamName is null);
        Check("virtual", member.HasSameMetadataDefinitionAs(typeof(int))
            && !member.HasSameMetadataDefinitionAs(typeof(string)));
        Check("virtual-kind", !member.HasSameMetadataDefinitionAs(typeof(object).GetMethod("GetType")!));
        Check("virtual-other-null", Error(() => member.HasSameMetadataDefinitionAs(null!)) is ArgumentNullException argument
            && argument.ParamName == "other");
        Check("virtual-receiver-null", Error(() => ((MemberInfo)null!).HasSameMetadataDefinitionAs(member)) is NullReferenceException);
        Check("virtual-both-null", Error(() => ((MemberInfo)null!).HasSameMetadataDefinitionAs(null!)) is NullReferenceException);
        Console.WriteLine("member metadata definition exact routes end");
    }

    public static void RunNativeRoutes()
    {
        Console.WriteLine("== member metadata definition reflection boundary ==");
        MethodInfo? named = null;
        Exception? namedError = Error(() => named = typeof(MemberInfo).GetMethod("HasSameMetadataDefinitionAs",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, new[] { typeof(MemberInfo) }));
        if (namedError is null && named is not null)
        {
            namedError = Error(() => named.Invoke(typeof(int), new object[] { typeof(int) }));
            if (namedError is TargetInvocationException invocation)
                namedError = invocation.InnerException;
        }
        Check("named-reflection-unavailable", namedError is PlatformNotSupportedException
            || namedError is null && named is null);
        Func<MemberInfo, bool> bound = Address(typeof(int));
        MethodInfo? method = null;
        Exception? methodError = Error(() => method = bound.Method);
        if (methodError is null && method is not null)
        {
            methodError = Error(() => method.Invoke(typeof(int), new object[] { typeof(int) }));
            if (methodError is TargetInvocationException invocation)
                methodError = invocation.InnerException;
        }
        Check("delegate-reflection-refuses", methodError is PlatformNotSupportedException);
        MemberInfo receiver = typeof(int);
        Func<MemberInfo, bool> virtualBound = receiver.HasSameMetadataDefinitionAs;
        method = null;
        methodError = Error(() => method = virtualBound.Method);
        Check("virtual-delegate-reflection", methodError is PlatformNotSupportedException
            || methodError is null && method is not null && method.DeclaringType != typeof(MemberInfo)
                && (bool)method.Invoke(receiver, new object[] { receiver })!);
        Console.WriteLine("member metadata definition reflection boundary end");
    }

    public static void RunArtifactChecks(string[] args)
    {
        RunExactRoutes();
        if (args.Length > 0 && args[0] == "native-boundary")
            RunNativeRoutes();
    }
}
