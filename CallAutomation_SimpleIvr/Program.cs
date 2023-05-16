using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var client = new CallAutomationClient(builder.Configuration["ConnectionString"]);
var baseUri = Environment.GetEnvironmentVariable("VS_TUNNEL_URL")?.TrimEnd('/');
if (string.IsNullOrEmpty(baseUri))
{
    baseUri = builder.Configuration["BaseUri"];
}
CommunicationIdentifierKind GetIdentifierKind(string participantnumber)
{
    //checks the identity type returns as string
    return Regex.Match(participantnumber, Constants.userIdentityRegex, RegexOptions.IgnoreCase).Success ? CommunicationIdentifierKind.UserIdentity :
 Regex.Match(participantnumber, Constants.phoneIdentityRegex, RegexOptions.IgnoreCase).Success ? CommunicationIdentifierKind.PhoneIdentity :
 CommunicationIdentifierKind.UnknownIdentity;
}
int addedParticipantsCount = 0;
int declineParticipantsCount = 0;
var target = builder.Configuration["ParticipantToAdd"];
string sourceCallerID = null;
var Participants = target.Split(';');
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
        var targetId = (string)(jsonObject["to"]["rawId"]);
        sourceCallerID = (string)(jsonObject["from"]["rawId"]);
        var incomingCallContext = (string)jsonObject["incomingCallContext"];
        var callbackUri = new Uri(baseUri + $"/api/calls/{Guid.NewGuid()}?callerId={sourceCallerID}");

        string caSourceId = builder.Configuration["TargetId"];
        var rejectcall = Convert.ToBoolean(builder.Configuration["declinecall"]);

        if (caSourceId.Contains(targetId))
        {
            if (rejectcall)
            {
                var response = client.RejectCallAsync(incomingCallContext);
                logger.LogInformation($"{response.Result}");
            }
            else
            {
                AnswerCallResult answerCallResult = await client.AnswerCallAsync(incomingCallContext, callbackUri);
                logger.LogInformation($"answerCall Response ------->  source callerId {answerCallResult.CallConnectionProperties.SourceIdentity.RawId}");
                logger.LogInformation($"targets ------->");
                foreach (var target in answerCallResult.CallConnectionProperties.Targets)
                {
                    logger.LogInformation($"{target.RawId}");
                }
            }
        }
    }
    return Results.Ok();
});

