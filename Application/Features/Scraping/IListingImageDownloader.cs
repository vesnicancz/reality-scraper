using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Features.Scraping;

public interface IListingImageDownloader
{
	Task DownloadImagesAsync(List<Listing> listings, CancellationToken cancellationToken);
}