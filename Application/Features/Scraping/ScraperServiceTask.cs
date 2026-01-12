using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping.Builders;
using RealityScraper.Application.Features.Scraping.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Application.Interfaces.Scraping;

namespace RealityScraper.Application.Features.Scraping;

/// <summary>
/// Hlavní služba běžící na pozadí
/// </summary>
public class ScraperServiceTask : IScheduledTask
{
	private readonly IEnumerable<IRealityScraperService> scraperServices;
	private readonly IScrapingReportProcessor scrapingReportProcessor;
	private readonly ScrapingReportBuilder scrapingReportBuilder;
	private readonly ILogger<ScraperServiceTask> logger;

	public ScraperServiceTask(
		IEnumerable<IRealityScraperService> scraperServices,
		IScrapingReportProcessor scrapingReportProcessor,
		ScrapingReportBuilder scrapingReportBuilder,
		ILogger<ScraperServiceTask> logger
		)
	{
		this.scraperServices = scraperServices;
		this.scrapingReportProcessor = scrapingReportProcessor;
		this.scrapingReportBuilder = scrapingReportBuilder;
		this.logger = logger;
	}

	public async Task ExecuteAsync(ScrapingConfiguration configuration, CancellationToken cancellationToken)
	{
		var scrapersDictionary = scraperServices.ToDictionary(s => s.ScrapersEnum);

		scrapingReportBuilder.ForScrapingReport(configuration.Id);

		// Spuštění scraperů a sestavení reportu
		foreach (var scraperConfig in configuration.Scrapers)
		{
			if (!scrapersDictionary.TryGetValue(scraperConfig.ScraperType, out var scraperService))
			{
				logger.LogWarning("Scraper '{scraperName}' not found.", scraperConfig.ScraperType);
				continue;
			}

			logger.LogInformation("Spouštím scraper: {scraperName}", scraperService.SiteName);
			var listings = await scraperService.ScrapeListingsAsync(scraperConfig, cancellationToken);

			// Použití builderu pro zpracování výsledků
			await scrapingReportBuilder.ProcessScraperResultsAsync(scraperService.SiteName, listings, cancellationToken);
		}

		logger.LogInformation("Všechny scrapery dokončeny. Zpracovávám report...");

		// Vytvoření finálního reportu
		var report = scrapingReportBuilder.Build();

		logger.LogInformation("Report vytvořen.");

		await scrapingReportProcessor.ProcessReportAsync(report, configuration.EmailRecipients, cancellationToken);
	}
}