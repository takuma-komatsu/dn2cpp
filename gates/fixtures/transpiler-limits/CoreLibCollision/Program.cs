using System;
using System.Globalization;

#pragma warning disable CS0436 // These fixtures deliberately declare distinct same-named application types.
internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#if APP_OBJECT
        Console.WriteLine("application Object: " + new Holder().Read());
#elif APP_VALUETYPE
        Console.WriteLine("application ValueType: " + new System.ValueType().GetType().Name);
#elif APP_UNSAFE
        Console.WriteLine("application Unsafe: " + System.Runtime.CompilerServices.Unsafe.SizeOf<long>());
#else
        Console.WriteLine("embedded attribute: " + new System.Runtime.CompilerServices.NullableAttribute(5).Value);
        Console.WriteLine("ordinary body: control-body");
        Console.WriteLine("ordinary input end");
#endif
    }
}

#if APP_OBJECT
internal sealed class Holder
{
    internal int Read() => 17;
}
#endif

#if APP_OBJECT
namespace System
{
    public class Object
    {
        public static string ApplicationBody() => "object-body";
    }
}
#elif APP_VALUETYPE
namespace System
{
    public class ValueType
    {
        public static string ApplicationBody() => "valuetype-body";
    }
}
#elif APP_UNSAFE
namespace System.Runtime.CompilerServices
{
    public static class Unsafe
    {
        public static int SizeOf<T>() => 123;
    }
}
#else
namespace System.Runtime.CompilerServices
{
    [CompilerGenerated]
    [Microsoft.CodeAnalysis.Embedded]
    internal sealed class NullableAttribute : Attribute
    {
        internal readonly byte Value;
        internal NullableAttribute(byte value) => Value = value;
    }
}

namespace Microsoft.CodeAnalysis
{
    [System.Runtime.CompilerServices.CompilerGenerated]
    [Embedded]
    internal sealed class EmbeddedAttribute : Attribute { }
}

namespace System
{
    internal static class ThrowHelper
    {
        internal static string ApplicationBody() => "polyfill-body";
    }
}
#endif
