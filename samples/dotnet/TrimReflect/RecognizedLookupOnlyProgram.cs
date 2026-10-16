using System;
using System.Globalization;
using System.Reflection;
using TrimReflectLib;

namespace TrimReflectLookup;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("recognized lookup prefix");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_RECOGNIZED_LOOKUP") == "1")
            return;
        Console.WriteLine("== recognized lookup owners under trim ==");
        // No type token names LibGadget; a field signature is the lookup's only owner.
        MethodInfo half = typeof(GadgetHolder).GetField("Gadget")!.FieldType.GetMethod("Half")!;
        Console.WriteLine("field-type owner=" + half.Name + ":" + half.ReturnType.Name);
        // Only constant names reach LibKept and LibBare: the resolved types keep their
        // identity and member metadata, not their bodies.
        Type named = Type.GetType("TrimReflectLib.LibKept, TrimReflectLib", true)!;
        Console.WriteLine("named type=" + named.FullName + "/" + named.GetMethod("Echo")!.ReturnType.Name
            + "/" + named.GetField("Keeper")!.FieldType.Name);
        Type? inAssembly = typeof(Factory).Assembly.GetType("TrimReflectLib.LibBare");
        Console.WriteLine("assembly-named type=" + inAssembly!.FullName + "/" + inAssembly.BaseType!.Name);
        // The owner is the runtime type of a factory result, which no summary resolves.
        object widget = Factory.Make();
        try
        {
            Console.WriteLine("runtime owner=" + widget.GetType().GetField("Amount")!.Name);
        }
        catch (PlatformNotSupportedException e)
        {
            Console.WriteLine("runtime owner refused: " + e.Message);
        }
        Console.WriteLine("recognized lookup owners under trim end");
    }
}

internal sealed class GadgetHolder
{
    public LibGadget Gadget;
}
