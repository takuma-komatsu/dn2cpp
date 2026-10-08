using System;
using HotUpdateBase;

namespace HotRenamedSlots;

class BelowRenamed : RenamedMeasure
{
}

sealed class BelowPatch : BelowRenamed
{
}

sealed class Replaced : RenamedMeasure, IRenamedMeasure
{
    public int Measure() => 47;
}

static class Program
{
    private static void Main()
    {
        RenamedSlotProbe.PinCulture();
        RenamedSlotProbe.Run("aot", new RenamedMeasure(), true);
        if (RenamedSlotProbe.BeforeBlock != 0)
            return;

        Console.WriteLine("== inherited renamed slots on patch receivers ==");
        RenamedSlotProbe.Run("patch", new BelowRenamed(), true);
        RenamedSlotProbe.Run("patch child", new BelowPatch(), true);
        RenamedSlotProbe.Run("replacement", new Replaced(), false);
        Console.WriteLine("inherited renamed slots on patch receivers end");
    }
}
