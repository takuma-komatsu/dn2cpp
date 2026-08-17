using System;
using System.Globalization;
using System.Reflection;

namespace PreserveControl;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine(PreserveControlLib.Live.Value());
        Console.WriteLine(typeof(PreserveControlLib.Outer.Nested<int>).Name);
        Console.WriteLine(typeof(PreserveControlLib.ConditionalUsed).Name);
        PreservedReflection();
    }

    private static void PreservedReflection()
    {
        const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
        Type type = typeof(PreserveControlLib.AttributeMembers);
        type.GetMethod("BuiltInMethod", Flags).Invoke(null, null);

        FieldInfo field = type.GetField("PreservedField", Flags);
        field.SetValue(null, 17);
        Console.WriteLine("preserved-field=" + field.GetValue(null));

        PropertyInfo property = type.GetProperty("PreservedProperty", Flags);
        property.SetValue(null, 23);
        Console.WriteLine("preserved-property=" + property.GetValue(null));

        Console.WriteLine("dropped-method=" + (type.GetMethod("DroppedMethod", Flags) is null));
        Console.WriteLine("delegate-invoke="
            + (typeof(PreserveControlLib.PreservedDelegate).GetMethod("Invoke") is not null));
    }
}
