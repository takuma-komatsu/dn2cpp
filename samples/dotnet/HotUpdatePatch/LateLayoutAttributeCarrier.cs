using HotGvmCall.Library;

namespace HotGvmCall;

// An application class is always carried; its field names the library carrier without
// reaching or allocating it.
[LateLayoutTag(typeof(LateLayoutCarriedTag))]
internal sealed class LateLayoutTaggedByApp
{
#pragma warning disable CS0649 // never assigned: the field type is the subject
    internal LateLayoutUnusedCarrier? Unused;
#pragma warning restore CS0649
}
