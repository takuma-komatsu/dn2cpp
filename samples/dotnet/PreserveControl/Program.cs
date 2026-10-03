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
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_PRESERVED_BOXES") == "1")
            return;
        Console.WriteLine("== preserved reflection boxes ==");
        PreservedValueBoxes();
        PreservedWrittenBack();
        Console.WriteLine("preserved reflection boxes end");
    }

    private static void PreservedWrittenBack()
    {
        // A struct a preserved method writes back through an out argument or returns by
        // reference dispatches its interface member through the box reflection makes.
        const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
        Type type = typeof(PreserveControlLib.PreservedWrites);
        object[] args = { null };
        type.GetMethod("Fill", Flags).Invoke(null, args);
        Console.WriteLine("preserved-written-back=" + Show(args[0]) + ","
            + Show(type.GetMethod("Lend", Flags).Invoke(null, null)));
    }

    private static void PreservedValueBoxes()
    {
        // A struct that only reflection boxes, read through preserved members or
        // created from a preserved type, dispatches its interface member.
        const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
        Type type = typeof(PreserveControlLib.PreservedBoxes);
        Console.WriteLine("preserved-boxes="
            + Show(type.GetField("Field", Flags).GetValue(null)) + ","
            + Show(type.GetProperty("Point", Flags).GetValue(null)) + ","
            + Show(type.GetMethod("Make", Flags).Invoke(null, null)) + ","
            + Show(Activator.CreateInstance(typeof(PreserveControlLib.PreservedMade))));
    }

    private static string Show(object boxed) => ((PreserveControlLib.IPreservedShow)boxed).Show();

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

public static class UnusedAppType
{
    public static void UnusedAppMethod() => Console.WriteLine("unused-app");
}
