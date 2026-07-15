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
		var listings = await listingRepository.GetByScraperTaskIdAsync(report.ScraperTaskId, cancellationToken);
		var now = dateTimeProvider.UtcNow;
		var reappearedCount = 0;

		// Viděný inzerát prokazatelně existuje, takže LastSeenAt (a případný návrat
		// vyřazeného) se aktualizuje i při částečném selhání scrapování.
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
		}

		if (!report.ScrapingSucceeded)
		{
			logger.LogWarning("Scrapování úlohy '{TaskName}' neproběhlo celé úspěšně, detekce vyřazených inzerátů se přeskakuje.", report.TaskName);
			return;
		}

		// Nezpracované inzeráty (selhané selektory) nejsou v SeenExternalIds a vypadaly by
		// jako vyřazené, přestože na portálu stále existují.
		if (report.FailedListingsCount > 0)
		{
			logger.LogWarning("Scrapování úlohy '{TaskName}': {Count} inzerátů se nepodařilo zpracovat, detekce vyřazených se přeskakuje.", report.TaskName, report.FailedListingsCount);
			return;
		}

		var activeListings = listings.Where(l => l.RemovedAt == null).ToList();

		// Ochrana proti planému poplachu: úspěšný běh s nula inzeráty při neprázdné DB
		// spíš znamená rozbité selektory než skutečně prázdný portál. Kontroluje se
		// každý portál zvlášť - inzeráty v DB nenesou informaci o portálu, takže
		// prázdný výsledek kteréhokoli z nich by mohl chybně vyřadit jeho inzeráty.
		if (activeListings.Count > 0)
		{
			if (report.SeenExternalIds.Count == 0)
			{
				logger.LogWarning("Scrapování úlohy '{TaskName}' nevrátilo žádné inzeráty, ale v databázi je {Count} aktivních. Detekce vyřazených se přeskakuje.", report.TaskName, activeListings.Count);
				return;
			}

			var emptyPortals = report.Results.Where(r => r.TotalListingsCount == 0).Select(r => r.SiteName).ToList();
			if (emptyPortals.Count > 0)
			{
				logger.LogWarning("Scrapování úlohy '{TaskName}': portály {Portals} nevrátily žádné inzeráty, ale v databázi je {Count} aktivních. Detekce vyřazených se přeskakuje.", report.TaskName, string.Join(", ", emptyPortals), activeListings.Count);
				return;
			}

			// Více cílů téhož portálu se agreguje do jednoho PortalReportu - prázdný cíl
			// tak kontrola prázdných portálů nezachytí, jeho inzeráty by se chybně vyřadily.
			if (report.AnyTargetEmpty)
			{
				logger.LogWarning("Scrapování úlohy '{TaskName}': některý cíl nevrátil žádné inzeráty, ale v databázi je {Count} aktivních. Detekce vyřazených se přeskakuje.", report.TaskName, activeListings.Count);
				return;
			}
		}

		var removedCount = 0;

		foreach (var listing in listings)
		{
			if (!report.SeenExternalIds.Contains(listing.ExternalId) && listing.RemovedAt == null)
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
