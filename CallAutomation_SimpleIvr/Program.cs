using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var callConnectionId = "";
var client = new CallAutomationClient(builder.Configuration["ConnectionString"]);
var baseUri = Environment.GetEnvironmentVariable("VS_TUNNEL_URL")?.TrimEnd('/');
if (string.IsNullOrEmpty(baseUri))
{
    baseUri = builder.Configuration["BaseUri"];
}

var app = builder.Build();
app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        logger.LogInformation($"Incoming Call event received : {JsonConvert.SerializeObject(eventGridEvent)}");
        // Handle system events
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the subscription validation event.
            if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }
        }
        var jsonObject = JsonNode.Parse(eventGridEvent.Data).AsObject();
        var callerId = (string)(jsonObject["from"]["rawId"]);
        var ToCallerID = (string)jsonObject["to"]["phoneNumber"]["value"];
        var incomingCallContext = (string)jsonObject["incomingCallContext"];
        var callbackUri = new Uri(baseUri + $"/api/calls/{Guid.NewGuid()}?callerId={callerId}");

        //Alternate way to redirect call using redirectcalloptions
        //client.RedirectCallAsync(new RedirectCallOptions(incomingCallContext, new CallInvite(new CommunicationUserIdentifier("+919175180707"))));

        // redirect call to a phone number
        //await client.RedirectCallAsync(incomingCallContext, new CallInvite(new PhoneNumberIdentifier("+917972400258"),new PhoneNumberIdentifier("+14352752486")));
        // beta version --            (incomingCallContext, new PhoneNumberIdentifier("+9175180707")); //this can be any phone number you have access to and should be provided in format +(countrycode)(phonenumber)

        //redirect to ACS identity
        //await client.RedirectCallAsync(incomingCallContext, new CallInvite(new CommunicationUserIdentifier("8:acs:4fecba10-f581-4e33-baf5-a2b7ed2ff6f8_00000018-c3c5-50ab-6a0b-343a0d00939d")));


        //redirect call to tollfree no.
        /*if (ToCallerID == "+18662318150")
        {
            AnswerCallResult answerCallResult = await client.AnswerCallAsync(incomingCallContext, callbackUri);
        }
        else
        {
            await client.RedirectCallAsync(incomingCallContext, new CallInvite(new PhoneNumberIdentifier("+18662318150"), new PhoneNumberIdentifier("+14352752486")));
        }*/
        AnswerCallResult answerCallResult = await client.AnswerCallAsync(incomingCallContext, callbackUri);
    }
    return Results.Ok();
});

//cancel play audio
app.MapPost("/api/cancelPlayAudio", async (
       [FromBody] EventGridEvent[] eventGridEvents,
          ILogger<Program> logger) =>
{
    var callConnection = client.GetCallConnection(callConnectionId);
    var callConnectionMedia = callConnection.GetCallMedia();
    callConnectionMedia.CancelAllMediaOperationsAsync().ConfigureAwait(false);

    return Results.Ok();
});

//play audio to all participants
app.MapPost("/api/playaudioall", async (ILogger<Program> logger) =>
{
    var callConnection = client.GetCallConnection(callConnectionId);
    var callConnectionMedia = callConnection.GetCallMedia();
    var fileSource = new FileSource(new Uri(baseUri + builder.Configuration["MainMenuAudio"]));
    var playOptions = new PlayToAllOptions(fileSource)
    {
        OperationContext = "PlayAudio",
        Loop = true,
    };
    //var fileSource1 = new FileSource(new Uri(baseUri + builder.Configuration["MarketingAudio"]));
    var result = await callConnectionMedia.PlayToAllAsync(playOptions);
    //var result = callConnectionMedia.PlayToAllAsync(fileSource, playOptions);
    //var result1 = callConnectionMedia.PlayToAllAsync(fileSource, playOptions);
});

// play audio to phone number
app.MapPost("/api/playAudio", async (
   [Required] string callerId,
   //[Required] object loop,
   ILogger<Program> logger) =>
{
    var callConnection = client.GetCallConnection(callConnectionId);
    var callConnectionMedia = callConnection.GetCallMedia();
    var targetUser = new PhoneNumberIdentifier(callerId);

    var playOptions = new PlayOptions(new FileSource(new Uri(baseUri + builder.Configuration["MarketingAudio"])), new PhoneNumberIdentifier[] { targetUser })
    {
        OperationContext = "PlayAudio",
        Loop = false,
    };

    await callConnectionMedia.PlayAsync(playOptions);
    return Results.Ok();
});


