using UnrealSharp;
using UnrealSharp.Attributes;
using UnrealSharp.Core;
using UnrealSharp.CoreUObject;
using UnrealSharp.Engine;
using UnrealSharp.Log;

namespace ManagedBaseline;

[UEnum]
public enum ESmokeChoice : byte
{
    First = 0,
    Second = 1,
}

[UClass]
public partial class AFeatureProbeActor : AActor
{
    [UProperty]
    public partial int FeatureScalar { get; set; }

    [UProperty]
    public partial ESmokeChoice FeatureChoice { get; set; }

    [UProperty]
    public partial bool FeatureEnabled { get; set; }

    [UProperty]
    public partial string FeatureString { get; set; }

    [UProperty]
    public partial FName FeatureName { get; set; }

    [UProperty]
    public partial FText FeatureText { get; set; }

    [UProperty]
    public partial FVector FeatureVector { get; set; }

    [UProperty]
    public partial TArray<int> FeatureArray { get; set; }

    [UProperty]
    public partial TSet<int> FeatureSet { get; set; }

    [UProperty]
    public partial TMap<string, int> FeatureMap { get; set; }

    [UProperty]
    public partial AActor? FeatureObject { get; set; }

    [UFunction(FunctionFlags.BlueprintCallable)]
    public int ProbeFeatures()
    {
        int result = 0;
        FeatureScalar = -12345;
        if (FeatureScalar == -12345) result |= 1;
        FeatureChoice = ESmokeChoice.Second;
        if (FeatureChoice == ESmokeChoice.Second) result |= 2;
        FeatureEnabled = true;
        if (FeatureEnabled) result |= 4;
        FeatureString = "日本語🌟";
        if (FeatureString == "日本語🌟") result |= 8;
        FeatureName = new FName("Probe_名");
        if (FeatureName.ToString() == "Probe_名") result |= 16;
        using (FText text = new("こんにちは🌟"))
        {
            FeatureText = text;
        }
        using (FText text = FeatureText)
        {
            if (text.ToString() == "こんにちは🌟") result |= 32;
        }
        FeatureVector = new FVector(1.25, -2.5, 8);
        FVector vector = FeatureVector;
        if (vector.X == 1.25 && vector.Y == -2.5 && vector.Z == 8) result |= 64;

        TArray<int> array = FeatureArray;
        array.Clear();
        array.Add(3);
        array.Add(6);
        array.Add(9);
        array.RemoveAt(1);
        TSet<int> set = FeatureSet;
        set.Clear();
        set.Add(7);
        set.Add(17);
        set.Add(7);
        TMap<string, int> map = FeatureMap;
        map.Clear();
        map["東京"] = 17;
        map["Paris"] = 25;
        if (FeatureArray.Count == 2 && FeatureArray[0] + FeatureArray[1] == 12 &&
            FeatureSet.Count == 2 && FeatureSet.Contains(7) && FeatureSet.Contains(17) &&
            FeatureMap.Count == 2 && FeatureMap["東京"] + FeatureMap["Paris"] == 42)
        {
            result |= 128;
        }
        FeatureObject = this;
        if (ReferenceEquals(FeatureObject, this)) result |= 256;
        FeatureObject = null;
        UnrealLogger.Log("Dn2CppSmoke", $"features={result}");
        return result;
    }

    [UFunction(FunctionFlags.BlueprintCallable)]
    public FVector TransformFeatureValues(FVector value, ref int count, out bool enabled)
    {
        count += 2;
        enabled = count == 42;
        FVector result = new(value.X * 2, value.Y * 2, value.Z * 2);
        UnrealLogger.Log("Dn2CppSmoke", $"ref-out={count}:{enabled}");
        return result;
    }
}
