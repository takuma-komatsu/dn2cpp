#nullable disable
// Two attribute-row shapes that drop SILENTLY when unhandled — the applied attributes
// simply go missing, with no diagnostic anywhere.
// (1) A named argument may bind a property (or field) declared on an attribute
//     BASE class — [RunOnKeyDown("x", Priority = 3)] sets the abstract
//     InputAttribute's Priority — so both the reach-side setter walk
//     (Compilation.ReachAttributesOf) and the emit-side member lookup
//     (CppEmitter.RenderNamedArg) must search the base chain, not just the
//     applied class's declared members.
// (2) A primitive-element array argument — RunOnAxis's float[] — must render
//     (CppEmitter.RenderAttrArray), covering both array reps: i4 (int/uint and
//     4-byte-underlying enums) and packed element-width Dn2CppArrayN
//     (float/double/…) — and must carry the precise ti_arr_ handle: the RunOnAxis
//     ctor runs LINQ Count(predicate) over its array arguments, an IEnumerable<T>
//     interface dispatch that aborts on the shared object[] header an untagged
//     allocation would leave behind.
// The discovery walk below is a real input-binding API shape:
// GetMethods() with default flags -> IsDefined(baseAttrType, true) ->
// (InputAttribute[])GetCustomAttributes(baseAttrType, true). Array contents are
// printed by direct indexing (invariant culture), so every line matches real
// .NET.
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ReflectAttrNamedBaseSubset;

public abstract class InputAttribute : Attribute
{
    public int Priority { get; set; }

    public bool OnlyUnhandled { get; set; } = true;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RunOnKeyAttribute : InputAttribute
{
    public RunOnKeyAttribute(string inputName)
    {
        InputName = inputName;
    }

    public string InputName { get; }
}

public sealed class RunOnKeyDownAttribute : RunOnKeyAttribute
{
    public RunOnKeyDownAttribute(string inputName) : base(inputName)
    {
    }
}

public enum AxisKind
{
    Linear = 0,
    Angular = 1,
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class RunOnAxisAttribute : InputAttribute
{
    public RunOnAxisAttribute(string[] inputNames, float[] associatedValues)
    {
        InputNames = inputNames;
        AssociatedValues = associatedValues;
        // The Thrive RunOnAxisAttribute ctor shape: LINQ over the array arguments
        // right inside the attribute ctor — IEnumerable<T> interface dispatch on the
        // attribute-built arrays themselves. An untagged (object[]-headed) allocation
        // has no dispatch map, so these abort loudly at attribute materialization; a
        // ti_arr_-tagged one rides the same map every body-allocated array uses.
        NameCount = inputNames.Count(n => n.StartsWith("g_", StringComparison.Ordinal));
        PositiveCount = associatedValues.Count(v => v > 0f);
    }

    public string[] InputNames { get; }

    public float[] AssociatedValues { get; }

    public int NameCount { get; }

    public int PositiveCount { get; }

    public int[] Codes { get; set; }

    public AxisKind[] Kinds { get; set; }

    public double[] Weights { get; set; }
}

// The named-FIELD counterpart of mechanism (1): Level is a public field on the
// BASE attribute class, set as a named argument on the derived one.
public abstract class TagBaseAttribute : Attribute
{
    public int Level;
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class TagAttribute : TagBaseAttribute
{
}

public class Player
{
    [RunOnKeyDown("g_build_structure")]
    public void OpenBuildMenu()
    {
    }

    [RunOnKeyDown("g_science", Priority = 2, OnlyUnhandled = false)]
    public bool ToggleResearchScreen()
    {
        return false;
    }

    [RunOnKey("g_cheat_glucose")]
    [RunOnKey("g_cheat_ammonia", Priority = 5)]
    public static void CheatGlucose()
    {
    }

    [RunOnAxis(new[] { "g_move_forward", "g_move_backward" }, new[] { -1.0f, 1.0f },
        Codes = new[] { 10, -20 }, Kinds = new[] { AxisKind.Angular, AxisKind.Linear },
        Weights = new[] { 0.5, 2.0 })]
    public void OnMovement()
    {
    }

    [Tag(Level = 4)]
    public void Tagged()
    {
    }
}

public static class Program
{
    internal static void Run()
    {
        Console.WriteLine("== named args on attribute base class + primitive arrays ==");
        var baseType = typeof(InputAttribute);
        var methods = typeof(Player).GetMethods()
            .Where(m => m.DeclaringType == typeof(Player))
            .OrderBy(m => m.Name, StringComparer.Ordinal);
        int total = 0;
        foreach (var mi in methods)
        {
            bool defined = mi.IsDefined(baseType, true);
            Console.WriteLine(mi.Name + " IsDefined=" + defined);
            if (!defined)
                continue;
            var attrs = (InputAttribute[])mi.GetCustomAttributes(baseType, true);
            foreach (var attr in attrs.OrderBy(a => Describe(a), StringComparer.Ordinal))
            {
                total++;
                Console.WriteLine("  " + Describe(attr));
                if (attr is RunOnAxisAttribute ax)
                {
                    Console.WriteLine("  names=" + string.Join(",", ax.InputNames));
                    Console.WriteLine("  values=" + JoinFloats(ax.AssociatedValues));
                    Console.WriteLine("  codes=" + JoinInts(ax.Codes));
                    Console.WriteLine("  kinds=" + JoinKinds(ax.Kinds));
                    Console.WriteLine("  weights=" + JoinDoubles(ax.Weights));
                    Console.WriteLine("  ctorLinq nameCount=" + ax.NameCount
                        + " positiveCount=" + ax.PositiveCount);
                }
            }
        }
        Console.WriteLine("total=" + total);

        var tag = (TagAttribute)typeof(Player).GetMethod("Tagged")
            .GetCustomAttributes(typeof(TagBaseAttribute), true)[0];
        Console.WriteLine("tag Level=" + tag.Level);
    }

    private static string Describe(InputAttribute a)
    {
        string extra = a is RunOnKeyAttribute k ? k.InputName : "-";
        return a.GetType().Name + "(" + extra + ",Priority=" + a.Priority
            + ",OnlyUnhandled=" + a.OnlyUnhandled + ")";
    }

    // Direct-indexing joins: array contents read by ldelem, deterministic
    // invariant-culture formatting.
    private static string JoinFloats(float[] xs)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < xs.Length; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(xs[i].ToString(CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    private static string JoinDoubles(double[] xs)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < xs.Length; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(xs[i].ToString(CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    private static string JoinInts(int[] xs)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < xs.Length; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(xs[i].ToString(CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    private static string JoinKinds(AxisKind[] xs)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < xs.Length; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(((int)xs[i]).ToString(CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }
}
