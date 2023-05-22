using Microsoft.ApplicationInsights;

namespace CallAutomation.Scenarios
{
    public interface ItelemetryKeys
    {
        public static string? EventName { get; set; }
        public static string? ServerCallId { get; set; }
        public static string? RecordingId { get; set; }
        public static string? StartTime { get; set; }
        public static string? ClientRequestId { get; set; }
        public static string? Status { get; set; }
        public static string? Content { get; set; }
        public static string? ContentStream { get; set; }
        public static string? ActionName { get; set; }
        //public Task TrackEvent();

    }
    public class TelemetryLogger
    {

        public static ItelemetryKeys _telemetryKeys;
        static TelemetryClient _telemetryClient;

        public TelemetryLogger(ItelemetryKeys telemetryKeys, TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
            _telemetryKeys = telemetryKeys;

        }
        public static void TrackEvent()
        {
            var properties = new Dictionary<string, string> { { "ServerCallId", _telemetryKeys.ServerCallId }, { "RecordingId", _telemetryKeys.RecordingId }, { "StartTime", _telemetryKeys.StartTime }, { "ActionName", _telemetryKeys.ActionName }, { "ClientRequestId", _telemetryKeys.ClientRequestId }, { "Status", _telemetryKeys.Status }, { "Content", _telemetryKeys.Content }, { "ContentStream", _telemetryKeys.ContentStream } };
            _telemetryClient.TrackEvent(_telemetryKeys.EventName, properties);
        }
        public static void TrackMetrics(string message)
        {
        }

    }

}
