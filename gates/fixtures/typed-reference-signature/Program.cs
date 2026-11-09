using System;
using System.Globalization;
using System.Reflection;

namespace TypedReferenceSignature;

internal static class Program
{
    private static int calls;

    // No type token, helper call or typed-reference value roots this identity.
    private static void SignatureOnly(TypedReference value) { calls++; }
    private static void Overload(TypedReference value) { calls++; }
    private static void Overload(object value) { calls++; }
    private static void GenericSignature<T>(TypedReference value) { calls++; }

    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        MethodInfo method = typeof(Program).GetMethod(nameof(SignatureOnly), flags);
        Type parameter = method.GetParameters()[0].ParameterType;
        Console.WriteLine("typedref signature=" + parameter.FullName + "/" + parameter.IsValueType + "/" + parameter.IsByRefLike);
        MethodInfo selected = typeof(Program).GetMethod(nameof(Overload), flags, null, new[] { parameter }, null);
        Console.WriteLine("typedref signature overload=" + (selected != null) + "/"
            + (selected.GetParameters()[0].ParameterType == parameter));
        MethodInfo generic = typeof(Program).GetMethod(nameof(GenericSignature), flags);
        Console.WriteLine("typedref signature generic=" + generic.IsGenericMethodDefinition + "/"
            + (generic.GetParameters()[0].ParameterType == parameter));
        foreach (object argument in new object[] { null, new object(), 3 })
        {
            try
            {
                method.Invoke(null, new object[] { argument });
                Console.WriteLine("typedref signature invoke=returned");
            }
            catch (Exception exception)
            {
                Console.WriteLine("typedref signature invoke=" + exception.GetType().Name);
            }
        }
        Console.WriteLine("typedref signature calls=" + calls);
        if (args.Length != 0)
        {
            try
            {
                _ = parameter.GetMethods();
                Console.WriteLine("typedref signature members=available");
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("typedref signature members=PlatformNotSupportedException");
            }
        }
        Console.WriteLine("typedref signature complete");
    }
}
