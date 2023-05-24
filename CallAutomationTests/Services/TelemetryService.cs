using CallAutomation.Scenarios.Interfaces;
using Microsoft.ApplicationInsights;

namespace CallAutomation.Scenarios.Services
{
    public class TelemetryService : ITelemetryService
    {

        private readonly TelemetryClient _telemetryClient;

        public TelemetryService(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }



        public bool TrackEvent(string eventName, Dictionary<string, string> properties)
        {
            _telemetryClient.TrackEvent(eventName, properties);
            return true;
        }
    }
}
