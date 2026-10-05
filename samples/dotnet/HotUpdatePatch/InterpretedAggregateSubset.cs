using System;
using HotUpdateBase;

namespace HotUpdatePatch;

public static class InterpretedAggregateSubset
{
    public static string Run()
    {
        Console.WriteLine("== interpreted derived aggregates ==");
        var first = new Exception("first");
        var second = new Exception("second");
        var array = new[] { first, second };
        var imported = new AggregateProbe("imported", array);
        var belowAot = new PatchAggregateProbe("below AOT", array);
        var direct = new DirectPatchAggregate("direct", array);
        var further = new FurtherPatchAggregate("further", array);
        array[0] = new Exception("replacement");
        Console.WriteLine("patch imported=" + DerivedAggregateSubset.Describe(imported, first)
            + "/" + DerivedAggregateSubset.Fields(imported.Code, imported.Tag));
        Console.WriteLine("patch below AOT=" + DerivedAggregateSubset.Describe(belowAot, first)
            + "/" + DerivedAggregateSubset.Fields(belowAot.Code, belowAot.Tag) + "/" + belowAot.Origin);
        Console.WriteLine("patch direct=" + DerivedAggregateSubset.Describe(direct, first)
            + "/" + DerivedAggregateSubset.Fields(direct.Code, direct.Tag));
        Console.WriteLine("patch direct base=" + direct.BaseMessage());
        direct.Code = 43;
        direct.Tag = "after";
        Console.WriteLine("patch rewritten=" + DerivedAggregateSubset.Describe(direct, first)
            + "/" + DerivedAggregateSubset.Fields(direct.Code, direct.Tag));
        Console.WriteLine("patch further=" + DerivedAggregateSubset.Describe(further, first)
            + "/" + DerivedAggregateSubset.Fields(further.Code, further.Tag)
            + "/" + DerivedAggregateSubset.Fields(further.Level, further.Origin));
        var empty = new DirectPatchAggregate();
        Console.WriteLine("patch empty=" + DerivedAggregateSubset.Describe(empty, null)
            + "/" + DerivedAggregateSubset.Fields(empty.Code, empty.Tag));
        var message = new DirectPatchAggregate((string?)null);
        Console.WriteLine("patch null message=" + DerivedAggregateSubset.Describe(message, null)
            + "/" + DerivedAggregateSubset.Fields(message.Code, message.Tag));
        var single = new DirectPatchAggregate("single", first);
        Console.WriteLine("patch single=" + DerivedAggregateSubset.Describe(single, first)
            + "/" + DerivedAggregateSubset.Fields(single.Code, single.Tag));
        return "interpreted derived aggregates end";
    }
}

internal sealed class PatchAggregateProbe : AggregateProbe
{
    internal string Origin;
    internal PatchAggregateProbe(string message, Exception[] inner) : base(message, inner)
    {
        Code = 31;
        Tag = "derived";
        Origin = "patch";
    }
}

internal class DirectPatchAggregate : AggregateException
{
    internal int Code;
    internal string Tag;
    internal DirectPatchAggregate() { Code = 17; Tag = "empty"; }
    internal DirectPatchAggregate(string? message) : base(message) { Code = 19; Tag = "message"; }
    internal DirectPatchAggregate(string message, Exception[] inner) : base(message, inner) { Code = 23; Tag = "array"; }
    internal DirectPatchAggregate(string message, Exception inner) : base(message, inner) { Code = 29; Tag = "single"; }
    internal string BaseMessage() => base.Message;
}

internal sealed class FurtherPatchAggregate : DirectPatchAggregate
{
    internal int Level;
    internal string Origin;
    internal FurtherPatchAggregate(string message, Exception[] inner) : base(message, inner)
    {
        Code = 47;
        Tag = "further";
        Level = 9;
        Origin = "patch";
    }
}
