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
}