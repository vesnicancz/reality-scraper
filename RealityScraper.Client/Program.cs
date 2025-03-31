namespace RealityScraper.Client;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

internal class Program
{
	private static async Task Main(string[] args)
	{
		var builder = WebAssemblyHostBuilder.CreateDefault(args);
		await builder.Build().RunAsync();
	}
}