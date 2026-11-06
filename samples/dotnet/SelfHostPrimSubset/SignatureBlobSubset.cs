using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SignatureBlobSubset;

internal static unsafe class Program
{
    public sealed class Target
    {
        private static int s_value;
        public int Value { get; init; }
        public static void Plain(int value) { }
        public static ref readonly int ReadOnlyRef() => ref s_value;
    }

    private const BindingFlags StaticFlags = BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags InstanceFlags = BindingFlags.NonPublic | BindingFlags.Instance;

    private static void Check(string label, bool success)
    {
        Console.WriteLine("signature-blob-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static void Shape(string label, MethodInfo method, int count)
    {
        Check("shape-" + label, method is not null && method.IsPrivate && method.IsStatic
            && !method.IsGenericMethod && method.ReturnType == typeof(int)
            && method.GetParameters().Length == count
            && (method.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) != 0);
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Signature blob contract ==");
        Shape("parameter", Type.GetType("System.Signature").GetMethod("GetParameterOffsetInternal", StaticFlags,
            new Type[] { typeof(void*), typeof(int), typeof(int) }), 3);
        Shape("type-parameter", Type.GetType("System.Signature").GetMethod("GetTypeParameterOffsetInternal", StaticFlags,
            new Type[] { typeof(void*), typeof(int), typeof(int), typeof(int) }), 4);
        Shape("calling-convention", Type.GetType("System.Signature").GetMethod("GetCallingConventionFromFunctionPointerAtOffsetInternal", StaticFlags,
            new Type[] { typeof(void*), typeof(int), typeof(int) }), 3);

        // Negative offsets return before accessing Signature's uninitialized blob fields.
        object receiver = RuntimeHelpers.GetUninitializedObject(Type.GetType("System.Signature"));
        Check("negative-type-offset", (int)Type.GetType("System.Signature")
            .GetMethod("GetTypeParameterOffset", InstanceFlags).Invoke(receiver, new object[] { -1, int.MaxValue }) == -1);
        Check("negative-callconv", Convert.ToInt32(Type.GetType("System.Signature")
            .GetMethod("GetCallingConventionFromFunctionPointerAtOffset", InstanceFlags).Invoke(receiver, new object[] { -1 })) == 0);

        ParameterInfo plain = typeof(Target).GetMethod("Plain").GetParameters()[0];
        Check("plain-modifiers", plain.GetRequiredCustomModifiers().Length == 0 && plain.GetOptionalCustomModifiers().Length == 0);
        ParameterInfo init = typeof(Target).GetProperty("Value").SetMethod.ReturnParameter;
        Type[] initModifiers = init.GetRequiredCustomModifiers();
        Check("init-modreq", initModifiers.Length == 1 && initModifiers[0] == typeof(IsExternalInit));
        Check("init-modopt", init.GetOptionalCustomModifiers().Length == 0);
        Type[] readOnly = typeof(Target).GetMethod("ReadOnlyRef").ReturnParameter.GetRequiredCustomModifiers();
        Check("readonly-modreq", readOnly.Length == 1 && readOnly[0] == typeof(InAttribute));
        Type function = typeof(delegate*<int, void>);
        Check("function-pointer", function.IsFunctionPointer && !function.IsUnmanagedFunctionPointer
            && function.GetFunctionPointerReturnType() == typeof(void)
            && function.GetFunctionPointerParameterTypes().Length == 1
            && function.GetFunctionPointerParameterTypes()[0] == typeof(int)
            && function.GetFunctionPointerCallingConventions().Length == 0);
        Type unmanaged = typeof(delegate* unmanaged[Cdecl]<int>);
        Check("unmanaged-pointer", unmanaged.IsFunctionPointer && unmanaged.IsUnmanagedFunctionPointer
            && unmanaged.GetFunctionPointerReturnType() == typeof(int)
            && unmanaged.GetFunctionPointerParameterTypes().Length == 0
            && unmanaged.GetFunctionPointerCallingConventions().Length == 0);
        Console.WriteLine("Signature blob contract end");
    }

    // Rewritten to exact CoreLib calls; this entry point is never a CLR oracle.
    private static void ParameterDirect() => throw new InvalidOperationException("fixture missing");
    private static void ParameterPointer() => throw new InvalidOperationException("fixture missing");
    private static void TypeParameterDirect() => throw new InvalidOperationException("fixture missing");
    private static void TypeParameterPointer() => throw new InvalidOperationException("fixture missing");
    private static void CallingConventionDirect() => throw new InvalidOperationException("fixture missing");
    private static void CallingConventionPointer() => throw new InvalidOperationException("fixture missing");

    private static void Refuse(string label, Action action)
    {
        try { action(); Console.WriteLine("signature-blob-" + label + ":accepted"); }
        catch (TargetInvocationException error)
        {
            Console.WriteLine("signature-blob-" + label + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message);
        }
        catch (Exception error)
        {
            Console.WriteLine("signature-blob-" + label + ":" + error.GetType().Name + ":" + error.Message);
        }
    }

    internal static void RunArtifactChecks(string[] args)
    {
        Run();
        Refuse("parameter-direct", ParameterDirect);
        Refuse("parameter-pointer", ParameterPointer);
        Refuse("type-parameter-direct", TypeParameterDirect);
        Refuse("type-parameter-pointer", TypeParameterPointer);
        Refuse("calling-convention-direct", CallingConventionDirect);
        Refuse("calling-convention-pointer", CallingConventionPointer);
        Refuse("parameter-reflection", () => Type.GetType("System.Signature").GetMethod("GetParameterOffsetInternal", StaticFlags,
            new Type[] { typeof(void*), typeof(int), typeof(int) }).Invoke(null, new object[] { null, 0, 0 }));
        Refuse("type-parameter-reflection", () => Type.GetType("System.Signature").GetMethod("GetTypeParameterOffsetInternal", StaticFlags,
            new Type[] { typeof(void*), typeof(int), typeof(int), typeof(int) }).Invoke(null, new object[] { null, 0, 0, 0 }));
        Refuse("calling-convention-reflection", () => Type.GetType("System.Signature").GetMethod("GetCallingConventionFromFunctionPointerAtOffsetInternal", StaticFlags,
            new Type[] { typeof(void*), typeof(int), typeof(int) }).Invoke(null, new object[] { null, 0, 0 }));
        Console.WriteLine("Signature blob boundary end");
    }
}
