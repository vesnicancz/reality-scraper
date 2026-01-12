using System.Globalization;
using RealityScraper.Application;
using RealityScraper.Infrastructure;
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

		builder.Host.UseSerilog((context, loggerConfig) =>
		{
			loggerConfig.ReadFrom.Configuration(context.Configuration);
			loggerConfig.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
		});

		// Registrace závislostí
		builder.Services.AddApplicationServices(builder.Configuration);
		builder.Services.AddInfrastructureServices(builder.Configuration);
		builder.Services.AddPresentation();

		var app = builder.Build();

		if (app.Environment.IsDevelopment())
		{
			app.UseOpenApiWithScalarUi();

			// blazor: start
			app.UseWebAssemblyDebugging();
			// blazor: end

			//app.ApplyMigrations();
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

		//app.UseAuthentication();

		//app.UseAuthorization();

		app.MapEndpoints();

		// Mapuje Identity API endpointy
		//app.MapGroup("/identity")
		//	.WithTags(Tags.Identity)
		//	.MapIdentityApi<ApplicationUser>();

		await app.RunAsync();
	}
}