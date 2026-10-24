using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

try
{
    Version runtime = new Version(10, 0, 13);
    Version icu = OperatingSystem.IsWindows() ? new Version(72, 1, 0, 4)
        : OperatingSystem.IsMacOS() ? new Version(78, 1, 0, 0)
        : OperatingSystem.IsLinux() ? new Version(74, 2, 0, 0)
        : throw new PlatformNotSupportedException("No culture reference for this host.");
    Console.WriteLine($"Host: {RuntimeInformation.OSDescription}; {RuntimeInformation.ProcessArchitecture}");
    Require("CLR", Environment.Version, runtime);

    // This host-only probe reads the pinned CoreLib's selected globalization
    // backend; checking an installed ICU library would not identify CLR's oracle.
    Assembly corelib = typeof(CultureInfo).Assembly;
    Type mode = corelib.GetType("System.Globalization.GlobalizationMode", throwOnError: true)!;
    const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    foreach (string name in OperatingSystem.IsWindows()
        ? new[] { "Invariant", "UseNls" } : new[] { "Invariant" })
    {
        PropertyInfo property = mode.GetProperty(name, flags)
            ?? throw new MissingMemberException(mode.FullName, name);
        if ((bool)property.GetValue(null)!)
            throw new InvalidOperationException($"Culture reference requires ICU: {name} is enabled.");
    }
    _ = CultureInfo.GetCultureInfo("en-US").CompareInfo.Version;
    Type interop = corelib.GetType("Interop+Globalization", throwOnError: true)!;
    MethodInfo method = interop.GetMethod("GetICUVersion", flags, Type.EmptyTypes)
        ?? throw new MissingMethodException(interop.FullName, "GetICUVersion");
    int packed = (int)method.Invoke(null, null)!;
    Version actual = new Version((packed >> 24) & 255, (packed >> 16) & 255,
        (packed >> 8) & 255, packed & 255);
    Require("ICU", actual, icu);
    Console.WriteLine("OK: culture reference versions");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"FAIL: culture reference: {error.Message}");
    return 1;
}

static void Require(string subject, Version actual, Version expected)
{
    Console.WriteLine($"{subject}: {actual}; expected: {expected}");
    if (actual != expected)
        throw new InvalidOperationException($"{subject} reference changed: expected {expected}, got {actual}.");
}
