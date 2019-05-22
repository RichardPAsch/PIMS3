/*
    Class contains properties defined in the appsettings.json file, and is used for accessing application settings
    via objects injected by DI, e.g., 'InvestorSvc' accesses app settings via IOptions<AppSettings> appSettings object
    that is injected into the constructor.

    Mapping of configuration sections to classes is done in the ConfigureServices method of the Startup.cs file.
 */
namespace PIMS3.Helpers
{
    public class AppSettings
    {
        public string Secret { get; set; }
    }
}
