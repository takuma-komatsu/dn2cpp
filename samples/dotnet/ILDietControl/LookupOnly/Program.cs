using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace ILDietLookupOnly;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("lookup-only prefix");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_METADATA_LOOKUP") == "1")
            return;
        Console.WriteLine("== metadata-only reflection ==");
        Type type = typeof(MetadataOnly);
        Console.WriteLine("method-found=" + (type.GetMethod("Uncalled") is not null));
        Console.WriteLine("method-parameter=" + type.GetMethod("Signature")?.GetParameters()[0].ParameterType.Name);
        Console.WriteLine("signature-virtual=" + (type.GetMethod("Signature")!.GetParameters()[0].ParameterType.GetMethod("UnusedVirtual") is not null));
        Console.WriteLine("generic-method=" + (type.GetMethod("Identity")?.IsGenericMethodDefinition ?? false));
        Console.WriteLine("property-found=" + (type.GetProperty("Label") is not null));
        Console.WriteLine("property-accessors=" + (type.GetProperty("Label")?.GetAccessors().Length ?? -1));
        Console.WriteLine("field-found=" + (type.GetField("Count") is not null));
        const BindingFlags Own = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        MethodInfo[] methods = type.GetMethods(Own);
        Console.WriteLine("declared-methods=" + methods.Length);
        bool add = false, remove = false;
        foreach (MethodInfo method in methods)
        {
            add |= method.Name == "add_Tick";
            remove |= method.Name == "remove_Tick";
        }
        Console.WriteLine("event-accessors=" + add + ":" + remove);
        Console.WriteLine("metadata-only reflection end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_FIELD_BOXING") == "1")
            return;
        Console.WriteLine("== reflected field boxing ==");
        Type wallet = typeof(Wallet);
        Console.WriteLine("application-box");
        Console.WriteLine(wallet.GetMethod("Deposit")!.GetParameters()[0].ParameterType.GetField("Zero")!.GetValue(null));
        Console.WriteLine("library-box");
        Console.WriteLine(wallet.GetMethod("DepositLibrary")!.GetParameters()[0].ParameterType.GetField("Zero")!.GetValue(null));
        Console.WriteLine("generic-box");
        Console.WriteLine(wallet.GetMethod("DepositGeneric")!.GetParameters()[0].ParameterType.GetField("Zero")!.GetValue(null));
        Console.WriteLine("reflected field boxing end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_FIELD_CONTEXT") == "1")
            return;
        Console.WriteLine("== closed field contexts ==");
        Console.WriteLine(wallet.GetMethod("DepositDerived")!.GetParameters()[0].ParameterType.BaseType!.GetField("Zero")!.GetValue(null));
        Console.WriteLine(wallet.GetMethod("DepositArray")!.GetParameters()[0].ParameterType.GetField("Zero")!.GetValue(null));
        Console.WriteLine("closed field contexts end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_FIELD_OWNER") == "1")
            return;
        Console.WriteLine("== reference field owners ==");
        Type outer = wallet.GetMethod("DepositOuter")!.GetParameters()[0].ParameterType;
        Console.WriteLine(outer.GetField("Left")!.FieldType.GetField("Zero")!.GetValue(null));
        Console.WriteLine(outer.GetField("Right")!.FieldType.GetField("Zero")!.GetValue(null));
        Console.WriteLine("reference field owners end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_FIELD_SHAPE") == "1")
            return;
        Console.WriteLine("== field type shapes ==");
        Type shapes = wallet.GetMethod("DepositShapes")!.GetParameters()[0].ParameterType;
        Console.WriteLine(shapes.GetField("Array")!.FieldType.GetElementType()!.GetField("Zero")!.GetValue(null));
        Console.WriteLine(shapes.GetField("Generic")!.FieldType.GetGenericArguments()[0].GetField("Zero")!.GetValue(null));
        Console.WriteLine("field type shapes end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_FIELD_CYCLE") == "1")
            return;
        Console.WriteLine("== finite field contexts ==");
        Type cycle = wallet.GetMethod("DepositCycle")!.GetParameters()[0].ParameterType;
        Type next = cycle.GetField("Next")!.FieldType;
        Console.WriteLine(next.GetField("Zero")!.GetValue(null));
        Console.WriteLine(next.GetField("Next")!.FieldType.GetField("Zero")!.GetValue(null));
        Console.WriteLine("finite field contexts end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_MEMBER_FIELD_SIGNATURE") == "1")
            return;
        Console.WriteLine("== closed member field signatures ==");
        Type memberOwner = typeof(MemberSignatureOwner<int>);
        Console.WriteLine(memberOwner.GetMethod("Return")!.ReturnType.GetField("Zero")!.GetValue(null));
        Console.WriteLine(memberOwner.GetMethod("Parameter")!.GetParameters()[0].ParameterType.GetField("Zero")!.GetValue(null));
        Console.WriteLine("closed member field signatures end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_BASE_FIELD_SIGNATURE") == "1")
            return;
        Console.WriteLine("== closed base field signatures ==");
        Type baseOwner = typeof(BaseSignatureDerived<int>).BaseType!;
        Console.WriteLine(baseOwner.GetMethod("Return")!.ReturnType.GetField("Zero")!.GetValue(null));
        Console.WriteLine(baseOwner.GetMethod("Parameter")!.GetParameters()[0].ParameterType.GetField("Zero")!.GetValue(null));
        Console.WriteLine("closed base field signatures end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_FIELD_OWNER_SIGNATURE") == "1")
            return;
        Console.WriteLine("== field owner member signatures ==");
        Type fieldOwner = typeof(FieldSignatureOuter<int>).GetField("Next")!.FieldType;
        Console.WriteLine(fieldOwner.GetMethod("Make")!.ReturnType.GetField("Zero")!.GetValue(null));
        Console.WriteLine("field owner member signatures end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_INTERFACE_OWNER_SIGNATURE") == "1")
            return;
        Console.WriteLine("== interface owner member signatures ==");
        Type interfaceOwner = typeof(InterfaceSignatureOuter<int>).GetInterfaces()[0].GetGenericArguments()[0];
        Console.WriteLine(interfaceOwner.GetMethod("Make")!.ReturnType.GetField("Zero")!.GetValue(null));
        Console.WriteLine("interface owner member signatures end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_REFLECTION_DEPTH_SEEDS") == "1")
            return;
        Console.WriteLine("== reflection depth seeds ==");
        _ = new List<List<List<int>>>();
        Type libraryDepth = typeof(LibraryDepthHolder).GetMethod("Uncalled")!.GetParameters()[0].ParameterType;
        Console.WriteLine(libraryDepth.GetField("Right")!.FieldType.GetField("Zero")!.GetValue(null));
        DepthSeed<List<List<List<int>>>>();
        Type methodDepth = typeof(MethodDepthHolder).GetMethod("Uncalled")!.GetParameters()[0].ParameterType;
        Console.WriteLine(methodDepth.GetField("Right")!.FieldType.GetField("Zero")!.GetValue(null));
        Console.WriteLine("reflection depth seeds end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_CALLER_DEPTH") == "1")
            return;
        Console.WriteLine("== caller reflection depth ==");