app.MapPost("/api/calls/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    ILogger<Program> logger) =>
{
    //var audioPlayOptions = new PlayToAllOptions() { OperationContext = "SimpleIVR", Loop = false };

    if (cloudEvents == null)
    {
        logger.LogWarning("cloudEvents parameter is null.");
        return Results.BadRequest("cloudEvents parameter is null.");
    }

    foreach (var cloudEvent in cloudEvents)
    {
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(cloudEvent)}");
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        if (@event == null)
        {
            logger.LogWarning("cloudEvents param is null");
            continue;
        }
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(@event)}");

        var callConnection = client.GetCallConnection(@event.CallConnectionId);
        var callMedia = callConnection?.GetCallMedia();

        if (callConnection == null || callMedia == null)
        {
            return Results.BadRequest($"Call objects failed to get for connection id {@event.CallConnectionId}.");
        }

        if (@event is CallConnected)
        {
            addedParticipantsCount = 0;
            declineParticipantsCount = 0;

            logger.LogInformation($"CallConnected event received for call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");

            var properties = callConnection.GetCallConnectionProperties();
            logger.LogInformation($"call connection properties -------> SourceIdentity : {properties.Value.SourceIdentity.RawId}," +
                $"CallConnection State : {properties.Value.CallConnectionState}");
            logger.LogInformation($"targets ------->");
            foreach (var target in properties.Value.Targets)
            {
                logger.LogInformation($"{target.RawId}");
            }

            // Start recognize prompt - play audio and recognize 1-digit DTMF input
            var recognizeOptions =
                new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: 1)
                {
                    InterruptPrompt = true,
                    InterToneTimeout = TimeSpan.FromSeconds(10),
                    InitialSilenceTimeout = TimeSpan.FromSeconds(5),
                    Prompt = new FileSource(new Uri(baseUri + builder.Configuration["MainMenuAudio"])),
                    OperationContext = "MainMenu"
                };
            await callMedia.StartRecognizingAsync(recognizeOptions);
        }
        if (@event is RecognizeCompleted { OperationContext: "MainMenu" })
        {
            var recognizeCompleted = (RecognizeCompleted)@event;
            DtmfResult collectedTones = (DtmfResult)recognizeCompleted.RecognizeResult;

            if (collectedTones.Tones[0] == DtmfTone.One)
            {
                PlaySource salesAudio = new FileSource(new Uri(baseUri + builder.Configuration["SalesAudio"]));
                var audioPlayOptions = new PlayToAllOptions(salesAudio) { OperationContext = "SimpleIVR", Loop = false };
                await callMedia.PlayToAllAsync(audioPlayOptions);
            }
            else if (collectedTones.Tones[0] == DtmfTone.Two)
            {
                PlaySource marketingAudio = new FileSource(new Uri(baseUri + builder.Configuration["MarketingAudio"]));
                await callMedia.PlayToAllAsync(new PlayToAllOptions(marketingAudio) 
                { OperationContext = "SimpleIVR", Loop = false });
            }
            else if (collectedTones.Tones[0] == DtmfTone.Three)
            {
                PlaySource customerCareAudio = new FileSource(new Uri(baseUri + builder.Configuration["CustomerCareAudio"]));
                await callMedia.PlayToAllAsync(new PlayToAllOptions(customerCareAudio)
                { OperationContext = "CustomerCare", Loop = false });
            }
            else if (collectedTones.Tones[0] == DtmfTone.Four)
            {
                PlaySource agentAudio = new FileSource(new Uri(baseUri + builder.Configuration["AgentAudio"]));
                await callMedia.PlayToAllAsync(new PlayToAllOptions(agentAudio)
                { OperationContext = "AgentConnect", Loop = false });
            }
            else if (collectedTones.Tones[0] == DtmfTone.Five)
            {
                // Hangup for everyone
                await callConnection.HangUpAsync(true);
                logger.LogInformation($"Call disconnected event received call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
            }
            else
            {
                PlaySource invalidAudio = new FileSource(new Uri(baseUri + builder.Configuration["InvalidAudio"]));
                await callMedia.PlayToAllAsync(new PlayToAllOptions(invalidAudio)
                { OperationContext = "SimpleIVR", Loop = false });
            }
        }
        if (@event is RecognizeFailed { OperationContext: "MainMenu" })
        {
            // play invalid audio
            PlaySource invalidAudio = new FileSource(new Uri(baseUri + builder.Configuration["InvalidAudio"]));
            await callMedia.PlayToAllAsync(new PlayToAllOptions(invalidAudio)
            { OperationContext = "SimpleIVR", Loop = false });
        }
        if (@event is PlayCompleted)
        {
            if (@event.OperationContext == "AgentConnect")
            {
                foreach (var Participantindentity in Participants)
                {
                    var identifierKind = GetIdentifierKind(Participantindentity);
                    CallInvite? callInvite = null;
                    if (!string.IsNullOrEmpty(Participantindentity))
                    {
                        if (identifierKind == CommunicationIdentifierKind.PhoneIdentity)
                        {
                            callInvite = new CallInvite(new PhoneNumberIdentifier(Participantindentity), new PhoneNumberIdentifier(builder.Configuration["ACSAlternatePhoneNumber"]));
                        }
                        if (identifierKind == CommunicationIdentifierKind.UserIdentity)
                        {
                            callInvite = new CallInvite(new CommunicationUserIdentifier(Participantindentity));
                        }
                    }

                    var addParticipantOptions = new AddParticipantOptions(callInvite);
                    var response = await callConnection.AddParticipantAsync(addParticipantOptions);
                    //var playSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AddParticipant));
                    PlaySource agentAudio = new FileSource(new Uri(baseUri + builder.Configuration["AddParticipant"]));
                    await callMedia.PlayToAllAsync(new PlayToAllOptions(agentAudio) { OperationContext = "addParticipant", Loop = false });

                    TimeSpan InterToneTimeout = TimeSpan.FromSeconds(20);
                    TimeSpan InitialSilenceTimeout = TimeSpan.FromSeconds(10);
                    logger.LogInformation($"AddParticipant event received for call connection id: {@event.CallConnectionId}" + $" Correlation id: {@event.CorrelationId}");
                    logger.LogInformation($"Addparticipant call: {response.Value.Participant}" + $"  Addparticipant ID: {Participantindentity}"
                         + $"  get response fron participant : {response.GetRawResponse()}" + $" call reason : {response.GetRawResponse().ReasonPhrase}");
                }
            }
            else if (@event.OperationContext == "CustomerCare")
            {
                var customerCareIdentity = builder.Configuration["customerCareIdentity"];
                var identifierKind = GetIdentifierKind(customerCareIdentity);
                CommunicationIdentifier? callInvite = null;
                if (!string.IsNullOrEmpty(customerCareIdentity))
                {
                    if (identifierKind == CommunicationIdentifierKind.PhoneIdentity)
                    {
                        callInvite = new PhoneNumberIdentifier(customerCareIdentity);
                    }
                    if (identifierKind == CommunicationIdentifierKind.UserIdentity)
                    {
                        callInvite = new CommunicationUserIdentifier(customerCareIdentity);
                    }
                }
                var transferResponse = await callConnection.TransferCallToParticipantAsync(callInvite);
                logger.LogInformation($"Call Transfered to : {customerCareIdentity}");
                logger.LogInformation($"Transfer call result : {transferResponse.GetRawResponse()}");
            }
        }
        if (@event is AddParticipantSucceeded addedParticipant)
        {
            addedParticipantsCount++;
            logger.LogInformation($"participant added ---> {addedParticipant.Participant.RawId}");

            if ((addedParticipantsCount + declineParticipantsCount) == Participants.Length)
            {
                await PerformHangUp(callConnection);
            }
        }
        if (@event is AddParticipantFailed failedParticipant)
        {
            declineParticipantsCount++;
            AddParticipantFailed addParticipantFailed = (AddParticipantFailed)@event;
            logger.LogInformation($"Failed participant Reason -------> {failedParticipant.ResultInformation?.Message}");
            if ((addedParticipantsCount + declineParticipantsCount) == Participants.Length)
            {
                await PerformHangUp(callConnection);
            }
        }
        if (@event is RemoveParticipantSucceeded)
        {
            RemoveParticipantSucceeded RemoveParticipantSucceeded = (RemoveParticipantSucceeded)@event;
            logger.LogInformation($"Remove Participant Succeeded RawId : {RemoveParticipantSucceeded.Participant.RawId}");
        }
        if (@event is RemoveParticipantFailed)
        {
            RemoveParticipantFailed removeParticipantFailed = (RemoveParticipantFailed)@event;
            logger.LogInformation($"Remove participant failed RawId:{removeParticipantFailed.Participant.RawId}");
        }
        if (@event.OperationContext == "SimpleIVR")
        {
            await callConnection.HangUpAsync(true);
        }
        if (@event is PlayFailed)
        {
            logger.LogInformation($"PlayFailed Event: {JsonConvert.SerializeObject(@event)}");
            await callConnection.HangUpAsync(true);
        }
        if (@event is ParticipantsUpdated updatedParticipantEvent)
        {
            logger.LogInformation($"Participant Updated Event Recieved");
            logger.LogInformation("-------Updated Participant List----- ");
            foreach (var participant in updatedParticipantEvent.Participants)
            {
                logger.LogInformation($"Participant Raw ID : {participant.Identifier.RawId},  IsMuted : {participant.IsMuted}");
            }
        }
        if (@event is CallTransferAccepted callTransferAccepted)
        {
            logger.LogInformation($"Transfer call accepted");
        }
        if (@event is CallTransferFailed callTransferFailed)
        {
            logger.LogInformation($"Transfer call Failed ----> {callTransferFailed.ResultInformation.Message}");
        }

        async Task PerformHangUp(CallConnection callConnection)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            var participantlistResponse = await callConnection.GetParticipantsAsync();
            logger.LogInformation("-------Participant List----- ");
            foreach (var participant in participantlistResponse.Value)
            {
                try
                {
                    logger.LogInformation($"{participant.Identifier.RawId}");
                    var response = callConnection.GetParticipant(participant.Identifier);
                    logger.LogInformation($"-------get participnat response  : {response} ----- ");
                }
                catch (Exception ex)
                {
                    logger.LogInformation($"------Error In GetParticipant() for participnat : {participant.Identifier.RawId} " +
                        $"-----> {ex.Message}");
                }
            }

            logger.LogInformation($"Number of Participants : {participantlistResponse.Value.Count}");

            int hangupScenario = Convert.ToInt32(builder.Configuration["HangUpScenarios"]);
            if (hangupScenario == 1)
            {
                logger.LogInformation($"CA hanging up the call for everyone." + $"Information of Call:{callConnection.GetCallConnectionProperties()}");
                var response = await callConnection.HangUpAsync(true);
                logger.LogInformation($"Hang up response : {response}");
            }
            else if (hangupScenario == 2)
            {
                logger.LogInformation($"CA hang up the call." + $"Information of Call:{callConnection.GetCallConnectionProperties()}");
                var response = await callConnection.HangUpAsync(false);
                logger.LogInformation($"Hang up response : {response}");
            }
            else if (hangupScenario == 3 || hangupScenario == 4)
            {
                if (addedParticipantsCount == 0 && hangupScenario == 3)
                {
                    logger.LogInformation($"No participants got addedd to remove");
                }
                else
                {
                    logger.LogInformation($"Going to remove added partipants.");
                    List<CallParticipant> participantsToRemoveAll = (await callConnection.GetParticipantsAsync()).Value.ToList();
                    CommunicationIdentifier sourceParticipant = null;
                    foreach (CallParticipant participantToRemove in participantsToRemoveAll)
                    {
                        if (!string.IsNullOrEmpty(participantToRemove.Identifier.ToString()) &&
                                target.Contains(participantToRemove.Identifier.ToString()) ||
                                (hangupScenario == 4 && participantToRemove.Identifier.RawId.Contains(sourceCallerID)))
                        {
                            if (hangupScenario == 4 && participantToRemove.Identifier.RawId.Contains(sourceCallerID))
                            {
                                sourceParticipant = participantToRemove.Identifier;
                            }
                            else
                            {
                                var RemoveParticipant = new RemoveParticipantOptions(participantToRemove.Identifier);
                                logger.LogInformation($"going to remove participant : {participantToRemove.Identifier.RawId}");
                                var removeParticipantResponse = await callConnection.RemoveParticipantAsync(RemoveParticipant);
                                logger.LogInformation($"Removing participant Response : {removeParticipantResponse.Value.ToString}");
                            }
                        }
                    }
                    if(sourceParticipant != null)
                    {
                        logger.LogInformation($"going to remove participant : {sourceParticipant.RawId}");
                        var removeParticipantResponse = await callConnection.RemoveParticipantAsync(sourceParticipant);
                        logger.LogInformation($"Removing participant Response : {removeParticipantResponse.Value.ToString}");
                    }
                }
            }
        }
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

public enum CommunicationIdentifierKind
{
    PhoneIdentity,
    UserIdentity,
    UnknownIdentity
}
public class Constants
{
    public const string userIdentityRegex = @"8:acs:[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}_[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}";
    public const string phoneIdentityRegex = @"^\+\d{10,14}$";
}
