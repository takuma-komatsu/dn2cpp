using System;

// SUBJECT: a C# 11 ref field on a user-defined ref struct — ctor store, a
// ref-returning member, read and write-through. Lowered to a plain pointer
// field, not through any Span intrinsic.
namespace RefFieldSubset;

ref struct RefBox
{
    private ref int _value;
    public RefBox(ref int v) { _value = ref v; }
    public ref int Value => ref _value;
    public int Get() => _value;
    public void Set(int v) { _value = v; }
}

class Program
{
    internal static void __GateEntry()
    {
        int x = 10;
        var box = new RefBox(ref x);
        box.Set(42);
        Console.WriteLine(x);          // 42
        Console.WriteLine(box.Get());  // 42
        ref int r = ref box.Value;
        r = 99;
        Console.WriteLine(x);          // 99
    }
}
