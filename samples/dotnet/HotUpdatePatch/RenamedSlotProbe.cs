using System;
using HotUpdateBase;

namespace HotRenamedSlots;

class BelowRenamed : RenamedMeasure
{
}

sealed class BelowPatch : BelowRenamed
{
}

class Replaced : RenamedMeasure, IRenamedMeasure, IPatchScore
{
    public int Measure() => 47;

    int IPatchScore.Score(int amount) => amount + 47;
}

sealed class BelowReplacement : Replaced
{
}

sealed class ExplicitReplacement : RenamedMeasure, IRenamedMeasure, IPatchScore
{
    int IRenamedMeasure.Measure() => 61;

    public int Score(int argument) => argument + 61;
}

class VirtualReplacement : RenamedMeasure, IPatchVirtualMeasure
{
    public override int Weigh() => 73;
}

sealed class VirtualChild : VirtualReplacement, IPatchVirtualMeasure
{
    public override int Weigh() => 79;
}

sealed class VirtualSplit : VirtualReplacement, IPatchVirtualMeasure
{
    public override int Weigh() => 83;

    int IPatchVirtualMeasure.Weigh() => 89;
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
        Console.WriteLine("== patch interface method identities ==");
        RenamedSlotProbe.Run("replacement identity", new Replaced(), true);
        RenamedSlotProbe.RunPatchIdentity("replacement", new Replaced());
        RenamedSlotProbe.RunPatchIdentity("replacement child", new BelowReplacement());
        RenamedSlotProbe.RunPatchIdentity("explicit replacement", new ExplicitReplacement());
        RenamedSlotProbe.RunVirtualPatchIdentity("replacement", new VirtualReplacement());
        RenamedSlotProbe.RunVirtualPatchIdentity("replacement child", new VirtualChild());
        RenamedSlotProbe.RunVirtualPatchOverride(new VirtualReplacement(), new VirtualChild());
        RenamedSlotProbe.RunVirtualSplitIdentity(new VirtualReplacement(), new VirtualSplit());
        RenamedSlotProbe.RunCrossRouteEquality();
        Console.WriteLine("patch interface method identities end");
    }
}
