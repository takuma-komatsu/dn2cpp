using System;
using System.Globalization;
using System.Reflection;

namespace CanonicalTypeComparisonSubset;

internal static class Program
{
    public sealed class Owner<T>
    {
        public T Value;
        public static T StaticValue;
    }

    public struct ValueOwner<T> { public T Value; }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("canonical-type-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Canonical type comparison contract ==");
        MethodInfo method = typeof(RuntimeTypeHandle).GetMethod("CompareCanonicalHandles",
            BindingFlags.NonPublic | BindingFlags.Static, new Type[] { Type.GetType("System.RuntimeType"), Type.GetType("System.RuntimeType") });
        Check("shape", method is not null && method.IsAssembly && method.IsStatic && !method.IsGenericMethod
            && method.DeclaringType == typeof(RuntimeTypeHandle) && method.ReturnType == typeof(bool)
            && (method.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) != 0);
        Type strings = typeof(Owner<string>);
        Type objects = typeof(Owner<object>);
        Check("type-identity", strings == typeof(Owner<string>) && strings != objects);
        Check("handle-identity", strings.TypeHandle.Equals(typeof(Owner<string>).TypeHandle)
            && !strings.TypeHandle.Equals(objects.TypeHandle));
        Check("handle-roundtrip", Type.GetTypeFromHandle(strings.TypeHandle) == strings
            && Type.GetTypeFromHandle(objects.TypeHandle) == objects);
        Check("generic-definition", strings.GetGenericTypeDefinition() == objects.GetGenericTypeDefinition()
            && strings.GetGenericArguments()[0] == typeof(string) && objects.GetGenericArguments()[0] == typeof(object));
        var text = new Owner<string>();
        var boxed = new Owner<object>();
        FieldInfo textField = strings.GetField("Value");
        FieldInfo objectField = objects.GetField("Value");
        textField.SetValue(text, "text");
        objectField.SetValue(boxed, "object");
        Check("reference-fields", textField.DeclaringType == strings && textField.FieldType == typeof(string)
            && objectField.DeclaringType == objects && objectField.FieldType == typeof(object)
            && (string)textField.GetValue(text) == "text" && (string)objectField.GetValue(boxed) == "object");
        strings.GetField("StaticValue").SetValue(null, "separate");
        Check("static-fields", Owner<string>.StaticValue == "separate" && Owner<object>.StaticValue == null);
        Check("value-arguments", typeof(Owner<int>) != typeof(Owner<long>)
            && !typeof(Owner<int>).TypeHandle.Equals(typeof(Owner<long>).TypeHandle)
            && typeof(Owner<int>).GetField("Value").FieldType == typeof(int)
            && typeof(Owner<long>).GetField("Value").FieldType == typeof(long));
        Type nestedStrings = typeof(Owner<ValueOwner<string>>);
        Type nestedObjects = typeof(Owner<ValueOwner<object>>);
        Check("nested-arguments", nestedStrings != nestedObjects
            && !nestedStrings.TypeHandle.Equals(nestedObjects.TypeHandle)
            && nestedStrings.GetField("Value").FieldType == typeof(ValueOwner<string>)
            && nestedObjects.GetField("Value").FieldType == typeof(ValueOwner<object>));
        object value = new ValueOwner<string>();
        FieldInfo valueField = typeof(ValueOwner<string>).GetField("Value");
        valueField.SetValue(value, "boxed");
        Check("value-owner", valueField.FieldType == typeof(string)
            && (string)valueField.GetValue(value) == "boxed" && ((ValueOwner<string>)value).Value == "boxed");
        Console.WriteLine("Canonical type comparison contract end");
    }
}
