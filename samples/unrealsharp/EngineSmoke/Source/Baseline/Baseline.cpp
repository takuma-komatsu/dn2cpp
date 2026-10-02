#include "Modules/ModuleManager.h"
#include "SmokePIE.h"
#include "SmokeLateCallback.h"
#include "Containers/Ticker.h"
#include "Engine/World.h"
#include "GameFramework/Actor.h"
#include "HAL/FileManager.h"
#include "Misc/CommandLine.h"
#include "Misc/CoreDelegates.h"
#include "Misc/FileHelper.h"
#include "Misc/Parse.h"
#include "Misc/Paths.h"
#include "UObject/UObjectIterator.h"
#include "UObject/StructOnScope.h"
#include "UObject/Script.h"
#include "UObject/GarbageCollection.h"
#include "UObject/UnrealType.h"
#if PLATFORM_MAC
#include <mach-o/dyld.h>
#endif
#if WITH_EDITOR
#include "Engine/Blueprint.h"
#include "Engine/BlueprintGeneratedClass.h"
#include "EdGraphSchema_K2.h"
#include "K2Node_CallFunction.h"
#include "K2Node_CallParentFunction.h"
#include "K2Node_Event.h"
#include "K2Node_CustomEvent.h"
#include "Kismet2/BlueprintEditorUtils.h"
#include "Kismet2/KismetEditorUtilities.h"
#include "Misc/PackageName.h"
#include "UObject/SavePackage.h"
#endif

