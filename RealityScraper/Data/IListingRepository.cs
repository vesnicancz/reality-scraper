using RealityScraper.Model;

namespace RealityScraper.Data;
public interface IListingRepository
{
	Task<Listing> GetByExternalIdAsync(long externalId);
}