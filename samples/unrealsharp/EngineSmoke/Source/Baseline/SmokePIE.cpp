#include "SmokePIE.h"
#include "CoreMinimal.h"

#if WITH_EDITOR
#include "Containers/Ticker.h"
#include "Editor.h"
#include "Engine/World.h"
#include "EngineUtils.h"
#include "GameFramework/Actor.h"
#include "Misc/CommandLine.h"
#include "Misc/CoreDelegates.h"
#include "Misc/FileHelper.h"
#include "Misc/Parse.h"
#include "PlayInEditorDataTypes.h"
#include "UObject/UnrealType.h"
#endif

void InstallSmokePIE()
{
#if WITH_EDITOR
    if (!FParse::Param(FCommandLine::Get(), TEXT("Dn2CppSmokePIE")))
        return;
    FCoreDelegates::GetOnPostEngineInit().AddLambda([]
    {
        FTSTicker::GetCoreTicker().AddTicker(FTickerDelegate::CreateLambda(
            [Phase = 0, Elapsed = 0.0f, Success = false](float Delta) mutable
        {
            Elapsed += Delta;
            if (Phase == 0 && GEditor)
            {
                FRequestPlaySessionParams Request;
                Request.SessionDestination = EPlaySessionDestinationType::InProcess;
                Request.WorldType = EPlaySessionWorldType::PlayInEditor;
                GEditor->RequestPlaySession(Request);
                Phase = 1;
            }
            if (Phase == 1 && GEditor && GEditor->PlayWorld && GEditor->PlayWorld->HasBegunPlay())
            {
                UWorld* World = GEditor->PlayWorld;
                for (TActorIterator<AActor> It(World); It; ++It)
                {
                    UFunction* Add = It->FindFunction(TEXT("RunBlueprintAdd"));
                    FIntProperty* Counter = FindFProperty<FIntProperty>(It->GetClass(), TEXT("Counter"));
                    if (!Add || !Counter)
                        continue;
                    Success = World->WorldType == EWorldType::PIE && It->HasActorBegunPlay() && It->IsHidden() &&
                        Counter->GetPropertyValue_InContainer(*It) == 42;
                    It->ProcessEvent(Add, nullptr);
                    Success &= Counter->GetPropertyValue_InContainer(*It) == 45;
                    break;
                }
                GEditor->RequestEndPlayMap();
                Phase = 2;
            }
            if ((Phase == 2 && GEditor && !GEditor->PlayWorld) || Elapsed > 90.0f)
            {
                Success &= Phase == 2 && GEditor && !GEditor->PlayWorld;
                FString ResultPath;
                if (!FParse::Value(FCommandLine::Get(), TEXT("Dn2CppSmokeResult="), ResultPath))
                    Success = false;
                else if (!FFileHelper::SaveStringToFile(Success
                    ? TEXT("pie=OK\nworld=PIE\nbegin-play=42\nblueprint-override=OK\nblueprint=45\nended=true\n")
                    : TEXT("pie=FAIL\n"), *ResultPath, FFileHelper::EEncodingOptions::ForceUTF8WithoutBOM))
                    Success = false;
                UE_LOG(LogTemp, Display, TEXT("Dn2CppSmoke PIE=%s"), Success ? TEXT("OK") : TEXT("FAIL"));
                FPlatformMisc::RequestExitWithStatus(false, Success ? 0 : 1);
                return false;
            }
            return true;
        }), 0.1f);
    });
#endif
}
