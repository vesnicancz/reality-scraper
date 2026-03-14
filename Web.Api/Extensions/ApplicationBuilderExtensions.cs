using Microsoft.EntityFrameworkCore;
using RealityScraper.Infrastructure.Contexts;
using Scalar.AspNetCore;

namespace RealityScraper.Web.Api.Extensions;

internal static class ApplicationBuilderExtensions
{
	public static IApplicationBuilder UseOpenApiWithScalarUi(this WebApplication app)
	{
		app.MapScalarApiReference(i => i.EnabledClients = [ScalarClient.RestSharp, ScalarClient.Curl]);
		app.MapOpenApi();

		return app;
	}

	public static void ApplyMigrations(this IApplicationBuilder app)
	{
		using var scope = app.ApplicationServices.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<RealityDbContext>();
		dbContext.Database.Migrate();
	}
}