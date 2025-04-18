using RealityScraper.Application;
using RealityScraper.Infrastructure;
using RealityScraper.Persistence;
using RealityScraper.Persistence.Seeding;

namespace RealityScraper.WebApi;

public static class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
		builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

		// Add services to the container.
		builder.Services.AddRazorPages();

		// Registrace závislostí
		builder.Services.AddApplicationServices();
		builder.Services.AddInfrastructureServices(builder.Configuration);
		builder.Services.AddPersistenceServices(builder.Configuration);

		var app = builder.Build();

		await DbSeeder.SeedTasksFromConfigurationAsync(app);

		// Configure the HTTP request pipeline.
		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Error");
		}

		app.UseRouting();

		app.UseAuthorization();

		app.MapStaticAssets();
		app.MapRazorPages()
		   .WithStaticAssets();
		app.MapControllers();

		app.Run();
	}
}