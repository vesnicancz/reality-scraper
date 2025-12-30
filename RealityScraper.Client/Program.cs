using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace RealityScraper.Client;

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

		SetHxComponents();

		builder.Services.AddScoped(sp => new HttpClient
		{
			BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
		});

		await builder.Build().RunAsync();
	}

	private static void SetHxComponents()
	{
		HxGrid.Defaults.PageSize = 15;
	}
}