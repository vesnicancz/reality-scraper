using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.Scraping;

public class ListingChangeProcessor : IListingChangeProcessor
{
	private readonly IListingRepository listingRepository;
	private readonly IDateTimeProvider dateTimeProvider;
	private readonly ILogger<ListingChangeProcessor> logger;

	public ListingChangeProcessor(
		IListingRepository listingRepository,
		IDateTimeProvider dateTimeProvider,
		ILogger<ListingChangeProcessor> logger)
	{
		this.listingRepository = listingRepository;
		this.dateTimeProvider = dateTimeProvider;
		this.logger = logger;
	}

	public async Task<List<Listing>> ProcessChangesAsync(ScrapingReport report, CancellationToken cancellationToken)
	{
		var listingsToDownload = new List<Listing>();
		var now = dateTimeProvider.UtcNow;

		foreach (var result in report.Results)
		{
			if (result.NewListings.Any())
			{
				foreach (var newListing in result.NewListings)
				{
					var listing = new Listing
					{
						Title = newListing.Title,
						Price = newListing.Price,
						Location = newListing.Location,
						Url = newListing.Url,
						ImageUrl = newListing.ImageUrl,
						ScraperTaskId = report.ScraperTaskId,
						ExternalId = newListing.ExternalId,
						CreatedAt = now,
						LastSeenAt = now,
						PriceFrom = now
					};
					listingRepository.Add(listing);
					listingsToDownload.Add(listing);
				}
			}
			if (result.PriceChangedListings.Any())
			{
				foreach (var priceChanged in result.PriceChangedListings)
				{
					var existingListing = await listingRepository.GetByExternalIdAsync(report.ScraperTaskId, priceChanged.ExternalId, cancellationToken);
					if (existingListing != null)
					{
						existingListing.PriceHistories.Add(new PriceHistory
						{
							Price = existingListing.Price,
							RecordedAt = existingListing.PriceFrom
						});
						existingListing.Price = priceChanged.Price;
						existingListing.LastSeenAt = now;
						existingListing.PriceFrom = now;
					}
				}
			}
		}

		return listingsToDownload;
	}
}