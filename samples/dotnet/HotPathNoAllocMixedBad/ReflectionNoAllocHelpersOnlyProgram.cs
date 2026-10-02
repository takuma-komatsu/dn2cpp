using System;
using System.Globalization;
using System.Reflection;
namespace Dn2Cpp.Runtime
{
    // Internal copy of Dn2Cpp.Runtime.HotPathAttribute (matched by full name only).
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class HotPathAttribute : Attribute
    {
        public bool SkipBoundsChecks { get; set; }
        public bool NoAlloc { get; set; }
    }
}

namespace ReflectionNoAllocHelpersOnly
{
    using Dn2Cpp.Runtime;
    internal sealed class Tagged
    {
        public int Value = 1;
        public int Property { get; set; }
        public int Read() => Value;
    }
    internal static class Program
    {
        [HotPath(NoAlloc = true)] private static object Make(Type type) => Activator.CreateInstance(type)!;
        [HotPath(NoAlloc = true)] private static object Construct(ConstructorInfo constructor) => constructor.Invoke(null);
        [HotPath(NoAlloc = true)] private static object? ReadField(FieldInfo field, object value) => field.GetValue(value);
        [HotPath(NoAlloc = true)] private static void WriteField(FieldInfo field, object value, object data) => field.SetValue(value, data);
        [HotPath(NoAlloc = true)] private static void WriteProperty(PropertyInfo property, object value, object data) => property.SetValue(value, data);
        [HotPath(NoAlloc = true)] private static object? InvokeMethod(MethodInfo method, object value) => method.Invoke(value, null);
        private static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            var value = new Tagged();
            object narrow = (byte)7;
            WriteField(typeof(Tagged).GetField("Value")!, value, narrow);
            Console.WriteLine(Make(typeof(Tagged)));
            Console.WriteLine(Construct(typeof(Tagged).GetConstructor(Type.EmptyTypes)!));
            Console.WriteLine(ReadField(typeof(Tagged).GetField("Value")!, value));
            object propertyValue = 2;
            WriteProperty(typeof(Tagged).GetProperty("Property")!, value, propertyValue);
            Console.WriteLine(InvokeMethod(typeof(Tagged).GetMethod("Read")!, value));
        }
    }
}
