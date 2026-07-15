using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping.Builders;
using RealityScraper.Application.Features.Scraping.Configuration;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Application.Interfaces.Scraping;

namespace RealityScraper.Application.Features.Scraping;

/// <summary>
/// Hlavní služba běžící na pozadí
/// </summary>
public class ScraperServiceTask : IScheduledJob
{
	private readonly IScraperTaskRepository scraperTaskRepository;
	private readonly IEnumerable<IRealityScraperService> scraperServices;
	private readonly IScrapingReportProcessor scrapingReportProcessor;
	private readonly ScrapingReportBuilder scrapingReportBuilder;
	private readonly ILogger<ScraperServiceTask> logger;

	public ScraperServiceTask(
		IScraperTaskRepository scraperTaskRepository,
		IEnumerable<IRealityScraperService> scraperServices,
		IScrapingReportProcessor scrapingReportProcessor,
		ScrapingReportBuilder scrapingReportBuilder,
		ILogger<ScraperServiceTask> logger
		)
	{
		this.scraperTaskRepository = scraperTaskRepository;
		this.scraperServices = scraperServices;
		this.scrapingReportProcessor = scrapingReportProcessor;
		this.scrapingReportBuilder = scrapingReportBuilder;
		this.logger = logger;
	}

	public async Task ExecuteAsync(Guid taskId, CancellationToken cancellationToken)
	{
		var scraperTask = await scraperTaskRepository.GetTaskWithDetailsAsync(taskId, cancellationToken);
		if (scraperTask == null)
		{
			logger.LogError("Scraper úloha {TaskId} nebyla nalezena.", taskId);
			return;
		}

		var configuration = ScrapingConfigurationFactory.CreateFromTask(scraperTask);

		var scrapersDictionary = scraperServices.ToDictionary(s => s.ScrapersEnum);

		scrapingReportBuilder.ForScrapingReport(configuration.Id, configuration.Name);

		// Spuštění scraperů a sestavení reportu
		foreach (var scraperConfig in configuration.Scrapers)
		{
			if (!scrapersDictionary.TryGetValue(scraperConfig.ScraperType, out var scraperService))
			{
				logger.LogWarning("Scraper '{ScraperName}' nebyl nalezen.", scraperConfig.ScraperType);
				scrapingReportBuilder.MarkScraperFailed();
				continue;
			}

			logger.LogInformation("Spouštím scraper: {ScraperName}", scraperService.SiteName);
			var scraperResult = await scraperService.ScrapeListingsAsync(scraperConfig, cancellationToken);

			// Použití builderu pro zpracování výsledků
			await scrapingReportBuilder.ProcessScraperResultsAsync(scraperService.SiteName, scraperResult, cancellationToken);
		}

		// Vytvoření finálního reportu
		var report = scrapingReportBuilder.Build();

		await scrapingReportProcessor.ProcessReportAsync(report, configuration.EmailRecipients, cancellationToken);
	}
}
