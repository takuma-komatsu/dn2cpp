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
        ReferencedAssemblyReflection();
        LateBoundConstruction();
    }

    private static void LateBoundConstruction()
    {
        // The [Preserve]'d library class is constructed ONLY late-bound: a
        // preserved instance ctor implies allocation, so interface dispatch on
        // the minted instance resolves the base's explicit implementation
        // (and its virtual hook) instead of landing on a slot-miss trap.
        object instance = Activator.CreateInstance(typeof(PreserveControlLib.LateInitService));
        Console.WriteLine("late-bound=" + ((PreserveControlLib.ILateInit)instance).InitializeAll());
    }

    private static void PreservedReflection()
    {
        const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
        Type type = typeof(PreserveControlLib.AttributeMembers);
        type.GetMethod("BuiltInMethod", Flags).Invoke(null, null);
        type.GetMethod("AssemblyAwareDerivedMethod", Flags).Invoke(null, null);

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

    private static void ReferencedAssemblyReflection()
    {
        const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
        Assembly assembly = Assembly.Load("PreserveAssemblyLib");
        Type fieldType = assembly.GetType("PreserveAssemblyLib.AssemblyFieldTarget", true);
        Console.WriteLine("initialized-field="
            + fieldType.GetField("InitializedField", Flags).GetValue(null));

        Type collisionType = assembly.GetType("PreserveAssemblyLib.CollidingAttributeTarget", true);
        Console.WriteLine("non-derived-dropped="
            + (collisionType.GetMethod("NonDerivedMethod", Flags) is null));
    }
}
