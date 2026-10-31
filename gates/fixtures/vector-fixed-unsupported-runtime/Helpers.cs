using System;

namespace VectorProbe;

internal enum VectorUnsupportedEnum : int { Zero, One }
internal struct VectorUnsupportedStruct { public int Value; }
internal struct VectorUnsupportedRefStruct { public string Value; }
internal static partial class Program
{
    private static string VectorUnsupportedEffects = "";
    private static void VectorUnsupportedRecord(string name, Func<object> action)
    {
        VectorUnsupportedEffects = "";
        try { Console.WriteLine($"{name}|result={action()}|effects={VectorUnsupportedEffects}"); }
        catch (Exception e) { Console.WriteLine($"{name}|exception={e.GetType().FullName}|effects={VectorUnsupportedEffects}"); }
    }
    private static object VectorUnsupportedObjectOperand(object value, bool fail = false)
    {
        VectorUnsupportedEffects += "A";
        if (fail) throw new InvalidOperationException();
        return value;
    }
    private static bool VectorUnsupportedConstrainedEqual<T>(T left, T right) where T : IEquatable<T> => left.Equals(right);
}
