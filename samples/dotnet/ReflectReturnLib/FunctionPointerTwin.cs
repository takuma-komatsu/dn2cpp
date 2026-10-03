namespace FunctionPointerTwin;

// ReflectFrameworkBind declares its own Leaf and Node; each assembly's
// signatures name its own.
public struct Leaf
{
    public int A;
}

public sealed class Node { }

public static unsafe class Peer
{
    public static delegate*<Leaf>* LeafCell() => (delegate*<Leaf>*)0x110;
    public static long LeafCellSink(delegate*<Leaf>* pointer) => (long)pointer;
    public static long NodeCellSink(delegate*<Node>* pointer) => (long)pointer;
    public static long ListCellSink(delegate*<System.Collections.Generic.List<Node>>* pointer) => (long)pointer;
    public static long NestedCellSink(delegate*<delegate*<Leaf>, void>* pointer) => (long)pointer;
    public static long LeafPointerSink(Leaf* pointer) => (long)pointer;
}
