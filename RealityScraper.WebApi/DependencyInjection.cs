using System.Reflection;
using Havit.Blazor.Components.Web;
using RealityScraper.WebApi.Extensions;
using RealityScraper.WebApi.Infrastructure;

namespace RealityScraper.WebApi;

public static class DependencyInjection
{
	public static IServiceCollection AddPresentation(this IServiceCollection services)
	{
		services.AddEndpointsApiExplorer();

		services.AddAntiforgery();

		services.AddExceptionHandler<GlobalExceptionHandler>();
		services.AddProblemDetails();

		services.AddOpenApi();

		services.AddControllers();
		services.AddEndpoints(Assembly.GetExecutingAssembly());

		// blazor start
		services.AddRazorComponents()
			.AddInteractiveWebAssemblyComponents()
			.AddAuthenticationStateSerialization();

		services.AddCascadingAuthenticationState();
		//services.AddScoped<IdentityRedirectManager>();

		// HttpClient for SSR (so components can inject HttpClient during server render)
		services.AddHttpContextAccessor();
		services.AddScoped(sp =>
		{
			var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
			var request = httpContext?.Request;
			if (request is not null)
			{
				var baseUri = new Uri($"{request.Scheme}://{request.Host}");
				return new HttpClient { BaseAddress = baseUri };
			}
			return new HttpClient();
		});

		// blazor end

		services.AddHxServices();
		services.AddHxMessenger();

		return services;
	}
}