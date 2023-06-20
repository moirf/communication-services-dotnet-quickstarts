using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation_Playground.Controllers
{
    /// <summary>
    /// This is controller where it will recieve interim events from Call automation service.
    /// We are utilizing event processor, this will handle events and relay to our business logic.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AddParticipantController : ControllerBase
    {
        private readonly ILogger<EventController> _logger;
        private readonly IOngoingEventHandler _ongoingEventHandler;
        private readonly CallAutomationClient _callAutomation;
        private readonly PlaygroundConfigs _playgroundConfig;
        CommunicationIdentifier formattedTargetIdentifier;

        public AddParticipantController(
            ILogger<EventController> logger,
            CallAutomationClient callAutomation,
            PlaygroundConfigs playgroundConfig)
        {
            _logger = logger;
            _callAutomation = callAutomation;
            _playgroundConfig = playgroundConfig;
        }

        [HttpGet]
        public IActionResult AddParticipant([FromQuery] string target)
        {

            //ICallingModules callingModule = new CallingModules(callConnection, _playgroundConfig);
            //var addedParticipants = target.Split(';');
            //foreach (var Participantidentity in addedParticipants)
            //{
            //    CallInvite? callInvite = null;
            //    if (!string.IsNullOrEmpty(Participantidentity))
            //    {
            //        formattedTargetIdentifier = Tools.FormateTargetIdentifier(Participantidentity.Trim());
            //        _logger.LogInformation($"TargetIdentifier to Call[{formattedTargetIdentifier}]");

            //        // then add the phone number
            //        callingModule.AddParticipantAsync(
            //           formattedTargetIdentifier,
            //           _playgroundConfig.AllPrompts.AddParticipantSuccess,
            //           _playgroundConfig.AllPrompts.AddParticipantFailure,
            //           _playgroundConfig.AllPrompts.Music);
            //        _logger.LogInformation($"Add Participant finished.");
            //    }
            //}

            return Ok();
        }
    }
}
