using Microsoft.AspNetCore.Mvc;
using Serilog;


namespace PIMS3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoggingController : ControllerBase
    {
        /*  API for logging client-side (Angular) errors, information, etc.  */

        [HttpPost("")]
        [Route("LogError")]
        public ActionResult WriteToFile([FromBody] dynamic errorObj)
        {
            string sourceComponent = errorObj.source.Value;
            string errorMsg = errorObj.message.Value;
            string eventLevel = errorObj.eventLevel.Value;
            string stackTrace = errorObj.stackTrace.Value;

            Log.Error($"Source: {sourceComponent}");
            Log.Error($"Message: {errorMsg}");
            Log.Error($"EventLevel: {eventLevel}");
            Log.Error($"StackTrace: {stackTrace}");

            Log.Information("Error logging complete.");
            return Ok();
        }

        [HttpPost("")]
        [Route("LogNonError")]
        public ActionResult WriteNonErrorToFile([FromBody] dynamic nonErrorObj)
        {
            // Logs any Debug, Information, Warning, or Fatal level event.
            Log.Information(nonErrorObj.message1.Value);
            return Ok();
        }



    }
}
