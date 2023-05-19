using CallAutomation.Scenarios.Handlers;
using CallAutomation.Scenarios.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallAutomation.Scenarios.Controllers
{
    [ApiController]
    public class ContextController : ControllerBase
    {
        private readonly IEventActionEventHandler<RecordingContext> _recordingActionHandler;
        public ContextController(IEventActionEventHandler<RecordingContext> recordingActionHandler)
        {            
            _recordingActionHandler = recordingActionHandler;
        }

        [HttpGet("getRecordingContext/{serverCallId}", Name = "GetRecording_Context")]
        public async Task<ActionResult> GetRecordingContextAsync([FromRoute] string serverCallId)
        {
            RecordingContext recordingContext = _recordingActionHandler.Handle(serverCallId);
            return new JsonResult(recordingContext);
        }
    }
}
