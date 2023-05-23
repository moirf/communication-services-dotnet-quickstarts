using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using CallAutomation_AppointmentReminder;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Fetch configuration and add call automation as singleton service
var callConfigurationSection = builder.Configuration.GetSection(nameof(CallConfiguration));
builder.Services.Configure<CallConfiguration>(callConfigurationSection);
builder.Services.AddSingleton(new CallAutomationClient(callConfigurationSection["ConnectionString"]));
var callConnectionId = "";
//var app = builder.Build();

var sourceIdentity = await CallAutomationMediaHelper.ProvisionAzureCommunicationServicesIdentity(callConfigurationSection["ConnectionString"]);
//var callAutoamtionOptions = new CallAutomationClientOptions(source: new CommunicationUserIdentifier(sourceIdentity));
CallAutomationClientOptions callAutoamtionOptions = new CallAutomationClientOptions() { Source = new CommunicationUserIdentifier(sourceIdentity) };
builder.Services.AddSingleton(new CallAutomationClient(callConfigurationSection["ConnectionString"], callAutoamtionOptions));
var app = builder.Build();

// Api to initiate out bound call beta version
/*app.MapPost("/api/call", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{
    var source = new CallSource(new CommunicationUserIdentifier(sourceIdentity))
    {
        CallerId = new PhoneNumberIdentifier(callConfiguration.Value.SourcePhoneNumber)
    };
    var target = new PhoneNumberIdentifier(callConfiguration.Value.TargetPhoneNumber);

    var createCallOption = new CreateCallOptions(source,
        new List<CommunicationIdentifier>() { target },
        new Uri(callConfiguration.Value.CallbackEventUri));

    var response = await callAutomationClient.CreateCallAsync(createCallOption).ConfigureAwait(false);

    logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" +
        $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}");
});*/

app.MapPost("/api/call", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{
    var targetPhoneNumber = new PhoneNumberIdentifier(callConfiguration.Value.TargetPhoneNumber);
    var sourcePhoneNumber = new PhoneNumberIdentifier(callConfiguration.Value.SourcePhoneNumber);
    var callInvite = new CallInvite(targetPhoneNumberIdentity: targetPhoneNumber, callerIdNumber: sourcePhoneNumber);
    //var callInvite = new CallInvite(new PhoneNumberIdentifier(callConfiguration.Value.TargetPhoneNumber), sourcePhoneNumber);
    //var acs_user1 = new CommunicationUserIdentifier(callConfiguration.Value.AcsUser2);
    //var callInvite  = new CallInvite(acs_user1);

    //var acs_user2 = new CommunicationUserIdentifier(callConfiguration.Value.AcsUser2);
    var createCallOption = new CreateCallOptions(callInvite, new Uri(callConfiguration.Value.CallbackEventUri));

    //var createCallOption = new CreateGroupCallOptions(new List<CommunicationIdentifier> { acs_user1, targetPhoneNumber }, new Uri(callConfiguration.Value.CallbackEventUri)) { SourceCallerIdNumber = sourcePhoneNumber };

    // 1:1 create call
    var response = await callAutomationClient.CreateCallAsync(createCallOption).ConfigureAwait(false);

    logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" + $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}");
});

app.MapPost("/api/callACSIdentity", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{
    var targetPhoneNumber = new PhoneNumberIdentifier(callConfiguration.Value.TargetPhoneNumber);
    var sourcePhoneNumber = new PhoneNumberIdentifier(callConfiguration.Value.SourcePhoneNumber);
    //var callInvite = new CallInvite(targetPhoneNumberIdentity: targetPhoneNumber, callerIdNumber: sourcePhoneNumber);
    //var callInvite = new CallInvite(new PhoneNumberIdentifier(callConfiguration.Value.TargetPhoneNumber), sourcePhoneNumber);
    var acs_user1 = new CommunicationUserIdentifier(callConfiguration.Value.AcsUser1);
    var callInvite = new CallInvite(acs_user1);

    //var acs_user2 = new CommunicationUserIdentifier(callConfiguration.Value.AcsUser2);
    var createCallOption = new CreateCallOptions(callInvite, new Uri(callConfiguration.Value.CallbackEventUri));

    //var createCallOption = new CreateGroupCallOptions(new List<CommunicationIdentifier> { acs_user1, targetPhoneNumber }, new Uri(callConfiguration.Value.CallbackEventUri)) { SourceCallerIdNumber = sourcePhoneNumber };

    // 1:1 create call
    var response = await callAutomationClient.CreateCallAsync(createCallOption).ConfigureAwait(false);

    logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" + $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}");
});

