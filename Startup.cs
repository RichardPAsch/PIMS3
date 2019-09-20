using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using PIMS3.Data;
using PIMS3.Data.Repositories.IncomeSummary;
using PIMS3.DataAccess.Investor;
using PIMS3.Helpers;
using PIMS3.Services;
using System.Threading.Tasks;
using PIMS3.Data.Entities;
using Serilog;
using Serilog.Events;
using System;


namespace PIMS3
{
    public class Startup
    {
        public string logFilePath = string.Empty;
        public IConfiguration Configuration { get; }


        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Context created as a scoped service; db settings available via 'appsettings.json'
            services.AddDbContext<PIMS3Context>(cfg =>
            {
                cfg.UseSqlServer(Configuration.GetConnectionString("PIMS3ConnectionString"));
            });

            services.AddTransient<ICommonSvc, CommonSvc>();

            // Configure DI for application services, e.g.,Service, interface & implementation(s), available 
            // for the lifetime of a request (scoped).
            services.AddScoped<IIncomeRepository, IncomeRepository>(); 
            services.AddScoped<InvestorDataProcessing>();
            services.AddScoped<InvestorSvc>();
            services.AddScoped<AppSettings>();

            services.AddCors();
            services.AddMvc()
                    .AddJsonOptions(opt => opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
                    // Required compatability for [ApiController] annotation.
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);


            // Configure strongly-typed settings objects.
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);


            // Configure JWT authentication.
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = System.Text.Encoding.ASCII.GetBytes(appSettings.Secret);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            // see: https://jasonwatmore.com/post/2018/06/26/aspnet-core-21-simple-api-for-authentication-registration-and-user-management#startup-cs
            .AddJwtBearer(x =>
            {
                x.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        var investorService = ctx.HttpContext.RequestServices.GetRequiredService<InvestorSvc>();
                        string investorId = ctx.Principal.Identity.Name;
                        Investor investor = investorService.GetById(investorId);
                        if (investor == null)
                        {
                            // Investor no longer exists.
                            ctx.Fail("Unauthorized");
                        }
                        return Task.CompletedTask;
                    }
                };
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });


            services.Configure<ApiBehaviorOptions>(options =>
            {
                // When an action parameter is annotated with the [FromForm] attribute, the multipart/form-data request content type is inferred.
                options.SuppressConsumesConstraintForFormFileParameters = true;
                // Inference rules are applied for the default data sources of action parameters, i.e., [FromBody],[FromForm],[FromRoute],[FromQuery]
                options.SuppressInferBindingSourcesForParameters = true;
                // ModelState validation errors automatically trigger an HTTP 400 response.
                options.SuppressModelStateInvalidFilter = true;
            });

            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            logFilePath = env.ContentRootPath + @"\Logs\PIMS3_log_.txt";
            ConfigureLogger();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            //app.UseMvc(routes =>
            //{
            //    routes.MapRoute(
            //        name: "default",
            //        template: "{controller}/{action=Index}/{id?}");
            //});

            // Set global CORS policy.
            app.UseCors(x => x
               .AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());

            app.UseAuthentication();

            // Convenience method replaces above "app.UseMvc(routes => ..."
            app.UseMvcWithDefaultRoute();
            
            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    //spa.UseAngularCliServer(npmScript: "start");
                    // Run 'npm start' to launch external/independent Angular CLI server before running app.
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:4200");
                    //spa.UseProxyToSpaDevelopmentServer("http://localhost:44328");
                }
            });

        }


        public void ConfigureLogger()
        {
            /* -- Configures 3rd-party logging library: 'Serilog' --
               Logging event levels may only be raised for sinks, not lowered.
               Event-level hierarchy: [ Verbose -> Debug -> Information -> Warning -> Error -> Fatal ].
            */
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.WithEnvironmentUserName()
                .Enrich.With(new SystemExceptionEnricher())
                .WriteTo.File(logFilePath,
                               rollingInterval: RollingInterval.Day,
                               outputTemplate: "[{Timestamp: MM-dd-yyyy HH:mm:ss} {Level:u3}]  {Message:lj} " + "{Properties:j}{NewLine}",
                               // {Message:lj} - format options cause data embedded in the message to be output as JSON (j), except for string literals, 
                               //                which are output as-is.
                               // {Level:u3} or {Level:w3} - formats three-character upper- or lowercase level names, respectively.
                               // {Properties:j} - added to the output template for including additional context information.
                               // {Exception} - removed to avoid verbose StackTrace data.
                               retainedFileCountLimit: 5,
                               fileSizeLimitBytes: 1_000_000,
                               rollOnFileSizeLimit: true,
                               flushToDiskInterval: TimeSpan.FromSeconds(1))
                .CreateLogger();

            Log.Information("=== Starting 'PIMS3' service ===");
        }

    }
}
