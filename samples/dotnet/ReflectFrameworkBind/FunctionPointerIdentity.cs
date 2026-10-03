// Each twin name binds to this assembly's type; ReflectReturnLib declares the
// same full names.
#pragma warning disable CS0436

using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReflectFrameworkBind;

// Pointer boxes cross between this assembly's methods and ReflectReturnLib's,
// whose signatures each name their own assembly's Leaf and Node.
static unsafe class FunctionPointerIdentity
{
    public static delegate*<FunctionPointerTwin.Leaf>* LeafCell() => (delegate*<FunctionPointerTwin.Leaf>*)0x10;
    public static delegate*<FunctionPointerTwin.Node>* NodeCell() => (delegate*<FunctionPointerTwin.Node>*)0x18;
    public static delegate*<List<FunctionPointerTwin.Node>>* ListCell() =>
        (delegate*<List<FunctionPointerTwin.Node>>*)0x20;
    public static delegate*<delegate*<FunctionPointerTwin.Leaf>, void>* NestedCell() =>
        (delegate*<delegate*<FunctionPointerTwin.Leaf>, void>*)0x28;
    public static FunctionPointerTwin.Leaf* LeafPointer() => (FunctionPointerTwin.Leaf*)0x30;
    public static long LeafCellSink(delegate*<FunctionPointerTwin.Leaf>* pointer) => (long)pointer;
    public static long NodeCellSink(delegate*<FunctionPointerTwin.Node>* pointer) => (long)pointer;
    public static long ListCellSink(delegate*<List<FunctionPointerTwin.Node>>* pointer) => (long)pointer;
    public static long NestedCellSink(delegate*<delegate*<FunctionPointerTwin.Leaf>, void>* pointer) => (long)pointer;
    public static long LeafPointerSink(FunctionPointerTwin.Leaf* pointer) => (long)pointer;

    private static void Pass(string label, MethodInfo? source, MethodInfo? sink)
    {
        try
        {
            object? box = source!.Invoke(null, null);
            Console.WriteLine(label + ": " + sink!.Invoke(null, new[] { box }));
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + ": " + ex.GetType().Name + " 0x" + ex.HResult.ToString("X8") + " " + ex.Message);
        }
    }

    internal static void Run()
    {
        Console.WriteLine("== function pointer identity ==");
        // A library method keeps its reflection row only once something calls it.
        FunctionPointerTwin.Peer.LeafCell();
        FunctionPointerTwin.Peer.LeafCellSink(null);
        FunctionPointerTwin.Peer.NodeCellSink(null);
        FunctionPointerTwin.Peer.ListCellSink(null);
        FunctionPointerTwin.Peer.NestedCellSink(null);
        FunctionPointerTwin.Peer.LeafPointerSink(null);
        Pass("local leaf cell, local sink", typeof(FunctionPointerIdentity).GetMethod("LeafCell"),
            typeof(FunctionPointerIdentity).GetMethod("LeafCellSink"));
        Pass("peer leaf cell, peer sink", typeof(FunctionPointerTwin.Peer).GetMethod("LeafCell"),
            typeof(FunctionPointerTwin.Peer).GetMethod("LeafCellSink"));
        Pass("local leaf cell, peer sink", typeof(FunctionPointerIdentity).GetMethod("LeafCell"),
            typeof(FunctionPointerTwin.Peer).GetMethod("LeafCellSink"));
        Pass("peer leaf cell, local sink", typeof(FunctionPointerTwin.Peer).GetMethod("LeafCell"),
            typeof(FunctionPointerIdentity).GetMethod("LeafCellSink"));
        Pass("local node cell, local sink", typeof(FunctionPointerIdentity).GetMethod("NodeCell"),
            typeof(FunctionPointerIdentity).GetMethod("NodeCellSink"));
        Pass("local node cell, peer sink", typeof(FunctionPointerIdentity).GetMethod("NodeCell"),
            typeof(FunctionPointerTwin.Peer).GetMethod("NodeCellSink"));
        Pass("local list cell, local sink", typeof(FunctionPointerIdentity).GetMethod("ListCell"),
            typeof(FunctionPointerIdentity).GetMethod("ListCellSink"));
        Pass("local list cell, peer sink", typeof(FunctionPointerIdentity).GetMethod("ListCell"),
            typeof(FunctionPointerTwin.Peer).GetMethod("ListCellSink"));
        Pass("local nested cell, local sink", typeof(FunctionPointerIdentity).GetMethod("NestedCell"),
            typeof(FunctionPointerIdentity).GetMethod("NestedCellSink"));
        Pass("local nested cell, peer sink", typeof(FunctionPointerIdentity).GetMethod("NestedCell"),
            typeof(FunctionPointerTwin.Peer).GetMethod("NestedCellSink"));
        Pass("local leaf pointer, local sink", typeof(FunctionPointerIdentity).GetMethod("LeafPointer"),
            typeof(FunctionPointerIdentity).GetMethod("LeafPointerSink"));
        Pass("local leaf pointer, peer sink", typeof(FunctionPointerIdentity).GetMethod("LeafPointer"),
            typeof(FunctionPointerTwin.Peer).GetMethod("LeafPointerSink"));
        Console.WriteLine("function pointer identity end");
    }
}
