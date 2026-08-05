#nullable disable
// The System.Attribute opaque-base instance surface: GetHashCode /
// IsDefaultAttribute / TypeId called through a receiver statically typed as
// Attribute — the base-token shape System.ComponentModel.TypeDescriptor's
// attribute pipeline reaches. Attribute is an opaque intrinsic base (its real
// GetHashCode/IsDefaultAttribute walk fields by reflection), so these lower to
// intrinsics: GetHashCode to the identity-hash object lowering (the VALUE
// diverges from real .NET's field hash, so only its per-instance STABILITY is
// asserted — never the raw value), IsDefaultAttribute to the base
// implementation's constant false, and TypeId to the runtime Type off the
// object header (the base implementation's this.GetType()). Every line below
// matches real .NET for a subclass that overrides none of the three.
using System;

namespace ReflectAttrBaseSubset;

[AttributeUsage(AttributeTargets.Class)]
public sealed class MarkAttribute : Attribute
{
    public string Name;
    public MarkAttribute(string n) { Name = n; }
}

[Mark("alpha")]
public class Target { }

[Mark("beta")]
public class Target2 { }

public static class Program
{
    internal static void Run()
    {
        Console.WriteLine("== attribute base surface ==");
        Attribute a = (Attribute)typeof(Target).GetCustomAttributes(typeof(MarkAttribute), false)[0];

        // GetHashCode: callable, non-throwing, stable on the same instance.
        int h1 = a.GetHashCode();
        int h2 = a.GetHashCode();
        Console.WriteLine("hash stable=" + (h1 == h2));

        // IsDefaultAttribute: the base implementation answers false.
        Console.WriteLine("isDefault=" + a.IsDefaultAttribute());

        // TypeId: the base implementation answers this.GetType() — the one
        // runtime Type instance typeof also surfaces.
        object id = a.TypeId;
        Console.WriteLine("typeId==typeof=" + ReferenceEquals(id, typeof(MarkAttribute)));
        Console.WriteLine("typeId==GetType=" + ReferenceEquals(id, a.GetType()));

        // Match(object): the base implementation is `return Equals(obj)`. Attribute is
        // opaque (dn2cpp does not field-walk it), so this lowers to the reference-
        // equality object comparison — where real .NET's Attribute.Equals compares field
        // VALUES. The two AGREE on the same instance (true) and on a genuinely different
        // attribute (false), which is all this asserts; a distinct-but-field-equal
        // attribute would diverge (the documented opaque-base approximation, as with
        // GetHashCode above). This is the shape TypeDescriptor's attribute filter
        // (ShouldHideMember) reaches.
        Attribute other = (Attribute)typeof(Target2).GetCustomAttributes(typeof(MarkAttribute), false)[0];
        Console.WriteLine("matchSelf=" + a.Match(a));
        Console.WriteLine("matchOther=" + a.Match(other));
    }
}
