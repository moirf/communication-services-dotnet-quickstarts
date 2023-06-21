<<<<<<< HEAD
﻿using Azure.Communication;
=======
using Azure.Communication;
>>>>>>> f70661d8037d852aeb6fad563cc6914f2ea0446e
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation_Playground.Controllers
{
<<<<<<< HEAD
=======
    /// <summary>
    /// This is controller where it will recieve interim events from Call automation service.
    /// We are utilizing event processor, this will handle events and relay to our business logic.
    /// </summary>
>>>>>>> f70661d8037d852aeb6fad563cc6914f2ea0446e
    [Route("api/[controller]")]
    [ApiController]
    public class AddParticipantController : ControllerBase
    {
<<<<<<< HEAD
        private readonly ILogger<AddParticipantController> _logger;
        private readonly PlaygroundConfigs _playgroundConfig;
        CommunicationIdentifier _target;

        public AddParticipantController(
            ILogger<AddParticipantController> logger,
            PlaygroundConfigs playgroundConfig)
        {
            _logger = logger;
            _playgroundConfig = playgroundConfig;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCall([FromQuery] string target)
        {
            string callConnectionId = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(target))
                {
                    var identifierKind = Tools.GetIdentifierKind(target);

                    if (identifierKind == Tools.CommunicationIdentifierKind.PhoneIdentity)
                    {
                        PhoneNumberIdentifier pstntarget = new PhoneNumberIdentifier(Tools.FormatPhoneNumbers(target));
                        _target = pstntarget;
                    }
                    else if (identifierKind == Tools.CommunicationIdentifierKind.UserIdentity)
                    {
                        CommunicationUserIdentifier communicationIdentifier = new CommunicationUserIdentifier(target);
                        _target = communicationIdentifier;
                    }
                    _logger.LogInformation($"Adding Participant [{_target}]");

                    ICallingModules callingModule = new CallingModules(callConnectionConfig.callConnection, _playgroundConfig);
                    await callingModule.AddParticipantAsync(_target,
                        _playgroundConfig.AllPrompts.AddParticipantSuccess,
                        _playgroundConfig.AllPrompts.AddParticipantFailure,
                        _playgroundConfig.AllPrompts.Music);
                }
            }
            catch (Exception e)
            {
                // Exception! likely the call was never established due to other party not answering.
                _logger.LogError($"Exception while doing outbound call. CallConnectionId[{callConnectionId}], Exception[{e}]");
            }

            return Ok(callConnectionId);
=======
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
>>>>>>> f70661d8037d852aeb6fad563cc6914f2ea0446e
        }
    }
}
