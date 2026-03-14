using System.Reflection;
using Havit.Blazor.Components.Web;
using RealityScraper.Web.Api.Extensions;
using RealityScraper.Web.Api.Infrastructure;

namespace RealityScraper.Web.Api;

public static class DependencyInjection
{
	public static IServiceCollection AddPresentation(this IServiceCollection services)
	{
		services.AddEndpointsApiExplorer();

		services.AddAntiforgery();

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
		//services.AddScoped<IdentityRedirectManager>();

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

		return services;
	}
}