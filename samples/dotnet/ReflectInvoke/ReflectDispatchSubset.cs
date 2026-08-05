#nullable enable
// SUBJECT: the dispatch shapes a plugin host leans on — static method and field
// access through reflection, overload resolution by parameter types, class
// relationship queries, a virtual call on a reflectively-created instance, and
// an enum parse through a runtime Type.
using System;
using System.Reflection;

namespace ReflectDispatchSubset;

public abstract class Shape { public abstract double Area(); }
public sealed class Square : Shape { public double Side { get; set; } public override double Area() => Side * Side; }

public static class Ops
{
    public static int Calls;                       // static field
    public static int Apply(int x) { Calls++; return x + 1; }            // overload A
    public static int Apply(int x, int y) { Calls++; return x + y; }     // overload B
    public static string Apply(string s) { Calls++; return s + "!"; }    // overload C
}

public enum Mode { Off = 0, On = 1, Auto = 2 }

public static class Program
{
    internal static void Run()
    {
        Type ops = typeof(Ops);

        // static field get/set through reflection (null instance)
        FieldInfo calls = ops.GetField("Calls")!;
        calls.SetValue(null, 0);

        // resolve a specific overload by parameter types, then invoke (null target)
        MethodInfo a1 = ops.GetMethod("Apply", new Type[] { typeof(int) })!;
        MethodInfo a2 = ops.GetMethod("Apply", new Type[] { typeof(int), typeof(int) })!;
        MethodInfo a3 = ops.GetMethod("Apply", new Type[] { typeof(string) })!;
        Console.WriteLine("apply(int)=" + a1.Invoke(null, new object[] { 10 }));
        Console.WriteLine("apply(int,int)=" + a2.Invoke(null, new object[] { 3, 4 }));
        Console.WriteLine("apply(string)=" + a3.Invoke(null, new object[] { "hi" }));
        Console.WriteLine("calls=" + calls.GetValue(null));

        // missing overload -> null
        MethodInfo? a4 = ops.GetMethod("Apply", new Type[] { typeof(double) });
        Console.WriteLine("apply(double)=" + (a4 is null ? "null" : "found"));

        // class relationship queries
        Type sq = typeof(Square), sh = typeof(Shape);
        Console.WriteLine($"sq<:sh={sq.IsSubclassOf(sh)} sh<:sq={sh.IsSubclassOf(sq)} self={sh.IsSubclassOf(sh)}");
        Console.WriteLine($"assignable={sh.IsAssignableFrom(sq)}");

        // reflectively create a derived instance and invoke its virtual method
        object inst = Activator.CreateInstance(sq)!;
        sq.GetProperty("Side")!.SetValue(inst, 5.0);
        MethodInfo area = sq.GetMethod("Area")!;
        Console.WriteLine("area=" + area.Invoke(inst, null));

        // enum parse through a runtime Type
        object m = Enum.Parse(typeof(Mode), "Auto");
        Console.WriteLine("mode=" + m + "=" + (int)(Mode)m);
    }
}
