namespace Dn2Cpp;

internal sealed record ArraySearchInstanceFieldStore(MethodInfo Method, FieldInfo Field,
    ArraySearchOrigin Receiver, ArraySearchOrigin Value, int Offset, bool StraightLine);
internal sealed record ArraySearchStaticFieldStore(MethodInfo Method, FieldInfo Field,
    ArraySearchOrigin Value, int Offset, bool StraightLine);
internal sealed record ArraySearchReflectedFieldStore(MethodInfo Method, ArraySearchOrigin Handle,
    ArraySearchOrigin? Receiver, ArraySearchOrigin Value, int Offset, bool StraightLine);

internal sealed partial class Compilation
{
    private readonly Dictionary<MethodInfo, List<ArraySearchInstanceFieldStore>> _arraySearchInstanceFieldStores = new();
    private readonly Dictionary<MethodInfo, List<ArraySearchStaticFieldStore>> _arraySearchStaticFieldStores = new();
    private readonly Dictionary<MethodInfo, List<ArraySearchReflectedFieldStore>> _arraySearchReflectedFieldStores = new();

    internal ArraySearchOrigin ArraySearchFieldReadOrigin(FieldInfo field, ArraySearchOrigin? receiver,
        MethodInfo owner, int offset, bool straightLine)
    {
        var origin = NewArraySearchOrigin();
        if (!TrackArraySearchOrigins)
            return origin;
        if (receiver is null)
            origin.Unknown = true;
        else
            origin.FieldRead = (field, receiver, owner, offset, straightLine);
        return origin;
    }

    internal void NoteArraySearchFieldStore(MethodInfo method, FieldInfo field,
        ArraySearchOrigin? receiver, ArraySearchOrigin? value, int offset, bool straightLine)
    {
        if (TrackArraySearchOrigins && receiver is not null && value is not null)
        {
            if (!_arraySearchInstanceFieldStores.TryGetValue(method, out var stores))
                _arraySearchInstanceFieldStores.Add(method, stores = new List<ArraySearchInstanceFieldStore>());
            stores.Add(new(method, field, receiver, value, offset, straightLine));
            MarkArraySearchDirty();
        }
    }

    internal ArraySearchOrigin ArraySearchStaticFieldReadOrigin(FieldInfo field,
        MethodInfo owner, int offset, bool straightLine)
    {
        var origin = NewArraySearchOrigin();
        if (TrackArraySearchOrigins)
            origin.StaticFieldRead = (field, owner, offset, straightLine);
        return origin;
    }

    internal void NoteArraySearchStaticFieldStore(MethodInfo method, FieldInfo field,
        ArraySearchOrigin? value, int offset, bool straightLine)
    {
        if (TrackArraySearchOrigins && value is not null)
        {
            if (!_arraySearchStaticFieldStores.TryGetValue(method, out var stores))
                _arraySearchStaticFieldStores.Add(method, stores = new List<ArraySearchStaticFieldStore>());
            stores.Add(new(method, field, value, offset, straightLine));
            MarkArraySearchDirty();
        }
    }

    internal void NoteArraySearchReflectedFieldStore(MethodInfo method, ArraySearchOrigin? handle,
        ArraySearchOrigin? receiver, ArraySearchOrigin? value, int offset, bool straightLine)
    {
        if (TrackArraySearchOrigins && handle is not null && value is not null)
        {
            if (!_arraySearchReflectedFieldStores.TryGetValue(method, out var stores))
                _arraySearchReflectedFieldStores.Add(method,
                    stores = new List<ArraySearchReflectedFieldStore>());
            stores.Add(new(method, handle, receiver, value, offset, straightLine));
            MarkArraySearchDirty();
        }
    }
}
