using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RealityScraper.Web.Client.Infrastructure;

namespace RealityScraper.Web.Client;

internal static class Program
{
	private static async Task Main(string[] args)
	{
		var builder = WebAssemblyHostBuilder.CreateDefault(args);

		builder.Services.AddAuthorizationCore();
		builder.Services.AddCascadingAuthenticationState();
		builder.Services.AddAuthenticationStateDeserialization();

		builder.Services.AddHxServices();
		builder.Services.AddHxMessenger();
		builder.Services.AddHxMessageBoxHost();

		SetHxComponents();

		builder.Services.AddTransient<AuthRedirectHandler>();

		builder.Services.AddHttpClient("Default", client =>
		{
			client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
		})
		.AddHttpMessageHandler<AuthRedirectHandler>();

		builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Default"));

		await builder.Build().RunAsync();
	}

	private static void SetHxComponents()
	{
		HxGrid.Defaults.PageSize = 15;
	}
}