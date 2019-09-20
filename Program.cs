using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;


namespace PIMS3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        // ** TODO: **
        // 'Kestrel' is the default cross-platform web development server for ASP.NET Core used in PIMS; however, for PRODUCTION public-facing
        // functionality, we'll need to place the server behind a more robust reverse proxy server, e.g., "IIS", that will receive public 
        // HTTP requests, forwarding them to 'Kestrel' after initial handling and security checks.

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>

            WebHost.CreateDefaultBuilder(args)
                   .UseStartup<Startup>();


        

    }
}