//Answer a group call
app.MapPost("/api/CreateGroupCall", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{
    var targetPhoneNumber = new PhoneNumberIdentifier(callConfiguration.Value.TargetPhoneNumber);
    var sourcePhoneNumber = new PhoneNumberIdentifier(callConfiguration.Value.SourcePhoneNumber);

    var acs_user1 = new CommunicationUserIdentifier(callConfiguration.Value.AcsUser1);
    var callInvite = new CallInvite(acs_user1);

    var acs_user2 = new CommunicationUserIdentifier(callConfiguration.Value.AcsUser2);
    var createCallOption = new CreateGroupCallOptions(new List<CommunicationIdentifier> { acs_user1, targetPhoneNumber }, new Uri(callConfiguration.Value.CallbackEventUri)) { SourceCallerIdNumber = sourcePhoneNumber };

    var response = await callAutomationClient.CreateGroupCallAsync(createCallOption).ConfigureAwait(false);
    logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" + $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}");
});

// transfer call pstn
app.MapPost("/api/transfer", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{
    //var transferDestination = new CallInvite(new PhoneNumberIdentifier("+917972400258"), new PhoneNumberIdentifier("+18662318150"));
    var transferOption = new TransferToParticipantOptions(new PhoneNumberIdentifier("+917972400258"));
    var callConnection = callAutomationClient.GetCallConnection(callConnectionId);
    var result = await callConnection.TransferCallToParticipantAsync(transferOption);
});

//transfer call to acs user
app.MapPost("/api/transferacsuser", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{
    var transferOption = new TransferToParticipantOptions(new CommunicationUserIdentifier(callConfiguration.Value.AcsUser1));
    var callConnection = callAutomationClient.GetCallConnection(callConnectionId);
    var result = await callConnection.TransferCallToParticipantAsync(transferOption);
});

//add participant to call
app.MapPost("/api/addParticipant", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration,
    [Required] string addCallerID,
            ILogger<Program> logger) =>

{
    CallInvite callInvite = new CallInvite(new PhoneNumberIdentifier(addCallerID), new PhoneNumberIdentifier("+14352752486"));        //new CommunicationUserIdentifier("8:acs:4fecba10-f581-4e33-baf5-a2b7ed2ff6f8_00000017-cc2a-0923-f883-0848220089f1"));;
    AddParticipantResult addParticipantResult = await callAutomationClient.GetCallConnection(callConnectionId).AddParticipantAsync(callInvite);

    return Results.Ok();
});

app.MapPost("/api/addACSParticipant", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration,
    [Required] string addCallerID,
            ILogger<Program> logger) =>

{
    CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(addCallerID));        //new CommunicationUserIdentifier("8:acs:4fecba10-f581-4e33-baf5-a2b7ed2ff6f8_00000017-cc2a-0923-f883-0848220089f1"));;
    AddParticipantResult addParticipantResult = await callAutomationClient.GetCallConnection(callConnectionId).AddParticipantAsync(callInvite);

    return Results.Ok();
});

//Play audio in call to targated participant
app.MapPost("/api/playaudio", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, [Required] string callerId, ILogger<Program> logger) =>
{
    var callConnection = callAutomationClient.GetCallConnection(callConnectionId);
    var fileSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AppointmentReminderMenuAudio));
    var callConnectionMedia = callConnection.GetCallMedia();
    var targetUser = new PhoneNumberIdentifier(callerId);
    var playOptions = new PlayOptions(fileSource, new PhoneNumberIdentifier[] { targetUser })
    {
        OperationContext = "PlayAudio",
        Loop = false,
    };
    //var result = await callConnectionMedia.PlayAsync(playOptions);
    var result = await callConnectionMedia.PlayAsync(playOptions);
});

//Play audio in call to targated participant
app.MapPost("/api/ACSplayaudio", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, [Required] string callerId, ILogger<Program> logger) =>
{
    var callConnection = callAutomationClient.GetCallConnection(callConnectionId);
    var callConnectionMedia = callConnection.GetCallMedia();
    var targetUser = new CommunicationUserIdentifier(callerId);
    var fileSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AppointmentConfirmedAudio));
    var playOptions = new PlayOptions(fileSource, new CommunicationUserIdentifier[] { targetUser })
    {
        OperationContext = "PlayAudio",
        Loop = false,
    };

    var result = await callConnectionMedia.PlayAsync(playOptions);
    return Results.Ok();
});

