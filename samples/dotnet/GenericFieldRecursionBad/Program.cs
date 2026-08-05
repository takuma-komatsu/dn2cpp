using System;
using System.Collections.Generic;

namespace GenericFieldRecursionBad;

/// <summary>The third AOT-hostile monomorphization shape, and the one that keeps the
/// depth bound load-bearing now that the other signature-driven shape no longer needs it:
/// a generic type one of whose FIELDS is typed at a deeper instantiation of its own
/// declaring type.
///
/// <para>Nothing calls anything here — <c>Main</c> only allocates a <c>Node&lt;int&gt;</c> —
/// but a field is not a method. An AOT compiler must know a type's layout the moment the
/// type is named, so it decodes every field's signature whether or not any code touches
/// the field. Decoding this one instantiates <c>Node&lt;List&lt;int&gt;&gt;</c>, whose own
/// <c>Next</c> instantiates <c>Node&lt;List&lt;List&lt;int&gt;&gt;&gt;</c>, without end.</para>
///
/// <para>This is what separates it from <see cref="GenericSignatureRecursionBad"/>, which
/// used to recurse the same way and no longer does. There, the deepening name sat in a
/// METHOD signature, and methods are decoded only when something reaches them — nothing
/// reaches <c>Deeper</c>, so the chain now stops after one step and the transpile
/// completes. A field has no such out: it is part of what the type IS, not of what it can
/// do. So this input must still FAIL, on the monomorphization bound, and the diagnostic
/// must still name the member whose signature drove it — because, as there, nothing in the
/// reach chain can. The program is never built or run.</para></summary>
public sealed class Node<T>
{
    public T? Value;
    public Node<List<T>>? Next;
}

internal static class Program
{
    private static void Main()
    {
        var n = new Node<int> { Value = 1 };
        Console.WriteLine(n.Value);
    }
}
