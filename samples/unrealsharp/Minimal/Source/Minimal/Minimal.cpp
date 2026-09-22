#include "Modules/ModuleManager.h"
#include "Containers/Ticker.h"
#include "Engine/World.h"
#include "GameFramework/Actor.h"
#include "Misc/CoreDelegates.h"
#include "UObject/StructOnScope.h"
#include "UObject/UObjectIterator.h"
#include "UObject/UnrealType.h"

#if PLATFORM_ANDROID
#include <link.h>
#endif

static bool IsClrLoaded()
{
#if PLATFORM_ANDROID
    bool Loaded = false;
    dl_iterate_phdr([](dl_phdr_info* Info, size_t, void* Context)
    {
        if (Info->dlpi_name)
        {
            const FString Image = UTF8_TO_TCHAR(Info->dlpi_name);
            if (Image.Contains(TEXT("libhostfxr")) || Image.Contains(TEXT("libcoreclr")) ||
                Image.Contains(TEXT("libhostpolicy")) || Image.Contains(TEXT("libclrjit")))
                *static_cast<bool*>(Context) = true;
        }
        return 0;
    }, &Loaded);
    return Loaded;
#else
    return false;
#endif
}

class FMinimalModule : public FDefaultGameModuleImpl
{
public:
    virtual void StartupModule() override
    {
#if PLATFORM_ANDROID && !WITH_EDITOR
        FCoreDelegates::GetOnPostEngineInit().AddLambda([]
        {
            FTSTicker::GetCoreTicker().AddTicker(FTickerDelegate::CreateLambda(
                [Elapsed = 0.0f](float Delta) mutable
            {
                Elapsed += Delta;
                UWorld* World = GWorld;
                if (!World || !World->IsGameWorld() || !World->HasBegunPlay())
                {
                    if (Elapsed >= 30.0f)
                        UE_LOG(LogTemp, Error, TEXT("Dn2CppAndroidMinimal status=FAIL reason=game-world-timeout"));
                    return Elapsed < 30.0f;
                }

                UClass* ManagedClass = nullptr;
                for (TObjectIterator<UClass> It; It; ++It)
                    if ((It->GetName() == TEXT("MinimalManagedActor") ||
                         It->GetName() == TEXT("MinimalManagedActor_C")) && It->IsChildOf(AActor::StaticClass()))
                    {
                        ManagedClass = *It;
                        break;
                    }
                AActor* Actor = ManagedClass ? World->SpawnActor<AActor>(ManagedClass) : nullptr;
                UFunction* Function = Actor ? Actor->FindFunction(TEXT("Answer")) : nullptr;
                FIntProperty* Return = Function ? CastField<FIntProperty>(Function->GetReturnProperty()) : nullptr;
                int32 Value = -1;
                if (Return)
                {
                    FStructOnScope Parameters(Function);
                    Actor->ProcessEvent(Function, Parameters.GetStructMemory());
                    Value = Return->GetPropertyValue_InContainer(Parameters.GetStructMemory());
                }
                const bool ClrLoaded = IsClrLoaded();
                UE_LOG(LogTemp, Display, TEXT("Dn2CppAndroidMinimal result=%d clr=%s status=%s"), Value,
                    ClrLoaded ? TEXT("present") : TEXT("absent"), Value == 42 && !ClrLoaded ? TEXT("OK") : TEXT("FAIL"));
                return false;
            }), 0.25f);
        });
#endif
    }
};

IMPLEMENT_PRIMARY_GAME_MODULE(FMinimalModule, Minimal, "Minimal");
