using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.Scraping;

public class RemovedListingDetector : IRemovedListingDetector
{
	private readonly IListingRepository listingRepository;
	private readonly IDateTimeProvider dateTimeProvider;
	private readonly ILogger<RemovedListingDetector> logger;

	public RemovedListingDetector(
		IListingRepository listingRepository,
		IDateTimeProvider dateTimeProvider,
		ILogger<RemovedListingDetector> logger)
	{
		this.listingRepository = listingRepository;
		this.dateTimeProvider = dateTimeProvider;
		this.logger = logger;
	}

	public async Task DetectAsync(ScrapingReport report, CancellationToken cancellationToken)
	{
		if (!report.ScrapingSucceeded)
		{
			logger.LogWarning("Scrapování úlohy '{TaskName}' neproběhlo celé úspěšně, detekce vyřazených inzerátů se přeskakuje.", report.TaskName);
			return;
		}

		var listings = await listingRepository.GetByScraperTaskIdAsync(report.ScraperTaskId, cancellationToken);
		var activeListings = listings.Where(l => l.RemovedAt == null).ToList();

		// Ochrana proti planému poplachu: úspěšný běh s nula inzeráty při neprázdné DB
		// spíš znamená rozbité selektory než skutečně prázdný portál.
		if (report.SeenExternalIds.Count == 0 && activeListings.Count > 0)
		{
			logger.LogWarning("Scrapování úlohy '{TaskName}' nevrátilo žádné inzeráty, ale v databázi je {Count} aktivních. Detekce vyřazených se přeskakuje.", report.TaskName, activeListings.Count);
			return;
		}

		var now = dateTimeProvider.UtcNow;
		var removedCount = 0;
		var reappearedCount = 0;

		foreach (var listing in listings)
		{
			if (report.SeenExternalIds.Contains(listing.ExternalId))
			{
				listing.LastSeenAt = now;
				if (listing.RemovedAt != null)
				{
					listing.RemovedAt = null;
					reappearedCount++;
					logger.LogInformation("Inzerát {ExternalId} se znovu objevil.", listing.ExternalId);
				}
			}
			else if (listing.RemovedAt == null)
			{
				listing.RemovedAt = now;
				removedCount++;
				logger.LogInformation("Inzerát {ExternalId} ('{Title}') byl vyřazen.", listing.ExternalId, listing.Title);
			}
		}

		if (removedCount > 0 || reappearedCount > 0)
		{
			logger.LogInformation("Úloha '{TaskName}': {RemovedCount} inzerátů vyřazeno, {ReappearedCount} se znovu objevilo.", report.TaskName, removedCount, reappearedCount);
		}
	}
}