#if METHOD_DEPTH_CALLER
        RunDepthCaller<List<List<List<int>>>>();
#else
        ClassDepthCaller<List<List<List<int>>>>.Run();
#endif
        Type callerDepth = typeof(CallerDepthHolder).GetMethod("Uncalled")!.GetParameters()[0].ParameterType;
        Console.WriteLine(callerDepth.GetField("Right")!.FieldType.GetField("Zero")!.GetValue(null));
        Console.WriteLine("caller reflection depth end");
#if EXTENDED_VIRTUAL_DEPTH || EXTENDED_FRAMEWORK_DEPTH || EXTENDED_GVM_DEPTH || EXTENDED_GVM_OWNER_DEPTH || EXTENDED_INHERITED_VIRTUAL_DEPTH || EXTENDED_FACTORY_VIRTUAL_DEPTH || EXTENDED_SYMBOLIC_GVM_DEPTH || EXTENDED_LATE_SYMBOLIC_GVM_DEPTH
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_EXTENDED_DEPTH") == "1")
            return;
        Console.WriteLine("== extended reflection depth ==");
#if EXTENDED_VIRTUAL_DEPTH
        object dispatch = new VirtualDepthCaller<List<List<List<List<int>>>>>();
        Console.WriteLine(dispatch.ToString());
