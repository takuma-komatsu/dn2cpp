using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Principal;

namespace DefaultPrincipalSubset;

internal sealed class UserIdentity : IIdentity
{
    public string Name => "explicit-user";
    public string AuthenticationType => "fixture";
    public bool IsAuthenticated => true;
}
internal sealed class UserPrincipal : IPrincipal
{
    private readonly IIdentity identity = new UserIdentity();
    public IIdentity Identity => identity;
    public bool IsInRole(string role) => role == "explicit-role";
}
internal static class Program
{
    // Native-only rewritten stubs; no GenericPrincipal reference or Claims assembly reference.
    private static object Direct(object marker) => throw new InvalidOperationException("native fixture required");
    private static object Pointer(object marker) => throw new InvalidOperationException("native fixture required");
    private static Func<object, object> Address() => Direct;
    private static object DelegateRoute(object marker) => Address()(marker);
    private static object Reflect(object marker) => typeof(AppDomain).GetMethod("<GetThreadPrincipal>g__GetDefaultPrincipal|107_0", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { marker });
    private static IPrincipal GetPrincipal(AppDomain domain) => throw new InvalidOperationException("native fixture required");
    private static int Trace;
    private static object Marker(bool throwing)
    {
        Trace++;
        if (throwing) throw new FormatException("marker producer");
        return new object();
    }
    private static object Produced(bool throwing) => Direct(Marker(throwing));
    private static void Culture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
    private static void Cases(string route, Func<object, object> call)
    {
        foreach (bool nonnull in new[] { false, true })
        {
            object marker = nonnull ? new object() : null;
            object first = call(marker), second = call(marker);
            var principal = (IPrincipal)first;
            bool ok = first.GetType().FullName == "System.Security.Principal.GenericPrincipal"
                && principal.Identity.GetType().FullName == "System.Security.Principal.GenericIdentity"
                && principal.Identity.Name == "" && principal.Identity.AuthenticationType == "" && !principal.Identity.IsAuthenticated
                && principal.IsInRole("") && !principal.IsInRole("admin")
                && !ReferenceEquals(first, second) && !ReferenceEquals(principal.Identity, ((IPrincipal)second).Identity)
                && !ReferenceEquals(first, marker);
            Console.WriteLine("default-principal-" + route + "-" + nonnull + ":" + ok);
        }
    }
    private static AppDomain Domain(PrincipalPolicy policy)
    {
        var domain = (AppDomain)RuntimeHelpers.GetUninitializedObject(typeof(AppDomain));
        domain.SetPrincipalPolicy(policy);
        return domain;
    }
    private static void PolicyChecks()
    {
        AppDomain none = Domain(PrincipalPolicy.NoPrincipal);
        Console.WriteLine("default-principal-policy-none:" + (GetPrincipal(none) == null));
        AppDomain windows = Domain(PrincipalPolicy.WindowsPrincipal);
        bool refused = false;
        try { GetPrincipal(windows); }
        catch (PlatformNotSupportedException) { refused = true; }
        Console.WriteLine("default-principal-policy-windows:" + refused);
        AppDomain unauthenticated = Domain(PrincipalPolicy.UnauthenticatedPrincipal);
        IPrincipal first = GetPrincipal(unauthenticated), second = GetPrincipal(unauthenticated);
        Console.WriteLine("default-principal-policy-unauthenticated:" + (first.Identity.Name == "" && first.IsInRole("") && !first.IsInRole("admin") && !ReferenceEquals(first, second)));
        var supplied = new UserPrincipal();
        none.SetThreadPrincipal(supplied);
        none.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
        IPrincipal selected = GetPrincipal(none);
        Console.WriteLine("default-principal-policy-explicit:" + (ReferenceEquals(selected, supplied) && selected.Identity.Name == "explicit-user" && selected.IsInRole("explicit-role") && !selected.IsInRole("admin")));
        bool twice = false;
        try { none.SetThreadPrincipal(new UserPrincipal()); }
        catch (SystemException error) { twice = error.GetType() == typeof(SystemException); }
        Console.WriteLine("default-principal-policy-second-set:" + (twice && ReferenceEquals(GetPrincipal(none), supplied)));
    }
    internal static void RunArtifactChecks(string[] args)
    {
        Culture();
        Cases("direct", Direct); Cases("calli", Pointer); Cases("delegate", DelegateRoute); Cases("reflection", Reflect);
        for (int mode = 0; mode < 2; mode++)
        {
            Trace = 0;
            bool ok = false;
            try { ok = Produced(mode == 1) is IPrincipal && mode == 0; }
            catch (FormatException error) { ok = mode == 1 && error.Message == "marker producer"; }
            Console.WriteLine("default-principal-producer-" + mode + ":" + (ok && Trace == 1));
        }
    }
    internal static void RunReflectionChecks(string[] args) { Culture(); Cases("reflection", Reflect); }
    internal static void RunPolicyChecks(string[] args) { Culture(); PolicyChecks(); }
}
