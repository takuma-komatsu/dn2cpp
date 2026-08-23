using System;
using System.Collections.Generic;

namespace RuntimeTypeHandleKeySubset;

struct HandleAndTag
{
    public RuntimeTypeHandle Handle;
    public int Tag;

    public HandleAndTag(RuntimeTypeHandle handle, int tag)
    {
        Handle = handle;
        Tag = tag;
    }
}

internal static class Program
{
    internal static void __GateEntry()
    {
        RuntimeTypeHandle text = typeof(string).TypeHandle;
        RuntimeTypeHandle textAgain = typeof(string).TypeHandle;
        RuntimeTypeHandle number = typeof(int).TypeHandle;
        var comparer = EqualityComparer<RuntimeTypeHandle>.Default;
        Console.WriteLine("handle-eq=" + comparer.Equals(text, textAgain)
            + ":" + comparer.Equals(text, number)
            + " object=" + text.Equals((object)textAgain)
            + " hash=" + (comparer.GetHashCode(text) == comparer.GetHashCode(textAgain))
            + ":" + (text.GetHashCode() == textAgain.GetHashCode()));
        object boxedText = text;
        object boxedTextAgain = textAgain;
        Console.WriteLine("handle-box=" + boxedText.Equals(boxedTextAgain)
            + " hash=" + (boxedText.GetHashCode() == boxedTextAgain.GetHashCode()));

        var byHandle = new Dictionary<RuntimeTypeHandle, KeyValuePair<int, int>>
        {
            [text] = new KeyValuePair<int, int>(2, 3),
            [number] = new KeyValuePair<int, int>(5, 7),
        };
        Console.WriteLine("handle-dict=" + byHandle[text].Key + ":" + byHandle[number].Value
            + " miss=" + byHandle.ContainsKey(typeof(long).TypeHandle));

        var a = new HandleAndTag(text, 9);
        var b = new HandleAndTag(textAgain, 9);
        var c = new HandleAndTag(number, 9);
        Console.WriteLine("handle-field=" + a.Equals(b) + ":" + a.Equals(c)
            + " hash=" + (a.GetHashCode() == b.GetHashCode()));
    }
}
