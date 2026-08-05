#nullable enable
// A shared canonical body dispatching through a CONCRETE generic interface
// instantiation reached via a concrete token — the case the shared-body
// backstop's ti_ arm must NOT flag.
//
// Box<string> and Box<Label> both canonicalize to Box<$CnRef>, and SumOver
// enumerates an IEnumerable<Item> whose element type does not involve T, so the
// canonical body names placeholder-free interface handles. Item is a reference
// type, so those interfaces are in a sharing group of their own; naming them is
// still sound, because a concrete-token-sourced handle resolves identically for
// every group member and the alias rows guarantee the row exists on every real
// receiver. DN2CPP_SHARED_ASSERT=1 (which this gate sets) arms that exempted
// path in Release, so a regression re-forbidding it needs no Debug suite run.
//
// Real System.Private.CoreLib + System.Collections (-r), run vs .NET.
using System;
using System.Collections.Generic;
namespace RefEnumDispatchSubset;

sealed class Item
{
    public int N;
    public Item(int n) { N = n; }
    public override string ToString() => "Item(" + N + ")";
}

sealed class Label
{
    public string Text;
    public Label(string text) { Text = text; }
    public override string ToString() => "Label:" + Text;
}

// The dispatch that matters never touches T's identity: it enumerates the
// concrete IEnumerable<Item>. _tag keeps T load-bearing in the layout.
sealed class Box<T> where T : class
{
    private readonly T _tag;
    public Box(T tag) { _tag = tag; }

    public int SumOver(IEnumerable<Item> items)
    {
        int sum = 0;
        foreach (Item it in items)          // callvirt IEnumerable<Item>.GetEnumerator, ...
            sum += it.N;
        return sum + _tag.ToString()!.Length;
    }
}

class Program
{
    internal static void __GateEntry()
    {
        var items = new List<Item> { new Item(1), new Item(2), new Item(4) };

        // Two reference-type instantiations, so both bind Box<$CnRef>'s body.
        var bs = new Box<string>("hello");
        var bl = new Box<Label>(new Label("w"));
        Console.WriteLine("box str sum=" + bs.SumOver(items));
        Console.WriteLine("box lbl sum=" + bl.SumOver(items));

        // A second concrete receiver type: an array boxed to IEnumerable<Item>.
        Item[] arr = { new Item(10), new Item(20) };
        Console.WriteLine("box str arr=" + bs.SumOver(arr));
    }
}
