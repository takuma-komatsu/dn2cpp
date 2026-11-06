using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DtfITokenizeSubset;
internal static class Program
{
    private enum Token : int { None }
    private ref struct Cursor { public int Index; public char m_current; }
    private delegate bool Query(DateTimeFormatInfo receiver, Token mask, ref Token token, ref int value, ref Cursor parser);
    private delegate bool ClosedQuery(Token mask, ref Token token, ref int value, ref Cursor parser);
    private const string Refusal = "DateTimeFormatInfo token-table parsing is not supported";
    private static bool Direct(DateTimeFormatInfo receiver, Token mask, ref Token token, ref int value, ref Cursor parser) => throw new InvalidOperationException("native fixture required");
    private static bool Virtual(DateTimeFormatInfo receiver, Token mask, ref Token token, ref int value, ref Cursor parser) => throw new InvalidOperationException("native fixture required");
    private static bool Pointer(DateTimeFormatInfo receiver, Token mask, ref Token token, ref int value, ref Cursor parser) => throw new InvalidOperationException("native fixture required");
    private static ClosedQuery Address(DateTimeFormatInfo receiver) => throw new InvalidOperationException("native fixture required");
    private static bool DelegateRoute(DateTimeFormatInfo receiver, Token mask, ref Token token, ref int value, ref Cursor parser) => Address(receiver)(mask, ref token, ref value, ref parser);
    private static void Check(string label, bool value)
    {
        if (!value) throw new InvalidOperationException(label);
        Console.WriteLine("dtfi-tokenize-" + label + ":True");
    }
    private static void PinCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
    private static void Route(string label, Query query, bool virtualCall)
    {
        for (int scenario = 0; scenario < (label == "delegate" ? 6 : 7); scenario++)
        {
            Token token = (Token)int.MinValue;
            int value = int.MaxValue;
            Cursor parser = default;
            parser.Index = -731;
            parser.m_current = '\ud800';
            ref Token tokenArg = ref token;
            ref int valueArg = ref value;
            ref Cursor parserArg = ref parser;
            if (scenario == 1) valueArg = ref Unsafe.As<Token, int>(ref token);
            if (scenario == 2 || scenario == 3) tokenArg = ref Unsafe.NullRef<Token>();
            if (scenario == 2 || scenario == 4) valueArg = ref Unsafe.NullRef<int>();
            if (scenario == 2 || scenario == 5) parserArg = ref Unsafe.NullRef<Cursor>();
            bool caught = false;
            try { _ = query(scenario == 6 ? null! : DateTimeFormatInfo.InvariantInfo, (Token)(-1), ref tokenArg, ref valueArg, ref parserArg); }
            catch (PlatformNotSupportedException error) { caught = !(virtualCall && scenario == 6) && error.Message == Refusal; }
            catch (NullReferenceException) { caught = virtualCall && scenario == 6; }
            Check(label + "-" + scenario, caught && token == (Token)int.MinValue && value == int.MaxValue
                && parser.Index == -731 && parser.m_current == '\ud800');
        }
    }
    private static int trace, throwAt;
    private static void Observe(int position)
    {
        trace = trace * 10 + position;
        if (throwAt == position) throw new FormatException("producer");
    }
    private static DateTimeFormatInfo Receiver() { Observe(1); return null!; }
    private static Token Mask() { Observe(2); return (Token)int.MaxValue; }
    private static ref Token TokenArgument(ref Token token) { Observe(3); return ref token; }
    private static ref int ValueArgument(ref int value) { Observe(4); return ref value; }
    private static ref Cursor ParserArgument(ref Cursor parser) { Observe(5); return ref parser; }
    private static bool Producer(bool virtualCall, ref Token token, ref int value, ref Cursor parser) =>
        Direct(Receiver(), Mask(), ref TokenArgument(ref token), ref ValueArgument(ref value), ref ParserArgument(ref parser));
    private static bool VirtualProducer(bool virtualCall, ref Token token, ref int value, ref Cursor parser) =>
        Direct(Receiver(), Mask(), ref TokenArgument(ref token), ref ValueArgument(ref value), ref ParserArgument(ref parser));
    private static void Producers(bool virtualCall)
    {
        int[] expected = { 12345, 1, 12, 123, 1234, 12345 };
        for (int position = 0; position <= 5; position++)
        {
            trace = 0; throwAt = position;
            Token token = (Token)777; int value = -991; Cursor parser = default;
            parser.Index = 42; parser.m_current = 'Z';
            bool caught = false;
            try
            {
                _ = virtualCall ? VirtualProducer(true, ref token, ref value, ref parser)
                    : Producer(false, ref token, ref value, ref parser);
            }
            catch (FormatException error) { caught = position != 0 && error.Message == "producer"; }
            catch (NullReferenceException) { caught = position == 0 && virtualCall; }
            catch (PlatformNotSupportedException error) { caught = position == 0 && !virtualCall && error.Message == Refusal; }
            Check((virtualCall ? "virtual" : "raw") + "-producer-" + position, caught && trace == expected[position]
                && token == (Token)777 && value == -991 && parser.Index == 42 && parser.m_current == 'Z');
        }
    }
    private static MethodInfo Metadata() => typeof(DateTimeFormatInfo).GetMethod("Tokenize", BindingFlags.NonPublic | BindingFlags.Instance)!;
    public static void RunReflectionChecks()
    {
        PinCulture();
        MethodInfo method = Metadata();
        Check("reflection-shape", method is not null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 4);
        bool denied = false;
        try { _ = method.Invoke(DateTimeFormatInfo.InvariantInfo, new object?[] { (Token)0, null, null, null }); }
        catch (NotSupportedException error) { denied = error.Message == "Cannot create boxed ByRef-like values."; }
        Check("reflection-byreflike", denied);
        bool nullTarget = false;
        try { _ = method.Invoke(null, new object?[] { (Token)0, null, null, null }); }
        catch (TargetException) { nullTarget = true; }
        Check("reflection-null-target", nullTarget);
    }
    public static void RunMinimal()
    {
        PinCulture();
        bool caught = false;
        try { _ = Direct(null!, (Token)0, ref Unsafe.NullRef<Token>(), ref Unsafe.NullRef<int>(), ref Unsafe.NullRef<Cursor>()); }
        catch (PlatformNotSupportedException error) { caught = error.Message == Refusal; }
        Check("minimal-no-access", caught);
    }
    public static void RunPublicOracle()
    {
        PinCulture();
        Check("public-parse", DateTime.Parse("2024-02-29", CultureInfo.InvariantCulture).Day == 29);
        Check("public-try-false", !DateTime.TryParse("not-a-date", CultureInfo.InvariantCulture, DateTimeStyles.None, out _));
        Check("public-exact", DateTime.ParseExact("20240229", "yyyyMMdd", CultureInfo.InvariantCulture).Month == 2);
        Check("public-exact-false", !DateTime.TryParseExact("20240230", "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _));
    }
    public static void Run()
    {
        PinCulture();
        Route("direct", Direct, false); Route("virtual", Virtual, true);
        Route("calli", Pointer, false); Route("delegate", DelegateRoute, false);
        Producers(false); Producers(true);
        RunReflectionChecks(); RunPublicOracle();
        Console.WriteLine("DTFI Tokenize boundary end");
    }
    public static void Main() => Run();
}
