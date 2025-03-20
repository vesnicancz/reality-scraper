using Microsoft.EntityFrameworkCore;
using RealityScraper.Data;
using RealityScraper.Mailing;
using RealityScraper.Scraping;
using RealityScraper.Scraping.Scrapers;

namespace RealityScraper;

internal static class Program
{
	private static async Task Main(string[] args)
	{
		var builder = Host.CreateDefaultBuilder(args)
			.ConfigureServices((hostContext, services) =>
			{
				services.AddHostedService<ScraperService>();
				services.AddDbContext<RealityDbContext>(options =>
					options.UseSqlite(hostContext.Configuration.GetConnectionString("DefaultConnection")));
				services.AddTransient<IListingRepository, ListingRepository>();
				services.AddTransient<IRealityScraperService, SRealityScraperService>();
				services.AddTransient<IRealityScraperService, RealityIdnesScraperService>();
				services.AddTransient<IEmailService, SendGridEmailService>();
				services.AddSingleton<IWebDriverFactory, ChromeDriverFactory>();
			});

		await builder.RunConsoleAsync();
	}

	// todo: reuse webdrivers/paralelizace scrapování
	// todo: vyřešit zpracování duplicit v nových inzerátech
}