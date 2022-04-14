using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;
using Microsoft.Extensions.Hosting;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;

namespace Umbraco.Cms.Web.UI.NetCore
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="webHostEnvironment">The Web Host Environment</param>
        /// <param name="config">The Configuration</param>
        /// <remarks>
        /// Only a few services are possible to be injected here https://github.com/dotnet/aspnetcore/issues/9337
        /// </remarks>
        public Startup(IWebHostEnvironment webHostEnvironment, IConfiguration config)
        {
            _env = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }



        /// <summary>
        /// Configures the services
        /// </summary>
        /// <remarks>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        /// </remarks>
        public void ConfigureServices(IServiceCollection services)
        {
#pragma warning disable IDE0022 // Use expression body for methods

            // Use NewtonSoft as json serializer - the new evil. 
            // services.AddControllers().AddNewtonsoftJson();

            services.AddUmbraco(_env, _config)
                .AddBackOffice()
                .AddWebsite()
                // // uSync example - you can just leave this out, and the default settings from appsettings.json are used.
                // .AdduSync(o => {
                //     o.ImportAtStartup = uSyncConstants.Groups.Settings; // import settings at startup.
                //     o.RootFolder = "/uSync/v8/"; // use a v8 folder (so you can just copy from v8
                //     }) 
                .AddComposers()
                .AddCustomHandlers()
                .Build();
#pragma warning restore IDE0022 // Use expression body for methods

        }

        /// <summary>
        /// Configures the application
        /// </summary>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseUmbraco()
                .WithMiddleware(u =>
                {
                    u.UseBackOffice();
                    u.UseWebsite();
                })
                .WithEndpoints(u =>
                {
                    u.UseInstallerEndpoints();
                    u.UseBackOfficeEndpoints();
                    u.UseWebsiteEndpoints();
                });
        }
    }


    internal static class uSyncCustomHandlerExtensions
    {
        public static IUmbracoBuilder AddCustomHandlers(this IUmbracoBuilder builder)
        {
            var customSet = "uSync:Sets:Custom";

            // default handler options, other people can load their own names handler options and 
            // they can be used throughout uSync (so complete will do this). 
            builder.Services.Configure<uSyncHandlerSetSettings>("Custom",
                builder.Config.GetSection(customSet));

            return builder;

        }
    }
}