#elif EXTENDED_LATE_SYMBOLIC_GVM_DEPTH
        ISymbolicDepthCaller receiver = MakeSymbolicDepthCaller<List<List<List<List<int>>>>>();
        receiver.Run<List<List<List<List<int>>>>>();
#elif EXTENDED_SYMBOLIC_GVM_DEPTH
        RunSymbolicDepthFactory<List<List<List<List<int>>>>>();
#elif EXTENDED_FACTORY_VIRTUAL_DEPTH
        VirtualDepthFactory<List<List<List<List<int>>>>>.Run();
#elif EXTENDED_INHERITED_VIRTUAL_DEPTH
        object dispatch = new InheritedVirtualDepthCaller<List<List<List<List<int>>>>>();
        Console.WriteLine(dispatch.ToString());
#elif EXTENDED_GVM_OWNER_DEPTH
        IOwnerDepthCaller<DepthLeaf> dispatch = new OwnerDepthCaller();
        dispatch.Run<List<List<List<List<int>>>>>(default(DepthLeaf));
#elif EXTENDED_GVM_DEPTH
        IGenericDepthCaller dispatch = new GenericDepthCaller();
        dispatch.Run<List<List<List<List<int>>>>>();
#else
        _ = new Dictionary<int, List<List<List<List<int>>>>>();
#endif
        Type extendedDepth = typeof(ExtendedDepthHolder).GetMethod("Uncalled")!.GetParameters()[0].ParameterType;
        Console.WriteLine(extendedDepth.GetField("Right")!.FieldType.GetField("Zero")!.GetValue(null));
        Console.WriteLine("extended reflection depth end");
#endif
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_STATE_MACHINE_SIGNATURES") == "1")
            return;
        StateMachineMethods.Run();
    }

    private static void DepthSeed<T>() { }
#if EXTENDED_SYMBOLIC_GVM_DEPTH
    private static void RunSymbolicDepthFactory<T>()
    {
        ISymbolicDepthCaller receiver = new SymbolicDepthCaller<T>();
        receiver.Run<T>();
    }
#endif

#if EXTENDED_LATE_SYMBOLIC_GVM_DEPTH
    private static ISymbolicDepthCaller MakeSymbolicDepthCaller<T>() => new SymbolicDepthCaller<T>();
#endif

    internal static void CallerSeed<T>() { }
#if METHOD_DEPTH_CALLER
    private static void RunDepthCaller<T>() => CallerSeed<List<T>>();
#endif
}

internal sealed class MetadataOnly
{
    public int Count = 0;

    public string Label { get; set; } = "label";

    public event Action Tick
    {
        add => BodyOnlyDependency.Run();
        remove => BodyOnlyDependency.Run();
    }

    public static string Uncalled() => BodyOnlyDependency.Run();

    public static string Signature(SignatureOnlyType argument) => BodyOnlyDependency.Run();

    public static T Identity<T>(T value) => value;
}

internal class SignatureOnlyType
{
    public virtual string UnusedVirtual() => BodyOnlyDependency.Run();
}

internal static class BodyOnlyDependency
{
    public static string Run() => "unreachable-body";
}

internal sealed class Wallet
{
    public static void Deposit(Money amount) => BodyOnlyDependency.Run();

    public static void DepositLibrary(LibraryWallet amount) => BodyOnlyDependency.Run();

    public static void DepositGeneric(GenericWallet<GenericMoney> amount) => BodyOnlyDependency.Run();

    public static void DepositDerived(DerivedWallet<InheritedMoney> amount) => BodyOnlyDependency.Run();

    public static void DepositArray(ArrayWallet<int> amount) => BodyOnlyDependency.Run();

    public static void DepositOuter(OuterWallet<ReferenceMoney, OtherReferenceMoney> amount) => BodyOnlyDependency.Run();

    public static void DepositShapes(FieldShapeWallet<ArrayOwnerMoney, GenericArgumentMoney> amount) => BodyOnlyDependency.Run();

