using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ILDietControlLib;

public interface IAttributeContract<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> { }
public class AttributeContractBase<T, U> : IAttributeContract<T> where U : new()
{
    public static U Create() => new U();
}
public sealed class AttributeContractTarget { }
public sealed class AttributeContractUnused { }
public sealed class AttributeContractDerived : AttributeContractBase<AttributeContractTarget, AttributeContractUnused> { }

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
public class TypeContractBase
{
    public object? ReadCode() => GetType().InvokeMember("Code",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod,
        null, this, null);

    private int Code() => 17;
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public interface ITypeContract { }

public interface IDerivedTypeContract : ITypeContract { }

public sealed class TypeContractDerived : TypeContractBase, IDerivedTypeContract
{
    private int Code() => 23;
    public int Size => 31;
}

public sealed class TypeContractTemplate<T> : TypeContractBase
{
    private static string ArgumentName() => typeof(T).Name;
}

public sealed class UnrelatedTypeContractTarget
{
    private int Code() => -1;
}

// Only ILDietLibraryOwnerOnly names these types, through reflection that selects
// members on a base type or on a type named by a constant string.
public class AppFacingBase
{
    public string BaseDescribe() => "app-facing-base:" + GetType().Name;
    public string BaseLabel => "app-facing-label";
    public string UnselectedAppFacing() => "unselected";
}

public class ReflectedOwnerBase
{
    public string InheritedDescribe() => "library-base:" + GetType().Name;
    public string UnselectedLibraryBase() => "unselected";
}

public sealed class ReflectedOwnerDerived : ReflectedOwnerBase { }

public static class NamedOnlyTarget
{
    public static string Run() => "named-only-run";
    public static string UnselectedNamed() => "unselected";
}

public static class NamedOnlyBox<T>
{
    public static string Describe() => "named-only-box:" + typeof(T).Name;
}

// Only metadata-only lookups and listings in ILDietLibraryOwnerOnly name these types;
// no site runs, binds or constructs one of their members.
public static class MetadataIdentityOnly
{
    public static string Hidden() => "metadata-identity-hidden";
}

public static class MetadataNamedTarget
{
    public static string Probe() => "metadata-probe";
    public static string Label => "metadata-label";
    public static string Unlooked() => "metadata-unlooked";
}

public static class MetadataListedTarget
{
    public static string ListedFirst() => "metadata-listed-first";
    public static int ListedSecond(int value) => value + 1;
    public static string ListedLabel => "metadata-listed-label";
}

public class MetadataBase
{
    public string BaseOnly() => "metadata-base-only";
    public string BaseUnlooked() => "metadata-base-unlooked";
}

// No application type derives from MetadataLibraryBase; a lookup on the library-derived
// type finds its member on this library base.
public class MetadataLibraryBase
{
    public string LibraryBaseOnly() => "metadata-library-base-only";
    public string LibraryBaseUnlooked() => "metadata-library-base-unlooked";
}

public sealed class MetadataLibraryDerived : MetadataLibraryBase { }

// Only ILDietLibraryOwnerOnly constructs it, through Assembly.CreateInstance over a
// constant name.
public sealed class AssemblyNamedTarget
{
    private readonly string _label;

    public AssemblyNamedTarget() => _label = "assembly-named-library";

    public override string ToString() => _label;
}

// Only ILDietInheritedStaticOnly's FlattenHierarchy field read on InheritedStaticDerived,
// named by a constant string, reaches the base's static field, whose initializer is not
// beforefieldinit.
public class InheritedStaticBase
{
    public static int Seed = Initial();

    static InheritedStaticBase()
    {
    }

    private static int Initial() => 42;
}

public sealed class InheritedStaticDerived : InheritedStaticBase { }

// Only an assembly listing through a type argument names these generic definitions,
// which a listing returns whether or not anything else reaches them.
public struct ListedAssemblyMarker { }

public sealed class AssemblyListedAlpha<T> { }

public static class AssemblyListedBeta<TKey, TValue> { }

// Only ILDietConditionalNamedOnly names this type, by a constant string. Its conditional
// descriptor keeps Run, which only a lookup by a member name no constant spells selects.
public static class ConditionalNamedTarget
{
    public static string Run() => "conditional-named-library";
}
