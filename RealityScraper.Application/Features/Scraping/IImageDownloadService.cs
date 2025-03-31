using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Features.Scraping;

public interface IImageDownloadService
{
	Task DownloadImageAsync(Listing listing, CancellationToken cancellationToken);
}