using Serilog.Core;
using Serilog.Events;

namespace PIMS3
{
    public class SystemExceptionEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if(logEvent.Exception != null)
            {
                if (logEvent.Level.ToString() == "Error")
                {
                    string shortenedException = logEvent.Exception.StackTrace.Substring(0, 160) +
                                                "Exception: " + logEvent.Exception.Message.ToString();

                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "TruncatedSystemException", shortenedException));
                }
            }
            
        }
    }
}
