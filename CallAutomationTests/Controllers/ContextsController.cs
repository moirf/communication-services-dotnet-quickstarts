using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation.Scenarios.Controllers
{
    [ApiController]
    public class ContextsController : ControllerBase
    {
        private readonly ICallContextService _callContextService;
        public ContextsController(ICallContextService callContextService)
        {
            _callContextService = callContextService;
        }

        [HttpGet("recordingcontext", Name = "GetRecording_Context")]
        public IActionResult GetRecordingContext([FromRoute] string serverCallId)
        {
            var recordingContext = _callContextService.GetRecordingContext(serverCallId);
            return new JsonResult(recordingContext);
        }

        [HttpPatch("recordingcontext", Name = "SetRecording_Context")]
        public IActionResult SetRecordingContext([FromBody] RecordingContext recordingContext)
        {
            _callContextService.SetRecordingContext(recordingContext.ServerCallId, recordingContext);
            return new OkResult();
        }

        [HttpDelete("recordingcontext", Name = "DeleteRecording_Context")]
        public IActionResult DeleteRecordingContext([FromBody] RecordingContext recordingContext)
        {
            _callContextService.DeleteRecordingContext(recordingContext.ServerCallId);
            return new OkResult();
        }


        [HttpGet("mediasignalingcontext", Name = "GetMediaSignaling_Context")]
        public IActionResult GetMediaSignalingContext([FromRoute] string serverCallId)
        {
            var recordingContext = _callContextService.GetMediaSignalingContext(serverCallId);
            return new JsonResult(recordingContext);
        }

        [HttpPatch("mediasignalingcontext", Name = "SetMediaSignaling_Context")]
        public IActionResult SetMediaSignalingContext([FromBody] MediaSignalingContext mediaSignalingContext)
        {
            _callContextService.SetMediaSignalingContext(mediaSignalingContext.ServerCallId, mediaSignalingContext);
            return new OkResult();
        }

        [HttpDelete("mediasignalingcontext", Name = "DeleteMediaSignaling_Context")]
        public IActionResult DeleteMediaSignalingContext([FromBody] MediaSignalingContext mediaSignalingContext)
        {
            _callContextService.DeleteMediaSignalingContext(mediaSignalingContext.ServerCallId);
            return new OkResult();
        }
    }
}