#if WITH_EDITOR
static UClass* CreateSmokeBlueprint(UClass* Parent)
{
    UFunction* ManagedAdd = Parent->FindFunctionByName(TEXT("Add"));
    if (!ManagedAdd)
    {
        UE_LOG(LogTemp, Error, TEXT("Dn2CppSmoke managed Add function missing"));
        return nullptr;
    }
    UPackage* Package = CreatePackage(TEXT("/Game/BP_Dn2CppSmoke"));
    UBlueprint* Blueprint = FindObject<UBlueprint>(Package, TEXT("BP_Dn2CppSmoke"));
    if (!Blueprint)
    {
        Blueprint = FKismetEditorUtilities::CreateBlueprint(Parent, Package, TEXT("BP_Dn2CppSmoke"),
            BPTYPE_Normal, UBlueprint::StaticClass(), UBlueprintGeneratedClass::StaticClass());
        UEdGraph* Graph = Blueprint->UbergraphPages[0];
        UK2Node_CustomEvent* Event = NewObject<UK2Node_CustomEvent>(Graph);
        Event->CustomFunctionName = TEXT("RunBlueprintAdd");
        Graph->AddNode(Event, false, false);
        Event->CreateNewGuid();
        Event->PostPlacedNewNode();
        Event->AllocateDefaultPins();
        UK2Node_CallFunction* Call = NewObject<UK2Node_CallFunction>(Graph);
        Call->SetFromFunction(ManagedAdd);
        Graph->AddNode(Call, false, false);
        Call->CreateNewGuid();
        Call->PostPlacedNewNode();
        Call->AllocateDefaultPins();
        Call->FindPinChecked(TEXT("value"))->DefaultValue = TEXT("3");
        Graph->GetSchema()->TryCreateConnection(Event->FindPinChecked(UEdGraphSchema_K2::PN_Then), Call->GetExecPin());
        UFunction* ManagedBeginPlay = Parent->FindFunctionByName(TEXT("ReceiveBeginPlay"));
        UFunction* SetHidden = AActor::StaticClass()->FindFunctionByName(TEXT("SetActorHiddenInGame"));
        if (!ManagedBeginPlay || !SetHidden)
        {
            UE_LOG(LogTemp, Error, TEXT("Dn2CppSmoke Blueprint BeginPlay override functions missing"));
            return nullptr;
        }
        UK2Node_Event* BeginPlay = nullptr;
        for (UEdGraphNode* Node : Graph->Nodes)
            if (UK2Node_Event* Candidate = Cast<UK2Node_Event>(Node))
                if (Candidate->EventReference.GetMemberName() == TEXT("ReceiveBeginPlay"))
                    BeginPlay = Candidate;
        if (!BeginPlay)
        {
            BeginPlay = NewObject<UK2Node_Event>(Graph);
            BeginPlay->EventReference.SetExternalMember(TEXT("ReceiveBeginPlay"), Parent);
            BeginPlay->bOverrideFunction = true;
            Graph->AddNode(BeginPlay, false, false);
            BeginPlay->CreateNewGuid();
            BeginPlay->PostPlacedNewNode();
            BeginPlay->AllocateDefaultPins();
        }
        UK2Node_CallParentFunction* ParentCall = NewObject<UK2Node_CallParentFunction>(Graph);
        ParentCall->SetFromFunction(ManagedBeginPlay);
        Graph->AddNode(ParentCall, false, false);
        ParentCall->CreateNewGuid();
        ParentCall->PostPlacedNewNode();
        ParentCall->AllocateDefaultPins();
        UK2Node_CallFunction* Hide = NewObject<UK2Node_CallFunction>(Graph);
        Hide->SetFromFunction(SetHidden);
        Graph->AddNode(Hide, false, false);
        Hide->CreateNewGuid();
        Hide->PostPlacedNewNode();
        Hide->AllocateDefaultPins();
        Hide->FindPinChecked(TEXT("bNewHidden"))->DefaultValue = TEXT("true");
        if (!Graph->GetSchema()->TryCreateConnection(BeginPlay->FindPinChecked(UEdGraphSchema_K2::PN_Then), ParentCall->GetExecPin()) ||
            !Graph->GetSchema()->TryCreateConnection(ParentCall->GetThenPin(), Hide->GetExecPin()))
        {
            UE_LOG(LogTemp, Error, TEXT("Dn2CppSmoke Blueprint BeginPlay override graph connection failed"));
            return nullptr;
        }
        FBlueprintEditorUtils::MarkBlueprintAsStructurallyModified(Blueprint);
        FKismetEditorUtilities::CompileBlueprint(Blueprint);
    }
    FSavePackageArgs SaveArgs;
    SaveArgs.TopLevelFlags = RF_Public | RF_Standalone;
    const FString Filename = FPackageName::LongPackageNameToFilename(TEXT("/Game/BP_Dn2CppSmoke"), FPackageName::GetAssetPackageExtension());
    IFileManager::Get().MakeDirectory(*FPaths::GetPath(Filename), true);
    return UPackage::SavePackage(Package, Blueprint, *Filename, SaveArgs) ? Blueprint->GeneratedClass : nullptr;
}
#endif

static bool ProbeManagedFeatures(UWorld* World)
{
    UClass* FeatureClass = nullptr;
    for (TObjectIterator<UClass> It; It; ++It)
        if ((It->GetName() == TEXT("FeatureProbeActor") || It->GetName() == TEXT("FeatureProbeActor_C")) && It->IsChildOf(AActor::StaticClass()))
            FeatureClass = *It;
    AActor* Actor = FeatureClass ? World->SpawnActor<AActor>(FeatureClass) : nullptr;
    UFunction* Probe = Actor ? Actor->FindFunction(TEXT("ProbeFeatures")) : nullptr;
    UFunction* Transform = Actor ? Actor->FindFunction(TEXT("TransformFeatureValues")) : nullptr;
    if (!Probe || !Transform)
        return false;
    FStructOnScope ProbeParameters(Probe);
    Actor->ProcessEvent(Probe, ProbeParameters.GetStructMemory());
    FIntProperty* ProbeReturn = CastField<FIntProperty>(Probe->GetReturnProperty());
    if (!ProbeReturn || ProbeReturn->GetPropertyValue_InContainer(ProbeParameters.GetStructMemory()) != 511)
        return false;
    FStructOnScope Parameters(Transform);
    FStructProperty* Value = FindFProperty<FStructProperty>(Transform, TEXT("value"));
    FIntProperty* Count = FindFProperty<FIntProperty>(Transform, TEXT("count"));
    FBoolProperty* Enabled = FindFProperty<FBoolProperty>(Transform, TEXT("enabled"));
    FStructProperty* Returned = CastField<FStructProperty>(Transform->GetReturnProperty());
    if (!Value || !Count || !Enabled || !Returned)
        return false;
    *Value->ContainerPtrToValuePtr<FVector>(Parameters.GetStructMemory()) = FVector(1, 2, 3);
    Count->SetPropertyValue_InContainer(Parameters.GetStructMemory(), 40);
    Actor->ProcessEvent(Transform, Parameters.GetStructMemory());
    const bool Success = Count->GetPropertyValue_InContainer(Parameters.GetStructMemory()) == 42 &&
        Enabled->GetPropertyValue_InContainer(Parameters.GetStructMemory()) &&
        *Returned->ContainerPtrToValuePtr<FVector>(Parameters.GetStructMemory()) == FVector(2, 4, 6);
    UE_LOG(LogTemp, Display, TEXT("Dn2CppSmoke features-native=%s"), Success ? TEXT("OK") : TEXT("FAIL"));
    return Success;
}

