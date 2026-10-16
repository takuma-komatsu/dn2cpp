using System;

namespace HotGvmCall.Library;

// An attribute row's arguments are noted only on a class the image carries.
// LateLayoutUnusedCarrier is named by a field type but neither allocated nor called, so
// the type its row names gets no type-info; the application class's row names
// LateLayoutCarriedTag, which gets one.
[AttributeUsage(AttributeTargets.Class)]
public sealed class LateLayoutTagAttribute : Attribute
{
    public LateLayoutTagAttribute(Type tag) => Tag = tag;

    public Type Tag { get; }
}

public sealed class LateLayoutCarriedTag
{
}

public sealed class LateLayoutUnusedTag
{
}

[LateLayoutTag(typeof(LateLayoutUnusedTag))]
public sealed class LateLayoutUnusedCarrier
{
}
