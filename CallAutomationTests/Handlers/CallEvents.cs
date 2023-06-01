// © Microsoft Corporation. All rights reserved.

using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomation.Scenarios.Handlers;

namespace CallAutomation.Scenarios.Handlers
{
    public class RecordingContext
    {
        /// <summary> The call id. </summary>
        public string? ServerCallId { get; set; }
        
        /// <summary> The recording id. </summary>
        public string? RecordingId { get; set; }
        
        /// <summary> The time at which the recording started. </summary>
        public DateTime? StartTime { get; set; }
        
        /// <summary> The recording start action duration in milliseconds. </summary>
        public double? StartDurationMS { get; set; }
        
        /// <summary> The recording pause action duration in milliseconds. </summary>
        public double? PauseDurationMS { get; set; }
        
        /// <summary> The recording resume action duration in milliseconds. </summary>
        public double? ResumeDurationMS { get; set; }

        /// <summary> The recording stop action duration in milliseconds. </summary>
        public double? StopDurationMS { get; set; }
        
        /// <summary> To keep track of recording action - Start, Stop, Pause, or Resume start time. </summary>
        public DateTime? RecordingActionStartTime { get; set; }
        public RecordingContext() { }
    }

    public class MediaSignalingContext
    {
        /// <summary> The call id. </summary>
        public string? ServerCallId { get; set; }

        /// <summary> The Add Participant action duration in milliseconds. </summary>
        public double? AddParticipantDurationMS { get; set; }

        /// <summary> The Remove Participant action duration in milliseconds. </summary>
        public double? RemoveParticipantDurationMS { get; set; }

        /// <summary> The Play Audio action duration in milliseconds. </summary>
        public double? PlayAudioDurationMS { get; set; }

        /// <summary> To keep track of media or signaling action - Add/Remove participant, play audio start time. </summary>
        public DateTime? ActionStartTime { get; set; }

        public MediaSignalingContext() { }
    }

    public class IncomingCallEvent
    {
        public CommunicationIdentifierModel? To { get; set; }
        public CommunicationIdentifierModel? From { get; set; }
        public string? CallerDisplayName { get; set; }
        public string? ServerCallId { get; set; }
        public string? IncomingCallContext { get; set; }
    }

    public class OutboundCallContext
    {
        public string? TargetId { get; set; }
    }

    public class RecordingFileStatusUpdatedEvent
    {
        public AcsRecordingStorageInfoProperties RecordingStorageInfo { get; }
        /// <summary> The time at which the recording started. </summary>
        public DateTimeOffset? RecordingStartTime { get; }
        /// <summary> The recording duration in milliseconds. </summary>
        public long? RecordingDurationMs { get; }
        /// <summary> The recording content type- AudioVideo, or Audio. </summary>
        public AcsRecordingContentType? ContentType { get; }
        /// <summary> The recording  channel type - Mixed, Unmixed. </summary>
        public AcsRecordingChannelType? ChannelType { get; }
        /// <summary> The recording format type - Mp4, Mp3, Wav. </summary>
        public AcsRecordingFormatType? FormatType { get; }
        /// <summary> The reason for ending recording session. </summary>
        public string SessionEndReason { get; }
    }

    public class CommunicationIdentifierModel
    {
        /// <summary> Raw Id of the identifier. Optional in requests, required in responses. </summary>
        public string RawId { get; set; }

        /// <summary> The communication user. </summary>
        public CommunicationUserIdentifierModel CommunicationUser { get; set; }

        /// <summary> The phone number. </summary>
        public PhoneNumberIdentifierModel PhoneNumber { get; set; }

        /// <summary> The Microsoft Teams user. </summary>
        public MicrosoftTeamsUserIdentifierModel MicrosoftTeamsUser { get; set; }
    }

    public class CommunicationUserIdentifierModel
    {
        /// <summary> The Id of the communication user. </summary>
        public string Id { get; set; }
    }

    public class PhoneNumberIdentifierModel
    {
        /// <summary> The phone number in E.164 format. </summary>
        public string Value { get; set; }
    }

    public class MicrosoftTeamsUserIdentifierModel
    {
        /// <summary> The Id of the Microsoft Teams user. If not anonymous, this is the AAD object Id of the user. </summary>
        public string UserId { get; set; }
    }
}