    public static void DepositCycle(CyclePair<CycleFirst, CycleSecond> amount) => BodyOnlyDependency.Run();
}

// Initializer bodies would root these types before the signature-only path.
#pragma warning disable CS0649, CS8618
internal struct Money
{
    public static Money Zero;

    public override string ToString() => "0 JPY";
}

internal sealed class LibraryWallet
{
    public static ILDietControlLib.ReflectionMoney Zero;
    public static SignatureOnlyType? Reference;
}

internal sealed class GenericWallet<T>
{
    public static T Zero;
}

internal sealed class DerivedWallet<T> : BaseWallet<T> { }

internal class BaseWallet<T>
{
    public static T Zero;
}

internal sealed class ArrayWallet<T>
{
    public static ValueBox<T[]> Zero;
}

internal sealed class OuterWallet<T, U>
{
    public static InnerWallet<T> Left;
    public static InnerWallet<U> Right;
}

internal sealed class FieldShapeWallet<T, U>
{
    public static InnerWallet<T>[] Array;
    public static List<InnerWallet<U>> Generic;
}

internal class InnerWallet<T>
{
    public static T Zero;

    public virtual string UnusedVirtual() => BodyOnlyDependency.Run();
}

internal class CyclePair<T, U>
{
    public static CyclePair<U, T> Next;
    public static T Zero;

    public virtual string UnusedVirtual() => BodyOnlyDependency.Run();
}
#pragma warning restore CS0649, CS8618

internal struct GenericMoney
{
    public override string ToString() => "0 EUR";
}

internal struct InheritedMoney
{
    public override string ToString() => "0 GBP";
}

internal struct ValueBox<T>
{
    public override string ToString() => "boxed-array-argument";
}

internal struct ReferenceMoney
{
    public override string ToString() => "0 CHF";
}

internal struct OtherReferenceMoney
{
    public override string ToString() => "0 CAD";
}

internal struct ArrayOwnerMoney
{
    public override string ToString() => "array-owner-box";
}

internal struct GenericArgumentMoney
{
    public override string ToString() => "generic-argument-owner-box";
}

internal struct CycleFirst
{
    public override string ToString() => "boxed-cycle-a";
}

internal struct CycleSecond
{
    public override string ToString() => "boxed-cycle-b";
}

internal sealed class MemberSignatureOwner<T>
{
    public static MemberSignaturePair<T, MemberReturnMoney>? Return()
    {
        BodyOnlyDependency.Run();
        return null;
    }

    public static void Parameter(MemberSignaturePair<T, MemberParameterMoney> value) => BodyOnlyDependency.Run();
}

#pragma warning disable CS0649, CS8618
internal sealed class MemberSignaturePair<A, B>
{
    public static B Zero;
}
#pragma warning restore CS0649, CS8618

internal struct MemberReturnMoney
{
    public override string ToString() => "0 NZD";
}

internal struct MemberParameterMoney
{
    public override string ToString() => "0 SGD";
}

internal sealed class BaseSignatureDerived<T> : BaseSignatureOwner<T> { }

internal class BaseSignatureOwner<T>
{
    public static MemberSignaturePair<T, BaseReturnMoney>? Return()
    {
        BodyOnlyDependency.Run();
        return null;
    }

    public static void Parameter(MemberSignaturePair<T, BaseParameterMoney> value) => BodyOnlyDependency.Run();
}

internal struct BaseReturnMoney
{
    public override string ToString() => "base-return-money";
}

internal struct BaseParameterMoney
{
    public override string ToString() => "base-parameter-money";
}

#pragma warning disable CS0649, CS8618
internal sealed class FieldSignatureOuter<T>
{
    public static FieldSignatureInner<T> Next;
}

internal sealed class FieldSignaturePair<A, B>
{
    public static B Zero;
}
#pragma warning restore CS0649, CS8618

internal sealed class FieldSignatureInner<T>
{
    public static FieldSignaturePair<T, FieldSignatureMoney>? Make()
    {
        FieldSignatureBodyOnlyDependency.Run();
        return null;
    }
}

internal struct FieldSignatureMoney
{
    public override string ToString() => "field-owner-money";
}

