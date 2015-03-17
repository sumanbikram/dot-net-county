﻿using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Diagnostics.Entity;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Data.Entity;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Console;
using MyCountries.Web.Data;
using MyCountries.Web.Models;
using Newtonsoft.Json.Serialization;
using System;
using MyCountries.Web.Services;

namespace MyCountries.Web
{
  public class Startup
  {
    public IConfiguration Configuration { get; set; }

    public Startup(IHostingEnvironment env)
    {
      // Setup configuration sources.
      Configuration = new Configuration()
          .AddJsonFile("config.json")
          .AddEnvironmentVariables();

    }

    // This method gets called by the runtime.
    public void ConfigureServices(IServiceCollection services)
    {
      var connectionString = Configuration.Get("Data:DefaultConnection:ConnectionString");

      // Add EF services to the services container.
      services.AddEntityFramework(Configuration)
          .AddSqlServer()
          .AddDbContext<MyCountriesContext>(options =>
          {
            options.UseSqlServer(connectionString);
          });

      // Add Identity services to the services container.
      services.AddIdentity<ApplicationUser, IdentityRole>(Configuration)
          .AddEntityFrameworkStores<MyCountriesContext>();

      // Add MVC services to the services container.
      services.AddMvc()
        .Configure<MvcOptions>(options =>
        {
      // See Strathweb's great discussion of formatters: http://www.strathweb.com/2014/11/formatters-asp-net-mvc-6/

      // Support Camelcasing in MVC API Controllers
      var jsonOutputFormatter = new JsonOutputFormatter();
          jsonOutputFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
          jsonOutputFormatter.SerializerSettings.DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore;

          options.OutputFormatters.RemoveAll(formatter => formatter.Instance.GetType() == typeof(JsonOutputFormatter));
          options.OutputFormatters.Insert(0, jsonOutputFormatter);
        }); ;

      // Add other services
      services.AddScoped<IMyCountriesRepository, MyCountriesRepository>();
#if DEBUG
      services.AddScoped<IEmailer, ConsoleEmailer>();
#else
  services.AddScoped<IEmailer, Emailer>();
#endif
    }

    // Configure is called after ConfigureServices is called.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerfactory)
    {
      // Configure the HTTP request pipeline.
      // Add the console logger.
      loggerfactory.AddConsole();

      // Add the following to the request pipeline only in development environment.
      if (string.Equals(env.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
      {
        app.UseBrowserLink();
        app.UseErrorPage(ErrorPageOptions.ShowAll);
        app.UseDatabaseErrorPage(DatabaseErrorPageOptions.ShowAll);
      }
      else
      {
        // Add Error handling middleware which catches all application specific errors and
        // send the request to the following path or controller action.
        app.UseErrorHandler("/Home/Error");
      }

      // Add static files to the request pipeline.
      app.UseStaticFiles();

      // Add cookie-based authentication to the request pipeline.
      app.UseIdentity();

      // Add MVC to the request pipeline.
      app.UseMvc(routes =>
      {
        routes.MapRoute(
              name: "default",
              template: "{controller}/{action}/{id?}",
              defaults: new { controller = "Home", action = "Index" });
      });

      // Add Sample Data
      var sampleData = ActivatorUtilities.CreateInstance<SampleData>(app.ApplicationServices);
      sampleData.InitializeData();

    }
  }
}
