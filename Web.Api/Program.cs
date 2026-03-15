using System.Globalization;
using RealityScraper.Application;
using RealityScraper.Infrastructure;
using RealityScraper.Application.Interfaces.Logging;
using RealityScraper.Infrastructure.Logging;
using RealityScraper.Web.Api.Components;
using RealityScraper.Web.Api.Extensions;
using Serilog;

namespace RealityScraper.Web.Api;

public static class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Configuration
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", true); // .gitignored

		// Registrace závislostí
		builder.Services.AddApplicationServices(builder.Configuration);
		builder.Services.AddInfrastructureServices(builder.Configuration);

		builder.Host.UseSerilog((context, services, loggerConfig) =>
		{
			loggerConfig.ReadFrom.Configuration(context.Configuration);
			loggerConfig.Enrich.FromLogContext();
			loggerConfig.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
			loggerConfig.WriteTo.Sink(new TaskLogSink(services.GetRequiredService<ITaskLogWriter>()));
		});
		builder.Services.AddPresentation();
		builder.Services.AddOidcAuthentication(builder.Configuration);
		builder.Services.AddHealthChecks();

		var app = builder.Build();

		await app.ApplyMigrationsAsync();

		if (app.Environment.IsDevelopment())
		{
			app.UseOpenApiWithScalarUi();

			// blazor: start
			app.UseWebAssemblyDebugging();
			// blazor: end
		}
		else
		{
			app.UseExceptionHandler("/Error");
			// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
			app.UseHsts();
		}

		// blazor: start
		//app.UseStatusCodePagesWithReExecute("/not-found", createScopeForErrors: true); //.net 10
		app.UseStatusCodePagesWithReExecute("/not-found");
		app.UseHttpsRedirection();
		app.UseAntiforgery();
		app.MapStaticAssets();
		app.MapRazorComponents<App>()
			.AddInteractiveWebAssemblyRenderMode()
			.AddAdditionalAssemblies(typeof(Client._Imports).Assembly);
		// blazor: end

		//app.UseRequestContextLogging();

		app.UseSerilogRequestLogging();

		app.UseExceptionHandler();

		app.UseOidcAuthentication();

		app.MapHealthChecks("/health");
		app.MapAuthenticationEndpoints();
		app.MapEndpoints();

		await app.RunAsync();
	}
}