using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ReflectNameBindOnly;

internal sealed class Subject
{
    private int Secret(int value) => value + 1;
    private static int Twice(int value) => value * 2;
}

internal unsafe delegate int FunctionProbe(delegate*<int, int> value);
internal unsafe delegate int TokenProbe(delegate*<CancellationToken, int> value);
internal unsafe class FunctionOverload<T>
{
    public static int Ref(delegate*<int, int> value) => 181;
    public static int Ref(delegate*<T, int> value) => 182;
}
internal unsafe class TokenOverload<T>
{
    public static int Ref(delegate*<CancellationToken, int> value) => 191;
    public static int Ref(delegate*<T, int> value) => 192;
}

internal unsafe delegate ValueTask<int>* TaskPointer(ValueTask<int>* value);
internal static class ArraySignals
{
    internal static int Fixed;
    internal static int Dependent;
    internal static nint Seen;
}
internal unsafe class ArrayElementOwner<T>
{
    public static ValueTask<int>* Pointer(ValueTask<int>* value)
    {
        ArraySignals.Fixed += 7;
        ArraySignals.Seen = (nint)value;
        return value;
    }

    public static T* Pointer(T* value)
    {
        ArraySignals.Dependent += 8;
        return value;
    }
}

internal unsafe delegate ValueTask<int[]>* ArrayLeafPointer(ValueTask<int[]>* value);
internal unsafe delegate ValueTask<Tuple<Guid, long>>* ConstantLeafPointer(ValueTask<Tuple<Guid, long>>* value);
internal static class LeafSignals
{
    internal static int Fixed;
    internal static int Dependent;
    internal static nint Seen;
}
internal unsafe class ArrayLeafOwner<T>
{
    public static ValueTask<int[]>* Pointer(ValueTask<int[]>* value)
    {
        LeafSignals.Fixed += 7;
        LeafSignals.Seen = (nint)value;
        return value;
    }

    public static ValueTask<T>* Pointer(ValueTask<T>* value)
    {
        LeafSignals.Dependent += 8;
        return value;
    }
}
internal unsafe class ConstantLeafOwner<T>
{
    public static ValueTask<Tuple<Guid, long>>* Pointer(ValueTask<Tuple<Guid, long>>* value)
    {
        LeafSignals.Fixed += 7;
        LeafSignals.Seen = (nint)value;
        return value;
    }

    public static ValueTask<Tuple<T, CancellationToken>>* Pointer(ValueTask<Tuple<T, CancellationToken>>* value)
    {
        LeafSignals.Dependent += 8;
        return value;
    }
}

internal unsafe class ClosedGenericOwner<T>
{
    public static ValueTask<int>* Pointer(ValueTask<int>* value)
    {
        LeafSignals.Fixed += 7;
        LeafSignals.Seen = (nint)value;
        return value;
    }

    public static T* Pointer(T* value)
    {
        LeafSignals.Dependent += 8;
        return value;
    }
}

internal static class Program
{
    private static unsafe int FunctionCall(Type owner, bool hard)
    {
        var target = (FunctionProbe)Delegate.CreateDelegate(typeof(FunctionProbe), owner, "Ref", false, hard)!;
        return target((delegate*<int, int>)(nint)256);
    }

    private static unsafe int TokenCall(Type owner, bool hard)
    {
        var target = (TokenProbe)Delegate.CreateDelegate(typeof(TokenProbe), owner, "Ref", false, hard)!;
        return target((delegate*<CancellationToken, int>)(nint)512);
    }

