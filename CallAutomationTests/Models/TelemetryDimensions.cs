using Azure.Communication.CallAutomation;
using CallAutomation.Scenarios.Interfaces;

namespace CallAutomation.Scenarios.Models
{
    public class RecordingTelemetryDimensions : ITelemetryDimensions
    {
        public Dictionary<string, string> GetDimensionsProperties() => GetTelemetryDimensionValues(this, DimensionPropertyNames);

        public string? EventName { get; set; }
        public DateTime? StartTime { get; set; }
        public string? ServerCallId { get; set; }
        public string? RecordingId { get; set; }
        public RecordingState? RecordingState { get; set; }
        public double? DurationInMS { get; set; }



        private static readonly string[] DimensionPropertyNames = new[] {
            nameof(EventName),
            nameof(StartTime),
            nameof(ServerCallId),
            nameof(RecordingId),
            nameof(RecordingState),
            nameof(DurationInMS)
            // Add new dimensions from here
        };

        public Dictionary<string, string> GetTelemetryDimensionValues(object telemetryDimensions, string[] dimensionPropertyNames)
        {
            Dictionary<string, string> _properties = new Dictionary<string, string>();

            if (telemetryDimensions != null)
            {
                var dimensionLength = dimensionPropertyNames.Length;
                for (var i = 0; i < dimensionLength; i++)
                {
                    _properties.Add(dimensionPropertyNames[i], telemetryDimensions.GetType()?.GetProperty(dimensionPropertyNames[i])?.GetValue(telemetryDimensions, null)?.ToString() ?? "");
                }
            }
            return _properties;
        }
    }
}