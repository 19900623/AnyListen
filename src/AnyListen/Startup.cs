using System.Text;
using AnyListen.Api.Music;
using AnyListen.Helper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace AnyListen
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true);

            if (env.IsEnvironment("Development"))
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(true);
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            loggerFactory.AddConsole(LogLevel.Warning).AddNLog();

            app.UseApplicationInsightsRequestTelemetry();
            app.UseApplicationInsightsExceptionTelemetry();
            app.UseMvc();

            InitUserCfg();
        }

        private void InitUserCfg()
        {
            var userCfg = Configuration.GetSection("UserConfig");
            var wyCookie = userCfg["WyCookie"];
            if (!string.IsNullOrEmpty(wyCookie))
            {
                WyMusic.WyNewCookie = wyCookie;
            }

            var ipAddr = userCfg["IpAddress"];
            if (!string.IsNullOrEmpty(ipAddr))
            {
                CommonHelper.IpAddr = ipAddr;
            }

            var signKey = userCfg["SignKey"];
            if (!string.IsNullOrEmpty(signKey))
            {
                CommonHelper.SignKey = signKey;
            }
        }
    }
}
