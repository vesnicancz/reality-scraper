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
		builder.Services.AddForwardedHeadersConfiguration(builder.Configuration);
		builder.Services.AddOidcAuthentication(builder.Configuration, builder.Environment);
		builder.Services.AddHealthChecks();

		var app = builder.Build();

		// Fail-closed: v produkci bez zapnuté autentizace a bez vědomého AllowAnonymous nestartujeme.
		app.EnsureAuthenticationConfigured();

		await app.ApplyMigrationsAsync();

		app.UseForwardedHeaders();

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
		app.UseStatusCodePagesWithReExecute("/not-found");
		if (!app.Environment.IsDevelopment())
		{
			app.UseHttpsRedirection();
		}

		app.UseSerilogRequestLogging();

		app.UseExceptionHandler();

		app.UseOidcAuthentication();

		app.UseRateLimiter();

		app.UseAntiforgery();
		app.MapStaticAssets();
		var razorComponents = app.MapRazorComponents<App>()
			.AddInteractiveWebAssemblyRenderMode()
			.AddAdditionalAssemblies(typeof(Client._Imports).Assembly);
		if (app.Configuration.IsAuthenticationEnabled())
		{
			// Bez zapnuté autentizace neběží autorizační middleware a metadata
			// RequireAuthorization by shodila každý request na Blazor stránky.
			razorComponents.RequireAuthorization();
		}
		else if (!app.Environment.IsDevelopment())
		{
			// Sem se v produkci dostaneme jen s vědomě nastaveným Authentication:AllowAnonymous=true.
			app.Logger.LogWarning("Autentizace je vypnutá (AllowAnonymous) - UI i API běží bez přihlášení. Pro produkční nasazení nastavte Authentication:Enabled=true.");
		}
		// blazor: end

		app.MapHealthChecks("/health");
		app.MapAuthenticationEndpoints();
		app.MapEndpoints();

		await app.RunAsync();
	}
}