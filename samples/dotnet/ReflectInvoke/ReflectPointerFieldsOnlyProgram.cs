using System;
using System.Globalization;
using System.Reflection;

// No application field or method signature names a pointer. A library field or
// an explicitly preserved field must keep the class its getter boxes as.
static class ReflectPointerFieldsOnlyProgram
{
    private static unsafe string Read(FieldInfo field, object? receiver) =>
        field.GetValue(receiver) is Pointer box
            ? box.GetType().FullName + ":" + ((nuint)Pointer.Unbox(box)).ToString("x") : "unexpected";

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#if POINTER_FIELDS_PRESERVED
        Type type = Type.GetType("ReflectReturnLib.PreservedPointerField, ReflectReturnLib")!;
        FieldInfo field = type.GetField("Address")!;
        Console.WriteLine("preserved pointer=" + Read(field, null));
        field.SetValue(null, (IntPtr)0x780);
        Console.WriteLine("preserved stored=" + Read(field, null));
#else
        var target = new ReflectReturnLib.PointerFieldOnly();
        FieldInfo inherited = typeof(ReflectReturnLib.PointerFieldOnly).GetField("Address")!;
        FieldInfo shared = typeof(ReflectReturnLib.PointerFieldOnly).GetField("Shared")!;
        Console.WriteLine("library inherited=" + Read(inherited, target));
        Console.WriteLine("library static=" + Read(shared, null));
        inherited.SetValue(target, (IntPtr)0x780);
        Console.WriteLine("library stored=" + Read(inherited, target));
#endif
        Console.WriteLine("pointer fields only end");
    }
}
