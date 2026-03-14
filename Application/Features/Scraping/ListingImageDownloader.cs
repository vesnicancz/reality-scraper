using Microsoft.Extensions.Logging;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Features.Scraping;

public class ListingImageDownloader : IListingImageDownloader
{
	private readonly IImageDownloadService imageDownloadService;
	private readonly ILogger<ListingImageDownloader> logger;

	public ListingImageDownloader(
		IImageDownloadService imageDownloadService,
		ILogger<ListingImageDownloader> logger)
	{
		this.imageDownloadService = imageDownloadService;
		this.logger = logger;
	}

	public async Task DownloadImagesAsync(List<Listing> listings, CancellationToken cancellationToken)
	{
		if (listings.Count == 0)
		{
			return;
		}

		foreach (var listing in listings)
		{
			await imageDownloadService.DownloadImageAsync(listing, cancellationToken);
		}

		logger.LogInformation("Obrázky pro {count} inzerátů byly staženy.", listings.Count);
	}
}