internal static class FieldSignatureBodyOnlyDependency
{
    public static void Run() { }
}

internal sealed class InterfaceSignatureOuter<T> : InterfaceSignatureMarker<InterfaceSignatureInner<T>> { }

internal interface InterfaceSignatureMarker<T> { }

#pragma warning disable CS0649, CS8618
internal sealed class InterfaceSignaturePair<A, B>
{
    public static B Zero;
}
#pragma warning restore CS0649, CS8618

internal sealed class InterfaceSignatureInner<T>
{
    public static InterfaceSignaturePair<T, InterfaceSignatureMoney>? Make()
    {
        InterfaceSignatureBodyOnlyDependency.Run();
        return null;
    }
}

internal struct InterfaceSignatureMoney
{
    public override string ToString() => "interface-owner-money";
}

internal static class InterfaceSignatureBodyOnlyDependency
{
    public static void Run() { }
}

internal sealed class LibraryDepthHolder
{
    public static void Uncalled(LibraryDepthOuter<DepthLeaf> value) => DepthBodyOnlyDependency.Run();
}

internal sealed class MethodDepthHolder
{
    public static void Uncalled(MethodDepthOuter<DepthLeaf> value) => DepthBodyOnlyDependency.Run();
}

#pragma warning disable CS0649, CS8618
internal sealed class LibraryDepthOuter<T>
{
    public static DepthInner<LibraryDepthFirst<DepthLayer<T>>> Left;
    public static DepthInner<LibraryDepthSecond<DepthLayer<T>>> Right;
}

internal sealed class MethodDepthOuter<T>
{
    public static DepthInner<MethodDepthFirst<DepthLayer<DepthLayer<T>>>> Left;
    public static DepthInner<MethodDepthSecond<DepthLayer<DepthLayer<T>>>> Right;
}

internal sealed class DepthInner<T>
{
    public static T Zero;
}
#pragma warning restore CS0649, CS8618

internal struct DepthLeaf { }
internal struct DepthLayer<T> { }
internal struct LibraryDepthFirst<T> { public override string ToString() => "library-depth-first"; }
internal struct LibraryDepthSecond<T> { public override string ToString() => "library-depth-second"; }
internal struct MethodDepthFirst<T> { public override string ToString() => "method-depth-first"; }
internal struct MethodDepthSecond<T> { public override string ToString() => "method-depth-second"; }

internal static class DepthBodyOnlyDependency
{
    public static void Run() { }
}

#if !METHOD_DEPTH_CALLER
internal sealed class ClassDepthCaller<T>
{
    public static void Run() => Program.CallerSeed<List<T>>();
}
#endif

internal sealed class CallerDepthHolder
{
    public static void Uncalled(CallerDepthOuter<DepthLeaf> value) => CallerDepthBodyOnlyDependency.Run();
}

#pragma warning disable CS0649, CS8618
internal sealed class CallerDepthOuter<T>
{
    public static CallerDepthInner<CallerDepthFirst<DepthLayer<DepthLayer<DepthLayer<T>>>>> Left;
    public static CallerDepthInner<CallerDepthSecond<DepthLayer<DepthLayer<DepthLayer<T>>>>> Right;
}

internal sealed class CallerDepthInner<T> { public static T Zero; }
#pragma warning restore CS0649, CS8618

internal struct CallerDepthFirst<T> { public override string ToString() => "caller-depth-first"; }
internal struct CallerDepthSecond<T>
{
#if METHOD_DEPTH_CALLER
    public override string ToString() => "method-caller-depth-second";
#else
    public override string ToString() => "class-caller-depth-second";
#endif
}

internal static class CallerDepthBodyOnlyDependency
{
    public static void Run() { }
}

#if EXTENDED_VIRTUAL_DEPTH
internal sealed class VirtualDepthCaller<T>
{
    public override string ToString()
    {
        Program.CallerSeed<List<T>>();
        return "virtual-depth-caller";
    }
}
#endif

#if EXTENDED_INHERITED_VIRTUAL_DEPTH
internal sealed class InheritedVirtualDepthCaller<T> : VirtualDepthBase<T> { }

