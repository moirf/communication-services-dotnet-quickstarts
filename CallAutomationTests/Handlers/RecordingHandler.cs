using Azure.Messaging.EventGrid;
using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;

namespace CallAutomation.Scenarios
{
    public class RecordingHandler :
        IEventActionEventHandler<StartRecordingEvent>, IEventActionEventHandler<StopRecordingEvent>, IEventActionEventHandler<GetRecordingStateEvent>,
         IEventActionEventHandler<PauseRecordingEvent>, IEventActionEventHandler<ResumeRecordingEvent>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CallEventHandler> _logger;
        private readonly ICallAutomationService _callAutomationService;
        private readonly ICallContextService _callContextService;
        static Dictionary<string, string> recordingData = new Dictionary<string, string>();
        public static string recFileFormat;
        public RecordingHandler(
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

        public async Task Handle(StartRecordingEvent startRecordingEvent)
        {
            try
            {
                var serverCallId = startRecordingEvent.serverCallId ?? throw new ArgumentNullException($"ServerCallId is null: {startRecordingEvent}");

                var startRecordingResponse = await _callAutomationService.StartRecordingAsync(serverCallId);
                Logger.LogInformation($"StartRecordingAsync response -- >  {startRecordingResponse.RecordingState}, Recording Id: {startRecordingResponse.RecordingId}");
                var recordingId = startRecordingResponse.RecordingId;

                _callContextService.SetRecordingContext(serverCallId, new RecordingContext() { StartTime = DateTime.UtcNow, RecordingId = recordingId });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Start Recording failed unexpectedly");
                throw;
            }
        }

        public async Task Handle(StopRecordingEvent StopRecordingEvent)
        {
            try
            {
                _logger.LogInformation("StopRecordingAsync received");
                string serverCallId = StopRecordingEvent.serverCallId;
                string recordingId = StopRecordingEvent.recordingId;
                if (!string.IsNullOrEmpty(serverCallId))
                {
                    if (string.IsNullOrEmpty(recordingId))
                    {
                        recordingId = recordingData[serverCallId];
                    }
                    else
                    {
                        if (!recordingData.ContainsKey(serverCallId))
                        {
                            recordingData[serverCallId] = recordingId;
                        }
                    }

                    var stopRecording = await _callAutomationService.StopRecordingAsync(recordingId);
                    Logger.LogInformation($"StopRecordingAsync response -- > {stopRecording}");
                    if (recordingData.ContainsKey(serverCallId))
                    {
                        recordingData.Remove(serverCallId);
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stop Recording failed unexpectedly");
                throw;
            }
        }

        public async Task Handle(PauseRecordingEvent pauseRecordingEvent)
        {
            try
            {
                _logger.LogInformation("pauseRecordingEvent received");
                string serverCallId = pauseRecordingEvent.serverCallId;
                string recordingId = pauseRecordingEvent.recordingId;
                if (!string.IsNullOrEmpty(serverCallId))
                {
                    if (string.IsNullOrEmpty(recordingId))
                    {
                        recordingId = recordingData[serverCallId];
                    }
                    else
                    {
                        if (!recordingData.ContainsKey(serverCallId))
                        {
                            recordingData[serverCallId] = recordingId;
                        }
                    }

                    var PauseRecording = await _callAutomationService.PauseRecordingAsync(recordingId);
                    Logger.LogInformation($"PauseRecordingAsync response -- > {PauseRecording}");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pause Recording failed unexpectedly");
                throw;
            }
        }
        public async Task Handle(ResumeRecordingEvent resumeRecordingEvent)
        {
            try
            {
                _logger.LogInformation("ResumeRecordingEvent received");
                string serverCallId = resumeRecordingEvent.serverCallId;
                string recordingId = resumeRecordingEvent.recordingId;
                if (!string.IsNullOrEmpty(serverCallId))
                {
                    if (string.IsNullOrEmpty(recordingId))
                    {
                        recordingId = recordingData[serverCallId];
                    }
                    else
                    {
                        if (!recordingData.ContainsKey(serverCallId))
                        {
                            recordingData[serverCallId] = recordingId;
                        }
                    }

                    var ResumeRecording = await _callAutomationService.ResumeRecordingAsync(recordingId);
                    Logger.LogInformation($"ResumeRecordingAsync response -- > {ResumeRecording}");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resume Recording failed unexpectedly");
                throw;
            }
        }

        public async Task Handle(GetRecordingStateEvent getRecordingStateEvent)
        {
            try
            {
                _logger.LogInformation("GetRecordingStateEvent received");
                string serverCallId = getRecordingStateEvent.serverCallId;
                string recordingId = getRecordingStateEvent.recordingId;
                if (!string.IsNullOrEmpty(serverCallId))
                {
                    if (string.IsNullOrEmpty(recordingId))
                    {
                        recordingId = recordingData[serverCallId];
                    }
                    else
                    {
                        if (!recordingData.ContainsKey(serverCallId))
                        {
                            recordingData[serverCallId] = recordingId;
                        }
                    }

                    var PauseRecording = await _callAutomationService.GetRecordingStateAsync(recordingId);
                    Logger.LogInformation($"PauseRecordingAsync response -- > {PauseRecording}");

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Recording state event failed unexpectedly");
                throw;
            }
        }


    }
}