//play audio to all participants
app.MapPost("/api/playaudioall", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{
    var callConnection = callAutomationClient.GetCallConnection(callConnectionId);
    var callConnectionMedia = callConnection.GetCallMedia();
    var fileSource1 = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AppointmentReminderMenuAudio));
    var fileSource2 = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AppointmentConfirmedAudio));
    var fileSource3 = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AppointmentCancelledAudio));
    var playOptions1 = new PlayToAllOptions(fileSource1)
    {
        OperationContext = "PlayAudio",
        Loop = true,
    };

    var playOptions2 = new PlayToAllOptions(fileSource2)
    {
        OperationContext = "PlayAudio",
        Loop = true,
    };
    var playOptions3 = new PlayToAllOptions(fileSource3)
    {
        OperationContext = "PlayAudio",
        Loop = true,
    };

    var result1 = await callConnectionMedia.PlayToAllAsync(playOptions1);
    //var result2=  callConnectionMedia.PlayToAllAsync(playOptions2);
    //var result3 =  callConnectionMedia.PlayToAllAsync(playOptions3);
});


//Cancel media
app.MapPost("/api/cancelmedia", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{
    var callConnection = callAutomationClient.GetCallConnection(callConnectionId);
    var callConnectionMedia = callConnection.GetCallMedia();
    await callConnectionMedia.CancelAllMediaOperationsAsync().ConfigureAwait(false);
});

//accept DTMF from caller
app.MapPost("/api/recognizeOptionsPhone", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration,
       [Required] string callerId,
          ILogger<Program> logger) =>
{
    var callConnection = callAutomationClient.GetCallConnection(callConnectionId);
    var callConnectionMedia = callConnection.GetCallMedia();
    var targetUser = new PhoneNumberIdentifier(callerId);
    var recognizeOptions = new CallMediaRecognizeDtmfOptions(targetUser, maxTonesToCollect: 1)
    {
        InterruptPrompt = true,
        InterToneTimeout = TimeSpan.FromSeconds(10),
        InitialSilenceTimeout = TimeSpan.FromSeconds(10),
        Prompt = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AppointmentReminderMenuAudio))
        {
            PlaySourceId = "123456789"
        },
        OperationContext = "MainMenu"
    };
    //await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
    callConnectionMedia.StartRecognizingAsync(recognizeOptions);
    return Results.Ok();
});

//accept DTMF from caller
app.MapPost("/api/recognizeOptionsACSIdentity", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration,
       [Required] string callerId,
          ILogger<Program> logger) =>
{
    var callConnection = callAutomationClient.GetCallConnection(callConnectionId);
    var callConnectionMedia = callConnection.GetCallMedia();
    var targetUser = new CommunicationUserIdentifier(callerId);
    var recognizeOptions = new CallMediaRecognizeDtmfOptions(targetUser, maxTonesToCollect: 1)
    {
        InterruptPrompt = true,
        InterToneTimeout = TimeSpan.FromSeconds(10),
        InitialSilenceTimeout = TimeSpan.FromSeconds(10),
        Prompt = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AppointmentReminderMenuAudio))
        {
            PlaySourceId = "123456789"
        },
        OperationContext = "MainMenu"
    };
    await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
    return Results.Ok();
});

//hangup call
app.MapPost("/api/hangup", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{
    var callConnection = callAutomationClient.GetCallConnection(callConnectionId);
    await callConnection.HangUpAsync(forEveryone: true);
});

