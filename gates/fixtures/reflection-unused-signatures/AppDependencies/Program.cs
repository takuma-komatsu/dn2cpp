using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace ReflectionSignatureDependencies;

internal sealed class MethodHolder<T>
{
    public Ext.Box<T>? Get() => null;
}

internal sealed class CtorHolder<T>
{
    public CtorHolder(Ext.Box<T>? value) { }
}

internal sealed class GenericHolder<T>
{
    public Ext.Box<T>? NeedsExt<U>(U value) => null;
}

internal sealed class Node<T>
{
    public Node<List<T>>? Next = default;
}

internal sealed class GrowingPair<A, B>
{
    public GrowingPair<List<B>, A>? Next = default;
}

internal sealed class ResetNode<T>
{
    public ResetNode<List<int>>? Next = default;
}

internal sealed class SwapNode<A, B>
{
    public SwapNode<B, A>? Next = default;
}

internal sealed class ConvergingNode<A, B>
{
    public ConvergingNode<List<B>, int>? Next = default;
}

internal sealed class BranchNode<A, B>
{
    public BranchNode<List<B>, int>? Left = default;
    public BranchNode<int, List<A>>? Right = default;
}

internal sealed class OriginNode<A, B>
{
    public OriginHelper<A>? Left = default;
    public OriginHelper<B>? Right = default;
}

internal sealed class OriginHelper<T>
{
    public OriginNode<int, List<T>>? Next = default;
}

internal sealed class FiniteBranches<A, B>
{
    public FiniteBranches<List<B>, int>? Left = default;
    public FiniteBranches<List<long>, int>? Right = default;
}

internal class SlotBase<T>
{
    public virtual Node<T>? Get() => null;
}

internal sealed class SlotDerived<T> : SlotBase<T>
{
    public override Node<T>? Get() => null;
}

internal class ResetSlotBase<T>
{
    public virtual ResetNode<T>? Get() => null;
}

internal sealed class ResetSlotDerived<T> : ResetSlotBase<T>
{
    public override ResetNode<T>? Get() => null;
}

internal interface ICompletionSlot<T>
{
    Node<T>? Get();
}

internal sealed class CompletionImpl<T> : ICompletionSlot<T>
{
    Node<T>? ICompletionSlot<T>.Get() => null;
}

internal interface IResetCompletionSlot<T>
{
    ResetNode<T>? Get();
}

internal sealed class ResetCompletionImpl<T> : IResetCompletionSlot<T>
{
    ResetNode<T>? IResetCompletionSlot<T>.Get() => null;
}

internal static class Subject
{
    public static int Used() => 42;
    public static U Supported<U>(U value) => value;
    public static ReflectionUnusedSignature.DefaultHolder<int> Plain(
        ReflectionUnusedSignature.DefaultHolder<int> value) => value;
    public static void PlainStruct(ReflectionUnusedSignature.SDefault<int> value) { }
    public static MethodHolder<int> MethodOnly<U>(MethodHolder<int> value, U fallback) => value;
    public static CtorHolder<int> CtorOnly<U>(CtorHolder<int> value, U fallback) => value;
    public static ReflectionUnusedSignature.IMethodSignature<int> InterfaceOnly<U>(
        ReflectionUnusedSignature.IMethodSignature<int> value, U fallback) => value;
    public static GenericHolder<int> GenericOwner<U>(GenericHolder<int> value, U fallback) => value;
    public static Node<int> Growing<U>(Node<int> value, U fallback) => value;
    public static GrowingPair<int, long> CrossGrowing<U>(GrowingPair<int, long> value, U fallback) => value;
    public static ResetNode<long> Reset<U>(ResetNode<long> value, U fallback) => value;
    public static SwapNode<int, long> Swap<U>(SwapNode<int, long> value, U fallback) => value;
    public static ConvergingNode<int, long> Converging<U>(ConvergingNode<int, long> value, U fallback) => value;
    public static BranchNode<int, long> Branched<U>(BranchNode<int, long> value, U fallback) => value;
    public static OriginNode<int, int> OriginOverlap<U>(OriginNode<int, int> value, U fallback) => value;
    public static FiniteBranches<int, long> BranchFinite<U>(FiniteBranches<int, long> value, U fallback) => value;
    public static ReflectionUnusedSignature.VirtualDerived<int> OverrideOnly<U>(
        ReflectionUnusedSignature.VirtualDerived<int> value, U fallback) => value;
    public static ReflectionUnusedSignature.VirtualBase<int> InheritedSlot<U>(
        ReflectionUnusedSignature.VirtualBase<int> value, U fallback) => value;
    public static ReflectionUnusedSignature.VirtualSibling<int> VirtualSibling<U>(
        ReflectionUnusedSignature.VirtualSibling<int> value, U fallback) => value;
    public static ReflectionUnusedSignature.DefaultHolder<int> DefaultInterface<U>(
        ReflectionUnusedSignature.DefaultHolder<int> value, U fallback) => value;
    public static ReflectionUnusedSignature.SupportedDefaultHolder<int> SupportedDefaultInterface<U>(
        ReflectionUnusedSignature.SupportedDefaultHolder<int> value, U fallback) => value;
    public static ReflectionUnusedSignature.IDefaultSignature<int> RelationOnlyInterface<U>(
        ReflectionUnusedSignature.IDefaultSignature<int> value, U fallback) => value;
    public static SlotDerived<int> OverrideGrowth<U>(SlotDerived<int> value, U fallback) => value;
    public static ResetSlotDerived<int> OverrideFinite<U>(ResetSlotDerived<int> value, U fallback) => value;
    public static CompletionImpl<int> ExplicitGrowth<U>(CompletionImpl<int> value, U fallback) => value;
    public static ResetCompletionImpl<int> ExplicitFinite<U>(ResetCompletionImpl<int> value, U fallback) => value;
    public static ReflectionUnusedSignature.SDefault<long> StructDefinitionOnly<U>(
        ReflectionUnusedSignature.SDefault<long> value, U fallback) => value;
    public static ReflectionUnusedSignature.SDefault<int> StructPair<U>(
        ReflectionUnusedSignature.SDefault<int> value, U fallback) => value;
}

