using Microsoft.EntityFrameworkCore;
using RealityScraper.Data;
using RealityScraper.Mailing;
using RealityScraper.Scheduler;
using RealityScraper.Scraping;
using RealityScraper.Scraping.Scrapers;

namespace RealityScraper;

internal static class Program
{
	private static async Task Main(string[] args)
	{
		await CreateHostBuilder(args)
			.RunConsoleAsync();
	}

	public static IHostBuilder CreateHostBuilder(string[] args)
		=> Host.CreateDefaultBuilder(args)
			.ConfigureServices((hostContext, services) =>
			{
				// Registrace služeb pro dependency injection
				services.AddSingleton<ISchedulerRegistry, SchedulerRegistry>();
				services.AddSingleton<IJobFactory, JobFactory>();

				// Registrace hostitele služby plánovače jako hosted service
				services.AddHostedService<SchedulerHostedService>();

				// registrace úloh
				services.AddTransient<ScraperServiceJob>();

				services.AddDbContext<RealityDbContext>(options =>
					options.UseSqlite(hostContext.Configuration.GetConnectionString("DefaultConnection")));
				services.AddTransient<IListingRepository, ListingRepository>();
				services.AddTransient<IRealityScraperService, SRealityScraperService>();
				services.AddTransient<IRealityScraperService, RealityIdnesScraperService>();
				services.AddTransient<IEmailService, SendGridEmailService>();
				services.AddSingleton<IWebDriverFactory, ChromeDriverFactory>();
			});
}