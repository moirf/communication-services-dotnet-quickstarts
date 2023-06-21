using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation_Playground.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RemoveParticipantController
    {

        private readonly ILogger<RemoveParticipantController> _logger;
        private readonly PlaygroundConfigs _playgroundConfig;
        CommunicationIdentifier _target;

        public RemoveParticipantController(
            ILogger<RemoveParticipantController> logger,
            PlaygroundConfigs playgroundConfig)
        {
            _logger = logger;
            _playgroundConfig = playgroundConfig;
        }

        [HttpPost]
        public async Task RemoveFromCall([FromQuery] string removeparticipant)
        {
            string callConnectionId = string.Empty;
            try
            {                
                 if (!string.IsNullOrEmpty(removeparticipant))
                {
                    var removeparticipants= removeparticipant.Split(',');
                    foreach (var RemoveParticipantId in removeparticipants)
                    {
                        if (!string.IsNullOrEmpty(RemoveParticipantId))
                        {
                            var identifierKind = Tools.GetIdentifierKind(RemoveParticipantId);

                            if (identifierKind == Tools.CommunicationIdentifierKind.PhoneIdentity)
                            {
                                PhoneNumberIdentifier pstntarget = new PhoneNumberIdentifier(Tools.FormatPhoneNumbers(RemoveParticipantId));
                                _target = pstntarget;
                            }
                            else if (identifierKind == Tools.CommunicationIdentifierKind.UserIdentity)
                            {
                                CommunicationUserIdentifier communicationIdentifier = new CommunicationUserIdentifier(RemoveParticipantId);
                                _target = communicationIdentifier;
                            }
                            _logger.LogInformation($"Remove Participant [{_target}]");

                            ICallingModules callingModule = new CallingModules(callConnectionConfig.callConnection, _playgroundConfig);
                            await callingModule.RemoveParticipantAsync(_target);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Exception! likely the call was never established due to other party not answering.
                _logger.LogError($"Exception while doing Removing Participant from call. CallConnectionId[{callConnectionId}], Exception[{e}]");
            }

            
        }
    }
}
