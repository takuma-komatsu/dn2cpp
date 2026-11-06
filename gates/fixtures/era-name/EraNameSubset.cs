using System;
using System.Globalization;
using System.Reflection;

namespace EraNameSubset;

internal static class Program
{
    private delegate string Query(DateTimeFormatInfo info, int era);
    private const string Prefix = "era-name-";
    private const string Header = "== Invariant era names ==";
    private static string prefix = Prefix;
    private static int checks;
    private static int trace;

    private static string Direct(DateTimeFormatInfo info, int era) => info.GetEraName(era);
    private static string Pointer(DateTimeFormatInfo info, int era) => info.GetEraName(era);
    private static Func<int, string> Address(DateTimeFormatInfo info) => info.GetEraName;
    private static string DelegateRoute(DateTimeFormatInfo info, int era) => Address(info)(era);
    private static MethodInfo Metadata() => typeof(DateTimeFormatInfo).GetMethod(nameof(DateTimeFormatInfo.GetEraName))!;
    private static string Reflect(DateTimeFormatInfo info, int era) => (string)Metadata().Invoke(info, new object[] { era })!;
    private static DateTimeFormatInfo Receiver() { trace = trace * 10 + 1; return null!; }
    private static int Argument() { trace = trace * 10 + 2; return 2; }
    private static int ThrowArgument() { trace = trace * 10 + 2; throw new FormatException("era producer"); }

