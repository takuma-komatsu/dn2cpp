#include "SmokeLateCallback.h"
#include "CoreMinimal.h"
#include "CSManagedCallbacksCache.h"
#include "CSManagedGCHandle.h"
#include "Engine/World.h"
#if WITH_EDITOR
#include "Editor.h"
#include "UObject/Script.h"
#endif
#include "GameFramework/Actor.h"
#include "Misc/CoreDelegates.h"
#include "UObject/UObjectIterator.h"
#include <cstdio>
#include <string>
#include <thread>

bool PrepareSmokeLateCallback(UWorld* World)
{
#if WITH_EDITOR
    FEditorScriptExecutionGuard ScriptGuard;
#endif
    const FString Path = FPlatformMisc::GetEnvironmentVariable(TEXT("DN2CPP_SMOKE_LATE_CALLBACK_FILE"));
    UClass* ProbeClass = nullptr;
    for (TObjectIterator<UClass> It; It; ++It)
        if (It->GetName() == TEXT("ShutdownProbeActor_C") && It->IsChildOf(AActor::StaticClass()))
            ProbeClass = *It;
    AActor* Actor = World && ProbeClass ? World->SpawnActor<AActor>(ProbeClass) : nullptr;
    UFunction* Prepare = Actor ? Actor->FindFunction(TEXT("PrepareLateCallback")) : nullptr;
    UFunction* AbiProbe = Actor ? Actor->FindFunction(TEXT("ProbeAbiShapes")) : nullptr;
    if (!AbiProbe)
    {
        UE_LOG(LogTemp, Error, TEXT("Dn2CppSmoke ABI probe unavailable: class=%d actor=%d"), ProbeClass != nullptr, Actor != nullptr);
        return false;
    }
    struct { int32 ReturnValue = 0; } Abi;
    Actor->ProcessEvent(AbiProbe, &Abi);
    if (Abi.ReturnValue != 7)
    {
        UE_LOG(LogTemp, Error, TEXT("Dn2CppSmoke ABI probe returned %d instead of 7"), Abi.ReturnValue);
        return false;
    }
    UE_LOG(LogTemp, Display, TEXT("Dn2CppSmoke abi-shapes=7"));
    if (Path.IsEmpty()) return true;
    if (!Prepare) return false;
    struct { int64 ReturnValue = 0; } Params;
    Actor->ProcessEvent(Prepare, &Params);
    const auto Callback = GetManagedCallbacks().InvokeDelegate;
    if (!Params.ReturnValue || !Callback) return false;
    const FGCHandleIntPtr Handle{reinterpret_cast<uint8*>(Params.ReturnValue)};
    const std::string Output(TCHAR_TO_UTF8(*Path));
    if (FILE* File = std::fopen(Output.c_str(), "a"))
    {
        std::fputs("abi-shapes=7\n", File);
        std::fclose(File);
    }
    Callback(Handle);
    // The host shuts down on OnPreExit; this raw callback arrives after handle release.
    FCoreDelegates::OnExit.AddLambda([Callback, Handle, Output]
    {
        std::thread Worker([Callback, Handle] { Callback(Handle); });
        Worker.join();
        if (FILE* File = std::fopen(Output.c_str(), "a"))
        {
            std::fputs("late-callback-returned\n", File);
            std::fclose(File);
        }
    });
    return true;
}
