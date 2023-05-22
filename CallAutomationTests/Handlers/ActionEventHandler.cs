using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;

namespace CallAutomation.Scenarios
{
    public class ActionEventHandler :
        IEventActionEventHandler<RecordingContext>,
        IEventActionEventHandler<OutboundCallContext>
    //IEventActionEventHandler<TelemetryLoggingContext>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CallEventHandler> _logger;
        private readonly ICallAutomationService _callAutomationService;
        private readonly ICallContextService _callContextService;
        //readonly TelemetryClient _telemetryClient;
        public ActionEventHandler(
            IConfiguration configuration,
            ILogger<CallEventHandler> logger,
            ICallAutomationService callAutomationService,
            ICallContextService callContextService,
            //TelemetryClient telemetryClient)

        {
            _configuration = configuration;
            _logger = logger;
            _callAutomationService = callAutomationService;
            _callContextService = callContextService;
            //_telemetryClient = telemetryClient;

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
                //var telemetryLogger = new TelemetryLoggingContext() { ServerCallId = recordingContext.ServerCallId, RecordingId = startRecordingResponse.RecordingId, StartTime = DateTime.UtcNow.ToString() };
                //_callContextService.SetTelemetryLoggingContext(serverCallId, new TelemetryLoggingContext() { RecordingId = startRecordingResponse.RecordingId, StartTime = DateTime.UtcNow.ToString() });
                //TelemetryHandle(serverCallId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Start Recording failed unexpectedly");
                throw;
            }
        }

        //public void TelemetryHandle(string serverCallId)
        //{
        //    TelemetryLoggingContext context = _callContextService.GetTelemetryLoggingContext(serverCallId);
        //    _telemetryClient.TrackEvent(context.EventName, new Dictionary<string, string> { { "ServerCallId", context.ServerCallId }, { "RecordingId", context.RecordingId }, { "actionName", context.ActionName }, { "ClientRequestId", context.ClientRequestId }, { "Status", context.Status }, { "Content", context.Content }, { "ContentStream", context.ContentStream } });
        //}
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
                    await _callAutomationService.PauseRecordingAsync(recordingId);
                    break;
                case "ResumeRecording":
                    await _callAutomationService.ResumeRecordingAsync(recordingId);
                    break;
                case "GetRecordingState":
                    await _callAutomationService.GetRecordingStateAsync(recordingId);
                    break;
                case "StopRecording":
                    await _callAutomationService.StopRecordingAsync(recordingId);
                    break;
            }
        }
    }
}
