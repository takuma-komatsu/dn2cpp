using System;
using Dn2Cpp.Scripting;

namespace PreserveControlLib;

public static class Live
{
    public static string Value() => "preserve control";
}

public class PreserveAttribute : Attribute
{
}

public sealed class DerivedPreserveAttribute : PreserveAttribute
{
}

public sealed class AttributeMembers
{
    [Dn2Cpp.Scripting.Preserve]
    private AttributeMembers(int ignored) => Console.WriteLine("attribute-constructor");

    [Dn2Cpp.Scripting.Preserve]
    private static void BuiltInMethod() => Console.WriteLine("built-in");

    [PreserveControlLib.Preserve]
    private static void SameNameMethod() => Console.WriteLine("same-name");

    [DerivedPreserve]
    private static void DerivedMethod() => Console.WriteLine("derived");

    [Preserve]
    private static int PreservedField;

    [Preserve]
    private static int PreservedProperty
    {
        get { Console.WriteLine("property-get"); return PreservedField; }
        set { Console.WriteLine("property-set"); PreservedField = value; }
    }

    [Preserve]
    private static event Action PreservedEvent
    {
        add { Console.WriteLine("event-add"); }
        remove { Console.WriteLine("event-remove"); }
    }

    private static void DroppedMethod() => Console.WriteLine("drop");
}

[Dn2Cpp.Scripting.Preserve]
public sealed class TypeTarget
{
    public TypeTarget()
    {
        Console.WriteLine("type-ctor");
    }

    private void NotKeptByTypeAttribute() => Console.WriteLine("type-method-drop");
}

[Dn2Cpp.Scripting.Preserve]
public delegate void PreservedDelegate(int value);

public sealed class XmlMembers
{
    private static void XmlMethod() => Console.WriteLine("xml-method");

    private static void XmlOverload() => SignatureNoArgMarker();
    private static void XmlOverload(int value) => SignatureIntMarker();
    private static void SignatureNoArgMarker() => Console.WriteLine("signature-no-arg");
    private static void SignatureIntMarker() => Console.WriteLine("signature-int");

    private int XmlProperty
    {
        get { Console.WriteLine("xml-property-get"); return 0; }
        set { Console.WriteLine("xml-property-set"); }
    }

    private event Action XmlEvent
    {
        add { Console.WriteLine("xml-event-add"); }
        remove { Console.WriteLine("xml-event-remove"); }
    }
}

public sealed class ConditionalUsed
{
    private static void ConditionalUsedMarker() => ConditionalChainLeaf();
    private static void ConditionalChainLeaf() => Console.WriteLine("conditional-used-chain");
}

public sealed class ConditionalUnused
{
    private static void ConditionalUnusedMarker() => ConditionalUnusedChainLeaf();
    private static void ConditionalUnusedChainLeaf() => Console.WriteLine("conditional-unused-chain");
}

public sealed class WildcardMembers
{
    private static void WildcardMethod() => SelectedChainLeaf();
    private static void SelectedChainLeaf() => Console.WriteLine("wildcard-chain");
    private static void NotSelected() => Console.WriteLine("wildcard-drop");
}

public sealed class FieldsModePayload
{
}

public sealed class FieldsMode
{
    private FieldsModePayload Payload;
    private static void FieldsModeMethodDrop() => Console.WriteLine("fields-mode-drop");
}

public sealed class FieldSignatureType
{
}

public sealed class SignatureMembers
{
    private FieldSignatureType SignatureField;

    private int SignatureProperty
    {
        get { SignaturePropertyGetMarker(); return 0; }
        set { SignaturePropertySetMarker(); }
    }

    private event Action SignatureEvent
    {
        add { SignatureEventAddMarker(); }
        remove { SignatureEventRemoveMarker(); }
    }

    private static void SignaturePropertyGetMarker() => Console.WriteLine("signature-property-get");
    private static void SignaturePropertySetMarker() => Console.WriteLine("signature-property-set");
    private static void SignatureEventAddMarker() => Console.WriteLine("signature-event-add");
    private static void SignatureEventRemoveMarker() => Console.WriteLine("signature-event-remove");
}

public sealed class FeatureMembers
{
    private static void ComMethod() => Console.WriteLine("feature-com");
    private static void SreMethod() => Console.WriteLine("feature-sre");
}

public static class Outer
{
    public sealed class Nested<T>
    {
        private static void GenericMethod() => Console.WriteLine("generic-method");
    }
}