// play audio to acs user
app.MapPost("/api/ACSplayAudio", async (
   [Required] string callerId,
   //[Required] object loop,
   ILogger<Program> logger) =>
{
    var callConnection = client.GetCallConnection(callConnectionId);
    var callConnectionMedia = callConnection.GetCallMedia();
    var targetUser = new CommunicationUserIdentifier(callerId);

    var playOptions = new PlayOptions(new FileSource(new Uri(baseUri + builder.Configuration["MainMenuAudio"])), new CommunicationUserIdentifier[] { targetUser })
    {
        OperationContext = "PlayAudio",
        Loop = false,
    };

    callConnectionMedia.PlayAsync(playOptions);
    return Results.Ok();
});

//add participant to call
app.MapPost("/api/addParticipant", async (
    [Required] string addCallerID,
            ILogger<Program> logger) =>

{
    CallInvite callInvite = new CallInvite(new PhoneNumberIdentifier(addCallerID), new PhoneNumberIdentifier("+14352752486"));        //new CommunicationUserIdentifier("8:acs:4fecba10-f581-4e33-baf5-a2b7ed2ff6f8_00000017-cc2a-0923-f883-0848220089f1"));;
    AddParticipantResult addParticipantResult = await client.GetCallConnection(callConnectionId).AddParticipantAsync(callInvite);

    return Results.Ok();
});

//add participant to call
app.MapPost("/api/addACSParticipant", async (
    [Required] string addCallerID,
            ILogger<Program> logger) =>

{
    CallInvite callInvite = new CallInvite(new CommunicationUserIdentifier(addCallerID));        //new CommunicationUserIdentifier("8:acs:4fecba10-f581-4e33-baf5-a2b7ed2ff6f8_00000017-cc2a-0923-f883-0848220089f1"));;
    AddParticipantResult addParticipantResult = await client.GetCallConnection(callConnectionId).AddParticipantAsync(callInvite);

    return Results.Ok();
});

// recognize options for speech
app.MapPost("/api/recognizeOptions", async (
       [Required] string callerId,
          ILogger<Program> logger) =>
{
    var callConnection = client.GetCallConnection(callConnectionId);
    var callConnectionMedia = callConnection.GetCallMedia();
    var targetUser = new PhoneNumberIdentifier(callerId);
    var recognizeOptions = new CallMediaRecognizeDtmfOptions(targetUser, maxTonesToCollect: 1)
    {
        InterruptPrompt = true,
        InterToneTimeout = TimeSpan.FromSeconds(10),
        InitialSilenceTimeout = TimeSpan.FromSeconds(10),
        Prompt = new FileSource(new Uri(baseUri + builder.Configuration["MainMenuAudio"]))
        {
            PlaySourceId = "123456789"
        },
        OperationContext = "MainMenu"
    };
    await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
    return Results.Ok();
});

// recognize options for ACS
app.MapPost("/api/ACSrecognizeOptions", async (
       [Required] string callerId,
          ILogger<Program> logger) =>
{
    var callConnection = client.GetCallConnection(callConnectionId);
    var callConnectionMedia = callConnection.GetCallMedia();
    var targetUser = new CommunicationUserIdentifier(callerId);
    var recognizeOptions = new CallMediaRecognizeDtmfOptions(targetUser, maxTonesToCollect: 1)
    {
        InterruptPrompt = true,
        InterToneTimeout = TimeSpan.FromSeconds(10),
        InitialSilenceTimeout = TimeSpan.FromSeconds(10),
        Prompt = new FileSource(new Uri(baseUri + builder.Configuration["MainMenuAudio"]))
        {
            PlaySourceId = "123456789"
        },
        OperationContext = "MainMenu"
    };
    await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
    return Results.Ok();
});

// transfer call pstn
app.MapPost("/api/transfer", async (
    [Required] string callerId,
    ILogger<Program> logger) =>
{
    //var transferDestination = new CallInvite(new PhoneNumberIdentifier("+917972400258"), new PhoneNumberIdentifier("+18662318150"));
    var transferOption = new TransferToParticipantOptions(new PhoneNumberIdentifier("+917972400258"));
    var callConnection = client.GetCallConnection(callConnectionId);
    var result = await callConnection.TransferCallToParticipantAsync(transferOption);
});

//transfer call to acs user
app.MapPost("/api/transferacsuser", async (
    [Required] string callerId, ILogger<Program> logger) =>
{
    var transferOption = new TransferToParticipantOptions(new CommunicationUserIdentifier("8:acs:4fecba10-f581-4e33-baf5-a2b7ed2ff6f8_00000018-c3c5-50ab-6a0b-343a0d00939d"));
    var callConnection = client.GetCallConnection(callConnectionId);
    var result = await callConnection.TransferCallToParticipantAsync(transferOption);

});

//hangup call
app.MapPost("/api/hangup", async (ILogger<Program> logger) =>
{
    var callConnection = client.GetCallConnection(callConnectionId);
    await callConnection.HangUpAsync(forEveryone: true);
});

