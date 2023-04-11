using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using CallAutomation_AppointmentReminder;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Fetch configuration and add call automation as singleton service
var callConfigurationSection = builder.Configuration.GetSection(nameof(CallConfiguration));
builder.Services.Configure<CallConfiguration>(callConfigurationSection);
builder.Services.AddSingleton(new CallAutomationClient(callConfigurationSection["ConnectionString"]));

var app = builder.Build();

var sourceIdentity = await app.ProvisionAzureCommunicationServicesIdentity(callConfigurationSection["ConnectionString"]);
TaskCompletionSource<bool> callEstablishedTask = null;

// Api to initiate out bound call
app.MapPost("/api/call", async (CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{
    callEstablishedTask = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously); ;

    var target = new PhoneNumberIdentifier(callConfiguration.Value.TargetPhoneNumber);
    var source = new CallInvite(target, new PhoneNumberIdentifier(callConfiguration.Value.SourcePhoneNumber));
    var createCallOption = new CreateCallOptions(source, new Uri(callConfiguration.Value.CallbackEventUri));

    var response = await callAutomationClient.CreateCallAsync(createCallOption).ConfigureAwait(false);
    logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" +
        $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}");

    var callconnectionID = response.Value.CallConnection.CallConnectionId;
    //Wait for operation to complete
    var callEstablished = await callEstablishedTask.Task.ConfigureAwait(false);
    if (callEstablished)
    {
        var callConnection = callAutomationClient.GetCallConnection(callconnectionID);
        logger.LogInformation($"server call ID ---> {response.Value.CallConnectionProperties.ServerCallId}");
        return Results.Json(callConnection.GetCallConnectionProperties().Value.ServerCallId);
    }
    else
    {
        return Results.BadRequest(new { Message = "Call disconnected unexpectedly" });
    }
});

//api to handle call back events
app.MapPost("/api/callbacks", async (CloudEvent[] cloudEvents, CallAutomationClient callAutomationClient, IOptions<CallConfiguration> callConfiguration, ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(cloudEvent)}");

        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        var callConnection = callAutomationClient.GetCallConnection(@event.CallConnectionId);
        var callConnectionMedia = callConnection.GetCallMedia();
        if (@event is CallConnected obj)
        {
            callEstablishedTask.TrySetResult(true);
            //Initiate recognition as call connected event is received
            logger.LogInformation($"CallConnected event received for call connection id: {@event.CallConnectionId}");
            var recognizeOptions =
            new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(callConfiguration.Value.TargetPhoneNumber), maxTonesToCollect: 1)
            {
                InterruptPrompt = true,
                InterToneTimeout = TimeSpan.FromSeconds(10),
                InitialSilenceTimeout = TimeSpan.FromSeconds(5),
                Prompt = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.AppointmentReminderMenuAudio)),
                OperationContext = "AppointmentReminderMenu"
            };

            //Start recognition 
            await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
        }
        if (@event is RecognizeCompleted { OperationContext: "AppointmentReminderMenu" })
        {
            // Play audio once recognition is completed sucessfully
            logger.LogInformation($"RecognizeCompleted event received for call connection id: {@event.CallConnectionId}");
            var recognizeCompletedEvent = (RecognizeCompleted)@event;
            var toneDetected = ((CollectTonesResult)recognizeCompletedEvent.RecognizeResult).Tones[0]; ;
            var playSource = Utils.GetAudioForTone(toneDetected, callConfiguration);

            // Play audio for dtmf response
            await callConnectionMedia.PlayToAllAsync(playSource, new PlayOptions { OperationContext = "ResponseToDtmf", Loop = false });
        }
        if (@event is RecognizeFailed { OperationContext: "AppointmentReminderMenu" })
        {
            logger.LogInformation($"RecognizeFailed event received for call connection id: {@event.CallConnectionId}");
            var recognizeFailedEvent = (RecognizeFailed)@event;

            // Check for time out, and then play audio message
            if (recognizeFailedEvent.ReasonCode.Equals(ReasonCode.RecognizeInitialSilenceTimedOut))
            {
                logger.LogInformation($"Recognition timed out for call connection id: {@event.CallConnectionId}");
                var playSource = new FileSource(new Uri(callConfiguration.Value.AppBaseUri + callConfiguration.Value.TimedoutAudio));

                //Play audio for time out
                await callConnectionMedia.PlayToAllAsync(playSource, new PlayOptions { OperationContext = "ResponseToDtmf", Loop = false });
            }
        }
        if (@event is PlayCompleted { OperationContext: "ResponseToDtmf" })
        {
            logger.LogInformation($"PlayCompleted event received for call connection id: {@event.CallConnectionId}");
            await callConnection.HangUpAsync(forEveryone: true);
        }
        if (@event is PlayFailed { OperationContext: "ResponseToDtmf" })
        {
            logger.LogInformation($"PlayFailed event received for call connection id: {@event.CallConnectionId}");
            await callConnection.HangUpAsync(forEveryone: true);
        }
        if (@event is CallDisconnected)
        {
            logger.LogInformation($"Call disconnected");
            callEstablishedTask.TrySetResult(false);
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

app.UseHttpsRedirection();
app.Run();
