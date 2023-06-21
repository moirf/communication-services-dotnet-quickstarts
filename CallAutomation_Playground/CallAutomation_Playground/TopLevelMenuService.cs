using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation_Playground.Interfaces;

namespace CallAutomation_Playground
{
    /// <summary>
    /// This is our top level menu that will have our greetings menu.
    /// </summary>
    public class TopLevelMenuService : ITopLevelMenuService
    {
        private readonly ILogger<TopLevelMenuService> _logger;
        private readonly CallAutomationClient _callAutomation;
        private readonly PlaygroundConfigs _playgroundConfig;
        CommunicationIdentifier formattedTargetIdentifier;

        public TopLevelMenuService(
            ILogger<TopLevelMenuService> logger,
            CallAutomationClient callAutomation,
            PlaygroundConfigs playgroundConfig)
        {
            _logger = logger;
            _callAutomation = callAutomation;
            _playgroundConfig = playgroundConfig;
        }

        public async Task InvokeTopLevelMenu(
            CommunicationIdentifier originalTarget,
            CallConnection callConnection,
            string serverCallId)
        {
            _logger.LogInformation($"Invoking top level menu, with CallConnectionId[{callConnection.CallConnectionId}]");

            // prepare calling modules to interact with this established call
            ICallingModules callingModule = new CallingModules(callConnection, _playgroundConfig);
            PhoneNumberIdentifier caller = new PhoneNumberIdentifier(_playgroundConfig.DirectOfferedPhonenumber);

            try
            {
                while (true)
                {
                    // Top Level DTMF Menu, ask for which menu to be selected
                    string selectedTone = await callingModule.RecognizeTonesAsync(
                        originalTarget,
                        1,
                        1,
                        _playgroundConfig.AllPrompts.MainMenu,
                        _playgroundConfig.AllPrompts.Retry);

                    _logger.LogInformation($"Caller selected DTMF Tone[{selectedTone}]");

                    switch (selectedTone)
                    {
                        // Option 1: Transfer Call to another PSTN endpoint
                        case "1":
                            //formattedTargetIdentifier = Tools.FormateTargetIdentifier(_playgroundConfig.ParticipantToTransfer);
                            //_logger.LogInformation($"Phonenumber to Transfer[{formattedTargetIdentifier}]");

                            //// then transfer to the phonenumber
                            //var trasnferSuccess = await callingModule.TransferCallAsync(
                            //    formattedTargetIdentifier,
                            //    _playgroundConfig.AllPrompts.TransferFailure);

                            //if (trasnferSuccess)
                            //{
                            //    _logger.LogInformation($"Successful Transfer - ending this logic.");
                            //    return;
                            //}
                            //else
                            //{
                            //    _logger.LogInformation($"Transfer Failed - back to main menu.");
                            //}
                            break;

                        // Option 2: Start Recording this call
                        case "2":
                            // ... then Start Recording
                            // this will accept serverCallId and uses main service client
                            _logger.LogInformation($"Start Recording...");
                            CallLocator callLocator = new ServerCallLocator(serverCallId);
                            StartRecordingOptions startRecordingOptions = new StartRecordingOptions(callLocator);
                            _ = await _callAutomation.GetCallRecording().StartAsync(startRecordingOptions);

                            // Play message of start of recording
                            await callingModule.PlayMessageThenWaitUntilItEndsAsync(_playgroundConfig.AllPrompts.PlayRecordingStarted);
                            break;

                        // Option 3: Play Message and terminate the call only for the original caller.
                        case "3":
                            _logger.LogInformation($"terminate the call only for the original caller.");
                            await callConnection.HangUpAsync(false);
                            return;

                        // Option 4: Play Message and terminate the call
                        case "4":
                            _logger.LogInformation($"Terminating Call. Due to wrong input too many times, exception happened, or user requested termination.");
                            await callingModule.PlayMessageThenWaitUntilItEndsAsync(_playgroundConfig.AllPrompts.Goodbye);
                            await callingModule.TerminateCallAsync();
                            return;

                        default:
                            // Wrong input!
                            // play message then retry this toplevel menu.
                            _logger.LogInformation($"Wrong Input! selectedTone[{selectedTone}]");
                            await callingModule.PlayMessageThenWaitUntilItEndsAsync(_playgroundConfig.AllPrompts.Retry);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Exception during Top Level Menu! [{e}]");
            }

            // wrong input too many times, exception happened, or user requested termination.
            // good bye and hangup
            _logger.LogInformation($"Terminating Call. Due to wrong input too many times, exception happened, or user requested termination.");
            await callingModule.PlayMessageThenWaitUntilItEndsAsync(_playgroundConfig.AllPrompts.Goodbye);
            await callingModule.TerminateCallAsync();
            return;
        }
    }
}
