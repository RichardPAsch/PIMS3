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


namespace PIMS3
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

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
            services.AddScoped<IIncomeRepository, IncomeRepository>(); // TODO: no longer needed ?
            services.AddScoped<InvestorDataProcessing>();
            services.AddScoped<IInvestorSvc, InvestorSvc>();
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
            .AddJwtBearer(x =>
            {
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
            //.AddJwtBearer(x =>
            //{
            //    OnTokenValidated = ctx =>
            //    {

            //    }

            //});


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
    }
}
