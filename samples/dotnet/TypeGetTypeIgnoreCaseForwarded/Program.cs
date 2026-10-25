using System;
using System.Globalization;

namespace TypeGetTypeIgnoreCaseForwarded;

internal static class Program
{
    private static string Show(Type type) => type?.FullName ?? "<null>";

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Console.WriteLine("== Type.GetType staged forwarding ==");
        Console.WriteLine("defined exact=" + Show(typeof(TypeCaseCollision.Growth)));
        Type sourceCollision = TypeGetTypeIgnoreCaseForwarder.SourceTypes.LocalCollision;
        Type targetCollision = TypeCaseCollision.CollisionTypes.TargetCollision;
        Console.WriteLine("source collision=" + Show(sourceCollision));
        Console.WriteLine("target collision=" + Show(targetCollision));
        Console.WriteLine("forwarded exact=" + Show(Type.GetType("TypeCaseCollision.Growth, TypeGetTypeIgnoreCaseForwarder", false, false)));
        Type folded = Type.GetType("typecasecollision.growth, TypeGetTypeIgnoreCaseForwarder", false, true);
        Console.WriteLine("forwarded folded=" + Show(folded));
        Console.WriteLine("forwarded folded target=" + (folded == targetCollision));
        Console.WriteLine("forwarded folded source=" + (folded == sourceCollision));
        Console.WriteLine("source exact=" + Show(Type.GetType("TypeCaseCollision.gROWTH, TypeGetTypeIgnoreCaseForwarder", false, false)));
        Type sourceLeaf = TypeGetTypeIgnoreCaseForwarder.SourceTypes.LocalLeaf;
        Type targetLeaf = TypeGetTypeIgnoreCase.ForwardedNestedTypes.TargetLeaf;
        string sourceLeafName = sourceLeaf.FullName + ", TypeGetTypeIgnoreCaseForwarder";
        string targetLeafName = targetLeaf.FullName + ", TypeGetTypeIgnoreCaseForwarder";
        Type foldedLeaf = Type.GetType(sourceLeafName.ToLowerInvariant(), false, true);
        Console.WriteLine("nested local exact=" + (Type.GetType(sourceLeafName, false, false) == sourceLeaf));
        Console.WriteLine("nested forwarded exact=" + (Type.GetType(targetLeafName, false, false) == targetLeaf));
        Console.WriteLine("nested folded local=" + (foldedLeaf == sourceLeaf));
        Console.WriteLine("nested folded forwarded=" + (foldedLeaf == targetLeaf));
        Console.WriteLine("Type.GetType staged forwarding end");
    }
}
