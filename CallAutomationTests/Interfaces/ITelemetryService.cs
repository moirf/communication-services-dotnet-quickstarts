using CallAutomation.Scenarios.Handlers;

namespace CallAutomation.Scenarios.Interfaces
{
    public interface ITelemetryService
    {
        bool TrackEvent(string eventName, Dictionary<string,string> properties);
        public bool TrackMetric(string metricName, double value);
    }
}
