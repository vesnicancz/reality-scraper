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
				services.AddHttpClient();

				// Registrace služeb pro dependency injection
				services.AddSingleton<ISchedulerRegistry, SchedulerRegistry>();
				services.AddSingleton<IJobFactory, JobFactory>();

				// registrace úloh
				services.AddTransient<ScraperServiceJob>();

				// entity framework
				services.AddDbContext<RealityDbContext>(options =>
					options.UseSqlite(hostContext.Configuration.GetConnectionString("DefaultConnection")));
				services.AddTransient<IUnitOfWork, UnitOfWork>();
				services.AddTransient<IListingRepository, ListingRepository>();

				// scrapers
				services.AddTransient<IRealityScraperService, SRealityScraperService>();
				services.AddTransient<IRealityScraperService, RealityIdnesScraperService>();

				// mailing
				services.AddTransient<IEmailService, SendGridEmailService>();
				//services.AddTransient<IEmailService, StupidEmailService>();
				services.AddTransient<IEmailGenerator, RazorEmailGenerator>();
				//services.AddTransient<IEmailGenerator, HtmlEmailGenerator>();

				// tools
				services.AddTransient<IImageDownloadService, ImageDownloadService>();
				services.AddSingleton<IWebDriverFactory, ChromeDriverFactory>();

				// Registrace hostitele služby plánovače jako hosted service
				services.AddHostedService<SchedulerHostedService>();
			});
}