static UClass* FindManagedActorClass(const FString& Name)
{
    for (TObjectIterator<UClass> It; It; ++It)
        if ((It->GetName() == Name || It->GetName() == Name + TEXT("_C")) && It->IsChildOf(AActor::StaticClass()))
            return *It;
    return nullptr;
}

static bool CallIntProbe(AActor* Actor, const TCHAR* Name, int32 Expected)
{
    UFunction* Function = Actor ? Actor->FindFunction(Name) : nullptr;
    FIntProperty* Return = Function ? CastField<FIntProperty>(Function->GetReturnProperty()) : nullptr;
    if (!Return)
        return false;
    FStructOnScope Parameters(Function);
    Actor->ProcessEvent(Function, Parameters.GetStructMemory());
    const int32 Value = Return->GetPropertyValue_InContainer(Parameters.GetStructMemory());
    UE_LOG(LogTemp, Display, TEXT("Dn2CppSmoke probe=%s value=%d expected=%d"), Name, Value, Expected);
    return Value == Expected;
}

static bool ProbeLifetimeAndAssemblies(UWorld* World)
{
    UClass* LifetimeClass = FindManagedActorClass(TEXT("LifetimeProbeActor"));
    UClass* AssemblyClass = FindManagedActorClass(TEXT("AssemblyProbeActor"));
    AActor* Lifetime = LifetimeClass ? World->SpawnActor<AActor>(LifetimeClass) : nullptr;
    AActor* Assembly = AssemblyClass ? World->SpawnActor<AActor>(AssemblyClass) : nullptr;
    if (!CallIntProbe(Assembly, TEXT("ProbeAssemblies"), 42) ||
        !CallIntProbe(Lifetime, TEXT("ProbeLifetimeReferences"), 255))
        return false;
    UFunction* Remember = Lifetime->FindFunction(TEXT("RememberVictim"));
    FObjectPropertyBase* VictimProperty = Remember ? FindFProperty<FObjectPropertyBase>(Remember, TEXT("victim")) : nullptr;
    if (!VictimProperty)
        return false;
    for (int32 Iteration = 0; Iteration < 3; ++Iteration)
    {
        AActor* Victim = World->SpawnActor<AActor>();
        FStructOnScope Parameters(Remember);
        VictimProperty->SetObjectPropertyValue_InContainer(Parameters.GetStructMemory(), Victim);
        Lifetime->ProcessEvent(Remember, Parameters.GetStructMemory());
        if (!World->DestroyActor(Victim))
            return false;
        CollectGarbage(RF_NoFlags);
        if (!CallIntProbe(Lifetime, TEXT("ProbeDestroyedVictim"), 7))
            return false;
    }
    return true;
}

