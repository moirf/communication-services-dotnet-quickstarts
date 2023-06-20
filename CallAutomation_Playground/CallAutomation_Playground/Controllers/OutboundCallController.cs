using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation_Playground.Controllers
{
    /// <summary>
    /// This is the controller for making an outbound call.
    /// Pass on PSTN target number here to make an outbound call to target.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OutboundCallController : ControllerBase
    {
        private readonly ILogger<OutboundCallController> _logger;
        private readonly CallAutomationClient _callAutomationClient;
        private readonly PlaygroundConfigs _playgroundConfig;
        private readonly ITopLevelMenuService _topLevelMenuService;
        private readonly IOngoingEventHandler _ongoingEventHandler;
        CommunicationIdentifier _target;
        CommunicationIdentifier formattedTargetIdentifier;
        CallConnection callConnection;
        string callConnectionId = string.Empty;

        public OutboundCallController(
            ILogger<OutboundCallController> logger,
            CallAutomationClient callAutomationClient,
            PlaygroundConfigs playgroundConfig,
            ITopLevelMenuService topLevelMenuService,
            IOngoingEventHandler ongoingEventHandler)
        {
            _logger = logger;
            _callAutomationClient = callAutomationClient;
            _playgroundConfig = playgroundConfig;
            _topLevelMenuService = topLevelMenuService;
            _ongoingEventHandler = ongoingEventHandler;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCall([FromQuery] string target)
        {

            PhoneNumberIdentifier caller = new PhoneNumberIdentifier(_playgroundConfig.DirectOfferedPhonenumber);
            try
            {
                if (!string.IsNullOrEmpty(target))
                {
                    CallInvite? callInvite = null;
                    var identifierKind = Tools.GetIdentifierKind(target);

                    if (identifierKind == Tools.CommunicationIdentifierKind.PhoneIdentity)
                    {
                        PhoneNumberIdentifier pstntarget = new PhoneNumberIdentifier(Tools.FormatPhoneNumbers(target));
                        callInvite = new CallInvite(pstntarget, caller);
                        _target = pstntarget;
                    }
                    else if (identifierKind == Tools.CommunicationIdentifierKind.UserIdentity)
                    {
                        CommunicationUserIdentifier communicationIdentifier = new CommunicationUserIdentifier(target);
                        callInvite = new CallInvite(communicationIdentifier);
                        _target = communicationIdentifier;
                    }
                    _logger.LogInformation($"Calling[{_target}] from DirectOfferNumber[{_playgroundConfig.DirectOfferedPhonenumber}]");


                    // create an outbound call to target using caller number
                    CreateCallResult createCallResult = await _callAutomationClient.CreateCallAsync(callInvite, _playgroundConfig.CallbackUri);
                    callConnectionId = createCallResult.CallConnectionProperties.CallConnectionId;

                    _ = Task.Run(async () =>
                    {
                        // attaching ongoing event handler for specific events
                        // This is useful for handling unexpected events could happen anytime (such as participants leaves the call and cal is disconnected)
                        _ongoingEventHandler.AttachCountParticipantsInTheCall(callConnectionId);
                        _ongoingEventHandler.AttachDisconnectedWrapup(callConnectionId);

                        // Waiting for event related to createCallResult, which is CallConnected
                        // Wait for 40 seconds before throwing timeout error.
                        var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(40));
                        CreateCallEventResult eventResult = await createCallResult.WaitForEventProcessorAsync(tokenSource.Token);
                        callConnection = createCallResult.CallConnection;

                        if (eventResult.IsSuccess)
                        {
                            // call connected returned! Call is now established.
                            // invoke top level menu now the call is connected;
                            await _topLevelMenuService.InvokeTopLevelMenu(
                                _target,
                                callConnection,
                                eventResult.SuccessResult.ServerCallId);
                        }
                    });
                }
            }
            catch (Exception e)
            {
                // Exception! likely the call was never established due to other party not answering.
                _logger.LogError($"Exception while doing outbound call. CallConnectionId[{callConnectionId}], Exception[{e}]");
            }

            return Ok(callConnectionId);
        }
        [HttpPost("AddParticipant")]
        public IActionResult AddParticipant([FromQuery] string target)
        {
            ICallingModules callingModule = new CallingModules(callConnection, _playgroundConfig);
            var addedParticipants = target.Split(';');
            foreach (var Participantidentity in addedParticipants)
            {
                CallInvite? callInvite = null;
                if (!string.IsNullOrEmpty(Participantidentity))
                {
                    formattedTargetIdentifier = Tools.FormateTargetIdentifier(Participantidentity.Trim());
                    _logger.LogInformation($"TargetIdentifier to Call[{formattedTargetIdentifier}]");

                    // then add the phone number
                    callingModule.AddParticipantAsync(
                       formattedTargetIdentifier,
                       _playgroundConfig.AllPrompts.AddParticipantSuccess,
                       _playgroundConfig.AllPrompts.AddParticipantFailure,
                       _playgroundConfig.AllPrompts.Music);
                    _logger.LogInformation($"Add Participant finished.");
                }
            }

            return Ok();
        }

    }
}
