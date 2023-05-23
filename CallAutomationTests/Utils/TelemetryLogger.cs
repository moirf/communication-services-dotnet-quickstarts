using Azure.Communication.CallAutomation;
using Microsoft.ApplicationInsights;
using System.Collections.Concurrent;

namespace CallAutomation.Scenarios
{
    public class TelemetryLoggingContext
    {
        public string? EventName { get; set; }
        public DateTime? StartTime { get; set; }
        public string? ServerCallId { get; set; }
        public string? RecordingId { get; set; }
        public string? ClientRequestId { get; set; }
        public int? Status { get; set; }
        public RecordingState? RecordingState { get; set; }
        public TelemetryLoggingContext() { }
    }
    public class TelemetryLogger
    {
        static TelemetryClient _telemetryClient = new TelemetryClient();
        private ConcurrentDictionary<string, TelemetryLoggingContext> _serverCallIdToTelemetryLoggingContext = new ConcurrentDictionary<string, TelemetryLoggingContext>();
        public TelemetryLoggingContext? GetTelemetryLoggingContext(string recordingId = null, string serverCallId = null)
        {
            if (_serverCallIdToTelemetryLoggingContext.TryGetValue(serverCallId != null ? serverCallId : recordingId, out var telemetryLoggingContext)) { return telemetryLoggingContext; }
            return null;
        }

        public void SetTelemetryLoggingContext(TelemetryLoggingContext telemetryLoggingContext, string recordingId = null, string serverCallId = null)
        {
            _serverCallIdToTelemetryLoggingContext.AddOrUpdate(serverCallId != null ? serverCallId : recordingId, telemetryLoggingContext, (_, _) => telemetryLoggingContext);
        }

        public void TrackEventHandler(TelemetryLoggingContext telemetryProperties)
        {
            var properties = new Dictionary<string, string> { { "ServerCallId", telemetryProperties.ServerCallId }, { "RecordingId", telemetryProperties.RecordingId }, { "StartTime", telemetryProperties.StartTime.ToString() }, { "ClientRequestId", telemetryProperties.ClientRequestId }, { "Status", telemetryProperties.Status.ToString() }, { "RecordingState", telemetryProperties.RecordingState.ToString() } };

            _telemetryClient.TrackEvent(telemetryProperties.EventName, properties);
        }

        public void TrackExceptionHandler(Exception ex)
        {
            _telemetryClient.TrackException(ex);
        }
        public void TrackMetrics(string message)
        {
        }

    }

}
