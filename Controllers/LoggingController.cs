using Microsoft.AspNetCore.Mvc;
using Serilog;


namespace PIMS3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoggingController : ControllerBase
    {
        /* 
            Provides API for logging client-side (Angular) errors, information, etc. 
        */

        // POST: api/Logging
        [HttpPost]
        public void WriteToFile([FromBody] object errorObj)
        {
            // WIP - Write contents of errorObj to text file.
            //string logFile = CommonSvc.LogFile; // ok, but not needed.
            //Log.Information($"LoggingController.WriteToFile() called, with path of {logFile}."); // ok
            Log.Information("LoggingController.WriteToFile() completed.");
        }

        

    }
}