static void FinishSmoke(bool Success, UWorld* World)
{
    Success &= PrepareSmokeLateCallback(World);
#if WITH_EDITOR
    if (World) World->DestroyWorld(false);
#endif
    bool ClrLoaded = false;
#if PLATFORM_MAC
    for (uint32 Index = 0; Index < _dyld_image_count(); ++Index)
    {
        const FString Image = UTF8_TO_TCHAR(_dyld_get_image_name(Index));
        ClrLoaded |= Image.Contains(TEXT("libhostfxr")) || Image.Contains(TEXT("libcoreclr")) ||
            Image.Contains(TEXT("libhostpolicy")) || Image.Contains(TEXT("libclrjit"));
    }
#endif
    const bool ExpectNative = FParse::Param(FCommandLine::Get(), TEXT("Dn2CppSmokeExpectNative"));
    Success &= ExpectNative ? !ClrLoaded : ClrLoaded;
    UE_LOG(LogTemp, Display, TEXT("Dn2CppSmoke engine-smoke=%s"), Success ? TEXT("OK") : TEXT("FAIL"));
    FString ResultPath;
    if (FParse::Value(FCommandLine::Get(), TEXT("Dn2CppSmokeResult="), ResultPath))
    {
        const FString Result = Success
            ? FString::Printf(TEXT("engine-smoke=OK\ncounter=45\nblueprint-override=OK\nasync=1023\nassemblies=42\nlifetime=255\ndestroyed=7\nfeatures=511\nref-out=42:True\nabi-shapes=7\nclr=%s\n"), ClrLoaded ? TEXT("present") : TEXT("absent"))
            : TEXT("engine-smoke=FAIL\n");
        if (!FFileHelper::SaveStringToFile(Result, *ResultPath, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM))
            Success = false;
    }
    FPlatformMisc::RequestExitWithStatus(false, Success ? 0 : 1);
}