app.MapPost("/api/calls/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    ILogger<Program> logger) =>
{
    var targetUser = new PhoneNumberIdentifier(callerId);
    var audioPlayOptionsMarketing = new PlayToAllOptions(new FileSource(new Uri(baseUri + builder.Configuration["MarketingAudio"])))
    {
        OperationContext = "SimpleIVR",
        Loop = true
    };
    var audioPlayOptionsSales = new PlayToAllOptions(new FileSource(new Uri(baseUri + builder.Configuration["SalesAudio"])))
    {
        OperationContext = "SimpleIVR",
        Loop = true
    };
    var audioPlayOptionscustomerCareAudio = new PlayToAllOptions(new FileSource(new Uri(baseUri + builder.Configuration["CustomerCareAudio"])))
    {
        OperationContext = "SimpleIVR",
        Loop = true
    };
    var audioPlayOptionsagentAudio = new PlayToAllOptions(new FileSource(new Uri(baseUri + builder.Configuration["AgentAudio"])))
    {
        OperationContext = "AgentConnect",
        Loop = true
    };
    var audioPlayOptionsinvalidAudio = new PlayToAllOptions(new FileSource(new Uri(baseUri + builder.Configuration["InvalidAudio"])))
    {
        OperationContext = "AgentConnect",
        Loop = true
    };

    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(@event)}");
        callConnectionId = @event.CallConnectionId;
        var callConnection = client.GetCallConnection(@event.CallConnectionId);
        var callMedia = callConnection?.GetCallMedia();

        if (callConnection == null || callMedia == null)
        {
            return Results.BadRequest($"Call objects failed to get for connection id {@event.CallConnectionId}.");
        }

        /*if (@event is CallConnected)
        {
            CallInvite callInvite = new CallInvite(new PhoneNumberIdentifier("+18662318150"), new PhoneNumberIdentifier("+14352752486"));        //new CommunicationUserIdentifier("8:acs:4fecba10-f581-4e33-baf5-a2b7ed2ff6f8_00000017-cc2a-0923-f883-0848220089f1"));;
            AddParticipantResult addParticipantResult = await client.GetCallConnection(callConnectionId).AddParticipantAsync(callInvite);
        }*/

        //callerId = "4:+18185386878";
        //List<CallParticipant> participantList = (await callConnection.GetParticipantsAsync()).Value.ToList();
        //logger.LogInformation($"Participants list: {participantList}");
        /* if (@event is CallConnected)
         {
             // Start recognize prompt - play audio and recognize 1-digit DTMF input
             PlaySource Prompt = null;
             var recognizeOptions =
                 new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: 1)
                 {
                     InterruptPrompt = true,
                     InterToneTimeout = TimeSpan.FromSeconds(10),
                     InitialSilenceTimeout = TimeSpan.FromSeconds(10),
                     Prompt = new FileSource(new Uri(baseUri + builder.Configuration["MainMenuAudio"]))
                     {
                         PlaySourceId = "123456789"
                     },
                     OperationContext = "MainMenu"
                 };

             await callMedia.StartRecognizingAsync(recognizeOptions);

         }*/

        //###################### PlayToAll group & 1:1 ######################
        if (@event is CallConnected)
        {
            /* //#### Add participant to call 1:1 call to group call ####
             CallInvite callInvite = new CallInvite(new PhoneNumberIdentifier("+917972400258"), new PhoneNumberIdentifier("+14352752486"));        //new CommunicationUserIdentifier("8:acs:4fecba10-f581-4e33-baf5-a2b7ed2ff6f8_00000017-cc2a-0923-f883-0848220089f1"));;
             AddParticipantResult addParticipantResult = await client.GetCallConnection(callConnectionId).AddParticipantAsync(callInvite);*/

            var fileSource = new FileSource(new Uri(baseUri + builder.Configuration["CustomerCareAudio"]));
            List<CallParticipant> participantList = (await callConnection.GetParticipantsAsync()).Value.ToList();
            logger.LogInformation($"Participants list: {participantList}");
            var playResponse = await callMedia.PlayToAllAsync(fileSource);
        }
        /*if (@event is PlayCompleted)
        {
            var fileSource1 = new FileSource(new Uri(baseUri + builder.Configuration["MarketingAudio"]));
            var playResponse1 = await callMedia.PlayToAllAsync(fileSource1);
        }*/


        //################## PlatToTarget user group & 1:1 #################
        /* if (@event is CallConnected)
         {
             //CallInvite callInvite = new CallInvite(new PhoneNumberIdentifier("+919175180707"), new PhoneNumberIdentifier("+14352752486"));        //new CommunicationUserIdentifier("8:acs:4fecba10-f581-4e33-baf5-a2b7ed2ff6f8_00000017-cc2a-0923-f883-0848220089f1"));;
             //AddParticipantResult addParticipantResult = await client.GetCallConnection(callConnectionId).AddParticipantAsync(callInvite);

             var targetUser = new PhoneNumberIdentifier("+18185386878");

             var fileSource = new FileSource(new Uri(baseUri + builder.Configuration["CustomerCareAudio"]));
             var playResponse = await callMedia.PlayAsync(fileSource, new PhoneNumberIdentifier[] { targetUser });

             //var playResponse1 = await callMedia.PlayAsync(fileSource, new PhoneNumberIdentifier[] { new PhoneNumberIdentifier("+919175180707") });

         }*/
        if (@event is PlayCompleted)
        {
            var fileSource1 = new FileSource(new Uri(baseUri + builder.Configuration["MarketingAudio"]));
            var targetUser1 = new PhoneNumberIdentifier(callerId);
            var playResponse1 = await callMedia.PlayAsync(fileSource1, new PhoneNumberIdentifier[] { targetUser1 });
            /*var playResponse2 = await callMedia.PlayAsync(fileSource1, new PhoneNumberIdentifier[] { new PhoneNumberIdentifier("+917972400258") });

            var targetuser2 = new CommunicationUserIdentifier(callerId);
            var playResponse3 = await callMedia.PlayAsync(fileSource1, new CommunicationIdentifier[] { targetuser2 });*/
        }

        if (@event is RecognizeCompleted { OperationContext: "MainMenu" })
        {
            var recognizeCompleted = (RecognizeCompleted)@event;

            string labelDetected = null;
            string phraseDetected = null;
            DtmfTone toneDetected = default;

            switch (recognizeCompleted.RecognizeResult)
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

            if (toneDetected == DtmfTone.One)
            {
                PlaySource salesAudio = new FileSource(new Uri(baseUri + builder.Configuration["SalesAudio"]));
                await callMedia.PlayToAllAsync(audioPlayOptionsSales);
                var playResponse = await callMedia.PlayAsync(salesAudio, new PhoneNumberIdentifier[] { targetUser });
            }
            else if (toneDetected == DtmfTone.Two)
            {
                PlaySource marketingAudio = new FileSource(new Uri(baseUri + builder.Configuration["MarketingAudio"]));
                await callMedia.PlayToAllAsync(audioPlayOptionsMarketing);
            }
            else if (toneDetected == DtmfTone.Three)
            {
                PlaySource customerCareAudio = new FileSource(new Uri(baseUri + builder.Configuration["CustomerCareAudio"]));
                await callMedia.PlayToAllAsync(audioPlayOptionscustomerCareAudio);
            }
            else if (toneDetected == DtmfTone.Four)
            {
                PlaySource agentAudio = new FileSource(new Uri(baseUri + builder.Configuration["AgentAudio"]));
                await callMedia.PlayToAllAsync(audioPlayOptionsagentAudio);
            }
            else if (toneDetected == DtmfTone.Five)
            {
                // Hangup for everyone
                await callConnection.HangUpAsync(true);
            }
            else
            {
                PlaySource invalidAudio = new FileSource(new Uri(baseUri + builder.Configuration["InvalidAudio"]));
                await callMedia.PlayToAllAsync(audioPlayOptionsinvalidAudio);
            }
        }
        if (@event is RecognizeFailed { OperationContext: "MainMenu" })
        {
            // play invalid audio
            await callMedia.PlayToAllAsync(audioPlayOptionsinvalidAudio);
        }
        /*if (@event is PlayCompleted)
        {
            if (@event.OperationContext == "AgentConnect")
            {
                var addParticipantOptions = new AddParticipantOptions(new CallInvite(new PhoneNumberIdentifier(builder.Configuration["ParticipantToAdd"]), new PhoneNumberIdentifier(builder.Configuration["ACSAlternatePhoneNumber"])));
              *//*  {
                    new PhoneNumberIdentifier(builder.Configuration["ParticipantToAdd"])
                });*/

        /*addParticipantOptions.SourceCallerId = new PhoneNumberIdentifier(builder.Configuration["ACSAlternatePhoneNumber"]);*//*
        await callConnection.AddParticipantAsync(addParticipantOptions);
    }
    if (@event.OperationContext == "SimpleIVR")
    {
        await callConnection.HangUpAsync(true);
    }
}*/
        /* if (@event is PlayFailed)
         {
             logger.LogInformation($"PlayFailed Event: {JsonConvert.SerializeObject(@event)}");
             await callConnection.HangUpAsync(true);
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

app.UseAuthorization();

app.MapControllers();

app.Run();