internal static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== signature dependency layouts ==");
        Console.WriteLine("used=" + Subject.Used());
        MethodInfo supported = typeof(Subject).GetMethod("Supported")!;
        Console.WriteLine("definition=" + supported.IsGenericMethodDefinition
            + " parameter=" + supported.GetGenericArguments()[0].Name);
#if METHOD_BODY_NEEDED
        Console.WriteLine("method body needed=" + (new MethodHolder<int>().Get() is null));
#elif GROWTH_BODY_NEEDED
        Console.WriteLine("growth body needed=" + (new Node<int>().Next is null));
#endif
        if (args.Length > 0 && args[0] == "describe-layout-dependencies")
        {
            Console.WriteLine("== unused signature dependencies ==");
            Describe(new[] { "MethodOnly", "CtorOnly", "InterfaceOnly", "GenericOwner",
                "Growing", "CrossGrowing", "Reset", "Swap", "Converging" });
            Console.WriteLine("unused signature dependencies end");
        }
        if (args.Length > 0 && args[0] == "describe-layout-paths")
        {
            Console.WriteLine("== additional signature paths ==");
            Describe(new[] { "Branched", "OriginOverlap", "BranchFinite", "OverrideOnly", "InheritedSlot", "VirtualSibling" });
            Console.WriteLine("additional signature paths end");
        }
        if (args.Length > 0 && args[0] == "describe-default-interface-layout")
        {
            Console.WriteLine("== default interface layout ==");
            Describe(new[] { "DefaultInterface", "SupportedDefaultInterface", "RelationOnlyInterface" });
            Console.WriteLine("default interface layout end");
        }
        if (args.Length > 0 && args[0] == "describe-completion-layouts")
        {
            Console.WriteLine("== completion signature layouts ==");
            MethodInfo? plain = typeof(Subject).GetMethod("Plain");
            Console.WriteLine("ordinary row=" + (plain is not null) + " generic=" + (plain is not null && plain.IsGenericMethod));
            Describe(new[] { "OverrideGrowth", "OverrideFinite", "ExplicitGrowth", "ExplicitFinite",
                "StructDefinitionOnly", "StructPair" }, definitions: true);
            Console.WriteLine("completion signature layouts end");
        }
        Console.WriteLine("signature dependency layouts end");
    }

    private static void Describe(string[] names, bool definitions = false)
    {
        foreach (string name in names)
        {
            MethodInfo? method = typeof(Subject).GetMethod(name);
            Console.WriteLine(name + " found=" + (method is not null));
            if (method is null)
                continue;
            if (definitions)
                Console.WriteLine(name + " definition=" + method.IsGenericMethodDefinition
                    + " formal=" + method.GetGenericArguments()[0].Name);
            Type result = method.ReturnType;
            Type parameter = method.GetParameters()[0].ParameterType;
            Type returnParameter = method.ReturnParameter.ParameterType;
            Console.WriteLine(name + " types=" + result.Name + "/" + parameter.Name + "/" + returnParameter.Name);
            Console.WriteLine(name + " identity=" + ReferenceEquals(result, parameter)
                + "/" + ReferenceEquals(result, returnParameter));
            if (name == "GenericOwner")
                Console.WriteLine("GenericOwner member found=" + (result.GetMethod("NeedsExt") is not null));
            if (name == "OverrideFinite")
            {
                MethodInfo getter = result.GetMethod("Get")!;
                Console.WriteLine("OverrideFinite getter=" + getter.ReturnType.Name
                    + " identity=" + ReferenceEquals(getter.ReturnType, getter.ReturnParameter.ParameterType)
                    + " parameters=" + getter.GetParameters().Length);
            }
        }
    }
}
