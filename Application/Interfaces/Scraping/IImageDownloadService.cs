using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Interfaces.Scraping;

public interface IImageDownloadService
{
	Task DownloadImageAsync(Listing listing, CancellationToken cancellationToken);
}