    private static void Check(string label, bool success)
    {
        Console.WriteLine(prefix + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
        checks++;
    }

    private static void PinCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    private static void Validate(string route, Query query, bool reflection, bool abbreviated = false)
    {
        var invariant = DateTimeFormatInfo.InvariantInfo;
        string expected = abbreviated ? "AD" : "A.D.";
        for (int value = 0; value <= 1; value++)
        {
            Check(route + "-" + value, query(invariant, value) == expected);
            Check(route + "-identity-" + value, ReferenceEquals(query(invariant, value), query(invariant, value)));
        }
        foreach (int value in new[] { -1, 2, int.MinValue, int.MaxValue })
        {
            Exception observed = null!;
            try { query(invariant, value); }
            catch (Exception error) { observed = error; }
            if (reflection)
            {
                Check(route + "-wrapped-" + value, observed is TargetInvocationException);
                observed = ((TargetInvocationException)observed).InnerException!;
            }
            if (observed is not ArgumentOutOfRangeException range)
                throw new InvalidOperationException(route + " wrong range category");
            string message = "Era value was not valid. (Parameter 'era')"
                + Environment.NewLine + "Actual value was " + value.ToString(CultureInfo.InvariantCulture) + ".";
            Check(route + "-range-" + value, range.ParamName == "era"
                && range.ActualValue is not null && range.ActualValue.GetType() == typeof(int)
                && (int)range.ActualValue == value && range.Message == message);
        }
    }

    private static void ValidateNull(Query query, string label)
    {
        foreach (int era in new[] { 0, 1, 2, int.MinValue })
        {
            bool observed = false;
            try { query(null!, era); }
            catch (NullReferenceException) { observed = true; }
            Check(label + "-" + era, observed);
        }
    }

    public static void RunReflectionChecks()
    {
        PinCulture();
        Validate("reflect", Reflect, true);
        Exception nullTarget = null!;
        try { Reflect(null!, 2); }
        catch (Exception error) { nullTarget = error; }
        Check("reflection-null-target", nullTarget is TargetException);
        Console.WriteLine(prefix + "reflection-end");
    }

    public static void RunDirectChecks()
    {
        PinCulture();
        Validate("direct", Direct, false);
        ValidateNull(Direct, "null-first");
        trace = 0;
        bool nullObserved = false;
        try { Direct(Receiver(), Argument()); }
        catch (NullReferenceException) { nullObserved = true; }
        Check("producer-order", nullObserved && trace == 12);
        trace = 0;
        try { Direct(Receiver(), ThrowArgument()); }
        catch (FormatException error) { Check("producer-preempts-null", trace == 12 && error.Message == "era producer"); return; }
        throw new InvalidOperationException("throwing argument did not preempt receiver check");
    }

    public static void RunMinimal()
    {
        PinCulture();
        string name = Direct(DateTimeFormatInfo.InvariantInfo, 0);
        Console.WriteLine("era-name-minimal:" + (name == "A.D."));
        if (name != "A.D.") throw new InvalidOperationException("current era");
    }

    public static void Run()
    {
        PinCulture();
        prefix = Prefix;
        checks = 0;
        Console.WriteLine(Header);
        RunDirectChecks();
        Validate("pointer", Pointer, false);
        ValidateNull(Pointer, "pointer-null-first");
        Validate("delegate", DelegateRoute, false);
        RunReflectionChecks();
        var clone = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        clone.DateSeparator = "~";
        Check("separator-clone-neighbor", clone.DateSeparator == "~" && DateTimeFormatInfo.InvariantInfo.DateSeparator == "/");
        Console.WriteLine(prefix + "end:" + checks);
        RunAbbreviated();
    }

    private static string DirectAbbreviated(DateTimeFormatInfo info, int era) => info.GetAbbreviatedEraName(era);
    private static string VirtualAbbreviated(DateTimeFormatInfo info, int era) => info.GetAbbreviatedEraName(era);
    private static string PointerAbbreviated(DateTimeFormatInfo info, int era) => info.GetAbbreviatedEraName(era);
    private static Func<int, string> AddressAbbreviated(DateTimeFormatInfo info) => info.GetAbbreviatedEraName;
    private static string DelegateAbbreviated(DateTimeFormatInfo info, int era) => AddressAbbreviated(info)(era);
    private static MethodInfo MetadataAbbreviated() => typeof(DateTimeFormatInfo).GetMethod(nameof(DateTimeFormatInfo.GetAbbreviatedEraName))!;
    private static string ReflectAbbreviated(DateTimeFormatInfo info, int era) => (string)MetadataAbbreviated().Invoke(info, new object[] { era })!;

    private static void AbbreviatedMode()
    {
        PinCulture();
        prefix = "abbreviated-era-name-";
    }
    public static void RunAbbreviatedDirectChecks()
    {
        AbbreviatedMode();
        Validate("direct", DirectAbbreviated, false, true);
        ValidateNull(DirectAbbreviated, "null-first");
        trace = 0;
        bool observed = false;
        try { DirectAbbreviated(Receiver(), Argument()); }
        catch (NullReferenceException) { observed = true; }
        Check("producer-order", observed && trace == 12);
        trace = 0;
        try { DirectAbbreviated(Receiver(), ThrowArgument()); }
        catch (FormatException error)
        {
            Check("producer-preempts-null", trace == 12 && error.Message == "era producer");
            return;
        }
        throw new InvalidOperationException("throwing argument did not preempt receiver check");
    }
    public static void RunAbbreviatedReflectionChecks()
    {
        AbbreviatedMode();
        Validate("reflect", ReflectAbbreviated, true, true);
        bool observed = false;
        try { ReflectAbbreviated(null!, 2); }
        catch (TargetException) { observed = true; }
        Check("reflection-null-target", observed);
        Console.WriteLine(prefix + "reflection-end");
    }
    public static void RunAbbreviatedMinimal()
    {
        AbbreviatedMode();
        string name = DirectAbbreviated(DateTimeFormatInfo.InvariantInfo, 0);
        bool valid = name.Length == 2 && name[0] == (char)65 && name[1] == (char)68;
        Console.WriteLine("abbreviated-era-name-minimal:" + valid);
        if (!valid) throw new InvalidOperationException("abbreviated current era");
    }
    public static void RunAbbreviated()
    {
        AbbreviatedMode();
        checks = 0;
        Console.WriteLine("== Invariant abbreviated era names ==");
        RunAbbreviatedDirectChecks();
        Validate("callvirt", VirtualAbbreviated, false, true);
        ValidateNull(VirtualAbbreviated, "callvirt-null-first");
        Validate("pointer", PointerAbbreviated, false, true);
        ValidateNull(PointerAbbreviated, "pointer-null-first");
        Validate("delegate", DelegateAbbreviated, false, true);
        RunAbbreviatedReflectionChecks();
        var info = DateTimeFormatInfo.InvariantInfo;
        Check("literal-identity", ReferenceEquals(DirectAbbreviated(info, 0), "AD") && ReferenceEquals(DirectAbbreviated(info, 0), DirectAbbreviated(info, 1)));
        Check("return-abi", MetadataAbbreviated().ReturnType == typeof(string) && AddressAbbreviated(info).Method.ReturnType == typeof(string));
        Console.WriteLine(prefix + "end:" + checks);
    }

}
