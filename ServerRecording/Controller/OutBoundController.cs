using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QuickStartApi.Controllers;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Options;
using QuickStartApi;
using Azure;

namespace RecordingApi.Controllers
{
    public class OutBoundController:Controller
    {
        private readonly CallAutomationClient callAutomationClient;
        private readonly string SourcePhoneNumber;
        private readonly string BaseUri;
        string targetID = null;
        public ILogger<CallRecordingController> Logger { get; }

        public OutBoundController(IConfiguration configuration, ILogger<CallRecordingController> logger)
        {
            callAutomationClient = new CallAutomationClient(configuration["ACSResourceConnectionString"]);
            SourcePhoneNumber = configuration["SourcePhoneNumber"];
            BaseUri = configuration["BaseUri"] + configuration["CallBackUri"];
            Logger = logger;
        }

        /// <summary>
        /// Method to start call 
        /// </summary>
        /// <param name="TergetID">terget id of the call</param>
        [HttpGet]
        [Route("/api/call")]
        public async Task<IActionResult> CreateCall([FromQuery] string PSTNTargetID)
        {           
            var CallerId = new PhoneNumberIdentifier(SourcePhoneNumber);
            var target = new PhoneNumberIdentifier(PSTNTargetID);
            var callInvite = new CallInvite(target, CallerId);

            var createCallOption = new CreateCallOptions(callInvite, new Uri(BaseUri));

            var response = await callAutomationClient.CreateCallAsync(createCallOption).ConfigureAwait(false);

            Logger.LogInformation($"Reponse from create call: {response.GetRawResponse()}" +
            $"CallConnection Id : {response.Value.CallConnection.CallConnectionId}" + $"Servercall id:{response.Value.CallConnectionProperties.ServerCallId}");
            return Json(response.Value.CallConnectionProperties.ServerCallId);
        }


        [HttpPost]
        [Route("/api/callbacks")]
        public  IActionResult Callbacks([FromBody] CloudEvent[] cloudEvents)
        {
            try
            {
                foreach (var cloudEvent in cloudEvents)
                {
                    Logger.LogInformation($"Event received: {JsonConvert.SerializeObject(cloudEvent)}");
                    CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
                    
                    if (@event is CallConnected)
                    {
                        
                        Logger.LogInformation($"Server Call Id: {@event.ServerCallId}");
                        return Json(@event.ServerCallId);

                    }

                }
                return Json("");

            }
            catch (Exception ex)
            {
                 return Json(new { Exception = ex });
            }

        }


    }
}
