﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Serialization;

using AutoMapper;

using Carmel.Models;
using Carmel.ViewModels;



namespace Carmel
{
    public class Startup
    {
        private IHostingEnvironment _env;
        private IConfigurationRoot _config;

        public Startup(IHostingEnvironment env)
        {
            _env = env;

            var builder = new ConfigurationBuilder()
              .SetBasePath(_env.ContentRootPath)
              .AddJsonFile("config.json")
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
              .AddEnvironmentVariables();

            _config = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_config);

            // Add authentication services
            //services.AddAuthentication(
            //    options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

            services.AddMvc()
                .AddJsonOptions(opt =>
                {
                    opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                });

            services.AddLogging();

            // Add functionality to inject IOptions<T>
            services.AddOptions();

            // Add the Auth0 Settings object so it can be injected
            services.Configure<Auth0Settings>(_config.GetSection("Auth0"));

            services.AddEntityFrameworkSqlServer().AddDbContext<CatalogContext>();

            services.AddTransient<CatalogContextSeedData>();

            services.AddScoped<ICatalogRepository, CatalogRepository>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, CatalogContextSeedData seeder, ILoggerFactory loggerFactory)
        {
            if (env.IsEnvironment("Development"))
            {
                app.UseDeveloperExceptionPage();
            }

            loggerFactory.AddDebug(LogLevel.Warning);

            app.UseStaticFiles();


            // JWT Middleware

            var options = new JwtBearerOptions
            {
                Audience = _config["auth0:clientId"],
                Authority = $"https://{_config["auth0:domain"]}/"
            };
            app.UseJwtBearerAuthentication(options);

            Mapper.Initialize(config =>
            {
                config.CreateMap<Component, ComponentViewModel>().ReverseMap();
                config.CreateMap<Sample, SampleViewModel>().ReverseMap();
            });

            app.UseMvc(config =>
            {
                config.MapRoute(
                    name: "Default",
                    template: "{controller}/{action}/{id?}",
                    defaults: new { controller = "App", action = "Index" }
                );
            });

            //
            // This gets called only at server startup and only of the database does not exist
            // (these test are a part of the CreateSeedData method, which also gets access to the 
            // CatalogContest reference in the seeder class.
            //
            seeder.CreateSeedData();
        }
    }
}
