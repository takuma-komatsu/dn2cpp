using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ILDietControlLib;
using MemberTypes = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace ILDietControl;

internal static class GenericParameterContracts
{
    internal static void Run()
    {
        Console.WriteLine("generic type contracts="
            + Create(Carrier<FirstTypeTarget>.Argument(typeof(FirstTypeTarget))) + "/"
            + Create(Carrier<SecondTypeTarget>.Argument(typeof(SecondTypeTarget))));
        Console.WriteLine("generic method contracts="
            + Create(MethodArgument<FirstMethodTarget>(typeof(FirstMethodTarget))) + "/"
            + Create(MethodArgument<SecondMethodTarget>(typeof(SecondMethodTarget))));
        Console.WriteLine("generic field contract=" + ReadField(FieldArgument<FieldTarget>(typeof(FieldTarget))));
        var storage = Type.GetType("System.String+SearchValuesStorage, System.Private.CoreLib", throwOnError: true)!;
        Console.WriteLine("generic primitive nested field=" + HasNestedStorage<string>(storage));
        GC.KeepAlive(Signature());
        ReadSlot<FieldSignatureTarget>();
        GC.KeepAlive(new DerivedCarrier());
        Console.WriteLine("generic signature contracts="
            + CreatedName(typeof(SignatureTarget)) + "/" + CreatedName(typeof(FieldSignatureTarget)));
        Console.WriteLine("generic inherited interface=" + CreatedName(typeof(InterfaceTarget)));
        Console.WriteLine("generic template contracts=" + CreateTemplateArguments(
            typeof(GenericContractTemplate<,>).MakeGenericType(typeof(TemplateTarget), typeof(TemplateBaseTarget))));
        var reflected = typeof(GenericParameterContracts).GetMethod(nameof(MethodArgument), BindingFlags.NonPublic | BindingFlags.Static)!;
        var closed = reflected.MakeGenericMethod(typeof(MethodIdentityTarget));
        Console.WriteLine("generic reflected method=" + CreatedName(closed.GetGenericArguments()[0]));
        Console.WriteLine("unannotated generic argument=" + Plain<UnannotatedTarget>().Name);
        GC.KeepAlive(AttributeContractBase<object, object>.Create());
        var row = typeof(GenericParameterContracts).GetMethod(nameof(AttributeArgument), BindingFlags.NonPublic | BindingFlags.Static)!
            .GetParameters()[0].GetCustomAttributesData()[0];
        var derived = (Type)row.ConstructorArguments[0].Value!;
        Console.WriteLine("generic attribute base=" + CreatedName(derived.BaseType!.GetGenericArguments()[0]));
    }

    // These helpers receive the target only at run time. The generic uses supply its contract.
    private static string Create(Type type) => ((IResult)Activator.CreateInstance(type)!).Value();
    private static object? ReadField(Type type) => type.InvokeMember("Value",
        BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField, null, null, null);
    private static bool HasNestedStorage<[DynamicallyAccessedMembers(MemberTypes.NonPublicNestedTypes)] T>(Type nested) =>
        nested.InvokeMember("WhiteSpaceChars", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField,
            null, null, null) is not null;
    private static string CreatedName(Type type) => Activator.CreateInstance(type)!.GetType().Name;
    private static string CreateTemplateArguments(Type type)
    {
        Type[] args = type.GetGenericArguments();
        return CreatedName(args[0]) + "/" + CreatedName(args[1]);
    }

    private static Type MethodArgument<
        [DynamicallyAccessedMembers(MemberTypes.PublicParameterlessConstructor)] T>(Type type) => type;

    private static Type FieldArgument<[DynamicallyAccessedMembers(MemberTypes.PublicFields)] T>(Type type) => type;
    private static Type Plain<T>() => typeof(T);

    private static void AttributeArgument([ContractType(typeof(AttributeContractDerived))] int value) { }
    private sealed class ContractTypeAttribute : Attribute
    {
        public ContractTypeAttribute(Type type) { }
    }

    private sealed class Carrier<[DynamicallyAccessedMembers(MemberTypes.PublicParameterlessConstructor)] T>
    {
        internal static Type Argument(Type type) => type;
    }

    private interface IResult { string Value(); }
    private sealed class FirstTypeTarget : IResult { public string Value() => "type-one"; }
    private sealed class SecondTypeTarget : IResult { public string Value() => "type-two"; }
    private sealed class FirstMethodTarget : IResult { public string Value() => "method-one"; }
    private sealed class SecondMethodTarget : IResult { public string Value() => "method-two"; }
    private sealed class FieldTarget { public static readonly string Value = "initialized"; }
    private sealed class UnannotatedTarget { public UnannotatedTarget() { } }
    private sealed class SignatureTarget { }
    private sealed class FieldSignatureTarget { }
    private sealed class InterfaceTarget { }
    private sealed class TemplateTarget { }
    private sealed class TemplateBaseTarget { }
    private sealed class MethodIdentityTarget { }
    private static Carrier<SignatureTarget>? Signature() => null;
    private static Carrier<UnannotatedTarget>? UnusedSignature() => null;
    private static class Storage<T> { internal static Carrier<T>? Slot = null; }
    private static void ReadSlot<T>() => GC.KeepAlive(Storage<T>.Slot);
    private interface ICarrier<[DynamicallyAccessedMembers(MemberTypes.PublicParameterlessConstructor)] T> { }
    private class BaseCarrier : ICarrier<InterfaceTarget> { }
    private sealed class DerivedCarrier : BaseCarrier { }
}

internal class GenericContractBase<[DynamicallyAccessedMembers(MemberTypes.PublicParameterlessConstructor)] T> { }
internal sealed class GenericContractTemplate<[DynamicallyAccessedMembers(MemberTypes.PublicParameterlessConstructor)] T, U>
    : GenericContractBase<U> { }