//api to handle call back events
app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(cloudEvent)}");

        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        callConnectionId = @event.CallConnectionId;
        var callConnection = callAutomationClient.GetCallConnection(@event.CallConnectionId);
        var callConnectionMedia = callConnection.GetCallMedia();
        /*if (@event is CallConnected)
        {
            //Initiate recognition as call connected event is received
            logger.LogInformation($"CallConnected event received for call connection id: {@event.CallConnectionId}");
            var recognizeOptions =
            //new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(callConfiguration.Value.TargetPhoneNumber), maxTonesToCollect: 1)
            new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(callConfiguration.Value.AcsUser1), maxTonesToCollect: 1)
            {
                InterruptPrompt = true,
                InterToneTimeout = TimeSpan.FromSeconds(10),
                InitialSilenceTimeout = TimeSpan.FromSeconds(5),
                Prompt = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AppointmentReminderMenuAudio)),
                OperationContext = "AppointmentReminderMenu"
            };

            //Start recognition 
            await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
        }*/

        //1:1 ACS PSTN to ACS user identity
        /* if(@event is CallConnected)
         {
             logger.LogInformation($"CallConnected event received for call connection id: {@event.CallConnectionId}");
             //playtoall

             var playOptions1 = new PlayOptions()
             {
                 OperationContext = "PlayAudio",
                 Loop = true,
             };
             var fileSource1 = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AppointmentConfirmedAudio));
             var fileSource2 = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.InvalidInputAudio));
             var fileSource3 = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.TimedoutAudio));
             var playResponse1 = callConnectionMedia.PlayToAllAsync(fileSource1, playOptions1);
             var playResponse2 = callConnectionMedia.PlayToAllAsync(fileSource2, playOptions1);
             var playResponse3 = callConnectionMedia.PlayToAllAsync(fileSource3, playOptions1);
         }*/
        /*if (@event is PlayCompleted)
        {
            var fileSource1 = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + "AppointmentCancelledAudio.wav"));
            var playResponse1 = await callConnectionMedia.PlayToAllAsync(fileSource1);
        }*/



        if (@event is RecognizeCompleted { OperationContext: "AppointmentReminderMenu" })
        {
            // Play audio once recognition is completed sucessfully
            logger.LogInformation($"RecognizeCompleted event received for call connection id: {@event.CallConnectionId}");
            var recognizeCompletedEvent = (RecognizeCompleted)@event;

            string labelDetected = null;
            string phraseDetected = null;
            DtmfTone toneDetected = default;

            switch (recognizeCompletedEvent.RecognizeResult)
            {
                // Take action for Recongition through Choices
                case ChoiceResult choiceResult:
                    labelDetected = choiceResult.Label;
                    phraseDetected = choiceResult.RecognizedPhrase;
                    //If choice is detected by phrase, choiceResult.RecognizedPhrase will have the phrase detected,
                    // if choice is detected using dtmf tone, phrase will be null
                    break;
                //Take action for Recongition through DTMF
                case CollectTonesResult collectTonesResult:
                    var tone = collectTonesResult.Tones[0];
                    toneDetected = tone;
                    break;

                default:
                    logger.LogError($"Unexpected recognize event result identified for connection id: {@event.CallConnectionId}");
                    break;
            }


            var playSource = Utils.GetAudioForTone(toneDetected, callConfiguration);

            // Play audio for dtmf response
            await callConnectionMedia.PlayToAllAsync(new PlayToAllOptions(playSource) { OperationContext = "ResponseToDtmf", Loop = false });
        }
        if (@event is RecognizeFailed { OperationContext: "AppointmentReminderMenu" })
        {
            logger.LogInformation($"RecognizeFailed event received for call connection id: {@event.CallConnectionId}");
            var recognizeFailedEvent = (RecognizeFailed)@event;

            // Check for time out, and then play audio message
            if (recognizeFailedEvent.ReasonCode.Equals("RecognizeInitialSilenceTimedOut"))
            {
                logger.LogInformation($"Recognition timed out for call connection id: {@event.CallConnectionId}");
                var playSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.TimedoutAudio));

                //Play audio for time out
                await callConnectionMedia.PlayToAllAsync(new PlayToAllOptions(playSource) { OperationContext = "ResponseToDtmf", Loop = false });
            }
        }


        /* if (@event is PlayCompleted { OperationContext: "ResponseToDtmf" })
         {
             logger.LogInformation($"PlayCompleted event received for call connection id: {@event.CallConnectionId}");
             await callConnection.HangUpAsync(forEveryone: true);
         }
         if (@event is PlayFailed { OperationContext: "ResponseToDtmf" })
         {
             logger.LogInformation($"PlayFailed event received for call connection id: {@event.CallConnectionId}");
             await callConnection.HangUpAsync(forEveryone: true);
         }*/
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
           Path.Combine(builder.Environment.ContentRootPath, "audio")),
    RequestPath = "/audio"
});

app.UseHttpsRedirection();
app.Run();
