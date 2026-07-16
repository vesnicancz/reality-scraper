using System.Reflection;
using System.Threading.RateLimiting;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.RateLimiting;
using RealityScraper.Web.Api.Extensions;
using RealityScraper.Web.Api.Infrastructure;

namespace RealityScraper.Web.Api;

public static class DependencyInjection
{
	public static IServiceCollection AddPresentation(this IServiceCollection services)
	{
		services.AddEndpointsApiExplorer();

		services.AddAntiforgery();

		services.AddRateLimiter(options =>
		{
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
			options.AddPolicy(RateLimitPolicies.Api, httpContext => RateLimitPartition.GetFixedWindowLimiter(
				httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
				_ => new FixedWindowRateLimiterOptions
				{
					PermitLimit = 100,
					Window = TimeSpan.FromMinutes(1),
					QueueLimit = 0
				}));
		});

		services.AddExceptionHandler<GlobalExceptionHandler>();
		services.AddProblemDetails();

		services.AddOpenApi();

		services.AddEndpoints(Assembly.GetExecutingAssembly());

		// blazor start
		services.AddRazorComponents()
			.AddInteractiveWebAssemblyComponents()
			.AddAuthenticationStateSerialization();

		services.AddAuthorization();
		services.AddCascadingAuthenticationState();

		// HttpClient for SSR (so components can inject HttpClient during server render)
		services.AddHttpContextAccessor();
		services.AddHttpClient("BlazorSSR");
		services.AddScoped(sp =>
		{
			var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
			var httpClient = httpClientFactory.CreateClient("BlazorSSR");

			var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
			var request = httpContext?.Request;
			if (request is not null)
			{
				httpClient.BaseAddress = new Uri($"{request.Scheme}://{request.Host}");
			}

			return httpClient;
		});

		// blazor end

		services.AddHxServices();
		services.AddHxMessenger();
		services.AddHxMessageBoxHost();

		return services;
	}
}