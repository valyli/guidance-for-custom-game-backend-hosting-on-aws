// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. SPDX-License-Identifier: MIT-0


#include "AmazonGameLiftIntegration.h"
#include "../../AWSGameSDK/AWSGameSDK.h"
#include "../../PlayerDataSave.h"
#include "Engine/World.h"
#include "Kismet/GameplayStatics.h"
#include "../../PlayerDataManager.h"

// Sets default values for this component's properties
UAmazonGameLiftIntegration::UAmazonGameLiftIntegration()
{
	// Set this component to be initialized when the game starts, and to be ticked every frame.  You can turn these features
	// off to improve performance if you don't need them.
	PrimaryComponentTick.bCanEverTick = true;

	// ...
}

// Called when the game starts
void UAmazonGameLiftIntegration::BeginPlay()
{
	Super::BeginPlay();

    // Get the subsystems
    UGameInstance* GameInstance = Cast<UGameInstance>(UGameplayStatics::GetGameInstance(GetWorld()));
    UAWSGameSDK* AWSGameSDK =  GameInstance->GetSubsystem<UAWSGameSDK>();
    UPlayerDataManager* PlayerDataManager = GameInstance->GetSubsystem<UPlayerDataManager>(); 
    
    // Init with the login endpoint defined in the Editor and a callback to handle errors for logging in and refresh
	AWSGameSDK->Init(this->m_loginEndpoint);
	AWSGameSDK->OnLoginFailure.AddUObject(this, &UAmazonGameLiftIntegration::OnLoginOrRefreshErrorCallback);

    // Define the OnLoginResult callback
	UAWSGameSDK::FLoginComplete loginCallback;
	loginCallback.BindUObject(this, &UAmazonGameLiftIntegration::OnLoginResultCallback);
    
    // Get player data if we have any 
    auto playerData = PlayerDataManager->LoadGameData();

    // If not saved player data, login as a new player
    if(playerData == nullptr){
        UE_LOG(LogTemp, Display, TEXT("No player data yet, request a new identity"));
        if(GEngine)
            GEngine->AddOnScreenDebugMessage(-1, 15.0f, FColor::Black, TEXT("No player data yet, request a new identity"));	

        // Login as a guest user
        AWSGameSDK->LoginAsNewGuestUser(loginCallback);
    }
    else {
        UE_LOG(LogTemp, Display, TEXT("Existing player data\n user_id: %s \n guest_secret: %s"), *playerData->UserId, *playerData->GuestSecret);
        if(GEngine)
            GEngine->AddOnScreenDebugMessage(-1, 30.0f, FColor::Black, FString::Printf(TEXT("Existing player data\n user_id: %s \n guest_secret: %s"), *playerData->UserId, *playerData->GuestSecret),false, FVector2D(1.5f,1.5f));

        AWSGameSDK->LoginAsGuestUser(playerData->UserId, playerData->GuestSecret, loginCallback);
    }
}

// Called when there is an error with login or token refresh. You will need to handle logging in again here
void UAmazonGameLiftIntegration::OnLoginOrRefreshErrorCallback(const FString& errorMessage){
    UE_LOG(LogTemp, Display, TEXT("Received login error: %s \n"), *errorMessage);
    if(GEngine)
        GEngine->AddOnScreenDebugMessage(-1, 30.0f, FColor::Red, FString::Printf(TEXT("Received login error: \n %s \n"), *errorMessage), false, FVector2D(1.5f,1.5f));

    // NOTE:  You will need to handle logging in again here
}

// Called when login is done
void UAmazonGameLiftIntegration::OnLoginResultCallback(const UserInfo& userInfo){
    UE_LOG(LogTemp, Display, TEXT("Received login response: %s \n"), *userInfo.ToString());
    if(GEngine)
        GEngine->AddOnScreenDebugMessage(-1, 30.0f, FColor::Black, FString::Printf(TEXT("Received login response: \n %s \n"), *userInfo.user_id), false, FVector2D(1.5f,1.5f));

    // Save the player data
    UGameInstance* GameInstance = Cast<UGameInstance>(UGameplayStatics::GetGameInstance(GetWorld()));
    UPlayerDataManager* PlayerDataManager = GameInstance->GetSubsystem<UPlayerDataManager>(); 
    PlayerDataManager->SaveGameData(userInfo.user_id, userInfo.guest_secret);

    // NOTE: You could get the expiration in seconds for refresh token (and modify to FDateTime) as well as the refresh token itself from the userInfo,
    // and login next time with the refresh token itself. This can be done by calling AWSGameSDK->LoginWithRefreshToken(refreshToken, loginCallback);

    // Test calling our custom backend system to set player data
	UAWSGameSDK::FRequestComplete requestMatchmakingCallback;
	requestMatchmakingCallback.BindUObject(this, &UAmazonGameLiftIntegration::OnRequestMatchmakingResponse);
    UAWSGameSDK* AWSGameSDK = GameInstance->GetSubsystem<UAWSGameSDK>();
	// POST Request with latencyInMs JSON to the gamelift backend endpoint
    AWSGameSDK->BackendPostRequest(this->m_gameliftIntegrationBackendEndpointUrl, "request-matchmaking", "{ \"latencyInMs\": { \"us-east-1\" : 50, \"us-west-2\" : 50, \"eu-west-1\" : 50 }}", requestMatchmakingCallback);
}

