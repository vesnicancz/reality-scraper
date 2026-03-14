using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Features.Scraping;

public interface IListingChangeProcessor
{
	Task<List<Listing>> ProcessChangesAsync(ScrapingReport report, CancellationToken cancellationToken);
}