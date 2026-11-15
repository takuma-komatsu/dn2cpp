using UnrealSharp;
using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;
using UnrealSharp.Engine;
using UnrealSharp.Log;

namespace ManagedBaseline;

[USingleDelegate]
public delegate void FLifetimeSingle(int value);

[UMultiDelegate]
public delegate void FLifetimeMulti(int value);

[UInterface]
public partial interface ILifetimeProbe
{
    [UFunction(FunctionFlags.BlueprintCallable | FunctionFlags.BlueprintEvent)]
    public int ReadLifetimeValue(int value);
}

[UClass]
public partial class ULifetimeProbeComponent : UActorComponent
{
    [UProperty]
    public partial int ProbeValue { get; set; }
}

[UClass]
public partial class ALifetimeProbeActor : AActor, ILifetimeProbe
{
    [UProperty(DefaultComponent = true)]
    public partial ULifetimeProbeComponent ProbeComponent { get; set; }

    [UProperty]
    public partial TSubclassOf<AActor> ClassReference { get; set; }

    [UProperty]
    public partial TWeakObjectPtr<AActor> WeakReference { get; set; }

    [UProperty]
    public partial TSoftObjectPtr<AActor> SoftReference { get; set; }

    [UProperty]
    public partial TSoftClassPtr<AActor> SoftClassReference { get; set; }

    [UProperty]
    public partial ILifetimeProbe InterfaceReference { get; set; }

    [UProperty]
    public partial TDelegate<FLifetimeSingle> SingleSignal { get; set; }

    [UProperty]
    public partial TMulticastDelegate<FLifetimeMulti> MultiSignal { get; set; }

    private int _singleCalls;
    private int _multiCalls;
    private AActor? _victim;
    private TWeakObjectPtr<AActor> _weakVictim;

    public partial int ReadLifetimeValue(int value);
    public partial int ReadLifetimeValue_Implementation(int value) => value + 1;

    [UFunction]
    public void HandleSingle(int value) => _singleCalls += value;

    [UFunction]
    public void HandleMultiFirst(int value) => _multiCalls += value;

    [UFunction]
    public void HandleMultiSecond(int value) => _multiCalls += value * 10;

    [UFunction(FunctionFlags.BlueprintCallable)]
    public int ProbeLifetimeReferences()
    {
        int result = 0;
        ULifetimeProbeComponent component = ProbeComponent;
        component.ProbeValue = 41;
        if (ReferenceEquals(component.Owner, this) && ProbeComponent.ProbeValue == 41) result |= 1;
        ClassReference = new TSubclassOf<AActor>(typeof(ALifetimeProbeActor));
        if (ClassReference.IsValid && ClassReference.DefaultObject is ALifetimeProbeActor) result |= 2;
        WeakReference = new TWeakObjectPtr<AActor>(this);
        if (WeakReference.IsValid && ReferenceEquals(WeakReference.Object, this)) result |= 4;
        SoftReference = new TSoftObjectPtr<AActor>(this);
        if (SoftReference.IsValid && ReferenceEquals(SoftReference.Object, this)) result |= 8;
        SoftClassReference = new TSoftClassPtr<AActor>(ClassReference);
        if (SoftClassReference.IsValid && SoftClassReference.LoadSynchronous() == ClassReference) result |= 16;
        InterfaceReference = this;
        ILifetimeProbe resolved = InterfaceReference;
        if (ReferenceEquals(resolved.AsUObject(), this) && resolved.ReadLifetimeValue(41) == 42) result |= 32;

        _singleCalls = 0;
        SingleSignal += HandleSingle;
        bool singleBound = SingleSignal.Contains(HandleSingle);
        SingleSignal.InnerDelegate.Invoke(3);
        SingleSignal -= HandleSingle;
        if (singleBound && _singleCalls == 3 && !SingleSignal.IsBound) result |= 64;

        _multiCalls = 0;
        MultiSignal += HandleMultiFirst;
        MultiSignal += HandleMultiSecond;
        bool multiBound = MultiSignal.Contains(HandleMultiFirst) && MultiSignal.Contains(HandleMultiSecond);
        MultiSignal.InnerDelegate.Invoke(2);
        MultiSignal -= HandleMultiFirst;
        bool removed = !MultiSignal.Contains(HandleMultiFirst) && MultiSignal.Contains(HandleMultiSecond);
        MultiSignal.InnerDelegate.Invoke(3);
        MultiSignal.Clear();
        if (multiBound && removed && _multiCalls == 52 && !MultiSignal.IsBound) result |= 128;
        UnrealLogger.Log("Dn2CppSmoke", $"lifetime={result}");
        return result;
    }

    [UFunction(FunctionFlags.BlueprintCallable)]
    public void RememberVictim(AActor victim)
    {
        _victim = victim;
        _weakVictim = new TWeakObjectPtr<AActor>(victim);
    }

    [UFunction(FunctionFlags.BlueprintCallable)]
    public int ProbeDestroyedVictim()
    {
        int result = 0;
        if (_victim is not null && _victim.NativeObject == IntPtr.Zero) result |= 1;
        if (_victim is not null && _victim.IsDestroyed) result |= 2;
        if (!_weakVictim.IsValid && _weakVictim.Object is null) result |= 4;
        _victim = null;
        UnrealLogger.Log("Dn2CppSmoke", $"destroyed={result}");
        return result;
    }
}
