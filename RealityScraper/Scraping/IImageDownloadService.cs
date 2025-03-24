using RealityScraper.Model;

namespace RealityScraper.Scraping;

public interface IImageDownloadService
{
	Task DownloadImageAsync(Listing listing, CancellationToken cancellationToken);
}