    private static unsafe void RunArrayElements()
    {
        Console.WriteLine("== intrinsic pointer array element identity ==");
        Type array = Array.CreateInstance(typeof(CancellationToken), 0).GetType();
        Console.WriteLine($"pointer Token array => {array.GetArrayRank()}/{array.GetElementType() == typeof(CancellationToken)}");
        Type owner = typeof(ArrayElementOwner<>).MakeGenericType(array);
        int fixedToken = owner.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)[0].MetadataToken;
        foreach (bool throwing in new[] { false, true })
        {
            ArraySignals.Fixed = 0;
            ArraySignals.Dependent = 0;
            ArraySignals.Seen = 0;
            var matching = (TaskPointer)Delegate.CreateDelegate(
                typeof(TaskPointer), owner, "Pointer", false, throwing)!;
            if (matching.Method.MetadataToken != fixedToken)
                throw new InvalidOperationException("Wrong array element overload");
            ValueTask<int>* zero = matching(null);
            ValueTask<int>* returned = matching((ValueTask<int>*)0x1234);
            Console.WriteLine($"call pointer array element {(throwing ? "hard" : "soft")} => {zero == null}/{(nint)returned}/{ArraySignals.Seen}/{ArraySignals.Fixed}/{ArraySignals.Dependent}/{matching.Method.MetadataToken == fixedToken}");
        }
        Console.WriteLine("intrinsic pointer array element identity end");
    }

    private static unsafe void RunArrayLeaves()
    {
        Console.WriteLine("== intrinsic pointer array leaf identity ==");
        Console.WriteLine(typeof(ValueTask<>).Name);
        Type array = Array.CreateInstance(typeof(CancellationToken), 0).GetType();
        Type owner = typeof(ArrayLeafOwner<>).MakeGenericType(array);
        int fixedToken = owner.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)[0].MetadataToken;
        foreach (bool throwing in new[] { false, true })
        {
            LeafSignals.Fixed = 0;
            LeafSignals.Dependent = 0;
            LeafSignals.Seen = 0;
            var matching = (ArrayLeafPointer)Delegate.CreateDelegate(
                typeof(ArrayLeafPointer), owner, "Pointer", false, throwing)!;
            if (matching.Method.MetadataToken != fixedToken)
                throw new InvalidOperationException("Wrong array leaf overload");
            ValueTask<int[]>* zero = matching(null);
            ValueTask<int[]>* returned = matching((ValueTask<int[]>*)0x1234);
            Console.WriteLine($"call pointer array leaf {(throwing ? "hard" : "soft")} => {zero == null}/{(nint)returned}/{LeafSignals.Seen}/{LeafSignals.Fixed}/{LeafSignals.Dependent}/{matching.Method.MetadataToken == fixedToken}");
        }
        Console.WriteLine("intrinsic pointer array leaf identity end");
    }

    private static unsafe void RunConstantLeaves()
    {
        Console.WriteLine("== intrinsic pointer constant leaf identity ==");
        Console.WriteLine(typeof(ValueTask<>).Name);
        Console.WriteLine(typeof(Tuple<,>).Name);
        Console.WriteLine(typeof(CancellationToken).Name);
        Type owner = typeof(ConstantLeafOwner<>).MakeGenericType(typeof(Guid));
        int fixedToken = owner.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)[0].MetadataToken;
        foreach (bool throwing in new[] { false, true })
        {
            LeafSignals.Fixed = 0;
            LeafSignals.Dependent = 0;
            LeafSignals.Seen = 0;
            var matching = (ConstantLeafPointer)Delegate.CreateDelegate(
                typeof(ConstantLeafPointer), owner, "Pointer", false, throwing)!;
            if (matching.Method.MetadataToken != fixedToken)
                throw new InvalidOperationException("Wrong constant leaf overload");
            ValueTask<Tuple<Guid, long>>* zero = matching(null);
            ValueTask<Tuple<Guid, long>>* returned = matching((ValueTask<Tuple<Guid, long>>*)0x1234);
            Console.WriteLine($"call pointer constant leaf {(throwing ? "hard" : "soft")} => {zero == null}/{(nint)returned}/{LeafSignals.Seen}/{LeafSignals.Fixed}/{LeafSignals.Dependent}/{matching.Method.MetadataToken == fixedToken}");
        }
        Console.WriteLine("intrinsic pointer constant leaf identity end");
    }

    private static unsafe void RunClosedGenericArguments()
    {
        Console.WriteLine("== intrinsic closed generic pointer argument ==");
        Type owner = typeof(ClosedGenericOwner<>).MakeGenericType(typeof(ValueTask<long>));
        int fixedToken = owner.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)[0].MetadataToken;
        foreach (bool throwing in new[] { false, true })
        {
            LeafSignals.Fixed = 0;
            LeafSignals.Dependent = 0;
            LeafSignals.Seen = 0;
            var matching = (TaskPointer)Delegate.CreateDelegate(
                typeof(TaskPointer), owner, "Pointer", false, throwing)!;
            if (matching.Method.MetadataToken != fixedToken)
                throw new InvalidOperationException("Wrong closed generic overload");
            ValueTask<int>* zero = matching(null);
            ValueTask<int>* returned = matching((ValueTask<int>*)0x1234);
            Console.WriteLine($"call pointer closed generic {(throwing ? "hard" : "soft")} => {zero == null}/{(nint)returned}/{LeafSignals.Seen}/{LeafSignals.Fixed}/{LeafSignals.Dependent}/{matching.Method.MetadataToken == fixedToken}");
        }
        Console.WriteLine("intrinsic closed generic pointer argument end");
    }

    private static string InitializerMethodFault(Delegate value)
    {
        try { return value.Method.Name; }
        catch (InvalidCastException) { return "InvalidCastException"; }
    }

    private static void RunInitializerNames()
    {
        Console.WriteLine("== delegate cold initializer body names ==");
        Console.WriteLine($"cold initializer before => {ReflectReturnLib.NamedInitializerSignals.Cold}");
        var soft = (Action)Delegate.CreateDelegate(typeof(Action), typeof(ReflectReturnLib.NamedColdInitializer), ".cctor", false, false)!;
        var hard = (Action)Delegate.CreateDelegate(typeof(Action), typeof(ReflectReturnLib.NamedColdInitializer), ".CCTOR\0suffix", true, true)!;
        Console.WriteLine($"cold initializer bind => {ReflectReturnLib.NamedInitializerSignals.Cold}/{soft.Target is null}/{InitializerMethodFault(soft)}");
        soft();
        Console.WriteLine($"cold initializer soft call => {ReflectReturnLib.NamedInitializerSignals.Cold}");
        hard();
        Console.WriteLine($"cold initializer hard call => {ReflectReturnLib.NamedInitializerSignals.Cold}/{InitializerMethodFault(hard)}");
        var helper = (Action)Delegate.CreateDelegate(typeof(Action), typeof(ReflectReturnLib.NamedHelperInitializer), ".cctor", false, true)!;
        Console.WriteLine($"helper initializer bind => {ReflectReturnLib.NamedInitializerSignals.Helper}");
        helper();
        Console.WriteLine($"helper initializer first => {ReflectReturnLib.NamedInitializerSignals.Helper}");
        helper();
        Console.WriteLine($"helper initializer second => {ReflectReturnLib.NamedInitializerSignals.Helper}/{InitializerMethodFault(helper)}");
        var inherited = (Action)Delegate.CreateDelegate(typeof(Action), typeof(ReflectReturnLib.NamedInitializerDerived), ".cctor", false, true)!;
        Console.WriteLine($"inherited initializer bind => {ReflectReturnLib.NamedInitializerSignals.Inherited}");
        inherited();
        inherited();
        Console.WriteLine($"inherited initializer calls => {ReflectReturnLib.NamedInitializerSignals.Inherited}/{InitializerMethodFault(inherited)}");
        var generic = (Action)Delegate.CreateDelegate(typeof(Action), typeof(ReflectReturnLib.NamedGenericInitializer<string>), ".cctor", false, true)!;
        Console.WriteLine($"generic initializer bind => {ReflectReturnLib.NamedInitializerSignals.Generic}");
        generic();
        generic();
        Console.WriteLine($"generic initializer calls => {ReflectReturnLib.NamedInitializerSignals.Generic}/{ReflectReturnLib.NamedInitializerSignals.GenericName}/{InitializerMethodFault(generic)}");
        Console.WriteLine($"initializer mismatch => {Delegate.CreateDelegate(typeof(Action<int>), typeof(ReflectReturnLib.NamedColdInitializer), ".cctor", false, false) is null}/{Delegate.CreateDelegate(typeof(Func<int>), typeof(ReflectReturnLib.NamedColdInitializer), ".cctor", false, false) is null}");
        Console.WriteLine($"initializer absent => {Delegate.CreateDelegate(typeof(Action), typeof(Subject), ".cctor", false, false) is null}");
        try { Delegate.CreateDelegate(typeof(Action), typeof(Subject), ".cctor", false, true); }
        catch (ArgumentException) { Console.WriteLine("initializer absent hard => ArgumentException"); }
        Console.WriteLine("delegate cold initializer body names end");
    }

    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        if (args.Length > 0 && args[0] == "intrinsic-boundary")
        {
            Type owner = typeof(TokenOverload<>).MakeGenericType(typeof(CancellationToken));
            Console.WriteLine("== matching intrinsic overload boundary ==");
            foreach (bool hard in new bool[] { false, true })
            {
                string outcome;
                try
                {
                    outcome = Delegate.CreateDelegate(typeof(TokenProbe), owner, "Ref", false, hard) is null ? "null" : "bound";
                }
                catch (PlatformNotSupportedException)
                {
                    outcome = "unsupported";
                }
                Console.WriteLine("intrinsic coincident " + (hard ? "hard" : "soft") + " => " + outcome);
            }
            Console.WriteLine("matching intrinsic overload boundary end");
            return;
        }

        Console.WriteLine("== delegate names as the only reflection entry ==");
        Subject target = new Subject();
        Type delegateType = typeof(Func<int, int>);
        var instance = (Func<int, int>)Delegate.CreateDelegate(delegateType, target, "Secret");
        var instanceCase = (Func<int, int>)Delegate.CreateDelegate(delegateType, target, "secret", true);
        var instanceSoft = (Func<int, int>)Delegate.CreateDelegate(delegateType, target, "Secret", false, false)!;
        Console.WriteLine($"instance names: {instance(41)}/{instanceCase(41)}/{instanceSoft(41)}");
        var stat = (Func<int, int>)Delegate.CreateDelegate(delegateType, typeof(Subject), "Twice");
        var statCase = (Func<int, int>)Delegate.CreateDelegate(delegateType, typeof(Subject), "twice", true);
        var statSoft = (Func<int, int>)Delegate.CreateDelegate(delegateType, typeof(Subject), "Twice", false, false)!;
        Console.WriteLine($"static names: {stat(21)}/{statCase(21)}/{statSoft(21)}");
        Console.WriteLine("delegate names only end");
        if (args.Length > 0 && args[0] == "before-intrinsic-overloads")
            return;
        Console.WriteLine("== intrinsic identity overload selection ==");
        Type tokenArgument = typeof(FunctionOverload<>).MakeGenericType(typeof(CancellationToken));
        Console.WriteLine($"intrinsic argument => {FunctionCall(tokenArgument, false)}/{FunctionCall(tokenArgument, true)}");
        Type tokenGuid = typeof(TokenOverload<>).MakeGenericType(typeof(Guid));
        Type tokenInt = typeof(TokenOverload<>).MakeGenericType(typeof(int));
        Console.WriteLine($"intrinsic leaf => {TokenCall(tokenGuid, false)}/{TokenCall(tokenGuid, true)}/{TokenCall(tokenInt, false)}/{TokenCall(tokenInt, true)}");
        Console.WriteLine("intrinsic identity overload selection end");
        if (args.Length > 0 && args[0] == "before-intrinsic-array-elements")
            return;
        RunArrayElements();
        if (args.Length > 0 && args[0] == "before-intrinsic-array-leaves")
            return;
        RunArrayLeaves();
        if (args.Length > 0 && args[0] == "before-intrinsic-constant-leaves")
            return;
        RunConstantLeaves();
        if (args.Length > 0 && args[0] == "before-intrinsic-closed-generic-arguments")
            return;
        RunClosedGenericArguments();
        if (args.Length > 0 && args[0] == "before-delegate-initializer-names")
            return;
        RunInitializerNames();
        if (args.Length > 0 && args[0] == "before-app-library-initializer-names")
            return;
        RunAppLibraryInitializerNames();
    }

    private abstract class AppInitializerDerived : ReflectReturnLib.NamedAppInitializerBase
    {
        private AppInitializerDerived() { }
    }

    private static void RunAppLibraryInitializerNames()
    {
        Console.WriteLine("== application library initializer names ==");
        Console.WriteLine($"app library initializer before => {ReflectReturnLib.NamedInitializerSignals.AppInherited}");
        var soft = (Action)Delegate.CreateDelegate(typeof(Action), typeof(AppInitializerDerived), ".cctor", false, false)!;
        var hard = (Action)Delegate.CreateDelegate(typeof(Action), typeof(AppInitializerDerived), ".cctor", false, true)!;
        Console.WriteLine($"app library initializer bind => {ReflectReturnLib.NamedInitializerSignals.AppInherited}/{soft is not null}/{soft.Target is null}/{InitializerMethodFault(soft)}");
        soft();
        Console.WriteLine($"app library initializer soft call => {ReflectReturnLib.NamedInitializerSignals.AppInherited}");
        hard();
        Console.WriteLine($"app library initializer hard call => {ReflectReturnLib.NamedInitializerSignals.AppInherited}/{InitializerMethodFault(hard)}");
        Console.WriteLine("application library initializer names end");
    }
}
