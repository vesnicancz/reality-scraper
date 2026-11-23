using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RealityScraper.Application.Diagnostics;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace RealityScraper.Web.Api.Extensions;

/// <summary>
/// Export trace, metrik a logů přes OTLP (OTel Collector -> Tempo / Prometheus / Loki).
/// Zapíná se jen nastavením <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>, bez něj se nic neregistruje.
/// </summary>
/// <remarks>
/// Konfigurace jde standardními proměnnými OTel SDK (<c>OTEL_EXPORTER_OTLP_ENDPOINT</c>,
/// <c>OTEL_EXPORTER_OTLP_PROTOCOL</c>, <c>OTEL_SERVICE_NAME</c>, <c>OTEL_RESOURCE_ATTRIBUTES</c>, ...).
/// </remarks>
public static class OpenTelemetryExtensions
{
	private const string EndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";
	private const string ServiceNameKey = "OTEL_SERVICE_NAME";
	private const string DefaultServiceName = "reality-scraper";

	public static bool IsOpenTelemetryEnabled(this IConfiguration configuration)
	{
		return !string.IsNullOrWhiteSpace(configuration[EndpointKey]);
	}

	public static IServiceCollection AddOpenTelemetryExport(this IServiceCollection services, IConfiguration configuration)
	{
		if (!configuration.IsOpenTelemetryEnabled())
		{
			return services;
		}

		services.AddOpenTelemetry()
			.ConfigureResource(resource => resource.AddService(GetServiceName(configuration), serviceVersion: GetServiceVersion()))
			.WithTracing(tracing => tracing
				.AddSource(ScraperTelemetry.Name)
				// Npgsql má vlastní ActivitySource - SQL dotazy včetně těch z EF Core
				.AddSource("Npgsql")
				.AddAspNetCoreInstrumentation(options =>
				{
					options.Filter = context => !IsNoiseRequest(context.Request.Path);
				})
				.AddHttpClientInstrumentation(options =>
				{
					// Každý příkaz WebDriveru (FindElement, GetAttribute, ...) je HTTP request na driver
					// nebo Selenium hub - u stovek inzerátů by to byly tisíce spanů na běh. Průběh
					// scrapování zachycují vlastní spany (Navigate / Parse page), latenci driveru metrika
					// http.client.request.duration, ta filtrem neprochází.
					options.FilterHttpRequestMessage = request => !IsSeleniumRequest(request);
				})
				.AddOtlpExporter())
			.WithMetrics(metrics => metrics
				.AddMeter(ScraperTelemetry.Name)
				.AddMeter("Npgsql")
				// Vestavěné metriky .NET: GC, thread pool, ...
				.AddMeter("System.Runtime")
				.AddAspNetCoreInstrumentation()
				.AddHttpClientInstrumentation()
				// Exemplary nesou trace_id - z grafu v Grafaně jde prokliknout na konkrétní běh v Tempu.
				.SetExemplarFilter(ExemplarFilterType.TraceBased)
				.AddOtlpExporter());

		return services;
	}

	/// <summary>
	/// Serilog sink pro Loki. Konzole i TaskLogSink zůstávají; TraceId/SpanId doplní sink sám
	/// z <see cref="System.Diagnostics.Activity.Current"/>.
	/// </summary>
	public static LoggerConfiguration WriteToOpenTelemetry(this LoggerConfiguration loggerConfig, IConfiguration configuration)
	{
		if (!configuration.IsOpenTelemetryEnabled())
		{
			return loggerConfig;
		}

		return loggerConfig.WriteTo.OpenTelemetry(
			options =>
			{
				options.ResourceAttributes = new Dictionary<string, object>
				{
					["service.name"] = GetServiceName(configuration),
					["service.version"] = GetServiceVersion()
				};
				options.IncludedData = IncludedData.MessageTemplateTextAttribute
					| IncludedData.TraceIdField
					| IncludedData.SpanIdField
					| IncludedData.SpecRequiredResourceAttributes;
			},
			// OTEL_* proměnné (endpoint, protokol, OTEL_RESOURCE_ATTRIBUTES) se čtou z IConfiguration,
			// tedy i z appsettings, nejen z prostředí.
			key => configuration[key]);
	}

	private static string GetServiceName(IConfiguration configuration)
	{
		var serviceName = configuration[ServiceNameKey];
		return string.IsNullOrWhiteSpace(serviceName) ? DefaultServiceName : serviceName;
	}

	private static string GetServiceVersion()
	{
		var version = typeof(OpenTelemetryExtensions).Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

		// "1.2.3+<commit sha>" - sha do labelu v Promethea nepatří
		var plusIndex = version.IndexOf('+');
		return plusIndex > 0 ? version[..plusIndex] : version;
	}

	private static bool IsNoiseRequest(PathString path)
	{
		return path.StartsWithSegments("/health")
			|| path.StartsWithSegments("/_framework")
			|| path.StartsWithSegments("/_content");
	}

	private static bool IsSeleniumRequest(HttpRequestMessage request)
	{
		// Selenium posílá User-Agent "selenium/<verze> (.net <platforma>)" - platí pro lokální
		// ChromeDriver i vzdálený hub, bez závislosti na konfiguraci adresy.
		return request.Headers.TryGetValues("User-Agent", out var userAgents)
			&& userAgents.Any(ua => ua.StartsWith("selenium/", StringComparison.OrdinalIgnoreCase));
	}
}
