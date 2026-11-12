using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // GC's TimeSpan overloads use this field-free validation body, not WaitHandle state.
    internal static bool IsWaitHandleTimeoutConversion(MethodInfo method)
    {
        if (method.Name != "ToTimeoutMilliseconds" || method.DeclaringClass.FullName != "System.Threading.WaitHandle"
            || method.Module.AssemblyName != "System.Private.CoreLib" || method.Module != method.DeclaringClass.Module
            || method.Handle.IsNil || method.Rva == 0 || method.PInvoke is not null
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL)
            return false;
        var signature = method.Signature;
        return signature.Header.RawValue == 0 && signature.GenericParameterCount == 0
            && signature.RequiredParameterCount == 1
            && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }
            && signature.ParameterTypes is [{ Kind: TypeKind.Class, Class: {
                FullName: "System.TimeSpan", IsValueType: true } timeout }]
            && timeout.Module == method.Module;
    }
}
