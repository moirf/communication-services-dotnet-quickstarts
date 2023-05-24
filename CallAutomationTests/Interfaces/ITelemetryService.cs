using CallAutomation.Scenarios.Handlers;

namespace CallAutomation.Scenarios.Interfaces
{
    public interface ITelemetryService
    {
        bool TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null);
        public bool TrackMetric(string metricName, double value);
    }
}