internal class VirtualDepthBase<T>
{
    public override string ToString()
    {
        Program.CallerSeed<List<T>>();
        return "inherited-virtual-depth-caller";
    }
}
#endif

#if EXTENDED_FACTORY_VIRTUAL_DEPTH
internal static class VirtualDepthFactory<T>
{
    public static void Run()
    {
        object dispatch = new FactoryVirtualDepthCaller<T>();
        Console.WriteLine(dispatch.ToString());
    }
}

internal sealed class FactoryVirtualDepthCaller<T> : FactoryVirtualDepthBase<T> { }

internal class FactoryVirtualDepthBase<T>
{
    public override string ToString()
    {
        Program.CallerSeed<List<T>>();
        return "factory-virtual-depth-caller";
    }
}
#endif

#if EXTENDED_SYMBOLIC_GVM_DEPTH || EXTENDED_LATE_SYMBOLIC_GVM_DEPTH
internal interface ISymbolicDepthCaller { void Run<U>(); }

internal sealed class SymbolicDepthCaller<T> : ISymbolicDepthCaller
{
    public void Run<U>() => Program.CallerSeed<List<U>>();
}
#endif

#if EXTENDED_GVM_DEPTH
internal interface IGenericDepthCaller { void Run<T>(); }

internal sealed class GenericDepthCaller : IGenericDepthCaller
{
    public void Run<T>() => Program.CallerSeed<List<T>>();
}
#endif

#if EXTENDED_GVM_OWNER_DEPTH
internal interface IOwnerDepthCaller<A> { void Run<T>(A value); }

internal sealed class OwnerDepthCaller : IOwnerDepthCaller<DepthLeaf>
{
    public void Run<T>(DepthLeaf value) => Program.CallerSeed<List<T>>();
}
#endif

#if EXTENDED_VIRTUAL_DEPTH || EXTENDED_FRAMEWORK_DEPTH || EXTENDED_GVM_DEPTH || EXTENDED_GVM_OWNER_DEPTH || EXTENDED_INHERITED_VIRTUAL_DEPTH || EXTENDED_FACTORY_VIRTUAL_DEPTH || EXTENDED_SYMBOLIC_GVM_DEPTH || EXTENDED_LATE_SYMBOLIC_GVM_DEPTH
internal sealed class ExtendedDepthHolder
{
    public static void Uncalled(ExtendedDepthOuter<DepthLeaf> value) => ExtendedDepthBodyOnlyDependency.Run();
}

#pragma warning disable CS0649, CS8618
internal sealed class ExtendedDepthOuter<T>
{
    public static ExtendedDepthInner<ExtendedDepthFirst<DepthLayer<DepthLayer<DepthLayer<DepthLayer<T>>>>>> Left;
    public static ExtendedDepthInner<ExtendedDepthSecond<DepthLayer<DepthLayer<DepthLayer<DepthLayer<T>>>>>> Right;
}

internal sealed class ExtendedDepthInner<T> { public static T Zero; }
#pragma warning restore CS0649, CS8618

internal struct ExtendedDepthFirst<T> { public override string ToString() => "extended-depth-first"; }
internal struct ExtendedDepthSecond<T>
{
#if EXTENDED_VIRTUAL_DEPTH
    public override string ToString() => "virtual-depth-second";
#elif EXTENDED_LATE_SYMBOLIC_GVM_DEPTH
    public override string ToString() => "late-symbolic-gvm-depth-second";
#elif EXTENDED_SYMBOLIC_GVM_DEPTH
    public override string ToString() => "symbolic-gvm-depth-second";
#elif EXTENDED_FACTORY_VIRTUAL_DEPTH
    public override string ToString() => "factory-virtual-depth-second";
#elif EXTENDED_INHERITED_VIRTUAL_DEPTH
    public override string ToString() => "inherited-virtual-depth-second";
#elif EXTENDED_GVM_OWNER_DEPTH
    public override string ToString() => "gvm-owner-depth-second";
#elif EXTENDED_GVM_DEPTH
    public override string ToString() => "gvm-depth-second";
#else
    public override string ToString() => "framework-depth-second";
#endif
}

internal static class ExtendedDepthBodyOnlyDependency
{
    public static void Run() { }
}
#endif