// Callback for matchmaking request
void UAmazonGameLiftIntegration::OnRequestMatchmakingResponse(const FString& response){
	UE_LOG(LogTemp, Display, TEXT("Received matchmaking response: %s \n"), *response);
    if(GEngine)
        GEngine->AddOnScreenDebugMessage(-1, 30.0f, FColor::Black, FString::Printf(TEXT("Received matchmaking response: %s \n"), *response), false, FVector2D(1.5f,1.5f));

    // Get TicketID from the response
    TSharedPtr<FJsonObject> JsonObject;
    TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(response);
    if (FJsonSerializer::Deserialize(Reader, JsonObject)) {
        // Get the ticket ID from the response
        this->m_ticketId = JsonObject->GetStringField("TicketId");
        UE_LOG(LogTemp, Display, TEXT("Received matchmaking ticketId: %s \n"), *this->m_ticketId);
    }
    // Test calling our custom backend system to get match status
    UAWSGameSDK::FRequestComplete getMatchStatusCallback;
    getMatchStatusCallback.BindUObject(this, &UAmazonGameLiftIntegration::OnGetMatchStatusResponse);
    TMap<FString,FString> params;
    params.Add("ticketId", this->m_ticketId);
    UGameInstance* GameInstance = Cast<UGameInstance>(UGameplayStatics::GetGameInstance(GetWorld()));
    UAWSGameSDK* AWSGameSDK = GameInstance->GetSubsystem<UAWSGameSDK>();
    AWSGameSDK->BackendGetRequest(this->m_gameliftIntegrationBackendEndpointUrl, "get-match-status", params, getMatchStatusCallback);
}

// Callback for match status request
void UAmazonGameLiftIntegration::OnGetMatchStatusResponse(const FString& response){
	UE_LOG(LogTemp, Display, TEXT("Received match status response: %s \n"), *response);

    if(GEngine)
        GEngine->AddOnScreenDebugMessage(-1, 30.0f, FColor::Black, FString::Printf(TEXT("Received match status response: %s \n"), *response), false, FVector2D(1.5f,1.5f));

    // Get the match status from the response
    FString matchStatus = "";
    TSharedPtr<FJsonObject> JsonObject;
    TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(response);
    if (FJsonSerializer::Deserialize(Reader, JsonObject)) {
        // Get the match status from the response
        matchStatus = JsonObject->GetStringField("MatchmakingStatus");
        UE_LOG(LogTemp, Display, TEXT("Received match status: %s \n"), *matchStatus);
    }
    else {
        UE_LOG(LogTemp, Display, TEXT("No valid status yet %s \n"));
        if(GEngine)
            GEngine->AddOnScreenDebugMessage(-1, 30.0f, FColor::Black, FString::Printf(TEXT("No valid status yet\n")), false, FVector2D(1.5f,1.5f));
    }

    // If matchStatus is empty, MatchmakingQueued, MatchmakingSearch, or PotentialMatchCreated, request match status again
    if(matchStatus.IsEmpty() || matchStatus == "MatchmakingQueued" || matchStatus == "MatchmakingSearching" || matchStatus == "PotentialMatchCreated"){
        UE_LOG(LogTemp, Display, TEXT("Requesting match status again..."));
        ScheduleGetMatchStatus(1.5f);
    }
    else if(matchStatus == "MatchmakingSucceeded"){
        UE_LOG(LogTemp, Display, TEXT("Matchmaking succeeded, connecting..."));
        if(GEngine)
            GEngine->AddOnScreenDebugMessage(-1, 30.0f, FColor::Black, FString::Printf(TEXT("Matchmaking succeeded, connecting...\n")), false, FVector2D(1.5f,1.5f));
        // TODO, Get connection info and connect
    }
    else {
        UE_LOG(LogTemp, Display, TEXT("Matchmaking failed."));
        if(GEngine)
            GEngine->AddOnScreenDebugMessage(-1, 30.0f, FColor::Black, FString::Printf(TEXT("Matchmaking failed.\n")), false, FVector2D(1.5f,1.5f));
    }
}

void UAmazonGameLiftIntegration::ScheduleGetMatchStatus(float waitTime)
{
	FTimerDelegate getMatchStatusDelegate;
	getMatchStatusDelegate.BindWeakLambda(this, [this]()
	{
        // Test calling our custom backend system to get match status
        UAWSGameSDK::FRequestComplete getMatchStatusCallback;
        getMatchStatusCallback.BindUObject(this, &UAmazonGameLiftIntegration::OnGetMatchStatusResponse);
        TMap<FString,FString> params;
        params.Add("ticketId", this->m_ticketId);
        UGameInstance* GameInstance = Cast<UGameInstance>(UGameplayStatics::GetGameInstance(GetWorld()));
        UAWSGameSDK* AWSGameSDK = GameInstance->GetSubsystem<UAWSGameSDK>();
        AWSGameSDK->BackendGetRequest(this->m_gameliftIntegrationBackendEndpointUrl, "get-match-status", params, getMatchStatusCallback);
	});

	GetWorld()->GetTimerManager().SetTimer(this->m_getMatchStatusTimerHandle, getMatchStatusDelegate, waitTime, false);
}

// Called every frame
void UAmazonGameLiftIntegration::TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction)
{
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

	// ...
}

