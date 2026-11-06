using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KnownConstantSubset;

internal static class Program
{
    private enum Choice { First, Last = 17 }
    private struct Plain { public long Value; }
    private struct WithReference { public string Value; }
    private struct Box<T> { public T Value; public string Reference; }
    private static int effects;
    private static string order;
    private static void Check(string label, bool value)
    {
        Console.WriteLine("known-constant-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }
    // The fixture installs inspected token-free IL here, never a private CLR call.
    private static bool HintType(Type value) => throw new InvalidOperationException("fixture missing");
    private static bool HintString(string value) => throw new InvalidOperationException("fixture missing");
    private static bool HintChar(char value) => throw new InvalidOperationException("fixture missing");
    private static bool HintGeneric<T>(T value) where T : struct => throw new InvalidOperationException("fixture missing");
    private static T Produce<T>(T value, string tag, bool fail = false)
    {
        effects++; order += tag;
        if (fail) throw new ArgumentException("producer");
        return value;
    }
    private static unsafe bool PointerType(Type value) { delegate* managed<Type, bool> p = &HintType; return p(value); }
    private static unsafe bool PointerString(string value) { delegate* managed<string, bool> p = &HintString; return p(value); }
    private static unsafe bool PointerChar(char value) { delegate* managed<char, bool> p = &HintChar; return p(value); }
    private static unsafe bool PointerGeneric(int value) { delegate* managed<int, bool> p = &HintGeneric<int>; return p(value); }
    private static bool ReflectionType(Type value) => (bool)typeof(RuntimeHelpers).GetMethod("IsKnownConstant", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Type) }, null).Invoke(null, new object[] { value });
    private static bool ReflectionString(string value) => (bool)typeof(RuntimeHelpers).GetMethod("IsKnownConstant", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(string) }, null).Invoke(null, new object[] { value });
    private static bool ReflectionChar(char value) => (bool)typeof(RuntimeHelpers).GetMethod("IsKnownConstant", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(char) }, null).Invoke(null, new object[] { value });
    private static MethodInfo ReflectionGenericMethod()
    {
        foreach (MemberInfo member in typeof(RuntimeHelpers).GetMember("IsKnownConstant", MemberTypes.Method, BindingFlags.NonPublic | BindingFlags.Static))
        {
            MethodInfo method = (MethodInfo)member;
            if (method.IsGenericMethodDefinition) return method;
        }
        throw new InvalidOperationException("generic hint metadata missing");
    }
    private static bool ReflectionGeneric(int value) => (bool)ReflectionGenericMethod().MakeGenericMethod(typeof(int)).Invoke(null, new object[] { value });
    internal static void RunGenericReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        // Only this closed instantiation is statically rooted; reflection must not invent closures.
        Func<int, bool> root = HintGeneric<int>;
        Check("generic-reflection-root", !root(123));
        Check("generic-reflection", !ReflectionGeneric(0) && !ReflectionGeneric(int.MaxValue));
        Console.WriteLine("Known constant generic reflection end");
    }
    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Check("reflection-type", !ReflectionType(null) && !ReflectionType(typeof(int)));
        Check("reflection-string", !ReflectionString(null) && !ReflectionString("") && !ReflectionString("value"));
        Check("reflection-char", !ReflectionChar('\0') && !ReflectionChar('\uffff'));
        Console.WriteLine("Known constant reflection end");
    }
    internal static void RunArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Known constant conservative fallback ==");
        Check("type", !HintType(null) && !HintType(typeof(int)));
        Check("string", !HintString(null) && !HintString("") && !HintString("value"));
        Check("char", !HintChar('\0') && !HintChar('A') && !HintChar('\ud800') && !HintChar('\uffff'));
        Check("generic", !HintGeneric(1) && !HintGeneric(Choice.Last) && !HintGeneric(new Plain { Value = 19 })
            && !HintGeneric(new WithReference { Value = "reference" }));
        effects = 0; order = "";
        bool a = HintType(Produce(typeof(int), "T"));
        bool b = HintString(Produce("text", "S"));
        bool c = HintChar(Produce('a', "C"));
        bool d = HintGeneric(Produce(new WithReference { Value = "ref" }, "G"));
        Check("effects-consumed", !a && !b && !c && !d && effects == 4 && order == "TSCG");
        effects = 0; order = "";
        HintType(Produce<Type>(null, "T")); HintString(Produce<string>(null, "S"));
        HintChar(Produce('a', "C")); HintGeneric(Produce(17, "G"));
        Check("effects-unused", effects == 4 && order == "TSCG");
        effects = 0; order = ""; int caught = 0;
        try { HintType(Produce<Type>(null, "T", true)); } catch (ArgumentException) { caught++; }
        try { HintString(Produce<string>(null, "S", true)); } catch (ArgumentException) { caught++; }
        try { HintChar(Produce('a', "C", true)); } catch (ArgumentException) { caught++; }
        try { HintGeneric(Produce(1, "G", true)); } catch (ArgumentException) { caught++; }
        Check("effects-throw", caught == 4 && effects == 4 && order == "TSCG");
        Check("calli", !PointerType(null) && !PointerString(null) && !PointerChar('a') && !PointerGeneric(7));
        Func<Type, bool> ft = HintType; Func<string, bool> fs = HintString;
        Func<char, bool> fc = HintChar; Func<int, bool> fg = HintGeneric<int>;
        Check("delegate", !ft(typeof(int)) && !fs("text") && !fc('\uffff') && !fg(7));
        RunReflectionArtifact(args);
        RunGenericReflectionArtifact(args);
        RunCallerArtifact(args);
        Run();
        Console.WriteLine("Known constant routes end");
    }
    // Patched only for native: the real two-candidate SpanHelpers caller, not MemoryExtensions.
    private static int Caller(ref short start, short a, short b, int length) => MemoryMarshal.CreateReadOnlySpan(ref start, length).IndexOfAny(a, b);
    private static int CallerExcept(ref short start, short a, short b, int length) => MemoryMarshal.CreateReadOnlySpan(ref start, length).IndexOfAnyExcept(a, b);
    internal static void RunCallerArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        bool valid = true;
        foreach (short a in new short[] { 65, 7, -1, 2000 })
        {
            short b = a == 65 ? (short)97 : (short)(a + 1);
            short other = 1234;
            foreach (int length in new[] { 0, 1, 2, 7, 15, 16, 17, 31, 32, 33, 65, 129 })
            foreach (int position in new[] { -1, 0, length / 2, length - 1 })
            {
                short[] data = new short[length + 2]; Array.Fill(data, other);
                data[0] = a; data[length + 1] = b;
                int expected = position >= 0 && position < length ? position : -1;
                if (expected >= 0) data[position + 1] = a;
                valid &= Caller(ref data[1], a, b, length) == expected;
                valid &= Caller(ref data[1], a, a, length) == expected;
                Array.Fill(data, a); data[0] = other; data[length + 1] = other;
                if (expected >= 0) data[position + 1] = other;
                valid &= CallerExcept(ref data[1], a, b, length) == expected;
                valid &= CallerExcept(ref data[1], a, a, length) == expected;
                // Each candidate must independently affect the real two-value caller.
                Array.Fill(data, other); data[0] = a; data[length + 1] = b;
                if (expected >= 0) data[position + 1] = b;
                valid &= Caller(ref data[1], a, b, length) == expected;
                for (int i = 1; i <= length; i++) data[i] = (i & 1) == 0 ? a : b;
                data[0] = other; data[length + 1] = other;
                if (expected >= 0) data[position + 1] = other;
                valid &= CallerExcept(ref data[1], a, b, length) == expected;
            }
        }
        Check("actual-caller", valid);
        Console.WriteLine("Known constant actual caller end");
    }
    private static void Searches<T>(T a, T b, T c, T other) where T : IEquatable<T>
    {
        foreach (int length in new[] { 0, 1, 2, 3, 7, 15, 16, 17, 31, 32, 33, 65, 129 })
        foreach (int position in new[] { -1, 0, length / 2, length - 1 })
        {
            T[] data = new T[length + 2]; Array.Fill(data, other); data[0] = a; data[length + 1] = b;
            int expected = position >= 0 && position < length ? position : -1;
            if (expected >= 0) data[expected + 1] = a;
            ReadOnlySpan<T> span = new ReadOnlySpan<T>(data, 1, length);
            if (span.IndexOfAny(a, b) != expected || span.IndexOfAny(a, b, c) != expected || span.IndexOfAny(a, a) != expected)
                throw new InvalidOperationException("IndexOfAny slice boundary");
            Array.Fill(data, a); data[0] = other; data[length + 1] = other;
            if (expected >= 0) data[expected + 1] = other;
            if (span.IndexOfAnyExcept(a, b) != expected || span.IndexOfAnyExcept(a, b, c) != expected || span.IndexOfAnyExcept(a, a) != expected)
                throw new InvalidOperationException("IndexOfAnyExcept slice boundary");
            T[] candidates = { a, b, c };
            for (int candidate = 0; candidate < candidates.Length; candidate++)
            {
                Array.Fill(data, other); data[0] = a; data[length + 1] = b;
                if (expected >= 0) data[expected + 1] = candidates[candidate];
                int expectedTwo = candidate < 2 ? expected : -1;
                if (span.IndexOfAny(a, b) != expectedTwo || span.IndexOfAny(a, b, c) != expected)
                    throw new InvalidOperationException("IndexOfAny independent candidate");
            }
            for (int count = 2; count <= 3; count++)
            {
                for (int i = 1; i <= length; i++) data[i] = candidates[(i - 1) % count];
                data[0] = other; data[length + 1] = other;
                if (expected >= 0) data[expected + 1] = other;
                int actual = count == 2 ? span.IndexOfAnyExcept(a, b) : span.IndexOfAnyExcept(a, b, c);
                if (actual != expected) throw new InvalidOperationException("IndexOfAnyExcept mixed candidates");
            }
        }
    }
    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Known constant public searches ==");
        Searches((byte)1, (byte)2, (byte)3, (byte)255);
        Searches('A', 'a', 'B', '\0'); Searches('\u1234', '\uffff', '\ud800', '\0');
        Searches((short)65, (short)97, (short)66, (short)-1); Searches(1, 2, 3, -1);
        Check("public-searches", true);
        Console.WriteLine("Known constant public searches end");
    }
    internal static void RunGenericStructArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        effects = 0; order = "";
        bool value = HintGeneric(Produce(new Box<string> { Value = "reference", Reference = "field" }, "B"));
        Check("generic-struct", !value && effects == 1 && order == "B");
        Console.WriteLine("Known constant generic struct end");
    }
}
