using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;

namespace Healthcheck
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddHealthChecks()
                    .AddSqlServer(connectionString: Configuration.GetConnectionString("SqlServerDatabase"),
                              healthQuery: "SELECT 1;",
                              name: "Sql Server",
                              failureStatus: HealthStatus.Degraded)
                    .AddRedis(redisConnectionString: Configuration.GetConnectionString("RedisCache"),
                                    name: "Redis",
                                    failureStatus: HealthStatus.Degraded)
                    .AddUrlGroup(new Uri("http://www.google.com"),
                                    name: "Base URL",
                                    failureStatus: HealthStatus.Degraded)
                    .AddApplicationInsightsPublisher();

            services.AddHealthChecksUI(setupSettings: setup =>
                    {
                        setup.AddHealthCheckEndpoint("Basic healthcheck", "http://localhost:59658/healthcheck");
                    });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
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
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseHealthChecks("/healthcheck", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            })
            .UseHealthChecksUI(setup =>
            {
                setup.AddCustomStylesheet(@"wwwroot\css\dotnet.css");
            });

            app.UseHealthChecksUI();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}