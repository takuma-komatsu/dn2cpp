using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    private static bool ParsePrimitive(TypeDesc type, PrimitiveTypeCode primitive) =>
        type is { Kind: TypeKind.Primitive, IsCanonPlaceholder: false } && type.Primitive == primitive;

    private static bool ParseNamed(TypeDesc type, string name) =>
        type is { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } owner }
        && owner.Module.AssemblyName == "System.Private.CoreLib" && owner.FullName == name;

    internal static PrimitiveParseContract.Input PrimitiveParseInput(TypeDesc type)
    {
        if (ParsePrimitive(type, PrimitiveTypeCode.String)) return PrimitiveParseContract.Input.String;
        if (type is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } span }
            || span.Module.AssemblyName != "System.Private.CoreLib"
            || span.Module.Owner.GenericDefFullName(span) != "System.ReadOnlySpan"
            || span.Context.TypeArgs is not [var element]) return PrimitiveParseContract.Input.None;
        return ParsePrimitive(element, PrimitiveTypeCode.Char) ? PrimitiveParseContract.Input.Chars
            : ParsePrimitive(element, PrimitiveTypeCode.Byte) ? PrimitiveParseContract.Input.Bytes : PrimitiveParseContract.Input.None;
    }

    internal static bool IsPrimitiveParseSignature(MethodSignature<TypeDesc> signature, bool isTry,
        PrimitiveTypeCode primitive, PrimitiveParseContract.Input input, bool styles, bool provider)
    {
        int count = 1 + (styles ? 1 : 0) + (provider ? 1 : 0) + (isTry ? 1 : 0);
        var ps = signature.ParameterTypes;
        return signature.Header.RawValue == 0 && signature.GenericParameterCount == 0
            && signature.RequiredParameterCount == count && ps.Length == count
            && ParsePrimitive(signature.ReturnType, isTry ? PrimitiveTypeCode.Boolean : primitive)
            && PrimitiveParseInput(ps[0]) == input
            && (!styles || ParseNamed(ps[1], "System.Globalization.NumberStyles"))
            && (!provider || ParseNamed(ps[styles ? 2 : 1], "System.IFormatProvider"))
            && (!isTry || ps[^1] is { Kind: TypeKind.ByRef, IsCanonPlaceholder: false, Element: { } element }
                && ParsePrimitive(element, primitive));
    }

    internal static bool IsPrimitiveParseSlotCandidate(MethodInfo method) => PrimitiveParseContract.IsSlot(method.Name)
        && method.Module.Owner.GenericDefFullName(method.DeclaringClass) is "System.Numerics.INumberBase" or "System.ISpanParsable"
        && method.Signature.ParameterTypes.Length != 0
        && PrimitiveParseInput(method.Signature.ParameterTypes[0]) is PrimitiveParseContract.Input.Chars or PrimitiveParseContract.Input.Bytes;

    internal static bool IsPrimitiveParseTarget(MethodInfo method, bool isTry, PrimitiveParseContract.Input input, bool styles)
    {
        var owner = method.DeclaringClass;
        return IsScalarNumericLeafOwner(owner) && ParseDefinitionMatchesModel(method)
            && IsPrimitiveParseSignature(method.Signature, isTry, PrimitiveParseContract.Primitive(owner.FullName), input, styles, true)
            && PrimitiveParseContract.IsTargetDefinition(method.Module.Reader, method.Handle, owner.FullName, isTry, input, styles, true);
    }

    private static bool ParseDefinitionMatchesModel(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (method.Module != owner.Module || owner.Handle.IsNil || method.Handle.IsNil || method.IsSynthetic
            || method.PInvoke is not null || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0)
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return definition.GetDeclaringType() == owner.Handle && definition.RelativeVirtualAddress == method.Rva
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && reader.StringComparer.Equals(definition.Name, method.Name);
    }

    private static bool IsPrimitiveParseBodyCandidate(MethodInfo method) =>
        PrimitiveParseContract.IsBodyCandidate(method.DeclaringClass.FullName, method.Name, method.Attributes,
            PrimitiveParseContract.IsFormatHelper(method.DeclaringClass.FullName, method.Name)
                ? method.Signature.ParameterTypes.Length : -1,
            PrimitiveParseContract.IsCharNumber(method.DeclaringClass.FullName, method.Name)
                && method.Signature.ParameterTypes is [var input, ..]
                && ParsePrimitive(input, PrimitiveTypeCode.String));

    internal static bool IsPrimitiveParseBody(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (!IsPrimitiveParseBodyCandidate(method)
            || owner.Module.AssemblyName != "System.Private.CoreLib" || !ParseDefinitionMatchesModel(method)
            || !PrimitiveParseContract.IsBodyDefinition(method.Module.Reader, method.Module.PE, method.Handle, owner.FullName, method.Name))
            return false;
        if (PrimitiveParseContract.IsFormatHelper(owner.FullName, method.Name))
            return !owner.IsValueType && !owner.IsInterface && owner.GenericArity == 0
                && owner.Context.TypeArgs.Length == 0 && owner.Context.MethodArgs.Length == 0
                && method.Signature.Header.RawValue == 0 && method.Signature.GenericParameterCount == 0
                && method.Signature.RequiredParameterCount == 0 && method.Signature.ParameterTypes.Length == 0
                && ParsePrimitive(method.Signature.ReturnType, PrimitiveTypeCode.Void);
        bool inner = PrimitiveParseContract.IsSlot(method.Name);
        bool styles = method.Name == PrimitiveParseContract.CharName(true, "Parse")
            || method.Name == PrimitiveParseContract.CharName(true, "TryParse");
        bool isTry = method.Name == "TryParse" || method.Name == PrimitiveParseContract.CharName(true, "TryParse")
            || method.Name == PrimitiveParseContract.CharName(false, "TryParse");
        return IsScalarNumericLeafOwner(owner)
            && IsPrimitiveParseSignature(method.Signature, isTry, PrimitiveTypeCode.Char, PrimitiveParseContract.Input.Chars, styles, !inner);
    }

    internal static void ValidatePrimitiveParseBody(MethodInfo method)
    {
        if (IsPrimitiveParseBodyCandidate(method)
            && !IsPrimitiveParseBody(method))
            throw new NotSupportedException("Primitive span parsing requires its concrete CoreLib body and signature");
    }
}
