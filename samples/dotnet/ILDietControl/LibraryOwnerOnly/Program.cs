using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using ILDietControlLib;

namespace ILDietLibraryOwnerOnly;

internal sealed class AppOwner : AppFacingBase { }

internal sealed class AppMetadataOwner : MetadataBase { }

// Only Assembly.CreateInstance over a constant name constructs it.
internal sealed class AssemblyNamedPlugin
{
    private readonly string _label;

    public AssemblyNamedPlugin() => _label = "assembly-named-application";

    public override string ToString() => _label;
}

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== library owners ==");
        // A lookup on a derived type finds these members on its user-library base type.
        object app = new AppOwner();
        Console.WriteLine(typeof(AppOwner).GetMethod("BaseDescribe")!.Invoke(app, null));
        Console.WriteLine(typeof(AppOwner).GetProperty("BaseLabel")!.GetValue(app));
        object library = new ReflectedOwnerDerived();
        Console.WriteLine(typeof(ReflectedOwnerDerived).GetMethod("InheritedDescribe")!.Invoke(library, null));
        Console.WriteLine("library base members end");
        // No IL token names these user-library types; a constant type-name string does.
        Type named = Type.GetType("ILDietControlLib.NamedOnlyTarget, ILDietControlLib")!;
        Console.WriteLine(named.GetMethod("Run")!.Invoke(null, null));
        Type box = Type.GetType("ILDietControlLib.NamedOnlyBox`1, ILDietControlLib")!.MakeGenericType(typeof(int));
        Console.WriteLine(box.GetMethod("Describe")!.Invoke(null, null));
        Console.WriteLine("constant type names end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_METADATA_OWNERS") == "1") return;
        MetadataOnly();
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_ASSEMBLY_CONSTRUCTION") == "1") return;
        AssemblyConstruction();
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_LISTED_OWNERS") == "1") return;
        ListedOwners();
    }

    // An assembly reached through a type argument lists every type it declares.
    private static void ListedOwners()
    {
        Console.WriteLine("== listed library owners ==");
        Console.WriteLine("listed types=" + ListedTypes<ListedAssemblyMarker>());
        Console.WriteLine("listed owners end");
    }

    // Only the exact instantiation names the assembly the listing reads.
    private static string ListedTypes<T>() where T : struct
    {
        var names = new List<string>();
        foreach (Type type in typeof(T).Assembly.GetTypes())
            if (type.IsGenericTypeDefinition && type.Name.StartsWith("AssemblyListed", StringComparison.Ordinal))
                names.Add(type.Name);
        names.Sort(StringComparer.Ordinal);
        return string.Join(",", names);
    }

    // The program's only construction of a type chosen at run time: an assembly creates
    // each instance from a constant name no IL token spells. The library call passes
    // the binding flags .NET defaults to.
    private static void AssemblyConstruction()
    {
        Console.WriteLine("== assembly-named construction ==");
        Console.WriteLine("application=" + typeof(Program).Assembly.CreateInstance("ILDietLibraryOwnerOnly.AssemblyNamedPlugin"));
        Console.WriteLine("library=" + typeof(AppFacingBase).Assembly.CreateInstance("ILDietControlLib.AssemblyNamedTarget",
            false, BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance, null, null,
            CultureInfo.InvariantCulture, null));
        Console.WriteLine("assembly-named construction end");
    }

    // No site in this body runs, binds or constructs a member: the lookups and listings
    // observe only the identity and member rows of types a constant string or a base
    // chain reaches.
    private static void MetadataOnly()
    {
        Console.WriteLine("== metadata-only library owners ==");
        Type? identity = Type.GetType("ILDietControlLib.MetadataIdentityOnly, ILDietControlLib");
        Console.WriteLine("identity=" + (identity is null ? "null" : identity.FullName));
        Type named = Type.GetType("ILDietControlLib.MetadataNamedTarget, ILDietControlLib")!;
        Console.WriteLine("named-method=" + (named.GetMethod("Probe") != null));
        Console.WriteLine("named-property=" + (named.GetProperty("Label") != null));
        Type listed = Type.GetType("ILDietControlLib.MetadataListedTarget, ILDietControlLib")!;
        var methods = listed.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        var names = new string[methods.Length];
        for (int i = 0; i < methods.Length; i++) names[i] = methods[i].Name;
        Array.Sort(names, StringComparer.Ordinal);
        Console.WriteLine("listed=" + string.Join(",", names));
        Console.WriteLine("base-method=" + (typeof(AppMetadataOwner).GetMethod("BaseOnly") != null));
        Console.WriteLine("library-base-method="
            + (typeof(MetadataLibraryDerived).GetMethod("LibraryBaseOnly") != null));
        Console.WriteLine("metadata-only owners end");
    }
}
