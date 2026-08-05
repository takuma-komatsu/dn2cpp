using System;

namespace GenericArrayFieldRecursionBad;

/// <summary>The array-deepening twin of <see cref="GenericFieldRecursionBad"/>: a generic type
/// one of whose FIELDS is typed at the same generic over an <em>array</em> of its own argument,
/// <c>Box&lt;T&gt;.Next : Box&lt;T[]&gt;</c>.
///
/// <para>Nothing calls anything — <c>Main</c> only allocates a <c>Box&lt;int&gt;</c> — but a
/// field is part of what the type IS, so an AOT compiler must decode its type the moment the
/// declaring type is laid out. Decoding <c>Next</c> instantiates <c>Box&lt;int[]&gt;</c>, whose
/// own <c>Next</c> instantiates <c>Box&lt;int[][]&gt;</c>, without end.</para>
///
/// <para>The deepening is carried by an array wrapper rather than a generic one, so this input
/// is what pins that an array level counts toward monomorphization DEPTH: treated as
/// transparent, every <c>Box</c> here measures depth 1, the depth bound never fires and the
/// runaway burns the instantiation <em>count</em> cap instead — slowly, and with a diagnostic
/// that blames the reference closure. It must fail promptly on the depth bound, naming the
/// field whose signature drove it. The program is never built or run.</para></summary>
public sealed class Box<T>
{
    public T? Value;
    public Box<T[]>? Next;
}

internal static class Program
{
    private static void Main()
    {
        var b = new Box<int> { Value = 1 };
        Console.WriteLine(b.Value);
    }
}
