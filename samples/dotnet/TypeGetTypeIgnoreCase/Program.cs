using System;
using System.Globalization;

namespace TypeCaseCollision;

internal sealed class Alpha { internal sealed class Other { } }
internal sealed class aLpha { internal sealed class InnerOnly { } }
internal sealed class Beta { }
internal sealed class bETA { }
internal sealed class Gamma { }
internal sealed class gAMMA { }
internal sealed class Box<T> { }
internal sealed class bOX<T> { }

public static class CollisionTypes
{
    public static Type TargetCollision => typeof(gROWTH);
}

internal static class Program
{
    private static string Show(Type type) => type?.FullName ?? "<null>";

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Console.WriteLine("== Type.GetType case collisions ==");
        Console.WriteLine("folded=" + Show(Type.GetType("typecasecollision.alpha", false, true)));
        Console.WriteLine("collision=" + Show(Type.GetType("TypeCaseCollision.aLpha", false, true)));
        Console.WriteLine("exact=" + Show(Type.GetType("TypeCaseCollision.aLpha", false, false)));
        Console.WriteLine("later bucket=" + Show(Type.GetType("typecasecollision.beta", false, true)));
        Console.WriteLine("earlier bucket=" + Show(Type.GetType("typecasecollision.gamma", false, true)));
        Console.WriteLine("grown bucket=" + Show(Type.GetType("typecasecollision.growth", false, true)));
        Console.WriteLine("grown exact=" + Show(Type.GetType("TypeCaseCollision.gROWTH", false, false)));
        Console.WriteLine("nested loser=" + Show(Type.GetType("typecasecollision.alpha+inneronly", false, true)));
        Console.WriteLine("nested exact=" + Show(Type.GetType("TypeCaseCollision.aLpha+InnerOnly", false, false)));
        Console.WriteLine("generic folded=" + Show(Type.GetType("typecasecollision.box`1", false, true)));
        Console.WriteLine("generic exact=" + Show(Type.GetType("TypeCaseCollision.bOX`1", false, false)));
        Console.WriteLine("generic closed=" + Show(Type.GetType("typecasecollision.box`1[[System.Int32, System.Private.CoreLib]]", false, true)));
        Console.WriteLine("generic controls=" + Show(typeof(Box<int>)) + "/" + Show(typeof(bOX<int>)));
        Console.WriteLine("array folded=" + Show(Type.GetType("typecasecollision.growth[]", false, true)));
        Console.WriteLine("array controls=" + Show(typeof(Growth[])) + "/" + Show(typeof(gROWTH[])));
        Console.WriteLine("one=" + Show(Type.GetType("TypeCaseCollision.Alpha")));
        Console.WriteLine("two=" + Show(Type.GetType("TypeCaseCollision.Alpha", true)));
        Console.WriteLine("caller first=" + Show(Type.GetType("System.Int32", false, true)));
        Console.WriteLine("corelib qualified=" + Show(Type.GetType("system.int32, System.Private.CoreLib", false, true)));
        Console.WriteLine("netstandard list exact=" + Show(Type.GetType("System.Collections.Generic.List`1, netstandard", false, false)));
        Console.WriteLine("netstandard list folded=" + Show(Type.GetType("system.collections.generic.list`1, netstandard", false, true)));
        Console.WriteLine("list control=" + Show(typeof(System.Collections.Generic.List<>)));
        Console.WriteLine("facade nested exact=" + Show(Type.GetType("System.TimeProvider+SystemTimeProviderTimer, System.Runtime", false, false)));
        Console.WriteLine("facade nested folded=" + Show(Type.GetType("system.timeprovider+systemtimeprovidertimer, System.Runtime", false, true)));
        Console.WriteLine("app qualified=" + Show(Type.GetType("system.int32, TypeGetTypeIgnoreCase", false, true)));
        Console.WriteLine("miss false=" + Show(Type.GetType("TypeCaseCollision.Missing", false, true)));
        try
        {
            Type.GetType("TypeCaseCollision.Missing", true, true);
            Console.WriteLine("miss true=<no throw>");
        }
        catch (TypeLoadException)
        {
            Console.WriteLine("miss true=TypeLoadException");
        }
        Console.WriteLine("Type.GetType case collisions end");
    }
}

