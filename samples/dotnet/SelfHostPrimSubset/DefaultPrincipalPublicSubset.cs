using System;
using System.Globalization;
using System.Security.Principal;

namespace DefaultPrincipalPublicSubset;

internal static class Program
{
    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Principal public neighbor ==");
        var principal = new GenericPrincipal(new GenericIdentity("explicit-user", "fixture"), new[] { "fixture-role" });
        Console.WriteLine("principal-public-explicit:" + (principal.Identity.Name == "explicit-user"
            && principal.Identity.IsAuthenticated && principal.IsInRole("fixture-role") && !principal.IsInRole("admin")));
        Console.WriteLine("Principal public neighbor end");
    }
}
