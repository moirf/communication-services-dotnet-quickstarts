using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;

namespace CallAutomation.Scenarios
{
    public class ActionEventHandler :
        IEventActionEventHandler<RecordingContext>,
        IEventActionEventHandler<OutboundCallContext>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CallEventHandler> _logger;
        private readonly ICallAutomationService _callAutomationService;
        private readonly ICallContextService _callContextService;
        public TelemetryLogger telemetryLogger = new TelemetryLogger();
        public ActionEventHandler(
            IConfiguration configuration,
            ILogger<CallEventHandler> logger,
            ICallAutomationService callAutomationService,
            ICallContextService callContextService)

        {
            _configuration = configuration;
            _logger = logger;
            _callAutomationService = callAutomationService;
            _callContextService = callContextService;

        }

        public async Task Handle(OutboundCallContext outboundCallContext)
        {
            _logger.LogInformation("Outbound call received");

            try
            {
                var targetId = (outboundCallContext.TargetId ?? _configuration["targetId"]) ?? throw new ArgumentNullException($"Target Id is null : {outboundCallContext}");
                var createCallResult = await _callAutomationService.CreateCallAsync(targetId);
                var callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbound call failed unexpectedly");
                throw;
            }
        }


        public async Task Handle(RecordingContext recordingContext)
        {
            try
            {
                var serverCallId = recordingContext.ServerCallId ?? throw new ArgumentNullException($"ServerCallId is null: {recordingContext}");
                var startRecordingResponse = await _callAutomationService.StartRecordingAsync(serverCallId);
                _callContextService.SetRecordingContext(serverCallId, new RecordingContext() { StartTime = DateTime.UtcNow, RecordingId = startRecordingResponse.RecordingId });
                telemetryLogger.TrackEventHandler(new TelemetryLoggingContext() { EventName = "StartRecording", StartTime = DateTime.UtcNow, ServerCallId = serverCallId, RecordingId = startRecordingResponse.RecordingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Start Recording failed unexpectedly");
                telemetryLogger.TrackExceptionHandler(ex);
                throw;
            }
        }


        public RecordingContext Handle(string serverCallId)
        {
            RecordingContext context = _callContextService.GetRecordingContext(serverCallId);
            return context;
        }

        public async Task Handle(string actionName, string recordingId)
        {
            switch (actionName)
            {
                case "PauseRecording":
                    var pauseRecordingResponse = await _callAutomationService.PauseRecordingAsync(recordingId);
                    telemetryLogger.TrackEventHandler(new TelemetryLoggingContext() { EventName = actionName, StartTime = DateTime.UtcNow, RecordingId = recordingId, ClientRequestId = pauseRecordingResponse.ClientRequestId, Status = pauseRecordingResponse.Status });
                    break;
                case "ResumeRecording":
                    var resumeRecordingResponse = await _callAutomationService.ResumeRecordingAsync(recordingId);
                    telemetryLogger.TrackEventHandler(new TelemetryLoggingContext() { EventName = actionName, StartTime = DateTime.UtcNow, RecordingId = recordingId, Status = resumeRecordingResponse.Status });
                    break;
                case "GetRecordingState":
                    var getRecordingStateResponse = await _callAutomationService.GetRecordingStateAsync(recordingId);
                    telemetryLogger.TrackEventHandler(new TelemetryLoggingContext() { EventName = actionName, StartTime = DateTime.UtcNow, RecordingId = recordingId, RecordingState = getRecordingStateResponse.RecordingState });
                    break;
                case "StopRecording":
                    var stopRecordingStateResponse = await _callAutomationService.StopRecordingAsync(recordingId);
                    telemetryLogger.TrackEventHandler(new TelemetryLoggingContext() { EventName = actionName, StartTime = DateTime.UtcNow, RecordingId = recordingId, ClientRequestId = stopRecordingStateResponse.ClientRequestId, Status = stopRecordingStateResponse.Status });
                    break;
            }
        }

    }
}
