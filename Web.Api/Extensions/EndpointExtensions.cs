using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RealityScraper.Web.Api.Endpoints;
using RealityScraper.Web.Api.Infrastructure;

namespace RealityScraper.Web.Api.Extensions;

internal static class EndpointExtensions
{
	public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
	{
		ServiceDescriptor[] serviceDescriptors = assembly
			.DefinedTypes
			.Where(type => type is { IsAbstract: false, IsInterface: false } &&
						   type.IsAssignableTo(typeof(IEndpoint)))
			.Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
			.ToArray();

		services.TryAddEnumerable(serviceDescriptors);

		return services;
	}

	public static IApplicationBuilder MapEndpoints(
		this WebApplication app,
		RouteGroupBuilder? routeGroupBuilder = null)
	{
		IEnumerable<IEndpoint> endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

		IEndpointRouteBuilder builder;
		if (routeGroupBuilder is null)
		{
			var group = app.MapGroup(string.Empty).RequireRateLimiting(RateLimitPolicies.Api);
			if (app.Configuration.IsAuthenticationEnabled())
			{
				group.RequireAuthorization();
			}

			builder = group;
		}
		else
		{
			builder = routeGroupBuilder;
		}

		foreach (IEndpoint endpoint in endpoints)
		{
			endpoint.MapEndpoint(builder);
		}

		return app;
	}

	public static RouteHandlerBuilder HasPermission(this RouteHandlerBuilder app, string permission)
	{
		return app.RequireAuthorization(permission);
	}
}