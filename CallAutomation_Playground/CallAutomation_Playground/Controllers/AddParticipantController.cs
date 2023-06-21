using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation_Playground.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddParticipantController : ControllerBase
    {
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
        }
    }
}