class FBaselineModule : public FDefaultGameModuleImpl
{
public:
    virtual void StartupModule() override
    {
        InstallSmokePIE();
        if (!FParse::Param(FCommandLine::Get(), TEXT("Dn2CppSmoke")))
            return;
        FCoreDelegates::GetOnPostEngineInit().AddLambda([]
        {
            FTSTicker::GetCoreTicker().AddTicker(FTickerDelegate::CreateLambda([](float)
            {
                FEditorScriptExecutionGuard ScriptGuard;
                UClass* SmokeClass = nullptr;
                for (TObjectIterator<UClass> It; It; ++It)
                    if ((It->GetName() == TEXT("SmokeActor") || It->GetName() == TEXT("SmokeActor_C")) && It->IsChildOf(AActor::StaticClass()))
                        SmokeClass = *It;
                if (!SmokeClass)
                {
                    UE_LOG(LogTemp, Error, TEXT("Dn2CppSmoke class lookup failed"));
                    for (TObjectIterator<UClass> It; It; ++It)
                        if (It->GetName().Contains(TEXT("Smoke")))
                            UE_LOG(LogTemp, Error, TEXT("Dn2CppSmoke candidate=%s actor=%d"), *It->GetPathName(), It->IsChildOf(AActor::StaticClass()));
                }
                bool Success = false;
                if (SmokeClass)
                {
#if WITH_EDITOR
                    SmokeClass = CreateSmokeBlueprint(SmokeClass);
                    UWorld::InitializationValues Initialization;
                    Initialization.AllowAudioPlayback(false).CreatePhysicsScene(false);
                    UWorld* World = UWorld::CreateWorld(EWorldType::Game, false, TEXT("Dn2CppSmokeMap"),
                        CreatePackage(TEXT("/Game/Dn2CppSmokeMap")), true, ERHIFeatureLevel::Num, &Initialization);
#else
                    SmokeClass = LoadObject<UClass>(nullptr, TEXT("/Game/BP_Dn2CppSmoke.BP_Dn2CppSmoke_C"));
                    UWorld* World = GWorld;
#endif
                    AActor* Actor = SmokeClass && World ? World->SpawnActor<AActor>(SmokeClass) : nullptr;
                    if (Actor)
                    {
#if WITH_EDITOR
                        World->SetFlags(RF_Public | RF_Standalone);
                        FSavePackageArgs SaveArgs;
                        SaveArgs.TopLevelFlags = RF_Public | RF_Standalone;
                        const FString MapFile = FPackageName::LongPackageNameToFilename(TEXT("/Game/Dn2CppSmokeMap"), FPackageName::GetMapPackageExtension());
                        if (!UPackage::SavePackage(World->GetPackage(), World, *MapFile, SaveArgs))
                        {
                            FPlatformMisc::RequestExitWithStatus(false, 1);
                            return false;
                        }
#endif
                        if (!Actor->HasActorBegunPlay())
                            Actor->DispatchBeginPlay();
                        UFunction* Add = Actor->FindFunction(TEXT("RunBlueprintAdd"));
                        FIntProperty* Counter = FindFProperty<FIntProperty>(SmokeClass, TEXT("Counter"));
                        if (Add && Counter)
                        {
                            const bool OverrideRan = Actor->HasActorBegunPlay() && Actor->IsHidden() &&
                                Counter->GetPropertyValue_InContainer(Actor) == 42;
                            UE_LOG(LogTemp, Display, TEXT("Dn2CppSmoke blueprint-override=%s"), OverrideRan ? TEXT("OK") : TEXT("FAIL"));
                            Actor->ProcessEvent(Add, nullptr);
                            const int32 Stored = Counter->GetPropertyValue_InContainer(Actor);
                            Success = OverrideRan && Stored == 45;
                            UE_LOG(LogTemp, Display, TEXT("Dn2CppSmoke blueprint-call=%d property=%d"), Stored, Stored);
                        }
                        else
                            UE_LOG(LogTemp, Error, TEXT("Dn2CppSmoke missing event=%d property=%d class=%s"), Add != nullptr, Counter != nullptr, *SmokeClass->GetPathName());
                    }
                    else
                        UE_LOG(LogTemp, Error, TEXT("Dn2CppSmoke actor spawn failed"));
                    Success &= World && ProbeManagedFeatures(World);
                    Success &= World && ProbeLifetimeAndAssemblies(World);
                    UClass* AsyncClass = nullptr;
                    for (TObjectIterator<UClass> It; It; ++It)
                        if ((It->GetName() == TEXT("AsyncProbeActor") || It->GetName() == TEXT("AsyncProbeActor_C")) && It->IsChildOf(AActor::StaticClass()))
                            AsyncClass = *It;
                    AActor* AsyncActor = AsyncClass && World ? World->SpawnActor<AActor>(AsyncClass) : nullptr;
                    UFunction* Start = AsyncActor ? AsyncActor->FindFunction(TEXT("StartProbe")) : nullptr;
                    FIntProperty* Result = AsyncClass ? FindFProperty<FIntProperty>(AsyncClass, TEXT("AsyncResult")) : nullptr;
                    if (Success && Start && Result)
                    {
                        AsyncActor->ProcessEvent(Start, nullptr);
                        FTSTicker::GetCoreTicker().AddTicker(FTickerDelegate::CreateLambda([World, AsyncActor, Result, Elapsed = 0.0f](float Delta) mutable
                        {
                            Elapsed += Delta;
                            const int32 Value = Result->GetPropertyValue_InContainer(AsyncActor);
                            if (Value == 0 && Elapsed < 15.0f)
                                return true;
                            UE_LOG(LogTemp, Display, TEXT("Dn2CppSmoke async-native=%d"), Value);
                            FinishSmoke(Value == 1023, World);
                            return false;
                        }), 0.05f);
                        return false;
                    }
                    FinishSmoke(false, World);
                    return false;
                }
                FinishSmoke(Success, nullptr);
                return false;
            }), 1.0f);
        });
    }
};

IMPLEMENT_PRIMARY_GAME_MODULE(FBaselineModule, Baseline, "Baseline");
